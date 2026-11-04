using MDPro3.Servant;
using MDPro3.UI;
using MDPro3.UI.ServantUI;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MDPro3.UI
{
    public class SelectionToggle_Mate : SelectionToggle_ScrollRectItem
    {
        public int code;
        public string mateName;

        public override void Refresh()
        {
            SetButtonText(mateName);
        }
        protected override void CallSubmitEvent()
        {
            AudioManager.PlaySE("SE_MENU_DECIDE");
            if (code == 0) return;
            Program.instance.mate.ViewMate(code);
        }
        protected override void CallToggleOnEvent()
        {
            base.CallToggleOnEvent();
            Program.instance.mate.lastSelectedMateItem = this;
        }


        protected override void OnClick()
        {
            Program.instance.mate.lastSelectedMateItem = this;
            CallSubmitEvent();
        }

        protected override void ToggleOn()
        {
            isOn = true;
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

        protected override void OnNavigation(AxisEventData eventData)
        {
            base.OnNavigation(eventData);

            if (eventData.moveDir == MoveDirection.Right)
            {
                UserInput.NextSelectionIsAxis = true;
                Program.instance.mate.GetUI<MateViewerUI>().SelectButtonInteract();
            }
        }
    }
}
