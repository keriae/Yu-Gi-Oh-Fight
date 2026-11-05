using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;
using MDPro3.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MDPro3.UI.Client
{
    // The duel owns rules and responses. This canvas owns only its auxiliary
    // menus and keeps their controls independent from the rule-response root.
    internal sealed partial class FightDuelMenu : MonoBehaviour
    {
        internal static FightDuelMenu Instance { get; private set; }
        internal static bool BlocksInput => Instance != null && (Instance.Showing || Instance.consumedInputFrame == Time.frameCount);
        internal bool Showing => canvas != null && canvas.enabled;
        internal RectTransform Root => root;
        internal RectTransform Dialog => dialog;
        internal string Page => page;
        private Canvas canvas;
        private RectTransform root, window, body, dialogShade, dialog;
        private MonoBehaviour previousBlocker;
        private GameObject previousSelection, dialogSelection;
        private readonly Stack<DialogFrame> dialogHistory = new Stack<DialogFrame>();
        private GameObject focusedControl;
        private sealed class DialogFrame
        {
            internal RectTransform Panel, Shade;
            internal GameObject OutsideSelection, Selection;
        }
        private string page = "settings", category = "audio", effectsSignature = "";
        private string backPage;
        private int consumedInputFrame = -1;
        private float scrollPosition = 1;
        private ScrollRect scroll;
        private int width, height;
        private const float Width = 1120, Height = 620;

        internal static FightDuelMenu Ensure()
        {
            if (Instance == null) Instance = Program.instance.gameObject.AddComponent<FightDuelMenu>();
            return Instance;
        }
        internal static void OpenSettings() => Ensure().Open("settings");
        internal static void OpenLogCard(int code)
        {
            if (code <= 0) return;
            var menu = Ensure(); menu.selectedCard = code; menu.Open("card");
        }
        internal static void ToggleEffects()
        {
            var menu = Ensure();
            if (menu.Showing && menu.page == "effects") menu.Close();
            else menu.Open("effects");
        }
        private void Awake()
        {
            Instance = this;
            var host = new GameObject("Fight duel menus", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            host.transform.SetParent(transform, false);
            canvas = host.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 160;
            host.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            root = FightUI.Node(host.transform, "Duel menu viewport", 0, 0, 1280, 720);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f);
            Fit(); canvas.enabled = false;
        }
        private void Fit()
        {
            width = Screen.width; height = Screen.height;
            float scale = Mathf.Min(Mathf.Max(1, width) / 1280f, Mathf.Max(1, height) / 720f);
            root.localScale = new Vector3(scale, scale, 1); root.anchoredPosition = Vector2.zero;
        }
        private void Update()
        {
            if (!Showing) return;
            if (Program.instance.currentServant != Program.instance.ocgcore) { Close(false); return; }
            if (width != Screen.width || height != Screen.height) Fit();
            if (UIManager.InputBlocker == this)
            {
                if ((Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) || UserInput.WasCancelPressed || UserInput.MouseRightDown)
                {
                    // The duel must not process the same cancel again after
                    // the final auxiliary layer has closed this frame.
                    consumedInputFrame = Time.frameCount;
                    Back();
                    if (!Showing) return;
                }
                if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
                    MoveFocus(Keyboard.current.shiftKey.isPressed ? -1 : 1);
            }
            KeepFocusInScope();
            if (page == "effects" && dialog == null)
            {
                string next = EffectsSignature();
                if (effectsSignature != next) { effectsSignature = next; Render(); }
            }
        }
        private void MoveFocus(int direction)
        {
            var choices = FocusControls();
            if (choices.Length == 0 || EventSystem.current == null) return;
            int current = Array.FindIndex(choices, item => item.gameObject == EventSystem.current.currentSelectedGameObject);
            int next = current < 0 ? (direction < 0 ? choices.Length - 1 : 0) : (current + direction + choices.Length) % choices.Length;
            Focus(choices[next]);
        }
        private void Back()
        {
            if (dialog != null) DismissDialog();
            else if (backPage != null) Open(backPage);
            else Close();
        }
        internal void Open(string next, string returnPage = null)
        {
            if (!Showing)
            {
                previousBlocker = UIManager.InputBlocker;
                previousSelection = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
                UIManager.InputBlocker = this; canvas.enabled = true;
                ResetCardReading();
                AudioManager.PlaySE("SE_MENU_SELECT_01");
            }
            CloseDialog(); page = next; backPage = returnPage; scrollPosition = 1; scroll = null;
            var ui = Program.instance.ocgcore.GetUI<OcgCoreUI>();
            ui?.CardDescription.Hide(); ui?.CardList?.Hide();
            if (next == "effects") { effectsSignature = EffectsSignature(); ui?.SwitchAttachedEffectState(); }
            else ui?.ResetAttachedEffectState();
            Render();
        }
        internal void Close(bool sound = true)
        {
            if (!Showing) return;
            bool ownsInput = UIManager.InputBlocker == this;
            CloseDialog();
            Config.Save(); canvas.enabled = false;
            if (ownsInput) UIManager.InputBlocker = previousBlocker;
            if (ownsInput && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
            Program.instance.ocgcore.GetUI<OcgCoreUI>()?.ResetAttachedEffectState();
            if (sound) AudioManager.PlaySE("SE_MENU_CANCEL");
            FightUI.Clear(root);
        }
        private void Render()
        {
            if (scroll != null) scrollPosition = scroll.verticalNormalizedPosition;
            CaptureCardReading();
            var selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            string focusedKey = selected == null ? null : selected.GetComponent<FightFocusKey>()?.Key;
            CloseDialog(); FightUI.Clear(root);
            if (page == "card") { window = body = null; ShowCardDialog(); return; }
            var shade = FightUI.Panel(root, -4000, -4000, 9280, 8720, new Color(.006f, .015f, .025f, .78f), false, "Duel menu backdrop");
            shade.gameObject.AddComponent<Button>().onClick.AddListener(() => Close());
            window = FightUI.Panel(root, 80, 50, Width, Height, FightUI.PanelColor, true, "Duel auxiliary menu");
            FightUI.Text(window, "YU-GI-OH! FIGHT", 28, 15, 300, 20, 12, FightUI.Muted).characterSpacing = 2;
            string settingsLabel = ContextText("对局设置", "回放设置", "观战设置");
            string title = page == "settings" ? settingsLabel : page == "effects" ? "已附加效果" : "当前卡组";
            FightUI.HeroTitle(window, title, 28, 36, 660, 44, 30).name = "Duel menu title";
            string mode = ContextText("对战模式", "回放模式", "观战模式");
            FightUI.Text(window, mode, Width - 360, 44, 140, 32, 16, FightUI.Muted, TextAlignmentOptions.MidlineRight).name = "Duel menu mode";
            var close = FightUI.FocusKey(FightUI.Button(window, ContextText("返回对局", "返回回放", "返回观战"), Width - 196, 32, 168, 44, () => Close()), "duel:close");
            FightUI.Line(window, 28, 94, Width - 56, FightUI.Border);
            body = FightUI.Node(window, "Duel menu body", 28, 112, Width - 56, Height - 186);
            if (page == "settings") RenderSettings();
            else if (page == "effects") RenderEffects();
            else RenderDeck();
            FightUI.Line(window, 28, Height - 60, Width - 56, FightUI.Border);
            FightUI.Text(window, page == "settings" ? "更改即时生效，关闭时保存。" : "点击卡片查看完整信息。", 28, Height - 44, Width - 300, 28, 14, FightUI.Muted);
            if (page != "settings") FightUI.FocusKey(FightUI.Button(window, settingsLabel, Width - 188, Height - 48, 160, 36, () => Open("settings")), "duel:settings");
            if (EventSystem.current != null)
            {
                var retained = string.IsNullOrEmpty(focusedKey) ? null : window.GetComponentsInChildren<Selectable>()
                    .FirstOrDefault(item => item.GetComponent<FightFocusKey>()?.Key == focusedKey && item.IsActive() && item.IsInteractable());
                Focus(retained != null ? retained : close);
            }
            RefreshNavigation();
        }
        private static string ContextText(string duel, string replay, string spectating) => OcgCore.condition == OcgCore.Condition.Replay
            ? replay : OcgCore.condition == OcgCore.Condition.Watch ? spectating : duel;
        private void RenderSettings()
        {
            const float railWidth = 204, readerX = 228, readerWidth = 836;
            var rail = FightUI.Panel(body, 0, 0, railWidth, 434, new Color(.015f, .034f, .047f, .6f), false, "Duel settings navigation");
            var categories = new[] { ("audio", "声音"), ("graphics", "显示与画质"), ("general", "操作与语言"), ("duel", "对局演出"), ("about", "关于与版权") };
            for (int i = 0; i < categories.Length; i++)
            {
                var entry = categories[i];
                var tab = FightUI.FocusKey(FightUI.Tab(rail, entry.Item2, category == entry.Item1, 12, 8 + i * 54, railWidth - 24, 46,
                    () => { category = entry.Item1; scrollPosition = 1; scroll = null; Render(); }), "duel:category:" + entry.Item1);
                tab.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
            }
            FightUI.Line(rail, 18, 298, railWidth - 36, FightUI.Border);
            if (OcgCore.condition == OcgCore.Condition.Duel && !OcgCore.inPuzzle)
                FightUI.FocusKey(FightUI.Button(rail, "查看当前卡组", 12, 314, railWidth - 24, 42, OpenDeck), "duel:deck");
            if (OcgCore.condition == OcgCore.Condition.Duel && RoomServant.FromSolo && Program.instance.solo.CanRetryLastSoloDuel())
                FightUI.FocusKey(FightUI.Button(rail, "重新开始", 12, 366, railWidth - 24, 42, () => ConfirmExit(true)), "duel:retry");
            var exit = FightUI.FocusKey(FightUI.Button(window, OcgCore.condition == OcgCore.Condition.Duel ? "结束对局" : OcgCore.condition == OcgCore.Condition.Replay ? "退出回放" : "退出观战",
                Width - 190, Height - 49, 162, 38, () => ConfirmExit(false)), "duel:exit");
            FightUI.StyleDestructiveButton(exit);
            string heading = categories.First(entry => entry.Item1 == category).Item2;
            FightUI.Text(body, heading, readerX, 0, readerWidth, 32, 22, FightUI.Active);
            var content = FightUI.Scroll(body, readerX, 44, readerWidth, 390, 1200, out scroll);
            float y = 0;
            if (category == "audio")
            {
                Section(content, "音量", ref y);
                Number(content, "BgmVol", "背景音乐", .7f, 0, 1, ref y, percentage: true);
                Number(content, "SEVol", "音效", .7f, 0, 1, ref y, percentage: true);
                Number(content, "VoiceVol", "角色语音", .7f, 0, 1, ref y, percentage: true);
                Switch(content, "BGMbyMySide", "背景音乐依据我方场地", true, ref y);
                FightUI.Button(content, "试听角色语音", 16, y + 8, 200, 42, () => AudioManager.PlayVoiceByResourcePath("VOICE/VoiceSample")); y += 66;
            }
            else if (category == "graphics")
            {
                Section(content, "屏幕", ref y);
                Choice(content, "ScreenMode", "显示模式", "1", new[] { "1", "2", "0" }, new[] { "窗口全屏", "窗口化（桌面）", "独占全屏（Windows）" }, ref y);
                var resolutions = new[] { Screen.width + " x " + Screen.height }.Concat(Screen.resolutions.Select(item => item.width + " x " + item.height)).Distinct().ToArray();
                Choice(content, "Resolution", "分辨率", Screen.width + " x " + Screen.height, resolutions, resolutions, ref y);
                Number(content, "Scale", "渲染比例", 1, .1f, 2, ref y);
                Choice(content, "FPS", "帧率上限", "60000", new[] { "30000", "60000", "90000", "120000", "144000", "240000", "0" }, new[] { "30 FPS", "60 FPS", "90 FPS", "120 FPS", "144 FPS", "240 FPS", "无限制" }, ref y);
                Switch(content, "ShowFPS", "显示 FPS", true, ref y);
                Section(content, "画质", ref y);
                Choice(content, "Quality", "画质", "2000", new[] { "0", "1000", "2000", "3000", "4000", "5000" }, new[] { "非常低", "低", "中等", "高", "非常高", "极致" }, ref y);
                Choice(content, "AAA", "抗锯齿模式", "0", new[] { "0", "1000", "2000", "3000", "4000", "5000" }, new[] { "关", "FXAA", "SMAA 低", "SMAA 中", "SMAA 高", "TAA" }, ref y);
                Choice(content, "Shadow", "阴影质量", "0", new[] { "0", "1000", "2000", "3000", "4000" }, new[] { "非常低", "低", "中等", "高", "非常高" }, ref y);
                Switch(content, "VideoCard", "动态卡图", true, ref y);
            }
            else if (category == "general")
            {
                Section(content, "操作", ref y);
                Switch(content, "Confirm", "确认按钮位于左侧", false, ref y);
                Switch(content, "Rumble", "手柄震动", true, ref y);
                Choice(content, "Layout", "布局方式", "0", new[] { "0", "1000", "2000" }, new[] { "自动判断", "桌面布局", "移动布局" }, ref y);
                Switch(content, "SyncDeck", "卡组同步", true, ref y);
                Section(content, "语言与卡面", ref y);
                string[] locales = Directory.Exists(Program.PATH_LOCALES) ? Directory.GetDirectories(Program.PATH_LOCALES).Select(Path.GetFileName).Where(item => new[] { Language.English, Language.SimplifiedChinese, Language.TraditionalChinese, Language.Japanese, Language.Korean, Language.German, Language.Spanish, Language.French, Language.Italian, Language.Portuguese }.Contains(item)).OrderBy(item => item).ToArray() : new[] { "zh-CN", "en-US" };
                Choice(content, "Language", "界面语言", Language.GetConfig(), locales, locales.Select(LocaleName).ToArray(), ref y);
                Choice(content, "CardLanguage", "卡片语言", Language.GetCardConfig(), locales, locales.Select(LocaleName).ToArray(), ref y);
                var styles = Enum.GetNames(typeof(CardRenderer.CardStyle));
                Choice(content, "CardStyle", "卡面风格", CardRenderer.CardStyle.OCG_TCG.ToString(), styles,
                    styles.Select(style => style == "OCG_TCG" ? "标准卡面（OCG / TCG）" : style == "RUSH_DUEL" ? "超速决斗（RUSH DUEL）" : style).ToArray(), ref y);
                var backgrounds = BackgroundManager.backgrounds;
                Choice(content, "Background", "背景", "0", new[] { "0" }.Concat(backgrounds.Keys.Select(item => item.ToString())).ToArray(), new[] { "随机" }.Concat(backgrounds.Values).ToArray(), ref y);
            }
            else if (category == "duel")
            {
                string prefix = OcgCore.condition.ToString();
                Section(content, "决斗者外观", ref y);
                FightUI.FocusKey(FightUI.Button(content, "编辑外观与角色", 16, y, 788, 46, ShowAppearance), "duel:appearance"); y += 62;
                Section(content, "演出与语音", ref y);
                foreach (var item in new[] { ("Voice", "角色语音"), ("Closeup", "近景演出"), ("Summon", "召唤动画"), ("Pendulum", "灵摆演出"), ("Cutin", "卡片特写"), ("Effect", "卡片效果演出"), ("Chain", "连锁演出"), ("Dice", "骰子演出"), ("Coin", "硬币演出") })
                    Switch(content, prefix + item.Item1, item.Item2, item.Item1 != "Voice" && item.Item1 != "Closeup", ref y);
                Section(content, "信息与播放", ref y);
                foreach (var item in new[] { ("AutoInfo", "自动卡片详情"), ("FaceDown", "盖卡信息"), ("PlayerMessage", "玩家消息"), ("SystemMessage", "系统消息"), ("AutoAcc", "自动加速") }) Switch(content, prefix + item.Item1, item.Item2, true, ref y);
                Number(content, prefix + "Acc", "加速倍率", 2, 1, 5, ref y);
                Switch(content, Config.LABEL_TIMING, "响应时机始终开启", false, ref y);
                Switch(content, "AutoRPS", "自动猜拳", false, ref y);
            }
            else
            {
                Section(content, "Yu-Gi-Oh! Fight", ref y);
                Paragraph(content, "由社区开发的非官方决斗客户端，提供卡组管理、练习学院、电脑对战、AI 分身与回放。", ref y);
                Section(content, "版权与第三方来源", ref y);
                Paragraph(content, "Yu-Gi-Oh! 的名称、卡片、角色及相关游戏资源属于其权利人，包括 Konami、集英社及 Studio Dice。本项目与这些权利人没有隶属、赞助或认可关系。", ref y);
                Paragraph(content, "客户端基于 MDPro3 和开源决斗组件。源代码与第三方组件遵循各自的许可；游戏资源不由本项目授予使用许可。完整来源、版权和许可见 THIRD_PARTY_NOTICES.md。", ref y);
                FightUI.Button(content, "项目维护者 · keriae", 16, y + 4, 300, 44, () => Application.OpenURL("https://github.com/keriae")); y += 66;
            }
            content.sizeDelta = new Vector2(readerWidth, Mathf.Max(390, y + 12));
            scroll.verticalNormalizedPosition = scrollPosition;
        }
        private static string LocaleName(string locale) => locale switch { "zh-CN" => "简体中文", "zh-TW" => "繁體中文", "en-US" => "English", "ja-JP" => "日本語", "ko-KR" => "한국어", "de-DE" => "Deutsch", "fr-FR" => "Français", "es-ES" => "Español", "it-IT" => "Italiano", "pt-PT" => "Português", _ => locale };
        private void Section(Transform parent, string title, ref float y)
        {
            FightUI.Text(parent, title, 16, y + 8, 800, 28, 16, FightUI.Gold); y += 42;
            FightUI.Line(parent, 16, y, 788, FightUI.Border); y += 12;
        }
        private void Paragraph(Transform parent, string value, ref float y)
        {
            var text = FightUI.Text(parent, value, 16, y, 782, 84, 18, FightUI.Foreground);
            text.overflowMode = TextOverflowModes.Overflow; text.enableWordWrapping = true;
            float measured = Mathf.Max(64, text.GetPreferredValues(text.text, 782, float.PositiveInfinity).y + 12);
            text.rectTransform.sizeDelta = new Vector2(782, measured); y += measured + 14;
        }
        private void Switch(Transform parent, string key, string label, bool defaultValue, ref float y)
        {
            FightUI.FocusKey(FightUI.SwitchRow(parent, label, Config.GetBool(key, defaultValue), 4, y, 800, 48,
                () => { Set(key, Config.GetBool(key, defaultValue) ? "0" : "1"); Render(); }), "duel:setting:" + key);
            FightUI.Line(parent, 16, y + 48, 788, FightUI.Border); y += 56;
        }
        private void Number(Transform parent, string key, string label, float defaultValue, float min, float max, ref float y, bool percentage = false)
        {
            float value = Config.GetFloat(key, defaultValue);
            FightUI.Text(parent, label, 16, y, 240, 44, 18, FightUI.Foreground, TextAlignmentOptions.MidlineLeft);
            var readout = FightUI.Text(parent, percentage ? Mathf.RoundToInt(value * 100) + "%" : value.ToString("0.##", CultureInfo.InvariantCulture) + "×", 694, y, 110, 44, 18, FightUI.Gold, TextAlignmentOptions.MidlineRight);
            var area = FightUI.Node(parent, "Slider " + key, 278, y + 2, 382, 40);
            var track = FightUI.Panel(area, 0, 18, 382, 3, FightUI.Border, false);
            var fill = FightUI.Panel(track, 0, 0, 382, 3, FightUI.Gold, false);
            fill.sizeDelta = Vector2.zero; fill.anchoredPosition = Vector2.zero;
            var handleArea = FightUI.Node(area, "Handle area", 8, 0, 366, 40);
            var handle = FightUI.Panel(handleArea, 0, 13, 14, 14, FightUI.Active, false);
            handle.pivot = new Vector2(.5f, .5f); handle.sizeDelta = new Vector2(14, -26); handle.anchoredPosition = Vector2.zero;
            var slider = FightUI.FocusKey(area.gameObject.AddComponent<Slider>(), "duel:setting:" + key);
            slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = min; slider.maxValue = max; slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(next =>
            {
                Set(key, Mathf.RoundToInt(next * 1000).ToString(CultureInfo.InvariantCulture));
                readout.text = percentage ? Mathf.RoundToInt(next * 100) + "%" : next.ToString("0.##", CultureInfo.InvariantCulture) + "×";
            });
            FightUI.Line(parent, 16, y + 48, 788, FightUI.Border); y += 58;
        }
        private void Choice(Transform parent, string key, string label, string defaultValue, string[] values, string[] labels, ref float y)
        {
            string current = Config.Get(key, defaultValue); int index = Array.IndexOf(values, current);
            FightUI.Text(parent, label, 16, y, 248, 44, 18, FightUI.Foreground, TextAlignmentOptions.MidlineLeft);
            var button = FightUI.FocusKey(FightUI.Button(parent, index >= 0 ? labels[index] : current, 278, y, 526, 44,
                () => Choose(label, values, labels, current, next => { Set(key, next); Render(); })), "duel:setting:" + key);
            button.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
            FightUI.Icon(button.transform, "chevron", 498, 16, 12, 12, FightUI.Gold);
            if (key == "ScreenMode") button.onClick.RemoveAllListeners();
            if (key == "ScreenMode") button.onClick.AddListener(() => Choose(label, values, labels, current, next => { Set(key, next); Render(); }, item => item != "0" || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor));
            FightUI.Line(parent, 16, y + 50, 788, FightUI.Border); y += 60;
        }
        private void Set(string key, string value)
        {
            Config.Set(key, value);
            float number = int.TryParse(value, out int parsed) ? parsed / 1000f : 0;
            if (key == "BgmVol") AudioManager.SetBGMVol(number);
            else if (key == "SEVol") AudioManager.SetSeVol(number);
            else if (key == "VoiceVol") AudioManager.SetVoiceVol(number);
            else if (key == "Scale") Program.instance.camera_.urpAsset.renderScale = number;
            else if (key == "Quality") QualitySettings.SetQualityLevel((int)number, true);
            else if (key == "AAA") SettingServantUI.ChangeAAA(number);
            else if (key == "Shadow") SettingServantUI.ChangeShadow(number);
            else if (key == "FPS") SettingServantUI.ChangeFPS(number);
            else if (key == "ShowFPS") SettingServantUI.ChangeShowFPS();
            else if (key == "ScreenMode" || key == "Resolution")
            {
                var parts = Config.Get("Resolution", Screen.width + " x " + Screen.height).Split('x');
                if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int w) && int.TryParse(parts[1].Trim(), out int h))
                    Screen.SetResolution(w, h, Config.Get("ScreenMode", "1") == "2" ? FullScreenMode.Windowed : Config.Get("ScreenMode", "1") == "0" ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.FullScreenWindow);
            }
            else if (key == "Layout") UIManager.ChangeLayout();
            else if (key == "Background") Program.instance.background_.Change(int.Parse(value));
            else if (key == "Language" || key == "CardLanguage" || key == "CardStyle")
            { if (key == "CardStyle") MaterialLoader.ClearCachedOverFrameMasks(); UIManager.ChangeLanguage(); }
            else if (key == "VideoCard") SystemEvent.CallVideoCardConfigChangeEvent();
            else if (key.EndsWith("FaceDown", StringComparison.Ordinal)) foreach (var card in OcgCore.cards) card.ShowFaceDownCardOrNot(card.NeedShowFaceDownCard());
            else if (key.EndsWith("Acc", StringComparison.Ordinal) && OcgCore.Accing && !key.EndsWith("AutoAcc", StringComparison.Ordinal)) Program.instance.ocgcore.GetUI<OcgCoreUI>().OnAcc();
        }
        private RectTransform ShowDialog(string title, float dialogHeight = 440, bool nested = false)
        {
            if (nested && dialog != null)
            {
                dialogHistory.Push(new DialogFrame { Panel = dialog, Shade = dialogShade, OutsideSelection = dialogSelection,
                    Selection = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject });
                dialog.gameObject.SetActive(false); dialogShade.gameObject.SetActive(false);
            }
            else CloseDialog();
            dialogSelection = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            dialogShade = FightUI.Panel(root, -4000, -4000, 9280, 8720, new Color(.005f, .014f, .022f, .7f), false, "Duel secondary backdrop");
            dialogShade.gameObject.AddComponent<Button>().onClick.AddListener(DismissDialog);
            dialog = FightUI.Panel(root, 326, (720 - dialogHeight) / 2, 628, dialogHeight, FightUI.PanelColor, true, "Duel secondary dialog");
            FightUI.Text(dialog, title, 24, 20, 532, 38, 24, FightUI.Active);
            var close = FightUI.FocusKey(FightUI.Button(dialog, dialogHistory.Count > 0 ? "←" : "×", 566, 18, 38, 38, DismissDialog), "duel:dialog-close");
            FightUI.Hint(close.gameObject, dialogHistory.Count > 0 ? "返回上一级" : "关闭弹窗");
            FightUI.Line(dialog, 24, 70, 580, FightUI.Border);
            Focus(close);
            return dialog;
        }
        private void DismissDialog()
        {
            if (dialogHistory.Count > 0)
            {
                DestroyDialog(dialog, dialogShade);
                var parent = dialogHistory.Pop(); dialog = parent.Panel; dialogShade = parent.Shade; dialogSelection = parent.OutsideSelection;
                dialogShade.gameObject.SetActive(true); dialog.gameObject.SetActive(true);
                var retained = parent.Selection == null ? null : parent.Selection.GetComponent<Selectable>();
                Focus(retained != null && retained.IsInteractable() ? retained : dialog.GetComponentInChildren<Selectable>());
                RefreshNavigation();
            }
            else if (page == "card") Close(); else CloseDialog();
        }
        private static void DestroyDialog(RectTransform panel, RectTransform shade)
        {
            if (panel != null) { panel.gameObject.SetActive(false); Destroy(panel.gameObject); }
            if (shade != null) { shade.gameObject.SetActive(false); Destroy(shade.gameObject); }
        }
        internal void CloseDialog()
        {
            bool hadDialog = dialog != null;
            DestroyDialog(dialog, dialogShade);
            while (dialogHistory.Count > 0)
            {
                var parent = dialogHistory.Pop(); DestroyDialog(parent.Panel, parent.Shade); dialogSelection = parent.OutsideSelection;
            }
            dialog = dialogShade = null;
            if (hadDialog && UIManager.InputBlocker == this && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(dialogSelection != null && dialogSelection.activeInHierarchy ? dialogSelection : null);
            dialogSelection = null;
            focusedControl = null;
            RefreshNavigation();
        }
        private void Choose(string title, string[] values, string[] labels, string current, Action<string> chosen, Func<string, bool> available = null)
        {
            var panel = ShowDialog(title, Mathf.Min(560, 104 + values.Length * 54));
            float listHeight = panel.rect.height - 96;
            var content = FightUI.Scroll(panel, 20, 84, 588, listHeight, values.Length * 54);
            Selectable selected = null;
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i]; bool active = value == current;
                var button = FightUI.FocusKey(FightUI.Button(content, labels[i], 4, i * 54, 566, 46, () => { CloseDialog(); chosen(value); }, active, available == null || available(value)), "duel:choice:" + value);
                var text = button.GetComponentInChildren<TextMeshProUGUI>(); text.alignment = TextAlignmentOptions.MidlineLeft;
                if (active) FightUI.Text(button.transform, "✓", 528, 0, 28, 46, 20, FightUI.Active, TextAlignmentOptions.Center);
                if (active && button.IsInteractable()) selected = button;
            }
            RefreshNavigation();
            if (selected != null) Focus(selected);
        }
        private void ConfirmExit(bool retry)
        {
            string title = retry ? "重新开始对局" : OcgCore.condition == OcgCore.Condition.Duel ? "结束对局" : OcgCore.condition == OcgCore.Condition.Replay ? "退出回放" : "退出观战";
            var panel = ShowDialog(title, 260);
            string message = retry ? "当前对局将结束，并按本次设置重新开始。"
                : ContextText("确定结束当前对局吗？正在进行的对局会按投降处理。", "确定退出当前回放吗？", "确定退出当前观战吗？");
            FightUI.Text(panel, message, 24, 90, 580, 70, 18, FightUI.Foreground).name = "Duel exit message";
            string continueLabel = ContextText("继续对局", "继续回放", "继续观战");
            FightUI.Button(panel, continueLabel, 24, 184, 270, 48, CloseDialog);
            var confirm = FightUI.FocusKey(FightUI.Button(panel, retry ? "确认重新开始" : "确认退出", 310, 184, 294, 48, () =>
            {
                Close();
                var core = Program.instance.ocgcore;
                if (retry) core.onSurrenderConfirmed = Program.instance.solo.QueueRetryLastSoloDuel;
                core.ConfirmAuxiliaryExit();
            }, true), "duel:confirm-exit");
            FightUI.StyleDestructiveButton(confirm);
            RefreshNavigation();
        }

        private void OnApplicationQuit()
        {
            if (Showing) Config.Save();
        }
        private void OnDestroy()
        {
            if (UIManager.InputBlocker == this) UIManager.InputBlocker = previousBlocker;
            if (canvas != null) Destroy(canvas.gameObject);
            if (Instance == this) Instance = null;
        }
    }
}
