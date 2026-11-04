using UnityEngine;

namespace YgomSystem.UI
{
	public class TweenPositionTargetFrom : Tween
	{
		private RectTransform rtrans;

		[SerializeField]
		public Vector3 from;

		[SerializeField]
		public bool updateX;

		[SerializeField]
		public bool updateY;

		[SerializeField]
		public bool updateZ;

		private Vector3 to;

		protected override void CaptureFrom()
		{
            rtrans = GetComponent<RectTransform>();
            to = rtrans.anchoredPosition3D;
        }

		protected override void OnSetValue(float par)
		{
            if (rtrans == null)
                rtrans = GetComponent<RectTransform>();

            Vector3 target = Vector3.Lerp(from, to, par);
            Vector3 crnt = rtrans.anchoredPosition3D;
            if (updateX)
                crnt.x = target.x;
            if (updateY)
                crnt.y = target.y;
            if (updateZ)
                crnt.z = target.z;
            rtrans.anchoredPosition3D = crnt;
        }
	}
}
