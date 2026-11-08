using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;


using UnityEngine;

using UnityEngine.EventSystems;
using UnityEngine.Networking;


namespace YuGiOhFight.Client.Controllers
{
    public class SettingServant : Servant
    {
        [Header("Setting Servant")]
        [HideInInspector] public UI.SelectionToggle_Setting lastSelectedToggle;
        [HideInInspector] public UI.SelectionButton_Setting lastSelectedButton;

        private string unsafeDownloadUrl;

        #region Servant

        public override int Depth => 1;
        protected override bool ShowLine => false;
        protected override float BlackAlpha => 0.6f;
        protected override float SubBlackAlpha => 0.9f;

        public override void Initialize()
        {
            returnServant = Runtime.Program.instance.menu;
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

            if (Runtime.Program.instance.currentServant == Runtime.Program.instance.ocgcore)
            {
                Runtime.Program.instance.currentSubServant = this;
                Runtime.UIManager.ShowFPSRight();
            }
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !Runtime.UserInput.NeedDefaultSelect())
                return;

            if (lastSelectable != null)
            {
                if (lastSelectable.TryGetComponent<UI.SelectionButton_Setting>(out _))
                    EventSystem.current.SetSelectedGameObject(lastSelectable.gameObject);
                else if (lastSelectable.TryGetComponent<UI.SelectionToggle_Setting>(out _))
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
            Runtime.AudioManager.PlaySE("SE_MENU_CANCEL");
            GameObject selected = EventSystem.current.currentSelectedGameObject;

            if (selected == null)
                OnExit();
            else if (Cursor.lockState == CursorLockMode.None)
                OnExit();
            else if (selected.TryGetComponent<UI.SelectionButton_Setting>(out _))
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
            Services.RuntimeDisplay.Initialize();

            Runtime.AudioManager.SetBGMVol(MDPro3.Config.GetFloat("BgmVol", 0.7f));
            Runtime.AudioManager.SetSeVol(MDPro3.Config.GetFloat("SEVol", 0.7f));
            Runtime.AudioManager.SetVoiceVol(MDPro3.Config.GetFloat("VoiceVol", 0.7f));

#if UNITY_ANDROID || UNITY_IOS
            if (Config.Have("Resolution"))
            {
                var resolution = Config.Get("Resolution", "1920 x 1080");
                Screen.SetResolution(int.Parse(Regex.Split(resolution, " x ")[0]), int.Parse(Regex.Split(resolution, " x ")[1]), FullScreenMode.FullScreenWindow);
            }
#endif

            Runtime.Program.instance.camera_.urpAsset.renderScale = UI.ServantUI.SettingServantUI.GetScale();
            // TODO FAA
            UI.ServantUI.SettingServantUI.ChangeAAA(MDPro3.Config.GetFloat("AAA", 0f));
            UI.ServantUI.SettingServantUI.ChangeShadow(MDPro3.Config.GetFloat("Shadow", 0f));
            SetFpsToConfig();
            UI.ServantUI.SettingServantUI.ChangeShowFPS();
            Runtime.Program.instance.background_.Change(int.Parse(MDPro3.Config.Get("Background", "0")));
        }

        public static void SetFpsToConfig()
        {
            UI.ServantUI.SettingServantUI.ChangeFPS(UI.ServantUI.SettingServantUI.GetFPS());
        }

        public float GetBGMVolum()
        {
            if (servantUI == null)
                return MDPro3.Config.GetFloat("BgmVol", 0.7f);
            else
                return GetUI<UI.ServantUI.SettingServantUI>().GetBGMVolum();
        }

        public void RefreshCharacterName()
        {
            if (servantUI == null)
                return;

            GetUI<UI.ServantUI.SettingServantUI>().RefreshCharacterName();
        }

        #endregion

        #region Download Prerelease

        private bool checkingPrereleaseUpdate;

        private struct UpdatePreleaseHints
        {
            public readonly string CheckingUpdate => MDPro3.InterString.Get("检查更新中");
            public readonly string CheckFailure => MDPro3.InterString.Get("检查更新失败！");
            public readonly string Updating => MDPro3.InterString.Get("正在更新，请耐心等待更待更新完成再进行其他操作。");
            public readonly string UpdateSuccess => MDPro3.InterString.Get("先行卡更新成功。");
            public readonly string UpdateFailure => MDPro3.InterString.Get("先行卡更新失败。");
            public readonly string Updated => MDPro3.InterString.Get("先行卡已是最新版。");
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
            var url = MDPro3.Settings.GetPrereleasePackUrl();
            var filePath = Path.Combine(Runtime.Program.PATH_EXPANSIONS, Path.GetFileName(url));
            if (!File.Exists(filePath) || MDPro3.Utility.Language.GetConfig() != MDPro3.Utility.Language.GetPrereleaseConfig())
            {
                MDPro3.Config.Set("Prerelease", MDPro3.Config.STRING_NO);
                MDPro3.Config.Save();
            }

            var etagConfigKey = "Prerelease";
            const long chunkSize = 10 * 1024 * 1024;

            UI.ServantUI.SettingServantUI ui = GetUI<UI.ServantUI.SettingServantUI>();
            ui.ButtonUpdatePrerelease.SetModeText(pHints.CheckingUpdate);

            try
            {
                using var headRequest = UnityWebRequest.Head(url);
                yield return headRequest.SendWebRequest();

                if (headRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"HEAD request failed: {headRequest.error}");
                    Runtime.MessageManager.Cast(pHints.CheckFailure);
                    yield break;
                }

