using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class PlatformPlayerNameGroup : MonoBehaviour
	{
		private readonly string k_CurrentPlatformIconPath;

		[SerializeField]
		private TMPro.TMP_Text m_YmdPlayerNameText;

		[SerializeField]
		private GameObject m_PlatformNameRoot;

		[SerializeField]
		private TMPro.TMP_Text m_PlatformPlayerNameText;

		[SerializeField]
		private UnityEngine.UI.Image m_PlatformPlayerIcon;

		[SerializeField]
		private bool m_IsDispPcode;

		public void Set(string ymdPlayerName, string platformPlayerName, long pcode, bool isRegistedPlatform)
		{
		}

		public string GetYmdPlayerName()
		{
			return null;
		}
	}
}
