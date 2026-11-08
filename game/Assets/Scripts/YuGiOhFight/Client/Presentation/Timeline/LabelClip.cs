using System;


namespace YuGiOhFight.Client.Presentation.Timeline
{
	[Serializable]
	public class LabelClip : UnityEngine.Playables.PlayableAsset
	{
		public LabelBehaviour template;

		public override UnityEngine.Playables.Playable CreatePlayable(UnityEngine.Playables.PlayableGraph graph, UnityEngine.GameObject go)
		{
			UnityEngine.Playables.ScriptPlayable<LabelBehaviour> playable = UnityEngine.Playables.ScriptPlayable<LabelBehaviour>.Create(graph);
			template = playable.GetBehaviour();
			return playable;
		}
	}
}
