// ScriptableObjectToConfigReplacements.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ScriptReplacementConfig", menuName = "Scriptable Objects/Script Replacement Configuration")]
public class ScriptReplacementConfig : ScriptableObject
{
    [Serializable]
    public class ScriptReplacementPair
    {
        public string scriptName; // 脚本名称
        public string oldGuid; // 旧的GUID
        public string newGuid; // 新的GUID
        public long oldFileId; // 旧的FileID
        public long newFileId; // 新的FileID
    }

    public List<ScriptReplacementPair> replacementPairs = new List<ScriptReplacementPair>();
}