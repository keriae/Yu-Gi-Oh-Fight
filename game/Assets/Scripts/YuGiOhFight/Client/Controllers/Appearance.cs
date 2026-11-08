using UnityEngine;


using UnityEngine.EventSystems;


using Cysharp.Threading.Tasks;


namespace YuGiOhFight.Client.Controllers
{
    public class Appearance : Servant
    {

        [Header("Appearance")]
        [HideInInspector] public UI.SelectionToggle_AppearanceGenre lastSelectedToggle;
        [HideInInspector] public UI.SelectionToggle_AppearanceItem lastSelectedItem;

        #region Assets

        public static Sprite duelFace0;
        public static Sprite duelFace1;
        public static Sprite watchFace0;
        public static Sprite watchFace1;
        public static Sprite replayFace0;
        public static Sprite replayFace1;
        public static Sprite duelFace0Tag;
        public static Sprite duelFace1Tag;
        public static Sprite watchFace0Tag;
        public static Sprite watchFace1Tag;
        public static Sprite replayFace0Tag;
        public static Sprite replayFace1Tag;
        public static Sprite defaultFace0;
        public static Sprite defaultFace1;

        public static Material duelFrameMat0;
        public static Material duelFrameMat1;
        public static Material watchFrameMat0;
        public static Material watchFrameMat1;
        public static Material replayFrameMat0;
        public static Material replayFrameMat1;
        public static Material duelFrameMat0Tag;
        public static Material duelFrameMat1Tag;
        public static Material watchFrameMat0Tag;
        public static Material watchFrameMat1Tag;
        public static Material replayFrameMat0Tag;
        public static Material replayFrameMat1Tag;

        public static Material duelProtector0;
        public static Material duelProtector1;
        public static Material watchProtector0;
        public static Material watchProtector1;
        public static Material replayProtector0;
        public static Material replayProtector1;
        public static Material duelProtector0Tag;
        public static Material duelProtector1Tag;
        public static Material watchProtector0Tag;
        public static Material watchProtector1Tag;
        public static Material replayProtector0Tag;
        public static Material replayProtector1Tag;

        public static Material matForFace;
        public static string player = "0";
        public const string meString = "Me";
        public const string opString = "Op";
        public const string meTagString = "MeTag";
        public const string opTagString = "OpTag";

        #endregion

        private const string FaceFrameMaterialCode = "1030001";

        public enum Condition
        {
            Duel,
            Watch,
            Replay,
            DeckEditor
        }
        public static Condition condition = Condition.Duel;
        public void SwitchCondition(Condition condition)
        {
            Appearance.condition = condition;
        }

        #region Servant

        public override int Depth => 7;
        protected override bool ShowLine => false;
        protected override float SubBlackAlpha => 0.9f;
        public override void Initialize()
        {
            base.Initialize();
            _ = LoadSettingAssets();
        }

        public override void OnReturn()
        {
            if (inTransition) return;
            if (returnAction != null)
            {
                returnAction.Invoke();
                return;
            }
            Runtime.AudioManager.PlaySE("SE_MENU_CANCEL");
            GameObject selected = EventSystem.current.currentSelectedGameObject;

            if (selected == null)
                OnExit();
            else if (Cursor.lockState == CursorLockMode.None)
                OnExit();
            else if (selected.TryGetComponent<UI.SelectionToggle_AppearanceItem>(out _)
                || selected == GetUI<UI.ServantUI.AppearanceUI>().InputPlayerName.gameObject)
            {
                if (lastSelectedToggle != null)
                    lastSelectedToggle.GetSelectable().Select();
                else
                    servantUI.SelectDefaultSelectable();
            }
            else
                OnExit();
        }

