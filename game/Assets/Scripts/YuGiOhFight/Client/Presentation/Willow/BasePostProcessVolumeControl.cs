namespace YuGiOhFight.Client.Presentation.Willow
{
	public class BasePostProcessVolumeControl : UnityEngine.MonoBehaviour, UnityEngine.Timeline.ITimeControl
	{
		private UnityEngine.Rendering.Volume m_volume;

		private bool m_isValid;

		private bool m_isStart;

		protected UnityEngine.Rendering.Volume volume => null;

		public void SetTime(double time)
		{
		}

		public void OnControlTimeStart()
		{
		}

		public void OnControlTimeStop()
		{
		}

		protected virtual void CheckPostProcess()
		{
		}

		protected virtual void StartPostProcess()
		{
		}

		protected virtual void UpdatePostProcess()
		{
		}
	}
}
