namespace YuGiOhFight.Client.Presentation.Willow.InGameField
{
	public class FieldParamEventController_AnimationEventReceiver : UnityEngine.MonoBehaviour
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
			//fieldParamEventController.RaiseFromFieldCommandSetBool(boolFieldEventCommandSet);
			string name = boolFieldEventCommandSet.boolFieldEvent.name.Replace("_event", "");
			//Debug.Log(boolFieldEventCommandSet.boolFieldEvent.name + "-" + boolFieldEventCommandSet.boolValue);
			foreach(UnityEngine.Transform t in transform.GetComponentsInChildren<UnityEngine.Transform>(true))
			{
				if(t.name.Contains(name))
					t.gameObject.SetActive(boolFieldEventCommandSet.boolValue);
			}

        }
	}
}
