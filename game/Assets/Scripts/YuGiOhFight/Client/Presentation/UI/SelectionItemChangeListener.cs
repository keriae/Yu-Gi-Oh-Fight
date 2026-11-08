using System.Runtime.CompilerServices;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class SelectionItemChangeListener : UnityEngine.MonoBehaviour
	{
		private SelectionItem m_LastSelectItem;

		public event System.Action<SelectionItem, SelectionItem> onChangedSelectionItemEvent
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public static SelectionItemChangeListener Attach(UnityEngine.GameObject owner)
		{
			return null;
		}

		private void LateUpdate()
		{
		}
	}
}
