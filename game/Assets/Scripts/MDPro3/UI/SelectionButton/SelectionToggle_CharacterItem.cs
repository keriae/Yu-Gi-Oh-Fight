using System;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;

namespace MDPro3.UI
{
    public class SelectionToggle_CharacterItem : SelectionToggle
    {
        public string characterID;
        private string charaName;
        private string charaProfile;

        protected override void Awake()
        {
            base.Awake();
            HoverOff();
            exclusiveToggle = true;
            canToggleOffSelf = false;
            manuallySetNavigation = false;
        }

        public void Refresh()
        {
            if (CharacterSelector.characters == null)
                return;

            charaName = CharacterSelector.characters.GetName(characterID);
            charaProfile = CharacterSelector.characters.GetProfile(characterID);
            charaProfile = Cid2Ydk.ReplaceWithCardName(charaProfile);

            var handle = Addressables.LoadAssetAsync<Sprite>("sn" + characterID);
            handle.Completed += (result) =>
            {
                var icon = Manager.GetElement<Image>("Out");
                icon.sprite = result.Result;
                icon.color = Color.white;
            };
        }

        protected override void CallHoverOnEvent()
        {
            base.CallHoverOnEvent();
            Program.instance.character.GetUI<CharacterSelectorUI>().SetHoverText(charaName);
        }

        protected override void CallToggleOnEvent()
        {
            base.CallToggleOnEvent();
            CallHoverOnEvent();
            Program.instance.character.GetUI<CharacterSelectorUI>().TextDetailName.text = charaName;
#if UNITY_EDITOR
            Program.instance.character.GetUI<CharacterSelectorUI>().TextDetailName.text += $" ({characterID})";
#endif
            Program.instance.character.GetUI<CharacterSelectorUI>().TextDetailDescription.text = charaProfile.Replace("\n", string.Empty);
            Config.Set(CharacterSelector.condition + "Character" + CharacterSelectorUI.player, characterID);
            Program.instance.ocgcore.CheckCharaFace();
            Program.instance.character.lastSelectedCharacter = this;
            Program.instance.currentServant.lastSelectable = Selectable;
            GetSelectable().Select();

            var detailImage = Program.instance.character.GetUI<CharacterSelectorUI>().ImageDetail;
            detailImage.color = Color.clear;

            var handle = Addressables.LoadAssetAsync<Sprite>("sn" + characterID + "_2");
            handle.Completed += (result) =>
            {
                if (result.Result == null)
                    return;
                detailImage.color = Color.white;
                detailImage.sprite = result.Result;
            };
        }

        protected override void OnClick()
        {
            AudioManager.PlaySE(SoundLabelClick);
            SetToggleOn();
            Program.instance.currentServant.lastSelectable = Selectable;
        }

        protected override int GetButtonsCount()
        {
            return Program.instance.character.GetUI<CharacterSelectorUI>().GetCurrentSerialCount();
        }

        private static GridLayoutGroup m_grid;
        private static GridLayoutGroup Grid =>
            m_grid = m_grid != null ? m_grid
            : Program.instance.character.GetUI<CharacterSelectorUI>()
            .ScrollRect.GetComponent<GridLayoutGroup>();

        protected override int GetColumnsCount()
        {
            return Grid.Size().x;
        }
    }
}
