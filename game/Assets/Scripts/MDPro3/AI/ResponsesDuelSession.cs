using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MDPro3.Duel.YGOSharp;
using MDPro3.Net;
using MDPro3.Servant;
using MDPro3.UI;
using MDPro3.UI.ServantUI;
using UnityEngine;
using WindBot.Game;
using GameDeck = MDPro3.Duel.YGOSharp.Deck;

namespace MDPro3.AI
{
    /// <summary>Owns only the local model duel and its cancellable bot workers.</summary>
    public static class ResponsesDuelSession
    {
        private sealed class Session
        {
            public readonly CancellationTokenSource Cancellation = new();
            public readonly List<GameClient> Clients = new();
            public readonly List<Thread> Workers = new();
            public readonly ConcurrentQueue<string> Messages = new();
            public readonly ConcurrentQueue<Action> Events = new();
            public readonly ConcurrentQueue<Package> ViewerMessages = new();
            public readonly HashSet<string> Results = new();
            public ResponsesAiConfig Config;
            public GameDeck PlayerDeck;
            public string DeckA, DeckB, TempDirectory;
            public int Port;
            public bool AiVsAi, ViewerJoined, ObserverRequested, PlayerReady, OpeningChatCleared;
            public volatile bool AllowStart, Initialized;
            public string Failure;
            public long EndedTicks;
            public bool ViewerWinQueued;
            public long ViewerDisconnectedTicks;
            public int Decisions;
            public void Fail(string message)
            {
                lock (Results)
                {
                    // A request can finish aborting after the native core has
                    // already delivered Win. Preserve failures that preceded
                    // the result, but do not turn shutdown into a new failure.
                    if (Cancellation.IsCancellationRequested || Interlocked.Read(ref EndedTicks) != 0) return;
                    Interlocked.CompareExchange(ref Failure, message, null);
                }
            }
        }

        private static Session current;
        private static ResponsesDuelOverlay overlay;
        public static bool IsActive => current != null;
        public static bool UsesAiAPerspective => current?.AiVsAi == true;
        public static string Status { get; private set; } = string.Empty;
        public static string FailureReason { get; private set; } = string.Empty;
        public static int NativeWinReason { get; private set; } = -1;
        public static int DecisionCount => current == null ? lastDecisionCount : current.Decisions;
        private static int lastDecisionCount;
        public static event Action<string> StatusChanged;
        public static event Action<string, string, string> DecisionMade;
        public static event Action<string, int> DuelEnded;

        public static void Start(ResponsesAiConfig config, bool aiVsAi, string deckA, string deckB)
        {
            config.Validate();
            var first = LoadDeck(deckA);
            var second = LoadDeck(deckB);
            if (Program.instance == null || !TcpHelper.canJoin)
                throw new InvalidOperationException("请等待当前连接结束后再开始 AI 对战。");
            Stop();
            YgoServer.StopServer();
            Program.instance.ocgcore.CloseConnection();
            var session = new Session
            {
                Config = config.Clone(), AiVsAi = aiVsAi, PlayerDeck = first,
                Port = FindPort(), TempDirectory = Path.Combine(Path.GetTempPath(), "mdpro3-ai-" + Guid.NewGuid().ToString("N"))
            };
            Directory.CreateDirectory(session.TempDirectory);
            // The normal WindBot YDK reader also reads cosmetic metadata as cards.
            // Feed it a normalized duel-only copy without touching the user's deck.
            session.DeckA = SaveDuelDeck(first, Path.Combine(session.TempDirectory, "A.ydk"));
            session.DeckB = SaveDuelDeck(second, Path.Combine(session.TempDirectory, "B.ydk"));
            lastDecisionCount = 0;
            FailureReason = string.Empty;
            NativeWinReason = -1;
            SetStatus("正在准备 " + (aiVsAi ? "AI 对 AI" : "玩家对 AI") + "…");
            RoomServant.FromSolo = true;
            RoomServant.FromHandTest = false;
            RoomServant.FromLocalHost = false;
            RoomServant.SoloLockHand = false;
            RoomServant.JoinWithReconnect = false;
            RoomServant.NeedSide = false;
            Program.instance.room.duelEnded = false;
            SoloSelector.port = session.Port.ToString();
            // Single duel, current master rule, full OCG/TCG card pool, 40–60 cards.
            // No turn clock: model latency must not lose the duel automatically.
            YgoServer.StartServer(session.Port + " -1 5 0 F F F 8000 5 1 0 0");
            current = session;
            var bootstrap = new Thread(() =>
            {
                try
                {
                    WindBot.Program.InitDatas("cards.cdb");
                    session.Initialized = true;
                    session.Messages.Enqueue("卡库已就绪，正在连接 AI…");
                }
                catch { session.Fail("AI 卡片数据库初始化失败，请检查本地卡库。"); }
            }) { IsBackground = true, Name = "Responses AI database" };
            session.Workers.Add(bootstrap);
            bootstrap.Start();
            Program.instance.StartCoroutine(Launch(session));
        }

