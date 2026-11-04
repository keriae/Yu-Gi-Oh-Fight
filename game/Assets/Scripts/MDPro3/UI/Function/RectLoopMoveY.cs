using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using YgomSystem.UI;
using DGTween = DG.Tweening.Tween;

namespace MDPro3.UI
{
    public class RectLoopMoveY : MonoBehaviour
    {
        public float range;
        public float time;

        [Header("Smoothing")]
        public Ease ease = Ease.InOutSine;

        [Tooltip("Normal = Update, Late = LateUpdate. Late is often smoother for UI.")]
        public UpdateType updateType = UpdateType.Late;

        [Tooltip("If true, animation ignores Time.timeScale (keeps moving in pause/slowmo).")]
        public bool ignoreTimeScale = true;

        private RectTransform _rt;
        private Image _img;

        private float _centerY;
        private DGTween _startupTween;
        private DGTween _loopTween;
        private bool _started;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _img = GetComponent<Image>();
        }

        private void Start()
        {
            _centerY = _rt.anchoredPosition.y;

            foreach (var alpha in GetComponents<TweenAlpha>())
            {
                if (alpha.label == "Show" && _img != null)
                    _img.color = new Color(1f, 1f, 1f, alpha.to);
            }

            foreach (var position in GetComponents<TweenPosition>())
            {
                if (position.label == "Loop")
                {
                    range = position.from.y - position.to.y;
                    time = position.duration;
                    break;
                }
            }

            if (time > 0f && !Mathf.Approximately(range, 0f))
                StartSmoothLoop();
            _started = true;
        }

        private void StartSmoothLoop()
        {
            KillTweens();

            float yMin = _centerY - range;
            float yMax = _centerY + range;

            float curY = _rt.anchoredPosition.y;

            float firstTarget = (Mathf.Abs(curY - yMax) <= Mathf.Abs(curY - yMin)) ? yMax : yMin;
            float otherTarget = (firstTarget == yMax) ? yMin : yMax;

            float fullDist = Mathf.Abs(yMax - yMin);
            float firstDist = Mathf.Abs(curY - firstTarget);
            float firstTime = (fullDist > 0.0001f) ? time * (firstDist / fullDist) : 0f;

            _startupTween = _rt.DOAnchorPosY(firstTarget, firstTime)
                .SetEase(ease)
                .SetUpdate(updateType, ignoreTimeScale);

            _startupTween.OnComplete(() =>
            {
                _loopTween = _rt.DOAnchorPosY(otherTarget, time)
                    .SetEase(ease)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(updateType, ignoreTimeScale);
            });
        }

        private void OnDisable()
        {
            KillTweens();
        }

        private void OnEnable()
        {
            if (_started && _rt != null && time > 0f && !Mathf.Approximately(range, 0f))
                StartSmoothLoop();
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        private void KillTweens()
        {
            KillTween(ref _startupTween);
            KillTween(ref _loopTween);
        }

        private static void KillTween(ref DGTween tween)
        {
            var activeTween = tween;
            tween = null;
            if (activeTween != null && activeTween.IsActive())
                activeTween.Kill(false);
        }
    }
}
