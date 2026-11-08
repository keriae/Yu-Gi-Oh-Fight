using System;
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace YuGiOhFight.Client.UI.Client
{
    // Both popup families enter here after their native controls have been
    // initialized. Rules, selected cards, navigation and response delegates
    // remain on the original objects; this component owns only presentation.
    internal sealed class FightDuelPopupSkin : MonoBehaviour
    {
        private const string DecorationPrefix = "Fight scene dialog";
        private RectTransform content;
        private RectTransform panel;
        private RectTransform safeMount, safeParent;
        private readonly List<VisibleGraphic> visibleGraphics = new();
        private bool presented, fieldWasHidden, responsivePanel;
        private readonly Dictionary<FightFrame, bool> originalFrames = new();
        private PopupBase legacy;
        private Popup.Popup modern;
        private FightDuelNativePopupLayout nativePromptLayout;
        private FightDuelPopupLayout compactLayout;
        private Graphic title;
        private FightSceneDialogFrame frame;
        private Image surface, materialGrain;
        private Image focusBackdrop;
        private readonly Dictionary<Image, ImageState> images = new();
        private readonly Dictionary<Graphic, TextState> texts = new();
        private readonly Dictionary<Selectable, ControlState> controls = new();
        private readonly Dictionary<PopupDuelSelectCardItem, CardItemState> cardItems = new();
        private readonly Dictionary<Scrollbar, FightDuelAuxiliarySkin.FineScrollbar> cardScrollbars = new();
        private readonly List<GameObject> decorations = new();
        private readonly List<FightToggleGraphic> sceneSwitches = new();
        private ScenePalette palette;
        private string sceneId;
        private bool applied;
        private float discoverAt;
        private readonly Vector3[] corners = new Vector3[4];
        private bool GlassPrompt => !responsivePanel && (legacy is PopupDuel || nativePromptLayout?.Supported == true);

        internal static bool SceneThemeActive
        {
            get
            {
                var program = Runtime.Program.instance;
                if (program == null || program.ocgcore == null || program.currentServant != program.ocgcore) return false;
                var bg = program.ocgcore.DuelBGManager;
                return bg != null && bg.loaded && bg.Scene3DActive && ScenePalette.For(bg.ActiveSceneId) != null;
            }
        }

        internal static void Attach(PopupBase popup)
        {
            if (popup == null || !SceneThemeActive || popup.window == null) return;
            var skin = popup.GetComponent<FightDuelPopupSkin>() ?? popup.gameObject.AddComponent<FightDuelPopupSkin>();
            skin.legacy = popup;
            skin.Bind(popup.window, popup.title);
        }

        internal static void Attach(Popup.Popup popup)
        {
            if (popup == null || !SceneThemeActive) return;
            var manager = popup.GetComponent<Presentation.Elements.ElementObjectManager>();
            if (manager == null) return;
            var content = manager.GetElement<RectTransform>("Content");
            if (content == null) return;
            var skin = popup.GetComponent<FightDuelPopupSkin>() ?? popup.gameObject.AddComponent<FightDuelPopupSkin>();
            skin.modern = popup;
            if (skin.nativePromptLayout == null)
                skin.nativePromptLayout = new FightDuelNativePopupLayout(popup, content, manager);
            skin.Bind(content, manager.GetElement<TextMeshProUGUI>("TitleText"));
        }

        internal static void Attach(RectTransform content, Graphic heading)
        {
            if (content == null || !SceneThemeActive) return;
            var skin = content.GetComponent<FightDuelPopupSkin>() ?? content.gameObject.AddComponent<FightDuelPopupSkin>();
            skin.responsivePanel = true;
            skin.Bind(content, heading);
        }

        private void Bind(RectTransform root, Graphic heading)
        {
            if (content != null) return;
            content = root; title = heading;
            if (legacy != null)
            {
                compactLayout = new FightDuelPopupLayout(legacy);
                if (title == null) title = compactLayout.TitleText;
            }
            if (!responsivePanel)
            {
                CreateSafeMount();
                if (safeParent != null)
                {
                    var focus = Node(safeParent, "focus", true);
                    focus.SetAsFirstSibling();
                    focusBackdrop = focus.gameObject.AddComponent<Image>();
                    focusBackdrop.color = Color.clear;
                    focusBackdrop.raycastTarget = false;
                }
            }
            panel = Node(content, "panel", true);
            panel.SetAsFirstSibling();
            surface = panel.gameObject.AddComponent<Image>();
            surface.sprite = FightSkin.Surface; surface.type = Image.Type.Sliced;
            // Keep the original modal hit surfaces authoritative.
            surface.raycastTarget = true;
            var grain = Node(panel, "material grain", true);
            grain.offsetMin = Vector2.one * 6; grain.offsetMax = Vector2.one * -6;
            materialGrain = grain.gameObject.AddComponent<Image>();
            materialGrain.raycastTarget = false;
            frame = Node(panel, "architecture", true).gameObject.AddComponent<FightSceneDialogFrame>();
            frame.raycastTarget = false;
            Discover();
            Repaint();
        }

        private RectTransform Node(Transform parent, string name, bool stretch)
        {
            var rect = FightUI.Node(parent, DecorationPrefix + " " + name, 0, 0, 1, 1);
            rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            if (stretch) FightUI.Stretch(rect);
            decorations.Add(rect.gameObject);
            return rect;
        }

        private static bool IsDecoration(Transform item)
        {
            for (var p = item; p != null; p = p.parent)
                if (p.name.StartsWith(DecorationPrefix, StringComparison.Ordinal)) return true;
            return false;
        }

        private void Discover()
        {
            if (legacy is PopupDuelSelectCard cards && cards.scrollView != null)
            {
                var scrollbar = cards.scrollView.horizontalScrollbar;
                if (scrollbar != null && !cardScrollbars.ContainsKey(scrollbar))
                    cardScrollbars.Add(scrollbar, new FightDuelAuxiliarySkin.FineScrollbar(scrollbar, DecorationPrefix));
            }
            foreach (var image in GetComponentsInChildren<Image>(true))
                if (!IsDecoration(image.transform) && !images.ContainsKey(image)) images.Add(image, new ImageState(image));
            foreach (var item in content.GetComponentsInChildren<PopupDuelSelectCardItem>(true))
                if (!cardItems.ContainsKey(item)) cardItems.Add(item, new CardItemState(item));
            foreach (var control in content.GetComponentsInChildren<Selectable>(true))
            {
                if (controls.ContainsKey(control) || IsDecoration(control.transform) || control is Scrollbar) continue;
                bool card = control.GetComponentInParent<PopupDuelSelectCardItem>() != null
                    || control.GetComponentInChildren<RawImage>(true) != null;
                if (legacy is PopupDuelPosition position && (control == position.btnPositionAttack
                    || control == position.btnPositionDefense || control == position.btnPositionDefenseDown)) card = true;
                if (legacy is PopupDuelPhase) card = false;
                bool primary = legacy != null && control == legacy.btnConfirm || control.name == "DecideButton"
                    || control.name == "btnFinish" || control.name == "Finish"
                    || responsivePanel && control.GetComponentInChildren<FightFrame>(true)?.Primary == true;
                var state = new ControlState(control, primary, card);
                state.Glass = GlassPrompt;
                if (legacy is PopupDuelPhase phase) state.Phase = phase;
                if (legacy is PopupDuel duelPopup && control == duelPopup.btnHide) state.FieldView = duelPopup;
                if (!card && !(control is Slider) && !(control is FightDuelReadingControl))
                {
                    var background = Node(control.transform, "control surface", true);
                    // FieldView toggles its original first child by index.
                    // Keep that child in place while adding the owned surface.
                    if (state.FieldView == null) background.SetAsFirstSibling();
                    state.Surface = background.gameObject.AddComponent<Image>();
                    state.Surface.sprite = FightSkin.ButtonSurface; state.Surface.type = Image.Type.Sliced;
                    state.Surface.raycastTarget = true;
                }
                var border = Node(control.transform, "control frame", true);
                border.offsetMin = new Vector2(-.5f, -.5f); border.offsetMax = new Vector2(.5f, .5f);
                state.Frame = border.gameObject.AddComponent<FightSceneDialogFrame>();
                state.Frame.ControlFrame = true; state.Frame.raycastTarget = false;
                state.Frame.RowFrame = state.Row;
                state.Feedback = control.gameObject.AddComponent<FightSceneDialogControl>();
                state.Feedback.Bind(control, state.Frame);
                if (state.FieldView != null)
                {
                    var symbol = Node(control.transform, "field view symbol", true);
                    symbol.offsetMin = new Vector2(7, 5); symbol.offsetMax = new Vector2(-7, -5);
                    state.ViewGlyph = symbol.gameObject.AddComponent<FightSceneViewGlyph>();
                    state.ViewGlyph.raycastTarget = false;
                    state.ViewHint = control.GetComponent<FightTooltip>();
                    state.OwnsViewHint = state.ViewHint == null;
                    if (state.OwnsViewHint) state.ViewHint = control.gameObject.AddComponent<FightTooltip>();
                    state.OriginalViewHint = state.ViewHint.Hint;
                    state.ViewHintWasEnabled = state.ViewHint.enabled;
                    state.ViewHintWasLocalized = state.ViewHint.LocalizeHint;
                }
                controls.Add(control, state);
            }
            foreach (var text in GetComponentsInChildren<TextMeshProUGUI>(true))
                if (!IsDecoration(text.transform) && !texts.ContainsKey(text)) texts.Add(text, new TextState(text));
            foreach (var text in GetComponentsInChildren<Text>(true))
                if (!IsDecoration(text.transform) && !texts.ContainsKey(text)) texts.Add(text, new TextState(text));
            foreach (var toggle in content.GetComponentsInChildren<FightToggleGraphic>(true))
                if (!sceneSwitches.Contains(toggle)) sceneSwitches.Add(toggle);
            if (responsivePanel)
                foreach (var border in content.GetComponentsInChildren<FightFrame>(true))
                    if (!originalFrames.ContainsKey(border)) originalFrames.Add(border, border.enabled);
            visibleGraphics.Clear();
            foreach (var graphic in content.GetComponentsInChildren<Graphic>(true))
                visibleGraphics.Add(new VisibleGraphic(graphic, content));
        }

        private void LateUpdate()
        {
            if (content == null) return;
            // Selection rows arrive asynchronously and native virtual lists
            // reuse existing objects. They acquire the same skin on arrival.
            if (Time.unscaledTime >= discoverAt) { Discover(); discoverAt = Time.unscaledTime + .1f; }
            Repaint();
        }

        private void Repaint()
        {
            if (!SceneThemeActive) { Restore(); return; }
            var id = Runtime.Program.instance.ocgcore.DuelBGManager.ActiveSceneId;
            bool changed = id != sceneId;
            if (changed || palette == null) { sceneId = id; palette = ScenePalette.DialogFor(id); }
            if (palette == null) { Restore(); return; }
            if (!applied)
            {
                foreach (var decoration in decorations) if (decoration != null) decoration.SetActive(true);
                applied = true; changed = true;
            }
            foreach (var decoration in decorations) if (decoration != null && !decoration.activeSelf) decoration.SetActive(true);
            compactLayout?.Apply(palette);
            foreach (var toggle in sceneSwitches) if (toggle != null) toggle.SetScene(palette);
            nativePromptLayout?.Apply();
            FitPanel();
            foreach (var border in originalFrames.Keys) if (border != null) border.enabled = false;
            var panelColor = palette.Panel;
            if (GlassPrompt) panelColor.a = palette.Kind == 0 ? .97f : .96f;
            if (changed || surface.color != panelColor)
            {
                surface.sprite = FightSkin.Surface; surface.type = Image.Type.Sliced;
                surface.overrideSprite = null; surface.material = null; surface.color = panelColor;
                surface.pixelsPerUnitMultiplier = 1;
                frame.Palette = palette; frame.SetVerticesDirty();
            }
            // The reading surface blocks field labels and effects beneath it.
            // Button feedback carries selection without a heavy outer frame.
            frame.enabled = !GlassPrompt;
            // Keep the material hint below the reading surface. A texture must
            // never compete with card art, rule text or the selection state.
            materialGrain.sprite = GlassPrompt ? null : DialogArtwork.Material(sceneId);
            materialGrain.enabled = !GlassPrompt;
            materialGrain.color = new Color(1, 1, 1, materialGrain.sprite != null ? .035f : 0);
            foreach (var pair in images)
            {
                var image = pair.Key;
                if (image == null || !ShouldStyle(image)) continue;
                pair.Value.Managed = true;
                var mask = image.GetComponent<Mask>();
                if (legacy is PopupDuel field && field.btnHide != null && (image.transform == field.btnHide.transform
                    || image.transform.IsChildOf(field.btnHide.transform)))
                {
                    // FieldView still toggles native child zero and its Animator.
                    // Its oversized legacy IconOff art yields to the owned tab
                    // glyph without changing that object or any listener.
                    if (mask != null) mask.showMaskGraphic = false;
                    Tint(image, Color.clear); image.raycastTarget = false; continue;
                }
                var scroll = image.GetComponentInParent<ScrollRect>();
                bool scrollHitArea = scroll != null && (image.gameObject == scroll.gameObject
                    || scroll.viewport != null && image.gameObject == scroll.viewport.gameObject);
                var bar = image.GetComponentInParent<Scrollbar>();
                bool outside = !image.transform.IsChildOf(content);
                var name = image.name.ToLowerInvariant();
                if (mask != null) { mask.showMaskGraphic = false; continue; }
                var slider = image.GetComponentInParent<Slider>();
                if (slider != null)
                {
                    bool handle = image.rectTransform == slider.handleRect || image == slider.targetGraphic;
                    Tint(image, handle || image.rectTransform == slider.fillRect ? palette.Accent : palette.Edge);
                    continue;
                }
                if (outside)
                {
                    if (GlassPrompt || focusBackdrop != null && IsModalBackdrop(image))
                    {
                        // Keep the publisher's modal hit surface and tween,
                        // while one owned visual supplies consistent focus.
                        Tint(image, Color.clear); continue;
                    }
                    // Native shadow tweens retain their fade and input block.
                    var tint = palette.Shadow; tint.a = image.color.a;
                    Tint(image, tint); continue;
                }
                var rect = image.rectTransform.rect;
                bool line = rect.width > 0 && rect.height > 0 && Mathf.Min(rect.width, rect.height) < 4
                    && Mathf.Max(rect.width, rect.height) > 30;
                Color color = bar != null ? name.Contains("handle") || name.Contains("thumb") ? palette.Accent : palette.Raised
                    : IsSymbol(name) ? palette.Accent : line ? palette.Edge : Color.clear;
                if (line && bar == null) color.a = responsivePanel && image.transform.parent == content ? .28f : .16f;
                if (legacy is PopupDuelPhase && image.GetComponentInParent<Button>() != null) color = Color.clear;
                Tint(image, color);
                if (bar == null && !scrollHitArea)
                {
                    var hit = image.GetComponent<Selectable>();
                    image.raycastTarget = hit != null && controls.TryGetValue(hit, out var state) && state.Card;
                }
            }
            foreach (var scrollbar in cardScrollbars.Values) scrollbar.Apply(palette);
            foreach (var item in cardItems.Values) item.Apply(palette, sceneId);
            foreach (var state in controls.Values)
            {
                if (state.Control == null) continue;
                state.Apply(palette, sceneId);
                state.Frame.Palette = palette; state.Frame.Danger = state.Destructive;
                state.Feedback.Refresh(palette);
            }
            foreach (var pair in texts)
            {
                var graphic = pair.Key;
                if (graphic == null || graphic.GetComponentInParent<PopupDuelSelectCardItem>() != null) continue;
                var owner = graphic.GetComponentInParent<Selectable>();
                var input = graphic.GetComponentInParent<TMP_InputField>();
                var oldInput = graphic.GetComponentInParent<InputField>();
                Color color = graphic == title ? palette.Heading : palette.Text;
                if (owner != null && controls.TryGetValue(owner, out var state)) color = state.Foreground(palette);
                else if (owner != null && !owner.IsInteractable()) color = palette.Muted;
                if (input != null && input.placeholder == graphic || oldInput != null && oldInput.placeholder == graphic) color = palette.Muted;
                var selection = graphic.GetComponentInParent<Popup.SelectionToggle_PopupSelectionItem>();
                if (selection != null && selection.color == "r") color = FightUI.Danger;
                else if (selection != null && (selection.color == "b" || selection.color == "g")) color = palette.Accent;
                if (pair.Value.Danger || owner != null && controls.TryGetValue(owner, out var dangerState) && dangerState.Destructive)
                    color = FightUI.Danger;
                // Validation owns a red input message; do not erase it.
                if ((input != null || oldInput != null) && graphic.color == FightUI.Danger) continue;
                pair.Value.Managed = true;
                Tint(graphic, color);
                pair.Value.Apply(graphic == title, palette, legacy == null && !responsivePanel);
            }
            FitSafeArea();
            UpdateFocusBackdrop();
        }

        private bool IsModalBackdrop(Image image)
        {
            if (legacy != null && image == legacy.shadow) return true;
            if (safeParent == null || image.GetComponent<Selectable>() != null) return false;
            var size = image.rectTransform.rect.size;
            var parentSize = safeParent.rect.size;
            return size.x >= parentSize.x * .85f && size.y >= parentSize.y * .85f;
        }

        private void UpdateFocusBackdrop()
        {
            if (focusBackdrop == null || safeParent == null) return;
            var program = Runtime.Program.instance;
            bool top = modern != null ? program.ui_.currentPopupB == modern
                : program.ui_.currentPopupB == null && (legacy is PopupDuel duel
                    ? program.ocgcore.currentPopup == duel : program.ui_.currentPopup == legacy);
            bool hidden = legacy is PopupDuel field && field.FieldViewHidden;
            float showY = legacy != null ? legacy.showY : 0;
            float travel = Mathf.Clamp01(1 - Mathf.Abs(content.anchoredPosition.y - showY)
                / Mathf.Max(1, safeParent.rect.height * .5f));
            var color = palette.Shadow;
            float maximumAlpha = GlassPrompt ? .10f : .36f;
            float targetAlpha = top && !hidden ? maximumAlpha * travel : 0;
            float step = maximumAlpha * Time.unscaledDeltaTime / .16f;
            color.a = hidden ? 0 : focusBackdrop.color.a
                + Mathf.Clamp(targetAlpha - focusBackdrop.color.a, -step, step);
            focusBackdrop.color = color;
        }

        private void FitPanel()
        {
            if (compactLayout != null && compactLayout.Supported)
            {
                panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f);
                panel.anchoredPosition = Vector2.zero; panel.sizeDelta = compactLayout.Size;
                return;
            }
            if (responsivePanel) { FightUI.Stretch(panel); return; }
            if (nativePromptLayout != null && nativePromptLayout.Supported)
            {
                panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f);
                panel.anchoredPosition = Vector2.zero; panel.sizeDelta = nativePromptLayout.Size;
                return;
            }
            // The old selection/card families resize baseRect after population.
            // Follow that exact surface, even when it is not the transition root.
            RectTransform sized = legacy is PopupDuelSelectCard cards ? cards.baseRect
                : legacy is PopupDuelSelection selection ? selection.baseRect : null;
            if (sized != null && sized != content)
            {
                sized.GetWorldCorners(corners);
                Vector2 min = content.InverseTransformPoint(corners[0]), max = content.InverseTransformPoint(corners[2]);
                float width = Mathf.Max(1, max.x - min.x), height = Mathf.Max(1, max.y - min.y);
                panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f); panel.pivot = new Vector2(.5f, .5f);
                panel.anchoredPosition = (min + max) * .5f - content.rect.center;
                panel.sizeDelta = new Vector2(width + 96, height + 56);
            }
            else if (legacy == null && content.GetComponent<VerticalLayoutGroup>() != null)
            {
                // Native selection content is a screen-height flex root. Bound
                // the architecture to its populated sections, not empty spacers.
                float lower = float.PositiveInfinity, upper = float.NegativeInfinity;
                foreach (Transform child in content)
                {
                    if (IsDecoration(child) || !child.gameObject.activeSelf || !(child is RectTransform rect)) continue;
                    if (child.GetComponent<LayoutElement>() is LayoutElement flex && flex.flexibleHeight > 0 && flex.minHeight <= 0) continue;
                    rect.GetWorldCorners(corners);
                    foreach (var corner in corners)
                    {
                        float y = content.InverseTransformPoint(corner).y;
                        lower = Mathf.Min(lower, y); upper = Mathf.Max(upper, y);
                    }
                }
                if (!float.IsInfinity(lower) && upper > lower)
                {
                    panel.anchorMin = new Vector2(0, .5f); panel.anchorMax = new Vector2(1, .5f); panel.pivot = new Vector2(.5f, .5f);
                    panel.anchoredPosition = new Vector2(0, (lower + upper) * .5f - content.rect.center.y);
                    panel.sizeDelta = new Vector2(0, Mathf.Min(content.rect.height, upper - lower + 32));
                }
            }
        }

        private void CreateSafeMount()
        {
            safeParent = content.parent as RectTransform;
            if (safeParent == null) return;
            int order = content.GetSiblingIndex();
            var anchorsMin = content.anchorMin; var anchorsMax = content.anchorMax; var pivot = content.pivot;
            var position = content.anchoredPosition; var size = content.sizeDelta; var scale = content.localScale;
            var rotation = content.localRotation;
            // Match the former parent's rect exactly. The native window keeps
            // its identity, anchor targets and drag position under this mount.
            safeMount = FightUI.Node(safeParent, "Fight popup safe placement", 0, 0, 1, 1);
            FightUI.Stretch(safeMount); safeMount.SetSiblingIndex(order);
            safeMount.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            content.SetParent(safeMount, false);
            content.anchorMin = anchorsMin; content.anchorMax = anchorsMax; content.pivot = pivot;
            content.sizeDelta = size; content.anchoredPosition = position;
            content.localScale = scale; content.localRotation = rotation;
        }

        private void FitSafeArea()
        {
            if (safeMount == null || safeParent == null || !TrySafeRect(out var safe)) return;
            var duelPopup = legacy as PopupDuel;
            if (duelPopup != null && duelPopup.FieldViewHidden)
            {
                // Native FieldView deliberately moves the window off screen.
                // Restore the mount so that its native hide distance still works.
                ResetSafeMount(); fieldWasHidden = true; return;
            }
            if (presented && duelPopup != null && Runtime.Program.instance.ocgcore.currentPopup != duelPopup) return;
            bool moving = DOTween.IsTweening(content);
            bool target = !presented || fieldWasHidden;
            if (moving && !target) return;
            if (!TryVisibleBounds(target, out var bounds)) return;
            float currentScale = safeMount.localScale.x;
            float fittedScale = Mathf.Min(1, Mathf.Min(currentScale * safe.width / bounds.width,
                currentScale * safe.height / bounds.height));
            if (fittedScale <= 0 || float.IsNaN(fittedScale) || float.IsInfinity(fittedScale)) return;
            if (Mathf.Abs(fittedScale - currentScale) > .0001f)
            {
                Vector2 center = bounds.center;
                safeMount.localScale = Vector3.one * fittedScale;
                if (!TryVisibleBounds(target, out bounds)) return;
                // Scale around the displayed group, preserving its drag position.
                safeMount.anchoredPosition += center - bounds.center;
                if (!TryVisibleBounds(target, out bounds)) return;
            }
            Vector2 correction = SafeCorrection(bounds, safe);
            if (correction.sqrMagnitude > .0001f) safeMount.anchoredPosition += correction;
            float showY = legacy != null ? legacy.showY : 0;
            if (!moving && Mathf.Abs(content.anchoredPosition.y - showY) < 2)
            { presented = true; fieldWasHidden = false; }
        }

        private bool TrySafeRect(out Rect safe)
        {
            safe = default;
            Rect pixels = Screen.safeArea;
            pixels = new Rect(pixels.xMin + 16, pixels.yMin + 16, pixels.width - 32, pixels.height - 32);
            if (pixels.width <= 0 || pixels.height <= 0) return false;
            var canvas = content.GetComponentInParent<Canvas>();
            var camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(safeParent, pixels.min, camera, out var min)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(safeParent, pixels.max, camera, out var max)) return false;
            safe = Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
            return safe.width > 0 && safe.height > 0;
        }

        private bool TryVisibleBounds(bool target, out Rect bounds)
        {
            bool any = false; bounds = default;
            if (panel != null) { bounds = RectInParent(panel); any = bounds.width > 0 && bounds.height > 0; }
            foreach (var visible in visibleGraphics)
            {
                var graphic = visible.Graphic;
                if (graphic == null || !graphic.isActiveAndEnabled || graphic.color.a <= .001f) continue;
                var maskGraphic = graphic.GetComponent<Mask>();
                if (maskGraphic != null && !maskGraphic.showMaskGraphic) continue;
                Rect rect = RectInParent(graphic.rectTransform);
                // Scroll lists may contain hundreds of cards outside their
                // viewport. Only their clipped visible footprints count.
                foreach (var mask in visible.Masks)
                {
                    if (mask == null || !mask.gameObject.activeInHierarchy) continue;
                    rect = Intersection(rect, RectInParent(mask));
                    if (rect.width <= 0 || rect.height <= 0) break;
                }
                if (rect.width <= 0 || rect.height <= 0) continue;
                bounds = any ? Union(bounds, rect) : rect; any = true;
            }
            if (!any) return false;
            if (target)
            {
                float showY = legacy != null ? legacy.showY : 0;
                var world = safeMount.TransformVector(new Vector3(0, showY - content.anchoredPosition.y, 0));
                Vector2 projected = safeParent.InverseTransformVector(world);
                bounds.position += projected;
            }
            return bounds.width > 0 && bounds.height > 0;
        }

        private Rect RectInParent(RectTransform rect)
        {
            rect.GetWorldCorners(corners);
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity), max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var corner in corners)
            {
                Vector2 point = safeParent.InverseTransformPoint(corner);
                min = Vector2.Min(min, point); max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        private static Rect Union(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
        private static Rect Intersection(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin), Mathf.Min(a.xMax, b.xMax), Mathf.Min(a.yMax, b.yMax));
        internal static Vector2 SafeCorrection(Rect bounds, Rect safe)
        {
            float x = bounds.xMin < safe.xMin ? safe.xMin - bounds.xMin : bounds.xMax > safe.xMax ? safe.xMax - bounds.xMax : 0;
            float y = bounds.yMin < safe.yMin ? safe.yMin - bounds.yMin : bounds.yMax > safe.yMax ? safe.yMax - bounds.yMax : 0;
            return new Vector2(x, y);
        }
        private void ResetSafeMount()
        {
            if (safeMount == null) return;
            safeMount.localScale = Vector3.one; safeMount.anchoredPosition = Vector2.zero;
        }
        private sealed class VisibleGraphic
        {
            internal readonly Graphic Graphic;
            internal readonly RectTransform[] Masks;
            internal VisibleGraphic(Graphic graphic, RectTransform content)
            {
                Graphic = graphic;
                var masks = new List<RectTransform>();
                for (var ancestor = graphic.transform.parent; ancestor != null; ancestor = ancestor.parent)
                {
                    var rectMask = ancestor.GetComponent<RectMask2D>();
                    var stencilMask = ancestor.GetComponent<Mask>();
                    if (rectMask != null && rectMask.isActiveAndEnabled || stencilMask != null && stencilMask.isActiveAndEnabled)
                        if (ancestor is RectTransform rect) masks.Add(rect);
                    if (ancestor == content) break;
                }
                Masks = masks.ToArray();
            }
        }

        private bool ShouldStyle(Image image)
        {
            var scrollbar = image.GetComponentInParent<Scrollbar>();
            if (scrollbar != null && cardScrollbars.ContainsKey(scrollbar)) return false;
            var name = image.name.ToLowerInvariant();
            // Card artwork, protectors, combat symbols and selection state
            // graphics are still authored by the rule UI, including dimming.
            var item = image.GetComponentInParent<PopupDuelSelectCardItem>();
            if (item != null)
            {
                // CardItemState owns group plates and keeps native status
                // icons below the card. Art and native dimming stay here.
                return false;
            }
            if (image.gameObject == gameObject || image.gameObject == content.gameObject || image.GetComponent<Mask>() != null
                || name.Contains("background") || name.Contains("surface") || name.Contains("base")) return true;
            if (name.Contains("card") || name.Contains("attribute") || name.Contains("race") || name.Contains("level")
                || name.Contains("pendulum") || name.Contains("tuner") || name.Contains("rarity") || name.Contains("limit")) return false;
            return true;
        }

        private static bool IsSymbol(string name) => name.Contains("icon") || name.Contains("arrow") || name.Contains("check")
            || name.Contains("chain") || name.Contains("target");

        private static void Tint(Graphic graphic, Color color)
        {
            graphic.color = color;
            var container = graphic.GetComponent<Presentation.UI.ColorContainerGraphic>();
            if (container != null) container.baseColor = color;
        }

        private void Restore()
        {
            foreach (var toggle in sceneSwitches) if (toggle != null) toggle.SetScene(null);
            if (!applied) return;
            foreach (var state in images.Values) state.Restore();
            foreach (var state in texts.Values) state.Restore();
            foreach (var state in controls.Values) state.Restore();
            foreach (var item in cardItems.Values) item.Restore();
            foreach (var scrollbar in cardScrollbars.Values) scrollbar.Restore();
            nativePromptLayout?.Restore();
            compactLayout?.Restore();
            foreach (var border in originalFrames) if (border.Key != null) border.Key.enabled = border.Value;
            ResetSafeMount(); presented = false; fieldWasHidden = false;
            foreach (var decoration in decorations) if (decoration != null) decoration.SetActive(false);
            applied = false; sceneId = null;
        }
        private void OnDisable() => Restore();
        private void OnDestroy()
        {
            // The focus layer is mounted beside the popup, so it is not
            // destroyed with the popup's own hierarchy.
            if (focusBackdrop != null) Destroy(focusBackdrop.gameObject);
            if (safeMount != null && !safeMount.IsChildOf(transform)) Destroy(safeMount.gameObject);
        }

        private sealed class ImageState
        {
            internal bool Managed;
            private readonly Image image;
            private readonly Color color, baseColor;
            private readonly bool raycast, maskVisible;
            private readonly Mask mask;
            private readonly Sprite sprite, overrideSprite;
            private readonly Image.Type type;
            private readonly float pixelsPerUnit;
            private bool materialManaged;
            internal ImageState(Image source)
            {
                image = source; color = source.color; raycast = source.raycastTarget;
                sprite = source.sprite; overrideSprite = source.overrideSprite;
                type = source.type; pixelsPerUnit = source.pixelsPerUnitMultiplier;
                baseColor = source.GetComponent<Presentation.UI.ColorContainerGraphic>()?.baseColor ?? color;
                mask = source.GetComponent<Mask>(); maskVisible = mask != null && mask.showMaskGraphic;
            }
            internal void ApplySurface(Sprite artwork, Color tint)
            {
                if (image == null) return;
                Managed = true;
                if (artwork != null)
                {
                    materialManaged = true;
                    image.sprite = artwork; image.overrideSprite = null;
                    image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 1.8f;
                }
                Tint(image, tint);
            }
            internal void Restore()
            {
                if (image == null || !Managed) return;
                Tint(image, color); image.raycastTarget = raycast;
                var container = image.GetComponent<Presentation.UI.ColorContainerGraphic>();
                if (container != null) container.baseColor = baseColor;
                if (mask != null) mask.showMaskGraphic = maskVisible;
                if (materialManaged)
                {
                    image.sprite = sprite; image.overrideSprite = overrideSprite;
                    image.type = type; image.pixelsPerUnitMultiplier = pixelsPerUnit;
                }
            }
        }

        private sealed class CardItemState
        {
            private readonly PopupDuelSelectCardItem item;
            private readonly int discoveredFrame;
            private ImageState plate, head, check, order;
            private TextState orderText;
            private readonly Dictionary<RectTransform, StatusRectState> statusRects = new();
            private readonly Dictionary<TextMeshProUGUI, StatusTextState> statusTexts = new();
            private bool captured, search, opponent;
            internal CardItemState(PopupDuelSelectCardItem source)
            { item = source; discoveredFrame = Time.frameCount; }

            internal void Apply(ScenePalette palette, string scene)
            {
                if (item == null || !item.isActiveAndEnabled || item.card == null) return;
                if (!captured)
                {
                    // Async population can discover a prefab before Start has
                    // set its opponent/search bands. Capture after native init
                    // so a later classic scene restores the actual group.
                    if (Time.frameCount <= discoveredFrame) return;
                    var background = item.GetComponent<Image>();
                    if (background != null) plate = new ImageState(background);
                    if (item.head != null) head = new ImageState(item.head);
                    if (item.checkOn != null && item.checkOn.TryGetComponent<Image>(out var checkedImage))
                        check = new ImageState(checkedImage);
                    if (item.orderBase != null && item.orderBase.TryGetComponent<Image>(out var orderImage))
                        order = new ImageState(orderImage);
                    if (item.orderText != null) orderText = new TextState(item.orderText);
                    // The native location icon and group boundaries are fixed
                    // for this popup. Keep its plate grouping fixed too, even
                    // if the selected card moves while the window fades out.
                    search = (item.card.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Search) != 0;
                    opponent = !search && item.card.p.controller != 0;
                    captured = true;
                }
                // Preserve opponent/search grouping while the card art and
                // native location symbols retain their own contrast.
                Color fill = opponent ? Color.Lerp(palette.Raised, FightUI.Danger, palette.Kind == 0 ? .18f : .12f)
                    : search ? palette.Panel : palette.Raised;
                if (item.unselectable && !item.selected) fill = Color.Lerp(fill, palette.Panel, .45f);
                fill.a = .10f;
                plate?.ApplySurface(FightSkin.ButtonSurface, fill);
                fill.a = .18f; head?.ApplySurface(FightSkin.ButtonSurface, fill);
                check?.ApplySurface(null, palette.Heading);
                order?.ApplySurface(null, palette.Accent);
                if (orderText != null)
                { orderText.Managed = true; Tint(item.orderText, palette.PrimaryText); }
                FitCardStatus();
            }

            private void FitCardStatus()
            {
                if (item.cardFace == null) return;
                // Card/Back is a curled-corner sprite covering the full card,
                // not the hidden card texture. Keep its face-down meaning in
                // a small footer alongside the native level/scale symbols.
                bool back = item.cardBack != null && item.cardBack.activeSelf;
                bool level = item.levelIcon != null && item.textLevel != null && !string.IsNullOrEmpty(item.textLevel.text);
                bool pendulum = item.pendulumIcon != null && item.pendulumIcon.gameObject.activeSelf && item.textPendulum != null;
                bool tuner = item.tunerIcon != null && item.tunerIcon.gameObject.activeSelf;
                int groups = (back ? 1 : 0) + (level ? 1 : 0) + (pendulum ? 1 : 0) + (tuner ? 1 : 0);
                float width = (back ? 20 : 0) + (level ? 40 : 0) + (pendulum ? 40 : 0) + (tuner ? 18 : 0)
                    + Mathf.Max(0, groups - 1) * 4;
                float x = -width * .5f;
                float y = item.cardFace.rectTransform.rect.yMin - 14;
                if (back)
                {
                    PlaceStatus(item.cardBack.transform as RectTransform, new Vector2(16.5f, 24), new Vector2(x + 10, y));
                    x += 24;
                }
                if (level)
                {
                    PlaceStatus(item.levelIcon.rectTransform, new Vector2(16, 18), new Vector2(x + 8, y));
                    PlaceStatus(item.textLevel.rectTransform, new Vector2(22, 24), new Vector2(x + 29, y));
                    StyleStatus(item.textLevel); x += 44;
                }
                if (pendulum)
                {
                    PlaceStatus(item.pendulumIcon.rectTransform, new Vector2(16, 18), new Vector2(x + 8, y));
                    PlaceStatus(item.textPendulum.rectTransform, new Vector2(22, 24), new Vector2(x + 29, y));
                    StyleStatus(item.textPendulum); x += 44;
                }
                if (tuner)
                    PlaceStatus(item.tunerIcon.rectTransform, new Vector2(18, 18), new Vector2(x + 9, y));
            }

            private void PlaceStatus(RectTransform rect, Vector2 size, Vector2 position)
            {
                if (rect == null || rect.parent == null) return;
                if (!statusRects.ContainsKey(rect)) statusRects.Add(rect, new StatusRectState(rect));
                var parent = rect.parent as RectTransform;
                Vector2 local = rect.parent.InverseTransformPoint(item.cardFace.transform.TransformPoint(position));
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
                rect.sizeDelta = size; rect.anchoredPosition = local - (parent != null ? parent.rect.center : Vector2.zero);
            }

            private void StyleStatus(TextMeshProUGUI text)
            {
                if (!statusTexts.ContainsKey(text)) statusTexts.Add(text, new StatusTextState(text));
                text.fontSize = 14; text.fontSizeMax = 14; text.fontSizeMin = 10; text.enableAutoSizing = true;
                text.enableWordWrapping = false; text.overflowMode = TextOverflowModes.Truncate;
                text.alignment = TextAlignmentOptions.Left;
            }

            internal void Restore()
            {
                if (!captured) return;
                plate?.Restore(); head?.Restore(); check?.Restore(); order?.Restore(); orderText?.Restore();
                foreach (var pair in statusRects) if (pair.Key != null) pair.Value.Restore(pair.Key);
                foreach (var pair in statusTexts) if (pair.Key != null) pair.Value.Restore(pair.Key);
            }

            private sealed class StatusRectState
            {
                private readonly Vector2 min, max, pivot, size, position;
                internal StatusRectState(RectTransform rect)
                { min = rect.anchorMin; max = rect.anchorMax; pivot = rect.pivot; size = rect.sizeDelta; position = rect.anchoredPosition; }
                internal void Restore(RectTransform rect)
                { rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot; rect.sizeDelta = size; rect.anchoredPosition = position; }
            }

            private sealed class StatusTextState
            {
                private readonly float size, min, max;
                private readonly bool autoSize, wrapping;
                private readonly TextOverflowModes overflow;
                private readonly TextAlignmentOptions alignment;
                internal StatusTextState(TextMeshProUGUI text)
                { size = text.fontSize; min = text.fontSizeMin; max = text.fontSizeMax; autoSize = text.enableAutoSizing; wrapping = text.enableWordWrapping; overflow = text.overflowMode; alignment = text.alignment; }
                internal void Restore(TextMeshProUGUI text)
                { text.fontSize = size; text.fontSizeMin = min; text.fontSizeMax = max; text.enableAutoSizing = autoSize; text.enableWordWrapping = wrapping; text.overflowMode = overflow; text.alignment = alignment; }
            }
        }

        private sealed class TextState
        {
            internal bool Managed;
            internal readonly bool Danger;
            private readonly Graphic graphic;
            private readonly Color color, baseColor;
            private readonly float spacing;
            private readonly float size, minSize, maxSize;
            private readonly TMP_FontAsset font;
            private readonly Material fontMaterial;
            private readonly bool autoSize, gradient;
            private readonly bool wordWrap;
            private readonly TextOverflowModes overflow;
            private readonly FontStyles tmpStyle;
            private readonly FontStyle oldStyle;
            internal TextState(Graphic source)
            {
                graphic = source; color = source.color; Danger = color == FightUI.Danger;
                baseColor = source.GetComponent<Presentation.UI.ColorContainerGraphic>()?.baseColor ?? color;
                if (source is TMP_Text tmp)
                {
                    spacing = tmp.characterSpacing; tmpStyle = tmp.fontStyle; size = tmp.fontSize;
                    minSize = tmp.fontSizeMin; maxSize = tmp.fontSizeMax;
                    font = tmp.font; fontMaterial = tmp.fontSharedMaterial; autoSize = tmp.enableAutoSizing; gradient = tmp.enableVertexGradient;
                    wordWrap = tmp.enableWordWrapping; overflow = tmp.overflowMode;
                }
                if (source is Text text) oldStyle = text.fontStyle;
            }
            internal void Apply(bool title, ScenePalette palette, bool native)
            {
                if (graphic is TMP_Text tmp)
                {
                    if (native)
                    {
                        var clientFont = Runtime.Program.instance.ui_.tmpFont;
                        if (clientFont != null) { tmp.font = clientFont; tmp.fontSharedMaterial = clientFont.material; }
                        var owner = graphic.GetComponentInParent<Button>();
                        bool footer = owner != null && owner.GetComponentInParent<ScrollRect>() == null
                            && (owner.name == "DecideButton" || owner.name == "CancelButton" || owner.name == "ConfirmButton");
                        tmp.enableAutoSizing = footer; tmp.enableVertexGradient = false;
                        bool listRow = owner != null && owner.GetComponentInParent<ScrollRect>() != null;
                        tmp.fontSize = title ? 24 : listRow ? 20 : 22;
                        if (footer)
                        {
                            tmp.fontSizeMax = 22; tmp.fontSizeMin = 18;
                            tmp.enableWordWrapping = false; tmp.overflowMode = TextOverflowModes.Ellipsis;
                        }
                        tmp.characterSpacing = title ? .6f : 0;
                        tmp.fontStyle = title ? FontStyles.Bold : FontStyles.Normal;
                    }
                    else if (title) { tmp.characterSpacing = .6f; tmp.fontStyle |= FontStyles.Bold; }
                }
                if (title && graphic is Text text) text.fontStyle = FontStyle.Bold;
            }
            internal void Restore()
            {
                if (graphic == null || !Managed) return;
                Tint(graphic, color);
                var container = graphic.GetComponent<Presentation.UI.ColorContainerGraphic>();
                if (container != null) container.baseColor = baseColor;
                if (graphic is TMP_Text tmp)
                {
                    tmp.characterSpacing = spacing; tmp.fontStyle = tmpStyle; tmp.fontSize = size;
                    tmp.fontSizeMin = minSize; tmp.fontSizeMax = maxSize;
                    tmp.font = font; tmp.fontSharedMaterial = fontMaterial; tmp.enableAutoSizing = autoSize; tmp.enableVertexGradient = gradient;
                    tmp.enableWordWrapping = wordWrap; tmp.overflowMode = overflow;
                }
                if (graphic is Text text) text.fontStyle = oldStyle;
            }
        }

        private sealed class ControlState
        {
            internal readonly Selectable Control;
            internal readonly bool Primary, Destructive, Row, Card;
            private readonly Color destructiveFill;
            internal Image Surface;
            internal bool CurrentPhase;
            internal bool Glass;
            internal FightSceneDialogFrame Frame;
            internal FightSceneDialogControl Feedback;
            internal PopupDuel FieldView;
            internal PopupDuelPhase Phase;
            internal FightSceneViewGlyph ViewGlyph;
            internal FightTooltip ViewHint;
            internal string OriginalViewHint;
            internal bool OwnsViewHint, ViewHintWasEnabled, ViewHintWasLocalized;
            private readonly Graphic target;
            private readonly ColorBlock colors;
            private readonly Selectable.Transition transition;
            internal ControlState(Selectable control, bool primary, bool card)
            {
                Control = control; Primary = primary; Card = card; target = control.targetGraphic; colors = control.colors; transition = control.transition;
                var outline = control.GetComponent<Outline>();
                Destructive = outline != null && outline.effectColor == FightUI.Danger;
                var surface = control.GetComponent<Image>();
                destructiveFill = surface != null ? surface.color : FightUI.Hex("301E21");
                Row = control is Slider || control is FightDuelReadingControl || control.GetComponent<FightSwitchFeedback>() != null
                    || control.GetComponent<FightFocusKey>()?.Key?.StartsWith("duel:category:", StringComparison.Ordinal) == true
                    || control.GetComponentInParent<ScrollRect>() != null && !primary;
            }
            internal void Apply(ScenePalette palette, string scene)
            {
                Feedback.enabled = true;
                if (ViewGlyph != null && FieldView != null)
                {
                    ViewGlyph.SetScene(palette, FieldView.FieldViewHidden);
                    ViewHint.Hint = FieldView.FieldViewHidden ? "返回操作" : "查看场地";
                    ViewHint.LocalizeHint = false; ViewHint.enabled = true;
                }
                if (Surface == null) return;
                // Retire SpriteSwap before moving its target, or a native
                // disabled/selected sprite can override the scene material.
                Control.transition = Selectable.Transition.ColorTint; Control.targetGraphic = Surface;
                CurrentPhase = FightDuelPopupLayout.IsCurrentPhase(Phase, Control);
                Feedback.Latched = CurrentPhase;
                Surface.sprite = FightSkin.ButtonSurface; Surface.type = Image.Type.Sliced;
                Surface.overrideSprite = null; Surface.pixelsPerUnitMultiplier = 1;
                var fill = Destructive ? destructiveFill : Primary || CurrentPhase ? palette.Action : palette.Raised;
                if (Glass)
                {
                    fill.a = CurrentPhase ? .98f : Control.IsInteractable() ? Primary ? .98f : .94f : .65f;
                }
                Surface.color = fill;
                var tint = Control.colors;
                tint.normalColor = Color.white; tint.highlightedColor = new Color(1.06f, 1.06f, 1.06f);
                tint.selectedColor = new Color(1.08f, 1.08f, 1.08f); tint.pressedColor = new Color(.84f, .84f, .84f);
                // The current phase is unavailable as a response, but its
                // selected appearance remains the strongest visual state.
                tint.disabledColor = CurrentPhase ? Color.white : new Color(.76f, .76f, .76f, 1); tint.fadeDuration = .12f;
                if (Control.colors != tint) Control.colors = tint;
            }
            internal Color Foreground(ScenePalette palette)
            {
                if (Glass)
                {
                    if (CurrentPhase || Primary && Control.IsInteractable()) return palette.PrimaryText;
                    return Control.IsInteractable() ? palette.Kind == 0 ? palette.Heading : palette.Text : palette.Muted;
                }
                bool action = Primary || CurrentPhase;
                return Control.IsInteractable() ? action ? palette.PrimaryText : palette.Text
                    : action ? Color.Lerp(palette.PrimaryText, palette.Action, .28f) : palette.Muted;
            }
            internal void Restore()
            {
                if (Control == null) return;
                Control.targetGraphic = target; Control.colors = colors; Control.transition = transition;
                Feedback.enabled = false;
                if (ViewHint != null)
                {
                    ViewHint.Hint = OriginalViewHint; ViewHint.LocalizeHint = ViewHintWasLocalized;
                    ViewHint.enabled = !OwnsViewHint && ViewHintWasEnabled;
                }
            }
        }

        internal static class DialogArtwork
        {
            private static readonly Dictionary<string, Sprite> cache = new();
            private static readonly Dictionary<string, Texture2D> textures = new();
            private static readonly Dictionary<string, float> retries = new();
            internal static Sprite Material(string scene)
            {
                string directory = Path.Combine(Runtime.Program.PATH_DATA, "ClientUI", "duel-scenes");
                string path = Path.Combine(directory, scene + "-field-materials-v4.png");
                int cell = scene == "ancient-egypt" ? 4 : 0;
                string key = path + ":dialog-material:" + cell;
                if (cache.TryGetValue(key, out var cached)) return cached;
                var texture = Texture(path);
                if (texture == null) return null;
                const float inset = .004f;
                float x = (cell % 4 * .25f + inset) * texture.width;
                float y = (3 - cell / 4) * .25f * texture.height + inset * texture.height;
                var sprite = Sprite.Create(texture, new Rect(x, y, (.25f - inset * 2) * texture.width,
                    (.25f - inset * 2) * texture.height), Vector2.one * .5f, 100);
                cache[key] = sprite; return sprite;
            }

            internal static Sprite Control(string scene, int role)
            {
                string path = Path.Combine(Runtime.Program.PATH_DATA, "ClientUI", "duel-scenes", scene + "-controls-fine.png");
                string key = path + ":" + role;
                if (cache.TryGetValue(key, out var cached)) return cached;
                var texture = Texture(path);
                if (texture == null) return null;
                role = Mathf.Clamp(role, 0, 3);
                // The AI strips have different native heights. Preserve their
                // exact boundaries instead of sampling a neighbouring strip.
                int[] bounds = scene == "battle-academy" ? new[] { 0,248,470,692,941 }
                    : scene == "ancient-egypt" ? new[] { 0,274,491,698,941 } : new[] { 0,280,497,730,941 };
                int top = bounds[role], bottom = bounds[role + 1];
                if (texture.height != 941) { top = Mathf.RoundToInt(role * texture.height / 4f); bottom = Mathf.RoundToInt((role + 1) * texture.height / 4f); }
                var sprite = Sprite.Create(texture, new Rect(0, texture.height - bottom, texture.width, bottom - top),
                    new Vector2(.5f, .5f), 340, 0, SpriteMeshType.FullRect, new Vector4(60,18,60,18));
                cache[key] = sprite; return sprite;
            }

            private static Texture2D Texture(string path)
            {
                if (textures.TryGetValue(path, out var cached)) return cached;
                if (retries.TryGetValue(path, out float retryAt) && Time.unscaledTime < retryAt) return null;
                retries[path] = Time.unscaledTime + 2;
                Texture2D texture = null;
                try
                {
                    if (File.Exists(path))
                    {
                        texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = Path.GetFileNameWithoutExtension(path) };
                        if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), true)) throw new InvalidDataException();
                        texture.wrapMode = TextureWrapMode.Clamp; texture.filterMode = FilterMode.Bilinear;
                    }
                }
                catch (Exception error)
                {
                    if (texture != null) Destroy(texture);
                    texture = null;
                    Debug.LogWarning("[FightClient] scene UI material unavailable: " + error.GetType().Name);
                }
                if (texture != null) { textures[path] = texture; retries.Remove(path); }
                return texture;
            }
        }
    }

    // FieldView remains the native button, including its first-child toggle.
    // This owned eye/return symbol gives the themed tab a readable purpose.
    internal sealed class FightSceneViewGlyph : MaskableGraphic
    {
        private ScenePalette palette;
        private bool hidden;
        internal void SetScene(ScenePalette scene, bool fieldView)
        {
            if (palette == scene && hidden == fieldView) return;
            palette = scene; hidden = fieldView; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (palette == null) return;
            var rect = rectTransform.rect;
            if (rect.width <= 1 || rect.height <= 1) return;
            Vector2 center = rect.center;
            float width = Mathf.Min(rect.width * .43f, 24);
            float height = Mathf.Min(rect.height * .25f, 10);
            Color accent = palette.Heading;
            if (hidden)
            {
                Line(mesh, center + new Vector2(-width * .55f, height * .3f), center + new Vector2(0, height * 1.05f), 2, accent);
                Line(mesh, center + new Vector2(0, height * 1.05f), center + new Vector2(width * .55f, height * .3f), 2, accent);
                for (int i = 0; i < 2; i++)
                    Line(mesh, center + new Vector2(-width * .55f, -height * (.35f + i * .7f)), center + new Vector2(width * .55f, -height * (.35f + i * .7f)), 1.5f, palette.Accent);
                return;
            }
            const int segments = 20;
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments, b = (i + 1) / (float)segments;
                var pa = new Vector2(Mathf.Lerp(-width, width, a), Mathf.Sin(a * (float)Math.PI) * height);
                var pb = new Vector2(Mathf.Lerp(-width, width, b), Mathf.Sin(b * (float)Math.PI) * height);
                Line(mesh, center + pa, center + pb, 1.8f, accent);
                Line(mesh, center + new Vector2(pa.x, -pa.y), center + new Vector2(pb.x, -pb.y), 1.8f, accent);
            }
            float pupil = height * .54f;
            for (int i = 0; i < 16; i++)
            {
                float a = i * (float)Math.PI * 2 / 16, b = (i + 1) * (float)Math.PI * 2 / 16;
                Line(mesh, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * pupil,
                    center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * pupil, 1.8f, palette.Accent);
            }
            if (palette.Kind == 1)
            {
                Line(mesh, center + new Vector2(width * .45f, -height * .75f), center + new Vector2(width * .25f, -height * 1.7f), 1.5f, palette.Accent);
                Line(mesh, center + new Vector2(-width * .7f, -height * .4f), center + new Vector2(-width * 1.05f, -height), 1.5f, palette.Accent);
            }
            else if (palette.Kind == 0)
                foreach (int side in new[] { -1, 1 })
                    Line(mesh, center + new Vector2(side * width * 1.15f, -height * .35f), center + new Vector2(side * width * 1.15f, height * .35f), 1.5f, palette.Accent);
            else
            {
                Line(mesh, center + new Vector2(-width * .35f, height * 1.55f), center + new Vector2(width * .35f, height * 1.55f), 1, palette.Accent);
                Line(mesh, center + new Vector2(-width * .2f, -height * 1.55f), center + new Vector2(width * .2f, -height * 1.55f), 1, palette.Accent);
            }
        }
        private static void Line(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color tint)
        {
            var direction = b - a;
            if (direction.sqrMagnitude <= .0001f) return;
            Vector2 normal = new Vector2(-direction.y, direction.x).normalized * width * .5f;
            int start = mesh.currentVertCount;
            mesh.AddVert(a - normal, tint, Vector2.zero); mesh.AddVert(a + normal, tint, Vector2.zero);
            mesh.AddVert(b + normal, tint, Vector2.zero); mesh.AddVert(b - normal, tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
        }
    }

    // The compact action menus share scene materials and extracted native
    // symbols while their card-space projection remains owned by DuelButton.
    internal sealed class FightSceneActionSkin : MonoBehaviour
    {
        private DuelButton action;
        private FightDuelActionComposition composition;
        private Image captionPlate;
        private FightSceneGlyphFrame glyphFrame;
        private FightSceneDialogFrame captionFrame;
        private FightSceneDialogFrame frame;
        private FightSceneDialogControl feedback;
        private Color originalText, originalPlate;
        private Sprite originalSprite;
        private Image.Type originalType;
        private float originalPixelsPerUnit;
        private bool applied;
        internal static void Attach(DuelButton action)
        {
            if (action == null || !FightDuelPopupSkin.SceneThemeActive) return;
            var skin = action.GetComponent<FightSceneActionSkin>() ?? action.gameObject.AddComponent<FightSceneActionSkin>();
            skin.Bind(action);
        }
        private void Bind(DuelButton source)
        {
            if (action != null) return;
            action = source;
            originalText = source.text != null ? source.text.color : Color.white;
            captionPlate = source.bg != null ? source.bg.GetComponent<Image>() : null;
            if (captionPlate != null) { originalPlate = captionPlate.color; originalSprite = captionPlate.sprite; originalType = captionPlate.type; originalPixelsPerUnit = captionPlate.pixelsPerUnitMultiplier; }
            composition = new FightDuelActionComposition(source);
            var glyph = FightUI.Node(transform, "Fight scene action metal housing", 0, 0, 1, 1);
            FightUI.Stretch(glyph);
            glyph.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            glyphFrame = glyph.gameObject.AddComponent<FightSceneGlyphFrame>();
            glyphFrame.raycastTarget = false;
            if (captionPlate != null)
            {
                var caption = FightUI.Node(captionPlate.transform, "Fight scene action caption architecture", 0, 0, 1, 1);
                FightUI.Stretch(caption);
                caption.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                captionFrame = caption.gameObject.AddComponent<FightSceneDialogFrame>();
                captionFrame.ControlFrame = true; captionFrame.raycastTarget = false;
            }
            var rect = FightUI.Node(transform, "Fight scene action frame", 0, 0, 1, 1);
            FightUI.Stretch(rect);
            rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            frame = rect.gameObject.AddComponent<FightSceneDialogFrame>();
            frame.ControlFrame = true; frame.raycastTarget = false;
            feedback = gameObject.AddComponent<FightSceneDialogControl>();
            feedback.Bind(GetComponent<Selectable>(), frame);
            Refresh();
        }
        private void LateUpdate() => Refresh();
        private void Refresh()
        {
            if (action == null || frame == null) return;
            if (!FightDuelPopupSkin.SceneThemeActive) { Restore(); return; }
            var palette = ScenePalette.For(Runtime.Program.instance.ocgcore.DuelBGManager.ActiveSceneId);
            if (palette == null) { Restore(); return; }
            applied = true;
            // Feedback has one logical frame. The transparent-symbol plate has
            // its own fine edge; the older native bezel is a missing-art fallback.
            frame.gameObject.SetActive(false);
            feedback.enabled = true; feedback.Refresh(palette);
            string scene = Runtime.Program.instance.ocgcore.DuelBGManager.ActiveSceneId;
            bool composed = composition.Apply(scene, palette, frame.Emphasis);
            glyphFrame.gameObject.SetActive(!composed); glyphFrame.Palette = palette;
            glyphFrame.Artwork = FightDuelPopupSkin.DialogArtwork.Control(scene, 1);
            glyphFrame.Emphasis = frame.Emphasis; glyphFrame.SetVerticesDirty();
            if (captionFrame != null)
            {
                captionFrame.gameObject.SetActive(true); captionFrame.Palette = palette;
                captionFrame.Emphasis = frame.Emphasis; captionFrame.SetVerticesDirty();
            }
            if (captionPlate != null)
            {
                captionPlate.sprite = FightSkin.ButtonSurface; captionPlate.type = Image.Type.Sliced;
                captionPlate.pixelsPerUnitMultiplier = 1;
                var color = palette.Panel; color.a = originalPlate.a; captionPlate.color = color;
            }
            if (!composed && action.text != null) action.text.color = palette.Heading;
        }
        private void Restore()
        {
            if (!applied) return;
            applied = false;
            composition?.Restore();
            if (frame != null) frame.gameObject.SetActive(false);
            if (glyphFrame != null) glyphFrame.gameObject.SetActive(false);
            if (captionFrame != null) captionFrame.gameObject.SetActive(false);
            if (feedback != null) feedback.enabled = false;
            if (action != null && action.text != null) action.text.color = originalText;
            if (captionPlate != null) { captionPlate.color = originalPlate; captionPlate.sprite = originalSprite; captionPlate.type = originalType; captionPlate.pixelsPerUnitMultiplier = originalPixelsPerUnit; }
        }
        private void OnDisable() => Restore();
        private void OnDestroy() => composition?.Dispose();
    }

    // A fine material bezel follows only the native icon's outer perimeter.
    // Its central opening leaves the native action symbol and sprite states
    // visible, so attack/summon/activate remain the original rule icons.
    internal sealed class FightSceneGlyphFrame : MaskableGraphic
    {
        internal ScenePalette Palette;
        internal float Emphasis;
        private Sprite artwork;
        internal Sprite Artwork
        {
            get => artwork;
            set { if (artwork == value) return; artwork = value; SetMaterialDirty(); SetVerticesDirty(); }
        }
        public override Texture mainTexture => artwork != null ? artwork.texture : s_WhiteTexture;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (Palette == null) return;
            var r = rectTransform.rect;
            if (r.width <= 1 || r.height <= 1) return;
            float half = Mathf.Min(r.width, r.height) * .5f;
            float radius = half * .89f;
            const int segments = 64;
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)Math.PI * 2 * i / segments;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float extent;
                if (Palette.Kind == 1)
                {
                    // Eight sided ceremonial bezel with gold corner bosses.
                    float sector = Mathf.Repeat(angle + (float)Math.PI / 8, (float)Math.PI / 4) - (float)Math.PI / 8;
                    extent = half * .94f / Mathf.Cos(sector);
                }
                else
                {
                    float square = half / Mathf.Max(Mathf.Abs(direction.x), Mathf.Abs(direction.y));
                    float chamfer = half * (Palette.Kind == 0 ? 1.25f : 1.3f) / (Mathf.Abs(direction.x) + Mathf.Abs(direction.y));
                    extent = Mathf.Min(square, chamfer);
                }
                float light = Mathf.Clamp01(direction.y * .5f + .5f);
                Color outer = artwork != null ? Color.white * (.82f + Emphasis * .25f)
                    : Color.Lerp(Palette.Raised, Palette.Edge, .32f + light * .38f);
                outer.a = color.a;
                Color bevel = Color.Lerp(Palette.Edge, Palette.Accent, .2f + light * .65f);
                bevel.a = color.a * (.26f + Emphasis * .6f);
                var at = r.center;
                Add(mesh, at + direction * extent, outer, r);
                Add(mesh, at + direction * Mathf.Min(radius + .8f, extent), outer, r);
                Add(mesh, at + direction * radius, bevel, r);
            }
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                for (int ring = 0; ring < 2; ring++)
                {
                    int a = i * 3 + ring, b = next * 3 + ring;
                    mesh.AddTriangle(a, b, a + 1); mesh.AddTriangle(a + 1, b, b + 1);
                }
            }
        }
        private void Add(VertexHelper mesh, Vector2 position, Color tint, Rect rect)
        {
            Vector2 uv = Vector2.zero;
            if (artwork != null)
            {
                var a = artwork.rect;
                uv = new Vector2((a.x + Mathf.InverseLerp(rect.xMin, rect.xMax, position.x) * a.width) / artwork.texture.width,
                    (a.y + Mathf.InverseLerp(rect.yMin, rect.yMax, position.y) * a.height) / artwork.texture.height);
            }
            mesh.AddVert(position, tint, uv);
        }
    }

    internal sealed class ScenePalette
    {
        internal readonly int Kind;
        internal readonly Color Panel, Raised, Action, Edge, Accent, Heading, Text, Muted, Shadow, PrimaryText;
        private ScenePalette(int kind, string panel, string raised, string action, string edge, string accent, string heading,
            string text = null, string muted = null, string shadow = null, string primaryText = null)
        {
            Kind = kind; Panel = FightUI.Hex(panel); Raised = FightUI.Hex(raised); Action = FightUI.Hex(action);
            Edge = FightUI.Hex(edge); Accent = FightUI.Hex(accent); Heading = FightUI.Hex(heading);
            Text = FightUI.Hex(text ?? (kind == 1 ? "F1E6CF" : "E6EEF3"));
            Muted = FightUI.Hex(muted ?? (kind == 1 ? "AF9E7E" : "8A9AA9"));
            Shadow = FightUI.Hex(shadow ?? (kind == 1 ? "171006" : "020912"));
            PrimaryText = primaryText == null ? Heading : FightUI.Hex(primaryText);
        }
        // HUD art keeps its authored dark surfaces. Dialogs have their own
        // palette so the academy's ivory material does not recolor that HUD.
        private static readonly ScenePalette Academy = new(0, "1A2832", "2A3C47", "3D5664", "879BA3", "A4C4D1", "F0EFE3");
        private static readonly ScenePalette Egypt = new(1, "29251E", "393326", "5A4931", "A28558", "D3B276", "F2DEB5");
        private static readonly ScenePalette City = new(2, "161E26", "26303A", "3E3A31", "857966", "C6AE7F", "E8D9BD");
        private static readonly ScenePalette AcademyDialog = new(0, "DED8C9", "EAE5D9", "436174", "819098", "466477", "253B49",
            "314754", "707B80", "0A1721", "F6F0E3");
        private static readonly ScenePalette EgyptDialog = new(1, "302417", "423220", "735335", "AF915E", "D6B779", "F1DFC0",
            "E5D6BC", "AF9B7D", "170C05", "FFF0D1");
        private static readonly ScenePalette CityDialog = new(2, "202831", "2B3540", "574535", "A18B6D", "D2AE79", "E9DCC7",
            "DBDFDF", "99A2A8", "060A10", "FFF0D5");
        internal static ScenePalette For(string id) => id == "battle-academy" ? Academy : id == "ancient-egypt" ? Egypt : id == "skyscraper" ? City : null;
        internal static ScenePalette DialogFor(string id) => id == "battle-academy" ? AcademyDialog : id == "ancient-egypt" ? EgyptDialog : id == "skyscraper" ? CityDialog : null;
    }

    // One quiet frame language keeps the same controls recognisable in every
    // field. Only the metal tint follows the surrounding scene.
    internal sealed class FightSceneDialogFrame : MaskableGraphic
    {
        internal ScenePalette Palette;
        internal bool ControlFrame, Danger, RowFrame, Focused, Selected;
        internal float Emphasis;
        private float previousEmphasis;
        private bool previousFocused, previousSelected;
        private ScenePalette previousPalette;
        private void LateUpdate()
        {
            if (previousPalette != Palette || previousFocused != Focused || previousSelected != Selected
                || Mathf.Abs(previousEmphasis - Emphasis) > .005f)
            { previousPalette = Palette; previousEmphasis = Emphasis; previousFocused = Focused; previousSelected = Selected; SetVerticesDirty(); }
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (Palette == null) return;
            var r = rectTransform.rect;
            if (r.width <= 2 || r.height <= 2) return;
            var edge = Danger ? FightUI.Danger : Color.Lerp(Palette.Edge, Palette.Accent, Emphasis);
            edge.a = Danger ? .92f : ControlFrame ? .4f + Emphasis * .55f : .78f;
            var inner = new Rect(r.xMin + 1.5f, r.yMin + 1.5f, r.width - 3, r.height - 3);
            if (RowFrame)
            {
                Line(mesh, new Vector2(inner.xMin + 10, inner.yMin), new Vector2(inner.xMax - 10, inner.yMin), .65f, edge);
                if (Emphasis > .25f)
                    Line(mesh, new Vector2(inner.xMin + 3, inner.yMin + 7), new Vector2(inner.xMin + 3, inner.yMax - 7), Selected ? 2.4f : 1.2f, edge);
                if (Focused) Outline(mesh, new Rect(inner.xMin + 3, inner.yMin + 3, inner.width - 6, inner.height - 6), 1.1f, edge, 3);
                return;
            }
            const float cut = 3;
            var shadow = Palette.Shadow; shadow.a = ControlFrame ? .24f : .35f;
            Outline(mesh, new Rect(inner.xMin, inner.yMin - .9f, inner.width, inner.height), 1.4f, shadow, cut);
            Outline(mesh, inner, ControlFrame ? .8f : 1.15f, edge, cut);
            var caughtLight = Color.Lerp(Palette.Edge, Color.white, Palette.Kind == 0 ? .66f : .35f);
            caughtLight.a = ControlFrame ? .42f : .56f;
            Line(mesh, new Vector2(inner.xMin + cut + 2, inner.yMax - .8f), new Vector2(inner.xMax - cut - 2, inner.yMax - .8f), .75f, caughtLight);
            if (ControlFrame)
            {
                if (Focused)
                {
                    var focus = Danger ? FightUI.Danger : Palette.Accent; focus.a = .95f;
                    Outline(mesh, new Rect(inner.xMin + 3, inner.yMin + 3, inner.width - 6, inner.height - 6), 1.1f, focus, Mathf.Max(1, cut - 2));
                }
                return;
            }
            var fine = Palette.Edge; fine.a = .22f;
            Outline(mesh, new Rect(inner.xMin + 4, inner.yMin + 4, inner.width - 8, inner.height - 8), .6f, fine, cut);
        }
        private static void Outline(VertexHelper m, Rect r, float thickness, Color c, float cut)
        {
            cut = Mathf.Min(cut, Mathf.Min(r.width, r.height) * .2f);
            var points = new[] { new Vector2(r.xMin + cut,r.yMin), new Vector2(r.xMax-cut,r.yMin), new Vector2(r.xMax,r.yMin+cut),
                new Vector2(r.xMax,r.yMax-cut), new Vector2(r.xMax-cut,r.yMax), new Vector2(r.xMin+cut,r.yMax),
                new Vector2(r.xMin,r.yMax-cut), new Vector2(r.xMin,r.yMin+cut) };
            for (int i=0; i<points.Length; i++) Line(m,points[i],points[(i+1)%points.Length],thickness,c);
        }
        private static void Line(VertexHelper m, Vector2 a, Vector2 b, float thickness, Color c)
        {
            var d=b-a; if (d.sqrMagnitude<.0001f) return;
            var normal=new Vector2(-d.y,d.x).normalized*thickness*.5f; int start=m.currentVertCount;
            m.AddVert(a-normal,c,Vector2.zero); m.AddVert(a+normal,c,Vector2.zero);
            m.AddVert(b+normal,c,Vector2.zero); m.AddVert(b-normal,c,Vector2.zero);
            m.AddTriangle(start,start+1,start+2); m.AddTriangle(start,start+2,start+3);
        }
    }

    internal sealed class FightSceneDialogControl : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        private Selectable control;
        private FightSceneDialogFrame frame;
        private bool hover, focus, pressed;
        internal bool Latched;
        internal void Bind(Selectable target, FightSceneDialogFrame border) { control=target; frame=border; focus=target != null && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == target.gameObject; }
        internal void Refresh(ScenePalette palette)
        {
            if (control==null || frame==null) return;
            focus=EventSystem.current != null && EventSystem.current.currentSelectedGameObject == control.gameObject;
            var card=control.GetComponentInParent<PopupDuelSelectCardItem>();
            bool enabled=control.IsInteractable() && (card==null || !card.unselectable);
            var toggle=control.GetComponent<SelectionToggle>();
            var nativeToggle=control.GetComponent<UnityEngine.UI.Toggle>();
            bool selected=Latched || (card!=null ? card.selected : toggle!=null ? toggle.isOn : nativeToggle!=null && nativeToggle.isOn);
            frame.Palette=palette; frame.Focused=enabled && focus; frame.Selected=enabled && selected;
            float target=enabled ? pressed ? .45f : selected || focus ? 1 : hover ? .75f : .08f : 0;
            frame.Emphasis=Mathf.Lerp(frame.Emphasis,target,Mathf.Clamp01(Time.unscaledDeltaTime*16));
            frame.canvasRenderer.SetAlpha(!enabled || card!=null && card.unselectable ? .38f : 1);
        }
        public void OnPointerEnter(PointerEventData e) { hover=true; }
        public void OnPointerExit(PointerEventData e) { hover=pressed=false; }
        public void OnPointerDown(PointerEventData e) { if(e.button==PointerEventData.InputButton.Left) pressed=true; }
        public void OnPointerUp(PointerEventData e) { pressed=false; }
        public void OnSelect(BaseEventData e) { focus=true; }
        public void OnDeselect(BaseEventData e) { focus=pressed=false; }
        private void OnDisable() { hover=focus=pressed=Latched=false; if(frame!=null) frame.Focused=frame.Selected=false; }
    }
}
