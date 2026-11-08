using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using UnityEngine;

using UnityEngine.UI;


using Cysharp.Threading.Tasks;


namespace YuGiOhFight.Client.UI
{
    public class SelectionToggle_AppearanceItem : SelectionToggle
    {
        #region Elements

        private CanvasGroup m_CG;
        private CanvasGroup CG =>
            m_CG = m_CG != null ? m_CG
            : GetComponent<CanvasGroup>();

        private const string LABEL_RIMG_PROTECTOR = "Protector";
        private RawImage m_Protector;
        private RawImage Protector =>
            m_Protector = m_Protector != null ? m_Protector
            : Manager.GetElement<RawImage>(LABEL_RIMG_PROTECTOR);


        private const string LABEL_IMG_WALLPAPER_BG = "WallpaperBG";
        private Image m_WallpaperBG;
        private Image WallpaperBG =>
            m_WallpaperBG = m_WallpaperBG != null ? m_WallpaperBG
            : Manager.GetElement<Image>(LABEL_IMG_WALLPAPER_BG);

        private GridLayoutGroup m_Grid;
        private GridLayoutGroup Grid =>
            m_Grid = m_Grid != null ? m_Grid
            : Runtime.Program.instance.appearance.GetUI<ServantUI.AppearanceUI>().ScrollRect
            .content.GetComponent<GridLayoutGroup>();

        #endregion

        public int itemID;
        public string itemName;
        public string description;
        public string path;
        private bool loaded;

        private Coroutine refreshCoroutine;
        private Coroutine hideCoroutine;

        private Image premiumOverlayIcon;
        private Coroutine premiumCrossfadeCoroutine;
        private const float CrossfadeHoldSeconds = 3.0f;
        private const float CrossfadeFadeSeconds = 1.0f;
        private static readonly Dictionary<int, Sprite> cachedArtSprites = new();

        protected override void Awake()
        {
            base.Awake();
            HoverOff();
            exclusiveToggle = true;
            canToggleOffSelf = false;
            manuallySetNavigation = false;
        }

        public void Refresh()
        {
            _ = RefreshAsync(this.GetCancellationTokenOnDestroy());
        }

        private async UniTask RefreshAsync(CancellationToken cancellationToken)
        {
            try
            {
                for (int i = 0; i < index; i++)
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                if (cancellationToken.IsCancellationRequested || this == null)
                    return;

                if (path.StartsWith("Protector"))
                {
                    Icon.gameObject.SetActive(false);
                    Protector.material = await Runtime.ABLoader.LoadProtectorMaterial(itemID.ToString(), destroyCancellationToken);
                    Protector.color = Color.white;
                }
                else if (path.Length > 0)
                {
                    Icon.sprite = await Runtime.Program.items.LoadItemIconAsync(itemID.ToString(), Presentation.Assets.Items.ItemType.Unknown);
                    if (cancellationToken.IsCancellationRequested || this == null || Manager == null)
                        return;
                    if (path.StartsWith("ProfileFrame"))
                    {
                        Icon.rectTransform.localScale = Vector3.one * 0.8f;
                        Icon.material = await Runtime.ABLoader.LoadFrameMaterial(itemID.ToString());
                        if (cancellationToken.IsCancellationRequested || this == null)
                            return;
                        Icon.material.SetTexture("_ProfileFrameTex", Icon.sprite.texture);
                        Icon.sprite = Runtime.TextureManager.container.black;
                    }
                    else if (path.StartsWith("DeckCase"))
                    {
                        Icon.transform.localPosition = new Vector3(0f, 15f, 0f);
                    }
                    else if (path.StartsWith("WallPaperIcon"))
                    {
                        WallpaperBG.gameObject.SetActive(true);
                    }
                    Icon.color = Color.white;
                    Protector.gameObject.SetActive(false);
                }
                else //CrossDuel Mate
                {
                    if(!cachedArtSprites.TryGetValue(itemID, out var sprite))
                    {
                        var art = await Presentation.CardImageLoader.LoadArtAsync(itemID, true, cancellationToken);
                        if (cancellationToken.IsCancellationRequested || this == null)
                            return;
                        sprite = Runtime.TextureManager.Texture2Sprite(art);
                        cachedArtSprites.TryAdd(itemID, sprite);
                    }

                    Icon.sprite = sprite;
                    Icon.color = Color.white;
                    Protector.gameObject.SetActive(false);
                }

                if (path.StartsWith("ProfileIcon") && !cancellationToken.IsCancellationRequested && this != null)
                    Icon.material = Controllers.Appearance.matForFace;

                loaded = true;
                StartPremiumCrossfade();
                PushLoadedPreviewToDetail();
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                refreshCoroutine = null;
            }
        }

