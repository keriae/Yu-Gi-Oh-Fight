using Cysharp.Threading.Tasks;
using MDPro3.Duel.YGOSharp;
using MDPro3.UI.PropertyOverride;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YgomSystem.ElementSystem;

namespace MDPro3.UI
{
    public class DeckViewForDuel : UIWidget
    {
        #region Elements

        private const string LABEL_DTM_LOADING = "Loading";
        private DoTweenManager m_TweenLoading;
        protected DoTweenManager TweenLoading =>
            m_TweenLoading = m_TweenLoading != null ? m_TweenLoading
            : Manager.GetElement<DoTweenManager>(LABEL_DTM_LOADING);

        private const string LABEL_SR_DECKVIEW = "ScrollRect";
        private ScrollRect m_ScrollRect;
        private ScrollRect ScrollRect =>
            m_ScrollRect = m_ScrollRect != null ? m_ScrollRect
            : Manager.GetElement<ScrollRect>(LABEL_SR_DECKVIEW);

        private const string LABEL_CG_VIEWPORT = "Viewport";
        private CanvasGroup m_Viewport;
        private CanvasGroup Viewport => 
            m_Viewport = m_Viewport != null ? m_Viewport
            : Manager.GetElement<CanvasGroup>(LABEL_CG_VIEWPORT);

        private const string LABEL_MONO_DECKNUM = "DeckNumCounter";
        private DeckNumCounter m_DeckNumCounter;
        private DeckNumCounter DeckNumCounter =>
            m_DeckNumCounter = m_DeckNumCounter != null ? m_DeckNumCounter
            : Manager.GetElement<DeckNumCounter>(LABEL_MONO_DECKNUM);

        #endregion

        private DeckScrollRectForDuel m_DeckScrollRect;
        private DeckScrollRectForDuel DeckScrollRect =>
            m_DeckScrollRect = m_DeckScrollRect != null ? m_DeckScrollRect
            : new DeckScrollRectForDuel(
                ScrollRect.GetComponent<ElementObjectManager>(), 
                cardDetailView, 
                PropertyOverrider.NeedMobileLayout() ? 5 : 8);

        private CardDetailView cardDetailView;

        protected override void Awake()
        {
            base.Awake();
            Viewport.alpha = 0f;
            Viewport.interactable = false;
        }

        public void SetCardDetailView(CardDetailView cardDetailView)
        {
            this.cardDetailView = cardDetailView;
        }

        public void PrintDeck(Deck deck)
        {
            _ = PrintDeckAsync(deck);
        }

        private async UniTask PrintDeckAsync(Deck deck)
        {
            TweenLoading.Show();
            await DeckScrollRect.PrintDeck(deck);
            DeckNumCounter.SetNum(deck.Main.Count, deck.Extra.Count, deck.Side.Count);
            TweenLoading.Hide();
            Viewport.alpha = 1f;
            Viewport.interactable = true;
        }


        private class DeckScrollRectForDuel
        {
            private readonly ElementObjectManager manager;

            private const string LABEL_RT_CONTENT = "ScrollContent";
            private RectTransform m_Content;
            private RectTransform Content =>
                m_Content = m_Content != null ? m_Content
                : manager.GetElement<RectTransform>(LABEL_RT_CONTENT);

            private const string LABEL_GO_TEMPLATE_HEADER = "TemplateHeader";
            private GameObject m_TemplateHeader;
            private GameObject TemplateHeader =>
                m_TemplateHeader = m_TemplateHeader != null ? m_TemplateHeader
                : manager.GetElement(LABEL_GO_TEMPLATE_HEADER);

            private const string LABEL_GO_TEMPLATE_CARD_GROUP = "TemplateCardGroup";
            private GameObject m_TemplateCardGroup;
            private GameObject TemplateCardGroup =>
                m_TemplateCardGroup = m_TemplateCardGroup != null ? m_TemplateCardGroup
                : manager.GetElement(LABEL_GO_TEMPLATE_CARD_GROUP);

            private readonly int columns;
            private CardDetailView cardDetailView;

