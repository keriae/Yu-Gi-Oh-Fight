using System;
using System.Collections.Generic;
using UnityEngine;
using YgomSystem.UI;

namespace YgomSystem.Utility
{
	public class TweenContainer
	{
		private List<Tween> tweens;

		private Action onPlayFinished;

		private string playingLabel;

		private bool playing;

		public void Setup(GameObject target)
		{
            tweens = new List<Tween>();
            if (target == null)
                return;
            var found = target.GetComponentsInChildren<Tween>(true);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null)
                    tweens.Add(found[i]);
            }
        }

		public void AddTweens(Tween[] tweens)
		{
            if (this.tweens == null)
                this.tweens = new List<Tween>();
            if (tweens == null)
                return;
            for (int i = 0; i < tweens.Length; i++)
            {
                if (tweens[i] != null && !this.tweens.Contains(tweens[i]))
                    this.tweens.Add(tweens[i]);
            }
        }

		public void Play(string playLabel, Action onPlayFinished, bool stop = true, string stopLabel = "")
		{
            if (stop)
                Stop(stopLabel);

            this.onPlayFinished = onPlayFinished;
            playingLabel = playLabel;
            playing = true;

            if (tweens == null)
                return;
            for (int i = 0; i < tweens.Count; i++)
            {
                var t = tweens[i];
                if (t != null && t.label == playLabel)
                    t.Play();
            }
        }

		public void Stop(string label)
		{
            if (tweens == null)
                return;
            for (int i = 0; i < tweens.Count; i++)
            {
                var t = tweens[i];
                if (t != null && (string.IsNullOrEmpty(label) || t.label == label))
                    t.Stop();
            }
            if (string.IsNullOrEmpty(label) || label == playingLabel)
                playing = false;
        }

		private bool IsPlaying(string label = null)
		{
            if (tweens == null)
                return false;
            for (int i = 0; i < tweens.Count; i++)
            {
                var t = tweens[i];
                if (t == null)
                    continue;
                if (!string.IsNullOrEmpty(label) && t.label != label)
                    continue;
                if (t.IsPlaying())
                    return true;
            }
            return false;
        }

		public bool Update()
		{
            if (!playing)
                return false;
            if (IsPlaying(playingLabel))
                return false;

            playing = false;
            InvokeOnPlayFinished();
            return true;
        }

		public void Immediate(string label)
		{
            if (tweens == null)
                return;
            for (int i = 0; i < tweens.Count; i++)
            {
                var t = tweens[i];
                if (t != null && (string.IsNullOrEmpty(label) || t.label == label))
                    t.End();
            }
        }

		public void CaptureFrom(string label = null)
		{
            if (tweens == null)
                return;
            for (int i = 0; i < tweens.Count; i++)
            {
                var t = tweens[i];
                if (t == null)
                    continue;
                if (!string.IsNullOrEmpty(label) && t.label != label)
                    continue;
                Tween.TargetCaptureFrom(t, false);
            }
        }

		private void InvokeOnPlayFinished()
		{
            var cb = onPlayFinished;
            onPlayFinished = null;
            cb?.Invoke();
        }
	}
}
