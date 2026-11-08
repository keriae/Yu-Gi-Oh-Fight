using System.Linq;


namespace YuGiOhFight.Client.Runtime.Network
{
    public static class TcpHelper
    {
        public static System.Net.Sockets.TcpClient tcpClient;
        private static System.Net.Sockets.NetworkStream networkStream;
        public static bool canJoin = true;
        public static bool onDisConnected;
        private static readonly System.Collections.Generic.List<byte[]> datas = new System.Collections.Generic.List<byte[]>();
        private static readonly object locker = new object();
        public static MDPro3.Duel.YGOSharp.Deck deck;
        public static System.Collections.Generic.List<string> deckStrings = new System.Collections.Generic.List<string>();
        public static string lastRecordName = "";
        public static System.Collections.Generic.List<Package> packagesInRecord = new System.Collections.Generic.List<Package>();

        private static readonly System.Collections.Generic.Queue<Package> messageQueue = new System.Collections.Generic.Queue<Package>();
        static System.Threading.Thread senderThread;
        static System.Threading.Thread linkThread;
        static System.Threading.Thread receiverThread;
        private static readonly object connectionGate = new object();
        private static volatile int connectionGeneration;
        private static volatile bool intentionalDisconnect;
        internal static int ConnectionGeneration => connectionGeneration;

        public static string joinedAddress;
        public static string joinedPort;
        public static string joinedPassword;


        public static bool LinkStart(string ipString, string name, string portString, string pswString, bool local, System.Action doWhenSuccess)
        {
            int generation;
            lock (connectionGate)
            {
                if (!canJoin) return false;
                generation = ++connectionGeneration;
                intentionalDisconnect = false;
                onDisConnected = false;
                canJoin = false;
            }
            lock (datas) datas.Clear();
            linkThread = new System.Threading.Thread(() =>
            {
                try
                {
                    bool joined = false;
                    do
                    {
                        if (!IsGenerationCurrent(generation)) break;
                        joined = Join(ipString, name, portString, pswString, doWhenSuccess, generation);
                        if (!local || joined) break;
                        System.Threading.Thread.Sleep(100);
                    } while (IsGenerationCurrent(generation) && MDPro3.Net.YgoServer.ServerRunning());
                    if (!joined && IsGenerationCurrent(generation)) MarkDisconnected(generation);
                }
                finally
                {
                    lock (connectionGate)
                        if (generation == connectionGeneration) canJoin = true;
                }
            }) { IsBackground = true, Name = "MDPro3 duel connection" };
            linkThread.Start();
            return true;
        }

        private static bool IsGenerationCurrent(int generation)
            => generation == connectionGeneration && !intentionalDisconnect;

        private static bool IsConnectionCurrent(System.Net.Sockets.TcpClient client, int generation)
            => IsGenerationCurrent(generation) && client != null && ReferenceEquals(client, tcpClient);

        private static void MarkDisconnected(int generation)
        {
            lock (connectionGate)
                if (IsGenerationCurrent(generation)) onDisConnected = true;
        }

        /// <summary>Discard the departed connection before changing scenes; late packets cannot reopen its duel.</summary>
        public static void DisconnectIntentional()
        {
            System.Net.Sockets.TcpClient client;
            System.Net.Sockets.NetworkStream stream;
            lock (connectionGate)
            {
                ++connectionGeneration;
                intentionalDisconnect = true;
                onDisConnected = false;
                client = tcpClient;
                stream = networkStream;
                tcpClient = null;
                networkStream = null;
                canJoin = true;
                if (Program.instance != null && Program.instance.room != null)
                    Program.instance.room.duelEnded = true;
            }
            try { client?.Close(); } catch { }
            try { stream?.Dispose(); } catch { }
            // These locks are reentrant when a packet handler requests an exit.
            lock (datas) datas.Clear();
            lock (locker) messageQueue.Clear();
            deck = null;
            deckStrings.Clear();
        }

