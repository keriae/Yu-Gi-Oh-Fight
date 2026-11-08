using UnityEngine;


namespace YuGiOhFight.Client.Presentation.Willow
{
	public class CameraSync : MonoBehaviour
	{
		[SerializeField]
		private Camera m_camera;

		[SerializeField]
		private Cinemachine.CinemachineVirtualCamera m_vcam;

		private void Awake()
		{
		}

		private void LateUpdate()
		{
		}

		public void SetCamera()
		{
		}
	}
}
