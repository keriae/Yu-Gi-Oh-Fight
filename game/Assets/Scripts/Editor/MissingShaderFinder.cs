#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class MissingShaderFinder : EditorWindow
{
    private List<Material> _materialsWithMissingShader = new List<Material>();

    [MenuItem("Tools/检查丢失Shader的材质")]
    public static void ShowWindow()
    {
        GetWindow<MissingShaderFinder>("丢失Shader的材质列表");
    }

    void OnGUI()
    {
        if (GUILayout.Button("扫描项目", GUILayout.Height(40)))
        {
            FindMaterialsWithMissingShader();
        }

        GUILayout.Label($"发现 {_materialsWithMissingShader.Count} 个材质丢失Shader:");
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (Material mat in _materialsWithMissingShader)
        {
            EditorGUILayout.BeginHorizontal();
            // 显示材质名称和路径
            EditorGUILayout.ObjectField(mat, typeof(Material), false);

            // 定位按钮
            if (GUILayout.Button("定位", GUILayout.Width(60)))
            {
                Selection.activeObject = mat;
                EditorGUIUtility.PingObject(mat);
            }

            // 修复按钮
            if (GUILayout.Button("修复(Standard)", GUILayout.Width(80)))
            {
                FixShader(mat, Shader.Find("Standard"));
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    private Vector2 scrollPos;

    private void FindMaterialsWithMissingShader()
    {
        _materialsWithMissingShader.Clear();
        string[] guids = AssetDatabase.FindAssets("t:Material");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat.shader == null || mat.shader.name.Contains("Error"))
            {
                _materialsWithMissingShader.Add(mat);
            }
        }
    }

    private void FixShader(Material mat, Shader replacementShader)
    {
        if (mat != null && replacementShader != null)
        {
            Undo.RecordObject(mat, "Fix Missing Shader");
            mat.shader = replacementShader;
            EditorUtility.SetDirty(mat);
            Debug.Log($"已修复材质: {mat.name}", mat);
        }
    }
}
#endif