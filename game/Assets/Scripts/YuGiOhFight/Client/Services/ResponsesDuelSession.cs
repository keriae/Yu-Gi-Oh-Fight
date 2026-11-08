using System.IO;
using System.Linq;


namespace YuGiOhFight.Client.Services
{
    /// <summary>Owns only the local model duel and its cancellable bot workers.</summary>
    public static class ResponsesDuelSession
    {
        private sealed class Session
        {
            public readonly System.Threading.CancellationTokenSource Cancellation = new();
            public readonly System.Collections.Generic.List<WindBot.Game.GameClient> Clients = new();
            public readonly System.Collections.Generic.List<System.Threading.Thread> Workers = new();
            public readonly System.Collections.Concurrent.ConcurrentQueue<string> Messages = new();
            public readonly System.Collections.Concurrent.ConcurrentQueue<System.Action> Events = new();
            public readonly System.Collections.Concurrent.ConcurrentQueue<Runtime.Network.Package> ViewerMessages = new();
            public readonly System.Collections.Generic.HashSet<string> Results = new();
            public MDPro3.AI.ResponsesAiConfig Config;
            public MDPro3.AI.ResponsesAiConfig ConfigA;
            public MDPro3.AI.AiDuelistProfile ProfileA, ProfileB;
            public MDPro3.OnlineAppearanceData ExpectedAppearanceA, ExpectedAppearanceB;
            public bool HasAppearanceA, HasAppearanceB;
            public volatile bool AppearanceAckA, AppearanceAckB;
            public MDPro3.Duel.YGOSharp.Deck PlayerDeck;
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
                    if (Cancellation.IsCancellationRequested || System.Threading.Interlocked.Read(ref EndedTicks) != 0) return;
                    System.Threading.Interlocked.CompareExchange(ref Failure, message, null);
                }
            }
        }

        private static Session current;
        private static UI.ResponsesDuelOverlay overlay;
        public static bool IsActive => current != null;
        public static bool UsesAiAPerspective => current?.AiVsAi == true;
        public static bool ProfileAppearancesConfirmed => current != null
            && (current.ProfileA == null || current.HasAppearanceA && current.AppearanceAckA)
            && (current.ProfileB == null || current.HasAppearanceB && current.AppearanceAckB);
        public static string Status { get; private set; } = string.Empty;
        public static string FailureReason { get; private set; } = string.Empty;
        public static int NativeWinReason { get; private set; } = -1;
        public static int DecisionCount => current == null ? lastDecisionCount : current.Decisions;
        private static int lastDecisionCount;
        public static event System.Action<string> StatusChanged;
        public static event System.Action<string, string, string> DecisionMade;
        public static event System.Action<string, int> DuelEnded;

        public static void Start(MDPro3.AI.ResponsesAiConfig config, bool aiVsAi, string deckA, string deckB)
            => StartCore(config, config, null, null, aiVsAi, deckA, deckB, null);

        public static void Start(MDPro3.AI.AiDuelistProfile profileA, MDPro3.AI.AiDuelistProfile profileB, bool aiVsAi, string deckA, string deckB,
            MDPro3.AI.ResponsesDuelOptions options = null)
        {
            if (profileB?.Config == null || (aiVsAi && profileA?.Config == null))
                throw new MDPro3.AI.AiConfigurationException("请先选择出战的 AI 分身。");
            StartCore(aiVsAi ? profileA.Config : profileB.Config, profileB.Config,
                aiVsAi ? profileA : null, profileB, aiVsAi, deckA, deckB, options);
        }

        private static void StartCore(MDPro3.AI.ResponsesAiConfig configA, MDPro3.AI.ResponsesAiConfig configB,
            MDPro3.AI.AiDuelistProfile profileA, MDPro3.AI.AiDuelistProfile profileB, bool aiVsAi, string deckA, string deckB, MDPro3.AI.ResponsesDuelOptions options)
        {
            options = options?.Clone() ?? new MDPro3.AI.ResponsesDuelOptions();
            options.Validate();
            configA.Validate(); configB.Validate();
            var first = LoadDeck(deckA, options);
            var second = LoadDeck(deckB, options);
            if (Runtime.Program.instance == null || !Runtime.Network.TcpHelper.canJoin)
                throw new System.InvalidOperationException("请等待当前连接结束后再开始 AI 对战。");
            Stop();
            MDPro3.Net.YgoServer.StopServer();
            Runtime.Program.instance.ocgcore.CloseConnection();
            var session = new Session
            {
                Config = configB.Clone(), ConfigA = configA.Clone(), ProfileA = profileA?.Clone(), ProfileB = profileB?.Clone(),
                AiVsAi = aiVsAi, PlayerDeck = first,
                Port = FindPort(), TempDirectory = Path.Combine(Path.GetTempPath(), "mdpro3-ai-" + System.Guid.NewGuid().ToString("N"))
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
            Controllers.RoomServant.FromSolo = true;
            Controllers.RoomServant.FromHandTest = false;
            Controllers.RoomServant.FromLocalHost = false;
            Controllers.RoomServant.SoloLockHand = false;
            Controllers.RoomServant.JoinWithReconnect = false;
            Controllers.RoomServant.NeedSide = false;
            Runtime.Program.instance.room.duelEnded = false;
            Controllers.SoloSelector.port = session.Port.ToString();
            // Single duel, selected master rule, full OCG/TCG card pool, 40–60 cards.
            // No turn clock: model latency must not lose the duel automatically.
            MDPro3.Net.YgoServer.StartServer(options.ToServerArguments(session.Port));
            current = session;
            var bootstrap = new System.Threading.Thread(() =>
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
            Runtime.Program.instance.StartCoroutine(Launch(session));
        }

        private static System.Collections.IEnumerator Launch(Session session)
        {
            var deadline = System.DateTime.UtcNow.AddSeconds(30);
            while (current == session && session.Failure == null
                && !session.Initialized)
            {
                if (System.DateTime.UtcNow > deadline) { session.Fail("本地 AI 对战服务启动超时。"); yield break; }
                yield return null;
            }
            if (current != session || session.Failure != null) yield break;
            // A native listener using SO_REUSEADDR can permit a separate bind to
            // loopback on macOS. Binding a probe is not a reliable ready check.
            yield return new UnityEngine.WaitForSecondsRealtime(0.25f);
            if (current != session || session.Failure != null || session.Cancellation.IsCancellationRequested) yield break;
            var host = StartBot(session, session.AiVsAi);
            deadline = System.DateTime.UtcNow.AddSeconds(15);
            while (current == session && session.Failure == null && !host.ResponsesJoined)
            {
                if (System.DateTime.UtcNow > deadline) { session.Fail("AI 未能加入本地对战。"); yield break; }
                yield return null;
            }
            if (current != session || session.Failure != null) yield break;
            var viewerName = session.AiVsAi ? "AI 对战观战" : MDPro3.Config.Get("DuelPlayerName0", "玩家");
            if (!Runtime.Network.TcpHelper.LinkStart("127.0.0.1", viewerName, session.Port.ToString(), string.Empty, true, null))
            { session.Fail("无法加入本地 AI 对战，请稍后重试。"); yield break; }
            deadline = System.DateTime.UtcNow.AddSeconds(15);
            while (current == session && session.Failure == null && !session.ViewerJoined)
            {
                if (System.DateTime.UtcNow > deadline) { session.Fail("玩家或观战连接超时。"); yield break; }
                yield return null;
            }
            if (current != session || session.Failure != null) yield break;
            if (session.AiVsAi) StartBot(session, false);
            session.AllowStart = true;
            session.Messages.Enqueue(session.AiVsAi ? "双方 AI 已连接，正在开始对战…" : "AI 已连接，正在开始对战…");
        }

        private static WindBot.Game.GameClient StartBot(Session session, bool first)
        {
            var profile = first ? session.ProfileA : session.ProfileB;
            var config = first ? session.ConfigA : session.Config;
            string path = first ? session.DeckA : session.DeckB;
            string name = ProfileBotName(session, first);
            string appearance = null;
            if (profile != null)
            {
                appearance = ProfileAppearanceMessage(profile, out var expected);
                if (first) { session.ExpectedAppearanceA = expected; session.HasAppearanceA = true; }
                else { session.ExpectedAppearanceB = expected; session.HasAppearanceB = true; }
            }
            var client = new WindBot.Game.GameClient(new WindBot.WindBotInfo
            {
                Name = name, DeckFile = path, Host = "127.0.0.1", Port = session.Port,
                Version = (int)MDPro3.Config.ClientVersion, Chat = false, Dialog = "default"
            })
            {
                ResponsesConfig = config.Clone(), ResponsesCancellation = session.Cancellation.Token,
                ResponsesCanStart = () => session.AllowStart && (!session.HasAppearanceA || session.AppearanceAckA)
                    && (!session.HasAppearanceB || session.AppearanceAckB),
                ResponsesStatus = text => session.Messages.Enqueue(name + " · " + text),
                ResponsesDecision = (kind, action) =>
                {
                    System.Threading.Interlocked.Increment(ref session.Decisions);
                    session.Messages.Enqueue(name + " · " + kind + " · " + action);
                    session.Events.Enqueue(() => DecisionMade?.Invoke(name, kind, action));
                },
                ResponsesDuelEnded = result => RecordResult(session, name, result)
            };
            if (session.AiVsAi && first)
                client.ResponsesGameMessage = (function, reader) =>
                {
                    var package = MDPro3.AI.ResponsesSpectatorFeed.Capture(function, reader);
                    if (package != null) session.ViewerMessages.Enqueue(package);
                };
            session.Clients.Add(client);
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    client.Start();
                    bool appearanceSent = false;
                    while (!session.Cancellation.IsCancellationRequested && client.Connection.IsConnected)
                    {
                        // Send from the socket's owning worker, after the viewer can receive it.
                        // The host waits for the viewer's acknowledgements before sending Start.
                        if (!appearanceSent && session.AllowStart && client.ResponsesJoined)
                        {
                            if (appearance != null) client.Chat(appearance);
                            appearanceSent = true;
                        }
                        client.Tick();
                        session.Cancellation.Token.WaitHandle.WaitOne(25);
                    }
                    if (!session.Cancellation.IsCancellationRequested && System.Threading.Interlocked.Read(ref session.EndedTicks) == 0)
                        session.Fail(name + " 的本地连接已断开。");
                }
                catch (System.OperationCanceledException) { }
                catch (MDPro3.AI.AiRequestException error) { session.Fail(name + "：" + error.Message); }
                catch { session.Fail(name + " 对战决策失败；已停止请求，请检查配置后重新开始。"); }
                finally { client.Stop(); }
            }) { IsBackground = true, Name = "Responses " + name };
            session.Workers.Add(thread);
            thread.Start();
            return client;
        }

        private static string ProfileBotName(Session session, bool first)
        {
            var profile = first ? session.ProfileA : session.ProfileB;
            if (profile == null) return first ? "AI A" : session.AiVsAi ? "AI B" : "AI";
            var other = first ? session.ProfileB : session.ProfileA;
            return MDPro3.AI.AiDuelistNames.ForSide(profile.Name, session.AiVsAi ? other?.Name : null, first);
        }

        private static string ProfileAppearanceMessage(MDPro3.AI.AiDuelistProfile profile, out MDPro3.OnlineAppearanceData expected)
        {
            var defaults = MDPro3.AI.AiDuelistProfile.NewAppearance();
            var keys = new[] { "Case", "Protector", "Field", "Grave", "Stand", "Mate", "Face", "Frame" };
            string message = MDPro3.ZeroWidthIntsCodec.Encode(keys.Select(key => profile.Appearance != null
                && profile.Appearance.TryGetValue(key, out string raw) && int.TryParse(raw, out int code) ? code : int.Parse(defaults[key])).ToArray());
            MDPro3.OnlineAppearanceSync.TryParse(message, out expected);
            // Resolve random/unsupported codes once, so the receiver confirms the same concrete appearance.
            return MDPro3.ZeroWidthIntsCodec.Encode(new[] { expected.Case, expected.Protector, expected.Field, expected.Grave,
                expected.Stand, expected.Mate, expected.Face, expected.Frame });
        }

        public static string NameForSide(int side, string fallback)
        {
            var session = current;
            var profile = side == 0 ? session?.AiVsAi == true ? session.ProfileA : null : session?.ProfileB;
            return profile == null ? fallback : ProfileBotName(session, side == 0);
        }

        /// <summary>Resolve local duelist presentation without changing the player's global settings.</summary>
        public static string AppearanceForSide(int side, string key, string fallback)
        {
            var session = current;
            var profile = side == 0 ? session?.AiVsAi == true ? session.ProfileA : null : session?.ProfileB;
            if (profile?.Appearance == null || !profile.Appearance.TryGetValue(key, out string value)
                || string.IsNullOrEmpty(value) || value == "0") return fallback;
            return value;
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
                    Runtime.Network.TcpHelper.CtosMessage_HsToObserver();
                }
            }
            else if (type < 4 && !session.PlayerReady)
            {
                session.PlayerReady = session.ViewerJoined = true;
                Runtime.Network.TcpHelper.CtosMessage_UpdateDeck(session.PlayerDeck);
                Runtime.Network.TcpHelper.CtosMessage_UpdateAppearance(session.PlayerDeck);
                Runtime.Network.TcpHelper.CtosMessage_HsReady();
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
                session.ViewerDisconnectedTicks = System.DateTime.UtcNow.Ticks;
            // Handle this in PerFrame after draining AI A's independent socket.
            // A normal Win must not become a disconnect toast or sibyl_quit.
            return true;
        }

        private static bool CanRetireCompletedSession(Session session, int pendingRenderedPackets, MDPro3.Duel.YGOSharp.GameMessage lastRenderedMessage)
        {
            return session.ViewerWinQueued && session.ViewerMessages.IsEmpty && pendingRenderedPackets == 0
                && (lastRenderedMessage == MDPro3.Duel.YGOSharp.GameMessage.Win || lastRenderedMessage == MDPro3.Duel.YGOSharp.GameMessage.sibyl_quit);
        }

        private static string ViewerStreamFailure(Session session, long utcTicks)
        {
            return !session.ViewerWinQueued && session.ViewerDisconnectedTicks != 0
                && utcTicks - session.ViewerDisconnectedTicks > System.TimeSpan.FromSeconds(1).Ticks
                ? "本地对战连接已断开，未收到完整的对战结果。" : null;
        }

        private static void RecordResult(Session session, string name, int result)
        {
            lock (session.Results)
            {
                if (!session.Results.Add(name)) return;
                session.Messages.Enqueue(name + (result == 0 ? " 获胜" : result == 1 ? " 败北" : " 平局"));
                session.Events.Enqueue(() => DuelEnded?.Invoke(name, result));
                System.Threading.Interlocked.CompareExchange(ref session.EndedTicks, System.DateTime.UtcNow.Ticks, 0);
            }
        }

        public static void PerFrame()
        {
            var session = current;
            if (session != null)
            {
                // Only Unity's main thread reads RoomServant's received metadata dictionary.
                // Cross-socket send ordering cannot prove that the spectator has received both cosmetics.
                if (session.HasAppearanceA && !session.AppearanceAckA
                    && Controllers.RoomServant.TryGetOnlineAppearanceForPlayer(0, out var appearanceA)
                    && appearanceA.Equals(session.ExpectedAppearanceA)) session.AppearanceAckA = true;
                if (session.HasAppearanceB && !session.AppearanceAckB
                    && Controllers.RoomServant.TryGetOnlineAppearanceForPlayer(session.AiVsAi ? 1 : 0, out var appearanceB)
                    && appearanceB.Equals(session.ExpectedAppearanceB)) session.AppearanceAckB = true;
                while (session.ViewerMessages.TryDequeue(out var package))
                    Runtime.Program.instance.room.ReceiveGameMessage(package);
                string latest = null;
                while (session.Messages.TryDequeue(out var message)) latest = message;
                while (session.Events.TryDequeue(out var notification)) notification();
                if (latest != null) SetStatus(latest);
                if (session.Failure != null)
                {
                    var failure = session.Failure;
                    FailureReason = failure;
                    Stop(); SetStatus(failure);
                    Runtime.Program.instance.ocgcore.ForceMSquit();
                    Runtime.MessageManager.Cast(failure);
                }
                else if (session.ViewerWinQueued)
                {
                    // Bot results and native Win arrive before the queued attack/result
                    // animations finish. Retire transport only after their actual consumer.
                    if (CanRetireCompletedSession(session, Controllers.OcgCore.packages.Count, Controllers.OcgCore.lastMessage))
                        Stop();
                }
                else if (session.ViewerDisconnectedTicks != 0)
                {
                    // Results on a bot connection do not prove that the viewer received
                    // its terminal stream. Preserve a genuine missing-stream failure.
                    var streamFailure = ViewerStreamFailure(session, System.DateTime.UtcNow.Ticks);
                    if (streamFailure != null) System.Threading.Interlocked.CompareExchange(ref session.Failure, streamFailure, null);
                }
                else if (session.ViewerJoined && (Runtime.Network.TcpHelper.tcpClient == null || !Runtime.Network.TcpHelper.tcpClient.Connected))
                    OnViewerDisconnected();
            }
            if (IsActive && Runtime.Program.instance.ocgcore.showing && overlay == null)
            {
                UI.ServantUI.OcgCoreUI ui = Runtime.Program.instance.ocgcore.GetUI<UI.ServantUI.OcgCoreUI>();
                if (ui != null)
                {
                    var go = new UnityEngine.GameObject("ResponsesDuelStatus", typeof(UnityEngine.RectTransform), typeof(UI.ResponsesDuelOverlay));
                    go.transform.SetParent(ui.Root, false);
                    overlay = go.GetComponent<UI.ResponsesDuelOverlay>();
                    overlay.Initialize(ui.TextPlayer0Name);
                }
            }
            if (current != null && Runtime.Program.instance.ocgcore.showing && !current.OpeningChatCleared)
            {
                // Room startup can still be animating when the native core
                // starts. SidePanel.Hide ignores that transition; retry until
                // its opening chat is hidden, then allow normal chat controls.
                UI.ChatPanel chat = Runtime.Program.instance.ui_.chatPanel;
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
            if (stopServer) MDPro3.Net.YgoServer.StopServer();
            Runtime.Network.TcpHelper.DisconnectIntentional();
            session.Cancellation.Cancel();
            foreach (var client in session.Clients) client.Connection?.Close();
            // Requests abort through cancellation and workers are background threads.
            // A short join permits deck readers to release handles before cleanup.
            foreach (var worker in session.Workers)
                if (worker != System.Threading.Thread.CurrentThread && worker.IsAlive) worker.Join(100);
            try { Directory.Delete(session.TempDirectory, true); } catch (IOException) { }
        }

        private static int FindPort()
        {
            var first = new System.Random().Next(18000, 28000);
            for (var offset = 0; offset < 200; offset++)
                if (Runtime.Network.TcpHelper.IsPortAvailable(first + offset)) return first + offset;
            throw new System.InvalidOperationException("没有可用的本地对战端口。");
        }

        public static string DeckPreparationIssue(string path)
            => DeckPreparationIssue(path, null);

        public static string DeckPreparationIssue(string path, MDPro3.AI.ResponsesDuelOptions options)
        {
            if (string.IsNullOrWhiteSpace(path)) return "尚未选择卡组。";
            try { LoadDeck(path, options); return null; }
            catch (System.Exception error) when (error is IOException || error is System.UnauthorizedAccessException
                || error is System.ArgumentException || error is System.InvalidOperationException || error is MDPro3.AI.AiConfigurationException)
            { return error.Message; }
        }

        private static MDPro3.Duel.YGOSharp.Deck LoadDeck(string path, MDPro3.AI.ResponsesDuelOptions options = null)
        {
            var banlist = options?.ResolveBanlist();
            if (!File.Exists(path)) path = Path.Combine(Runtime.Program.PATH_DECK, path.EndsWith(".ydk", System.StringComparison.OrdinalIgnoreCase) ? path : path + ".ydk");
            if (!File.Exists(path)) throw new System.InvalidOperationException("请为双方选择有效的本地卡组。");
            var deck = new MDPro3.Duel.YGOSharp.Deck(path);
            if (deck.Main.Count < 40 || deck.Main.Count > 60 || deck.Extra.Count > 15 || deck.Side.Count > 15)
                throw new System.InvalidOperationException("卡组需有 40–60 张主卡、最多 15 张额外卡和 15 张副卡。");
            var quantities = new System.Collections.Generic.Dictionary<int, int>();
            foreach (var code in deck.Main.Concat(deck.Extra).Concat(deck.Side))
            {
                var card = MDPro3.Duel.YGOSharp.CardsManager.Get(code);
                if (card == null || card.Id == 0 || card.HasType(MDPro3.Duel.YGOSharp.CardType.Token))
                    throw new System.InvalidOperationException("卡组包含当前卡池无法用于对战的卡片：" + code);
                var identity = card.Alias == 0 ? card.Id : card.Alias;
                quantities.TryGetValue(identity, out var count);
                if (count >= 3) throw new System.InvalidOperationException("卡组中的同名卡不能超过 3 张：" + card.Name);
                if (banlist != null && count >= banlist.GetQuantity(identity))
                    throw new System.InvalidOperationException("卡组不符合所选限卡表：" + card.Name + "（" + banlist.Name + "）。");
                quantities[identity] = count + 1;
                var extra = card.HasType(MDPro3.Duel.YGOSharp.CardType.Fusion) || card.HasType(MDPro3.Duel.YGOSharp.CardType.Synchro) || card.HasType(MDPro3.Duel.YGOSharp.CardType.Xyz) || card.HasType(MDPro3.Duel.YGOSharp.CardType.Link);
                if (deck.Main.Contains(code) && extra || deck.Extra.Contains(code) && !extra)
                    throw new System.InvalidOperationException("请检查主卡组与额外卡组的位置：" + card.Name);
            }
            return deck;
        }

        private static string SaveDuelDeck(MDPro3.Duel.YGOSharp.Deck deck, string path)
        {
            File.WriteAllText(path, "#main\n" + string.Join("\n", deck.Main) + "\n#extra\n" + string.Join("\n", deck.Extra) + "\n!side\n" + string.Join("\n", deck.Side) + "\n");
            return path;
        }
    }
}
