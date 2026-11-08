using Cysharp.Threading.Tasks;


using UnityEngine;


namespace YuGiOhFight.Client.Presentation
{
    public static class CardImageLoader
    {

        #region 字段和属性

        private class VideoCacheEntry
        {
            public Texture Texture;
            public CardRenderer Renderer;
            public int ReferenceCount;
            public bool IsPersistent;
            public System.Threading.Tasks.Task<(Texture Texture, CardRenderer Renderer)> LoadingTask;
        }

        private const int CARD_NAME_HEIGHT = 175;
        public const float DEFAULT_ASPECT = 723 / 1054f;

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, CacheEntry> cachedArts = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, CacheEntry> cachedCards = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, Texture2D> cachedCardNames = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, Texture2D> cachedOverFrames = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, VideoCacheEntry> cachedVideoCards = new();

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, System.Threading.SemaphoreSlim> artLoadingLocks = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, System.Threading.SemaphoreSlim> overFrameLoadingLocks = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, System.Threading.SemaphoreSlim> cardLoadingLocks = new();

        private static readonly System.Collections.Generic.List<CardRenderer> videos = new();
        private static int cardCacheEpoch;
        private static int videoCacheEpoch;
        private static System.Threading.CancellationTokenSource videoCacheCancellation = new();

        public static bool lastCardFoundArt;
        public static bool lastCardRenderSucceed;

        private static System.Threading.SemaphoreSlim artSemaphore;
        private static System.Threading.SemaphoreSlim overFrameSemaphore;
        private static System.Threading.SemaphoreSlim cardSemaphore;
        private static int maxLoads;

        #endregion

        #region 初始化

        public static void Initialize()
        {
            maxLoads = GetOptimalConcurrency();

            artSemaphore = new System.Threading.SemaphoreSlim(maxLoads, maxLoads);
            overFrameSemaphore = new System.Threading.SemaphoreSlim(maxLoads, maxLoads);
            cardSemaphore = new System.Threading.SemaphoreSlim(maxLoads, maxLoads);

            Runtime.SystemEvent.OnVideoCardConfigChange -= CheckArtVideoConfig;
            Runtime.SystemEvent.OnVideoCardConfigChange += CheckArtVideoConfig;
        }

        public static async UniTask InitializeAsync()
        {
            maxLoads = GetOptimalConcurrency();

            artSemaphore = new System.Threading.SemaphoreSlim(maxLoads, maxLoads);
            overFrameSemaphore = new System.Threading.SemaphoreSlim(maxLoads, maxLoads);
            cardSemaphore = new System.Threading.SemaphoreSlim(maxLoads, maxLoads);

            Runtime.SystemEvent.OnVideoCardConfigChange -= CheckArtVideoConfig;
            Runtime.SystemEvent.OnVideoCardConfigChange += CheckArtVideoConfig;

            await UniTask.CompletedTask;
        }

        private static int GetOptimalConcurrency()
        {
            if (MDPro3.Utility.DeviceInfo.OnMobile())
                return 1;
            else
                return 2;
        }

        #endregion

        #region API

        public static async UniTask<Texture2D> LoadArtAsync(
            int code,
            bool persistent = false,
            System.Threading.CancellationToken token = default)
        {
            var lockObj = artLoadingLocks.GetOrAdd(code, _ => new System.Threading.SemaphoreSlim(1, 1));
            await lockObj.WaitAsync(token);

            try
            {
                if (cachedArts.TryGetValue(code, out var entry))
                {

                    if (entry.LoadingTask != null)
                        await entry.LoadingTask.AsUniTask().AttachExternalCancellation(token);
                    if (entry.Texture != null)
                    {

                        System.Threading.Interlocked.Increment(ref entry.ReferenceCount);
                        entry.IsPersistent |= persistent;
                    }
                    else
                        Debug.LogError($"Art texture is null for code {code}");

                    return entry.Texture;
                }

                CacheEntry newEntry = new()
                {
                    LoadingTask = InternalLoadArtAsync(code, token)
                };

                if (cachedArts.TryAdd(code, newEntry))
                {

                    newEntry.ReferenceCount = 1;
                    newEntry.IsPersistent = persistent;
                    try
                    {
                        newEntry.Texture = await newEntry.LoadingTask;
                    }
                    catch(System.OperationCanceledException ex)
                    {
                        cachedArts.TryRemove(code, out _);
                        throw ex;
                    }
                    if (newEntry.Texture == null)
                        Debug.LogError($"{code} art is null");

                    newEntry.LoadingTask = null;
                    return newEntry.Texture;
                }
                else
                {
                    Debug.LogError("CardImageLoader: Unexpected Errror.");
                    return null;
                }
            }
            catch (System.Exception e)
            {
                throw e;
            }
            finally
            {
                lockObj.Release();
                artLoadingLocks.TryRemove(code, out _);
            }
        }

