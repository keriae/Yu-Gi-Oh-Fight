using MDPro3.Duel.YGOSharp;
using MDPro3.Utility;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3
{
    public abstract class CardBuilder : MonoBehaviour
    {
        [Header("Common")]
        [SerializeField] protected CardRenderer.CardStyle CardStyle = CardRenderer.CardStyle.OCG_TCG;
        [SerializeField] protected float CardNameRectMaxWidth = 544f;
        [SerializeField] protected GameObject cardCanvas;
        [SerializeField] protected GameObject CardNameCanvas;
        [SerializeField] protected TextMeshProUGUI tmpDescription;
        [SerializeField] protected TextMeshProUGUI tmpDescriptionPendulum;
        [SerializeField] protected TextMeshProUGUI tmpCardName;
        [SerializeField] protected TextMeshProUGUI tmpCardNameOutline;
        [SerializeField] protected TextMeshProUGUI tmpSpellTrapType;
        [SerializeField] protected TextMeshProUGUI tmpAttrRuby;
        [SerializeField] protected TextMeshProUGUI tmpAuther;

        [SerializeField] protected Image imageFrame;
        [SerializeField] protected RawImage imageOverframe;
        [SerializeField] protected RawImage illustOverframe;
        [SerializeField] protected RawImage frameOverframe;
        [SerializeField] protected Image imageAttr;
        [SerializeField] protected Text textLScale;
        [SerializeField] protected Text textRScale;
        [SerializeField] protected Text textPassword;

        protected string currentFontLanguage;

        protected virtual float[] FontSizeSimplifiedChinese => new float[] { 52f, 41f };
        protected virtual float[] FontSizeTraditionalChinese => new float[] { 57f, 41f };
        protected virtual float[] FontSizeKorean => new float[] { 52f, 41f };
        protected virtual float[] FontSizeJapanese => new float[] { 57f, 41f };
        protected virtual float[] FontSizeEnglish => new float[] { 65f, 44f };

        private RectTransform _tmpCardNameRT;
        protected RectTransform TmpCardNameRT =>
            _tmpCardNameRT = _tmpCardNameRT != null ? _tmpCardNameRT : tmpCardName.GetComponent<RectTransform>();
        private ContentSizeFitter _tmpCardNameCSF;
        protected ContentSizeFitter TmpCardNameCSF =>
            _tmpCardNameCSF = _tmpCardNameCSF != null ? _tmpCardNameCSF : tmpCardName.GetComponent<ContentSizeFitter>();
        private RectTransform _tmpCardNameOutlineRT;
        protected RectTransform TmpCardNameOutlineRT =>
            _tmpCardNameOutlineRT = _tmpCardNameOutlineRT != null ? _tmpCardNameOutlineRT : tmpCardNameOutline.GetComponent<RectTransform>();

        protected bool needOutline;
        protected bool fontMaterialInstantiated;

        protected virtual void SwitchLanguage(string language = null)
        {
            language ??= Language.GetCardConfig();

            if (currentFontLanguage == language)
                return;
            currentFontLanguage = language;

            if(language == Language.SimplifiedChinese)
            {
                tmpCardName.fontSize = FontSizeSimplifiedChinese[0];
                tmpCardNameOutline.fontSize = FontSizeSimplifiedChinese[0];
                tmpSpellTrapType.fontSize = FontSizeSimplifiedChinese[1];
                SetFont(CardRenderer.tmpFontChineseSimplified);
            }
            else if(language == Language.TraditionalChinese)
            {
                tmpCardName.fontSize = FontSizeTraditionalChinese[0];
                tmpCardNameOutline.fontSize = FontSizeTraditionalChinese[0];
                tmpSpellTrapType.fontSize = FontSizeTraditionalChinese[1];
                SetFont(CardRenderer.tmpFontChineseTraditional);
            }
            else if(language == Language.Korean)
            {
                tmpCardName.fontSize = FontSizeKorean[0];
                tmpCardNameOutline.fontSize = FontSizeKorean[0];
                tmpSpellTrapType.fontSize = FontSizeKorean[1];
                SetFont(CardRenderer.tmpFontKorean);
            }
            else if(language == Language.Japanese)
            {
                tmpCardName.fontSize = FontSizeJapanese[0];
                tmpCardNameOutline.fontSize = FontSizeJapanese[0];
                tmpSpellTrapType.fontSize = FontSizeJapanese[1];
                SetFont(CardRenderer.tmpFontJapanese);
            }
            else
            {
                tmpCardName.fontSize = FontSizeEnglish[0];
                tmpCardNameOutline.fontSize = FontSizeEnglish[0];
                tmpSpellTrapType.fontSize = FontSizeEnglish[1];
                SetFont(CardRenderer.tmpFontEnglish);
            }

            if (Language.UseLatin(language))
            {
                tmpCardName.fontStyle = FontStyles.SmallCaps;
                tmpCardNameOutline.fontStyle = FontStyles.SmallCaps;
            }
            else
            {
                tmpCardName.fontStyle = FontStyles.Normal;
                tmpCardNameOutline.fontStyle = FontStyles.Normal;
            }
        }

        protected virtual void SetFont(TMP_FontAsset tmpFont)
        {
            tmpDescription.font = tmpFont;
            tmpDescriptionPendulum.font = tmpFont;
            tmpCardName.font = tmpFont;
            tmpCardNameOutline.font = tmpFont;
            tmpSpellTrapType.font = tmpFont;
            tmpAttrRuby.font = tmpFont;
            tmpAuther.font = tmpFont;
        }

        public virtual void SetCardName(Card data, string language)
        {
            SwitchLanguage(language);

            tmpCardName.text = data.Name;
            tmpCardName.color = Color.white;
            TmpCardNameRT.localScale = Vector3.one;
            TmpCardNameCSF.SetLayoutHorizontal();
            var nameWidth = TmpCardNameRT.rect.width;
            if(nameWidth > CardNameRectMaxWidth)
                TmpCardNameRT.localScale = new Vector3(CardNameRectMaxWidth / nameWidth, 1f, 1f);
            cardCanvas.SetActive(false);
            SetCardNameOutline(false);
        }

        public virtual void SetCard(Card data, string language, Texture art, Texture2D overFrame = null)
        {
            SetCardName(data, language);
            tmpCardName.color = Color.black;
            cardCanvas.SetActive(true);

            SetAllIllustPartsOff();
            ResetParts();
            SetPassword(data);
            tmpSpellTrapType.color = Color.black;
            imageFrame.gameObject.SetActive(true);
            imageFrame.sprite = TextureManager.container.GetCardFrame(data, CardStyle);
            tmpAttrRuby.text = IDS.GetAttributeRubyText(data, language);

            ProcessOverframe(data, overFrame, art);
        }

        protected virtual void ProcessOverframe(Card data, Texture2D overFrame, Texture art)
        {
            if (overFrame == null || data == null)
                return;

            needOutline = true;
            imageOverframe.gameObject.SetActive(true);
            frameOverframe.gameObject.SetActive(true);

            if (overFrame.width == 512 && overFrame.height == 1024)
            {
                if(art.width == art.height && !data.HasType(CardType.Pendulum))
                {
                }
                else
                {
                    imageFrame.gameObject.SetActive(false);
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
            }
        }

        protected GameObject copiedCardName;

        protected virtual void SetCardNameOutline(bool on)
        {
            if (on)
            {

                tmpCardNameOutline.gameObject.SetActive(true);
                tmpCardNameOutline.color = tmpCardName.color == Color.white ? Color.black : Color.white;
                tmpCardNameOutline.text = tmpCardName.text;
                TmpCardNameOutlineRT.position = TmpCardNameRT.position;
                TmpCardNameOutlineRT.localScale = TmpCardNameRT.localScale;
                if(!fontMaterialInstantiated)
                    tmpCardNameOutline.fontMaterial = Instantiate(tmpCardNameOutline.fontMaterial);
                var mat = tmpCardNameOutline.fontMaterial;
                mat.SetColor("_OutlineColor", tmpCardName.color == Color.white ? Color.black : Color.white);
                mat.SetFloat("_OutlineWidth", 0.3f);
                mat.SetFloat("_OutlineSoftness", 0.1f);
            }
            else
            {
                tmpCardNameOutline.gameObject.SetActive(false);
            }
        }

        protected virtual void SetPassword(Card data)
        {
            if (Settings.Data.CardRenderPassword)
                textPassword.text = data.Id.ToString("D8");
            else
                textPassword.text = string.Empty;
        }

        protected Texture2D GetDescMask(bool isPendulum)
        {
            if (isPendulum)
                return TextureManager.container.rd_CardDescColumnMask;
            else
                return TextureManager.container.CardDescColumnMask;
        }

        public virtual RawImage GetArtPartForVideo(bool isPendulum)
        {
            return null;
        }

        public virtual void SetAllIllustPartsOff()
        {
            illustOverframe.gameObject.SetActive(false);
        }

        protected virtual void ResetParts()
        {
            SetAllIllustPartsOff();
            tmpCardName.color = Color.black;
            tmpDescription.text = string.Empty;
            tmpDescriptionPendulum.text = string.Empty;
            tmpSpellTrapType.text = string.Empty;
            tmpAttrRuby.text = string.Empty;
            tmpAuther.text = string.Empty;
            tmpAuther.color = Color.black;
            textPassword.text = string.Empty;
            textPassword.color = Color.black;
            imageFrame.gameObject.SetActive(true);
            imageOverframe.gameObject.SetActive(false);
            textLScale.text = string.Empty;
            textRScale.text = string.Empty;
            frameOverframe.gameObject.SetActive(false);
            needOutline = false;
        }

        #region Text Process

        private const string BIG_SLASH = "／";
        private const string SMALL_SLASH = " / ";

        protected static string TextForRender(string description, Card data)
        {
            if (string.IsNullOrEmpty(description))
                return string.Empty;

            if (data.IsRushDuelCard())
                description = data.GetRushDescriptionForRender(description);

            var language = data.isPre ? Language.GetPrereleaseConfig() : Language.GetCardConfig();
            description = description.Replace("\r", string.Empty);

            if (!data.IsRushDuelCard())
            {
                description = description.Replace("\t\n", "\f\f");
                description = description.Replace("\n●", "●●●");
                description = description.Replace("\n", string.Empty);
                description = description.Replace("\f\f", Program.STRING_LINE_BREAK);
                description = description.Replace("●●●", $"{Program.STRING_LINE_BREAK}●");
            }

            if (!Language.UseLatin(language))
                description = description.Replace(Program.STRING_SLASH, BIG_SLASH);
            else
                description = description.Replace(Program.STRING_SLASH, SMALL_SLASH);

            //if (!Language.UseLatin(language))
            //    description = description.Replace(" ", "\u00A0");
            description = description.Replace($"{Program.STRING_LINE_BREAK}{Program.STRING_LINE_BREAK}", Program.STRING_LINE_BREAK);
            return description;
        }

        protected static List<string> GetAuthorFromDescription(string description)
        {
            var lines = description.Replace("\r", string.Empty).Split("\n");
            var returnValue = new List<string>();

            StringBuilder beforeDiySymbol = new();
            bool foundDIY = false;

            foreach (var line in lines)
            {
                if (!foundDIY && line.StartsWith(Settings.Data.DiySymbol))
                {
                    var beforeDiySymbolText = beforeDiySymbol.ToString();
                    returnValue.Add(beforeDiySymbolText);
                    returnValue.Add(line);
                    foundDIY = true;
                }
                else if (!foundDIY && !string.IsNullOrEmpty(line))
                {
                    beforeDiySymbol.Append(line);
                }

                if (foundDIY)
                    break;
            }

            if (!foundDIY)
            {
                returnValue.Add(description);
                returnValue.Add(string.Empty);
            }

            return returnValue;
        }

        #endregion

    }
}
