using UnityEngine;
using UnityEngine.InputSystem;


namespace YuGiOhFight.Client.Services
{
    internal static class RuntimeDisplay
    {
        private static Vector2Int windowSize = new Vector2Int(1280, 800);

        internal static FullScreenMode ResolveMode(string mode, RuntimePlatform platform)
        {
            if (mode == "2") return FullScreenMode.Windowed;
            if (mode == "0" && (platform == RuntimePlatform.WindowsPlayer || platform == RuntimePlatform.WindowsEditor))
                return FullScreenMode.ExclusiveFullScreen;
            return platform == RuntimePlatform.OSXPlayer || platform == RuntimePlatform.OSXEditor
                ? FullScreenMode.MaximizedWindow : FullScreenMode.FullScreenWindow;
        }

        internal static Vector2Int FitWindow(int width, int height, int displayWidth, int displayHeight)
        {
            // Leave room for the window controls, menu bar and Dock.
            float scale = Mathf.Min(1, Mathf.Min(Mathf.Min(1920, displayWidth * .8f) / Mathf.Max(1, width),
                Mathf.Min(1080, displayHeight * .8f) / Mathf.Max(1, height)));
            return new Vector2Int(Mathf.Max(320, Mathf.RoundToInt(width * scale)), Mathf.Max(240, Mathf.RoundToInt(height * scale)));
        }

        internal static bool IsFullscreen => Screen.fullScreen || Screen.currentResolution.width > 0
            && Screen.currentResolution.height > 0 && Screen.width >= Screen.currentResolution.width * .96f
            && Screen.height >= Screen.currentResolution.height * .96f;

        internal static void Initialize()
        {
            if (Application.isMobilePlatform) return;
            if (int.TryParse(MDPro3.Config.Get("WindowWidth", "1280"), out int width)
                && int.TryParse(MDPro3.Config.Get("WindowHeight", "800"), out int height) && width > 0 && height > 0)
                windowSize = new Vector2Int(width, height);
            ApplySavedMode();
        }

        internal static void ApplySavedMode()
        {
            var size = MDPro3.Config.Get("Resolution", Screen.currentResolution.width + " x " + Screen.currentResolution.height).Split('x');
            if (size.Length == 2 && int.TryParse(size[0].Trim(), out int width) && int.TryParse(size[1].Trim(), out int height))
                Apply(width, height, MDPro3.Config.Get("ScreenMode", "1"));
        }

        internal static void Apply(int width, int height, string mode)
        {
            var fullscreenMode = ResolveMode(mode, Application.platform);
            if (fullscreenMode == FullScreenMode.Windowed)
            {
                var fitted = FitWindow(width, height, Screen.currentResolution.width, Screen.currentResolution.height);
                windowSize = fitted;
                Screen.SetResolution(fitted.x, fitted.y, fullscreenMode);
            }
            else Screen.SetResolution(width, height, fullscreenMode);
        }

        internal static void ToggleFullscreen()
        {
            if (Application.isMobilePlatform) return;
            if (IsFullscreen)
            {
                MDPro3.Config.Set("ScreenMode", "2");
                Apply(windowSize.x, windowSize.y, "2");
            }
            else
            {
                windowSize = new Vector2Int(Screen.width, Screen.height);
                MDPro3.Config.Set("WindowWidth", windowSize.x.ToString()); MDPro3.Config.Set("WindowHeight", windowSize.y.ToString());
                MDPro3.Config.Set("ScreenMode", "1");
                Apply(Screen.currentResolution.width, Screen.currentResolution.height, "1");
            }
            MDPro3.Config.Save();
        }

        internal static void PerFrame()
        {
            if (ReplayVideoExport.Busy || !Application.isFocused || Application.isMobilePlatform) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            bool macShortcut = Application.platform == RuntimePlatform.OSXPlayer
                && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
                && (keyboard.leftMetaKey.isPressed || keyboard.rightMetaKey.isPressed) && keyboard.fKey.wasPressedThisFrame;
            if (keyboard.f11Key.wasPressedThisFrame || macShortcut) ToggleFullscreen();
        }
    }
}