        public static void ReleaseArt(int code)
        {
            if (!cachedArts.TryGetValue(code, out var entry)) return;

            var newCount = System.Threading.Interlocked.Decrement(ref entry.ReferenceCount);

            if(newCount < 0)
                Debug.LogError($"Art reference count for code {code} is less than zero.");

            if (newCount == 0 && !entry.IsPersistent)
                if (cachedArts.TryRemove(code, out _))
                    Object.Destroy(entry.Texture);
        }

        public static async UniTask<Texture2D> LoadOverFrameAsync(
            int code,
            System.Threading.CancellationToken token = default)
        {
            var lockObj = overFrameLoadingLocks.GetOrAdd(code, _ => new System.Threading.SemaphoreSlim(1, 1));
            await lockObj.WaitAsync(token);

            try
            {
                if (cachedOverFrames.TryGetValue(code, out var tex))
                    return tex;

                var of = await InternalLoadOverFrameAsync(code, token);
                if(of != null)
                    cachedOverFrames[code] = of;
                return of;
            }
            catch (System.Exception e)
            {
                throw e;
            }
            finally
            {
                lockObj.Release();
                overFrameLoadingLocks.TryRemove(code, out _);
            }
        }


        public static async UniTask<Texture> LoadCardAsync(
            int code,
            bool persistent = false,
            System.Threading.CancellationToken token = default,
            bool forceTexture = false)
        {
            if(MDPro3.IO.ResourceManager.CardHasOverFrame(code))
                persistent = true;

            var epoch = System.Threading.Volatile.Read(ref cardCacheEpoch);
            var lockObj = cardLoadingLocks.GetOrAdd(code, _ => new System.Threading.SemaphoreSlim(1, 1));
            await lockObj.WaitAsync(token);
            try
            {
                EnsureCurrentCardCache(epoch, token);
                if (!CardRenderer.CardHasVideoArt(code) || forceTexture)
                {
                    if (cachedCards.TryGetValue(code, out var entry))
                    {
                        if (entry.LoadingTask != null)
                            await entry.LoadingTask.AsUniTask().AttachExternalCancellation(token);
                        EnsureCurrentCardCache(epoch, token);
                        if (entry.Texture != null)
                        {
                            System.Threading.Interlocked.Increment(ref entry.ReferenceCount);
                            entry.IsPersistent |= persistent;
                        }
                        else
                            Debug.LogError($"Card texture is null for code {code}");
                        return entry.Texture;
                    }

                    CacheEntry newEntry = new();

                    if (cachedCards.TryAdd(code, newEntry))
                    {
                        newEntry.ReferenceCount = 1;
                        newEntry.IsPersistent = persistent;
                        try
                        {
                            newEntry.LoadingTask = InternalLoadCardAsync(code, token);
                            newEntry.Texture = await newEntry.LoadingTask;
                            EnsureCurrentCardCache(epoch, token);
                        }
                        catch
                        {
                            RemoveMatchingEntry(cachedCards, code, newEntry);
                            DestroyOwnedCardTexture(newEntry.Texture);
                            throw;
                        }
                        newEntry.LoadingTask = null;
                        return newEntry.Texture;
                    }
                    else
                    {
                        Debug.LogError("CardImageLoader: Unexpected Error.");
                        return null;
                    }
                }
                else
                {
                    var videoEpoch = System.Threading.Volatile.Read(ref videoCacheEpoch);
                    using var videoCancellation = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(token, videoCacheCancellation.Token);
                    var videoToken = videoCancellation.Token;
                    try
                    {
                        EnsureCurrentVideoCache(epoch, videoEpoch, videoToken);
                        if (cachedVideoCards.TryGetValue(code, out var entry))
                        {
                            if (entry.LoadingTask != null)
                                await entry.LoadingTask.AsUniTask().AttachExternalCancellation(videoToken);
                            EnsureCurrentVideoCache(epoch, videoEpoch, videoToken);
                            if (entry.Texture != null)
                            {
                                System.Threading.Interlocked.Increment(ref entry.ReferenceCount);
                                entry.IsPersistent |= persistent;
                            }
                            else
                                Debug.LogError($"Video card texture is null for code {code}");
                            return entry.Texture;
                        }

                        VideoCacheEntry newEntry = new();

                        if (cachedVideoCards.TryAdd(code, newEntry))
                        {
                            newEntry.ReferenceCount = 1;
                            newEntry.IsPersistent = persistent;
                            try
                            {
                                newEntry.LoadingTask = InternalLoadVideoCardAsync(code, videoToken, epoch, videoEpoch).AsTask();
                                var result = await newEntry.LoadingTask;
                                newEntry.Texture = result.Texture;
                                newEntry.Renderer = result.Renderer;
                                EnsureCurrentVideoCache(epoch, videoEpoch, videoToken);
                            }
                            catch
                            {
                                RemoveMatchingEntry(cachedVideoCards, code, newEntry);
                                if (newEntry.Renderer != null)
                                {
                                    videos.Remove(newEntry.Renderer);
                                    newEntry.Renderer.Dispose();
                                }
                                throw;
                            }
                            if (newEntry.Texture == null)
                                Debug.LogError($"{code} video card is null");

                            newEntry.LoadingTask = null;
                            return newEntry.Texture;
                        }
                        else
                        {
                            Debug.LogError("CardImageLoader: Unexpected Error.");
                            return null;
                        }
                    }
                    catch (System.Exception e)
                    {
                        throw e;
                    }
                }
            }
            catch (System.Exception e)
            {
                throw e;
            }
            finally
            {
                lockObj.Release();
                // Waiters still own this semaphore after Release. Keep the
                // per-code lock so a later request cannot bypass that queue.
            }
        }

