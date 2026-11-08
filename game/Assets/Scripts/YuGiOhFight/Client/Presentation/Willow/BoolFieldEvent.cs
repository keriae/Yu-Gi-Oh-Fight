using System.Runtime.CompilerServices;
using UnityEngine;


namespace YuGiOhFight.Client.Presentation.Willow.InGameField
{
#if UNITY_EDITOR
	[CreateAssetMenu]
#endif
	public class BoolFieldEvent : BaseFieldEvent
	{
		private readonly System.Collections.Generic.List<BoolFieldEventListener> m_fieldEventListeners;

		private event System.Action<bool> m_responseBool
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

		public void RegisterListener(System.Action<bool> action)
		{
		}

		public void UnregisterListener(System.Action<bool> action)
		{
		}

		public void RegisterListener(BoolFieldEventListener listener)
		{
		}

		public void UnregisterListener(BoolFieldEventListener listener)
		{
		}

		public void Raise(bool value)
		{
		}
	}
}
