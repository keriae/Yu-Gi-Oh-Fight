using System;
using System.IO;
using UnityEngine;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

using UnityEngine.Networking;


using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;


namespace YuGiOhFight.Client.Presentation
{
    public class Boot : MonoBehaviour
    {
        public Slider progressBar;
        public Text text;

        private string title;
        private string dots;
        private float time;
        private bool extracting;
        private int totalNum;
        private int nowNum;

        private readonly List<string> zips = new();

        private void Start()
        {
            // The native player changes cwd. Restore the launcher's writable save root.
            var stateRoot = Environment.GetEnvironmentVariable("MDPRO3_STATE_ROOT");
            try
            {
                if (!string.IsNullOrEmpty(stateRoot)) Environment.CurrentDirectory = stateRoot;
            }
            catch (Exception error)
            {
                Client.UI.Client.FightStartupPresentation.Install();
                Client.UI.Client.FightStartupPresentation.ReportFailure("无法访问运行目录：" + error.Message);
                return;
            }
            Client.UI.Client.FightStartupPresentation.Install();
            MDPro3.Utility.RuntimeFramePacing.Initialize();
            try
            {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            Environment.CurrentDirectory = Application.persistentDataPath;
            Directory.SetCurrentDirectory(Application.persistentDataPath);
            if (VersionCheck())
            {
                BetterStreamingAssets.Initialize();
                var paths = BetterStreamingAssets.GetFiles("\\", "*.zip");
                foreach (var zip in paths)
                    zips.Add(Path.GetFileName(zip).Replace(".zip", ""));
                StartCoroutine(ProtectedStartup(CheckFile()));
            }
            else FightStartupPresentation.ReportFailure(title ?? "安装版本不符合启动要求。");
#else
            InitializeLanguage();
            StartCoroutine(ProtectedStartup(LoadMainSceneAsync()));
#endif
            }
            catch (Exception error)
            {
                Debug.LogError("[FightPresentation] Boot initialization: " + error);
                Client.UI.Client.FightStartupPresentation.ReportFailure("启动初始化失败：" + error.Message);
            }
        }

        private IEnumerator ProtectedStartup(IEnumerator actions)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(actions);
            string failure = null;
            while (stack.Count > 0)
            {
                var current = stack.Peek(); bool more = false; object next = null;
                try { more = current.MoveNext(); if (more) next = current.Current; }
                catch (Exception error) { failure = error.Message; Debug.LogError("[FightPresentation] Boot load: " + error); }
                if (failure != null) break;
                if (!more) { stack.Pop(); (current as IDisposable)?.Dispose(); continue; }
                if (next is IEnumerator nested) { stack.Push(nested); continue; }
                yield return next;
            }
            while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
            if (failure != null) Client.UI.Client.FightStartupPresentation.ReportFailure("启动加载失败：" + failure);
        }

        private void Update()
        {
            time += Time.deltaTime;
            if (time > 0.33f)
            {
                time = 0;
                dots += ".";
                if (dots == "....")
                    dots = "";
            }
            if (extracting && totalNum != 0)
            {
                float progress = (float)nowNum / totalNum;
                progressBar.value = progress;
            }
            if (totalNum == 0)
                text.text = title + dots;
            else
                text.text = title + "(" + nowNum + Runtime.Program.STRING_SLASH + totalNum + ")";
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            MDPro3.Utility.RuntimeFramePacing.SetFocus(hasFocus);
        }

        private void OnApplicationPause(bool paused)
        {
            MDPro3.Utility.RuntimeFramePacing.SetPaused(paused);
        }

        private bool InitializeLanguage()
        {
            if (!Directory.Exists(Runtime.Program.PATH_DATA))
            {
                Directory.CreateDirectory(Runtime.Program.PATH_DATA);
                MDPro3.Config.Initialize(Runtime.Program.PATH_CONFIG);
                return true;
            }
            else
            {
                MDPro3.Config.Initialize(Runtime.Program.PATH_CONFIG);
                if(MDPro3.Config.Get("Version", "Version") == "Version")
                    return true;
                MDPro3.InterString.Initialize();
                return false;
            }
        }

        private IEnumerator CheckFile()
        {
            foreach (string zip in zips)
            {
                if (!MDPro3.Config.GetBool(zip + "_install", false))
                {
                    yield return Check(zip);
                    MDPro3.Config.Set(zip + "_install", "1");
                    MDPro3.Config.Save();
                    GC.Collect();
                }
            }
            yield return null;

            string cacheDir = Application.temporaryCachePath;
            if (Directory.Exists(cacheDir))
                foreach (string file in Directory.GetFiles(cacheDir, "*_temp.zip"))
                    SafeDeleteFile(file);

            yield return LoadMainSceneAsync();
        }