        private static void EnsureCurrentCardCache(int epoch, System.Threading.CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (epoch != System.Threading.Volatile.Read(ref cardCacheEpoch))
                throw new System.OperationCanceledException("Card cache changed during loading.", token);
        }

        private static void EnsureCurrentVideoCache(int epoch, int videoEpoch, System.Threading.CancellationToken token)
        {
            EnsureCurrentCardCache(epoch, token);
            if (videoEpoch != System.Threading.Volatile.Read(ref videoCacheEpoch))
                throw new System.OperationCanceledException("Video card cache changed during loading.", token);
        }

        private static bool RemoveMatchingEntry<T>(System.Collections.Concurrent.ConcurrentDictionary<int, T> cache, int code, T entry)
        {
            return ((System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int, T>>)cache).Remove(new System.Collections.Generic.KeyValuePair<int, T>(code, entry));
        }

        private static void DestroyOwnedCardTexture(Texture2D texture)
        {
            if (texture == null) return;
            // Unknown-card previews borrow the shared fallback texture.
            if (Runtime.TextureManager.container != null && texture == Runtime.TextureManager.container.unknownCard.texture) return;
            Object.Destroy(texture);
        }

        // Acquire a ready static texture without an async frame or loading fade.
        // The caller owns one reference and must release it with forceTexture.
        internal static bool TryAcquireStaticCard(int code, out Texture texture)
        {
            texture = null;
            if (!cachedCards.TryGetValue(code, out var entry) || entry.LoadingTask != null || entry.Texture == null) return false;
            System.Threading.Interlocked.Increment(ref entry.ReferenceCount);
            texture = entry.Texture;
            return true;
        }

        public static void ReleaseCard(int code) => ReleaseCard(code, false);

