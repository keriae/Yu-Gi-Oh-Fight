using System;


namespace YuGiOhFight.Client.Presentation.Willow
{
	[Serializable]
	public class ActiveObjectTrack : UnityEngine.Timeline.TrackAsset
	{
		public const string kTrackName = "Active Object Track";

		public override UnityEngine.Playables.Playable CreateTrackMixer(UnityEngine.Playables.PlayableGraph graph, UnityEngine.GameObject go, int inputCount)
		{
            return UnityEngine.Playables.Playable.Create(graph, inputCount);
        }

		protected override void OnCreateClip(UnityEngine.Timeline.TimelineClip clip)
		{

		}

		private void ChangeDisplayName(UnityEngine.Timeline.TimelineClip clip)
		{
		}
	}
}
