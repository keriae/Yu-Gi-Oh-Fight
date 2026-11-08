using System;


namespace YuGiOhFight.Client.Presentation.Timeline
{
	public class EventPlayableAsset : UnityEngine.Playables.PlayableAsset
	{
		[Serializable]
		public class EventInfo
		{
			public string label;

			public double time;
		}

		public string label;

		public System.Collections.Generic.List<EventInfo> eventList;

		public override double duration => 0.0;

		public override UnityEngine.Playables.Playable CreatePlayable(UnityEngine.Playables.PlayableGraph graph, UnityEngine.GameObject go)
		{
            UnityEngine.Playables.ScriptPlayable<EventPlayableBehaviour> playable = UnityEngine.Playables.ScriptPlayable<EventPlayableBehaviour>.Create(graph);
            EventPlayableBehaviour behaviour = playable.GetBehaviour();
			behaviour.label = label;
			behaviour.eventList = new System.Collections.Generic.List<EventPlayableBehaviour.EventInfo>();
			foreach(EventInfo info in eventList)
			{
				behaviour.eventList.Add(new EventPlayableBehaviour.EventInfo()
				{
					label = info.label,
					time = info.time,
					isDone = false
				});
            }
			return playable;
        }
    }
}
