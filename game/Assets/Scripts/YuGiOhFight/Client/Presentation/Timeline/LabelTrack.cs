using System;

using UnityEngine;

using UnityEngine.Timeline;


namespace YuGiOhFight.Client.Presentation.Timeline
{
	[Serializable]
	[ExcludeFromPreset]
	[TrackClipType(typeof(LabelClip))]
	public class LabelTrack : TrackAsset
	{
		protected override void OnCreateClip(TimelineClip clip)
		{
		}

		public override UnityEngine.Playables.Playable CreateTrackMixer(UnityEngine.Playables.PlayableGraph graph, GameObject go, int inputCount)
		{
			UnityEngine.Playables.ScriptPlayable<LoopMixerBehaviour> playable = UnityEngine.Playables.ScriptPlayable<LoopMixerBehaviour>.Create(graph, inputCount);
			LoopMixerBehaviour behaviour = playable.GetBehaviour();

			behaviour.loopClips = new System.Collections.Generic.List<TimelineClip>();
			System.Collections.Generic.IEnumerable<TimelineClip> clips = GetClips();
			foreach(TimelineClip clip in clips)
			{
				if(clip.asset is LabelClipEX clipEx)
				{
					if(clipEx.wrapmode == LabelDirectorWrapMode.Loop)
                        behaviour.loopClips.Add(clip);
                }
			}
			behaviour.track = this;
            return playable;
		}
	}
}
