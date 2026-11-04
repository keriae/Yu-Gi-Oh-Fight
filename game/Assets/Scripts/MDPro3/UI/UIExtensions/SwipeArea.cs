using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MDPro3.UI
{
    public class SwipeArea : MonoBehaviour, ICanvasRaycastFilter
    {
        public UnityEvent OnSwipeLeft;
        public UnityEvent OnSwipeRight;

        private Vector2 startTouchPosition;
        private Vector2 currentTouchPosition;
        private bool clickIn;
        public float swipeRange = 50f;

        private void OnDisable()
        {
            clickIn = false;
        }

        public void OnPointerDown(BaseEventData data)
        {
            if (UIManager.InputBlocker != null)
                return;

            startTouchPosition = UserInput.MousePos;
            clickIn = true;
        }

        public void OnPointerUp(BaseEventData data)
        {
            if (UIManager.InputBlocker != null)
                return;

            if (clickIn)
                DetectSwipe();
            clickIn = false;
        }

        private void DetectSwipe()
        {
            currentTouchPosition = UserInput.MousePos;
            Vector2 distance = currentTouchPosition - startTouchPosition;

            if (distance.magnitude > swipeRange)
            {
                if (Mathf.Abs(distance.x) > Mathf.Abs(distance.y))
                {
                    if (distance.x > 0)
                    {
                        OnSwipeRight?.Invoke();
                    }
                    else if (distance.x < 0)
                    {
                        OnSwipeLeft?.Invoke();
                    }
                }
            }
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (UserInput.MouseLeftDown)
            {
                OnPointerDown(new BaseEventData(EventSystem.current));
            }
            else if (UserInput.MouseLeftUp)
            {
                OnPointerUp(new BaseEventData(EventSystem.current));
            }

            return true;
        }

    }
}
