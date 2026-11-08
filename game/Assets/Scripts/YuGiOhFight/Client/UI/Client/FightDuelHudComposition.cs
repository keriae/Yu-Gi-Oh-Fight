using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YuGiOhFight.Client.UI.Client
{
    // A reversible composition over the authored HUD. Original Buttons, LP
    // text, avatar materials, badges and animation parents stay authoritative.
    internal sealed class FightDuelHudComposition : IDisposable
    {
        private readonly List<PlayerPlate> players = new();
        private readonly List<HudButton> buttons = new();
        private readonly List<RectState> speedLayout = new();
        private readonly Image speedBacking;
        private readonly bool speedBackingEnabled;
        private readonly Sprite[] glyphs = new Sprite[12];
        private Texture2D glyphTexture;
        private bool loaded, active;
        private float glyphRetryAt;
        private string scene;

        internal FightDuelHudComposition(ServantUI.OcgCoreUI ui)
        {
            players.Add(new PlayerPlate(ui, 0)); players.Add(new PlayerPlate(ui, 1));
            if (ui.Buttons == null) return;
            foreach (var control in ui.Buttons.GetComponentsInChildren<Button>(true))
            {
                int slot = control.name switch
                {
                    "ButtonSetting" or "ButtonSettings" => 0,
                    "ButtonLog" => 1, "ButtonAttachedEffect" => 2,
                    "ButtonInfo" => 3, "ButtonTiming" => 4,
                    "ButtonStop" or "ButtonPlay" or "ButtonAcc" or "ButtonNor" => -2,
                    _ => -1
                };
                if (slot != -1) buttons.Add(new HudButton(control, slot, ui));
            }
            var speed = ui.Buttons.transform.Find("Buttons ControlSpeed") as RectTransform;
            if (speed != null)
            {
                speedLayout.Add(new RectState(speed));
                foreach (var child in new[] { "PP", "FF" })
                    if (speed.Find(child) is RectTransform rect) speedLayout.Add(new RectState(rect));
                speedBacking = speed.GetComponent<Image>();
                speedBackingEnabled = speedBacking != null && speedBacking.enabled;
            }
        }

        internal void Refresh()
        {
            if (!FightDuelPopupSkin.SceneThemeActive) { Restore(); return; }
            var id = Runtime.Program.instance.ocgcore.DuelBGManager.ActiveSceneId;
            var palette = ScenePalette.For(id);
            if (palette == null) { Restore(); return; }
            bool glyphsArrived = LoadGlyphs();
            bool changed = !active || id != scene || glyphsArrived;
            active = true; scene = id;
            foreach (var player in players) player.Apply(id, palette, changed);
            if (speedLayout.Count > 0)
            {
                for (int i = 0; i < speedLayout.Count; i++)
                {
                    var rect = speedLayout[i].Rect;
                    if (rect == null) continue;
                    if (i == 0)
                    {
                        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0);
                        rect.sizeDelta = new Vector2(112, 50); rect.anchoredPosition = new Vector2(-24, 102);
                    }
                    else
                    {
                        rect.anchorMin = rect.anchorMax = new Vector2(0, .5f); rect.pivot = Vector2.one * .5f;
                        rect.sizeDelta = new Vector2(48, 44); rect.anchoredPosition = new Vector2(i == 1 ? 28 : 84, 0);
                    }
                }
            }
            if (speedBacking != null) speedBacking.enabled = false;
            foreach (var button in buttons) button.Apply(id, palette, glyphs, changed);
        }

        private bool LoadGlyphs()
        {
            if (loaded || Time.unscaledTime < glyphRetryAt) return false;
            glyphRetryAt = Time.unscaledTime + 2;
            Texture2D candidate = null;
            try
            {
                var path = Path.Combine(Runtime.Program.PATH_DATA, "ClientUI", "duel-scenes", "hud-glyphs-v2.png");
                if (!File.Exists(path)) return false;
                candidate = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(candidate, File.ReadAllBytes(path), true)
                    || candidate.width != 320 || candidate.height != 240)
                    throw new InvalidDataException("Unexpected HUD symbol atlas");
                candidate.name = "Scene HUD native symbols";
                candidate.wrapMode = TextureWrapMode.Clamp; candidate.filterMode = FilterMode.Bilinear;
                float width = candidate.width / 4f, height = candidate.height / 3f;
                for (int i = 0; i < glyphs.Length; i++)
                    glyphs[i] = Sprite.Create(candidate, new Rect(i % 4 * width, (2 - i / 4) * height, width, height), Vector2.one * .5f, 100);
                glyphTexture = candidate; loaded = true;
                return true;
            }
            catch (Exception error)
            {
                foreach (var glyph in glyphs) if (glyph != null) UnityEngine.Object.Destroy(glyph);
                Array.Clear(glyphs, 0, glyphs.Length);
                if (candidate != null) UnityEngine.Object.Destroy(candidate);
                Debug.LogWarning("[FightClient] HUD symbols unavailable: " + error.GetType().Name);
                return false;
            }
        }

        internal void Restore()
        {
            if (!active) return;
            foreach (var player in players) player.Restore();
            foreach (var button in buttons) button.Restore();
            foreach (var state in speedLayout) state.Restore();
            if (speedBacking != null) speedBacking.enabled = speedBackingEnabled;
            active = false; scene = null;
        }

        public void Dispose()
        {
            Restore();
            foreach (var glyph in glyphs) if (glyph != null) UnityEngine.Object.Destroy(glyph);
            if (glyphTexture != null) UnityEngine.Object.Destroy(glyphTexture);
        }

        private static RectTransform Decoration(Transform parent, string name)
        {
            var rect = FightUI.Node(parent, "Fight composed HUD " + name, 0, 0, 1, 1);
            FightUI.Stretch(rect); rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            return rect;
        }

        private sealed class PlayerPlate
        {
            private readonly int side;
            private readonly RectTransform root;
            private readonly Image native, nativeName, surface;
            private readonly bool nativeEnabled, nameEnabled;
            private readonly FightSceneDialogFrame frame;
            private readonly List<RectState> rects = new();
            private readonly TextMeshProUGUI name, points, label;
            private readonly TextState nameState, pointsState, labelState;
            private readonly RectTransform materialRect, frameRect;
            private readonly RectTransform nameBase;

            internal PlayerPlate(ServantUI.OcgCoreUI ui, int player)
            {
                side = player;
                points = player == 0 ? ui.TextPlayer0LP : ui.TextPlayer1LP;
                name = player == 0 ? ui.TextPlayer0Name : ui.TextPlayer1Name;
                root = points != null ? points.transform.parent as RectTransform : null;
                if (root == null) return;
                native = root.GetComponent<Image>(); nativeEnabled = native != null && native.enabled;
                nativeName = root.Find("NameBase")?.GetComponent<Image>(); nameEnabled = nativeName != null && nativeName.enabled;
                nameBase = nativeName != null ? nativeName.rectTransform : root.Find("NameBase") as RectTransform;
                label = root.Find("LP")?.GetComponent<TextMeshProUGUI>();
                nameState = new TextState(name); pointsState = new TextState(points); labelState = new TextState(label);
                rects.Add(new RectState(root));
                rects.Add(new RectState((player == 0 ? ui.AvatarPlayer0 : ui.AvatarPlayer1)?.rectTransform));
                rects.Add(new RectState(points.rectTransform));
                if (name != null) rects.Add(new RectState(name.rectTransform));
                if (label != null) rects.Add(new RectState(label.rectTransform));
                if (nameBase != null) rects.Add(new RectState(nameBase));
                materialRect = Decoration(root, "player material"); materialRect.SetAsFirstSibling();
                surface = materialRect.gameObject.AddComponent<Image>(); surface.raycastTarget = false;
                frameRect = Decoration(root, "player filigree"); frameRect.SetSiblingIndex(1);
                frame = frameRect.gameObject.AddComponent<FightSceneDialogFrame>(); frame.ControlFrame = true; frame.raycastTarget = false;
                materialRect.gameObject.SetActive(false); frameRect.gameObject.SetActive(false);
            }

            internal void Apply(string id, ScenePalette palette, bool changed)
            {
                if (root == null) return;
                if (native != null) native.enabled = false;
                if (nativeName != null) nativeName.enabled = false;
                materialRect.gameObject.SetActive(true); frameRect.gameObject.SetActive(true);
                var artwork = FightDuelPopupSkin.DialogArtwork.Control(id, 2);
                if (changed || surface.sprite != (artwork ?? FightSkin.Surface))
                {
                    surface.sprite = artwork ?? FightSkin.Surface;
                    surface.type = Image.Type.Sliced; surface.pixelsPerUnitMultiplier = 2.4f;
                    surface.overrideSprite = null; surface.material = null; surface.color = artwork != null ? Color.white : palette.Panel;
                    frame.Palette = palette; frame.Emphasis = .2f; frame.SetVerticesDirty();
                }
                // Both prefab sides have different native anchors. Give every
                // child an explicit column within the plate, including the
                // NameBase parent, so LP digits never reach the avatar column.
                root.sizeDelta = new Vector2(304, 100);
                var avatar = rects[1].Rect;
                float direction = side == 0 ? 1 : -1;
                PlaceCentered(avatar, new Vector2(-104 * direction, 0), new Vector2(84, 84));
                PlaceCentered(nameBase, Vector2.zero, new Vector2(304, 100));
                PlaceCentered(points.rectTransform, new Vector2(74 * direction, -14), new Vector2(138, 44));
                points.alignment = side == 0 ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
                points.enableWordWrapping = false; points.overflowMode = TextOverflowModes.Truncate;
                points.enableAutoSizing = true; points.fontSizeMax = 34; points.fontSizeMin = 20;
                points.characterSpacing = .6f;
                if (label != null)
                {
                    PlaceCentered(label.rectTransform, new Vector2(-24 * direction, -14), new Vector2(36, 26));
                    label.fontSize = 18; label.alignment = TextAlignmentOptions.Center; label.color = palette.Muted;
                }
                if (name != null)
                {
                    PlaceCentered(name.rectTransform, new Vector2(50 * direction, 30), new Vector2(188, 28));
                    name.alignment = side == 0 ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
                    name.enableWordWrapping = false; name.overflowMode = TextOverflowModes.Ellipsis;
                    name.fontSizeMax = 25; name.fontSizeMin = 18; name.enableAutoSizing = true;
                    name.color = palette.Heading; name.characterSpacing = .3f;
                }
            }

            private static void PlaceCentered(RectTransform rect, Vector2 position, Vector2 size)
            {
                if (rect == null) return;
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
                rect.sizeDelta = size; rect.anchoredPosition = position;
            }

            internal void Restore()
            {
                if (root == null) return;
                if (native != null) native.enabled = nativeEnabled;
                if (nativeName != null) nativeName.enabled = nameEnabled;
                foreach (var state in rects) state.Restore();
                nameState.Restore(); pointsState.Restore(); labelState.Restore();
                materialRect.gameObject.SetActive(false); frameRect.gameObject.SetActive(false);
            }
        }

        private sealed class HudButton
        {
            private readonly Button control;
            private readonly ServantUI.OcgCoreUI ui;
            private readonly RectState rect;
            private readonly RectState badgeRect;
            private readonly TextMeshProUGUI badgeLabel;
            private readonly TextState badgeText;
            private readonly int slot;
            private readonly Image native, plate, symbol;
            private readonly bool nativeEnabled;
            private readonly Graphic nativeTarget;
            private readonly Selectable.Transition transition;
            private readonly ColorBlock colors;
            private readonly FightSceneDialogFrame frame;
            private readonly FightSceneDialogControl feedback;
            private readonly FightDuelControlFeedback oldFeedback;
            private readonly RectTransform plateRect, symbolRect, frameRect;

            internal HudButton(Button button, int order, ServantUI.OcgCoreUI owner)
            {
                control = button; ui = owner; slot = order; rect = new RectState(button.transform as RectTransform);
                badgeRect = new RectState(button.transform.Find("Badge") as RectTransform);
                badgeLabel = badgeRect.Rect != null ? badgeRect.Rect.GetComponentInChildren<TextMeshProUGUI>(true) : null;
                badgeText = new TextState(badgeLabel);
                native = button.GetComponent<Image>(); nativeEnabled = native != null && native.enabled;
                nativeTarget = button.targetGraphic; transition = button.transition; colors = button.colors;
                oldFeedback = button.GetComponent<FightDuelControlFeedback>();
                plateRect = Decoration(button.transform, "button material"); plateRect.SetAsFirstSibling();
                plate = plateRect.gameObject.AddComponent<Image>(); plate.raycastTarget = true;
                symbolRect = Decoration(button.transform, "native symbol"); symbolRect.SetSiblingIndex(1);
                symbolRect.anchorMin = symbolRect.anchorMax = Vector2.one * .5f; symbolRect.pivot = Vector2.one * .5f;
                symbolRect.sizeDelta = new Vector2(40, 40); symbolRect.anchoredPosition = Vector2.zero;
                symbol = symbolRect.gameObject.AddComponent<Image>(); symbol.raycastTarget = false;
                symbol.preserveAspect = true;
                frameRect = Decoration(button.transform, "button filigree");
                frame = frameRect.gameObject.AddComponent<FightSceneDialogFrame>(); frame.ControlFrame = true; frame.raycastTarget = false;
                feedback = button.gameObject.AddComponent<FightSceneDialogControl>(); feedback.Bind(button, frame); feedback.enabled = false;
                plateRect.gameObject.SetActive(false); symbolRect.gameObject.SetActive(false); frameRect.gameObject.SetActive(false);
            }

            private int GlyphIndex()
            {
                if (control.name == "ButtonTiming") return Controllers.OcgCore.chainCondition switch
                { Controllers.OcgCore.ChainCondition.No => 4, Controllers.OcgCore.ChainCondition.Smart => 5, _ => 6 };
                return control.name switch
                { "ButtonSetting" or "ButtonSettings" => 0,
                  "ButtonLog" => ui != null && ui.DuelLog != null && ui.DuelLog.showing ? 11 : 1, "ButtonInfo" => 2,
                  "ButtonAttachedEffect" => 3, "ButtonStop" => 7, "ButtonPlay" => 8, "ButtonNor" => 10, _ => 9 };
            }

            internal void Apply(string id, ScenePalette palette, Sprite[] glyphs, bool changed)
            {
                if (control == null) return;
                // Inactive replay controls may run their native OnEnable
                // layout after the scene arrives. Keep the compact hit box.
                if (slot >= 0)
                {
                    rect.Rect.sizeDelta = new Vector2(64, 64);
                    rect.Rect.anchoredPosition = new Vector2(-24 - slot * 72, 22);
                }
                else rect.Rect.sizeDelta = new Vector2(48, 44);
                if (changed)
                {
                    if (badgeRect.Rect != null)
                    {
                        badgeRect.Rect.anchorMin = badgeRect.Rect.anchorMax = badgeRect.Rect.pivot = Vector2.one;
                        badgeRect.Rect.sizeDelta = new Vector2(26, 24); badgeRect.Rect.anchoredPosition = new Vector2(5, 5);
                        if (badgeLabel != null) { badgeLabel.fontSizeMax = 16; badgeLabel.fontSizeMin = 10; badgeLabel.enableAutoSizing = true; }
                    }
                }
                if (glyphs[GlyphIndex()] == null) return;
                if (native != null) native.enabled = false;
                oldFeedback?.SetComposed(true);
                plateRect.gameObject.SetActive(true); symbolRect.gameObject.SetActive(true); frameRect.gameObject.SetActive(true);
                if (changed)
                {
                    plate.type = Image.Type.Sliced; plate.pixelsPerUnitMultiplier = 2.4f; plate.color = Color.white;
                    var tint = colors; tint.normalColor = Color.white; tint.highlightedColor = new Color(1.12f,1.12f,1.12f);
                    tint.selectedColor = tint.highlightedColor; tint.pressedColor = new Color(.75f,.75f,.75f);
                    tint.disabledColor = new Color(.42f,.42f,.42f,.65f); tint.fadeDuration = .12f; control.colors = tint;
                    frame.Palette = palette; frame.SetVerticesDirty();
                }
                bool latched = control.name == "ButtonLog" && ui != null && ui.DuelLog != null && ui.DuelLog.showing
                    || control.name == "ButtonNor" || control.name == "ButtonTiming"
                    && Controllers.OcgCore.chainCondition == Controllers.OcgCore.ChainCondition.All;
                // Native sprite-state updates continue on their original Image.
                // Clear SpriteSwap before assigning the owned material target.
                control.transition = Selectable.Transition.ColorTint; control.targetGraphic = plate;
                var artwork = FightDuelPopupSkin.DialogArtwork.Control(id, latched ? 0 : 1);
                plate.sprite = artwork ?? FightSkin.ButtonSurface;
                plate.color = artwork != null ? Color.white : latched ? palette.Action : palette.Raised;
                plate.overrideSprite = null; plate.material = null;
                symbol.sprite = glyphs[GlyphIndex()]; symbol.color = !control.IsInteractable() ? palette.Muted : latched ? palette.Accent : palette.Heading;
                feedback.enabled = true; feedback.Refresh(palette);
                if (latched && control.IsInteractable()) frame.Emphasis = Mathf.Max(frame.Emphasis, .64f);
            }

            internal void Restore()
            {
                if (control == null) return;
                rect.Restore(); badgeRect.Restore(); badgeText.Restore(); if (native != null) native.enabled = nativeEnabled;
                control.targetGraphic = nativeTarget; control.transition = transition; control.colors = colors;
                oldFeedback?.SetComposed(false); feedback.enabled = false;
                plateRect.gameObject.SetActive(false); symbolRect.gameObject.SetActive(false); frameRect.gameObject.SetActive(false);
            }
        }

        private sealed class RectState
        {
            internal readonly RectTransform Rect;
            private readonly Vector2 size, position, min, max, pivot;
            internal RectState(RectTransform rect) { Rect = rect; if (rect != null) { size = rect.sizeDelta; position = rect.anchoredPosition; min = rect.anchorMin; max = rect.anchorMax; pivot = rect.pivot; } }
            internal void Restore() { if (Rect != null) { Rect.anchorMin = min; Rect.anchorMax = max; Rect.pivot = pivot; Rect.sizeDelta = size; Rect.anchoredPosition = position; } }
        }

        private sealed class TextState
        {
            private readonly TextMeshProUGUI text;
            private readonly float size, min, max, spacing;
            private readonly bool autosize;
            private readonly Color color;
            private readonly TextAlignmentOptions alignment;
            private readonly TextOverflowModes overflow;
            private readonly bool wrapping;
            internal TextState(TextMeshProUGUI value)
            { text = value; if (value != null) { size = value.fontSize; min = value.fontSizeMin; max = value.fontSizeMax; autosize = value.enableAutoSizing; spacing = value.characterSpacing; color = value.color; alignment = value.alignment; overflow = value.overflowMode; wrapping = value.enableWordWrapping; } }
            internal void Restore()
            { if (text != null) { text.fontSize = size; text.fontSizeMin = min; text.fontSizeMax = max; text.enableAutoSizing = autosize; text.characterSpacing = spacing; text.color = color; text.alignment = alignment; text.overflowMode = overflow; text.enableWordWrapping = wrapping; } }
        }
    }
}
