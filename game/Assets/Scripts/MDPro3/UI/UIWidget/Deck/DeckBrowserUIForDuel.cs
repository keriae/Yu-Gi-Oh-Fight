using MDPro3.Duel.YGOSharp;
using UnityEngine;

namespace MDPro3.UI
{
    public class DeckBrowserUIForDuel : UIWidgetFullScreen
    {
        #region Elements

        private const string LABEL_MONO_DECK_VIEW = "DeckViewForDuel";
        private DeckViewForDuel m_DeckView;
        private DeckViewForDuel DeckView =>
            m_DeckView = m_DeckView != null ? m_DeckView
            : Manager.GetElement<DeckViewForDuel>(LABEL_MONO_DECK_VIEW);

        private const string LABEL_MONO_CARD_DETAIL_VIEW = "CardDetailView";
        private CardDetailView m_CardDetailView;
        private CardDetailView CardDetailView =>
            m_CardDetailView = m_CardDetailView != null ? m_CardDetailView
            : Manager.GetElement<CardDetailView>(LABEL_MONO_CARD_DETAIL_VIEW);

        #endregion

        private Deck deck;

        public void SetDeck(Deck deck)
        {
            this.deck = deck;
        }

        public override void Show()
        {
            if (deck == null) return;
            base.Show();
        }

        protected override void ShowEvent()
        {
            base.ShowEvent();
            DeckView.SetCardDetailView(CardDetailView);
            DeckView.PrintDeck(deck);
        }
    }
}
