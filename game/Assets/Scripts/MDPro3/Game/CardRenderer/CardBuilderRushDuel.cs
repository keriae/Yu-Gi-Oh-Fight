using MDPro3.Duel.YGOSharp;
using MDPro3.IO;
using MDPro3.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3
{
    public class CardBuilderRushDuel : CardBuilder
    {
        [Header("Rush Duel")]
        [SerializeField] private RawImage illust;
        [SerializeField] private RawImage illustPendulum;
        [SerializeField] private RawImage illustPendulumWide;

        [SerializeField] private GameObject legend;
        [SerializeField] private RectTransform moveParts;
        [SerializeField] private GameObject maxAtk;
        [SerializeField] private TextMeshProUGUI numMaxAtk;
        [SerializeField] private GameObject atk;
        [SerializeField] private TextMeshProUGUI numAtk;
        [SerializeField] private GameObject def;
        [SerializeField] private TextMeshProUGUI numDef;
        [SerializeField] private GameObject level;
        [SerializeField] private TextMeshProUGUI numLevel;
        [SerializeField] private GameObject rank;
        [SerializeField] private TextMeshProUGUI numRank;
        [SerializeField] private GameObject link;
        [SerializeField] private float moveDistance;
        [SerializeField] private Vector2 cardNamePosition;
        [SerializeField] private Vector2 cardNameOcgPosition;

        protected override float[] FontSizeSimplifiedChinese => new float[] { 52f, 28f };
        protected override float[] FontSizeTraditionalChinese => new float[] { 57f, 29f };
        protected override float[] FontSizeKorean => new float[] { 52f, 28f };
        protected override float[] FontSizeJapanese => new float[] { 57f, 30f };
        protected override float[] FontSizeEnglish => new float[] { 65f, 31f };

        private const float MOVE_PARTS_OFFSET_X = -3f;

        public override void SetAllIllustPartsOff()
        {
            base.SetAllIllustPartsOff();
            illust.gameObject.SetActive(false);
            illustPendulum.gameObject.SetActive(false);
            illustPendulumWide.gameObject.SetActive(false);
        }

        protected override void ResetParts()
        {
            base.ResetParts();
            textPassword.color = Color.white;
            tmpAuther.color = Color.white;
            legend.SetActive(false);
            moveParts.gameObject.SetActive(false);
            moveParts.anchoredPosition = new Vector2(MOVE_PARTS_OFFSET_X, 0f);
            maxAtk.SetActive(false);
            numMaxAtk.text = string.Empty;            
            def.SetActive(false);
            numDef.text = string.Empty;
            level.SetActive(false);
            numLevel.text = string.Empty;
            rank.SetActive(false);
            numRank.text = string.Empty;
            link.SetActive(false);
            MoveCardNameRectTo(true);
        }

        protected override void SetPassword(Card data)
        {
            if (Settings.Data.CardRenderPassword)
                textPassword.text = data.GetRushDuelPasswordForRender();
            else
                textPassword.text = string.Empty;
        }        

        protected override void ProcessOverframe(Card data, Texture2D overFrame, Texture art)
        {
            if (overFrame == null || data == null)
                return;

            needOutline = true;
            imageOverframe.gameObject.SetActive(true);
            frameOverframe.gameObject.SetActive(true);

            if (overFrame.width == 512 && overFrame.height == 1024)
            {
                if (art.width == art.height && !data.HasType(CardType.Pendulum))
                {
                }
                else
                {
                    illustOverframe.gameObject.SetActive(true);
                    illustOverframe.texture = overFrame;
                }

                overFrame = TextureProcessor.InvertAlpha(overFrame);
                var descMask = TextureManager.container.GetDescMask(CardStyle, data.HasType(CardType.Pendulum), data.Id, overFrame);
                var maskedOF = TextureProcessor.ApplyMaskToAlpha(overFrame, descMask, invertMask: true);
#if UNITY_EDITOR
                maskedOF.alphaIsTransparency = true;
#endif
                imageOverframe.texture = maskedOF;
            }
            else
            {
                var descMask = TextureManager.container.GetDescMask(CardStyle, data.HasType(CardType.Pendulum), data.Id, overFrame);
                var maskedOF = TextureProcessor.ApplyMaskToAlpha(overFrame, descMask, invertMask: true);
#if UNITY_EDITOR
                maskedOF.alphaIsTransparency = true;
#endif
                imageOverframe.texture = maskedOF;

                if (CardRenderer.OverFrameIsOpaque(data.Id, overFrame))
                    MoveCardNameRectTo(false);
            }
        }

        public override void SetCardName(Card data, string language)
        {
            base.SetCardName(data, language);
            if(ResourceManager.CardHasOverFrame(data.Id))
                MoveCardNameRectTo(!CardRenderer.OverFrameIsOpaque(data.Id, null));
        }

        public override void SetCard(Card data, string language, Texture art, Texture2D overFrame = null)
        {
            base.SetCard(data, language, art, overFrame);

            numAtk.text = data.GetAttackString();
            numDef.text = data.GetDefenseString();
            def.SetActive(!data.HasType(CardType.Link));
            imageAttr.sprite = TextureManager.container.GetCardAttributeIcon(data, true);
            tmpSpellTrapType.text = data.GetTypeForRushDuelRender();

            if (data.HasType(CardType.Monster))
                moveParts.gameObject.SetActive(true);

            if (data.HasType(CardType.Pendulum))
            {
                moveParts.anchoredPosition = new Vector2(MOVE_PARTS_OFFSET_X, moveDistance);

                if (art.width == art.height)
                {
                    illust.gameObject.SetActive(true);
                    illust.texture = art;
                }
                else if (art.width > art.height)
                {
                    illustPendulumWide.gameObject.SetActive(true);
                    illustPendulumWide.texture = art;
                }
                else
                {
                    illustPendulum.gameObject.SetActive(true);
                    illustPendulum.texture = art;
                }
                tmpDescriptionPendulum.text = TextForRender(data.GetPendulumDescription(true), data);

                var authorSplit = GetAuthorFromDescription(data.GetMonsterDescription(true));
                tmpAuther.text = authorSplit[1];
                tmpDescription.text = TextForRender(authorSplit[0], data);

                textLScale.text = data.LScale.ToString();
                textRScale.text = data.RScale.ToString();
            }
            else
            {
                illust.gameObject.SetActive(true);
                illust.texture = art;
                var authorSplit = GetAuthorFromDescription(data.Desc);
                tmpDescription.text = TextForRender(authorSplit[0], data);
                tmpAuther.text = authorSplit[1];
            }

            if (data.IsLevelZeroMonster())
                data.Level = 0;
            if (data.HasType(CardType.Link))
            {
                tmpCardName.color = Color.white;
                numDef.text = string.Empty;
                numLevel.text = data.GetLinkCount().ToString();

                link.SetActive(true);
                for (int i = 0; i < 8; i++)
                {
                    int bitIndex = i < 4 ? i : i + 1;
                    link.transform.GetChild(i).gameObject.SetActive((data.LinkMarker & (1 << bitIndex)) != 0);
                }
            }
            else if (data.HasType(CardType.Xyz))
            {
                tmpCardName.color = Color.white;
                if (!data.HasType(CardType.Pendulum))
                    tmpSpellTrapType.color = Color.white;
                rank.SetActive(true);
                numRank.text = data.Level.ToString();
            }
            else if (data.HasType(CardType.Monster))
            {
                level.SetActive(true);
                numLevel.text = data.Level.ToString();
            }

            legend.SetActive(data.HasType(CardType.Legend));

            if (data.IsMaximumCard(true))
            {
                maxAtk.SetActive(true);
                numMaxAtk.text = data.GetMaximumAttackString(true);
            }

            if (needOutline)
                SetCardNameOutline(true);
        }

        public override RawImage GetArtPartForVideo(bool isPendulum)
        {
            return isPendulum ? illustPendulum : illust;
        }

        private void MoveCardNameRectTo(bool original)
        {
            TmpCardNameRT.anchoredPosition = original ? cardNamePosition : cardNameOcgPosition;
        }
    }
}
