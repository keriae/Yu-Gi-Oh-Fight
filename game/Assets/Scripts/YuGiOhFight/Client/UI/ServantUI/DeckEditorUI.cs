using Cysharp.Threading.Tasks;
using DG.Tweening;


using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static YuGiOhFight.Client.Controllers.DeckEditor;


namespace YuGiOhFight.Client.UI.ServantUI
{
    public class DeckEditorUI : ServantUI
    {

        #region Elements

        private const string LABEL_MONO_DECKVIEW = "DeckView";
        private DeckView m_DeckView;
        public DeckView DeckView =>
            m_DeckView = m_DeckView != null ? m_DeckView
            : Manager.GetElement<DeckView>(LABEL_MONO_DECKVIEW);

        private const string LABEL_MONO_CARDCOLLECTIONVIEW = "CardCollectionView";
        private CardCollectionView m_CardCollectionView;
        public CardCollectionView CardCollectionView =>
            m_CardCollectionView = m_CardCollectionView != null ? m_CardCollectionView
            : Manager.GetElement<CardCollectionView>(LABEL_MONO_CARDCOLLECTIONVIEW);

        private const string LABEL_MONO_CARDDETAILVIEW = "CardDetailView";
        private CardDetailView m_CardDetailView;
        public CardDetailView CardDetailView =>
            m_CardDetailView = m_CardDetailView != null ? m_CardDetailView
            : Manager.GetElement<CardDetailView>(LABEL_MONO_CARDDETAILVIEW);

        private const string LABEL_MONO_CARDACTIONMENU = "CardActionMenu";
        private CardActionMenu m_CardActionMenu;
        public CardActionMenu CardActionMenu =>
            m_CardActionMenu = m_CardActionMenu != null ? m_CardActionMenu
            : Manager.GetElement<CardActionMenu>(LABEL_MONO_CARDACTIONMENU);

        private const string LABEL_RT_DRAGCARD = "DragCard";
        private RectTransform m_DragCard;
        public RectTransform DragCard =>
        m_DragCard = m_DragCard != null ? m_DragCard
            : Manager.GetElement<RectTransform>(LABEL_RT_DRAGCARD);

        private const string LABEL_RIMG_DRAGCARDIMAGE = "DragCard/ImageCard";
        private RawImage m_DragCardImage;
        public RawImage DragCardImage =>
            m_DragCardImage = m_DragCardImage != null ? m_DragCardImage
            : Manager.GetNestedElement<RawImage>(LABEL_RIMG_DRAGCARDIMAGE);

        #region Header

        private const string LABEL_SBN_BACK = "Header/ButtonBack";
        private SelectionButton m_ButtonBack;
        private SelectionButton ButtonBack =>
            m_ButtonBack = m_ButtonBack != null ? m_ButtonBack
            : Manager.GetNestedElement<SelectionButton>(LABEL_SBN_BACK);
        private RectTransform m_RectBack;
        private Sequence backButtonSequence;
        private RectTransform RectBack =>
            m_RectBack = m_RectBack != null ? m_RectBack
            : ButtonBack.GetComponent<RectTransform>();

        private const string LABEL_SBN_INFO = "Header/ButtonInfoSwitching";
        private SelectionButton m_ButtonInfo;
        private SelectionButton ButtonInfo =>
            m_ButtonInfo = m_ButtonInfo != null ? m_ButtonInfo
            : Manager.GetNestedElement<SelectionButton>(LABEL_SBN_INFO);

        private const string LABEL_SBN_REGULATION = "Header/ButtonRegulation";
        private SelectionButton m_ButtonRegulation;
        private SelectionButton ButtonRegulation =>
            m_ButtonRegulation = m_ButtonRegulation != null ? m_ButtonRegulation
            : Manager.GetNestedElement<SelectionButton>(LABEL_SBN_REGULATION);

        private const string LABEL_SBN_TEST = "Header/ButtonTest";
        private SelectionButton m_ButtonTest;
        private SelectionButton ButtonTest =>
            m_ButtonTest = m_ButtonTest != null ? m_ButtonTest
            : Manager.GetNestedElement<SelectionButton>(LABEL_SBN_TEST);

        private const string LABEL_SBN_SAVE = "Header/ButtonSave";
        private SelectionButton m_ButtonSave;
        private SelectionButton ButtonSave =>
            m_ButtonSave = m_ButtonSave != null ? m_ButtonSave
            : Manager.GetNestedElement<SelectionButton>(LABEL_SBN_SAVE);

        private const string LABEL_SBN_CHANGESIDE = "Header/ButtonChangeSide";
        private SelectionButton m_ButtonChangeSide;
        private SelectionButton ButtonChangeSide =>
            m_ButtonChangeSide = m_ButtonChangeSide != null ? m_ButtonChangeSide
            : Manager.GetNestedElement<SelectionButton>(LABEL_SBN_CHANGESIDE);

        #endregion

        #region Over Head

        private const string LABEL_SBN_APPEARANCE = "AppearanceGroup";
        private SelectionButton m_ButtonAppearance;
        private SelectionButton ButtonAppearance =>
            m_ButtonAppearance = m_ButtonAppearance != null ? m_ButtonAppearance
            : Manager.GetNestedElement<SelectionButton>(LABEL_SBN_APPEARANCE);

        private const string LABEL_IMG_ICONCASE = "OverHeader/IconCase";
        private Image m_IconCase;
        public Image IconCase =>
            m_IconCase = m_IconCase != null ? m_IconCase
            : Manager.GetNestedElement<Image>(LABEL_IMG_ICONCASE);

        private const string LABEL_IMG_ICONPROTECTOR = "OverHeader/IconProtector";
        private Image m_IconProtector;
        public Image IconProtector =>
            m_IconProtector = m_IconProtector != null ? m_IconProtector
            : Manager.GetNestedElement<Image>(LABEL_IMG_ICONPROTECTOR);

        private const string LABEL_IMG_ICONFIELD = "OverHeader/IconField";
        private Image m_IconField;
        public Image IconField =>
            m_IconField = m_IconField != null ? m_IconField
            : Manager.GetNestedElement<Image>(LABEL_IMG_ICONFIELD);

        private const string LABEL_IMG_ICONGRAVE = "OverHeader/IconGrave";
        private Image m_IconGrave;
        public Image IconGrave =>
            m_IconGrave = m_IconGrave != null ? m_IconGrave
            : Manager.GetNestedElement<Image>(LABEL_IMG_ICONGRAVE);

        private const string LABEL_IMG_ICONSTAND = "OverHeader/IconStand";
        private Image m_IconStand;
        public Image IconStand =>
            m_IconStand = m_IconStand != null ? m_IconStand
            : Manager.GetNestedElement<Image>(LABEL_IMG_ICONSTAND);

