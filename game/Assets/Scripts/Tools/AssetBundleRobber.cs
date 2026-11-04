using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System.Threading;
using System.Collections.Concurrent;
using System;
using TMPro;
using Cysharp.Threading.Tasks;

public class AssetBundleRobber : MonoBehaviour
{
    public Text text;
    private static Text sText;
    public TMP_InputField input;

    public bool mode_Window = true;
    public bool mode_Android = false;
    public bool mode_IOS = false;
    public bool mode_Swtich = false;

    public bool fullCopy;

    private string pathAB;
    private string pathStore;

    private const string PATH_AB_WINDOWS = "D:/Game/Steam/steamapps/common/Yu-Gi-Oh!  Master Duel/LocalData/16165626/0000/";
    private const string PATH_AB_ANDROID = "D:/Game/Steam/steamapps/common/Yu-Gi-Oh!  Master Duel/LocalData/Android/0000/";
    private const string PATH_AB_IOS = "D:/Game/Steam/steamapps/common/Yu-Gi-Oh!  Master Duel/LocalData/iOS/0000/";
    private const string PATH_AB_Swtich = "D:/Game/Steam/steamapps/common/Yu-Gi-Oh!  Master Duel/LocalData/Swtich/";

    private const string PATH_STORE_WINDOWS = "Platforms/StandaloneWindows64/Robber/";
    private const string PATH_STORE_ANDROID = "Platforms/Android/Robber/";
    private const string PATH_STORE_IOS = "Platforms/iOS/Robber/";
    private const string PATH_STORE_Swtich = "Platforms/Switch/Robber/";

    public static int fileCount;
    public static int currentFileCount;
    private readonly object _lock = new();
    private bool noSave = false;
    private readonly ConcurrentQueue<string> logQueue = new();
    private int count = 0;
    private AssetStudio.AssetsManager assetManager;

    private readonly int threads = 32;
    private AssetType copyAssetType = AssetType.All;

    public struct AssetbundleInfo
    {
        public string path;
        public string name;
        public List<string> dependencies;
    }
    public static List<AssetbundleInfo> files = new();
    public static List<AssetbundleInfo> newFiles = new();

    public static void SetHint(string hint)
    {
        if (sText == null)
            sText = GameObject.Find("Canvas").transform.GetChild(1).GetComponent<Text>();

        sText.text = hint;
    }

    private void Start()
    {
        _ = IniAsync();
    }

    private async UniTask IniAsync()
    {
        sText = text;
        assetManager = GetComponent<AssetStudio.AssetsManager>();

        Application.targetFrameRate = 0;

        await UniTask.RunOnThreadPool(() => 
        {
            if (mode_Android)
            {
                pathAB = PATH_AB_ANDROID;
                pathStore = PATH_STORE_ANDROID;
            }
            else if (mode_IOS)
            {
                pathAB = PATH_AB_IOS;
                pathStore = PATH_STORE_IOS;
            }
            else if (mode_Swtich)
            {
                pathAB = PATH_AB_Swtich;
                pathStore = PATH_STORE_Swtich;
            }
            else
            {
                pathAB = PATH_AB_WINDOWS;
                pathStore = PATH_STORE_WINDOWS;
            }

            copyAssetType = AssetType.All;

            Initialize();
        });

        StartCoroutine(RefreshFileResources());

    }

    public void CopyAB()
    {
        Copy(input.text);
    }

    private void Copy(string path)
    {
        foreach (var file in files)
        {
            if (file.path == path)
            {
                Directory.CreateDirectory(pathStore + path);
                File.Copy(GetFullPath(file.path), pathStore + path + "/" + file.path);
                foreach (var dep in file.dependencies)
                {
                    File.Copy(GetFullPath(dep), pathStore + path + "/" + dep);
                }
            }

        }
        Debug.Log(path + ": Copy Done!");
    }

    private void Initialize()
    {
        if (!Directory.Exists(pathStore))
            Directory.CreateDirectory(pathStore);
        var fullText = "";
        if (File.Exists(pathStore + "FileList.txt"))
            fullText = File.ReadAllText(pathStore + "FileList.txt");
        else
            noSave = true;
        var lines = fullText.Replace("\r", "").Split('\n');
        AssetbundleInfo file = new AssetbundleInfo();
        file.dependencies = new List<string>();
        foreach (var line in lines)
        {
            if (!line.StartsWith("-"))
            {
                if (file.name != null)
                {
                    files.Add(file);
                    file = new AssetbundleInfo
                    {
                        dependencies = new List<string>(),
                        path = line
                    };
                }
                else
                    file.path = line;
            }
            else if (line.StartsWith("--"))
                file.dependencies.Add(line.Replace("--", ""));
            else
                file.name = line.Replace("-", "");
        }
        if (!noSave)
        {
            Debug.Log("Preloged：" + files.Count);
        }
        else
        {
            Debug.Log("No FileList to load.");
        }
    }

