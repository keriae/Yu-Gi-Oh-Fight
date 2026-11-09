using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


using static YuGiOhFight.Client.Controllers.OcgCore;


namespace YuGiOhFight.Client.UI.Client
{
    // Presentation only: retain the native controls, response handlers, icons,
    // dynamic stat colours, card text and safe-area geometry.
    internal sealed class FightDuelHudSkin : MonoBehaviour
    {
        private bool applied;
        private SceneHintSkin sceneHint;
        private FightDuelHudComposition composition;

        internal static void Attach(ServantUI.OcgCoreUI ui)
        {
            if (ui == null || ui.Root == null) return;
            FightDuelHudSkin skin = ui.GetComponent<FightDuelHudSkin>() ?? ui.gameObject.AddComponent<FightDuelHudSkin>();
            skin.Apply(ui);
        }

        private void Apply(ServantUI.OcgCoreUI ui)
        {
            if (applied) return;
            applied = true;
            StylePlayerName(ui.TextPlayer0Name);
            StylePlayerName(ui.TextPlayer1Name);
            // The original LP font and damage animation remain authoritative.
            if (ui.TextPlayer0LP != null) ui.TextPlayer0LP.color = FightUI.Foreground;
            if (ui.TextPlayer1LP != null) ui.TextPlayer1LP.color = FightUI.Foreground;
            if (ui.TextHint != null)
            {
                ui.TextHint.color = FightUI.CyanText;
                ui.TextHint.lineSpacing = 3;
            }
            if (ui.Hint != null && ui.Hint.TryGetComponent<Image>(out var hintPlate))
                hintPlate.color = new Color(.025f, .055f, .08f, .96f);

            foreach (var control in ui.Root.GetComponentsInChildren<Selectable>(true))
            {
                if (!IsHudControl(control.name)) continue;
                FightDuelControlFeedback feedback = control.GetComponent<FightDuelControlFeedback>()
                    ?? control.gameObject.AddComponent<FightDuelControlFeedback>();
                feedback.Bind(control, ui);
            }
            StyleDescription(ui.CardDescription);
            sceneHint = new SceneHintSkin(ui);
            composition = new FightDuelHudComposition(ui);
        }

        private void LateUpdate() { sceneHint?.Refresh(); composition?.Refresh(); }
        private void OnDisable() { sceneHint?.Restore(); composition?.Restore(); }
        private void OnDestroy() => composition?.Dispose();

