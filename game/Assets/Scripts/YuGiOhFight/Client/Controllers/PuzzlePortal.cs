namespace YuGiOhFight.Client.Controllers
{
    public class PuzzlePortal : Servant
    {
        public const string PATH_PUZZLE_SE = Runtime.Program.PATH_PUZZLE + "SE/";
        public const string PATH_PUZZLE_CARD_EFFECT = "CardEffect/";
        public const string PATH_PUZZLE_FINAL_ATTACK = "FinalAttack/";
        public const string PATH_PUZZLE_SPECIAL_WIN = "SpecialWin/";
        public const string PATH_PUZZLE_TEST = "Test/";

        #region Servant

        public override int Depth => 1;
        public override float TransitionTime => 0.65f;
        protected override bool ShowLine => true;
        public override void Initialize()
        {
            returnServant = Runtime.Program.instance.menu;
            base.Initialize();
        }

        #endregion


    }
}
