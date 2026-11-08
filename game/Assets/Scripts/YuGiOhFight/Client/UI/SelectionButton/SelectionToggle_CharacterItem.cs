using UnityEngine;
using UnityEngine.AddressableAssets;

using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
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
            if (Controllers.CharacterSelector.characters == null)
                return;

            charaName = Controllers.CharacterSelector.characters.GetName(characterID);
            charaProfile = Controllers.CharacterSelector.characters.GetProfile(characterID);
            charaProfile = MDPro3.Cid2Ydk.ReplaceWithCardName(charaProfile);

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
            Runtime.Program.instance.character.GetUI<ServantUI.CharacterSelectorUI>().SetHoverText(charaName);
        }

        protected override void CallToggleOnEvent()
        {
            base.CallToggleOnEvent();
            CallHoverOnEvent();
            Runtime.Program.instance.character.GetUI<ServantUI.CharacterSelectorUI>().TextDetailName.text = charaName;
#if UNITY_EDITOR
            Program.instance.character.GetUI<CharacterSelectorUI>().TextDetailName.text += $" ({characterID})";
#endif
            Runtime.Program.instance.character.GetUI<ServantUI.CharacterSelectorUI>().TextDetailDescription.text = charaProfile.Replace("\n", string.Empty);
            MDPro3.Config.Set(Controllers.CharacterSelector.condition + "Character" + ServantUI.CharacterSelectorUI.player, characterID);
            Runtime.Program.instance.ocgcore.CheckCharaFace();
            Runtime.Program.instance.character.lastSelectedCharacter = this;
            Runtime.Program.instance.currentServant.lastSelectable = Selectable;
            GetSelectable().Select();

            var detailImage = Runtime.Program.instance.character.GetUI<ServantUI.CharacterSelectorUI>().ImageDetail;
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
            Runtime.AudioManager.PlaySE(SoundLabelClick);
            SetToggleOn();
            Runtime.Program.instance.currentServant.lastSelectable = Selectable;
        }

        protected override int GetButtonsCount()
        {
            return Runtime.Program.instance.character.GetUI<ServantUI.CharacterSelectorUI>().GetCurrentSerialCount();
        }

        private static GridLayoutGroup m_grid;
        private static GridLayoutGroup Grid =>
            m_grid = m_grid != null ? m_grid
            : Runtime.Program.instance.character.GetUI<ServantUI.CharacterSelectorUI>()
            .ScrollRect.GetComponent<GridLayoutGroup>();

        protected override int GetColumnsCount()
        {
            return Grid.Size().x;
        }
    }
}
