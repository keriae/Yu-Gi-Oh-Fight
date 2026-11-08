using System;
using UnityEngine;
using UnityEngine.Events;


namespace YuGiOhFight.Client.UI.Popup
{
    public class SelectionToggle_PopupSelectionItem : SelectionToggle_ScrollRectItem
    {
        public string selection;
        public string color = string.Empty;
        public Action clickAction;
        public PopupSelection manager;

        private void SetTextColor()
        {
            var targetColor = Color.white;
            if(color == "r")
                targetColor = Color.red;
            else if(color == "g")
                targetColor = Color.green;
            else if(color == "b")
                targetColor = Color.cyan;

            Presentation.UI.ColorContainerGraphic ccg = ButtonText.GetComponent<Presentation.UI.ColorContainerGraphic>();
            ccg.baseColor = targetColor;
            SetButtonTextColor(targetColor);
        }

        public override void Refresh()
        {
            SetButtonText(selection);
            SetTextColor();
            RemoveAllListeners();
            SetClickEvent(new UnityAction(clickAction));
        }

        protected override void ToggleOn()
        {
            isOn = true;
            if (manager != null)
                manager.lastSelectedItem = this;
        }
        public override void ToggleOnNow()
        {
            isOn = true;
        }
        protected override void ToggleOff()
        {
            isOn = false;
        }
        public override void ToggleOffNow()
        {
            isOn = false;
        }
    }
}
