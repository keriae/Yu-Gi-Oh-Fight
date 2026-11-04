using DG.Tweening;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using static MDPro3.Servant.PuzzleSelector;

namespace MDPro3.UI.ServantUI
{
    public class PuzzlePortalUI : ServantUI
    {
        #region Elements

        private const string LABEL_RT_MAIN_GROUP = "MainGroup";
        private RectTransform m_MainGroup;
        private RectTransform MainGroup =>
           m_MainGroup = m_MainGroup != null ? m_MainGroup
            : Manager.GetElement<RectTransform>(LABEL_RT_MAIN_GROUP);

        private const string LABEL_RT_GATE_LIST_GROUP = "GateListGroup";
        private RectTransform m_GateListGroup;
        private RectTransform GateListGroup =>
           m_GateListGroup = m_GateListGroup != null ? m_GateListGroup
            : Manager.GetElement<RectTransform>(LABEL_RT_GATE_LIST_GROUP);

        private const string LABEL_SBN_TEMPLATE = "GateListTemplate";
        private SelectionButton m_GateListTemplate;
        private SelectionButton GateListTemplate =>
           m_GateListTemplate = m_GateListTemplate != null ? m_GateListTemplate
            : Manager.GetElement<SelectionButton>(LABEL_SBN_TEMPLATE);

        private const string LABEL_SBN_RECOMMEND_1 = "RecommendButton1";
        private SelectionButton m_RecommendButton1;
        private SelectionButton RecommendButton1 =>
           m_RecommendButton1 = m_RecommendButton1 != null ? m_RecommendButton1
            : Manager.GetElement<SelectionButton>(LABEL_SBN_RECOMMEND_1);

        private const string LABEL_SBN_RECOMMEND_2 = "RecommendButton2";
        private SelectionButton m_RecommendButton2;
        private SelectionButton RecommendButton2 =>
           m_RecommendButton2 = m_RecommendButton2 != null ? m_RecommendButton2
            : Manager.GetElement<SelectionButton>(LABEL_SBN_RECOMMEND_2);

        private const string LABEL_SBN_LAST_PLAY = "LastPlayButton";
        private SelectionButton m_LastPlayButton;
        private SelectionButton LastPlayButton =>
           m_LastPlayButton = m_LastPlayButton != null ? m_LastPlayButton
            : Manager.GetElement<SelectionButton>(LABEL_SBN_LAST_PLAY);

        #endregion

        #region ServantUI

        private void Awake()
        {
            if (!Directory.Exists(Program.PATH_PUZZLE))
                Directory.CreateDirectory(Program.PATH_PUZZLE);
        }

        public override void Show(bool cover)
        {
            ShowAction();
            ShowEvent();

            CreatePuzzleGroupButtons();
            SetRecommendPuzzleButtons();
        }

        public override void Hide(bool cover)
        {
            HideAction();
            HideEvent();
        }

        private void ShowAction()
        {
            KillTransitionTweens();
            gameObject.SetActive(true);
            CG.alpha = 0f;
            CG.blocksRaycasts = false;

            DOTween.Sequence()
                .SetTarget(gameObject)
                .SetUpdate(true)
                .AppendInterval(0.2f)
                .Append(CG.DOFade(1f, 0.3f).SetEase(Ease.Linear))
                .OnComplete(() =>
                {
                    CG.blocksRaycasts = true;
                    AfterShowEvent();
                });

            var op = MainGroup.anchoredPosition;
            op.x = -1920f;
            MainGroup.anchoredPosition = op;
            DOTween.Sequence()
                .SetTarget(gameObject)
                .SetUpdate(true)
                .AppendInterval(0.2f)
                .Append(MainGroup.DOAnchorPosX(0f, 0.45f).SetEase(Ease.OutQuart));

            op = GateListGroup.anchoredPosition;
            op.x = 1920f;
            GateListGroup.anchoredPosition = op;
            DOTween.Sequence()
                .SetTarget(gameObject)
                .SetUpdate(true)
                .AppendInterval(0.2f)
                .Append(GateListGroup.DOAnchorPosX(0f, 0.45f).SetEase(Ease.OutQuart));
        }

        private void HideAction()
        {
            KillTransitionTweens();
            CG.blocksRaycasts = false;
            CG.DOFade(0f, 0.3f)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    AfterHideEvent();
                });

