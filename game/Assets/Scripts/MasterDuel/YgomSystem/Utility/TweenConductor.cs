using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using YgomSystem.UI;

namespace YgomSystem.Utility
{
	internal class TweenConductor : MonoBehaviour
	{
		public bool playOnStart;

		public bool destroyOnFinished;

		[SerializeField]
		private GameObject target;

		[SerializeField]
		private List<string> labelList;

		[SerializeField]
		private UnityEvent<string> onStarted;

		[SerializeField]
		private UnityEvent<string> onFinished;

		[SerializeField]
		private UnityEvent onCompleted;

		private bool playing;

		private int currentLabelIndex;

		private string currentLabel;

		private List<Tween> tweens;

		public static TweenConductor Create(GameObject addComponentTarget, GameObject target, List<string> labelList, UnityAction<string> onStarted, UnityAction<string> onFinished, UnityAction onCompleted)
		{
            var conductor = addComponentTarget.AddComponent<TweenConductor>();
            conductor.target = target;
            conductor.labelList = labelList != null ? new List<string>(labelList) : new List<string>();
            if (onStarted != null)
            {
                if (conductor.onStarted == null)
                    conductor.onStarted = new UnityEvent<string>();
                conductor.onStarted.AddListener(onStarted);
            }
            if (onFinished != null)
            {
                if (conductor.onFinished == null)
                    conductor.onFinished = new UnityEvent<string>();
                conductor.onFinished.AddListener(onFinished);
            }
            if (onCompleted != null)
            {
                if (conductor.onCompleted == null)
                    conductor.onCompleted = new UnityEvent();
                conductor.onCompleted.AddListener(onCompleted);
            }
            return conductor;
        }

		private void OnDestroy()
		{
            if (tweens != null)
            {
                for (int i = 0; i < tweens.Count; i++)
                {
                    if (tweens[i] != null)
                        tweens[i].Stop();
                }
            }
        }

		private void Start()
		{
            if (playOnStart)
                PlayStart();
        }

		public void PlayStart()
		{
            currentLabelIndex = 0;
            playing = true;
            if (labelList != null && labelList.Count > 0)
                Play(labelList[0]);
            else
                playing = false;
        }

		private void Update()
		{
            if (!playing)
                return;
            if (string.IsNullOrEmpty(currentLabel))
                return;

            if (!IsPlaying(currentLabel))
            {
                onFinished?.Invoke(currentLabel);
                currentLabelIndex++;
                if (labelList != null && currentLabelIndex < labelList.Count)
                {
                    Play(labelList[currentLabelIndex]);
                }
                else
                {
                    playing = false;
                    onCompleted?.Invoke();
                    if (destroyOnFinished)
                        Destroy(gameObject);
                }
            }
        }

		private bool IsPlaying(string label)
		{
            if (target == null)
                return false;
            if (tweens == null)
            {
                tweens = new List<Tween>(target.GetComponentsInChildren<Tween>(true));
            }
            for (int i = 0; i < tweens.Count; i++)
            {
                var t = tweens[i];
                if (t != null && t.label == label && t.IsPlaying())
                    return true;
            }
            return false;
        }

		private void Play(string label)
		{
            currentLabel = label;
            if (target != null)
                Tween.TargetPlayLabel(target, label, true, true);
            onStarted?.Invoke(label);
        }
	}
}
