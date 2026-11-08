using UnityEngine;
using UnityEngine.UI;

using System.Threading;

using Cysharp.Threading.Tasks;


namespace YuGiOhFight.Client.UI
{
    [RequireComponent(typeof(RawImage))]
    public class CardRawImageHandler : MonoBehaviour
    {
        #region 字段和属性

        public bool cache;
        public MDPro3.Duel.YGOSharp.Card card;
        public int protectorCode = 1070001;

        protected bool m_Refreshed;
        public bool Refreshed => m_Refreshed;

        private RawImage m_RawImage;
        public RawImage RawImage =>
            m_RawImage = m_RawImage != null ? m_RawImage
            : GetComponent<RawImage>();

        protected Material normalMat;
        protected Material tempMat;
        private CancellationTokenSource cts;

        private int currentLoadId = 0;
        private int loadedCardId = 0;
        private bool isRenderTexture;
        private Client.FightCardPoolCache clientPool;
        private bool loadedStatic;
        private RectTransform printRarityBadge;

        private void RefreshPrintRarityBadge()
        {
            if (printRarityBadge == null && card != null && card.Id > 0
                && (GetComponentInParent<SelectionButton_CardInCollection>() != null
                    || GetComponentInParent<SelectionButton_CardInDeck>() != null))
                printRarityBadge = Client.FightUI.PrintRarityBadge(transform, card);
            if (printRarityBadge != null) Client.FightUI.UpdatePrintRarityBadge(printRarityBadge, card);
        }

        internal void SetClientPool(Client.FightCardPoolCache pool)
        {
            if (clientPool == pool) return;
            CancelCurrentLoad(); ReleaseCard(); clientPool = pool;
            if (card != null) SetCard(card);
        }

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            Runtime.SystemEvent.OnLanguageChange += OnLanguageChange;
            Runtime.SystemEvent.OnVideoCardConfigChange += OnVideoCardConfigChange;
        }

        private void OnEnable()
        {
            if (clientPool != null || card == null || card.Id <= 0 || m_Refreshed) return;
            CancelCurrentLoad();
            _ = LoadCardPicAsync();
        }

        protected void OnDestroy()
        {
            CancelCurrentLoad();
            Destroy(normalMat);
            Destroy(tempMat);
            ReleaseCard();

            Runtime.SystemEvent.OnLanguageChange -= OnLanguageChange;
            Runtime.SystemEvent.OnVideoCardConfigChange -= OnVideoCardConfigChange;
        }

        #endregion

        #region 公共API

        public void SetCard(int code)
        {
            if (code <= 0)
            {
                card = null;
                RefreshPrintRarityBadge();
                SetProtector(protectorCode);
            }
            else
                SetCard(MDPro3.Duel.YGOSharp.CardsManager.Get(code));
        }

        public void SetCard(MDPro3.Duel.YGOSharp.Card data)
        {
            if (data == null)
            {
                card = null;
                RefreshPrintRarityBadge();
                ReleaseCard();
                return;
            }

            card = data;
            RefreshPrintRarityBadge();
            if (loadedCardId == data.Id && m_Refreshed)
                return;

            CancelCurrentLoad();
            card = data;
            if (clientPool != null)
            {
                ReleaseCard();
                if (clientPool.TryAcquire(data.Id, out var ready)) ApplyClientTexture(data.Id, ready);
                else
                {
                    if (normalMat == null) normalMat = MaterialLoader.GetCardMaterial(-1);
                    normalMat.SetTexture("_LoadingTex", Runtime.TextureManager.container.GetCardLoadingTexture(data));
                    normalMat.SetFloat("_LoadingBlend", 1f); RawImage.material = normalMat;
                    _ = LoadClientTextureAsync(data.Id, ++currentLoadId);
                }
                return;
            }
            if (card != null)
            {
                _ = LoadCardPicAsync();
            }
            else
            {
                ReleaseCard();
            }
        }

        public void CancelCurrentLoad()
        {
            try
            {
                currentLoadId++;
                cts?.Cancel();
                cts?.Dispose();
            }
            finally
            {
                cts = null;
            }
        }

