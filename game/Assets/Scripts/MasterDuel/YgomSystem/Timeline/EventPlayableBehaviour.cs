using DG.Tweening;
using MDPro3;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace YgomSystem.Timeline
{
	public class EventPlayableBehaviour : PlayableBehaviour
	{
		public class EventInfo
		{
			public string label;

			public double time;

			public bool isDone;
		}

		public List<EventInfo> eventList;

		public double startTime;

		private bool processed;

		//DIY
        public string label;
        PlayableDirector director;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
		{
            PlayContent();
            foreach (var e in eventList)
            {
                if (e.label == "WinStart" && !e.isDone)
                {
                    e.isDone = true;
                    DOTween.To(v => { }, 0, 0, (float)e.time).OnComplete(() =>
                    {
                        OcgCore.endingAction?.Invoke();
                    });
                }
            }
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
		{
            if(playable.GetPlayState() == PlayState.Playing)
                PlayContent();
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
		{
        }

		private void CheckEventInfos(Playable playable)
		{
		}

		bool played = false;
		private void PlayContent()
		{
			if(played)
				return;
			played = true;
            if (label == "StartCard")
            {
                OcgCore.startCard?.Invoke();
            }
            else if (label == "StrongSummon")
            {
                if (Program.instance == null)
                    return;
                if (Program.instance.currentServant != Program.instance.ocgcore)
                    return;
                if (OcgCore.summonCard == null)
                    return;
                var code = OcgCore.summonCard.GetData().Id;
                if (CutinViewer.CutinExist(code))
                    _ = CutinViewer.Play(code, (int)OcgCore.summonCard.p.controller);
            }
            else if(label == "Next")
            {
                OcgCore.nextEventAction?.Invoke();
            }
        }

    }
}
