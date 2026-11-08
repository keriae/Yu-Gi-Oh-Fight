using DG.Tweening;
using MDPro3.Duel.YGOSharp;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
{
    public class SelectionButton_CardInDeck : SelectionButton, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        #region Elements

        private const string LABEL_IMAGE_CARD = "ImageCard";
        private CardRawImageHandler m_ImageCardHandler;
        private CardRawImageHandler ImageCardHandler =>
            m_ImageCardHandler = m_ImageCardHandler != null ? m_ImageCardHandler
            : Manager.GetElement<CardRawImageHandler>(LABEL_IMAGE_CARD);

        private const string LABEL_ICON_LIMIT = "IconLimit";
        private Image m_IconLimit;
        private Image IconLimit =>
            m_IconLimit = m_IconLimit != null ? m_IconLimit
            : Manager.GetElement<Image>(LABEL_ICON_LIMIT);

        private const string LABEL_ICON_ATTRIBUTE = "IconAttribute";
        private Image m_IconAttribute;
        private Image IconAttribute =>
            m_IconAttribute = m_IconAttribute != null ? m_IconAttribute
            : Manager.GetElement<Image>(LABEL_ICON_ATTRIBUTE);

        private const string LABEL_ICON_SPELL_TRAP_TYPE = "IconSpellTrapType";
        private Image m_IconSpellTrapType;
        private Image IconSpellTrapType =>
            m_IconSpellTrapType = m_IconSpellTrapType != null ? m_IconSpellTrapType
            : Manager.GetElement<Image>(LABEL_ICON_SPELL_TRAP_TYPE);

        private const string LABEL_ICON_RACE = "IconRace";
        private Image m_IconRace;
        private Image IconRace =>
            m_IconRace = m_IconRace != null ? m_IconRace
            : Manager.GetElement<Image>(LABEL_ICON_RACE);

        private const string LABEL_ICON_POOL = "IconPool";
        private Image m_IconPool;
        private Image IconPool =>
            m_IconPool = m_IconPool != null ? m_IconPool
            : Manager.GetElement<Image>(LABEL_ICON_POOL);

        private const string LABEL_ICON_TUNER = "IconTuner";
        private Image m_IconTuner;
        private Image IconTuner =>
            m_IconTuner = m_IconTuner != null ? m_IconTuner
            : Manager.GetElement<Image>(LABEL_ICON_TUNER);

        private const string LABEL_ICON_LEVEL = "IconLevel";
        private Image m_IconLevel;
        private Image IconLevel =>
            m_IconLevel = m_IconLevel != null ? m_IconLevel
            : Manager.GetElement<Image>(LABEL_ICON_LEVEL);

        private const string LABEL_ICON_RANK = "IconRank";
        private Image m_IconRank;
        private Image IconRank =>
            m_IconRank = m_IconRank != null ? m_IconRank
            : Manager.GetElement<Image>(LABEL_ICON_RANK);

        private const string LABEL_ICON_LINK = "IconLink";
        private Image m_IconLink;
        private Image IconLink =>
            m_IconLink = m_IconLink != null ? m_IconLink
            : Manager.GetElement<Image>(LABEL_ICON_LINK);

        private const string LABEL_ICON_PENDULUM_SCALE = "IconPendulumScale";
        private Image m_IconPendulumScale;
        private Image IconPendulumScale =>
            m_IconPendulumScale = m_IconPendulumScale != null ? m_IconPendulumScale
            : Manager.GetElement<Image>(LABEL_ICON_PENDULUM_SCALE);

        private const string LABEL_TEXT_LEVEL = "TextLevel";
        private TextMeshProUGUI m_TextLevel;
        private TextMeshProUGUI TextLevel =>
            m_TextLevel = m_TextLevel != null ? m_TextLevel
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TEXT_LEVEL);

        private const string LABEL_TEXT_RANK = "TextRank";
        private TextMeshProUGUI m_TextRank;
        private TextMeshProUGUI TextRank =>
            m_TextRank = m_TextRank != null ? m_TextRank
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TEXT_RANK);

        private const string LABEL_TEXT_LINK = "TextLink";
        private TextMeshProUGUI m_TextLink;
        private TextMeshProUGUI TextLink =>
            m_TextLink = m_TextLink != null ? m_TextLink
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TEXT_LINK);

        private const string LABEL_TEXT_PENDULUM_SCALE = "TextPendulumScale";
        private TextMeshProUGUI m_TextPendulumScale;
        private TextMeshProUGUI TextPendulumScale =>
            m_TextPendulumScale = m_TextPendulumScale != null ? m_TextPendulumScale
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TEXT_PENDULUM_SCALE);

        private const string LABEL_GO_PICKUP_CURSOR = "PickupCursor";
        private GameObject m_PickupCursor;
        private GameObject PickupCursor =>
            m_PickupCursor = m_PickupCursor != null ? m_PickupCursor
            : Manager.GetElement(LABEL_GO_PICKUP_CURSOR);

        private const string LABEL_TXT_PICKUP_CURSOR = "PickupCursor/Text";
        private TextMeshProUGUI m_TextPickupCursor;
        private TextMeshProUGUI TextPickupCursor =>
            m_TextPickupCursor = m_TextPickupCursor != null ? m_TextPickupCursor
            : Manager.GetNestedElement<TextMeshProUGUI>(LABEL_TXT_PICKUP_CURSOR);

        private const string LABEL_GO_CARD_POINT = "CardPointRoot";
        private GameObject m_CardPoint;
        private GameObject CardPoint =>
            m_CardPoint = m_CardPoint != null ? m_CardPoint
            : Manager.GetElement(LABEL_GO_CARD_POINT);

        private const string LABEL_TXT_CARD_POINT = "TextCardPointValue";
        private TextMeshProUGUI m_TextCardPoint;
        private TextMeshProUGUI TextCardPoint =>
            m_TextCardPoint = m_TextCardPoint != null ? m_TextCardPoint
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_CARD_POINT);

        #endregion

        [Header("SelectionButton CardInDeck")]
        [HideInInspector] public DeckView deckView;
        private Card _card;
        public Card Card
        {
            get { return _card; }
            set
            {
                if(_card == null || value.Id != _card.Id)
                {
                    _card = value;
                    SetIcons();
                    Refresh();
                }
            }
        }

        public bool Refreshed => ImageCardHandler.Refreshed;
        public DeckView.DeckLocation location;
        private Vector3 dragScale = new(1.7f, 1.7f, 1f);
        private RectTransform child;

        protected override void Awake()
        {
            manuallySetNavigation = false;
            base.Awake();
            child = transform.GetChild(0).GetComponent<RectTransform>();

            SetClickEvent(() =>
            {
                if (Runtime.Program.instance.currentServant == Runtime.Program.instance.deckEditor)
                {
                    if (Runtime.UserInput.gamepadType == Runtime.UserInput.GamepadType.None)
                    {
                        if (Controllers.DeckEditor.UseMobileLayout)
                        {
                            if (dragProcessing)
                                return;
                            Runtime.AudioManager.PlaySE("SE_MENU_DECIDE");
                            Runtime.Program.instance.deckEditor.lastSelectedCardInDeck = this;
                            Runtime.Program.instance.deckEditor.ResponseRegion = ServantUI.DeckEditorUI.ResponseRegion.Deck;
                            Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().ShowCardActionMenu();
                        }
                        else
                        {
                            Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().AddHistoryCard(Card.Id);
                            ShowThisCard();
                        }
                    }
                    else
                    {
                        if (Controllers.DeckEditor.condition == Controllers.DeckEditor.Condition.EditDeck)
                            Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().RemoveCardWithAnimation(this);
                    }
                }
                else if (Runtime.Program.instance.currentServant == Runtime.Program.instance.deckBrowser)
                {
                    if (dragProcessing)
                        return;

                    Runtime.AudioManager.PlaySE("SE_MENU_DECIDE");
                    PickupClick();
                }
            });
            SetRightClickEvent(() =>
            {
                if (Runtime.Program.instance.currentServant == Runtime.Program.instance.deckEditor)
                {
                    if (Controllers.DeckEditor.condition != Controllers.DeckEditor.Condition.ChangeSide)
                    {
                        Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().RemoveCardWithAnimation(this);
                    }
                    else
                    {
                        Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().CardChangeSide(this);
                    }
                    ShowThisCard();
                }
            });
            SetMiddleClickEvent(() =>
            {
                if (Runtime.Program.instance.currentServant == Runtime.Program.instance.deckEditor)
                {
                    Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().AddCard(Card);
                    ShowThisCard();
                }
            });
        }

        protected override void OnSelect(bool playSE)
        {
            base.OnSelect(playSE);

            foreach (Presentation.UI.ColorContainerGraphic ccg in Manager.GetElement<Transform>("ImageCard")
                .GetComponentsInChildren<Presentation.UI.ColorContainerGraphic>(true))
                ccg.SetColor(Presentation.UI.ColorContainer.SelectMode.Selected, hovering ? Presentation.UI.ColorContainer.StatusMode.Enter : Presentation.UI.ColorContainer.StatusMode.Normal, Selectable.interactable);

            ShowThisCard();
            if(Runtime.Program.instance.currentServant == Runtime.Program.instance.deckEditor)
            {
                Runtime.Program.instance.deckEditor.lastSelectedCardInDeck = this;
                Runtime.Program.instance.deckEditor.ResponseRegion = ServantUI.DeckEditorUI.ResponseRegion.Deck;
            }
            else if(Runtime.Program.instance.currentServant == Runtime.Program.instance.deckBrowser)
            {
                Runtime.Program.instance.deckBrowser.lastSelectedCardInDeck = this;
                Runtime.Program.instance.deckBrowser.ResponseRegion = ServantUI.DeckBrowserUI.ResponseRegion.Deck;
            }
        }

        private void Refresh()
        {
            ImageCardHandler.SetCard(Card);
        }

        public void RefreshRarity(int code)
        {
            ImageCardHandler.RefreshRarity(code);
        }

        public void SetRegulationIcon()
        {
            IconLimit.sprite =
                Runtime.TextureManager.container.GetCardRegulationIcon(Card.Id, Controllers.DeckEditor.Banlist);
        }

        public void RefreshCredit()
        {
            TextCardPoint.text = Card.GetCreditString();
            TextCardPoint.color = BanlistManager.currentGenesysBanList.GetCreditColor(Card.GetCredit()); ;
        }

        private void SetIcons()
        {
            SetRegulationIcon();

            var attributeIcon = Runtime.TextureManager.container.GetCardAttributeIcon(Card);
            IconAttribute.sprite =
                attributeIcon == null
                ? Runtime.TextureManager.container.typeNone
                : attributeIcon;

            var spellTrapTypeIcon = Runtime.TextureManager.container.GetCardSpellTrapTypeIcon(Card);
            IconSpellTrapType.sprite =
                spellTrapTypeIcon == null
                ? Runtime.TextureManager.container.typeNone
                : spellTrapTypeIcon;

            var raceIcon = Runtime.TextureManager.container.GetCardRaceIcon(Card);
            IconRace.sprite =
                raceIcon == null
                ? Runtime.TextureManager.container.typeNone
                : raceIcon;
            IconPool.sprite =
                Runtime.TextureManager.container.GetCardPoolIcon(Card);

            TextLevel.text = Card.Level.ToString();
            TextRank.text = Card.Level.ToString();
            TextLink.text = Card.GetLinkCount().ToString();
            TextPendulumScale.text = Card.LScale.ToString();

            RefreshCredit();
            RefreshIcons();
        }

        public void RefreshIcons()
        {
            if(Runtime.Program.instance.currentServant == Runtime.Program.instance.deckEditor)
            {
                IconAttribute.gameObject.SetActive(Controllers.DeckEditor.cardInfoType == Controllers.DeckEditor.CardInfoType.Detail);
                IconSpellTrapType.gameObject.SetActive(Controllers.DeckEditor.cardInfoType == Controllers.DeckEditor.CardInfoType.Detail);
                IconRace.gameObject.SetActive(Controllers.DeckEditor.cardInfoType == Controllers.DeckEditor.CardInfoType.Detail);
                IconTuner.gameObject.SetActive(Controllers.DeckEditor.cardInfoType == Controllers.DeckEditor.CardInfoType.Detail
                    && Card.HasType(CardType.Tuner));
                Card.LevelType levelType = Card.GetLevelType();
                IconLevel.gameObject.SetActive(Controllers.DeckEditor.cardInfoType == Controllers.DeckEditor.CardInfoType.Detail
                    && Card.HasType(CardType.Monster) && levelType == Card.LevelType.Level);
                IconRank.gameObject.SetActive(Controllers.DeckEditor.cardInfoType == Controllers.DeckEditor.CardInfoType.Detail
                    && Card.HasType(CardType.Monster) && levelType == Card.LevelType.Rank);
                IconLink.gameObject.SetActive(Controllers.DeckEditor.cardInfoType == Controllers.DeckEditor.CardInfoType.Detail
                    && Card.HasType(CardType.Monster) && levelType == Card.LevelType.Link);
                IconPendulumScale.gameObject.SetActive(Controllers.DeckEditor.cardInfoType == Controllers.DeckEditor.CardInfoType.Detail
                    && Card.HasType(CardType.Pendulum));
                IconPool.gameObject.SetActive(Controllers.DeckEditor.cardInfoType == Controllers.DeckEditor.CardInfoType.Pool);
                CardPoint.SetActive(Controllers.DeckEditor.cardInfoType == Controllers.DeckEditor.CardInfoType.Genesys);
                IconLimit.gameObject.SetActive(Controllers.DeckEditor.cardInfoType != Controllers.DeckEditor.CardInfoType.Genesys);
            }
            else if (Runtime.Program.instance.currentServant == Runtime.Program.instance.deckBrowser)
            {
                IconAttribute.gameObject.SetActive(false);
                IconSpellTrapType.gameObject.SetActive(false);
                IconRace.gameObject.SetActive(false);
                IconTuner.gameObject.SetActive(false);
                IconLevel.gameObject.SetActive(false);
                IconRank.gameObject.SetActive(false);
                IconLink.gameObject.SetActive(false);
                IconPendulumScale.gameObject.SetActive(false);
                IconPool.gameObject.SetActive(false);
                CardPoint.SetActive(false);
            }
        }

        public void PlayBirthAnimation()
        {
            StartCoroutine(PlayBirthAnimationAsync());
        }

        private IEnumerator PlayBirthAnimationAsync()
        {
            yield return null;
            child.SetParent(Runtime.Program.instance.ui_.transform, true);
            child.localScale = dragScale;
            child.DOScale(Vector3.one, 0.3f).SetEase(Ease.InQuart).OnComplete(() =>
            {
                child.SetParent(transform, true);
                child.localPosition = Vector3.zero;
                child.localScale = Vector3.one;
                child.localEulerAngles = Vector3.zero;
            });
        }

        /// <summary>
        /// 锁定卡片child当前的位置，并在下一帧开始移动到父级初始位置
        /// </summary>
        public void LockPosition()
        {
            //child.SetParent(Program.instance.ui_.transform, true);
            child.SetParent(deckView.TempView, true);
            StartCoroutine(AutoMoveToParent());
        }

        /// <summary>
        /// 锁定卡片child到指定位置position，并在下一帧开始移动到父级初始位置
        /// </summary>
        /// <param name="position"></param>
        /// <param name="scale"></param>
        public void LockPosition(Vector3 position, Vector3 scale)
        {
            child.SetParent(Runtime.Program.instance.ui_.transform, true);
            child.position = position;
            child.localScale = scale;
            StartCoroutine(AutoMoveToParent());
        }

        private IEnumerator AutoMoveToParent()
        {
            yield return null;
            foreach (Presentation.UI.ColorContainerGraphic ccg in child.GetComponentsInChildren<Presentation.UI.ColorContainerGraphic>(true))
                ccg.SetColor(selected ? Presentation.UI.ColorContainer.SelectMode.Selected : Presentation.UI.ColorContainer.SelectMode.Unselected, Presentation.UI.ColorContainer.StatusMode.Normal, Selectable.interactable);

            var position = transform.position;
            DOTween.Sequence()
                .Append(child.DOMove(position, 0.1f).SetEase(Ease.OutCubic))
                .Join(child.DOScale(Vector3.one, 0.1f).SetEase(Ease.OutCubic))
                .OnComplete(() =>
                {
                    child.SetParent(transform, true);
                    child.localPosition = Vector3.zero;
                    child.localScale = Vector3.one;
                    child.localEulerAngles = Vector3.zero;
                });
        }

        public void MoveToParent(Vector3 position)
        {
            child.SetParent(Runtime.Program.instance.ui_.transform, true);
            child.localScale = dragScale;
            child.position = position;
            StartCoroutine(AutoMoveToParent());
        }

        public void MoveToParentSequence(Vector3 position)
        {
            if (!Controllers.DeckEditor.UseMobileLayout)
            {
                child.SetParent(Runtime.Program.instance.ui_.transform, true);
                child.localScale = dragScale;
                child.position = position;
            }
            StartCoroutine(AutoMoveToParentSequence(position));
        }

        private IEnumerator AutoMoveToParentSequence(Vector3 position)
        {
            if (Controllers.DeckEditor.UseMobileLayout)
            {
                child.gameObject.SetActive(false);
                yield return null;
                deckView.ScrollTo(this);
            }

            yield return null;
            if (Controllers.DeckEditor.UseMobileLayout)
            {
                child.gameObject.SetActive(true);
                child.SetParent(Runtime.Program.instance.ui_.transform, true);
                child.localScale = dragScale;
                child.position = position;
            }
            yield return null;
            foreach (Presentation.UI.ColorContainerGraphic ccg in child.GetComponentsInChildren<Presentation.UI.ColorContainerGraphic>(true))
                ccg.SetColor(Presentation.UI.ColorContainer.SelectMode.Unselected, Presentation.UI.ColorContainer.StatusMode.Normal, Selectable.interactable);

            var endPosition = transform.position;
            DOTween.Sequence()
                .Append(child.DOMove(endPosition, 0.2f).SetEase(Ease.OutCubic))
                .Append(child.DOScale(Vector3.one, 0.2f).SetEase(Ease.InCubic))
                .OnComplete(() =>
                {
                    child.SetParent(transform, true);
                    child.localPosition = Vector3.zero;
                    child.localScale = Vector3.one;
                    child.localEulerAngles = Vector3.zero;
                });
        }

        public bool IsHovering()
        {
            return hovering;
        }

        private void ShowThisCard()
        {
            var codes = deckView.GetAllCardCodes();

            if (Runtime.Program.instance.currentServant == Runtime.Program.instance.deckEditor)
                Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().ShowDetail(codes, GetIndex());
            else if (Runtime.Program.instance.currentServant == Runtime.Program.instance.deckBrowser)
                Runtime.Program.instance.deckBrowser.GetUI<ServantUI.DeckBrowserUI>().ShowDetail(codes, GetIndex());
        }

        #region Pickup

        public bool picked = false;
        public int pickupIndex = -1;
        private static PickupCardSelection pickupCardSelection
            => Runtime.Program.instance.deckBrowser.GetUI<ServantUI.DeckBrowserUI>().PickupCardSelection;

        public void PrePickThis(int index)
        {
            picked = true;
            pickupIndex = index;
            PickupCursor.SetActive(true);
            TextPickupCursor.text = (index + 1).ToString();
        }

        public void PickThisByIndex(int index)
        {
            PrePickThis(index);
            deckView.Pickup(this);
            pickupCardSelection.SetPickup(ImageCardHandler.card, pickupIndex);
        }

        public void DepickupThis()
        {
            picked = false;
            pickupIndex = -1;
            PickupCursor.SetActive(false);
        }

        private void PickupClick()
        {
            Runtime.Program.instance.deckBrowser.lastSelectedCardInDeck = this;

            ShowThisCard();
            if (picked)
            {
                pickupCardSelection.SetPickup(null, pickupIndex);
                DepickupThis();
            }
            else
            {
                PickThisByIndex(pickupCardSelection.GetPickupIndex());
            }
        }

        #endregion

        private int GetIndex()
        {
            int index = 0;
            var cards = deckView.cards;

            for (int i = 0; i < cards.Count; i++)
                if (cards[i] == this)
                {
                    index = i;
                    break;
                }
            return index;
        }

        #region Drag

        private RectTransform dragTarget;
        private Vector2 dragStartPosition;
        private bool dragProcessing;
        private bool draging;
        private bool dragIni;

        private bool NeedResponseDrag()
        {
            return deckView.condition == DeckView.Condition.Editable
                || deckView.condition == DeckView.Condition.ChangeSide;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            deckView.ScrollRect.OnBeginDrag(eventData);
            dragStartPosition = eventData.position;
            dragProcessing = true;
            draging = !Controllers.DeckEditor.UseMobileLayout;
            dragIni = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (draging)
            {
                if(!dragIni)
                {
                    dragTarget = Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().DragCard;
                    dragTarget.gameObject.SetActive(true);
                    dragTarget.GetChild(0).GetComponent<RawImage>().texture
                        = ImageCardHandler.RawImage.texture;
                    dragTarget.GetChild(0).GetComponent<RawImage>().material
                        = ImageCardHandler.RawImage.material;
                    dragIni = true;

                    UIHover.HoveringLabel = string.Empty;
                    Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().CardCollectionView.SetBookmarkDropArea(Card.Id);

                    Runtime.UserInput.Draging = true;
                    var cg = GetComponent<CanvasGroup>();
                    cg.blocksRaycasts = false;
                    cg.alpha = 0f;
                }

                RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    dragTarget, eventData.position, eventData.enterEventCamera, out var position);
                dragTarget.position = position;
                var anchoredPositon = dragTarget.anchoredPosition3D;
                anchoredPositon.z = -10f;
                dragTarget.anchoredPosition3D = anchoredPositon;
            }
            else
            {
                deckView.ScrollRect.OnDrag(eventData);
                draging = NeedResponseDrag()
                    & NeedStartDrag(dragStartPosition, eventData.position);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            deckView.ScrollRect.OnEndDrag(eventData);
            dragProcessing = false;

            Runtime.UserInput.Draging = false;

            if(draging)
            {
                var cg = GetComponent<CanvasGroup>();
                cg.blocksRaycasts = true;
                cg.alpha = 1f;

                dragTarget.gameObject.SetActive(false);

                Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().DragCardTo(this);
                deckView.HideDeckLocationTable();
            }
        }

        private Vector3 lastDragStartPosition;
        private bool diffYOverLimit;
        private bool NeedStartDrag(Vector3 startPosition, Vector3 position)
        {
            if (lastDragStartPosition == null || startPosition != lastDragStartPosition)
            {
                diffYOverLimit = false;
                lastDragStartPosition = startPosition;
            }

            if (diffYOverLimit)
                return false;
            var diffX = Mathf.Abs(position.x - startPosition.x);
            var diffY = Mathf.Abs(position.y - startPosition.y);
            if (diffY > 100f)
            {
                diffYOverLimit = true;
                return false;
            }
            if (diffX > 10f && diffX > diffY)
                return true;
            return false;
        }

        #endregion

        #region Navigation

        protected override int GetButtonsCount()
        {
            return deckView.GetDeckLocationCount(location);
        }

        protected override int GetColumnsCount()
        {
            return deckView.GetDeckLocationParent(location)
                .GetComponent<GridLayoutGroup>().Size().x;
        }

        protected override void OnNavigation(AxisEventData eventData)
        {
            var selfIndex = transform.GetSiblingIndex();

            var count = GetButtonsCount();
            var columes = GetColumnsCount();
            if(columes == 0)
            {
                Debug.LogError("divide by zero");
                return;
            }

            var targetIndex = selfIndex + 1;

            if (eventData.moveDir == MoveDirection.Left)
            {
                if (selfIndex % columes == 0)
                    return;
                targetIndex = selfIndex - 1;
            }
            else if (eventData.moveDir == MoveDirection.Right)
            {
                if (selfIndex % columes == columes - 1
                    || selfIndex == count - 1)
                {
                    if (Runtime.Program.instance.currentServant == Runtime.Program.instance.deckEditor)
                        Runtime.Program.instance.deckEditor.SelectNearestCollectionViewItem(transform.position);
                    else if (Runtime.Program.instance.currentServant == Runtime.Program.instance.deckBrowser)
                        Runtime.Program.instance.deckBrowser.GetUI<ServantUI.DeckBrowserUI>().PickupCardSelection.Select();
                    return;
                }
            }
            else if (eventData.moveDir == MoveDirection.Up)
            {
                targetIndex = selfIndex - columes;
                if (targetIndex < 0)
                {
                    SelectTarget(GetNavivationTarget(eventData.moveDir));
                    return;
                }
            }
            else if (eventData.moveDir == MoveDirection.Down)
            {
                targetIndex = selfIndex + columes;
                if (targetIndex >= count)
                {
                    if(location == DeckView.DeckLocation.SideDeck
                        && !Utility.Tools.InLastRow(selfIndex, count, columes))
                        targetIndex = count - 1;
                    else
                    {
                        SelectTarget(GetNavivationTarget(eventData.moveDir));
                        return;
                    }
                }
            }

            for (int i = 0; i < transform.parent.childCount; i++)
            {
                var child = transform.parent.GetChild(i);
                if (!child.gameObject.activeSelf)
                    continue;

                var buttonIndex = child.GetComponent<SelectionButton>().index;
                if (buttonIndex < 0)
                    buttonIndex = i;

                if (buttonIndex == targetIndex)
                {
                    Runtime.UserInput.NextSelectionIsAxis = true;
                    EventSystem.current.SetSelectedGameObject(transform.parent.GetChild(i).gameObject);
                    break;
                }
            }
        }

        private SelectionButton_CardInDeck GetNavivationTarget(MoveDirection direction)
        {
            if (direction == MoveDirection.Up)
            {
                if (location == DeckView.DeckLocation.MainDeck)
                    return null;
                else if(location == DeckView.DeckLocation.ExtraDeck)
                    return deckView.GetNavigationTarget(DeckView.DeckLocation.MainDeck, direction, transform.position);
                else if (location == DeckView.DeckLocation.SideDeck)
                    return deckView.GetNavigationTarget(DeckView.DeckLocation.ExtraDeck, direction, transform.position);
            }
            else if (direction == MoveDirection.Down)
            {
                if (location == DeckView.DeckLocation.MainDeck)
                    return deckView.GetNavigationTarget(DeckView.DeckLocation.ExtraDeck, direction, transform.position);
                else if (location == DeckView.DeckLocation.ExtraDeck)
                    return deckView.GetNavigationTarget(DeckView.DeckLocation.SideDeck, direction, transform.position);
                else if (location == DeckView.DeckLocation.SideDeck)
                    return null;
            }
            return null;
        }

        private void SelectTarget(SelectionButton_CardInDeck target)
        {
            if (target == null)
                return;
            Runtime.UserInput.NextSelectionIsAxis = true;
            target.GetSelectable().Select();
        }

        #endregion
    }
}