        private IEnumerator Check(string type)
        {
            title = MDPro3.InterString.Get("正在读取[?]", type + ".zip");
            nowNum = 0;
            totalNum = 0;

            string tempZipPath = Path.Combine(Application.temporaryCachePath, type + "_temp.zip");

            string filePath = Path.Combine(Application.streamingAssetsPath, type + ".zip");
            using UnityWebRequest request = UnityWebRequest.Get(filePath);
            request.downloadHandler = new DownloadHandlerFile(tempZipPath);
            request.SendWebRequest();
            while (!request.isDone)
            {
                float progress = Mathf.Clamp01(request.downloadProgress / 0.9f);
                progressBar.value = progress;
                yield return null;
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                title = MDPro3.InterString.Get("正在解压[?]", type + ".zip");
                string outPath = "";
                if (type.Contains("_"))
                    outPath = type.Split('_')[0];
                if (outPath.Length > 0 && !Directory.Exists(outPath))
                    Directory.CreateDirectory(outPath);

                IEnumerator extractEnumerator = ExtractZipFileFromFile(tempZipPath, outPath);
                while (extractEnumerator.MoveNext())
                    yield return extractEnumerator.Current;
            }
            else
                throw new IOException("读取安装资源失败：" + request.error);

            SafeDeleteFile(tempZipPath);
        }

        private IEnumerator LoadMainSceneAsync()
        {
            nowNum = 0;
            totalNum = 0;
            progressBar.value = 0;

            title = MDPro3.InterString.Get("正在初始化");

            Runtime.Program.SetRoot();
            var cache = Runtime.ABLoader.CacheMasterDuelOutDuelBundles().AsTask();
            while (!cache.IsCompleted)
            {
                progressBar.value = Runtime.ABLoader.mdCachedProgress;
                yield return null;
            }
            cache.GetAwaiter().GetResult();

            MDPro3.Config.Initialize(Runtime.Program.PATH_CONFIG);
            MDPro3.Config.Set("Version", Application.version[..5]);
            MDPro3.Config.Save();

            // Keep the initialization handle until its completion status has
            // been inspected; the default overload auto-releases that handle.
            var ini = Addressables.InitializeAsync(false);
            try
            {
                while (!ini.IsDone)
                {
                    progressBar.value = ini.PercentComplete;
                    yield return null;
                }
                if (ini.Status != AsyncOperationStatus.Succeeded) throw ini.OperationException ?? new InvalidOperationException("资源系统初始化失败。");
            }
            finally { if (ini.IsValid()) Addressables.Release(ini); }

            title = MDPro3.InterString.Get("正在读取数据");
            var handle = Addressables.LoadAssetAsync<Assets.Items>("ScriptableObjects/Items.asset");
            while (!handle.IsDone)
            {
                progressBar.value = handle.PercentComplete;
                yield return null;
            }
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                throw handle.OperationException ?? new InvalidOperationException("基础数据加载失败。");
            Runtime.Program.items = handle.Result;

            title = MDPro3.InterString.Get("正在进入游戏");
            var load = Addressables.LoadSceneAsync("SceneMain");
            while (!load.IsDone)
            {
                yield return null;
                progressBar.value = load.PercentComplete;
            }
            if (load.Status != AsyncOperationStatus.Succeeded) throw load.OperationException ?? new InvalidOperationException("主场景加载失败。");
        }

