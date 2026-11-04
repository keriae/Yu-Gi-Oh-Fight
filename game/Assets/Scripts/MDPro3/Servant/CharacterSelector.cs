using DG.Tweening;
using MDPro3;
using MDPro3.UI;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using MDPro3.Utility;
using UnityEngine.EventSystems;
using MDPro3.UI.ServantUI;

namespace MDPro3.Servant
{
    public class CharacterSelector : Servant
    {
        [HideInInspector] public SelectionToggle_CharacterSeries lastSelectedToggle;
        [HideInInspector] public SelectionToggle_CharacterItem lastSelectedCharacter;

        public static Characters characters;
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

            var handle = Addressables.LoadAssetAsync<Characters>("ScriptableObjects/Characters.asset");
            handle.Completed += (result) =>
            {
                characters = result.Result;
                LoadCharacters();
                Program.instance.setting.RefreshCharacterName();
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
            if (UserInput.WasLeftShoulderPressed)
                GetUI<CharacterSelectorUI>().TogglePlayer0.OnLeftSelection();
            if (UserInput.WasRightShoulderPressed)
                GetUI<CharacterSelectorUI>().TogglePlayer0.OnRightSelection();

            if (UserInput.MouseRightDown || UserInput.WasCancelPressed)
                OnReturn();
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !UserInput.NeedDefaultSelect())
                return;

            if (lastSelectable != null)
            {
                if (lastSelectable.TryGetComponent<SelectionToggle_CharacterItem>(out _)
                    || lastSelectable.TryGetComponent<SelectionToggle_CharacterSeries>(out _))
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
            AudioManager.PlaySE("SE_MENU_CANCEL");
            GameObject selected = EventSystem.current.currentSelectedGameObject;

            if (selected == null)
                OnExit();
            else if (Cursor.lockState == CursorLockMode.None)
                OnExit();
            else if (selected.TryGetComponent<SelectionToggle_CharacterItem>(out _))
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
            if (Program.instance.currentSubServant == this)
                Program.instance.ShowSubServant(Program.instance.setting);
            else
                Program.instance.ShiftToServant(Program.instance.setting);
        }

        #endregion

        public void LoadCharacters()
        {
            characters.Initialize();
            characters.ChangeLanguage(Language.GetConfig());
        }

    }
}
