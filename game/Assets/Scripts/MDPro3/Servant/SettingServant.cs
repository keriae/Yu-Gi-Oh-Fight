using MDPro3.Net;
using MDPro3.UI;
using MDPro3.UI.ServantUI;
using MDPro3.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using YgomSystem.LocalFileSystem;
using static MDPro3.CardRenderer;
using static MDPro3.Net.NetUtil;

namespace MDPro3.Servant
{
    public class SettingServant : Servant
    {
        [Header("Setting Servant")]
        [HideInInspector] public SelectionToggle_Setting lastSelectedToggle;
        [HideInInspector] public SelectionButton_Setting lastSelectedButton;

        private string unsafeDownloadUrl;

        #region Servant

        public override int Depth => 1;
        protected override bool ShowLine => false;
        protected override float BlackAlpha => 0.6f;
        protected override float SubBlackAlpha => 0.9f;

        public override void Initialize()
        {
            returnServant = Program.instance.menu;
            base.Initialize();

            InitializeSettings();
        }

        protected override void ApplyShowArrangement(int preDepth)
        {
            base.ApplyShowArrangement(preDepth);
            RefreshCharacterName();

            if (preDepth <= Depth)
            {
                servantUI.SelectDefaultSelectable();
            }

            if (Program.instance.currentServant == Program.instance.ocgcore)
            {
                Program.instance.currentSubServant = this;
                UIManager.ShowFPSRight();
            }
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !UserInput.NeedDefaultSelect())
                return;

            if (lastSelectable != null)
            {
                if (lastSelectable.TryGetComponent<SelectionButton_Setting>(out _))
                    EventSystem.current.SetSelectedGameObject(lastSelectable.gameObject);
                else if (lastSelectable.TryGetComponent<SelectionToggle_Setting>(out _))
                    EventSystem.current.SetSelectedGameObject(lastSelectable.gameObject);
                else
                    servantUI.SelectDefaultSelectable();
            }
            else
                servantUI.SelectDefaultSelectable();
        }

        public override void OnReturn()
        {
            if (inTransition) return;
            if(returnAction != null)
            {
                returnAction.Invoke();
                return;
            }
            AudioManager.PlaySE("SE_MENU_CANCEL");
            GameObject selected = EventSystem.current.currentSelectedGameObject;

            if (selected == null)
                OnExit();
            else if (Cursor.lockState == CursorLockMode.None)
                OnExit();
            else if (selected.TryGetComponent<SelectionButton_Setting>(out _))
            {
                if (lastSelectedToggle != null)
                    lastSelectedToggle.GetSelectable().Select();
                else
                    servantUI.SelectDefaultSelectable();
            }
            else
                OnExit();
        }

        #endregion

        #region Common
        private void InitializeSettings()
        {
            QualitySettings.vSyncCount = 0;

            AudioManager.SetBGMVol(Config.GetFloat("BgmVol", 0.7f));
            AudioManager.SetSeVol(Config.GetFloat("SEVol", 0.7f));
            AudioManager.SetVoiceVol(Config.GetFloat("VoiceVol", 0.7f));

#if UNITY_ANDROID || UNITY_IOS
            if (Config.Have("Resolution"))
            {
                var resolution = Config.Get("Resolution", "1920 x 1080");
                Screen.SetResolution(int.Parse(Regex.Split(resolution, " x ")[0]), int.Parse(Regex.Split(resolution, " x ")[1]), FullScreenMode.FullScreenWindow);
            }
#endif

            Program.instance.camera_.urpAsset.renderScale = SettingServantUI.GetScale();
            // TODO FAA
            SettingServantUI.ChangeAAA(Config.GetFloat("AAA", 0f));
            SettingServantUI.ChangeShadow(Config.GetFloat("Shadow", 0f));
            SetFpsToConfig();
            SettingServantUI.ChangeShowFPS();
            Program.instance.background_.Change(int.Parse(Config.Get("Background", "0")));
        }

        public static void SetFpsToConfig()
        {
            SettingServantUI.ChangeFPS(SettingServantUI.GetFPS());
        }

        public float GetBGMVolum()
        {
            if (servantUI == null)
                return Config.GetFloat("BgmVol", 0.7f);
            else
                return GetUI<SettingServantUI>().GetBGMVolum();
        }

        public void RefreshCharacterName()
        {
            if (servantUI == null)
                return;

            GetUI<SettingServantUI>().RefreshCharacterName();
        }

