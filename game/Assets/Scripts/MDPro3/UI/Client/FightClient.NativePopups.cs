using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YgomSystem.ElementSystem;
using YgomSystem.UI;

namespace MDPro3.UI.Client
{
    internal partial class FightClient
    {
        private void StyleNativePopups()
        {
            if (nativePopupViewport == null || !ClientCanvasVisible) return;
            foreach (var popup in nativePopupViewport.GetComponentsInChildren<MDPro3.UI.Popup.Popup>(true))
            {
                // Some native factories retain a disabled YesOrNo component as
                // the template for a different popup on the same GameObject.
                if (!popup.enabled) continue;
                var skin = popup.GetComponent<FightNativePopupSkin>() ?? popup.gameObject.AddComponent<FightNativePopupSkin>();
                skin.Bind(popup);
            }
        }

        private void RestoreNativePopupSkins()
        {
            if (nativePopupViewport == null) return;
            foreach (var skin in nativePopupViewport.GetComponentsInChildren<FightNativePopupSkin>(true)) skin.Restore();
        }

        internal bool NativePopupSkinActive(Transform popup) => ClientCanvasVisible && nativePopupViewport != null
            && popup.IsChildOf(nativePopupViewport);
    }

    // A skin around the original popup controls. Content remains the native
    // transition/layout root; TMP values, validation, navigation and listeners
    // are never replaced. LateUpdate wins over the old color-container tweens.
    internal sealed class FightNativePopupSkin : MonoBehaviour
    {
        private MDPro3.UI.Popup.Popup popup;
        private RectTransform content;
        private NativeSelectionLayout selectionLayout;
        private TextMeshProUGUI title;
        private GameObject close;
        private readonly Dictionary<Image, NativeImage> images = new();
        private readonly Dictionary<TextMeshProUGUI, NativeText> texts = new();
        private readonly Dictionary<Selectable, NativeControl> controls = new();
        private readonly List<GameObject> decorations = new();
        private bool applied;
        internal Image Surface { get; private set; }

