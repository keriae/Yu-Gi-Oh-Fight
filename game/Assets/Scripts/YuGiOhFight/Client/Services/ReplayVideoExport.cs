using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;

namespace YuGiOhFight.Client.Services
{
    internal sealed class ReplayVideoExportOptions
    {
        public int Width = 1920, Height = 1080, Fps = 60;
        public string Quality = "high";
        public bool Audio = true, Overwrite, Quit;
        public float MaxSeconds, TailSeconds = 10;
        public string Output;
        internal int Bitrate => checked((int)Math.Min(80000000, Math.Max(4000000, (long)Width * Height * Fps * (Quality == "high" ? .24 : Quality == "medium" ? .14 : .08))));
        internal void Validate()
        {
            if (Width < 320 || Height < 240 || Width > 7680 || Height > 4320 || Width % 2 != 0 || Height % 2 != 0) throw new ArgumentException("分辨率需要是 320×240 至 7680×4320 之间的偶数。");
            if (Fps != 24 && Fps != 30 && Fps != 60 && Fps != 120) throw new ArgumentException("FPS 请选择 24、30、60 或 120。");
            if (Quality != "high" && Quality != "medium" && Quality != "small") throw new ArgumentException("画质请选择 high、medium 或 small。");
            if (float.IsNaN(MaxSeconds) || float.IsInfinity(MaxSeconds) || float.IsNaN(TailSeconds) || float.IsInfinity(TailSeconds)
                || MaxSeconds < 0 || (double)MaxSeconds * Fps > int.MaxValue || TailSeconds < 0 || TailSeconds > 60) throw new ArgumentException("导出时长设置无效。");
            if (string.IsNullOrWhiteSpace(Output) || !Path.IsPathRooted(Output) || !Output.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("请选择 MP4 文件的完整保存路径。");
            Output = Path.GetFullPath(Output);
            if (File.Exists(Output) && !Overwrite) throw new IOException("目标文件已存在，请选择其他名称或确认覆盖。");
        }
    }