        private static bool Join(string ipString, string name, string portString, string pswString,
            System.Action doWhenSuccess, int generation)
        {
            System.Net.Sockets.TcpClient client = null;
            try
            {
                if (!IsGenerationCurrent(generation)) return false;
                if (tcpClient != null && tcpClient.Connected)
                {
                    MarkDisconnected(generation);
                    return false;
                }
                UnityEngine.Debug.LogFormat("Try Address: {0}, Port: {1}, Password: {2}", ipString, portString, pswString);
                client = new TcpClientWithTimeout(ipString, int.Parse(portString), 3000)
                    .Connect(() => IsGenerationCurrent(generation));
                var stream = client.GetStream();
                lock (connectionGate)
                {
                    if (!IsGenerationCurrent(generation)) { client.Close(); return false; }
                    tcpClient = client;
                    networkStream = stream;
                    Program.instance.room.duelEnded = false;
                    lock (locker) messageQueue.Clear();
                    receiverThread = new System.Threading.Thread(() => Receiver(client, stream, generation))
                        { IsBackground = true, Name = "MDPro3 duel receiver" };
                    receiverThread.Start();
                    InitializeSender(client, generation);
                    CtosMessage_ExternalAddress(ipString);
                    CtosMessage_PlayerInfo(name);
                    CtosMessage_JoinGame(pswString);
                    joinedAddress = ipString;
                    joinedPort = portString;
                    joinedPassword = pswString;
                    Controllers.OcgCore.mycardDuel = joinedAddress == Services.MyCard.duelUrl;
                    doWhenSuccess?.Invoke();
                }
                UnityEngine.Debug.LogFormat("Joind Address: {0}, Port: {1}, Password: {2}", joinedAddress, joinedPort, joinedPassword);
                return true;
            }
            catch
            {
                try { client?.Close(); } catch { }
                return false;
            }
        }

        public static void InitializeSender()
        {
            lock (connectionGate) InitializeSender(tcpClient, connectionGeneration);
        }

        private static void InitializeSender(System.Net.Sockets.TcpClient client, int generation)
        {
            if (!IsConnectionCurrent(client, generation)) return;
            senderThread = new System.Threading.Thread(() => Sender(client, generation))
                { IsBackground = true, Name = "MDPro3 duel sender" };
            senderThread.Start();
        }

        public static void Receiver()
        {
            System.Net.Sockets.TcpClient client;
            System.Net.Sockets.NetworkStream stream;
            int generation;
            lock (connectionGate)
            {
                client = tcpClient;
                stream = networkStream;
                generation = connectionGeneration;
            }
            Receiver(client, stream, generation);
        }

        private static void Receiver(System.Net.Sockets.TcpClient client, System.Net.Sockets.NetworkStream stream, int generation)
        {
            try
            {
                while (IsConnectionCurrent(client, generation) && stream != null && client.Connected
                    && Program.Running && !Program.instance.room.duelEnded)
                {
                    var data = SocketMaster.ReadPacket(stream);
                    lock (datas)
                        if (IsConnectionCurrent(client, generation)) datas.Add(data);
                }
                MarkDisconnected(generation);
            }
            catch { MarkDisconnected(generation); }
        }

        public static void AddDateJumoLine(byte[] data)
        {
            // PercyOCG also supplies local puzzle messages through this method.
            // TCP receiver packets instead use the connection-bound path above.
            lock (datas) datas.Add(data);
        }

