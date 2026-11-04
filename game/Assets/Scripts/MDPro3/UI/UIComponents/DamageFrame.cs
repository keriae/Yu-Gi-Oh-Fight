using DG.Tweening;
using UnityEngine;

namespace MDPro3.UI
{
    public class DamageFrame : MonoBehaviour
    {
        private CanvasGroup cg;
        private readonly float duration = 0.25f;

        private void Awake()
        {
            cg = GetComponent<CanvasGroup>();
            cg.alpha = 0f;
        }

        public void Show()
        {
            DOTween.Sequence()
                .Append(cg.DOFade(1f, duration).SetEase(Ease.OutQuad))
                .Append(cg.DOFade(0f, duration).SetEase(Ease.InQuad));
        }
    }
}
