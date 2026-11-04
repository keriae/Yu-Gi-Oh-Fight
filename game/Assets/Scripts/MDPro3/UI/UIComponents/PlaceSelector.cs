using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MDPro3.GameCard;

namespace MDPro3.UI
{
    public class PlaceSelector : MonoBehaviour
    {
        public GPS p;

        public List<DuelButtonInfo> buttons = new List<DuelButtonInfo>();
        public List<DuelButton> buttonObjs = new List<DuelButton>();

        private DuelButton selectButton;

        private GameObject highlight;
        private GameObject select;
        private GameObject selectPush;
        private GameObject selectCard;
        private GameObject selectCardPush;
        private GameObject disable;
        public GameCard cookieCard;

        private bool hover;
        private bool selecting;
        private bool selected;

        public bool cardSelecting;
        public bool cardSelected;
        private bool cardPreselected;
        private bool cardUnselectable;

        private bool countShowing;
        private bool buttonsCreated = false;
        private GameObject hintObj;

        private void Start()
        {
            var collider = gameObject.AddComponent<BoxCollider>();
            if (p.InLocation(CardLocation.Deck))
            {
                highlight = ABLoader.LoadMasterDuelGameObject("eff_duel_highlight10");
                transform.localEulerAngles = new Vector3(0, -19.5f, 0);
                collider.size = new Vector3(8f, 1f, 10f);
            }
            else if (p.InLocation(CardLocation.Extra))
            {
                highlight = ABLoader.LoadMasterDuelGameObject("eff_duel_highlight10");
                transform.localEulerAngles = new Vector3(0, 19.5f, 0);
                collider.size = new Vector3(8f, 1f, 10f);
            }
            else if (p.InLocation(CardLocation.MonsterZone))
            {
                highlight = ABLoader.LoadMasterDuelGameObject("eff_duel_highlight11");
                collider.size = new Vector3(8f, 1f, 8f);
                select = ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_mst_001");
                selectPush = ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_mst_Push_001");
                selectCard = ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_card_001");
                selectCardPush = ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_card_Push_001");
                disable = new GameObject("Disable");
                CreateSelectButton();
            }
            else if (p.InLocation(CardLocation.SpellZone))
            {
                if (p.sequence == 5)
                {
                    highlight = ABLoader.LoadMasterDuelGameObject("eff_duel_highlight13");
                    collider.size = new Vector3(6f, 1f, 7f);
                    select = ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_card_001");
                    selectPush = ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_card_Push_001");
                    selectCard = ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_card_001");
                    selectCardPush = ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_card_Push_001");
                    select.transform.localScale = Vector3.one * 0.8f;
                    selectPush.transform.localScale = Vector3.one * 0.8f;
                }
                else
                {
                    highlight = ABLoader.LoadMasterDuelGameObject("eff_duel_highlight12");
                    collider.size = new Vector3(8f, 1f, 7f);
                    select = ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_trpmgc_001");
                    selectPush = ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_trpmgc_Push_001");
                    selectCard = ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_card_001");
                    selectCardPush = ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_card_Push_001");
                }
                selectCard.transform.localScale = Vector3.one * 0.8f;
                selectCardPush.transform.localScale = Vector3.one * 0.8f;
                disable = new GameObject("Disable");
                CreateSelectButton();
            }

            highlight.transform.SetParent(transform, false);
            transform.localPosition = GetCardPosition(p);
            if (select != null)
            {
                select.transform.SetParent(transform, false);
                selectPush.transform.SetParent(transform, false);
                selectCard.transform.SetParent(transform, false);
                selectCardPush.transform.SetParent(transform, false);
                var main = select.GetComponent<ParticleSystem>().main;
                main.playOnAwake = true;
                main = selectPush.GetComponent<ParticleSystem>().main;
                main.playOnAwake = true;
                main = selectCard.GetComponent<ParticleSystem>().main;
                main.playOnAwake = true;
                main = selectCardPush.GetComponent<ParticleSystem>().main;
                main.playOnAwake = true;
                select.SetActive(false);
                selectPush.SetActive(false);
                selectCard.SetActive(false);
                selectCardPush.SetActive(false);
            }
            if (disable != null)
            {
                disable.transform.SetParent(transform, false);
                disable.transform.localEulerAngles = new Vector3(90, 0, 0);
                disable.transform.localScale = new Vector3(3, 3, 1);
                var spriteRenderer = disable.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = TextureManager.container.CardAffectDisable;
                disable.SetActive(false);
            }
        }

