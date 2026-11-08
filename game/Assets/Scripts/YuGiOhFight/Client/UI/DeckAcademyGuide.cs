using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
{
    public class DeckAcademyGuide : MonoBehaviour
    {
        private ServantUI.OcgCoreUI coreUI;
        private GameObject body;
        private TextMeshProUGUI stepTitle;
        private TextMeshProUGUI chapterTitle;
        private TextMeshProUGUI modeStatus;
        private TextMeshProUGUI readingStatus;
        private TextMeshProUGUI guideHeading;
        private TextMeshProUGUI stepCounter;
        private TextMeshProUGUI instructions;
        private TextMeshProUGUI cardsHeading;
        private TextMeshProUGUI cardsText;
        private TextMeshProUGUI fontTemplate;
        private SelectionButton collapseButton;
        private SelectionButton previousButton;
        private SelectionButton nextButton;
        private SelectionButton fullGuideButton;
        private SelectionButton playbackButton;
        private SelectionButton singleStepButton;
        private SelectionButton followButton;
        private GameObject replayControls;
        private ScrollRect instructionsScroll;
        private LayoutElement instructionsLayout;
        private RectTransform panel;
        private Vector2 lastParentSize;
        private bool collapsed;
        private bool constrained;
        private bool lastReplay;
        private bool lastPause;
        private bool lastFollow;
        private bool lastFinished;
        private bool lastCanFollow;
        private int lastPage = -2;
        private Services.DeckAcademyDeck lastDeck;
        private string lastLanguage, lastCardLanguage;
        private readonly List<TextMeshProUGUI> actionCaptions = new();
        private readonly Dictionary<int, float> readingPositions = new();
        private Outline guideBorder;
        private CanvasGroup dockVisibility;
        private bool dockOccluded;
        private float dockOriginalAlpha;
        private bool dockOriginalInteractable, dockOriginalBlocksRaycasts;

        public void Initialize(ServantUI.OcgCoreUI ui, SelectionButton buttonTemplate, TextMeshProUGUI textTemplate)
        {
            coreUI = ui;
            fontTemplate = textTemplate;
            panel = GetComponent<RectTransform>();
            panel.anchorMin = panel.anchorMax = new Vector2(1f, 1f);
            panel.pivot = new Vector2(1f, 1f);
            panel.anchoredPosition = new Vector2(-20f, -170f);
            panel.sizeDelta = new Vector2(350f, 0f);
            var background = gameObject.AddComponent<Image>();
            background.color = new Color(Client.FightUI.PanelColor.r, Client.FightUI.PanelColor.g, Client.FightUI.PanelColor.b, .96f);
            var border = gameObject.AddComponent<Outline>();
            guideBorder = border;
            border.effectColor = Client.FightUI.GoldBorder;
            border.effectDistance = new Vector2(1f, -1f);
            var layout = gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayout(layout);
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8f;
            gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var header = Row("GuideHeader", transform, 34f);
            guideHeading = Label("GuideHeading", header, "分步指导", 22f, 34f);
            guideHeading.color = Client.FightUI.Gold; guideHeading.fontStyle = FontStyles.Bold;
            guideHeading.alignment = TextAlignmentOptions.MidlineLeft;
            guideHeading.enableWordWrapping = false;
            guideHeading.enableAutoSizing = true;
            guideHeading.fontSizeMax = 22f; guideHeading.fontSizeMin = 18f;
            guideHeading.GetComponent<LayoutElement>().flexibleWidth = 1f;
            stepCounter = Label("GuideStepCount", header, "", 16f, 34f);
            stepCounter.alignment = TextAlignmentOptions.Center; stepCounter.color = Client.FightUI.Muted;
            stepCounter.GetComponent<LayoutElement>().minWidth = stepCounter.GetComponent<LayoutElement>().preferredWidth = 72f;
            collapseButton = GuideButton(buttonTemplate, header, "收起", ToggleCollapsed, 34f, 60f);

            // Playback and reading remain visible when the prose is collapsed.
            modeStatus = Label("GuideModeStatus", transform, "", 17f, 18f);
            modeStatus.color = Client.FightUI.Gold; modeStatus.enableWordWrapping = false;
            modeStatus.overflowMode = TextOverflowModes.Ellipsis;
            readingStatus = Label("GuideReadingStatus", transform, "", 16f, 18f);
            readingStatus.color = Client.FightUI.Muted; readingStatus.enableWordWrapping = false;
            readingStatus.overflowMode = TextOverflowModes.Ellipsis;

            body = new GameObject("GuideBody", typeof(RectTransform), typeof(VerticalLayoutGroup));
            body.transform.SetParent(transform, false);
            var bodyLayout = body.GetComponent<VerticalLayoutGroup>();
            ConfigureLayout(bodyLayout);
            bodyLayout.spacing = 8f;
            chapterTitle = Label("GuideDeckTitle", body.transform, "", 18f, 26f);
            chapterTitle.color = Client.FightUI.Muted; chapterTitle.enableWordWrapping = false;
            chapterTitle.overflowMode = TextOverflowModes.Ellipsis;
            stepTitle = Label("StepTitle", body.transform, string.Empty, 24f, 54f);
            stepTitle.color = Client.FightUI.Foreground; stepTitle.fontStyle = FontStyles.Bold;
            stepTitle.enableAutoSizing = false;
            stepTitle.enableWordWrapping = true;
            stepTitle.overflowMode = TextOverflowModes.Overflow;
            BuildInstructions(body.transform);

            var pages = Row("GuidePages", body.transform, 36f);
            previousButton = GuideButton(buttonTemplate, pages, "上一步", () => ChangePage(-1), 36f);
            nextButton = GuideButton(buttonTemplate, pages, "下一步", () => ChangePage(1), 36f);
            fullGuideButton = GuideButton(buttonTemplate, pages, "完整教程", OpenFullTutorial, 36f);

            var playback = Row("GuideReplay", body.transform, 36f);
            replayControls = playback.gameObject;
            playbackButton = GuideButton(buttonTemplate, playback, "暂停", TogglePlayback, 36f);
            singleStepButton = GuideButton(buttonTemplate, playback, "单步播放", Services.DeckAcademy.StepReplayEvent, 36f);
            followButton = GuideButton(buttonTemplate, playback, "跟随提示", Services.DeckAcademy.FollowCurrentReplay, 36f);
            Client.FightUI.Hint(previousButton.gameObject, "阅读上一步。播放中的演示会先暂停。");
            Client.FightUI.Hint(nextButton.gameObject, "阅读下一步。播放中的演示会先暂停。");
            Client.FightUI.Hint(singleStepButton.gameObject, "播放下一个录像事件后暂停，便于观察场面变化。");
            Client.FightUI.Hint(followButton.gameObject, "将阅读位置重新对齐到演示正在播放的步骤。");
            ScrollSpeed.Apply(gameObject);
            Refresh();
        }

        public void Refresh()
        {
            if (!Services.DeckAcademy.HasActiveGuide)
            {
                readingPositions.Clear(); lastDeck = null; lastPage = -2;
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);
            Services.DeckAcademyDeck deck = Services.DeckAcademy.ActiveDeck;
            var page = Mathf.Clamp(Services.DeckAcademy.ActiveTutorialPage, 0, deck.steps.Length);
            var replay = Services.DeckAcademy.HasActiveReplay;
            var follow = Services.DeckAcademy.FollowReplayGuide;
            var finished = replay && Services.DeckAcademy.ReplayFinished;
            var canFollow = Services.DeckAcademy.CanFollowReplayGuide;
            bool chapterChanged = lastDeck != deck || lastPage != page || lastLanguage != MDPro3.Utility.Language.GetConfig() || lastCardLanguage != MDPro3.Utility.Language.GetCardConfig();
            if (lastDeck != deck || lastReplay != replay) readingPositions.Clear();
            else if (chapterChanged) RememberReadingPosition(lastPage);
            else RememberReadingPosition(page);
            modeStatus.text = Client.FightText.Get(!replay ? "固定起手练习 · 手动阅读" : finished ? "演示已结束 · 可继续阅读"
                : (Controllers.OcgCore.pause ? "演示暂停" : "演示播放中") + " · " + (follow ? "跟随提示" : "手动阅读"));
            replayControls.SetActive(replay);
            if (chapterChanged)
            {
                if (page < deck.steps.Length)
                {
                    Services.DeckAcademyStep step = deck.steps[page];
                    chapterTitle.text = Client.FightText.Get(deck.name);
                    stepCounter.text = Client.FightText.Get((page + 1).ToString("00") + " / " + deck.steps.Length.ToString("00"));
                    stepTitle.text = Client.FightText.Get(step.title);
                    instructions.text = Client.FightText.Get(string.IsNullOrWhiteSpace(step.text) ? "此步骤暂未提供文字说明，可查看涉及卡片或打开完整教程。" : step.text);
                    cardsHeading.text = Client.FightText.Get("涉及卡片 · " + (step.card_codes?.Length ?? 0) + " 张");
                    cardsText.text = Services.DeckAcademy.CardNames(step.card_codes);
                }
                else
                {
                    chapterTitle.text = Client.FightText.Get(deck.name);
                    stepCounter.text = Client.FightText.Get("终场");
                    stepTitle.text = Client.FightText.Get("终场与注意事项");
                    string cautionText = string.Join("\n\n", deck.cautions ?? new string[0]);
                    instructions.text = Client.FightText.Get((string.IsNullOrWhiteSpace(deck.end_board?.text) ? "完成展开路线后，可结合终场卡片检查场面。" : deck.end_board.text)
                        + (string.IsNullOrWhiteSpace(cautionText) ? "" : "\n\n注意事项\n" + cautionText));
                    cardsHeading.text = Client.FightText.Get("终场卡片 · " + (deck.end_board?.codes?.Length ?? 0) + " 张");
                    cardsText.text = Services.DeckAcademy.CardNames(deck.end_board?.codes);
                }
                cardsHeading.gameObject.SetActive(!string.IsNullOrWhiteSpace(cardsText.text));
                cardsText.gameObject.SetActive(!string.IsNullOrWhiteSpace(cardsText.text));
            }
            UpdateReadingStatus();
            // Native cloned captions are only translated at creation. Reapply
            // their original keys when the interface language changes in a duel.
            previousButton.SetButtonText(Client.FightText.Get("上一步"));
            nextButton.SetButtonText(Client.FightText.Get("下一步"));
            fullGuideButton.SetButtonText(Client.FightText.Get("完整教程"));
            singleStepButton.SetButtonText(Client.FightText.Get("单步播放"));
            ResizeDock(replay);
            Canvas.ForceUpdateCanvases();
            RestoreReadingPosition(page);
            UpdateReadingStatus();
            previousButton.GetComponent<Selectable>().interactable = page > 0;
            nextButton.GetComponent<Selectable>().interactable = page < deck.steps.Length;
            playbackButton.SetButtonText(Client.FightText.Get(finished ? "已结束" : Controllers.OcgCore.pause ? "播放" : "暂停"));
            playbackButton.GetComponent<Selectable>().interactable = !finished;
            singleStepButton.GetComponent<Selectable>().interactable = !finished;
            followButton.SetButtonText(Client.FightText.Get(follow ? "已跟随" : canFollow ? "跟随提示" : "手动阅读"));
            followButton.GetComponent<Selectable>().interactable = canFollow && !follow;
            lastDeck = deck; lastLanguage = MDPro3.Utility.Language.GetConfig(); lastCardLanguage = MDPro3.Utility.Language.GetCardConfig();
            lastPage = page;
            lastReplay = replay;
            lastPause = Controllers.OcgCore.pause;
            lastFollow = follow;
            lastFinished = finished;
            lastCanFollow = canFollow;
        }

        private void Update()
        {
            if (!Services.DeckAcademy.HasActiveGuide || lastLanguage != MDPro3.Utility.Language.GetConfig() || lastCardLanguage != MDPro3.Utility.Language.GetCardConfig() || lastDeck != Services.DeckAcademy.ActiveDeck
                || lastPage != Mathf.Clamp(Services.DeckAcademy.ActiveTutorialPage, 0, Services.DeckAcademy.ActiveDeck.steps.Length)
                || lastReplay != Services.DeckAcademy.HasActiveReplay || lastPause != Controllers.OcgCore.pause
                || lastFollow != Services.DeckAcademy.FollowReplayGuide || lastFinished != Services.DeckAcademy.ReplayFinished
                || lastCanFollow != Services.DeckAcademy.CanFollowReplayGuide
                || (panel.parent is RectTransform parent && parent.rect.size != lastParentSize))
                Refresh();
        }

        private void LateUpdate()
        {
            UpdateDockVisibility();
            UpdateReadingStatus();
            var palette = Client.FightDuelPopupSkin.SceneThemeActive
                ? Client.ScenePalette.DialogFor(Runtime.Program.instance.ocgcore.DuelBGManager.ActiveSceneId) : null;
            // The dock shares the popup reading palette. Keeping a single
            // font scale avoids reflow and conflicting colours when the field
            // skin updates later in the same frame.
            if (palette != null && GetComponent<Client.FightDuelPopupSkin>() == null)
                Client.FightDuelPopupSkin.Attach(panel, guideHeading);
            if (guideBorder != null) guideBorder.effectColor = palette != null ? palette.Edge : Client.FightUI.GoldBorder;
            if (guideHeading != null) guideHeading.color = palette != null ? palette.Heading : Client.FightUI.Gold;
            if (modeStatus != null) modeStatus.color = palette != null ? palette.Muted : Client.FightUI.Gold;
            // Native selection events repaint their inherited colour settings.
            // Apply the guide palette after those events while keeping the
            // original input, sound and surface feedback.
            foreach (var caption in actionCaptions)
            {
                if (caption == null) continue;
                var control = caption.GetComponentInParent<Selectable>();
                bool available = control == null || control.IsInteractable();
                var color = palette != null ? available ? palette.Heading : palette.Muted
                    : available ? Client.FightUI.Gold : Client.FightUI.Muted;
                caption.color = color;
                if (caption.TryGetComponent<Presentation.UI.ColorContainerGraphic>(out Presentation.UI.ColorContainerGraphic container))
                    container.baseColor = color;
                caption.canvasRenderer.SetColor(new Color(1, 1, 1, available ? 1 : .48f));
            }
        }

        private void UpdateDockVisibility()
        {
            bool occupied = Client.FightDuelPopupSkin.SceneThemeActive && coreUI != null
                && (Client.FightDuelLogSkin.OccupiesGuideDock(coreUI.DuelLog)
                    || (coreUI.CardList != null && coreUI.CardList.OccupiesGuideDock));
            if (!occupied)
            {
                RestoreDockVisibility();
                return;
            }
            if (dockVisibility == null)
                dockVisibility = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            if (!dockOccluded)
            {
                dockOriginalAlpha = dockVisibility.alpha;
                dockOriginalInteractable = dockVisibility.interactable;
                dockOriginalBlocksRaycasts = dockVisibility.blocksRaycasts;
                dockOccluded = true;
            }
            // Leave the guide active: its collapsed flag, reading page,
            // playback status and ScrollRect keep their original ownership.
            dockVisibility.alpha = 0;
            dockVisibility.interactable = false;
            dockVisibility.blocksRaycasts = false;
        }

        private void RestoreDockVisibility()
        {
            if (!dockOccluded) return;
            if (dockVisibility != null)
            {
                dockVisibility.alpha = dockOriginalAlpha;
                dockVisibility.interactable = dockOriginalInteractable;
                dockVisibility.blocksRaycasts = dockOriginalBlocksRaycasts;
            }
            dockOccluded = false;
        }

        private void OnDisable()
        {
            RestoreDockVisibility();
        }
        private void OnDestroy() => RestoreDockVisibility();

        private void ResizeDock(bool replay)
        {
            if (!(panel.parent is RectTransform parent))
                return;
            var size = parent.rect.size;
            if (size.x <= 0f || size.y <= 0f)
                return;

            // Use the safe-area canvas units, not display pixels. Keep the
            // opponent's LP above the guide and playback/duel controls below it.
            var topInset = Mathf.Min(170f, size.y * 0.18f);
            var bottomInset = Mathf.Min(230f, size.y * 0.22f);
            var availableHeight = Mathf.Min(500f, size.y - topInset - bottomInset);
            var width = Mathf.Min(Mathf.Clamp(size.x * 0.25f, 300f, 350f), size.x - 40f);
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            panel.anchoredPosition = new Vector2(-20f, -topInset);

            // The inherited CJK font has a taller line box than its glyphs.
            // Measure the real status before budgeting the remaining reading area.
            float statusHeight = Mathf.Max(18f, modeStatus.GetPreferredValues(modeStatus.text, Mathf.Max(1f, width - 24f), float.PositiveInfinity).y + 2f);
            var statusLayout = modeStatus.GetComponent<LayoutElement>();
            statusLayout.minHeight = statusLayout.preferredHeight = statusHeight;
            float readingHeight = Mathf.Max(18f, readingStatus.GetPreferredValues(readingStatus.text, Mathf.Max(1f, width - 24f), float.PositiveInfinity).y + 2f);
            var readingLayout = readingStatus.GetComponent<LayoutElement>();
            readingLayout.minHeight = readingLayout.preferredHeight = readingHeight;
            float titleHeight = Mathf.Max(54f, stepTitle.GetPreferredValues(stepTitle.text, Mathf.Max(1f, width - 24f), float.PositiveInfinity).y + 2f);
            var titleLayout = stepTitle.GetComponent<LayoutElement>();
            titleLayout.minHeight = titleLayout.preferredHeight = titleHeight;
            float chapterHeight = Mathf.Max(26f, chapterTitle.GetPreferredValues(chapterTitle.text, Mathf.Max(1f, width - 24f), float.PositiveInfinity).y + 2f);
            var chapterLayout = chapterTitle.GetComponent<LayoutElement>();
            chapterLayout.minHeight = chapterLayout.preferredHeight = chapterHeight;
            var fixedHeight = (replay ? 250f : 206f) + titleHeight - 54f + chapterHeight - 26f
                + statusHeight + 8f + readingHeight + 8f;
            constrained = availableHeight < fixedHeight + 80f || width < 300f;
            instructionsLayout.minHeight = instructionsLayout.preferredHeight =
                Mathf.Clamp(availableHeight - fixedHeight, 80f, 230f);
            body.SetActive(!collapsed && !constrained);
            collapseButton.SetButtonText(Client.FightText.Get(constrained ? "教程" : collapsed ? "展开" : "收起"));
            // Reserve the measured counter and caption widths; the heading
            // takes the remaining space and can scale between 18 and 22 points.
            var countLayout = stepCounter.GetComponent<LayoutElement>();
            countLayout.minWidth = countLayout.preferredWidth = Mathf.Max(48f,
                stepCounter.GetPreferredValues(stepCounter.text, float.PositiveInfinity, float.PositiveInfinity).x + 8f);
            var collapseCaption = collapseButton.GetComponent<Presentation.Elements.ElementObjectManager>().GetElement<TextMeshProUGUI>("ButtonText");
            collapseCaption.fontSize = collapseCaption.fontSizeMax;
            var collapseLayout = collapseButton.GetComponent<LayoutElement>();
            collapseLayout.minWidth = collapseLayout.preferredWidth = Mathf.Max(52f,
                collapseCaption.GetPreferredValues(collapseCaption.text, float.PositiveInfinity, float.PositiveInfinity).x
                + collapseCaption.rectTransform.offsetMin.x - collapseCaption.rectTransform.offsetMax.x + 2f);
            lastParentSize = size;
        }

        private void RememberReadingPosition(int page)
        {
            if (page < 0 || body == null || !body.activeInHierarchy) return;
            readingPositions[page] = instructionsScroll.content.rect.height <= instructionsScroll.viewport.rect.height + 1f
                ? 1f : Mathf.Clamp01(instructionsScroll.verticalNormalizedPosition);
        }

        private void RestoreReadingPosition(int page)
        {
            if (!body.activeInHierarchy) return;
            instructionsScroll.StopMovement();
            instructionsScroll.verticalNormalizedPosition = instructionsScroll.content.rect.height <= instructionsScroll.viewport.rect.height + 1f
                ? 1f : readingPositions.TryGetValue(page, out var position) ? position : 1f;
        }

        private void UpdateReadingStatus()
        {
            if (readingStatus == null || !Services.DeckAcademy.HasActiveGuide) return;
            int count = Services.DeckAcademy.ActiveDeck.steps.Length;
            int page = Mathf.Clamp(Services.DeckAcademy.ActiveTutorialPage, 0, count);
            string location = page < count ? "阅读步骤 " + (page + 1).ToString("00") + " / " + count.ToString("00") : "阅读终场";
            if (body.activeSelf && instructionsScroll.content.rect.height > instructionsScroll.viewport.rect.height + 1f)
                location += instructionsScroll.verticalNormalizedPosition <= .01f ? " · 已读到底" : " · 可向下阅读";
            if (readingStatus.text != location) readingStatus.text = Client.FightText.Get(location);
        }

        private void ChangePage(int delta)
        {
            var page = Mathf.Clamp(Services.DeckAcademy.ActiveTutorialPage, 0, Services.DeckAcademy.ActiveDeck.steps.Length);
            var target = Mathf.Clamp(page + delta, 0, Services.DeckAcademy.ActiveDeck.steps.Length);
            if (target == page) return;
            // Reading another chapter leaves automatic replay following. Pause
            // through the native authority so the board stays with the text.
            if (Services.DeckAcademy.HasActiveReplay && !Services.DeckAcademy.ReplayFinished && !Controllers.OcgCore.pause)
                coreUI.OnStop();
            Services.DeckAcademy.SetGuidePage(target);
            Refresh();
        }

        private void ToggleCollapsed()
        {
            if (constrained)
            {
                OpenFullTutorial();
                return;
            }
            RememberReadingPosition(lastPage);
            collapsed = !collapsed;
            ResizeDock(Services.DeckAcademy.HasActiveReplay);
            Canvas.ForceUpdateCanvases();
            if (!collapsed) RestoreReadingPosition(lastPage);
            UpdateReadingStatus();
        }

        private void TogglePlayback()
        {
            if (Controllers.OcgCore.pause)
                coreUI.OnPlay();
            else
                coreUI.OnStop();
            Refresh();
        }

        private void OpenFullTutorial()
        {
            if (Services.DeckAcademy.HasActiveReplay)
                coreUI.OnStop();
            Runtime.UIManager.ShowPopupDeckAcademy(true);
        }

        private void BuildInstructions(Transform parent)
        {
            var go = new GameObject("GuideInstructions", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(ScrollRect));
            go.transform.SetParent(parent, false);
            instructionsLayout = go.GetComponent<LayoutElement>();
            instructionsLayout.minHeight = instructionsLayout.preferredHeight = 230f;
            go.GetComponent<Image>().color = Client.FightUI.Background;
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(go.transform, false);
            var rect = viewport.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10f, 10f);
            rect.offsetMax = new Vector2(-16f, -10f);
            var content = new GameObject("ReadingContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var textRect = content.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.sizeDelta = Vector2.zero;
            var readingLayout = content.GetComponent<VerticalLayoutGroup>();
            ConfigureLayout(readingLayout);
            readingLayout.spacing = 10f;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            instructions = Label("StepInstructions", content.transform, string.Empty, 22f, 0f);
            instructions.lineSpacing = 4f; instructions.paragraphSpacing = 4f;
            cardsHeading = Label("GuideCardsHeading", content.transform, string.Empty, 16f, 0f);
            cardsHeading.color = Client.FightUI.Muted;
            cardsText = Label("GuideCards", content.transform, string.Empty, 18f, 0f);
            cardsText.color = Client.FightUI.Gold; cardsText.lineSpacing = 3f;
            instructionsScroll = go.GetComponent<ScrollRect>();
            instructionsScroll.viewport = rect;
            instructionsScroll.content = textRect;
            instructionsScroll.horizontal = false;
            instructionsScroll.scrollSensitivity = 12f;
            instructionsScroll.movementType = ScrollRect.MovementType.Clamped;
            AddReadingScrollbar(go.GetComponent<RectTransform>(), instructionsScroll);
        }

        private SelectionButton GuideButton(SelectionButton template, Transform parent, string caption, System.Action action, float height, float width = 0f)
        {
            // Keep the original selection, sound and bound caption components;
            // only cover their decorative surface with the client palette.
            SelectionButton control = Popup.PopupDeckAcademy.CreateButton(template, parent, caption, action, height, width, compact: true);
            var face = new GameObject("Guide button face", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Outline));
            face.transform.SetParent(control.transform, false);
            face.GetComponent<LayoutElement>().ignoreLayout = true;
            var rect = face.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = face.GetComponent<Image>(); image.color = Client.FightUI.Raised; image.raycastTarget = false;
            var outline = face.GetComponent<Outline>(); outline.effectColor = Client.FightUI.Border; outline.effectDistance = new Vector2(1f, -1f);
            foreach (var text in control.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                text.rectTransform.SetAsLastSibling(); text.color = Client.FightUI.Gold;
                text.fontSize = text.fontSizeMax = 19f; text.fontSizeMin = 16f;
                actionCaptions.Add(text);
            }
            var button = control.GetComponent<Button>();
            if (button != null)
            {
                button.targetGraphic = image; button.transition = Selectable.Transition.ColorTint;
                var colors = button.colors; colors.normalColor = Color.white;
                colors.highlightedColor = colors.selectedColor = new Color(1.3f, 1.35f, 1.4f);
                colors.pressedColor = Client.FightUI.Hex("7A909C"); colors.disabledColor = new Color(.5f, .5f, .5f, .65f);
                colors.fadeDuration = .12f; button.colors = colors;
            }
            return control;
        }

        private static void AddReadingScrollbar(RectTransform parent, ScrollRect scroll)
        {
            var rail = new GameObject("Guide reading scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            rail.transform.SetParent(parent, false);
            var rect = rail.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f); rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-16f, 6f); rect.offsetMax = new Vector2(-3f, -6f);
            rail.GetComponent<Image>().color = Color.clear;
            var track = new GameObject("Track", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(rail.transform, false);
            var trackRect = track.GetComponent<RectTransform>();
            trackRect.anchorMin = Vector2.zero; trackRect.anchorMax = Vector2.one;
            trackRect.offsetMin = new Vector2(9f, 0f); trackRect.offsetMax = Vector2.zero;
            track.GetComponent<Image>().color = Client.FightUI.Border; track.GetComponent<Image>().raycastTarget = false;
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(rail.transform, false);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero; handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = handleRect.offsetMax = Vector2.zero;
            handle.GetComponent<Image>().color = Color.clear; handle.GetComponent<Image>().raycastTarget = false;
            var thumb = new GameObject("Thumb", typeof(RectTransform), typeof(Image));
            thumb.transform.SetParent(handle.transform, false);
            var thumbRect = thumb.GetComponent<RectTransform>();
            thumbRect.anchorMin = Vector2.zero; thumbRect.anchorMax = Vector2.one;
            thumbRect.offsetMin = new Vector2(9f, 0f); thumbRect.offsetMax = Vector2.zero;
            thumb.GetComponent<Image>().color = Client.FightUI.GoldBorder;
            var bar = rail.GetComponent<Scrollbar>(); bar.direction = Scrollbar.Direction.BottomToTop;
            bar.handleRect = handleRect; bar.targetGraphic = thumb.GetComponent<Image>();
            scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        private RectTransform Row(string name, Transform parent, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(parent, false);
            var element = go.GetComponent<LayoutElement>();
            element.minWidth = element.preferredWidth = 0f;
            element.preferredHeight = height;
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            ConfigureLayout(layout);
            layout.childForceExpandWidth = false;
            layout.spacing = 6f;
            return go.GetComponent<RectTransform>();
        }

        private TextMeshProUGUI Label(string name, Transform parent, string text, float size, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var element = go.GetComponent<LayoutElement>();
            element.minWidth = element.preferredWidth = 0f;
            if (height > 0f)
                element.minHeight = element.preferredHeight = height;
            var label = go.GetComponent<TextMeshProUGUI>();
            label.font = fontTemplate.font;
            label.fontSharedMaterial = fontTemplate.fontSharedMaterial;
            label.text = Client.FightText.Get(text);
            if (name != "GuideCards") Client.FightText.Bind(label, text);
            label.fontSize = size;
            label.richText = false;
            label.color = Client.FightUI.Foreground;
            label.raycastTarget = false;
            label.enableWordWrapping = true;
            label.alignment = TextAlignmentOptions.TopLeft;
            return label;
        }

        private static void ConfigureLayout(HorizontalOrVerticalLayoutGroup layout)
        {
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;
        }
    }
}
