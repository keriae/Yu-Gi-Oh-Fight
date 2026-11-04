using System;
using System.Threading;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using MDPro3.Servant;
using MDPro3.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI
{
    /// <summary>Original card illustrations, cropped without stretching, for the opponent list.</summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class SoloEntryArtwork : MonoBehaviour
    {
        public string Mode { get; private set; } = "Deck";
        public bool UsesBanner => Mode != "Deck";
        public RectTransform BannerRoot { get; private set; }
        public RawImage CardImage { get; private set; }
        public int LoadedCard { get; private set; }
        public int CoverCard => requestedCard;

        private SoloBannerGraphic banner;
        private TextMeshProUGUI modeLabel, detailLabel, title;
        private CancellationTokenSource loading;
        private int requestedCard, version;
        private Vector2 previousSize;
        private float previousTitleHeight;
        private Texture previousTexture;
        private Vector2 cropFocus = new Vector2(0.5f, 0.5f);
        private float cropAspect = 2.45f;

        public void Configure(SoloSelector.BotInfo bot, bool modelAi, TextMeshProUGUI titleText)
        {
            EnsureLayers(titleText);
            var match = Regex.Match(bot.command ?? string.Empty, @"(?:^|\s)Random=(AI_\w+)(?:\s|$)");
            Mode = modelAi ? "ModelAI" : match.Success ? match.Groups[1].Value : "Deck";
            banner.SetMode(Mode);
            modeLabel.gameObject.SetActive(UsesBanner);
            modeLabel.text = Mode switch
            {
                "ModelAI" => "AI",
                "AI_LV1" => "LV. 01",
                "AI_LV2" => "LV. 02",
                "AI_LV3" => "LV. 03",
                "AI_LV4" => "LV. 04",
                "AI_WILD" => "WILD",
                "AI_ANTI_META" => "ANTI-META",
                _ => "RANDOM"
            };
            detailLabel.text = bot.desc ?? string.Empty;
            CardImage.enabled = true;
            var cover = Mode switch
            {
                "ModelAI" => 86066372, // Accesscode Talker
                "AI_LV1" => 27288416,   // Mokey Mokey
                "AI_LV2" => 55285840,   // Time Thief Redoer
                "AI_LV3" => 89631139,   // Blue-Eyes White Dragon
                "AI_LV4" => 96633955,   // Swordsoul Supreme Sovereign - Chengying
                "AI_WILD" => 41685633,  // Thunder Dragon Titan
                "AI_ANTI_META" => 15397015, // Inspector Boarder
                _ => bot.main0
            };
            cropFocus = new Vector2(0.5f, cover switch
            {
                86066372 => 0.70f,
                27288416 => 0.54f,
                55285840 => 0.70f,
                89631139 => 0.65f,
                96633955 => 0.73f,
                41685633 => 0.60f,
                15397015 => 0.73f,
                _ => 0.65f
            });
            cropAspect = cover == 41685633 ? 2.1f : 2.45f;
            LayoutArtwork(true);
            if (requestedCard == cover && (LoadedCard == cover || loading != null)) return;
            ++version;
            requestedCard = cover;
            CancelLoad();
            ReleaseCard();
            CardImage.texture = TextureManager.container.unknownArt.texture;
            if (requestedCard <= 0) return;
            loading = new CancellationTokenSource();
            _ = LoadCardAsync(requestedCard, version, loading.Token);
        }

        private void EnsureLayers(TextMeshProUGUI titleText)
        {
            title = titleText;
            if (BannerRoot != null) return;
            CardImage = GetComponent<RawImage>();
            CardImage.raycastTarget = false;
            CardImage.color = Color.white;
            CardImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            var root = new GameObject("SoloBannerArtwork", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(SoloBannerGraphic), typeof(LayoutElement));
            root.transform.SetParent(transform.parent, false);
            root.transform.SetAsFirstSibling();
            root.GetComponent<LayoutElement>().ignoreLayout = true;
            BannerRoot = root.GetComponent<RectTransform>();
            BannerRoot.anchorMin = Vector2.zero;
            BannerRoot.anchorMax = Vector2.one;
            banner = root.GetComponent<SoloBannerGraphic>();
            banner.raycastTarget = false;
            var fade = new GameObject("ArtworkFade", typeof(RectTransform), typeof(CanvasRenderer), typeof(SoloBannerGraphic));
            fade.transform.SetParent(transform, false);
            var fadeRect = fade.GetComponent<RectTransform>();
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.offsetMin = fadeRect.offsetMax = Vector2.zero;
            var fadeGraphic = fade.GetComponent<SoloBannerGraphic>();
            fadeGraphic.PhotoFade = true;
            fadeGraphic.raycastTarget = false;
            modeLabel = CreateLabel("ModeLabel", 30f, FontStyles.Bold);
            detailLabel = CreateLabel("DetailLabel", 21f, FontStyles.Normal);
        }

        private TextMeshProUGUI CreateLabel(string name, float size, FontStyles style)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            label.transform.SetParent(BannerRoot, false);
            var text = label.GetComponent<TextMeshProUGUI>();
            text.font = title.font;
            text.fontSharedMaterial = title.fontSharedMaterial;
            text.fontSize = text.fontSizeMax = size;
            text.fontSizeMin = size - 4f;
            text.enableAutoSizing = true;
            text.fontStyle = style;
            text.color = new Color(0.9f, 0.94f, 1f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.richText = false;
            text.raycastTarget = false;
            return text;
        }

        private void LateUpdate() => LayoutArtwork(false);

        private void LayoutArtwork(bool force)
        {
            if (BannerRoot == null || title == null || CardImage == null) return;
            var parent = transform.parent as RectTransform;
            if (parent == null) return;
            var size = parent.rect.size;
            var titleHeight = title.rectTransform.parent is RectTransform titleArea ? titleArea.rect.height : 42f;
            // The shipping prefab keeps a 256px artwork behind a 122px mask.
            // Use the actual title-free area, including the taller mobile layout.
            if (!force && size == previousSize && titleHeight == previousTitleHeight && CardImage.texture == previousTexture) return;
            previousSize = size;
            previousTitleHeight = titleHeight;
            previousTexture = CardImage.texture;
            BannerRoot.offsetMin = Vector2.zero;
            BannerRoot.offsetMax = new Vector2(0f, -titleHeight);
            var height = Mathf.Max(1f, size.y - titleHeight);
            // Titan's three heads need a taller source crop than a single portrait.
            var width = Mathf.Min(height * cropAspect, size.x * 0.52f);
            var art = CardImage.rectTransform;
            art.anchorMin = art.anchorMax = Vector2.zero;
            art.pivot = Vector2.zero;
            art.sizeDelta = new Vector2(width, height);
            art.anchoredPosition = Vector2.zero;
            var sourceAspect = CardImage.texture == null ? 1f : CardImage.texture.width / (float)Mathf.Max(1, CardImage.texture.height);
            var targetAspect = width / height;
            var uvWidth = Mathf.Min(1f, targetAspect / sourceAspect);
            var uvHeight = Mathf.Min(1f, sourceAspect / targetAspect);
            CardImage.uvRect = new Rect(Mathf.Clamp(cropFocus.x - uvWidth * 0.5f, 0f, 1f - uvWidth),
                Mathf.Clamp(cropFocus.y - uvHeight * 0.5f, 0f, 1f - uvHeight), uvWidth, uvHeight);
            var left = Mathf.Clamp((width + 24f) / Mathf.Max(1f, size.x), 0.2f, 0.6f);
            PlaceLabel(modeLabel.rectTransform, new Vector2(left, 0.36f), new Vector2(0.80f, 0.96f));
            PlaceLabel(detailLabel.rectTransform, new Vector2(left, UsesBanner ? 0.04f : 0.1f),
                new Vector2(0.94f, UsesBanner ? 0.40f : 0.9f));
        }

        private static void PlaceLabel(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private async UniTask LoadCardAsync(int code, int generation, CancellationToken token)
        {
            var ownsTexture = false;
            try
            {
                var texture = await CardImageLoader.LoadArtAsync(code, false, token);
                ownsTexture = texture != null;
                if (!ownsTexture || token.IsCancellationRequested || this == null || generation != version ||
                    requestedCard != code) return;
                CardImage.texture = texture;
                LoadedCard = code;
                ownsTexture = false;
                LayoutArtwork(true);
            }
            catch (OperationCanceledException) { }
            catch (MissingReferenceException) { }
            catch (Exception error) { Debug.LogWarning("Unable to load opponent artwork: " + error.GetType().Name); }
            finally
            {
                if (ownsTexture) CardImageLoader.ReleaseArt(code);
                if (this != null && generation == version)
                {
                    loading?.Dispose();
                    loading = null;
                }
            }
        }

        private void CancelLoad()
        {
            loading?.Cancel();
            loading?.Dispose();
            loading = null;
        }

        private void ReleaseCard()
        {
            CardImage.texture = null;
            if (LoadedCard == 0) return;
            CardImageLoader.ReleaseArt(LoadedCard);
            LoadedCard = 0;
        }

        private void OnDestroy()
        {
            ++version;
            CancelLoad();
            if (CardImage != null) ReleaseCard();
            else if (LoadedCard != 0) CardImageLoader.ReleaseArt(LoadedCard);
            if (BannerRoot != null) Destroy(BannerRoot.gameObject);
        }
    }

    /// <summary>Subtle cover backgrounds, level markers and the artwork edge fade.</summary>
    public sealed class SoloBannerGraphic : MaskableGraphic
    {
        public bool PhotoFade { get; set; }
        private string mode = "Deck";

        public void SetMode(string value)
        {
            if (mode == value) return;
            mode = value;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = rectTransform.rect;
            var dark = new Color(0.025f, 0.045f, 0.085f);
            if (PhotoFade)
            {
                var clear = new Color(dark.r, dark.g, dark.b, 0f);
                Quad(mesh, new Rect(rect.xMax - Mathf.Min(32f, rect.width * 0.25f), rect.yMin,
                    Mathf.Min(32f, rect.width * 0.25f), rect.height), clear, dark);
                return;
            }
            var accent = mode switch
            {
                "ModelAI" => new Color(0.40f, 0.80f, 1f),
                "AI_LV1" => new Color(0.34f, 0.86f, 0.63f),
                "AI_LV2" => new Color(0.32f, 0.66f, 1f),
                "AI_LV3" => new Color(0.68f, 0.51f, 1f),
                "AI_LV4" => new Color(1f, 0.77f, 0.28f),
                "AI_WILD" => new Color(1f, 0.35f, 0.27f),
                "AI_ANTI_META" => new Color(0.96f, 0.42f, 0.75f),
                _ => new Color(0.43f, 0.59f, 0.74f)
            };
            Quad(mesh, rect, dark, Color.Lerp(dark, accent, 0.10f));
            var level = mode switch { "AI_LV1" => 1, "AI_LV2" => 2, "AI_LV3" => 3, "AI_LV4" => 4, _ => 0 };
            if (level == 0) return;
            for (var i = 0; i < 4; ++i)
            {
                var tint = i < level ? accent : new Color(0.22f, 0.28f, 0.36f);
                Quad(mesh, new Rect(rect.xMax - 76f + i * 14f, rect.yMin + rect.height * 0.58f, 9f, 7f), tint, tint);
            }
        }

        private static void Quad(VertexHelper mesh, Rect rect, Color left, Color right)
        {
            var index = mesh.currentVertCount;
            mesh.AddVert(new Vector2(rect.xMin, rect.yMin), left, Vector2.zero);
            mesh.AddVert(new Vector2(rect.xMin, rect.yMax), left, Vector2.zero);
            mesh.AddVert(new Vector2(rect.xMax, rect.yMax), right, Vector2.zero);
            mesh.AddVert(new Vector2(rect.xMax, rect.yMin), right, Vector2.zero);
            mesh.AddTriangle(index, index + 1, index + 2);
            mesh.AddTriangle(index, index + 2, index + 3);
        }
    }
}
