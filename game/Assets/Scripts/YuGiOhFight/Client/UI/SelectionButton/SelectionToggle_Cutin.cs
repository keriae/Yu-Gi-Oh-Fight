using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;


namespace YuGiOhFight.Client.UI
{
    public class SelectionToggle_Cutin : SelectionToggle_ScrollRectItem
    {
        public int code;
        public string cardName;

        private Color colorForDiyCutin = Color.gray;

        public override void Refresh()
        {
            Manager.GetElement<TextMeshProUGUI>("ButtonText").text = cardName;
            if(Controllers.CutinViewer.codes.Contains(code))
                Manager.GetElement<TextMeshProUGUI>("ButtonText").color = Color.white;
            else
                Manager.GetElement<TextMeshProUGUI>("ButtonText").color = colorForDiyCutin;
        }
        protected override void CallSubmitEvent()
        {
            Runtime.AudioManager.PlaySE("SE_MENU_DECIDE");
            if (Controllers.CutinViewer.CutinExist(code))
                _ = Controllers.CutinViewer.Play(code, 0);
            else
                Debug.LogWarning($"Cutin with code {code} does not exist.");
        }
        protected override void CallToggleOnEvent()
        {
            base.CallToggleOnEvent();
            Runtime.Program.instance.cutin.lastSelectedCutinItem = this;
        }


        protected override void OnClick()
        {
            Runtime.Program.instance.cutin.lastSelectedCutinItem = this;
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
                Runtime.Program.instance.cutin.GetUI<ServantUI.CutinViewerUI>().ButtonAutoPlay.GetSelectable().Select();
            }
        }
    }
}