        private const string LABEL_IMG_ICONMATE = "OverHeader/IconMate";
        private Image m_IconMate;
        public Image IconMate =>
            m_IconMate = m_IconMate != null ? m_IconMate
            : Manager.GetNestedElement<Image>(LABEL_IMG_ICONMATE);

        #endregion

        #endregion

        private bool gotoAppearance;
        private static bool deckLiked;
        public bool callExit;
        internal Action ClientSave { get; set; }
        internal Transform ClientOverlayParent { get; set; }
        private readonly List<Action> bookmarkCardMenuActions = new();
        private int bookmarkCardMenuCode;

        public enum ResponseRegion
        {
            Deck,
            Collection,
            Action
        }
        private ResponseRegion m_ResponseRegion;
        public ResponseRegion _ResponseRegion
        {
            get { return m_ResponseRegion; }
            set
            {
                m_ResponseRegion = value;
                ShiftToResponseRegion();
            }
        }

        private void Awake()
        {
            InitializeCardActionMenu();
            InitializeDeckView();
            InitializeCardDetailView();
            InitializeCardCollectionView();
            InitializeHeader();
            InitializeOverHeader();
        }

        private void ShiftToResponseRegion()
        {
            DeckView.SetCursor(_ResponseRegion == ResponseRegion.Deck);
            CardCollectionView.SetCursor(_ResponseRegion == ResponseRegion.Collection);
        }

        public override void ShowEvent()
        {
            base.ShowEvent();

            if (!gotoAppearance && !Controllers.RoomServant.FromHandTest)
            {
                if (condition != Condition.ChangeSide)
                    ShowBackButton();
                else
                    HideBackButton();
            }
            else
            {
                gotoAppearance = false;
                Controllers.RoomServant.FromHandTest = false;
                ShowBackButton();
            }

            Runtime.UIManager.SetCanvasMatch(Runtime.Program.instance.deckEditor.GetCanvasMatch(), 0.45f);
        }

        protected override void HideEvent()
        {
            base.HideEvent();
            Runtime.UIManager.SetCanvasMatch(1f, 0.4f);

            HideBackButton();
            MDPro3.CardRarity.Save();
        }

        protected override void AfterHideEvent()
        {
            base.AfterHideEvent();

            if(!gotoAppearance && !Controllers.RoomServant.FromHandTest)
                Dispose();
        }

        private void Dispose()
        {
            backButtonSequence?.Kill();
            backButtonSequence = null;
            Destroy(gameObject);
            if (loadOnlineDeckCoroutine != null)
                StopCoroutine(loadOnlineDeckCoroutine);
            callExit = false;
            deckLiked = false;
        }

        #region Action Menu

        private void InitializeCardActionMenu()
        {
            CardActionMenu.SetRelatedCardEvent(() =>
            {
                CardActionMenu.blockMark = ResponseRegion.Collection;
                CardActionMenu.Hide();
                ShowRelatedCard(CardActionMenu.Card);
            });
        }

        public void ShowCardActionMenu()
        {
            if (_ResponseRegion == ResponseRegion.Deck
                && Runtime.Program.instance.deckEditor.lastSelectedCardInDeck != null)
            {
                var list = new List<MDPro3.Duel.YGOSharp.Card>();
                var index = 0;
                for (int i = 0; i < DeckView.cards.Count; i++)
                {
                    list.Add(DeckView.cards[i].Card);
                    if (DeckView.cards[i] == Runtime.Program.instance.deckEditor.lastSelectedCardInDeck)
                        index = i;
                }
                CardActionMenu.Show(list, index, _ResponseRegion);
                _ResponseRegion = ResponseRegion.Action;
            }
            else if (_ResponseRegion == ResponseRegion.Collection)
            {
                if (CardCollectionView.printedCards == null
                    || CardCollectionView.printedCards.Count == 0)
                    return;
                if (Runtime.Program.instance.deckEditor.lastSelectedCardInCollection == null
                    || !Runtime.Program.instance.deckEditor.lastSelectedCardInCollection.selected)
                    return;
                var list = new List<MDPro3.Duel.YGOSharp.Card>();
                var index = 0;
                for (int i = 0; i < CardCollectionView.printedCards.Count; i++)
                {
                    if (Runtime.Program.instance.deckEditor.lastSelectedCardInCollection.card.Id == CardCollectionView.printedCards[i])
                    {
                        index = i;
                        break;
                    }
                }
                CardActionMenu.Show(CardCollectionView.printedCards, index, _ResponseRegion);
                _ResponseRegion = ResponseRegion.Action;
            }
        }

        #endregion

        #region Detail View

        private void InitializeCardDetailView()
        {
            if (CardDetailView == null)
                return;

            CardDetailView.SetRelatedCardEvent(() =>
            {
                ShowRelatedCard(CardDetailView.Card);
            });
        }

        public void ShowDetail(MDPro3.Duel.YGOSharp.Card data)
        {
            if (CardDetailView != null)
                CardDetailView.ShowCard(data);
        }

        public void ShowDetail(List<int> cards, int index)
        {
            if (CardDetailView != null)
                CardDetailView.ShowCard(cards, index);
        }

        public void ChangeRarity(MDPro3.CardRarity.Rarity rarity)
        {
            var code = 0;
            if (_ResponseRegion == ResponseRegion.Action)
                code = CardActionMenu.Card.Id;
            else if (CardDetailView != null)
                code = CardDetailView.Card.Id;
            MDPro3.CardRarity.SetRarity(code, rarity);
            UpdateRarity(code);
        }

        private void UpdateRarity(int code)
        {
            if (CardDetailView != null)
                CardDetailView.RefreshRarity(code);
            if (CardActionMenu.showing)
                CardActionMenu.RefreshRarity(code);
            CardCollectionView.RefreshRarity(code);
            DeckView.RefreshRarity(code);
        }

        #endregion

        #region Deck View

        private void InitializeDeckView()
        {
            DeckView.SetNoItemButtonNavigationEvent(MoveDirection.Right, () =>
            {
                Runtime.UserInput.NextSelectionIsAxis = true;
                Runtime.Program.instance.deckEditor.SelectLastCollectionViewItem();
            });
            DeckView.GetInputField().onEndEdit.AddListener((string text) =>
            {
                Runtime.Program.instance.deckEditor.SelectLastDeckViewItem();
            });
            DeckView.Condition editConditon = DeckView.Condition.Editable;
            if (condition == Condition.OnlineDeck
                || condition == Condition.ReplayDeck
                || condition == Condition.RecommendationDeck)
                editConditon = DeckView.Condition.NonEditable;
            else if (condition == Condition.ChangeSide)
                editConditon = DeckView.Condition.ChangeSide;

            DeckView.ButtonDeck.SetClickEvent(OnDeckButtonClicked);
            if (Deck == null)
                loadOnlineDeckCoroutine = StartCoroutine(LoadOnlineDeckAsync());
            else
                DeckView.PrintDeck(Deck, DeckName, editConditon);
            SetDeckButtonText();
        }

