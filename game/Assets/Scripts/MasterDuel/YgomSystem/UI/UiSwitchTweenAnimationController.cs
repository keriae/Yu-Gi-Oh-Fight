using System;
using UnityEngine;
using UnityEngine.Events;

namespace YgomSystem.UI
{
	public class UiSwitchTweenAnimationController : MonoBehaviour
	{
		public bool activeControl;

		public string labelShow;

		public string labelShow_Event;

		public string labelHide;

		public string labelHide_Event;

		public string labelChain;

		public string labelChain_Event;

		private UnityAction m_onShow;

		private UnityAction m_onHide;

		private UnityAction<int> m_onChain;

		public event UnityAction onShow
		{
			add { m_onShow += value; }
			remove { m_onShow -= value; }
		}

		public event UnityAction onHide
		{
			add { m_onHide += value; }
			remove { m_onHide -= value; }
		}

		public event UnityAction<int> onChain
		{
			add { m_onChain += value; }
			remove { m_onChain -= value; }
		}

		private void OnEnable()
		{
            if (activeControl)
                RegistChainTween(true);
        }

		public void Show()
		{
            gameObject.SetActive(true);
            Tween.TargetPlayLabel(gameObject, labelShow, true, true);
            OnShow();
        }

		public void Hide()
		{
            Tween.TargetPlayLabel(gameObject, labelHide, true, true);
            OnHide();
        }

		public void Chain()
		{
            Tween.TargetPlayLabel(gameObject, labelChain, true, true);
            OnChain(0);
        }

		private void OnShow()
		{
            if (!string.IsNullOrEmpty(labelShow_Event))
                Tween.TargetPlayLabel(gameObject, labelShow_Event, true, true);
            m_onShow?.Invoke();
        }

		private void OnHide()
		{
            if (!string.IsNullOrEmpty(labelHide_Event))
                Tween.TargetPlayLabel(gameObject, labelHide_Event, true, true);
            m_onHide?.Invoke();
        }

		private void OnChain(int index)
		{
            if (!string.IsNullOrEmpty(labelChain_Event))
                Tween.TargetPlayLabel(gameObject, labelChain_Event, true, true);
            m_onChain?.Invoke(index);
        }

		private void RegistChainTween(bool isEventTween)
		{
            if (isEventTween)
            {
                if (!string.IsNullOrEmpty(labelShow_Event))
                    Tween.TargetPlayLabel(gameObject, labelShow_Event, true, true);
                if (!string.IsNullOrEmpty(labelHide_Event))
                    Tween.TargetPlayLabel(gameObject, labelHide_Event, true, true);
                if (!string.IsNullOrEmpty(labelChain_Event))
                    Tween.TargetPlayLabel(gameObject, labelChain_Event, true, true);
            }
        }

		private void StopAllTween()
		{
            var tweens = GetComponentsInChildren<Tween>(true);
            for (int i = 0; i < tweens.Length; i++)
            {
                if (tweens[i] != null)
                    tweens[i].Stop();
            }
        }

		private void OnDestroy()
		{
            m_onShow = null;
            m_onHide = null;
            m_onChain = null;
        }
	}
}
