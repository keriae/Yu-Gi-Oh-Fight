using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI
{
    public class OnScrollSetFreeze : MonoBehaviour
    {
        public static bool Freeze;
        private Coroutine coroutine;

        private ScrollRect m_ScrollRect;
        private ScrollRect ScrollRect =>
            m_ScrollRect = m_ScrollRect != null ? m_ScrollRect
            : GetComponent<ScrollRect>();

        private float minVelocity = 200f;

        private void Awake()
        {
            ScrollRect.onValueChanged.AddListener(SetFreeze);
        }

        public void SetFreeze(Vector2 position)
        {
            var y = Mathf.Abs(ScrollRect.velocity.y);
            if (y > 0 && y < minVelocity)
                return;

            if (coroutine != null)
                StopCoroutine(coroutine);
            coroutine = StartCoroutine(SetFreezeAsync());
        }

        private IEnumerator SetFreezeAsync()
        {
            if(Cursor.lockState == CursorLockMode.Locked)
                yield break;
            Freeze = true;
            float timeElapsed = 0f;
            while (timeElapsed < 0.15f)
            {
                yield return null;
                timeElapsed += Time.unscaledDeltaTime;
                var y = Mathf.Abs(ScrollRect.velocity.y);
                if (y > 0 && y < minVelocity)
                    break;
            }
            Freeze = false;
        }
    }
}
