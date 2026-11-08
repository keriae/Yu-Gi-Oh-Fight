using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class TweenSE : Tween
	{
		[SerializeField]
		public string m_SoundLabel;

		[SerializeField]
		public bool m_PlayOnFinish;

		protected override void OnSetValue(float par)
		{
			// Keep serialized tween data and completion events. The label audio
			// service was never initialized and its playback path was inert.
		}
	}
}
