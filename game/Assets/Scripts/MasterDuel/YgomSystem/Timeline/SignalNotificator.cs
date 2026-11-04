using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Playables;

namespace YgomSystem.Timeline
{
	public class SignalNotificator : MonoBehaviour, INotificationReceiver
	{
		public event Action<string, double> callback
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

		public void OnNotify(Playable origin, INotification notification, object context)
		{
		}

		private void OnDestroy()
		{
		}
	}
}
