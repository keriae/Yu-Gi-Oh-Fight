using UnityEngine;


namespace YuGiOhFight.Client.Presentation.Tweens
{
	internal class TweenConductor : MonoBehaviour
	{
		public bool playOnStart;

		public bool destroyOnFinished;

		[SerializeField]
		private GameObject target;

		[SerializeField]
		private System.Collections.Generic.List<string> labelList;

		[SerializeField]
		private UnityEngine.Events.UnityEvent<string> onStarted;

		[SerializeField]
		private UnityEngine.Events.UnityEvent<string> onFinished;

		[SerializeField]
		private UnityEngine.Events.UnityEvent onCompleted;

		private bool playing;

		private int currentLabelIndex;

		private string currentLabel;

		private System.Collections.Generic.List<UI.Tween> tweens;

		public static TweenConductor Create(GameObject addComponentTarget, GameObject target, System.Collections.Generic.List<string> labelList, UnityEngine.Events.UnityAction<string> onStarted, UnityEngine.Events.UnityAction<string> onFinished, UnityEngine.Events.UnityAction onCompleted)
		{
            TweenConductor conductor = addComponentTarget.AddComponent<TweenConductor>();
            conductor.target = target;
            conductor.labelList = labelList != null ? new System.Collections.Generic.List<string>(labelList) : new System.Collections.Generic.List<string>();
            if (onStarted != null)
            {
                if (conductor.onStarted == null)
                    conductor.onStarted = new UnityEngine.Events.UnityEvent<string>();
                conductor.onStarted.AddListener(onStarted);
            }
            if (onFinished != null)
            {
                if (conductor.onFinished == null)
                    conductor.onFinished = new UnityEngine.Events.UnityEvent<string>();
                conductor.onFinished.AddListener(onFinished);
            }
            if (onCompleted != null)
            {
                if (conductor.onCompleted == null)
                    conductor.onCompleted = new UnityEngine.Events.UnityEvent();
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
                tweens = new System.Collections.Generic.List<UI.Tween>(target.GetComponentsInChildren<UI.Tween>(true));
            }
            for (int i = 0; i < tweens.Count; i++)
            {
                UI.Tween t = tweens[i];
                if (t != null && t.label == label && t.IsPlaying())
                    return true;
            }
            return false;
        }

		private void Play(string label)
		{
            currentLabel = label;
            if (target != null)
                UI.Tween.TargetPlayLabel(target, label, true, true);
            onStarted?.Invoke(label);
        }
	}
}