        public void RefreshRarity(int code)
        {
            if (card == null || card.Id != code)
                return;
            if (clientPool != null)
            {
                ApplyClientMaterial(code);
                return;
            }

            if (tempMat != null)
                DestroyImmediate(tempMat);

            if (MDPro3.CardRarity.GetRarity(card.Id) == MDPro3.CardRarity.Rarity.Normal)
            {
                if (normalMat != null)
                {
                    normalMat.SetFloat("_LoadingBlend", 0f);
                    RawImage.material = normalMat;
                }
            }
            else
            {
                _ = LoadMatAsync(0f, default);
            }
        }

        public void SetProtector(int code)
        {
            protectorCode = code;
            _ = LoadProtectorAsync(code);
        }

        #endregion

        #region 私有实现

        private void ReleaseCard()
        {
            if (loadedCardId != 0)
            {
                Presentation.CardImageLoader.ReleaseCard(loadedCardId, loadedStatic);
                loadedCardId = 0;
                loadedStatic = false;
            }
            RawImage.texture = null;
            m_Refreshed = false;
        }

        private void ApplyClientTexture(int code, Texture texture)
        {
            loadedCardId = code; loadedStatic = true; isRenderTexture = false;
            // Static search thumbnails stay readable while cells are recycled.
            ApplyClientMaterial(code); RawImage.texture = texture; m_Refreshed = true;
        }

        private void ApplyClientMaterial(int code)
        {
            if (tempMat != null) { Destroy(tempMat); tempMat = null; }
            if (normalMat == null) normalMat = MaterialLoader.GetCardMaterial(-1);
            var material = normalMat;
            if (MDPro3.CardRarity.GetRarity(code) != MDPro3.CardRarity.Rarity.Normal)
                material = tempMat = MaterialLoader.GetCardMaterial(code);
            material.SetFloat("_LoadingBlend", 0f);
            RawImage.material = material;
        }

        private async UniTaskVoid LoadClientTextureAsync(int code, int generation)
        {
            Client.FightCardPoolCache pool = clientPool;
            cts = new CancellationTokenSource();
            var token = cts.Token;
            try
            {
                var texture = await pool.AcquireAsync(code, token);
                if (texture == null) return;
                if (this == null || token.IsCancellationRequested || generation != currentLoadId || clientPool != pool || card == null || card.Id != code)
                { Presentation.CardImageLoader.ReleaseCard(code, true); return; }
                ApplyClientTexture(code, texture);
            }
            catch (System.OperationCanceledException) { }
            catch (System.Exception error) { Debug.LogError("Client card thumbnail failed: " + error.Message); }
        }

