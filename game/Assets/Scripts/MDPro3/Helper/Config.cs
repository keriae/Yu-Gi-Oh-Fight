using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using UnityEngine;
using MDPro3.Utility;

namespace MDPro3
{
    public static class Config
    {
        public const string LABEL_TIMING = "Timing";
        private const string SEPARATOR = "->";
        public const string EMPTY_STRING = "@ui";
        public const string STRING_YES = "1";
        public const string STRING_NO = "0";

        public static uint ClientVersion = 0x1362;

        private static readonly List<OneString> translations = new List<OneString>();
        private static string path;

        public static void Initialize(string path)
        {
            Config.path = path;
            if (!File.Exists(path))
            {
                File.Create(path).Close();
                if (Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    Language.SetConfig(Language.SimplifiedChinese);
                else if (Application.systemLanguage == SystemLanguage.ChineseTraditional)
                    Language.SetConfig(Language.TraditionalChinese);
                else if (Application.systemLanguage == SystemLanguage.Spanish)
                    Language.SetConfig(Language.Spanish);
                else if (Application.systemLanguage == SystemLanguage.Japanese)
                    Language.SetConfig(Language.Japanese);
                else if (Application.systemLanguage == SystemLanguage.Korean)
                    Language.SetConfig(Language.Korean);
                else if (Application.systemLanguage == SystemLanguage.French)
                    Language.SetConfig(Language.French);
                else if (Application.systemLanguage == SystemLanguage.German)
                    Language.SetConfig(Language.German);
                else if (Application.systemLanguage == SystemLanguage.Italian)
                    Language.SetConfig(Language.Italian);
                else if (Application.systemLanguage == SystemLanguage.Portuguese)
                    Language.SetConfig(Language.Portuguese);
                else
                    Set(Language.ConfigName, Language.English);
                Save();
            }
            var txtString = File.ReadAllText(path);
            var lines = txtString.Replace("\r", "").Split('\n');
            translations.Clear();
            for (var i = 0; i < lines.Length; i++)
            {
                var mats = Regex.Split(lines[i], SEPARATOR);
                if (mats.Length == 2)
                {
                    var s = new OneString
                    {
                        original = mats[0],
                        translated = mats[1]
                    };
                    translations.Add(s);
                }
            }
        }

        public static bool Have(string original)
        {
            var found = false;
            for (var i = 0; i < translations.Count; i++)
                if (translations[i].original == original)
                {
                    found = true;
                    break;
                }
            return found;
        }

        public static string Get(string original, string defau)
        {
            var return_value = defau;
            var found = false;
            for (var i = 0; i < translations.Count; i++)
                if (translations[i].original == original)
                {
                    return_value = translations[i].translated;
                    found = true;
                    break;
                }

            if (found == false)
                if (path != null)
                {
                    File.AppendAllText(path, original + SEPARATOR + defau + YuGiOhFight.Client.Runtime.Program.STRING_LINE_BREAK);
                    var s = new OneString
                    {
                        original = original,
                        translated = defau
                    };
                    return_value = defau;
                    translations.Add(s);
                }

            return return_value.Replace(EMPTY_STRING, string.Empty);
        }

        public static void Set(string original, string setted)
        {
            var found = false;
            for (var i = 0; i < translations.Count; i++)
                if (translations[i].original == original)
                {
                    found = true;
                    translations[i].translated = setted;
                }

            if (found == false)
            {
                var s = new OneString();
                s.original = original;
                s.translated = setted;
                translations.Add(s);
            }
        }

        public static float GetFloat(string v, float defau)
        {
            var getted = 0;
            try
            {
                getted = int.Parse(Get(v, (defau * 1000).ToString()));
            }
            catch { }

            return getted / 1000f;
        }

        public static void SetFloat(string v, float f)
        {
            Set(v, ((int)(f * 1000f)).ToString());
        }

        public static bool GetBool(string original, bool value)
        {
            try
            {
                value = Get(original, value ? STRING_YES : STRING_NO) == STRING_YES;
            }
            catch { }

            return value;
        }

        public static void SetBool(string original, bool value)
        {
            Set(original, value ? STRING_YES : STRING_NO);
        }

        public static void Save()
        {
            var all = "";
            for (var i = 0; i < translations.Count; i++)
                all += translations[i].original + SEPARATOR + translations[i].translated + YuGiOhFight.Client.Runtime.Program.STRING_LINE_BREAK;

            try
            {
                File.WriteAllText(path, all);
            }
            catch (Exception e)
            {
                YuGiOhFight.Client.Runtime.Program.noAccess = true;
                Debug.Log(e);
            }
        }

        // Commit a form's changed fields together. A failed write must leave
        // both the saved file and the live configuration at their old values.
        internal static void SaveValues(IEnumerable<KeyValuePair<string, string>> values)
        {
            if (string.IsNullOrEmpty(path)) throw new IOException("配置文件尚未初始化。");
            var staged = new List<OneString>(translations.Count);
            foreach (var entry in translations)
                staged.Add(new OneString { original = entry.original, translated = entry.translated });
            foreach (var pair in values)
            {
                bool found = false;
                foreach (var entry in staged)
                    if (entry.original == pair.Key) { entry.translated = pair.Value; found = true; }
                if (!found) staged.Add(new OneString { original = pair.Key, translated = pair.Value });
            }
            var content = new StringBuilder();
            foreach (var entry in staged)
                content.Append(entry.original).Append(SEPARATOR).Append(entry.translated).Append(YuGiOhFight.Client.Runtime.Program.STRING_LINE_BREAK);
            string temporary = path + ".fight-save-" + Guid.NewGuid().ToString("N");
            try
            {
                File.WriteAllText(temporary, content.ToString());
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
                translations.Clear(); translations.AddRange(staged);
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private class OneString
        {
            public string original = "";
            public string translated = "";
        }

        public static float GetUIScale(float maxUIScale = 1.5f)
        {
            var defau = 1000f;
#if UNITY_ANDROID || UNITY_IOS
            defau = 1500f;
#endif
            var scale = float.Parse(Get("UIScale", defau.ToString())) / 1000;
            return scale > maxUIScale ? maxUIScale : scale;
        }

        public static string GetConfigDeckName(bool containType = true)
        {
            var config = Get("DeckInUse", EMPTY_STRING);
            if(config.StartsWith("/") && config.Length > 1)
                config = config[1..];
            if (!containType && config.Contains("/"))
                config = Path.GetFileName(config);
            return config;
        }

        public static void SetConfigDeck(string deckName, bool needCheck = false)
        {
            if (needCheck)
            {
                var type = YuGiOhFight.Client.Runtime.Program.instance.deckSelector.DeckType;
                var config = type == string.Empty ? deckName : $"{type}/{deckName}";
                Set("DeckInUse", config);
            }
            else
                Set("DeckInUse", deckName);
        }
    }
}
