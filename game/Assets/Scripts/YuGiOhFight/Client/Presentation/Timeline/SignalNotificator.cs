using System.Runtime.CompilerServices;


namespace YuGiOhFight.Client.Presentation.Timeline
{
	public class SignalNotificator : UnityEngine.MonoBehaviour, UnityEngine.Playables.INotificationReceiver
	{
		public event System.Action<string, double> callback
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public void OnNotify(UnityEngine.Playables.Playable origin, UnityEngine.Playables.INotification notification, object context)
		{
		}

		private void OnDestroy()
		{
		}
	}
}
