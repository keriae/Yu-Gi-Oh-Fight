using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace MDPro3.Utility
{
    /// <summary>
    /// Keep menu and background rendering bounded while preserving the duel FPS setting.
    /// Updates only on lifecycle, camera, or setting changes, never once per frame.
    /// </summary>
    internal static class RuntimeFramePacing
    {
        internal const int MenuFrameRate = 30;
        internal const int BackgroundFrameRate = 15;
        internal const int BackgroundRenderInterval = 3;
        // The pinned shipping player stripped the public setter, but retains
        // this field and the native GetRenderFrameInterval callback that reads it.
        // Cache the field once; no reflection runs in the per-frame game loop.
        private static readonly FieldInfo renderIntervalField = typeof(OnDemandRendering)
            .GetField("m_RenderFrameInterval", BindingFlags.Static | BindingFlags.NonPublic);
        private static bool warnedAboutRenderInterval;

        internal static int ConfiguredFrameRate { get; private set; } = 60;
        internal static int EffectiveFrameRate { get; private set; } = MenuFrameRate;
        internal static bool DuelRendering { get; private set; }
        internal static bool HasFocus { get; private set; } = true;
        internal static bool Paused { get; private set; }

        internal static void Initialize()
        {
            // Also covers entering SceneMain directly, without the boot scene.
            ConfiguredFrameRate = 60;
            DuelRendering = false;
            HasFocus = Application.isFocused;
            // Preserve a pause event received while transitioning out of Boot.
            // The shipping player already runs in the background; keep pumping
            // duel transport and AI responses at the background cap.
            Apply();
        }

        internal static void SetConfiguredFrameRate(int frameRate)
        {
            ConfiguredFrameRate = frameRate;
            Apply();
        }

        internal static void SetDuelRendering(bool enabled)
        {
            DuelRendering = enabled;
            Apply();
        }

        internal static void SetFocus(bool hasFocus)
        {
            HasFocus = hasFocus;
            Apply();
        }

        internal static void SetPaused(bool paused)
        {
            Paused = paused;
            Apply();
        }

        internal static int ResolveFrameRate(int configuredFrameRate, bool duelRendering,
            bool hasFocus, bool paused)
        {
            if (!hasFocus || paused)
                return BackgroundFrameRate;

            // Zero remains the saved UI sentinel for an uncapped foreground duel.
            if (duelRendering)
                return configuredFrameRate <= 0 ? -1 : configuredFrameRate;

            return configuredFrameRate <= 0 ? MenuFrameRate
                : Math.Min(configuredFrameRate, MenuFrameRate);
        }

        internal static void SetRenderFrameInterval(int interval)
        {
            if (renderIntervalField != null && renderIntervalField.FieldType == typeof(int))
            {
                renderIntervalField.SetValue(null, Math.Max(1, interval));
                return;
            }
            if (!warnedAboutRenderInterval)
            {
                warnedAboutRenderInterval = true;
                Debug.LogWarning("Background rendering interval is unavailable in this Unity player.");
            }
        }

        private static void Apply()
        {
            // Quality changes can restore vSync, so reapply it with the effective cap.
            QualitySettings.vSyncCount = 0;
            EffectiveFrameRate = ResolveFrameRate(ConfiguredFrameRate, DuelRendering, HasFocus, Paused);
            Application.targetFrameRate = EffectiveFrameRate;
            // Keep transport, AI, timers and animations updating at 15 Hz in the
            // background, but submit a picture only every third update. No
            // rendering is skipped once the window returns to the foreground.
            SetRenderFrameInterval(!HasFocus || Paused ? BackgroundRenderInterval : 1);
        }
    }
}
