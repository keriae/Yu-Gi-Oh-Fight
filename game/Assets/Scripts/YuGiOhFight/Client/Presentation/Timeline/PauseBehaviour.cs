using System;


namespace YuGiOhFight.Client.Presentation.Timeline
{
	[Serializable]
	public class PauseBehaviour : UnityEngine.Playables.PlayableBehaviour
	{
		private UnityEngine.Playables.PlayableDirector m_Director;

		[NonSerialized]
		public double startTime;

		[NonSerialized]
		public double endTime;

		private bool m_Ready;

		private bool m_IsCompletePause;

		private bool m_IsCompleteResume;

		public override void OnPlayableCreate(UnityEngine.Playables.Playable playable)
		{
		}

		public override void OnGraphStart(UnityEngine.Playables.Playable playable)
		{
		}

		public override void OnGraphStop(UnityEngine.Playables.Playable playable)
		{
		}

		public override void OnBehaviourPlay(UnityEngine.Playables.Playable playable, UnityEngine.Playables.FrameData info)
		{
		}

		public override void OnBehaviourPause(UnityEngine.Playables.Playable playable, UnityEngine.Playables.FrameData info)
		{
		}

		public override void PrepareFrame(UnityEngine.Playables.Playable playable, UnityEngine.Playables.FrameData info)
		{
		}

		public override void OnPlayableDestroy(UnityEngine.Playables.Playable playable)
		{
		}
	}
}
