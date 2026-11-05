using MDPro3.Servant;
using MDPro3.UI.Popup;
using MDPro3.UI.Client;
using MDPro3.UI.ServantUI;
using MDPro3.Utility;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YgomSystem.UI;

namespace MDPro3.UI
{
    public class DeckAcademyGuide : MonoBehaviour
    {
        private OcgCoreUI coreUI;
        private GameObject body;
        private TextMeshProUGUI stepTitle;
        private TextMeshProUGUI chapterTitle;
        private TextMeshProUGUI modeStatus;
        private TextMeshProUGUI readingStatus;
        private TextMeshProUGUI stepCounter;
        private TextMeshProUGUI instructions;
        private TextMeshProUGUI cardsHeading;
        private TextMeshProUGUI cardsText;
        private TextMeshProUGUI fontTemplate;
        private SelectionButton collapseButton;
        private SelectionButton previousButton;
        private SelectionButton nextButton;
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
        private DeckAcademyDeck lastDeck;
        private readonly List<TextMeshProUGUI> actionCaptions = new();
        private readonly Dictionary<int, float> readingPositions = new();

        public void Initialize(OcgCoreUI ui, SelectionButton buttonTemplate, TextMeshProUGUI textTemplate)
        {
            coreUI = ui;
            fontTemplate = textTemplate;
            panel = GetComponent<RectTransform>();
            panel.anchorMin = panel.anchorMax = new Vector2(1f, 1f);
            panel.pivot = new Vector2(1f, 1f);
            panel.anchoredPosition = new Vector2(-20f, -170f);
            panel.sizeDelta = new Vector2(350f, 0f);
            var background = gameObject.AddComponent<Image>();
            background.color = new Color(FightUI.PanelColor.r, FightUI.PanelColor.g, FightUI.PanelColor.b, .96f);
            var border = gameObject.AddComponent<Outline>();
            border.effectColor = FightUI.GoldBorder;
            border.effectDistance = new Vector2(1f, -1f);
            var layout = gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayout(layout);
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8f;
            gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var header = Row("GuideHeader", transform, 34f);
            var heading = Label("GuideHeading", header, "分步指导", 20f, 34f);
            heading.color = FightUI.Gold; heading.fontStyle = FontStyles.Bold;
            heading.alignment = TextAlignmentOptions.MidlineLeft;
            heading.GetComponent<LayoutElement>().flexibleWidth = 1f;
            stepCounter = Label("GuideStepCount", header, "", 13f, 34f);
            stepCounter.alignment = TextAlignmentOptions.Center; stepCounter.color = FightUI.Muted;
            stepCounter.GetComponent<LayoutElement>().minWidth = stepCounter.GetComponent<LayoutElement>().preferredWidth = 72f;
            collapseButton = GuideButton(buttonTemplate, header, "收起", ToggleCollapsed, 34f, 60f);

            // Playback and reading remain visible when the prose is collapsed.
            modeStatus = Label("GuideModeStatus", transform, "", 14f, 18f);
            modeStatus.color = FightUI.Gold; modeStatus.enableWordWrapping = false;
            modeStatus.overflowMode = TextOverflowModes.Ellipsis;
            readingStatus = Label("GuideReadingStatus", transform, "", 13f, 18f);
            readingStatus.color = FightUI.Muted; readingStatus.enableWordWrapping = false;
            readingStatus.overflowMode = TextOverflowModes.Ellipsis;

            body = new GameObject("GuideBody", typeof(RectTransform), typeof(VerticalLayoutGroup));
            body.transform.SetParent(transform, false);
            var bodyLayout = body.GetComponent<VerticalLayoutGroup>();
            ConfigureLayout(bodyLayout);
            bodyLayout.spacing = 8f;
            chapterTitle = Label("GuideDeckTitle", body.transform, "", 16f, 26f);
            chapterTitle.color = FightUI.Muted; chapterTitle.enableWordWrapping = false;
            chapterTitle.overflowMode = TextOverflowModes.Ellipsis;
            stepTitle = Label("StepTitle", body.transform, string.Empty, 22f, 54f);
            stepTitle.color = FightUI.Foreground; stepTitle.fontStyle = FontStyles.Bold;
            stepTitle.enableAutoSizing = false;
            stepTitle.enableWordWrapping = true;
            stepTitle.overflowMode = TextOverflowModes.Overflow;
            BuildInstructions(body.transform);

            var pages = Row("GuidePages", body.transform, 36f);
            previousButton = GuideButton(buttonTemplate, pages, "上一步", () => ChangePage(-1), 36f);
            nextButton = GuideButton(buttonTemplate, pages, "下一步", () => ChangePage(1), 36f);
            GuideButton(buttonTemplate, pages, "完整教程", OpenFullTutorial, 36f);

            var playback = Row("GuideReplay", body.transform, 36f);
            replayControls = playback.gameObject;
            playbackButton = GuideButton(buttonTemplate, playback, "暂停", TogglePlayback, 36f);
            singleStepButton = GuideButton(buttonTemplate, playback, "单步播放", DeckAcademy.StepReplayEvent, 36f);
            followButton = GuideButton(buttonTemplate, playback, "跟随提示", DeckAcademy.FollowCurrentReplay, 36f);
            FightUI.Hint(previousButton.gameObject, "阅读上一步。播放中的演示会先暂停。");
            FightUI.Hint(nextButton.gameObject, "阅读下一步。播放中的演示会先暂停。");
            FightUI.Hint(singleStepButton.gameObject, "播放下一个录像事件后暂停，便于观察场面变化。");
            FightUI.Hint(followButton.gameObject, "将阅读位置重新对齐到演示正在播放的步骤。");
            ScrollSpeed.Apply(gameObject);
            Refresh();
        }

