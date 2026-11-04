using UnityEngine;
using UnityEngine.UI;

namespace YgomSystem.UI
{
	public class TweenSize : Tween
	{
		private RectTransform rtrans;

		[SerializeField]
		private Vector2 from;

		[SerializeField]
		public Vector2 to;

		[SerializeField]
		public bool ignoreWidth;

		[SerializeField]
		public bool ignoreHeight;

		private LayoutElement layout;

		protected override void CaptureFrom()
		{
            rtrans = GetComponent<RectTransform>();
            layout = GetComponent<LayoutElement>();
        }

		protected override void OnSetValue(float par)
		{
            if (rtrans == null)
                rtrans = GetComponent<RectTransform>();
            if (layout == null)
                layout = GetComponent<LayoutElement>();

            Vector2 target = Vector2.Lerp(from, to, par);
            Vector2 crnt = layout != null
                ? new Vector2(layout.preferredWidth, layout.preferredHeight)
                : rtrans.sizeDelta;

            if (ignoreWidth)
                target.x = crnt.x;
            if (ignoreHeight)
                target.y = crnt.y;

            if (layout != null)
            {
                layout.preferredWidth = target.x;
                layout.preferredHeight = target.y;
            }
            else
            {
                rtrans.sizeDelta = target;
            }
        }
	}
}
