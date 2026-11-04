using Cysharp.Threading.Tasks;
using Ionic.Zip;
using MDPro3.Duel.YGOSharp;
using MDPro3.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace MDPro3.Utility
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
            public Task<(Texture Texture, CardRenderer Renderer)> LoadingTask;
        }

        private const int CARD_NAME_HEIGHT = 175;
        public const float DEFAULT_ASPECT = 723 / 1054f;

        private static readonly ConcurrentDictionary<int, CacheEntry> cachedArts = new();
        private static readonly ConcurrentDictionary<int, CacheEntry> cachedCards = new();
        private static readonly ConcurrentDictionary<int, Texture2D> cachedCardNames = new();
        private static readonly ConcurrentDictionary<int, Texture2D> cachedOverFrames = new();
        private static readonly ConcurrentDictionary<int, VideoCacheEntry> cachedVideoCards = new();

        private static readonly ConcurrentDictionary<int, SemaphoreSlim> artLoadingLocks = new();
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> overFrameLoadingLocks = new();
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> cardLoadingLocks = new();

        private static readonly List<CardRenderer> videos = new();

        public static bool lastCardFoundArt;
        public static bool lastCardRenderSucceed;

        private static SemaphoreSlim artSemaphore;
        private static SemaphoreSlim overFrameSemaphore;
        private static SemaphoreSlim cardSemaphore;
        private static int maxLoads;

        #endregion

        #region 初始化

        public static void Initialize()
        {
            maxLoads = GetOptimalConcurrency();

            artSemaphore = new SemaphoreSlim(maxLoads, maxLoads);
            overFrameSemaphore = new SemaphoreSlim(maxLoads, maxLoads);
            cardSemaphore = new SemaphoreSlim(maxLoads, maxLoads);

            SystemEvent.OnVideoCardConfigChange -= CheckArtVideoConfig;
            SystemEvent.OnVideoCardConfigChange += CheckArtVideoConfig;
        }

        public static async UniTask InitializeAsync()
        {
            maxLoads = GetOptimalConcurrency();

            artSemaphore = new SemaphoreSlim(maxLoads, maxLoads);
            overFrameSemaphore = new SemaphoreSlim(maxLoads, maxLoads);
            cardSemaphore = new SemaphoreSlim(maxLoads, maxLoads);

            SystemEvent.OnVideoCardConfigChange -= CheckArtVideoConfig;
            SystemEvent.OnVideoCardConfigChange += CheckArtVideoConfig;

            await UniTask.CompletedTask;
        }

        private static int GetOptimalConcurrency()
        {
            if (DeviceInfo.OnMobile())
                return 1;
            else
                return 2;
        }

        #endregion

        #region API

        public static async UniTask<Texture2D> LoadArtAsync(
            int code,
            bool persistent = false,
            CancellationToken token = default)
        {
            var lockObj = artLoadingLocks.GetOrAdd(code, _ => new SemaphoreSlim(1, 1));
            await lockObj.WaitAsync(token);

            try
            {
                if (cachedArts.TryGetValue(code, out var entry))
                {

                    if (entry.LoadingTask != null)
                        await entry.LoadingTask.AsUniTask().AttachExternalCancellation(token);
                    if (entry.Texture != null)
                    {

                        Interlocked.Increment(ref entry.ReferenceCount);
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
                    catch(OperationCanceledException ex)
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
            catch (Exception e)
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

            var newCount = Interlocked.Decrement(ref entry.ReferenceCount);

            if(newCount < 0)
                Debug.LogError($"Art reference count for code {code} is less than zero.");

            if (newCount == 0 && !entry.IsPersistent)
                if (cachedArts.TryRemove(code, out _))
                    UnityEngine.Object.Destroy(entry.Texture);
        }

        public static async UniTask<Texture2D> LoadOverFrameAsync(
            int code,
            CancellationToken token = default)
        {
            var lockObj = overFrameLoadingLocks.GetOrAdd(code, _ => new SemaphoreSlim(1, 1));
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
            catch (Exception e)
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
            CancellationToken token = default,
            bool forceTexture = false)
        {
            if(ResourceManager.CardHasOverFrame(code))
                persistent = true;

            var lockObj = cardLoadingLocks.GetOrAdd(code, _ => new SemaphoreSlim(1, 1));
            await lockObj.WaitAsync(token);
            try
            {
                if (!CardRenderer.CardHasVideoArt(code) || forceTexture)
                {
                    if (cachedCards.TryGetValue(code, out var entry))
                    {
                        if (entry.LoadingTask != null)
                            await entry.LoadingTask.AsUniTask().AttachExternalCancellation(token);
                        if (entry.Texture != null)
                        {
                            Interlocked.Increment(ref entry.ReferenceCount);
                            entry.IsPersistent |= persistent;
                        }
                        else
                            Debug.LogError($"Card texture is null for code {code}");
                        return entry.Texture;
                    }

                    CacheEntry newEntry = new()
                    {
                        LoadingTask = InternalLoadCardAsync(code, token)
                    };

                    if (cachedCards.TryAdd(code, newEntry))
                    {
                        newEntry.ReferenceCount = 1;
                        newEntry.IsPersistent = persistent;
                        try
                        {
                            newEntry.Texture = await newEntry.LoadingTask;
                        }
                        catch (OperationCanceledException ex)
                        {
                            cachedCards.TryRemove(code, out _);
                            throw ex;
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
                    try
                    {
                        if (cachedVideoCards.TryGetValue(code, out var entry))
                        {
                            if (entry.LoadingTask != null)
                                await entry.LoadingTask.AsUniTask().AttachExternalCancellation(token);
                            if (entry.Texture != null)
                            {
                                Interlocked.Increment(ref entry.ReferenceCount);
                                entry.IsPersistent |= persistent;
                            }
                            else
                                Debug.LogError($"Video card texture is null for code {code}");
                            return entry.Texture;
                        }

                        VideoCacheEntry newEntry = new()
                        {
                            LoadingTask = InternalLoadVideoCardAsync(code, token).AsTask()
                        };

                        if (cachedVideoCards.TryAdd(code, newEntry))
                        {
                            newEntry.ReferenceCount = 1;
                            newEntry.IsPersistent = persistent;
                            try
                            {
                                var result = await newEntry.LoadingTask;
                                newEntry.Texture = result.Texture;
                                newEntry.Renderer = result.Renderer;
                            }
                            catch (OperationCanceledException ex)
                            {
                                cachedVideoCards.TryRemove(code, out _);
                                throw ex;
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
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                lockObj.Release();
                cardLoadingLocks.TryRemove(code, out _);
            }
        }

        // Acquire a ready static texture without an async frame or loading fade.
        // The caller owns one reference and must release it with forceTexture.
        internal static bool TryAcquireStaticCard(int code, out Texture texture)
        {
            texture = null;
            if (!cachedCards.TryGetValue(code, out var entry) || entry.LoadingTask != null || entry.Texture == null) return false;
            Interlocked.Increment(ref entry.ReferenceCount);
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
                var count = Interlocked.Decrement(ref videoEntry.ReferenceCount);
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
                var newCount = Interlocked.Decrement(ref entry.ReferenceCount);
                if (newCount == 0 && !entry.IsPersistent)
                    if (cachedCards.TryRemove(code, out _))
                        UnityEngine.Object.Destroy(entry.Texture);
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
            // 普通卡片
            foreach (var card in cachedCards.Values)
                UnityEngine.Object.Destroy(card.Texture);
            cachedCards.Clear();

            foreach (var cardNameTex in cachedCardNames.Values)
                UnityEngine.Object.Destroy(cardNameTex);
            cachedCardNames.Clear();

            // 视频卡
            foreach (var video in cachedVideoCards.Values)
            {
                if (video.Renderer != null)
                    video.Renderer.Dispose();
            }
            cachedVideoCards.Clear();
            videos.Clear();
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
            public Task<Texture2D> LoadingTask;
        }

        private static async Task<Texture2D> InternalLoadArtAsync(
            int code,
            CancellationToken token)
        {
            await artSemaphore.WaitAsync(token);
            lastCardFoundArt = true;

            try
            {
                var result = await ResourceManager.GetCardIllustAsync(code, token);
                if(result == null)
                {
                    result = await ResourceManager.GetCardImageAsync(code, token);
                    if(result != null)
                        result = CropCardToArt(result, code);
                }

                lastCardFoundArt = !(result == null);
                return result;
            }
            finally 
            {
                artSemaphore.Release(); 
            }
        }

        private static async UniTask<Texture2D> InternalLoadOverFrameAsync(int code, CancellationToken token)
        {
            await overFrameSemaphore.WaitAsync(token);

            try
            {
                var result = await ResourceManager.GetCardOverFrameAsync(code, token);
                if (result != null)
                    result.name = "Opaque";
                if(result == null)
                    result = await ResourceManager.GetCardOverFrame2Async(code, token);
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

        private static async Task<Texture2D> InternalLoadCardAsync(
            int code,
            CancellationToken token)
        {
            await UniTask.WaitUntil(() => TextureManager.container != null, cancellationToken: token);
            await cardSemaphore.WaitAsync(token);
            lastCardRenderSucceed = true;

            try
            {

                var data = CardsManager.Get(code, true);
                if (data.Id == 0)
                {
                    lastCardRenderSucceed = false;
                    return TextureManager.container.unknownCard.texture;
                }

                var art = await LoadArtAsync(code, false, token).AttachExternalCancellation(token);
                if (token.IsCancellationRequested)
                    throw new OperationCanceledException(token);

                if (art == null)
                {
                    Debug.LogError($"Get null from ArtLoad for Card {data.Id}:");
                    art = TextureManager.container.unknownArt.texture;
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
                    if (!Program.instance.cardRenderer.RenderCard(code, art, overFrame))
                    {
                        lastCardRenderSucceed = false;
                        return TextureManager.container.unknownCard.texture;
                    }

                    var target = Program.instance.cardRenderer.renderTexture;
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
                        UnityEngine.Object.Destroy(returnValue);
                    throw;
                }
                finally
                {
                    RenderTexture.active = previousTarget;
                    ReleaseArt(code);
                }
            }
            finally { cardSemaphore.Release(); }
        }

        private static async UniTask<(Texture Texture, CardRenderer Renderer)> InternalLoadVideoCardAsync(
            int code,
            CancellationToken token)
        {
            await UniTask.WaitUntil(() => TextureManager.container != null, cancellationToken: token);
            await cardSemaphore.WaitAsync(token);
            lastCardRenderSucceed = true;

            try
            {
                var data = CardsManager.Get(code, true);
                if (data.Id == 0)
                {
                    lastCardRenderSucceed = false;
                    return (TextureManager.container.unknownCard.texture, null);
                }
                var art = await LoadArtAsync(code, false, token).AttachExternalCancellation(token);
                if (token.IsCancellationRequested)
                    throw new OperationCanceledException(token);

                if (art == null)
                {
                    Debug.LogError($"Get null from ArtLoad for Card {data.Id}:");
                    art = TextureManager.container.unknownArt.texture;
                }

                var cardRenderer = (await Addressables.InstantiateAsync("Prefab/CardRenderer.prefab")).GetComponent<CardRenderer>();
                videos.Add(cardRenderer);
                try
                {
                    var texture = await cardRenderer.GetVideoCardAsync(code);
                    return (texture, cardRenderer);
                }
                catch
                {
                    videos.Remove(cardRenderer);
                    cardRenderer.Dispose();
                    throw;
                }
            }
            finally { cardSemaphore.Release(); }
        }

        private static Texture2D InternalLoadCardName(int code)
        {
            var previousTarget = RenderTexture.active;
            Texture2D result = null;
            try
            {
                Program.instance.cardRenderer.RenderName(code);
                var target = Program.instance.cardRenderer.renderTexture;
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
                    UnityEngine.Object.Destroy(result);
                throw;
            }
            finally
            {
                RenderTexture.active = previousTarget;
            }
        }

        private static Texture2D CropCardToArt(Texture2D pic, int code)
        {
            var data = CardsManager.Get(code);
            if (code >= 120000000 && code < 130000000)
                return GetArtFromRushDuelCard(pic);
            else if (data.HasType(CardType.Pendulum))
                return GetArtFromPendulumCard(pic);
            else
                return GetArtFromCard(pic);
        }

        private static void CheckArtVideoConfig()
        {
            var config = Config.GetBool("VideoCard", true);
            if (!config)
                ClearArtVideos();
        }

        private static void ClearArtVideos()
        {
            foreach (var video in cachedVideoCards.Values)
            {
                if (video.Texture != null)
                    UnityEngine.Object.Destroy(video.Texture);
                if (video.Renderer != null)
                    video.Renderer.Dispose();
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