        public void Refresh()
        {
            if (!DeckAcademy.HasActiveGuide)
            {
                readingPositions.Clear(); lastDeck = null; lastPage = -2;
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);
            var deck = DeckAcademy.ActiveDeck;
            var page = Mathf.Clamp(DeckAcademy.ActiveTutorialPage, 0, deck.steps.Length);
            var replay = DeckAcademy.HasActiveReplay;
            var follow = DeckAcademy.FollowReplayGuide;
            var finished = replay && DeckAcademy.ReplayFinished;
            var canFollow = DeckAcademy.CanFollowReplayGuide;
            bool chapterChanged = lastDeck != deck || lastPage != page;
            if (lastDeck != deck || lastReplay != replay) readingPositions.Clear();
            else if (chapterChanged) RememberReadingPosition(lastPage);
            else RememberReadingPosition(page);
            modeStatus.text = !replay ? "固定起手练习 · 手动阅读" : finished ? "演示已结束 · 可继续阅读"
                : (OcgCore.pause ? "演示暂停" : "演示播放中") + " · " + (follow ? "跟随提示" : "手动阅读");
            replayControls.SetActive(replay);
            if (chapterChanged)
            {
                if (page < deck.steps.Length)
                {
                    var step = deck.steps[page];
                    chapterTitle.text = deck.name;
                    stepCounter.text = (page + 1).ToString("00") + " / " + deck.steps.Length.ToString("00");
                    stepTitle.text = step.title;
                    instructions.text = string.IsNullOrWhiteSpace(step.text) ? "此步骤暂未提供文字说明，可查看涉及卡片或打开完整教程。" : step.text;
                    cardsHeading.text = "涉及卡片 · " + (step.card_codes?.Length ?? 0) + " 张";
                    cardsText.text = DeckAcademy.CardNames(step.card_codes);
                }
                else
                {
                    chapterTitle.text = deck.name;
                    stepCounter.text = "终场";
                    stepTitle.text = "终场与注意事项";
                    string cautionText = string.Join("\n\n", deck.cautions ?? new string[0]);
                    instructions.text = (string.IsNullOrWhiteSpace(deck.end_board?.text) ? "完成展开路线后，可结合终场卡片检查场面。" : deck.end_board.text)
                        + (string.IsNullOrWhiteSpace(cautionText) ? "" : "\n\n注意事项\n" + cautionText);
                    cardsHeading.text = "终场卡片 · " + (deck.end_board?.codes?.Length ?? 0) + " 张";
                    cardsText.text = DeckAcademy.CardNames(deck.end_board?.codes);
                }
                cardsHeading.gameObject.SetActive(!string.IsNullOrWhiteSpace(cardsText.text));
                cardsText.gameObject.SetActive(!string.IsNullOrWhiteSpace(cardsText.text));
            }
            UpdateReadingStatus();
            ResizeDock(replay);
            Canvas.ForceUpdateCanvases();
            RestoreReadingPosition(page);
            UpdateReadingStatus();
            previousButton.GetComponent<Selectable>().interactable = page > 0;
            nextButton.GetComponent<Selectable>().interactable = page < deck.steps.Length;
            playbackButton.SetButtonText(finished ? "已结束" : OcgCore.pause ? "播放" : "暂停");
            playbackButton.GetComponent<Selectable>().interactable = !finished;
            singleStepButton.GetComponent<Selectable>().interactable = !finished;
            followButton.SetButtonText(follow ? "已跟随" : canFollow ? "跟随提示" : "手动阅读");
            followButton.GetComponent<Selectable>().interactable = canFollow && !follow;
            lastDeck = deck;
            lastPage = page;
            lastReplay = replay;
            lastPause = OcgCore.pause;
            lastFollow = follow;
            lastFinished = finished;
            lastCanFollow = canFollow;
        }

        private void Update()
        {
            if (!DeckAcademy.HasActiveGuide || lastDeck != DeckAcademy.ActiveDeck
                || lastPage != Mathf.Clamp(DeckAcademy.ActiveTutorialPage, 0, DeckAcademy.ActiveDeck.steps.Length)
                || lastReplay != DeckAcademy.HasActiveReplay || lastPause != OcgCore.pause
                || lastFollow != DeckAcademy.FollowReplayGuide || lastFinished != DeckAcademy.ReplayFinished
                || lastCanFollow != DeckAcademy.CanFollowReplayGuide
                || (panel.parent is RectTransform parent && parent.rect.size != lastParentSize))
                Refresh();
        }

