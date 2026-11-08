using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YuGiOhFight.Client.UI.Client
{
    // Replaces only the native action's opaque circular art. The same Button,
    // callbacks, response list and final show scale remain authoritative.
    internal sealed class FightDuelActionComposition : IDisposable
    {
        private readonly DuelButton action;
        private readonly Button control;
        private readonly Image native, plate, symbol;
        private readonly bool nativeEnabled;
        private readonly Graphic target;
        private readonly Selectable.Transition transition;
        private readonly ColorBlock colors;
        private readonly RectTransform root, plateRect, symbolRect, frameRect;
        private readonly RectState rootState, captionState, textRectState;
        private readonly TextState textState;
        private readonly FightSceneDialogFrame frame;
        private Atlas atlas;
        private bool active;
        private string scene;
        internal static bool GlyphsAvailable => Atlas.cached != null && Atlas.cached.Glyphs[0] != null;

        internal FightDuelActionComposition(DuelButton source)
        {
            action = source; control = source.GetComponent<Button>(); native = source.GetComponent<Image>();
            nativeEnabled = native != null && native.enabled;
            if (control == null || native == null) return;
            target = control.targetGraphic; transition = control.transition; colors = control.colors;
            root = source.transform as RectTransform; rootState = new RectState(root);
            captionState = new RectState(source.bg != null ? source.bg.transform as RectTransform : null);
            textRectState = new RectState(source.text != null ? source.text.rectTransform : null); textState = new TextState(source.text);
            plateRect = Decoration(source.transform, "material"); plateRect.SetAsFirstSibling();
            plate = plateRect.gameObject.AddComponent<Image>(); plate.raycastTarget = true;
            symbolRect = Decoration(source.transform, "native symbol"); symbolRect.SetSiblingIndex(1);
            symbolRect.anchorMin = symbolRect.anchorMax = symbolRect.pivot = Vector2.one * .5f;
            symbolRect.sizeDelta = new Vector2(56, 56); symbolRect.anchoredPosition = Vector2.zero;
            symbol = symbolRect.gameObject.AddComponent<Image>(); symbol.raycastTarget = false;
            symbol.preserveAspect = true;
            frameRect = Decoration(source.transform, "fine edge");
            frame = frameRect.gameObject.AddComponent<FightSceneDialogFrame>(); frame.ControlFrame = true; frame.raycastTarget = false;
            plateRect.gameObject.SetActive(false); symbolRect.gameObject.SetActive(false); frameRect.gameObject.SetActive(false);
        }

        private static RectTransform Decoration(Transform parent, string name)
        {
            var rect = FightUI.Node(parent, "Fight scene action composition " + name, 0, 0, 1, 1);
            FightUI.Stretch(rect); rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true; return rect;
        }

        internal bool Apply(string id, ScenePalette palette, float emphasis)
        {
            if (control == null || native == null || root == null || palette == null) { Restore(); return false; }
            int slot = (int)action.type;
            if (slot < 0 || slot >= 13) { Restore(); return false; }
            // The authored BG and Text are direct siblings under DuelButton.
            // Avoid world/inverse conversion while its show scale is zero.
            var caption = captionState.Rect;
            if (caption != null && caption.parent != root
                || action.text != null && action.text.transform.parent != root && action.text.transform.parent != caption)
            { Restore(); return false; }
            if (atlas == null) atlas = Atlas.Acquire();
            if (atlas == null || atlas.Glyphs[slot] == null) { Restore(); return false; }
            bool changed = !active || scene != id;
            active = true; scene = id;
            native.enabled = false;
            plateRect.gameObject.SetActive(true); symbolRect.gameObject.SetActive(true); frameRect.gameObject.SetActive(true);
            if (changed)
            {
                root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 96);
                root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 96);
                var desired = new Vector2(root.rect.center.x, root.rect.yMin - 19);
                if (caption != null)
                {
                    PlaceCaption(caption, desired);
                }
                if (action.text != null)
                {
                    var textRect = action.text.rectTransform;
                    // The native hint may intentionally be empty for icon
                    // actions. Preserve it verbatim instead of adding copy.
                    if (caption != null && textRect.parent == caption) FightUI.Stretch(textRect);
                    else PlaceCaption(textRect, desired);
                    action.text.margin = new Vector4(4, 0, 4, 0); action.text.alignment = TextAlignmentOptions.Center;
                    action.text.fontSize = action.text.fontSizeMax = 20; action.text.fontSizeMin = 16; action.text.enableAutoSizing = true;
                    action.text.enableWordWrapping = false; action.text.overflowMode = TextOverflowModes.Overflow; action.text.characterSpacing = 0;
                }
                plate.type = Image.Type.Sliced; plate.pixelsPerUnitMultiplier = 1;
                frame.Palette = palette; frame.SetVerticesDirty();
            }
            control.transition = Selectable.Transition.ColorTint; control.targetGraphic = plate;
            plate.sprite = FightSkin.ButtonSurface;
            plate.color = action.type == ButtonType.Decide ? palette.Action : palette.Raised;
            plate.overrideSprite = null; plate.material = null;
            var tint = colors; tint.normalColor = Color.white; tint.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
            tint.selectedColor = new Color(1.15f, 1.15f, 1.15f); tint.pressedColor = new Color(.76f, .76f, .76f);
            tint.disabledColor = new Color(.42f, .42f, .42f, .65f); tint.fadeDuration = .12f;
            if (control.colors != tint) control.colors = tint;
            symbol.sprite = atlas.Glyphs[slot]; symbol.color = control.IsInteractable() ? palette.Accent : palette.Muted;
            frame.Emphasis = emphasis; frame.canvasRenderer.SetAlpha(control.IsInteractable() ? 1 : .38f);
            if (action.text != null) action.text.color = control.IsInteractable() ? palette.Heading : palette.Muted;
            return true;
        }

        private void PlaceCaption(RectTransform rect, Vector2 rootCentre)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.sizeDelta = new Vector2(116, 28); rect.anchoredPosition = rootCentre - root.rect.center;
        }

        internal void Restore()
        {
            if (active)
            {
                if (native != null) native.enabled = nativeEnabled;
                if (control != null) { control.targetGraphic = target; control.transition = transition; control.colors = colors; }
                // Root position/scale belong to projection and show/hide tweens.
                rootState?.Restore(true); captionState?.Restore(); textRectState?.Restore(); textState?.Restore();
                if (plateRect != null) plateRect.gameObject.SetActive(false);
                if (symbolRect != null) symbolRect.gameObject.SetActive(false);
                if (frameRect != null) frameRect.gameObject.SetActive(false);
                active = false; scene = null;
            }
            if (symbol != null) symbol.sprite = null;
            if (atlas != null) { Atlas.Release(atlas); atlas = null; }
        }
        public void Dispose()
        {
            Restore();
            foreach (var rect in new[] { plateRect, symbolRect, frameRect }) if (rect != null) UnityEngine.Object.Destroy(rect.gameObject);
        }

        private sealed class RectState
        {
            internal readonly RectTransform Rect;
            private readonly Vector2 min, max, pivot, size, position;
            internal RectState(RectTransform rect)
            { Rect = rect; if (rect != null) { min = rect.anchorMin; max = rect.anchorMax; pivot = rect.pivot; size = rect.sizeDelta; position = rect.anchoredPosition; } }
            internal void Restore(bool keepPosition = false)
            {
                if (Rect == null) return;
                var current = Rect.anchoredPosition; Rect.anchorMin = min; Rect.anchorMax = max; Rect.pivot = pivot; Rect.sizeDelta = size;
                Rect.anchoredPosition = keepPosition ? current : position;
            }
        }
        private sealed class TextState
        {
            private readonly TextMeshProUGUI text;
            private readonly float size, min, max, spacing;
            private readonly bool fit, wrap;
            private readonly TextOverflowModes overflow;
            private readonly TextAlignmentOptions alignment;
            private readonly Vector4 margin;
            private readonly Color color;
            internal TextState(TextMeshProUGUI value)
            {
                text = value;
                if (value == null) return;
                size = value.fontSize; min = value.fontSizeMin; max = value.fontSizeMax; spacing = value.characterSpacing;
                fit = value.enableAutoSizing; wrap = value.enableWordWrapping; overflow = value.overflowMode;
                alignment = value.alignment; margin = value.margin; color = value.color;
            }
            internal void Restore()
            {
                if (text == null) return;
                text.fontSize = size; text.fontSizeMin = min; text.fontSizeMax = max; text.characterSpacing = spacing;
                text.enableAutoSizing = fit; text.enableWordWrapping = wrap; text.overflowMode = overflow;
                text.alignment = alignment; text.margin = margin; text.color = color;
            }
        }

        private sealed class Atlas
        {
            internal static Atlas cached;
            private static float retryAt;
            private readonly Texture2D texture;
            internal readonly Sprite[] Glyphs = new Sprite[13];
            private int references;
            private Atlas(Texture2D source)
            {
                texture = source;
                // Nonzero-alpha bounds of the unchanged atlas, with four
                // pixels of padding. Empty cell space must not shrink symbols.
                var crops = new[]
                {
                    new Rect(18, 305, 62, 52), new Rect(109, 323, 71, 25), new Rect(203, 319, 75, 29),
                    new Rect(301, 308, 74, 57), new Rect(24, 208, 52, 64), new Rect(116, 212, 56, 56),
                    new Rect(216, 203, 52, 69), new Rect(302, 217, 70, 43), new Rect(19, 116, 62, 56),
                    new Rect(118, 111, 56, 61), new Rect(203, 111, 74, 76), new Rect(312, 107, 52, 69),
                    new Rect(14, 25, 70, 43)
                };
                for (int i = 0; i < Glyphs.Length; i++)
                    Glyphs[i] = Sprite.Create(texture, crops[i], Vector2.one * .5f, 100);
            }
            internal static Atlas Acquire()
            {
                if (cached != null) { cached.references++; return cached; }
                if (Time.unscaledTime < retryAt) return null;
                retryAt = Time.unscaledTime + 1;
                string path = Path.Combine(Runtime.Program.PATH_DATA, "ClientUI", "duel-scenes", "action-glyphs-v2.png");
                if (!File.Exists(path)) return null;
                Texture2D image = null;
                try
                {
                    image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!ImageConversion.LoadImage(image, File.ReadAllBytes(path), true) || image.width != 384 || image.height != 384)
                        throw new InvalidDataException("Unexpected action symbol atlas");
                    image.name = "Scene action native symbols"; image.wrapMode = TextureWrapMode.Clamp; image.filterMode = FilterMode.Bilinear;
                    cached = new Atlas(image) { references = 1 }; return cached;
                }
                catch (Exception error)
                {
                    if (image != null) UnityEngine.Object.Destroy(image);
                    Debug.LogWarning("[FightClient] action symbols unavailable: " + error.GetType().Name); return null;
                }
            }
            internal static void Release(Atlas source)
            {
                if (--source.references > 0) return;
                if (cached == source) { cached = null; retryAt = 0; }
                foreach (var sprite in source.Glyphs) if (sprite != null) UnityEngine.Object.Destroy(sprite);
                if (source.texture != null) UnityEngine.Object.Destroy(source.texture);
            }
        }
    }
}
