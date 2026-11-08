using System;
using System.IO;
using System.Reflection;


using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI.Client
{
    // Created by Program.Awake, before any async resource or scene UI loading.
    // Startup and final shutdown share one persistent opaque cover. The client
    // retains its own preparation surface until the real duel is ready.
    internal sealed class FightStartupPresentation : MonoBehaviour
    {
        internal static FightStartupPresentation Instance { get; private set; }
        internal static bool BlocksInput => Instance != null && Instance.cover != null && Instance.cover.enabled;
        internal static bool StartupFailed => Instance != null && Instance.failed;

        private Canvas cover;
        private RawImage brand;
        private Image backdropImage;
        private bool commandLineProcessed, handedOff, synchronizing, failed, stopping;
        private string nativeLaunch, failureMessage;
        private RectTransform failureRoot;
        private Font errorFont;
        private GameObject errorEventSystem;

        internal static void Install()
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
            Instance.failed = true; Instance.failureMessage = message;
            Instance.cover.enabled = true; Instance.GetComponent<GraphicRaycaster>().enabled = true;
            Instance.ShowFailure();
        }

        internal static void PrepareToQuit()
        {
            BeginShutdown();
        }

        // The shipped Unity player retains the native quit-intent delegate and
        // Internal_ApplicationWantsToQuit, but strips the public event accessors.
        // Bind that real callback without inventing a runtime API or replacing
        // Unity's quit implementation.
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
            FightStartupPresentation presentation = Instance;
            presentation.stopping = true;
            presentation.EnsureShutdownCover();
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
            var path = Runtime.Program.PATH_DATA + "ClientUI/brand.png";
            if (File.Exists(path))
            {
                try { brand.texture = FightAsset.Acquire(path); brand.color = Color.white; }
                catch (Exception error) { brand.enabled = false; Debug.LogWarning("[FightPresentation] startup brand: " + error.Message); }
            }
            else brand.enabled = false;
            FitBrand();
            Canvas.willRenderCanvases += BeforeRender;
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
            button.onClick.AddListener(Runtime.Program.GameQuit);
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

        internal static bool NativeFrameReady(Controllers.Servant servant)
        {
            if (servant == null || servant.servantUI == null || !servant.servantUI.gameObject.activeInHierarchy) return false;
            if (servant is Controllers.OcgCore core)
            {
                // OcgCore does not clear Servant.inTransition. Its real asset/HUD
                // readiness determines the direct-launch handoff instead.
                Runtime.Program program = Runtime.Program.instance;
                if (program == null || !core.showing || program.currentServant != core || program.ui_ == null || program.camera_ == null
                    || program.camera_.cameraMain == null || !program.camera_.cameraMain.isActiveAndEnabled
                    || program.camera_.camera2D != null && program.camera_.camera2D.isActiveAndEnabled) return false;
                ServantUI.OcgCoreUI ui = core.GetUI<ServantUI.OcgCoreUI>();
                return core.DuelBGManager != null && core.DuelBGManager.loaded && ui != null && ui.CG.alpha >= .95f
                    && ui.Buttons.activeInHierarchy && core.DuelBGManager.field0Manager != null
                    && core.DuelBGManager.field0Manager.gameObject.activeInHierarchy && core.DuelBGManager.field1Manager != null
                    && core.DuelBGManager.field1Manager.gameObject.activeInHierarchy
                    && (Runtime.Program.instance.ui_.blackBack == null || Runtime.Program.instance.ui_.blackBack.color.a <= .02f)
                    && (Runtime.Program.instance.camera_.black == null || Runtime.Program.instance.camera_.black.color.a <= .02f);
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
                if (Runtime.Program.instance == null) return;
                // Direct CLI launches do not install FightClient. A selector's
                // async completion can still activate its old page after it has
                // switched to the core; suppress that page before every render.
                if (FightClient.Instance == null && Runtime.Program.instance.currentServant == Runtime.Program.instance.ocgcore)
                    FightClient.HideInactiveServantUIs();
                FightClient.SynchronizeServantPresentation();
                if (handedOff) return;
                transform.SetAsLastSibling();
                FightClient client = FightClient.Instance;
                bool ready = client != null && client.StartupFrameReady && client.HasOpaqueWindowBackdrop;
                if (!ready && commandLineProcessed && Runtime.Program.exitOnReturn)
                {
                    Controllers.Servant servant = Runtime.Program.instance.currentServant;
                    bool nativeReady = servant != Runtime.Program.instance.menu && NativeFrameReady(servant);
                    bool failedReplay = nativeLaunch == "replay" && servant == Runtime.Program.instance.replay && NativeFrameReady(servant);
                    bool failedOnline = nativeLaunch == "online" && servant == Runtime.Program.instance.menu && Runtime.Network.TcpHelper.canJoin && Runtime.Network.TcpHelper.tcpClient == null;
                    if (failedReplay || failedOnline)
                    {
                        ReportFailure(nativeLaunch == "online" ? "无法连接到服务器，请检查启动参数和网络。"
                            : nativeLaunch == "replay" ? "回放没有录制完整或无法读取。" : "原生启动未能完成，请检查启动参数。" );
                        return;
                    }
                    // A replay selector is an intermediate load target; only the
                    // actual original replay core can replace the startup cover.
                    ready = nativeReady && !(nativeLaunch == "replay" && servant == Runtime.Program.instance.replay);
                }
                if (!ready) return;
                // This callback precedes the canvas render. The client body has
                // finished its reveal and its opaque full-window backdrop exists;
                // this exact frame can replace the cover without an empty frame.
                cover.enabled = false; GetComponent<GraphicRaycaster>().enabled = false;
                handedOff = true;
            }
            finally { synchronizing = false; }
        }

        private void OnApplicationQuit() => BeginShutdown();

        private void OnDestroy()
        {
            Canvas.willRenderCanvases -= BeforeRender;
            if (errorFont != null) Destroy(errorFont);
            if (errorEventSystem != null) Destroy(errorEventSystem);
            if (Instance == this) Instance = null;
        }
    }
}
