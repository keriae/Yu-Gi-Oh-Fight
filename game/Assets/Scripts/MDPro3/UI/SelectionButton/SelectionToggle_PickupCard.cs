using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YgomSystem.ElementSystem;
using YgomSystem.UI;

namespace MDPro3.UI
{
    public class SelectionToggle_PickupCard : SelectionToggle
    {
        #region Elements

        private const string LABEL_IMAGE_CARD = "ImageCard";
        private CardRawImageHandler m_ImageCardHandler;
        private CardRawImageHandler ImageCardHandler =>
            m_ImageCardHandler = m_ImageCardHandler != null ? m_ImageCardHandler
            : Manager.GetElement<CardRawImageHandler>(LABEL_IMAGE_CARD);

        private const string LABEL_ICON_LIMIT = "IconLimit";
        private Image m_IconLimit;
        private Image IconLimit =>
            m_IconLimit = m_IconLimit != null ? m_IconLimit
            : Manager.GetElement<Image>(LABEL_ICON_LIMIT);

        private const string LABEL_ICON_ATTRIBUTE = "IconAttribute";
        private Image m_IconAttribute;
        private Image IconAttribute =>
            m_IconAttribute = m_IconAttribute != null ? m_IconAttribute
            : Manager.GetElement<Image>(LABEL_ICON_ATTRIBUTE);

        private const string LABEL_ICON_SPELL_TRAP_TYPE = "IconSpellTrapType";
        private Image m_IconSpellTrapType;
        private Image IconSpellTrapType =>
            m_IconSpellTrapType = m_IconSpellTrapType != null ? m_IconSpellTrapType
            : Manager.GetElement<Image>(LABEL_ICON_SPELL_TRAP_TYPE);

        private const string LABEL_ICON_RACE = "IconRace";
        private Image m_IconRace;
        private Image IconRace =>
            m_IconRace = m_IconRace != null ? m_IconRace
            : Manager.GetElement<Image>(LABEL_ICON_RACE);

        private const string LABEL_ICON_POOL = "IconPool";
        private Image m_IconPool;
        private Image IconPool =>
            m_IconPool = m_IconPool != null ? m_IconPool
            : Manager.GetElement<Image>(LABEL_ICON_POOL);

        private const string LABEL_ICON_TUNER = "IconTuner";
        private Image m_IconTuner;
        private Image IconTuner =>
            m_IconTuner = m_IconTuner != null ? m_IconTuner
            : Manager.GetElement<Image>(LABEL_ICON_TUNER);

        private const string LABEL_ICON_LEVEL = "IconLevel";
        private Image m_IconLevel;
        private Image IconLevel =>
            m_IconLevel = m_IconLevel != null ? m_IconLevel
            : Manager.GetElement<Image>(LABEL_ICON_LEVEL);

        private const string LABEL_ICON_RANK = "IconRank";
        private Image m_IconRank;
        private Image IconRank =>
            m_IconRank = m_IconRank != null ? m_IconRank
            : Manager.GetElement<Image>(LABEL_ICON_RANK);

        private const string LABEL_ICON_LINK = "IconLink";
        private Image m_IconLink;
        private Image IconLink =>
            m_IconLink = m_IconLink != null ? m_IconLink
            : Manager.GetElement<Image>(LABEL_ICON_LINK);

        private const string LABEL_ICON_PENDULUM_SCALE = "IconPendulumScale";
        private Image m_IconPendulumScale;
        private Image IconPendulumScale =>
            m_IconPendulumScale = m_IconPendulumScale != null ? m_IconPendulumScale
            : Manager.GetElement<Image>(LABEL_ICON_PENDULUM_SCALE);

        private const string LABEL_TEXT_LEVEL = "TextLevel";
        private TextMeshProUGUI m_TextLevel;
        private TextMeshProUGUI TextLevel =>
            m_TextLevel = m_TextLevel != null ? m_TextLevel
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TEXT_LEVEL);

        private const string LABEL_TEXT_RANK = "TextRank";
        private TextMeshProUGUI m_TextRank;
        private TextMeshProUGUI TextRank =>
            m_TextRank = m_TextRank != null ? m_TextRank
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TEXT_RANK);

        private const string LABEL_TEXT_LINK = "TextLink";
        private TextMeshProUGUI m_TextLink;
        private TextMeshProUGUI TextLink =>
            m_TextLink = m_TextLink != null ? m_TextLink
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TEXT_LINK);

        private const string LABEL_TEXT_PENDULUM_SCALE = "TextPendulumScale";
        private TextMeshProUGUI m_TextPendulumScale;
        private TextMeshProUGUI TextPendulumScale =>
            m_TextPendulumScale = m_TextPendulumScale != null ? m_TextPendulumScale
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TEXT_PENDULUM_SCALE);

        private const string LABEL_CCG_CURSOR_CARD_SELECT = "CursorCardSelect";
        private ColorContainerGraphic m_CursorCardSelect;
        private ColorContainerGraphic CursorCardSelect =>
            m_CursorCardSelect = m_CursorCardSelect != null ? m_CursorCardSelect
            : Manager.GetElement<ColorContainerGraphic>(LABEL_CCG_CURSOR_CARD_SELECT);

        private const string LABEL_RIMG_BACK = "ImageBack";
        private RawImage m_ImageBack;
        private RawImage ImageBack =>
            m_ImageBack = m_ImageBack != null ? m_ImageBack
            : Manager.GetElement<RawImage>(LABEL_RIMG_BACK);

        #endregion

        public int pickUpIndex;
        [HideInInspector] public bool cardSetted = false;

        protected override void Awake()
        {
            base.Awake();

            exclusiveToggle = true;
            canToggleOffSelf = false;
        }

        protected override void OnEnter()
        {
            base.OnEnter();
            CursorCardSelect.SetColor(ColorContainerGraphic.SelectMode.Selected, ColorContainer.StatusMode.Normal);
        }

        public void SetCard(Card card, bool save)
        {
            ImageCardHandler.SetCard(card);
            cardSetted = true;
            ImageBack.gameObject.SetActive(false);
            if (save)
            {
                Program.instance.deckEditor.GetUI<DeckEditorUI>().DeckView.SetDirty(true);
                DeckEditor.Deck.Pickup[pickUpIndex] = card.Id;
            }
        }

        public void ClearCard(bool alsoSelect, bool save)
        {
            cardSetted = false;
            ImageBack.gameObject.SetActive(true);
            Program.instance.deckBrowser.GetUI<DeckBrowserUI>().DeckView.Depickup(pickUpIndex);
            if(alsoSelect)
                SetToggleOn();
            if (save)
            {
                Program.instance.deckEditor.GetUI<DeckEditorUI>().DeckView.SetDirty(true);
                DeckEditor.Deck.Pickup[pickUpIndex] = 0;
            }
        }

        protected override void OnClick()
        {
            OnSubmit();
        }

        protected override void OnSubmit()
        {
            if (isOn)
            {
                AudioManager.PlaySE(Selectable.interactable ? SoundLabelClick : SoundLabelClickInactive);
                ClearCard(true, true);
            }
            else
            {
                AudioManager.PlaySE(Selectable.interactable ? SoundLabelClick : SoundLabelClickInactive);
                SetToggleOn();
            }
        }
    }
}