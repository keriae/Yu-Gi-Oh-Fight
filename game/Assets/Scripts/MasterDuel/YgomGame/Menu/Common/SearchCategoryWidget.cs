using TMPro;


namespace YgomGame.Menu.Common
{
	public class SearchCategoryWidget : YuGiOhFight.Client.Presentation.UI.ElementWidget.ElementWidgetBase
	{
		private int m_categoryId;

		private string m_categoryName;

		private TextMeshProUGUI m_TextOn;

		private TextMeshProUGUI m_TextOff;

		public int categoryId => 0;

		public string categoryName => null;

		public SearchCategoryWidget(YuGiOhFight.Client.Presentation.Elements.ElementObjectManager eom)
			: base(null)
		{
		}

		public SearchCategoryWidget Binding(int id, string name)
		{
			return null;
		}
	}
}