        private Coroutine loadOnlineDeckCoroutine;
        private IEnumerator LoadOnlineDeckAsync()
        {
            var task = MDPro3.Net.OnlineDeck.GetDeck(onlineDeckID);
            while (!task.IsCompleted)
                yield return null;
            MDPro3.Net.OnlineDeck.OnlineDeckData onlineDeckData = task.Result;
            if (onlineDeckData == null)
            {
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("网络异常，获取在线卡组失败。"));
                yield break;
            }

            DeckName = onlineDeckData.deckName;
            Deck = new MDPro3.Duel.YGOSharp.Deck(onlineDeckData.deckYdk, onlineDeckData.deckContributor);
            InitializeDeckView();

            loadOnlineDeckCoroutine = null;
        }

        private void RefreshShowingCardCount()
        {
            if (CardDetailView != null)
                CardDetailView.SetCardCount();
            CardCollectionView.RefreshCardCount();
            if (_ResponseRegion == ResponseRegion.Action)
                CardActionMenu.SetCardCount();
        }

        /// <summary>
        /// +1按钮、CardInDeck中键添加卡片
        /// </summary>
        public void AddCard(MDPro3.Duel.YGOSharp.Card data)
        {
            bool playAnimation = _ResponseRegion != ResponseRegion.Action;
            if (!DeckView.AddCard(data, playAnimation, playAnimation))
                return;
            AddHistoryCard(data.Id);
            RefreshShowingCardCount();
        }

        /// <summary>
        /// 拖动CollectionCard添加进卡组
        /// </summary>
        /// <param name="code"></param>
        public void AddCardFromCollection(MDPro3.Duel.YGOSharp.Card data)
        {
            if (!DeckView.AddCardFromPosition(data, GetDragCardPositon()))
                return;
            if (CardCollectionView.area != CardCollectionView.Area.History)
                AddHistoryCard(data.Id);
            RefreshShowingCardCount();
        }

        /// <summary>
        /// 右击CollectionCard添加进卡组
        /// </summary>
        /// <param name="code"></param>
        /// <param name="position"></param>
        public void AddCardFromCollection(MDPro3.Duel.YGOSharp.Card data, Vector3 position)
        {
            if (!DeckView.AddCardFromPositionWithSequence(data, position))
                return;
            if (CardCollectionView.area != CardCollectionView.Area.History)
                AddHistoryCard(data.Id);
            RefreshShowingCardCount();
        }

        /// <summary>
        /// -1按钮删除卡片
        /// </summary>
        /// <param name="data"></param>
        public void RemoveCard(MDPro3.Duel.YGOSharp.Card data)
        {
            if (condition == Condition.ChangeSide)
                return;

            SelectionButton_CardInDeck card = DeckView.GetCardByData(data);
            if (card == null)
            {
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("无法删除更多卡片"));
                return;
            }