        #endregion

        #region Download Prerelease

        private bool checkingPrereleaseUpdate;

        private struct UpdatePreleaseHints
        {
            public readonly string CheckingUpdate => InterString.Get("检查更新中");
            public readonly string CheckFailure => InterString.Get("检查更新失败！");
            public readonly string Updating => InterString.Get("正在更新，请耐心等待更待更新完成再进行其他操作。");
            public readonly string UpdateSuccess => InterString.Get("先行卡更新成功。");
            public readonly string UpdateFailure => InterString.Get("先行卡更新失败。");
            public readonly string Updated => InterString.Get("先行卡已是最新版。");
            public readonly string Progress(float progress) 
                => (progress * 100f).ToString("0.##") + "%";
        }

        private readonly UpdatePreleaseHints pHints = new();

        public void UpdatePrerelease()
        {
            if (!checkingPrereleaseUpdate)
            {
                checkingPrereleaseUpdate = true;
                StartCoroutine(UpdatePrereleaseChunkedCoroutine());
            }
        }

        private IEnumerator UpdatePrereleaseChunkedCoroutine()
        {
            var url = Settings.GetPrereleasePackUrl();
            var filePath = Path.Combine(Program.PATH_EXPANSIONS, Path.GetFileName(url));
            if (!File.Exists(filePath) || Language.GetConfig() != Language.GetPrereleaseConfig())
            {
                Config.Set("Prerelease", Config.STRING_NO);
                Config.Save();
            }

            var etagConfigKey = "Prerelease";
            const long chunkSize = 10 * 1024 * 1024;

            var ui = GetUI<SettingServantUI>();
            ui.ButtonUpdatePrerelease.SetModeText(pHints.CheckingUpdate);

            try
            {
                using var headRequest = UnityWebRequest.Head(url);
                yield return headRequest.SendWebRequest();

                if (headRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"HEAD request failed: {headRequest.error}");
                    MessageManager.Cast(pHints.CheckFailure);
                    yield break;
                }

                string remoteEtag = headRequest.GetResponseHeader("ETag");
                string contentLengthHeader = headRequest.GetResponseHeader("Content-Length");
                if (!long.TryParse(contentLengthHeader, out long totalSize) || totalSize <= 0)
                {
                    Debug.LogError("Invalid or missing Content-Length, cannot proceed.");
                    MessageManager.Cast(pHints.CheckFailure);
                    yield break;
                }

                if (!string.IsNullOrEmpty(remoteEtag))
                {
                    string localEtag = Config.Get(etagConfigKey, Config.STRING_NO);
                    if (localEtag == remoteEtag && File.Exists(filePath))
                    {
                        MessageManager.Cast(pHints.Updated);
                        yield break;
                    }
                }

                bool supportsRange = false;
                using var probeRequest = UnityWebRequest.Get(url);
                probeRequest.SetRequestHeader("Range", "bytes=0-0");
                yield return probeRequest.SendWebRequest();

                if (probeRequest.result == UnityWebRequest.Result.Success && probeRequest.responseCode == 206)
                {
                    supportsRange = true;
                }
                else
                {
                    Debug.LogWarning("Server does not support Range requests, falling back to full download.");
                }

                if (supportsRange)
                {
                    using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 81920);
                    long totalDownloadedBytes = 0;
                    int maxConcurrent = 5;

                    long chunks = (totalSize + chunkSize - 1) / chunkSize;
                    long[] chunkStart = new long[chunks];
                    long[] chunkEnd = new long[chunks];
                    for (long i = 0; i < chunks; i++)
                    {
                        chunkStart[i] = i * chunkSize;
                        chunkEnd[i] = Math.Min(chunkStart[i] + chunkSize - 1, totalSize - 1);
                    }

                    int nextChunkIndex = 0;
                    List<UnityWebRequest> activeRequests = new List<UnityWebRequest>();
                    List<int> activeChunkIndices = new List<int>();

                    while (totalDownloadedBytes < totalSize)
                    {
                        long currentDownloaded = totalDownloadedBytes;
                        foreach (var req in activeRequests)
                        {
                            currentDownloaded += (long)req.downloadedBytes;
                        }
                        float progress = Mathf.Clamp01((float)currentDownloaded / totalSize);
                        ui.ButtonUpdatePrerelease.SetModeText(pHints.Progress(progress));

                        while (activeRequests.Count < maxConcurrent && nextChunkIndex < chunks)
                        {
                            int idx = nextChunkIndex++;
                            var req = UnityWebRequest.Get(url);
                            req.SetRequestHeader("Range", $"bytes={chunkStart[idx]}-{chunkEnd[idx]}");
                            req.SendWebRequest();
                            activeRequests.Add(req);
                            activeChunkIndices.Add(idx);
                        }

                        yield return null;

                        for (int i = activeRequests.Count - 1; i >= 0; i--)
                        {
                            var req = activeRequests[i];
                            if (req.isDone)
                            {
                                int chunkIdx = activeChunkIndices[i];
                                if (req.result != UnityWebRequest.Result.Success)
                                {
                                    Debug.LogError($"Range request failed for chunk {chunkIdx}: {req.error}");
                                    foreach (var r in activeRequests) r.Dispose();
                                    MessageManager.Cast(pHints.UpdateFailure);
                                    File.Delete(filePath);
                                    yield break;
                                }

                                byte[] data = req.downloadHandler.data;
                                if (data != null && data.Length > 0)
                                {
                                    fs.Seek(chunkStart[chunkIdx], SeekOrigin.Begin);
                                    fs.Write(data, 0, data.Length);
                                    totalDownloadedBytes += data.Length;
                                }

                                req.Dispose();
                                activeRequests.RemoveAt(i);
                                activeChunkIndices.RemoveAt(i);
                            }
                        }
                    }

                    ui.ButtonUpdatePrerelease.SetModeText(pHints.Progress(1f));

                    if (new FileInfo(filePath).Length != totalSize)
                    {
                        Debug.LogError("Downloaded file size mismatch.");
                        MessageManager.Cast(pHints.UpdateFailure);
                        File.Delete(filePath);
                        yield break;
                    }
                }
                else
                {
                    using var fullRequest = UnityWebRequest.Get(url);
                    fullRequest.downloadHandler = new DownloadHandlerBuffer();
                    var operation = fullRequest.SendWebRequest();

                    using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 81920);
                    long writtenBytes = 0;