            public DeckScrollRectForDuel(ElementObjectManager manager, CardDetailView cardDetailView, int columns)
            {
                this.manager = manager;
                TemplateHeader.SetActive(false);
                TemplateCardGroup.SetActive(false);
                this.columns = columns;
                this.cardDetailView = cardDetailView;
            }

            public async UniTask PrintDeck(Deck deck)
            {
                var main = DeckView.GetCardsInDisplayOrder(deck.Main).Select(card => card.Id).ToList();
                var extra = DeckView.GetCardsInDisplayOrder(deck.Extra).Select(card => card.Id).ToList();
                var side = DeckView.GetCardsInDisplayOrder(deck.Side).Select(card => card.Id).ToList();

                if(main.Count > 0)
                {
                    PrintHeader(DeckView.DeckLocation.MainDeck);
                    var list = new List<int>();
                    for(int i = 0; i < main.Count; i++)
                    {
                        list.Add(main[i]);
                        if(list.Count == columns || i == main.Count - 1)
                        {
                            //await PrintCards(list);
                            _ = PrintCards(list);
                            await UniTask.Yield();
                            list.Clear();
                        }
                    }
                }

                if (extra.Count > 0)
                {
                    PrintHeader(DeckView.DeckLocation.ExtraDeck);
                    var list = new List<int>();
                    for (int i = 0; i < extra.Count; i++)
                    {
                        list.Add(extra[i]);
                        if (list.Count == columns || i == extra.Count - 1)
                        {
                            await PrintCards(list);
                            list.Clear();
                        }
                    }
                }

                if (side.Count > 0)
                {
                    PrintHeader(DeckView.DeckLocation.SideDeck);
                    var list = new List<int>();
                    for (int i = 0; i < side.Count; i++)
                    {
                        list.Add(side[i]);
                        if (list.Count == columns || i == side.Count - 1)
                        {
                            await PrintCards(list);
                            list.Clear();
                        }
                    }
                }
            }

            private void PrintHeader(DeckView.DeckLocation location)
            {
                var header = Instantiate(TemplateHeader, Content, false);
                header.SetActive(true);
                var manager = header.GetComponent<ElementObjectManager>();
                manager.GetElement("IconMainDeck").SetActive(location == DeckView.DeckLocation.MainDeck);
                manager.GetElement("IconExtraDeck").SetActive(location == DeckView.DeckLocation.ExtraDeck);
                manager.GetElement("IconSideDeck").SetActive(location == DeckView.DeckLocation.SideDeck);
                manager.GetElement<TextMeshProUGUI>("TextDeck").text = location switch
                {
                    DeckView.DeckLocation.MainDeck => InterString.Get("主卡组"),
                    DeckView.DeckLocation.ExtraDeck => InterString.Get("额外卡组"),
                    DeckView.DeckLocation.SideDeck => InterString.Get("副卡组"),
                    _ => string.Empty
                };
            }

            private async UniTask PrintCards(List<int> cards)
            {
                var cardGroup = Instantiate(TemplateCardGroup, Content, false);
                cardGroup.SetActive(true);
                var manager = cardGroup.GetComponent<ElementObjectManager>();
                var template = manager.GetElement("CardTemplate");
                template.SetActive(false);
                var parent = manager.GetElement<RectTransform>("CardParent");
                for(int i = 0; i < cards.Count; i++)
                {
                    var cardObj = Instantiate(template, parent);
                    cardObj.SetActive(true);
                    var cardManager = cardObj.GetComponent<ElementObjectManager>();
                    var image = cardManager.GetElement<CardRawImageHandler>("CardImage");
                    var code = cards[i];
                    image.SetCard(code);
                    cardManager.GetComponent<SelectionButton>().SetClickEvent(() =>
                    {
                        if (cardDetailView != null)
                        {
                            var data = CardsManager.GetCard(code);
                            cardDetailView.ShowCard(data);
                        }
                    });
                    if(i == cards.Count - 1)
                        await UniTask.WaitUntil(() => image.Refreshed);
                }
            }
        }

    }
}
