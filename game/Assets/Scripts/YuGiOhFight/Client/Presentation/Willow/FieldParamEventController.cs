namespace YuGiOhFight.Client.Presentation.Willow.InGameField
{
	public class FieldParamEventController : UnityEngine.MonoBehaviour
	{
		public Blackboard blackboard;

		public void Init()
		{
		}

		public void AttachAnimationEventReceiver(UnityEngine.GameObject target)
		{
		}

		public void AttachFieldParamEventController_SignalReceiver(UnityEngine.GameObject root)
		{
		}

		public void RaiseFromFieldCommandSet(UnityEngine.Object fieldEventCommandSet)
		{
		}

		public void RaiseFromFieldCommandSetInt(IntFieldEventCommandSet intFieldEventCommandSet)
		{
		}

		public void RaiseFromFieldCommandSetBool(BoolFieldEventCommandSet boolFieldEventCommandSet)
		{
		}

		public void RaiseFromFieldCommandSetTrigger(TriggerFieldEventCommandSet triggerFieldEventCommandSet)
		{
		}

		public void RaiseIntEvent(string targetEventName, int intValue)
		{
		}

		public void RaiseBoolEvent(string targetEventName, bool boolValue)
		{
		}

		public void RaiseTriggerEvent(string targetEventName)
		{
		}
	}
}
