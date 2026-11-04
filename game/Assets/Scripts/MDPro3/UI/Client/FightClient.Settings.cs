using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using MDPro3.AI;
using MDPro3.IO;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;
using MDPro3.Utility;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR || (!UNITY_ANDROID && !UNITY_IOS)
using SFB;
#endif

namespace MDPro3.UI.Client
{
    internal partial class FightClient
    {
        private sealed class ClientSetting
        {
            internal string Key, Label, Category, Default, SliderName;
            internal bool Boolean, Floating, Whole;
            internal float Min, Max;
            internal string[] Values, Labels;
        }
        private readonly List<ClientSetting> clientSettings = new List<ClientSetting>();
        private readonly Dictionary<string, string> settingsValues = new Dictionary<string, string>();
        private readonly Dictionary<string, string> settingsOriginal = new Dictionary<string, string>();
        private readonly Dictionary<string, string> settingsErrors = new Dictionary<string, string>();
        private readonly Dictionary<string, TMP_InputField> settingsInputs = new Dictionary<string, TMP_InputField>();
        private readonly Dictionary<string, TextMeshProUGUI> settingsErrorLabels = new Dictionary<string, TextMeshProUGUI>();
        private readonly Dictionary<string, RectTransform> settingsFieldAnchors = new Dictionary<string, RectTransform>();
        private readonly List<AiConfigField> aiHeaders = new List<AiConfigField>();
        private readonly HashSet<AiConfigField> visibleHeaders = new HashSet<AiConfigField>();
        private readonly Dictionary<AiConfigField, int> aiHeaderIds = new Dictionary<AiConfigField, int>();
        private int nextAiHeaderId;
        private ResponsesAiConfig aiSettingsDraft;
        private string aiSettingsOriginal, aiTimeout, aiOutput, aiBudget, settingsOrigin, settingsCategory = "general";
        private string settingsCondition = "Duel", aiTestStatus = "尚未测试", aiLoadError;
        private bool aiAdvanced, aiKeyVisible;
        private int settingsTestVersion;
        private string testedAiRequest, testedAiServiceStatus;
        private CancellationTokenSource settingsTest;
        private TextMeshProUGUI settingsDirtyLabel;
        private TextMeshProUGUI settingsAiTestLabel;
        private float settingsScrollPosition = 1;
        private ScrollRect settingsScroll;
        internal bool SettingsDirty => settingsValues.Any(pair => !settingsOriginal.TryGetValue(pair.Key, out var previous) || pair.Value != previous)
            || (aiSettingsDraft != null && AiDraftSignature() != aiSettingsOriginal);

        internal void OpenSettings(string category = "general", string origin = null)
        {
            if (clientSettings.Count == 0) BuildClientSettings();
            if (settingsValues.Count == 0 || page != "settings" && !SettingsDirty) LoadSettingsDraft();
            settingsCategory = category; settingsOrigin = origin; settingsScrollPosition = 1;
            Navigate("settings");
        }