        // Placement, tribute, attack target and other board operations share
        // the native Hint banner rather than a Popup prefab. Theme its original
        // animation root so its show/hide timing and pointer behavior survive.
        private sealed class SceneHintSkin
        {
            private readonly GameObject root;
            private readonly TextMeshProUGUI text;
            private readonly RectTransform rootRect;
            private readonly Vector2 nativeSize, textAnchorMin, textAnchorMax, textOffsetMin, textOffsetMax;
            private readonly float nativeFontSize, nativeMinSize, nativeMaxSize;
            private readonly bool nativeAutosize, nativeWrap;
            private readonly TextOverflowModes nativeOverflow;
            private readonly Image plate;
            private readonly Color textColor, textBaseColor;
            private readonly System.Collections.Generic.List<HintImageState> images = new();
            private readonly bool ownsPlate;
            private bool active;
            private string scene;
            internal SceneHintSkin(ServantUI.OcgCoreUI ui)
            {
                root = ui.Hint; text = ui.TextHint;
                rootRect = root != null ? root.transform as RectTransform : null;
                if (rootRect != null) nativeSize = rootRect.sizeDelta;
                if (text != null)
                {
                    textColor = text.color;
                    textBaseColor = text.GetComponent<Presentation.UI.ColorContainerGraphic>()?.baseColor ?? textColor;
                    nativeFontSize = text.fontSize; nativeMinSize = text.fontSizeMin; nativeMaxSize = text.fontSizeMax;
                    nativeAutosize = text.enableAutoSizing; nativeWrap = text.enableWordWrapping; nativeOverflow = text.overflowMode;
                    textAnchorMin = text.rectTransform.anchorMin; textAnchorMax = text.rectTransform.anchorMax;
                    textOffsetMin = text.rectTransform.offsetMin; textOffsetMax = text.rectTransform.offsetMax;
                }
                if (root == null) return;
                foreach (var image in root.GetComponentsInChildren<Image>(true)) images.Add(new HintImageState(image));
                var native = root.GetComponent<Image>();
                if (native != null && native.GetComponent<Mask>() == null) plate = native;
                else
                {
                    var backdrop = FightUI.Node(root.transform, "Fight scene hint surface", 0, 0, 1, 1);
                    FightUI.Stretch(backdrop); backdrop.SetAsFirstSibling();
                    backdrop.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                    plate = backdrop.gameObject.AddComponent<Image>(); plate.raycastTarget = false;
                    ownsPlate = true; backdrop.gameObject.SetActive(false);
                }
            }
            internal void Refresh()
            {
                if (root == null || plate == null) return;
                if (!FightDuelPopupSkin.SceneThemeActive) { Restore(); return; }
                string id = Runtime.Program.instance.ocgcore.DuelBGManager.ActiveSceneId;
                var palette = ScenePalette.DialogFor(id);
                if (palette == null) { Restore(); return; }
                bool changed = !active || scene != id;
                active = true; scene = id;
                if (ownsPlate) plate.gameObject.SetActive(true);
                if (changed)
                {
                    plate.sprite = FightSkin.ButtonSurface; plate.overrideSprite = null;
                    plate.type = Image.Type.Sliced; plate.pixelsPerUnitMultiplier = 1;
                    plate.material = null;
                }
                foreach (var state in images)
                {
                    var image = state.Image;
                    if (image == null || image == plate || image.GetComponent<Mask>() != null) continue;
                    string name = image.name.ToLowerInvariant();
                    if (name.Contains("icon") || name.Contains("arrow") || name.Contains("check")) continue;
                    // Retain renderer alpha from native fade/animation tracks.
                    image.color = new Color(image.color.r, image.color.g, image.color.b, 0);
                    var container = image.GetComponent<Presentation.UI.ColorContainerGraphic>();
                    if (container != null) container.baseColor = image.color;
                }
                // Hint uses SetActive, not an Image-alpha fade track. One
                // fixed reading surface remains translucent on every frame.
                var color = palette.Raised;
                color.a = palette.Kind == 0 ? .80f : .74f;
                plate.color = color;
                var plateContainer = plate.GetComponent<Presentation.UI.ColorContainerGraphic>();
                if (plateContainer != null) plateContainer.baseColor = color;
                if (text != null)
                {
                    // Fit the instruction around its actual text instead of
                    // spanning a thousand-unit ribbon over the field props.
                    text.enableAutoSizing = false; text.fontSize = 26;
                    text.enableWordWrapping = true; text.overflowMode = TextOverflowModes.Overflow;
                    float width = Mathf.Clamp(text.GetPreferredValues(text.text).x + 64, 420, 760);
                    float height = Mathf.Max(62, text.GetPreferredValues(text.text, width - 48, float.PositiveInfinity).y + 22);
                    if (rootRect != null) rootRect.sizeDelta = new Vector2(width, height);
                    FightUI.Stretch(text.rectTransform);
                    text.rectTransform.offsetMin = new Vector2(24, 10); text.rectTransform.offsetMax = new Vector2(-24, -10);
                    var heading = palette.Heading; heading.a = text.color.a;
                    text.color = heading;
                    var container = text.GetComponent<Presentation.UI.ColorContainerGraphic>();
                    if (container != null) container.baseColor = heading;
                }
            }
            internal void Restore()
            {
                if (!active) return;
                foreach (var state in images) state.Restore();
                if (text != null)
                {
                    text.fontSize = nativeFontSize; text.fontSizeMin = nativeMinSize; text.fontSizeMax = nativeMaxSize;
                    text.enableAutoSizing = nativeAutosize; text.enableWordWrapping = nativeWrap; text.overflowMode = nativeOverflow;
                    text.rectTransform.anchorMin = textAnchorMin; text.rectTransform.anchorMax = textAnchorMax;
                    text.rectTransform.offsetMin = textOffsetMin; text.rectTransform.offsetMax = textOffsetMax;
                    text.color = textColor;
                    var container = text.GetComponent<Presentation.UI.ColorContainerGraphic>();
                    if (container != null) container.baseColor = textBaseColor;
                }
                if (rootRect != null) rootRect.sizeDelta = nativeSize;
                if (ownsPlate && plate != null) plate.gameObject.SetActive(false);
                active = false; scene = null;
            }
            private sealed class HintImageState
            {
                internal readonly Image Image;
                internal readonly Color Color;
                private readonly Color baseColor;
                private readonly Sprite sprite, overrideSprite;
                private readonly Material material;
                private readonly Image.Type type;
                private readonly float pixelsPerUnit;
                internal HintImageState(Image image)
                {
                    Image = image; Color = image.color; sprite = image.sprite; overrideSprite = image.overrideSprite;
                    baseColor = image.GetComponent<Presentation.UI.ColorContainerGraphic>()?.baseColor ?? Color;
                    material = image.material; type = image.type; pixelsPerUnit = image.pixelsPerUnitMultiplier;
                }
                internal void Restore()
                {
                    if (Image == null) return;
                    Image.color = Color; Image.sprite = sprite; Image.overrideSprite = overrideSprite; Image.material = material;
                    var container = Image.GetComponent<Presentation.UI.ColorContainerGraphic>();
                    if (container != null) container.baseColor = baseColor;
                    Image.type = type; Image.pixelsPerUnitMultiplier = pixelsPerUnit;
                }
            }
        }

