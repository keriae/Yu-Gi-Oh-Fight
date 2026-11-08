using System;


using UnityEngine;

using Newtonsoft.Json;


using Cysharp.Threading.Tasks;


namespace YuGiOhFight.Client.Services
{
    public static class MyCard
    {
        public const string duelUrl = "tiramisu.moenext.com";
        public const int entertainPort = 7911;
        public const int athleticPort = 8911;

        const string loginUrl = "https://sapi.moecube.com:444/accounts/signin";
        const string authUrl = "https://sapi.moecube.com:444/accounts/authUser";
        const string appsUrl = "https://cdntx.moecube.com/apps.json";
        const string expUrl = "https://sapi.moecube.com:444/ygopro/arena/user?username=";
        const string matchUrl = "https://sapi.moecube.com:444/ygopro/match";
        const string userUrl = "https://sapi.moecube.com:444/accounts/users/{username}.json";
        const string athleticWatchUrl = "wss://tiramisu.moenext.com:8923?filter=started";
        const string entertainWatchUrl = "wss://tiramisu.moenext.com:7923?filter=started";
        const string contentTypeHeader = "Content-Type";
        const string jsonHeader = "application/json";
        const string authHeader = "Authorization";
        public static MyCardAccount account;
        public static MyCardApp ygopro;
        public static Texture2D avatar;

        private const string avatarSavePath = "Picture/MyCardAvatars/";
        private static readonly System.Collections.Generic.Dictionary<string, string> cachedAvatarAddress = new();
        private static readonly System.Collections.Generic.Dictionary<string, Texture2D> cachedAvatars = new();
        private const int AVATAR_SIZE = 256;

        public static async System.Threading.Tasks.Task<MyCardAccount> Login(string account, string password)
        {
            var data = new { account = account, password = password};
            string json = JsonConvert.SerializeObject(data);

            using var request = UnityEngine.Networking.UnityWebRequest.Post(loginUrl, json, jsonHeader);

            request.SetRequestHeader("Content-Type", jsonHeader);
            request.SetRequestHeader("Origin", "https://accounts.moecube.com");
            request.SetRequestHeader("Referer", "https://accounts.moecube.com/");

            var send = request.SendWebRequest();
            await MDPro3.Utility.TaskUtility.WaitUntil(() => send.isDone);
            if(!Application.isPlaying)
                return null;

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                MyCard.account = JsonUtility.FromJson<MyCardAccount>(request.downloadHandler.text);

                if(MDPro3.Config.GetBool("SyncDeck", true))
                {
                    var response = MDPro3.Net.OnlineDeck.GetAllDecks();
                    await MDPro3.Utility.TaskUtility.WaitUntil(() => response.IsCompleted);
                    if (!Application.isPlaying)
                        return null;
                }

                return MyCard.account;
            }
            else
            {
                var returnValue = new MyCardAccount();
                returnValue.user = new MyCardUserInfo();
                returnValue.user.username = JsonUtility.FromJson<MyCardMessage>(request.downloadHandler.text).message;
                return returnValue;
            }
        }

