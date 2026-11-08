using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
{
    public class SelectionButton_CardInCollection : SelectionButton, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        #region Elements

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

        private int _cardCode;
        public int CardCode
        {
            get { return _cardCode; }
            set
            {
                if (_cardCode != value)
                {
                    _cardCode = value;
                    card = MDPro3.Duel.YGOSharp.CardsManager.Get(_cardCode);
                    SetIcons();
                    Refresh();
                }
            }
        }
        public MDPro3.Duel.YGOSharp.Card card;
        public CardCollectionView cardCollectionView;

        private CardRawImageHandler m_ImageHandler;
        private CardRawImageHandler ImageHandler =>
            m_ImageHandler = m_ImageHandler != null ? m_ImageHandler
            : Manager.GetElement<CardRawImageHandler>("ImageCard");

        protected override void Awake()
        {
            manuallySetNavigation = false;
            base.Awake();
            SetClickEvent(() =>
            {
                if(Runtime.UserInput.gamepadType == Runtime.UserInput.GamepadType.None)
                {

                    if (Controllers.DeckEditor.UseMobileLayout)
                    {
                        if (dragProcessing)
                            return;
                        Runtime.AudioManager.PlaySE("SE_MENU_DECIDE");
                        Runtime.Program.instance.deckEditor.lastSelectedCardInCollection = this;
                        Runtime.Program.instance.deckEditor.ResponseRegion = ServantUI.DeckEditorUI.ResponseRegion.Collection;
                        Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().ShowCardActionMenu();
                    }
                    else
                    {
                        ShowThis();
                        if (cardCollectionView.area != CardCollectionView.Area.History)
                            Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().AddHistoryCard(card.Id);
                    }
                }
                else
                {
                    AddThisToDeck();
                }
            });
            SetRightClickEvent(() =>
            {
                AddThisToDeck();
            });
        }

        protected override void OnSelect(bool playSE)
        {
            base.OnSelect(playSE);
            ShowThis();
            Runtime.Program.instance.deckEditor.lastSelectedCardInCollection = this;
            Runtime.Program.instance.deckEditor.ResponseRegion = ServantUI.DeckEditorUI.ResponseRegion.Collection;
        }

        private void AddThisToDeck()
        {
            var position = transform.GetChild(0).position;
            Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().AddCardFromCollection(card, position);
            ShowThis();
        }

        public void Refresh()
        {
            ImageHandler.SetCard(card);
        }

        public void RefreshRarity(int code)
        {
            ImageHandler.RefreshRarity(code);
        }

        public void SetRegulationIcon()
        {
            Manager.GetElement<Image>("IconLimit").sprite
                = Runtime.TextureManager.container.GetCardRegulationIcon(CardCode, Controllers.DeckEditor.Banlist);
        }

        public void RefreshCredit()
        {
            TextCardPoint.text = card.GetCreditString();
            TextCardPoint.color = MDPro3.Duel.YGOSharp.BanlistManager.currentGenesysBanList.GetCreditColor(card.GetCredit());
        }

        private void SetIcons()
        {
            SetRegulationIcon();

            var attributeIcon = Runtime.TextureManager.container.GetCardAttributeIcon(card);
            Manager.GetElement<Image>("IconAttribute").sprite =
                attributeIcon == null
                ? Runtime.TextureManager.container.typeNone
                : attributeIcon;

            var spellTrapTypeIcon = Runtime.TextureManager.container.GetCardSpellTrapTypeIcon(card);
            Manager.GetElement<Image>("IconSpellTrapType").sprite =
                spellTrapTypeIcon == null
                ? Runtime.TextureManager.container.typeNone
                : spellTrapTypeIcon;

            var raceIcon = Runtime.TextureManager.container.GetCardRaceIcon(card);
            Manager.GetElement<Image>("IconRace").sprite =
                raceIcon == null
                ? Runtime.TextureManager.container.typeNone
                : raceIcon;
            Manager.GetElement<Image>("IconPool").sprite =
                Runtime.TextureManager.container.GetCardPoolIcon(card);

            Manager.GetElement<TextMeshProUGUI>("TextLevel").text = card.Level.ToString();
            Manager.GetElement<TextMeshProUGUI>("TextRank").text = card.Level.ToString();
            Manager.GetElement<TextMeshProUGUI>("TextLink").text = card.GetLinkCount().ToString();
            Manager.GetElement<TextMeshProUGUI>("TextPendulumScale").text = card.LScale.ToString();

            RefreshCredit();
            RefreshIcons();
            RefreshCountIcon();
        }

        public void RefreshIcons()
        {
            Manager.GetElement("IconAttribute").SetActive(Controllers.DeckEditor.cardInfoType == Controllers.DeckEditor.CardInfoType.Detail);
            Manager.GetElement("IconSpellTrapType").SetActive(Controllers.DeckEditor.cardInfoType == Controllers.DeckEditor.CardInfoType.Detail);
            Manager.GetElement("IconRace").SetActive(Controllers.DeckEditor.cardInfoType == Controllers.DeckEditor.CardInfoType.Detail);
            Manager.GetElement("IconTuner").SetActive(Controllers.DeckEditor.cardInfoType == Controllers.DeckEditor.CardInfoType.Detail
                && card.HasType(MDPro3.Duel.YGOSharp.CardType.Tuner));
            MDPro3.Duel.YGOSharp.Card.LevelType levelType = card.GetLevelType();
            Manager.GetElement("IconLevel").SetActive(Controllers.DeckEditor.cardInfoType == Controllers.DeckEditor.CardInfoType.Detail
                && card.HasType(MDPro3.Duel.YGOSharp.CardType.Monster) && levelType == MDPro3.Duel.YGOSharp.Card.LevelType.Level);
            Manager.GetElement("IconRank").SetActive(Controllers.DeckEditor.cardInfoType == Controllers.DeckEditor.CardInfoType.Detail
                && card.HasType(MDPro3.Duel.YGOSharp.CardType.Monster) && levelType == MDPro3.Duel.YGOSharp.Card.LevelType.Rank);
            Manager.GetElement("IconLink").SetActive(Controllers.DeckEditor.cardInfoType == Controllers.DeckEditor.CardInfoType.Detail
                && card.HasType(MDPro3.Duel.YGOSharp.CardType.Monster) && levelType == MDPro3.Duel.YGOSharp.Card.LevelType.Link);
            Manager.GetElement("IconPendulumScale").SetActive(Controllers.DeckEditor.cardInfoType == Controllers.DeckEditor.CardInfoType.Detail
                && card.HasType(MDPro3.Duel.YGOSharp.CardType.Pendulum));
            Manager.GetElement("IconPool").SetActive(Controllers.DeckEditor.cardInfoType == Controllers.DeckEditor.CardInfoType.Pool);

            CardPoint.SetActive(Controllers.DeckEditor.cardInfoType == Controllers.DeckEditor.CardInfoType.Genesys);
            Manager.GetElement("IconLimit").SetActive(Controllers.DeckEditor.cardInfoType != Controllers.DeckEditor.CardInfoType.Genesys);

        }

        public void RefreshCountIcon()
        {
            var ragulation = Controllers.DeckEditor.Banlist.GetQuantity(card.Id);
            var count = Runtime.Program.instance.deckEditor
                .GetUI<ServantUI.DeckEditorUI>().DeckView.GetCardCount(card.Id);
            var color = Color.white;
            if(count == ragulation)
                color = Color.yellow;
            if(count > ragulation)
                color = Color.red;

            Manager.GetElement<Image>("IconCardUse1").color = color;
            Manager.GetElement<Image>("IconCardUse2").color = color;
            Manager.GetElement<Image>("IconCardUse3").color = color;

            for (int i = 1; i <= count && i < 4; i++)
                Manager.GetElement("IconCardUse" + i).SetActive(true);
            for (int i = count + 1; i < 4; i++)
                Manager.GetElement("IconCardUse" + i).SetActive(false);
        }

        private void ShowThis()
        {
            var cards = Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().CardCollectionView.printedCards;
            int index = 0;
            for (int i = 0; i < cards.Count; i++)
                if (cards[i] == card.Id)
                {
                    index = i;
                    break;
                }

            Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().ShowDetail(cards, index);
        }

        #region Drag

        private RectTransform dragTarget;
        private Vector2 dragStartPosition;
        private bool dragProcessing;
        private bool draging;
        private bool dragIni;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            cardCollectionView.superScrollView.scrollRect.OnBeginDrag(eventData);
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
                ServantUI.DeckEditorUI deckEditorUI = Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>();
                if (!dragIni)
                {
                    dragTarget = deckEditorUI.DragCard;
                    dragTarget.gameObject.SetActive(true);
                    dragTarget.GetChild(0).GetComponent<RawImage>().texture
                        = ImageHandler.RawImage.texture;
                    dragTarget.GetChild(0).GetComponent<RawImage>().material
                        = ImageHandler.RawImage.material;
                    dragIni = true;

                    deckEditorUI.CardCollectionView.SetDropAreaActive(false);
                    deckEditorUI.DeckView.SetCollectionDragHoverState(card);
                    UIHover.HoveringLabel = string.Empty;
                    Runtime.UserInput.Draging = true;
                }

                RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    dragTarget, eventData.position, eventData.enterEventCamera, out var position);
                dragTarget.position = position;
                var anchoredPositon = dragTarget.anchoredPosition3D;
                anchoredPositon.z = -10f;
                dragTarget.anchoredPosition3D = anchoredPositon;
                deckEditorUI.DeckView.RefreshCollectionDragHoverState(card);
            }
            else
            {
                cardCollectionView.superScrollView.scrollRect.OnDrag(eventData);
                draging = NeedStartDrag(dragStartPosition, eventData.position);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            cardCollectionView.superScrollView.scrollRect.OnEndDrag(eventData);
            dragProcessing = false;

            if (draging)
            {
                Runtime.UserInput.Draging = false;
                ServantUI.DeckEditorUI deckEditorUI = Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>();
                deckEditorUI.CardCollectionView.SetDropAreaActive(true);
                dragTarget.gameObject.SetActive(false);
                deckEditorUI.AddCardFromCollection(card);
                deckEditorUI.DeckView.HideDeckLocationTable();
            }
        }

        private Vector3 lastDragStartPosition;
        private bool diffYOverLimit;
        private bool NeedStartDrag(Vector3 startPosition, Vector3 position)
        {
            if (!NeedDrag()) return false;

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

        private bool NeedDrag()
        {
            return Controllers.DeckEditor.condition != Controllers.DeckEditor.Condition.ChangeSide;
        }

        #endregion

        #region Navigation

        protected override int GetButtonsCount()
        {
            return Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().CardCollectionView.superScrollView.items.Count;
        }

        protected override int GetColumnsCount()
        {
            return 6;
        }

        protected override void OnNavigationLeftBorder()
        {
            base.OnNavigationLeftBorder();
            Runtime.Program.instance.deckEditor.SelectNearestDeckViewItem(transform.GetChild(0).position);
        }

        protected override void OnNavigationUpBorder()
        {
            base.OnNavigationUpBorder();
            var target = Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().CardCollectionView.GetUpNavigationObject();
            if(target != null)
            {
                Runtime.UserInput.NextSelectionIsAxis = true;
                EventSystem.current.SetSelectedGameObject(target);
            }
        }

        #endregion
    }
}
