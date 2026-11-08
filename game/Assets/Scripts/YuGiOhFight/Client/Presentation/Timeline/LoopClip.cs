using System;
using UnityEngine;


namespace YuGiOhFight.Client.Presentation.Timeline
{
	[Serializable]
	public class LoopClip : UnityEngine.Playables.PlayableAsset
	{
		[HideInInspector] public LoopBehaviour template;

		[HideInInspector] public UnityEngine.Timeline.TimelineClip loopClip;

		public override UnityEngine.Playables.Playable CreatePlayable(UnityEngine.Playables.PlayableGraph graph, GameObject go)
		{
			UnityEngine.Playables.ScriptPlayable<LoopBehaviour> playable = UnityEngine.Playables.ScriptPlayable<LoopBehaviour>.Create(graph);
			template = playable.GetBehaviour();
			return playable;
		}

		public void PassClip()
		{
			if (template != null)
                template.loopClip = loopClip;
            else
                Debug.Log("LoopClip template is Null !");
		}
	}
}
