using UnityEngine;


namespace YuGiOhFight.Client.Presentation.Willow.InGameField
{
#if UNITY_EDITOR
	[CreateAssetMenu]
#endif
	public class BoolFieldEventCommandSet : ScriptableObject
	{
		public BoolFieldEvent boolFieldEvent;

		public bool boolValue;
	}
}
