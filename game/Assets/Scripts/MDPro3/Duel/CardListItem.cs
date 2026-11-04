using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MDPro3.Duel.YGOSharp;
using TMPro;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;
using MDPro3.Utility;
using Cysharp.Threading.Tasks;

namespace MDPro3.UI
{
    public class CardListItem : MonoBehaviour
    {
        public RawImage face;
        public GameObject cardBack;
        public Image levelIcon;
        public TextMeshProUGUI textLevel;
        public GameObject chain;
        public Text chainText;
        public GameObject target;
        public Button button;
        public Image attachedEffectPlayerIcon;
        public Image attachedEffectTypeIcon;
        public GameCard card;

        [SerializeField] private CardList parent;
        private static Color myColor = Color.cyan;
        private static Color opColor = Color.red;

        private void Start()
        {
            _ = RefreshFace();
            button.onClick.AddListener(OnClick);

            if (card.p.location != (uint)CardLocation.Unknown)
            {
                attachedEffectPlayerIcon.gameObject.SetActive(false);
                attachedEffectTypeIcon.gameObject.SetActive(false);
                cardBack.SetActive((card.p.position & (uint)CardPosition.FaceUp) == 0);

                if (card.GetData().Id != 0)
                {
                    if (card.GetData().HasType(CardType.Monster))
                    {
                        levelIcon.sprite = TextureManager.GetCardLevelIcon(card.GetData());
                        if (card.GetData().HasType(CardType.Link))
                            textLevel.text = card.GetData().GetLinkCount().ToString();
                        else
                            textLevel.text = card.GetData().Level.ToString();
                    }
                    else
                    {
                        levelIcon.sprite = TextureManager.container.typeNone;
                        textLevel.text = string.Empty;
                    }
                    if (card.chains.Count > 0)
                    {
                        chain.SetActive(true);
                        chainText.text = card.chains[0].i.ToString();
                        if (card.p.controller == 0)
                            chainText.color = Color.cyan;
                        else
                            chainText.color = Color.red;
                        target.SetActive(false);
                    }
                    else
                    {
                        chain.SetActive(false);
                        if (OcgCore.cardsBeTarget.Contains(card))
                            target.SetActive(true);
                        else
                            target.SetActive(false);
                    }
                }
                else
                {
                    levelIcon.gameObject.SetActive(false);
                    textLevel.text = string.Empty;
                    chain.SetActive(false);
                    cardBack.SetActive(false);
                }
            }
            else
            {
                attachedEffectPlayerIcon.gameObject.SetActive(true);
                attachedEffectTypeIcon.gameObject.SetActive(true);
                attachedEffectPlayerIcon.sprite = parent.GetAttachedEffectPlayerIcon(card.GetData().Id);
                cardBack.SetActive(false);
                levelIcon.sprite = TextureManager.container.typeNone;
                textLevel.text = string.Empty;
                chain.SetActive(false);
                target.SetActive(false);
            }
        }

        private async UniTask RefreshFace()
        {
            face.texture = TextureManager.container.unknownCard.texture;
            var code = card.GetData().Id;
            if (code != 0)
            {
                face.texture = await CardImageLoader.LoadCardAsync(code, false);
                face.material = MaterialLoader.GetCardMaterial(code);
                face.material.mainTexture = face.texture;
            }
            else
            {
                face.texture = null;
                switch (OcgCore.condition)
                {
                    case OcgCore.Condition.Duel:
                        if (card.p.controller == 0)
                            face.material = Appearance.duelProtector0;
                        else
                            face.material = Appearance.duelProtector1;
                        break;
                    case OcgCore.Condition.Watch:
                        if (card.p.controller == 0)
                            face.material = Appearance.watchProtector0;
                        else
                            face.material = Appearance.watchProtector1;
                        break;
                    case OcgCore.Condition.Replay:
                        if (card.p.controller == 0)
                            face.material = Appearance.replayProtector0;
                        else
                            face.material = Appearance.replayProtector1;
                        break;
                }
            }
        }

        private void OnClick()
        {
            Program.instance.ocgcore.GetUI<OcgCoreUI>().CardDescription.Show(card, face.material);
        }
    }
}
