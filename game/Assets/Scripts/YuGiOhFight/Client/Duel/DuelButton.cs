using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
{
    public enum ButtonType
    {
        Select,
        Decide,
        Cancel,
        Activate,
        SetPendulum,
        Battle,
        ToAttackPosition,
        ToDefensePosition,
        SpSummon,
        Summon,
        PenSummon,
        SetSpell,
        SetMonster,
    }

    public class DuelButton : MonoBehaviour
    {
        public Duel.GameCard cookieCard;
        public List<int> response = new List<int>();
        // response -1 for bg buttons activate
        // response -2 for bg buttons spsummon
        public string hint;
        public ButtonType type;
        public int id;
        public int buttonsCount;

        //for bg Buttons
        public uint location;
        public uint controller;
        //for select Button
        public uint sequence;

        public TextMeshProUGUI text;
        public GameObject bg;
        static float transitionTime = 0.1f;
        bool showing;
        private static readonly List<DuelButton> liveButtons = new List<DuelButton>();
        private readonly List<LayoutItem> layoutItems = new List<LayoutItem>();
        private readonly Vector3[] corners = new Vector3[4];
        private RectTransform buttonRect;
        private Graphic[] graphics;
        private int layoutFrame = -1;
        private Button control;
        private bool normalInteractable;
        private Tween scaleTween;
        private GameObject focusOrigin;

        private struct LayoutItem
        {
            internal DuelButton Button;
            internal Vector2 Position, Anchor;
            internal Rect Footprint, HitArea;
            internal int Index;
        }

        private void Awake()
        {
            liveButtons.Add(this);
            control = GetComponent<Button>(); normalInteractable = control.interactable;
            control.interactable = false; transform.localScale = Vector3.zero;
        }

        private void OnDisable() => ResetVisibility();
        private void OnDestroy()
        {
            ResetVisibility(); liveButtons.Remove(this);
        }

        private void LateUpdate()
        {
            // Re-project visible groups after card/camera movement, display
            // changes or safe-area changes. One member lays out the whole group.
            if (showing && layoutFrame != Time.frameCount) RefreshPosition();
            else if (!showing) RestoreVisibleSelection();
        }

        private void Start()
        {
            control.onClick.AddListener(OnClick);
            transform.SetParent(Runtime.Program.instance.ui_.duelButton, false);
            text.text = hint;
            Client.FightSceneActionSkin.Attach(this);
            if(hint == string.Empty)
                bg.SetActive(false);
            RefreshPosition();
            if (!showing) transform.localScale = Vector3.zero;
            StartCoroutine(RefreshIcons());
        }

        private IEnumerator RefreshIcons()
        {
            while (Runtime.TextureManager.container == null)
                yield return null;
            var spriteState = new SpriteState();
            switch (type)
            {
                case ButtonType.Select:
                    GetComponent<Image>().sprite = Runtime.TextureManager.container.select[0];
                    spriteState.highlightedSprite = Runtime.TextureManager.container.select[1];
                    spriteState.pressedSprite = Runtime.TextureManager.container.select[2];
                    spriteState.disabledSprite = Runtime.TextureManager.container.select[3];
                    break;
                case ButtonType.Decide:
                    GetComponent<Image>().sprite = Runtime.TextureManager.container.decide[0];
                    spriteState.highlightedSprite = Runtime.TextureManager.container.decide[1];
                    spriteState.pressedSprite = Runtime.TextureManager.container.decide[2];
                    spriteState.disabledSprite = Runtime.TextureManager.container.decide[3];
                    break;
                case ButtonType.Cancel:
                    GetComponent<Image>().sprite = Runtime.TextureManager.container.cancel[0];
                    spriteState.highlightedSprite = Runtime.TextureManager.container.cancel[1];
                    spriteState.pressedSprite = Runtime.TextureManager.container.cancel[2];
                    spriteState.disabledSprite = Runtime.TextureManager.container.cancel[3];
                    break;
                case ButtonType.Activate:
                    GetComponent<Image>().sprite = Runtime.TextureManager.container.activate[0];
                    spriteState.highlightedSprite = Runtime.TextureManager.container.activate[1];
                    spriteState.pressedSprite = Runtime.TextureManager.container.activate[2];
                    spriteState.disabledSprite = Runtime.TextureManager.container.activate[3];
                    break;
                case ButtonType.Battle:
                    GetComponent<Image>().sprite = Runtime.TextureManager.container.battle[0];
                    spriteState.highlightedSprite = Runtime.TextureManager.container.battle[1];
                    spriteState.pressedSprite = Runtime.TextureManager.container.battle[2];
                    spriteState.disabledSprite = Runtime.TextureManager.container.battle[3];
                    break;
                case ButtonType.ToAttackPosition:
                    GetComponent<Image>().sprite = Runtime.TextureManager.container.toAttack[0];
                    spriteState.highlightedSprite = Runtime.TextureManager.container.toAttack[1];
                    spriteState.pressedSprite = Runtime.TextureManager.container.toAttack[2];
                    spriteState.disabledSprite = Runtime.TextureManager.container.toAttack[3];
                    break;
                case ButtonType.ToDefensePosition:
                    GetComponent<Image>().sprite = Runtime.TextureManager.container.toDefense[0];
                    spriteState.highlightedSprite = Runtime.TextureManager.container.toDefense[1];
                    spriteState.pressedSprite = Runtime.TextureManager.container.toDefense[2];
                    spriteState.disabledSprite = Runtime.TextureManager.container.toDefense[3];
                    break;
                case ButtonType.SpSummon:
                    GetComponent<Image>().sprite = Runtime.TextureManager.container.spSummon[0];
                    spriteState.highlightedSprite = Runtime.TextureManager.container.spSummon[1];
                    spriteState.pressedSprite = Runtime.TextureManager.container.spSummon[2];
                    spriteState.disabledSprite = Runtime.TextureManager.container.spSummon[3];
                    break;
                case ButtonType.Summon:
                    GetComponent<Image>().sprite = Runtime.TextureManager.container.summon[0];
                    spriteState.highlightedSprite = Runtime.TextureManager.container.summon[1];
                    spriteState.pressedSprite = Runtime.TextureManager.container.summon[2];
                    spriteState.disabledSprite = Runtime.TextureManager.container.summon[3];
                    break;
                case ButtonType.PenSummon:
                    GetComponent<Image>().sprite = Runtime.TextureManager.container.penSummon[0];
                    spriteState.highlightedSprite = Runtime.TextureManager.container.penSummon[1];
                    spriteState.pressedSprite = Runtime.TextureManager.container.penSummon[2];
                    spriteState.disabledSprite = Runtime.TextureManager.container.penSummon[3];
                    break;
                case ButtonType.SetSpell:
                    GetComponent<Image>().sprite = Runtime.TextureManager.container.setSpell[0];
                    spriteState.highlightedSprite = Runtime.TextureManager.container.setSpell[1];
                    spriteState.pressedSprite = Runtime.TextureManager.container.setSpell[2];
                    spriteState.disabledSprite = Runtime.TextureManager.container.setSpell[3];
                    break;
                case ButtonType.SetMonster:
                    GetComponent<Image>().sprite = Runtime.TextureManager.container.setMonster[0];
                    spriteState.highlightedSprite = Runtime.TextureManager.container.setMonster[1];
                    spriteState.pressedSprite = Runtime.TextureManager.container.setMonster[2];
                    spriteState.disabledSprite = Runtime.TextureManager.container.setMonster[3];
                    break;
                case ButtonType.SetPendulum:
                    GetComponent<Image>().sprite = Runtime.TextureManager.container.setPendulum[0];
                    spriteState.highlightedSprite = Runtime.TextureManager.container.setPendulum[1];
                    spriteState.pressedSprite = Runtime.TextureManager.container.setPendulum[2];
                    spriteState.disabledSprite = Runtime.TextureManager.container.setPendulum[3];
                    break;
            }
            GetComponent<Button>().spriteState = spriteState;
        }

        private void RefreshPosition()
        {
            if (Runtime.Program.instance == null || Runtime.Program.instance.ui_ == null || response.Count == 0) return;
            EnsureLayoutData();
            if (IsFixedAction)
            {
                // Confirmation/cancellation retain their independent original
                // anchors; they never participate in a card or pile group.
                buttonRect.anchoredPosition = OriginalPosition();
                layoutFrame = Time.frameCount;
                return;
            }
            var parent = Runtime.Program.instance.ui_.duelButton;
            if (parent == null) return;
            int count = Mathf.Max(1, buttonsCount);
            layoutItems.Clear();
            for (int index = 0; index < count; index++)
            {
                DuelButton member = null;
                // Newly recreated actions win over an old group whose Destroy
                // is still deferred until the end of this frame.
                for (int candidateIndex = liveButtons.Count - 1; candidateIndex >= 0; candidateIndex--)
                {
                    var candidate = liveButtons[candidateIndex];
                    if (candidate != null && candidate.id == index && SameGroup(candidate)) { member = candidate; break; }
                }
                var sample = member != null ? member : this;
                sample.EnsureLayoutData();
                Vector2 position = member != null ? member.OriginalPosition()
                    : OriginalPosition() + new Vector2((index - id) * NativeSpacing, 0);
                layoutItems.Add(new LayoutItem
                {
                    Button = member, Index = index, Position = position,
                    Anchor = AnchorReference(sample.buttonRect, parent),
                    Footprint = sample.FinalFootprint(), HitArea = sample.RootFootprint()
                });
            }
            float spacing = NativeSpacing;
            Rect bounds = GroupBounds(spacing, count);
            if (TrySafeArea(parent, out var safe))
            {
                if (bounds.width > safe.width && count > 1)
                {
                    // Remove only the spare gap. If a physically tiny viewport
                    // cannot hold the final hit areas, keep them distinct and
                    // centre the group rather than shrinking or reordering it.
                    float minimum = 0;
                    for (int index = 0; index + 1 < layoutItems.Count; index++)
                    {
                        var left = layoutItems[index]; var right = layoutItems[index + 1];
                        float leftBase = left.Position.x + left.Anchor.x - (left.Index - (count - 1) * .5f) * NativeSpacing;
                        float rightBase = right.Position.x + right.Anchor.x - (right.Index - (count - 1) * .5f) * NativeSpacing;
                        minimum = Mathf.Max(minimum, leftBase + left.HitArea.xMax - rightBase - right.HitArea.xMin);
                    }
                    minimum = Mathf.Min(NativeSpacing, minimum);
                    if (GroupBounds(minimum, count).width <= safe.width)
                    {
                        float low = minimum, high = NativeSpacing;
                        for (int step = 0; step < 16; step++)
                        {
                            float middle = (low + high) * .5f;
                            if (GroupBounds(middle, count).width <= safe.width) low = middle;
                            else high = middle;
                        }
                        spacing = low;
                    }
                    else spacing = minimum;
                    bounds = GroupBounds(spacing, count);
                }
                var shift = new Vector2(FitShift(bounds.xMin, bounds.xMax, safe.xMin, safe.xMax),
                    FitShift(bounds.yMin, bounds.yMax, safe.yMin, safe.yMax));
                ApplyGroup(spacing, count, shift);
            }
            else ApplyGroup(spacing, count, Vector2.zero);
        }

        private float NativeSpacing => Client.FightDuelPopupSkin.SceneThemeActive && Client.FightDuelActionComposition.GlyphsAvailable ? 124 : 160;

        private bool IsFixedAction => response.Count > 0 && (response[0] == -4 || response[0] == -5);

        private bool SameGroup(DuelButton other)
            => other.response.Count > 0 && !other.IsFixedAction && other.buttonsCount == buttonsCount
                && (cookieCard != null ? other.cookieCard == cookieCard
                    : other.cookieCard == null && other.location == location
                        && other.controller == controller && other.sequence == sequence);

        private void EnsureLayoutData()
        {
            if (buttonRect == null) buttonRect = GetComponent<RectTransform>();
            if (graphics == null) graphics = GetComponentsInChildren<Graphic>(true);
        }

        private static Vector2 AnchorReference(RectTransform rect, RectTransform parent)
            => new Vector2(parent.rect.xMin + parent.rect.width * Mathf.Lerp(rect.anchorMin.x, rect.anchorMax.x, rect.pivot.x),
                parent.rect.yMin + parent.rect.height * Mathf.Lerp(rect.anchorMin.y, rect.anchorMax.y, rect.pivot.y));

        private Rect RootFootprint()
        {
            buttonRect.GetLocalCorners(corners);
            var result = new Rect();
            for (int index = 0; index < 4; index++)
                Include(ref result, buttonRect.localRotation * corners[index], index == 0);
            return result;
        }

        private Rect FinalFootprint()
        {
            Rect result = RootFootprint();
            foreach (var graphic in graphics)
            {
                if (graphic == null || !graphic.enabled) continue;
                var source = graphic.rectTransform;
                bool active = true;
                for (Transform node = source; node != buttonRect && node != null; node = node.parent)
                    if (!node.gameObject.activeSelf) { active = false; break; }
                if (!active) continue;
                // Build the relative transform without the button's animated
                // scale (which may be zero). Its final show scale is always one.
                Matrix4x4 relative = Matrix4x4.identity;
                for (Transform node = source; node != buttonRect && node != null; node = node.parent)
                    relative = Matrix4x4.TRS(node.localPosition, node.localRotation, node.localScale) * relative;
                source.GetLocalCorners(corners);
                for (int index = 0; index < 4; index++)
                    Include(ref result, buttonRect.localRotation * relative.MultiplyPoint3x4(corners[index]), false);
            }
            return result;
        }

        private static void Include(ref Rect rect, Vector3 point, bool first)
        {
            if (first) rect = new Rect(point.x, point.y, 0, 0);
            else rect = Rect.MinMaxRect(Mathf.Min(rect.xMin, point.x), Mathf.Min(rect.yMin, point.y),
                Mathf.Max(rect.xMax, point.x), Mathf.Max(rect.yMax, point.y));
        }

        private Rect GroupBounds(float spacing, int count)
        {
            var result = new Rect(); bool first = true;
            foreach (var item in layoutItems)
            {
                Vector2 origin = item.Position + item.Anchor + new Vector2((item.Index - (count - 1) * .5f) * (spacing - NativeSpacing), 0);
                Include(ref result, origin + item.Footprint.min, first); first = false;
                Include(ref result, origin + item.Footprint.max, false);
            }
            return result;
        }

        private static float FitShift(float min, float max, float safeMin, float safeMax)
            => max - min > safeMax - safeMin ? (safeMin + safeMax - min - max) * .5f
                : Mathf.Clamp(0, safeMin - min, safeMax - max);

        private static bool TrySafeArea(RectTransform parent, out Rect safe)
        {
            safe = new Rect();
            if (Screen.width <= 0 || Screen.height <= 0) return false;
            Rect pixels = Screen.safeArea;
            pixels = Rect.MinMaxRect(Mathf.Max(0, pixels.xMin), Mathf.Max(0, pixels.yMin),
                Mathf.Min(Screen.width, pixels.xMax), Mathf.Min(Screen.height, pixels.yMax));
            if (pixels.width <= 0 || pixels.height <= 0) return false;
            float margin = Mathf.Min(8, Mathf.Min(pixels.width, pixels.height) * .1f);
            pixels = Rect.MinMaxRect(pixels.xMin + margin, pixels.yMin + margin, pixels.xMax - margin, pixels.yMax - margin);
            var canvas = parent.GetComponentInParent<Canvas>();
            Camera camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            for (int index = 0; index < 4; index++)
            {
                var point = new Vector2(index < 2 ? pixels.xMin : pixels.xMax, index % 2 == 0 ? pixels.yMin : pixels.yMax);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, point, camera, out var local)) return false;
                Include(ref safe, local, index == 0);
            }
            return safe.width > 0 && safe.height > 0;
        }

        private void ApplyGroup(float spacing, int count, Vector2 shift)
        {
            foreach (var item in layoutItems)
                if (item.Button != null)
                {
                    item.Button.buttonRect.anchoredPosition = item.Position
                        + new Vector2((item.Index - (count - 1) * .5f) * (spacing - NativeSpacing), 0) + shift;
                    item.Button.layoutFrame = Time.frameCount;
                }
        }

        private Vector2 OriginalPosition()
        {
            if (response[0] == -4) // 确认
            {
                var middle = Runtime.Program.instance.ui_.GetComponent<RectTransform>().sizeDelta.x / 2;
                return new Vector2(middle + 340, 620);
            }
            else if (response[0] == -5) // 取消
            {
                var middle = Runtime.Program.instance.ui_.GetComponent<RectTransform>().sizeDelta.x / 2;
                return new Vector2(middle - 340, 620);
            }
            else
            {
                Vector2 uiPoint;
                float height = 150f;
                if (cookieCard == null || cookieCard.model == null)
                {
                    Duel.GPS gps = new Duel.GPS
                    {
                        location = location,
                        controller = controller,
                        sequence = sequence
                    };
                    var position = Duel.GameCard.GetCardPosition(gps);
                    uiPoint = Runtime.UIManager.WorldToScreenPoint(Runtime.Program.instance.camera_.cameraMain, position);
                    if ((location & ((uint)MDPro3.Duel.YGOSharp.CardLocation.Deck + (uint)MDPro3.Duel.YGOSharp.CardLocation.Extra)) > 0)
                    {
                        if (controller == 0)
                            height = 230f;
                        else
                            height = -150f;
                    }
                    else if ((location & (uint)MDPro3.Duel.YGOSharp.CardLocation.SpellZone) >0)
                    {
                        if (controller != 0)
                            height = -150f;
                    }
                }
                else
                {
                    uiPoint = Runtime.UIManager.WorldToScreenPoint(Runtime.Program.instance.camera_.cameraMain, cookieCard.model.transform.position);
                    if (cookieCard != null)
                    {
                        if ((cookieCard.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Hand) > 0)
                        {
                            if(cookieCard.p.controller == 0)
                                height = 250f;
                            else
                                height = -200;
                        }
                    }
                }
                return new Vector2(uiPoint.x - (buttonsCount - 1) * NativeSpacing * .5f + id * NativeSpacing, uiPoint.y + height);
            }
        }

        public void Show()
        {
            RefreshPosition();
            if (showing) return;
            var events = EventSystem.current;
            var selected = events == null ? null : events.currentSelectedGameObject;
            if (selected != null && selected.GetComponentInParent<DuelButton>() == null) focusOrigin = selected;
            showing = true;
            control.interactable = normalInteractable;
            AnimateScale(1);
        }

        public void Hide()
        {
            bool wasShowing = showing;
            if (wasShowing) normalInteractable = control.interactable;
            showing = false;
            control.interactable = false;
            RestoreVisibleSelection();
            if (wasShowing) AnimateScale(0);
        }

        private void AnimateScale(float target)
        {
            DOTweenLifecycle.Kill(scaleTween);
            var next = transform.DOScale(target, transitionTime).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            scaleTween = next;
            next.OnKill(() => { if (scaleTween == next) scaleTween = null; });
        }

        private void ResetVisibility()
        {
            if (control != null)
            {
                if (showing) normalInteractable = control.interactable;
                control.interactable = false;
            }
            showing = false;
            DOTweenLifecycle.Kill(scaleTween); scaleTween = null;
            RestoreVisibleSelection(); transform.localScale = Vector3.zero;
        }

        private void RestoreVisibleSelection()
        {
            var events = EventSystem.current;
            var selected = events == null ? null : events.currentSelectedGameObject;
            if (selected == null || selected != gameObject && !selected.transform.IsChildOf(transform)) return;
            var program = Runtime.Program.instance;
            var core = program != null ? program.ocgcore : null;
            Selectable target = null;
            if (core != null && core.showing && program.currentServant == core
                && Runtime.UIManager.InputBlocker == null && !Client.FightDuelMenu.BlocksInput)
            {
                var origin = focusOrigin == null ? null : focusOrigin.GetComponent<Selectable>();
                if (VisibleControl(origin)) target = origin;
                else
                {
                    var ui = core.GetUI<ServantUI.OcgCoreUI>();
                    var timing = ui?.ButtonTiming;
                    var fallback = timing != null && timing.IsActive() ? timing : ui?.ButtonLog;
                    if (VisibleControl(fallback)) target = fallback;
                }
            }
            // Selection is restored without submitting, including when an
            // auxiliary menu later tries to restore a now-hidden action.
            events.SetSelectedGameObject(target != null ? target.gameObject : null);
        }

        private static bool VisibleControl(Selectable candidate)
        {
            if (candidate == null || !candidate.IsActive() || !candidate.IsInteractable()) return false;
            var canvas = candidate.GetComponentInParent<Canvas>();
            return canvas != null && canvas.isActiveAndEnabled;
        }

        private void OnClick()
        {
            if (!showing || !isActiveAndEnabled || control == null || !control.IsInteractable()) return;
            Runtime.AudioManager.PlaySE("SE_DUEL_DECIDE");

            if (response[0] >= 0)
            {
                switch (Controllers.OcgCore.currentMessage)
                {
                    case MDPro3.Duel.YGOSharp.GameMessage.SelectBattleCmd:
                    case MDPro3.Duel.YGOSharp.GameMessage.SelectIdleCmd:
                        if (response.Count == 1 || type != ButtonType.Activate)
                        {
                            Runtime.Network.BinaryMaster p = new Runtime.Network.BinaryMaster();
                            p.writer.Write(response[0]);
                            Runtime.Program.instance.ocgcore.SendReturn(p.Get());
                        }
                        else
                        {
                            var selections = new List<string>() { MDPro3.InterString.Get("效果选择") };
                            var responses = new List<int> { };
                            for (var i = 0; i < cookieCard.effects.Count; i++)
                            {
                                var desc = cookieCard.effects[i].desc;
                                if (desc.Length <= 2)
                                    desc = MDPro3.InterString.Get("发动效果");
                                selections.Add(desc);
                                responses.Add(cookieCard.effects[i].ptr);
                            }
                            selections.Add(MDPro3.InterString.Get("放弃"));
                            responses.Add(-233);
                            Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>().ShowPopupSelection(selections, responses);
                        }
                        break;
                }
            }
            else if (response[0] == -1 || response[0] == -2)
            {
                var responseCards = new List<Duel.GameCard>();
                foreach (Duel.GameCard card in Controllers.OcgCore.cards)
                    if (card.p.controller == controller)
                        if ((card.p.location & location) > 0)
                            foreach (Duel.GameCard.DuelButtonInfo btn in card.buttons)
                                if (btn.type == type)
                                {
                                    responseCards.Add(card);
                                    break;
                                }
                if (type == ButtonType.Activate)
                    Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>().ShowPopupSelectCard(MDPro3.InterString.Get("选择效果发动。"), responseCards, 1, 1, true, false);
                else
                    Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>().ShowPopupSelectCard(MDPro3.InterString.Get("选择怪兽特殊召唤。"), responseCards, 1, 1, true, false);
            }
            else if (response[0] == -3)
            {
                foreach (PlaceSelector place in Runtime.Program.instance.ocgcore.Places)
                {
                    if (place.p.controller == controller)
                        if (place.p.location == location)
                            if (place.p.sequence == sequence)
                                place.SelectCardInThisZone();
                }
            }
            else if (response[0] == -4)
            {
                Runtime.Program.instance.ocgcore.FieldSelectedSend();
            }
            else if (response[0] == -5)
            {
                Runtime.Program.instance.ocgcore.FieldSelectedCancel();
            }
        }
    }
}
