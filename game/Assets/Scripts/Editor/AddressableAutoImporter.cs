using MDPro3;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public class AddressableAutoImporter : AssetPostprocessor
{
    private const string PATH_ROOT = "Assets/Addressables/";
    private const string PATH_AB_ROOT = "Assets/AddressableAssetsData/";
    private const string NAME_ICON_GROUP = "Icon";
    private const string NAME_SOUND_GROUP = "Sound";
    private const string NAME_TEXT_GROUP = "Text";
    private const string PATH_DUEL_LINKS_ICONS = "Assets/Addressables/Icon/DuelLinks/";
    private const string PATH_DUEL_LINKS_CHARA001 = PATH_DUEL_LINKS_ICONS + "Chara001/";
    private const string PATH_DUEL_LINKS_CHARA002 = PATH_DUEL_LINKS_ICONS + "Chara002/";
    private const string PATH_DUEL_LINKS_CHARA003 = PATH_DUEL_LINKS_ICONS + "Chara003/";
    private const string PATH_CHARACTERS_ASSET = "Assets/Prefabs/ScriptableObjects/Characters.asset";
    private static bool duelLinksCharacterSyncQueued;

    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedToAssets,
        string[] movedFromAssets)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return;

        bool dirty = false;

        foreach (var assetPath in importedAssets)
        {
            dirty |= AddAssetToAddressables(settings, assetPath);
            if (IsDuelLinksCharacterSprite(assetPath))
                QueueDuelLinksCharacterSync();
        }

        foreach (var assetPath in movedToAssets)
        {
            dirty |= AddAssetToAddressables(settings, assetPath);
            if (IsDuelLinksCharacterSprite(assetPath))
                QueueDuelLinksCharacterSync();
        }

        if(dirty)
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
    }

    private static AddressableAssetGroup GetGroup(AddressableAssetSettings setting, string path)
    {
        if (path.Contains("/Icon/"))
            return setting.FindGroup(NAME_ICON_GROUP);
        else if (path.Contains("/Sound/"))
            return setting.FindGroup(NAME_SOUND_GROUP);
        else if(path.Contains("/Text/"))
            return setting.FindGroup(NAME_TEXT_GROUP);
        return null;
    }

    private static string GetAddress(string path)
    {
        var value = Path.GetFileNameWithoutExtension(path);
        if (path.Contains("/SD/"))
            value += "_SD";
        if (path.Contains("/HD/"))
            value += "_HD";
        return value;
    }

    private static bool IsFolder(string path)
    {
        return AssetDatabase.IsValidFolder(path);
    }

    private static bool AddAssetToAddressables(AddressableAssetSettings settings, string assetPath)
    {
        if (IsFolder(assetPath))
            return false;
        if (!assetPath.StartsWith(PATH_ROOT)) return false;
        if (assetPath.StartsWith(PATH_AB_ROOT))
        {
            Debug.Log("Skip");
            return false;
        }

        var group = GetGroup(settings, assetPath);
        if (group == null)
        {
            Debug.LogError($"Auto Addressables: Find Group Faild: {assetPath}.");
            return false;
        }

        var guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (string.IsNullOrEmpty(guid))
            return false;

        var address = GetAddress(assetPath);
        var entry = settings.FindAssetEntry(guid);
        if (entry != null && entry.parentGroup == group && entry.address == address)
            return false;

        entry = settings.CreateOrMoveEntry(guid, group);
        entry.address = address;
        return true;
    }

    [MenuItem("Tools/MDPro3/Sync Duel Links Characters")]
    public static void SyncDuelLinksCharacters()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings != null)
        {
            var addressablesDirty = false;
            foreach (var assetPath in Directory.GetFiles(PATH_DUEL_LINKS_ICONS, "*.png", SearchOption.AllDirectories))
                addressablesDirty |= AddAssetToAddressables(settings, assetPath.Replace("\\", "/"));

            if (addressablesDirty)
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
        }

        var addedCount = SyncCharactersAsset();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Duel Links character sync complete. Added {addedCount} character(s).");
    }

    private static void QueueDuelLinksCharacterSync()
    {
        if (duelLinksCharacterSyncQueued)
            return;

        duelLinksCharacterSyncQueued = true;
        EditorApplication.delayCall += () =>
        {
            duelLinksCharacterSyncQueued = false;
            var addedCount = SyncCharactersAsset();
            if (addedCount > 0)
                AssetDatabase.SaveAssets();
        };
    }

    private static int SyncCharactersAsset()
    {
        var characters = AssetDatabase.LoadAssetAtPath<Characters>(PATH_CHARACTERS_ASSET);
        if (characters == null)
        {
            Debug.LogError($"Duel Links character sync failed: missing {PATH_CHARACTERS_ASSET}.");
            return 0;
        }

        var existingIds = new HashSet<string>();
        AddExistingIds(existingIds, characters.dm);
        AddExistingIds(existingIds, characters.gx);
        AddExistingIds(existingIds, characters._5ds);
        AddExistingIds(existingIds, characters.dsod);
        AddExistingIds(existingIds, characters.zexal);
        AddExistingIds(existingIds, characters.arcv);
        AddExistingIds(existingIds, characters.vrains);
        AddExistingIds(existingIds, characters.sevens);
        AddExistingIds(existingIds, characters.npc);
        AddExistingIds(existingIds, characters.gorush);

        var discoveredIds = DiscoverCompleteDuelLinksCharacterIds();
        var addedCount = 0;
        foreach (var id in discoveredIds)
        {
            if (existingIds.Contains(id))
                continue;

            var targetList = GetSeriesCharacters(characters, id);
            if (targetList == null)
                continue;

            targetList.Add(new Characters.SeriesCharacter { id = id });
            targetList.Sort((left, right) => string.CompareOrdinal(left.id, right.id));
            existingIds.Add(id);
            addedCount++;
        }

        if (addedCount > 0)
            EditorUtility.SetDirty(characters);

        return addedCount;
    }

    private static void AddExistingIds(HashSet<string> existingIds, List<Characters.SeriesCharacter> source)
    {
        if (source == null)
            return;

        foreach (var character in source)
            if (!string.IsNullOrEmpty(character.id))
                existingIds.Add(character.id);
    }

    private static List<string> DiscoverCompleteDuelLinksCharacterIds()
    {
        var ids = new List<string>();
        if (!Directory.Exists(PATH_DUEL_LINKS_CHARA001))
            return ids;

        foreach (var path in Directory.GetFiles(PATH_DUEL_LINKS_CHARA001, "sn????.png"))
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            if (!TryGetCharacterIdFromBaseIcon(fileName, out var id))
                continue;

            if (!IsKnownCharacterSeries(id))
                continue;

            if (!File.Exists(PATH_DUEL_LINKS_CHARA002 + "sn" + id + "_2.png"))
                continue;

            if (!File.Exists(PATH_DUEL_LINKS_CHARA003 + "sn" + id + "_3_1.png"))
                continue;

            ids.Add(id);
        }

        ids.Sort(string.CompareOrdinal);
        return ids;
    }

    private static List<Characters.SeriesCharacter> GetSeriesCharacters(Characters characters, string id)
    {
        return id[..2] switch
        {
            "00" => characters.dm,
            "01" => characters.gx,
            "02" => characters._5ds,
            "03" => characters.dsod,
            "04" => characters.zexal,
            "05" => characters.arcv,
            "06" => characters.vrains,
            "07" => characters.sevens,
            "08" => characters.npc,
            "09" => characters.gorush,
            _ => null,
        };
    }

    private static bool IsDuelLinksCharacterSprite(string path)
    {
        return path.StartsWith(PATH_DUEL_LINKS_ICONS)
            && path.EndsWith(".png")
            && (path.Contains("/Chara001/")
                || path.Contains("/Chara002/")
                || path.Contains("/Chara003/"));
    }

    private static bool TryGetCharacterIdFromBaseIcon(string fileName, out string id)
    {
        id = string.Empty;
        if (fileName.Length != 6 || !fileName.StartsWith("sn"))
            return false;

        id = fileName[2..];
        for (int i = 0; i < id.Length; i++)
            if (!char.IsDigit(id[i]))
                return false;

        return true;
    }

    private static bool IsKnownCharacterSeries(string id)
    {
        return id.Length >= 2 && int.TryParse(id[..2], out var series) && series >= 0 && series <= 9;
    }

    private void OnPostprocessTexture(Texture2D texture)
    {
        if (!assetPath.StartsWith(PATH_ROOT)) return;

        TextureImporter importer = (TextureImporter)assetImporter;
        if (importer == null) return;

        if(importer.textureType != TextureImporterType.Sprite)
        {
            EditorApplication.delayCall += () =>
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                if (TextureIsCharacterAvatar(assetPath))
                {
                    importer.isReadable = true;
                    var defaultSettings = importer.GetDefaultPlatformTextureSettings();
                    defaultSettings.format = TextureImporterFormat.RGBA32;
                    importer.SetPlatformTextureSettings(defaultSettings);
                }
                importer.SaveAndReimport();
            };
        }
    }

    private static readonly Regex Pattern = new(@"^sn\d{4}_3_\d+$");
    private static bool TextureIsCharacterAvatar(string path)
    {
        return Pattern.IsMatch(Path.GetFileNameWithoutExtension(path));
    }

}
