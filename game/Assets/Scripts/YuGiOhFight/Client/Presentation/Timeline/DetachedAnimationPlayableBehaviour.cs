using System;


namespace YuGiOhFight.Client.Presentation.Timeline
{
	[Serializable]
	public class DetachedAnimationPlayableBehaviour : UnityEngine.Playables.PlayableBehaviour
	{
		public UnityEngine.AnimationClip animClip;

		[NonSerialized]
		public UnityEngine.Animator target;

		private UnityEngine.Playables.PlayableGraph m_PlayableGraph;

		public override void OnPlayableCreate(UnityEngine.Playables.Playable playable)
		{
		}

		public override void OnBehaviourPlay(UnityEngine.Playables.Playable playable, UnityEngine.Playables.FrameData info)
		{
		}

		public override void OnBehaviourPause(UnityEngine.Playables.Playable playable, UnityEngine.Playables.FrameData info)
		{
		}

		public override void OnPlayableDestroy(UnityEngine.Playables.Playable playable)
		{
		}
	}
}
