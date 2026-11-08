using Cysharp.Threading.Tasks;


using System.Collections.Concurrent;

using System.IO;

using System.Threading;

using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;

using UnityEngine.Video;


namespace YuGiOhFight.Client.Presentation
{
    public class CardRenderer : MonoBehaviour
    {

        public const string BIG_SLASH = "／";
        public const string SMALL_SLASH = " / ";
        private static int prefabIndex = 0;

        #region Reference

        [Header("CardRenderer")]
        [SerializeField] private RectTransform builder;
        [SerializeField] private Camera renderCamera;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private Material matComposite;
        private Material matCompositeInstance;
        private Texture2D texFrame;
        public RenderTexture renderTexture;
        private ScriptableRenderContext context;

        #endregion

        #region Fonts

        private static bool fontsLoaded;

        //public static Font fontChineseSimplified;
        //public static Font fontChineseTraditional;
        //public static Font fontKorean;
        //public static Font fontJapanese;
        //public static Font fontEnglish;

        public static TMP_FontAsset tmpFontChineseSimplified;
        public static TMP_FontAsset tmpFontChineseTraditional;
        public static TMP_FontAsset tmpFontKorean;
        public static TMP_FontAsset tmpFontJapanese;
        public static TMP_FontAsset tmpFontEnglish;

        private static async UniTask LoadFontsAsync()
        {
            if (fontsLoaded)
                return;

            //fontChineseSimplified = await Addressables.LoadAssetAsync<Font>("RenderFontChineseSimplified").ToUniTask();
            tmpFontChineseSimplified = await Addressables.LoadAssetAsync<TMP_FontAsset>("RenderFontChineseSimplified").ToUniTask();
            //fontChineseTraditional = await Addressables.LoadAssetAsync<Font>("RenderFontChineseTraditional").ToUniTask();
            tmpFontChineseTraditional = await Addressables.LoadAssetAsync<TMP_FontAsset>("RenderFontChineseTraditional").ToUniTask();
            //fontKorean = await Addressables.LoadAssetAsync<Font>("RenderFontKorean").ToUniTask();
            tmpFontKorean = await Addressables.LoadAssetAsync<TMP_FontAsset>("RenderFontKorean").ToUniTask();
            //fontJapanese = await Addressables.LoadAssetAsync<Font>("RenderFontJapanese").ToUniTask();
            tmpFontJapanese = await Addressables.LoadAssetAsync<TMP_FontAsset>("RenderFontJapanese").ToUniTask();
            //fontEnglish = await Addressables.LoadAssetAsync<Font>("RenderFontEnglish").ToUniTask();
            tmpFontEnglish = await Addressables.LoadAssetAsync<TMP_FontAsset>("RenderFontEnglish").ToUniTask();

            fontsLoaded = true;
        }

        #endregion

        #region Card Builders

        private const string ADDR_STYLE_OCG = "Prefab/CardBuilderOCG.prefab";
        private const string ADDR_STYLE_RUSHDUEL = "Prefab/CardBuilderRushDuel.prefab";

        private static GameObject ocgBuilderPrefab;
        private static GameObject rdBuilderPrefab;
        private static bool buildersLoaded;
        private async UniTask LoadBuildersAsync()
        {
            if (buildersLoaded)
                return;
            ocgBuilderPrefab = await Addressables.LoadAssetAsync<GameObject>(ADDR_STYLE_OCG).ToUniTask();
            rdBuilderPrefab = await Addressables.LoadAssetAsync<GameObject>(ADDR_STYLE_RUSHDUEL).ToUniTask();
            buildersLoaded = true;
        }

        private CardBuilder ocgBuilder;
        private CardBuilder rdBuilder;

        private CardBuilder GetCardBuilder(CardStyle style)
        {
            currentStyle = style;
            if (ocgBuilderPrefab == null || rdBuilderPrefab == null)
            {
                Debug.LogError("[CardRenderer]: CardBuilder not loaded.");
                return null;
            }

            if (style == CardStyle.OCG_TCG)
            {
                if(ocgBuilder == null)
                    ocgBuilder = Instantiate(ocgBuilderPrefab, builder, false).GetComponent<CardBuilder>();
                if (rdBuilder != null)
                    rdBuilder.gameObject.SetActive(false);
                ocgBuilder.gameObject.SetActive(true);
                return ocgBuilder;
            }
            else
            {
                if(rdBuilder == null)
                    rdBuilder = Instantiate(rdBuilderPrefab, builder, false).GetComponent<CardBuilder>();
                if (ocgBuilder != null)
                    ocgBuilder.gameObject.SetActive(false);
                rdBuilder.gameObject.SetActive(true);
                return rdBuilder;
            }
        }

        #endregion

        #region Card Style

        public enum CardStyle
        {
            OCG_TCG,
            RUSH_DUEL
        }

        private CardStyle currentStyle;

