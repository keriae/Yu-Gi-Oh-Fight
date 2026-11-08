using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;

public class TimelineYAMLMerger
{
    private class FileIDMapper
    {
        private Dictionary<string, Dictionary<long, long>> _fileToOldNewIDMap = new Dictionary<string, Dictionary<long, long>>();
        private long _nextAvailableID = 1140000000000000000; // 使用更符合Unity风格的起始值

        public long GetNewID(string assetPath, long oldID)
        {
            // 获取或创建该资产路径的ID映射字典
            if (!_fileToOldNewIDMap.TryGetValue(assetPath, out var idMap))
            {
                idMap = new Dictionary<long, long>();
                _fileToOldNewIDMap[assetPath] = idMap;
            }

            // 获取或生成该旧ID对应的新ID
            if (!idMap.TryGetValue(oldID, out var newID))
            {
                newID = _nextAvailableID++;
                idMap[oldID] = newID;
            }
            return newID;
        }

        // 检查是否已经处理过某个资产的某个ID
        public bool HasMappingFor(string assetPath, long oldID)
        {
            return _fileToOldNewIDMap.TryGetValue(assetPath, out var idMap) && idMap.ContainsKey(oldID);
        }

        // 获取所有处理过的资产路径
        public IEnumerable<string> GetAllProcessedAssetPaths() => _fileToOldNewIDMap.Keys;

        // 根据旧ID获取新ID
        public long GetNewIDFromOld(string assetPath, long oldID)
        {
            if (_fileToOldNewIDMap.TryGetValue(assetPath, out var idMap) && idMap.TryGetValue(oldID, out var newID))
            {
                return newID;
            }
            return oldID; // 如果找不到映射，返回原值
        }
    }

    [MenuItem("Assets/合并Timeline及其所有依赖 (YAML)", false, 104)]
    private static void MergeTimelineAndDependencies()
    {
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);

        // 获取原文件名（不含扩展名）
        string originalFileName = Path.GetFileNameWithoutExtension(assetPath);

        // 获取所有依赖，包括间接和重复的依赖
        string[] allDependencies = AssetDatabase.GetDependencies(assetPath, true);

        // 初始化ID映射器
        FileIDMapper idMapper = new FileIDMapper();

        // 读取主文件的YAML头
        string mainFileHeader = ExtractYamlHeader(assetPath);

        // 用于存储所有需要合并的YAML内容
        List<string> allYamlBlocks = new List<string>();
        // 用于记录已经处理过的资产，避免重复（例如多个资产依赖同一个材质）
        HashSet<string> processedAssets = new HashSet<string>();

        // 记录主资产的fileID，用于后续修改m_Name
        long mainAssetNewFileID = -1;

        // 第一阶段：收集所有资产的fileID映射
        CollectFileIDMappings(assetPath, idMapper, processedAssets);
        foreach (var dependencyPath in allDependencies)
        {
            if (dependencyPath == assetPath || dependencyPath.EndsWith(".cs") || processedAssets.Contains(dependencyPath))
                continue;
            CollectFileIDMappings(dependencyPath, idMapper, processedAssets);
        }

        processedAssets.Clear(); // 清空，为第二阶段做准备

        // 第二阶段：处理所有资产内容
        ProcessAsset(assetPath, idMapper, allYamlBlocks, processedAssets, true, originalFileName, ref mainAssetNewFileID);
        foreach (var dependencyPath in allDependencies)
        {
            if (dependencyPath == assetPath || dependencyPath.EndsWith(".cs") || processedAssets.Contains(dependencyPath))
                continue;
            ProcessAsset(dependencyPath, idMapper, allYamlBlocks, processedAssets, false, originalFileName, ref mainAssetNewFileID);
        }

        // 3. 将所有YAML块合并为一个字符串，加上主文件的头
        string combinedYaml = mainFileHeader + string.Join("\n", allYamlBlocks);

        // 4. 写入新文件
        string originalDirectory = Path.GetDirectoryName(assetPath);
        string originalExtension = Path.GetExtension(assetPath);
        string newFileName = $"{originalFileName}_merged{originalExtension}";
        string newFilePath = Path.Combine(originalDirectory, newFileName);

        // 确保文件名不重复
        newFilePath = AssetDatabase.GenerateUniqueAssetPath(newFilePath);

        // 获取新文件名（不含扩展名）
        string newFileNameWithoutExt = Path.GetFileNameWithoutExtension(newFilePath);

        // 更新主资产的m_Name以匹配新文件名
        if (mainAssetNewFileID != -1)
        {
            combinedYaml = UpdateMainAssetName(combinedYaml, mainAssetNewFileID, newFileNameWithoutExt);
        }

        combinedYaml = combinedYaml.Replace("{fileID: 337831424, guid: 1f79187d04fa6fdd168e19d4bafe7dfa, type: 3}", "{fileID: 11500000, guid: bfda56da833e2384a9677cd3c976a436, type: 3}");

        File.WriteAllText(newFilePath, combinedYaml);

