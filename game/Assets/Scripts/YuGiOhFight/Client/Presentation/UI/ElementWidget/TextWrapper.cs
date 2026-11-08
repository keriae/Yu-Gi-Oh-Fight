namespace YuGiOhFight.Client.Presentation.UI.ElementWidget
{
	public class TextWrapper
	{
		private enum Mode
		{
			uGUI = 0,
			TMP = 1
		}

		private Mode mode;

		private MDText textComponent;

		private TMPro.TMP_Text tmpTextComponent;

		public UnityEngine.GameObject gameObject => null;

		public string text
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public UnityEngine.Color color
		{
			get
			{
				return default(UnityEngine.Color);
			}
			set
			{
			}
		}

		public bool enabled
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public TextWrapper(MDText text)
		{
		}

		public TextWrapper(TMPro.TMP_Text text)
		{
		}
	}
}
