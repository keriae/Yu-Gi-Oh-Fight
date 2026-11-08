using System;


namespace YuGiOhFight.Client.Presentation.Timeline
{
	[Serializable]
	public class LabelClipEX : LabelClip
	{
		public bool overrideWrapMode;

		public LabelDirectorWrapMode wrapmode;

		public override UnityEngine.Playables.Playable CreatePlayable(UnityEngine.Playables.PlayableGraph graph, UnityEngine.GameObject go)
		{
            UnityEngine.Playables.ScriptPlayable<LabelBehaviour> playable = UnityEngine.Playables.ScriptPlayable<LabelBehaviour>.Create(graph);
            template = playable.GetBehaviour();
            return playable;
        }
    }
}