        protected override void CallHoverOnEvent()
        {
            base.CallHoverOnEvent();
            Runtime.Program.instance.appearance.GetUI<ServantUI.AppearanceUI>().SetHoverText(itemName);
        }

        protected override void CallToggleOnEvent()
        {
            base.CallToggleOnEvent();
            CallHoverOnEvent();

            AppearanceDetail detail = Runtime.Program.instance.appearance.GetUI<ServantUI.AppearanceUI>().Detail;
            detail.SetItem(itemID, itemName, description, path == string.Empty, path, GetPreviewSprite(), GetPreviewMaterial());

            Runtime.Program.instance.appearance.GetUI<ServantUI.AppearanceUI>().SetHoverText(itemName);
            Runtime.Program.instance.appearance.lastSelectedItem = this;
            Runtime.Program.instance.currentServant.lastSelectable = Selectable;
            GetSelectable().Select();

            if (Controllers.Appearance.condition == Controllers.Appearance.Condition.DeckEditor)
            {
                if (path.StartsWith("DeckCase"))
                {
                    if(Controllers.DeckEditor.Deck.Case != itemID)
                    {
                        Controllers.DeckEditor.Deck.Case = itemID;
                        Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().DeckView.SetDirty(true);
                        Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().IconCase.sprite = Icon.sprite;
                    }
                }
                else if (path.StartsWith("Protector"))
                {
                    if (Controllers.DeckEditor.Deck.Protector != itemID)
                    {
                        Controllers.DeckEditor.Deck.Protector = itemID;
                        Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().DeckView.SetDirty(true);
                        Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().IconProtector.material = Protector.material;
                    }
                }
                else if (path.StartsWith("FieldIcon"))
                {
                    if (Controllers.DeckEditor.Deck.Field != itemID)
                    {
                        Controllers.DeckEditor.Deck.Field = itemID;
                        Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().DeckView.SetDirty(true);
                        Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().IconField.sprite = Icon.sprite;
                    }
                }
                else if (path.StartsWith("FieldObj"))
                {
                    if (Controllers.DeckEditor.Deck.Grave != itemID)
                    {
                        Controllers.DeckEditor.Deck.Grave = itemID;
                        Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().DeckView.SetDirty(true);
                        Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().IconGrave.sprite = Icon.sprite;
                    }
                }
                else if (path.StartsWith("FieldAvatarBase"))
                {
                    if (Controllers.DeckEditor.Deck.Stand != itemID)
                    {
                        Controllers.DeckEditor.Deck.Stand = itemID;
                        Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().DeckView.SetDirty(true);
                        Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().IconStand.sprite = Icon.sprite;
                    }
                }
                else
                {
                    var normalizedMateId = Duel.PremiumMateRules.GetBaseMateId(itemID);
                    if (Controllers.DeckEditor.Deck.Mate != normalizedMateId)
                    {
                        Controllers.DeckEditor.Deck.Mate = normalizedMateId;
                        Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().DeckView.SetDirty(true);
                        Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().IconMate.sprite = Icon.sprite;
                    }
                }
            }
            else
            {
                if (ServantUI.AppearanceUI.currentContent == "Wallpaper")
                    MDPro3.Config.Set("Wallpaper", itemID.ToString());
                else if (ServantUI.AppearanceUI.currentContent == "Mate")
                    MDPro3.Config.Set(Controllers.Appearance.condition.ToString() + ServantUI.AppearanceUI.currentContent + Controllers.Appearance.player,
                        Duel.PremiumMateRules.GetBaseMateId(itemID).ToString());
                else
                    MDPro3.Config.Set(Controllers.Appearance.condition.ToString() + ServantUI.AppearanceUI.currentContent + Controllers.Appearance.player, itemID.ToString());

                if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Duel
                    && Controllers.Appearance.player == "0"
                    && MDPro3.Config.GetBool("OverrideDeckAppearance", false)
                    && Runtime.Program.instance.room != null
                    && Runtime.Program.instance.room.showing
                    && Controllers.RoomServant.SelfType < 4)
                    Runtime.Network.TcpHelper.CtosMessage_UpdateAppearanceFromCurrentDeck();
            }