        public static void PerFrameFunction()
        {
            if (datas.Count > 0)
                if (System.Threading.Monitor.TryEnter(datas))
                {
                    var batchGeneration = connectionGeneration;
                    for (var i = 0; i < datas.Count && batchGeneration == connectionGeneration; i++)
                        try
                        {
                            var memoryStream = new System.IO.MemoryStream(datas[i]);
                            var r = new System.IO.BinaryReader(memoryStream);
                            var ms = (MDPro3.Duel.YGOSharp.StocMessage)r.ReadByte();
                            switch (ms)
                            {
                                case MDPro3.Duel.YGOSharp.StocMessage.GameMsg:
                                    Program.instance.room.StocMessage_GameMsg(r);
                                    break;
                                case MDPro3.Duel.YGOSharp.StocMessage.ErrorMsg:
                                    Program.instance.room.StocMessage_ErrorMsg(r);
                                    break;
                                case MDPro3.Duel.YGOSharp.StocMessage.SelectHand:
                                    Program.instance.room.StocMessage_SelectHand(r);
                                    break;
                                case MDPro3.Duel.YGOSharp.StocMessage.SelectTp:
                                    Program.instance.room.StocMessage_SelectTp(r);
                                    break;
                                case MDPro3.Duel.YGOSharp.StocMessage.HandResult:
                                    Program.instance.room.StocMessage_HandResult(r);
                                    break;
                                case MDPro3.Duel.YGOSharp.StocMessage.TpResult:
                                    Program.instance.room.StocMessage_TpResult(r);
                                    break;
                                case MDPro3.Duel.YGOSharp.StocMessage.ChangeSide:
                                    Program.instance.room.StocMessage_ChangeSide(r);
                                    break;
                                case MDPro3.Duel.YGOSharp.StocMessage.WaitingSide:
                                    Program.instance.room.StocMessage_WaitingSide(r);
                                    break;
                                case MDPro3.Duel.YGOSharp.StocMessage.DeckCount:
                                    Program.instance.room.StocMessage_DeckCount(r);
                                    break;
                                case MDPro3.Duel.YGOSharp.StocMessage.CreateGame:
                                    Program.instance.room.StocMessage_CreateGame(r);
                                    break;
                                case MDPro3.Duel.YGOSharp.StocMessage.JoinGame:
                                    Program.instance.room.StocMessage_JoinGame(r);
                                    break;
                                case MDPro3.Duel.YGOSharp.StocMessage.TypeChange:
                                    Program.instance.room.StocMessage_TypeChange(r);
                                    break;
                                case MDPro3.Duel.YGOSharp.StocMessage.LeaveGame:
                                    Program.instance.room.StocMessage_LeaveGame(r);
                                    break;
                                case MDPro3.Duel.YGOSharp.StocMessage.DuelStart:
                                    Program.instance.room.StocMessage_DuelStart(r);
                                    break;
                                case MDPro3.Duel.YGOSharp.StocMessage.DuelEnd:
                                    Program.instance.room.StocMessage_DuelEnd(r);
                                    break;
                                case MDPro3.Duel.YGOSharp.StocMessage.Replay:
                                    Program.instance.room.StocMessage_Replay(r);
                                    break;
                                case MDPro3.Duel.YGOSharp.StocMessage.TimeLimit:
                                    Program.instance.ocgcore.StocMessage_TimeLimit(r);
                                    break;
                                case MDPro3.Duel.YGOSharp.StocMessage.Chat:
                                    Program.instance.room.StocMessage_Chat(r);
                                    break;
                                case MDPro3.Duel.YGOSharp.StocMessage.HsPlayerEnter:
                                    Program.instance.room.StocMessage_HsPlayerEnter(r);
                                    break;
                                case MDPro3.Duel.YGOSharp.StocMessage.HsPlayerChange:
                                    Program.instance.room.StocMessage_HsPlayerChange(r);
                                    break;
                                case MDPro3.Duel.YGOSharp.StocMessage.HsWatchChange:
                                    Program.instance.room.StocMessage_HsWatchChange(r);
                                    break;
                                case MDPro3.Duel.YGOSharp.StocMessage.TeammateSurrender:
                                    Program.instance.ocgcore.StocMessage_TeammateSurrender();
                                    break;
                            }
                        }
                        catch//(Exception e)
                        {
                            //Debug.LogError(e);
                        }

                    datas.Clear();
                    System.Threading.Monitor.Exit(datas);
                }

            if (onDisConnected && !intentionalDisconnect)
            {
                System.Net.Sockets.TcpClient client;
                lock (connectionGate)
                {
                    if (!onDisConnected || intentionalDisconnect) return;
                    ++connectionGeneration;
                    onDisConnected = false;
                    client = tcpClient;
                    tcpClient = null;
                    networkStream = null;
                }
                try { client?.Close(); } catch { }
                deck = null;
                deckStrings.Clear();
                canJoin = true;
                if (Program.instance.ocgcore.showing)
                {
                    Program.instance.room.duelEnded = true;
                    if (!Services.ResponsesDuelSession.OnViewerDisconnected())
                    {
                        Program.instance.ocgcore.ForceMSquit();
                        MessageManager.Cast(MDPro3.InterString.Get("对方已离开游戏，您现在可以离开。"));
                    }
                }
                else if (Program.instance.deckEditor.showing)
                {
                    MessageManager.Cast(MDPro3.InterString.Get("对方已离开游戏，您现在可以离开。"));
                    Program.instance.ShiftToServant(Program.instance.online);
                }
                else if (!Program.instance.online.showing && !Program.instance.solo.showing)
                {
                    MessageManager.Cast(MDPro3.InterString.Get("连接被断开。"));
                    if (Program.instance.room.showing)
                        Program.instance.room.OnExit();
                }
            }
        }

