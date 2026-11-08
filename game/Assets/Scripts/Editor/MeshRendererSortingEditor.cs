using UnityEngine;
using UnityEditor;
using System.Reflection;

[CustomEditor(typeof(MeshRenderer))]
public class MeshRendererSortingEditor : Editor
{
    private SerializedProperty sortingLayerID;
    private SerializedProperty sortingOrder;
    private static MethodInfo sortingLayerFieldMethod;

    private void OnEnable()
    {
        sortingLayerID = serializedObject.FindProperty("m_SortingLayerID");
        sortingOrder = serializedObject.FindProperty("m_SortingOrder");

        // 缓存方法信息，避免重复查找
        if (sortingLayerFieldMethod == null)
        {
            sortingLayerFieldMethod = typeof(EditorGUILayout).GetMethod(
                "SortingLayerField",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new System.Type[] { typeof(GUIContent), typeof(SerializedProperty), typeof(GUIStyle) },
                null
            );
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sorting", EditorStyles.boldLabel);

        if (sortingLayerFieldMethod != null)
        {
            // 通过反射调用内部方法
            sortingLayerFieldMethod.Invoke(
                null,
                new object[] { new GUIContent("Sorting Layer"), sortingLayerID, EditorStyles.popup }
            );
        }
        else
        {
            // 备用方案：如果反射失败，降级为显示一个整数字段
            EditorGUILayout.PropertyField(sortingLayerID, new GUIContent("Sorting Layer ID"));
        }

        EditorGUILayout.PropertyField(sortingOrder, new GUIContent("Order in Layer"));

        serializedObject.ApplyModifiedProperties();
    }
}