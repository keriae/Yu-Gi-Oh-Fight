using Cysharp.Threading.Tasks;
using DG.Tweening;


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Timeline;


using YuGiOhFight.Client.Presentation.Timeline;
using static YuGiOhFight.Client.Controllers.OcgCore;


using YuGiOhFight.Client.Presentation.Extensions;


namespace YuGiOhFight.Client.Duel
{
    public class DuelBGManager
    {
        #region Parameters

        private Controllers.OcgCore Core => Runtime.Program.instance.ocgcore;
        private MDPro3.Duel.YGOSharp.Deck deck;

        private bool mate0Random = true;
        private bool mate1Random = true;
        private Tween mate0RandomCooldownTween;
        private Tween mate1RandomCooldownTween;
        private int bgPhase0 = 0;
        private int bgPhase1 = 0;
        private bool backgroundFieldInitialize = false;
        public bool loaded;
        internal bool Scene3DActive { get; private set; } = true;
        internal string ActiveSceneId { get; private set; } = DuelSceneCatalog.DefaultId;
        private int deckPresentationGeneration;
        private bool deckPresentationActive;
        private int entryLoadGeneration;
        private bool entryLoadActive;

        internal int EntryLoadGeneration => entryLoadGeneration;
        internal bool EntryLoading => entryLoadActive;
        private bool IsEntryLoadCurrent(int generation, Controllers.OcgCore core)
            => entryLoadActive && generation == entryLoadGeneration && core != null && core.showing
                && Runtime.Program.instance != null && Runtime.Program.instance.ocgcore == core
                && Runtime.Program.instance.currentServant == core;

        internal int DeckPresentationGeneration => deckPresentationGeneration;
        internal bool IsDeckPresentationCurrent(int generation)
            => deckPresentationActive && generation == deckPresentationGeneration
                && Runtime.Program.instance != null && Runtime.Program.instance.ocgcore != null
                && Runtime.Program.instance.ocgcore.showing
                && Runtime.Program.instance.currentServant == Runtime.Program.instance.ocgcore;

        private void RetireDeckPresentation()
        {
            UI.Client.FightSceneCinematic.Cancel(processor);
            ++deckPresentationGeneration;
            deckPresentationActive = false;
            ++entryLoadGeneration;
            entryLoadActive = false;
        }

        private float field0TapTime;
        private float field1TapTime;
        private float mate0TapTime;
        private float mate1TapTime;

        #endregion

        #region Objects

        public DuelMessage processor;

        public readonly List<GameObject> allGameObjects = new();
        private readonly List<GameObject> turnEndDeleteObjects = new();
        private static readonly List<int> cardEffectCodes = new();
        private static readonly List<GameObject> cardEffects = new();

        private GameObject attackLine;
        private GameObject targetLine;
        private readonly List<GameObject> targetLines = new();
        private GameObject equipLine;
        public GameObject fieldSummonRightInfo;

        public Presentation.Fields.BgEffectManager field0Manager;
        public Presentation.Fields.BgEffectManager field1Manager;
        private Presentation.Fields.BgEffectManager grave0Manager;
        private Presentation.Fields.BgEffectManager grave1Manager;
        private readonly List<GraveBehaviour> graves = new();
        private Presentation.Fields.BgEffectManager stand0Manager;
        private Presentation.Fields.BgEffectManager stand1Manager;
        private Presentation.Mate mate0;
        private Presentation.Mate mate1;
        private PremiumMateState premiumMate0;
        private PremiumMateState premiumMate1;

        private GameObject phaseButton;
        private UI.TimerHandler timerHandler;
        private PlayableGuide playableGuide;
        public Presentation.Elements.ElementObjectManager myDeck;
        public Presentation.Elements.ElementObjectManager myExtra;
        public Presentation.Elements.ElementObjectManager opDeck;
        public Presentation.Elements.ElementObjectManager opExtra;
        public List<UI.PlaceSelector> places = new();
        private DuelFinalBlow duelFinalBlow;
        private UI.Client.FightSceneCombatFeedback finalBlowFeedback;
        private DuelSceneEnvironment sceneEnvironment;
        private DuelSceneFieldProps fieldProps;

        #endregion

        private sealed class PremiumMateState
        {
            public int Side;
            public PremiumMateRule Rule;
            public PremiumMateSwapEffect SwapEffect;
            public Transform Anchor;
            public readonly Dictionary<int, Presentation.Mate> Forms = new();
            public int ActiveMateId;
            public bool IsTransitioning;
            public int QueuedMateId;
            public bool IsPermanentSub;
            public bool InBattlePhaseSub;
            public bool PendingDirectAttack;
            public IReadOnlyList<string> OverrideTriggerPriority;
            public int PendingTurnSwapToken;
            public int RayeEngageMateId;
            public int RayeBattleMateId;
        }

        private readonly struct SwapEffectLabelPlaybackResult
        {
            public bool Consumed { get; }

            public SwapEffectLabelPlaybackResult(bool consumed)
            {
                Consumed = consumed;
            }

            public static SwapEffectLabelPlaybackResult None => new(false);
        }

        #region Public

        public async UniTask LoadAssetsAsync()
        {
            RetireDeckPresentation();
            Controllers.OcgCore loadCore = Core;
            int loadGeneration = entryLoadGeneration;
            entryLoadActive = true;
            bool CurrentLoad() => IsEntryLoadCurrent(loadGeneration, loadCore);
            bool AcceptLoadedObject(GameObject asset, bool ownedRoot = false)
            {
                if (!CurrentLoad())
                {
                    if (asset != null) { asset.SetActive(false); UnityEngine.Object.Destroy(asset); }
                    return false;
                }
                if (ownedRoot && asset != null) allGameObjects.Add(asset);
                return true;
            }
            if (!CurrentLoad()) { entryLoadActive = false; return; }
            // One preference snapshot per entry; settings changed during play
            // apply to the next duel without rebuilding cards or presentation.
            bool scene3DEnabled = MDPro3.Config.GetBool(DuelSceneCatalog.EnabledKey, true);
            string requestedScene = MDPro3.Config.Get(DuelSceneCatalog.SceneKey, DuelSceneCatalog.DefaultId);
            DuelSceneDefinition selectedScene = DuelSceneCatalog.Resolve(requestedScene);
            if (Services.ReplayVideoExport.Busy)
                Debug.Log("[ReplayVideoExport] requested scene=" + requestedScene + ", selected scene=" + selectedScene.Id + ", 3D=" + scene3DEnabled);
            bool integratedTheme = scene3DEnabled && DuelSceneCatalog.IsIntegratedTheme(selectedScene.Builder);
            Scene3DActive = scene3DEnabled;
            ActiveSceneId = selectedScene.Id;
            loaded = false;
            ResetPremiumMateStates();

            deck = null;
            var deckName = MDPro3.Config.GetConfigDeckName();
            if (condition == Condition.Duel
                && !inPuzzle
                && File.Exists(Runtime.Program.PATH_DECK + deckName + Runtime.Program.EXPANSION_YDK))
                deck = new MDPro3.Duel.YGOSharp.Deck(Runtime.Program.PATH_DECK + deckName + Runtime.Program.EXPANSION_YDK);

            var hasSide0Appearance = Controllers.RoomServant.TryGetOnlineAppearanceForSide(0, out MDPro3.OnlineAppearanceData side0Appearance) && condition != Condition.Replay;
            var hasSide1Appearance = Controllers.RoomServant.TryGetOnlineAppearanceForSide(1, out MDPro3.OnlineAppearanceData side1Appearance) && condition != Condition.Replay;
            var overrideDeckAppearance = MDPro3.Config.GetBool("OverrideDeckAppearance", false);

            // Resource loading uses a plain cover, with no entering-duel timeline.
            Runtime.UIManager.CancelDuelTransition();
            Runtime.CameraManager.ResetDuelBlack();
            Runtime.UIManager.ShowBlackBack(1f, 0f);
            await UniTask.WaitUntil(() => !CurrentLoad() || Controllers.Appearance.loaded);
            if (!CurrentLoad()) return;
            if(inPuzzle || condition != Condition.Duel)
            {
                await Runtime.Program.instance.appearance.LoadSettingAssets();
                if (!CurrentLoad()) return;
            }
            Services.ReplayAppearance.Capture(deck);
            await Runtime.ABLoader.CacheMasterDuelBundles();
            if (!CurrentLoad()) return;
            loadCore.LoadDuelButton();
            await loadCore.GetUI<UI.ServantUI.OcgCoreUI>().LoadExtraUI();
            if (!CurrentLoad()) return;

            Runtime.CameraManager.ShiftTo3D();
            Runtime.UIManager.HideExitButton(0);
            Runtime.UIManager.HideLine(0);
            Runtime.AudioManager.StopBGM();

            if (attackLine == null)
            {
                attackLine = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_atk_select_arrow_001");
                attackLine.SetActive(false);
                await UniTask.Yield();
                if (!CurrentLoad()) return;
            }
            if (targetLine == null)
            {
                targetLine = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_target_arrow_001");
                targetLine.SetActive(false);
                await UniTask.Yield();
                if (!CurrentLoad()) return;
            }
            if (equipLine == null)
            {
                equipLine = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_equip_arrow_001");
                equipLine.SetActive(false);
                await UniTask.Yield();
                if (!CurrentLoad()) return;
            }
            if (fieldSummonRightInfo == null)
            {
                var handle = Addressables.InstantiateAsync("Prefab/FieldSummonRightInfo.prefab");
                await handle;
                if (!CurrentLoad())
                {
                    if (handle.Result != null) { handle.Result.SetActive(false); Addressables.ReleaseInstance(handle.Result); }
                    return;
                }
                fieldSummonRightInfo = handle.Result;
                fieldSummonRightInfo.SetActive(false);
                fieldSummonRightInfo.transform.SetParent(Runtime.Program.instance.container_3D);
            }

            #region Field

            var path = Runtime.Program.items.GetAssetPath(
                Services.ReplayAppearance.Get(condition.ToString() + "Field0",
                Runtime.Program.items.mats[0].id.ToString()), Presentation.Assets.Items.ItemType.Mat);
            if (condition == Condition.Watch && hasSide0Appearance)
                path = Runtime.Program.items.GetAssetPath(side0Appearance.Field.ToString(), Presentation.Assets.Items.ItemType.Mat);
            else if (deck != null && !overrideDeckAppearance)
                path = Runtime.Program.items.GetAssetPath(Services.ReplayAppearance.ResolveDeck("Field", deck.Field).ToString(), Presentation.Assets.Items.ItemType.Mat);
            if (integratedTheme)
                path = Runtime.Program.items.GetAssetPath(Runtime.Program.items.mats[0].id.ToString(), Presentation.Assets.Items.ItemType.Mat);
            path = "MasterDuel/" + path;
            var field0 = await Runtime.ABLoader.LoadFromFileAsync(path + "_near", false, true);
            if (!AcceptLoadedObject(field0, true)) return;
            field0.transform.SetParent(Runtime.Program.instance.container_3D, false);
            field0Manager = field0.GetComponent<Presentation.Fields.BgEffectManager>();

            var field1Config = Services.ReplayAppearance.Get(condition.ToString() + "Field1",
                Runtime.Program.items.mats[0].id.ToString());
            if (hasSide1Appearance)
                field1Config = side1Appearance.Field.ToString();
            if (integratedTheme) field1Config = Runtime.Program.items.mats[0].id.ToString();
            var field1 = await Runtime.ABLoader.LoadFromFileAsync("MasterDuel/" +
                Runtime.Program.items.GetAssetPath(field1Config, Presentation.Assets.Items.ItemType.Mat, 1) + "_far"
                , false, true);
            if (!AcceptLoadedObject(field1, true)) return;
            field1.transform.SetParent(Runtime.Program.instance.container_3D, false);
            field1Manager = field1.GetComponent<Presentation.Fields.BgEffectManager>();

            var collider = field0.AddComponent<BoxCollider>();
            collider.center = new Vector3(38, 5, -10);
            collider.size = new Vector3(10, 10, 10);
            collider = field1.AddComponent<BoxCollider>();
            collider.center = new Vector3(-38, 5, 10);
            collider.size = new Vector3(10, 10, 10);

            Transform pos_Grave_near = field0.transform.GetChildByName("POS_Grave_near");
            Transform pos_Grave_far = field1.transform.GetChildByName("POS_Grave_far");
            Transform pos_AvatarStand_near = field0.transform.GetChildByName("POS_AvatarStand_near");
            Transform pos_AvatarStand_far = field1.transform.GetChildByName("POS_AvatarStand_far");
            Transform pos_Avatar_near = field0.transform.GetChildByName("POS_Avatar_near");
            Transform pos_Avatar_far = field1.transform.GetChildByName("POS_Avatar_far");

            #endregion

            #region Grave

            path = Runtime.Program.items.GetAssetPath(
                Services.ReplayAppearance.Get(condition.ToString() + "Grave0",
                Runtime.Program.items.graves[0].id.ToString()), Presentation.Assets.Items.ItemType.Grave);
            if (condition == Condition.Watch && hasSide0Appearance)
                path = Runtime.Program.items.GetAssetPath(side0Appearance.Grave.ToString(), Presentation.Assets.Items.ItemType.Grave);
            else if (deck != null && !overrideDeckAppearance)
                path = Runtime.Program.items.GetAssetPath(Services.ReplayAppearance.ResolveDeck("Grave", deck.Grave).ToString(), Presentation.Assets.Items.ItemType.Grave);
            if (integratedTheme)
                path = Runtime.Program.items.GetAssetPath(Runtime.Program.items.graves[0].id.ToString(), Presentation.Assets.Items.ItemType.Grave);
            path = "MasterDuel/" + path;

            var grave0 = await Runtime.ABLoader.LoadFromFileAsync(path + "_near", false, true);
            if (!AcceptLoadedObject(grave0)) return;
            grave0.transform.SetParent(pos_Grave_near, false);
            grave0Manager = grave0.GetComponent<Presentation.Fields.BgEffectManager>();
            InitializeGraveMaterialParameters(grave0);

            var grave1Config = Services.ReplayAppearance.Get(condition.ToString() + "Grave1",
                Runtime.Program.items.graves[0].id.ToString());
            if (hasSide1Appearance)
                grave1Config = side1Appearance.Grave.ToString();
            if (integratedTheme) grave1Config = Runtime.Program.items.graves[0].id.ToString();
            var grave1 = await Runtime.ABLoader.LoadFromFileAsync("MasterDuel/" +
                Runtime.Program.items.GetAssetPath(grave1Config, Presentation.Assets.Items.ItemType.Grave, 1) + "_far"
                , false, true);
            if (!AcceptLoadedObject(grave1)) return;
            grave1.transform.SetParent(pos_Grave_far, false);
            grave1Manager = grave1.GetComponent<Presentation.Fields.BgEffectManager>();
            InitializeGraveMaterialParameters(grave1);

            Utility.Tools.PlayAnimation(grave0.transform, "StartToPhase1");
            Utility.Tools.PlayAnimation(grave1.transform, "StartToPhase1");

            graves.Clear();
            GraveBehaviour g0 = grave0.AddComponent<GraveBehaviour>();
            g0.controller = 0;
            graves.Add(g0);
            GraveBehaviour g1 = grave1.AddComponent<GraveBehaviour>();
            g1.controller = 1;
            graves.Add(g1);

            #endregion

            #region Stand

            GameObject stand0Root = null, stand1Root = null;
            var standConfig = Services.ReplayAppearance.Get(condition.ToString() + "Stand0", Runtime.Program.items.stands[0].id.ToString());
            if (condition == Condition.Watch && hasSide0Appearance)
                standConfig = side0Appearance.Stand.ToString();
            if (!integratedTheme && (standConfig != Presentation.Assets.Items.CODE_NONE.ToString() || deck != null))
            {
                path = Runtime.Program.items.GetAssetPath(standConfig, Presentation.Assets.Items.ItemType.Stand);
                if (deck != null && !overrideDeckAppearance)
                    path = Runtime.Program.items.GetAssetPath(Services.ReplayAppearance.ResolveDeck("Stand", deck.Stand).ToString(), Presentation.Assets.Items.ItemType.Stand);
                path = "MasterDuel/" + path;
                var stand0 = await Runtime.ABLoader.LoadFromFileAsync(path + "_near", false, true);
                if (!AcceptLoadedObject(stand0)) return;
                stand0.transform.SetParent(pos_AvatarStand_near, false);
                stand0Root = stand0;

                pos_Avatar_near = stand0.transform.GetChildByName("POS_Avatar_near");
                Utility.Tools.PlayAnimation(stand0.transform, "StartToPhase1");
                stand0Manager = stand0.GetComponent<Presentation.Fields.BgEffectManager>();
            }

            standConfig = Services.ReplayAppearance.Get(condition.ToString() + "Stand1", Runtime.Program.items.stands[0].id.ToString());
            if (hasSide1Appearance)
                standConfig = side1Appearance.Stand.ToString();
            if (!integratedTheme && standConfig != Presentation.Assets.Items.CODE_NONE.ToString())
            {
                var stand1 = await Runtime.ABLoader.LoadFromFileAsync("MasterDuel/" +
                Runtime.Program.items.GetAssetPath(standConfig, Presentation.Assets.Items.ItemType.Stand, 1) + "_far"
                , false, true);
                if (!AcceptLoadedObject(stand1)) return;
                stand1.transform.SetParent(pos_AvatarStand_far, false);
                stand1Root = stand1;

                pos_Avatar_far = stand1.transform.GetChildByName("POS_Avatar_far");
                Utility.Tools.PlayAnimation(stand1.transform, "StartToPhase1");
                stand1Manager = stand1.GetComponent<Presentation.Fields.BgEffectManager>();
            }

            #endregion

            #region Mate

            var mateConfig = Services.ReplayAppearance.Get(condition.ToString() + "Mate0", Runtime.Program.items.mates[0].id.ToString());
            if (condition == Condition.Watch && hasSide0Appearance)
                mateConfig = side0Appearance.Mate.ToString();
            int mateCode = int.Parse(mateConfig);
            if (deck != null && !overrideDeckAppearance)
                mateCode = Services.ReplayAppearance.ResolveDeck("Mate", deck.Mate);
            await LoadMateForSideAsync(0, mateCode, pos_Avatar_near, CurrentLoad);
            if (!CurrentLoad()) return;

            mateConfig = Services.ReplayAppearance.Get(condition.ToString() + "Mate1", Runtime.Program.items.mates[0].id.ToString());
            if (hasSide1Appearance)
                mateConfig = side1Appearance.Mate.ToString();
            await LoadMateForSideAsync(1, int.Parse(mateConfig), pos_Avatar_far, CurrentLoad);
            if (!CurrentLoad()) return;

            #endregion

            #region CelestialSphere

            var matBack = Runtime.ABLoader.LoadMasterDuelGameObject("CelestialSphere_c001");
            matBack.transform.SetParent(Runtime.Program.instance.container_3D, false);
            matBack.transform.localScale = Vector3.one * 2;

            allGameObjects.Add(matBack);

            if (!scene3DEnabled || selectedScene.Builder != DuelSceneCatalog.DefaultId)
            {
                DuelSceneEnvironment entryEnvironment = null;
                try
                {
                    entryEnvironment = DuelSceneEnvironment.Create(Runtime.Program.instance.container_3D, field0, field1, matBack, scene3DEnabled,
                        new DuelSceneBindings(pos_Avatar_near, pos_Avatar_far, stand0Root, stand1Root, grave0Manager, grave1Manager));
                    sceneEnvironment = entryEnvironment;
                    allGameObjects.Add(entryEnvironment.gameObject);
                    bool built = await entryEnvironment.BuildAsync(scene3DEnabled, selectedScene, CurrentLoad);
                    if (!built && sceneEnvironment == entryEnvironment)
                    {
                        sceneEnvironment = null;
                        if (CurrentLoad()) { Scene3DActive = true; ActiveSceneId = DuelSceneCatalog.DefaultId; }
                    }
                }
                catch (Exception error)
                {
                    if (entryEnvironment != null) entryEnvironment.Retire();
                    if (sceneEnvironment == entryEnvironment) sceneEnvironment = null;
                    if (CurrentLoad())
                    {
                        Scene3DActive = true; ActiveSceneId = DuelSceneCatalog.DefaultId;
                        Debug.LogWarning("[FightClient] duel scene initialization failed (" + error.GetType().Name + "); using classic environment.");
                    }
                }
                if (!CurrentLoad()) return;
            }

            #endregion

            #region PhaseButton

            GameObject loadedPhaseButton;
            if (MatIsSpecial(field1.name))
            {
                loadedPhaseButton = await Runtime.ABLoader.LoadFromFileAsync("MasterDuel/BG/Timer/PhaseButton_013", true, true);
                if (!AcceptLoadedObject(loadedPhaseButton, true)) return;
                loadedPhaseButton.GetComponent<Animator>().SetTrigger("Start");
                Utility.Tools.PlayAnimation(loadedPhaseButton.transform, "StartToPhase1");
            }
            else
            {
                loadedPhaseButton = Runtime.ABLoader.LoadMasterDuelGameObject("PhaseButton_c001");
                allGameObjects.Add(loadedPhaseButton);
                await UniTask.Yield();
                if (!CurrentLoad()) return;
            }

            phaseButton = loadedPhaseButton;
            phaseButton.transform.SetParent(Runtime.Program.instance.container_3D, false);
            phaseButton.AddComponent<UI.PhaseButtonHandler>();

            #endregion

            #region Timer

            if (condition == Condition.Duel && !inputMode)
            {
                GameObject timer;
                if (MatIsSpecial(field1.name))
                {
                    timer = await Runtime.ABLoader.LoadFromFileAsync("MasterDuel/BG/Timer/Timer_013", true, true);
                    if (!AcceptLoadedObject(timer, true)) return;
                }
                else
                {
                    timer = Runtime.ABLoader.LoadMasterDuelGameObject("Timer_c001");
                    allGameObjects.Add(timer);
                    await UniTask.Yield();
                    if (!CurrentLoad()) return;
                }

                timerHandler = timer.AddComponent<UI.TimerHandler>();
                timer.transform.SetParent(Runtime.Program.instance.container_3D, false);
                timerHandler.SetTimeLimit(timeLimit);
                timerHandler.SetTime(isFirst ? 0 : 1, timeLimit);
                timerHandler.Pause();
            }

            #endregion

            if (sceneEnvironment != null && sceneEnvironment.HasOwnTheme)
            {
                fieldProps = DuelSceneFieldProps.Create(Runtime.Program.instance.container_3D, ActiveSceneId,
                    phaseButton, timerHandler, grave0Manager, grave1Manager, sceneEnvironment.Backdrop);
                if (fieldProps != null) allGameObjects.Add(fieldProps.gameObject);
                else
                {
                    sceneEnvironment.Retire(); sceneEnvironment = null;
                    Scene3DActive = true; ActiveSceneId = DuelSceneCatalog.DefaultId;
                }
            }

            #region Playable Guide

            PlayableGuide entryGuide = Create<PlayableGuide>();
            playableGuide = entryGuide;
            entryGuide.Load(field0.name, field1.name);
            await UniTask.WaitUntil(() => !CurrentLoad() || entryGuide == null || entryGuide.loaded);
            if (!CurrentLoad()) return;

            if (MDPro3.Utility.DeviceInfo.OnAndroid())
                playableGuide.SetHeight(0.6f);
            sceneEnvironment?.SuppressAuthoredEffect(entryGuide.gameObject);

            #endregion

            #region Deck Model

            var deckMat = Controllers.Appearance.duelProtector0;
            if (deck != null && !overrideDeckAppearance)
            {
                deckMat = await Runtime.ABLoader.LoadProtectorMaterial(Services.ReplayAppearance.ResolveDeck("Protector", deck.Protector).ToString(), Application.exitCancellationToken);
                if (!CurrentLoad()) return;
            }

            if (condition == Condition.Duel)
                myProtector = deckMat;
            else if (condition == Condition.Watch)
            {
                myProtector = Controllers.Appearance.watchProtector0;
                if (hasSide0Appearance)
                {
                    var mat = await Runtime.ABLoader.LoadProtectorMaterial(side0Appearance.Protector.ToString(), Application.exitCancellationToken);
                    if (!CurrentLoad()) return;
                    if (mat != null)
                        myProtector = mat;
                }
            }
            else if (condition == Condition.Replay)
                myProtector = Controllers.Appearance.replayProtector0;

            if (condition == Condition.Duel)
            {
                opProtector = Controllers.Appearance.duelProtector1;
                if (hasSide1Appearance)
                {
                    var mat = await Runtime.ABLoader.LoadProtectorMaterial(side1Appearance.Protector.ToString(), Application.exitCancellationToken);
                    if (!CurrentLoad()) return;
                    if (mat != null)
                        opProtector = mat;
                }
            }
            else if (condition == Condition.Watch)
            {
                opProtector = Controllers.Appearance.watchProtector1;
                if (hasSide1Appearance)
                {
                    var mat = await Runtime.ABLoader.LoadProtectorMaterial(side1Appearance.Protector.ToString(), Application.exitCancellationToken);
                    if (!CurrentLoad()) return;
                    if (mat != null)
                        opProtector = mat;
                }
            }
            else if (condition == Condition.Replay)
                opProtector = Controllers.Appearance.replayProtector1;

            var deckModel = Runtime.ABLoader.LoadMasterDuelGameObject("DuelDeckAppearance");

            myDeck = deckModel.GetComponent<Presentation.Elements.ElementObjectManager>();
            InitializeDeckModel(myDeck, 0, MDPro3.Duel.YGOSharp.CardLocation.Deck);
            myExtra = UnityEngine.Object.Instantiate(deckModel).GetComponent<Presentation.Elements.ElementObjectManager>();
            InitializeDeckModel(myExtra, 0, MDPro3.Duel.YGOSharp.CardLocation.Extra);
            opDeck = UnityEngine.Object.Instantiate(deckModel).GetComponent<Presentation.Elements.ElementObjectManager>();
            InitializeDeckModel(opDeck, 1, MDPro3.Duel.YGOSharp.CardLocation.Deck);
            opExtra = UnityEngine.Object.Instantiate(deckModel).GetComponent<Presentation.Elements.ElementObjectManager>();
            InitializeDeckModel(opExtra, 1, MDPro3.Duel.YGOSharp.CardLocation.Extra);

            // The entry load owns a separate generation. Expose the deck
            // presentation only once its four native models are installed.
            ++deckPresentationGeneration;
            deckPresentationActive = true;

            #endregion

            #region Places

            places.Clear();
            for (uint c = 0; c < 2; c++)
            {
                GPS gps = new()
                {
                    controller = c,
                    location = (uint)MDPro3.Duel.YGOSharp.CardLocation.Deck
                };
                CreatePlaceSelector(gps);

                gps = new()
                {
                    controller = c,
                    location = (uint)MDPro3.Duel.YGOSharp.CardLocation.Extra
                };
                CreatePlaceSelector(gps);

                for (uint s = 0; s < (c == 0 ? 7 : 5); s++)
                {
                    gps = new()
                    {
                        controller = c,
                        location = (uint)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone,
                        sequence = s
                    };
                    CreatePlaceSelector(gps);
                }
                for (uint s = 0; s < 6; s++)
                {
                    gps = new()
                    {
                        controller = c,
                        location = (uint)MDPro3.Duel.YGOSharp.CardLocation.SpellZone,
                        sequence = s
                    };
                    CreatePlaceSelector(gps);
                }
            }

            #endregion

            #region Quit Load

            await processor.PreloadPlayerNames();
            if (!CurrentLoad()) return;
            if (Core.NeedVoice())
            {
                Core.GetUI<UI.ServantUI.OcgCoreUI>().CG.alpha = 1;
                Core.GetUI<UI.ServantUI.OcgCoreUI>().CG.blocksRaycasts = true;
            }
            Core.GetUI<UI.ServantUI.OcgCoreUI>().Buttons.SetActive(false);

            Runtime.UIManager.ShowFPSLeft();
            Runtime.UIManager.HideBlackBack(0f);

            backgroundFieldInitialize = false;
            BackgroundFieldInitialize();

            if (condition == Condition.Duel && MDPro3.Config.GetBool("DuelAutoAcc", false)
                || condition == Condition.Watch && MDPro3.Config.GetBool("WatchAutoAcc", false)
                || condition == Condition.Replay && MDPro3.Config.GetBool("ReplayAutoAcc", false))
                Core.GetUI<UI.ServantUI.OcgCoreUI>().OnAcc();

            #endregion

            InitializeFieldMaterialParameters(field0);
            InitializeFieldMaterialParameters(field1);
            if (UsesThemeFixtures && condition != Condition.Replay)
                UI.Client.FightSceneCinematic.Prepare(processor, ActiveSceneId);
            loaded = true;
            entryLoadActive = false;
        }

