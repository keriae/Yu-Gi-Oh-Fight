using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MDPro3.UI
{
    public class HintAutoMove : MonoBehaviour, IPointerEnterHandler
    {
        private bool top;
        private RectTransform m_Rect;
        private RectTransform Rect =>
            m_Rect = m_Rect != null ? m_Rect : GetComponent<RectTransform>();

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (top)
            {
                top = false;
                Rect.anchoredPosition = new Vector2(0, -280);
            }
            else
            {
                top = true;
                Rect.anchoredPosition = new Vector2(0, 280);
            }
        }
    }
}
