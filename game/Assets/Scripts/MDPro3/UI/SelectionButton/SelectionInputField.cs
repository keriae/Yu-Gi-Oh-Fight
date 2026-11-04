using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace MDPro3.UI
{
    [RequireComponent(typeof(TMP_InputField))]
    public class SelectionInputField : SelectionButton
    {
        [Header("SelectionButton_InputField")]
        [SerializeField] private int upNum = 10;
        [SerializeField] private int rightNum = 1;

        private TMP_InputField m_InputField;
        public TMP_InputField InputField =>
            m_InputField = m_InputField != null ? m_InputField
            : GetComponent<TMP_InputField>();

        public override void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
            pressing = false;
            if(!InputField.isFocused)
                OnExit();
        }

        protected override void HoverOn()
        {
            base.HoverOn();
            var hover = Manager.GetElement("ButtonHover");
            if (hover != null)
            {
                if (hover.TryGetComponent<CanvasGroup>(out var cg))
                    cg.alpha = 1.0f;
                if (hover.TryGetComponent<DOTweenAnimation>(out var animation))
                    animation.DORestart();
            }
        }

        protected override void HoverOff(bool forced = false)
        {
            base.HoverOff(forced);
            var hover = Manager.GetElement("ButtonHover");
            if (hover != null)
            {
                if (hover.TryGetComponent<DOTweenAnimation>(out var animation))
                    animation.DOPause();
                if (hover.TryGetComponent<CanvasGroup>(out var cg))
                    cg.alpha = 0.0f;
            }
        }

        public virtual void SetSubmitEvent(UnityAction<string> call)
        {
            if (Selectable is InputField inputField)
                inputField.onSubmit.AddListener(call);
        }

        protected override void OnNavigation(AxisEventData eventData)
        {
            if (eventData.moveDir == MoveDirection.Up)
            {
                if(InputField.isFocused) 
                {
                    TryAdjustFocusedIntegerValue(upNum);
                    return;
                }
                else if (navigationEvent.onUpNavigation.GetPersistentEventCount() > 0)
                {
                    navigationEvent.onUpNavigation.Invoke();
                    return;
                }
            }
            else if (eventData.moveDir == MoveDirection.Down)
            {
                if (InputField.isFocused)
                {
                    TryAdjustFocusedIntegerValue(-upNum);
                    return;
                }
                else if (navigationEvent.onDownNavigation.GetPersistentEventCount() > 0)
                {
                    navigationEvent.onDownNavigation.Invoke();
                    return;
                }
            }
            else if (eventData.moveDir == MoveDirection.Left)
            {
                if (InputField.isFocused)
                {
                    TryAdjustFocusedIntegerValue(-rightNum);
                    return;
                }
                else if (navigationEvent.onLeftNavigation.GetPersistentEventCount() > 0)
                {
                    navigationEvent.onLeftNavigation.Invoke();
                    return;
                }
            }
            else if (eventData.moveDir == MoveDirection.Right)
            {
                if (InputField.isFocused)
                {
                    TryAdjustFocusedIntegerValue(rightNum);
                    return;
                }
                else if (navigationEvent.onRightNavigation.GetPersistentEventCount() > 0)
                {
                    navigationEvent.onRightNavigation.Invoke();
                    return;
                }
            }

            base.OnNavigation(eventData);
        }

        private void TryAdjustFocusedIntegerValue(int delta)
        {
            if (!AllowArrowAdjustForFocusedInput())
                return;
            if (int.TryParse(InputField.text, out int num))
                InputField.text = (num + delta).ToString();
        }

        private bool AllowArrowAdjustForFocusedInput()
        {
            return InputField.contentType == TMP_InputField.ContentType.IntegerNumber
                || InputField.characterValidation == TMP_InputField.CharacterValidation.Integer;
        }
    }
}
