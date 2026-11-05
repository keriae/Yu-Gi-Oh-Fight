using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MDPro3.AI;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;
using MDPro3.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MDPro3.UI.Client
{
    /// <summary>New desktop client; the original rules, duel scene and HUD remain owned by Program.</summary>
    internal partial class FightClient : MonoBehaviour
    {
        internal static FightClient Instance { get; private set; }
        internal static bool BlocksLegacyInput => FightStartupPresentation.BlocksInput
            || Instance != null && Instance.clientCanvas != null && Instance.clientCanvas.enabled
            && (Program.instance.currentServant != Program.instance.deckEditor || Instance.modal != null);
        internal bool IsClientEditor => page == "editor" && clientCanvas != null && clientCanvas.enabled;
        internal RectTransform Body { get; private set; }
        internal float BodyWidth => W - Gap * 2;
        internal float BodyHeight => H - BodyTop - Footer - Gap;
        internal bool Compact => W < 1600;
        internal bool LargeViewport => W >= 2400;
        internal float Gap => Compact ? 16 : 24;
        internal float W => designWidth;
        internal float H => designHeight;
        private float designWidth = 1280, designHeight = 720;
        private float lastPhysicalResize;
        private float Header => Compact ? 88 : LargeViewport ? 104 : 120;
        private float Footer => Compact ? 32 : LargeViewport ? 36 : 48;
        private float BodyTop => page == "home" ? Header + (Compact ? 11 : 16) : Header + Gap;
        private Canvas clientCanvas;
        private CanvasGroup clientInteraction;
        private Canvas nativePopupCanvas;
        private readonly List<NativeUiRootMount> nativeUiRoots = new List<NativeUiRootMount>();
        private readonly Dictionary<Selectable, NativeInputMount> nativeInputs = new Dictionary<Selectable, NativeInputMount>();
        private RectTransform root, modal, nativePopupViewport;
        private GameObject dialogOriginFocus;
        private sealed class RefreshFocus
        {
            internal string Page, Name, FieldKey, StableKey, Hint, Parent;
            internal Type Type;
            internal Vector3 Position;
            internal bool InDialog;
            internal RectTransform Dialog;
        }
        private RefreshFocus refreshingFocus;
        private TextMeshProUGUI statusText, fpsText;
        private string page = "home", status = "准备就绪";
        private string renderedPage;
        private sealed class ClientNavigationState
        {
            internal string Search = "", Group = "全部", SelectedPath;
            internal float[] Scrolls = Array.Empty<float>();
        }
        private readonly Dictionary<string, ClientNavigationState> navigationStates = new Dictionary<string, ClientNavigationState>();
        private float statusUntil;
        private bool statusIsError;
        private Button statusInspectButton;
        private FightIcon statusIcon, statusInspectArrow;
        private int oldWidth, oldHeight;
        private bool oldFullscreen;
        private bool wasCore, corePresented, initialized, synchronizingPresentation, presentationStopping;
        private int corePresentationVersion;
        private string coreSourcePage;
        private int navigationFrame = -1;
        private DeckAcademyCatalog academy;
        private List<DeckLibraryEntry> recommendations;
        private DeckAcademyDeck tutorial;
        private int tutorialStep = -1;
        private string tutorialOrigin = "academy";
        private string search = "", selectedPath;
        private Action<string> pickerCommit;
        private string pickerOrigin, pickerSlot;
        private int selectedBot;
        private ResponsesAiConfig battle;
        private string cpuDeck, cpuCustomDeck;
        private bool cpuNoCheck, cpuNoShuffle;
        private int cpuLp = 8000, cpuHand = 5, cpuDraw = 1;

        internal static void Install()
        {
            if (Instance != null || Program.exitOnReturn || FightStartupPresentation.StartupFailed) return;
            Instance = Program.instance.gameObject.AddComponent<FightClient>();
        }
        internal static void SynchronizeServantPresentation()
        {
            if (Instance != null) Instance.SynchronizePresentation();
        }
        internal bool StartupFrameReady => initialized && clientCanvas != null && clientCanvas.enabled
            && clientCanvas.gameObject.activeInHierarchy && Body != null && Body.gameObject.activeInHierarchy
            && Body.GetComponent<CanvasGroup>() != null && Body.GetComponent<CanvasGroup>().alpha >= .999f;
        internal bool ClientCanvasVisible => clientCanvas != null && clientCanvas.enabled && clientCanvas.gameObject.activeInHierarchy;
        internal bool CoreFramePresented => wasCore && corePresented;
        internal int CorePresentationVersion => corePresentationVersion;
        internal string PresentedPage => renderedPage;
        internal bool HasOpaqueWindowBackdrop => clientCanvas != null && clientCanvas.transform.Find("Window backdrop") is RectTransform backdrop
            && backdrop.gameObject.activeInHierarchy && backdrop.GetComponent<Image>() != null
            && backdrop.GetComponent<Image>().color.a >= .999f && backdrop.anchorMin == Vector2.zero && backdrop.anchorMax == Vector2.one;
        internal static void PrepareToQuit()
        {
            FightStartupPresentation.PrepareToQuit();
            if (Instance == null || Instance.presentationStopping) return;
            Instance.presentationStopping = true;
            foreach (var cleanup in new Action[] { Instance.CancelSettingsTest, Instance.CleanupCollectionPreview, Instance.ReleaseEditor, Instance.RestoreNativeUiRoots })
                try { cleanup(); }
                catch (Exception error) { Debug.LogWarning("[FightClient] exit cleanup: " + error); }
            if (Instance.clientCanvas != null) Instance.clientCanvas.enabled = false;
            FightAsset.ClearCache();
        }
        private void OnApplicationQuit() => PrepareToQuit();
        private void Start()
        {
            try
            {
                academy = DeckAcademy.Load(); recommendations = DeckLibrary.Load();
                battle = ReadBattleService(out _, out _); cpuDeck = Config.GetConfigDeckName();
                var choices = ResponsesDeckLibrary.Load(Program.PATH_DECK);
                battle.DeckA = choices.ResolveSelection(battle.DeckA) ?? choices.FindCurrentDeck(Config.GetConfigDeckName()) ?? "";
                battle.DeckB = choices.ResolveSelection(battle.DeckB) ?? "";
                var obj = new GameObject("Yu-Gi-Oh! Fight Client", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
                var canvasRoot = obj.GetComponent<RectTransform>(); obj.transform.SetParent(transform, false);
                clientCanvas = obj.GetComponent<Canvas>(); clientCanvas.renderMode = RenderMode.ScreenSpaceOverlay; clientCanvas.sortingOrder = 100;
                clientInteraction = obj.GetComponent<CanvasGroup>();
                var scale = obj.GetComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                var backdrop = FightUI.Panel(canvasRoot, 0, 0, Screen.width, Screen.height, FightUI.Background, false, "Window backdrop");
                FightUI.Stretch(backdrop); backdrop.GetComponent<Image>().raycastTarget = true;
                root = FightUI.Node(canvasRoot, "Client Design Viewport", 0, 0, W, H);
                SelectViewportTemplate(); FitClientViewport();
                LiftLegacyPopups();
                initialized = true; Rebuild();
                Program.instance.menu.returnAction = () => Navigate("home");
                Debug.Log("[FightClient] ready: " + academy.decks.Count + " tutorials, " + recommendations.Count + " recommendations");
#if FIGHT_UI_PROOF
                if (Environment.GetEnvironmentVariable("FIGHT_UI_TEST") == "1") StartCoroutine(RunClientProof());
#endif
            }
            catch (Exception error)
            {
                Debug.LogError("[FightClient] initialization: " + error);
                FightStartupPresentation.ReportFailure("客户端初始化失败：" + error.Message);
            }
        }
        private void Update()
        {
            if (!initialized || presentationStopping) return;
            SynchronizePresentation();
            bool core = Program.instance.currentServant == Program.instance.ocgcore;
            // Read the action directly: this component can update before
            // UserInput has copied this frame's button state.
            bool cancelPressed = UserInput.PlayerInput != null
                ? UserInput.PlayerInput.actions["Cancel"].WasPressedThisFrame() : UserInput.WasCancelPressed;
            if (core)
            {
                if (!corePresented && navigationFrame != Time.frameCount && cancelPressed)
                {
                    navigationFrame = Time.frameCount;
                    Program.instance.ocgcore.OnExit();
                }
                return;
            }
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame
                && (modal != null || !NativeUiInteractionActive()))
                AdvanceClientFocus(Keyboard.current.shiftKey.isPressed);
            TickCollectionPreview();
            if (fpsText != null)
            {
                fpsText.gameObject.SetActive(Config.GetBool("ShowFPS", true));
                if (Program.instance.ui_.fps.text != null) fpsText.text = Program.instance.ui_.fps.text.text;
            }
            if (oldWidth != Screen.width || oldHeight != Screen.height || oldFullscreen != UsesFullscreenLayout)
            {
                if (oldFullscreen != UsesFullscreenLayout) QueueFullscreenReflow(UsesFullscreenLayout);
                else if (UsesFullscreenLayout) QueueFullscreenReflow(false);
                oldWidth = Screen.width; oldHeight = Screen.height; oldFullscreen = UsesFullscreenLayout;
                lastPhysicalResize = Time.unscaledTime;
                FitClientViewport();
            }
            // Window drags keep the current objects. Fullscreen is a deliberate
            // density change and reflows the active page without discarding edits.
            if (fullscreenReflowPending && Time.unscaledTime - lastPhysicalResize >= .15f && !UserInput.Draging)
                ApplyFullscreenReflow();
            else if (PreferredViewportWidth != W && Time.unscaledTime - lastPhysicalResize >= .15f
                && !ViewportInteractionActive()) Rebuild();
            if (page == "editor") TickEditor();
            if (statusUntil > 0 && Time.unscaledTime > statusUntil) ResetClientStatus();
            if (navigationFrame != Time.frameCount && cancelPressed && !UserInput.InputFieldActivating())
            {
                if (modal != null) CloseDialog();
                else if (page != "editor" && !NativeUiInteractionActive())
                {
                    if (page == "picker") Navigate(pickerOrigin);
                    else if (page == "tutorial") ReturnAcademyDirectory(tutorialOrigin, tutorial);
                    else if (page == "deck-detail") Navigate(detailOrigin);
                    else if (page == "puzzle") ReturnAcademyDirectory("puzzles", selectedPuzzle);
                    else if (page == "settings") Navigate(settingsOrigin ?? "home");
                    else if (page == "animations" || page == "appearance") Navigate("collection");
                    else if (page == "cpu" || page == "ai" || page == "spectator") Navigate("modes");
                    else if (page == "home") Navigate("exit");
                    else Navigate(page == "editor" ? "decks" : "home");
                }
            }
        }
        // This also runs on servant switches and immediately before rendering.
        // An addressable completion or duel return after our Update therefore
        // cannot leave the old camera UI visible for one rendered frame.
        private void SynchronizePresentation()
        {
            if (!initialized || synchronizingPresentation || presentationStopping) return;
            synchronizingPresentation = true;
            try
            {
                bool core = Program.instance.currentServant == Program.instance.ocgcore;
                if (core)
                {
                    if (!wasCore)
                    {
                        UIManager.CancelDuelTransition();
                        corePresented = false; ++corePresentationVersion;
                        if (coreSourcePage == null) coreSourcePage = page;
                        CancelSettingsTest(); CleanupCollectionPreview(); CloseDialog();
                        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                    }
                    RestoreNativeUiRoots(); HideInactiveLegacyServantUIs();
                    // Servant ownership changes before its addressable assets,
                    // field and HUD exist. Keep the opaque preparation surface
                    // until those real objects can replace it in this frame.
                    // Latch that handoff: later in-duel HUD changes must never
                    // bring the preparation page back over the field.
                    if (!corePresented && FightStartupPresentation.NativeFrameReady(Program.instance.ocgcore))
                        corePresented = true;
                    clientCanvas.enabled = !corePresented;
                    clientInteraction.interactable = false;
                    clientInteraction.blocksRaycasts = true;
                }
                else
                {
                    corePresented = false;
                    clientCanvas.enabled = true;
                    clientInteraction.interactable = true;
                    LiftLegacyPopups();
                }
                if (!core && wasCore)
                {
                    if (coreSourcePage != null && page != coreSourcePage)
                    {
                        page = coreSourcePage;
                        if (navigationStates.TryGetValue(page, out var restored)) { search = restored.Search; deckGroup = restored.Group; selectedPath = restored.SelectedPath; }
                    }
                    coreSourcePage = null;
                    if (page != "editor") Program.instance.ShiftToServant(Program.instance.menu);
                    Rebuild(); ResetClientStatus();
                }
                wasCore = core;
            }
            finally { synchronizingPresentation = false; }
        }
        private bool PreferredCompactViewport => Mathf.Min(Screen.width, Screen.height * (16f / 9f)) < 1600;
        // macOS native Spaces fullscreen does not update Unity's fullScreen
        // flag. The drawable then covers the entire current display instead.
        private bool UsesFullscreenLayout => Screen.fullScreen || Screen.currentResolution.width > 0 && Screen.currentResolution.height > 0
            && Screen.width >= Screen.currentResolution.width - 8 && Screen.height >= Screen.currentResolution.height - 8;
        private float PreferredTemplateWidth => PreferredCompactViewport ? 1280
            : UsesFullscreenLayout && Mathf.Min(Screen.width, Screen.height * (16f / 9f)) >= 2400 ? 2560 : 1920;
        private float PreferredViewportWidth => UsesFullscreenLayout
            ? Mathf.Max(PreferredTemplateWidth, PreferredTemplateWidth * (9f / 16f) * Mathf.Max(1, Screen.width) / Mathf.Max(1, Screen.height))
            : PreferredTemplateWidth;
        private float PreferredViewportHeight => UsesFullscreenLayout
            ? PreferredViewportWidth * Mathf.Max(1, Screen.height) / Mathf.Max(1, Screen.width)
            : PreferredViewportWidth * (9f / 16f);
        private void SelectViewportTemplate()
        {
            designWidth = PreferredViewportWidth;
            // Fill the fullscreen drawable at the selected control density.
            // The separate native popup viewport retains its 1920x1080 fit.
            designHeight = PreferredViewportHeight;
        }
        private static void FitDesignViewport(RectTransform viewport, float width, float height)
        {
            if (viewport == null) return;
            viewport.anchorMin = viewport.anchorMax = viewport.pivot = new Vector2(.5f, .5f);
            viewport.anchoredPosition = Vector2.zero; viewport.sizeDelta = new Vector2(width, height);
            float fit = Mathf.Max(.001f, Mathf.Min(Mathf.Max(1, Screen.width) / width, Mathf.Max(1, Screen.height) / height));
            viewport.localScale = new Vector3(fit, fit, 1);
        }
        private void FitClientViewport()
        {
            FitDesignViewport(root, W, H);
            FitDesignViewport(nativePopupViewport, 1920, 1080);
            if (nativePopupViewport != null && root != null)
            {
                float scale = Mathf.Min(nativePopupViewport.localScale.x, Mathf.Max(1, root.localScale.x));
                nativePopupViewport.localScale = new Vector3(scale, scale, 1);
            }
        }
        private bool ViewportInteractionActive()
        {
            if (modal != null || page == "editor" || page == "collection" || page == "animations" || page == "appearance" || UserInput.Draging) return true;
            var selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            var input = selected == null ? null : selected.GetComponent<TMP_InputField>();
            // TMP applies ActivateInputField in LateUpdate. Preserve the selected
            // field during that frame as well as while its caret is active.
            if (input != null && input.isActiveAndEnabled && input.interactable) return true;
            return NativeUiInteractionActive();
        }
        private bool NativeUiInteractionActive() => UIManager.InputBlocker != null
            || Program.instance.ui_.currentSidePanel != null || Program.instance.ui_.currentPopup != null || Program.instance.ui_.currentPopupB != null;
        internal RectTransform TooltipRootFor(Transform source)
        {
            if (root != null && source.IsChildOf(root)) return root;
            if (nativePopupViewport != null && source.IsChildOf(nativePopupViewport)) return nativePopupViewport;
            return null;
        }
        private void LiftLegacyPopups()
        {
            if (Program.instance == null || Program.instance.ui_ == null) return;
            if (nativePopupCanvas == null)
            {
                var source = Program.instance.ui_.popup == null ? null : Program.instance.ui_.popup.GetComponentInParent<Canvas>()?.rootCanvas;
                var host = new GameObject("Fight native popup overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                host.transform.SetParent(transform, false);
                nativePopupCanvas = host.GetComponent<Canvas>();
                nativePopupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                nativePopupCanvas.sortingOrder = 200;
                nativePopupCanvas.targetDisplay = source == null ? 0 : source.targetDisplay;
                if (source != null) nativePopupCanvas.additionalShaderChannels = source.additionalShaderChannels;
                var scaler = host.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                nativePopupViewport = FightUI.Node(host.transform, "Native Popup Design Viewport", 0, 0, 1920, 1080);
                FitDesignViewport(nativePopupViewport, 1920, 1080);
            }
            foreach (var rt in new[] { Program.instance.ui_.popup, Program.instance.ui_.sidePanel })
            {
                if (rt == null || nativeUiRoots.Any(mount => mount.Rect == rt)) continue;
                // A sorting override inside the camera canvas is still drawn behind the client overlay.
                // Keep the original popup objects and callbacks, but move their full-size roots into a real overlay canvas.
                var mount = new NativeUiRootMount(rt); nativeUiRoots.Add(mount);
                mount.Attach(nativePopupViewport);
            }
            // Popups are instantiated after their roots are mounted. Configure
            // new fields once, including older dialogs with Unity InputField.
            foreach (var input in nativePopupViewport.GetComponentsInChildren<TMP_InputField>(true))
                AttachNativeInput(input);
            foreach (var input in nativePopupViewport.GetComponentsInChildren<InputField>(true))
                AttachNativeInput(input);
            StyleNativePopups();
        }
        private void AttachNativeInput(Selectable input)
        {
            if (nativeInputs.ContainsKey(input)) return;
            var mount = new NativeInputMount(input); nativeInputs.Add(input, mount); mount.Attach();
        }
        internal static void HideInactiveLegacyServantUIs()
        {
            // MainMenu's wallpaper is outside its servant UI and otherwise
            // fades for several frames after the switch. Async menu setup can
            // also activate it again, so enforce this before every render.
            if (Program.instance.ui_ != null && Program.instance.ui_.wallpaper != null)
                Program.instance.ui_.wallpaper.gameObject.SetActive(false);
            foreach (var servant in Program.instance.GetComponentsInChildren<MDPro3.Servant.Servant>(true))
            {
                if (servant == Program.instance.ocgcore || servant == Program.instance.currentSubServant || servant.servantUI == null) continue;
                if (servant.servantUI.gameObject.activeInHierarchy) servant.servantUI.ShutDown();
            }
        }
        private void RestoreNativeUiRoots()
        {
            RestoreNativePopupSkins();
            foreach (var input in nativeInputs.Values) input.Restore();
            nativeInputs.Clear();
            foreach (var mount in nativeUiRoots) mount.Restore();
            nativeUiRoots.Clear();
            if (nativePopupCanvas != null) { nativePopupCanvas.enabled = false; Destroy(nativePopupCanvas.gameObject); }
            nativePopupCanvas = null;
            nativePopupViewport = null;
        }
        // Permanent native panels can survive a duel transition. Restore their
        // input presentation along with the roots, then reapply on client return.
        private sealed class NativeInputMount
        {
            private readonly Selectable control;
            private readonly TMP_InputField tmp;
            private readonly InputField legacy;
            private readonly bool customCaret, richText, richEditing;
            private readonly Color caret, selection;
            private readonly int width;
            private readonly float blink;
            private readonly NativeInputText text, placeholder;
            private readonly Outline originalOutline;
            private readonly Color outlineColor;
            private readonly bool outlineEnabled, maskEnabled;
            private RectMask2D mask;
            internal NativeInputMount(Selectable input)
            {
                control = input; tmp = input as TMP_InputField; legacy = input as InputField;
                originalOutline = input.GetComponent<Outline>();
                outlineColor = originalOutline == null ? Color.clear : originalOutline.effectColor;
                outlineEnabled = originalOutline != null && originalOutline.enabled;
                if (tmp != null)
                {
                    customCaret = tmp.customCaretColor; caret = tmp.caretColor; selection = tmp.selectionColor;
                    width = tmp.caretWidth; blink = tmp.caretBlinkRate; richText = tmp.richText; richEditing = tmp.isRichTextEditingAllowed;
                    text = new NativeInputText(tmp.textComponent); placeholder = new NativeInputText(tmp.placeholder as TMP_Text);
                    mask = tmp.textViewport == null ? null : tmp.textViewport.GetComponent<RectMask2D>();
                    maskEnabled = mask != null && mask.enabled;
                }
                else if (legacy != null)
                {
                    customCaret = legacy.customCaretColor; caret = legacy.caretColor; selection = legacy.selectionColor;
                    width = legacy.caretWidth; blink = legacy.caretBlinkRate;
                }
            }
            internal void Attach()
            {
                if (tmp != null)
                {
                    FightUI.ConfigurePlainTextInput(tmp);
                    if (tmp.textViewport != null) mask = tmp.textViewport.GetComponent<RectMask2D>();
                    if (mask != null) mask.enabled = true;
                }
                else if (legacy != null)
                {
                    legacy.customCaretColor = true; legacy.caretColor = FightUI.Cyan; legacy.caretWidth = 2; legacy.caretBlinkRate = .85f;
                    legacy.selectionColor = new Color(.16f, .6f, .72f, .3f); FightUI.ConfigureInputFeedback(legacy);
                }
                var feedback = control.GetComponent<FightInputFeedback>(); if (feedback != null) feedback.enabled = true;
                var outline = control.GetComponent<Outline>(); if (outline != null) outline.enabled = true;
                foreach (var frame in control.GetComponentsInChildren<FightFrame>(true)) if (frame.InputFrame) frame.enabled = true;
            }
            internal void Restore()
            {
                if (control == null) return;
                var feedback = control.GetComponent<FightInputFeedback>(); if (feedback != null) feedback.enabled = false;
                foreach (var frame in control.GetComponentsInChildren<FightFrame>(true)) if (frame.InputFrame) frame.enabled = false;
                if (tmp != null)
                {
                    tmp.caretColor = caret; tmp.customCaretColor = customCaret; tmp.selectionColor = selection;
                    tmp.caretWidth = width; tmp.caretBlinkRate = blink; tmp.richText = richText; tmp.isRichTextEditingAllowed = richEditing;
                    text.Restore(); placeholder.Restore(); if (mask != null) mask.enabled = maskEnabled;
                }
                else if (legacy != null)
                {
                    legacy.caretColor = caret; legacy.customCaretColor = customCaret; legacy.selectionColor = selection;
                    legacy.caretWidth = width; legacy.caretBlinkRate = blink;
                }
                var outline = control.GetComponent<Outline>();
                if (outline != null) { outline.effectColor = outlineColor; outline.enabled = outlineEnabled; }
            }
            private sealed class NativeInputText
            {
                private readonly TMP_Text value;
                private readonly bool rich, wrap;
                private readonly TextAlignmentOptions alignment;
                private readonly TextOverflowModes overflow;
                internal NativeInputText(TMP_Text source)
                {
                    value = source;
                    if (value == null) return;
                    rich = value.richText; wrap = value.enableWordWrapping; alignment = value.alignment; overflow = value.overflowMode;
                }
                internal void Restore()
                {
                    if (value == null) return;
                    value.richText = rich; value.enableWordWrapping = wrap; value.alignment = alignment; value.overflowMode = overflow;
                }
            }
        }
        private sealed class NativeUiRootMount
        {
            internal readonly RectTransform Rect;
            private readonly Transform parent;
            private readonly int sibling;
            private readonly Vector2 anchorMin, anchorMax, pivot, sizeDelta;
            private readonly Vector3 position, scale;
            private readonly Quaternion rotation;
            private readonly Canvas canvas;
            private readonly bool canvasEnabled, overrideSorting;
            private readonly int sortingOrder, sortingLayer;
            private readonly GraphicRaycaster raycaster;
            private readonly bool raycasterEnabled;
            private readonly GraphicRaycaster.BlockingObjects blockingObjects;
            private GraphicRaycaster addedRaycaster;
            internal NativeUiRootMount(RectTransform rect)
            {
                Rect = rect; parent = rect.parent; sibling = rect.GetSiblingIndex();
                anchorMin = rect.anchorMin; anchorMax = rect.anchorMax; pivot = rect.pivot; sizeDelta = rect.sizeDelta;
                position = rect.anchoredPosition3D; scale = rect.localScale; rotation = rect.localRotation;
                canvas = rect.GetComponent<Canvas>();
                if (canvas != null) { canvasEnabled = canvas.enabled; overrideSorting = canvas.overrideSorting; sortingOrder = canvas.sortingOrder; sortingLayer = canvas.sortingLayerID; }
                raycaster = rect.GetComponent<GraphicRaycaster>();
                if (raycaster != null) { raycasterEnabled = raycaster.enabled; blockingObjects = raycaster.blockingObjects; }
            }
            internal void Attach(Transform host)
            {
                Rect.SetParent(host, false); Rect.anchorMin = Vector2.zero; Rect.anchorMax = Vector2.one;
                Rect.pivot = new Vector2(.5f, .5f); Rect.sizeDelta = Vector2.zero; Rect.anchoredPosition3D = Vector3.zero;
                Rect.localScale = Vector3.one; Rect.localRotation = Quaternion.identity;
                if (canvas != null)
                {
                    canvas.enabled = true; canvas.overrideSorting = true; canvas.sortingOrder = 200; canvas.sortingLayerID = 0;
                    if (raycaster == null) addedRaycaster = Rect.gameObject.AddComponent<GraphicRaycaster>();
                }
                if (raycaster != null) { raycaster.enabled = true; raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None; }
            }
            internal void Restore()
            {
                if (Rect == null) return;
                Rect.SetParent(parent != null ? parent : null, false); Rect.anchorMin = anchorMin; Rect.anchorMax = anchorMax; Rect.pivot = pivot;
                Rect.sizeDelta = sizeDelta; Rect.anchoredPosition3D = position; Rect.localScale = scale; Rect.localRotation = rotation;
                if (parent != null) Rect.SetSiblingIndex(Mathf.Min(sibling, parent.childCount - 1));
                if (canvas != null) { canvas.enabled = canvasEnabled; canvas.overrideSorting = overrideSorting; canvas.sortingOrder = sortingOrder; canvas.sortingLayerID = sortingLayer; }
                if (raycaster != null) { raycaster.enabled = raycasterEnabled; raycaster.blockingObjects = blockingObjects; }
                if (addedRaycaster != null) { addedRaycaster.enabled = false; UnityEngine.Object.Destroy(addedRaycaster); }
            }
        }
        private void AdvanceClientFocus(bool reverse)
        {
            var parent = modal != null ? modal : root;
            var controls = parent.GetComponentsInChildren<Selectable>().Where(s => s.IsInteractable() && s.gameObject.activeInHierarchy)
                .Where(s => !(s is Scrollbar))
                .Where(s => s.GetComponent<FightFocusKey>()?.KeyboardTarget == null)
                .OrderByDescending(s => ClientFocusGroupPosition(s).y).ThenBy(s => ClientFocusGroupPosition(s).x)
                .ThenByDescending(s => ClientFocusPosition(s).y).ThenBy(s => ClientFocusPosition(s).x).ToArray();
            if (controls.Length == 0 || EventSystem.current == null) return;
            var current = EventSystem.current.currentSelectedGameObject;
            var proxy = current == null ? null : current.GetComponent<FightFocusKey>()?.KeyboardTarget;
            if (proxy != null) current = proxy.gameObject;
            int index = Array.FindIndex(controls, s => s.gameObject == current);
            index = index < 0 ? (reverse ? controls.Length - 1 : 0) : (index + (reverse ? -1 : 1) + controls.Length) % controls.Length;
            var selected = controls[index]; selected.Select(); RevealClientControl(selected);
        }
        private static void RevealClientControl(Selectable selected)
        {
            foreach (var scroll in selected.GetComponentsInParent<ScrollRect>())
            {
                if (scroll.content == null || scroll.viewport == null || !selected.transform.IsChildOf(scroll.content)) continue;
                var corners = new Vector3[4]; selected.GetComponent<RectTransform>().GetWorldCorners(corners);
                if (scroll.vertical)
                {
                    float range = scroll.content.rect.height - scroll.viewport.rect.height;
                    if (range > 0)
                    {
                        float top = scroll.content.rect.yMax - scroll.content.InverseTransformPoint(corners[1]).y;
                        float bottom = scroll.content.rect.yMax - scroll.content.InverseTransformPoint(corners[0]).y;
                        float offset = (1 - scroll.verticalNormalizedPosition) * range;
                        if (top < offset + 8) offset = top - 8;
                        else if (bottom > offset + scroll.viewport.rect.height - 8) offset = bottom - scroll.viewport.rect.height + 8;
                        scroll.StopMovement(); scroll.verticalNormalizedPosition = 1 - Mathf.Clamp(offset, 0, range) / range;
                    }
                }
                if (scroll.horizontal)
                {
                    float range = scroll.content.rect.width - scroll.viewport.rect.width;
                    if (range > 0)
                    {
                        float left = scroll.content.InverseTransformPoint(corners[0]).x - scroll.content.rect.xMin;
                        float right = scroll.content.InverseTransformPoint(corners[3]).x - scroll.content.rect.xMin;
                        float offset = scroll.horizontalNormalizedPosition * range;
                        if (left < offset + 8) offset = left - 8;
                        else if (right > offset + scroll.viewport.rect.width - 8) offset = right - scroll.viewport.rect.width + 8;
                        scroll.StopMovement(); scroll.horizontalNormalizedPosition = Mathf.Clamp(offset, 0, range) / range;
                    }
                }
            }
        }
        private static Vector3 ClientFocusPosition(Selectable control)
        {
            var position = control.transform.position;
            foreach (var scroll in control.GetComponentsInParent<ScrollRect>())
                if (scroll.content != null && control.transform.IsChildOf(scroll.content))
                    position -= scroll.content.parent.TransformVector(new Vector3(0, scroll.content.anchoredPosition.y, 0));
            return position;
        }
        private Vector3 ClientFocusGroupPosition(Selectable control)
        {
            var group = control.transform;
            while (group.parent != null && group.parent != Body && group.parent != root && group.parent != modal)
                group = group.parent;
            return group.position;
        }
        internal void RememberFocusForRefresh(Transform clearing)
        {
            if (EventSystem.current == null || clientCanvas == null || !clientCanvas.enabled || renderedPage != page) return;
            var captured = CaptureFocusedControl(clearing);
            if (captured == null) return;
            refreshingFocus = captured;
            StartCoroutine(RestoreRefreshedFocus(captured));
        }
        private RefreshFocus CaptureFocusedControl(Transform clearing)
        {
            if (EventSystem.current == null) return null;
            var current = EventSystem.current.currentSelectedGameObject;
            if (current == null || !current.transform.IsChildOf(clearing)) return null;
            var control = current.GetComponent<Selectable>(); if (control == null) return null;
            return new RefreshFocus
            {
                Page = page, Name = current.name.TrimStart('◆', '✓', '◇', ' '), Type = control.GetType(),
                Hint = current.GetComponent<FightTooltip>()?.Hint, Parent = current.transform.parent?.name,
                FieldKey = settingsFieldAnchors.FirstOrDefault(pair => pair.Value != null && pair.Value.gameObject == current).Key,
                StableKey = current.GetComponent<FightFocusKey>()?.Key,
                Position = root.InverseTransformPoint(ClientFocusPosition(control)), InDialog = modal != null && current.transform.IsChildOf(modal), Dialog = modal
            };
        }
        private IEnumerator RestoreRefreshedFocus(RefreshFocus captured)
        {
            yield return null;
            if (captured != refreshingFocus || captured.Page != page || EventSystem.current == null || !clientCanvas.enabled
                || (captured.InDialog ? captured.Dialog == null || modal != captured.Dialog : modal != null)) yield break;
            refreshingFocus = null;
            var scope = captured.InDialog ? modal : root;
            var current = EventSystem.current.currentSelectedGameObject;
            // Explicit error-field focus takes precedence over a deferred refresh restore.
            if (current != null && current.activeInHierarchy && current.transform.IsChildOf(scope)
                && current.GetComponent<Selectable>() is Selectable currentControl && currentControl.IsInteractable()) yield break;
            Selectable match = null;
            if (!string.IsNullOrEmpty(captured.StableKey))
                match = scope.GetComponentsInChildren<Selectable>().FirstOrDefault(control => control.IsInteractable()
                    && control.GetComponent<FightFocusKey>()?.Key == captured.StableKey);
            if (match == null && !string.IsNullOrEmpty(captured.FieldKey)
                && settingsFieldAnchors.TryGetValue(captured.FieldKey, out var anchor) && anchor != null)
                match = anchor.GetComponent<Selectable>();
            if (match == null)
                match = scope.GetComponentsInChildren<Selectable>().Where(control => control.IsInteractable() && control.GetType() == captured.Type
                    && control.name.TrimStart('◆', '✓', '◇', ' ') == captured.Name
                    && (captured.Name != "Card Image" || control.transform.parent?.name == captured.Parent)
                    && (string.IsNullOrEmpty(captured.Hint) || control.GetComponent<FightTooltip>()?.Hint == captured.Hint))
                    .OrderBy(control => (root.InverseTransformPoint(ClientFocusPosition(control)) - captured.Position).sqrMagnitude).FirstOrDefault();
            if (match != null && match.IsInteractable()) match.Select();
            else if (captured.InDialog) StartCoroutine(FocusNewDialog(modal));
        }
        internal void Navigate(string next)
        {
            if (string.IsNullOrEmpty(next)) next = "home";
            if (!TryLeaveAppearance(next)) return;
            if (next == "exit")
            {
                if (page == "editor" && EditorDirty) { Unsaved("@quit", true); return; }
                if (page == "settings" && SettingsDirty) { Unsaved("@quit", false); return; }
                Program.GameQuit(); return;
            }
            if (page == "editor" && next != "editor" && EditorDirty) { Unsaved(next, true); return; }
            if (page == "settings" && next != "settings" && SettingsDirty) { Unsaved(next, false); return; }
            NavigateNow(next);
        }
        private void Unsaved(string next, bool editor)
        {
            var panel = ShowDialog("尚有未保存的修改", 640, 192);
            FightUI.Button(panel, "取消", 28, 112, 160, 48, CloseDialog);
            var discard = FightUI.Button(panel, "放弃修改", 208, 112, 160, 48, () => { if (editor) DiscardEditor(); else DiscardSettings(); NavigateNow(next); });
            FightUI.StyleDestructiveButton(discard);
            FightUI.Button(panel, "保存并继续", 388, 112, 224, 48, () =>
            {
                if (editor) SaveEditorThen(() => NavigateNow(next));
                else { CloseDialog(); if (SaveSettings()) NavigateNow(next); }
            }, true);
        }
        private void NavigateNow(string next)
        {
#if FIGHT_UI_PROOF
            if (Environment.GetEnvironmentVariable("FIGHT_UI_TEST") == "1")
                Debug.Log("[FightProofNavigation] " + page + " -> " + next + " cancel=" + UserInput.WasCancelPressed);
#endif
            if (next == "@quit") { Program.GameQuit(); return; }
            if (next == "exit") { Navigate("exit"); return; }
            if (next != page) ResetClientStatus();
            CaptureNavigationState();
            navigationFrame = Time.frameCount;
            if (page == "settings" && next != "settings") CancelSettingsTest();
            ReleaseEditor(); CloseDialog();
            if (next != "editor" && Program.instance.currentServant != Program.instance.menu) Program.instance.ShiftToServant(Program.instance.menu);
            page = next;
            if (!navigationStates.TryGetValue(page, out var restored)) restored = new ClientNavigationState();
            search = restored.Search; deckGroup = restored.Group; selectedPath = restored.SelectedPath;
            Rebuild();
        }
        private void CaptureNavigationState()
        {
            if (Body == null || renderedPage == null) return;
            if (!navigationStates.TryGetValue(renderedPage, out var state)) navigationStates[renderedPage] = state = new ClientNavigationState();
            if (renderedPage == page) { state.Search = search; state.Group = deckGroup; state.SelectedPath = selectedPath; }
            state.Scrolls = Body.GetComponentsInChildren<ScrollRect>().Where(s => s.name == "Scroll" && s.vertical)
                .Select(s => s.content != null && s.viewport != null && s.content.rect.height <= s.viewport.rect.height + 1
                    ? 1f : Mathf.Clamp01(s.verticalNormalizedPosition)).ToArray();
        }
        private void Rebuild()
        {
            if (root == null) return;
            fullscreenReflowPending = false; retainedPageLayout = null;
            SelectViewportTemplate(); FitClientViewport();
            CaptureNavigationState();
            CleanupCollectionPreview();
            ReleaseEditor(); CloseDialog(); FightUI.Clear(root);
            oldWidth = Screen.width; oldHeight = Screen.height; oldFullscreen = UsesFullscreenLayout;
            var bg = FightUI.Panel(root, 0, 0, W, H, FightUI.Background, false); bg.name = "Client Background";
            clientBackground = bg;
            bg.GetComponent<Image>().sprite = FightSkin.Ambient; bg.GetComponent<Image>().color = Color.white;
            DrawHeader(); Body = FightUI.Node(root, "Client Page " + page, Gap, BodyTop, BodyWidth, BodyHeight);
            Body.gameObject.AddComponent<FightPageReveal>();
            renderedPage = page;
            switch (page)
            {
                case "home": RenderHome(); break;
                case "modes": RenderModes(); break;
                case "cpu": RenderCpu(); break;
                case "ai": case "spectator": RenderAI(); break;
                case "decks": case "recommended": case "competitive": case "picker": RenderDecks(); break;
                case "deck-detail": RenderDeckDetail(); break;
                case "editor": RenderEditor(); break;
                case "academy": RenderAcademy(); break;
                case "tutorial": RenderTutorial(); break;
                case "puzzles": case "puzzle": RenderPuzzles(); break;
                case "replays": RenderReplays(); break;
                case "collection": case "animations": case "appearance": RenderCollection(); break;
                case "settings": RenderSettings(); break;
                default: page = "home"; RenderHome(); break;
            }
            if (page != "settings" && navigationStates.TryGetValue(page, out var state) && state.Scrolls.Length > 0)
            {
                Canvas.ForceUpdateCanvases();
                var scrolls = Body.GetComponentsInChildren<ScrollRect>().Where(s => s.name == "Scroll" && s.vertical).ToArray();
                for (int i = 0; i < Mathf.Min(scrolls.Length, state.Scrolls.Length); i++) scrolls[i].verticalNormalizedPosition = state.Scrolls[i];
            }
            DrawFooter();
        }
        private void DrawFooter()
        {
            var foot = FightUI.Panel(root, 0, H - Footer, W, Footer, FightUI.PanelColor, false, "Client Footer");
            clientFooter = foot;
            FightUI.Line(foot, 0, 0, W, FightUI.Border);
            statusIcon = FightUI.Icon(foot, "diamond", Gap + 4, (Footer - (Compact ? 18 : 22)) / 2, Compact ? 18 : 22, Compact ? 18 : 22);
            float statusX = Gap + (Compact ? 32 : 40), statusWidth = W - Gap - 160 - statusX - 16;
            statusInspectButton = FightUI.TextAction(foot, "状态提示", statusX, 0, statusWidth, Footer, ShowClientStatusDetails, false);
            FightUI.FocusKey(statusInspectButton, "client:status-details");
            var statusColors = statusInspectButton.colors;
            statusColors.normalColor = statusColors.disabledColor = Color.white;
            statusColors.highlightedColor = new Color(1.18f, 1.18f, 1.18f);
            statusColors.selectedColor = Color.white; statusInspectButton.colors = statusColors;
            statusText = statusInspectButton.GetComponentInChildren<TextMeshProUGUI>();
            statusText.fontSize = Compact ? 16 : 18; statusText.fontStyle = FontStyles.Normal;
            statusText.richText = false; statusText.enableWordWrapping = false;
            FightUI.Place(statusText.rectTransform, 0, 0, statusWidth - 26, Footer);
            statusInspectArrow = FightUI.Icon(statusInspectButton.transform, "arrow", statusWidth - 20, (Footer - 14) / 2, 14, 14, FightUI.Muted);
            RefreshFooterStatus();
            fpsText = FightUI.Text(foot, "", W - Gap - 160, 0, 160, Footer, Compact ? 16 : 18, FightUI.Muted, TextAlignmentOptions.MidlineRight);
        }
        private void DrawHeader()
        {
            var top = FightUI.Panel(root, 0, 0, W, Header, FightUI.Background, false, "Client Header");
            clientHeader = top;
            FightUI.Line(top, 0, Header - 1, W, FightUI.Border);
            FightUI.Line(top, 0, Header - 2, W * .34f, new Color(.4f, .33f, .20f, .32f));
            var brandW = Compact ? 176 : LargeViewport ? 204 : 240; var playW = Compact ? 184 : LargeViewport ? 220 : 256;
            var brand = FightUI.Asset(top, "brand.png", Gap + (Compact ? 4 : 8), Compact ? 8 : 10, brandW - (Compact ? 8 : 16), Header - (Compact ? 14 : 20), false);
            brand.raycastTarget = true;
            var brandButton = brand.gameObject.AddComponent<Button>(); brandButton.targetGraphic = brand;
            brandButton.onClick.AddListener(() => Navigate("home"));
            FightUI.FocusKey(brandButton, "navigation:brand");
            var brandOutline = brand.gameObject.AddComponent<FightOutline>(); brandOutline.enabled = false;
            var brandFrameRoot = FightUI.Node(brand.transform, "Brand focus frame", 0, 0, 0, 0); FightUI.Stretch(brandFrameRoot);
            var brandFrame = brandFrameRoot.gameObject.AddComponent<FightFrame>(); brandFrame.ButtonFrame = true;
            brandFrame.Bind(brandOutline);
            brand.gameObject.AddComponent<FightButtonFeedback>().Bind(brandFrame);
            FightUI.Hint(brand.gameObject, "返回首页");
            float playX = brandW + Gap * 2, playH = Compact ? 56 : LargeViewport ? 60 : 72, playY = (Header - playH) / 2;
            var play = FightUI.Button(top, "◇  开始决斗", playX, playY, playW, playH, () => Navigate("modes"), true);
            play.GetComponent<Image>().sprite = FightSkin.ActionSurface; play.GetComponent<Image>().color = FightUI.ActionFill;
            play.GetComponent<Outline>().effectColor = FightUI.Cyan;
            float duelIcon = Compact ? 30 : LargeViewport ? 34 : 44;
            FightUI.Icon(play.transform, "duel", Compact ? 14 : 20, (playH - duelIcon) / 2, duelIcon, duelIcon, FightUI.Active);
            var playText = play.GetComponentInChildren<TextMeshProUGUI>(); playText.text = "开始决斗"; playText.fontSize = Compact ? 26 : LargeViewport ? 28 : 34; playText.fontStyle = FontStyles.Bold;
            float textLeft = Compact ? 52 : LargeViewport ? 64 : 76;
            FightUI.Place(playText.rectTransform, textLeft, 0, playW - textLeft - 10, playH);
            string[] labels = { "首页", "卡组", "学院", "回放", "收藏" };
            string[] routes = { "home", "decks", "academy", "replays", "collection" };
            float nx = playX + playW + Gap * 1.5f, nw = Compact ? 110 : LargeViewport ? 144 : 160;
            for (int i = 0; i < labels.Length; i++)
            {
                var route = routes[i]; var b = FightUI.Button(top, labels[i], nx + nw * i + 3, 22, nw - 6, Header - 42,
                    () => Navigate(route));
                b.GetComponent<Image>().color = Color.clear; b.GetComponent<Outline>().enabled = false;
                FightUI.FocusKey(b, "navigation:" + route);
                foreach (var t in b.GetComponentsInChildren<TextMeshProUGUI>()) { t.fontSize = Compact ? 22 : LargeViewport ? 24 : 27; t.characterSpacing = Compact ? 1.5f : 2; t.color = ActiveSection() == route ? FightUI.Active : FightUI.Muted; }
                if (ActiveSection() == route)
                {
                    b.GetComponent<Image>().color = new Color(.14f, .20f, .23f, .3f);
                    FightUI.Line(top, nx + nw * i + 14, Header - 20, nw - 28, FightUI.GoldBorder);
                    FightUI.Icon(top, "diamond", nx + nw * i + nw / 2 - 7, Header - 27, 14, 14, FightUI.Active);
                }
            }
            FightUI.Panel(top, W - Gap - (Compact ? 126 : LargeViewport ? 148 : 176), Header * .32f,
                1, Header * .36f, FightUI.Border, false, "Utility divider").GetComponent<Image>().raycastTarget = false;
            float iconW = Compact ? 52 : LargeViewport ? 60 : 72, iconSize = Compact ? 28 : LargeViewport ? 32 : 40;
            HeaderIcon(top, "设置", "settings", W - Gap - iconW * 2 - (Compact ? 10 : 24), iconW, iconSize, () => OpenSettings(), ActiveSection() == "settings");
            HeaderIcon(top, "退出", "exit", W - Gap - iconW, iconW, iconSize, () => Navigate("exit"), false);
        }
        private void HeaderIcon(Transform top, string label, string icon, float x, float width, float iconSize, Action action, bool selected)
        {
            var b = FightUI.Button(top, label, x, 16, width, Header - 32, action);
            b.GetComponent<Image>().color = Color.clear; b.GetComponent<Outline>().enabled = false;
            b.GetComponentInChildren<TextMeshProUGUI>().gameObject.SetActive(false);
            FightUI.Icon(b.transform, icon, (width - iconSize) / 2, (Header - 32 - iconSize) / 2, iconSize, iconSize, selected ? FightUI.Active : FightUI.Gold);
            FightUI.Hint(b.gameObject, label);
            if (selected) FightUI.Line(top, x + 8, Header - 20, width - 16, FightUI.Gold);
        }
        private string ActiveSection()
        {
            if (new[] { "decks", "recommended", "competitive", "deck-detail", "editor", "picker" }.Contains(page)) return "decks";
            if (new[] { "academy", "tutorial", "puzzles", "puzzle" }.Contains(page)) return "academy";
            if (new[] { "collection", "animations", "appearance" }.Contains(page)) return "collection";
            if (page == "cpu" || page == "ai" || page == "spectator") return "modes";
            return page;
        }
        private void ResetClientStatus()
        {
            status = "准备就绪"; statusUntil = 0; statusIsError = false;
            RefreshFooterStatus();
        }
        private void RefreshFooterStatus()
        {
            if (statusText == null || statusInspectButton == null) return;
            bool available = statusUntil != 0;
            statusText.text = available ? status : ""; statusText.color = statusIsError ? FightUI.Danger : FightUI.Muted;
            statusInspectButton.interactable = available;
            FightUI.Hint(statusInspectButton.gameObject, available ? "查看完整操作提示" : "");
            FightUI.Hint(statusText.gameObject, available ? status + "\n单击查看完整提示" : "");
            if (statusIcon != null) { statusIcon.gameObject.SetActive(available); statusIcon.Kind = statusIsError ? "alert" : "diamond"; statusIcon.color = statusIsError ? FightUI.Danger : FightUI.Gold; }
            if (statusInspectArrow != null) { statusInspectArrow.gameObject.SetActive(available); statusInspectArrow.color = statusText.color; }
            var underline = statusInspectButton.transform.Find("Action underline");
            if (underline != null)
            {
                var rect = underline.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(Mathf.Min(statusText.rectTransform.rect.width, statusText.GetPreferredValues(status, float.PositiveInfinity, float.PositiveInfinity).x), 1);
                underline.GetComponent<Image>().color = statusText.color;
            }
        }
        internal void ShowStatus(string message) => ShowStatus(message, false);
        internal void ShowStatus(string message, bool error)
        {
            status = string.IsNullOrWhiteSpace(message) ? error ? "操作未完成，请重试。" : "准备就绪" : message;
            statusIsError = error;
            statusUntil = error ? -1 : Time.unscaledTime + Mathf.Clamp(5 + status.Length * .15f, 8, 20);
            RefreshFooterStatus();
            Debug.Log("[FightClient] " + status);
        }
        private void ShowClientStatusDetails()
        {
            if (statusUntil == 0) return;
            string message = status; bool error = statusIsError;
            float width = Mathf.Min(920, W - 48), height = Mathf.Min(560, H - 48);
            var box = ShowDialog(error ? "操作未完成" : "操作提示", width, height);
            var text = FightUI.Text(box, message, 28, 88, width - 74, 120, 20, error ? FightUI.Danger : FightUI.Foreground);
            text.text = message; text.richText = false; text.enableWordWrapping = true; text.overflowMode = TextOverflowModes.Overflow;
            text.lineSpacing = 4; text.paragraphSpacing = 6; text.name = "Complete operation message";
            float preferred = text.GetPreferredValues(message, width - 74, float.PositiveInfinity).y + 4;
            var reading = FightUI.Scroll(box, 28, 88, width - 56, height - 170, preferred);
            text.rectTransform.SetParent(reading, false);
            FightUI.Place(text.rectTransform, 0, 0, width - 74, preferred);
            FightUI.Line(box, 28, height - 74, width - 56);
            Button copy = null;
            copy = FightUI.Button(box, "复制提示", width - 196, height - 66, 168, 48, () =>
            {
                GUIUtility.systemCopyBuffer = message;
                copy.GetComponentInChildren<TextMeshProUGUI>().text = "已复制";
            });
        }
        internal RectTransform ShowDialog(string title, float width = 620, float height = 360, bool refresh = false)
        {
            bool refreshingDialog = refresh && modal != null;
            var retainedOrigin = dialogOriginFocus;
            var retainedFocus = refreshingDialog ? CaptureFocusedControl(modal) : null;
            CloseDialog(); dialogOriginFocus = refreshingDialog ? retainedOrigin : EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            modal = FightUI.Panel(root, 0, 0, W, H, new Color(0, 0, 0, .68f), false, "Modal");
            width = Mathf.Min(width, W - 48); height = Mathf.Min(height, H - 48);
            var box = FightUI.Panel(modal, (W - width) / 2, (H - height) / 2, width, height);
            clientModalBox = box; clientModalSize = new Vector2(width, height);
            box.gameObject.AddComponent<FightPageReveal>();
            var heading = FightUI.Text(box, title, 28, 22, width - 96, 42, 27, FightUI.Active); heading.characterSpacing = 1;
            heading.text = title; heading.enableWordWrapping = false; heading.enableAutoSizing = true;
            heading.fontSizeMin = 22; heading.fontSizeMax = 27; heading.maxVisibleLines = 1;
            FightUI.Hint(heading.gameObject, title);
            FightUI.Line(box, 28, 67, width - 56, FightUI.Border);
            var close = FightUI.Button(box, "×", width - 64, 18, 40, 40, CloseDialog);
            close.GetComponent<Image>().color = Color.clear; close.GetComponent<Outline>().enabled = false;
            close.GetComponentInChildren<TextMeshProUGUI>().gameObject.SetActive(false);
            FightUI.Icon(close.transform, "close", 10, 10, 20, 20, FightUI.Gold);
            FightUI.Hint(close.gameObject, "关闭 · Esc");
            if (retainedFocus != null)
            {
                retainedFocus.Dialog = modal; refreshingFocus = retainedFocus;
                StartCoroutine(RestoreRefreshedFocus(retainedFocus));
            }
            else StartCoroutine(FocusNewDialog(modal));
            return box;
        }
        private IEnumerator FocusNewDialog(RectTransform opened)
        {
            yield return null;
            if (opened == null || modal != opened || EventSystem.current == null || !clientCanvas.enabled) yield break;
            if (refreshingFocus != null && refreshingFocus.InDialog && refreshingFocus.Dialog == opened) yield break;
            var current = EventSystem.current.currentSelectedGameObject;
            if (current != null && current.transform.IsChildOf(opened)) yield break;
            var choices = opened.GetComponentsInChildren<Button>().Where(button => button.IsInteractable()).ToArray();
            var safe = choices.FirstOrDefault(button => button.name.StartsWith("取消", StringComparison.Ordinal))
                ?? choices.FirstOrDefault(button => button.name == "×");
            if (safe != null) safe.Select();
        }
        internal void CloseDialog()
        {
            CancelEditorSaveContinuation();
            if (modal == null) return;
            modal.gameObject.SetActive(false); Destroy(modal.gameObject); modal = null;
            clientModalBox = null;
            if (dialogOriginFocus != null && dialogOriginFocus.activeInHierarchy && clientCanvas != null && clientCanvas.enabled && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(dialogOriginFocus);
            dialogOriginFocus = null;
        }
        internal void Confirm(string title, string message, Action accepted)
        {
            var box = ShowDialog(title, 620, 280);
            float width = box.rect.width, readingWidth = width - 56;
            var body = FightUI.Text(box, message, 28, 88, readingWidth, 94, 20);
            body.enableWordWrapping = true;
            float preferred = body.GetPreferredValues(message, readingWidth, float.PositiveInfinity).y + 2;
            float readingHeight = Mathf.Min(Mathf.Max(94, preferred), H - 234);
            float height = readingHeight + 186, actionsY = height - 72;
            FightUI.Place(box, (W - width) / 2, (H - height) / 2, width, height);
            clientModalSize = new Vector2(width, height);
            if (preferred > readingHeight)
            {
                var reading = FightUI.Scroll(box, 28, 88, readingWidth, readingHeight, preferred);
                body.rectTransform.SetParent(reading, false);
                readingWidth -= 22;
                preferred = body.GetPreferredValues(message, readingWidth, float.PositiveInfinity).y + 2;
                reading.sizeDelta = new Vector2(reading.rect.width, preferred);
                FightUI.Place(body.rectTransform, 0, 0, readingWidth, preferred);
            }
            else FightUI.Place(body.rectTransform, 28, 88, readingWidth, readingHeight);
            FightUI.Button(box, "取消", width - 360, actionsY, 144, 48, CloseDialog);
            string verb = new[] { "删除", "清除", "清空", "重置", "覆盖" }.FirstOrDefault(action => title.StartsWith(action, StringComparison.Ordinal));
            var confirm = FightUI.Button(box, verb == null ? "确认" : "确认" + verb, width - 196, actionsY, 168, 48,
                () => { CloseDialog(); Try(accepted); }, verb == null);
            if (verb != null) FightUI.StyleDestructiveButton(confirm);
        }
        private void Try(Action action) { try { action?.Invoke(); } catch (Exception e) { ShowStatus(e.Message, true); Debug.LogWarning("[FightClient] " + e); } }
        private void PageTitle(string title)
        {
            var heading = FightUI.HeroTitle(Body, title, 0, 2, BodyWidth * .6f, 48, Compact ? 30 : 36);
            heading.characterSpacing = .8f;
        }
        private void StartCore(Action start)
        {
            coreSourcePage = page;
            CloseDialog(); CancelSettingsTest(); CleanupCollectionPreview();
            bool started = false;
            Try(() => { start?.Invoke(); started = true; });
            if (!started && Program.instance.currentServant != Program.instance.ocgcore) { coreSourcePage = null; return; }
            if (Program.instance.currentServant == Program.instance.ocgcore) SynchronizePresentation();
            else ShowStatus("正在启动…");
        }
        private void OnDestroy() { CancelSettingsTest(); CleanupCollectionPreview(); ReleaseEditor(); RestoreNativeUiRoots(); if (Instance == this) Instance = null; }
    }
}
