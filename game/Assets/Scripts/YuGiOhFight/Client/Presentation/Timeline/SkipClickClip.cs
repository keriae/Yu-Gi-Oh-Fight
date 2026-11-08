using System;


namespace YuGiOhFight.Client.Presentation.Timeline
{
	[Serializable]
	public class SkipClickClip : UnityEngine.Playables.PlayableAsset
	{
		public SkipClickBehaviour template;

		[NonSerialized]
		public double endTime;

		[NonSerialized]
		public UI.SelectionButton waitButton;

		public override UnityEngine.Playables.Playable CreatePlayable(UnityEngine.Playables.PlayableGraph graph, UnityEngine.GameObject go)
		{
			return default(UnityEngine.Playables.Playable);
		}
	}
}
