using MDPro3.Net;
using UnityEngine;
using UnityEngine.EventSystems;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;

namespace MDPro3.UI
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
            AudioManager.PlaySE("SE_MENU_DECIDE");
            Program.instance.online.GetUI<OnlineServantUI>().PageLegacy
                .SetHost(addressHost, addressPort, addressPassword);
        }

        protected override void CallToggleOnEvent()
        {
            base.CallHoverOnEvent();
            Program.instance.online.lastSelectedAddressItem = this;
        }

        protected override void OnClick()
        {
            Program.instance.online.lastSelectedAddressItem = this;
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
                PageLegacy.SelectYpkServerButton();
            }
            else if(!editable && eventData.moveDir == MoveDirection.Up)
            {
                PageLegacy.SelectHostServerButton();
            }
        }

        protected override void OnNavigation(AxisEventData eventData)
        {
            base.OnNavigation(eventData);

            if (eventData.moveDir == MoveDirection.Right)
            {
                UserInput.NextSelectionIsAxis = true;
                Program.instance.online.GetUI<OnlineServantUI>().SelectLastSelectable(null);
            }
        }

        public void OnDelete()
        {
            if (!editable)
                return;

            Program.instance.online.GetUI<OnlineServantUI>()
                .PageLegacy.DeleteAddress(addressName);
        }

        public void OnMoveUp()
        {
            if (!editable)
                return;

            Program.instance.online.GetUI<OnlineServantUI>()
                .PageLegacy.AddressMoveUp(addressName);
        }
    }
}
