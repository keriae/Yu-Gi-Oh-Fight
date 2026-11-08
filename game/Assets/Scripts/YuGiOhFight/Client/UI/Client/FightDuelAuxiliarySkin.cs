using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YuGiOhFight.Client.UI.Client
{
    // Information drawers contain semantic card/stat/log colours. Theme only
    // their shell and owned toolbar controls, leaving the reading content alone.
    internal sealed class FightDuelAuxiliarySkin : MonoBehaviour
    {
        private readonly Dictionary<Image, bool> backings = new();
        private readonly Dictionary<Behaviour, bool> borders = new();
        private readonly List<FineButton> buttons = new();
        private readonly HashSet<Selectable> themedControls = new();
        private readonly List<FineScrollbar> scrollbars = new();
        private RectTransform surfaceRect, frameRect;
        private Image surface;
        private FightSceneDialogFrame frame;
        private Graphic heading;
        private Color headingColor;
        private bool active;
        private string scene;

        internal static void Attach(RectTransform window, Image[] backingImages, Graphic title, params Selectable[] controls)
        {
            if (window == null) return;
            var skin = window.GetComponent<FightDuelAuxiliarySkin>() ?? window.gameObject.AddComponent<FightDuelAuxiliarySkin>();
            skin.Bind(window, backingImages, title, controls);
        }

        private void Bind(RectTransform window, Image[] backingImages, Graphic title, Selectable[] controls)
        {
            bool interceptsPointer = false;
            foreach (var image in backingImages) if (image != null)
            {
                if (!backings.ContainsKey(image)) backings.Add(image, image.enabled);
                interceptsPointer |= image.enabled && image.raycastTarget;
            }
            foreach (var border in window.GetComponentsInChildren<FightFrame>(true))
                if (!borders.ContainsKey(border)) borders.Add(border, border.enabled);
            foreach (var border in window.GetComponentsInChildren<FightOutline>(true))
                if (!borders.ContainsKey(border)) borders.Add(border, border.enabled);
            foreach (var control in controls)
                if (control != null && themedControls.Add(control)) buttons.Add(new FineButton(control));
            // CardDescription binds its exact bands in Awake; the regular HUD
            // can append its own backing/frame later without creating a second
            // skin or losing the earlier semantic exclusions.
            if (surface != null) { surface.raycastTarget |= interceptsPointer; return; }
            heading = title; headingColor = title != null ? title.color : Color.white;
            surfaceRect = Decoration(window, "Fight auxiliary scene surface"); surfaceRect.SetAsFirstSibling();
            surface = surfaceRect.gameObject.AddComponent<Image>(); surface.raycastTarget = interceptsPointer;
            frameRect = Decoration(window, "Fight auxiliary scene frame");
            frame = frameRect.gameObject.AddComponent<FightSceneDialogFrame>(); frame.raycastTarget = false;
            foreach (var scrollbar in window.GetComponentsInChildren<Scrollbar>(true)) scrollbars.Add(new FineScrollbar(scrollbar));
            surfaceRect.gameObject.SetActive(false); frameRect.gameObject.SetActive(false);
        }

        private static RectTransform Decoration(Transform parent, string name)
        {
            var rect = FightUI.Node(parent, name, 0, 0, 1, 1); FightUI.Stretch(rect);
            rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true; return rect;
        }

        private void LateUpdate()
        {
            if (surface == null) return;
            if (!FightDuelPopupSkin.SceneThemeActive) { Restore(); return; }
            string id = Runtime.Program.instance.ocgcore.DuelBGManager.ActiveSceneId;
            var palette = ScenePalette.For(id);
            if (palette == null) { Restore(); return; }
            bool changed = !active || scene != id; active = true; scene = id;
            surfaceRect.gameObject.SetActive(true); frameRect.gameObject.SetActive(true);
            foreach (var pair in backings) if (pair.Key != null) pair.Key.enabled = false;
            foreach (var pair in borders) if (pair.Key != null) pair.Key.enabled = false;
            if (changed)
            {
                surface.sprite = FightSkin.Surface; surface.type = Image.Type.Sliced;
                surface.material = null; surface.pixelsPerUnitMultiplier = 1;
                surface.color = palette.Panel;
                frame.Palette = palette; frame.SetVerticesDirty();
            }
            if (heading != null) heading.color = palette.Heading;
            foreach (var button in buttons) button.Apply(id, palette);
            foreach (var scrollbar in scrollbars) scrollbar.Apply(palette);
        }

        private void Restore()
        {
            if (!active) return;
            foreach (var pair in backings) if (pair.Key != null) pair.Key.enabled = pair.Value;
            foreach (var pair in borders) if (pair.Key != null) pair.Key.enabled = pair.Value;
            if (heading != null) heading.color = headingColor;
            foreach (var button in buttons) button.Restore();
            foreach (var scrollbar in scrollbars) scrollbar.Restore();
            surfaceRect.gameObject.SetActive(false); frameRect.gameObject.SetActive(false);
            active = false; scene = null;
        }
        private void OnDisable() => Restore();

        private sealed class FineButton
        {
            private readonly Selectable control;
            private readonly Graphic target;
            private readonly ColorBlock colors;
            private readonly Selectable.Transition transition;
            private readonly Image native, plate;
            private readonly bool nativeEnabled;
            private readonly FightSceneDialogFrame border;
            private readonly FightSceneDialogControl feedback;
            private readonly Dictionary<Graphic, Color> labels = new();
            private readonly Dictionary<Image, Color> icons = new();
            internal FineButton(Selectable source)
            {
                control = source; target = source.targetGraphic; colors = source.colors; transition = source.transition;
                native = source.GetComponent<Image>(); nativeEnabled = native != null && native.enabled;
                foreach (var label in source.GetComponentsInChildren<TextMeshProUGUI>(true)) labels.Add(label, label.color);
                foreach (var label in source.GetComponentsInChildren<Text>(true)) labels.Add(label, label.color);
                foreach (var icon in source.GetComponentsInChildren<Image>(true))
                    if (icon != native) icons.Add(icon, icon.color);
                var material = Decoration(source.transform, "Fight auxiliary button material"); material.SetAsFirstSibling();
                plate = material.gameObject.AddComponent<Image>(); plate.raycastTarget = true;
                var edge = Decoration(source.transform, "Fight auxiliary button frame");
                border = edge.gameObject.AddComponent<FightSceneDialogFrame>(); border.ControlFrame = true; border.raycastTarget = false;
                feedback = source.gameObject.AddComponent<FightSceneDialogControl>(); feedback.Bind(source, border); feedback.enabled = false;
                plate.gameObject.SetActive(false); border.gameObject.SetActive(false);
            }
            internal void Apply(string id, ScenePalette palette)
            {
                if (control == null) return;
                plate.gameObject.SetActive(true); border.gameObject.SetActive(true);
                if (native != null) native.enabled = false;
                plate.sprite = FightSkin.ButtonSurface; plate.type = Image.Type.Sliced;
                plate.pixelsPerUnitMultiplier = 1; plate.color = palette.Raised;
                control.targetGraphic = plate; control.transition = Selectable.Transition.ColorTint;
                var tint = colors; tint.normalColor = Color.white; tint.highlightedColor = new Color(1.1f,1.1f,1.1f);
                tint.selectedColor = new Color(1.13f,1.13f,1.13f); tint.pressedColor = new Color(.79f,.79f,.79f);
                tint.disabledColor = new Color(.48f,.48f,.48f,.6f); tint.fadeDuration = .12f; control.colors = tint;
                foreach (var label in labels) if (label.Key != null)
                    label.Key.color = label.Value == FightUI.Danger ? FightUI.Danger : control.IsInteractable() ? palette.Heading : palette.Muted;
                foreach (var icon in icons) if (icon.Key != null) icon.Key.color = control.IsInteractable() ? palette.Accent : palette.Muted;
                feedback.enabled = true; feedback.Refresh(palette);
            }
            internal void Restore()
            {
                if (control == null) return;
                control.targetGraphic = target; control.transition = transition; control.colors = colors;
                if (native != null) native.enabled = nativeEnabled;
                foreach (var label in labels) if (label.Key != null) label.Key.color = label.Value;
                foreach (var icon in icons) if (icon.Key != null) icon.Key.color = icon.Value;
                feedback.enabled = false; plate.gameObject.SetActive(false); border.gameObject.SetActive(false);
            }
        }

        internal sealed class FineScrollbar
        {
            private readonly Scrollbar control;
            private readonly Graphic target;
            private readonly ColorBlock colors;
            private readonly Image nativeTrack, nativeThumb, track, thumb, pointer;
            private readonly bool trackEnabled, thumbEnabled;
            internal FineScrollbar(Scrollbar source, string decorationPrefix = "Fight auxiliary")
            {
                control = source; target = source.targetGraphic; colors = source.colors;
                nativeTrack = source.GetComponent<Image>(); trackEnabled = nativeTrack != null && nativeTrack.enabled;
                nativeThumb = source.handleRect != null ? source.handleRect.GetComponent<Image>() : null;
                thumbEnabled = nativeThumb != null && nativeThumb.enabled;
                var hit = Decoration(source.transform, decorationPrefix + " scrollbar pointer"); hit.SetAsFirstSibling();
                pointer = hit.gameObject.AddComponent<Image>(); pointer.color = Color.clear;
                pointer.raycastTarget = nativeTrack != null && nativeTrack.raycastTarget;
                track = Thin(source.transform, source, decorationPrefix + " scrollbar track");
                thumb = source.handleRect != null ? Thin(source.handleRect, source, decorationPrefix + " scrollbar thumb") : null;
                pointer.gameObject.SetActive(false); track.gameObject.SetActive(false);
                if (thumb != null) thumb.gameObject.SetActive(false);
            }
            private static Image Thin(Transform parent, Scrollbar control, string name)
            {
                var rect = Decoration(parent, name);
                bool vertical = control.direction == Scrollbar.Direction.BottomToTop || control.direction == Scrollbar.Direction.TopToBottom;
                rect.anchorMin = vertical ? new Vector2(.5f, 0) : new Vector2(0, .5f);
                rect.anchorMax = vertical ? new Vector2(.5f, 1) : new Vector2(1, .5f);
                rect.sizeDelta = vertical ? new Vector2(3, 0) : new Vector2(0, 3);
                rect.anchoredPosition = Vector2.zero;
                var image = rect.gameObject.AddComponent<Image>(); image.raycastTarget = false; return image;
            }
            internal void Apply(ScenePalette palette)
            {
                if (control == null) return;
                if (nativeTrack != null) nativeTrack.enabled = false;
                if (nativeThumb != null) nativeThumb.enabled = false;
                pointer.gameObject.SetActive(true); track.gameObject.SetActive(true);
                var rail = palette.Edge; rail.a = .23f; track.color = rail;
                if (thumb == null) return;
                thumb.gameObject.SetActive(true); thumb.color = palette.Accent;
                control.targetGraphic = thumb;
                var tint = colors; tint.normalColor = Color.white; tint.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
                tint.selectedColor = tint.highlightedColor; tint.pressedColor = new Color(.8f, .8f, .8f);
                tint.disabledColor = new Color(.4f, .4f, .4f, .6f); control.colors = tint;
            }
            internal void Restore()
            {
                if (control == null) return;
                if (nativeTrack != null) nativeTrack.enabled = trackEnabled;
                if (nativeThumb != null) nativeThumb.enabled = thumbEnabled;
                control.targetGraphic = target; control.colors = colors;
                pointer.gameObject.SetActive(false); track.gameObject.SetActive(false);
                if (thumb != null) thumb.gameObject.SetActive(false);
            }
        }
    }
}
