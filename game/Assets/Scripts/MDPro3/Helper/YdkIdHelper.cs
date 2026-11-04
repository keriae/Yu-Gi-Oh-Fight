using MDPro3.Duel.YGOSharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MDPro3
{
    public static class YdkIdHelper
    {
        public const string FILE_NAME = "idChangelog.jsonp";

        private static readonly object _lock = new();
        private static bool _loaded;
        private static Dictionary<int, int> _map;

        // Matches an ID line (optionally with # and whitespace), e.g. "12345678", "#12345678", "   12345678  "
        private static readonly Regex _ydkIdLine =
            new(@"^(\s*#?\s*)(\d+)(\s*)$", RegexOptions.Compiled);

        private enum DeckSection
        {
            None,
            Main,
            Extra,
            Side
        }

        public static void EnsureLoaded()
        {
            if (_loaded) return;
            lock (_lock)
            {
                if (_loaded) return;
                Load();
                _loaded = true;
            }
        }

        private static void Load()
        {
            _map = new Dictionary<int, int>();

            try
            {
                var path = Path.Combine(Program.PATH_DATA, FILE_NAME);
                if (!File.Exists(path))
                    return;
                var text = File.ReadAllText(path);
                _map = JsonConvert.DeserializeObject<Dictionary<int, int>>(text);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[YDK_Helper] Failed to load mappings: {e.Message}");
                _map?.Clear();
            }
        }

        public static int ToOfficial(int id)
        {
            EnsureLoaded();
            var official = (_map != null && _map.TryGetValue(id, out var mapped)) ? mapped : id;
            if(official == id)
                return id;
            bool exist = CardsManager.Get(official, true).Id != 0;
            return exist ? official : id;
        }

        public static int NormalizeList(List<int> list)
        {
            if (list == null || list.Count == 0) return 0;

            EnsureLoaded();
            if (_map == null || _map.Count == 0) return 0;

            var changed = 0;
            for (var i = 0; i < list.Count; i++)
            {
                var oldId = list[i];
                if (_map.TryGetValue(oldId, out var newId) && newId != oldId)
                {
                    list[i] = newId;
                    changed++;
                }
            }
            return changed;
        }

        public static int NormalizeDeck(Deck deck)
        {
            if (deck == null) return 0;

            var changed = 0;
            changed += NormalizeList(deck.Main);
            changed += NormalizeList(deck.Extra);
            changed += NormalizeList(deck.Side);
            return changed;
        }

        /// <summary>
        /// Reads a .ydk file and rewrites it on disk, replacing pre-release ids with official ids
        /// only inside #main, #extra and !side sections.
        /// Returns the number of lines changed.
        /// </summary>
        public static int NormalizeYdkFile(string path)
        {
            EnsureLoaded();
            if (_map == null || _map.Count == 0) return 0;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return 0;

            string original;
            try
            {
                original = File.ReadAllText(path, Encoding.UTF8);
            }
            catch
            {
                original = File.ReadAllText(path);
            }

            // Preserve original newline style.
            var newline = original.Contains("\r\n") ? "\r\n" : "\n";

            // Preserve empty lines and trailing newline (do NOT RemoveEmptyEntries).
            var lines = original.Replace("\r\n", "\n").Split('\n');

            var changed = 0;
            var section = DeckSection.None;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = line.TrimStart();

                if (trimmed.StartsWith("#main", StringComparison.Ordinal))
                {
                    section = DeckSection.Main;
                    continue;
                }
                if (trimmed.StartsWith("#extra", StringComparison.Ordinal))
                {
                    section = DeckSection.Extra;
                    continue;
                }
                if (trimmed.StartsWith("!side", StringComparison.Ordinal))
                {
                    section = DeckSection.Side;
                    continue;
                }

                var m = _ydkIdLine.Match(line);
                if (!m.Success)
                {
                    // Any other ydk directive/header exits card sections.
                    if (trimmed.StartsWith("#", StringComparison.Ordinal) ||
                        trimmed.StartsWith("!", StringComparison.Ordinal))
                    {
                        section = DeckSection.None;
                    }
                    continue;
                }

                if (section == DeckSection.None) continue;

                if (!int.TryParse(m.Groups[2].Value, out var id)) continue;
                if (id <= 100) continue;

                var mapped = ToOfficial(id);
                if (mapped == id) continue;

                // Rebuild line, preserving prefix and trailing whitespace.
                lines[i] = m.Groups[1].Value + mapped.ToString() + m.Groups[3].Value;
                changed++;
            }

            if (changed <= 0) return 0;

            var sb = new StringBuilder(original.Length);
            for (int i = 0; i < lines.Length; i++)
            {
                sb.Append(lines[i]);
                if (i < lines.Length - 1) sb.Append(newline);
            }

            try
            {
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            }
            catch
            {
                File.WriteAllText(path, sb.ToString());
            }

            return changed;
        }

        public static void SetDirty()
        {
            _loaded = false;
        }
    }
}
