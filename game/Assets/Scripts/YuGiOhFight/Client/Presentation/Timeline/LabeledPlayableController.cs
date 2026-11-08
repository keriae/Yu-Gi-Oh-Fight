using System.Runtime.CompilerServices;
using UnityEngine;


namespace YuGiOhFight.Client.Presentation.Timeline
{
	public class LabeledPlayableController : MonoBehaviour
	{
		private UnityEngine.Playables.PlayableDirector m_Director;

		private System.Collections.Generic.Dictionary<string, UnityEngine.Timeline.TimelineClip> m_TrackClips;

		private System.Collections.Generic.Dictionary<string, LabelDirectorWrapMode> m_LabelModeTable;

		private string[] m_ClipLabels;

		private UnityEngine.Playables.DirectorWrapMode m_WrapMode;

		[SerializeField]
		private LabelDirectorWrapMode m_LabelWrapMode;

		[SerializeField]
		private string m_PlayLabel;

		private double m_LastCheckedTime;

		public bool initialized;

		public UnityEngine.Playables.DirectorWrapMode wrapMode
		{
			get
			{
				return default(UnityEngine.Playables.DirectorWrapMode);
			}
			set
			{
			}
		}

		public LabelDirectorWrapMode labelWrapMode
		{
			get
			{
				return default(LabelDirectorWrapMode);
			}
			set
			{
			}
		}

		public UnityEngine.Playables.PlayState state => default(UnityEngine.Playables.PlayState);

		public string playLabel => null;

		public event System.Action<string, LabeledPlayableController> stopped
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public event System.Action<string, LabeledPlayableController> played
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public event System.Action<string, LabeledPlayableController> paused
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public LoopMixerBehaviour loopMixerBehaviour;

		public static LabeledPlayableController Create(UnityEngine.Playables.PlayableDirector target)
		{
			LabeledPlayableController controller = target.gameObject.AddComponent<LabeledPlayableController>();
			controller.m_Director = target;
			return controller;
		}

		private void Awake()
		{


			initialized = true;
		}

		public void OnCreatedLabelMixer(LabelMixerBehaviour labelMixer)
		{
		}

		public void Pause()
		{
		}

		public bool SetLabelClipExWrapMode(string label, LabelDirectorWrapMode mode)
		{
			return false;
		}

        public void PlayLabel(string label, UnityEngine.Timeline.TimelineClip loopClip)
        {
			loopMixerBehaviour.PlayClip(label, loopClip);
        }

        public void PlayLabel(string label)
		{

		}

		public void PlayLabel(string label, UnityEngine.Playables.PlayableAsset asset)
		{
		}

		public void PlayLabel(string label, LabelDirectorWrapMode mode)
		{
		}

		public void PlayLabel(string label, UnityEngine.Playables.PlayableAsset asset, LabelDirectorWrapMode mode)
		{
		}

		public bool PlayNextLabel()
		{
			return false;
		}

		public string SearchNextLabel()
		{
			return null;
		}

		public void Resume()
		{
		}

		public void Stop()
		{
		}

		public bool IsPlayingLabel(string label)
		{
			return false;
		}

		private void Update()
		{
		}

		private void OnStopped(UnityEngine.Playables.PlayableDirector director)
		{
		}

		private void OnPlayed(UnityEngine.Playables.PlayableDirector director)
		{
		}

		private void OnPaused(UnityEngine.Playables.PlayableDirector director)
		{
		}
	}
}
