namespace YuGiOhFight.Client.Presentation.UI.ElementWidget
{
	public class DirectionalToggleGroupWidget : ElementWidgetBehaviourBase<DirectionalToggleGroupWidget>
	{
		private ToggleGroupWidget m_ToggleGroupWidget;

		public bool focusIdxToSelectionTransitionItem;

		public ToggleGroupWidget toggleGroupWidget => null;

		public static DirectionalToggleGroupWidget Create(Elements.ElementObjectManager eom, string elabelFormat, int defaultIdx = 0)
		{
			return null;
		}

		public static DirectionalToggleGroupWidget Create(Elements.ElementObjectManager eom, ToggleWidget[] toggles, int defaultIdx = 0)
		{
			return null;
		}

		private void Start()
		{
		}

		private void OnSelectedToggleCallback()
		{
		}

		private void OnChangeIdx(int idx)
		{
		}

		private void SetCurrentToggleToSelector()
		{
		}

		public DirectionalToggleGroupWidget()
		{
			//((ElementWidgetBehaviourBase<>)(object)this)._002Ector();
		}
	}
}
