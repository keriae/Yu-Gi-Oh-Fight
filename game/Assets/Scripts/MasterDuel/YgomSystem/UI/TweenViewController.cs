using UnityEngine;

namespace YgomSystem.UI
{
	public class TweenViewController : ViewController
	{
		public string PushLabel;

		public string PopLabel;

		public string CoverLabel;

		public string UncoverLabel;

		public string SwapInLabel;

		public string SwapOutLabel;

		private string m_LastTweenLabel;

		protected virtual GameObject m_TweenTarget => gameObject;

		protected string k_SwapLabelAlphaFade => "AlphaFade";

		protected string k_SwapLabelFlip => "Flip";

		protected void AddLabelSuffix(string suffix)
		{
            PushLabel += suffix;
            PopLabel += suffix;
            CoverLabel += suffix;
            UncoverLabel += suffix;
            SwapInLabel += suffix;
            SwapOutLabel += suffix;
        }

		public string GetTransitionType2Label(TransitionType type)
		{
            switch (type)
            {
                case TransitionType.Push: return PushLabel;
                case TransitionType.Pop: return PopLabel;
                case TransitionType.Cover: return CoverLabel;
                case TransitionType.Uncover: return UncoverLabel;
                case TransitionType.SwapIn: return SwapInLabel;
                case TransitionType.SwapOut: return SwapOutLabel;
                default: return null;
            }
        }

		public override void TransitionStart(TransitionType type)
		{
            var label = GetTransitionType2Label(type);
            if (string.IsNullOrEmpty(label))
                return;
            m_LastTweenLabel = label;
            var target = m_TweenTarget;
            if (target != null)
                Tween.TargetPlayLabel(target, label, true, true);
        }

		public override bool TransitionUpdate(TransitionType type)
		{
            if (string.IsNullOrEmpty(m_LastTweenLabel))
                return true;
            var target = m_TweenTarget;
            if (target == null)
                return true;
            return !Tween.TargetIsPlaying(target, m_LastTweenLabel, true);
        }
	}
}
