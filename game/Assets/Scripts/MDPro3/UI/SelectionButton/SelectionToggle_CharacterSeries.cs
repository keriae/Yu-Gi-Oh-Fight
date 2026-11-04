using MDPro3.UI.ServantUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MDPro3.UI
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
            Program.instance.character.GetUI<CharacterSelectorUI>().SetHoverText(string.Empty);

            Program.instance.character.GetUI<CharacterSelectorUI>().ShowCharacters(serialIndex);
            Program.instance.character.lastSelectedToggle = this;
        }

        protected override void OnSubmit()
        {
            base.OnSubmit();

            UserInput.NextSelectionIsAxis = true;
            var target = Program.instance.character.lastSelectedCharacter.gameObject;
            if (!target.activeSelf)
                target = Program.instance.character.GetUI<CharacterSelectorUI>().GetFirstActiveCharacterItem();
            EventSystem.current.SetSelectedGameObject(target);
        }


    }
}