        private async UniTaskVoid LoadCardPicAsync()
        {
            if (card == null)
                return;

            int targetCardId = card.Id;
            int loadId = ++currentLoadId;
            m_Refreshed = false;

            try
            {
                cts = new CancellationTokenSource();
                var token = cts.Token;
                await UniTask.Yield(PlayerLoopTiming.Update, token);
                if (card == null || card.Id != targetCardId || loadId != currentLoadId)
                    return;

                if (normalMat == null)
                    normalMat = MaterialLoader.GetCardMaterial(-1);

                normalMat.SetTexture("_LoadingTex", Runtime.TextureManager.container
                    .GetCardLoadingTexture(MDPro3.Duel.YGOSharp.CardsManager.Get(card.Id)));
                normalMat.SetFloat("_LoadingBlend", 1f);
                RawImage.material = normalMat;

                if (tempMat != null)
                {
                    Destroy(tempMat);
                    tempMat = null;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
                if (card == null || card.Id != targetCardId || loadId != currentLoadId)
                    return;

                var cardTex = await Presentation.CardImageLoader.LoadCardAsync(
                    card.Id,
                    cache,
                    token);
                if (card == null || card.Id != targetCardId || loadId != currentLoadId)
                {
                    if (cardTex != null)
                        Presentation.CardImageLoader.ReleaseCard(targetCardId, !(cardTex is RenderTexture));
                    return;
                }

                if (cardTex == null)
                {
                    Debug.LogError($"Failed to load texture for card {card.Id}");
                    return;
                }

                if (loadedCardId != 0)
                    Presentation.CardImageLoader.ReleaseCard(loadedCardId, loadedStatic);

                loadedCardId = card.Id;
                loadedStatic = !(cardTex is RenderTexture);
                RawImage.texture = cardTex;
                isRenderTexture = cardTex is RenderTexture;

                if (MDPro3.CardRarity.GetRarity(card.Id) == MDPro3.CardRarity.Rarity.Normal)
                    await SetMaterialFloatAsync(normalMat, "_LoadingBlend", 0f, 0.1f, token);
                else
                    await LoadMatAsync(0.1f, token);
                if (card == null || card.Id != targetCardId || loadId != currentLoadId)
                    return;

                m_Refreshed = true;
            }
            catch (System.OperationCanceledException)
            {
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Load card picture failed: {e.Message}");
            }
        }

        protected async UniTask LoadMatAsync(float fadeTime, CancellationToken token)
        {
            if (card == null) return;

            try
            {
                tempMat = MaterialLoader.GetCardMaterial(card.Id);

                if (cts.Token.IsCancellationRequested || card == null)
                    return;

                tempMat.SetFloat("_LoadingBlend", 1f);
                tempMat.SetTexture("_LoadingTex", normalMat.GetTexture("_LoadingTex"));
                RawImage.material = tempMat;
                await SetMaterialFloatAsync(tempMat, "_LoadingBlend", 0f, fadeTime, token);
            }
            catch (System.OperationCanceledException)
            {
                // 任务被取消，不做处理
            }
        }

        private async UniTask LoadProtectorAsync(int code)
        {
            m_Refreshed = false;

            CancelCurrentLoad();
            ReleaseCard();
            card = null;

            try
            {
                var material = await Runtime.ABLoader.LoadProtectorMaterial(
                    code.ToString(),
                    destroyCancellationToken);

                if (material != null)
                {
                    RawImage.material = material;
                    RawImage.texture = null;
                    m_Refreshed = true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Load protector failed: {e.Message}");
            }
        }

        public static async UniTask SetMaterialFloatAsync(Material mat, string propertyName, float endValue, float duration, CancellationToken token)
        {
            if (mat == null)
                return;

            float startValue = mat.GetFloat(propertyName);
            float elapsedTime = 0f;

            while (elapsedTime < duration && !token.IsCancellationRequested)
            {
                await UniTask.Yield(cancellationToken: token);
                if (mat == null || token.IsCancellationRequested)
                    return;

                float t = elapsedTime / duration;
                float currentValue = Mathf.Lerp(startValue, endValue, t);
                mat.SetFloat(propertyName, currentValue);
                elapsedTime += Time.deltaTime;
            }

            if (mat != null && !token.IsCancellationRequested)
                mat.SetFloat(propertyName, endValue);
        }

        private void OnVideoCardConfigChange()
        {
            if (languageReloadFrame == Time.frameCount || clientPool != null || card == null) return;
            var config = MDPro3.Config.GetBool("VideoCard", true);
            if (config && MDPro3.IO.ResourceManager.CardHasVideo(card.Id))
                _ = ReloadAsync();
            else if (!config && (isRenderTexture || MDPro3.IO.ResourceManager.CardHasVideo(card.Id)))
            {
                CancelCurrentLoad();
                if (isRenderTexture)
                {
                    // CheckArtVideoConfig already cleared this video entry.
                    loadedCardId = 0;
                    loadedStatic = false;
                    isRenderTexture = false;
                    RawImage.texture = null;
                }
                m_Refreshed = false;
                if (isActiveAndEnabled) _ = LoadCardPicAsync();
            }
        }

        private int languageReloadFrame = -1;
        private void OnLanguageChange()
        {
            if (card == null || card.Id <= 0) return;
            MDPro3.Duel.YGOSharp.CardsManager.RefreshLocalizedText(card);
            if (clientPool != null) return;
            languageReloadFrame = Time.frameCount;
            CancelCurrentLoad();
            // The old cache was cleared before this event. Releasing its ID
            // now could subtract a reference from a newly loaded texture.
            loadedCardId = 0;
            loadedStatic = false;
            RawImage.texture = null;
            m_Refreshed = false;
            if (isActiveAndEnabled) _ = LoadCardPicAsync();
        }

        private async UniTask ReloadAsync()
        {
            await UniTask.Yield();
            CancelCurrentLoad();
            _ = LoadCardPicAsync();
        }

        #endregion
    }
}
