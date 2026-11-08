using System;


using UnityEngine;
using UnityEngine.EventSystems;


namespace YuGiOhFight.Client.UI
{
    public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action hoverIn;
        public Action hoverOut;

        public void OnPointerEnter(PointerEventData eventData)
        {
            hoverIn?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hoverOut?.Invoke();

        }
    }
}
