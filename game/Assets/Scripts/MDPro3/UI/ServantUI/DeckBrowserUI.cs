using DG.Tweening;
using MDPro3.Servant;
using MDPro3.UI.PropertyOverride;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static MDPro3.Servant.DeckBrowser;

namespace MDPro3.UI.ServantUI
{
    public class DeckBrowserUI : ServantUI
    {

        #region Elements

        private const string LABEL_CG_BG = "DialogBG";
        private CanvasGroup m_DialogBG;
        private CanvasGroup DialogBG =>
            m_DialogBG = m_DialogBG != null ? m_DialogBG
            : Manager.GetElement<CanvasGroup>(LABEL_CG_BG);

        private const string LABEL_MONO_DECKVIEW = "DeckView";
        private DeckView m_DeckView;
        public DeckView DeckView =>
            m_DeckView = m_DeckView != null ? m_DeckView
            : Manager.GetElement<DeckView>(LABEL_MONO_DECKVIEW);

        private const string LABEL_MONO_CARDDETAILVIEW = "CardDetailView";
        private CardDetailView m_CardDetailView;
        public CardDetailView CardDetailView =>
            m_CardDetailView = m_CardDetailView != null ? m_CardDetailView
            : Manager.GetElement<CardDetailView>(LABEL_MONO_CARDDETAILVIEW);

        private const string LABEL_RT_OPTIONAL_AREA_LOCATOR = "OptionalAreaLocator";
        private RectTransform m_OptionalAreaLocator;
        private RectTransform OptionalAreaLocator =>
            m_OptionalAreaLocator = m_OptionalAreaLocator != null ? m_OptionalAreaLocator
            : Manager.GetElement<RectTransform>(LABEL_RT_OPTIONAL_AREA_LOCATOR);

        private const string LABEL_RT_OPTIONAL_AREA_LOCATOR_2 = "OptionalAreaLocator2";
        private RectTransform m_OptionalAreaLocator2;
        private RectTransform OptionalAreaLocator2 =>
            m_OptionalAreaLocator2 = m_OptionalAreaLocator2 != null ? m_OptionalAreaLocator2
            : Manager.GetElement<RectTransform>(LABEL_RT_OPTIONAL_AREA_LOCATOR_2);

        #endregion

        [HideInInspector] public PickupCardSelection PickupCardSelection;

        private const string LABEL_WIDGET_PICKUP = "UIWidges/DeckBrowserOptionForPickupCardSelection.prefab";

        public enum ResponseRegion
        {
            Deck,
            Option
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
            DeckView.PrintDeck(DeckEditor.Deck, DeckEditor.DeckName, DeckView.Condition.Pickup);
            DeckView.SetNoItemButtonNavigationEvent(UnityEngine.EventSystems.MoveDirection.Right, () =>
            {
                UserInput.NextSelectionIsAxis = true;
                PickupCardSelection.Select();
            });
        }

        public override void ShowEvent()
        {
            base.ShowEvent();
            DialogBG.alpha = 0f;
            DialogBG.DOFade(1f, 0.3f);

            UIManager.SetCanvasMatch(Program.instance.deckEditor.GetCanvasMatch(), 0.45f);
        }

        protected override void HideEvent()
        {
            base.HideEvent();
            DialogBG.DOFade(0f, 0.3f);

            UIManager.SetCanvasMatch(1f, 0.4f);
        }

        protected override void AfterHideEvent()
        {
            base.AfterHideEvent();

            Dispose();
        }

        public void ShowDetail(List<int> cards, int index)
        {
            if (CardDetailView != null)
                CardDetailView.ShowCard(cards, index);
        }

        private void ShiftToResponseRegion()
        {
            DeckView.SetCursor(_ResponseRegion == ResponseRegion.Deck);
            
        }

        public void SetCondition(Condition condition)
        {
            switch (condition)
            {
                case Condition.ChangePickup:
                    Title.text = InterString.Get("变更三大代表卡");
                    LoadOptionalArea(LABEL_WIDGET_PICKUP, 1);
                    break;
            }
        }

        private void LoadOptionalArea(string address, int areaIndex)
        {
            if (PropertyOverrider.NeedMobileLayout())
                areaIndex = 1;
            var load = Addressables.InstantiateAsync(address);
            load.Completed += (result) =>
            {
                result.Result.transform.SetParent(areaIndex == 1 ? OptionalAreaLocator : OptionalAreaLocator2, false);
                PickupCardSelection = result.Result.GetComponent<PickupCardSelection>();
            };
        }

        private void Dispose()
        {
            Destroy(gameObject);
        }
    }
}