        private void Update()
        {
            hover = false;
            if (UserInput.HoverObject == gameObject)
                hover = true;

            if (hover)
            {
                highlight.SetActive(true);
                if (UserInput.MouseLeftUp)
                    OnClick();
                if ((p.location & (uint)CardLocation.Onfield) == 0 && !countShowing)
                {
                    countShowing = true;
                    Program.instance.ocgcore.GetUI<OcgCoreUI>().ShowLocationCount(p);
                }
            }
            else
            {
                if (UserInput.MouseLeftUp)
                    HideButtons();

                highlight.SetActive(false);
                if (countShowing)
                {
                    countShowing = false;
                    Program.instance.ocgcore.GetUI<OcgCoreUI>().HidePlaceCount();
                }
            }

            if (UserInput.HoverObject == gameObject)
                highlight.SetActive(true);
            else
                highlight.SetActive(false);
        }

        public void Reset()
        {
            if(disable != null)
                disable.SetActive(false);
        }

        private void OnClick()
        {
            if (selecting)
            {
                AudioManager.PlaySE("SE_DUEL_SELECT");
                if (!selected)
                {
                    selected = true;
                    select.SetActive(false);
                    selectPush.SetActive(false);
                    selectPush.SetActive(true);
                }
                else
                {
                    selected = false;
                    select.SetActive(true);
                }
                var selectedCount = 0;
                foreach(var place in Program.instance.ocgcore.Places)
                    if(place.selected)
                        selectedCount++;
                if (selectedCount == OcgCore.ES_min)
                {
                    var binaryMaster = new BinaryMaster();
                    foreach (var place in Program.instance.ocgcore.Places)
                        if (place.selected)
                        {
                            var response = new byte[3];
                            response[0] = OcgCore.isFirst ? (byte)place.p.controller : (byte)(1 - place.p.controller);
                            response[1] = (byte)place.p.location;
                            response[2] = (byte)place.p.sequence;
                            binaryMaster.writer.Write(response);
                        }
                    Program.instance.ocgcore.SendReturn(binaryMaster.Get());
                }
            }
            else if (cardSelecting)
            {
                AudioManager.PlaySE("SE_DUEL_SELECT");
                if (OcgCore.currentMessage == GameMessage.SelectCounter)
                {
                    if (!cardUnselectable)
                        selectButton.Show();
                }
                else if (!cardSelected && !cardUnselectable && !cardPreselected)
                    selectButton.Show();
                else if (cardSelected && !cardUnselectable && !cardPreselected)
                    UnselectCardInThisZone();
                var card = FindCardInThisPlace();
                if (card != null)
                    card.OnClick();
            }
            else
            {
                if (p.InLocation(CardLocation.SpellZone))
                {
                    if (p.InMyControl())
                    {
                        OcgCore.HideMyHandCard = true;
                        OcgCore.HideOpHandCard = false;
                    }
                    else
                    {
                        OcgCore.HideOpHandCard = true;
                        OcgCore.HideMyHandCard = false;
                    }
                }
                else
                {
                    OcgCore.HideMyHandCard = false;
                    OcgCore.HideOpHandCard = false;
                }

                if (p.InLocation(CardLocation.Onfield))
                {
                    var card = FindCardInThisPlace();
                    if (card != null)
                        card.OnClick();
                    else
                    {
                        Program.instance.ocgcore.GetUI<OcgCoreUI>().CardDescription.Hide();
                        Program.instance.ocgcore.GetUI<OcgCoreUI>().CardList.Hide();
                    }
                    foreach (var c in OcgCore.cards)
                        if (c != card)
                            c.NotClickThis();
                }
                else
                {
                    AudioManager.PlaySE("SE_DUEL_SELECT");
                    List<GameCard> cards = new();
                    foreach (var card in OcgCore.cards)
                        if ((card.p.location & p.location) > 0)
                            if (card.p.controller == p.controller)
                                cards.Add(card);
                    Program.instance.ocgcore.GetUI<OcgCoreUI>().CardList.Show(cards, (CardLocation)p.location, (int)p.controller);

                    if (!buttonsCreated)
                    {
                        bool spsummmon = false;
                        bool activate = false;
                        foreach (var card in OcgCore.cards)
                            if ((card.p.location & p.location) > 0)
                                if (card.p.controller == p.controller)
                                    foreach (var btn in card.buttons)
                                    {
                                        if (btn.type == ButtonType.Activate)
                                            activate = true;
                                        if (btn.type == ButtonType.SpSummon)
                                            spsummmon = true;
                                    }
                        if (activate)
                        {
                            int response = -1;
                            buttons.Add(new DuelButtonInfo() { response = new List<int>() { response }, hint = InterString.Get("发动效果"), type = ButtonType.Activate });
                        }
                        if (spsummmon)
                        {
                            int response = -2;
                            buttons.Add(new DuelButtonInfo() { response = new List<int>() { response }, hint = InterString.Get("特殊召唤"), type = ButtonType.SpSummon });
                        }
                        CreateButtons();
                    }
                    else
                    {
                        if (Program.instance.ocgcore.returnAction == null)
                            ShowButtons();
                    }
                }
            }
        }