        public static void ReleaseCard(int code, bool forceTexture)
        {
            // Static previews and live video cards may coexist for one code.
            // Release the cache corresponding to the load request, otherwise
            // leaving a duel can invalidate a client preview that is still open.
            if (!forceTexture && cachedVideoCards.TryGetValue(code, out var videoEntry))
            {
                var count = System.Threading.Interlocked.Decrement(ref videoEntry.ReferenceCount);
                if (count == 0 && !videoEntry.IsPersistent && cachedVideoCards.TryRemove(code, out _))
                {
                    if (videoEntry.Renderer != null)
                    {
                        videoEntry.Renderer.Dispose(); videos.Remove(videoEntry.Renderer);
                    }
                }
                return;
            }
            // 普通卡片释放
            if (cachedCards.TryGetValue(code, out var entry))
            {
                var newCount = System.Threading.Interlocked.Decrement(ref entry.ReferenceCount);
                if (newCount == 0 && !entry.IsPersistent)
                    if (cachedCards.TryRemove(code, out _))
                        DestroyOwnedCardTexture(entry.Texture);
                return;
            }

        }

        public static Texture2D LoadCardName(int code)
        {
            if (cachedCardNames.TryGetValue(code, out var tex))
                return tex;

            tex = InternalLoadCardName(code);
            cachedCardNames.TryAdd(code, tex);

            return tex;
        }

        public static void ClearCache()
        {
            System.Threading.Interlocked.Increment(ref cardCacheEpoch);
            // 普通卡片
            foreach (var card in cachedCards.Values)
                DestroyOwnedCardTexture(card.Texture);
            cachedCards.Clear();

            foreach (var cardNameTex in cachedCardNames.Values)
                Object.Destroy(cardNameTex);
            cachedCardNames.Clear();

            ClearArtVideos();
        }

        public static int GetCardReferenceCount(int code)
        {
            if (cachedCards.TryGetValue(code, out var entry))
                return entry.ReferenceCount;
            if (cachedVideoCards.TryGetValue(code, out var videoEntry))
                return videoEntry.ReferenceCount;
            return 0;
        }

        #endregion

        #region Private

        private class CacheEntry
        {
            public Texture2D Texture;
            public int ReferenceCount;
            public bool IsPersistent;
            public System.Threading.Tasks.Task<Texture2D> LoadingTask;
        }

        private static async System.Threading.Tasks.Task<Texture2D> InternalLoadArtAsync(
            int code,
            System.Threading.CancellationToken token)
        {
            await artSemaphore.WaitAsync(token);
            lastCardFoundArt = true;

            try
            {
                var result = await MDPro3.IO.ResourceManager.GetCardIllustAsync(code, token);
                if(result == null)
                {
                    var picture = await MDPro3.IO.ResourceManager.GetCardImageAsync(code, token);
                    if (picture != null)
                    {
                        try { result = CropCardToArt(picture, code); }
                        finally
                        {
                            if (!ReferenceEquals(result, picture)
                                && (Runtime.TextureManager.container == null
                                    || picture != Runtime.TextureManager.container.unknownCard.texture
                                        && picture != Runtime.TextureManager.container.unknownArt.texture))
                                Object.Destroy(picture);
                        }
                    }
                }

                lastCardFoundArt = !(result == null);
                return result;
            }
            finally
            {
                artSemaphore.Release();
            }
        }

        private static async UniTask<Texture2D> InternalLoadOverFrameAsync(int code, System.Threading.CancellationToken token)
        {
            await overFrameSemaphore.WaitAsync(token);

            try
            {
                var result = await MDPro3.IO.ResourceManager.GetCardOverFrameAsync(code, token);
                if (result != null)
                    result.name = "Opaque";
                if(result == null)
                    result = await MDPro3.IO.ResourceManager.GetCardOverFrame2Async(code, token);
                if(result != null)
                    result.name = "Transparent";

                if (result != null)
                {
                    var aspect = result.width / (float)result.height;
                    if (aspect > DEFAULT_ASPECT)
                        result = TextureProcessor.PadBottomToMatchAspect(result, DEFAULT_ASPECT);
                }
                return result;
            }
            finally
            {
                overFrameSemaphore.Release();
            }
        }