                string remoteEtag = headRequest.GetResponseHeader("ETag");
                string contentLengthHeader = headRequest.GetResponseHeader("Content-Length");
                if (!long.TryParse(contentLengthHeader, out long totalSize) || totalSize <= 0)
                {
                    Debug.LogError("Invalid or missing Content-Length, cannot proceed.");
                    Runtime.MessageManager.Cast(pHints.CheckFailure);
                    yield break;
                }

                if (!string.IsNullOrEmpty(remoteEtag))
                {
                    string localEtag = MDPro3.Config.Get(etagConfigKey, MDPro3.Config.STRING_NO);
                    if (localEtag == remoteEtag && File.Exists(filePath))
                    {
                        Runtime.MessageManager.Cast(pHints.Updated);
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
                                    Runtime.MessageManager.Cast(pHints.UpdateFailure);
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
                        Runtime.MessageManager.Cast(pHints.UpdateFailure);
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
                        Runtime.MessageManager.Cast(pHints.UpdateFailure);
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
                        Runtime.MessageManager.Cast(pHints.UpdateFailure);
                        File.Delete(filePath);
                        yield break;
                    }
                }

                if (!string.IsNullOrEmpty(remoteEtag))
                {
                    MDPro3.Config.Set(etagConfigKey, remoteEtag);
                    MDPro3.Config.Save();
                }
                Runtime.MessageManager.Cast(pHints.UpdateSuccess);
                Runtime.Program.instance.InitializeForDataChange();
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
                    MDPro3.InterString.Get("下载卡包@n请输入卡包的下载地址"),
                    string.Empty
                };
                Runtime.UIManager.ShowPopupInput(selections, DownloadYPK, null, UI.TmpInputValidation.ValidationType.None);
            }
            else
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("正在下载中，请稍后再试。"));
        }

        private void DownloadYPK(string url)
        {
            string[] supportedExts = new string[] { ".zip", ".ypk" };
            if (!MDPro3.Net.NetUtil.IsValidDownloadUrl(url, supportedExts))
            {
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("无效的下载地址。"));
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
                GetUI<UI.ServantUI.SettingServantUI>().ButtonDownloadYPK.SetModeText((request.downloadProgress * 100f).ToString("0.##") + "%");
                yield return new WaitForSeconds(0.3f);
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                var filePath = Path.Combine(Runtime.Program.PATH_EXPANSIONS, Path.GetFileName(url));
                if (!Directory.Exists(Runtime.Program.PATH_EXPANSIONS))
                    Directory.CreateDirectory(Runtime.Program.PATH_EXPANSIONS);
                File.WriteAllBytes(filePath, request.downloadHandler.data);
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("下载成功。"));
                Runtime.Program.instance.InitializeForDataChange();
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
                        MDPro3.InterString.Get("下载链接不安全"),
                        MDPro3.InterString.Get("安全证书验证失败！连接可能不安全。@n是否继续下载？"),
                        MDPro3.InterString.Get("确认"),
                        MDPro3.InterString.Get("取消"),
                        MDPro3.Config.STRING_YES
                    };
                    unsafeDownloadUrl = url;
                    Runtime.UIManager.ShowPopupYesOrNo(selections, DownloadYpkUnsafe, null);
                }
                // 2. 网络连接错误
                else if (request.result == UnityWebRequest.Result.ConnectionError)
                {
                    errorMessage = MDPro3.InterString.Get("网络连接失败：") + request.error;
                }
                // 3. HTTP协议错误（404, 500等）
                else if (request.result == UnityWebRequest.Result.ProtocolError)
                {
                    errorMessage = MDPro3.InterString.Get("服务器返回错误：") + request.responseCode;
                }
                // 4. 数据处理错误
                else if (request.result == UnityWebRequest.Result.DataProcessingError)
                {
                    errorMessage = MDPro3.InterString.Get("数据处理错误：") + request.error;
                }
                // 5. 其他未知错误
                else
                {
                    errorMessage = MDPro3.InterString.Get("未知错误：") + request.error;
                }

                if(errorMessage != string.Empty)
                    Runtime.MessageManager.Cast(errorMessage);
            }
            GetUI<UI.ServantUI.SettingServantUI>().ButtonDownloadYPK.SetModeText(string.Empty);
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
            request.certificateHandler = new MDPro3.Net.NetUtil.AcceptAllCertificateHandler();
            request.SendWebRequest();
            while (!request.isDone)
            {
                GetUI<UI.ServantUI.SettingServantUI>().ButtonDownloadYPK.SetModeText((request.downloadProgress * 100f).ToString("0.##") + "%");
                yield return new WaitForSeconds(0.3f);
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                var filePath = Path.Combine(Runtime.Program.PATH_EXPANSIONS, Path.GetFileName(url));
                if (!Directory.Exists(Runtime.Program.PATH_EXPANSIONS))
                    Directory.CreateDirectory(Runtime.Program.PATH_EXPANSIONS);
                File.WriteAllBytes(filePath, request.downloadHandler.data);
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("下载成功。"));
                Runtime.Program.instance.InitializeForDataChange();
            }
            else
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("下载失败。"));
            GetUI<UI.ServantUI.SettingServantUI>().ButtonDownloadYPK.SetModeText(string.Empty);
            downloadingYPK = false;
        }

        #endregion

    }
}
