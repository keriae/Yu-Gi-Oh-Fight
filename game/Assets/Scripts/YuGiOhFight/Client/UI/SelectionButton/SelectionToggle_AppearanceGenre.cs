using UnityEngine.EventSystems;


namespace YuGiOhFight.Client.UI
{
    public class SelectionToggle_AppearanceGenre : SelectionToggle
    {

        protected override void Awake()
        {
            base.Awake();

            exclusiveToggle = true;
            canToggleOffSelf = false;
            toggleWhenSelected = true;
        }

        public override void SetToggleOn(bool callEvent = true)
        {
            if (!isOn)
                Runtime.Program.instance.appearance.GetUI<ServantUI.AppearanceUI>().ScrollRect.verticalScrollbar.value = 1f;
            base.SetToggleOn();
        }

        protected override void CallToggleOnEvent()
        {
            base.CallToggleOnEvent();
            Runtime.Program.instance.appearance.GetUI<ServantUI.AppearanceUI>().ShowItems(name.Split(" ")[1]);
            Runtime.Program.instance.appearance.lastSelectedToggle = this;
        }

        protected override void OnSubmit()
        {
            base.OnSubmit();

            Runtime.UserInput.NextSelectionIsAxis = true;
            var target = Runtime.Program.instance.appearance.GetUI<ServantUI.AppearanceUI>().GetCurrentContentItem();
            if (target == null)
                return;
            EventSystem.current.SetSelectedGameObject(target);
        }
    }
}
