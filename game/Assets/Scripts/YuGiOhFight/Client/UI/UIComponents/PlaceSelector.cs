using System.Collections.Generic;
using UnityEngine;
using static YuGiOhFight.Client.Duel.GameCard;


namespace YuGiOhFight.Client.UI
{
    public class PlaceSelector : MonoBehaviour
    {
        public Duel.GPS p;

        public List<DuelButtonInfo> buttons = new List<DuelButtonInfo>();
        public List<DuelButton> buttonObjs = new List<DuelButton>();

        private DuelButton selectButton;

        private GameObject highlight;
        private GameObject select;
        private GameObject selectPush;
        private GameObject selectCard;
        private GameObject selectCardPush;
        private GameObject disable;
        public Duel.GameCard cookieCard;

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
        private SceneColorState sceneColors;

        private void Start()
        {
            var collider = gameObject.AddComponent<BoxCollider>();
            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Deck))
            {
                highlight = Runtime.ABLoader.LoadMasterDuelGameObject("eff_duel_highlight10");
                transform.localEulerAngles = new Vector3(0, -19.5f, 0);
                collider.size = new Vector3(8f, 1f, 10f);
            }
            else if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Extra))
            {
                highlight = Runtime.ABLoader.LoadMasterDuelGameObject("eff_duel_highlight10");
                transform.localEulerAngles = new Vector3(0, 19.5f, 0);
                collider.size = new Vector3(8f, 1f, 10f);
            }
            else if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.MonsterZone))
            {
                highlight = Runtime.ABLoader.LoadMasterDuelGameObject("eff_duel_highlight11");
                collider.size = new Vector3(8f, 1f, 8f);
                select = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_mst_001");
                selectPush = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_mst_Push_001");
                selectCard = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_card_001");
                selectCardPush = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_card_Push_001");
                disable = new GameObject("Disable");
                CreateSelectButton();
            }
            else if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.SpellZone))
            {
                if (p.sequence == 5)
                {
                    highlight = Runtime.ABLoader.LoadMasterDuelGameObject("eff_duel_highlight13");
                    collider.size = new Vector3(6f, 1f, 7f);
                    select = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_card_001");
                    selectPush = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_card_Push_001");
                    selectCard = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_card_001");
                    selectCardPush = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_card_Push_001");
                    select.transform.localScale = Vector3.one * 0.8f;
                    selectPush.transform.localScale = Vector3.one * 0.8f;
                }
                else
                {
                    highlight = Runtime.ABLoader.LoadMasterDuelGameObject("eff_duel_highlight12");
                    collider.size = new Vector3(8f, 1f, 7f);
                    select = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_trpmgc_001");
                    selectPush = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_trpmgc_Push_001");
                    selectCard = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_card_001");
                    selectCardPush = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_Select_card_Push_001");
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
                spriteRenderer.sprite = Runtime.TextureManager.container.CardAffectDisable;
                disable.SetActive(false);
            }
            sceneColors = new SceneColorState(highlight, select, selectPush, selectCard, selectCardPush);
        }

        private void Update()
        {
            hover = false;
            if (Runtime.UserInput.HoverObject == gameObject)
                hover = true;

            if (hover)
            {
                highlight.SetActive(true);
                if (Runtime.UserInput.MouseLeftUp)
                    OnClick();
                if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Onfield) == 0 && !countShowing)
                {
                    countShowing = true;
                    Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>().ShowLocationCount(p, this);
                }
            }
            else
            {
                if (Runtime.UserInput.MouseLeftUp)
                    HideButtons();

                highlight.SetActive(false);
                if (countShowing)
                {
                    countShowing = false;
                    Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>().HidePlaceCount(this);
                }
            }

            if (Runtime.UserInput.HoverObject == gameObject)
                highlight.SetActive(true);
            else
                highlight.SetActive(false);
        }

        private void LateUpdate() => sceneColors?.Refresh();

        private void OnDisable()
        {
            sceneColors?.Restore();
            countShowing = false;
            var program = Runtime.Program.instance;
            if (program != null && program.ocgcore != null)
                program.ocgcore.GetUI<ServantUI.OcgCoreUI>()?.HidePlaceCount(this);
        }

        private void OnDestroy() => sceneColors?.Dispose();

        public void Reset()
        {
            if(disable != null)
                disable.SetActive(false);
        }

        private void OnClick()
        {
            if (selecting)
            {
                Runtime.AudioManager.PlaySE("SE_DUEL_SELECT");
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
                foreach(PlaceSelector place in Runtime.Program.instance.ocgcore.Places)
                    if(place.selected)
                        selectedCount++;
                if (selectedCount == Controllers.OcgCore.ES_min)
                {
                    Runtime.Network.BinaryMaster binaryMaster = new Runtime.Network.BinaryMaster();
                    foreach (PlaceSelector place in Runtime.Program.instance.ocgcore.Places)
                        if (place.selected)
                        {
                            var response = new byte[3];
                            response[0] = Controllers.OcgCore.isFirst ? (byte)place.p.controller : (byte)(1 - place.p.controller);
                            response[1] = (byte)place.p.location;
                            response[2] = (byte)place.p.sequence;
                            binaryMaster.writer.Write(response);
                        }
                    Runtime.Program.instance.ocgcore.SendReturn(binaryMaster.Get());
                }
            }
            else if (cardSelecting)
            {
                Runtime.AudioManager.PlaySE("SE_DUEL_SELECT");
                if (Controllers.OcgCore.currentMessage == MDPro3.Duel.YGOSharp.GameMessage.SelectCounter)
                {
                    if (!cardUnselectable)
                        selectButton.Show();
                }
                else if (!cardSelected && !cardUnselectable && !cardPreselected)
                    selectButton.Show();
                else if (cardSelected && !cardUnselectable && !cardPreselected)
                    UnselectCardInThisZone();
                Duel.GameCard card = FindCardInThisPlace();
                if (card != null)
                    card.OnClick();
            }
            else
            {
                if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.SpellZone))
                {
                    if (p.InMyControl())
                    {
                        Controllers.OcgCore.HideMyHandCard = true;
                        Controllers.OcgCore.HideOpHandCard = false;
                    }
                    else
                    {
                        Controllers.OcgCore.HideOpHandCard = true;
                        Controllers.OcgCore.HideMyHandCard = false;
                    }
                }
                else
                {
                    Controllers.OcgCore.HideMyHandCard = false;
                    Controllers.OcgCore.HideOpHandCard = false;
                }

                if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Onfield))
                {
                    Duel.GameCard card = FindCardInThisPlace();
                    if (card != null)
                        card.OnClick();
                    else
                    {
                        Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>().CardDescription.Hide();
                        Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>().CardList.Hide();
                    }
                    foreach (Duel.GameCard c in Controllers.OcgCore.cards)
                        if (c != card)
                            c.NotClickThis();
                }
                else
                {
                    Runtime.AudioManager.PlaySE("SE_DUEL_SELECT");
                    List<Duel.GameCard> cards = new();
                    foreach (Duel.GameCard card in Controllers.OcgCore.cards)
                        if ((card.p.location & p.location) > 0)
                            if (card.p.controller == p.controller)
                                cards.Add(card);
                    Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>().CardList.Show(cards, (MDPro3.Duel.YGOSharp.CardLocation)p.location, (int)p.controller);

                    if (!buttonsCreated)
                    {
                        bool spsummmon = false;
                        bool activate = false;
                        foreach (Duel.GameCard card in Controllers.OcgCore.cards)
                            if ((card.p.location & p.location) > 0)
                                if (card.p.controller == p.controller)
                                    foreach (DuelButtonInfo btn in card.buttons)
                                    {
                                        if (btn.type == ButtonType.Activate)
                                            activate = true;
                                        if (btn.type == ButtonType.SpSummon)
                                            spsummmon = true;
                                    }
                        if (activate)
                        {
                            int response = -1;
                            buttons.Add(new DuelButtonInfo() { response = new List<int>() { response }, hint = MDPro3.InterString.Get("发动效果"), type = ButtonType.Activate });
                        }
                        if (spsummmon)
                        {
                            int response = -2;
                            buttons.Add(new DuelButtonInfo() { response = new List<int>() { response }, hint = MDPro3.InterString.Get("特殊召唤"), type = ButtonType.SpSummon });
                        }
                        CreateButtons();
                    }
                    else
                    {
                        if (Runtime.Program.instance.ocgcore.returnAction == null)
                            ShowButtons();
                    }
                }
            }
        }

        private void CreateButtons()
        {
            if (buttonsCreated || Runtime.Program.instance.ocgcore.returnAction != null || buttons.Count == 0)
            {
                buttons.Clear();
                return;
            }

            for (int i = 0; i < buttons.Count; i++)
            {
                var obj = Runtime.ABLoader.LoadMasterDuelGameObject("DuelButton");
                DuelButton mono = obj.GetComponent<DuelButton>();
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
            var obj = Runtime.ABLoader.LoadMasterDuelGameObject("DuelButton");
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
            foreach (DuelButton button in buttonObjs)
                button.Show();
        }

        public void HideButtons()
        {
            foreach (DuelButton button in buttonObjs)
                button.Hide();
            if (selectButton != null)
                selectButton.Hide();
        }

        public void ClearButtons()
        {
            foreach (DuelButton go in buttonObjs)
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

        public void InitializeSelectCardInThisZone(List<Duel.GameCard> cards)
        {
            foreach (Duel.GameCard card in cards)
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
                    if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone) > 0
                        && p.sequence == 5
                        && card.p.controller == 1
                        && (card.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone) > 0
                        && card.p.sequence == 6
                        )
                    {
                        cardSelecting = true;
                        cookieCard = card;
                        ShowSelectCardHighlight();
                        break;
                    }
                    if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone) > 0
                        && p.sequence == 6
                        && card.p.controller == 1
                        && (card.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone) > 0
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
            if (cardSelecting && Controllers.OcgCore.currentMessage == MDPro3.Duel.YGOSharp.GameMessage.SelectSum)
            {
                if (Controllers.OcgCore.cardsMustBeSelected.Contains(cookieCard))
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

            if (Controllers.OcgCore.currentMessage != MDPro3.Duel.YGOSharp.GameMessage.SelectCounter)
                selectCard.SetActive(false);
            selectCardPush.SetActive(false);
            selectCardPush.SetActive(true);
            Runtime.Program.instance.ocgcore.FieldSelectRefresh(cookieCard);
        }

        public void UnselectCardInThisZone()
        {
            if (Controllers.OcgCore.currentMessage == MDPro3.Duel.YGOSharp.GameMessage.SelectCounter)
                return;
            cardSelected = false;
            Runtime.Program.instance.ocgcore.FieldSelectRefresh(cookieCard);
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

        public Duel.GPS HighlightThisZone(uint place, int min)
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
                    if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone) > 0)
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
                    if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.SpellZone) > 0)
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
            if ((cookieCard.p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.Attack) > 0)
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

        public Duel.GameCard FindCardInThisPlace()
        {
            if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone) > 0 && p.sequence == 5)
                foreach (Duel.GameCard card in Controllers.OcgCore.cards)
                {
                    if ((card.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone) > 0)
                    {
                        if (card.p.controller == 0 && card.p.sequence == 5
                            && (card.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Overlay) == 0)
                            return card;
                        if (card.p.controller == 1 && card.p.sequence == 6
                            && (card.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Overlay) == 0)
                            return card;
                    }
                }
            else if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone) > 0 && p.sequence == 6)
                foreach (Duel.GameCard card in Controllers.OcgCore.cards)
                {
                    if ((card.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone) > 0)
                    {
                        if (card.p.controller == 0 && card.p.sequence == 6
                            && (card.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Overlay) == 0)
                            return card;
                        if (card.p.controller == 1 && card.p.sequence == 5
                            && (card.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Overlay) == 0)
                            return card;
                    }
                }
            else
                foreach (Duel.GameCard card in Controllers.OcgCore.cards)
                    if (p.controller == card.p.controller)
                        if ((card.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Overlay) == 0)
                            if (p.location == card.p.location)
                                if (p.sequence == card.p.sequence)
                                    return card;

            return null;
        }

        public void ShowHint(uint location, uint controller)
        {
            if ((location & p.location) > 0 && controller == p.controller)
            {
                hintObj = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_EXdeck_001");
                hintObj.transform.SetParent(transform, false);
                int cardCount = Runtime.Program.instance.ocgcore.GetLocationCardCount((MDPro3.Duel.YGOSharp.CardLocation)location, controller);
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
            if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Onfield) == 0)
                return;

            if (p.location == (uint)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone && (p.sequence == 5 || p.sequence == 6))
                return;

            int order = 0;
            if (p.controller != 0 && Controllers.OcgCore.isFirst
                || p.controller == 0 && !Controllers.OcgCore.isFirst)
                order += 16;

            if (p.location == (uint)MDPro3.Duel.YGOSharp.CardLocation.SpellZone)
                order += 8;
            order += (int)p.sequence;
            if ((filter & (1 << order)) > 0)
                disable.SetActive(true);
            else
                disable.SetActive(false);
        }

        public bool InTheSameLine(Duel.GPS gps)
        {
            if ((gps.location & ((uint)MDPro3.Duel.YGOSharp.CardLocation.Deck + (uint)MDPro3.Duel.YGOSharp.CardLocation.Extra)) > 0)
                return false;
            if ((p.location & ((uint)MDPro3.Duel.YGOSharp.CardLocation.Deck + (uint)MDPro3.Duel.YGOSharp.CardLocation.Extra)) > 0)
                return false;

            if ((p.sequence == gps.sequence) && (p.controller == gps.controller))
                return true;
            if ((p.sequence == (4 - gps.sequence)) && (p.controller != gps.controller))
                return true;

            return false;
        }

        // Only colour changes: the publisher's meshes, particles, rollover
        // timing and PlaceSelector response state remain on their native path.
        private sealed class SceneColorState
        {
            private readonly List<RendererColor> renderers = new();
            private string scene;

            internal SceneColorState(params GameObject[] effects)
            {
                foreach (var effect in effects)
                {
                    if (effect == null) continue;
                    foreach (var renderer in effect.GetComponentsInChildren<Renderer>(true))
                    {
                        // The audited effects have one material per renderer.
                        // Native particle light/push renderers arrive with a
                        // block already installed. Preserve it while owning
                        // just their audited colour; reject unknown overrides.
                        var materials = renderer.sharedMaterials;
                        if (materials.Length != 1 || materials[0] == null || materials[0].shader == null) continue;
                        var material = materials[0];
                        string shader = material.shader.name;
                        bool rollover = shader == "VFX_Shader/Exclusive/fxs_HL_Rollover_001";
                        bool nativeParticle = rollover && renderer is ParticleSystemRenderer
                            && (material.name == "fxm_HL_Select_light_001" || material.name == "fxm_HL_Select_push_001");
                        if (renderer.HasPropertyBlock() && !nativeParticle) continue;
                        string property = rollover ? "_Color"
                            : shader == "VFX_Shader/Exclusive/fxs_HL_RolloverSurface_001"
                                || shader == "VFX_Shader/HitghLight/EFF_DUEL_HIGHLIGHT_alpha" ? "_color" : null;
                        if (property != null && material.HasProperty(property))
                            renderers.Add(new RendererColor(renderer, material, Shader.PropertyToID(property),
                                rollover, nativeParticle));
                    }
                }
            }

            internal void Refresh()
            {
                if (!Client.FightDuelPopupSkin.SceneThemeActive) { Restore(); return; }
                string id = Runtime.Program.instance.ocgcore.DuelBGManager.ActiveSceneId;
                if (scene == id)
                {
                    foreach (var renderer in renderers) renderer.RefreshParticleColor();
                    return;
                }
                var palette = Client.ScenePalette.For(id);
                if (palette == null) { Restore(); return; }
                // Academy light rails, Egyptian gold and city silver retain
                // the same readable native selection pulse and click flash.
                Color color = palette.Kind == 2 ? palette.Text : palette.Accent;
                foreach (var renderer in renderers) renderer.Apply(color);
                scene = id;
            }

            internal void Restore()
            {
                if (scene == null) return;
                foreach (var renderer in renderers) renderer.Restore();
                scene = null;
            }

            internal void Dispose()
            {
                foreach (var renderer in renderers) renderer.Dispose();
                scene = null;
            }

            private sealed class RendererColor
            {
                private readonly Renderer renderer;
                private readonly int property;
                private readonly MaterialPropertyBlock originalBlock = new(), themed = new();
                private readonly bool hadBlock, nativeParticle;
                private readonly float intensity, alpha;
                private readonly Material originalMaterial;
                private Material ownedMaterial;
                private Color currentColor;
                private bool applied;

                internal RendererColor(Renderer source, Material material, int colorProperty, bool ownsColorMaterial, bool particle)
                {
                    renderer = source; property = colorProperty;
                    hadBlock = source.HasPropertyBlock(); nativeParticle = particle;
                    source.GetPropertyBlock(originalBlock);
                    Color color = material.GetColor(property);
                    intensity = Mathf.Max(color.r, Mathf.Max(color.g, color.b)); alpha = color.a;
                    if (ownsColorMaterial) originalMaterial = material;
                }

                internal void Apply(Color color)
                {
                    if (renderer == null) return;
                    currentColor = new Color(color.r * intensity, color.g * intensity, color.b * intensity, alpha);
                    applied = true;
                    if (originalMaterial != null)
                    {
                        // The bundled Metal rollover program takes RGB only
                        // from UnityPerMaterial._Color; particle vertex R/A
                        // control opacity. Native Light/Push blocks must be
                        // retained, rather than excluding those renderers.
                        // Bind an owned material without altering particle RGB,
                        // alpha curves, instancing flags or renderer state.
                        if (ownedMaterial == null)
                            ownedMaterial = new Material(originalMaterial) { name = originalMaterial.name + " [Fight scene colour]" };
                        ownedMaterial.SetColor(property, currentColor);
                        renderer.sharedMaterial = ownedMaterial;
                        if (nativeParticle) ApplyParticleColor();
                        return;
                    }
                    renderer.GetPropertyBlock(themed);
                    // Raw vectors preserve authored HDR click intensity and
                    // alpha-zero surface graphs without changing opacity.
                    themed.SetVector(property, new Vector4(currentColor.r, currentColor.g, currentColor.b, currentColor.a));
                    renderer.SetPropertyBlock(themed);
                }

                internal void RefreshParticleColor()
                {
                    if (nativeParticle && applied && renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy
                        && renderer.sharedMaterial == ownedMaterial) ApplyParticleColor();
                }

                private void ApplyParticleColor()
                {
                    // Merge the current native block so particle updates keep
                    // all non-colour properties and their current values.
                    renderer.GetPropertyBlock(themed);
                    themed.SetVector(property, new Vector4(currentColor.r, currentColor.g, currentColor.b, currentColor.a));
                    renderer.SetPropertyBlock(themed);
                }

                internal void Restore()
                {
                    if (!applied) return;
                    applied = false;
                    if (renderer == null) return;
                    if (originalMaterial != null)
                    {
                        if (renderer.sharedMaterial != ownedMaterial) return;
                        renderer.sharedMaterial = originalMaterial;
                    }
                    renderer.SetPropertyBlock(hadBlock ? originalBlock : null);
                }

                internal void Dispose()
                {
                    Restore();
                    if (ownedMaterial != null) Object.Destroy(ownedMaterial);
                    ownedMaterial = null;
                }
            }
        }
    }
}
