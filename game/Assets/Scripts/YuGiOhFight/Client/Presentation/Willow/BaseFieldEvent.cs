using System;


using UnityEngine.Scripting;


namespace YuGiOhFight.Client.Presentation.Willow.InGameField
{
	[RequireDerived]
	public abstract class BaseFieldEvent : UnityEngine.ScriptableObject
	{
		[NonSerialized]
		public BaseFieldEvent parent;

		[NonSerialized]
		public System.Collections.Generic.List<BaseFieldEvent> childEventList;

		public EventScope eventScope;
	}
}
