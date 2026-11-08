using System.Runtime.CompilerServices;
using UnityEngine;


namespace YuGiOhFight.Client.Presentation.Willow.InGameField
{
#if UNITY_EDITOR
	[CreateAssetMenu]
#endif
	public class TriggerFieldEvent : BaseFieldEvent
	{
		private readonly System.Collections.Generic.List<TriggerFieldEventListener> m_fieldEventListeners;

		private event System.Action m_responseTrigger
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

		private void OnDisable()
		{
		}

		public void RegisterListener(System.Action action)
		{
		}

		public void UnregisterListener(System.Action action)
		{
		}

		public void RegisterListener(TriggerFieldEventListener listener)
		{
		}

		public void UnregisterListener(TriggerFieldEventListener listener)
		{
		}

		public void Raise()
		{
		}
	}
}
