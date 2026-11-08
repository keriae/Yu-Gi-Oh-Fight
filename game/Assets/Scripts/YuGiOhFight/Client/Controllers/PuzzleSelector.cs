using UnityEngine;


namespace YuGiOhFight.Client.Controllers
{
    public class PuzzleSelector : Servant
    {

        [HideInInspector] public string currentPuzzle;
        [HideInInspector] public UI.SelectionToggle_Puzzle lastPuzzleItem;
        private Runtime.PercyOCG percy;

        #region Servant

        public override int Depth => 2;
        protected override bool ShowLine => false;

        public override void Initialize()
        {
            returnServant = Runtime.Program.instance.puzzlePortal;
            base.Initialize();
        }

        protected override void FirstLoadEvent()
        {
            base.FirstLoadEvent();
            _ = Runtime.Program.instance.texture_.SetCommonShopButtonMaterial(GetUI<UI.ServantUI.PuzzleSelectorUI>().ImageOut, false);
            _ = Runtime.Program.instance.texture_.SetCommonShopButtonMaterial(GetUI<UI.ServantUI.PuzzleSelectorUI>().ImageHover, true);
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !Runtime.UserInput.NeedDefaultSelect())
                return;

            lastPuzzleItem.GetSelectable().Select();
        }

        public override void OnExit()
        {
            if(Runtime.Program.exitOnReturn)
                Runtime.Program.GameQuit();
            else
                Runtime.Program.instance.ShiftToServant(returnServant);
        }

        #endregion

        public struct PuzzleGroup
        {
            public string pathPrefix;
            public string namePattern;
        }

        public PuzzleGroup puzzleGroup = new()
        {
            pathPrefix = string.Empty,
            namePattern = string.Empty
        };

        public void StartCurrentPuzzle(bool check = true)
        {
            //防止多次点击
            if (check && Runtime.Program.instance.currentServant != Runtime.Program.instance.puzzle)
                return;

            StartPuzzle(currentPuzzle);
        }

        public void StartPuzzle(string puzzle)
        {
            MDPro3.Config.Set("LastPlayPuzzle", puzzle);
            MDPro3.Config.Save();

            percy?.Dispose();
            percy = new Runtime.PercyOCG();
            percy.StartPuzzle(puzzle + Runtime.Program.EXPANSION_LUA);
        }

        public void SelectLastPuzzleItem()
        {
            Runtime.UserInput.NextSelectionIsAxis = true;
            Select();
        }
    }
}