        // 5. 刷新数据库并选中新文件
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        var newAsset = AssetDatabase.LoadAssetAtPath<Object>(newFilePath);
        Selection.activeObject = newAsset;

        Debug.Log($"合并完成！新文件已保存至: {newFilePath}");
    }

    [MenuItem("Assets/合并Timeline及其所有依赖 (YAML)", true)]
    private static bool ValidateReplaceStringsInAsset()
    {
        return Selection.activeObject != null &&
               !AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
    }

    // 第一阶段：收集所有资产的fileID映射
    private static void CollectFileIDMappings(string assetPath, FileIDMapper idMapper, HashSet<string> processedAssets)
    {
        if (processedAssets.Contains(assetPath)) return;

        string fullPath = Path.GetFullPath(assetPath);
        string[] yamlLines = File.ReadAllLines(fullPath);

        // 正则表达式匹配YAML文档开头和fileID行，例如：--- !u!114 &11400000
        Regex headerRegex = new Regex(@"^--- !u!([0-9]+) &([0-9]+)$");

        foreach (var line in yamlLines)
        {
            // 检查是否是YAML文档的开始
            if (line.StartsWith("---"))
            {
                // 尝试从文档头中提取旧的fileID和类型
                Match headerMatch = headerRegex.Match(line);
                if (headerMatch.Success && long.TryParse(headerMatch.Groups[2].Value, out long oldID))
                {
                    // 预先生成新ID，但不立即使用（在第二阶段使用）
                    idMapper.GetNewID(assetPath, oldID);
                }
            }
        }

        processedAssets.Add(assetPath);
    }

    // 提取YAML文件头（---之前的行）
    private static string ExtractYamlHeader(string assetPath)
    {
        string[] lines = File.ReadAllLines(assetPath);
        StringBuilder header = new StringBuilder();

        foreach (string line in lines)
        {
            if (line.StartsWith("---"))
                break;

            header.AppendLine(line);
        }

        return header.ToString();
    }

    // 更新主资产的m_Name以匹配新文件名
    private static string UpdateMainAssetName(string yamlContent, long mainAssetFileID, string newName)
    {
        // 构建主资产的fileID行模式
        string mainAssetHeaderPattern = $"--- !u!114 &{mainAssetFileID}";

        // 分割YAML内容为行
        string[] lines = yamlContent.Split('\n');
        bool isInsideMainAsset = false;
        bool nameUpdated = false;

        StringBuilder result = new StringBuilder();

        foreach (string line in lines)
        {
            // 检查是否进入主资产块
            if (line.StartsWith(mainAssetHeaderPattern))
            {
                isInsideMainAsset = true;
                result.AppendLine(line);
                continue;
            }

            // 检查是否离开主资产块（进入下一个资产块）
            if (isInsideMainAsset && line.StartsWith("--- !u!"))
            {
                isInsideMainAsset = false;
                result.AppendLine(line);
                continue;
            }

            // 如果在主资产块内且找到m_Name行，则更新它
            if (isInsideMainAsset && !nameUpdated && line.TrimStart().StartsWith("m_Name:"))
            {
                // 保留缩进，只更新名称
                int indentLength = line.IndexOf("m_Name:");
                string indent = new string(' ', indentLength);
                result.AppendLine($"{indent}m_Name: {newName}");
                nameUpdated = true;
            }
            else
            {
                result.AppendLine(line);
            }
        }

        return result.ToString();
    }

    private static void ProcessAsset(string assetPath, FileIDMapper idMapper, List<string> yamlBlocks, HashSet<string> processedAssets, bool isMainAsset, string originalFileName, ref long mainAssetNewFileID)
    {
        if (processedAssets.Contains(assetPath)) return;

        string fullPath = Path.GetFullPath(assetPath);
        string[] yamlLines = File.ReadAllLines(fullPath);

        List<string> currentBlock = new List<string>();
        bool isInsideAsset = false;
        bool skipHeader = !isMainAsset; // 非主资产跳过YAML头

        // 用于记录当前正在处理的YAML文档的原始fileID
        long currentOldFileID = -1;
        long currentNewFileID = -1;

        // 正则表达式匹配YAML文档开头和fileID行，例如：--- !u!114 &11400000
        Regex headerRegex = new Regex(@"^--- !u!([0-9]+) &([0-9]+)$");
        // 正则表达式匹配简单引用：{fileID: 12345678}
        Regex simpleRefRegex = new Regex(@"{fileID:\s*([0-9]+)\s*}");
        // 正则表达式匹配复杂引用：{fileID: 7400000, guid: aa54d0da6b3cc424dbdb7130447a5662, type: 2}
        Regex complexRefRegex = new Regex(@"{fileID:\s*([0-9]+)\s*,\s*guid:\s*([0-9a-fA-F]+)\s*,\s*type:\s*([0-9]+)\s*}");
        // 正则表达式匹配m_Script行
        Regex mScriptRegex = new Regex(@"m_Script:\s*{fileID:\s*([0-9]+)\s*(,\s*guid:\s*[0-9a-fA-F]+\s*,\s*type:\s*[0-9]+\s*)?}");

        foreach (var line in yamlLines)
        {
            // 跳过非主资产的YAML头
            if (skipHeader && !line.StartsWith("---"))
                continue;

            skipHeader = false; // 遇到第一个---后就不再跳过头

            // 检查是否是YAML文档的开始
            if (line.StartsWith("---"))
            {
                // 如果遇到一个新的文档开头，且当前已经有一个正在处理的块，则先处理并保存前一个块
                if (isInsideAsset && currentBlock.Count > 0)
                {
                    string processedBlock = ProcessYamlBlock(currentBlock, assetPath, idMapper, simpleRefRegex, complexRefRegex, mScriptRegex);
                    yamlBlocks.Add(processedBlock);
                    currentBlock.Clear();
                }

                // 开始一个新的YAML文档块
                isInsideAsset = true;

                // 尝试从文档头中提取旧的fileID和类型
                Match headerMatch = headerRegex.Match(line);
                if (headerMatch.Success && long.TryParse(headerMatch.Groups[2].Value, out long oldID))
                {
                    currentOldFileID = oldID;
                    // 获取映射的新ID
                    currentNewFileID = idMapper.GetNewIDFromOld(assetPath, currentOldFileID);
                    // 保留!u!部分，只替换&后面的fileID
                    string newLine = $"--- !u!{headerMatch.Groups[1].Value} &{currentNewFileID}";
                    currentBlock.Add(newLine);

                    // 如果是主资产，记录其新fileID
                    if (isMainAsset && mainAssetNewFileID == -1)
                    {
                        mainAssetNewFileID = currentNewFileID;
                    }
                }
                else
                {
                    // 如果没有匹配到fileID，直接添加原行
                    currentBlock.Add(line);
                    currentOldFileID = -1; // 无效或没有fileID
                    currentNewFileID = -1;
                }
            }
            else if (isInsideAsset)
            {
                // 处理文档内部的行
                if (!string.IsNullOrEmpty(line) && currentOldFileID != -1)
                {
                    string processedLine = line;

                    // 检查是否是m_Script行，如果是则跳过处理
                    if (mScriptRegex.IsMatch(processedLine))
                    {
                        currentBlock.Add(processedLine);
                        continue;
                    }

                    // 1. 处理复杂引用（外部文件引用）
                    processedLine = complexRefRegex.Replace(processedLine, match =>
                    {
                        if (long.TryParse(match.Groups[1].Value, out long referencedOldID))
                        {
                            string guid = match.Groups[2].Value;

                            // 查找GUID对应的资产路径
                            string refAssetPath = AssetDatabase.GUIDToAssetPath(guid);
                            if (!string.IsNullOrEmpty(refAssetPath) && refAssetPath != assetPath)
                            {
                                // 这是一个外部引用，我们需要获取它的新ID
                                long referencedNewID = idMapper.GetNewIDFromOld(refAssetPath, referencedOldID);
                                return $"{{fileID: {referencedNewID}}}";
                            }
                            else if (!string.IsNullOrEmpty(refAssetPath) && refAssetPath == assetPath)
                            {
                                // 这是对同一文件的引用
                                long referencedNewID = idMapper.GetNewIDFromOld(assetPath, referencedOldID);
                                return $"{{fileID: {referencedNewID}}}";
                            }
                        }
                        return match.Value; // 如果解析失败或无需替换，返回原值
                    });

                    // 2. 处理简单引用（同一文件内的引用）
                    processedLine = simpleRefRegex.Replace(processedLine, match =>
                    {
                        if (long.TryParse(match.Groups[1].Value, out long referencedOldID))
                        {
                            // 跳过fileID: 0（空引用）
                            if (referencedOldID == 0)
                                return match.Value;

                            // 检查是否在同一文件中
                            long referencedNewID = idMapper.GetNewIDFromOld(assetPath, referencedOldID);
                            if (referencedNewID != referencedOldID)
                            {
                                return $"{{fileID: {referencedNewID}}}";
                            }
                        }
                        return match.Value; // 如果解析失败或无需替换，返回原值
                    });

                    currentBlock.Add(processedLine);
                }
                else
                {
                    currentBlock.Add(line);
                }
            }
        }

        // 处理文件末尾的最后一个块
        if (isInsideAsset && currentBlock.Count > 0)
        {
            string processedBlock = ProcessYamlBlock(currentBlock, assetPath, idMapper, simpleRefRegex, complexRefRegex, mScriptRegex);
            yamlBlocks.Add(processedBlock);
        }

        processedAssets.Add(assetPath);
    }

    private static string ProcessYamlBlock(List<string> yamlBlock, string assetPath, FileIDMapper idMapper, Regex simpleRefRegex, Regex complexRefRegex, Regex mScriptRegex)
    {
        // 这个函数现在已经在ProcessAsset中逐行处理时完成了大部分工作
        // 这里主要起一个兜底和格式化的作用，确保块以换行符结束
        return string.Join("\n", yamlBlock);
    }

}