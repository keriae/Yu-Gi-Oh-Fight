using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MDPro3.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YgomSystem.ElementSystem;
using YgomSystem.UI;

namespace MDPro3.UI.Popup
{
    public class PopupDeckAcademy : Popup
    {
        public DeckAcademyCatalog catalog;
        public DeckAcademyDeck initialDeck;
        public bool helpOnly;

        private static readonly FieldInfo NavigationEventField =
            typeof(SelectionButton).GetField("navigationEvent", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo NavigationAddedField =
            typeof(SelectionButton).GetField("nonPersistentNavigationEventAdded", BindingFlags.Instance | BindingFlags.NonPublic);

        private DeckAcademyDeck selectedDeck;
        private int page = -1;
        private bool leaving;
        private TextMeshProUGUI textTemplate;
        private TextMeshProUGUI pageTitle;
        private TextMeshProUGUI pageText;
        private TextMeshProUGUI pageNumber;
        private ScrollRect scroll;
        private ScrollRect libraryScroll;
        private UIScrollToSelection libraryAutoScroll;
        private TextMeshProUGUI libraryCount;
        private GameObject noResults;
        private TMP_InputField searchInput;
        private string category;
        private SelectionButton previousButton;
        private SelectionButton nextButton;
        private SelectionButton overviewButton;
        private SelectionButton practiceButton;
        private SelectionButton replayButton;
        private SelectionButton importButton;
        private bool hasMatches = true;
        private readonly List<SelectionButton> deckButtons = new();
        private readonly List<DeckAcademyDeck> deckEntries = new();
        private readonly Dictionary<DeckAcademyDeck, string> searchableCards = new();
        private readonly Dictionary<string, SelectionButton> categoryButtons = new();

        protected override void InitializeSelections()
        {
            args = new List<string> { helpOnly ? "展开教程" : "卡组学院 / 展开教程" };
            base.InitializeSelections();
            selectedDeck = initialDeck ?? catalog.decks[0];
            if (helpOnly)
                page = Mathf.Clamp(DeckAcademy.ActiveTutorialPage, -1, selectedDeck.steps.Length);

            var content = Manager.GetElement<RectTransform>("Content");
            content.anchoredPosition = Vector2.zero;
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1160f);
            if (content.TryGetComponent<ContentSizeFitter>(out var contentFitter))
                contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            var icon = Manager.GetElement("Icon");
            if (icon != null)
                icon.SetActive(false);
            textTemplate = Manager.GetElement<TextMeshProUGUI>("FrameText");
            if (textTemplate != null)
                textTemplate.transform.parent.gameObject.SetActive(false);

            BuildPanel(content);
            ScrollSpeed.Apply(gameObject);
            RefreshPage();
            ApplyFilters();
            lastSelectable = deckButtons[deckEntries.IndexOf(selectedDeck)].GetComponent<Selectable>();
        }

