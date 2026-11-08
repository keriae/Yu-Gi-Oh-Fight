using UnityEngine;


namespace YuGiOhFight.Client.Controllers
{
    public class DeckSelector : Servant
    {
        [HideInInspector] public UI.SelectionToggle_Deck lastSelectedDeckItem;

        public enum Condition
        {
            ForEdit,
            ForDuel,
            ForSolo,
            MyCard
        }
        public static Condition condition = Condition.ForEdit;
        public void SwitchCondition(Condition condition)
        {
            DeckSelector.condition = condition;
            switch (condition)
            {
                case Condition.ForEdit:
                    returnServant = Runtime.Program.instance.menu;
                    break;
                case Condition.ForDuel:
                    returnServant = Runtime.Program.instance.room;
                    break;
                case Condition.ForSolo:
                    returnServant = Runtime.Program.instance.solo;
                    break;
                case Condition.MyCard:
                    returnServant = Runtime.Program.instance.online;
                    break;
            }
        }

        public override int Depth => 4;
        protected override bool ShowLine => true;

        public string DeckType => GetUI<UI.ServantUI.DeckSelectorUI>().deckType;

        public override void Initialize()
        {
            base.Initialize();
            SwitchCondition(Condition.ForEdit);
        }

        public override void OnExit()
        {
            if (Runtime.Program.exitOnReturn)
                Runtime.Program.GameQuit();
            else
                Runtime.Program.instance.ShiftToServant(returnServant);
        }

        public override void PerFrameFunction()
        {
            if (!showing) return;
            if (NeedResponseInput())
            {

                if (Runtime.UserInput.WasLeftStickPressed)
                    GetUI<UI.ServantUI.DeckSelectorUI>().TogglePickupCard.SwitchToggle();

                if (Runtime.UserInput.WasGamepadButtonWestPressed)
                {
                    Runtime.AudioManager.PlaySE("SE_MENU_SELECT_01");
                    if (GetUI<UI.ServantUI.DeckSelectorUI>().ButtonOnline.gameObject.activeSelf)
                        GetUI<UI.ServantUI.DeckSelectorUI>().OnOnlineDeckView();
                    else
                        GetUI<UI.ServantUI.DeckSelectorUI>().OnConfirm();
                }
                if (Runtime.UserInput.WasGamepadButtonNorthPressed)
                {
                    Runtime.AudioManager.PlaySE("SE_MENU_SELECT_01");
                    GetUI<UI.ServantUI.DeckSelectorUI>().ActivateInputField();
                }
                if (Runtime.UserInput.WasLeftShoulderPressed)
                {
                    if (GetUI<UI.ServantUI.DeckSelectorUI>().ButtonType.gameObject.activeSelf)
                    {
                        Runtime.AudioManager.PlaySE("SE_MENU_SELECT_01");
                        GetUI<UI.ServantUI.DeckSelectorUI>().OnType();
                    }
                }
                if (Runtime.UserInput.WasRightShoulderPressed)
                {
                    if (GetUI<UI.ServantUI.DeckSelectorUI>().ButtonOnline.gameObject.activeSelf)
                    {
                        Runtime.AudioManager.PlaySE("SE_MENU_SELECT_01");
                        GetUI<UI.ServantUI.DeckSelectorUI>().OnDelete();
                    }
                }
                if (Runtime.UserInput.MouseRightDown || Runtime.UserInput.WasCancelPressed)
                {
                    if (GetUI<UI.ServantUI.DeckSelectorUI>().ButtonOnline.gameObject.activeSelf)
                        OnReturn();
                    else
                    {
                        Runtime.AudioManager.PlaySE("SE_MENU_CANCEL");
                        GetUI<UI.ServantUI.DeckSelectorUI>().OnCancel();
                    }
                }
            }
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !Runtime.UserInput.NeedDefaultSelect())
                return;

            if (lastSelectedDeckItem != null) lastSelectedDeckItem.GetSelectable().Select();
            else GetUI<UI.ServantUI.DeckSelectorUI>().ButtonOnline.GetSelectable().Select();
        }

        public override bool NeedResponseInput()
        {
            if(servantUI == null
                || GetUI<UI.ServantUI.DeckSelectorUI>().buttonLayoutSwitching)
                return false;
            return base.NeedResponseInput();
        }

        public void SelectLastDeckItem()
        {
            Runtime.UserInput.NextSelectionIsAxis = true;
            Select();
        }

    }
}
