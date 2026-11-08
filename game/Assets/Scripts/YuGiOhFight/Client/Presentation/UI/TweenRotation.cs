namespace YuGiOhFight.Client.Presentation.UI
{
	public class TweenRotation : Tween
	{
		public UnityEngine.Vector3 from;

		public UnityEngine.Vector3 to;

		public bool quaternionLerp;

		protected override void OnSetValue(float par)
		{
            if (quaternionLerp)
            {
                transform.localRotation = UnityEngine.Quaternion.Slerp(UnityEngine.Quaternion.Euler(from), UnityEngine.Quaternion.Euler(to), par);
            }
            else
            {
                transform.localEulerAngles = UnityEngine.Vector3.Lerp(from, to, par);
            }
        }
	}
}
