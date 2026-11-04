using Cysharp.Threading.Tasks;
using DG.Tweening;
using MDPro3.Duel.YGOSharp;
using MDPro3.UI;
using MDPro3.UI.ServantUI;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.UI;
using YgomSystem.ElementSystem;

namespace MDPro3.Servant
{
    public class CutinViewer : Servant
    {

        public const float CUTIN_PLAY_TIME = 1.6f;

        public static int controller = 0;
        public static List<Card> cards = new();
        public static List<int> codes = new();
        public static List<int> codes2 = new();
        private static DirectoryInfo[] dirInfos;
        private static FileInfo[] fileInfos;
        private bool randomBGMPlayed;
        [HideInInspector] public SelectionToggle_Cutin lastSelectedCutinItem;

        private static bool playing;
        private static bool autoPlaying;
        private CancellationTokenSource cts;

        #region Servant
        public override int Depth => 1;
        protected override bool ShowLine => false;

        public override void Initialize()
        {
            returnServant = Program.instance.menu;
            base.Initialize();
            LoadCutins();
        }

        protected override void ApplyShowArrangement(int preDepth)
        {
            base.ApplyShowArrangement(preDepth);
            UserInput.SetMoveRepeatSpeed(2f);
        }

        protected override void ApplyHideArrangement(int nextDepth)
        {
            base.ApplyHideArrangement(nextDepth);
            UserInput.SetMoveRepeatSpeed(1f);

            if (randomBGMPlayed)
            {
                randomBGMPlayed = false;
                AudioManager.PlayBGM(AudioManager.BGM_MENU_MAIN);
            }

            CameraManager.DuelOverlayEffect3DCount = 0;
            CameraManager.DuelOverlayEffect3DMinus();
        }

        protected override void AfterHidingEvent()
        {
            Program.instance.UnloadUnusedAssets();
        }

        public override void PerFrameFunction()
        {
            if (NeedResponseInput())
            {
                if (UserInput.MouseRightDown || UserInput.WasCancelPressed)
                    OnReturn();

#if UNITY_ANDROID || UNITY_IOS
                //if (UserInput.MouseLeftDown)
                //    if(autoPlay != null)
                //        OnReturn();
#endif

                if (UserInput.WasGamepadButtonWestPressed)
                    GetUI<CutinViewerUI>().FocusOnInputField();
                if (UserInput.WasGamepadButtonNorthPressed)
                    AutoPlay();
            }
        }

        public override void OnReturn()
        {
            if (returnAction != null) return;
            if (inTransition) return;
            AudioManager.PlaySE("SE_MENU_CANCEL");
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;

                UIManager.ShowExitButton(TransitionTime);
                servantUI.CG.alpha = 1;
                servantUI.CG.blocksRaycasts = true;
            }
            else
                OnExit();
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !UserInput.NeedDefaultSelect())
                return;
            lastSelectedCutinItem.GetSelectable().Select();
        }