        private void BuildClientSettings()
        {
            AddChoice("Language", "界面语言", "general", Language.GetConfig(), LocaleValues());
            AddChoice("CardLanguage", "卡片语言", "general", Language.GetCardConfig(), LocaleValues());
            AddBool("Confirm", "确认按钮位置", "general", false);
            AddBool("Rumble", "手柄震动", "general", true);
            AddChoice("Layout", "布局方式", "general", "0", new[] { "0", "1000", "2000" }, new[] { "自动判断", "桌面布局", "移动布局" });
            var cardStyles = Enum.GetNames(typeof(CardRenderer.CardStyle));
            AddChoice("CardStyle", "卡面风格", "general", CardRenderer.CardStyle.OCG_TCG.ToString(), cardStyles,
                cardStyles.Select(style => style == "OCG_TCG" ? "标准卡面（OCG / TCG）" : style == "RUSH_DUEL" ? "超速决斗（RUSH DUEL）" : style).ToArray());
            AddBool("VideoCard", "动态卡图", "general", true);
            AddBool("BGMbyMySide", "背景音乐依据我方场地", "general", true);
            AddBool("SyncDeck", "卡组同步", "general", true);
            var backgrounds = BackgroundManager.backgrounds;
            AddChoice("Background", "背景", "general", "0", new[] { "0" }.Concat(backgrounds.Keys.Select(key => key.ToString())).ToArray(),
                new[] { "随机" }.Concat(backgrounds.Values).ToArray());
            AddChoice("ScreenMode", "显示模式", "graphics", "1", new[] { "0", "1", "2" }, new[] { "独占全屏（Windows）", "窗口全屏", "窗口化（桌面）" });
            AddChoice("Resolution", "分辨率", "graphics", Screen.width + " x " + Screen.height, ResolutionValues());
            AddNumber("Scale", "渲染比例", "graphics", DeviceInfo.OnMobile() ? .5f : 1, .1f, 2, false, "ButtonScale");
            AddNumber("Quality", "画质", "graphics", 2, 0, 5, true, "ButtonQuality");
            AddNumber("FAA", "快速抗锯齿", "graphics", 1, 1, 1, true, "ButtonFAA");
            AddNumber("AAA", "抗锯齿模式", "graphics", 0, 0, 5, true, "ButtonAAA");
            AddNumber("Shadow", "阴影质量", "graphics", 0, 0, 4, true, "ButtonShadow");
            AddNumber("FPS", "帧率上限", "graphics", DeviceInfo.OnMobile() ? 30 : 60, 0, 240, true, "ButtonFPS");
            AddBool("ShowFPS", "显示 FPS", "graphics", true);
            AddNumber("BgmVol", "背景音乐音量", "graphics", .7f, 0, 1, false, "ButtonBGM");
            AddNumber("SEVol", "音效音量", "graphics", .7f, 0, 1, false, "ButtonSE");
            AddNumber("VoiceVol", "角色语音音量", "graphics", .7f, 0, 1, false, "ButtonVoice");
            foreach (var condition in new[] { "Duel", "Watch", "Replay" })
            {
                foreach (var pair in new[] { "Voice:角色语音", "Closeup:近景演出", "Pendulum:灵摆演出", "Summon:召唤动画", "Cutin:卡片特写", "Effect:卡片效果演出", "Chain:连锁演出", "Dice:骰子演出", "Coin:硬币演出", "AutoInfo:自动卡片详情", "FaceDown:盖卡信息", "PlayerMessage:玩家消息", "SystemMessage:系统消息", "AutoAcc:自动加速" })
                {
                    var entry = pair.Split(':'); AddBool(condition + entry[0], entry[1], "duel", entry[0] != "Voice" && entry[0] != "Closeup");
                }
                AddNumber(condition + "Acc", "加速倍率", "duel", 2, 1, 5, false, "Button" + condition + "Acc");
            }
            AddBool(Config.LABEL_TIMING, "响应时机始终开启", "duel", false);
            AddBool("Expansions", "本地扩展支持", "data", true);
        }
        private string[] LocaleValues() => Directory.Exists(Program.PATH_LOCALES)
            ? Directory.GetDirectories(Program.PATH_LOCALES).Select(Path.GetFileName).Where(name => name != "ko-KRIDS").ToArray()
            : new[] { Language.GetConfig() };
        private string[] ResolutionValues()
        {
            var values = new List<string> { Screen.width + " x " + Screen.height };
            foreach (var resolution in Screen.resolutions)
            {
                int width = resolution.width, height = resolution.height;
                if (DeviceInfo.OnMobile())
                {
                    if (height > width) (width, height) = (height, width);
                    foreach (var target in new[] { 540, 720, 1080, 1200, 1440, 1600, 1800, 2160 })
                        if (height > target) values.Add(width * target / height + " x " + target);
                }
                values.Add(width + " x " + height);
            }
            return values.Distinct().ToArray();
        }
        private void AddBool(string key, string label, string category, bool value) => clientSettings.Add(new ClientSetting { Key = key, Label = label, Category = category, Default = value ? "1" : "0", Boolean = true });
        private void AddChoice(string key, string label, string category, string value, string[] values, string[] labels = null)
            => clientSettings.Add(new ClientSetting { Key = key, Label = label, Category = category, Default = value, Values = values,
                Labels = labels ?? (key == "Language" || key == "CardLanguage" ? values.Select(name => InterString.Get(name)).ToArray() : values) });
        private void AddNumber(string key, string label, string category, float value, float min, float max, bool whole, string original)
        {
            var field = new ClientSetting { Key = key, Label = label, Category = category, Default = ((int)(value * 1000)).ToString(CultureInfo.InvariantCulture), Floating = true, Min = min, Max = max, Whole = whole, SliderName = original };
            // The bundled original control remains the authority for non-integer ranges.
            // Reading its serialized Slider does not attach its immediate-save callbacks.
            try
            {
                var ui = Program.instance.setting.GetUI<SettingServantUI>();
                if (ui != null)
                {
                    var control = typeof(SettingServantUI).GetProperty(original, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(ui);
                    var slider = control == null ? null : typeof(SelectionButton_Setting).GetField("slider", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(control) as Slider;
                    if (slider != null && key != "FAA") { field.Min = key == "FPS" ? 0 : slider.minValue; field.Max = slider.maxValue; field.Whole = slider.wholeNumbers; }
                }
            }
            catch { /* Runtime assets may not be loaded yet; the source-supported range remains available. */ }
            clientSettings.Add(field);
        }
        private void LoadSettingsDraft()
        {
            settingsValues.Clear(); settingsOriginal.Clear(); settingsErrors.Clear(); aiLoadError = null;
            foreach (var field in clientSettings) settingsValues[field.Key] = settingsOriginal[field.Key] = Config.Have(field.Key) ? Config.Get(field.Key, field.Default) : field.Default;
            settingsValues["LocalDuelPort"] = settingsOriginal["LocalDuelPort"] = Config.Have("LocalDuelPort") ? Config.Get("LocalDuelPort", SoloSelector.port) : SoloSelector.port;
            try { aiSettingsDraft = ResponsesAiConfigStore.Load(); }
            catch (AiConfigurationException error) { aiSettingsDraft = new ResponsesAiConfig(); aiLoadError = error.Message; }
            aiTimeout = aiSettingsDraft.TimeoutSeconds.ToString(); aiOutput = aiSettingsDraft.MaxOutputTokens.ToString(); aiBudget = aiSettingsDraft.PromptBudgetTokens.ToString();
            aiHeaders.Clear(); visibleHeaders.Clear(); aiHeaderIds.Clear();
            try { aiHeaders.AddRange(AiConfigForm.ReadHeaders(aiSettingsDraft.ExtraHeadersJson)); }
            catch (AiConfigurationException error) { settingsErrors["headers"] = error.Message; }
            aiSettingsOriginal = AiDraftSignature(); aiTestStatus = "尚未测试";
        }
        private string AiDraftSignature() => aiSettingsDraft == null ? "" : string.Join("\n", aiSettingsDraft.BaseUrl, aiSettingsDraft.ApiKey, aiSettingsDraft.Model,
            aiSettingsDraft.RememberApiKey, aiSettingsDraft.ReasoningEffort, aiTimeout, aiOutput, aiBudget, aiSettingsDraft.ExtraBodyJson,
            string.Join("\n", aiHeaders.Select(field => field.Name + "\0" + field.Value)));
        private static string AiRequestSignature(ResponsesAiConfig config) => string.Join("\n", config.BaseUrl, config.ApiKey, config.Model,
            config.ReasoningEffort, config.TimeoutSeconds, config.MaxOutputTokens, config.PromptBudgetTokens, config.ExtraHeadersJson, config.ExtraBodyJson);
        internal string GetAiServiceStatus(ResponsesAiConfig saved)
        {
            try { saved.Validate(); }
            catch (AiConfigurationException) { return "未配置"; }
            return testedAiRequest == AiRequestSignature(saved) ? testedAiServiceStatus : "已配置未测试";
        }
        private void SettingsChanged(string key, string value)
        {
            settingsValues[key] = value; settingsErrors.Remove(key); UpdateSettingsDirtyLabel();
            ClearSettingsFieldError(key);
        }
        private void AiSettingsChanged(string key = null)
        {
            CancelSettingsTest(); aiTestStatus = "尚未测试（请求配置已修改）";
            if (settingsAiTestLabel != null) { settingsAiTestLabel.text = aiTestStatus; settingsAiTestLabel.color = FightUI.Muted; }
            var cleared = key == null ? settingsErrors.Keys.Where(IsAiSettingsKey).ToArray() : new[] { key, "ai" };
            foreach (var errorKey in cleared) { settingsErrors.Remove(errorKey); ClearSettingsFieldError(errorKey); }
            if (key != null && (key == "headers" || key.StartsWith("header:", StringComparison.Ordinal)))
            {
                // Duplicate names involve two rows. Repairing either row must clear
                // the other row's obsolete error without hiding an unrelated error.
                var previous = settingsErrors.Keys.Where(errorKey => errorKey == "headers" || errorKey.StartsWith("header:", StringComparison.Ordinal)).ToArray();
                var current = AiHeaderErrors();
                foreach (var errorKey in previous)
                {
                    if (current.TryGetValue(errorKey, out var issue))
                    {
                        settingsErrors[errorKey] = issue;
                        if (settingsErrorLabels.TryGetValue(errorKey, out var label) && label != null) label.text = "! " + issue;
                    }
                    else { settingsErrors.Remove(errorKey); ClearSettingsFieldError(errorKey); }
                }
            }
            UpdateSettingsDirtyLabel();
        }
        private static bool IsAiSettingsKey(string key) => new[] { "baseUrl", "apiKey", "model", "reasoning", "timeout", "output", "budget", "headers", "body", "ai" }.Contains(key)
            || key.StartsWith("header:", StringComparison.Ordinal);
        private string AiHeaderKey(AiConfigField field, string part)
        {
            if (!aiHeaderIds.TryGetValue(field, out var id)) aiHeaderIds[field] = id = ++nextAiHeaderId;
            return "header:" + id + ":" + part;
        }
        private Dictionary<string, string> AiHeaderErrors()
        {
            var errors = new Dictionary<string, string>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in aiHeaders)
            {
                if (string.IsNullOrEmpty(field.Name) && string.IsNullOrEmpty(field.Value)) continue;
                string nameKey = AiHeaderKey(field, "name");
                if (!string.IsNullOrEmpty(field.Name) && !names.Add(field.Name)) errors[nameKey] = "请求头名称不能重复（不区分大小写）。";
                if (string.IsNullOrWhiteSpace(field.Name)) errors[nameKey] = "请填写请求头名称。";
                else
                {
                    try { AiConfigForm.WriteHeaders(new[] { new AiConfigField { Name = field.Name, Value = "" } }); }
                    catch (AiConfigurationException error) { errors[nameKey] = error.Message; }
                }
                if (field.Value != null && field.Value.Any(char.IsControl)) errors[AiHeaderKey(field, "value")] = "请求头值不能包含换行或控制字符。";
            }
            return errors;
        }
        private void ClearSettingsFieldError(string key)
        {
            if (settingsErrorLabels.TryGetValue(key, out var label) && label != null) label.text = "";
            if (settingsInputs.TryGetValue(key, out var input) && input != null) input.GetComponent<Outline>().effectColor = FightUI.Border;
        }
        private void UpdateSettingsDirtyLabel()
        {
            if (settingsDirtyLabel == null) return;
            settingsDirtyLabel.text = SettingsDirty ? "● 有未保存的修改" : "";
            settingsDirtyLabel.color = SettingsDirty ? FightUI.Gold : FightUI.Muted;
        }

        internal void RenderSettings()
        {
            if (clientSettings.Count == 0) BuildClientSettings();
            if (settingsValues.Count == 0) LoadSettingsDraft();
            FightUI.Clear(Body); settingsInputs.Clear(); settingsErrorLabels.Clear(); settingsFieldAnchors.Clear();
            settingsLayoutAiLeft = settingsLayoutAiRight = null;
            float sidebar = SettingsSidebarWidth, inset = SettingsWorkspaceInset, headingHeight = SettingsHeadingHeight;
            var formWidth = SettingsFormWidth(BodyWidth - sidebar, settingsCategory == "ai" && aiAdvanced);
            var workspaceX = (BodyWidth - sidebar - formWidth) / 2;
            var workspace = FightUI.Panel(Body, workspaceX, 0, sidebar + formWidth, BodyHeight, FightUI.PanelColor, true, "Settings workspace");
            workspace.GetComponent<Outline>().effectColor = FightUI.Border;
            settingsLayoutWorkspace = workspace;
            var rail = FightUI.Panel(workspace, 0, 0, sidebar, BodyHeight, new Color(.025f, .041f, .05f, .56f), false, "Settings navigation");
            settingsLayoutRail = rail;
            settingsLayoutRailIcon = FightUI.Icon(rail, "settings", 22, 27, 22, 22, FightUI.Gold).rectTransform;
            settingsLayoutRailTitle = FightUI.Text(rail, "设置", 56, 20, sidebar - 72, 40, 23, FightUI.Gold, TextAlignmentOptions.MidlineLeft).rectTransform;
            settingsLayoutRailDivider = FightUI.Panel(rail, 20, headingHeight - 1, sidebar - 40, 1, FightUI.Border, false, "Settings navigation divider");
            settingsLayoutMainDivider = FightUI.Panel(workspace, sidebar, 0, 1, BodyHeight, FightUI.Border, false, "Settings content divider");
            settingsLayoutMainDivider.GetComponent<Image>().raycastTarget = false;
            var categories = new[] { "general", "graphics", "duel", "ai", "data", "about" };
            var titles = new[] { "通用", "画面与声音", "对局与演出", "AI 服务", "数据与资源", "关于" };
            for (var i = 0; i < categories.Length; i++)
            {
                var category = categories[i];
                var selected = settingsCategory == category;
                var button = FightUI.Button(rail, titles[i], 12, headingHeight + 12 + i * 56, sidebar - 24, 46,
                    () => { settingsCategory = category; settingsScrollPosition = 1; RenderSettings(); }, false, settingsTest == null);
                button.GetComponent<Outline>().enabled = false;
                button.GetComponent<Image>().color = selected ? FightUI.Raised : Color.clear;
                var label = button.GetComponentInChildren<TextMeshProUGUI>(); label.alignment = TextAlignmentOptions.MidlineLeft; label.color = selected ? FightUI.Active : FightUI.Muted;
                label.fontSize = 18;
                FightUI.Place(label.rectTransform, 18, 0, sidebar - 60, 46);
                if (selected) FightUI.Panel(button.transform, 0, 10, 2, 26, FightUI.Gold, false);
            }
            var title = titles[Mathf.Max(0, Array.IndexOf(categories, settingsCategory))];
            var innerWidth = Mathf.Min(settingsCategory == "ai" && aiAdvanced ? float.PositiveInfinity : settingsCategory == "ai" ? 920 : 880, formWidth - inset * 2 - 24);
            var readerWidth = innerWidth + 24;
            var readerInset = (formWidth - readerWidth) / 2;
            var readerX = sidebar + readerInset;
            var heading = FightUI.Text(workspace, title, readerX, 20, readerWidth - 260, 44, Compact ? 28 : 32, FightUI.Gold, TextAlignmentOptions.MidlineLeft);
            heading.fontStyle = FontStyles.Bold; heading.characterSpacing = .6f;
            settingsLayoutHeading = heading;
            settingsDirtyLabel = FightUI.Text(workspace, "", readerX + readerWidth - 260, 26, 260, 32, 15, FightUI.Muted, TextAlignmentOptions.MidlineRight); UpdateSettingsDirtyLabel();
            settingsLayoutHeadingDivider = FightUI.Panel(workspace, readerX, headingHeight - 1, readerWidth, 1, FightUI.Border, false, "Settings heading divider");
            var actionsY = BodyHeight - SettingsActionsHeight;
            var actions = FightUI.Node(workspace, "Settings actions", sidebar, actionsY, formWidth, SettingsActionsHeight);
            settingsLayoutActions = actions;
            settingsLayoutActionsDivider = FightUI.Panel(actions, readerInset, 0, readerWidth, 1, FightUI.Border, false, "Settings action divider");
            var viewportHeight = Mathf.Max(160, actionsY - headingHeight - 24);
            var content = FightUI.Scroll(workspace, readerX, headingHeight + 12, readerWidth, viewportHeight, 2600, out settingsScroll);
            settingsScroll.GetComponent<Image>().color = Color.clear;
            float y = 8;
            if (settingsCategory == "ai") RenderAiSettings(content, innerWidth, ref y);
            else if (settingsCategory == "data") RenderDataSettings(content, innerWidth, ref y);
            else if (settingsCategory == "about") RenderAboutSettings(content, innerWidth, ref y);
            else
            {
                if (settingsCategory == "duel")
                {
                    var conditions = new[] { "Duel", "Watch", "Replay" }; var labels = new[] { "对战", "观战", "回放" };
                    for (var i = 0; i < 3; i++) { var condition = conditions[i]; FightUI.Tab(content, (condition == settingsCondition ? "◆ " : "") + labels[i], condition == settingsCondition, i * 152, y, 144, 44, () => { settingsCondition = condition; settingsScrollPosition = 1; RenderSettings(); }); }
                    y += 60;
                    y += SettingsWrappedText(content, "情境项分别保存；下方全局操作适用于所有情境。", 0, y, innerWidth, 32, 17, FightUI.Muted).rectTransform.sizeDelta.y + 16;
                    FightUI.Button(content, "收藏 · 外观与角色", 0, y, 240, 48, () => Navigate("appearance")); y += 68;
                }
                foreach (var field in clientSettings.Where(field => field.Category == settingsCategory && (settingsCategory != "duel" || !field.Key.StartsWith("Duel") && !field.Key.StartsWith("Watch") && !field.Key.StartsWith("Replay") || field.Key.StartsWith(settingsCondition))))
                {
                    string section = field.Key == "Language" ? "语言与阅读" : field.Key == "Confirm" ? "操作与表现"
                        : field.Key == "ScreenMode" ? "显示与画质" : field.Key == "BgmVol" ? "声音"
                        : field.Key == settingsCondition + "Voice" ? "对局演出" : field.Key == settingsCondition + "AutoInfo" ? "信息与响应"
                        : field.Key == Config.LABEL_TIMING ? "全局操作" : null;
                    if (section != null) { SettingsSection(content, innerWidth, section, ref y); }
                    RenderSettingField(content, innerWidth, field, ref y);
                }
                if (settingsCategory == "general")
                {
                    FightUI.Button(content, "管理默认卡组", 0, y, 240, 48, () => Navigate("decks")); y += 68;
                    SettingsSection(content, innerWidth, "高级启动参数", ref y);
                    RenderSettingsInput(content, innerWidth, "LocalDuelPort", "本地决斗端口", settingsValues["LocalDuelPort"], "1–65535", ref y, value => SettingsChanged("LocalDuelPort", value));
                }
            }
            content.sizeDelta = new Vector2(readerWidth, Mathf.Max(y + 24, viewportHeight));
            settingsScroll.verticalNormalizedPosition = settingsScrollPosition;
            settingsScroll.onValueChanged.AddListener(_ => settingsScrollPosition = settingsScroll.verticalNormalizedPosition);
            if (settingsCategory == "about" || settingsCategory == "data")
                settingsLayoutReset = null;
            else settingsLayoutReset = FightUI.Button(actions, "恢复本页默认", readerInset, 16, 170, 48, RestoreSettingsDefaults, false, settingsTest == null).GetComponent<RectTransform>();
            var cancelLabel = settingsOrigin == null ? "取消修改" : "返回对战准备";
            var saveLabel = settingsOrigin == null ? "保存设置" : "保存并返回对战";
            settingsLayoutCancel = FightUI.Button(actions, cancelLabel, readerInset + readerWidth - 400, 16, 180, 48, () =>
            {
                if (settingsOrigin == null) { DiscardSettings(); RenderSettings(); }
                else Navigate(settingsOrigin);
            }).GetComponent<RectTransform>();
            settingsLayoutSave = FightUI.Button(actions, saveLabel, readerInset + readerWidth - 208, 16, 208, 48, () =>
            {
                if (!SaveSettings()) return;
                if (settingsOrigin != null) Navigate(settingsOrigin); else RenderSettings();
            }, true, settingsTest == null && settingsCategory != "about").GetComponent<RectTransform>();
            var workspaceFrame = workspace.Find("Metal frame");
            if (workspaceFrame != null) workspaceFrame.SetAsLastSibling();
            CaptureSettingsViewportLayout(content);
        }
        private void SettingsSection(Transform parent, float width, string label, ref float y)
        {
            if (y > 16) y += 18;
            var t = FightUI.Text(parent, label, 0, y, width, 30, 18, FightUI.Gold); t.fontStyle = FontStyles.Bold;
            FightUI.Line(parent, 0, y + 34, width, new Color(.2f, .28f, .33f, .42f)); y += 48;
        }
        private void RenderSettingField(Transform parent, float width, ClientSetting field, ref float y)
        {
            var value = settingsValues[field.Key];
            float labelWidth = Mathf.Min(240, width * .27f), controlX = labelWidth + 16, controlWidth = width - controlX;
            if (field.Boolean)
            {
                var enabled = value == "1";
                var booleanLabel = field.Key == "Confirm" ? field.Label + " · " + (enabled ? "左侧" : "右侧") : field.Label;
                var row = FightUI.SwitchRow(parent, booleanLabel, enabled, 0, y, width, 54,
                    () => { SettingsChanged(field.Key, enabled ? "0" : "1"); RenderSettings(); });
                var label = row.GetComponentInChildren<TextMeshProUGUI>();
                label.enableWordWrapping = true; label.maxVisibleLines = 2;
                float rowHeight = Mathf.Max(54, label.GetPreferredValues(booleanLabel, width - 90, float.PositiveInfinity).y + 4);
                FightUI.ReflowSwitchRow(row, width, rowHeight, 0);
                if (field.Key == "Confirm") FightUI.Hint(row.gameObject, "设置原对局确认弹窗中确认按钮的左右位置。保存后生效。");
                settingsFieldAnchors[field.Key] = row.GetComponent<RectTransform>();
                y += rowHeight + 8;
            }
            else if (field.Values != null)
            {
                var label = SettingsFieldLabel(parent, field.Label, 0, y + 3, labelWidth, 48);
                var index = Array.IndexOf(field.Values, value); var display = index < 0 ? value : field.Labels[index];
                var choice = FightUI.Button(parent, display, controlX, y + 2, controlWidth, 46, () => SettingsChoose(field.Label, field.Values, field.Labels, next =>
                {
                    if (field.Key == "ScreenMode" && (DeviceInfo.OnMobile() && next != "1" || next == "0" && Application.platform != RuntimePlatform.WindowsPlayer && Application.platform != RuntimePlatform.WindowsEditor)) { ShowStatus("此显示模式仅在对应桌面平台可用。"); return; }
                    SettingsChanged(field.Key, next); RenderSettings();
                }, value));
                StyleSettingsChoice(choice, controlWidth, 46);
                settingsFieldAnchors[field.Key] = choice.GetComponent<RectTransform>();
                y += Mathf.Max(68, label.rectTransform.sizeDelta.y + 20);
            }
            else
            {
                var label = SettingsFieldLabel(parent, field.Label, 0, y + 3, labelWidth, 48);
                var number = ParseStoredFloat(value); var display = value.StartsWith("invalid:", StringComparison.Ordinal) ? value.Substring(8) : number.ToString("0.###", CultureInfo.InvariantCulture);
                if (field.Min == field.Max) display = "关闭";
                Slider slider = null; TMP_InputField input = null;
                var hint = SettingsWrappedText(parent, SettingsNumberHint(field, number), controlX, y + 50, controlWidth, 26, 15, FightUI.Muted);
                if (field.Min < field.Max)
                {
                    float errorHeight = new[] { float.NaN, field.Max + 1, field.Key == "FPS" ? 1 : field.Min - 1 }
                        .Max(sample => hint.GetPreferredValues(SettingsNumberError(field, sample) ?? "", controlWidth, float.PositiveInfinity).y + 2);
                    hint.rectTransform.sizeDelta = new Vector2(controlWidth, Mathf.Max(hint.rectTransform.sizeDelta.y, errorHeight));
                }
                var valueRow = FightUI.Node(parent, "Setting value " + field.Key, controlX, y + 2, controlWidth, 44);
                input = FightUI.Input(valueRow, display, field.Min + "–" + field.Max, 0, 0, 94, 44, text =>
                {
                    bool parsed = float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var next);
                    string issue = parsed ? SettingsNumberError(field, next) : "请输入有效数值";
                    if (issue == null)
                    {
                        SettingsChanged(field.Key, ((int)(next * 1000)).ToString(CultureInfo.InvariantCulture));
                        if (slider != null) slider.SetValueWithoutNotify(field.Key == "FPS" && next == 0 ? 29 : next);
                    }
                    else { settingsValues[field.Key] = "invalid:" + text; settingsErrors.Remove(field.Key); ClearSettingsFieldError(field.Key); UpdateSettingsDirtyLabel(); }
                    hint.text = issue ?? SettingsNumberHint(field, next); hint.color = issue == null ? FightUI.Muted : FightUI.Danger;
                    if (input != null) input.GetComponent<Outline>().effectColor = issue == null ? FightUI.Border : FightUI.Danger;
                }); input.richText = false; input.textComponent.richText = false; settingsInputs[field.Key] = input;
                input.interactable = field.Min < field.Max;
                FightUI.Hint(input.gameObject, SettingsNumberRangeHint(field));
                settingsFieldAnchors[field.Key] = input.GetComponent<RectTransform>();
                slider = CreateClientSettingsSlider(valueRow, controlWidth - 110, 2, field, number, changed =>
                {
                    if (field.Key == "FPS" && changed == 29) changed = 0;
                    SettingsChanged(field.Key, ((int)(changed * 1000)).ToString(CultureInfo.InvariantCulture));
                    input.SetTextWithoutNotify(changed.ToString("0.###", CultureInfo.InvariantCulture));
                    hint.text = SettingsNumberHint(field, changed); hint.color = FightUI.Muted;
                }, 110);
                FightUI.Hint(slider.gameObject, SettingsNumberRangeHint(field));
                if (value.StartsWith("invalid:", StringComparison.Ordinal)) { hint.text = SettingsNumberDraftError(field, value) ?? "请输入有效数值"; hint.color = FightUI.Danger; input.GetComponent<Outline>().effectColor = FightUI.Danger; }
                y += Mathf.Max(Mathf.Max(80, label.rectTransform.sizeDelta.y + 18), 50 + hint.rectTransform.sizeDelta.y + 8);
            }
            RenderSettingsError(parent, width, field.Key, ref y); FightUI.Line(parent, 0, y + 2, width, new Color(.2f, .28f, .33f, .20f)); y += 8;
        }
        private TextMeshProUGUI SettingsFieldLabel(Transform parent, string value, float x, float y, float width, float height)
        {
            var label = FightUI.Text(parent, value, x, y, width, height, 19, FightUI.Foreground, TextAlignmentOptions.MidlineLeft);
            if (label.GetPreferredValues(value, float.PositiveInfinity, float.PositiveInfinity).x > width)
            {
                label.enableWordWrapping = true; label.maxVisibleLines = 2;
                float lineHeight = label.GetPreferredValues("国", float.PositiveInfinity, float.PositiveInfinity).y;
                label.rectTransform.sizeDelta = new Vector2(width, Mathf.Max(height, Mathf.Min(lineHeight * 2 + 4, label.GetPreferredValues(value, width, float.PositiveInfinity).y + 2)));
            }
            FightUI.Hint(label.gameObject, value); return label;
        }
        private void StyleSettingsChoice(Button choice, float width, float height)
        {
            choice.GetComponent<Outline>().effectColor = FightUI.Border;
            var caption = choice.GetComponentInChildren<TextMeshProUGUI>(); caption.alignment = TextAlignmentOptions.MidlineLeft; caption.color = FightUI.Foreground;
            FightUI.Place(caption.rectTransform, 14, 0, width - 48, height);
            FightUI.Hint(caption.gameObject, caption.text);
            FightUI.Icon(choice.transform, "chevron", width - 30, (height - 15) / 2, 15, 15, FightUI.Gold);
        }
        private static string SettingsNumberError(ClientSetting field, float number)
        {
            if (float.IsNaN(number) || float.IsInfinity(number)) return "请输入有效数值";
            if (field.Key == "FPS" && number != 0 && number < 30) return "请输入 0 或 30–" + field.Max + " 之间的整数";
            if (number < field.Min || number > field.Max || field.Whole && number != Mathf.Round(number))
                return "请输入 " + field.Min.ToString(CultureInfo.InvariantCulture) + "–" + field.Max.ToString(CultureInfo.InvariantCulture) + (field.Whole ? " 之间的整数" : " 范围内的数值");
            return null;
        }
        private static string SettingsNumberDraftError(ClientSetting field, string stored)
        {
            if (!stored.StartsWith("invalid:", StringComparison.Ordinal)) return SettingsNumberError(field, ParseStoredFloat(stored));
            return float.TryParse(stored.Substring(8), NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                ? SettingsNumberError(field, number) : "请输入有效数值";
        }
        private static string SettingsNumberHint(ClientSetting field, float number)
        {
            if (field.Key.EndsWith("Vol") || field.Key == "Scale") return float.IsNaN(number) ? "" : Mathf.RoundToInt(number * 100) + "%";
            if (field.Key == "FAA") return "当前引擎关闭 MSAA，此项不可调整";
            if (field.Key == "FPS") return number == 0 ? "无限制" : "FPS · 输入 0 为无限制";
            if (field.Key.EndsWith("Acc")) return "倍";
            string current = null;
            if (!float.IsNaN(number) && number == Mathf.Round(number))
            {
                int index = (int)number;
                if (field.Key == "Quality" && index >= 0 && index <= 5) current = new[] { "非常低", "低", "中等", "高", "非常高", "极致" }[index];
                if (field.Key == "Shadow" && index >= 0 && index <= 4) current = new[] { "非常低", "低", "中等", "高", "非常高" }[index];
                if (field.Key == "AAA" && index >= 0 && index <= 5) current = new[] { "关闭", "FXAA", "SMAA 低", "SMAA 中", "SMAA 高", "TAA" }[index];
            }
            return current ?? "";
        }
        private static string SettingsNumberRangeHint(ClientSetting field)
        {
            if (field.Key == "FAA") return "当前引擎关闭 MSAA，此项不可调整。";
            if (field.Key == "FPS") return "输入 0 为无限制，或 30–" + field.Max.ToString(CultureInfo.InvariantCulture) + " 之间的整数。";
            return "范围 " + field.Min.ToString(CultureInfo.InvariantCulture) + "–" + field.Max.ToString(CultureInfo.InvariantCulture)
                + (field.Whole ? "，仅限整数。" : "。");
        }
        private Slider CreateClientSettingsSlider(Transform parent, float width, float y, ClientSetting field, float value, Action<float> changed, float x = 0)
        {
            var area = FightUI.Node(parent, "Slider " + field.Key, x, y, width, 40);
            var track = FightUI.Panel(area, 0, 18, width, 3, FightUI.Border, false);
            var fill = FightUI.Panel(track, 0, 0, width, 3, FightUI.Gold, false);
            fill.sizeDelta = Vector2.zero; fill.anchoredPosition = Vector2.zero;
            var handles = FightUI.Node(area, "Handle area", 8, 0, width - 16, 40);
            var handle = FightUI.Panel(handles, 0, 10, 16, 20, FightUI.Active, false);
            handle.pivot = new Vector2(.5f, .5f); handle.sizeDelta = new Vector2(16, -20); handle.anchoredPosition = Vector2.zero;
            var slider = area.gameObject.AddComponent<Slider>();
            slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight; slider.minValue = field.Key == "FPS" ? 29 : field.Min;
            slider.maxValue = field.Max; slider.wholeNumbers = field.Whole;
            slider.value = float.IsNaN(value) ? slider.minValue : field.Key == "FPS" && value == 0 ? 29 : value;
            slider.interactable = slider.minValue < slider.maxValue;
            slider.onValueChanged.AddListener(valueChanged => changed(valueChanged)); return slider;
        }
        private static float ParseStoredFloat(string value) => int.TryParse(value, out var number) ? number / 1000f : float.NaN;
        private void RenderSettingsInput(Transform parent, float width, string key, string label, string value, string hint, ref float y, Action<string> changed, bool password = false, bool multiline = false, float height = 48, bool showHint = true)
        {
            FightUI.Text(parent, label, 0, y, width, 28, 19); y += 30;
            var input = FightUI.Input(parent, value, hint, 0, y, width, height, changed, password, multiline);
            input.richText = false; input.isRichTextEditingAllowed = false; input.textComponent.richText = false;
            if (input.placeholder is TMP_Text placeholder) placeholder.richText = false;
            settingsInputs[key] = input; y += height + 8;
            settingsFieldAnchors[key] = input.GetComponent<RectTransform>();
            FightUI.Hint(input.gameObject, hint);
            if (settingsErrors.ContainsKey(key)) input.GetComponent<Outline>().effectColor = FightUI.Danger;
            if (showHint && !string.IsNullOrEmpty(hint)) y += SettingsWrappedText(parent, hint, 0, y, width, 32, 16, FightUI.Muted).rectTransform.sizeDelta.y + 2;
            RenderSettingsError(parent, width, key, ref y); y += 12;
        }
        private TextMeshProUGUI SettingsWrappedText(Transform parent, string value, float x, float y, float width, float height, float size, Color color)
        {
            var text = FightUI.Text(parent, value, x, y, width, height, size, color);
            text.enableWordWrapping = true;
            text.rectTransform.sizeDelta = new Vector2(width, Mathf.Max(height, text.GetPreferredValues(value, width, float.PositiveInfinity).y + 2));
            return text;
        }
        private void RenderSettingsError(Transform parent, float width, string key, ref float y, float x = 0)
        {
            if (!settingsErrors.TryGetValue(key, out var error)) return;
            var label = FightUI.Text(parent, "! " + error, x, y, width, 34, 17, FightUI.Danger);
            label.enableWordWrapping = true;
            float height = Mathf.Max(30, label.GetPreferredValues(label.text, width, float.PositiveInfinity).y + 4);
            label.rectTransform.sizeDelta = new Vector2(width, height); settingsErrorLabels[key] = label;
            if (!settingsFieldAnchors.ContainsKey(key)) settingsFieldAnchors[key] = label.rectTransform;
            if (settingsInputs.TryGetValue(key, out var input)) input.GetComponent<Outline>().effectColor = FightUI.Danger;
            y += height + 8;
        }
        private void SettingsChoose(string title, string[] values, string[] labels, Action<string> chosen, string current = null)
        {
            bool searchable = values.Length > 8;
            float height = Mathf.Min(600, 140 + (searchable ? 64 : 0) + Mathf.Max(1, values.Length) * 52);
            var modal = ShowDialog(title, 560, height);
            float listTop = searchable ? 136 : 72, viewportHeight = height - listTop - 68;
            var content = FightUI.Scroll(modal, 24, listTop, 512, viewportHeight, values.Length * 52, out var scroll);
            Action<string> renderOptions = query =>
            {
                FightUI.Clear(content);
                var indices = Enumerable.Range(0, values.Length).Where(index => string.IsNullOrWhiteSpace(query)
                    || (labels[index] ?? "").IndexOf(query.Trim(), StringComparison.OrdinalIgnoreCase) >= 0
                    || (values[index] ?? "").IndexOf(query.Trim(), StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                int selectedIndex = -1;
                for (int row = 0; row < indices.Length; row++)
                {
                    int index = indices[row]; var value = values[index]; bool selected = value == current;
                    var button = FightUI.FocusKey(FightUI.Button(content, labels[index], 8, row * 52, 496, 44, () => { CloseDialog(); chosen(value); }), "settings:choice:" + title + ":" + value);
                    button.GetComponent<Outline>().enabled = false; button.GetComponent<Image>().color = selected ? FightUI.Raised : row % 2 == 0 ? new Color(.04f, .075f, .10f, .55f) : Color.clear;
                    var text = button.GetComponentInChildren<TextMeshProUGUI>(); text.alignment = TextAlignmentOptions.MidlineLeft; text.color = selected ? FightUI.Active : FightUI.Foreground;
                    FightUI.Place(text.rectTransform, 16, 0, 422, 44);
                    if (selected) { selectedIndex = row; FightUI.Panel(button.transform, 0, 10, 2, 24, FightUI.Gold, false); FightUI.Text(button.transform, "✓", 452, 0, 28, 44, 20, FightUI.Active, TextAlignmentOptions.Center); }
                    FightUI.Hint(text.gameObject, labels[index]);
                }
                if (indices.Length == 0) FightUI.Text(content, "没有匹配项，请尝试其他关键词。", 16, 12, 480, 64, 18, FightUI.Muted, TextAlignmentOptions.Center);
                content.sizeDelta = new Vector2(512, Mathf.Max(viewportHeight, Mathf.Max(1, indices.Length) * 52));
                scroll.verticalNormalizedPosition = selectedIndex >= 0 && string.IsNullOrEmpty(query)
                    ? Mathf.Clamp01(1 - Mathf.Max(0, selectedIndex * 52 - 52) / Mathf.Max(1, content.sizeDelta.y - viewportHeight)) : 1;
            };
            if (searchable) FightUI.Input(modal, "", "搜索名称或当前值", 24, 74, 512, 46, renderOptions);
            renderOptions("");
            FightUI.Button(modal, "取消", 368, height - 58, 160, 44, CloseDialog);
        }

        private void RenderAiSettings(Transform parent, float width, ref float y)
        {
            FightUI.Tab(parent, (!aiAdvanced ? "◆ " : "") + "连接", !aiAdvanced, 0, y, 160, 44, () => { aiAdvanced = false; settingsScrollPosition = 1; RenderSettings(); }, settingsTest == null);
            FightUI.Tab(parent, (aiAdvanced ? "◆ " : "") + "高级", aiAdvanced, 172, y, 160, 44, () => { aiAdvanced = true; settingsScrollPosition = 1; RenderSettings(); }, settingsTest == null);
            FightUI.Line(parent, 0, y + 52, width, FightUI.Border); y += 76;
            if (aiLoadError != null) { FightUI.Text(parent, aiLoadError, 0, y, width, 60, 17, FightUI.Danger); y += 72; }
            if (!aiAdvanced)
            {
                float connectionWidth = Mathf.Min(width, 920);
                var panel = FightUI.Node(parent, "AI connection settings", 0, y, connectionWidth, 570);
                var form = FightUI.Node(panel, "Connection Fields", 0, 0, connectionWidth, 540);
                float fw = connectionWidth, fy = 0;
                SettingsSection(form, fw, "连接参数", ref fy);
                RenderSettingsInput(form, fw, "baseUrl", "服务地址", aiSettingsDraft.BaseUrl, "支持根地址、/v1 或完整 /responses 地址", ref fy, value => { aiSettingsDraft.BaseUrl = value; AiSettingsChanged("baseUrl"); });
                RenderSettingsInput(form, fw - 100, "apiKey", "API Key", aiSettingsDraft.ApiKey, "密钥仅用于请求认证", ref fy, value => { aiSettingsDraft.ApiKey = value; AiSettingsChanged("apiKey"); }, !aiKeyVisible, showHint: false);
                var keyInput = settingsInputs["apiKey"];
                FightUI.FocusKey(FightUI.Button(form, aiKeyVisible ? "隐藏" : "显示", fw - 88, -keyInput.GetComponent<RectTransform>().anchoredPosition.y, 88, 48, () => { aiKeyVisible = !aiKeyVisible; RenderSettings(); }), "settings:ai:key-visibility");
                RenderSettingsInput(form, fw, "model", "模型", aiSettingsDraft.Model, "填写服务支持的模型名称", ref fy, value => { aiSettingsDraft.Model = value; AiSettingsChanged("model"); }, showHint: false);
                FightUI.Line(form, 0, fy, fw, FightUI.GoldBorder); fy += 16;
                var remember = FightUI.SwitchRow(form, "记住密钥和额外请求配置", aiSettingsDraft.RememberApiKey, 0, fy, fw, 48, () => { aiSettingsDraft.RememberApiKey = !aiSettingsDraft.RememberApiKey; UpdateSettingsDirtyLabel(); RenderSettings(); });
                fy += 60;
                fy += SettingsWrappedText(form, "关闭时仅保留本次运行；保存关闭记忆会移除已存凭据。", 0, fy, fw, 48, 16, FightUI.Muted).rectTransform.sizeDelta.y + 10;
                panel.sizeDelta = new Vector2(connectionWidth, fy + 32); y += fy + 48;
            }
            else
            {
                bool stacked = Compact || width < 1050;
                float columnWidth = stacked ? width : (width - AiSettingsPaneGap) / 2, paneY = y;
                var left = FightUI.Node(parent, "AI request settings", 0, paneY, columnWidth, 430);
                var right = FightUI.Node(parent, "AI extra settings", stacked ? 0 : columnWidth + AiSettingsPaneGap, paneY, columnWidth, 430);
                settingsLayoutAiLeft = left; settingsLayoutAiRight = right;
                float inner = columnWidth - 32, ly = 14;
                ModuleDivider(left, "推理与预算", 16, ly, inner, 22); ly += 62;
                var efforts = new[] { "", "none", "minimal", "low", "medium", "high", "xhigh", "max" };
                var labels = new[] { "默认", "无（none）", "最小（minimal）", "低", "中", "高", "极高（xhigh）", "最大（max）" };
                var effortIndex = Array.IndexOf(efforts, aiSettingsDraft.ReasoningEffort ?? "");
                var reasoningRow = AiSettingsFieldRow(left, inner, "reasoning", "思考强度", ly);
                float controlX = AiSettingsLabelWidth(inner) + 20, controlWidth = AiSettingsControlWidth(inner);
                var effort = FightUI.FocusKey(FightUI.Button(reasoningRow, effortIndex < 0 ? aiSettingsDraft.ReasoningEffort : labels[effortIndex], controlX, 0, controlWidth, AiSettingsFieldHeight,
                    () => SettingsChoose("思考强度", efforts, labels, value => { aiSettingsDraft.ReasoningEffort = value; AiSettingsChanged("reasoning"); RenderSettings(); }, aiSettingsDraft.ReasoningEffort ?? "")), "settings:ai:reasoning");
                StyleSettingsChoice(effort, controlWidth, AiSettingsFieldHeight); settingsFieldAnchors["reasoning"] = effort.GetComponent<RectTransform>(); ly += AiSettingsRowHeight;
                RenderSettingsError(left, inner, "reasoning", ref ly, 16);
                AiBudgetField(left, inner, "timeout", "请求超时", "秒", aiTimeout, ref ly, value => { aiTimeout = value; AiSettingsChanged("timeout"); });
                AiBudgetField(left, inner, "output", "输出上限", "tokens", aiOutput, ref ly, value => { aiOutput = value; AiSettingsChanged("output"); });
                AiBudgetField(left, inner, "budget", "上下文预算", "tokens", aiBudget, ref ly, value => { aiBudget = value; AiSettingsChanged("budget"); });
                ly += SettingsWrappedText(left, "上下文预算须高于实际输出，并预留至少 1024 tokens。", 16, ly + 4, inner, 42, 16, FightUI.Muted).rectTransform.sizeDelta.y + 20;
                left.sizeDelta = new Vector2(columnWidth, ly + 16);

                float ry = 14;
                var headers = FightUI.Node(right, "AI request headers", 16, ry, inner, 200);
                float hy = 0;
                ModuleDivider(headers, "额外请求头", 0, hy, inner, 22); hy += 62;
                float nameWidth = Mathf.Min(220, inner * .34f), valueX = nameWidth + 16, valueWidth = inner - nameWidth - 176;
                if (aiHeaders.Count == 0) { FightUI.Text(headers, "暂无额外请求头", 0, hy, inner, 28, 16, FightUI.Muted); hy += 44; }
                else
                {
                    FightUI.Text(headers, "名称", 0, hy, nameWidth, 24, 15, FightUI.Muted);
                    FightUI.Text(headers, "值", valueX, hy, valueWidth, 24, 15, FightUI.Muted); hy += 30;
                }
                foreach (var field in aiHeaders.ToArray())
                {
                    string nameKey = AiHeaderKey(field, "name"), valueKey = AiHeaderKey(field, "value");
                    var row = FightUI.Node(headers, "AI header " + nameKey, 0, hy, inner, AiSettingsFieldHeight);
                    var name = FightUI.FocusKey(FightUI.Input(row, field.Name, "名称", 0, 0, nameWidth, AiSettingsFieldHeight, value => { field.Name = value; AiSettingsChanged(nameKey); }), "settings:ai:" + nameKey);
                    var headerValue = FightUI.FocusKey(FightUI.Input(row, field.Value, "值", valueX, 0, valueWidth, AiSettingsFieldHeight, value => { field.Value = value; AiSettingsChanged(valueKey); }, !visibleHeaders.Contains(field)), "settings:ai:" + valueKey);
                    settingsInputs[nameKey] = name; settingsInputs[valueKey] = headerValue;
                    settingsFieldAnchors[nameKey] = name.GetComponent<RectTransform>(); settingsFieldAnchors[valueKey] = headerValue.GetComponent<RectTransform>();
                    FightUI.Hint(name.gameObject, "请求头名称：使用 HTTP 名称，不能重复或包含空格与换行。");
                    FightUI.Hint(headerValue.gameObject, "请求头值：纯文本，不能包含换行或控制字符。");
                    FightUI.FocusKey(FightUI.Button(row, visibleHeaders.Contains(field) ? "隐藏" : "显示", inner - 144, 0, 72, AiSettingsFieldHeight, () => { if (!visibleHeaders.Add(field)) visibleHeaders.Remove(field); RenderSettings(); }), "settings:ai:" + nameKey + ":visibility");
                    FightUI.FocusKey(FightUI.Button(row, "删除", inner - 64, 0, 64, AiSettingsFieldHeight, () => { aiHeaders.Remove(field); visibleHeaders.Remove(field); AiSettingsChanged(nameKey); AiSettingsChanged(valueKey); RenderSettings(); }), "settings:ai:" + nameKey + ":delete"); hy += AiSettingsRowHeight;
                    RenderSettingsError(headers, inner, nameKey, ref hy);
                    RenderSettingsError(headers, inner, valueKey, ref hy);
                }
                RenderSettingsError(headers, inner, "headers", ref hy);
                FightUI.Button(headers, "+ 添加请求头", 0, hy, 200, AiSettingsFieldHeight, () => { aiHeaders.Add(new AiConfigField()); AiSettingsChanged("headers"); RenderSettings(); }); hy += AiSettingsFieldHeight;
                headers.sizeDelta = new Vector2(inner, hy);
                ry += hy + 32;
                var json = FightUI.Node(right, "AI JSON parameters", 16, ry, inner, 320);
                float jy = 0;
                ModuleDivider(json, "额外 JSON 参数", 0, jy, inner, 22); jy += 62;
                const string jsonHint = "支持嵌套对象与数组；不能覆盖模型、输入、格式或会话。";
                var bodyInput = FightUI.FocusKey(FightUI.Input(json, aiSettingsDraft.ExtraBodyJson, "{}", 0, jy, inner, AiSettingsJsonHeight,
                    value => { aiSettingsDraft.ExtraBodyJson = value; AiSettingsChanged("body"); }, false, true), "settings:ai:body");
                settingsInputs["body"] = bodyInput; settingsFieldAnchors["body"] = bodyInput.GetComponent<RectTransform>();
                FightUI.Hint(bodyInput.gameObject, jsonHint);
                if (settingsErrors.ContainsKey("body")) bodyInput.GetComponent<Outline>().effectColor = FightUI.Danger;
                jy += AiSettingsJsonHeight + 12;
                jy += SettingsWrappedText(json, jsonHint, 0, jy, inner, 42, 16, FightUI.Muted).rectTransform.sizeDelta.y + 12;
                RenderSettingsError(json, inner, "body", ref jy);
                json.sizeDelta = new Vector2(inner, jy);
                float rightHeight = ry + jy + 16;
                FightUI.Place(right, stacked ? 0 : columnWidth + AiSettingsPaneGap, stacked ? paneY + left.rect.height + AiSettingsPaneGap : paneY, columnWidth, rightHeight);
                y = paneY + (stacked ? left.rect.height + AiSettingsPaneGap + rightHeight : Mathf.Max(left.rect.height, rightHeight)) + 24;
            }
            y += SettingsWrappedText(parent, "测试检查 Responses 与结构化输出协议；修改在下一场生效。", 0, y, width, 42, 16, FightUI.Muted).rectTransform.sizeDelta.y + 6;
            var testButton = FightUI.Button(parent, settingsTest == null ? "测试连接" : "取消测试", 0, y, 180, 44, TestSettingsConnection);
            var testColor = aiTestStatus.StartsWith("测试通过", StringComparison.Ordinal) ? FightUI.Gold
                : aiTestStatus.StartsWith("尚未测试", StringComparison.Ordinal) || aiTestStatus.StartsWith("正在测试", StringComparison.Ordinal) || aiTestStatus == "连接测试已取消" ? FightUI.Muted : FightUI.Danger;
            settingsAiTestLabel = FightUI.Text(parent, aiTestStatus, 200, y + 4, width - 200, 48, 17, testColor);
            settingsAiTestLabel.enableWordWrapping = true;
            float statusHeight = Mathf.Max(48, Mathf.Min(160, settingsAiTestLabel.GetPreferredValues(aiTestStatus, width - 200, float.PositiveInfinity).y + 4));
            settingsAiTestLabel.rectTransform.sizeDelta = new Vector2(width - 200, statusHeight); FightUI.Hint(settingsAiTestLabel.gameObject, aiTestStatus); y += statusHeight + 18;
            RenderSettingsError(parent, width, "ai", ref y);
            if (settingsTest != null)
            {
                foreach (var input in parent.GetComponentsInChildren<TMP_InputField>()) input.interactable = false;
                foreach (var button in parent.GetComponentsInChildren<Button>()) if (button != testButton) button.interactable = false;
            }
        }
        private float AiSettingsLabelWidth(float width) => Mathf.Min(168, width * .3f);
        private float AiSettingsControlWidth(float width) => Mathf.Min(320, width - AiSettingsLabelWidth(width) - 112);
        private RectTransform AiSettingsFieldRow(Transform parent, float width, string key, string label, float y)
        {
            var row = FightUI.Node(parent, "AI setting " + key, 16, y, width, AiSettingsFieldHeight);
            FightUI.Text(row, label, 0, 0, AiSettingsLabelWidth(width), AiSettingsFieldHeight, 18, FightUI.Foreground, TextAlignmentOptions.MidlineLeft);
            return row;
        }
        private void AiBudgetField(Transform parent, float width, string key, string label, string unit, string value, ref float y, Action<string> changed)
        {
            var row = AiSettingsFieldRow(parent, width, key, label, y);
            float controlX = AiSettingsLabelWidth(width) + 20, controlWidth = AiSettingsControlWidth(width);
            var input = FightUI.FocusKey(FightUI.Input(row, value, label, controlX, 0, controlWidth, AiSettingsFieldHeight, changed), "settings:ai:" + key);
            FightUI.Text(row, unit, controlX + controlWidth + 12, 0, 80, AiSettingsFieldHeight, 15, FightUI.Muted, TextAlignmentOptions.MidlineLeft);
            settingsInputs[key] = input; y += AiSettingsRowHeight;
            settingsFieldAnchors[key] = input.GetComponent<RectTransform>();
            RenderSettingsError(parent, width, key, ref y, 16);
        }

        private ResponsesAiConfig ReadAiSettings()
        {
            settingsErrors.Clear();
            var next = aiSettingsDraft.Clone();
            if (!int.TryParse(aiTimeout, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timeout) || timeout < 5 || timeout > 600) settingsErrors["timeout"] = "请输入 5–600 之间的整数秒数。"; else next.TimeoutSeconds = timeout;
            if (!int.TryParse(aiOutput, NumberStyles.Integer, CultureInfo.InvariantCulture, out var output) || output < 128 || output > 128000) settingsErrors["output"] = "请输入 128–128000 之间的整数。"; else next.MaxOutputTokens = output;
            if (!int.TryParse(aiBudget, NumberStyles.Integer, CultureInfo.InvariantCulture, out var budget) || budget < 8192 || budget > 2000000) settingsErrors["budget"] = "请输入 8192–2000000 之间的整数。"; else next.PromptBudgetTokens = budget;
            try { next.Endpoint(); } catch (AiConfigurationException error) { settingsErrors["baseUrl"] = error.Message; }
            foreach (var error in AiHeaderErrors()) settingsErrors[error.Key] = error.Value;
            if (!settingsErrors.Keys.Any(key => key.StartsWith("header:", StringComparison.Ordinal)))
                try { next.ExtraHeadersJson = AiConfigForm.WriteHeaders(aiHeaders); } catch (AiConfigurationException error) { settingsErrors["headers"] = error.Message; }
            try { next.Body(); } catch (AiConfigurationException error) { settingsErrors["body"] = error.Message; }
            if (string.IsNullOrWhiteSpace(next.Model) || next.Model.Length > 256 || next.Model.Any(char.IsControl)) settingsErrors["model"] = "请填写有效模型名称。";
            if (next.ApiKey != null && (next.ApiKey.Length > 16384 || next.ApiKey.Any(char.IsControl))) settingsErrors["apiKey"] = "密钥格式无效。";
            if (settingsErrors.Count == 0)
            {
                try { next.Validate(); }
                catch (AiConfigurationException error)
                {
                    var key = error.Message.Contains("上下文") ? "budget" : error.Message.Contains("密钥") ? "apiKey" : error.Message.Contains("思考") ? "reasoning" : "body";
                    settingsErrors[key] = error.Message;
                }
            }
            if (settingsErrors.Count != 0) { FocusSettingsError(); return null; }
            return next;
        }
        private void FocusSettingsError()
        {
            var key = settingsErrors.Keys.First(); settingsCategory = "ai";
            aiAdvanced = key == "timeout" || key == "output" || key == "budget" || key == "body" || key == "headers" || key == "reasoning" || key.StartsWith("header:", StringComparison.Ordinal);
            settingsScrollPosition = 1; RenderSettings();
            FocusRenderedSettingsField(key);
            ShowStatus(settingsErrors[key]);
        }
        private void FocusRenderedSettingsField(string key)
        {
            if (settingsScroll == null || !settingsFieldAnchors.TryGetValue(key, out var anchor) || anchor == null) return;
            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4]; anchor.GetWorldCorners(corners);
            float top = -settingsScroll.content.InverseTransformPoint(corners[1]).y;
            float available = Mathf.Max(1, settingsScroll.content.rect.height - settingsScroll.viewport.rect.height);
            settingsScroll.verticalNormalizedPosition = Mathf.Clamp01(1 - Mathf.Max(0, top - 40) / available);
            if (settingsInputs.TryGetValue(key, out var input)) { input.Select(); input.ActivateInputField(); }
        }
        private async void TestSettingsConnection()
        {
            if (settingsTest != null) { CancelSettingsTest(); aiTestStatus = "连接测试已取消"; RenderSettings(); return; }
            var config = ReadAiSettings(); if (config == null) return;
            var cancel = new CancellationTokenSource(); settingsTest = cancel; var version = ++settingsTestVersion;
            aiTestStatus = "正在测试连接…"; RenderSettings();
            try
            {
                var schema = JObject.Parse("{\"type\":\"object\",\"properties\":{\"connected\":{\"type\":\"boolean\"}},\"required\":[\"connected\"],\"additionalProperties\":false}");
                using (var client = new ResponsesAiClient(config))
                {
                    var output = await client.RequestAsync("This is an API connectivity test. Return JSON with connected=true.", "Confirm the connection works.", schema, cancel.Token);
                    if (this == null || cancel.IsCancellationRequested || version != settingsTestVersion) return;
                    if (JObject.Parse(output)["connected"]?.Value<bool>() != true) throw new AiRequestException("服务未返回有效的结构化测试结果。");
                    aiTestStatus = "测试通过 · " + config.Model;
                    testedAiRequest = AiRequestSignature(config); testedAiServiceStatus = "上次测试可用";
                }
            }
            catch (OperationCanceledException) { if (this != null && version == settingsTestVersion) aiTestStatus = "连接测试已取消"; }
            catch (Exception error)
            {
                if (this != null && version == settingsTestVersion)
                {
                    aiTestStatus = error is AiRequestException || error is AiConfigurationException ? error.Message : "服务未返回有效的测试结果。";
                    testedAiRequest = AiRequestSignature(config); testedAiServiceStatus = "连接异常";
                }
            }
            finally
            {
                if (settingsTest == cancel) settingsTest = null; cancel.Dispose();
                if (this != null && version == settingsTestVersion) { ShowStatus(aiTestStatus); RenderSettings(); }
            }
        }
        internal void CancelSettingsTest() { ++settingsTestVersion; settingsTest?.Cancel(); settingsTest = null; }

        internal bool SaveSettings()
        {
            if (settingsTest != null) { ShowStatus("请先结束连接测试。"); return false; }
            var modified = settingsValues.Where(pair => settingsOriginal[pair.Key] != pair.Value).Select(pair => pair.Key).ToArray();
            foreach (var field in clientSettings.Where(field => modified.Contains(field.Key) && field.Floating))
            {
                var value = ParseStoredFloat(settingsValues[field.Key]);
                if (float.IsNaN(value) || value < field.Min || value > field.Max || field.Whole && value != Mathf.Round(value) || field.Key == "FPS" && value != 0 && value < 30)
                {
                    settingsCategory = field.Category;
                    if (field.Category == "duel")
                        foreach (var condition in new[] { "Duel", "Watch", "Replay" }) if (field.Key.StartsWith(condition, StringComparison.Ordinal)) settingsCondition = condition;
                    settingsErrors[field.Key] = SettingsNumberDraftError(field, settingsValues[field.Key]) ?? "请输入有效数值";
                    settingsScrollPosition = 1; RenderSettings(); FocusRenderedSettingsField(field.Key); ShowStatus(field.Label + "：" + settingsErrors[field.Key]); return false;
                }
            }
            if (!int.TryParse(settingsValues["LocalDuelPort"], out var port) || port < 1 || port > 65535)
            { settingsCategory = "general"; settingsErrors["LocalDuelPort"] = "端口必须为 1–65535。"; settingsScrollPosition = 1; RenderSettings(); FocusRenderedSettingsField("LocalDuelPort"); ShowStatus(settingsErrors["LocalDuelPort"]); return false; }
            ResponsesAiConfig aiNext = null;
            if (aiSettingsDraft != null && AiDraftSignature() != aiSettingsOriginal) { aiNext = ReadAiSettings(); if (aiNext == null) return false; }
            try
            {
                if (aiNext != null) { ResponsesAiConfigStore.SaveService(aiNext); aiSettingsDraft = aiNext; aiSettingsOriginal = AiDraftSignature(); aiLoadError = null; }
                foreach (var key in modified) Config.Set(key, settingsValues[key]);
                Config.Save();
                var configPath = typeof(Config).GetField("path", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as string;
                if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath)) throw new IOException("无法访问配置文件。");
                var saved = File.ReadAllText(configPath);
                if (modified.Any(key => !saved.Split('\n').Any(line => line.TrimEnd('\r') == key + "->" + settingsValues[key]))) throw new IOException("无法写入配置文件，请检查权限。");
                foreach (var key in modified) settingsOriginal[key] = settingsValues[key];
                string applyError = null;
                try { ApplyClientSettings(modified, port); }
                catch (Exception error) { applyError = error.Message; Debug.LogWarning("Fight settings apply: " + error); }
                settingsErrors.Clear(); UpdateSettingsDirtyLabel();
                ShowStatus(applyError == null ? "设置已保存。" : "设置已保存，部分项目未立即生效：" + applyError); return true;
            }
            catch (Exception error)
            {
                foreach (var key in modified) Config.Set(key, settingsOriginal[key]);
                ShowStatus("保存失败：" + error.Message); return false;
            }
        }
        private void ApplyClientSettings(string[] modified, int port)
        {
            SoloSelector.port = port.ToString();
            SynchronizeLegacySettings(modified);
            if (modified.Contains("BgmVol")) AudioManager.SetBGMVol(Config.GetFloat("BgmVol", .7f));
            if (modified.Contains("SEVol")) AudioManager.SetSeVol(Config.GetFloat("SEVol", .7f));
            if (modified.Contains("VoiceVol")) AudioManager.SetVoiceVol(Config.GetFloat("VoiceVol", .7f));
            if (modified.Contains("Scale")) Program.instance.camera_.urpAsset.renderScale = Config.GetFloat("Scale", 1);
            // The original camera applies the selected 3D quality on entering the duel;
            // the 2D menu uses its dedicated quality level.
            if (modified.Contains("AAA")) SettingServantUI.ChangeAAA(Config.GetFloat("AAA", 0));
            if (modified.Contains("Shadow")) SettingServantUI.ChangeShadow(Config.GetFloat("Shadow", 0));
            if (modified.Contains("FPS")) SettingServantUI.ChangeFPS(Config.GetFloat("FPS", 60));
            if (modified.Contains("ShowFPS")) SettingServantUI.ChangeShowFPS();
            if (modified.Contains("Background")) Program.instance.background_.Change(int.Parse(Config.Get("Background", "0")));
            if (modified.Contains("Expansions")) { FileManager.Dispose(); Program.instance.InitializeForDataChange(); }
            if (modified.Contains("ScreenMode") || modified.Contains("Resolution"))
            {
                var size = Config.Get("Resolution", Screen.width + " x " + Screen.height).Split('x');
                var mode = Config.Get("ScreenMode", "1");
                if (size.Length == 2 && int.TryParse(size[0].Trim(), out var w) && int.TryParse(size[1].Trim(), out var h))
                    Screen.SetResolution(w, h, mode == "0" ? FullScreenMode.ExclusiveFullScreen : mode == "2" ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow);
            }
            if (modified.Contains("Layout")) UIManager.ChangeLayout();
            if (modified.Contains("CardStyle")) MaterialLoader.ClearCachedOverFrameMasks();
            if (modified.Contains("VideoCard")) SystemEvent.CallVideoCardConfigChangeEvent();
            if (modified.Contains("Language") || modified.Contains("CardLanguage") || modified.Contains("CardStyle")) UIManager.ChangeLanguage();
        }
        private void SynchronizeLegacySettings(string[] modified)
        {
            var ui = Program.instance.setting.GetUI<SettingServantUI>();
            if (ui == null) return;
            foreach (var field in clientSettings.Where(field => modified.Contains(field.Key)))
            {
                var property = field.SliderName ?? "Button" + field.Key;
                if (field.Key == "Expansions") property = "ButtonExpansionSupport";
                if (field.Key == "BGMbyMySide") property = "ButtonBgmBy";
                var control = typeof(SettingServantUI).GetProperty(property, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(ui) as SelectionButton_Setting;
                if (control == null) continue;
                var value = settingsValues[field.Key];
                if (field.Floating)
                {
                    var number = ParseStoredFloat(value);
                    var slider = typeof(SelectionButton_Setting).GetField("slider", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(control) as Slider;
                    if (slider != null) slider.SetValueWithoutNotify(field.Key == "FPS" && number == 0 ? 29 : number);
                    var mode = number.ToString("0.###", CultureInfo.InvariantCulture);
                    if (field.Key == "FPS" && number == 0) mode = "无限制";
                    if (field.Key == "FAA") mode = "Off";
                    if (field.Key == "AAA" && number >= 0 && number <= 5) mode = new[] { "Off", "FAA", "SMAA Low", "SMAA Medium", "SMAA High", "TAA" }[(int)number];
                    if (field.Key == "Shadow" && number >= 0 && number <= 4) mode = new[] { "非常低", "低", "中等", "高", "非常高" }[(int)number];
                    if (field.Key == "Quality" && number >= 0 && number <= 5) mode = new[] { "非常低", "低", "中等", "高", "非常高", "极致" }[(int)number];
                    control.SetModeText(mode);
                }
                else if (field.Boolean) control.SetModeText(field.Key == "Confirm" ? value == "1" ? "左" : "右"
                    : field.Key == "BGMbyMySide" ? value == "1" ? "我方" : "对方"
                    : field.Key == "Expansions" ? value == "1" ? "是" : "否" : value == "1" ? "开" : "关");
                else { var index = Array.IndexOf(field.Values, value); control.SetModeText(index < 0 ? value : field.Labels[index]); }
            }
        }
        internal void DiscardSettings() { CancelSettingsTest(); LoadSettingsDraft(); settingsScrollPosition = 1; }
        private void RestoreSettingsDefaults()
        {
            if (settingsCategory == "ai")
            {
                var defaults = new ResponsesAiConfig();
                string[] keys;
                if (aiAdvanced)
                {
                    aiTimeout = defaults.TimeoutSeconds.ToString(); aiOutput = defaults.MaxOutputTokens.ToString(); aiBudget = defaults.PromptBudgetTokens.ToString();
                    aiSettingsDraft.ReasoningEffort = defaults.ReasoningEffort; aiHeaders.Clear(); visibleHeaders.Clear(); aiHeaderIds.Clear(); aiSettingsDraft.ExtraBodyJson = "{}";
                    keys = new[] { "timeout", "output", "budget", "reasoning", "headers", "body" }.Concat(settingsErrors.Keys.Where(key => key.StartsWith("header:", StringComparison.Ordinal))).ToArray();
                }
                else
                {
                    aiSettingsDraft.BaseUrl = defaults.BaseUrl; aiSettingsDraft.Model = defaults.Model; aiSettingsDraft.ApiKey = ""; aiSettingsDraft.RememberApiKey = false;
                    keys = new[] { "baseUrl", "model", "apiKey" };
                }
                foreach (var key in keys) AiSettingsChanged(key);
            }
            else foreach (var field in clientSettings.Where(field => field.Category == settingsCategory && (settingsCategory != "duel" || field.Key.StartsWith(settingsCondition, StringComparison.Ordinal)))) SettingsChanged(field.Key, field.Default);
            if (settingsCategory == "general") SettingsChanged("LocalDuelPort", "7911");
            RenderSettings(); ShowStatus(settingsCategory == "duel" ? "已恢复当前情境默认，保存后生效。" : "已恢复本页默认，保存后生效。");
        }

        private void RenderDataSettings(Transform parent, float width, ref float y)
        {
            var inDuel = Program.instance.ocgcore.showing;
            if (inDuel) { FightUI.Text(parent, "当前在对局中：导入和清除不可用", 0, y, width, 40, 18, FightUI.Muted); y += 56; }
            SettingsSection(parent, width, "导入", ref y);
            RenderDataAction(parent, width, "导入卡组、回放与扩展资源", "同名文件生成独立副本，保留原文件。", "选择文件", () => ImportClientFiles(false), !inDuel, ref y);
            RenderDataAction(parent, width, "导入背景", "更新自定义背景图片，保留原图片备份。", "选择图片", () => ImportClientFiles(true), !inDuel, ref y);
            SettingsSection(parent, width, "导出", ref y);
            RenderDataAction(parent, width, "导出全部卡组", "包含分组；复制到新的独立导出文件夹。", "导出", () => ExportClientFiles(Program.PATH_DECK, "卡组"), true, ref y);
            RenderDataAction(parent, width, "导出全部回放", "保留客户端内的全部原回放文件。", "导出", () => ExportClientFiles(Program.PATH_REPLAY, "回放"), true, ref y);
            RenderDataAction(parent, width, "导出卡图", "复制卡图；保留本地卡图缓存。", "导出", () => ExportClientFiles(Program.PATH_CARD_PIC, "卡图"), true, ref y);
            SettingsSection(parent, width, "资源管理", ref y);
            RenderSettingField(parent, width, clientSettings.First(field => field.Key == "Expansions"), ref y);
            RenderDataAction(parent, width, "管理扩展", "打开本地扩展目录。", "打开目录", () => { Directory.CreateDirectory(Program.PATH_EXPANSIONS); Application.OpenURL(new Uri(Path.GetFullPath(Program.PATH_EXPANSIONS)).AbsoluteUri); }, !inDuel, ref y);
            RenderDataAction(parent, width, "清除导入卡图", "仅删除导入卡图，不影响卡组和回放。", "清除", () => ClearClientFiles(Program.PATH_ALT_ART, "导入卡图", false), !inDuel, ref y);
            RenderDataAction(parent, width, "清除动态卡图", "仅删除动态卡图文件。", "清除", () => ClearClientFiles(Program.PATH_VIDEO_ART, "动态卡图", false), !inDuel, ref y);
            RenderDataAction(parent, width, "清除扩展资源", "仅删除扩展目录中的导入文件。", "清除", () => ClearClientFiles(Program.PATH_EXPANSIONS, "扩展资源", true), !inDuel, ref y);
        }
        private void RenderDataAction(Transform parent, float width, string title, string description, string label, Action action, bool enabled, ref float y)
        {
            var panel = FightUI.Node(parent, title + " setting", 0, y, width, 96);
            var heading = SettingsFieldLabel(panel, title, 0, 4, width - 180, 34);
            float descriptionY = Mathf.Max(42, heading.rectTransform.sizeDelta.y + 10);
            var detail = SettingsWrappedText(panel, description, 0, descriptionY, width - 180, 30, 15, FightUI.Muted);
            float rowHeight = Mathf.Max(96, descriptionY + detail.rectTransform.sizeDelta.y + 16);
            panel.sizeDelta = new Vector2(width, rowHeight);
            var button = FightUI.Button(panel, label, width - 140, (rowHeight - 48) / 2, 140, 48, action, false, enabled);
            if (label == "清除")
            {
                button.GetComponent<Outline>().effectColor = FightUI.Danger;
                button.GetComponentInChildren<TextMeshProUGUI>().color = FightUI.Danger;
            }
            FightUI.Line(panel, 0, rowHeight - 1, width, new Color(.2f, .28f, .33f, .20f));
            if (!enabled) FightUI.Hint(button.gameObject, "当前在对局中，请退出对局后操作。"); y += rowHeight + 6;
        }
        private void ClearClientFiles(string path, string label, bool expansions)
        {
            if (Program.instance.ocgcore.showing) { ShowStatus("决斗中不能进行此操作。"); return; }
            var files = Directory.Exists(path) ? Directory.GetFiles(path) : Array.Empty<string>();
            Confirm("清除" + label, "将删除 " + files.Length + " 个" + label + "文件。卡组、回放和其他存档不会删除。", () =>
            {
                try
                {
                    if (expansions) FileManager.Dispose();
                    foreach (var file in files) if (File.Exists(file)) File.Delete(file);
                    if (path == Program.PATH_VIDEO_ART) ResourceManager.ClearCardVideos();
                    if (expansions) Program.instance.InitializeForDataChange();
                    ShowStatus(label + "已清除。");
                }
                catch (Exception error) { ShowStatus("清除失败：" + error.Message); }
            });
        }
        private void ImportClientFiles(bool background)
        {
            if (Program.instance.ocgcore.showing) return;
#if UNITY_EDITOR || (!UNITY_ANDROID && !UNITY_IOS)
            var extensions = background ? new[] { new ExtensionFilter("背景图片", "png", "jpg", "jpeg") } : new[] { new ExtensionFilter("卡组 / 回放 / 资源", "ydk", "yrp", "yrp3d", "ypk", "zip", "cdb", "conf", "png", "jpg", "jpeg", "mp4") };
            StandaloneFileBrowser.OpenFilePanelAsync("选择导入文件", "", extensions, !background, paths => CopyClientImports(paths, background));
#else
            if (background) NativeFilePicker.PickFile(path => CopyClientImports(new[] { path }, true), new[] { "image/png", "image/jpeg" });
            else NativeFilePicker.PickMultipleFiles(paths => CopyClientImports(paths, false), null);
#endif
        }
        private void CopyClientImports(string[] paths, bool background)
        {
            if (paths == null || paths.Length == 0 || Program.instance.ocgcore.showing) return;
            int copied = 0; bool data = false; var errors = new List<string>();
            foreach (var source in paths.Where(path => !string.IsNullOrEmpty(path)))
            {
                try
                {
                    if (!File.Exists(source)) continue;
                    var extension = Path.GetExtension(source).ToLowerInvariant();
                    var directory = background ? Program.PATH_DIY : extension == ".ydk" ? Program.PATH_DECK : extension == ".yrp" || extension == ".yrp3d" ? Program.PATH_REPLAY
                        : extension == ".png" || extension == ".jpg" || extension == ".jpeg" ? Program.PATH_ALT_ART : extension == ".mp4" ? Program.PATH_VIDEO_ART
                        : extension == ".ypk" || extension == ".zip" || extension == ".cdb" || extension == ".conf" ? Program.PATH_EXPANSIONS : null;
                    if (directory == null) { errors.Add(Path.GetFileName(source) + "：不支持的格式"); continue; }
                    Directory.CreateDirectory(directory); var destination = UniqueClientPath(directory, (background ? "Imported " : "") + Path.GetFileName(source)); File.Copy(source, destination, false); copied++;
                    data |= directory == Program.PATH_EXPANSIONS;
                    if (background)
                    {
                        var chosen = destination;
                        var texture = new Texture2D(2, 2);
                        try
                        {
                            if (!texture.LoadImage(File.ReadAllBytes(chosen))) throw new IOException("无法读取图片。");
                            var current = Path.Combine(Program.PATH_DIY, "Background.png"); var temporary = current + "." + Guid.NewGuid().ToString("N") + ".tmp";
                            File.WriteAllBytes(temporary, texture.EncodeToPNG());
                            if (File.Exists(current)) { File.Copy(current, UniqueClientPath(Program.PATH_DIY, "Background.backup.png"), false); File.Replace(temporary, current, null); } else File.Move(temporary, current);
                            Program.instance.background_.Refresh();
                        }
                        finally { Destroy(texture); }
                    }
                }
                catch (Exception error) { errors.Add(Path.GetFileName(source) + "：" + error.Message); }
            }
            if (data) { FileManager.Dispose(); Program.instance.InitializeForDataChange(); }
            ShowStatus(background && errors.Count == 0 ? "自定义背景图片已导入。" : "已复制导入 " + copied + " 个文件。" + (errors.Count == 0 ? "" : " " + string.Join("；", errors)));
        }
        private static string UniqueClientPath(string directory, string name)
        {
            var path = Path.Combine(directory, name); var stem = Path.GetFileNameWithoutExtension(name); var extension = Path.GetExtension(name); int suffix = 2;
            while (File.Exists(path) || Directory.Exists(path)) path = Path.Combine(directory, stem + " (" + suffix++ + ")" + extension);
            return path;
        }
        private void ExportClientFiles(string directory, string label)
        {
            var files = Directory.Exists(directory) ? Directory.GetFiles(directory, "*", SearchOption.AllDirectories) : Array.Empty<string>();
            if (files.Length == 0) { ShowStatus("没有可导出的" + label + "。"); return; }
#if UNITY_EDITOR || (!UNITY_ANDROID && !UNITY_IOS)
            StandaloneFileBrowser.OpenFolderPanelAsync("选择导出目录", "", false, paths =>
            {
                if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0])) return;
                try
                {
                    var target = UniqueClientPath(paths[0], "Yu-Gi-Oh Fight " + label + " " + DateTime.Now.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(target);
                    var root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    foreach (var source in files) { var destination = Path.Combine(target, Path.GetFullPath(source).Substring(root.Length)); Directory.CreateDirectory(Path.GetDirectoryName(destination)); File.Copy(source, destination, false); }
                    ShowStatus("已导出 " + files.Length + " 个文件到 " + target);
                }
                catch (Exception error) { ShowStatus("导出失败：" + error.Message); }
            });
#else
            NativeFilePicker.ExportMultipleFiles(files, success => ShowStatus(success ? label + "已复制导出。" : "导出已取消或失败。"));
#endif
        }
        private void RenderAboutSettings(Transform parent, float width, ref float y)
        {
            var banner = FightUI.Node(parent, "Client identity", 0, y, width, 170);
            FightUI.Asset(banner, "brand.png", 0, 16, 224, 132, false);
            FightUI.Text(banner, "Yu-Gi-Oh! Fight", 252, 32, width - 280, 44, 28, FightUI.Gold);
            FightUI.Text(banner, "客户端 0.1.0", 254, 82, width - 280, 28, 18, FightUI.Foreground);
            FightUI.Text(banner, "MDPro3 1.4.7-2  ·  协议 " + Config.ClientVersion.ToString("X"), 254, 118, width - 280, 28, 16, FightUI.Muted);
            y += 182;
            AboutParagraph(parent, width, "游戏与客户端", "Yu-Gi-Oh! Fight 基于 MDPro3 客户端与游戏王规则引擎开发。游戏规则、卡片数据与资源沿用已安装的本地内容。", ref y);
            AboutParagraph(parent, width, "第三方组件与资源来源", "MDPro3、ocgcore、WindBot、Unity、TextMesh Pro、Newtonsoft.Json 及客户端所附第三方组件。各组件与内容的许可和版权由原作者保留。游戏王及相关卡牌、角色和素材属于其各自权利人。", ref y);
            AboutParagraph(parent, width, "支持信息", "运行、构建、原生组件与来源说明见本项目 README.md、native/README.md 和随项目提供的许可声明。", ref y);
            FightUI.Button(parent, "查看来源声明", 0, y, 240, 48, () =>
            {
                var path = FindClientSourceNotice();
                if (path == null) { ShowStatus("来源声明见客户端安装目录中的文档。"); return; }
                var modal = ShowDialog("来源声明", 900, 650); var content = FightUI.Scroll(modal, 24, 72, 852, 502, 2600);
                var notice = FightUI.Text(content, File.ReadAllText(path), 12, 12, 820, 2550, 18);
                notice.enableWordWrapping = true;
                float noticeHeight = notice.GetPreferredValues(notice.text, 820, float.PositiveInfinity).y + 8;
                notice.rectTransform.sizeDelta = new Vector2(820, noticeHeight); content.sizeDelta = new Vector2(852, Mathf.Max(502, noticeHeight + 24));
                FightUI.Button(modal, "关闭", 700, 586, 160, 44, CloseDialog);
            }); y += 72;
        }
        private void AboutParagraph(Transform parent, float width, string title, string text, ref float y)
        {
            SettingsSection(parent, width, title, ref y);
            var paragraph = FightUI.Text(parent, text, 0, y, width, 160, 19, FightUI.Foreground);
            float height = paragraph.GetPreferredValues(text, width, 1000).y + 10;
            paragraph.rectTransform.sizeDelta = new Vector2(width, height);
            y += height + 16;
        }
        private static string FindClientSourceNotice()
        {
            foreach (var start in new[] { Application.dataPath, Environment.CurrentDirectory })
            {
                var directory = new DirectoryInfo(start);
                for (var depth = 0; directory != null && depth < 7; depth++, directory = directory.Parent)
                    foreach (var name in new[] { "THIRD_PARTY_NOTICES.md", "ThirdPartyNotices.txt", "THIRD_PARTY_NOTICES.txt", "LICENSE", "LICENSE.md", "COPYING" })
                    {
                        var path = Path.Combine(directory.FullName, name);
                        if (File.Exists(path)) return path;
                    }
            }
            return null;
        }
    }
}
