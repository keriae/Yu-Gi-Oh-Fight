using MDPro3.UI;
using MDPro3.UI.ServantUI;
using UnityEngine;

namespace MDPro3.Servant
{
    public class PuzzleSelector : Servant
    {

        [HideInInspector] public string currentPuzzle;
        [HideInInspector] public SelectionToggle_Puzzle lastPuzzleItem;
        private PercyOCG percy;

        #region Servant

        public override int Depth => 2;
        protected override bool ShowLine => false;

        public override void Initialize()
        {
            returnServant = Program.instance.puzzlePortal;
            base.Initialize();
        }

        protected override void FirstLoadEvent()
        {
            base.FirstLoadEvent();
            _ = Program.instance.texture_.SetCommonShopButtonMaterial(GetUI<PuzzleSelectorUI>().ImageOut, false);
            _ = Program.instance.texture_.SetCommonShopButtonMaterial(GetUI<PuzzleSelectorUI>().ImageHover, true);
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !UserInput.NeedDefaultSelect())
                return;

            lastPuzzleItem.GetSelectable().Select();
        }

        public override void OnExit()
        {
            if(Program.exitOnReturn)
                Program.GameQuit();
            else
                Program.instance.ShiftToServant(returnServant);
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
            if (check && Program.instance.currentServant != Program.instance.puzzle)
                return;

            StartPuzzle(currentPuzzle);
        }

        public void StartPuzzle(string puzzle)
        {
            Config.Set("LastPlayPuzzle", puzzle);
            Config.Save();

            percy?.Dispose();
            percy = new PercyOCG();
            percy.StartPuzzle(puzzle + Program.EXPANSION_LUA);
        }

        public void SelectLastPuzzleItem()
        {
            UserInput.NextSelectionIsAxis = true;
            Select();
        }
    }
}
