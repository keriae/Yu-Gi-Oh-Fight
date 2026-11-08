using DG.Tweening;


using UnityEngine.Playables;


namespace YuGiOhFight.Client.Presentation.Timeline
{
	public class EventPlayableBehaviour : PlayableBehaviour
	{
		public class EventInfo
		{
			public string label;

			public double time;

			public bool isDone;
		}

		public System.Collections.Generic.List<EventInfo> eventList;

		public double startTime;

		private bool processed;

		//DIY
        public string label;
        PlayableDirector director;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
		{
            PlayContent();
            foreach (EventInfo e in eventList)
            {
                if (e.label == "WinStart" && !e.isDone)
                {
                    e.isDone = true;
                    DOTween.To(v => { }, 0, 0, (float)e.time).OnComplete(() =>
                    {
                        Controllers.OcgCore.endingAction?.Invoke();
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
                Controllers.OcgCore.startCard?.Invoke();
            }
            else if (label == "StrongSummon")
            {
                if (Runtime.Program.instance == null)
                    return;
                if (Runtime.Program.instance.currentServant != Runtime.Program.instance.ocgcore)
                    return;
                if (Controllers.OcgCore.summonCard == null)
                    return;
                int code = Controllers.OcgCore.summonCard.GetData().Id;
                if (Controllers.CutinViewer.CutinExist(code))
                    _ = Controllers.CutinViewer.Play(code, (int)Controllers.OcgCore.summonCard.p.controller);
            }
            else if(label == "Next")
            {
                Controllers.OcgCore.nextEventAction?.Invoke();
            }
        }

    }
}
