namespace YuGiOhFight.Client.Presentation.Timeline
{
	public static class TimelineUtlity
	{
		public static UnityEngine.Timeline.TrackAsset GetTrackAsset(this UnityEngine.Playables.PlayableDirector pd, string tracklabel)
		{
			foreach(UnityEngine.Playables.PlayableBinding pb in pd.playableAsset.outputs)
			{
				UnityEngine.Timeline.TrackAsset track = pb.sourceObject as UnityEngine.Timeline.TrackAsset;
				if (track == null) continue;
				if(track.name == tracklabel)
					return track;
			}

			return null;
		}

		public static UnityEngine.Timeline.TimelineClip GetTimelineClip(this UnityEngine.Timeline.TrackAsset ta, string cliplabel)
		{
			return null;
		}

		public static UnityEngine.Timeline.TimelineClip GetTimelineClip(this UnityEngine.Playables.PlayableDirector pd, string tracklabel, string cliplabel)
		{
			return null;
		}

		public static UnityEngine.Timeline.TimelineClip GetTimelineClip(this UnityEngine.Playables.PlayableDirector pd, string cliplabel)
		{
			return null;
		}

		public static UnityEngine.Timeline.SignalEmitter GetSignalEmitter(this UnityEngine.Playables.PlayableDirector pd, string label)
		{
			return null;
		}

		public static T GetTrack<T>(this UnityEngine.Playables.PlayableDirector pd) where T : UnityEngine.Timeline.TrackAsset
		{
			return null;
		}

		public static System.Collections.Generic.List<T> GetTracks<T>(this UnityEngine.Playables.PlayableDirector pd) where T : UnityEngine.Timeline.TrackAsset
		{
			return null;
		}

		public static UnityEngine.Timeline.TrackAsset GetTrack(this UnityEngine.Playables.PlayableDirector pd, string label)
		{
			return null;
		}

		public static System.Collections.Generic.List<UnityEngine.Timeline.TrackAsset> GetTracks(this UnityEngine.Playables.PlayableDirector pd, string label)
		{
			return null;
		}

		public static T GetTrack<T>(this UnityEngine.Playables.PlayableDirector pd, string label) where T : UnityEngine.Timeline.TrackAsset
		{
			return null;
		}

		public static System.Collections.Generic.List<T> GetTracks<T>(this UnityEngine.Playables.PlayableDirector pd, string label) where T : UnityEngine.Timeline.TrackAsset
		{
			return null;
		}

		public static void SetSpeed(this UnityEngine.Playables.PlayableDirector pd, double speed)
		{
		}
	}
}
