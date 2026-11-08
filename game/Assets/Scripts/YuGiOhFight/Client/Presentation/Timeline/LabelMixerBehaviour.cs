using System;


namespace YuGiOhFight.Client.Presentation.Timeline
{
	[Serializable]
	public class LabelMixerBehaviour : UnityEngine.Playables.PlayableBehaviour
	{
		private UnityEngine.Playables.PlayableDirector m_Director;

		private System.Collections.Generic.Dictionary<string, UnityEngine.Timeline.TimelineClip> m_TrackClips;

		public System.Collections.Generic.IReadOnlyDictionary<string, UnityEngine.Timeline.TimelineClip> trackClips => null;

		public void Initialize(System.Collections.Generic.IReadOnlyList<UnityEngine.Timeline.TimelineClip> trackClips)
		{
		}

		public override void OnPlayableCreate(UnityEngine.Playables.Playable playable)
		{

		}
	}
}