                    while (!operation.isDone)
                    {
                        ulong downloaded = fullRequest.downloadedBytes;
                        if (downloaded > (ulong)writtenBytes)
                        {
                            byte[] data = fullRequest.downloadHandler.data;
                            int newDataLength = (int)(downloaded - (ulong)writtenBytes);
                            if (data != null && newDataLength > 0)
                            {
                                fs.Write(data, (int)writtenBytes, newDataLength);
                                writtenBytes += newDataLength;
                            }
                        }

                        float progress = fullRequest.downloadProgress;
                        ui.ButtonUpdatePrerelease.SetModeText(pHints.Progress(progress));
                        yield return null;
                    }

                    if (fullRequest.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Full download failed: {fullRequest.error}");
                        MessageManager.Cast(pHints.UpdateFailure);
                        File.Delete(filePath);
                        yield break;
                    }

                    byte[] finalData = fullRequest.downloadHandler.data;
                    if (finalData != null && finalData.Length > writtenBytes)
                    {
                        fs.Write(finalData, (int)writtenBytes, finalData.Length - (int)writtenBytes);
                    }

                    if (new FileInfo(filePath).Length != totalSize)
                    {
                        Debug.LogError("Downloaded file size mismatch.");
                        MessageManager.Cast(pHints.UpdateFailure);
                        File.Delete(filePath);
                        yield break;
                    }
                }

                if (!string.IsNullOrEmpty(remoteEtag))
                {
                    Config.Set(etagConfigKey, remoteEtag);
                    Config.Save();
                }
                MessageManager.Cast(pHints.UpdateSuccess);
                Program.instance.InitializeForDataChange();
            }
            finally
            {
                ui.ButtonUpdatePrerelease.SetModeText(string.Empty);
                checkingPrereleaseUpdate = false;
            }
        }
        #endregion

        #region Download YPK

        private bool downloadingYPK;

        public void DownloadYPK()
        {
            if (!downloadingYPK)
            {
                var selections = new List<string>()
                {
                    InterString.Get("下载卡包@n请输入卡包的下载地址"),
                    string.Empty
                };
                UIManager.ShowPopupInput(selections, DownloadYPK, null, TmpInputValidation.ValidationType.None);
            }
            else
                MessageManager.Toast(InterString.Get("正在下载中，请稍后再试。"));
        }

        private void DownloadYPK(string url)
        {
            string[] supportedExts = new string[] { ".zip", ".ypk" };
            if (!NetUtil.IsValidDownloadUrl(url, supportedExts))
            {
                MessageManager.Toast(InterString.Get("无效的下载地址。"));
                return;
            }
            StartCoroutine(DownloadYpkCoroutine(url));
        }


        private IEnumerator DownloadYpkCoroutine(string url)
        {
            downloadingYPK = true;
            using var request = UnityWebRequest.Get(url);
            request.SendWebRequest();
            while (!request.isDone)
            {
                GetUI<SettingServantUI>().ButtonDownloadYPK.SetModeText((request.downloadProgress * 100f).ToString("0.##") + "%");
                yield return new WaitForSeconds(0.3f);
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                var filePath = Path.Combine(Program.PATH_EXPANSIONS, Path.GetFileName(url));
                if (!Directory.Exists(Program.PATH_EXPANSIONS))
                    Directory.CreateDirectory(Program.PATH_EXPANSIONS);
                File.WriteAllBytes(filePath, request.downloadHandler.data);
                MessageManager.Cast(InterString.Get("下载成功。"));
                Program.instance.InitializeForDataChange();
            }
            else
            {
                //MessageManager.Cast(InterString.Get("下载失败。"));

                string errorMessage = string.Empty;

                // 1. 证书安全问题（SSL错误）
                if (request.result == UnityWebRequest.Result.ConnectionError &&
                    (request.error.Contains("SSL") || request.error.Contains("certificate")))
                {
                    List<string> selections = new()
                    {
                        InterString.Get("下载链接不安全"),
                        InterString.Get("安全证书验证失败！连接可能不安全。@n是否继续下载？"),
                        InterString.Get("确认"),
                        InterString.Get("取消"),
                        Config.STRING_YES
                    };
                    unsafeDownloadUrl = url;
                    UIManager.ShowPopupYesOrNo(selections, DownloadYpkUnsafe, null);
                }
                // 2. 网络连接错误
                else if (request.result == UnityWebRequest.Result.ConnectionError)
                {
                    errorMessage = InterString.Get("网络连接失败：") + request.error;
                }
                // 3. HTTP协议错误（404, 500等）
                else if (request.result == UnityWebRequest.Result.ProtocolError)
                {
                    errorMessage = InterString.Get("服务器返回错误：") + request.responseCode;
                }
                // 4. 数据处理错误
                else if (request.result == UnityWebRequest.Result.DataProcessingError)
                {
                    errorMessage = InterString.Get("数据处理错误：") + request.error;
                }
                // 5. 其他未知错误
                else
                {
                    errorMessage = InterString.Get("未知错误：") + request.error;
                }

                if(errorMessage != string.Empty)
                    MessageManager.Cast(errorMessage);
            }
            GetUI<SettingServantUI>().ButtonDownloadYPK.SetModeText(string.Empty);
            downloadingYPK = false;
        }

        private void DownloadYpkUnsafe()
        {
            StartCoroutine(DownloadYpkUnsafeCoroutine(unsafeDownloadUrl));
        }

        private IEnumerator DownloadYpkUnsafeCoroutine(string url)
        {
            downloadingYPK = true;
            using var request = UnityWebRequest.Get(url);
            request.certificateHandler = new AcceptAllCertificateHandler();
            request.SendWebRequest();
            while (!request.isDone)
            {
                GetUI<SettingServantUI>().ButtonDownloadYPK.SetModeText((request.downloadProgress * 100f).ToString("0.##") + "%");
                yield return new WaitForSeconds(0.3f);
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                var filePath = Path.Combine(Program.PATH_EXPANSIONS, Path.GetFileName(url));
                if (!Directory.Exists(Program.PATH_EXPANSIONS))
                    Directory.CreateDirectory(Program.PATH_EXPANSIONS);
                File.WriteAllBytes(filePath, request.downloadHandler.data);
                MessageManager.Cast(InterString.Get("下载成功。"));
                Program.instance.InitializeForDataChange();
            }
            else
                MessageManager.Cast(InterString.Get("下载失败。"));
            GetUI<SettingServantUI>().ButtonDownloadYPK.SetModeText(string.Empty);
            downloadingYPK = false;
        }

        #endregion

    }
}
