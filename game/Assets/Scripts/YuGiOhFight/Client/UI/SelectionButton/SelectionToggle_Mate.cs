using UnityEngine.EventSystems;


namespace YuGiOhFight.Client.UI
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
            Runtime.AudioManager.PlaySE("SE_MENU_DECIDE");
            if (code == 0) return;
            Runtime.Program.instance.mate.ViewMate(code);
        }
        protected override void CallToggleOnEvent()
        {
            base.CallToggleOnEvent();
            Runtime.Program.instance.mate.lastSelectedMateItem = this;
        }


        protected override void OnClick()
        {
            Runtime.Program.instance.mate.lastSelectedMateItem = this;
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
                Runtime.UserInput.NextSelectionIsAxis = true;
                Runtime.Program.instance.mate.GetUI<ServantUI.MateViewerUI>().SelectButtonInteract();
            }
        }
    }
}
