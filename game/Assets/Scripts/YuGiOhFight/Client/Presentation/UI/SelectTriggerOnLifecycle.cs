using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class SelectTriggerOnLifecycle : MonoBehaviour
	{
		[SerializeField]
		private SelectionItem m_SelectedCheckItem;

		[SerializeField]
		private System.Collections.Generic.List<SelectionItem> m_OnEnableTargets;

		[SerializeField]
		private System.Collections.Generic.List<SelectionItem> m_OnDisableTargets;

		public SelectionItem selectedCheckItem
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public System.Collections.Generic.List<SelectionItem> onEnableTargets => null;

		public System.Collections.Generic.List<SelectionItem> onDisableTargets => null;

		private void OnEnable()
		{
		}

		private void OnDisable()
		{
		}
	}
}
