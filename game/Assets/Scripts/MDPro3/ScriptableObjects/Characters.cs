using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.AddressableAssets;
using UnityEngine;

namespace MDPro3
{
    [CreateAssetMenu(fileName = "Characters", menuName = "Scriptable Objects/Characters")]
    public class Characters : ScriptableObject
    {
        [Serializable]
        public struct SeriesCharacter
        {
            public string id;
            public string originalId;
            public string descriptionId;
            public bool notReady;

            public readonly string GetOriginalId()
            {
                return string.IsNullOrEmpty(originalId) ? id : originalId;
            }
        }

        public List<SeriesCharacter> dm;
        public List<SeriesCharacter> gx;
        public List<SeriesCharacter> _5ds;
        public List<SeriesCharacter> dsod;
        public List<SeriesCharacter> zexal;
        public List<SeriesCharacter> arcv;
        public List<SeriesCharacter> vrains;
        public List<SeriesCharacter> sevens;
        public List<SeriesCharacter> npc;
        public List<SeriesCharacter> gorush;

        private List<List<SeriesCharacter>> characters;
        private const string CHARACTER_ADDRESS_PREFIX = "sn";
        private const int CHARACTER_ID_LENGTH = 4;

        NPC_Names names;
        NPC_Profiles profiles;

        public string language = "zh-CN";
        private static Characters instance;

        public string GetCharacterSeries(string charaID)
        {
            foreach (var c in dm)
                if (c.id == charaID)
                    return "00";
            foreach (var c in gx)
                if (c.id == charaID)
                    return "01";
            foreach (var c in _5ds)
                if (c.id == charaID)
                    return "02";
            foreach (var c in dsod)
                if (c.id == charaID)
                    return "03";
            foreach (var c in zexal)
                if (c.id == charaID)
                    return "04";
            foreach (var c in arcv)
                if (c.id == charaID)
                    return "05";
            foreach (var c in vrains)
                if (c.id == charaID)
                    return "06";
            foreach (var c in sevens)
                if (c.id == charaID)
                    return "07";
            foreach (var c in npc)
                if (c.id == charaID)
                    return "08";
            foreach (var c in gorush)
                if (c.id == charaID)
                    return "09";
            return "00";
        }

        public List<SeriesCharacter> GetSeriesCharacters(string serial)
        {
            return serial switch
            {
                "00" => dm,
                "01" => gx,
                "02" => _5ds,
                "03" => dsod,
                "04" => zexal,
                "05" => arcv,
                "06" => vrains,
                "07" => sevens,
                "08" => npc,
                "09" => gorush,
                _ => dm,
            };
        }

        public string GetCharacterOriginalId(string charaID)
        {
            foreach(var c in characters)
                foreach (var ch in c)
                    if (ch.id == charaID)
                        return ch.GetOriginalId();
            return charaID;
        }

        public string GetCharacterDescriptionId(string charaID)
        {
            foreach (var c in characters)
                foreach (var ch in c)
                    if (ch.id == charaID)
                        return ch.descriptionId;
            return charaID;
        }

        static bool initialized = false;

        public void Initialize()
        {
            if(initialized) 
                return;

            var path = Program.PATH_DATA + "DuelLinks_NPC_NAME.json";
            names = JsonConvert.DeserializeObject<NPC_Names>(File.ReadAllText(path));
            path = Program.PATH_DATA + "DuelLinks_Profile.json";
            profiles = JsonConvert.DeserializeObject<NPC_Profiles>(File.ReadAllText(path));
            EnsureCharacterLists();
            characters = new List<List<SeriesCharacter>>()
            {
                dm, gx, _5ds, dsod, zexal, arcv, vrains, sevens, npc, gorush
            };
            AutoAddAddressableCharacters();
            initialized = true;
            instance = this;
        }

        public void ChangeLanguage(string language)
        {
            this.language = language;

        }

        public string GetName(string id)
        {
            if(!initialized)
                Initialize();

            if (names.NPC_NAME.TryGetValue("NAME_ID" + id, out var data))
            {
                return language switch
                {
                    "ja-JP" => data.japanese,
                    "en-US" => data.english,
                    "fr-FR" => data.french,
                    "it-IT" => data.italian,
                    "de-DE" => data.german,
                    "es-ES" => data.spanish,
                    "pt-BR" => data.portuguese,
                    "ru-RU" => data.russian,
                    "ko-KR" => data.korean,
                    "zh-TW" => data.tChinese,
                    "zh-CN" => data.sChinese,
                    _ => data.english,
                };
            }
            else
                return "SN" + id;
        }