        internal void Bind(MDPro3.UI.Popup.Popup source)
        {
            if (popup == null)
            {
                popup = source;
                var manager = source.GetComponent<ElementObjectManager>();
                if (manager == null) return;
                content = manager.GetElement<RectTransform>("Content");
                title = manager.GetElement<TextMeshProUGUI>("TitleText");
                close = manager.GetElement("CloseButton");
                if (content == null) return;
                if (source is MDPro3.UI.Popup.PopupSelection) selectionLayout = new NativeSelectionLayout(content, manager);
                Surface = CreateSurface(content, "Fight native popup surface", FightUI.PanelColor, false, out _);
            }
            if (content == null) return;

            // Selection popups populate asynchronously and recycle their rows.
            // Discover new objects without altering the existing layout.
            foreach (var image in GetComponentsInChildren<Image>(true))
                if (!IsDecoration(image.transform) && !images.ContainsKey(image)) images.Add(image, new NativeImage(image));
            foreach (var control in GetComponentsInChildren<Selectable>(true))
            {
                if (control.gameObject == close || controls.ContainsKey(control)
                    || !(control is Button || control is TMP_InputField || control is InputField || control is UnityEngine.UI.Toggle)) continue;
                var state = new NativeControl(control);
                controls.Add(control, state);
                bool input = control is TMP_InputField || control is InputField;
                state.NativeToggle = control is UnityEngine.UI.Toggle ? null : control.GetComponent<MDPro3.UI.SelectionToggle>();
                bool boolean = control is UnityEngine.UI.Toggle || popup is MDPro3.UI.Popup.PopupHandTestSettings && state.NativeToggle != null;
                var existingInputFrame = input ? control.GetComponentInChildren<FightFrame>(true) : null;
                state.Primary = !input && control.name == "DecideButton";
                state.Surface = CreateSurface(control.transform, "Fight native control surface",
                    input ? FightUI.Hex("07121B") : state.Primary ? FightUI.ActionFill : FightUI.Raised, true, out var frame,
                    !input || existingInputFrame == null);
                state.Frame = frame;
                if (input)
                {
                    state.InputOutline = control.GetComponent<Outline>() ?? control.gameObject.AddComponent<FightOutline>();
                    state.OutlineColor = state.InputOutline.effectColor;
                    state.OutlineEnabled = state.InputOutline.enabled;
                    if (existingInputFrame != null)
                    {
                        // The shared input feedback already owns this frame.
                        // Keep that exact frame so caret/focus proofs and error
                        // outlines continue to refer to the original input.
                        state.Frame = existingInputFrame;
                    }
                    state.Frame.Bind(state.InputOutline);
                    state.Frame.InputFrame = state.Frame.ButtonFrame = true;
                    if (control is TMP_InputField field) FightUI.ConfigurePlainTextInput(field);
                    else FightUI.ConfigureInputFeedback(control);
                    state.InputFeedback = control.GetComponent<FightInputFeedback>();
                }
                else if (boolean)
                {
                    var glyph = FightUI.Node(control.transform, "Fight native switch glyph", 0, 0, 46, 24);
                    glyph.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                    glyph.anchorMin = glyph.anchorMax = new Vector2(1, .5f); glyph.pivot = new Vector2(1, .5f);
                    glyph.anchoredPosition = new Vector2(-16, 0);
                    state.SwitchGraphic = glyph.gameObject.AddComponent<FightToggleGraphic>();
                    state.SwitchGraphic.raycastTarget = false;
                    control.targetGraphic = state.Surface;
                    state.SwitchFeedback = control.GetComponent<FightSwitchFeedback>() ?? control.gameObject.AddComponent<FightSwitchFeedback>();
                    state.SwitchFeedback.Bind(control, state.SwitchGraphic);
                    decorations.Add(glyph.gameObject);
                }
                else
                {
                    state.Frame.Primary = state.Primary;
                    state.Frame.ButtonFrame = true;
                    state.Feedback = control.GetComponent<FightButtonFeedback>() ?? control.gameObject.AddComponent<FightButtonFeedback>();
                    state.Feedback.ManageCaptionOpacity = false;
                    state.Feedback.Bind(state.Frame);
                }
            }
            foreach (var text in GetComponentsInChildren<TextMeshProUGUI>(true))
                if (!IsDecoration(text.transform) && !texts.ContainsKey(text))
                {
                    var state = new NativeText(text); texts.Add(text, state);
                    state.ApplyTypography(text == title);
                }
            Repaint();
        }

        private Image CreateSurface(Transform parent, string name, Color fill, bool control, out FightFrame frame, bool createFrame = true)
        {
            var rect = FightUI.Node(parent, name, 0, 0, 0, 0);
            rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            FightUI.Stretch(rect); rect.SetAsFirstSibling();
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = control ? FightSkin.ButtonSurface : FightSkin.Surface;
            image.type = Image.Type.Sliced; image.color = fill; image.raycastTarget = true;
            var outline = rect.gameObject.AddComponent<FightOutline>();
            outline.effectColor = FightUI.GoldBorder;
            frame = null;
            if (createFrame)
            {
                var border = FightUI.Node(rect, "Fight native frame", 0, 0, 0, 0); FightUI.Stretch(border);
                frame = border.gameObject.AddComponent<FightFrame>(); frame.ButtonFrame = control; frame.Bind(outline);
            }
            decorations.Add(rect.gameObject);
            return image;
        }

        private bool IsDecoration(Transform value)
        {
            for (var ancestor = value; ancestor != null && ancestor != transform; ancestor = ancestor.parent)
                if (ancestor.name.StartsWith("Fight native")) return true;
            return false;
        }

        private void LateUpdate() { Repaint(); }