        public static async System.Threading.Tasks.Task<MyCardAccount> TokenIn(string token)
        {
            using var request = UnityEngine.Networking.UnityWebRequest.Get(authUrl);
            request.SetRequestHeader(authHeader, "Bearer " + token);
            try
            {
                await request.SendWebRequest();
            }
            catch (Exception e)
            {
                var error = e.ToString();
                Debug.Log(e);
                if(error.Contains("Authentication Error"))
                {
                    Runtime.MessageManager.Cast(MDPro3.InterString.Get("MyCard登陆信息已过期，请重新登陆。"));
                    MDPro3.Config.Set("MyCardToken", MDPro3.Config.STRING_NO);
                    MDPro3.Config.Save();
                }
                else
                {
                    Runtime.MessageManager.Cast(MDPro3.InterString.Get("MyCard登录失败。") + error);
                }
                return null;
            }

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                var user = JsonUtility.FromJson<MyCardUserInfo>(request.downloadHandler.text);
                account = new MyCardAccount
                {
                    user = user,
                    token = token
                };

                if (MDPro3.Config.GetBool("SyncDeck", true))
                {
                    var response = MDPro3.Net.OnlineDeck.GetAllDecks();
                    await response;
                    await MDPro3.Utility.TaskUtility.WaitUntil(() => response.IsCompleted);
                    if (!Application.isPlaying)
                        return null;
                }

                return account;
            }
            else
            {
                var returnValue = new MyCardAccount
                {
                    user = new MyCardUserInfo()
                };
                var message = JsonUtility.FromJson<MyCardMessage>(request.downloadHandler.text);
                if (message != null)
                    returnValue.user.username = JsonUtility.FromJson<MyCardMessage>(request.downloadHandler.text).message;
                else
                    returnValue.user.username = MDPro3.InterString.Get("请检查网络连接");
                return returnValue;
            }
        }

        public static async UniTask<Texture2D> GetAvatarAsync(string userName, System.Threading.CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if(!System.IO.Directory.Exists(avatarSavePath))
                System.IO.Directory.CreateDirectory(avatarSavePath);

            string fullPath = string.Empty;
            string avatarName = string.Empty;
            bool cached = false;
            lock (cachedAvatarAddress)
            {
                if (cachedAvatarAddress.TryGetValue(userName, out avatarName))
                {
                    fullPath = avatarSavePath + avatarName + Runtime.Program.EXPANSION_PNG;
                    cached = true;
                }
            }

            if (cached)
            {
                lock (cachedAvatars)
                    if (cachedAvatars.ContainsKey(avatarName))
                        return cachedAvatars[avatarName];

                var pic = await Runtime.TextureManager.LoadPicFromFileAsync(fullPath);
                cancellationToken.ThrowIfCancellationRequested();

                lock (cachedAvatars)
                    if (!cachedAvatars.ContainsKey(avatarName))
                        cachedAvatars[avatarName] = pic;
                return pic;
            }

            string avatarAddress;

            using(var request = UnityEngine.Networking.UnityWebRequest.Get(userUrl.Replace("{username}", userName)))
            {
                await request.SendWebRequest().WithCancellation(cancellationToken);
                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    avatarAddress = JsonUtility.FromJson<MyCardRoomUserInfo>(request.downloadHandler.text).user.avatar;
                }
                else
                {
                    Debug.LogError($"Failed to get user info: {request.error}");
                    return null;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            var requestAvatar = Utility.Tools.DownloadImageAsync(avatarAddress, cancellationToken);
            await requestAvatar;
            cancellationToken.ThrowIfCancellationRequested();
            Texture2D downloadImage = requestAvatar.Result;
            if (downloadImage == null)
                return null;

            var returnValue = downloadImage;
            try
            {
                if(downloadImage != null)
                {
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(avatarAddress);
                    fullPath = avatarSavePath + fileName + Runtime.Program.EXPANSION_PNG;
                    if (downloadImage.width > AVATAR_SIZE)
                    {
                        returnValue = Presentation.TextureProcessor.ResizeTexture2D(downloadImage, AVATAR_SIZE, AVATAR_SIZE);
                        UnityEngine.Object.Destroy(downloadImage);
                    }

                    System.IO.File.WriteAllBytes(fullPath, returnValue.EncodeToPNG());
                    lock (cachedAvatarAddress)
                    {
                        if (!cachedAvatarAddress.ContainsKey(userName))
                            cachedAvatarAddress[userName] = fileName;
                    }
                    lock(cachedAvatars)
                    {
                        if (!cachedAvatars.ContainsKey(avatarName))
                            cachedAvatars[avatarName] = returnValue;
                    }
                }
            }
            catch { }

            return returnValue;
        }

        public static async System.Threading.Tasks.Task<MyCardNews> GetNews()
        {
            using var request = UnityEngine.Networking.UnityWebRequest.Get(appsUrl);
            var send = request.SendWebRequest();
            await MDPro3.Utility.TaskUtility.WaitUntil(() => send.isDone);
            if(!Application.isPlaying)
                return null;

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                var json = request.downloadHandler.text;
                json = json.Replace("\"news\":[]", "\"news\":{}");
                var apps = JsonConvert.DeserializeObject<MyCardApp[]>(json);
                foreach(var app in apps)
                    if (app.id == "ygopro")
                    {
                        ygopro = app;
                        return app.news;
                    }
                return null;
            }
            else
            {
                Debug.LogError($"Failed to get apps: {request.error}");
                return null;
            }
        }

        public static async System.Threading.Tasks.Task<MyCardUserExp> GetExp()
        {
            if (account == null)
                return null;

            using var request = UnityEngine.Networking.UnityWebRequest.Get(expUrl + $"{account.user.username}");
            var send = request.SendWebRequest();
            await MDPro3.Utility.TaskUtility.WaitUntil(() => send.isDone);
            if(!Application.isPlaying)
                return null;

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                return JsonConvert.DeserializeObject<MyCardUserExp>(request.downloadHandler.text);
            else
            {
                Debug.LogError($"Failed to get Exp: {request.error}");
                return null;
            }
        }

        public static async System.Threading.Tasks.Task<MyCardMatchInfo> GetMatchInfo(string arena)
        {
            using var request = UnityEngine.Networking.UnityWebRequest.PostWwwForm(matchUrl + "?arena=" + arena, jsonHeader);
            var u16Secret = await GetUserU16SecretAsync();
            request.SetRequestHeader(contentTypeHeader, jsonHeader);
            request.SetRequestHeader(authHeader, "Basic " + CustomBase64Encode(account.user.username + ":" + u16Secret));

            var send = request.SendWebRequest();
            await MDPro3.Utility.TaskUtility.WaitUntil(() => send.isDone);
            if(!Application.isPlaying)
                return null;

            if(request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                return JsonUtility.FromJson<MyCardMatchInfo>(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"{arena} Match error: {request.error}");
                return null;
            }
        }

        private static string CustomBase64Encode(string input)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(bytes);
        }

        #region WebSocket Athletic Watch

        private static string currentEventType;

        private static UnityWebSocket.WebSocket socket;

        public static void ConnectToAthleticWatchListWebSocket()
        {
            socket = new UnityWebSocket.WebSocket(athleticWatchUrl);
            socket.OnOpen += OnSocketOpen;
            socket.OnClose += OnSocketClose;
            socket.OnMessage += OnSocketMessage;
            socket.OnError += OnSocketError;

            socket.ConnectAsync();
        }

        public static void CloseAthleticWatchListWebSocket()
        {
            try
            {
                socket?.CloseAsync();
                Runtime.Program.instance.online.ClearWatchList();
            }
            catch { }

            socket = null;
        }

        private static void OnSocketOpen(object sender, UnityWebSocket.OpenEventArgs e)
        {
            Debug.Log(string.Format("Socket Connected: {0}", athleticWatchUrl));
        }

        private static void OnSocketClose(object sender, UnityWebSocket.CloseEventArgs e)
        {
            Debug.Log(string.Format("Socket Closed: StatusCode: {0}, Reason: {1}", e.StatusCode, e.Reason));
        }

        private static void OnSocketError(object sender, UnityWebSocket.ErrorEventArgs e)
        {
            Debug.Log(string.Format("Socket Error: {0}", e.Message));
        }

        private static void OnSocketMessage(object sender, UnityWebSocket.MessageEventArgs e)
        {
            //Debug.Log(string.Format("Receive: {0}", e.Data));
            HandleWatchInfo(e.Data);
        }

        private static void HandleWatchInfo(string json)
        {
            var info = JsonConvert.DeserializeObject<MyCardWatchInfo>(json);
            currentEventType = info.eventType;
            switch (currentEventType)
            {
                case "init":
                case "create":
                case "update":
                    var roomsOrSingleRoom = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JToken>(info.data.ToString());
                    if(roomsOrSingleRoom is Newtonsoft.Json.Linq.JArray array)
                        HandleRooms(array);
                    else
                        HandleSingleRoom((Newtonsoft.Json.Linq.JObject)roomsOrSingleRoom);
                    break;
                case "delete":
                    HandleDelete(info.data as string);
                    break;
                default:
                    Debug.LogWarning("Unknown event type: " + info.eventType);
                    break;
            }
        }

        private static void HandleRooms(Newtonsoft.Json.Linq.JArray rooms)
        {
            var list = new System.Collections.Generic.List<MyCardRoom>();
            foreach(var room in rooms)
            {
                var roomInstance = room.ToObject<MyCardRoom>();
                list.Add(roomInstance);
            }
            Runtime.Program.instance.online.SetWatchRooms(list);
        }

        private static void HandleSingleRoom(Newtonsoft.Json.Linq.JObject room)
        {
            var roomInstance = room.ToObject<MyCardRoom>();
            switch(currentEventType)
            {
                case "create":
                    Runtime.Program.instance.online.CreateWatchRoom(roomInstance);
                    break;
                case "update":
                    Runtime.Program.instance.online.UpdateWatchRoom(roomInstance);
                    break;
            }
        }

        private static void HandleDelete(string roomId)
        {
            Runtime.Program.instance.online.DeleteWatchRoom(roomId);
        }

        #endregion

        #region MyCard Tools

        public enum RoomAction
        {
            CreatePublic = 1,
            CreatePrivate = 2,
            JoinPublic = 3,
            JoinPrivate = 5,
        }

        public static async UniTask<string> GetJoinRoomPassword(MyCardRoomOptions options, string roomId, int userId, bool _private = false)
        {
            byte[] optionsBuffer = new byte[6];
            optionsBuffer[1] = (byte)((_private ? (int)RoomAction.JoinPrivate : (int)RoomAction.JoinPublic) << 4);

            await EncryptBuffer(optionsBuffer);

            string base64String = Convert.ToBase64String(optionsBuffer);

            return base64String + roomId;
        }

        public static async System.Threading.Tasks.Task<string> GetCreateRoomPasswd(MyCardRoomOptions options, string roomID, int userId, bool _private = false)
        {
            byte[] optionsBuffer = new byte[6];
            optionsBuffer[1] = (byte)(((byte)(_private ? RoomAction.CreatePrivate : RoomAction.CreatePublic) << 4) | (byte)(options.duel_rule << 1) | (options.auto_death ? 0x1 : 0));

            optionsBuffer[2] = (byte)(((byte)options.rule << 5) | ((byte)options.mode << 3) | (options.no_check_deck ? 1 << 1 : 0) | (options.no_shuffle_deck ? 1 : 0));
            WriteUInt16LE(optionsBuffer, 3, (ushort)options.start_lp);
            optionsBuffer[5] = (byte)(((byte)options.start_hand << 4) | options.draw_count);

            await EncryptBuffer(optionsBuffer);
            string base64String = Convert.ToBase64String(optionsBuffer);

            return base64String + roomID;
        }

        private static async UniTask EncryptBuffer(byte[] buffer)
        {
            int checksum = 0;

            for (int i = 1; i < buffer.Length; i++)
            {
                checksum -= buffer[i];
            }

            buffer[0] = (byte)(checksum & 0xff);

            var u16Secret = await GetUserU16SecretAsync();
            int secret = u16Secret % 65535 + 1;

            for (int i = 0; i < buffer.Length; i += 2)
            {
                ushort value = ReadUInt16LE(buffer, i);
                ushort xorResult = (ushort)(value ^ secret);
                WriteUInt16LE(buffer, i, xorResult);
            }
        }

        public static int GetPrivateRoomID(int external_id)
        {
            return external_id ^ 0x54321;
        }

        private static ushort ReadUInt16LE(byte[] buffer, int offset)
        {
            return (ushort)((buffer[offset + 1] << 8) | buffer[offset]);
        }

        private static void WriteUInt16LE(byte[] buffer, int offset, ushort value)
        {
            buffer[offset] = (byte)(value & 0xff);
            buffer[offset + 1] = (byte)((value >> 8) & 0xff);
        }

        [Serializable]
        private class AuthResponse
        {
            public int u16Secret;
        }

        private static async UniTask<int> GetUserU16SecretAsync()
        {
            static void Bad(string message)
            {
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("MyCard: 获取用户密钥失败。请重新登录。([?])", message));
                throw new Exception($"Get U16 secret failed: {message}");
            }

            if(account == null)
                Bad("No account info");
            if (string.IsNullOrEmpty(account.token))
                Bad("no token");

            var token = account.token;
            using var request = UnityEngine.Networking.UnityWebRequest.Get(authUrl);
            request.SetRequestHeader("Authorization", $"Bearer {token}");
            request.SetRequestHeader("Content-Type", "application/json");
            await request.SendWebRequest();
            if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Bad(request.error);
                return 0;
            }

            try
            {
                var jsonResponse = request.downloadHandler.text;
                var authInfo = JsonUtility.FromJson<AuthResponse>(jsonResponse);
                if (authInfo == null || authInfo.u16Secret == 0)
                {
                    Bad("no secret or invalid response");
                    return 0;
                }

                return authInfo.u16Secret;
            }
            catch (Exception e)
            {
                Bad(e.ToString());
                return 0;
            }
        }

        #endregion
    }

    [Serializable]
    public class MyCardAccount
    {
        public MyCardUserInfo user;
        public string token;
    }

    [Serializable]
    public class MyCardRoomUserInfo
    {
        public MyCardUserInfo user;
    }

    [Serializable]
    public class MyCardUserInfo
    {
        public int id;
        public string username;
        public string name;
        public string email;
        public string password_hash;
        public string salt;
        public bool active;
        public bool admin;
        public string avatar;
        public string locale;
        public string registration_ip_address;
        public string ip_address;
        public string created_at;
        public string updated_at;
    }

    [Serializable]
    public class MyCardMessage
    {
        public string message;
    }

    [Serializable]
    public class MyCardApp
    {
        public string id;
        public MyCardServers[] servers;
        public MyCardWindbots[] windbot;
        public MyCardNews news;
    }

    [Serializable]
    public class MyCardServers
    {
        public string id;
        public string url;
        public string name;
        public int port;
        public bool custom;
        public bool replay;
        public string address;
        public string[] windbot;
    }

    [Serializable]
    public class MyCardWindbots
    {
        [JsonProperty("en-US")]
        public string[] EnglishUS { get; set; }
        [JsonProperty("zh-CN")]
        public string[] ChineseCN { get; set; }
    }

    [Serializable]
    public class MyCardNews
    {
        [JsonProperty("en-US")]
        public News[] EnglishUS { get; set; }
        [JsonProperty("zh-CN")]
        public News[] ChineseCN { get; set; }
    }

    [Serializable]
    public class News
    {
        public string url;
        public string text;
        public string image;
        public string title;
        public string updated_at;
    }

    [Serializable]
    public class MyCardUserExp
    {
        public int exp;
        public int pt;
        public int entertain_win;
        public int entertain_lose;
        public int entertain_draw;
        public int entertain_all;
        public string entertain_wl_ratio;
        public int exp_rank;
        public int athletic_win;
        public int athletic_lose;
        public int athletic_draw;
        public int athletic_all;
        public string athletic_wl_ratio;
        public int arena_rank;
    }

    [Serializable]
    public class MyCardMatchInfo
    {
        public string address;
        public int port;
        public string password;
    }

    [Serializable]
    public class MyCardWatchInfo
    {
        [JsonProperty("event")]
        public string eventType;
        public object data;
    }

    [Serializable]
    public class MyCardRoom
    {
        public string id;
        public string title;
        public MyCardRoomUser user;
        public MyCardRoomUser[] users;
        public MyCardRoomOptions options;
        public string arena;
    }

    [Serializable]
    public class MyCardRoomUser
    {
        public string username;
        public int position;
    }

    [Serializable]
    public class MyCardRoomOptions
    {
        public int lflist;
        public int rule;
        public int mode;
        public int duel_rule;
        public bool no_check_deck;
        public bool no_shuffle_deck;
        public int start_lp;
        public int start_hand;
        public int draw_count;
        public int time_limit;
        public bool no_watch;
        public bool auto_death;
        public int replay_mode;
    }

}
