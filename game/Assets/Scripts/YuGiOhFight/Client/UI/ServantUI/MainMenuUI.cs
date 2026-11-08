using UnityEngine;


using System.Collections.Generic;


namespace YuGiOhFight.Client.UI.ServantUI
{
    public class MainMenuUI : ServantUI
    {
        #region Elements

        private const string LABEL_MYCARD_NEWS = "News";
        private Runtime.NewsManager m_News;
        public Runtime.NewsManager News =>
            m_News = m_News != null ? m_News
            : Manager.GetElement<Runtime.NewsManager>(LABEL_MYCARD_NEWS);

        #endregion

        public override void Initialize(Controllers.Servant servant)
        {
            base.Initialize(servant);

            Title.text = "MDPro3 v" + Application.version;
        }

        #region Button Function

        public void OnSolo()
        {
            if (Runtime.Program.exitOnReturn)
                return;

            Runtime.Program.instance.solo.SwitchCondition(Controllers.SoloSelector.Condition.ForSolo);
            Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.solo);
        }

        public void OnOnline()
        {
        }

        public void OnPuzzle()
        {
            if (Runtime.Program.exitOnReturn)
                return;

            Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.puzzlePortal);
        }

        public void OnReplay()
        {
            if (Runtime.Program.exitOnReturn)
                return;

            Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.replay);
        }

        public void OnCutin()
        {
            if (Runtime.Program.exitOnReturn)
                return;

            Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.cutin);
        }
        public void OnMate()
        {
            if (Runtime.Program.exitOnReturn)
                return;

            Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.mate);
        }

        public void OnDeck()
        {
            if (Runtime.Program.exitOnReturn)
                return;

            Runtime.Program.instance.deckSelector.SwitchCondition(Controllers.DeckSelector.Condition.ForEdit);
            Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.deckSelector);
        }

        public void OnSetting()
        {
            if (Runtime.Program.exitOnReturn)
                return;

            Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.setting);
        }

        public void OnExit()
        {
            if (Runtime.Program.exitOnReturn)
                return;

            List<string> selections = new()
            {
                MDPro3.InterString.Get("确认退出"),
                MDPro3.InterString.Get("即将退出应用程序，@n是否确认？"),
                MDPro3.InterString.Get("确认"),
                MDPro3.InterString.Get("取消"),
                MDPro3.Config.STRING_YES
            };
            Runtime.UIManager.ShowPopupYesOrNo(selections, Runtime.Program.GameQuit, null);
        }

        #endregion
    }
}