        private void BuildPanel(RectTransform content)
        {
            var template = Manager.GetElement<SelectionButton>("DecideButton");
            var originalGroup = template.transform.parent.gameObject;
            var root = Layout("DeckAcademy", content, false, 0f, 0f);
            root.SetSiblingIndex(originalGroup.transform.GetSiblingIndex());
            var rootLayout = root.GetComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(36, 36, 20, 20);
            rootLayout.spacing = 16f;
            root.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var body = Layout("Body", root, true, 0f, 470f);
            body.GetComponent<HorizontalLayoutGroup>().spacing = 24f;
            body.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var choices = Layout("Decks", body, false, 260f, 470f);
            choices.GetComponent<LayoutElement>().flexibleWidth = 0f;
            choices.GetComponent<VerticalLayoutGroup>().spacing = 7f;
            var libraryLabel = CreateLabel("LibraryLabel", choices, "当前游戏卡池", 24f, 32f);
            libraryLabel.enableWordWrapping = false;
            libraryLabel.overflowMode = TextOverflowModes.Ellipsis;
            BuildLibrary(choices, template);
            if (helpOnly)
                CreateLabel("PracticeHelp", choices, DeckAcademy.HasActiveReplay
                    ? "按录制的展开逐步查看。\n可随时关闭教程，继续演示。"
                    : "按步骤完成操作。\n可随时关闭教程，继续练习。", 23f, 160f);

            var details = Layout("Details", body, false, 0f, 470f);
            details.GetComponent<LayoutElement>().flexibleWidth = 1f;
            details.GetComponent<VerticalLayoutGroup>().spacing = 10f;
            pageTitle = CreateLabel("PageTitle", details, string.Empty, 29f, 42f);
            var pages = Layout("Pages", details, true, 0f, 44f);
            pages.GetComponent<HorizontalLayoutGroup>().spacing = 8f;
            previousButton = CreateButton(template, pages, "上一步", () => ChangePage(-1), 44f, 128f);
            overviewButton = CreateButton(template, pages, "总览", () => { page = -1; RefreshPage(); }, 44f, 110f);
            pageNumber = CreateLabel("PageNumber", pages, string.Empty, 22f, 44f);
            pageNumber.alignment = TextAlignmentOptions.Center;
            pageNumber.GetComponent<LayoutElement>().flexibleWidth = 1f;
            nextButton = CreateButton(template, pages, "下一步", () => ChangePage(1), 44f, 128f);
            BuildScroll(details);

            var actions = Layout("Actions", root, true, 0f, 56f);
            actions.GetComponent<HorizontalLayoutGroup>().spacing = 12f;
            if (!helpOnly)
            {
                importButton = CreateButton(template, actions, "导入并编辑", () => LeaveWith(DeckAcademy.ImportAndEdit), 56f);
                practiceButton = CreateButton(template, actions, "固定起手练习", () => LeaveWith(DeckAcademy.StartPractice), 56f);
                replayButton = CreateButton(template, actions, "观看展开演示", () => LeaveWith(DeckAcademy.PlayReplay), 56f);
            }
            CreateButton(template, actions, "关闭", Hide, 56f);
            originalGroup.SetActive(false);
            foreach (var selectable in root.GetComponentsInChildren<Selectable>(true))
            {
                var navigation = selectable.navigation;
                navigation.mode = Navigation.Mode.Automatic;
                selectable.navigation = navigation;
            }
        }