        private static bool IsHudControl(string name)
            => name == "ButtonStop" || name == "ButtonPlay" || name == "ButtonAcc" || name == "ButtonNor"
            || name == "ButtonTiming" || name == "ButtonLog" || name == "ButtonAttachedEffect"
            || name == "ButtonSetting" || name == "ButtonSettings" || name == "ButtonInfo";

        private static void StylePlayerName(TextMeshProUGUI text)
        {
            if (text == null) return;
            text.color = FightUI.Foreground;
            text.characterSpacing = .5f;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.fontSizeMax = text.fontSize;
            text.fontSizeMin = Mathf.Min(text.fontSize, 22);
            text.enableAutoSizing = true;
            text.raycastTarget = false;
            if (text.transform.Find("Fight player name hover") == null)
            {
                // The opponent LP group deliberately rejects raycasts. A
                // transparent child receives hover without changing the name's fade.
                var hover = FightUI.Node(text.transform, "Fight player name hover", 0, 0, 1, 1);
                FightUI.Stretch(hover);
                var surface = hover.gameObject.AddComponent<Image>();
                surface.color = Color.clear;
                surface.canvasRenderer.cullTransparentMesh = false;
                var hoverGroup = hover.gameObject.AddComponent<CanvasGroup>();
                hoverGroup.ignoreParentGroups = true;
                hoverGroup.blocksRaycasts = true;
                hover.gameObject.AddComponent<FightDuelNameRaycastFilter>();
            }
            // Empty Hint deliberately uses the current displayed name on hover.
            FightTooltip tooltip = text.GetComponent<FightTooltip>() ?? text.gameObject.AddComponent<FightTooltip>();
            tooltip.LocalizeHint = false;
        }

