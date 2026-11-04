using Cysharp.Threading.Tasks;
using MDPro3.IO;
using MDPro3.UI.PropertyOverride;
using MDPro3.UI.ServantUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using YgomSystem.ElementSystem;

namespace MDPro3.UI
{
    public class PageLegacy : MonoBehaviour
    {

        #region Elements

        private ElementObjectManager m_Manager;
        private ElementObjectManager Manager =>
            m_Manager = m_Manager != null ? m_Manager
            : GetComponent<ElementObjectManager>();

        private const string LABEL_SR_PRESET = "ScrollRectPreset";
        private ScrollRect m_ScrollRectPreset;
        public ScrollRect ScrollRectPreset =>
            m_ScrollRectPreset = m_ScrollRectPreset != null ? m_ScrollRectPreset
            : Manager.GetElement<ScrollRect>(LABEL_SR_PRESET);

        private const string LABEL_SR_YPK = "ScrollRectYPK";
        private ScrollRect m_ScrollRectYPK;
        public ScrollRect ScrollRectYPK =>
            m_ScrollRectYPK = m_ScrollRectYPK != null ? m_ScrollRectYPK
            : Manager.GetElement<ScrollRect>(LABEL_SR_YPK);

        private const string LABEL_TXT_YPK = "TextYPK";
        private TextMeshProUGUI m_TextYPK;
        private TextMeshProUGUI TextYPK =>
            m_TextYPK = m_TextYPK != null ? m_TextYPK
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_YPK);

        private const string LABEL_IPT_NAME = "InputFieldName";
        private TMP_InputField m_InputName;
        private TMP_InputField InputName =>
            m_InputName = m_InputName != null ? m_InputName
            : Manager.GetElement<TMP_InputField>(LABEL_IPT_NAME);

        private const string LABEL_IPT_HOST = "InputFieldHost";
        private TMP_InputField m_InputHost;
        private TMP_InputField InputHost =>
            m_InputHost = m_InputHost != null ? m_InputHost
            : Manager.GetElement<TMP_InputField>(LABEL_IPT_HOST);

        private const string LABEL_IPT_PORT = "InputFieldPort";
        private TMP_InputField m_InputPort;
        private TMP_InputField InputPort =>
            m_InputPort = m_InputPort != null ? m_InputPort
            : Manager.GetElement<TMP_InputField>(LABEL_IPT_PORT);

        private const string LABEL_IPT_PASSWORD = "InputFieldPassword";
        private TMP_InputField m_InputPassword;
        private TMP_InputField InputPassword =>
            m_InputPassword = m_InputPassword != null ? m_InputPassword
            : Manager.GetElement<TMP_InputField>(LABEL_IPT_PASSWORD);

        private const string LABEL_SBN_SAVE = "ButtonSave";
        private SelectionButton m_ButtonSave;
        private SelectionButton ButtonSave =>
            m_ButtonSave = m_ButtonSave != null ? m_ButtonSave
            : Manager.GetElement<SelectionButton>(LABEL_SBN_SAVE);

        private const string LABEL_SBN_JOIN = "ButtonJoin";
        private SelectionButton m_ButtonJoin;
        private SelectionButton ButtonJoin =>
            m_ButtonJoin = m_ButtonJoin != null ? m_ButtonJoin
            : Manager.GetElement<SelectionButton>(LABEL_SBN_JOIN);

        #endregion

        private struct HostAddress
        {
            public string name;
            public string host;
            public string port;
            public string password;
        }
        private readonly List<HostAddress> addresses = new();
        private readonly string pathSaveAddress = Utility.Language.UseChinese() ? "Data/hosts.conf" : "Data/hosts2.conf";
        private SuperScrollView hostSuperScrollView;
        private bool addressedLoaded = false;

        private void Awake()
        {
            ResetLegacy();
        }

        private void ResetLegacy()
        {
            InputName.text = Config.Get("DuelPlayerName0", Config.EMPTY_STRING);
            InputHost.text = Config.Get("Host", "s1.ygo233.com");
            InputPort.text = Config.Get("Port", "233");
            InputPassword.text = Config.Get("Password", Config.EMPTY_STRING);
        }

        private void LoadHostAddresses()
        {
            if (!File.Exists(pathSaveAddress))
                return;
            var txtString = File.ReadAllText(pathSaveAddress);
            var lines = txtString.Replace("\r", string.Empty).Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var mats = Regex.Split(lines[i], " ");
                var address = new HostAddress();
                if (mats.Length >= 3)
                {
                    address.name = mats[0];
                    address.host = mats[1];
                    address.port = mats[2];
                    address.password = string.Empty;
                    if (mats.Length > 3)
                        address.password = mats[3];
                    addresses.Add(address);
                }
            }

