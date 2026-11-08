using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;


namespace YuGiOhFight.Client.Controllers
{
    public class OnlineServant : Servant
    {
        [HideInInspector] public UI.SelectionToggle_Address lastSelectedAddressItem;
        [HideInInspector] public UI.SelectionToggle_Watch lastSelectedWatchItem;

        public const int DEFAULT_TIME = 180;
        public const int DEFAULT_PORT = 7911;
        public const int DEFAULT_LP = 8000;
        public const int DEFAULT_HAND = 5;
        public const int DEFAULT_DRAW = 1;

        #region Servant

        public override int Depth => 1;
        protected override bool ShowLine => true;

        public override void Initialize()
        {
            returnServant = Runtime.Program.instance.menu;
            base.Initialize();
            TryTokenIn();
        }

        protected override void ApplyShowArrangement(int preDepth)
        {
            if (Runtime.Program.exitOnReturn)
            {
                Runtime.Program.GameQuit();
                return;
            }

            base.ApplyShowArrangement(preDepth);
            if (servantUI != null)
                RefreshDeckSelector();
        }

        protected override void FirstLoadEvent()
        {
            base.FirstLoadEvent();
            RefreshDeckSelector();
            if (logined)
                GetUI<UI.ServantUI.OnlineServantUI>().PageMyCard.DisablePageLogin();
            RefreshMyCardAssets();
            Services.MyCard.ConnectToAthleticWatchListWebSocket();
        }

        protected override void ApplyHideArrangement(int preDepth)
        {
            base.ApplyHideArrangement(preDepth);
            MDPro3.Config.Save();
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !Runtime.UserInput.NeedDefaultSelect())
                return;

            if (servantUI != null)
                GetUI<UI.ServantUI.OnlineServantUI>().SelectLastSelectable(lastSelectable);
        }

        public override void PerFrameFunction()
        {
            if (!NeedResponseInput())
                return;

            if (Runtime.UserInput.MouseRightDown || Runtime.UserInput.WasCancelPressed)
                OnReturn();
            if (Runtime.UserInput.WasLeftShoulderPressed)
                GetUI<UI.ServantUI.OnlineServantUI>().PageLeft();
            if (Runtime.UserInput.WasRightShoulderPressed)
                GetUI<UI.ServantUI.OnlineServantUI>().PageRight();
        }

        #endregion

        #region Server connections

        public void KF_OnlineGame(string name, string ip, string port, string password)
        {
            if (string.IsNullOrEmpty(name))
            {
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("用户名不能为空。"));
                return;
            }

