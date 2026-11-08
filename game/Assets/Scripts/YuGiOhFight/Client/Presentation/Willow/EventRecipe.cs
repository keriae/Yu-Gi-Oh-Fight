using System;

using UnityEngine;


namespace YuGiOhFight.Client.Presentation.Willow.InGameField
{
	[Serializable]
#if UNITY_EDITOR
	[CreateAssetMenu]
#endif
	public class EventRecipe : ScriptableObject
	{
		public System.Collections.Generic.List<IntFieldEvent> intFieldEventList;

		public System.Collections.Generic.List<BoolFieldEvent> boolFieldEventList;
	}
}