        private void CreateButtons()
        {
            if (buttonsCreated || Program.instance.ocgcore.returnAction != null || buttons.Count == 0)
            {
                buttons.Clear();
                return;
            }

            for (int i = 0; i < buttons.Count; i++)
            {
                var obj = ABLoader.LoadMasterDuelGameObject("DuelButton");
                var mono = obj.GetComponent<DuelButton>();
                buttonObjs.Add(mono);
                mono.response = buttons[i].response;
                mono.hint = buttons[i].hint;
                mono.type = buttons[i].type;
                mono.id = i;
                mono.buttonsCount = buttons.Count;
                mono.cookieCard = null;
                mono.location = p.location;
                mono.controller = p.controller;
                mono.Show();
            }
            buttonsCreated = true;
        }

        private void CreateSelectButton()
        {
            var obj = ABLoader.LoadMasterDuelGameObject("DuelButton");
            selectButton = obj.GetComponent<DuelButton>();
            selectButton.response.Add(-3);
            selectButton.hint = "";
            selectButton.type = ButtonType.Select;
            selectButton.id = 0;
            selectButton.buttonsCount = 1;
            selectButton.cookieCard = null;
            selectButton.location = p.location;
            selectButton.controller = p.controller;
            selectButton.sequence = p.sequence;
            selectButton.Hide();
        }

        public void ShowButtons()
        {
            foreach (var button in buttonObjs)
                button.Show();
        }

        public void HideButtons()
        {
            foreach (var button in buttonObjs)
                button.Hide();
            if (selectButton != null)
                selectButton.Hide();
        }

        public void ClearButtons()
        {
            foreach (var go in buttonObjs)
                Destroy(go.gameObject);
            buttonObjs.Clear();
            buttons.Clear();
            buttonsCreated = false;
        }

        public void StopResponse()
        {
            if (selecting)
            {
                selecting = false;
                select.SetActive(false);
                if (selected)
                {
                    selectPush.SetActive(false);
                    selectPush.SetActive(true);
                    selected = false;
                }
            }
            if (cardSelecting)
            {
                cardSelecting = false;
                cardSelected = false;
                selectCard.SetActive(false);
                cookieCard = null;
                cardUnselectable = false;
                cardPreselected = false;
            }
        }

        public void InitializeSelectCardInThisZone(List<GameCard> cards)
        {
            foreach (var card in cards)
            {
                if (card.p.controller == p.controller)
                {
                    if (card.p.location == p.location)
                        if (card.p.sequence == p.sequence)
                        {
                            cardSelecting = true;
                            cookieCard = card;
                            ShowSelectCardHighlight();
                            break;
                        }
                }
                else
                {
                    if ((p.location & (uint)CardLocation.MonsterZone) > 0
                        && p.sequence == 5
                        && card.p.controller == 1
                        && (card.p.location & (uint)CardLocation.MonsterZone) > 0
                        && card.p.sequence == 6
                        )
                    {
                        cardSelecting = true;
                        cookieCard = card;
                        ShowSelectCardHighlight();
                        break;
                    }
                    if ((p.location & (uint)CardLocation.MonsterZone) > 0
                        && p.sequence == 6
                        && card.p.controller == 1
                        && (card.p.location & (uint)CardLocation.MonsterZone) > 0
                        && card.p.sequence == 5
                        )
                    {
                        cardSelecting = true;
                        cookieCard = card;
                        ShowSelectCardHighlight();
                        break;
                    }
                }
            }
            if (cardSelecting && OcgCore.currentMessage == GameMessage.SelectSum)
            {
                if (OcgCore.cardsMustBeSelected.Contains(cookieCard))
                {
                    cardPreselected = true;
                    cardSelected = true;
                    selectCard.SetActive(false);
                }
            }
        }

        public void SelectCardInThisZone()
        {
            cardSelected = true;

            if (OcgCore.currentMessage != GameMessage.SelectCounter)
                selectCard.SetActive(false);
            selectCardPush.SetActive(false);
            selectCardPush.SetActive(true);
            Program.instance.ocgcore.FieldSelectRefresh(cookieCard);
        }

        public void UnselectCardInThisZone()
        {
            if (OcgCore.currentMessage == GameMessage.SelectCounter)
                return;
            cardSelected = false;
            Program.instance.ocgcore.FieldSelectRefresh(cookieCard);
        }

        public void CardInThisZoneSelectable()
        {
            cardUnselectable = false;
            selectCard.SetActive(true);
        }

        public void CardInThisZoneUnselectable()
        {
            cardUnselectable = true;
            selectCard.SetActive(false);
        }

