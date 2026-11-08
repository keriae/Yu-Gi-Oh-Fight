using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class InputBlocker : MonoBehaviour
	{
		[SerializeField]
		private UnityEngine.UI.Graphic m_GameMaxRaycastBlocker;

		private int m_BlockCounter;

		public bool isBlocked => false;

		private void Awake()
		{
		}

		private void Clear()
		{
		}

		public void Reboot()
		{
		}

		public void SetBlockEnabled(bool enabled)
		{
		}
	}
}
