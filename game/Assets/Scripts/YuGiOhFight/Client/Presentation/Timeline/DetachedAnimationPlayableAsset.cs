namespace YuGiOhFight.Client.Presentation.Timeline
{
	public class DetachedAnimationPlayableAsset : UnityEngine.Playables.PlayableAsset
	{
		public DetachedAnimationPlayableBehaviour template;

		public override UnityEngine.Playables.Playable CreatePlayable(UnityEngine.Playables.PlayableGraph graph, UnityEngine.GameObject go)
		{
			return default(UnityEngine.Playables.Playable);
		}
	}
}
