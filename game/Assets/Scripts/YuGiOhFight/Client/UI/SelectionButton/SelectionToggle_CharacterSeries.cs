using UnityEngine;
using UnityEngine.EventSystems;


namespace YuGiOhFight.Client.UI
{
    public class SelectionToggle_CharacterSeries : SelectionToggle
    {
        [SerializeField] private string serialIndex;

        protected override void Awake()
        {
            base.Awake();
            exclusiveToggle = true;
            canToggleOffSelf = false;
            toggleWhenSelected = true;
        }

        protected override void CallToggleOnEvent()
        {
            base.CallToggleOnEvent();
            Runtime.Program.instance.character.GetUI<ServantUI.CharacterSelectorUI>().SetHoverText(string.Empty);

            Runtime.Program.instance.character.GetUI<ServantUI.CharacterSelectorUI>().ShowCharacters(serialIndex);
            Runtime.Program.instance.character.lastSelectedToggle = this;
        }

        protected override void OnSubmit()
        {
            base.OnSubmit();

            Runtime.UserInput.NextSelectionIsAxis = true;
            var target = Runtime.Program.instance.character.lastSelectedCharacter.gameObject;
            if (!target.activeSelf)
                target = Runtime.Program.instance.character.GetUI<ServantUI.CharacterSelectorUI>().GetFirstActiveCharacterItem();
            EventSystem.current.SetSelectedGameObject(target);
        }


    }
}
