namespace YuGiOhFight.Client.Presentation.UI
{
	public class TweenStepRotation : Tween
	{
		public UnityEngine.Vector3 from;

		public UnityEngine.Vector3 to;

		public UnityEngine.Vector3 origin;

		public UnityEngine.Vector3 step;

		protected override void OnSetValue(float par)
		{
            // 按 step 步进旋转：目标角度 = from + step * n，n 由 par 决定
            UnityEngine.Vector3 target = UnityEngine.Vector3.Lerp(from, to, par);

            UnityEngine.Vector3 snapped = target;
            if (step.x > 0f)
                snapped.x = from.x + UnityEngine.Mathf.Round((target.x - from.x) / step.x) * step.x;
            if (step.y > 0f)
                snapped.y = from.y + UnityEngine.Mathf.Round((target.y - from.y) / step.y) * step.y;
            if (step.z > 0f)
                snapped.z = from.z + UnityEngine.Mathf.Round((target.z - from.z) / step.z) * step.z;

            // 支持绕 origin 轴心旋转（本地坐标）
            if (origin != UnityEngine.Vector3.zero)
            {
                UnityEngine.Vector3 pos = transform.localPosition - origin;
                UnityEngine.Quaternion rot = UnityEngine.Quaternion.Euler(snapped);
                transform.localPosition = origin + rot * pos;
            }
            transform.localRotation = UnityEngine.Quaternion.Euler(snapped);
        }
	}
}
