namespace YuGiOhFight.Client.Presentation.UI
{
	public class ScenarioShaker : UnityEngine.MonoBehaviour
	{
		private bool m_Playing;

		private float m_CycleDuration;

		private float m_Amount;

		private float m_AutoStopSec;

		private float m_CycleSec;

		private UnityEngine.RectTransform m_RectTran;

		private UnityEngine.Vector3 m_StartPos;

		private UnityEngine.Vector3 m_From;

		private UnityEngine.Vector3 m_To;

		private UnityEngine.Vector3 m_MoveMin;

		private UnityEngine.Vector3 m_MoveMax;

		private int m_MoveDir;

		private bool m_IsAutoStop;

		private System.Action<float> m_OnShakeSetValueAction;

		public static ScenarioShaker Attouch(UnityEngine.GameObject target)
		{
			return null;
		}

		public void PlayShake(float cycleDuration, float amount, bool shakeX, bool shakeY, float stopSec = 0f)
		{
		}

		public void StopShake()
		{
		}

		private void Update()
		{
		}

		private void OnShakeXYSetValue(float par)
		{
		}

		private void OnShakeXSetValue(float par)
		{
		}

		private void OnShakeYSetValue(float par)
		{
		}
	}
}
