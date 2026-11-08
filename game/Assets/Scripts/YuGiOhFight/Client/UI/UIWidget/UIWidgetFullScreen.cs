using DG.Tweening;

using UnityEngine;

using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
{
    public class UIWidgetFullScreen : UIWidget
    {
        [HideInInspector] public bool showing;
        protected MonoBehaviour lastInputBlocker;
        protected virtual float TransitionTime => 0.1f;
        protected virtual Vector3 HideScale => new(0.95f, 0.95f, 1f);
        protected virtual string Label_SE_Show => "SE_DECK_WINDOW_OPEN";
        protected virtual string Label_SE_Hide => "SE_MENU_CANCEL";
        protected virtual bool DestroyOnHide => true;
        protected virtual Selectable DefaultSelectable => null;

        public virtual void Show()
        {
            if(showing) return;
            showing = true;
            ShowEvent();

            KillTransitionTweens();
            CG.alpha = 0f;
            CG.blocksRaycasts = true;
            CG.DOFade(1f, TransitionTime).SetUpdate(true);

            Rect.localScale = HideScale;
            Rect.DOScale(Vector3.one, TransitionTime)
                .SetUpdate(true)
                .SetEase(Ease.OutQuart)
                .OnComplete(AfterShowEvent);
        }

        public virtual void Hide()
        {
            if (!showing) return;
            showing = false;
            HideEvent();

            KillTransitionTweens();
            CG.DOFade(0f, TransitionTime).SetUpdate(true);
            Rect.DOScale(HideScale, TransitionTime)
                .SetUpdate(true)
                .SetEase(Ease.InCubic)
                .OnComplete(() =>
                {
                    CG.blocksRaycasts = false;
                    AfterHideEvent();
                });
        }

        protected virtual void Update()
        {
            if (!NeedResponse()) return;

            if (Runtime.UserInput.WasCancelPressed || Runtime.UserInput.MouseRightDown)
                Hide();
        }

        protected virtual bool NeedResponse()
        {
            if (!showing)
                return false;
            if (Runtime.UIManager.InputBlocker != this)
                return false;
            if (Runtime.Program.instance.ui_.currentPopupB != null)
                return false;
            return true;
        }

        protected virtual void ShowEvent()
        {
            Runtime.AudioManager.PlaySE(Label_SE_Show);

            lastInputBlocker = Runtime.UIManager.InputBlocker;
            Runtime.UIManager.InputBlocker = this;
        }

        protected virtual void AfterShowEvent()
        {
            Select();
        }

        protected virtual void HideEvent()
        {
            Runtime.AudioManager.PlaySE(Label_SE_Hide);
        }

        protected virtual void AfterHideEvent()
        {
            Runtime.UIManager.InputBlocker = lastInputBlocker;
            if (lastInputBlocker == null)
                Runtime.Program.instance.currentServant.Select();
            else if (lastInputBlocker is UIWidgetFullScreen ui)
                ui.Select();
            if (DestroyOnHide)
                Destroy(gameObject);
        }

        private void KillTransitionTweens()
        {
            DOTweenLifecycle.KillTarget(CG);
            DOTweenLifecycle.KillTarget(Rect);
        }

    }
}
