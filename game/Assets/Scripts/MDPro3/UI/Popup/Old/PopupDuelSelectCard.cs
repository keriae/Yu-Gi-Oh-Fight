using DG.Tweening;
using MDPro3.Duel;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using YgomSystem.UI;

namespace MDPro3.UI
{
    public class PopupDuelSelectCard : PopupDuel
    {
        [Header("Popup Duel SelectCard Reference")]
        public ScrollRect scrollView;
        public RectTransform baseRect;
        public Button btnFinish;
        public string hint;
        public List<GameCard> cards;
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
        private OcgCore core;

        public List<PopupDuelSelectCardItem> monos = new();

        public override void InitializeSelections()
        {
            core = Program.instance.ocgcore;
            if(OcgCore.currentMessage == GameMessage.ConfirmCards)
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
                    var mono = card.GetComponent<PopupDuelSelectCardItem>();
                    monos.Add(mono);
                    mono.id = i;
                    mono.card = cards[i];
                    mono.cards = cards;
                    mono.manager = this;
                }

                if (OcgCore.currentMessage == GameMessage.SelectSum)
                {
                    foreach (var card in OcgCore.cardsMustBeSelected)
                        foreach (var mono in monos)
                            if (mono.card == card)
                            {
                                mono.PreSelectThis();
                                break;
                            }
                    foreach (var mono in monos)
                        if (!mono.selected)
                            if (OcgCore.CheckSelectableInSum(OcgCore.cardsInSelection, mono.card, OcgCore.cardsMustBeSelected, max + OcgCore.cardsMustBeSelected.Count))
                                mono.SelectableThis();
                            else
                                mono.UnselectableThis();
                    title.text = hint + "-" + OcgCore.GetSelectLevelSum(GetSelected())[0].ToString() + Program.STRING_SLASH + OcgCore.ES_level;
                }
                else if (OcgCore.currentMessage == GameMessage.SortCard
                || OcgCore.currentMessage == GameMessage.SortChain)
                {
                    order = true;
                    title.text = hint;
                }
                else if (OcgCore.currentMessage == GameMessage.SelectCard)
                    title.text = hint + "-0/" + max;
                else
                    title.text = hint;
            };
        }

        public override void Show()
        {
            base.Show();
            Program.instance.currentServant.returnAction = OnCancel;
            if (!exitable)
                Program.instance.currentServant.returnAction = FieldView;
            if (OcgCore.currentMessage == GameMessage.ConfirmCards)
                Program.instance.currentServant.returnAction = OnFinish;
        }

        private void Refresh()
        {
            if (OcgCore.currentMessage == GameMessage.SelectSum)
            {
                var sum = OcgCore.GetSelectLevelSum(GetSelected());
                if ((OcgCore.ES_overFlow && (OcgCore.ES_level <= sum[0] || OcgCore.ES_level <= sum[1]))
                    ||
                    (!OcgCore.ES_overFlow && (OcgCore.ES_level == sum[0] || OcgCore.ES_level == sum[1]))
                    )
                    btnConfirm.interactable = true;
                else
                    btnConfirm.interactable = false;

                if (!OcgCore.ES_overFlow)
                {
                    var selected = new List<GameCard>();
                    foreach (var mono in monos)
                        if (mono.selected)
                            selected.Add(mono.card);

                    foreach (var mono in monos)
                        if (!mono.selected)
                            if (OcgCore.CheckSelectableInSum(OcgCore.cardsInSelection, mono.card, selected, max + OcgCore.cardsMustBeSelected.Count))
                                mono.SelectableThis();
                            else
                                mono.UnselectableThis();
                }
                var selectedSum = OcgCore.GetSelectLevelSum(GetSelected());
                if (!OcgCore.ES_overFlow)
                {
                    if (selectedSum[0] == OcgCore.ES_level || selectedSum[1] == OcgCore.ES_level)
                        btnConfirm.GetComponent<ButtonPress>().SetInteractable(true);
                    else
                        btnConfirm.GetComponent<ButtonPress>().SetInteractable(false);
                }
                else
                {
                    if (selectedSum[0] > OcgCore.ES_level || selectedSum[1] > OcgCore.ES_level)
                        btnConfirm.GetComponent<ButtonPress>().SetInteractable(true);
                    else
                        btnConfirm.GetComponent<ButtonPress>().SetInteractable(false);
                }
                title.text = hint + "-" + selectedSum[0].ToString() + Program.STRING_SLASH + OcgCore.ES_level;
            }
            else if(OcgCore.currentMessage == GameMessage.ConfirmCards)
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
                    foreach (var mono in monos)
                        if (!mono.selected)
                            mono.UnselectableThis();
                }
                else
                {
                    foreach (var mono in monos)
                        mono.SelectableThis();
                }
                if (OcgCore.currentMessage == GameMessage.SelectCard)
                    title.text = hint + "-" + GetSelected().Count + Program.STRING_SLASH + max.ToString();
            }
        }

        public void RemoveOrder(int i)
        {
            foreach (var mono in monos)
                mono.RemoveOrder(i);
        }

        private List<GameCard> GetSelected()
        {
            var list = new List<GameCard>();
            foreach (var mono in monos)
                if (mono.selected)
                    list.Add(mono.card);
            return list;
        }

        private bool CheckSelectable(GameCard card, List<GameCard> addedCards = null)
        {
            bool returnValue = false;

            var sum = OcgCore.GetSelectLevelSum(GetSelected());
            if (addedCards != null)
            {
                foreach (var c in addedCards)
                {
                    sum[0] += c.levelForSelect_1;
                    sum[1] += c.levelForSelect_2;
                }
            }
            if (sum[0] + card.levelForSelect_1 == OcgCore.ES_level || sum[1] + card.levelForSelect_2 == OcgCore.ES_level)
                return true;
            else
            {
                var newAddedCards = new List<GameCard>();
                if (addedCards != null)
                    foreach (var c in addedCards)
                        newAddedCards.Add(c);
                newAddedCards.Add(card);
                foreach (var mono in monos)
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
            switch (OcgCore.currentMessage)
            {
                case GameMessage.SelectEffectYn:
                    var binaryMaster = new BinaryMaster();
                    binaryMaster.writer.Write(1);
                    SendReturn(binaryMaster.Get());
                    break;
                case GameMessage.SelectChain:
                    foreach (var mono in monos)
                        if (mono.selected)
                        {
                            if (mono.card.effects.Count == 1)
                            {
                                binaryMaster = new BinaryMaster();
                                binaryMaster.writer.Write(mono.card.effects[0].ptr);
                                SendReturn(binaryMaster.Get());
                            }
                            else
                            {
                                var selections = new List<string>() { InterString.Get("效果选择") };
                                var responses = new List<int> { };
                                for (var i = 0; i < mono.card.effects.Count; i++)
                                {
                                    var desc = mono.card.effects[i].desc;
                                    if (desc.Length <= 2)
                                        desc = InterString.Get("发动效果");
                                    selections.Add(desc);
                                    responses.Add(mono.card.effects[i].ptr);
                                }
                                Program.instance.ocgcore.GetUI<OcgCoreUI>().ShowPopupSelection(selections, responses);
                            }
                        }
                    break;
                case GameMessage.SelectUnselect:
                case GameMessage.SelectTribute:
                case GameMessage.SelectSum:
                case GameMessage.SelectCard:
                    int count = 0;
                    foreach (var mono in monos)
                        if (mono.selected)
                            count++;
                    binaryMaster = new BinaryMaster();
                    if (OcgCore.currentMessage == GameMessage.SelectUnselect && count == 0)
                        binaryMaster.writer.Write(-1);
                    else
                    {
                        binaryMaster.writer.Write((byte)count);
                        foreach (var mono in monos)
                            if (mono.selected)
                                binaryMaster.writer.Write((byte)mono.card.selectPtr);
                    }
                    SendReturn(binaryMaster.Get());
                    break;
                case GameMessage.SelectIdleCmd:
                case GameMessage.SelectBattleCmd:
                    PopupDuelSelectCardItem selectedCard = null;
                    foreach(var mono in monos)
                        if (mono.selected)
                        {
                            selectedCard = mono;
                            break;
                        }

                    if (selectedCard == null)
                        break;

                    int response = 0;
                    bool needSend = true;
                    if (hint == InterString.Get("选择效果发动。"))
                    {
                        if (selectedCard.card.effects.Count == 1)
                            response = selectedCard.card.effects[0].ptr;
                        else
                        {
                            var selections = new List<string>() { InterString.Get("效果选择") };
                            var responses = new List<int> { };
                            for (var i = 0; i < selectedCard.card.effects.Count; i++)
                            {
                                var desc = selectedCard.card.effects[i].desc;
                                if (desc.Length <= 2)
                                    desc = InterString.Get("发动效果");
                                selections.Add(desc);
                                responses.Add(selectedCard.card.effects[i].ptr);
                            }
                            Program.instance.ocgcore.GetUI<OcgCoreUI>().ShowPopupSelection(selections, responses);
                            needSend = false;
                        }
                    }
                    else
                    {
                        foreach (var btn in selectedCard.card.buttons)
                            if (btn.type == ButtonType.SpSummon)
                                response = btn.response[0];
                    }
                    if (needSend)
                    {
                        binaryMaster = new BinaryMaster();
                        binaryMaster.writer.Write(response);
                        SendReturn(binaryMaster.Get());
                    }
                    break;
                case GameMessage.AnnounceCard:
                    foreach (var mono in monos)
                        if (mono.selected)
                        {
                            binaryMaster = new BinaryMaster();
                            binaryMaster.writer.Write(mono.card.GetData().Id);
                            SendReturn(binaryMaster.Get());
                        }
                    Program.instance.ocgcore.ClearAnnounceCards();
                    break;
                case GameMessage.SortCard:
                case GameMessage.SortChain:
                    var bytes = new byte[monos.Count];
                    for(int i = 0; i < monos.Count; i++)
                        bytes[i] = (byte)(monos[i].GetOrder() - 1);
                    binaryMaster = new BinaryMaster();
                    binaryMaster.writer.Write(bytes);
                    SendReturn(binaryMaster.Get());
                    break;
            }
            AudioManager.PlaySE("SE_DUEL_DECIDE");
            Hide();
        }

        public override void OnCancel()
        {
            base.OnCancel();
            if (!exitable)
                return;

            AudioManager.PlaySE("SE_DUEL_CANCEL");
            switch (OcgCore.currentMessage)
            {
                case GameMessage.SelectEffectYn:
                    var binaryMaster = new BinaryMaster();
                    binaryMaster.writer.Write(0);
                    SendReturn(binaryMaster.Get());
                    break;
                case GameMessage.AnnounceCard:
                    var ss = new List<string>()
                    {
                        InterString.Get("请输入关键字："),
                        InterString.Get("搜索"),
                        string.Empty,
                        string.Empty
                    };
                    whenQuitDo = () => { Program.instance.ocgcore.GetUI<OcgCoreUI>().ShowPopupInput(ss, Program.instance.ocgcore.OnAnnounceCard, null); };
                    Program.instance.ocgcore.ClearAnnounceCards();
                    break;
                case GameMessage.SelectIdleCmd:
                case GameMessage.SelectBattleCmd:
                    break;
                case GameMessage.SelectTribute:
                case GameMessage.SelectCard:
                case GameMessage.SelectUnselect:
                case GameMessage.SelectChain:
                default:
                    binaryMaster = new BinaryMaster();
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
                Program.instance.ocgcore.messageDispatcher.playerResponed = true;
            });
            AudioManager.PlaySE("SE_DUEL_DECIDE");
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
                Program.instance.ocgcore.returnAction = null;
                whenQuitDo?.Invoke();
            });
            Program.instance.ocgcore.currentPopup = null;
        }
    }
}
