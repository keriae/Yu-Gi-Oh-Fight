namespace YuGiOhFight.Client.Presentation.UI
{
	public class ColorContainerMatHDR : ColorContainer
	{
		public string colorParamName;

		private UnityEngine.Renderer m_TargetRenderer;

		private UnityEngine.Renderer targetRenderer => null;

		private UnityEngine.Color GetColorByMode(SelectMode selectMode, StatusMode statusMode, bool isActive)
		{
			return default(UnityEngine.Color);
		}

		public override void SetColor(SelectMode select_mode, StatusMode status_mode, bool is_active = true)
		{
		}
	}
}
