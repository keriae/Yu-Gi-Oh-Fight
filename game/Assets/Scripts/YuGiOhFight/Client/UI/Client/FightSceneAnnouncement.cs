using System;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace YuGiOhFight.Client.UI.Client
{
    // The original director remains the rule clock and sound source. Only its
    // native visual banner yields to this small, translucent scene cue.
    internal sealed class FightSceneAnnouncement : MonoBehaviour
    {
        private Duel.DuelBGManager owner;
        private PlayableDirector director;
        private Renderer[] nativeRenderers;
        private Canvas[] nativeCanvases;
        private Graphic[] nativeGraphics;
        private RectTransform viewport, plate;
        private CanvasGroup group;
        private ScenePalette palette;
        private string scene;
        private bool closed;

        internal static FightSceneAnnouncement Phase(Duel.DuelBGManager background, GameObject nativeBanner,
            int player, MDPro3.Duel.YGOSharp.DuelPhase phase)
        {
            string label = phase switch
            {
                MDPro3.Duel.YGOSharp.DuelPhase.Draw => "抽卡阶段",
                MDPro3.Duel.YGOSharp.DuelPhase.Standby => "准备阶段",
                MDPro3.Duel.YGOSharp.DuelPhase.Main1 => "主要阶段 1",
                MDPro3.Duel.YGOSharp.DuelPhase.Main2 => "主要阶段 2",
                MDPro3.Duel.YGOSharp.DuelPhase.End => "结束阶段",
                _ => "战斗阶段",
            };
            return Create(background, nativeBanner, player, label, null);
        }

        internal static FightSceneAnnouncement Turn(Duel.DuelBGManager background, GameObject nativeBanner, int player, int turn)
            => Create(background, nativeBanner, player, player == 0 ? "我方回合" : "对方回合", turn.ToString("00"));

        private static FightSceneAnnouncement Create(Duel.DuelBGManager background, GameObject nativeBanner,
            int player, string label, string turn)
        {
            var program = Runtime.Program.instance;
            if (!FightDuelPopupSkin.SceneThemeActive || background == null || nativeBanner == null
                || program == null || program.ocgcore == null || program.ocgcore.DuelBGManager != background) return null;
            var clock = nativeBanner.GetComponent<PlayableDirector>();
            // The academy's bare ivory floor needs dark caption text.
            var colors = ScenePalette.DialogFor(background.ActiveSceneId);
            if (clock == null || colors == null) return null;
            GameObject host = null;
            FightSceneAnnouncement presenter = null;
            try
            {
                host = new GameObject("Fight scene phase announcement", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                host.transform.SetParent(program.transform, false);
                var canvas = host.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 130;
                host.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                presenter = host.AddComponent<FightSceneAnnouncement>();
                presenter.owner = background; presenter.director = clock; presenter.palette = colors; presenter.scene = background.ActiveSceneId;
                presenter.nativeRenderers = nativeBanner.GetComponentsInChildren<Renderer>(true);
                presenter.nativeCanvases = nativeBanner.GetComponentsInChildren<Canvas>(true);
                presenter.nativeGraphics = nativeBanner.GetComponentsInChildren<Graphic>(true);
                presenter.Build(player, label, turn);
                presenter.HideNative();
                return presenter;
            }
            catch (Exception error)
            {
                // A presentation failure falls back to the original banner;
                // it must not cancel or stop that banner's native clock.
                if (presenter != null) presenter.closed = true;
                if (host != null) { host.SetActive(false); Destroy(host); }
                Debug.LogWarning("[FightClient] scene announcement unavailable: " + error.GetType().Name);
                return null;
            }
        }

        private void Build(int player, string label, string turn)
        {
            group = gameObject.AddComponent<CanvasGroup>(); group.alpha = 0;
            group.blocksRaycasts = false; group.interactable = false;
            viewport = FightUI.Node(transform, "Announcement viewport", 0, 0, 1280, 720);
            viewport.anchorMin = viewport.anchorMax = viewport.pivot = new Vector2(.5f, .5f);
            viewport.anchoredPosition = Vector2.zero;
            plate = FightUI.Node(viewport, "Announcement plate", 0, 0, 360, 66);
            plate.anchorMin = plate.anchorMax = plate.pivot = new Vector2(.5f, .5f);
            plate.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            // A translucent reading surface lets the field show through while
            // keeping the caption legible; it does not need an enclosing frame.
            var surface = plate.gameObject.AddComponent<Image>();
            surface.sprite = FightSkin.ButtonSurface; surface.type = Image.Type.Sliced;
            surface.pixelsPerUnitMultiplier = 1; surface.raycastTarget = false;
            var tint = palette.Raised; tint.a = .42f; surface.color = tint;

            // A small side marker preserves player ownership without flooding
            // the screen with the old near/far blue and red full-width bars.
            var side = player == 0 ? palette.Accent : FightUI.Danger;
            var badge = FightUI.Node(plate, "Announcement owner", 22, 20, 46, 26);
            var mark = badge.gameObject.AddComponent<FightSceneAnnouncementMark>(); mark.Palette = palette; mark.color = side; mark.raycastTarget = false;
            var ownerText = FightUI.RawText(badge, turn ?? FightText.Get(player == 0 ? "我方" : "对方"), 0, 0, 46, 26,
                turn != null ? 18 : 13, palette.Heading, TextAlignmentOptions.Center);
            ownerText.enableWordWrapping = false; ownerText.overflowMode = TextOverflowModes.Overflow; ownerText.raycastTarget = false;
            var caption = FightUI.RawText(plate, FightText.Get(label), 80, 16, 258, 34, 24, palette.Heading, TextAlignmentOptions.Center);
            caption.fontStyle = FontStyles.Bold; caption.characterSpacing = .6f;
            caption.enableAutoSizing = true; caption.fontSizeMin = 18; caption.fontSizeMax = 24;
            caption.enableWordWrapping = false; caption.raycastTarget = false;
            Fit();
        }

        private bool Current()
        {
            var program = Runtime.Program.instance;
            return !closed && director != null && program != null && program.ocgcore != null && program.ocgcore.showing
                && program.currentServant == program.ocgcore && program.ocgcore.DuelBGManager == owner
                && owner.loaded && owner.Scene3DActive && owner.ActiveSceneId == scene;
        }

        private void LateUpdate()
        {
            if (closed) return;
            if (!Current()) { Cancel(); return; }
            HideNative(); Fit();
            double duration = director.duration;
            if (duration <= 0 || double.IsInfinity(duration) || double.IsNaN(duration)) return;
            float elapsed = Mathf.Max(0, (float)director.time);
            float remaining = Mathf.Max(0, (float)(duration - director.time));
            float enter = Smooth(elapsed / .18f), exit = Smooth(remaining / .18f);
            group.alpha = Mathf.Min(enter, exit);
            // The director still owns the cue duration and audio. A short
            // shared fade gives every phase the same restrained presentation.
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
            float raise = Mathf.Min(112, Mathf.Max(0, safe.height / scale * .5f - 68));
            plate.anchoredPosition = safeCentre + new Vector2(0, raise);
        }

        private void HideNative()
        {
            foreach (var canvas in nativeCanvases) if (canvas != null) canvas.enabled = false;
            foreach (var renderer in nativeRenderers) if (renderer != null) renderer.enabled = false;
            foreach (var graphic in nativeGraphics) if (graphic != null) graphic.enabled = false;
        }
        private static float Smooth(float value) { value = Mathf.Clamp01(value); return value * value * (3 - 2 * value); }
        private void Cancel()
        {
            if (closed) return;
            closed = true;
            // Stopping only this retired banner releases its original WaitAsync
            // when the duel has exited; no new delay or response is introduced.
            if (director != null) director.Stop();
            gameObject.SetActive(false); Destroy(gameObject);
        }
        internal void Finish()
        {
            if (closed) return;
            closed = true; gameObject.SetActive(false); Destroy(gameObject);
        }
        private void OnDisable() { if (!closed) Cancel(); }
    }

    internal sealed class FightSceneAnnouncementMark : MaskableGraphic
    {
        internal ScenePalette Palette;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (Palette == null) return;
            var r = rectTransform.rect;
            var tint = color; tint.a *= .7f;
            foreach (int side in new[] { -1, 1 })
            {
                float x = side < 0 ? r.xMin : r.xMax;
                Line(mesh, new Vector2(x, r.yMin + 3), new Vector2(x, r.yMax - 3), .8f, tint);
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