            if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(port))
            {
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("主机地址和端口不能为空。"));
                return;
            }

            RoomServant.FromSolo = false;
            RoomServant.FromLocalHost = false;
            RoomServant.FromHandTest = false;
            Runtime.Network.TcpHelper.LinkStart(ip, name, port, password, false, null);
        }

        #endregion

        #region Local Host

        public void CreateServer(List<string> serverArgs)
        {
            int port = 7911;
            while (!Runtime.Network.TcpHelper.IsPortAvailable(port))
            {
                port++;
                if (port == 65536)
                    port = 1;
            }

            string args = string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}",
                port.ToString(),
                MDPro3.Duel.YGOSharp.BanlistManager.GetIndexByName(serverArgs[1]),
                GetPoolCodeByName(serverArgs[2]),
                GetModeCodeByName(serverArgs[3]),
                "F",
                serverArgs[4],
                serverArgs[5],
                serverArgs[7],
                serverArgs[8],
                serverArgs[9],
                serverArgs[6],
                "0");

            RoomServant.FromSolo = false;
            RoomServant.FromLocalHost = true;
            RoomServant.FromHandTest = false;

            MDPro3.Net.YgoServer.StartServer(args);
            Runtime.Network.TcpHelper.LinkStart("127.0.0.1", MDPro3.Config.Get("DuelPlayerName0", MDPro3.Config.EMPTY_STRING), port.ToString(), string.Empty, true, null);
        }

        private string GetPoolCodeByName(string pool)
        {
            for (int i = 1481; i < 1487; i++)
            {
                if (Presentation.StringHelper.GetUnsafe(i) == pool)
                    return (i - 1481).ToString();
            }
            return "5";
        }

        private string GetModeCodeByName(string mode)
        {
            for (int i = 1244; i < 1247; i++)
            {
                if (Presentation.StringHelper.GetUnsafe(i) == mode)
                    return (i - 1244).ToString();
            }
            return "0";
        }

        #endregion

        #region MyCard

        private bool logined;
        private bool assetsRefreshed;

        public void LoginMyCard(string name, string passwd)
        {
            StartCoroutine(LoginMyCardAsync(name, passwd));
        }

        private IEnumerator LoginMyCardAsync(string account, string passwd)
        {
            GetUI<UI.ServantUI.OnlineServantUI>().PageMyCard.PageLogin.SetActive(false);

            var task = Services.MyCard.Login(account, passwd);
            while(!task.IsCompleted)
                yield return null;
            if (task.Result.user.id == 0)
            {
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("登录失败：") + task.Result.user.username);
                GetUI<UI.ServantUI.OnlineServantUI>().PageMyCard.PageLogin.SetActive(true);
                if (Runtime.UserInput.NeedDefaultSelect())
                    Select();
                yield break;
            }
            MDPro3.Config.Set("MyCardToken", task.Result.token);
            MDPro3.Config.Save();
            LoginSuccessEvent();
        }

        private void TryTokenIn()
        {
            StartCoroutine(TryTokenInAsync());
        }

        private IEnumerator TryTokenInAsync()
        {
            var token = MDPro3.Config.Get("MyCardToken", MDPro3.Config.STRING_NO);
            if(token == MDPro3.Config.STRING_NO)
                yield break;
            var task = Services.MyCard.TokenIn(token);
            while (!task.IsCompleted)
                yield return null;

            if(task.Result == null)
            {
                yield break;
            }

            if (task.Result.user.id == 0)
            {
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("MyCard登录失败。"));
                yield break;
            }
            LoginSuccessEvent();
        }

        private void LoginSuccessEvent()
        {
            if (Services.MyCard.account == null || Services.MyCard.account.user.id == 0)
                return;

            logined = true;
            _ = MDPro3.Net.OnlineService.SyncRarityConfigAsync();
            StartCoroutine(SyncDecks());
            RefreshMyCardAssets();
        }

        private void RefreshMyCardAssets()
        {
            if (servantUI == null || !logined)
                return;
            StartCoroutine(RefreshMyCardAssetsAsync());
        }

        private IEnumerator RefreshMyCardAssetsAsync()
        {
            if(assetsRefreshed)
                yield break;

            UI.ServantUI.OnlineServantUI onlineUI = GetUI<UI.ServantUI.OnlineServantUI>();
            onlineUI.PageMyCard.DisablePageLogin();
            UI.UserProfile userProfile = onlineUI.PageMyCard.UserProfile;

            var task = Services.MyCard.GetExp();
            while (!task.IsCompleted)
                yield return null;

            Services.MyCardUserExp expData = null;
            if (!task.IsCanceled && task.Exception == null)
                expData = task.Result;
            userProfile.SetProfile(expData);

            while (!Appearance.loaded)
                yield return null;
            if (userProfile.Avatar != null)
                userProfile.Avatar.material = Appearance.duelFrameMat0;

            if (Services.MyCard.avatar == null)
            {
                var avatarAddress = Services.MyCard.account?.user?.avatar;
                if (!string.IsNullOrEmpty(avatarAddress))
                {
                    var avatarTask = Utility.Tools.DownloadImageAsync(avatarAddress);
                    while (!avatarTask.IsCompleted)
                        yield return null;
                    Services.MyCard.avatar = avatarTask.Result;
                }
            }

            if (userProfile.Avatar != null && Services.MyCard.avatar != null)
                userProfile.Avatar.texture = Services.MyCard.avatar;

            onlineUI.PageMyCard.ActivePageFunction();
            assetsRefreshed = true;
        }

        private void RefreshDeckSelector()
        {
            GetUI<UI.ServantUI.OnlineServantUI>().PageMyCard.ButtonDeckSelector.SetConfigDeck(MDPro3.InterString.Get("未选中有效卡组"));
        }

        private float timeError = 3f;

        private string GetDeckNameWithType(string path)
        {
            var type = Path.GetFileName(Path.GetDirectoryName(path));
            if(type == "Deck" && type == Path.GetDirectoryName(path))
                type = string.Empty;
            return (type == string.Empty ? string.Empty : $"{type}/") + Path.GetFileNameWithoutExtension(path);
        }

        private string GetDeckTypeFromPath(string path)
        {
            var type = Path.GetFileName(Path.GetDirectoryName(path));
            if (type == "Deck" && type == Path.GetDirectoryName(path))
                type = string.Empty;
            return type;
        }

        private string GetDeckTypeFromName(string deckName)
        {
            if (!deckName.Contains("/"))
                return string.Empty;
            return deckName.Split('/')[0];
        }

        private IEnumerator SyncDecks()
        {
            if (!MDPro3.Config.GetBool("SyncDeck", true))
                yield break;

            if (MDPro3.Net.OnlineDeck.decks == null)
            {
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("同步卡组失败。"));
                yield break;
            }

            var deckFiles = Directory.GetFiles(Runtime.Program.PATH_DECK, "*.ydk", SearchOption.AllDirectories);
            var decks = new List<MDPro3.Duel.YGOSharp.Deck>();
            foreach (var deckPath in deckFiles)
                decks.Add(new MDPro3.Duel.YGOSharp.Deck(deckPath));

            var decksNeedUpload = new Dictionary<string, MDPro3.Duel.YGOSharp.Deck>();//没在服务器找到对应的deckId的本地卡组
            var decksNeedUpdateToServer = new Dictionary<string, MDPro3.Duel.YGOSharp.Deck>();//找到deckId但本地时间大于服务器时间timeError秒以上的卡组
            var decksNeedUpdateFromServer = new Dictionary<string, MDPro3.Duel.YGOSharp.Deck>();//找到deckId但本地时间小于服务器时间timeError秒以上的卡组
            var localFoundIds = new List<string>();

            for (int i = 0; i < decks.Count; i++)
            {
                var deckName = GetDeckNameWithType(deckFiles[i]);
                var type = GetDeckTypeFromPath(deckFiles[i]);
                decks[i].type = type;

                bool deckIdFound = false;
                foreach (MDPro3.Net.OnlineDeck.OnlineDeckData od in MDPro3.Net.OnlineDeck.decks)
                {
                    if (od.deckId == decks[i].deckId)
                    {
                        deckIdFound = true;
                        localFoundIds.Add(od.deckId);
                        if (od.isDelete)
                        {
                            File.Delete(deckFiles[i]);
                        }
                        else
                        {
                            var fileInfo = new FileInfo(deckFiles[i]);
                            var serverTime = od.GetUpdateUtcTime();
                            var diff = serverTime - fileInfo.LastWriteTimeUtc;

                            //Debug.Log($"{od.deckName}: serverTimeUtc: {od.GetUpdateUtcTime()}, localTime: {fileInfo.LastWriteTimeUtc} diff: {diff.TotalSeconds}");

                            if (diff.TotalSeconds > timeError || diff.TotalSeconds < -timeError)
                            {
                                if (fileInfo.LastWriteTimeUtc > serverTime)
                                    decksNeedUpdateToServer.Add(deckName, decks[i]);
                                else
                                    decksNeedUpdateFromServer.Add(deckName, decks[i]);
                            }
                            if((Path.GetFileName(deckName) != od.deckName || GetDeckTypeFromName(deckName) != od.GetType())
                                && !decksNeedUpdateFromServer.Keys.Contains(deckName)
                                && !decksNeedUpdateToServer.Keys.Contains(deckName))
                            {
                                //Debug.Log($"[{Path.GetFileName(deckName)}] [{od.deckName}] [{GetDeckTypeFromName(deckName)}] [{od.GetType()}]");
                                decksNeedUpdateFromServer.Add(deckName, decks[i]);
                            }
                        }
                        break;
                    }
                }

                if (!deckIdFound)
                    decksNeedUpload.Add(deckName, decks[i]);
            }

            //上传已经有Id的本地较新卡组
            foreach (var deck in decksNeedUpdateToServer)
            {
                Debug.LogFormat("卡组[{0}]需要更新上传。", deck.Key);
                var time = DateTime.UtcNow;
                var task = MDPro3.Net.OnlineDeck.SyncDeck(deck.Value.deckId, Path.GetFileName(deck.Key), deck.Value, time, false);
                while (!task.IsCompleted)
                    yield return null;
            }
            //更新已经有Id的本地较旧卡组
            foreach (var deck in decksNeedUpdateFromServer)
            {
                Debug.LogFormat("卡组[{0}]需要更新。", deck.Key);

                MDPro3.Net.OnlineDeck.OnlineDeckData od = MDPro3.Net.OnlineDeck.GetByID(deck.Value.deckId);
                var oldPath = Runtime.Program.PATH_DECK + deck.Key + Runtime.Program.EXPANSION_YDK;
                if (Path.GetFileName(deck.Key) != od.deckName || deck.Value.type != od.GetType())
                    File.Delete(oldPath);
                var newPath = Runtime.Program.PATH_DECK + (od.GetType() == string.Empty ? string.Empty : $"{od.GetType()}/") + od.deckName + Runtime.Program.EXPANSION_YDK;
                if(!Directory.Exists(Path.GetDirectoryName(newPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                File.WriteAllText(newPath, od.deckYdk);
                File.SetLastWriteTimeUtc(newPath, od.GetUpdateUtcTime());
            }

            //下载本地ID不存在的服务器卡组
            //OnlineDeck.UploadDecks() 会刷新OnlineDeck.decks，因此本功能需要在[上传没有Id的本地卡组]之前执行
            var odtd = MDPro3.Net.OnlineDeck.decks
                .Where(od => !od.isDelete && !localFoundIds.Contains(od.deckId));
            foreach (MDPro3.Net.OnlineDeck.OnlineDeckData deck in odtd)
            {
                var path = Runtime.Program.PATH_DECK + (deck.GetType() == string.Empty ? string.Empty : $"{deck.GetType()}/") + deck.deckName + Runtime.Program.EXPANSION_YDK;
                if (File.Exists(path))
                {
                    Debug.Log($"删除服务器同名卡组 [{deck.GetType()}/{deck.deckName}]  [{deck.deckId}]。");
                    _ = MDPro3.Net.OnlineDeck.DeleteDecks(new List<string> { deck.deckId });
                    continue;
                }
                Debug.Log($"卡组[{deck.GetType()} / {deck.deckName}] [{deck.deckId}]需要下载。");

                MDPro3.Duel.YGOSharp.Deck d = new MDPro3.Duel.YGOSharp.Deck(deck.deckYdk, deck.deckId, Services.MyCard.account.user.username)
                {
                    type = deck.GetType()
                };
                d.Save(Path.GetFileName(deck.deckName), deck.GetUpdateUtcTime(), false);
            }

            //上传没有Id的本地卡组
            if (decksNeedUpload.Count > 0)
            {
                var decksToUp = new List<MDPro3.Duel.YGOSharp.Deck>();
                var deckNames = new List<string>();
                foreach (var deck in decksNeedUpload)
                {
                    var info = string.Format("卡组[{0}]需要上传：{1}。", deck.Key, deck.Value.deckId);
                    Debug.Log(info);

                    deckNames.Add(Path.GetFileName(deck.Key));
                    decksToUp.Add(deck.Value);
                }
                var task2 = MDPro3.Net.OnlineDeck.UploadDecks(decksToUp, deckNames);
                while (!task2.IsCompleted)
                    yield return null;
            }


#if UNITY_EDITOR
            MessageManager.Cast("Deck Sync Finished.");
#endif
        }

        #region MyCard Match

        public static IEnumerator entertainMatch;
        public static IEnumerator athleticMatch;

        public void EntertainMatch()
        {
            if(entertainMatch == null)
            {
                StartCoroutine(entertainMatch = EntertainMatchAsync());
                if(athleticMatch != null)
                {
                    StopCoroutine(athleticMatch);
                    athleticMatch = null;
                }
                GetUI<UI.ServantUI.OnlineServantUI>().PageMyCard.ButtonAMatch.SetButtonText(MDPro3.InterString.Get("竞技匹配"));
            }
            else
            {
                GetUI<UI.ServantUI.OnlineServantUI>().PageMyCard.ButtonEMatch.SetButtonText(MDPro3.InterString.Get("娱乐匹配"));
                StopCoroutine(entertainMatch);
                entertainMatch = null;
            }
        }

        public void AthleticMatch()
        {
            if (athleticMatch == null)
            {
                StartCoroutine(athleticMatch = AthleticMatchAsync());
                if (entertainMatch != null)
                {
                    StopCoroutine(entertainMatch);
                    entertainMatch = null;
                }
                GetUI<UI.ServantUI.OnlineServantUI>().PageMyCard.ButtonEMatch.SetButtonText(MDPro3.InterString.Get("娱乐匹配"));
            }
            else
            {
                GetUI<UI.ServantUI.OnlineServantUI>().PageMyCard.ButtonAMatch.SetButtonText(MDPro3.InterString.Get("竞技匹配"));
                StopCoroutine(athleticMatch);
                athleticMatch = null;
            }
        }

        private IEnumerator EntertainMatchAsync()
        {
            var task = Services.MyCard.GetMatchInfo("entertain");
            var startTime = DateTime.Now;

            while(!task.IsCompleted)
            {
                var elapsedTimeInSeconds = (DateTime.Now - startTime).TotalSeconds;
                int minutes = (int)Math.Floor(elapsedTimeInSeconds / 60);
                int seconds = (int)(elapsedTimeInSeconds % 60);
                GetUI<UI.ServantUI.OnlineServantUI>().PageMyCard.ButtonEMatch.SetButtonText($"{minutes:D2}:{seconds:D2}");
                yield return new WaitForSeconds(0.5f);
            }

            if(task.Result != null)
            {
                GetUI<UI.ServantUI.OnlineServantUI>().PageMyCard.ButtonEMatch.SetButtonText(MDPro3.InterString.Get("娱乐匹配"));
                Runtime.Network.TcpHelper.LinkStart(task.Result.address, Services.MyCard.account.user.username, task.Result.port.ToString(), task.Result.password, false, null);
            }
            else
            {
                GetUI<UI.ServantUI.OnlineServantUI>().PageMyCard.ButtonEMatch.SetButtonText(MDPro3.InterString.Get("匹配失败"));
            }
            entertainMatch = null;
        }

        private IEnumerator AthleticMatchAsync()
        {
            var task = Services.MyCard.GetMatchInfo("athletic");
            var startTime = DateTime.Now;

            while (!task.IsCompleted)
            {
                var elapsedTimeInSeconds = (DateTime.Now - startTime).TotalSeconds;
                int minutes = (int)Math.Floor(elapsedTimeInSeconds / 60);
                int seconds = (int)(elapsedTimeInSeconds % 60);
                GetUI<UI.ServantUI.OnlineServantUI>().PageMyCard.ButtonAMatch.SetButtonText($"{minutes:D2}:{seconds:D2}");
                yield return new WaitForSeconds(0.5f);
            }

            if (task.Result != null)
            {
                GetUI<UI.ServantUI.OnlineServantUI>().PageMyCard.ButtonAMatch.SetButtonText(MDPro3.InterString.Get("竞技匹配"));
                Runtime.Network.TcpHelper.LinkStart(task.Result.address, Services.MyCard.account.user.username, task.Result.port.ToString(), task.Result.password, false, null);
            }
            else
            {
                GetUI<UI.ServantUI.OnlineServantUI>().PageMyCard.ButtonAMatch.SetButtonText(MDPro3.InterString.Get("匹配失败"));
            }
            athleticMatch = null;
        }

        #endregion

        #region Watch List

        public void SetWatchRooms(List<Services.MyCardRoom> rooms)
        {
            GetUI<UI.ServantUI.OnlineServantUI>().PageMyCard.WatchList.SetRooms(rooms);
        }

        public void CreateWatchRoom(Services.MyCardRoom room)
        {
            GetUI<UI.ServantUI.OnlineServantUI>().PageMyCard.WatchList.CreateRoom(room);
        }

        public void UpdateWatchRoom(Services.MyCardRoom room)
        {
            GetUI<UI.ServantUI.OnlineServantUI>().PageMyCard.WatchList.UpdateRoom(room);
        }

        public void DeleteWatchRoom(string roomId)
        {
            GetUI<UI.ServantUI.OnlineServantUI>().PageMyCard.WatchList.DeleteRoom(roomId);
        }

        public void ClearWatchList()
        {
            GetUI<UI.ServantUI.OnlineServantUI>().PageMyCard.WatchList.Clear();
        }

        #endregion

        #endregion
    }
}