        private static async System.Threading.Tasks.Task<Texture2D> InternalLoadCardAsync(
            int code,
            System.Threading.CancellationToken token)
        {
            await UniTask.WaitUntil(() => Runtime.TextureManager.container != null, cancellationToken: token);
            await cardSemaphore.WaitAsync(token);
            lastCardRenderSucceed = true;
            bool artAcquired = false;

            try
            {

                var data = MDPro3.Duel.YGOSharp.CardsManager.Get(code, true);
                if (data.Id == 0)
                {
                    lastCardRenderSucceed = false;
                    return Runtime.TextureManager.container.unknownCard.texture;
                }

                var art = await LoadArtAsync(code, false, token).AttachExternalCancellation(token);
                artAcquired = art != null;
                if (token.IsCancellationRequested)
                    throw new System.OperationCanceledException(token);

                if (art == null)
                {
                    Debug.LogError($"Get null from ArtLoad for Card {data.Id}:");
                    art = Runtime.TextureManager.container.unknownArt.texture;
                }

                Texture2D overFrame = await LoadOverFrameAsync(code, token).AttachExternalCancellation(token);
                if (overFrame != null)
                {
                    CardRenderer.SetOverFrameOpaque(code, overFrame.name == "Opaque");
                    await UniTask.Yield(cancellationToken: token);
                }

                // Camera.Render and ReadPixels both use shared render state.
                // Restore the caller's target before any asynchronous work resumes.
                var previousTarget = RenderTexture.active;
                Texture2D returnValue = null;
                try
                {
                    if (!Runtime.Program.instance.cardRenderer.RenderCard(code, art, overFrame))
                    {
                        lastCardRenderSucceed = false;
                        return Runtime.TextureManager.container.unknownCard.texture;
                    }

                    var target = Runtime.Program.instance.cardRenderer.renderTexture;
                    RenderTexture.active = target;
                    returnValue = new Texture2D(target.width, target.height, TextureFormat.RGB24, true);
                    returnValue.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                    returnValue.Apply();
                    returnValue.name = "Card_" + code;
                    return returnValue;
                }
                catch
                {
                    if (returnValue != null)
                        Object.Destroy(returnValue);
                    throw;
                }
                finally
                {
                    RenderTexture.active = previousTarget;
                }
            }
            finally
            {
                if (artAcquired) ReleaseArt(code);
                cardSemaphore.Release();
            }
        }

        private static async UniTask<(Texture Texture, CardRenderer Renderer)> InternalLoadVideoCardAsync(
            int code,
            System.Threading.CancellationToken token,
            int epoch,
            int videoEpoch)
        {
            await UniTask.WaitUntil(() => Runtime.TextureManager.container != null, cancellationToken: token);
            await cardSemaphore.WaitAsync(token);
            lastCardRenderSucceed = true;
            bool artAcquired = false;

            try
            {
                EnsureCurrentVideoCache(epoch, videoEpoch, token);
                var data = MDPro3.Duel.YGOSharp.CardsManager.Get(code, true);
                if (data.Id == 0)
                {
                    lastCardRenderSucceed = false;
                    return (Runtime.TextureManager.container.unknownCard.texture, null);
                }
                var art = await LoadArtAsync(code, false, token).AttachExternalCancellation(token);
                artAcquired = art != null;
                EnsureCurrentVideoCache(epoch, videoEpoch, token);

                if (art == null)
                {
                    Debug.LogError($"Get null from ArtLoad for Card {data.Id}:");
                    art = Runtime.TextureManager.container.unknownArt.texture;
                }

                CardRenderer cardRenderer = (await UnityEngine.AddressableAssets.Addressables.InstantiateAsync("Prefab/CardRenderer.prefab")).GetComponent<CardRenderer>();
                try
                {
                    EnsureCurrentVideoCache(epoch, videoEpoch, token);
                    videos.Add(cardRenderer);
                    var texture = await cardRenderer.GetVideoCardAsync(code, token);
                    EnsureCurrentVideoCache(epoch, videoEpoch, token);
                    return (texture, cardRenderer);
                }
                catch
                {
                    videos.Remove(cardRenderer);
                    cardRenderer.Dispose();
                    throw;
                }
            }
            finally
            {
                if (artAcquired) ReleaseArt(code);
                cardSemaphore.Release();
            }
        }

