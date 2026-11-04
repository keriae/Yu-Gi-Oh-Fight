using UnityEngine;
using UnityEngine.UI;

namespace YgomSystem.UI
{
	public class TweenScroll : Tween
	{
		public enum SCROLL_TYPE
		{
			HORIZONTAL = 0,
			VERTICAL = 1
		}

		[SerializeField]
		public float from;

		[SerializeField]
		public float to;

		[SerializeField]
		public SCROLL_TYPE scrollType;

		[SerializeField]
		public bool normalize;

		private ScrollRect m_ScrollRect;

		private ScrollRect scrollRect => m_ScrollRect != null ? m_ScrollRect : (m_ScrollRect = GetComponent<ScrollRect>());

		protected override void CaptureFrom()
		{
            var sr = scrollRect;
            if (sr == null)
                return;
            if (normalize)
            {
                from = scrollType == SCROLL_TYPE.HORIZONTAL ? sr.horizontalNormalizedPosition : sr.verticalNormalizedPosition;
            }
            else
            {
                from = scrollType == SCROLL_TYPE.HORIZONTAL ? sr.content.anchoredPosition.x : sr.content.anchoredPosition.y;
            }
        }

		protected override void OnSetValue(float par)
		{
            var sr = scrollRect;
            if (sr == null || sr.content == null)
                return;
            float v = Mathf.Lerp(from, to, par);
            if (normalize)
            {
                if (scrollType == SCROLL_TYPE.HORIZONTAL)
                    sr.horizontalNormalizedPosition = v;
                else
                    sr.verticalNormalizedPosition = v;
            }
            else
            {
                var pos = sr.content.anchoredPosition;
                if (scrollType == SCROLL_TYPE.HORIZONTAL)
                {
                    pos.x = v;
                    sr.content.anchoredPosition = pos;
                }
                else
                {
                    pos.y = v;
                    sr.content.anchoredPosition = pos;
                }
            }
        }
	}
}
