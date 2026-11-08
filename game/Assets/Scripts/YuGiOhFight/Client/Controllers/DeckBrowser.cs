using UnityEngine;


namespace YuGiOhFight.Client.Controllers
{
    public class DeckBrowser : Servant
    {
        public override int Depth => 8;
        protected override bool ShowLine => true;
        protected override string Label_UI =>
            UI.PropertyOverride.PropertyOverrider.NeedMobileLayout()
            ? "ServantUI/DeckBrowserUIMobile.prefab" : "ServantUI/DeckBrowserUI.prefab";

        public UI.ServantUI.DeckBrowserUI.ResponseRegion ResponseRegion
        {
            get { return GetUI<UI.ServantUI.DeckBrowserUI>()._ResponseRegion; }
            set { GetUI<UI.ServantUI.DeckBrowserUI>()._ResponseRegion = value; }
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
                    returnServant = Runtime.Program.instance.appearance;
                    break;
            }
        }

        [HideInInspector] public UI.SelectionButton_CardInDeck lastSelectedCardInDeck;

        public override void Initialize()
        {
            Runtime.SystemEvent.OnResolutionChange += ChangeCanvasMatch;
            returnServant = Runtime.Program.instance.appearance;
            base.Initialize();
        }

        protected override void FirstLoadEvent()
        {
            base.FirstLoadEvent();
            GetUI<UI.ServantUI.DeckBrowserUI>().SetCondition(condition);
        }

        private void ChangeCanvasMatch()
        {
            if (!showing)
                return;

            Runtime.UIManager.SetCanvasMatch(Runtime.Program.instance.deckEditor.GetCanvasMatch(), 0f);
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !Runtime.UserInput.NeedDefaultSelect())
                return;

            if (lastSelectedCardInDeck != null)
                lastSelectedCardInDeck.GetSelectable().Select();
            else
                GetUI<UI.ServantUI.DeckBrowserUI>().DeckView.SelectDefaultItem();
        }
    }
}
