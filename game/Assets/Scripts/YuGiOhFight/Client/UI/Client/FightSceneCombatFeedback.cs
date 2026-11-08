using System;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace YuGiOhFight.Client.UI.Client
{
    // The native attack/result objects keep their directors, audio and events.
    // This non-interactive translucent cue replaces only their oversized visual layer.
    internal sealed class FightSceneCombatFeedback : MonoBehaviour
    {
        private enum FeedbackKind { DirectAttack, FinalBlow, Result }
        private Duel.DuelBGManager owner;
        private GameObject native;
        private PlayableDirector director;
        private Renderer[] nativeRenderers;
        private Canvas[] nativeCanvases;
        private Graphic[] nativeGraphics;
        private RectTransform viewport, plate;
        private CanvasGroup group;
        private ScenePalette palette;
        private string scene;
        private FeedbackKind kind;
        private float exitClock;
        private bool closed, leaving;

        internal static FightSceneCombatFeedback DirectAttack(Duel.DuelBGManager background, GameObject nativeBanner, int player)
            => Create(background, nativeBanner, FeedbackKind.DirectAttack, player, "直接攻击", 0);

        internal static FightSceneCombatFeedback FinalBlow(Duel.DuelBGManager background, GameObject nativeBanner, int player)
            => Create(background, nativeBanner, FeedbackKind.FinalBlow, player, "终极一击", 0);

        internal static FightSceneCombatFeedback Result(Duel.DuelBGManager background, GameObject nativeBanner, string asset)
        {
            int result = asset == "DuelTextWin" ? 1 : asset == "DuelTextLose" ? -1 : 0;
            return Create(background, nativeBanner, FeedbackKind.Result, 0,
                result > 0 ? "决斗胜利" : result < 0 ? "决斗败北" : "决斗平局", result);
        }

        private static FightSceneCombatFeedback Create(Duel.DuelBGManager background, GameObject nativeBanner,
            FeedbackKind feedback, int player, string label, int result)
        {
            var program = Runtime.Program.instance;
            if (!FightDuelPopupSkin.SceneThemeActive || background == null || nativeBanner == null
                || program == null || program.ocgcore == null || program.ocgcore.DuelBGManager != background) return null;
            var clock = nativeBanner.GetComponent<PlayableDirector>();
            var colors = ScenePalette.DialogFor(background.ActiveSceneId);
            if (clock == null || colors == null) return null;
            GameObject host = null;
            FightSceneCombatFeedback presenter = null;
            try
            {
                host = new GameObject("Fight scene combat feedback", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                host.transform.SetParent(program.transform, false);
                var canvas = host.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 131;
                host.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                presenter = host.AddComponent<FightSceneCombatFeedback>();
                presenter.owner = background; presenter.native = nativeBanner; presenter.director = clock;
                presenter.palette = colors; presenter.scene = background.ActiveSceneId; presenter.kind = feedback;
                presenter.nativeRenderers = nativeBanner.GetComponentsInChildren<Renderer>(true);
                presenter.nativeCanvases = nativeBanner.GetComponentsInChildren<Canvas>(true);
                presenter.nativeGraphics = nativeBanner.GetComponentsInChildren<Graphic>(true);
                presenter.Build(player, label, result);
                presenter.HideNative();
                return presenter;
            }
            catch (Exception error)
            {
                // Build first: a missing font or material must leave the
                // original visuals and their rule clock available as fallback.
                if (presenter != null) presenter.closed = true;
                if (host != null) { host.SetActive(false); Destroy(host); }
                Debug.LogWarning("[FightClient] scene combat feedback unavailable: " + error.GetType().Name);
                return null;
            }
        }

        private void Build(int player, string label, int result)
        {
            group = gameObject.AddComponent<CanvasGroup>(); group.alpha = 0;
            group.blocksRaycasts = false; group.interactable = false;
            viewport = FightUI.Node(transform, "Combat feedback viewport", 0, 0, 1280, 720);
            viewport.anchorMin = viewport.anchorMax = viewport.pivot = new Vector2(.5f, .5f);
            viewport.anchoredPosition = Vector2.zero;
            bool ending = kind == FeedbackKind.Result;
            float width = ending ? 400 : kind == FeedbackKind.FinalBlow ? 320 : 360;
            float height = ending ? 142 : kind == FeedbackKind.FinalBlow ? 54 : 66;
            plate = FightUI.Node(viewport, "Combat feedback plate", 0, 0, width, height);
            plate.anchorMin = plate.anchorMax = plate.pivot = new Vector2(.5f, .5f);
            // Match phase and turn cues with a quiet translucent surface,
            // keeping the field visible without an enclosing frame.
            var surface = plate.gameObject.AddComponent<Image>();
            surface.sprite = FightSkin.ButtonSurface; surface.type = Image.Type.Sliced;
            surface.pixelsPerUnitMultiplier = 1; surface.raycastTarget = false;
            var tint = palette.Raised; tint.a = .42f; surface.color = tint;

            if (ending)
            {
                var emblem = FightUI.Node(plate, "Combat result emblem", 20, 34, 76, 76);
                var ornament = emblem.gameObject.AddComponent<FightSceneCombatOrnament>();
                ornament.Palette = palette; ornament.Emblem = true; ornament.Result = result;
                ornament.color = result < 0 ? FightUI.Danger : palette.Accent; ornament.raycastTarget = false;
                var caption = Caption(FightText.Get(label), 108, 46, 270, 52, 34, result < 0 ? Color.Lerp(palette.Heading, FightUI.Danger, .35f) : palette.Heading);
                caption.fontStyle = FontStyles.Bold; caption.fontSizeMin = 24;
            }
            else
            {
                var badge = FightUI.Node(plate, "Combat feedback owner", 20, (height - 26) * .5f, 46, 26);
                var mark = badge.gameObject.AddComponent<FightSceneAnnouncementMark>();
                mark.Palette = palette; mark.color = player == 0 ? palette.Accent : FightUI.Danger; mark.raycastTarget = false;
                var ownerText = Caption(FightText.Get(player == 0 ? "我方" : "对方"), 20, (height - 26) * .5f, 46, 26, 13, palette.Heading);
                ownerText.alignment = TextAlignmentOptions.Center;
                var caption = Caption(FightText.Get(label), 78, (height - 34) * .5f, width - 100, 34, kind == FeedbackKind.FinalBlow ? 22 : 24, palette.Heading);
                caption.fontStyle = FontStyles.Bold;
                var rail = FightUI.Node(plate, "Combat feedback light rail", 18, height - 12, width - 36, 5);
                var ornament = rail.gameObject.AddComponent<FightSceneCombatOrnament>();
                ornament.Palette = palette; ornament.color = player == 0 ? palette.Accent : FightUI.Danger; ornament.raycastTarget = false;
            }
            Fit();
        }

        private TextMeshProUGUI Caption(string value, float x, float y, float width, float height, float size, Color color)
        {
            var text = FightUI.RawText(plate, value, x, y, width, height, size, color, TextAlignmentOptions.Left);
            text.enableWordWrapping = false; text.overflowMode = TextOverflowModes.Overflow; text.raycastTarget = false;
            text.enableAutoSizing = true; text.fontSizeMin = size * .75f; text.fontSizeMax = size;
            text.characterSpacing = .6f;
            return text;
        }

        private bool Current()
        {
            var program = Runtime.Program.instance;
            return !closed && native != null && director != null && program != null && program.ocgcore != null && program.ocgcore.showing
                && program.currentServant == program.ocgcore && program.ocgcore.DuelBGManager == owner
                && owner.loaded && owner.Scene3DActive && owner.ActiveSceneId == scene;
        }

        private void LateUpdate()
        {
            if (closed) return;
            if (!Current()) { Cancel(); return; }
            HideNative(); Fit();
            float elapsed = Mathf.Max(0, (float)director.time);
            float enter = Smooth(elapsed / (kind == FeedbackKind.Result ? .3f : .16f));
            float exit = 1;
            if (kind == FeedbackKind.FinalBlow)
            {
                // StopLoop remains owned by DuelFinalBlow. Its director
                // proceeds out of the original .25-.30 loop after release.
                if (leaving) exit = 1 - Smooth(Mathf.Max(0, elapsed - exitClock) / .35f);
            }
            else
            {
                double duration = director.duration;
                if (duration > 0 && !double.IsInfinity(duration) && !double.IsNaN(duration))
                    exit = Smooth(Mathf.Max(0, (float)(duration - director.time)) / (kind == FeedbackKind.Result ? .3f : .14f));
            }
            group.alpha = Mathf.Min(enter, exit);
            plate.anchoredPosition += new Vector2(0, -6 * (1 - enter));
            plate.localScale = Vector3.one;
        }

        private void Fit()
        {
            if (viewport == null || plate == null) return;
            float scale = Mathf.Min(Mathf.Max(1, Screen.width) / 1280f, Mathf.Max(1, Screen.height) / 720f);
            viewport.localScale = new Vector3(scale, scale, 1);
            Rect safe = Screen.safeArea;
            Vector2 safeCentre = (safe.center - new Vector2(Screen.width * .5f, Screen.height * .5f)) / scale;
            float preferred = kind == FeedbackKind.Result ? 88 : kind == FeedbackKind.FinalBlow ? 196 : 112;
            float raise = Mathf.Min(preferred, Mathf.Max(0, safe.height / scale * .5f - plate.sizeDelta.y * .5f - 24));
            plate.anchoredPosition = safeCentre + new Vector2(0, raise);
        }

        private void HideNative()
        {
            foreach (var canvas in nativeCanvases) if (canvas != null) canvas.enabled = false;
            foreach (var renderer in nativeRenderers) if (renderer != null) renderer.enabled = false;
            foreach (var graphic in nativeGraphics) if (graphic != null) graphic.enabled = false;
        }

        internal void BeginExit()
        {
            if (closed || leaving) return;
            leaving = true; exitClock = director != null ? (float)director.time : 0;
        }

        private static float Smooth(float value) { value = Mathf.Clamp01(value); return value * value * (3 - 2 * value); }
        private void Cancel()
        {
            if (closed) return;
            // Only a finite result has an awaiting caller. Retiring it on duel
            // exit releases that original wait; attack loop semantics stay native.
            if (kind == FeedbackKind.Result && director != null) director.Stop();
            Finish();
        }
        internal void Finish()
        {
            if (closed) return;
            closed = true; gameObject.SetActive(false); Destroy(gameObject);
        }
        private void OnDisable() { if (!closed) Cancel(); }
    }

    // The same result mark and quiet rule keep cues recognisable across fields.
    // Only their tint follows the scene; native attack arrows remain untouched.
    internal sealed class FightSceneCombatOrnament : MaskableGraphic
    {
        internal ScenePalette Palette;
        internal bool Emblem;
        internal int Result;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (Palette == null) return;
            var r = rectTransform.rect;
            Color edge = color; edge.a *= .58f;
            Color soft = color; soft.a *= .16f;
            if (!Emblem)
            {
                Line(mesh, new Vector2(r.xMin + 8, r.center.y), new Vector2(r.xMax - 8, r.center.y), .65f, edge);
                return;
            }
            Vector2 c = r.center;
            float k = Mathf.Min(r.width, r.height) / 76f;
            Ring(mesh, c, 25 * k, .9f * k, soft);
            if (Result > 0)
                Path(mesh, c, new[] { new Vector2(-14, 0), new Vector2(-4, -10), new Vector2(16, 13) }, k, 1.8f, edge);
            else if (Result < 0)
            {
                Line(mesh, c + new Vector2(-12, -12) * k, c + new Vector2(12, 12) * k, 1.6f * k, edge);
                Line(mesh, c + new Vector2(-12, 12) * k, c + new Vector2(12, -12) * k, 1.6f * k, edge);
            }
            else
                foreach (int row in new[] { -1, 1 })
                    Line(mesh, c + new Vector2(-13, row * 5) * k, c + new Vector2(13, row * 5) * k, 1.6f * k, edge);
        }
        private static void Path(VertexHelper mesh, Vector2 center, Vector2[] points, float scale, float width, Color tint)
        { for (int i = 1; i < points.Length; i++) Line(mesh, center + points[i - 1] * scale, center + points[i] * scale, width * scale, tint); }
        private static void Ring(VertexHelper mesh, Vector2 center, float radius, float width, Color tint)
        {
            for (int i = 0; i < 32; i++)
            {
                float a = i * (float)Math.PI * 2 / 32, b = (i + 1) * (float)Math.PI * 2 / 32;
                Line(mesh, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                    center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, width, tint);
            }
        }
        private static void Line(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color tint)
        {
            var direction = b - a;
            var normal = new Vector2(-direction.y, direction.x).normalized * width * .5f;
            int start = mesh.currentVertCount;
            mesh.AddVert(a - normal, tint, Vector2.zero); mesh.AddVert(a + normal, tint, Vector2.zero);
            mesh.AddVert(b + normal, tint, Vector2.zero); mesh.AddVert(b - normal, tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
