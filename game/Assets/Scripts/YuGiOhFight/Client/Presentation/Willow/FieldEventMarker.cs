using System;
using System.ComponentModel;


using UnityEngine.Scripting;


namespace YuGiOhFight.Client.Presentation.Willow.InGameField
{
	[Serializable]
	[DisplayName]
	[Preserve]
	public class FieldEventMarker : UnityEngine.Timeline.Marker, UnityEngine.Playables.INotification
	{
		public TriggerFieldEventCommandSet triggerFieldEventCommandSet;

		public UnityEngine.PropertyName id => default(UnityEngine.PropertyName);
	}
}