            RemoveCardWithAnimation(card);
        }

        /// <summary>
        /// -1按钮 调用此方法
        /// CardInDeck右键删除卡片
        /// </summary>
        /// <param name="card"></param>
        public void RemoveCardWithAnimation(SelectionButton_CardInDeck card)
        {
            bool needSelect = _ResponseRegion != ResponseRegion.Action;
            if (!DeckView.RemoveCard(card, needSelect, true, false))
                return;
            AddHistoryCard(card.Card.Id);

            Runtime.AudioManager.PlaySE("SE_DECK_MINUS");

            card.transform.SetParent(ClientOverlayParent != null ? ClientOverlayParent : transform, true);
            card.transform.localScale = new Vector3(2f, 2f, 1f);
            var cg = card.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            RefreshShowingCardCount();

            if (needSelect)
            {
                var endPostion = CardCollectionView.GetRubbishBinPositon();
                endPostion.z -= 1f;
                var startPostion = card.transform.position;
                startPostion.z = endPostion.z;
                card.transform.position = startPostion;

                DOTween.Sequence()
                    .Append(card.transform.DOMove(endPostion, 0.4f).SetEase(Ease.OutCubic))
                    .Append(card.transform.DOScale(1f, 0.2f).SetEase(Ease.InCubic))
                    .Join(cg.DOFade(0f, 0.2f))
                    .OnComplete(() =>
                    {
                        Destroy(card.gameObject);
                    });
            }
            else
            {
                Destroy(card.gameObject);
            }
        }

        /// <summary>
        /// 拖动Card到DropArea删除卡片
        /// </summary>
        /// <param name="card"></param>
        public void RemoveCardByDrag(SelectionButton_CardInDeck card)
        {
            if (!DeckView.RemoveCard(card, false, true, true))
                return;
            AddHistoryCard(card.Card.Id);
            PlayDragCardShrinkAnimation();
        }

        public void DragCardTo(SelectionButton_CardInDeck dragCard)
        {
            if (!DeckView.deckLoaded) return;
            if (condition != Condition.ChangeSide
            && !DeckView.CanEditCard()) return;

            var position = GetDragCardPositon();
            SelectionButton_CardInDeck hoverCard = DeckView.GetHoveringCard();

            DeckView.DeckLocation location = DeckView.DeckLocation.All;
            if (hoverCard == null)
            {
                if (UIHover.HoveringLabel == UIHover.LABEL_REMOVEDECK)
                {
                    RemoveCardByDrag(dragCard);
                    return;
                }
                else if (UIHover.HoveringLabel == UIHover.LABEL_ADDBOOKMARK)
                {
                    BookmarkCard(dragCard.Card.Id);
                    PlayDragCardShrinkAnimation();
                    return;
                }
                else if (UIHover.HoveringLabel == UIHover.LABEL_CANNOTADDBOOKMARK)
                {
                    if (TryRemoveCardFromBook(dragCard.Card.Id))
                        PlayDragCardShrinkAnimation();
                    return;
                }
                else if (UIHover.HoveringLabel == UIHover.LABEL_MAINDECK)
                    location = DeckView.DeckLocation.MainDeck;
                else if (UIHover.HoveringLabel == UIHover.LABEL_EXTRADECK)
                    location = DeckView.DeckLocation.ExtraDeck;
                else if (UIHover.HoveringLabel == UIHover.LABEL_SIDEDECK)
                    location = DeckView.DeckLocation.SideDeck;
                else
                {
                    dragCard.MoveToParent(position);
                    return;
                }
            }
            else
                location = hoverCard.location;

            if (!DeckView.CanSwitchPosition(dragCard.Card, location))
            {
                dragCard.MoveToParent(position);
                return;
            }

            if (hoverCard == null)
            {
                if (dragCard.location == location)
                    dragCard.MoveToParent(position);
                else
                    DeckView.MoveCardToLocation(dragCard, location, GetDragCardPositon());
            }
            else
                DeckView.MoveCardToLocationWithSiblingIndex(dragCard, location
                    , hoverCard.transform.GetSiblingIndex(), GetDragCardPositon());
        }

        public void CardChangeSide(SelectionButton_CardInDeck card)
        {
            DeckView.DeckLocation location = DeckView.DeckLocation.SideDeck;
            if (card.location == DeckView.DeckLocation.SideDeck)
                location = card.Card.IsExtraCard() ? DeckView.DeckLocation.ExtraDeck : DeckView.DeckLocation.MainDeck;
            DeckView.MoveCardToLocation(card, location, card.transform.position);
        }

        public Vector3 GetDragCardPositon()
        {
            return DragCard.position;
        }

        public void OnDeckButtonClicked()
        {
            if (!DeckView.deckLoaded) return;

            if (!DeckView.ButtonDeck.gameObject.activeSelf)
                return;

            if (condition == Condition.EditDeck && DeckIsFromLocal && Services.MyCard.account == null)
            {
                OnDeckAcademy();
                return;
            }

            if (!DeckIsFromLocal && condition == Condition.OnlineDeck)
            {
                MDPro3.Net.OnlineDeck.LikeDeck(onlineDeckID);
                deckLiked = true;
                SetDeckButtonText();
                return;
            }

            if (DeckView.GetDirty() || !DeckIsFromLocal)
            {
                if (condition != Condition.ChangeSide)
                    Runtime.MessageManager.Toast(MDPro3.InterString.Get("请先保存卡组"));
                return;
            }

            if (Services.MyCard.account != null && MDPro3.Config.GetBool("SyncDeck", true))
            {
                MDPro3.Net.OnlineDeck.OnlineDeckData onlineDeck = MDPro3.Net.OnlineDeck.GetByID(Deck.deckId);
                if (onlineDeck == null || onlineDeck.isDelete)
                    return;
                _ = MDPro3.Net.OnlineDeck.UpdatePublicState(Deck.deckId, !onlineDeck.isPublic);
                onlineDeck.isPublic = !onlineDeck.isPublic;
            }

            SetDeckButtonText();
        }

        private void SetDeckButtonText()
        {
            string text = string.Empty;
            if (condition == Condition.EditDeck && DeckIsFromLocal && Services.MyCard.account == null)
            {
                DeckView.ButtonDeck.SetButtonText("展开教程");
                DeckView.ButtonDeck.gameObject.SetActive(true);
                return;
            }
            if (DeckIsFromLocal)
            {
                if (Services.MyCard.account != null)
                {
                    if(MDPro3.Config.GetBool("SyncDeck", true))
                    {
                        MDPro3.Net.OnlineDeck.OnlineDeckData onlineDeck = MDPro3.Net.OnlineDeck.GetByID(Deck.deckId);
                        if (onlineDeck == null || onlineDeck.isDelete)
                        {
                            DeckView.ButtonDeck.gameObject.SetActive(false);
                            return;
                        }
                        else
                        {
                            if (onlineDeck.isPublic)
                                text = MDPro3.InterString.Get("公开中");
                            else
                                text = MDPro3.InterString.Get("非公开中");
                        }
                    }
                    else
                    {
                        text = MDPro3.InterString.Get("停止同步中");
                    }
                }
            }
            else
            {
                if (condition == Condition.OnlineDeck)
                {
                    text = MDPro3.InterString.Get("点赞");
                    if (deckLiked)
                    {
                        DeckView.ButtonDeck.gameObject.SetActive(false);
                        return;
                    }
                }
            }
            DeckView.ButtonDeck.SetButtonText(text);
            DeckView.ButtonDeck.gameObject.SetActive(text != string.Empty);
        }

        public void OnDeckAcademy()
        {
            if (condition == Condition.RecommendationDeck)
            {
                if (Recommendation?.Academy != null)
                    Runtime.UIManager.ShowPopupDeckAcademy(false, Recommendation.Academy);
                return;
            }
            Runtime.UIManager.ShowPopupDeckAcademy();
        }

        public void LoadAcademyDeck(string name)
        {
            if (!Services.DeckAcademy.CanLeaveEditor())
                return;
            MDPro3.Config.SetConfigDeck(name);
            Runtime.Program.instance.deckEditor.SwitchCondition(Condition.EditDeck);
            CardCollectionView.historyCards = historyCards;
            DeckView.PrintDeck(Deck, DeckName, DeckView.Condition.Editable);
            SetDeckButtonText();
            ButtonRegulation.SetButtonText(Banlist.Name);
            RefreshRegulationIcons();
            _ = RefreshOverHeaderIconsAsync();
        }


        #endregion

        #region Card Collection View

        private void InitializeCardCollectionView()
        {
            CardCollectionView.historyCards = historyCards;
            CardCollectionView.SetNoItemButtonNavigationEvent(MoveDirection.Left, () =>
            {
                Runtime.UserInput.NextSelectionIsAxis = true;
                Runtime.Program.instance.deckEditor.SelectLastDeckViewItem();
            });
        }

        public void BookmarkCard(int code)
        {
            MDPro3.CardRarity.BookmarkCard(code);
            RefreshBookmarkViews();
        }

        public void UnbookmarkCard(int code)
        {
            MDPro3.CardRarity.UnbookmarkCard(code);
            RefreshBookmarkViews();
        }

        public bool TryRemoveCardFromBook(int code)
        {
            if (!CardCollectionView.TryRemoveCardFromBook(code))
                return false;

            RefreshBookmarkViews();
            return true;
        }

        public void ShowBookmarkListSelection(int code)
        {
            bookmarkCardMenuCode = code;
            bookmarkCardMenuActions.Clear();

            var selections = new List<string>
            {
                MDPro3.InterString.Get("收藏列表"),
                MDPro3.InterString.Get("选择一个列表来添加或移除卡片")
            };

            var added = MDPro3.CardRarity.CardInBookmarkList(code, string.Empty);
            AddBookmarkCardMenuEntry(
                selections,
                () => ToggleCardBookmarkList(string.Empty),
                MDPro3.InterString.Get("默认收藏列表"),
                added ? "g" : string.Empty);

            foreach (var listName in MDPro3.CardRarity.GetBookmarkListNames())
            {
                var capturedName = listName;
                var added2 = MDPro3.CardRarity.CardInBookmarkList(code, capturedName);
                AddBookmarkCardMenuEntry(
                    selections,
                    () => ToggleCardBookmarkList(capturedName),
                    capturedName,
                    added2 ? "g" : string.Empty);
            }

            AddBookmarkCardMenuEntry(selections, CreateBookmarkListForCard, MDPro3.InterString.Get("新建列表并加入"), "b");
            Runtime.UIManager.ShowPopupSelection(selections, OnBookmarkCardMenuSelection);
        }

        private void AddBookmarkCardMenuEntry(List<string> selections, Action action, string label, string color = "")
        {
            bookmarkCardMenuActions.Add(action);
            selections.Add(string.IsNullOrEmpty(color) ? label : $"{color}:{label}");
        }

        private void OnBookmarkCardMenuSelection()
        {
            if (EventSystem.current.currentSelectedGameObject == null)
                return;

            var index = EventSystem.current.currentSelectedGameObject.transform.GetSiblingIndex();
            if (index < 0 || index >= bookmarkCardMenuActions.Count)
                return;

            bookmarkCardMenuActions[index]?.Invoke();
        }

        private void ToggleCardBookmarkList(string listName)
        {
            var result = MDPro3.CardRarity.ToggleCardTo(bookmarkCardMenuCode, listName);
            if(result == 1)
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("已加入收藏列表「[?]」。", listName));
            else if(result == 2)
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("已从收藏列表「[?]」移除。", listName));

            RefreshBookmarkViews();
        }

        private void CreateBookmarkListForCard()
        {
            var selections = new List<string>
            {
                MDPro3.InterString.Get("新建收藏列表"),
                string.Empty
            };
            Runtime.UIManager.ShowPopupInput(selections, CreateBookmarkListForCard, null, TmpInputValidation.ValidationType.None);
        }

        private void CreateBookmarkListForCard(string listName)
        {
            if (!TryNormalizeBookmarkListName(listName, out var normalized))
                return;
            if (!MDPro3.CardRarity.AddBookmarkList(normalized))
            {
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("该收藏列表已存在！"));
                return;
            }

            MDPro3.CardRarity.ToggleCardTo(bookmarkCardMenuCode, normalized);
            if (CardCollectionView.area == CardCollectionView.Area.Bookmark)
                CardCollectionView.ShowBookmarkList(normalized);
            RefreshBookmarkViews();
        }

        private bool TryNormalizeBookmarkListName(string listName, out string normalized)
        {
            normalized = listName == null ? string.Empty : listName.Trim();
            if (normalized == string.Empty)
            {
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("收藏列表名不能为空！"));
                return false;
            }
            if (normalized.Contains(":"))
            {
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("收藏列表名不能包含“:”！"));
                return false;
            }
            if (normalized.Contains("\n") || normalized.Contains("\r"))
            {
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("收藏列表名不能换行！"));
                return false;
            }
            if (MDPro3.CardRarity.BookmarkListExists(normalized))
            {
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("该收藏列表已存在！"));
                return false;
            }

            return true;
        }

        private void RefreshBookmarkViews()
        {
            CardCollectionView.RefreshBookmarkTab();
            if (CardDetailView != null)
                CardDetailView.RefreshBookmarkToggle();
            if (_ResponseRegion == ResponseRegion.Action)
                CardActionMenu.RefreshBookmarkToggle();
            if (CardCollectionView.area == CardCollectionView.Area.Bookmark)
                CardCollectionView.PrintBookmarkCards();
        }

        public void AddHistoryCard(int code)
        {
            if (condition == Condition.ChangeSide)
                return;
            CardCollectionView.AddHistoryCard(code);
        }

        public void AddHistoryCards(List<int> codes)
        {
            if (condition == Condition.ChangeSide)
                return;
            CardCollectionView.AddHistoryCards(codes);
        }

        public bool NeedAddCardToHistoryWhenClick()
        {
            return CardCollectionView.area != CardCollectionView.Area.History;
        }

        public void ShowRelatedCard(MDPro3.Duel.YGOSharp.Card data)
        {
            CardCollectionView.ShowRelatedCard(data);
        }

        public void HideRelatedCard()
        {
            CardCollectionView.HideRelatedCard();
        }

        #endregion

        #region Header

        private void InitializeHeader()
        {
            ButtonRegulation.SetButtonText(Banlist.Name);

            if (condition == Condition.ChangeSide)
            {
                Destroy(ButtonTest.gameObject);
                Destroy(ButtonSave.gameObject);
            }
            else
            {
                Destroy(ButtonChangeSide.gameObject);
                RefreshHeaderActions();
            }
        }

        private void RefreshHeaderActions()
        {
            var recommendation = condition == Condition.RecommendationDeck;
            ButtonSave.SetButtonText(recommendation ? "复制到我的" : MDPro3.InterString.Get("保存"));
            ButtonTest.SetButtonText(recommendation ? Recommendation?.TutorialLabel ?? "查看教程" : MDPro3.InterString.Get("手牌测试"));
            ButtonTest.gameObject.SetActive(!recommendation || Recommendation?.Academy != null);
        }

        public void SetCardInfoType()
        {
            CardInfoType type = (CardInfoType)(((int)cardInfoType + 1) % 4);
            Runtime.Program.instance.deckEditor.SetCardInfoType(type);
        }

        public void SetCardInfoType(CardInfoType type)
        {
            Runtime.AudioManager.PlaySE("SE_MENU_SELECT_01");
            switch (cardInfoType)
            {
                case CardInfoType.None:
                    Runtime.MessageManager.Toast(MDPro3.InterString.Get("切换到简单显示"));
                    break;
                case CardInfoType.Detail:
                    Runtime.MessageManager.Toast(MDPro3.InterString.Get("切换到详情显示"));
                    break;
                case CardInfoType.Pool:
                    Runtime.MessageManager.Toast(MDPro3.InterString.Get("切换到归属显示"));
                    break;
                case CardInfoType.Genesys:
                    Runtime.MessageManager.Toast(MDPro3.InterString.Get("切换到Genesys积分显示"));
                    break;
            }
            DeckView.SetCardInfoType(type);
            CardCollectionView.SetCardInfoType();
            if (CardDetailView != null)
                CardDetailView.RefreshRegulation();
            ButtonRegulation.SetButtonText(Banlist.Name);
        }

        private void RefreshRegulationIcons()
        {
            foreach (SelectionButton_CardInDeck card in DeckView.cards)
            {
                card.SetRegulationIcon();
                card.RefreshCredit();
            }
            foreach (var go in CardCollectionView.superScrollView.gameObjects)
            {
                SelectionButton_CardInCollection mono = go.GetComponent<SelectionButton_CardInCollection>();
                mono.SetRegulationIcon();
                mono.RefreshCredit();
            }
            if (CardDetailView != null)
                CardDetailView.RefreshRegulation();
        }

        public void OnRegulation()
        {
            Runtime.AudioManager.PlaySE("SE_MENU_DECIDE");
            List<string> selections = new()
            {
                MDPro3.InterString.Get("禁限卡表"),
                string.Empty
            };
            var names = cardInfoType == CardInfoType.Genesys ?
                MDPro3.Duel.YGOSharp.BanlistManager.GetAllGenesysNames() : MDPro3.Duel.YGOSharp.BanlistManager.GetAllNames();
            selections.AddRange(names);
            Runtime.UIManager.ShowPopupSelection(selections, ChangeRegulation);
        }

        private void ChangeRegulation()
        {
            string selected = EventSystem.current.currentSelectedGameObject
                .GetComponent<SelectionButton>().GetButtonText();
            SetClientRegulation(selected);
        }

        public void SetClientRegulation(string selected)
        {
            if(cardInfoType == CardInfoType.Genesys)
                MDPro3.Duel.YGOSharp.BanlistManager.currentGenesysBanList = MDPro3.Duel.YGOSharp.BanlistManager.GetByName(selected);
            else
                MDPro3.Duel.YGOSharp.BanlistManager.currentBanList = MDPro3.Duel.YGOSharp.BanlistManager.GetByName(selected);
            ButtonRegulation.SetButtonText(selected);
            RefreshRegulationIcons();
        }

        public void OnSubMenu()
        {
            if (!DeckView.deckLoaded) return;
            if (Client.FightClient.Instance != null && Client.FightClient.Instance.IsClientEditor)
            {
                Client.FightClient.Instance.OpenEditorMore();
                return;
            }
            if (condition == Condition.RecommendationDeck)
            {
                var recommendationMenus = new List<string> { MDPro3.InterString.Get("副菜单"), "复制到我的" };
                var recommendationActions = new List<Action> { null, CopyRecommendation };
                if (!string.IsNullOrWhiteSpace(Recommendation?.Description))
                {
                    recommendationMenus.Add("构筑思路");
                    recommendationActions.Add(() => Runtime.UIManager.ShowPopupText(new List<string> {
                        Recommendation.Name, Recommendation.Description }));
                }
                if (Recommendation?.Academy != null)
                {
                    recommendationMenus.Add(Recommendation.TutorialLabel);
                    recommendationActions.Add(OnDeckAcademy);
                }
                Runtime.UIManager.ShowSubMenu(recommendationMenus, recommendationActions);
                return;
            }
            var menus = new List<string>()
                {
                    MDPro3.InterString.Get("副菜单")
                };
            var actions = new List<Action>()
                {
                    null
                };

            if (condition != Condition.ChangeSide)
            {
                menus.Add("卡组学院 / 展开教程");
                actions.Add(OnDeckAcademy);
                menus.Add(MDPro3.InterString.Get("手牌测试设置"));
                actions.Add(OnHandTestSettings);
                menus.Add("YDKE");
                actions.Add(OnYdke);
            }

            menus.AddRange(new List<string>
            {
                MDPro3.InterString.Get("重置"),
                MDPro3.InterString.Get("打乱")
            });
            actions.AddRange(new List<Action>()
            {
                OnReset,
                OnRandom
            });

            if (condition != Condition.ChangeSide)
            {
                menus.AddRange(new List<string>
                {
                    MDPro3.InterString.Get("复制"),
                    MDPro3.InterString.Get("分享"),
                    MDPro3.InterString.Get("清空")
                });
                actions.AddRange(new List<Action>()
                {
                    OnCopy,
                    OnShare,
                    OnClearDeck
                });
            }
            Runtime.UIManager.ShowSubMenu(menus, actions);
        }

        public void OnSave()
        {
            if (ClientSave != null)
            {
                ClientSave();
                return;
            }
            if (condition == Condition.RecommendationDeck)
            {
                CopyRecommendation();
                return;
            }
            if (DeckIsFromLocal && !DeckView.GetDirty()) return;

            if (DeckIsFromLocal)
                if (Banlist.Name != MDPro3.Duel.YGOSharp.BanlistManager.EMPTY_LIST_NAME)
                {
                    if (DeckView.mainCount > 60 || DeckView.extraCount > 15 || DeckView.sideCount > 15)
                    {
                        List<string> tasks = new()
                        {
                            MDPro3.InterString.Get("保存失败"),
                            MDPro3.InterString.Get("卡组内卡片张数超过限制。@n如需无视限制，请将禁限卡表设置为无（N/A）。")
                        };
                        Runtime.UIManager.ShowPopupConfirm(tasks);
                        callExit = false;
                        return;
                    }
                }

            if (!DeckIsFromLocal && File.Exists(Runtime.Program.PATH_DECK + DeckName + Runtime.Program.EXPANSION_YDK))
            {
                List<string> tasks = new()
                {
                    MDPro3.InterString.Get("该卡组名已存在"),
                    MDPro3.InterString.Get("该卡组名的文件已存在，是否直接覆盖创建？"),
                    MDPro3.InterString.Get("覆盖"),
                    MDPro3.InterString.Get("取消")
                };

                Runtime.UIManager.ShowPopupYesOrNo(tasks, OnSaveConfirmed, () => { callExit = false; });
            }
            else
                OnSaveConfirmed();
        }

        private void OnSaveConfirmed()
        {
            if (condition == Condition.RecommendationDeck)
            {
                CopyRecommendation();
                return;
            }
            if (!DeckView.Save())
                return;

            if (callExit)
            {
                CG.blocksRaycasts = false;
                Runtime.Program.instance.deckEditor.CallExitIn(2f);
                return;
            }
            DeckIsFromLocal = true;
            SetDeckButtonText();
        }

        private void CopyRecommendation()
        {
            if (!DeckView.deckLoaded || Recommendation == null) return;
            try
            {
                var name = MDPro3.Utility.DeckLibrary.CreateEditableCopy(Recommendation);
                LoadAcademyDeck(name);
                Runtime.Program.instance.deckSelector.GetUI<DeckSelectorUI>().PrepareMineOnReturn();
                RefreshHeaderActions();
                Runtime.MessageManager.Toast("已复制到我的：" + name);
            }
            catch (Exception error) { Runtime.MessageManager.Cast("复制卡组失败：" + error.Message); }
        }

        public void OnReset()
        {
            DeckView.ResetDeck();
        }

        public void OnSort()
        {
            DeckView.Sort();
        }

        public void OnRandom()
        {
            DeckView.Randomize();
        }

        public void OnCopy()
        {
            DeckView.Copy();
        }

        public void OnShare()
        {
            DeckView.Share();
        }

        public void OnHandTest()
        {
            if (Client.FightClient.Instance != null && Client.FightClient.Instance.IsClientEditor)
            {
                Client.FightClient.Instance.OpenClientHandTest();
                return;
            }
            if (condition == Condition.RecommendationDeck)
            {
                OnDeckAcademy();
                return;
            }
            TryStartHandTest(Controllers.HandTestSettings.Load());
        }

        private void OnHandTestSettings()
        {
            if (Client.FightClient.Instance != null && Client.FightClient.Instance.IsClientEditor)
            {
                Client.FightClient.Instance.OpenClientHandTest();
                return;
            }
            Runtime.UIManager.ShowPopupHandTestSettings(Controllers.HandTestSettings.Load(), TryStartHandTest);
        }

        internal bool StartClientHandTest(Controllers.HandTestSettings settings) => TryStartHandTest(settings);

        private int handTestLaunchGeneration;
        private bool handTestLaunching;

        private void OnYdke()
        {
            Runtime.UIManager.ShowPopupYdke(OnImportYdke, OnExportYdke);
        }

        private void OnImportYdke()
        {
            MDPro3.Duel.YGOSharp.Deck importedDeck = MDPro3.YdkeConverter.Ydke2Deck(GUIUtility.systemCopyBuffer);
            if (importedDeck == null)
                return;

            DeckView.ImportCardLists(importedDeck);
        }

        private void OnExportYdke()
        {
            if (!DeckView.deckLoaded)
                return;

            var ydke = MDPro3.YdkeConverter.DeckToYdke(DeckView.FromObjectDeckToCodedDeck());
            if (string.IsNullOrEmpty(ydke))
                return;

            GUIUtility.systemCopyBuffer = ydke;
            Runtime.MessageManager.Toast(Presentation.StringHelper.Get(1480));
        }

        private bool TryStartHandTest(Controllers.HandTestSettings settings)
        {
            if (handTestLaunching)
            {
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("手牌测试正在启动，请稍候。"));
                return false;
            }
            if (!DeckView.deckLoaded)
                return false;
            if (condition == Condition.ChangeSide)
                return false;
            if (!DeckIsFromLocal)
            {
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("请先保存卡组"));
                return false;
            }
            if (!Runtime.Network.TcpHelper.canJoin || Runtime.Network.TcpHelper.tcpClient != null)
            {
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("已有对战连接，请先退出当前对战。"));
                return false;
            }

            MDPro3.Duel.YGOSharp.Deck deck = DeckView.FromObjectDeckToCodedDeck();
            Controllers.HandTestSettings launchSettings = settings != null ? settings.Clone() : Controllers.HandTestSettings.Load();
            launchSettings.Clamp();
            handTestLaunching = true;
            _ = HandTestAsync(launchSettings, deck, ++handTestLaunchGeneration, Runtime.Network.TcpHelper.ConnectionGeneration);
            return true;
        }

        private bool OwnsHandTestConnection(int generation, int connection,
            System.Net.Sockets.TcpClient transport)
            => generation == handTestLaunchGeneration && connection == Runtime.Network.TcpHelper.ConnectionGeneration
                && Controllers.RoomServant.FromHandTest && !Controllers.RoomServant.FromSolo && !Controllers.RoomServant.FromLocalHost
                && (transport == null || Runtime.Network.TcpHelper.tcpClient == null || ReferenceEquals(transport, Runtime.Network.TcpHelper.tcpClient));

        private async UniTask HandTestAsync(Controllers.HandTestSettings settings, MDPro3.Duel.YGOSharp.Deck deck, int generation, int priorConnection)
        {
            int connection = -1, port = 7911;
            bool serverStarted = false, enteredCore = false;
            System.Net.Sockets.TcpClient localClient = null;
            try
            {
                // Finish the launching UI action before asynchronous status or
                // errors arrive. Network progress below follows native state.
                await UniTask.Yield();
                if (generation != handTestLaunchGeneration || this == null
                    || Runtime.Program.instance.currentServant != Runtime.Program.instance.deckEditor
                    || priorConnection != Runtime.Network.TcpHelper.ConnectionGeneration || !Runtime.Network.TcpHelper.canJoin || Runtime.Network.TcpHelper.tcpClient != null)
                    return;
                while (!Runtime.Network.TcpHelper.IsPortAvailable(port))
                {
                    port++;
                    if (port == 65536) port = 1;
                    if (port == 7911) throw new InvalidOperationException("没有可用的本地对战端口。");
                }

                connection = Runtime.Network.TcpHelper.ConnectionGeneration;
                Controllers.RoomServant.FromSolo = false;
                Controllers.RoomServant.FromLocalHost = false;
                Controllers.RoomServant.FromHandTest = true;
                Controllers.RoomServant.SelfType = -1; Controllers.RoomServant.IsHost = false;
                MDPro3.Net.YgoServer.StartServer(settings.BuildServerArgs(port));
                serverStarted = true;
                Runtime.UIManager.ShowBlackBack(1f, 0f);
                Controllers.OcgCore.handler = Runtime.Program.instance.room.Handler;
                bool linking = Runtime.Network.TcpHelper.LinkStart("127.0.0.1", MDPro3.Config.Get("DuelPlayerName0", MDPro3.Config.EMPTY_STRING),
                    port.ToString(), string.Empty, true, null);
                connection = Runtime.Network.TcpHelper.ConnectionGeneration;
                if (!linking) throw new InvalidOperationException("已有对战连接，请先退出当前对战。");

                string expectedPort = port.ToString();
                float deadline = Time.realtimeSinceStartup + 30;
                bool readySent = false, botStarted = false, startSent = false;
                while (Time.realtimeSinceStartup < deadline)
                {
                    if (!OwnsHandTestConnection(generation, connection, localClient))
                    {
                        return;
                    }
                    if (Runtime.Program.instance.currentServant == Runtime.Program.instance.ocgcore)
                    {
                        enteredCore = startSent && Controllers.OcgCore.condition == Controllers.OcgCore.Condition.Duel
                            && Runtime.Network.TcpHelper.joinedAddress == "127.0.0.1" && Runtime.Network.TcpHelper.joinedPort == expectedPort;
                        return;
                    }
                    if (this == null || Runtime.Program.instance.currentServant != Runtime.Program.instance.deckEditor)
                        return;
                    if (!MDPro3.Net.YgoServer.ServerRunning() || Runtime.Network.TcpHelper.onDisConnected)
                        throw new InvalidOperationException("手牌测试连接已中断，请重试。");

                    var transport = Runtime.Network.TcpHelper.tcpClient;
                    bool joined = transport != null && transport.Connected && Runtime.Network.TcpHelper.joinedAddress == "127.0.0.1"
                        && Runtime.Network.TcpHelper.joinedPort == expectedPort;
                    if (joined)
                    {
                        if (localClient == null) localClient = transport;
                        if (Controllers.RoomServant.SelfType == 0 && Controllers.RoomServant.IsHost && Controllers.RoomServant.players[0] != null)
                        {
                            if (!botStarted)
                            {
                                // The real host seat also proves the server is
                                // listening before WindBot's single connection.
                                Runtime.Program.instance.solo.StartAIForHandTest(port);
                                botStarted = true;
                            }
                            if (!readySent)
                            {
                                Runtime.Network.TcpHelper.CtosMessage_UpdateDeck(deck);
                                Runtime.Network.TcpHelper.CtosMessage_HsReady(); readySent = true;
                            }
                            if (!startSent && Controllers.RoomServant.players[1] != null
                                && Controllers.RoomServant.players[0].ready && Controllers.RoomServant.players[1].ready)
                            {
                                Runtime.Network.TcpHelper.CtosMessage_HsStart(); startSent = true;
                            }
                        }
                    }
                    await UniTask.Yield();
                }
                throw new TimeoutException("手牌测试启动超时，请重试。");
            }
            catch (Exception error)
            {
                if (generation == handTestLaunchGeneration)
                {
                    Runtime.MessageManager.Cast(error.Message);
                }
            }
            finally
            {
                // A departed launch cannot retire the transport of a later
                // duel, even if both launches reuse the same local port.
                try
                {
                    if (!enteredCore && OwnsHandTestConnection(generation, connection, localClient))
                    {
                        try { if (serverStarted) MDPro3.Net.YgoServer.StopServer(); }
                        finally
                        {
                            if (serverStarted) Runtime.Network.TcpHelper.DisconnectIntentional();
                            Controllers.RoomServant.FromHandTest = false;
                            if (serverStarted) Runtime.UIManager.HideBlackBack(0f);
                        }
                    }
                }
                finally
                {
                    if (generation == handTestLaunchGeneration) handTestLaunching = false;
                }
            }
        }

        internal void OnClearDeck()
        {
            var codes = new List<int>();
            foreach (SelectionButton_CardInDeck card in DeckView.cards)
                codes.Add(card.Card.Id);
            if (!DeckView.ClearDeck())
                return;
            Runtime.AudioManager.PlaySE("SE_DECK_MINUS");
            AddHistoryCards(codes);
            RefreshShowingCardCount();
        }

        public void OnChangeSideComplete()
        {
            Controllers.OcgCore.sideChangedDeck = DeckView.FromObjectDeckToCodedDeck();
            Runtime.Network.TcpHelper.CtosMessage_UpdateDeck(Controllers.OcgCore.sideChangedDeck);
        }

        private void ShowBackButton()
        {
            backButtonSequence?.Kill();
            ButtonBack.gameObject.SetActive(true);

            RectBack.anchoredPosition3D = new Vector3(24f, 120f, 0f);
            backButtonSequence = DOTween.Sequence()
                .AppendInterval(0.6f)
                .Append(RectBack.DOAnchorPos3D(new Vector3(24f, 0f, 0f), 0.2f).SetEase(Ease.OutQuart));
        }

        private void HideBackButton()
        {
            backButtonSequence?.Kill();
            backButtonSequence = null;
            ButtonBack.gameObject.SetActive(false);
        }

        #endregion

        #region Over Header

        private void InitializeOverHeader()
        {
            if (condition == Condition.ChangeSide)
                ButtonAppearance.gameObject.SetActive(false);
            else
            {
                _ = RefreshOverHeaderIconsAsync();
            }
        }

        private async UniTask RefreshOverHeaderIconsAsync()
        {
            IconCase.color = Color.clear;
            IconProtector.color = Color.clear;
            IconField.color = Color.clear;
            IconGrave.color = Color.clear;
            IconStand.color = Color.clear;
            IconMate.color = Color.clear;

            while (Deck == null)
                await MDPro3.Utility.TaskUtility.WaitOneFrame(gameObject);

            var sprite = await Runtime.Program.items.LoadDeckCaseIconAsync(Deck.Case, "_L_SD");
            if (sprite != null)
            {
                IconCase.color = Color.white;
                IconCase.sprite = sprite;
            }

            IconProtector.material = await Runtime.ABLoader.LoadProtectorMaterial(Deck.Protector.ToString(), Application.exitCancellationToken);
            IconProtector.color = Color.white;

            IconField.sprite = await Runtime.Program.items.LoadItemIconAsync(Deck.Field.ToString(), Presentation.Assets.Items.ItemType.Mat);
            IconField.color = Color.white;

            IconGrave.sprite = await Runtime.Program.items.LoadItemIconAsync(Deck.Grave.ToString(), Presentation.Assets.Items.ItemType.Grave);
            IconGrave.color = Color.white;

            IconStand.sprite = await Runtime.Program.items.LoadItemIconAsync(Deck.Stand.ToString(), Presentation.Assets.Items.ItemType.Stand);
            IconStand.color = Color.white;

            var mate = Deck.Mate.ToString();
            if (mate.Length == 7 && mate.StartsWith("100"))
            {
                IconMate.sprite = await Runtime.Program.items.LoadItemIconAsync(mate, Presentation.Assets.Items.ItemType.Mate);
                IconMate.color = Color.white;
            }
            else
            {
                var art = await Presentation.CardImageLoader.LoadArtAsync(Deck.Mate, true);
                IconMate.sprite = Runtime.TextureManager.Texture2Sprite(art);
                IconMate.color = Color.white;
            }
        }

        public void ShiftToAppearance()
        {
            if (!DeckView.deckLoaded)
                return;
            if (condition == Condition.ChangeSide)
                return;
            if (!DeckIsFromLocal)
            {
                if (condition != Condition.ChangeSide)
                    Runtime.MessageManager.Toast(MDPro3.InterString.Get("请先保存卡组"));
                return;
            }
            gotoAppearance = true;
            Runtime.Program.instance.appearance.SwitchCondition(Controllers.Appearance.Condition.DeckEditor);
            Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.appearance);
        }

        #endregion

        private void PlayDragCardShrinkAnimation()
        {
            DragCard.gameObject.SetActive(true);
            DragCard.localScale = Vector3.one;
            DragCard.DOScale(0.5f, 0.2f).SetEase(Ease.InCubic);
            DragCardImage.DOFade(0.5f, 0.2f).SetEase(Ease.InCubic)
                .OnComplete(() =>
                {
                    DragCard.localScale = Vector3.one;
                    DragCardImage.color = Color.white;
                    DragCard.gameObject.SetActive(false);
                });
        }

    }
}
