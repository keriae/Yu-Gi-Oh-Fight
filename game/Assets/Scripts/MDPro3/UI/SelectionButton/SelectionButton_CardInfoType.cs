using UnityEngine;
using UnityEngine.Events;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;
using TMPro;
using MDPro3.Net;
using MDPro3.Duel.YGOSharp;

namespace MDPro3.UI
{
    public class SelectionButton_CardInfoType : SelectionButton
    {
        #region Elements

        private const string LABEL_GO_INFO0 = "IconInfoSwitching0";
        private GameObject m_Info0;
        private GameObject Info0 =>
            m_Info0 = m_Info0 != null ? m_Info0
            : Manager.GetElement(LABEL_GO_INFO0);

        private const string LABEL_GO_INFO1 = "IconInfoSwitching1";
        private GameObject m_Info1;
        private GameObject Info1 =>
            m_Info1 = m_Info1 != null ? m_Info1
            : Manager.GetElement(LABEL_GO_INFO1);

        private const string LABEL_GO_INFO2 = "IconInfoSwitching2";
        private GameObject m_Info2;
        private GameObject Info2 =>
            m_Info2 = m_Info2 != null ? m_Info2
            : Manager.GetElement(LABEL_GO_INFO2);

        private const string LABEL_GO_INFO3 = "IconInfoSwitching3";
        private GameObject m_Info3;
        private GameObject Info3 =>
            m_Info3 = m_Info3 != null ? m_Info3
            : Manager.GetElement(LABEL_GO_INFO3);

        private const string LABEL_TXT_GC = "TextGenesysCredit";
        private TextMeshProUGUI m_TextGC;
        private TextMeshProUGUI TextGC =>
            m_TextGC = m_TextGC != null ? m_TextGC
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_GC);

        #endregion

        public static SelectionButton_CardInfoType instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
            SetCardInfoTypeIcon(DeckEditor.cardInfoType);
            SetClickEvent(ClickEvent);
        }

        private void ClickEvent()
        {
            var type = (DeckEditor.CardInfoType)(((int)DeckEditor.cardInfoType + 1) % 4);
            Program.instance.deckEditor.SetCardInfoType(type);
        }

        public void SetCardInfoTypeIcon(DeckEditor.CardInfoType type)
        {
            Info0.SetActive(type == DeckEditor.CardInfoType.None);
            Info1.SetActive(type == DeckEditor.CardInfoType.Detail);
            Info2.SetActive(type == DeckEditor.CardInfoType.Pool);
            Info3.SetActive(type == DeckEditor.CardInfoType.Genesys);
            TextGC.gameObject.SetActive(type == DeckEditor.CardInfoType.Genesys);
        }

        public static void SetGenesysCredits(int gc)
        {
            if (instance == null)
                return;
            instance.TextGC.text = gc.ToString();
            instance.TextGC.color = BanlistManager.currentGenesysBanList.GetCreditLimitColor(gc);
        }
    }
}