        public static CardStyle GetCardStyleByCode(int code)
        {
            if (code >= 120000000 && code < 130000000)
                return CardStyle.RUSH_DUEL;
            var config = MDPro3.Config.Get("CardStyle", CardStyle.OCG_TCG.ToString());
            if (config == CardStyle.RUSH_DUEL.ToString())
                return CardStyle.RUSH_DUEL;
            return CardStyle.OCG_TCG;
        }

        #endregion

        #region Render Card
        private void Awake()
        {
            applicationFocused = Application.isFocused;
            if (videoPlayer != null)
                videoPlayer.playOnAwake = false;
            _ = LoadFontsAsync();
            _ = LoadBuildersAsync();

            prefabIndex++;
            transform.position = new Vector3(0f, 200f * prefabIndex, 0f);
        }

        public void SwitchLanguage(string language = null)
        {
            if (!fontsLoaded)
                return;
            language ??= MDPro3.Utility.Language.GetCardConfig();

        }

        public static bool NeedRushDuelStyle(int code)
        {
            var config = MDPro3.Config.Get("CardStyle", CardStyle.OCG_TCG.ToString());
            if (config == CardStyle.RUSH_DUEL.ToString())
                return true;
            if(code >= 120000000 && code < 130000000)
                return true;
            return false;
        }

        public void RenderName(int code)
        {
            MDPro3.Duel.YGOSharp.Card data = MDPro3.Duel.YGOSharp.CardsManager.GetRenderCard(code);
            if (data == null || data.Id == 0)
                return;
            var language = MDPro3.Utility.Language.GetCardConfig();
            if (data.isPre)
                language = MDPro3.Utility.Language.GetPrereleaseConfig();

            CardBuilder builder = GetCardBuilder(GetCardStyleByCode(code));
            builder.SetCardName(data, language);
            renderCamera.Render();
        }

        public bool RenderCard(int code, Texture2D art, Texture2D overFrame)
        {
            MDPro3.Duel.YGOSharp.Card data = MDPro3.Duel.YGOSharp.CardsManager.GetRenderCard(code);
            if (data == null || data.Id == 0)
                return false;
            var language = MDPro3.Utility.Language.GetCardConfig();
            if (data.isPre)
                language = MDPro3.Utility.Language.GetPrereleaseConfig();

            CardBuilder builder = GetCardBuilder(GetCardStyleByCode(code));
            builder.SetCard(data, language, art, overFrame);
            renderCamera.Render();
            return true;
        }

        #endregion

        #region Video Card

        private int code;
        private bool applicationFocused = true;
        private bool applicationPaused;
        private bool videoPlaybackRequested;
        private bool resumeVideoOnForeground;
        private long lastCompositeFrame = -1;
        private Texture lastCompositeSource;

        private bool VideoInForeground => applicationFocused && !applicationPaused;

        private void OnApplicationFocus(bool focused)
        {
            applicationFocused = focused;
            UpdateVideoApplicationState();
        }

        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            UpdateVideoApplicationState();
        }

        private void UpdateVideoApplicationState()
        {
            if (videoPlayer == null)
                return;
            if (!VideoInForeground)
            {
                if (videoPlayer.isPlaying)
                {
                    resumeVideoOnForeground = videoPlaybackRequested;
                    videoPlayer.Pause();
                }
                return;
            }
            if (resumeVideoOnForeground && videoPlaybackRequested
                && CardImageLoader.GetCardReferenceCount(code) > 0 && videoPlayer.isPrepared)
                videoPlayer.Play();
            resumeVideoOnForeground = false;
        }

        public static bool CardHasVideoArt(int code)
        {
            if (!MDPro3.Config.GetBool("VideoCard", true))
                return false;
            if (File.Exists(Runtime.Program.PATH_VIDEO_ART + code.ToString() + Runtime.Program.EXPANSION_MP4))
                return true;
            return false;
        }

        private static string GetVideoURL(int code)
        {
            string path = Runtime.Program.PATH_VIDEO_ART + code.ToString() + Runtime.Program.EXPANSION_MP4;
            path = Utility.Tools.GetPlatformFilePath(path);

            return path;
        }

        public UniTask<Texture> GetVideoCardAsync(int code) => GetVideoCardAsync(code, default);

