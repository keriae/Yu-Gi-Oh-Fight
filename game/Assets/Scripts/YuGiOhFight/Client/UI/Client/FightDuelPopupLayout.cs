using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YuGiOhFight.Client.UI.Client
{
    // Rule prompts are measured around their contents. Native card slots,
    // response controls and scroll ownership retain their object identities.
    internal sealed class FightDuelPopupLayout
    {
        private readonly PopupBase popup;
        private readonly Dictionary<RectTransform, RectState> rects = new();
        private readonly Dictionary<Text, TextState> texts = new();
        private readonly Dictionary<Text, string> phaseCaptions = new();
        private readonly Dictionary<GameObject, bool> objects = new();
        private readonly float nativeHeight, nativeHideY;
        internal readonly Text TitleText;
        private bool applied;
        private bool optionsLaidOut;
        private bool optionsPopulated;
        private int optionsSignature;
        private bool cardsLaidOut;
        private int cardsSignature;
        private float titleHeight = 32;
        internal Vector2 Size { get; private set; }
        internal float HeadingHeight => titleHeight;
        internal bool Supported => popup is PopupDuelPosition || popup is PopupDuelPhase
            || popup is PopupDuelYesOrNo || popup is PopupDuelSelection
            || popup is PopupDuelSelectCard || popup is PopupDuelInput;

        internal FightDuelPopupLayout(PopupBase source)
        {
            popup = source; nativeHeight = source.height;
            nativeHideY = source is PopupDuel duel && source.window != null
                ? -540f - source.height * source.window.localScale.x - duel.extraHeight : source.hideY;
            TitleText = source.title;
            if (TitleText == null && source.window != null)
                foreach (var text in source.window.GetComponentsInChildren<Text>(true))
                    if (text.name == "#Text_Title") { TitleText = text; break; }
        }

        internal bool Apply(ScenePalette palette)
        {
            if (!Supported || popup.window == null) return false;
            applied = true;
            Remember(popup.window);
            // Native large-window scale is unnecessary for a two-button rule
            // prompt. Its former scale is restored when the theme is removed.
            float scale = Mathf.Min(1, rects[popup.window].Scale.x);
            popup.window.localScale = Vector3.one * scale;
            if (popup is PopupDuelPosition positions) Position(positions);
            else if (popup is PopupDuelPhase phases) Phase(phases);
            else if (popup is PopupDuelYesOrNo yesNo) YesNo(yesNo);
            else if (popup is PopupDuelSelection options) Options(options);
            else if (popup is PopupDuelSelectCard cards) SelectCards(cards);
            else if (popup is PopupDuelInput input) Input(input);
            Title();
            if (popup is PopupDuel duel && duel.btnHide != null)
            {
                // The native FieldView tween lowers the panel until its top
                // reaches the screen edge. Its original button remains the
                // visible return tab immediately above that edge.
                Place(duel.btnHide.transform as RectTransform, new Vector2(48, 36),
                    new Vector2(Size.x * .5f - 28, Size.y * .5f + 19));
                popup.height = popup.window.rect.center.y + Size.y * .5f;
                popup.hideY = -540f - popup.height * scale - duel.extraHeight;
            }
            return true;
        }

        private float MeasureTitle(float width)
        {
            if (TitleText == null) return titleHeight = 32;
            StyleTitle();
            Remember(TitleText.rectTransform);
            TitleText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            return titleHeight = Mathf.Max(30, TitleText.preferredHeight + 4);
        }

        private float SingleCardWidth(float width)
        {
            if (TitleText == null || string.IsNullOrEmpty(TitleText.text)) return width;
            StyleTitle();
            int paragraphStart = TitleText.text.LastIndexOf('\n') + 1;
            if (paragraphStart > 0 && paragraphStart < TitleText.text.Length)
            {
                string paragraph = TitleText.text.Substring(paragraphStart);
                if (!string.IsNullOrWhiteSpace(paragraph))
                {
                    var settings = TitleText.GetGenerationSettings(Vector2.zero);
                    float paragraphWidth = TitleText.cachedTextGeneratorForLayout.GetPreferredWidth(paragraph, settings)
                        / TitleText.pixelsPerUnit;
                    float required = Mathf.Ceil(paragraphWidth) + 68;
                    if (required <= 500) width = Mathf.Max(width, required);
                }
            }
            // Keep tagged titles on their native paragraph measurement.
            if (TitleText.supportRichText && TitleText.text.Contains("<") && TitleText.text.Contains(">")) return width;
            // Keep a short trailing paragraph whole, then check every native
            // wrapped line so the earlier question cannot leave punctuation alone.
            for (float candidate = Mathf.Ceil(width / 4) * 4; candidate <= 500; candidate += 4)
                if (TitleLinesFit(candidate)) return candidate;
            return width;
        }

        private bool TitleLinesFit(float width)
        {
            const string opening = "「『（【《〈〔［｛〖〘〚｟〝“‘([{";
            const string closing = "」』）】》〉〕］｝〗〙〛｠〞〟”’，。！？；：、…⋯,.!?;:)]}";
            var settings = TitleText.GetGenerationSettings(new Vector2(width - 64, 0));
            settings.updateBounds = true;
            var generator = TitleText.cachedTextGeneratorForLayout;
            if (!generator.Populate(TitleText.text, settings)) return false;
            var lines = generator.lines;
            if (lines.Count == 0) return false;
            string text = TitleText.text;
            for (int line = 0; line < lines.Count; line++)
            {
                int start = Mathf.Clamp(lines[line].startCharIdx, 0, text.Length);
                int end = line + 1 < lines.Count ? Mathf.Clamp(lines[line + 1].startCharIdx, start, text.Length) : text.Length;
                int first = start, last = end - 1;
                while (first <= last && char.IsWhiteSpace(text[first])) first++;
                while (last >= first && char.IsWhiteSpace(text[last])) last--;
                if (first <= last && (opening.IndexOf(text[last]) >= 0 || closing.IndexOf(text[first]) >= 0)) return false;
                bool punctuation = false, content = false;
                for (int character = start; character < end; character++)
                {
                    char value = text[character];
                    if (char.IsWhiteSpace(value)) continue;
                    if (!char.IsPunctuation(value)) { content = true; break; }
                    punctuation = true;
                }
                if (punctuation && !content) return false;
            }
            return true;
        }

        private void SelectCards(PopupDuelSelectCard source)
        {
            if (source.scrollView == null || source.scrollView.content == null) { Size = new Vector2(420, 360); return; }
            int count = source.cards != null ? source.cards.Count : source.monos.Count;
            int signature = count * 31 + (TitleText != null ? TitleText.text.GetHashCode() : 0);
            signature = signature * 31 + (source.btnFinish != null && source.btnFinish.gameObject.activeSelf ? 1 : 0);
            // Population is asynchronous. The rule prefab owns slot widths,
            // location groups, card overlays and every subsequent drag frame.
            if (cardsLaidOut && signature == cardsSignature) return;
            cardsLaidOut = true; cardsSignature = signature;
            float cardWidth = Mathf.Max(150, count * 150);
            float width = Mathf.Clamp(cardWidth + 56, 420, 956);
            if (count == 1) width = SingleCardWidth(width);
            float heading = MeasureTitle(width - 64);
            // The status footer ends 236 units below the content top. Keep
            // room for it when the native 20-high horizontal scrollbar shows.
            const float viewportHeight = 260;
            Size = new Vector2(width, heading + viewportHeight + 112);
            if (source.baseRect != null) Place(source.baseRect, Size, Vector2.zero);
            var scroll = source.scrollView;
            float horizontal = scroll.horizontalNormalizedPosition;
            Vector2 velocity = scroll.velocity;
            Place(scroll.transform as RectTransform, new Vector2(Mathf.Min(cardWidth, width - 56), viewportHeight),
                new Vector2(0, Size.y * .5f - 24 - heading - 8 - viewportHeight * .5f));
            // Keep the native 220-high content and 150-wide card step, including
            // the overlapping group backgrounds and all selection markers.
            Remember(scroll.content);
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cardWidth);
            scroll.horizontalNormalizedPosition = horizontal; scroll.velocity = velocity;
            Footer(source.btnConfirm, source.btnCancel, source.btnFinish, width, 44);
        }

        private void Input(PopupDuelInput source)
        {
            bool numeric = source.input != null && (source.input.contentType == InputField.ContentType.IntegerNumber
                || source.input.contentType == InputField.ContentType.DecimalNumber
                || source.input.characterValidation == InputField.CharacterValidation.Integer
                || source.input.characterValidation == InputField.CharacterValidation.Decimal);
            float width = numeric ? 380 : 500;
            float heading = MeasureTitle(width - 64);
            bool multiLine = source.input != null && source.input.lineType != InputField.LineType.SingleLine;
            float inputHeight = multiLine ? 104 : 52;
            Size = new Vector2(width, heading + inputHeight + 116);
            var baseRect = popup.window.Find("Base") as RectTransform;
            if (baseRect != null) Place(baseRect, Size, Vector2.zero);
            if (source.input != null)
            {
                Place(source.input.transform as RectTransform, new Vector2(width - 56, inputHeight),
                    new Vector2(0, Size.y * .5f - 24 - heading - 18 - inputHeight * .5f));
                if (source.input.textComponent != null)
                {
                    StyleText(source.input.textComponent, 22);
                    Remember(source.input.textComponent.rectTransform); FightUI.Stretch(source.input.textComponent.rectTransform);
                    source.input.textComponent.rectTransform.offsetMin = new Vector2(16, 6);
                    source.input.textComponent.rectTransform.offsetMax = new Vector2(-16, -6);
                }
                if (source.input.placeholder is Text placeholder)
                {
                    StyleText(placeholder, 22);
                    Remember(placeholder.rectTransform); FightUI.Stretch(placeholder.rectTransform);
                    placeholder.rectTransform.offsetMin = new Vector2(16, 6);
                    placeholder.rectTransform.offsetMax = new Vector2(-16, -6);
                }
            }
            Footer(source.btnConfirm, source.btnCancel, null, width, 44);
        }

        private void Footer(Button confirm, Button cancel, Button finish, float width, float height)
        {
            bool hasConfirm = confirm != null && confirm.gameObject.activeSelf;
            bool hasCancel = cancel != null && cancel.gameObject.activeSelf;
            float buttonWidth = hasConfirm && hasCancel ? Mathf.Min(240, (width - 68) * .5f) : Mathf.Min(240, width - 64);
            float y = -Size.y * .5f + 20 + height * .5f;
            if (hasConfirm && hasCancel)
            {
                bool left = rects.TryGetValue(confirm.transform as RectTransform, out var state)
                    ? state.Position.x < 0 : (confirm.transform as RectTransform).anchoredPosition.x < 0;
                Place(confirm.transform as RectTransform, new Vector2(buttonWidth, height), new Vector2((left ? -1 : 1) * (buttonWidth + 12) * .5f, y));
                Place(cancel.transform as RectTransform, new Vector2(buttonWidth, height), new Vector2((left ? 1 : -1) * (buttonWidth + 12) * .5f, y));
            }
            else if (hasConfirm) Place(confirm.transform as RectTransform, new Vector2(buttonWidth, height), new Vector2(0, y));
            else if (hasCancel) Place(cancel.transform as RectTransform, new Vector2(buttonWidth, height), new Vector2(0, y));
            if (finish != null) Place(finish.transform as RectTransform, new Vector2(Mathf.Min(240, width - 64), height), new Vector2(0, y));
            ButtonCaption(confirm, 21); ButtonCaption(cancel, 21); ButtonCaption(finish, 21);
        }

        private void Position(PopupDuelPosition source)
        {
            var cards = new List<RawImage>();
            void Add(RawImage image) { if (image != null && image.gameObject.activeSelf) cards.Add(image); }
            Add(source.positionAttack); Add(source.positionDefense); Add(source.positionDefenseDown);
            // Unused cards are Destroyed by InitializeSelections at frame end.
            if (source.count == 2 && cards.Count > 2)
            {
                cards.Clear();
                if (source.option1 == 1 || source.option1 == 2) Add(source.positionAttack);
                else Add(source.positionDefense);
                if (source.option2 == 4) Add(source.positionDefense);
                else Add(source.positionDefenseDown);
            }
            float width = cards.Count > 2 ? 540 : 400;
            float heading = MeasureTitle(width - 64);
            Size = new Vector2(width, Mathf.Max(258, heading + 218));
            float spacing = cards.Count > 2 ? 160 : 164;
            for (int index = 0; index < cards.Count; index++)
            {
                var card = cards[index].rectTransform;
                Place(card, new Vector2(90, 130), new Vector2((index - (cards.Count - 1) * .5f) * spacing,
                    Size.y * .5f - 22 - heading - 20 - 65));
                card.localScale = Vector3.one;
            }
        }

        private void Phase(PopupDuelPhase source)
        {
            const float width = 740;
            if (TitleText != null)
            {
                if (!phaseCaptions.ContainsKey(TitleText)) phaseCaptions.Add(TitleText, TitleText.text);
                TitleText.text = FightText.Get("选择阶段");
            }
            float heading = MeasureTitle(width - 48);
            Size = new Vector2(width, heading + 328);
            var buttons = new[] { source.main1, source.battle, source.main2, source.end };
            float buttonY = Size.y * .5f - heading - 142;
            for (int index = 0; index < buttons.Length; index++)
            {
                var button = buttons[index];
                if (button == null) continue;
                Place(button.transform as RectTransform, new Vector2(136, 136),
                    new Vector2((index - 1.5f) * 156, buttonY));
                ButtonCaption(button, 22);
                var caption = button.GetComponentInChildren<Text>();
                if (caption != null)
                {
                    if (!phaseCaptions.ContainsKey(caption)) phaseCaptions.Add(caption, caption.text);
                    // The original phase icon remains the central glyph.
                    // The skin places the localized phase name below the dial.
                    caption.text = "";
                    caption.supportRichText = false;
                    caption.alignment = TextAnchor.MiddleCenter;
                    caption.rectTransform.offsetMin = new Vector2(18, 28);
                    caption.rectTransform.offsetMax = new Vector2(-18, -28);
                    caption.horizontalOverflow = HorizontalWrapMode.Overflow;
                    caption.verticalOverflow = VerticalWrapMode.Overflow;
                    caption.fontStyle = FontStyle.Bold;
                    caption.lineSpacing = 1;
                }
                var group = button.transform.parent;
                if (group != null)
                {
                    var draw = group.Find("Draw"); if (draw != null) Hide(draw.gameObject);
                    var standby = group.Find("Standby"); if (standby != null) Hide(standby.gameObject);
                }
            }
            if (source.btnCancel != null)
            {
                Place(source.btnCancel.transform as RectTransform, new Vector2(160, 44),
                    new Vector2(0, -Size.y * .5f + 42));
                ButtonCaption(source.btnCancel, 20);
            }
        }

        internal static bool IsCurrentPhase(PopupDuelPhase source, Selectable control)
        {
            if (source == null || source.selections == null || source.selections.Count == 0) return false;
            string current = source.selections[0];
            return control == source.main1 && current == MDPro3.Duel.YGOSharp.DuelPhase.Main1.ToString()
                || control == source.main2 && current == MDPro3.Duel.YGOSharp.DuelPhase.Main2.ToString()
                || control == source.end && current == MDPro3.Duel.YGOSharp.DuelPhase.End.ToString()
                || control == source.battle && current != null && (current.StartsWith("Battle", StringComparison.Ordinal)
                    || current.StartsWith("Damage", StringComparison.Ordinal));
        }

        internal static int PhaseIndex(PopupDuelPhase source, Selectable control)
            => source == null ? -1 : control == source.main1 ? 0 : control == source.battle ? 1
                : control == source.main2 ? 2 : control == source.end ? 3 : -1;

        private void YesNo(PopupDuelYesOrNo source)
        {
            float width = 440;
            float bodyHeight = 54;
            if (source.description != null)
            {
                StyleText(source.description, 22);
                source.description.horizontalOverflow = HorizontalWrapMode.Wrap;
                source.description.verticalOverflow = VerticalWrapMode.Overflow;
                if (source.description.preferredWidth > 510) width = 540;
                Remember(source.description.rectTransform);
                source.description.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width - 56);
                bodyHeight = Mathf.Max(42, source.description.preferredHeight + 6);
            }
            float heading = MeasureTitle(width - 64);
            Size = new Vector2(width, Mathf.Max(188, 22 + heading + 16 + bodyHeight + 20 + 44 + 20));
            if (source.description != null)
                Place(source.description.rectTransform, new Vector2(width - 56, bodyHeight),
                    new Vector2(0, Size.y * .5f - 22 - heading - 16 - bodyHeight * .5f));
            Footer(source.btnConfirm, source.btnCancel, null, width, 44);
        }

        private void Options(PopupDuelSelection source)
        {
            if (source.scrollRect == null || source.scrollRect.content == null) { Size = new Vector2(440, 196); return; }
            int signature = TitleText != null ? TitleText.text.GetHashCode() : 17;
            unchecked
            {
                foreach (Transform child in source.scrollRect.content)
                {
                    if (!child.gameObject.activeSelf) continue;
                    var button = child.GetComponentInChildren<Button>();
                    if (button == null) continue;
                    var label = button.GetComponentInChildren<Text>();
                    signature = signature * 31 + child.GetInstanceID();
                    signature = signature * 31 + (label != null ? label.text.GetHashCode() : 0);
                }
            }
            // Native ScrollRect owns every drag/inertia frame. Reflow only
            // when rows or localized rule text actually change.
            if (optionsLaidOut && optionsSignature == signature) return;
            optionsLaidOut = true; optionsSignature = signature;
            const float width = 500;
            float heading = MeasureTitle(width - 64);
            float scrollPosition = source.scrollRect.verticalNormalizedPosition;
            Vector2 velocity = source.scrollRect.velocity;
            var previousViewport = source.scrollRect.viewport != null ? source.scrollRect.viewport
                : source.scrollRect.transform as RectTransform;
            bool hadScrollRange = previousViewport != null
                && source.scrollRect.content.rect.height > previousViewport.rect.height + 1;
            var rows = new List<(RectTransform Rect, Button Button, float Height)>();
            float total = 0;
            foreach (Transform child in source.scrollRect.content)
            {
                if (!child.gameObject.activeSelf || !(child is RectTransform row)) continue;
                var button = child.GetComponentInChildren<Button>();
                if (button == null) continue;
                float height = 44;
                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    StyleText(label, 21); label.horizontalOverflow = HorizontalWrapMode.Wrap;
                    label.verticalOverflow = VerticalWrapMode.Overflow;
                    Remember(label.rectTransform);
                    label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width - 90);
                    height = Mathf.Max(44, label.preferredHeight + 18);
                }
                rows.Add((row, button, height)); total += height + 8;
            }
            total = Mathf.Max(44, total - 8);
            float viewport = Mathf.Min(320, total);
            Size = new Vector2(width, 22 + heading + 16 + viewport + 22);
            if (source.baseRect != null) Place(source.baseRect, Size, Vector2.zero);
            Place(source.scrollRect.transform as RectTransform, new Vector2(width - 48, viewport),
                new Vector2(0, Size.y * .5f - 22 - heading - 16 - viewport * .5f));
            var content = source.scrollRect.content; Remember(content);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = new Vector2(0, total);
            content.anchoredPosition = new Vector2(0, content.anchoredPosition.y);
            if (source.scrollRect.viewport != null && source.scrollRect.viewport != source.scrollRect.transform)
            {
                Remember(source.scrollRect.viewport); FightUI.Stretch(source.scrollRect.viewport);
                source.scrollRect.viewport.offsetMax = new Vector2(-8, 0);
            }
            float y = 0;
            foreach (var row in rows)
            {
                Remember(row.Rect);
                row.Rect.anchorMin = new Vector2(0, 1); row.Rect.anchorMax = new Vector2(1, 1); row.Rect.pivot = new Vector2(.5f, 1);
                row.Rect.sizeDelta = new Vector2(0, row.Height); row.Rect.anchoredPosition = new Vector2(0, -y);
                var button = row.Button.transform as RectTransform; Remember(button); FightUI.Stretch(button);
                var label = row.Button.GetComponentInChildren<Text>();
                if (label != null) { Remember(label.rectTransform); FightUI.Stretch(label.rectTransform); label.rectTransform.offsetMin = new Vector2(14, 4); label.rectTransform.offsetMax = new Vector2(-14, -4); }
                y += row.Height + 8;
            }
            if (source.scrollRect.verticalScrollbar != null)
            {
                var bar = source.scrollRect.verticalScrollbar.transform as RectTransform; Remember(bar);
                bar.anchorMin = new Vector2(1, 0); bar.anchorMax = Vector2.one; bar.pivot = new Vector2(1, .5f);
                bar.offsetMin = new Vector2(-5, 0); bar.offsetMax = Vector2.zero;
            }
            // A native list that fitted its previous viewport reports zero,
            // even after InitializeSelections requested the top. Once this
            // compact viewport becomes scrollable, that zero means the bottom.
            bool startAtTop = !optionsPopulated || !hadScrollRange;
            source.scrollRect.verticalNormalizedPosition = startAtTop ? 1 : scrollPosition;
            source.scrollRect.velocity = startAtTop ? Vector2.zero : velocity;
            if (rows.Count > 0) optionsPopulated = true;
        }

        private void Title()
        {
            if (TitleText == null) return;
            StyleTitle();
            bool phase = popup is PopupDuelPhase;
            float height = titleHeight;
            Place(TitleText.rectTransform, new Vector2(Size.x - (phase ? 48 : 64), height),
                new Vector2(0, Size.y * .5f - (phase ? 34 : 22) - height * .5f));
        }

        private void StyleTitle()
        {
            StyleText(TitleText, 24);
            // Cached card/option layouts must measure the same weight that
            // PopupSkin renders, before a long title chooses its line breaks.
            TitleText.fontStyle = FontStyle.Bold;
            TitleText.lineSpacing = 1;
            TitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            TitleText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void ButtonCaption(Button button, int size)
        {
            if (button == null) return;
            var text = button.GetComponentInChildren<Text>();
            if (text == null) return;
            StyleText(text, size); Remember(text.rectTransform); FightUI.Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(10, 2); text.rectTransform.offsetMax = new Vector2(-10, -2);
        }

        private void StyleText(Text text, int size)
        {
            if (!texts.ContainsKey(text)) texts.Add(text, new TextState(text));
            text.fontSize = size; text.resizeTextForBestFit = false;
        }

        private void Place(RectTransform rect, Vector2 size, Vector2 centre)
        {
            if (rect == null || rect.parent == null) return;
            Remember(rect);
            Vector2 local = rect.parent.InverseTransformPoint(popup.window.TransformPoint(popup.window.rect.center + centre));
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            var parent = rect.parent as RectTransform;
            rect.anchoredPosition = local - (parent != null ? parent.rect.center : Vector2.zero);
            rect.sizeDelta = size;
        }
        private void Remember(RectTransform rect) { if (rect != null && !rects.ContainsKey(rect)) rects.Add(rect, new RectState(rect)); }
        private void Hide(GameObject item) { if (!objects.ContainsKey(item)) objects.Add(item, item.activeSelf); item.SetActive(false); }

        internal void Restore()
        {
            if (!applied) return;
            foreach (var pair in rects) if (pair.Key != null) pair.Value.Restore(pair.Key, pair.Key == popup.window);
            foreach (var pair in texts) if (pair.Key != null) pair.Value.Restore(pair.Key);
            foreach (var pair in phaseCaptions) if (pair.Key != null) pair.Key.text = pair.Value;
            foreach (var pair in objects) if (pair.Key != null) pair.Key.SetActive(pair.Value);
            popup.height = nativeHeight; popup.hideY = nativeHideY;
            var program = Runtime.Program.instance;
            if (popup is PopupDuel duel && duel.FieldViewHidden && popup.window != null
                && program != null && program.ocgcore != null && program.ocgcore.currentPopup == duel)
            {
                // Only a live FieldView transition has no completion callback.
                // Its original geometry needs the original return-tab target;
                // closing/showing/dragging transitions otherwise keep position.
                popup.window.DOKill(false);
                var position = popup.window.anchoredPosition;
                position.y = -540f - nativeHeight * popup.window.localScale.x;
                popup.window.anchoredPosition = position;
            }
            optionsLaidOut = cardsLaidOut = false;
            optionsPopulated = false;
            applied = false;
        }

        private sealed class RectState
        {
            internal readonly Vector3 Scale;
            internal Vector2 Position => position;
            private readonly Vector2 min, max, pivot, size, position;
            private readonly Quaternion rotation;
            internal RectState(RectTransform rect) { min = rect.anchorMin; max = rect.anchorMax; pivot = rect.pivot; size = rect.sizeDelta; position = rect.anchoredPosition; Scale = rect.localScale; rotation = rect.localRotation; }
            internal void Restore(RectTransform rect, bool keepPosition = false)
            {
                var currentPosition = rect.anchoredPosition;
                rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot; rect.sizeDelta = size;
                rect.anchoredPosition = keepPosition ? currentPosition : position; rect.localScale = Scale; rect.localRotation = rotation;
            }
        }
        private sealed class TextState
        {
            private readonly int size;
            private readonly bool fit;
            private readonly FontStyle style;
            private readonly float spacing;
            private readonly HorizontalWrapMode horizontal;
            private readonly VerticalWrapMode vertical;
            private readonly TextAnchor alignment;
            private readonly bool rich;
            internal TextState(Text text) { size = text.fontSize; fit = text.resizeTextForBestFit; style = text.fontStyle; spacing = text.lineSpacing; horizontal = text.horizontalOverflow; vertical = text.verticalOverflow; alignment = text.alignment; rich = text.supportRichText; }
            internal void Restore(Text text) { text.fontSize = size; text.resizeTextForBestFit = fit; text.fontStyle = style; text.lineSpacing = spacing; text.horizontalOverflow = horizontal; text.verticalOverflow = vertical; text.alignment = alignment; text.supportRichText = rich; }
        }
    }

    // The simple TMP popup family uses full-height flex roots. Scene prompts
    // keep those transition roots, while their original contents are measured
    // into a compact title/body/footer. Virtual selection lists stay native.
    internal sealed class FightDuelNativePopupLayout
    {
        private readonly Popup.Popup popup;
        private readonly RectTransform root;
        private readonly TextMeshProUGUI title, body;
        private readonly RectTransform titleGroup, bodyGroup, footer;
        private readonly TMP_InputField input;
        private readonly ScrollRect scroll;
        private readonly Button decide, cancel;
        private readonly GameObject icon;
        private readonly Dictionary<RectTransform, NativeRect> rects = new();
        private readonly Dictionary<Behaviour, bool> layouts = new();
        private bool applied;
        private int signature;
        internal Vector2 Size { get; private set; }
        internal bool Supported => popup is Popup.PopupYesOrNo || popup is Popup.PopupInput
            || popup is Popup.PopupConfirm || popup is Popup.PopupText || popup is Popup.PopupSelection;

        internal FightDuelNativePopupLayout(Popup.Popup source, RectTransform content, Presentation.Elements.ElementObjectManager manager)
        {
            popup = source; root = content;
            if (!Supported) return;
            title = manager.GetElement<TextMeshProUGUI>("TitleText");
            body = source is Popup.PopupSelection ? manager.GetElement<TextMeshProUGUI>("MessageText")
                : source is Popup.PopupText ? manager.GetElement<TextMeshProUGUI>("Text")
                : source is Popup.PopupInput ? null : manager.GetElement<TextMeshProUGUI>("FrameText");
            input = source is Popup.PopupInput prompt ? prompt.input : null;
            scroll = source is Popup.PopupConfirm ? manager.GetElement<ScrollRect>("ScrollView")
                : source is Popup.PopupText || source is Popup.PopupSelection ? manager.GetElement<ScrollRect>("EntryButtonsScrollView") : null;
            titleGroup = title != null ? title.transform.parent as RectTransform : null;
            bodyGroup = source is Popup.PopupSelection ? manager.GetElement<RectTransform>("MessageArea")
                : source is Popup.PopupYesOrNo && body != null ? body.transform.parent as RectTransform
                : source is Popup.PopupConfirm && scroll != null ? scroll.transform.parent as RectTransform : null;
            decide = manager.GetElement<Button>("DecideButton"); cancel = manager.GetElement<Button>("CancelButton");
            footer = decide != null ? decide.transform.parent as RectTransform : cancel != null ? cancel.transform.parent as RectTransform : null;
            icon = source is Popup.PopupSelection ? null : manager.GetElement("Icon");
            foreach (var rect in content.GetComponentsInChildren<RectTransform>(true)) rects.Add(rect, new NativeRect(rect));
            foreach (var layout in content.GetComponentsInChildren<LayoutGroup>(true)) layouts.Add(layout, layout.enabled);
            foreach (var fitter in content.GetComponentsInChildren<ContentSizeFitter>(true)) layouts.Add(fitter, fitter.enabled);
        }

        internal void Apply()
        {
            if (!Supported || root == null) return;
            int next;
            unchecked
            {
                next = (title != null ? title.text.GetHashCode() : 0) * 31 + (body != null ? body.text.GetHashCode() : 0);
                next = next * 31 + (decide != null && decide.gameObject.activeSelf ? 1 : 0);
                next = next * 31 + (cancel != null && cancel.gameObject.activeSelf ? 1 : 0);
                next = next * 31 + (input != null ? (int)input.lineType * 31 + (int)input.contentType : 0);
                if (popup is Popup.PopupSelection selection)
                    next = next * 31 + (selection.args != null ? selection.args.Count : 0);
            }
            if (applied && signature == next) return;
            applied = true; signature = next;
            foreach (var pair in layouts)
                if (pair.Key != null && !(popup is Popup.PopupSelection && scroll != null && pair.Key.transform.IsChildOf(scroll.transform)))
                    pair.Key.enabled = false;
            if (popup is Popup.PopupSelection choices) { Selection(choices); return; }
            bool numeric = input != null && (input.contentType == TMP_InputField.ContentType.IntegerNumber
                || input.contentType == TMP_InputField.ContentType.DecimalNumber
                || input.characterValidation == TMP_InputField.CharacterValidation.Integer
                || input.characterValidation == TMP_InputField.CharacterValidation.Decimal);
            float width = scroll != null ? 620 : numeric ? 380 : 500;
            float titleHeight = Measure(title, width - 56 - (icon != null && icon.activeSelf ? 42 : 0), 24, 30);
            float bodyHeight = input != null ? input.lineType == TMP_InputField.LineType.SingleLine ? 52 : 104
                : Measure(body, width - 72, 22, 42);
            float viewportHeight = scroll != null ? Mathf.Min(340, bodyHeight) : bodyHeight;
            const float footerHeight = 44;
            Size = new Vector2(width, 24 + titleHeight + 18 + viewportHeight + 22 + footerHeight + 20);
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            float top = Size.y * .5f - 24;
            Place(titleGroup, new Vector2(width - 56, titleHeight), new Vector2(0, top - titleHeight * .5f));
            if (title != null)
            {
                Stretch(title.rectTransform);
                if (icon != null && icon.activeSelf)
                {
                    title.rectTransform.offsetMin = new Vector2(42, 0);
                    Place(icon.transform as RectTransform, new Vector2(28, 28), new Vector2(-width * .5f + 42, top - titleHeight * .5f));
                }
            }
            float centre = top - titleHeight - 18 - viewportHeight * .5f;
            if (input != null)
            {
                Place(input.transform as RectTransform, new Vector2(width - 56, bodyHeight), new Vector2(0, centre));
                if (input.textViewport != null)
                {
                    Stretch(input.textViewport); input.textViewport.offsetMin = new Vector2(16, 6); input.textViewport.offsetMax = new Vector2(-16, -6);
                }
                if (input.textComponent != null) Stretch(input.textComponent.rectTransform);
                if (input.placeholder != null) Stretch(input.placeholder.rectTransform);
            }
            else if (scroll != null) Scroll(width - 56, viewportHeight, bodyHeight, centre);
            else if (body != null)
            {
                Place(bodyGroup, new Vector2(width - 56, bodyHeight), new Vector2(0, centre));
                Stretch(body.rectTransform);
            }
            float buttonY = -Size.y * .5f + 20 + footerHeight * .5f;
            Place(footer, new Vector2(width - 56, footerHeight), new Vector2(0, buttonY));
            bool pairActive = decide != null && cancel != null && decide.gameObject.activeSelf && cancel.gameObject.activeSelf;
            float buttonWidth = pairActive ? (width - 68) * .5f : 220;
            bool left = pairActive && decide.transform.GetSiblingIndex() < cancel.transform.GetSiblingIndex();
            Place(decide?.transform as RectTransform, new Vector2(buttonWidth, footerHeight),
                new Vector2(pairActive ? (left ? -1 : 1) * (buttonWidth + 12) * .5f : 0, buttonY));
            Place(cancel?.transform as RectTransform, new Vector2(buttonWidth, footerHeight),
                new Vector2(pairActive ? (left ? 1 : -1) * (buttonWidth + 12) * .5f : 0, buttonY));
            Caption(decide); Caption(cancel);
        }

        private void Selection(Popup.PopupSelection source)
        {
            // The virtual pool still owns its native row width, stride, content
            // height and drag frames. Only the title, viewport and footer move.
            bool mobile = PropertyOverride.PropertyOverrider.NeedMobileLayout();
            float width = mobile ? 1012 : 820;
            float heading = Measure(title, width - 56, 24, 30);
            bool hasMessage = bodyGroup != null && bodyGroup.gameObject.activeSelf && body != null && !string.IsNullOrEmpty(body.text);
            float messageHeight = hasMessage ? Measure(body, width - 56, 22, 30) : 0;
            int count = source.args != null ? Mathf.Max(0, source.args.Count - 2) : 0;
            float listHeight = count * (mobile ? 118 : 88) + (mobile ? 74 : 44);
            float viewportHeight = Mathf.Clamp(listHeight, mobile ? 192 : 132, 360);
            bool hasFooter = cancel != null && cancel.gameObject.activeSelf;
            float footerHeight = hasFooter ? 44 : 0;
            Size = new Vector2(width, 22 + heading + (hasMessage ? 14 + messageHeight : 0)
                + 16 + viewportHeight + (hasFooter ? 18 + footerHeight : 0) + 22);
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            float top = Size.y * .5f - 22;
            Place(titleGroup, new Vector2(width - 56, heading), new Vector2(0, top - heading * .5f));
            if (title != null) Stretch(title.rectTransform);
            top -= heading;
            if (hasMessage)
            {
                top -= 14;
                Place(bodyGroup, new Vector2(width - 56, messageHeight), new Vector2(0, top - messageHeight * .5f));
                if (body.transform.parent != bodyGroup && body.transform.parent is RectTransform messageContent) Stretch(messageContent);
                Stretch(body.rectTransform); top -= messageHeight;
            }
            top -= 16;
            if (scroll != null)
            {
                float vertical = scroll.verticalNormalizedPosition;
                Vector2 velocity = scroll.velocity;
                Place(scroll.transform as RectTransform, new Vector2(width - 56, viewportHeight), new Vector2(0, top - viewportHeight * .5f));
                if (scroll.viewport != null) { Stretch(scroll.viewport); scroll.viewport.offsetMax = new Vector2(-12, 0); }
                if (scroll.verticalScrollbar != null)
                {
                    var bar = scroll.verticalScrollbar.transform as RectTransform;
                    bar.anchorMin = new Vector2(1, 0); bar.anchorMax = Vector2.one; bar.pivot = new Vector2(1, .5f);
                    bar.offsetMin = new Vector2(-4, 6); bar.offsetMax = new Vector2(0, -6);
                }
                scroll.verticalNormalizedPosition = vertical; scroll.velocity = velocity;
            }
            if (hasFooter)
            {
                float y = -Size.y * .5f + 22 + footerHeight * .5f;
                Place(footer, new Vector2(width - 56, footerHeight), new Vector2(0, y));
                Place(cancel.transform as RectTransform, new Vector2(220, footerHeight), new Vector2(0, y));
                Caption(cancel);
            }
        }

        private static float Measure(TextMeshProUGUI text, float width, float size, float minimum)
        {
            if (text == null) return minimum;
            var clientFont = Runtime.Program.instance.ui_.tmpFont;
            if (clientFont != null) { text.font = clientFont; text.fontSharedMaterial = clientFont.material; }
            text.fontStyle = size >= 24 ? FontStyles.Bold : FontStyles.Normal;
            text.characterSpacing = size >= 24 ? .6f : 0;
            text.fontSize = size; text.enableAutoSizing = false; text.enableWordWrapping = true; text.overflowMode = TextOverflowModes.Overflow;
            return Mathf.Max(minimum, text.GetPreferredValues(text.text, width, float.PositiveInfinity).y + 4);
        }
        private void Scroll(float width, float viewportHeight, float textHeight, float centre)
        {
            float vertical = scroll.verticalNormalizedPosition;
            Vector2 velocity = scroll.velocity;
            if (bodyGroup != null) Place(bodyGroup, new Vector2(width, viewportHeight), new Vector2(0, centre));
            Place(scroll.transform as RectTransform, new Vector2(width, viewportHeight), new Vector2(0, centre));
            if (scroll.viewport != null) { Stretch(scroll.viewport); scroll.viewport.offsetMax = new Vector2(-14, 0); }
            if (scroll.content != null)
            {
                var content = scroll.content;
                content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
                content.sizeDelta = new Vector2(0, textHeight); content.anchoredPosition = new Vector2(0, content.anchoredPosition.y);
                if (body != null && body.rectTransform != content) Stretch(body.rectTransform);
            }
            if (scroll.verticalScrollbar != null)
            {
                var bar = scroll.verticalScrollbar.transform as RectTransform;
                bar.anchorMin = new Vector2(1, 0); bar.anchorMax = Vector2.one; bar.pivot = new Vector2(1, .5f);
                bar.offsetMin = new Vector2(-5, 0); bar.offsetMax = Vector2.zero;
            }
            scroll.verticalNormalizedPosition = vertical; scroll.velocity = velocity;
        }
        private static void Caption(Button button)
        {
            if (button == null) return;
            foreach (var caption in button.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                Stretch(caption.rectTransform); caption.rectTransform.offsetMin = new Vector2(12, 2); caption.rectTransform.offsetMax = new Vector2(-12, -2);
            }
        }
        private static void Stretch(RectTransform rect) { if (rect != null) FightUI.Stretch(rect); }
        private void Place(RectTransform rect, Vector2 size, Vector2 centre)
        {
            if (rect == null || rect.parent == null) return;
            Vector2 local = rect.parent.InverseTransformPoint(root.TransformPoint(root.rect.center + centre));
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            var parent = rect.parent as RectTransform;
            rect.anchoredPosition = local - (parent != null ? parent.rect.center : Vector2.zero); rect.sizeDelta = size;
        }
        internal void Restore()
        {
            if (!applied) return;
            float vertical = scroll != null ? scroll.verticalNormalizedPosition : 1;
            Vector2 velocity = scroll != null ? scroll.velocity : Vector2.zero;
            foreach (var pair in rects) if (pair.Key != null) pair.Value.Restore(pair.Key, pair.Key == root || scroll != null && pair.Key == scroll.content);
            foreach (var pair in layouts) if (pair.Key != null) pair.Key.enabled = pair.Value;
            if (scroll != null) { scroll.verticalNormalizedPosition = vertical; scroll.velocity = velocity; }
            applied = false;
        }
        private sealed class NativeRect
        {
            private readonly Vector2 min, max, pivot, position, size;
            internal NativeRect(RectTransform rect) { min = rect.anchorMin; max = rect.anchorMax; pivot = rect.pivot; position = rect.anchoredPosition; size = rect.sizeDelta; }
            internal void Restore(RectTransform rect, bool preservePosition)
            {
                var current = rect.anchoredPosition; rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot; rect.sizeDelta = size;
                rect.anchoredPosition = preservePosition ? current : position;
            }
        }
    }
}
