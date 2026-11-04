using MDPro3.Duel.YGOSharp;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;
using static MDPro3.UI.CardCollectionView;

namespace MDPro3.UI.Popup
{
    public class PopupSearchFilter : Popup
    {
        [Header("Popup SearchFilter")]
        public SelectionToggle_SearchFilter lastSelectedToggle;
        public TMP_InputField inputLevelFrom;
        public TMP_InputField inputLevelTo;
        public TMP_InputField inputAttackFrom;
        public TMP_InputField inputAttackTo;
        public TMP_InputField inputDefenceFrom;
        public TMP_InputField inputDefenceTo;
        public TMP_InputField inputScaleFrom;
        public TMP_InputField inputScaleTo;
        public TMP_InputField inputGPFrom;
        public TMP_InputField inputGPTo;
        public TMP_InputField inputYearFrom;
        public TMP_InputField inputYearTo;

        private void Start()
        {
            var f = CardCollectionView.filters;
            if (f.Count > 0)
            {
                foreach (var toggle in transform.GetComponentsInChildren<SelectionToggle_SearchFilter>())
                {
                    if (toggle.group == 0 && (toggle.filterCode & f[0]) > 0)
                        toggle.SetToggleOn();
                    else if (toggle.group == 1 && (toggle.filterCode & f[1]) > 0)
                        toggle.SetToggleOn();
                    else if (toggle.group == 2 && (toggle.filterCode & f[2]) > 0)
                        toggle.SetToggleOn();
                    else if (toggle.group == 3 && (toggle.filterCode & f[3]) > 0)
                        toggle.SetToggleOn();
                    else if (toggle.group == 4 && (toggle.filterCode & f[4]) > 0)
                        toggle.SetToggleOn();
                    else if (toggle.group == 5 && (toggle.filterCode & f[5]) > 0)
                        toggle.SetToggleOn();
                    else if (toggle.group == 6 && (toggle.filterCode & f[6]) > 0)
                        toggle.SetToggleOn();
                    else if (toggle.group == 7 && (toggle.filterCode & f[7]) > 0)
                        toggle.SetToggleOn();
                    else if (toggle.group == 8 && (toggle.filterCode & f[8]) > 0)
                        toggle.SetToggleOn();
                    else if (toggle.group == 9 && (toggle.filterCode & f[9]) > 0)
                        toggle.SetToggleOn();
                    else if (toggle.group == 10 && (toggle.filterCode & f[10]) > 0)
                        toggle.SetToggleOn();
                    else if (toggle.group == 11 && (toggle.filterCode & f[11]) > 0)
                        toggle.SetToggleOn();
                    else if (toggle.group == 12 && (toggle.filterCode & f[12]) > 0)
                        toggle.SetToggleOn();
                }

                if (f[13] > 0)
                    inputLevelFrom.text = f[13].ToString();
                if (f[14] > 0)
                    inputLevelTo.text = f[14].ToString();
                if (f[15] > 0)
                    inputAttackFrom.text = f[15].ToString();
                if (f[16] > 0)
                    inputAttackTo.text = f[16].ToString();
                if (f[17] > 0)
                    inputDefenceFrom.text = f[17].ToString();
                if (f[18] > 0)
                    inputDefenceTo.text = f[18].ToString();
                if (f[19] > 0)
                    inputScaleFrom.text = f[19].ToString();
                if (f[20] > 0)
                    inputScaleTo.text = f[20].ToString();
                if (f[21] >= 0)
                    inputGPFrom.text = f[21].ToString();
                if (f[22] >= 0)
                    inputGPTo.text = f[22].ToString();
                if (f[23] > 0)
                    inputYearFrom.text = f[23].ToString();
                if (f[24] > 0)
                    inputYearTo.text = f[24].ToString();
            }
            Manager.GetElement<SelectionButton>("ButtonPack")
                .SetButtonText(CardCollectionView.packName == string.Empty 
                ? InterString.Get("所有卡包") : CardCollectionView.packName);
        }

        public override void Show()
        {
            base.Show();
            AudioManager.PlaySE("SE_SYS_VERIFY");
        }

