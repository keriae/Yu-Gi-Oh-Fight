using UnityEngine;
using MDPro3.UI;
using MDPro3.UI.ServantUI;

namespace MDPro3.Servant
{
    public class DeckSelector : Servant
    {
        [HideInInspector] public SelectionToggle_Deck lastSelectedDeckItem;

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
                    returnServant = Program.instance.menu;
                    break;
                case Condition.ForDuel:
                    returnServant = Program.instance.room;
                    break;
                case Condition.ForSolo:
                    returnServant = Program.instance.solo;
                    break;
                case Condition.MyCard:
                    returnServant = Program.instance.online;
                    break;
            }
        }

        public override int Depth => 4;
        protected override bool ShowLine => true;

        public string DeckType => GetUI<DeckSelectorUI>().deckType;

        public override void Initialize()
        {
            base.Initialize();
            SwitchCondition(Condition.ForEdit);
        }

        public override void OnExit()
        {
            if (Program.exitOnReturn)
                Program.GameQuit();
            else
                Program.instance.ShiftToServant(returnServant);
        }

        public override void PerFrameFunction()
        {
            if (!showing) return;
            if (NeedResponseInput())
            {

                if (UserInput.WasLeftStickPressed)
                    GetUI<DeckSelectorUI>().TogglePickupCard.SwitchToggle();

                if (UserInput.WasGamepadButtonWestPressed)
                {
                    AudioManager.PlaySE("SE_MENU_SELECT_01");
                    if (GetUI<DeckSelectorUI>().ButtonOnline.gameObject.activeSelf)
                        GetUI<DeckSelectorUI>().OnOnlineDeckView();
                    else
                        GetUI<DeckSelectorUI>().OnConfirm();
                }
                if (UserInput.WasGamepadButtonNorthPressed)
                {
                    AudioManager.PlaySE("SE_MENU_SELECT_01");
                    GetUI<DeckSelectorUI>().ActivateInputField();
                }
                if (UserInput.WasLeftShoulderPressed)
                {
                    if (GetUI<DeckSelectorUI>().ButtonType.gameObject.activeSelf)
                    {
                        AudioManager.PlaySE("SE_MENU_SELECT_01");
                        GetUI<DeckSelectorUI>().OnType();
                    }
                }
                if (UserInput.WasRightShoulderPressed)
                {
                    if (GetUI<DeckSelectorUI>().ButtonOnline.gameObject.activeSelf)
                    {
                        AudioManager.PlaySE("SE_MENU_SELECT_01");
                        GetUI<DeckSelectorUI>().OnDelete();
                    }
                }
                if (UserInput.MouseRightDown || UserInput.WasCancelPressed)
                {
                    if (GetUI<DeckSelectorUI>().ButtonOnline.gameObject.activeSelf)
                        OnReturn();
                    else
                    {
                        AudioManager.PlaySE("SE_MENU_CANCEL");
                        GetUI<DeckSelectorUI>().OnCancel();
                    }
                }
            }
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !UserInput.NeedDefaultSelect())
                return;

            if (lastSelectedDeckItem != null) lastSelectedDeckItem.GetSelectable().Select();
            else GetUI<DeckSelectorUI>().ButtonOnline.GetSelectable().Select();
        }

        public override bool NeedResponseInput()
        {
            if(servantUI == null
                || GetUI<DeckSelectorUI>().buttonLayoutSwitching)
                return false;
            return base.NeedResponseInput();
        }

        public void SelectLastDeckItem()
        {
            UserInput.NextSelectionIsAxis = true;
            Select();
        }

    }
}
