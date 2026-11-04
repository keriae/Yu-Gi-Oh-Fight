using Cysharp.Threading.Tasks;
using MDPro3.Servant;
using MDPro3.Utility;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using YgomSystem.ElementSystem;

namespace MDPro3.UI
{
    public class AppearanceDetail : MonoBehaviour
    {
        #region Elements

        private ElementObjectManager m_Manager;
        private ElementObjectManager Manager =>
            m_Manager = m_Manager != null ? m_Manager
            : GetComponent<ElementObjectManager>();

        private CanvasGroup m_CG;
        private CanvasGroup CG =>
             m_CG = m_CG != null ? m_CG
            : GetComponent<CanvasGroup>();

        private const string LABEL_TXT_SETTING = "TextDetailSetting";
        private TextMeshProUGUI m_TextSetting;
        private TextMeshProUGUI TextSetting =>
            m_TextSetting = m_TextSetting != null ? m_TextSetting
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_SETTING);

        private const string LABEL_TXT_DESCRIPTION = "TextDetailDescription";
        private TextMeshProUGUI m_TextDescription;
        private TextMeshProUGUI TextDescription =>
            m_TextDescription = m_TextDescription != null ? m_TextDescription
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_DESCRIPTION);

        private const string LABEL_IMG = "Image";
        private Image m_Image;
        private Image Image =>
            m_Image = m_Image != null ? m_Image
            : Manager.GetElement<Image>(LABEL_IMG);

        private const string LABEL_RIMG = "RawImage";
        private RawImage m_RawImage;
        private RawImage RawImage =>
            m_RawImage = m_RawImage != null ? m_RawImage
            : Manager.GetElement<RawImage>(LABEL_RIMG);

        private const string LABEL_IMG_WALLPAPER_BG = "WallpaperBG";
        private Image m_WallpaperBG;
        private Image WallpaperBG =>
            m_WallpaperBG = m_WallpaperBG != null ? m_WallpaperBG
            : Manager.GetElement<Image>(LABEL_IMG_WALLPAPER_BG);

        #endregion

        private CancellationTokenSource cts;
        private int currentCode = int.MinValue;
        private bool currentIsCard;
        private string currentPath = string.Empty;

        public void Show()
        {
            CG.alpha = 1;
            CG.interactable = true;
            CG.blocksRaycasts = true;
        }

        public void Hide()
        {
            CG.alpha = 0;
            CG.interactable = false;
            CG.blocksRaycasts = false;
        }

        public void SetItem(int code, string itemName, string desc, bool isCard, string path = null, Sprite preloadedSprite = null, Material preloadedMaterial = null)
        {
            TextSetting.text = itemName;
#if UNITY_EDITOR
            TextSetting.text = $"{itemName} ({code})";
#endif
            TextDescription.text = desc;
            currentCode = code;
            currentIsCard = isCard;
            currentPath = path ?? string.Empty;

            CancelLoading();
            ResetPreview();
            ApplyLoadedPreview(code, preloadedSprite, preloadedMaterial);
            cts = new CancellationTokenSource();
            _ = SetIconAsync(code, cts.Token, isCard);
        }

        public void ApplyLoadedPreview(int code, Sprite sprite = null, Material material = null)
        {
            if (code != currentCode)
                return;

            if (currentIsCard)
            {
                if (sprite == null)
                    return;

                ResetPreview();
                Image.sprite = sprite;
                Image.gameObject.SetActive(true);
                return;
            }

            if (currentPath.StartsWith("Protector"))
            {
                if (material == null)
                    return;

                ResetPreview();
                RawImage.material = material;
                RawImage.gameObject.SetActive(true);
                return;
            }

            if (currentPath.StartsWith("ProfileFrame") || sprite == null)
                return;

            ResetPreview();
            Image.sprite = sprite;
            Image.gameObject.SetActive(true);
            WallpaperBG.gameObject.SetActive(currentPath.StartsWith("WallPaperIcon"));
        }

        private async UniTask SetIconAsync(int code, CancellationToken token, bool isCard)
        {
            var codeString = code.ToString();

            ResetPreview();

            if (isCard) //Cross Duel Mate
            {
                var art = await CardImageLoader.LoadArtAsync(code, true, token);
                if (art != null)
                {
                    Image.sprite = TextureManager.Texture2Sprite(art);
                    Image.gameObject.SetActive(true);
                }
            }
            else
            {
                if (codeString.StartsWith("107")) // Protector
                {
                    RawImage.material = await ABLoader.LoadProtectorMaterial(code.ToString(), token);
                    if (RawImage.material != null)
                    {
                        RawImage.material.renderQueue = 3000;
                        RawImage.gameObject.SetActive(true);
                    }
                }
                else
                {
                    var address = Items.GetIconAddress(codeString, DeviceInfo.OnMobile() ? 1 : 2);
                    var load = Addressables.LoadAssetAsync<Sprite>(address);
                    while (!load.IsDone)
                        await TaskUtility.WaitOneFrame(gameObject, token);

                    if (load.Result != null)
                    {
                        if (codeString.StartsWith("103")) // Profile Frame
                        {
                            Image.material = await ABLoader.LoadFrameMaterial(codeString);
                            Image.material.SetTexture("_ProfileFrameTex", load.Result.texture);

                            var config = $"{Appearance.condition}Face{Appearance.player}";
                            var configProfileIcon = Config.Get(config, Program.items.faces[0].id.ToString());
                            var address2 = Items.GetIconAddress(configProfileIcon, DeviceInfo.OnMobile() ? 1 : 2);
                            var load2 = Addressables.LoadAssetAsync<Sprite>(address2);
                            while (!load2.IsDone)
                                await TaskUtility.WaitOneFrame(gameObject, token);
                            if (load2.Result != null)
                                Image.sprite = load2.Result;
                            else
                                Image.sprite = TextureManager.container.black;
                        }

                        if (!codeString.StartsWith("103")) // Profile Frame
                            Image.sprite = load.Result;

                        Image.gameObject.SetActive(true);
                        WallpaperBG.gameObject.SetActive(codeString.StartsWith("113"));
                    }
                }
            }
        }

        private void ResetPreview()
        {
            Image.gameObject.SetActive(false);
            Image.material = null;
            RawImage.gameObject.SetActive(false);
            RawImage.material = null;
            WallpaperBG.gameObject.SetActive(false);
        }

        private void CancelLoading()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }
    }
}
