using Cysharp.Threading.Tasks;
using DG.Tweening;


using System;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;


using static YuGiOhFight.Client.Controllers.OcgCore;


using YuGiOhFight.Client.Presentation.Extensions;


namespace YuGiOhFight.Client.Duel
{
    public class GPS
    {
        public uint controller;
        public uint location;
        public uint sequence;
        public int position;
        public uint reason;

        public GPS Copy()
        {
            return (GPS)MemberwiseClone();
        }

        public bool InMyControl()
        {
            return controller == 0;
        }

        public bool InLocation(MDPro3.Duel.YGOSharp.CardLocation location)
        {
            return (this.location & (uint)location) > 0;
        }

        public bool InLocation(MDPro3.Duel.YGOSharp.CardLocation location1, MDPro3.Duel.YGOSharp.CardLocation location2)
        {
            return InLocation(location1) || InLocation(location2);
        }

        public bool InLocation(MDPro3.Duel.YGOSharp.CardLocation location1, MDPro3.Duel.YGOSharp.CardLocation location2, MDPro3.Duel.YGOSharp.CardLocation location3)
        {
            return InLocation(location1) || InLocation(location2) || InLocation(location3);
        }

        public bool InPosition(MDPro3.Duel.YGOSharp.CardPosition position)
        {
            return (this.position & (uint)position) > 0;
        }

        public bool InPendulumSequence() //TODO
        {
            return sequence == 0 || sequence == 4 || sequence == 6 || sequence == 7;
        }

        public bool IsReason(MDPro3.Duel.YGOSharp.CardReason reason)
        {
            return IsReason(this.reason, reason);
        }

        public bool InSequence(uint player, MDPro3.Duel.YGOSharp.CardLocation location, int targetSequence)
        {
            return player == controller && InLocation(location) && sequence == targetSequence;
        }

        public static bool IsReason(uint reason, MDPro3.Duel.YGOSharp.CardReason cardReason)
        {
            return (reason & (uint)cardReason) > 0;
        }

    }

    public class Effect
    {
        public string desc;
        public int flag;
        public int ptr;
        public bool forced;
    }

    public class GameCard : MonoBehaviour
    {
        private MDPro3.Duel.YGOSharp.Card data = new ();
        private readonly MDPro3.Duel.YGOSharp.Card lastValidData = new();
        public GPS p;
        private bool m_disabled;
        public bool Disabled
        {
            get
            {
                return m_disabled;
            }
            set
            {
                m_disabled = value;
                SetDisabled();
            }
        }
        public bool negated;
        public bool disabledInChain;
        public bool SemiNomiSummoned = false;
        public int selectPtr = 0;
        public int levelForSelect_1 = 0;
        public int levelForSelect_2 = 0;
        public int counterCanCount = 0;
        public int counterSelected = 0;
        public List<GameCard> targets = new();
        public List<GameCard> effectTargets = new();
        public List<GameCard> overlays = new();
        public GameCard overlayParent;
        public GameCard equipedCard;
        public List<Effect> effects = new();
        public bool forSelect;

        public int overFatherCount;
        private const float closeupLineColorIntensity = 1.5f;

        public GameObject model;
        public Presentation.Elements.ElementObjectManager manager;
        public GPS cacheP;

        private bool inAnimation;
        private bool clicked;
        private bool hover;
        private bool hoving;
        private int handLayoutVersion = -1;
        private Bounds handColliderBounds;
        private bool handColliderReady;

        private int currentCardCode = 0;
        private Texture currentCardTexture;
        private Material normalMat;
        private Material tempMat;

        private GameObject hoverObject;
        private GameObject HoverObject =>
            hoverObject = hoverObject != null ? hoverObject
            : manager.GetElement("CardModel");

        private void Awake()
        {
            UI.ServantUI.OcgCoreUI.OnInputDebug += DebugHighlights;
            Runtime.SystemEvent.OnLanguageChange += ReloadForLanguageChange;
            Runtime.SystemEvent.OnVideoCardConfigChange += ReloadFaceWhenConfigChange;
        }

        public void Dispose()
        {
            RetireAnimations();
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }

            lock (this)
            {
                if (currentCardTexture != null && currentCardCode != 0)
                {
                    Presentation.CardImageLoader.ReleaseCard(currentCardCode, !(currentCardTexture is RenderTexture));
                    currentCardTexture = null;
                    currentCardCode = 0;
                }
            }

            Destroy(model);
            model = null;
            manager = null;
            Destroy(normalMat);
            Destroy(tempMat);
            Destroy(this);
            UI.ServantUI.OcgCoreUI.OnInputDebug -= DebugHighlights;
            Runtime.SystemEvent.OnLanguageChange -= ReloadForLanguageChange;
            Runtime.SystemEvent.OnVideoCardConfigChange -= ReloadFaceWhenConfigChange;
        }

