using Cysharp.Threading.Tasks;
using MDPro3.Duel.YGOSharp;
using Newtonsoft.Json;

using System;

using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace MDPro3.Net
{
    public static class OnlineService
    {

        public static void Initialize()
        {
        }

        public static UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        #region Genesys lflist

        private const string URL_GENESYS_LFLIST = "https://cdntx.moecube.com/ygopro-genesys/lflist.conf";

        private static async UniTask InitializeGenesysLflist()
        {
            var eTag = await GetETagAsync(URL_GENESYS_LFLIST);
            if (string.IsNullOrEmpty(eTag))
                return;

            var configTag = Config.Get(GetLocalETagKey(URL_GENESYS_LFLIST), Config.EMPTY_STRING);
            if(!string.Equals(eTag, configTag, StringComparison.Ordinal))
            {
                YuGiOhFight.Client.Runtime.Program.Debug("Update Genesys Lflist.");
                await DownloadGenesysLflist(eTag);
                BanlistManager.InitializeForGenesys();
            }
            else
                YuGiOhFight.Client.Runtime.Program.Debug("Genesys Lflist do not need update.");
        }

        private static async UniTask DownloadGenesysLflist(string ETag)
        {
            using var request = UnityWebRequest.Get(URL_GENESYS_LFLIST);
            request.timeout = 15;

            try
            {
                await request.SendWebRequest();
            }
            catch
            {
                YuGiOhFight.Client.Runtime.MessageManager.Cast(InterString.Get("下载Genesys禁卡表失败。"));
                return;
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                File.WriteAllText(YuGiOhFight.Client.Runtime.Program.PATH_DATA + BanlistManager.FILE_NAME_GENESYS, request.downloadHandler.text);
                Config.Set(GetLocalETagKey(URL_GENESYS_LFLIST), ETag);
                Config.Save();
            }
            else
                YuGiOhFight.Client.Runtime.MessageManager.Cast(InterString.Get("下载Genesys禁卡表失败。"));
        }

        #endregion

        #region MyCard Apps

        private const string URL_MYCARD_APPS = "https://cdntx.moecube.com/apps.json";
        private const string PATH_MYCARD_APPS = YuGiOhFight.Client.Runtime.Program.PATH_DATA + "mycard_apps.json";
        public static YuGiOhFight.Client.Services.MyCardNews myCardNews;

        private static async UniTask InitializeMyCardAppsAsync()
        {
            var eTag = await GetETagAsync(URL_MYCARD_APPS);
            if (!string.IsNullOrEmpty(eTag))
            {
                var configTag = Config.Get(GetLocalETagKey(URL_MYCARD_APPS), Config.EMPTY_STRING);
                if (!string.Equals(eTag, configTag, StringComparison.Ordinal))
                {
                    YuGiOhFight.Client.Runtime.Program.Debug("Update MyCard Apps.");
                    await DownloadMyCardApps(eTag);
                }
                else
                    YuGiOhFight.Client.Runtime.Program.Debug("MyCard Apps do not need update.");
            }

            ParseMyCardNews();
        }

        private static async UniTask DownloadMyCardApps(string ETag)
        {
            using var request = UnityWebRequest.Get(URL_MYCARD_APPS);
            request.timeout = 15;

            try
            {
                await request.SendWebRequest();
            }
            catch
            {
                YuGiOhFight.Client.Runtime.Program.Debug("下载MyCard apps.json失败。");
                return;
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                File.WriteAllText(PATH_MYCARD_APPS, request.downloadHandler.text);
                Config.Set(GetLocalETagKey(URL_MYCARD_APPS), ETag);
                Config.Save();
            }
            else
                YuGiOhFight.Client.Runtime.Program.Debug("下载MyCard apps.json失败。");
        }

        private static void ParseMyCardNews()
        {
            if (!File.Exists(PATH_MYCARD_APPS))
                return;

            var json = File.ReadAllText(PATH_MYCARD_APPS);
            json = json.Replace("\"news\":[]", "\"news\":{}");
            var apps = JsonConvert.DeserializeObject<YuGiOhFight.Client.Services.MyCardApp[]>(json);
            foreach (var app in apps)
                if (app.id == "ygopro")
                {
                    myCardNews = app.news;
                    return;
                }
        }

        #endregion

        #region Id Change Log

        private const string URL_ID_CHANGE_LOG = "https://ygocdb-mirror.moecube.com/api/v0/idChangelog.jsonp";

        private static async UniTask InitializeIdChangeLog()
        {
            var eTag = await GetETagAsync(URL_ID_CHANGE_LOG);
            if(string.IsNullOrEmpty(eTag))
                return;
            var configTag = Config.Get(GetLocalETagKey(URL_ID_CHANGE_LOG), Config.EMPTY_STRING);
            if (!string.Equals(eTag, configTag, StringComparison.Ordinal))
            {
                YuGiOhFight.Client.Runtime.Program.Debug("Update Id Change Log.");
                await DownloadIdChangeLog(eTag);
                //
            }
            else
                YuGiOhFight.Client.Runtime.Program.Debug("Id Change Log do not need update.");
        }

        private static async UniTask DownloadIdChangeLog(string ETag)
        {
            using var request = UnityWebRequest.Get(URL_ID_CHANGE_LOG);
            request.timeout = 15;

            try
            {
                await request.SendWebRequest();
            }
            catch
            {
                YuGiOhFight.Client.Runtime.MessageManager.Cast(InterString.Get("下载IdChangeLog失败。"));
                return;
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                File.WriteAllText(YuGiOhFight.Client.Runtime.Program.PATH_DATA + YdkIdHelper.FILE_NAME, request.downloadHandler.text);
                Config.Set(GetLocalETagKey(URL_ID_CHANGE_LOG), ETag);
                Config.Save();
                YdkIdHelper.SetDirty();
            }
            else
                YuGiOhFight.Client.Runtime.MessageManager.Cast(InterString.Get("下载IdChangeLog失败。"));
        }

        #endregion

        #region User Config Sync (Rarity.json)

        // 卡组中心在线服务的用户配置接口：
        // GET  /api/mdpro3/user/config/{userId}
        // POST /api/mdpro3/user/config  body: {userId, config, extra1~5}
        // extra1 保存 Rarity.json 上次上传的 UTC 毫秒时间戳，用于两端比新。
        private const string URL_USER_CONFIG = "http://rarnu.xyz:38383/api/mdpro3/user/config";
        private const string REQ_SOURCE_HEADER = "ReqSource";
        private const string REQ_SOURCE_VALUE = "MDPro3";
        private const string TOKEN_HEADER = "token";
        private const string JSON_CONTENT_TYPE = "application/json";

        private static bool raritySynced;

        // 开机自动同步入口：MyCard 登录成功后（OnlineServant.LoginSuccessEvent）与 InitializeAsync 时都会调用。
        // CardRarity 是方法内懒加载，本同步在懒加载触发前把服务器副本写入本地文件即可生效；
        // 若懒加载已先发生，ApplyServerContent 会同时刷新内存缓存。
        public static async UniTask SyncRarityConfigAsync()
        {
            if (raritySynced || YuGiOhFight.Client.Services.MyCard.account == null || YuGiOhFight.Client.Services.MyCard.account.user.id == 0)
                return;
            raritySynced = true;
            try
            {
                await SyncRarityWithServer(YuGiOhFight.Client.Services.MyCard.account.user.id, YuGiOhFight.Client.Services.MyCard.account.token);
            }
            catch (Exception e)
            {
                raritySynced = false; // 失败允许下次登录重试
                YuGiOhFight.Client.Runtime.Program.Debug("Rarity.json 同步失败：" + e.Message);
            }
        }

        private static async UniTask SyncRarityWithServer(int userId, string token)
        {
            var (ok, remote) = await GetUserConfigAsync(userId, token);
            if (!ok)
            {
                // 网络故障 ≠ 服务器无配置，直接中止，避免误上传
                YuGiOhFight.Client.Runtime.Program.Debug("Rarity.json 同步中止：无法访问服务器。");
                return;
            }
            var localPath = CardRarity.GetFilePath();
            var localText = File.Exists(localPath) ? File.ReadAllText(localPath) : null;

            // 服务器确认存在但还没有配置：直接把本地存档上传
            if (remote == null || string.IsNullOrEmpty(remote.config))
            {
                if (!string.IsNullOrEmpty(localText))
                {
                    await UploadRarityAsync(userId, token, localText);
                    YuGiOhFight.Client.Runtime.Program.Debug("Rarity.json 同步：服务器无配置，已上传本地。");
                }
                else
                    YuGiOhFight.Client.Runtime.Program.Debug("Rarity.json 同步：本地与服务器均无数据。");
                return;
            }

            // 本地没有文件：下载服务器副本
            if (string.IsNullOrEmpty(localText))
            {
                CardRarity.ApplyServerContent(remote.config);
                YuGiOhFight.Client.Runtime.Program.Debug("Rarity.json 同步：本地无文件，已下载服务器副本。");
                return;
            }

            // 内容一致：无需同步
            if (localText == remote.config)
            {
                YuGiOhFight.Client.Runtime.Program.Debug("Rarity.json 同步：内容一致，无需同步。");
                return;
            }

            // 两边都有且不同：比时间戳，新的一方胜出
            var localMs = new DateTimeOffset(File.GetLastWriteTimeUtc(localPath), TimeSpan.Zero).ToUnixTimeMilliseconds();
            var remoteMs = ParseLong(remote.extra1);
            if (remoteMs > localMs)
            {
                CardRarity.ApplyServerContent(remote.config);
                YuGiOhFight.Client.Runtime.Program.Debug("Rarity.json 同步：服务器副本较新，已覆盖本地。");
            }
            else
            {
                await UploadRarityAsync(userId, token, localText);
                YuGiOhFight.Client.Runtime.Program.Debug("Rarity.json 同步：本地副本较新，已上传服务器。");
            }
        }

        // 返回 (ok, cfg)：ok=false 表示请求本身失败（网络/HTTP 错误），cfg=null 仅在 ok=true 时才表示"服务器确实无配置"。
        private static async UniTask<(bool ok, UserConfigData cfg)> GetUserConfigAsync(int userId, string token)
        {
            using var request = UnityWebRequest.Get($"{URL_USER_CONFIG}/{userId}");
            request.timeout = 15;
            request.SetRequestHeader(REQ_SOURCE_HEADER, REQ_SOURCE_VALUE);
            request.SetRequestHeader(TOKEN_HEADER, token);

            try
            {
                await request.SendWebRequest();
            }
            catch (UnityWebRequestException e)
            {
                // UniTask 的 awaiter 在 result!=Success 时统一抛 UnityWebRequestException，带上状态码与响应体便于定位
                var body = e.Text ?? "";
                YuGiOhFight.Client.Runtime.Program.Debug($"获取用户配置失败：HTTP {e.ResponseCode} {e.Error} {body}");
                return (false, null);
            }
            catch (Exception e)
            {
                YuGiOhFight.Client.Runtime.Program.Debug("获取用户配置失败：" + e.Message);
                return (false, null);
            }

            // 兼容 data 包裹与裸对象两种返回结构
            var response = JsonUtility.FromJson<UserConfigResponse>(request.downloadHandler.text);
            if (response == null)
                return (true, null);
            if (response.data != null && !string.IsNullOrEmpty(response.data.config))
                return (true, response.data);
            if (!string.IsNullOrEmpty(response.config))
                return (true, new UserConfigData
                {
                    userId = response.userId,
                    config = response.config,
                    extra1 = response.extra1,
                    extra2 = response.extra2,
                    extra3 = response.extra3,
                    extra4 = response.extra4,
                    extra5 = response.extra5
                });
            return (true, null);
        }

        private static async UniTask UploadRarityAsync(int userId, string token, string configJson)
        {
            var body = new PostUserConfigBody
            {
                userId = userId,
                config = configJson,
                extra1 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
            };

            var json = JsonUtility.ToJson(body);
            using var request = UnityWebRequest.Post(URL_USER_CONFIG, json, JSON_CONTENT_TYPE);
            request.timeout = 15;
            request.SetRequestHeader(REQ_SOURCE_HEADER, REQ_SOURCE_VALUE);
            request.SetRequestHeader("Content-Type", JSON_CONTENT_TYPE);
            request.SetRequestHeader(TOKEN_HEADER, token);

            try
            {
                await request.SendWebRequest();
            }
            catch (UnityWebRequestException e)
            {
                var respBody = e.Text ?? "";
                YuGiOhFight.Client.Runtime.Program.Debug($"上传用户配置失败：HTTP {e.ResponseCode} {e.Error} {respBody}");
                return;
            }
            catch (Exception e)
            {
                YuGiOhFight.Client.Runtime.Program.Debug("上传用户配置失败：" + e.Message);
                return;
            }

            var response = JsonUtility.FromJson<UserConfigPostResponse>(request.downloadHandler.text);
            if (response == null || response.code != 0)
                YuGiOhFight.Client.Runtime.Program.Debug("上传用户配置被拒绝：" + (response?.message ?? request.downloadHandler.text));
            else
                YuGiOhFight.Client.Runtime.Program.Debug("上传用户配置成功。");
        }

        private static long ParseLong(string value)
        {
            return long.TryParse(value, out var result) ? result : 0;
        }

        [Serializable]
        public class UserConfigData
        {
            public int userId;
            public string config;
            public string extra1;
            public string extra2;
            public string extra3;
            public string extra4;
            public string extra5;
        }

        [Serializable]
        public class UserConfigResponse
        {
            public int code;
            public string message;
            public int userId;
            public string config;
            public string extra1;
            public string extra2;
            public string extra3;
            public string extra4;
            public string extra5;
            public UserConfigData data;
        }

        [Serializable]
        public class UserConfigPostResponse
        {
            public int code;
            public string message;
        }

        [Serializable]
        public class PostUserConfigBody
        {
            public int userId;
            public string config;
            public string extra1 = string.Empty;
            public string extra2 = string.Empty;
            public string extra3 = string.Empty;
            public string extra4 = string.Empty;
            public string extra5 = string.Empty;
        }

        #endregion

        #region Online Tools

        private static string GetLocalETagKey(string url) => $"ETag_{url.GetHashCode()}";

        public static async UniTask<string> GetETagAsync(string url)
        {
            using var headRequest = UnityWebRequest.Head(url);
            headRequest.timeout = 8;
            try
            {
                await headRequest.SendWebRequest();
            }
            catch
            {
                return null;
            }

            if(headRequest.result != UnityWebRequest.Result.Success)
            {
                YuGiOhFight.Client.Runtime.Program.Debug($"HEAD({url})请求失败：{headRequest.error}");
                return null;
            }

            var onlineETag = headRequest.GetResponseHeader("ETag")?.Trim();
            if (string.IsNullOrEmpty(onlineETag))
            {
                YuGiOhFight.Client.Runtime.Program.Debug($"未找到ETag({url})，服务器可能未启用缓存");
                return null;
            }
            return onlineETag;
        }

        #endregion

    }
}
