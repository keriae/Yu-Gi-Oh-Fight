using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
{
    public class SelectionToggle : SelectionButton
    {
        #region Elements

        private const string LABEL_GO_ON = "On";
        private GameObject m_PartOn;
        private GameObject PartOn =>
            m_PartOn = m_PartOn != null ? m_PartOn
            : Manager.GetElement(LABEL_GO_ON);

        private const string LABEL_GO_OFF = "Off";
        private GameObject m_PartOff;
        private GameObject PartOff =>
            m_PartOff = m_PartOff != null ? m_PartOff
            : Manager.GetElement(LABEL_GO_OFF);

        private const string LABEL_CG_HoverOn = "HoverOn";
        private CanvasGroup m_HoverOnCG;
        private CanvasGroup HoverOnCG =>
            m_HoverOnCG = m_HoverOnCG != null ? m_HoverOnCG
            : Manager.GetElement<CanvasGroup>(LABEL_CG_HoverOn);
        private DOTweenAnimation m_HoverOnAnimation;
        private DOTweenAnimation HoverOnAnimation =>
            m_HoverOnAnimation = m_HoverOnAnimation != null ? m_HoverOnAnimation
            : Manager.GetElement<DOTweenAnimation>(LABEL_CG_HoverOn);

        private const string LABEL_CG_HoverOff = "HoverOff";
        private CanvasGroup m_HoverOffCG;
        private CanvasGroup HoverOffCG =>
            m_HoverOffCG = m_HoverOffCG != null ? m_HoverOffCG
            : Manager.GetElement<CanvasGroup>(LABEL_CG_HoverOff);
        private DOTweenAnimation m_HoverOffAnimation;
        private DOTweenAnimation HoverOffAnimation =>
            m_HoverOffAnimation = m_HoverOffAnimation != null ? m_HoverOffAnimation
            : Manager.GetElement<DOTweenAnimation>(LABEL_CG_HoverOff);

        private const string LABEL_GO_ICONON = "IconOn";
        private GameObject m_IconOn;
        private GameObject IconOn =>
            m_IconOn = m_IconOn != null ? m_IconOn
            : Manager.GetElement(LABEL_GO_ICONON);

        private const string LABEL_GO_ICONOFF = "IconOff";
        private GameObject m_IconOff;
        private GameObject IconOff =>
            m_IconOff = m_IconOff != null ? m_IconOff
            : Manager.GetElement(LABEL_GO_ICONOFF);

        private const string LABEL_TXT_ON = "TextOn";
        private TextMeshProUGUI m_TextOn;
        private TextMeshProUGUI TextOn =>
            m_TextOn = m_TextOn != null ? m_TextOn
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_ON);

        private const string LABEL_TXT_OFF = "TextOff";
        private TextMeshProUGUI m_TextOff;
        private TextMeshProUGUI TextOff =>
            m_TextOff = m_TextOff != null ? m_TextOff
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_OFF);

        #endregion

        [Header("Selection Toggle")]
        [SerializeField] protected string SoundLabelClickOn;
        [SerializeField] protected string SoundLabelClickOff;
        [SerializeField] protected SelectionToggleEvent toggleEvent;
        [SerializeField] protected SelectionSubmitEvent submitEvent;

        [HideInInspector] public bool isOn;
        protected bool exclusiveToggle;
        protected bool exclusiveCallOffEvent = true;
        protected bool canToggleOffSelf = true;
        protected bool toggleWhenSelected;

        #region Input Control

        //protected override void Awake()
        //{
        //    base.Awake();
        //    ToggleOff();
        //}

        protected override void OnClick()
        {
            if (!Selectable.IsInteractable())
            {
                Runtime.AudioManager.PlaySE(SoundLabelClickInactive);
                return;
            }
            if (SetToResponser)
            {
                if (Runtime.Program.instance.ui_.currentPopupB != null)
                    Runtime.Program.instance.ui_.currentPopupB.lastSelectable = Selectable;
                else
                    Runtime.Program.instance.currentServant.lastSelectable = Selectable;
            }

            if (isOn)
            {
                if (canToggleOffSelf)
                {
                    Runtime.AudioManager.PlaySE(SoundLabelClickOff);
                    SetToggleOff();
                }
                else
                {
                    Runtime.AudioManager.PlaySE(Selectable.interactable ? SoundLabelClick : SoundLabelClickInactive);
                    CallSubmitEvent();
                }
            }
            else
            {
                if (canToggleOffSelf)
                    Runtime.AudioManager.PlaySE(SoundLabelClickOn);
                else
                    Runtime.AudioManager.PlaySE(Selectable.interactable ? SoundLabelClick : SoundLabelClickInactive);
                SetToggleOn();
            }
        }

        protected override void OnSubmit()
        {
            if (!Selectable.IsInteractable())
            {
                Runtime.AudioManager.PlaySE(SoundLabelClickInactive);
                return;
            }
            if (isOn)
            {
                if (canToggleOffSelf)
                {
                    Runtime.AudioManager.PlaySE(SoundLabelClickOff);
                    SetToggleOff();
                }
                else
                {
                    Runtime.AudioManager.PlaySE(Selectable.interactable ? SoundLabelClick : SoundLabelClickInactive);
                    CallSubmitEvent();
                }
            }
            else
            {
                if (canToggleOffSelf)
                    Runtime.AudioManager.PlaySE(SoundLabelClickOn);
                else
                    Runtime.AudioManager.PlaySE(Selectable.interactable ? SoundLabelClick : SoundLabelClickInactive);

                SetToggleOn();
            }
        }

        protected override void OnSelect(bool playSE)
        {
            base.OnSelect(playSE);
            if (toggleWhenSelected && Selectable.IsInteractable())
                SetToggleOn();
        }

        #endregion

        #region Input Reaction

        protected virtual void ToggleOn()
        {
            if (isOn)
                return;

            if(SelectCursorOffset != null)
            {
                SelectCursorOffset.alpha = 1f;
                SelectCursorOffsetAnimation.DORestart();
            }

            if(PartOn != null)
                PartOn.SetActive(true);
            if(PartOff != null)
                PartOff.SetActive(false);

            if(HoverOnCG != null)
            {
                HoverOnCG.alpha = 1f;
                if(HoverOnAnimation != null)
                    HoverOnAnimation.DORestart();
            }

            if(IconOn != null)
                IconOn.SetActive(true);
            if(IconOff != null)
                IconOff.SetActive(false);
        }

        protected virtual void ToggleOff()
        {
            if (SelectCursorOffset != null)
            {
                SelectCursorOffset.alpha = 1f;
                SelectCursorOffsetAnimation.DORestart();
            }

            if (PartOn != null)
                PartOn.SetActive(false);
            if (PartOff != null)
                PartOff.SetActive(true);

            if (HoverOffCG != null)
            {
                HoverOffCG.alpha = 1f;
                if(HoverOffAnimation != null)
                    HoverOffAnimation.DORestart();
            }

            if (IconOn != null)
                IconOn.SetActive(false);
            if (IconOff != null)
                IconOff.SetActive(true);
        }

        protected override void HoverOn()
        {
            if (hoverd)
                return;
            base.HoverOn();

            if (isOn)
            {
                if (HoverOnCG != null)
                {
                    HoverOnCG.alpha = 1f;
                    if (HoverOnAnimation != null)
                        HoverOnAnimation.DORestart();
                }
            }
            else
            {
                if (HoverOffCG != null)
                {
                    HoverOffCG.alpha = 1f;
                    if (HoverOffAnimation != null)
                        HoverOffAnimation.DORestart();
                }
            }
        }

        protected override void HoverOff(bool force = false)
        {
            if (!hoverd)
                return;
            base.HoverOff();

            if (isOn)
            {
                if(HoverOnCG != null)
                {
                    if(HoverOnAnimation != null)
                        HoverOnAnimation.DOPause();
                    HoverOnCG.alpha = 0f;
                }
            }
            else
            {
                if(HoverOffCG != null)
                {
                    if(HoverOffAnimation != null)
                        HoverOffAnimation.DOPause();
                    HoverOffCG.alpha = 0f;
                }
            }
        }

        protected virtual void CallToggleOnEvent()
        {
            toggleEvent.onToggleOn?.Invoke();
        }

        protected virtual void CallToggleOffEvent()
        {
            toggleEvent.onToggleOff?.Invoke();
        }

        protected virtual void CallSubmitEvent()
        {
            submitEvent.onSubmit?.Invoke();
        }

        #endregion

        #region Public Function

        public virtual void SetToggleOn(bool callEvent = true)
        {
            ToggleOn();

            isOn = true;

            if (exclusiveToggle)
            {
                for (int i = 0; i < transform.parent.childCount; i++)
                {
                    if (!transform.parent.GetChild(i).TryGetComponent<SelectionToggle>(out SelectionToggle toggle))
                        continue;
                    if (toggle != this)
                        toggle.SetToggleOff(exclusiveCallOffEvent);
                }
            }

            if (callEvent)
                CallToggleOnEvent();
        }

        public virtual void SetToggleOff(bool callEvent = true)
        {
            isOn = false;
            ToggleOff();
            if(callEvent)
                CallToggleOffEvent();
        }

        public virtual void SwitchToggle()
        {
            if (isOn)
            {
                Runtime.AudioManager.PlaySE(SoundLabelClickOff);
                SetToggleOff();
            }
            else
            {
                Runtime.AudioManager.PlaySE(SoundLabelClickOn);
                SetToggleOn();
            }
        }

        public virtual void SetToggleOnEvent(UnityAction call)
        {
            toggleEvent.onToggleOn.AddListener(call);
        }

        public virtual void SetToggleOffEvent(UnityAction call)
        {
            toggleEvent.onToggleOff.AddListener(call);
        }

        public virtual void SetSubmitEvent(UnityAction call)
        {
            submitEvent.onSubmit.AddListener(call);
        }

        public override void SetButtonText(string title)
        {
            base.SetButtonText(title);
            var buttonTextOn = Manager.GetElement("TextOn");
            if (buttonTextOn != null && buttonTextOn.TryGetComponent<TextMeshProUGUI>(out var textOn))
                textOn.text = title;
            var buttonTextOff = Manager.GetElement("TextOff");
            if (buttonTextOff != null && buttonTextOff.TryGetComponent<TextMeshProUGUI>(out var textOff))
                textOff.text = title;
        }

        public virtual void SetToggleText(string titleOn, string titleOff)
        {
            var buttonTextOn = Manager.GetElement("TextOn");
            if (buttonTextOn != null && buttonTextOn.TryGetComponent<TextMeshProUGUI>(out var textOn))
                textOn.text = titleOn;
            var buttonTextOff = Manager.GetElement("TextOff");
            if (buttonTextOff != null && buttonTextOff.TryGetComponent<TextMeshProUGUI>(out var textOff))
                textOff.text = titleOff;
        }

        public override string GetButtonText()
        {
            var returnValue = base.GetButtonText();
            if (returnValue != string.Empty)
                return returnValue;

            if(TextOn != null)
                if(TextOn.text != string.Empty)
                    return TextOn.text;
            if(TextOff != null)
                if(TextOff.text != string.Empty)
                    return TextOff.text;

            return returnValue;
        }

        #endregion
    }

    [Serializable]
    public class SelectionToggleEvent
    {
        public UnityEvent onToggleOn;
        public UnityEvent onToggleOff;
    }

    [Serializable]
    public class SelectionSubmitEvent
    {
        public UnityEvent onSubmit;
    }
}
