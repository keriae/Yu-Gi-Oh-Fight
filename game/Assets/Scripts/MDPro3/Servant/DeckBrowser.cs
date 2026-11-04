using MDPro3.UI;
using MDPro3.UI.PropertyOverride;
using MDPro3.UI.ServantUI;
using UnityEngine;
using static MDPro3.UI.ServantUI.DeckBrowserUI;

namespace MDPro3.Servant
{
    public class DeckBrowser : Servant
    {
        public override int Depth => 8;
        protected override bool ShowLine => true;
        protected override string Label_UI =>
            PropertyOverrider.NeedMobileLayout()
            ? "ServantUI/DeckBrowserUIMobile.prefab" : "ServantUI/DeckBrowserUI.prefab";

        public ResponseRegion ResponseRegion
        {
            get { return GetUI<DeckBrowserUI>()._ResponseRegion; }
            set { GetUI<DeckBrowserUI>()._ResponseRegion = value; }
        }
        public enum Condition
        {
            ChangePickup
        }

        public Condition condition;
        public void SwitchCondition(Condition condition)
        {
            this.condition = condition;
            switch (condition)
            {
                case Condition.ChangePickup:
                    returnServant = Program.instance.appearance;
                    break;
            }
        }

        [HideInInspector] public SelectionButton_CardInDeck lastSelectedCardInDeck;

        public override void Initialize()
        {
            SystemEvent.OnResolutionChange += ChangeCanvasMatch;
            returnServant = Program.instance.appearance;
            base.Initialize();
        }

        protected override void FirstLoadEvent()
        {
            base.FirstLoadEvent();
            GetUI<DeckBrowserUI>().SetCondition(condition);
        }

        private void ChangeCanvasMatch()
        {
            if (!showing)
                return;

            UIManager.SetCanvasMatch(Program.instance.deckEditor.GetCanvasMatch(), 0f);
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !UserInput.NeedDefaultSelect())
                return;

            if (lastSelectedCardInDeck != null)
                lastSelectedCardInDeck.GetSelectable().Select();
            else
                GetUI<DeckBrowserUI>().DeckView.SelectDefaultItem();
        }
    }
}
