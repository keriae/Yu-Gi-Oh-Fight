using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MDPro3.Duel.YGOSharp
{
    internal static class RelatedCardsManager
    {
        private const string FileName = "RelatedCards.json";

        private sealed class RelatedCardsEntry
        {
            public List<int> ID { get; set; }
            public List<int> Arch { get; set; }
            public List<int> LevelLinkRank { get; set; }
            public List<int> Race { get; set; }
            public List<int> Type { get; set; }
            public List<int> Other { get; set; }
            public List<int> Targets { get; set; }

            [JsonProperty("Level/Link/Rank")]
            private List<int> LevelLinkRankProperty
            {
                get => LevelLinkRank;
                set => LevelLinkRank = value;
            }

            internal void ApplyDefaults()
            {
                ID ??= new List<int>();
                Arch ??= new List<int>();
                LevelLinkRank ??= new List<int>();
                Race ??= new List<int>();
                Type ??= new List<int>();
                Other ??= new List<int>();
                Targets ??= new List<int>();
            }

            internal IEnumerable<int> EnumerateIds()
            {
                foreach (var id in ID)
                    yield return id;
                foreach (var id in Arch)
                    yield return id;
                foreach (var id in LevelLinkRank)
                    yield return id;
                foreach (var id in Race)
                    yield return id;
                foreach (var id in Type)
                    yield return id;
                foreach (var id in Other)
                    yield return id;
                foreach (var id in Targets)
                    yield return id;
            }
        }

        private static bool initialized;
        private static Dictionary<int, RelatedCardsEntry> entries;

        internal static List<Card> Search(int code)
        {
            Initialize();
            if (entries == null || !entries.TryGetValue(code, out var entry))
                return null;

            entry.ApplyDefaults();
            var cards = new List<Card>();
            var seen = new HashSet<int> { code };
            foreach (var relatedId in entry.EnumerateIds())
            {
                if (!seen.Add(relatedId))
                    continue;
                var card = CardsManager.GetCard(relatedId);
                if (card == null || card.HasType(CardType.Token))
                    continue;
                cards.Add(card);
            }

            return cards;
        }

        private static void Initialize()
        {
            if (initialized)
                return;
            initialized = true;

            string json = null;
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            if (BetterStreamingAssets.FileExists(FileName))
                json = BetterStreamingAssets.ReadAllText(FileName);
#else
            var path = Path.Combine(Application.streamingAssetsPath, FileName);
            if (File.Exists(path))
                json = File.ReadAllText(path);
#endif

            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                var rawEntries = JsonConvert.DeserializeObject<Dictionary<string, RelatedCardsEntry>>(json);
                if (rawEntries == null)
                    return;

                entries = rawEntries
                    .Select(pair =>
                    {
                        if (!int.TryParse(pair.Key, out var key) || pair.Value == null)
                            return (valid: false, key: 0, value: (RelatedCardsEntry)null);
                        pair.Value.ApplyDefaults();
                        return (valid: true, key, value: pair.Value);
                    })
                    .Where(item => item.valid)
                    .ToDictionary(item => item.key, item => item.value);
            }
            catch (JsonException ex)
            {
                Debug.LogError($"Failed to parse {FileName}: {ex.Message}");
            }
        }
    }
}
