using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class AssetReferenceTool
{
    private static bool _isProcessing = false;

    private static readonly HashSet<string> AssetExtensions = new HashSet<string>
    {
        ".prefab", ".unity", ".asset", ".mat",
        ".controller", ".anim", ".overrideController",
        ".physicMaterial", ".physicsMaterial2D", ".flare",
        ".fontsettings", ".guiskin", ".renderTexture",
        ".lighting", ".terrainlayer", ".signal", ".mixer"
    };

    // -------------------- 菜单：合并重复资产（基于文件名） --------------------
    [MenuItem("Assets/合并重复资产", true)]
    static bool ValidateMergeDuplicate()
    {
        return Selection.objects.Length > 1;
    }

    [MenuItem("Assets/合并重复资产")]
    static void MergeDuplicate()
    {
        _isProcessing = true;
        try { MergeDuplicateInternal(); }
        finally { _isProcessing = false; }
    }

    static void MergeDuplicateInternal()
    {
        var assetPaths = Selection.objects
            .Select(AssetDatabase.GetAssetPath)
            .Where(p => !AssetDatabase.IsValidFolder(p))
            .ToList();

        if (assetPaths.Count < 2)
        {
            EditorUtility.DisplayDialog("错误", "请至少选择两个资产文件（非文件夹）。", "确定");
            return;
        }

        var fileNames = assetPaths.Select(p => Path.GetFileNameWithoutExtension(p)).ToList();
        string baseName = fileNames.OrderBy(n => n.Length).First();
        var keepCandidates = new List<string>();
        var deletePaths = new List<string>();

        foreach (var path in assetPaths)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            if (name == baseName)
                keepCandidates.Add(path);
            else if (IsNumberedVariant(baseName, name))
                deletePaths.Add(path);
            else
            {
                EditorUtility.DisplayDialog("错误",
                    $"文件 \"{name}\" 不符合预期格式。\n期望所有文件共享基础名 \"{baseName}\"，且副本命名格式为 \"基础名_数字\"。",
                    "确定");
                return;
            }
        }

        if (keepCandidates.Count != 1 || deletePaths.Count == 0)
        {
            EditorUtility.DisplayDialog("错误", "未找到唯一基础文件或没有重复副本。", "确定");
            return;
        }

        string keepPath = keepCandidates[0];
        string keepName = Path.GetFileName(keepPath);
        string deleteNames = string.Join("\n", deletePaths.Select(Path.GetFileName));
        if (!EditorUtility.DisplayDialog("确认合并",
            $"将删除以下重复文件，并将所有引用指向保留文件：\n\n保留：{keepName}\n\n待删除：\n{deleteNames}\n\n此操作不可撤销！",
            "确认", "取消"))
            return;

        string keepGuid = AssetDatabase.AssetPathToGUID(keepPath);
        var deleteGuids = deletePaths.Select(AssetDatabase.AssetPathToGUID).ToList();
        ReplaceGUIDReferences(deleteGuids, keepGuid);

        foreach (var path in deletePaths) AssetDatabase.DeleteAsset(path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", "重复资产合并完成。", "确定");
    }

    static bool IsNumberedVariant(string baseName, string name)
    {
        if (!name.StartsWith(baseName + "_")) return false;
        string suffix = name.Substring(baseName.Length + 1);
        return Regex.IsMatch(suffix, @"^\d+$");
    }

    // -------------------- 菜单：替换单个资产的 GUID --------------------
    [MenuItem("Assets/替换资产GUID", true)]
    static bool ValidateReplaceGUID()
    {
        if (Selection.objects.Length != 1) return false;
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        return !string.IsNullOrEmpty(path) && !AssetDatabase.IsValidFolder(path);
    }

    [MenuItem("Assets/替换资产GUID")]
    static void ShowReplaceGUIDWindow()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        string guid = AssetDatabase.AssetPathToGUID(path);
        ReplaceGUIDWindow.OpenWindow(path, guid);
    }

    // -------------------- 菜单：查看资产引用 --------------------
    [MenuItem("Assets/查看资产引用", true)]
    static bool ValidateShowReferences()
    {
        if (Selection.objects.Length != 1) return false;
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        return !string.IsNullOrEmpty(path) && !AssetDatabase.IsValidFolder(path);
    }

    [MenuItem("Assets/查看资产引用")]
    static void ShowReferencesWindow()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        ReferenceViewerWindow.OpenWindow(path);
    }

    // -------------------- 菜单：查看资产被引用 --------------------
    [MenuItem("Assets/查看资产被引用", true)]
    static bool ValidateShowDependents()
    {
        if (Selection.objects.Length != 1) return false;
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        return !string.IsNullOrEmpty(path) && !AssetDatabase.IsValidFolder(path);
    }

    [MenuItem("Assets/查看资产被引用")]
    static void ShowDependentsWindow()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        DependentsViewerWindow.OpenWindow(path);
    }

    // -------------------- 菜单：通过 GUID 查找资产 --------------------
    [MenuItem("Assets/通过GUID查找资产", true)]
    static bool ValidateFindByGuid() { return true; }

    [MenuItem("Assets/通过GUID查找资产")]
    static void ShowFindByGuidWindow()
    {
        FindAssetByGuidWindow.OpenWindow();
    }

    // -------------------- 菜单：合并文件夹内的重复资产（增强版） --------------------
    [MenuItem("Assets/合并文件夹内的重复资产", true)]
    static bool ValidateMergeFolderDuplicates()
    {
        if (Selection.objects.Length != 1) return false;
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        return AssetDatabase.IsValidFolder(path);
    }

    [MenuItem("Assets/合并文件夹内的重复资产")]
    static void MergeFolderDuplicates()
    {
        string folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog("错误", "请选择一个有效的文件夹。", "确定");
            return;
        }

        _isProcessing = true;
        try { MergeFolderDuplicatesInternal(folderPath); }
        finally { _isProcessing = false; }
    }

    static void MergeFolderDuplicatesInternal(string folderPath)
    {
        string[] allPaths = AssetDatabase.GetAllAssetPaths()
            .Where(p => p.StartsWith(folderPath) && !AssetDatabase.IsValidFolder(p))
            .ToArray();

        if (allPaths.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "所选文件夹及其子文件夹内没有资产文件。", "确定");
            return;
        }

        // 第一阶段：主对象名称匹配
        var mainNameGroups = new Dictionary<string, List<string>>();
        foreach (string path in allPaths)
        {
            Object mainObj = AssetDatabase.LoadMainAssetAtPath(path);
            if (mainObj == null) continue;
            string mainName = mainObj.name;
            if (!mainNameGroups.ContainsKey(mainName))
                mainNameGroups[mainName] = new List<string>();
            mainNameGroups[mainName].Add(path);
        }

        var mergeGroups = new Dictionary<string, List<string>>();
        var processedPaths = new HashSet<string>();

        foreach (var kvp in mainNameGroups)
        {
            string mainName = kvp.Key;
            List<string> paths = kvp.Value;
            var baseFiles = paths.Where(p => Path.GetFileNameWithoutExtension(p) == mainName).ToList();
            if (baseFiles.Count != 1) continue;

            string basePath = baseFiles[0];
            var duplicates = paths.Where(p => p != basePath && Path.GetFileNameWithoutExtension(p) != mainName).ToList();
            if (duplicates.Count > 0)
            {
                mergeGroups[basePath] = duplicates;
                processedPaths.Add(basePath);
                foreach (var d in duplicates) processedPaths.Add(d);
            }
        }

        // 第二阶段：内容哈希匹配
        var remainingPaths = allPaths.Where(p => !processedPaths.Contains(p)).ToList();
        if (remainingPaths.Count > 1)
        {
            var extGroups = remainingPaths.GroupBy(p => Path.GetExtension(p).ToLower());
            foreach (var group in extGroups)
            {
                var paths = group.ToList();
                if (paths.Count < 2) continue;

                var hashToPaths = new Dictionary<string, List<string>>();
                foreach (var path in paths)
                {
                    string hash = GetFileMD5(path);
                    if (string.IsNullOrEmpty(hash)) continue;
                    if (!hashToPaths.ContainsKey(hash))
                        hashToPaths[hash] = new List<string>();
                    hashToPaths[hash].Add(path);
                }

                foreach (var hashGroup in hashToPaths)
                {
                    if (hashGroup.Value.Count < 2) continue;
                    var sorted = hashGroup.Value.OrderBy(p => Path.GetFileNameWithoutExtension(p).Length).ToList();
                    string keepPath = sorted[0];
                    var duplicates = sorted.Skip(1).ToList();
                    if (duplicates.Count > 0)
                        mergeGroups[keepPath] = duplicates;
                }
            }
        }

        if (mergeGroups.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "未找到任何可合并的重复资产。", "确定");
            return;
        }

        var sb = new StringBuilder("检测到以下重复组，将自动合并：\n\n");
        foreach (var group in mergeGroups)
        {
            sb.AppendLine($"保留: {Path.GetFileName(group.Key)}");
            foreach (var dup in group.Value)
                sb.AppendLine($"  删除: {Path.GetFileName(dup)}");
            sb.AppendLine();
        }
        sb.Append("此操作将替换所有引用并删除重复文件，不可撤销！");

        if (!EditorUtility.DisplayDialog("确认合并", sb.ToString(), "全部合并", "取消"))
            return;

        EnsureCacheReady();

        foreach (var group in mergeGroups)
        {
            string keepPath = group.Key;
            List<string> deletePaths = group.Value;

            string keepGuid = AssetDatabase.AssetPathToGUID(keepPath);
            var deleteGuids = deletePaths.Select(AssetDatabase.AssetPathToGUID).ToList();
            ReplaceGUIDReferences(deleteGuids, keepGuid);

            foreach (string del in deletePaths)
                AssetDatabase.DeleteAsset(del);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", "文件夹内的重复资产已合并完成。", "确定");
    }

    private static string GetFileMD5(string filePath)
    {
        try
        {
            byte[] data = File.ReadAllBytes(filePath);
            using (var md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(data);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
        catch
        {
            return null;
        }
    }

    // -------------------- 菜单：Prefab中Sprite引用type改为3（简化版） --------------------
    [MenuItem("Assets/将Sprite引用类型改为3", true)]
    static bool ValidateFixSpriteRefType()
    {
        if (Selection.objects.Length != 1) return false;
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        return !string.IsNullOrEmpty(path) && Path.GetExtension(path).ToLower() == ".prefab";
    }

    [MenuItem("Assets/将Sprite引用类型改为3")]
    static void FixSpriteRefType()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            EditorUtility.DisplayDialog("错误", "未找到有效的预制体文件。", "确定");
            return;
        }

        if (!EditorUtility.DisplayDialog("确认修改",
            $"将把预制体 \"{Path.GetFileName(path)}\" 中所有 m_Sprite 引用的 type: 2 改为 type: 3。\n\n此操作会影响资源加载方式，建议先备份。",
            "继续", "取消"))
            return;

        string[] lines = File.ReadAllLines(path);
        bool modified = false;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith("m_Sprite:"))
            {
                if (lines[i].Contains("type: 2"))
                {
                    lines[i] = lines[i].Replace("type: 2", "type: 3");
                    modified = true;
                }
            }
        }

        if (!modified)
        {
            EditorUtility.DisplayDialog("提示", "未找到任何 m_Sprite 行中包含 type: 2 的引用，无需修改。", "确定");
            return;
        }

        File.WriteAllLines(path, lines);
        AssetDatabase.ImportAsset(path);
        EditorUtility.DisplayDialog("完成", "预制体中的 Sprite 引用类型已修改为 type: 3。", "确定");
    }

    // -------------------- 替换 GUID 窗口 --------------------
    internal class ReplaceGUIDWindow : EditorWindow
    {
        private string assetPath, currentGuid, newGuidInput = "";
        private Object assetObject;

        public static void OpenWindow(string path, string guid)
        {
            var window = GetWindow<ReplaceGUIDWindow>(true, "替换资产 GUID");
            window.assetPath = path;
            window.currentGuid = guid;
            window.newGuidInput = "";
            window.assetObject = AssetDatabase.LoadMainAssetAtPath(path);
            window.minSize = new Vector2(450, 220);
            window.maxSize = new Vector2(600, 260);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("资产路径:");
            EditorGUILayout.ObjectField(assetObject, typeof(Object), false);

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("当前 GUID:");
            EditorGUILayout.SelectableLabel(currentGuid, EditorStyles.textField, GUILayout.Height(18));

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("新 GUID:");
            newGuidInput = EditorGUILayout.TextField(newGuidInput);

            EditorGUILayout.Space(15);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("替换", GUILayout.Width(100))) Replace();
            if (GUILayout.Button("取消", GUILayout.Width(100))) Close();
            EditorGUILayout.EndHorizontal();
        }

        private void Replace()
        {
            string newGuid = newGuidInput.Trim();
            if (!Regex.IsMatch(newGuid, @"^[a-fA-F0-9]{32}$"))
            {
                EditorUtility.DisplayDialog("格式错误", "GUID 必须为 32 位十六进制字符串。", "确定");
                return;
            }
            if (newGuid == currentGuid)
            {
                EditorUtility.DisplayDialog("无变化", "新 GUID 与当前 GUID 相同。", "确定");
                return;
            }
            string existPath = AssetDatabase.GUIDToAssetPath(newGuid);
            if (!string.IsNullOrEmpty(existPath) && existPath != assetPath)
            {
                EditorUtility.DisplayDialog("冲突", $"GUID 已被占用:\n{existPath}", "确定");
                return;
            }

            if (!EditorUtility.DisplayDialog("确认替换", $"将 GUID 从 {currentGuid} 改为 {newGuid}？", "确认", "取消"))
                return;

            _isProcessing = true;
            try
            {
                string metaPath = assetPath + ".meta";
                if (File.Exists(metaPath))
                {
                    string content = File.ReadAllText(metaPath);
                    content = Regex.Replace(content, @"^guid:\s*\S+", "guid: " + newGuid, RegexOptions.Multiline);
                    File.WriteAllText(metaPath, content);
                }
                EnsureCacheReady();
                ReplaceGUIDReferences(new List<string> { currentGuid }, newGuid);
                AssetDatabase.ImportAsset(assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("完成", "资产 GUID 替换完成。", "确定");
            }
            finally { _isProcessing = false; }
            Close();
        }
    }

    // -------------------- 查看资产引用窗口 --------------------
    internal class ReferenceViewerWindow : EditorWindow
    {
        private string assetPath;
        private List<Object> referencedObjects = new List<Object>();
        private Vector2 scroll;

        static readonly HashSet<string> ScriptExts = new HashSet<string> { ".cs", ".js", ".boo" };

        public static void OpenWindow(string path)
        {
            var window = GetWindow<ReferenceViewerWindow>(true, "资产引用列表");
            window.assetPath = path;
            window.LoadRefs();
            window.minSize = new Vector2(450, 300);
            window.Show();
        }

        void LoadRefs()
        {
            referencedObjects.Clear();
            if (string.IsNullOrEmpty(assetPath)) return;

            foreach (var dep in AssetDatabase.GetDependencies(assetPath, false))
            {
                if (dep == assetPath) continue;
                string ext = Path.GetExtension(dep)?.ToLower();
                if (ext != null && ScriptExts.Contains(ext)) continue;

                Object obj = AssetDatabase.LoadMainAssetAtPath(dep);
                if (obj != null)
                    referencedObjects.Add(obj);
            }
        }

        void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("资产:", assetPath);
            EditorGUILayout.LabelField("引用数量:", referencedObjects.Count.ToString());
            EditorGUILayout.Space(5);

            if (referencedObjects.Count == 0)
            {
                EditorGUILayout.HelpBox("没有引用任何非脚本资产。", MessageType.Info);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var obj in referencedObjects)
            {
                EditorGUILayout.ObjectField(obj, typeof(Object), false);
            }
            EditorGUILayout.EndScrollView();
        }
    }

    // -------------------- 查看资产被引用窗口 --------------------
    internal class DependentsViewerWindow : EditorWindow
    {
        private string assetPath;
        private List<Object> dependentObjects = new List<Object>();
        private Vector2 scroll;
        static readonly HashSet<string> ScriptExts = new HashSet<string> { ".cs", ".js", ".boo" };

        public static void OpenWindow(string path)
        {
            var window = GetWindow<DependentsViewerWindow>(true, "资产被引用列表");
            window.assetPath = path;
            window.LoadDependents();
            window.minSize = new Vector2(450, 300);
            window.Show();
        }

        void LoadDependents()
        {
            dependentObjects.Clear();
            if (string.IsNullOrEmpty(assetPath)) return;

            string targetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            EnsureCacheReady();

            if (_guidRefMap != null && _guidRefMap.TryGetValue(targetGuid, out var referencerPaths))
            {
                foreach (string refPath in referencerPaths)
                {
                    string ext = Path.GetExtension(refPath)?.ToLower();
                    if (!string.IsNullOrEmpty(ext) && ScriptExts.Contains(ext))
                        continue;

                    Object obj = AssetDatabase.LoadMainAssetAtPath(refPath);
                    if (obj != null)
                        dependentObjects.Add(obj);
                }
            }
        }

        void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("资产:", assetPath);
            EditorGUILayout.LabelField("被引用数量:", dependentObjects.Count.ToString());
            EditorGUILayout.Space(5);

            if (dependentObjects.Count == 0)
            {
                EditorGUILayout.HelpBox("该资产未被任何非脚本资产引用。", MessageType.Info);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var obj in dependentObjects)
            {
                EditorGUILayout.ObjectField(obj, typeof(Object), false);
            }
            EditorGUILayout.EndScrollView();
        }
    }

    // -------------------- 通过 GUID 查找资产窗口 --------------------
    internal class FindAssetByGuidWindow : EditorWindow
    {
        private string searchGuid = "";
        private Object foundAsset;

        public static void OpenWindow()
        {
            var window = GetWindow<FindAssetByGuidWindow>(true, "通过 GUID 查找资产");
            window.minSize = new Vector2(400, 150);
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.Space(10);
            searchGuid = EditorGUILayout.TextField("输入 GUID:", searchGuid);

            if (GUILayout.Button("查找", GUILayout.Width(100)))
            {
                FindAsset();
            }

            EditorGUILayout.Space(10);
            if (foundAsset != null)
            {
                EditorGUILayout.ObjectField("找到的资产:", foundAsset, typeof(Object), false);
            }
            else if (!string.IsNullOrEmpty(searchGuid))
            {
                EditorGUILayout.HelpBox("未找到对应的资产。", MessageType.Warning);
            }
        }

        void FindAsset()
        {
            foundAsset = null;
            string guid = searchGuid.Trim();
            if (guid.Length != 32 || !Regex.IsMatch(guid, @"^[a-fA-F0-9]{32}$"))
            {
                EditorUtility.DisplayDialog("错误", "GUID 格式无效。", "确定");
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                foundAsset = null;
                Repaint();
                return;
            }

            foundAsset = AssetDatabase.LoadMainAssetAtPath(path);
            if (foundAsset)
            {
                EditorGUIUtility.PingObject(foundAsset);
                Selection.activeObject = foundAsset;
            }
            Repaint();
        }
    }

    // ---- 共用引用缓存系统 ----
    private static Dictionary<string, HashSet<string>> _guidRefMap;
    private static readonly object _lock = new object();

    internal static void EnsureCacheReady()
    {
        lock (_lock)
        {
            if (_guidRefMap != null) return;
            var prevMode = EditorSettings.serializationMode;
            bool switched = false;
            if (prevMode != SerializationMode.ForceText)
            {
                EditorSettings.serializationMode = SerializationMode.ForceText;
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                switched = true;
            }
            try { BuildGUIDReferenceMap(); }
            finally
            {
                if (switched)
                {
                    EditorSettings.serializationMode = prevMode;
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
        }
    }

    private static void BuildGUIDReferenceMap()
    {
        _guidRefMap = new Dictionary<string, HashSet<string>>();
        var files = AssetDatabase.GetAllAssetPaths()
            .Where(p => p.StartsWith("Assets/"))
            .Where(p => AssetExtensions.Contains(Path.GetExtension(p)?.ToLower()))
            .ToArray();
        var regex = new Regex(@"[a-fA-F0-9]{32}", RegexOptions.Compiled);
        for (int i = 0; i < files.Length; i++)
        {
            string path = files[i];
            if (Directory.Exists(path)) continue;
            if (i % 100 == 0)
            {
                if (EditorUtility.DisplayCancelableProgressBar("建立引用缓存", Path.GetFileName(path), (float)i / files.Length))
                {
                    EditorUtility.ClearProgressBar();
                    _guidRefMap = null;
                    return;
                }
            }
            try
            {
                string content = File.ReadAllText(path);
                foreach (Match m in regex.Matches(content))
                {
                    string g = m.Value;
                    if (!_guidRefMap.ContainsKey(g)) _guidRefMap[g] = new HashSet<string>();
                    _guidRefMap[g].Add(path);
                }
            }
            catch { }
        }
        EditorUtility.ClearProgressBar();
    }

    public static void InvalidateCache()
    {
        if (_isProcessing) return;
        lock (_lock) { _guidRefMap = null; }
    }

    internal static void ReplaceGUIDReferences(List<string> oldGuids, string newGuid)
    {
        EnsureCacheReady();
        if (_guidRefMap == null) { Debug.LogError("引用缓存构建失败。"); return; }

        var filesToModify = new HashSet<string>();
        lock (_lock)
        {
            foreach (var g in oldGuids)
                if (_guidRefMap.TryGetValue(g, out var fset))
                    foreach (var f in fset) filesToModify.Add(f);
        }

        var prevMode = EditorSettings.serializationMode;
        bool switched = false;
        if (prevMode != SerializationMode.ForceText)
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            switched = true;
        }

        try
        {
            int total = filesToModify.Count;
            int index = 0;
            foreach (var fp in filesToModify)
            {
                if (!File.Exists(fp)) continue;
                string content = File.ReadAllText(fp);
                bool changed = false;
                foreach (var g in oldGuids)
                {
                    if (content.Contains(g))
                    {
                        content = content.Replace(g, newGuid);
                        changed = true;
                    }
                }
                if (changed)
                {
                    File.WriteAllText(fp, content);
                    AssetDatabase.ImportAsset(fp);
                    lock (_lock)
                    {
                        foreach (var g in oldGuids)
                            if (_guidRefMap.TryGetValue(g, out var refs)) refs.Remove(fp);
                        if (!_guidRefMap.TryGetValue(newGuid, out var newRefs))
                        {
                            newRefs = new HashSet<string>();
                            _guidRefMap[newGuid] = newRefs;
                        }
                        newRefs.Add(fp);
                    }
                }
                if (++index % 20 == 0)
                    EditorUtility.DisplayProgressBar("替换引用", Path.GetFileName(fp), (float)index / total);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            if (switched)
            {
                EditorSettings.serializationMode = prevMode;
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
}

// 全局监听
internal class DuplicateAssetCacheInvalidator : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        AssetReferenceTool.InvalidateCache();
    }
}