        private void Repaint()
        {
            if (popup == null || content == null) return;
            if (FightClient.Instance == null || !FightClient.Instance.NativePopupSkinActive(transform)) { Restore(); return; }
            selectionLayout?.Apply(Surface.rectTransform);
            if (!applied)
            {
                foreach (var decoration in decorations) if (decoration != null) decoration.SetActive(true);
                foreach (var state in controls.Values) state.Apply();
                foreach (var state in texts.Values) state.ApplyTypography(state.Text == title);
                applied = true;
            }
            foreach (var pair in images)
            {
                var image = pair.Key;
                if (image == null) continue;
                string name = image.name.ToLowerInvariant();
                var mask = image.GetComponent<Mask>();
                bool outside = !image.transform.IsChildOf(content);
                var owner = image.GetComponentInParent<Selectable>();
                bool oldSwitchGraphic = owner != null && controls.TryGetValue(owner, out var switchState) && switchState.SwitchGraphic != null;
                bool ruleAsset = name.Contains("card") || name.Contains("attribute") || name.Contains("rarity") || name.Contains("limit");
                if (ruleAsset && !oldSwitchGraphic) { pair.Value.Managed = false; continue; }
                bool semantic = name.Contains("icon") || name.Contains("arrow") || name.Contains("check");
                bool scrollbar = image.GetComponentInParent<Scrollbar>() != null;
                var scroll = image.GetComponentInParent<ScrollRect>();
                bool scrollHitArea = scroll != null && (image.gameObject == scroll.gameObject
                    || scroll.viewport != null && image.gameObject == scroll.viewport.gameObject);
                var size = image.rectTransform.rect.size;
                float thin = Mathf.Min(size.x, size.y), longSide = Mathf.Max(size.x, size.y);
                // BackgroundLine is a full-window scan texture in the native
                // prefab. Only a genuinely thin rect is a solid divider.
                bool line = (name.Contains("line") || name.Contains("separator") || name.Contains("divider"))
                    && !name.Contains("background") && !name.Contains("scan") && !name.Contains("texture") && !name.Contains("glow")
                    && thin > 0 && thin <= 4 && longSide >= 24 && longSide >= thin * 12;
                image.material = null;
                if (mask != null)
                {
                    // An alpha-zero mask can discard its stencil mesh. Hide
                    // only the mask's visible fill, retaining its alpha/shape.
                    SetTint(image, pair.Value.OriginalColor); mask.showMaskGraphic = false;
                }
                else if (outside && close != null && (image.gameObject == close || image.transform.IsChildOf(close.transform)))
                    SetTint(image, new Color(.02f, .035f, .05f, .7f));
                else if (oldSwitchGraphic) SetTint(image, Color.clear);
                else if (scrollbar) SetTint(image, name.Contains("handle") || name.Contains("thumb") ? FightUI.GoldBorder : FightUI.Border);
                else if (semantic) SetTint(image, FightUI.Gold);
                else if (line)
                {
                    image.sprite = null; image.type = Image.Type.Simple; SetTint(image, FightUI.GoldBorder);
                }
                else SetTint(image, Color.clear);
                // Keep original Mask graphics and hit areas intact. Decorative
                // scan/corner/glow images no longer catch the pointer.
                if (mask == null && !outside)
                    image.raycastTarget = scrollbar || scrollHitArea ? pair.Value.OriginalRaycast : false;
                image.canvasRenderer.SetColor(Color.white);
            }
            foreach (var state in controls.Values)
            {
                if (state.Control == null || state.Surface == null) continue;
                state.Apply();
                if (state.InputOutline != null && state.InputOutline.effectColor != FightUI.Danger)
                    state.InputOutline.effectColor = FightUI.Border;
                if (state.Frame != null)
                {
                    // Native input LayoutGroups must never consume the focus
                    // frame as a row or shrink it to zero at the right edge.
                    var rect = state.Frame.rectTransform;
                    if (rect.parent == state.Control.transform) FightUI.Stretch(rect);
                }
            }
            foreach (var pair in texts)
            {
                var text = pair.Key;
                if (text == null) continue;
                var control = text.GetComponentInParent<Selectable>();
                Color color = text == title ? FightUI.Gold : FightUI.Foreground;
                if (control is TMP_InputField input && input.placeholder == text) color = FightUI.Muted;
                else if (control != null && controls.TryGetValue(control, out var state))
                {
                    color = !control.IsInteractable() ? FightUI.Muted : state.Primary ? FightUI.CyanText
                        : control is TMP_InputField || control is InputField ? FightUI.Foreground : FightUI.Gold;
                    if (state.SwitchGraphic != null)
                    {
                        color = control.IsInteractable() ? FightUI.Foreground : FightUI.Muted;
                        text.alignment = TextAlignmentOptions.Left;
                        var margin = text.margin; margin.z = Mathf.Max(margin.z, 78); text.margin = margin;
                    }
                }
                var selection = text.GetComponentInParent<MDPro3.UI.Popup.SelectionToggle_PopupSelectionItem>();
                if (selection != null && selection.color == "r") color = FightUI.Danger;
                else if (selection != null && (selection.color == "b" || selection.color == "g")) color = FightUI.CyanText;
                SetTint(text, color);
                text.canvasRenderer.SetColor(new Color(1, 1, 1, control != null && !control.IsInteractable() ? .55f : 1));
            }
        }

