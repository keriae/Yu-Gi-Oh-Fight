using UnityEngine;


namespace YuGiOhFight.Client.Controllers
{
    public class ReplaySelector : Servant
    {

        [HideInInspector] public UI.SelectionToggle_Replay lastSelectedReplayItem;

        public override int Depth => 1;
        protected override bool ShowLine => false;

        public override void Initialize()
        {
            returnServant = Runtime.Program.instance.menu;
            base.Initialize();
        }

        protected override void ApplyShowArrangement(int preDepth)
        {
            base.ApplyShowArrangement(preDepth);
            RefreshButtonMaterials();
            GetUI<UI.ServantUI.ReplaySelectorUI>().Print();
        }

        protected override void AfterHidingEvent()
        {
            base.AfterHidingEvent();
            GetUI<UI.ServantUI.ReplaySelectorUI>().superScrollView.Clear();
        }

        public override void OnExit()
        {
            if (Runtime.Program.exitOnReturn)
                Runtime.Program.GameQuit();
            else
                Runtime.Program.instance.ShiftToServant(returnServant);
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !Runtime.UserInput.NeedDefaultSelect())
                return;

            if (lastSelectedReplayItem == null)
                return;
            lastSelectedReplayItem.GetSelectable().Select();
        }

        public void SelecLastReplayItem()
        {
            Runtime.UserInput.NextSelectionIsAxis = true;
            Select();
        }

        protected override void FirstLoadEvent()
        {
            base.FirstLoadEvent();
            RefreshButtonMaterials();
            if(Runtime.Program.exitOnReturn)
                GetUI<UI.ServantUI.ReplaySelectorUI>().KF_Replay(replayName);
        }

        private void RefreshButtonMaterials()
        {
            UI.ServantUI.ReplaySelectorUI ui = GetUI<UI.ServantUI.ReplaySelectorUI>();
            ui.ButtonGodView.ResetVisualState(true);
            _ = Runtime.Program.instance.texture_.SetCommonShopButtonMaterial(ui.ImageOut, false);
            _ = Runtime.Program.instance.texture_.SetCommonShopButtonMaterial(ui.ImageHover, true);
        }

        private string replayName;
        public void PlayReplay(string replayName)
        {
            if (servantUI == null)
            {
                this.replayName = replayName;
                Runtime.Program.instance.ShiftToServant(this);
            }
            else
                GetUI<UI.ServantUI.ReplaySelectorUI>().KF_Replay(replayName);
        }
    }
}
