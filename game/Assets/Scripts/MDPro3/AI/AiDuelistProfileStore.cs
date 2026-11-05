using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MDPro3.AI
{
    /// <summary>A reusable duelist. Request secrets are serialized only in its private credential file.</summary>
    public sealed class AiDuelistProfile
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = "新的 AI 分身";
        public ResponsesAiConfig Config { get; set; } = new ResponsesAiConfig();
        public Dictionary<string, string> Appearance { get; set; } = NewAppearance();
        [JsonProperty] internal string CredentialRevision { get; set; } = string.Empty;

        public AiDuelistProfile Clone()
        {
            var result = (AiDuelistProfile)MemberwiseClone();
            result.Config = Config?.Clone() ?? new ResponsesAiConfig();
            result.Appearance = Appearance == null ? NewAppearance() : new Dictionary<string, string>(Appearance);
            return result;
        }

        internal static Dictionary<string, string> NewAppearance() => new Dictionary<string, string>
        {
            ["Face"] = "1010001", ["Frame"] = "1030001", ["Protector"] = "1070001",
            ["Field"] = "1090001", ["Mate"] = "1000001", ["Character"] = "0001",
            ["Grave"] = "1100001", ["Stand"] = "1110001", ["Case"] = "1080001"
        };
    }

    /// <summary>Lazy legacy migration, independent draft snapshots and per-duelist credential lifetimes.</summary>
    public static class AiDuelistProfileStore
    {
        public const string ProfilesPath = "Data/LLMAI/profiles.json";
        public const string CredentialsDirectory = "Data/LLMAI/profile-credentials";
        public const string LegacyProfileId = "legacy-default";
        private static readonly object Gate = new object();
        private static List<AiDuelistProfile> session;
        private sealed class Document
        {
            public int Version { get; set; } = 1;
            public List<AiDuelistProfile> Profiles { get; set; } = new List<AiDuelistProfile>();
        }

        public static List<AiDuelistProfile> LoadAll()
        {
            lock (Gate)
            {
                EnsureLoaded();
                return session.Select(profile => profile.Clone()).ToList();
            }
        }

        public static AiDuelistProfile Get(string id)
        {
            lock (Gate)
            {
                EnsureLoaded();
                return session.FirstOrDefault(profile => profile.Id == id)?.Clone();
            }
        }

        public static AiDuelistProfile Create(string name = "新的 AI 分身")
        {
            lock (Gate)
            {
                EnsureLoaded();
                var profile = new AiDuelistProfile { Id = Guid.NewGuid().ToString("N"), Name = UniqueName(name) };
                Save(profile);
                return profile.Clone();
            }
        }

        public static AiDuelistProfile Duplicate(string id)
        {
            lock (Gate)
            {
                var source = Get(id);
                if (source == null) throw new AiConfigurationException("这个 AI 分身已不存在，请重新选择。");
                source.Id = Guid.NewGuid().ToString("N");
                source.Name = UniqueName(source.Name.Substring(0, Math.Min(source.Name.Length, 55)) + " 副本");
                source.CredentialRevision = string.Empty;
                Save(source);
                return source.Clone();
            }
        }

        /// <summary>An incomplete service may be saved while building a duelist; Start validates actual requests.</summary>
        public static void Save(AiDuelistProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            lock (Gate)
            {
                EnsureLoaded();
                var next = profile.Clone();
                ValidateProfile(next);
                var profiles = session.Select(value => value.Clone()).ToList();
                int index = profiles.FindIndex(value => value.Id == next.Id);
                if (index >= 0)
                {
                    // A stale editor cannot redirect credentials to another revision or another profile.
                    next.CredentialRevision = profiles[index].CredentialRevision;
                    profiles[index] = next;
                }
                else
                {
                    if (profiles.Count >= 128) throw new AiConfigurationException("最多可保存 128 个 AI 分身。");
                    next.CredentialRevision = string.Empty;
                    profiles.Add(next);
                }
                Commit(profiles, next.Id);
            }
        }

        public static void Delete(string id)
        {
            lock (Gate)
            {
                EnsureLoaded();
                var profiles = session.Where(value => value.Id != id).Select(value => value.Clone()).ToList();
                if (profiles.Count == session.Count) return;
                Commit(profiles, null);
            }
        }

        private static string UniqueName(string name)
        {
            name = (name ?? string.Empty).Trim();
            if (name.Length == 0) name = "新的 AI 分身";
            if (!session.Any(profile => profile.Name == name)) return name;
            var stem = name.Substring(0, Math.Min(name.Length, 55));
            for (int i = 2; ; i++)
            {
                string candidate = stem + "（" + i + "）";
                if (!session.Any(profile => profile.Name == candidate)) return candidate;
            }
        }

        private static void EnsureLoaded()
        {
            if (session != null) return;
            try
            {
                if (!File.Exists(ProfilesPath))
                {
                    // Do not alter legacy files or write anything merely by opening Settings.
                    session = new List<AiDuelistProfile>
                    {
                        new AiDuelistProfile { Id = LegacyProfileId, Name = "默认 AI 分身", Config = ResponsesAiConfigStore.Load() }
                    };
                    return;
                }
                var document = JsonConvert.DeserializeObject<Document>(File.ReadAllText(ProfilesPath));
                if (document == null || document.Version != 1 || document.Profiles == null || document.Profiles.Count > 128)
                    throw new JsonException();
                var profiles = document.Profiles;
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var profile in profiles)
                {
                    if (profile == null || !ids.Add(profile.Id)) throw new JsonException();
                    ValidateProfile(profile);
                    if (profile.Config.RememberApiKey && !string.IsNullOrEmpty(profile.CredentialRevision))
                    {
                        string path = CredentialPath(profile.Id, profile.CredentialRevision);
                        var secret = JObject.Parse(File.ReadAllText(path));
                        profile.Config.ApiKey = (string)secret["api_key"] ?? string.Empty;
                        profile.Config.ExtraHeadersJson = (string)secret["extra_headers"] ?? "{}";
                        profile.Config.ExtraBodyJson = (string)secret["extra_body"] ?? "{}";
                        ResponsesAiConfigStore.Restrict(path);
                        profile.Config.Headers(); profile.Config.Body();
                    }
                }
                session = profiles;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is JsonException || error is ArgumentException)
            {
                throw new AiConfigurationException("无法读取 AI 分身，请检查 Data/LLMAI 目录。");
            }
        }

        private static void ValidateProfile(AiDuelistProfile profile)
        {
            if (profile.Id != LegacyProfileId && !Regex.IsMatch(profile.Id ?? "", "^[a-f0-9]{32}$"))
                throw new AiConfigurationException("AI 分身标识无效，请重新打开设置。");
            profile.Name = (profile.Name ?? string.Empty).Trim();
            if (profile.Name.Length == 0 || profile.Name.Length > 64 || profile.Name.Any(char.IsControl))
                throw new AiConfigurationException("请填写 1～64 个字符的分身名称。");
            if (profile.Config == null) throw new AiConfigurationException("AI 分身配置无效。");
            profile.Config.Headers(); profile.Config.Body();
            var defaults = AiDuelistProfile.NewAppearance();
            if (profile.Appearance == null) profile.Appearance = defaults;
            foreach (var key in defaults.Keys)
            {
                if (!profile.Appearance.TryGetValue(key, out string raw)) profile.Appearance[key] = defaults[key];
                else if (raw == null || raw.Length > 16 || !int.TryParse(raw, out int code) || code < 0)
                    throw new AiConfigurationException("AI 分身的外观编号无效，请重新选择。");
            }
            foreach (string key in profile.Appearance.Keys.Where(key => !defaults.ContainsKey(key)).ToArray())
                profile.Appearance.Remove(key);
            if (!string.IsNullOrEmpty(profile.CredentialRevision) && !Regex.IsMatch(profile.CredentialRevision, "^[a-f0-9]{32}$"))
                throw new AiConfigurationException("AI 分身凭据索引无效。");
        }

        private static string CredentialPath(string id, string revision)
        {
            if ((id != LegacyProfileId && !Regex.IsMatch(id ?? "", "^[a-f0-9]{32}$"))
                || !Regex.IsMatch(revision ?? "", "^[a-f0-9]{32}$")) throw new JsonException();
            return Path.Combine(CredentialsDirectory, id + "-" + revision + ".json");
        }

        private static void Commit(List<AiDuelistProfile> profiles, string changedId)
        {
            bool migrating = !File.Exists(ProfilesPath);
            var created = new List<string>();
            var oldPaths = session.Where(profile => !string.IsNullOrEmpty(profile.CredentialRevision))
                .Select(profile => CredentialPath(profile.Id, profile.CredentialRevision)).ToArray();
            try
            {
                string directory = Path.GetDirectoryName(ProfilesPath);
                Directory.CreateDirectory(directory); ResponsesAiConfigStore.RestrictDirectory(directory);
                foreach (var profile in profiles)
                {
                    if (!profile.Config.RememberApiKey) { profile.CredentialRevision = string.Empty; continue; }
                    if (profile.Id != changedId && !string.IsNullOrEmpty(profile.CredentialRevision)) continue;
                    Directory.CreateDirectory(CredentialsDirectory); ResponsesAiConfigStore.RestrictDirectory(CredentialsDirectory);
                    profile.CredentialRevision = Guid.NewGuid().ToString("N");
                    string path = CredentialPath(profile.Id, profile.CredentialRevision);
                    var secret = new JObject { ["api_key"] = profile.Config.ApiKey,
                        ["extra_headers"] = profile.Config.ExtraHeadersJson, ["extra_body"] = profile.Config.ExtraBodyJson };
                    ResponsesAiConfigStore.AtomicWrite(path, secret.ToString(Formatting.Indented), true);
                    created.Add(path);
                }
                // Publish one manifest only after all newly referenced secrets exist.
                ResponsesAiConfigStore.AtomicWrite(ProfilesPath,
                    JsonConvert.SerializeObject(new Document { Profiles = profiles }, Formatting.Indented), false);
                session = profiles.Select(profile => profile.Clone()).ToList();
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is JsonException)
            {
                foreach (var path in created) TryDelete(path);
                throw new AiConfigurationException("无法保存 AI 分身，请检查 Data/LLMAI 的写入权限。");
            }
            var kept = new HashSet<string>(profiles.Where(profile => !string.IsNullOrEmpty(profile.CredentialRevision))
                .Select(profile => CredentialPath(profile.Id, profile.CredentialRevision)), StringComparer.Ordinal);
            foreach (var path in oldPaths.Where(path => !kept.Contains(path))) TryDelete(path);
            // The new manifest now owns credential lifetimes. Retire the old shared secret
            // only after publishing a complete migrated copy, so disabling remembrance works.
            if (migrating) TryDelete(ResponsesAiConfigStore.CredentialsPath);
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