        private static void SetTint(Graphic graphic, Color color)
        {
            graphic.color = color;
            var container = graphic.GetComponent<ColorContainerGraphic>();
            if (container != null) container.baseColor = color;
        }

        internal void Restore()
        {
            if (!applied) return;
            foreach (var state in images.Values) state.Restore();
            foreach (var state in texts.Values) state.Restore();
            foreach (var state in controls.Values) state.Restore();
            selectionLayout?.Restore();
            foreach (var decoration in decorations) if (decoration != null) decoration.SetActive(false);
            applied = false;
        }

        private void OnDisable() { Restore(); }

        // Selection menus retain a full-height transition root. Their native
        // flex spacers otherwise stretch the title/footer over the whole screen.
        private sealed class NativeSelectionLayout
        {
            private readonly RectTransform root, heading, message, viewport, cancel;
            private readonly VerticalLayoutGroup layout;
            private readonly bool expand;
            private readonly TextAnchor alignment;
            private readonly NativeLayoutHeight titleHeight, footerHeight;
            private readonly Vector3[] corners = new Vector3[4];
            internal NativeSelectionLayout(RectTransform content, ElementObjectManager manager)
            {
                root = content; layout = content.GetComponent<VerticalLayoutGroup>();
                if (layout != null) { expand = layout.childForceExpandHeight; alignment = layout.childAlignment; }
                heading = content.Find("TitleArea") as RectTransform;
                message = manager.GetElement<RectTransform>("MessageArea");
                var scroll = manager.GetElement<ScrollRect>("EntryButtonsScrollView");
                viewport = scroll == null ? null : scroll.viewport != null ? scroll.viewport : scroll.GetComponent<RectTransform>();
                cancel = manager.GetElement<RectTransform>("CancelButton");
                titleHeight = new NativeLayoutHeight(heading == null ? null : heading.GetComponent<LayoutElement>(), 104);
                var footer = content.Find("FooterButtonArea");
                footerHeight = new NativeLayoutHeight(footer == null ? null : footer.GetComponent<LayoutElement>(), 140);
            }
            internal void Apply(RectTransform surface)
            {
                if (layout == null || surface == null) return;
                layout.childForceExpandHeight = false; layout.childAlignment = TextAnchor.MiddleCenter;
                titleHeight.Apply(); footerHeight.Apply();
                float lower = float.PositiveInfinity, upper = float.NegativeInfinity;
                foreach (var rect in new[] { heading, message, viewport, cancel })
                {
                    if (rect == null || !rect.gameObject.activeInHierarchy || rect.rect.height <= 0) continue;
                    rect.GetWorldCorners(corners);
                    foreach (var corner in corners)
                    {
                        float y = root.InverseTransformPoint(corner).y;
                        lower = Mathf.Min(lower, y); upper = Mathf.Max(upper, y);
                    }
                }
                if (float.IsInfinity(lower) || upper <= lower) return;
                lower = Mathf.Max(root.rect.yMin + 8, lower - 24); upper = Mathf.Min(root.rect.yMax - 8, upper + 24);
                var minAnchor = new Vector2(0, .5f); var maxAnchor = new Vector2(1, .5f);
                if (surface.anchorMin != minAnchor) surface.anchorMin = minAnchor;
                if (surface.anchorMax != maxAnchor) surface.anchorMax = maxAnchor;
                if (surface.pivot != minAnchor) surface.pivot = minAnchor;
                var size = new Vector2(0, upper - lower);
                var position = new Vector2(0, (lower + upper) * .5f - root.rect.center.y);
                if (Vector2.Distance(surface.sizeDelta, size) > .01f) surface.sizeDelta = size;
                if (Vector2.Distance(surface.anchoredPosition, position) > .01f) surface.anchoredPosition = position;
            }
            internal void Restore()
            {
                if (layout != null) { layout.childForceExpandHeight = expand; layout.childAlignment = alignment; }
                titleHeight.Restore(); footerHeight.Restore();
            }
            private sealed class NativeLayoutHeight
            {
                private readonly LayoutElement element;
                private readonly float preferred, flexible, minimum;
                internal NativeLayoutHeight(LayoutElement source, float fallback)
                {
                    element = source;
                    preferred = source == null ? -1 : source.preferredHeight; flexible = source == null ? -1 : source.flexibleHeight;
                    minimum = source == null || source.minHeight < 0 ? fallback : source.minHeight;
                }
                internal void Apply() { if (element != null) { element.flexibleHeight = 0; element.preferredHeight = minimum; } }
                internal void Restore() { if (element != null) { element.preferredHeight = preferred; element.flexibleHeight = flexible; } }
            }
        }

