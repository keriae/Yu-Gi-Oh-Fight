using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace MDPro3.UI.Client
{
    // Scalable surfaces and geometry, independent of the decorative artwork.
    internal static class FightSkin
    {
        private static Sprite surface, buttonSurface, actionSurface, ambient;
        private static TMP_FontAsset titleFont;
        private static bool triedTitleFont;
        internal static Sprite Surface => surface != null ? surface : surface = MakeSurface(0);
        internal static Sprite ButtonSurface => buttonSurface != null ? buttonSurface : buttonSurface = MakeSurface(1);
        internal static Sprite ActionSurface => actionSurface != null ? actionSurface : actionSurface = MakeSurface(2);
        internal static Sprite Ambient => ambient != null ? ambient : ambient = MakeAmbient();
        internal static TMP_FontAsset TitleFont
        {
            get
            {
                if (triedTitleFont) return titleFont;
                triedTitleFont = true;
                try
                {
                    const string songti = "/System/Library/Fonts/Supplemental/Songti.ttc";
                    titleFont = File.Exists(songti)
                        ? TMP_FontAsset.CreateFontAsset(songti, 1, 72, 9, GlyphRenderMode.SDFAA, 1024, 1024)
                        : TMP_FontAsset.CreateFontAsset("Songti SC", "Bold", 72);
                    if (titleFont == null) return null;
                    titleFont.name = "Fight inscription title";
                    titleFont.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { Program.instance.ui_.tmpFont };
                    titleFont.TryAddCharacters("唤醒传说中的白龙");
                }
                catch (Exception) { titleFont = null; }
                return titleFont;
            }
        }
        private static Sprite MakeSurface(int kind)
        {
            const int size = 48, cut = 7;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Fight scalable surface " + kind;
            texture.wrapMode = TextureWrapMode.Clamp; texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var edge = UnityEngine.Mathf.Min(UnityEngine.Mathf.Min(x + y, x + size - 1 - y), UnityEngine.Mathf.Min(size - 1 - x + y, size * 2 - 2 - x - y));
                    var alpha = UnityEngine.Mathf.Clamp01(edge - cut + 1);
                    var t = y / (float)(size - 1);
                    var shade = kind == 0 ? UnityEngine.Mathf.Lerp(.84f, 1.08f, t)
                        : kind == 1 ? UnityEngine.Mathf.Lerp(.74f, 1.12f, t)
                        : .58f + .53f * UnityEngine.Mathf.Pow(UnityEngine.Mathf.Abs(t - .48f) * 2, 2) + .2f * t;
                    pixels[y * size + x] = new Color(shade, shade, shade, alpha);
                }
            texture.SetPixels(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(9, 9, 9, 9));
        }
        private static Sprite MakeAmbient()
        {
            const int width = 512, height = 288;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp; texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)width, v = y / (float)height;
                    float blue = Mathf.Exp(-((u - .68f) * (u - .68f) * 7 + (v - .62f) * (v - .62f) * 5));
                    float low = Mathf.Exp(-((u - .08f) * (u - .08f) * 14 + (v - .02f) * (v - .02f) * 13));
                    float arc = Mathf.Abs(Mathf.Sqrt((u - .78f) * (u - .78f) + (v - .14f) * (v - .14f)) - .67f);
                    float line = Mathf.Clamp01((.003f - arc) / .003f) * .018f;
                    float grain = ((x * 29 + y * 71) % 19) / 19f * .002f;
                    pixels[y * width + x] = new Color(.016f + blue * .009f + low * .003f + grain + line,
                        .035f + blue * .021f + low * .027f + grain + line, .052f + blue * .037f + low * .039f + grain + line, 1);
                }
            texture.SetPixels(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .5f));
        }
    }

    // Retains the Outline API used by existing selection controls. The actual
    // border is a separate mesh, so it can follow clipped corners precisely.
    internal sealed class FightOutline : Outline
    {
        public override void ModifyMesh(VertexHelper vertices) { }
    }

    internal sealed class FightFrame : MaskableGraphic
    {
        private Outline source;
        private Image surface;
        private Color lastColor;
        private bool lastEnabled, hover, focused;
        private float highlight, lastHighlight;
        internal bool Primary, ButtonFrame, InputFrame;
        internal void Bind(Outline outline, Image image)
        {
            // Original controls can own a LayoutGroup. Decorative overlays
            // must not consume a row or be collapsed by that layout.
            var layout = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>(); layout.ignoreLayout = true;
            source = outline; surface = image; raycastTarget = false; SetVerticesDirty();
        }
        internal void Feedback(bool pointer, bool focus)
        {
            if (hover == pointer && focused == focus) return;
            hover = pointer; focused = focus; SetVerticesDirty();
        }
        private void LateUpdate()
        {
            if (source == null) return;
            highlight = Mathf.Clamp01(highlight + (hover ? 1 : -1) * Time.unscaledDeltaTime * 7);
            if (Mathf.Abs(lastHighlight - highlight) > .002f) { lastHighlight = highlight; SetVerticesDirty(); }
            if (lastColor != source.effectColor || lastEnabled != source.enabled)
            { lastColor = source.effectColor; lastEnabled = source.enabled; SetVerticesDirty(); }
            if (transform.GetSiblingIndex() != transform.parent.childCount - 1) transform.SetAsLastSibling();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (source == null) return;
            if (!source.enabled)
            {
                if (focused) Polygon(vh, Corners(rectTransform.rect, 5, InputFrame ? 2 : 1), InputFrame ? 2 : 1.5f, InputFrame ? FightUI.Cyan : FightUI.Active);
                else if (highlight > 0)
                {
                    var r0 = rectTransform.rect; var glow = FightUI.Gold; glow.a = highlight * .7f;
                    Segment(vh, new Vector2(r0.xMin + 12, r0.yMin + 1), new Vector2(r0.xMax - 12, r0.yMin + 1), 1, glow);
                }
                return;
            }
            var r = rectTransform.rect; var c = source.effectColor;
            if (highlight > 0) c = Color.Lerp(c, Primary || InputFrame ? FightUI.CyanText : FightUI.Active, .55f * highlight);
            var control = GetComponentInParent<Selectable>();
            if (control != null && !control.interactable) c.a *= .4f;
            var cut = UnityEngine.Mathf.Min(7, UnityEngine.Mathf.Min(r.width * .05f, r.height * .14f));
            var points = Corners(r, cut, 1);
            if (Primary)
                for (int i = 3; i > 0; i--)
                {
                    var glow = c; glow.a *= .035f * (4 - i);
                    Polygon(vh, Corners(r, cut + i, -i), 1.2f, glow);
                }
            Polygon(vh, points, Primary ? 1.5f : 1, c);
            var inner = c; inner.a *= Primary ? .27f : .15f;
            Polygon(vh, Corners(r, UnityEngine.Mathf.Max(2, cut - 2), 3), 1, inner);
            if (focused)
            {
                var focus = InputFrame ? FightUI.Cyan : Primary ? FightUI.CyanText : FightUI.Active;
                focus.a = InputFrame ? 1 : .8f;
                // Keep an input's focus ring inside the control so scroll masks
                // cannot trim it, and leave validation's red frame intact.
                Polygon(vh, Corners(r, cut, InputFrame ? 2 : -2), InputFrame ? 2 : 1.5f, focus);
            }
            if (!ButtonFrame && r.width > 120 && r.height > 80)
            {
                // Small metal inlays at the four corner joins.
                for (int i = 0; i < 4; i++)
                {
                    var x = i % 2 == 0 ? r.xMin + 3 : r.xMax - 3;
                    var y = i < 2 ? r.yMax - 3 : r.yMin + 3;
                    var dx = i % 2 == 0 ? 1 : -1; var dy = i < 2 ? -1 : 1;
                    Segment(vh, new Vector2(x, y + 10 * dy), new Vector2(x + 10 * dx, y), 1, c);
                    var jewel = new Vector2(x + 5 * dx, y + 5 * dy);
                    var bright = c; bright.a *= .7f;
                    Segment(vh, jewel + new Vector2(-2, 0), jewel + new Vector2(0, 2), 1, bright);
                    Segment(vh, jewel + new Vector2(0, 2), jewel + new Vector2(2, 0), 1, bright);
                }
            }
        }
        private static Vector2[] Corners(Rect r, float cut, float inset)
        {
            var l = r.xMin + inset; var rt = r.xMax - inset; var b = r.yMin + inset; var t = r.yMax - inset;
            return new[] { new Vector2(l + cut, t), new Vector2(rt - cut, t), new Vector2(rt, t - cut),
                new Vector2(rt, b + cut), new Vector2(rt - cut, b), new Vector2(l + cut, b), new Vector2(l, b + cut), new Vector2(l, t - cut) };
        }
        internal static void Polygon(VertexHelper vh, Vector2[] points, float weight, Color tint)
        {
            for (int i = 0; i < points.Length; i++) Segment(vh, points[i], points[(i + 1) % points.Length], weight, tint);
        }
        internal static void Segment(VertexHelper vh, Vector2 a, Vector2 b, float weight, Color tint)
        {
            var normal = new Vector2(-(b - a).y, (b - a).x).normalized * weight * .5f;
            var start = vh.currentVertCount;
            vh.AddVert(a - normal, tint, Vector2.zero); vh.AddVert(a + normal, tint, Vector2.zero);
            vh.AddVert(b + normal, tint, Vector2.zero); vh.AddVert(b - normal, tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }
    }
    internal sealed class FightInputFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private Selectable control;
        private TMP_InputField tmp;
        private InputField legacy;
        private FightFrame frame;
        private bool pointer;
        internal bool VisualFocused { get; private set; }
        internal void Bind(Selectable input)
        {
            control = input; tmp = input as TMP_InputField; legacy = input as InputField;
            frame = input.GetComponentInChildren<FightFrame>(true);
            if (frame == null)
            {
                var outline = input.GetComponent<Outline>() ?? input.gameObject.AddComponent<FightOutline>();
                outline.effectColor = FightUI.Border;
                var root = FightUI.Node(input.transform, "Input focus frame", 0, 0, 0, 0); FightUI.Stretch(root);
                frame = root.gameObject.AddComponent<FightFrame>();
                frame.Bind(outline, input.targetGraphic as Image);
            }
            frame.InputFrame = frame.ButtonFrame = true;
            Synchronize();
        }
        private void LateUpdate() { Synchronize(); }
        private void Synchronize()
        {
            bool editing = tmp != null ? tmp.isFocused : legacy != null && legacy.isFocused;
            bool selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
            VisualFocused = control != null && control.isActiveAndEnabled && control.IsInteractable() && (editing || selected);
            frame?.Feedback(pointer && control != null && control.IsInteractable(), VisualFocused);
        }
        public void OnPointerEnter(PointerEventData e) { pointer = true; Synchronize(); }
        public void OnPointerExit(PointerEventData e) { pointer = false; Synchronize(); }
        public void OnSelect(BaseEventData e) { Synchronize(); }
        public void OnDeselect(BaseEventData e) { Synchronize(); }
        private void OnDisable() { pointer = VisualFocused = false; frame?.Feedback(false, false); }
    }
    internal sealed class FightButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private FightFrame frame; private bool pointer, focus;
        internal void Bind(FightFrame border)
        {
            frame = border;
            focus = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
            frame?.Feedback(pointer, focus);
        }
        public void OnPointerEnter(PointerEventData e) { pointer = true; frame?.Feedback(pointer, focus); }
        public void OnPointerExit(PointerEventData e) { pointer = false; frame?.Feedback(pointer, focus); }
        public void OnSelect(BaseEventData e) { focus = true; frame?.Feedback(pointer, focus); }
        public void OnDeselect(BaseEventData e) { focus = false; frame?.Feedback(pointer, focus); }
        private void OnDisable() { pointer = focus = false; frame?.Feedback(false, false); }
    }
    internal sealed class FightTextActionFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private Button control;
        private Image underline;
        private bool pointer, focus;
        private float opacity;
        internal void Bind(Button button, Image rule)
        {
            control = button; underline = rule; underline.canvasRenderer.SetColor(new Color(1, 1, 1, 0));
        }
        private void Update()
        {
            if (underline == null || control == null) return;
            float wanted = control.IsInteractable() && (pointer || focus) ? 1 : 0;
            if (Mathf.Abs(wanted - opacity) < .001f) return;
            float next = Mathf.Clamp01(opacity + (wanted > opacity ? 1 : -1) * Time.unscaledDeltaTime * 8);
            if (Mathf.Abs(next - opacity) < .001f) return;
            opacity = next; underline.canvasRenderer.SetColor(new Color(1, 1, 1, opacity));
        }
        public void OnPointerEnter(PointerEventData e) { pointer = true; }
        public void OnPointerExit(PointerEventData e) { pointer = false; }
        public void OnSelect(BaseEventData e) { focus = true; }
        public void OnDeselect(BaseEventData e) { focus = false; }
        private void OnDisable() { pointer = focus = false; opacity = 0; if (underline != null) underline.canvasRenderer.SetColor(new Color(1, 1, 1, 0)); }
    }
    internal sealed class FightSwitchFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        private Selectable control;
        private FightToggleGraphic graphic;
        private TMP_Text label;
        private bool pointer, pressed;
        internal void Bind(Selectable selectable, FightToggleGraphic toggle, TMP_Text caption = null)
        {
            control = selectable; graphic = toggle; label = caption; Refresh();
        }
        internal void Refresh()
        {
            if (graphic == null || control == null) return;
            bool available = isActiveAndEnabled && control.isActiveAndEnabled && control.IsInteractable();
            bool focused = available && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == control.gameObject;
            graphic.SetFeedback(available, pointer && available, focused, pressed && available);
            if (label != null)
            {
                var tint = available ? pointer || focused ? FightUI.CyanText : FightUI.Foreground : FightUI.Muted;
                if (!available) tint.a *= .55f;
                label.color = tint;
            }
        }
        private void LateUpdate() { Refresh(); }
        private void OnEnable() { Refresh(); }
        private void OnDisable() { pointer = pressed = false; Refresh(); }
        public void OnPointerEnter(PointerEventData e) { pointer = true; Refresh(); }
        public void OnPointerExit(PointerEventData e) { pointer = pressed = false; Refresh(); }
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) { pressed = true; Refresh(); } }
        public void OnPointerUp(PointerEventData e) { pressed = false; Refresh(); }
        public void OnSelect(BaseEventData e) { Refresh(); }
        public void OnDeselect(BaseEventData e) { Refresh(); }
    }
    internal sealed class FightToggleGraphic : MaskableGraphic
    {
        private bool value, initialized, interactable = true, hover, focused, pressed;
        private float thumb, highlight;
        internal bool Value
        {
            get => value;
            set
            {
                if (this.value == value) return;
                this.value = value;
                if (!initialized) thumb = value ? 1 : 0;
                SetVerticesDirty();
            }
        }
        internal bool Interactable => interactable;
        internal bool Hovered => hover;
        internal bool Focused => focused;
        internal void SetFeedback(bool available, bool pointer, bool focus, bool down = false)
        {
            if (!initialized) { initialized = true; thumb = value ? 1 : 0; }
            pointer &= available; focus &= available; down &= available;
            if (interactable == available && hover == pointer && focused == focus && pressed == down) return;
            interactable = available; hover = pointer; focused = focus; pressed = down; SetVerticesDirty();
        }
        private void Update()
        {
            float nextThumb = Step(thumb, value ? 1 : 0, Time.unscaledDeltaTime * 9);
            float nextHighlight = Step(highlight, interactable && (hover || focused) ? 1 : 0, Time.unscaledDeltaTime * 8);
            if (Mathf.Abs(nextThumb - thumb) < .001f && Mathf.Abs(nextHighlight - highlight) < .001f) return;
            thumb = nextThumb; highlight = nextHighlight; SetVerticesDirty();
        }
        private static float Step(float current, float target, float step) => current + Mathf.Clamp(target - current, -step, step);
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect;
            if (r.width <= 0 || r.height <= 0) return;
            float scale = Mathf.Min(r.width / 46, r.height / 24), cy = r.center.y;
            float alpha = color.a * (interactable ? 1 : .42f);
            Color Tint(Color tint) { tint.a *= alpha; return tint; }
            var track = new Rect(r.xMin, cy - 12 * scale, r.width, 24 * scale);
            var fill = value ? FightUI.Hex("259FAF") : FightUI.Hex("354955");
            fill = Color.Lerp(fill, value ? FightUI.Hex("32B2C2") : FightUI.Hex("435D6B"), highlight * .6f);
            Capsule(vh, track, 12 * scale, Tint(fill));
            if (focused)
            {
                var focus = FightUI.Cyan; focus.a = .7f;
                CapsuleRing(vh, new Rect(r.xMin - 3 * scale, cy - 15 * scale, r.width + 6 * scale, 30 * scale),
                    15 * scale, 1.2f * scale, Tint(focus));
            }
            var center = new Vector2(Mathf.Lerp(r.xMin + 12 * scale, r.xMax - 12 * scale, thumb), cy);
            var knob = interactable ? FightUI.Hex("F0F6F8") : FightUI.Hex("B2C0C7");
            Round(vh, center, (pressed ? 9.5f : 10) * scale, Tint(knob));
        }
        private static Vector2[] CapsulePoints(Rect rect, float radius)
        {
            const int segments = 12;
            var points = new Vector2[(segments + 1) * 2];
            var right = new Vector2(rect.xMax - radius, rect.center.y); var left = new Vector2(rect.xMin + radius, rect.center.y);
            for (int i = 0; i <= segments; i++)
            {
                float angle = -(float)Math.PI * .5f + (float)Math.PI * i / segments;
                points[i] = right + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                angle += (float)Math.PI;
                points[segments + 1 + i] = left + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            return points;
        }
        private static void Capsule(VertexHelper vh, Rect rect, float radius, Color tint)
        {
            var points = CapsulePoints(rect, radius); int start = vh.currentVertCount;
            vh.AddVert(rect.center, tint, Vector2.zero);
            foreach (var point in points) vh.AddVert(point, tint, Vector2.zero);
            for (int i = 0; i < points.Length; i++) vh.AddTriangle(start, start + i + 1, start + (i + 1) % points.Length + 1);
        }
        private static void CapsuleRing(VertexHelper vh, Rect rect, float radius, float weight, Color tint)
        {
            var outside = CapsulePoints(rect, radius);
            var inside = CapsulePoints(new Rect(rect.xMin + weight, rect.yMin + weight, rect.width - weight * 2, rect.height - weight * 2), radius - weight);
            int start = vh.currentVertCount;
            for (int i = 0; i < outside.Length; i++) { vh.AddVert(outside[i], tint, Vector2.zero); vh.AddVert(inside[i], tint, Vector2.zero); }
            for (int i = 0; i < outside.Length; i++)
            {
                int next = (i + 1) % outside.Length;
                vh.AddTriangle(start + i * 2, start + next * 2, start + i * 2 + 1);
                vh.AddTriangle(start + i * 2 + 1, start + next * 2, start + next * 2 + 1);
            }
        }
        internal static void Round(VertexHelper vh, Vector2 center, float radius, Color tint)
        {
            for (int i = 0; i < 32; i++)
            {
                float a = i * (float)Math.PI / 16, b = (i + 1) * (float)Math.PI / 16; var start = vh.currentVertCount;
                vh.AddVert(center, tint, Vector2.zero);
                vh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, tint, Vector2.zero);
                vh.AddVert(center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, tint, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
            }
        }
        internal static void Quad(VertexHelper vh, float l, float b, float r, float t, Color tint)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector2(l, b), tint, Vector2.zero); vh.AddVert(new Vector2(l, t), tint, Vector2.zero);
            vh.AddVert(new Vector2(r, t), tint, Vector2.zero); vh.AddVert(new Vector2(r, b), tint, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
    }
    internal sealed class FightCardBack : MaskableGraphic
    {
        internal bool Loading, Failed;
        private float pulse;
        private void Update()
        {
            if (!Loading) return;
            float next = Mathf.Floor(Time.unscaledTime * 6);
            if (next != pulse) { pulse = next; SetVerticesDirty(); }
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect; var c = r.center;
            float width = Mathf.Min(r.width * .78f, r.height * .60f), height = Mathf.Min(r.height * .86f, width * 1.46f);
            FightToggleGraphic.Quad(vh, c.x - width / 2, c.y - height / 2, c.x + width / 2, c.y + height / 2, FightUI.Hex("0D1D28"));
            var edge = color; edge.a *= .55f;
            var corners = new[] { c + new Vector2(-width / 2, height / 2), c + new Vector2(width / 2, height / 2), c + new Vector2(width / 2, -height / 2), c + new Vector2(-width / 2, -height / 2) };
            FightFrame.Polygon(vh, corners, 1, edge);
            for (int i = 0; i < 3; i++)
            {
                float d = width * (.16f + i * .055f); var tint = color; tint.a = .22f + i * .06f;
                FightFrame.Polygon(vh, new[] { c + Vector2.up * d * 1.3f, c + Vector2.right * d, c + Vector2.down * d * 1.3f, c + Vector2.left * d }, 1, tint);
            }
            FightFrame.Segment(vh, c + new Vector2(0, height * .28f), c + new Vector2(0, height * .40f), 1, edge);
            FightFrame.Segment(vh, c + new Vector2(0, -height * .28f), c + new Vector2(0, -height * .40f), 1, edge);
            if (Loading)
                for (int i = 0; i < 3; i++)
                {
                    var tint = color; tint.a = .25f + .4f * Mathf.Clamp01(Mathf.Sin(Time.unscaledTime * 5 - i * .9f));
                    FightToggleGraphic.Round(vh, new Vector2(c.x + (i - 1) * 7, c.y - height * .42f), 1.2f, tint);
                }
            if (Failed)
            {
                var center = c + new Vector2(0, -height * .40f); var tint = FightUI.Muted;
                FightFrame.Segment(vh, center + new Vector2(-3, -3), center + new Vector2(3, 3), 1, tint);
                FightFrame.Segment(vh, center + new Vector2(-3, 3), center + new Vector2(3, -3), 1, tint);
            }
        }
    }
    internal sealed class FightPageReveal : MonoBehaviour
    {
        private CanvasGroup group;
        private void Awake() { group = gameObject.AddComponent<CanvasGroup>(); group.alpha = .05f; }
        private void Update()
        {
            group.alpha = Mathf.Min(1, group.alpha + Time.unscaledDeltaTime * 5);
            if (group.alpha >= 1) enabled = false;
        }
    }
    internal sealed class FightIcon : MaskableGraphic
    {
        internal string Kind;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect; var scale = UnityEngine.Mathf.Min(r.width, r.height);
            var center = r.center;
            Action<Vector2, Vector2, float> line = (a, b, width) => FightFrame.Segment(vh, center + a * scale, center + b * scale, width, color);
            if (Kind == "close")
            {
                line(new Vector2(-.3f, .3f), new Vector2(.3f, -.3f), 1.7f);
                line(new Vector2(.3f, .3f), new Vector2(-.3f, -.3f), 1.7f);
            }
            else if (Kind == "deck")
            {
                FightFrame.Polygon(vh, new[] { center + new Vector2(-.24f, .28f) * scale, center + new Vector2(.29f, .28f) * scale,
                    center + new Vector2(.29f, -.4f) * scale, center + new Vector2(-.24f, -.4f) * scale }, 1.5f, color);
                line(new Vector2(-.37f, -.2f), new Vector2(-.37f, .4f), 1.5f);
                line(new Vector2(-.37f, .4f), new Vector2(.15f, .4f), 1.5f);
                FightFrame.Polygon(vh, new[] { center + new Vector2(.02f, .13f) * scale, center + new Vector2(.17f, -.02f) * scale,
                    center + new Vector2(.02f, -.17f) * scale, center + new Vector2(-.13f, -.02f) * scale }, 1.2f, color);
            }
            else if (Kind == "chevron")
            {
                line(new Vector2(-.34f, .16f), new Vector2(0, -.18f), 1.6f);
                line(new Vector2(0, -.18f), new Vector2(.34f, .16f), 1.6f);
            }
            else if (Kind == "settings")
            {
                const int count = 32;
                for (int i = 0; i < count; i++)
                {
                    var a = (float)(i * Math.PI * 2 / count); var b = (float)((i + 1) * Math.PI * 2 / count);
                    var radius = i % 4 < 2 ? .43f : .34f;
                    var ra = new Vector2(UnityEngine.Mathf.Cos(a), UnityEngine.Mathf.Sin(a)); var rb = new Vector2(UnityEngine.Mathf.Cos(b), UnityEngine.Mathf.Sin(b));
                    var start = vh.currentVertCount;
                    vh.AddVert(center + ra * (scale * .21f), color, Vector2.zero); vh.AddVert(center + ra * (scale * radius), color, Vector2.zero);
                    vh.AddVert(center + rb * (scale * radius), color, Vector2.zero); vh.AddVert(center + rb * (scale * .21f), color, Vector2.zero);
                    vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
                }
            }
            else if (Kind == "exit")
            {
                line(new Vector2(.12f, .35f), new Vector2(-.32f, .35f), 2.5f);
                line(new Vector2(-.32f, .35f), new Vector2(-.32f, -.35f), 2.5f);
                line(new Vector2(-.32f, -.35f), new Vector2(.12f, -.35f), 2.5f);
                line(new Vector2(-.04f, 0), new Vector2(.38f, 0), 3);
                line(new Vector2(.22f, .14f), new Vector2(.38f, 0), 3);
                line(new Vector2(.38f, 0), new Vector2(.22f, -.14f), 3);
            }
            else if (Kind == "arrow")
            {
                line(new Vector2(-.38f, 0), new Vector2(.38f, 0), 1.8f);
                line(new Vector2(.1f, .27f), new Vector2(.38f, 0), 1.8f);
                line(new Vector2(.38f, 0), new Vector2(.1f, -.27f), 1.8f);
            }
            else if (Kind == "swords")
            {
                line(new Vector2(-.35f, .36f), new Vector2(.27f, -.29f), 2.4f);
                line(new Vector2(.35f, .36f), new Vector2(-.27f, -.29f), 2.4f);
                line(new Vector2(.07f, -.31f), new Vector2(.3f, -.08f), 2.4f);
                line(new Vector2(-.07f, -.31f), new Vector2(-.3f, -.08f), 2.4f);
                line(new Vector2(.27f, -.29f), new Vector2(.39f, -.4f), 3.3f);
                line(new Vector2(-.27f, -.29f), new Vector2(-.39f, -.4f), 3.3f);
            }
            else if (Kind == "search")
            {
                for (int i = 0; i < 24; i++)
                {
                    float a = i * (float)Math.PI / 12, b = (i + 1) * (float)Math.PI / 12;
                    line(new Vector2(-.09f + Mathf.Cos(a) * .27f, .09f + Mathf.Sin(a) * .27f),
                        new Vector2(-.09f + Mathf.Cos(b) * .27f, .09f + Mathf.Sin(b) * .27f), 1.6f);
                }
                line(new Vector2(.1f, -.1f), new Vector2(.4f, -.4f), 2);
            }
            else if (Kind == "bookmark")
                FightFrame.Polygon(vh, new[] { center + new Vector2(-.27f, .4f) * scale, center + new Vector2(.27f, .4f) * scale,
                    center + new Vector2(.27f, -.4f) * scale, center + new Vector2(0, -.23f) * scale, center + new Vector2(-.27f, -.4f) * scale }, 1.6f, color);
            else if (Kind == "book")
            {
                FightFrame.Polygon(vh, new[] { center + new Vector2(-.42f, .32f) * scale, center + new Vector2(-.18f, .32f) * scale,
                    center + new Vector2(0, .22f) * scale, center + new Vector2(.18f, .32f) * scale, center + new Vector2(.42f, .32f) * scale,
                    center + new Vector2(.42f, -.3f) * scale, center + new Vector2(.18f, -.3f) * scale, center + new Vector2(0, -.4f) * scale,
                    center + new Vector2(-.18f, -.3f) * scale, center + new Vector2(-.42f, -.3f) * scale }, 1.5f, color);
                line(new Vector2(0, .22f), new Vector2(0, -.4f), 1.3f);
            }
            else if (Kind == "play")
                FightFrame.Polygon(vh, new[] { center + new Vector2(-.25f, .36f) * scale, center + new Vector2(.38f, 0) * scale,
                    center + new Vector2(-.25f, -.36f) * scale }, 1.7f, color);
            else if (Kind == "share")
            {
                line(new Vector2(-.3f, -.3f), new Vector2(-.3f, .1f), 1.6f);
                line(new Vector2(-.3f, -.3f), new Vector2(.3f, -.3f), 1.6f);
                line(new Vector2(.3f, -.3f), new Vector2(.3f, .05f), 1.6f);
                line(new Vector2(0, -.05f), new Vector2(0, .4f), 1.8f);
                line(new Vector2(-.2f, .2f), new Vector2(0, .4f), 1.8f);
                line(new Vector2(0, .4f), new Vector2(.2f, .2f), 1.8f);
            }
            else if (Kind == "refresh" || Kind == "shuffle")
            {
                line(new Vector2(-.4f, .24f), new Vector2(-.17f, .24f), 1.5f);
                line(new Vector2(-.17f, .24f), new Vector2(.16f, Kind == "shuffle" ? -.24f : .24f), 1.5f);
                line(new Vector2(.16f, -.24f), new Vector2(.4f, -.24f), 1.5f);
                line(new Vector2(-.4f, -.24f), new Vector2(-.17f, -.24f), 1.5f);
                line(new Vector2(-.17f, -.24f), new Vector2(.16f, Kind == "shuffle" ? .24f : -.24f), 1.5f);
                line(new Vector2(.16f, .24f), new Vector2(.4f, .24f), 1.5f);
                line(new Vector2(.27f, .37f), new Vector2(.4f, .24f), 1.5f);
                line(new Vector2(.4f, .24f), new Vector2(.27f, .11f), 1.5f);
                line(new Vector2(.27f, -.11f), new Vector2(.4f, -.24f), 1.5f);
                line(new Vector2(.4f, -.24f), new Vector2(.27f, -.37f), 1.5f);
            }
            else if (Kind == "replay")
            {
                FightFrame.Polygon(vh, new[] { center + new Vector2(-.43f, .32f) * scale, center + new Vector2(.43f, .32f) * scale,
                    center + new Vector2(.43f, -.32f) * scale, center + new Vector2(-.43f, -.32f) * scale }, 1.7f, color);
                FightFrame.Polygon(vh, new[] { center + new Vector2(-.08f, .16f) * scale, center + new Vector2(.2f, 0) * scale,
                    center + new Vector2(-.08f, -.16f) * scale }, 1.5f, color);
                line(new Vector2(-.27f, -.4f), new Vector2(.27f, -.4f), 1.7f);
            }
            else if (Kind == "duel")
            {
                FightFrame.Polygon(vh, new[] { center + new Vector2(-.4f, .35f) * scale, center + new Vector2(.4f, .35f) * scale,
                    center + new Vector2(0, -.42f) * scale }, 1.5f, color);
                line(new Vector2(-.32f, .3f), new Vector2(.2f, -.25f), 1.5f);
                line(new Vector2(.32f, .3f), new Vector2(-.2f, -.25f), 1.5f);
                line(new Vector2(0, .3f), new Vector2(0, -.34f), 1.5f);
                line(new Vector2(-.19f, .4f), new Vector2(.08f, -.08f), 2);
                line(new Vector2(.19f, .4f), new Vector2(-.08f, -.08f), 2);
            }
            else
            {
                FightFrame.Polygon(vh, new[] { center + Vector2.up * scale * .42f, center + Vector2.right * scale * .42f,
                    center + Vector2.down * scale * .42f, center + Vector2.left * scale * .42f }, 1.4f, color);
                FightFrame.Polygon(vh, new[] { center + Vector2.up * scale * .18f, center + Vector2.right * scale * .18f,
                    center + Vector2.down * scale * .18f, center + Vector2.left * scale * .18f }, 1, color);
            }
        }
    }
}
