using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

public class EncodingChecker : EditorWindow
{
    private List<string> nonUTF8Files = new List<string>();
    private Vector2 scrollPos;

    [MenuItem("Tools/编码检测/检查非UTF-8脚本")]
    private static void ShowWindow() => GetWindow<EncodingChecker>("脚本编码检测器");

    private void OnGUI()
    {
        if (GUILayout.Button("扫描.cs文件（排除Packages）", GUILayout.Height(30)))
            ScanScripts();

        if (nonUTF8Files.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label($"发现 {nonUTF8Files.Count} 个非UTF-8文件：");
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var file in nonUTF8Files)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(file);
                if (GUILayout.Button("用VS打开", GUILayout.Width(80)))
                    OpenInVS(file);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("一键转换为UTF-8 without BOM", GUILayout.Height(30)))
                ConvertAllToUTF8();
        }
        else
        {
            GUILayout.Label("✅ 所有脚本均为UTF-8 without BOM格式");
        }
    }

    // 扫描并排除Packages目录
    private void ScanScripts()
    {
        nonUTF8Files.Clear();
        string[] allScripts = AssetDatabase.FindAssets("t:Script");
        int total = allScripts.Length;

        for (int i = 0; i < total; i++)
        {
            string guid = allScripts[i];
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // 排除Packages目录
            if (path.StartsWith("Packages/") || path.EndsWith(".dll"))
                continue;

            if (path.Contains("/Demigiant/"))
                continue;

            if (path.Contains("/Windbot/"))
                continue;

            if (path.StartsWith("Assets/Tools/"))
                continue;

            if (path.StartsWith("Assets/Scripts/YGOSharp"))
                continue;

            if (EditorUtility.DisplayCancelableProgressBar(
                "扫描中...",
                $"检测: {Path.GetFileName(path)} ({i + 1}/{total})",
                (float)i / total))
                break;

            if (!IsUTF8WithoutBOM(path))
                nonUTF8Files.Add(path);
        }
        EditorUtility.ClearProgressBar();
    }

    // 检测UTF-8 without BOM
    private bool IsUTF8WithoutBOM(string path)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(path);

            // 排除带BOM的文件
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return false;

            // 验证是否为合法UTF-8
            return IsValidUTF8(bytes);
        }
        catch { return false; }
    }

    // UTF-8字节流验证算法
    private bool IsValidUTF8(byte[] bytes)
    {
        int i = 0;
        while (i < bytes.Length)
        {
            if ((bytes[i] & 0x80) == 0x00) i++; // 单字节
            else if ((bytes[i] & 0xE0) == 0xC0) // 双字节
            {
                if (i + 1 >= bytes.Length || (bytes[i + 1] & 0xC0) != 0x80)
                    return false;
                i += 2;
            }
            else if ((bytes[i] & 0xF0) == 0xE0) // 三字节
            {
                if (i + 2 >= bytes.Length ||
                    (bytes[i + 1] & 0xC0) != 0x80 ||
                    (bytes[i + 2] & 0xC0) != 0x80)
                    return false;
                i += 3;
            }
            else if ((bytes[i] & 0xF8) == 0xF0) // 四字节
            {
                if (i + 3 >= bytes.Length ||
                    (bytes[i + 1] & 0xC0) != 0x80 ||
                    (bytes[i + 2] & 0xC0) != 0x80 ||
                    (bytes[i + 3] & 0xC0) != 0x80)
                    return false;
                i += 4;
            }
            else return false;
        }
        return true;
    }

    // 用VS打开文件（支持VS Code）
    private void OpenInVS(string filePath)
    {
        string fullPath = Path.GetFullPath(filePath);

        // 优先尝试Visual Studio
        if (File.Exists(@"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe"))
            Process.Start("devenv.exe", $"/Edit \"{fullPath}\"");
        // 备用方案：用VS Code打开
        else
            Process.Start("code", $"\"{fullPath}\"");
    }

    // 安全转换编码（保留原内容）
    private void ConvertAllToUTF8()
    {
        foreach (string path in nonUTF8Files)
        {
            // 使用系统编码读取（兼容中文）
            string content = File.ReadAllText(path, Encoding.Default);
            // 写入为无BOM的UTF-8
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }
        AssetDatabase.Refresh();
        UnityEngine.Debug.Log($"已转换 {nonUTF8Files.Count} 个文件");
        nonUTF8Files.Clear();
    }
}