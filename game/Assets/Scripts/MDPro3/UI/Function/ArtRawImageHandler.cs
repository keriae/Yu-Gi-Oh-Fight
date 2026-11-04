using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using MDPro3.Utility;
using System.Threading.Tasks;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MDPro3.UI
{
    [RequireComponent(typeof(RawImage))]
    public class ArtRawImageHandler : MonoBehaviour
    {
        #region 字段和属性

        public bool cache = true;

        private int code = 0;
        private int loadedCode = 0;
        private CancellationTokenSource cts;
        private RawImage m_RawImage;
        private RawImage RawImage =>
            m_RawImage = m_RawImage != null ? m_RawImage
            : GetComponent<RawImage>();

        #endregion

        private void OnDestroy()
        {
            CancelLoad();
            ReleaseArt();
        }

        private void CancelLoad()
        {
            try
            {
                cts?.Cancel();
                cts?.Dispose();
            }
            finally
            {
                cts = null;
            }
        }

        private void ReleaseArt()
        {
            if(loadedCode != 0)
            {
                CardImageLoader.ReleaseArt(loadedCode);
                loadedCode = 0;
            }
        }

        public void SetArt(int art)
        {
            if (code == art)
                return;

            ReleaseArt();
            code = art;

            _ = LoadArtsAsync();
        }

        private async UniTask LoadArtsAsync()
        {
            CancelLoad();
            RawImage.texture = TextureManager.container.unknownArt.texture;
            if (code == 0)
                return;

            cts = new CancellationTokenSource();
            var art = await CardImageLoader.LoadArtAsync(code, cache, cts.Token);
            if(art != null)
            {
                RawImage.texture = art;
                loadedCode = code;
            }
        }
    }
}
