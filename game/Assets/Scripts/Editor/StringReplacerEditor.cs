// StringReplacerEditor.cs
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class StringReplacerEditor : EditorWindow
{
    private StringReplacementConfig config;
    private Vector2 scrollPosition;
    private bool processTextFiles = true;
    private bool processSerializedAssets = true;

    [MenuItem("Assets/替换字符串", false, 103)]
    private static void ReplaceStringsInAsset()
    {
        // 首先显示确认弹窗
        bool userConfirmed = EditorUtility.DisplayDialog(
            "危险操作确认", // 标题
            $"你确定要进行【替换字符串】' 吗？此操作不可逆！\n\n建议提前备份项目。", // 消息
            "继续", // 确定按钮
            "取消" // 取消按钮
        );

        // 如果用户点击了“取消”，则终止操作
        if (!userConfirmed)
        {
            Debug.Log("用户取消了操作。");
            return;
        }

        ProcessSelectedAsset(AssetDatabase.GetAssetPath(Selection.activeObject));
    }

    [MenuItem("Assets/替换字符串", true)]
    private static bool ValidateReplaceStringsInAsset()
    {
        return Selection.activeObject != null &&
               !AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
    }

    [MenuItem("Tools/String Replacer")]
    public static void ShowWindow()
    {
        GetWindow<StringReplacerEditor>("String Replacer");
    }

    private static void ProcessSelectedAsset(string assetPath)
    {
        // 查找配置文件
        StringReplacementConfig config = FindConfig();
        if (config == null || !config.HasRules())
        {
            Debug.LogError("请先创建StringReplacementConfig并配置替换规则");
            return;
        }

        // 根据文件类型选择处理方法
        string extension = Path.GetExtension(assetPath).ToLower();
        bool isTextFile = IsTextFileExtension(extension);
        bool isSerializedAsset = IsSerializedAssetExtension(extension);

        bool processed = false;

        if (isTextFile)
        {
            processed = ProcessTextAsset(assetPath, config);
        }
        else if (isSerializedAsset)
        {
            processed = ProcessSerializedAsset(assetPath, config);
        }
        else
        {
            Debug.LogWarning($"不支持的文件类型: {extension}");
            return;
        }

        if (processed)
        {
            Debug.Log($"成功处理: {assetPath}");
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }
        else
        {
            Debug.Log($"未找到匹配的字符串: {assetPath}");
        }
    }

    private static bool ProcessTextAsset(string assetPath, StringReplacementConfig config)
    {
        string content = File.ReadAllText(assetPath);
        bool modified = false;

        foreach (var rule in config.replacementRules)
        {
            StringComparison comparison = rule.caseSensitive ?
                StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            if (content.IndexOf(rule.findString, comparison) >= 0)
            {
                content = content.Replace(rule.findString, rule.replaceString);
                modified = true;
                Debug.Log($"替换文本: {rule.findString} -> {rule.replaceString}");
            }
        }

        if (modified)
        {
            File.WriteAllText(assetPath, content, Encoding.UTF8);
            return true;
        }

        return false;
    }

    private static bool ProcessSerializedAsset(string assetPath, StringReplacementConfig config)
    {
        // 对于序列化资产，我们需要更谨慎的处理
        // 这里使用Unity的序列化API来安全地修改资产
        bool modified = false;

        SerializedObject serializedObject = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(assetPath)[0]);
        SerializedProperty property = serializedObject.GetIterator();

        while (property.Next(true))
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                string originalValue = property.stringValue;
                string newValue = originalValue;
                
                foreach (var rule in config.replacementRules)
                {
                    if (ShouldReplace(originalValue, rule))
                    {
                        newValue = newValue.Replace(rule.findString, rule.replaceString);
                    }
                }

                if (newValue != originalValue)
                {
                    property.stringValue = newValue;
                    modified = true;
                    Debug.Log($"替换属性 {property.name}: {originalValue} -> {newValue}");
                }
            }
        }

        if (modified)
        {
            serializedObject.ApplyModifiedProperties();
            return true;
        }

        return false;
    }

    private static bool ShouldReplace(string text, StringReplacementConfig.ReplacementRule rule)
    {
        if (rule.caseSensitive)
        {
            return text.Contains(rule.findString);
        }
        else
        {
            return text.IndexOf(rule.findString, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    private static StringReplacementConfig FindConfig()
    {
        string[] guids = AssetDatabase.FindAssets("t:StringReplacementConfig");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<StringReplacementConfig>(path);
        }
        return null;
    }

    private static bool IsTextFileExtension(string extension)
    {
        return true;
        return extension == ".txt" || extension == ".xml" || extension == ".json" ||
               extension == ".csv" || extension == ".html" || extension == ".htm" ||
               extension == ".yaml" || extension == ".yml" || extension == ".cs" ||
               extension == ".js" || extension == ".shader" || extension == ".cginc";
    }

    private static bool IsSerializedAssetExtension(string extension)
    {
        return extension == ".asset" || extension == ".prefab" || extension == ".unity" ||
               extension == ".mat" || extension == ".controller" || extension == ".anim" ||
               extension == ".playable";
    }

    private void OnGUI()
    {
        GUILayout.Label("字符串批量替换工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 配置选择
        config = (StringReplacementConfig)EditorGUILayout.ObjectField("替换配置", config, typeof(StringReplacementConfig), false);

        if (config == null)
        {
            EditorGUILayout.HelpBox("请先创建或指定一个StringReplacementConfig资产", MessageType.Warning);
            if (GUILayout.Button("创建新配置"))
            {
                CreateNewConfig();
            }
            return;
        }

        // 处理选项
        EditorGUILayout.Space();
        GUILayout.Label("处理选项", EditorStyles.boldLabel);
        processTextFiles = EditorGUILayout.Toggle("处理文本文件", processTextFiles);
        processSerializedAssets = EditorGUILayout.Toggle("处理序列化资产", processSerializedAssets);

        // 显示和编辑配置
        EditorGUILayout.Space();
        GUILayout.Label("替换规则配置", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        SerializedObject serializedConfig = new SerializedObject(config);
        SerializedProperty rulesProperty = serializedConfig.FindProperty("replacementRules");
        EditorGUILayout.PropertyField(rulesProperty, true);
        serializedConfig.ApplyModifiedProperties();
        EditorGUILayout.EndScrollView();

        // 操作按钮
        EditorGUILayout.Space();
        if (GUILayout.Button("处理选中资产"))
        {
            if (Selection.activeObject != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                ProcessSelectedAsset(assetPath);
            }
            else
            {
                Debug.LogWarning("请先选择一个资产");
            }
        }

        if (GUILayout.Button("处理文件夹中所有资产"))
        {
            if (Selection.activeObject != null)
            {
                string folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (AssetDatabase.IsValidFolder(folderPath))
                {
                    ProcessAllAssetsInFolder(folderPath);
                }
                else
                {
                    Debug.LogWarning("请选择一个文件夹");
                }
            }
        }
    }

    private void CreateNewConfig()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "创建替换配置",
            "StringReplacementConfig",
            "asset",
            "请输入配置文件名");

        if (!string.IsNullOrEmpty(path))
        {
            StringReplacementConfig newConfig = CreateInstance<StringReplacementConfig>();
            AssetDatabase.CreateAsset(newConfig, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            config = newConfig;
        }
    }

    private void ProcessAllAssetsInFolder(string folderPath)
    {
        StringReplacementConfig config = FindConfig();
        if (config == null || !config.HasRules())
        {
            Debug.LogError("请先配置替换规则");
            return;
        }

        // 获取文件夹中的所有资产
        string[] allFiles = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
        int processedCount = 0;

        foreach (string file in allFiles)
        {
            if (file.EndsWith(".meta")) continue;

            string assetPath = file.Replace(Application.dataPath, "Assets");
            string extension = Path.GetExtension(assetPath).ToLower();

            bool isTextFile = processTextFiles && IsTextFileExtension(extension);
            bool isSerializedAsset = processSerializedAssets && IsSerializedAssetExtension(extension);

            if (isTextFile || isSerializedAsset)
            {
                if (ProcessTextAsset(assetPath, config) || ProcessSerializedAsset(assetPath, config))
                {
                    processedCount++;
                }
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"处理完成！共处理了 {processedCount} 个文件");
    }
}