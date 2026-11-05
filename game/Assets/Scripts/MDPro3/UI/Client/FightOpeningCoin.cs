using System;
using System.IO;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI.Client
{
    // Presents the server's actual opening order. It never chooses or sends a
    // protocol response, and lives under the existing movable native popup root.
    public sealed class FightOpeningCoin : MonoBehaviour
    {
        private const float Duration = 2f;
        private const float TossDuration = 1.3f;
        private static FightOpeningCoin current;
        private object owner;
        private bool completed, cancelled;
        private CanvasGroup group;
        private FightOpeningCoinGraphic coin;
        private TMP_Text result;
        private float started;

        public static bool IsShowing => current != null && current.gameObject.activeInHierarchy;
        public static RectTransform CurrentRoot => current == null ? null : current.transform as RectTransform;
        public static bool LastIsFirst { get; private set; }
        public static int StartCount { get; private set; }
        public static int CompletionCount { get; private set; }
        public static int CancelCount { get; private set; }
        public static float Phase => current == null ? 1 : Mathf.Clamp01((Time.unscaledTime - current.started) / Duration);

        public static async UniTask PlayAsync(object owner, bool isFirst, string firstPlayerName,
            bool observer, Func<bool> stillCurrent)
        {
            if (Program.instance == null || Program.instance.ui_ == null || Program.instance.ui_.popup == null
                || stillCurrent != null && !stillCurrent()) return;
            if (current != null) current.CancelInstance();

            var root = FightUI.Node(Program.instance.ui_.popup, "Fight opening coin", 0, 0, 0, 0);
            FightUI.Stretch(root); root.SetAsLastSibling();
            var surface = root.gameObject.AddComponent<Image>();
            surface.color = new Color(.015f, .028f, .043f, .24f); surface.raycastTarget = true;
            var presenter = root.gameObject.AddComponent<FightOpeningCoin>();
            presenter.owner = owner; presenter.started = Time.unscaledTime;
            presenter.group = root.gameObject.AddComponent<CanvasGroup>();
            presenter.group.blocksRaycasts = true; presenter.group.interactable = false;
            var content = FightUI.Node(root, "Opening content", 0, 0, 600, 440);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(.5f, .5f);
            content.anchoredPosition = Vector2.zero;
            var coinRoot = FightUI.Node(content, "Coin", 140, 24, 320, 320);
            presenter.coin = coinRoot.gameObject.AddComponent<FightOpeningCoinGraphic>();
            presenter.coin.LoadArtwork();
            presenter.coin.IsFirst = isFirst; presenter.coin.raycastTarget = false;
            var label = isFirst ? "先手" : "后手";
            presenter.result = FightUI.Text(content, label, 24, 344, 552, 64, 36,
                isFirst ? FightUI.Gold : FightUI.CyanText, TextAlignmentOptions.Center);
            presenter.result.name = "Opening result"; presenter.result.raycastTarget = false;
            presenter.result.fontStyle = FontStyles.Bold; presenter.result.outlineWidth = .14f;
            presenter.result.color = new Color(presenter.result.color.r, presenter.result.color.g, presenter.result.color.b, 0);
            current = presenter; LastIsFirst = isFirst; StartCount++;
            try
            {
                while (presenter != null && !presenter.cancelled && Time.unscaledTime - presenter.started < Duration)
                {
                    if (stillCurrent != null && !stillCurrent()) { presenter.CancelInstance(); break; }
                    presenter.Paint();
                    await UniTask.Yield();
                }
                if (presenter != null && !presenter.cancelled)
                {
                    presenter.completed = true; CompletionCount++;
                }
            }
            finally
            {
                if (presenter != null)
                {
                    if (current == presenter) current = null;
                    presenter.gameObject.SetActive(false);
                    Destroy(presenter.gameObject);
                }
            }
        }

        public static void Cancel(object owner)
        {
            if (current != null && ReferenceEquals(current.owner, owner)) current.CancelInstance();
        }
        private void CancelInstance()
        {
            if (completed || cancelled) return;
            cancelled = true; CancelCount++;
            if (current == this) current = null;
            gameObject.SetActive(false);
        }
        private void Paint()
        {
            float elapsed = Time.unscaledTime - started;
            coin.Progress = Mathf.Clamp01(elapsed / TossDuration);
            group.alpha = Mathf.Min(Mathf.Clamp01(elapsed / .12f), Mathf.Clamp01((Duration - elapsed) / .12f));
            var tint = result.color; tint.a = Mathf.Clamp01((elapsed - TossDuration) / .1f); result.color = tint;
        }
        private void OnDestroy()
        {
            if (!completed && !cancelled) { cancelled = true; CancelCount++; }
            if (current == this) current = null;
        }
    }

    internal sealed class FightOpeningCoinGraphic : MaskableGraphic
    {
        internal bool IsFirst;
        private float progress;
        internal float Progress
        {
            get => progress;
            set { if (Mathf.Abs(progress - value) < .0001f) return; progress = value; SetVerticesDirty(); }
        }
        private Texture2D artwork;
        internal bool ArtworkLoaded => artwork != null && artwork.width >= 1024 && artwork.width == artwork.height * 2;
        internal bool VisibleFaceIsSun { get; private set; }
        public override Texture mainTexture => artwork != null ? artwork : base.mainTexture;
        internal void LoadArtwork()
        {
            string path = Program.PATH_DATA + "ClientUI/opening-coin-atlas.png";
            if (File.Exists(path)) artwork = FightAsset.Acquire(path);
            SetMaterialDirty(); SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var origin = rectTransform.rect.center;
            float ease = 1 - (1 - progress) * (1 - progress) * (1 - progress);
            float angle = ease * 6 * (float)Math.PI;
            float face = Mathf.Cos(angle), squeeze = Mathf.Max(.025f, Mathf.Abs(face));
            float rise = Mathf.Sin(progress * (float)Math.PI) * 78;
            var center = origin + Vector2.up * rise;
            bool front = face >= 0 ? IsFirst : !IsFirst;
            VisibleFaceIsSun = front;
            // Soft contact shadow narrows as the coin rises. Center UV samples
            // the opaque metal, so it also works with the transparent artwork.
            for (int layer = 0; layer < 5; layer++)
                Shadow(vh, origin + Vector2.down * 144, 103 - rise * .35f + layer * 4, 9 + layer * 2,
                    new Color(0, 0, 0, .04f));
            var rim = new Color(.47f, .33f, .16f, 1);
            Face(vh, center + Vector2.right * (9 * Mathf.Sin(angle)), 148 * squeeze, 148, rim, true);
            Face(vh, center, 148 * squeeze, 148, Color.white, front);
        }
        private static void Face(VertexHelper vh, Vector2 center, float halfWidth, float halfHeight, Color tint, bool sun)
        {
            int start = vh.currentVertCount;
            float left = sun ? 0 : .5f, right = sun ? .5f : 1;
            vh.AddVert(center + new Vector2(-halfWidth, -halfHeight), tint, new Vector2(left, 0));
            vh.AddVert(center + new Vector2(-halfWidth, halfHeight), tint, new Vector2(left, 1));
            vh.AddVert(center + new Vector2(halfWidth, halfHeight), tint, new Vector2(right, 1));
            vh.AddVert(center + new Vector2(halfWidth, -halfHeight), tint, new Vector2(right, 0));
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }
        private static void Shadow(VertexHelper vh, Vector2 center, float width, float height, Color tint)
        {
            const int count = 96; int start = vh.currentVertCount;
            var uv = new Vector2(.25f, .5f);
            vh.AddVert(center, tint, uv);
            for (int i = 0; i <= count; i++)
            {
                float a = i * 2 * (float)Math.PI / count;
                vh.AddVert(center + new Vector2(Mathf.Cos(a) * width, Mathf.Sin(a) * height), tint, uv);
                if (i > 0) vh.AddTriangle(start, start + i, start + i + 1);
            }
        }
    }
}
