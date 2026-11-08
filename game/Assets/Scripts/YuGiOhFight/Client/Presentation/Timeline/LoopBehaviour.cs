using System;
using UnityEngine.Playables;


namespace YuGiOhFight.Client.Presentation.Timeline
{
	[Serializable]
	public class LoopBehaviour : PlayableBehaviour
	{
        PlayableDirector m_director;
        public UnityEngine.Timeline.TimelineClip loopClip;

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            if (m_director == null)
                m_director = playable.GetGraph().GetResolver() as PlayableDirector;
            if (loopClip != null && m_director != null)
                if (m_director.time > loopClip.extrapolatedStart + loopClip.duration)
                    m_director.time = loopClip.extrapolatedStart;
        }
    }
}
