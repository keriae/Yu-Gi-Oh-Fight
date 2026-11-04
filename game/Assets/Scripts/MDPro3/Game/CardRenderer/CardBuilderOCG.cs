using MDPro3.Duel.YGOSharp;
using MDPro3.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3
{
    public class CardBuilderOCG : CardBuilder
    {
        [Header("OCG")]

        [SerializeField] private RawImage illust;
        [SerializeField] private RawImage illustPendulum;
        [SerializeField] private RawImage illustPendulumSquare;
        [SerializeField] private RawImage illustPendulumWide;
        [SerializeField] private TextMeshProUGUI tmpMonsterType;

        [SerializeField] private GameObject levels;
        [SerializeField] private GameObject ranks;
        [SerializeField] private GameObject linkMarkers;
        [SerializeField] private GameObject noneEffectArea;
        [SerializeField] private GameObject atk;
        [SerializeField] private GameObject def;
        [SerializeField] private Text numAtk;
        [SerializeField] private Text numDef;
        [SerializeField] private Image imageLinkCount;

        protected override void SwitchLanguage(string language = null)
        {
            base.SwitchLanguage(language);

            if (Language.UseLatin(language))
            {
                tmpMonsterType.fontSize = 28f;
                tmpMonsterType.transform.localScale = new Vector3(1.2f, 1f, 1f);
                tmpMonsterType.fontStyle = (FontStyles)((int)FontStyles.Bold + (int)FontStyles.SmallCaps);
            }
            else
            {
                tmpMonsterType.fontSize = 26f;
                tmpMonsterType.transform.localScale = Vector3.one;
                tmpMonsterType.fontStyle = FontStyles.Normal;
            }
        }

        protected override void SetFont(TMP_FontAsset tmpFont)
        {
            base.SetFont(tmpFont);
            tmpMonsterType.font = tmpFont;
        }

        protected override void ResetParts()
        {
            base.ResetParts();

            tmpMonsterType.gameObject.SetActive(false);
            levels.SetActive(false);
            ranks.SetActive(false);
            linkMarkers.SetActive(false);
            noneEffectArea.SetActive(false);
            def.SetActive(false);
            imageLinkCount.gameObject.SetActive(false);
        }

        public override void SetAllIllustPartsOff()
        {
            base.SetAllIllustPartsOff();
            illust.gameObject.SetActive(false);
            illustPendulum.gameObject.SetActive(false);
            illustPendulumSquare.gameObject.SetActive(false);
            illustPendulumWide.gameObject.SetActive(false);
        }

        public override RawImage GetArtPartForVideo(bool isPendulum)
        {
            return isPendulum ? illustPendulum : illust;
        }

        public override void SetCard(Card data, string language, Texture art, Texture2D overFrame = null)
        {
            base.SetCard(data, language, art, overFrame);

            imageAttr.sprite = TextureManager.container.GetCardAttributeIcon(data, true);
            def.SetActive(!data.HasType(CardType.Link));
            numAtk.text = data.GetAttackString();
            numDef.text = data.GetDefenseString();

            if (data.HasType(CardType.Pendulum))
            {
                if (art.width == art.height)
                {
                    illustPendulumSquare.gameObject.SetActive(true);
                    illustPendulumSquare.texture = art;
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

                var pendulumDesc = data.GetDescriptionSplit(true);
                tmpDescriptionPendulum.text = TextForRender(pendulumDesc[0], data);

                var authorSplit = GetAuthorFromDescription(pendulumDesc[1]);
                tmpDescription.text = TextForRender(authorSplit[0], data);
                tmpAuther.text = authorSplit[1];

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

            if (data.HasType(CardType.Monster))
            {
                tmpMonsterType.gameObject.SetActive(true);
                tmpMonsterType.text = data.GetTypeForRushDuelRender();
                noneEffectArea.SetActive(true);

                if (data.IsLevelZeroMonster())
                    data.Level = 0;
                if (data.HasType(CardType.Link))
                {
                    tmpCardName.color = Color.white;
                    linkMarkers.SetActive(true);
                    imageLinkCount.gameObject.SetActive(true);
                    imageLinkCount.sprite = TextureManager.container.GetOcgLinkCount(data.GetLinkCount());
                    for (int i = 0; i < 8; i++)
                    {
                        int bitIndex = i < 4 ? i : i + 1;
                        linkMarkers.transform.GetChild(i).gameObject.SetActive((data.LinkMarker & (1 << bitIndex)) != 0);
                    }
                }
                else if (data.HasType(CardType.Xyz))
                {
                    tmpCardName.color = Color.white;
                    if (!data.HasType(CardType.Pendulum))
                    {
                        textPassword.color = Color.white;
                        tmpAuther.color = Color.white;
                    }
                    ranks.SetActive(true);
                    for (int i = 0; i < 13; i++)
                        ranks.transform.GetChild(i).gameObject.SetActive(data.Level > i);
                    ranks.GetComponent<HorizontalLayoutGroup>().padding = data.Level > 12 ?
                        new RectOffset(0, 0, 0, 0)
                        : new RectOffset(25, 0, 0, 0);
                }
                else
                {
                    levels.SetActive(true);
                    for (int i = 0; i < 12; i++)
                        levels.transform.GetChild(i).gameObject.SetActive(data.Level > i);
                }
            }
            else
            {
                tmpCardName.color = Color.white;
                tmpSpellTrapType.text = data.GetSpellTypeForOCGRender();
            }

            if (needOutline)
                SetCardNameOutline(true);
        }

    }
}