        protected override void OnDecide()
        {
            Hide();

            long type = 0;
            long attribute = 0;
            //ContinuousSpell = 0x8000000
            //ContinuousTrap = 0x10000000
            long spellType = 0;
            long race = 0;
            //NonEffect = 0x8000000
            long ability = 0;
            //Ban = 1
            //Limit = 2
            //SemiLimit = 4
            //NoLimit = 8
            long limit = 0;
            //OCG =1
            //TCG = 2
            //SCOCG = 8
            //DIY = 4
            //OCGOnly = 16
            //TCGOnly = 32
            //NonOnly = 64
            long pool = 0;
            long effect = 0;
            long rarity = 0;
            //Yes = 1
            //No = 2
            long cutin = 0;
            long video = 0;
            long overframe = 0;
            long link = 0;

            bool dirty = false;
            foreach (var toggle in transform.GetComponentsInChildren<SelectionToggle_SearchFilter>())
            {
                if (toggle.isOn)
                {
                    dirty = true;
                    if (toggle.group == 0)
                        type += toggle.filterCode;
                    else if (toggle.group == 1)
                        attribute += toggle.filterCode;
                    else if (toggle.group == 2)
                        spellType += toggle.filterCode;
                    else if (toggle.group == 3)
                        race += toggle.filterCode;
                    else if (toggle.group == 4)
                        ability += toggle.filterCode;
                    else if (toggle.group == 5)
                        limit += toggle.filterCode;
                    else if (toggle.group == 6)
                        pool += toggle.filterCode;
                    else if (toggle.group == 7)
                        effect += toggle.filterCode;
                    else if (toggle.group == 8)
                        rarity += toggle.filterCode;
                    else if (toggle.group == 9)
                        cutin += toggle.filterCode;
                    else if (toggle.group == 10)
                        video += toggle.filterCode;
                    else if (toggle.group == 11)
                        overframe += toggle.filterCode;
                    else if (toggle.group == 12)
                        link += toggle.filterCode;
                }
            }
            var filters = new List<long>()
            { type, attribute, spellType, race, ability, limit, pool, effect, rarity, cutin, video, overframe, link };
            if (inputLevelFrom.text.Length > 0)
            {
                dirty = true;
                filters.Add(long.Parse(inputLevelFrom.text));
            }
            else
                filters.Add(-233);
            if (inputLevelTo.text.Length > 0)
            {
                dirty = true;
                filters.Add(long.Parse(inputLevelTo.text));
            }
            else
                filters.Add(-233);
            if (inputAttackFrom.text.Length > 0)
            {
                dirty = true;
                filters.Add(long.Parse(inputAttackFrom.text));
            }
            else
                filters.Add(-233);
            if (inputAttackTo.text.Length > 0)
            {
                dirty = true;
                filters.Add(long.Parse(inputAttackTo.text));
            }
            else
                filters.Add(-233);
            if (inputDefenceFrom.text.Length > 0)
            {
                dirty = true;
                filters.Add(long.Parse(inputDefenceFrom.text));
            }
            else
                filters.Add(-233);
            if (inputDefenceTo.text.Length > 0)
            {
                dirty = true;
                filters.Add(long.Parse(inputDefenceTo.text));
            }
            else
                filters.Add(-233);
            if (inputScaleFrom.text.Length > 0)
            {
                dirty = true;
                filters.Add(long.Parse(inputScaleFrom.text));
            }
            else
                filters.Add(-233);
            if (inputScaleTo.text.Length > 0)
            {
                dirty = true;
                filters.Add(long.Parse(inputScaleTo.text));
            }
            else
                filters.Add(-233);
            if (inputGPFrom.text.Length > 0)
            {
                dirty = true;
                filters.Add(long.Parse(inputGPFrom.text));
            }
            else
                filters.Add(-233);
            if (inputGPTo.text.Length > 0)
            {
                dirty = true;
                filters.Add(long.Parse(inputGPTo.text));
            }
            else
                filters.Add(-233);
            if (inputYearFrom.text.Length > 0)
            {
                dirty = true;
                filters.Add(long.Parse(inputYearFrom.text));
            }
            else
                filters.Add(-233);
            if (inputYearTo.text.Length > 0)
            {
                dirty = true;
                filters.Add(long.Parse(inputYearTo.text));
            }
            else
                filters.Add(-233);

            var btnPack = Manager.GetElement<SelectionButton>("ButtonPack");
            if(btnPack.GetButtonText() != InterString.Get("所有卡包"))
            {
                dirty = true;
                CardCollectionView.packName = btnPack.GetButtonText();
            }

            if (dirty)
            {
                SelectionToggle_CardFilter.Instance.SetToggleOn();
                CardCollectionView.filters = filters;
            }
            else
            {
                SelectionToggle_CardFilter.Instance.SetToggleOff();
                CardCollectionView.filters.Clear();
            }

            var view = Program.instance.deckEditor.GetUI<DeckEditorUI>().CardCollectionView;
            if (view.area == Area.Collection)
                view.PrintSearchCards();
            else if (view.area == Area.Bookmark)
                view.PrintBookmarkCards();
        }

        public void OnReset()
        {
            foreach (var toggle in transform.GetComponentsInChildren<SelectionToggle_SearchFilter>())
                toggle.SetToggleOff();
            inputLevelFrom.text = string.Empty;
            inputLevelTo.text = string.Empty;
            inputAttackFrom.text = string.Empty;
            inputAttackTo.text = string.Empty;
            inputDefenceFrom.text = string.Empty;
            inputDefenceTo.text = string.Empty;
            inputScaleFrom.text = string.Empty;
            inputScaleTo.text = string.Empty;
            inputGPFrom.text = string.Empty;
            inputGPTo.text = string.Empty;
            inputYearFrom.text = string.Empty;
            inputYearTo.text = string.Empty;
            Manager.GetElement<SelectionButton>("ButtonPack").SetButtonText(InterString.Get("所有卡包"));
        }

        public void OnPack()
        {
            var selections = new List<string>()
            {
                InterString.Get("卡包"),
                string.Empty
            };
            foreach (var pack in PacksManager.packs)
                selections.Add(pack.fullName);
            UIManager.ShowPopupSelection(selections, OnPackSelect, OnPackClose);
        }

        private void OnPackSelect()
        {
            string selected = UnityEngine.EventSystems.EventSystem.current.
                currentSelectedGameObject.GetComponent<SelectionButton>().GetButtonText();
            Manager.GetElement<SelectionButton>("ButtonPack").SetButtonText(selected);
        }
        private void OnPackClose()
        {
            //Program.instance.currentServant.returnAction = Hide;
        }

        protected override void Update()
        {
            if (!NeedResponseInput())
                return;

            if ((UserInput.MouseRightDown || UserInput.WasCancelPressed) && cancelCallHide)
            {
                AudioManager.PlaySE("SE_MENU_CANCEL");
                Hide();
            }

            if (UserInput.WasGamepadButtonWestPressed)
            {
                AudioManager.PlaySE("SE_MENU_DECIDE");
                if (UserInput.WasLeftShoulderPressing)
                    OnReset();
                else
                    OnDecide();
            }
        }
    }
}

