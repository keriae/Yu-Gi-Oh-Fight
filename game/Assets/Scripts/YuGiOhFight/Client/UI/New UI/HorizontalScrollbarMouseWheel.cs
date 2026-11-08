using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


namespace YuGiOhFight.Client.UI
{
    [RequireComponent(typeof(ScrollRect))]
    public class HorizontalScrollbarMouseWheel : MonoBehaviour, IScrollHandler
    {
        Scrollbar scrollbar;
        ScrollRect scrollRect;
        public float scrollSensitivity = 0.3f;
        void Start ()
        {
            scrollRect = GetComponent<ScrollRect>();
            scrollbar = scrollRect.horizontalScrollbar;
        }
        public void OnScroll(PointerEventData eventData)
        {
            // Horizontal ScrollRect already maps wheel Y to X. Running this
            // second handler would scroll the same view in the opposite direction.
            if (scrollRect == null || scrollRect.horizontal)
                return;
            float scrollDelta = eventData.scrollDelta.y;
            if (scrollbar != null && scrollDelta != 0f)
                scrollbar.value -= Mathf.Clamp(scrollDelta, -1f, 1f)
                    * scrollbar.size * scrollSensitivity * ScrollSpeed.WheelScale;
        }
    }
}
