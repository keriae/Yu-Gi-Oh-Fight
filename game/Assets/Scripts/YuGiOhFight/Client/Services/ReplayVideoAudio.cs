using System;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace YuGiOhFight.Client.Services
{
    // Unity renders its complete audio mix on the capture clock. This includes
    // compressed clips, one-shots, voices, music and timeline audio effects.
    internal static class ReplayVideoAudio
    {
        private static MethodInfo start, stop, count, render;
        private static int channels;
        internal static int SampleRate { get; private set; }
        private static bool started;
        internal static void Begin()
        {
            var type = ReplayVideoClock.Bindings("AudioRenderer");
            start = type.GetMethod("Internal_AudioRenderer_Start"); stop = type.GetMethod("Internal_AudioRenderer_Stop");
            count = type.GetMethod("Internal_AudioRenderer_GetSampleCountForCaptureFrame"); render = type.GetMethod("Internal_AudioRenderer_Render");
            var settings = ReplayVideoClock.Bindings("AudioSettings");
            SampleRate = (int)settings.GetMethod("GetSampleRate").Invoke(null, null);
            int mode = (int)settings.GetMethod("GetSpeakerMode").Invoke(null, null);
            channels = mode == 1 ? 1 : mode == 2 || mode == 7 ? 2 : mode == 3 ? 4 : mode == 4 ? 5 : mode == 5 ? 6 : mode == 6 ? 8 : 0;
            if (SampleRate < 8000 || SampleRate > 192000 || channels == 0)
                throw new InvalidOperationException("当前音频输出格式不支持视频导出。");
            if (!(bool)start.Invoke(null, null)) throw new InvalidOperationException("无法启动游戏音频导出。");
            started = true;
            Debug.Log("[ReplayVideoExport] Unity audio capture: " + SampleRate + " Hz, " + channels + " channels");
        }
        internal static float[] Mix(out int frames)
        {
            frames = (int)count.Invoke(null, null);
            if (frames < 0 || frames > SampleRate * 2) throw new InvalidOperationException("游戏音频帧计数无效。");
            var input = new float[checked(frames * channels)];
            var pinned = GCHandle.Alloc(input, GCHandleType.Pinned);
            try
            {
                if (!(bool)render.Invoke(null, new object[] { pinned.AddrOfPinnedObject(), input.Length }))
                    throw new InvalidOperationException("游戏音频渲染失败。");
            }
            finally { pinned.Free(); }
            if (channels == 2) return input;
            var stereo = new float[checked(frames * 2)];
            for (int frame = 0; frame < frames; frame++)
            {
                int n = frame * channels;
                float left = input[n], right = channels == 1 ? left : input[n + 1];
                if (channels >= 5) { left += input[n + 2] * .7071f; right += input[n + 2] * .7071f; }
                int surroundStart = channels >= 6 ? 4 : channels == 5 ? 3 : 2;
                for (int channel = surroundStart; channel < channels; channel++)
                    if ((channel - surroundStart) % 2 == 0) left += input[n + channel] * .5f; else right += input[n + channel] * .5f;
                stereo[frame * 2] = Mathf.Clamp(left, -1, 1); stereo[frame * 2 + 1] = Mathf.Clamp(right, -1, 1);
            }
            return stereo;
        }
        internal static void End()
        {
            if (!started) return;
            started = false; stop.Invoke(null, null);
        }
    }
}
