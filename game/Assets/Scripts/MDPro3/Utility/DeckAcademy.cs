using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using MDPro3.UI;
using MDPro3.UI.ServantUI;
using Newtonsoft.Json;

namespace MDPro3.Utility
{
    public class DeckAcademyStep
    {
        public string title;
        public string text;
        public int[] card_codes;
        public int? replay_frame;
    }

    public class DeckAcademyBoard
    {
        public string text;
        public int[] codes;
    }

    public class DeckAcademyDeck
    {
        public string id;
        public string name;
        public string deck_file;
        public string description;
        public string regulation;
        public int[] starter_hand;
        public int[] starter_cards;
        public DeckAcademyStep[] steps;
        public DeckAcademyBoard end_board;
        public string[] cautions;
        public string replay_file;
        public string category;
        public string difficulty;
        public string[] tags;
    }

    public class DeckAcademyCatalog
    {
        public int schema;
        public List<DeckAcademyDeck> decks;
        public Dictionary<string, string> files;
        public DeckLibraryCatalog library;
    }

    public static class DeckAcademy
    {
        public const string ContentPath = "Data/DeckAcademy/";
        public static DeckAcademyDeck ActiveDeck { get; private set; }
        public static int ActiveTutorialPage { get; set; } = -1;
        private static string activePuzzle;
        private static string activeReplayName;
        private static bool activeReplay;
        private static bool singleReplayStep;
        private static int replayFrameCount;
        public static int ReplayFrame { get; private set; } = -1;
        public static bool ReplayFinished { get; private set; }
        public static bool FollowReplayGuide { get; private set; }
        public static bool ReplayStepRequested => singleReplayStep;

