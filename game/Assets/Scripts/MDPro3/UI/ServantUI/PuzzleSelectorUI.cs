using MDPro3.UI.PropertyOverride;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using static MDPro3.Servant.PuzzleSelector;
using static MDPro3.UI.ServantUI.PuzzleSelectorUI;

namespace MDPro3.UI.ServantUI
{
    public class PuzzleSelectorUI : ServantUI
    {
        #region Elements

        private const string LABEL_SBN_PLAY = "ButtonPlay";
        private SelectionButton m_ButtonPlay;
        public SelectionButton ButtonPlay =>
            m_ButtonPlay = m_ButtonPlay != null ? m_ButtonPlay
            : Manager.GetElement<SelectionButton>(LABEL_SBN_PLAY);

        private const string LABEL_SR = "ScrollRect";
        private ScrollRect m_ScrollRect;
        private ScrollRect ScrollRect =>
            m_ScrollRect = m_ScrollRect != null ? m_ScrollRect
            : Manager.GetElement<ScrollRect>(LABEL_SR);

        private const string LABEL_TXT_OVERVIEW = "TextOverview";
        private TextMeshProUGUI m_TextOverview;
        private TextMeshProUGUI TextOverview =>
            m_TextOverview = m_TextOverview != null ? m_TextOverview
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_OVERVIEW);

        private const string LABEL_MONO_ART = "ArtImage";
        private ArtRawImageHandler m_Art;
        public ArtRawImageHandler Art =>
            m_Art = m_Art != null ? m_Art
            : Manager.GetElement<ArtRawImageHandler>(LABEL_MONO_ART);

        private const string LABEL_IMG_HOVER = "ButtonHover";
        private Image m_ImageHover;
        public Image ImageHover =>
            m_ImageHover = m_ImageHover != null ? m_ImageHover
            : Manager.GetElement<Image>(LABEL_IMG_HOVER);

        private const string LABEL_IMG_OUT = "ButtonOut";
        private Image m_ImageOut;
        public Image ImageOut =>
            m_ImageOut = m_ImageOut != null ? m_ImageOut
            : Manager.GetElement<Image>(LABEL_IMG_OUT);

        #endregion

        public const int DEFAULT_CARD = 5990062;
        private static readonly List<string> ignoreInPuzzleListPrefix = new()
        { "卡片特效", "胜利特效" };

        public struct Puzzle
        {
            public string name;
            public string firstCard;
            public string description;
            public string solution;
            public string pathPrefix;
            public string namePattern;

            public readonly string GetPath()
            {
                return Program.PATH_PUZZLE + pathPrefix + name + Program.EXPANSION_LUA;
            }

            public readonly string GetEnterConfigKey()
            {
                return Program.PATH_PUZZLE + pathPrefix + name + "_Enter";
            }

            public readonly string GetClearConfigKey()
            {
                return Program.PATH_PUZZLE + pathPrefix + name + "_Clear";
            }
        }
        private static readonly List<Puzzle> puzzles = new();
        public SuperScrollView superScrollView;

        public override void ShowEvent()
        {
            allPuzzles.Clear();

            if(Program.instance.lastServant != Program.instance.ocgcore || superScrollView == null)
            {
                GetPuzzles(Program.instance.puzzle.puzzleGroup);
                Print();
                Title.text = PuzzlePortalUI.GetPuzzleGroupName(Program.instance.puzzle.puzzleGroup);
            }
            else
            {
                var allToggles = superScrollView.gameObjects;
                foreach(var toggle in allToggles)
                {
                    var mono = toggle.GetComponent<SelectionToggle_Puzzle>();
                    mono.RefreshBadge();
                }
            }
        }