        private void AutoAddAddressableCharacters()
        {
            var discoveredIds = new HashSet<string>();

            foreach (var locator in Addressables.ResourceLocators)
            {
                if (locator.Keys == null)
                    continue;

                foreach (var key in locator.Keys)
                {
                    if (TryGetBaseCharacterId(key, out var id)
                        && IsKnownCharacterSeries(id)
                        && AddressableSpriteExists(CHARACTER_ADDRESS_PREFIX + id)
                        && AddressableSpriteExists(CHARACTER_ADDRESS_PREFIX + id + "_2")
                        && AddressableSpriteExists(CHARACTER_ADDRESS_PREFIX + id + "_3_1"))
                    {
                        discoveredIds.Add(id);
                    }
                }
            }

            foreach (var id in discoveredIds)
            {
                if (ContainsCharacter(id))
                    continue;

                var targetList = GetSeriesCharacters(id[..2]);
                if (targetList == null)
                    continue;

                targetList.Add(new SeriesCharacter { id = id });
                targetList.Sort((left, right) => string.CompareOrdinal(left.id, right.id));
            }
        }

        private void EnsureCharacterLists()
        {
            dm ??= new List<SeriesCharacter>();
            gx ??= new List<SeriesCharacter>();
            _5ds ??= new List<SeriesCharacter>();
            dsod ??= new List<SeriesCharacter>();
            zexal ??= new List<SeriesCharacter>();
            arcv ??= new List<SeriesCharacter>();
            vrains ??= new List<SeriesCharacter>();
            sevens ??= new List<SeriesCharacter>();
            npc ??= new List<SeriesCharacter>();
            gorush ??= new List<SeriesCharacter>();
        }

        private static bool TryGetBaseCharacterId(object key, out string id)
        {
            id = string.Empty;
            if (key is not string address)
                return false;

            if (address.Length != CHARACTER_ADDRESS_PREFIX.Length + CHARACTER_ID_LENGTH)
                return false;

            if (!address.StartsWith(CHARACTER_ADDRESS_PREFIX, StringComparison.Ordinal))
                return false;

            var possibleId = address[CHARACTER_ADDRESS_PREFIX.Length..];
            for (int i = 0; i < possibleId.Length; i++)
            {
                if (!char.IsDigit(possibleId[i]))
                    return false;
            }

            id = possibleId;
            return true;
        }

        private static bool IsKnownCharacterSeries(string id)
        {
            if (id.Length < 2)
                return false;

            return int.TryParse(id[..2], out var series) && series >= 0 && series <= 9;
        }

        private static bool AddressableSpriteExists(string address)
        {
            foreach (var locator in Addressables.ResourceLocators)
            {
                if (locator.Locate(address, typeof(Sprite), out var locations)
                    && locations != null
                    && locations.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ContainsCharacter(string id)
        {
            foreach (var series in characters)
            {
                if (series == null)
                    continue;

                foreach (var character in series)
                    if (character.id == id)
                        return true;
            }

            return false;
        }

        public string GetProfile(string id)
        {
            if (!initialized)
                Initialize();
            var value = string.Empty;

            if (profiles.PROFILE.TryGetValue("ID" + id, out var data))
            {
                value = language switch
                {
                    "ja-JP" => data.japanese,
                    "en-US" => data.english,
                    "fr-FR" => data.french,
                    "it-IT" => data.italian,
                    "de-DE" => data.german,
                    "es-ES" => data.spanish,
                    "pt-BR" => data.portuguese,
                    "ru-RU" => data.russian,
                    "ko-KR" => data.korean,
                    "zh-TW" => data.tChinese,
                    "zh-CN" => data.sChinese,
                    _ => data.english,
                };
            }

            if(string.IsNullOrEmpty(value))
            {
                var dID = GetCharacterDescriptionId(id);
                if (profiles.PROFILE.TryGetValue("ID" + dID, out var data2))
                {
                    value = language switch
                    {
                        "ja-JP" => data2.japanese,
                        "en-US" => data2.english,
                        "fr-FR" => data2.french,
                        "it-IT" => data2.italian,
                        "de-DE" => data2.german,
                        "es-ES" => data2.spanish,
                        "pt-BR" => data2.portuguese,
                        "ru-RU" => data2.russian,
                        "ko-KR" => data2.korean,
                        "zh-TW" => data2.tChinese,
                        "zh-CN" => data2.sChinese,
                        _ => data2.english,
                    };
                }
            }

            return value;
        }
    }

    [Serializable]
    public class NPC_Data
    {
        public string id;
        public string note;
        [JsonProperty("ja-JP")]
        public string japanese;
        [JsonProperty("en-US")]
        public string english;
        [JsonProperty("fr-FR")]
        public string french;
        [JsonProperty("it-IT")]
        public string italian;
        [JsonProperty("de-DE")]
        public string german;
        [JsonProperty("es-ES")]
        public string spanish;
        [JsonProperty("pt-BR")]
        public string portuguese;
        [JsonProperty("ru-RU")]
        public string russian;
        [JsonProperty("ko-KR")]
        public string korean;
        [JsonProperty("zh-TW")]
        public string tChinese;
        [JsonProperty("zh-CN")]
        public string sChinese;
        public string date;
    }

    public class NPC_Names
    {
        public Dictionary<string, NPC_Data> NPC_NAME;
    }

    public class NPC_Profiles
    {
        public Dictionary<string, NPC_Data> PROFILE;
    }
}