        public async UniTask<Texture> GetVideoCardAsync(int code, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (!CardHasVideoArt(code))
                return null;

            this.code = code;

            MDPro3.Duel.YGOSharp.Card data = MDPro3.Duel.YGOSharp.CardsManager.GetRenderCard(code);
            if (data == null || data.Id == 0)
                return null;
            var language = MDPro3.Utility.Language.GetCardConfig();
            if (data.isPre)
                language = MDPro3.Utility.Language.GetPrereleaseConfig();

            CardBuilder builder = GetCardBuilder(GetCardStyleByCode(code));
            builder.SetCard(data, language, null);
            builder.SetAllIllustPartsOff();

            var previousTarget = RenderTexture.active;
            Texture2D frame = null;
            try
            {
                renderCamera.Render();
                RenderTexture.active = renderTexture;
                frame = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, true);
                frame.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                frame.Apply();
                frame.name = "Card_" + code;
                texFrame = frame;
            }
            catch
            {
                if (frame != null)
                    Destroy(frame);
                throw;
            }
            finally
            {
                // Preparing the video below can yield for several frames.
                RenderTexture.active = previousTarget;
            }
            var sourceRenderTexture = renderTexture;
            RenderTexture videoRenderTexture = null;
            try
            {
                Destroy(builder.gameObject);

                videoPlayer.gameObject.SetActive(true);
                videoPlayer.url = GetVideoURL(code);
                videoPlayer.Prepare();
                await UniTask.WaitUntil(() => videoPlayer.isPrepared, cancellationToken: token);

                videoRenderTexture = Instantiate(sourceRenderTexture);
                renderTexture = videoRenderTexture;
                SetMaterial(data);
                lastCompositeFrame = -1;
                lastCompositeSource = null;
                PlayVideo();
                return renderTexture;
            }
            catch
            {
                Destroy(frame);
                texFrame = null;
                if (matCompositeInstance != null)
                {
                    Destroy(matCompositeInstance);
                    matCompositeInstance = null;
                }
                if (videoRenderTexture != null)
                    Destroy(videoRenderTexture);
                renderTexture = sourceRenderTexture;
                throw;
            }
        }

        private void SetMaterial(MDPro3.Duel.YGOSharp.Card data)
        {
            matCompositeInstance = Instantiate(matComposite);
            matCompositeInstance.SetTexture("_MainTex", videoPlayer.texture);
            matCompositeInstance.SetTexture("_FrameTex", texFrame);
            matCompositeInstance.SetVector("_VideoRect", GetVideoRect(data));
        }

        private Vector4 GetVideoRect(MDPro3.Duel.YGOSharp.Card data)
        {
            if (NeedRushDuelStyle(data.Id))
            {
                return new Vector4(0.049f, 0.281f, 0.902f, 0.62f);
            }
            else
            {
                if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Pendulum))
                    return new Vector4(0.696f, 0.2275f, 0.86f, 0.5918f);
                else
                    return new Vector4(0.1235f, 0.297f, 0.7528f, 0.5175f);
            }
        }

        private void LateUpdate()
        {
            if (matCompositeInstance == null || videoPlayer == null)
                return;
            if (!VideoInForeground)
            {
                UpdateVideoApplicationState();
                return;
            }
            if (CardImageLoader.GetCardReferenceCount(code) <= 0)
            {
                if (videoPlayer.isPlaying)
                    videoPlayer.Pause();
                resumeVideoOnForeground = false;
                return;
            }
            if (videoPlaybackRequested)
                PlayVideo();
            CompositeVideoFrame();
        }

        private bool CompositeVideoFrame()
        {
            if (videoPlayer == null || !videoPlayer.isPrepared || matCompositeInstance == null || renderTexture == null)
                return false;
            var source = videoPlayer.texture;
            var frame = videoPlayer.frame;
            if (source == null || frame < 0 || (frame == lastCompositeFrame && source == lastCompositeSource))
                return false;
            var previousTarget = RenderTexture.active;
            try
            {
                matCompositeInstance.SetTexture("_MainTex", source);
                Graphics.Blit(source, renderTexture, matCompositeInstance);
                lastCompositeFrame = frame;
                lastCompositeSource = source;
                return true;
            }
            finally
            {
                RenderTexture.active = previousTarget;
            }
        }

        public void PauseVideo()
        {
            videoPlaybackRequested = false;
            resumeVideoOnForeground = false;
            if (videoPlayer != null && videoPlayer.isPlaying)
                videoPlayer.Pause();
        }

        public void PlayVideo()
        {
            videoPlaybackRequested = true;
            if (!VideoInForeground || videoPlayer == null || !videoPlayer.isPrepared || videoPlayer.isPlaying)
                return;
            videoPlayer.Play();
        }

        public void Dispose()
        {
            if(matCompositeInstance != null)
            {
                Destroy(matCompositeInstance);
                Destroy(renderTexture);
                Destroy(texFrame);
            }
            Destroy(gameObject);
        }

        #endregion

        #region Opaque Check

        private static readonly ConcurrentDictionary<int, bool> opaqueCache = new();

        public static void SetOverFrameOpaque(int code, bool isOpaque)
        {
            opaqueCache[code] = isOpaque;
        }

        public static bool OverFrameIsOpaque(int code, Texture2D art)
        {
            if (opaqueCache.TryGetValue(code, out bool isOpaque))
                return isOpaque;
            if (art == null)
                return true;
            isOpaque = art.name == "Opaque";
            opaqueCache[code] = isOpaque;
            return isOpaque;
        }

        #endregion

    }
}
