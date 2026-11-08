using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class TweenPositionTo : Tween
	{
		private RectTransform rtrans;

		private Vector3 from;

		[SerializeField]
		public Vector3 to;

		protected override void CaptureFrom()
		{
            rtrans = GetComponent<RectTransform>();
            // UI positions stay relative to their anchors; ordinary objects
            // animate in their parent's local space.
            from = rtrans != null ? rtrans.anchoredPosition3D : transform.localPosition;
        }

		protected override void OnSetValue(float par)
		{
            if (rtrans == null)
                rtrans = GetComponent<RectTransform>();
            Vector3 position = Vector3.Lerp(from, to, par);
            if (rtrans != null)
                rtrans.anchoredPosition3D = position;
            else
                transform.localPosition = position;
        }
    }
}
