using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using UnityEngine.EventSystems;


namespace YuGiOhFight.Client.UI
{
    public class PopupBase : MonoBehaviour
    {
        [Header("Popup Reference")]
        public Image shadow;
        public RectTransform window;
        public Text title;
        public float transitionTime = 0.3f;
        public Button btnConfirm;
        public Button btnCancel;
        public float buttomOffest = 180;
        public float maxUIScale = 1.5f;
        public float showY = 0f;
        public float height = 195f;
        public Selectable defaultSelect;

        [HideInInspector]
        public List<string> selections;
        public Action whenQuitDo;
        public float hideY = 0f;

        protected PopupBase lastPopup;

        public virtual void Initialize()
        {
            if (shadow != null)
                shadow.color = Color.clear;

            var uiScale = MDPro3.Config.GetUIScale(maxUIScale);
            window.localScale = Vector3.one * uiScale;

            hideY = -540f - height * uiScale;
            window.anchoredPosition = new Vector2(0f, hideY);

            Runtime.UIManager.Translate(gameObject);

            if (btnConfirm != null && btnCancel != null)
            {
                bool confirmOnLeft = MDPro3.Config.GetBool("Confirm", false);
                float height = btnConfirm.GetComponent<RectTransform>().anchoredPosition.y;
                if (!confirmOnLeft)
                {
                    btnConfirm.GetComponent<RectTransform>().anchoredPosition = new Vector2(buttomOffest, height);
                    btnCancel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-buttomOffest, height);
                }
            }
            if (btnConfirm != null)
                btnConfirm.onClick.AddListener(OnConfirm);
            if (btnCancel != null)
                btnCancel.onClick.AddListener(OnCancel);

            lastPopup = Runtime.Program.instance.ui_.currentPopup;
            Runtime.Program.instance.ui_.currentPopup = this;

            InitializeSelections();
            Client.FightDuelPopupSkin.Attach(this);
        }
        public virtual void InitializeSelections()
        {
            if (title != null)
                title.text = selections[0];
        }

        public virtual void Show()
        {
            Initialize();
            window.DOAnchorPos(new Vector2(0f, showY), transitionTime).OnComplete(() =>
            {
                Runtime.Program.instance.currentServant.returnAction = Hide;
            });
            if (shadow != null)
            {
                shadow.DOFade(0.8f, transitionTime);
                shadow.raycastTarget = true;
            }

            if (defaultSelect != null)
                EventSystem.current.SetSelectedGameObject(defaultSelect.gameObject);
        }

        public virtual void Hide()
        {
            if (shadow != null)
                shadow.DOFade(0f, transitionTime);
            Controllers.Servant servant = Runtime.Program.instance.currentServant;
            window.DOAnchorPos(new Vector2(0f, hideY), transitionTime).OnComplete(() =>
            {
                Destroy(gameObject);
                servant.returnAction = null;
                Runtime.Program.instance.ui_.currentPopup = lastPopup;
                if (lastPopup == null && Cursor.lockState == CursorLockMode.Locked)
                    Runtime.Program.instance.currentServant.Select();

                whenQuitDo?.Invoke();
            });
        }
        public virtual void OnConfirm()
        {
            Runtime.AudioManager.PlaySE("SE_MENU_DECIDE");
        }

        public virtual void OnCancel()
        {
            Runtime.AudioManager.PlaySE("SE_MENU_CANCEL");
        }
    }
}
