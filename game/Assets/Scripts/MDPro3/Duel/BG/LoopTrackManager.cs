using UnityEngine;
using UnityEngine.Playables;
using YgomSystem.Timeline;

namespace MDPro3.Duel
{
    public class LoopTrackManager : MonoBehaviour
    {
        private PlayableDirector playableDirector;
        private PlayableDirector PlayableDirector =>
            playableDirector = playableDirector != null ? playableDirector
            : GetComponent<PlayableDirector>();

        public LoopMixerBehaviour loopMixerBehaviour;
        public LoopBehaviour loopBehaviour;

        public void StopLoop()
        {
            if(loopMixerBehaviour != null)
            {
                loopMixerBehaviour.needLoop = false;
            }

            if(loopBehaviour != null)
            {
                loopBehaviour.loopClip = null;
                loopBehaviour = null;
            }
        }
    }
}