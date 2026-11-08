using DG.Tweening;


using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
{
    public class CardList : MonoBehaviour
    {
        [SerializeField] private RectTransform baseRect;
        [SerializeField] private CanvasGroup cg;
        [SerializeField] private Image locationIcon;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private GameObject item;
        [SerializeField] private GameObject IconH;
        [SerializeField] private GameObject IconV;
        [SerializeField] private RectTransform originPos;
        [SerializeField] private RectTransform targetPos;
        [SerializeField] private TextMeshProUGUI textCount;

        private bool showing;
        private bool single = true;
        private List<Duel.GameCard> cards;
        private readonly List<GameObject> cardObjs = new();
        private const float transitionTime = 0.1f;
        private MDPro3.Duel.YGOSharp.CardLocation location;
        private int controller;
        private bool showWithCloseDuelLog = false;
        private bool restoringDuelLog;
        private Sequence transitionTween;
        private int transitionVersion;
        private GameObject focusOrigin;
        private RectTransform stretchedRoot;
        private bool stretchTargetSet;
        private float stretchRevealX;
        private bool UsingMobileLayout => name.StartsWith("GenericCardList_Mobile");
        private Vector2 TemplateSize => UsingMobileLayout ? new Vector2(150f, 210f) : new Vector2(90f, 140f);
        private float SingleLayoutWidth => UsingMobileLayout ? 195f : 130f;
        private float TripleLayoutWidth => 3 * TemplateSize.x + (UsingMobileLayout ? 8f : 32f);
        internal bool OccupiesGuideDock => isActiveAndEnabled && baseRect != null
            && baseRect.gameObject.activeInHierarchy && (showing || (cg != null && cg.alpha > .001f));

        private ServantUI.OcgCoreUI _coreUI;
        private ServantUI.OcgCoreUI CoreUI =>
            _coreUI = _coreUI != null ? _coreUI
            : Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>();

        private void Awake()
        {
            baseRect.transform.localPosition = originPos.localPosition;
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            // Theme only the six authored shell Images. Native card artwork,
            // count/location/target markers and scroll masks
            // remain under the existing CanvasGroup and rule callbacks.
            var shellImages = new List<Image> { baseRect.GetComponent<Image>() };
            foreach (var path in new[] { "Frame", "Scroll View/BG/BG", "Scroll View/BG/HeaderBg",
                "Scroll View/BG/Top", "Scroll View/BG/Line" })
            {
                var shell = baseRect.Find(path);
                if (shell != null && shell.TryGetComponent<Image>(out var image)) shellImages.Add(image);
            }
            var layoutButton = baseRect.Find("LayoutBG/LayoutChangeButton")?.GetComponent<Selectable>();
            Client.FightDuelAuxiliarySkin.Attach(baseRect, shellImages.ToArray(), null, layoutButton);
        }

        private void LateUpdate()
        {
            if (!showing) RestoreVisibleSelection();
        }

        public void Show(List<Duel.GameCard> cards, MDPro3.Duel.YGOSharp.CardLocation location, int controller)
        {
            if(Controllers.OcgCore.cantCheckGrave && location == MDPro3.Duel.YGOSharp.CardLocation.Grave)
            {
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("现在不能查看此处的卡片。"));
                return;
            }

            bool replacing = showing;
            if (!replacing && (transitionTween == null || !transitionTween.IsActive())) stretchTargetSet = false;
            int version = CancelTransition();
            this.cards = cards;
            this.location = location;
            this.controller = controller;
            showing = true;
            cg.blocksRaycasts = true;
            cg.interactable = !replacing;

            if (!replacing)
            {
                var selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
                if (selected != null && !selected.transform.IsChildOf(baseRect)) focusOrigin = selected;
                RefreshList();
                var reveal = DOTween.Sequence().Join(baseRect.DOMove(targetPos.position, transitionTime))
                    .Join(cg.DOFade(1f, transitionTime));
                JoinStretch(reveal, true);
                TrackTransition(reveal);
                if (Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>().DuelLog.showing)
                {
                    Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>().OnLog(true);
                    showWithCloseDuelLog = true;
                }
                textCount.text = cards.Count.ToString();
            }
            else
            {
                if(location != MDPro3.Duel.YGOSharp.CardLocation.Unknown)
                {
                    Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>().ResetAttachedEffectState();
                    showForAttachedEffects = false;
                }
                var replace = DOTween.Sequence().Join(baseRect.DOMove(targetPos.position, transitionTime))
                    .Join(cg.DOFade(0f, transitionTime));
                JoinStretch(replace, true);
                replace.OnComplete(() =>
                {
                    if (version != transitionVersion || !showing || !isActiveAndEnabled) return;
                    RefreshList();
                    cg.blocksRaycasts = true;
                    cg.interactable = true;
                    textCount.text = this.cards.Count.ToString();
                    // Keep the original two-stage replacement and read the
                    // current target after the first phase; only one owned
                    // sequence can be active, and Hide cancels either phase.
                    TrackTransition(DOTween.Sequence().Join(baseRect.DOMove(targetPos.position, transitionTime))
                        .Join(cg.DOFade(1f, transitionTime)));
                });
                TrackTransition(replace);
            }
        }

        public void Hide()
        {
            // Reopening the log synchronously asks this list to hide again.
            // The outer call owns that close transition and its original fade.
            if (restoringDuelLog) return;
            bool wasShowing = showing;
            CancelTransition();
            showing = false;
            cg.blocksRaycasts = false; cg.interactable = false;
            if (showWithCloseDuelLog)
            {
                showWithCloseDuelLog = false;
                restoringDuelLog = true;
                try { Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>().OnLog(); }
                finally { restoringDuelLog = false; }
            }

            RestoreVisibleSelection();
            if (!wasShowing)
            {
                cg.alpha = 0f; baseRect.position = originPos.position;
                ResetStretch(); ClearList(); return;
            }
            if (showForAttachedEffects)
                Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>().ResetAttachedEffectState();

            showForAttachedEffects = false;
            var hide = DOTween.Sequence().Join(baseRect.DOMove(originPos.position, transitionTime))
                .Join(cg.DOFade(0f, transitionTime));
            JoinStretch(hide, false);
            int version = transitionVersion;
            hide.OnComplete(() => { if (!showing && version == transitionVersion) ClearList(); });
            TrackTransition(hide);
        }

        private int CancelTransition()
        {
            transitionVersion++;
            DOTweenLifecycle.Kill(transitionTween); transitionTween = null;
            return transitionVersion;
        }

        private void TrackTransition(Sequence sequence)
        {
            transitionTween = sequence;
            sequence.SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnKill(() => { if (transitionTween == sequence) transitionTween = null; });
        }

        private void JoinStretch(Sequence sequence, bool reveal)
        {
            if (!UsingMobileLayout) return;
            var root = CoreUI.Root;
            if (root != stretchedRoot) stretchTargetSet = false;
            stretchedRoot = root;
            if (reveal && !stretchTargetSet)
            {
                stretchRevealX = -Mathf.Abs(root.offsetMax.x) - 195f;
                stretchTargetSet = true;
            }
            float target = reveal ? stretchRevealX : -SafeAreaAdapter.GetSafeAreaRightOffset();
            // Own the same mobile root-offset transition so a quick reversal
            // also retires its setter instead of leaving an untracked tween.
            sequence.Join(DOTween.To(() => root.offsetMax.x,
                value => root.offsetMax = new Vector2(value, root.offsetMax.y), target, transitionTime));
        }

        private void OnDisable() => ResetVisibility();
        private void OnDestroy() => ResetVisibility();

        private void ResetVisibility()
        {
            CancelTransition(); showing = false;
            showWithCloseDuelLog = false; showForAttachedEffects = false;
            if (cg != null) { cg.alpha = 0; cg.blocksRaycasts = false; cg.interactable = false; }
            if (baseRect != null && originPos != null) baseRect.position = originPos.position;
            ResetStretch();
            RestoreVisibleSelection();
            ClearList();
        }

        private void ResetStretch()
        {
            stretchTargetSet = false;
            if (stretchedRoot != null)
                stretchedRoot.offsetMax = new Vector2(-SafeAreaAdapter.GetSafeAreaRightOffset(), stretchedRoot.offsetMax.y);
        }

        private void RestoreVisibleSelection()
        {
            var events = EventSystem.current;
            var selected = events == null ? null : events.currentSelectedGameObject;
            if (selected == null || !selected.transform.IsChildOf(baseRect)) return;
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
                    var fallback = core.GetUI<ServantUI.OcgCoreUI>()?.ButtonLog;
                    if (VisibleControl(fallback)) target = fallback;
                }
            }
            events.SetSelectedGameObject(target != null ? target.gameObject : null);
        }

        private static bool VisibleControl(Selectable control)
        {
            if (control == null || !control.IsActive() || !control.IsInteractable()) return false;
            var canvas = control.GetComponentInParent<Canvas>();
            return canvas != null && canvas.isActiveAndEnabled;
        }

        private void RefreshList()
        {
            locationIcon.sprite = GetListLocationIcon(location, controller);
            ClearList();
            for (int i = 0; i < cards.Count; i++)
            {
                var go = Instantiate(item);
                go.SetActive(true);
                cardObjs.Add(go);
                go.transform.SetParent(scrollRect.content, false);
                CardListItem mono = go.GetComponent<CardListItem>();
                mono.card = cards[i];
            }
            SetButtonState(true);
            RefreshScrollView(true);
            single = true;
        }

        private void RefreshScrollView(bool single)
        {
            var y = baseRect.sizeDelta.y;
            if (single)
            {
                baseRect.sizeDelta = new Vector2(SingleLayoutWidth, y);
                scrollRect.content.sizeDelta = new Vector2(scrollRect.content.sizeDelta.x, TemplateSize.y * cards.Count);
                for (int i = 0; i < cardObjs.Count; i++)
                    cardObjs[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -TemplateSize.y * (cards.Count - 1 - i));
            }
            else
            {
                baseRect.sizeDelta = new Vector2(TripleLayoutWidth, y);
                scrollRect.content.sizeDelta = new Vector2(scrollRect.content.sizeDelta.x, TemplateSize.y * ((cards.Count / 3) + 1));
                for (int i = 0; i < cardObjs.Count; i++)
                    cardObjs[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(GetCardXPosition(i), GetCardYPosition(i, cards.Count));
            }
        }

        private float GetCardXPosition(int index)
        {
            int sign = (index % 3) switch
            {
                0 => -1,
                1 => 0,
                2 => 1,
                _ => 0,
            };
            return sign * TemplateSize.x;
        }

        private float GetCardYPosition(int index, int count)
        {
            int lines = (count - 1 - index) / 3;
            return -TemplateSize.y * lines;
        }

        private void ClearList()
        {
            if (cardObjs.Count == 0) return;
            var selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            if (selected != null && scrollRect != null && selected.transform.IsChildOf(scrollRect.content)) RestoreVisibleSelection();
            foreach (var obj in cardObjs)
            {
                if (obj == null) continue;
                obj.SetActive(false);
                Destroy(obj);
            }
            cardObjs.Clear();
        }

        private void SetButtonState(bool single)
        {
            IconH.SetActive(!single);
            IconV.SetActive(single);
        }

        public void SwitchLayout()
        {
            SwitchLayout(!single);
        }

        private void SwitchLayout(bool single)
        {
            if (single == this.single)
                return;

            this.single = single;

            SetButtonState(single);
            RefreshScrollView(single);
        }

        public static Sprite GetListLocationIcon(MDPro3.Duel.YGOSharp.CardLocation location, int controller)
        {
            if (location == MDPro3.Duel.YGOSharp.CardLocation.Unknown)
                return Runtime.TextureManager.container.listInfo;

            if (controller == 0)
            {
                if ((location & MDPro3.Duel.YGOSharp.CardLocation.Deck) > 0)
                    return Runtime.TextureManager.container.listMyDeck;
                else if ((location & MDPro3.Duel.YGOSharp.CardLocation.Extra) > 0)
                    return Runtime.TextureManager.container.listMyExtra;
                else if ((location & MDPro3.Duel.YGOSharp.CardLocation.Grave) > 0)
                    return Runtime.TextureManager.container.listMyGrave;
                else if ((location & MDPro3.Duel.YGOSharp.CardLocation.Removed) > 0)
                    return Runtime.TextureManager.container.listMyRemoved;
                else
                    return Runtime.TextureManager.container.listMyXyz;
            }
            else
            {
                if ((location & MDPro3.Duel.YGOSharp.CardLocation.Deck) > 0)
                    return Runtime.TextureManager.container.listOpDeck;
                else if ((location & MDPro3.Duel.YGOSharp.CardLocation.Extra) > 0)
                    return Runtime.TextureManager.container.listOpExtra;
                else if ((location & MDPro3.Duel.YGOSharp.CardLocation.Grave) > 0)
                    return Runtime.TextureManager.container.listOpGrave;
                else if ((location & MDPro3.Duel.YGOSharp.CardLocation.Removed) > 0)
                    return Runtime.TextureManager.container.listOpRemoved;
                else
                    return Runtime.TextureManager.container.listOpXyz;
            }
        }

        #region Attached Effects

        private readonly Dictionary<int, int> attachedEffects = new();
        internal IReadOnlyDictionary<int, int> AuxiliaryEffects => attachedEffects;
        private bool showForAttachedEffects;
        private readonly List<Duel.GameCard> gameCardsForAttachedEffects = new();
        private readonly List<Duel.GameCard> _attachedEffectCards = new();
        private bool attachedEffectsDirty = true;

        public void SwitchForAttachedEffects()
        {
            if (showing && showForAttachedEffects)
                Hide();
            else if (!showForAttachedEffects)
                ShowForAttachedEffects();
        }

        private void ShowForAttachedEffects()
        {
            Show(GetAttachedEffectCards(), MDPro3.Duel.YGOSharp.CardLocation.Unknown, 0);
            showForAttachedEffects = true;
            Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>().SwitchAttachedEffectState();
        }

        private List<Duel.GameCard> GetAttachedEffectCards()
        {
            if (attachedEffectsDirty)
            {
                _attachedEffectCards.Clear();
                foreach (var effect in attachedEffects)
                {
                    Duel.GameCard card = CreateGameCard(effect.Key);
                    _attachedEffectCards.Add(card);
                }
                attachedEffectsDirty = false;
            }
            return _attachedEffectCards;
        }

        private Duel.GameCard CreateGameCard(int code)
        {
            Duel.GameCard existing = gameCardsForAttachedEffects.Find(c => c.GetData().Id == code);
            if (existing != null)
                return existing;
            Duel.GameCard card = gameObject.AddComponent<Duel.GameCard>();
            card.p = new Duel.GPS() { location = (uint)MDPro3.Duel.YGOSharp.CardLocation.Unknown, position = (int)MDPro3.Duel.YGOSharp.CardPosition.FaceUpAttack };
            card.SetCode(code);
            gameCardsForAttachedEffects.Add(card);
            return card;
        }

        public bool AddAttachedEffect(int code, int player)
        {
            attachedEffectsDirty = true;
            var value = 1 << player;

            if (attachedEffects.ContainsKey(code))
            {
                if ((attachedEffects[code] & value) > 0)
                    return false;
                attachedEffects[code] += value;
                return true;
            }
            else
            {
                attachedEffects.Add(code, value);
                SetNum();
                return true;
            }
        }

        public bool RemoveAttachedEffect(int code, int player)
        {
            attachedEffectsDirty = true;
            var value = 1 << player;

            if (!attachedEffects.ContainsKey(code))
                return false;
            if ((attachedEffects[code] & value) == 0)
                return false;
            if(attachedEffects[code] == value)
            {
                attachedEffects.Remove(code);
                SetNum();
            }
            else
                attachedEffects[code] -= value;
            return true;
        }

        private int GetAttachedEffectPlayer(int code)
        {
            if(attachedEffects.ContainsKey(code))
                return attachedEffects[code];
            return 0;
        }

        public Sprite GetAttachedEffectPlayerIcon(int code)
        {
            var player = GetAttachedEffectPlayer(code);
            if (player == 1)
                return Runtime.TextureManager.container.attachedEffect_Blue;
            else if (player == 2)
                return Runtime.TextureManager.container.attachedEffect_Red;
            else if (player == 3)
                return Runtime.TextureManager.container.attachedEffect_BlueRed;
            else
                return Runtime.TextureManager.container.typeNone;
        }

        public void ResetAttachedEffects()
        {
            attachedEffects.Clear();
            _attachedEffectCards.Clear();
            attachedEffectsDirty = true;
            Hide();
            ServantUI.OcgCoreUI ui = Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>();
            ui.ResetAttachedEffectState();
            foreach(Duel.GameCard card in gameCardsForAttachedEffects)
                card.Dispose();
            gameCardsForAttachedEffects.Clear();
            SetNum();
        }

        private void SetNum()
        {
            var num = attachedEffects.Count;
            Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>().SetAttachedEffectNum(num);
        }

        #endregion
    }
}
