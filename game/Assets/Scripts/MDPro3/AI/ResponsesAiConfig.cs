using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MDPro3.AI
{
    public sealed class AiConfigurationException : Exception
    {
        public AiConfigurationException(string message) : base(message) { }
    }

    /// <summary>Independent of Unity scenes; credentials never enter config.conf.</summary>
    public sealed class ResponsesAiConfig
    {
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";
        [JsonIgnore] public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-6.1-sol";
        [JsonIgnore] public string ExtraHeadersJson { get; set; } = "{}";
        [JsonIgnore] public string ExtraBodyJson { get; set; } = "{}";
        public int TimeoutSeconds { get; set; } = 90;
        public int MaxOutputTokens { get; set; } = 4096;
        public int PromptBudgetTokens { get; set; } = 200000;
        public string ReasoningEffort { get; set; } = "low";
        public bool RememberApiKey { get; set; }
        public string DeckA { get; set; } = string.Empty;
        public string DeckB { get; set; } = string.Empty;
        public bool AiVsAi { get; set; }
        public string ProfileA { get; set; } = string.Empty;
        public string ProfileB { get; set; } = string.Empty;

        public ResponsesAiConfig Clone() => (ResponsesAiConfig)MemberwiseClone();

        public void Validate()
        {
            Endpoint();
            if (string.IsNullOrWhiteSpace(Model) || Model.Length > 256 || Model.Any(char.IsControl))
                throw new AiConfigurationException("请填写有效的模型名称。");
            if (TimeoutSeconds < 5 || TimeoutSeconds > 600)
                throw new AiConfigurationException("请求超时应在 5～600 秒之间。");
            if (MaxOutputTokens < 128 || MaxOutputTokens > 128000)
                throw new AiConfigurationException("输出上限应在 128～128000 之间。");
            if (PromptBudgetTokens < 8192 || PromptBudgetTokens > 2000000)
                throw new AiConfigurationException("上下文上限应在 8192～2000000 tokens 之间。");
            if (!string.IsNullOrEmpty(ReasoningEffort) && !new[] { "none", "minimal", "low", "medium", "high", "xhigh", "max" }.Contains(ReasoningEffort))
                throw new AiConfigurationException("思考强度无效，请重新选择。");
            var headers = Headers();
            if (string.IsNullOrWhiteSpace(ApiKey) && !headers.Properties().Any(p => p.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase)))
                throw new AiConfigurationException("请填写密钥，或添加 Authorization 请求头。");
            if (ApiKey != null && (ApiKey.Length > 16384 || ApiKey.Any(char.IsControl)))
                throw new AiConfigurationException("密钥格式无效。");
            var body = Body();
            // Advanced parameters replace the normal output limit in the actual request.
            // Validate that effective value before saving settings or starting a duel.
            int outputTokens = ResponsesPromptBudget.ReservedOutput(new JObject
            {
                ["max_output_tokens"] = body["max_output_tokens"]?.DeepClone() ?? new JValue(MaxOutputTokens)
            });
            if (PromptBudgetTokens <= outputTokens + ResponsesPromptBudget.EnvelopeReserveTokens)
                throw new AiConfigurationException("上下文上限需要大于实际输出上限并预留至少 1024 tokens。");
        }

        public Uri Endpoint()
        {
            if (!Uri.TryCreate(BaseUrl?.Trim(), UriKind.Absolute, out var uri)
                || (uri.Scheme != "https" && uri.Scheme != "http")
                || string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo)
                || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
                throw new AiConfigurationException("请填写有效的服务地址，地址不能包含用户名、查询参数或片段。");
            var path = uri.AbsolutePath.TrimEnd('/');
            if (!path.EndsWith("/responses", StringComparison.OrdinalIgnoreCase))
                path = (path.Length == 0 ? "/v1" : path) + "/responses";
            var builder = new UriBuilder(uri) { Path = path };
            return builder.Uri;
        }

        internal JObject Headers()
        {
            var headers = ParseObject(ExtraHeadersJson, "额外请求头");
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers.Properties())
            {
                if (!seen.Add(header.Name) || !Regex.IsMatch(header.Name, "^[!#$%&'*+.^_`|~0-9A-Za-z-]+$")
                    || header.Value.Type != JTokenType.String
                    || ((string)header.Value).Any(c => c == '\r' || c == '\n' || char.IsControl(c)))
                    throw new AiConfigurationException("请求头名称不能重复，名称和内容不能含换行。");
                if (new[] { "Host", "Content-Length", "Transfer-Encoding", "Connection", "Expect", "Range", "Date", "Trailer" }
                    .Contains(header.Name, StringComparer.OrdinalIgnoreCase))
                    throw new AiConfigurationException("该请求头由连接程序管理，请删除后重试。");
            }
            return headers;
        }

        internal JObject Body()
        {
            var body = ParseObject(ExtraBodyJson, "额外参数");
            foreach (var reserved in new[] { "model", "input", "instructions", "text", "stream", "store", "tools", "tool_choice", "previous_response_id", "conversation", "background" })
                if (body.Property(reserved, StringComparison.OrdinalIgnoreCase) != null)
                    throw new AiConfigurationException("额外参数不能覆盖模型、对战输入、输出格式或会话设置。");
            return body;
        }

        private static JObject ParseObject(string json, string label)
            => AiConfigForm.ParseObject(json, label);
    }

    public static class ResponsesAiConfigStore
    {
        public const string SettingsPath = "Data/LLMAI/settings.json";
        public const string CredentialsPath = "Data/LLMAI/credentials.json";
        private static readonly object Gate = new object();
        private static ResponsesAiConfig session;

        public static ResponsesAiConfig Load()
        {
            lock (Gate)
            {
                if (session != null) return session.Clone();
                try
                {
                    var config = File.Exists(SettingsPath)
                        ? JsonConvert.DeserializeObject<ResponsesAiConfig>(File.ReadAllText(SettingsPath)) ?? new ResponsesAiConfig()
                        : new ResponsesAiConfig();
                    if (config.RememberApiKey && File.Exists(CredentialsPath))
                    {
                        var secret = JObject.Parse(File.ReadAllText(CredentialsPath));
                        config.ApiKey = (string)secret["api_key"] ?? string.Empty;
                        config.ExtraHeadersJson = (string)secret["extra_headers"] ?? "{}";
                        config.ExtraBodyJson = (string)secret["extra_body"] ?? "{}";
                        Restrict(CredentialsPath);
                    }
                    session = config;
                    return config.Clone();
                }
                catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is JsonException || error is ArgumentException)
                {
                    throw new AiConfigurationException("无法读取 AI 配置，请检查 Data/LLMAI 目录。");
                }
            }
        }

        /// <summary>Commit a service draft without overwriting a newer duel selection.</summary>
        public static void SaveService(ResponsesAiConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            lock (Gate)
            {
                var latest = Load();
                var merged = config.Clone();
                merged.DeckA = latest.DeckA;
                merged.DeckB = latest.DeckB;
                merged.AiVsAi = latest.AiVsAi;
                merged.ProfileA = latest.ProfileA;
                merged.ProfileB = latest.ProfileB;
                Save(merged);
            }
        }

        /// <summary>Persist only duel choices, including while the service is unconfigured.</summary>
        public static void SaveChoices(string deckA, string deckB, bool aiVsAi)
            => SaveChoices(deckA, deckB, aiVsAi, null, null);

        public static void SaveChoices(string deckA, string deckB, bool aiVsAi, string profileA, string profileB)
        {
            lock (Gate)
            {
                var latest = Load();
                latest.DeckA = deckA ?? string.Empty;
                latest.DeckB = deckB ?? string.Empty;
                latest.AiVsAi = aiVsAi;
                if (profileA != null) latest.ProfileA = profileA;
                if (profileB != null) latest.ProfileB = profileB;
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                    RestrictDirectory(Path.GetDirectoryName(SettingsPath));
                    AtomicWrite(SettingsPath, JsonConvert.SerializeObject(latest, Formatting.Indented), false);
                    // A deck selection cannot delete, replace or newly remember credentials.
                    session = latest.Clone();
                }
                catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
                {
                    throw new AiConfigurationException("无法保存对战选择，请检查 Data/LLMAI 的写入权限。");
                }
            }
        }

        public static void Save(ResponsesAiConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            config.Validate();
            lock (Gate)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                    RestrictDirectory(Path.GetDirectoryName(SettingsPath));
                    if (config.RememberApiKey)
                    {
                        var secret = new JObject { ["api_key"] = config.ApiKey, ["extra_headers"] = config.ExtraHeadersJson, ["extra_body"] = config.ExtraBodyJson };
                        AtomicWrite(CredentialsPath, secret.ToString(Formatting.Indented), true);
                    }
                    else if (File.Exists(CredentialsPath)) File.Delete(CredentialsPath);
                    AtomicWrite(SettingsPath, JsonConvert.SerializeObject(config, Formatting.Indented), false);
                    session = config.Clone();
                }
                catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
                {
                    throw new AiConfigurationException("无法保存 AI 配置，请检查 Data/LLMAI 的写入权限。");
                }
            }
        }

        internal static void AtomicWrite(string path, string value, bool secret)
        {
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    if (secret) Restrict(temporary);
                    var bytes = new UTF8Encoding(false).GetBytes(value);
                    stream.Write(bytes, 0, bytes.Length);
                }
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
                if (secret) Restrict(path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        [DllImport("libc", SetLastError = true)] private static extern int chmod(string path, uint mode);
        private static bool Unix => Path.DirectorySeparatorChar == '/';
        internal static void Restrict(string path)
        {
            if (Unix && chmod(Path.GetFullPath(path), 384) != 0) throw new IOException("Cannot restrict credentials");
        }
        internal static void RestrictDirectory(string path)
        {
            if (Unix && chmod(Path.GetFullPath(path), 448) != 0) throw new IOException("Cannot restrict credentials directory");
        }
    }
}
