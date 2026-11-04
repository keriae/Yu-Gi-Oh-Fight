// StringReplacementConfig.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StringReplacementConfig", menuName = "Scriptable Objects/String Replacement Configuration")]
public class StringReplacementConfig : ScriptableObject
{
    [Serializable]
    public class ReplacementRule
    {
        public string findString;    // 要查找的字符串
        public string replaceString; // 替换成的字符串
        [Tooltip("是否区分大小写")]
        public bool caseSensitive = true;
    }

    public List<ReplacementRule> replacementRules = new List<ReplacementRule>();

    // 可选：添加一些实用方法
    public bool HasRules()
    {
        return replacementRules != null && replacementRules.Count > 0;
    }
}