            addressedLoaded = true;
        }

        private void SaveHostAddresses()
        {
            var content = string.Empty;
            foreach (var address in addresses)
            {
                content += address.name + " ";
                content += address.host + " ";
                content += address.port + " ";
                content += address.password + Program.STRING_LINE_BREAK;
            }
            File.WriteAllText(pathSaveAddress, content);
        }

        private void ItemOnListRefresh(string[] task, GameObject item)
        {
            var handler = item.GetComponent<SelectionToggle_Address>();
            handler.addressName = task[0];
            handler.addressHost = task[1];
            handler.addressPort = task[2];
            handler.addressPassword = task[3];
            handler.editable = task[4] == Config.STRING_YES;
            handler.Refresh();
        }

        private void AddAddress(string name)
        {
            var address = new HostAddress
            {
                name = name,
                host = InputHost.text,
                port = InputPort.text,
                password = InputPassword.text
            };
            foreach (var add in addresses)
                if (add.name == name)
                {
                    addresses.Remove(add);
                    break;
                }

            addresses.Add(address);
            SaveHostAddresses();
            PrintAddresses();
        }

        public void PrintAddresses(string search = "")
        {
            if (!addressedLoaded)
                LoadHostAddresses();

            hostSuperScrollView?.Clear();
            ypkSuperScrollView?.Clear();

            InitializeYpkServers();

            List<string[]> ypkTasks = null;
            if (ypkServerConfigs == null || ypkServerConfigs.Count == 0)
                SetYpkAddressActive(false);
            else
            {
                SetYpkAddressActive(true);
                ypkTasks = new();
                foreach(var config in ypkServerConfigs)
                {
                    if (config.ServerName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        string[] task = new string[] { config.ServerName, config.ServerHost, config.ServerPort, string.Empty, Config.STRING_NO };
                        ypkTasks.Add(task);
                    }
                }
            }

            var tasks = new List<string[]>();
            foreach (var address in addresses)
            {
                if (address.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string[] task = new string[] { address.name, address.host, address.port, address.password, Config.STRING_YES };
                    tasks.Add(task);
                }
            }

            _ = PrintAddressesAsync(tasks, ypkTasks);
        }

        private async UniTask PrintAddressesAsync(List<string[]> tasks, List<string[]> ypkTasks)
        {
            await UniTask.Yield(); // wait VerticleLayoutGroup to update
            var prefab = await Addressables.LoadAssetAsync<GameObject>("UI/ItemAddress.prefab");
            var itemWidth = PropertyOverrider.NeedMobileLayout() ? 460f : 360f;
            var itemHeight = PropertyOverrider.NeedMobileLayout() ? 80f : 40f;

            hostSuperScrollView = new SuperScrollView(
                1,
                itemWidth,
                itemHeight,
                0,
                0,
                prefab,
                ItemOnListRefresh,
                ScrollRectPreset);
            hostSuperScrollView.Print(tasks);
            if (hostSuperScrollView.items.Count > 0)
            {
                Program.instance.online.lastSelectedAddressItem
                    = hostSuperScrollView.items[0].gameObject.GetComponent<SelectionToggle_Address>();
            }

            if (ypkTasks != null)
            {
                ypkSuperScrollView = new SuperScrollView(
                    1,
                    itemWidth,
                    itemHeight,
                    0,
                    0,
                    prefab,
                    ItemOnListRefresh,
                    ScrollRectYPK);
                ypkSuperScrollView.Print(ypkTasks);
                if (hostSuperScrollView.items.Count == 0
                && ypkSuperScrollView.items.Count > 0)
                {
                    Program.instance.online.lastSelectedAddressItem
                        = ypkSuperScrollView.items[0].gameObject.GetComponent<SelectionToggle_Address>();
                }
            }

        }

        public void SetHost(string host, string port, string passwd)
        {
            InputHost.text = host;
            InputPort.text = port;
            InputPassword.text = passwd;
            OnNameChange(InputName.text);
            OnHostChange(host);
            OnPortChange(port);
            OnPasswordChange(passwd);
        }

        public void DeleteAddress(string hostName)
        {
            foreach (var address in addresses)
                if (address.name == hostName)
                {
                    addresses.Remove(address);
                    break;
                }
            SaveHostAddresses();
            PrintAddresses();
        }

        public void AddressMoveUp(string hostName)
        {
            int index = -1;
            for (int i = 0; i < addresses.Count; i++)
                if (addresses[i].name == hostName)
                {
                    index = i;
                    break;
                }
            if (index < 0)
            {
                Debug.LogError("Did not find target host.");
                return;
            }
            if (index == 0)
                return;

            var host = addresses[index];
            addresses.RemoveAt(index);
            index--;
            addresses.Insert(index, host);
            SaveHostAddresses();
            PrintAddresses();
        }

        public void OnNameChange(string name)
        {
            Config.Set("DuelPlayerName0", name == "" ? Config.EMPTY_STRING : name);
            Config.Save();
        }

        public void OnHostChange(string host)
        {
            Config.Set("Host", host);
            Config.Save();
        }

        public void OnPortChange(string port)
        {

            Config.Set("Port", port);
            Config.Save();
        }

        public void OnPasswordChange(string password)
        {
            Config.Set("Password", password == "" ? Config.EMPTY_STRING : password);
            Config.Save();
        }

        public void OnPresetSave()
        {
            var selections = new List<string>()
            {
                InterString.Get("请输入预设名称"),
                string.Empty
            };
            UIManager.ShowPopupInput(selections, AddAddress, null, TmpInputValidation.ValidationType.NoSpace);
        }

        public void OnJoin()
        {
            Program.instance.online.KF_OnlineGame(InputName.text, InputHost.text, InputPort.text, InputPassword.text);
        }

        public void SelectLastAddressItem()
        {
            if (Program.instance.online.lastSelectedAddressItem != null)
            {
                UserInput.NextSelectionIsAxis = true;
                Program.instance.online.lastSelectedAddressItem.GetSelectable().Select();
            }
        }

        public void SelectDefault()
        {
            ButtonJoin.GetSelectable().Select();
        }

        #region YPK Address

        private static bool ypkServerConfigInitialized;
        private List<MobileServerConfig> ypkServerConfigs;
        private SuperScrollView ypkSuperScrollView;

        private void SetYpkAddressActive(bool active)
        {
            TextYPK.gameObject.SetActive(active);
            ScrollRectYPK.gameObject.SetActive(active);
        }

        private void InitializeYpkServers()
        {
            if (ypkServerConfigInitialized)
                return;
            var inis = ResourceManager.GetAllIniTexts();
            ypkServerConfigs = new();
            if(inis != null)
                foreach(var ini in inis)
                    if (MobileServerConfig.TryParse(ini, out var config))
                        ypkServerConfigs.Add(config);
            ypkServerConfigInitialized = true;
        }

        public static void YpkServerSetDirty()
        {
            ypkServerConfigInitialized = false;
        }

        private class MobileServerConfig
        {
            public string ServerName { get; private set; }
            public string ServerDesc { get; private set; }
            public string ServerHost { get; private set; }
            public string ServerPort { get; private set; }

            public MobileServerConfig(string ini)
            {
                if (string.IsNullOrEmpty(ini))
                    return;

                var lines = ini.Replace("\r", string.Empty).Split('\n', System.StringSplitOptions.RemoveEmptyEntries);
                var inTargetSection = false;
                
                foreach( var rawLine in lines)
                {
                    var line = rawLine.Trim();
                    if(string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#")) 
                        continue;

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        string sectionName = line[1..^1];
                        inTargetSection = sectionName.Equals("YGOMobileAddServer", StringComparison.OrdinalIgnoreCase);
                        continue;
                    }

                    if (inTargetSection)
                    {
                        var kvp = line.Split(new char[] { '=' }, 2);
                        if (kvp.Length == 2)
                        {
                            string key = kvp[0].Trim();
                            string value = kvp[1].Trim();
                            switch (key)
                            {
                                case "ServerName":
                                    ServerName = value;
                                    break;
                                case "ServerDesc":
                                    ServerDesc = value;
                                    break;
                                case "ServerHost":
                                    ServerHost = value;
                                    break;
                                case "ServerPort":
                                    ServerPort = value;
                                    break;
                            }
                        }
                    }
                }

            }

            public static bool TryParse(string ini, out MobileServerConfig config)
            {
                config = new MobileServerConfig(ini);
                return !string.IsNullOrEmpty(config.ServerName) 
                    && !string.IsNullOrEmpty(config.ServerHost) 
                    && int.TryParse(config.ServerPort, out _);
            }

            public override string ToString()
            {
                return $"ServerName: {ServerName}, Desc: {ServerDesc}, Host: {ServerHost}, Port: {ServerPort}";
            }

        }

        public static void SelectYpkServerButton()
        {
            var instance = Program.instance.online.GetUI<OnlineServantUI>().PageLegacy;
            if (instance.ypkSuperScrollView == null || instance.ypkSuperScrollView.gameObjects.Count == 0)
                return;

            var target = instance.ypkSuperScrollView.gameObjects.GetHighestY();
            target.GetComponent<SelectionToggle_Address>().GetSelectable().Select();
        }

        public static void SelectHostServerButton()
        {
            var instance = Program.instance.online.GetUI<OnlineServantUI>().PageLegacy;
            if (instance.hostSuperScrollView == null || instance.hostSuperScrollView.gameObjects.Count == 0)
                return;

            var target = instance.hostSuperScrollView.gameObjects.GetLowestY();
            target.GetComponent<SelectionToggle_Address>().GetSelectable().Select();
        }

        #endregion

    }
}
