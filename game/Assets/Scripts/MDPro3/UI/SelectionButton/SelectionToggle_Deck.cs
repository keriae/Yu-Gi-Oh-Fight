using UnityEngine;
using static YgomSystem.UI.ColorContainer;
using YgomSystem.UI;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Threading;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;
using MDPro3.Utility;
using Cysharp.Threading.Tasks;

namespace MDPro3.UI
{
    public class SelectionToggle_Deck : SelectionToggle_ScrollRectItem
    {
        [HideInInspector] public string deckName;
        [HideInInspector] public string sourceKey;
        [HideInInspector] public int coverCard;
        [HideInInspector] public bool isReadOnly;
        private bool? createButtonOverride;
        public bool isCreateButton
        {
            get => createButtonOverride ?? (index == 0 && string.IsNullOrEmpty(deckName));
            set => createButtonOverride = value;
        }

        private RawImage coverImage;
        private RectTransform coverArea;
        private AspectRatioFitter coverFitter;
        private GameObject lockBadge;
        private CancellationTokenSource coverCts;
        internal DeckCoverArtCache coverCache;
        private DeckCoverArtCache privateCoverCache;
        private DeckCoverArtCache.Entry coverEntry;
        private int loadedCoverCard;
        private int refreshVersion;
        [HideInInspector] public int deckCase;
        [HideInInspector] public int card0;
        [HideInInspector] public int card1;
        [HideInInspector] public int card2;
        [HideInInspector] public string protector;
        [HideInInspector] public bool toggleMode;
        protected bool pickuping;
        protected bool forcedPickup;
        protected IEnumerator enumeratorCase;
        private readonly List<Tweener> pickupTweens = new();
        private readonly List<Tweener> pickdownTweens = new();

        protected override void Awake()
        {
            base.Awake();
            exclusiveToggle = false;
            manuallySetNavigation = false;
            simpleMove = false;
            selectedWhenHover = true;
            ApplyHideToggle();
            ApplyHidePickup(true);
        }

        protected override void OnDisable()
        {
            KillPickupTweens();
            CancelCover();
            ReleaseCover();
            refreshVersion++;
            SelectionEdgeHighlight.SetSelected(this, false);
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            CancelCover();
            ReleaseCover();
            privateCoverCache?.Dispose();
            base.OnDestroy();
        }

        public override void Refresh()
        {
            refreshVersion++;
            cts?.Cancel();
            if (isCreateButton)
            {
                Manager.GetElement("DeckCaseIcon").SetActive(false);
                Manager.GetElement("TextDeckName").SetActive(false);
                Manager.GetElement("IconAddDeck").SetActive(true);
                Manager.GetElement("SelectedStateToggle").SetActive(false);

                SoundLabelPointerEnter = string.Empty;
                SoundLabelSelectedGamePad = "SE_MENU_OVERLAP_02";
            }
            else
            {
                Manager.GetElement("DeckCaseIcon").SetActive(true);
                Manager.GetElement("TextDeckName").SetActive(true);
                Manager.GetElement("IconAddDeck").SetActive(false);
                Manager.GetElement("SelectedStateToggle").SetActive(toggleMode);

                SoundLabelPointerEnter = "SE_DECK_CARD_SELECT";
                SoundLabelSelectedGamePad = "SE_DECK_CARD_SELECT";
                Manager.GetElement("IconOn").SetActive(isOn);

                Manager.GetElement<TextMeshProUGUI>("TextDeckName").text = deckName;

                if (forcedPickup && !pickuping)
                {
                    ShowPickup(true);
                }
            }

            RefreshCover();
            SelectionEdgeHighlight.SetSelected(this, selected || hovering);
            _ = RefreshDeckCaseAsync();

            refreshed = false;
            if (pickuping)
                StartRefresh();
        }

