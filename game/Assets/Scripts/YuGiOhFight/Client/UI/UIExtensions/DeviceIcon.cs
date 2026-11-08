using UnityEngine;


namespace YuGiOhFight.Client.UI
{
    public class DeviceIcon : MonoBehaviour
    {
        public enum InputDevice
        {
            PointingDevice = 0,
            GamePad = 1
        }
        public InputDevice displayInputDevice;

        public enum DispType
        {
            Graphic = 0,
            GameObject = 1
        }
        public DispType dispTarget;

        private void Awake()
        {
            Runtime.UserInput.OnControlDeviceChange += OnControlDeviceChange;
            OnControlDeviceChange(Runtime.UserInput.PlayerInput.currentControlScheme);
        }

        private void OnDestroy()
        {
            Runtime.UserInput.OnControlDeviceChange -= OnControlDeviceChange;
        }

        private void OnControlDeviceChange(string scheme)
        {
            if(displayInputDevice == InputDevice.GamePad)
            {
                if (scheme == Runtime.UserInput.GamepadSchemeName)
                    Show();
                else
                    Hide();
            }
            else
            {
                if (scheme == Runtime.UserInput.GamepadSchemeName)
                    Hide();
                else
                    Show();
            }
        }

        private void Show()
        {
            if (dispTarget == DispType.Graphic)
            {
                var cg = GetComponent<CanvasGroup>();
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
            }
            else
            {
                gameObject.SetActive(true);
            }
        }

        private void Hide()
        {
            if (dispTarget == DispType.Graphic)
            {
                var cg = GetComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg.blocksRaycasts = false;
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}