        private static void StyleDescription(Duel.CardDescription description)
        {
            if (description == null) return;
            Presentation.Elements.ElementObjectManager manager = description.GetComponent<Presentation.Elements.ElementObjectManager>();
            if (manager == null) return;
            var window = manager.GetElement<RectTransform>("Window");
            if (window == null) return;
            var backing = window.GetComponent<Image>();
            if (backing != null) backing.color = FightUI.PanelColor;
            else
            {
                var backdrop = FightUI.Node(window, "Fight card information backdrop", 0, 0, 1, 1);
                FightUI.Stretch(backdrop);
                backdrop.SetAsFirstSibling();
                backing = backdrop.gameObject.AddComponent<Image>();
                backing.color = new Color(.025f, .055f, .08f, .97f);
                backing.raycastTarget = false;
                backdrop.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            }
            var edge = FightUI.Node(window, "Fight card information frame", 0, 0, 1, 1);
            FightUI.Stretch(edge);
            FightOutline outline = edge.gameObject.AddComponent<FightOutline>(); outline.effectColor = FightUI.GoldBorder;
            edge.gameObject.AddComponent<FightFrame>().Bind(outline);
            FightDuelAuxiliarySkin.Attach(window, new[] { backing }, null);
            var effect = manager.GetElement<TextMeshProUGUI>("TextDescription");
            if (effect != null)
            {
                effect.color = FightUI.Foreground;
                effect.lineSpacing = 5;
                effect.paragraphSpacing = 8;
                // CardDescription.Show owns the user's configured font size and
                // its rich text highlights for changed and inactive effects.
            }
            var cardButton = manager.GetElement<Button>("CardButton");
            if (cardButton != null) FightUI.Hint(cardButton.gameObject, "查看完整卡片信息");
            foreach (var scrollbar in window.GetComponentsInChildren<Scrollbar>(true))
            {
                if (scrollbar.targetGraphic == null) continue;
                scrollbar.targetGraphic.color = FightUI.GoldBorder;
                var colours = scrollbar.colors;
                colours.normalColor = Color.white;
                colours.highlightedColor = FightUI.Active;
                colours.pressedColor = FightUI.Cyan;
                colours.selectedColor = FightUI.CyanText;
                colours.disabledColor = new Color(.45f, .45f, .45f, .5f);
                colours.fadeDuration = .12f;
                scrollbar.colors = colours;
            }
        }
    }

