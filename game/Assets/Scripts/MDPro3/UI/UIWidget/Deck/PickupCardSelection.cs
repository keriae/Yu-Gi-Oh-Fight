using Cysharp.Threading.Tasks;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using MDPro3.UI.PropertyOverride;
using MDPro3.UI.ServantUI;
using MDPro3.Utility;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI
{
    public class PickupCardSelection : UIWidget
    {
        #region Elements

        private const string LABEL_PICKUP_CARD_0 = "pick0";
        private SelectionToggle_PickupCard m_Pickup0;
        private SelectionToggle_PickupCard Pickup0 =>
            m_Pickup0 = m_Pickup0 != null ? m_Pickup0
            : Manager.GetElement<SelectionToggle_PickupCard>(LABEL_PICKUP_CARD_0);

        private const string LABEL_PICKUP_CARD_1 = "pick1";
        private SelectionToggle_PickupCard m_Pickup1;
        private SelectionToggle_PickupCard Pickup1 =>
            m_Pickup1 = m_Pickup1 != null ? m_Pickup1
            : Manager.GetElement<SelectionToggle_PickupCard>(LABEL_PICKUP_CARD_1);

        private const string LABEL_PICKUP_CARD_2 = "pick2";
        private SelectionToggle_PickupCard m_Pickup2;
        private SelectionToggle_PickupCard Pickup2 =>
            m_Pickup2 = m_Pickup2 != null ? m_Pickup2
            : Manager.GetElement<SelectionToggle_PickupCard>(LABEL_PICKUP_CARD_2);

        private const string LABEL_IMG_DECK_CASE = "DeckCaseIcon";
        private Image m_IconDeckCase;
        private Image IconDeckCase =>
            m_IconDeckCase = m_IconDeckCase != null ? m_IconDeckCase
            : Manager.GetElement<Image>(LABEL_IMG_DECK_CASE);

        #endregion

        protected override void Awake()
        {
            base.Awake();

            SetPickups(DeckEditor.Deck);
            if (PropertyOverrider.NeedMobileLayout())
                IconDeckCase.gameObject.SetActive(false);
            else
                _ = LoadDeckCaseAsync(DeckEditor.Deck.Case);
        }

        public void SetPickups(Deck deck)
        {
            SetPickup(CardsManager.Get(deck.Pickup[0]), 0, false);
            SetPickup(CardsManager.Get(deck.Pickup[1]), 1, false);
            SetPickup(CardsManager.Get(deck.Pickup[2]), 2, false);

            Pickup0.SetToggleOn();
        }

        public void SetPickup(Card card, int index, bool selectNext = true)
        {
            if(card == null || card.Id == 0)
            {
                ClearPickup(index, selectNext);
                return;
            }

            switch (index)
            {
                case 0: Pickup0.SetCard(card, selectNext); break;
                case 1: Pickup1.SetCard(card, selectNext); break;
                case 2: Pickup2.SetCard(card, selectNext); break;
            }

            if (selectNext)
                SelectNextPickup(index);
        }

        public void ClearPickup(int index, bool toggleOn)
        {
            switch (index)
            {
                case 0: 
                    Pickup0.ClearCard(toggleOn, toggleOn);
                    break;
                case 1: 
                    Pickup1.ClearCard(toggleOn, toggleOn);
                    break;
                case 2: 
                    Pickup2.ClearCard(toggleOn, toggleOn);
                    break;
            }
        }

        public int GetPickupIndex()
        {
            if(Pickup0.isOn)
                return 0;
            else if(Pickup1.isOn)
                return 1;
            else
                return 2;
        }

        private void SelectNextPickup(int index)
        {
            SelectionToggle_PickupCard other1;
            SelectionToggle_PickupCard other2;
            if (index == 0)
            {
                other1 = Pickup1;
                other2 = Pickup2;
            }
            else if (index == 1) 
            {
                other1 = Pickup2;
                other2 = Pickup0;
            }
            else
            {
                other1 = Pickup0;
                other2 = Pickup1;
            }
            if (!other1.cardSetted)
            {
                other1.SetToggleOn();
                return;
            }
            if (!other2.cardSetted)
            {
                other2.SetToggleOn();
                return;
            }
        }

        private async UniTask LoadDeckCaseAsync(int deckCase)
        {
            var sprite = await Program.items.LoadDeckCaseIconAsync(deckCase, "_L_HD");
            if (gameObject != null)
                IconDeckCase.sprite = sprite;
            else
                IconDeckCase.gameObject.SetActive(false);
        }

        public void Select()
        {
            Pickup0.GetSelectable().Select();
        }

        public void OnLeftNavigation()
        {
            Program.instance.deckBrowser.GetUI<DeckBrowserUI>().DeckView.SelectNearestCard(Pickup0.transform.position);
        }
    }
}