        public static void Send(Package message)
        {
            lock (locker)
            {
                if (!intentionalDisconnect && tcpClient != null)
                    messageQueue.Enqueue(message);
            }
        }

        private static void Sender(System.Net.Sockets.TcpClient client, int generation)
        {
            while (IsConnectionCurrent(client, generation) && client.Connected)
            {
                System.Threading.Thread.Sleep(100);
                Package currentMessage;
                lock (locker)
                {
                    if (!IsConnectionCurrent(client, generation)) break;
                    if (messageQueue.Count == 0) continue;
                    currentMessage = messageQueue.Dequeue();
                }
                try
                {
                    var data = currentMessage.Data.Get();
                    using (System.IO.MemoryStream memstream = new System.IO.MemoryStream())
                    {
                        using (System.IO.BinaryWriter b = new System.IO.BinaryWriter(memstream))
                        {
                            b.Write(System.BitConverter.GetBytes((short)(data.Length + 1)), 0, 2);
                            b.Write(System.BitConverter.GetBytes((byte)currentMessage.Function), 0, 1);
                            b.Write(data, 0, data.Length);
                        }
                        if (!IsConnectionCurrent(client, generation)) break;
                        client.Client.Send(memstream.ToArray());
                    }
                }
                catch
                {
                    MarkDisconnected(generation);
                    break;
                }
            }
        }

        #region CtosMessage

        public static void CtosMessage_Response(byte[] response)
        {
            var message = new Package();
            message.Function = (int)MDPro3.Duel.YGOSharp.CtosMessage.Response;
            message.Data.writer.Write(response);
            Send(message);
        }

        public static void CtosMessage_UpdateDeck(MDPro3.Duel.YGOSharp.Deck deckFor)
        {
            if (deckFor.Main.Count == 0)
                return;
            deckStrings.Clear();
            deck = deckFor;
            var message = new Package();
            message.Function = (int)MDPro3.Duel.YGOSharp.CtosMessage.UpdateDeck;
            message.Data.writer.Write(deckFor.Main.Count + deckFor.Extra.Count);
            message.Data.writer.Write(deckFor.Side.Count);
            for (var i = 0; i < deckFor.Main.Count; i++)
            {
                message.Data.writer.Write(deckFor.Main[i]);
                var c = MDPro3.Duel.YGOSharp.CardsManager.Get(deckFor.Main[i]);
                deckStrings.Add(c.Name);
            }

            for (var i = 0; i < deckFor.Extra.Count; i++) message.Data.writer.Write(deckFor.Extra[i]);
            for (var i = 0; i < deckFor.Side.Count; i++) message.Data.writer.Write(deckFor.Side[i]);
            Send(message);
            CtosMessage_UpdateAppearance(deckFor);
        }

        public static void CtosMessage_HandResult(int res)
        {
            var message = new Package();
            message.Function = (int)MDPro3.Duel.YGOSharp.CtosMessage.HandResult;
            message.Data.writer.Write((byte)res);
            Send(message);
        }

        public static void CtosMessage_TpResult(bool tp)
        {
            var message = new Package();
            message.Function = (int)MDPro3.Duel.YGOSharp.CtosMessage.TpResult;
            if (tp)
                message.Data.writer.Write((byte)1);
            else
                message.Data.writer.Write((byte)0);
            Send(message);
        }

        public static void CtosMessage_ExternalAddress(string hostname)
        {
            var message = new Package();
            message.Function = (int)MDPro3.Duel.YGOSharp.CtosMessage.ExternalAddress;
            message.Data.writer.Write((uint)0);
            message.Data.writer.WriteUnicode(hostname, hostname.Length + 1);
            Send(message);
        }

        public static void CtosMessage_PlayerInfo(string name)
        {
            var message = new Package();
            message.Function = (int)MDPro3.Duel.YGOSharp.CtosMessage.PlayerInfo;
            message.Data.writer.WriteUnicode(name, 20);
            Send(message);
        }

        public static void CtosMessage_CreateGame()
        {
        }

        public static void CtosMessage_JoinGame(string psw)
        {
            deckStrings.Clear();
            deck = null;
            var message = new Package();
            message.Function = (int)MDPro3.Duel.YGOSharp.CtosMessage.JoinGame;
            message.Data.writer.Write((short)MDPro3.Config.ClientVersion);
            message.Data.writer.Write((byte)204);
            message.Data.writer.Write((byte)204);
            message.Data.writer.Write(0);
            message.Data.writer.WriteUnicode(psw, 20);
            Send(message);
        }