        private static IEnumerator Launch(Session session)
        {
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (current == session && session.Failure == null
                && !session.Initialized)
            {
                if (DateTime.UtcNow > deadline) { session.Fail("本地 AI 对战服务启动超时。"); yield break; }
                yield return null;
            }
            if (current != session || session.Failure != null) yield break;
            // A native listener using SO_REUSEADDR can permit a separate bind to
            // loopback on macOS. Binding a probe is not a reliable ready check.
            yield return new WaitForSecondsRealtime(0.25f);
            if (current != session || session.Failure != null || session.Cancellation.IsCancellationRequested) yield break;
            var host = StartBot(session, session.AiVsAi ? "AI A" : "AI", session.AiVsAi ? session.DeckA : session.DeckB);
            deadline = DateTime.UtcNow.AddSeconds(15);
            while (current == session && session.Failure == null && !host.ResponsesJoined)
            {
                if (DateTime.UtcNow > deadline) { session.Fail("AI 未能加入本地对战。"); yield break; }
                yield return null;
            }
            if (current != session || session.Failure != null) yield break;
            var viewerName = session.AiVsAi ? "AI 对战观战" : Config.Get("DuelPlayerName0", "玩家");
            if (!TcpHelper.LinkStart("127.0.0.1", viewerName, session.Port.ToString(), string.Empty, true, null))
            { session.Fail("无法加入本地 AI 对战，请稍后重试。"); yield break; }
            deadline = DateTime.UtcNow.AddSeconds(15);
            while (current == session && session.Failure == null && !session.ViewerJoined)
            {
                if (DateTime.UtcNow > deadline) { session.Fail("玩家或观战连接超时。"); yield break; }
                yield return null;
            }
            if (current != session || session.Failure != null) yield break;
            if (session.AiVsAi) StartBot(session, "AI B", session.DeckB);
            session.AllowStart = true;
            session.Messages.Enqueue(session.AiVsAi ? "双方 AI 已连接，正在开始对战…" : "AI 已连接，正在开始对战…");
        }

        private static GameClient StartBot(Session session, string name, string path)
        {
            var client = new GameClient(new WindBot.WindBotInfo
            {
                Name = name, DeckFile = path, Host = "127.0.0.1", Port = session.Port,
                Version = (int)Config.ClientVersion, Chat = false, Dialog = "default"
            })
            {
                ResponsesConfig = session.Config.Clone(), ResponsesCancellation = session.Cancellation.Token,
                ResponsesCanStart = () => session.AllowStart,
                ResponsesStatus = text => session.Messages.Enqueue(name + " · " + text),
                ResponsesDecision = (kind, action) =>
                {
                    Interlocked.Increment(ref session.Decisions);
                    session.Messages.Enqueue(name + " · " + kind + " · " + action);
                    session.Events.Enqueue(() => DecisionMade?.Invoke(name, kind, action));
                },
                ResponsesDuelEnded = result => RecordResult(session, name, result)
            };
            if (session.AiVsAi && name == "AI A")
                client.ResponsesGameMessage = (function, reader) =>
                {
                    var package = ResponsesSpectatorFeed.Capture(function, reader);
                    if (package != null) session.ViewerMessages.Enqueue(package);
                };
            session.Clients.Add(client);
            var thread = new Thread(() =>
            {
                try
                {
                    client.Start();
                    while (!session.Cancellation.IsCancellationRequested && client.Connection.IsConnected)
                    {
                        client.Tick();
                        session.Cancellation.Token.WaitHandle.WaitOne(25);
                    }
                    if (!session.Cancellation.IsCancellationRequested && Interlocked.Read(ref session.EndedTicks) == 0)
                        session.Fail(name + " 的本地连接已断开。");
                }
                catch (OperationCanceledException) { }
                catch (AiRequestException error) { session.Fail(name + "：" + error.Message); }
                catch { session.Fail(name + " 对战决策失败；已停止请求，请检查配置后重新开始。"); }
                finally { client.Stop(); }
            }) { IsBackground = true, Name = "Responses " + name };
            session.Workers.Add(thread);
            thread.Start();
            return client;
        }