    internal sealed class ReplayVideoExport : MonoBehaviour
    {
        internal static ReplayVideoExport Active { get; private set; }
        internal static bool Busy => Active != null;
        internal static bool Capturing => Active != null && Active.encoder != IntPtr.Zero;
        internal static float UnscaledClock => Capturing ? Active.captureStartUnscaled + Active.frame / (float)Active.options.Fps : Time.unscaledTime;
        internal static float UnscaledDelta => Capturing ? 1f / Active.options.Fps : Time.unscaledDeltaTime;
        internal static float PlaybackClock => Capturing ? Active.captureStartClock + Active.frame / (float)Active.options.Fps : Time.realtimeSinceStartup;
        private ReplayVideoExportOptions options;
        private Action launch;
        private Action<string, bool> completed;
        private string replayName, stage, stageJson, failure, requestedScene, activeScene;
        private IntPtr encoder;
        private Texture2D pixels;
        private int frame, finishedFrame = -1, oldWidth, oldHeight, oldFps;
        private FullScreenMode oldMode;
        private float oldTimeScale, captureStartClock, captureStartUnscaled;
        private bool clockStarted;
        private bool scene3DActive;
        private bool cancelled, saved;
        private readonly Dictionary<string, string> oldSettings = new Dictionary<string, string>();
        private readonly List<object> events = new List<object>();
        private readonly List<GameObject> hidden = new List<GameObject>();
        private readonly List<InputSystemUIInputModule> disabledInputs = new List<InputSystemUIInputModule>();
        private UniversalRenderPipelineAsset renderAsset, uiRenderAsset;
        private float renderScale, uiRenderScale;
        private const string Library = "ReplayVideo";
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr ReplayVideoCreate([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int width, int height, int fps, int bitrate, int audio);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int ReplayVideoAppend(IntPtr handle, byte[] rgba, int flip, float[] audio, int frames);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int ReplayVideoFinish(IntPtr handle);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern void ReplayVideoCancel(IntPtr handle);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr ReplayVideoError(IntPtr handle);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int ReplayVideoCancelled(IntPtr handle);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr ReplayVideoCaptureClock();
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int ReplayVideoSetAudioSampleRate(IntPtr handle, int rate);
        internal static void Start(string name, ReplayVideoExportOptions options, Action launch, Action<string, bool> completed = null)
        {
            if (Busy) throw new InvalidOperationException("已有回放正在导出。");
            if (Application.platform != RuntimePlatform.OSXPlayer) throw new PlatformNotSupportedException("回放视频导出需要 macOS 客户端。");
            options.Validate();
            var service = Runtime.Program.instance.gameObject.AddComponent<ReplayVideoExport>();
            service.options = options; service.launch = launch; service.replayName = name; service.completed = completed; Active = service;
            service.StartCoroutine(service.Run());
        }
        internal static bool TryCommandLine(string[] args, string replay, Action launch)
        {
            int target = Array.IndexOf(args, "--export-replay-video");
            if (target < 0) return false;
            if (string.IsNullOrWhiteSpace(replay) || target + 1 >= args.Length) throw new ArgumentException("视频导出需要 -r 回放名称和 --export-replay-video 保存路径。");
            var settings = new ReplayVideoExportOptions { Output = args[target + 1], Quit = true };
            for (int i = 0; i < args.Length; i++)
            {
                string key = args[i];
                if (key == "--export-overwrite") settings.Overwrite = true;
                else if (key == "--export-no-audio") settings.Audio = false;
                else if (key.StartsWith("--export-", StringComparison.Ordinal) && key != "--export-replay-video")
                {
                    if (i + 1 >= args.Length) throw new ArgumentException("缺少导出选项值：" + key);
                    string value = args[++i];
                    if (key == "--export-fps") settings.Fps = int.Parse(value, CultureInfo.InvariantCulture);
                    else if (key == "--export-width") settings.Width = int.Parse(value, CultureInfo.InvariantCulture);
                    else if (key == "--export-height") settings.Height = int.Parse(value, CultureInfo.InvariantCulture);
                    else if (key == "--export-quality") settings.Quality = value;
                    else if (key == "--export-max-seconds") settings.MaxSeconds = float.Parse(value, CultureInfo.InvariantCulture);
                    else if (key == "--export-tail-seconds") settings.TailSeconds = float.Parse(value, CultureInfo.InvariantCulture);
                    else throw new ArgumentException("未知导出选项：" + key);
                }
            }
            Start(replay, settings, launch); return true;
        }
        private void Setting(string key, string value, string fallback)
        {
            oldSettings[key] = MDPro3.Config.Get(key, fallback); MDPro3.Config.Set(key, value);
        }
        private IEnumerator Run()
        {
            // Iterator exceptions are handled at each boundary, so all exit
            // paths restore display, pacing, audio and temporary files.
            if (!Attempt(Prepare)) { Finish(); yield break; }
            float deadline = Time.realtimeSinceStartup + 60;
            while (!cancelled && Runtime.Program.instance.currentServant != Runtime.Program.instance.ocgcore && Time.realtimeSinceStartup < deadline) yield return null;
            while (!cancelled && !UI.Client.FightStartupPresentation.NativeFrameReady(Runtime.Program.instance.ocgcore) && Time.realtimeSinceStartup < deadline) yield return null;
            if (cancelled) { Finish(); yield break; }
            if (Time.realtimeSinceStartup >= deadline) { failure = "回放加载超时。"; Finish(); yield break; }
            yield return new WaitForEndOfFrame();
            if (!Attempt(BeginCapture)) { Finish(); yield break; }
            while (!cancelled)
            {
                yield return new WaitForEndOfFrame();
                if (!Attempt(CaptureFrame)) break;
                if (Runtime.Program.instance.currentServant != Runtime.Program.instance.ocgcore) { cancelled = true; break; }
                if (finishedFrame >= 0 && frame - finishedFrame >= Mathf.CeilToInt(options.TailSeconds * options.Fps)) break;
                if (options.MaxSeconds > 0 && frame >= Mathf.CeilToInt(options.MaxSeconds * options.Fps)) break;
            }
            Finish();
        }
        private bool Attempt(Action action)
        {
            try { action(); return true; }
            catch (Exception error) { failure = error.Message; Debug.LogError("[ReplayVideoExport] " + error); return false; }
        }
        private void Prepare()
        {
            oldWidth = Screen.width; oldHeight = Screen.height; oldMode = RuntimeDisplay.ResolveMode(MDPro3.Config.Get("ScreenMode", "1"), Application.platform);
            oldFps = MDPro3.Utility.RuntimeFramePacing.EffectiveFrameRate; oldTimeScale = Runtime.Program.instance.TimeScale;
            requestedScene = MDPro3.Config.Get(Duel.DuelSceneCatalog.SceneKey, Duel.DuelSceneCatalog.DefaultId);
            Directory.CreateDirectory(Path.GetDirectoryName(options.Output));
            stage = Path.Combine(Path.GetDirectoryName(options.Output), "." + Path.GetFileNameWithoutExtension(options.Output) + "." + Guid.NewGuid().ToString("N") + ".mp4");
            Setting("ShowFPS", "0", "1"); Setting("FPS", (options.Fps * 1000).ToString(), "60000");
            Setting("ReplayAcc", "1000", "2000"); Setting("ReplayAutoAcc", "0", "1");
            Screen.SetResolution(options.Width, options.Height, FullScreenMode.FullScreenWindow);
            renderAsset = Runtime.Program.instance.camera_.urpAsset; uiRenderAsset = Runtime.Program.instance.camera_.urpAssetForUI;
            if (renderAsset != null) renderScale = renderAsset.renderScale;
            if (uiRenderAsset != null) uiRenderScale = uiRenderAsset.renderScale;
            if (renderAsset != null) renderAsset.renderScale = 1;
            if (uiRenderAsset != null) uiRenderAsset.renderScale = 1;
            Runtime.Program.instance.TimeScale = 1;
            MDPro3.Utility.RuntimeFramePacing.SetExportFrameRate(options.Fps);
            Debug.Log("[ReplayVideoExport] preparing " + options.Width + "x" + options.Height + " @ " + options.Fps + " FPS");
            launch();
        }
        private void BeginCapture()
        {
            if (Screen.width != options.Width || Screen.height != options.Height) throw new InvalidOperationException("全屏渲染尺寸与导出设置不同：" + Screen.width + "×" + Screen.height + "。请选择屏幕支持的分辨率。");
            var background = Runtime.Program.instance.ocgcore.DuelBGManager;
            activeScene = background.ActiveSceneId; scene3DActive = background.Scene3DActive;
            Debug.Log("[ReplayVideoExport] active scene=" + activeScene + ", 3D=" + scene3DActive + ", theme=" + background.UsesThemeFixtures);
            if (renderAsset != null) renderAsset.renderScale = 1;
            if (uiRenderAsset != null) uiRenderAsset.renderScale = 1;
            // Only the UI input modules are suspended. Replay packets, canvas
            // rendering, animation and the input device state continue running.
            foreach (var module in Resources.FindObjectsOfTypeAll<InputSystemUIInputModule>())
                if (module != null && module.isActiveAndEnabled) { disabledInputs.Add(module); module.enabled = false; }
            pixels = new Texture2D(options.Width, options.Height, TextureFormat.RGBA32, false);
            encoder = ReplayVideoCreate(stage, options.Width, options.Height, options.Fps, options.Bitrate, options.Audio ? 1 : 0);
            if (encoder == IntPtr.Zero) throw new IOException(NativeError());
            captureStartClock = Time.realtimeSinceStartup; captureStartUnscaled = Time.unscaledTime;
            ReplayVideoClock.Begin(options.Fps); clockStarted = true;
            if (options.Audio)
            {
                ReplayVideoAudio.Begin();
                if (ReplayVideoSetAudioSampleRate(encoder, ReplayVideoAudio.SampleRate) != 1) throw new IOException("无法设置导出音频采样率。");
            }
            var fpsRoot = Runtime.Program.instance.ui_.fps;
            if (fpsRoot != null && fpsRoot.gameObject.activeSelf) { hidden.Add(fpsRoot.gameObject); fpsRoot.gameObject.SetActive(false); }
            Mark("capture-start", 0, 0, 0);
        }
        private void CaptureFrame()
        {
            if (Screen.width != options.Width || Screen.height != options.Height) throw new IOException("导出期间渲染尺寸发生变化。");
            var previous = RenderTexture.active;
            try { RenderTexture.active = null; pixels.ReadPixels(new Rect(0, 0, options.Width, options.Height), 0, 0, false); }
            finally { RenderTexture.active = previous; }
            int sampleFrames = 0;
            var audio = options.Audio ? ReplayVideoAudio.Mix(out sampleFrames) : null;
            if (ReplayVideoAppend(encoder, pixels.GetRawTextureData(), 1, audio, options.Audio ? sampleFrames : 0) != 1)
            {
                if (ReplayVideoCancelled(encoder) != 0) { cancelled = true; return; }
                throw new IOException(NativeError());
            }
            frame++;
            if (frame == options.Fps) Debug.Log("[ReplayVideoExport] frame delta=" + Time.deltaTime + ", unscaled delta=" + Time.unscaledDeltaTime);
            if (frame % options.Fps == 0) Debug.Log("[ReplayVideoExport] " + frame + " frames (" + (frame / options.Fps) + "s)");
        }
        private string NativeError()
        {
            var pointer = ReplayVideoError(encoder); return pointer == IntPtr.Zero ? "视频编码失败。" : Marshal.PtrToStringAnsi(pointer);
        }
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame || encoder != IntPtr.Zero && ReplayVideoCancelled(encoder) != 0) cancelled = true;
        }
        internal static void NotifyEnded()
        {
            if (!Capturing) return;
            Active.finishedFrame = Active.frame; Active.Mark("replay-end", 0, 0, 0);
        }
        internal static void PacketStart(int message, int consumed, int total)
        {
            if (Capturing) Active.Mark("packet-start", message, consumed, total);
        }
        internal static void Packet(int message, int consumed, int total)
        {
            if (Capturing) Active.Mark("packet", message, consumed, total);
        }
        private void Mark(string kind, int message, int consumed, int total)
        {
            events.Add(new { kind, frame, seconds = frame / (double)options.Fps, message, consumed, total, packet_index = kind == "packet-start" ? consumed : consumed - 1 });
        }
        private void Finish()
        {
            if (saved) return; saved = true;
            bool success = !cancelled && failure == null && encoder != IntPtr.Zero && frame > 0;
            try
            {
                if (encoder != IntPtr.Zero)
                {
                    var writer = encoder; encoder = IntPtr.Zero;
                    if (success)
                    {
                        if (ReplayVideoFinish(writer) != 1) { success = false; failure = NativeError(); }
                    }
                    else ReplayVideoCancel(writer);
                }
                if (success)
                {
                    PublishVideo();
                }
            }
            catch (Exception error) { success = false; failure = error.Message; Debug.LogError("[ReplayVideoExport] " + error); }
            finally
            {
                Restore(ReplayVideoAudio.End);
                if (clockStarted) Restore(ReplayVideoClock.End);
                foreach (var entry in oldSettings) MDPro3.Config.Set(entry.Key, entry.Value);
                Active = null;
                MDPro3.Utility.RuntimeFramePacing.SetExportFrameRate(0);
                if (Runtime.Program.instance != null) Runtime.Program.instance.TimeScale = oldTimeScale;
                Application.targetFrameRate = oldFps;
                if (oldWidth > 0) Screen.SetResolution(oldWidth, oldHeight, oldMode);
                foreach (var module in disabledInputs) if (module != null) module.enabled = true;
                if (renderAsset != null) renderAsset.renderScale = renderScale;
                if (uiRenderAsset != null) uiRenderAsset.renderScale = uiRenderScale;
                foreach (var obj in hidden) if (obj != null) obj.SetActive(true);
                if (pixels != null) Destroy(pixels);
                Restore(() => { if (stage != null && File.Exists(stage)) File.Delete(stage); });
                Restore(() => { if (stageJson != null && File.Exists(stageJson)) File.Delete(stageJson); });
            }
            string result = success ? "回放视频已导出：" + options.Output : cancelled ? "已取消视频导出。" : "回放视频导出失败：" + failure;
            Debug.Log("[ReplayVideoExport] " + result);
            try { completed?.Invoke(result, !success && !cancelled); }
            catch (Exception error) { Debug.LogError("[ReplayVideoExport] completion callback: " + error); }
            finally { if (options.Quit) Application.Quit(success ? 0 : 1); Destroy(this); }
        }
        private static void Restore(Action action)
        {
            try { action(); } catch (Exception error) { Debug.LogWarning("[ReplayVideoExport] restore: " + error.Message); }
        }
        private void PublishVideo()
        {
            string metadata = options.Output + ".json";
            stageJson = stage + ".json";
            File.WriteAllText(stageJson, JsonConvert.SerializeObject(new { schema = 1, replay = replayName, width = options.Width, height = options.Height,
                fps = options.Fps, frames = frame, audio = options.Audio, source_audio_sample_rate = options.Audio ? ReplayVideoAudio.SampleRate : 0,
                quality = options.Quality, duration_seconds = frame / (double)options.Fps,
                requested_scene = requestedScene, active_scene = activeScene, scene_3d = scene3DActive, events }, Formatting.Indented));
            string backup = stage + ".previous";
            bool published = false;
            try
            {
                if (File.Exists(options.Output)) { if (!options.Overwrite) throw new IOException("目标文件已存在。"); File.Replace(stage, options.Output, backup); }
                else File.Move(stage, options.Output);
                published = true;
                if (File.Exists(metadata)) File.Replace(stageJson, metadata, null); else File.Move(stageJson, metadata);
            }
            catch
            {
                if (published)
                {
                    if (File.Exists(backup)) File.Replace(backup, options.Output, null); else File.Delete(options.Output);
                }
                throw;
            }
            Restore(() => { if (File.Exists(backup)) File.Delete(backup); });
        }
        private void OnDestroy()
        {
            if (!saved && options != null) { cancelled = true; Finish(); }
        }
    }
}
