using MDPro3.UI;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;

namespace MDPro3.UI
{
    public class SelectionToggle_Cutin : SelectionToggle_ScrollRectItem
    {
        public int code;
        public string cardName;

        private Color colorForDiyCutin = Color.gray;

        public override void Refresh()
        {
            Manager.GetElement<TextMeshProUGUI>("ButtonText").text = cardName;
            if(CutinViewer.codes.Contains(code))
                Manager.GetElement<TextMeshProUGUI>("ButtonText").color = Color.white;
            else
                Manager.GetElement<TextMeshProUGUI>("ButtonText").color = colorForDiyCutin;
        }
        protected override void CallSubmitEvent()
        {
            AudioManager.PlaySE("SE_MENU_DECIDE");
            if (CutinViewer.CutinExist(code))
                _ = CutinViewer.Play(code, 0);
            else
                Debug.LogWarning($"Cutin with code {code} does not exist.");
        }
        protected override void CallToggleOnEvent()
        {
            base.CallToggleOnEvent();
            Program.instance.cutin.lastSelectedCutinItem = this;
        }


        protected override void OnClick()
        {
            Program.instance.cutin.lastSelectedCutinItem = this;
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
                Program.instance.cutin.GetUI<CutinViewerUI>().ButtonAutoPlay.GetSelectable().Select();
            }
        }
    }
}