        private static void InitializeFieldMaterialParameters(GameObject field)
        {
            if (field == null || SystemInfo.graphicsDeviceType != GraphicsDeviceType.Metal) return;
            int shadowDistance = Shader.PropertyToID("_ShadowSoftDistance");
            foreach (var renderer in field.GetComponentsInChildren<Renderer>(true))
            {
                // Existing artist or animation overrides already use this
                // submission path; leave their renderer and slot blocks intact.
                if (renderer.HasPropertyBlock()) continue;
                var materials = renderer.sharedMaterials;
                for (int index = 0; index < materials.Length; index++)
                {
                    var material = materials[index];
                    if (material == null || material.shader == null
                        || !material.shader.name.StartsWith("Shader Graphs/UnlitRecieveShadow", StringComparison.Ordinal)
                        || !material.HasProperty(shadowDistance)) continue;
                    // Older bundled field graphs can enter Metal with stale
                    // per-material constants. Submit the authored value once
                    // through the renderer without changing the shared asset.
                    var block = new MaterialPropertyBlock();
                    block.SetFloat(shadowDistance, material.GetFloat(shadowDistance));
                    renderer.SetPropertyBlock(block, index);
                }
            }
        }

        private static void InitializeGraveMaterialParameters(GameObject grave)
        {
            if (grave == null || SystemInfo.graphicsDeviceType != GraphicsDeviceType.Metal) return;
            int cardIntensity = Shader.PropertyToID("_IntensityGraveCardExist");
            foreach (var renderer in grave.GetComponentsInChildren<Renderer>(true))
            {
                // A pre-existing block already uses per-object submission.
                if (renderer.HasPropertyBlock()) continue;
                var materials = renderer.sharedMaterials;
                for (int index = 0; index < materials.Length; index++)
                {
                    var material = materials[index];
                    if (material == null || material.shader == null
                        || material.shader.name != "Shader Graphs/GraveCommon01"
                        || !material.HasProperty(cardIntensity)) continue;
                    // This bundled graph can lose its per-object light data in
                    // Metal SRP batches, leaving the stone black. Submitting one
                    // static authored value preserves its texture, animation,
                    // and count-driven glow while using the per-object path.
                    var block = new MaterialPropertyBlock();
                    block.SetFloat(cardIntensity, material.GetFloat(cardIntensity));
                    renderer.SetPropertyBlock(block, index);
                }
            }
        }

        public async UniTask ExitDuelAsync()
        {
            RetireDeckPresentation();
            int exitGeneration = deckPresentationGeneration;
            Controllers.OcgCore exitCore = Core;
            UI.ServantUI.OcgCoreUI exitUI = exitCore.GetUI<UI.ServantUI.OcgCoreUI>();
            loaded = false;
            // Retire the departed protocol before the preparation page can
            // start another duel and append its packets to the shared queues.
            NoMoreWait = true;
            packages.Clear();
            allPackages.Clear();
            pause = false;
            nextMoveAction = null;
            ClearResponse();
            Runtime.CameraManager.BlackOut(0f, 0.3f);

            Runtime.UIManager.UIBlackIn(Core.TransitionTime);
            if (exitUI != null) exitUI.CloseHint();
            HideAttackLine();
            HideDuelFinalBlowText();
            await UniTask.WaitForSeconds(Core.TransitionTime);
            if (exitGeneration != deckPresentationGeneration || Runtime.Program.instance == null
                || exitCore == null || Runtime.Program.instance.ocgcore != exitCore) return;
            if (exitCore.servantUI != null) exitCore.servantUI.ShutDown();

            Runtime.AudioManager.ResetSESource();
            mycardDuel = false;
            Core.CloseCharaFace();
            Dispose();
            exitGeneration = deckPresentationGeneration;

            foreach (GameCard card in cards)
                card.Dispose();
            cards.Clear();
            Core.cachedCharaFaces.Clear();
            Runtime.CameraManager.ShiftTo2D();
            if (exitUI != null)
            {
                if (exitUI.Buttons != null) exitUI.Buttons.SetActive(false);
                if (exitUI.DuelLog != null) exitUI.DuelLog.ClearLog();
            }
            Runtime.Program.instance.ui_.chatPanel.Hide();
            await UniTask.WaitForSeconds(0.3f);
            if (exitGeneration != deckPresentationGeneration || Runtime.Program.instance == null
                || exitCore == null || Runtime.Program.instance.ocgcore != exitCore) return;
            Runtime.UIManager.UIBlackOut(Core.TransitionTime);
            await UniTask.WaitForSeconds(Core.TransitionTime);
            if (exitGeneration != deckPresentationGeneration || Runtime.Program.instance == null
                || exitCore == null || Runtime.Program.instance.ocgcore != exitCore) return;
            Runtime.UIManager.ShowFPSRight();
            Runtime.AudioManager.PlayBGM(Runtime.AudioManager.BGM_MENU_MAIN);
        }

        private void InitializeDeckModel(Presentation.Elements.ElementObjectManager deckManager, int player, MDPro3.Duel.YGOSharp.CardLocation location)
        {
            deckManager.transform.SetParent(player == 0 ? field0Manager.transform : field1Manager.transform, false);
            deckManager.transform.localPosition = _positionMap[(player, location)];
            deckManager.transform.localEulerAngles = _angleMap[(player, location)];
            allGameObjects.Add(deckManager.gameObject);

            var mat = player == 0 ? myProtector : opProtector;
            deckManager.GetNestedElement<MeshRenderer>("DummyDeck/DummyCardModel_back").material = mat;
            deckManager.GetNestedElement<MeshRenderer>("CardShuffleTop/CardModel01_back").material = mat;
            deckManager.GetNestedElement<MeshRenderer>("CardShuffleTop/CardModel02_back").material = mat;
            deckManager.GetNestedElement<MeshRenderer>("CardShuffleTop/CardModel03_back").material = mat;
            deckManager.GetNestedElement<MeshRenderer>("CardShuffleTop/CardModel04_back").material = mat;

            deckManager.gameObject.SetActive(false);
        }

        private static readonly Dictionary<(int, MDPro3.Duel.YGOSharp.CardLocation), Vector3> _positionMap = new()
        {
            [(0, MDPro3.Duel.YGOSharp.CardLocation.Deck)] = new(26.6f, 1.5f, -23.5f),
            [(0, MDPro3.Duel.YGOSharp.CardLocation.Extra)] = new(-26.6f, 1.5f, -23.5f),
            [(1, MDPro3.Duel.YGOSharp.CardLocation.Deck)] = new(-26.6f, 1.5f, 23.5f),
            [(1, MDPro3.Duel.YGOSharp.CardLocation.Extra)] = new(26.6f, 1.5f, 23.5f)
        };

        private static readonly Dictionary<(int, MDPro3.Duel.YGOSharp.CardLocation), Vector3> _angleMap = new()
        {
            [(0, MDPro3.Duel.YGOSharp.CardLocation.Deck)] = new(0f, -20f, 0f),
            [(0, MDPro3.Duel.YGOSharp.CardLocation.Extra)] = new(0f, 20f, 0f),
            [(1, MDPro3.Duel.YGOSharp.CardLocation.Deck)] = new(0f, 160f, 0f),
            [(1, MDPro3.Duel.YGOSharp.CardLocation.Extra)] = new(0f, -160f, 0f)
        };

        public async UniTask ShowDecksAsync()
        {
            if (!TryCaptureDeckPresentation(out DeckPresentation presentation)) return;
            if (!await ShowAllDeckModelsAsync(presentation) || !IsDeckPresentationCurrent(presentation)) return;

            Core.GetUI<UI.ServantUI.OcgCoreUI>().CG.alpha = 1;
            Core.GetUI<UI.ServantUI.OcgCoreUI>().CG.blocksRaycasts = true;
            Core.GetUI<UI.ServantUI.OcgCoreUI>().Buttons.SetActive(true);
            Runtime.AudioManager.PlayBgmNormal(MDPro3.Config.GetBool("BGMbyMySide", true) ? field0Manager.name : field1Manager.name);
        }

