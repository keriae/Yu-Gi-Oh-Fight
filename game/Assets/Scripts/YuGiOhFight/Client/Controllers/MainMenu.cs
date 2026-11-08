using System.Collections.Generic;
using System;
using UnityEngine;


namespace YuGiOhFight.Client.Controllers
{
    public class MainMenu : Servant
    {
        [HideInInspector] public UI.SelectionButton_MainMenu lastSelectedButton;

        public override int Depth => 0;
        protected override bool ShowLine => false;
        protected override bool NeedExitButton => false;

        public override void Initialize()
        {
            base.Initialize();

            showing = true;
            Runtime.UIManager.HideExitButton(0f);
            Runtime.UIManager.HideLine(0f);
            Runtime.Program.instance.currentServant = this;
            Runtime.Program.instance.depth = 0;

            LoadUI();
        }

        public override void OnExit()
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

        protected override void FirstLoadEvent()
        {
            base.FirstLoadEvent();
            servantUI.ResetUI();
            HideUnavailableMenuItems();

            EnsureButtonsWidth();
            try { Runtime.Program.instance.ReadParams(); }
            catch (Exception error)
            {
                Debug.LogError("[FightPresentation] command line launch: " + error);
                UI.Client.FightStartupPresentation.ReportFailure("启动失败：" + error.Message);
            }
            finally { UI.Client.FightStartupPresentation.CommandLineProcessed(); }
            UI.Client.FightClient.Install();
        }

        protected override void ApplyShowArrangement(int preDepth)
        {
            base.ApplyShowArrangement(preDepth);

            Runtime.UIManager.ShowWallpaper(TransitionTime);
        }

        protected override void ApplyHideArrangement(int preDepth)
        {
            base.ApplyHideArrangement(preDepth);

            Runtime.UIManager.HideWallpaper(TransitionTime);
        }

        public override void PerFrameFunction()
        {
            if (NeedResponseInput())
            {
                if (Runtime.UserInput.WasCancelPressed)
                    OnReturn();

                if (GetUI<UI.ServantUI.MainMenuUI>().News.showing)
                {
                    if (Runtime.UserInput.WasLeftPressed)
                        GetUI<UI.ServantUI.MainMenuUI>().News.OnLeft();
                    else if (Runtime.UserInput.WasRightPressed)
                        GetUI<UI.ServantUI.MainMenuUI>().News.OnRight();

                    if (Runtime.UserInput.WasGamepadButtonWestPressed)
                        GetUI<UI.ServantUI.MainMenuUI>().News.OnNewsClick();
                    if (Runtime.UserInput.WasGamepadButtonNorthPressed)
                        GetUI<UI.ServantUI.MainMenuUI>().News.OnClose();
                }
            }
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !Runtime.UserInput.NeedDefaultSelect())
                return;

            if (lastSelectedButton != null)
                lastSelectedButton.GetSelectable().Select();
            else
                GetUI<UI.ServantUI.MainMenuUI>().SelectDefaultSelectable();
        }

        private void HideUnavailableMenuItems()
        {
            servantUI.Root.GetChild(1).Find("ButtonOnline").gameObject.SetActive(false);
            GetUI<UI.ServantUI.MainMenuUI>().News.gameObject.SetActive(false);
        }

        private UI.SelectionButton_MainMenu[] buttons;
        private UI.SelectionButton_MainMenu[] Buttons
            => buttons ??= GetButtons();

        private UI.SelectionButton_MainMenu[] GetButtons()
        {
            if (servantUI == null)
                return null;
            return transform.GetComponentsInChildren<UI.SelectionButton_MainMenu>(true);
        }

        public void EnsureButtonsWidth()
        {
            if (Buttons == null)
                return;

            var widths = new float[Buttons.Length];
            for (int i = 0; i < Buttons.Length; i++)
                widths[i] = buttons[i].GetPreferredWidth();
            var maxWidth = Mathf.Max(widths);
            foreach(UI.SelectionButton_MainMenu button in Buttons)
                button.SetWidth(maxWidth);
        }

    }
}