        private void OnDestroy()
        {
            Runtime.SystemEvent.OnLanguageChange -= ReloadForLanguageChange;
            Runtime.SystemEvent.OnVideoCardConfigChange -= ReloadFaceWhenConfigChange;
            UI.ServantUI.OcgCoreUI.OnInputDebug -= DebugHighlights;
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        internal void RetireAnimations()
        {
            // This component shares container_3D with every other card. Only
            // retire its own roots and model targets, without completing them.
            enabled = false;
            DOTween.Kill(this, false);
            if (model == null) return;
            foreach (var target in model.GetComponentsInChildren<Transform>(true))
                DOTween.Kill(target, false);
        }

        private void LateUpdate()
        {
            if (model == null)
                return;

            RefreshHandLayout();

            hover = Runtime.UserInput.HoverObject == HoverObject;
            if (hover)
            {
                if(p.InMyControl() && HideMyHandCard)
                    HideMyHandCard = false;
                else if(!p.InMyControl() && HideOpHandCard)
                    HideOpHandCard = false;
            }
            if (!hover) hoving = false;

            if (hover && Runtime.UserInput.MouseLeftUp && !handCardDraged)
                OnClick();
            else if (!hover && Runtime.UserInput.MouseLeftUp)
            {
                if (Runtime.UserInput.HoverObject == null)
                    NotClickThis();
                else if (Runtime.UserInput.HoverObject.name != "PlaceSelector")
                    NotClickThis();
            }

            if (Math.Abs(handOffset - lastHandOffset) > 10)
                NotClickThis();

            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand))
            {
                if (addingCardToMyHand || cardLeavingMyHand)
                {
                    if (!handDefault)
                    {
                        NotClickThis();
                        handDefault = true;
                        hoving = false;
                        clicked = false;
                        AnimationHandDefault(0.1f);
                    }
                    return;
                }

                if (inAnimation) return;

                if (hover && hoving == false && clicked == false)
                {
                    hoving = true;
                    handDefault = false;
                    AnimationHandHover();
                    MoveToHandDefault(0.1f);
                }
                if (hover && Runtime.UserInput.MouseLeftUp && !handCardDraged)
                {
                    clicked = true;
                    handDefault = false;
                    AnimationHandAppeal();
                }
                if (!hover && Runtime.UserInput.MouseLeftDown)
                {
                    clicked = false;
                }
                if (!hover && !clicked && !handDefault)
                {
                    AnimationHandDefault(0.1f);
                }
                if (dragingCard)
                    SetHandDefault();
            }
        }

        public Material GetMaterial()
        {
            if (model == null)
                return null;
            var origin = manager.GetElement<Transform>("CardModel").GetChild(1).GetComponent<Renderer>().material;
            var copy = Instantiate(origin);
            copy.SetFloat("_isFade", 0f);
            copy.SetFloat("_LoadingBlend", 0f);
            return copy;
        }

        public void OnClick()
        {
            if (model == null)
                return;
            if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Hand) == 0)
                Runtime.AudioManager.PlaySE("SE_DUEL_SELECT");
            Runtime.Program.instance.ocgcore.GetUI<UI.ServantUI.OcgCoreUI>().CardDescription.Show(this);
            if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Xyz) && (p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone) > 0)
                Runtime.Program.instance.ocgcore.GetUI<UI.ServantUI.OcgCoreUI>().CardList.Show(Runtime.Program.instance.ocgcore.GCS_GetOverlays(this), MDPro3.Duel.YGOSharp.CardLocation.Overlay, (int)p.controller);
            else
                Runtime.Program.instance.ocgcore.GetUI<UI.ServantUI.OcgCoreUI>().CardList.Hide();

            if (equipedCard != null)
                Runtime.Program.instance.ocgcore.ShowEquipLine(model.transform.position, equipedCard.model.transform.position);
            if (targets != null)
                Runtime.Program.instance.ocgcore.ShowTargetLines(model.transform.position, targets);

            if (buttons.Count == 0 || Runtime.Program.instance.ocgcore.currentPopup != null)
                return;
            foreach (UI.DuelButton button in buttonObjs)
                button.Show();
            if (hightYellow)
                manager.GetElement("EffectHighlightYellowSelect").SetActive(true);
            else
                manager.GetElement("EffectHighlightBlueSelect").SetActive(true);
        }

        public void NotClickThis()
        {
            if (model == null || buttons.Count == 0)
                return;
            foreach (UI.DuelButton button in buttonObjs)
                button.Hide();
            if (hightYellow)
                manager.GetElement("EffectHighlightYellowSelect").SetActive(false);
            else
                manager.GetElement("EffectHighlightBlueSelect").SetActive(false);
        }

        private void SetParticleSortingLayer(GameObject obj, string sortingLayerName)
        {
            var pr = obj.transform.GetChild(0).GetComponent<ParticleSystemRenderer>();
            pr.sortingLayerName = sortingLayerName;
        }

        private void SetParticleRenderQueue(GameObject obj, int queue)
        {
            var pr = obj.transform.GetChild(0).GetComponent<ParticleSystemRenderer>();
            pr.material.renderQueue = queue;
        }

        private List<GameObject> highlights = new();

        private GameObject CreateModel(bool real = true)
        {
            model = Runtime.ABLoader.LoadMasterDuelGameObject("DuelCardModel");
            manager = model.GetComponent<Presentation.Elements.ElementObjectManager>();

            manager.GetElement("StatusLabelRoot").SetActive(false);

            GameCardMono cardMono = manager.GetElement<GameCardMono>("CardModel");
            cardMono.cookieCard = this;
            // The authored prefab chain is axis-aligned at instantiation.
            // Capture before parenting/pose animation: this player strips the
            // BoxCollider size/center getters, but preserves Collider.bounds.
            handColliderReady = false;
            var collider = cardMono.GetComponent<Collider>();
            if (collider != null)
            {
                Bounds world = collider.bounds;
                handColliderBounds = new Bounds(collider.transform.InverseTransformPoint(world.center),
                    collider.transform.InverseTransformVector(world.size));
                handColliderReady = handColliderBounds.size.x > 0 && handColliderBounds.size.z > 0;
            }

            var cardParmUp = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_cardparm_up_001");
            var cardParmDown = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_cardparm_down_001");
            var cardParmChange = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_cardparm_change_001");

            var cardBuffActive = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_bff_active_001");
            var cardNegate = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_bff_disable_001");
            var cardDisquiet = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_bff_disquiet_001");
            var cardBlueHighlight = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_set_001");
            var cardBlueHighlightSelect = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_set_sct_001");
            var cardYellowHighlight = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_SPsom_001");
            var cardYellowHighlightSelect = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_HL_SPsom_sct_001");

            highlights.Clear();
            highlights.Add(cardBlueHighlight);
            highlights.Add(cardBlueHighlightSelect);
            highlights.Add(cardYellowHighlight);
            highlights.Add(cardYellowHighlightSelect);
            //foreach (var hl in highlights)
            //    SetParticleRenderQueue(hl, 3000);
            //foreach (var hl in highlights)
            //    SetParticleSortingLayer(hl, "Default");

            var root = manager.GetElement<Transform>("Turn").GetChild(1);
            cardParmUp.transform.SetParent(root, false);
            cardParmDown.transform.SetParent(root, false);
            cardParmChange.transform.SetParent(root, false);
            cardBuffActive.transform.SetParent(root, false);
            cardBuffActive.transform.localPosition = new Vector3(0, 0.1f, 0);
            cardNegate.transform.SetParent(root, false);
            cardNegate.transform.localPosition = new Vector3(0, 0.1f, 0);
            cardDisquiet.transform.SetParent(root, false);
            cardDisquiet.transform.localPosition = new Vector3(0, 0f, 0);
            cardDisquiet.transform.localEulerAngles = new Vector3(0f, 0f, 180f);

            var highlight = new GameObject("Highlight");
            cardBlueHighlight.transform.SetParent(highlight.transform, false);
            cardBlueHighlightSelect.transform.SetParent(highlight.transform, false);
            cardYellowHighlight.transform.SetParent(highlight.transform, false);
            cardYellowHighlightSelect.transform.SetParent(highlight.transform, false);
            highlight.transform.SetParent(root, false);

            Presentation.Elements.ElementObject e1 = cardParmUp.AddComponent<Presentation.Elements.ElementObject>();
            e1.label = "EffectBuff";
            Presentation.Elements.ElementObject e2 = cardParmDown.AddComponent<Presentation.Elements.ElementObject>();
            e2.label = "EffectDebuff";
            Presentation.Elements.ElementObject e3 = cardParmChange.AddComponent<Presentation.Elements.ElementObject>();
            e3.label = "EffectChange";
            Presentation.Elements.ElementObject e4 = cardBuffActive.AddComponent<Presentation.Elements.ElementObject>();
            e4.label = "EffectBuffActive";
            Presentation.Elements.ElementObject e5 = cardNegate.AddComponent<Presentation.Elements.ElementObject>();
            e5.label = "EffectNegate";
            Presentation.Elements.ElementObject e6 = cardDisquiet.AddComponent<Presentation.Elements.ElementObject>();
            e6.label = "EffectDisquiet";

            Presentation.Elements.ElementObject e7 = cardBlueHighlight.AddComponent<Presentation.Elements.ElementObject>();
            e7.label = "EffectHighlightBlue";
            Presentation.Elements.ElementObject e8 = cardBlueHighlightSelect.AddComponent<Presentation.Elements.ElementObject>();
            e8.label = "EffectHighlightBlueSelect";
            Presentation.Elements.ElementObject e9 = cardYellowHighlight.AddComponent<Presentation.Elements.ElementObject>();
            e9.label = "EffectHighlightYellow";
            Presentation.Elements.ElementObject e10 = cardYellowHighlightSelect.AddComponent<Presentation.Elements.ElementObject>();
            e10.label = "EffectHighlightYellowSelect";

            e7.transform.localScale = new Vector3(1.03f, 1f, 1.025f);
            e9.transform.localScale = new Vector3(1.02f, 1f, 1f);

            var list = manager.serializedElements.ToList();
            list.Add(e1);
            list.Add(e2);
            list.Add(e3);
            list.Add(e4);
            list.Add(e5);
            list.Add(e6);

            list.Add(e7);
            list.Add(e8);
            list.Add(e9);
            list.Add(e10);

            manager.serializedElements = list.ToArray();

            cardParmUp.SetActive(false);
            cardParmDown.SetActive(false);
            cardParmChange.SetActive(false);
            cardBuffActive.SetActive(false);
            cardNegate.SetActive(false);
            cardDisquiet.SetActive(false);

            cardBlueHighlight.SetActive(false);
            cardBlueHighlightSelect.SetActive(false);
            cardYellowHighlight.SetActive(false);
            cardYellowHighlightSelect.SetActive(false);

            var back = manager.GetElement<Transform>("CardModel").GetChild(0).GetComponent<Renderer>();

            back.material = p.controller ==0 ? myProtector : opProtector;
            back.material.renderQueue = 3000;
            back.material.SetFloat("_ZWrite", 1f);
            SetFace();

            model.transform.SetParent(Runtime.Program.instance.ocgcore.GetFieldTransform(p.controller));

            if (real)
                return model;
            else
            {
                var go = model;
                model = null;
                return go;
            }
        }

        private void DebugHighlights(string text)
        {
            if(int.TryParse(text, out var queue))
            {
                foreach (var highlight in highlights)
                    SetParticleRenderQueue(highlight, queue);
            }
            else if (float.TryParse(text, out float offset))
            {
                AdjustHighlight(offset);
            }
            else if(text == "debug")
            {
                if(highlights.Count == 0)
                    return;
                foreach (var highlight in highlights)
                {
                    if (highlight.activeInHierarchy)
                    {
                        var pr = highlight.transform.GetChild(0).GetComponent<ParticleSystemRenderer>();
                        Runtime.MessageManager.Cast($"Highlight RenderQueue: {pr.material.renderQueue}, Sorting Layer: {pr.sortingLayerName}, Offset: {pr.transform.localPosition}");
                        break;
                    }
                }
            }
            else
            {
                foreach (var highlight in highlights)
                    SetParticleSortingLayer(highlight, text);
            }
        }

        private CancellationTokenSource cts;

        private void SetFace(float? retainedFaceFade = null)
        {
            if(cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            cts = new CancellationTokenSource();
            _ = SetFaceAsync(cts.Token, retainedFaceFade);
        }

        private bool isRenderTexture;
        private int languageReloadFrame = -1;

        private void ReloadForLanguageChange()
        {
            if (this == null || !enabled) return;
            languageReloadFrame = Time.frameCount;
            MDPro3.Duel.YGOSharp.CardsManager.RefreshLocalizedText(data);
            MDPro3.Duel.YGOSharp.CardsManager.RefreshLocalizedText(lastValidData);
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
            // ChangeLanguage already invalidated the shared cache. Forget its
            // old ownership before acquiring a texture from the new catalog.
            currentCardTexture = null;
            currentCardCode = 0;
            if (model == null || data.Id <= 0) return;
            var face = manager.GetElement<Transform>("CardModel").GetChild(1).GetComponent<Renderer>();
            SetFace(face.material.GetFloat("_isFade"));
        }

        private void ReloadFaceWhenConfigChange()
        {
            if (languageReloadFrame == Time.frameCount) return;
            var config = MDPro3.Config.GetBool("VideoCard", true);
            if (!MDPro3.IO.ResourceManager.CardHasVideo(data.Id) && (config || !isRenderTexture)) return;
            // The video cache was retired before this event. Its old
            // ownership must not release a coexisting static texture.
            if (!config && isRenderTexture)
            {
                currentCardTexture = null;
                currentCardCode = 0;
                isRenderTexture = false;
            }
            float? faceFade = null;
            if (model != null)
                faceFade = manager.GetElement<Transform>("CardModel").GetChild(1).GetComponent<Renderer>()
                    .material.GetFloat("_isFade");
            SetFace(faceFade);
        }

        private async UniTask SetFaceAsync(CancellationToken cancellationToken, float? retainedFaceFade = null)
        {
            Texture oldTexture = null;
            int oldCode = 0;
            lock (this)
            {
                oldTexture = currentCardTexture;
                oldCode = currentCardCode;
                currentCardTexture = null;
                currentCardCode = 0;
            }
            if (oldTexture != null && oldCode != 0)
            {
                Presentation.CardImageLoader.ReleaseCard(oldCode, !(oldTexture is RenderTexture));
            }

            if (model == null)
                return;

            if (normalMat == null)
                normalMat = UI.MaterialLoader.GetCardMaterial(-1);
            var cardCode = data.Id;
            Renderer cardFace = manager.GetElement<Transform>("CardModel").
                    GetChild(1).GetComponent<Renderer>();

            var loadingTex = Runtime.TextureManager.container
                            .GetCardLoadingTexture(MDPro3.Duel.YGOSharp.CardsManager.Get(data.Id));
            normalMat.SetTexture("_LoadingTex", loadingTex);
            normalMat.SetFloat("_LoadingBlend", 1f);
            cardFace.material = normalMat;
            if (retainedFaceFade.HasValue)
                cardFace.material.SetFloat("_isFade", retainedFaceFade.Value);

            if(tempMat != null)
            {
                Destroy(tempMat);
                tempMat = null;
            }

            Texture texture = null;
            try
            {
                texture = await Presentation.CardImageLoader.LoadCardAsync(cardCode, true, cancellationToken);
                isRenderTexture = texture is RenderTexture;

                if (this == null || model == null || data.Id != cardCode || cancellationToken.IsCancellationRequested)
                {
                    if (texture != null)
                        Presentation.CardImageLoader.ReleaseCard(cardCode, !(texture is RenderTexture));
                    return;
                }

                MDPro3.CardRarity.Rarity rarity = MDPro3.CardRarity.GetRarity(data.Id);
                if(rarity != MDPro3.CardRarity.Rarity.Normal)
                {
                    // Duel visibility can change while localized art loads.
                    // Transfer the live state when replacing its material.
                    float faceFade = retainedFaceFade.HasValue ? cardFace.material.GetFloat("_isFade") : 0f;
                    tempMat = UI.MaterialLoader.GetCardMaterial(data.Id, true);
                    cardFace.material = tempMat;
                    if (retainedFaceFade.HasValue)
                        cardFace.material.SetFloat("_isFade", faceFade);
                    cardFace.material.renderQueue = 3000;
                    cardFace.material.SetTexture("_LoadingTex", loadingTex);
                    cardFace.material.SetFloat("_LoadingBlend", 1f);
                }

                cardFace.material.mainTexture = texture;
                SetDisabled(retainedFaceFade.HasValue);
                lock (this)
                {
                    currentCardTexture = texture;
                    currentCardCode = cardCode;
                }
                _ = UI.CardRawImageHandler.SetMaterialFloatAsync(cardFace.material, "_LoadingBlend", 0f, 0.1f, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (texture != null)
                    Presentation.CardImageLoader.ReleaseCard(cardCode, !(texture is RenderTexture));
                throw;
            }
            catch (Exception)
            {
                if (texture != null)
                    Presentation.CardImageLoader.ReleaseCard(cardCode, !(texture is RenderTexture));
                throw;
            }
        }

        public MDPro3.Duel.YGOSharp.Card GetData()
        {
            return data;
        }

        public MDPro3.Duel.YGOSharp.Card GetValidData()
        {
            if (data.Id > 0)
                return data;
            else
                return lastValidData;
        }

        public void SetData(MDPro3.Duel.YGOSharp.Card d)
        {
            if (d.Id > 0)
                d.CloneTo(lastValidData);
            else if (data.Id > 0)
                data.CloneTo(lastValidData);

            d.Attack = MDPro3.Duel.YGOSharp.Card.NormalizeBattleValue(d.Attack, false);
            d.Defense = MDPro3.Duel.YGOSharp.Card.NormalizeBattleValue(d.Defense, false);
            d.rAttack = MDPro3.Duel.YGOSharp.Card.NormalizeBattleValue(d.rAttack, false);
            d.rDefense = MDPro3.Duel.YGOSharp.Card.NormalizeBattleValue(d.rDefense, false);

            if (d.Id != data.Id)
            {
                data = d;
                if (model != null)
                {
                    if (data.Id > 0)
                    {
                        SetFace();
                        ShowFaceDownCardOrNot(NeedShowFaceDownCard());
                        RefreshHandShadowState();
                    }
                }
            }
            data = d;
            if (model != null && p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand) && !inAnimation)
            {
                RefreshHandTurnByCode();
                RefreshHandShadowState();
            }
            RefreshLabel();
            UpdateExDeckTop();
        }

        public void SetCode(int code)
        {
            if (code > 0)
            {
                if (data.Id != code)
                {
                    SetData(MDPro3.Duel.YGOSharp.CardsManager.Get(code));
                    data.Id = code;
                    if (p.controller == 1)
                        if (condition == Controllers.OcgCore.Condition.Duel)
                            if (!sideReference.Main.Contains(code))
                                sideReference.Main.Add(code);
                }
            }
        }

        public void RefreshData()
        {
            MDPro3.Duel.YGOSharp.CardsManager.Get(data.Id).CloneTo(data);
            SetData(data);
            ClearAllTails();
        }

        public void EraseData()
        {
            SetData(MDPro3.Duel.YGOSharp.CardsManager.Get(0));
            Disabled = false;
            ClearAllTails();
        }

        public void AddTarget(GameCard card)
        {
            if (!targets.Contains(card))
                targets.Add(card);
        }

        public void RemoveTarget(GameCard card)
        {
            targets.Remove(card);
        }

        public void AddEffectTarget(GameCard card)
        {
            if (!effectTargets.Contains(card))
                effectTargets.Add(card);
        }

        public void RemoveEffectTarget(GameCard card)
        {
            effectTargets.Remove(card);
        }

        public static Vector3 GetCardPosition(GPS p)
        {
            var returnValue = Vector3.zero;

            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Search))
            {
                return new Vector3(0, -50, 0);
            }
            else if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Unknown))
            {
                return new Vector3(0, 10, 0);
            }
            else if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand))
            {
                return DuelHandLayout.Position(p, OriginalHandPosition(p));
            }
            else if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Deck))
            {
                if (p.controller == 0)
                    returnValue = new Vector3(26.6f, 1.5f, -23.5f);
                else
                    returnValue = new Vector3(-26.6f, 1.5f, 23.5f);
                returnValue.y += p.sequence * 0.11f;
            }
            else if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Extra))
            {
                if (p.InMyControl())
                    returnValue = new Vector3(-26.6f, 1.5f, -23.5f);
                else
                    returnValue = new Vector3(26.6f, 1.5f, 23.5f);
                returnValue.y += p.sequence * 0.11f;
            }
            else if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Grave))
            {
                var offset = (p.InMyControl() ? movingToMyGrave : movingToOpGrave) - 1;
                if (offset < 0)
                    offset = 0;
                if (p.InMyControl())
                    returnValue = new Vector3(25.74f - 3.75f * offset, 5f + 0.1f * offset, -14.26f);
                else
                    returnValue = new Vector3(-25.74f + 3.75f * offset, 5f + 0.1f * offset, 14.26f);
                returnValue = ThemePilePosition(p, false, returnValue);
            }
            else if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Removed))
            {
                var offset = (p.InMyControl() ? movingToMyExclude : movingToOpExclude) - 1;
                if (offset < 0)
                    offset = 0;
                if (p.controller == 0)
                    returnValue = new Vector3(25.74f + 1.842971f - 3.75f * offset, 5f + 0.1f * offset, -14.26f + 6.236011f);
                else
                    returnValue = new Vector3(-25.74f - 1.842971f + 3.75f * offset, 5f + 0.1f * offset, 14.26f - 6.236011f);
                returnValue = ThemePilePosition(p, true, returnValue);
            }

            else if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.MonsterZone))
            {
                var realIndex = p.sequence;
                if (p.controller == 0)
                {
                    realIndex = p.sequence;
                    returnValue.y = 0.2f;
                    returnValue.z = -9.48f;
                }
                else
                {
                    if (realIndex <= 4)
                        realIndex = 4 - p.sequence;
                    else if (realIndex == 5)
                        realIndex = 6;
                    else if (realIndex == 6) realIndex = 5;
                    returnValue.y = 0.2f;
                    returnValue.z = 9.51f;
                }

                switch (realIndex)
                {
                    case 0:
                        returnValue.x = -17.2f;
                        break;
                    case 1:
                        returnValue.x = -8.6f;
                        break;
                    case 2:
                        returnValue.x = 0f;
                        break;
                    case 3:
                        returnValue.x = 8.6f;
                        break;
                    case 4:
                        returnValue.x = 17.2f;
                        break;
                    case 5:
                        returnValue.x = -8.6f;
                        returnValue.z = 0;
                        break;
                    case 6:
                        returnValue.x = 8.6f;
                        returnValue.z = 0;
                        break;
                }
            }
            else if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.SpellZone))
            {
                if (p.sequence < 5 || (p.sequence == 6 || p.sequence == 7) && MasterRule >= 4)
                {
                    var realIndex = p.sequence;
                    if (p.controller == 0)
                    {
                        realIndex = p.sequence;
                        returnValue.y = 0.2f;
                        returnValue.z = -18f;
                    }
                    else
                    {
                        if (realIndex <= 4)
                            realIndex = 4 - p.sequence;
                        else if (realIndex == 7)
                            realIndex = 6;
                        else if (realIndex == 6) realIndex = 7;
                        returnValue.y = 0.2f;
                        returnValue.z = 18f;
                    }

                    switch (realIndex)
                    {
                        case 0:
                            returnValue.x = -17.2f;
                            break;
                        case 1:
                            returnValue.x = -8.6f;
                            break;
                        case 2:
                            returnValue.x = 0f;
                            break;
                        case 3:
                            returnValue.x = 8.6f;
                            break;
                        case 4:
                            returnValue.x = 17.2f;
                            break;
                        case 6:
                            returnValue.x = -8.6f;
                            break;
                        case 7:
                            returnValue.x = 8.6f;
                            break;
                    }
                }

                if (p.sequence == 5)
                {
                    if (p.controller == 0)
                        returnValue = new Vector3(-25f, 0.1f, -10f);
                    else
                        returnValue = new Vector3(25f, 0.1f, 10f);
                }

                if (MasterRule <= 3)
                {
                    if (p.sequence == 6)
                    {
                        if (p.controller == 0)
                            returnValue = new Vector3(-30f, 10, -15f);
                        else
                            returnValue = new Vector3(30f, 10, 10f);
                    }

                    if (p.sequence == 7)
                    {
                        if (p.controller == 0)
                            returnValue = new Vector3(30f, 10, -15f);
                        else
                            returnValue = new Vector3(-30f, 10, 10f);
                    }
                }
            }

            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Overlay))
            {
                returnValue.y -= (p.position + 2) * 0.02f;
                returnValue.x += (p.position + 1) * 0.2f;
            }
            return returnValue;
        }

        private static Vector3 ThemePilePosition(GPS p, bool banish, Vector3 original)
        {
            var background = Runtime.Program.instance?.ocgcore?.DuelBGManager;
            if (background == null || !background.TryGetThemePilePosition((int)p.controller, banish, out var target))
                return original;
            float x = 25.74f + (banish ? 1.842971f : 0);
            float z = -14.26f + (banish ? 6.236011f : 0);
            var native = p.controller == 0 ? new Vector3(x, 5, z) : new Vector3(-x, 5, -z);
            // Preserve the original concurrent-card offset and height while matching
            // the image fixture used by its native menu and movement effects.
            return original + target - native;
        }

        internal static Vector3 OriginalHandPosition(GPS p)
        {
            int count = p.InMyControl() ? Runtime.Program.instance.ocgcore.GetMyHandCount()
                : Runtime.Program.instance.ocgcore.GetOpHandCount();
            float x = p.sequence * 4 - (count - 1) * 2;
            return p.InMyControl()
                ? new Vector3(x + handOffset * Runtime.UIManager.ScreenLengthWithoutScalerX(0.038f), 15, GetMyHandBaseZ())
                : new Vector3(-x, 15, GetOpHandBaseZ());
        }

        private void RefreshHandLayout()
        {
            if (!p.InMyControl() || !p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand))
            { handLayoutVersion = -1; return; }
            int version = DuelHandLayout.Version;
            if (handLayoutVersion == version || inAnimation || addingCardToMyHand || cardLeavingMyHand) return;
            var position = model.transform.localPosition;
            position.x = DuelHandLayout.Position(p, OriginalHandPosition(p)).x;
            model.transform.localPosition = position;
            handLayoutVersion = version;
        }

        internal bool DefaultHandCorners(Vector3 position, Vector3[] corners)
        {
            if (model == null || !handColliderReady) return false;
            Vector3 rotation = GetCardRotation(p, data.Id);
            Quaternion plane = Quaternion.Euler(rotation.x, rotation.y == 0 || rotation.y == 270 ? 0 : 180, 0);
            float x = position.x, abs = Mathf.Abs(x);
            Quaternion offset = Quaternion.Euler(0, x * (abs * -.006f + 1.2f), handAngle);
            Quaternion turn = Quaternion.Euler(0, 0, rotation.z);
            Vector3 pivot = new Vector3(0, 0, -abs * (abs * .0055f + .08f));
            Vector3 half = handColliderBounds.extents;
            for (int i = 0; i < 4; i++)
            {
                var local = handColliderBounds.center + new Vector3(i < 2 ? -half.x : half.x, 0, i == 0 || i == 3 ? -half.z : half.z);
                corners[i] = position + plane * (pivot + offset * (turn * local));
            }
            return true;
        }

        private static float GetMyHandBaseZ()
        {
            if(MDPro3.Utility.DeviceInfo.OnMobile())
                return -28 + (30 - Runtime.Program.instance.camera_.cameraMain.fieldOfView) * 0.7f;
            else
                return -28f - GetHandDepthOffsetByFov();
        }

        private static float GetOpHandBaseZ()
        {
            if (MDPro3.Utility.DeviceInfo.OnMobile())
                return 17 - (30 - Runtime.Program.instance.camera_.cameraMain.fieldOfView) * 0.7f;
            else
            {
                var baseZ = 17f;
                if (CurrentReplayGodView)
                    baseZ += 2f;
                return baseZ + GetHandDepthOffsetByFov();
            }
        }

        private static float GetHandDepthOffsetByFov()
        {
            var fovDelta = Runtime.Program.instance.camera_.cameraMain.fieldOfView - 30f;
            if (fovDelta <= 0) return 0;
            return fovDelta * 0.7f;
        }

        public static Vector3 GetCardRotation(GPS p, int code = 0)
        {
            CardRuleCondition condition = CardRuleCondition.MeUpAtk;
            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Deck))
            {
                if ((p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.FaceUp) > 0)
                    condition = CardRuleCondition.MeUpDeck;
                else
                    condition = CardRuleCondition.MeDownDeck;
            }
            else if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Extra))
            {
                if ((p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.FaceUp) > 0)
                    condition = CardRuleCondition.MeUpExDeck;
                else
                    condition = CardRuleCondition.MeDownExDeck;
            }
            else if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Grave))
            {
                if ((p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.FaceUp) > 0)
                    condition = CardRuleCondition.MeUpGrave;
                else
                    condition = CardRuleCondition.MeDownGrave;
            }
            else if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Removed))
            {
                if ((p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.FaceUp) > 0)
                    condition = CardRuleCondition.MeUpRemoved;
                else
                    condition = CardRuleCondition.MeDownRemoved;
            }
            else if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.MonsterZone))
            {
                if ((p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.FaceUp) > 0)
                {
                    if ((p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.Attack) > 0)
                        condition = CardRuleCondition.MeUpAtk;
                    else
                        condition = CardRuleCondition.MeUpDef;
                }
                else
                {
                    if ((p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.Attack) > 0)
                        condition = CardRuleCondition.MeDownAtk;
                    else
                        condition = CardRuleCondition.MeDownDef;

                }
            }
            else if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.SpellZone))
            {
                if ((p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.FaceUp) > 0)
                    condition = CardRuleCondition.MeUpAtk;
                else
                    condition = CardRuleCondition.MeDownAtk;
            }
            else if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand))
            {
                if (code != 0)
                    condition = CardRuleCondition.MeUpHand;
                else
                    condition = CardRuleCondition.MeDownHand;
            }

            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Overlay))
                condition = CardRuleCondition.MeUpAtk;

            if (p.controller != 0)
            {
                switch (condition)
                {
                    case CardRuleCondition.MeUpAtk:
                        condition = CardRuleCondition.OpUpAtk;
                        break;
                    case CardRuleCondition.MeDownAtk:
                        condition = CardRuleCondition.OpDownAtk;
                        break;
                    case CardRuleCondition.MeUpDef:
                        condition = CardRuleCondition.OpUpDef;
                        break;
                    case CardRuleCondition.MeDownDef:
                        condition = CardRuleCondition.OpDownDef;
                        break;
                    case CardRuleCondition.MeUpDeck:
                        condition = CardRuleCondition.OpUpDeck;
                        break;
                    case CardRuleCondition.MeDownDeck:
                        condition = CardRuleCondition.OpDownDeck;
                        break;
                    case CardRuleCondition.MeUpExDeck:
                        condition = CardRuleCondition.OpUpExDeck;
                        break;
                    case CardRuleCondition.MeDownExDeck:
                        condition = CardRuleCondition.OpDownExDeck;
                        break;
                    case CardRuleCondition.MeUpGrave:
                        condition = CardRuleCondition.OpUpGrave;
                        break;
                    case CardRuleCondition.MeDownGrave:
                        condition = CardRuleCondition.OpDownGrave;
                        break;
                    case CardRuleCondition.MeUpRemoved:
                        condition = CardRuleCondition.OpUpRemoved;
                        break;
                    case CardRuleCondition.MeDownRemoved:
                        condition = CardRuleCondition.OpDownRemoved;
                        break;
                    case CardRuleCondition.MeUpHand:
                        condition = CardRuleCondition.OpUpHand;
                        break;
                    case CardRuleCondition.MeDownHand:
                        condition = CardRuleCondition.OpDownHand;
                        break;
                }
            }

            return condition switch
            {
                CardRuleCondition.MeUpAtk => new Vector3(0, 0, 0),
                CardRuleCondition.MeUpDef => new Vector3(0, 270, 0),
                CardRuleCondition.MeDownAtk => new Vector3(0, 0, 180),
                CardRuleCondition.MeDownDef => new Vector3(0, 270, 180),
                CardRuleCondition.MeUpDeck => new Vector3(0, -19.5f, 0),
                CardRuleCondition.MeDownDeck => new Vector3(0, -19.5f, 180),
                CardRuleCondition.MeUpExDeck => new Vector3(0, 19.5f, 0),
                CardRuleCondition.MeDownExDeck => new Vector3(0, 19.5f, 180),
                CardRuleCondition.MeUpGrave => new Vector3(0, 0, 0),
                CardRuleCondition.MeDownGrave => new Vector3(0, 270, 0),
                CardRuleCondition.MeUpRemoved => new Vector3(0, 90, 0),
                CardRuleCondition.MeDownRemoved => new Vector3(0, 90, 180),
                CardRuleCondition.MeUpHand => new Vector3(-20, 0, 0),
                CardRuleCondition.MeDownHand => new Vector3(-20, 0, 180),
                CardRuleCondition.OpUpAtk => new Vector3(0, 180, 0),
                CardRuleCondition.OpUpDef => new Vector3(0, 90, 0),
                CardRuleCondition.OpDownAtk => new Vector3(0, 180, 180),
                CardRuleCondition.OpDownDef => new Vector3(0, 90, 180),
                CardRuleCondition.OpUpDeck => new Vector3(0, 160.5f, 0),
                CardRuleCondition.OpDownDeck => new Vector3(0, 160.5f, 180),
                CardRuleCondition.OpUpExDeck => new Vector3(0, 199.5f, 0),
                CardRuleCondition.OpDownExDeck => new Vector3(0, 199.5f, 180),
                CardRuleCondition.OpUpGrave => new Vector3(0, 180, 0),
                CardRuleCondition.OpDownGrave => new Vector3(0, 180, 180),
                CardRuleCondition.OpUpRemoved => new Vector3(0, 270, 0),
                CardRuleCondition.OpDownRemoved => new Vector3(0, 270, 180),
                CardRuleCondition.OpUpHand => new Vector3(20, 180, 0),
                CardRuleCondition.OpDownHand => new Vector3(20, 180, 180),
                _ => Vector3.zero,
            };
        }

        public static Vector3 GetEffectRotaion(GPS p)
        {
            if (p.InMyControl())
            {
                if (p.InPosition(MDPro3.Duel.YGOSharp.CardPosition.Attack))
                    return new Vector3(0, 0, 0);
                else
                    return new Vector3(0, 270, 0);
            }
            else
            {
                if (p.InPosition(MDPro3.Duel.YGOSharp.CardPosition.Attack))
                    return new Vector3(0, 180, 0);
                else
                    return new Vector3(0, 90, 0);
            }
        }

        public static Vector3 GetCardScale(GPS p)
        {
            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.SpellZone))
                return new Vector3(0.8f, 1f, 0.8f);
            else if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Deck, MDPro3.Duel.YGOSharp.CardLocation.Extra))
                return new Vector3(0.9f, 1f, 0.9f);
            return Vector3.one;
        }

        private bool ThisLocationShouldHaveModel(GPS p)
        {
            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand))
                return true;
            else if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Overlay))
                return false;
            else if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Onfield))
                return true;
            else
                return false;
        }

        public bool InPendulumZone()
        {
            return InPendulumZoneIf(p, data.Id);
        }

        public static bool InPendulumZoneIf(GPS p, int code)
        {
            MDPro3.Duel.YGOSharp.Card data = MDPro3.Duel.YGOSharp.CardsManager.Get(code);
            if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.SpellZone) == 0)
                return false;
            if (MasterRule > 3)
            {
                if (p.sequence != 0 && p.sequence != 4)
                    return false;
            }
            else if (p.sequence != 6 && p.sequence != 7)
                return false;
            if (!data.HasType(MDPro3.Duel.YGOSharp.CardType.Pendulum))
                return false;
            if ((p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.FaceDown) > 0)
                return false;
            return true;
        }

        public void UpdateExDeckTop()
        {
            var player = p.controller;
            if(cacheP != null)
                player = cacheP.controller;

            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Extra) ||
                (cacheP != null && cacheP.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Extra)))
                Runtime.Program.instance.ocgcore.UpdateExDeckTop(player);
        }

        public async UniTask MoveAsync(GPS gps, bool rush = false, float wait = 0f, float overrideMoveTime = 0f)
        {

            #region Analyze

            lastMoveCard = this;

            //Reset State
            if (p.location != gps.location || gps.InPosition(MDPro3.Duel.YGOSharp.CardPosition.FaceDown))
            {
                targets.Clear();
                equipedCard = null;
                foreach (GameCard card in cards)
                    card.RemoveTarget(this);
                Disabled = false;
                setOverTurn = false;
                RefreshData();
            }

            overlays = Runtime.Program.instance.ocgcore.GCS_GetOverlays(this);
            cacheP = p;
            p = gps;

            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand)
                && p.InMyControl())
                p.position = (int)MDPro3.Duel.YGOSharp.CardPosition.FaceUpAttack;

            if (!SemiNomiSummoned
                && MDPro3.Duel.YGOSharp.CardsManager.Get(data.Id).HasType(MDPro3.Duel.YGOSharp.CardType.Monster)
                && (MDPro3.Duel.YGOSharp.CardsManager.Get(data.Id).Type & 0x68020C0) > 0
                && p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Grave, MDPro3.Duel.YGOSharp.CardLocation.Removed))
                AddStringTail(MDPro3.InterString.Get("未正规登场"));
            else
                RemoveStringTail(MDPro3.InterString.Get("未正规登场"), true);
            //Debug.LogFormat("{0}: reason: {1:X} location: {2:X}", data.Name, p.reason, p.location);

            for (int i = 0; i < overlays.Count; i++)
            {
                overlays[i].overlayParent = this;
                overlays[i].p.controller = gps.controller;
                overlays[i].p.location = gps.location | (uint)MDPro3.Duel.YGOSharp.CardLocation.Overlay;
                overlays[i].p.sequence = gps.sequence;
                overlays[i].p.position = i;
            }
            Runtime.Program.instance.ocgcore.ArrangeCards();

            if (currentMessage == MDPro3.Duel.YGOSharp.GameMessage.Move
                && cacheP.location != p.location
                && p.IsReason(MDPro3.Duel.YGOSharp.CardReason.MATERIAL)
                && !cacheP.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Overlay))
                materialCards.Add(this);

            if (!ThisLocationShouldHaveModel(p) && cacheP.location == p.location)
                return;
            if (cacheP.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Overlay)
                && (p.IsReason(MDPro3.Duel.YGOSharp.CardReason.RULE) || p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Extra)))
                return;

            #endregion

            #region Pre

            float moveTime = 0.3f;

            if (rush)
            {
                if (ThisLocationShouldHaveModel(p) && model == null)
                {
                    CreateModel();
                    ModelAt(p);
                    ShowFaceDownCardOrNot(NeedShowFaceDownCard());
                    RefreshHandShadowState();
                    if (IsFaceDownOnSpellZone())
                        setOverTurn = true;
                    if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand))
                    {
                        if (p.InMyControl())
                            needRefreshMyHand = true;
                        else
                            needRefreshOpHand = true;
                    }
                }
                return;
            }

            if (!ThisLocationShouldHaveModel(cacheP) && model != null)
            {
                Destroy(model, 1f);
                model = null;
            }
            if (model == null)
            {
                CreateModel();
                if (cacheP.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Deck))
                    cacheP.position = (int)MDPro3.Duel.YGOSharp.CardPosition.FaceDownAttack;
                model.transform.SetParent(Runtime.Program.instance.ocgcore.GetFieldTransform(p.controller));
                ModelAt(cacheP);
            }
            if ( nextMoveAction != null)
            {
                model.SetActive(false);
                var code = data.Id;
                if (code == 0)
                {
                    code = Runtime.Program.instance.ocgcore.GetNextConfirmedCardCode();
                    SetCode(code);
                }
                nextMoveAction.Invoke(code);
                await UniTask.WaitForSeconds(nextMoveActionDuration);
                return;
            }

            inAnimation = true;
            needRefreshMyHand = true;
            needRefreshOpHand = true;

            string se = string.Empty;
            var sequence = DOTween.Sequence().SetTarget(this);
            float timePassed = 0f;

            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Grave, MDPro3.Duel.YGOSharp.CardLocation.Removed))
            {
                if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Grave))
                {
                    if (p.InMyControl())
                        movingToMyGrave++;
                    else
                        movingToOpGrave++;
                }
                else
                {
                    if (p.InMyControl())
                        movingToMyExclude++;
                    else
                        movingToOpExclude++;
                }
            }

            if(cacheP != null
                && cacheP.controller == 0
                && cacheP.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand)
                && !p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand))
            {
                cardLeavingMyHand = true;
            }

            var position = GetCardPosition(p);
            var rotation = GetCardRotation(p, data.Id);

            bool handAppeal = false;
            float curveHeight = 20;
            var ease = Ease.Unset;
            if (overrideMoveTime > 0f)
            {
                moveTime = overrideMoveTime;
            }
            else
            {
                switch (currentMessage)
                {
                    case MDPro3.Duel.YGOSharp.GameMessage.Draw:
                        curveHeight = 5f;
                        if (p.controller == 0)
                        {
                            moveTime = 0.5f;
                            handAppeal = true;
                        }
                        else
                            moveTime = 0.25f;
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.Move:
                        moveTime = 0.4f;
                        if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand)
                            && p.InMyControl())
                        {
                            moveTime = 0.5f;
                            handAppeal = true;
                        }
                        if(cacheP != null
                            && !cacheP.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Onfield)
                            && p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Onfield))
                        {
                            curveHeight = 40f;
                        }
                        if (cacheP != null
                            && !cacheP.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Deck)
                            && p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Deck))
                        {
                            curveHeight = 30f;
                        }
                        if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Overlay))
                            curveHeight = 20f;
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.FlipSummoning:
                    case MDPro3.Duel.YGOSharp.GameMessage.PosChange:
                    case MDPro3.Duel.YGOSharp.GameMessage.ShuffleSetCard:
                    case MDPro3.Duel.YGOSharp.GameMessage.Swap:
                        curveHeight = 10f;
                        moveTime = 0.2f;
                        break;
                }
            }

            addingCardToMyHand = handAppeal;

            #endregion

            #region Special Move SE and Effect

            //From Grave or Exclude
            if (cacheP.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Grave, MDPro3.Duel.YGOSharp.CardLocation.Removed))
                timePassed += SequenceFromGrave(sequence, cacheP);
            //From Deck to Hand
            if (cacheP.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Deck)
                && p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand))
                se = "SE_CARD_MOVE_0" + UnityEngine.Random.Range(1, 5);
            //Destroy
            if(p.IsReason(MDPro3.Duel.YGOSharp.CardReason.DESTROY)
                && model != null
                && cacheP.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Onfield, MDPro3.Duel.YGOSharp.CardLocation.Hand))
            {
                moveTime = 0.4f;
                se = "SE_CARDBREAK_01";
                if (!data.HasType(MDPro3.Duel.YGOSharp.CardType.Token))
                {
                    var breakEffectPath = "fxp_cardbrk_bff_001";
                    var trail1Path = "fxp_grave_brksol_trail_001";
                    var trail2Path = "fxp_grave_ReCard_move_001";
                    if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Removed))
                    {
                        breakEffectPath = "fxp_exclude_001";
                        trail1Path = "fxp_exclude_brksol_trail_001";
                        trail2Path = "fxp_exclude_ReCard_move_001";
                    }
                    var fx = Runtime.ABLoader.LoadMasterDuelGameObject(breakEffectPath);
                    fx.transform.position = model.transform.position;
                    Destroy(fx, 3f);

                    manager.GetElement<Transform>("CardPlane").localScale = Vector3.zero;
                    var trail1 = Runtime.ABLoader.LoadMasterDuelGameObject(trail1Path);
                    var trail2 = Runtime.ABLoader.LoadMasterDuelGameObject(trail2Path);
                    trail1.transform.SetParent(model.transform, false);
                    trail2.transform.SetParent(model.transform, false);
                    Destroy(trail1, 3f);
                    Destroy(trail2, 3f);
                }
            }
            //Xyz Material
            if (cacheP.InLocation(MDPro3.Duel.YGOSharp.CardLocation.MonsterZone)
                && p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Overlay)
                && p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Extra))
            {
                Runtime.AudioManager.PlaySE(se);
                Runtime.AudioManager.PlaySE("SE_SUMMON_XYZ_MATERIAL");
                var fx = Runtime.ABLoader.LoadMasterDuelGameObject("XYZTrailFieldCard01");
                fx.transform.localPosition = model.transform.position;
                fx.transform.localEulerAngles = GetEffectRotaion(cacheP);
                Presentation.Elements.ElementObjectManager manager = fx.GetComponent<Presentation.Elements.ElementObjectManager>();
                var dummyFace = manager.GetNestedElement<MeshRenderer>("DummyCard01/DummyCardModel_front");
                dummyFace.material = GetMaterial();
                if (cacheP.InPosition(MDPro3.Duel.YGOSharp.CardPosition.Defence))
                    fx.transform.eulerAngles = new Vector3(0, 90, 0);
                Destroy(model);
                var director = fx.GetComponent<PlayableDirector>();
                _ = director.AutoDestroy();
                //TODO 送去墓地的特效
                await UniTask.WaitForSeconds(0.2f);
                return;
            }
            //Material
            if (ThisLocationShouldHaveModel(cacheP)
                && p.IsReason(MDPro3.Duel.YGOSharp.CardReason.MATERIAL)
                && !p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Onfield)
                && ((p.reason & ((uint)MDPro3.Duel.YGOSharp.CardReason.Ritual) + (uint)MDPro3.Duel.YGOSharp.CardReason.Fusion + (uint)MDPro3.Duel.YGOSharp.CardReason.Synchro + (uint)MDPro3.Duel.YGOSharp.CardReason.Link) > 0))
            {
                Runtime.AudioManager.PlaySE(se);
                var se2 = string.Empty;
                var trail = string.Empty;
                if (p.IsReason(MDPro3.Duel.YGOSharp.CardReason.Ritual))
                {
                    se2 = "SE_SUMMON_RITUAL_MATERIAL";
                    trail = "RitualTrailFieldCard01";
                }
                else if (p.IsReason(MDPro3.Duel.YGOSharp.CardReason.Fusion))
                {
                    se2 = "SE_SUMMON_FUS_MATERIAL";
                    trail = "FusionTrailFieldCard01";
                }
                else if (p.IsReason(MDPro3.Duel.YGOSharp.CardReason.Synchro))
                {
                    se2 = "SE_SUMMON_SYNC_MATERIAL";
                    if(GetData().HasType(MDPro3.Duel.YGOSharp.CardType.Tuner))
                        trail = "Synchro00TrailFieldCard01";
                    else
                        trail = "Synchro01TrailFieldCard01";
                }
                else if (p.IsReason(MDPro3.Duel.YGOSharp.CardReason.Link))
                {
                    se2 = "SE_SUMMON_LINK_MATERIAL";
                    trail = "LinkTrailFieldCard01";
                }

                Runtime.AudioManager.PlaySE(se2);
                var fx = Runtime.ABLoader.LoadMasterDuelGameObject(trail);
                fx.transform.localPosition = model.transform.position;
                fx.transform.localEulerAngles = GetEffectRotaion(cacheP);
                Presentation.Elements.ElementObjectManager manager = fx.GetComponent<Presentation.Elements.ElementObjectManager>();
                var dummyFace = manager.GetNestedElement<MeshRenderer>("DummyCard01/DummyCardModel_front");
                dummyFace.material = GetMaterial();

                if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Extra)
                    && !p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Overlay)
                    && p.InPosition(MDPro3.Duel.YGOSharp.CardPosition.FaceUp))
                    Runtime.Program.instance.ocgcore.SetExDeckTop(this);
                var director = fx.GetComponent<PlayableDirector>();
                _ = director.AutoDestroy();
            }
            //from unknown
            if (cacheP.location == 0)
            {
                //Token In
                if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Token))
                {
                    Runtime.AudioManager.PlaySE("SE_CARD_TOKEN_SUMMON");
                    var fx = Runtime.ABLoader.LoadMasterDuelGameObject("SummonToken01");
                    fx.transform.position = GetCardPosition(p);
                    fx.transform.localEulerAngles = GetEffectRotaion(p);
                    ModelAt(p);
                    model.SetActive(false);
                    Presentation.Elements.ElementObjectManager tokenManager = fx.GetComponent<Presentation.Elements.ElementObjectManager>();
                    _ = Runtime.Program.instance.texture_.LoadDummyCard(tokenManager.GetElement<Presentation.Elements.ElementObjectManager>("DummyCard01"), data.Id, cacheP.controller);
                    _ = UniTask.WaitForSeconds(1.25f).ContinueWith(() => { model.SetActive(true); });
                    Destroy(fx, 2f);
                    await UniTask.WaitForSeconds(0.5f);
                    return;
                }
                //Card Cheat
                else
                {
                    model.SetActive(false);
                    var fx = await Runtime.ABLoader.LoadFromFolderAsync<PlayableDirector>("MasterDuel/Timeline/Summon/CardCheat/CardCheatAppear", true, true);
                    fx.transform.position = new Vector3(0f, 48.6f, -17.7f);
                    fx.transform.localEulerAngles = new Vector3(-20f, 0f, 0f);
                    Runtime.AudioManager.PlaySE("SE_CARDCHEAT_APPEAR");
                    model.transform.position = fx.transform.position;
                    model.transform.localEulerAngles = fx.transform.localEulerAngles;
                    sequence.Pause();
                    if (data.Id == 0)
                    {
                        var code = Runtime.Program.instance.ocgcore.GetUpdateDataIdByGameCard(this);
                        SetCode(code);
                    }
                    Presentation.Elements.ElementObjectManager fxManager = fx.GetComponent<Presentation.Elements.ElementObjectManager>();
                    _ = Runtime.Program.instance.texture_.LoadDummyCard(
                        fxManager.GetElement<Presentation.Elements.ElementObjectManager>("DummyCard01"),
                        data.Id, cacheP.controller, false,
                        fxManager.GetElement<Renderer>("DummyCardModel_front02"),
                        fxManager.GetElement<Renderer>("DummyCardModel_front03"));

                    overrideMoveTime = 0.4f;
                    await UniTask.WaitForSeconds(1.25f);

                    Destroy(fx);
                    model.SetActive(true);
                }
            }
            //Token Out (to unknow)
            if (p.location == 0)
            {
                Runtime.AudioManager.PlaySE("SE_CARD_TOKEN_BREAK");
                var fx = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_bff_tokese_001");
                fx.transform.position = model.transform.position;
                fx.transform.localEulerAngles = GetEffectRotaion(p);
                Destroy(model);
                Destroy(fx, 3f);
                await UniTask.WaitForSeconds(0.2f);
                return;
            }
            //Release
            if ((p.reason & (uint)MDPro3.Duel.YGOSharp.CardReason.RELEASE) > 0 && model != null)
            {
                se = "SE_SUMMON_ADVANCE";
                var fx = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_sacrifice_rls_001");
                fx.transform.position = model.transform.position;
                Destroy(fx, 5f);
            }
            //SpSummon
            if (p.IsReason(MDPro3.Duel.YGOSharp.CardReason.SPSUMMON)
                && p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.MonsterZone)
                && !cacheP.InLocation(MDPro3.Duel.YGOSharp.CardLocation.MonsterZone)
                && !p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Overlay))
            {
                bool needSummonEffect = true;
                if (condition == Controllers.OcgCore.Condition.Duel
                    && !MDPro3.Config.GetBool("DuelSummon", true))
                    needSummonEffect = false;
                if (condition == Controllers.OcgCore.Condition.Watch
                    && !MDPro3.Config.GetBool("WatchSummon", true))
                    needSummonEffect = false;
                if (condition == Controllers.OcgCore.Condition.Replay
                    && !MDPro3.Config.GetBool("ReplaySummon", true))
                    needSummonEffect = false;

                if (needSummonEffect
                    && materialCards.Count > 0
                    && (TypeMatchReason(data.Type, (int)materialCards[0].p.reason)
                    || TypeMatchReason(data.Type, materialCards[0].GetData().Reason)))
                {
                    Runtime.Program.instance.ocgcore.GetUI<UI.ServantUI.OcgCoreUI>().CardDescription.Hide();
                    Runtime.Program.instance.ocgcore.GetUI<UI.ServantUI.OcgCoreUI>().CardList.Hide();

                    Runtime.AudioManager.PlaySE(se);
                    summonCard = this;
                    await Runtime.TimelineHelper.PlaySummonTimelineAsync();
                    return;
                }
                else
                {
                    Runtime.AudioManager.PlaySE(se);

                    bool cutin = Controllers.CutinViewer.CutinExist(data.Id);
                    sequence.Pause();
                    if (cutin)
                    {
                        if (cacheP.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Grave, MDPro3.Duel.YGOSharp.CardLocation.Removed))
                            model?.SetActive(false);
                        await Controllers.CutinViewer.Play(data.Id, (int)p.controller);
                    }
                    if (data.IsHighLevel())
                        await SequenceStrongSummon(sequence, position, rotation, 0, timePassed).WaitAsync();
                    else
                        await SequenceNormalSummon(sequence, position, rotation, 0, timePassed).WaitAsync();
                    return;
                }
            }
            //Summon
            else if (p.InPosition(MDPro3.Duel.YGOSharp.CardPosition.FaceUp)
                && p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.MonsterZone)
                && cacheP.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand)
                && !p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Overlay))
            {
                Runtime.AudioManager.PlaySE(se);
                sequence.Pause();
                bool cutin = Controllers.CutinViewer.CutinExist(data.Id);
                if (cutin)
                    await Controllers.CutinViewer.Play(data.Id, (int)p.controller);
                if (data.IsHighLevel())
                    await SequenceStrongSummon(sequence, position, rotation, 0, timePassed).WaitAsync();
                else
                    await SequenceNormalSummon(sequence, position, rotation, 0, timePassed).WaitAsync();
                return;
            }

            #endregion

            #region Normal Move

            var cardPlane = manager.GetElement<Transform>("CardPlane");
            var pivot = manager.GetElement<Transform>("Pivot");
            var offset = manager.GetElement<Transform>("Offset");
            var turn = manager.GetElement<Transform>("Turn");

            sequence.AppendInterval(wait);
            if (handAppeal)
            {
                ease = Ease.OutCubic;
            }

            sequence.Append(model.transform.DOPath(GenerateCurvePath(model.transform.position, position, curveHeight),
                moveTime, PathType.Linear, PathMode.Full3D, 10).OnStart(() =>
            {
                MoveStartAction();
            }));
            sequence.Join(model.transform.DOLocalRotate(Vector3.zero, moveTime));

            sequence.Join(pivot.DOScale(GetCardScale(p), moveTime * 0.95f));
            //Turn
            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Removed)
                || p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Deck)
                || p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Extra))
                sequence.Join(turn.DOLocalRotate(new Vector3(0, 0, rotation.z), moveTime * 0.6f));
            else
                sequence.Join(turn.DOLocalRotate(new Vector3(0, (rotation.y == 0) || (rotation.y == 180) ? 0 : 270, rotation.z), moveTime * 0.6f).SetEase(ease));
            if (handAppeal && overrideMoveTime == 0)
            {
                sequence.Join(turn.DOLocalMove(new Vector3(0, 0, 10), moveTime).SetEase(Ease.OutCubic).OnComplete(() =>
                {
                    turn.DOLocalMove(Vector3.zero, 0.2f).SetEase(Ease.InCubic); // TODO: tween in tween
                }));
            }

            //CardPlane
            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Deck)
                || p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Extra)
                || p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Removed))
                sequence.Join(cardPlane.DOLocalRotate(new Vector3(rotation.x, rotation.y, 0), moveTime * 0.5f));
            else
                sequence.Join(cardPlane.DOLocalRotate(new Vector3(rotation.x, (rotation.y == 0 || rotation.y == 270) ? 0 : 180, 0), moveTime * 0.5f));

            //Pivot && Offset
            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand))
            {
                sequence.Join(pivot.DOLocalMove(new Vector3(0, 0, HandOffsetPositionByX(position.x)), moveTime / 4));
                sequence.Join(offset.DOLocalRotate(new Vector3(0, HandOffsetRotationByX(position.x), handAngle), moveTime / 4));
                handDefault = true;
            }
            else
            {
                sequence.Join(offset.DOLocalMove(Vector3.zero, moveTime / 4));
                sequence.Join(offset.DOLocalRotate(Vector3.zero, 0.21f));
                sequence.Join(pivot.DOLocalMove(Vector3.zero, moveTime / 4));
                sequence.Join(pivot.DOLocalRotate(Vector3.zero, moveTime / 4));
            }

            //To Grave or Exclude
            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Grave, MDPro3.Duel.YGOSharp.CardLocation.Removed))
            {
                if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Grave))
                    timePassed += SequenceToGrave(sequence, p);
                else
                    timePassed += SequenceToExclude(sequence, p);
            }

            //Overlay Out
            if (cacheP.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Overlay)
                && (p.IsReason(MDPro3.Duel.YGOSharp.CardReason.EFFECT) || !p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Overlay))
                && !p.IsReason(MDPro3.Duel.YGOSharp.CardReason.RULE))
            {
                se = "SE_CARD_XYZ_OUT";
                var fx = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_bff_overlay_out_001");
                fx.transform.position = GetCardPosition(cacheP);
                Destroy(fx, 3f);

                var trail = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_bff_overlay_trail_001");
                trail.transform.SetParent(model.transform, false);
            }

            //Overlay In
            if (!cacheP.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Overlay)
                && !p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Extra)
                && p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Overlay)
                && p.IsReason(MDPro3.Duel.YGOSharp.CardReason.Xyz))
            {
                se = string.Empty;
                DOTween.To(v => { }, 0, 0, moveTime + timePassed).SetTarget(this).OnComplete(() =>
                {
                    Runtime.AudioManager.PlaySE("SE_CARD_XYZ_IN");
                    var fx = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_bff_overlay_in_001");
                    fx.transform.position = GetCardPosition(p);
                    Destroy(fx, 3f);
                });

                var trail = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_bff_overlay_trail_001");
                trail.transform.SetParent(model.transform, false);
            }

            if (handAppeal)
            {
                sequence.AppendInterval(0.2f);
            }

            sequence.OnComplete(() =>
            {
                MoveEndAction();
            });

            Runtime.AudioManager.PlaySE(se);

            sequence.Play();
            await sequence.WaitAsync();

            #endregion

        }

        private Vector3[] GenerateCurvePath(Vector3 start, Vector3 end, float curveHeight = 40f)
        {
            //curveHeight = Program.instance.testFloat;
            var endBias = 0.9f;
            Vector3 midPoint = start * (1 - endBias) + end * endBias;
            Vector3 controlPoint = midPoint + Vector3.up * curveHeight;

            Vector3[] path = new Vector3[10];
            for (int i = 0; i < 10; i++)
            {
                float rawT = i / 9.0f;
                float mappedT = Mathf.Pow(rawT, 0.5f);
                path[i] = CalculateBezierPoint(start, controlPoint, end, mappedT);
            }
            return path;
        }

        private Vector3 CalculateBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1 - t;
            return u * u * p0 + 2 * u * t * p1 + t * t * p2;
        }

        private void MoveStartAction()
        {
            if (cacheP != null && cacheP.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Extra))
                UpdateExDeckTop();
            if ((cacheP != null && cacheP.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand))
                || p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand))
                Runtime.Program.instance.ocgcore.RefreshHandCardPosition();
            if (cacheP != null && cacheP.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Deck, MDPro3.Duel.YGOSharp.CardLocation.Extra))
                Runtime.Program.instance.ocgcore.DuelBGManager.ResizeDecks();
            if (cacheP != null && cacheP.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Grave, MDPro3.Duel.YGOSharp.CardLocation.Removed))
                Runtime.Program.instance.ocgcore.DuelBGManager.RefreshGravesState();
            if ((p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.FaceDown) > 0
                || (p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone) == 0)
                HideLabel();
        }

        private void MoveEndAction()
        {
            inAnimation = false;

            var shouldHaveModel = ThisLocationShouldHaveModel(p);
            if (!shouldHaveModel)
            {
                if (model != null)
                    Destroy(model);
            }
            else if (model != null && manager != null)
            {
                var cardPlane = manager.GetElement<Transform>("CardPlane");
                if (cardPlane != null)
                    cardPlane.localScale = Vector3.one;
            }

            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Extra)
                && !p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Overlay)
                && p.InPosition(MDPro3.Duel.YGOSharp.CardPosition.FaceUp))
                Runtime.Program.instance.ocgcore.SetExDeckTop(this);

            if (shouldHaveModel && model != null && manager != null)
            {
                ShowFaceDownCardOrNot(NeedShowFaceDownCard());
                RefreshHandTurnByCode();
                RefreshHandShadowState();
            }

            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Deck, MDPro3.Duel.YGOSharp.CardLocation.Extra))
                Runtime.Program.instance.ocgcore.DuelBGManager.ResizeDecks();
            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Grave, MDPro3.Duel.YGOSharp.CardLocation.Removed))
                Runtime.Program.instance.ocgcore.DuelBGManager.RefreshGravesState();

            addingCardToMyHand = false;
            cardLeavingMyHand = false;
        }

        private void RefreshHandTurnByCode()
        {
            if (model == null || !p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand))
                return;
            manager.GetElement<Transform>("Turn").localEulerAngles = new Vector3(0, 0, data.Id == 0 ? 180 : 0);
        }

        private void RefreshHandShadowState()
        {
            if (model == null)
                return;

            var mode = p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand) && !p.InMyControl()
                ? ShadowCastingMode.Off
                : ShadowCastingMode.On;
            var cardModel = manager.GetElement<Transform>("CardModel");
            for (int i = 0; i < cardModel.childCount; i++)
            {
                var renderer = cardModel.GetChild(i).GetComponent<Renderer>();
                if (renderer != null)
                    renderer.shadowCastingMode = mode;
            }
        }

        public Sequence StartCardSequence(Vector3 fromPosition, Vector3 fromRotation, float interval = 0f)
        {
            if (model == null)
                return null;

            ResetModelPositon();
            model.transform.localPosition = fromPosition;
            model.transform.eulerAngles = fromRotation;
            var position = GetCardPosition(p);
            var rotaion = GetCardRotation(p);
            var sequence = DOTween.Sequence().SetTarget(this);
            if (data.IsHighLevel())
                return SequenceStrongSummon(sequence, position, rotaion, interval, 0f);
            else
                return SequenceNormalSummon(sequence, position, rotaion, interval, 0f);
        }

        private Sequence SequenceStrongSummon(Sequence sequence, Vector3 position, Vector3 angle, float interval, float timePassed)
        {
            var cardPlane = manager.GetElement<Transform>("CardPlane");
            var pivot = manager.GetElement<Transform>("Pivot");
            var offset = manager.GetElement<Transform>("Offset");
            var turn = manager.GetElement<Transform>("Turn");

            sequence.AppendInterval(interval);
            sequence.AppendCallback(() =>
            {
                UpdateExDeckTop();
            });

            var midP = position;
            midP.y = 15;

            sequence.Append(manager.transform.DOMove(midP, 0.4f).SetEase(Ease.InOutSine).OnStart(() =>
            {
                MoveStartAction();
            }));
            sequence.Join(manager.transform.DOLocalRotate(Vector3.zero, 0.4f).SetEase(Ease.InOutSine));
            sequence.Join(cardPlane.DOLocalRotate(new Vector3(0, (angle.y == 0) || (angle.y == 270) ? 0 : 180, 0), 0.4f).SetEase(Ease.InOutSine));
            sequence.Join(pivot.DOScale(GetCardScale(p), 0.16f).SetEase(Ease.InOutSine));
            sequence.Join(pivot.DOLocalMove(Vector3.zero, 0.16f).SetEase(Ease.InOutSine));
            sequence.Join(pivot.DOLocalRotate(Vector3.zero, 0.16f).SetEase(Ease.InOutSine));

            sequence.Join(offset.DOLocalMove(Vector3.zero, 0.4f).SetEase(Ease.InOutSine));
            sequence.Join(offset.DOLocalRotate(Vector3.zero, 0.4f).SetEase(Ease.InOutSine));
            sequence.Join(turn.DOLocalRotate(new Vector3(0, (angle.y == 0) || (angle.y == 180) ? 0 : 270, angle.z), 0.2f).SetEase(Ease.InOutSine));

            sequence.AppendInterval(0.26f);
            sequence.Insert(timePassed + interval + 0.26f, pivot.DOLocalMoveY(10, 0.4f).SetEase(Ease.InOutQuart));
            sequence.Insert(timePassed + interval + 0.26f, pivot.DOLocalRotate(new Vector3(-35, 0, 0), 0.4f).SetEase(Ease.InOutQuart));

            sequence.Append(pivot.DOLocalRotate(Vector3.zero, 0.14f).SetEase(Ease.InQuart));
            sequence.Join(pivot.DOLocalMoveY(0, 0.14f).SetEase(Ease.InQuart));
            sequence.Join(manager.transform.DOMove(position, 0.14f).SetEase(Ease.InQuart));

            sequence.OnComplete(() =>
            {
                MoveEndAction();
            });
            return sequence;
        }

        private Sequence SequenceNormalSummon(Sequence sequence, Vector3 position, Vector3 angle, float interval, float timePassed)
        {
            var cardPlane = manager.GetElement<Transform>("CardPlane");
            var pivot = manager.GetElement<Transform>("Pivot");
            var offset = manager.GetElement<Transform>("Offset");
            var turn = manager.GetElement<Transform>("Turn");

            var midP = position;
            midP.y = 10;

            sequence.AppendInterval(interval);
            sequence.Append(manager.transform.DOMove(midP, 0.3f).SetEase(Ease.InOutSine).OnStart(() =>
            {
                MoveStartAction();
            }));
            sequence.Join(manager.transform.DOLocalRotate(Vector3.zero, 0.3f).SetEase(Ease.InOutSine));
            sequence.Join(cardPlane.DOLocalRotate(new Vector3(0, (angle.y == 0) || (angle.y == 270) ? 0 : 180, 0), 0.3f).SetEase(Ease.InOutSine));
            sequence.Join(pivot.DOScale(GetCardScale(p), 0.16f).SetEase(Ease.InOutSine));
            sequence.Join(pivot.DOLocalMove(Vector3.zero, 0.16f).SetEase(Ease.InOutSine));
            sequence.Join(pivot.DOLocalRotate(Vector3.zero, 0.16f).SetEase(Ease.InOutSine));

            sequence.Join(offset.DOLocalMove(Vector3.zero, 0.3f).SetEase(Ease.InOutSine));
            sequence.Join(offset.DOLocalRotate(Vector3.zero, 0.3f).SetEase(Ease.InOutSine));
            sequence.Join(turn.DOLocalRotate(new Vector3(0, (angle.y == 0) || (angle.y == 180) ? 0 : 270, angle.z), 0.1f).SetEase(Ease.InOutSine));

            sequence.AppendInterval(0.16f);
            sequence.Insert(timePassed + interval + 0.16f, pivot.DOLocalMoveY(5, 0.3f).SetEase(Ease.InOutQuart));
            sequence.Insert(timePassed + interval + 0.16f, pivot.DOLocalRotate(new Vector3(-15, 0, 0), 0.3f).SetEase(Ease.InOutQuart));

            sequence.Append(pivot.DOLocalRotate(Vector3.zero, 0.14f).SetEase(Ease.InQuart));
            sequence.Join(pivot.DOLocalMoveY(0, 0.14f).SetEase(Ease.InQuart));
            sequence.Join(manager.transform.DOMove(position, 0.14f).SetEase(Ease.InQuart));

            sequence.OnComplete(() =>
            {
                MoveEndAction();
            });
            return sequence;
        }

        private float SequenceFromGrave(Sequence sequence, GPS p)
        {
            var dummy = Runtime.ABLoader.LoadMasterDuelGameObject(p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Grave) ? "DuelFromGrave01" : "DuelFromExclude01");
            dummy.transform.position = GetCardPosition(p);
            dummy.transform.eulerAngles = GetCardRotation(p);
            dummy.SetActive(false);
            var time = (35f / 60f);
            Presentation.Elements.ElementObjectManager dummyManager = dummy.GetComponent<Presentation.Elements.ElementObjectManager>();
            _ = Runtime.Program.instance.texture_.LoadDummyCard(dummyManager.GetElement<Presentation.Elements.ElementObjectManager>("DummyCard01"), data.Id, p.controller);
            sequence.AppendCallback(() =>
            {
                model?.SetActive(false);
                dummy.SetActive(true);
                Runtime.Program.instance.ocgcore.PlayGraveEffect(p, false);
            });
            sequence.AppendInterval(time);
            sequence.AppendCallback(() =>
            {
                model?.SetActive(true);
                Destroy(dummy);
            });

            return time;
        }

        private float SequenceToGrave(Sequence sequence, GPS p)
        {
            var count = p.InMyControl() ? movingToMyGrave : movingToOpGrave;
            if (count > 5)
            {
                Debug.Log("movingToGrave Over 5");
                count = 5;
            }
            if (count < 1)
            {
                Debug.Log("movingToGrave Less than 1");
                count = 1;
            }
            var dummy = Runtime.ABLoader.LoadMasterDuelGameObject("DuelToGrave0" + count);
            dummy.transform.position = GetCardPosition(p);
            dummy.transform.eulerAngles = GetCardRotation(p);
            var time = (30f / 60f);
            dummy.SetActive(false);
            Presentation.Elements.ElementObjectManager dummyManager = dummy.GetComponent<Presentation.Elements.ElementObjectManager>();
            _ = Runtime.Program.instance.texture_.LoadDummyCard(dummyManager.GetElement<Presentation.Elements.ElementObjectManager>("DummyCard01"), data.Id, p.controller);
            sequence.AppendCallback(() =>
            {
                Destroy(model);
                dummy.SetActive(true);
                if(!NextMessageIsMovingToGrave(p.controller))
                    Runtime.Program.instance.ocgcore.PlayGraveEffect(p, true);
                if (p.InMyControl())
                    movingToMyGrave--;
                else
                    movingToOpGrave--;
            });
            sequence.AppendInterval(time);
            sequence.AppendCallback(() =>
            {
                Destroy(dummy);
            });

            return time;
        }

        private float SequenceToExclude(Sequence sequence, GPS p)
        {
            var count = p.InMyControl() ? movingToMyExclude : movingToOpExclude;
            if (count > 5)
            {
                Debug.Log("movingToExclude Over 5");
                count = 5;
            }
            if (count < 1)
            {
                Debug.Log("movingToExclude Less than 1");
                count = 1;
            }

            var dummy = Runtime.ABLoader.LoadMasterDuelGameObject("DuelToExclude0" + count);
            dummy.transform.position = GetCardPosition(p);
            dummy.transform.eulerAngles = GetCardRotation(p);
            var time = (30f / 60f);
            dummy.SetActive(false);
            Presentation.Elements.ElementObjectManager dummyManager = dummy.GetComponent<Presentation.Elements.ElementObjectManager>();
            _ = Runtime.Program.instance.texture_.LoadDummyCard(dummyManager.GetElement<Presentation.Elements.ElementObjectManager>("DummyCard01"), data.Id, p.controller);
            sequence.AppendCallback(() =>
            {
                Destroy(model);
                dummy.SetActive(true);
                if(!NextMessageIsMovingToExclude(p.controller))
                    Runtime.Program.instance.ocgcore.PlayGraveEffect(p, true);
                if (p.InMyControl())
                    movingToMyExclude--;
                else
                    movingToOpExclude--;
            });
            sequence.AppendInterval(time);
            sequence.AppendCallback(() =>
            {
                Destroy(dummy);
            });

            return time;
        }

        private void ResetModelPositon()
        {
            if (model == null)
                return;
            model.transform.localEulerAngles = Vector3.zero;
            manager.GetElement<Transform>("CardPlane").localEulerAngles = Vector3.zero;
            manager.GetElement<Transform>("CardPlane").localPosition = Vector3.zero;
            manager.GetElement<Transform>("Pivot").localScale = GetCardScale(p);
            manager.GetElement<Transform>("Pivot").localEulerAngles = Vector3.zero;
            manager.GetElement<Transform>("Pivot").localPosition = Vector3.zero;
            manager.GetElement<Transform>("Offset").localEulerAngles = Vector3.zero;
            manager.GetElement<Transform>("Offset").localPosition = Vector3.zero;
            manager.GetElement<Transform>("Turn").localEulerAngles = Vector3.zero;
            manager.GetElement<Transform>("Turn").localPosition = Vector3.zero;
        }

        public void ResetModelRotation()
        {
            if (model == null)
                return;
            manager.transform.localEulerAngles = Vector3.zero;
            manager.GetElement<Transform>("CardPlane").localEulerAngles = Vector3.zero;
            manager.GetElement<Transform>("Pivot").localEulerAngles = Vector3.zero;
            manager.GetElement<Transform>("Offset").localEulerAngles = Vector3.zero;
            manager.GetElement<Transform>("Turn").localEulerAngles = Vector3.zero;
            manager.GetElement<Transform>("CardModel").localEulerAngles = Vector3.zero;
        }

        #region Animation

        public static float handAngle = -10f;
        private bool handDefault;
        private bool appealed = false;

        private void ModelAt(GPS gps, GameObject model = null)
        {
            Presentation.Elements.ElementObjectManager manager;
            if (model == null)
            {
                model = this.model;
                manager = model.GetComponent<Presentation.Elements.ElementObjectManager>();
            }
            else
                manager = this.manager;
            model.transform.localPosition = GetCardPosition(gps);
            Vector3 rotation = GetCardRotation(gps, data.Id);

            var cardPlane = manager.GetElement<Transform>("CardPlane");
            if ((gps.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Deck) > 0
                || (gps.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Extra) > 0
                || (gps.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Removed) > 0)
                cardPlane.localEulerAngles = new Vector3(rotation.x, rotation.y, 0);
            else
                cardPlane.localEulerAngles = new Vector3(rotation.x, (rotation.y == 0 || rotation.y == 270) ? 0 : 180, 0);

            manager.GetElement<Transform>("Pivot").localScale = GetCardScale(p);

            if ((rotation.y == 90 || rotation.y == 270) && (gps.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Removed) == 0)
                manager.GetElement<Transform>("Turn").localEulerAngles = new Vector3(0, 270, rotation.z);
            else
                manager.GetElement<Transform>("Turn").localEulerAngles = new Vector3(0, 0, rotation.z);
        }

        public void AnimationShuffle(float shuffleTime)
        {
            inAnimation = true;
            if (!p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand))
                return;
            if (model != null)
            {
                manager.GetElement<Transform>("Pivot").DOLocalMoveZ(0, shuffleTime);
                manager.GetElement<Transform>("Offset").DOLocalRotate(Vector3.zero, shuffleTime);
                manager.GetElement<Transform>("Turn").DOLocalRotate(new Vector3(0, 0, 180), shuffleTime);

                model.transform.DOLocalMoveX(0, shuffleTime).OnComplete(() =>
                {
                    if (cards.Contains(this))
                        AnimationHandDefault(shuffleTime, true);
                    else
                        Dispose();// null for TagSwap

                    //shuffling = false;
                });
            }
            else// null for TagSwap
            {
                DOTween.To(v => { }, 0, 0, shuffleTime).SetTarget(this).OnComplete(() =>
                {
                    CreateModel();
                    ModelAt(p);
                    manager.GetElement<Transform>("Pivot").localPosition = Vector3.zero;
                    manager.GetElement<Transform>("Offset").localEulerAngles = Vector3.zero;
                    manager.GetElement<Transform>("Turn").localEulerAngles = new Vector3(0, 0, 180);
                    var position = model.transform.localPosition;
                    model.transform.localPosition = new Vector3(0, position.y, position.z);
                    AnimationHandDefault(shuffleTime, true);
                });
            }
        }

        public float HandOffsetRotationByX(float x)
        {
            x = DuelHandLayout.FanX(this, x);
            var abs = x > 0 ? x : -x;
            return x * (abs * -0.006f + 1.2f) * ((p.controller == 0) ? 1 : -1);
        }

        public float HandOffsetPositionByX(float x)
        {
            x = DuelHandLayout.FanX(this, x);
            var abs = x > 0 ? x : -x;
            return -abs * (abs * 0.0055f + 0.08f);
        }

        public void AnimationHandDefault(float time, bool ignore = false)
        {
            if (model == null || !p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand) || (inAnimation && !ignore))
                return;

