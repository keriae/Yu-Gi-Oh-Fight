using MDPro3.UI;
using MDPro3.UI.ServantUI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MDPro3.Servant
{
    public class ReplaySelector : Servant
    {

        [HideInInspector] public SelectionToggle_Replay lastSelectedReplayItem;

        public override int Depth => 1;
        protected override bool ShowLine => false;

        public override void Initialize()
        {
            returnServant = Program.instance.menu;
            base.Initialize();
        }

        protected override void ApplyShowArrangement(int preDepth)
        {
            base.ApplyShowArrangement(preDepth);
            RefreshButtonMaterials();
            GetUI<ReplaySelectorUI>().Print();
        }

        protected override void AfterHidingEvent()
        {
            base.AfterHidingEvent();
            GetUI<ReplaySelectorUI>().superScrollView.Clear();
        }

        public override void OnExit()
        {
            if (Program.exitOnReturn)
                Program.GameQuit();
            else
                Program.instance.ShiftToServant(returnServant);
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !UserInput.NeedDefaultSelect())
                return;

            if (lastSelectedReplayItem == null)
                return;
            lastSelectedReplayItem.GetSelectable().Select();
        }

        public void SelecLastReplayItem()
        {
            UserInput.NextSelectionIsAxis = true;
            Select();
        }

        protected override void FirstLoadEvent()
        {
            base.FirstLoadEvent();
            RefreshButtonMaterials();
            if(Program.exitOnReturn)
                GetUI<ReplaySelectorUI>().KF_Replay(replayName);
        }

        private void RefreshButtonMaterials()
        {
            var ui = GetUI<ReplaySelectorUI>();
            ui.ButtonGodView.ResetVisualState(true);
            _ = Program.instance.texture_.SetCommonShopButtonMaterial(ui.ImageOut, false);
            _ = Program.instance.texture_.SetCommonShopButtonMaterial(ui.ImageHover, true);
        }

        private string replayName;
        public void PlayReplay(string replayName)
        {
            if (servantUI == null)
            {
                this.replayName = replayName;
                Program.instance.ShiftToServant(this);
            }
            else
                GetUI<ReplaySelectorUI>().KF_Replay(replayName);
        }
    }
}
