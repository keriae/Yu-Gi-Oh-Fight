using UnityEngine;

using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
{
    public class SelectionToggle_CharacterPlayer : SelectionToggle
    {
        [SerializeField] private string playerCode;

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
            Runtime.Program.instance.character.GetUI<ServantUI.CharacterSelectorUI>().SwitchPlayer(playerCode);
        }

        public void OnLeftSelection()
        {
            Runtime.AudioManager.PlaySE(SoundLabelClick);
            var leftSelectable = Selectable.navigation.selectOnLeft;
            if (isOn)
            {
                if(leftSelectable != null)
                    leftSelectable.GetComponent<SelectionToggle_CharacterPlayer>().SetToggleOn();
            }
            else
            {
                if (leftSelectable != null)
                    leftSelectable.GetComponent<SelectionToggle_CharacterPlayer>().OnLeftSelection();
            }
        }
        public void OnRightSelection()
        {
            Runtime.AudioManager.PlaySE(SoundLabelClick);
            var rightSelectable = Selectable.navigation.selectOnRight;
            if (isOn)
            {
                if (rightSelectable != null)
                    rightSelectable.GetComponent<SelectionToggle_CharacterPlayer>().SetToggleOn();
            }
            else
            {
                if (rightSelectable != null)
                    rightSelectable.GetComponent<SelectionToggle_CharacterPlayer>().OnRightSelection();
            }
        }
    }
}
