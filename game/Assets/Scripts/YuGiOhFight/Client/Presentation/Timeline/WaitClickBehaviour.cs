using System;


namespace YuGiOhFight.Client.Presentation.Timeline
{
	[Serializable]
	public class WaitClickBehaviour : UnityEngine.Playables.PlayableBehaviour
	{
		private UnityEngine.Playables.PlayableDirector m_Director;

		private bool m_WaitTrigger;

		private bool m_Waited;

		private bool m_IsLooping;

		public bool isLoop;

		[NonSerialized]
		public double startTime;

		[NonSerialized]
		public double endTime;

		[NonSerialized]
		public UI.SelectionButton waitButton;

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

		public void OnClicked()
		{
		}
	}
}