    private void AddLog(int i)
    {
        var file = assetManager.assetsFileList[i];
        count++;
        string filePath = file.originalPath.Substring(file.originalPath.Length - 8);

        string fileName = "";
        foreach (var obj in file.Objects)
            if (obj is AssetStudio.AssetBundle assetBundle)
                foreach (var pair in assetBundle.m_Container)
                {
                    fileName = pair.Key;
                    break;
                }

        var fileStruct = new AssetbundleInfo();
        fileStruct.path = filePath;
        fileStruct.name = fileName;
        fileStruct.dependencies = GetDependencies(filePath);
        lock (_lock)
        {
            files.Add(fileStruct);
            newFiles.Add(fileStruct);
        }

        var content = string.Empty;
        content += fileStruct.path + "\r\n";
        content += "-" + fileStruct.name + "\r\n";
        foreach (var depend in fileStruct.dependencies)
            content += "--" + depend + "\r\n";

        logQueue.Enqueue(content);
    }

    private IEnumerator RefreshFileResources()
    {
        var ie = assetManager.LoadFolderAsync(pathAB);
        StartCoroutine(ie);
        while (ie.MoveNext())
            yield return null;

        Debug.Log("new files: " + assetManager.assetsFileList.Count);

        indexQueue = new ConcurrentQueue<int>();
        workerThreads = new List<Thread> { };
        isProcessing = true;

        for (int i = 0; i < threads; i++)
        {
            Thread workerThread = new Thread(ProcessLogs);
            workerThreads.Add(workerThread);
            workerThread.Start();
        }

        ie = EnqueueLogs(assetManager.assetsFileList.Count);
        while (ie.MoveNext())
        {
            text.text = "Logging: " + count + "/" + assetManager.assetsFileList.Count;
            if (logQueue.TryDequeue(out var log))
                File.AppendAllText(pathStore + "FileList.txt", log);
            yield return null;
        }

        while (logQueue.TryDequeue(out var log))
        {
            File.AppendAllText(pathStore + "FileList.txt", log);
            text.text = "Writing Left: " + logQueue.Count;
            yield return null;
        }

        StartCoroutine(CopyBundles());
    }

    private ConcurrentQueue<int> indexQueue;
    private List<Thread> workerThreads;
    private bool isProcessing;

    public void StopProcessingLogs()
    {
        isProcessing = false;

        foreach (Thread workerThread in workerThreads)
        {
            workerThread.Join();
        }

        workerThreads.Clear();
    }

    private IEnumerator EnqueueLogs(int count)
    {
        int processedIndexCount = 0;
        int indexesPerFrame = 32;

        for (int i = 0; i < count; i++)
        {
            indexQueue.Enqueue(i);
            processedIndexCount++;

            if (processedIndexCount >= indexesPerFrame)
            {
                processedIndexCount = 0;
                yield return null;
            }
        }

        while (!indexQueue.IsEmpty)
        {
            yield return null;
        }

        StopProcessingLogs();
    }

    private void ProcessLogs()
    {
        while (isProcessing)
        {
            if (indexQueue.TryDequeue(out int index))
            {
                AddLog(index);
            }
            else
            {
                Thread.Sleep(10);
            }
        }
    }

    public void OnApplicationQuit()
    {
        StopProcessingLogs();
    }