        public static void CtosMessage_LeaveGame()
        {
            deck = null;
            deckStrings.Clear();
            var message = new Package();
            message.Function = (int)MDPro3.Duel.YGOSharp.CtosMessage.LeaveGame;
            Send(message);
        }

        public static void CtosMessage_Surrender()
        {
            var message = new Package();
            message.Function = (int)MDPro3.Duel.YGOSharp.CtosMessage.Surrender;
            Send(message);
        }

        public static void CtosMessage_TimeConfirm()
        {
            var message = new Package();
            message.Function = (int)MDPro3.Duel.YGOSharp.CtosMessage.TimeConfirm;
            Send(message);
        }

        public static void CtosMessage_Chat(string str)
        {
            var message = new Package();
            message.Function = (int)MDPro3.Duel.YGOSharp.CtosMessage.Chat;
            message.Data.writer.WriteUnicode(str, str.Length + 1);
            Send(message);
        }

        public static void CtosMessage_UpdateAppearance(MDPro3.Duel.YGOSharp.Deck deckFor)
        {
            if (deckFor == null)
                return;
            var syncMessage = MDPro3.OnlineAppearanceSync.BuildMessageForLocalPlayer(deckFor);
            CtosMessage_Chat(syncMessage);
        }

        public static void CtosMessage_UpdateAppearanceFromCurrentDeck()
        {
            if (deck != null)
            {
                CtosMessage_UpdateAppearance(deck);
                return;
            }

            var deckPath = Program.PATH_DECK + MDPro3.Config.GetConfigDeckName() + Program.EXPANSION_YDK;
            if (!System.IO.File.Exists(deckPath))
                return;

            CtosMessage_UpdateAppearance(new MDPro3.Duel.YGOSharp.Deck(deckPath));
        }

        public static void CtosMessage_HsToDuelist()
        {
            var message = new Package();
            message.Function = (int)MDPro3.Duel.YGOSharp.CtosMessage.HsToDuelist;
            Send(message);
        }

        public static void CtosMessage_HsToObserver()
        {
            var message = new Package();
            message.Function = (int)MDPro3.Duel.YGOSharp.CtosMessage.HsToObserver;
            Send(message);
        }

        public static void CtosMessage_HsReady()
        {
            var message = new Package();
            message.Function = (int)MDPro3.Duel.YGOSharp.CtosMessage.HsReady;
            Send(message);
        }

        public static void CtosMessage_HsNotReady()
        {
            var message = new Package();
            message.Function = (int)MDPro3.Duel.YGOSharp.CtosMessage.HsNotReady;
            Send(message);
        }

        public static void CtosMessage_HsKick(int pos)
        {
            var message = new Package();
            message.Function = (int)MDPro3.Duel.YGOSharp.CtosMessage.HsKick;
            message.Data.writer.Write((byte)pos);
            Send(message);
        }

        public static void CtosMessage_HsStart()
        {
            var message = new Package();
            message.Function = (int)MDPro3.Duel.YGOSharp.CtosMessage.HsStart;
            Send(message);
        }
        #endregion

        public static System.Collections.Generic.List<Package> ReadPackagesInRecord(string path)
        {
            System.Collections.Generic.List<Package> re = null;
            try
            {
                re = GetPackages(System.IO.File.ReadAllBytes(path));
            }
            catch (System.Exception e)
            {
                re = new System.Collections.Generic.List<Package>();
                UnityEngine.Debug.Log(e);
            }

            return re;
        }

        public static System.Collections.Generic.List<Package> GetPackages(byte[] buffer)
        {
            var re = new System.Collections.Generic.List<Package>();
            try
            {
                System.IO.BinaryReader reader;
                using (reader = new System.IO.BinaryReader(new System.IO.MemoryStream(buffer)))
                {
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        var p = new Package();
                        p.Function = reader.ReadByte();
                        p.Data = new BinaryMaster(reader.ReadBytes((int)reader.ReadUInt32()));
                        re.Add(p);
                    }
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.Log(e);
            }
            return re;
        }

