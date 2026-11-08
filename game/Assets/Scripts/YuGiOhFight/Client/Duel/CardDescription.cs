using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


using static YuGiOhFight.Client.Controllers.OcgCore;


using Cysharp.Threading.Tasks;


namespace YuGiOhFight.Client.Duel
{
    public class CardDescription : MonoBehaviour
    {
        Presentation.Elements.ElementObjectManager manager;

        public static Color upColor = Color.cyan;
        public static Color downColor = Color.red;
        public static Color equalColor = Color.white;
        public MDPro3.Duel.YGOSharp.Card data;
        public bool showing;
        private int faceVersion;
        private CancellationTokenSource faceCancellation;
        private RawImage face;
        private Color faceColor;
        private Material ownedFaceMaterial;
        private Texture loadedFaceTexture;
        private int loadedFaceCode;
        private UI.Client.FightCardBack facePlaceholder;

        private void Awake()
        {
            manager = GetComponent<Presentation.Elements.ElementObjectManager>();
            var window = manager.GetElement<RectTransform>("Window");
            if (window == null) return;
            // Only the drawer's authored backing bands yield to the scene
            // material. Card art, stat colours, attribute/type icons and the
            // description's own scrolling/reading geometry stay native.
            var shell = new List<Image> { window.GetComponent<Image>() };
            foreach (var path in new[] { "Line", "Player", "NameBase", "TypeBase", "ActivatedBase", "Frame" })
            {
                var node = window.Find(path);
                if (node != null && node.TryGetComponent<Image>(out var image)) shell.Add(image);
            }
            var plus = window.Find("Property/FontSizePlus")?.GetComponent<Selectable>();
            var minus = window.Find("Property/FontSizeMinus")?.GetComponent<Selectable>();
            UI.Client.FightDuelAuxiliarySkin.Attach(window, shell.ToArray(), null, plus, minus);
        }

        private void Start()
        {
            EnsureFace();
            manager.GetElement<Button>("CardButton").onClick.AddListener(ShowDetail);
        }

        private void EnsureFace()
        {
            if (face != null) return;
            face = manager.GetElement<RawImage>("Card"); faceColor = face.color;
        }

        public void Hide()
        {
            showing = false;
            RetireFace();
            var window = manager.GetElement<RectTransform>("Window");
            DOTween.Kill(window, false);
            window.anchoredPosition = new Vector2(-1020 - UI.SafeAreaAdapter.GetSafeAreaLeftOffset(), window.anchoredPosition.y);
        }

        private void ShowDetail()
        {
            Runtime.Program.instance.ocgcore.GetUI<UI.ServantUI.OcgCoreUI>().CardList.Hide();
            Runtime.Program.instance.ui_.chatPanel.Hide();

            Runtime.UIManager.ShowCardInfoDetail(data);
        }

        private async UniTask RefreshFace(int code, int version, CancellationToken token)
        {
            Texture texture = null;
            Material material = null;
            try
            {
                texture = await Presentation.CardImageLoader.LoadCardAsync(code, false, token);
                if (!CurrentFace(version) || token.IsCancellationRequested) return;
                if (texture == null) { ShowFacePlaceholder(false); return; }
                material = UI.MaterialLoader.GetCardMaterial(code);
                material.mainTexture = texture;
                ownedFaceMaterial = material; material = null;
                loadedFaceTexture = texture; loadedFaceCode = code; texture = null;
                face.material = ownedFaceMaterial; face.color = faceColor;
                if (facePlaceholder != null) facePlaceholder.gameObject.SetActive(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception error)
            {
                if (CurrentFace(version)) ShowFacePlaceholder(false);
                Debug.LogWarning("Card description image: " + error.Message);
            }
            finally
            {
                if (material != null) Destroy(material);
                if (texture != null) Presentation.CardImageLoader.ReleaseCard(code, !(texture is RenderTexture));
            }
        }

        private bool CurrentFace(int version)
            => this != null && isActiveAndEnabled && showing && version == faceVersion;

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
                var placeholder = UI.Client.FightUI.Node(face.transform, "Fight card information loading", 0, 0, 1, 1);
                UI.Client.FightUI.Stretch(placeholder);
                placeholder.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                facePlaceholder = placeholder.gameObject.AddComponent<UI.Client.FightCardBack>();
                facePlaceholder.raycastTarget = false; facePlaceholder.color = UI.Client.FightUI.GoldBorder;
            }
            facePlaceholder.Loading = loading; facePlaceholder.Failed = !loading;
            facePlaceholder.gameObject.SetActive(true); facePlaceholder.SetVerticesDirty();
        }

