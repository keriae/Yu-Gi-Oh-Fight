using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MDPro3.Duel.YGOSharp;
using MDPro3.Utility;

namespace MDPro3.AI
{
    /// <summary>A fresh snapshot of the decks available to the editor and AI battle.</summary>
    internal sealed class ResponsesDeckLibrary
    {
        private readonly ResponsesDeckCatalog catalog = new();
        private readonly string deckRoot;
        private List<DeckLibraryEntry> recommendations;

        public IReadOnlyList<ResponsesDeckChoice> Choices => catalog.Choices;
        public string RecommendationError { get; private set; }

        private ResponsesDeckLibrary(string deckRoot)
        {
            this.deckRoot = Path.GetFullPath(deckRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public static ResponsesDeckLibrary Load(string deckRoot)
        {
            var library = new ResponsesDeckLibrary(deckRoot);
            library.LoadRecommendations();
            library.LoadPersonalDecks();
            return library;
        }

        private void LoadRecommendations()
        {
            try
            {
                recommendations = DeckLibrary.Load();
                // A recommendation remains the visible source of a shared
                // construction; personal copies retain their names and paths as aliases.
                foreach (var entry in recommendations)
                {
                    var category = entry.Page == DeckLibraryPage.Classic ? "经典" : "竞技";
                    AddDeck(category + " · " + entry.Name, entry.SourcePath, DeckLibrary.SearchText(entry));
                }
            }
            catch (Exception error) { RecommendationError = error.Message; }
        }

        private void LoadPersonalDecks()
        {
            if (!Directory.Exists(deckRoot)) return;
            // The editor exposes the root and one level of named deck groups.
            var directories = new[] { deckRoot }.Concat(Directory.GetDirectories(deckRoot).OrderBy(Path.GetFileName));
            foreach (var directory in directories)
                foreach (var path in Directory.GetFiles(directory, "*.ydk").OrderBy(Path.GetFileName))
                {
                    var name = Path.GetFileNameWithoutExtension(path);
                    if (recommendations != null && DeckLibrary.IsBundledTemplate(path))
                    {
                        // Keep a current bundled copy under its recommendation caption.
                        var bytes = File.ReadAllBytes(path);
                        var current = recommendations.FirstOrDefault(entry => entry.TemplateName == name && bytes.SequenceEqual(entry.TemplateBytes));
                        var choice = current == null ? null : catalog.FindByPath(current.SourcePath);
                        if (choice != null) AddDeck(choice.Name, Path.GetFullPath(path));
                        continue;
                    }
                    if (directory != deckRoot) name = Path.GetFileName(directory) + "/" + name;
                    var full = Path.GetFullPath(path);
                    AddDeck("我的 · " + name, full);
                }
        }

        private ResponsesDeckChoice AddDeck(string name, string path, string searchText = null)
        {
            if (!File.Exists(path)) return null;
            try
            {
                var deck = new Deck(path);
                return catalog.Add(name, path, deck.Main, deck.Extra, deck.Side, searchText);
            }
            catch
            {
                // Preserve an existing unreadable personal file as its own choice.
                return catalog.Add(name, path, null, null, null, searchText);
            }
        }

        public string ResolveSelection(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            var full = Path.GetFullPath(path);
            return catalog.FindByPath(full) != null ? full : null;
        }

        public string FindCurrentDeck(string name) => string.IsNullOrEmpty(name)
            ? null : ResolveSelection(Path.Combine(deckRoot, name + ".ydk"));
    }
}
