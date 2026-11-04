using UnityEngine;

namespace YgomSystem.UI
{
	public class TweenPosition : Tween
	{
		private RectTransform rtrans;

		[SerializeField]
		public Vector3 from;

		[SerializeField]
		public Vector3 to;

		protected override void CaptureFrom()
		{
            rtrans = GetComponent<RectTransform>();
        }

		protected override void OnSetValue(float par)
		{
            if (rtrans == null)
                rtrans = GetComponent<RectTransform>();
            rtrans.anchoredPosition3D = Vector3.Lerp(from, to, par);
        }
	}
}
