using System;
using System.Runtime.CompilerServices;
// Unity retains these native capture-clock bindings in the pinned player,
// although its managed declarations were stripped from UnityEngine.Time.
// This separate assembly is invoked explicitly; it never replaces that type.
namespace UnityEngine
{
    public static class Time
    {
        [MethodImpl(MethodImplOptions.InternalCall)] public static extern float get_captureDeltaTime();
        [MethodImpl(MethodImplOptions.InternalCall)] public static extern void set_captureDeltaTime(float seconds);
    }
    public static class AudioRenderer
    {
        [MethodImpl(MethodImplOptions.InternalCall)] public static extern bool Internal_AudioRenderer_Start();
        [MethodImpl(MethodImplOptions.InternalCall)] public static extern bool Internal_AudioRenderer_Stop();
        [MethodImpl(MethodImplOptions.InternalCall)] public static extern int Internal_AudioRenderer_GetSampleCountForCaptureFrame();
        [MethodImpl(MethodImplOptions.InternalCall)] public static extern bool Internal_AudioRenderer_Render(IntPtr samples, int length);
    }
    public static class AudioSettings
    {
        [MethodImpl(MethodImplOptions.InternalCall)] public static extern int GetSampleRate();
        [MethodImpl(MethodImplOptions.InternalCall)] public static extern int GetSpeakerMode();
    }
}
