using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MDPro3.AI
{
    /// <summary>One selectable construction, with source paths and searchable labels.</summary>
    internal sealed class ResponsesDeckChoice
    {
        public string Name { get; }
        public string Path { get; }
        private readonly HashSet<string> paths = new(StringComparer.Ordinal);
        private readonly HashSet<string> searchTexts = new(StringComparer.Ordinal);

        public ResponsesDeckChoice(string name, string path, string searchText = null)
        {
            Name = name;
            Path = path;
            AddAlias(name, path, searchText);
        }

        public void AddAlias(string name, string path, string searchText = null)
        {
            paths.Add(path);
            searchTexts.Add(string.IsNullOrWhiteSpace(searchText) ? name : name + " " + searchText);
        }

        public bool ContainsPath(string path) => path != null && paths.Contains(path);
        public bool Matches(IEnumerable<string> terms) => searchTexts.Any(name =>
            terms.All(term => name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0));
    }

    /// <summary>Deduplicates playable constructions without modifying their files.</summary>
    internal sealed class ResponsesDeckCatalog
    {
        public List<ResponsesDeckChoice> Choices { get; } = new();
        private readonly Dictionary<string, ResponsesDeckChoice> byPath = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ResponsesDeckChoice> byConstruction = new(StringComparer.Ordinal);

        public ResponsesDeckChoice FindByPath(string path) => path != null && byPath.TryGetValue(path, out var choice)
            ? choice : null;

        public ResponsesDeckChoice Add(string name, string path, IReadOnlyCollection<int> main,
            IReadOnlyCollection<int> extra, IReadOnlyCollection<int> side, string searchText = null)
        {
            if (byPath.TryGetValue(path, out var existing))
            {
                existing.AddAlias(name, path, searchText);
                return existing;
            }

            var identity = ConstructionIdentity(main, extra, side);
            if (identity != null && byConstruction.TryGetValue(identity, out existing))
                existing.AddAlias(name, path, searchText);
            else
            {
                existing = new ResponsesDeckChoice(name, path, searchText);
                Choices.Add(existing);
                if (identity != null) byConstruction.Add(identity, existing);
            }
            byPath.Add(path, existing);
            return existing;
        }

        internal static string ConstructionIdentity(IReadOnlyCollection<int> main,
            IReadOnlyCollection<int> extra, IReadOnlyCollection<int> side)
        {
            // Unreadable or unusable decks stay separate. Ignore ordering and cosmetics,
            // but preserve card counts and the main/extra/side boundaries.
            if (main == null || extra == null || side == null || main.Count < 40 || main.Count > 60
                || extra.Count > 15 || side.Count > 15
                || main.Concat(extra).Concat(side).Any(code => code <= 100))
                return null;
            return Section(main) + "|" + Section(extra) + "|" + Section(side);
        }

        private static string Section(IEnumerable<int> cards) => string.Join(",",
            cards.OrderBy(code => code).Select(code => code.ToString(CultureInfo.InvariantCulture)));
    }
}
