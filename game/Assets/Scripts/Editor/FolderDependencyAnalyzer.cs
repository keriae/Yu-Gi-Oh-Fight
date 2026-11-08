using Codice.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class FolderDependencyAnalyzer : EditorWindow
{
    private string targetFolderPath;
    private Dictionary<string, List<string>> externalDependencies = new Dictionary<string, List<string>>();
    private Vector2 scrollPosition;
    private bool showInternalDependencies = false;

    [MenuItem("Assets/Analyze Folder External Dependencies", false, 20)]
    static void AnalyzeFolderDependencies()
    {
        // 获取选中的文件夹
        string folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog("错误", "请选择一个有效的文件夹", "确定");
            return;
        }

        // 创建并显示分析窗口
        var window = CreateInstance<FolderDependencyAnalyzer>();
        window.targetFolderPath = folderPath;
        window.titleContent = new GUIContent($"依赖分析: {Path.GetFileName(folderPath)}");
        window.ShowUtility();
        window.AnalyzeDependencies();
    }

    [MenuItem("Assets/Analyze Folder External Dependencies", true)]
    static bool ValidateAnalyzeFolderDependencies()
    {
        return Selection.activeObject != null && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
    }

    void AnalyzeDependencies()
    {
        externalDependencies.Clear();
        EditorUtility.DisplayProgressBar("分析依赖", "收集文件夹内资源...", 0);

        try
        {
            // 获取文件夹内所有资源[3,6](@ref)
            string[] allAssetPaths = AssetDatabase.FindAssets("", new[] { targetFolderPath })
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(path => !path.EndsWith(".meta"))
                .ToArray();

            // 分析每个资源的依赖关系[6,8](@ref)
            for (int i = 0; i < allAssetPaths.Length; i++)
            {
                string assetPath = allAssetPaths[i];
                EditorUtility.DisplayProgressBar("分析依赖", $"分析: {Path.GetFileName(assetPath)}", (float)i / allAssetPaths.Length);

                // 获取该资源的所有依赖[3,7](@ref)
                string[] dependencies = AssetDatabase.GetDependencies(assetPath, true);

                // 过滤出不在当前文件夹内的依赖
                List<string> externalDeps = dependencies
                    .Where(depPath =>
                        !depPath.StartsWith(targetFolderPath) && // 不在当前文件夹
                        !depPath.Equals(assetPath) && // 排除自身
                        !depPath.EndsWith(".cs") && // 排除脚本文件
                        !depPath.EndsWith(".dll") && // 排除DLL文件
                        !depPath.EndsWith(".shader")) // 排除shader文件
                    .ToList();

                if (externalDeps.Count > 0)
                {
                    externalDependencies[assetPath] = externalDeps;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"分析文件夹: {targetFolderPath}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"发现 {externalDependencies.Count} 个资源有外部依赖");
        EditorGUILayout.Space();

        if (GUILayout.Button("重新分析"))
        {
            AnalyzeDependencies();
        }

        showInternalDependencies = EditorGUILayout.Toggle("显示内部依赖", showInternalDependencies);
        EditorGUILayout.Space();

        if (externalDependencies.Count == 0)
        {
            EditorGUILayout.HelpBox("未发现外部依赖，或者该文件夹内没有资源。", MessageType.Info);
            return;
        }

        // 显示外部依赖列表
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (var item in externalDependencies)
        {
            string assetPath = item.Key;
            List<string> deps = item.Value;

            EditorGUILayout.BeginVertical("box");

            // 显示资源名称和路径
            EditorGUILayout.BeginHorizontal();
            GUIStyle boldStyle = new GUIStyle(EditorStyles.foldout);
            boldStyle.fontStyle = FontStyle.Bold;

            bool isExpanded = EditorGUILayout.Foldout(false, $"{Path.GetFileName(assetPath)} ({deps.Count} 个外部依赖)", true, boldStyle);

            if (GUILayout.Button("选择", GUILayout.Width(50)))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"路径: {assetPath}", EditorStyles.miniLabel);

            // 显示外部依赖详情
            EditorGUI.indentLevel++;
            foreach (string depPath in deps)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Path.GetFileName(depPath), GUILayout.Width(200));
                EditorGUILayout.LabelField(depPath, EditorStyles.miniLabel);

                if (GUILayout.Button("定位", GUILayout.Width(40)))
                {
                    Object depObject = AssetDatabase.LoadAssetAtPath<Object>(depPath);
                    if (depObject != null)
                    {
                        Selection.activeObject = depObject;
                        EditorGUIUtility.PingObject(depObject);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();
    }

    // 添加工具窗口菜单项
    [MenuItem("Tools/资源分析/文件夹依赖分析器")]
    static void ShowWindow()
    {
        var window = CreateInstance<FolderDependencyAnalyzer>();
        window.titleContent = new GUIContent("文件夹依赖分析");
        window.Show();
    }
}