        private void OnDisable() { showing = false; RetireFace(); }
        private void OnDestroy() => RetireFace();

        private static bool IsPendulumEffectContext(MDPro3.Duel.YGOSharp.Card data, GPS p)
        {
            return p != null
                && (((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.PendulumZone) > 0)
                || ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.SpellZone) > 0
                && !data.HasType(MDPro3.Duel.YGOSharp.CardType.Equip)
                && !data.HasType(MDPro3.Duel.YGOSharp.CardType.Continuous)
                && !data.HasType(MDPro3.Duel.YGOSharp.CardType.Trap)));
        }

        private static string WrapInactiveText(string text)
        {
            return string.IsNullOrEmpty(text) ? string.Empty : "<color=#666666>" + text + "</color>";
        }

        public static bool IsRenamedForDisplay(MDPro3.Duel.YGOSharp.Card data)
        {
            if (data == null || data.Alias == 0 || data.Id == data.Alias)
                return false;

            MDPro3.Duel.YGOSharp.Card origin = MDPro3.Duel.YGOSharp.CardsManager.Get(data.Id);
            return origin.Id != 0 && data.Alias != origin.Alias;
        }

        public static string GetDisplayedCardName(MDPro3.Duel.YGOSharp.Card data, bool highlightRenamed = false)
        {
            if (data == null)
                return string.Empty;
            if (!IsRenamedForDisplay(data))
                return data.Name;

            MDPro3.Duel.YGOSharp.Card alias = MDPro3.Duel.YGOSharp.CardsManager.Get(data.Alias);
            if (alias.Id == 0)
                return data.Name;

            return highlightRenamed ? $"<color=#FFF000>{alias.Name}</color>" : alias.Name;
        }

        public static string GetDisplayedCardIdWithBracket(MDPro3.Duel.YGOSharp.Card data)
        {
            if (data == null)
                return string.Empty;
            if (!IsRenamedForDisplay(data))
                return data.GetIdWithBracket();

            MDPro3.Duel.YGOSharp.Card origin = MDPro3.Duel.YGOSharp.CardsManager.Get(data.Id);
            return origin.Id != 0 ? origin.GetIdWithBracket() : data.GetIdWithBracket();
        }

        private static string BuildPendulumDescription(MDPro3.Duel.YGOSharp.Card data, MDPro3.Duel.YGOSharp.Card origin, GPS p)
        {
            var inPendulumContext = IsPendulumEffectContext(data, p);
            var inMonsterContext = p != null && (p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone) > 0;
            if (!inPendulumContext && !inMonsterContext)
                return data.GetDescription(true);

            var setName = data.GetSetNameWithColor();
            var pendulumText = origin.GetPendulumDescription()?.Trim('\r', '\n');
            var monsterText = origin.GetMonsterDescription()?.Trim('\r', '\n');

            if (MDPro3.Utility.Language.GetConfig() == MDPro3.Utility.Language.Korean)
            {
                var monsterHeader = MDPro3.InterString.Get("【怪兽效果】");
                if (!data.HasType(MDPro3.Duel.YGOSharp.CardType.Effect))
                    monsterHeader = MDPro3.InterString.Get("【怪兽描述】");

                monsterHeader = MDPro3.Duel.YGOSharp.Card.NormalizeBracketLabel(monsterHeader);
                var pendulumHeader = MDPro3.Duel.YGOSharp.Card.NormalizeBracketLabel(MDPro3.InterString.Get("【灵摆效果】"));
                var pendulumBlock = pendulumHeader + "\n" + (pendulumText ?? string.Empty);
                var monsterBlock = monsterHeader + "\n" + (monsterText ?? string.Empty);

                if (inPendulumContext)
                    return setName + pendulumBlock + "\n" + WrapInactiveText(monsterBlock);
                if (inMonsterContext)
                    return setName + monsterBlock + "\n" + WrapInactiveText(pendulumBlock);

                return data.GetDescription(true);
            }

            var pendulumHeaderDefault = MDPro3.Duel.YGOSharp.Card.NormalizeBracketLabel(MDPro3.InterString.Get("【灵摆效果】"));
            var result = setName;

            if (!string.IsNullOrEmpty(monsterText))
                result += inPendulumContext ? WrapInactiveText(monsterText) : monsterText;

            if (!string.IsNullOrEmpty(pendulumText))
            {
                var pendulumBlock = pendulumHeaderDefault + "\n" + pendulumText;

                if (!string.IsNullOrEmpty(monsterText))
                {
                    if (inMonsterContext)
                    {
                        result += "\n" + WrapInactiveText(MDPro3.Duel.YGOSharp.Card.PendulumSeparatorLine + "\n" + pendulumBlock);
                    }
                    else if (inPendulumContext)
                    {
                        result += WrapInactiveText("\n" + MDPro3.Duel.YGOSharp.Card.PendulumSeparatorLine) + "\n" + pendulumBlock;
                    }
                    else
                    {
                        result += "\n" + MDPro3.Duel.YGOSharp.Card.PendulumSeparatorLine + "\n" + pendulumBlock;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(result))
                        result += "\n";
                    result += inMonsterContext ? WrapInactiveText(pendulumBlock) : pendulumBlock;
                }
            }

            return result;
        }

        public void Show(GameCard card)
        {
            var material = card.GetMaterial();
            try { Show(card, material); }
            finally { if (material != null) Destroy(material); }
        }

        public void Show(GameCard card, Material mat, int code = -1, GPS gps = null)
        {
            MDPro3.Duel.YGOSharp.Card data;
            if(code > -1)
                data = MDPro3.Duel.YGOSharp.CardsManager.Get(code);
            else
                data = card.GetData();
            MDPro3.Duel.YGOSharp.Card origin = MDPro3.Duel.YGOSharp.CardsManager.Get(data.Id);

            if (origin.Id == 0)
                return;

            EnsureFace();
            int version = RetireFace();

            var tails = string.Empty;
            if(code == -1)
                tails = "<color=#0FFF0F>" + card.tails.managedString + "</color>";
            GPS p;
            if (code > -1)
                p = gps;
            else
                p = card.p;

            this.data = data;

            var window = manager.GetElement<RectTransform>("Window");
            DOTween.Kill(window, false);
            window.anchoredPosition = new Vector2(20, window.anchoredPosition.y);
            showing = true;

            if (p == null || (p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Search) > 0)
            {
                manager.GetElement<Image>("Player").color = new Color(0, 0, 0, 0.3f);
                manager.GetElement("BaseActivated").SetActive(false);
            }
            else if (p.controller == 0)
            {
                manager.GetElement<Image>("Player").color = new Color(0, 0, 1, 0.3f);
                if (myActivated.Contains(data.Id))
                    manager.GetElement("BaseActivated").SetActive(true);
                else
                    manager.GetElement("BaseActivated").SetActive(false);
            }
            else
            {
                manager.GetElement<Image>("Player").color = new Color(1, 0, 0, 0.3f);
                if (opActivated.Contains(data.Id))
                    manager.GetElement("BaseActivated").SetActive(true);
                else
                    manager.GetElement("BaseActivated").SetActive(false);
            }

            var cardName = GetDisplayedCardName(data, true);
            manager.GetElement<Text>("TextName").text = cardName;
            AttributeSprite attributeSprite = GetCardAttribute(data);
            manager.GetElement<Image>("Attribute").sprite = attributeSprite.sprite;
            manager.GetElement("ArributeOutline").SetActive(attributeSprite.notOriginal);
            var frameColors = GetCardFrameColor(data);
            manager.GetElement<Image>("BaseName").color = frameColors[0];
            manager.GetElement<Image>("BaseType").color = frameColors[1];
            manager.GetElement<Image>("BaseActivated").color = frameColors[1];

            if (mat == null)
            {
                ShowFacePlaceholder(true);
                faceCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
                _ = RefreshFace(data.Id, version, faceCancellation.Token);
            }
            else
            {
                ownedFaceMaterial = Instantiate(mat);
                ownedFaceMaterial.SetFloat("_Monochrome", 0);
                ownedFaceMaterial.renderQueue = 3000;
                face.material = ownedFaceMaterial; face.color = faceColor;
            }

            manager.GetElement<Text>("TextType").text = data.GetTypeForUI();
            if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Pendulum))
                manager.GetElement<TextMeshProUGUI>("TextDescription").text = tails + BuildPendulumDescription(data, origin, p);
            else
                manager.GetElement<TextMeshProUGUI>("TextDescription").text = tails + data.GetDescription(true);

            manager.GetElement<TextMeshProUGUI>("TextDescription").fontSize = 25f * MDPro3.Config.GetUIScale(1.35f);

            bool isMonster = CardIsMonster(data);
            if (isMonster)
            {
                manager.GetElement("PropertyMonster").SetActive(true);
                manager.GetElement("PropertySpell").SetActive(false);

                RaceSprite raceSprite = GetCardRace(data);
                manager.GetElement<Image>("Race").sprite = raceSprite.sprite;
                manager.GetElement("RaceOutline").SetActive(raceSprite.notOriginal);
                bool isTuner = false;
                if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Tuner))
                {
                    isTuner = true;
                    manager.GetElement("Tuner").SetActive(true);
                }
                else
                    manager.GetElement("Tuner").SetActive(false);

                if(isTuner)
                {
                    if (origin.HasType(MDPro3.Duel.YGOSharp.CardType.Tuner))
                        manager.GetElement("TunerOutline").SetActive(false);
                    else
                        manager.GetElement("TunerOutline").SetActive(true);
                }
                else
                    manager.GetElement("TunerOutline").SetActive(false);

                manager.GetElement<Image>("Level").sprite = Runtime.TextureManager.GetCardLevelIcon(data);
                if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Link))
                {
                    manager.GetElement<Text>("TextLevel").text = data.GetLinkCount().ToString();

                    manager.GetElement("Scale").SetActive(false);
                    manager.GetElement("TextScale").SetActive(false);
                    manager.GetElement("Defense").SetActive(false);
                    manager.GetElement("TextDefense").SetActive(false);

                    manager.GetElement<RectTransform>("Attack").anchoredPosition = new Vector2(0, -45);
                    manager.GetElement<RectTransform>("TextAttack").anchoredPosition = new Vector2(40, -45);
                }
                else
                {
                    manager.GetElement<Text>("TextLevel").text = data.Level.ToString();
                    if (data.Level > origin.Level)
                        manager.GetElement<Text>("TextLevel").color = upColor;
                    else if (data.Level < origin.Level)
                        manager.GetElement<Text>("TextLevel").color = downColor;
                    else
                        manager.GetElement<Text>("TextLevel").color = equalColor;

                    manager.GetElement("Defense").SetActive(true);
                    manager.GetElement("TextDefense").SetActive(true);
                    var defense = MDPro3.Duel.YGOSharp.Card.NormalizeBattleValue(data.Defense, true);
                    var originDefense = MDPro3.Duel.YGOSharp.Card.NormalizeBattleValue(origin.Defense, false);
                    manager.GetElement<Text>("TextDefense").text = MDPro3.Duel.YGOSharp.Card.FormatBattleValue(defense);
                    if (MDPro3.Duel.YGOSharp.Card.NormalizeBattleValue(defense, false) > originDefense)
                        manager.GetElement<Text>("TextDefense").color = upColor;
                    else if (MDPro3.Duel.YGOSharp.Card.NormalizeBattleValue(defense, false) < originDefense)
                        manager.GetElement<Text>("TextDefense").color = downColor;
                    else
                        manager.GetElement<Text>("TextDefense").color = equalColor;

                    if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Pendulum))
                    {
                        manager.GetElement("Scale").SetActive(true);
                        manager.GetElement("TextScale").SetActive(true);
                        manager.GetElement<RectTransform>("Attack").anchoredPosition = new Vector2(0, -90);
                        manager.GetElement<RectTransform>("TextAttack").anchoredPosition = new Vector2(40, -90);
                        manager.GetElement<RectTransform>("Defense").anchoredPosition = new Vector2(0, -135);
                        manager.GetElement<RectTransform>("TextDefense").anchoredPosition = new Vector2(40, -135);

                        manager.GetElement<Text>("TextScale").text = data.LScale.ToString();
                        if (data.LScale > origin.LScale)
                            manager.GetElement<Text>("TextScale").color = upColor;
                        else if (data.LScale < origin.LScale)
                            manager.GetElement<Text>("TextScale").color = downColor;
                        else
                            manager.GetElement<Text>("TextScale").color = equalColor;
                    }
                    else
                    {
                        manager.GetElement("Scale").SetActive(false);
                        manager.GetElement("TextScale").SetActive(false);
                        manager.GetElement<RectTransform>("Attack").anchoredPosition = new Vector2(0, -45);
                        manager.GetElement<RectTransform>("TextAttack").anchoredPosition = new Vector2(40, -45);
                        manager.GetElement<RectTransform>("Defense").anchoredPosition = new Vector2(0, -90);
                        manager.GetElement<RectTransform>("TextDefense").anchoredPosition = new Vector2(40, -90);
                    }
                }

                var attack = MDPro3.Duel.YGOSharp.Card.NormalizeBattleValue(data.Attack, true);
                var originAttack = MDPro3.Duel.YGOSharp.Card.NormalizeBattleValue(origin.Attack, false);
                manager.GetElement<Text>("TextAttack").text = MDPro3.Duel.YGOSharp.Card.FormatBattleValue(attack);
                if (MDPro3.Duel.YGOSharp.Card.NormalizeBattleValue(attack, false) > originAttack)
                    manager.GetElement<Text>("TextAttack").color = upColor;
                else if (MDPro3.Duel.YGOSharp.Card.NormalizeBattleValue(attack, false) < originAttack)
                    manager.GetElement<Text>("TextAttack").color = downColor;
                else
                    manager.GetElement<Text>("TextAttack").color = equalColor;
            }
            else
            {
                manager.GetElement("PropertyMonster").SetActive(false);
                manager.GetElement("PropertySpell").SetActive(true);
                manager.GetElement<Image>("SpellType").sprite = Runtime.TextureManager.GetSpellTrapTypeIcon(data);
                manager.GetElement<Text>("TextSpellType").text = data.GetSpellTrapType();
            }
            RefreshLimitIcon(data.Id);
        }

        private void RefreshLimitIcon(int code)
        {
            MDPro3.Duel.YGOSharp.Banlist banlist = MDPro3.Duel.YGOSharp.BanlistManager.currentBanList;
            var limit = banlist.GetQuantity(code);
            if (limit == 3)
                manager.GetElement<Image>("Limit").sprite = Runtime.TextureManager.container.typeNone;
            else if (limit == 2)
                manager.GetElement<Image>("Limit").sprite = Runtime.TextureManager.container.limit2;
            else if (limit == 1)
                manager.GetElement<Image>("Limit").sprite = Runtime.TextureManager.container.limit1;
            else
                manager.GetElement<Image>("Limit").sprite = Runtime.TextureManager.container.banned;
        }

        public static bool CardIsMonster(MDPro3.Duel.YGOSharp.Card data)
        {
            MDPro3.Duel.YGOSharp.Card origin = MDPro3.Duel.YGOSharp.CardsManager.Get(data.Id);
            if (!origin.HasType(MDPro3.Duel.YGOSharp.CardType.Monster))
            {
                if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Monster))
                    return true;
                else
                    return false;
            }
            else
            {
                if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Spell))
                    return false;
                else if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Trap))
                    return false;
                else
                    return true;
            }
        }

        public struct AttributeSprite
        {
            public Sprite sprite;
            public bool notOriginal;
        }

        private static AttributeSprite GetCardAttribute(MDPro3.Duel.YGOSharp.Card data)
        {
            MDPro3.Duel.YGOSharp.Card origin = MDPro3.Duel.YGOSharp.CardsManager.Get(data.Id);
            AttributeSprite returnValue = new AttributeSprite();

            bool isMonster = CardIsMonster(data);

            if (isMonster)
            {
                if (!origin.HasType(MDPro3.Duel.YGOSharp.CardType.Monster))
                {
                    returnValue.notOriginal = true;
                    returnValue.sprite = Runtime.TextureManager.container.GetCardAttributeIcon(data);
                }
                else
                {
                    if (origin.HasType(MDPro3.Duel.YGOSharp.CardType.Trap))
                    {
                        returnValue.notOriginal = true;
                        returnValue.sprite = Runtime.TextureManager.container.GetCardAttributeIcon(data);
                    }
                    else
                    {
                        if (data.Attribute == origin.Attribute)
                        {
                            returnValue.notOriginal = false;
                            returnValue.sprite = Runtime.TextureManager.container.GetCardAttributeIcon(data);
                        }
                        else
                        {
                            returnValue.notOriginal = true;
                            MDPro3.Duel.YGOSharp.Card newData = data.Clone();
                            newData.Attribute = data.Attribute ^ origin.Attribute;
                            returnValue.sprite = Runtime.TextureManager.container.GetCardAttributeIcon(newData);
                        }
                    }
                }
            }
            else
            {
                if (!origin.HasType(MDPro3.Duel.YGOSharp.CardType.Monster))
                {
                    if ((data.Type & (uint)MDPro3.Duel.YGOSharp.CardType.Spell & origin.Type) > 0)
                    {
                        returnValue.sprite = Runtime.TextureManager.container.attributeSpell;
                        returnValue.notOriginal = false;
                    }
                    else if ((data.Type & (uint)MDPro3.Duel.YGOSharp.CardType.Trap & origin.Type) > 0)
                    {
                        returnValue.sprite = Runtime.TextureManager.container.attributeTrap;
                        returnValue.notOriginal = false;
                    }
                    else
                    {
                        returnValue.notOriginal = true;
                        if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Spell))
                            returnValue.sprite = Runtime.TextureManager.container.attributeSpell;
                        else
                            returnValue.sprite = Runtime.TextureManager.container.attributeTrap;
                    }
                }
                else
                {
                    returnValue.notOriginal = true;
                    if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Spell))
                        returnValue.sprite = Runtime.TextureManager.container.attributeSpell;
                    else
                        returnValue.sprite = Runtime.TextureManager.container.attributeTrap;
                }
            }

            return returnValue;
        }

        public struct RaceSprite
        {
            public Sprite sprite;
            public bool notOriginal;
        }

        public static RaceSprite GetCardRace(MDPro3.Duel.YGOSharp.Card data)
        {
            RaceSprite returnValue = new RaceSprite();
            returnValue.notOriginal = false;
            MDPro3.Duel.YGOSharp.Card origin = MDPro3.Duel.YGOSharp.CardsManager.Get(data.Id);
            if (!origin.HasType(MDPro3.Duel.YGOSharp.CardType.Monster))
                returnValue.notOriginal = true;
            else
                if (data.Race != origin.Race)
                returnValue.notOriginal = true;
            returnValue.sprite = Runtime.TextureManager.GetCardRaceIcon(data.Race);
            return returnValue;
        }

        public static Color[] GetCardFrameColor(MDPro3.Duel.YGOSharp.Card data)
        {
            var returnValue = new Color[2];
            returnValue[0] = new Color(0.7764f, 0.6784f, 0.6274f, 1f);
            returnValue[1] = returnValue[0];
            if (data.Id == 0)
                return returnValue;
            MDPro3.Duel.YGOSharp.Card origin = MDPro3.Duel.YGOSharp.CardsManager.Get(data.Id);
            if (data.Id == 10000000)
            {
                returnValue[0] = new Color(0.4745f, 0.4549f, 1f, 1f);
                returnValue[1] = returnValue[0];
            }
            else if (data.Id == 10000020)
            {
                returnValue[0] = new Color(1f, 0.2470f, 0.2156f, 1f);
                returnValue[1] = returnValue[0];
            }
            else if (data.Id == 10000010)
            {
                returnValue[0] = new Color(1f, 0.9882f, 0.1882f, 1f);
                returnValue[1] = returnValue[0];
            }
            else if (origin.HasType(MDPro3.Duel.YGOSharp.CardType.Pendulum))
            {
                if (origin.HasType(MDPro3.Duel.YGOSharp.CardType.Ritual))
                {
                    returnValue[0] = new Color(0.3176f, 0.5882f, 1f, 1f);
                    returnValue[1] = new Color(0f, 0.8901f, 0.7411f, 1f);
                }
                else if (origin.HasType(MDPro3.Duel.YGOSharp.CardType.Fusion))
                {
                    returnValue[0] = new Color(0.8823f, 0.345f, 1f, 1f);
                    returnValue[1] = new Color(0f, 0.8901f, 0.7411f, 1f);
                }
                else if (origin.HasType(MDPro3.Duel.YGOSharp.CardType.Synchro))
                {
                    returnValue[0] = new Color(1f, 1f, 1f, 1f);
                    returnValue[1] = new Color(0f, 0.8901f, 0.7411f, 1f);
                }
                else if (origin.HasType(MDPro3.Duel.YGOSharp.CardType.Xyz))
                {
                    returnValue[0] = new Color(0f, 0f, 0f, 1f);
                    returnValue[1] = new Color(0f, 0.8901f, 0.7411f, 1f);
                }
                else if (origin.HasType(MDPro3.Duel.YGOSharp.CardType.Effect))
                {
                    returnValue[0] = new Color(1f, 0.4745f, 0.1882f, 1f);
                    returnValue[1] = new Color(0f, 0.8901f, 0.7411f, 1f);
                }
                else if (origin.HasType(MDPro3.Duel.YGOSharp.CardType.Normal))
                {
                    returnValue[0] = new Color(1f, 0.7450f, 0.3294f, 1f);
                    returnValue[1] = new Color(0f, 0.8901f, 0.7411f, 1f);
                }
            }
            else if (origin.HasType(MDPro3.Duel.YGOSharp.CardType.Ritual) && origin.HasType(MDPro3.Duel.YGOSharp.CardType.Monster))
            {
                returnValue[0] = new Color(0.3176f, 0.5882f, 1f, 1f);
                returnValue[1] = returnValue[0];
            }
            else if (origin.HasType(MDPro3.Duel.YGOSharp.CardType.Fusion))
            {
                returnValue[0] = new Color(0.8823f, 0.345f, 1f, 1f);
                returnValue[1] = returnValue[0];
            }
            else if (origin.HasType(MDPro3.Duel.YGOSharp.CardType.Synchro))
            {
                returnValue[0] = new Color(1f, 1f, 1f, 1f);
                returnValue[1] = returnValue[0];
            }
            else if (origin.HasType(MDPro3.Duel.YGOSharp.CardType.Xyz))
            {
                returnValue[0] = new Color(0f, 0f, 0f, 1f);
                returnValue[1] = returnValue[0];
            }
            else if (origin.HasType(MDPro3.Duel.YGOSharp.CardType.Link))
            {
                returnValue[0] = new Color(0f, 0.3764f, 0.7764f, 1f);
                returnValue[1] = returnValue[0];
            }
            else if (origin.HasType(MDPro3.Duel.YGOSharp.CardType.Token))
            {
                returnValue[0] = new Color(0.7764f, 0.6784f, 0.6274f, 1f);
                returnValue[1] = returnValue[0];
            }
            else if (origin.HasType(MDPro3.Duel.YGOSharp.CardType.Effect))
            {
                returnValue[0] = new Color(1f, 0.4745f, 0.1882f, 1f);
                returnValue[1] = returnValue[0];
            }
            else if (origin.HasType(MDPro3.Duel.YGOSharp.CardType.Normal))
            {
                returnValue[0] = new Color(1f, 0.7450f, 0.3294f, 1f);
                returnValue[1] = returnValue[0];
            }
            else if (origin.HasType(MDPro3.Duel.YGOSharp.CardType.Spell))
            {
                if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Effect))
                {
                    returnValue[0] = new Color(1f, 0.4745f, 0.1882f, 1f);
                    returnValue[1] = new Color(0f, 0.8901f, 0.7411f, 1f);
                }
                else if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Normal))
                {
                    returnValue[0] = new Color(1f, 0.7450f, 0.3294f, 1f);
                    returnValue[1] = new Color(0f, 0.8901f, 0.7411f, 1f);
                }
                else
                {
                    returnValue[0] = new Color(0f, 0.8901f, 0.7411f, 1f);
                    returnValue[1] = returnValue[0];
                }
            }
            else if (origin.HasType(MDPro3.Duel.YGOSharp.CardType.Trap))
            {
                if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Effect))
                {
                    returnValue[0] = new Color(1f, 0.4745f, 0.1882f, 1f);
                    returnValue[1] = new Color(1f, 0.0509f, 0.6784f, 1f);
                }
                else if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Normal))
                {
                    returnValue[0] = new Color(1f, 0.7450f, 0.3294f, 1f);
                    returnValue[1] = new Color(1f, 0.0509f, 0.6784f, 1f);
                }
                else
                {
                    returnValue[0] = new Color(1f, 0.0509f, 0.6784f, 1f);
                    returnValue[1] = returnValue[0];
                }
            }
            return returnValue;
        }

    }
}
