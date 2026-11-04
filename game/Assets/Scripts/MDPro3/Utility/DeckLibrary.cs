using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using MDPro3.Duel.YGOSharp;

namespace MDPro3.Utility
{
    public enum DeckLibraryPage { Mine, Competitive, Classic }

    public sealed class DeckLibraryRecord
    {
        public string id;
        public string name;
        public string page;
        public string source_file;
        public string legacy_name;
        public int cover_card;
        public string academy_id;
        public string tutorial_name;
        public string theme_id;
        public string variant;
        public string description;
        public string[] tags;
    }

    public sealed class DeckLibraryLegacyRecord
    {
        public string name;
        public string source_file;
    }

    public sealed class DeckLibraryCatalog
    {
        public int schema;
        public List<DeckLibraryRecord> decks;
        public List<DeckLibraryLegacyRecord> legacy_presets;
    }

    public sealed class DeckLibraryEntry
    {
        public readonly string Id;
        public readonly string Name;
        public readonly DeckLibraryPage Page;
        public readonly string SourcePath;
        public readonly string LegacyName;
        public readonly int CoverCard;
        public readonly DeckAcademyDeck Academy;
        public readonly string ThemeId;
        public readonly string Variant;
        public readonly string Description;
        public readonly string[] Tags;
        public bool IsCurated => SourcePath.Contains(Path.DirectorySeparatorChar + "CuratedDecks" + Path.DirectorySeparatorChar);
        public string TutorialLabel => IsCurated ? "基础教程" : "查看教程";
        internal readonly byte[] TemplateBytes;

        internal DeckLibraryEntry(DeckLibraryRecord record, string path, byte[] data, DeckAcademyDeck academy)
        {
            Id = record.id;
            Name = record.name;
            Page = record.page == "competitive" ? DeckLibraryPage.Competitive : DeckLibraryPage.Classic;
            SourcePath = path;
            LegacyName = record.legacy_name;
            CoverCard = record.cover_card;
            Academy = academy;
            ThemeId = record.theme_id ?? record.id;
            Variant = record.variant;
            Description = record.description;
            Tags = record.tags ?? Array.Empty<string>();
            TemplateBytes = data;
        }
    }

    public static class DeckLibrary
    {
        private static Dictionary<string, byte[]> legacyTemplates = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        public static List<DeckLibraryEntry> Load()
        {
            var catalog = DeckAcademy.Load();
            if (catalog.library == null || catalog.library.schema != 1 || catalog.library.decks == null
                || catalog.library.decks.Count == 0 || catalog.files == null)
                throw new InvalidDataException("推荐卡组目录格式无效。");
            var entries = new List<DeckLibraryEntry>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var names = new HashSet<string>(StringComparer.Ordinal);
            var sources = new HashSet<string>(StringComparer.Ordinal);
            var academyIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var record in catalog.library.decks)
            {
                if (record == null || record.id == null || !Regex.IsMatch(record.id, "^[a-z0-9-]{1,64}$")
                    || !ids.Add(record.id) || !ValidName(record.name) || !names.Add(record.name)
                    || !ValidName(record.legacy_name) || record.cover_card <= 0
                    || (record.page != "competitive" && record.page != "classic") || !sources.Add(record.source_file))
                    throw new InvalidDataException("推荐卡组条目无效。");
                var path = SourcePath(record.source_file);
                var data = File.ReadAllBytes(path);
                if (!catalog.files.TryGetValue(record.source_file, out var digest) || Digest(data) != digest)
                    throw new InvalidDataException("推荐卡组模板已更改：" + record.name);
                if (record.legacy_name != Path.GetFileNameWithoutExtension(path))
                    throw new InvalidDataException("推荐卡组来源名称不一致。");
                DeckAcademyDeck academy = null;
                if (!string.IsNullOrEmpty(record.academy_id))
                {
                    academy = catalog.decks.FirstOrDefault(entry => entry.id == record.academy_id);
                    if (academy == null || (record.source_file.StartsWith("CuratedDecks/", StringComparison.Ordinal)
                        ? record.theme_id != academy.id || !ValidName(record.tutorial_name) : academy.deck_file != record.source_file))
                        throw new InvalidDataException("推荐卡组教程来源不一致。");
                    academyIds.Add(record.academy_id);
                }
                if (record.source_file.StartsWith("CuratedDecks/", StringComparison.Ordinal)
                    && (string.IsNullOrWhiteSpace(record.theme_id) || string.IsNullOrWhiteSpace(record.variant)
                        || string.IsNullOrWhiteSpace(record.description)))
                    throw new InvalidDataException("强化构筑缺少主题或打法说明。");
                var deck = new Deck(path);
                if (deck.Main.Count < 40 || deck.Main.Count > 60 || deck.Extra.Count > 15 || deck.Side.Count > 15
                    || !deck.Main.Concat(deck.Extra).Concat(deck.Side).Contains(record.cover_card))
                    throw new InvalidDataException("推荐卡组数量或封面卡无效：" + record.name);
                entries.Add(new DeckLibraryEntry(record, path, data, academy));
            }
            if (academyIds.Count != catalog.decks.Count)
                throw new InvalidDataException("推荐卡组目录未覆盖全部教程。");
            var oldTemplates = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (var record in catalog.library.legacy_presets ?? new List<DeckLibraryLegacyRecord>())
            {
                if (record == null || !ValidName(record.name) || oldTemplates.ContainsKey(record.name)
                    || record.name != Path.GetFileNameWithoutExtension(record.source_file))
                    throw new InvalidDataException("旧预置卡组索引无效。");
                var data = File.ReadAllBytes(SourcePath(record.source_file));
                if (!catalog.files.TryGetValue(record.source_file, out var digest) || Digest(data) != digest)
                    throw new InvalidDataException("旧预置卡组模板已更改。");
                oldTemplates.Add(record.name, data);
            }
            legacyTemplates = oldTemplates;
            return entries;
        }