        /// <returns>True when this session supplied the viewer's appearance/deck.</returns>
        public static bool OnViewerTypeChange(int type)
        {
            var session = current;
            if (session == null) return false;
            if (session.AiVsAi)
            {
                if (type == 7) session.ViewerJoined = true;
                else if (!session.ObserverRequested)
                {
                    session.ObserverRequested = true;
                    TcpHelper.CtosMessage_HsToObserver();
                }
            }
            else if (type < 4 && !session.PlayerReady)
            {
                session.PlayerReady = session.ViewerJoined = true;
                TcpHelper.CtosMessage_UpdateDeck(session.PlayerDeck);
                TcpHelper.CtosMessage_UpdateAppearance(session.PlayerDeck);
                TcpHelper.CtosMessage_HsReady();
            }
            return true;
        }

        // Called on Unity's main thread; bot workers never manipulate scene objects.
        public static void OnNativeWin(int winner, int reason)
        {
            var session = current;
            if (session == null || winner < 0 || winner > 2) return;
            session.ViewerWinQueued = true;
            NativeWinReason = reason;
            foreach (var client in session.Clients)
                if (client.ResponsesNativePlayer >= 0)
                    RecordResult(session, client.Username, winner == 2 ? 2
                        : winner == client.ResponsesNativePlayer ? 0 : 1);
            // Store every player's result before aborting model requests: an
            // abort may immediately invoke worker error/cancellation handlers.
            session.Cancellation.Cancel();
        }

        /// <summary>The observer socket can close before AI A's queued Win reaches Unity.</summary>
        public static bool OnViewerDisconnected()
        {
            var session = current;
            if (session == null) return false;
            if (session.ViewerDisconnectedTicks == 0)
                session.ViewerDisconnectedTicks = DateTime.UtcNow.Ticks;
            // Handle this in PerFrame after draining AI A's independent socket.
            // A normal Win must not become a disconnect toast or sibyl_quit.
            return true;
        }

        private static bool CanRetireCompletedSession(Session session, int pendingRenderedPackets, GameMessage lastRenderedMessage)
        {
            return session.ViewerWinQueued && session.ViewerMessages.IsEmpty && pendingRenderedPackets == 0
                && (lastRenderedMessage == GameMessage.Win || lastRenderedMessage == GameMessage.sibyl_quit);
        }

        private static string ViewerStreamFailure(Session session, long utcTicks)
        {
            return !session.ViewerWinQueued && session.ViewerDisconnectedTicks != 0
                && utcTicks - session.ViewerDisconnectedTicks > TimeSpan.FromSeconds(1).Ticks
                ? "本地对战连接已断开，未收到完整的对战结果。" : null;
        }

        private static void RecordResult(Session session, string name, int result)
        {
            lock (session.Results)
            {
                if (!session.Results.Add(name)) return;
                session.Messages.Enqueue(name + (result == 0 ? " 获胜" : result == 1 ? " 败北" : " 平局"));
                session.Events.Enqueue(() => DuelEnded?.Invoke(name, result));
                Interlocked.CompareExchange(ref session.EndedTicks, DateTime.UtcNow.Ticks, 0);
            }
        }

        public static void PerFrame()
        {
            var session = current;
            if (session != null)
            {
                while (session.ViewerMessages.TryDequeue(out var package))
                    Program.instance.room.ReceiveGameMessage(package);
                string latest = null;
                while (session.Messages.TryDequeue(out var message)) latest = message;
                while (session.Events.TryDequeue(out var notification)) notification();
                if (latest != null) SetStatus(latest);
                if (session.Failure != null)
                {
                    var failure = session.Failure;
                    FailureReason = failure;
                    Stop(); SetStatus(failure);
                    Program.instance.ocgcore.ForceMSquit();
                    MessageManager.Cast(failure);
                }
                else if (session.ViewerWinQueued)
                {
                    // Bot results and native Win arrive before the queued attack/result
                    // animations finish. Retire transport only after their actual consumer.
                    if (CanRetireCompletedSession(session, OcgCore.packages.Count, OcgCore.lastMessage))
                        Stop();
                }
                else if (session.ViewerDisconnectedTicks != 0)
                {
                    // Results on a bot connection do not prove that the viewer received
                    // its terminal stream. Preserve a genuine missing-stream failure.
                    var streamFailure = ViewerStreamFailure(session, DateTime.UtcNow.Ticks);
                    if (streamFailure != null) Interlocked.CompareExchange(ref session.Failure, streamFailure, null);
                }
                else if (session.ViewerJoined && (TcpHelper.tcpClient == null || !TcpHelper.tcpClient.Connected))
                    OnViewerDisconnected();
            }
            if (IsActive && Program.instance.ocgcore.showing && overlay == null)
            {
                var ui = Program.instance.ocgcore.GetUI<OcgCoreUI>();
                if (ui != null)
                {
                    var go = new GameObject("ResponsesDuelStatus", typeof(RectTransform), typeof(ResponsesDuelOverlay));
                    go.transform.SetParent(ui.Root, false);
                    overlay = go.GetComponent<ResponsesDuelOverlay>();
                    overlay.Initialize(ui.TextPlayer0Name);
                }
            }
            if (current != null && Program.instance.ocgcore.showing && !current.OpeningChatCleared)
            {
                // Room startup can still be animating when the native core
                // starts. SidePanel.Hide ignores that transition; retry until
                // its opening chat is hidden, then allow normal chat controls.
                var chat = Program.instance.ui_.chatPanel;
                if (chat != null)
                {
                    chat.Hide();
                    current.OpeningChatCleared = !chat.showing;
                }
            }
            if (overlay != null) overlay.Refresh();
        }

