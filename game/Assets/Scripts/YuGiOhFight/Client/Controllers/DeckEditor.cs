using DG.Tweening;


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


namespace YuGiOhFight.Client.Controllers
{
    public class DeckEditor : Servant
    {

        #region Reference

        public static MDPro3.Duel.YGOSharp.Deck Deck { get; set; }
        public static string DeckName { get; set; }
        public static bool DeckIsFromLocal;
        public static MDPro3.Duel.YGOSharp.Banlist Banlist => cardInfoType == CardInfoType.Genesys ? MDPro3.Duel.YGOSharp.BanlistManager.currentGenesysBanList : MDPro3.Duel.YGOSharp.BanlistManager.currentBanList;
        public static List<int> historyCards;
        public static bool UseMobileLayout => UI.PropertyOverride.PropertyOverrider.NeedMobileLayout();
        public static string onlineDeckID;
        public static MDPro3.Utility.DeckLibraryEntry Recommendation { get; set; }

        public enum Condition
        {
            EditDeck,
            OnlineDeck,
            ReplayDeck,
            ChangeSide,
            RecommendationDeck
        }
        public static Condition condition = Condition.EditDeck;
        public void SwitchCondition(Condition condition, string deckName = "", MDPro3.Duel.YGOSharp.Deck deck = null)
        {
            DeckEditor.condition = condition;
            switch (condition)
            {
                case Condition.RecommendationDeck:
                    returnServant = Runtime.Program.instance.deckSelector;
                    DeckName = deckName;
                    Deck = deck;
                    DeckIsFromLocal = false;
                    historyCards = new();
                    break;
                case Condition.EditDeck:
                    returnServant = Runtime.Program.instance.deckSelector;
                    DeckName = string.IsNullOrEmpty(deckName) ? MDPro3.Config.GetConfigDeckName() : deckName;

                    var deckPath = Runtime.Program.PATH_DECK + DeckName + Runtime.Program.EXPANSION_YDK;

                    // Auto-fix the file on disk (pre-release ids -> official ids).
                    MDPro3.YdkIdHelper.NormalizeYdkFile(deckPath);

                    Deck = new MDPro3.Duel.YGOSharp.Deck(deckPath);
                    DeckIsFromLocal = true;
                    historyCards = new();
                    break;

                case Condition.OnlineDeck:
                    returnServant = Runtime.Program.instance.onlineDeckViewer;
                    DeckName = deckName;
                    Deck = null;
                    DeckIsFromLocal = false;
                    historyCards = new();
                    break;

                case Condition.ReplayDeck:
                    returnServant = Runtime.Program.instance.replay;
                    DeckName = deckName;
                    Deck = deck;
                    DeckIsFromLocal = false;
                    historyCards = new();
                    break;

                case Condition.ChangeSide:
                    DeckName = MDPro3.Config.GetConfigDeckName();
                    Deck = Runtime.Network.TcpHelper.deck;
                    DeckIsFromLocal = false;
                    historyCards = OcgCore.sideReference.Main;
                    break;
            }
        }

        public UI.ServantUI.DeckEditorUI.ResponseRegion ResponseRegion
        {
            get { return GetUI<UI.ServantUI.DeckEditorUI>()._ResponseRegion; }
            set { GetUI<UI.ServantUI.DeckEditorUI>()._ResponseRegion = value; }
        }

        public enum CardInfoType
        {
            None = 0,
            Detail = 1,
            Pool = 2,
            Genesys = 3
        }
        public static CardInfoType cardInfoType { get; private set; }

        #endregion

        #region Servant

        [HideInInspector] public UI.SelectionButton_CardInDeck lastSelectedCardInDeck;
        [HideInInspector] public UI.SelectionButton_CardInCollection lastSelectedCardInCollection;
        public static bool ToHandTest;

        public override int Depth => 6;
        protected override bool ShowLine => false;
        protected override bool NeedExitButton => false;
        public override float TransitionTime => 0.6f;
        protected override string Label_UI =>
            UI.PropertyOverride.PropertyOverrider.NeedMobileLayout()
            ? "ServantUI/DeckEditorUIMobile.prefab" : "ServantUI/DeckEditorUI.prefab";

        public override void Initialize()
        {
            Runtime.SystemEvent.OnResolutionChange += ChangeCanvasMatch;
            returnServant = Runtime.Program.instance.deckSelector;
            base.Initialize();
        }

