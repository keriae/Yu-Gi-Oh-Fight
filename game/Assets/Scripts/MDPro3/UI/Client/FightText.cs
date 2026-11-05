using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MDPro3.Utility;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

namespace MDPro3.UI.Client
{
    /// <summary>Localizes client copy while keeping card data and user input in their own language.</summary>
    internal static class FightText
    {
        private sealed class PhraseNode
        {
            internal readonly Dictionary<char, PhraseNode> Children = new Dictionary<char, PhraseNode>();
            internal string Translation;
        }
        private sealed class Catalog
        {
            internal readonly Dictionary<string, string> Entries;
            internal readonly PhraseNode Fragments = new PhraseNode();
            internal Catalog(Dictionary<string, string> entries)
            {
                Entries = new Dictionary<string, string>(entries);
                // Older catalogs contain the dropdown glyph in exact keys.
                // Get normalizes it for fonts without that glyph; keep those
                // phrases reachable instead of translating their fragments.
                foreach (var entry in entries)
                {
                    string normalized = NormalizeSymbols(entry.Key);
                    if (normalized != entry.Key && !Entries.ContainsKey(normalized))
                        Entries[normalized] = entry.Value.Replace("▾", "·").Replace("☑", "✓");
                }
                foreach (var entry in Entries)
                {
                    // Single-character words are only translated as full labels.
                    // Embedded punctuation/numbers and unknown text stay intact.
                    if (entry.Key.Length < 2 || !ContainsChinese(entry.Key)) continue;
                    var node = Fragments;
                    foreach (char character in entry.Key)
                    {
                        if (!node.Children.TryGetValue(character, out var child))
                            node.Children[character] = child = new PhraseNode();
                        node = child;
                    }
                    node.Translation = entry.Value;
                }
            }
        }
        private static readonly Dictionary<string, Catalog> catalogs = new Dictionary<string, Catalog>();
        private static readonly Dictionary<string, string> cache = new Dictionary<string, string>();
        private static string cachedLanguage;
        private static readonly Dictionary<string, string> emergencyEnglish = new Dictionary<string, string>
        {
            { "首页", "Home" }, { "开始决斗", "Duel" }, { "卡组", "Decks" }, { "学院", "Academy" },
            { "回放", "Replays" }, { "收藏", "Collection" }, { "设置", "Settings" }, { "退出", "Exit" },
            { "取消", "Cancel" }, { "关闭", "Close" }, { "确认", "Confirm" }, { "返回", "Back" },
            { "全部", "All" }, { "保存设置", "Save settings" }, { "准备就绪", "Ready" }
        };
        internal static string Get(string source)
        {
            if (string.IsNullOrEmpty(source)) return source ?? "";
            int protectedStart = source.IndexOf('\uFFF9');
            if (protectedStart >= 0)
            {
                int protectedEnd = source.IndexOf('\uFFFB', protectedStart + 1);
                if (protectedEnd >= 0)
                    return Get(source.Substring(0, protectedStart))
                        + source.Substring(protectedStart + 1, protectedEnd - protectedStart - 1)
                        + Get(source.Substring(protectedEnd + 1));
            }
            // Normalize UI symbols only after removing protected user/card
            // names, so their literal arrows and checkboxes remain intact.
            source = NormalizeSymbols(source);
            string language = Language.GetConfig();
            if (language == Language.SimplifiedChinese || language == "zh-SC") return source;
            if (cachedLanguage != language) { cachedLanguage = language; cache.Clear(); }
            if (cache.TryGetValue(source, out var translated)) return translated;
            Catalog catalog = Load(language == Language.TraditionalChinese ? Language.TraditionalChinese : Language.English);
            // Existing native translations remain useful in the other locales.
            // New client copy falls back to English until that locale is supplied.
            if (language != Language.English && language != Language.TraditionalChinese
                && InterString.TryGet(source, out translated) && translated != source)
                return Remember(source, translated);
            if (catalog.Entries.TryGetValue(source, out translated)) return Remember(source, translated);
            if (!ContainsChinese(source)) return source;
            return Remember(source, TranslateFragments(source, catalog.Fragments));
        }
        internal static string Keep(string value) => "\uFFF9" + (value ?? "") + "\uFFFB";
        private static string NormalizeSymbols(string value) => value.Replace("▾", "· 选择").Replace("☑", "✓");
        private static string Remember(string source, string value)
        {
            if (cache.Count > 4096) cache.Clear();
            cache[source] = value; return value;
        }
        private static Catalog Load(string language)
        {
            if (catalogs.TryGetValue(language, out var catalog)) return catalog;
            Dictionary<string, string> entries = null;
            string path = Program.PATH_DATA + "ClientUI/localization/" + language + ".json";
            try
            {
                if (File.Exists(path)) entries = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));
            }
            catch (Exception error) { Debug.LogWarning("[FightClient] localization catalog: " + error.Message); }
            if (entries == null) entries = language == Language.TraditionalChinese
                ? new Dictionary<string, string>() : emergencyEnglish;
            return catalogs[language] = new Catalog(entries);
        }
        private static string TranslateFragments(string source, PhraseNode root)
        {
            var result = new StringBuilder(source.Length);
            for (int start = 0; start < source.Length;)
            {
                var node = root; string replacement = null; int end = start;
                for (int next = start; next < source.Length && node.Children.TryGetValue(source[next], out node); next++)
                    if (node.Translation != null) { replacement = node.Translation; end = next + 1; }
                if (replacement == null) result.Append(source[start++]);
                else
                {
                    // Chinese labels can concatenate words without separators.
                    // Keep their translated Latin words readable, while leaving
                    // numeric stats, markup boundaries and CJK text untouched.
                    if (result.Length > 0 && replacement.Length > 0
                        && IsLatinLetter(result[result.Length - 1]) && IsLatinLetter(replacement[0]))
                        result.Append(' ');
                    result.Append(replacement); start = end;
                }
            }
            return result.ToString();
        }
        private static bool IsLatinLetter(char value) => value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
        private static bool ContainsChinese(string value)
        {
            foreach (char character in value)
                if (character >= '\u3400' && character <= '\u9fff') return true;
            return false;
        }
        internal static void Bind(TextMeshProUGUI label, string source)
        {
            var binding = label.gameObject.AddComponent<FightLocalizedText>();
            binding.Initialize(label, source);
        }
    }

    // The client updates status/count/error captions directly. Observe those
    // assignments before rendering, without modifying their underlying values.
    [DefaultExecutionOrder(10000)]
    internal sealed class FightLocalizedText : MonoBehaviour
    {
        private TextMeshProUGUI label;
        private string source, rendered, language;
        internal void Initialize(TextMeshProUGUI target, string value)
        {
            label = target; source = value ?? ""; Apply();
        }
        private void LateUpdate()
        {
            if (label == null) return;
            bool changed = label.text != rendered;
            if (changed) source = label.text ?? "";
            if (changed || language != Language.GetConfig()) Apply();
        }
        private void Apply()
        {
            language = Language.GetConfig(); rendered = FightText.Get(source);
            label.text = rendered;
        }
    }
}
