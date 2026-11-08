using UnityEngine;


namespace YuGiOhFight.Client.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class GamepadCursor : MonoBehaviour
    {
        private bool _show = true;
        public bool Show
        {
            get { return _show; }
            set
            {
                _show = value;
                OnControlDeviceChange(Runtime.UserInput.PlayerInput.currentControlScheme);
            }
        }

        private void OnEnable()
        {
            Runtime.UserInput.OnControlDeviceChange += OnControlDeviceChange;
            OnControlDeviceChange(Runtime.UserInput.PlayerInput.currentControlScheme);
        }

        private void OnDisable()
        {
            Runtime.UserInput.OnControlDeviceChange -= OnControlDeviceChange;
        }

        private void OnControlDeviceChange(string scheme)
        {
            if (scheme == Runtime.UserInput.GamepadSchemeName && Show)
                GetComponent<CanvasGroup>().alpha = 1f;
            else
                GetComponent<CanvasGroup>().alpha = 0f;
        }
    }
}
