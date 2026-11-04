using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

public class ScriptReferenceEditor : EditorWindow
{
    private UnityEngine.Object selectedAsset;
    private string assetPath;
    private List<ScriptReference> scriptReferences = new List<ScriptReference>();
    private List<ScriptReference> filteredReferences = new List<ScriptReference>();
    private Vector2 scrollPosition;
    private TextAsset assetTextPreview;
    private bool showRawText = false;
    private Dictionary<string, bool> modifiedReferences = new Dictionary<string, bool>();
    private bool selectAllForBatchUpdate = false;
    private bool showAdvancedOptions = false;
    private string searchFilter = "";
    private bool caseSensitiveSearch = false;

    [MenuItem("Assets/编辑脚本引用", false, 101)]
    public static void InspectScriptReferences()
    {
        var selected = Selection.activeObject;
        if (selected != null)
        {
            var window = GetWindow<ScriptReferenceEditor>("Script Reference Editor");
            window.selectedAsset = selected;
            window.assetPath = AssetDatabase.GetAssetPath(selected);
            window.ParseAssetReferences();
            window.Focus();
        }
    }

    [MenuItem("Assets/编辑脚本引用", true)]
    private static bool ValidateReplaceStringsInAsset()
    {
        return Selection.activeObject != null &&
               !AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
    }

    [MenuItem("Tools/Script Reference Editor")]
    public static void ShowWindow()
    {
        GetWindow<ScriptReferenceEditor>("Script Reference Editor");
    }

