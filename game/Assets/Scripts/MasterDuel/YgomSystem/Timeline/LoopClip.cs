using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace YgomSystem.Timeline
{
	[Serializable]
	public class LoopClip : PlayableAsset
	{
		[HideInInspector] public LoopBehaviour template;

		[HideInInspector] public TimelineClip loopClip;

		public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
		{
			var playable = ScriptPlayable<LoopBehaviour>.Create(graph);
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