    private IEnumerator CopyBundles()
    {
        fileCount = files.Count;
        currentFileCount = 0;
        List<AssetbundleInfo> targetFiles;
        if (fullCopy)
            targetFiles = files;
        else
            targetFiles = newFiles;

        foreach (var file in targetFiles)
        {
            currentFileCount++;
            var type = GetAssetType(file.name);

            if(copyAssetType != AssetType.All)
                if(type != copyAssetType)
                    continue;

            if (!pathStore.Contains("Windows") && AssetIsWindowsOnly(type))
                continue;
            
            if (type == AssetType.AvatarStand)
            {
                if (!Directory.Exists(pathStore + "AvatarStand"))
                    Directory.CreateDirectory(pathStore + "AvatarStand");
                var targetName = pathStore + "AvatarStand/" + Path.GetFileName(file.name).Replace(EXTENSION_PREFAB, string.Empty).Replace("avatarstand_", "AvatarStand_");

                if (!File.Exists(targetName))
                    File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if (type == AssetType.FrameMat)
            {
                if (!Directory.Exists(pathStore + "Frame"))
                    Directory.CreateDirectory(pathStore + "Frame");
                var targetName = pathStore + "Frame/" + Path.GetFileName(file.name).Replace(EXTENSION_MAT, string.Empty).Replace("profileframemat", "ProfileFrameMat");
                if(!File.Exists(targetName))
                    File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if (type == AssetType.Grave)
            {
                if (!Directory.Exists(pathStore + "Grave"))
                    Directory.CreateDirectory(pathStore + "Grave");
                var targetName = pathStore + "Grave/" + Path.GetFileName(file.name).Replace(EXTENSION_PREFAB, string.Empty).Replace("grave_", "Grave_");
                if (!File.Exists(targetName))
                    File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if (type == AssetType.Mat)
            {
                if (!Directory.Exists(pathStore + "Mat"))
                    Directory.CreateDirectory(pathStore + "Mat");
                var targetName = pathStore + "Mat/" + Path.GetFileName(file.name).Replace(EXTENSION_PREFAB, string.Empty).Replace("mat_", "Mat_");
                if (!File.Exists(targetName))
                    File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if (type == AssetType.Mate)
            {
                if (!Directory.Exists(pathStore + "Mate"))
                    Directory.CreateDirectory(pathStore + "Mate");
                
                var targetName = pathStore + "Mate/" + Path.GetFileName(file.name).Replace(EXTENSION_PREFAB, string.Empty)
                    .Replace("_model", "_Model").Replace("_sd_", "_SD_").Replace("m", "M").Replace("v", "V");

                if (file.dependencies.Count == 0)
                {
                    if (!File.Exists(targetName))
                        File.Copy(GetFullPath(file.path), targetName, true);
                }
                else
                {
                    var targetFolder = targetName;
                    if(!Directory.Exists(targetFolder))
                        Directory.CreateDirectory(targetFolder);
                    File.Copy(GetFullPath(file.path), Path.Combine(targetFolder, file.path), true);
                    foreach (string depen in file.dependencies)
                    {
                        if (File.Exists(GetFullPath(depen)))
                        {
                            if (!File.Exists(targetFolder + "/" + depen))
                                File.Copy(GetFullPath(depen), targetFolder + "/" + depen, true);
                        }
                        else
                            Debug.Log("未找到" + file.path + "的依赖：" + depen + ": " + GetFullPath(depen));
                    }
                }
            }
            else if (type == AssetType.Protector)
            {
                if (!Directory.Exists(pathStore + "Protector"))
                    Directory.CreateDirectory(pathStore + "Protector");
                string subDir = "107" + Regex.Split(file.name, "/")[4];
                var targetFolder = pathStore + "Protector/" + subDir;
                var targetName = targetFolder + "/" + Path.GetFileName(file.name).Replace("pmat.mat", subDir)
                    .Replace("protectoricon", "ProtectorIcon").Replace(EXTENSION_PNG, string.Empty);

                if (!Directory.Exists(targetFolder))
                    Directory.CreateDirectory(targetFolder);
                if (!File.Exists(targetName))
                    File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if (type == AssetType.Wallpaper)
            {
                if (!Directory.Exists(pathStore + "Wallpaper"))
                    Directory.CreateDirectory(pathStore + "Wallpaper");
                var subDir = Path.GetFileName(file.name).Replace(EXTENSION_PREFAB, string.Empty).Replace("front", "Front");
                var targetFolder = pathStore + "Wallpaper/" + subDir;
                if (!Directory.Exists(targetFolder))
                    Directory.CreateDirectory(targetFolder);

                File.Copy(GetFullPath(file.path), Path.Combine(targetFolder, file.path), true);
                var depens = new List<string>(file.dependencies);
                foreach (string depen in depens)
                {
                    if (File.Exists(GetFullPath(depen)))
                        File.Copy(GetFullPath(depen), targetFolder + "/" + depen, true);
                    else
                        Debug.Log("未找到" + file.path + "的依赖：" + depen);
                }
            }
            else if (type == AssetType.Background)
            {
                if (!Directory.Exists(pathStore + "Background"))
                    Directory.CreateDirectory(pathStore + "Background");
                string subDir = Path.GetFileName(file.name).Replace("back", "Back").Replace(EXTENSION_PREFAB, "");
                var targetFolder = pathStore + "Background/" + subDir;
                if (!Directory.Exists(targetFolder))
                    Directory.CreateDirectory(targetFolder);
                if (!File.Exists(targetFolder + "/" + subDir))
                    File.Copy(GetFullPath(file.path), Path.Combine(targetFolder, file.path), true);
                else
                    Debug.LogError($"Background File {file.path} already exist.");

                var depens = new List<string>(file.dependencies);
                foreach (string depen in depens)
                {
                    if (File.Exists(GetFullPath(depen)))
                        File.Copy(GetFullPath(depen), targetFolder + "/" + depen, true);
                    else
                        Debug.LogError("未找到" + file.path + "的依赖：" + depen);
                }
            }
            else if (type == AssetType.Card)
            {
                if (!Directory.Exists(pathStore + "Card"))
                    Directory.CreateDirectory(pathStore + "Card");
                string subDir = int.Parse(Regex.Split(file.name, "/")[6].Replace("ef", string.Empty)).ToString();
                subDir = GetYdkID(subDir);
                if (file.name.Contains(LABEL_HD))
                    subDir = "HD" + subDir;
                else if (file.name.Contains(LABEL_SD))
                    subDir = "SD" + subDir;
                var targetFolder = pathStore + "Card/" + subDir;
                if (!Directory.Exists(targetFolder))
                    Directory.CreateDirectory(targetFolder);
                File.Copy(GetFullPath(file.path), Path.Combine(targetFolder, file.path), true);
                var depens = new List<string>(file.dependencies);
                foreach (string depen in depens)
                {
                    if (File.Exists(GetFullPath(depen)))
                    {
                        if (!File.Exists(targetFolder + "/" + depen))
                            File.Copy(GetFullPath(depen), targetFolder + "/" + depen, true);
                    }
                    else
                        Debug.LogError($"未找到 {file.path} 的依赖：{depen}");
                }
            }
            else if (type == AssetType.MonsterCutin)
            {
                if (file.name.Contains(LABEL_SD))
                    if (pathStore.Contains("Windows"))
                        continue;
                if (!Directory.Exists(pathStore + "MonsterCutin"))
                    Directory.CreateDirectory(pathStore + "MonsterCutin");
                string subDir = Regex.Split(file.name, "/")[7].Replace("p", "");
                subDir = GetYdkID(subDir);
                var targetFolder = pathStore + "MonsterCutin/" + subDir;
                if (!Directory.Exists(targetFolder))
                    Directory.CreateDirectory(targetFolder);
                File.Copy(GetFullPath(file.path), Path.Combine(targetFolder, file.path), true);
                var depens = new List<string>(file.dependencies);
                foreach (string depen in depens)
                {
                    if (File.Exists(GetFullPath(depen)))
                    {
                        if (!File.Exists(targetFolder + "/" + depen))
                            File.Copy(GetFullPath(depen), targetFolder + "/" + depen, true);
                    }
                    else
                        Debug.LogError($"未找到 {file.path} 的依赖：{depen}");
                }
            }
            else if (type == AssetType.SpecialWin)
            {
                if (file.name.Contains(LABEL_SD))
                    if (pathStore.Contains("Windows"))
                        continue;
                if (!Directory.Exists(pathStore + "SpecialWin"))
                    Directory.CreateDirectory(pathStore + "SpecialWin");
                string subDir = Regex.Split(file.name, "/")[8];
                if (subDir.Contains(EXTENSION_PREFAB))//4027 艾克佐迪亚
                {
                    subDir = subDir.Replace(EXTENSION_PREFAB, string.Empty).Replace("summonspecialwin", string.Empty);
                }
                else
                    subDir = subDir.Replace("p", string.Empty);
                subDir = GetYdkID(subDir);
                var targetFolder = pathStore + "SpecialWin/" + subDir;
                if (!Directory.Exists(targetFolder))
                    Directory.CreateDirectory(targetFolder);
                File.Copy(GetFullPath(file.path), Path.Combine(targetFolder, file.path), true);
                var depens = new List<string>(file.dependencies);
                foreach (string depen in depens)
                {
                    if (File.Exists(GetFullPath(depen)))
                    {
                        if (!File.Exists(targetFolder + "/" + depen))
                            File.Copy(GetFullPath(depen), targetFolder + "/" + depen, true);
                    }
                    else
                        Debug.LogError($"未找到 {file.path} 的依赖：{depen}");
                }
            }
            else if (type == AssetType.BGM)
            {
                if (!Directory.Exists(pathStore + "Sound/BGM"))
                    Directory.CreateDirectory(pathStore + "Sound/BGM");
                var targetName = pathStore + "Sound/BGM/" + Path.GetFileName(file.name).Replace(EXTENSION_WAV, string.Empty);
                File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if (type == AssetType.SE_DUEL)
            {
                if (!Directory.Exists(pathStore + "Sound/SE_DUEL"))
                    Directory.CreateDirectory(pathStore + "Sound/SE_DUEL");
                var targetName = pathStore + "Sound/SE_DUEL/" + Path.GetFileName(file.name).Replace(EXTENSION_WAV, string.Empty);
                File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if (type == AssetType.SE_FIELD)
            {
                if (!Directory.Exists(pathStore + "Sound/SE_FIELD"))
                    Directory.CreateDirectory(pathStore + "Sound/SE_FIELD");
                var targetName = pathStore + "Sound/SE_FIELD/" + Path.GetFileName(file.name).Replace(EXTENSION_WAV, string.Empty);
                File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if (type == AssetType.SE_MATE)
            {
                if (!Directory.Exists(pathStore + "Sound/SE_MATE"))
                    Directory.CreateDirectory(pathStore + "Sound/SE_MATE");
                var targetName = pathStore + "Sound/SE_MATE/" + Path.GetFileName(file.name).Replace(EXTENSION_WAV, string.Empty);
                File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if (type == AssetType.SE_SYS)
            {
                if (!Directory.Exists(pathStore + "Sound/SE_SYS"))
                    Directory.CreateDirectory(pathStore + "Sound/SE_SYS");
                var targetName = pathStore + "Sound/SE_SYS/" + Path.GetFileName(file.name).Replace(EXTENSION_WAV, string.Empty);
                try
                {
                    File.Copy(GetFullPath(file.path), targetName, true);
                }
                catch(Exception e)
                {
                    Debug.LogException(e);
                }
            }

            //Icons
            else if(type == AssetType.AvatarStandIcon)
            {
                if (!Directory.Exists(pathStore + "Icon/AvatarBase/SD"))
                    Directory.CreateDirectory(pathStore + "Icon/AvatarBase/SD");
                var targetName = pathStore + "Icon/AvatarBase/SD/"
                    + Path.GetFileName(file.name).Replace(EXTENSION_PNG, string.Empty).Replace("fieldavatarbaseicon", "FieldAvatarBaseIcon");
                if (!File.Exists(targetName))
                    File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if (type == AssetType.AvatarStandIconHD)
            {
                if (!Directory.Exists(pathStore + "Icon/AvatarBase/HD"))
                    Directory.CreateDirectory(pathStore + "Icon/AvatarBase/HD");
                var targetName = pathStore + "Icon/AvatarBase/HD/"
                    + Path.GetFileName(file.name).Replace(EXTENSION_PNG, string.Empty).Replace("fieldavatarbaseicon", "FieldAvatarBaseIcon");
                if (!File.Exists(targetName))
                    File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if(type == AssetType.DeckCaseIcon)
            {
                if (!Directory.Exists(pathStore + "Icon/DeckCase/S"))
                    Directory.CreateDirectory(pathStore + "Icon/DeckCase/S");
                var targetName = pathStore + "Icon/DeckCase/S/"
                    + Path.GetFileName(file.name).Replace(EXTENSION_PNG, string.Empty).Replace("deckcase", "DeckCase");
                if (!File.Exists(targetName))
                    File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if(type == AssetType.DeckCaseL
             || type == AssetType.DeckCaseOpen
             || type == AssetType.DeckCaseReverse)
            {
                if (!Directory.Exists(pathStore + "Icon/DeckCase/SD"))
                    Directory.CreateDirectory(pathStore + "Icon/DeckCase/SD");
                var targetName = pathStore + "Icon/DeckCase/SD/"
                    + Path.GetFileName(file.name)
                        .Replace(EXTENSION_PNG, string.Empty)
                        .Replace("deckcase", "DeckCase")
                        .Replace("_open", "_Open")
                        .Replace("_l", "_L");
                if (!File.Exists(targetName))
                    File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if (type == AssetType.DeckCaseLHD
             || type == AssetType.DeckCaseOpenHD
             || type == AssetType.DeckCaseReverseHD)
            {
                if (!Directory.Exists(pathStore + "Icon/DeckCase/HD"))
                    Directory.CreateDirectory(pathStore + "Icon/DeckCase/HD");
                var targetName = pathStore + "Icon/DeckCase/HD/"
                    + Path.GetFileName(file.name)
                        .Replace(EXTENSION_PNG, string.Empty)
                        .Replace("deckcase", "DeckCase")
                        .Replace("_open", "_Open")
                        .Replace("_l", "_L");
                if (!File.Exists(targetName))
                    File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if(type == AssetType.MatIcon)
            {
                if (!Directory.Exists(pathStore + "Icon/Field/SD"))
                    Directory.CreateDirectory(pathStore + "Icon/Field/SD");
                var targetName = pathStore + "Icon/Field/SD/"
                    + Path.GetFileName(file.name)
                        .Replace(EXTENSION_PNG, string.Empty)
                        .Replace("fieldicon", "FieldIcon");
                if (!File.Exists(targetName))
                    File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if (type == AssetType.MatIconHD)
            {
                if (!Directory.Exists(pathStore + "Icon/Field/HD"))
                    Directory.CreateDirectory(pathStore + "Icon/Field/HD");
                var targetName = pathStore + "Icon/Field/HD/"
                    + Path.GetFileName(file.name)
                        .Replace(EXTENSION_PNG, string.Empty)
                        .Replace("fieldicon", "FieldIcon");
                if (!File.Exists(targetName))
                    File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if (type == AssetType.GraveIcon)
            {
                if (!Directory.Exists(pathStore + "Icon/Grave/SD"))
                    Directory.CreateDirectory(pathStore + "Icon/Grave/SD");
                var targetName = pathStore + "Icon/Grave/SD/"
                    + Path.GetFileName(file.name)
                        .Replace(EXTENSION_PNG, string.Empty)
                        .Replace("fieldobjicon", "FieldObjIcon");
                if (!File.Exists(targetName))
                    File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if (type == AssetType.GraveIconHD)
            {
                if (!Directory.Exists(pathStore + "Icon/Grave/HD"))
                    Directory.CreateDirectory(pathStore + "Icon/Grave/HD");
                var targetName = pathStore + "Icon/Grave/HD/"
                    + Path.GetFileName(file.name)
                        .Replace(EXTENSION_PNG, string.Empty)
                        .Replace("fieldobjicon", "FieldObjIcon");
                if (!File.Exists(targetName))
                    File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if(type == AssetType.MateIcon)
            {
                if (!Directory.Exists(pathStore + "Icon/Mate"))
                    Directory.CreateDirectory(pathStore + "Icon/Mate");
                var targetName = pathStore + "Icon/Mate/"
                    + Path.GetFileName(file.name)
                        .Replace(EXTENSION_PNG, string.Empty);
                if (!File.Exists(targetName))
                    File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if(type == AssetType.ProfileIcon)
            {
                if (!Directory.Exists(pathStore + "Icon/ProfileIcon/S"))
                    Directory.CreateDirectory(pathStore + "Icon/ProfileIcon/S");
                var targetName = pathStore + "Icon/ProfileIcon/S/"
                    + Path.GetFileName(file.name)
                        .Replace(EXTENSION_PNG, string.Empty)
                        .Replace("profileicon", "ProfileIcon");
                if (!File.Exists(targetName))
                    File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if (type == AssetType.ProfileIconL)
            {
                if (!Directory.Exists(pathStore + "Icon/ProfileIcon/SD"))
                    Directory.CreateDirectory(pathStore + "Icon/ProfileIcon/SD");
                var targetName = pathStore + "Icon/ProfileIcon/SD/"
                    + Path.GetFileName(file.name)
                        .Replace(EXTENSION_PNG, string.Empty)
                        .Replace("profileicon", "ProfileIcon")
                        .Replace("_l", "_L");
                if (!File.Exists(targetName))
                    File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if (type == AssetType.ProfileIconLHD)
            {
                if (!Directory.Exists(pathStore + "Icon/ProfileIcon/HD"))
                    Directory.CreateDirectory(pathStore + "Icon/ProfileIcon/HD");
                var targetName = pathStore + "Icon/ProfileIcon/HD/"
                    + Path.GetFileName(file.name)
                        .Replace(EXTENSION_PNG, string.Empty)
                        .Replace("profileicon", "ProfileIcon")
                        .Replace("_l", "_L");
                if (!File.Exists(targetName))
                    File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if(type == AssetType.WallpaperIcon)
            {
                if (!Directory.Exists(pathStore + "Icon/Wallpaper"))
                    Directory.CreateDirectory(pathStore + "Icon/Wallpaper");
                var targetName = pathStore + "Icon/Wallpaper/"
                    + Path.GetFileName(file.name)
                        .Replace(EXTENSION_PNG, string.Empty)
                        .Replace("wallpapericon", "WallPaperIcon");
                if (!File.Exists(targetName))
                    File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if(type == AssetType.FrameIcon)
            {
                if (!Directory.Exists(pathStore + "Icon/ProfileFrame/S"))
                    Directory.CreateDirectory(pathStore + "Icon/ProfileFrame/S");
                var targetName = pathStore + "Icon/ProfileFrame/S/"
                    + Path.GetFileName(file.name)
                        .Replace(EXTENSION_PNG, string.Empty)
                        .Replace("profileframe", "ProfileFrame");
                if (!File.Exists(targetName))
                    File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if (type == AssetType.FrameL)
            {
                if (!Directory.Exists(pathStore + "Icon/ProfileFrame/SD"))
                    Directory.CreateDirectory(pathStore + "Icon/ProfileFrame/SD");
                var targetName = pathStore + "Icon/ProfileFrame/SD/"
                    + Path.GetFileName(file.name)
                        .Replace(EXTENSION_PNG, string.Empty)
                        .Replace("profileframe", "ProfileFrame")
                        .Replace("_l", "_L");
                if (!File.Exists(targetName))
                    File.Copy(GetFullPath(file.path), targetName, true);
            }
            else if (type == AssetType.FrameLHD)
            {
                if (!Directory.Exists(pathStore + "Icon/ProfileFrame/HD"))
                    Directory.CreateDirectory(pathStore + "Icon/ProfileFrame/HD");
                var targetName = pathStore + "Icon/ProfileFrame/HD/"
                    + Path.GetFileName(file.name)
                        .Replace(EXTENSION_PNG, string.Empty)
                        .Replace("profileframe", "ProfileFrame")
                        .Replace("_l", "_L");
                if (!File.Exists(targetName))
                    File.Copy(GetFullPath(file.path), targetName, true);
            }

            // Text
            else if(type == AssetType.XML)
            {
                if (!Directory.Exists(pathStore + "XML"))
                    Directory.CreateDirectory(pathStore + "XML");
                var targetName = pathStore + "XML/"
                    + Path.GetFileName(file.name)
                        .Replace(EXTENSION_XML, string.Empty);
                if (!File.Exists(targetName))
                    File.Copy(GetFullPath(file.path), targetName, true);
            }

            text.text = "Copying: " + currentFileCount + "/" + fileCount;
            yield return null;
        }
        text.text = "Copy Complete.";
    }

    private const string prefix = "assets/resourcesassetbundle";
    private const string LABEL_SD = "/sd/";
    private const string LABEL_HD = "/highend_hd/";
    private const string EXTENSION_PNG = ".png";
    private const string EXTENSION_PREFAB = ".prefab";
    private const string EXTENSION_WAV = ".wav";
    private const string EXTENSION_MAT = ".mat";
    private const string EXTENSION_XML = ".xml";

    private AssetType GetAssetType(string name)
    {
        if (!name.StartsWith(prefix))
            return AssetType.None;

        if (name.Contains("/duel/bg/avatarstand/"))
        {
            if (name.EndsWith(EXTENSION_PREFAB))
                return AssetType.AvatarStand;
        }
        else if (name.Contains("/images/profileframe/"))
        {
            if (name.EndsWith(EXTENSION_MAT))
                return AssetType.FrameMat;
            else if (name.EndsWith(EXTENSION_PNG))
            {
                if (name.Contains(LABEL_HD))
                    return AssetType.FrameLHD;
                else if (name.Contains(LABEL_SD))
                    return AssetType.FrameL;
                else
                    return AssetType.FrameIcon;
            }
        }
        else if (name.Contains("/duel/bg/grave/"))
        {
            if (name.EndsWith(EXTENSION_PREFAB))
                return AssetType.Grave;
        }
        else if (name.Contains("/duel/bg/mat/"))
        {
            if (name.EndsWith(EXTENSION_PREFAB))
                return AssetType.Mat;
        }
        else if (name.Contains("/mate/"))
        {
            if (name.EndsWith(EXTENSION_PREFAB))
                return AssetType.Mate;
            else if (name.EndsWith(EXTENSION_PNG))
                return AssetType.MateIcon;
        }
        else if (name.Contains("/protector/"))
        {
            if (!name.Contains("/protector/shaders/"))
                return AssetType.Protector;
        }
        else if (name.Contains("/wallpaper/"))
        {
            if (name.EndsWith(EXTENSION_PREFAB))
                return AssetType.Wallpaper;
            else if (name.EndsWith(EXTENSION_PNG) && name.Contains("wallpapericon"))
                return AssetType.WallpaperIcon;
        }
        else if (name.Contains("/prefabs/outgamebg/back/"))
        {
            if (name.EndsWith(EXTENSION_PREFAB))
                return AssetType.Background;
        }
        else if (name.Contains("/duel/timeline/card/"))
        {
            if (name.EndsWith(EXTENSION_PREFAB))
                return AssetType.Card;
        }
        else if (name.Contains("/duel/timeline/duel/monstercutin/"))
        {
            if (name.EndsWith(EXTENSION_PREFAB))
                return AssetType.MonsterCutin;
        }
        else if (name.Contains("/duel/timeline/duel/universal/summon/summonspecialwin/"))
        {
            if (name.EndsWith(EXTENSION_PREFAB))
                if (Path.GetFileName(name).Contains("summonspecialwin"))
                    return AssetType.SpecialWin;
        }
        else if (name.Contains("/bgm/"))
        {
            if (name.EndsWith(EXTENSION_WAV))
                return AssetType.BGM;
        }
        else if (name.Contains("/se_duel/"))
        {
            if (name.EndsWith(EXTENSION_WAV))
                return AssetType.SE_DUEL;
        }
        else if (name.Contains("/se_field/"))
        {
            if (name.EndsWith(EXTENSION_WAV))
                return AssetType.SE_FIELD;
        }
        else if (name.Contains("/se_mate/"))
        {
            if (name.EndsWith(EXTENSION_WAV))
                return AssetType.SE_MATE;
        }
        else if (name.Contains("/se_sys/"))
        {
            if (name.EndsWith(EXTENSION_WAV))
                return AssetType.SE_SYS;
        }
        //Icons
        else if (name.StartsWith("assets/resourcesassetbundle/images/fieldavatarbase/"))
        {
            if (name.EndsWith(EXTENSION_PNG))
            {
                if (name.Contains(LABEL_HD))
                    return AssetType.AvatarStandIconHD;
                else if (name.Contains(LABEL_SD))
                    return AssetType.AvatarStandIcon;
            }
        }
        else if (name.StartsWith("assets/resourcesassetbundle/images/deckcase/"))
        {
            if (name.EndsWith(EXTENSION_PNG))
            {
                if (name.Contains(LABEL_HD))
                {
                    if (name.EndsWith("_l_reverse.png"))
                        return AssetType.DeckCaseReverseHD;
                    else if (name.EndsWith("_open_l.png"))
                        return AssetType.DeckCaseOpenHD;
                    else if (name.EndsWith("_l.png"))
                        return AssetType.DeckCaseLHD;
                }
                else if (name.Contains(LABEL_SD))
                {
                    if (name.EndsWith("_l_reverse.png"))
                        return AssetType.DeckCaseReverse;
                    else if (name.EndsWith("_open_l.png"))
                        return AssetType.DeckCaseOpen;
                    else if (name.EndsWith("_l.png"))
                        return AssetType.DeckCaseL;
                }
                else
                    return AssetType.DeckCaseIcon;
            }
        }
        else if (name.StartsWith("assets/resourcesassetbundle/images/field/"))
        {
            if (name.EndsWith(EXTENSION_PNG))
            {
                if (name.Contains(LABEL_HD))
                    return AssetType.MatIconHD;
                else if (name.Contains(LABEL_SD))
                    return AssetType.MatIcon;
            }
        }
        else if (name.StartsWith("assets/resourcesassetbundle/images/fieldobj/"))
        {
            if (name.EndsWith(EXTENSION_PNG))
            {
                if (name.Contains(LABEL_HD))
                    return AssetType.GraveIconHD;
                else if (name.Contains(LABEL_SD))
                    return AssetType.GraveIcon;
            }
        }
        else if (name.StartsWith("assets/resourcesassetbundle/images/profileicon/"))
        {
            if (name.EndsWith(EXTENSION_PNG))
            {
                if (name.EndsWith("_l.png") && name.Contains(LABEL_HD))
                    return AssetType.ProfileIconLHD;
                else if (name.EndsWith("_l.png") && name.Contains(LABEL_SD))
                    return AssetType.ProfileIconL;
                else
                    return AssetType.ProfileIcon;
            }
        }
        // Text
        else if (name.StartsWith("/sound/xml/"))
        {
            return AssetType.XML;
        }

        return AssetType.None;
    }

    public enum AssetType
    {
        None,
        All,
        AvatarStand,
        AvatarStandIcon,
        AvatarStandIconHD,
        Card,
        DeckCaseIcon,
        DeckCaseL,
        DeckCaseLHD,
        DeckCaseReverse,
        DeckCaseReverseHD,
        DeckCaseOpen,
        DeckCaseOpenHD,
        FrameIcon,
        FrameL,
        FrameLHD,
        FrameMat,
        Grave,
        GraveIcon,
        GraveIconHD,
        Mat,
        MatIcon,
        MatIconHD,
        Mate,
        MateIcon,
        ProfileIcon,
        ProfileIconL,
        ProfileIconLHD,
        MonsterCutin,
        Protector,
        Wallpaper,
        WallpaperIcon,
        Background,
        SpecialWin,
        BGM,
        SE_DUEL,
        SE_FIELD,
        SE_MATE,
        SE_SYS,
        XML,
    }

    private bool AssetIsWindowsOnly(AssetType type)
    {
        return type switch
        {
            AssetType.AvatarStandIcon => true,
            AssetType.AvatarStandIconHD => true,
            AssetType.DeckCaseIcon => true,
            AssetType.DeckCaseL => true,
            AssetType.DeckCaseLHD => true,
            AssetType.DeckCaseReverse => true,
            AssetType.DeckCaseReverseHD => true,
            AssetType.DeckCaseOpen => true,
            AssetType.DeckCaseOpenHD => true,
            AssetType.FrameIcon => true,
            AssetType.FrameL => true,
            AssetType.FrameLHD => true,
            AssetType.GraveIcon => true,
            AssetType.GraveIconHD => true,
            AssetType.MatIcon => true,
            AssetType.MatIconHD => true,
            AssetType.MateIcon => true,
            AssetType.ProfileIcon => true,
            AssetType.ProfileIconL => true,
            AssetType.ProfileIconLHD => true,
            AssetType.WallpaperIcon => true,
            AssetType.BGM => true,
            AssetType.SE_DUEL => true,
            AssetType.SE_FIELD => true,
            AssetType.SE_MATE => true,
            AssetType.SE_SYS => true,
            AssetType.XML => true,
            _ => false,
        };
    }

    private List<string> GetDependencies(string fileName, List<string> parentDepends = null)
    {
        byte[] bytes = Decompress(fileName);
        List<int> dependencyPositions = new List<int>();
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == 0x2F)
                if (i + 9 < bytes.Length)
                    if (bytes[i + 2] == bytes[i - 1])
                        if (bytes[i + 1] == bytes[i - 2])
                        {
                            bool check = true;
                            if (bytes[i + 9] != 0x0)
                                check = false;
                            if (check)
                            {
                                for (int j = 1; j < 9; j++)
                                {
                                    if (bytes[i + j] >= 48 && bytes[i + j] <= 57
                                        || bytes[i + j] >= 65 && bytes[i + j] <= 90
                                        || bytes[i + j] >= 97 && bytes[i + j] <= 122
                                        )
                                    {
                                    }
                                    else
                                        check = false;
                                }
                            }
                            if (check)
                                dependencyPositions.Add(i);
                        }
        }

        List<string> dependencies = new List<string>();
        for (int i = 0; i < dependencyPositions.Count; i++)
        {
            List<byte> temp = new List<byte>();
            for (int j = dependencyPositions[i] + 1; j < dependencyPositions[i] + 9; j++)
            {
                temp.Add(bytes[j]);
            }
            var s = Encoding.UTF8.GetString(temp.ToArray());
            if (s != fileName)
            {
                if (parentDepends != null)
                {
                    if (!parentDepends.Contains(s))
                        dependencies.Add(s);
                }
                else
                    dependencies.Add(s);
            }
        }

        List<string> newParentDepends = new List<string>(dependencies);
        if (parentDepends != null)
            foreach (var dependency in parentDepends)
                if (!newParentDepends.Contains(dependency))
                    newParentDepends.Add(dependency);

        List<string> subdepends = new List<string>();
        foreach (var value in dependencies)
        {
            var ss = GetDependencies(value, newParentDepends);
            foreach (var s in ss)
                if (!subdepends.Contains(s))
                    subdepends.Add(s);
        }
        foreach (var value in subdepends)
            if (!dependencies.Contains(value))
                dependencies.Add(value);
        return dependencies;
    }

    private byte[] Decompress(string path)
    {
        var manager = new AssetsTools.NET.Extra.AssetsManager();
        if (!File.Exists(GetFullPath(path)))
        {
            Debug.Log("Not Find: " + path);
            return new byte[0];
        }
        BundleFileInstance bundleInst = manager.LoadBundleFile(GetFullPath(path), false);
        AssetBundleFile bundle = bundleInst.file;
        MemoryStream bundleStream = new MemoryStream();
        bundle.Unpack(new AssetsFileWriter(bundleStream));
        return bundleStream.GetBuffer();
    }

    private string GetYdkID(string mdID)
    {
        return MDPro3.Cid2Ydk.GetYDK(int.Parse(mdID)).ToString();
    }

    private string GetFullPath(string path)
    {
        if (path.Length <= 2)
        {
            Debug.Log("Too short: " + path);
            return string.Empty;
        }
        var returnValue = pathAB + path.Substring(0, 2) + "/" + path;
        //Debug.Log(path + ": " + returnValue);
        return returnValue;
    }
}