            StartCoroutine(ConfigSetAsync());
        }

        private IEnumerator ConfigSetAsync()
        {
            while (!loaded)
                yield return null;

            if (!Icon.gameObject.activeSelf)//Protector
            {
                if (Controllers.Appearance.player == "0")
                {
                    if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Duel)
                        Controllers.Appearance.duelProtector0 = Protector.material;
                    else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Watch)
                        Controllers.Appearance.watchProtector0 = Protector.material;
                    else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Replay)
                        Controllers.Appearance.replayProtector0 = Protector.material;
                }
                else if (Controllers.Appearance.player == "1")
                {
                    if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Duel)
                        Controllers.Appearance.duelProtector1 = Protector.material;
                    else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Watch)
                        Controllers.Appearance.watchProtector1 = Protector.material;
                    else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Replay)
                        Controllers.Appearance.replayProtector1 = Protector.material;
                }
                else if (Controllers.Appearance.player == "0Tag")
                {
                    if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Duel)
                        Controllers.Appearance.duelProtector0Tag = Protector.material;
                    else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Watch)
                        Controllers.Appearance.watchProtector0Tag = Protector.material;
                    else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Replay)
                        Controllers.Appearance.replayProtector0Tag = Protector.material;
                }
                else if (Controllers.Appearance.player == "1Tag")
                {
                    if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Duel)
                        Controllers.Appearance.duelProtector1Tag = Protector.material;
                    else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Watch)
                        Controllers.Appearance.watchProtector1Tag = Protector.material;
                    else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Replay)
                        Controllers.Appearance.replayProtector1Tag = Protector.material;
                }
            }
            else
            {
                if (path.StartsWith("ProfileIcon"))
                {
                    if (Controllers.Appearance.player == "0")
                    {
                        if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Duel)
                            Controllers.Appearance.duelFace0 = Icon.sprite;
                        else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Watch)
                            Controllers.Appearance.watchFace0 = Icon.sprite;
                        else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Replay)
                            Controllers.Appearance.replayFace0 = Icon.sprite;
                    }
                    else if (Controllers.Appearance.player == "1")
                    {
                        if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Duel)
                            Controllers.Appearance.duelFace1 = Icon.sprite;
                        else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Watch)
                            Controllers.Appearance.watchFace1 = Icon.sprite;
                        else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Replay)
                            Controllers.Appearance.replayFace1 = Icon.sprite;
                    }
                    else if (Controllers.Appearance.player == "0Tag")
                    {
                        if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Duel)
                            Controllers.Appearance.duelFace0Tag = Icon.sprite;
                        else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Watch)
                            Controllers.Appearance.watchFace0Tag = Icon.sprite;
                        else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Replay)
                            Controllers.Appearance.replayFace0Tag = Icon.sprite;
                    }
                    else if (Controllers.Appearance.player == "1Tag")
                    {
                        if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Duel)
                            Controllers.Appearance.duelFace1Tag = Icon.sprite;
                        else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Watch)
                            Controllers.Appearance.watchFace1Tag = Icon.sprite;
                        else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Replay)
                            Controllers.Appearance.replayFace1Tag = Icon.sprite;
                    }
                }
                else if (path.StartsWith("ProfileFrame"))
                {
                    if (Controllers.Appearance.player == "0")
                    {
                        if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Duel)
                            Controllers.Appearance.duelFrameMat0 = Icon.material;
                        else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Watch)
                            Controllers.Appearance.watchFrameMat0 = Icon.material;
                        else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Replay)
                            Controllers.Appearance.replayFrameMat0 = Icon.material;
                    }
                    else if (Controllers.Appearance.player == "1")
                    {
                        if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Duel)
                            Controllers.Appearance.duelFrameMat1 = Icon.material;
                        else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Watch)
                            Controllers.Appearance.watchFrameMat1 = Icon.material;
                        else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Replay)
                            Controllers.Appearance.replayFrameMat1 = Icon.material;
                    }
                    else if (Controllers.Appearance.player == "0Tag")
                    {
                        if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Duel)
                            Controllers.Appearance.duelFrameMat0Tag = Icon.material;
                        else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Watch)
                            Controllers.Appearance.watchFrameMat0Tag = Icon.material;
                        else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Replay)
                            Controllers.Appearance.replayFrameMat0Tag = Icon.material;
                    }
                    else if (Controllers.Appearance.player == "1Tag")
                    {
                        if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Duel)
                            Controllers.Appearance.duelFrameMat1Tag = Icon.material;
                        else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Watch)
                            Controllers.Appearance.watchFrameMat1Tag = Icon.material;
                        else if (Controllers.Appearance.condition == Controllers.Appearance.Condition.Replay)
                            Controllers.Appearance.replayFrameMat1Tag = Icon.material;
                    }
                }
            }
        }

        protected override void OnClick()
        {
            Runtime.AudioManager.PlaySE(SoundLabelClick);
            SetToggleOn();
            Runtime.Program.instance.currentServant.lastSelectable = Selectable;
        }

        protected override int GetButtonsCount()
        {
            return Runtime.Program.instance.appearance.GetUI<ServantUI.AppearanceUI>().GetCurrentGenreCount();
        }

        protected override int GetColumnsCount()
        {
            return Grid.Size().x;
        }

        public void Hide()
        {
            if (hideCoroutine != null || !gameObject.activeSelf)
                return;
            hideCoroutine = StartCoroutine(HideAsync());
            StopPremiumCrossfade();

            GetComponent<LayoutElement>().ignoreLayout = true;
            GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }

        private IEnumerator HideAsync()
        {
            CG.alpha = 0f;
            CG.blocksRaycasts = false;
            while (!loaded)
                yield return null;
            hideCoroutine = null;
            gameObject.SetActive(false);
        }

        public void Show()
        {
            if(hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }
            gameObject.SetActive(true);
            CG.alpha = 1f;
            CG.blocksRaycasts = true;

            GetComponent<LayoutElement>().ignoreLayout = false;
            transform.SetSiblingIndex(index);
            StartPremiumCrossfade();
        }

        public void Dispose()
        {
            StopPremiumCrossfade();

            if(refreshCoroutine != null)
                StopCoroutine(refreshCoroutine);

            if (hideCoroutine != null)
                StopCoroutine(hideCoroutine);

            Destroy(gameObject);
        }

        private Sprite GetPreviewSprite()
        {
            if (!loaded)
                return null;

            if (path.StartsWith("Protector") || path.StartsWith("ProfileFrame"))
                return null;

            return Icon != null ? Icon.sprite : null;
        }

        private Material GetPreviewMaterial()
        {
            if (!loaded || !path.StartsWith("Protector"))
                return null;

            return Protector.material;
        }

        private void PushLoadedPreviewToDetail()
        {
            if (!isOn || Runtime.Program.instance?.appearance == null)
                return;

            Runtime.Program.instance.appearance.GetUI<ServantUI.AppearanceUI>().Detail.ApplyLoadedPreview(
                itemID,
                GetPreviewSprite(),
                GetPreviewMaterial());
        }

        #region Premium Mate Crossfade

        private void StartPremiumCrossfade()
        {
            StopPremiumCrossfade();
            if (!loaded)
                return;
            if (ServantUI.AppearanceUI.currentContent != "Mate")
                return;
            if (!Duel.PremiumMateRules.IsPremiumBaseId(itemID))
                return;
            if (Icon == null || !Icon.gameObject.activeSelf)
                return;

            premiumCrossfadeCoroutine = StartCoroutine(PremiumCrossfadeAsync());
        }

        private void StopPremiumCrossfade()
        {
            if (premiumCrossfadeCoroutine != null)
            {
                StopCoroutine(premiumCrossfadeCoroutine);
                premiumCrossfadeCoroutine = null;
            }
            if (premiumOverlayIcon != null)
            {
                Destroy(premiumOverlayIcon.gameObject);
                premiumOverlayIcon = null;
            }
        }

        private Image CreateOverlayIcon()
        {
            var overlayGo = new GameObject("PremiumOverlay");
            overlayGo.transform.SetParent(Icon.transform.parent, false);
            overlayGo.transform.SetSiblingIndex(Icon.transform.GetSiblingIndex() + 1);

            var overlayImg = overlayGo.AddComponent<Image>();
            overlayImg.raycastTarget = false;
            overlayImg.preserveAspect = Icon.preserveAspect;
            overlayImg.type = Icon.type;
            overlayImg.maskable = Icon.maskable;

            var overlayRt = overlayImg.rectTransform;
            var iconRt = Icon.rectTransform;
            overlayRt.anchorMin = iconRt.anchorMin;
            overlayRt.anchorMax = iconRt.anchorMax;
            overlayRt.pivot = iconRt.pivot;
            overlayRt.anchoredPosition = iconRt.anchoredPosition;
            overlayRt.sizeDelta = iconRt.sizeDelta;
            overlayRt.localScale = iconRt.localScale;
            overlayRt.localRotation = iconRt.localRotation;

            var c = Color.white;
            c.a = 0f;
            overlayImg.color = c;

            return overlayImg;
        }

        private IEnumerator PremiumCrossfadeAsync()
        {
            if (!Duel.PremiumMateRules.TryGetRuleByBaseId(itemID, out Duel.PremiumMateRule rule))
                yield break;

            Sprite subSprite = null;
            foreach (var variantId in rule.VariantIds)
            {
                var task = Runtime.Program.items.TryLoadItemIconAsync(variantId.ToString(), Presentation.Assets.Items.ItemType.Mate);
                while (task.Status == UniTaskStatus.Pending)
                    yield return null;

                subSprite = task.GetAwaiter().GetResult();
                if (subSprite != null)
                    break;
            }

            if (subSprite == null || this == null || Icon == null)
                yield break;

            premiumOverlayIcon = CreateOverlayIcon();
            premiumOverlayIcon.sprite = subSprite;

            // Icon shows base (alpha=1), overlay shows sub (alpha=0) initially.
            // Crossfade loop: hold → fade overlay in → hold → fade overlay out → repeat.
            while (true)
            {
                // Hold on base icon
                yield return new WaitForSecondsRealtime(CrossfadeHoldSeconds);

                // Fade in overlay (base → sub)
                yield return FadeOverlay(0f, 1f, CrossfadeFadeSeconds);

                // Hold on sub icon
                yield return new WaitForSecondsRealtime(CrossfadeHoldSeconds);

                // Fade out overlay (sub → base)
                yield return FadeOverlay(1f, 0f, CrossfadeFadeSeconds);
            }
        }

        private IEnumerator FadeOverlay(float fromAlpha, float toAlpha, float duration)
        {
            if (premiumOverlayIcon == null)
                yield break;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t); // smoothstep
                var alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
                if (premiumOverlayIcon != null)
                {
                    var c = premiumOverlayIcon.color;
                    c.a = alpha;
                    premiumOverlayIcon.color = c;
                }
                yield return null;
            }

            if (premiumOverlayIcon != null)
            {
                var c = premiumOverlayIcon.color;
                c.a = toAlpha;
                premiumOverlayIcon.color = c;
            }
        }

        #endregion
    }
}
