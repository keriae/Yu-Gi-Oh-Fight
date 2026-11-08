using UnityEngine;
using UnityEngine.AddressableAssets;


using UnityEngine.EventSystems;


namespace YuGiOhFight.Client.Controllers
{
    public class CharacterSelector : Servant
    {
        [HideInInspector] public UI.SelectionToggle_CharacterSeries lastSelectedToggle;
        [HideInInspector] public UI.SelectionToggle_CharacterItem lastSelectedCharacter;

        public static MDPro3.Characters characters;
        public static GameObject characterItem;


        public enum Condition
        {
            Duel,
            Watch,
            Replay
        }
        public static Condition condition = Condition.Duel;

        public void SwitchCondition(Condition condition)
        {
            CharacterSelector.condition = condition;
        }

        #region Servant

        public override int Depth => 3;
        protected override bool ShowLine => false;
        protected override float SubBlackAlpha => 0.9f;

        public override void Initialize()
        {
            base.Initialize();

            var handle = Addressables.LoadAssetAsync<MDPro3.Characters>("ScriptableObjects/Characters.asset");
            handle.Completed += (result) =>
            {
                characters = result.Result;
                LoadCharacters();
                Runtime.Program.instance.setting.RefreshCharacterName();
            };

            var handle2 = Addressables.LoadAssetAsync<GameObject>("UI/ItemCharacter.prefab");
            handle2.Completed += (result) =>
            {
                characterItem = result.Result;
            };
        }

        public override void PerFrameFunction()
        {
            if (!NeedResponseInput())
                return;
            if (Runtime.UserInput.WasLeftShoulderPressed)
                GetUI<UI.ServantUI.CharacterSelectorUI>().TogglePlayer0.OnLeftSelection();
            if (Runtime.UserInput.WasRightShoulderPressed)
                GetUI<UI.ServantUI.CharacterSelectorUI>().TogglePlayer0.OnRightSelection();

            if (Runtime.UserInput.MouseRightDown || Runtime.UserInput.WasCancelPressed)
                OnReturn();
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
            else if (selected.TryGetComponent<UI.SelectionToggle_CharacterItem>(out _))
            {
                if (lastSelectedToggle != null)
                    EventSystem.current.SetSelectedGameObject(lastSelectedToggle.gameObject);
                else
                    servantUI.SelectDefaultSelectable();
            }
            else
                OnExit();
        }

        public override void OnExit()
        {
            if (Runtime.Program.instance.currentSubServant == this)
                Runtime.Program.instance.ShowSubServant(Runtime.Program.instance.setting);
            else
                Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.setting);
        }

        #endregion

        public void LoadCharacters()
        {
            characters.Initialize();
            characters.ChangeLanguage(MDPro3.Utility.Language.GetConfig());
        }

    }
}