        protected override void ApplyShowArrangement(int preDepth)
        {
            base.ApplyShowArrangement(preDepth);
            Runtime.UserInput.SetMoveRepeatSpeed(0.8f);
        }

        protected override void ApplyHideArrangement(int nextDepth)
        {
            base.ApplyHideArrangement(nextDepth);
            Runtime.UserInput.SetMoveRepeatSpeed(1f);
        }

        public override void PerFrameFunction()
        {
            if (!NeedResponseInput())
                return;

            if (Runtime.UserInput.WasRightShoulderPressing)
            {
                if (Runtime.UserInput.WasGamepadButtonNorthPressed)
                    GetUI<UI.ServantUI.DeckEditorUI>().OnRegulation();
                else if (Runtime.UserInput.WasGamepadButtonWestPressed)
                    GetUI<UI.ServantUI.DeckEditorUI>().SetCardInfoType();
                else if (Runtime.UserInput.WasGamepadStartPressed)
                    GetUI<UI.ServantUI.DeckEditorUI>().ShiftToAppearance();
                return;
            }

            if (Runtime.UserInput.WasCancelPressed && condition != Condition.ChangeSide)
                OnReturn();

            if (Runtime.UserInput.WasGamepadSelectPressed)
            {
                if(condition == Condition.ChangeSide)
                    GetUI<UI.ServantUI.DeckEditorUI>().OnChangeSideComplete();
                else
                    GetUI<UI.ServantUI.DeckEditorUI>().OnSave();
            }

            if (Runtime.UserInput.WasGamepadStartPressed)
                GetUI<UI.ServantUI.DeckEditorUI>().OnSubMenu();

            if (Runtime.UserInput.WasLeftTriggerPressed)
                GetUI<UI.ServantUI.DeckEditorUI>().ShowCardActionMenu();

            if (Runtime.UserInput.WasRightTriggerPressed)
            {
                if (ResponseRegion == UI.ServantUI.DeckEditorUI.ResponseRegion.Deck)
                    SelectLastCollectionViewItem();
                else if (ResponseRegion == UI.ServantUI.DeckEditorUI.ResponseRegion.Collection)
                    SelectLastDeckViewItem();
            }


            if (ResponseRegion == UI.ServantUI.DeckEditorUI.ResponseRegion.Deck)
            {
                if (Runtime.UserInput.WasGamepadButtonNorthPressed)
                    GetUI<UI.ServantUI.DeckEditorUI>().DeckView.ActivateInputField();
                else if (Runtime.UserInput.WasGamepadButtonWestPressed)
                    GetUI<UI.ServantUI.DeckEditorUI>().OnDeckButtonClicked();
            }
            else if (ResponseRegion == UI.ServantUI.DeckEditorUI.ResponseRegion.Collection)
            {
                if (GetUI<UI.ServantUI.DeckEditorUI>().CardCollectionView.area == UI.CardCollectionView.Area.Collection)
                {
                    if (Runtime.UserInput.WasLeftStickPressed)
                        GetUI<UI.ServantUI.DeckEditorUI>().CardCollectionView.PrintSearchCards();

                    if (GetUI<UI.ServantUI.DeckEditorUI>().CardCollectionView.showingRelatedCards)
                        return;

                    if (Runtime.UserInput.WasGamepadButtonNorthPressed)
                    {
                        if (Runtime.UserInput.WasLeftShoulderPressing)
                            GetUI<UI.ServantUI.DeckEditorUI>().CardCollectionView.ShowSortOrder();
                        else
                            GetUI<UI.ServantUI.DeckEditorUI>().CardCollectionView.InputSearch.InputField.ActivateInputField();
                    }
                    else if (Runtime.UserInput.WasGamepadButtonWestPressed)
                    {
                        if (Runtime.UserInput.WasLeftShoulderPressing)
                            GetUI<UI.ServantUI.DeckEditorUI>().CardCollectionView.ResetFilters();
                        else
                            GetUI<UI.ServantUI.DeckEditorUI>().CardCollectionView.ShowFilters();
                    }
                }

                if (Runtime.UserInput.WasRightStickPressed)
                    GetUI<UI.ServantUI.DeckEditorUI>().CardCollectionView.OnTabRight();
            }
        }

