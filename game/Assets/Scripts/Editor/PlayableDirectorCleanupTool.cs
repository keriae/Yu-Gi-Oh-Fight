using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

public class PlayableDirectorCleaner : EditorWindow
{
    private static GameObject targetPrefab;
    private static string originalPrefabPath;

    [MenuItem("Assets/清理PlayableDirector引用", false, 105)]
    private static void CleanPlayableDirectorReferences()
    {
        // 获取选中的Prefab
        targetPrefab = Selection.activeGameObject;
        if (targetPrefab == null)
        {
            EditorUtility.DisplayDialog("错误", "请选择一个Prefab资产", "确定");
            return;
        }

        originalPrefabPath = AssetDatabase.GetAssetPath(targetPrefab);
        if (string.IsNullOrEmpty(originalPrefabPath))
        {
            EditorUtility.DisplayDialog("错误", "选中的对象不是Prefab资产", "确定");
            return;
        }

        // 显示保存选项窗口
        ShowSaveOptionWindow();
    }

    [MenuItem("Assets/清理PlayableDirector引用", true)]
    private static bool Validate()
    {
        return Selection.activeObject != null &&
               !AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
    }

    private static void ShowSaveOptionWindow()
    {
        if (EditorUtility.DisplayDialog("清理选项",
            "请选择保存方式", "写入源文件", "新建文件保存"))
        {
            // 写入源文件
            CleanAndSavePrefab(originalPrefabPath);
        }
        else
        {
            // 新建文件保存
            string newPath = GetNewPrefabPath(originalPrefabPath);
            CleanAndSavePrefab(newPath);
        }
    }

    private static string GetNewPrefabPath(string path)
    {
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);

        string newFileName = fileName + "_Cleaned";
        string newPath = Path.Combine(directory, newFileName + extension);