#if UNITY_EDITOR
            if (Program.instance.debugCardAnimation)
            {
                Debug.Log("AnimationHandDefault: " + data.Name);
            }

#endif

            model.transform.SetParent(null, true);
            handDefault = true;
            appealed = false;

            var targetPosition = GetCardPosition(p);
            var x = targetPosition.x;

            Transform pivot = manager.GetElement<Transform>("Pivot");
            Transform offset = manager.GetElement<Transform>("Offset");
            Transform turn = manager.GetElement<Transform>("Turn");

            MoveToHandDefault(time, true);
            pivot.DOLocalMove(new Vector3(0, 0, HandOffsetPositionByX(x)), time).OnComplete(() =>
            {
                if (ignore)
                {
                    inAnimation = false;
                }
            });
            offset.DOLocalMove(Vector3.zero, time);
            offset.DOLocalRotate(new Vector3(0, HandOffsetRotationByX(x), handAngle), time);
            if (data.Id == 0)
                turn.DOLocalRotate(new Vector3(0, 0, 180), time);
            else
                turn.DOLocalRotate(Vector3.zero, time);
        }

        private void MoveToHandDefault(float duration, bool ignore = false)
        {
            if ((inAnimation && !ignore) || !p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand))
                return;

#if UNITY_EDITOR
            if (Program.instance.debugCardAnimation)
            {
                Debug.Log("MoveToHandDefault: " + data.Name);
            }

