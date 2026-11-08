namespace YuGiOhFight.Client.Presentation.Timeline
{
	public class SoundStopPlayableAsset : UnityEngine.Playables.PlayableAsset
	{
		public string stopLabel;

		public float fade;

		public bool mute;

		public override double duration => 0.0;

		public override UnityEngine.Playables.Playable CreatePlayable(UnityEngine.Playables.PlayableGraph graph, UnityEngine.GameObject go)
		{
			return default(UnityEngine.Playables.Playable);
		}
	}
}
