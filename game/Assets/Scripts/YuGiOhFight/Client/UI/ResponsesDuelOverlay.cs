using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
{
    public sealed class ResponsesDuelOverlay : MonoBehaviour
    {
        private TextMeshProUGUI status;
        private TextMeshProUGUI heading;
        private TextMeshProUGUI decisions;
        private TextMeshProUGUI readingHint;
        private TextMeshProUGUI stopCaption;
        private RectTransform panel, readingViewport, readingContent, stopRect;
        private ScrollRect readingScroll;
        private Vector2 lastParentSize;
        private string previous;
        private int previousDecisions = -1;

        public void Initialize(TextMeshProUGUI template)
        {
            panel = GetComponent<RectTransform>();
            panel.anchorMin = panel.anchorMax = new Vector2(1f, 1f);
            panel.pivot = new Vector2(1f, 1f);
            panel.anchoredPosition = new Vector2(-20f, -170f);
            panel.sizeDelta = new Vector2(330f, 152f);
            var background = gameObject.AddComponent<Image>();
            background.color = new Color(Client.FightUI.PanelColor.r, Client.FightUI.PanelColor.g, Client.FightUI.PanelColor.b, .96f);
            var border = gameObject.AddComponent<Outline>(); border.effectColor = Client.FightUI.GoldBorder;
            border.effectDistance = new Vector2(1f, -1f);

            heading = CreateLabel("AIHeading", transform, template, 16f, Client.FightUI.Gold);
            heading.text = "AI 对战"; heading.fontStyle = FontStyles.Bold;
            heading.alignment = TextAlignmentOptions.MidlineLeft;
            var indicator = Child(transform, "Active AI indicator", 14, 19, 5, 5).gameObject.AddComponent<Image>();
            indicator.color = Client.FightUI.Cyan; indicator.raycastTarget = false;
            Place(heading.rectTransform, 28, 8, 260, 28);
            decisions = CreateLabel("AIDecisionCount", transform, template, 13f, Client.FightUI.Muted);
            decisions.alignment = TextAlignmentOptions.MidlineRight;
            decisions.enableWordWrapping = false;
            decisions.enableAutoSizing = true; decisions.fontSizeMin = 11f; decisions.fontSizeMax = 13f;
            Place(decisions.rectTransform, 156, 8, 160, 28);

            readingViewport = Child(transform, "AI status reading area", 14, 42, 302, 56);
            readingViewport.gameObject.AddComponent<RectMask2D>();
            var readingSurface = readingViewport.gameObject.AddComponent<Image>();
            readingSurface.color = Client.FightUI.Background;
            readingContent = Child(readingViewport, "AI status content", 8, 6, 280, 56);
            status = CreateLabel("AIStatus", readingContent, template, 18f, Client.FightUI.Foreground);
            status.enableWordWrapping = true; status.lineSpacing = 3f; status.paragraphSpacing = 3f;
            status.overflowMode = TextOverflowModes.Overflow;
            Place(status.rectTransform, 0, 0, 280, 56);
            readingScroll = readingViewport.gameObject.AddComponent<ScrollRect>();
            readingScroll.viewport = readingViewport; readingScroll.content = readingContent;
            readingScroll.horizontal = false; readingScroll.vertical = true;
            readingScroll.movementType = ScrollRect.MovementType.Clamped; readingScroll.scrollSensitivity = 18f;
            BuildScrollbar(readingViewport, readingScroll);
            readingHint = CreateLabel("AIReadingHint", transform, template, 12f, Client.FightUI.Muted);
            readingHint.enableWordWrapping = false;
            readingHint.alignment = TextAlignmentOptions.MidlineLeft;
            readingHint.gameObject.SetActive(false);
            readingScroll.onValueChanged.AddListener(_ => RefreshReadingHint());

            stopRect = Child(transform, "StopAI", 14, 110, 302, 34);
            var actionSurface = stopRect.gameObject.AddComponent<Image>(); actionSurface.color = Client.FightUI.Raised;
            var actionBorder = stopRect.gameObject.AddComponent<Outline>();
            actionBorder.effectColor = new Color(Client.FightUI.Danger.r, Client.FightUI.Danger.g, Client.FightUI.Danger.b, .45f);
            actionBorder.effectDistance = new Vector2(1f, -1f);
            stopCaption = CreateLabel("Label", stopRect, template, 17f, Client.FightUI.Danger);
            stopCaption.alignment = TextAlignmentOptions.Center; stopCaption.text = "停止 AI 对战";
            stopCaption.rectTransform.anchorMin = Vector2.zero; stopCaption.rectTransform.anchorMax = Vector2.one;
            stopCaption.rectTransform.offsetMin = stopCaption.rectTransform.offsetMax = Vector2.zero;
            var button = stopRect.gameObject.AddComponent<Button>(); button.targetGraphic = actionSurface;
            var colors = button.colors; colors.highlightedColor = colors.selectedColor = new Color(1.35f, 1.25f, 1.2f);
            colors.pressedColor = Client.FightUI.Hex("7A909C"); colors.fadeDuration = .12f; button.colors = colors;
            button.onClick.AddListener(() =>
            {
                Services.ResponsesDuelSession.Stop();
                Runtime.Program.instance.ocgcore.OnExit();
            });
            Refresh();
        }

        public void Refresh()
        {
            var visible = Services.ResponsesDuelSession.IsActive && Runtime.Program.instance.ocgcore.showing;
            gameObject.SetActive(visible);
            if (!visible) return;
            heading.text = Services.ResponsesDuelSession.UsesAiAPerspective ? "AI 观战" : "AI 对战";
            stopCaption.text = Services.ResponsesDuelSession.UsesAiAPerspective ? "结束 AI 观战" : "停止 AI 对战";
            bool countChanged = previousDecisions != Services.ResponsesDuelSession.DecisionCount;
            if (countChanged)
            {
                previousDecisions = Services.ResponsesDuelSession.DecisionCount;
                decisions.text = previousDecisions == 0 ? "等待首个决策" : "已完成 " + previousDecisions + " 次决策";
            }
            bool changed = previous != Services.ResponsesDuelSession.Status;
            if (changed)
            {
                previous = Services.ResponsesDuelSession.Status;
                status.text = string.IsNullOrWhiteSpace(previous) ? "等待 AI 状态更新…" : previous;
            }
            var size = panel.parent is RectTransform parent ? parent.rect.size : lastParentSize;
            if (changed || countChanged || size != lastParentSize)
            {
                FitDock(size);
                if (changed) readingScroll.verticalNormalizedPosition = 1f;
            }
            RefreshReadingHint();
        }

        private void FitDock(Vector2 parentSize)
        {
            // All dimensions are native HUD canvas units. Keep the same upper
            // right safe-area docking as the original overlay.
            float width = parentSize.x > 0f ? Mathf.Min(Mathf.Clamp(parentSize.x * .25f, 300f, 350f), parentSize.x - 40f) : 330f;
            width = Mathf.Max(180f, width);
            float inset = parentSize.y > 0f ? Mathf.Min(170f, parentSize.y * .18f) : 170f;
            float available = parentSize.y > 0f ? parentSize.y - inset - Mathf.Min(230f, parentSize.y * .22f) : 500f;
            float textWidth = width - 48f;
            float measured = Mathf.Max(32f, status.GetPreferredValues(status.text, textWidth, float.PositiveInfinity).y + 2f);
            bool narrow = width < 290f;
            float headingWidth = narrow ? width - 42f : 112f, decisionsWidth = narrow ? width - 28f : width - 156f;
            float headingHeight = Mathf.Max(28f, heading.GetPreferredValues(heading.text, headingWidth, float.PositiveInfinity).y + 2f);
            float decisionsHeight = Mathf.Max(28f, decisions.GetPreferredValues(decisions.text, decisionsWidth, float.PositiveInfinity).y + 2f);
            float headerHeight = narrow ? headingHeight + decisionsHeight + 20f : Mathf.Max(headingHeight, decisionsHeight) + 14f;
            float stopHeight = Mathf.Max(34f, stopCaption.GetPreferredValues(stopCaption.text, width - 28f, float.PositiveInfinity).y + 4f);
            float actionHeight = stopHeight + 20f;
            float hintLineHeight = Mathf.Max(readingHint.GetPreferredValues("滚动查看完整状态", width - 28f, float.PositiveInfinity).y,
                readingHint.GetPreferredValues("已读到底", width - 28f, float.PositiveInfinity).y) + 4f;
            float maximumReading = Mathf.Min(112f, Mathf.Max(48f, available - headerHeight - actionHeight));
            bool overflow = measured + 12f > maximumReading + 1f;
            bool showReadingHint = overflow && available >= headerHeight + actionHeight + hintLineHeight + 48f;
            if (showReadingHint) maximumReading = Mathf.Min(112f, Mathf.Max(48f, available - headerHeight - actionHeight - hintLineHeight));
            float readingHeight = Mathf.Clamp(measured + 12f, 48f, maximumReading);
            float hintHeight = showReadingHint ? hintLineHeight : 0f;
            float height = headerHeight + readingHeight + hintHeight + actionHeight;
            panel.sizeDelta = new Vector2(width, height); panel.anchoredPosition = new Vector2(-20f, -inset);
            Place(heading.rectTransform, 28, 8, headingWidth, headingHeight);
            Place(decisions.rectTransform, narrow ? 14f : 142f, narrow ? headingHeight + 12f : 8f, decisionsWidth, decisionsHeight);
            decisions.alignment = narrow ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;
            Place(readingViewport, 14, headerHeight, width - 28f, readingHeight);
            Place(readingContent, 8, 6, textWidth, Mathf.Max(measured, readingHeight - 12f));
            Place(status.rectTransform, 0, 0, textWidth, measured);
            readingHint.gameObject.SetActive(showReadingHint);
            Place(readingHint.rectTransform, 14, headerHeight + readingHeight + 2f, width - 28f, hintLineHeight);
            Place(stopRect, 14, height - stopHeight - 10f, width - 28f, stopHeight);
            lastParentSize = parentSize;
            Canvas.ForceUpdateCanvases();
        }

        private void RefreshReadingHint()
        {
            if (readingHint == null || !readingHint.gameObject.activeSelf) return;
            readingHint.text = readingScroll.verticalNormalizedPosition <= .01f ? "已读到底" : "滚动查看完整状态";
        }

        private static TextMeshProUGUI CreateLabel(string name, Transform parent, TextMeshProUGUI template, float size, Color color)
        {
            var rect = Child(parent, name, 0, 0, 0, 0);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = template.font; text.fontSharedMaterial = template.fontSharedMaterial;
            text.fontSize = size; text.color = color; text.richText = false; text.raycastTarget = false;
            return text;
        }
        private static RectTransform Child(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); Place(rect, x, y, width, height); return rect;
        }
        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        }
        private static void BuildScrollbar(RectTransform parent, ScrollRect scroll)
        {
            var rail = Child(parent, "AI reading scrollbar", 0, 0, 13, 0);
            rail.anchorMin = new Vector2(1f, 0f); rail.anchorMax = Vector2.one;
            rail.offsetMin = new Vector2(-16f, 5f); rail.offsetMax = new Vector2(-3f, -5f);
            rail.gameObject.AddComponent<Image>().color = Color.clear;
            var track = Child(rail, "Track", 0, 0, 4, 0);
            track.anchorMin = Vector2.zero; track.anchorMax = Vector2.one;
            track.offsetMin = new Vector2(9f, 0f); track.offsetMax = Vector2.zero;
            var trackImage = track.gameObject.AddComponent<Image>(); trackImage.color = Client.FightUI.Border; trackImage.raycastTarget = false;
            var handle = Child(rail, "Handle", 0, 0, 13, 0);
            handle.anchorMin = Vector2.zero; handle.anchorMax = Vector2.one;
            handle.offsetMin = handle.offsetMax = Vector2.zero;
            var thumb = Child(handle, "Thumb", 0, 0, 4, 0);
            thumb.anchorMin = Vector2.zero; thumb.anchorMax = Vector2.one;
            thumb.offsetMin = new Vector2(9f, 0f); thumb.offsetMax = Vector2.zero;
            var image = thumb.gameObject.AddComponent<Image>(); image.color = Client.FightUI.GoldBorder;
            var bar = rail.gameObject.AddComponent<Scrollbar>(); bar.direction = Scrollbar.Direction.BottomToTop;
            bar.handleRect = handle; bar.targetGraphic = image;
            scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }
    }
}