        private void BuildScroll(Transform parent)
        {
            var go = new GameObject("TutorialScroll", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(ScrollRect));
            go.transform.SetParent(parent, false);
            var layout = go.GetComponent<LayoutElement>();
            layout.preferredHeight = 364f;
            layout.flexibleWidth = 1f;
            go.GetComponent<Image>().color = new Color(0.015f, 0.04f, 0.075f, 0.75f);
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(go.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(18f, 14f);
            viewportRect.offsetMax = new Vector2(-18f, -14f);
            pageText = CreateLabel("TutorialText", viewport.transform, string.Empty, 24f, 0f);
            var textRect = pageText.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;
            pageText.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll = go.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = textRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 12f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
        }

        private void SelectDeck(DeckAcademyDeck entry)
        {
            selectedDeck = entry;
            page = -1;
            RefreshPage();
        }

        private void BuildLibrary(Transform parent, SelectionButton template)
        {
            if (!helpOnly)
            {
                searchInput = BuildSearch(parent);
                var first = Layout("CategoriesFirst", parent, true, 0f, 34f);
                first.GetComponent<HorizontalLayoutGroup>().spacing = 4f;
                var second = Layout("CategoriesSecond", parent, true, 0f, 34f);
                second.GetComponent<HorizontalLayoutGroup>().spacing = 4f;
                var categories = new[] { "全部", "动画经典", "经典竞技", "神与经典", "当前精选" };
                for (var index = 0; index < categories.Length; index++)
                {
                    var value = categories[index];
                    categoryButtons[value] = CreateButton(template, index < 3 ? first : second, CategoryLabel(value),
                        () => { category = value == "全部" ? null : value; ApplyFilters(); }, 34f, compact: true);
                }
            }
            var viewportObject = new GameObject("LibraryScroll", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(ScrollRect));
            viewportObject.transform.SetParent(parent, false);
            var element = viewportObject.GetComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = helpOnly ? 54f : 264f;
            viewportObject.GetComponent<Image>().color = new Color(0.01f, 0.025f, 0.045f, 0.45f);
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(viewportObject.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(0f, 4f);
            viewportRect.offsetMax = new Vector2(-10f, -4f);
            var entriesRoot = Layout("Entries", viewport.transform, false, 0f, 0f);
            entriesRoot.anchorMin = new Vector2(0f, 1f);
            entriesRoot.anchorMax = Vector2.one;
            entriesRoot.pivot = new Vector2(0.5f, 1f);
            entriesRoot.sizeDelta = Vector2.zero;
            entriesRoot.GetComponent<VerticalLayoutGroup>().spacing = 6f;
            entriesRoot.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            foreach (var entry in helpOnly ? new[] { selectedDeck } : catalog.decks.ToArray())
            {
                var captured = entry;
                deckEntries.Add(entry);
                deckButtons.Add(CreateButton(template, entriesRoot, entry.name, () => SelectDeck(captured), 42f));
                if (!helpOnly)
                {
                    var deck = DeckAcademy.Template(entry);
                    searchableCards[entry] = DeckAcademy.CardNames(deck.Main.Concat(deck.Extra).Distinct());
                }
            }
            noResults = CreateLabel("NoResults", entriesRoot, "没有匹配的卡组", 22f, 42f).gameObject;
            libraryScroll = viewportObject.GetComponent<ScrollRect>();
            libraryScroll.viewport = viewportRect;
            libraryScroll.content = entriesRoot;
            libraryScroll.horizontal = false;
            libraryScroll.vertical = true;
            libraryScroll.scrollSensitivity = 12f;
            libraryScroll.movementType = ScrollRect.MovementType.Clamped;
            libraryAutoScroll = viewportObject.AddComponent<UIScrollToSelection>();
            libraryAutoScroll.topPadding = libraryAutoScroll.bottomPadding = 4f;
            BuildLibraryScrollbar(viewportObject.transform, libraryScroll);
            if (!helpOnly)
                libraryCount = CreateLabel("LibraryCount", parent, string.Empty, 20f, 26f);
        }

        private TMP_InputField BuildSearch(Transform parent)
        {
            var go = new GameObject("LibrarySearch", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().minHeight = go.GetComponent<LayoutElement>().preferredHeight = 38f;
            var image = go.GetComponent<Image>();
            image.color = new Color(0.025f, 0.06f, 0.1f, 1f);
            var viewport = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(go.transform, false);
            var rect = viewport.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 5f);
            rect.offsetMax = new Vector2(-8f, -5f);
            var text = CreateLabel("SearchText", viewport.transform, string.Empty, 21f, 0f);
            var placeholder = CreateLabel("Placeholder", viewport.transform, "搜索卡组或卡名", 21f, 0f);
            placeholder.color = new Color(0.67f, 0.75f, 0.82f, 1f);
            foreach (var label in new[] { text, placeholder })
            {
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.enableWordWrapping = false;
                label.raycastTarget = false;
            }
            var input = go.GetComponent<TMP_InputField>();
            input.textViewport = rect;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.targetGraphic = image;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 64;
            input.onValueChanged.AddListener(_ => ApplyFilters());
            return input;
        }

        private void BuildLibraryScrollbar(Transform parent, ScrollRect target)
        {
            var go = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-7f, 4f);
            rect.offsetMax = new Vector2(0f, -4f);
            go.GetComponent<Image>().color = new Color(0.22f, 0.32f, 0.42f, 0.35f);
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

        private void ApplyFilters()
        {
            var previouslyMatched = hasMatches;
            var count = 0;
            DeckAcademyDeck first = null;
            for (var index = 0; index < deckEntries.Count; index++)
            {
                var entry = deckEntries[index];
                var visible = helpOnly || DeckAcademy.Matches(entry, searchInput.text, category, searchableCards[entry]);
                deckButtons[index].gameObject.SetActive(visible);
                if (visible) { count++; if (first == null) first = entry; }
            }
            noResults.SetActive(count == 0);
            hasMatches = count > 0;
            if (libraryCount != null)
                libraryCount.text = count + " / " + catalog.decks.Count + " 套卡组";
            foreach (var pair in categoryButtons)
                pair.Value.SetButtonText((pair.Key == (category ?? "全部") ? "▶ " : string.Empty) + CategoryLabel(pair.Key));
            if (first != null && !deckButtons[deckEntries.IndexOf(selectedDeck)].gameObject.activeSelf)
            {
                selectedDeck = first;
                page = -1;
                RefreshPage();
            }
            else if (hasMatches && !previouslyMatched)
                RefreshPage();
            if (!hasMatches)
            {
                pageTitle.text = "没有匹配的卡组";
                pageNumber.text = string.Empty;
                pageText.text = "请清空或更换关键词，也可调整分类。";
                previousButton.GetComponent<Selectable>().interactable = false;
                nextButton.GetComponent<Selectable>().interactable = false;
            }
            if (importButton != null)
                importButton.GetComponent<Selectable>().interactable = hasMatches;
            overviewButton.GetComponent<Selectable>().interactable = hasMatches;
            if (practiceButton != null)
                practiceButton.GetComponent<Selectable>().interactable = hasMatches && DeckAcademy.CanPractice(selectedDeck);
            if (replayButton != null)
                replayButton.GetComponent<Selectable>().interactable = hasMatches && DeckAcademy.CanReplay(selectedDeck);
            Canvas.ForceUpdateCanvases();
            libraryScroll.verticalNormalizedPosition = 1f;
            if (hasMatches)
                libraryAutoScroll.VerticalScrollTo(deckButtons[deckEntries.IndexOf(selectedDeck)].GetComponent<RectTransform>());
        }

        private static string CategoryLabel(string value) => value switch
        {
            "动画经典" => "动画",
            "经典竞技" => "竞技",
            "当前精选" => "精选",
            _ => value
        };

        private void ChangePage(int delta)
        {
            if (!hasMatches)
                return;
            page = Mathf.Clamp(page + delta, -1, selectedDeck.steps.Length);
            RefreshPage();
        }

        private void RefreshPage()
        {
            var text = new StringBuilder();
            if (page < 0)
            {
                var deck = DeckAcademy.Template(selectedDeck);
                pageTitle.text = selectedDeck.name + " · 核心玩法";
                pageNumber.text = "总览";
                text.AppendLine(selectedDeck.description).AppendLine();
                text.Append("主卡组 ").Append(deck.Main.Count).Append(" / 额外 ").Append(deck.Extra.Count)
                    .Append(" / 副卡组 ").Append(deck.Side.Count).AppendLine();
                text.Append(DeckAcademy.Category(selectedDeck));
                if (!string.IsNullOrEmpty(selectedDeck.difficulty))
                    text.Append(" · ").Append(selectedDeck.difficulty);
                text.AppendLine();
                text.Append("适用禁限表：").AppendLine(selectedDeck.regulation).AppendLine();
                text.AppendLine("固定起手（5 张）").AppendLine(DeckAcademy.CardNames(selectedDeck.starter_hand)).AppendLine();
                if (selectedDeck.starter_cards != null && selectedDeck.starter_cards.Length > 0)
                    text.AppendLine("核心启动卡").AppendLine(DeckAcademy.CardNames(selectedDeck.starter_cards)).AppendLine();
            }
            else if (page < selectedDeck.steps.Length)
            {
                var step = selectedDeck.steps[page];
                pageTitle.text = selectedDeck.name + " · " + step.title;
                pageNumber.text = "步骤 " + (page + 1) + " / " + selectedDeck.steps.Length;
                text.AppendLine(step.text);
                if (step.card_codes != null && step.card_codes.Length > 0)
                    text.AppendLine().AppendLine("涉及卡片").AppendLine(DeckAcademy.CardNames(step.card_codes));
            }
            else
            {
                pageTitle.text = selectedDeck.name + " · 终场与注意事项";
                pageNumber.text = "终场";
                if (selectedDeck.end_board != null)
                {
                    text.AppendLine(selectedDeck.end_board.text).AppendLine();
                    if (selectedDeck.end_board.codes != null && selectedDeck.end_board.codes.Length > 0)
                        text.AppendLine(DeckAcademy.CardNames(selectedDeck.end_board.codes)).AppendLine();
                }
                if (selectedDeck.cautions != null)
                    foreach (var caution in selectedDeck.cautions)
                        text.Append("• ").AppendLine(caution).AppendLine();
            }
            pageText.text = text.ToString();
            if (helpOnly)
                DeckAcademy.SetGuidePage(page);
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1f;
            previousButton.GetComponent<Selectable>().interactable = page >= 0;
            nextButton.GetComponent<Selectable>().interactable = page < selectedDeck.steps.Length;
            if (practiceButton != null)
                practiceButton.GetComponent<Selectable>().interactable = DeckAcademy.CanPractice(selectedDeck);
            if (replayButton != null)
                replayButton.gameObject.SetActive(DeckAcademy.CanReplay(selectedDeck));
            for (var index = 0; index < deckButtons.Count; index++)
                deckButtons[index].SetButtonText((deckEntries[index] == selectedDeck ? "▶ " : string.Empty) + deckEntries[index].name);
        }

        private void LeaveWith(Action<DeckAcademyDeck> action)
        {
            if (leaving || !hasMatches || !DeckAcademy.CanLeaveEditor())
                return;
            leaving = true;
            var entry = selectedDeck;
            quitAction = () =>
            {
                try { action(entry); }
                catch (Exception error) { MessageManager.Cast("卡组教程操作失败：" + error.Message); }
            };
            Hide();
        }

        private RectTransform Layout(string name, Transform parent, bool horizontal, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var layout = horizontal ? (HorizontalOrVerticalLayoutGroup)go.AddComponent<HorizontalLayoutGroup>()
                : go.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var element = go.GetComponent<LayoutElement>();
            element.minWidth = element.preferredWidth = width;
            if (height > 0f)
                element.minHeight = element.preferredHeight = height;
            return go.GetComponent<RectTransform>();
        }

        private TextMeshProUGUI CreateLabel(string name, Transform parent, string text, float size, float height)
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
            label.color = Color.white;
            label.fontSize = size;
            label.richText = false;
            label.enableWordWrapping = true;
            label.alignment = TextAlignmentOptions.TopLeft;
            if (height > 0f)
                go.GetComponent<LayoutElement>().preferredHeight = height;
            return label;
        }

        public static SelectionButton CreateButton(SelectionButton template, Transform parent, string text, Action action,
            float height = 52f, float width = 0f, bool compact = false)
        {
            var button = UnityEngine.Object.Instantiate(template.gameObject, parent, false).GetComponent<SelectionButton>();
            button.name = "AcademyButton";
            button.RemoveAllListeners();
            if (button.TryGetComponent<Button>(out var nativeButton))
            {
                nativeButton.onClick = new Button.ButtonClickedEvent();
                nativeButton.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            }
            NavigationEventField?.SetValue(button, new SelectionButtonNavigationEvent());
            NavigationAddedField?.SetValue(button, false);
            var selectable = button.GetComponent<Selectable>();
            button.SetClickEvent(() =>
            {
                if (selectable != null && selectable.IsInteractable())
                    action?.Invoke();
            });
            button.SetButtonText(text);
            var rect = button.GetComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            var element = button.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = height;
            element.minWidth = element.preferredWidth = width;
            element.flexibleWidth = width > 0f ? 0f : 1f;
            foreach (var label in button.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                label.richText = false;
                label.enableAutoSizing = true;
                label.fontSizeMin = 18f;
                label.fontSizeMax = 26f;
                label.enableWordWrapping = false;
                label.overflowMode = TextOverflowModes.Ellipsis;
            }
            if (compact)
            {
                var caption = button.GetComponent<ElementObjectManager>().GetElement<TextMeshProUGUI>("ButtonText");
                if (caption != null)
                {
                    // The full-size prefab caption has wide insets. Give narrow
                    // guide buttons their own text box while keeping the bound caption.
                    var captionRect = caption.rectTransform;
                    captionRect.SetParent(rect, false);
                    captionRect.localScale = Vector3.one;
                    captionRect.anchorMin = Vector2.zero;
                    captionRect.anchorMax = Vector2.one;
                    captionRect.offsetMin = new Vector2(6f, 2f);
                    captionRect.offsetMax = new Vector2(-6f, -2f);
                    var captionLayout = caption.GetComponent<LayoutElement>() ?? caption.gameObject.AddComponent<LayoutElement>();
                    captionLayout.ignoreLayout = true;
                    caption.margin = Vector4.zero;
                    caption.alignment = TextAlignmentOptions.Center;
                    caption.fontSize = caption.fontSizeMax = 20f;
                    caption.fontSizeMin = 16f;
                }
            }
            return button;
        }
    }
}