        private void RefreshCover()
        {
            var hasCover = !isCreateButton && coverCard > 0;
            if (hasCover)
                EnsureCover();
            if (coverImage != null)
                coverArea.gameObject.SetActive(hasCover);
            if (lockBadge != null)
                lockBadge.SetActive(!isCreateButton && isReadOnly);
            Manager.GetElement("DeckCaseIcon").SetActive(!isCreateButton && !hasCover);

            if (!hasCover)
            {
                CancelCover();
                ReleaseCover();
                return;
            }

            if (loadedCoverCard == coverCard && coverImage.texture != null)
                return;

            CancelCover();
            ReleaseCover();
            var cache = coverCache ?? (privateCoverCache ??= new DeckCoverArtCache());
            coverEntry = cache.Acquire(coverCard);
            if (coverEntry.Texture != null)
            {
                ApplyCover(coverEntry.Texture, coverCard);
                return;
            }
            coverImage.texture = TextureManager.container.unknownArt.texture;
            coverImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            coverFitter.aspectRatio = 1f;
            coverCts = new CancellationTokenSource();
            _ = LoadCoverAsync(coverEntry, refreshVersion, coverCts.Token);
        }

        private void EnsureCover()
        {
            if (coverImage == null)
            {
                var area = new GameObject("DeckCoverArea", typeof(RectTransform), typeof(LayoutElement));
                area.transform.SetParent(transform, false);
                area.GetComponent<LayoutElement>().ignoreLayout = true;
                coverArea = area.GetComponent<RectTransform>();
                coverArea.anchorMin = new Vector2(0.065f, 0.24f);
                coverArea.anchorMax = new Vector2(0.935f, 0.94f);
                coverArea.offsetMin = coverArea.offsetMax = Vector2.zero;
                var frame = new GameObject("ArtworkFrame", typeof(RectTransform));
                frame.transform.SetParent(coverArea, false);
                var frameRect = frame.GetComponent<RectTransform>();
                frameRect.anchorMin = Vector2.zero;
                frameRect.anchorMax = Vector2.one;
                frameRect.offsetMin = new Vector2(28f, 0f);
                frameRect.offsetMax = new Vector2(-28f, 0f);
                var cover = new GameObject("DeckRepresentativeArt", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(RawImage), typeof(LayoutElement), typeof(AspectRatioFitter));
                cover.transform.SetParent(frameRect, false);
                cover.GetComponent<LayoutElement>().ignoreLayout = true;
                coverImage = cover.GetComponent<RawImage>();
                coverImage.raycastTarget = false;
                coverImage.uvRect = new Rect(0f, 0f, 1f, 1f);
                coverFitter = cover.GetComponent<AspectRatioFitter>();
                coverFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                coverFitter.aspectRatio = 1f;
            }

            if (lockBadge == null)
            {
                lockBadge = new GameObject("RecommendationLock", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Image), typeof(LayoutElement));
                lockBadge.transform.SetParent(coverArea, false);
                lockBadge.GetComponent<LayoutElement>().ignoreLayout = true;
                var background = lockBadge.GetComponent<Image>();
                background.color = new Color(0.07f, 0.11f, 0.14f, 0.90f);
                background.raycastTarget = false;
                var rect = lockBadge.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-4f, -4f);
                rect.sizeDelta = new Vector2(24f, 24f);

