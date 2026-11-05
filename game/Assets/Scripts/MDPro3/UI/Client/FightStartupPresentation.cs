using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MDPro3.UI.Client
{
    // Created by Program.Awake, before any async resource or legacy UI loading.
    // Startup and final shutdown share one persistent opaque cover. Between
    // those boundaries it synchronizes presentation without covering the duel.
    internal sealed class FightStartupPresentation : MonoBehaviour
    {
        internal sealed class FrameSample
        {
            public int frame, renderedFrame;
            public string stage, lifecycle, servant, page;
            public bool covered, coverTopmost, clientVisible, opaqueClient, clientReady, core, protectedFrame;
            public string[] externalOverlays, nativePopups, legacyServantUIs;
        }

        internal static FightStartupPresentation Instance { get; private set; }
        internal static bool BlocksInput => Instance != null && Instance.cover != null && Instance.cover.enabled;
        internal static bool StartupFailed => Instance != null && Instance.failed;
        internal readonly List<FrameSample> Samples = new List<FrameSample>();
        internal int InstalledFrame { get; private set; }
        internal int HandoffFrame { get; private set; } = -1;
        internal int ShutdownFrame { get; private set; } = -1;
        internal bool StartupImageCaptured { get; private set; }
        internal bool ClientImageCaptured { get; private set; }
        internal bool StartupPixelsOpaque { get; private set; }
        internal bool StartupBrandRendered { get; private set; }

        private Canvas cover;
        private RawImage brand;
        private Image backdropImage;
        private bool commandLineProcessed, handedOff, synchronizing, auditEnabled, failed, stopping;
        private string auditDirectory, stage = "startup", nativeLaunch, failureMessage;
        private RectTransform failureRoot;
        private Font errorFont;
        private GameObject errorEventSystem;
        private int lastSampleFrame = -1;

        internal static void Install(MonoBehaviour owner)
        {
            if (Instance != null) return;
            var host = new GameObject("Fight startup presentation", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            // Boot and SceneMain have different lifetimes. Keep one root cover
            // across the addressable scene switch; Program reuses this instance.
            DontDestroyOnLoad(host);
            host.AddComponent<FightStartupPresentation>();
        }

        internal static void BeginNativeLaunch(string kind)
        {
            if (Instance != null) Instance.nativeLaunch = kind;
        }

        internal static void ReportFailure(string message)
        {
            if (Instance == null) return;
            Instance.failed = true; Instance.failureMessage = message; Instance.stage = "startup-error";
            Instance.cover.enabled = true; Instance.GetComponent<GraphicRaycaster>().enabled = true;
            Instance.ShowFailure(); Instance.SaveAudit();
        }

        internal static void PrepareToQuit()
        {
            BeginShutdown();
        }

        // The shipped Unity player retains the native quit-intent delegate and
        // Internal_ApplicationWantsToQuit, but strips the public event accessors.
        // Bind that real callback without inventing a runtime API or replacing
        // Unity's quit implementation. The native proof exercises this exact hook.
        private static readonly FieldInfo quitIntent = typeof(Application).GetField("wantsToQuit",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        internal static bool AddQuitIntentHandler(Func<bool> handler)
        {
            if (quitIntent == null || quitIntent.FieldType != typeof(Func<bool>)) return false;
            quitIntent.SetValue(null, Delegate.Combine(quitIntent.GetValue(null) as Delegate, handler));
            return true;
        }
        internal static void RemoveQuitIntentHandler(Func<bool> handler)
        {
            if (quitIntent == null || quitIntent.FieldType != typeof(Func<bool>)) return;
            quitIntent.SetValue(null, Delegate.Remove(quitIntent.GetValue(null) as Delegate, handler));
        }

        internal static void BeginShutdown()
        {
            if (Instance == null) return;
            var presentation = Instance;
            bool first = !presentation.stopping;
            presentation.stopping = true;
            presentation.EnsureShutdownCover();
            if (!first) return;
            presentation.ShutdownFrame = Time.frameCount;
            presentation.stage = "shutdown-cover-mounted";
            if (presentation.auditEnabled) presentation.Samples.Add(presentation.ObserveFrame());
            presentation.SaveAudit();
        }

        private void EnsureShutdownCover()
        {
            gameObject.SetActive(true);
            cover.enabled = true; cover.sortingOrder = short.MaxValue;
            backdropImage.enabled = true; backdropImage.color = FightUI.Background;
            // The brand uses the same cache the client is about to release.
            // Shutdown deliberately keeps only an opaque, texture-free surface.
            if (brand != null) { brand.enabled = false; brand.texture = null; }
            if (failureRoot != null) failureRoot.gameObject.SetActive(false);
            GetComponent<GraphicRaycaster>().enabled = true;
            transform.SetAsLastSibling();
        }

        internal static void CommandLineProcessed()
        {
            if (Instance != null) Instance.commandLineProcessed = true;
        }

        private void Awake()
        {
            Instance = this;
            InstalledFrame = Time.frameCount;
            cover = GetComponent<Canvas>();
            cover.renderMode = RenderMode.ScreenSpaceOverlay;
            // Above every normal/native loading canvas, including async prefabs
            // that arrive after Awake. Required native popups resume at handoff.
            cover.sortingOrder = short.MaxValue;
            GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var backdrop = new GameObject("Opaque startup backdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(transform, false);
            var rect = backdrop.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = backdrop.GetComponent<Image>(); image.color = FightUI.Background; image.raycastTarget = true;
            backdropImage = image;
            var logo = new GameObject("Fight startup brand", typeof(RectTransform), typeof(RawImage));
            logo.transform.SetParent(transform, false);
            brand = logo.GetComponent<RawImage>(); brand.raycastTarget = false;
            var logoRect = brand.rectTransform;
            logoRect.anchorMin = logoRect.anchorMax = logoRect.pivot = new Vector2(.5f, .5f);
            var path = Program.PATH_DATA + "ClientUI/brand.png";
            if (File.Exists(path))
            {
                try { brand.texture = FightAsset.Acquire(path); brand.color = Color.white; }
                catch (Exception error) { brand.enabled = false; Debug.LogWarning("[FightPresentation] startup brand: " + error.Message); }
            }
            else brand.enabled = false;
            FitBrand();
#if FIGHT_UI_PROOF
            auditEnabled = Environment.GetEnvironmentVariable("FIGHT_UI_TEST") == "1";
#endif
            auditDirectory = Environment.GetEnvironmentVariable("FIGHT_PROOF_DIR") ?? Directory.GetCurrentDirectory();
            Canvas.willRenderCanvases += BeforeRender;
            if (auditEnabled)
            {
                Directory.CreateDirectory(auditDirectory);
                StartCoroutine(ObserveRenderedFrames());
            }
        }

        private void FitBrand()
        {
            if (brand == null) return;
            float width = Mathf.Clamp(Screen.width * .22f, 224, 416);
            float ratio = brand.texture != null ? (float)brand.texture.width / brand.texture.height : 2;
            brand.rectTransform.sizeDelta = new Vector2(width, width / ratio);
            brand.rectTransform.anchoredPosition = failed ? new Vector2(0, Mathf.Min(180, Screen.height * .26f)) : Vector2.zero;
            if (failureRoot != null) failureRoot.sizeDelta = new Vector2(Mathf.Min(720, Mathf.Max(280, Screen.width - 48)), 300);
        }

        private void ShowFailure()
        {
            if (failureRoot != null) Destroy(failureRoot.gameObject);
            var host = new GameObject("Startup error and exit", typeof(RectTransform)); host.transform.SetParent(transform, false);
            failureRoot = host.GetComponent<RectTransform>();
            failureRoot.anchorMin = failureRoot.anchorMax = failureRoot.pivot = new Vector2(.5f, .5f);
            failureRoot.anchoredPosition = new Vector2(0, -35);
            if (errorFont == null)
                errorFont = Font.CreateDynamicFontFromOSFont(new[] { "PingFang SC", "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 28);
            if (EventSystem.current == null)
            {
                errorEventSystem = new GameObject("Startup error input", typeof(EventSystem), typeof(InputSystemUIInputModule));
                DontDestroyOnLoad(errorEventSystem);
                errorEventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            float width = Mathf.Min(720, Mathf.Max(280, Screen.width - 48));
            ErrorText("启动失败", 0, 0, width, 56, 30, FightUI.Active);
            ErrorText(failureMessage ?? "游戏未能初始化。", 16, 72, width - 32, 140, 21, FightUI.Foreground);
            var buttonRect = FightUI.Node(failureRoot, "退出应用", (width - 208) / 2, 232, 208, 52);
            var fill = buttonRect.gameObject.AddComponent<Image>(); fill.color = FightUI.Raised;
            var button = buttonRect.gameObject.AddComponent<Button>(); button.targetGraphic = fill;
            button.onClick.AddListener(Program.GameQuit);
            var label = ErrorText("退出应用", 0, 0, 208, 52, 22, FightUI.Gold, buttonRect);
            label.raycastTarget = false;
            button.Select();
            FitBrand();
        }

        private Text ErrorText(string value, float x, float y, float width, float height, int size, Color color, Transform parent = null)
        {
            var rect = FightUI.Node(parent ?? failureRoot, "Startup error text", x, y, width, height);
            var label = rect.gameObject.AddComponent<Text>(); label.font = errorFont; label.text = value;
            label.fontSize = size; label.color = color; label.alignment = TextAnchor.MiddleCenter;
            label.supportRichText = false; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        internal static bool NativeFrameReady(MDPro3.Servant.Servant servant)
        {
            if (servant == null || servant.servantUI == null || !servant.servantUI.gameObject.activeInHierarchy) return false;
            if (servant is OcgCore core)
            {
                // OcgCore does not clear Servant.inTransition. Its real asset/HUD
                // readiness determines the direct-launch handoff instead.
                var ui = core.GetUI<OcgCoreUI>();
                return core.DuelBGManager != null && core.DuelBGManager.loaded && ui != null && ui.CG.alpha >= .95f
                    && ui.Buttons.activeInHierarchy && core.DuelBGManager.field0Manager != null
                    && core.DuelBGManager.field0Manager.gameObject.activeInHierarchy && core.DuelBGManager.field1Manager != null
                    && core.DuelBGManager.field1Manager.gameObject.activeInHierarchy
                    && (Program.instance.ui_.blackBack == null || Program.instance.ui_.blackBack.color.a <= .02f)
                    && (Program.instance.camera_.black == null || Program.instance.camera_.black.color.a <= .02f);
            }
            return !servant.inTransition;
        }

        private void BeforeRender()
        {
            if (stopping) { EnsureShutdownCover(); return; }
            if (synchronizing) return;
            synchronizing = true;
            try
            {
                FitBrand();
                if (failed) { transform.SetAsLastSibling(); return; }
                if (Program.instance == null) return;
                // Direct CLI launches do not install FightClient. A selector's
                // async completion can still activate its old page after it has
                // switched to the core; suppress that page before every render.
                if (FightClient.Instance == null && Program.instance.currentServant == Program.instance.ocgcore)
                    FightClient.HideInactiveLegacyServantUIs();
                FightClient.SynchronizeServantPresentation();
                if (handedOff) return;
                transform.SetAsLastSibling();
                var client = FightClient.Instance;
                bool ready = client != null && client.StartupFrameReady && client.HasOpaqueWindowBackdrop;
                if (!ready && commandLineProcessed && Program.exitOnReturn)
                {
                    var servant = Program.instance.currentServant;
                    bool nativeReady = servant != Program.instance.menu && NativeFrameReady(servant);
                    bool failedReplay = nativeLaunch == "replay" && servant == Program.instance.replay && NativeFrameReady(servant);
                    bool failedOnline = nativeLaunch == "online" && servant == Program.instance.menu && TcpHelper.canJoin && TcpHelper.tcpClient == null;
                    if (failedReplay || failedOnline)
                    {
                        ReportFailure(nativeLaunch == "online" ? "无法连接到服务器，请检查启动参数和网络。"
                            : nativeLaunch == "replay" ? "回放没有录制完整或无法读取。" : "原生启动未能完成，请检查启动参数。" );
                        return;
                    }
                    // A replay selector is an intermediate load target; only the
                    // actual original replay core can replace the startup cover.
                    ready = nativeReady && !(nativeLaunch == "replay" && servant == Program.instance.replay);
                }
                if (!ready) return;
                // This callback precedes the canvas render. The client body has
                // finished its reveal and its opaque full-window backdrop exists;
                // this exact frame can replace the cover without an empty frame.
                cover.enabled = false; GetComponent<GraphicRaycaster>().enabled = false;
                handedOff = true; HandoffFrame = Time.frameCount;
#if FIGHT_UI_PROOF
                Debug.Log("[FightPresentation] startup handoff frame=" + HandoffFrame + " client=" + (client != null));
#endif
            }
            finally { synchronizing = false; }
        }

        internal void SetAuditStage(string value) => stage = value;

        internal FrameSample ObserveFrame()
        {
            var program = Program.instance;
            var client = FightClient.Instance;
            bool core = program != null && program.currentServant == program.ocgcore;
            bool covered = cover != null && cover.enabled && gameObject.activeInHierarchy;
            var external = UnityEngine.Object.FindObjectsOfType<Canvas>()
                .Where(canvas => canvas.enabled && canvas.gameObject.activeInHierarchy && canvas.isRootCanvas
                    && canvas.renderMode == RenderMode.ScreenSpaceOverlay && canvas != cover
                    && !IsClientCanvas(canvas) && !IsNativePopupCanvas(canvas)
                    && canvas.GetComponentsInChildren<Graphic>().Any(graphic => graphic.enabled && graphic.gameObject.activeInHierarchy))
                .ToArray();
            bool topmost = covered && external.All(canvas => canvas.sortingOrder < cover.sortingOrder);
            bool opaqueClient = client != null && client.ClientCanvasVisible && client.HasOpaqueWindowBackdrop;
            var visibleExternal = external.Where(canvas => canvas.sortingOrder >= 100).Select(canvas => canvas.name + ":" + canvas.sortingOrder).ToArray();
            var popups = program == null || program.ui_ == null || program.ui_.popup == null ? Array.Empty<string>()
                : program.ui_.popup.GetComponentsInChildren<MDPro3.UI.Popup.Popup>()
                    .Where(popup => popup.gameObject.activeInHierarchy).Select(popup => popup.GetType().Name).ToArray();
            var legacy = program == null ? Array.Empty<string>() : program.GetComponentsInChildren<MDPro3.Servant.Servant>(true)
                .Where(servant => servant != program.ocgcore && (servant != program.deckEditor || core) && servant != program.currentSubServant
                    && servant.servantUI != null && servant.servantUI.gameObject.activeInHierarchy)
                .Select(servant => servant.GetType().Name).ToArray();
            return new FrameSample
            {
                frame = Time.frameCount, renderedFrame = Time.renderedFrameCount, stage = stage,
                lifecycle = program == null ? "boot" : client == null ? "scene-main" : "client",
                servant = program == null || program.currentServant == null ? null : program.currentServant.GetType().Name,
                page = client == null ? null : client.PresentedPage, covered = covered, coverTopmost = topmost,
                clientVisible = client != null && client.ClientCanvasVisible, opaqueClient = opaqueClient,
                clientReady = client != null && client.StartupFrameReady, core = core,
                protectedFrame = covered ? topmost : core ? (client == null || !client.ClientCanvasVisible) && legacy.Length == 0
                    : opaqueClient && visibleExternal.Length == 0,
                externalOverlays = visibleExternal, nativePopups = popups, legacyServantUIs = legacy
            };
        }

        private static bool IsClientCanvas(Canvas canvas) => canvas.name == "Yu-Gi-Oh! Fight Client"
            || FightClient.Instance != null && FightClient.Instance.Body != null && canvas.transform.IsChildOf(FightClient.Instance.Body.parent);
        private static bool IsNativePopupCanvas(Canvas canvas) => canvas.name == "Fight native popup overlay";

        private IEnumerator ObserveRenderedFrames()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();
                if (lastSampleFrame == Time.frameCount) continue;
                lastSampleFrame = Time.frameCount;
                var sample = ObserveFrame(); Samples.Add(sample);
                if (!StartupImageCaptured && sample.covered)
                {
                    CaptureFrame(Path.Combine(auditDirectory, "presentation-startup-first-frame.png"), true);
                    StartupImageCaptured = true;
                }
                if (!ClientImageCaptured && !sample.covered && sample.clientReady && !sample.core)
                {
                    CaptureFrame(Path.Combine(auditDirectory, "presentation-first-client-frame.png"), false);
                    ClientImageCaptured = true;
                }
            }
        }

        private void CaptureFrame(string path, bool validateStartup)
        {
            var previous = RenderTexture.active;
            Texture2D texture = null;
            try
            {
                RenderTexture.active = null;
                texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false); texture.Apply(false, false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                if (validateStartup)
                {
                    // Verify the actual framebuffer, including its outer corners,
                    // rather than relying solely on the cover GameObject state.
                    var points = new[] { new Vector2Int(4, 4), new Vector2Int(Screen.width - 5, 4),
                        new Vector2Int(4, Screen.height - 5), new Vector2Int(Screen.width - 5, Screen.height - 5) };
                    StartupPixelsOpaque = points.All(point => Vector3.Distance((Vector3)(Vector4)texture.GetPixel(point.x, point.y),
                        (Vector3)(Vector4)FightUI.Background) < .075f);
                    int bright = 0;
                    int cx = Screen.width / 2, cy = Screen.height / 2;
                    int halfW = Mathf.Min(cx - 1, Mathf.CeilToInt(brand.rectTransform.rect.width / 2));
                    int halfH = Mathf.Min(cy - 1, Mathf.CeilToInt(brand.rectTransform.rect.height / 2));
                    for (int y = cy - halfH; y < cy + halfH; y += 4)
                        for (int x = cx - halfW; x < cx + halfW; x += 4)
                            if (texture.GetPixel(x, y).maxColorComponent > .2f) bright++;
                    StartupBrandRendered = brand.enabled && brand.texture != null && bright > 50;
                }
            }
            finally { RenderTexture.active = previous; if (texture != null) Destroy(texture); }
        }

        internal void SaveAudit()
        {
            if (!auditEnabled) return;
            File.WriteAllText(Path.Combine(auditDirectory, "presentation-frames.json"), JsonConvert.SerializeObject(new
            {
                installedFrame = InstalledFrame, handoffFrame = HandoffFrame, shutdownFrame = ShutdownFrame,
                failed = failed, failureMessage = failureMessage,
                startupImageCaptured = StartupImageCaptured, clientImageCaptured = ClientImageCaptured,
                startupPixelsOpaque = StartupPixelsOpaque, startupBrandRendered = StartupBrandRendered,
                frames = Samples
            }, Formatting.Indented));
        }

        private void OnApplicationQuit() { BeginShutdown(); SaveAudit(); }
        private void OnDestroy()
        {
            Canvas.willRenderCanvases -= BeforeRender;
            if (errorFont != null) Destroy(errorFont);
            if (errorEventSystem != null) Destroy(errorEventSystem);
            if (Instance == this) Instance = null;
        }
    }
}
