namespace YuGiOhFight.Client.Presentation.UI
{
	public class ShortcutIcon : DeviceIcon
	{
		public enum Mode
		{
			None = 0,
			PadKey = 1,
			Mouse = 2,
			Analog = 3
		}

		public enum AnalogDirection
		{
			Horizontal = 0,
			Vertical = 1
		}

		public SelectorManager.KeyType keyType;

		public SelectorManager.AnalogType analogType;

		public SelectorManager.MouseType mouseType;

		public YgomSystem.Utility.GamePadIconUtil.Variation iconVariation;

		public Mode mode;

		public AnalogDirection analogDirection;

		public void SetKeyType(SelectorManager.KeyType keyType, YgomSystem.Utility.GamePadIconUtil.Variation variation = YgomSystem.Utility.GamePadIconUtil.Variation.Var00, System.Action onCompleted = null)
		{
		}

		public void SetAnalogType(SelectorManager.AnalogType analogType, YgomSystem.Utility.GamePadIconUtil.Variation variation = YgomSystem.Utility.GamePadIconUtil.Variation.Var00, System.Action onCompleted = null)
		{
		}

		public void SetMouseType(SelectorManager.MouseType mouseType, YgomSystem.Utility.GamePadIconUtil.Variation variation = YgomSystem.Utility.GamePadIconUtil.Variation.Var00, System.Action onCompleted = null)
		{
		}

		protected override void UpdateDisplay(System.Action onCompleted = null)
		{
		}

		private void SetIcon(System.Action onCompleted = null)
		{
		}
	}
}
