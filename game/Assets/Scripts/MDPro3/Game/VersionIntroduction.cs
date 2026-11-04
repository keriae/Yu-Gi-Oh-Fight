
using MDPro3.Utility;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

#if UNITY_STANDALONE_WIN
using Microsoft.Win32;
#endif

namespace MDPro3
{
    public static class VersionIntroduction
    {

#if UNITY_STANDALONE_WIN
        private const string ROOT_KEY_PATH = @"Software\MDPro3\MDPro3Install";
        private const string VERSION_VALUE_NAME = "Version";
        private const string INSTALL_PATH_VALUE_NAME = "InstallPath";
#endif

        private const string NOTICE_TEXT = "<#FF0000>本软件完全免费，如果您通过付费途径获取本软件，请立即进行举报。</color>";

        public static void Show()
        {
            if (NeedShow())
                Load();
        }

        private static bool NeedShow()
        {
            var language = Language.GetConfig();
            if(language != Language.SimplifiedChinese)
                return false;
            var version = Application.version;
            var configName = $"{version}Notified";
            var config = Config.GetBool(configName, false);

#if UNITY_STANDALONE_WIN
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(ROOT_KEY_PATH);
            if (key == null)
                return true;
            var registryVersion = key.GetValue(VERSION_VALUE_NAME);
            if (registryVersion == null)
                return true;
            var registryVersionString = registryVersion.ToString();
            if (string.IsNullOrEmpty(registryVersionString))
                return true;
            if (registryVersionString != version)
                return true;
#endif

            if (!config)
            {
                Config.SetBool(configName, true);
                return true;
            }

            return false;
        }

        private static void Load()
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>("UpdateContent");
            handle.Completed += (result) =>
            {
                var text = NOTICE_TEXT + "\n\n" + GetLatest(result.Result.text);

                var tasks = new List<string>()
                {
                    "新版本内容",
                    text,
                    string.Empty
                };
                UIManager.ShowPopupConfirm(tasks);
            };
        }

        private static string GetLatest(string content)
        {
            var lines = content.Replace("\r", string.Empty).Split('\n', System.StringSplitOptions.RemoveEmptyEntries);
            bool find = false;
            var result = string.Empty;
            foreach (var line in lines)
            {
                if (line.StartsWith("MDPro3 v") && find)
                    break;
                if (line.StartsWith("MDPro3 v"))
                    find = true;
                result += line + "\n";
            }
            return result;
        }
    }
}