        public static void FastExtractZipFile(string file, string dir, string password = "")
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            FastZip zip = new FastZip();
            zip.Password = password;
            zip.ExtractZip(file, dir, "");
        }

        private IEnumerator ExtractZipFile(byte[] data, string outFolder)
        {
            ZipFile zf = null;
            using MemoryStream mstrm = new(data);
            zf = new ZipFile(mstrm);
            int count = 0;
            foreach (ZipEntry zipEntry in zf)
                count++;
            totalNum = count;
            nowNum = 0;
            extracting = true;
            foreach (ZipEntry zipEntry in zf)
            {
                nowNum++;
                if (!zipEntry.IsFile)
                {
                    continue;
                }
                string entryFileName = zipEntry.Name;
                byte[] buffer = new byte[4096];
                Stream zipStream = zf.GetInputStream(zipEntry);
                string fullZipToPath = Path.Combine(outFolder, entryFileName);
                string directoryName = Path.GetDirectoryName(fullZipToPath);
                if (directoryName.Length > 0)
                    Directory.CreateDirectory(directoryName);
                using (FileStream streamWriter = File.Create(fullZipToPath))
                {
                    StreamUtils.Copy(zipStream, streamWriter, buffer);
                }
                yield return null;
            }
            if (zf != null)
            {
                zf.IsStreamOwner = true;
                zf.Close();
            }
        }

        private bool VersionCheck()
        {
            var firstInstall = InitializeLanguage();

            var installVersion = Application.version;
            var installedVersion = MDPro3.Config.Get("Version", "Version");
            if (installedVersion == "Version")
                firstInstall = true;

            if(firstInstall)
            {
                if(installVersion.Length > 5 || !installVersion.EndsWith("0"))
                {
                    title = "不能直接安装更新包。Can not install update apk directly.";
                    Directory.Delete(Runtime.Program.PATH_DATA);
                    return false;
                }
                else
                    return true;
            }
            else //firstInstall
            {
                if(installVersion == installedVersion)
                    return true;

                if (installVersion.Length > 5)
                {
                    if(installVersion.EndsWith("0"))
                    {
                        if (InstallNext(installedVersion, installVersion))
                            return true;
                        else if (installVersion[..5] == installedVersion)
                            return true;
                        else
                        {
                            title = MDPro3.InterString.Get("当前更新包需要的版本：「[?]」。", VersionPre(installVersion));
                            title += MDPro3.InterString.Get("已安装版本：「[?]」。", installedVersion);
                            return false;
                        }
                    }
                    else
                    {
                        if (installVersion.Substring(0, 5) == installedVersion)
                            return true;
                        else
                        {
                            title = MDPro3.InterString.Get("当前更新包需要的版本：「[?]」。", installVersion.Substring(0, 5));
                            title += MDPro3.InterString.Get("已安装版本：「[?]」。", installedVersion);
                            return false;
                        }
                    }
                }
                else
                {
                    if (installVersion.EndsWith("0"))
                        return true;
                    else
                    {
                        if(VersionPre(installVersion) == installedVersion.Substring(0, 5))
                            return true;
                        else
                        {
                            title = MDPro3.InterString.Get("当前更新包需要的版本：「[?]」。", VersionPre(installVersion));
                            title += MDPro3.InterString.Get("已安装版本：「[?]」。", installedVersion);
                            return false;
                        }
                    }
                }
            }
        }

        private bool InstallNext(string installedVersion, string installVersion)
        {
            var installedInt = GetVersionInt(installedVersion);
            var installInt = GetVersionInt(installVersion);
            if(installInt -  installedInt == 1)
                return true;
            else
                return false;
        }

        private string VersionPre(string version)
        {
            var versionInt = GetVersionInt(version);
            string returnValue = (versionInt - 1).ToString("D3");
            return returnValue.Substring(0, 1) + "." + returnValue.Substring(1, 1) + "." + returnValue.Substring(2, 1);
        }

        private int GetVersionInt(string version)
        {
            string textInt = version.Substring(0, 1) + version.Substring(2, 1) + version.Substring(4, 1);
            return int.Parse(textInt);
        }

        private IEnumerator ExtractZipFileFromFile(string zipPath, string outFolder)
        {
            ZipFile zf = null;
            try
            {
                zf = new ZipFile(zipPath);
                int count = 0;
                foreach (ZipEntry zipEntry in zf)
                    count++;
                totalNum = count;
                nowNum = 0;
                extracting = true;

                foreach (ZipEntry zipEntry in zf)
                {
                    nowNum++;
                    if (!zipEntry.IsFile)
                        continue;

                    string entryFileName = zipEntry.Name;
                    byte[] buffer = new byte[4096];
                    Stream zipStream = zf.GetInputStream(zipEntry);
                    string fullZipToPath = Path.Combine(outFolder, entryFileName);
                    string directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                        Directory.CreateDirectory(directoryName);

                    using (FileStream streamWriter = File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipStream, streamWriter, buffer);
                    }

                    yield return null;
                }
            }
            finally
            {
                if (zf != null)
                {
                    zf.IsStreamOwner = true;
                    zf.Close();
                }
                extracting = false;
            }
        }

        private static void SafeDeleteFile(string path)
        {
            if (File.Exists(path))
            {
                try { File.Delete(path); }
                catch (Exception e) { Debug.LogWarning($"Failed to delete file {path}: {e.Message}"); }
            }
        }
    }
}