        public override void OnExit()
        {
            if (condition != Condition.DeckEditor)
            {
                if (Runtime.Program.instance.currentSubServant == this)
                    Runtime.Program.instance.ShowSubServant(Runtime.Program.instance.setting);
                else
                    Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.setting);
            }
            else
            {
                Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.deckEditor);
            }
        }

        public override void PerFrameFunction()
        {
            if (NeedResponseInput())
            {
                if (Runtime.UserInput.WasLeftShoulderPressed)
                    if (GetUI<UI.ServantUI.AppearanceUI>().CanSwitchPlayer())
                        GetUI<UI.ServantUI.AppearanceUI>().OnPlayerLeft();
                if (Runtime.UserInput.WasRightShoulderPressed)
                    if (GetUI<UI.ServantUI.AppearanceUI>().CanSwitchPlayer())
                        GetUI<UI.ServantUI.AppearanceUI>().OnPlayerRight();

                if (Runtime.UserInput.WasGamepadButtonNorthPressed)
                    if (GetUI<UI.ServantUI.AppearanceUI>().InPickupPage())
                        GetUI<UI.ServantUI.AppearanceUI>().OnPickupChange();

                if (Runtime.UserInput.MouseRightDown || Runtime.UserInput.WasCancelPressed)
                    OnReturn();
            }
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !Runtime.UserInput.NeedDefaultSelect())
                return;

            if (lastSelectable != null)
            {
                if (lastSelectable.TryGetComponent<UI.SelectionToggle_CharacterItem>(out _)
                    || lastSelectable.TryGetComponent<UI.SelectionToggle_CharacterSeries>(out _))
                    lastSelectable.Select();
                else
                    servantUI.SelectDefaultSelectable();
            }
            else
                servantUI.SelectDefaultSelectable();
        }

        #endregion

        public static bool loaded;
        public async UniTask LoadSettingAssets()
        {
            if (!loaded && matForFace != null) return;
            loaded = false;
            await LoadSettingAssetsCore();
        }

        private static void ApplyFrameTexture(Material material, Sprite sprite)
        {
            if (material != null && sprite != null)
                material.SetTexture("_ProfileFrameTex", sprite.texture);
        }

        private async UniTask LoadSettingAssetsCore()
        {
            Services.ReplayAppearance.BeginAssetLoad();
            matForFace = await Runtime.ABLoader.LoadFrameMaterial(FaceFrameMaterialCode);

            #region Face
            duelFace0 = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("DuelFace0", Runtime.Program.items.faces[0].id.ToString()), Presentation.Assets.Items.ItemType.Face, 0);
            duelFace1 = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("DuelFace1", Runtime.Program.items.faces[0].id.ToString()), Presentation.Assets.Items.ItemType.Face, 1);
            duelFace0Tag = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("DuelFace0Tag", Runtime.Program.items.faces[0].id.ToString()), Presentation.Assets.Items.ItemType.Face, 2);
            duelFace1Tag = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("DuelFace1Tag", Runtime.Program.items.faces[0].id.ToString()), Presentation.Assets.Items.ItemType.Face, 3);
            watchFace0 = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("WatchFace0", Runtime.Program.items.faces[0].id.ToString()), Presentation.Assets.Items.ItemType.Face, 0);
            watchFace1 = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("WatchFace1", Runtime.Program.items.faces[0].id.ToString()), Presentation.Assets.Items.ItemType.Face, 1);
            watchFace0Tag = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("WatchFace0Tag", Runtime.Program.items.faces[0].id.ToString()), Presentation.Assets.Items.ItemType.Face, 2);
            watchFace1Tag = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("WatchFace1Tag", Runtime.Program.items.faces[0].id.ToString()), Presentation.Assets.Items.ItemType.Face, 3);
            replayFace0 = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("ReplayFace0", Runtime.Program.items.faces[0].id.ToString()), Presentation.Assets.Items.ItemType.Face, 0);
            replayFace1 = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("ReplayFace1", Runtime.Program.items.faces[0].id.ToString()), Presentation.Assets.Items.ItemType.Face, 1);
            replayFace0Tag = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("ReplayFace0Tag", Runtime.Program.items.faces[0].id.ToString()), Presentation.Assets.Items.ItemType.Face, 2);
            replayFace1Tag = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("ReplayFace1Tag", Runtime.Program.items.faces[0].id.ToString()), Presentation.Assets.Items.ItemType.Face, 3);
            defaultFace0 = await Runtime.Program.items.LoadConcreteItemIconAsync("1010039", Presentation.Assets.Items.ItemType.Face);
            defaultFace1 = await Runtime.Program.items.LoadConcreteItemIconAsync("1010001", Presentation.Assets.Items.ItemType.Face);

            #endregion

            #region Frame

            var duelFrame0 = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("DuelFrame0", Runtime.Program.items.frames[0].id.ToString()), Presentation.Assets.Items.ItemType.Frame);
            duelFrameMat0 = await Runtime.ABLoader.LoadFrameMaterial(Services.ReplayAppearance.Get("DuelFrame0", Runtime.Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(duelFrameMat0, duelFrame0);

            var duelFrame1 = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("DuelFrame1", Runtime.Program.items.frames[0].id.ToString()), Presentation.Assets.Items.ItemType.Frame);
            duelFrameMat1 = await Runtime.ABLoader.LoadFrameMaterial(Services.ReplayAppearance.Get("DuelFrame1", Runtime.Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(duelFrameMat1, duelFrame1);

            var duelFrame0Tag = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("DuelFrame0Tag", Runtime.Program.items.frames[0].id.ToString()), Presentation.Assets.Items.ItemType.Frame);
            duelFrameMat0Tag = await Runtime.ABLoader.LoadFrameMaterial(Services.ReplayAppearance.Get("DuelFrame0Tag", Runtime.Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(duelFrameMat0Tag, duelFrame0Tag);

            var duelFrame1Tag = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("DuelFrame1Tag", Runtime.Program.items.frames[0].id.ToString()), Presentation.Assets.Items.ItemType.Frame);
            duelFrameMat1Tag = await Runtime.ABLoader.LoadFrameMaterial(Services.ReplayAppearance.Get("DuelFrame1Tag", Runtime.Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(duelFrameMat1Tag, duelFrame1Tag);

            var watchFrame0 = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("WatchFrame0", Runtime.Program.items.frames[0].id.ToString()), Presentation.Assets.Items.ItemType.Frame);
            watchFrameMat0 = await Runtime.ABLoader.LoadFrameMaterial(Services.ReplayAppearance.Get("WatchFrame0", Runtime.Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(watchFrameMat0, watchFrame0);

            var watchFrame1 = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("WatchFrame1", Runtime.Program.items.frames[0].id.ToString()), Presentation.Assets.Items.ItemType.Frame);
            watchFrameMat1 = await Runtime.ABLoader.LoadFrameMaterial(Services.ReplayAppearance.Get("WatchFrame1", Runtime.Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(watchFrameMat1, watchFrame1);

            var watchFrame0Tag = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("WatchFrame0Tag", Runtime.Program.items.frames[0].id.ToString()), Presentation.Assets.Items.ItemType.Frame);
            watchFrameMat0Tag = await Runtime.ABLoader.LoadFrameMaterial(Services.ReplayAppearance.Get("WatchFrame0Tag", Runtime.Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(watchFrameMat0Tag, watchFrame0Tag);

            var watchFrame1Tag = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("WatchFrame1Tag", Runtime.Program.items.frames[0].id.ToString()), Presentation.Assets.Items.ItemType.Frame);
            watchFrameMat1Tag = await Runtime.ABLoader.LoadFrameMaterial(Services.ReplayAppearance.Get("WatchFrame1Tag", Runtime.Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(watchFrameMat1Tag, watchFrame1Tag);

            var replayFrame0 = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("ReplayFrame0", Runtime.Program.items.frames[0].id.ToString()), Presentation.Assets.Items.ItemType.Frame);
            replayFrameMat0 = await Runtime.ABLoader.LoadFrameMaterial(Services.ReplayAppearance.Get("ReplayFrame0", Runtime.Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(replayFrameMat0, replayFrame0);

            var replayFrame1 = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("ReplayFrame1", Runtime.Program.items.frames[0].id.ToString()), Presentation.Assets.Items.ItemType.Frame);
            replayFrameMat1 = await Runtime.ABLoader.LoadFrameMaterial(Services.ReplayAppearance.Get("ReplayFrame1", Runtime.Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(replayFrameMat1, replayFrame1);

            var replayFrame0Tag = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("ReplayFrame0Tag", Runtime.Program.items.frames[0].id.ToString()), Presentation.Assets.Items.ItemType.Frame);
            replayFrameMat0Tag = await Runtime.ABLoader.LoadFrameMaterial(Services.ReplayAppearance.Get("ReplayFrame0Tag", Runtime.Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(replayFrameMat0Tag, replayFrame0Tag);

            var replayFrame1Tag = await Runtime.Program.items.LoadConcreteItemIconAsync(Services.ReplayAppearance.Get("ReplayFrame1Tag", Runtime.Program.items.frames[0].id.ToString()), Presentation.Assets.Items.ItemType.Frame);
            replayFrameMat1Tag = await Runtime.ABLoader.LoadFrameMaterial(Services.ReplayAppearance.Get("ReplayFrame1Tag", Runtime.Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(replayFrameMat1Tag, replayFrame1Tag);

            #endregion

            #region Protector

            duelProtector0 = await Runtime.ABLoader.LoadProtectorMaterial(Services.ReplayAppearance.Get("DuelProtector0", Runtime.Program.items.protectors[0].id.ToString()), default);
            duelProtector1 = await Runtime.ABLoader.LoadProtectorMaterial(Services.ReplayAppearance.Get("DuelProtector1", Runtime.Program.items.protectors[0].id.ToString()), default);
            duelProtector0Tag = await Runtime.ABLoader.LoadProtectorMaterial(Services.ReplayAppearance.Get("DuelProtector0Tag", Runtime.Program.items.protectors[0].id.ToString()), default);
            duelProtector1Tag = await Runtime.ABLoader.LoadProtectorMaterial(Services.ReplayAppearance.Get("DuelProtector1Tag", Runtime.Program.items.protectors[0].id.ToString()), default);
            watchProtector0 = await Runtime.ABLoader.LoadProtectorMaterial(Services.ReplayAppearance.Get("WatchProtector0", Runtime.Program.items.protectors[0].id.ToString()), default);
            watchProtector1 = await Runtime.ABLoader.LoadProtectorMaterial(Services.ReplayAppearance.Get("WatchProtector1", Runtime.Program.items.protectors[0].id.ToString()), default);
            watchProtector0Tag = await Runtime.ABLoader.LoadProtectorMaterial(Services.ReplayAppearance.Get("WatchProtector0Tag", Runtime.Program.items.protectors[0].id.ToString()), default);
            watchProtector1Tag = await Runtime.ABLoader.LoadProtectorMaterial(Services.ReplayAppearance.Get("WatchProtector1Tag", Runtime.Program.items.protectors[0].id.ToString()), default);
            replayProtector0 = await Runtime.ABLoader.LoadProtectorMaterial(Services.ReplayAppearance.Get("ReplayProtector0", Runtime.Program.items.protectors[0].id.ToString()), default);
            replayProtector1 = await Runtime.ABLoader.LoadProtectorMaterial(Services.ReplayAppearance.Get("ReplayProtector1", Runtime.Program.items.protectors[0].id.ToString()), default);
            replayProtector0Tag = await Runtime.ABLoader.LoadProtectorMaterial(Services.ReplayAppearance.Get("ReplayProtector0Tag", Runtime.Program.items.protectors[0].id.ToString()), default);
            replayProtector1Tag = await Runtime.ABLoader.LoadProtectorMaterial(Services.ReplayAppearance.Get("ReplayProtector1Tag", Runtime.Program.items.protectors[0].id.ToString()), default);

            #endregion

            loaded = true;
            if (Runtime.Program.instance.currentServant == Runtime.Program.instance.room)
                Runtime.Program.instance.room.Realize();
        }

    }
}
