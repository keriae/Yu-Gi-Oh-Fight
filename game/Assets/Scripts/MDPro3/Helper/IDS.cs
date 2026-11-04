using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MDPro3.Duel.YGOSharp;

namespace MDPro3
{
    public class IDS
    {

        #region IDS_SYS

        private static Dictionary<string, string> idsSysText;
        private static readonly Dictionary<string, Dictionary<string, string>> cachedIdsDics = new();

        private static void LoadIDS(string language)
        {
            if (cachedIdsDics.TryGetValue(language, out var cachedDic))
            {
                idsSysText = cachedDic;
                return;
            }
            idsSysText = new();
            var path = $"{Program.PATH_LOCALES}{language}/IDS/IDS_SYS.txt";
            if (!File.Exists(path))
            {
                cachedIdsDics[language] = idsSysText;
                return;
            }
            var text = File.ReadAllText(path);
            var lines = text.Replace("\r", string.Empty).Split('\n', System.StringSplitOptions.RemoveEmptyEntries);

            string currentKey = null;
            string currentValue = null;

            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"(?<=\[IDS_SYS\.).*?(?=\])");
                if (match.Success)
                {
                    if (currentValue != null)
                        idsSysText[currentKey] = currentValue;
                    currentKey = match.Value;
                }
                else
                    currentValue = line;
            }

            if (currentKey != null && currentValue != null)
                idsSysText[currentKey] = currentValue;
            cachedIdsDics[language] = idsSysText;
        }

        private static string GetIdsSysText(string key, string language)
        {
            LoadIDS(language);
            if (idsSysText.TryGetValue(key, out var value))
                return value;
            return string.Empty;
        }

        public static string GetAttributeRubyText(Card data, string language)
        {
            if (data.HasType(CardType.Spell))
                return GetIdsSysText("ATTR_MAGIC_RUBY", language);
            else if (data.HasType(CardType.Trap))
                return GetIdsSysText("ATTR_TRAP_RUBY", language);
            else if (data.IsAttribute(CardAttribute.Light))
                return GetIdsSysText("ATTR_LIGHT_RUBY", language);
            else if (data.IsAttribute(CardAttribute.Dark))
                return GetIdsSysText("ATTR_DARK_RUBY", language);
            else if (data.IsAttribute(CardAttribute.Water))
                return GetIdsSysText("ATTR_WATER_RUBY", language);
            else if (data.IsAttribute(CardAttribute.Fire))
                return GetIdsSysText("ATTR_FIRE_RUBY", language);
            else if (data.IsAttribute(CardAttribute.Earth))
                return GetIdsSysText("ATTR_EARTH_RUBY", language);
            else if (data.IsAttribute(CardAttribute.Wind))
                return GetIdsSysText("ATTR_WIND_RUBY", language);
            else if (data.IsAttribute(CardAttribute.Divine))
                return GetIdsSysText("ATTR_GOD_RUBY", language);
            else
                return string.Empty;
        }

        public static string GetRaceText(int race, string language)
        {
            return race switch
            {
                26 => GetIdsSysText("RACE_MAGICAL_KNIGHT", language),
                27 => GetIdsSysText("RACE_HYDRAGON", language),
                28 => GetIdsSysText("RACE_OMEGAPSYCHO", language),
                29 => GetIdsSysText("RACE_CELESTIAL_WARRIOR", language),
                30 => GetIdsSysText("RACE_GALAXY", language),
                31 => GetIdsSysText("RACE_CYBORG", language),
                _ => string.Empty,
            };
        }

        public static string GetTypeText(int race, string language)
        {
            return race switch
            {
                0x8000 => GetIdsSysText("TYPE_MAXIMUM", language),
                _ => string.Empty,
            };
        }

        #endregion

    }
}
