namespace YuGiOhFight.Client.Presentation.UI
{
	public class ColorContainerSprite : ColorContainer
	{
		private UnityEngine.SpriteRenderer _targetSpriteRenderer;

		public UnityEngine.SpriteRenderer targetSpriteRenderer => null;

		public override void SetColor(SelectMode select_mode, StatusMode status_mode, bool is_active = true)
		{
		}

		private void SetTargetGraphicColor(SelectMode select_mode, StatusMode status_mode, bool is_active)
		{
		}

		private void SetTargetGraphicColor(UnityEngine.Color select_color, ColorMode select_color_mode, UnityEngine.Color status_color, ColorMode status_color_mode, UnityEngine.Color active_color, ColorMode active_color_mode)
		{
		}
	}
}
