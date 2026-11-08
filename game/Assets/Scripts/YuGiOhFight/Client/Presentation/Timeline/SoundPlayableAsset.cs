namespace YuGiOhFight.Client.Presentation.Timeline
{
	public class SoundPlayableAsset : UnityEngine.Playables.PlayableAsset
	{
		public string startLabel;

		public bool mute;

		public bool skipDuplicate;

		public LabeledPlayableController LabeledPlayableController;

		public override double duration => 0.0;

		public override UnityEngine.Playables.Playable CreatePlayable(UnityEngine.Playables.PlayableGraph graph, UnityEngine.GameObject go)
		{
			UnityEngine.Playables.ScriptPlayable<SoundPlayableBehaviour> playable = UnityEngine.Playables.ScriptPlayable<SoundPlayableBehaviour>.Create(graph);
			SoundPlayableBehaviour behaviour = playable.GetBehaviour();
			behaviour.playableAsset = this;
			behaviour.startLabel = startLabel;
			behaviour.skipDuplicate = skipDuplicate;
			return playable;
		}
	}
}
