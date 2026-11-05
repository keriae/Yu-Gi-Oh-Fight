using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MDPro3.Duel.YGOSharp;
using UnityEngine.AddressableAssets;
using MDPro3.Net;
using MDPro3.Servant;
using Cysharp.Threading.Tasks;
using MDPro3.Utility;
using MDPro3.IO;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MDPro3
{
    public class Program : MonoBehaviour
    {
#if UNITY_EDITOR
        [Header("Debug")]
        public bool debugGameMessage;
        public bool debugCardAnimation;
        public AnimationCurve curve;
#endif

        [Header("Public References")]
        public Transform container_3D;
        public Transform container_2D;
        public CardRenderer cardRenderer;

        [Header("Manager")]
        public CameraManager camera_;
        public UIManager ui_;
        public BackgroundManager background_;
        public AudioManager audio_;
        public TextureManager texture_;
        public MessageManager message_;

        [Header("Servants")]
        public Servant.MainMenu menu;
        public SoloSelector solo;
        public OnlineServant online;
        public PuzzlePortal puzzlePortal;
        public PuzzleSelector puzzle;
        public ReplaySelector replay;
        public CutinViewer cutin;
        public MateViewer mate;
        public DeckSelector deckSelector;
        public SettingServant setting;
        public Appearance appearance;
        public CharacterSelector character;
        public OcgCore ocgcore;
        public RoomServant room;
        public DeckEditor deckEditor;
        public OnlineDeckViewer onlineDeckViewer;
        public DeckBrowser deckBrowser;

#if UNITY_EDITOR
        [Header("Test")]
        public float testFloat = 1f;
        public float testFloat2 = 1f;
        public float testFloat3 = 1f;
        public float testFloat4 = 1f;
        public float testFloat5 = 1f;
#endif

        [HideInInspector]
        public Servant.Servant currentServant;
        [HideInInspector]
        public Servant.Servant lastServant;
        [HideInInspector]
        public Servant.Servant currentSubServant;
        [HideInInspector]
        public int depth;

        #region Const
        public static bool Running = true;
        public const string PATH_ART = "Picture/Art/";
        public const string PATH_ALT_ART = "Picture/Art2/";
        public const string PATH_CARD_PIC = "Picture/CardGenerated/";
        public const string PATH_CLOSEUP = "Picture/Closeup/";
        public const string PATH_OVER_FRAME = "Picture/OverFrame/";
        public const string PATH_DATA = "Data/";
        public const string PATH_LOCALES = "Data/locales/";
        public const string PATH_CONFIG = "Data/config.conf";
        public const string PATH_DECK = "Deck/";
        public const string PATH_EXPANSIONS = "Expansions/";
        public const string PATH_PUZZLE = "Puzzle/";
        public const string PATH_REPLAY = "Replay/";
        public const string PATH_DIY = "Picture/DIY/";
        public const string PATH_VIDEO_ART = "Video/Art/";

        public const string EXPANSION_CONF = ".conf";
        public const string EXPANSION_YDK = ".ydk";
        public const string EXPANSION_PNG = ".png";
        public const string EXPANSION_JPG = ".jpg";
        public const string EXPANSION_YRP = ".yrp";
        public const string EXPANSION_YRP3D = ".yrp3d";
        public const string EXPANSION_LUA = ".lua";
        public const string EXPANSION_MP4 = ".mp4";
        public const string STRING_SLASH = "/";
        public const string STRING_LINE_BREAK = "\r\n";
        public const string SCRIPT_ZIP = "script.zip";

        #endregion

        #region Initializement

        public static Program instance;
        public static Items items;

        private readonly List<Manager> managers = new();
        private readonly List<Servant.Servant> servants = new();
        public static bool exitOnReturn = false;

        private async UniTask Initialize()
        {
            try { await InitializeRuntime(); }
            catch (Exception error)
            {
                UnityEngine.Debug.LogError("[FightPresentation] runtime initialization: " + error);
                UI.Client.FightStartupPresentation.ReportFailure("游戏初始化失败：" + error.Message);
            }
        }

        private async UniTask InitializeRuntime()
        {
            if (!Directory.Exists(PATH_DATA))
                Directory.CreateDirectory(PATH_DATA);
            Config.Initialize(PATH_CONFIG);

            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            await ResourceManager.InitializeAsync();
            if(!ABLoader.mdCached)
                await ABLoader.CacheMasterDuelOutDuelBundles();

            if(items == null)
            {
                var handle = Addressables.LoadAssetAsync<Items>("ScriptableObjects/Items.asset");
                await handle.Task;
                items = handle.Result;
            }

            cardRenderer = (await Addressables.InstantiateAsync("Prefab/CardRenderer.prefab")).GetComponent<CardRenderer>();

            items.Initialize();
            await BanlistManager.InitializeAsync();
            await CardImageLoader.InitializeAsync();
            _ = OnlineService.InitializeAsync();

            InitializeAllManagers();
            InitializeAllServants();
#if FIGHT_UI_PROOF
            Utility.RuntimeFramePacingProof.RunIfRequested();
#endif
        }

        public void ReadParams()
        {
            var args = Environment.GetCommandLineArgs();
            //args = new string[11]
            //{
                //"-r",
                //"TURN023"

                //"-s",
                //"6ace for win!"

            //    "-d",
            //    "LL铁兽",

            //    "-n",
            //    "赤子奈落",

            //    "-h",
            //    "mygo.superpre.pro",
            //    "-p",
            //    "888",
            //    "-w",
            //    "M#1008611",
            //    "-j"
            //};

            string nick = null;
            string host = null;
            string port = null;
            string password = null;
            string deck = null;
            string replay = null;
            string puzzle = null;
            var join = false;
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower() == "-n" && args.Length > i + 1)
                {
                    nick = args[++i];
                    Config.Set("DuelPlayerName0", nick);
                    Config.Save();
                }

                if (args[i].ToLower() == "-h" && args.Length > i + 1) 
                    host = args[++i];
                if (args[i].ToLower() == "-p" && args.Length > i + 1) 
                    port = args[++i];
                if (args[i].ToLower() == "-w" && args.Length > i + 1)
                    password = args[++i];
                if (args[i].ToLower() == "-d" && args.Length > i + 1)
                    deck = args[++i];
                if (args[i].ToLower() == "-r" && args.Length > i + 1)
                    replay = args[++i];
                if (args[i].ToLower() == "-s" && args.Length > i + 1)
                    puzzle = args[++i];

                if (args[i].ToLower() == "-j")
                {
                    join = true;
                    Config.SetConfigDeck(deck);
                    Config.Save();
                }
            }

            if (join)
            {
                exitOnReturn = true;
                if (string.IsNullOrWhiteSpace(nick) || string.IsNullOrWhiteSpace(host)
                    || !int.TryParse(port, out int parsedPort) || parsedPort < 1 || parsedPort > 65535)
                    throw new ArgumentException("联机启动需要用户名、主机地址和有效端口。");
                UI.Client.FightStartupPresentation.BeginNativeLaunch("online");
                online.KF_OnlineGame(nick, host, port, password);
            }
            else if (deck != null)
            {
                exitOnReturn = true;
                if (!File.Exists(PATH_DECK + deck + EXPANSION_YDK)) throw new FileNotFoundException("找不到卡组：" + deck);
                UI.Client.FightStartupPresentation.BeginNativeLaunch("editor");
                Config.SetConfigDeck(deck);
                deckEditor.SwitchCondition(DeckEditor.Condition.EditDeck);
                ShiftToServant(deckEditor);
            }
            else if (replay != null)
            {
                exitOnReturn = true;
                var replayPath = PATH_REPLAY + replay + (replay.EndsWith(EXPANSION_YRP, StringComparison.OrdinalIgnoreCase) ? string.Empty : EXPANSION_YRP3D);
                if (!File.Exists(replayPath) && !File.Exists(replayPath.Replace(EXPANSION_YRP3D, EXPANSION_YRP)))
                    throw new FileNotFoundException("找不到回放：" + replay);
                UI.Client.FightStartupPresentation.BeginNativeLaunch("replay");
                this.replay.PlayReplay(replay);
            }
            else if (puzzle != null)
            {
                exitOnReturn = true;
                if (!File.Exists(PATH_PUZZLE + puzzle + EXPANSION_LUA)) throw new FileNotFoundException("找不到残局：" + puzzle);
                UI.Client.FightStartupPresentation.BeginNativeLaunch("puzzle");
                this.puzzle.StartPuzzle(PATH_PUZZLE + puzzle);
                if (currentServant != ocgcore) throw new InvalidOperationException("残局未能启动：" + puzzle);
            }
        }

        public void InitializeForDataChange(Action delegation = null)
        {
            UIManager.ShowLoading();
            _ = InitializeForDataChangeAsync(delegation);
        }

        private async UniTask InitializeForDataChangeAsync(Action delegation)
        {
            await ResourceManager.InitializeAsync();
            await BanlistManager.InitializeAsync();
            await OnlineService.InitializeAsync();
            await CardImageLoader.InitializeAsync();

            await UniTask.RunOnThreadPool(() =>
            {
                StringHelper.Initialize();
                CardsManager.Initialize();
                delegation?.Invoke();
            });

            UIManager.HideLoading();
        }

        private void InitializeAllManagers()
        {
            managers.Add(texture_);
            managers.Add(ui_);
            managers.Add(camera_);
            managers.Add(audio_);
            managers.Add(background_);
            managers.Add(message_);

            foreach (Manager manager in managers)
                manager.Initialize();
        }

        private void InitializeAllServants()
        {
            servants.Add(setting);
            servants.Add(menu);
            servants.Add(solo);
            servants.Add(online);
            servants.Add(puzzlePortal);
            servants.Add(puzzle);
            servants.Add(replay);
            servants.Add(cutin);
            servants.Add(mate);
            servants.Add(deckSelector);
            servants.Add(appearance);
            servants.Add(character);
            servants.Add(ocgcore);
            servants.Add(room);
            servants.Add(deckEditor);
            servants.Add(onlineDeckViewer);
            servants.Add(deckBrowser);
            foreach (Servant.Servant servant in servants)
                servant.Initialize();
        }

        #endregion

        #region MonoBehaviors

        public const string PATH_ROOT_EDITOR = "Platforms/";
        public const string PATH_ROOT_WINDOWS64 = "StandaloneWindows64/";
        public const string PATH_ROOT_LINUX = "StandaloneLinux64/";
        public const string PATH_ROOT_ANDROID = "Android/";
        public const string PATH_ROOT_IOS = "iOS/";
        public const string PATH_ROOT_MAC = "StandaloneOSX/";
        public const string PATH_TEMP_FOLDER = "TempFolder/";
        public static string root = PATH_ROOT_WINDOWS64;

        private void Awake()
        {
            RuntimeFramePacing.Initialize();
            gameObject.AddComponent<MDPro3.UI.ScrollSpeed>();
            SetRoot();
            instance = this;
            // Protect the first rendered frame before resource/addressable work
            // can instantiate the retired menu and animated camera background.
            UI.Client.FightStartupPresentation.Install(this);
            UI.Client.FightStartupPresentation.AddQuitIntentHandler(WantsToQuit);
            Application.quitting += UI.Client.FightStartupPresentation.BeginShutdown;
            _ = Initialize();

            //ABLoader.LoadFromFolder("Robber/fxp_somldg_Advance_S1imp_001");
            //ABLoader.LoadFromFolder("Robber/fxp_somldg_Advance_S2imp_001");
            //ABLoader.LoadFromFolder("Robber/fxp_somldg_Fusion_S1imp_001");
            //ABLoader.LoadFromFolder("Robber/fxp_somldg_Fusion_S2imp_001");
        }

        public static void SetRoot()
        {
            root = PATH_ROOT_WINDOWS64;

#if UNITY_ANDROID
            root = PATH_ROOT_ANDROID;
#elif UNITY_IOS
            root = PATH_ROOT_IOS;
#elif UNITY_STANDALONE_OSX
            root = PATH_ROOT_MAC;
#elif UNITY_STANDALONE_LINUX
            root = PATH_ROOT_LINUX;
#endif

#if UNITY_EDITOR
            root = PATH_ROOT_EDITOR + root;
#endif
        }

        public float TimeScale
        {
            get 
            { 
                return m_TimeScale;
            }
            set 
            {
                m_TimeScale = value;
                Time.timeScale = value;
            }
        }
        private float m_TimeScale = 1f;
