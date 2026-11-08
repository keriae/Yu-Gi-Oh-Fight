using System;
using System.Collections.Generic;


namespace YgomGame.Dialog.CommonDialog
{
	public class CommonDialogCheckBoxGroupWidget : ContentWidgetBase<CommonDialogCheckBoxGroupWidget, EntryCheckBoxListData>
	{
		private readonly string k_ELabelTemplateCheckBox;

		private bool m_IsEnableMulti;

		private YuGiOhFight.Client.Presentation.Elements.ElementObjectManager m_TemplateCheckBoxEom;

		private List<CommonDialogCheckBoxWidget> m_CheckBoxes;

		//private LayoutElement m_LayoutElement;

		private Action<List<bool>> OnCompleteCallback;

		public static CommonDialogCheckBoxGroupWidget Create(YuGiOhFight.Client.Presentation.Elements.ElementObjectManager eom)
		{
			return null;
		}

		protected override void CollectComponents()
		{
		}

		public CommonDialogCheckBoxWidget GetCheckBoxWidget(int idx)
		{
			return null;
		}

		protected override void InnerBinding(EntryCheckBoxListData entryData)
		{
		}

		private void UpdateCheckBoxes(int index, bool isOn)
		{
		}

		public void OnCompleteEvent()
		{
		}

		public List<bool> GetCheckValues()
		{
			return null;
		}

		public CommonDialogCheckBoxGroupWidget()
		{
			//((ContentWidgetBase<, >)(object)this)._002Ector();
		}
	}
}
