using UnityEngine;


namespace YuGiOhFight.Client.Presentation.Willow.InGameField
{
#if UNITY_EDITOR
	[CreateAssetMenu]
#endif
	public class IntFieldEventCommandSet : ScriptableObject
	{
		public IntFieldEvent intFieldEvent;

		public int intValue;
	}
}
