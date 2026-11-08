using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;


using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;

using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI.Popup
{
    /// <summary>Runtime UI built from the game's existing popup and button assets.</summary>
    public class PopupResponsesAI : Popup
    {
        private MDPro3.AI.ResponsesAiConfig config;
        private TextMeshProUGUI textTemplate;
        private SelectionButton buttonTemplate;
        private TextMeshProUGUI statusText;
        private TMP_InputField baseUrl;
        private TMP_InputField apiKey;
        private TMP_InputField model;
        private List<MDPro3.AI.AiConfigField> headerFields = new();
        private RectTransform headerEntries;
        private TMP_InputField extraBody;
        private ScrollRect advancedScroll;
        private SelectionButton addHeaderButton;
        private TMP_InputField timeout;
        private TMP_InputField maxOutput;
        private TMP_InputField promptBudget;
        private SelectionButton reasoningButton;
        private string reasoningEffort;
        private TMP_InputField search;
        private SelectionButton rememberButton;
        private SelectionButton playerMode;
        private SelectionButton spectatorMode;
        private SelectionButton slotA;
        private SelectionButton slotB;
        private SelectionButton testButton;
        private SelectionButton startButton;
        private SelectionButton saveButton;
        private readonly List<GameObject> pages = new();
        private readonly List<SelectionButton> tabs = new();
        private readonly List<MDPro3.AI.ResponsesDeckChoice> decks = new();
        private readonly List<SelectionButton> deckButtons = new();
        private TextMeshProUGUI emptyDecks;
        private ScrollRect deckScroll;
        private int deckSlot = 1;
        private bool aiVsAi;
        private bool rememberKey;
        private bool leaving;
        private CancellationTokenSource connectionTest;

        public static void Open()
        {
            var handle = UnityEngine.AddressableAssets.Addressables.InstantiateAsync("Popup/PopupYesOrNo.prefab");
            handle.Completed += result =>
            {
                if (result.Result == null)
                {
                    Runtime.MessageManager.Cast("AI 对战界面加载失败。");
                    return;
                }
                result.Result.transform.SetParent(Runtime.Program.instance.ui_.popup, false);
                result.Result.GetComponent<PopupYesOrNo>().enabled = false;
                result.Result.AddComponent<PopupResponsesAI>().Show();
            };
        }

        protected override void InitializeSelections()
        {
            args = new List<string> { "AI 对战" };
            base.InitializeSelections();
            try { config = MDPro3.AI.ResponsesAiConfigStore.Load(); }
            catch { config = new MDPro3.AI.ResponsesAiConfig(); }
            aiVsAi = config.AiVsAi;
            rememberKey = config.RememberApiKey;

            var content = Manager.GetElement<RectTransform>("Content");
            content.anchoredPosition = Vector2.zero;
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1160f);
            if (content.TryGetComponent<ContentSizeFitter>(out var fitter))
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            Manager.GetElement("Icon")?.SetActive(false);
            textTemplate = Manager.GetElement<TextMeshProUGUI>("FrameText");
            if (textTemplate != null)
                textTemplate.transform.parent.gameObject.SetActive(false);
            buttonTemplate = Manager.GetElement<SelectionButton>("DecideButton");
            var originalGroup = buttonTemplate.transform.parent;
            var root = Layout("ResponsesAI", content, false);
            root.SetSiblingIndex(originalGroup.GetSiblingIndex());
            root.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(36, 36, 18, 18);
            root.GetComponent<VerticalLayoutGroup>().spacing = 12f;
            root.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var modes = Layout("Modes", root, true, 44f);
            playerMode = Button(modes, "玩家 vs AI", () => SetMode(false), 44f);
            spectatorMode = Button(modes, "AI vs AI", () => SetMode(true), 44f);
            var tabRow = Layout("Tabs", root, true, 42f);
            var names = new[] { "连接设置", "对战卡组", "高级设置" };
            for (var i = 0; i < names.Length; i++)
            {
                var index = i;
                tabs.Add(Button(tabRow, names[i], () => ShowPage(index), 42f));
            }

            var panel = Layout("Pages", root, false, 358f);
            BuildConnection(panel);
            BuildDecks(panel);
            BuildAdvanced(panel);
            statusText = Label("Status", root, string.Empty, 21f, 54f);
            statusText.overflowMode = TextOverflowModes.Ellipsis;
            var actions = Layout("Actions", root, true, 52f);
            saveButton = Button(actions, "保存设置", Save, 52f);
            startButton = Button(actions, "开始对战", StartDuel, 52f);
            Button(actions, "关闭", Hide, 52f);
            originalGroup.gameObject.SetActive(false);
            ApplyConfig();
            LoadDecks();
            SetMode(aiVsAi);
            ShowPage(0);
            ScrollSpeed.Apply(gameObject);
            lastSelectable = baseUrl;
        }

        private void BuildConnection(Transform parent)
        {
            var page = Layout("Connection", parent, false, 358f);
            pages.Add(page.gameObject);
            page.GetComponent<VerticalLayoutGroup>().spacing = 12f;
            baseUrl = InputRow(page, "服务地址", "https://api.openai.com/v1", 52f);
            apiKey = InputRow(page, "密钥", string.Empty, 52f, password: true);
            model = InputRow(page, "模型名称", "gpt-6.1-sol", 52f);
            var tools = Layout("ConnectionTools", page, true, 44f);
            testButton = Button(tools, "测试连接", TestConnection, 44f);
            rememberButton = Button(page, string.Empty, () =>
            {
                rememberKey = !rememberKey;
                UpdateRememberButton();
            }, 42f);
        }

        private void BuildDecks(Transform parent)
        {
            var page = Layout("Decks", parent, false, 358f);
            pages.Add(page.gameObject);
            page.GetComponent<VerticalLayoutGroup>().spacing = 8f;
            var slots = Layout("Slots", page, true, 48f);
            slotA = Button(slots, string.Empty, () => { deckSlot = 0; RefreshDecks(); }, 48f);
            slotB = Button(slots, string.Empty, () => { deckSlot = 1; RefreshDecks(); }, 48f);
            search = Input("SearchDeck", page, "搜索我的 / 推荐卡组", 42f);
            search.onValueChanged.AddListener(_ => RefreshDecks());
            var entries = Scroll("DeckChoices", page, 240f, out deckScroll);
            emptyDecks = Label("EmptyDecks", entries, "没有匹配的卡组", 22f, 40f);
        }

        private void BuildAdvanced(Transform parent)
        {
            var page = Layout("Advanced", parent, false, 358f);
            pages.Add(page.gameObject);
            var entries = Scroll("AdvancedSettings", page, 358f, out advancedScroll);
            entries.GetComponent<VerticalLayoutGroup>().spacing = 10f;
            timeout = InputRow(entries, "请求超时（秒）", "90", 44f);
            var reasoningRow = Layout("Reasoning", entries, true, 44f);
            var caption = Label("Label", reasoningRow, "思考强度", 23f, 44f);
            Width(caption.gameObject, 250f);
            reasoningButton = Button(reasoningRow, string.Empty, ChooseReasoning, 44f);
            maxOutput = InputRow(entries, "输出上限", "4096", 44f);
            promptBudget = InputRow(entries, "上下文上限", "200000", 44f);
            Label("HeadersLabel", entries, "额外请求头", 23f, 32f);
            headerEntries = Layout("HeaderFields", entries, false);
            addHeaderButton = Button(entries, "＋ 添加请求头", AddHeader, 42f);
            Label("BodyLabel", entries, "额外参数", 23f, 32f);
            extraBody = Input("ExtraBody", entries, "{}", 160f, multiline: true);
            extraBody.characterLimit = 65536;
        }

        private void ApplyConfig()
        {
            baseUrl.text = config.BaseUrl ?? string.Empty;
            apiKey.text = config.ApiKey ?? string.Empty;
            model.text = config.Model ?? "gpt-6.1-sol";
            headerFields = MDPro3.AI.AiConfigForm.ReadHeaders(config.ExtraHeadersJson);
            extraBody.text = config.ExtraBodyJson ?? "{}";
            RenderForms();
            timeout.text = config.TimeoutSeconds.ToString();
            reasoningEffort = EffortValues.Contains(config.ReasoningEffort) ? config.ReasoningEffort : string.Empty;
            UpdateReasoningButton();
            maxOutput.text = config.MaxOutputTokens.ToString();
            promptBudget.text = config.PromptBudgetTokens.ToString();
            rememberKey = config.RememberApiKey;
            UpdateRememberButton();
        }

        private MDPro3.AI.ResponsesAiConfig ReadConfig(bool validate = true)
        {
            MDPro3.AI.ResponsesAiConfig next = config.Clone();
            next.BaseUrl = baseUrl.text.Trim();
            next.ApiKey = apiKey.text.Trim();
            next.Model = model.text.Trim();
            next.ExtraHeadersJson = MDPro3.AI.AiConfigForm.WriteHeaders(headerFields);
            next.ExtraBodyJson = extraBody.text;
            if (!int.TryParse(timeout.text, out var seconds))
                throw new InvalidOperationException("请求超时需要填写整数秒数。");
            if (!int.TryParse(maxOutput.text, out var tokens))
                throw new InvalidOperationException("输出上限需要填写整数。");
            if (!int.TryParse(promptBudget.text, out var budget))
                throw new InvalidOperationException("上下文上限需要填写整数 tokens 数。");
            next.TimeoutSeconds = seconds;
            next.MaxOutputTokens = tokens;
            next.PromptBudgetTokens = budget;
            next.ReasoningEffort = reasoningEffort;
            next.RememberApiKey = rememberKey;
            next.AiVsAi = aiVsAi;
            if (validate)
                next.Validate();
            return next;
        }

        private void UpdateRememberButton()
        {
            rememberButton.SetButtonText("记住密钥和高级配置：" + (rememberKey ? "开启" : "关闭"));
        }

        private static readonly string[] EffortValues = { "", "low", "medium", "high" };
        private static readonly string[] EffortNames = { "默认", "低", "中", "高" };

        private void ChooseReasoning()
        {
            lastSelectable = reasoningButton.GetComponent<Selectable>();
            var options = new List<string> { "思考强度", string.Empty };
            options.AddRange(EffortNames);
            Runtime.UIManager.ShowPopupSelection(options, () =>
            {
                if (this == null || leaving) return;
                var selected = EventSystem.current.currentSelectedGameObject.GetComponent<SelectionButton>().GetButtonText();
                var index = Array.IndexOf(EffortNames, selected);
                if (index < 0) return;
                reasoningEffort = EffortValues[index];
                UpdateReasoningButton();
            });
        }

        private void UpdateReasoningButton()
        {
            var index = Array.IndexOf(EffortValues, reasoningEffort);
            reasoningButton.SetButtonText(index < 0 ? reasoningEffort : EffortNames[index]);
        }

        private void AddHeader()
        {
            headerFields.Add(new MDPro3.AI.AiConfigField());
            RenderForms();
            var input = headerEntries.GetChild(headerEntries.childCount - 1).GetComponentInChildren<TMP_InputField>();
            if (input != null)
            {
                lastSelectable = input;
                input.Select();
                input.ActivateInputField();
                advancedScroll.GetComponent<UIScrollToSelection>().VerticalScrollTo(input.GetComponent<RectTransform>());
            }
        }

        private void RenderForms()
        {
            var position = advancedScroll.verticalNormalizedPosition;
            for (var i = headerEntries.childCount - 1; i >= 0; i--)
            {
                var child = headerEntries.GetChild(i).gameObject;
                child.SetActive(false);
                child.transform.SetParent(null, false);
                Destroy(child);
            }
            foreach (MDPro3.AI.AiConfigField field in headerFields)
            {
                var row = Layout("HeaderRow", headerEntries, true, 44f);
                row.GetComponent<HorizontalLayoutGroup>().spacing = 8f;
                var name = Input("HeaderName", row, "请求头名称", 44f);
                Width(name.gameObject, 300f);
                name.SetTextWithoutNotify(field.Name ?? string.Empty);
                name.onValueChanged.AddListener(value => field.Name = value);
                var value = Input("HeaderValue", row, "值", 44f);
                value.characterLimit = 65536;
                value.SetTextWithoutNotify(field.Value ?? string.Empty);
                value.onValueChanged.AddListener(text => field.Value = text);
                SelectionButton remove = Button(row, "删除", () => { headerFields.Remove(field); RenderForms(); }, 44f);
                Width(remove.gameObject, 78f);
            }
            lastSelectable = timeout;
            Canvas.ForceUpdateCanvases();
            advancedScroll.verticalNormalizedPosition = position;
        }

        private static void Width(GameObject go, float width)
        {
            var element = go.GetComponent<LayoutElement>();
            element.minWidth = element.preferredWidth = width;
            element.flexibleWidth = 0f;
            element.layoutPriority = 2;
        }

        private void SetMode(bool spectator)
        {
            aiVsAi = spectator;
            playerMode.SetButtonText("玩家 vs AI");
            spectatorMode.SetButtonText("AI vs AI");
            SelectionEdgeHighlight.SetSelected(playerMode, !spectator);
            SelectionEdgeHighlight.SetSelected(spectatorMode, spectator);
            RefreshDecks();
        }

        private void ShowPage(int page)
        {
            var names = new[] { "连接设置", "对战卡组", "高级设置" };
            for (var i = 0; i < pages.Count; i++)
            {
                pages[i].SetActive(i == page);
                tabs[i].SetButtonText(names[i]);
                SelectionEdgeHighlight.SetSelected(tabs[i], i == page);
            }
            lastSelectable = page == 0 ? baseUrl : page == 1 ? search : timeout;
            Canvas.ForceUpdateCanvases();
        }

        private void LoadDecks()
        {
            var parent = emptyDecks.transform.parent;
            MDPro3.AI.ResponsesDeckLibrary library = MDPro3.AI.ResponsesDeckLibrary.Load(Runtime.Program.PATH_DECK);
            if (library.RecommendationError != null)
                Status("推荐卡组加载失败：" + library.RecommendationError, true);
            decks.AddRange(library.Choices);
            BuildDeckButtons(parent);
            RestoreDeckSelections(library);
            RefreshDecks();
        }

        private void BuildDeckButtons(Transform parent)
        {
            foreach (MDPro3.AI.ResponsesDeckChoice deck in decks)
            {
                MDPro3.AI.ResponsesDeckChoice captured = deck;
                deckButtons.Add(Button(parent, deck.Name, () =>
                {
                    if (deckSlot == 0) config.DeckA = captured.Path;
                    else config.DeckB = captured.Path;
                    RefreshDecks(false);
                }, 42f));
            }
            emptyDecks.transform.SetAsLastSibling();
        }

        private void RestoreDeckSelections(MDPro3.AI.ResponsesDeckLibrary library)
        {
            var previousA = config.DeckA;
            var previousB = config.DeckB;
            config.DeckA = library.ResolveSelection(previousA);
            config.DeckB = library.ResolveSelection(previousB);
            if (string.IsNullOrEmpty(previousA))
                config.DeckA = library.FindCurrentDeck(MDPro3.Config.GetConfigDeckName())
                    ?? decks.FirstOrDefault()?.Path;
            if (string.IsNullOrEmpty(previousB))
                config.DeckB = decks.Count > 1 ? decks[1].Path : decks.FirstOrDefault()?.Path;
            if ((!string.IsNullOrEmpty(previousA) && config.DeckA == null)
                || (!string.IsNullOrEmpty(previousB) && config.DeckB == null))
                Status("先前选择的卡组已不在当前列表，请重新选择。", true);
        }

        private string DeckName(string path) => decks.FirstOrDefault(deck => deck.ContainsPath(path))?.Name ?? "未选择";

        private void RefreshDecks(bool resetScroll = true)
        {
            if (slotA == null || search == null)
                return;
            slotA.SetButtonText((aiVsAi ? "AI A：" : "玩家：") + DeckName(config.DeckA));
            slotB.SetButtonText((aiVsAi ? "AI B：" : "AI：") + DeckName(config.DeckB));
            SelectionEdgeHighlight.SetSelected(slotA, deckSlot == 0);
            SelectionEdgeHighlight.SetSelected(slotB, deckSlot == 1);
            var count = 0;
            var terms = search.text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var selected = deckSlot == 0 ? config.DeckA : config.DeckB;
            for (var i = 0; i < decks.Count; i++)
            {
                var visible = decks[i].Matches(terms);
                deckButtons[i].gameObject.SetActive(visible);
                deckButtons[i].SetButtonText(decks[i].Name);
                SelectionEdgeHighlight.SetSelected(deckButtons[i], decks[i].ContainsPath(selected));
                if (visible) count++;
            }
            emptyDecks.gameObject.SetActive(count == 0);
            Canvas.ForceUpdateCanvases();
            if (resetScroll)
                deckScroll.verticalNormalizedPosition = 1f;
        }

        private void Save()
        {
            try
            {
                config = ReadConfig(false);
                MDPro3.AI.ResponsesAiConfigStore.Save(config);
                Status("设置已保存。");
            }
            catch (Exception error) { Status(error.Message, true); }
        }

        private async void TestConnection()
        {
            if (connectionTest != null)
            {
                connectionTest.Cancel();
                return;
            }
            MDPro3.AI.ResponsesAiConfig next;
            try { next = ReadConfig(); }
            catch (Exception error) { Status(error.Message, true); return; }
            var cancellation = new CancellationTokenSource();
            connectionTest = cancellation;
            SetTesting(true);
            Status("正在测试连接 · " + next.Model + "…");
            try
            {
                var schema = JObject.Parse("{\"type\":\"object\",\"properties\":{\"connected\":{\"type\":\"boolean\"}},\"required\":[\"connected\"],\"additionalProperties\":false}");
                using MDPro3.AI.ResponsesAiClient client = new MDPro3.AI.ResponsesAiClient(next);
                var output = await client.RequestAsync("This is an API connectivity test. Return JSON with connected=true.",
                    "Confirm the connection works.", schema, cancellation.Token);
                if (this == null || leaving || cancellation.IsCancellationRequested)
                    return;
                if (JObject.Parse(output)["connected"]?.Value<bool>() != true)
                    throw new InvalidOperationException("服务返回了非预期的连接测试结果。");
                Status("连接成功 · " + next.Model);
            }
            catch (OperationCanceledException)
            {
                if (this != null && !leaving) Status("连接测试已取消。");
            }
            catch (Exception error)
            {
                if (this != null && !leaving)
                    Status(error is MDPro3.AI.AiRequestException || error is MDPro3.AI.AiConfigurationException
                        ? error.Message : "连接测试失败，服务未返回有效的测试结果。", true);
            }
            finally
            {
                if (connectionTest == cancellation)
                    connectionTest = null;
                cancellation.Dispose();
                if (this != null && !leaving) SetTesting(false);
            }
        }

        private void SetTesting(bool testing)
        {
            testButton.SetButtonText(testing ? "取消测试" : "测试连接");
            startButton.GetComponent<Selectable>().interactable = !testing;
            saveButton.GetComponent<Selectable>().interactable = !testing;
        }

        private void StartDuel()
        {
            if (leaving || connectionTest != null)
                return;
            try
            {
                MDPro3.AI.ResponsesAiConfig next = ReadConfig();
                if (string.IsNullOrEmpty(next.DeckA) || string.IsNullOrEmpty(next.DeckB)
                    || !File.Exists(next.DeckA) || !File.Exists(next.DeckB))
                    throw new InvalidOperationException("请在“对战卡组”中为双方选择有效的卡组。");
                MDPro3.AI.ResponsesAiConfigStore.Save(next);
                config = next;
                quitAction = () =>
                {
                    try { Services.ResponsesDuelSession.Start(next, aiVsAi, next.DeckA, next.DeckB); }
                    catch (Exception error) { Runtime.MessageManager.Cast("AI 对战启动失败：" + error.Message); }
                };
                Hide();
            }
            catch (Exception error) { Status(error.Message, true); }
        }

        private void Status(string message, bool error = false)
        {
            statusText.text = message;
            statusText.color = error ? new Color(1f, 0.55f, 0.45f) : new Color(0.72f, 0.88f, 0.97f);
        }

        public override void Hide()
        {
            if (leaving) return;
            leaving = true;
            connectionTest?.Cancel();
            base.Hide();
        }

        protected override void OnDestroy()
        {
            connectionTest?.Cancel();
            base.OnDestroy();
        }

        private RectTransform Layout(string name, Transform parent, bool horizontal, float height = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var layout = horizontal ? (HorizontalOrVerticalLayoutGroup)go.AddComponent<HorizontalLayoutGroup>()
                : go.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = !horizontal;
            layout.childForceExpandHeight = false;
            layout.spacing = horizontal ? 12f : 8f;
            var element = go.GetComponent<LayoutElement>();
            if (height > 0f) element.minHeight = element.preferredHeight = height;
            return go.GetComponent<RectTransform>();
        }

        private TextMeshProUGUI Label(string name, Transform parent, string text, float fontSize, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<TextMeshProUGUI>();
            if (textTemplate != null)
            {
                label.font = textTemplate.font;
                label.fontSharedMaterial = textTemplate.fontSharedMaterial;
            }
            label.text = text;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.richText = false;
            label.enableWordWrapping = true;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            if (height > 0f) go.GetComponent<LayoutElement>().preferredHeight = height;
            return label;
        }

        private SelectionButton Button(Transform parent, string text, Action action, float height)
        {
            SelectionButton button = PopupDeckAcademy.CreateButton(buttonTemplate, parent, text, action, height, compact: true);
            button.name = "ResponsesButton";
            // Prefab decoration can extend past a narrow button. Only the root
            // rectangle should receive clicks beside editable fields.
            foreach (var graphic in button.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
            var hitArea = button.GetComponent<Image>();
            if (hitArea == null)
            {
                hitArea = button.gameObject.AddComponent<Image>();
                hitArea.color = Color.clear;
            }
            hitArea.raycastTarget = true;
            return button;
        }

        private TMP_InputField InputRow(Transform parent, string label, string placeholder, float height, bool password = false)
        {
            var row = Layout("Row" + label, parent, true, height);
            var caption = Label("Label", row, label, 23f, height);
            var element = caption.GetComponent<LayoutElement>();
            element.minWidth = element.preferredWidth = 250f;
            element.flexibleWidth = 0f;
            return Input("Input" + label, row, placeholder, height, password);
        }

        private TMP_InputField Input(string name, Transform parent, string placeholderText, float height, bool password = false, bool multiline = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            var element = go.GetComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = height;
            element.layoutPriority = 2;
            element.minWidth = element.preferredWidth = 0f;
            element.flexibleWidth = 1f;
            var image = go.GetComponent<Image>();
            image.color = new Color(0.025f, 0.06f, 0.1f, 1f);
            var viewport = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(go.transform, false);
            var rect = viewport.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10f, 6f);
            rect.offsetMax = new Vector2(-10f, -6f);
            var text = Label("Text", viewport.transform, string.Empty, 22f, 0f);
            var placeholder = Label("Placeholder", viewport.transform, placeholderText, 22f, 0f);
            placeholder.color = new Color(0.58f, 0.68f, 0.76f, 1f);
            foreach (var label in new[] { text, placeholder })
            {
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
                label.alignment = multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
                label.enableWordWrapping = multiline;
                label.richText = false;
            }
            var input = go.GetComponent<TMP_InputField>();
            input.textViewport = rect;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.targetGraphic = image;
            input.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
            input.contentType = password ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            // TMP also checks its own richText flag before adding IME composition
            // underline tags. Keep it in sync with the plain-text label.
            input.richText = false;
            input.isRichTextEditingAllowed = false;
            input.characterLimit = multiline ? 16384 : password ? 8192 : 2048;
            input.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            input.onSelect.AddListener(_ => lastSelectable = input);
            return input;
        }

        private RectTransform Scroll(string name, Transform parent, float height, out ScrollRect scroll)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(ScrollRect));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().minHeight = go.GetComponent<LayoutElement>().preferredHeight = height;
            go.GetComponent<Image>().color = new Color(0.01f, 0.025f, 0.045f, 0.45f);
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(go.transform, false);
            var rect = viewport.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 6f);
            rect.offsetMax = new Vector2(-18f, -6f);
            var entries = Layout("Entries", rect, false);
            entries.anchorMin = new Vector2(0f, 1f);
            entries.anchorMax = Vector2.one;
            entries.pivot = new Vector2(0.5f, 1f);
            entries.sizeDelta = Vector2.zero;
            entries.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll = go.GetComponent<ScrollRect>();
            scroll.viewport = rect;
            scroll.content = entries;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 12f;
            UIScrollToSelection autoScroll = go.AddComponent<UIScrollToSelection>();
            autoScroll.topPadding = autoScroll.bottomPadding = 4f;
            AddScrollbar(go.transform, scroll);
            return entries;
        }

        private void AddScrollbar(Transform parent, ScrollRect target)
        {
            var go = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-7f, 6f);
            rect.offsetMax = new Vector2(-1f, -6f);
            go.GetComponent<Image>().color = new Color(0.22f, 0.32f, 0.42f, 0.4f);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(go.transform, false);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = handleRect.offsetMax = Vector2.zero;
            handle.GetComponent<Image>().color = new Color(0.63f, 0.77f, 0.87f, 0.85f);
            var scrollbar = go.GetComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handle.GetComponent<Image>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            target.verticalScrollbar = scrollbar;
            target.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }
    }
}
