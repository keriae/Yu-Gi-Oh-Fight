using System.Collections.Generic;
using DG.Tweening;


using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
{
    public class CardActionMenu : UIWidgetCardBase
    {
        #region Elements

        private const string LABEL_SBN_NEXT = "NextButton";
        private SelectionButton m_ButtonNext;
        protected SelectionButton ButtonNext =>
            m_ButtonNext = m_ButtonNext != null ? m_ButtonNext
            : Manager.GetElement<SelectionButton>(LABEL_SBN_NEXT);

        private const string LABEL_SBN_PREV = "PrevButton";
        private SelectionButton m_ButtonPrev;
        protected SelectionButton ButtonPrev =>
            m_ButtonPrev = m_ButtonPrev != null ? m_ButtonPrev
            : Manager.GetElement<SelectionButton>(LABEL_SBN_PREV);

        #endregion

        [HideInInspector] public bool showing;

        private List<MDPro3.Duel.YGOSharp.Card> cards;
        private List<int> cardCodes;
        private int index;
        private bool shifting;
        public object blockMark;

        protected override void Awake()
        {
            base.Awake();

            CG.alpha = 0f;
            CG.blocksRaycasts = false;

            ImageCard.GetComponent<Button>().onClick.AddListener(ShowCardExpand);
        }

        private void Update()
        {
            if (!showing || shifting || Runtime.UIManager.InputBlocker != null)
                return;
            if (Runtime.UserInput.WasCancelPressed || Runtime.UserInput.MouseRightDown)
                Hide();
            if(Runtime.UserInput.WasRightPressed || Runtime.UserInput.WasRightShoulderPressed)
                OnNext();
            if (Runtime.UserInput.WasLeftPressed || Runtime.UserInput.WasLeftShoulderPressed)
                OnPrev();
            if(Runtime.UserInput.WasLeftTriggerPressed)
                ShowCardExpand();
        }

        public void Show(List<MDPro3.Duel.YGOSharp.Card> cards, int index, object blockMark)
        {
            showing = true;
            shifting = true;
            this.cards = cards;
            cardCodes = null;
            this.index = index;
            CG.alpha = 1f;
            CG.blocksRaycasts = true;

            Runtime.AudioManager.PlaySE("SE_DECK_WINDOW_OPEN");
            Runtime.UIManager.ShowFPSLeft();

            ShowTween(Window);
            ShowTween(ButtonGroup.GetComponent<RectTransform>());

            BG.alpha = 0f;
            BG.DOFade(1f, 0.25f).OnComplete(() => shifting = false);

            SetCardData(index);

            this.blockMark = blockMark;
        }

        public void Show(List<int> cardCodes, int index, object blockMark)
        {
            showing = true;
            shifting = true;
            cards = null;
            this.cardCodes = cardCodes;
            this.index = index;
            CG.alpha = 1f;
            CG.blocksRaycasts = true;

            Runtime.AudioManager.PlaySE("SE_DECK_WINDOW_OPEN");
            Runtime.UIManager.ShowFPSLeft();

            ShowTween(Window);
            ShowTween(ButtonGroup.GetComponent<RectTransform>());

            BG.alpha = 0f;
            BG.DOFade(1f, 0.25f).OnComplete(() => shifting = false);

            SetCardData(index);

            this.blockMark = blockMark;
        }

        protected void SetCardData(int index)
        {
            MDPro3.Duel.YGOSharp.Card data;
            if (cards != null)
                data = cards[index];
            else
                data = MDPro3.Duel.YGOSharp.CardsManager.Get(cardCodes[index]);
            SetCardData(data);
            if(Cursor.lockState == CursorLockMode.Locked)
                SelectDefaultButton();
            CheckButtonState();
        }

        public virtual void SelectDefaultButton()
        {
            EventSystem.current.SetSelectedGameObject(ButtonAddCard.gameObject);
        }

        private void ShowTween(RectTransform rect)
        {
            rect.localScale = new Vector3(0.75f, 0.75f, 1f);
            rect.DOScale(1f, 0.25f).SetEase(Ease.OutQuart);

            var cg = rect.GetComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.DOFade(1f, 0.25f);
        }

        public void Hide()
        {
            Runtime.AudioManager.PlaySE("SE_MENU_CANCEL");
            Runtime.UIManager.ShowFPSRight();

            HideTween(Manager.GetElement<RectTransform>("Window"));
            HideTween(Manager.GetElement<RectTransform>("ButtonGroup"));
            Manager.GetElement<CanvasGroup>("BG").DOFade(0f, 0.2f).OnComplete(() =>
            {
                showing = false;
                CG.alpha = 0f;
                CG.blocksRaycasts = false;
                Runtime.Program.instance.currentServant.JudgeInputBlockerExitMark(blockMark);
                Runtime.Program.instance.currentServant.Select();
            });
        }

        private void HideTween(RectTransform rect)
        {
            rect.DOScale(0.75f, 0.2f).SetEase(Ease.InCubic);

            var cg = rect.GetComponent<CanvasGroup>();
            cg.DOFade(0f, 0.2f);
        }

        public void OnNext()
        {
            if (shifting) return;
            if (cards != null && index == cards.Count - 1)
                return;
            if(cardCodes != null && index == cardCodes.Count - 1)
                return;

            shifting = true;
            Runtime.AudioManager.PlaySE("SE_MENU_SELECT_01");

            var rect = Manager.GetElement<RectTransform>("Window");
            rect.anchoredPosition = new Vector2(0f, -32f);

            var cg = rect.GetComponent<CanvasGroup>();
            cg.alpha = 1f;

            DOTween.Sequence()
                .Append(rect.DOAnchorPos(new Vector2(-480f, -32f), 0.1f).SetEase(Ease.InCubic))
                .Join(cg.DOFade(0f, 0.1f).OnComplete(() =>
                {
                    rect.anchoredPosition = new Vector2(480f, -32f);
                    SetCardData(++index);
                }))
                .Append(rect.DOAnchorPos(new Vector2(0f, -32f), 0.2f).SetEase(Ease.OutQuart))
                .Join(cg.DOFade(1f, 0.2f))
                .OnComplete(() =>
                {
                    shifting = false;
                });
        }

        public void OnPrev()
        {
            if(index == 0 || shifting) return;
            shifting = true;
            Runtime.AudioManager.PlaySE("SE_MENU_SELECT_01");

            var rect = Manager.GetElement<RectTransform>("Window");
            rect.anchoredPosition = new Vector2(0f, -32f);

            var cg = rect.GetComponent<CanvasGroup>();
            cg.alpha = 1f;

            DOTween.Sequence()
                .Append(rect.DOAnchorPos(new Vector2(480f, -32f), 0.1f).SetEase(Ease.InCubic))
                .Join(cg.DOFade(0f, 0.1f).OnComplete(() =>
                {
                    rect.anchoredPosition = new Vector2(-480f, -32f);
                    SetCardData(--index);
                }))
                .Append(rect.DOAnchorPos(new Vector2(0f, -32f), 0.2f).SetEase(Ease.OutQuart))
                .Join(cg.DOFade(1f, 0.2f))
                .OnComplete(() =>
                {
                    shifting = false;
                });
        }

        private void ShowCardExpand()
        {
            Runtime.UIManager.ShowCardExpand(Card);
        }

        private int GetCardsCount()
        {
            if(cards == null)
                return cardCodes.Count;
            else return cards.Count;
        }

        private void CheckButtonState()
        {
            bool nextEnabled = true;
            bool prevEnabled = true;
            var cc = GetCardsCount();
            if (cc < 2)
            {
                ButtonNext.gameObject.SetActive(false);
                ButtonPrev.gameObject.SetActive(false);
                return;
            }
            else
            {
                ButtonNext.gameObject.SetActive(true);
                ButtonPrev.gameObject.SetActive(true);
            }

            if(index == 0)
                prevEnabled = false;
            if (index == cc - 1)
                nextEnabled = false;
            ButtonNext.SetInteractable(nextEnabled);
            ButtonPrev.SetInteractable(prevEnabled);
        }
    }
}