#endregion

        public void LoadCutins()
        {
            if(dirInfos == null || fileInfos == null)
            {
                var targetFolder = Program.root + "MonsterCutin";
                var targetFolder2 = Program.root + "MonsterCutin2";

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                targetFolder = Path.Combine(Application.dataPath, Program.root + "MonsterCutin");
                targetFolder2 = Path.Combine(Application.dataPath, Program.root + "MonsterCutin2");
#endif
                if (!Directory.Exists(targetFolder))
                    Directory.CreateDirectory(targetFolder);
                if (!Directory.Exists(targetFolder2))
                    Directory.CreateDirectory(targetFolder2);
                dirInfos = new DirectoryInfo(targetFolder).GetDirectories();
                fileInfos = new DirectoryInfo(targetFolder2).GetFiles();
            }

            cards.Clear();
            codes.Clear();
            codes2.Clear();

            for (int i = 0; i < dirInfos.Length; i++)
            {
                if(int.TryParse(dirInfos[i].Name, out var code))
                {
                    Card card = CardsManager.GetCard(code);
                    if (card == null)
                        continue;
                    cards.Add(card);
                    codes.Add(card.Id);
                }
            }
            for (int i = 0; i < fileInfos.Length; i++)
            {
                if(int.TryParse(fileInfos[i].Name, out var code))
                {
                    if (!codes.Contains(code))
                    {
                        Card card = CardsManager.Get(code);
                        cards.Add(card);
                        codes2.Add(card.Id);
                    }
                }
            }
            cards.Sort(CardsManager.ComparisonOfCard());

            if (servantUI != null)
                GetUI<CutinViewerUI>().Print();
        }

        public void SelectLastCutinItem()
        {
            UserInput.NextSelectionIsAxis = true;
            Select();
        }

        public static bool CutinExist(int code)
        {
            if (OcgCore.condition == OcgCore.Condition.Duel
                && !Config.GetBool("DuelCutin", true))
                return false;
            if (OcgCore.condition == OcgCore.Condition.Watch
                && !Config.GetBool("WatchCutin", true))
                return false;
            if (OcgCore.condition == OcgCore.Condition.Replay
                && !Config.GetBool("ReplayCutin", true))
                return false;
            code = SpecialCards.GetOriginalCutinCode(code);
            return cards.Any(c => c.Id == code);
        }

        public static async UniTask Play(int code, int controller)
        {
            if (playing)
            {
                Debug.LogWarning("A cutin is already playing. Ignoring the new cutin request.");
                return;
            }
            playing = true;

            code = SpecialCards.GetOriginalCutinCode(code);
            Card card = CardsManager.Get(code);

            GameObject cutin = null;
            bool diy = false;
            if (codes.Contains(code))
                cutin = await ABLoader.LoadMonsterCutinAsync(code, false, true);
            else
            {
                cutin = await ABLoader.LoadFromFileAsync("MonsterCutin2/" + code, false, true);
                diy = true;
            }
            cutin.transform.SetParent(Program.instance.container_2D, false);
            if (!diy)
            {
                cutin.transform.localPosition = Vector3.zero;
                cutin.transform.GetComponent<PlayableDirector>().time = 0;
            }

            //BackEffects
            string backPath;
            if (card.IsAttribute(CardAttribute.Dark)) // 125
                backPath = "SummonMonster_Bgdak_S2";
            else if (card.IsAttribute(CardAttribute.Light)) // 100
                backPath = "SummonMonster_Bglit_S2";
            else if (card.IsAttribute(CardAttribute.Earth)) // 56
                backPath = "SummonMonster_Bgeah_S2";
            else if (card.IsAttribute(CardAttribute.Water)) // 35
                backPath = "SummonMonster_Bgwtr_S2";
            else if (card.IsAttribute(CardAttribute.Fire)) // 31
                backPath = "SummonMonster_Bgfie_S2";
            else if (card.IsAttribute(CardAttribute.Wind)) // 25
                backPath = "SummonMonster_Bgwid_S2";
            else // 4
                backPath = "SummonMonster_Bgdve_S2";
            GameObject back = ABLoader.LoadMasterDuelOutDuelObject(backPath);
            back.transform.SetParent(Program.instance.container_2D, false);

            //Name Bar
            GameObject nameBar;
            if (controller == 0)
                nameBar = ABLoader.LoadMasterDuelOutDuelObject("SummonMonster_Name_near");
            else
                nameBar = ABLoader.LoadMasterDuelOutDuelObject("SummonMonster_Name_far");
            nameBar.transform.SetParent(Program.instance.container_2D, false);
            var manager = nameBar.GetComponent<ElementObjectManager>();
            var tmp = manager.GetElement<TextMeshPro>("Monster_Name_TMP");
            tmp.text = card.Name;
            var para = "ATK " + card.GetAttackString();
            if (!card.HasType(CardType.Link))
            {
                para += " DEF " + card.GetDefenseString();
                Destroy(manager.GetElement("Icon_LINK"));
            }
            else
            {
                Destroy(manager.GetElement("Icon_Level"));
                Destroy(manager.GetElement("Icon_Level_Odd"));
                Destroy(manager.GetElement("Icon_Rank"));
                Destroy(manager.GetElement("Icon_Rank_Odd"));
                switch (card.GetLinkCount())
                {
                    case 2:
                        manager.GetElement<ElementObjectManager>("Icon_LINK").
                            GetElement<SpriteRenderer>("LINK1").sprite = TextureManager.container.link2;
                        break;
                    case 3:
                        manager.GetElement<ElementObjectManager>("Icon_LINK").
                            GetElement<SpriteRenderer>("LINK1").sprite = TextureManager.container.link3;
                        break;
                    case 4:
                        manager.GetElement<ElementObjectManager>("Icon_LINK").
                            GetElement<SpriteRenderer>("LINK1").sprite = TextureManager.container.link4;
                        break;
                    case 5:
                        manager.GetElement<ElementObjectManager>("Icon_LINK").
                            GetElement<SpriteRenderer>("LINK1").sprite = TextureManager.container.link5;
                        break;
                    case 6:
                        manager.GetElement<ElementObjectManager>("Icon_LINK").
                            GetElement<SpriteRenderer>("LINK1").sprite = TextureManager.container.link6;
                        break;
                }
            }
            ElementObjectManager subManager;
            if (!card.HasType(CardType.Xyz))
            {
                Destroy(manager.GetElement("Icon_Rank"));
                Destroy(manager.GetElement("Icon_Rank_Odd"));
                if (card.Level % 2 == 0)
                {
                    subManager = manager.GetElement<ElementObjectManager>("Icon_Level");
                    Destroy(manager.GetElement("Icon_Level_Odd"));
                }
                else
                {
                    subManager = manager.GetElement<ElementObjectManager>("Icon_Level_Odd");
                    Destroy(manager.GetElement("Icon_Level"));
                }
            }
            else
            {
                Destroy(manager.GetElement("Icon_Level"));
                Destroy(manager.GetElement("Icon_Level_Odd"));
                if (card.Level % 2 == 0)
                {
                    subManager = manager.GetElement<ElementObjectManager>("Icon_Rank");
                    Destroy(manager.GetElement("Icon_Rank_Odd"));
                }
                else
                {
                    subManager = manager.GetElement<ElementObjectManager>("Icon_Rank_Odd");
                    Destroy(manager.GetElement("Icon_Rank"));
                }
            }
            if (!card.HasType(CardType.Link))
                for (int i = card.Level + 1; i < 14; i++)
                    Destroy(subManager.GetElement("Icon" + i));
            manager.GetElement<TextMesh>("Monster_Para").text = para;

            //front Effect
            //var frontEffect = ABLoader.LoadMasterDuelOutDuelObject(card.IsHighLevel() ? "SummonMonster_Thunder_power" : "SummonMonster_Thunder_normal");
            var frontEffect = ABLoader.LoadMasterDuelOutDuelObject("SummonMonster_Thunder_power");
            frontEffect.transform.SetParent(Program.instance.container_2D, false);

            if (Program.instance.ocgcore.showing)
                AudioManager.PlayBgmKeyCard();

            await UniTask.WaitForSeconds(CUTIN_PLAY_TIME).ContinueWith(() =>
            {
                Destroy(cutin);
                Destroy(back);
                Destroy(nameBar);
                Destroy(frontEffect);

                playing = false;
            });
        }

        public void AutoPlay()
        {
            cts = new();
            _ = AutoPlayAsync(cts.Token);
        }

        private async UniTask AutoPlayAsync(CancellationToken token)
        {
            await UniTask.WaitWhile(() => playing, cancellationToken : token);
            if(!showing)
                return;

            AudioManager.PlayRandomKeyCardBGM();
            randomBGMPlayed = true;
            servantUI.CG.alpha = 0f;
            servantUI.CG.blocksRaycasts = false;
            UIManager.HideExitButton(TransitionTime);
            int count = 0;
            foreach (var card in cards)
            {
                await Play(card.Id, 0);
                count++;
                if (count % 20 == 0)
                    await Resources.UnloadUnusedAssets();
                if (token.IsCancellationRequested)
                    return;
            }
            servantUI.CG.alpha = 1f;
            servantUI.CG.blocksRaycasts = true;
            UIManager.ShowExitButton(TransitionTime);
            autoPlaying = false;
            cts.Dispose();
            cts = null;
        }

        public static async UniTask CacheCutin(int code)
        {
            if (!CutinExist(code))
                return;

            code = SpecialCards.GetOriginalCutinCode(code);
            if (codes.Contains(code))
                await ABLoader.LoadMonsterCutinAsync(code, true, false);
            else
                await ABLoader.LoadFromFileAsync("MonsterCutin2/" + code, true, false);
        }

    }
}
