using System.Runtime.CompilerServices;


namespace YuGiOhFight.Client.Presentation.Timeline
{
	public class TimelineObject : UnityEngine.MonoBehaviour
	{
		public TimelineManager.EndEventType endEventType;

		public UnityEngine.Events.UnityAction onDestroy;

		protected UnityEngine.Playables.PlayableDirector m_PlayableDirector;

		protected System.Collections.Generic.Queue<UnityEngine.Events.UnityAction<UnityEngine.Playables.PlayableDirector>> onStopQueue;

		protected System.Collections.Generic.Queue<UnityEngine.Events.UnityAction<UnityEngine.Playables.PlayableDirector>> onPlayQueue;

		protected System.Collections.Generic.Queue<UnityEngine.Events.UnityAction<UnityEngine.Playables.PlayableDirector>> onPauseQueue;

		public UnityEngine.Playables.PlayState state => default(UnityEngine.Playables.PlayState);

		public double time => 0.0;

		public string path
		{
			[CompilerGenerated]
			get
			{
				return null;
			}
			[CompilerGenerated]
			private set
			{
			}
		}

		public string group
		{
			[CompilerGenerated]
			get
			{
				return null;
			}
			[CompilerGenerated]
			private set
			{
			}
		}

		public UnityEngine.Playables.PlayableDirector playableDirector => null;

		public static TimelineObject CreateTimelineObject(UnityEngine.GameObject gob, string path, string group)
		{
			return null;
		}

		public void AddOnStopCallBack(UnityEngine.Events.UnityAction<UnityEngine.Playables.PlayableDirector> onstop)
		{
		}

		public void AddOnPlayCallback(UnityEngine.Events.UnityAction<UnityEngine.Playables.PlayableDirector> onplay)
		{
		}

		public void AddOnPauseCallback(UnityEngine.Events.UnityAction<UnityEngine.Playables.PlayableDirector> onpause)
		{
		}

		private void OnPlayableDirectorStop(UnityEngine.Playables.PlayableDirector pd)
		{
		}

		public void Play()
		{
		}

		public void Stop()
		{
		}

		public void Pause()
		{
		}

		public void Resume()
		{
		}

		public void Evaluate()
		{
		}

		public void SkipTo(float target, float start = 0f)
		{
		}

		public LabeledPlayableController GetLabeledPlayableController()
		{
			return null;
		}

		public void Destroy()
		{
		}

		public void Recycle()
		{
		}

		public void OnCached()
		{
		}

		private void OnPlayableDirectorPlay(UnityEngine.Playables.PlayableDirector pd)
		{
		}

		private void OnPlayableDirectorPause(UnityEngine.Playables.PlayableDirector pd)
		{
		}

		private void ResetEventQueue(System.Collections.Generic.Queue<UnityEngine.Events.UnityAction<UnityEngine.Playables.PlayableDirector>> eventQueue)
		{
		}

		private void OnDestroy()
		{
		}
	}
}