        public static DeckAcademyCatalog Load()
        {
            var catalog = JsonConvert.DeserializeObject<DeckAcademyCatalog>(File.ReadAllText(ContentPath + "manifest.json"));
            if (catalog == null || catalog.schema != 1 || catalog.decks == null || catalog.decks.Count == 0)
                throw new InvalidDataException("卡组学院目录格式无效。");
            var ids = new HashSet<string>();
            foreach (var entry in catalog.decks)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.name) || entry.id == null
                    || !Regex.IsMatch(entry.id, "^[a-z0-9_-]{1,64}$") || !ids.Add(entry.id)
                    || entry.steps == null || entry.steps.Length == 0
                    || entry.starter_hand == null || entry.starter_hand.Length != 5)
                    throw new InvalidDataException("卡组或展开教程条目无效。");
                var deck = Template(entry);
                if (deck.Main.Count < 40 || deck.Main.Count > 60 || deck.Extra.Count > 15 || deck.Side.Count > 15)
                    throw new InvalidDataException("卡组数量不符合规则：" + entry.name);
            }
            foreach (var entry in catalog.decks)
            {
                var recommendation = catalog.library?.decks?.FirstOrDefault(record => record.academy_id == entry.id
                    && (record.source_file == entry.deck_file || !string.IsNullOrWhiteSpace(record.tutorial_name)));
                var title = recommendation?.source_file == entry.deck_file ? recommendation.name : recommendation?.tutorial_name;
                if (!string.IsNullOrWhiteSpace(title))
                    entry.name = title;
            }
            return catalog;
        }

        public static string TemplatePath(DeckAcademyDeck entry)
        {
            if (entry.deck_file == null || !entry.deck_file.StartsWith("Decks/", StringComparison.Ordinal)
                || !entry.deck_file.EndsWith(".ydk", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("无效的卡组模板路径。");
            var root = Path.GetFullPath(ContentPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var path = Path.GetFullPath(Path.Combine(root, entry.deck_file));
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidDataException("卡组模板路径超出内容目录。");
            return path;
        }

        public static Deck Template(DeckAcademyDeck entry) => new Deck(TemplatePath(entry));

        public static string CardName(int code)
        {
            return CardsManager.GetCardRaw(code)?.Name ?? code.ToString();
        }

        public static string CardNames(IEnumerable<int> codes)
        {
            return codes == null ? string.Empty : string.Join("、", codes.Select(CardName));
        }

        public static string Category(DeckAcademyDeck entry) =>
            string.IsNullOrWhiteSpace(entry.category) ? "当前精选" : entry.category;

        public static bool Matches(DeckAcademyDeck entry, string query, string category, string cardNames = null)
        {
            if (!string.IsNullOrEmpty(category) && Category(entry) != category)
                return false;
            if (string.IsNullOrWhiteSpace(query))
                return true;
            var text = string.Join(" ", new[] { entry.id, entry.name, entry.description, Category(entry),
                entry.difficulty, string.Join(" ", entry.tags ?? Array.Empty<string>()), cardNames });
            return query.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .All(term => text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static DeckAcademyDeck FindCurrent(DeckAcademyCatalog catalog)
        {
            var name = DeckEditor.DeckName ?? string.Empty;
            var recommendation = catalog.library?.decks?.Where(record => record != null
                && !string.IsNullOrEmpty(record.academy_id) && !string.IsNullOrEmpty(record.name)
                && name.StartsWith(record.name, StringComparison.Ordinal))
                .OrderByDescending(record => record.name.Length).FirstOrDefault();
            if (recommendation != null)
            {
                var lesson = catalog.decks.FirstOrDefault(entry => entry.id == recommendation.academy_id);
                if (lesson != null) return lesson;
            }
            return catalog.decks.FirstOrDefault(entry => name.StartsWith(Path.GetFileNameWithoutExtension(entry.deck_file), StringComparison.Ordinal))
                ?? catalog.decks.FirstOrDefault(entry => name.Contains(entry.name));
        }

        public static bool CanLeaveEditor()
        {
            if (Program.instance.currentServant != Program.instance.deckEditor)
                return true;
            var ui = Program.instance.deckEditor.GetUI<DeckEditorUI>();
            if (ui == null || !ui.DeckView.deckLoaded)
            {
                MessageManager.Toast("卡组仍在加载，请稍后再导入卡组或启动练习。");
                return false;
            }
            if (ui.DeckView.GetDirty())
            {
                MessageManager.Toast("请先保存当前卡组，再导入卡组或启动练习。");
                return false;
            }
            return true;
        }

        public static void ImportAndEdit(DeckAcademyDeck entry)
        {
            if (!CanLeaveEditor())
                return;
            var name = CreateEditableCopy(entry);
            if (Program.instance.currentServant == Program.instance.deckEditor)
                Program.instance.deckEditor.GetUI<DeckEditorUI>().LoadAcademyDeck(name);
            else
            {
                Config.SetConfigDeck(name);
                Program.instance.deckEditor.SwitchCondition(DeckEditor.Condition.EditDeck);
                Program.instance.ShiftToServant(Program.instance.deckEditor);
            }
            MessageManager.Toast("已导入独立卡组副本：" + name);
        }

        public static string CreateEditableCopy(DeckAcademyDeck entry)
        {
            return DeckLibrary.CreateEditableCopy(File.ReadAllBytes(TemplatePath(entry)), entry.name);
        }

        public static bool CanPractice(DeckAcademyDeck entry) => File.Exists(PuzzlePath(entry) + Program.EXPANSION_LUA);
        public static string PuzzlePath(DeckAcademyDeck entry) => Program.PATH_PUZZLE + "Academy/" + entry.id;
        public static bool CanReplay(DeckAcademyDeck entry) => !string.IsNullOrEmpty(entry.replay_file)
            && File.Exists(Program.PATH_REPLAY + "Academy/" + entry.id + Program.EXPANSION_YRP3D);

        public static bool HasActivePractice => ActiveDeck != null && !activeReplay && OcgCore.inPuzzle
            && Program.instance != null && Program.instance.currentServant == Program.instance.ocgcore
            && OcgCore.condition == OcgCore.Condition.Duel
            && Config.Get("LastPlayPuzzle", string.Empty) == activePuzzle;

        public static bool HasActiveReplay => ActiveDeck != null && activeReplay
            && Program.instance != null && Program.instance.currentServant == Program.instance.ocgcore
            && OcgCore.condition == OcgCore.Condition.Replay;
        public static bool HasActiveGuide => HasActivePractice || HasActiveReplay;
        public static bool CanFollowReplayGuide => HasActiveReplay
            && HasReplayTimeline(ActiveDeck, replayFrameCount);

        public static bool HasReplayTimeline(DeckAcademyDeck entry, int totalFrames = 0)
        {
            if (entry?.steps == null || entry.steps.Length == 0)
                return false;
            var previous = -1;
            foreach (var step in entry.steps)
            {
                if (step?.replay_frame == null || step.replay_frame.Value <= previous
                    || (totalFrames > 0 && step.replay_frame.Value >= totalFrames))
                    return false;
                previous = step.replay_frame.Value;
            }
            return true;
        }

        public static int ReplayGuidePage(DeckAcademyDeck entry, int frame)
        {
            if (!HasReplayTimeline(entry))
                return 0;
            var page = 0;
            for (var index = 0; index < entry.steps.Length; index++)
            {
                if (entry.steps[index].replay_frame.Value > frame)
                    break;
                page = index;
            }
            return page;
        }

        public static void SetGuidePage(int page, bool manual = true)
        {
            if (ActiveDeck == null)
                return;
            ActiveTutorialPage = Math.Max(-1, Math.Min(page, ActiveDeck.steps.Length));
            if (manual && activeReplay)
                FollowReplayGuide = false;
        }

        public static void FollowCurrentReplay()
        {
            if (!CanFollowReplayGuide)
                return;
            FollowReplayGuide = true;
            ActiveTutorialPage = ReplayFinished ? ActiveDeck.steps.Length : ReplayGuidePage(ActiveDeck, ReplayFrame);
        }

        public static void NotifyReplayStarted(string name)
        {
            activeReplay = ActiveDeck != null && name == activeReplayName;
            ReplayFrame = -1;
            replayFrameCount = 0;
            ReplayFinished = false;
            singleReplayStep = false;
            FollowReplayGuide = activeReplay && HasReplayTimeline(ActiveDeck);
            if (activeReplay)
                ActiveTutorialPage = 0;
        }

        public static void CancelReplayStep() => singleReplayStep = false;

        public static void StepReplayEvent()
        {
            if (!HasActiveReplay || ReplayFinished)
                return;
            Program.instance.ocgcore.GetUI<OcgCoreUI>().OnPlay();
            singleReplayStep = true;
        }

        public static void NotifyReplayPacketProcessed(int message, int consumed, int total, bool deferCompletion = false)
        {
            if (!HasActiveReplay)
                return;
            var frame = Math.Max(-1, consumed - 1);
            if (frame < ReplayFrame)
                singleReplayStep = false;
            ReplayFrame = frame;
            replayFrameCount = total;
            ReplayFinished = total > 0 && consumed >= total && !deferCompletion;
            if (!HasReplayTimeline(ActiveDeck, total))
                FollowReplayGuide = false;
            if (FollowReplayGuide)
                ActiveTutorialPage = ReplayFinished ? ActiveDeck.steps.Length : ReplayGuidePage(ActiveDeck, frame);
            if (singleReplayStep && message != (int)GameMessage.UpdateCard
                && message != (int)GameMessage.UpdateData && (message < 200 || ReplayFinished))
            {
                singleReplayStep = false;
                Program.instance.ocgcore.GetUI<OcgCoreUI>().OnStop();
            }
        }

        public static void CompleteReplay()
        {
            if (!HasActiveReplay)
                return;
            ReplayFinished = true;
            singleReplayStep = false;
            if (FollowReplayGuide)
                ActiveTutorialPage = ActiveDeck.steps.Length;
        }

        public static void StartPractice(DeckAcademyDeck entry)
        {
            if (!CanLeaveEditor())
                return;
            if (!CanPractice(entry))
                throw new FileNotFoundException("固定起手练习尚未安装：" + entry.name);
            var origin = Program.instance.currentServant;
            var previousFlag = DeckEditor.ToHandTest;
            var previousRoomFlag = RoomServant.FromHandTest;
            ActiveDeck = entry;
            activeReplay = false;
            activeReplayName = null;
            ActiveTutorialPage = 0;
            activePuzzle = PuzzlePath(entry);
            RoomServant.FromHandTest = origin == Program.instance.deckEditor;
            DeckEditor.ToHandTest = true;
            try
            {
                Program.instance.puzzle.StartPuzzle(activePuzzle);
                if (Program.instance.currentServant == Program.instance.ocgcore)
                    Program.instance.ocgcore.returnServant = origin;
                else
                {
                    ActiveDeck = null;
                    RoomServant.FromHandTest = previousRoomFlag;
                }
            }
            finally { DeckEditor.ToHandTest = previousFlag; }
        }

        public static void PlayReplay(DeckAcademyDeck entry)
        {
            if (!CanLeaveEditor())
                return;
            if (!CanReplay(entry))
                throw new FileNotFoundException("展开演示回放尚未安装：" + entry.name);
            _ = PlayReplayAsync(entry);
        }

        private static async UniTask PlayReplayAsync(DeckAcademyDeck entry)
        {
            var origin = Program.instance.currentServant;
            var previousRoomFlag = RoomServant.FromHandTest;
            RoomServant.FromHandTest = origin == Program.instance.deckEditor;
            ActiveDeck = entry;
            activeReplayName = "Academy/" + entry.id;
            activeReplay = true;
            ActiveTutorialPage = 0;
            ReplayFrame = -1;
            ReplayFinished = false;
            FollowReplayGuide = HasReplayTimeline(entry);
            try
            {
                if (Program.instance.replay.servantUI == null)
                {
                    Program.instance.replay.PlayReplay("Academy/" + entry.id);
                    for (var attempt = 0; attempt < 100 && Program.instance.replay.servantUI == null; attempt++)
                        await UniTask.Delay(50);
                    if (Program.instance.replay.servantUI == null)
                        throw new InvalidOperationException("回放界面加载失败。");
                    for (var attempt = 0; attempt < 100 && Program.instance.replay.inTransition; attempt++)
                        await UniTask.Delay(50);
                    if (Program.instance.currentServant != Program.instance.replay
                        && !(Program.exitOnReturn && Program.instance.currentServant == Program.instance.ocgcore))
                    {
                        activeReplay = false;
                        RoomServant.FromHandTest = previousRoomFlag;
                        return;
                    }
                    if (Program.instance.replay.inTransition && Program.instance.currentServant == Program.instance.replay)
                        throw new InvalidOperationException("回放界面加载超时。");
                }
                if (Program.instance.currentServant != Program.instance.ocgcore)
                    Program.instance.replay.GetUI<ReplaySelectorUI>().KF_Replay("Academy/" + entry.id);
                if (Program.instance.currentServant == Program.instance.ocgcore)
                    Program.instance.ocgcore.returnServant = origin;
                else
                {
                    activeReplay = false;
                    RoomServant.FromHandTest = previousRoomFlag;
                }
            }
            catch (Exception error)
            {
                activeReplay = false;
                RoomServant.FromHandTest = previousRoomFlag;
                MessageManager.Cast("展开演示启动失败：" + error.Message);
            }
        }
    }
}