        public static void SaveRecord(string replayName)
        {
            try
            {
                if (packagesInRecord.Count > 10)
                {
                    var write = false;
                    var i = 0;
                    var startI = 0;
                    foreach (var item in packagesInRecord)
                    {
                        i++;
                        try
                        {
                            if (item.Function == (int)MDPro3.Duel.YGOSharp.GameMessage.Start)
                            {
                                write = true;
                                startI = i;
                            }

                            if (item.Function == (int)MDPro3.Duel.YGOSharp.GameMessage.ReloadField)
                            {
                                write = true;
                                startI = i;
                            }
                        }
                        catch (System.Exception e)
                        {
                            UnityEngine.Debug.Log(e);
                        }
                    }
                    if (write)
                    {
                        if (startI > packagesInRecord.Count)
                            startI = packagesInRecord.Count;
                        packagesInRecord.Insert(startI, Program.instance.ocgcore.GetNamePacket());
                        if (System.IO.File.Exists(Program.PATH_REPLAY + replayName + Program.EXPANSION_YRP3D))
                            System.IO.File.Delete(Program.PATH_REPLAY + replayName + Program.EXPANSION_YRP3D);
                        var stream = System.IO.File.Create(Program.PATH_REPLAY + replayName + Program.EXPANSION_YRP3D);
                        var writer = new System.IO.BinaryWriter(stream);
                        int k = 0;
                        for (int j = startI - 1; j < packagesInRecord.Count; j++)
                        {
                            k++;
                            writer.Write((byte)packagesInRecord[j].Function);
                            writer.Write((uint)packagesInRecord[j].Data.GetLength());
                            writer.Write(packagesInRecord[j].Data.Get());
                        }
                        stream.Flush();
                        writer.Close();
                        stream.Close();
                        if (Controllers.OcgCore.duelEnded)
                            packagesInRecord.Clear();
                    }
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.Log(e);
            }
        }

        public static void AddRecordLine(Package p)
        {
            if (Controllers.OcgCore.condition != Controllers.OcgCore.Condition.Replay)
                packagesInRecord.Add(p);
        }

        public static bool IsPortAvailable(int port)
        {
            if (port < System.Net.IPEndPoint.MinPort || port > System.Net.IPEndPoint.MaxPort)
            {
                throw new System.ArgumentException($"指定的端口号 {port} 超出有效范围。");
            }

            bool? systemQueryResult = IsPortOccupiedBySystem(port);
            if (systemQueryResult == true)
                return false;

            return TryBindSocket(port);
        }

        private static bool? IsPortOccupiedBySystem(int port)
        {
            try
            {
                System.Net.NetworkInformation.IPGlobalProperties ipProperties = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
                System.Net.IPEndPoint[] activeTcpListeners = ipProperties.GetActiveTcpListeners();

                bool isInUse = activeTcpListeners.Any(endpoint => endpoint.Port == port);
                return isInUse;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        private static bool TryBindSocket(int port)
        {
            try
            {
                using System.Net.Sockets.Socket socket = new(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                socket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port));
                return true;
            }
            catch (System.Net.Sockets.SocketException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }

    }

    public class Package
    {
        public BinaryMaster Data;
        public int Function;

        public Package()
        {
            Function = (int)MDPro3.Duel.YGOSharp.CtosMessage.Response;
            Data = new BinaryMaster();
        }
    }

    public class BinaryMaster
    {
        private System.IO.MemoryStream memstream;
        public System.IO.BinaryReader reader;
        public System.IO.BinaryWriter writer;

        public BinaryMaster(byte[] raw = null)
        {
            if (raw == null)
                memstream = new System.IO.MemoryStream();
            else
                memstream = new System.IO.MemoryStream(raw);
            reader = new System.IO.BinaryReader(memstream);
            writer = new System.IO.BinaryWriter(memstream);
        }

        public void Set(byte[] raw)
        {
            memstream = new System.IO.MemoryStream(raw);
            reader = new System.IO.BinaryReader(memstream);
            writer = new System.IO.BinaryWriter(memstream);
        }

        public byte[] Get()
        {
            var bytes = memstream.ToArray();
            return bytes;
        }

        public int GetLength()
        {
            return (int)memstream.Length;
        }

        public override string ToString()
        {
            var return_value = "";
            var bytes = Get();
            for (var i = 0; i < bytes.Length; i++)
            {
                return_value += ((int)bytes[i]).ToString();
                if (i < bytes.Length - 1) return_value += ",";
            }

            return return_value;
        }
    }

    public static class BinaryExtensions
    {
        public static void WriteUnicode(this System.IO.BinaryWriter writer, string text, int len)
        {
            try
            {
                var unicode = System.Text.Encoding.Unicode.GetBytes(text);
                var result = new byte[len * 2];
                for (var i = 0; i < result.Length; i++) result[i] = 204;
                var max = len * 2 - 2;
                System.Array.Copy(unicode, result, unicode.Length > max ? max : unicode.Length);
                result[unicode.Length] = 0;
                result[unicode.Length + 1] = 0;
                writer.Write(result);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.Log(e);
            }
        }

        public static string ReadUnicode(this System.IO.BinaryReader reader, int len)
        {
            var unicode = reader.ReadBytes(len * 2);
            var text = System.Text.Encoding.Unicode.GetString(unicode);
            text = text[..text.IndexOf('\0')];
            return text;
        }

        public static string ReadALLUnicode(this System.IO.BinaryReader reader)
        {
            var unicode = reader.ReadToEnd();
            var text = System.Text.Encoding.Unicode.GetString(unicode);
            text = text.Substring(0, text.IndexOf('\0'));
            return text;
        }

        public static byte[] ReadToEnd(this System.IO.BinaryReader reader)
        {
            return reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
        }

        public static Duel.GPS ReadGPS(this System.IO.BinaryReader reader)
        {
            Duel.GPS a = new Duel.GPS();
            a.controller = (uint)Controllers.OcgCore.LocalPlayer(reader.ReadByte());
            a.location = reader.ReadByte();
            a.sequence = reader.ReadByte();
            a.position = reader.ReadByte();
            return a;
        }

        public static Duel.GPS ReadShortGPS(this System.IO.BinaryReader reader)
        {
            Duel.GPS a = new Duel.GPS();
            a.controller = (uint)Controllers.OcgCore.LocalPlayer(reader.ReadByte());
            a.location = reader.ReadByte();
            a.sequence = reader.ReadByte();
            a.position = (int)MDPro3.Duel.YGOSharp.CardPosition.FaceUpAttack;
            return a;
        }

        public static void ReadCardData(this System.IO.BinaryReader r, Duel.GameCard cardTemp = null)
        {
            Duel.GameCard cardToRefresh = cardTemp;
            var flag = r.ReadInt32();
            var code = 0;
            Duel.GPS gps = new Duel.GPS();

            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.Code) != 0)
                code = r.ReadInt32();
            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.Position) != 0)
            {
                gps = r.ReadGPS();
                cardToRefresh = Program.instance.ocgcore.GCS_Get(gps);
            }


