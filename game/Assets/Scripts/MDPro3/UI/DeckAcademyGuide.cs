using MDPro3.Servant;
using MDPro3.UI.Popup;
using MDPro3.UI.ServantUI;
using MDPro3.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI
{
    public class DeckAcademyGuide : MonoBehaviour
    {
        private OcgCoreUI coreUI;
        private GameObject body;
        private TextMeshProUGUI stepTitle;
        private TextMeshProUGUI instructions;
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
        private int lastPage = -2;
        private DeckAcademyDeck lastDeck;

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
            background.color = new Color(0.018f, 0.038f, 0.068f, 0.94f);
            var border = gameObject.AddComponent<Outline>();
            border.effectColor = new Color(0.35f, 0.56f, 0.73f, 0.7f);
            border.effectDistance = new Vector2(1f, -1f);
            var layout = gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureLayout(layout);
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8f;
            gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var header = Row("GuideHeader", transform, 34f);
            var heading = Label("GuideHeading", header, "分步指导", 22f, 34f);
            heading.GetComponent<LayoutElement>().flexibleWidth = 1f;
            collapseButton = PopupDeckAcademy.CreateButton(buttonTemplate, header, "收起", ToggleCollapsed, 34f, 76f, compact: true);

            body = new GameObject("GuideBody", typeof(RectTransform), typeof(VerticalLayoutGroup));
            body.transform.SetParent(transform, false);
            var bodyLayout = body.GetComponent<VerticalLayoutGroup>();
            ConfigureLayout(bodyLayout);
            bodyLayout.spacing = 8f;
            stepTitle = Label("StepTitle", body.transform, string.Empty, 23f, 58f);
            stepTitle.enableAutoSizing = true;
            stepTitle.fontSizeMin = 16f;
            stepTitle.fontSizeMax = 23f;
            BuildInstructions(body.transform);

            var pages = Row("GuidePages", body.transform, 36f);
            previousButton = PopupDeckAcademy.CreateButton(buttonTemplate, pages, "上一步", () => ChangePage(-1), 36f, compact: true);
            nextButton = PopupDeckAcademy.CreateButton(buttonTemplate, pages, "下一步", () => ChangePage(1), 36f, compact: true);
            PopupDeckAcademy.CreateButton(buttonTemplate, pages, "完整教程", OpenFullTutorial, 36f, compact: true);

            var playback = Row("GuideReplay", body.transform, 36f);
            replayControls = playback.gameObject;
            playbackButton = PopupDeckAcademy.CreateButton(buttonTemplate, playback, "暂停", TogglePlayback, 36f, compact: true);
            singleStepButton = PopupDeckAcademy.CreateButton(buttonTemplate, playback, "单步播放", DeckAcademy.StepReplayEvent, 36f, compact: true);
            followButton = PopupDeckAcademy.CreateButton(buttonTemplate, playback, "跟随提示", DeckAcademy.FollowCurrentReplay, 36f, compact: true);
            ScrollSpeed.Apply(gameObject);
            Refresh();
        }

        public void Refresh()
        {
            if (!DeckAcademy.HasActiveGuide)
            {
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);
            var deck = DeckAcademy.ActiveDeck;
            var page = Mathf.Clamp(DeckAcademy.ActiveTutorialPage, 0, deck.steps.Length);
            var replay = DeckAcademy.HasActiveReplay;
            var follow = DeckAcademy.FollowReplayGuide;
            var finished = replay && DeckAcademy.ReplayFinished;
            replayControls.SetActive(replay);
            ResizeDock(replay);
            if (lastDeck != deck || lastPage != page)
            {
                if (page < deck.steps.Length)
                {
                    var step = deck.steps[page];
                    stepTitle.text = deck.name + "\n" + (page + 1) + "/" + deck.steps.Length + " · " + step.title;
                    instructions.text = step.text;
                    cardsText.text = "涉及卡片：" + DeckAcademy.CardNames(step.card_codes);
                }
                else
                {
                    stepTitle.text = deck.name + "\n终场与注意事项";
                    instructions.text = (deck.end_board?.text ?? string.Empty) + "\n\n"
                        + string.Join("\n\n", deck.cautions ?? new string[0]);
                    cardsText.text = "终场卡片：" + DeckAcademy.CardNames(deck.end_board?.codes);
                }
                Canvas.ForceUpdateCanvases();
                instructionsScroll.verticalNormalizedPosition = 1f;
            }
            previousButton.GetComponent<Selectable>().interactable = page > 0;
            nextButton.GetComponent<Selectable>().interactable = page < deck.steps.Length;
            playbackButton.SetButtonText(OcgCore.pause ? "播放" : "暂停");
            playbackButton.GetComponent<Selectable>().interactable = !finished;
            singleStepButton.GetComponent<Selectable>().interactable = !finished;
            followButton.GetComponent<Selectable>().interactable = DeckAcademy.CanFollowReplayGuide && !follow;
            lastDeck = deck;
            lastPage = page;
            lastReplay = replay;
            lastPause = OcgCore.pause;
            lastFollow = follow;
            lastFinished = finished;
        }

        private void Update()
        {
            if (!DeckAcademy.HasActiveGuide || lastDeck != DeckAcademy.ActiveDeck
                || lastPage != Mathf.Clamp(DeckAcademy.ActiveTutorialPage, 0, DeckAcademy.ActiveDeck.steps.Length)
                || lastReplay != DeckAcademy.HasActiveReplay || lastPause != OcgCore.pause
                || lastFollow != DeckAcademy.FollowReplayGuide || lastFinished != DeckAcademy.ReplayFinished
                || (panel.parent is RectTransform parent && parent.rect.size != lastParentSize))
                Refresh();
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

            // Header, title, footer rows and their padding have fixed heights.
            var fixedHeight = replay ? 220f : 176f;
            constrained = availableHeight < fixedHeight + 80f || width < 300f;
            instructionsLayout.minHeight = instructionsLayout.preferredHeight =
                Mathf.Clamp(availableHeight - fixedHeight, 80f, 230f);
            body.SetActive(!collapsed && !constrained);
            collapseButton.SetButtonText(constrained ? "教程" : collapsed ? "展开" : "收起");
            lastParentSize = size;
        }

        private void ChangePage(int delta)
        {
            var page = Mathf.Clamp(DeckAcademy.ActiveTutorialPage, 0, DeckAcademy.ActiveDeck.steps.Length);
            DeckAcademy.SetGuidePage(page + delta);
            Refresh();
        }

        private void ToggleCollapsed()
        {
            if (constrained)
            {
                OpenFullTutorial();
                return;
            }
            collapsed = !collapsed;
            ResizeDock(DeckAcademy.HasActiveReplay);
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
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.18f);
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(go.transform, false);
            var rect = viewport.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 6f);
            rect.offsetMax = new Vector2(-8f, -6f);
            var content = new GameObject("ReadingContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var textRect = content.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.sizeDelta = Vector2.zero;
            var readingLayout = content.GetComponent<VerticalLayoutGroup>();
            ConfigureLayout(readingLayout);
            readingLayout.spacing = 8f;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            instructions = Label("StepInstructions", content.transform, string.Empty, 21f, 0f);
            cardsText = Label("GuideCards", content.transform, string.Empty, 18f, 0f);
            instructionsScroll = go.GetComponent<ScrollRect>();
            instructionsScroll.viewport = rect;
            instructionsScroll.content = textRect;
            instructionsScroll.horizontal = false;
            instructionsScroll.scrollSensitivity = 12f;
            instructionsScroll.movementType = ScrollRect.MovementType.Clamped;
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
            label.color = Color.white;
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