        private static void SetStatus(string text)
        {
            Status = text;
            StatusChanged?.Invoke(text);
        }

        public static void Stop() => Stop(true);
        public static void Stop(bool stopServer)
        {
            var session = current;
            if (session == null) return;
            current = null;
            lastDecisionCount = session.Decisions;
            // Canceling workers can immediately close their sockets. Retire the
            // native host first so user Stop cannot race native EOF/EndDuel.
            if (stopServer) YgoServer.StopServer();
            TcpHelper.DisconnectIntentional();
            session.Cancellation.Cancel();
            foreach (var client in session.Clients) client.Connection?.Close();
            // Requests abort through cancellation and workers are background threads.
            // A short join permits deck readers to release handles before cleanup.
            foreach (var worker in session.Workers)
                if (worker != Thread.CurrentThread && worker.IsAlive) worker.Join(100);
            try { Directory.Delete(session.TempDirectory, true); } catch (IOException) { }
        }

        private static int FindPort()
        {
            var first = new System.Random().Next(18000, 28000);
            for (var offset = 0; offset < 200; offset++)
                if (TcpHelper.IsPortAvailable(first + offset)) return first + offset;
            throw new InvalidOperationException("没有可用的本地对战端口。");
        }

        public static string DeckPreparationIssue(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "尚未选择卡组。";
            try { LoadDeck(path); return null; }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException
                || error is ArgumentException || error is InvalidOperationException)
            { return error.Message; }
        }

        private static GameDeck LoadDeck(string path)
        {
            if (!File.Exists(path)) path = Path.Combine(Program.PATH_DECK, path.EndsWith(".ydk", StringComparison.OrdinalIgnoreCase) ? path : path + ".ydk");
            if (!File.Exists(path)) throw new InvalidOperationException("请为双方选择有效的本地卡组。");
            var deck = new GameDeck(path);
            if (deck.Main.Count < 40 || deck.Main.Count > 60 || deck.Extra.Count > 15 || deck.Side.Count > 15)
                throw new InvalidOperationException("卡组需有 40–60 张主卡、最多 15 张额外卡和 15 张副卡。");
            var quantities = new Dictionary<int, int>();
            foreach (var code in deck.Main.Concat(deck.Extra).Concat(deck.Side))
            {
                var card = CardsManager.Get(code);
                if (card == null || card.Id == 0 || card.HasType(CardType.Token))
                    throw new InvalidOperationException("卡组包含当前卡池无法用于对战的卡片：" + code);
                var identity = card.Alias == 0 ? card.Id : card.Alias;
                quantities.TryGetValue(identity, out var count);
                if (count >= 3) throw new InvalidOperationException("卡组中的同名卡不能超过 3 张：" + card.Name);
                quantities[identity] = count + 1;
                var extra = card.HasType(CardType.Fusion) || card.HasType(CardType.Synchro) || card.HasType(CardType.Xyz) || card.HasType(CardType.Link);
                if (deck.Main.Contains(code) && extra || deck.Extra.Contains(code) && !extra)
                    throw new InvalidOperationException("请检查主卡组与额外卡组的位置：" + card.Name);
            }
            return deck;
        }

        private static string SaveDuelDeck(GameDeck deck, string path)
        {
            File.WriteAllText(path, "#main\n" + string.Join("\n", deck.Main) + "\n#extra\n" + string.Join("\n", deck.Extra) + "\n!side\n" + string.Join("\n", deck.Side) + "\n");
            return path;
        }
    }
}
