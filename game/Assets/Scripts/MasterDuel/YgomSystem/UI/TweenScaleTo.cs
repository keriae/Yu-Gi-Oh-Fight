using UnityEngine;

namespace YgomSystem.UI
{
	public class TweenScaleTo : Tween
	{
		private Vector3 from;

		[SerializeField]
		public Vector3 to;

		protected override void OnSetValue(float par)
		{
            transform.localScale = Vector3.Lerp(from, to, par);
        }

		protected override void CaptureFrom()
		{
            from = transform.localScale;
        }
	}
}
