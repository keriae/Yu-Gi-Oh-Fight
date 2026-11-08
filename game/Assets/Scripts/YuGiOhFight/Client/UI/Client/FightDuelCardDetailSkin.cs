using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YuGiOhFight.Client.UI.Client
{
    // Full-screen card readers are UIWidgets rather than either Popup family.
    // Only their backdrop, reading shells and navigation acquire scene chrome.
    internal sealed class FightDuelCardDetailSkin : MonoBehaviour
    {
        private readonly Dictionary<Image, ImageState> backgrounds = new();
        private readonly Dictionary<Image, ImageState> navigationIcons = new();
        private readonly Dictionary<Image, FightIcon> closeSymbols = new();
        private readonly Dictionary<Graphic, Color> shortcuts = new();
        private ScenePalette palette;
        private bool bound, applied;
        private RectTransform closeRect;

        internal static void Attach(UIWidgetFullScreen owner, bool expanded)
        {
            if (owner == null || !FightDuelPopupSkin.SceneThemeActive) return;
            var skin = owner.GetComponent<FightDuelCardDetailSkin>() ?? owner.gameObject.AddComponent<FightDuelCardDetailSkin>();
            skin.Bind(owner, expanded);
        }

        private void Bind(UIWidgetFullScreen owner, bool expanded)
        {
            if (bound) { Repaint(); return; }
            var manager = owner.GetComponent<Presentation.Elements.ElementObjectManager>();
            if (manager == null) return;
            palette = ScenePalette.For(Runtime.Program.instance.ocgcore.DuelBGManager.ActiveSceneId);
            var backdrop = manager.GetElement<RectTransform>("BG") ?? owner.transform.Find("Root/BG") as RectTransform;
            if (backdrop != null)
            {
                foreach (var image in backdrop.GetComponentsInChildren<Image>(true))
                {
                    // Back/arrow/controller glyphs retain their authored shape.
                    if (image.transform == backdrop || image.name == "FullScreenUIBg" || image.name == "BgEff"
                        || image.name == "CloseButton") backgrounds.Add(image, new ImageState(image));
                    else if (image.name == "IconBack")
                    {
                        navigationIcons.Add(image, new ImageState(image));
                        // The native circle-X bitmap contains baked lime RGB.
                        // Use the established close glyph so scene colour stays
                        // exact, retaining DeviceIcon and the parent's click.
                        var glyph = FightUI.Icon(image.transform, "close", 0, 0, 78, 78, palette.Accent);
                        FightUI.Stretch(glyph.rectTransform);
                        glyph.rectTransform.offsetMin = new Vector2(20, 20);
                        glyph.rectTransform.offsetMax = new Vector2(-20, -20);
                        glyph.raycastTarget = false;
                        closeSymbols.Add(image, glyph);
                    }
                }
                var shortcut = backdrop.Find("ShortCut");
                if (shortcut != null)
                    foreach (var text in shortcut.GetComponentsInChildren<TextMeshProUGUI>(true)) shortcuts.Add(text, text.color);
            }
            if (!expanded)
            {
                var window = manager.GetElement<RectTransform>("Window");
                if (window != null)
                {
                    foreach (var path in new[] { "StatusArea/TitleArea", "StatusArea/ParamatorArea",
                        "StatusArea/PendulumDescriptionArea", "StatusArea/DescriptionArea" })
                    {
                        var area = window.Find(path) as RectTransform;
                        if (area == null) continue;
                        var shells = new List<Image>();
                        foreach (var name in new[] { "Frame", "Base", "Line", "TitleBase" })
                        {
                            var node = area.Find(name);
                            if (node != null && node.TryGetComponent<Image>(out var image)) shells.Add(image);
                        }
                        string plateName = path.EndsWith("/TitleArea") ? "PlateTitle"
                            : path.EndsWith("/ParamatorArea") ? "PlateParamator"
                            : path.EndsWith("/PendulumDescriptionArea") ? "PlatePendulumDescription" : "PlateDescription";
                        var colorPlate = manager.GetElement<Image>(plateName);
                        if (colorPlate != null) shells.Add(colorPlate);
                        FightDuelAuxiliarySkin.Attach(area, shells.ToArray(), null);
                    }
                }
                // These are native 128x148 pager controls, separate from the
                // full-screen outside-click target. Keep callbacks and arrows.
                foreach (var name in new[] { "PrevButton", "NextButton", "SavePictureButton" })
                {
                    var control = manager.GetElement<Selectable>(name);
                    if (control == null) continue;
                    var chrome = new List<Image>();
                    if (name == "SavePictureButton")
                        foreach (var path in new[] { "Out", "Hover/Base", "SelectCursor/Offset/Cursor", "SelectCursor/Offset/Corner" })
                        {
                            var part = control.transform.Find(path);
                            if (part != null && part.TryGetComponent<Image>(out var image)) chrome.Add(image);
                        }
                    FightDuelAuxiliarySkin.Attach(control.transform as RectTransform,
                        chrome.ToArray(), null, control);
                }
            }
            else if (backdrop != null)
            {
                // A small visible close action complements the native outside
                // click and Escape path, including its zoom transition guard.
                var close = FightUI.Button(backdrop, "关闭", 0, 0, 96, 44, owner.Hide);
                closeRect = close.transform as RectTransform;
                closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = Vector2.one;
                closeRect.anchoredPosition = new Vector2(-28, -24);
                FightDuelAuxiliarySkin.Attach(closeRect, new Image[0], null, close);
            }
            bound = true;
            Repaint();
        }

        private void LateUpdate() => Repaint();
        private void Repaint()
        {
            if (!bound) return;
            if (!FightDuelPopupSkin.SceneThemeActive) { Restore(); return; }
            palette = ScenePalette.For(Runtime.Program.instance.ocgcore.DuelBGManager.ActiveSceneId);
            if (palette == null) { Restore(); return; }
            applied = true;
            if (closeRect != null) closeRect.gameObject.SetActive(true);
            foreach (var pair in backgrounds)
            {
                var image = pair.Key;
                if (image == null) continue;
                image.material = null; image.sprite = null; image.type = Image.Type.Simple;
                var color = palette.Shadow;
                color.a = image.name == "BG" ? .94f : 0;
                image.color = color;
                var container = image.GetComponent<Presentation.UI.ColorContainerGraphic>();
                if (container != null) container.baseColor = color;
            }
            foreach (var pair in navigationIcons)
            {
                if (pair.Key == null) continue;
                pair.Key.enabled = false;
                if (closeSymbols.TryGetValue(pair.Key, out var glyph) && glyph != null)
                { glyph.gameObject.SetActive(true); glyph.color = palette.Accent; }
                var container = pair.Key.GetComponent<Presentation.UI.ColorContainerGraphic>();
                if (container != null) container.baseColor = palette.Accent;
            }
            foreach (var pair in shortcuts) if (pair.Key != null) pair.Key.color = palette.Heading;
        }

        private void Restore()
        {
            if (!applied) return;
            foreach (var state in backgrounds.Values) state.Restore();
            foreach (var state in navigationIcons.Values) state.Restore();
            foreach (var glyph in closeSymbols.Values) if (glyph != null) glyph.gameObject.SetActive(false);
            foreach (var pair in shortcuts) if (pair.Key != null) pair.Key.color = pair.Value;
            if (closeRect != null) closeRect.gameObject.SetActive(false);
            applied = false;
        }
        private void OnDisable() => Restore();

        private sealed class ImageState
        {
            private readonly Image image;
            private readonly Sprite sprite;
            private readonly Material material;
            private readonly Image.Type type;
            private readonly Color color, baseColor;
            private readonly bool enabled;
            internal ImageState(Image source)
            {
                image = source; sprite = source.sprite; material = source.material;
                type = source.type; color = source.color;
                enabled = source.enabled;
                baseColor = source.GetComponent<Presentation.UI.ColorContainerGraphic>()?.baseColor ?? color;
            }
            internal void Restore()
            {
                if (image == null) return;
                image.sprite = sprite; image.material = material; image.type = type; image.color = color;
                image.enabled = enabled;
                var container = image.GetComponent<Presentation.UI.ColorContainerGraphic>();
                if (container != null) container.baseColor = baseColor;
            }
        }
    }
}
