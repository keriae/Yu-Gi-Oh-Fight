using MDPro3.UI.ServantUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MDPro3.UI
{
    public class SelectionToggle_AppearancePlayer : SelectionToggle
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
            Program.instance.appearance.GetUI<AppearanceUI>().SwitchPlayer(playerCode);
        }

        public void OnLeftSelection()
        {
            AudioManager.PlaySE(SoundLabelClick);
            var leftSelectable = Selectable.navigation.selectOnLeft;
            if (isOn)
            {
                if (leftSelectable != null)
                    leftSelectable.GetComponent<SelectionToggle_AppearancePlayer>().SetToggleOn();
            }
            else
            {
                if (leftSelectable != null)
                    leftSelectable.GetComponent<SelectionToggle_AppearancePlayer>().OnLeftSelection();
            }
        }
        public void OnRightSelection()
        {
            AudioManager.PlaySE(SoundLabelClick);
            var rightSelectable = Selectable.navigation.selectOnRight;
            if (isOn)
            {
                if (rightSelectable != null)
                    rightSelectable.GetComponent<SelectionToggle_AppearancePlayer>().SetToggleOn();
            }
            else
            {
                if (rightSelectable != null)
                    rightSelectable.GetComponent<SelectionToggle_AppearancePlayer>().OnRightSelection();
            }
        }
    }
}
