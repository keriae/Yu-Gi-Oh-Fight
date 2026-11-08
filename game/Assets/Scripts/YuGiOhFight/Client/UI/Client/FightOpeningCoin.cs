using System;
using System.IO;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


namespace YuGiOhFight.Client.UI.Client
{
    // Presents the server's actual opening order. It never chooses or sends a
    // protocol response. A screen overlay keeps the toss above native cards.
    public sealed class FightOpeningCoin : MonoBehaviour
    {
        private const float Duration = 1.92f;
        private const float TossDuration = 1.18f;
        private static FightOpeningCoin current;
        private object owner;
        private bool completed, cancelled;
        private CanvasGroup group;
        private FightOpeningCoinGraphic coin;
        private TMP_Text result;
        private float started;
        private AsyncOperationHandle<AudioClip> throwSound, landingSound;
        private bool throwPlayed, landingPlayed;

        public static bool IsShowing => FightSceneCinematic.IsShowing || current != null && current.gameObject.activeInHierarchy;

        public static async UniTask PlayAsync(object owner, bool isFirst, Func<bool> stillCurrent)
        {
            var background = Runtime.Program.instance?.ocgcore?.DuelBGManager;
            if (background != null && background.Scene3DActive && Duel.DuelSceneCatalog.IsIntegratedTheme(background.ActiveSceneId))
            {
                await FightSceneCinematic.PlayAsync(owner, background.ActiveSceneId, isFirst, stillCurrent);
                return;
            }
            if (Runtime.Program.instance == null || Runtime.Program.instance.ui_ == null || Runtime.Program.instance.ui_.popup == null
                || stillCurrent != null && !stillCurrent()) return;
            if (current != null) current.CancelInstance();

            var canvasRoot = new GameObject("Fight opening coin", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var root = canvasRoot.GetComponent<RectTransform>();
            root.SetParent(Runtime.Program.instance.transform, false);
            var canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32000;
            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            var surface = root.gameObject.AddComponent<Image>();
            surface.color = new Color(.008f, .018f, .028f, .54f); surface.raycastTarget = true;
            FightOpeningCoin presenter = root.gameObject.AddComponent<FightOpeningCoin>();
            presenter.owner = owner; presenter.started = Time.unscaledTime;
            presenter.group = root.gameObject.AddComponent<CanvasGroup>();
            presenter.group.blocksRaycasts = true; presenter.group.interactable = false;
            var content = FightUI.Node(root, "Opening content", 0, 0, 480, 360);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(.5f, .5f);
            content.anchoredPosition = Vector2.zero;
            var coinRoot = FightUI.Node(content, "Coin", 116, 24, 248, 248);
            presenter.coin = coinRoot.gameObject.AddComponent<FightOpeningCoinGraphic>();
            presenter.coin.LoadArtwork();
            presenter.coin.IsFirst = isFirst; presenter.coin.raycastTarget = false;
            var label = isFirst ? "先手" : "后手";
            presenter.result = FightUI.Text(content, label, 24, 292, 432, 58, 32,
                isFirst ? FightUI.Gold : FightUI.CyanText, TextAlignmentOptions.Center);
            presenter.result.name = "Opening result"; presenter.result.raycastTarget = false;
            presenter.result.fontStyle = FontStyles.Bold; presenter.result.outlineWidth = .06f;
            presenter.result.color = new Color(presenter.result.color.r, presenter.result.color.g, presenter.result.color.b, 0);
            presenter.throwSound = Addressables.LoadAssetAsync<AudioClip>("SE_COIN_THROW");
            presenter.landingSound = Addressables.LoadAssetAsync<AudioClip>("SE_COIN_DECIDE");
            current = presenter;
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
                    presenter.completed = true;
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
            FightSceneCinematic.Cancel(owner);
            if (current != null && ReferenceEquals(current.owner, owner)) current.CancelInstance();
        }
        private void CancelInstance()
        {
            if (completed || cancelled) return;
            cancelled = true;
            if (current == this) current = null;
            gameObject.SetActive(false);
        }
        private void Paint()
        {
            float elapsed = Time.unscaledTime - started;
            coin.Progress = Mathf.Clamp01(elapsed / TossDuration);
            group.alpha = Mathf.Min(Mathf.Clamp01(elapsed / .12f), Mathf.Clamp01((Duration - elapsed) / .12f));
            float resultReveal = Mathf.Clamp01((elapsed - TossDuration) / .16f);
            var tint = result.color; tint.a = resultReveal * resultReveal * (3 - 2 * resultReveal); result.color = tint;
            if (!throwPlayed && elapsed >= .08f) { throwPlayed = true; PlaySound(throwSound, .22f); }
            if (!landingPlayed && elapsed >= TossDuration) { landingPlayed = true; PlaySound(landingSound, .30f); }
        }
        private static void PlaySound(AsyncOperationHandle<AudioClip> handle, float volume)
        {
            // Slow clips are skipped, preventing a late sound in another duel.
            if (handle.IsValid() && handle.IsDone && handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                Runtime.AudioManager.PlaySEClip(handle.Result, volume);
        }
        private void OnDestroy()
        {
            if (!completed && !cancelled) cancelled = true;
            if (current == this) current = null;
            if (throwSound.IsValid()) Addressables.Release(throwSound);
            if (landingSound.IsValid()) Addressables.Release(landingSound);
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
        public override Texture mainTexture => artwork != null ? artwork : base.mainTexture;
        internal void LoadArtwork()
        {
            string path = Runtime.Program.PATH_DATA + "ClientUI/opening-coin-atlas.png";
            try
            {
                if (File.Exists(path))
                {
                    var candidate = FightAsset.Acquire(path);
                    if (candidate != null && candidate.width >= 128 && candidate.height >= 64) artwork = candidate;
                }
            }
            catch (Exception error)
            { Debug.LogWarning("[FightClient] opening coin artwork unavailable (" + error.GetType().Name + "); using engraved coin."); }
            SetMaterialDirty(); SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var origin = rectTransform.rect.center;
            float flight = Mathf.Clamp01(progress / .86f);
            float ease = 1 - (1 - flight) * (1 - flight) * (1 - flight);
            float angle = ease * 4 * (float)Math.PI;
            float face = Mathf.Cos(angle), squeeze = Mathf.Max(.025f, Mathf.Abs(face));
            float settle = Mathf.Clamp01((progress - .86f) / .14f);
            float rise = Mathf.Sin(flight * (float)Math.PI) * 58 + Mathf.Sin(settle * (float)Math.PI) * 5;
            var center = origin + Vector2.up * rise;
            bool front = face >= 0 ? IsFirst : !IsFirst;
            // Contact narrows while airborne; a small rebound settles before
            // the authoritative order label and landing sound appear.
            for (int layer = 0; layer < 5; layer++)
                Shadow(vh, origin + Vector2.down * 124, 84 - rise * .3f + layer * 3, 7 + layer * 1.5f,
                    new Color(0, 0, 0, .04f));
            var rim = new Color(.47f, .33f, .16f, 1);
            if (artwork != null)
            {
                Face(vh, center + Vector2.right * (7 * Mathf.Sin(angle)), 119 * squeeze, 119, rim, true);
                Face(vh, center, 119 * squeeze, 119, Color.white, front);
            }
            else EngravedCoin(vh, center, squeeze, front);
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
        private static void EngravedCoin(VertexHelper vh, Vector2 center, float squeeze, bool sun)
        {
            var metal = sun ? new Color(.88f, .73f, .40f) : new Color(.64f, .79f, .88f);
            var recess = sun ? new Color(.24f, .17f, .07f) : new Color(.12f, .22f, .29f);
            Ellipse(vh, center, 119 * squeeze, 119, metal);
            Ellipse(vh, center, 107 * squeeze, 107, recess);
            Ellipse(vh, center, 102 * squeeze, 102, metal);
            Ellipse(vh, center, 93 * squeeze, 93, recess);
            if (sun)
            {
                Ellipse(vh, center, 29 * squeeze, 29, metal);
                for (int index = 0; index < 12; index++)
                {
                    float a = index * (float)Math.PI / 6;
                    var direction = new Vector2(Mathf.Cos(a) * squeeze, Mathf.Sin(a));
                    var cross = new Vector2(-Mathf.Sin(a) * squeeze, Mathf.Cos(a)) * 3.5f;
                    var inner = center + direction * 39; var outer = center + direction * 63;
                    int start = vh.currentVertCount;
                    vh.AddVert(inner - cross, metal, Vector2.zero); vh.AddVert(inner + cross, metal, Vector2.zero);
                    vh.AddVert(outer + cross, metal, Vector2.zero); vh.AddVert(outer - cross, metal, Vector2.zero);
                    vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
                }
            }
            else
            {
                Ellipse(vh, center, 48 * squeeze, 48, metal);
                Ellipse(vh, center + new Vector2(22 * squeeze, 15), 43 * squeeze, 43, recess);
            }
        }
        private static void Ellipse(VertexHelper vh, Vector2 center, float width, float height, Color tint)
        {
            const int count = 64; int start = vh.currentVertCount;
            vh.AddVert(center, tint, Vector2.zero);
            for (int i = 0; i <= count; i++)
            {
                float a = i * 2 * (float)Math.PI / count;
                vh.AddVert(center + new Vector2(Mathf.Cos(a) * width, Mathf.Sin(a) * height), tint, Vector2.zero);
                if (i > 0) vh.AddTriangle(start, start + i, start + i + 1);
            }
        }
        private void Shadow(VertexHelper vh, Vector2 center, float width, float height, Color tint)
        {
            const int count = 64; int start = vh.currentVertCount;
            // The production atlas is metal at this sample. The fallback uses
            // Unity's white texture so opacity remains a soft neutral shadow.
            var uv = artwork != null ? new Vector2(.25f, .5f) : Vector2.zero;
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
