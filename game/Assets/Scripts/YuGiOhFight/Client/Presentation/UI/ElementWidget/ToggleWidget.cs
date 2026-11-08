using System.Runtime.CompilerServices;


namespace YuGiOhFight.Client.Presentation.UI.ElementWidget
{
	public class ToggleWidget : ElementWidgetBase
	{
		private readonly string k_ELabelButton;

		private readonly string k_ELabelOn;

		private readonly string k_ELabelOff;

		private readonly string k_ELabelBadge;

		private readonly string k_PLabelSoundClickOn;

		private readonly string k_PLabelSoundClickOff;

		public readonly SelectionButton button;

		public readonly UnityEngine.GameObject onImage;

		public readonly UnityEngine.GameObject offImage;

		public readonly YgomSystem.Utility.PropertyContainer propertyContainer;

		private bool m_IsOn;

		public bool isOn
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public UnityEngine.GameObject badge => null;

		public event System.Action<bool> onChangeValue
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

		public ToggleWidget(Elements.ElementObjectManager eom, bool isOn = false)
			: base(null)
		{
		}

		public void ResetIsOn(bool isOn)
		{
		}

		public void UpdateView()
		{
		}

		private void OnClick()
		{
		}
	}
}
