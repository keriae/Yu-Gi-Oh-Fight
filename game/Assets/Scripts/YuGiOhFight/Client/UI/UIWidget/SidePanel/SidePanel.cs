using DG.Tweening;

using UnityEngine;


namespace YuGiOhFight.Client.UI
{
    public class SidePanel : UIWidget
    {
        #region Elements

        protected SelectionButton m_ButtonBG;
        protected SelectionButton ButtonBG =>
            m_ButtonBG = m_ButtonBG != null ? m_ButtonBG
            : Manager.GetElement<SelectionButton>(LABEL_CG_BG);

        #endregion

        [HideInInspector] public bool showing;

        protected SidePanel lastSidePanel;
        protected bool shifting;
        private Tween windowTween;
        private Tween bgTween;

        protected virtual float TransitionTime => 0.2f;
        protected virtual float Width => Window.rect.width;
        protected virtual bool Permanent => false;
        protected virtual bool TakeOverInput => true;

        protected override void Awake()
        {
            if (ButtonBG != null)
                ButtonBG.SetClickEvent(() => Hide());

            if (Permanent)
            {
                gameObject.SetActive(false);
                if(CG != null)
                    CG.alpha = 1.0f;
            }
        }

        protected virtual void OnEnable()
        {
            Runtime.UserInput.OnMouseCursorHide += OnMouseCursorHide;
        }

        protected virtual void OnDisable()
        {
            Runtime.UserInput.OnMouseCursorHide -= OnMouseCursorHide;
        }

        protected virtual void Update()
        {
            if (!NeedResponse()) return;

            if (Runtime.UserInput.WasCancelPressed || Runtime.UserInput.MouseRightDown)
                HideWithSound();
        }

        protected virtual bool NeedResponse()
        {
            if (!showing)
                return false;
            if (!TakeOverInput)
                return false;

            return Runtime.Program.instance.ui_.currentSidePanel == this;
        }

        protected virtual void OnMouseCursorHide()
        {
            if (showing && NeedResponse())
                Select();
        }

        public virtual void Show()
        {
            if (showing || shifting) return;

            showing = true;
            shifting = true;
            Runtime.AudioManager.PlaySE("SE_MENU_SLIDE_01");

            if (Permanent)
            {
                gameObject.SetActive(true);
            }

            KillTransitionTweens();
            windowTween = Window.DOAnchorPosX(0f, TransitionTime).SetEase(Ease.OutQuart)
                .OnComplete(() =>
                {
                    windowTween = null;
                    shifting = false;
                    ShowCompleteCallback();
                });

            if (BG != null)
            {
                bgTween = BG.DOFade(1f, TransitionTime).OnKill(() => bgTween = null);
                BG.blocksRaycasts = true;
            }
        }

        protected virtual void ShowCompleteCallback()
        {
            if (TakeOverInput)
            {
                lastSidePanel = Runtime.Program.instance.ui_.currentSidePanel;
                Runtime.Program.instance.ui_.currentSidePanel = this;
            }
        }

        public virtual void Hide()
        {
            if (!showing || shifting) return;

            showing = false;
            shifting = true;

            KillTransitionTweens();
            windowTween = Window.DOAnchorPosX(Width + SafeAreaAdapter.GetSafeAreaRightOffset(), TransitionTime)
                .SetEase(Ease.OutQuart)
                .OnComplete(() =>
                {
                    windowTween = null;
                    shifting = false;
                    HideCompleteCallback();
                });

            if (BG != null)
            {
                bgTween = BG.DOFade(0f, TransitionTime).OnComplete(() =>
                {
                    bgTween = null;
                    BG.blocksRaycasts = false;
                });
            }
        }

        protected virtual void HideCompleteCallback()
        {
            if (TakeOverInput && Runtime.Program.instance.ui_.currentSidePanel == this)
            {
                Runtime.Program.instance.ui_.currentSidePanel = null;
                Runtime.Program.instance.currentServant.Select();
            }

            if (Permanent)
                gameObject.SetActive(false);
            else
                Destroy(gameObject);
        }
        protected virtual void HideWithSound()
        {
            Runtime.AudioManager.PlaySE("SE_MENU_SLIDE_02");
            Hide();
        }

        protected override void OnDestroy()
        {
            KillTransitionTweens();
            base.OnDestroy();
        }

        private void KillTransitionTweens()
        {
            DOTweenLifecycle.Kill(windowTween);
            windowTween = null;
            DOTweenLifecycle.Kill(bgTween);
            bgTween = null;
            DOTweenLifecycle.KillTarget(Window);
            DOTweenLifecycle.KillTarget(BG);
        }
    }
}
