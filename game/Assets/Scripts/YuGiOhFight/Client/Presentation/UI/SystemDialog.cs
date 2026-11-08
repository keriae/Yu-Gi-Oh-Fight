using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class SystemDialog : MonoBehaviour
	{
		public const string k_ArgKeyTitle = "title";

		public const string k_ArgKeyUpperMessage = "upperMessage";

		public const string k_ArgKeyLowerMessage = "lowerMessage";

		public const string k_ArgKeyLowerMessageScrollHeight = "lowerMessageScrollHeight";

		public const string k_ArgKeyTime = "time";

		public const string k_ArgKeyImagePath = "imagePath";

		public const string k_ArgKeyImageScale = "imageScale";

		public const string k_ArgKeyImageVisible = "imageVisible";

		public const string k_ArgKeyButtonLabel = "buttonLabel";

		public const string k_ArgKeyAction = "action";

		public const string k_ArgKeyButtonLabel2 = "buttonLabel2";

		public const string k_ArgKeyAction2 = "action2";

		public const string k_ArgKeyRebootButtonVisible = "rebootVisible";

		public const string k_ArgKeyWindowWidth = "windowWidth";

		public const string k_ArgKeyOpenSe = "opense";

		private readonly string k_MaintenanceBannerPath;

		[SerializeField]
		private Elements.ElementObjectManager m_SystemDialogPref;

		private YgomGame.Dialog.CommonDialog.CommonDialogContentContainerWidget m_SystemDialogWidget;

		private System.Collections.IEnumerator m_FontLoadCoroutine;

		private System.Action m_ActionCallback1;

		private System.Action m_ActionCallback2;

		public bool isExistsDialog => false;

		private int defaultMinHeight => 0;

		public bool OpenFatalErrorDialog(string message, string label1 = null, System.Action action1 = null, System.Collections.Generic.Dictionary<string, object> args = null)
		{
			return false;
		}

		public System.Collections.Generic.Dictionary<string, object> GetMaintenanceDialogArgs()
		{
			return null;
		}

		public void OpenOperationMainteDialog(System.Collections.Generic.Dictionary<string, object> defaultArgs = null, System.Collections.Generic.Dictionary<string, object> overrideArgs = null)
		{
		}

		public bool OpenMaintenanceDialog(System.Collections.Generic.Dictionary<string, object> args)
		{
			return false;
		}

		private bool OpenSystemDialog(System.Collections.Generic.List<YgomGame.Dialog.CommonDialog.IEntryData> entryDatas, System.Collections.Generic.Dictionary<string, object> args = null)
		{
			return false;
		}

		private System.Collections.IEnumerator waitLocalizedFontsLoad(System.Action callback)
		{
			return null;
		}

		private void OpenSystemDialogCore(System.Collections.Generic.List<YgomGame.Dialog.CommonDialog.IEntryData> entryDatas, System.Collections.Generic.Dictionary<string, object> args = null)
		{
		}

		private void CloseSystemDialog()
		{
		}

		private void ClearCallbacks()
		{
		}

		public void ClearOnReboot()
		{
		}

		private void OnClickReboot()
		{
		}

		private void OnClickAction1()
		{
		}

		private void OnClickAction2()
		{
		}
	}
}
