using DG.Tweening;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI
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
        private List<GameCard> cards;
        private readonly List<GameObject> cardObjs = new();
        private const float transitionTime = 0.1f;
        private CardLocation location;
        private int controller;
        private bool showWithCloseDuelLog = false;
        private bool UsingMobileLayout => name.StartsWith("GenericCardList_Mobile");
        private Vector2 TemplateSize => UsingMobileLayout ? new Vector2(150f, 210f) : new Vector2(90f, 140f);
        private float SingleLayoutWidth => UsingMobileLayout ? 195f : 130f;
        private float TripleLayoutWidth => 3 * TemplateSize.x + (UsingMobileLayout ? 8f : 32f);

        private OcgCoreUI _coreUI;
        private OcgCoreUI CoreUI =>
            _coreUI = _coreUI != null ? _coreUI
            : Program.instance.ocgcore.GetUI<OcgCoreUI>();

        private void Awake()
        {
            baseRect.transform.localPosition = originPos.localPosition;
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
        }

        public void Show(List<GameCard> cards, CardLocation location, int controller)
        {
            if(OcgCore.cantCheckGrave && location == CardLocation.Grave)
            {
                MessageManager.Cast(InterString.Get("现在不能查看此处的卡片。"));
                return;
            }

            this.cards = cards;
            this.location = location;
            this.controller = controller;

            if (!showing)
            {
                RefreshList();
                baseRect.DOMove(targetPos.position, transitionTime);
                cg.DOFade(1f, transitionTime);
                cg.blocksRaycasts = true;
                if (Program.instance.ocgcore.GetUI<OcgCoreUI>().DuelLog.showing)
                {
                    Program.instance.ocgcore.GetUI<OcgCoreUI>().OnLog(true);
                    showWithCloseDuelLog = true;
                }
                textCount.text = cards.Count.ToString();
            }
            else
            {
                if(location != CardLocation.Unknown)
                {
                    Program.instance.ocgcore.GetUI<OcgCoreUI>().ResetAttachedEffectState();
                    showForAttachedEffects = false;
                }
                baseRect.DOMove(targetPos.position, transitionTime).OnComplete(() =>
                {
                    RefreshList();
                    baseRect.DOMove(targetPos.position, transitionTime);
                    cg.DOFade(1f, transitionTime); // TODO: tween in tween
                    cg.blocksRaycasts = true;
                    textCount.text = cards.Count.ToString();
                });
                cg.DOFade(0f, transitionTime);
            }

            if(UsingMobileLayout && !showing)
                CoreUI.StretchIn(transitionTime);
            showing = true;
        }

        public void Hide()
        {
            if (showWithCloseDuelLog)
            {
                showWithCloseDuelLog = false;
                Program.instance.ocgcore.GetUI<OcgCoreUI>().OnLog();
            }

            if (!showing)
                return;
            showing = false;
            if (showForAttachedEffects)
                Program.instance.ocgcore.GetUI<OcgCoreUI>().ResetAttachedEffectState();

            showForAttachedEffects = false;
            baseRect.DOMove(originPos.position, transitionTime);
            cg.DOFade(0f, transitionTime);
            cg.blocksRaycasts = false;
            if (UsingMobileLayout)
                CoreUI.StretchOut(transitionTime);
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
                var mono = go.GetComponent<CardListItem>();
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
            foreach (var obj in cardObjs)
                Destroy(obj);
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

        public static Sprite GetListLocationIcon(CardLocation location, int controller)
        {
            if (location == CardLocation.Unknown)
                return TextureManager.container.listInfo;

            if (controller == 0)
            {
                if ((location & CardLocation.Deck) > 0)
                    return TextureManager.container.listMyDeck;
                else if ((location & CardLocation.Extra) > 0)
                    return TextureManager.container.listMyExtra;
                else if ((location & CardLocation.Grave) > 0)
                    return TextureManager.container.listMyGrave;
                else if ((location & CardLocation.Removed) > 0)
                    return TextureManager.container.listMyRemoved;
                else
                    return TextureManager.container.listMyXyz;
            }
            else
            {
                if ((location & CardLocation.Deck) > 0)
                    return TextureManager.container.listOpDeck;
                else if ((location & CardLocation.Extra) > 0)
                    return TextureManager.container.listOpExtra;
                else if ((location & CardLocation.Grave) > 0)
                    return TextureManager.container.listOpGrave;
                else if ((location & CardLocation.Removed) > 0)
                    return TextureManager.container.listOpRemoved;
                else
                    return TextureManager.container.listOpXyz;
            }
        }

        #region Attached Effects

        private readonly Dictionary<int, int> attachedEffects = new();
        private bool showForAttachedEffects;
        private readonly List<GameCard> gameCardsForAttachedEffects = new();
        private readonly List<GameCard> _attachedEffectCards = new();
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
            Show(GetAttachedEffectCards(), CardLocation.Unknown, 0);
            showForAttachedEffects = true;
            Program.instance.ocgcore.GetUI<OcgCoreUI>().SwitchAttachedEffectState();
        }

        private List<GameCard> GetAttachedEffectCards()
        {
            if (attachedEffectsDirty)
            {
                _attachedEffectCards.Clear();
                foreach (var effect in attachedEffects)
                {
                    var card = CreateGameCard(effect.Key);
                    _attachedEffectCards.Add(card);
                }
                attachedEffectsDirty = false;
            }
            return _attachedEffectCards;
        }

        private GameCard CreateGameCard(int code)
        {
            var existing = gameCardsForAttachedEffects.Find(c => c.GetData().Id == code);
            if (existing != null)
                return existing;
            var card = gameObject.AddComponent<GameCard>();
            card.p = new GPS() { location = (uint)CardLocation.Unknown, position = (int)CardPosition.FaceUpAttack };
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
                return TextureManager.container.attachedEffect_Blue;
            else if (player == 2)
                return TextureManager.container.attachedEffect_Red;
            else if (player == 3)
                return TextureManager.container.attachedEffect_BlueRed;
            else
                return TextureManager.container.typeNone;
        }

        public void ResetAttachedEffects()
        {
            attachedEffects.Clear();
            _attachedEffectCards.Clear();
            attachedEffectsDirty = true;
            Hide();
            var ui = Program.instance.ocgcore.GetUI<OcgCoreUI>();
            ui.ResetAttachedEffectState();
            foreach(var card in gameCardsForAttachedEffects)
                card.Dispose();
            gameCardsForAttachedEffects.Clear();
            SetNum();
        }

        private void SetNum()
        {
            var num = attachedEffects.Count;
            Program.instance.ocgcore.GetUI<OcgCoreUI>().SetAttachedEffectNum(num);
        }

        #endregion
    }
}
