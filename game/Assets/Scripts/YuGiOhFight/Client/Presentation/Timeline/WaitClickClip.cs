using System;


namespace YuGiOhFight.Client.Presentation.Timeline
{
	[Serializable]
	public class WaitClickClip : UnityEngine.Playables.PlayableAsset
	{
		public WaitClickBehaviour template;

		[NonSerialized]
		public double startTime;

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