    internal sealed class FightDuelNameRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        private TextMeshProUGUI text;
        private void Awake() { text = transform.parent.GetComponent<TextMeshProUGUI>(); }
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
            => text != null && text.isActiveAndEnabled
                && text.color.a * text.canvasRenderer.GetInheritedAlpha() > .001f;
    }

    internal sealed class FightDuelControlFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        private Selectable control;
        private ServantUI.OcgCoreUI ui;
        private FightDuelFocusRing ring;
        private FightSceneGlyphFrame sceneBezel;
        private FightTooltip hint;
        private bool hover, focus, pressed;
        private string previousHint;
        private bool composed;
        internal void SetComposed(bool value) => composed = value;

        internal void Bind(Selectable target, ServantUI.OcgCoreUI owner)
        {
            control = target; ui = owner;
            if (ring == null)
            {
                var overlay = FightUI.Node(target.transform, "Fight HUD focus ring", 0, 0, 1, 1);
                FightUI.Stretch(overlay);
                overlay.offsetMin = new Vector2(-3, -3); overlay.offsetMax = new Vector2(3, 3);
                overlay.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                ring = overlay.gameObject.AddComponent<FightDuelFocusRing>();
                ring.raycastTarget = false; ring.color = new Color(0, 0, 0, 0);
            }
            hint = target.GetComponent<FightTooltip>() ?? target.gameObject.AddComponent<FightTooltip>();
            if (sceneBezel == null)
            {
                var bezel = FightUI.Node(target.transform, "Fight HUD scene bezel", 0, 0, 1, 1);
                FightUI.Stretch(bezel);
                bezel.offsetMin = new Vector2(-1, -1); bezel.offsetMax = new Vector2(1, 1);
                bezel.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                sceneBezel = bezel.gameObject.AddComponent<FightSceneGlyphFrame>();
                sceneBezel.raycastTarget = false; bezel.gameObject.SetActive(false);
            }
            focus = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == target.gameObject;
            RefreshHint();
        }

        private void LateUpdate()
        {
            if (control == null || ring == null) return;
            if (composed)
            {
                if (sceneBezel != null) sceneBezel.gameObject.SetActive(false);
                ring.gameObject.SetActive(false); RefreshHint(); return;
            }
            bool available = control.IsInteractable();
            var palette = FightDuelPopupSkin.SceneThemeActive
                ? ScenePalette.For(Runtime.Program.instance.ocgcore.DuelBGManager.ActiveSceneId) : null;
            if (sceneBezel != null)
            {
                bool themed = palette != null;
                sceneBezel.gameObject.SetActive(themed); ring.gameObject.SetActive(!themed);
                if (themed)
                {
                    sceneBezel.Palette = palette;
                    sceneBezel.Artwork = FightDuelPopupSkin.DialogArtwork.Control(Runtime.Program.instance.ocgcore.DuelBGManager.ActiveSceneId, 1);
                    sceneBezel.Emphasis = Mathf.Lerp(sceneBezel.Emphasis, available ? pressed ? .35f : focus ? 1 : hover ? .75f : .08f : 0,
                        Mathf.Clamp01(Services.ReplayVideoExport.UnscaledDelta * 14));
                    sceneBezel.color = new Color(1, 1, 1, available ? pressed ? .72f : 1 : .35f);
                    sceneBezel.SetVerticesDirty();
                }
            }
            var target = !available ? new Color(.36f, .4f, .44f, .3f)
                : palette != null ? focus || hover ? palette.Accent : palette.Edge
                : focus ? FightUI.Cyan : hover ? FightUI.Active : FightUI.GoldBorder;
            target.a = !available ? .3f : pressed ? .45f : focus || hover ? .95f : .22f;
            var next = Color.Lerp(ring.color, target, Mathf.Clamp01(Services.ReplayVideoExport.UnscaledDelta * 12));
            if (Mathf.Abs(next.r - target.r) + Mathf.Abs(next.g - target.g)
                + Mathf.Abs(next.b - target.b) + Mathf.Abs(next.a - target.a) < .005f) next = target;
            if (ring.color != next) ring.color = next;
            RefreshHint();
        }

        private void RefreshHint()
        {
            if (control == null || hint == null) return;
            string message = control.name switch
            {
                "ButtonStop" => "暂停播放",
                "ButtonPlay" => "继续播放",
                "ButtonAcc" => "加速播放 · 使用对局设置中的速度",
                "ButtonNor" => "恢复正常速度",
                "ButtonTiming" => chainCondition switch
                {
                    ChainCondition.No => "连锁响应：关闭\n单击切换响应方式",
                    ChainCondition.Smart => "连锁响应：自动\n单击切换响应方式",
                    _ => "连锁响应：始终确认\n单击切换响应方式"
                },
                "ButtonLog" => ui != null && ui.DuelLog != null && ui.DuelLog.showing ? "收起对局日志" : "查看对局日志",
                "ButtonInfo" => "按住查看对局信息",
                "ButtonAttachedEffect" => "查看已附加效果",
                _ => "对局设置"
            };
            if (message == previousHint) return;
            previousHint = message; hint.Hint = message;
        }

        public void OnPointerEnter(PointerEventData eventData) { hover = true; }
        public void OnPointerExit(PointerEventData eventData) { hover = false; pressed = false; }
        public void OnPointerDown(PointerEventData eventData) { if (eventData.button == PointerEventData.InputButton.Left) pressed = true; }
        public void OnPointerUp(PointerEventData eventData) { pressed = false; }
        public void OnSelect(BaseEventData eventData) { focus = true; }
        public void OnDeselect(BaseEventData eventData) { focus = false; pressed = false; }
        private void OnDisable() { hover = focus = pressed = false; }
    }

    internal sealed class FightDuelFocusRing : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            var rect = rectTransform.rect;
            if (rect.width <= 0 || rect.height <= 0 || color.a <= .001f) return;
            var radius = new Vector2(rect.width * .5f - 1, rect.height * .5f - 1);
            var inner = new Vector2(Mathf.Max(0, radius.x - 1.5f), Mathf.Max(0, radius.y - 1.5f));
            const int segments = 48;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * (float)Math.PI * 2 / segments;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                vertices.AddVert(rect.center + Vector2.Scale(direction, radius), color, Vector2.zero);
                vertices.AddVert(rect.center + Vector2.Scale(direction, inner), color, Vector2.zero);
            }
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                vertices.AddTriangle(i * 2, next * 2, i * 2 + 1);
                vertices.AddTriangle(i * 2 + 1, next * 2, next * 2 + 1);
            }
        }
    }
}
