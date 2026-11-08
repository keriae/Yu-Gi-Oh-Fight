namespace YuGiOhFight.Client.Presentation.UI
{
	public class ScreenSelector : Selector
	{
		public enum Type
		{
			FullScreen = 0,
			Untouchable = 1
		}

		private System.Collections.Generic.List<SelectionButton> buttonList;

		public static ScreenSelector Create(UnityEngine.Transform parent, string group_label, int group_priority)
		{
			return null;
		}

		public SelectionButton AddShortCutKeyReceiver(SelectorManager.KeyType key_type, Type type, UnityEngine.Events.UnityAction click_event)
		{
			return null;
		}

		public void DeleteAllShortCutButton()
		{
		}
	}
}
