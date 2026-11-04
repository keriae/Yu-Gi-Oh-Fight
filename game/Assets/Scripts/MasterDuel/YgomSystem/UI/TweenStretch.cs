using UnityEngine;

namespace YgomSystem.UI
{
	public class TweenStretch : TweenStretchTo
	{
		[SerializeField]
		public StretchOffset from;

		protected override void CaptureFrom()
		{
            m_From = from;
        }

		protected override void OnSetValue(float par)
		{
            var rt = m_RectTransform;
            StretchOffset crnt = m_From + (to - m_From) * par;
            rt.offsetMin = new Vector2(crnt.left, crnt.bottom);
            rt.offsetMax = new Vector2(-crnt.right, -crnt.top);
        }
	}
}
