using MDPro3.UI.ServantUI;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI
{
    public class SelectionToggle_Rarity : SelectionToggle
    {
        [Header("SelectionToggle Rarity")]
        [SerializeField] public CardRarity.Rarity rarity;

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
            Program.instance.deckEditor.GetUI<DeckEditorUI>().ChangeRarity(rarity);
        }

        protected override void CallToggleOffEvent()
        {
            base.CallToggleOffEvent();
            Program.instance.deckEditor.GetUI<DeckEditorUI>().ChangeRarity(CardRarity.Rarity.Normal);
        }
    }
}