        public void BackgroundFieldInitialize()
        {
            if (field0Manager == null || field1Manager == null)
                return;
            if (bgPhase0 == 1 && bgPhase1 == 1) return;

            if (backgroundFieldInitialize)
            {
                field0Manager.gameObject.SetActive(false);
                field1Manager.gameObject.SetActive(false);
                field0Manager.gameObject.SetActive(true);
                field1Manager.gameObject.SetActive(true);
            }

            field0Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.StartToPhase1);
            grave0Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.StartToPhase1);
            bgPhase0 = 1;
            field1Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.StartToPhase1);
            grave1Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.StartToPhase1);
            bgPhase1 = 1;
            if (sceneEnvironment != null && sceneEnvironment.HasOwnTheme) RefreshGravesState();

            InitializeMateEntry(0);
            InitializeMateEntry(1);

            if (timerHandler != null)
                timerHandler.DuelStart();

            RestartMateRandomCooldown(0);
            RestartMateRandomCooldown(1);

            foreach (UI.PlaceSelector p in places)
                p.Reset();

            backgroundFieldInitialize = true;
            sceneEnvironment?.ApplyVisibility();
        }

        public void OnNewTurn(bool myTurn, int turn)
        {
            ApplyPremiumNewTurn(GetPremiumMateState(0), myTurn, turn);
            ApplyPremiumNewTurn(GetPremiumMateState(1), !myTurn, turn);
        }

        public void OnNewPhase(int turnPlayer, MDPro3.Duel.YGOSharp.DuelPhase phase)
        {
            ApplyPremiumPhase(GetPremiumMateState(0), turnPlayer == 0, phase);
            ApplyPremiumPhase(GetPremiumMateState(1), turnPlayer == 1, phase);
        }

        public void OnSpecialSummonFromExtra(int ownerPlayer)
        {
            PremiumMateState state = GetPremiumMateState(ownerPlayer);
            if (state == null)
                return;

            if (state.Rule.Behavior == PremiumMateBehavior.GaiaExtraDeckPermanent
                || state.Rule.Behavior == PremiumMateBehavior.FiendsmithExtraDeckOrEquipPermanent)
            {
                if (state.IsPermanentSub)
                    return;
                state.IsPermanentSub = true;
                RequestPremiumMateForm(state, state.Rule.SubId);
            }
        }

        public void OnEquipApplied(int ownerPlayer)
        {
            PremiumMateState state = GetPremiumMateState(ownerPlayer);
            if (state == null)
                return;
            if (state.Rule.Behavior != PremiumMateBehavior.FiendsmithExtraDeckOrEquipPermanent)
                return;
            if (state.IsPermanentSub)
                return;

            state.IsPermanentSub = true;
            RequestPremiumMateForm(state, state.Rule.SubId);
        }

        public void OnLifePointsChanged(int player, int lifePoint)
        {
            PremiumMateState state = GetPremiumMateState(player);
            if (state == null)
                return;
            if (state.Rule.Behavior != PremiumMateBehavior.ShuraigLpThreshold)
                return;
            if (state.IsPermanentSub)
                return;
            if (lifePoint > state.Rule.LpThreshold)
                return;

            state.IsPermanentSub = true;
            RequestPremiumMateForm(state, state.Rule.SubId);
        }

        public void OnDirectAttack(int attackerPlayer)
        {
            PremiumMateState state = GetPremiumMateState(attackerPlayer);
            if (state == null)
                return;
            if (state.Rule.Behavior != PremiumMateBehavior.RayeBattlePhaseAndDirectAttack)
                return;
            var battleMateId = GetRayeBattleMateId(state);
            if (state.ActiveMateId != battleMateId)
                return;

            state.PendingDirectAttack = true;
        }

        public void OnPlayerDamaged(int defenderPlayer, int amount)
        {
            if (amount <= 0)
                return;

            PremiumMateState state0 = GetPremiumMateState(0);
            PremiumMateState state1 = GetPremiumMateState(1);
            TryResolveRayeDirectAttack(state0, defenderPlayer);
            TryResolveRayeDirectAttack(state1, defenderPlayer);
        }

        private void TryResolveRayeDirectAttack(PremiumMateState state, int defenderPlayer)
        {
            if (state == null)
                return;
            if (state.Rule.Behavior != PremiumMateBehavior.RayeBattlePhaseAndDirectAttack)
                return;
            if (!state.PendingDirectAttack)
                return;
            if (state.Side == defenderPlayer)
                return;

            state.PendingDirectAttack = false;
            state.InBattlePhaseSub = false;
            RequestPremiumMateForm(state, GetRayeEngageMateId(state));
        }

        private void ApplyPremiumNewTurn(PremiumMateState state, bool ownerTurn, int turn)
        {
            if (state == null)
                return;
            _ = ownerTurn;

            state.PendingDirectAttack = false;

            switch (state.Rule.Behavior)
            {
                case PremiumMateBehavior.LaundryBattlePhaseRoundTrip:
                    state.InBattlePhaseSub = false;
                    if (!state.IsPermanentSub)
                        RequestPremiumMateForm(state, state.Rule.BaseId);
                    break;
                case PremiumMateBehavior.RayeBattlePhaseAndDirectAttack:
                    state.InBattlePhaseSub = false;
                    break;
                case PremiumMateBehavior.IpSpTurnParity:
                    var target = (turn % 2 != 0) ? state.Rule.BaseId : state.Rule.SubId;
                    const float ipSpPreSwapDelaySeconds = 0.80f;
                    var turnSwapToken = ++state.PendingTurnSwapToken;
                    _ = RequestPremiumMateFormDelayedAsync(state, target, ipSpPreSwapDelaySeconds, turnSwapToken);
                    break;
            }
        }

        private async UniTask RequestPremiumMateFormDelayedAsync(PremiumMateState state, int targetMateId, float delaySeconds, int token)
        {
            if (state == null)
                return;
            if (delaySeconds > 0f)
                await UniTask.WaitForSeconds(delaySeconds);
            if (state.PendingTurnSwapToken != token)
                return;
            if (GetPremiumMateState(state.Side) != state)
                return;

            RequestPremiumMateForm(state, targetMateId);
        }

        private void ApplyPremiumPhase(PremiumMateState state, bool ownerTurn, MDPro3.Duel.YGOSharp.DuelPhase phase)
        {
            if (state == null)
                return;

            switch (state.Rule.Behavior)
            {
                case PremiumMateBehavior.LaundryBattlePhaseRoundTrip:
                    if (ownerTurn && phase == MDPro3.Duel.YGOSharp.DuelPhase.BattleStart)
                    {
                        state.InBattlePhaseSub = true;
                        RequestPremiumMateForm(state, state.Rule.SubId);
                    }
                    else if ((!ownerTurn || phase == MDPro3.Duel.YGOSharp.DuelPhase.Main2 || phase == MDPro3.Duel.YGOSharp.DuelPhase.End) && state.InBattlePhaseSub)
                    {
                        state.InBattlePhaseSub = false;
                        RequestPremiumMateForm(state, state.Rule.BaseId);
                    }
                    break;
                case PremiumMateBehavior.RayeBattlePhaseAndDirectAttack:
                    var battleMateId = GetRayeBattleMateId(state);
                    if (ownerTurn && phase == MDPro3.Duel.YGOSharp.DuelPhase.BattleStart)
                    {
                        state.PendingDirectAttack = false;
                        state.InBattlePhaseSub = true;
                        if (state.ActiveMateId != battleMateId)
                            RequestPremiumMateForm(state, battleMateId);
                    }
                    else if ((phase == MDPro3.Duel.YGOSharp.DuelPhase.Main2 || phase == MDPro3.Duel.YGOSharp.DuelPhase.End) && state.InBattlePhaseSub)
                    {
                        state.PendingDirectAttack = false;
                        state.InBattlePhaseSub = false;
                    }
                    break;
            }
        }

        private void InitializeMateEntry(int side)
        {
            PremiumMateState state = GetPremiumMateState(side);
            if (state != null)
            {
                foreach (var pair in state.Forms)
                    if (pair.Value != null)
                        pair.Value.gameObject.SetActive(false);

                if (!state.Forms.TryGetValue(state.ActiveMateId, out Presentation.Mate activeMate) || activeMate == null)
                    activeMate = state.Forms.Values.FirstOrDefault(m => m != null);

                if (activeMate == null)
                {
                    SetMateForSide(side, null);
                    return;
                }

                state.ActiveMateId = activeMate.code;
                ResetMateTransform(activeMate, state.Anchor);
                SetExclusivePremiumMateActiveForm(state, activeMate);
                activeMate.PrepareForPremiumSwapActivation();
                activeMate.Play(Presentation.Mate.MateAction.Entry);
                SetMateForSide(side, activeMate);
                return;
            }

            Presentation.Mate mate = GetMateForSide(side);
            if (mate != null)
            {
                mate.gameObject.SetActive(true);
                mate.Play(Presentation.Mate.MateAction.Entry);
            }
        }

        private async UniTask LoadMateForSideAsync(int side, int mateCode, Transform anchor, Func<bool> stillCurrent)
        {
            if (!stillCurrent()) return;
            SetPremiumMateState(side, null);
            SetMateForSide(side, null);

            if (mateCode == Presentation.Assets.Items.CODE_NONE)
                return;

            var normalizedMateCode = PremiumMateRules.GetBaseMateId(mateCode);
            if (PremiumMateRules.TryGetRuleByBaseId(normalizedMateCode, out PremiumMateRule rule))
            {
                var formIds = new List<int> { rule.BaseId };
                foreach (var variantId in rule.VariantIds)
                    if (!formIds.Contains(variantId))
                        formIds.Add(variantId);

                var loadedForms = new Dictionary<int, Presentation.Mate>();
                foreach (var formId in formIds)
                {
                    Presentation.Mate formMate = await Runtime.ABLoader.LoadMateAsync(formId);
                    if (!stillCurrent())
                    {
                        if (formMate != null) { formMate.gameObject.SetActive(false); UnityEngine.Object.Destroy(formMate.gameObject); }
                        foreach (Presentation.Mate form in loadedForms.Values)
                            if (form != null) { form.gameObject.SetActive(false); UnityEngine.Object.Destroy(form.gameObject); }
                        return;
                    }
                    if (formMate == null)
                        continue;

                    ConfigureLoadedMate(formMate, anchor);
                    loadedForms[formId] = formMate;
                }

                if (loadedForms.TryGetValue(rule.BaseId, out Presentation.Mate baseMate)
                    && loadedForms.TryGetValue(rule.SubId, out _))
                {
                    PremiumMateState state = new PremiumMateState
                    {
                        Side = side,
                        Rule = rule,
                        SwapEffect = PremiumMateSwapEffects.GetOrDefault(rule.BaseId),
                        Anchor = anchor,
                        ActiveMateId = rule.BaseId
                    };
                    foreach (var pair in loadedForms)
                        state.Forms[pair.Key] = pair.Value;

                    if (rule.Behavior == PremiumMateBehavior.RayeBattlePhaseAndDirectAttack)
                    {
                        // The premium Raye mate's item IDs do not match the visual form order:
                        // 1003003 = Engage, 1003103 = Kagari, 1003203 = Raye.
                        // The opening Raye-only startup is disabled; start directly on Engage.
                        state.RayeEngageMateId = rule.BaseId;
                        state.RayeBattleMateId = loadedForms.ContainsKey(1003103) ? 1003103 : rule.SubId;
                        state.ActiveMateId = state.RayeEngageMateId;
                    }

                    SetPremiumMateState(side, state);
                    if (!loadedForms.TryGetValue(state.ActiveMateId, out Presentation.Mate initialMate) || initialMate == null)
                        initialMate = baseMate;
                    SetMateForSide(side, initialMate);
                    return;
                }

                Presentation.Mate fallbackMate = loadedForms.Values.FirstOrDefault();
                if (fallbackMate != null)
                {
                    SetMateForSide(side, fallbackMate);
                    return;
                }
            }

            Presentation.Mate mate = await Runtime.ABLoader.LoadMateAsync(normalizedMateCode);
            if (!stillCurrent())
            {
                if (mate != null) { mate.gameObject.SetActive(false); UnityEngine.Object.Destroy(mate.gameObject); }
                return;
            }
            if (mate == null)
                return;

            ConfigureLoadedMate(mate, anchor);
            SetMateForSide(side, mate);
        }

        private static void ConfigureLoadedMate(Presentation.Mate mate, Transform anchor)
        {
            if (mate == null)
                return;

            ResetMateTransform(mate, anchor);
            mate.gameObject.SetActive(false);
        }

        private static void ResetMateTransform(Presentation.Mate mate, Transform anchor)
        {
            if (mate == null || anchor == null)
                return;

            mate.parent = anchor;
            mate.transform.SetParent(anchor, false);
            mate.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        private static int GetRayeEngageMateId(PremiumMateState state)
        {
            if (state == null)
                return 0;
            if (state.RayeEngageMateId > 0)
                return state.RayeEngageMateId;
            return state.Rule.BaseId;
        }

        private static int GetRayeBattleMateId(PremiumMateState state)
        {
            if (state == null)
                return 0;
            return state.RayeBattleMateId > 0 ? state.RayeBattleMateId : state.Rule.SubId;
        }

        private static int ResolvePremiumMateTargetId(PremiumMateState state, int targetMateId)
        {
            if (state == null)
                return targetMateId;

            if (state.Rule.Behavior == PremiumMateBehavior.RayeBattlePhaseAndDirectAttack
                && targetMateId == GetRayeBattleMateId(state)
                && !state.InBattlePhaseSub)
            {
                return GetRayeEngageMateId(state);
            }

            return targetMateId;
        }

        private void ActivatePremiumMateFormImmediate(PremiumMateState state, int targetMateId)
        {
            if (state == null)
                return;
            if (!state.Forms.TryGetValue(targetMateId, out Presentation.Mate targetMate) || targetMate == null)
                return;

            ResetMateTransform(targetMate, state.Anchor);
            SetExclusivePremiumMateActiveForm(state, targetMate);
            targetMate.PrepareForPremiumSwapActivation();
            state.ActiveMateId = targetMateId;
            SetMateForSide(state.Side, targetMate);
            RestartMateRandomCooldown(state.Side);
        }

        private static void SetExclusivePremiumMateActiveForm(PremiumMateState state, Presentation.Mate activeMate, Presentation.Mate additionalActiveMate = null)
        {
            if (state == null)
                return;
            foreach (var pair in state.Forms)
            {
                Presentation.Mate mate = pair.Value;
                if (mate == null)
                    continue;
                mate.gameObject.SetActive(mate == activeMate || mate == additionalActiveMate);
            }
        }

        private static bool ShouldSuppressAmbientPremiumMateActions(PremiumMateState state)
        {
            return state != null && state.Rule.Behavior == PremiumMateBehavior.RayeBattlePhaseAndDirectAttack;
        }

        private static bool HasNonChangeTrigger(IReadOnlyList<string> triggerPriority)
        {
            if (triggerPriority == null || triggerPriority.Count == 0)
                return false;

            for (var i = 0; i < triggerPriority.Count; i++)
            {
                var trigger = triggerPriority[i];
                if (string.IsNullOrEmpty(trigger))
                    continue;
                if (!trigger.StartsWith("Change", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool IsChangeTrigger(string triggerName)
        {
            return !string.IsNullOrEmpty(triggerName) && triggerName.StartsWith("Change", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasConfiguredChangeTrigger(IReadOnlyList<string> triggerPriority)
        {
            if (triggerPriority == null || triggerPriority.Count == 0)
                return false;

            for (var i = 0; i < triggerPriority.Count; i++)
            {
                if (IsChangeTrigger(triggerPriority[i]))
                    return true;
            }

            return false;
        }

        private static IReadOnlyList<string> BuildNonChangeFirstPriority(IReadOnlyList<string> triggerPriority)
        {
            if (triggerPriority == null || triggerPriority.Count <= 1)
                return triggerPriority;

            var hasLeadingChange = false;
            var hasFollowingNonChange = false;
            for (var i = 0; i < triggerPriority.Count; i++)
            {
                var trigger = triggerPriority[i];
                if (string.IsNullOrEmpty(trigger))
                    continue;
                if (IsChangeTrigger(trigger))
                {
                    hasLeadingChange = true;
                    continue;
                }

                if (hasLeadingChange)
                {
                    hasFollowingNonChange = true;
                    break;
                }
            }

            if (!hasFollowingNonChange)
                return triggerPriority;

            var reordered = new List<string>(triggerPriority.Count);
            for (var i = 0; i < triggerPriority.Count; i++)
            {
                var trigger = triggerPriority[i];
                if (string.IsNullOrEmpty(trigger) || IsChangeTrigger(trigger))
                    continue;
                reordered.Add(trigger);
            }

            for (var i = 0; i < triggerPriority.Count; i++)
            {
                var trigger = triggerPriority[i];
                if (string.IsNullOrEmpty(trigger) || !IsChangeTrigger(trigger))
                    continue;
                reordered.Add(trigger);
            }

            return reordered.Count > 0 ? reordered : triggerPriority;
        }

        private static bool MateHasAnyChangeTriggerParameter(Presentation.Mate mate, IReadOnlyList<string> triggerPriority)
        {
            if (mate == null || triggerPriority == null || triggerPriority.Count == 0)
                return false;
            var checkedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < triggerPriority.Count; i++)
            {
                var trigger = triggerPriority[i];
                if (!IsChangeTrigger(trigger))
                    continue;
                if (!checkedSet.Add(trigger))
                    continue;
                if (mate.HasMasterDuelTriggerParameter(trigger))
                    return true;
            }

            return false;
        }

        private static bool MateHasAnyTransitionCandidate(Presentation.Mate mate, IReadOnlyList<string> triggerPriority)
        {
            if (mate == null || triggerPriority == null || triggerPriority.Count == 0)
                return false;

            return mate.TryDescribeMasterDuelTransitionCandidates(triggerPriority, out _);
        }

        private static UniTask WaitForSecondsUnscaledAsync(float seconds)
        {
            if (seconds <= 0f)
                return UniTask.CompletedTask;

            return Services.ReplayVideoExport.Capturing ? UniTask.WaitForSeconds(seconds)
                : UniTask.Delay(TimeSpan.FromSeconds(seconds), DelayType.UnscaledDeltaTime);
        }

        private static UniTask WaitForSecondsSwapTimingAsync(float seconds, bool useUnscaledTiming)
        {
            if (seconds <= 0f)
                return UniTask.CompletedTask;

            return useUnscaledTiming
                ? WaitForSecondsUnscaledAsync(seconds)
                : UniTask.WaitForSeconds(seconds);
        }

        private static async UniTask<float> MeasureCurrentTransitionQueueDelayAsync(
            Presentation.Mate mate,
            int beforeStateHash,
            bool beforeInTransition,
            float maxWaitSeconds)
        {
            if (mate == null || maxWaitSeconds <= 0f)
                return 0f;

            var startRealtime = Services.ReplayVideoExport.PlaybackClock;
            while (Services.ReplayVideoExport.PlaybackClock - startRealtime < maxWaitSeconds)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
                if (!mate.TryGetMasterDuelPrimaryAnimatorSnapshot(
                        out var stateHash,
                        out _,
                        out _,
                        out var inTransition,
                        out var activeInHierarchy)
                    || !activeInHierarchy)
                {
                    return 0f;
                }

                if ((!beforeInTransition && inTransition) || stateHash != beforeStateHash)
                    return Services.ReplayVideoExport.PlaybackClock - startRealtime;
            }

            return 0f;
        }

        private void RequestPremiumMateForm(PremiumMateState state, int targetMateId)
        {
            if (state == null)
                return;
            targetMateId = ResolvePremiumMateTargetId(state, targetMateId);
            if (!state.Forms.ContainsKey(targetMateId))
                return;

            if (state.IsTransitioning && state.QueuedMateId == targetMateId)
                return;
            if (!state.IsTransitioning && state.ActiveMateId == targetMateId)
                return;
            state.QueuedMateId = targetMateId;
            if (state.IsTransitioning)
                return;

            _ = ProcessPremiumMateSwapQueueAsync(state);
        }

        private async UniTask ProcessPremiumMateSwapQueueAsync(PremiumMateState state)
        {
            if (state == null)
                return;

            state.IsTransitioning = true;
            try
            {
                while (state.QueuedMateId != 0)
                {
                    if (state.QueuedMateId == state.ActiveMateId)
                    {
                        state.QueuedMateId = 0;
                        break;
                    }

                    var nextMateId = state.QueuedMateId;
                    state.QueuedMateId = 0;
                    await SwapPremiumMateAsync(state, nextMateId);
                }
            }
            finally
            {
                state.IsTransitioning = false;
            }
        }

        private async UniTask SwapPremiumMateAsync(PremiumMateState state, int targetMateId)
        {
            if (state == null)
                return;
            targetMateId = ResolvePremiumMateTargetId(state, targetMateId);
            if (!state.Forms.TryGetValue(targetMateId, out Presentation.Mate nextMate) || nextMate == null)
                return;

            Presentation.Mate currentMate = GetMateForSide(state.Side);
            if (currentMate == null && state.Forms.TryGetValue(state.ActiveMateId, out Presentation.Mate fromState))
                currentMate = fromState;

            if (currentMate == nextMate)
            {
                SetExclusivePremiumMateActiveForm(state, nextMate);
                state.ActiveMateId = targetMateId;
                SetMateForSide(state.Side, nextMate);
                return;
            }

            var toSub = state.Rule.Behavior == PremiumMateBehavior.RayeBattlePhaseAndDirectAttack
                ? targetMateId == GetRayeBattleMateId(state)
                : targetMateId == state.Rule.SubId;
            var swapDelay = toSub ? state.SwapEffect.ToSubDelaySeconds : state.SwapEffect.ToBaseDelaySeconds;
            var effectAssetPath = state.SwapEffect.GetEffectAssetPath(toSub);
            var useUnscaledSwapTiming = state.SwapEffect.UseUnscaledSwapTiming;
            var changeOnTargetMate = state.SwapEffect.PlayChangeOnTargetMate;
            var changeOnBothMates = state.SwapEffect.PlayChangeOnBothMates;
            var playChangeOnCurrentMate = !changeOnTargetMate || changeOnBothMates;
            var playChangeOnNextMate = changeOnTargetMate || changeOnBothMates;
            var triggerPriorityOverride = state.OverrideTriggerPriority;
            var currentTriggerPriority = triggerPriorityOverride ?? (toSub
                ? state.SwapEffect.ToSubCurrentTriggerPriority
                : state.SwapEffect.ToBaseCurrentTriggerPriority);
            var nextTriggerPriority = triggerPriorityOverride ?? (toSub
                ? state.SwapEffect.ToSubNextTriggerPriority
                : state.SwapEffect.ToBaseNextTriggerPriority);
            IReadOnlyList<string> effectiveNextTriggerPriority = nextTriggerPriority;

            if (state.SwapEffect.UseChangeMotion && currentMate != null && playChangeOnCurrentMate
                && !MateHasAnyTransitionCandidate(currentMate, currentTriggerPriority))
                playChangeOnCurrentMate = false;
            state.OverrideTriggerPriority = null;
            var playedChangeOnCurrent = false;
            var playedChangeOnNext = false;
            var currentTransitionDelay = 0f;
            var nextTransitionDelay = 0f;
            var currentTriggerQueueDelay = 0f;
            var currentTriggerQueueProbeStarted = false;
            var currentTriggerQueueDelayTask = UniTask.FromResult(0f);
            var currentBeforeStateHash = 0;
            var currentBeforeInTransition = false;
            var hasCurrentSnapshot = false;
            if (state.SwapEffect.CompensateCurrentTriggerQueueDelay && currentMate != null)
            {
                hasCurrentSnapshot = currentMate.TryGetMasterDuelPrimaryAnimatorSnapshot(
                    out currentBeforeStateHash,
                    out _,
                    out _,
                    out currentBeforeInTransition,
                    out _);
            }

            void StartCurrentTriggerQueueProbeIfNeeded()
            {
                if (currentTriggerQueueProbeStarted || !playedChangeOnCurrent || !hasCurrentSnapshot)
                    return;
                if (state.SwapEffect.MaxCurrentTriggerQueueDelaySeconds <= 0f)
                    return;

                currentTriggerQueueProbeStarted = true;
                currentTriggerQueueDelayTask = MeasureCurrentTransitionQueueDelayAsync(
                    currentMate,
                    currentBeforeStateHash,
                    currentBeforeInTransition,
                    state.SwapEffect.MaxCurrentTriggerQueueDelaySeconds);
            }

            var effectPosition = currentMate != null ? currentMate.transform.position : state.Anchor.position;
            var effectLabel = toSub ? state.SwapEffect.ToSubEffectLabel : state.SwapEffect.ToBaseEffectLabel;
            var preferEffectLabelPlayback = state.SwapEffect.PreferEffectLabelPlayback && !string.IsNullOrEmpty(effectLabel);
            if (!preferEffectLabelPlayback && state.SwapEffect.UseChangeMotion && currentMate != null && playChangeOnCurrentMate)
            {
                playedChangeOnCurrent = currentMate.PlayChangeTransition(currentTriggerPriority, out currentTransitionDelay);
                StartCurrentTriggerQueueProbeIfNeeded();
            }

            if (state.SwapEffect.SourceToEffectDelaySeconds > 0f)
                await WaitForSecondsSwapTimingAsync(state.SwapEffect.SourceToEffectDelaySeconds, useUnscaledSwapTiming);

            SwapEffectLabelPlaybackResult effectPlayback = await SpawnPremiumSwapEffectAsync(state, effectAssetPath, effectPosition, effectLabel, toSub);
            if (preferEffectLabelPlayback && state.SwapEffect.UseChangeMotion && currentMate != null && playChangeOnCurrentMate)
            {
                if (!effectPlayback.Consumed)
                {
                    playedChangeOnCurrent = currentMate.PlayChangeTransition(currentTriggerPriority, out currentTransitionDelay);
                    StartCurrentTriggerQueueProbeIfNeeded();
                }
            }

            var delay = swapDelay;
            if (currentTriggerQueueProbeStarted)
            {
                currentTriggerQueueDelay = await currentTriggerQueueDelayTask;
                if (currentTriggerQueueDelay > 0f)
                    delay += currentTriggerQueueDelay;
            }

            if (playedChangeOnCurrent)
            {
                if (state.SwapEffect.UseTransitionDelayAsMinimum)
                    delay = Mathf.Max(delay, currentTransitionDelay);
                else if (delay <= 0f)
                    delay = currentTransitionDelay;
            }

            var keepCurrentAliveForOverlap = currentMate != null
                && playedChangeOnCurrent
                && changeOnBothMates
                && currentTransitionDelay > delay;
            if (delay > 0f)
                await WaitForSecondsSwapTimingAsync(delay, useUnscaledSwapTiming);

            targetMateId = ResolvePremiumMateTargetId(state, targetMateId);
            if (!state.Forms.TryGetValue(targetMateId, out nextMate) || nextMate == null)
                return;

            ResetMateTransform(nextMate, state.Anchor);
            if (currentMate != null && !keepCurrentAliveForOverlap)
                currentMate.gameObject.SetActive(false);
            SetExclusivePremiumMateActiveForm(state, nextMate, keepCurrentAliveForOverlap ? currentMate : null);
            nextMate.PrepareForPremiumSwapActivation();
            state.ActiveMateId = targetMateId;
            SetMateForSide(state.Side, nextMate);

            if (state.SwapEffect.PreferNonChangeNextWhenChangeTriggerMissing
                && triggerPriorityOverride == null
                && HasConfiguredChangeTrigger(nextTriggerPriority)
                && !MateHasAnyChangeTriggerParameter(nextMate, nextTriggerPriority))
            {
                var reorderedPriority = BuildNonChangeFirstPriority(nextTriggerPriority);
                if (!ReferenceEquals(reorderedPriority, nextTriggerPriority))
                    effectiveNextTriggerPriority = reorderedPriority;
            }

            if (state.SwapEffect.NextMotionLeadInSeconds > 0f)
                await WaitForSecondsUnscaledAsync(state.SwapEffect.NextMotionLeadInSeconds);

            var hasNextActivationTrigger = HasNonChangeTrigger(effectiveNextTriggerPriority);
            var shouldPlayNextMotion = state.SwapEffect.UseChangeMotion
                && (playChangeOnNextMate || !playedChangeOnCurrent || hasNextActivationTrigger);
            if (shouldPlayNextMotion)
            {
                playedChangeOnNext = nextMate.PlayChangeTransition(effectiveNextTriggerPriority, out nextTransitionDelay);
                var nextStageWait = nextTransitionDelay;
                if (state.SwapEffect.NextMotionMinDurationSeconds > 0f)
                    nextStageWait = Mathf.Max(nextStageWait, state.SwapEffect.NextMotionMinDurationSeconds);
                if (nextStageWait > 0f)
                    await WaitForSecondsUnscaledAsync(nextStageWait);
                if (state.SwapEffect.NextMotionDelaySeconds > 0f)
                    await WaitForSecondsUnscaledAsync(state.SwapEffect.NextMotionDelaySeconds);
            }

            if (keepCurrentAliveForOverlap && currentMate != null)
                currentMate.gameObject.SetActive(false);

            RestartMateRandomCooldown(state.Side);
        }

        private async UniTask<SwapEffectLabelPlaybackResult> SpawnPremiumSwapEffectAsync(PremiumMateState state, string effectPath, Vector3 position, string effectLabel = null, bool toSub = true)
        {
            if (sceneEnvironment != null && sceneEnvironment.MatesHidden)
                return SwapEffectLabelPlaybackResult.None;
            if (state == null || state.SwapEffect == null)
                return SwapEffectLabelPlaybackResult.None;
            if (!state.SwapEffect.HasChangeEffectAsset(toSub))
                return SwapEffectLabelPlaybackResult.None;

            var fullPath = Path.Combine(Runtime.Program.root, effectPath);
            GameObject effect = null;
            var selectedScore = int.MinValue;
            void ConsiderEffectCandidate(GameObject candidate)
            {
                if (candidate == null)
                    return;

                var score = GetSwapEffectCandidateScore(candidate);
                if (effect == null || score > selectedScore)
                {
                    if (effect != null && effect != candidate)
                        UnityEngine.Object.Destroy(effect);

                    effect = candidate;
                    selectedScore = score;
                }
                else
                {
                    UnityEngine.Object.Destroy(candidate);
                }
            }

            if (Directory.Exists(fullPath))
            {
                var leafName = Path.GetFileName(effectPath);
                var directBundlePath = Path.Combine(fullPath, leafName);
                if (!string.IsNullOrEmpty(leafName) && File.Exists(directBundlePath))
                {
                    var loadPath = effectPath + "/" + leafName;
                    ConsiderEffectCandidate(await Runtime.ABLoader.LoadFromFileAsync(loadPath, false, true));
                }

                ConsiderEffectCandidate(await Runtime.ABLoader.LoadFromFolderAsync<PlayableDirector>(effectPath, false, true));
                ConsiderEffectCandidate(await Runtime.ABLoader.LoadFromFolderAsync<ParticleSystem>(effectPath, false, true));
            }
            else
            {
                ConsiderEffectCandidate(await Runtime.ABLoader.LoadFromFileAsync(effectPath, false, true));
            }

            if (effect == null)
                return SwapEffectLabelPlaybackResult.None;

            // The active theme can change while an effect bundle is loading.
            if (sceneEnvironment != null && sceneEnvironment.MatesHidden)
            {
                UnityEngine.Object.Destroy(effect);
                return SwapEffectLabelPlaybackResult.None;
            }
            effect.transform.SetParent(Runtime.Program.instance.container_3D, false);
            effect.transform.position = position;
            SwapEffectLabelPlaybackResult labelResult = await PlayPremiumSwapEffectLabelAsync(effect, effectLabel);
            UnityEngine.Object.Destroy(effect, 5f);
            return labelResult;
        }

        private async UniTask<SwapEffectLabelPlaybackResult> PlayPremiumSwapEffectLabelAsync(GameObject effect, string effectLabel)
        {
            if (effect == null)
                return SwapEffectLabelPlaybackResult.None;

            var labelPlayed = false;
            var fallbackPlayed = false;

            var animators = effect.GetComponentsInChildren<Animator>(true);
            for (var i = 0; i < animators.Length; i++)
            {
                var animator = animators[i];
                if (animator == null)
                    continue;

                if (TryPlayAnimatorEffectLabel(animator, effectLabel))
                    labelPlayed = true;
            }

            var directors = effect.GetComponentsInChildren<PlayableDirector>(true);
            for (var i = 0; i < directors.Length; i++)
            {
                var director = directors[i];
                if (director == null)
                    continue;

                if (TryPlayDirectorEffectLabel(director, effectLabel, out var startTime))
                {
                    director.time = startTime;
                    director.Play();
                    labelPlayed = true;
                }
            }

            var changeEffects = effect.GetComponentsInChildren<Presentation.Fields.BgAvatarChangeEffect>(true);
            if (!labelPlayed && !string.IsNullOrEmpty(effectLabel))
            {
                for (var i = 0; i < changeEffects.Length; i++)
                {
                    if (!TryPlayBgAvatarChangeEffectLabel(changeEffects[i], effectLabel))
                        continue;

                    labelPlayed = true;
                    break;
                }
            }

            if (!labelPlayed && !string.IsNullOrEmpty(effectLabel))
            {
                if (TryPlayElementObjectLabel(effect, effectLabel))
                    labelPlayed = true;
                else if (TryPlayAnyComponentLabel(effect, effectLabel))
                    labelPlayed = true;
            }

            var labeledControllers = effect.GetComponentsInChildren<LabeledPlayableController>(true);
            if (!labelPlayed && !string.IsNullOrEmpty(effectLabel) && labeledControllers.Length > 0)
            {
                for (var i = 0; i < directors.Length; i++)
                {
                    var director = directors[i];
                    if (director == null || director.state == PlayState.Playing)
                        continue;
                    director.Play();
                }

                const int maxFramesToWait = 3;
                for (var frame = 0; frame <= maxFramesToWait && !labelPlayed; frame++)
                {
                    for (var i = 0; i < labeledControllers.Length; i++)
                    {
                        LabeledPlayableController controller = labeledControllers[i];
                        if (controller == null || controller.loopMixerBehaviour == null)
                            continue;

                        controller.PlayLabel(effectLabel, (TimelineClip)null);
                        labelPlayed = true;
                        break;
                    }

                    if (!labelPlayed && frame < maxFramesToWait)
                        await UniTask.Yield();
                }
            }

            if (!labelPlayed && !string.IsNullOrEmpty(effectLabel) && directors.Length == 1)
            {
                var director = directors[0];
                if (director != null)
                {
                    director.time = 0d;
                    director.Play();
                    fallbackPlayed = true;
                }
            }

            return new SwapEffectLabelPlaybackResult(labelPlayed || fallbackPlayed);
        }

        private static bool TryPlayBgAvatarChangeEffectLabel(Presentation.Fields.BgAvatarChangeEffect changeEffect, string effectLabel)
        {
            if (changeEffect == null || string.IsNullOrEmpty(effectLabel))
                return false;

            var toMain = LabelEquals(changeEffect.toMainLabel, effectLabel);
            var toSub = LabelEquals(changeEffect.toSubLabel, effectLabel);
            if (!toMain && !toSub)
                return false;

            var fieldName = toMain ? "toMainObj" : "toSubObj";
            var field = typeof(Presentation.Fields.BgAvatarChangeEffect).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var particle = field != null ? field.GetValue(changeEffect) as ParticleSystem : null;
            if (particle != null)
            {
                PlayTargetObject(particle.gameObject);
                return true;
            }

            if (TryPlayElementObjectLabel(changeEffect.gameObject, effectLabel))
                return true;

            return TryPlayAnyComponentLabel(changeEffect.gameObject, effectLabel);
        }

        private static bool TryPlayElementObjectLabel(GameObject root, string effectLabel)
        {
            if (root == null || string.IsNullOrEmpty(effectLabel))
                return false;

            var elements = root.GetComponentsInChildren<Presentation.Elements.ElementObject>(true);
            var played = false;
            for (var i = 0; i < elements.Length; i++)
            {
                Presentation.Elements.ElementObject element = elements[i];
                if (element == null || !LabelEquals(element.label, effectLabel))
                    continue;

                PlayTargetObject(element.gameObject);
                played = true;
            }

            return played;
        }

        private static bool TryPlayAnyComponentLabel(GameObject root, string effectLabel)
        {
            if (root == null || string.IsNullOrEmpty(effectLabel))
                return false;

            var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            var played = false;
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null)
                    continue;

                var field = behaviour.GetType().GetField("label", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null || field.FieldType != typeof(string))
                    continue;

                var value = field.GetValue(behaviour) as string;
                if (!LabelEquals(value, effectLabel))
                    continue;

                PlayTargetObject(behaviour.gameObject);
                played = true;
            }

            return played;
        }

        private static void PlayTargetObject(GameObject target)
        {
            if (target == null)
                return;

            target.SetActive(true);

            var particles = target.GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < particles.Length; i++)
            {
                var particle = particles[i];
                if (particle == null)
                    continue;

                particle.gameObject.SetActive(true);
                particle.Clear(true);
                particle.Play(true);
            }

            var animators = target.GetComponentsInChildren<Animator>(true);
            for (var i = 0; i < animators.Length; i++)
            {
                var animator = animators[i];
                if (animator == null)
                    continue;
                animator.gameObject.SetActive(true);
                animator.Rebind();
                animator.Update(0f);
                animator.Play(0, 0, 0f);
            }

            var directors = target.GetComponentsInChildren<PlayableDirector>(true);
            for (var i = 0; i < directors.Length; i++)
            {
                var director = directors[i];
                if (director == null)
                    continue;
                director.gameObject.SetActive(true);
                director.time = 0d;
                director.Play();
            }
        }

        private static bool LabelEquals(string a, string b)
        {
            return !string.IsNullOrEmpty(a)
                && !string.IsNullOrEmpty(b)
                && a.Equals(b, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryPlayDirectorEffectLabel(PlayableDirector director, string effectLabel, out double startTime)
        {
            startTime = 0d;
            if (director == null || string.IsNullOrEmpty(effectLabel))
                return false;

            var playableName = director.playableAsset != null ? director.playableAsset.name : string.Empty;
            var nameMatch =
                (!string.IsNullOrEmpty(playableName) && playableName.IndexOf(effectLabel, StringComparison.OrdinalIgnoreCase) >= 0)
                || director.name.IndexOf(effectLabel, StringComparison.OrdinalIgnoreCase) >= 0
                || director.gameObject.name.IndexOf(effectLabel, StringComparison.OrdinalIgnoreCase) >= 0;
            if (nameMatch)
                return true;

            return TryGetDirectorLabelStartTime(director, effectLabel, out startTime);
        }

        private static bool TryGetDirectorLabelStartTime(PlayableDirector director, string effectLabel, out double startTime)
        {
            startTime = 0d;
            if (director == null || string.IsNullOrEmpty(effectLabel) || director.playableAsset == null)
                return false;

            var outputs = director.playableAsset.outputs;
            foreach (var output in outputs)
            {
                var track = output.sourceObject as TrackAsset;
                if (track == null)
                    continue;

                foreach (var clip in track.GetClips())
                {
                    if (clip == null)
                        continue;

                    var displayName = clip.displayName;
                    if (!string.IsNullOrEmpty(displayName)
                        && displayName.Equals(effectLabel, StringComparison.OrdinalIgnoreCase))
                    {
                        startTime = clip.start;
                        return true;
                    }
                }
            }

            return false;
        }

        private static int GetSwapEffectCandidateScore(GameObject candidate)
        {
            if (candidate == null)
                return int.MinValue;

            var labeledControllers = candidate.GetComponentsInChildren<LabeledPlayableController>(true).Length;
            var directors = candidate.GetComponentsInChildren<PlayableDirector>(true).Length;
            var animators = candidate.GetComponentsInChildren<Animator>(true).Length;
            var particles = candidate.GetComponentsInChildren<ParticleSystem>(true).Length;
            var changeEffects = candidate.GetComponentsInChildren<Presentation.Fields.BgAvatarChangeEffect>(true).Length;
            var elementLabels = candidate.GetComponentsInChildren<Presentation.Elements.ElementObject>(true).Length;
            return labeledControllers * 100 + changeEffects * 60 + directors * 20 + animators * 8 + elementLabels * 4 + particles * 2;
        }

        private static bool TryPlayAnimatorEffectLabel(Animator animator, string effectLabel)
        {
            if (animator == null || string.IsNullOrEmpty(effectLabel))
                return false;

            var played = false;
            var parameters = animator.parameters;
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter.type != AnimatorControllerParameterType.Trigger)
                    continue;
                if (!parameter.name.Equals(effectLabel, StringComparison.OrdinalIgnoreCase))
                    continue;

                animator.SetTrigger(parameter.name);
                played = true;
            }
            if (played)
                return true;

            for (var layer = 0; layer < animator.layerCount; layer++)
            {
                var layerName = animator.GetLayerName(layer);
                var shortHash = Animator.StringToHash(effectLabel);
                var fullHash = Animator.StringToHash($"{layerName}.{effectLabel}");
                if (animator.HasState(layer, fullHash))
                {
                    animator.Play(fullHash, layer, 0f);
                    played = true;
                    continue;
                }
                if (animator.HasState(layer, shortHash))
                {
                    animator.Play(shortHash, layer, 0f);
                    played = true;
                }
            }
            if (played)
                return true;

            var controller = animator.runtimeAnimatorController;
            if (controller == null)
                return false;

            var clips = controller.animationClips;
            for (var i = 0; i < clips.Length; i++)
            {
                var clip = clips[i];
                if (clip == null)
                    continue;
                if (!clip.name.Equals(effectLabel, StringComparison.OrdinalIgnoreCase))
                    continue;

                animator.Play(clip.name, 0, 0f);
                return true;
            }

            return false;
        }

        private Presentation.Mate GetMateForSide(int side)
        {
            return side == 0 ? mate0 : mate1;
        }

        private void SetMateForSide(int side, Presentation.Mate mate)
        {
            if (side == 0)
                mate0 = mate;
            else
                mate1 = mate;
        }

        private PremiumMateState GetPremiumMateState(int side)
        {
            return side == 0 ? premiumMate0 : premiumMate1;
        }

        private void SetPremiumMateState(int side, PremiumMateState state)
        {
            if (side == 0)
                premiumMate0 = state;
            else
                premiumMate1 = state;
        }

        private void ResetPremiumMateStates()
        {
            premiumMate0 = null;
            premiumMate1 = null;
            mate0RandomCooldownTween?.Kill();
            mate0RandomCooldownTween = null;
            mate1RandomCooldownTween?.Kill();
            mate1RandomCooldownTween = null;
        }

        public void RefreshBgState()
        {
            ResizeDecks();
            RefreshGravesState();
        }

        public void ResizeDecks()
        {
            if (myDeck == null || myExtra == null
                || opDeck == null || opExtra == null)
                return;

            ResizeDeckModel(myDeck, Core.GetLocationCardCount(MDPro3.Duel.YGOSharp.CardLocation.Deck, 0));
            ResizeDeckModel(myExtra, Core.GetLocationCardCount(MDPro3.Duel.YGOSharp.CardLocation.Extra, 0));
            ResizeDeckModel(opDeck, Core.GetLocationCardCount(MDPro3.Duel.YGOSharp.CardLocation.Deck, 1));
            ResizeDeckModel(opExtra, Core.GetLocationCardCount(MDPro3.Duel.YGOSharp.CardLocation.Extra, 1));
        }

        internal bool UsesThemeFixtures => fieldProps != null && sceneEnvironment != null && sceneEnvironment.HasOwnTheme;

        internal bool TryGetThemePilePosition(int side, bool banish, out Vector3 position)
        {
            position = Vector3.zero;
            return UsesThemeFixtures && fieldProps.TryGetPilePosition(side, banish, out position);
        }
        internal void SetSceneOpeningProgress(float battle) => sceneEnvironment?.SetOpeningProgress(battle);

        public void RefreshGravesState()
        {
            if (grave0Manager == null
                || grave1Manager == null)
                return;

            int grave0Count = Core.GetLocationCardCount(MDPro3.Duel.YGOSharp.CardLocation.Grave, 0);
            int exclude0Count = Core.GetLocationCardCount(MDPro3.Duel.YGOSharp.CardLocation.Removed, 0);
            int grave1Count = Core.GetLocationCardCount(MDPro3.Duel.YGOSharp.CardLocation.Grave, 1);
            int exclude1Count = Core.GetLocationCardCount(MDPro3.Duel.YGOSharp.CardLocation.Removed, 1);
            if (!UsesThemeFixtures)
            {
                RefreshGraveState(grave0Manager, grave0Count);
                RefreshExcludeState(grave0Manager, exclude0Count);
                RefreshGraveState(grave1Manager, grave1Count);
                RefreshExcludeState(grave1Manager, exclude1Count);
            }
        }

        private void ResizeDeckModel(Presentation.Elements.ElementObjectManager deck, int count)
        {
            var deckSetOffset = deck.GetElement<Transform>("DeckSetOffset");
            if (count == 0)
                deckSetOffset.transform.localScale = Vector3.zero;
            else
                deckSetOffset.transform.localScale = new Vector3(0.9f, count / 40f, 0.9f);

            var cardShuffle = deck.GetElement<Transform>("CardShuffleTop");
            if (count == 0)
                cardShuffle.transform.localScale = Vector3.zero;
            else
                cardShuffle.transform.localScale = new Vector3(0.9f, count / 40f, 0.9f);
        }

        private void RefreshGraveState(Presentation.Elements.ElementObjectManager grave, int count)
        {
            var stateMap = new Dictionary<Func<int, bool>, string>
            {
                { c => c >= 20, "GraveIdleS3" },
                { c => c >= 10, "GraveIdleS2" },
                { c => c > 0,  "GraveIdleS1" }
            };

            var particleSystems = new[] { "GraveIdleS1", "GraveIdleS2", "GraveIdleS3" }
                .ToDictionary(name => name, grave.GetElement<ParticleSystem>);

            foreach (var ps in particleSystems.Values)
                ps.Stop();

            var activeSystem = stateMap.FirstOrDefault(kv => kv.Key(count)).Value;
            if (activeSystem != null && particleSystems.TryGetValue(activeSystem, out var systemToPlay))
                systemToPlay.Play();

            grave.GetElement<Renderer>("Material01").material.SetFloat(
                "_GraveCardExist",
                count > 0 ? 1 : 0
            );
        }

        private void RefreshExcludeState(Presentation.Elements.ElementObjectManager exclude, int count)
        {
            var stateMap = new Dictionary<Func<int, bool>, string>
            {
                { c => c >= 20, "ExcludeIdleS3" },
                { c => c >= 10, "ExcludeIdleS2" },
                { c => c > 0,  "ExcludeIdleS1" }
            };

            var particleSystems = new[] { "ExcludeIdleS1", "ExcludeIdleS2", "ExcludeIdleS3" }
                .ToDictionary(name => name, exclude.GetElement<ParticleSystem>);

            foreach (var ps in particleSystems.Values)
                ps.Stop();

            var activeSystem = stateMap.FirstOrDefault(kv => kv.Key(count)).Value;
            if (activeSystem != null && particleSystems.TryGetValue(activeSystem, out var systemToPlay))
                systemToPlay.Play();

            exclude.GetElement<Renderer>("Material01").material.SetFloat(
                "_ExcludeCardExist",
                count > 0 ? 1 : 0
            );
        }

        public void UpdateBgEffects(int player, bool first = false)
        {
            if (myDeck == null || myExtra == null
                || opDeck == null || opExtra == null
                || grave0Manager == null
                || grave1Manager == null)
                return;

            if (player == 0)
            {
                if (!first)
                    field0Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.PhaseToDamagePhaseAll);
                //field0Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhaseToNextPhaseAll);
                if (!first && CanPlayMateAction(0, mate0))
                    mate0.Play(Presentation.Mate.MateAction.GetDamage);
                if (bgPhase0 == 1 && life0 < (lpLimit * 0.75f))
                {
                    bgPhase0++;
                    var seLabel = "SE_FIELD_MAT" + field0Manager.name.Substring(4, 3) + "_PHASE1_P";
                    field0Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase1ToPhase2, seLabel);
                    field1Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.OtherSideDamagePhase1ToPhase2);
                    grave0Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase1ToPhase2);
                    if (stand0Manager != null)
                        stand0Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase1ToPhase2);
                }
                if (bgPhase0 == 2 && life0 < (lpLimit * 0.5f))
                {
                    bgPhase0++;
                    var seLabel = "SE_FIELD_MAT" + field0Manager.name.Substring(4, 3) + "_PHASE2_P";
                    field0Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase2ToPhase3, seLabel);
                    field1Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.OtherSideDamagePhase2ToPhase3);
                    grave0Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase2ToPhase3);
                    if (stand0Manager != null)
                        stand0Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase2ToPhase3);
                }
                if (bgPhase0 == 3 && life0 < (lpLimit * 0.25f))
                {
                    bgPhase0++;
                    var seLabel = "SE_FIELD_MAT" + field0Manager.name.Substring(4, 3) + "_PHASE3_P";
                    field0Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase3ToPhase4, seLabel);
                    field1Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.OtherSideDamagePhase3ToPhase4);
                    grave0Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase3ToPhase4);
                    if (stand0Manager != null)
                        stand0Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase3ToPhase4);
                    Runtime.AudioManager.PlayBgmClimax();
                }
                if (bgPhase0 == 4 && life0 <= 0)
                {
                    //bgPhase0++;
                    //var seLabel = "SE_FIELD_MAT" + field0Manager.name.Substring(4, 3) + "_PHASE4_P";
                    //field0Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase4ToEnd, seLabel);
                    //field0Manager.PlayAnimatorTrigger(TriggerLabelDefine.EndLose);
                    //field1Manager.PlayAnimatorTrigger(TriggerLabelDefine.EndWin);
                    //grave0Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase4ToEnd);
                    //if (stand0Manager != null)
                    //    stand0Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase4ToEnd);
                }
            }
            else
            {
                if (!first)
                    field1Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.PhaseToDamagePhaseAll);
                //field1Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhaseToNextPhaseAll);
                if (!first && CanPlayMateAction(1, mate1))
                    mate1.Play(Presentation.Mate.MateAction.GetDamage);
                if (bgPhase1 == 1 && life1 < (lpLimit * 0.75f))
                {
                    bgPhase1++;
                    var seLabel = "SE_FIELD_MAT" + field1Manager.name.Substring(4, 3) + "_PHASE1_P";
                    field1Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase1ToPhase2, seLabel);
                    field0Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.OtherSideDamagePhase1ToPhase2);
                    grave1Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase1ToPhase2);
                    if (stand1Manager != null)
                        stand1Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase1ToPhase2);
                }
                if (bgPhase1 == 2 && life1 < (lpLimit * 0.5f))
                {
                    bgPhase1++;
                    var seLabel = "SE_FIELD_MAT" + field1Manager.name.Substring(4, 3) + "_PHASE2_P";
                    field1Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase2ToPhase3, seLabel);
                    field0Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.OtherSideDamagePhase2ToPhase3);
                    grave1Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase2ToPhase3);
                    if (stand1Manager != null)
                        stand1Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase2ToPhase3);
                }
                if (bgPhase1 == 3 && life1 < (lpLimit * 0.25f))
                {
                    bgPhase1++;
                    var seLabel = "SE_FIELD_MAT" + field1Manager.name.Substring(4, 3) + "_PHASE3_P";
                    field1Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase3ToPhase4, seLabel);
                    field0Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.OtherSideDamagePhase3ToPhase4);
                    grave1Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase3ToPhase4);
                    if (stand1Manager != null)
                        stand1Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase3ToPhase4);
                    Runtime.AudioManager.PlayBgmClimax();
                }
                if (bgPhase1 == 4 && life1 <= 0)
                {
                    //bgPhase1++;
                    //var seLabel = "SE_FIELD_MAT" + field1Manager.name.Substring(4, 3) + "_PHASE4_P";
                    //field1Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase4ToEnd, seLabel);
                    //field0Manager.PlayAnimatorTrigger(TriggerLabelDefine.EndWin);
                    //field1Manager.PlayAnimatorTrigger(TriggerLabelDefine.EndLose);
                    //grave1Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase4ToEnd);
                    //if (stand1Manager != null)
                    //    stand1Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase4ToEnd);
                }
            }
        }

        public void PlayGraveEffect(GPS p, bool isIn)
        {
            int generation = deckPresentationGeneration;
            if (!IsDeckPresentationCurrent(generation) || grave0Manager == null || grave1Manager == null)
                return;

            Presentation.Elements.ElementObjectManager manager;
            if (p.InMyControl())
                manager = grave0Manager;
            else
                manager = grave1Manager;
            if (manager == null)
                return;

            Presentation.Fields.BgEffectSetting effect;
            Presentation.Fields.BgEffectSetting effectEnd;
            string audio = string.Empty;
            if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Grave) > 0)
            {
                if (isIn)
                {
                    effect = manager.GetElement<Presentation.Fields.BgEffectSetting>("GraveIn");
                    effectEnd = manager.GetElement<Presentation.Fields.BgEffectSetting>("GraveInend");
                    audio = "SE_CEMETARY_ABSORB";
                }
                else
                {
                    effect = manager.GetElement<Presentation.Fields.BgEffectSetting>("GraveOut");
                    effectEnd = manager.GetElement<Presentation.Fields.BgEffectSetting>("GraveOutend");
                    audio = "SE_CEMETARY_GOOUT";
                }
            }
            else
            {
                if (isIn)
                {
                    effect = manager.GetElement<Presentation.Fields.BgEffectSetting>("ExcludeIn");
                    effectEnd = manager.GetElement<Presentation.Fields.BgEffectSetting>("ExcludeInend");
                    audio = "SE_EXCLUSION_ABSORB";
                }
                else
                {
                    effect = manager.GetElement<Presentation.Fields.BgEffectSetting>("ExcludeOut");
                    effectEnd = manager.GetElement<Presentation.Fields.BgEffectSetting>("ExcludeOutend");
                    audio = "SE_EXCLUSION_GOOUT";
                }
            }
            bool CanPlay(Presentation.Fields.BgEffectSetting queuedEffect) => IsDeckPresentationCurrent(generation)
                && manager != null && queuedEffect != null && queuedEffect.particle != null;
            if (effect != null && effect.particle != null)
            {
                DOTween.To(v => { }, 0, 0, effect.delay).OnComplete(() =>
                {
                    // Delays can outlive the field or even enter another duel.
                    if (!CanPlay(effect)) return;
                    effect.particle.Play();
                    Runtime.AudioManager.PlaySE(audio);
                });
            }
            if (effectEnd != null && effectEnd.particle != null)
            {
                DOTween.To(v => { }, 0, 0, effectEnd.delay).OnComplete(() =>
                {
                    if (!CanPlay(effectEnd)) return;
                    effectEnd.particle.Play();
                });
            }
        }

        public void ShowBgHint()
        {
            if (myDeck == null || myExtra == null
                || opDeck == null || opExtra == null
                || grave0Manager == null
                || grave1Manager == null)
                return;

            bool haveHint = false;
            var cardsActivated = cards.Where(c => c.buttons.Count > 0);

            foreach (GameCard card in cardsActivated)
                if (card.p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Grave))
                    if (card.p.InMyControl())
                    {
                        var effect = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_active_grave_001");
                        effect.transform.SetParent(grave0Manager.GetElement<Transform>("GraveHighlightNear"), false);
                        sceneEnvironment?.SuppressAuthoredEffect(effect);
                        UnityEngine.Object.Destroy(effect, 3f);
                        grave0Manager.GetElement<Animator>("GraveHighlightNear").SetBool("On", true);
                        haveHint = true;
                        break;
                    }
            foreach (GameCard card in cardsActivated)
                if (card.p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Grave))
                    if (!card.p.InMyControl())
                    {
                        var effect = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_active_grave_001");
                        effect.transform.SetParent(grave1Manager.GetElement<Transform>("GraveHighlightFar"), false);
                        sceneEnvironment?.SuppressAuthoredEffect(effect);
                        UnityEngine.Object.Destroy(effect, 3f);
                        grave1Manager.GetElement<Animator>("GraveHighlightFar").SetBool("On", true);
                        haveHint = true;
                        break;
                    }
            foreach (GameCard card in cardsActivated)
                if (card.p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Removed))
                    if (card.p.InMyControl())
                    {
                        var effect = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_active_exclude_001");
                        effect.transform.SetParent(grave0Manager.GetElement<Transform>("ExcludeHighlightNear"), false);
                        sceneEnvironment?.SuppressAuthoredEffect(effect);
                        UnityEngine.Object.Destroy(effect, 3f);
                        grave0Manager.GetElement<Animator>("ExcludeHighlightNear").SetBool("On", true);
                        haveHint = true;
                        break;
                    }
            foreach (GameCard card in cardsActivated)
                if (card.p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Removed))
                    if (!card.p.InMyControl())
                    {
                        var effect = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_active_exclude_001");
                        effect.transform.SetParent(grave1Manager.GetElement<Transform>("ExcludeHighlightFar"), false);
                        sceneEnvironment?.SuppressAuthoredEffect(effect);
                        UnityEngine.Object.Destroy(effect, 3f);
                        grave1Manager.GetElement<Animator>("ExcludeHighlightFar").SetBool("On", true);
                        haveHint = true;
                        break;
                    }
            foreach (GameCard card in cardsActivated)
                if ((card.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Extra) > 0)
                    if (card.p.InMyControl())
                    {
                        var effect = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_active_Exdeck_001");
                        effect.transform.SetParent(myExtra.transform, false);
                        effect.transform.position = Utility.Tools.GetDeckModelTopPosition(myExtra);
                        foreach (UI.PlaceSelector place in places)
                            place.ShowHint((uint)MDPro3.Duel.YGOSharp.CardLocation.Extra, 0u);
                        UnityEngine.Object.Destroy(effect, 3f);
                        haveHint = true;
                        break;
                    }
            foreach (GameCard card in cardsActivated)
                if ((card.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Extra) > 0)
                    if (!card.p.InMyControl())
                    {
                        var effect = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_active_Exdeck_001");
                        effect.transform.SetParent(opExtra.transform, false);
                        effect.transform.position = Utility.Tools.GetDeckModelTopPosition(opExtra);
                        foreach (UI.PlaceSelector place in places)
                            place.ShowHint((uint)MDPro3.Duel.YGOSharp.CardLocation.Extra, 1u);
                        UnityEngine.Object.Destroy(effect, 3f);
                        haveHint = true;
                        break;
                    }
            foreach (GameCard card in cardsActivated)
                if ((card.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Deck) > 0)
                    if (card.p.controller == 0)
                    {
                        var effect = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_active_Exdeck_001");
                        effect.transform.SetParent(myDeck.transform, false);
                        effect.transform.position = Utility.Tools.GetDeckModelTopPosition(myDeck);
                        foreach (UI.PlaceSelector place in places)
                            place.ShowHint((uint)MDPro3.Duel.YGOSharp.CardLocation.Deck, 0u);
                        UnityEngine.Object.Destroy(effect, 3f);
                        haveHint = true;
                        break;
                    }
            foreach (GameCard card in cardsActivated)
                if ((card.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Deck) > 0)
                    if (!card.p.InMyControl())
                    {
                        var effect = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_active_Exdeck_001");
                        effect.transform.SetParent(opDeck.transform, false);
                        effect.transform.position = Utility.Tools.GetDeckModelTopPosition(opDeck);
                        foreach (UI.PlaceSelector place in places)
                            place.ShowHint((uint)MDPro3.Duel.YGOSharp.CardLocation.Deck, 1u);
                        UnityEngine.Object.Destroy(effect, 3f);
                        haveHint = true;
                        break;
                    }
            if (haveHint)
                Runtime.AudioManager.PlaySE("SE_DUEL_ACTIVE_POSSIBLE");
        }

        public void ClearResponse()
        {

            var myMaxDeck = Core.GetLocationCardCount(MDPro3.Duel.YGOSharp.CardLocation.Deck, 0);
            var opMaxDeck = Core.GetLocationCardCount(MDPro3.Duel.YGOSharp.CardLocation.Deck, 1);
            foreach (GameCard card in cards)
            {
                card.effects.Clear();
                card.ClearButtons();
                if (card.forSelect)
                {
                    card.forSelect = false;
                    if (card.p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Deck))
                    {
                        if (deckReserved)
                        {
                            if (card.p.controller == 0 && card.p.sequence != myMaxDeck - 1)
                                card.EraseData();
                            if (card.p.controller == 1 && card.p.sequence != opMaxDeck - 1)
                                card.EraseData();
                        }
                        else
                        {
                            card.EraseData();
                        }
                    }
                }
            }

            foreach (UI.PlaceSelector place in places)
            {
                place.StopResponse();
                place.HideHint();
                place.ClearButtons();
            }
            foreach (GraveBehaviour grave in graves)
            {
                grave.ClearGraveButtons();
                grave.ClearExcludeButtons();
            }

            UI.PhaseButtonHandler.battlePhase = false;
            UI.PhaseButtonHandler.main2Phase = false;
            UI.PhaseButtonHandler.endPhase = false;
            UI.PhaseButtonHandler.CloseHint();

            CloseBgHint();
            FieldSelectReset();
            ES_selectHint = string.Empty;

        }

        private void CloseBgHint()
        {
            if (grave0Manager == null || grave1Manager == null)
                return;

            grave0Manager.GetElement<Animator>("GraveHighlightNear").SetBool("On", false);
            grave0Manager.GetElement<Animator>("ExcludeHighlightNear").SetBool("On", false);
            grave1Manager.GetElement<Animator>("GraveHighlightFar").SetBool("On", false);
            grave1Manager.GetElement<Animator>("ExcludeHighlightFar").SetBool("On", false);
        }

        private void FieldSelectReset()
        {
            foreach (UI.PlaceSelector place in places)
                place.StopResponse();
            if (btnConfirm != null) btnConfirm.Hide();
            if (btnCancel != null) btnCancel.Hide();
            UI.ServantUI.OcgCoreUI ui = Core.GetUI<UI.ServantUI.OcgCoreUI>();
            if (ui != null) ui.CloseHint();
        }

        public void Dispose()
        {
            RetireDeckPresentation();
            if (finalBlowFeedback != null) finalBlowFeedback.Finish();
            finalBlowFeedback = null;
            loaded = false;
            if (sceneEnvironment != null) sceneEnvironment.Retire();
            sceneEnvironment = null;
            if (fieldProps != null) fieldProps.Retire();
            fieldProps = null;
            foreach (GameCard card in cards)
                if (card != null) card.RetireAnimations();
            foreach(var go in turnEndDeleteObjects)
                UnityEngine.Object.Destroy(go);
            turnEndDeleteObjects.Clear();
            foreach (var go in allGameObjects)
                UnityEngine.Object.Destroy(go);
            allGameObjects.Clear();
            bgPhase0 = 0;
            bgPhase1 = 0;
            ResetPremiumMateStates();
            mate0 = null;
            mate1 = null;
            myDeck = null;
            myExtra = null;
            opDeck = null;
            opExtra = null;
            field0Manager = null;
            field1Manager = null;
            grave0Manager = null;
            grave1Manager = null;
            stand0Manager = null;
            stand1Manager = null;
            phaseButton = null;
            timerHandler = null;
            playableGuide = null;
            places.Clear();
            graves.Clear();
        }

        public async UniTask ShowDuelResultText(string text)
        {
            var go = Runtime.ABLoader.LoadMasterDuelGameObject(text);
            allGameObjects.Add(go);
            var director = go.GetComponent<PlayableDirector>();
            var feedback = UI.Client.FightSceneCombatFeedback.Result(this, go, text);
            try { await director.WaitAsync(); }
            finally
            {
                if (feedback != null) feedback.Finish();
                UnityEngine.Object.Destroy(go);
            }
        }

        public void DuelEndEvent()
        {
            if(timerHandler != null)
                timerHandler.DuelEnd();
            if(playableGuide != null)
                playableGuide.End();
        }

        public async UniTask PlayCommonSpecialWin(int[] codes)
        {
            var count = codes.Length;
            var go = await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/SpecialWin/SpecialWinCommonCard0" + count, false, true);
            allGameObjects.Add(go);
            Presentation.Elements.ElementObjectManager mner = go.GetComponent<Presentation.Elements.ElementObjectManager>();
            foreach (var child in mner.transform.GetComponentsInChildren<Transform>(true))
                if (child.name == "White")
                    child.gameObject.SetActive(false);

            _ = Runtime.Program.instance.texture_.LoadDummyCard(mner.GetElement<Presentation.Elements.ElementObjectManager>("DummyCard01"), codes[0], 0, true);
            mner.GetElement<Presentation.Elements.ElementObjectManager>("DummyCard01").GetElement<Renderer>("DummyCardModel_front").material.renderQueue = 4000;
            if (count > 1)
                _ = Runtime.Program.instance.texture_.LoadDummyCard(mner.GetElement<Presentation.Elements.ElementObjectManager>("DummyCard02"), codes[1], 0, true);
            if (count > 2)
                _ = Runtime.Program.instance.texture_.LoadDummyCard(mner.GetElement<Presentation.Elements.ElementObjectManager>("DummyCard03"), codes[2], 0, true);
            if (count > 3)
                _ = Runtime.Program.instance.texture_.LoadDummyCard(mner.GetElement<Presentation.Elements.ElementObjectManager>("DummyCard04"), codes[3], 0, true);
            if (count > 4)
                _ = Runtime.Program.instance.texture_.LoadDummyCard(mner.GetElement<Presentation.Elements.ElementObjectManager>("DummyCard05"), codes[4], 0, true);

            var director = mner.GetComponent<PlayableDirector>();
            director.Play();
            await director.WaitAsync();
            UnityEngine.Object.Destroy(go);
        }

        public async UniTask PlaySpecialWin(string path, Action<Presentation.Elements.ElementObjectManager> action = null)
        {
            var go = await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/SpecialWin/" + path, false, true);
            allGameObjects.Add(go);
            Presentation.Elements.ElementObjectManager manager = go.GetComponent<Presentation.Elements.ElementObjectManager>();
            action?.Invoke(manager);
            var director = go.GetComponent<PlayableDirector>();
            director.Play();
            await director.WaitAsync();
            UnityEngine.Object.Destroy(go);
        }

        public void ShowBGEnd(DuelResult result)
        {
            void HeroWin()
            {
                field0Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.EndWin);
                if (CanPlayMateAction(0, mate0))
                    mate0.Play(Presentation.Mate.MateAction.Victory);
            }

            void HeroLose()
            {
                bgPhase0 = 4;
                var seLabel = "SE_FIELD_MAT" + field0Manager.name.Substring(4, 3) + "_PHASE4_P";
                field0Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase1ToPhase2);
                field0Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase2ToPhase3);
                field0Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase3ToPhase4);
                field0Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase4ToEnd, seLabel);
                field0Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.EndLose);

                if (stand0Manager != null)
                    stand0Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase4ToEnd);
                if (CanPlayMateAction(0, mate0))
                    mate0.Play(Presentation.Mate.MateAction.Defeat);
            }

            void RivalWin()
            {
                field1Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.EndWin);
                if (CanPlayMateAction(1, mate1))
                    mate1.Play(Presentation.Mate.MateAction.Victory);
            }

            void RivalLose()
            {
                bgPhase1 = 4;
                var seLabel = "SE_FIELD_MAT" + field0Manager.name.Substring(4, 3) + "_PHASE4_R";
                field1Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase1ToPhase2);
                field1Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase2ToPhase3);
                field1Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase3ToPhase4);
                field1Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase4ToEnd, seLabel);
                field1Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.EndLose);

                if (stand1Manager != null)
                    stand1Manager.PlayAnimatorTrigger(Presentation.Fields.BgEffectSettingInner.TriggerLabelDefine.DamagePhase4ToEnd);
                if (CanPlayMateAction(1, mate1))
                    mate1.Play(Presentation.Mate.MateAction.Defeat);
            }

            if (result == DuelResult.Win)
            {
                HeroWin();
                RivalLose();
            }
            else if (result == DuelResult.Lose)
            {
                HeroLose();
                RivalWin();
            }
            else if(result == DuelResult.Draw)
            {
                HeroLose();
                RivalLose();
            }
        }

        public async UniTask ShowChainStack()
        {
            int chain = cardsInChain.Count;
            if (chain == 1)
                return;
            if (!NeedChainAnimation())
                return;

            GameObject animation;
            if (chain < 3)
                animation = Runtime.ABLoader.LoadMasterDuelGameObject("DuelChainStack01");
            else
            {
                animation = Runtime.ABLoader.LoadMasterDuelGameObject("DuelChainStack02");
                DOTween.To(v => { }, 0, 0, 0.0166f).OnComplete(() =>
                {
                    Runtime.AudioManager.PlaySE("SE_DUELCHAIN_STACK02");
                });
                DOTween.To(v => { }, 0, 0, 0.767f).OnComplete(() =>
                {
                    if (chain == 3)
                        Runtime.AudioManager.PlaySE("SE_DUEL_CHAIN_NUMEFF_01");
                    else if (chain == 4)
                        Runtime.AudioManager.PlaySE("SE_DUEL_CHAIN_NUMEFF_02");
                    else
                        Runtime.AudioManager.PlaySE("SE_DUEL_CHAIN_NUMEFF_03");
                });
            }
            var director = animation.GetComponent<PlayableDirector>();
            Presentation.Elements.ElementObjectManager manager = animation.GetComponent<Presentation.Elements.ElementObjectManager>();

            if(chain >= 4)
            {
                director.GetTrackAsset("LCardLightSetScaleC03").muted = true;
                director.GetTrackAsset("LCardLightSetScaleC04").muted = false;
                director.GetTrackAsset("RCardLightSetScaleC03").muted = true;
                director.GetTrackAsset("RCardLightSetScaleC04").muted = false;
            }

            Presentation.Elements.ElementObjectManager targetCardD;
            if (controllerInChain[chain - 1] == 0)
            {
                targetCardD = manager.GetElement<Presentation.Elements.ElementObjectManager>("DummyChainCardDL");
                manager.GetElement("ChainCardSetDROffset").SetActive(false);
                ChangeChainNumber(
                    manager.GetElement<SpriteRenderer>("ChainNumDL_Digit"),
                    manager.GetElement<SpriteRenderer>("ChainNumDL_Ones"),
                    manager.GetElement<SpriteRenderer>("ChainNumDL_Tens"),
                    chain);
            }
            else
            {
                targetCardD = manager.GetElement<Presentation.Elements.ElementObjectManager>("DummyChainCardDR");
                manager.GetElement("ChainCardSetDLOffset").SetActive(false);
                ChangeChainNumber(
                    manager.GetElement<SpriteRenderer>("ChainNumDR_Digit"),
                    manager.GetElement<SpriteRenderer>("ChainNumDR_Ones"),
                    manager.GetElement<SpriteRenderer>("ChainNumDR_Tens"),
                    chain);
            }
            _ = Runtime.Program.instance.texture_.LoadDummyCard(targetCardD, codesInChain[chain - 1], 0, true);

            if (controllerInChain[chain - 1] == controllerInChain[chain - 2])
            {
                manager.GetElement("ChainStraightCLtoDR").SetActive(false);
                manager.GetElement("ChainStraightCRtoDL").SetActive(false);
            }
            else
            {
                if (controllerInChain[chain - 1] == 0)
                    manager.GetElement("ChainStraightCLtoDR").SetActive(false);
                else
                    manager.GetElement("ChainStraightCRtoDL").SetActive(false);
            }
            Presentation.Elements.ElementObjectManager targetCardC;
            if (controllerInChain[chain - 2] == 0)
            {
                targetCardC = manager.GetElement<Presentation.Elements.ElementObjectManager>("DummyChainCardCL");
                manager.GetElement("ChainCardSetCROffset").SetActive(false);
                ChangeChainNumber(
                    manager.GetElement<SpriteRenderer>("ChainNumCL_Digit"),
                    manager.GetElement<SpriteRenderer>("ChainNumCL_Ones"),
                    manager.GetElement<SpriteRenderer>("ChainNumCL_Tens"),
                    chain - 1);
            }
            else
            {
                targetCardC = manager.GetElement<Presentation.Elements.ElementObjectManager>("DummyChainCardCR");
                manager.GetElement("ChainCardSetCLOffset").SetActive(false);
                ChangeChainNumber(
                    manager.GetElement<SpriteRenderer>("ChainNumCR_Digit"),
                    manager.GetElement<SpriteRenderer>("ChainNumCR_Ones"),
                    manager.GetElement<SpriteRenderer>("ChainNumCR_Tens"),
                    chain - 1);
            }
            _ = Runtime.Program.instance.texture_.LoadDummyCard(targetCardC, codesInChain[chain - 2], 0, true);

            if (chain > 2)
            {
                if (controllerInChain[chain - 2] == controllerInChain[chain - 3])
                {
                    manager.GetElement("ChainStraightBLtoCR").SetActive(false);
                    manager.GetElement("ChainStraightBRtoCL").SetActive(false);
                }
                else
                {
                    if (controllerInChain[chain - 2] == 0)
                        manager.GetElement("ChainStraightBLtoCR").SetActive(false);
                    else
                        manager.GetElement("ChainStraightBRtoCL").SetActive(false);
                }
                Presentation.Elements.ElementObjectManager targetCardB;
                if (controllerInChain[chain - 3] == 0)
                {
                    targetCardB = manager.GetElement<Presentation.Elements.ElementObjectManager>("DummyChainCardBL");
                    manager.GetElement("ChainCardSetBROffset").SetActive(false);
                    ChangeChainNumber(
                        manager.GetElement<SpriteRenderer>("ChainNumBL_Digit"),
                        manager.GetElement<SpriteRenderer>("ChainNumBL_Ones"),
                        manager.GetElement<SpriteRenderer>("ChainNumBL_Tens"),
                        chain - 2);
                }
                else
                {
                    targetCardB = manager.GetElement<Presentation.Elements.ElementObjectManager>("DummyChainCardBR");
                    manager.GetElement("ChainCardSetBLOffset").SetActive(false);
                    ChangeChainNumber(
                        manager.GetElement<SpriteRenderer>("ChainNumBR_Digit"),
                        manager.GetElement<SpriteRenderer>("ChainNumBR_Ones"),
                        manager.GetElement<SpriteRenderer>("ChainNumBR_Tens"),
                        chain - 2);
                }
                _ = Runtime.Program.instance.texture_.LoadDummyCard(targetCardB, codesInChain[chain - 3], 0, true);

                if (chain > 3)
                {
                    if (controllerInChain[chain - 3] == controllerInChain[chain - 4])
                    {
                        manager.GetElement("ChainStraightALtoBR").SetActive(false);
                        manager.GetElement("ChainStraightARtoBL").SetActive(false);
                    }
                    else
                    {
                        if (controllerInChain[chain - 3] == 0)
                            manager.GetElement("ChainStraightALtoBR").SetActive(false);
                        else
                            manager.GetElement("ChainStraightARtoBL").SetActive(false);
                    }
                    Presentation.Elements.ElementObjectManager targetCardA;
                    if (controllerInChain[chain - 4] == 0)
                    {
                        targetCardA = manager.GetElement<Presentation.Elements.ElementObjectManager>("DummyChainCardAL");
                        manager.GetElement("ChainCardSetAROffset").SetActive(false);
                        ChangeChainNumber(
                            manager.GetElement<SpriteRenderer>("ChainNumAL_Digit"),
                            manager.GetElement<SpriteRenderer>("ChainNumAL_Ones"),
                            manager.GetElement<SpriteRenderer>("ChainNumAL_Tens"),
                            chain - 3);
                    }
                    else
                    {
                        targetCardA = manager.GetElement<Presentation.Elements.ElementObjectManager>("DummyChainCardAR");
                        manager.GetElement("ChainCardSetALOffset").SetActive(false);
                        ChangeChainNumber(
                            manager.GetElement<SpriteRenderer>("ChainNumAR_Digit"),
                            manager.GetElement<SpriteRenderer>("ChainNumAR_Ones"),
                            manager.GetElement<SpriteRenderer>("ChainNumAR_Tens"),
                            chain - 3);
                    }
                    _ = Runtime.Program.instance.texture_.LoadDummyCard(targetCardA, codesInChain[chain - 4], 0, true);
                }
                else
                {
                    manager.GetElement("ChainStraightALtoBR").SetActive(false);
                    manager.GetElement("ChainStraightARtoBL").SetActive(false);
                    manager.GetElement("ChainCardSetALOffset").SetActive(false);
                    manager.GetElement("ChainCardSetAROffset").SetActive(false);
                }
            }

            await director.WaitAsync();
            UnityEngine.Object.Destroy(animation);
        }

        public async UniTask ShowChainResolve()
        {
            var chain = chainSolvingIndex;
            if (cardsInChain.Count == 1)
                return;
            if (!NeedChainAnimation())
                return;

            GameObject animation;
            if (chain == 1)
                animation = Runtime.ABLoader.LoadMasterDuelGameObject("DuelChainResolve01");
            else if (chain == 2)
                animation = Runtime.ABLoader.LoadMasterDuelGameObject("DuelChainResolve02");
            else
                animation = Runtime.ABLoader.LoadMasterDuelGameObject("DuelChainResolve03");

            var director = animation.GetComponent<PlayableDirector>();
            Presentation.Elements.ElementObjectManager manager = animation.GetComponent<Presentation.Elements.ElementObjectManager>();

            Presentation.Elements.ElementObjectManager targetCardD;
            if (controllerInChain[chain - 1] == 0)
            {
                targetCardD = manager.GetElement<Presentation.Elements.ElementObjectManager>("DummyChainCardDL");
                manager.GetElement("ChainCardSetDROffset").SetActive(false);
                ChangeChainNumber(
                    manager.GetElement<SpriteRenderer>("ChainNumDL_Digit"),
                    manager.GetElement<SpriteRenderer>("ChainNumDL_Ones"),
                    manager.GetElement<SpriteRenderer>("ChainNumDL_Tens"),
                    chain);
            }
            else
            {
                targetCardD = manager.GetElement<Presentation.Elements.ElementObjectManager>("DummyChainCardDR");
                manager.GetElement("ChainCardSetDLOffset").SetActive(false);
                ChangeChainNumber(
                    manager.GetElement<SpriteRenderer>("ChainNumDR_Digit"),
                    manager.GetElement<SpriteRenderer>("ChainNumDR_Ones"),
                    manager.GetElement<SpriteRenderer>("ChainNumDR_Tens"),
                    chain);
            }
            _ = Runtime.Program.instance.texture_.LoadDummyCard(targetCardD, codesInChain[chain - 1], 0, true);

            if (chain > 1)
            {
                if (chain != cardsInChain.Count)
                {
                    manager.GetComponent<PlayableDirector>().time = 0.83f;
                    manager.GetElement("ResolveTextSet").SetActive(false);
                }
                if (controllerInChain[chain - 1] == controllerInChain[chain - 2])
                {
                    manager.GetElement("ChainStraightCLtoDR").SetActive(false);
                    manager.GetElement("ChainStraightCRtoDL").SetActive(false);
                }
                else
                {
                    if (controllerInChain[chain - 1] == 0)
                        manager.GetElement("ChainStraightCLtoDR").SetActive(false);
                    else
                        manager.GetElement("ChainStraightCRtoDL").SetActive(false);
                }

                Presentation.Elements.ElementObjectManager targetCardC;
                if (controllerInChain[chain - 2] == 0)
                {
                    targetCardC = manager.GetElement<Presentation.Elements.ElementObjectManager>("DummyChainCardCL");
                    manager.GetElement("ChainCardSetCROffset").SetActive(false);
                    ChangeChainNumber(
                        manager.GetElement<SpriteRenderer>("ChainNumCL_Digit"),
                        manager.GetElement<SpriteRenderer>("ChainNumCL_Ones"),
                        manager.GetElement<SpriteRenderer>("ChainNumCL_Tens"),
                        chain - 1);
                }
                else
                {
                    targetCardC = manager.GetElement<Presentation.Elements.ElementObjectManager>("DummyChainCardCR");
                    manager.GetElement("ChainCardSetCLOffset").SetActive(false);
                    ChangeChainNumber(
                        manager.GetElement<SpriteRenderer>("ChainNumCR_Digit"),
                        manager.GetElement<SpriteRenderer>("ChainNumCR_Ones"),
                        manager.GetElement<SpriteRenderer>("ChainNumCR_Tens"),
                        chain - 1);
                }
                _ = Runtime.Program.instance.texture_.LoadDummyCard(targetCardC, codesInChain[chain - 2], 0, true);
            }

            if (chain > 2)
            {
                if (controllerInChain[chain - 2] == controllerInChain[chain - 3])
                {
                    manager.GetElement("ChainStraightBLtoCR").SetActive(false);
                    manager.GetElement("ChainStraightBRtoCL").SetActive(false);
                }
                else
                {
                    if (controllerInChain[chain - 2] == 0)
                        manager.GetElement("ChainStraightBLtoCR").SetActive(false);
                    else
                        manager.GetElement("ChainStraightBRtoCL").SetActive(false);
                }

                Presentation.Elements.ElementObjectManager targetCardB;
                if (controllerInChain[chain - 3] == 0)
                {
                    targetCardB = manager.GetElement<Presentation.Elements.ElementObjectManager>("DummyChainCardBL");
                    manager.GetElement("ChainCardSetBROffset").SetActive(false);
                    ChangeChainNumber(
                        manager.GetElement<SpriteRenderer>("ChainNumBL_Digit"),
                        manager.GetElement<SpriteRenderer>("ChainNumBL_Ones"),
                        manager.GetElement<SpriteRenderer>("ChainNumBL_Tens"),
                        chain - 2);
                }
                else
                {
                    targetCardB = manager.GetElement<Presentation.Elements.ElementObjectManager>("DummyChainCardBR");
                    manager.GetElement("ChainCardSetBLOffset").SetActive(false);
                    ChangeChainNumber(
                        manager.GetElement<SpriteRenderer>("ChainNumBR_Digit"),
                        manager.GetElement<SpriteRenderer>("ChainNumBR_Ones"),
                        manager.GetElement<SpriteRenderer>("ChainNumBR_Tens"),
                        chain - 2);
                }
                _ = Runtime.Program.instance.texture_.LoadDummyCard(targetCardB, codesInChain[chain - 3], 0, true);
            }

            await director.WaitAsync();
            UnityEngine.Object.Destroy(animation);
        }

        public async UniTask ShowCardEffectAnimation()
        {
            GameCard card = cardsInChain[chainSolvingIndex - 1];
            card.ResolveChain(chainSolvingIndex);

            if (card.GetData().Id != codesInChain[chainSolvingIndex - 1])
                return;
            if (negatedInChain.Contains(chainSolvingIndex)
                || card.disabledInChain)
                return;
            if (negatedInChain.Contains(chainSolvingIndex)
                || Core.CurrentChainDisabled(chainSolvingIndex)
                || card.negated
                || card.Disabled)
            {
                card.disabledInChain = true;
                await card.AnimationNegate().WaitAsync();
                return;
            }

            if (condition == Condition.Duel
                && !MDPro3.Config.GetBool("DuelEffect",true))
                return;
            if (condition == Condition.Watch
                && !MDPro3.Config.GetBool("WatchEffect", true))
                return;
            if (condition == Condition.Replay
                && !MDPro3.Config.GetBool("ReplayEffect", true))
                return;

            var code = card.GetData().GetOriginalID();
            var cardCode = card.GetData().Id;
            if (cardCode == 83764719)//死者苏生 异画
                code = 83764719;
            if (cardCode == 63166096)//闪刀起动-交闪 异画
                code = 63166096;
            if (cardCode == 32807848)//增援 异画
                code = 32807848;
            if (cardCode == 49238329)//强欲而金满之壶 异画
                code = 49238329;
            if (cardCode == 24224831)//墓穴的指名者 异画
                code = 24224831;
            if (cardCode == 6498707)//融合派兵 异画
                code = 6498707;
            if (cardCode == 35269905)//三战之号 异画
                code = 35269905;

            var targetFolder = Runtime.Program.root + "MasterDuel/Card/" + code.ToString();
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            targetFolder = Path.Combine(Application.dataPath, targetFolder);
#endif

            if (Directory.Exists(targetFolder))
            {
                if (!cardEffectCodes.Contains(code))
                {
                    var prefabs = await Runtime.ABLoader.LoadsFromFolderAsync<PlayableDirector>("MasterDuel/Card/" + code.ToString());
                    if (code == 83764718)
                        prefabs[0].name = "Ef83764718";
                    else if(code == 83764719)
                        prefabs[0].name = "Ef83764719";
                    cardEffects.AddRange(prefabs);
                    cardEffectCodes.Add(code);
                }

                GameObject effect = null;

                // 旋风
                if (code == 5318639)
                {
                    if (card.effectTargets.Count > 0 && card.effectTargets[0].model != null)
                    {
                        Runtime.AudioManager.PlaySE("SE_EV_CYCLONE");
                        effect = GetCardEffectPrefab("Ef04909_OP");
                        effect.transform.position = card.effectTargets[0].model.transform.position;
                        if (card.p.controller != 0)
                            effect.transform.localEulerAngles = new Vector3(0, 180, 0);
                    }
                    else
                        return;
                }
                // 月女神之镞
                else if (code == 2263869)
                {
                    if (card.effectTargets.Count > 0 && card.effectTargets[0].model != null)
                    {
                        Runtime.AudioManager.PlaySE("SE_EV_ULTIMATE_SLAYER");
                        effect = GetCardEffectPrefab("Ef17469");
                    }
                    else
                        return;
                }
                // 雷击
                else if (code == 12580477)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_RAIGEKI");
                    effect = GetCardEffectPrefab(card.p.InMyControl() ? "Ef04343_Far" : "Ef04343_Near");
                    DOTween.To(v => { }, 0, 0, 0.4f).OnComplete(() =>
                    {
                        Runtime.CameraManager.ShakeCamera(true);
                    });
                }
                // 灰流丽
                else if (code == 14558127)
                {
                    if (chainSolvingIndex > 1)
                    {
                        Runtime.AudioManager.PlaySE("SE_EV_ASH_BLOSSOM_v2");
                        effect = GetCardEffectPrefab("Ef03891");
                        effect.transform.localPosition = GameCard.GetCardPosition(cardsInChain[chainSolvingIndex - 2].p);
                    }
                    else
                        return;
                }
                // 鹰身女妖的羽毛扫
                else if (code == 18144506)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_HARPIESFEATHER_DUSTER_3D");
                    effect = GetCardEffectPrefab(card.p.InMyControl() ? "Ef04678" : "Ef04678Op");
                    effect.transform.DestroyChildrenByName("DistPlane");
                }
                // 红色重启
                else if (code == 23002292)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_REDREBOOT");
                    effect = GetCardEffectPrefab("Ef13622");
                }
                // 墓穴的指名者
                else if (code == 24224830)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_CALLED_GRAVE");
                    effect = GetCardEffectPrefab(card.p.InMyControl() ? "Ef13619" : "Ef13619Op");
                }
                // 墓穴的指名者 异画
                else if (code == 24224831)
                {
                    Runtime.AudioManager.PlaySE(card.p.InMyControl() ? "SE_EV_EF21233_R" : "SE_EV_EF21233_P");
                    effect = GetCardEffectPrefab(card.p.InMyControl() ? Utility.Tools.IsAspectRatioWidescreen() ? "Ef21233_Op" : "Ef21233_Op_4x3" : "Ef21233");
                }
                // 禁忌的一滴
                else if (code == 24299458)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_FORBIDDEN_DROPLET");
                    effect = GetCardEffectPrefab(card.p.InMyControl() ? "Ef15299_Far" : "Ef15299_Near");
                }
                // 三战之才
                else if (code == 25311006)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_TRIPLETACTICS_TALENT");
                    effect = GetCardEffectPrefab("Ef15296");
                }
                // 神之宣告
                else if (code == 41420027)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_SOLEMNJUDGMENT");
                    effect = GetCardEffectPrefab("Ef04861");
                }
                // 神圣防护罩 -反射镜力-
                else if (code == 44095762)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_MIRRORFORCE");
                    effect = GetCardEffectPrefab(card.p.InMyControl() ? "Ef04887" : "Ef04887Op");
                }
                // 黑洞
                else if (code == 53129443)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_BLACKHOLE");
                    effect = GetCardEffectPrefab("Ef04342");
                }
                // 冥王结界波
                else if (code == 54693926)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_DARKRULER_NOMORE");
                    effect = GetCardEffectPrefab(card.p.InMyControl() ? "Ef14742" : "Ef14742Op");
                }
                // 王宫的敕命
                else if (code == 61740673)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_IMPERIAL_ORDER");
                    effect = GetCardEffectPrefab("Ef04960");
                }
                // 魔法筒
                else if (code == 62279055)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_MAGIC_CYLINDER");
                    effect = GetCardEffectPrefab(card.p.InMyControl() ? "Ef05124_near" : "Ef05124_far");
                    Presentation.Effects.ScreenEffect se = effect.AddComponent<Presentation.Effects.ScreenEffect>();
                    se.cameraViewType = Presentation.Effects.ScreenEffect.ViewType.View3D;
                    se.useMainCameraSetting = true;

                    //Tools.ChangeLayer(effect, "Default");
                }
                // 千把刀
                else if (code == 63391643)
                {
                    if (card.effectTargets.Count > 0 && card.effectTargets[0].model != null)
                    {
                        Runtime.AudioManager.PlaySE("SE_EV_THOUSANDKNIVES");
                        effect = GetCardEffectPrefab("Ef05166");
                        effect.transform.position = card.effectTargets[0].model.transform.position;
                        if (!card.p.InMyControl())
                            effect.transform.localEulerAngles = new Vector3(0, 180, 0);
                    }
                    else
                        return;
                }
                // 抹杀之指名者
                else if (code == 65681983)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_CROSSOUT_DESIGNATOR");
                    effect = GetCardEffectPrefab(card.p.InMyControl() ? "Ef14627_Far" : "Ef14627_Near");
                }
                // 光之护封剑
                else if (code == 72302403)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_GOFUKEN");
                    effect = GetCardEffectPrefab(card.p.InMyControl() ? "Ef04354" : "Ef04354Op");
                }
                // 封印之黄金柜
                else if (code == 75500286)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_GOLD_SARCOPHAGUS");
                    effect = GetCardEffectPrefab("Ef06161");
                }
                // 死者苏生
                else if (code == 83764718 || code == 83764719)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_MONSTER_REBORN");
                    effect = GetCardEffectPrefab("Ef" + code);
                }
                // 闪刀起动-交闪
                else if (code == 63166095 || code == 63166096)
                {
                    nextMoveActionDuration = 2.2f + 5f;
                    nextMoveNeedCode = true;
                    nextMoveAction = (code2) =>
                    {
                        effect = GetCardEffectPrefab(code == 63166095 ? "Ef13671" : "Ef03434");
                        Presentation.Elements.ElementObjectManager manager = effect.GetComponent<Presentation.Elements.ElementObjectManager>();
                        nextMoveManager = manager;
                        var renderer = manager.GetElement<Renderer>("SummonPosDummy");
                        _ = Runtime.Program.instance.texture_.LoadCardToRendererWithMaterialAsync(renderer, code2, true);
                        UnityEngine.Object.Destroy(effect, 2.2f);
                        addingCardToMyHand = true;
                    };

                    nextEventAction = () =>
                    {
                        if (nextMoveManager == null)
                            return;

                        var target = nextMoveManager.GetElement<Transform>("DummyCard01");
                        GameCard card = lastMoveCard;
                        card.model.SetActive(true);
                        card.ResetModelRotation();
                        card.model.transform.position = target.position;
                        card.model.transform.eulerAngles = new Vector3(-target.eulerAngles.x, 0f, 0f);

                        nextMoveAction = null;
                        nextEventAction = null;
                        nextMoveManager = null;
                        _ = card.MoveAsync(card.p, false, 0f, 0.7f).ContinueWith(() =>
                        {
                            NoMoreWait = true;
                            addingCardToMyHand = false;
                        });
                    };

                    return;
                }
                // 大风暴
                else if (code == 19613556)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_HEAVY_STORM");
                    effect = GetCardEffectPrefab("Ef04891");
                }
                // 天霆号 阿宙斯
                else if (code == 90448279)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_AZEUS");
                    effect = GetCardEffectPrefab("Ef15524");
                }
                // 增援
                else if (code == 32807846)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_039_NORMAL");
                    effect = GetCardEffectPrefab("Ef05328");
                }
                // 增援 异画
                else if (code == 32807848)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_039_SPECIAL");
                    effect = GetCardEffectPrefab("Ef20040");
                }
                // 幽鬼兔
                else if (code == 59438930)
                {
                    if (chainSolvingIndex > 1)
                    {
                        GameCard targetCard = cardsInChain[chainSolvingIndex - 2];
                        if (!targetCard.p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Onfield))
                            return;
                        Runtime.AudioManager.PlaySE("SE_EV_038_NORMAL");
                        effect = GetCardEffectPrefab("Ef11708");
                        Presentation.Elements.ElementObjectManager manager = effect.GetComponent<Presentation.Elements.ElementObjectManager>();
                        var cardEffect = manager.GetElement<Transform>("EffectOffset");
                        cardEffect.localPosition = GameCard.GetCardPosition(targetCard.p);
                        cardEffect.localEulerAngles = GameCard.GetCardRotation(targetCard.p);
                        cardEffect.localScale = GameCard.GetCardScale(targetCard.p);
                        manager.GetNestedElement<MeshRenderer>("CardOffset/DummyCard/DummyCardModel_front")
                            .material = targetCard.GetMaterial();
                    }
                    else
                        return;
                }
                // 欢聚友伴·抖抖海月水母
                else if (code == 84192580)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_040_NORMAL");
                    effect = GetCardEffectPrefab("Ef20206_Act01");
                    Presentation.Elements.ElementObjectManager manager = effect.GetComponent<Presentation.Elements.ElementObjectManager>();
                    UnityEngine.Object.Destroy(manager.GetElement(card.p.InMyControl() ? "Hand01" : "EnHand01"));
                }
                // 欢聚友伴·茸茸长尾山雀
                else if (code == 42141493)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_041_NORMAL");
                    effect = GetCardEffectPrefab("Ef20500_Act01");
                    Presentation.Elements.ElementObjectManager manager = effect.GetComponent<Presentation.Elements.ElementObjectManager>();

                    if (card.p.InMyControl())
                    {
                        UnityEngine.Object.Destroy(manager.GetElement("MainDeck"));
                        UnityEngine.Object.Destroy(manager.GetElement("ExDeck"));
                        DuelEffectUtil.SetDeckModelAppearance(manager.GetElement<Presentation.Elements.ElementObjectManager>("EnMainDeck")
                            , Core.GetLocationCardCount(MDPro3.Duel.YGOSharp.CardLocation.Deck, 1), opDeck);
                        DuelEffectUtil.SetDeckModelAppearance(manager.GetElement<Presentation.Elements.ElementObjectManager>("EnExDeck")
                            , Core.GetLocationCardCount(MDPro3.Duel.YGOSharp.CardLocation.Extra, 1), opExtra);
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(manager.GetElement("EnMainDeck"));
                        UnityEngine.Object.Destroy(manager.GetElement("EnExDeck"));
                        DuelEffectUtil.SetDeckModelAppearance(manager.GetElement<Presentation.Elements.ElementObjectManager>("MainDeck")
                            , Core.GetLocationCardCount(MDPro3.Duel.YGOSharp.CardLocation.Deck, 0), myDeck);
                        DuelEffectUtil.SetDeckModelAppearance(manager.GetElement<Presentation.Elements.ElementObjectManager>("ExDeck")
                            , Core.GetLocationCardCount(MDPro3.Duel.YGOSharp.CardLocation.Extra, 0), myExtra);
                    }
                }
                // 欢聚友伴·喵喵豹猫
                else if (code == 87126721)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_042_NORMAL");
                    effect = GetCardEffectPrefab("Ef20764_Act01");

                    foreach (var sr in effect.transform.GetComponentsInChildren<SpriteRenderer>(true))
                        sr.maskInteraction = SpriteMaskInteraction.None;

                    if (card.p.InMyControl())
                        UnityEngine.Object.Destroy(effect.transform.GetChildByName("GraveSet").gameObject);
                    else
                        UnityEngine.Object.Destroy(effect.transform.GetChildByName("EnGraveSet").gameObject);
                }
                // 灵王的波动
                else if (code == 40366667)
                {
                    if (chainSolvingIndex > 1)
                    {
                        nextNegateAction_Additional = () =>
                        {
                            Runtime.AudioManager.PlaySE("SE_EV_044_NORMAL");
                            effect = GetCardEffectPrefab("Ef20555");
                            effect.transform.DestroyChildByName("BG");
                            Presentation.Elements.ElementObjectManager manager = effect.GetComponent<Presentation.Elements.ElementObjectManager>();
                            manager.GetNestedElement("CardOffset/DummyCard").SetActive(false);
                            var targetEff = card.setOverTurn ? "CardOffset/nomalEf" : "CardOffset/handEf";
                            manager.GetNestedElement(targetEff).SetActive(true);
                            nextNegateAction_AdditionalManager = manager;
                            nextNegateAction_AdditionalTime = 0.5f;
                            UnityEngine.Object.Destroy(effect, 2f);
                        };
                    }
                    return;
                }
                // 圣王的粉碎
                else if (code == 97045737)
                {
                    if (chainSolvingIndex > 1)
                    {
                        nextNegateAction_Additional = () =>
                        {
                            Runtime.AudioManager.PlaySE("SE_EV_043_NORMAL");
                            effect = GetCardEffectPrefab("Ef20257");
                            effect.transform.DestroyChildByName("BG");
                            Presentation.Elements.ElementObjectManager manager = effect.GetComponent<Presentation.Elements.ElementObjectManager>();
                            manager.GetElement("CardOffset").SetActive(false);
                            nextNegateAction_AdditionalManager = manager;
                            nextNegateAction_AdditionalTime = 0.8f;
                            UnityEngine.Object.Destroy(effect, 2f);
                        };
                    }
                    return;
                }
                // 小丑与锁鸟
                else if (code == 94145021)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_EF09279_v1");
                    effect = GetCardEffectPrefab("Ef09279_Act01");
                    Presentation.Elements.ElementObjectManager manager = effect.GetComponent<Presentation.Elements.ElementObjectManager>();

                    if (card.p.InMyControl())
                    {
                        UnityEngine.Object.Destroy(manager.GetElement("MainDeck"));
                        DuelEffectUtil.SetDeckModelAppearance(manager.GetElement<Presentation.Elements.ElementObjectManager>("EnMainDeck")
                            , Core.GetLocationCardCount(MDPro3.Duel.YGOSharp.CardLocation.Deck, 1), opDeck);
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(manager.GetElement("EnMainDeck"));
                        DuelEffectUtil.SetDeckModelAppearance(manager.GetElement<Presentation.Elements.ElementObjectManager>("MainDeck")
                            , Core.GetLocationCardCount(MDPro3.Duel.YGOSharp.CardLocation.Deck, 0), myDeck);
                    }
                }
                // 强欲而金满之壶 异画
                else if (code == 49238329)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_EF14144_v2");
                    effect = GetCardEffectPrefab("Ef21234");
                }
                // 融合派兵 异画
                else if (code == 6498707)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_EF22188");
                    effect = GetCardEffectPrefab("Ef22188");
                }
                // 三战之号 异画
                else if (code == 35269905)
                {
                    Runtime.AudioManager.PlaySE("SE_EV_EF22189");
                    effect = GetCardEffectPrefab("Ef22189");
                }

                var director = effect.GetComponent<PlayableDirector>();
                await director.AutoDestroy();
            }
            else
            {
                // 技能抽取
                if (code == 82732705)
                {
                    if (card.model == null)
                        return;

                    Runtime.AudioManager.PlaySE("SE_EV_SKILLDRAIN");
                    var effArea = await Runtime.ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Effects/MagicTrapEffects/fxp_05740_Area", true, true);
                    var effCard = await Runtime.ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Effects/MagicTrapEffects/fxp_05740_Card", true, true);
                    effCard.SetActive(false);
                    effCard.transform.position = card.model.transform.position;
                    await UniTask.WaitForSeconds(0.5f);
                    effCard.SetActive(true);
                    await UniTask.WaitForSeconds(1f);
                    UnityEngine.Object.Destroy(effArea);
                    UnityEngine.Object.Destroy(effCard);
                }
                // 无限泡影
                else if (code == 10045474)
                {
                    if (card.effectTargets.Count == 0 || card.effectTargets[0].model == null)
                        return;

                    var time = 1.5f;

                    Runtime.AudioManager.PlaySE("SE_EV_INFINITE_IMPERMANENCE");
                    var effCard = await Runtime.ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Effects/MagicTrapEffects/fxp_13631_Card", true, true);
                    effCard.transform.position = card.effectTargets[0].model.transform.position;
                    if (card.effectTargets[0].p.InPosition(MDPro3.Duel.YGOSharp.CardPosition.Attack))
                        UnityEngine.Object.Destroy(effCard.transform.GetChild(0).GetChild(1).gameObject);
                    else
                        UnityEngine.Object.Destroy(effCard.transform.GetChild(0).GetChild(0).gameObject);
                    if (card.setOverTurn)
                    {
                        GameObject effArea = await Runtime.ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Effects/MagicTrapEffects/fxp_13631_Area", true, false);
                        GameObject effAreaLoop = await Runtime.ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Effects/MagicTrapEffects/fxp_13631_Area_Loop", true, false);
                        foreach (UI.PlaceSelector place in places)
                        {
                            if (place.InTheSameLine(card.p))
                            {
                                var area = UnityEngine.Object.Instantiate(effArea);
                                area.transform.position = place.transform.position;
                                UnityEngine.Object.Destroy(area, time);
                                var loop = UnityEngine.Object.Instantiate(effAreaLoop);
                                loop.transform.SetParent(place.transform, false);
                                if ((place.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone) > 0)
                                    loop.transform.localScale = new Vector3(1f, 1f, 1.1f);
                                allGameObjects.Add(loop);
                                turnEndDeleteObjects.Add(loop);
                            }
                        }
                    }
                    await UniTask.WaitForSeconds(time);
                    UnityEngine.Object.Destroy(effCard);
                }
                // 闪电风暴
                else if (code == 14532163)
                {
                    var eff = await Runtime.ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Effects/MagicTrapEffects/fxp_14876", true, true);
                    Presentation.Elements.ElementObjectManager manager = eff.GetComponent<Presentation.Elements.ElementObjectManager>();
                    if (card.p.InMyControl())
                    {
                        Runtime.AudioManager.PlaySE("SE_EV_LIGHTNINGSTORM_P");
                        manager.GetElement("NearMonster").SetActive(false);
                    }
                    else
                    {
                        Runtime.AudioManager.PlaySE("SE_EV_LIGHTNINGSTORM_R");
                        manager.GetElement("FarMonster").SetActive(false);
                    }
                    await UniTask.WaitForSeconds(1f);
                    UnityEngine.Object.Destroy(eff);
                }
                // 效果遮蒙者
                else if (code == 97268402)
                {
                    if (card.effectTargets.Count == 0 || card.effectTargets[0].model == null)
                        return;
                    Runtime.AudioManager.PlaySE("SE_EV_EFFECT_VEILER");
                    var eff = await Runtime.ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Effects/MonsterEffectProcess/fxp_mep08933_01", true, true);
                    eff.transform.position = card.effectTargets[0].model.transform.position;
                    await UniTask.WaitForSeconds(1f);
                    UnityEngine.Object.Destroy(eff);
                }
                // 屋敷童
                else if (code == 73642296)
                {
                    if (chainSolvingIndex < 2)
                        return;

                    await Runtime.ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Effects/MonsterEffectProcess/ef13587", true, false);
                    if (!cardEffectCodes.Contains(73642297))
                    {
                        var prefabs = await Runtime.ABLoader.LoadsFromFolderAsync<PlayableDirector>("MasterDuel/Card/73642297");
                        cardEffects.AddRange(prefabs);
                        cardEffectCodes.Add(73642297);
                    }

                    nextNegateAction = () =>
                    {
                        Runtime.AudioManager.PlaySE("SE_EV_GHOSTBELLE");

                        GameCard targetCard = cardsInChain[chainSolvingIndex - 2];
                        var eff = Runtime.ABLoader.LoadFromFolder<ParticleSystem>("MasterDuel/Effects/MonsterEffectProcess/ef13587", true, true);
                        eff.transform.localPosition = GameCard.GetCardPosition(targetCard.p);
                        Utility.Tools.ChangeLayer(eff, "DuelOverlay3D");
                        Runtime.CameraManager.DuelOverlay3DPlus();

                        if (card.GetData().Id == 73642297)
                        {
                            Runtime.AudioManager.PlaySE("SE_EV_037_SPECIAL");
                            var model = GetCardEffectPrefab(card.p.InMyControl() ? "Ef03892" : "Ef03892_Op");
                            model.transform.localPosition = GameCard.GetCardPosition(targetCard.p);
                            Utility.Tools.ChangeLayer(model, "DuelOverlay3D");
                            UnityEngine.Object.Destroy(model, 2f);
                        }
                        DOTween.To(v => { }, 0, 0, 2f).OnComplete(() =>
                        {
                            UnityEngine.Object.Destroy(eff);
                            Runtime.CameraManager.DuelOverlay3DMinus();
                        });
                    };
                }
            }
        }

        private sealed class DeckPresentation
        {
            internal int Generation;
            internal Presentation.Elements.ElementObjectManager[] Models;
            internal GameObject[] Offsets, ShuffleTops;
        }

        private bool TryCaptureDeckPresentation(out DeckPresentation presentation)
        {
            presentation = null;
            if (!IsDeckPresentationCurrent(deckPresentationGeneration)
                || myDeck == null || myExtra == null || opDeck == null || opExtra == null) return false;
            var models = new[] { myDeck, myExtra, opDeck, opExtra };
            var offsets = new GameObject[4]; var shuffleTops = new GameObject[4];
            for (int i = 0; i < models.Length; i++)
            {
                offsets[i] = models[i].GetElement("DeckSetOffset");
                shuffleTops[i] = models[i].GetElement("CardShuffleTop");
                if (offsets[i] == null || shuffleTops[i] == null) return false;
            }
            presentation = new DeckPresentation { Generation = deckPresentationGeneration, Models = models, Offsets = offsets, ShuffleTops = shuffleTops };
            return true;
        }

        private bool IsDeckPresentationCurrent(DeckPresentation presentation)
        {
            if (presentation == null || !IsDeckPresentationCurrent(presentation.Generation)
                || field0Manager == null || field1Manager == null) return false;
            var current = new[] { myDeck, myExtra, opDeck, opExtra };
            for (int i = 0; i < current.Length; i++)
                if (presentation.Models[i] == null || !ReferenceEquals(presentation.Models[i], current[i])
                    || presentation.Offsets[i] == null || presentation.ShuffleTops[i] == null) return false;
            return true;
        }

        public async UniTask ShowAllDeckModelsAsync()
        {
            if (TryCaptureDeckPresentation(out DeckPresentation presentation)) await ShowAllDeckModelsAsync(presentation);
        }

        private UniTask<bool> ShowAllDeckModelsAsync(DeckPresentation presentation)
        {
            if (!IsDeckPresentationCurrent(presentation)) return UniTask.FromResult(false);
            for (int i = 0; i < presentation.Models.Length; i++)
            {
                var director = presentation.Models[i].GetComponent<PlayableDirector>();
                if (director != null)
                {
                    director.playOnAwake = false;
                    director.Stop();
                    director.enabled = false;
                }
                presentation.Models[i].gameObject.SetActive(true);
                presentation.Offsets[i].SetActive(false);
                presentation.ShuffleTops[i].SetActive(true);
            }
            return UniTask.FromResult(true);
        }

        public void ShowTargetLines(Vector3 start, List<GameCard> targets)
        {
            if (targetLine == null)
                return;

            foreach (GameCard card in targets)
            {
                if ((card.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Onfield) > 0)
                {
                    var newLine = UnityEngine.Object.Instantiate(targetLine);
                    newLine.SetActive(true);
                    var line = newLine.transform.GetChild(0).GetComponent<LineRenderer>();
                    var end = card.model.transform.position;
                    var posArr = new Vector3[9]
                    {
                        new(start.x, 1f, start.z),
                        new(start.x + (end.x - start.x) * 0.125f, 5f, start.z + (end.z - start.z) * 0.125f),
                        new(start.x + (end.x - start.x) * 0.25f, 9f, start.z + (end.z - start.z) * 0.25f),
                        new(start.x + (end.x - start.x) * 0.375f, 11f, start.z + (end.z - start.z) * 0.375f),
                        new(start.x + (end.x - start.x) * 0.5f, 12f, start.z + (end.z - start.z) * 0.5f),
                        new(start.x + (end.x - start.x) * 0.625f, 11f, start.z + (end.z - start.z) * 0.625f),
                        new(start.x + (end.x - start.x) * 0.75f, 9f, start.z + (end.z - start.z) * 0.75f),
                        new(start.x + (end.x - start.x) * 0.875f, 5f, start.z + (end.z - start.z) * 0.875f),
                        new(end.x, 1f, end.z),
                    };
                    line.SetPositions(posArr);
                    targetLines.Add(newLine);
                }
            }
        }

        public void HideTargetLines()
        {
            foreach (var line in targetLines)
                UnityEngine.Object.Destroy(line);
            targetLines.Clear();
        }

        public void ShowEquipLine(Vector3 start, Vector3 end)
        {
            if (equipLine == null)
                return;

            var line = equipLine.transform.GetChild(0).GetComponent<LineRenderer>();
            var posArr = new Vector3[9]
            {
                new(start.x, 1f, start.z),
                new(start.x + (end.x - start.x) * 0.125f, 1.5f, start.z + (end.z - start.z) * 0.125f),
                new(start.x + (end.x - start.x) * 0.25f, 2f, start.z + (end.z - start.z) * 0.25f),
                new(start.x + (end.x - start.x) * 0.375f, 2.5f, start.z + (end.z - start.z) * 0.375f),
                new(start.x + (end.x - start.x) * 0.5f, 2.8f, start.z + (end.z - start.z) * 0.5f),
                new(start.x + (end.x - start.x) * 0.625f, 2.5f, start.z + (end.z - start.z) * 0.625f),
                new(start.x + (end.x - start.x) * 0.75f, 2f, start.z + (end.z - start.z) * 0.75f),
                new(start.x + (end.x - start.x) * 0.875f, 1.5f, start.z + (end.z - start.z) * 0.875f),
                new(end.x, 1f, end.z),
            };
            line.SetPositions(posArr);
            equipLine.SetActive(true);
        }

        public void HideEquipLine()
        {
            if (equipLine == null)
                return;
            equipLine.SetActive(false);
        }

        public void ShowAttackLine(Vector3 end, Vector3 start)
        {
            if (attackLine == null)
                return;

            Presentation.Elements.ElementObjectManager lineManager = attackLine.GetComponent<Presentation.Elements.ElementObjectManager>();
            var line1 = lineManager.GetElement<LineRenderer>("arrowlimeRollover");
            var line2 = lineManager.GetElement<LineRenderer>("arrowRollover");
            var posArr = new Vector3[9]
            {
                new(start.x, 5, start.z),
                new(start.x + (end.x - start.x) * 0.125f, 5.8f, start.z + (end.z - start.z) * 0.125f),
                new(start.x + (end.x - start.x) * 0.25f, 6.3f, start.z + (end.z - start.z) * 0.25f),
                new(start.x + (end.x - start.x) * 0.375f, 6.5f, start.z + (end.z - start.z) * 0.375f),
                new(start.x + (end.x - start.x) * 0.5f, 6.5f, start.z + (end.z - start.z) * 0.5f),
                new(start.x + (end.x - start.x) * 0.625f, 6.5f, start.z + (end.z - start.z) * 0.625f),
                new(start.x + (end.x - start.x) * 0.75f, 6.3f, start.z + (end.z - start.z) * 0.75f),
                new(start.x + (end.x - start.x) * 0.875f, 5.8f, start.z + (end.z - start.z) * 0.875f),
                new(end.x, 5, end.z),
            };
            line1.SetPositions(posArr);
            line2.SetPositions(posArr);
            attackLine.SetActive(true);
        }

        public void HideAttackLine()
        {

            if (attackLine == null)
                return;
            attackLine.SetActive(false);
        }

        public void ShowDuelDirectAttackText(int player)
        {
            var effect = Runtime.ABLoader.LoadMasterDuelGameObject("DuelDirectAtk00");
            UI.Client.FightSceneCombatFeedback.DirectAttack(this, effect, player);
            UnityEngine.Object.Destroy(effect, 1f);
            Runtime.AudioManager.PlaySE("SE_DA_TEXT");
        }

        public void ShowDuelFinalBlowText(int player = 0)
        {
            if(duelFinalBlow != null)
                UnityEngine.Object.Destroy(duelFinalBlow.gameObject);
            if (finalBlowFeedback != null) finalBlowFeedback.Finish();
            duelFinalBlow = Runtime.ABLoader.LoadMasterDuelGameObject("DuelFinalBlow").GetComponent<DuelFinalBlow>();
            finalBlowFeedback = UI.Client.FightSceneCombatFeedback.FinalBlow(this, duelFinalBlow.gameObject, player);
        }

        public void HideDuelFinalBlowText()
        {
            if(duelFinalBlow == null) return;
            if (finalBlowFeedback != null) finalBlowFeedback.BeginExit();
            duelFinalBlow.Destroy();
        }

        public bool IsFinalBlow()
        {
            return duelFinalBlow != null;
        }

        public async UniTask<bool> NeedSpecialFinalAttackAsync(GameCard attackCard, Vector3 attackedPosition)
        {
            MDPro3.Duel.YGOSharp.Card data = attackCard.GetData();
            FinalAttackType returnValue = FinalAttackType.Normal;
            if (MDPro3.Settings.Data.FinalAttackBlueEyes.Contains(data.Id) || MDPro3.Settings.Data.FinalAttackBlueEyes.Contains(data.Alias))
                returnValue = FinalAttackType.BlueEyes;
            if (MDPro3.Settings.Data.FinalAttackDarkM.Contains(data.Id) || MDPro3.Settings.Data.FinalAttackDarkM.Contains(data.Alias))
                returnValue = FinalAttackType.DarkM;
            if (MDPro3.Settings.Data.FinalAttackRedEyes.Contains(data.Id) || MDPro3.Settings.Data.FinalAttackRedEyes.Contains(data.Alias))
                returnValue = FinalAttackType.RedEyes;
            if (MDPro3.Settings.Data.FinalAttackObelisk.Contains(data.Id) || MDPro3.Settings.Data.FinalAttackObelisk.Contains(data.Alias))
                returnValue = FinalAttackType.Obelisk;
            if (MDPro3.Settings.Data.FinalAttackRa.Contains(data.Id) || MDPro3.Settings.Data.FinalAttackRa.Contains(data.Alias))
                returnValue = FinalAttackType.Ra;
            if (MDPro3.Settings.Data.FinalAttackSlifer.Contains(data.Id) || MDPro3.Settings.Data.FinalAttackSlifer.Contains(data.Alias))
                returnValue = FinalAttackType.Slifer;

            if (returnValue == FinalAttackType.Normal)
                return false;
            await AnimationFinalAttackAsync(returnValue, attackCard, attackedPosition);
            return true;
        }

        public void FinishDamageEffect()
        {
            Runtime.AudioManager.StopBGM();
            Core.GetUI<UI.ServantUI.OcgCoreUI>().OnNor();

#if UNITY_EDITOR
            Program.instance.timeScaleForEditor = 0.1f;
            DOTween.To(() => Program.instance.timeScaleForEditor, x => Program.instance.timeScaleForEditor = x, 1f, 0.85f).SetEase(Ease.InQuad);
#else
            Runtime.Program.instance.TimeScale = 0.1f;
            DOTween.To(() => Runtime.Program.instance.TimeScale, x => Runtime.Program.instance.TimeScale = x, 1f, 0.85f).SetEase(Ease.InQuad);
#endif

            if (life0 <= 0)
            {
                var hitObj = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_dithit_fin_near_001");
                hitObj.transform.position = new Vector3(0, 15, -25);
                UnityEngine.Object.Destroy(hitObj, 10);
            }
            if (life1 <= 0)
            {
                var hitObj = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_dithit_fin_far_001");
                hitObj.transform.position = new Vector3(0, 15, 25);
                UnityEngine.Object.Destroy(hitObj, 10);
            }
        }

        public void ReleaseTurnObjects()
        {
            foreach(var go in turnEndDeleteObjects)
                UnityEngine.Object.Destroy(go);
            turnEndDeleteObjects.Clear();
        }

        public async UniTask ShowTurnChangeBanner(int player)
        {
            if (turns < 2)
                return;

            var banner = Runtime.ABLoader.LoadMasterDuelGameObject("DuelTurnChange0" + player);
            var director = banner.GetComponent<PlayableDirector>();
            var sceneAnnouncement = UI.Client.FightSceneAnnouncement.Turn(this, banner, player, turns);
            if (sceneAnnouncement != null)
            {
                try { await director.WaitAsync(); }
                finally
                {
                    sceneAnnouncement.Finish();
                    UnityEngine.Object.Destroy(banner);
                }
                return;
            }
            await director.WaitAsync();
            UnityEngine.Object.Destroy(banner);
        }

        public async UniTask ShowPhaseBanner(int player, MDPro3.Duel.YGOSharp.DuelPhase phase)
        {
            if (phase == MDPro3.Duel.YGOSharp.DuelPhase.BattleStep
                || phase == MDPro3.Duel.YGOSharp.DuelPhase.Damage
                || phase == MDPro3.Duel.YGOSharp.DuelPhase.DamageCal
                || phase == MDPro3.Duel.YGOSharp.DuelPhase.Battle
                || phase == MDPro3.Duel.YGOSharp.DuelPhase.Damage)
                return;

            var tail = player == 0 ? "_near" : "_far";
            GameObject banner = phase switch
            {
                MDPro3.Duel.YGOSharp.DuelPhase.Draw => Runtime.ABLoader.LoadMasterDuelGameObject("DuelDrawPhase" + tail),
                MDPro3.Duel.YGOSharp.DuelPhase.Standby => Runtime.ABLoader.LoadMasterDuelGameObject("DuelStanbyPhase" + tail),
                MDPro3.Duel.YGOSharp.DuelPhase.Main1 => Runtime.ABLoader.LoadMasterDuelGameObject("DuelMain01Phase" + tail),
                MDPro3.Duel.YGOSharp.DuelPhase.Main2 => Runtime.ABLoader.LoadMasterDuelGameObject("DuelMain02Phase" + tail),
                MDPro3.Duel.YGOSharp.DuelPhase.End => Runtime.ABLoader.LoadMasterDuelGameObject("DuelEndPhase" + tail),
                _ => Runtime.ABLoader.LoadMasterDuelGameObject("DuelBattlePhase" + tail),
            };

            string se = phase switch
            {
                MDPro3.Duel.YGOSharp.DuelPhase.Draw => $"SE_PHASE{(player == 0 ? string.Empty : "_OPP")}_DRAW",
                MDPro3.Duel.YGOSharp.DuelPhase.Standby => $"SE_PHASE{(player == 0 ? string.Empty : "_OPP")}_STANDBY",
                MDPro3.Duel.YGOSharp.DuelPhase.Main1 => $"SE_PHASE{(player == 0 ? string.Empty : "_OPP")}_MAIN1",
                MDPro3.Duel.YGOSharp.DuelPhase.Main2 => $"SE_PHASE{(player == 0 ? string.Empty : "_OPP")}_MAIN2",
                MDPro3.Duel.YGOSharp.DuelPhase.End => $"SE_PHASE{(player == 0 ? string.Empty : "_OPP")}_END",
                _ => $"SE_PHASE{(player == 0 ? string.Empty : "_OPP")}_BATTLE",
            };

            Runtime.AudioManager.PlaySE(se);
            var director = banner.GetComponent<PlayableDirector>();
            var sceneAnnouncement = UI.Client.FightSceneAnnouncement.Phase(this, banner, player, phase);
            if (sceneAnnouncement != null)
            {
                try { await director.WaitAsync(); }
                finally
                {
                    sceneAnnouncement.Finish();
                    UnityEngine.Object.Destroy(banner);
                }
                return;
            }
            await director.WaitAsync();
            UnityEngine.Object.Destroy(banner);
        }

        public void SetPlayableGuide(bool isMe)
        {
            if (playableGuide == null)
                return;

            playableGuide.Set(isMe);
        }

        public async UniTask PlayShuffleDeckAsync(int player)
        {
            var animator = (player == 0 ? myDeck : opDeck).GetElement<Animator>("CardShuffleTop");
            animator.speed = 2;
            animator.SetTrigger("Shuffle");
            Runtime.CameraManager.Overlay3DReset();
            Runtime.CameraManager.DuelOverlay3DPlus();
            Utility.Tools.ChangeLayer(animator.gameObject, "DuelOverlay3D");
            Runtime.Program.instance.audio_.PlayShuffleSE();
            await UniTask.WaitForSeconds(0.5f);
            animator.SetTrigger("Idle");
            await UniTask.Yield();
            Runtime.CameraManager.DuelOverlay3DMinus();
            Utility.Tools.ChangeLayer(animator.gameObject, "Default");
        }

        public async UniTask PlaySummonPendulum()
        {
            Core.GetUI<UI.ServantUI.OcgCoreUI>().CardDescription.Hide();

            var pendulum = Runtime.ABLoader.LoadMasterDuelGameObject("SummonPendulum01");
            Presentation.Elements.ElementObjectManager manager = pendulum.GetComponent<Presentation.Elements.ElementObjectManager>();
            manager = manager.GetElement<Presentation.Elements.ElementObjectManager>("SummonPendulumShowCard");
            pendulum.transform.SetParent(Runtime.Program.instance.container_3D, false);

            Presentation.Elements.ElementObjectManager card1 = manager.GetElement<Presentation.Elements.ElementObjectManager>("DummyCard01");
            Presentation.Elements.ElementObjectManager card2 = manager.GetElement<Presentation.Elements.ElementObjectManager>("DummyCard02");
            _ = Runtime.Program.instance.texture_.LoadDummyCard(card1, cardsBeTarget[0].GetData().Id, cardsBeTarget[0].p.controller);
            _ = Runtime.Program.instance.texture_.LoadDummyCard(card2, cardsBeTarget[1].GetData().Id, cardsBeTarget[1].p.controller);
            var scale1 = cardsBeTarget[0].GetData().LScale;
            var scale2 = cardsBeTarget[1].GetData().RScale;

            if (scale1 < 10)
            {
                UnityEngine.Object.Destroy(manager.GetElement("LPendulumNum00Ones"));
                UnityEngine.Object.Destroy(manager.GetElement("LPendulumNum00Tens"));
                UnityEngine.Object.Destroy(manager.GetElement("LPendulumNum00OnesA"));
                UnityEngine.Object.Destroy(manager.GetElement("LPendulumNum00TensA"));

                manager.GetElement<MeshRenderer>("LPendulumNum00Digit").material.mainTexture
                    = Runtime.ABLoader.LoadMasterDuelTexture("LPendulumNum0" + scale1);
                manager.GetElement<MeshRenderer>("LPendulumNum00DigitA").material.mainTexture
                    = Runtime.ABLoader.LoadMasterDuelTexture("LPendulumNum0" + scale1);
            }
            else
            {
                UnityEngine.Object.Destroy(manager.GetElement("LPendulumNum00Digit"));
                UnityEngine.Object.Destroy(manager.GetElement("LPendulumNum00DigitA"));

                manager.GetElement<MeshRenderer>("LPendulumNum00Tens").material.mainTexture
                    = Runtime.ABLoader.LoadMasterDuelTexture("LPendulumNum01");
                manager.GetElement<MeshRenderer>("LPendulumNum00TensA").material.mainTexture
                    = Runtime.ABLoader.LoadMasterDuelTexture("LPendulumNum01");
                manager.GetElement<MeshRenderer>("LPendulumNum00Ones").material.mainTexture
                    = Runtime.ABLoader.LoadMasterDuelTexture("LPendulumNum0" + (scale1 - 10));
                manager.GetElement<MeshRenderer>("LPendulumNum00OnesA").material.mainTexture
                    = Runtime.ABLoader.LoadMasterDuelTexture("LPendulumNum0" + (scale1 - 10));
            }

            if (scale2 < 10)
            {
                UnityEngine.Object.Destroy(manager.GetElement("RPendulumNum00Ones"));
                UnityEngine.Object.Destroy(manager.GetElement("RPendulumNum00Tens"));
                UnityEngine.Object.Destroy(manager.GetElement("RPendulumNum00OnesA"));
                UnityEngine.Object.Destroy(manager.GetElement("RPendulumNum00TensA"));

                manager.GetElement<MeshRenderer>("RPendulumNum00Digit").material.mainTexture
                    = Runtime.ABLoader.LoadMasterDuelTexture("RPendulumNum0" + scale2);
                manager.GetElement<MeshRenderer>("RPendulumNum00DigitA").material.mainTexture
                    = Runtime.ABLoader.LoadMasterDuelTexture("RPendulumNum0" + scale2);
            }
            else
            {
                UnityEngine.Object.Destroy(manager.GetElement("RPendulumNum00Digit"));
                UnityEngine.Object.Destroy(manager.GetElement("RPendulumNum00DigitA"));

                manager.GetElement<MeshRenderer>("RPendulumNum00Tens").material.mainTexture
                    = Runtime.ABLoader.LoadMasterDuelTexture("RPendulumNum01");
                manager.GetElement<MeshRenderer>("RPendulumNum00TensA").material.mainTexture
                    = Runtime.ABLoader.LoadMasterDuelTexture("RPendulumNum01");
                manager.GetElement<MeshRenderer>("RPendulumNum00Ones").material.mainTexture
                    = Runtime.ABLoader.LoadMasterDuelTexture("RPendulumNum0" + (scale2 - 10));
                manager.GetElement<MeshRenderer>("RPendulumNum00OnesA").material.mainTexture
                    = Runtime.ABLoader.LoadMasterDuelTexture("RPendulumNum0" + (scale2 - 10));
            }

            if(MasterRule >= 4)
            {
                var scaleSet = Runtime.ABLoader.LoadMasterDuelGameObject("SummonPendulumScaleSet");
                scaleSet.transform.SetParent(Runtime.Program.instance.container_3D);
                if (!cardsBeTarget[0].p.InMyControl())
                    scaleSet.transform.localEulerAngles = new Vector3(0, 180, 0);
                LoopTrackManager ltm = scaleSet.GetComponent<LoopTrackManager>();
                await UniTask.WaitForSeconds(3.5f);
                ltm.StopLoop();
                await UniTask.WaitForSeconds(0.5f);
                UnityEngine.Object.Destroy(scaleSet);
                UnityEngine.Object.Destroy(pendulum);
            }
            else
            {
                await UniTask.WaitForSeconds(4f);
                UnityEngine.Object.Destroy(pendulum);
            }
        }

        public bool HoveringField0()
        {
            if (field0Manager == null)
                return false;
            if (Runtime.UserInput.HoverObject == field0Manager.gameObject)
                return true;
            return false;
        }

        public bool HoveringField1()
        {
            if (field1Manager == null)
                return false;
            if (Runtime.UserInput.HoverObject == field1Manager.gameObject)
                return true;
            return false;
        }

        public void TapField0()
        {
            if (field0Manager == null)
                return;
            if (Time.time - field0TapTime < 1f)
                return;
            field0TapTime = Time.time;
            field0Manager.PlayTapAnimation();
        }

        public void TapField1()
        {
            if (field1Manager == null)
                return;
            if (Time.time - field1TapTime < 1f)
                return;
            field1TapTime = Time.time;
            field1Manager.PlayTapAnimation();
        }

        public bool HoveringMate0()
        {
            if (mate0 == null)
                return false;
            if (Runtime.UserInput.HoverObject == mate0.gameObject)
                return true;
            if (Runtime.UserInput.HoverObject != null && Runtime.UserInput.HoverObject.transform.IsChildOf(mate0.transform))
                return true;
            return false;
        }

        public bool HoveringMate1()
        {
            if (mate1 == null)
                return false;
            if (Runtime.UserInput.HoverObject == mate1.gameObject)
                return true;
            if (Runtime.UserInput.HoverObject != null && Runtime.UserInput.HoverObject.transform.IsChildOf(mate1.transform))
                return true;
            return false;
        }

        public void TapMate0()
        {
            if (!CanPlayMateAction(0, mate0, false))
                return;
            if (Time.time - mate0TapTime < 1f)
                return;
            mate0TapTime = Time.time;
            mate0.Play(Presentation.Mate.MateAction.Tap);
        }

        public void TapMate1()
        {
            if (!CanPlayMateAction(1, mate1, false))
                return;
            if (Time.time - mate1TapTime < 1f)
                return;
            mate1TapTime = Time.time;
            mate1.Play(Presentation.Mate.MateAction.Tap);
        }

        public void PlayMate0Random()
        {
            if (!CanPlayMateAction(0, mate0))
                return;
            if (ShouldSuppressAmbientPremiumMateActions(GetPremiumMateState(0)))
                return;
            if (!mate0Random)
                return;

            mate0.Play(Presentation.Mate.MateAction.Random);
            RestartMateRandomCooldown(0);
        }

        public void PlayMate1Random()
        {
            if (!CanPlayMateAction(1, mate1))
                return;
            if (ShouldSuppressAmbientPremiumMateActions(GetPremiumMateState(1)))
                return;
            if (!mate1Random)
                return;

            mate1.Play(Presentation.Mate.MateAction.Random);
            RestartMateRandomCooldown(1);
        }

        private bool CanPlayMateAction(int side, Presentation.Mate mate, bool skipWhileTransitioning = true)
        {
            if (mate == null)
                return false;
            if (skipWhileTransitioning && IsPremiumMateTransitioning(side))
                return false;
            return true;
        }

        private bool IsPremiumMateTransitioning(int side)
        {
            PremiumMateState state = GetPremiumMateState(side);
            return state != null && state.IsTransitioning;
        }

        private void RestartMateRandomCooldown(int side)
        {
            PremiumMateState premiumState = GetPremiumMateState(side);
            if (ShouldSuppressAmbientPremiumMateActions(premiumState))
            {
                if (side == 0)
                {
                    mate0Random = false;
                    mate0RandomCooldownTween?.Kill();
                    mate0RandomCooldownTween = null;
                }
                else
                {
                    mate1Random = false;
                    mate1RandomCooldownTween?.Kill();
                    mate1RandomCooldownTween = null;
                }
                return;
            }

            Presentation.Mate mate = side == 0 ? mate0 : mate1;
            var delay = mate != null && mate.type == Presentation.Mate.MateType.MasterDuel
                ? UnityEngine.Random.Range(20f, 70f)
                : UnityEngine.Random.Range(8f, 16f);

            if (side == 0)
            {
                mate0Random = false;
                mate0RandomCooldownTween?.Kill();
                mate0RandomCooldownTween = DOTween.To(v => { }, 0, 0, delay).OnComplete(() =>
                {
                    mate0Random = true;
                });
            }
            else
            {
                mate1Random = false;
                mate1RandomCooldownTween?.Kill();
                mate1RandomCooldownTween = DOTween.To(v => { }, 0, 0, delay).OnComplete(() =>
                {
                    mate1Random = true;
                });
            }
        }

        public void SetBgTimeScale(float timeScale)
        {
            if (field0Manager != null)
            {
                Utility.Tools.SetAnimatorTimescale(field0Manager.transform, timeScale);
                Utility.Tools.SetParticleSystemSimulationSpeed(field0Manager.transform, timeScale);
            }
            if (field1Manager != null)
            {
                Utility.Tools.SetAnimatorTimescale(field1Manager.transform, timeScale);
                Utility.Tools.SetParticleSystemSimulationSpeed(field1Manager.transform, timeScale);
            }
            if (phaseButton != null) Utility.Tools.SetAnimatorTimescale(phaseButton.transform, timeScale);
            if (timerHandler != null) Utility.Tools.SetAnimatorTimescale(timerHandler.transform, timeScale);
        }

        public void SetTimeLimit(int player, int time)
        {
            if (timerHandler == null)
                return;
            timerHandler.SetTime(player, time);
        }

        public void TimeContinue()
        {
            if (timerHandler == null)
                return;
            timerHandler.StopPause();
        }

        public void PauseTime()
        {
            if (timerHandler == null)
                return;
            timerHandler.Pause();
        }

        public void ResetFields()
        {
            field0Manager.gameObject.SetActive(false);
            field1Manager.gameObject.SetActive(false);
            field0Manager.gameObject.SetActive(true);
            field1Manager.gameObject.SetActive(true);
            sceneEnvironment?.ApplyVisibility();
        }

        private CancellationTokenSource exTopCts;

        public void SetExDeckTop(GameCard card)
        {
            if(exTopCts != null)
            {
                exTopCts.Cancel();
                exTopCts.Dispose();
            }

            exTopCts = new CancellationTokenSource();

            Presentation.Elements.ElementObjectManager deck = card.p.InMyControl() ? myExtra : opExtra;
            var code = card.GetData().Id;
            var targetMat = UI.MaterialLoader.GetCardMaterial(code, true);
            foreach (var r in deck.transform.GetComponentsInChildren<Renderer>(true))
                if (r.name.Contains("back"))
                    r.material = targetMat;
            _ = SetCardToMaterialAsync(targetMat, code, exTopCts.Token);
        }

        private async UniTask SetCardToMaterialAsync(Material mat, int code, CancellationToken token)
        {
            var tex = await Presentation.CardImageLoader.LoadCardAsync(code, true, token);
            if(mat == null)
                return;
            mat.mainTexture = tex;
        }

        public void UpdateExDeckTop(uint controller)
        {
            GameCard topCard = null;
            var extraCount = Core.GetLocationCardCount(MDPro3.Duel.YGOSharp.CardLocation.Extra, controller);
            foreach (GameCard c in cards)
                if (c.p.InSequence(controller, MDPro3.Duel.YGOSharp.CardLocation.Extra, extraCount - 1))
                {
                    topCard = c;
                    break;
                }

            if (topCard != null && topCard.p.InPosition(MDPro3.Duel.YGOSharp.CardPosition.FaceUp) && topCard.GetData().Id != 0)
            {
                SetExDeckTop(topCard);
                return;
            }

            Presentation.Elements.ElementObjectManager deck = controller == 0 ? myExtra : opExtra;
            var targetMat = controller == 0 ? myProtector : opProtector;
            foreach (var r in deck.transform.GetComponentsInChildren<Renderer>(true))
                if (r.name.Contains("back"))
                    r.material = targetMat;
        }

        public static bool MatIsSpecial(string matName)
        {
            if (matName.StartsWith("Mat_013") || matName.StartsWith("Mat_045"))
                return true;
            return false;
        }

        #endregion

        #region Private

        private T Create<T>(bool addToList = true) where T : MonoBehaviour
        {
            GameObject obj = new(typeof(T).Name);
            T component = obj.AddComponent<T>();
            if (addToList)
                allGameObjects.Add(obj);
            return component;
        }

        private void CreatePlaceSelector(GPS p)
        {
            var go = new GameObject("PlaceSelector");
            UI.PlaceSelector mono = go.AddComponent<UI.PlaceSelector>();
            mono.p = p;
            go.transform.SetParent(Runtime.Program.instance.container_3D);
            allGameObjects.Add(go);
            places.Add(mono);
        }

        private bool NeedChainAnimation()
        {
            bool config = true;
            if (condition == Condition.Duel && MDPro3.Config.Get("DuelChain", "1") == "0")
                config = false;
            else if (condition == Condition.Watch && MDPro3.Config.Get("WatchChain", "1") == "0")
                config = false;
            else if (condition == Condition.Replay && MDPro3.Config.Get("ReplayChain", "1") == "0")
                config = false;
            return config;
        }

        private void ChangeChainNumber(SpriteRenderer digit, SpriteRenderer one, SpriteRenderer ten, int number)
        {
            if (number < 10)
            {
                one.gameObject.SetActive(false);
                ten.gameObject.SetActive(false);
                digit.sprite = Runtime.TextureManager.container.GetChainNumSprite(number);
            }
            else
            {
                digit.gameObject.SetActive(false);
                one.sprite = Runtime.TextureManager.container.GetChainNumSprite(number % 10);
                ten.sprite = Runtime.TextureManager.container.GetChainNumSprite((number / 10) % 10);
            }
        }

        private GameObject GetCardEffectPrefab(string name)
        {
            foreach(var go in cardEffects)
                if (go.name == name)
                    return UnityEngine.Object.Instantiate(go);
            return null;
        }

        private async UniTask AnimationFinalAttackAsync(FinalAttackType type, GameCard attackCard, Vector3 attackedPosition)
        {
            if (type == FinalAttackType.Normal)
                return;

            Sequence sequence;
            if (type == FinalAttackType.BlueEyes)
                sequence = await AnimationFinalAttack_BlueEyes(attackCard, attackedPosition);
            else if (type == FinalAttackType.DarkM)
                sequence = await AnimationFinalAttack_DarkM(attackCard, attackedPosition);
            else if (type == FinalAttackType.RedEyes)
                sequence = await AnimationFinalAttack_RedEyes(attackCard, attackedPosition);
            else if (type == FinalAttackType.Ra)
                sequence = await AnimationFinalAttack_Ra(attackCard, attackedPosition);
            else if (type == FinalAttackType.Slifer)
                sequence = await AnimationFinalAttack_Slifer(attackCard, attackedPosition);
            else// if (type == FinalAttackType.Obelisk)
                sequence = await AnimationFinalAttack_Obelisk(attackCard, attackedPosition);

            await sequence.WaitAsync();
        }

        private async UniTask<Sequence> AnimationFinalAttack_BlueEyes(GameCard attackCard, Vector3 attackedPosition)
        {
            await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/BlueEyes/CardSet", true, false);
            await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Effects.ScreenEffect>("MasterDuel/Timeline/FinalAttack/BlueEyes/ScreenEffect", true, false);
            await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/BlueEyes/Hit" + (attackCard.p.InMyControl() ? "Far" : "Near"), true, false);
            await Runtime.ABLoader.LoadFromFolderAsync<PlayableDirector>("MasterDuel/Timeline/FinalAttack/BlueEyes/Beam", true, false);

            var attackRotation = attackCard.p.InMyControl() ? Vector3.zero : new Vector3(0f, 180f, 0f);
            Runtime.CameraManager.Duel3DOverlayStickWithMain(true);

            var cardSet = await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/BlueEyes/CardSet", true, true);
            var attackTransform = cardSet.transform;
            Presentation.Elements.ElementObjectManager cardSetManager = attackTransform.GetComponent<Presentation.Elements.ElementObjectManager>();
            Presentation.Elements.ElementObjectManager subManager = cardSetManager.GetElement<Presentation.Elements.ElementObjectManager>("Card");
            _ = Runtime.Program.instance.texture_.LoadDummyCard(subManager, attackCard.GetData().Id, attackCard.p.controller);
            attackCard.model.SetActive(false);
            Utility.Tools.ChangeLayer(cardSet, "DuelOverlay3D");
            var screenEffect = await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Effects.ScreenEffect>("MasterDuel/Timeline/FinalAttack/BlueEyes/ScreenEffect", true, true);
            screenEffect.transform.SetParent(Runtime.Program.instance.camera_.cameraDuelOverlay3D.transform, true);

            var hit = await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/BlueEyes/Hit" + (attackCard.p.InMyControl() ? "Far" : "Near"), true, true);
            hit.transform.position = attackedPosition;
            hit.SetActive(false);

            var beam = await Runtime.ABLoader.LoadFromFolderAsync<PlayableDirector>("MasterDuel/Timeline/FinalAttack/BlueEyes/Beam", true, true);
            beam.transform.SetParent(cardSetManager.transform, false);
            beam.transform.localPosition = new Vector3(0f, 1f, 0f);
            beam.GetComponent<PlayableDirector>().enabled = true;
            beam.GetComponent<PlayableDirector>().playOnAwake = true;
            beam.SetActive(false);

            var offset = new Vector3(0f, 5f, -8f);
            if (!attackCard.p.InMyControl())
                offset.z = 8f;
            var attackPosition = attackCard.model.transform.position;
            attackTransform.position = attackPosition + offset;
            attackTransform.LookAt(attackedPosition);
            var faceAngle = attackTransform.eulerAngles;
            faceAngle.x = 0;
            attackTransform.eulerAngles = attackRotation;
            attackTransform.position = attackPosition;

            Runtime.AudioManager.PlaySE("SE_MONSTERATTACK_BE_01");
            Sequence sequence = DOTween.Sequence();

            faceAngle.z = faceAngle.y > 0 && faceAngle.y < 180 ? -60f : 60f;
            offset = new Vector3(0f, 5f, -15f);
            if (!attackCard.p.InMyControl())
                offset.z = 15f;
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 0.6f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.DORotate(faceAngle + new Vector3(45f, 0f, 0f), 0.6f).SetEase(Ease.InOutCubic));

            offset = new Vector3(0f, 5f, -8f);
            if (!attackCard.p.InMyControl())
                offset.z = 8f;
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 0.3f).SetEase(Ease.InOutCubic));
            faceAngle.z = 0f;
            sequence.Join(attackTransform.DORotate(faceAngle, 0.3f).SetEase(Ease.InOutCubic));
            sequence.Join(DOTween.To(v => { }, 0f, 0f, 0.1f).OnComplete(() =>
            {
                beam.SetActive(true);
            }));

            offset = new Vector3(0f, 3f, 8f);
            if (!attackCard.p.InMyControl())
                offset.z = -8f;
            sequence.Append(Runtime.Program.instance.camera_.cameraMain.transform.DOLocalMove(Runtime.CameraManager.mainCameraDefaultPosition + offset, 0.1f));
            sequence.Join(DOTween.To(v => { }, 0f, 0f, 0.15f).OnComplete(() =>
            {
                hit.SetActive(true);
                Runtime.AudioManager.PlaySE("SE_MONSTERATTACK_BE_02");
                if (NextMessageIs(MDPro3.Duel.YGOSharp.GameMessage.Damage))
                    NoMoreWait = true;
                Runtime.CameraManager.ShakeCamera(true);
            }));
            sequence.AppendInterval(1f);
            sequence.Append(attackTransform.DOMove(attackPosition, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.DORotate(attackRotation, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(Runtime.Program.instance.camera_.cameraMain.transform.DOLocalMove(Runtime.CameraManager.mainCameraDefaultPosition, 0.2f));

            sequence.OnComplete(() =>
            {
                UnityEngine.Object.Destroy(cardSet);
                UnityEngine.Object.Destroy(hit);
                UnityEngine.Object.Destroy(screenEffect);
                attackCard.model.SetActive(true);
                Runtime.CameraManager.Duel3DOverlayStickWithMain(false);
            });

            return sequence;
        }

        private async UniTask<Sequence> AnimationFinalAttack_DarkM(GameCard attackCard, Vector3 attackedPosition)
        {
            await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/DarkM/CardSet", true, false);
            await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Effects.ScreenEffect>("MasterDuel/Timeline/FinalAttack/DarkM/ScreenEffect", true, false);
            await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/DarkM/Hit" + (attackCard.p.InMyControl() ? "Far" : "Near"), true, false);
            await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/DarkM/LineRendererDA", true, false);
            await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/DarkM/TargetPoint", true, false);

            var attackRotation = attackCard.p.InMyControl() ? Vector3.zero : new Vector3(0f, 180f, 0f);
            Runtime.CameraManager.Duel3DOverlayStickWithMain(true);
            Runtime.CameraManager.DuelOverlay3DPlus();

            var cardSet = await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/DarkM/CardSet", true, true);
            var screenEffect = await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Effects.ScreenEffect>("MasterDuel/Timeline/FinalAttack/DarkM/ScreenEffect", true, true);
            var hit = await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/DarkM/Hit" + (attackCard.p.InMyControl() ? "Far" : "Near"), true, true);
            var lineRendererDA = await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/DarkM/LineRendererDA", true, true);
            var targetPoint = await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/DarkM/TargetPoint", true, true);

            Utility.Tools.ChangeLayer(cardSet, "DuelOverlay3D");
            Utility.Tools.ChangeLayer(screenEffect, "DuelOverlay3D");
            Utility.Tools.ChangeLayer(hit, "DuelOverlay3D");
            Utility.Tools.ChangeLayer(lineRendererDA, "DuelOverlay3D");
            Utility.Tools.ChangeLayer(targetPoint, "DuelOverlay3D");

            var attackTransform = cardSet.transform;
            Presentation.Elements.ElementObjectManager cardSetManager = attackTransform.GetComponent<Presentation.Elements.ElementObjectManager>();
            Presentation.Elements.ElementObjectManager subManager = cardSetManager.GetElement<Presentation.Elements.ElementObjectManager>("Card");
            _ = Runtime.Program.instance.texture_.LoadDummyCard(subManager, attackCard.GetData().Id, attackCard.p.controller);
            attackCard.model.SetActive(false);
            screenEffect.transform.SetParent(Runtime.Program.instance.camera_.cameraDuelOverlay3D.transform, true);

            hit.transform.position = attackedPosition;
            hit.SetActive(false);
            targetPoint.transform.position = attackedPosition;
            targetPoint.GetComponent<PlayableDirector>().playOnAwake = true;
            targetPoint.transform.LookAt(attackCard.model.transform);
            targetPoint.transform.GetChild(0).transform.localEulerAngles = new Vector3(0f, 180f, 0f);
            targetPoint.SetActive(false);

            Presentation.Elements.ElementObjectManager lineManager = lineRendererDA.GetComponent<Presentation.Elements.ElementObjectManager>();
            var line1 = lineManager.GetElement<LineRenderer>("Line01");
            var line2 = lineManager.GetElement<LineRenderer>("Line02");
            lineRendererDA.SetActive(false);

            var offset = new Vector3(0f, 20f, -8f);
            if (!attackCard.p.InMyControl())
                offset.z = 8f;
            var attackPosition = attackCard.model.transform.position;
            attackTransform.position = attackPosition + offset;

            var positions = new Vector3[2]
            {
                attackTransform.position + new Vector3(0f, 1f, 0f),
                attackedPosition
            };
            line1.SetPositions(positions);
            line2.SetPositions(positions);

            attackTransform.LookAt(attackedPosition);
            var faceAngle = attackTransform.eulerAngles;
            faceAngle.x = 0;
            attackTransform.eulerAngles = attackRotation;
            attackTransform.position = attackPosition;

            Runtime.AudioManager.PlaySE("SE_MONSTERATTACK_BM_01");
            Sequence sequence = DOTween.Sequence();

            faceAngle.z = faceAngle.y > 0 && faceAngle.y < 180 ? -60f : 60f;
            offset = new Vector3(0f, 40f, -15f);
            if (!attackCard.p.InMyControl())
                offset.z = 15f;
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 0.8f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.DORotate(faceAngle + new Vector3(45f, 0f, 0f), 0.8f).SetEase(Ease.InOutCubic));

            offset = new Vector3(0f, 20f, -8f);
            if (!attackCard.p.InMyControl())
                offset.z = 8f;
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 0.3f).SetEase(Ease.InOutCubic));
            faceAngle.z = 0f;
            sequence.Join(attackTransform.DORotate(faceAngle + new Vector3(30f, 0f, 0f), 0.3f).SetEase(Ease.InOutCubic));
            sequence.Join(DOTween.To(v => { }, 0f, 0f, 0.1f).OnComplete(() =>
            {
                lineRendererDA.SetActive(true);
                UnityEngine.Object.Destroy(lineRendererDA, 0.58f);
            }));

            offset = new Vector3(0f, 3f, 8f);
            if (attackCard.p.controller != 0)
                offset.z = -8f;
            sequence.Append(Runtime.Program.instance.camera_.cameraMain.transform.DOLocalMove(Runtime.CameraManager.mainCameraDefaultPosition + offset, 0.1f));
            sequence.Join(DOTween.To(v => { }, 0f, 0f, 0.15f).OnComplete(() =>
            {
                hit.SetActive(true);
                targetPoint.SetActive(true);
                Runtime.AudioManager.PlaySE("SE_MONSTERATTACK_BM_02");
                if (NextMessageIs(MDPro3.Duel.YGOSharp.GameMessage.Damage))
                    NoMoreWait = true;
                Runtime.CameraManager.ShakeCamera(true);
            }));

            sequence.AppendInterval(0.6f);

            sequence.Append(attackTransform.DOMove(attackPosition, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.DORotate(attackRotation, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(Runtime.Program.instance.camera_.cameraMain.transform.DOLocalMove(Runtime.CameraManager.mainCameraDefaultPosition, 0.2f));

            sequence.OnComplete(() =>
            {
                UnityEngine.Object.Destroy(cardSet);
                UnityEngine.Object.Destroy(hit);
                UnityEngine.Object.Destroy(screenEffect);
                UnityEngine.Object.Destroy(targetPoint);
                attackCard.model.SetActive(true);
                Runtime.CameraManager.Duel3DOverlayStickWithMain(false);
                Runtime.CameraManager.DuelOverlay3DMinus();
            });

            return sequence;
        }

        private async UniTask<Sequence> AnimationFinalAttack_RedEyes(GameCard attackCard, Vector3 attackedPosition)
        {
            await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/RedEyes/CardSet", true, false);
            await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Effects.ScreenEffect>("MasterDuel/Timeline/FinalAttack/RedEyes/ScreenEffect", true, false);
            await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/RedEyes/Hit" + (attackCard.p.controller == 0 ? "Far" : "Near"), true, false);
            await Runtime.ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Timeline/FinalAttack/RedEyes/Bless", true, false);

            var attackRotation = attackCard.p.controller == 0 ? Vector3.zero : new Vector3(0f, 180f, 0f);
            Runtime.CameraManager.Duel3DOverlayStickWithMain(true);
            Runtime.CameraManager.DuelOverlay3DPlus();

            var cardSet = await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/RedEyes/CardSet", true, true);
            var screenEffect = await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Effects.ScreenEffect>("MasterDuel/Timeline/FinalAttack/RedEyes/ScreenEffect", true, true);
            var hit = await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/RedEyes/Hit" + (attackCard.p.controller == 0 ? "Far" : "Near"), true, true);
            var bless = await Runtime.ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Timeline/FinalAttack/RedEyes/Bless", true, true);
            bless.SetActive(false);

            Utility.Tools.ChangeLayer(cardSet, "DuelOverlay3D");
            var attackTransform = cardSet.transform;
            Presentation.Elements.ElementObjectManager cardSetManager = attackTransform.GetComponent<Presentation.Elements.ElementObjectManager>();
            Presentation.Elements.ElementObjectManager subManager = cardSetManager.GetElement<Presentation.Elements.ElementObjectManager>("Card");
            _ = Runtime.Program.instance.texture_.LoadDummyCard(subManager, attackCard.GetData().Id, attackCard.p.controller);
            cardSetManager.GetComponent<PlayableDirector>().Play();
            attackCard.model.SetActive(false);
            screenEffect.transform.SetParent(Runtime.Program.instance.camera_.cameraDuelOverlay3D.transform, true);

            hit.transform.position = attackedPosition;
            hit.SetActive(false);

            var offset = new Vector3(0f, 20f, -8f);
            if (!attackCard.p.InMyControl())
                offset.z = 8f;
            var attackPosition = attackCard.model.transform.position;
            attackTransform.position = attackPosition + offset;

            attackTransform.LookAt(attackedPosition);
            var faceAngle = attackTransform.eulerAngles;
            faceAngle.x = 0;
            attackTransform.eulerAngles = attackRotation;
            attackTransform.position = attackPosition;

            Runtime.AudioManager.PlaySE("SE_MONSTERATTACK_RE_01");
            Sequence sequence = DOTween.Sequence();

            faceAngle.z = faceAngle.y > 0 && faceAngle.y < 180 ? -60f : 60f;
            offset = new Vector3(0f, 40f, -15f);
            if (!attackCard.p.InMyControl())
                offset.z = 15f;
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 0.6f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.DORotate(faceAngle + new Vector3(45f, 0f, 0f), 0.6f).SetEase(Ease.InOutCubic));

            offset = new Vector3(0f, 20f, -8f);
            if (!attackCard.p.InMyControl())
                offset.z = 8f;
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 0.3f).SetEase(Ease.InOutCubic));
            faceAngle.z = 0f;
            sequence.Join(attackTransform.DORotate(faceAngle + new Vector3(30f, 0f, 0f), 0.3f).SetEase(Ease.InOutCubic));

            offset = new Vector3(0f, 3f, 8f);
            if (!attackCard.p.InMyControl())
                offset.z = -8f;
            sequence.Append(Runtime.Program.instance.camera_.cameraMain.transform.DOLocalMove(Runtime.CameraManager.mainCameraDefaultPosition + offset, 0.3f));
            sequence.Join(DOTween.To(v => { }, 0f, 0f, 0.05f).OnComplete(() =>
            {
                bless.SetActive(true);
                bless.transform.position = attackTransform.position;
                bless.transform.LookAt(attackedPosition);
                bless.transform.DOMove(attackedPosition, 0.3f);
            }));

            sequence.AppendCallback(() =>
            {
                hit.SetActive(true);
                Runtime.AudioManager.PlaySE("SE_MONSTERATTACK_RE_02");
                if (NextMessageIs(MDPro3.Duel.YGOSharp.GameMessage.Damage))
                    NoMoreWait = true;
                Runtime.CameraManager.ShakeCamera(true);
            });
            sequence.AppendInterval(0.6f);
            sequence.Append(attackTransform.DOMove(attackPosition, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.DORotate(attackRotation, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(Runtime.Program.instance.camera_.cameraMain.transform.DOLocalMove(Runtime.CameraManager.mainCameraDefaultPosition, 0.2f));

            sequence.OnComplete(() =>
            {
                UnityEngine.Object.Destroy(cardSet);
                UnityEngine.Object.Destroy(hit);
                UnityEngine.Object.Destroy(screenEffect);
                UnityEngine.Object.Destroy(bless);
                attackCard.model.SetActive(true);
                Runtime.CameraManager.Duel3DOverlayStickWithMain(false);
                Runtime.CameraManager.DuelOverlay3DMinus();
            });

            return sequence;
        }

        private async UniTask<Sequence> AnimationFinalAttack_Ra(GameCard attackCard, Vector3 attackedPosition)
        {
            await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Ra/CardSet", true, false);
            await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Effects.ScreenEffect>("MasterDuel/Timeline/FinalAttack/Ra/ScreenEffect", true, false);
            await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Ra/Hit" + (attackCard.p.controller == 0 ? "Far" : "Near"), true, false);
            await Runtime.ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Timeline/FinalAttack/Ra/Bless", true, false);

            var attackRotation = attackCard.p.controller == 0 ? Vector3.zero : new Vector3(0f, 180f, 0f);
            Runtime.CameraManager.Duel3DOverlayStickWithMain(true);
            Runtime.CameraManager.DuelOverlay3DPlus();

            var cardSet = await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Ra/CardSet", true, true);
            var screenEffect = await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Effects.ScreenEffect>("MasterDuel/Timeline/FinalAttack/Ra/ScreenEffect", true, true);
            var hit = await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Ra/Hit" + (attackCard.p.controller == 0 ? "Far" : "Near"), true, true);
            var bless = await Runtime.ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Timeline/FinalAttack/Ra/Bless", true, true);
            Utility.Tools.SetParticleSystemSimulationSpeed(bless.transform, 0.5f);
            bless.SetActive(false);

            Utility.Tools.ChangeLayer(cardSet, "DuelOverlay3D");
            var attackTransform = cardSet.transform;
            Presentation.Elements.ElementObjectManager cardSetManager = attackTransform.GetComponent<Presentation.Elements.ElementObjectManager>();
            Presentation.Elements.ElementObjectManager subManager = cardSetManager.GetElement<Presentation.Elements.ElementObjectManager>("Card");
            _ = Runtime.Program.instance.texture_.LoadDummyCard(subManager, attackCard.GetData().Id, attackCard.p.controller);
            cardSetManager.GetComponent<PlayableDirector>().Play();
            attackCard.model.SetActive(false);
            screenEffect.transform.SetParent(Runtime.Program.instance.camera_.cameraDuelOverlay3D.transform, true);

            hit.transform.position = attackedPosition;
            hit.SetActive(false);

            var offset = new Vector3(0f, 20f, -8f);
            if (!attackCard.p.InMyControl())
                offset.z = 8f;
            var attackPosition = attackCard.model.transform.position;
            attackTransform.position = attackPosition + offset;

            attackTransform.LookAt(attackedPosition);
            var faceAngle = attackTransform.eulerAngles;
            faceAngle.x = 0;
            attackTransform.eulerAngles = attackRotation;
            attackTransform.position = attackPosition;

            Runtime.AudioManager.PlaySE("SE_MONSTERATTACK_RA_01");
            Sequence sequence = DOTween.Sequence();
            faceAngle.z = 0f;
            offset = new Vector3(0f, 40f, -15f);
            if (!attackCard.p.InMyControl())
                offset.z = 15f;
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 1f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.DORotate(faceAngle + new Vector3(-30f, 0f, 0f), 1f).SetEase(Ease.InOutCubic));

            sequence.AppendCallback(() =>
            {
                bless.SetActive(true);
                bless.transform.position = attackTransform.position;
                bless.transform.LookAt(attackedPosition);

                bless.transform.DOMove(attackedPosition, 0.3f);

                offset = new Vector3(0f, 3f, 8f);
                if (attackCard.p.controller != 0)
                    offset.z = -8f;
                Runtime.Program.instance.camera_.cameraMain.transform.DOLocalMove(Runtime.CameraManager.mainCameraDefaultPosition + offset, 0.3f);
            });

            offset = new Vector3(0f, 20f, 0f);
            if (!attackCard.p.InMyControl())
                offset.z = 0f;
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 0.3f).SetEase(Ease.OutCubic));
            sequence.Join(attackTransform.DORotate(faceAngle + new Vector3(30f, 0f, 0f), 0.3f).SetEase(Ease.OutCubic));


            sequence.AppendCallback(() =>
            {
                hit.SetActive(true);
                Runtime.AudioManager.PlaySE("SE_MONSTERATTACK_RA_02");
                if (NextMessageIs(MDPro3.Duel.YGOSharp.GameMessage.Damage))
                    NoMoreWait = true;
                Runtime.CameraManager.ShakeCamera(true);
            });
            sequence.AppendInterval(0.6f);
            sequence.Append(attackTransform.DOMove(attackPosition, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.DORotate(attackRotation, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(Runtime.Program.instance.camera_.cameraMain.transform.DOLocalMove(Runtime.CameraManager.mainCameraDefaultPosition, 0.2f));

            sequence.OnComplete(() =>
            {
                UnityEngine.Object.Destroy(cardSet);
                UnityEngine.Object.Destroy(hit);
                UnityEngine.Object.Destroy(screenEffect);
                UnityEngine.Object.Destroy(bless);
                attackCard.model.SetActive(true);
                Runtime.CameraManager.Duel3DOverlayStickWithMain(false);
                Runtime.CameraManager.DuelOverlay3DMinus();
            });

            return sequence;
        }

        private async UniTask<Sequence> AnimationFinalAttack_Slifer(GameCard attackCard, Vector3 attackedPosition)
        {
            await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Slifer/CardSet", true, false);
            await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Effects.ScreenEffect>("MasterDuel/Timeline/FinalAttack/Slifer/ScreenEffect", true, false);
            await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Slifer/Hit" + (attackCard.p.InMyControl() ? "Far" : "Near"), true, false);
            await Runtime.ABLoader.LoadFromFolderAsync<PlayableDirector>("MasterDuel/Timeline/FinalAttack/Slifer/Beam", true, false);

            var attackRotation = attackCard.p.InMyControl() ? Vector3.zero : new Vector3(0f, 180f, 0f);
            Runtime.CameraManager.Duel3DOverlayStickWithMain(true);
            Runtime.CameraManager.DuelOverlay3DPlus();

            var cardSet = await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Slifer/CardSet", true, true);
            Utility.Tools.ChangeLayer(cardSet, "DuelOverlay3D");
            var attackTransform = cardSet.transform;
            Presentation.Elements.ElementObjectManager cardSetManager = attackTransform.GetComponent<Presentation.Elements.ElementObjectManager>();
            Presentation.Elements.ElementObjectManager subManager = cardSetManager.GetElement<Presentation.Elements.ElementObjectManager>("Card");
            _ = Runtime.Program.instance.texture_.LoadDummyCard(subManager, attackCard.GetData().Id, attackCard.p.controller);
            attackCard.model.SetActive(false);

            var screenEffect = await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Effects.ScreenEffect>("MasterDuel/Timeline/FinalAttack/Slifer/ScreenEffect", true, true);
            screenEffect.transform.SetParent(Runtime.Program.instance.camera_.cameraDuelOverlay3D.transform, true);
            screenEffect.SetActive(false);

            var hit = await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Slifer/Hit" + (attackCard.p.InMyControl() ? "Far" : "Near"), true, true);
            hit.transform.position = attackedPosition;
            hit.SetActive(false);

            var beam = await Runtime.ABLoader.LoadFromFolderAsync<PlayableDirector>("MasterDuel/Timeline/FinalAttack/Slifer/Beam", true, true);
            beam.transform.SetParent(cardSetManager.transform, false);
            beam.transform.localPosition = new Vector3(0f, 1f, 0f);
            beam.GetComponent<PlayableDirector>().enabled = true;
            beam.GetComponent<PlayableDirector>().playOnAwake = true;
            beam.SetActive(false);

            var offset = new Vector3(0f, 20f, -8f);
            if (!attackCard.p.InMyControl())
                offset.z = 8f;
            var attackPosition = attackCard.model.transform.position;
            attackTransform.position = attackPosition + offset;
            attackTransform.LookAt(attackedPosition);
            var faceAngle = attackTransform.eulerAngles;
            faceAngle.x = 0;
            attackTransform.eulerAngles = attackRotation;
            attackTransform.position = attackPosition;

            Runtime.AudioManager.PlaySE("SE_MONSTERATTACK_SLIFER_01");
            Sequence sequence = DOTween.Sequence();

            faceAngle.z = 0;
            offset = new Vector3(0f, 40f, -15f);
            if (!attackCard.p.InMyControl())
                offset.z = 15f;
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 0.8f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.DORotate(faceAngle + new Vector3(30f, 0f, 0f), 0.8f).SetEase(Ease.InOutCubic));
            sequence.Join(DOTween.To(v => { }, 0f, 0f, 0.4f).OnComplete(() =>
            {
                screenEffect.SetActive(true);
            }));

            offset = new Vector3(0f, 20f, -8f);
            if (!attackCard.p.InMyControl())
                offset.z = 8f;
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 0.3f).SetEase(Ease.InOutCubic));
            faceAngle.z = 0f;
            sequence.Join(attackTransform.DORotate(faceAngle, 0.3f).SetEase(Ease.InOutCubic));
            sequence.Join(DOTween.To(v => { }, 0f, 0f, 0.1f).OnComplete(() =>
            {
                beam.SetActive(true);
            }));
            sequence.AppendCallback(() =>
            {
                offset = new Vector3(0f, 3f, 8f);
                if (attackCard.p.controller != 0)
                    offset.z = -8f;
                Runtime.Program.instance.camera_.cameraMain.transform.DOLocalMove(Runtime.CameraManager.mainCameraDefaultPosition + offset, 0.1f);
            });
            sequence.AppendInterval(0.1f);
            sequence.AppendCallback(() =>
            {
                hit.SetActive(true);
                Runtime.AudioManager.PlaySE("SE_MONSTERATTACK_SLIFER_02");
                if (NextMessageIs(MDPro3.Duel.YGOSharp.GameMessage.Damage))
                    NoMoreWait = true;
                Runtime.CameraManager.ShakeCamera(true);
            });
            sequence.AppendInterval(0.6f);
            sequence.Append(attackTransform.DOMove(attackPosition, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.DORotate(attackRotation, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(Runtime.Program.instance.camera_.cameraMain.transform.DOLocalMove(Runtime.CameraManager.mainCameraDefaultPosition, 0.2f));

            sequence.OnComplete(() =>
            {
                UnityEngine.Object.Destroy(cardSet);
                UnityEngine.Object.Destroy(hit);
                UnityEngine.Object.Destroy(screenEffect);
                attackCard.model.SetActive(true);
                Runtime.CameraManager.Duel3DOverlayStickWithMain(false);
                Runtime.CameraManager.DuelOverlay3DMinus();
            });

            return sequence;
        }

        private async UniTask<Sequence> AnimationFinalAttack_Obelisk(GameCard attackCard, Vector3 attackedPosition)
        {
            await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Obelisk/CardSet", true, false);
            await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Effects.ScreenEffect>("MasterDuel/Timeline/FinalAttack/Obelisk/ScreenEffect", true, false);
            await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Obelisk/Hit" + (attackCard.p.controller == 0 ? "Far" : "Near"), true, false);
            await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Obelisk/Punch", true, false);

            var attackRotation = attackCard.p.controller == 0 ? Vector3.zero : new Vector3(0f, 180f, 0f);
            Runtime.CameraManager.Duel3DOverlayStickWithMain(true);
            Runtime.CameraManager.DuelOverlay3DPlus();

            var cardSet = await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Obelisk/CardSet", true, true);
            var screenEffect = await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Effects.ScreenEffect>("MasterDuel/Timeline/FinalAttack/Obelisk/ScreenEffect", true, true);
            var hit = await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Obelisk/Hit" + (attackCard.p.controller == 0 ? "Far" : "Near"), true, true);
            var punch = await Runtime.ABLoader.LoadFromFolderAsync<Presentation.Elements.ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Obelisk/Punch", true, true);

            punch.GetComponent<PlayableDirector>().enabled = true;
            if (!attackCard.p.InMyControl())
                punch.transform.eulerAngles = new Vector3(0f, 180f, 0f);

            Utility.Tools.ChangeLayer(cardSet, "DuelOverlay3D");
            Utility.Tools.ChangeLayer(punch, "DuelOverlay3D");

            var attackTransform = cardSet.transform;
            Presentation.Elements.ElementObjectManager cardSetManager = attackTransform.GetComponent<Presentation.Elements.ElementObjectManager>();
            Presentation.Elements.ElementObjectManager subManager = cardSetManager.GetElement<Presentation.Elements.ElementObjectManager>("Card");
            _ = Runtime.Program.instance.texture_.LoadDummyCard(subManager, attackCard.GetData().Id, attackCard.p.controller);
            cardSetManager.GetComponent<PlayableDirector>().Play();
            attackCard.model.SetActive(false);
            screenEffect.transform.SetParent(Runtime.Program.instance.camera_.cameraDuelOverlay3D.transform, true);

            hit.transform.position = attackedPosition;
            hit.SetActive(false);
            punch.transform.position = attackedPosition;
            punch.SetActive(false);

            var offset = new Vector3(0f, 5f, -8f);
            if (!attackCard.p.InMyControl())
                offset.z = 8f;
            var attackPosition = attackCard.model.transform.position;
            attackTransform.position = attackPosition + offset;

            attackTransform.LookAt(attackedPosition);
            var faceAngle = attackTransform.eulerAngles;
            faceAngle.x = 0;
            attackTransform.eulerAngles = attackRotation;
            attackTransform.position = attackPosition;

            Runtime.AudioManager.PlaySE("SE_MONSTERATTACK_OBELISK_01");
            Sequence sequence = DOTween.Sequence();

            faceAngle.z = 0f;
            offset = new Vector3(5f, 40f, -15f);
            if (!attackCard.p.InMyControl())
            {
                offset.x = -5f;
                offset.z = 15f;
            }
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 1.5f).SetEase(Ease.InOutCubic));
            offset = new Vector3(-30f, 35f, 0f);
            sequence.Join(attackTransform.DORotate(faceAngle + offset, 1.5f).SetEase(Ease.InOutCubic));
            sequence.Join(DOTween.To(v => { }, 0f, 0f, 1f).OnComplete(() =>
            {
                punch.SetActive(true);
            }));

            offset = new Vector3(0f, 20f, -8f);
            if (!attackCard.p.InMyControl())
                offset.z = 8f;
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 0.4f).SetEase(Ease.InOutCubic));
            faceAngle.z = 0f;
            offset = new Vector3(20f, 0f, 0f);
            sequence.Join(attackTransform.DORotate(faceAngle + offset, 0.4f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.GetChild(0).DOLocalMoveZ(10f, 0.4f));
            sequence.Join(attackTransform.GetChild(0).DOLocalRotate(new Vector3(0f, -30f, 0f), 0.4f));
            offset = new Vector3(0f, 3f, 8f);
            if (!attackCard.p.InMyControl())
                offset.z = -8f;
            sequence.Join(Runtime.Program.instance.camera_.cameraMain.transform.DOLocalMove(Runtime.CameraManager.mainCameraDefaultPosition + offset, 0.4f));
            sequence.AppendInterval(0.1f);
            sequence.AppendCallback(() =>
            {
                hit.SetActive(true);
                Runtime.AudioManager.PlaySE("SE_MONSTERATTACK_OBELISK_02");
                if (NextMessageIs(MDPro3.Duel.YGOSharp.GameMessage.Damage))
                    NoMoreWait = true;
                Runtime.CameraManager.ShakeCamera(true);
            });

            sequence.AppendInterval(0.6f);
            sequence.Append(attackTransform.DOMove(attackPosition, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.DORotate(attackRotation, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.GetChild(0).DOLocalMove(Vector3.zero, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.GetChild(0).DOLocalRotate(Vector3.zero, 0.5f).SetEase(Ease.InOutCubic));

            sequence.Join(Runtime.Program.instance.camera_.cameraMain.transform.DOLocalMove(Runtime.CameraManager.mainCameraDefaultPosition, 0.2f));

            sequence.OnComplete(() =>
            {
                UnityEngine.Object.Destroy(cardSet);
                UnityEngine.Object.Destroy(hit);
                UnityEngine.Object.Destroy(screenEffect);
                UnityEngine.Object.Destroy(punch);
                attackCard.model.SetActive(true);
                Runtime.CameraManager.Duel3DOverlayStickWithMain(false);
                Runtime.CameraManager.DuelOverlay3DMinus();
            });

            return sequence;
        }

        #endregion

        #region Enum

        private enum FinalAttackType
        {
            BlueEyes,
            DarkM,
            RedEyes,
            Slifer,
            Obelisk,
            Ra,
            Normal
        }

        #endregion

    }
}
