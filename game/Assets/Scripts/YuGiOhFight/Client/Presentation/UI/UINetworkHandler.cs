namespace YuGiOhFight.Client.Presentation.UI
{
	public class UINetworkHandler : UnityEngine.MonoBehaviour
	{
		private System.Collections.Generic.List<YgomSystem.Network.Handle> m_NetworkProgressDispOwners;

		private int m_ResourceNetworkProgressCnt;

		private bool m_Retry;

		private int m_SkipHandleSectionMainteCnt;

		private System.Collections.Generic.List<YgomSystem.Network.Handle> m_RetryHandles;

		private static bool isIgnoreNetworkProgressCommand(string command)
		{
			return false;
		}

		public void SetSkipHandleSectionMainteEnabled(bool enabled)
		{
		}

		public void SetSystemHandler()
		{
		}

		public void ClearOnReboot()
		{
		}

		private void networkStartHandle(YgomSystem.Network.Handle handle)
		{
		}

		private void networkCompleteHandle(YgomSystem.Network.Handle handle)
		{
		}

		private void networkErrorHandle(YgomSystem.Network.Handle handle)
		{
		}

		public void networkDisconnectErrorDialog()
		{
		}

		private void resourceProgressHandle(bool isshow)
		{
		}

		private bool? resourceRetryHandle(bool firstcall)
		{
			return null;
		}

		private void resourceErrorHandle(string path)
		{
		}

		public void OpenNetworkErrorDialog(YgomSystem.Network.Handle handle)
		{
		}
	}
}