        public GPS HighlightThisZone(uint place, int min)
        {
            for (var i = 0; i < min; i++)
            {
                uint passController;
                if (p.controller == 0)
                    passController = place & 0xFFFF;
                else
                    passController = place >> 16;

                if ((passController & 0x7F) > 0)
                {
                    if ((p.location & (uint)CardLocation.MonsterZone) > 0)
                    {
                        var filter = passController & 0x7F;
                        if ((filter & (1u << (int)p.sequence)) > 0)
                        {
                            ShowSelectZoneHighlight();
                            selecting = true;
                            return p;
                        }
                    }
                }
                if ((passController & 0x3F00) > 0)
                {
                    if ((p.location & (uint)CardLocation.SpellZone) > 0)
                    {
                        var filter = passController >> 8;
                        if ((filter & (1u << (int)p.sequence)) > 0)
                        {
                            ShowSelectZoneHighlight();
                            selecting = true;
                            return p;
                        }
                    }
                }
            }
            return null;
        }

        public void ShowSelectZoneHighlight()
        {
            select.SetActive(true);
        }
        public void ShowSelectCardHighlight()
        {
            selectCard.SetActive(true);
            if ((cookieCard.p.position & (uint)CardPosition.Attack) > 0)
            {
                selectCard.transform.localEulerAngles = Vector3.zero;
                selectCardPush.transform.localEulerAngles = Vector3.zero;
            }
            else
            {
                selectCard.transform.localEulerAngles = new Vector3(0, 90, 0);
                selectCardPush.transform.localEulerAngles = new Vector3(0, 90, 0);
            }

        }

        public GameCard FindCardInThisPlace()
        {
            if ((p.location & (uint)CardLocation.MonsterZone) > 0 && p.sequence == 5)
                foreach (var card in OcgCore.cards)
                {
                    if ((card.p.location & (uint)CardLocation.MonsterZone) > 0)
                    {
                        if (card.p.controller == 0 && card.p.sequence == 5
                            && (card.p.location & (uint)CardLocation.Overlay) == 0)
                            return card;
                        if (card.p.controller == 1 && card.p.sequence == 6
                            && (card.p.location & (uint)CardLocation.Overlay) == 0)
                            return card;
                    }
                }
            else if ((p.location & (uint)CardLocation.MonsterZone) > 0 && p.sequence == 6)
                foreach (var card in OcgCore.cards)
                {
                    if ((card.p.location & (uint)CardLocation.MonsterZone) > 0)
                    {
                        if (card.p.controller == 0 && card.p.sequence == 6
                            && (card.p.location & (uint)CardLocation.Overlay) == 0)
                            return card;
                        if (card.p.controller == 1 && card.p.sequence == 5
                            && (card.p.location & (uint)CardLocation.Overlay) == 0)
                            return card;
                    }
                }
            else
                foreach (var card in OcgCore.cards)
                    if (p.controller == card.p.controller)
                        if ((card.p.location & (uint)CardLocation.Overlay) == 0)
                            if (p.location == card.p.location)
                                if (p.sequence == card.p.sequence)
                                    return card;

            return null;
        }

        public void ShowHint(uint location, uint controller)
        {
            if ((location & p.location) > 0 && controller == p.controller)
            {
                hintObj = ABLoader.LoadMasterDuelGameObject("fxp_HL_EXdeck_001");
                hintObj.transform.SetParent(transform, false);
                int cardCount = Program.instance.ocgcore.GetLocationCardCount((CardLocation)location, controller);
                hintObj.transform.localScale = new Vector3(1f, cardCount * 0.1f, 1f);
            }
        }

        public void HideHint()
        {
            if (hintObj != null)
                Destroy(hintObj);
        }

        public void SetDisabled(uint filter)
        {
            if ((p.location & (uint)CardLocation.Onfield) == 0)
                return;

            if (p.location == (uint)CardLocation.MonsterZone && (p.sequence == 5 || p.sequence == 6))
                return;

            int order = 0;
            if (p.controller != 0 && OcgCore.isFirst
                || p.controller == 0 && !OcgCore.isFirst)
                order += 16;

            if (p.location == (uint)CardLocation.SpellZone)
                order += 8;
            order += (int)p.sequence;
            if ((filter & (1 << order)) > 0)
                disable.SetActive(true);
            else
                disable.SetActive(false);
        }

        public bool InTheSameLine(GPS gps)
        {
            if ((gps.location & ((uint)CardLocation.Deck + (uint)CardLocation.Extra)) > 0)
                return false;
            if ((p.location & ((uint)CardLocation.Deck + (uint)CardLocation.Extra)) > 0)
                return false;

            if ((p.sequence == gps.sequence) && (p.controller == gps.controller))
                return true;
            if ((p.sequence == (4 - gps.sequence)) && (p.controller != gps.controller))
                return true;

            return false;
        }
    }
}
