using Cysharp.Threading.Tasks;


using TMPro;
using UnityEngine;

using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
{
    public class DeckPickup : MonoBehaviour
    {

        #region Elements

        private Presentation.Elements.ElementObjectManager m_Manager;
        private Presentation.Elements.ElementObjectManager Manager =>
            m_Manager = m_Manager != null ? m_Manager
            : GetComponent<Presentation.Elements.ElementObjectManager>();

        private const string LABEL_IMG_DECKCASE = "DeckCase";
        private Image m_DeckCase;
        private Image DeckCase =>
            m_DeckCase = m_DeckCase != null ? m_DeckCase
            : Manager.GetElement<Image>(LABEL_IMG_DECKCASE);

        private const string LABEL_CRH_CARD1 = "Card1";
        private CardRawImageHandler m_Card1;
        private CardRawImageHandler Card1 =>
            m_Card1 = m_Card1 != null ? m_Card1
            : Manager.GetElement<CardRawImageHandler>(LABEL_CRH_CARD1);

        private const string LABEL_CRH_CARD2 = "Card2";
        private CardRawImageHandler m_Card2;
        private CardRawImageHandler Card2 =>
            m_Card2 = m_Card2 != null ? m_Card2
            : Manager.GetElement<CardRawImageHandler>(LABEL_CRH_CARD2);

        private const string LABEL_CRH_CARD3 = "Card3";
        private CardRawImageHandler m_Card3;
        private CardRawImageHandler Card3 =>
            m_Card3 = m_Card3 != null ? m_Card3
            : Manager.GetElement<CardRawImageHandler>(LABEL_CRH_CARD3);

        private const string LABEL_TXT_SETTING = "TextSetting";
        private TextMeshProUGUI m_TextSetting;
        private TextMeshProUGUI TextSetting =>
            m_TextSetting = m_TextSetting != null ? m_TextSetting
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_SETTING);

        #endregion

        public async void SetDeck(MDPro3.Duel.YGOSharp.Deck deck)
        {
            var setting = string.Empty;
            setting += GetCardName(deck.Pickup[0], 1);
            setting += GetCardName(deck.Pickup[1], 2);
            setting += GetCardName(deck.Pickup[2], 3);
            TextSetting.text = setting;

            Card1.protectorCode = deck.Protector;
            Card2.protectorCode = deck.Protector;
            Card3.protectorCode = deck.Protector;

            Card1.SetCard(deck.Pickup[0]);
            Card2.SetCard(deck.Pickup[1]);
            Card3.SetCard(deck.Pickup[2]);
            await LoadDeckCaseAsync(deck.Case);
        }

        private async UniTask LoadDeckCaseAsync(int deckCase)
        {
            var sprite = await Runtime.Program.items.LoadDeckCaseIconAsync(deckCase, "_Open_L_HD");
            if (sprite != null)
                DeckCase.sprite = sprite;
        }

        private string GetCardName(int code, int index)
        {
            var result = $"{index} : ";
            MDPro3.Duel.YGOSharp.Card card = MDPro3.Duel.YGOSharp.CardsManager.Get(code);
            if (card == null || card.Id == 0)
                result += MDPro3.InterString.Get("未设置");
            else
                result += card.Name;
            result += Runtime.Program.STRING_LINE_BREAK;
            return result;
        }
    }
}