            if (cardToRefresh == null)
            {
                //if(code != 0)
                //    Debug.Log("TcpHelper未找到：" + CardsManager.Get(code).Name);
                return;
            }

            var data = cardToRefresh.GetData();

            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.Code) != 0)
                if (data.Id != code)
                {
                    data = MDPro3.Duel.YGOSharp.CardsManager.Get(code);
                    data.Id = code;
                }
            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.Position) != 0) cardToRefresh.p = gps;
            if (data.Id > 0)
                if ((cardToRefresh.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Hand) > 0)
                    if (cardToRefresh.p.controller == 1)
                        cardToRefresh.p.position = (int)MDPro3.Duel.YGOSharp.CardPosition.FaceUpAttack;

            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.Alias) != 0)
                data.Alias = r.ReadInt32();
            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.Type) != 0)
                data.Type = r.ReadInt32();

            var l1 = 0;
            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.Level) != 0) l1 = r.ReadInt32();
            var l2 = 0;
            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.Rank) != 0) l2 = r.ReadInt32();

            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.Attribute) != 0)
                data.Attribute = r.ReadInt32();
            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.Race) != 0)
                data.Race = r.ReadInt32();
            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.Attack) != 0)
                data.Attack = r.ReadInt32();
            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.Defence) != 0)
                data.Defense = r.ReadInt32();
            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.BaseAttack) != 0)
                data.rAttack = r.ReadInt32();
            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.BaseDefence) != 0)
                data.rDefense = r.ReadInt32();
            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.Reason) != 0)
                data.Reason = r.ReadInt32();
            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.ReasonCard) != 0)
                data.ReasonCard = r.ReadInt32();
            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.EquipCard) != 0)
                cardToRefresh.AddTarget(Program.instance.ocgcore.GCS_Get(r.ReadGPS()));
            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.TargetCard) != 0)
            {
                var count = r.ReadInt32();
                for (var i = 0; i < count; ++i)
                    cardToRefresh.AddTarget(Program.instance.ocgcore.GCS_Get(r.ReadGPS()));
            }

            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.OverlayCard) != 0)
            {
                var overs = Program.instance.ocgcore.GCS_GetOverlays(cardToRefresh);
                var count = r.ReadInt32();
                for (var i = 0; i < count; ++i)
                    if (i < overs.Count)
                        overs[i].SetCode(r.ReadInt32());
                    else
                        r.ReadInt32();
            }
            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.Counters) != 0)
            {
                var count = r.ReadInt32();
                for (var i = 0; i < count; ++i)
                    r.ReadInt32();
            }

            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.Owner) != 0)
                r.ReadInt32();
            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.Status) != 0)
            {
                var status = r.ReadInt32();
                cardToRefresh.Disabled = (status & 0x0001) == 0x0001;
                cardToRefresh.SemiNomiSummoned = (status & 0x0008) == 0x0008;
            }

            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.LScale) != 0)
                data.LScale = r.ReadInt32();
            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.RScale) != 0)
                data.RScale = r.ReadInt32();
            var l3 = 0;
            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.Link) != 0)
            {
                l3 = r.ReadInt32(); //link value
                data.LinkMarker = r.ReadInt32();
            }

            if ((flag & (int)MDPro3.Duel.YGOSharp.Query.Level) != 0 || (flag & (int)MDPro3.Duel.YGOSharp.Query.Rank) != 0 || (flag & (int)MDPro3.Duel.YGOSharp.Query.Link) != 0)
            {
                if (l1 > l2)
                    data.Level = l1;
                else
                    data.Level = l2;
                if (l3 > data.Level)
                    data.Level = l3;
            }
            cardToRefresh.SetData(data);
        }
    }

    public class SocketMaster
    {
        private static byte[] ReadFull(System.Net.Sockets.NetworkStream stream, int length)
        {
            var buf = new byte[length];
            var rlen = 0;
            while (rlen < buf.Length)
            {
                var currentLength = stream.Read(buf, rlen, buf.Length - rlen);
                rlen += currentLength;
                if (currentLength == 0)
                {
                    throw new System.IO.EndOfStreamException("The duel connection closed before the packet was complete.");
                }
            }
            return buf;
        }

        public static byte[] ReadPacket(System.Net.Sockets.NetworkStream stream)
        {
            var hdr = ReadFull(stream, 2);
            var plen = System.BitConverter.ToUInt16(hdr, 0);
            var buf = ReadFull(stream, plen);
            return buf;
        }
    }

    public class TcpClientWithTimeout
    {
        protected string _hostname;
        protected int _port;
        protected int _timeout_milliseconds;
        protected bool connected;
        protected System.Net.Sockets.TcpClient connection;
        protected System.Exception exception;

        public TcpClientWithTimeout(string hostname, int port, int timeout_milliseconds)
        {
            _hostname = hostname;
            _port = port;
            _timeout_milliseconds = timeout_milliseconds;
        }

        private readonly object connectGate = new object();
        private volatile bool cancelled;

        public System.Net.Sockets.TcpClient Connect() => Connect(null);

        public System.Net.Sockets.TcpClient Connect(System.Func<bool> canContinue)
        {
            connected = false;
            exception = null;
            cancelled = false;
            var thread = new System.Threading.Thread(BeginConnect) { IsBackground = true, Name = "MDPro3 socket connect" };
            thread.Start();
            var deadline = System.DateTime.UtcNow.AddMilliseconds(_timeout_milliseconds);
            while (thread.IsAlive && System.DateTime.UtcNow < deadline)
            {
                if (canContinue != null && !canContinue())
                {
                    lock (connectGate)
                    {
                        cancelled = true;
                        try { connection?.Close(); } catch { }
                    }
                    throw new System.OperationCanceledException();
                }
                thread.Join(25);
            }
            lock (connectGate)
            {
                if (connected && (canContinue == null || canContinue())) return connection;
                cancelled = true;
                try { connection?.Close(); } catch { }
                if (exception != null) throw exception;
                throw new System.TimeoutException("The duel connection attempt timed out.");
            }
        }

        protected void BeginConnect()
        {
            try
            {
                var client = new System.Net.Sockets.TcpClient(_hostname, _port);
                lock (connectGate)
                {
                    if (cancelled) { client.Close(); return; }
                    connection = client;
                    connected = true;
                }
            }
            catch (System.Exception ex)
            {
                lock (connectGate) exception = ex;
            }
        }


    }


}
