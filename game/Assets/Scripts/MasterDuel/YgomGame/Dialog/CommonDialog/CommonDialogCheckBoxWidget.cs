using System;
using TMPro;


namespace YgomGame.Dialog.CommonDialog
{
	public class CommonDialogCheckBoxWidget : YuGiOhFight.Client.Presentation.UI.ElementWidget.ElementWidgetBehaviourBase<CommonDialogCheckBoxWidget>
	{
		private readonly string k_ELabelText;

		private readonly string k_ELabelToggle;

		private int index;

		private TMP_Text m_Text;

		private UnityEngine.UI.Toggle m_Toggle;

		private YuGiOhFight.Client.Presentation.UI.SelectionButton m_Button;

		public Action<int, bool> OnValueChanegedCallBack;

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

		public static CommonDialogCheckBoxWidget Create(YuGiOhFight.Client.Presentation.Elements.ElementObjectManager eom)
		{
			return null;
		}

		protected override void CollectComponents()
		{
		}

		public CommonDialogCheckBoxWidget Binding(EntryCheckBoxListData.EntryCheckBoxData entryData, int index)
		{
			return null;
		}

		private void OnClick()
		{
		}

		public CommonDialogCheckBoxWidget()
		{
			//((ElementWidgetBehaviourBase<>)(object)this)._002Ector();
		}
	}
}
