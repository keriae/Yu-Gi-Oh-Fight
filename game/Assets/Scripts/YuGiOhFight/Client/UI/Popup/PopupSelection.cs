using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI.Popup
{
    public class PopupSelection : Popup
    {
        [Header("Popup Selection")]
        public SelectionToggle_PopupSelectionItem lastSelectedItem;

        public Action decideAction;
        private SuperScrollView selectionsView;

        private void LateUpdate()
        {
            // The async row asset can arrive before the native layout has a
            // viewport height. Fill the visible pool once that layout settles.
            selectionsView?.SynchronizeViewport();
        }

        protected override void InitializeSelections()
        {
            base.InitializeSelections();

            Manager.GetElement<TextMeshProUGUI>("MessageText").text = args[1];
            Manager.GetElement("MessageArea").SetActive(args[1] != string.Empty);

            var itemWidth = PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 940f : 748f;
            var itemHeight = PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 98f : 72f;
            var topPadding = PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 47f : 30f;
            var space = PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 20f : 16f;

            var selectionCount = args.Count - 2;
            var preferredHeight = selectionCount * (itemHeight + space) + topPadding * 2 - space;
            Manager.GetElement<LayoutElement>("EntryButtonsScrollView").preferredHeight = preferredHeight;

            var handle = Addressables.LoadAssetAsync<GameObject>("Popup/PopupSelectionItem.prefab");
            handle.Completed += (result) =>
            {

                selectionsView = new SuperScrollView(
                    1,
                    itemWidth,
                    itemHeight + space,
                    topPadding,
                    topPadding - space,
                    result.Result,
                    ItemOnListRefresh,
                    Manager.GetElement<ScrollRect>("EntryButtonsScrollView"));

                var tasks = new List<string[]>();
                for (int i = 2; i < args.Count; i++)
                {
                    var task = args[i].Split(":");
                    tasks.Add(task);
                }
                selectionsView.Print(tasks);
                if (selectionsView.items.Count > 0)
                    EventSystem.current.SetSelectedGameObject(selectionsView.items[0].gameObject);
            };
        }

        private void ItemOnListRefresh(string[] task, GameObject item)
        {
            SelectionToggle_PopupSelectionItem handler = item.GetComponent<SelectionToggle_PopupSelectionItem>();
            if(task.Length == 2)
            {
                handler.color = task[0];
                handler.selection = task[1];
            }
            else
            {
                handler.color = string.Empty;
                handler.selection = task[0];
            }
            handler.clickAction = OnClick;
            handler.manager = this;
            handler.Refresh();
        }

        private void OnClick()
        {
            decideAction?.Invoke();
            Hide();
        }

        protected override void SelectLastSelected()
        {
            if(lastSelectedItem != null)
                EventSystem.current.SetSelectedGameObject(lastSelectedItem.gameObject);
            else
                EventSystem.current.SetSelectedGameObject(Manager.GetElement<ScrollRect>("EntryButtonsScrollView").content.GetChild(0).gameObject);
        }

        public void SelectLastItem()
        {
            Runtime.UserInput.NextSelectionIsAxis = true;
            SelectLastSelected();
        }
    }
}
