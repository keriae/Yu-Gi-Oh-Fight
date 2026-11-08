using System;


using UnityEngine.Playables;


namespace YuGiOhFight.Client.Presentation.Timeline
{
	[Serializable]
	public class LoopMixerBehaviour : PlayableBehaviour
	{
		private readonly double k_SecMargine;

		private PlayableDirector m_Director;

		[NonSerialized]
		public System.Collections.Generic.List<UnityEngine.Timeline.TimelineClip> loopClips;

		private bool m_Initialized;


		public LabelTrack track;

		UnityEngine.Timeline.TimelineClip currentClip;

		public override void OnPlayableCreate(Playable playable)
		{
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
		{
		}

		public override void OnBehaviourPause(Playable playable, FrameData info)
		{
		}

		public override void PrepareData(Playable playable, FrameData info)
		{
		}

		public override void ProcessFrame(Playable playable, FrameData info, object playerData)
		{
			if(!m_Initialized)
			{
				if(!inied)
					Ini(playable);

                if (m_Director.TryGetComponent<LabeledPlayableController>(out LabeledPlayableController lpc))
				{
                    lpc.loopMixerBehaviour = this;
                    m_Initialized = true;
                }
            }
        }

        private bool inied;
        private void Ini(Playable playable)
		{
            UnityEngine.IExposedPropertyTable resolver = playable.GetGraph().GetResolver();
            if (resolver is PlayableDirector)
                m_Director = (PlayableDirector)resolver;
			if(loopClips != null && loopClips.Count > 0)
			{
                currentClip = loopClips[0];
				foreach(UnityEngine.Timeline.TimelineClip clip in loopClips)
				{
					LoopClip loopClip = clip.asset as LoopClip;
					if( loopClip != null)
					{
						loopClip.loopClip = clip;
						loopClip.PassClip();
					}
				}
            }

			if(m_Director.TryGetComponent<Duel.LoopTrackManager>(out Duel.LoopTrackManager ltm))
			{
				ltm.loopMixerBehaviour = this;
            }

            inied = true;
        }

		public bool needLoop = true;

        public override void PrepareFrame(Playable playable, FrameData info)
		{
			if (currentClip != null && needLoop)
				if (m_Director.time > currentClip.extrapolatedStart + currentClip.duration - 0.02f)
					m_Director.time = currentClip.extrapolatedStart;
		}

		public void PlayClip(string label, UnityEngine.Timeline.TimelineClip loopClip)
		{
            System.Collections.Generic.IEnumerable<UnityEngine.Timeline.TimelineClip> clips = track.GetClips();
			foreach(UnityEngine.Timeline.TimelineClip clip in clips)
			{
                if (clip.asset is LabelClip)
                {
					if(clip.displayName == label)
					{
                        m_Director.time = clip.extrapolatedStart;
                        currentClip = loopClip;
                        break;
                    }
                }
            }
        }

        private void CheckLoopClip()
		{
		}

		private void SetNextClipDuration()
		{
		}

		private UnityEngine.Timeline.TimelineClip SearchCurrentClip()
		{
			return null;
		}

		private UnityEngine.Timeline.TimelineClip SearchNextClip()
		{
			return null;
		}
	}
}
