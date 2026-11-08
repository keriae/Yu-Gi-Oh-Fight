using UnityEngine;
using UnityEngine.Playables;


namespace YuGiOhFight.Client.Duel
{
    public class LoopTrackManager : MonoBehaviour
    {
        private PlayableDirector playableDirector;
        private PlayableDirector PlayableDirector =>
            playableDirector = playableDirector != null ? playableDirector
            : GetComponent<PlayableDirector>();

        public Presentation.Timeline.LoopMixerBehaviour loopMixerBehaviour;
        public Presentation.Timeline.LoopBehaviour loopBehaviour;

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