        private static PuzzleGroup? GetPuzzleGroupByPath(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var count = path.Count(c => c == '/');
                    var prefix = string.Empty;
                    //var folder = path.Replace(Path.GetFullPath(Program.PATH_PUZZLE), string.Empty).Split('/')[0];
                    if (count == 0)
                        throw new Exception("Invalid puzzle path");
                    if (count == 2)
                        prefix = path.Substring(Program.PATH_PUZZLE.Length, path.LastIndexOf('/') - Program.PATH_PUZZLE.Length + 1);
                    var puzzleGroup = new PuzzleGroup
                    {
                        pathPrefix = prefix,
                        namePattern = PuzzlePortalUI.GetPuzzleNamePattern(prefix)
                    };
                    return puzzleGroup;
                }
            }
            catch { }
            return new PuzzleGroup { pathPrefix = string.Empty, namePattern = string.Empty };
        }

        private static Puzzle GetPuzzle(string path)
        {
            var puzzleGroup = GetPuzzleGroupByPath(path);

            string text = File.ReadAllText(path);
            string st = text.Replace("\r", "");
            string[] lines = st.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            string card = "0";
            int messageStart = 0;
            int messageEnd = 0;
            int solutionStart = 0;
            int solutionEnd = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("Debug.AddCard(") && card == "0")
                    card = lines[i].Replace("Debug.AddCard(", "").Split(',')[0];
                else if (lines[i].StartsWith("--[[message"))
                    messageStart = i + 1;
                else if (lines[i].StartsWith("Solution:"))
                    solutionStart = i;
                else if (lines[i].StartsWith("]]"))
                {
                    if (messageEnd == 0)
                        messageEnd = i;
                    else
                        solutionEnd = i;
                }
            }
            string description = string.Empty;
            string solution = string.Empty;
            if (messageStart != 0 && messageEnd != 0)
                for (int i = messageStart; i < messageEnd; i++)
                    description += lines[i] + Program.STRING_LINE_BREAK;
            if (solutionStart != 0 && solutionEnd != 0)
                for (int i = solutionStart; i < solutionEnd; i++)
                    solution += lines[i] + Program.STRING_LINE_BREAK;
            description = description.Replace("\r\n\t\r\n\t", "\r\n\t");
            Puzzle puzzle = new()
            {
                name = Path.GetFileNameWithoutExtension(path),
                firstCard = card,
                description = description,
                solution = solution,
                pathPrefix = puzzleGroup.Value.pathPrefix,
                namePattern = puzzleGroup.Value.namePattern
            };
            return puzzle;
        }

        private static void GetPuzzles(PuzzleGroup puzzleGroup)
        {
            puzzles?.Clear();
            var path = Program.PATH_PUZZLE + puzzleGroup.pathPrefix;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            FileInfo[] fileInfos = new DirectoryInfo(path).GetFiles("*.lua");
            foreach (FileInfo fileInfo in fileInfos)
            {
                bool skip = false;
                if(puzzleGroup.pathPrefix == string.Empty)
                    foreach (var prefix in ignoreInPuzzleListPrefix)
                    {
                        if (fileInfo.Name.StartsWith(prefix))
                        {
                            skip = true;
                            break;
                        }
                    }
                if (skip) continue;

                var puzzle = GetPuzzle(path + fileInfo.Name);
                puzzles.Add(puzzle);
            }
        }

        public void Print()
        {
            superScrollView?.Clear();
            List<string[]> tasks = new();

            for (int i = 0; i < puzzles.Count; i++)
            {
                string[] task = new string[]
                {
                    i.ToString(),
                    puzzles[i].name,
                    puzzles[i].firstCard,
                    puzzles[i].description,
                    puzzles[i].solution,
                    puzzles[i].pathPrefix,
                    puzzles[i].namePattern
                };
                tasks.Add(task);
            }
            var handle = Addressables.LoadAssetAsync<GameObject>("UI/ItemPuzzle.prefab");
            handle.Completed += (result) =>
            {
                var itemHeight = PropertyOverrider.NeedMobileLayout() ? 180f : 150f;
                float topPadding = PropertyOverrider.NeedMobileLayout() ? 148f : 134f;
                float space = itemHeight - (PropertyOverrider.NeedMobileLayout() ? 152f : 122f);
                float bottomPadding = (PropertyOverrider.NeedMobileLayout() ? 64f : 54f) - space;
                superScrollView = new SuperScrollView(
                    1,
                    700,
                    itemHeight,
                    topPadding,
                    bottomPadding,
                    result.Result,
                    ItemOnListRefresh,
                    ScrollRect);
                superScrollView.Print(tasks);
                SelectFirst();
            };
        }

        private void SelectFirst()
        {
            if (superScrollView == null || superScrollView.gameObjects.Count == 0)
                return;

            var item0 = superScrollView.items[0].gameObject.GetComponent<SelectionToggle_Puzzle>();
            item0.SetToggleOn();
        }

        private void ItemOnListRefresh(string[] task, GameObject item)
        {
            var handler = item.GetComponent<SelectionToggle_Puzzle>();
            Puzzle puzzle = new()
            {
                name = task[1],
                firstCard = task[2],
                description = task[3],
                solution = task[4],
                pathPrefix = task[5],
                namePattern = task[6],
            };
            handler.puzzle = puzzle;
            handler.Refresh();
        }

        public void SetOverview(string overview)
        {
            TextOverview.text = overview;
            TextOverview.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }

        public void OnPlayPuzzle()
        {
            Program.instance.puzzle.StartCurrentPuzzle();
        }

        public void SelectLastPuzzleItem()
        {
            UserInput.NextSelectionIsAxis = true;
            Program.instance.puzzle.lastPuzzleItem.GetSelectable().Select();
        }

        public static Puzzle GetFirstPuzzle(PuzzleGroup puzzleGroup)
        {
            GetPuzzles(puzzleGroup);
            if (puzzles.Count == 0)
                return default;
            return puzzles[0];
        }

        public static List<Puzzle> GetRecommendPuzzles(int count)
        {
            var returnValue = new List<Puzzle>();
            var allPuzzles = GetAllPuzzles();

            if (returnValue.Count < count)
                for (int i = 0; i < allPuzzles.Count; i++)
                    if (!returnValue.Contains(allPuzzles[i]))
                    {
                        AddRecommendPuzzle(returnValue, allPuzzles[i]);
                        if (returnValue.Count >= count)
                            break;
                    }

            if (returnValue.Count < count)
                for (int i = 0; i < allPuzzles.Count; i++)
                    if (!returnValue.Contains(allPuzzles[i]))
                    {
                        AddNotClearPuzzle(returnValue, allPuzzles[i]);
                        if (returnValue.Count >= count)
                            break;
                    }

            if (returnValue.Count < count)
                for (int i = 0; i < allPuzzles.Count; i++)
                    if (!returnValue.Contains(allPuzzles[i]))
                    {
                        returnValue.Add(allPuzzles[i]);
                        if (returnValue.Count >= count)
                            break;
                    }

            return returnValue;
        }


        private static readonly List<Puzzle> allPuzzles = new();
        private static List<Puzzle> GetAllPuzzles()
        {
            if(allPuzzles.Count > 0)
                return allPuzzles;

            var puzzleGroup = new PuzzleGroup
            {
                pathPrefix = string.Empty,
                namePattern = string.Empty
            };
            GetPuzzles(puzzleGroup);
            allPuzzles.AddRange(puzzles);
            var folders = Directory.GetDirectories(Program.PATH_PUZZLE);
            foreach (var folder in folders)
            {
                string folderName = Path.GetFileName(folder) + "/";
                var pg = new PuzzleGroup
                {
                    pathPrefix = folderName,
                    namePattern = PuzzlePortalUI.GetPuzzleNamePattern(folderName)
                };
                GetPuzzles(pg);
                allPuzzles.AddRange(puzzles);
            }
            return allPuzzles;
        }

        private static void AddRecommendPuzzle(List<Puzzle> list, Puzzle puzzle)
        {
            var isNew = !Config.GetBool(puzzle.GetEnterConfigKey(), false);
            if (isNew) 
                list.Add(puzzle);
        }

        private static void AddNotClearPuzzle(List<Puzzle> list, Puzzle puzzle)
        {
            var cleared = Config.GetBool(puzzle.GetClearConfigKey(), false);
            if (!cleared)
                list.Add(puzzle);
        }

        public static Puzzle? GetLastPlayPuzzle()
        {
            try
            {
                var lastPlayPuzzleName = Config.Get("LastPlayPuzzle", Config.EMPTY_STRING);
                if (lastPlayPuzzleName != Config.EMPTY_STRING)
                {
                    var path = lastPlayPuzzleName + Program.EXPANSION_LUA;
                    if (File.Exists(path))
                    {
                        var count = path.Count(c => c == '/');
                        var prefix = string.Empty;
                        if(count == 0)
                            throw new Exception("Invalid puzzle path");
                        if(count == 2)
                            prefix = path.Substring(Program.PATH_PUZZLE.Length, path.LastIndexOf('/') - Program.PATH_PUZZLE.Length + 1);
                        var puzzleGroup = new PuzzleGroup
                        {
                            pathPrefix = prefix,
                            namePattern = PuzzlePortalUI.GetPuzzleNamePattern(prefix)
                        };
                        return GetPuzzle(path);
                    }
                }
            }
            catch {}
            return null;
        }

        public static bool GroupHasNew(PuzzleGroup puzzleGroup)
        {
            GetPuzzles(puzzleGroup);
            return puzzles.Any(p => !Config.GetBool(p.GetEnterConfigKey(), false));            
        }

        public static bool GroupAllClear(PuzzleGroup puzzleGroup)
        {
            GetPuzzles(puzzleGroup);
            return puzzles.All(p => Config.GetBool(p.GetClearConfigKey(), false));
        }
    }
}