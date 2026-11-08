using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;


namespace YuGiOhFight.Client.Controllers
{
    public class SoloSelector : Servant
    {

        public class BotInfo
        {
            public string name;
            public string command;
            public string desc;
            public string[] flags;
            public int main0;
        }
        public static readonly List<BotInfo> bots = new();

        private const string WINDBOT_DIALOG_PATH = "Data/Windbot/Dialogs/";
        private const string DECK_PREFIX = "Data/Windbot/Decks/AI_";
        private const int DEFAULT_CARD = 5990062;
        public static string port = "7911";

        public enum Condition
        {
            ForSolo,
            ForRoom
        }
        public static Condition condition = Condition.ForSolo;
        public void SwitchCondition(Condition condition)
        {
            SoloSelector.condition = condition;
            switch (condition)
            {
                case Condition.ForSolo:
                    returnServant = Runtime.Program.instance.menu;
                    break;
                case Condition.ForRoom:
                    returnServant = Runtime.Program.instance.room;
                    break;
            }

        }

        [HideInInspector] public UI.SelectionToggle_Solo lastSoloItem;

        private string lastSoloCommand = string.Empty;
        private int lastSoloPort = 7911;
        private int lastSoloLp = 8000;
        private int lastSoloHand = 5;
        private int lastSoloDraw = 1;
        private string lastSoloBanlist = MDPro3.Duel.YGOSharp.BanlistManager.EMPTY_LIST_NAME;
        private bool lastSoloLockHand;
        private bool lastSoloNoCheck;
        private bool lastSoloNoShuffle;
        private bool hasLastSoloLaunchConfig;
        private bool retryLastSoloDuelWhenShown;
        private int clientLaunchGeneration;

        #region Servant

        public override int Depth => 3;
        protected override bool ShowLine => false;

        public override void Initialize()
        {
            returnServant = Runtime.Program.instance.menu;
            base.Initialize();
            LoadBots();
        }

        protected override void ApplyShowArrangement(int preDepth)
        {
            base.ApplyShowArrangement(preDepth);
            if (retryLastSoloDuelWhenShown)
                StartCoroutine(RetryLastSoloDuelWhenReady());
        }

        protected override void FirstLoadEvent()
        {
            base.FirstLoadEvent();
            Print();
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !Runtime.UserInput.NeedDefaultSelect())
                return;

            if (servantUI == null)
                return;

            if (lastSoloItem != null)
                lastSoloItem.GetSelectable().Select();
            else
                servantUI.SelectDefaultSelectable();
        }

        #endregion

        #region Solo

        public void LoadBots()
        {
            ReadBots(Runtime.Program.PATH_LOCALES + MDPro3.Utility.Language.GetConfig() + "/bot.conf");
            Print();
        }

