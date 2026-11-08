using Cysharp.Threading.Tasks;


using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
{
    public class PopupDuelSelectCardItem : MonoBehaviour
    {
        public Image head;
        public Image locationIcon;
        public RawImage cardFace;
        public GameObject cardBack;
        public Button button;
        public Image levelIcon;
        public TextMeshProUGUI textLevel;
        public Image pendulumIcon;
        public TextMeshProUGUI textPendulum;
        public Image tunerIcon;
        public GameObject checkOn;
        public GameObject orderBase;
        public Text orderText;
        public GameObject chain;
        public Text chainText;
        public GameObject target;


        public int id;
        public List<Duel.GameCard> cards;
        public PopupDuelSelectCard manager;

        public Duel.GameCard card;
        private static Color opColor = new(0.9f, 0, 0, 1);

        public bool selected;
        public bool unselectable;
        private static Color unselectableColor = new(0.5f, 0.5f, 0.5f, 1f);
        public bool preselected;
        private static readonly float doubleClickTime = 0.2f;


        private void Start()
        {
            _ = RefreshCard(card.GetData().Id);
            card.forSelect = true;

            if ((card.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Search) > 0)
            {
                GetComponent<Image>().color = Color.black;
                head.color = Color.black;
            }
            else if (card.p.controller != 0)
            {
                GetComponent<Image>().color = opColor;
                head.color = opColor;
            }

            bool showHead = false;
            if (id == 0)
                showHead = true;
            else if (card.p.location != cards[id - 1].p.location
                || card.p.controller != cards[id - 1].p.controller)
                showHead = true;
            if (showHead)
                locationIcon.sprite = Runtime.TextureManager.GetCardLocationIcon(card.p);
            else
                head.gameObject.SetActive(false);

            bool isEnd = false;
            if (id == cards.Count - 1)
                isEnd = true;
            else if (card.p.location != cards[id + 1].p.location
                || card.p.controller != cards[id + 1].p.controller)
                isEnd = true;
            if (isEnd)
                GetComponent<RectTransform>().sizeDelta = new Vector2(145, 180);
            else
                GetComponent<RectTransform>().sizeDelta = new Vector2(180, 180);


            if ((card.p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.FaceUp) > 0)
                cardBack.SetActive(false);

            if (card.chains.Count > 0)
            {
                chain.SetActive(true);
                chainText.text = card.chains[0].i.ToString();
            }
            else
            {
                chain.SetActive(false);

                if (Controllers.OcgCore.cardsBeTarget.Contains(card))
                    target.SetActive(true);
                else
                    target.SetActive(false);
            }

            MDPro3.Duel.YGOSharp.Card origin = MDPro3.Duel.YGOSharp.CardsManager.Get(card.GetData().Id);

            if (origin.HasType(MDPro3.Duel.YGOSharp.CardType.Monster))
            {
                levelIcon.sprite = Runtime.TextureManager.GetCardLevelIcon(card.GetData());
                if (card.GetData().HasType(MDPro3.Duel.YGOSharp.CardType.Link))
                    textLevel.text = card.GetData().GetLinkCount().ToString();
                else
                    textLevel.text = card.GetData().Level.ToString();

                if (origin.HasType(MDPro3.Duel.YGOSharp.CardType.Tuner))
                    tunerIcon.gameObject.SetActive(true);
            }
            else
            {
                levelIcon.sprite = Runtime.TextureManager.container.typeNone;
                textLevel.text = string.Empty;
            }

            if (origin.HasType(MDPro3.Duel.YGOSharp.CardType.Pendulum))
            {
                pendulumIcon.gameObject.SetActive(true);
                textPendulum.text = card.GetData().LScale.ToString();
            }
            else
            {
                pendulumIcon.gameObject.SetActive(false);
                textPendulum.text = string.Empty;
            }

            button.onClick.AddListener(OnClick);
        }

        private async UniTask RefreshCard(int code)
        {
            cardFace.texture = Runtime.TextureManager.container.unknownCard.texture;

            cardFace.material = MaterialLoader.GetCardMaterial(code);
            cardFace.material.mainTexture = await Presentation.CardImageLoader.LoadCardAsync(code, false, destroyCancellationToken);
            cardFace.texture = cardFace.material.mainTexture;
        }

        float clickTime;
        void OnClick()
        {
            Runtime.AudioManager.PlaySE("SE_MENU_SELECT_01");
            if ((card.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Onfield) > 0
                && (card.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Overlay) == 0)
            {
                if (manager.arrow == null)
                {
                    manager.arrow = Runtime.ABLoader.LoadMasterDuelGameObject("fxp_arrow_aim_001");
                    Runtime.Program.instance.ocgcore.AllGameObjects.Add(manager.arrow);
                }
                manager.arrow.transform.position = card.model.transform.position;
            }
            else
            {
                if (manager.arrow != null)
                    manager.arrow.SetActive(false);
            }

            Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>().CardDescription.Show(card, cardFace.material);

            if (selected)
            {
                if (!unselectable)
                {
                    if ((Time.time - clickTime) < doubleClickTime * Time.timeScale)
                    {
                        if (manager.SelectedCount == 1 && manager.min == 1 && manager.max == 1)
                            manager.OnConfirm();
                        else
                            UnselectThis();
                    }
                    else
                        UnselectThis();
                }
            }
            else
            {
                if (!unselectable)
                {
                    SelectThis();
                    clickTime = Time.time;
                }
                else
                {
                    if (manager.max == 1
                        && manager.min == 1
                        && Controllers.OcgCore.currentMessage != MDPro3.Duel.YGOSharp.GameMessage.SelectSum)
                    {
                        foreach (PopupDuelSelectCardItem card in manager.monos)
                            card.UnselectThis();
                        SelectThis();
                        clickTime = Time.time;
                    }
                }
            }

        }

        void SelectThis()
        {
            if (selected) return;
            selected = true;
            manager.SelectedCount++;

            if(Controllers.OcgCore.currentMessage == MDPro3.Duel.YGOSharp.GameMessage.ConfirmCards)
            {
            }
            else if (!manager.order)
                checkOn.SetActive(true);
            else
            {
                orderBase.SetActive(true);
                orderText.text = manager.SelectedCount.ToString();
            }
        }

        public void RemoveOrder(int i)
        {
            if (!selected)
                return;
            int order = int.Parse(orderText.text);
            if (order > i)
                orderText.text = (order - 1).ToString();
        }

        public int GetOrder()
        {
            return int.Parse(orderText.text);
        }

        void UnselectThis()
        {
            if (!selected || unselectable) return;
            selected = false;
            manager.SelectedCount--;

            if (!manager.order)
                checkOn.SetActive(false);
            else
            {
                orderBase.SetActive(false);
                manager.RemoveOrder(GetOrder());
            }
        }
        public void UnselectableThis()
        {
            unselectable = true;
            cardFace.color = unselectableColor;
            cardBack.GetComponent<Image>().color = unselectableColor;
            levelIcon.color = unselectableColor;
            pendulumIcon.color = unselectableColor;
            textLevel.color = unselectableColor;
            textPendulum.color = unselectableColor;
            tunerIcon.color = unselectableColor;
        }
        public void SelectableThis()
        {
            if (preselected)
                return;
            unselectable = false;
            cardFace.color = Color.white;
            cardBack.GetComponent<Image>().color = Color.white;
            levelIcon.color = Color.white;
            pendulumIcon.color = Color.white;
            textLevel.color = Color.white;
            textPendulum.color = Color.white;
            tunerIcon.color = Color.white;
        }

        public void PreSelectThis()
        {
            preselected = true;
            SelectThis();
            UnselectableThis();
        }
    }
}
