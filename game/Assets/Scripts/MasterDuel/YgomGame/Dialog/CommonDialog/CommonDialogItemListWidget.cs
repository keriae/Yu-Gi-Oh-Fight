using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace YgomGame.Dialog.CommonDialog
{
	public class CommonDialogItemListWidget : ContentWidgetBase<CommonDialogItemListWidget, EntryItemListData>, IContentWidgetAsyncLoader, IContentWidgetDirectionalInputListener, IContentLifecycleHandler
	{
		public class ItemLineWidget : YuGiOhFight.Client.Presentation.UI.ElementWidget.ElementWidgetBase
		{
			private readonly string k_ELabelItemButton;

			private readonly string k_ELabelItemIcon;

			private readonly string k_ELabelItemNameText;

			private readonly string k_ELabelItemNumText;

			private readonly string k_OVGroupLabel_Default;

			private readonly string k_OVGroupLabel_Structure;

			private readonly YuGiOhFight.Client.Presentation.UI.PropertyOverrider.PlatformOverriderGroup m_OvGroup;

			public readonly YuGiOhFight.Client.Presentation.UI.SelectionButton button;

			public readonly GameObject itemIcon;

			public readonly TMP_Text itemNameText;

			public readonly TMP_Text itemNumText;

			public ItemLineWidget(YuGiOhFight.Client.Presentation.Elements.ElementObjectManager eom)
				: base(null)
			{
			}

			public void ApplySwitchLabel(bool isPeriod, int itemCategory, int itemId)
			{
			}
		}

		private readonly string k_ELabelScrollView;

		private YuGiOhFight.Client.Presentation.UI.InfinityScroll.InfinityScrollView m_ScrollView;

		//private ScrollRect m_ScrollRect;

		private YuGiOhFight.Client.Presentation.UI.Selector m_Selector;

		private Dictionary<GameObject, ItemLineWidget> m_EntityMap;

		private EntryItemListData m_EntryData;

		private Action m_OnCompleteCallback;

		public static CommonDialogItemListWidget Create(YuGiOhFight.Client.Presentation.Elements.ElementObjectManager eom)
		{
			return null;
		}

		protected override void CollectComponents()
		{
		}

		protected override void InnerBinding(EntryItemListData entryData)
		{
		}

		private IEnumerator yAsyncBinding()
		{
			return null;
		}

		public void AsyncBinding(IEntryData entryData, Action onComplete)
		{
		}

		private void OnScrollInitialized()
		{
		}

		private void OnCreatedEntityCallback(GameObject entity)
		{
		}

		private void OnUpdatedEntityCallback(GameObject entity, int idx)
		{
		}

		private bool OnCustomEdgeTransition(YuGiOhFight.Client.Presentation.UI.SelectionItem selectionItem, YuGiOhFight.Client.Presentation.UI.PadInputDirection direction)
		{
			return false;
		}

		private void TrySelectHeadItem()
		{
		}

		private void TrySelectBottomItem()
		{
		}

		private bool TrySelectItem(int entityIdx, YuGiOhFight.Client.Presentation.UI.SelectionItem currentItem)
		{
			return false;
		}

		private bool TrySelectItem(GameObject activeEntity, YuGiOhFight.Client.Presentation.UI.SelectionItem currentItem)
		{
			return false;
		}

		private void CheckSelectOnAnalogInput(Vector2 dir)
		{
		}

		public void OnMainAnalogInput(Vector2 dir)
		{
		}

		public void OnSubAnalogInput(Vector2 dir)
		{
		}

		public void OnLeftInput()
		{
		}

		public void OnRightInput()
		{
		}

		public void OnUpInput()
		{
		}

		public void OnDownInput()
		{
		}

		public bool OnBack()
		{
			return false;
		}

		public CommonDialogItemListWidget()
		{
			//((ContentWidgetBase<, >)(object)this)._002Ector();
		}
	}
}
