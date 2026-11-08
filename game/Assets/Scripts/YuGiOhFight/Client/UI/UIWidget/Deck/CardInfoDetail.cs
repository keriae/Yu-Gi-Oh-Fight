using Cysharp.Threading.Tasks;
using DG.Tweening;


using System;

using System.Collections.Generic;
using System.IO;
using System.Threading;
using TMPro;
using UnityEngine;

using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
{
    public class CardInfoDetail : UIWidgetFullScreen
    {
        #region Elements

        #region BG

        private const string LABEL_SBN_NEXT = "NextButton";
        private SelectionButton m_ButtonNext;
        private SelectionButton ButtonNext =>
            m_ButtonNext = m_ButtonNext != null ? m_ButtonNext
            : Manager.GetElement<SelectionButton>(LABEL_SBN_NEXT);

        private const string LABEL_SBN_PREV = "PrevButton";
        private SelectionButton m_ButtonPrev;
        private SelectionButton ButtonPrev =>
            m_ButtonPrev = m_ButtonPrev != null ? m_ButtonPrev
            : Manager.GetElement<SelectionButton>(LABEL_SBN_PREV);

        #endregion

        #region Card Area

        private const string LABEL_MONO_IMAGECARD = "ImageCard";
        private CardRawImageHandler m_ImageCard;
        private CardRawImageHandler ImageCard =>
            m_ImageCard = m_ImageCard != null ? m_ImageCard
            : Manager.GetElement<CardRawImageHandler>(LABEL_MONO_IMAGECARD);

        private const string LABEL_IMG_LIMIT = "IconLimit";
        private Image m_IconLimit;
        protected Image IconLimit =>
            m_IconLimit = m_IconLimit != null ? m_IconLimit
            : Manager.GetElement<Image>(LABEL_IMG_LIMIT);

        #endregion

        #region Title Area

        private const string LABEL_MS_PLATE_TITLE = "PlateTitle";
        private MaterialSetter m_PlateTitle;
        protected MaterialSetter PlateTitle =>
            m_PlateTitle = m_PlateTitle != null ? m_PlateTitle
            : Manager.GetElement<MaterialSetter>(LABEL_MS_PLATE_TITLE);

        private const string LABEL_TXT_CARDNAME = "TextCardName";
        private TextMeshProUGUI m_TextCardName;
        protected TextMeshProUGUI TextCardName =>
            m_TextCardName = m_TextCardName != null ? m_TextCardName
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_CARDNAME);

        private const string LABEL_IMG_ATTRIBUTE = "IconAttribute";
        private Image m_IconAttribute;
        private Image IconAttribute =>
            m_IconAttribute = m_IconAttribute != null ? m_IconAttribute
            : Manager.GetElement<Image>(LABEL_IMG_ATTRIBUTE);

        #endregion

        #region Paramator Area

        private const string LABEL_MS_PLATE_PARAMATOR = "PlateParamator";
        private MaterialSetter m_PlateParamator;
        protected MaterialSetter PlateParamator =>
            m_PlateParamator = m_PlateParamator != null ? m_PlateParamator
            : Manager.GetElement<MaterialSetter>(LABEL_MS_PLATE_PARAMATOR);

        private const string LABEL_GO_PARAMATOR_AREA_TOP = "ParamatorAreaTop";
        private GameObject m_ParamatorAreaTop;
        private GameObject ParamatorAreaTop =>
            m_ParamatorAreaTop = m_ParamatorAreaTop != null ? m_ParamatorAreaTop
            : Manager.GetElement(LABEL_GO_PARAMATOR_AREA_TOP);

        private const string LABEL_GO_PARAMATOR_AREA_BOTTOM = "ParamatorAreaBottom";
        private GameObject m_ParamatorAreaBottom;
        private GameObject ParamatorAreaBottom =>
            m_ParamatorAreaBottom = m_ParamatorAreaBottom != null ? m_ParamatorAreaBottom
            : Manager.GetElement(LABEL_GO_PARAMATOR_AREA_BOTTOM);

        private const string LABEL_IMG_LEVEL = "IconLevel";
        private Image m_IconLevel;
        protected Image IconLevel =>
            m_IconLevel = m_IconLevel != null ? m_IconLevel
            : Manager.GetElement<Image>(LABEL_IMG_LEVEL);

        private const string LABEL_TXT_LEVEL = "TextLevel";
        private TextMeshProUGUI m_TextLevel;
        protected TextMeshProUGUI TextLevel =>
            m_TextLevel = m_TextLevel != null ? m_TextLevel
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_LEVEL);

        private const string LABEL_IMG_RANK = "IconRank";
        private Image m_IconRank;
        protected Image IconRank =>
            m_IconRank = m_IconRank != null ? m_IconRank
            : Manager.GetElement<Image>(LABEL_IMG_RANK);

        private const string LABEL_TXT_RANK = "TextRank";
        private TextMeshProUGUI m_TextRank;
        protected TextMeshProUGUI TextRank =>
            m_TextRank = m_TextRank != null ? m_TextRank
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_RANK);

        private const string LABEL_IMG_PENDULUMSCALE = "IconPendulumScale";
        private Image m_IconPendulumScale;
        protected Image IconPendulumScale =>
            m_IconPendulumScale = m_IconPendulumScale != null ? m_IconPendulumScale
            : Manager.GetElement<Image>(LABEL_IMG_PENDULUMSCALE);

        private const string LABEL_TXT_PENDULUMSCALE = "TextPendulumScale";
        private TextMeshProUGUI m_TextPendulumScale;
        protected TextMeshProUGUI TextPendulumScale =>
            m_TextPendulumScale = m_TextPendulumScale != null ? m_TextPendulumScale
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_PENDULUMSCALE);

        private const string LABEL_IMG_LINK = "IconLink";
        private Image m_IconLink;
        protected Image IconLink =>
            m_IconLink = m_IconLink != null ? m_IconLink
            : Manager.GetElement<Image>(LABEL_IMG_LINK);

        private const string LABEL_TXT_LINK = "TextLink";
        private TextMeshProUGUI m_TextLink;
        protected TextMeshProUGUI TextLink =>
            m_TextLink = m_TextLink != null ? m_TextLink
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_LINK);

        private const string LABEL_GO_RACE_GROUP = "RaceGroup";
        private GameObject m_RaceGroup;
        private GameObject RaceGroup =>
            m_RaceGroup = m_RaceGroup != null ? m_RaceGroup
            : Manager.GetElement(LABEL_GO_RACE_GROUP);

        private const string LABEL_IMG_RACE = "IconRace";
        private Image m_IconRace;
        protected Image IconRace =>
            m_IconRace = m_IconRace != null ? m_IconRace
            : Manager.GetElement<Image>(LABEL_IMG_RACE);

        private const string LABEL_GO_TUNER_GROUP = "TunerGroup";
        private GameObject m_TunerGroup;
        private GameObject TunerGroup =>
            m_TunerGroup = m_TunerGroup != null ? m_TunerGroup
            : Manager.GetElement(LABEL_GO_TUNER_GROUP);

        private const string LABEL_IMG_TUNER = "IconTuner";
        private Image m_IconTuner;
        protected Image IconTuner =>
            m_IconTuner = m_IconTuner != null ? m_IconTuner
            : Manager.GetElement<Image>(LABEL_IMG_TUNER);

        private const string LABEL_GO_SPELLTRAPTYPE = "SpellTrapType";
        private GameObject m_SpellTrapType;
        protected GameObject SpellTrapType =>
            m_SpellTrapType = m_SpellTrapType != null ? m_SpellTrapType
            : Manager.GetElement(LABEL_GO_SPELLTRAPTYPE);

        private const string LABEL_IMG_SPELLTRAPTYPE = "IconSpellTrapType";
        private Image m_IconSpellTrapType;
        protected Image IconSpellTrapType =>
            m_IconSpellTrapType = m_IconSpellTrapType != null ? m_IconSpellTrapType
            : Manager.GetElement<Image>(LABEL_IMG_SPELLTRAPTYPE);

        private const string LABEL_TXT_SPELLTRAPTYPE = "TextSpellTrapType";
        private TextMeshProUGUI m_TextSpellTrapType;
        protected TextMeshProUGUI TextSpellTrapType =>
            m_TextSpellTrapType = m_TextSpellTrapType != null ? m_TextSpellTrapType
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_SPELLTRAPTYPE);

        private const string LABEL_TXT_ATK = "TextAtk";
        private TextMeshProUGUI m_TextAtk;
        protected TextMeshProUGUI TextAtk =>
            m_TextAtk = m_TextAtk != null ? m_TextAtk
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_ATK);

        private const string LABEL_IMG_DEF = "IconDef";
        private Image m_IconDef;
        protected Image IconDef =>
            m_IconDef = m_IconDef != null ? m_IconDef
            : Manager.GetElement<Image>(LABEL_IMG_DEF);

        private const string LABEL_TXT_DEF = "TextDef";
        private TextMeshProUGUI m_TextDef;
        protected TextMeshProUGUI TextDef =>
            m_TextDef = m_TextDef != null ? m_TextDef
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_DEF);

        private const string LABEL_RT_POOL_GROUP = "PoolGroup";
        private RectTransform m_PoolGroup;
        private RectTransform PoolGroup =>
            m_PoolGroup = m_PoolGroup != null ? m_PoolGroup
            : Manager.GetElement<RectTransform>(LABEL_RT_POOL_GROUP);

        private const string LABEL_IMG_ICON_OCG = "IconOCG";
        private Image m_IconOCG;
        protected Image IconOCG =>
            m_IconOCG = m_IconOCG != null ? m_IconOCG
            : Manager.GetElement<Image>(LABEL_IMG_ICON_OCG);

        private const string LABEL_IMG_ICON_TCG = "IconTCG";
        private Image m_IconTCG;
        protected Image IconTCG =>
            m_IconTCG = m_IconTCG != null ? m_IconTCG
            : Manager.GetElement<Image>(LABEL_IMG_ICON_TCG);

        private const string LABEL_IMG_ICON_CCG = "IconCCG";
        private Image m_IconCCG;
        protected Image IconCCG =>
            m_IconCCG = m_IconCCG != null ? m_IconCCG
            : Manager.GetElement<Image>(LABEL_IMG_ICON_CCG);

        private const string LABEL_IMG_ICON_DIY = "IconDIY";
        private Image m_IconDIY;
        protected Image IconDIY =>
            m_IconDIY = m_IconDIY != null ? m_IconDIY
            : Manager.GetElement<Image>(LABEL_IMG_ICON_DIY);

        private const string LABEL_IMG_ICON_RD = "IconRD";
        private Image m_IconRD;
        protected Image IconRD =>
            m_IconRD = m_IconRD != null ? m_IconRD
            : Manager.GetElement<Image>(LABEL_IMG_ICON_RD);

        private const string LABEL_IMG_ICON_BETA = "IconBETA";
        private Image m_IconBETA;
        protected Image IconBETA =>
            m_IconBETA = m_IconBETA != null ? m_IconBETA
            : Manager.GetNestedElement<Image>(LABEL_IMG_ICON_BETA);

        private const string LABEL_TXT_GP = "TextGP";
        private TextMeshProUGUI m_TextGP;
        protected TextMeshProUGUI TextGP =>
            m_TextGP = m_TextGP != null ? m_TextGP
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_GP);

        #endregion

        #region Pendulum Description Area

        private const string LABEL_GO_PENDULUM_DESCRIPTION_AREA = "PendulumDescriptionArea";
        private GameObject m_PendulumDescriptionArea;
        private GameObject PendulumDescriptionArea =>
            m_PendulumDescriptionArea = m_PendulumDescriptionArea != null ? m_PendulumDescriptionArea
            : Manager.GetElement(LABEL_GO_PENDULUM_DESCRIPTION_AREA);

        private const string LABEL_MS_PLATE_PENDULUM_DESCRIPTION = "PlatePendulumDescription";
        private MaterialSetter m_PlatePendulumDescription;
        private MaterialSetter PlatePendulumDescription =>
            m_PlatePendulumDescription = m_PlatePendulumDescription != null ? m_PlatePendulumDescription
            : Manager.GetElement<MaterialSetter>(LABEL_MS_PLATE_PENDULUM_DESCRIPTION);

        private const string LABEL_TXT_PENDULUM_DESCRIPTION_VALUE = "TextPendulumDescriptionValue";
        private TextMeshProUGUI m_TextPendulumDescriptionValue;
        private TextMeshProUGUI TextPendulumDescriptionValue =>
            m_TextPendulumDescriptionValue = m_TextPendulumDescriptionValue != null ? m_TextPendulumDescriptionValue
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_PENDULUM_DESCRIPTION_VALUE);

        #endregion

        #region Description Area

        private const string LABEL_MS_PLATE_DESCRIPTION = "PlateDescription";
        private MaterialSetter m_PlateDescription;
        private MaterialSetter PlateDescription =>
            m_PlateDescription = m_PlateDescription != null ? m_PlateDescription
            : Manager.GetElement<MaterialSetter>(LABEL_MS_PLATE_DESCRIPTION);

        private const string LABEL_TXT_DESCRIPTION_ITEM = "TextDescriptionItem";
        private TextMeshProUGUI m_TextDescriptionItem;
        private TextMeshProUGUI TextDescriptionItem =>
            m_TextDescriptionItem = m_TextDescriptionItem != null ? m_TextDescriptionItem
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_DESCRIPTION_ITEM);

        private const string LABEL_TXT_DESCRIPTION_VALUE = "TextDescriptionValue";
        private TextMeshProUGUI m_TextDescriptionValue;
        private TextMeshProUGUI TextDescriptionValue =>
            m_TextDescriptionValue = m_TextDescriptionValue != null ? m_TextDescriptionValue
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_DESCRIPTION_VALUE);

        #endregion

        #endregion

        private MDPro3.Duel.YGOSharp.Card card;
        private List<int> cards;
        private int cardIndex;
        private bool shifting;

        public override void Show()
        {
            Client.FightDuelCardDetailSkin.Attach(this, false);
            base.Show();
        }

        public void Show(MDPro3.Duel.YGOSharp.Card data)
        {
            cards = null;
            SetData(data);
            ButtonNext.gameObject.SetActive(false);
            ButtonPrev.gameObject.SetActive(false);
            Runtime.UIManager.ShowFPSLeft();
            Show();
        }

        public void Show(List<int> cards, int index)
        {
            this.cards = cards;
            cardIndex = index;
            SetData(MDPro3.Duel.YGOSharp.CardsManager.Get(cards[index]));
            ButtonNext.gameObject.SetActive(true);
            ButtonPrev.gameObject.SetActive(true);
            SetButtons();
            Runtime.UIManager.ShowFPSLeft();
            Show();
        }

        public override void Hide()
        {
            base.Hide();
            if(Runtime.Program.instance.currentServant != Runtime.Program.instance.ocgcore)
                Runtime.UIManager.ShowFPSRight();
        }

        private void SetData(MDPro3.Duel.YGOSharp.Card data)
        {
            card = data;

            #region Card Area

            ImageCard.SetCard(data);
            IconLimit.sprite = Runtime.TextureManager.container
                .GetCardRegulationIcon(data.Id, Controllers.DeckEditor.Banlist);

            IconLimit.sprite = Runtime.TextureManager.container
                .GetCardRegulationIcon(data.Id, Controllers.DeckEditor.Banlist);

            #endregion

            #region Status Area

            var colors = Duel.CardDescription.GetCardFrameColor(data);
            Action<Material> plateLoad = (matetial) =>
            {
                matetial.SetColor("_Color0", colors[0]);
                matetial.SetColor("_Color1", colors[1]);
            };

            PlateTitle.SetMaterialAction(plateLoad);
            PlateParamator.SetMaterialAction(plateLoad);
            PlateDescription.SetMaterialAction(plateLoad);

            TextCardName.text = " " + Duel.CardDescription.GetDisplayedCardName(data, true);
            IconAttribute.sprite = Runtime.TextureManager.container.GetCardAttributeIcon(data);

            #endregion

            #region Paramator Area

            if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Monster))
            {
                IconLevel.gameObject.SetActive(true);
                IconRank.gameObject.SetActive(true);
                IconLink.gameObject.SetActive(true);
                IconPendulumScale.gameObject.SetActive(true);
                RaceGroup.SetActive(true);
                TunerGroup.SetActive(true);

                ParamatorAreaBottom.SetActive(true);

                MDPro3.Duel.YGOSharp.Card.LevelType levelType = data.GetLevelType();

                IconLevel.gameObject.SetActive(data.HasType(MDPro3.Duel.YGOSharp.CardType.Monster) && levelType == MDPro3.Duel.YGOSharp.Card.LevelType.Level);
                TextLevel.text = data.Level.ToString();

                IconRank.gameObject.SetActive(levelType == MDPro3.Duel.YGOSharp.Card.LevelType.Rank);
                TextRank.text = data.Level.ToString();

                IconLink.gameObject.SetActive(levelType == MDPro3.Duel.YGOSharp.Card.LevelType.Link);
                TextLink.text = data.GetLinkCount().ToString();

                IconPendulumScale.gameObject.SetActive(data.HasType(MDPro3.Duel.YGOSharp.CardType.Pendulum));
                TextPendulumScale.text = data.LScale.ToString();

                IconRace.sprite = Runtime.TextureManager.container.GetCardRaceIcon(data);
                IconTuner.gameObject.SetActive(data.HasType(MDPro3.Duel.YGOSharp.CardType.Tuner));

                SpellTrapType.SetActive(false);

                TextAtk.text = data.GetAttackString();
                IconDef.gameObject.SetActive(levelType != MDPro3.Duel.YGOSharp.Card.LevelType.Link);
                TextDef.text = data.GetDefenseString();

                PoolGroup.SetParent(ParamatorAreaBottom.transform, false);
            }
            else
            {
                IconLevel.gameObject.SetActive(false);
                IconRank.gameObject.SetActive(false);
                IconLink.gameObject.SetActive(false);
                IconPendulumScale.gameObject.SetActive(false);
                RaceGroup.SetActive(false);
                TunerGroup.SetActive(false);

                ParamatorAreaBottom.SetActive(false);
                SpellTrapType.SetActive(true);

                var spellTrapTypeIcon = Runtime.TextureManager.container.GetCardSpellTrapTypeIcon(data);
                var hasSpellTrapTypeIcon = spellTrapTypeIcon != null
                    && spellTrapTypeIcon != Runtime.TextureManager.container.typeNone;
                IconSpellTrapType.gameObject.SetActive(hasSpellTrapTypeIcon);
                IconSpellTrapType.sprite = hasSpellTrapTypeIcon ? spellTrapTypeIcon : null;
                TextSpellTrapType.text = data.GetSpellTrapType();
                PoolGroup.SetParent(ParamatorAreaTop.transform, false);
            }

            IconOCG.gameObject.SetActive((data.Ot & 1) > 0);
            IconTCG.gameObject.SetActive((data.Ot & 2) > 0);
            IconCCG.gameObject.SetActive((data.Ot & 8) > 0);
            if (data.IsRushDuelCard())
            {
                IconDIY.gameObject.SetActive(false);
                IconRD.gameObject.SetActive(true);
            }
            else
            {
                IconDIY.gameObject.SetActive((data.Ot & 4) > 0);
                IconRD.gameObject.SetActive(false);
            }
            IconBETA.gameObject.SetActive(data.isPre);

            TextGP.text = string.Format("G:{0}", data.GetCreditString());
            TextGP.color = MDPro3.Duel.YGOSharp.BanlistManager.currentGenesysBanList.GetCreditColor(card.GetCredit());

            #endregion

            #region Description Area

            if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Pendulum))
            {
                PendulumDescriptionArea.SetActive(true);
                PlatePendulumDescription.SetMaterialAction(plateLoad);
                TextPendulumDescriptionValue.text = data.GetPendulumDescription();
            }
            else
            {
                PendulumDescriptionArea.SetActive(false);
            }
            TextDescriptionItem.text = data.GetTypeForUI() + data.GetSetNameWithBracket() + Duel.CardDescription.GetDisplayedCardIdWithBracket(data);
            TextDescriptionValue.text = data.GetMonsterDescription();

            #endregion
        }

        private void SetButtons()
        {
            ButtonNext.SetInteractable(CanNext());
            ButtonPrev.SetInteractable(CanPrev());
        }

        private bool CanNext()
        {
            if(cards == null)
                return false;
            if(cardIndex >= cards.Count - 1)
                return false;
            return true;
        }

        private bool CanPrev()
        {
            if (cards == null)
                return false;
            if (cardIndex == 0)
                return false;
            return true;
        }

        public void ShowCardExpand()
        {
            Runtime.UIManager.ShowCardExpand(card);
        }

        public void OnNext()
        {
            if (shifting) return;
            if(!CanNext()) return;

            shifting = true;
            Runtime.AudioManager.PlaySE("SE_MENU_SELECT_01");

            WindowCG.alpha = 1f;

            DOTween.Sequence()
                .SetUpdate(true)
                .Append(Window.DOAnchorPos(new Vector2(-480f, -32f), 0.1f).SetEase(Ease.InCubic))
                .Join(WindowCG.DOFade(0f, 0.1f).OnComplete(() =>
                {
                    Window.anchoredPosition = new Vector2(480f, 0f);
                    SetData(MDPro3.Duel.YGOSharp.CardsManager.Get(cards[++cardIndex]));
                    SetButtons();
                }))
                .Append(Window.DOAnchorPos(Vector2.zero, 0.2f).SetEase(Ease.OutQuart))
                .Join(WindowCG.DOFade(1f, 0.2f))
                .OnComplete(() =>
                {
                    shifting = false;
                });
        }

        public void OnPrev()
        {
            if (shifting) return;
            if (!CanPrev()) return;

            shifting = true;
            Runtime.AudioManager.PlaySE("SE_MENU_SELECT_01");

            WindowCG.alpha = 1f;

            DOTween.Sequence()
                .SetUpdate(true)
                .Append(Window.DOAnchorPos(new Vector2(480f, -32f), 0.1f).SetEase(Ease.InCubic))
                .Join(WindowCG.DOFade(0f, 0.1f).OnComplete(() =>
                {
                    Window.anchoredPosition = new Vector2(-480f, -32f);
                    SetData(MDPro3.Duel.YGOSharp.CardsManager.Get(cards[--cardIndex]));
                    SetButtons();
                }))
                .Append(Window.DOAnchorPos(new Vector2(0f, -32f), 0.2f).SetEase(Ease.OutQuart))
                .Join(WindowCG.DOFade(1f, 0.2f))
                .OnComplete(() =>
                {
                    shifting = false;
                });
        }

        protected override void Update()
        {
            if (!NeedResponse()) return;

            if (Runtime.UserInput.WasCancelPressed || Runtime.UserInput.MouseRightDown)
                Hide();

            if (Runtime.UserInput.WasLeftTriggerPressed)
                ShowCardExpand();

            if(Runtime.UserInput.WasLeftShoulderPressed || Runtime.UserInput.WasLeftPressed)
                OnPrev();
            if(Runtime.UserInput.WasRightShoulderPressed || Runtime.UserInput.WasRightPressed)
                OnNext();
        }

        #region Save Card Picture

        private string errorLog;
        private CancellationTokenSource cts;

        public void OnCardPictureSave()
        {
            if (Runtime.Program.instance.ocgcore.showing
                || (Runtime.Program.instance.deckEditor.showing && Controllers.DeckEditor.condition == Controllers.DeckEditor.Condition.ChangeSide))
            {
                SaveShowingCard();
                return;
            }

            List<string> selections = new()
            {
                MDPro3.InterString.Get("保存选项"),
                string.Empty,
                MDPro3.InterString.Get("当前卡片卡图"),
                MDPro3.InterString.Get("当前卡组卡图"),
                MDPro3.InterString.Get("所有衍生物卡图"),
                MDPro3.InterString.Get("所有卡图"),
            };
            Runtime.UIManager.ShowPopupSelection(selections, CardPictureSaveOption);
        }

        private void CardPictureSaveOption()
        {
            string selected = UnityEngine.EventSystems.EventSystem.current.
                currentSelectedGameObject.GetComponent<SelectionButton>().GetButtonText();
            if (selected == MDPro3.InterString.Get("当前卡片卡图"))
                SaveShowingCard();
            else if (selected == MDPro3.InterString.Get("当前卡组卡图"))
                SaveDeckCards();
            else if (selected == MDPro3.InterString.Get("所有衍生物卡图"))
                SaveAllTokens();
            else if (selected == MDPro3.InterString.Get("所有卡图"))
                SaveAllCards();
        }

        private void SaveShowingCard()
        {
            var rawImage = ImageCard.RawImage;
            var texture = rawImage.texture;
            if (texture == null)
                texture = rawImage.material.mainTexture;

            if(texture is RenderTexture)
            {
                _ = SaveShowingCardAsyncIfVideo(card.Id);
                return;
            }

            if (SaveCardPicture(card.Id, (Texture2D)texture))
            {
                var fullPath = Runtime.Program.PATH_CARD_PIC + card.Id + Runtime.Program.EXPANSION_PNG;
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("卡图已保存于：[?]", fullPath));
            }
            else
            {
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("没有写入权限，无法保存。"));
            }
        }

        private async UniTask SaveShowingCardAsyncIfVideo(int code)
        {
            var result = await SaveCardAsync(code, default);
            if (result)
            {
                var fullPath = Runtime.Program.PATH_CARD_PIC + card.Id + Runtime.Program.EXPANSION_PNG;
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("卡图已保存于：[?]", fullPath));
            }
            else
            {
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("没有写入权限，无法保存。"));
            }
        }

        private void SaveDeckCards()
        {
            cts = new();
            _ = SaveCardsAsync(Runtime.Program.instance.deckEditor
                .GetUI<ServantUI.DeckEditorUI>().DeckView.GetAllCardCodes(), cts.Token);
        }

        private void SaveAllTokens()
        {
            var cards = MDPro3.Duel.YGOSharp.CardsManager.GetAllCards();
            var tokens = new List<int>();
            foreach (MDPro3.Duel.YGOSharp.Card card in cards)
                if (card.HasType(MDPro3.Duel.YGOSharp.CardType.Token))
                    tokens.Add(card.Id);

            cts = new();
            _ = SaveCardsAsync(tokens, cts.Token);
        }

        private void SaveAllCards()
        {
            cts = new();
            _ = SaveCardsAsync(MDPro3.Duel.YGOSharp.CardsManager.GetAllCardCodes(), cts.Token);
        }

        private static Texture2D CreateSaveTextureCopy(Texture2D source)
        {
            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.SetPixels(source.GetPixels());
            copy.Apply();
            return copy;
        }

        private bool SaveCardPicture(int code, Texture2D tex)
        {
            if (!Directory.Exists(Runtime.Program.PATH_CARD_PIC))
                Directory.CreateDirectory(Runtime.Program.PATH_CARD_PIC);

            Texture2D workingTexture = tex;
            bool destroyWorkingTexture = false;
            try
            {
                var size = MDPro3.Settings.Data.SavedCardSize;
                if (size.Length > 1 && size[0] > 0 && size[1] > 0)
                    if (size[0] != tex.width || size[1] != tex.height)
                    {
                        workingTexture = Presentation.TextureProcessor.ResizeTexture2D(CreateSaveTextureCopy(tex), size[0], size[1]);
                        destroyWorkingTexture = true;
                    }

                byte[] pic;
                string fullPath;
                var format = MDPro3.Settings.Data.SavedCardFormat.ToLower();
                if (format == Runtime.Program.EXPANSION_PNG)
                {
                    pic = workingTexture.EncodeToPNG();
                    fullPath = Runtime.Program.PATH_CARD_PIC + code + Runtime.Program.EXPANSION_PNG;
                }
                else
                {
                    pic = workingTexture.EncodeToJPG(85);
                    fullPath = Runtime.Program.PATH_CARD_PIC + code + Runtime.Program.EXPANSION_JPG;
                }

                File.WriteAllBytes(fullPath, pic);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (destroyWorkingTexture && workingTexture != null)
                    Destroy(workingTexture);
            }
        }

        private async UniTask SaveCardsAsync(List<int> cards, CancellationToken token)
        {
#if UNITY_EDITOR
            var time = Time.time;
#endif
            Popup.PopupProgress popupProgress = null;
            int errorCount = 0;
            errorLog = string.Empty;
            var errorLogPath = Runtime.Program.PATH_CARD_PIC + "MissingAndFailedCards.txt";
            if (!Directory.Exists(Runtime.Program.PATH_CARD_PIC))
                Directory.CreateDirectory(Runtime.Program.PATH_CARD_PIC);

            try
            {
                var pop = await UnityEngine.AddressableAssets.Addressables.InstantiateAsync("Popup/PopupProgress.prefab").WithCancellation(token);
                pop.transform.SetParent(Runtime.Program.instance.ui_.popup, false);
                popupProgress = pop.GetComponent<Popup.PopupProgress>();
                popupProgress.args = new List<string> { MDPro3.InterString.Get("卡图保存中") };
                popupProgress.cancelAction = StopSaving;
                popupProgress.text.text = string.Empty;
                popupProgress.progressBar.value = 0f;
                popupProgress.Show();
                await UniTask.WaitForSeconds(popupProgress.transitionTime, cancellationToken : token);

                if (File.Exists(errorLogPath))
                    File.Delete(errorLogPath);

                for (int i = 0; i < cards.Count; i++)
                {
                    token.ThrowIfCancellationRequested();

                    var result = await SaveCardAsync(cards[i], token);
                    if (!result)
                    {
                        errorCount++;
                        errorLog += cards[i].ToString() + Runtime.Program.STRING_LINE_BREAK;
                    }

                    popupProgress.text.text = (i + 1) + Runtime.Program.STRING_SLASH + cards.Count + Runtime.Program.STRING_LINE_BREAK + MDPro3.InterString.Get("错误：") + errorCount;
                    popupProgress.progressBar.value = cards.Count == 0 ? 1f : (float)(i + 1) / cards.Count;
                    if ((i + 1) % 8 == 0 || i == cards.Count - 1)
                        await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            finally
            {
                popupProgress.Hide();
                if (!string.IsNullOrEmpty(errorLog))
                    File.WriteAllText(errorLogPath, errorLog);
                if (cts != null && cts.Token == token)
                {
                    cts.Dispose();
                    cts = null;
                }
            }
#if UNITY_EDITOR
            Debug.Log($"Time Used: {Time.time - time}");
#endif
        }

        private async UniTask<bool> SaveCardAsync(int code, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var format = MDPro3.Settings.Data.SavedCardFormat;
            if (format != Runtime.Program.EXPANSION_PNG)
                format = Runtime.Program.EXPANSION_JPG;
            if (File.Exists(Runtime.Program.PATH_CARD_PIC + code + format))
                return true;

            Texture2D tex = null;
            try
            {
                tex = await Presentation.CardImageLoader.LoadCardAsync(code, false, token, true) as Texture2D;
                if (tex == null)
                    return false;

                if (!SaveCardPicture(code, tex)
                    || !Presentation.CardImageLoader.lastCardFoundArt
                    || !Presentation.CardImageLoader.lastCardRenderSucceed)
                    return false;
                return true;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                return false;
            }
            finally
            {
                var unknownCard = Runtime.TextureManager.container != null ? Runtime.TextureManager.container.unknownCard.texture : null;
                if (tex != null && tex != unknownCard)
                    Presentation.CardImageLoader.ReleaseCard(code, true);
            }
        }

        private void StopSaving()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
            if (!string.IsNullOrEmpty(errorLog))
                File.WriteAllText(Runtime.Program.PATH_CARD_PIC + "MissingAndFailedCards.txt", errorLog);
        }

        #endregion
    }
}
