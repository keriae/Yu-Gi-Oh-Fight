using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace YgomSystem.UI
{
	public abstract class Tween : MonoBehaviour
	{
		public enum Easing
		{
			Linear = 0,
			CubicIn = 1,
			CubicOut = 2,
			CubicInOut = 3,
			BackIn = 4,
			BackOut = 5,
			BackInOut = 6,
			BounceIn = 7,
			BounceOut = 8,
			BounceInOut = 9,
			Customize = 10,
			QuartIn = 11,
			QuartOut = 12,
			QuartInOut = 13
		}

		public static DG.Tweening.Ease GetDGTweenEase(Easing ease)
		{
			switch (ease)
			{
                case Easing.Linear: return DG.Tweening.Ease.Linear;
                case Easing.CubicIn: return DG.Tweening.Ease.InCubic;
                case Easing.CubicOut: return DG.Tweening.Ease.OutCubic;
                case Easing.CubicInOut: return DG.Tweening.Ease.InOutCubic;
                case Easing.BackIn: return DG.Tweening.Ease.InBack;
                case Easing.BackOut: return DG.Tweening.Ease.OutBack;
                case Easing.BackInOut: return DG.Tweening.Ease.InOutBack;
                case Easing.BounceIn: return DG.Tweening.Ease.InBounce;
                case Easing.BounceOut: return DG.Tweening.Ease.OutBounce;
                case Easing.BounceInOut: return DG.Tweening.Ease.InOutBounce;
                case Easing.QuartIn: return DG.Tweening.Ease.InQuart;
                case Easing.QuartOut: return DG.Tweening.Ease.OutQuart;
                case Easing.QuartInOut: return DG.Tweening.Ease.InOutQuart;
                default: return DG.Tweening.Ease.Linear;
            }
        }

        public enum Style
		{
			Once = 0,
			Loop = 1,
			PingPong = 2,
			PingPongLoop = 3,
			SyncLoop = 4,
			SyncPingPongLoop = 5
		}

		private static readonly float FRAMERATE_LIMIT = 0.05f;

		public string label;

		[SerializeField]
		public Easing easing;

		[SerializeField]
		public Style style;

		[SecField]
		[SerializeField]
		public float duration;

		[SerializeField]
		[SecField]
		public float setupWait;

		[SecField]
		[SerializeField]
		public float startDelay;

		[SerializeField]
		public bool ignoreTimeScale;

		[SerializeField]
		public UnityEvent onFinished;

		[SerializeField]
		public bool callOnFinishedDestroy;

		[HideInInspector]
		public AnimationCurve curve;

		protected float timeDelta;

		protected float crntTime;

		private bool isCaptured;

		private float setupWaitCount;

		private bool isExecFinished;

		private static float BounceOut(float k)
		{
            if (k < (1 / 2.75f))
                return 7.5625f * k * k;
            else if (k < (2 / 2.75f))
                return 7.5625f * (k -= (1.5f / 2.75f)) * k + 0.75f;
            else if (k < (2.5 / 2.75))
                return 7.5625f * (k -= (2.25f / 2.75f)) * k + 0.9375f;
            else
                return 7.5625f * (k -= (2.625f / 2.75f)) * k + 0.984375f;
        }

		private static float BounceIn(float k)
		{
            return 1 - BounceOut(1 - k);
        }

		public static float EasingValue(float k, Easing e)
		{
            switch (e)
            {
                case Easing.Linear: return k;
                case Easing.CubicIn: return k * k * k;
                case Easing.CubicOut: return 1 - Mathf.Pow(1 - k, 3);
                case Easing.CubicInOut: return k < 0.5 ? 4 * k * k * k : 1 - Mathf.Pow(-2 * k + 2, 3) / 2;
                case Easing.BackIn: return k * k * ((1.70158f + 1) * k - 1.70158f);
                case Easing.BackOut: return 1 + (--k) * k * ((1.70158f + 1) * k + 1.70158f);
                case Easing.BackInOut:
                    float s = 1.70158f * 1.525f;
                    return (k *= 2) < 1 ?
                        0.5f * (k * k * ((s + 1) * k - s)) :
                        0.5f * ((k -= 2) * k * ((s + 1) * k + s) + 2);
                case Easing.BounceIn: return BounceIn(k);
                case Easing.BounceOut: return BounceOut(k);
                case Easing.BounceInOut: return k < 0.5f ? BounceIn(k * 2) * 0.5f : BounceOut(k * 2 - 1) * 0.5f + 0.5f;
                case Easing.QuartIn: return k * k * k * k;
                case Easing.QuartOut: return 1 - Mathf.Pow(1 - k, 4);
                case Easing.QuartInOut: return k < 0.5 ? 8 * k * k * k * k : 1 - Mathf.Pow(-2 * k + 2, 4) / 2;
                default: return k;
            }
        }

		private float GetEasing(float k)
		{
            return easing == Easing.Customize ? curve.Evaluate(k) : EasingValue(k, easing);
        }

		protected virtual void CaptureAwake()
		{
		}

		protected virtual void CaptureFrom()
		{
		}

		protected abstract void OnSetValue(float par);

		private void Awake()
		{
            CaptureAwake();
        }

		private void ExecSetup()
		{
            if (isCaptured)
                return;
            CaptureFrom();
            isCaptured = true;
        }

		private void ExecPlay(float time, bool forceUpdate = false)
		{
            crntTime = time;
            float t = duration > 0 ? crntTime / duration : 1f;

            // 处理播放风格
            switch (style)
            {
                case Style.Once:
                    t = Mathf.Clamp01(t);
                    break;
                case Style.Loop:
                case Style.SyncLoop:
                    t %= 1f;
                    break;
                case Style.PingPong:
                case Style.PingPongLoop:
                case Style.SyncPingPongLoop:
                    t = Mathf.PingPong(t, 1f);
                    break;
            }

            OnSetValue(GetEasing(t));

            // 完成判定：PingPong 单次往返需要 2 倍时长
            bool isComplete = style == Style.PingPong
                ? crntTime >= duration * 2f
                : crntTime >= duration;

            if (isComplete)
            {
                switch (style)
                {
                    case Style.Once:
                    case Style.PingPong:
                        isExecFinished = true;
                        enabled = false;
                        onFinished?.Invoke();
                        if (callOnFinishedDestroy) DestroySelf();
                        break;
                    case Style.Loop:
                        ResetWithTimeDelta();
                        break;
                    // Loop(PingPong)/Sync 系列持续运行，不做完成处理
                }
            }
        }

		private void OnDestroy()
		{
            Stop();
        }

		private void Start()
		{
		}

		private void Update()
		{
            if (isExecFinished)
                return;

            if (setupWaitCount > 0f)
            {
                setupWaitCount -= ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
                return;
            }

            if (!isCaptured)
                ExecSetup();

            float delta = ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
            if (delta > FRAMERATE_LIMIT)
                delta = FRAMERATE_LIMIT;

            timeDelta += delta;
            ExecPlay(timeDelta);
        }

		public void Play()
		{
            setupWaitCount = setupWait + startDelay;
            if (setupWaitCount <= 0f && !isCaptured)
                ExecSetup();
            timeDelta = 0f;
            crntTime = 0f;
            isExecFinished = false;
            enabled = true;
        }

		public void Pause()
		{
            enabled = false;
        }

		public void Stop()
		{
            enabled = false;
            isExecFinished = true;
        }

		public void End()
		{
            if (isExecFinished)
                return;
            if (!isCaptured)
                ExecSetup();
            isExecFinished = true;
            enabled = false;
            OnSetValue(GetEasing(1f));
            onFinished?.Invoke();
            if (callOnFinishedDestroy) DestroySelf();
        }

		public void Reset()
		{
            isCaptured = false;
            isExecFinished = false;
            timeDelta = 0f;
            crntTime = 0f;
            ExecSetup();
            OnSetValue(GetEasing(0f));
            enabled = false;
        }

		public void ResetWithTimeDelta()
		{
            crntTime = 0;
            timeDelta = 0;
        }

		public void GotoAndPlay(float time)
		{
            if (!isCaptured)
                ExecSetup();
            timeDelta = time;
            crntTime = time;
            isExecFinished = false;
            enabled = true;
            ExecPlay(time, true);
        }

		public void GotoAndPause(float time)
		{
            if (!isCaptured)
                ExecSetup();
            timeDelta = time;
            crntTime = time;
            ExecPlay(time, true);
            enabled = false;
        }

		public void DestroySelf()
		{
            Destroy(gameObject);
        }

		public void PlayLabel(string _label)
		{
            TargetPlayLabel(gameObject, _label, false, false);
        }

		public bool IsPlaying(string _label = "", bool isActive = false)
		{
            if (!string.IsNullOrEmpty(_label) && label != _label)
                return false;
            if (isActive && !gameObject.activeInHierarchy)
                return false;
            return !isExecFinished && enabled;
        }

		public bool IsFinished()
		{
            return isExecFinished;
        }

		public static void TargetPlayLabel(GameObject target, string _label = "", bool includeChildren = false, bool wakeup = false)
		{
            var tweens = GetTweenTarget(target, _label, includeChildren);
            for (int i = 0; i < tweens.Count; i++)
            {
                if (wakeup)
                    TargetCaptureFrom(tweens[i], true);
                tweens[i].Play();
            }
        }

		public static bool TargetIsPlaying(GameObject target, string _label = "", bool includeChildren = false, bool isActive = false)
		{
            var tweens = GetTweenTarget(target, _label, includeChildren);
            for (int i = 0; i < tweens.Count; i++)
            {
                if (tweens[i].IsPlaying("", isActive))
                    return true;
            }
            return false;
        }

		public static void TargetGotoAndPlayLabel(GameObject target, float time, string _label = "", bool includeChildren = false, bool wakeup = false)
		{
            var tweens = GetTweenTarget(target, _label, includeChildren);
            for (int i = 0; i < tweens.Count; i++)
            {
                if (wakeup)
                    TargetCaptureFrom(tweens[i], true);
                tweens[i].GotoAndPlay(time);
            }
        }

		public static void TargetGotoAndPauseLabel(GameObject target, float time, string _label = "", bool includeChildren = false, bool wakeup = false)
		{
            var tweens = GetTweenTarget(target, _label, includeChildren);
            for (int i = 0; i < tweens.Count; i++)
            {
                if (wakeup)
                    TargetCaptureFrom(tweens[i], true);
                tweens[i].GotoAndPause(time);
            }
        }

		public static void TargetPauseLabel(GameObject target, string _label = "", bool includeChildren = false)
		{
            var tweens = GetTweenTarget(target, _label, includeChildren);
            for (int i = 0; i < tweens.Count; i++)
                tweens[i].Pause();
        }

		public static void TargetStopLabel(GameObject target, string _label = "", bool includeChildren = false, string exlabel = "")
		{
            var tweens = GetTweenTarget(target, _label, includeChildren);
            for (int i = 0; i < tweens.Count; i++)
                tweens[i].Stop();
            if (!string.IsNullOrEmpty(exlabel))
            {
                var exTweens = GetTweenTarget(target, exlabel, includeChildren);
                for (int i = 0; i < exTweens.Count; i++)
                    exTweens[i].Stop();
            }
        }

		public static void TargetEndLabel(GameObject target, string _label = "", bool includeChildren = false)
		{
            var tweens = GetTweenTarget(target, _label, includeChildren);
            for (int i = 0; i < tweens.Count; i++)
                tweens[i].End();
        }

		public static void TargetForwardLabel(GameObject target, float sec, string _label = "", bool includeChildren = false)
		{
            var tweens = GetTweenTarget(target, _label, includeChildren);
            for (int i = 0; i < tweens.Count; i++)
            {
                tweens[i].timeDelta += sec;
                tweens[i].ExecPlay(tweens[i].timeDelta);
            }
        }

		public static void TargetCaptureFrom(GameObject target, string _label = "", bool includeChildren = false, bool force = false)
		{
            var tweens = GetTweenTarget(target, _label, includeChildren);
            for (int i = 0; i < tweens.Count; i++)
                TargetCaptureFrom(tweens[i], force);
        }

		public static void TargetCaptureFrom(Tween tween, bool force)
		{
            if (force || !tween.isCaptured)
                tween.ExecSetup();
        }

		public static void AllPlayLabel(string label)
		{
            var tweens = GetTweenAll(label);
            for (int i = 0; i < tweens.Count; i++)
                tweens[i].Play();
        }

		public static void AllStopLabel(string label)
		{
            var tweens = GetTweenAll(label);
            for (int i = 0; i < tweens.Count; i++)
                tweens[i].Stop();
        }

		public static void AllPauseLabel(string label)
		{
            var tweens = GetTweenAll(label);
            for (int i = 0; i < tweens.Count; i++)
                tweens[i].Pause();
        }

		public static void AllEndLabel(string label)
		{
            var tweens = GetTweenAll(label);
            for (int i = 0; i < tweens.Count; i++)
                tweens[i].End();
        }

		public static List<Tween> GetTweenTarget(GameObject target, string _label = "", bool includeChildren = false)
		{
            List<Tween> result = new List<Tween>();
            Tween[] tweens = includeChildren ? target.GetComponentsInChildren<Tween>(true) : target.GetComponents<Tween>();

            foreach (Tween t in tweens)
            {
                if (string.IsNullOrEmpty(_label) || t.label == _label)
                    result.Add(t);
            }
            return result;
        }

		public static List<Tween> GetTweenAll(string label)
		{
            List<Tween> result = new List<Tween>();
            var all = FindObjectsByType<Tween>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].label == label)
                    result.Add(all[i]);
            }
            return result;
        }
	}
}