        private sealed class NativeImage
        {
            internal bool Managed = true;
            internal Color OriginalColor => color;
            internal bool OriginalRaycast => raycast;
            private readonly Image image;
            private readonly Color color, baseColor;
            private readonly Material material;
            private readonly Sprite sprite;
            private readonly Image.Type type;
            private readonly bool raycast;
            private readonly Mask mask;
            private readonly bool showMask;
            internal NativeImage(Image value)
            {
                image = value; color = value.color; material = value.material; sprite = value.sprite; type = value.type; raycast = value.raycastTarget;
                baseColor = value.GetComponent<ColorContainerGraphic>()?.baseColor ?? color;
                mask = value.GetComponent<Mask>(); showMask = mask != null && mask.showMaskGraphic;
            }
            internal void Restore()
            {
                if (image == null || !Managed) return;
                image.color = color; image.material = material; image.sprite = sprite; image.type = type; image.raycastTarget = raycast;
                image.canvasRenderer.SetColor(Color.white);
                var container = image.GetComponent<ColorContainerGraphic>(); if (container != null) container.baseColor = baseColor;
                if (mask != null) mask.showMaskGraphic = showMask;
            }
        }

        private sealed class NativeText
        {
            internal readonly TextMeshProUGUI Text;
            private readonly TMP_FontAsset font;
            private readonly Material material;
            private readonly Color color, baseColor;
            private readonly float size, spacing;
            private readonly bool autoSize, gradient;
            private readonly TextAlignmentOptions alignment;
            private readonly Vector4 margin;
            internal NativeText(TextMeshProUGUI value)
            {
                Text = value; font = value.font; material = value.fontSharedMaterial; color = value.color; size = value.fontSize;
                spacing = value.characterSpacing; autoSize = value.enableAutoSizing; gradient = value.enableVertexGradient; alignment = value.alignment;
                margin = value.margin;
                baseColor = value.GetComponent<ColorContainerGraphic>()?.baseColor ?? color;
            }
            internal void ApplyTypography(bool title)
            {
                if (Text == null) return;
                Text.font = Program.instance.ui_.tmpFont; Text.fontSharedMaterial = Text.font.material;
                Text.enableVertexGradient = false; Text.enableAutoSizing = false;
                var input = Text.GetComponentInParent<TMP_InputField>();
                Text.fontSize = title ? 26 : input != null ? 20 : Text.GetComponentInParent<Selectable>() != null ? 22 : 20;
                Text.characterSpacing = title ? 1 : 0;
                if (input != null && input.lineType == TMP_InputField.LineType.SingleLine) Text.alignment = TextAlignmentOptions.Left;
            }
            internal void Restore()
            {
                if (Text == null) return;
                Text.font = font; Text.fontSharedMaterial = material; Text.color = color; Text.fontSize = size; Text.characterSpacing = spacing;
                Text.enableAutoSizing = autoSize; Text.enableVertexGradient = gradient; Text.alignment = alignment; Text.margin = margin; Text.canvasRenderer.SetColor(Color.white);
                var container = Text.GetComponent<ColorContainerGraphic>(); if (container != null) container.baseColor = baseColor;
            }
        }