        private static Texture2D InternalLoadCardName(int code)
        {
            var previousTarget = RenderTexture.active;
            Texture2D result = null;
            try
            {
                Runtime.Program.instance.cardRenderer.RenderName(code);
                var target = Runtime.Program.instance.cardRenderer.renderTexture;
                RenderTexture.active = target;
                result = new Texture2D(target.width, CARD_NAME_HEIGHT, TextureFormat.RGBA32, false);
                var rect = new Rect(0, target.height - CARD_NAME_HEIGHT, target.width, CARD_NAME_HEIGHT);
                result.ReadPixels(rect, 0, 0);
                result.Apply();
                result.wrapMode = TextureWrapMode.Clamp;
                return result;
            }
            catch
            {
                if (result != null)
                    Object.Destroy(result);
                throw;
            }
            finally
            {
                RenderTexture.active = previousTarget;
            }
        }

        private static Texture2D CropCardToArt(Texture2D pic, int code)
        {
            var data = MDPro3.Duel.YGOSharp.CardsManager.Get(code);
            if (code >= 120000000 && code < 130000000)
                return GetArtFromRushDuelCard(pic);
            else if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Pendulum))
                return GetArtFromPendulumCard(pic);
            else
                return GetArtFromCard(pic);
        }

        private static void CheckArtVideoConfig()
        {
            var config = MDPro3.Config.GetBool("VideoCard", true);
            if (!config)
                ClearArtVideos();
        }

        private static void ClearArtVideos()
        {
            System.Threading.Interlocked.Increment(ref videoCacheEpoch);
            var previousCancellation = videoCacheCancellation;
            videoCacheCancellation = new System.Threading.CancellationTokenSource();
            previousCancellation.Cancel();
            previousCancellation.Dispose();
            foreach (var video in cachedVideoCards.Values)
            {
                if (video.Renderer != null)
                    video.Renderer.Dispose();
                else if (video.Texture is Texture2D texture)
                    DestroyOwnedCardTexture(texture);
            }
            cachedVideoCards.Clear();
            videos.Clear();
        }

        #endregion

        #region Crop Texture

        private static Texture2D GetArtFromCard(Texture2D cardPic)
        {
            var startX = Mathf.CeilToInt(cardPic.width * 0.13f);
            var startY = Mathf.CeilToInt(cardPic.height * 0.3f);
            var width = Mathf.CeilToInt(cardPic.width * 0.87f);
            var height = Mathf.CeilToInt(cardPic.height * 0.81f);
            return TextureProcessor.GetCroppingTex(cardPic, startX, startY, width, height);
        }

        private static Texture2D GetArtFromPendulumCard(Texture2D cardPic)
        {
            var startX = Mathf.CeilToInt(cardPic.width * 0.067f);
            var startY = Mathf.CeilToInt(cardPic.height * 0.38f);
            var width = Mathf.CeilToInt(cardPic.width * 0.933f);
            var height = Mathf.CeilToInt(cardPic.height * 0.81f);
            return TextureProcessor.GetCroppingTex(cardPic, startX, startY, width, height);
        }

        private static Texture2D GetArtFromRushDuelCard(Texture2D cardPic)
        {
            var startX = Mathf.CeilToInt(cardPic.width * 0.052f);
            var startY = Mathf.CeilToInt(cardPic.height * 0.2815f);
            var width = Mathf.CeilToInt(cardPic.width * 0.948f);
            var height = Mathf.CeilToInt(cardPic.height * 0.90f);
            return TextureProcessor.GetCroppingTex(cardPic, startX, startY, width, height);
        }

        #endregion

        #region OverFrame

        public static Texture2D GetOverFrame(int code)
        {
            if(cachedOverFrames.ContainsKey(code))
                return cachedOverFrames[code];
            return null;
        }

        #endregion

    }
}
