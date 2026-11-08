using System;


namespace YuGiOhFight.Client.Presentation.Timeline
{
	[Serializable]
	public class SkipClickBehaviour : UnityEngine.Playables.PlayableBehaviour
	{
		private UnityEngine.Playables.PlayableDirector m_Director;

		[NonSerialized]
		public double endTime;

		[NonSerialized]
		public UI.SelectionButton waitButton;

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

		public void OnClicked()
		{
		}
	}
}
