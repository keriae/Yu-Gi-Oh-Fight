using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace YuGiOhFight.Client.UI
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
        public Duel.GameCard card;

        [SerializeField] private CardList parent;
        private bool started;
        private int faceVersion;
        private Color faceColor;
        private CancellationTokenSource faceCancellation;
        private Material ownedFaceMaterial;
        private Texture loadedFaceTexture;
        private int loadedFaceCode;
        private Client.FightCardBack facePlaceholder;

        private void Awake() => faceColor = face.color;

        private void Start()
        {
            started = true;
            BeginFace();
            button.onClick.AddListener(OnClick);

            if (card.p.location != (uint)MDPro3.Duel.YGOSharp.CardLocation.Unknown)
            {
                attachedEffectPlayerIcon.gameObject.SetActive(false);
                attachedEffectTypeIcon.gameObject.SetActive(false);
                cardBack.SetActive((card.p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.FaceUp) == 0);

                if (card.GetData().Id != 0)
                {
                    if (card.GetData().HasType(MDPro3.Duel.YGOSharp.CardType.Monster))
                    {
                        levelIcon.sprite = Runtime.TextureManager.GetCardLevelIcon(card.GetData());
                        if (card.GetData().HasType(MDPro3.Duel.YGOSharp.CardType.Link))
                            textLevel.text = card.GetData().GetLinkCount().ToString();
                        else
                            textLevel.text = card.GetData().Level.ToString();
                    }
                    else
                    {
                        levelIcon.sprite = Runtime.TextureManager.container.typeNone;
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
                        if (Controllers.OcgCore.cardsBeTarget.Contains(card))
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
                levelIcon.sprite = Runtime.TextureManager.container.typeNone;
                textLevel.text = string.Empty;
                chain.SetActive(false);
                target.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (started) BeginFace();
        }

        private void BeginFace()
        {
            int version = RetireFace();
            var code = card.GetData().Id;
            if (code != 0)
            {
                ShowFacePlaceholder(true);
                faceCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
                _ = RefreshFace(code, version, faceCancellation.Token);
            }
            else
            {
                face.material = Protector();
                face.color = faceColor;
            }
        }

        private Material Protector()
            => Controllers.OcgCore.condition switch
            {
                Controllers.OcgCore.Condition.Duel => card.p.controller == 0 ? Controllers.Appearance.duelProtector0 : Controllers.Appearance.duelProtector1,
                Controllers.OcgCore.Condition.Watch => card.p.controller == 0 ? Controllers.Appearance.watchProtector0 : Controllers.Appearance.watchProtector1,
                Controllers.OcgCore.Condition.Replay => card.p.controller == 0 ? Controllers.Appearance.replayProtector0 : Controllers.Appearance.replayProtector1,
                _ => null
            };

        private async UniTask RefreshFace(int code, int version, CancellationToken token)
        {
            Texture texture = null;
            Material material = null;
            try
            {
                texture = await Presentation.CardImageLoader.LoadCardAsync(code, false, token);
                if (!CurrentFace(version) || token.IsCancellationRequested) return;
                if (texture == null || texture == Runtime.TextureManager.container.unknownCard.texture)
                { ShowFacePlaceholder(false); return; }
                material = MaterialLoader.GetCardMaterial(code);
                material.mainTexture = texture;
                ownedFaceMaterial = material; material = null;
                loadedFaceTexture = texture; loadedFaceCode = code; texture = null;
                face.texture = loadedFaceTexture; face.material = ownedFaceMaterial; face.color = faceColor;
                if (facePlaceholder != null) facePlaceholder.gameObject.SetActive(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception error)
            {
                if (CurrentFace(version)) ShowFacePlaceholder(false);
                Debug.LogWarning("Card list image: " + error.Message);
            }
            finally
            {
                if (material != null) Destroy(material);
                if (texture != null) Presentation.CardImageLoader.ReleaseCard(code, !(texture is RenderTexture));
            }
        }

        private bool CurrentFace(int version)
            => this != null && isActiveAndEnabled && version == faceVersion;

        private int RetireFace()
        {
            faceVersion++;
            faceCancellation?.Cancel(); faceCancellation?.Dispose(); faceCancellation = null;
            if (face != null) { face.material = null; face.texture = null; face.color = Color.clear; }
            if (ownedFaceMaterial != null) Destroy(ownedFaceMaterial);
            ownedFaceMaterial = null;
            if (loadedFaceTexture != null)
                Presentation.CardImageLoader.ReleaseCard(loadedFaceCode, !(loadedFaceTexture is RenderTexture));
            loadedFaceTexture = null;
            if (facePlaceholder != null) facePlaceholder.gameObject.SetActive(false);
            return faceVersion;
        }

        private void ShowFacePlaceholder(bool loading)
        {
            if (facePlaceholder == null)
            {
                var placeholder = Client.FightUI.Node(face.transform, "Fight list card loading", 0, 0, 1, 1);
                Client.FightUI.Stretch(placeholder);
                placeholder.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                facePlaceholder = placeholder.gameObject.AddComponent<Client.FightCardBack>();
                facePlaceholder.raycastTarget = false; facePlaceholder.color = Client.FightUI.GoldBorder;
            }
            facePlaceholder.Loading = loading; facePlaceholder.Failed = !loading;
            facePlaceholder.gameObject.SetActive(true); facePlaceholder.SetVerticesDirty();
        }

        private void OnDisable() => RetireFace();
        private void OnDestroy() => RetireFace();

        private void OnClick()
        {
            // The description holds its own image lease, so closing or
            // replacing this list cannot retire the texture it is reading.
            Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>().CardDescription.Show(card,
                card.GetData().Id == 0 ? Protector() : null);
        }
    }
}
