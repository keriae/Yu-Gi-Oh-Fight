using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
{
    public class CardCollectionView : UIWidget
    {
        internal Client.FightCardPoolCache ClientImageCache { get; set; }

        #region Elements

        #region Root

        private const string LABEL_SBN_NOITEM = "NoItemButton";
        private SelectionButton m_ButtonNoItem;
        protected SelectionButton ButtonNoItem =>
            m_ButtonNoItem = m_ButtonNoItem != null ? m_ButtonNoItem
            : Manager.GetElement<SelectionButton>(LABEL_SBN_NOITEM);

        private const string LABEL_GPC_CURSORWINDOWSELECT = "CursorWindowSelect";
        private GamepadCursor m_CursorWindowSelect;
        protected GamepadCursor CursorWindowSelect =>
            m_CursorWindowSelect = m_CursorWindowSelect != null ? m_CursorWindowSelect
            : Manager.GetElement<GamepadCursor>(LABEL_GPC_CURSORWINDOWSELECT);

        #endregion

        #region TabArea

        private const string LABEL_STG_CARDLIST = "TabArea/CardListToggle";
        private SelectionToggle_CardCollectionTab m_ToggleCardList;
        protected SelectionToggle_CardCollectionTab ToggleCardList =>
            m_ToggleCardList = m_ToggleCardList != null ? m_ToggleCardList
            : Manager.GetNestedElement<SelectionToggle_CardCollectionTab>(LABEL_STG_CARDLIST);

        private const string LABEL_STG_BOOKMARK = "TabArea/BookmarkToggle";
        private SelectionToggle_CardCollectionTab m_ToggleBookmark;
        protected SelectionToggle_CardCollectionTab ToggleBookmark =>
            m_ToggleBookmark = m_ToggleBookmark != null ? m_ToggleBookmark
            : Manager.GetNestedElement<SelectionToggle_CardCollectionTab>(LABEL_STG_BOOKMARK);

        private const string LABEL_STG_HISTORY = "TabArea/HistoryToggle";
        private SelectionToggle_CardCollectionTab m_ToggleHistory;
        protected SelectionToggle_CardCollectionTab ToggleHistory =>
            m_ToggleHistory = m_ToggleHistory != null ? m_ToggleHistory
            : Manager.GetNestedElement<SelectionToggle_CardCollectionTab>(LABEL_STG_HISTORY);

        #endregion

        #region FilterAndSortArea

        private const string LABEL_GO_FILTERANDSORTAREA = "FilterAndSortArea";
        private GameObject m_FilterAndSortArea;
        protected GameObject FilterAndSortArea =>
            m_FilterAndSortArea = m_FilterAndSortArea != null ? m_FilterAndSortArea
            : Manager.GetElement(LABEL_GO_FILTERANDSORTAREA);

        private const string LABEL_IPT_SEARCH = "FilterAndSortArea/InputField";
        private SelectionInputField m_InputSearch;
        public SelectionInputField InputSearch =>
            m_InputSearch = m_InputSearch != null ? m_InputSearch
            : Manager.GetNestedElement<SelectionInputField>(LABEL_IPT_SEARCH);

        private const string LABEL_SBN_SEARCH = "FilterAndSortArea/SearchButton";
        private SelectionButton m_ButtonSearch;
        protected SelectionButton ButtonSearch =>
            m_ButtonSearch = m_ButtonSearch != null ? m_ButtonSearch
            : Manager.GetNestedElement<SelectionButton>(LABEL_SBN_SEARCH);

        private const string LABEL_STG_FILTER = "FilterAndSortArea/FilterToggle";
        private SelectionToggle_CardFilter m_ToggleFilter;
        protected SelectionToggle_CardFilter ToggleFilter =>
            m_ToggleFilter = m_ToggleFilter != null ? m_ToggleFilter
            : Manager.GetNestedElement<SelectionToggle_CardFilter>(LABEL_STG_FILTER);

        private const string LABEL_SBN_SORT = "FilterAndSortArea/SortButton";
        private SelectionButton m_ButtonSort;
        protected SelectionButton ButtonSort =>
            m_ButtonSort = m_ButtonSort != null ? m_ButtonSort
            : Manager.GetNestedElement<SelectionButton>(LABEL_SBN_SORT);

        private const string LABEL_SBN_CLEAR = "FilterAndSortArea/ClearButton";
        private SelectionButton m_ButtonClear;
        protected SelectionButton ButtonClear =>
            m_ButtonClear = m_ButtonClear != null ? m_ButtonClear
            : Manager.GetNestedElement<SelectionButton>(LABEL_SBN_CLEAR);

        #endregion

        #region RelatedArea

        private const string LABEL_GO_RELATEDAREA = "RelatedArea";
        private GameObject m_RelatedArea;
        protected GameObject RelatedArea =>
            m_RelatedArea = m_RelatedArea != null ? m_RelatedArea
            : Manager.GetNestedElement(LABEL_GO_RELATEDAREA);

        private const string LABEL_SBN_RELATEDCARD = "RelatedArea/RelatedCard/RelatedCardButton";
        private SelectionButton m_ButtonRelatedCard;
        protected SelectionButton ButtonRelatedCard =>
            m_ButtonRelatedCard = m_ButtonRelatedCard != null ? m_ButtonRelatedCard
            : Manager.GetNestedElement<SelectionButton>(LABEL_SBN_RELATEDCARD);

        private const string LABEL_TXT_RELATEDCARD = "RelatedArea/RelatedCard/RelatedCardText";
        private TextMeshProUGUI m_TextRelatedCard;
        protected TextMeshProUGUI TextRelatedCard =>
            m_TextRelatedCard = m_TextRelatedCard != null ? m_TextRelatedCard
            : Manager.GetNestedElement<TextMeshProUGUI>(LABEL_TXT_RELATEDCARD);

        private const string LABEL_BTN_CLOSE = "RelatedArea/CloseButton";
        private SelectionButton m_ButtonClose;
        protected SelectionButton ButtonClose =>
            m_ButtonClose = m_ButtonClose != null ? m_ButtonClose
            : Manager.GetNestedElement<SelectionButton>(LABEL_BTN_CLOSE);

        #endregion

        #region CardListArea

        private const string LABEL_RT_COLLECTIONAREACENTER = "CardListArea/CollectionAreaCenter";
        private RectTransform m_CollectionAreaCenter;
        protected RectTransform CollectionAreaCenter =>
            m_CollectionAreaCenter = m_CollectionAreaCenter != null ? m_CollectionAreaCenter
            : Manager.GetNestedElement<RectTransform>(LABEL_RT_COLLECTIONAREACENTER);

        private const string LABEL_SR_CARDLIST = "CardListArea/CardList";
        private ScrollRect m_ScrollRect;
        protected ScrollRect ScrollRect =>
            m_ScrollRect = m_ScrollRect != null ? m_ScrollRect
            : Manager.GetNestedElement<ScrollRect>(LABEL_SR_CARDLIST);

        private const string LABEL_GO_TEMPLATE = "CardListArea/CardList/template";
        private GameObject m_Template;
        protected GameObject Template =>
            m_Template = m_Template != null ? m_Template
            : Manager.GetNestedElement(LABEL_GO_TEMPLATE);

        private const string LABEL_TXT_NOITEM = "CardListArea/NoItemText";
        private TextMeshProUGUI m_TextNoItem;
        protected TextMeshProUGUI TextNoItem =>
            m_TextNoItem = m_TextNoItem != null ? m_TextNoItem
            : Manager.GetNestedElement<TextMeshProUGUI>(LABEL_TXT_NOITEM);

        private const string LABEL_DTM_LOADING = "CardListArea/Loading";
        private DoTweenManager m_Loading;
        protected DoTweenManager Loading =>
            m_Loading = m_Loading != null ? m_Loading
            : Manager.GetNestedElement<DoTweenManager>(LABEL_DTM_LOADING);

        #endregion

        #region Others

        private const string LABEL_DA_DROPAREA = "DropArea";
        private DropArea m_DropArea;
        protected DropArea DropArea =>
            m_DropArea = m_DropArea != null ? m_DropArea
            : Manager.GetElement<DropArea>(LABEL_DA_DROPAREA);

        #endregion


        #endregion

        #region Reference

        public enum Area
        {
            Collection = 0,
            Bookmark = 1,
            History = 2,
        }
        [HideInInspector] public Area area = Area.Collection;
        [HideInInspector] public Area defaultArea = Area.Collection;
        [HideInInspector] public bool showingRelatedCards;

        public enum SortOrder
        {
            ByType = 1,
            ByTypeReverse = 2,
            ByLevelUp = 3,
            ByLevelDown = 4,
            ByAttackUp = 5,
            ByAttackDown = 6,
            ByDefenceUp = 7,
            ByDefenceDown = 8,
            ByRarityUp = 9,
            ByRarityDown = 10,
            ByGPUp = 11,
            ByGPDown = 12
        }
        public static SortOrder _SortOrder = SortOrder.ByType;

        public SuperScrollView superScrollView;
        public static List<long> filters = new();
        public static string packName = string.Empty;
        [HideInInspector] public List<int> historyCards = new();
        [HideInInspector] public List<int> printedCards;
        private string bookmarkTabText = string.Empty;
        private RectTransform bookmarkListPanelRect;
        private Popup.SelectionToggle_PopupSelectionItem bookmarkListButton;
        private GameObject bookmarkListItemTemplate;
        private readonly List<Action> bookmarkListMenuActions = new();
        private RectTransform scrollRectRect;
        private RectTransform textNoItemRect;
        private Vector2 scrollRectDefaultOffsetMin;
        private Vector2 scrollRectDefaultOffsetMax;
        private Vector2 textNoItemDefaultAnchoredPosition;
        private Vector2 textNoItemDefaultSizeDelta;
        private bool bookmarkListTemplateRequested;
        private bool bookmarkListLayoutCaptured;

        private const float BOOKMARK_LIST_BUTTON_MARGIN_DESKTOP = 12f;
        private const float BOOKMARK_LIST_BUTTON_MARGIN_MOBILE = 16f;
        private const float BOOKMARK_LIST_BUTTON_TOP_DESKTOP = 8f;
        private const float BOOKMARK_LIST_BUTTON_TOP_MOBILE = 12f;
        private const float BOOKMARK_LIST_BUTTON_HEIGHT_DESKTOP = 72f;
        private const float BOOKMARK_LIST_BUTTON_HEIGHT_MOBILE = 98f;
        private const string ADDRESS_BOOKMARK_LIST_ITEM = "Popup/PopupSelectionItem.prefab";

        #endregion

        #region Public Functions

        public void SetNoItemButtonNavigationEvent(MoveDirection direction, UnityAction action)
        {
            ButtonNoItem.SetNavigationEvent(direction, action);
        }

        public void SelectDefaultItem()
        {
            if (superScrollView.gameObjects.Count > 0)
                EventSystem.current.SetSelectedGameObject(superScrollView.gameObjects[0]);
            else if(showingRelatedCards)
                ButtonRelatedCard.GetSelectable().Select();
            else
                ButtonNoItem.GetSelectable().Select();
        }

        public void SelectNearestCard(Vector3 position)
        {
            if (superScrollView.gameObjects.Count == 0)
            {
                ButtonNoItem.GetSelectable().Select();
                return;
            }

            var distance = new Dictionary<GameObject, float>();
            foreach (var card in superScrollView.gameObjects)
                distance.Add(card, Utility.Tools.CalculateWeightedDistance(position, card.transform.GetChild(0).position, 'x'));
            EventSystem.current.SetSelectedGameObject(distance.Aggregate((left, right) => left.Value < right.Value ? left : right).Key);
        }

        public void PrintSearchCards(string text = "")
        {
            var cards = new List<int>();
            var results = MDPro3.Duel.YGOSharp.CardsManager.Search(InputSearch.InputField.text, filters, Controllers.DeckEditor.Banlist, packName);
            SortCards(results);
            foreach(MDPro3.Duel.YGOSharp.Card card in results)
                cards.Add(card.Id);

            ButtonSearch.SetButtonText(cards.Count == 0 ? MDPro3.InterString.Get("搜索") : cards.Count.ToString());

            PrintCards(cards);
        }

        protected MDPro3.Duel.YGOSharp.Card relatedCardData;
        protected void PrintRelatedCards(MDPro3.Duel.YGOSharp.Card data)
        {
            relatedCardData = data;
            var cards = new List<int>();
            var results = MDPro3.Duel.YGOSharp.CardsManager.RelatedSearch(data.Id);
            foreach (MDPro3.Duel.YGOSharp.Card card in results)
                cards.Add(card.Id);

            ButtonSearch.SetButtonText(cards.Count == 0 ? MDPro3.InterString.Get("搜索") : cards.Count.ToString());

            PrintCards(cards);
        }

        public void PrintBookmarkCards()
        {
            var cards = new List<int>();
            var results = MDPro3.Duel.YGOSharp.CardsManager.Search(
                InputSearch.InputField.text,
                filters,
                Controllers.DeckEditor.Banlist,
                packName,
                MDPro3.CardRarity.GetBookCards());
            SortCards(results);
            foreach (MDPro3.Duel.YGOSharp.Card card in results)
                cards.Add(card.Id);

            ButtonSearch.SetButtonText(cards.Count == 0 ? MDPro3.InterString.Get("搜索") : cards.Count.ToString());

            PrintCards(cards);
        }

        public void PrintHistoryCards()
        {
            PrintCards(historyCards);
        }

        private void SortCards(List<MDPro3.Duel.YGOSharp.Card> cards)
        {
            cards.Sort(MDPro3.Duel.YGOSharp.CardsManager.GetSort(_SortOrder));
        }

        public void AddHistoryCard(int code)
        {
            historyCards.Remove(code);
            historyCards.Insert(0, code);
            if (area == Area.History)
                PrintHistoryCards();
        }

        public void AddHistoryCards(List<int> codes)
        {
            foreach (var code in codes)
            {
                historyCards.Remove(code);
                historyCards.Insert(0, code);
            }
            if (area == Area.History)
                PrintHistoryCards();
        }

        public void SetSortIcon(Sprite icon)
        {
            ButtonSort.SetIconSprite(icon);
        }

        public void SetSortText(string text)
        {
            ButtonSort.SetButtonText(text);
        }

        public void SetCursor(bool selected)
        {
            CursorWindowSelect.Show = selected;
            foreach (ShortcutIcon shortcut in transform.GetComponentsInChildren<ShortcutIcon>(true))
                shortcut.GroupShow = selected;
        }

        public void RefreshBookmarkTab()
        {
            bookmarkTabText = string.IsNullOrEmpty(bookmarkTabText) ? MDPro3.InterString.Get("卡片收藏") : bookmarkTabText;
            var count = MDPro3.CardRarity.GetBookCards().Count;
            ToggleBookmark.SetButtonText($"{bookmarkTabText} ({count})");
            RefreshBookmarkListPanel();
        }

        public void OnTabRight()
        {
            ToggleCardList.OnRightSelection();
        }

        public Vector3 GetRubbishBinPositon()
        {
            if (area == Area.Collection)
                return CollectionAreaCenter.position;
            else
                return ToggleCardList.transform.position;
        }

        public void ShowFilters()
        {
            if (Client.FightClient.Instance != null && Client.FightClient.Instance.IsClientEditor)
            {
                Client.FightClient.Instance.OpenEditorFilters();
                return;
            }
            var handle = Addressables.InstantiateAsync("Popup/PopupSearchFilter.prefab");
            handle.Completed += (result) =>
            {
                result.Result.transform.SetParent(Runtime.Program.instance.ui_.popup, false);
                Popup.PopupSearchFilter popupSearchFilter = result.Result.GetComponent<Popup.PopupSearchFilter>();
                popupSearchFilter.Show();
            };
        }

        public void ResetFilters()
        {
            Runtime.AudioManager.PlaySE("SE_MENU_DECIDE");
            filters.Clear();
            packName = string.Empty;
            SelectionToggle_CardFilter.Instance.SetToggleOff();
            InputSearch.InputField.text = string.Empty;
            RefreshCurrentArea();
        }

        public void ShowSortOrder()
        {
            var handle = Addressables.InstantiateAsync("Popup/PopupSearchOrder.prefab");
            handle.Completed += (result) =>
            {
                result.Result.transform.SetParent(Runtime.Program.instance.ui_.popup, false);
                result.Result.GetComponent<Popup.PopupSearchOrder>().Show();
            };
        }

        public void ActivateInputField()
        {
            InputSearch.InputField.ActivateInputField();
        }

        public TMP_InputField GetInputField()
        {
            return InputSearch.InputField;
        }

        public void RefreshCardCount()
        {
            foreach (var go in superScrollView.gameObjects)
                go.GetComponent<SelectionButton_CardInCollection>()
                    .RefreshCountIcon();
        }

        public void SetCardInfoType()
        {
            foreach (var go in superScrollView.gameObjects)
                go.GetComponent<SelectionButton_CardInCollection>()
                    .RefreshIcons();
        }

        public void ShowArea(Area area)
        {
            if (this.area == area) return;
            this.area = area;
            RefreshToolArea();
            if (area == Area.Collection)
            {
                if (showingRelatedCards)
                    ShowRelatedCard(relatedCardData);
                else
                {
                    HideRelatedCardArea();
                    PrintSearchCards();
                }
            }
            else if (area == Area.Bookmark)
            {
                HideRelatedCardArea();
                PrintBookmarkCards();
            }
            else if (area == Area.History)
            {
                HideRelatedCardArea();
                PrintHistoryCards();
            }

            SetDropArea();
        }

        public void RefreshRarity(int code)
        {
            foreach (var go in superScrollView.gameObjects)
                go.GetComponent<SelectionButton_CardInCollection>()
                    .RefreshRarity(code);
            if (showingRelatedCards)
                ButtonRelatedCard.GetComponent<CardRawImageHandler>().RefreshRarity(code);
        }

        public void ShowRelatedCard(MDPro3.Duel.YGOSharp.Card data)
        {
            showingRelatedCards = true;

            ToggleCardList.SetToggleOn(false);
            FilterAndSortArea.SetActive(true);
            InputSearch.SetInteractable(false);
            ButtonSearch.SetInteractable(false);
            ToggleFilter.SetInteractable(false);
            ButtonSort.SetInteractable(false);
            ButtonClear.SetInteractable(false);

            RelatedArea.SetActive(true);
            ButtonRelatedCard.GetComponent<CardRawImageHandler>()
                .SetCard(data);
            TextRelatedCard.text = MDPro3.InterString.Get("「[?]」的相关卡片", data.Name);
            PrintRelatedCards(data);
        }

        public void HideRelatedCard()
        {
            showingRelatedCards = false;
            HideRelatedCardArea();
            PrintSearchCards();
        }

        protected void HideRelatedCardArea()
        {
            InputSearch.SetInteractable(true);
            ButtonSearch.SetInteractable(true);
            ToggleFilter.SetInteractable(true);
            ButtonSort.SetInteractable(true);
            ButtonClear.SetInteractable(true);

            RelatedArea.SetActive(false);
            RefreshToolArea();
        }

        public GameObject GetUpNavigationObject()
        {
            if(!showingRelatedCards)
                return null;

            return ButtonRelatedCard.gameObject;
        }

        public void OnRalatedAreaNavigationDown()
        {
            if(Runtime.Program.instance.deckEditor.lastSelectedCardInCollection != null)
            {
                Runtime.UserInput.NextSelectionIsAxis = true;
                EventSystem.current.SetSelectedGameObject
                    (Runtime.Program.instance.deckEditor.lastSelectedCardInCollection.gameObject);
            }
        }

        public void SetDropAreaActive(bool active)
        {
            DropArea.active = active;
        }

        public void SetBookmarkDropArea(int code)
        {
            if (area != Area.Bookmark)
                return;
            DropArea.ClearLabels();
            var canAddBookmark = !MDPro3.CardRarity.CardBookmarked(code);

            if (!canAddBookmark)
                DropArea.SetShowLabel(UIHover.LABEL_CANNOTADDBOOKMARK);
            else
                DropArea.SetShowLabel(UIHover.LABEL_ADDBOOKMARK);
            if (Controllers.DeckEditor.UseMobileLayout)
            {
                DropArea.SetShowLabel(UIHover.LABEL_RIGHT);
                DropArea.SetShowLabel(UIHover.LABEL_MAINDECK);
                DropArea.SetShowLabel(UIHover.LABEL_EXTRADECK);
                DropArea.SetShowLabel(UIHover.LABEL_SIDEDECK);
            }
        }

        #endregion

        #region Protected Functions

        protected override void Awake()
        {
            base.Awake();
            _SortOrder = SortOrder.ByType;

            Template.transform.SetParent(transform, false);
            Template.SetActive(false);

            superScrollView = new SuperScrollView
            (
                6,
                Controllers.DeckEditor.UseMobileLayout ? 158 : 88,
                Controllers.DeckEditor.UseMobileLayout ? 239 : 143,
                Controllers.DeckEditor.UseMobileLayout ? 10 : 5,
                Controllers.DeckEditor.UseMobileLayout ? 10 : 5,
                Template, ItemOnListRefresh, ScrollRect
            );

            bookmarkTabText = ToggleBookmark.GetButtonText();
            RefreshBookmarkTab();
            EnsureBookmarkListPanel();
            RefreshToolArea();

            InputSearch.InputField.onEndEdit.AddListener(RefreshCurrentArea);
            ButtonSearch.SetClickEvent(() => RefreshCurrentArea());
            ButtonSort.SetClickEvent(ShowSortOrder);
            ButtonClear.SetClickEvent(ResetFilters);

            RelatedArea.SetActive(false);

            ButtonRelatedCard.SetClickEvent(() =>
            {
                Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().ShowDetail(relatedCardData);
            });
            ButtonClose.SetClickEvent(HideRelatedCard);
            if (Controllers.DeckEditor.condition != Controllers.DeckEditor.Condition.ChangeSide)
                SetDropArea();
            else
                defaultArea = Area.History;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            superScrollView?.Clear();
            filters.Clear();
            packName = string.Empty;
        }

        protected void ItemOnListRefresh(string[] tasks, GameObject item)
        {
            SelectionButton_CardInCollection handler = item.GetComponent<SelectionButton_CardInCollection>();
            item.GetComponentInChildren<CardRawImageHandler>(true)?.SetClientPool(ClientImageCache);
            handler.CardCode = int.Parse(tasks[0]);
            handler.cardCollectionView = this;
        }

        protected void PrintCards(List<int> cards)
        {
            TextNoItem.gameObject.SetActive(cards.Count == 0);
            printedCards = cards;

            var args = new List<string[]>();
            for(int i = 0; i < cards.Count; i++)
            {
                var arg = new string[1] { cards[i].ToString() };
                args.Add(arg);
            }
            superScrollView.Print(args);
            if (Runtime.Program.instance.deckEditor.ResponseRegion == ServantUI.DeckEditorUI.ResponseRegion.Collection)
                SelectDefaultItem();
        }

        protected void RefreshCurrentArea(string _ = "")
        {
            if (area == Area.Collection)
            {
                if (showingRelatedCards)
                    PrintRelatedCards(relatedCardData);
                else
                    PrintSearchCards();
            }
            else if (area == Area.Bookmark)
            {
                PrintBookmarkCards();
            }
            else if (area == Area.History)
            {
                PrintHistoryCards();
            }
        }

        protected void RefreshToolArea()
        {
            FilterAndSortArea.SetActive(area == Area.Collection || area == Area.Bookmark);

            var showCollectionTools = area == Area.Collection || area == Area.Bookmark;
            ToggleFilter.gameObject.SetActive(showCollectionTools);
            ButtonSort.gameObject.SetActive(showCollectionTools);
            ButtonClear.gameObject.SetActive(showCollectionTools);
            RefreshBookmarkListPanel();
        }

        public bool TryRemoveCardFromBook(int code)
        {
            if (!MDPro3.CardRarity.CardBookmarked(code))
                return false;

            MDPro3.CardRarity.UnbookmarkCard(code);
            return true;
        }

        public void ShowBookmarkList(string listName)
        {
            ShowBookmarkArea(listName);
        }

        private void ShowBookmarkListMenu()
        {
            bookmarkListMenuActions.Clear();

            var selections = new List<string>
            {
                MDPro3.InterString.Get("收藏列表"),
                GetBookmarkDropdownButtonText()
            };

            AddBookmarkListMenuEntry(
                selections,
                () => ShowBookmarkArea(string.Empty),
                GetDefaultBookmarkListDisplayName(),
                string.IsNullOrEmpty(MDPro3.CardRarity.GetCurrentBookListName()) ? "g" : string.Empty);

            foreach (var listName in MDPro3.CardRarity.GetBookmarkListNames())
            {
                var capturedName = listName;
                AddBookmarkListMenuEntry(
                    selections,
                    () => ShowBookmarkArea(capturedName),
                    capturedName,
                    string.Equals(MDPro3.CardRarity.GetCurrentBookListName(), capturedName, StringComparison.InvariantCultureIgnoreCase) ? "g" : string.Empty);
            }

            AddBookmarkListMenuEntry(selections, CreateBookmarkList, MDPro3.InterString.Get("新建收藏列表"), "b");
            if (!string.IsNullOrEmpty(MDPro3.CardRarity.GetCurrentBookListName()))
            {
                AddBookmarkListMenuEntry(selections, RenameBookmarkList, MDPro3.InterString.Get("重命名收藏列表"), "b");
                AddBookmarkListMenuEntry(selections, DeleteActiveBookmarkList, MDPro3.InterString.Get("删除收藏列表"), "r");
            }

            Runtime.UIManager.ShowPopupSelection(selections, OnBookmarkListMenuSelection);
        }

        private void AddBookmarkListMenuEntry(List<string> selections, Action action, string label, string color = "")
        {
            bookmarkListMenuActions.Add(action);
            selections.Add(string.IsNullOrEmpty(color) ? label : $"{color}:{label}");
        }

        private void OnBookmarkListMenuSelection()
        {
            var index = GetCurrentPopupSelectionIndex();
            if (index < 0 || index >= bookmarkListMenuActions.Count)
                return;

            bookmarkListMenuActions[index]?.Invoke();
        }

        private int GetCurrentPopupSelectionIndex()
        {
            if (EventSystem.current.currentSelectedGameObject == null)
                return -1;

            return EventSystem.current.currentSelectedGameObject.transform.GetSiblingIndex();
        }

        private void ShowBookmarkArea(string listName)
        {
            SetActiveBookmarkList(listName);
            if (area != Area.Bookmark)
                ToggleBookmark.SetToggleOn();
            else
                PrintBookmarkCards();
        }

        private void SetActiveBookmarkList(string listName)
        {
            var normalized = NormalizeBookmarkListName(listName);
            if (normalized != string.Empty && !MDPro3.CardRarity.BookmarkListExists(normalized))
                normalized = string.Empty;

            MDPro3.CardRarity.SetCurrentBookListName(normalized);
            Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().CardDetailView.RefreshBookmarkToggle();
            RefreshBookmarkTab();
            RefreshBookmarkListPanel();
        }

        private string GetActiveBookmarkListDisplayName()
        {
            var activeBookmarkList = MDPro3.CardRarity.GetCurrentBookListName();
            return string.IsNullOrEmpty(activeBookmarkList)
                ? GetDefaultBookmarkListDisplayName()
                : activeBookmarkList;
        }

        private string GetDefaultBookmarkListDisplayName()
        {
            return MDPro3.InterString.Get("默认收藏列表");
        }

        private void CreateBookmarkList()
        {
            var selections = new List<string>
            {
                MDPro3.InterString.Get("新建收藏列表"),
                string.Empty
            };
            Runtime.UIManager.ShowPopupInput(selections, CreateBookmarkList, null, TmpInputValidation.ValidationType.None);
        }

        private void CreateBookmarkList(string listName)
        {
            if (!TryNormalizeBookmarkListName(listName, out var normalized))
                return;
            if (!MDPro3.CardRarity.AddBookmarkList(normalized))
            {
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("该收藏列表已存在！"));
                return;
            }

            ShowBookmarkArea(normalized);
        }

        private void RenameBookmarkList()
        {
            var selections = new List<string>
            {
                MDPro3.InterString.Get("重命名收藏列表"),
                MDPro3.CardRarity.GetCurrentBookListName()
            };
            Runtime.UIManager.ShowPopupInput(selections, RenameActiveBookmarkList, null, TmpInputValidation.ValidationType.None);
        }

        private void RenameActiveBookmarkList(string listName)
        {
            var activeBookmarkList = MDPro3.CardRarity.GetCurrentBookListName();
            if (!TryNormalizeBookmarkListName(listName, out var normalized, activeBookmarkList))
                return;
            if (string.Equals(activeBookmarkList, normalized, StringComparison.InvariantCultureIgnoreCase))
                return;
            if (!MDPro3.CardRarity.RenameBookmarkList(activeBookmarkList, normalized))
            {
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("该收藏列表已存在！"));
                return;
            }

            activeBookmarkList = normalized;
            RefreshBookmarkTab();
            RefreshBookmarkListPanel();
            if (area == Area.Bookmark)
                PrintBookmarkCards();
        }

        private void DeleteActiveBookmarkList()
        {
            if (string.IsNullOrEmpty(MDPro3.CardRarity.GetCurrentBookListName()))
                return;

            var selections = new List<string>
            {
                MDPro3.InterString.Get("删除收藏列表"),
                MDPro3.InterString.Get("该收藏列表包含[?]张卡片，是否确认删除？", MDPro3.CardRarity.GetBookCards().Count.ToString()),
                MDPro3.InterString.Get("确认"),
                MDPro3.InterString.Get("取消")
            };
            Runtime.UIManager.ShowPopupYesOrNo(selections, ConfirmDeleteActiveBookmarkList, null);
        }

        private void ConfirmDeleteActiveBookmarkList()
        {
            var activeBookmarkList = MDPro3.CardRarity.GetCurrentBookListName();
            if (string.IsNullOrEmpty(activeBookmarkList))
                return;

            MDPro3.CardRarity.DeleteBookmarkList(activeBookmarkList);
            RefreshBookmarkTab();
            RefreshBookmarkListPanel();
            if (area == Area.Bookmark)
                PrintBookmarkCards();
        }

        private bool TryNormalizeBookmarkListName(string listName, out string normalized, string ignoreListName = "")
        {
            normalized = NormalizeBookmarkListName(listName);
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

            var ignored = NormalizeBookmarkListName(ignoreListName);
            if (!string.Equals(normalized, ignored, StringComparison.InvariantCultureIgnoreCase)
                && MDPro3.CardRarity.BookmarkListExists(normalized))
            {
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("该收藏列表已存在！"));
                return false;
            }

            return true;
        }

        private string NormalizeBookmarkListName(string listName)
        {
            return listName == null ? string.Empty : listName.Trim();
        }

        private void EnsureBookmarkListPanel()
        {
            if (bookmarkListTemplateRequested)
                return;

            bookmarkListTemplateRequested = true;
            var handle = Addressables.LoadAssetAsync<GameObject>(ADDRESS_BOOKMARK_LIST_ITEM);
            handle.Completed += result =>
            {
                bookmarkListItemTemplate = result.Result;
                CreateBookmarkListPanel();
                RefreshBookmarkListPanel();
            };
        }

        private void CreateBookmarkListPanel()
        {
            if (bookmarkListPanelRect != null || bookmarkListItemTemplate == null)
                return;

            CaptureBookmarkListLayoutDefaults();

            var panel = new GameObject("BookmarkListPanel", typeof(RectTransform));
            panel.transform.SetParent(CollectionAreaCenter, false);
            bookmarkListPanelRect = panel.GetComponent<RectTransform>();
            bookmarkListPanelRect.SetAsLastSibling();

            var buttonObject = Instantiate(bookmarkListItemTemplate, panel.transform, false);
            buttonObject.name = "BookmarkListButton";
            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(0.5f, 1f);
            buttonRect.offsetMin = new Vector2(0f, -GetBookmarkListButtonHeight());
            buttonRect.offsetMax = Vector2.zero;

            bookmarkListButton = buttonObject.GetComponent<Popup.SelectionToggle_PopupSelectionItem>();
            bookmarkListButton.manager = null;
            bookmarkListButton.color = string.Empty;
            bookmarkListButton.clickAction = ShowBookmarkListMenu;

            panel.SetActive(false);
        }

        private void CaptureBookmarkListLayoutDefaults()
        {
            if (bookmarkListLayoutCaptured)
                return;

            scrollRectRect = ScrollRect.GetComponent<RectTransform>();
            textNoItemRect = TextNoItem.rectTransform;
            scrollRectDefaultOffsetMin = scrollRectRect.offsetMin;
            scrollRectDefaultOffsetMax = scrollRectRect.offsetMax;
            textNoItemDefaultAnchoredPosition = textNoItemRect.anchoredPosition;
            textNoItemDefaultSizeDelta = textNoItemRect.sizeDelta;
            bookmarkListLayoutCaptured = true;
        }

        private void RefreshBookmarkListPanel()
        {
            CaptureBookmarkListLayoutDefaults();

            var showPanel = area == Area.Bookmark && bookmarkListPanelRect != null;
            ApplyBookmarkListPanelLayout(showPanel);
            if (!showPanel || bookmarkListButton == null)
                return;

            bookmarkListButton.color = string.Empty;
            bookmarkListButton.selection = GetBookmarkDropdownButtonText();
            bookmarkListButton.Refresh();
        }

        private void ApplyBookmarkListPanelLayout(bool showPanel)
        {
            if (!bookmarkListLayoutCaptured || bookmarkListPanelRect == null)
                return;

            if (!showPanel)
            {
                bookmarkListPanelRect.gameObject.SetActive(false);
                scrollRectRect.offsetMin = scrollRectDefaultOffsetMin;
                scrollRectRect.offsetMax = scrollRectDefaultOffsetMax;
                textNoItemRect.anchoredPosition = textNoItemDefaultAnchoredPosition;
                textNoItemRect.sizeDelta = textNoItemDefaultSizeDelta;
                return;
            }

            var margin = Controllers.DeckEditor.UseMobileLayout ? BOOKMARK_LIST_BUTTON_MARGIN_MOBILE : BOOKMARK_LIST_BUTTON_MARGIN_DESKTOP;
            var top = Controllers.DeckEditor.UseMobileLayout ? BOOKMARK_LIST_BUTTON_TOP_MOBILE : BOOKMARK_LIST_BUTTON_TOP_DESKTOP;
            var height = GetBookmarkListButtonHeight();
            bookmarkListPanelRect.anchorMin = new Vector2(0f, 1f);
            bookmarkListPanelRect.anchorMax = new Vector2(1f, 1f);
            bookmarkListPanelRect.pivot = new Vector2(0.5f, 1f);
            bookmarkListPanelRect.offsetMin = new Vector2(margin, -top - height);
            bookmarkListPanelRect.offsetMax = new Vector2(-margin, -top);
            bookmarkListPanelRect.gameObject.SetActive(true);

            var reservedTop = top + height + margin;
            scrollRectRect.offsetMin = scrollRectDefaultOffsetMin;
            scrollRectRect.offsetMax = new Vector2(scrollRectDefaultOffsetMax.x, scrollRectDefaultOffsetMax.y - reservedTop);
            textNoItemRect.anchoredPosition = textNoItemDefaultAnchoredPosition + new Vector2(0f, -reservedTop * 0.5f);
            textNoItemRect.sizeDelta = textNoItemDefaultSizeDelta + new Vector2(0f, -reservedTop);
        }

        private float GetBookmarkListButtonHeight()
        {
            return Controllers.DeckEditor.UseMobileLayout ? BOOKMARK_LIST_BUTTON_HEIGHT_MOBILE : BOOKMARK_LIST_BUTTON_HEIGHT_DESKTOP;
        }

        private string GetBookmarkDropdownButtonText()
        {
            return $"{GetActiveBookmarkListDisplayName()} ({(MDPro3.CardRarity.GetBookCards().Count)})";
        }

        protected void SetDropArea()
        {
            if (area == Area.Bookmark)
            {
                DropArea.ClearLabels();
                DropArea.SetShowLabel(UIHover.LABEL_ADDBOOKMARK);
            }
            else
            {
                DropArea.ClearLabels();
                if(Controllers.DeckEditor.condition != Controllers.DeckEditor.Condition.ChangeSide)
                    DropArea.SetShowLabel(UIHover.LABEL_REMOVEDECK);
            }
            if (Controllers.DeckEditor.UseMobileLayout)
            {
                DropArea.SetShowLabel(UIHover.LABEL_MAINDECK);
                DropArea.SetShowLabel(UIHover.LABEL_EXTRADECK);
                DropArea.SetShowLabel(UIHover.LABEL_SIDEDECK);
                if (Controllers.DeckEditor.condition != Controllers.DeckEditor.Condition.ChangeSide)
                    DropArea.SetShowLabel(UIHover.LABEL_RIGHT);
            }
        }

        #endregion

    }
}
