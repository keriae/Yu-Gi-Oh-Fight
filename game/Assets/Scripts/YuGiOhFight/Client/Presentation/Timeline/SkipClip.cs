using System;


namespace YuGiOhFight.Client.Presentation.Timeline
{
	[Serializable]
	public class SkipClip : UnityEngine.Playables.PlayableAsset
	{
		public SkipBehaviour template;

		[NonSerialized]
		public double endTime;

		public override UnityEngine.Playables.Playable CreatePlayable(UnityEngine.Playables.PlayableGraph graph, UnityEngine.GameObject go)
		{
			return default(UnityEngine.Playables.Playable);
		}
	}
}
