using DG.Tweening;
using MDPro3.Duel.YGOSharp;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MDPro3.UI
{
    public class CardExpand : UIWidgetFullScreen
    {
        #region Elements

        private const string LABEL_RIMG_CARD = "ImageCard";
        private CardRawImageHandler m_ImageCard;
        protected CardRawImageHandler ImageCard =>
            m_ImageCard = m_ImageCard != null ? m_ImageCard
            : Manager.GetElement<CardRawImageHandler>(LABEL_RIMG_CARD);
        private RectTransform m_CardRect;
        protected RectTransform CardRect =>
            m_CardRect = m_CardRect != null ? m_CardRect
            : Manager.GetElement<RectTransform>(LABEL_RIMG_CARD);

        private const string LABEL_TXT_Shortcut = "TextShortcut";
        private TextMeshProUGUI m_TextShortcut;
        protected TextMeshProUGUI TextShortcut =>
            m_TextShortcut = m_TextShortcut != null ? m_TextShortcut
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_Shortcut);

        #endregion

        private bool expanded;
        private bool isRushDuelCard;
        private bool isPendulumCard;
        private bool shifting;

        protected override string Label_SE_Show => "SE_CARDEXPAND_DISPLAY";
        protected override string Label_SE_Hide => "SE_CARDEXPAND_CLOSE";

        protected override void Awake()
        {
            ImageCard.GetComponent<Button>()
                .onClick.AddListener(Zoom);
            BG.GetComponent<SelectionButton>()
                .SetClickEvent(Hide);
            TextShortcut.text = InterString.Get("扩大");
        }

        protected override void Update()
        {
            if (!NeedResponse()) return;
            if (UserInput.WasCancelPressed || UserInput.MouseRightDown)
            {
                if (expanded)
                    ZoomOut();
                else
                    Hide();
            }
            if (UserInput.WasLeftTriggerPressed)
                Zoom();
        }

        protected override bool NeedResponse()
        {
            if(shifting) return false;
            return base.NeedResponse();
        }

        public void Show(Card data)
        {
            AudioManager.PlaySE("SE_CARDEXPAND_DISPLAY");
            shifting = true;

            ImageCard.SetCard(data);
            isRushDuelCard = CardRenderer.NeedRushDuelStyle(data.Id);
            isPendulumCard = data.HasType(CardType.Pendulum);

            Show();
        }

        public override void Show()
        {
            if (showing) return;
            showing = true;
            ShowEvent();

            BG.alpha = 0.3f;
            BG.DOFade(1f, 0.1f);

            ImageCard.RawImage.color = new Color(1f, 1f, 1f, 0.3f);
            ImageCard.RawImage.DOFade(1f, 0.1f);
            ImageCard.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
            ImageCard.transform.DOScale(new Vector3(1.5f, 1.5f, 1f), 0.25f).SetEase(Ease.OutQuart);
            ImageCard.transform.localEulerAngles = new Vector3(8f, 12f, 2f);
            ImageCard.transform.DOLocalRotate(Vector3.zero, 0.35f).SetEase(Ease.OutQuart)
                .OnComplete(() => { shifting = false; });
            EventSystem.current.SetSelectedGameObject(ImageCard.gameObject);
        }

        protected override void AfterShowEvent()
        {
            Select(true);
        }

        public override void Hide()
        {
            if (shifting) return;
            shifting = true;
            HideEvent();

            BG.DOFade(0f, 0.21f).OnComplete(() =>
            {
                Destroy(gameObject);
                AfterHideEvent();
            });

            DOTween.Sequence()
                .AppendInterval(0.05f)
                .Append(ImageCard.RawImage.DOFade(0f, 0.15f));
            ImageCard.transform.DOScale(new Vector3(1.05f, 1.05f, 1f), 0.2f).SetEase(Ease.InCubic);
            ImageCard.transform.DOLocalRotate(new Vector3(-10f, -16f, -3f), 0.2f).SetEase(Ease.InCubic);
        }

        private void Zoom()
        {
            if (expanded)
                ZoomOut();
            else
                ZoomIn();
        }

        private float GetZoomPositionY()
        {
            if (isRushDuelCard)
            {
                if (isPendulumCard)
                    return -320f;
                else
                    return -150f;
            }
            else
            {
                if (isPendulumCard)
                    return -213f;
                else
                    return -103f;
            }
        }

        private float GetZoomScale()
        {
            if (isRushDuelCard)
            {
                if(isPendulumCard)
                    return 3.15f;
                else
                    return 2.5f;
            }
            else
            {
                if (isPendulumCard)
                    return 3.3f;
                else
                    return 2.82f;
            }
        }

        private void ZoomIn()
        {
            if (shifting) return;
            shifting = true;
            AudioManager.PlaySE("SE_CARDEXPAND_ZOOMIN");
            expanded = true;

            CardRect.DOScale(new Vector3(GetZoomScale(), GetZoomScale(), 1f), 0.2f).SetEase(Ease.OutCubic);
            CardRect.DOLocalMove(new Vector3(0f, GetZoomPositionY(), -100f), 0.2f).SetEase(Ease.OutCubic);
            CardRect.DORotate(new Vector3(4f, 6f, 1f), 0.15f).OnComplete(() =>
            {
                CardRect.DORotate(Vector3.zero, 0.2f).OnComplete(() => shifting = false); // TODO: tween in tween
            });

            TextShortcut.text = InterString.Get("缩小");
        }

        private void ZoomOut()
        {
            if (shifting) return;
            shifting = true;
            AudioManager.PlaySE("SE_CARDEXPAND_ZOOMOUT");
            expanded = false;

            CardRect.DOScale(new Vector3(1.5f, 1.5f, 1f), 0.2f).SetEase(Ease.OutCubic);
            CardRect.DOLocalMove(new Vector3(0f, 0f, -100f), 0.2f).SetEase(Ease.OutCubic);
            CardRect.DORotate(new Vector3(-4f, -6f, -1f), 0.15f).OnComplete(() =>
            {
                CardRect.DORotate(Vector3.zero, 0.2f).OnComplete(() => shifting = false); // TODO: tween in tween
            });

            TextShortcut.text = InterString.Get("扩大");
        }

    }
}
