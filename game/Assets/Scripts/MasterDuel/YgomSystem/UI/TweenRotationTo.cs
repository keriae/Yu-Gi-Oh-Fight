using UnityEngine;

namespace YgomSystem.UI
{
	public class TweenRotationTo : Tween
	{
		private Vector3 from;

		public Vector3 to;

		public bool quaternionLerp;

		protected override void CaptureFrom()
		{
            from = transform.localEulerAngles;
        }

		protected override void OnSetValue(float par)
		{
            if (quaternionLerp)
            {
                transform.localRotation = Quaternion.Slerp(Quaternion.Euler(from), Quaternion.Euler(to), par);
            }
            else
            {
                transform.localEulerAngles = Vector3.Lerp(from, to, par);
            }
        }
	}
}
