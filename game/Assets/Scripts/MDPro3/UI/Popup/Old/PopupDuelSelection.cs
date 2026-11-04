using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MDPro3.UI 
{
    public class PopupDuelSelection : PopupDuel
    {
        [Header("Popup Duel Select Reference")]
        public ScrollRect scrollRect;
        public GameObject item;
        public RectTransform baseRect;
        public List<int> responses;

        public override void InitializeSelections()
        {
            base.InitializeSelections();
            Program.instance.currentServant.returnAction = null;

            var selectionButtons = new List<Button>();
            for (int i = 1; i < selections.Count; i++)
            {
                GameObject newSelection = Instantiate(item);
                newSelection.transform.SetParent(scrollRect.content, false);
                newSelection.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = selections[i];
                var buttonTransform = newSelection.transform.GetChild(0);
                buttonTransform.name = responses[i - 1].ToString();
                var button = buttonTransform.GetComponent<Button>();
                button.onClick.AddListener(() =>
                {
                    string selected = UnityEngine.EventSystems.EventSystem.current.
                        currentSelectedGameObject.name;
                    if (selected != "-233")
                    {
                        var binaryMaster = new BinaryMaster();
                        binaryMaster.writer.Write(int.Parse(selected));
                        SendReturn(binaryMaster.Get());
                    }
                    Hide();
                });
                selectionButtons.Add(button);
                newSelection.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -20 - 90 * (i - 1));
            }

            for (int i = 0; i < selectionButtons.Count; i++)
            {
                var button = selectionButtons[i];
                var navigation = button.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnUp = selectionButtons[Mathf.Max(0, i - 1)];
                navigation.selectOnDown = selectionButtons[Mathf.Min(selectionButtons.Count - 1, i + 1)];
                navigation.selectOnLeft = button;
                navigation.selectOnRight = button;
                button.navigation = navigation;
            }

            if (scrollRect.verticalScrollbar != null)
            {
                var scrollbarNavigation = scrollRect.verticalScrollbar.navigation;
                scrollbarNavigation.mode = Navigation.Mode.None;
                scrollRect.verticalScrollbar.navigation = scrollbarNavigation;
            }

            scrollRect.content.sizeDelta = new Vector2(scrollRect.content.sizeDelta.x, 25 + (selections.Count - 1) * 90);
            baseRect.sizeDelta = new Vector2(baseRect.sizeDelta.x,
                scrollRect.content.sizeDelta.y + 50 > 800 ?
                800 :
                scrollRect.content.sizeDelta.y + 50
                );

            scrollRect.verticalNormalizedPosition = 1f;
            if (selectionButtons.Count > 0)
                EventSystem.current.SetSelectedGameObject(selectionButtons[0].gameObject);
        }
    }

}

