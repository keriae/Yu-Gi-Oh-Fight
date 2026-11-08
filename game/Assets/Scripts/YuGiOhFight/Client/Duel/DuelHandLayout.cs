using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YuGiOhFight.Client.Duel
{
    // Horizontal presentation only. Budget the visible, default card polygons,
    // never the hand's animated hover pose or corners below the screen.
    internal static class DuelHandLayout
    {
        private const float Padding = 4f;
        private static readonly List<HudRect> hud = new();
        private static readonly List<HandCard> hand = new();
        private static readonly Vector3[] corners = new Vector3[4];
        private static readonly Vector3[] clipped = new Vector3[12];
        private static readonly HandCard access = new();
        private static UI.ServantUI.OcgCoreUI owner;
        private static Camera view;
        private static Matrix4x4 lastView, lastProjection;
        private static Rect lastPixels, lastSafe;
        private static int frame = -1, lastCount = -1, lastModels, version;
        private static bool initialized;
        private static float scale = 1f, shift;

        private sealed class HudRect
        {
            internal RectTransform Rect;
            internal Canvas Canvas;
            internal CanvasGroup[] Groups;
            internal Rect Bounds;
            internal bool Visible;
        }

        private sealed class HandCard
        {
            internal float X;
            internal readonly Vector3[] Polygon = new Vector3[12];
            internal int Count;
        }

        internal static Vector3 Position(GPS p, Vector3 original)
        {
            if (!p.InMyControl()) return original;
            Refresh();
            original.x = original.x * scale + shift;
            return original;
        }

        internal static int Version { get { Refresh(); return version; } }

        internal static float FanX(GameCard card, float original)
            => card.p.InMyControl() && card.p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand)
                && (scale != 1f || shift != 0f) ? GameCard.OriginalHandPosition(card.p).x : original;

        private static void SetLayout(float nextScale, float nextShift)
        {
            if (Mathf.Abs(scale - nextScale) < .00001f && Mathf.Abs(shift - nextShift) < .00001f) return;
            scale = nextScale; shift = nextShift; version++;
        }

        private static void Refresh()
        {
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            var program = Runtime.Program.instance;
            var core = program == null ? null : program.ocgcore;
            var camera = program == null || program.camera_ == null ? null : program.camera_.cameraMain;
            var ui = core == null ? null : core.GetUI<UI.ServantUI.OcgCoreUI>();
            if (core == null || !core.showing || ui == null || camera == null || !camera.isActiveAndEnabled)
            {
                initialized = false; SetLayout(1f, 0f); return;
            }
            bool changed = !initialized || view != camera || owner != ui
                || !camera.worldToCameraMatrix.Equals(lastView) || !camera.projectionMatrix.Equals(lastProjection)
                || camera.pixelRect != lastPixels || Screen.safeArea != lastSafe;
            if (owner != ui) BindHud(ui);
            foreach (var item in hud)
            {
                bool visible = IsVisible(item);
                Rect bounds = visible ? ScreenBounds(item) : default;
                changed |= item.Visible != visible || item.Bounds != bounds;
                item.Visible = visible; item.Bounds = bounds;
            }
            int count = core.GetMyHandCount(), models = 17;
            foreach (var card in Controllers.OcgCore.cards)
                if (card.p.InMyControl() && card.p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand))
                    models = unchecked(models * 31 + card.p.sequence.GetHashCode()
                        + (card.model == null ? 0 : card.model.GetInstanceID()));
            changed |= count != lastCount || models != lastModels;
            view = camera; lastView = camera.worldToCameraMatrix; lastProjection = camera.projectionMatrix;
            lastPixels = camera.pixelRect; lastSafe = Screen.safeArea; lastCount = count; lastModels = models; initialized = true;
            if (!changed) return;
            if (count < 1 || count > 10) { hand.Clear(); SetLayout(1f, 0f); return; }
            Rebuild(count);
        }

        private static void BindHud(UI.ServantUI.OcgCoreUI ui)
        {
            owner = ui; hud.Clear();
            // LP0 is the authored common parent of the value and avatar. The
            // avatar extends outside it, so include its visible descendants.
            AddRect(ui.TextPlayer0LP.transform.parent as RectTransform);
            foreach (var graphic in ui.TextPlayer0LP.transform.parent.GetComponentsInChildren<Graphic>(true))
                if (graphic.raycastTarget) AddRect(graphic.rectTransform);
            foreach (var graphic in ui.Buttons.GetComponentsInChildren<Graphic>(true))
                if (graphic.raycastTarget) AddRect(graphic.rectTransform);
        }

        private static void AddRect(RectTransform rect)
        {
            if (rect == null) return;
            foreach (var item in hud) if (item.Rect == rect) return;
            hud.Add(new HudRect { Rect = rect, Canvas = rect.GetComponentInParent<Canvas>(),
                Groups = rect.GetComponentsInParent<CanvasGroup>(true) });
        }

        private static bool IsVisible(HudRect item)
        {
            if (item.Rect == null || !item.Rect.gameObject.activeInHierarchy
                || item.Canvas == null || !item.Canvas.isActiveAndEnabled) return false;
            foreach (var group in item.Groups)
            {
                if (group == null || !group.isActiveAndEnabled) continue;
                if (group.alpha <= .001f) return false;
                if (group.ignoreParentGroups) break;
            }
            return true;
        }

        private static Rect ScreenBounds(HudRect item)
        {
            item.Rect.GetWorldCorners(corners);
            Camera camera = item.Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : item.Canvas.worldCamera;
            Vector2 first = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Rect result = new Rect(first, Vector2.zero);
            for (int i = 1; i < 4; i++)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
                result = Rect.MinMaxRect(Mathf.Min(result.xMin, point.x), Mathf.Min(result.yMin, point.y),
                    Mathf.Max(result.xMax, point.x), Mathf.Max(result.yMax, point.y));
            }
            return result;
        }

        private static void Rebuild(int count)
        {
            hand.Clear();
            Rect safe = Screen.safeArea;
            safe = Rect.MinMaxRect(Mathf.Max(safe.xMin, lastPixels.xMin), Mathf.Max(safe.yMin, lastPixels.yMin),
                Mathf.Min(safe.xMax, lastPixels.xMax), Mathf.Min(safe.yMax, lastPixels.yMax));
            if (safe.width <= 0 || safe.height <= 0) { SetLayout(1f, 0f); return; }
            Rect visibleBounds = default;
            bool first = true;
            foreach (var card in Controllers.OcgCore.cards)
            {
                if (!card.p.InMyControl() || !card.p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand)) continue;
                var original = GameCard.OriginalHandPosition(card.p);
                if (!card.DefaultHandCorners(original, corners)) { SetLayout(1f, 0f); return; }
                var item = new HandCard { X = original.x, Count = 4 };
                for (int i = 0; i < 4; i++) item.Polygon[i] = corners[i];
                ClipY(item, safe.yMin, true); ClipY(item, safe.yMax, false);
                if (item.Count == 0) { SetLayout(1f, 0f); return; }
                for (int i = 0; i < item.Count; i++)
                {
                    Vector3 point = view.WorldToScreenPoint(item.Polygon[i]);
                    if (point.z <= 0) { SetLayout(1f, 0f); return; }
                    if (first) { visibleBounds = new Rect(point.x, point.y, 0, 0); first = false; }
                    else visibleBounds = Rect.MinMaxRect(Mathf.Min(visibleBounds.xMin, point.x), Mathf.Min(visibleBounds.yMin, point.y),
                        Mathf.Max(visibleBounds.xMax, point.x), Mathf.Max(visibleBounds.yMax, point.y));
                }
                hand.Add(item);
            }
            if (hand.Count != count) { SetLayout(1f, 0f); return; }
            float left = safe.xMin, right = safe.xMax;
            bool covered = visibleBounds.xMin < left || visibleBounds.xMax > right;
            foreach (var item in hud)
            {
                if (!item.Visible || item.Bounds.yMax <= visibleBounds.yMin || item.Bounds.yMin >= visibleBounds.yMax) continue;
                if (!covered && PolygonOverlap(item.Bounds)) covered = true;
                if (item.Bounds.center.x < safe.center.x) left = Mathf.Max(left, item.Bounds.xMax + Padding);
                else right = Mathf.Min(right, item.Bounds.xMin - Padding);
            }
            // Partial overlap is already harmless when each original card has
            // a usable visible strip. This keeps the authored desktop fan.
            if (!covered || right <= left || HasAccess(left, right)) { SetLayout(1f, 0f); return; }
            if (ShiftInterval(1f, left, right, out float low, out float high))
            { SetLayout(1f, Mathf.Clamp(0f, low, high)); return; }
            if (!ShiftInterval(0f, left, right, out _, out _)) { SetLayout(1f, 0f); return; }
            float min = 0f, max = 1f;
            for (int i = 0; i < 24; i++)
            {
                float middle = (min + max) * .5f;
                if (ShiftInterval(middle, left, right, out _, out _)) min = middle;
                else max = middle;
            }
            ShiftInterval(min, left, right, out low, out high);
            SetLayout(min, Mathf.Clamp(0f, low, high));
        }

        private static void ClipY(HandCard item, float boundary, bool above)
            => ClipAxis(item, boundary, above, false);

        private static void ClipAxis(HandCard item, float boundary, bool lower, bool horizontal)
        {
            if (item.Count == 0) return;
            float normal = horizontal ? (boundary - lastPixels.xMin) / lastPixels.width * 2f - 1f
                : (boundary - lastPixels.yMin) / lastPixels.height * 2f - 1f;
            Matrix4x4 projection = lastProjection * lastView;
            int length = 0;
            Vector3 previous = item.Polygon[item.Count - 1];
            Vector4 previousClip = projection * new Vector4(previous.x, previous.y, previous.z, 1);
            float previousDistance = ((horizontal ? previousClip.x : previousClip.y) - normal * previousClip.w) * (lower ? 1 : -1);
            for (int i = 0; i < item.Count; i++)
            {
                Vector3 point = item.Polygon[i];
                Vector4 clip = projection * new Vector4(point.x, point.y, point.z, 1);
                float distance = ((horizontal ? clip.x : clip.y) - normal * clip.w) * (lower ? 1 : -1);
                if ((distance >= 0) != (previousDistance >= 0))
                    clipped[length++] = Vector3.LerpUnclamped(previous, point, previousDistance / (previousDistance - distance));
                if (distance >= 0) clipped[length++] = point;
                previous = point; previousDistance = distance;
            }
            item.Count = length;
            for (int i = 0; i < length; i++) item.Polygon[i] = clipped[i];
        }

        private static bool HasAccess(float left, float right)
        {
            foreach (var item in hand)
            {
                access.Count = item.Count;
                for (int i = 0; i < item.Count; i++) access.Polygon[i] = item.Polygon[i];
                ClipAxis(access, left, true, true); ClipAxis(access, right, false, true);
                float min = float.PositiveInfinity, max = float.NegativeInfinity;
                for (int i = 0; i < access.Count; i++)
                {
                    float x = view.WorldToScreenPoint(access.Polygon[i]).x;
                    min = Mathf.Min(min, x); max = Mathf.Max(max, x);
                }
                if (max - min < 18f) return false;
            }
            return true;
        }

        private static bool PolygonOverlap(Rect rect)
        {
            foreach (var item in hand)
            {
                // Convex polygon/rect separating axes, including the rectangle's axes.
                bool separate = false;
                for (int axisIndex = -2; axisIndex < item.Count && !separate; axisIndex++)
                {
                    Vector2 axis;
                    if (axisIndex == -2) axis = Vector2.right;
                    else if (axisIndex == -1) axis = Vector2.up;
                    else
                    {
                        Vector2 a = view.WorldToScreenPoint(item.Polygon[axisIndex]);
                        Vector2 b = view.WorldToScreenPoint(item.Polygon[(axisIndex + 1) % item.Count]);
                        axis = new Vector2(a.y - b.y, b.x - a.x);
                    }
                    if (Vector2.Dot(axis, axis) < .000001f) continue;
                    float min = float.PositiveInfinity, max = float.NegativeInfinity;
                    for (int i = 0; i < item.Count; i++)
                    {
                        float value = Vector2.Dot(axis, view.WorldToScreenPoint(item.Polygon[i]));
                        min = Mathf.Min(min, value); max = Mathf.Max(max, value);
                    }
                    float center = Vector2.Dot(axis, rect.center);
                    float radius = Mathf.Abs(axis.x) * rect.width * .5f + Mathf.Abs(axis.y) * rect.height * .5f;
                    separate = max <= center - radius || min >= center + radius;
                }
                if (!separate) return true;
            }
            return false;
        }

        private static bool ShiftInterval(float factor, float left, float right, out float low, out float high)
        {
            low = float.NegativeInfinity; high = float.PositiveInfinity;
            Matrix4x4 projection = lastProjection * lastView;
            Vector4 direction = projection * new Vector4(1, 0, 0, 0);
            float leftNormal = (left - lastPixels.xMin) / lastPixels.width * 2f - 1f;
            float rightNormal = (right - lastPixels.xMin) / lastPixels.width * 2f - 1f;
            foreach (var item in hand)
                for (int i = 0; i < item.Count; i++)
                {
                    var point = item.Polygon[i];
                    Vector4 clip = projection * new Vector4(point.x, point.y, point.z, 1);
                    float leftSlope = direction.x - leftNormal * direction.w;
                    float rightSlope = direction.x - rightNormal * direction.w;
                    if (Mathf.Abs(leftSlope) < .000001f || Mathf.Abs(rightSlope) < .000001f) return false;
                    float a = (leftNormal * clip.w - clip.x) / leftSlope;
                    float b = (rightNormal * clip.w - clip.x) / rightSlope;
                    if (a > b) { float swap = a; a = b; b = swap; }
                    float delta = (factor - 1f) * item.X;
                    low = Mathf.Max(low, a - delta); high = Mathf.Min(high, b - delta);
                }
            return low <= high;
        }
    }
}
