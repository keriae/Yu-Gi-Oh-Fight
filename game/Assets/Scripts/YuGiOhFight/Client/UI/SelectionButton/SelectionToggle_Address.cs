using UnityEngine;
using UnityEngine.EventSystems;


namespace YuGiOhFight.Client.UI
{
    public class SelectionToggle_Address : SelectionToggle_ScrollRectItem
    {
        [Header("SelectionToggle Address")]
        public string addressName;
        public string addressHost;
        public string addressPort;
        public string addressPassword;
        public bool editable;

        [SerializeField] private GameObject btnDelete;
        [SerializeField] private GameObject btnMoveUp;

        public override void Refresh()
        {
            btnDelete.SetActive(editable);
            btnMoveUp.SetActive(editable);
            SetButtonText(addressName);
        }

        protected override void CallSubmitEvent()
        {
            base.CallSubmitEvent();
            Runtime.AudioManager.PlaySE("SE_MENU_DECIDE");
            Runtime.Program.instance.online.GetUI<ServantUI.OnlineServantUI>().ServerAddressPage
                .SetHost(addressHost, addressPort, addressPassword);
        }

        protected override void CallToggleOnEvent()
        {
            base.CallHoverOnEvent();
            Runtime.Program.instance.online.lastSelectedAddressItem = this;
        }

        protected override void OnClick()
        {
            Runtime.Program.instance.online.lastSelectedAddressItem = this;
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

        protected override void ExtraNavigation(AxisEventData eventData)
        {
            if(editable && eventData.moveDir == MoveDirection.Down)
            {
                ServerAddressPage.SelectYpkServerButton();
            }
            else if(!editable && eventData.moveDir == MoveDirection.Up)
            {
                ServerAddressPage.SelectHostServerButton();
            }
        }

        protected override void OnNavigation(AxisEventData eventData)
        {
            base.OnNavigation(eventData);

            if (eventData.moveDir == MoveDirection.Right)
            {
                Runtime.UserInput.NextSelectionIsAxis = true;
                Runtime.Program.instance.online.GetUI<ServantUI.OnlineServantUI>().SelectLastSelectable(null);
            }
        }

        public void OnDelete()
        {
            if (!editable)
                return;

            Runtime.Program.instance.online.GetUI<ServantUI.OnlineServantUI>()
                .ServerAddressPage.DeleteAddress(addressName);
        }

        public void OnMoveUp()
        {
            if (!editable)
                return;

            Runtime.Program.instance.online.GetUI<ServantUI.OnlineServantUI>()
                .ServerAddressPage.AddressMoveUp(addressName);
        }
    }
}
