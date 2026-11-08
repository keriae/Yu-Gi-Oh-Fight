using DG.Tweening;


using UnityEngine;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
{
    public class PopupDuel : PopupBase
    {
        [Header("Popup Duel Reference")]
        public Button btnHide;
        [HideInInspector]
        public bool exitable;
        bool hided;
        internal bool FieldViewHidden => hided;
        public float extraHeight = 65f;

        public override void Initialize()
        {
            base.Initialize();
            Runtime.Program.instance.ocgcore.AllGameObjects.Add(gameObject);
            if (btnHide != null)
                btnHide.onClick.AddListener(FieldView);
            Runtime.Program.instance.ocgcore.currentPopup = this;
            if (!exitable)
            {
                if (btnHide != null)
                    Runtime.Program.instance.ocgcore.returnAction = FieldView;
                else
                    Runtime.Program.instance.ocgcore.returnAction = () => { };
            }

            var uiScale = MDPro3.Config.GetUIScale(maxUIScale);
            hideY = -540f - height * uiScale - extraHeight;
            window.anchoredPosition = new Vector2(0f, hideY);
        }

        public virtual void FieldView()
        {
            if (hided)
            {
                hided = false;
                window.DOAnchorPosY(showY, transitionTime);
                Runtime.AudioManager.PlaySE("SE_MENU_SLIDE_03");
                btnHide.transform.GetChild(0).gameObject.SetActive(false);
            }
            else
            {
                hided = true;
                var scale = window.transform.localScale.x;
                var targetY = -540 - height * scale;
                window.DOAnchorPosY(targetY, transitionTime);
                Runtime.AudioManager.PlaySE("SE_MENU_SLIDE_04");
                btnHide.transform.GetChild(0).gameObject.SetActive(true);
            }
        }

        public override void Show()
        {
            Initialize();
            window.DOAnchorPos(new Vector2(0f, showY), transitionTime);
            if (shadow != null)
            {
                shadow.DOFade(0.8f, transitionTime);
                shadow.raycastTarget = true;
            }
        }

        public override void Hide()
        {
            if (shadow != null)
                shadow.DOFade(0f, transitionTime);
            window.DOAnchorPos(new Vector2(0f, hideY), transitionTime).OnComplete(() =>
            {
                Destroy(gameObject);
                Runtime.Program.instance.ocgcore.returnAction = null;
                whenQuitDo?.Invoke();
            });
            Runtime.Program.instance.ocgcore.currentPopup = null;
        }

        public override void OnConfirm()
        {
            Runtime.AudioManager.PlaySE("SE_DUEL_DECIDE");
        }

        public override void OnCancel()
        {
            Runtime.AudioManager.PlaySE("SE_DUEL_CANCEL");
        }

        public void OnDestroy()
        {
            Runtime.Program.instance.ocgcore.returnAction = null;
        }

        protected void SendReturn(byte[] buffer)
        {
            Runtime.Program.instance.ocgcore.SendReturn(buffer, transitionTime);
        }

    }
}
