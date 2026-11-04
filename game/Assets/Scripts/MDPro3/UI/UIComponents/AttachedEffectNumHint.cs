using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI
{
    public class AttachedEffectNumHint : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private CanvasGroup cg;
        [SerializeField] private CanvasGroup offsetCG;
        private int num = 0;

        public void SetNum(int num)
        {
            if(this.num == num) return;
            this.num = num;
            if(num == 0)
            {
                cg.DOFade(0f, 0.1f);
            }
            else
            {
                cg.alpha = 0f;
                cg.DOFade(1f, 0.1f);
                text.text = FormatNum(num);
                DOTween.Sequence()
                    .Append(text.rectTransform.DOScale(new Vector3(1.5f, 1.5f, 1f), 0.15f).SetEase(Ease.OutQuad))
                    .Append(text.rectTransform.DOScale(Vector3.one, 0.15f).SetEase(Ease.InQuad));
                offsetCG.alpha = 0.7f;
                offsetCG.DOFade(0f, 0.3f).SetEase(Ease.InCubic);
                offsetCG.transform.localScale = new Vector3(0.1f, 0.1f, 1f);
                offsetCG.transform.DOScale(new Vector3(0.9f, 0.9f, 1f), 0.3f).SetEase(Ease.OutCubic);
            }
        }

        private string FormatNum(int num)
        {
            if (num > 99)
                return "99+";
            return num.ToString();
        }
    }
}