    private void OnGUI()
    {
        GUILayout.Label("Script Reference Editor", EditorStyles.boldLabel);

        // Asset selection
        EditorGUI.BeginChangeCheck();
        selectedAsset = EditorGUILayout.ObjectField("Asset", selectedAsset, typeof(UnityEngine.Object), false);
        if (EditorGUI.EndChangeCheck() && selectedAsset != null)
        {
            assetPath = AssetDatabase.GetAssetPath(selectedAsset);
            ParseAssetReferences();
        }

        if (string.IsNullOrEmpty(assetPath))
        {
            EditorGUILayout.HelpBox("Please select an asset to inspect.", MessageType.Info);
            return;
        }

        // Search filter
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Search by Name:", GUILayout.Width(120));
        EditorGUI.BeginChangeCheck();
        searchFilter = EditorGUILayout.TextField(searchFilter);
        if (EditorGUI.EndChangeCheck())
        {
            FilterReferences();
        }
        caseSensitiveSearch = EditorGUILayout.ToggleLeft("Case Sensitive", caseSensitiveSearch, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        // Toggle for raw text preview
        showRawText = EditorGUILayout.Toggle("Show Raw Text", showRawText);
        if (showRawText)
        {
            if (assetTextPreview == null && File.Exists(assetPath))
            {
                assetTextPreview = new TextAsset(File.ReadAllText(assetPath));
            }

            if (assetTextPreview != null)
            {
                EditorGUILayout.TextArea(assetTextPreview.text, GUILayout.Height(200));
            }
        }

        // Display references
        GUILayout.Space(10);
        GUILayout.Label($"References in: {assetPath}", EditorStyles.boldLabel);

        if (scriptReferences.Count == 0)
        {
            EditorGUILayout.HelpBox("No script references found in this asset.", MessageType.Info);
            return;
        }

        // Show filter info
        if (!string.IsNullOrEmpty(searchFilter) && filteredReferences.Count != scriptReferences.Count)
        {
            EditorGUILayout.HelpBox($"Showing {filteredReferences.Count} of {scriptReferences.Count} references matching '{searchFilter}'", MessageType.Info);
        }

        // Advanced options
        showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options");
        if (showAdvancedOptions)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Batch update options
            GUILayout.Label("Batch Update Options", EditorStyles.boldLabel);
            selectAllForBatchUpdate = EditorGUILayout.Toggle("Select All for Batch Update", selectAllForBatchUpdate);

            if (selectAllForBatchUpdate)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Apply to All Selected"))
                {
                    ApplyToAllSelected();
                }
                if (GUILayout.Button("Deselect All"))
                {
                    DeselectAllForBatchUpdate();
                }
                EditorGUILayout.EndHorizontal();
            }

            // Global update options
            GUILayout.Space(10);
            GUILayout.Label("Global Update", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply to All Same GUID"))
            {
                ApplyToAllSameGUID();
            }
            if (GUILayout.Button("Apply to All Same FileID"))
            {
                ApplyToAllSameFileID();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Use filtered references if search is active
        var displayReferences = string.IsNullOrEmpty(searchFilter) ? scriptReferences : filteredReferences;

        for (int i = 0; i < displayReferences.Count; i++)
        {
            var reference = displayReferences[i];

            // Change background color if modified
            bool isModified = modifiedReferences.ContainsKey(reference.uniqueId) && modifiedReferences[reference.uniqueId];
            if (isModified)
            {
                GUI.backgroundColor = new Color(0.8f, 1.0f, 0.8f); // Light green
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white; // Reset color

            GUILayout.Label($"Reference {i + 1}:", EditorStyles.boldLabel);

            // Display current values
            EditorGUILayout.LabelField("Name", reference.m_Name);
            reference.fileID = EditorGUILayout.LongField("File ID", reference.fileID);
            reference.guid = EditorGUILayout.TextField("GUID", reference.guid);
            reference.type = EditorGUILayout.IntField("Type", reference.type);
            EditorGUILayout.LabelField("Line Number", reference.lineNumber.ToString());

            // Batch update selection
            if (selectAllForBatchUpdate)
            {
                reference.selectedForBatchUpdate = EditorGUILayout.Toggle("Include in Batch", reference.selectedForBatchUpdate);
            }

            // Add apply button for each reference
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Changes"))
            {
                ApplyReferenceChange(i, displayReferences);
            }

            if (GUILayout.Button("Apply to All Same"))
            {
                ApplyToAllSameReferences(i, displayReferences);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        EditorGUILayout.EndScrollView();

        // Add button to apply all changes at once
        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply All Changes", GUILayout.Height(30)))
        {
            ApplyAllChanges();
        }

        // Add button to reload references
        if (GUILayout.Button("Reload References"))
        {
            ParseAssetReferences();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void FilterReferences()
    {
        if (string.IsNullOrEmpty(searchFilter))
        {
            filteredReferences = scriptReferences;
            return;
        }

        filteredReferences = scriptReferences.Where(r =>
        {
            if (caseSensitiveSearch)
                return r.m_Name.Contains(searchFilter);
            else
                return r.m_Name.ToLower().Contains(searchFilter.ToLower());
        }).ToList();
    }

    private void ParseAssetReferences()
    {
        scriptReferences.Clear();
        filteredReferences.Clear();
        modifiedReferences.Clear();

        if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath)) return;

        // Read the asset file
        string[] lines = File.ReadAllLines(assetPath);
        assetTextPreview = new TextAsset(File.ReadAllText(assetPath));

        // More flexible regex pattern to match script references with varying whitespace
        // Updated to handle negative fileID values
        Regex scriptPattern = new Regex(@"\s*m_Script\s*:\s*\{\s*fileID\s*:\s*(-?\d+)\s*,\s*guid\s*:\s*([0-9a-fA-F]{32})\s*,\s*type\s*:\s*(-?\d+)\s*\}");

        for (int i = 0; i < lines.Length; i++)
        {
            Match match = scriptPattern.Match(lines[i]);
            if (match.Success)
            {
                // Try to find m_Name in the following lines
                string m_Name = "Not found";
                for (int j = i + 1; j < Mathf.Min(i + 10, lines.Length); j++)
                {
                    if (lines[j].Contains("m_Name:"))
                    {
                        var nameMatch = Regex.Match(lines[j], @"m_Name\s*:\s*(.+)");
                        if (nameMatch.Success)
                        {
                            m_Name = nameMatch.Groups[1].Value.Trim();
                        }
                        break;
                    }
                }

                ScriptReference reference = new ScriptReference
                {
                    lineNumber = i,
                    fileID = long.Parse(match.Groups[1].Value),
                    guid = match.Groups[2].Value,
                    type = int.Parse(match.Groups[3].Value),
                    originalLine = lines[i],
                    m_Name = m_Name,
                    uniqueId = $"{match.Groups[1].Value}_{match.Groups[2].Value}_{match.Groups[3].Value}",
                    originalFileID = long.Parse(match.Groups[1].Value),
                    originalGUID = match.Groups[2].Value,
                    originalType = int.Parse(match.Groups[3].Value)
                };

                scriptReferences.Add(reference);
            }
            else
            {
                // Try a more flexible approach for lines that might have different formatting
                if (lines[i].Contains("m_Script") && lines[i].Contains("fileID") && lines[i].Contains("guid"))
                {
                    Debug.LogWarning($"Potential script reference found but not parsed correctly at line {i}: {lines[i]}");
                }
            }
        }

        // If no references found with the main pattern, try a more manual approach
        if (scriptReferences.Count == 0)
        {
            FindScriptReferencesManually(lines);
        }

        // Apply any existing filter
        FilterReferences();
    }

    private void FindScriptReferencesManually(string[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("m_Script"))
            {
                // Look for the complete reference across multiple lines if needed
                string combinedLine = lines[i];
                int j = i;

                // Combine subsequent lines until we have a complete reference
                while (!combinedLine.Contains("}") && j < lines.Length - 1)
                {
                    j++;
                    combinedLine += " " + lines[j].Trim();
                }

                // Try to extract the components with flexible parsing
                ExtractScriptReference(combinedLine, i, lines);
            }
        }
    }

    private void ExtractScriptReference(string line, int lineNumber, string[] allLines)
    {
        // Try to extract fileID (can be negative)
        long fileID = 0;
        int fileIDIndex = line.IndexOf("fileID");
        if (fileIDIndex >= 0)
        {
            int colonIndex = line.IndexOf(':', fileIDIndex);
            int commaIndex = line.IndexOf(',', colonIndex);
            if (colonIndex >= 0 && commaIndex > colonIndex)
            {
                string fileIDStr = line.Substring(colonIndex + 1, commaIndex - colonIndex - 1).Trim();
                long.TryParse(fileIDStr, out fileID);
            }
        }

        // Try to extract guid
        string guid = "";
        int guidIndex = line.IndexOf("guid");
        if (guidIndex >= 0)
        {
            int colonIndex = line.IndexOf(':', guidIndex);
            int commaIndex = line.IndexOf(',', colonIndex);
            if (colonIndex >= 0 && commaIndex > colonIndex)
            {
                guid = line.Substring(colonIndex + 1, commaIndex - colonIndex - 1).Trim();
            }
        }

        // Try to extract type (can be negative)
        int type = 0;
        int typeIndex = line.IndexOf("type");
        if (typeIndex >= 0)
        {
            int colonIndex = line.IndexOf(':', typeIndex);
            int braceIndex = line.IndexOf('}', colonIndex);
            if (colonIndex >= 0 && braceIndex > colonIndex)
            {
                string typeStr = line.Substring(colonIndex + 1, braceIndex - colonIndex - 1).Trim();
                int.TryParse(typeStr, out type);
            }
        }

        // Try to find m_Name in the following lines
        string m_Name = "Not found";
        for (int j = lineNumber + 1; j < Mathf.Min(lineNumber + 10, allLines.Length); j++)
        {
            if (allLines[j].Contains("m_Name:"))
            {
                var nameMatch = Regex.Match(allLines[j], @"m_Name\s*:\s*(.+)");
                if (nameMatch.Success)
                {
                    m_Name = nameMatch.Groups[1].Value.Trim();
                }
                break;
            }
        }

        // If we found all components, add the reference
        if (!string.IsNullOrEmpty(guid))
        {
            ScriptReference reference = new ScriptReference
            {
                lineNumber = lineNumber,
                fileID = fileID,
                guid = guid,
                type = type,
                originalLine = line,
                m_Name = m_Name,
                uniqueId = $"{fileID}_{guid}_{type}",
                originalFileID = fileID,
                originalGUID = guid,
                originalType = type
            };

            scriptReferences.Add(reference);
            Debug.Log($"Manually extracted reference: fileID={fileID}, guid={guid}, type={type}, name={m_Name}");
        }
    }

    private void ApplyReferenceChange(int index, List<ScriptReference> displayList)
    {
        if (index < 0 || index >= displayList.Count) return;

        var reference = displayList[index];
        string[] lines = File.ReadAllLines(assetPath);

        // Create the new line with updated values
        string newLine = lines[reference.lineNumber];
        newLine = Regex.Replace(newLine, @"fileID\s*:\s*-?\d+", $"fileID: {reference.fileID}");
        newLine = Regex.Replace(newLine, @"guid\s*:\s*[0-9a-fA-F]{32}", $"guid: {reference.guid}");
        newLine = Regex.Replace(newLine, @"type\s*:\s*-?\d+", $"type: {reference.type}");

        lines[reference.lineNumber] = newLine;

        // Write back to file
        File.WriteAllLines(assetPath, lines);

        // Mark as modified
        modifiedReferences[reference.uniqueId] = true;

        // Refresh the asset database
        AssetDatabase.ImportAsset(assetPath);

        Debug.Log($"Updated reference at line {reference.lineNumber}");
    }

    private void ApplyToAllSameReferences(int index, List<ScriptReference> displayList)
    {
        if (index < 0 || index >= displayList.Count) return;

        var reference = displayList[index];
        string[] lines = File.ReadAllLines(assetPath);

        // Find all references with the same original values
        var sameReferences = scriptReferences.Where(r =>
            r.originalFileID == reference.originalFileID &&
            r.originalGUID == reference.originalGUID &&
            r.originalType == reference.originalType).ToList();

        foreach (var sameRef in sameReferences)
        {
            // Create the new line with updated values
            string newLine = lines[sameRef.lineNumber];
            newLine = Regex.Replace(newLine, @"fileID\s*:\s*-?\d+", $"fileID: {reference.fileID}");
            newLine = Regex.Replace(newLine, @"guid\s*:\s*[0-9a-fA-F]{32}", $"guid: {reference.guid}");
            newLine = Regex.Replace(newLine, @"type\s*:\s*-?\d+", $"type: {reference.type}");

            lines[sameRef.lineNumber] = newLine;

            // Mark as modified
            modifiedReferences[sameRef.uniqueId] = true;
        }

        // Write back to file
        File.WriteAllLines(assetPath, lines);

        // Refresh the asset database
        AssetDatabase.ImportAsset(assetPath);

        Debug.Log($"Updated {sameReferences.Count} references with same original values");
    }

    private void ApplyToAllSelected()
    {
        var selectedReferences = scriptReferences.Where(r => r.selectedForBatchUpdate).ToList();
        if (selectedReferences.Count == 0) return;

        string[] lines = File.ReadAllLines(assetPath);

        foreach (var reference in selectedReferences)
        {
            // Create the new line with updated values
            string newLine = lines[reference.lineNumber];
            newLine = Regex.Replace(newLine, @"fileID\s*:\s*-?\d+", $"fileID: {reference.fileID}");
            newLine = Regex.Replace(newLine, @"guid\s*:\s*[0-9a-fA-F]{32}", $"guid: {reference.guid}");
            newLine = Regex.Replace(newLine, @"type\s*:\s*-?\d+", $"type: {reference.type}");

            lines[reference.lineNumber] = newLine;

            // Mark as modified
            modifiedReferences[reference.uniqueId] = true;
        }

        // Write back to file
        File.WriteAllLines(assetPath, lines);

        // Refresh the asset database
        AssetDatabase.ImportAsset(assetPath);

        Debug.Log($"Updated {selectedReferences.Count} selected references");
    }

    private void ApplyToAllSameGUID()
    {
        if (scriptReferences.Count == 0) return;

        // Get the most common GUID
        var guidGroups = scriptReferences.GroupBy(r => r.guid)
                                        .OrderByDescending(g => g.Count())
                                        .ToList();

        if (guidGroups.Count == 0) return;

        string mostCommonGUID = guidGroups[0].Key;

        // Ask for confirmation
        if (!EditorUtility.DisplayDialog("Confirm Update",
            $"This will update all references with GUID: {mostCommonGUID}\n\nAre you sure?",
            "Yes", "No"))
        {
            return;
        }

        string[] lines = File.ReadAllLines(assetPath);

        foreach (var reference in scriptReferences.Where(r => r.guid == mostCommonGUID))
        {
            // Create the new line with updated values
            string newLine = lines[reference.lineNumber];
            newLine = Regex.Replace(newLine, @"fileID\s*:\s*-?\d+", $"fileID: {reference.fileID}");
            newLine = Regex.Replace(newLine, @"guid\s*:\s*[0-9a-fA-F]{32}", $"guid: {reference.guid}");
            newLine = Regex.Replace(newLine, @"type\s*:\s*-?\d+", $"type: {reference.type}");

            lines[reference.lineNumber] = newLine;

            // Mark as modified
            modifiedReferences[reference.uniqueId] = true;
        }

        // Write back to file
        File.WriteAllLines(assetPath, lines);

        // Refresh the asset database
        AssetDatabase.ImportAsset(assetPath);

        Debug.Log($"Updated all references with GUID: {mostCommonGUID}");
    }

    private void ApplyToAllSameFileID()
    {
        if (scriptReferences.Count == 0) return;

        // Get the most common FileID
        var fileIDGroups = scriptReferences.GroupBy(r => r.fileID)
                                          .OrderByDescending(g => g.Count())
                                          .ToList();

        if (fileIDGroups.Count == 0) return;

        long mostCommonFileID = fileIDGroups[0].Key;

        // Ask for confirmation
        if (!EditorUtility.DisplayDialog("Confirm Update",
            $"This will update all references with FileID: {mostCommonFileID}\n\nAre you sure?",
            "Yes", "No"))
        {
            return;
        }

        string[] lines = File.ReadAllLines(assetPath);

        foreach (var reference in scriptReferences.Where(r => r.fileID == mostCommonFileID))
        {
            // Create the new line with updated values
            string newLine = lines[reference.lineNumber];
            newLine = Regex.Replace(newLine, @"fileID\s*:\s*-?\d+", $"fileID: {reference.fileID}");
            newLine = Regex.Replace(newLine, @"guid\s*:\s*[0-9a-fA-F]{32}", $"guid: {reference.guid}");
            newLine = Regex.Replace(newLine, @"type\s*:\s*-?\d+", $"type: {reference.type}");

            lines[reference.lineNumber] = newLine;

            // Mark as modified
            modifiedReferences[reference.uniqueId] = true;
        }

        // Write back to file
        File.WriteAllLines(assetPath, lines);

        // Refresh the asset database
        AssetDatabase.ImportAsset(assetPath);

        Debug.Log($"Updated all references with FileID: {mostCommonFileID}");
    }

    private void DeselectAllForBatchUpdate()
    {
        foreach (var reference in scriptReferences)
        {
            reference.selectedForBatchUpdate = false;
        }
    }

    private void ApplyAllChanges()
    {
        if (scriptReferences.Count == 0) return;

        string[] lines = File.ReadAllLines(assetPath);

        foreach (var reference in scriptReferences)
        {
            // Create the new line with updated values
            string newLine = lines[reference.lineNumber];
            newLine = Regex.Replace(newLine, @"fileID\s*:\s*-?\d+", $"fileID: {reference.fileID}");
            newLine = Regex.Replace(newLine, @"guid\s*:\s*[0-9a-fA-F]{32}", $"guid: {reference.guid}");
            newLine = Regex.Replace(newLine, @"type\s*:\s*-?\d+", $"type: {reference.type}");

            lines[reference.lineNumber] = newLine;

            // Mark as modified
            modifiedReferences[reference.uniqueId] = true;
        }

        // Write back to file
        File.WriteAllLines(assetPath, lines);

        // Refresh the asset database
        AssetDatabase.ImportAsset(assetPath);

        Debug.Log($"Updated {scriptReferences.Count} references in {assetPath}");
    }

    [System.Serializable]
    private class ScriptReference
    {
        public int lineNumber;
        public long fileID;
        public string guid;
        public int type;
        public string originalLine;
        public string m_Name;
        public string uniqueId;
        public bool selectedForBatchUpdate;

        // Store original values for comparison
        public long originalFileID;
        public string originalGUID;
        public int originalType;
    }
}