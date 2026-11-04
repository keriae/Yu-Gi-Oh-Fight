using Cysharp.Threading.Tasks;
using MDPro3.Servant;
using MDPro3.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MDPro3
{
    [CreateAssetMenu(fileName = "Items", menuName = "Scriptable Objects/Items")]
    public class Items : ScriptableObject
    {
        [Serializable]
        public struct Item
        {
            public int id;

            private bool nameLoaded;
            public string m_name;
            public string name
            {
                get
                {
                    if (!nameLoaded)
                    {
                        var listName = instance.GetName(id, m_name);
                        if (listName != STRING_NULL || string.IsNullOrEmpty(m_name))
                            m_name = listName;
                        nameLoaded = true;
                    }
                    return m_name;
                }
                set
                {
                    m_name = value;
                }
            }

            private bool descriptionLoaded;
            public string m_description;
            public string description
            {
                get
                {
                    if (!diy && !descriptionLoaded)
                    {
                        var listDescription = instance.GetDescription(id);
                        if(listDescription != STRING_NULL)
                            m_description = listDescription;
                        if(string.IsNullOrEmpty(m_description))
                            m_description = STRING_NULL;
                        descriptionLoaded = true;
                    }
                    if(diy && !descriptionLoaded)
                    {
                        if(m_description.Contains("@"))
                            m_description = InterString.Get("由「[?]」投稿。", m_description);
                    }
                    return m_description;
                }
                set
                {
                    m_description = value;
                }
            }

            public string path;
            public bool secondFace;
            public bool diy;
            public bool notReady;
        }

        public enum ItemType
        {
            Unknown,
            Wallpaper,
            Face,
            Frame,
            Protector,
            Mat,
            Grave,
            Stand,
            Mate,
            Case
        }

        public List<Item> mates;            // 100
        public List<Item> faces;              // 101
        public List<Item> frames;           // 103
        public List<Item> protectors;   // 107
        public List<Item> cases;            // 108
        public List<Item> mats;             // 109
        public List<Item> graves;           // 110
        public List<Item> stands;           // 111
        public List<Item> wallpapers;   // 113
        public List<List<Item>> kinds;

        private const int CODE_DEFAULT_DECK_CASE = 1080001;
        private const string ADDRESS_DEFAULT_DECK_CASE_LEGACY = "DeckCase0001_L";

        public const string STRING_NULL = "coming soon";
        public const int CODE_NONE = 0;
        public const int CODE_RANDOM = 9999;
        public const int CODE_SAME = 8888;
        public const int CODE_DIY = 9998;
        public const string PATH_ICON_NONE = "Menu-NoImage";
        public const string PATH_ICON_RANDOM = "Menu-Random";
        public const string PATH_ICON_SAME = "Menu-Same";
        public const string PATH_ICON_DIY = "Menu-DIY";

        public static bool initialized = false;
        private static string language = string.Empty;
        private static Items instance;
        private readonly Dictionary<int, string> names = new();
        private readonly Dictionary<int, string> descriptions = new();
        private readonly Dictionary<int, string> categories = new();
        private readonly Dictionary<string, Sprite> cachedIcons = new();
        private readonly Dictionary<string, bool> iconAddressExists = new();

        private string lastMat0;
        private string lastMat1;
        public static string lastRandomFrameID;

        #region Initialize

        public void Initialize()
        {
            if (!initialized)
            {
                instance = this;
                kinds = new List<List<Item>>()
                {
                    wallpapers,
                    faces,
                    frames,
                    protectors,
                    mats,
                    graves,
                    stands,
                    mates,
                    cases,
                };

                initialized = true;
            }
            var currentLanguage = Language.GetConfig();
            if (language != currentLanguage)
            {
                language = currentLanguage;
                Load();
            }
        }

        private void Load()
        {
            LoadText(Program.PATH_LOCALES + language + "/IDS/IDS_ITEM.txt");
            LoadText(Program.PATH_LOCALES + language + "/IDS/IDS_ITEMDESC.txt");
            LoadText(Program.PATH_LOCALES + language + "/IDS/IDS_CATEGORY.txt");
        }

        private void LoadText(string path)
        {
            int type = 0;
            if (path.EndsWith("IDS_ITEMDESC.txt"))
                type = 1;
            if (path.EndsWith("IDS_CATEGORY.txt"))
                type = 2;
            var targetDic = GetDic(type);
            targetDic.Clear();

            var text = File.ReadAllText(path);
            var lines = text.Replace("\r", string.Empty).Split('\n');

            int currentKey = 0;
            List<string> currentValue = null;
            bool notNeed = false;

            foreach (var line in lines)
            {
                if (TryParseKey(line, out var key, type))
                {
                    if (currentValue != null)
                        targetDic.Add(currentKey, string.Join(Environment.NewLine, currentValue));
                    currentKey = key;
                    currentValue = new List<string>();
                    notNeed = false;
                }
                else if (line.StartsWith("[IDS_ITEM."))// [IDS_ITEM.CATEGORY_0001] [IDS_ITEM.SPCATEGORY_0003]
                {
                    notNeed = true;
                }
                else
                {
                    if(!notNeed)
                        currentValue?.Add(line);
                }
            }

            if (currentValue != null)
            {
                targetDic.Add(currentKey, string.Join(Environment.NewLine, currentValue));
            }
        }

        private Dictionary<int, string> GetDic(int type)
        {
            return type switch
            {
                0 => names,
                1 => descriptions,
                2 => categories,
                _ => throw new ArgumentOutOfRangeException(nameof(type), "Invalid type for dictionary retrieval.")
            };
        }

        private bool TryParseKey(string line, out int key, int type)
        {
            var match = GetMatch(line, type);
            if (match.Success)
            {
                key = int.Parse(match.Groups[1].Value);
                return true;
            }
            key = 0;
            return false;
        }

        private Match GetMatch(string line, int type)
        {
            return type switch
            {
                0 => Regex.Match(line, @"^\[IDS_ITEM\.ID(\d+)\]$"),
                1 => Regex.Match(line, @"^\[IDS_ITEMDESC\.ID(\d+)\]$"),
                2 => Regex.Match(line, @"^\[IDS_CATEGORY.NAME_(\d+)\]$"),
                _ => throw new ArgumentOutOfRangeException(nameof(type), "Invalid type for match retrieval.")
            };
        }

        private string GetName(int code, string mName)
        {
            names.TryGetValue(code, out var returnValue);
            if (string.IsNullOrEmpty(returnValue))
            {
                if (string.IsNullOrEmpty(mName))
                    returnValue = STRING_NULL;
                else
                    returnValue = mName;
            }
            return Cid2Ydk.ReplaceWithCardName(returnValue);
        }

        private string GetDescription(int code)
        {
            descriptions.TryGetValue(code, out var returnValue);
            if (string.IsNullOrEmpty(returnValue))
                return STRING_NULL;
            returnValue = ReplaceWithCategory(returnValue);
            return Cid2Ydk.ReplaceWithCardName(returnValue);
        }

        private string ReplaceWithCategory(string text)
        {
            return Regex.Replace(text, @"<category id='(\d+)'/>", EvaluatorReplaceCategory);
        }

        private string EvaluatorReplaceCategory(Match match)
        {
            var key = match.Groups[1].Value;
            if (categories.TryGetValue(int.Parse(key), out var value))
                return value;
            else
                return match.Value;
        }

        #endregion

        public Item GetRandomItem(ItemType type)
        {
            var result = type switch
            {
                ItemType.Wallpaper => wallpapers[UnityEngine.Random.Range(0, wallpapers.Count)],
                ItemType.Face => faces[UnityEngine.Random.Range(0, faces.Count)],
                ItemType.Frame => frames[UnityEngine.Random.Range(0, frames.Count)],
                ItemType.Protector => protectors[UnityEngine.Random.Range(0, protectors.Count)],
                ItemType.Mat => mats[UnityEngine.Random.Range(0, mats.Count)],
                ItemType.Grave => graves[UnityEngine.Random.Range(0, graves.Count)],
                ItemType.Stand => stands[UnityEngine.Random.Range(0, stands.Count)],
                ItemType.Mate => mates[UnityEngine.Random.Range(0, mates.Count)],
                ItemType.Case => cases[UnityEngine.Random.Range(0, cases.Count)],
                _ => mats[UnityEngine.Random.Range(0, mats.Count)],
            };

            if(result.notReady)
                return GetRandomItem(type);
            else
                return result;
        }

        public string GetWallpaperPath(string code)
        {
            const string DEFAULT_WALLPAPER_PATH = "Wallpaper/Front0001";

            if(code == CODE_RANDOM.ToString())
                return GetRandomItem(ItemType.Wallpaper).path;

            foreach (var item in wallpapers)
                if (item.id.ToString() == code)
                    return item.path;
            return DEFAULT_WALLPAPER_PATH;
        }

        public string GetSameCode(ItemType type, string mapCode)
        {
            if (mapCode.Length != 7)
                mapCode = "1090001";
            if (mapCode == "1098001" && type != ItemType.Mat)
                mapCode = "1090009";
            if (mapCode == "1098002" && type != ItemType.Mat)
                mapCode = "1090003";

            if (type == ItemType.Grave)
                return "110" + mapCode[3..];
            else if (type == ItemType.Stand)
                return "111" + mapCode[3..];
            else if (type == ItemType.Mat)
            {
                lastMat1 = lastMat0;
                return lastMat0;
            }
            else
                return mapCode;
        }

        public string GetAssetPath(string code, ItemType type, int player = 0)
        {
            if(code == CODE_RANDOM.ToString())
            {
                var item = GetRandomItem(type);
                if (type == ItemType.Mat)
                {
                    if(player == 0)
                        lastMat0 = item.id.ToString();
                    else
                        lastMat1 = item.id.ToString();
                }
                return item.path;
            }
            if(type == ItemType.Mat)
            {
                if (player == 0)
                    lastMat0 = code;
                else
                    lastMat1 = code;
            }
            if (code == CODE_SAME.ToString())
                code = GetSameCode(type, player == 0 ? lastMat0 : lastMat1);

            if (type == ItemType.Unknown)
                return GetIconAddress(code);
            foreach (var kind in kinds)
                foreach (var item in kind)
                    if (item.id.ToString() == code)
                        return item.path;
            switch (type)
            {
                case ItemType.Wallpaper:
                    return wallpapers[0].path;
                case ItemType.Face:
                    return faces[0].path;
                case ItemType.Frame:
                    return frames[0].path;
                case ItemType.Protector:
                    return protectors[0].path;
                case ItemType.Mat:
                    return mats[0].path;
                case ItemType.Grave:
                    return graves[0].path;
                case ItemType.Stand:
                    return stands[0].path;
                case ItemType.Mate:
                    return mates[0].path;
                case ItemType.Case:
                    return cases[0].path;
                default:
                    return mats[0].path;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="size">0 for S, 1 for SD, 2 for HD, if exist.</param>
        /// <returns></returns>
        public static string GetIconAddress(string id, int size = 0)
        {
            if (id == CODE_RANDOM.ToString())
                return PATH_ICON_RANDOM;
            if(id == CODE_SAME.ToString())
                return PATH_ICON_SAME;
            if (id == CODE_NONE.ToString())
                return PATH_ICON_NONE;
            if (id == CODE_DIY.ToString())
                return PATH_ICON_DIY;

            var type = id[..3];
            string pathPrefix;
            string pathSuffix;
            switch (type)
            {
                case "100":
                    pathPrefix = string.Empty;
                    pathSuffix = string.Empty;
                    break;
                case "101":
                    pathPrefix = "ProfileIcon";
                    pathSuffix = size switch
                    {
                        1 => "_L_SD",
                        2 => "_L_HD",
                        _ => string.Empty
                    };
                    break;
                case "103":
                    pathPrefix = "ProfileFrame";
                    pathSuffix = size switch
                    {
                        1 => "_L_SD",
                        2 => "_L_HD",
                        _ => string.Empty
                    };
                    break;
                case "107":
                    pathPrefix = "ProtectorIcon";
                    pathSuffix = string.Empty;
                    break;
                case "108":
                    pathPrefix = "DeckCase";
                    pathSuffix = size switch
                    {
                        1 => "_L_SD",
                        2 => "_L_HD",
                        _ => string.Empty
                    };
                    break;
                case "109":
                    pathPrefix = "FieldIcon";
                    pathSuffix = size switch
                    {
                        2 => "_HD",
                        _ => "_SD"
                    };
                    break;
                case "110":
                    pathPrefix = "FieldObjIcon";
                    pathSuffix = size switch
                    {
                        2 => "_HD",
                        _ => "_SD"
                    };
                    break;
                case "111":
                    pathPrefix = "FieldAvatarBaseIcon";
                    pathSuffix = size switch
                    {
                        2 => "_HD",
                        _ => "_SD"
                    };
                    break;
                case "113":
                    pathPrefix = "WallPaperIcon";
                    pathSuffix = string.Empty;
                    break;
                default:
                    pathPrefix = string.Empty;
                    pathSuffix = string.Empty;
                    break;
            }

            if(type == "108")
                return pathPrefix + id[3..] + pathSuffix;
            else
                return pathPrefix + id + pathSuffix;
        }

        public async UniTask<Sprite> LoadItemIconAsync(string id, ItemType type)
        {
            lock (cachedIcons)
                if (cachedIcons.ContainsKey(id))
                    return cachedIcons[id];

            var handle = Addressables.LoadAssetAsync<Sprite>(GetIconAddress(id));
            var result = await handle;

            if (result == null)
                return null;

            Sprite returnValue;
            lock (cachedIcons)
            {
                if(cachedIcons.ContainsKey(id))
                {
                    returnValue = cachedIcons[id];
                    Addressables.Release(handle);
                }
                else
                {
                    returnValue = handle.Result;
                    cachedIcons.Add(id, returnValue);
                }
            }

            return returnValue;
        }

        private async UniTask<bool> HasItemIconAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
                return false;

            lock (iconAddressExists)
                if (iconAddressExists.TryGetValue(address, out var exists))
                    return exists;

            var handle = Addressables.LoadResourceLocationsAsync(address, typeof(Sprite));
            try
            {
                await handle.Task;
                return handle.Result != null && handle.Result.Count > 0;
            }
            catch
            {
                return false;
            }
            finally
            {
                lock (iconAddressExists)
                    iconAddressExists[address] = handle.IsValid() && handle.Result != null && handle.Result.Count > 0;

                if (handle.IsValid())
                    Addressables.Release(handle);
            }
        }

        public async UniTask<Sprite> TryLoadItemIconAsync(string id, ItemType type)
        {
            var address = GetIconAddress(id);
            if (!await HasItemIconAddress(address))
                return null;

            return await LoadItemIconAsync(id, type);
        }

        public async UniTask<Sprite> LoadConcreteItemIconAsync(string id, ItemType type, int player = 0)
        {
            if(id == CODE_RANDOM.ToString())
            {
                id = GetRandomItem(type).id.ToString();
                if(type == ItemType.Frame)
                    lastRandomFrameID = id;
            }

            if(id == CODE_DIY.ToString())
            {
                var path = Program.PATH_DIY;
                switch (player)
                {
                    case 0:
                        path += Appearance.meString;
                        break;
                    case 1:
                        path += Appearance.opString;
                        break;
                    case 2:
                        path += Appearance.meTagString;
                        break;
                    case 3:
                        path += Appearance.opTagString;
                        break;
                }
                if(File.Exists(path + Program.EXPANSION_PNG))
                {
                    var tex = await TextureManager.LoadPicFromFileAsync(path + Program.EXPANSION_PNG);
                    return TextureManager.Texture2Sprite(tex);
                }
                else if (File.Exists(path + Program.EXPANSION_JPG))
                {
                    var tex = await TextureManager.LoadPicFromFileAsync(path + Program.EXPANSION_JPG);
                    return TextureManager.Texture2Sprite(tex);
                }
                else
                    id = faces[0].id.ToString();
            }

            return await LoadItemIconAsync(id, type);
        }

        public bool ListHaveRandom(List<Item> target)
        {
            if(target == wallpapers) 
                return true;
            else if (target == faces)
                return true;
            else if (target == frames)
                return true;
            else if (target == protectors)
                return true;
            else if (target == mats)
                return true;
            else if (target == graves)
                return true;
            else if (target == stands)
                return true;
            else if (target == mates)
                return true;
            else if (target == cases)
                return true;
            else
                return false;
        }

        public bool ListHaveSame(List<Item> target)
        {
            if (target == graves)
                return true;
            else if (target == stands)
                return true;
            else
                return false;
        }

        public bool ListHaveNone(List<Item> target)
        {
            if (target == wallpapers)
                return true;
            else if (target == faces)
                return false;
            else if (target == frames)
                return false;
            else if (target == protectors)
                return false;
            else if (target == mats)
                return false;
            else if (target == graves)
                return false;
            else if (target == stands)
                return true;
            else if (target == mates)
                return true;
            else if (target == cases)
                return false;
            else
                return false;
        }

        public bool ListHaveDIY(List<Item> target)
        {
            if (target == faces)
                return true;
            else
                return false;
        }

        public async UniTask<Sprite> LoadDeckCaseIconAsync(int code, string suffix)
        {
            var sprite = await TryLoadAddressableSprite(GetDeckCaseAddress(code, suffix));
            if (sprite != null)
                return sprite;

            sprite = await TryLoadAddressableSprite(GetDeckCaseAddress(CODE_DEFAULT_DECK_CASE, suffix));
            if (sprite != null)
                return sprite;

            return await TryLoadAddressableSprite(ADDRESS_DEFAULT_DECK_CASE_LEGACY);
        }

        private static string GetDeckCaseAddress(int code, string suffix)
        {
            if (code < 1080000 || code > 1089999)
                code = CODE_DEFAULT_DECK_CASE;

            return $"DeckCase{code.ToString()[3..]}{suffix ?? string.Empty}";
        }

        private async UniTask<Sprite> TryLoadAddressableSprite(string address)
        {
            try
            {
                return await LoadAddressableSprite(address);
            }
            catch
            {
                return null;
            }
        }

        private async UniTask<Sprite> LoadAddressableSprite(string address)
        {
            return await Addressables.LoadAssetAsync<Sprite>(address);
        }

    }
}