#endif

            var targetPosition = GetCardPosition(p);
            if (HideMyHandCard && p.InMyControl())
            {
                var z = GetMyHandBaseZ();
                if(z > -28f) z = -28f;
                targetPosition.z = z;
            }
            if (HideOpHandCard && !p.InMyControl())
            {
                var z = GetOpHandBaseZ();
                if (z < 17f) z = 17f;
                targetPosition.z = z;
            }
            model.transform.DOLocalMove(targetPosition, duration).OnComplete(() =>
            {
                // A display change may have reprojected X while this original
                // root move was still writing its captured target.
                handLayoutVersion = -1;
            }).OnKill(() => handLayoutVersion = -1);
        }

        public void SetHandToDefault()
        {
            if (model == null || !p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand) || inAnimation)
                return;

            clicked = false;
            handDefault = false;
        }

        public void SetHandDefault()
        {
            if (model == null || !p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand))
                return;

#if UNITY_EDITOR
            if (Program.instance.debugCardAnimation)
            {
                Debug.Log("SetHandDefault: " + data.Name);
            }

#endif
            KillHandTransformTweens();
            appealed = false;
            model.transform.localPosition = GetCardPosition(p);
            float x = model.transform.localPosition.x;
            manager.GetElement<Transform>("Pivot").localPosition = new Vector3(0, 0, HandOffsetPositionByX(x));
            manager.GetElement<Transform>("Offset").localPosition = Vector3.zero;
            manager.GetElement<Transform>("Offset").localEulerAngles = new Vector3(0, HandOffsetRotationByX(x), handAngle);
            manager.GetElement<Transform>("Turn").localEulerAngles = new Vector3(0, 0, (data.Id == 0) ? 180 : 0);
        }

        private void KillHandTransformTweens()
        {
            if(model == null)
                return;

            var pivot = manager.GetElement<Transform>("Pivot");
            var offset = manager.GetElement<Transform>("Offset");
            var turn = manager.GetElement<Transform>("Turn");
            DOTween.Kill(model.transform, false);
            DOTween.Kill(pivot, false);
            DOTween.Kill(offset, false);
            DOTween.Kill(turn, false);
        }

        private void AnimationHandHover()
        {
            if (inAnimation || !p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand) || clickingHandCard)
                return;
            if(model == null)
                ShowUnexpectedModelMissingError("AnimationHandHover");

