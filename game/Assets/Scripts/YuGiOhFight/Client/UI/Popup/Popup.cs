using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI.Popup
{
    public class Popup : MonoBehaviour
    {
        [Header("Popup")]
        public Selectable lastSelectable;

        [HideInInspector] public List<string> args;
        [HideInInspector] public float transitionTime = 0.2f;
        public Action quitAction;

        private Presentation.Elements.ElementObjectManager m_Manager;
        protected Presentation.Elements.ElementObjectManager Manager
        {
            get
            {
                if (m_Manager == null)
                    m_Manager = GetComponent<Presentation.Elements.ElementObjectManager>();
                return m_Manager;
            }
        }
        protected bool inTransition = true;
        protected bool cancelCallHide = true;

        private Popup lastPopup;

        protected virtual void Initialize()
        {
            var closeButton = Manager.GetElement("CloseButton");
            if (closeButton != null)
            {
                if (closeButton.TryGetComponent<Button>(out var btn))
                    btn.onClick.AddListener(Hide);
                if (closeButton.TryGetComponent<CanvasGroup>(out var cg))
                    cg.alpha = 0f;
            }
            Manager.GetElement<RectTransform>("Content").anchoredPosition = new Vector2(0f, -1080f);

            Runtime.UIManager.Translate(gameObject);
            SetButtonsLayout();

            lastPopup = Runtime.Program.instance.ui_.currentPopupB;
            Runtime.Program.instance.ui_.currentPopupB = this;

            InitializeSelections();
            Client.FightDuelPopupSkin.Attach(this);
        }

        protected virtual void InitializeSelections()
        {
            if (Manager.GetElement("TitleText") != null && args != null && args.Count > 0)
                Manager.GetElement<TextMeshProUGUI>("TitleText").text = args[0];
        }

        public virtual void Show()
        {
            Initialize();
            KillTransitionTweens();
            Manager.GetElement<RectTransform>("Content").DOAnchorPosY(0f, transitionTime).SetEase(Ease.OutCubic).OnComplete(() =>
            {
                inTransition = false;
                if (Cursor.lockState == CursorLockMode.Locked)
                    SelectLastSelected();
            });

            if (Manager.GetElement("CloseButton") != null)
                Manager.GetElement<CanvasGroup>("CloseButton").DOFade(0.8f, transitionTime);
        }
        public virtual void Hide()
        {
            inTransition = true;
            KillTransitionTweens();
            Manager.GetElement<RectTransform>("Content").DOAnchorPosY(-1080f, transitionTime).SetEase(Ease.InCubic).OnComplete(() =>
            {
                Destroy(gameObject);
                Runtime.Program.instance.ui_.currentPopupB = lastPopup;
                quitAction?.Invoke();

                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    if (lastPopup != null)
                        lastPopup.SelectLastSelected();
                    else
                        Runtime.Program.instance.currentServant.Select();
                }
            });

            if (Manager.GetElement("CloseButton") != null)
                Manager.GetElement<CanvasGroup>("CloseButton").DOFade(0f, transitionTime);
        }

        protected virtual void OnDecide()
        {
            Hide();
        }
        protected virtual void OnCancel()
        {
            Hide();
        }

        protected virtual void SetButtonsLayout()
        {
            var btnCancel = Manager.GetElement("CancelButton");
            var btnDecide = Manager.GetElement("DecideButton");

            if(btnCancel != null)
                btnCancel.GetComponent<Button>().onClick.AddListener(OnCancel);
            if(btnDecide != null)
                btnDecide.GetComponent<Button>().onClick.AddListener(OnDecide);

            if (btnCancel == null || btnDecide == null)
                return;

            var buttonCancel = btnCancel.GetComponent<Button>();
            var buttonDecide = btnDecide.GetComponent<Button>();

            if (MDPro3.Config.Have("Confirm") && MDPro3.Config.GetBool("Confirm", false))
            {
                btnDecide.transform.SetSiblingIndex(1);
                var navigation = buttonCancel.navigation;
                navigation.selectOnRight = null;
                buttonCancel.navigation = navigation;

                navigation = buttonDecide.navigation;
                navigation.selectOnLeft = null;
                buttonDecide.navigation = navigation;
            }
            else
            {
                var navigation = buttonCancel.navigation;
                navigation.selectOnLeft = null;
                buttonCancel.navigation = navigation;

                navigation = buttonDecide.navigation;
                navigation.selectOnRight = null;
                buttonDecide.navigation = navigation;
            }
        }

        protected virtual void SelectLastSelected()
        {
            EventSystem.current.SetSelectedGameObject(lastSelectable.gameObject);
        }

        protected virtual void OnEnable()
        {
            Runtime.UserInput.OnMouseCursorHide += SelectLastSelected;
        }
        protected virtual void OnDisable()
        {
            Runtime.UserInput.OnMouseCursorHide -= SelectLastSelected;
        }

        protected virtual void OnDestroy()
        {
            DOTweenLifecycle.KillFor(gameObject);
        }

        private void KillTransitionTweens()
        {
            DOTweenLifecycle.KillTarget(Manager.GetElement<RectTransform>("Content"));
            var closeButton = Manager.GetElement("CloseButton");
            if (closeButton != null && closeButton.TryGetComponent<CanvasGroup>(out var closeButtonCG))
                DOTweenLifecycle.KillTarget(closeButtonCG);
        }

        protected virtual bool NeedResponseInput()
        {
            if (inTransition)
                return false;
            if (Runtime.Program.instance.ui_.currentPopupB != this)
                return false;
            if(Runtime.UserInput.InputFieldActivating())
                return false;

            return true;
        }

        protected virtual void Update()
        {
            if (!NeedResponseInput())
                return;

            if ((Runtime.UserInput.MouseRightDown || Runtime.UserInput.WasCancelPressed) && cancelCallHide)
            {
                Runtime.AudioManager.PlaySE("SE_MENU_CANCEL");
                Hide();
            }
        }
    }
}
