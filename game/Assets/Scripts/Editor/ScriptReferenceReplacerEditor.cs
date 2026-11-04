using System.IO;
using UnityEditor;
using UnityEngine;

public class ScriptReferenceReplacerEditor : EditorWindow
{
    private ScriptReplacementConfig replacementConfig;
    private Vector2 scrollPosition;

    [MenuItem("Assets/替换脚本引用", false, 102)]
    private static void ReplaceScriptReferences()
    {
        // 首先显示确认弹窗
        bool userConfirmed = EditorUtility.DisplayDialog(
            "危险操作确认", // 标题
            $"你确定要进行【替换脚本引用】' 吗？此操作不可逆！\n\n建议提前备份项目。", // 消息
            "继续", // 确定按钮
            "取消" // 取消按钮
        );

        // 如果用户点击了“取消”，则终止操作
        if (!userConfirmed)
        {
            Debug.Log("用户取消了操作。");
            return;
        }

        // 尝试查找现有的配置
        string[] configGUIDs = AssetDatabase.FindAssets("t:ScriptReplacementConfig");
        ScriptReplacementConfig config = null;
        if (configGUIDs.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(configGUIDs[0]);
            config = AssetDatabase.LoadAssetAtPath<ScriptReplacementConfig>(path);
        }

        if (config == null)
        {
            Debug.LogError("Please create a ScriptReplacementConfig first and assign the replacement pairs.");
            return;
        }

        // 处理资产
        var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        ProcessAsset(assetPath, config);
    }

    [MenuItem("Assets/替换脚本引用", true)]
    private static bool ValidateReplaceStringsInAsset()
    {
        return Selection.activeObject != null &&
               !AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
    }

    private static void ProcessAsset(string assetPath, ScriptReplacementConfig config)
    {
        // 读取资产的文本内容
        string assetContent = File.ReadAllText(assetPath);
        bool modified = false;

        // 遍历所有需要替换的配置
        foreach (var pair in config.replacementPairs)
        {
            // 构造旧的m_Script行内容
            string oldScriptLine = $"m_Script: {{fileID: {pair.oldFileId}, guid: {pair.oldGuid}, type: 3}}";
            // 构造新的m_Script行内容
            string newScriptLine = $"m_Script: {{fileID: {pair.newFileId}, guid: {pair.newGuid}, type: 3}}";

            // 检查并替换
            if (assetContent.Contains(oldScriptLine))
            {
                assetContent = assetContent.Replace(oldScriptLine, newScriptLine);
                modified = true;
                Debug.Log($"Replaced script reference in {assetPath}: {oldScriptLine} -> {newScriptLine}");
            }
        }

        // 如果内容被修改，写回文件并重新导入资产
        if (modified)
        {
            File.WriteAllText(assetPath, assetContent);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"Successfully processed and reimported: {assetPath}");
        }
        else
        {
            Debug.Log($"No matching script references found in: {assetPath}");
        }
    }

    // 提供一个编辑器窗口以便于操作和查看配置（可选）
    [MenuItem("Tools/Script Reference Replacer")]
    public static void ShowWindow()
    {
        GetWindow<ScriptReferenceReplacerEditor>("Script Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Script Reference Replacer Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 允许拖拽赋值
        replacementConfig = (ScriptReplacementConfig)EditorGUILayout.ObjectField("Replacement Config", replacementConfig, typeof(ScriptReplacementConfig), false);

        if (replacementConfig == null)
        {
            EditorGUILayout.HelpBox("Please assign a ScriptReplacementConfig asset.", MessageType.Warning);
            if (GUILayout.Button("Create New Configuration"))
            {
                CreateNewConfig();
            }
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        SerializedObject serializedConfig = new SerializedObject(replacementConfig);
        SerializedProperty pairsProperty = serializedConfig.FindProperty("replacementPairs");
        EditorGUILayout.PropertyField(pairsProperty, true);
        serializedConfig.ApplyModifiedProperties();
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        if (GUILayout.Button("Process Selected Asset"))
        {
            if (Selection.activeObject != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                ProcessAsset(assetPath, replacementConfig);
            }
            else
            {
                Debug.LogWarning("Please select an asset first.");
            }
        }
    }

    private void CreateNewConfig()
    {
        string path = EditorUtility.SaveFilePanelInProject("Create Replacement Config", "ScriptReplacementConfig", "asset", "Please enter a file name for the configuration");
        if (!string.IsNullOrEmpty(path))
        {
            ScriptReplacementConfig newConfig = CreateInstance<ScriptReplacementConfig>();
            AssetDatabase.CreateAsset(newConfig, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            replacementConfig = newConfig;
        }
    }
}