        public override bool NeedResponseInput()
        {
            if(servantUI == null)
                return false;
            if(GetUI<UI.ServantUI.DeckEditorUI>().CardActionMenu.showing)
                return false;
            return base.NeedResponseInput();
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !Runtime.UserInput.NeedDefaultSelect())
                return;

            if (ResponseRegion == UI.ServantUI.DeckEditorUI.ResponseRegion.Collection)
                SelectLastCollectionViewItem();
            else if (ResponseRegion == UI.ServantUI.DeckEditorUI.ResponseRegion.Deck)
                SelectLastDeckViewItem();
            else if (ResponseRegion == UI.ServantUI.DeckEditorUI.ResponseRegion.Action)
            {
                if (lastSelectable != null)
                    lastSelectable.Select();
                else
                    GetUI<UI.ServantUI.DeckEditorUI>().CardActionMenu.SelectDefaultButton();
            }
        }

        public void SelectLastDeckViewItem()
        {
            ResponseRegion = UI.ServantUI.DeckEditorUI.ResponseRegion.Deck;
            if (lastSelectedCardInDeck != null)
                lastSelectedCardInDeck.GetSelectable().Select();
            else
                GetUI<UI.ServantUI.DeckEditorUI>().DeckView.SelectDefaultItem();
        }

        public void SelectNearestDeckViewItem(Vector3 position)
        {
            ResponseRegion = UI.ServantUI.DeckEditorUI.ResponseRegion.Deck;
            Runtime.UserInput.NextSelectionIsAxis = true;
            GetUI<UI.ServantUI.DeckEditorUI>().DeckView.SelectNearestCard(position);
        }

        public void SelectLastCollectionViewItem()
        {
            ResponseRegion = UI.ServantUI.DeckEditorUI.ResponseRegion.Collection;
            if (lastSelectedCardInCollection != null)
                EventSystem.current
                    .SetSelectedGameObject(lastSelectedCardInCollection.gameObject);
            else
                GetUI<UI.ServantUI.DeckEditorUI>().CardCollectionView.SelectDefaultItem();
        }

        public void SelectNearestCollectionViewItem(Vector3 position)
        {
            ResponseRegion = UI.ServantUI.DeckEditorUI.ResponseRegion.Collection;
            Runtime.UserInput.NextSelectionIsAxis = true;
            GetUI<UI.ServantUI.DeckEditorUI>().CardCollectionView.SelectNearestCard(position);
        }

        public override void OnReturn()
        {
            // A client host owns the three-way save/discard/cancel guard.
            // The original two-choice guard remains for unhosted editor contexts.
            if (returnAction != null)
            {
                returnAction.Invoke();
                return;
            }
            if (!GetUI<UI.ServantUI.DeckEditorUI>().DeckView.GetDirty() || !DeckIsFromLocal)
                base.OnReturn();
            else
            {
                GetUI<UI.ServantUI.DeckEditorUI>().callExit = true;

                var selections = new List<string>
                {
                    MDPro3.InterString.Get("卡组未保存"),
                    MDPro3.InterString.Get("卡组已修改，是否保存？"),
                    MDPro3.InterString.Get("保存"),
                    MDPro3.InterString.Get("不保存")
                };
                Runtime.UIManager.ShowPopupYesOrNo(selections, GetUI<UI.ServantUI.DeckEditorUI>().OnSave, OnExit);
            }
        }

        public override void JudgeInputBlockerExitMark(object o)
        {
            ResponseRegion = (UI.ServantUI.DeckEditorUI.ResponseRegion)o;
        }

        public void CallExitIn(float time)
        {
            inTransition = true;//block input
            DOTween.To(v => { }, 0, 0, time).OnComplete(() =>
            {
                OnExit();
            });
        }

        #endregion

        #region Other

        private void ChangeCanvasMatch()
        {
            if (!showing)
                return;

            Runtime.UIManager.SetCanvasMatch(GetCanvasMatch(), 0f);
        }

        public float GetCanvasMatch()
        {
            if ((float)Screen.width / Screen.height > 16f / 9f)
                return 1f;
            else return 0f;
        }

        public void SetCardInfoType(CardInfoType type)
        {
            cardInfoType = type;
            GetUI<UI.ServantUI.DeckEditorUI>().SetCardInfoType(type);
            UI.SelectionButton_CardInfoType.instance.SetCardInfoTypeIcon(type);
        }

        #endregion
    }
}
