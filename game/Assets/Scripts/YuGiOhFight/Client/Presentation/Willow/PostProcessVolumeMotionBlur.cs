namespace YuGiOhFight.Client.Presentation.Willow
{
	public class PostProcessVolumeMotionBlur : BasePostProcessVolumeControl
	{
		private UnityEngine.Rendering.Universal.MotionBlur m_motionBlur;

		public float m_intensity;

		public float m_clamp;

		private bool m_usedIntensity;

		private bool m_usedClamp;

		private float m_prevIntensity;

		private float m_prevClamp;

		private UnityEngine.Rendering.Universal.MotionBlur motionBlur => null;

		protected override void CheckPostProcess()
		{
		}

		protected override void StartPostProcess()
		{
		}

		protected override void UpdatePostProcess()
		{
		}
	}
}
