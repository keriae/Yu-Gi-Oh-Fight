using UnityEngine;

namespace YgomSystem.UI
{
	public class TweenTransformTo : Tween
	{
		private Vector3 fromPosition;

		private Quaternion fromRotation;

		private Vector3 fromScale;

		[SerializeField]
		public Transform to;

		protected override void CaptureFrom()
		{
            if (to == null)
                return;
            fromPosition = transform.position;
            fromRotation = transform.rotation;
            fromScale = transform.localScale;
        }

		protected override void OnSetValue(float par)
		{
            if (to == null)
                return;
            transform.position = Vector3.Lerp(fromPosition, to.position, par);
            transform.rotation = Quaternion.Slerp(fromRotation, to.rotation, par);
            transform.localScale = Vector3.Lerp(fromScale, to.localScale, par);
        }
	}
}
