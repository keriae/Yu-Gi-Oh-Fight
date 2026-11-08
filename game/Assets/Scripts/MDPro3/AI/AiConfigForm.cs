using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MDPro3.AI
{
    /// <summary>One editable request header.</summary>
    public sealed class AiConfigField
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>Maps forms to the existing private configuration storage.</summary>
    public static class AiConfigForm
    {
        private const int MaximumDepth = 32;
        private static readonly Regex NumberPattern = new Regex(@"^-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?$", RegexOptions.CultureInvariant);

        private sealed class FormJsonReader : JsonTextReader
        {
            private readonly string source;
            private readonly List<int> lineOffsets = new List<int> { 0 };
            public readonly List<string> NumericLiterals = new List<string>();

            public FormJsonReader(string json) : base(new StringReader(json))
            {
                source = json;
                for (var i = 0; i < json.Length; i++)
                {
                    if (json[i] == '\r')
                    {
                        if (i + 1 < json.Length && json[i + 1] == '\n') i++;
                        lineOffsets.Add(i + 1);
                    }
                    else if (json[i] == '\n') lineOffsets.Add(i + 1);
                }
                MaxDepth = MaximumDepth;
                DateParseHandling = DateParseHandling.None;
            }

            public override bool Read()
            {
                var read = base.Read();
                if (read && (TokenType == JsonToken.Integer || TokenType == JsonToken.Float))
                {
                    var end = lineOffsets[LineNumber - 1] + LinePosition;
                    var start = end;
                    while (start > 0 && "0123456789+-.eE".IndexOf(source[start - 1]) >= 0) start--;
                    var literal = source.Substring(start, end - start);
                    if (!NumberPattern.IsMatch(literal)
                        || (start > 0 && (char.IsLetterOrDigit(source[start - 1]) || source[start - 1] == '_')))
                        throw new JsonReaderException();
                    // Preserve the input spelling as a numeric value rather than converting
                    // decimal digits to a binary floating point value during storage loading.
                    NumericLiterals.Add(literal);
                }
                return read;
            }
        }

        internal static JObject ParseObject(string json, string label)
        {
            if (string.IsNullOrWhiteSpace(json)) return new JObject();
            if (json.Length > 65536) throw new AiConfigurationException(label + " 不能超过 64 KB。");
            try
            {
                using (var reader = new FormJsonReader(json))
                {
                    var result = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                    if (reader.Read()) throw new JsonReaderException();
                    if (result.Descendants().Any(token => token.Type == JTokenType.Undefined || token.Type == JTokenType.Constructor))
                        throw new JsonReaderException();
                    var values = result.Descendants().Where(token => token.Type == JTokenType.Integer || token.Type == JTokenType.Float).ToList();
                    if (values.Count != reader.NumericLiterals.Count) throw new JsonReaderException();
                    for (var i = 0; i < values.Count; i++) values[i].Replace(new JRaw(reader.NumericLiterals[i]));
                    return result;
                }
            }
            catch (Exception error) when (error is JsonException || error is ArgumentException)
            {
                throw new AiConfigurationException(label + " 的配置格式无效，请重新填写。");
            }
        }

        public static List<AiConfigField> ReadHeaders(string json)
        {
            var headers = new ResponsesAiConfig { ExtraHeadersJson = json }.Headers();
            return headers.Properties().Select(p => new AiConfigField { Name = p.Name, Value = (string)p.Value }).ToList();
        }

        public static string WriteHeaders(IEnumerable<AiConfigField> fields)
        {
            var result = new JObject();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in fields ?? Enumerable.Empty<AiConfigField>())
            {
                RequireField(field);
                if (IsEmptyRow(field)) continue;
                if (string.IsNullOrWhiteSpace(field.Name) || !seen.Add(field.Name))
                    throw new AiConfigurationException("请填写不重复的请求头名称和文本内容。");
                result.Add(field.Name, field.Value ?? string.Empty);
            }
            var json = result.ToString(Formatting.None);
            new ResponsesAiConfig { ExtraHeadersJson = json }.Headers();
            return json;
        }

        private static bool IsEmptyRow(AiConfigField field)
            => string.IsNullOrEmpty(field.Name) && string.IsNullOrEmpty(field.Value);

        private static void RequireField(AiConfigField field)
        {
            if (field == null) throw new AiConfigurationException("配置中有无效的表单行，请删除后重试。");
        }
    }
}
