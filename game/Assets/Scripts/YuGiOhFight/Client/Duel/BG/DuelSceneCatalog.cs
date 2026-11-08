using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;

namespace YuGiOhFight.Client.Duel
{
    public sealed class DuelSceneDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public string PreviewPath { get; }
        public string OverviewPath { get; }
        public string Style { get; }
        public Color AccentColor { get; }
        public string Builder { get; }
        public string TexturePath { get; }

        internal DuelSceneDefinition(string id, string name, string description, string preview, string overview,
            string style, Color accent, string builder, string texture)
        {
            Id = id; Name = name; Description = description;
            PreviewPath = preview; Builder = builder; TexturePath = texture;
            OverviewPath = overview; Style = style; AccentColor = accent;
        }
    }

    // UI and presentation share stable identifiers; reading never rewrites a preference.
    public static class DuelSceneCatalog
    {
        public const string EnabledKey = "DuelScene3D";
        public const string SceneKey = "DuelScene";
        public const string DefaultId = "classic";
        public const string AcademyId = "battle-academy";
        public const string EgyptId = "ancient-egypt";
        public const string CityId = "skyscraper";
        public static bool IsIntegratedTheme(string id)
            => id == AcademyId || id == EgyptId || id == CityId;
        private static readonly Regex Identifier = new Regex(@"\A[a-z][a-z0-9-]{0,63}\z");
        private static readonly Regex Accent = new Regex(@"\A#[0-9a-fA-F]{6}\z");
        private static readonly DuelSceneDefinition BuiltInClassic = new DuelSceneDefinition(
            DefaultId, "经典场地", "熟悉的场地与天空，延续当前对局体验。", "duel-scenes/classic.jpg",
            "duel-scenes/classic-overview.jpg", "自然场地 · 经典体验", new Color(.77f, .70f, .51f), DefaultId, "");
        private static readonly HashSet<string> resolutionWarnings = new HashSet<string>();
        private static ReadOnlyCollection<DuelSceneDefinition> definitions;
        private static string loadedPath;

        public static IReadOnlyList<DuelSceneDefinition> Definitions
        {
            get { EnsureLoaded(); return definitions; }
        }

        public static DuelSceneDefinition Find(string id)
        {
            EnsureLoaded();
            foreach (var definition in definitions)
                if (string.Equals(definition.Id, id, StringComparison.Ordinal)) return definition;
            return null;
        }

        public static DuelSceneDefinition Resolve(string id)
        {
            var definition = Find(id);
            if (IsAvailable(definition)) return definition;
            string reason = definition == null ? "unknown identifier"
                : !DuelSceneGeometry.Supports(definition.Builder) ? "unavailable builder"
                : "missing scene texture";
            if (resolutionWarnings.Add(reason))
                Debug.LogWarning("[FightClient] duel scene unavailable (" + reason + "); using classic.");
            return Find(DefaultId) ?? BuiltInClassic;
        }

        public static bool IsAvailable(DuelSceneDefinition definition)
        {
            if (definition == null) return false;
            if (definition.Builder == DefaultId) return true;
            if (!DuelSceneGeometry.Supports(definition.Builder) || !File.Exists(ContentFile(definition.TexturePath))) return false;
            if (IsIntegratedTheme(definition.Builder))
            {
                foreach (string part in new[] { "controls-fine", "court-v3", "overview", "field-materials-v4", "opening-parts-v2", "opening-order-v3" })
                    if (!File.Exists(ContentFile("duel-scenes/" + definition.Builder + "-" + part + ".png"))) return false;
                if (!File.Exists(ContentFile("duel-scenes/" + definition.Builder + "-fixture-layout-v3.json"))) return false;
            }
            return true;
        }

        internal static string ContentFile(string relative)
        {
            if (!ValidContentPath(relative)) throw new InvalidDataException("Invalid duel scene content path.");
            return Path.Combine(Runtime.Program.PATH_DATA, "ClientUI", relative.Replace('/', Path.DirectorySeparatorChar));
        }

        private static bool ValidContentPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path) || path.Contains("\\") || path.Contains(":")) return false;
            foreach (string part in path.Split('/'))
                if (part.Length == 0 || part == "." || part == "..") return false;
            return true;
        }

        private static void EnsureLoaded()
        {
            string path = Path.GetFullPath(Path.Combine(Runtime.Program.PATH_DATA, "ClientUI", "duel-scenes", "catalog.json"));
            if (definitions != null && loadedPath == path) return;
            loadedPath = path; resolutionWarnings.Clear();
            try
            {
                var catalog = JsonConvert.DeserializeObject<CatalogData>(File.ReadAllText(path));
                if (catalog == null || catalog.Schema != 2 || catalog.Scenes == null || catalog.Scenes.Count == 0)
                    throw new InvalidDataException("Unsupported duel scene catalog.");
                var result = new List<DuelSceneDefinition>();
                var identifiers = new HashSet<string>(StringComparer.Ordinal);
                foreach (var scene in catalog.Scenes)
                {
                    if (scene == null || scene.Id == null || !Identifier.IsMatch(scene.Id) || !identifiers.Add(scene.Id)
                        || string.IsNullOrWhiteSpace(scene.Name) || string.IsNullOrWhiteSpace(scene.Description)
                        || string.IsNullOrWhiteSpace(scene.Style) || !ValidContentPath(scene.OverviewPath)
                        || scene.Accent == null || !Accent.IsMatch(scene.Accent)
                        || !ValidContentPath(scene.PreviewPath) || scene.Builder == null || !Identifier.IsMatch(scene.Builder)
                        || (scene.Builder != DefaultId && !ValidContentPath(scene.TexturePath))
                        || (scene.Id == DefaultId && scene.Builder != DefaultId))
                        throw new InvalidDataException("Invalid duel scene definition.");
                    ColorUtility.TryParseHtmlString(scene.Accent, out var accent);
                    result.Add(new DuelSceneDefinition(scene.Id, scene.Name, scene.Description, scene.PreviewPath,
                        scene.OverviewPath, scene.Style, accent, scene.Builder, scene.TexturePath ?? ""));
                }
                if (!identifiers.Contains(DefaultId)) throw new InvalidDataException("Missing classic duel scene.");
                definitions = result.AsReadOnly();
            }
            catch (Exception error)
            {
                definitions = new List<DuelSceneDefinition> { BuiltInClassic }.AsReadOnly();
                Debug.LogWarning("[FightClient] duel scene catalog unavailable (" + error.GetType().Name + "); using built-in classic.");
            }
        }

        private sealed class CatalogData
        {
            [JsonProperty("schema")] public int Schema { get; set; }
            [JsonProperty("scenes")] public List<SceneData> Scenes { get; set; }
        }

        private sealed class SceneData
        {
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("description")] public string Description { get; set; }
            [JsonProperty("preview_path")] public string PreviewPath { get; set; }
            [JsonProperty("overview_path")] public string OverviewPath { get; set; }
            [JsonProperty("style")] public string Style { get; set; }
            [JsonProperty("accent")] public string Accent { get; set; }
            [JsonProperty("builder")] public string Builder { get; set; }
            [JsonProperty("texture_path")] public string TexturePath { get; set; }
        }
    }
}
