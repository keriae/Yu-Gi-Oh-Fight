
namespace MDPro3.Utility
{
    public static class Language
    {
        public const string ConfigName = "Language";
        public const string CardConfigName = "CardLanguage";
        public const string PrereleaseName = "PrereleaseLanguage";

        public const string English = "en-US";
        public const string Spanish = "es-ES";
        public const string French = "fr-FR";
        public const string German = "de-DE";
        public const string Italian = "it-IT";
        public const string Japanese = "ja-JP";
        public const string Korean = "ko-KR";
        public const string Portuguese = "pt-PT";
        public const string SimplifiedChinese = "zh-CN";
        public const string TraditionalChinese = "zh-TW";

        private const string MasterDuelPortuguese = "pt-BR";

        public static string GetConfig()
        {
            return Config.Get(ConfigName, SimplifiedChinese);
        }

        public static void SetConfig(string language)
        {
            Config.Set(ConfigName, language);
        }

        public static string GetCardConfig()
        {
            return Config.Get(CardConfigName, SimplifiedChinese);
        }

        public static void SetCardConfig(string language)
        {
            Config.Set(CardConfigName, language);
        }

        public static string GetPrereleaseConfig()
        {
            return Config.Get(PrereleaseName, SimplifiedChinese);
        }

        public static void SetPrereleaseConfig(string language)
        {
            Config.Set(PrereleaseName, language);
        }

        public static bool NeedBlankToAddWord()
        {
            var config = GetConfig();
            if (UseLatin() || config == Korean)
                return true;
            return false;
        }

        public static string GetBlankIfNeed()
        {
            if (NeedBlankToAddWord())
                return " ";
            return string.Empty;
        }

        public static bool UseChinese()
        {
            var language = GetConfig();
            if (language == SimplifiedChinese || language == TraditionalChinese)
                return true;
            return false;
        }

        public static bool UseLatin(string language)
        {
            if (language == English
                || language == Spanish
                || language == Portuguese
                || language == French
                || language == German
                || language == Italian)
                return true;
            return false;
        }

        public static bool UseLatin()
        {
            return UseLatin(GetConfig());
        }

        public static bool CardUseLatin()
        {
            return UseLatin(GetCardConfig());
        }

        public static bool NeedSmallBracket(string language)
        {
            if (UseLatin(language) || language == Korean)
                return true;
            return false;
        }

        public static bool NeedSmallBracket()
        {
            return NeedSmallBracket(GetConfig());
        }

        public static bool CardNeedSmallBracket()
        {
            return NeedSmallBracket(GetCardConfig());
        }

        public static string GetLeftBracket()
        {
            if (NeedSmallBracket())
                return "[";
            return "【";
        }

        public static string GetRightBracket()
        {
            var language = GetConfig();
            if (UseLatin(language))
                return "]  ";
            return "】";
        }

        public static bool NeedSpSummonString(string language)
        {
            return !UseLatin(language);
        }

        public static bool NeedRaceSuffix(string language)
        {
            return language == SimplifiedChinese || language == TraditionalChinese;
        }

        public static string GetMasterDuelLanguage(string language)
        {
            if (language == Portuguese)
                return MasterDuelPortuguese;
            return language;
        }

        public static bool AttributeNeedRuby()
        {
            var language = GetCardConfig();
            if (language == SimplifiedChinese || language == TraditionalChinese)
                return false;
            return true;
        }
    }
}