        private void LateUpdate()
        {
            UpdateReadingStatus();
            // Native selection events repaint their inherited colour settings.
            // Apply the guide palette after those events while keeping the
            // original input, sound and surface feedback.
            foreach (var caption in actionCaptions)
            {
                if (caption == null) continue;
                var control = caption.GetComponentInParent<Selectable>();
                bool available = control == null || control.IsInteractable();
                var color = available ? FightUI.Gold : FightUI.Muted;
                caption.color = color;
                if (caption.TryGetComponent<ColorContainerGraphic>(out var container))
                    container.baseColor = color;
                caption.canvasRenderer.SetColor(new Color(1, 1, 1, available ? 1 : .48f));
            }
        }

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
            collapseButton.SetButtonText(constrained ? "教程" : collapsed ? "展开" : "收起");
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
            if (readingStatus == null || !DeckAcademy.HasActiveGuide) return;
            int count = DeckAcademy.ActiveDeck.steps.Length;
            int page = Mathf.Clamp(DeckAcademy.ActiveTutorialPage, 0, count);
            string location = page < count ? "阅读步骤 " + (page + 1).ToString("00") + " / " + count.ToString("00") : "阅读终场";
            if (body.activeSelf && instructionsScroll.content.rect.height > instructionsScroll.viewport.rect.height + 1f)
                location += instructionsScroll.verticalNormalizedPosition <= .01f ? " · 已读到底" : " · 可向下阅读";
            if (readingStatus.text != location) readingStatus.text = location;
        }

        private void ChangePage(int delta)
        {
            var page = Mathf.Clamp(DeckAcademy.ActiveTutorialPage, 0, DeckAcademy.ActiveDeck.steps.Length);
            var target = Mathf.Clamp(page + delta, 0, DeckAcademy.ActiveDeck.steps.Length);
            if (target == page) return;
            // Reading another chapter leaves automatic replay following. Pause
            // through the native authority so the board stays with the text.
            if (DeckAcademy.HasActiveReplay && !DeckAcademy.ReplayFinished && !OcgCore.pause)
                coreUI.OnStop();
            DeckAcademy.SetGuidePage(target);
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
            ResizeDock(DeckAcademy.HasActiveReplay);
            Canvas.ForceUpdateCanvases();
            if (!collapsed) RestoreReadingPosition(lastPage);
            UpdateReadingStatus();
        }

        private void TogglePlayback()
        {
            if (OcgCore.pause)
                coreUI.OnPlay();
            else
                coreUI.OnStop();
            Refresh();
        }

        private void OpenFullTutorial()
        {
            if (DeckAcademy.HasActiveReplay)
                coreUI.OnStop();
            UIManager.ShowPopupDeckAcademy(true);
        }

        private void BuildInstructions(Transform parent)
        {
            var go = new GameObject("GuideInstructions", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(ScrollRect));
            go.transform.SetParent(parent, false);
            instructionsLayout = go.GetComponent<LayoutElement>();
            instructionsLayout.minHeight = instructionsLayout.preferredHeight = 230f;
            go.GetComponent<Image>().color = FightUI.Background;
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
            instructions = Label("StepInstructions", content.transform, string.Empty, 20f, 0f);
            instructions.lineSpacing = 4f; instructions.paragraphSpacing = 4f;
            cardsHeading = Label("GuideCardsHeading", content.transform, string.Empty, 14f, 0f);
            cardsHeading.color = FightUI.Muted;
            cardsText = Label("GuideCards", content.transform, string.Empty, 16f, 0f);
            cardsText.color = FightUI.Gold; cardsText.lineSpacing = 3f;
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
            var control = PopupDeckAcademy.CreateButton(template, parent, caption, action, height, width, compact: true);
            var face = new GameObject("Guide button face", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Outline));
            face.transform.SetParent(control.transform, false);
            face.GetComponent<LayoutElement>().ignoreLayout = true;
            var rect = face.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = face.GetComponent<Image>(); image.color = FightUI.Raised; image.raycastTarget = false;
            var outline = face.GetComponent<Outline>(); outline.effectColor = FightUI.Border; outline.effectDistance = new Vector2(1f, -1f);
            foreach (var text in control.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                text.rectTransform.SetAsLastSibling(); text.color = FightUI.Gold;
                text.fontSize = text.fontSizeMax = 17f; text.fontSizeMin = 14f;
                actionCaptions.Add(text);
            }
            var button = control.GetComponent<Button>();
            if (button != null)
            {
                button.targetGraphic = image; button.transition = Selectable.Transition.ColorTint;
                var colors = button.colors; colors.normalColor = Color.white;
                colors.highlightedColor = colors.selectedColor = new Color(1.3f, 1.35f, 1.4f);
                colors.pressedColor = FightUI.Hex("7A909C"); colors.disabledColor = new Color(.5f, .5f, .5f, .65f);
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
            track.GetComponent<Image>().color = FightUI.Border; track.GetComponent<Image>().raycastTarget = false;
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
            thumb.GetComponent<Image>().color = FightUI.GoldBorder;
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
            label.text = text;
            label.fontSize = size;
            label.richText = false;
            label.color = FightUI.Foreground;
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
