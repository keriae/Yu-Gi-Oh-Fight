using System;
using UnityEngine;


namespace YuGiOhFight.Client.Presentation.Timeline
{
	[Serializable]
	[ExcludeFromPreset]
	public class PauseTrack : UnityEngine.Timeline.TrackAsset
	{
		protected override void OnCreateClip(UnityEngine.Timeline.TimelineClip clip)
		{
		}

		public override UnityEngine.Playables.Playable CreateTrackMixer(UnityEngine.Playables.PlayableGraph graph, GameObject go, int inputCount)
		{
			return default(UnityEngine.Playables.Playable);
		}
	}
}