#if UNITY_EDITOR
            if (Program.instance.debugCardAnimation)
            {
                Debug.Log("AnimationHandHover: " + data.Name);
            }

#endif

            var offset = manager.GetElement<Transform>("Offset");
            DOTween.Kill(offset, false);
            offset.DOLocalMove(new Vector3(0, 2, 1), 0.1f);
        }

        public void AnimationHandAppeal()
        {
            if (appealed || inAnimation || !p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand))
                return;
            if (model == null)
                ShowUnexpectedModelMissingError("AnimationHandAppeal");

#if UNITY_EDITOR
            if (Program.instance.debugCardAnimation)
            {
                Debug.Log("AnimationHandAppeal: " + data.Name);
            }

#endif

            appealed = true;
            var pivot = manager.GetElement<Transform>("Pivot");
            var offset = manager.GetElement<Transform>("Offset");
            DOTween.Kill(pivot, false);
            DOTween.Kill(offset, false);
            pivot.DOLocalMove(new Vector3(0, 2, 3), 0.1f);
            offset.DOLocalRotate(Vector3.zero, 0.1f);
            offset.DOLocalMove(Vector3.zero, 0.1f);
            Runtime.AudioManager.PlaySE("SE_CARD_MOVE_0" + UnityEngine.Random.Range(1, 5));
        }

        public Sequence AnimationNegate()
        {
            nextNegateAction?.Invoke();
            nextNegateAction = null;
            Runtime.AudioManager.PlaySE("SE_EFFECT_INVALID");

            Runtime.CameraManager.BlackInOut(0f, 0.1f, 0.7f, 0.2f);
            Presentation.Elements.ElementObjectManager manager;
            GameObject model;
            if (ThisLocationShouldHaveModel(p))
            {
                if(this.model == null)
                {
                    ShowUnexpectedModelMissingError("AnimationNegate");
                    CreateModel();
                    ModelAt(p);
                }

                model = this.model;
                manager = this.manager;
            }
            else
            {
                model = CreateModel(false);
                ModelAt(p, model);
                manager = model.GetComponent<Presentation.Elements.ElementObjectManager>();
            }

            var cardFace = manager.GetElement<Transform>("CardModel").GetChild(1).GetComponent<Renderer>();
            var originMono = cardFace.material.GetFloat("_Monochrome");
            Utility.Tools.ChangeLayer(model, "DuelOverlay3D");
            Runtime.CameraManager.DuelOverlay3DPlus();
            bool overlayReleased = false;
            bool temporaryModel = model != this.model;
            void ReleaseOverlay()
            {
                if (overlayReleased) return;
                overlayReleased = true;
                Runtime.CameraManager.DuelOverlay3DMinus();
            }
            manager.GetElement("EffectNegate").SetActive(false);
            manager.GetElement("EffectNegate").SetActive(true);
            var pivot = manager.GetElement<Transform>("Pivot");
            var offset = manager.GetElement<Transform>("Offset");
            var scale = pivot.localScale;

            bool additional = false;
            if (nextNegateAction_Additional != null)
            {
                nextNegateAction_Additional?.Invoke();
                nextNegateAction_AdditionalManager.transform.SetParent(offset, false);
                nextNegateAction_Additional = null;
                additional = true;
            }

            var showTime = 0.7f;
            if (additional)
                showTime += nextNegateAction_AdditionalTime;

            var sequence = DOTween.Sequence().SetTarget(this).OnKill(() =>
            {
                ReleaseOverlay();
                if (temporaryModel && model != null) Destroy(model);
            });
            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Deck, MDPro3.Duel.YGOSharp.CardLocation.Extra, MDPro3.Duel.YGOSharp.CardLocation.Onfield))
            {
                HideLabel();
                var height = 10f;
                if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Deck, MDPro3.Duel.YGOSharp.CardLocation.Extra))
                    height = 5f;
                sequence.Append(offset.DOLocalMoveY(height, 0.1f));
                sequence.Join(DOTween.To(() => originMono, x => cardFace.material.SetFloat("_Monochrome", x), 1, 0.1f));
                sequence.Join(pivot.DOScale(1f, 0.1f));
                sequence.AppendInterval(showTime);
                sequence.Append(offset.DOLocalMoveY(0f, 0.2f));
                if (Disabled)
                    originMono = 1f;
                if (originMono != 1f)
                    sequence.Join(DOTween.To(() => 1, x => cardFace.material.SetFloat("_Monochrome", x), originMono, 0.2f));
                sequence.Join(pivot.DOScale(scale, 0.2f));
                sequence.OnComplete(() =>
                {
                    Utility.Tools.ChangeLayer(model, "Default");
                    ReleaseOverlay();
                    RefreshLabel();
                    if (!ThisLocationShouldHaveModel(p))
                        Destroy(model);
                });
            }
            else if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Hand) > 0)
            {
                inAnimation = true;
                if (p.controller != 0)
                    manager.GetElement<Transform>("Turn").DOLocalRotate(Vector3.zero, 0.1f);
                var originRotaion = pivot.localEulerAngles;

                sequence.Append(offset.DOLocalMoveY(1, 0.1f));
                sequence.Join(offset.DOLocalMoveZ(5, 0.1f));
                sequence.Join(offset.DOLocalRotate(Vector3.zero, 0.1f));
                sequence.Join(pivot.DOLocalRotate(Vector3.zero, 0.1f));
                sequence.Join(manager.GetElement<Transform>("Turn").DOLocalRotate(Vector3.zero, 0.1f));
                sequence.Join(DOTween.To(() => originMono, x => cardFace.material.SetFloat("_Monochrome", x), 1, 0.1f));
                sequence.Append(offset.DOLocalMoveY(1.2f, showTime));
                sequence.Join(offset.DOLocalMoveZ(5.5f, showTime));
                sequence.Append(offset.DOLocalMoveY(0f, 0.2f));
                sequence.Join(offset.DOLocalMoveZ(0f, 0.2f));
                sequence.Join(pivot.DOLocalRotate(originRotaion, 0.15f));
                sequence.Join(DOTween.To(() => 1, x => cardFace.material.SetFloat("_Monochrome", x), 0, 0.2f));
                sequence.Insert(0, pivot.DOScale(1.2f, 0.2f));
                sequence.Insert(showTime + 0.1f, pivot.DOScale(scale, 0.2f));
                sequence.OnComplete(() =>
                {
                    Utility.Tools.ChangeLayer(model, "Default");
                    ReleaseOverlay();
                    inAnimation = false;
                    cardFace.material.SetFloat("_Monochrome", originMono);
                });
            }
            else if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Grave) > 0
                || (p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Removed) > 0)
            {
                offset.localPosition = new Vector3(0, -2, 0);
                sequence.Append(offset.DOLocalMoveY(0, 0.1f));
                sequence.Join(DOTween.To(() => originMono, x => cardFace.material.SetFloat("_Monochrome", x), 1, 0.1f));
                sequence.Join(offset.DOScale(1f, 0.1f));
                sequence.AppendInterval(showTime);
                sequence.Append(offset.DOLocalMoveY(-2f, 0.2f));
                sequence.Join(DOTween.To(() => 1, x => cardFace.material.SetFloat("_Monochrome", x), 0, 0.2f));
                sequence.Join(offset.DOScale(Vector3.one * 0.5f, 0.2f));
                sequence.OnComplete(() =>
                {
                    Destroy(model);
                    ReleaseOverlay();
                    cardFace.material.SetFloat("_Monochrome", originMono);
                });
            }

            return sequence;
        }

        public Sequence AnimationActivate()
        {
            Runtime.AudioManager.PlaySE("SE_CARDVIEW_01");
            Runtime.CameraManager.BlackInOut(0f, 0.3f, 0.4f, 0.3f);
            Presentation.Elements.ElementObjectManager manager;
            GameObject model;
            if (ThisLocationShouldHaveModel(p))
            {
                if (this.model == null)
                {
                    ShowUnexpectedModelMissingError("AnimationActivate");
                    CreateModel();
                    ModelAt(p);
                }

                model = this.model;
                manager = this.manager;
            }
            else
            {
                model = CreateModel(false);
                ModelAt(p, model);
                manager = model.GetComponent<Presentation.Elements.ElementObjectManager>();
            }
            Utility.Tools.ChangeLayer(model, "DuelOverlay3D");
            Runtime.CameraManager.DuelOverlay3DPlus();
            bool overlayReleased = false;
            bool temporaryModel = model != this.model;
            void ReleaseOverlay()
            {
                if (overlayReleased) return;
                overlayReleased = true;
                Runtime.CameraManager.DuelOverlay3DMinus();
            }
            manager.GetElement("EffectBuffActive").SetActive(false);
            manager.GetElement("EffectBuffActive").SetActive(true);
            var pivot = manager.GetElement<Transform>("Pivot");
            var offset = manager.GetElement<Transform>("Offset");
            var scale = pivot.localScale;
            var sequence = DOTween.Sequence().SetTarget(this).OnKill(() =>
            {
                ReleaseOverlay();
                if (temporaryModel && model != null) Destroy(model);
            });

            var ease = Ease.OutCubic;

            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Deck, MDPro3.Duel.YGOSharp.CardLocation.Extra, MDPro3.Duel.YGOSharp.CardLocation.Onfield))
            {
                HideLabel();
                var height = 10f;
                if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Deck, MDPro3.Duel.YGOSharp.CardLocation.Extra))
                    height = 5f;
                sequence.Append(offset.DOLocalMoveY(height, 0.2f).SetEase(ease));
                sequence.Join(pivot.DOScale(1f, 0.2f).SetEase(ease));
                sequence.Append(offset.DOLocalMoveY(height * 1.1f, 0.7f).SetEase(ease));
                sequence.Append(offset.DOLocalMoveY(0f, 0.2f));
                sequence.Join(pivot.DOScale(scale, 0.2f));
                sequence.OnComplete(() =>
                {
                    Utility.Tools.ChangeLayer(model, "Default");
                    ReleaseOverlay();
                    RefreshLabel();
                    if (!ThisLocationShouldHaveModel(p))
                        Destroy(model);
                });
            }
            else if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Hand) > 0)
            {
                inAnimation = true;
                if (p.controller != 0)
                    manager.GetElement<Transform>("Turn").DOLocalRotate(Vector3.zero, 0.1f);
                var originRotaion = pivot.localEulerAngles;

                sequence.Append(offset.DOLocalMoveY(1, 0.2f).SetEase(ease));
                sequence.Join(offset.DOLocalMoveZ(5, 0.2f).SetEase(ease));
                sequence.Join(offset.DOLocalRotate(Vector3.zero, 0.2f).SetEase(ease));
                sequence.Join(pivot.DOLocalRotate(Vector3.zero, 0.2f).SetEase(ease));
                sequence.Join(manager.GetElement<Transform>("Turn").DOLocalRotate(Vector3.zero, 0.2f).SetEase(ease));
                sequence.Append(offset.DOLocalMoveY(1.1f, 0.7f));
                sequence.Join(offset.DOLocalMoveZ(5.1f, 0.7f));
                sequence.Append(offset.DOLocalMoveY(0f, 0.2f));
                sequence.Join(offset.DOLocalMoveZ(0f, 0.2f));
                sequence.Join(pivot.DOLocalRotate(originRotaion, 0.2f));
                sequence.Insert(0, pivot.DOScale(1.1f, 0.15f).SetEase(ease));
                sequence.Insert(0.9f, pivot.DOScale(scale, 0.2f));
                sequence.OnComplete(() =>
                {
                    Utility.Tools.ChangeLayer(model, "Default");
                    ReleaseOverlay();
                    inAnimation = false;
                });
            }
            else if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Grave) > 0
                || (p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Removed) > 0)
            {
                offset.localPosition = new Vector3(0, -1, 0);
                offset.localScale = Vector3.one * 0.5f;
                sequence.Append(offset.DOLocalMoveY(5f, 0.2f).SetEase(ease));
                sequence.Join(offset.DOScale(1f, 0.2f).SetEase(ease));
                sequence.Append(offset.DOLocalMoveY(5.5f, 0.7f).SetEase(ease));
                sequence.Append(offset.DOLocalMoveY(-1f, 0.2f));
                sequence.Join(offset.DOScale(0.5f, 0.2f));
                sequence.OnComplete(() =>
                {
                    Destroy(model);
                    ReleaseOverlay();
                });
            }

            return sequence;
        }

        public Sequence AnimationConfirm(int id)
        {
            if (!ThisLocationShouldHaveModel(p))
            {
                CreateModel();
                ModelAt(p);
                model.SetActive(false);
            }
            else
            {
                if (model == null)
                {
                    ShowUnexpectedModelMissingError("AnimationConfirm");
                    CreateModel();
                    ModelAt(p);
                }
            }
            inAnimation = true;
            var offset = manager.GetElement<Transform>("Offset");
            var offsetPosition = offset.localPosition;
            var turn = manager.GetElement<Transform>("Turn");
            var turnEulerAngles = turn.localEulerAngles;

            var sequence = DOTween.Sequence().SetTarget(this);
            if(id > 0)
                sequence.AppendInterval(id);

            var endPosition = new Vector3(0f, 2f, 3f);
            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Grave, MDPro3.Duel.YGOSharp.CardLocation.Removed))
                endPosition.z = 0f;

            sequence.Append(offset.DOLocalMove(endPosition, 0.1f).OnStart(() =>
            {
                model.SetActive(true);
                ShowFaceDownCardOrNot(false);
                Runtime.AudioManager.PlaySE("SE_CARDVIEW_02");
                if(Runtime.Program.instance.ocgcore.GetAutoInfo())
                    Runtime.Program.instance.ocgcore.GetUI<UI.ServantUI.OcgCoreUI>().CardDescription.Show(this, null);
            }));
            sequence.Join(turn.DOLocalRotate(Vector3.zero, 0.1f).OnComplete(() =>
            {
                var highlight = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_card_decide_001");
                highlight.transform.SetPositionAndRotation(offset.position, offset.rotation);
                highlight.transform.localScale = GetCardScale(p);
                Destroy(highlight, 1f);
            }));
            sequence.AppendInterval(0.8f);
            sequence.AppendCallback(() =>
            {
                inAnimation = false;
                if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Hand) > 0)
                    SetHandToDefault();
                else
                {
                    offset.DOLocalMove(offsetPosition, 0.1f);
                    turn.DOLocalRotate(turnEulerAngles, 0.1f).OnComplete(() =>
                    {
                        if (!ThisLocationShouldHaveModel(p) && model != null)
                            Destroy(model);
                    });
                }
                ShowFaceDownCardOrNot(NeedShowFaceDownCard());
            });
            return sequence;
        }

        public void AnimationPosition(float delay = 0)
        {
            if (model == null)
            {
                ShowUnexpectedModelMissingError("AnimationPosition");
                return;
            }

            Presentation.Elements.ElementObjectManager positionManager = manager.GetElement<Presentation.Elements.ElementObjectManager>("FieldCardChangeIcon");
            var sequence = DOTween.Sequence().SetTarget(this);
            sequence.AppendInterval(delay);
            if ((p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.Attack) > 0)
            {
                Runtime.AudioManager.PlaySE("SE_DISP_ATTACK", 0.6f);
                positionManager.GetElement<Transform>("Sword1").localEulerAngles = new Vector3(90, 0, 0);
                positionManager.GetElement<Transform>("Sword2").localEulerAngles = new Vector3(90, 0, 0);
                sequence.Append(positionManager.GetElement<Transform>("Sword1").DOLocalRotate(new Vector3(90, 0, 60), 0.2f).SetEase(Ease.OutCubic));
                sequence.Join(positionManager.GetElement<Transform>("Sword2").DOLocalRotate(new Vector3(90, 0, -60), 0.2f).SetEase(Ease.OutCubic));
                sequence.Join(positionManager.GetElement<SpriteRenderer>("Sword1").DOFade(1, 0.35f).SetEase(Ease.OutCubic));
                sequence.Join(positionManager.GetElement<SpriteRenderer>("Sword2").DOFade(1, 0.35f).SetEase(Ease.OutCubic));
                sequence.Insert(0.2f, positionManager.GetElement<Transform>("Sword1").DOLocalRotate(new Vector3(90, 0, 45), 0.15f).SetEase(Ease.OutCubic));
                sequence.Insert(0.2f, positionManager.GetElement<Transform>("Sword2").DOLocalRotate(new Vector3(90, 0, -45), 0.15f).SetEase(Ease.OutCubic));
                sequence.AppendInterval(0.2f);
                sequence.Append(positionManager.GetElement<SpriteRenderer>("Sword1").DOFade(0, 0.3f).SetEase(Ease.InQuad));
                sequence.Join(positionManager.GetElement<SpriteRenderer>("Sword2").DOFade(0, 0.3f).SetEase(Ease.InQuad));
            }
            else
            {
                Runtime.AudioManager.PlaySE("SE_DISP_DEFENS", 0.6f);
                positionManager.GetElement<Transform>("Defense").localScale = new Vector3(3, 3, 3);
                sequence.Append(positionManager.GetElement<Transform>("Defense").DOScale(new Vector3(3.8f, 3.8f, 3.8f), 0.35f));
                sequence.Join(positionManager.GetElement<SpriteRenderer>("Defense").DOFade(1, 0.3f).SetEase(Ease.OutCubic));
                sequence.AppendInterval(0.2f);
                sequence.Append(positionManager.GetElement<SpriteRenderer>("Defense").DOFade(0, 0.3f).SetEase(Ease.InCubic));
            }
        }

        public void AnimationTarget()
        {
            Runtime.AudioManager.PlaySE("SE_CEMETERY_CARD");

            GameObject model;
            if (ThisLocationShouldHaveModel(p))
            {
                if(this.model == null)
                {
                    ShowUnexpectedModelMissingError("AnimationTarget");
                    CreateModel();
                    ModelAt(p);
                }
                model = this.model;
            }
            else
            {
                model = CreateModel(false);
                ModelAt(p, model);
                Destroy(model, 0.49f);
            }

            var fx = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_card_decide_001");
            fx.transform.position = model.transform.position;
            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.MonsterZone) && p.InPosition(MDPro3.Duel.YGOSharp.CardPosition.Defence))
                fx.transform.localEulerAngles = new Vector3(0, 90, 0);
            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Removed))
                fx.transform.localEulerAngles = new Vector3(0, 90, 0);
            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.SpellZone))
                fx.transform.localScale = Vector3.one * 0.8f;
            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Deck, MDPro3.Duel.YGOSharp.CardLocation.Extra))
                fx.transform.localEulerAngles = new Vector3(0, GetCardRotation(p).y, 0);
            Destroy(fx, 1f);
        }

        private readonly float[] bounceDutationsHuge = new float[] { 0.4f, 0.2f, 0.133f, 0.133f, 0.067f, 0.067f };
        private readonly float[] bounceDutationsSlight = new float[] { 0.2f, 0.133f, 0.133f, 0.067f };

        public void AnimationLandShake(GameCard card, int shakeLevel)
        {
            if(shakeLevel == 0)
                return;
            if (card == this)
                return;
            if (!p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Onfield))
                return;
            if (model == null) // Overlays
                return;

            const float minCardsDistance = 8.6f;
            const float delayOffset = 0.01f;
            float distance = Vector3.Distance(card.model.transform.position, model.transform.position);
            float delay = Math.Max(0, distance - minCardsDistance) * delayOffset;
            Vector3 direction = (card.model.transform.position - model.transform.position).normalized;
            direction.y = 0;

            if (shakeLevel == 2)
            {
                int bounceCount = 6;
                float height = 5f;
                float angle = 50f;

                Sequence seq = DOTween.Sequence().SetTarget(this);
                seq.AppendInterval(delay);
                model.transform.GetPositionAndRotation(out Vector3 originalPosition, out Quaternion originalRotation);

                for (int i = 0; i < bounceCount; i++)
                {
                    float decay = Mathf.Pow(0.7f, i);

                    float duration = bounceDutationsHuge[i];
                    float currentHeight = height * decay;
                    float currentAngle = angle * decay;

                    Vector3 bounceDirection = (i % 2 == 0) ? direction : -direction;
                    Vector3 axis = Vector3.Cross(bounceDirection, Vector3.up).normalized;
                    Quaternion rotation = Quaternion.AngleAxis(currentAngle, axis);

                    seq.Append(model.transform.DOMoveY(originalPosition.y + currentHeight, duration / 2)
                        .SetEase(Ease.OutQuad))
                        .Join(model.transform.DORotateQuaternion(rotation, duration / 2).SetEase(Ease.OutQuad));

                    seq.Append(model.transform.DOMoveY(originalPosition.y, duration / 2).SetEase(Ease.InQuad))
                        .Join(model.transform.DORotateQuaternion(originalRotation, duration / 2).SetEase(Ease.InQuad));
                }
            }
            else if(shakeLevel == 1)
            {
                int bounceCount = 4;
                float height = 3f;
                float angle = 10f;

                Sequence seq = DOTween.Sequence().SetTarget(this);
                seq.AppendInterval(delay);
                model.transform.GetPositionAndRotation(out Vector3 originalPosition, out Quaternion originalRotation);

                for (int i = 0; i < bounceCount; i++)
                {
                    float decay = Mathf.Pow(0.7f, i);

                    float duration = bounceDutationsSlight[i];
                    float currentHeight = height * decay;
                    float currentAngle = angle * decay;

                    Vector3 bounceDirection = (i % 2 == 0) ? direction : -direction;
                    Vector3 axis = Vector3.Cross(bounceDirection, Vector3.up).normalized;
                    Quaternion rotation = Quaternion.AngleAxis(currentAngle, axis);

                    seq.Append(model.transform.DOMoveY(originalPosition.y + currentHeight, duration / 2)
                        .SetEase(Ease.OutQuad))
                        .Join(model.transform.DORotateQuaternion(rotation, duration / 2).SetEase(Ease.OutQuad));

                    seq.Append(model.transform.DOMoveY(originalPosition.y, duration / 2).SetEase(Ease.InQuad))
                        .Join(model.transform.DORotateQuaternion(originalRotation, duration / 2).SetEase(Ease.InQuad));
                }
            }
        }

        public Sequence AnimationConfirmDeckTop(int id)
        {
            CreateModel();
            GPS pTop = p.Copy();
            pTop.sequence = (uint)Runtime.Program.instance.ocgcore.GetLocationCardCount((MDPro3.Duel.YGOSharp.CardLocation)p.location, p.controller) - 1;

            ModelAt(pTop);
            model.SetActive(false);
            var turn = manager.GetElement<Transform>("Turn");
            var sequence = DOTween.Sequence().SetTarget(this);
            sequence.AppendInterval(1f * id);
            sequence.Append(turn.DOLocalMoveY(2, 0.1f).OnStart(() =>
            {
                if (Runtime.Program.instance.ocgcore.GetAutoInfo())
                    Runtime.Program.instance.ocgcore.GetUI<UI.ServantUI.OcgCoreUI>().CardDescription.Show(this, null);

                model.SetActive(true);
                if (Runtime.Program.instance.ocgcore.GetLocationCardCount(MDPro3.Duel.YGOSharp.CardLocation.Deck, p.controller) == 1)
                {
                    Presentation.Elements.ElementObjectManager deckModel = Runtime.Program.instance.ocgcore.GetDeckModel(p.controller, MDPro3.Duel.YGOSharp.CardLocation.Deck);
                    Runtime.Program.instance.ocgcore.SetDeckModelActive(deckModel, false);
                }
            }));
            sequence.Join(turn.DOLocalRotate(Vector3.zero, 0.1f));
            sequence.Append(turn.DOLocalMoveY(0.1f * (id + 1), 0.1f).OnComplete(() =>
            {
                Runtime.AudioManager.PlaySE("SE_CARDVIEW_02");
                var effect = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_card_decide_deck_001");
                effect.transform.SetPositionAndRotation(turn.position, turn.rotation);
                effect.transform.localScale = GetCardScale(p);
                Destroy(effect, 1f);
            }));
            sequence.AppendInterval(0.6f);
            sequence.Append(turn.DOLocalMoveY(2, 0.1f));
            sequence.Join(turn.DOLocalRotate(new Vector3(0, 0, 180), 0.1f));
            sequence.Append(turn.DOLocalMoveY(0f, 0.1f));
            sequence.OnComplete(() =>
            {
                Destroy(model);
                Presentation.Elements.ElementObjectManager deckModel = Runtime.Program.instance.ocgcore.GetDeckModel(p.controller, MDPro3.Duel.YGOSharp.CardLocation.Deck);
                Runtime.Program.instance.ocgcore.SetDeckModelActive(deckModel, true);
            });

            return sequence;
        }

        private void ShowUnexpectedModelMissingError(string animationName)
        {
            Runtime.MessageManager.Cast($"Card [{data?.Name}] model unexpected missing in {animationName}.");
        }

        #endregion

        #region Button

        public struct DuelButtonInfo
        {
            public List<int> response;
            public string hint;
            public UI.ButtonType type;
        }

        private bool hightYellow = false;

        public List<DuelButtonInfo> buttons = new();
        private readonly List<UI.DuelButton> buttonObjs = new();

        public void AddButton(int response, string hint, UI.ButtonType type)
        {
            bool exist = false;
            foreach (DuelButtonInfo button in buttons)
                if (button.type == type)
                {
                    exist = true;
                    button.response.Add(response);
                }
            if (!exist)
                buttons.Add(new DuelButtonInfo() { response = new List<int>() { response }, hint = hint, type = type });
        }
        public void CreateButtons()
        {
            if (model == null || buttons.Count == 0)
                return;
            buttons.Sort((x, y) => x.type.CompareTo(y.type));
            for (int i = 0; i < buttons.Count; i++)
            {
                var obj = Runtime.ABLoader.LoadMasterDuelGameObject("DuelButton");
                UI.DuelButton mono = obj.GetComponent<UI.DuelButton>();
                buttonObjs.Add(mono);
                mono.response = buttons[i].response;
                mono.hint = buttons[i].hint;
                mono.type = buttons[i].type;
                mono.id = i;
                mono.buttonsCount = buttons.Count;
                mono.cookieCard = this;
            }
            hightYellow = false;
            foreach (DuelButtonInfo button in buttons)
            {
                if (button.type == UI.ButtonType.Activate
                    || button.type == UI.ButtonType.PenSummon
                    || button.type == UI.ButtonType.SetPendulum
                    || button.type == UI.ButtonType.SpSummon
                    )
                { hightYellow = true; break; }
            }

            AdjustHighlight(0.05f);
        }

        private void AdjustHighlight(float offset)
        {
            if(model == null) return;
            if (hightYellow)
                manager.GetElement("EffectHighlightYellow").SetActive(true);
            else
                manager.GetElement("EffectHighlightBlue").SetActive(true);

            var t1 = manager.GetElement<Transform>("EffectHighlightYellow").GetChild(0);
            var t2 = manager.GetElement<Transform>("EffectHighlightBlue").GetChild(0);
            var t3 = manager.GetElement<Transform>("EffectHighlightYellowSelect").GetChild(0);
            var t4 = manager.GetElement<Transform>("EffectHighlightBlueSelect").GetChild(0);
            var needDown = p.InPosition(MDPro3.Duel.YGOSharp.CardPosition.FaceUp);
            if (p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand))
                needDown = true;
            var position = new Vector3(0f, needDown ? -offset : offset, 0f);
            t1.localPosition = position;
            t2.localPosition = position;
            t3.localPosition = position;
            t4.localPosition = position;
        }

        public void ClearButtons()
        {
            buttons.Clear();
            if (model == null)
                return;
            foreach (UI.DuelButton button in buttonObjs)
                Destroy(button.gameObject);
            buttonObjs.Clear();
            manager.GetElement("EffectHighlightBlue").SetActive(false);
            manager.GetElement("EffectHighlightYellow").SetActive(false);
            manager.GetElement("EffectHighlightBlueSelect").SetActive(false);
            manager.GetElement("EffectHighlightYellowSelect").SetActive(false);
        }

        #endregion

        #region Label

        public bool labelShowing = false;
        private const string upColor = "<color=#00FFFF>";
        private const string upGrayColor = "<color=#009999>";
        private const string normalColor = "<color=#FFFFFF>";
        private const string normalGrayColor = "<color=#999999>";
        private const string downColor = "<color=#FF0000>";
        private const string downGrayColor = "<color=#990000>";
        private const string colorEndLabel = "</color>";
        private const string smallSize = "<size=75%>";
        private const string sizeEndLabel = "</size>";

        private int attack = 0;
        private int defense = 0;
        private float changeTime = 0.6f;
        private int lastAttribute;
        private int lastRace;
        private bool closeupShowing;
        private int setTurn = 0;
        public bool setOverTurn;

        public void RefreshLabel()
        {
            if(model == null)
                return;
            if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Onfield) == 0 || (p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.FaceUp) == 0)
            {
                HideLabel();
                return;
            }

            MDPro3.Duel.YGOSharp.Card origin = MDPro3.Duel.YGOSharp.CardsManager.Get(data.Id);

            if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone) > 0)
            {
                //LinkMarker
                if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Link))
                {
                    manager.GetElement("LinkMarker0").SetActive((data.LinkMarker & (uint)MDPro3.Duel.YGOSharp.CardLinkMarker.TopLeft) > 0);
                    manager.GetElement("LinkMarker1").SetActive((data.LinkMarker & (uint)MDPro3.Duel.YGOSharp.CardLinkMarker.Top) > 0);
                    manager.GetElement("LinkMarker2").SetActive((data.LinkMarker & (uint)MDPro3.Duel.YGOSharp.CardLinkMarker.TopRight) > 0);
                    manager.GetElement("LinkMarker3").SetActive((data.LinkMarker & (uint)MDPro3.Duel.YGOSharp.CardLinkMarker.Left) > 0);
                    manager.GetElement("LinkMarker4").SetActive((data.LinkMarker & (uint)MDPro3.Duel.YGOSharp.CardLinkMarker.Right) > 0);
                    manager.GetElement("LinkMarker5").SetActive((data.LinkMarker & (uint)MDPro3.Duel.YGOSharp.CardLinkMarker.BottomLeft) > 0);
                    manager.GetElement("LinkMarker6").SetActive((data.LinkMarker & (uint)MDPro3.Duel.YGOSharp.CardLinkMarker.Bottom) > 0);
                    manager.GetElement("LinkMarker7").SetActive((data.LinkMarker & (uint)MDPro3.Duel.YGOSharp.CardLinkMarker.BottomRight) > 0);
                }
                else
                    for (int i = 0; i < 8; i++)
                        manager.GetElement("LinkMarker" + i).SetActive(false);

                //Overlay Material
                if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Xyz))
                {
                    manager.GetElement("MonsterMaterialsRoot").SetActive(true);
                    int overlayCounts = Runtime.Program.instance.ocgcore.GCS_GetOverlays(this).Count;
                    manager.GetElement<TextMeshPro>("TextMonsterMaterials").text = overlayCounts.ToString();
                }
                else
                    manager.GetElement("MonsterMaterialsRoot").SetActive(false);
                //Attack/Defence
                manager.GetElement("CardAttackBody").SetActive(true);
                var text = manager.GetElement<TextMeshPro>("TextPowerPoint");
                if (!labelShowing)
                {
                    string atkDef = string.Empty;

                    if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Link))
                    {
                        if (data.Attack > data.rAttack)
                            atkDef = upColor + data.Attack.ToString() + colorEndLabel;
                        else if (data.Attack < data.rAttack)
                            atkDef = downColor + data.Attack.ToString() + colorEndLabel;
                        else
                            atkDef = normalColor + data.Attack.ToString() + colorEndLabel;
                    }
                    else
                    {
                        int rAtk = data.rAttack;
                        int rDef = data.rDefense;
                        if (rAtk < 0) rAtk = 0;
                        if (rDef < 0) rDef = 0;
                        if (data.Attack > rAtk)
                        {
                            if (p.InPosition(MDPro3.Duel.YGOSharp.CardPosition.Attack))
                                atkDef += upColor;
                            else
                                atkDef += upGrayColor + smallSize;
                        }
                        else if (data.Attack < rAtk)
                        {
                            if (p.InPosition(MDPro3.Duel.YGOSharp.CardPosition.Attack))
                                atkDef += downColor;
                            else
                                atkDef += downGrayColor + smallSize;
                        }
                        else
                        {
                            if (p.InPosition(MDPro3.Duel.YGOSharp.CardPosition.Attack))
                                atkDef += normalColor;
                            else
                                atkDef += normalGrayColor + smallSize;
                        }
                        atkDef += data.Attack.ToString() + colorEndLabel;
                        if (!p.InPosition(MDPro3.Duel.YGOSharp.CardPosition.Attack))
                            atkDef += sizeEndLabel;
                        atkDef += "/";
                        if (data.Defense > rDef)
                        {
                            if ((p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.Attack) > 0)
                                atkDef += upGrayColor + smallSize;
                            else
                                atkDef += upColor;
                        }
                        else if (data.Defense < rDef)
                        {
                            if ((p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.Attack) > 0)
                                atkDef += downGrayColor + smallSize;
                            else
                                atkDef += downColor;
                        }
                        else
                        {
                            if ((p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.Attack) > 0)
                                atkDef += normalGrayColor + smallSize;
                            else
                                atkDef += normalColor;
                        }
                        atkDef += data.Defense.ToString() + colorEndLabel;
                    }
                    text.text = atkDef;
                }
                else
                {
                    if (data.Attack > attack)
                    {
                        Runtime.AudioManager.PlaySE("SE_BUFF_ATTACK");
                        var buff = manager.GetElement("EffectBuff");
                        buff.SetActive(false);
                        buff.SetActive(true);
                    }
                    else if (data.Attack < attack)
                    {
                        Runtime.AudioManager.PlaySE("SE_DEBUFF_ATTACK");
                        var buff = manager.GetElement("EffectDebuff");
                        buff.SetActive(false);
                        buff.SetActive(true);
                    }

                    if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Link))
                    {
                        var s1 = string.Empty;
                        if (data.Attack > data.rAttack)
                            s1 = upColor;
                        else if (data.Attack < data.rAttack)
                            s1 = downColor;
                        else
                            s1 = normalColor;

                        if (attack != data.Attack)
                        {
                            var originAttack = attack;
                            DOTween.To(() => originAttack, x =>
                            {
                                text.text = s1 + x + colorEndLabel;
                            }, data.Attack, changeTime).SetTarget(this);
                        }
                        else
                            text.text = s1 + data.Attack.ToString() + colorEndLabel;
                    }
                    else
                    {
                        int rAtk = data.rAttack;
                        int rDef = data.rDefense;
                        if (rAtk < 0) rAtk = 0;
                        if (rDef < 0) rDef = 0;
                        string s1 = string.Empty;

                        if (data.Attack > rAtk)
                        {
                            if (p.InPosition(MDPro3.Duel.YGOSharp.CardPosition.Attack))
                                s1 += upColor;
                            else
                                s1 += upGrayColor + smallSize;
                        }
                        else if (data.Attack < rAtk)
                        {
                            if (p.InPosition(MDPro3.Duel.YGOSharp.CardPosition.Attack))
                                s1 += downColor;
                            else
                                s1 += downGrayColor + smallSize;
                        }
                        else
                        {
                            if (p.InPosition(MDPro3.Duel.YGOSharp.CardPosition.Attack))
                                s1 += normalColor;
                            else
                                s1 += normalGrayColor + smallSize;
                        }
                        string s2 = colorEndLabel;
                        if (!p.InPosition(MDPro3.Duel.YGOSharp.CardPosition.Attack))
                            s2 += sizeEndLabel;
                        s2 += "/";
                        string s3 = string.Empty;
                        if (data.Defense > rDef)
                        {
                            if (p.InPosition(MDPro3.Duel.YGOSharp.CardPosition.Attack))
                                s3 += upGrayColor + smallSize;
                            else
                                s3 += upColor;
                        }
                        else if (data.Defense < rDef)
                        {
                            if (p.InPosition(MDPro3.Duel.YGOSharp.CardPosition.Attack))
                                s3 += downGrayColor + smallSize;
                            else
                                s3 += downColor;
                        }
                        else
                        {
                            if (p.InPosition(MDPro3.Duel.YGOSharp.CardPosition.Attack))
                                s3 += normalGrayColor + smallSize;
                            else
                                s3 += normalColor;
                        }
                        string s4 = colorEndLabel;
                        if (p.InPosition(MDPro3.Duel.YGOSharp.CardPosition.Attack))
                            s4 += sizeEndLabel;
                        var originAttack = attack;
                        var originDefense = defense;
                        if (data.Attack != attack && data.Defense == defense)
                        {
                            DOTween.To(() => originAttack, x =>
                            {
                                text.text = s1 + x + s2 + s3 + data.Defense + s4;
                            }, data.Attack, changeTime).SetTarget(this);
                        }
                        else if (data.Attack == attack && data.Defense != defense)
                        {
                            DOTween.To(() => originDefense, x =>
                            {
                                text.text = s1 + data.Attack + s2 + s3 + x + s4;
                            }, data.Defense, changeTime).SetTarget(this);
                        }
                        else if (data.Attack != attack && data.Defense != defense)
                        {
                            DOTween.To(() => originAttack, x =>
                            {
                                text.text = s1 + x + s2;
                            }, data.Attack, changeTime).SetTarget(this);
                            DOTween.To(() => originDefense, x =>
                            {
                                text.text += s3 + x + s4;
                            }, data.Defense, changeTime).SetTarget(this);
                        }
                        else
                            text.text = s1 + data.Attack + s2 + s3 + data.Defense + s4;
                    }
                }
                attack = data.Attack;
                defense = data.Defense;
                manager.GetElement("CardPendulumBody").SetActive(false);
                //Link Count & Level Count
                if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Link))
                {
                    manager.GetElement("LinkCount").SetActive(true);
                    manager.GetElement<TextMeshPro>("TextLinkCount").text = data.GetLinkCount().ToString();
                    manager.GetElement("CardLevel").SetActive(false);
                }
                else
                {
                    manager.GetElement("LinkCount").SetActive(false);
                    manager.GetElement("CardLevel").SetActive(true);
                    if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Xyz))
                        manager.GetElement<SpriteRenderer>("IconLevel").sprite = Runtime.TextureManager.container.typeRank;
                    else
                        manager.GetElement<SpriteRenderer>("IconLevel").sprite = Runtime.TextureManager.container.typeLevel;
                    string lv = "";
                    if (data.Level > origin.Level)
                        lv += upColor;
                    else if (data.Level < origin.Level)
                        lv += downColor;
                    else
                        lv += normalColor;
                    lv += data.Level.ToString() + "</color>";
                    manager.GetElement<TextMeshPro>("TextLevel").text = lv;
                }
                //Tuner
                if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Tuner))
                {
                    manager.GetElement("TunerIconRoot").SetActive(true);
                    if (origin.HasType(MDPro3.Duel.YGOSharp.CardType.Tuner))
                        manager.GetElement("TunerIconOutline").SetActive(false);
                    else
                        manager.GetElement("TunerIconOutline").SetActive(true);
                }
                else
                    manager.GetElement("TunerIconRoot").SetActive(false);
                //Attribute
                if ((data.Attribute & (uint)MDPro3.Duel.YGOSharp.CardAttribute.Light) > 0)
                {
                    manager.GetElement<SpriteRenderer>("IconAttribute").sprite = Runtime.TextureManager.container.attributeLight;
                    manager.GetElement<MeshRenderer>("Closeup").material.SetColor("_Color", new Color(1, 1, 0, 1) * closeupLineColorIntensity);
                }
                else if ((data.Attribute & (uint)MDPro3.Duel.YGOSharp.CardAttribute.Dark) > 0)
                {
                    manager.GetElement<SpriteRenderer>("IconAttribute").sprite = Runtime.TextureManager.container.attributeDark;
                    manager.GetElement<MeshRenderer>("Closeup").material.SetColor("_Color", new Color(1, 0, 1, 1) * closeupLineColorIntensity);
                }
                else if ((data.Attribute & (uint)MDPro3.Duel.YGOSharp.CardAttribute.Water) > 0)
                {
                    manager.GetElement<SpriteRenderer>("IconAttribute").sprite = Runtime.TextureManager.container.attributeWater;
                    manager.GetElement<MeshRenderer>("Closeup").material.SetColor("_Color", new Color(0, 1, 1, 1) * closeupLineColorIntensity);
                }
                else if ((data.Attribute & (uint)MDPro3.Duel.YGOSharp.CardAttribute.Fire) > 0)
                {
                    manager.GetElement<SpriteRenderer>("IconAttribute").sprite = Runtime.TextureManager.container.attributeFire;
                    manager.GetElement<MeshRenderer>("Closeup").material.SetColor("_Color", new Color(1, 0, 0, 1) * closeupLineColorIntensity);
                }
                else if ((data.Attribute & (uint)MDPro3.Duel.YGOSharp.CardAttribute.Earth) > 0)
                {
                    manager.GetElement<SpriteRenderer>("IconAttribute").sprite = Runtime.TextureManager.container.attributeEarth;
                    manager.GetElement<MeshRenderer>("Closeup").material.SetColor("_Color", new Color(1, 0.2f, 0, 1) * closeupLineColorIntensity);
                }
                else if ((data.Attribute & (uint)MDPro3.Duel.YGOSharp.CardAttribute.Wind) > 0)
                {
                    manager.GetElement<SpriteRenderer>("IconAttribute").sprite = Runtime.TextureManager.container.attributeWind;
                    manager.GetElement<MeshRenderer>("Closeup").material.SetColor("_Color", new Color(0, 1, 0, 1) * closeupLineColorIntensity);
                }
                else if ((data.Attribute & (uint)MDPro3.Duel.YGOSharp.CardAttribute.Divine) > 0)
                {
                    manager.GetElement<SpriteRenderer>("IconAttribute").sprite = Runtime.TextureManager.container.attributeDivine;
                    manager.GetElement<MeshRenderer>("Closeup").material.SetColor("_Color", new Color(1, 1, 0, 1) * closeupLineColorIntensity);
                }
                if (data.Id > 0 && data.Attribute != origin.Attribute)
                {
                    manager.GetElement("IconAttributeChange").SetActive(true);
                    if (lastAttribute != data.Attribute)
                    {
                        lastAttribute = data.Attribute;
                        Runtime.AudioManager.PlaySE("SE_BUFF_CHANGE");
                        manager.GetElement("EffectChange").SetActive(false);
                        manager.GetElement("EffectChange").SetActive(true);
                    }
                }
                else
                    manager.GetElement("IconAttributeChange").SetActive(false);
                manager.GetElement<SpriteRenderer>("MagicType").sprite = Runtime.TextureManager.container.typeNone;
                manager.GetElement("MagicTypeChange").SetActive(false);
                //Race
                if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.Warrior) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.raceWarrior;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.SpellCaster) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.raceSpellCaster;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.Fairy) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.raceFairy;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.Fiend) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.raceFiend;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.Zombie) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.raceZombie;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.Machine) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.raceMachine;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.Aqua) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.raceAqua;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.Pyro) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.racePyro;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.Rock) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.raceRock;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.WindBeast) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.raceWindBeast;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.Plant) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.racePlant;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.Insect) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.raceInsect;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.Thunder) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.raceThunder;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.Dragon) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.raceDragon;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.Beast) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.raceBeast;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.BeastWarrior) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.raceBeastWarrior;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.Dinosaur) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.raceDinosaur;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.Fish) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.raceFish;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.SeaSerpent) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.raceSeaSerpent;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.Reptile) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.raceReptile;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.Psycho) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.racePsycho;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.DivineBeast) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.raceDivineBeast;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.CreatorGod) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.raceCreatorGod;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.Wyrm) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.raceWyrm;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.Cyberse) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.raceCyberse;
                else if ((data.Race & (uint)MDPro3.Duel.YGOSharp.CardRace.Illustion) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.raceIllustion;
                if (data.Id > 0 && data.Race != origin.Race)
                {
                    manager.GetElement("IconTypeChange").SetActive(true);
                    if (lastRace != data.Race)
                    {
                        lastRace = data.Race;
                        Runtime.AudioManager.PlaySE("SE_BUFF_CHANGE");
                        manager.GetElement("EffectChange").SetActive(false);
                        manager.GetElement("EffectChange").SetActive(true);
                    }
                }
                else
                    manager.GetElement("IconTypeChange").SetActive(false);
            }
            else
            {
                for (int i = 0; i < 8; i++)
                    manager.GetElement("LinkMarker" + i).SetActive(false);
                manager.GetElement("MonsterMaterialsRoot").SetActive(false);
                manager.GetElement("CardAttackBody").SetActive(false);
                int p1 = 0;
                int p2 = 4;
                if (MasterRule <= 3)
                {
                    p1 = 6;
                    p2 = 7;
                }

                //Pendulum Scale
                if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.PendulumZone) > 0 ||
                    (data.HasType(MDPro3.Duel.YGOSharp.CardType.Pendulum)
                    && (p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.SpellZone) > 0
                    && !data.HasType(MDPro3.Duel.YGOSharp.CardType.Equip)
                    && !data.HasType(MDPro3.Duel.YGOSharp.CardType.Continuous)
                    && !data.HasType(MDPro3.Duel.YGOSharp.CardType.Trap))
                    && (p.sequence == p1 || p.sequence == p2))
                {
                    manager.GetElement("CardPendulumBody").SetActive(true);
                    string pendulum = "";
                    if (p.sequence == p1)
                    {
                        manager.GetElement("PendulumLeft").SetActive(true);
                        manager.GetElement("PendulumRight").SetActive(false);
                        if (data.LScale > origin.LScale)
                            pendulum += upColor;
                        else if (data.LScale < origin.LScale)
                            pendulum += downColor;
                        else
                            pendulum += normalColor;
                        pendulum += data.LScale.ToString() + "</color>";
                        manager.GetElement<TextMeshPro>("TextPendulumLeft").text = pendulum;
                    }
                    else
                    {
                        manager.GetElement("PendulumLeft").SetActive(false);
                        manager.GetElement("PendulumRight").SetActive(true);
                        if (data.RScale > origin.RScale)
                            pendulum += upColor;
                        else if (data.RScale < origin.RScale)
                            pendulum += downColor;
                        else
                            pendulum += normalColor;
                        pendulum += data.RScale.ToString() + "</color>";
                        manager.GetElement<TextMeshPro>("TextPendulumRight").text = pendulum;
                    }
                }
                else
                    manager.GetElement("CardPendulumBody").SetActive(false);
                manager.GetElement("LinkCount").SetActive(false);
                manager.GetElement("CardLevel").SetActive(false);
                manager.GetElement("TunerIconRoot").SetActive(false);
                //Attribute
                if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Spell))
                    manager.GetElement<SpriteRenderer>("IconAttribute").sprite = Runtime.TextureManager.container.attributeSpell;
                else
                    manager.GetElement<SpriteRenderer>("IconAttribute").sprite = Runtime.TextureManager.container.attributeTrap;
                manager.GetElement("IconAttributeChange").SetActive(false);

                //Magic Trap Type
                if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Counter))
                    manager.GetElement<SpriteRenderer>("MagicType").sprite = Runtime.TextureManager.container.typeCounter;
                else if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Field))
                    manager.GetElement<SpriteRenderer>("MagicType").sprite = Runtime.TextureManager.container.typeField;
                else if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Equip))
                    manager.GetElement<SpriteRenderer>("MagicType").sprite = Runtime.TextureManager.container.typeEquip;
                else if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Continuous))
                    manager.GetElement<SpriteRenderer>("MagicType").sprite = Runtime.TextureManager.container.typeContinuous;
                else if (data.HasType(MDPro3.Duel.YGOSharp.CardType.QuickPlay))
                    manager.GetElement<SpriteRenderer>("MagicType").sprite = Runtime.TextureManager.container.typeQuickPlay;
                else if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Ritual) && !origin.HasType(MDPro3.Duel.YGOSharp.CardType.Monster))
                    manager.GetElement<SpriteRenderer>("MagicType").sprite = Runtime.TextureManager.container.typeRitual;
                else
                    manager.GetElement<SpriteRenderer>("MagicType").sprite = Runtime.TextureManager.container.typeNone;
                manager.GetElement("MagicTypeChange").SetActive(false);
                manager.GetElement<SpriteRenderer>("IconType").sprite = Runtime.TextureManager.container.typeNone;
                manager.GetElement("IconTypeChange").SetActive(false);
            }

            if (cardCounters.Count > 0)
            {
                int counter = 0;
                int count = 0;
                foreach (var cc in cardCounters)
                {
                    counter = cc.Key;
                    count = cc.Value;
                    break;
                }
                manager.GetElement<SpriteRenderer>("IconCounter").sprite = Runtime.TextureManager.GetCardCounterIcon(counter);
                manager.GetElement<TextMeshPro>("TextCounter").text = count.ToString();
            }
            else
            {
                manager.GetElement<SpriteRenderer>("IconCounter").sprite = Runtime.TextureManager.container.typeNone;
                manager.GetElement<TextMeshPro>("TextCounter").text = string.Empty;
            }

            manager.GetElement<Transform>("DefaultShow").localEulerAngles = new Vector3(0f, 0f, 0f);
            manager.GetElement<Transform>("DefaultHide").localEulerAngles = new Vector3(0f, 0f, 0f);
            manager.GetElement<Transform>("CloseupOffset").localEulerAngles = new Vector3(0f, 0f, 0f);

            if (p.controller == 0)
            {
                manager.GetElement<Transform>("MonsterMaterialsRoot").localEulerAngles = new Vector3(0f, 0f, 0f);
                manager.GetElement<Transform>("CardAttackBody").localEulerAngles = new Vector3(0f, 0f, 0f);
                manager.GetElement<Transform>("CardPendulumBody").localEulerAngles = new Vector3(0f, 0f, 0f);
                manager.GetElement<Transform>("LinkCount").localEulerAngles = new Vector3(0f, 0f, 0f);
                manager.GetElement<Transform>("CardLevel").localEulerAngles = new Vector3(0f, 0f, 0f);
                manager.GetElement<Transform>("TunerIconRoot").localEulerAngles = new Vector3(0f, 0f, 0f);
                manager.GetElement<Transform>("CardAttribute").localEulerAngles = new Vector3(0f, 0f, 0f);
                manager.GetElement<Transform>("MagicTypeBase").localEulerAngles = new Vector3(0f, 0f, 0f);
                manager.GetElement<Transform>("CardType").localEulerAngles = new Vector3(0f, 0f, 0f);
                manager.GetElement<Transform>("CardCounter").localEulerAngles = new Vector3(0f, 0f, 0f);
                manager.GetElement<Transform>("StatusIcon").localEulerAngles = new Vector3(0f, 0f, 0f);
                manager.GetElement<Transform>("LinkMarkerRoot").localEulerAngles = new Vector3(0f, 0f, 0f);
            }
            else
            {
                if (CloseupConfig() && (p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone) > 0)
                {
                    manager.GetElement<Transform>("DefaultShow").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("DefaultHide").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("CloseupOffset").localEulerAngles = new Vector3(0f, 180f, 0f);

                    manager.GetElement<Transform>("MonsterMaterialsRoot").localEulerAngles = new Vector3(0f, 0f, 0f);
                    manager.GetElement<Transform>("CardAttackBody").localEulerAngles = new Vector3(0f, 0f, 0f);
                    manager.GetElement<Transform>("CardPendulumBody").localEulerAngles = new Vector3(0f, 0f, 0f);
                    manager.GetElement<Transform>("LinkCount").localEulerAngles = new Vector3(0f, 0f, 0f);
                    manager.GetElement<Transform>("CardLevel").localEulerAngles = new Vector3(0f, 0f, 0f);
                    manager.GetElement<Transform>("TunerIconRoot").localEulerAngles = new Vector3(0f, 0f, 0f);
                    manager.GetElement<Transform>("CardAttribute").localEulerAngles = new Vector3(0f, 0f, 0f);
                    manager.GetElement<Transform>("MagicTypeBase").localEulerAngles = new Vector3(0f, 0f, 0f);
                    manager.GetElement<Transform>("CardType").localEulerAngles = new Vector3(0f, 0f, 0f);
                    manager.GetElement<Transform>("CardCounter").localEulerAngles = new Vector3(0f, 0f, 0f);
                    manager.GetElement<Transform>("StatusIcon").localEulerAngles = new Vector3(0f, 0f, 0f);
                    manager.GetElement<Transform>("LinkMarkerRoot").localEulerAngles = new Vector3(0f, 180f, 0f);
                }
                else
                {
                    manager.GetElement<Transform>("MonsterMaterialsRoot").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("CardAttackBody").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("CardPendulumBody").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("LinkCount").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("CardLevel").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("TunerIconRoot").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("CardAttribute").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("MagicTypeBase").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("CardType").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("CardCounter").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("StatusIcon").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("LinkMarkerRoot").localEulerAngles = new Vector3(0f, 0f, 0f);
                }
            }

            ShowLabel();
        }

        public void ShowLabel()
        {
            labelShowing = true;
            Transform labelRoot = manager.GetElement<Transform>("StatusLabelRoot");
            labelRoot.gameObject.SetActive(true);
            labelRoot.DOScale(1, 0.2f).SetLink(labelRoot.gameObject, LinkBehaviour.KillOnDestroy).SetEase(Ease.InCubic);
            if (NeedShowCloseup())
            {
                var renderer = manager.GetElement<MeshRenderer>("Closeup");
                if(renderer.material.mainTexture == null)
                    closeupShowing = false;
                if (!closeupShowing)
                {
                    closeupShowing = true;
                    _ = Runtime.Program.instance.texture_.LoadCloseupAsync(data.Id, renderer);
                }
            }
            else
            {
                closeupShowing = false;
                manager.GetElement("Closeup").SetActive(false);
            }

            HideHiddenLabel();
        }

        public void HideLabel()
        {
            labelShowing = false;
            Transform labelRoot = manager.GetElement<Transform>("StatusLabelRoot");
            labelRoot.DOScale(0, 0.2f).SetLink(labelRoot.gameObject, LinkBehaviour.KillOnDestroy).SetEase(Ease.OutCubic).OnComplete(() => labelRoot.gameObject.SetActive(false));
            manager.GetElement("Closeup").SetActive(false);
            closeupShowing = false;
        }

        public void ShowHiddenLabel()
        {
            if (model == null)
                return;
            manager.GetElement("CardAttribute").SetActive(true);
            manager.GetElement("MagicTypeBase").SetActive(true);
            manager.GetElement("CardType").SetActive(true);

            Transform linkMarker = manager.GetElement<Transform>("LinkMarkerRoot");
            linkMarker.DOScale(1.7f, 0.2f).SetLink(linkMarker.gameObject, LinkBehaviour.KillOnDestroy).SetEase(Ease.InOutCubic);
            foreach (var sr in linkMarker.GetComponentsInChildren<SpriteRenderer>(true))
                sr.sortingLayerName = "CardStatus";
        }

        public void HideHiddenLabel()
        {
            if (model == null || !labelShowing)
                return;
            if (!manager.GetElement("IconAttributeChange").activeSelf)
                manager.GetElement("CardAttribute").SetActive(false);
            else
                manager.GetElement("CardAttribute").SetActive(true);
            if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.SpellZone) == 0
                || !manager.GetElement("MagicTypeChange").activeSelf)
                manager.GetElement("MagicTypeBase").SetActive(false);
            else
                manager.GetElement("MagicTypeBase").SetActive(true);
            if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone) == 0
                || !manager.GetElement("IconTypeChange").activeSelf)
                manager.GetElement("CardType").SetActive(false);
            else
                manager.GetElement("CardType").SetActive(true);

            Transform linkMarker = manager.GetElement<Transform>("LinkMarkerRoot");
            linkMarker.DOScale(1f, 0.2f).SetLink(linkMarker.gameObject, LinkBehaviour.KillOnDestroy).SetEase(Ease.InOutCubic);
            foreach (var sr in linkMarker.GetComponentsInChildren<SpriteRenderer>(true))
                sr.sortingLayerName = "Default";
        }

        private void SetDisabled(bool retainDuelState = false)
        {
            if (model == null)
                return;
            var cardFace = manager.GetElement<Transform>("CardModel").GetChild(1).GetComponent<Renderer>();
            if (!retainDuelState)
            {
                if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Onfield) == 0)
                    m_disabled = false;
                if ((p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.FaceDown) > 0)
                    m_disabled = false;
            }

            if (Disabled)
            {
                cardFace.material.SetFloat("_Monochrome", 1);
                manager.GetElement<Renderer>("Closeup").material.SetVector("RGBA", Vector4.zero);
                manager.GetElement<Renderer>("Closeup").material.SetFloat("Mono", 1);
            }
            else
            {
                cardFace.material.SetFloat("_Monochrome", 0);
                manager.GetElement<Renderer>("Closeup").material.SetVector("RGBA", Vector4.one);
                manager.GetElement<Renderer>("Closeup").material.SetFloat("Mono", 0);
            }
        }

        private bool NeedShowCloseup()
        {
            if (model == null)
                return false;
            if(!CloseupConfig())
                return false;
            if((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone) == 0)
                return false;
            if ((p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.FaceDown) > 0)
                return false;
            if(!File.Exists(Runtime.Program.PATH_CLOSEUP + data.Id + Runtime.Program.EXPANSION_PNG))
                return false;
            return true;
        }

        private bool CloseupConfig()
        {
            if (condition == Controllers.OcgCore.Condition.Duel && !MDPro3.Config.GetBool("DuelCloseup", true))
                return false;
            if (condition == Controllers.OcgCore.Condition.Watch && !MDPro3.Config.GetBool("WatchCloseup", true))
                return false;
            if (condition == Controllers.OcgCore.Condition.Replay && !MDPro3.Config.GetBool("ReplayCloseup", true))
                return false;
            return true;
        }

        public bool NeedShowFaceDownCard()
        {
            if(data.Id == 0)
                return false;
            if((p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.FaceUp) > 0)
                return false;
            if((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Onfield) == 0)
                return false;

            return true;
        }

        public bool IsFaceDownOnSpellZone()
        {
            if ((p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.FaceUp) > 0)
                return false;
            if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.SpellZone) == 0)
                return false;

            return true;
        }

        public void ShowFaceDownCardOrNot(bool show)
        {
            if (model == null)
                return;
            if (IsFaceDownOnSpellZone())
                setTurn = turns;
            else
                setTurn = 0;

            //var back = manager.GetElement<Transform>("CardModel").GetChild(0).GetComponent<Renderer>();
            var face = manager.GetElement<Transform>("CardModel").GetChild(1).GetComponent<Renderer>();

            if (condition == Controllers.OcgCore.Condition.Duel && !MDPro3.Config.GetBool("DuelFaceDown", true))
                show = false;
            if (condition == Controllers.OcgCore.Condition.Watch && !MDPro3.Config.GetBool("WatchFaceDown", true))
                show = false;
            if (condition == Controllers.OcgCore.Condition.Replay && !MDPro3.Config.GetBool("ReplayFaceDown", true))
                show = false;

            if (show)
            {
                face.transform.localEulerAngles = new Vector3(180f, 0f, 0f);
                face.transform.localPosition = new Vector3(0f, -0.05f, 0f);
                face.material.SetFloat("_isFade", 1f);
            }
            else
            {
                face.transform.localEulerAngles = new Vector3(180f, 0f, -180f);
                face.transform.localPosition = Vector3.zero;
                face.material.SetFloat("_isFade", 0f);
                manager.GetElement("EffectDisquiet").SetActive(false);
            }
        }

        public void ShowDisquiet()
        {
            if (model == null)
                return;
            if (setTurn == 0)
                return;
            if (setTurn >= turns)
                return;
            if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.SpellZone) == 0)
                return;
            if ((p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.FaceDown) == 0)
                return;

            manager.GetElement("EffectDisquiet").SetActive(true);
            setOverTurn = true;
        }

        #endregion

        #region CardCounter

        Dictionary<int, int> cardCounters = new Dictionary<int, int>();

        public void AddCounter(int counter, int count)
        {
            Runtime.AudioManager.PlaySE("SE_CARD_COUNTER");

            int fullCount = count;

            bool haveCounter = cardCounters.Any(cc => cc.Key == counter);
            if (haveCounter)
            {
                fullCount += cardCounters[counter];
                cardCounters.Remove(counter);
            }
            cardCounters.Add(counter, fullCount);

            var counterName = Presentation.StringHelper.Get("counter", counter);
            for (int i = 0; i < count; i++)
                AddStringTail(counterName);
            RefreshLabel();
        }

        public void RemoveCounter(int counter, int count)
        {
            Runtime.AudioManager.PlaySE("SE_CARD_COUNTER");
            var fullCount = cardCounters[counter] - count;
            cardCounters.Remove(counter);
            if (fullCount > 0)
                cardCounters.Add(counter, fullCount);

            var counterName = Presentation.StringHelper.Get("counter", counter);
            for (int i = 0; i < count; i++)
                RemoveStringTail(counterName);
            RefreshLabel();
        }

        public void ClearCounter()
        {
            cardCounters.Clear();
        }

        public int GetCounterCount(int type)
        {
            cardCounters.TryGetValue(type, out var count);
            return count;
        }

        #endregion

        #region String Tail

        public MDPro3.MultiStringMaster tails = new();

        public void AddStringTail(string tail)
        {
            tails.Add(tail);
        }

        public void RemoveStringTail(string tail, bool all = false)
        {
            tails.Remove(tail, all);
        }

        public void ClearAllTails()
        {
            ClearCounter();
            tails.Clear();
        }

        #endregion

        #region Chain

        public class Chain
        {
            public int i;
            public DuelChainSpot chainSpot;
        }

        public List<Chain> chains = new();

        public void AddChain(int i)
        {
            var obj = Runtime.ABLoader.LoadMasterDuelGameObject("ChainSpot");
            Runtime.Program.instance.ocgcore.AllGameObjects.Add(obj);
            chains.Add(new Chain() { i = i, chainSpot = obj.GetComponent<DuelChainSpot>() });
            chains[^1].chainSpot.Play(i, p, model != null);
        }

        public void ResolveChain(int i)
        {
            foreach (Chain chain in chains)
            {
                if (chain.i == i)
                {
                    if (chain.chainSpot != null)
                        chain.chainSpot.OnChainResolveBegin();
                    break;
                }
            }
        }

        public void RemoveChain(int i)
        {
            foreach (Chain chain in chains)
            {
                if (chain.i == i)
                {
                    if (chain.chainSpot != null)
                    {
                        chain.chainSpot.OnChainResolveEnd();
                        Destroy(chain.chainSpot.gameObject, 1f);
                    }
                    chains.Remove(chain);
                    break;
                }
            }
        }

        public void RemoveAllChain()
        {
            foreach (Chain chain in chains)
                if (chain.chainSpot != null)
                    Destroy(chain.chainSpot.gameObject, 1f);
            chains.Clear();
        }

        #endregion

        #region enum

        public enum Condition
        {
            None,
            Chaining,
            Selected
        }

        private enum CardRuleCondition
        {
            MeUpAtk,
            MeUpDef,
            MeDownAtk,
            MeDownDef,
            OpUpAtk,
            OpUpDef,
            OpDownAtk,
            OpDownDef,
            MeUpDeck,
            MeDownDeck,
            OpUpDeck,
            OpDownDeck,
            MeUpExDeck,
            MeDownExDeck,
            OpUpExDeck,
            OpDownExDeck,
            MeUpGrave,
            MeDownGrave,
            OpUpGrave,
            OpDownGrave,
            MeUpRemoved,
            MeDownRemoved,
            OpUpRemoved,
            OpDownRemoved,
            MeUpHand,
            MeDownHand,
            OpUpHand,
            OpDownHand
        }

        #endregion

    }

}
