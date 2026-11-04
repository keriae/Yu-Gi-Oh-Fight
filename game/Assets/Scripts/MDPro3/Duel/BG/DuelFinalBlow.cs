using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace MDPro3.Duel
{
    public class DuelFinalBlow : LoopTrackManager
    {
        public void Destroy()
        {
            StopLoop();
            Destroy(gameObject, 0.5f);
        }
    }
}