        private void ReadBots(string confPath)
        {
            bots.Clear();
            StreamReader reader = new(new FileStream(confPath, FileMode.Open, FileAccess.Read));
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine().Trim();
                if (line.Length > 0 && line[0] == '!')
                {
                    BotInfo newBot = new()
                    {
                        name = line.TrimStart('!'),
                        command = reader.ReadLine().Trim(),
                        desc = reader.ReadLine().Trim()
                    };
                    line = reader.ReadLine().Trim();
                    newBot.flags = line.Split(' ');

                    newBot.main0 = DEFAULT_CARD;
                    try
                    {
                        string deckName = string.Empty;
                        deckName = newBot.command.Split(new string[] { "Deck=", " Dialog=" }
                        , StringSplitOptions.RemoveEmptyEntries)[1].Replace("'", string.Empty).Replace(" ", string.Empty);
                        if (File.Exists(DECK_PREFIX + deckName + Runtime.Program.EXPANSION_YDK))
                        {
                            MDPro3.Duel.YGOSharp.Deck aiDeck = new MDPro3.Duel.YGOSharp.Deck(DECK_PREFIX + deckName + Runtime.Program.EXPANSION_YDK);
                            if (aiDeck.Main.Count > 0)
                                newBot.main0 = aiDeck.Main[0];
                        }
                    }
                    catch { }

                    bots.Add(newBot);
                }
            }
        }

        private void Print()
        {
            if (servantUI == null)
                return;

            GetUI<UI.ServantUI.SoloSelectorUI>().Print();
        }

        private string GetWindBotCommand(int aiCode, bool diyDeck)
        {
            BotInfo bot = bots[aiCode];
            string aiCommand = bot.command;
            if (diyDeck)
            {
                string selectedDeck = GetUI<UI.ServantUI.SoloSelectorUI>().GetAIDeck();
                if (!File.Exists(Runtime.Program.PATH_DECK + selectedDeck + Runtime.Program.EXPANSION_YDK))
                {
                    Runtime.MessageManager.Cast(MDPro3.InterString.Get("请先为AI选择有效的卡组。"));
                    return string.Empty;
                }
                aiCommand += " DeckFile=\"" + selectedDeck + "\"";
            }
            Match match = Regex.Match(aiCommand, "Random=(\\w+)");
            if (match.Success)
            {
                string randomFlag = match.Groups[1].Value;
                string command = GetRandomBot(randomFlag);
                if (command != string.Empty)
                    aiCommand = command;
            }
            return aiCommand;
        }

        public void StartAIForSolo(int aiCode, bool diyDeck)
        {
            string aiCommand = GetWindBotCommand(aiCode, diyDeck);
            if (!string.IsNullOrEmpty(aiCommand))
            {
                UI.ServantUI.SoloSelectorUI ui = GetUI<UI.ServantUI.SoloSelectorUI>();
                var lockHand = ui.IsLockHand();
                var noCheck = ui.IsNoCheck();
                var noShuffle = ui.IsNoShuffle();
                var duelPort = ui.GetPort();
                var duelLp = ui.GetLP();
                var duelHand = ui.GetHand();
                var duelDraw = ui.GetDraw();

                RememberLastSoloLaunch(aiCommand, duelPort, duelLp, duelHand, duelDraw, lockHand, noCheck, noShuffle);
                LaunchWithConfig(aiCommand, duelPort, duelLp, duelHand, duelDraw, lockHand, noCheck, noShuffle);
            }
        }

        // The client passes an explicit preparation snapshot to the CPU launch.
        public void StartClientDuel(int aiCode, string customDeck, int duelPort, int duelLp, int duelHand, int duelDraw,
            bool lockHand, bool noCheck, bool noShuffle)
            => StartClientDuel(aiCode, customDeck, duelPort, duelLp, duelHand, duelDraw, lockHand, noCheck, noShuffle, MDPro3.Duel.YGOSharp.BanlistManager.EMPTY_LIST_NAME);

        public void StartClientDuel(int aiCode, string customDeck, int duelPort, int duelLp, int duelHand, int duelDraw,
            bool lockHand, bool noCheck, bool noShuffle, string banlistName)
        {
            MDPro3.Duel.YGOSharp.Banlist banlist = new MDPro3.AI.ResponsesDuelOptions { BanlistName = banlistName }.ResolveBanlist();
            if (aiCode < 0 || aiCode >= bots.Count) throw new ArgumentOutOfRangeException(nameof(aiCode));
            if (duelPort < 1 || duelPort > 65535) throw new ArgumentException("本地端口应在 1～65535 之间。");
            var command = bots[aiCode].command;
            if (command.Contains("Lucky"))
            {
                if (string.IsNullOrWhiteSpace(customDeck)) throw new ArgumentException("请先选择电脑卡组。");
                var customPath = Path.GetFullPath(Runtime.Program.PATH_DECK + customDeck + Runtime.Program.EXPANSION_YDK);
                if (!File.Exists(customPath)) throw new FileNotFoundException("电脑卡组不存在。");
                if (customPath.Contains("\"")) throw new ArgumentException("电脑卡组文件名不能包含引号。");
                MDPro3.Duel.YGOSharp.Deck custom = new MDPro3.Duel.YGOSharp.Deck(customPath);
                if (custom.Main.Count == 0 || custom.Main.Count > 60 || custom.Extra.Count > 15 || custom.Side.Count > 15)
                    throw new ArgumentException("电脑卡组超过支持的卡数范围。");
                if (!noCheck && custom.Check(banlist ?? MDPro3.Duel.YGOSharp.BanlistManager.GetByName(MDPro3.Duel.YGOSharp.BanlistManager.EMPTY_LIST_NAME), true, true) != 0)
                    throw new ArgumentException("电脑卡组不符合所选限卡表或构筑规则。");
                command += " DeckFile=\"" + customPath + "\"";
            }
            var match = Regex.Match(command, "Random=(\\w+)");
            if (match.Success) command = GetRandomBot(match.Groups[1].Value);
            if (string.IsNullOrEmpty(command)) throw new InvalidOperationException("未找到可用电脑算法。");
            if (!Runtime.Network.TcpHelper.IsPortAvailable(duelPort)) throw new InvalidOperationException("本地端口被占用，请在通用设置中更换端口。");
            if (Runtime.Network.TcpHelper.tcpClient != null && Runtime.Network.TcpHelper.tcpClient.Connected)
                throw new InvalidOperationException("已有对战连接，请先退出当前对战。");
            var playerDeckPath = Runtime.Program.PATH_DECK + MDPro3.Config.GetConfigDeckName() + Runtime.Program.EXPANSION_YDK;
            if (!File.Exists(playerDeckPath)) throw new FileNotFoundException("玩家卡组不存在。");
            MDPro3.Duel.YGOSharp.Deck playerDeck = new MDPro3.Duel.YGOSharp.Deck(playerDeckPath);
            if (!noCheck && playerDeck.Check(banlist ?? MDPro3.Duel.YGOSharp.BanlistManager.GetByName(MDPro3.Duel.YGOSharp.BanlistManager.EMPTY_LIST_NAME), true, true) != 0)
                throw new ArgumentException("玩家卡组不符合所选限卡表或构筑规则。");
            RememberLastSoloLaunch(command, duelPort, duelLp, duelHand, duelDraw, lockHand, noCheck, noShuffle, banlistName);
            // A transport connection only queues JoinGame. Wait for the actual
            // server-assigned host seat before allowing WindBot to join.
            RoomServant.SelfType = -1; RoomServant.IsHost = false;
            LaunchWithConfig(command, duelPort, duelLp, duelHand, duelDraw, lockHand, noCheck, noShuffle, true, banlistName);
            StartCoroutine(PrepareClientRoom(clientLaunchGeneration, duelPort, playerDeck, command, lockHand));
        }

        // The preparation page owns the Ready/Start controls for CPU duels.
        // Send the same protocol messages after both real local seats are ready;
        // the native game still owns rock-paper-scissors and turn-order selection.
        private IEnumerator PrepareClientRoom(int generation, int duelPort, MDPro3.Duel.YGOSharp.Deck playerDeck, string command, bool lockHand)
        {
            float deadline = Time.realtimeSinceStartup + 20;
            string expectedPort = duelPort.ToString();
            bool readySent = false, botStarted = false;
            System.Net.Sockets.TcpClient localClient = null;
            while (generation == clientLaunchGeneration && Time.realtimeSinceStartup < deadline)
            {
                if (!RoomServant.FromSolo || !MDPro3.Net.YgoServer.ServerRunning()) yield break;
                var transport = Runtime.Network.TcpHelper.tcpClient;
                bool connected = transport != null && transport.Connected;
                bool joined = connected && Runtime.Network.TcpHelper.joinedAddress == "127.0.0.1" && Runtime.Network.TcpHelper.joinedPort == expectedPort;
                // A departed launch must never prepare or stop a later socket.
                if (localClient != null && transport != null && !ReferenceEquals(localClient, transport)) yield break;
                if (localClient != null && connected && !joined) yield break;
                if (joined && Runtime.Program.instance.currentServant == Runtime.Program.instance.ocgcore) yield break;
                if (localClient != null && !joined) break;
                if (joined && localClient == null) localClient = transport;
                if (joined
                    && Runtime.Program.instance.currentServant == Runtime.Program.instance.room && RoomServant.FromSolo
                    && RoomServant.SelfType == 0 && RoomServant.IsHost
                    && RoomServant.players[RoomServant.SelfType] != null)
                {
                    localClient = transport;
                    if (!botStarted)
                    {
                        StartWindBot(command, "127.0.0.1", expectedPort, string.Empty, lockHand, 0);
                        botStarted = true;
                    }
                    if (!readySent)
                    {
                        Runtime.Network.TcpHelper.CtosMessage_UpdateDeck(playerDeck);
                        Runtime.Network.TcpHelper.CtosMessage_HsReady();
                        readySent = true;
                    }
                    if (RoomServant.players[0] != null && RoomServant.players[1] != null
                        && RoomServant.players[0].ready && RoomServant.players[1].ready)
                    {
                        Runtime.Network.TcpHelper.CtosMessage_HsStart();
                        yield break;
                    }
                }
                yield return null;
            }
            if (generation != clientLaunchGeneration || !RoomServant.FromSolo || !MDPro3.Net.YgoServer.ServerRunning()) yield break;
            var currentTransport = Runtime.Network.TcpHelper.tcpClient;
            if (currentTransport != null && currentTransport.Connected)
            {
                if (Runtime.Network.TcpHelper.joinedAddress != "127.0.0.1" || Runtime.Network.TcpHelper.joinedPort != expectedPort
                    || localClient != null && !ReferenceEquals(localClient, currentTransport)) yield break;
                if (Runtime.Program.instance.currentServant == Runtime.Program.instance.ocgcore) yield break;
            }
            MDPro3.Net.YgoServer.StopServer();
            Runtime.Network.TcpHelper.DisconnectIntentional();
            Runtime.MessageManager.Cast(MDPro3.InterString.Get("本地电脑对手连接超时，请重试。"));
        }

        public void StartAIForRoom(int aiCode, bool diyDeck)
        {
            string aiCommand = GetWindBotCommand(aiCode, diyDeck);
            if (!string.IsNullOrEmpty(aiCommand))
            {
                StartWindBot(aiCommand, Runtime.Network.TcpHelper.joinedAddress, Runtime.Network.TcpHelper.joinedPort, Runtime.Network.TcpHelper.joinedPassword, GetUI<UI.ServantUI.SoloSelectorUI>().IsLockHand(), 600);
                Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.room);
            }
        }

        public void StartAIForHandTest(int port)
        {
            string aiCommand = GetWindBotCommand(0, false);
            if (!string.IsNullOrEmpty(aiCommand))
                StartWindBot(aiCommand, "127.0.0.1", port.ToString(), string.Empty, true, 0);
        }

        private string GetRandomBot(string flag)
        {
            IList<BotInfo> foundBots = new List<BotInfo>();
            foreach (BotInfo bot in bots)
            {
                if (Array.IndexOf(bot.flags, flag) >= 0) foundBots.Add(bot);
            }
            if (foundBots.Count > 0)
            {
                System.Random rand = new System.Random();
                BotInfo bot = foundBots[rand.Next(foundBots.Count)];
                return bot.command;
            }
            return "";
        }
        public void StartWindBot(string command, string ip, string port, string password, bool lockHand, int delay)
        {
            command = command.Replace("'", "\"");
            if (lockHand)
                command += " Hand=1";
            command += " Host=" + ip;
            command += " Port=" + port;
            command += " HostInfo=" + password;

            var args = Utility.Tools.SplitWithPreservedQuotes(command);
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("Dialog="))
                {
                    var path = args[i][7..];
                    if (!File.Exists(WINDBOT_DIALOG_PATH + path + ".json"))
                    {
                        var config = MDPro3.Utility.Language.GetConfig();
                        if (config == "en-US")//WindBot use en-US as default;
                            config = "default";
                        args[i] = "Dialog=" + config;
                    }
                    break;
                }
            }

            (new Thread(() => { Thread.Sleep(delay); WindBot.Program.Main(args); })).Start();
        }
        public void Launch(string command, bool lockHand, bool noCheck, bool noShuffle)
        {
            UI.ServantUI.SoloSelectorUI ui = GetUI<UI.ServantUI.SoloSelectorUI>();
            LaunchWithConfig(command, ui.GetPort(), ui.GetLP(), ui.GetHand(), ui.GetDraw(), lockHand, noCheck, noShuffle);
        }

        public bool CanRetryLastSoloDuel()
        {
            return hasLastSoloLaunchConfig;
        }

        public void QueueRetryLastSoloDuel()
        {
            if (!CanRetryLastSoloDuel())
                return;
            retryLastSoloDuelWhenShown = true;
        }

        private void RememberLastSoloLaunch(string command, int duelPort, int duelLp, int duelHand, int duelDraw
            , bool lockHand, bool noCheck, bool noShuffle, string banlistName = MDPro3.Duel.YGOSharp.BanlistManager.EMPTY_LIST_NAME)
        {
            lastSoloCommand = command;
            lastSoloPort = duelPort;
            lastSoloLp = duelLp;
            lastSoloHand = duelHand;
            lastSoloDraw = duelDraw;
            lastSoloLockHand = lockHand;
            lastSoloNoCheck = noCheck;
            lastSoloNoShuffle = noShuffle;
            lastSoloBanlist = banlistName;
            hasLastSoloLaunchConfig = true;
        }

        private IEnumerator RetryLastSoloDuelWhenReady()
        {
            yield return null;
            while (showing && inTransition)
                yield return null;

            if (!showing || !retryLastSoloDuelWhenShown || !hasLastSoloLaunchConfig)
                yield break;

            retryLastSoloDuelWhenShown = false;
            LaunchWithConfig(lastSoloCommand, lastSoloPort, lastSoloLp, lastSoloHand, lastSoloDraw
                , lastSoloLockHand, lastSoloNoCheck, lastSoloNoShuffle, false, lastSoloBanlist);
        }

        private void LaunchWithConfig(string command, int duelPort, int duelLp, int duelHand, int duelDraw
            , bool lockHand, bool noCheck, bool noShuffle)
            => LaunchWithConfig(command, duelPort, duelLp, duelHand, duelDraw, lockHand, noCheck, noShuffle, false);

        private void LaunchWithConfig(string command, int duelPort, int duelLp, int duelHand, int duelDraw
            , bool lockHand, bool noCheck, bool noShuffle, bool deferClientBot, string banlistName = MDPro3.Duel.YGOSharp.BanlistManager.EMPTY_LIST_NAME)
        {
            ++clientLaunchGeneration;
            port = duelPort.ToString();

            string lp = duelLp.ToString();
            string hand = duelHand.ToString();
            string draw = duelDraw.ToString();
            MDPro3.Duel.YGOSharp.Banlist banlist = new MDPro3.AI.ResponsesDuelOptions { BanlistName = banlistName }.ResolveBanlist();
            int listIndex = banlist == null ? -1 : MDPro3.Duel.YGOSharp.BanlistManager.GetIndexByName(banlistName);
            string args = port + " " + listIndex + " 5 0 F " + (noCheck ? "T " : "F ") + (noShuffle ? "T " : "F ") + lp + " " + hand + " " + draw + " 0 0";
            if (Runtime.Network.TcpHelper.IsPortAvailable(duelPort))
            {
                MDPro3.Net.YgoServer.StartServer(args);
                RoomServant.FromSolo = true;
                if (lockHand)
                    RoomServant.SoloLockHand = true;
                else
                    RoomServant.SoloLockHand = false;
                RoomServant.FromLocalHost = false;
                RoomServant.FromHandTest = false;

                Runtime.Network.TcpHelper.LinkStart("127.0.0.1", MDPro3.Config.Get("DuelPlayerName0", MDPro3.Config.EMPTY_STRING), port, string.Empty, true
                    , () => { if (!deferClientBot) StartWindBot(command, "127.0.0.1", port, string.Empty, lockHand, 0); });
            }
            else
            {
                Runtime.MessageManager.messageFromSubString
                    = MDPro3.InterString.Get("端口被占用， 请尝试修改端口后再尝试。端口号应大于0，小于65535。");
            }
        }

        #endregion
    }
}
