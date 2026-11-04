using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections.Concurrent;
using UnityEngine.Networking;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MDPro3.AI
{
    public sealed class AiRequestException : Exception
    {
        public int? HttpStatusCode { get; }
        public AiRequestException(string message, int? status = null) : base(message) { HttpStatusCode = status; }
    }

    /// <summary>Responses protocol, without dependencies absent from shipping Unity Mono.</summary>
    public sealed class ResponsesAiClient : IDisposable
    {
        private sealed class PendingRequest
        {
            public ResponsesAiClient Owner;
            public string Json;
            public CancellationToken Token;
            public CancellationTokenRegistration Registration;
            public DateTime Deadline;
            public volatile bool Cancelled;
            public readonly TaskCompletionSource<string> Completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            public UnityWebRequest Request;
        }

        private readonly ResponsesAiConfig config;
        private readonly object gate = new object();
        private readonly HashSet<PendingRequest> active = new HashSet<PendingRequest>();
        private bool disposed;
        private static readonly ConcurrentQueue<PendingRequest> Queue = new ConcurrentQueue<PendingRequest>();
        private static readonly object DispatchGate = new object();
        private static readonly List<PendingRequest> Running = new List<PendingRequest>();
        private static volatile bool shuttingDown;
        private const ulong MaxResponseBytes = 2 * 1024 * 1024;
        public static int InFlightCount => Running.Count;

        public ResponsesAiClient(ResponsesAiConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            config.Validate();
            this.config = config.Clone();
        }

        public string Request(string instructions, string input, JObject schema, CancellationToken token)
            => RequestAsync(instructions, input, schema, token).GetAwaiter().GetResult();

        // Creation, cancellation and completion can originate on bot workers.
        // Unity networking is started and disposed only by the main-thread pump.
        public Task<string> RequestAsync(string instructions, string input, JObject schema, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (shuttingDown) throw new AiRequestException("游戏已关闭，AI 请求已停止。");
            var pending = new PendingRequest
            {
                Owner = this, Json = BuildRequest(instructions, input, schema).ToString(Formatting.None),
                Token = token, Deadline = DateTime.UtcNow.AddSeconds(config.TimeoutSeconds)
            };
            lock (gate)
            {
                if (disposed) throw new ObjectDisposedException(nameof(ResponsesAiClient));
                active.Add(pending);
            }
            pending.Registration = token.Register(() => Cancel(pending));
            lock (DispatchGate)
            {
                if (shuttingDown) { Cancel(pending); Release(pending); }
                else Queue.Enqueue(pending);
            }
            return pending.Completion.Task;
        }

        /// <summary>Call once per frame from Program.Update, before duel session work.</summary>
        public static void Pump()
        {
            while (Queue.TryDequeue(out var pending))
            {
                if (shuttingDown || pending.Cancelled || pending.Completion.Task.IsCompleted)
                { Release(pending); continue; }
                if (DateTime.UtcNow >= pending.Deadline)
                {
                    pending.Completion.TrySetException(new AiRequestException("AI 请求超时，请调整超时设置或检查服务。"));
                    Release(pending); continue;
                }
                try
                {
                    var config = pending.Owner.config;
                    pending.Request = UnityWebRequest.Post(config.Endpoint().AbsoluteUri, pending.Json, "application/json");
                    pending.Request.timeout = config.TimeoutSeconds;
                    // Credentials must not be forwarded to a different redirect destination.
                    pending.Request.redirectLimit = 0;
                    pending.Request.SetRequestHeader("Accept", "application/json");
                    if (!string.IsNullOrWhiteSpace(config.ApiKey))
                        pending.Request.SetRequestHeader("Authorization", "Bearer " + config.ApiKey.Trim());
                    foreach (var header in config.Headers().Properties())
                        pending.Request.SetRequestHeader(header.Name, (string)header.Value);
                    pending.Request.SendWebRequest();
                    Running.Add(pending);
                }
                catch
                {
                    // Unity native failures can include request details; expose a static message.
                    pending.Completion.TrySetException(new AiRequestException("无法发送 AI 请求，请检查服务地址和额外请求头。"));
                    Release(pending);
                }
            }
            for (var i = Running.Count - 1; i >= 0; i--)
            {
                var pending = Running[i];
                var request = pending.Request;
                bool done;
                try
                {
                    if (shuttingDown || pending.Cancelled || pending.Token.IsCancellationRequested)
                    { Cancel(pending); done = true; }
                    else if (DateTime.UtcNow >= pending.Deadline)
                    {
                        pending.Completion.TrySetException(new AiRequestException("AI 请求超时，请调整超时设置或检查服务。"));
                        done = true;
                    }
                    else if (request.downloadedBytes > MaxResponseBytes)
                    {
                        pending.Completion.TrySetException(new AiRequestException("AI 响应超过大小限制。"));
                        done = true;
                    }
                    else if (!request.isDone) continue;
                    else
                    {
                        done = true;
                        var status = (int)request.responseCode;
                        if (status >= 300)
                            pending.Completion.TrySetException(new AiRequestException(StatusMessage(status), status));
                        else if (request.result != UnityWebRequest.Result.Success)
                            pending.Completion.TrySetException(new AiRequestException("无法连接 AI 服务，请检查服务地址、网络和证书。"));
                        else
                        {
                            var data = request.downloadHandler.data;
                            if (data == null || (ulong)data.LongLength > MaxResponseBytes)
                                pending.Completion.TrySetException(new AiRequestException("AI 服务返回空响应或超过大小限制。"));
                            else
                            {
                                var text = new UTF8Encoding(false, true).GetString(data);
                                pending.Completion.TrySetResult(ParseResponse(text));
                            }
                        }
                    }
                }
                catch (AiRequestException error) { pending.Completion.TrySetException(error); done = true; }
                catch { pending.Completion.TrySetException(new AiRequestException("AI 响应处理失败，请检查 AI 服务。")); done = true; }
                if (done) { Running.RemoveAt(i); Release(pending); }
            }
        }

        private static void Cancel(PendingRequest pending)
        {
            pending.Cancelled = true;
            pending.Completion.TrySetCanceled(pending.Token);
        }

        private static void Release(PendingRequest pending)
        {
            if (pending.Request != null)
            {
                try { if (!pending.Request.isDone) pending.Request.Abort(); }
                catch { }
                try { pending.Request.Dispose(); }
                catch { }
                pending.Request = null;
            }
            pending.Registration.Dispose();
            pending.Json = null;
            lock (pending.Owner.gate) pending.Owner.active.Remove(pending);
        }

        /// <summary>Call on the main thread during application shutdown.</summary>
        public static void Shutdown()
        {
            lock (DispatchGate)
            {
                shuttingDown = true;
                while (Queue.TryDequeue(out var queued)) { Cancel(queued); Release(queued); }
            }
            foreach (var pending in Running) { Cancel(pending); Release(pending); }
            Running.Clear();
        }

        public JObject BuildRequest(string instructions, string input, JObject schema)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            var body = new JObject
            {
                ["model"] = config.Model.Trim(),
                ["instructions"] = instructions ?? string.Empty,
                ["input"] = input ?? string.Empty,
                ["max_output_tokens"] = config.MaxOutputTokens
            };
            if (!string.IsNullOrWhiteSpace(config.ReasoningEffort))
                body["reasoning"] = new JObject { ["effort"] = config.ReasoningEffort };
            body.Merge(config.Body(), new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace, MergeNullValueHandling = MergeNullValueHandling.Merge });
            body["store"] = false;
            body["stream"] = false;
            body["text"] = new JObject
            {
                ["format"] = new JObject
                {
                    ["type"] = "json_schema",
                    ["name"] = "duel_decision",
                    ["strict"] = true,
                    ["schema"] = schema.DeepClone()
                }
            };
            ResponsesPromptBudget.Apply(body, config.PromptBudgetTokens);
            return body;
        }

        public static string ParseResponse(string json)
        {
            JObject response;
            try
            {
                using (var reader = new JsonTextReader(new StringReader(json)) { MaxDepth = 64, DateParseHandling = DateParseHandling.None })
                {
                    response = JObject.Load(reader);
                    if (reader.Read()) throw new JsonReaderException();
                }
            }
            catch (Exception error) when (error is JsonException || error is ArgumentException)
            { throw new AiRequestException("AI 服务返回的内容无效。"); }
            var status = StringValue(response["status"]);
            if (status == "incomplete") throw new AiRequestException("AI 输出未完成，请提高输出上限或降低思考强度。");
            if (status != "completed") throw new AiRequestException("AI 服务未完成请求，请检查模型和服务配置。");
            var output = new StringBuilder();
            if (response["output"] is JArray items)
            {
                foreach (var item in items.OfType<JObject>())
                {
                    if (StringValue(item["type"]) != "message" || !(item["content"] is JArray content)) continue;
                    foreach (var part in content.OfType<JObject>())
                    {
                        if (StringValue(part["type"]) == "refusal") throw new AiRequestException("AI 拒绝了本次对战决策。");
                        if (StringValue(part["type"]) == "output_text" && part["text"]?.Type == JTokenType.String) output.Append((string)part["text"]);
                    }
                }
            }
            // Some compatible gateways also provide the SDK convenience field.
            if (output.Length == 0 && response["output_text"]?.Type == JTokenType.String) output.Append((string)response["output_text"]);
            if (output.Length == 0) throw new AiRequestException("AI 响应中没有决策内容。");
            if (output.Length > 262144) throw new AiRequestException("AI 决策内容超过大小限制。");
            return output.ToString();
        }

        private static string StringValue(JToken value) => value?.Type == JTokenType.String ? (string)value : null;

        private static string StatusMessage(int status)
        {
            if (status == 401 || status == 403) return "AI 服务鉴权失败（HTTP " + status + "），请检查密钥和额外请求头。";
            if (status == 404) return "AI 接口不存在（HTTP 404），请检查服务地址和模型名称。";
            if (status == 429) return "AI 服务限流或额度不足（HTTP 429），请稍后重试。";
            if (status >= 300 && status < 400) return "AI 服务返回重定向，请直接填写最终服务地址。";
            return "AI 服务请求失败（HTTP " + status + "），请检查模型和高级设置。";
        }

        public void Dispose()
        {
            PendingRequest[] pending;
            lock (gate)
            {
                if (disposed) return;
                disposed = true;
                pending = active.ToArray();
            }
            foreach (var request in pending) Cancel(request);
        }
    }
}
