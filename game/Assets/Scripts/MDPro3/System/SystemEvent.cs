using UnityEngine;

namespace MDPro3
{
    public class SystemEvent : MonoBehaviour
    {
        public delegate void SafeAreaUpdate();
        public static event SafeAreaUpdate OnSafeAreaUpdate;

        public delegate void ResolutionChange();
        public static event ResolutionChange OnResolutionChange;

        public delegate void LanguageChange();
        public static event LanguageChange OnLanguageChange;

        public delegate void VideoCardConfigChange();
        public static event VideoCardConfigChange OnVideoCardConfigChange;

        private Rect safeArea;
        private int screenWidth;
        private int screenHeight;
        private void Start()
        {
            safeArea = Screen.safeArea;
            screenWidth = Screen.width;
            screenHeight = Screen.height;
        }

        private void Update()
        {
            if (safeArea != Screen.safeArea)
            {
                safeArea = Screen.safeArea;
                OnSafeAreaUpdate?.Invoke();
            }
            if (screenWidth != Screen.width || screenHeight != Screen.height)
            {
                screenWidth = Screen.width;
                screenHeight = Screen.height;
                OnResolutionChange?.Invoke();
            }
        }

        public static void CallLanguageChangeEvent()
        {
            OnLanguageChange?.Invoke();
        }

        public static void CallVideoCardConfigChangeEvent()
        {
            OnVideoCardConfigChange?.Invoke();
        }
    }
}