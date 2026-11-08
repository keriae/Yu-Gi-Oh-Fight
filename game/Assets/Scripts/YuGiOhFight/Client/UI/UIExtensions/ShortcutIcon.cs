using System.Collections;
using UnityEngine;

using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(LayoutElement))]
    public class ShortcutIcon : MonoBehaviour
    {
        public enum GamePadButton
        {
            ButtonSouth,
            ButtonEast,
            ButtonWest,
            ButtonNorth,
            LeftShoulder,
            RightShoulder,
            LeftTrigger,
            RightTrigger,
            LeftStick,
            RightStick,
            Select,
            Start
        }
        public GamePadButton button;
        public GamePadButton button2;

        public Image Image;
        public Image Image2;

        private bool show = true;
        public bool Show
        {
            get {  return show; }
            set
            {
                show = value;
                ChangeShortcutIcon(string.Empty);
            }
        }

        private bool groupShow = true;
        public bool GroupShow
        {
            get { return groupShow; }
            set
            {
                groupShow = value;
                ChangeShortcutIcon(string.Empty);
            }
        }

        private void OnEnable()
        {
            Runtime.UserInput.OnControlDeviceChange += ChangeShortcutIcon;
            ChangeShortcutIcon(string.Empty);
        }
        private void OnDisable()
        {
            Runtime.UserInput.OnControlDeviceChange -= ChangeShortcutIcon;
        }

        private void ChangeShortcutIcon(string scheme)
        {
            if (!gameObject.activeInHierarchy) return;

            if(Runtime.UserInput.gamepadType == Runtime.UserInput.GamepadType.None || !Show || !GroupShow)
            {
                GetComponent<CanvasGroup>().alpha = 0f;
                GetComponent<LayoutElement>().ignoreLayout = true;
            }
            else
                StartCoroutine(SetIconAsync());
        }

        private IEnumerator SetIconAsync()
        {
            GetComponent<CanvasGroup>().alpha = 0f;
            GetComponent<LayoutElement>().ignoreLayout = true;

            while (Runtime.TextureManager.container == null)
                yield return null;

            var icon = Runtime.TextureManager.container.GetGamepadIcon(button);
            if(icon != null)
                Image.sprite = icon;

            if (Image2 != null)
            {
                var icon2 = Runtime.TextureManager.container.GetGamepadIcon(button2);
                if (icon2 != null)
                    Image2.sprite = icon2;
            }

            GetComponent<CanvasGroup>().alpha = 1f;
            GetComponent<LayoutElement>().ignoreLayout = false;
        }
    }
}