                var symbol = new GameObject("Lock", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(DeckCoverLockGraphic));
                symbol.transform.SetParent(lockBadge.transform, false);
                var symbolRect = symbol.GetComponent<RectTransform>();
                symbolRect.anchorMin = Vector2.zero;
                symbolRect.anchorMax = Vector2.one;
                symbolRect.offsetMin = new Vector2(4f, 3f);
                symbolRect.offsetMax = new Vector2(-4f, -3f);
                symbol.GetComponent<DeckCoverLockGraphic>().color = new Color(0.88f, 0.91f, 0.76f, 1f);
            }
        }

        private void ApplyCover(Texture2D art, int code)
        {
            coverImage.texture = art;
            coverImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            coverFitter.aspectRatio = art.width / (float)Mathf.Max(1, art.height);
            loadedCoverCard = code;
        }

        private async UniTask LoadCoverAsync(DeckCoverArtCache.Entry entry, int version, CancellationToken token)
        {
            try
            {
                var art = await entry.LoadingTask.AsUniTask().AttachExternalCancellation(token);
                if (art == null || token.IsCancellationRequested || this == null || gameObject == null ||
                    !gameObject.activeInHierarchy || version != refreshVersion || coverCard != entry.Code ||
                    coverEntry != entry || coverImage == null)
                    return;
                ApplyCover(art, entry.Code);
            }
            catch (System.OperationCanceledException) { }
            catch (MissingReferenceException) { }
            catch (System.Exception e)
            {
                Debug.LogWarning("Unable to load deck cover: " + e.GetType().Name);
            }
        }

        private void CancelCover()
        {
            if (coverCts == null)
                return;
            coverCts.Cancel();
            coverCts.Dispose();
            coverCts = null;
        }

        private void ReleaseCover()
        {
            if (coverImage != null)
                coverImage.texture = null;
            if (coverEntry != null)
                coverEntry.Owner.Release(coverEntry);
            coverEntry = null;
            loadedCoverCard = 0;
        }

        protected virtual void StartRefresh()
        {
            if (gameObject.activeInHierarchy)
            {
                try
                {
                    cts?.Cancel();
                    cts?.Dispose();
                }
                catch { };
                cts = new();
                _ = RefreshAsync();
            }
        }

        protected virtual async UniTask RefreshDeckCaseAsync()
        {
            if (isCreateButton || coverCard > 0)
                return;

            var version = refreshVersion;
            var requestedCase = deckCase;
            try
            {
                await UniTask.WaitWhile(
                    () => Program.instance.deckSelector.inTransition,
                    cancellationToken: destroyCancellationToken);

                int siblingIndex = transform.GetSiblingIndex();
                for (int i = 0; i < siblingIndex; i++)
                    await UniTask.Yield(cancellationToken: destroyCancellationToken);

                if (this == null || gameObject == null)
                    return;

                var sprite = await Program.items.LoadDeckCaseIconAsync(requestedCase, "_L_SD");
                if (this == null || gameObject == null || sprite == null || version != refreshVersion)
                    return;

                Manager.GetElement<Image>("DeckImage").sprite = sprite;
            }
            catch (System.OperationCanceledException)
            {
            }
            catch (MissingReferenceException)
            {
            }
        }

        protected override async UniTask RefreshAsync()
        {
            if (isCreateButton)
                return;

            var cancellationToken = cts != null ? cts.Token : destroyCancellationToken;
            try
            {
                await UniTask.WaitWhile(
                    () => Program.instance.deckSelector.inTransition,
                    cancellationToken: cancellationToken);

                if (!CanContinueRefresh(cancellationToken))
                    return;

                Material protectorMaterial = null;
                protectorMaterial = await RefreshCardImageAsync("CardImage0", card0, protectorMaterial, cancellationToken);
                if (!CanContinueRefresh(cancellationToken))
                    return;

                protectorMaterial = await RefreshCardImageAsync("CardImage1", card1, protectorMaterial, cancellationToken);
                if (!CanContinueRefresh(cancellationToken))
                    return;

                await RefreshCardImageAsync("CardImage2", card2, protectorMaterial, cancellationToken);
            }
            catch (System.OperationCanceledException)
            {
            }
            catch (MissingReferenceException)
            {
            }
        }

        private async UniTask<Material> RefreshCardImageAsync(string elementName, int cardCode, Material protectorMaterial,
            CancellationToken cancellationToken)
        {
            if (!TryGetCardImage(elementName, cancellationToken, out var cardImage))
                return protectorMaterial;

            if (cardCode != 0)
            {
                var texture = await CardImageLoader.LoadCardAsync(cardCode, true);
                if (!CanApplyDeckImage(cardImage, cancellationToken))
                    return protectorMaterial;

                cardImage.texture = texture;
                cardImage.material = null;
                return protectorMaterial;
            }

            if (protectorMaterial == null)
                protectorMaterial = await ABLoader.LoadProtectorMaterial(protector, cancellationToken);
            if (!CanApplyDeckImage(cardImage, cancellationToken))
                return protectorMaterial;

            cardImage.texture = null;
            cardImage.material = protectorMaterial;
            return protectorMaterial;
        }

        private bool TryGetCardImage(string elementName, CancellationToken cancellationToken, out RawImage cardImage)
        {
            cardImage = null;
            if (!CanContinueRefresh(cancellationToken))
                return false;

            cardImage = Manager.GetElement<RawImage>(elementName);
            return CanApplyDeckImage(cardImage, cancellationToken);
        }

        private bool CanContinueRefresh(CancellationToken cancellationToken)
        {
            return !cancellationToken.IsCancellationRequested && this != null && gameObject != null;
        }

        private bool CanApplyDeckImage(RawImage cardImage, CancellationToken cancellationToken)
        {
            if (!CanContinueRefresh(cancellationToken))
                return false;
            return cardImage != null;
        }

        protected override void OnClick()
        {
            if (isCreateButton)
            {
                AudioManager.PlaySE(SoundLabelClick);
                Program.instance.deckSelector.GetUI<DeckSelectorUI>().DeckCreate();
                return;
            }

            if (isReadOnly)
            {
                AudioManager.PlaySE(SoundLabelClick);
                Program.instance.deckSelector.GetUI<DeckSelectorUI>().OnRecommendationSelected(sourceKey);
                return;
            }

            if(toggleMode)
            {
                isOn = !isOn;
                if (isOn)
                {
                    Manager.GetElement("IconOn").SetActive(true);
                    Program.instance.deckSelector.GetUI<DeckSelectorUI>().superScrollView.items[index].args[6] = "1";
                }
                else
                {
                    Manager.GetElement("IconOn").SetActive(false);
                    Program.instance.deckSelector.GetUI<DeckSelectorUI>().superScrollView.items[index].args[6] = "0";
                }
                AudioManager.PlaySE(isOn ? SoundLabelClickOn : SoundLabelClickOff);
            }
            else
            {
                AudioManager.PlaySE(SoundLabelClick);
                Config.SetConfigDeck(deckName, true);
                if (DeckSelector.condition == DeckSelector.Condition.ForEdit)
                {
                    Program.instance.deckEditor.SwitchCondition(DeckEditor.Condition.EditDeck);
                    Program.instance.ShiftToServant(Program.instance.deckEditor);
                }
                else if (DeckSelector.condition == DeckSelector.Condition.MyCard)
                {
                    Program.instance.ShiftToServant(Program.instance.online);
                }
                else if (DeckSelector.condition == DeckSelector.Condition.ForDuel)
                {
                    Program.instance.ShiftToServant(Program.instance.room);
                }
                else if (DeckSelector.condition == DeckSelector.Condition.ForSolo)
                {
                    Program.instance.ShiftToServant(Program.instance.solo);
                }
            }
        }

        protected override void OnSubmit()
        {
            OnClick();
        }

        protected override void OnSelect(bool playSE)
        {
            HoverOn();
            SelectionEdgeHighlight.SetSelected(this, true);
            if (playSE)
                AudioManager.PlaySE(SoundLabelSelectedGamePad);
            Program.instance.currentServant.lastSelectable = Selectable;
            Program.instance.deckSelector.lastSelectedDeckItem = this;
            foreach (var ccg in transform.GetComponentsInChildren<ColorContainerGraphic>(true))
                ccg.SetColor(SelectMode.Selected, hovering ? StatusMode.Enter : StatusMode.Normal, Selectable.interactable);
        }

        public virtual void ShowPickup(bool forced = false)
        {
            if (forced)
                forcedPickup = true;
            if (isCreateButton)
                return;
            if (pickuping)
                return;
            pickuping = true;

            ApplyShowPickup();
        }

        protected virtual void ApplyShowPickup()
        {
            DOTweenLifecycle.KillAndClear(pickdownTweens);

            var tween1 = Manager.GetElement<CanvasGroup>("CardPos0").DOFade(1f, 0.2f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween1);
            var tween2 = Manager.GetElement<CanvasGroup>("CardPos1").DOFade(1f, 0.22f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween2);
            var tween3 = Manager.GetElement<CanvasGroup>("CardPos2").DOFade(1f, 0.24f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween3);

            var tween4 = Manager.GetElement<RectTransform>("CardImage0").DOAnchorPos3D(new Vector3(0f, 10f, 0f), 0.2f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween4);
            var tween5 = Manager.GetElement<RectTransform>("CardImage1").DOAnchorPos3D(new Vector3(0f, 10f, 0f), 0.22f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween5);
            var tween6 = Manager.GetElement<RectTransform>("CardImage2").DOAnchorPos3D(new Vector3(0f, 10f, 0f), 0.24f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween6);

            var tween7 = Manager.GetElement<RectTransform>("CardImage0").DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween7);
            var tween8 = Manager.GetElement<RectTransform>("CardImage2").DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween8);

            if(!refreshed)
                StartRefresh();
        }

        public virtual void HidePickup(bool forced = false)
        {
            if (isCreateButton)
                return;
            if (forced)
                forcedPickup = false;
            if (!pickuping) 
                return;
            if (forcedPickup)
                return;
            pickuping = false;

            ApplyHidePickup();
        }

        protected virtual void ApplyHidePickup(bool instant = false)
        {
            DOTweenLifecycle.KillAndClear(pickupTweens);

            if (instant)
            {
                DOTweenLifecycle.KillAndClear(pickdownTweens);
                ApplyHidePickupNow();
                return;
            }

            var tween1 = Manager.GetElement<CanvasGroup>("CardPos0").DOFade(0f, 0.2f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween1);
            var tween2 = Manager.GetElement<CanvasGroup>("CardPos1").DOFade(0f, 0.22f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween2);
            var tween3 = Manager.GetElement<CanvasGroup>("CardPos2").DOFade(0f, 0.24f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween3);

            var tween4 = Manager.GetElement<RectTransform>("CardImage0").DOAnchorPos3D(new Vector3(0f, -40f, 0f), 0.2f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween4);
            var tween5 = Manager.GetElement<RectTransform>("CardImage1").DOAnchorPos3D(new Vector3(0f, -40f, 0f), 0.22f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween5);
            var tween6 = Manager.GetElement<RectTransform>("CardImage2").DOAnchorPos3D(new Vector3(0f, -40f, 0f), 0.24f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween6);

            var tween7 = Manager.GetElement<RectTransform>("CardImage0").DOLocalRotate(new Vector3(0f, 0f, -20f), 0.2f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween7);
            var tween8 = Manager.GetElement<RectTransform>("CardImage2").DOLocalRotate(new Vector3(0f, 0f, 20f), 0.2f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween8);
        }

        private void ApplyHidePickupNow()
        {
            Manager.GetElement<CanvasGroup>("CardPos0").alpha = 0f;
            Manager.GetElement<CanvasGroup>("CardPos1").alpha = 0f;
            Manager.GetElement<CanvasGroup>("CardPos2").alpha = 0f;

            var card0 = Manager.GetElement<RectTransform>("CardImage0");
            card0.anchoredPosition3D = new Vector3(0f, -40f, 0f);
            card0.localEulerAngles = new Vector3(0f, 0f, -20f);

            var card1 = Manager.GetElement<RectTransform>("CardImage1");
            card1.anchoredPosition3D = new Vector3(0f, -40f, 0f);

            var card2 = Manager.GetElement<RectTransform>("CardImage2");
            card2.anchoredPosition3D = new Vector3(0f, -40f, 0f);
            card2.localEulerAngles = new Vector3(0f, 0f, 20f);
        }

        private void KillPickupTweens()
        {
            DOTweenLifecycle.KillAndClear(pickupTweens);
            DOTweenLifecycle.KillAndClear(pickdownTweens);
        }

        public void ShowToggle()
        {
            if (isCreateButton || isReadOnly)
            {
                return;
            }

            ApplyShowToggle();
        }

        private void ApplyShowToggle()
        {
            toggleMode = true;
            isOn = false;
            Program.instance.deckSelector.GetUI<DeckSelectorUI>().superScrollView.items[index].args[6] = "0";

            Manager.GetElement("SelectedStateToggle").SetActive(true);
            Manager.GetElement("IconOn").SetActive(false);
        }

        public void HideToggle()
        {
            ApplyHideToggle();
        }

        private void ApplyHideToggle()
        {
            toggleMode = false;
            Manager.GetElement("SelectedStateToggle").SetActive(false);
            Manager.GetElement("IconOn").SetActive(false);
            isOn = false;
        }

        public override void ToggleOnNow()
        {
        }

        public override void ToggleOffNow()
        {
        }

        protected override void ToggleOn()
        {
        }

        protected override void ToggleOff()
        {
        }

        protected override void HoverOn()
        {
            base.HoverOn();
            SelectionEdgeHighlight.SetSelected(this, true);
            ShowPickup();
        }

        protected override void HoverOff(bool force = false)
        {
            base.HoverOff(force);
            SelectionEdgeHighlight.SetSelected(this, selected || hovering);
            HidePickup();
        }

        protected override void OnDeselect()
        {
            base.OnDeselect();
            SelectionEdgeHighlight.SetSelected(this, hovering);
        }

        protected override int GetButtonsCount()
        {
            return Program.instance.deckSelector.GetUI<DeckSelectorUI>().superScrollView.items.Count;
        }

        protected override int GetColumnsCount()
        {
            return Program.instance.deckSelector.GetUI<DeckSelectorUI>().superScrollView.GetColumnCount();
        }

    }
}