#if UNITY_EDITOR
        public float timeScaleForEditor = 1;
#endif

        private void Update()
        {
            AI.ResponsesAiClient.Pump();
            TcpHelper.PerFrameFunction();
            AI.ResponsesDuelSession.PerFrame();
#if FIGHT_UI_PROOF
            AI.ResponsesAiProof.PerFrame();
            AI.ResponsesAiUiProof.PerFrame();
            AI.ResponsesSpectatorUiProof.PerFrame();
            Utility.DeckLibraryUiProof.PerFrame();
#endif
            foreach (Manager manager in managers) 
                manager.PerFrameFunction();
            foreach (Servant.Servant servant in servants) 
                servant.PerFrameFunction();

#if UNITY_EDITOR
            TimeScale = timeScaleForEditor;
#endif
        }

        public void UnloadUnusedAssets()
        {
            _ = UnloadUnusedAssetsAsync();
        }

        private async UniTask UnloadUnusedAssetsAsync()
        {
            await Resources.UnloadUnusedAssets();
        }

        public static bool noAccess = false;

        #endregion

        #region Tools

        public static void Debug(string text)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log(text);
#endif
        }

        public void ShiftToServant(Servant.Servant servant)
        {
            // A main-page navigation closes every previous overlay. Keeping a
            // hidden overlay here would send the global Back button to that page.
            currentSubServant = null;
            lastServant = currentServant;
            currentServant = servant;
            foreach (var ser in servants)
                if (ser != servant)
                    ser.Hide(servant.Depth);
            servant.Show(depth);
            depth = servant.Depth;
            UI.Client.FightClient.SynchronizeServantPresentation();
        }

        public void ShowSubServant(Servant.Servant servant)
        {
            if (currentSubServant == null)
            {
                servant.Show(0);
                currentSubServant = servant;
            }
            else
            {
                currentSubServant.Hide(servant.Depth);
                servant.Show(currentSubServant.Depth);
                currentSubServant = servant;
            }
        }

        public void ExitCurrentServant()
        {
            if (currentSubServant != null && !currentSubServant.showing)
                currentSubServant = null;
            if (currentSubServant != null)
                currentSubServant.OnReturn();
            else
            {
                if (currentServant == null || !currentServant.showing)
                {
                    currentServant = null;
                    foreach (var servant in servants)
                        if (servant.showing)
                        {
                            currentServant = servant;
                            break;
                        }
                    if(currentServant == null)
                        currentServant = online;
                }
                currentServant.OnReturn();
            }
        }

        public void ExitDuel()
        {
            currentSubServant.OnReturn();
            currentServant.OnReturn();
        }

        #endregion

        #region System

        private void OnDestroy()
        {
            UI.Client.FightStartupPresentation.RemoveQuitIntentHandler(WantsToQuit);
            Application.quitting -= UI.Client.FightStartupPresentation.BeginShutdown;
        }

        private bool WantsToQuit()
        {
            // System/window exits must cover the scene before any component's
            // quit cleanup can restore the legacy UI or remove the client UI.
            UI.Client.FightStartupPresentation.BeginShutdown();
            return true;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            RuntimeFramePacing.SetFocus(hasFocus);
        }

        private void OnApplicationPause(bool paused)
        {
            RuntimeFramePacing.SetPaused(paused);
        }

        private void OnApplicationQuit()
        {
            UI.Client.FightStartupPresentation.BeginShutdown();
            Running = false;
            AI.ResponsesDuelSession.Stop();
            AI.ResponsesAiClient.Shutdown();
            Config.Save();
            ClearCache();
            YgoServer.StopServer();
            FileManager.Dispose();
            try
            { TcpHelper.tcpClient.Close(); }
            catch { }
            TcpHelper.tcpClient = null;
            MyCard.CloseAthleticWatchListWebSocket();
        }

        private void ClearCache()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject cacheDir = currentActivity.Call<AndroidJavaObject>("getCacheDir");
            string cachePath = cacheDir.Call<string>("getAbsolutePath");
            Tools.ClearDirectoryRecursively(new DirectoryInfo(cachePath));
#else
            if (Directory.Exists(PATH_TEMP_FOLDER))
                Directory.Delete(PATH_TEMP_FOLDER, true);
#endif
        }

        public static void GameQuit()
        {
            UI.Client.FightStartupPresentation.BeginShutdown();
            MDPro3.UI.Client.FightClient.PrepareToQuit();
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion
    }
}
