using UnityEngine;


namespace YuGiOhFight.Client.UI
{
    public class SelectionToggle_Rarity : SelectionToggle
    {
        [Header("SelectionToggle Rarity")]
        [SerializeField] public MDPro3.CardRarity.Rarity rarity;

        protected override void Awake()
        {
            base.Awake();

            exclusiveToggle = true;
            exclusiveCallOffEvent = false;
            canToggleOffSelf = true;
            toggleWhenSelected = false;
        }

        public override void SetToggleOff(bool fromSelf = true)
        {
            isOn = false;
            ToggleOff();
            if(fromSelf)
                CallToggleOffEvent();
        }

        protected override void CallToggleOnEvent()
        {
            base.CallToggleOnEvent();
            Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().ChangeRarity(rarity);
        }

        protected override void CallToggleOffEvent()
        {
            base.CallToggleOffEvent();
            Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().ChangeRarity(MDPro3.CardRarity.Rarity.Normal);
        }
    }
}
