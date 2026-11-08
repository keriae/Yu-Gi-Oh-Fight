using DG.Tweening;


using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
{
    public class PopupDuelSelectCard : PopupDuel
    {
        [Header("Popup Duel SelectCard Reference")]
        public ScrollRect scrollView;
        public RectTransform baseRect;
        public Button btnFinish;
        public string hint;
        public List<Duel.GameCard> cards;
        public int min;
        public int max;
        public bool sendable;
        public bool order;
        public int currentSort;

        public GameObject arrow;

        public int SelectedCount
        {
            get
            {
                return m_selectedCount;
            }
            set
            {
                m_selectedCount = value;
                Refresh();
            }
        }
        private int m_selectedCount;
        private Controllers.OcgCore core;

        public List<PopupDuelSelectCardItem> monos = new();

        public override void InitializeSelections()
        {
            core = Runtime.Program.instance.ocgcore;
            if(Controllers.OcgCore.currentMessage == MDPro3.Duel.YGOSharp.GameMessage.ConfirmCards)
            {
                btnConfirm.gameObject.SetActive(false);
                btnCancel.gameObject.SetActive(false);
                btnFinish.gameObject.SetActive(true);
            }
            else
            {
                btnCancel.GetComponent<ButtonPress>().SetInteractable(exitable);
                btnConfirm.GetComponent<ButtonPress>().SetInteractable(sendable);
            }
            if (cards.Count <= 4)
            {
                baseRect.sizeDelta = new Vector2(650, 420);
                scrollView.GetComponent<RectTransform>().sizeDelta = new Vector2(150 * cards.Count, 240);
            }
            else if (cards.Count >= 5 && cards.Count <= 7)
            {
                baseRect.sizeDelta = new Vector2(150 * cards.Count, 420);
                scrollView.GetComponent<RectTransform>().sizeDelta = new Vector2(150 * cards.Count, 240);
            }
            else
            {
                baseRect.sizeDelta = new Vector2(950, 420);
                scrollView.GetComponent<RectTransform>().sizeDelta = new Vector2(950, 240);
            }
            scrollView.content.sizeDelta = new Vector2(150 * cards.Count, 220);

            var handle = Addressables.LoadAssetAsync<GameObject>("UI/PopupDuelSelectCardItem.prefab");
            handle.Completed += (result) =>
            {
                for (int i = 0; i < cards.Count; i++)
                {
                    var card = Instantiate(result.Result);
                    card.transform.SetParent(scrollView.content, false);
                    card.GetComponent<RectTransform>().anchoredPosition = new Vector2(i * 150, -220);
                    PopupDuelSelectCardItem mono = card.GetComponent<PopupDuelSelectCardItem>();
                    monos.Add(mono);
                    mono.id = i;
                    mono.card = cards[i];
                    mono.cards = cards;
                    mono.manager = this;
                }

                if (Controllers.OcgCore.currentMessage == MDPro3.Duel.YGOSharp.GameMessage.SelectSum)
                {
                    foreach (Duel.GameCard card in Controllers.OcgCore.cardsMustBeSelected)
                        foreach (PopupDuelSelectCardItem mono in monos)
                            if (mono.card == card)
                            {
                                mono.PreSelectThis();
                                break;
                            }
                    foreach (PopupDuelSelectCardItem mono in monos)
                        if (!mono.selected)
                            if (Controllers.OcgCore.CheckSelectableInSum(Controllers.OcgCore.cardsInSelection, mono.card, Controllers.OcgCore.cardsMustBeSelected, max + Controllers.OcgCore.cardsMustBeSelected.Count))
                                mono.SelectableThis();
                            else
                                mono.UnselectableThis();
                    title.text = hint + "-" + Controllers.OcgCore.GetSelectLevelSum(GetSelected())[0].ToString() + Runtime.Program.STRING_SLASH + Controllers.OcgCore.ES_level;
                }
                else if (Controllers.OcgCore.currentMessage == MDPro3.Duel.YGOSharp.GameMessage.SortCard
                || Controllers.OcgCore.currentMessage == MDPro3.Duel.YGOSharp.GameMessage.SortChain)
                {
                    order = true;
                    title.text = hint;
                }
                else if (Controllers.OcgCore.currentMessage == MDPro3.Duel.YGOSharp.GameMessage.SelectCard)
                    title.text = hint + "-0/" + max;
                else
                    title.text = hint;
            };
        }

        public override void Show()
        {
            base.Show();
            Runtime.Program.instance.currentServant.returnAction = OnCancel;
            if (!exitable)
                Runtime.Program.instance.currentServant.returnAction = FieldView;
            if (Controllers.OcgCore.currentMessage == MDPro3.Duel.YGOSharp.GameMessage.ConfirmCards)
                Runtime.Program.instance.currentServant.returnAction = OnFinish;
        }

        private void Refresh()
        {
            if (Controllers.OcgCore.currentMessage == MDPro3.Duel.YGOSharp.GameMessage.SelectSum)
            {
                var sum = Controllers.OcgCore.GetSelectLevelSum(GetSelected());
                if ((Controllers.OcgCore.ES_overFlow && (Controllers.OcgCore.ES_level <= sum[0] || Controllers.OcgCore.ES_level <= sum[1]))
                    ||
                    (!Controllers.OcgCore.ES_overFlow && (Controllers.OcgCore.ES_level == sum[0] || Controllers.OcgCore.ES_level == sum[1]))
                    )
                    btnConfirm.interactable = true;
                else
                    btnConfirm.interactable = false;

                if (!Controllers.OcgCore.ES_overFlow)
                {
                    var selected = new List<Duel.GameCard>();
                    foreach (PopupDuelSelectCardItem mono in monos)
                        if (mono.selected)
                            selected.Add(mono.card);

                    foreach (PopupDuelSelectCardItem mono in monos)
                        if (!mono.selected)
                            if (Controllers.OcgCore.CheckSelectableInSum(Controllers.OcgCore.cardsInSelection, mono.card, selected, max + Controllers.OcgCore.cardsMustBeSelected.Count))
                                mono.SelectableThis();
                            else
                                mono.UnselectableThis();
                }
                var selectedSum = Controllers.OcgCore.GetSelectLevelSum(GetSelected());
                if (!Controllers.OcgCore.ES_overFlow)
                {
                    if (selectedSum[0] == Controllers.OcgCore.ES_level || selectedSum[1] == Controllers.OcgCore.ES_level)
                        btnConfirm.GetComponent<ButtonPress>().SetInteractable(true);
                    else
                        btnConfirm.GetComponent<ButtonPress>().SetInteractable(false);
                }
                else
                {
                    if (selectedSum[0] > Controllers.OcgCore.ES_level || selectedSum[1] > Controllers.OcgCore.ES_level)
                        btnConfirm.GetComponent<ButtonPress>().SetInteractable(true);
                    else
                        btnConfirm.GetComponent<ButtonPress>().SetInteractable(false);
                }
                title.text = hint + "-" + selectedSum[0].ToString() + Runtime.Program.STRING_SLASH + Controllers.OcgCore.ES_level;
            }
            else if(Controllers.OcgCore.currentMessage == MDPro3.Duel.YGOSharp.GameMessage.ConfirmCards)
            {

            }
            else
            {
                if (SelectedCount >= min)
                    btnConfirm.GetComponent<ButtonPress>().SetInteractable(true);
                else
                    btnConfirm.GetComponent<ButtonPress>().SetInteractable(false);

                if (SelectedCount >= max)
                {
                    foreach (PopupDuelSelectCardItem mono in monos)
                        if (!mono.selected)
                            mono.UnselectableThis();
                }
                else
                {
                    foreach (PopupDuelSelectCardItem mono in monos)
                        mono.SelectableThis();
                }
                if (Controllers.OcgCore.currentMessage == MDPro3.Duel.YGOSharp.GameMessage.SelectCard)
                    title.text = hint + "-" + GetSelected().Count + Runtime.Program.STRING_SLASH + max.ToString();
            }
        }

        public void RemoveOrder(int i)
        {
            foreach (PopupDuelSelectCardItem mono in monos)
                mono.RemoveOrder(i);
        }

        private List<Duel.GameCard> GetSelected()
        {
            var list = new List<Duel.GameCard>();
            foreach (PopupDuelSelectCardItem mono in monos)
                if (mono.selected)
                    list.Add(mono.card);
            return list;
        }

        private bool CheckSelectable(Duel.GameCard card, List<Duel.GameCard> addedCards = null)
        {
            bool returnValue = false;

            var sum = Controllers.OcgCore.GetSelectLevelSum(GetSelected());
            if (addedCards != null)
            {
                foreach (Duel.GameCard c in addedCards)
                {
                    sum[0] += c.levelForSelect_1;
                    sum[1] += c.levelForSelect_2;
                }
            }
            if (sum[0] + card.levelForSelect_1 == Controllers.OcgCore.ES_level || sum[1] + card.levelForSelect_2 == Controllers.OcgCore.ES_level)
                return true;
            else
            {
                var newAddedCards = new List<Duel.GameCard>();
                if (addedCards != null)
                    foreach (Duel.GameCard c in addedCards)
                        newAddedCards.Add(c);
                newAddedCards.Add(card);
                foreach (PopupDuelSelectCardItem mono in monos)
                    if (!mono.selected && !newAddedCards.Contains(mono.card))
                    {
                        returnValue = CheckSelectable(mono.card, newAddedCards);
                        if (returnValue)
                            return true;
                    }
            }
            return returnValue;
        }

        public override void OnConfirm()
        {
            base.OnConfirm();
            switch (Controllers.OcgCore.currentMessage)
            {
                case MDPro3.Duel.YGOSharp.GameMessage.SelectEffectYn:
                    Runtime.Network.BinaryMaster binaryMaster = new Runtime.Network.BinaryMaster();
                    binaryMaster.writer.Write(1);
                    SendReturn(binaryMaster.Get());
                    break;
                case MDPro3.Duel.YGOSharp.GameMessage.SelectChain:
                    foreach (PopupDuelSelectCardItem mono in monos)
                        if (mono.selected)
                        {
                            if (mono.card.effects.Count == 1)
                            {
                                binaryMaster = new Runtime.Network.BinaryMaster();
                                binaryMaster.writer.Write(mono.card.effects[0].ptr);
                                SendReturn(binaryMaster.Get());
                            }
                            else
                            {
                                var selections = new List<string>() { MDPro3.InterString.Get("效果选择") };
                                var responses = new List<int> { };
                                for (var i = 0; i < mono.card.effects.Count; i++)
                                {
                                    var desc = mono.card.effects[i].desc;
                                    if (desc.Length <= 2)
                                        desc = MDPro3.InterString.Get("发动效果");
                                    selections.Add(desc);
                                    responses.Add(mono.card.effects[i].ptr);
                                }
                                Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>().ShowPopupSelection(selections, responses);
                            }
                        }
                    break;
                case MDPro3.Duel.YGOSharp.GameMessage.SelectUnselect:
                case MDPro3.Duel.YGOSharp.GameMessage.SelectTribute:
                case MDPro3.Duel.YGOSharp.GameMessage.SelectSum:
                case MDPro3.Duel.YGOSharp.GameMessage.SelectCard:
                    int count = 0;
                    foreach (PopupDuelSelectCardItem mono in monos)
                        if (mono.selected)
                            count++;
                    binaryMaster = new Runtime.Network.BinaryMaster();
                    if (Controllers.OcgCore.currentMessage == MDPro3.Duel.YGOSharp.GameMessage.SelectUnselect && count == 0)
                        binaryMaster.writer.Write(-1);
                    else
                    {
                        binaryMaster.writer.Write((byte)count);
                        foreach (PopupDuelSelectCardItem mono in monos)
                            if (mono.selected)
                                binaryMaster.writer.Write((byte)mono.card.selectPtr);
                    }
                    SendReturn(binaryMaster.Get());
                    break;
                case MDPro3.Duel.YGOSharp.GameMessage.SelectIdleCmd:
                case MDPro3.Duel.YGOSharp.GameMessage.SelectBattleCmd:
                    PopupDuelSelectCardItem selectedCard = null;
                    foreach(PopupDuelSelectCardItem mono in monos)
                        if (mono.selected)
                        {
                            selectedCard = mono;
                            break;
                        }

                    if (selectedCard == null)
                        break;

                    int response = 0;
                    bool needSend = true;
                    if (hint == MDPro3.InterString.Get("选择效果发动。"))
                    {
                        if (selectedCard.card.effects.Count == 1)
                            response = selectedCard.card.effects[0].ptr;
                        else
                        {
                            var selections = new List<string>() { MDPro3.InterString.Get("效果选择") };
                            var responses = new List<int> { };
                            for (var i = 0; i < selectedCard.card.effects.Count; i++)
                            {
                                var desc = selectedCard.card.effects[i].desc;
                                if (desc.Length <= 2)
                                    desc = MDPro3.InterString.Get("发动效果");
                                selections.Add(desc);
                                responses.Add(selectedCard.card.effects[i].ptr);
                            }
                            Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>().ShowPopupSelection(selections, responses);
                            needSend = false;
                        }
                    }
                    else
                    {
                        foreach (Duel.GameCard.DuelButtonInfo btn in selectedCard.card.buttons)
                            if (btn.type == ButtonType.SpSummon)
                                response = btn.response[0];
                    }
                    if (needSend)
                    {
                        binaryMaster = new Runtime.Network.BinaryMaster();
                        binaryMaster.writer.Write(response);
                        SendReturn(binaryMaster.Get());
                    }
                    break;
                case MDPro3.Duel.YGOSharp.GameMessage.AnnounceCard:
                    foreach (PopupDuelSelectCardItem mono in monos)
                        if (mono.selected)
                        {
                            binaryMaster = new Runtime.Network.BinaryMaster();
                            binaryMaster.writer.Write(mono.card.GetData().Id);
                            SendReturn(binaryMaster.Get());
                        }
                    Runtime.Program.instance.ocgcore.ClearAnnounceCards();
                    break;
                case MDPro3.Duel.YGOSharp.GameMessage.SortCard:
                case MDPro3.Duel.YGOSharp.GameMessage.SortChain:
                    var bytes = new byte[monos.Count];
                    for(int i = 0; i < monos.Count; i++)
                        bytes[i] = (byte)(monos[i].GetOrder() - 1);
                    binaryMaster = new Runtime.Network.BinaryMaster();
                    binaryMaster.writer.Write(bytes);
                    SendReturn(binaryMaster.Get());
                    break;
            }
            Runtime.AudioManager.PlaySE("SE_DUEL_DECIDE");
            Hide();
        }

        public override void OnCancel()
        {
            base.OnCancel();
            if (!exitable)
                return;

            Runtime.AudioManager.PlaySE("SE_DUEL_CANCEL");
            switch (Controllers.OcgCore.currentMessage)
            {
                case MDPro3.Duel.YGOSharp.GameMessage.SelectEffectYn:
                    Runtime.Network.BinaryMaster binaryMaster = new Runtime.Network.BinaryMaster();
                    binaryMaster.writer.Write(0);
                    SendReturn(binaryMaster.Get());
                    break;
                case MDPro3.Duel.YGOSharp.GameMessage.AnnounceCard:
                    var ss = new List<string>()
                    {
                        MDPro3.InterString.Get("请输入关键字："),
                        MDPro3.InterString.Get("搜索"),
                        string.Empty,
                        string.Empty
                    };
                    whenQuitDo = () => { Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>().ShowPopupInput(ss, Runtime.Program.instance.ocgcore.OnAnnounceCard, null); };
                    Runtime.Program.instance.ocgcore.ClearAnnounceCards();
                    break;
                case MDPro3.Duel.YGOSharp.GameMessage.SelectIdleCmd:
                case MDPro3.Duel.YGOSharp.GameMessage.SelectBattleCmd:
                    break;
                case MDPro3.Duel.YGOSharp.GameMessage.SelectTribute:
                case MDPro3.Duel.YGOSharp.GameMessage.SelectCard:
                case MDPro3.Duel.YGOSharp.GameMessage.SelectUnselect:
                case MDPro3.Duel.YGOSharp.GameMessage.SelectChain:
                default:
                    binaryMaster = new Runtime.Network.BinaryMaster();
                    binaryMaster.writer.Write(-1);
                    SendReturn(binaryMaster.Get());
                    break;
            }
            Hide();
        }

        public void OnFinish()
        {
            DOTween.To(v => { }, 0, 0, transitionTime).OnComplete(() =>
            {
                Runtime.Program.instance.ocgcore.messageDispatcher.playerResponed = true;
            });
            Runtime.AudioManager.PlaySE("SE_DUEL_DECIDE");
            Hide();
        }

        public override void Hide()
        {
            Destroy(arrow);

            if (shadow != null)
                shadow.DOFade(0f, transitionTime);
            window.DOAnchorPos(new Vector2(0f, -1100f), transitionTime).OnComplete(() =>
            {
                Destroy(gameObject);
                Runtime.Program.instance.ocgcore.returnAction = null;
                whenQuitDo?.Invoke();
            });
            Runtime.Program.instance.ocgcore.currentPopup = null;
        }
    }
}
