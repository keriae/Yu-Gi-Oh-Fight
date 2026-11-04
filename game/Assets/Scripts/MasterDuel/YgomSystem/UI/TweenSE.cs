using USnd;
using UnityEngine;

namespace YgomSystem.UI
{
	public class TweenSE : Tween
	{
		[SerializeField]
		public string m_SoundLabel;

		[SerializeField]
		public bool m_PlayOnFinish;

		private int instanceId;

		private bool m_IsPlayInFrame;

		protected override void OnSetValue(float par)
		{
            if (m_PlayOnFinish)
            {
                if (par >= 1f && !m_IsPlayInFrame)
                {
                    m_IsPlayInFrame = true;
                    PlaySound();
                }
            }
            else if (par <= 0f && !m_IsPlayInFrame)
            {
                m_IsPlayInFrame = true;
                PlaySound();
            }
        }

		private void PlaySound()
		{
            if (string.IsNullOrEmpty(m_SoundLabel))
                return;
            if (AudioManager.IsInitialized() && AudioManager.FindLabel(m_SoundLabel))
                instanceId = AudioManager.Play(m_SoundLabel);
        }

		private void LateUpdate()
		{
            m_IsPlayInFrame = false;
        }
	}
}
