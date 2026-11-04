using MDPro3.Servant;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MDPro3.Servant.CharacterSelector;
using MDPro3.Duel;

namespace MDPro3.UI.ServantUI
{
    public class CharacterSelectorUI : ServantUI
    {

        #region Elements

        private const string LABEL_SR = "ScrollRect";
        private ScrollRect m_ScrollRect;
        public ScrollRect ScrollRect =>
            m_ScrollRect = m_ScrollRect != null ? m_ScrollRect
            : Manager.GetElement<ScrollRect>(LABEL_SR);

        private const string LABEL_TXT_HOVER = "TextHover";
        private TextMeshProUGUI m_TextHover;
        private TextMeshProUGUI TextHover =>
            m_TextHover = m_TextHover != null ? m_TextHover
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_HOVER);

        private const string LABEL_IMG_DETAIL = "ImageDetail";
        private Image m_ImageDetail;
        public Image ImageDetail =>
            m_ImageDetail = m_ImageDetail != null ? m_ImageDetail
            : Manager.GetElement<Image>(LABEL_IMG_DETAIL);

        private const string LABEL_TXT_DETAILNAME = "TextDetailName";
        private TextMeshProUGUI m_TextDetailName;
        public TextMeshProUGUI TextDetailName =>
            m_TextDetailName = m_TextDetailName != null ? m_TextDetailName
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_DETAILNAME);

        private const string LABEL_TXT_DETAILDESCRIPTION = "TextDetailDescription";
        private TextMeshProUGUI m_TextDetailDescription;
        public TextMeshProUGUI TextDetailDescription =>
            m_TextDetailDescription = m_TextDetailDescription != null ? m_TextDetailDescription
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_DETAILDESCRIPTION);

        private const string LABEL_STG_PAGE00 = "Page00";
        private SelectionToggle_CharacterSeries m_TogglePage00;
        private SelectionToggle_CharacterSeries TogglePage00 =>
            m_TogglePage00 = m_TogglePage00 != null ? m_TogglePage00
            : Manager.GetElement<SelectionToggle_CharacterSeries>(LABEL_STG_PAGE00);

        private const string LABEL_STG_PAGE01 = "Page01";
        private SelectionToggle_CharacterSeries m_TogglePage01;
        private SelectionToggle_CharacterSeries TogglePage01 =>
            m_TogglePage01 = m_TogglePage01 != null ? m_TogglePage01
            : Manager.GetElement<SelectionToggle_CharacterSeries>(LABEL_STG_PAGE01);

        private const string LABEL_STG_PAGE02 = "Page02";
        private SelectionToggle_CharacterSeries m_TogglePage02;
        private SelectionToggle_CharacterSeries TogglePage02 =>
            m_TogglePage02 = m_TogglePage02 != null ? m_TogglePage02
            : Manager.GetElement<SelectionToggle_CharacterSeries>(LABEL_STG_PAGE02);

        private const string LABEL_STG_PAGE03 = "Page03";
        private SelectionToggle_CharacterSeries m_TogglePage03;
        private SelectionToggle_CharacterSeries TogglePage03 =>
            m_TogglePage03 = m_TogglePage03 != null ? m_TogglePage03
            : Manager.GetElement<SelectionToggle_CharacterSeries>(LABEL_STG_PAGE03);

        private const string LABEL_STG_PAGE04 = "Page04";
        private SelectionToggle_CharacterSeries m_TogglePage04;
        private SelectionToggle_CharacterSeries TogglePage04 =>
            m_TogglePage04 = m_TogglePage04 != null ? m_TogglePage04
            : Manager.GetElement<SelectionToggle_CharacterSeries>(LABEL_STG_PAGE04);

        private const string LABEL_STG_PAGE05 = "Page05";
        private SelectionToggle_CharacterSeries m_TogglePage05;
        private SelectionToggle_CharacterSeries TogglePage05 =>
            m_TogglePage05 = m_TogglePage05 != null ? m_TogglePage05
            : Manager.GetElement<SelectionToggle_CharacterSeries>(LABEL_STG_PAGE05);

        private const string LABEL_STG_PAGE06 = "Page06";
        private SelectionToggle_CharacterSeries m_TogglePage06;
        private SelectionToggle_CharacterSeries TogglePage06 =>
            m_TogglePage06 = m_TogglePage06 != null ? m_TogglePage06
            : Manager.GetElement<SelectionToggle_CharacterSeries>(LABEL_STG_PAGE06);

        private const string LABEL_STG_PAGE07 = "Page07";
        private SelectionToggle_CharacterSeries m_TogglePage07;
        private SelectionToggle_CharacterSeries TogglePage07 =>
            m_TogglePage07 = m_TogglePage07 != null ? m_TogglePage07
            : Manager.GetElement<SelectionToggle_CharacterSeries>(LABEL_STG_PAGE07);

        private const string LABEL_STG_PAGE08 = "Page08";
        private SelectionToggle_CharacterSeries m_TogglePage08;
        private SelectionToggle_CharacterSeries TogglePage08 =>
            m_TogglePage08 = m_TogglePage08 != null ? m_TogglePage08
            : Manager.GetElement<SelectionToggle_CharacterSeries>(LABEL_STG_PAGE08);

        private const string LABEL_STG_PLAYER0 = "TogglePlayer0";
        private SelectionToggle_CharacterPlayer m_TogglePlayer0;
        public SelectionToggle_CharacterPlayer TogglePlayer0 =>
            m_TogglePlayer0 = m_TogglePlayer0 != null ? m_TogglePlayer0
            : Manager.GetElement<SelectionToggle_CharacterPlayer>(LABEL_STG_PLAYER0);

        #endregion

        public static string player = "0";
        private string currentSerial = "00";

        private static readonly List<GameObject> dm = new();
        private static readonly List<GameObject> gx = new();
        private static readonly List<GameObject> _5ds = new();
        private static readonly List<GameObject> dsod = new();
        private static readonly List<GameObject> zexal = new();
        private static readonly List<GameObject> arcv = new();
        private static readonly List<GameObject> vrains = new();
        private static readonly List<GameObject> sevens = new();
        private static readonly List<GameObject> npc = new();
        private static readonly List<GameObject> gorush = new();

        private List<GameObject> targetItems = new();

        private readonly Dictionary<string, List<GameObject>> pools = new()
        {
            { "00", dm},
            { "01", gx},
            { "02", _5ds},
            { "03", dsod},
            { "04", zexal},
            { "05", arcv},
            { "06", vrains},
            { "07", sevens},
            { "08", npc},
            { "09", gorush}
        };


        public override void ShowEvent()
        {
            base.ShowEvent();

            switch (condition)
            {
                case Condition.Duel:
                    Title.text = InterString.Get("决斗角色");
                    break;
                case Condition.Watch:
                    Title.text = InterString.Get("观战角色");
                    break;
                case Condition.Replay:
                    Title.text = InterString.Get("回放角色");
                    break;
            }

            TogglePlayer0.SetToggleOn();
        }

        protected override void AfterHideEvent()
        {
            base.AfterHideEvent();

            foreach (var pool in pools)
                foreach (var c in pool.Value)
                    Destroy(c);
            foreach (var pool in pools)
                pool.Value.Clear();
        }

        public void SwitchPlayer(string player)
        {
            CharacterSelectorUI.player = player;

            var configCharacter = Config.Get(condition + "Character" + player, VoicePlayer.defaultCharacter);
            var configSeries = characters.GetCharacterSeries(configCharacter);
            Manager.GetElement<SelectionToggle_CharacterSeries>("Page" + configSeries).SetToggleOn();
        }

        public void ShowCharacters(string serial)
        {
            currentSerial = serial;

            if (characters == null || characterItem == null)
                return;

            foreach (var pool in pools)
            {
                if (pool.Key != currentSerial)
                {
                    foreach (var character in pool.Value)
                        character.SetActive(false);
                }
                else
                    targetItems = pool.Value;
            }

            if (targetItems.Count == 0)
            {
                var targetCharacters = characters.GetSeriesCharacters(currentSerial);
                int count = 0;
                for (int i = 0; i < targetCharacters.Count; i++)
                {
                    if (targetCharacters[i].notReady)
                        continue;
                    var item = Instantiate(characterItem);
                    var mono = item.GetComponent<SelectionToggle_CharacterItem>();
                    mono.index = count;
                    mono.characterID = targetCharacters[i].id;
                    mono.Refresh();
                    item.transform.SetParent(Manager.GetElement<ScrollRect>("ScrollRect").content, false);
                    targetItems.Add(item);
                    count++;
                }
            }

            foreach (var item in targetItems)
            {
                item.SetActive(true);
                var mono = item.GetComponent<SelectionToggle_CharacterItem>();
                var config = Config.Get(condition + "Character" + player, VoicePlayer.defaultCharacter);

                if (mono.characterID == config)
                    mono.SetToggleOn();
            }
        }

        public int GetCurrentSerialCount()
        {
            if (characters == null || characterItem == null)
                return 0;

            foreach (var pool in pools)
                if (pool.Key == currentSerial)
                    return pool.Value.Count;

            return 0;
        }


        public void SetHoverText(string txt)
        {
            TextHover.text = txt;
        }

        public GameObject GetFirstActiveCharacterItem()
        {
            for (int i = 0; i < ScrollRect.content.childCount; i++)
                if (ScrollRect.content.GetChild(i).gameObject.activeSelf)
                    return ScrollRect.content.GetChild(i).gameObject;
            return null;
        }
    }
}
