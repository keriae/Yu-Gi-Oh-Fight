using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using MDPro3.Utility;

namespace MDPro3.UI.Client
{
    internal sealed class FightCardSlotFocus : MaskableGraphic
    {
        private Vector3 previousScale;
        protected override void Awake() { base.Awake(); raycastTarget = false; }
        private void LateUpdate()
        {
            if (previousScale == transform.lossyScale) return;
            previousScale = transform.lossyScale; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            float scale = Mathf.Max(.01f, Mathf.Min(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y)));
            float stroke = 2f / scale, inset = 2f / scale, cut = 4f / scale;
            var rect = rectTransform.rect;
            float left = rect.xMin + inset, right = rect.xMax - inset, bottom = rect.yMin + inset, top = rect.yMax - inset;
            var points = new[] { new Vector2(left + cut, bottom), new Vector2(right - cut, bottom),
                new Vector2(right, bottom + cut), new Vector2(right, top - cut), new Vector2(right - cut, top),
                new Vector2(left + cut, top), new Vector2(left, top - cut), new Vector2(left, bottom + cut) };
            for (int index = 0; index < points.Length; index++)
                FightFrame.Segment(vertices, points[index], points[(index + 1) % points.Length], stroke, FightUI.Cyan);
        }
    }

    // Real Unity controls. Coordinates are local, measured from the top left.
    internal static class FightUI
    {
        public static readonly Color Background = Hex("060D14"), PanelColor = Hex("0B1821"), Raised = Hex("142631"),
            Border = Hex("2B414D"), GoldBorder = Hex("756345"), Gold = Hex("CDB681"), Active = Hex("EAD09A"),
            Foreground = Hex("E1DDD2"), Muted = Hex("A6B1B8"), Cyan = Hex("62D6E8"), CyanText = Hex("D2F7FF"),
            ActionFill = Hex("0B3445"), Danger = Hex("E29387");
        public static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var c); return c; }
        public static string BalancedLines(string value, int targetCharacters)
        {
            if (string.IsNullOrEmpty(value) || value.IndexOfAny(new[] { '\n', '\r', '\u2028', '\u2029' }) >= 0) return value;
            targetCharacters = Math.Max(1, targetCharacters);
            if (value.Length <= targetCharacters) return value;
            int[] elements = System.Globalization.StringInfo.ParseCombiningCharacters(value);
            var boundaries = new List<(int end, int next, int before, int after)>();
            bool previousCjk = false; int longest = 0, segmentStart = 0, segmentFirst = 0;
            for (int element = 0; element < elements.Length; element++)
            {
                int index = elements[element], next = element + 1 < elements.Length ? elements[element + 1] : value.Length;
                char character = value[index];
                bool nonbreaking = character == '\u00A0' || character == '\u2007' || character == '\u202F' || character == '\u2060' || character == '\u200C' || character == '\u200D';
                if (char.IsWhiteSpace(character) && !nonbreaking)
                {
                    int firstSpace = element;
                    while (element + 1 < elements.Length && char.IsWhiteSpace(value[elements[element + 1]])
                        && value[elements[element + 1]] != '\u00A0' && value[elements[element + 1]] != '\u2007' && value[elements[element + 1]] != '\u202F')
                    {
                        element++; next = element + 1 < elements.Length ? elements[element + 1] : value.Length;
                    }
                    if (index > segmentStart && next < value.Length)
                    { boundaries.Add((index, next, firstSpace, element + 1)); longest = Math.Max(longest, firstSpace - segmentFirst); segmentStart = next; segmentFirst = element + 1; }
                    previousCjk = false; continue;
                }
                int scalar = char.IsHighSurrogate(character) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1])
                    ? char.ConvertToUtf32(character, value[index + 1]) : character;
                bool cjk = (scalar >= 0x2E80 && scalar <= 0x9FFF) || (scalar >= 0xAC00 && scalar <= 0xD7FF)
                    || (scalar >= 0xF900 && scalar <= 0xFAFF) || (scalar >= 0x20000 && scalar <= 0x323AF);
                // Keep whole alphabetic words, nonbreaking spans and Unicode
                // text elements intact, including Latin prefixes in CJK names.
                if (index > segmentStart && (previousCjk || cjk) && !nonbreaking
                    && value[index - 1] != '\u00A0' && value[index - 1] != '\u2007' && value[index - 1] != '\u202F' && value[index - 1] != '\u2060' && value[index - 1] != '\u200C' && value[index - 1] != '\u200D')
                { boundaries.Add((index, index, element, element)); longest = Math.Max(longest, element - segmentFirst); segmentStart = index; segmentFirst = element; }
                previousCjk = cjk;
            }
            if (boundaries.Count == 0) return value;
            longest = Math.Max(longest, elements.Length - segmentFirst);
            // A long printed word sets the useful line width instead of
            // creating extra, fragmentary lines around short connecting words.
            float lineWidth = Math.Max(targetCharacters, longest);
            int lines = Math.Min(boundaries.Count + 1, Math.Max(2, longest > targetCharacters
                ? (int)Math.Round(elements.Length / lineWidth, MidpointRounding.AwayFromZero) : (int)Math.Ceiling(elements.Length / lineWidth)));
            var result = new System.Text.StringBuilder(); int start = 0, startWeight = 0, firstBoundary = 0;
            float maximumLineWidth = lineWidth + 2;
            while (lines > 1 || elements.Length - startWeight > maximumLineWidth)
            {
                // Use another legal line when a balanced grouping would make
                // a long word share an overwide line with its neighbour.
                lines = Math.Min(boundaries.Count - firstBoundary + 1, Math.Max(lines,
                    (int)Math.Ceiling((elements.Length - startWeight) / maximumLineWidth)));
                float target = (elements.Length - startWeight) / (float)lines;
                int best = firstBoundary;
                for (int candidate = firstBoundary + 1; candidate <= boundaries.Count - lines + 1; candidate++)
                {
                    if (boundaries[candidate].before - startWeight > maximumLineWidth
                        || Math.Abs(boundaries[candidate].before - startWeight - target) > Math.Abs(boundaries[best].before - startWeight - target)) break;
                    best = candidate;
                }
                result.Append(value, start, boundaries[best].end - start).Append('\n');
                start = boundaries[best].next; startWeight = boundaries[best].after; firstBoundary = best + 1; lines--;
            }
            return result.Append(value, start, value.Length - start).ToString();
        }
        public static FightTooltip Hint(GameObject target, string message)
        {
            var hint = target.GetComponent<FightTooltip>() ?? target.AddComponent<FightTooltip>();
            hint.Hint = message; hint.LocalizeHint = true; return hint;
        }
        public static FightTooltip RawHint(GameObject target, string message)
        {
            var hint = target.GetComponent<FightTooltip>() ?? target.AddComponent<FightTooltip>();
            hint.Hint = message; hint.LocalizeHint = false; return hint;
        }
        public static T FocusKey<T>(T control, string key) where T : Selectable
        {
            var identity = control.GetComponent<FightFocusKey>() ?? control.gameObject.AddComponent<FightFocusKey>();
            identity.Key = key; return control;
        }
        public static T KeyboardProxy<T>(T control, Selectable keyboardTarget) where T : Selectable
        {
            var identity = control.GetComponent<FightFocusKey>() ?? control.gameObject.AddComponent<FightFocusKey>();
            identity.KeyboardTarget = keyboardTarget == control ? null : keyboardTarget;
            return control;
        }
        public static void RevealControl(Selectable selected)
        {
            foreach (var scroll in selected.GetComponentsInParent<ScrollRect>())
            {
                if (scroll.content == null || scroll.viewport == null || !selected.transform.IsChildOf(scroll.content)) continue;
                var corners = new Vector3[4]; selected.GetComponent<RectTransform>().GetWorldCorners(corners);
                if (scroll.vertical)
                {
                    float range = scroll.content.rect.height - scroll.viewport.rect.height;
                    if (range > 0)
                    {
                        float top = scroll.content.rect.yMax - scroll.content.InverseTransformPoint(corners[1]).y;
                        float bottom = scroll.content.rect.yMax - scroll.content.InverseTransformPoint(corners[0]).y;
                        float offset = (1 - scroll.verticalNormalizedPosition) * range;
                        if (top < offset + 8) offset = top - 8;
                        else if (bottom > offset + scroll.viewport.rect.height - 8) offset = bottom - scroll.viewport.rect.height + 8;
                        scroll.StopMovement(); scroll.verticalNormalizedPosition = 1 - Mathf.Clamp(offset, 0, range) / range;
                    }
                }
                if (scroll.horizontal)
                {
                    float range = scroll.content.rect.width - scroll.viewport.rect.width;
                    if (range > 0)
                    {
                        float left = scroll.content.InverseTransformPoint(corners[0]).x - scroll.content.rect.xMin;
                        float right = scroll.content.InverseTransformPoint(corners[3]).x - scroll.content.rect.xMin;
                        float offset = scroll.horizontalNormalizedPosition * range;
                        if (left < offset + 8) offset = left - 8;
                        else if (right > offset + scroll.viewport.rect.width - 8) offset = right - scroll.viewport.rect.width + 8;
                        scroll.StopMovement(); scroll.horizontalNormalizedPosition = Mathf.Clamp(offset, 0, range) / range;
                    }
                }
            }
        }
        public static RectTransform Node(Transform parent, string name, float x, float y, float w, float h)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false); Place(rt, x, y, w, h); return rt;
        }
        public static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
        }
        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        public static RectTransform Panel(Transform parent, float x, float y, float w, float h, Color? fill = null, bool border = true, string name = "Panel")
        {
            var rt = Node(parent, name, x, y, w, h);
            var image = rt.gameObject.AddComponent<Image>(); image.color = fill ?? PanelColor;
            if (border)
            {
                image.sprite = FightSkin.Surface; image.type = Image.Type.Sliced;
                var line = rt.gameObject.AddComponent<FightOutline>(); line.effectColor = GoldBorder;
                var frame = Node(rt, "Metal frame", 0, 0, w, h); Stretch(frame);
                frame.gameObject.AddComponent<FightFrame>().Bind(line);
            }
            return rt;
        }
        public static TextMeshProUGUI Text(Transform parent, string value, float x, float y, float w, float h, float size = 20, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
            => CreateText(parent, value, x, y, w, h, size, color, align, true);
        public static TextMeshProUGUI RawText(Transform parent, string value, float x, float y, float w, float h, float size = 20, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
            => CreateText(parent, value, x, y, w, h, size, color, align, false);
        private static TextMeshProUGUI CreateText(Transform parent, string value, float x, float y, float w, float h, float size, Color? color, TextAlignmentOptions align, bool localize)
        {
            value = value ?? "";
            var rt = Node(parent, "Text", x, y, w, h); var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = Program.instance.ui_.tmpFont; FightSkin.EnsureSymbolFallback(t.font); t.text = localize ? FightText.Get(value) : value; t.fontSize = size; t.color = color ?? Foreground;
            t.alignment = align; t.richText = false; t.raycastTarget = true; t.enableWordWrapping = true; t.overflowMode = TextOverflowModes.Ellipsis;
            t.wordSpacing = 1.5f;
            rt.gameObject.AddComponent<FightTooltip>().LocalizeHint = localize;
            if (!t.text.Contains("\n") && h <= size * 2)
            {
                t.enableWordWrapping = false;
                if (localize && Language.GetConfig() != Language.SimplifiedChinese && Language.GetConfig() != Language.TraditionalChinese)
                { t.enableAutoSizing = true; t.fontSizeMax = size; t.fontSizeMin = Mathf.Max(12, size * .76f); }
                // The publisher font has a taller CJK line box than its visual
                // glyph height. Reserve its measured line, otherwise TMP's
                // vertical ellipsis can discard an entire short heading.
                float lineHeight = t.GetPreferredValues(t.text, float.PositiveInfinity, float.PositiveInfinity).y;
                rt.sizeDelta = new Vector2(w, Mathf.Max(h, lineHeight + 2));
            }
            if (localize) FightText.Bind(t, value);
            return t;
        }
        public static TextMeshProUGUI HeroTitle(Transform parent, string value, float x, float y, float w, float h, float size, bool localize = true)
        {
            var t = localize ? Text(parent, value, x, y, w, h, size, Active) : RawText(parent, value, x, y, w, h, size, Active);
            if (FightSkin.TitleFont != null)
            {
                FightSkin.TitleFont.TryAddCharacters(t.text);
                t.font = FightSkin.TitleFont;
            }
            t.fontStyle = FontStyles.Bold; t.characterSpacing = 1;
            t.color = Color.white;
            t.enableVertexGradient = true;
            t.colorGradient = new VertexGradient(Hex("E9D9B8"), Hex("E9D9B8"), Hex("A58B61"), Hex("A58B61"));
            t.enableWordWrapping = false;
            float titleLine = t.GetPreferredValues(t.text, float.PositiveInfinity, float.PositiveInfinity).y;
            t.rectTransform.sizeDelta = new Vector2(w, Mathf.Max(h, titleLine + 4));
            return t;
        }
        public static Button Button(Transform parent, string label, float x, float y, float w, float h, Action action, bool primary = false, bool enabled = true, bool localize = true)
        {
            var rt = Panel(parent, x, y, w, h, primary ? ActionFill : Raised, true, label);
            rt.GetComponent<Outline>().effectColor = primary ? Cyan : GoldBorder;
            rt.GetComponent<Image>().sprite = primary ? FightSkin.ActionSurface : FightSkin.ButtonSurface;
            var frame = rt.GetComponentInChildren<FightFrame>(); frame.Primary = primary; frame.ButtonFrame = true;
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = rt.GetComponent<Image>();
            var colors = b.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1.26f, 1.28f, 1.3f);
            colors.pressedColor = Hex("819CA8"); colors.selectedColor = new Color(1.18f, 1.15f, 1.1f); colors.disabledColor = new Color(.52f, .52f, .52f, .8f); colors.fadeDuration = .14f; b.colors = colors;
            b.interactable = enabled; b.onClick.AddListener(() => { if (action != null) action(); });
            var size = h >= 64 ? 27 : h >= 50 ? 22 : h >= 40 ? 19 : 17;
            var caption = localize ? Text(rt, label, 10, 0, w - 20, h, size, primary ? CyanText : Gold, TextAlignmentOptions.Center)
                : RawText(rt, label, 10, 0, w - 20, h, size, primary ? CyanText : Gold, TextAlignmentOptions.Center);
            var language = Language.GetConfig();
            if (!(label ?? "").Contains("\n") && !caption.text.Contains("\n")
                && language != Language.SimplifiedChinese && language != Language.TraditionalChinese && language != "zh-SC")
            {
                caption.enableWordWrapping = false;
                caption.enableAutoSizing = true;
                caption.fontSizeMax = size;
                caption.fontSizeMin = Mathf.Max(12, size * .76f);
            }
            caption.characterSpacing = primary ? .9f : .35f;
            rt.gameObject.AddComponent<FightButtonFeedback>().Bind(frame);
            return b;
        }
        public static Button SwitchRow(Transform parent, string label, bool value, float x, float y, float w, float h, Action action)
        {
            // Keep a full-row Button hit area and the existing Outline API,
            // while presenting a switch rather than a bevelled action button.
            var rt = Panel(parent, x, y, w, h, Color.clear, true, label);
            var surface = rt.GetComponent<Image>(); surface.sprite = null; surface.type = Image.Type.Simple;
            rt.GetComponent<Outline>().enabled = false;
            rt.GetComponentInChildren<FightFrame>().enabled = false;
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = rt.GetComponent<Image>();
            b.onClick.AddListener(() => action?.Invoke());
            var colors = b.colors; colors.highlightedColor = Color.white; colors.selectedColor = Color.white; colors.fadeDuration = .12f; b.colors = colors;
            var caption = Text(rt, label, 14, 0, Mathf.Max(32, w - 90), h, 18, Foreground, TextAlignmentOptions.Left);
            var toggle = Node(rt, "Switch " + (value ? "on" : "off"), w - 65, (h - 24) / 2, 46, 24).gameObject.AddComponent<FightToggleGraphic>();
            toggle.Value = value; toggle.color = Color.white; toggle.raycastTarget = false;
            rt.gameObject.AddComponent<FightSwitchFeedback>().Bind(b, toggle, caption);
            return b;
        }
        public static void StyleDestructiveButton(Button button)
        {
            if (button == null) return;
            var surface = button.GetComponent<Image>();
            if (surface != null) surface.color = Hex("301E21");
            var outline = button.GetComponent<Outline>();
            if (outline != null) outline.effectColor = Danger;
            var frame = button.GetComponentInChildren<FightFrame>(true);
            if (frame != null) frame.Primary = false;
            var caption = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (caption != null) caption.color = Danger;
        }
        public static void SetSwitchValue(Button row, bool value)
        {
            var graphic = row == null ? null : row.GetComponentInChildren<FightToggleGraphic>(true);
            if (graphic == null) return;
            graphic.Value = value; graphic.name = "Switch " + (value ? "on" : "off");
            row.GetComponent<FightSwitchFeedback>()?.Refresh();
        }
        public static void ReflowSwitchRow(Button row, float width, float height, float labelInset = 14)
        {
            if (row == null) return;
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
            var caption = row.GetComponentInChildren<TextMeshProUGUI>(true);
            if (caption != null) Place(caption.rectTransform, labelInset, 0, Mathf.Max(32, width - 90), height);
            var graphic = row.GetComponentInChildren<FightToggleGraphic>(true);
            if (graphic != null) Place(graphic.rectTransform, width - 65, (height - 24) / 2, 46, 24);
        }
        public static Button Tab(Transform parent, string label, bool selected, float x, float y, float w, float h, Action action, bool enabled = true)
        {
            var b = Button(parent, label, x, y, w, h, action, false, enabled);
            b.GetComponent<Image>().color = selected ? Raised : new Color(0, 0, 0, .08f);
            b.GetComponent<Outline>().enabled = false;
            var text = b.GetComponentInChildren<TextMeshProUGUI>(); text.color = selected ? Active : Muted;
            if (selected)
            {
                Line(b.transform, 12, h - 2, w - 24, Gold);
                Icon(b.transform, "diamond", w / 2 - 4, h - 7, 8, 8, Active);
            }
            return b;
        }
        public static Button TextAction(Transform parent, string label, float x, float y, float w, float h, Action action, bool enabled = true)
        {
            var rt = Node(parent, label, x, y, w, h); var image = rt.gameObject.AddComponent<Image>(); image.color = Color.clear;
            var button = rt.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.interactable = enabled;
            button.onClick.AddListener(() => action?.Invoke());
            var text = Text(rt, label, 0, 0, w, h, h >= 56 ? 26 : h >= 44 ? 22 : 20, enabled ? Gold : Muted, TextAlignmentOptions.MidlineLeft);
            text.fontStyle = FontStyles.Bold;
            button.targetGraphic = text;
            var colors = button.colors; colors.highlightedColor = new Color(1.2f, 1.2f, 1.15f); colors.fadeDuration = .12f; button.colors = colors;
            var rule = Panel(rt, 0, Mathf.Min(h - 2, h * .5f + text.fontSize * .5f + 5), Mathf.Min(w, text.preferredWidth), 1, Gold, false, "Action underline");
            rule.GetComponent<Image>().raycastTarget = false;
            rt.gameObject.AddComponent<FightTextActionFeedback>().Bind(button, rule.GetComponent<Image>());
            return button;
        }
        public static FightIcon Icon(Transform parent, string kind, float x, float y, float w, float h, Color? color = null)
        {
            var icon = Node(parent, "Icon " + kind, x, y, w, h).gameObject.AddComponent<FightIcon>();
            icon.Kind = kind; icon.color = color ?? Gold; icon.raycastTarget = false; return icon;
        }
        public static void OrnamentLine(Transform parent, float x, float y, float w)
        {
            Icon(parent, "diamond", x, y - 5, 14, 14, GoldBorder);
            Line(parent, x + 22, y + 2, Mathf.Max(0, w - 22), GoldBorder);
        }
        public static TMP_InputField Input(Transform parent, string value, string placeholder, float x, float y, float w, float h, Action<string> changed = null, bool password = false, bool multiline = false)
        {
            var rt = Panel(parent, x, y, w, h, Hex("07121B"), true, "Input");
            var border = rt.GetComponent<Outline>(); border.effectColor = Border;
            var frame = rt.GetComponentInChildren<FightFrame>(); frame.Primary = false; frame.ButtonFrame = true;
            var viewport = Node(rt, "Viewport", 12, 5, w - 24, h - 10); viewport.gameObject.AddComponent<RectMask2D>();
            var t = RawText(viewport, "", 0, 0, w - 24, h - 10, 18, Foreground, multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.Left);
            var p = Text(viewport, placeholder, 0, 0, w - 24, h - 10, 18, Muted, multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.Left);
            // TMP creates its caret renderer in OnEnable, which requires the
            // text component to have been assigned already.
            rt.gameObject.SetActive(false);
            var input = rt.gameObject.AddComponent<TMP_InputField>(); input.textViewport = viewport; input.textComponent = t; input.placeholder = p;
            input.targetGraphic = rt.GetComponent<Image>(); input.contentType = password ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            input.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
            ConfigurePlainTextInput(input);
            input.text = value ?? "";
            input.onValueChanged.AddListener(v => changed?.Invoke(v)); rt.gameObject.SetActive(true); return input;
        }
        public static void ConfigurePlainTextInput(TMP_InputField input)
        {
            // TMP checks its own richText flag when decorating IME composition.
            // Disabling only the label exposes its generated <u> tags as text.
            input.richText = false; input.isRichTextEditingAllowed = false;
            input.caretColor = Cyan; input.customCaretColor = true; input.caretWidth = 2;
            input.caretBlinkRate = .85f;
            input.selectionColor = new Color(.16f, .6f, .72f, .3f);
            if (input.textComponent != null)
            {
                input.textComponent.richText = false;
                input.textComponent.overflowMode = TextOverflowModes.Overflow;
                input.textComponent.enableWordWrapping = input.lineType != TMP_InputField.LineType.SingleLine;
                // Geometry/Midline alignment builds the caret around local y=0.
                // Top-pivot viewports then clip its upper half. Middle uses the
                // character's actual ascender/descender within the text rect.
                if (input.lineType == TMP_InputField.LineType.SingleLine || input.textComponent.verticalAlignment == VerticalAlignmentOptions.Geometry)
                    input.textComponent.verticalAlignment = VerticalAlignmentOptions.Middle;
            }
            if (input.placeholder is TMP_Text placeholder)
            {
                placeholder.richText = false;
                if (input.lineType == TMP_InputField.LineType.SingleLine || placeholder.verticalAlignment == VerticalAlignmentOptions.Geometry)
                    placeholder.verticalAlignment = VerticalAlignmentOptions.Middle;
            }
            if (input.textViewport != null && input.textViewport.GetComponent<RectMask2D>() == null
                && input.textViewport.GetComponent<Mask>() == null)
                input.textViewport.gameObject.AddComponent<RectMask2D>();
            ConfigureInputFeedback(input);
        }
        public static void ConfigureInputFeedback(Selectable input)
        {
            var feedback = input.GetComponent<FightInputFeedback>() ?? input.gameObject.AddComponent<FightInputFeedback>();
            feedback.Bind(input);
        }
        public static RectTransform Scroll(Transform parent, float x, float y, float w, float h, float contentHeight, out ScrollRect scroll)
        {
            var rt = Panel(parent, x, y, w, h, PanelColor, false, "Scroll");
            var vp = Node(rt, "Viewport", 0, 0, w, h); vp.gameObject.AddComponent<RectMask2D>();
            var content = Node(vp, "Content", 0, 0, w, Mathf.Max(h, contentHeight));
            scroll = rt.gameObject.AddComponent<ScrollRect>(); scroll.viewport = vp; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;
            scroll.decelerationRate = .08f;
            // The visual rail stays thin; its transparent hit area is wide
            // enough to grab reliably at both desktop sizes.
            var rail = Panel(rt, w - 15, 2, 14, h - 4, Color.clear, false, "Scrollbar");
            var track = Panel(rail, 10, 0, 4, h - 4, Border, false, "Track"); track.GetComponent<Image>().raycastTarget = false;
            var handle = Node(rail, "Scrollbar handle", 0, 0, 14, h - 4); Stretch(handle);
            var thumb = Panel(handle, 10, 0, 4, h - 4, GoldBorder, false, "Thumb");
            Stretch(thumb); thumb.offsetMin = new Vector2(10, 0); thumb.offsetMax = Vector2.zero;
            var bar = rail.gameObject.AddComponent<Scrollbar>(); bar.targetGraphic = thumb.GetComponent<Image>(); bar.handleRect = handle; bar.direction = Scrollbar.Direction.BottomToTop;
            var colors = bar.colors; colors.highlightedColor = new Color(1.5f, 1.4f, 1.15f); colors.pressedColor = new Color(1.75f, 1.65f, 1.3f); colors.fadeDuration = .12f; bar.colors = colors;
            scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return content;
        }
        public static RectTransform Scroll(Transform parent, float x, float y, float w, float h, float contentHeight)
            => Scroll(parent, x, y, w, h, contentHeight, out _);
        public static void Clear(Transform parent)
        {
            FightClient.Instance?.RememberFocusForRefresh(parent);
            foreach (Transform child in parent) { child.gameObject.SetActive(false); UnityEngine.Object.Destroy(child.gameObject); }
        }
        public static void Line(Transform parent, float x, float y, float w, Color? color = null)
            => Panel(parent, x, y, w, 1, color ?? Border, false, "Divider");
        public static RawImage Art(Transform parent, int code, float x, float y, float w, float h, bool fullCard = false)
        {
            var rt = Panel(parent, x, y, w, h, Raised, false, "CardArt " + code);
            var pixels = Node(rt, "Card Image", 0, 0, w, h);
            var placeholder = Node(rt, "Card placeholder", 0, 0, w, h).gameObject.AddComponent<FightCardBack>();
            placeholder.raycastTarget = false; placeholder.color = GoldBorder;
            pixels.SetAsLastSibling();
            var img = pixels.gameObject.AddComponent<RawImage>(); rt.GetComponent<Image>().raycastTarget = false;
            img.color = Color.clear; img.raycastTarget = false;
            rt.gameObject.AddComponent<FightArt>().Load(code, img, fullCard); return img;
        }
        public static Button CardButton(RawImage image, Action action)
        {
            var button = image.gameObject.AddComponent<Button>(); image.raycastTarget = true; button.targetGraphic = image;
            button.transition = Selectable.Transition.None; button.onClick.AddListener(() => action?.Invoke());
            var rt = image.transform.parent as RectTransform;
            var outline = rt.gameObject.AddComponent<FightOutline>(); outline.effectColor = Gold; outline.enabled = false;
            var frameRoot = Node(rt, "Card focus frame", 0, 0, rt.rect.width, rt.rect.height); Stretch(frameRoot);
            var frame = frameRoot.gameObject.AddComponent<FightFrame>(); frame.ButtonFrame = true; frame.Bind(outline);
            image.gameObject.AddComponent<FightButtonFeedback>().Bind(frame);
            return button;
        }
        public static void PreviewOnFocus(Button button, Action preview)
        {
            var focus = button.gameObject.AddComponent<FightHoverPreview>();
            focus.Preview = preview;
        }
        public static RawImage Asset(Transform parent, string filename, float x, float y, float w, float h, bool cover = true)
        {
            var rt = Node(parent, "Visual", x, y, w, h); var img = rt.gameObject.AddComponent<RawImage>(); img.raycastTarget = false;
            var path = "Data/ClientUI/" + filename;
            if (File.Exists(path))
            {
                var tex = FightAsset.Acquire(path); img.texture = tex;
                float imageRatio = (float)tex.width / tex.height, frameRatio = w / h;
                if (cover)
                {
                    if (imageRatio > frameRatio) { float visible = frameRatio / imageRatio; img.uvRect = new Rect((1 - visible) / 2, 0, visible, 1); }
                    else { float visible = imageRatio / frameRatio; img.uvRect = new Rect(0, (1 - visible) / 2, 1, visible); }
                }
                else
                {
                    float fitW = imageRatio > frameRatio ? w : h * imageRatio, fitH = imageRatio > frameRatio ? w / imageRatio : h;
                    Place(rt, x + (w - fitW) / 2, y + (h - fitH) / 2, fitW, fitH);
                }
            }
            else img.color = Raised;
            return img;
        }
    }
    internal sealed class FightAsset : MonoBehaviour
    {
        private static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        internal static Texture2D Acquire(string path)
        {
            if (textures.TryGetValue(path, out var cached) && cached != null) return cached;
            var texture = new Texture2D(2, 2); texture.name = "Fight artwork " + Path.GetFileName(path);
            texture.LoadImage(File.ReadAllBytes(path)); texture.wrapMode = TextureWrapMode.Clamp;
            textures[path] = texture; return texture;
        }
        internal static void ClearCache()
        {
            foreach (var texture in textures.Values) if (texture != null) Destroy(texture);
            textures.Clear();
        }
    }
    internal sealed class FightFocusKey : MonoBehaviour
    {
        internal string Key;
        internal Selectable KeyboardTarget;
    }
    internal sealed class FightHoverPreview : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        internal Action Preview;
        private void ShowPreview()
        {
            var button = GetComponent<Button>();
            if (button != null && button.IsInteractable()) Preview?.Invoke();
        }
        public void OnPointerEnter(PointerEventData pointer) => ShowPreview();
        public void OnSelect(BaseEventData selection) => ShowPreview();
    }
    internal sealed class FightTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler
    {
        private static FightTooltip activeTooltip;
        private RectTransform tooltip;
        private TextMeshProUGUI tooltipLabel;
        private string displayedMessage;
        private Vector2 parentSize;
        private Coroutine delayed;
        private Vector2 pointerPosition;
        private Camera eventCamera;
        internal string Hint;
        internal bool LocalizeHint = true;
        public void OnPointerEnter(PointerEventData pointer)
        {
            Hide();
            var text = GetComponent<TextMeshProUGUI>();
            if (text != null && (text.GetComponentInParent<TMP_InputField>() != null
                || string.IsNullOrEmpty(Hint) && !text.isTextOverflowing && !text.isTextTruncated)) return;
            if (string.IsNullOrEmpty(Hint ?? (text != null ? text.text : null))) return;
            pointerPosition = pointer.position; eventCamera = pointer.enterEventCamera; delayed = StartCoroutine(ShowAfterDelay());
        }
        public void OnSelect(BaseEventData selection)
        {
            if (string.IsNullOrEmpty(Hint) || GetComponent<TMP_InputField>() != null || GetComponent<InputField>() != null) return;
            Hide();
            var canvas = GetComponentInParent<Canvas>();
            var rect = GetComponent<RectTransform>();
            if (canvas == null || rect == null) return;
            eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            pointerPosition = RectTransformUtility.WorldToScreenPoint(eventCamera, rect.TransformPoint(rect.rect.center));
            delayed = StartCoroutine(ShowAfterDelay());
        }
        public void OnDeselect(BaseEventData selection) => Hide();
        private void Update()
        {
            // Captions share their parent control's pointer target. Implementing
            // pointer-down on the caption would consume TMP input activation.
            if (tooltip == null && delayed == null) return;
            var mouse = Mouse.current; var keyboard = Keyboard.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)
                || keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame
                    || keyboard.escapeKey.wasPressedThisFrame)
                || UserInput.WasSubmitPressed || UserInput.WasCancelPressed) { Hide(); return; }
            if (tooltip == null) return;
            var source = GetComponent<TextMeshProUGUI>();
            var message = Hint ?? (source != null ? source.text : null);
            var parent = tooltip.parent as RectTransform;
            if (string.IsNullOrEmpty(message) || parent == null) { Hide(); return; }
            if (message != displayedMessage || parent.rect.size != parentSize) LayoutTooltip(message);
        }
        private IEnumerator ShowAfterDelay()
        {
            yield return new WaitForSecondsRealtime(.35f); delayed = null;
            var text = GetComponent<TextMeshProUGUI>(); var canvas = GetComponentInParent<Canvas>();
            var message = Hint ?? (text != null ? text.text : null);
            if (canvas == null || !canvas.enabled || string.IsNullOrEmpty(message)) yield break;
            var parent = FightClient.Instance?.TooltipRootFor(transform) ?? canvas.rootCanvas.transform as RectTransform;
            if (parent == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, pointerPosition, eventCamera, out var local)) yield break;
            // A caption and its parent can both receive the same pointer enter.
            // Keep one tooltip visible rather than stacking two reading layers.
            if (activeTooltip != null && activeTooltip != this) activeTooltip.Hide();
            activeTooltip = this;
            tooltip = FightUI.Panel(parent, 16, 16, 160, 60, FightUI.Raised, true, "Full text");
            tooltip.GetComponent<Image>().raycastTarget = false;
            tooltipLabel = FightUI.RawText(tooltip, message, 16, 12, 128, 36, 18);
            tooltipLabel.enableWordWrapping = true; tooltipLabel.raycastTarget = false;
            LayoutTooltip(message);
            if (tooltip != null) tooltip.gameObject.AddComponent<FightPageReveal>();
        }
        private void LayoutTooltip(string message)
        {
            var parent = tooltip != null ? tooltip.parent as RectTransform : null;
            if (parent == null || tooltipLabel == null || parent.rect.width < 64 || parent.rect.height < 64
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, pointerPosition, eventCamera, out var local)) { Hide(); return; }
            displayedMessage = message; parentSize = parent.rect.size;
            if (LocalizeHint) message = FightText.Get(message);
            tooltipLabel.text = message;
            float parentWidth = parent.rect.width, parentHeight = parent.rect.height;
            float width = Mathf.Min(Mathf.Min(520, parentWidth - 32), Mathf.Max(160,
                tooltipLabel.GetPreferredValues(message, float.PositiveInfinity, float.PositiveInfinity).x + 32));
            float height = Mathf.Min(Mathf.Min(320, parentHeight - 32), Mathf.Max(48,
                tooltipLabel.GetPreferredValues(message, width - 32, float.PositiveInfinity).y + 26));
            float x = Mathf.Clamp(local.x - parent.rect.xMin + 16, 16, parentWidth - width - 16);
            var y = parent.rect.yMax - local.y + 16;
            var anchor = GetComponent<RectTransform>();
            if (anchor != null)
            {
                var corners = new Vector3[4]; anchor.GetWorldCorners(corners);
                var bottom = parent.InverseTransformPoint(corners[0]); var top = parent.InverseTransformPoint(corners[1]);
                y = parent.rect.yMax - bottom.y + 10;
                if (y + height > parentHeight - 16) y = parent.rect.yMax - top.y - height - 10;
            }
            y = Mathf.Clamp(y, 16, parentHeight - height - 16);
            FightUI.Place(tooltip, x, y, width, height);
            FightUI.Place(tooltipLabel.rectTransform, 16, 12, width - 32, height - 24);
        }
        public void OnPointerExit(PointerEventData pointer) => Hide();
        private void OnDisable() => Hide();
        private void OnDestroy() => Hide();
        private void Hide()
        {
            if (delayed != null) StopCoroutine(delayed);
            delayed = null;
            if (tooltip != null) { tooltip.gameObject.SetActive(false); Destroy(tooltip.gameObject); }
            tooltip = null; tooltipLabel = null; displayedMessage = null;
            if (activeTooltip == this) activeTooltip = null;
        }
    }
    internal sealed class FightArt : MonoBehaviour
    {
        private int card; private bool art, ownsReference, disposed, started;
        private RawImage target;
        private FightCardBack placeholder;
        private float reveal;
        private RectMask2D[] masks;
        private readonly Vector3[] corners = new Vector3[4];
        private CancellationTokenSource cancel = new CancellationTokenSource();
        internal bool Ready => target != null && target.texture != null && target.canvasRenderer.GetColor().a > .9f;
        internal bool Visible
        {
            get
            {
                if (target == null || !gameObject.activeInHierarchy) return false;
                var visible = Bounds(target.rectTransform);
                if (!Intersects(visible, new Rect(0, 0, Screen.width, Screen.height))) return false;
                foreach (var mask in masks)
                    if (mask != null && mask.enabled && !Intersects(visible, Bounds(mask.GetComponent<RectTransform>()))) return false;
                return true;
            }
        }
        public void Load(int code, RawImage image, bool fullCard)
        {
            card = code; art = !fullCard; target = image; masks = image.GetComponentsInParent<RectMask2D>();
            placeholder = GetComponentInChildren<FightCardBack>();
            // A long directory must not fill the shared loader queue with
            // hidden rows before the selected construction can appear.
        }
        private void Update()
        {
            if (disposed || target == null) return;
            if (!started)
            {
                if (!Visible) return;
                started = true; if (placeholder != null) placeholder.Loading = true;
                LoadVisible();
            }
            if (target.texture == null) return;
            reveal = Mathf.Min(1, reveal + Time.unscaledDeltaTime * 7);
            target.canvasRenderer.SetColor(new Color(1, 1, 1, reveal));
            if (reveal >= 1) { if (placeholder != null) placeholder.gameObject.SetActive(false); enabled = false; }
        }
        private Rect Bounds(RectTransform rect)
        {
            rect.GetWorldCorners(corners);
            return new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
        }
        private static bool Intersects(Rect a, Rect b) => a.xMax > b.xMin && a.xMin < b.xMax && a.yMax > b.yMin && a.yMin < b.yMax;
        private async void LoadVisible()
        {
            try
            {
                // Read-only client previews use a complete rendered card face.
                // The original duel/editor retains its live video materials.
                var tex = art ? await CardImageLoader.LoadArtAsync(card, false, cancel.Token) : await CardImageLoader.LoadCardAsync(card, false, cancel.Token, true);
                ownsReference = tex != null;
                if (disposed || cancel.IsCancellationRequested || target == null) ReleaseReference();
                else if (tex != null)
                {
                    target.texture = tex; target.color = Color.white; target.canvasRenderer.SetColor(new Color(1, 1, 1, 0));
                    if (placeholder != null) placeholder.Loading = false;
                    if (art)
                    {
                        var rect = target.rectTransform.rect;
                        float imageRatio = (float)tex.width / tex.height, frameRatio = rect.width / rect.height;
                        if (imageRatio > frameRatio) { float crop = frameRatio / imageRatio; target.uvRect = new Rect((1 - crop) / 2, 0, crop, 1); }
                        else { float crop = imageRatio / frameRatio; target.uvRect = new Rect(0, (1 - crop) / 2, 1, crop); }
                    }
                }
                else ShowMissing();
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { ShowMissing(); Debug.LogWarning("Fight card art: " + e.Message); }
        }
        private void ShowMissing()
        {
            if (disposed || target == null) return;
            enabled = false;
            if (placeholder != null) { placeholder.Loading = false; placeholder.Failed = true; placeholder.SetVerticesDirty(); }
            FightUI.Hint(target.gameObject, "卡图暂不可用，卡片名称与说明仍可查看。");
        }
        private void ReleaseReference()
        {
            if (!ownsReference) return;
            ownsReference = false;
            if (art) CardImageLoader.ReleaseArt(card); else CardImageLoader.ReleaseCard(card, true);
        }
        private void OnDestroy() { disposed = true; cancel.Cancel(); ReleaseReference(); cancel.Dispose(); }
    }
}
