using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Scripting;


namespace YuGiOhFight.Client.Presentation.Willow.InGameField
{
#if UNITY_EDITOR
	[CreateAssetMenu]
#endif
	[Preserve]
	public class IntFieldEvent : BaseFieldEvent
	{
		private readonly System.Collections.Generic.List<IntFieldEventListener> m_fieldEventListeners;

		private event System.Action<int> m_responseInt
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

		[Preserve]
		private void OnDisable()
		{
		}

		[Preserve]
		public void RegisterListener(System.Action<int> action)
		{
		}

		[Preserve]
		public void UnregisterListener(System.Action<int> action)
		{
		}

		[Preserve]
		public void RegisterListener(IntFieldEventListener listener)
		{
		}

		[Preserve]
		public void UnregisterListener(IntFieldEventListener listener)
		{
		}

		[Preserve]
		public void Raise(int value)
		{
		}
	}
}
