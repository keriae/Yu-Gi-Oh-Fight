using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI
{
    internal static class DOTweenLifecycle
    {
        public static void Kill(Tween tween)
        {
            if (tween != null && tween.IsActive())
                tween.Kill(false);
        }

        public static void KillAndClear<T>(IList<T> tweens) where T : Tween
        {
            if (tweens == null)
                return;

            for (int i = 0; i < tweens.Count; i++)
                Kill(tweens[i]);
            tweens.Clear();
        }

        public static void KillTarget(Object target)
        {
            if (target != null)
                DOTween.Kill(target, false);
        }

        public static void AttachKillOnDestroy(GameObject root)
        {
            if (root == null || root.GetComponent<DOTweenLifecycleCleaner>() != null)
                return;
            root.AddComponent<DOTweenLifecycleCleaner>();
        }

        public static void KillFor(GameObject root)
        {
            if (root == null)
                return;

            DOTween.Kill(root, false);

            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
                if (rect != null)
                    DOTween.Kill(rect, false);

            foreach (var canvasGroup in root.GetComponentsInChildren<CanvasGroup>(true))
                if (canvasGroup != null)
                    DOTween.Kill(canvasGroup, false);

            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
                if (graphic != null)
                    DOTween.Kill(graphic, false);
        }
    }

    internal sealed class DOTweenLifecycleCleaner : MonoBehaviour
    {
        private void OnDestroy()
        {
            DOTweenLifecycle.KillFor(gameObject);
        }
    }
}
