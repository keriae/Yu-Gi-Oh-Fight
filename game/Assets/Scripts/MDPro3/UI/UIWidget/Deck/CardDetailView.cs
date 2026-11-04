using MDPro3.Duel.YGOSharp;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YgomSystem.ElementSystem;

namespace MDPro3.UI
{
    public class CardDetailView : UIWidgetCardBase
    {
        #region Elements

        private const string LABEL_CG_CONTENT = "Content";
        private CanvasGroup m_Content;
        protected CanvasGroup Content =>
            m_Content = m_Content != null ? m_Content 
            : Manager.GetElement<CanvasGroup>(LABEL_CG_CONTENT);

        private const string LABEL_SBN_CARD = "CardArea/ButtonCard";
        private SelectionButton m_ButtonCard;
        protected SelectionButton ButtonCard =>
            m_ButtonCard = m_ButtonCard != null ? m_ButtonCard
            : Manager.GetNestedElement<SelectionButton>(LABEL_SBN_CARD);

        #endregion

        private List<int> cards;
        private int showingCardIndex;

        protected override void Awake()
        {
            base.Awake();
            pendulumTextNeedSplit = false;

            Content.alpha = 0f;
            Content.blocksRaycasts = false;

            ButtonCard.SetClickEvent(ShowCardDetail);
        }

        public void ShowCard(Card data)
        {
            Content.alpha = 1f;
            Content.blocksRaycasts = true;

            cards = null;
            SetCardData(data);
        }

        public void ShowCard(List<int> cards, int index)
        {
            Content.alpha = 1f;
            Content.blocksRaycasts = true;

            this.cards = cards;
            showingCardIndex = index;
            SetCardData(CardsManager.Get(cards[index]));
        }

        private void ShowCardDetail()
        {
            if (cards != null)
                UIManager.ShowCardInfoDetail(cards, showingCardIndex);
            else if(Card != null)
                UIManager.ShowCardInfoDetail(Card);
        }
    }
}