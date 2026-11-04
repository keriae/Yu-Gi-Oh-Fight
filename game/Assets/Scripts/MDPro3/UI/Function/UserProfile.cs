using MDPro3.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YgomSystem.ElementSystem;

namespace MDPro3.UI
{
    public class UserProfile : MonoBehaviour
    {
        #region Elements

        private ElementObjectManager m_Manager;
        private ElementObjectManager Manager =>
            m_Manager = m_Manager != null ? m_Manager 
            : GetComponent<ElementObjectManager>();

        #region Top

        private const string LABEL_IMG_ICONBG = "IconBG";
        private Image m_IconBG;
        private Image IconBG =>
            m_IconBG = m_IconBG != null ? m_IconBG
            : Manager.GetElement<Image>(LABEL_IMG_ICONBG);

        private const string LABEL_IMG_ICONRANK = "IconRank";
        private Image m_IconRank;
        private Image IconRank =>
            m_IconRank = m_IconRank != null ? m_IconRank
            : Manager.GetElement<Image>(LABEL_IMG_ICONRANK);

        private const string LABEL_IMG_ICONTIER1 = "IconTier1";
        private Image m_IconTier1;
        private Image IconTier1 =>
            m_IconTier1 = m_IconTier1 != null ? m_IconTier1
            : Manager.GetElement<Image>(LABEL_IMG_ICONTIER1);

        private const string LABEL_IMG_ICONTIER2 = "IconTier2";
        private Image m_IconTier2;
        private Image IconTier2 =>
            m_IconTier2 = m_IconTier2 != null ? m_IconTier2
            : Manager.GetElement<Image>(LABEL_IMG_ICONTIER2);

        private const string LABEL_IMG_ICONTIER3 = "IconTier3";
        private Image m_IconTier3;
        private Image IconTier3 =>
            m_IconTier3 = m_IconTier3 != null ? m_IconTier3
            : Manager.GetElement<Image>(LABEL_IMG_ICONTIER3);

        #endregion

        #region Profile

        private const string LABEL_RIMG_AVATAR = "RawImageAvatar";
        private RawImage m_Avatar;
        public RawImage Avatar =>
            m_Avatar = m_Avatar != null ? m_Avatar
            : Manager.GetElement<RawImage>(LABEL_RIMG_AVATAR);

        private const string LABEL_TXT_USERNAME = "TextUserName";
        private TextMeshProUGUI m_TextUserName;
        private TextMeshProUGUI TextUserName =>
            m_TextUserName = m_TextUserName != null ? m_TextUserName
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_USERNAME);

        private const string LABEL_TXT_EXPVALUE = "TextExpValue";
        private TextMeshProUGUI m_TextExpValue;
        private TextMeshProUGUI TextExpValue =>
            m_TextExpValue = m_TextExpValue != null ? m_TextExpValue
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_EXPVALUE);

        private const string LABEL_TXT_DPVALUE = "TextDPValue";
        private TextMeshProUGUI m_TextDPValue;
        private TextMeshProUGUI TextDPValue =>
            m_TextDPValue = m_TextDPValue != null ? m_TextDPValue
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_DPVALUE);

        private const string LABEL_TXT_ATHLETICWINVALUE = "TextAthleticWinValue";
        private TextMeshProUGUI m_TextAthleticWinValue;
        private TextMeshProUGUI TextAthleticWinValue =>
            m_TextAthleticWinValue = m_TextAthleticWinValue != null ? m_TextAthleticWinValue
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_ATHLETICWINVALUE);

        private const string LABEL_TXT_AthleticWinRatioVALUE = "TextAthleticWinRatioValue";
        private TextMeshProUGUI m_TextAthleticWinRatioValue;
        private TextMeshProUGUI TextAthleticWinRatioValue =>
            m_TextAthleticWinRatioValue = m_TextAthleticWinRatioValue != null ? m_TextAthleticWinRatioValue
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_AthleticWinRatioVALUE);

        private const string LABEL_TXT_ATHLETICRANKVALUE = "TextAthleticRankValue";
        private TextMeshProUGUI m_TextAthleticRankValue;
        private TextMeshProUGUI TextAthleticRankValue =>
            m_TextAthleticRankValue = m_TextAthleticRankValue != null ? m_TextAthleticRankValue
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_ATHLETICRANKVALUE);

        private const string LABEL_TXT_ENTERTAINCOUNTVALUE = "TextEntertainCountValue";
        private TextMeshProUGUI m_TextEntertainCountValue;
        private TextMeshProUGUI TextEntertainCountValue =>
            m_TextEntertainCountValue = m_TextEntertainCountValue != null ? m_TextEntertainCountValue
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_ENTERTAINCOUNTVALUE);

        private const string LABEL_TXT_ENTERTAINRANKVALUE = "TextEntertainRankValue";
        private TextMeshProUGUI m_TextEntertainRankValue;
        private TextMeshProUGUI TextEntertainRankValue =>
            m_TextEntertainRankValue = m_TextEntertainRankValue != null ? m_TextEntertainRankValue
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_ENTERTAINRANKVALUE);

        #endregion

        #endregion

        public void SetProfile(MyCardUserExp data)
        {
            if (data == null)
                return;

            if(MyCard.account != null)
                TextUserName.text = MyCard.account.user.name;
            TextExpValue.text = data.exp.ToString();
            TextDPValue.text = data.pt.ToString();
            TextAthleticWinValue.text = data.athletic_win.ToString();
            TextAthleticWinRatioValue.text = data.athletic_wl_ratio + "%";
            TextAthleticRankValue.text = data.arena_rank.ToString();
            TextEntertainCountValue.text = data.entertain_all.ToString();
            TextEntertainRankValue.text = data.exp_rank.ToString();

            var rankSprites = TextureManager.container.GetRankSprites(data.pt);
            IconBG.sprite = rankSprites[0];
            IconRank.sprite = rankSprites[1];
            IconTier1.sprite = rankSprites[2];
            IconTier2.sprite = rankSprites[3];
            IconTier3.sprite = rankSprites[4];
        }
    }
}
