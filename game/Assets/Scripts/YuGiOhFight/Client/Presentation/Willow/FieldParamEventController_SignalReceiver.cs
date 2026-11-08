using UnityEngine.Scripting;


namespace YuGiOhFight.Client.Presentation.Willow.InGameField
{
	[Preserve]
	public class FieldParamEventController_SignalReceiver : UnityEngine.MonoBehaviour, UnityEngine.Playables.INotificationReceiver
	{
		public FieldParamEventController fieldParamEventController;

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

		public void OnNotify(UnityEngine.Playables.Playable origin, UnityEngine.Playables.INotification notification, object context)
		{
		}
	}
}
