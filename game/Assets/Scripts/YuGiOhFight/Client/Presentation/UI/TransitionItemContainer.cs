using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class TransitionItemContainer : MonoBehaviour
	{
		[SerializeField]
		private System.Collections.Generic.List<SelectionItem> _itemListUp;

		[SerializeField]
		private System.Collections.Generic.List<SelectionItem> _itemListDown;

		[SerializeField]
		private System.Collections.Generic.List<SelectionItem> _itemListRight;

		[SerializeField]
		private System.Collections.Generic.List<SelectionItem> _itemListLeft;

		private System.Collections.Generic.List<SelectionItem> itemListUp => null;

		private System.Collections.Generic.List<SelectionItem> itemListDown => null;

		private System.Collections.Generic.List<SelectionItem> itemListRight => null;

		private System.Collections.Generic.List<SelectionItem> itemListLeft => null;

		public SelectionItem GetItem(PadInputDirection direction)
		{
			return null;
		}
	}
}
