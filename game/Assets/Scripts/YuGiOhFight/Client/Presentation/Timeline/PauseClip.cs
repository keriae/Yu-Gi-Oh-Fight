using System;


namespace YuGiOhFight.Client.Presentation.Timeline
{
	[Serializable]
	public class PauseClip : UnityEngine.Playables.PlayableAsset
	{
		public PauseBehaviour template;

		[NonSerialized]
		public double startTime;

		[NonSerialized]
		public double endTime;

		public override UnityEngine.Playables.Playable CreatePlayable(UnityEngine.Playables.PlayableGraph graph, UnityEngine.GameObject go)
		{
			return default(UnityEngine.Playables.Playable);
		}
	}
}