            MainGroup.DOAnchorPosX(-1920f, 0.3f).SetEase(Ease.InCubic).SetUpdate(true);
            GateListGroup.DOAnchorPosX(1920f, 0.3f).SetEase(Ease.InCubic).SetUpdate(true);
        }

        public void SelectNearestPuzzleGroup(Transform call)
        {
            if(puzzleGroupButtons.Count == 0) return;
            var buttonTransforms = new List<Transform>();
            foreach(var button in puzzleGroupButtons)
                buttonTransforms.Add(button.transform);
            var nearest = call.GetNearest(buttonTransforms);
            UserInput.NextSelectionIsAxis = true;
            nearest.GetComponent<SelectionButton>().GetSelectable().Select();
        }

        #endregion

        #region Functions

        private readonly List<GameObject> puzzleGroupButtons = new();

        private void CreatePuzzleGroupButtons()
        {
            foreach (var button in puzzleGroupButtons) {
                Destroy(button);
            }
            puzzleGroupButtons.Clear();

            GateListTemplate.gameObject.SetActive(false);

            var puzzleGroup = new PuzzleGroup
            {
                pathPrefix = string.Empty,
                namePattern = string.Empty
            };
            CreatePuzzleGroupButton(puzzleGroup);

            var folders = Directory.GetDirectories(Program.PATH_PUZZLE);
            foreach (var folder in folders)
            {
                string folderName = Path.GetFileName(folder) + "/";
                var pg = new PuzzleGroup
                {
                    pathPrefix = folderName,
                    namePattern = GetPuzzleNamePattern(folderName)
                };
                CreatePuzzleGroupButton(pg);
            }
        }

        private void CreatePuzzleGroupButton(PuzzleGroup puzzleGroup)
        {
            var button = Instantiate(GateListTemplate.gameObject, GateListTemplate.transform.parent);
            puzzleGroupButtons.Add(button);
            button.SetActive(true);
            var sbn = button.GetComponent<SelectionButton>();
            sbn.SetClickEvent(() =>
            {
                Program.instance.puzzle.puzzleGroup = puzzleGroup;
                Program.instance.ShiftToServant(Program.instance.puzzle);
            });
            sbn.SetButtonText(GetPuzzleGroupName(puzzleGroup));
            sbn.SetElementActive("NumBadge", PuzzleSelectorUI.GroupHasNew(puzzleGroup));
            sbn.SetElementActive("TextComplete", PuzzleSelectorUI.GroupAllClear(puzzleGroup));

            if (puzzleGroup.pathPrefix != string.Empty)
            {
                var puzzle = PuzzleSelectorUI.GetFirstPuzzle(puzzleGroup);
                if (int.TryParse(puzzle.firstCard, out var code))
                {
                    var card = CardsManager.Get(code, true);
                    if (card.Id != 0)
                    {
                        sbn.SetElementActive("ImageGate", false);
                        var image = sbn.GetElement<ArtRawImageHandler>("ImageCard");
                        image.gameObject.SetActive(true);
                        image.SetArt(code);
                        sbn.SetElementActive("ImageLine", true);
                    }
                }
            }
        }

        public static string GetPuzzleGroupName(PuzzleGroup puzzleGroup)
        {
            return puzzleGroup.pathPrefix switch
            {
                "" => InterString.Get("残局挑战"),
                PuzzlePortal.PATH_PUZZLE_CARD_EFFECT => InterString.Get("卡片效果特效速览"),
                PuzzlePortal.PATH_PUZZLE_FINAL_ATTACK => InterString.Get("最终一击特效速览"),
                PuzzlePortal.PATH_PUZZLE_SPECIAL_WIN => InterString.Get("特殊胜利特效速览"),
                PuzzlePortal.PATH_PUZZLE_TEST => InterString.Get("卡片测试"),
                _ => puzzleGroup.pathPrefix
            };
        }

        public static string GetPuzzleNamePattern(string pathPrefix)
        {
            return pathPrefix switch
            {
                PuzzlePortal.PATH_PUZZLE_CARD_EFFECT => "[?]",
                PuzzlePortal.PATH_PUZZLE_FINAL_ATTACK => "[?]",
                PuzzlePortal.PATH_PUZZLE_SPECIAL_WIN => "[?]",
                _ => string.Empty
            };
        }

        public static string GetPuzzleDisplayName(string puzzleFileName, string namePattern)
        {
            if (namePattern == string.Empty)
                return puzzleFileName;
            var code = puzzleFileName.ExtractLeadingInt();
            if (code.HasValue)
            {
                var card = CardsManager.Get(code.Value, true);
                return namePattern.Replace("[?]", card.Name);
            }
            else
                return puzzleFileName;
        }

        private void SetRecommendPuzzleButtons()
        {
            int need = 2;
            var recommendPuzzles = PuzzleSelectorUI.GetRecommendPuzzles(need);
            for (int i = 0; i < 2; i++)
                SetRecommendPuzzleButton(i == 0 ? RecommendButton1 : RecommendButton2, recommendPuzzles[i]);
            if (recommendPuzzles.Count < 2)
                RecommendButton2.gameObject.SetActive(false);
            if(recommendPuzzles.Count < 1)
                RecommendButton1.gameObject.SetActive(false);

            var lastPlayPuzzle = PuzzleSelectorUI.GetLastPlayPuzzle();
            if(lastPlayPuzzle != null)
            {
                LastPlayButton.gameObject.SetActive(true);
                SetRecommendPuzzleButton(LastPlayButton, lastPlayPuzzle.Value);
            }
            else
                LastPlayButton.gameObject.SetActive(false);
        }

        private void SetRecommendPuzzleButton(SelectionButton sbn, PuzzleSelectorUI.Puzzle puzzle)
        {
            var puzzleGroup = new PuzzleGroup
            {
                pathPrefix = puzzle.pathPrefix,
                namePattern = puzzle.namePattern
            };
            sbn.GetElement<TextMeshProUGUI>("TextCategoryName").text = GetPuzzleGroupName(puzzleGroup);
            sbn.SetButtonText(GetPuzzleDisplayName(puzzle.name, puzzle.namePattern));
            sbn.GetElement<ArtRawImageHandler>("ImageGate")
                .SetArt(int.TryParse(puzzle.firstCard, out var code) ? code : PuzzleSelectorUI.DEFAULT_CARD);
            sbn.SetElementActive("NumBadge", !Config.GetBool(puzzle.GetEnterConfigKey(), false));
            sbn.SetElementActive("TextComplete", Config.GetBool(puzzle.GetClearConfigKey(), false));
            sbn.SetClickEvent(() =>
            {
                Program.instance.puzzle.puzzleGroup = puzzleGroup;
                Program.instance.puzzle.currentPuzzle = Program.PATH_PUZZLE + puzzleGroup.pathPrefix + puzzle.name;
                Program.instance.puzzle.StartCurrentPuzzle(false);
            });
        }

        #endregion

    }
}
