using UnityEngine.Scripting;


namespace YuGiOhFight.Client.Presentation.Willow.InGameField
{
	[RequireDerived]
	public abstract class BaseFieldEventListener : UnityEngine.MonoBehaviour
	{
		public abstract BaseFieldEvent GetTargetFieldEvent();

		public abstract void SetOrOverwriteEvent(BaseFieldEvent fieldEvent);
	}
}
