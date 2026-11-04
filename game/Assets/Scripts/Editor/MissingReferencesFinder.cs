using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using UnityEngine.Playables;

public class MissingReferencesFinder : EditorWindow
{
    private Vector2 scrollPosition;
    private List<MissingReference> missingReferences = new List<MissingReference>();
    private bool includeScenes = true;
    private bool includePrefabs = true;
    private bool includeScriptableObjects = true;

    [MenuItem("Tools/缺失引用查找器")]
    public static void ShowWindow()
    {
        GetWindow<MissingReferencesFinder>("缺失引用查找器");
    }

    private void OnGUI()
    {
        GUILayout.Label("缺失引用查找器", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // 配置选项
        includeScenes = EditorGUILayout.Toggle("包含场景", includeScenes);
        includePrefabs = EditorGUILayout.Toggle("包含Prefabs", includePrefabs);
        includeScriptableObjects = EditorGUILayout.Toggle("包含ScriptableObjects", includeScriptableObjects);

        GUILayout.Space(10);

        if (GUILayout.Button("扫描整个项目", GUILayout.Height(30)))
        {
            ScanEntireProject();
        }

        if (GUILayout.Button("扫描选中资源", GUILayout.Height(30)))
        {
            ScanSelectedAssets();
        }

        GUILayout.Space(10);

        // 显示结果
        GUILayout.Label($"找到 {missingReferences.Count} 个缺失引用");

        if (missingReferences.Count > 0)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

            foreach (var missingRef in missingReferences)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"资源: {missingRef.assetPath}", EditorStyles.boldLabel);
                if (GUILayout.Button("定位", GUILayout.Width(60)))
                {
                    Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(missingRef.assetPath);
                    EditorGUIUtility.PingObject(Selection.activeObject);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField($"组件: {missingRef.componentType}");
                EditorGUILayout.LabelField($"属性: {missingRef.propertyName}");
                EditorGUILayout.LabelField($"节点路径: {missingRef.gameObjectPath}");

                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void ScanEntireProject()
    {
        missingReferences.Clear();
        int totalScanned = 0;

        try
        {
            // 扫描Prefabs
            if (includePrefabs)
            {
                string[] prefabPaths = AssetDatabase.GetAllAssetPaths()
                    .Where(path => path.EndsWith(".prefab") 
                    && path.StartsWith("Assets/AssetBundles")
                    && !path.Contains("Mat_002")
                    )
                    .ToArray();

                foreach (string path in prefabPaths)
                {
                    EditorUtility.DisplayProgressBar("扫描Prefabs", path, (float)totalScanned / prefabPaths.Length);
                    ScanAssetAtPath(path);
                    totalScanned++;
                }
            }

            // 扫描场景
            if (includeScenes)
            {
                string[] scenePaths = AssetDatabase.GetAllAssetPaths()
                    .Where(path => path.EndsWith(".unity"))
                    .ToArray();

                foreach (string path in scenePaths)
                {
                    EditorUtility.DisplayProgressBar("扫描场景", path, (float)totalScanned / (totalScanned + scenePaths.Length));
                    ScanAssetAtPath(path);
                    totalScanned++;
                }
            }

            // 扫描ScriptableObjects
            if (includeScriptableObjects)
            {
                string[] soPaths = AssetDatabase.GetAllAssetPaths()
                    .Where(path =>
                        path.EndsWith(".asset") &&
                        !path.Contains("/Resources/") &&
                        AssetDatabase.GetMainAssetTypeAtPath(path)?.IsSubclassOf(typeof(ScriptableObject)) == true)
                    .ToArray();

                foreach (string path in soPaths)
                {
                    EditorUtility.DisplayProgressBar("扫描ScriptableObjects", path, (float)totalScanned / (totalScanned + soPaths.Length));
                    ScanScriptableObjectAtPath(path);
                    totalScanned++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log($"扫描完成！找到 {missingReferences.Count} 个缺失引用");
    }

    private void ScanSelectedAssets()
    {
        missingReferences.Clear();
        var selectedObjects = Selection.objects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("请先选择要扫描的资源");
            return;
        }

        foreach (var obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path))
            {
                ScanAssetAtPath(path);
            }
        }

        Debug.Log($"扫描完成！找到 {missingReferences.Count} 个缺失引用");
    }

    private void ScanAssetAtPath(string assetPath)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (asset == null) return;

        // 检查Prefab或场景中的GameObject
        Component[] components = asset.GetComponentsInChildren<Component>(true);

        foreach (var component in components)
        {
            if (component == null)
            {
                // 缺失的组件（如被删除的脚本）
                missingReferences.Add(new MissingReference
                {
                    assetPath = assetPath,
                    componentType = "Missing Script",
                    propertyName = "Component",
                    //gameObjectPath = GetGameObjectPath(component.gameObject, asset.transform)
                });
                continue;
            }

            CheckComponentForMissingReferences(component, assetPath, asset.transform);
        }
    }

    private void ScanScriptableObjectAtPath(string assetPath)
    {
        ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
        if (so == null) return;

        SerializedObject serializedObject = new SerializedObject(so);
        var property = serializedObject.GetIterator();

        while (property.NextVisible(true))
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                if (property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                {
                    missingReferences.Add(new MissingReference
                    {
                        assetPath = assetPath,
                        componentType = so.GetType().Name,
                        propertyName = property.name,
                        gameObjectPath = "ScriptableObject"
                    });
                }
            }
        }
    }

