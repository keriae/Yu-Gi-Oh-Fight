using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;


using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI.Client
{
    internal partial class FightClient
    {
        private ServantUI.DeckEditorUI clientEditorUI;
        private string editorRelativeName;
        private string editorLoadedPath;
        private readonly List<EditorMount> editorMounts = new List<EditorMount>();
        private RectTransform editorViewHost, editorCollectionHost, editorDetailHost;
        private TextMeshProUGUI editorTitleText, editorStateText, editorCountsText, editorSummaryText;
        private Button editorSaveButton;
        private Button editorSectionJumpButton;
        private readonly List<Button> editorToolbarButtons = new List<Button>();
        private RectTransform editorDivider, editorActionHost, editorHeaderInput, editorHeaderArea, editorHeaderNameGroup;
        private Rect editorHeaderInputBounds;
        private float editorActionNativeHeight;
        private EditorWidget editorDeckWidget, editorPoolWidget;
        private EditorInspector editorInspector;
        private readonly Dictionary<int, float> editorCardReadingPositions = new Dictionary<int, float>();
        private FightCardPoolCache editorImageCache;
        private readonly List<EditorTypography> editorTypography = new List<EditorTypography>();
        private readonly Dictionary<ScrollRect, EditorScrollAnchor> editorScrollAnchors = new Dictionary<ScrollRect, EditorScrollAnchor>();
        private readonly Dictionary<SelectionButton_CardInDeck, EditorCardGeometry> editorCardGeometry = new Dictionary<SelectionButton_CardInDeck, EditorCardGeometry>();
        private FightCardSlotFocus editorCardFocusFrame;
        private bool editorOverwriteAllowed;
        private bool editorAwaitingMount;
        private bool editorAwaitingOverwrite;
        private Action editorSaveContinuation;
        private int editorInspectedCode;
        private EditorDeckScroll editorDeckScroll;
        private readonly List<EditorDeckSection> editorDeckSections = new List<EditorDeckSection>();
        private readonly List<EditorTab> editorTabs = new List<EditorTab>();
        private readonly List<EditorGraphic> editorGraphics = new List<EditorGraphic>();
        private readonly Dictionary<Graphic, EditorGraphicStyle> editorGraphicStyles = new Dictionary<Graphic, EditorGraphicStyle>();
        private readonly Dictionary<Selectable, EditorControlStyle> editorControlStyles = new Dictionary<Selectable, EditorControlStyle>();
        private readonly List<Component> editorSkinComponents = new List<Component>();
        private readonly Dictionary<TextMeshProUGUI, string> editorNativeLabels = new Dictionary<TextMeshProUGUI, string>();
        private readonly Dictionary<Selectable, TextMeshProUGUI> editorPoolToolLabels = new Dictionary<Selectable, TextMeshProUGUI>();
        private SelectionButton editorPoolSearchCommand;
        private TextMeshProUGUI editorPoolSearchCount;
        private TextMeshProUGUI editorPoolSortCaption;
        private readonly List<EditorBehaviour> editorBehaviours = new List<EditorBehaviour>();
        private readonly List<GameObject> editorAdditions = new List<GameObject>();
        private int editorLayoutSignature = -1;
        private SelectionButton editorHeaderTutorial;
        private const string EditorInteractionHint = "左键查看 · 卡池右键添加 · 构筑右键移除 · 中键增加 · 拖动调整";
        private string EditorInteractionCaption => Compact ? "左键查看 · 右键添加 / 移除 · 中键增加 · 拖动排序" : EditorInteractionHint;

        internal bool EditorDirty => clientEditorUI != null && clientEditorUI.DeckView != null && clientEditorUI.DeckView.GetDirty();

        internal void OpenEditor(string personalRelativeName)
        {
            try
            {
                var root = Path.GetFullPath(Runtime.Program.PATH_DECK).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                var path = Path.GetFullPath(Path.Combine(root, personalRelativeName + Runtime.Program.EXPANSION_YDK));
                if (!path.StartsWith(root, StringComparison.Ordinal) || !File.Exists(path))
                    throw new FileNotFoundException("找不到这套个人卡组。");
                if (Runtime.Program.instance.currentServant == Runtime.Program.instance.deckEditor && EditorDirty)
                {
                    var box = ShowDialog("当前卡组尚未保存", 640, 192);
                    FightUI.Button(box, "取消", 28, 112, 160, 48, CloseDialog);
                    var discard = FightUI.Button(box, "放弃修改", 208, 112, 160, 48, () => { DiscardEditor(); OpenEditor(personalRelativeName); });
                    FightUI.StyleDestructiveButton(discard);
                    FightUI.Button(box, "保存并打开", 388, 112, 224, 48, () => SaveEditorThen(() => OpenEditor(personalRelativeName)), true);
                    return;
                }
                ReleaseEditor();
                MDPro3.Duel.YGOSharp.BanlistManager.currentBanList = MDPro3.Duel.YGOSharp.BanlistManager.GetByName(MDPro3.Duel.YGOSharp.BanlistManager.DefaultBanlistName);
                editorCardReadingPositions.Clear();
                editorRelativeName = personalRelativeName;
                editorLoadedPath = path;
                editorOverwriteAllowed = false;
                // An explicit file can be opened even if the saved default is empty.
                // Browsing/editing never selects a default.
                MDPro3.Duel.YGOSharp.BanlistManager.currentBanList = MDPro3.Duel.YGOSharp.BanlistManager.GetByName(MDPro3.Duel.YGOSharp.BanlistManager.DefaultBanlistName);
                Runtime.Program.instance.deckEditor.SwitchCondition(Controllers.DeckEditor.Condition.EditDeck, personalRelativeName);
                Controllers.DeckEditor.DeckName = editorRelativeName;
                Controllers.DeckEditor.Deck = new MDPro3.Duel.YGOSharp.Deck(path);
                Controllers.DeckEditor.Deck.type = Path.GetDirectoryName(personalRelativeName) ?? string.Empty;
                Controllers.DeckEditor.DeckIsFromLocal = true;
                Controllers.DeckEditor.Recommendation = null;
                Runtime.Program.instance.deckEditor.returnServant = Runtime.Program.instance.menu;
                Runtime.Program.instance.deckEditor.returnAction = () => Navigate("decks");
                ServantUI.DeckEditorUI existingUI = Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>();
                if (existingUI != null)
                {
                    existingUI.CardCollectionView.historyCards = Controllers.DeckEditor.historyCards;
                    existingUI.DeckView.PrintDeck(Controllers.DeckEditor.Deck, editorRelativeName, DeckView.Condition.Editable);
                }
                Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.deckEditor);
                editorAwaitingMount = true;
                Navigate("editor");
            }
            catch (Exception e) { ShowStatus("打开卡组失败：" + e.Message); }
        }

        internal void RenderEditor()
        {
            ReleaseEditor();
            FightUI.Clear(Body);
            var buttonWidth = Compact ? 108f : 136f;
            var saveWidth = Compact ? 128f : 168f;
            var actionGap = Compact ? 10f : Gap;
            var actionWidth = buttonWidth * 4 + saveWidth + actionGap * 4;
            editorTitleText = FightUI.Text(Body, "我的 / " + (editorRelativeName == null ? "卡组编辑器" : FightText.Keep(editorRelativeName)), 0, 4,
                Mathf.Max(120, BodyWidth - actionWidth - 16), 36, Compact ? 25 : 32, FightUI.Gold);
            editorTitleText.enableAutoSizing = true; editorTitleText.fontSizeMin = Compact ? 22 : 28; editorTitleText.fontSizeMax = Compact ? 25 : 32;
            FightUI.Hint(editorTitleText.gameObject, editorRelativeName == null ? "卡组编辑器" : FightText.Keep(editorRelativeName));
            editorStateText = FightUI.Text(Body, "正在加载构筑…", 0, 43, BodyWidth - actionWidth, 24, 16, FightUI.Muted);
            var bx = BodyWidth - actionWidth;
            editorToolbarButtons.Add(FightUI.Button(Body, "展开教程", bx, 8, buttonWidth, 44, OpenEditorTutorial, false, false)); bx += buttonWidth + actionGap;
            editorToolbarButtons.Add(FightUI.Button(Body, "禁限卡表", bx, 8, buttonWidth, 44, OpenEditorBanlist, false, false)); bx += buttonWidth + actionGap;
            editorToolbarButtons.Add(FightUI.Button(Body, "手牌测试", bx, 8, buttonWidth, 44, () => clientEditorUI?.OnHandTest(), false, false)); bx += buttonWidth + actionGap;
            editorToolbarButtons.Add(FightUI.Button(Body, "更多", bx, 8, buttonWidth, 44, OpenEditorMore, false, false)); bx += buttonWidth + actionGap;
            editorSaveButton = FightUI.Button(Body, "保存卡组", bx, 8, saveWidth, 44, () => SaveEditor(), true, false);
            FightUI.Hint(editorToolbarButtons[0].gameObject, "查看当前卡组主题的完整展开教程");
            FightUI.Hint(editorToolbarButtons[1].gameObject, "查看与选择禁限卡表");

            var top = Compact ? 76f : 88f;
            var viewHeight = Mathf.Max(240, BodyHeight - top - 40);
            var detailWidth = Compact ? 228f : 336f;
            var collectionWidth = Compact ? 392f : 592f;
            var deckWidth = BodyWidth - detailWidth - collectionWidth - Gap * 2;
            if (deckWidth < 400)
            {
                collectionWidth = Mathf.Max(300, BodyWidth * .32f);
                detailWidth = Mathf.Max(180, BodyWidth * .18f);
                deckWidth = BodyWidth - detailWidth - collectionWidth - Gap * 2;
            }
            FightUI.Line(Body, 0, top - 8, BodyWidth, FightUI.GoldBorder);
            editorDivider = Body.GetChild(Body.childCount - 1) as RectTransform;
            editorDetailHost = FightUI.Panel(Body, 0, top, detailWidth, viewHeight, FightUI.Raised);
            editorViewHost = FightUI.Panel(Body, detailWidth + Gap, top, deckWidth, viewHeight, FightUI.PanelColor);
            editorCollectionHost = FightUI.Panel(Body, detailWidth + Gap + deckWidth + Gap, top, collectionWidth, viewHeight, FightUI.PanelColor);
            editorDetailHost.gameObject.AddComponent<RectMask2D>();
            editorViewHost.gameObject.AddComponent<RectMask2D>();
            editorCollectionHost.gameObject.AddComponent<RectMask2D>();
            editorCountsText = FightUI.Text(Body, EditorInteractionCaption, 0,
                BodyHeight - 30, BodyWidth - 370, 28, 15, FightUI.Muted);
            editorCountsText.enableWordWrapping = false;
            FightUI.Hint(editorCountsText.gameObject, EditorInteractionHint);
            editorSummaryText = FightUI.Text(Body, "正在读取卡组…", BodyWidth - 270, BodyHeight - 30, 270, 28, 16, FightUI.Gold, TextAlignmentOptions.MidlineRight);
            editorSectionJumpButton = FightUI.TextAction(Body, "定位", BodyWidth - 356, BodyHeight - 32, 76, 30, OpenEditorSectionJump, false);
            editorSectionJumpButton.GetComponentInChildren<TextMeshProUGUI>().fontSize = 16;
            FightUI.FocusKey(editorSectionJumpButton, "editor:section-jump");
            FightUI.Hint(editorSectionJumpButton.gameObject, "定位主卡、额外或副卡区域，不改变构筑");
            editorAwaitingMount = true;
            LayoutEditorShell();
            TryMountEditor();
        }

        internal void ReflowEditorViewport()
        {
            if (Body == null || editorViewHost == null || editorCollectionHost == null || editorDetailHost == null) return;
            var inputs = Body.GetComponentsInChildren<TMP_InputField>(true).Where(input => input.isFocused)
                .Select(input => new EditorInputAnchor(input)).ToArray();
            RememberEditorScroll(clientEditorUI?.DeckView?.ScrollRect);
            RememberEditorScroll(clientEditorUI?.CardCollectionView?.superScrollView?.scrollRect);
            RememberEditorScroll(editorInspector?.Scroll);
            FightUI.Place(Body, Gap, BodyTop, BodyWidth, BodyHeight);
            LayoutEditorShell();
            ReflowEditorWidget(editorDeckWidget, true);
            ReflowEditorWidget(editorPoolWidget, false);
            LayoutEditorHeader();
            Canvas.ForceUpdateCanvases();
            editorLayoutSignature = -1;
            LayoutEditorDeckSections();
            foreach (EditorTab tab in editorTabs) tab.Reflow();
            LayoutEditorInspector();
            if (editorActionHost != null && clientEditorUI?.CardActionMenu != null)
            {
                FightUI.Place(editorActionHost, 0, 0, BodyWidth, BodyHeight);
                var action = clientEditorUI.CardActionMenu.GetComponent<RectTransform>();
                var scale = Mathf.Min(1, (BodyHeight - 16) / editorActionNativeHeight);
                FightUI.Place(action, 0, 0, BodyWidth / scale, BodyHeight / scale);
                action.localScale = new Vector3(scale, scale, 1);
                LayoutEditorTypography(action, scale);
            }
            Canvas.ForceUpdateCanvases();
            RestoreEditorScroll(clientEditorUI?.DeckView?.ScrollRect);
            RestoreEditorScroll(clientEditorUI?.CardCollectionView?.superScrollView?.scrollRect);
            RestoreEditorScroll(editorInspector?.Scroll);
            clientEditorUI?.CardCollectionView?.superScrollView?.SynchronizeViewport();
            foreach (EditorInputAnchor input in inputs) input.Restore();
        }

        private void LayoutEditorShell()
        {
            var buttonWidths = editorToolbarButtons.Select(button => Mathf.Max(Compact ? 108f : 136f,
                Mathf.Ceil(MeasureEditorCaption(button.GetComponentInChildren<TextMeshProUGUI>(), FightText.Get(button.name), 19) + 24))).ToArray();
            var saveWidth = Compact ? 128f : 168f;
            var actionGap = Compact ? 10f : Gap;
            var actionWidth = SumEditorWidths(buttonWidths) + saveWidth + actionGap * 4;
            FightUI.Place(editorTitleText.rectTransform, 0, 4, Mathf.Max(120, BodyWidth - actionWidth - 16), 36);
            editorTitleText.fontSizeMin = Compact ? 22 : 28; editorTitleText.fontSizeMax = Compact ? 25 : 32;
            editorTitleText.fontSize = editorTitleText.fontSizeMax;
            FightUI.Place(editorStateText.rectTransform, 0, 43, BodyWidth - actionWidth, 24);
            var bx = BodyWidth - actionWidth;
            for (int i = 0; i < editorToolbarButtons.Count; i++)
            {
                var button = editorToolbarButtons[i];
                FightUI.Place(button.GetComponent<RectTransform>(), bx, 8, buttonWidths[i], 44);
                FightUI.Place(button.GetComponentInChildren<TextMeshProUGUI>().rectTransform, 10, 0, buttonWidths[i] - 20, 44);
                bx += buttonWidths[i] + actionGap;
            }
            FightUI.Place(editorSaveButton.GetComponent<RectTransform>(), bx, 8, saveWidth, 44);
            var top = Compact ? 76f : 88f;
            var viewHeight = Mathf.Max(240, BodyHeight - top - 40);
            var detailWidth = Compact ? 228f : 336f;
            // The native pool owns six columns. Extra desktop space belongs to
            // the deck grid; keep pool controls and card art at authored scale.
            var collectionWidth = Compact ? 392f : 592f;
            var deckWidth = BodyWidth - detailWidth - collectionWidth - Gap * 2;
            if (deckWidth < 400)
            {
                collectionWidth = Mathf.Max(300, BodyWidth * .32f);
                detailWidth = Mathf.Max(180, BodyWidth * .18f);
                deckWidth = BodyWidth - detailWidth - collectionWidth - Gap * 2;
            }
            FightUI.Place(editorDivider, 0, top - 8, BodyWidth, 1);
            FightUI.Place(editorDetailHost, 0, top, detailWidth, viewHeight);
            FightUI.Place(editorViewHost, detailWidth + Gap, top, deckWidth, viewHeight);
            FightUI.Place(editorCollectionHost, detailWidth + Gap + deckWidth + Gap, top, collectionWidth, viewHeight);
            FightUI.Place(editorCountsText.rectTransform, 0, BodyHeight - 30, BodyWidth - 370, 28);
            editorCountsText.text = EditorInteractionCaption;
            FightUI.Place(editorSummaryText.rectTransform, BodyWidth - 270, BodyHeight - 30, 270, 28);
            if (editorSectionJumpButton != null) FightUI.Place(editorSectionJumpButton.GetComponent<RectTransform>(), BodyWidth - 356, BodyHeight - 32, 76, 30);
        }

        private void RememberEditorScroll(ScrollRect scroll)
        {
            if (scroll == null || scroll.content == null) return;
            if (!editorScrollAnchors.TryGetValue(scroll, out EditorScrollAnchor anchor))
                editorScrollAnchors[scroll] = anchor = new EditorScrollAnchor();
            anchor.Remember(scroll);
        }

        private void RestoreEditorScroll(ScrollRect scroll)
        {
            if (scroll != null && editorScrollAnchors.TryGetValue(scroll, out EditorScrollAnchor anchor)) anchor.Restore(scroll);
        }

        private void OpenEditorTutorial()
        {
            Services.DeckAcademyDeck entry = Services.DeckAcademy.FindCurrent(Services.DeckAcademy.Load());
            if (entry != null) OpenTutorial(entry);
            else Navigate("academy");
        }

        internal void TickEditor()
        {
            if (Runtime.Program.instance == null || Runtime.Program.instance.currentServant != Runtime.Program.instance.deckEditor)
                return;
            if (editorAwaitingMount) TryMountEditor();
            if (clientEditorUI == null || editorStateText == null) return;
            DeckView view = clientEditorUI.DeckView;
            RefreshEditorCardFocus();
            if (editorTitleText != null)
            {
                editorTitleText.text = "我的 / " + (editorRelativeName == null ? "卡组编辑器" : FightText.Keep(editorRelativeName));
                FightUI.Hint(editorTitleText.gameObject, editorRelativeName == null ? "卡组编辑器" : FightText.Keep(editorRelativeName));
            }
            if (editorSaveButton != null) editorSaveButton.interactable = view.deckLoaded;
            if (editorSectionJumpButton != null)
            {
                editorSectionJumpButton.interactable = view.deckLoaded;
                editorSectionJumpButton.GetComponentInChildren<TextMeshProUGUI>().color = view.deckLoaded ? FightUI.Gold : FightUI.Muted;
            }
            foreach (var toolbarButton in editorToolbarButtons)
                if (toolbarButton != null) toolbarButton.interactable = view.deckLoaded;
            if (editorHeaderTutorial != null && editorHeaderTutorial.GetButtonText() == "展开教程")
                editorHeaderTutorial.gameObject.SetActive(false);
            LayoutEditorDeckSections();
            foreach (EditorTab tab in editorTabs) tab.Refresh();
            foreach (var label in editorPoolToolLabels)
                if (label.Key != null && label.Value != null)
                {
                    bool enabled = label.Key.IsInteractable();
                    label.Value.color = enabled ? FightUI.Gold : FightUI.Muted;
                    label.Value.canvasRenderer.SetAlpha(enabled ? 1 : .48f);
                }
            if (editorPoolSearchCommand != null && editorPoolSearchCount != null)
            {
                int resultCount = clientEditorUI.CardCollectionView.superScrollView?.items.Count ?? 0;
                editorPoolSearchCount.text = resultCount + " 张";
                AttachEditorToolHint(editorPoolSearchCommand.GetSelectable().gameObject, "重新搜索当前卡池 · 当前 " + resultCount + " 张结果");
            }
            if (editorPoolSortCaption != null)
            {
                var order = CardCollectionView._SortOrder;
                editorPoolSortCaption.text = FightText.Get(order == CardCollectionView.SortOrder.ByRarityUp ? "稀有度 ↑"
                    : order == CardCollectionView.SortOrder.ByRarityDown ? "稀有度 ↓" : "排序");
            }
            editorStateText.text = !view.deckLoaded ? "正在加载构筑…" :
                (view.GetDirty() ? "◆ 未保存" : "已保存") + "    禁限表 " + Controllers.DeckEditor.Banlist.Name;
            editorStateText.color = view.GetDirty() ? FightUI.Gold : FightUI.Muted;
            FightUI.Hint(editorStateText.gameObject, editorStateText.text);
            if (editorSummaryText != null)
                editorSummaryText.text = view.deckLoaded ? "主 " + view.mainCount + "  ·  额外 " + view.extraCount + "  ·  副 " + view.sideCount : "正在读取卡组…";
            MDPro3.Duel.YGOSharp.Card inspected = clientEditorUI.CardDetailView != null ? clientEditorUI.CardDetailView.Card : null;
            if (inspected != null && inspected.Id != editorInspectedCode) RenderEditorInspector(inspected);
            RefreshEditorInspectorCopies();
        }

        private void TryMountEditor()
        {
            if (editorViewHost == null || Runtime.Program.instance.deckEditor.servantUI == null || Runtime.Program.instance.deckEditor.inTransition)
                return;
            ServantUI.DeckEditorUI ui = Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>();
            if (ui == null || ui.DeckView == null || ui.CardCollectionView == null) return;
            clientEditorUI = ui;
            // Resolve every element before detaching from the prefab's manager.
            CardDetailView detail = ui.CardDetailView;
            CardActionMenu action = ui.CardActionMenu;
            var drag = ui.DragCard;
            var dragImage = ui.DragCardImage;
            Canvas.ForceUpdateCanvases();
            editorDeckWidget = MountEditorWidget(ui.DeckView.GetComponent<RectTransform>(), editorViewHost, 772, ui.DeckView.ScrollRect.GetComponent<RectTransform>(), true);
            editorPoolWidget = MountEditorWidget(ui.CardCollectionView.GetComponent<RectTransform>(), editorCollectionHost, 552, ui.CardCollectionView.superScrollView.scrollRect.GetComponent<RectTransform>(), false);
            MountEditorHeader(ui.DeckView);
            MountEditorDeckSections(ui.DeckView);
            MountEditorTabs(ui.CardCollectionView);
            LabelEditorPoolTools(ui.CardCollectionView);
            BindEditorNativeLabel(ui.CardCollectionView.InputSearch.InputField.placeholder as TextMeshProUGUI, "搜索卡名或编号");
            BindEditorNativeLabel(ui.CardCollectionView.GetComponent<Presentation.Elements.ElementObjectManager>()
                ?.GetNestedElement<TextMeshProUGUI>("CardListArea/NoItemText"), "没有匹配的卡片");
            editorImageCache = new FightCardPoolCache();
            ui.CardCollectionView.ClientImageCache = editorImageCache;
            foreach (CardRawImageHandler image in ui.CardCollectionView.GetComponentsInChildren<CardRawImageHandler>(true))
                if (image.GetComponentInParent<SelectionButton_CardInCollection>() != null) image.SetClientPool(editorImageCache);
            if (detail != null)
            {
                // CardDetailView remains the real selection/data controller. The new
                // inspector renders that state with readable desktop typography.
                if (Controllers.DeckEditor.Deck.Main.Count > 0)
                {
                    MDPro3.Duel.YGOSharp.Card first = MDPro3.Duel.YGOSharp.CardsManager.Get(Controllers.DeckEditor.Deck.Main[0]);
                    ui.ShowDetail(first); RenderEditorInspector(first);
                }
                else RenderEditorInspector(null);
            }
            else FightUI.Text(editorDetailHost, "卡片详情暂不可用", 16, 20, editorDetailHost.rect.width - 32, 64, 18, FightUI.Muted);

            if (action != null)
            {
                EditorMount state = new EditorMount(action.GetComponent<RectTransform>()); editorMounts.Add(state);
                editorActionNativeHeight = state.Rect.rect.height > 64 ? state.Rect.rect.height : 810;
                var scale = Mathf.Min(1, (BodyHeight - 16) / editorActionNativeHeight);
                editorActionHost = FightUI.Node(Body, "Card Actions", 0, 0, BodyWidth, BodyHeight);
                editorActionHost.gameObject.AddComponent<RectMask2D>();
                state.Rect.SetParent(editorActionHost, false);
                FightUI.Place(state.Rect, 0, 0, BodyWidth / scale, BodyHeight / scale);
                state.Rect.localScale = new Vector3(scale, scale, 1);
                SkinEditorWidget(state.Rect, scale);
            }
            if (drag != null)
            {
                EditorMount state = new EditorMount(drag); editorMounts.Add(state);
                drag.SetParent(Body, false); drag.anchorMin = drag.anchorMax = new Vector2(.5f, .5f); drag.pivot = new Vector2(.5f, .5f);
                drag.anchoredPosition = Vector2.zero; drag.localScale = Vector3.one; drag.gameObject.SetActive(false);
            }
            ui.DeckView.KeepCurrentDeckOnSave = true;
            ui.ClientSave = () => SaveEditor();
            ui.ClientOverlayParent = Body;
            editorAwaitingMount = false;
        }

        private void MountEditorHeader(DeckView view)
        {
            SelectionButton tutorialButton = view.ButtonDeck;
            if (tutorialButton != null && tutorialButton.GetButtonText() == "展开教程")
            {
                editorMounts.Add(new EditorMount(tutorialButton.GetComponent<RectTransform>()));
                tutorialButton.gameObject.SetActive(false); editorHeaderTutorial = tutorialButton;
            }
            var input = view.GetInputField()?.GetComponent<RectTransform>();
            RectTransform header = null;
            for (var parent = input != null ? input.parent as RectTransform : null; parent != null; parent = parent.parent as RectTransform)
                if (parent.name == "HeaderArea") { header = parent; break; }
            if (header == null || input == null) return;
            var group = header.Find("NameAreaGroup") as RectTransform;
            if (group == null || !input.IsChildOf(group)) group = input.parent != header ? input.parent as RectTransform : null;
            if (group == input) group = null;
            var bounds = EditorBounds(group != null ? group : header, input);
            var icon = header.Find("IconDeck") as RectTransform;
            bounds.x = (icon != null ? EditorBounds(header, icon).xMax : 56) + 16;
            if (group != null)
            {
                editorMounts.Add(new EditorMount(group));
                DisableEditorLayoutDrivers(group);
            }
            editorMounts.Add(new EditorMount(input));
            editorHeaderInput = input; editorHeaderArea = header; editorHeaderNameGroup = group; editorHeaderInputBounds = bounds;
            LayoutEditorHeader();
        }

        private void LayoutEditorHeader()
        {
            if (editorHeaderInput == null || editorHeaderArea == null) return;
            var bounds = editorHeaderInputBounds;
            var width = Mathf.Max(120, editorHeaderArea.rect.width - bounds.x - 16);
            if (editorHeaderNameGroup != null)
            {
                var groupBounds = EditorBounds(editorHeaderArea, editorHeaderNameGroup);
                FightUI.Place(editorHeaderNameGroup, bounds.x, groupBounds.y, width, groupBounds.height);
                FightUI.Place(editorHeaderInput, 0, bounds.y, width, bounds.height);
            }
            else FightUI.Place(editorHeaderInput, bounds.x, bounds.y, width, bounds.height);
        }

        private EditorWidget MountEditorWidget(RectTransform rect, RectTransform host, float authoredWidth, RectTransform columnAnchor, bool expand)
        {
            EditorMount state = new EditorMount(rect); editorMounts.Add(state);
            // The prefab component is a full-screen wrapper. Its actual deck/pool
            // column is fixed-width and aligned within that wrapper. Scale the
            // column, then crop/translate the unchanged authored wrapper into it.
            // Resizing the wrapper to the column would break stretch margins.
            var frame = FindEditorColumn(rect, authoredWidth, columnAnchor);
            var before = EditorBounds(rect, frame);
            var nativeWidth = before.width;
            var oldHeight = rect.rect.height;
            var oldWidth = rect.rect.width;
            var extent = EditorColumnExtent(rect, before, nativeWidth);
            var topInset = Mathf.Max(0, extent.y);
            var bottomInset = Mathf.Max(0, oldHeight - extent.yMax);
            EditorWidget widget = new EditorWidget(rect, host, frame, oldWidth, nativeWidth, topInset, bottomInset,
                Mathf.Max(0, before.y - extent.y), Mathf.Max(0, extent.yMax - before.yMax));
            if (frame != rect) editorMounts.Add(new EditorMount(frame));
            var scale = Mathf.Min(1, (host.rect.width - 16) / nativeWidth);
            rect.SetParent(host, false);
            ReflowEditorWidget(widget, expand);
            SkinEditorWidget(rect, scale);
            return widget;
        }

        private void ReflowEditorWidget(EditorWidget widget, bool expand)
        {
            if (widget == null || widget.Rect == null || widget.Frame == null || widget.Host == null) return;
            var rect = widget.Rect; var frame = widget.Frame; var host = widget.Host;
            var scale = Mathf.Clamp((host.rect.width - 16) / widget.NativeWidth, .001f, 1);
            var columnWidth = expand ? (host.rect.width - 16) / scale : widget.NativeWidth;
            var desiredHeight = (host.rect.height - 16) / scale;
            FightUI.Place(rect, 0, 0, widget.WrapperWidth,
                desiredHeight + widget.TopInset + widget.BottomInset);
            rect.localScale = new Vector3(scale, scale, 1);
            if (frame != rect)
            {
                frame.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, columnWidth);
                frame.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                    Mathf.Max(160, desiredHeight - widget.TopBleed - widget.BottomBleed));
            }
            Canvas.ForceUpdateCanvases();
            var after = EditorBounds(rect, frame);
            rect.anchoredPosition = new Vector2(8 - after.x * scale, -8 + (after.y - widget.TopBleed) * scale);
            LayoutEditorTypography(rect, scale);
        }

        private static RectTransform FindEditorColumn(RectTransform wrapper, float authoredWidth, RectTransform preferred)
        {
            var selected = preferred;
            if (selected == null || EditorBounds(wrapper, selected).width < authoredWidth * .6f
                || EditorBounds(wrapper, selected).width > authoredWidth * 1.6f)
            {
                selected = wrapper.GetComponentsInChildren<RectTransform>(true)
                    .Where(r => r != wrapper && r.gameObject.activeInHierarchy && EditorBounds(wrapper, r).width >= authoredWidth * .8f
                        && EditorBounds(wrapper, r).width <= authoredWidth * 1.3f)
                    .OrderByDescending(r => r.rect.height).FirstOrDefault();
            }
            if (selected == null) return wrapper;
            for (var parent = selected.parent as RectTransform; parent != null && parent != wrapper; parent = parent.parent as RectTransform)
            {
                var width = EditorBounds(wrapper, parent).width;
                if (width >= EditorBounds(wrapper, selected).width && width <= authoredWidth * 1.3f) selected = parent;
            }
            return selected;
        }

        private static Rect EditorBounds(RectTransform wrapper, RectTransform item)
        {
            var corners = new Vector3[4]; item.GetWorldCorners(corners);
            var left = wrapper.InverseTransformPoint(corners[0]);
            var right = wrapper.InverseTransformPoint(corners[2]);
            return new Rect(left.x + wrapper.rect.width * wrapper.pivot.x,
                wrapper.rect.height * (1 - wrapper.pivot.y) - right.y, right.x - left.x, right.y - left.y);
        }

        private static Rect EditorColumnExtent(RectTransform wrapper, Rect frame, float nativeWidth)
        {
            var top = frame.y; var bottom = frame.yMax;
            foreach (var r in wrapper.GetComponentsInChildren<RectTransform>(true))
            {
                if (r == wrapper || !r.gameObject.activeInHierarchy) continue;
                var scroll = r.GetComponentInParent<ScrollRect>();
                if (scroll != null && scroll.transform.IsChildOf(wrapper)
                    && r != scroll.GetComponent<RectTransform>() && r != scroll.viewport) continue;
                var bounds = EditorBounds(wrapper, r);
                if (bounds.width < nativeWidth * .8f || bounds.width > nativeWidth * 1.3f || bounds.height < 8) continue;
                if (bounds.xMax < frame.x || bounds.x > frame.xMax) continue;
                top = Mathf.Min(top, bounds.y); bottom = Mathf.Max(bottom, bounds.yMax);
            }
            return new Rect(frame.x, top, nativeWidth, Mathf.Max(1, bottom - top));
        }

        private void MountEditorDeckSections(DeckView view)
        {
            var scroll = view.ScrollRect;
            var frame = scroll.GetComponent<RectTransform>();
            editorDeckScroll = new EditorDeckScroll(scroll);
            var viewport = FightUI.Node(frame, "Fight Deck Viewport", 8, 4, frame.rect.width - 24, frame.rect.height - 8);
            viewport.gameObject.AddComponent<RectMask2D>();
            // Empty space and group headings must also receive wheel/drag input.
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            var loading = viewport.gameObject.AddComponent<CanvasGroup>();
            loading.alpha = view.deckLoaded ? 1 : 0; loading.blocksRaycasts = view.deckLoaded;
            var content = FightUI.Node(viewport, "Fight Deck Content", 0, 0, viewport.rect.width, viewport.rect.height);
            editorAdditions.Add(viewport.gameObject);
            editorDeckScroll.Content = content; editorDeckScroll.Viewport = viewport; editorDeckScroll.Loading = loading;
            scroll.StopMovement(); scroll.content = content; scroll.viewport = viewport;
            scroll.enabled = true;
            scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 48;
            if (scroll.verticalScrollbar != null)
            {
                editorMounts.Add(new EditorMount(scroll.verticalScrollbar.GetComponent<RectTransform>()));
                scroll.verticalScrollbar.gameObject.SetActive(false);
            }
            var rail = FightUI.Panel(frame, frame.rect.width - 10, 4, 5, frame.rect.height - 8, FightUI.Border, false, "Fight Deck Scrollbar");
            editorDeckScroll.Rail = rail;
            var thumb = FightUI.Panel(rail, 0, 0, 5, rail.rect.height, FightUI.GoldBorder, false, "Thumb");
            FightUI.Stretch(thumb);
            var bar = rail.gameObject.AddComponent<Scrollbar>();
            bar.targetGraphic = thumb.GetComponent<Image>(); bar.handleRect = thumb; bar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            editorAdditions.Add(rail.gameObject);

            var locations = new[] { DeckView.DeckLocation.MainDeck, DeckView.DeckLocation.ExtraDeck, DeckView.DeckLocation.SideDeck };
            var labels = new[] { "主卡组", "额外卡组", "副卡组" };
            for (var i = 0; i < locations.Length; i++)
            {
                var gridRect = view.GetDeckLocationParent(locations[i]);
                var grid = gridRect.GetComponent<GridLayoutGroup>();
                var root = gridRect.GetComponentInParent<UIHover>().GetComponent<RectTransform>();
                editorMounts.Add(new EditorMount(root)); editorMounts.Add(new EditorMount(gridRect));
                DisableEditorLayoutDrivers(root); DisableEditorLayoutDrivers(gridRect, grid);
                // Keep the actual hover/drop region and card grid. The original
                // bottom/percentage-anchored headings are replaced inside it.
                foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
                {
                    if (graphic.transform == root || graphic.transform.IsChildOf(gridRect)) continue;
                    editorGraphics.Add(new EditorGraphic(graphic)); graphic.enabled = false;
                }
                root.SetParent(content, false);
                var header = FightUI.Node(root, "Fight Section Header", 4, 0, content.rect.width - 8, 34);
                var title = FightUI.Text(header, labels[i], 0, 0, 180, 32, 24, FightUI.Gold, TextAlignmentOptions.MidlineLeft);
                var facts = FightUI.Text(header, "", 180, 0, header.rect.width - 180, 32, Mathf.Max(20, 16 / view.transform.localScale.x), FightUI.Muted, TextAlignmentOptions.MidlineRight);
                title.raycastTarget = facts.raycastTarget = false;
                string emptyLabel = locations[i] == DeckView.DeckLocation.MainDeck ? "主卡暂空 · 从卡池添加或拖入卡片"
                    : locations[i] == DeckView.DeckLocation.ExtraDeck ? "额外卡组暂空 · 将额外怪兽拖入这里" : "副卡暂空 · 将卡片拖入这里";
                var emptyHint = FightUI.Text(root, emptyLabel, 12, 58, content.rect.width - 24, 40, 24, FightUI.Muted, TextAlignmentOptions.MidlineLeft);
                emptyHint.raycastTarget = false; editorAdditions.Add(emptyHint.gameObject);
                editorAdditions.Add(header.gameObject);
                editorDeckSections.Add(new EditorDeckSection(root, grid, locations[i], labels[i], title, facts, emptyHint));
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                grid.childAlignment = TextAnchor.UpperLeft; grid.padding = new RectOffset(); grid.spacing = new Vector2(4, 4);
            }
            view.ClientDeckLayout = true;
            editorLayoutSignature = -1;
            LayoutEditorDeckSections();
            scroll.verticalNormalizedPosition = 1;
        }

        private void DisableEditorLayoutDrivers(RectTransform rect, Behaviour retained = null)
        {
            foreach (var driver in rect.GetComponents<Behaviour>())
            {
                if (driver == retained || (!(driver is LayoutGroup) && !(driver is ContentSizeFitter))) continue;
                editorBehaviours.Add(new EditorBehaviour(driver)); driver.enabled = false;
            }
        }

        private void LayoutEditorDeckSections()
        {
            if (editorDeckScroll == null || clientEditorUI == null || editorDeckScroll.Content == null
                || editorDeckScroll.Viewport == null || editorDeckScroll.Loading == null) return;
            DeckView view = clientEditorUI.DeckView; var scroll = view.ScrollRect;
            var frame = scroll.GetComponent<RectTransform>();
            var viewport = editorDeckScroll.Viewport; var content = editorDeckScroll.Content;
            editorDeckScroll.Loading.alpha = view.deckLoaded ? 1 : 0;
            editorDeckScroll.Loading.blocksRaycasts = view.deckLoaded;
            var width = frame.rect.width - 24; var height = frame.rect.height - 8;
            var signature = Mathf.RoundToInt(width) * 31 + Mathf.RoundToInt(height) + (view.deckLoaded ? 1 : 0);
            foreach (EditorDeckSection section in editorDeckSections)
            {
                section.ResizeCells(LargeViewport);
                signature = signature * 67 + view.GetDeckLocationCount(section.Location);
                section.Refresh(view);
            }
            if (view.cards != null) foreach (SelectionButton_CardInDeck card in view.cards)
            {
                if (card == null || card.transform.childCount == 0) continue;
                EditorDeckSection section = editorDeckSections.FirstOrDefault(value => value.Location == card.location);
                if (section == null) continue;
                if (!editorCardGeometry.TryGetValue(card, out EditorCardGeometry geometry))
                    editorCardGeometry[card] = geometry = new EditorCardGeometry(card.transform.GetChild(0) as RectTransform);
                geometry.Apply(LargeViewport ? section.Grid.cellSize.x / section.NativeCell.x : 1);
            }
            if (signature == editorLayoutSignature) return;
            editorLayoutSignature = signature;
            FightUI.Place(viewport, 8, 4, width, height);
            if (editorDeckScroll.Rail != null)
                FightUI.Place(editorDeckScroll.Rail, frame.rect.width - 10, 4, 5, height);
            var y = 0f;
            foreach (EditorDeckSection section in editorDeckSections)
            {
                var grid = section.Grid; var count = view.GetDeckLocationCount(section.Location);
                var gridWidth = width - 8;
                var columns = Mathf.Max(1, Mathf.FloorToInt((gridWidth + grid.spacing.x) / (grid.cellSize.x + grid.spacing.x)));
                var rows = Mathf.Max(1, Mathf.CeilToInt((float)count / columns));
                var gridHeight = rows * grid.cellSize.y + (rows - 1) * grid.spacing.y;
                var sectionHeight = 38 + gridHeight + 12;
                grid.constraintCount = columns;
                FightUI.Place(section.Root, 0, y, width, sectionHeight);
                FightUI.Place(grid.GetComponent<RectTransform>(), 4, 38, gridWidth, gridHeight);
                section.Reflow(width, view.transform.localScale.x);
                section.Root.localScale = Vector3.one;
                y += sectionHeight + 8;
            }
            content.sizeDelta = new Vector2(width, Mathf.Max(height, y - 8));
            content.anchoredPosition = new Vector2(0, Mathf.Clamp(content.anchoredPosition.y, 0, Mathf.Max(0, content.rect.height - height)));
        }

        private void MountEditorTabs(CardCollectionView collection)
        {
            var scale = collection.transform.localScale.x;
            var tabArea = collection.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(rect => rect.name == "TabArea");
            if (tabArea == null) return;
            foreach (var graphic in tabArea.GetComponentsInChildren<Graphic>(true))
            {
                editorGraphics.Add(new EditorGraphic(graphic)); graphic.enabled = false;
            }
            var toggles = tabArea.GetComponentsInChildren<SelectionToggle_CardCollectionTab>(true);
            var host = FightUI.Node(tabArea, "Fight card pool tabs", 0, 0, tabArea.rect.width, tabArea.rect.height);
            host.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            FightUI.Stretch(host); editorAdditions.Add(host.gameObject);
            var areas = new[] { CardCollectionView.Area.Collection, CardCollectionView.Area.Bookmark, CardCollectionView.Area.History };
            var labels = new[] { "卡池", "收藏", "历史" };
            float width = tabArea.rect.width / 3;
            for (int i = 0; i < areas.Length; i++)
            {
                CardCollectionView.Area area = areas[i]; SelectionToggle_CardCollectionTab native = toggles.FirstOrDefault(toggle => toggle.ClientArea == area);
                var panel = FightUI.Panel(host, i * width, 0, width, tabArea.rect.height, FightUI.PanelColor, true, "Fight Tab " + labels[i]);
                panel.GetComponent<Outline>().effectColor = FightUI.Border;
                var button = panel.gameObject.AddComponent<Button>(); button.targetGraphic = panel.GetComponent<Image>();
                FightFrame frame = panel.GetComponentInChildren<FightFrame>(); frame.ButtonFrame = true;
                panel.gameObject.AddComponent<FightButtonFeedback>().Bind(frame);
                button.onClick.AddListener(() => { if (native != null) native.SetToggleOn(); else collection.ShowArea(area); });
                var text = FightUI.Text(panel, labels[i], 0, 0, width, tabArea.rect.height, Mathf.Max(22, 17 / scale), FightUI.Muted, TextAlignmentOptions.Center);
                FightUI.Stretch(text.rectTransform); text.raycastTarget = false;
                var selectedLine = FightUI.Panel(panel, 12, tabArea.rect.height - 2, width - 24, 1, FightUI.Gold, false, "Selected tab line");
                selectedLine.GetComponent<Image>().raycastTarget = false;
                var selectedMark = FightUI.Icon(panel, "diamond", width / 2 - 4, tabArea.rect.height - 7, 8, 8, FightUI.Active).rectTransform;
                EditorTab tab = new EditorTab(collection, area, panel.GetComponent<Image>(), panel.GetComponent<Outline>(), text, selectedLine, selectedMark);
                editorTabs.Add(tab); tab.Refresh();
            }
        }

        private void RenderEditorInspector(MDPro3.Duel.YGOSharp.Card card)
        {
            if (editorDetailHost == null) return;
            if (editorInspector?.Scroll != null)
            {
                editorCardReadingPositions[editorInspector.Card.Id] = Mathf.Clamp01(editorInspector.Scroll.verticalNormalizedPosition);
                editorScrollAnchors.Remove(editorInspector.Scroll);
            }
            FightUI.Clear(editorDetailHost);
            editorInspectedCode = card != null ? card.Id : 0;
            var width = editorDetailHost.rect.width; var height = editorDetailHost.rect.height;
            editorInspector = new EditorInspector { Card = card };
            if (card == null)
            {
                editorInspector.EmptyIcon = FightUI.Icon(editorDetailHost, "deck", (width - 48) / 2, 48, 48, 48, FightUI.GoldBorder).GetComponent<RectTransform>();
                editorInspector.Heading = FightUI.Text(editorDetailHost, "尚未选中卡片", 16, 120, width - 32, 36, 20, FightUI.Foreground, TextAlignmentOptions.Center);
                LayoutEditorInspector();
                return;
            }
            var inspector = FightUI.Scroll(editorDetailHost, 12, 0, width - 24, 120, 120, out var scroll);
            scroll.name = "Card effect scroll";
            editorInspector.Content = inspector; editorInspector.Scroll = scroll;
            var heading = FightUI.RawText(editorDetailHost, card.Name, 12, 10, width - 24, 100, Compact ? 20 : 25, FightUI.Foreground);
            heading.enableWordWrapping = true; heading.enableAutoSizing = true;
            FightUI.RawHint(heading.gameObject, card.Name); editorInspector.Heading = heading;
            editorInspector.ArtDivider = FightUI.Panel(editorDetailHost, 12, 0, width - 24, 1, FightUI.Border, false, "Divider");
            var typeLabel = card.HasType(MDPro3.Duel.YGOSharp.CardType.Monster) ? card.GetAttributeString() + "  " + card.GetTypeForUI() : card.GetTypeForUI();
            editorInspector.Type = FightUI.Text(editorDetailHost, typeLabel, 12, 0, width - 24, 40, 16, FightUI.Muted);
            editorInspector.Type.enableWordWrapping = true;
            editorInspector.Parameters = FightUI.Text(editorDetailHost, EditorCardParameters(card), 12, 0, width - 24, 40, 16);
            var art = FightUI.Art(editorDetailHost, card.Id, 0, 0, 138, 138 * 1.46f, true);
            FightUI.CardButton(art, () => Runtime.UIManager.ShowCardInfoDetail(card));
            editorInspector.Art = art.transform.parent as RectTransform;
            foreach (RectTransform child in editorInspector.Art) FightUI.Stretch(child);
            FightUI.PrintRarityBadge(art.transform, card);
            editorInspector.EffectHeading = FightUI.Text(editorDetailHost, "完整卡文 ↗", 12, 0, width - 24, 30, 16, FightUI.Gold);
            editorInspector.EffectHeading.raycastTarget = true;
            var read = editorInspector.EffectHeading.gameObject.AddComponent<Button>(); read.targetGraphic = editorInspector.EffectHeading;
            FightUI.FocusKey(read, "editor:read-card");
            read.onClick.AddListener(() => ShowCard(card.Id));
            FightUI.Hint(read.gameObject, "放大阅读当前卡片；返回后保留构筑与卡池的状态");
            editorInspector.Copies = FightUI.Text(editorDetailHost, "", width - 102, 0, 90, 30, 14, FightUI.Muted, TextAlignmentOptions.MidlineRight);
            editorInspector.Copies.raycastTarget = false;
            editorInspector.EffectDivider = FightUI.Panel(editorDetailHost, 12, 0, width - 24, 1, FightUI.Border, false, "Effect divider");
            editorInspector.Description = FightUI.RawText(inspector, card.Desc, 0, 0, width - 44, 500, Compact ? 17 : 19, FightUI.Foreground);
            editorInspector.Description.lineSpacing = 7;
            editorInspector.Description.paragraphSpacing = 10;
            editorInspector.Description.overflowMode = TextOverflowModes.Overflow;
            editorInspector.ControlsDivider = FightUI.Panel(editorDetailHost, 12, 0, width - 24, 1, FightUI.GoldBorder, false, "Divider");
            editorInspector.Add = FightUI.Button(editorDetailHost, "+", 12, 0, 44, 40, () => clientEditorUI?.AddCard(card));
            editorInspector.Remove = FightUI.Button(editorDetailHost, "−", 64, 0, 44, 40, () => clientEditorUI?.RemoveCard(card));
            FightUI.Hint(editorInspector.Add.gameObject, "加入构筑");
            FightUI.Hint(editorInspector.Remove.gameObject, "从构筑移除");
            editorInspector.More = FightUI.Button(editorDetailHost, "更多", 116, 0, width - 128, 40, () => OpenEditorCardMore(card));
            editorInspector.Add.GetComponentInChildren<TextMeshProUGUI>().text = "加入";
            editorInspector.Remove.GetComponentInChildren<TextMeshProUGUI>().text = "移除";
            RefreshEditorInspectorCopies();
            LayoutEditorInspector();
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = editorCardReadingPositions.TryGetValue(card.Id, out var readingPosition) ? readingPosition : 1;
            scroll.onValueChanged.AddListener(_ => editorCardReadingPositions[card.Id] = Mathf.Clamp01(scroll.verticalNormalizedPosition));
        }

        private void RefreshEditorInspectorCopies()
        {
            EditorInspector item = editorInspector; DeckView view = clientEditorUI?.DeckView;
            if (item?.Card == null || item.Copies == null || view == null || !view.deckLoaded) return;
            int copies = view.GetCardCount(item.Card.Id);
            if (copies == item.CopyCount) return;
            item.CopyCount = copies;
            item.Copies.text = copies > 0 ? "同名 ×" + copies : "未加入构筑";
            item.Copies.color = copies > 0 ? FightUI.Gold : FightUI.Muted;
            item.Remove.interactable = copies > 0;
            FightUI.Hint(item.Remove.gameObject, copies > 0 ? "从构筑中移除一张当前卡片" : "构筑中暂无这张卡片");
        }

        private SelectionButton_CardInDeck FindEditorDeckCard(MDPro3.Duel.YGOSharp.Card card)
        {
            var cards = clientEditorUI?.DeckView?.cards;
            if (card == null || cards == null) return null;
            SelectionButton_CardInDeck exact = cards.FirstOrDefault(item => item?.Card != null && item.Card.Id == card.Id);
            if (exact != null) return exact;
            int nameCode = card.Alias == 0 ? card.Id : card.Alias;
            return cards.FirstOrDefault(item => item?.Card != null && (item.Card.Id == nameCode || item.Card.Alias == nameCode));
        }

        private void RefreshEditorCardFocus()
        {
            var selected = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
            SelectionButton_CardInDeck slot = modal == null && selected != null && selected.activeInHierarchy
                ? selected.GetComponentInParent<SelectionButton_CardInDeck>() : null;
            var art = slot != null && slot.deckView == clientEditorUI.DeckView
                ? slot.GetComponentInChildren<CardRawImageHandler>()?.RawImage : null;
            if (art == null)
            {
                if (editorCardFocusFrame != null) editorCardFocusFrame.gameObject.SetActive(false);
                return;
            }
            if (editorCardFocusFrame == null)
            {
                var overlay = FightUI.Node(art.transform, "Focused construction card", 0, 0, 0, 0);
                FightUI.Stretch(overlay);
                editorCardFocusFrame = overlay.gameObject.AddComponent<FightCardSlotFocus>();
                overlay.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                editorAdditions.Add(overlay.gameObject);
            }
            else if (editorCardFocusFrame.transform.parent != art.transform)
            {
                editorCardFocusFrame.transform.SetParent(art.transform, false);
                FightUI.Stretch(editorCardFocusFrame.rectTransform);
            }
            editorCardFocusFrame.gameObject.SetActive(true);
            editorCardFocusFrame.transform.SetAsLastSibling();
        }

        private void LocateEditorDeckCard(MDPro3.Duel.YGOSharp.Card card)
        {
            SelectionButton_CardInDeck target = FindEditorDeckCard(card);
            if (target == null) { ShowStatus("当前构筑中没有这张卡片。"); return; }
            CloseDialog();
            Canvas.ForceUpdateCanvases();
            var control = target.GetSelectable();
            control.Select(); RevealClientControl(control);
            string location = target.location == DeckView.DeckLocation.MainDeck ? "主卡组"
                : target.location == DeckView.DeckLocation.ExtraDeck ? "额外卡组" : "副卡组";
            ShowStatus("已定位到" + location + "：" + FightText.Keep(target.Card.Name));
        }

        private void LayoutEditorInspector()
        {
            if (editorInspector == null || editorDetailHost == null) return;
            EditorInspector item = editorInspector;
            var width = editorDetailHost.rect.width; var height = editorDetailHost.rect.height;
            if (item.Card == null)
            {
                float emptyTop = Mathf.Max(32, (height - 112) / 2);
                FightUI.Place(item.EmptyIcon, (width - 48) / 2, emptyTop, 48, 48);
                FightUI.Place(item.Heading.rectTransform, 16, emptyTop + 72, width - 32, 40);
                return;
            }
            item.Heading.fontSizeMin = Compact ? 17 : 21; item.Heading.fontSizeMax = Compact ? 20 : 25;
            item.Heading.fontSize = item.Heading.fontSizeMax;
            var headingHeight = Mathf.Clamp(item.Heading.GetPreferredValues(item.Heading.text, width - 24, float.PositiveInfinity).y + 6, 38, Compact ? 92 : 108);
            FightUI.Place(item.Heading.rectTransform, 12, 10, width - 24, headingHeight);
            var artTop = 10 + headingHeight + 24;
            FightUI.Place(item.ArtDivider, 12, artTop - 12, width - 24, 1);
            var typeHeight = item.Type.GetPreferredValues(item.Type.text, width - 24, float.PositiveInfinity).y + 6;
            var parametersHeight = item.Parameters.GetPreferredValues(item.Parameters.text, width - 24, float.PositiveInfinity).y + 6;
            // Reserve a useful effect viewport before assigning space to the art.
            var effectBudget = Mathf.Clamp(height * .27f, 110, 250);
            var artHeightBudget = Mathf.Max(64, height - artTop - 12 - typeHeight - parametersHeight - 112 - effectBudget);
            var artWidth = Mathf.Min(Mathf.Min(Compact ? 138 : 220, width - 32), artHeightBudget / 1.46f);
            var artHeight = artWidth * 1.46f;
            FightUI.Place(item.Art, (width - artWidth) / 2, artTop, artWidth, artHeight);
            var y = artTop + artHeight + 12;
            FightUI.Place(item.Type.rectTransform, 12, y, width - 24, typeHeight); y += typeHeight;
            FightUI.Place(item.Parameters.rectTransform, 12, y, width - 24, parametersHeight); y += parametersHeight;
            y += 8;
            FightUI.Place(item.EffectDivider, 12, y, width - 24, 1); y += 10;
            FightUI.Place(item.EffectHeading.rectTransform, 12, y, Mathf.Max(64, width - 126), 26);
            FightUI.Place(item.Copies.rectTransform, width - 102, y, 90, 26); y += 34;
            var scrollHeight = Mathf.Max(48, height - 72 - y);
            var scrollWidth = width - 24;
            FightUI.Place(item.Scroll.GetComponent<RectTransform>(), 12, y, scrollWidth, scrollHeight);
            FightUI.Place(item.Scroll.viewport, 0, 0, scrollWidth, scrollHeight);
            var rail = item.Scroll.verticalScrollbar.GetComponent<RectTransform>();
            FightUI.Place(rail, scrollWidth - 15, 2, 14, scrollHeight - 4);
            var track = rail.Find("Track") as RectTransform;
            if (track != null) FightUI.Place(track, 10, 0, 4, scrollHeight - 4);
            item.Description.fontSize = Compact ? 17 : 19;
            var preferred = item.Description.GetPreferredValues(item.Description.text, scrollWidth - 20, float.PositiveInfinity).y + 12;
            FightUI.Place(item.Description.rectTransform, 0, 2, scrollWidth - 20, preferred);
            item.Content.sizeDelta = new Vector2(scrollWidth, Mathf.Max(scrollHeight, preferred + 4));
            item.Content.anchoredPosition = new Vector2(0, Mathf.Clamp(item.Content.anchoredPosition.y, 0, Mathf.Max(0, item.Content.rect.height - scrollHeight)));
            var controlsY = height - 50;
            FightUI.Place(item.ControlsDivider, 12, controlsY - 8, width - 24, 1);
            var actions = new[] { item.Add, item.Remove, item.More };
            var captions = actions.Select(action => action.GetComponentInChildren<TextMeshProUGUI>()).ToArray();
            var labels = new[] { FightText.Get("加入"), FightText.Get("移除"), FightText.Get("更多") };
            float available = width - 40, fontSize = Compact ? 16 : 18;
            Func<float[]> measure = () => captions.Select((caption, index) => MeasureEditorCaption(caption, labels[index], fontSize) + 16).ToArray();
            var actionWidths = measure();
            while (fontSize > 12 && SumEditorWidths(actionWidths) > available)
            { fontSize -= .5f; actionWidths = measure(); }
            float extra = Mathf.Max(0, available - SumEditorWidths(actionWidths)) / 3, x = 12;
            for (int i = 0; i < actions.Length; i++)
            {
                float actionWidth = actionWidths[i] + extra;
                FightUI.Place(actions[i].GetComponent<RectTransform>(), x, controlsY, actionWidth, 40);
                var caption = captions[i];
                caption.enableWordWrapping = false; caption.enableAutoSizing = true;
                caption.fontSizeMax = fontSize; caption.fontSizeMin = 12; caption.fontSize = fontSize;
                FightUI.Place(caption.rectTransform, 4, 0, actionWidth - 8, 40);
                x += actionWidth + 8;
            }
        }

        private static float SumEditorWidths(float[] widths)
        {
            float total = 0;
            foreach (float width in widths) total += width;
            return total;
        }

        private static float MeasureEditorCaption(TextMeshProUGUI text, string value, float size)
        {
            float previousSize = text.fontSize;
            bool previousAutoSize = text.enableAutoSizing;
            var previousAlignment = text.alignment;
            text.enableAutoSizing = false; text.fontSize = size; text.alignment = TextAlignmentOptions.TopLeft;
            text.ForceMeshUpdate(true, true);
            float width = text.GetPreferredValues(value, float.PositiveInfinity, float.PositiveInfinity).x;
            text.fontSize = previousSize; text.enableAutoSizing = previousAutoSize; text.alignment = previousAlignment;
            return width;
        }

        private void BindEditorNativeLabel(TextMeshProUGUI text, string source)
        {
            if (text == null) return;
            if (!editorNativeLabels.ContainsKey(text)) editorNativeLabels.Add(text, text.text);
            FightLocalizedText binding = text.gameObject.AddComponent<FightLocalizedText>();
            binding.Initialize(text, source); editorSkinComponents.Add(binding);
        }

        private static string EditorCardParameters(MDPro3.Duel.YGOSharp.Card card)
        {
            if (!card.HasType(MDPro3.Duel.YGOSharp.CardType.Monster)) return card.GetSpellTrapType();
            MDPro3.Duel.YGOSharp.Card.LevelType levelType = card.GetLevelType();
            string value;
            if (levelType == MDPro3.Duel.YGOSharp.Card.LevelType.Link)
            {
                var arrows = new List<string>();
                if (card.HasLinkMarker(MDPro3.Duel.YGOSharp.CardLinkMarker.TopLeft)) arrows.Add("↖");
                if (card.HasLinkMarker(MDPro3.Duel.YGOSharp.CardLinkMarker.Top)) arrows.Add("↑");
                if (card.HasLinkMarker(MDPro3.Duel.YGOSharp.CardLinkMarker.TopRight)) arrows.Add("↗");
                if (card.HasLinkMarker(MDPro3.Duel.YGOSharp.CardLinkMarker.Left)) arrows.Add("←");
                if (card.HasLinkMarker(MDPro3.Duel.YGOSharp.CardLinkMarker.Right)) arrows.Add("→");
                if (card.HasLinkMarker(MDPro3.Duel.YGOSharp.CardLinkMarker.BottomLeft)) arrows.Add("↙");
                if (card.HasLinkMarker(MDPro3.Duel.YGOSharp.CardLinkMarker.Bottom)) arrows.Add("↓");
                if (card.HasLinkMarker(MDPro3.Duel.YGOSharp.CardLinkMarker.BottomRight)) arrows.Add("↘");
                value = "LINK-" + card.GetLinkCount() + "   ATK " + card.GetAttackString()
                    + "\n连接箭头 " + (arrows.Count == 0 ? "—" : string.Join(" ", arrows));
            }
            else value = (levelType == MDPro3.Duel.YGOSharp.Card.LevelType.Rank ? "阶级 " : "等级 ") + card.Level
                + "\nATK " + card.GetAttackString() + "   DEF " + card.GetDefenseString();
            if (card.HasType(MDPro3.Duel.YGOSharp.CardType.Pendulum)) value += "\n灵摆刻度 " + card.LScale + " / " + card.RScale;
            return value;
        }

        private void SkinEditorWidget(Transform widget, float scale)
        {
            // Include inactive ancestors: the shipping card templates are hidden
            // during mounting, and later clones must retain their masks/materials.
            bool IsCardVisual(Component component)
            {
                // Walk explicitly: the shipping Unity references expose only
                // the active-parent overload, which misses hidden templates.
                for (var parent = component.transform; parent != null; parent = parent.parent)
                    if (parent.GetComponent<SelectionButton_CardInDeck>() != null
                        || parent.GetComponent<SelectionButton_CardInCollection>() != null
                        || parent.GetComponent<CardRawImageHandler>() != null) return true;
                return false;
            }
            void Tint(Graphic graphic, Color color)
            {
                if (!editorGraphicStyles.TryGetValue(graphic, out EditorGraphicStyle style))
                    editorGraphicStyles.Add(graphic, style = new EditorGraphicStyle(graphic));
                style.Tint(color);
            }
            foreach (var text in widget.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (IsCardVisual(text)) continue;
                var input = text.GetComponentInParent<TMP_InputField>();
                bool placeholder = input != null && input.placeholder == text;
                Tint(text, placeholder ? FightUI.Muted : input != null ? FightUI.Foreground
                    : text.GetComponentInParent<Selectable>() != null ? FightUI.Gold : FightUI.Foreground);
                if (placeholder) text.fontStyle = FontStyles.Normal;
                if (!editorTypography.Any(item => item.Text == text)) editorTypography.Add(new EditorTypography(text));
            }
            foreach (var image in widget.GetComponentsInChildren<Image>(true))
            {
                if (IsCardVisual(image)) continue;
                var name = image.name.ToLowerInvariant();
                if (name.Contains("card") || name.Contains("attribute") || name.Contains("rarity")
                    || name.Contains("icondeck") || name.Contains("limit") || name.Contains("point")) continue;
                var control = image.GetComponentInParent<Selectable>();
                bool ordinaryControl = control != null && (control is Button || control is TMP_InputField
                    || control.GetComponent<SelectionButton>() != null)
                    && !control.name.ToLowerInvariant().Contains("noitem");
                if (name == "mask" || name == "caret") continue;
                if (!editorGraphicStyles.ContainsKey(image)) editorGraphicStyles.Add(image, new EditorGraphicStyle(image));
                // Native controls keep their selection and click handlers. Their
                // bevel/cursor artwork is replaced by the client focus frame.
                if (ordinaryControl && (name == "out" || name == "corner" || name == "cursor" || name.Contains("hover")))
                { editorGraphicStyles[image].Hide(); continue; }
                bool surface = name.Contains("background") || name.Contains("plate") || name.Contains("window")
                    || name == "bg" || name == "base" || name.Contains("frame")
                    || ordinaryControl && control.targetGraphic == image;
                if (name.Contains("icon") || ordinaryControl && !surface && name != "line")
                {
                    image.material = null;
                    Tint(image, FightUI.Gold);
                    continue;
                }
                if (name == "line")
                {
                    image.material = null;
                    Tint(image, ordinaryControl ? Color.clear : FightUI.GoldBorder);
                    continue;
                }
                if (surface)
                {
                    editorGraphicStyles[image].ReplaceSurface(ordinaryControl ? FightSkin.ButtonSurface : FightSkin.Surface);
                    image.material = null;
                    Tint(image, FightUI.Raised);
                    if (ordinaryControl && control.transform.Find("Fight control frame") == null)
                    {
                        FightOutline border = control.gameObject.AddComponent<FightOutline>();
                        editorSkinComponents.Add(border);
                        border.effectColor = FightUI.GoldBorder; border.effectDistance = new Vector2(1, -1);
                        var frame = FightUI.Node(control.transform, "Fight control frame", 0, 0, 0, 0);
                        FightUI.Stretch(frame);
                        FightFrame metal = frame.gameObject.AddComponent<FightFrame>();
                        metal.ButtonFrame = true; metal.Bind(border);
                        editorAdditions.Add(frame.gameObject);
                    }
                }
            }
            foreach (var control in widget.GetComponentsInChildren<Selectable>(true))
            {
                if (IsCardVisual(control)) continue;
                if (!editorControlStyles.ContainsKey(control)) editorControlStyles.Add(control, new EditorControlStyle(control));
                FightFrame frame = control.transform.Find("Fight control frame")?.GetComponent<FightFrame>();
                if (frame != null)
                {
                    FightButtonFeedback feedback = control.GetComponents<FightButtonFeedback>().FirstOrDefault(item => item.enabled);
                    if (feedback == null) { feedback = control.gameObject.AddComponent<FightButtonFeedback>(); editorSkinComponents.Add(feedback); }
                    feedback.Bind(frame);
                }
                if (control is TMP_InputField input)
                {
                    FightUI.ConfigurePlainTextInput(input);
                }
            }
            LayoutEditorTypography(widget, scale);
        }

        private void LabelEditorPoolTools(CardCollectionView collection)
        {
            var tools = new[] { "FilterToggle", "SortButton", "ClearButton", "SearchButton" };
            var labels = new[] { "筛选", "排序", "清空", "" };
            var hints = new[] { "筛选卡池中的卡片", "调整卡池排序", "清除搜索、筛选条件与卡包范围", "重新搜索当前卡池 · 数字为结果数量" };
            Presentation.Elements.ElementObjectManager toolManager = collection.GetComponent<Presentation.Elements.ElementObjectManager>()?.GetElement<Presentation.Elements.ElementObjectManager>("FilterAndSortArea");
            if (toolManager == null) return;
            for (int i = 0; i < tools.Length; i++)
            {
                SelectionButton native = toolManager.GetElement<SelectionButton>(tools[i]);
                var control = native?.GetSelectable();
                if (control == null) continue;
                var rect = control.GetComponent<RectTransform>();
                var icon = i < 3 ? native.GetComponent<Presentation.Elements.ElementObjectManager>()?.GetElement<Image>("Icon") : null;
                DisableEditorLayoutDrivers(rect);
                foreach (var image in control.GetComponentsInChildren<Image>(true))
                {
                    if (image == icon) continue;
                    if (!editorGraphicStyles.ContainsKey(image)) editorGraphicStyles.Add(image, new EditorGraphicStyle(image));
                    editorGraphicStyles[image].Hide();
                }
                foreach (var text in control.GetComponentsInChildren<TextMeshProUGUI>(true))
                { editorGraphics.Add(new EditorGraphic(text)); text.enabled = false; }
                var surface = FightUI.Panel(control.transform, 0, 0, rect.rect.width, rect.rect.height, FightUI.Raised, true, "Fight tool surface");
                surface.GetComponent<Image>().sprite = FightSkin.ButtonSurface;
                surface.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                FightUI.Stretch(surface); surface.SetAsFirstSibling();
                FightFrame frame = surface.GetComponentInChildren<FightFrame>(); frame.ButtonFrame = true;
                FightButtonFeedback feedback = control.GetComponents<FightButtonFeedback>().FirstOrDefault(item => item.enabled);
                if (feedback == null) { feedback = control.gameObject.AddComponent<FightButtonFeedback>(); editorSkinComponents.Add(feedback); }
                feedback.Bind(frame);
                editorAdditions.Add(surface.gameObject);
                if (icon != null)
                {
                    editorMounts.Add(new EditorMount(icon.rectTransform));
                    icon.rectTransform.SetParent(control.transform, false);
                    FightUI.Place(icon.rectTransform, 16, (rect.rect.height - 28) / 2, 28, 28);
                }
                var caption = FightUI.Text(control.transform, i == 3 ? (collection.superScrollView?.items.Count ?? 0) + " 张" : labels[i], icon != null ? 54 : 10, 0,
                    rect.rect.width - (icon != null ? 64 : 20), rect.rect.height, Mathf.Max(22, 16 / collection.transform.localScale.x),
                    FightUI.Gold, icon != null ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center);
                caption.raycastTarget = false;
                if (i == 1)
                {
                    editorPoolSortCaption = caption;
                    caption.enableAutoSizing = true; caption.fontSizeMin = 16; caption.fontSizeMax = caption.fontSize;
                    caption.enableWordWrapping = false;
                }
                if (i == 3)
                {
                    caption.enableAutoSizing = true; caption.enableWordWrapping = false;
                    caption.fontSizeMin = 20; caption.fontSizeMax = caption.fontSize;
                }
                caption.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                editorAdditions.Add(caption.gameObject);
                editorTypography.Add(new EditorTypography(caption));
                editorPoolToolLabels.Add(control, caption);
                if (i == 3) { editorPoolSearchCommand = native; editorPoolSearchCount = caption; }
                AttachEditorToolHint(control.gameObject, hints[i]);
            }
        }

        private void AttachEditorToolHint(GameObject target, string hint)
        {
            FightTooltip existing = target.GetComponents<FightTooltip>().FirstOrDefault(item => item.enabled);
            if (existing != null)
            {
                if (editorSkinComponents.Contains(existing)) existing.Hint = hint;
                return;
            }
            FightTooltip tooltip = target.AddComponent<FightTooltip>(); tooltip.Hint = hint;
            editorSkinComponents.Add(tooltip);
        }

        private void LayoutEditorTypography(Transform widget, float scale)
        {
            foreach (EditorTypography item in editorTypography)
                if (item.Text != null && item.Text.transform.IsChildOf(widget)) item.Apply(scale);
        }

        internal bool SaveEditorThen(Action continued)
        {
            CancelEditorSaveContinuation();
            editorSaveContinuation = continued;
            return SaveEditor();
        }

        internal void CancelEditorSaveContinuation()
        {
            if (!editorAwaitingOverwrite) return;
            editorAwaitingOverwrite = false;
            editorSaveContinuation = null;
            editorOverwriteAllowed = false;
        }

        private void ResetEditorSaveContinuation()
        {
            editorAwaitingOverwrite = false;
            editorSaveContinuation = null;
            editorOverwriteAllowed = false;
        }

        internal bool SaveEditor()
        {
            if (clientEditorUI == null || !clientEditorUI.DeckView.deckLoaded)
            {
                ResetEditorSaveContinuation();
                ShowStatus("卡组仍在加载，请稍后再保存。"); return false;
            }
            DeckView view = clientEditorUI.DeckView;
            try
            {
                if (Controllers.DeckEditor.Banlist.Name != MDPro3.Duel.YGOSharp.BanlistManager.EMPTY_LIST_NAME
                    && (view.mainCount > 60 || view.extraCount > 15 || view.sideCount > 15))
                    throw new InvalidOperationException("构筑超过张数上限。未完成的卡组可以保存，超出上限时请选择无禁限卡表。");
                var name = MDPro3.Duel.YGOSharp.Deck.NormalizeDeckFileName(view.GetDeckName());
                if (!MDPro3.Duel.YGOSharp.Deck.IsValidDeckFileName(name)) throw new InvalidOperationException("请填写有效的卡组名称。");
                MDPro3.Duel.YGOSharp.Deck data = view.FromObjectDeckToCodedDeck();
                var relative = string.IsNullOrEmpty(data.type) ? name : data.type + "/" + name;
                var target = Path.GetFullPath(Runtime.Program.PATH_DECK + relative + Runtime.Program.EXPANSION_YDK);
                if (!editorOverwriteAllowed && File.Exists(target)
                    && !string.Equals(target, editorLoadedPath, StringComparison.OrdinalIgnoreCase))
                {
                    var box = ShowDialog("覆盖同名卡组", 620, 280);
                    FightUI.Text(box, "“" + relative + "”已经存在。保存将替换这套个人卡组的文件。", 28, 88, 560, 94, 20);
                    FightUI.Button(box, "取消", 260, 208, 144, 48, CloseDialog);
                    var overwrite = FightUI.Button(box, "覆盖并保存", 424, 208, 168, 48, () =>
                    {
                        editorAwaitingOverwrite = false;
                        CloseDialog();
                        editorOverwriteAllowed = true;
                        SaveEditor();
                    });
                    FightUI.StyleDestructiveButton(overwrite);
                    editorAwaitingOverwrite = true;
                    return false;
                }
                view.KeepCurrentDeckOnSave = true;
                if (!view.Save()) { ResetEditorSaveContinuation(); return false; }
                var oldRelative = editorRelativeName;
                var oldPath = editorLoadedPath;
                editorRelativeName = relative;
                editorLoadedPath = target;
                Controllers.DeckEditor.Deck = view.Deck;
                Controllers.DeckEditor.DeckName = relative;
                Controllers.DeckEditor.DeckIsFromLocal = true;
                editorOverwriteAllowed = false;
                var sourceRenamed = !string.Equals(oldPath, target, StringComparison.Ordinal)
                    && (!File.Exists(oldPath) || string.Equals(oldPath, target, StringComparison.OrdinalIgnoreCase));
                if (sourceRenamed && MDPro3.Config.GetConfigDeckName() == oldRelative && oldRelative != relative)
                    MDPro3.Config.SetConfigDeck(relative);
                if (sourceRenamed)
                {
                    MDPro3.AI.ResponsesAiConfig config = MDPro3.AI.ResponsesAiConfigStore.Load();
                    var changed = false;
                    if (SameEditorPath(config.DeckA, oldPath)) { config.DeckA = target; changed = true; }
                    if (SameEditorPath(config.DeckB, oldPath)) { config.DeckB = target; changed = true; }
                    if (changed) MDPro3.AI.ResponsesAiConfigStore.SaveChoices(config.DeckA, config.DeckB, config.AiVsAi);
                }
                MDPro3.Config.Save();
                ShowStatus("已保存个人卡组：" + relative);
                var continuation = editorSaveContinuation;
                ResetEditorSaveContinuation();
                continuation?.Invoke();
                return true;
            }
            catch (Exception e) { ResetEditorSaveContinuation(); ShowStatus("保存失败：" + e.Message); return false; }
        }

        private static bool SameEditorPath(string value, string path)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(path)) return false;
            try { return string.Equals(Path.GetFullPath(value), path, StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }

        internal void DiscardEditor()
        {
            if (clientEditorUI == null) return;
            if (!string.IsNullOrEmpty(editorLoadedPath) && File.Exists(editorLoadedPath))
            {
                MDPro3.Duel.YGOSharp.Deck disk = new MDPro3.Duel.YGOSharp.Deck(editorLoadedPath);
                disk.type = Path.GetDirectoryName(editorRelativeName) ?? string.Empty;
                Controllers.DeckEditor.Deck = disk;
                Controllers.DeckEditor.DeckName = editorRelativeName;
                clientEditorUI.DeckView.PrintDeck(disk, editorRelativeName, DeckView.Condition.Editable);
                clientEditorUI.DeckView.SetDirty(false);
            }
            else clientEditorUI.DeckView.SetDirty(false);
        }

        internal void ReleaseEditor()
        {
            ResetEditorSaveContinuation();
            // Clear active ownership before restoring native objects. A second
            // release (including shutdown callbacks) must see a detached editor.
            ServantUI.DeckEditorUI ui = clientEditorUI; clientEditorUI = null;
            EditorDeckScroll scrolling = editorDeckScroll; editorDeckScroll = null;
            editorAwaitingMount = false;
            try
            {
                if (ui != null)
                {
                    ui.ClientSave = null;
                    ui.ClientOverlayParent = null;
                    if (ui.CardCollectionView != null)
                    {
                        ui.CardCollectionView.ClientImageCache = null;
                        foreach (CardRawImageHandler image in ui.CardCollectionView.GetComponentsInChildren<CardRawImageHandler>(true))
                            if (image.GetComponentInParent<SelectionButton_CardInCollection>() != null) image.SetClientPool(null);
                    }
                    editorImageCache?.Dispose(); editorImageCache = null;
                    if (ui.DeckView != null)
                    {
                        ui.DeckView.KeepCurrentDeckOnSave = false;
                        ui.DeckView.ClientDeckLayout = false;
                    }
                }
                foreach (EditorDeckSection section in editorDeckSections) section.Restore();
                foreach (EditorGraphic graphic in editorGraphics) graphic.Restore();
                foreach (EditorGraphicStyle style in editorGraphicStyles.Values) style.Restore();
                foreach (EditorControlStyle style in editorControlStyles.Values) style.Restore();
                foreach (EditorTypography typography in editorTypography) typography.Restore();
                foreach (var label in editorNativeLabels) if (label.Key != null) label.Key.text = label.Value;
                foreach (EditorCardGeometry geometry in editorCardGeometry.Values) geometry.Apply(1);
                for (var i = editorMounts.Count - 1; i >= 0; i--) editorMounts[i].Restore();
                scrolling?.Restore();
                foreach (EditorBehaviour driver in editorBehaviours) driver.Restore();
            }
            finally
            {
                // Destroy only client additions after cards and native controls
                // have returned to their original parents. Pending Destroy nodes
                // are hidden immediately so another mount in this frame is safe.
                foreach (var addition in editorAdditions)
                    if (addition != null) { addition.SetActive(false); Destroy(addition); }
                foreach (var component in editorSkinComponents)
                    if (component != null)
                    {
                        if (component is Behaviour behaviour) behaviour.enabled = false;
                        Destroy(component);
                    }
                editorDeckSections.Clear(); editorTabs.Clear(); editorGraphics.Clear(); editorBehaviours.Clear(); editorAdditions.Clear();
                editorGraphicStyles.Clear(); editorControlStyles.Clear(); editorSkinComponents.Clear();
                editorNativeLabels.Clear();
                editorPoolToolLabels.Clear();
                editorPoolSearchCommand = null; editorPoolSearchCount = null;
                editorPoolSortCaption = null;
                editorSectionJumpButton = null;
                editorToolbarButtons.Clear(); editorTypography.Clear(); editorScrollAnchors.Clear();
                editorCardGeometry.Clear();
                editorCardFocusFrame = null;
                editorLayoutSignature = -1;
                editorHeaderTutorial = null;
                editorDeckWidget = editorPoolWidget = null; editorInspector = null;
                editorDivider = editorActionHost = editorHeaderInput = editorHeaderArea = editorHeaderNameGroup = null;
                editorMounts.Clear();
                editorViewHost = editorCollectionHost = editorDetailHost = null;
                editorTitleText = editorStateText = editorCountsText = editorSummaryText = null; editorSaveButton = null;
            }
        }

        private sealed class EditorWidget
        {
            internal readonly RectTransform Rect, Host, Frame;
            internal readonly float WrapperWidth, NativeWidth, TopInset, BottomInset, TopBleed, BottomBleed;
            internal EditorWidget(RectTransform rect, RectTransform host, RectTransform frame,
                float wrapperWidth, float nativeWidth, float topInset, float bottomInset, float topBleed, float bottomBleed)
            {
                Rect = rect; Host = host; Frame = frame; WrapperWidth = wrapperWidth; NativeWidth = nativeWidth;
                TopInset = topInset; BottomInset = bottomInset; TopBleed = topBleed; BottomBleed = bottomBleed;
            }
        }

        private sealed class EditorInspector
        {
            internal MDPro3.Duel.YGOSharp.Card Card;
            internal int CopyCount = -1;
            internal ScrollRect Scroll;
            internal RectTransform Content, Art, ArtDivider, ControlsDivider, EmptyIcon, EffectDivider;
            internal TextMeshProUGUI Heading, Type, Parameters, Description, EffectHeading, Copies;
            internal Button Add, Remove, More;
        }

        private sealed class EditorInputAnchor
        {
            private readonly TMP_InputField input;
            private readonly string text;
            private readonly int caret, anchor, focus;
            internal EditorInputAnchor(TMP_InputField value)
            {
                input = value; text = value.text; caret = value.caretPosition;
                anchor = value.selectionAnchorPosition; focus = value.selectionFocusPosition;
            }
            internal void Restore()
            {
                if (input == null || !input.isFocused || input.text != text) return;
                input.caretPosition = caret; input.selectionAnchorPosition = anchor; input.selectionFocusPosition = focus;
            }
        }

        private sealed class EditorScrollAnchor
        {
            private bool known;
            private float requested, applied;
            internal void Remember(ScrollRect scroll)
            {
                var current = scroll.verticalNormalizedPosition;
                var viewport = scroll.viewport != null ? scroll.viewport : scroll.GetComponent<RectTransform>();
                bool scrollable = scroll.content != null && scroll.content.rect.height > viewport.rect.height + .5f;
                // A taller viewport may temporarily fit the whole content. Keep
                // the previous position until the user actually scrolls again.
                if (!known) requested = scrollable ? Mathf.Clamp01(current) : 1;
                else if (scrollable && Mathf.Abs(current - applied) > .001f) requested = Mathf.Clamp01(current);
                known = true;
            }
            internal void Restore(ScrollRect scroll)
            {
                if (scroll.content == null) return;
                var viewport = scroll.viewport != null ? scroll.viewport : scroll.GetComponent<RectTransform>();
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = scroll.content.rect.height > viewport.rect.height + .5f ? requested : 1;
                applied = scroll.verticalNormalizedPosition;
            }
        }

        private sealed class EditorTypography
        {
            internal readonly TextMeshProUGUI Text;
            private readonly float size, min, max;
            internal EditorTypography(TextMeshProUGUI text)
            { Text = text; size = text.fontSize; min = text.fontSizeMin; max = text.fontSizeMax; }
            internal void Apply(float scale)
            {
                Text.fontSize = Mathf.Clamp(size, 16 / scale, 28 / scale);
                if (!Text.enableAutoSizing) return;
                Text.fontSizeMin = Mathf.Clamp(min, 16 / scale, 28 / scale);
                Text.fontSizeMax = Mathf.Clamp(max, Text.fontSizeMin, 28 / scale);
            }
            internal void Restore()
            {
                if (Text == null) return;
                Text.fontSize = size; Text.fontSizeMin = min; Text.fontSizeMax = max;
            }
        }

        private sealed class EditorCardGeometry
        {
            private readonly RectTransform[] rects;
            private readonly Vector2[] sizes;
            private readonly Vector3[] positions;
            private float applied = 1;
            internal EditorCardGeometry(RectTransform root)
            {
                rects = root != null ? root.GetComponentsInChildren<RectTransform>(true) : Array.Empty<RectTransform>();
                sizes = rects.Select(rect => rect.sizeDelta).ToArray();
                positions = rects.Select(rect => rect.anchoredPosition3D).ToArray();
            }
            internal void Apply(float scale)
            {
                if (Mathf.Approximately(scale, applied)) return;
                applied = scale;
                // Size authored child geometry so the native drag animation can
                // keep using its own localScale without erasing viewport density.
                for (int i = 0; i < rects.Length; i++)
                {
                    if (rects[i] == null) continue;
                    rects[i].sizeDelta = sizes[i] * scale;
                    rects[i].anchoredPosition3D = positions[i] * scale;
                }
            }
        }

        private sealed class EditorDeckScroll
        {
            private readonly ScrollRect scroll;
            private readonly RectTransform originalContent, originalViewport;
            private readonly Scrollbar originalBar;
            private readonly ScrollRect.ScrollbarVisibility visibility;
            private readonly ScrollRect.MovementType movement;
            private readonly bool horizontal, vertical, enabled;
            private readonly float sensitivity;
            private readonly Vector3 originalContentPosition;
            internal RectTransform Content, Viewport, Rail;
            internal CanvasGroup Loading;
            internal EditorDeckScroll(ScrollRect value)
            {
                scroll = value; originalContent = value.content; originalViewport = value.viewport;
                originalBar = value.verticalScrollbar; visibility = value.verticalScrollbarVisibility;
                movement = value.movementType; horizontal = value.horizontal; vertical = value.vertical;
                sensitivity = value.scrollSensitivity;
                enabled = value.enabled;
                originalContentPosition = originalContent != null ? originalContent.anchoredPosition3D : Vector3.zero;
            }
            internal void Restore()
            {
                if (scroll == null) return;
                scroll.StopMovement(); scroll.content = originalContent; scroll.viewport = originalViewport;
                scroll.verticalScrollbar = originalBar; scroll.verticalScrollbarVisibility = visibility;
                scroll.movementType = movement; scroll.horizontal = horizontal; scroll.vertical = vertical;
                scroll.scrollSensitivity = sensitivity;
                scroll.enabled = enabled;
                // The native prefab can intentionally have no ScrollRect content.
                // Its normalized setter dereferences that null content. Restore
                // geometry directly without running a bounds calculation.
                if (originalContent != null) originalContent.anchoredPosition3D = originalContentPosition;
            }
        }

        private sealed class EditorDeckSection
        {
            internal readonly RectTransform Root;
            internal readonly GridLayoutGroup Grid;
            internal readonly DeckView.DeckLocation Location;
            internal readonly Vector2 NativeCell;
            private readonly string label;
            private readonly TextMeshProUGUI title, facts, emptyHint;
            private readonly GridLayoutGroup.Constraint constraint;
            private readonly int columns;
            private readonly GridLayoutGroup.Axis axis;
            private readonly TextAnchor alignment;
            private readonly RectOffset padding;
            private readonly Vector2 spacing;
            internal EditorDeckSection(RectTransform root, GridLayoutGroup grid, DeckView.DeckLocation location,
                string name, TextMeshProUGUI heading, TextMeshProUGUI counts, TextMeshProUGUI hint)
            {
                Root = root; Grid = grid; Location = location; label = name; title = heading; facts = counts; emptyHint = hint;
                constraint = grid.constraint; columns = grid.constraintCount; axis = grid.startAxis;
                NativeCell = grid.cellSize;
                alignment = grid.childAlignment; padding = grid.padding; spacing = grid.spacing;
            }
            internal void Refresh(DeckView view)
            {
                var count = Location == DeckView.DeckLocation.MainDeck ? view.mainCount
                    : Location == DeckView.DeckLocation.ExtraDeck ? view.extraCount : view.sideCount;
                title.text = label + " · " + count;
                if (emptyHint != null) emptyHint.gameObject.SetActive(count == 0);
                if (view.cards == null) return;
                var types = Location == DeckView.DeckLocation.ExtraDeck
                    ? new[] { MDPro3.Duel.YGOSharp.CardType.Fusion, MDPro3.Duel.YGOSharp.CardType.Synchro, MDPro3.Duel.YGOSharp.CardType.Xyz, MDPro3.Duel.YGOSharp.CardType.Link }
                    : new[] { MDPro3.Duel.YGOSharp.CardType.Monster, MDPro3.Duel.YGOSharp.CardType.Spell, MDPro3.Duel.YGOSharp.CardType.Trap };
                var names = Location == DeckView.DeckLocation.ExtraDeck
                    ? new[] { "融合", "同调", "超量", "连接" } : new[] { "怪兽", "魔法", "陷阱" };
                var values = new List<string>();
                for (var i = 0; i < types.Length; i++)
                {
                    var number = view.cards.Count(c => c != null && c.location == Location && c.Card != null && c.Card.Id != 0 && c.Card.HasType(types[i]));
                    if (number > 0) values.Add(names[i] + " " + number);
                }
                facts.text = string.Join(" · ", values);
            }
            internal void Reflow(float width, float scale)
            {
                var header = title.transform.parent as RectTransform;
                FightUI.Place(header, 4, 0, width - 8, 34);
                FightUI.Place(title.rectTransform, 0, 0, 180, 32);
                FightUI.Place(facts.rectTransform, 180, 0, Mathf.Max(1, width - 188), 32);
                facts.fontSize = Mathf.Max(20, 16 / scale);
                FightUI.Place(emptyHint.rectTransform, 12, 58, width - 24, 40);
            }
            internal void ResizeCells(bool large)
            {
                Grid.cellSize = large ? new Vector2(94, 94 * 1.46f) : NativeCell;
            }
            internal void Restore()
            {
                if (Grid == null) return;
                Grid.constraint = constraint; Grid.constraintCount = columns; Grid.startAxis = axis;
                Grid.childAlignment = alignment; Grid.padding = padding; Grid.spacing = spacing;
                Grid.cellSize = NativeCell;
            }
        }

        private sealed class EditorTab
        {
            private readonly CardCollectionView collection;
            private readonly CardCollectionView.Area area;
            private readonly Image image;
            private readonly Outline outline;
            private readonly TextMeshProUGUI text;
            private readonly RectTransform selectedLine, selectedMark;
            internal EditorTab(CardCollectionView control, CardCollectionView.Area selectedArea, Image fill, Outline border, TextMeshProUGUI label,
                RectTransform line, RectTransform mark)
            { collection = control; area = selectedArea; image = fill; outline = border; text = label; selectedLine = line; selectedMark = mark; }
            internal void Refresh()
            {
                if (collection == null || image == null) return;
                bool selected = collection.area == area;
                image.color = selected ? FightUI.Raised : FightUI.PanelColor;
                outline.effectColor = selected ? FightUI.GoldBorder : FightUI.Border;
                text.color = selected ? FightUI.Active : FightUI.Muted;
                selectedLine.gameObject.SetActive(selected); selectedMark.gameObject.SetActive(selected);
            }
            internal void Reflow()
            {
                if (image == null || collection == null) return;
                var panel = image.rectTransform;
                var host = panel.parent as RectTransform;
                var width = host.rect.width / 3;
                var index = area == CardCollectionView.Area.Collection ? 0 : area == CardCollectionView.Area.Bookmark ? 1 : 2;
                FightUI.Place(panel, index * width, 0, width, host.rect.height);
                text.fontSize = Mathf.Max(22, 17 / collection.transform.localScale.x);
                FightUI.Place(selectedLine, 12, host.rect.height - 2, width - 24, 1);
                FightUI.Place(selectedMark, width / 2 - 4, host.rect.height - 7, 8, 8);
            }
        }

        private sealed class EditorControlStyle
        {
            private readonly Selectable control;
            private readonly ColorBlock colors;
            private readonly Selectable.Transition transition;
            internal EditorControlStyle(Selectable value)
            {
                control = value; colors = value.colors; transition = value.transition;
                var next = colors; next.normalColor = Color.white; next.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1);
                next.selectedColor = new Color(1.1f, 1.1f, 1.1f, 1); next.pressedColor = FightUI.Hex("819CA8");
                next.disabledColor = new Color(.48f, .48f, .48f, .72f); next.fadeDuration = .14f;
                value.colors = next; value.transition = Selectable.Transition.ColorTint;
            }
            internal void Restore() { if (control != null) { control.colors = colors; control.transition = transition; } }
        }

        private sealed class EditorGraphicStyle
        {
            // SelectionButton invokes these containers directly, including when
            // disabled. Configure their serialized palette instead of disabling
            // the component or repainting over native events every frame.
            private static readonly string[] PaletteNames = { "colorUnselected", "colorSelected", "colorButtonDown", "colorButtonEnter", "colorButtonInactive",
                "colorModeUnselected", "colorModeSelected", "colorModeButtonDown", "colorModeButtonEnter", "colorModeButtonInactive",
                "intensityUnselected", "intensitySelected", "intensityButtonDown", "intensityButtonEnter", "intensityButtonInactive" };
            private static readonly FieldInfo[] PaletteFields = PaletteNames.Select(name => typeof(Presentation.UI.ColorContainer)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)).ToArray();
            private readonly Graphic graphic;
            private readonly Color color, baseColor;
            private readonly Material material;
            private readonly bool enabled;
            private readonly Sprite sprite;
            private readonly Image.Type imageType;
            private readonly FontStyles fontStyle;
            private readonly Presentation.UI.ColorContainerGraphic container;
            private readonly object[] palette;
            private bool replacedSurface, hidden;
            internal EditorGraphicStyle(Graphic value)
            {
                graphic = value; color = value.color; material = value.material; enabled = value.enabled;
                if (value is Image image) { sprite = image.sprite; imageType = image.type; }
                if (value is TextMeshProUGUI text) fontStyle = text.fontStyle;
                container = value.GetComponent<Presentation.UI.ColorContainerGraphic>();
                if (container != null)
                {
                    baseColor = container.baseColor;
                    palette = PaletteFields.Select(field => field?.GetValue(container)).ToArray();
                }
            }
            internal void Tint(Color value)
            {
                graphic.color = value;
                if (container == null) return;
                // GetColor* already multiplies baseColor. A neutral base keeps
                // Multiple mode from squaring the target color on every event.
                container.baseColor = Color.white;
                var factors = new[] { value, value * new Color(1.1f, 1.1f, 1.1f, 1), FightUI.Hex("819CA8"),
                    new Color(1.22f, 1.22f, 1.22f, 1), new Color(.48f, .48f, .48f, .72f) };
                for (int i = 0; i < PaletteFields.Length; i++)
                    PaletteFields[i]?.SetValue(container, i < 5 ? (object)factors[i]
                        : i < 10 ? Presentation.UI.ColorContainer.ColorMode.Multiple : (object)1f);
            }
            internal void Hide() { graphic.enabled = false; hidden = true; }
            internal void ReplaceSurface(Sprite value)
            {
                if (!(graphic is Image image)) return;
                image.sprite = value; image.type = Image.Type.Sliced; replacedSurface = true;
            }
            internal void Restore()
            {
                if (graphic == null) return;
                graphic.color = color; graphic.material = material;
                if (hidden) graphic.enabled = enabled;
                if (replacedSurface && graphic is Image image) { image.sprite = sprite; image.type = imageType; }
                if (graphic is TextMeshProUGUI text) text.fontStyle = fontStyle;
                if (container == null) return;
                container.baseColor = baseColor;
                for (int i = 0; i < PaletteFields.Length; i++) PaletteFields[i]?.SetValue(container, palette[i]);
            }
        }

        private sealed class EditorGraphic
        {
            private readonly Graphic graphic;
            private readonly bool enabled;
            internal EditorGraphic(Graphic value) { graphic = value; enabled = value.enabled; }
            internal void Restore() { if (graphic != null) graphic.enabled = enabled; }
        }

        private sealed class EditorBehaviour
        {
            private readonly Behaviour behaviour;
            private readonly bool enabled;
            internal EditorBehaviour(Behaviour value) { behaviour = value; enabled = value.enabled; }
            internal void Restore() { if (behaviour != null) behaviour.enabled = enabled; }
        }

        private sealed class EditorMount
        {
            internal readonly RectTransform Rect;
            private readonly Transform parent;
            private readonly int sibling;
            private readonly Vector2 min, max, pivot, size;
            private readonly Vector3 position;
            private readonly Vector3 scale;
            private readonly Quaternion rotation;
            private readonly bool active;
            internal EditorMount(RectTransform rect)
            {
                Rect = rect; parent = rect.parent; sibling = rect.GetSiblingIndex(); min = rect.anchorMin; max = rect.anchorMax;
                pivot = rect.pivot; position = rect.anchoredPosition3D; size = rect.sizeDelta; scale = rect.localScale;
                rotation = rect.localRotation; active = rect.gameObject.activeSelf;
            }
            internal void Restore()
            {
                if (Rect == null || parent == null) return;
                Rect.SetParent(parent, false); Rect.SetSiblingIndex(sibling); Rect.anchorMin = min; Rect.anchorMax = max;
                Rect.pivot = pivot; Rect.anchoredPosition3D = position; Rect.sizeDelta = size; Rect.localScale = scale;
                Rect.localRotation = rotation; Rect.gameObject.SetActive(active);
            }
        }
    }
}
