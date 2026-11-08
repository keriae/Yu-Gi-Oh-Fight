using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;


namespace YuGiOhFight.Client.UI
{
    public class ButtonHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public UnityEvent onPointDown;
        public UnityEvent onPointUp;
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                onPointDown?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                onPointUp?.Invoke();
        }
    }
}