        private sealed class NativeControl
        {
            internal readonly Selectable Control;
            internal Image Surface;
            internal FightFrame Frame;
            internal FightButtonFeedback Feedback;
            internal MDPro3.UI.SelectionToggle NativeToggle;
            internal FightToggleGraphic SwitchGraphic;
            internal FightSwitchFeedback SwitchFeedback;
            internal FightInputFeedback InputFeedback;
            internal Outline InputOutline;
            internal Color OutlineColor;
            internal bool OutlineEnabled, Primary;
            private readonly Graphic target;
            private readonly Selectable.Transition transition;
            private readonly ColorBlock colors;
            internal NativeControl(Selectable value) { Control = value; target = value.targetGraphic; transition = value.transition; colors = value.colors; }
            internal void Apply()
            {
                if (Control == null || Surface == null) return;
                Control.targetGraphic = Surface; Control.transition = Selectable.Transition.ColorTint;
                var tint = Control.colors; tint.normalColor = Color.white; tint.highlightedColor = new Color(1.26f, 1.28f, 1.3f);
                tint.pressedColor = FightUI.Hex("7A909C"); tint.selectedColor = new Color(1.18f, 1.15f, 1.1f);
                tint.disabledColor = new Color(.42f, .42f, .42f, .72f); tint.fadeDuration = .12f;
                if (Control.colors != tint) Control.colors = tint;
                if (Feedback != null) Feedback.enabled = true;
                if (InputFeedback != null) InputFeedback.enabled = true;
                if (Frame != null) Frame.enabled = SwitchGraphic == null;
                var outline = Surface.GetComponent<Outline>(); outline.effectColor = Primary ? FightUI.Cyan : FightUI.GoldBorder;
                if (SwitchGraphic != null)
                {
                    SwitchGraphic.Value = Control is UnityEngine.UI.Toggle toggle ? toggle.isOn : NativeToggle != null && NativeToggle.isOn;
                    SwitchFeedback.enabled = true;
                    Surface.sprite = null; Surface.type = Image.Type.Simple; Surface.color = Color.clear;
                }
                if (Primary) { Surface.sprite = FightSkin.ActionSurface; Surface.color = FightUI.ActionFill; }
            }
            internal void Restore()
            {
                if (Control == null) return;
                Control.targetGraphic = target; Control.transition = transition; Control.colors = colors;
                if (Feedback != null) Feedback.enabled = false;
                if (SwitchFeedback != null) SwitchFeedback.enabled = false;
                if (InputFeedback != null) { InputFeedback.enabled = false; if (Frame != null) Frame.enabled = false; }
                if (InputOutline != null) { InputOutline.effectColor = OutlineColor; InputOutline.enabled = OutlineEnabled; }
            }
        }
    }
}