        // 确保路径唯一
        return AssetDatabase.GenerateUniqueAssetPath(newPath);
    }

    private static void CleanAndSavePrefab(string savePath)
    {
        Debug.Log($"[PlayableDirector清理] 开始清理: {originalPrefabPath}");

        // 加载Prefab实例进行编辑
        GameObject prefabInstance = PrefabUtility.LoadPrefabContents(originalPrefabPath);

        bool cleaned = false;
        int directorsCleaned = 0;
        PlayableDirector[] directors = prefabInstance.GetComponentsInChildren<PlayableDirector>(true);

        Debug.Log($"[PlayableDirector清理] 找到 {directors.Length} 个PlayableDirector组件");

        foreach (PlayableDirector director in directors)
        {
            if (CleanDirectorReferences(director))
            {
                cleaned = true;
                directorsCleaned++;
                Debug.Log($"[PlayableDirector清理] 已清理Director: {director.gameObject.name}", director);
            }
        }

        if (cleaned)
        {
            // 保存Prefab
            PrefabUtility.SaveAsPrefabAsset(prefabInstance, savePath, out bool success);

            if (success)
            {
                Debug.Log($"[PlayableDirector清理] 成功清理并保存到: {savePath}\n清理了 {directorsCleaned} 个Director的无效引用");
                EditorUtility.DisplayDialog("成功", $"PlayableDirector引用已清理并保存到: {savePath}\n清理了 {directorsCleaned} 个Director的无效引用", "确定");
            }
            else
            {
                Debug.LogError("[PlayableDirector清理] 保存Prefab失败");
                EditorUtility.DisplayDialog("错误", "保存Prefab失败", "确定");
            }
        }
        else
        {
            Debug.Log("[PlayableDirector清理] 未找到需要清理的PlayableDirector引用");
            EditorUtility.DisplayDialog("信息", "未找到需要清理的PlayableDirector引用", "确定");
        }

        // 卸载Prefab实例
        PrefabUtility.UnloadPrefabContents(prefabInstance);
    }

    private static bool CleanDirectorReferences(PlayableDirector director)
    {
        bool modified = false;
        SerializedObject so = new SerializedObject(director);

        // 获取TimelineAsset引用
        PlayableAsset timelineAsset = director.playableAsset;
        SerializedProperty playableAssetProp = so.FindProperty("m_PlayableAsset");

        if (timelineAsset == null)
        {
            Debug.LogWarning($"[PlayableDirector清理] Director {director.gameObject.name} 没有设置TimelineAsset，将清空所有绑定", director);

            // 清理所有绑定和暴露引用
            SerializedProperty sceneBindingsProp2 = so.FindProperty("m_SceneBindings");
            if (sceneBindingsProp2 != null && sceneBindingsProp2.isArray && sceneBindingsProp2.arraySize > 0)
            {
                Debug.Log($"[PlayableDirector清理] 清空 {sceneBindingsProp2.arraySize} 个场景绑定");
                sceneBindingsProp2.ClearArray();
                modified = true;
            }

            SerializedProperty exposedReferencesProp2 = so.FindProperty("m_ExposedReferences");
            if (exposedReferencesProp2 != null)
            {
                SerializedProperty referencesProp = exposedReferencesProp2.FindPropertyRelative("m_References");
                if (referencesProp != null && referencesProp.isArray && referencesProp.arraySize > 0)
                {
                    Debug.Log($"[PlayableDirector清理] 清空 {referencesProp.arraySize} 个暴露引用");
                    referencesProp.ClearArray();
                    modified = true;
                }
            }

            if (modified)
            {
                so.ApplyModifiedProperties();
            }

            return modified;
        }

        Debug.Log($"[PlayableDirector清理] 处理Director: {director.gameObject.name}, Timeline: {timelineAsset.name}");

        // 获取当前TimelineAsset的所有轨道绑定信息
        Dictionary<Object, bool> validBindings = new Dictionary<Object, bool>();
        SerializedObject timelineSo = new SerializedObject(timelineAsset);
        SerializedProperty tracksProp = timelineSo.FindProperty("m_Tracks");

        if (tracksProp != null && tracksProp.isArray)
        {
            for (int i = 0; i < tracksProp.arraySize; i++)
            {
                SerializedProperty trackProp = tracksProp.GetArrayElementAtIndex(i);
                Object trackObject = trackProp.objectReferenceValue;
                if (trackObject != null)
                {
                    validBindings[trackObject] = true;
                }
            }
        }

        Debug.Log($"[PlayableDirector清理] Timeline {timelineAsset.name} 包含 {validBindings.Count} 个有效轨道");

        // 清理m_SceneBindings
        SerializedProperty sceneBindingsProp = so.FindProperty("m_SceneBindings");
        if (sceneBindingsProp != null && sceneBindingsProp.isArray)
        {
            int initialCount = sceneBindingsProp.arraySize;
            for (int i = sceneBindingsProp.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty bindingProp = sceneBindingsProp.GetArrayElementAtIndex(i);
                SerializedProperty keyProp = bindingProp.FindPropertyRelative("key");
                SerializedProperty valueProp = bindingProp.FindPropertyRelative("value");

                Object keyObject = keyProp.objectReferenceValue;

                if (keyObject == null)
                {
                    Debug.Log($"[PlayableDirector清理: {i}");
                    sceneBindingsProp.DeleteArrayElementAtIndex(i);
                    modified = true;
                }
            }

            if (modified)
            {
                Debug.Log($"[PlayableDirector清理] 场景绑定清理: {initialCount} → {sceneBindingsProp.arraySize}");
            }
        }

        // 清理m_ExposedReferences
        SerializedProperty exposedReferencesProp = so.FindProperty("m_ExposedReferences");
        if (exposedReferencesProp != null)
        {
            SerializedProperty referencesProp = exposedReferencesProp.FindPropertyRelative("m_References");
            if (referencesProp != null && referencesProp.isArray)
            {
                int initialCount = referencesProp.arraySize;
                int removedCount = 0;

                for (int i = referencesProp.arraySize - 1; i >= 0; i--)
                {
                    SerializedProperty referenceProp = referencesProp.GetArrayElementAtIndex(i);

                    // 检查两种序列化格式：
                    // 1. - fileID: {fileID: 0}
                    // 2. - 903425b99fe89ab4ea3d8f4394881b4a: {fileID: 0}

                    bool shouldRemove = false;

                    // 检查第一种格式：直接检查fileID属性
                    SerializedProperty fileIDProp = referenceProp.FindPropertyRelative("fileID");
                    if (fileIDProp != null)
                    {
                        if (fileIDProp.intValue == 0)
                        {
                            shouldRemove = true;
                            removedCount++;
                        }
                    }
                    else
                    {
                        // 检查第二种格式：遍历所有子属性
                        var iterator = referenceProp.Copy();
                        var endProperty = referenceProp.GetEndProperty();

                        bool hasNonZeroValue = false;
                        while (iterator.NextVisible(true) && !SerializedProperty.EqualContents(iterator, endProperty))
                        {
                            if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                            {
                                if (iterator.objectReferenceValue != null)
                                {
                                    hasNonZeroValue = true;
                                    break;
                                }
                            }
                            else if (iterator.propertyType == SerializedPropertyType.Integer)
                            {
                                if (iterator.intValue != 0)
                                {
                                    hasNonZeroValue = true;
                                    break;
                                }
                            }
                        }

                        if (!hasNonZeroValue)
                        {
                            shouldRemove = true;
                            removedCount++;
                        }
                    }

                    if (shouldRemove)
                    {
                        referencesProp.DeleteArrayElementAtIndex(i);
                        modified = true;
                    }
                }

                if (removedCount > 0)
                {
                    Debug.Log($"[PlayableDirector清理] 移除 {removedCount} 个空暴露引用");
                }
            }
        }

        if (modified)
        {
            so.ApplyModifiedProperties();
            Debug.Log($"[PlayableDirector清理] Director {director.gameObject.name} 引用清理完成", director);
        }
        else
        {
            Debug.Log($"[PlayableDirector清理] Director {director.gameObject.name} 无需清理", director);
        }

        return modified;
    }
}