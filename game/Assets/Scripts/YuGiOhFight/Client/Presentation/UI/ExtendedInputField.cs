using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class ExtendedInputField : UnityEngine.UI.InputField, YgomSystem.LocalizedFont.ILocalizedFontOwner
	{
		[SerializeField]
		private YgomSystem.LocalizedFont.LocalizedFontSettingsBase.FontType m_localizedFontType;

		[SerializeField]
		private int m_localizedFontMaterialIndex;

		private Event m_ProcessingEvent;

		private int preAnchor;

		private int preFocus;

		private string preText;

		public YgomSystem.LocalizedFont.LocalizedFontSettingsBase.FontType localizedFontType
		{
			get
			{
				return default(YgomSystem.LocalizedFont.LocalizedFontSettingsBase.FontType);
			}
			set
			{
			}
		}

		public int localizedFontMaterialIndex
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		protected override void Start()
		{
		}

		//public override void OnSelect(BaseEventData eventData)
		//{
		//}

		//public override void OnUpdateSelected(BaseEventData eventData)
		//{
		//}
	}
}
