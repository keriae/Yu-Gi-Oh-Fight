using System.Text;
using TMPro;
using UnityEngine;
using YgomSystem.ElementSystem;

namespace MDPro3.UI
{
    [RequireComponent(typeof(ElementObjectManager))]
    public class DeckNumCounter : MonoBehaviour
    {
        private ElementObjectManager manager;
        protected ElementObjectManager Manager =>
            manager = manager != null ? manager
            : GetComponent<ElementObjectManager>();

        private const string LABEL_TEXT_MAIN = "MainDeckNumText";
        private TextMeshProUGUI m_TextMain;
        private TextMeshProUGUI TextMain =>
            m_TextMain = m_TextMain != null ? m_TextMain
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TEXT_MAIN);

        private const string LABEL_TEXT_EXTRA = "ExtraDeckNumText";
        private TextMeshProUGUI m_TextExtra;
        private TextMeshProUGUI TextExtra =>
            m_TextExtra = m_TextExtra != null ? m_TextExtra
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TEXT_EXTRA);

        private const string LABEL_TEXT_SIDE = "SideDeckNumText";
        private TextMeshProUGUI m_TextSide;
        private TextMeshProUGUI TextSide =>
            m_TextSide = m_TextSide != null ? m_TextSide
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TEXT_SIDE);



        public void SetNum(int main, int extra, int side)
        {
            TextMain.text = Tools.GetNormalizedNumText(main);
            TextExtra.text = Tools.GetNormalizedNumText(extra);
            TextSide.text = Tools.GetNormalizedNumText(side);
        }

    }
}