        private static string Digest(byte[] data)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(data)).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static bool ValidName(string value) => !string.IsNullOrWhiteSpace(value)
            && value == value.Trim() && value != "." && value != ".."
            && !Regex.IsMatch(value, "[\\\\/:*?\"<>|\\p{Cc}]");

        public static string SourcePath(string relative)
        {
            if (relative == null || !Regex.IsMatch(relative, "^(Decks|BundledDecks|CuratedDecks)/[^/\\\\]+\\.ydk$")
                || relative.Contains(".."))
                throw new InvalidDataException("无效的推荐卡组模板路径。");
            var root = Path.GetFullPath(DeckAcademy.ContentPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var path = Path.GetFullPath(Path.Combine(root, relative));
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidDataException("推荐卡组模板路径超出内容目录。");
            return path;
        }

        public static bool Matches(DeckLibraryEntry entry, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;
            var text = SearchText(entry);
            return query.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .All(term => text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static string SearchText(DeckLibraryEntry entry)
        {
            var academy = entry.Academy;
            return string.Join(" ", new[] { entry.Id, entry.Name, entry.LegacyName, entry.ThemeId,
                entry.Variant, entry.Description, string.Join(" ", entry.Tags), academy?.description,
                string.Join(" ", academy?.tags ?? Array.Empty<string>()) });
        }

        public static bool IsUntouchedRecommendation(string path, IEnumerable<DeckLibraryEntry> entries)
        {
            // Only the old root-level preset filename qualifies. Copies and grouped decks remain personal.
            var fullPath = Path.GetFullPath(path);
            if (!string.Equals(Path.GetDirectoryName(fullPath), Path.GetFullPath(Program.PATH_DECK)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.Ordinal))
                return false;
            var name = Path.GetFileNameWithoutExtension(fullPath);
            var matches = entries.Where(entry => entry.LegacyName == name).ToArray();
            var bytes = File.ReadAllBytes(fullPath);
            return matches.Any(entry => bytes.SequenceEqual(entry.TemplateBytes))
                || legacyTemplates.TryGetValue(name, out var legacy) && bytes.SequenceEqual(legacy);
        }

        public static string CreateEditableCopy(DeckLibraryEntry entry) =>
            CreateEditableCopy(entry.TemplateBytes, entry.Name);

        internal static string CreateEditableCopy(byte[] data, string original)
        {
            if (!ValidName(original)) throw new InvalidDataException("无效的卡组名称。");
            Directory.CreateDirectory(Program.PATH_DECK);
            for (var number = 1; number < 10000; number++)
            {
                var name = original + " 副本" + (number == 1 ? string.Empty : " (" + number + ")");
                var target = Path.Combine(Program.PATH_DECK, name + Program.EXPANSION_YDK);
                FileStream stream;
                try
                {
                    stream = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                }
                catch (IOException) when (File.Exists(target)) { continue; }

                try
                {
                    using (stream)
                        stream.Write(data, 0, data.Length);
                    return name;
                }
                catch
                {
                    // The name belongs to this copy only after CreateNew succeeds.
                    try { File.Delete(target); } catch { }
                    throw;
                }
            }
            throw new IOException("无法为卡组创建独立副本。");
        }

        public static int CoverFor(Deck deck)
        {
            var pickup = deck.Pickup?.FirstOrDefault(code => code > 0) ?? 0;
            if (pickup > 0) return pickup;
            foreach (var code in deck.Extra.Concat(deck.Main))
            {
                var card = CardsManager.GetCardRaw(code);
                if (card != null && card.HasType(CardType.Monster)) return code;
            }
            return deck.Main.FirstOrDefault();
        }
    }
}