    private void CheckComponentForMissingReferences(Component component, string assetPath, Transform rootTransform)
    {
        if (component is PlayableDirector)
            return;

        SerializedObject serializedObject = new SerializedObject(component);
        var property = serializedObject.GetIterator();

        // 使用反射获取更准确的对象引用信息[4](@ref)
        var objRefValueMethod = typeof(SerializedProperty).GetProperty("objectReferenceStringValue",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        while (property.NextVisible(true))
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                string objectReferenceStringValue = string.Empty;
                if (objRefValueMethod != null)
                {
                    objectReferenceStringValue = (string)objRefValueMethod.GetGetMethod(true)
                        .Invoke(property, new object[0]);
                }

                // 检查是否为缺失引用[1,4](@ref)
                if (property.objectReferenceValue == null &&
                    (property.objectReferenceInstanceIDValue != 0 ||
                     (objectReferenceStringValue != null && objectReferenceStringValue.StartsWith("Missing"))))
                {
                    missingReferences.Add(new MissingReference
                    {
                        assetPath = assetPath,
                        componentType = component.GetType().Name,
                        propertyName = property.name,
                        gameObjectPath = GetGameObjectPath(component.gameObject, rootTransform)
                    });
                }
            }
        }
    }

    private string GetGameObjectPath(GameObject gameObject, Transform rootTransform)
    {
        List<string> path = new List<string>();
        Transform current = gameObject.transform;

        while (current != null && current != rootTransform)
        {
            path.Insert(0, current.name);
            current = current.parent;
        }

        return string.Join("/", path);
    }

    [MenuItem("Assets/查找缺失引用", false, 20)]
    private static void FindMissingReferencesInSelection()
    {
        var window = GetWindow<MissingReferencesFinder>();
        window.ScanSelectedAssets();
    }

    [MenuItem("CONTEXT/Component/检查缺失引用")]
    private static void FindMissingReferencesInComponent(MenuCommand command)
    {
        Component component = command.context as Component;
        if (component != null)
        {
            var window = GetWindow<MissingReferencesFinder>();
            window.CheckSingleComponent(component);
        }
    }

    private void CheckSingleComponent(Component component)
    {
        missingReferences.Clear();

        string assetPath = AssetDatabase.GetAssetPath(component.gameObject);
        if (string.IsNullOrEmpty(assetPath))
        {
            assetPath = "Current Scene";
        }

        CheckComponentForMissingReferences(component, assetPath,
            component.transform.root ?? component.transform);

        Debug.Log($"检查完成！找到 {missingReferences.Count} 个缺失引用");
    }
}

[System.Serializable]
public class MissingReference
{
    public string assetPath;
    public string componentType;
    public string propertyName;
    public string gameObjectPath;
}