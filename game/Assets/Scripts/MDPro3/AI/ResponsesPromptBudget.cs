using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MDPro3.AI
{
    public static class ResponsesPromptBudget
    {
        public const int EnvelopeReserveTokens = 1024;

        public static int ReservedOutput(JObject request)
        {
            var value = request["max_output_tokens"];
            if (value == null || (value.Type != JTokenType.Integer && value.Type != JTokenType.Raw)
                || !int.TryParse(value.ToString(Formatting.None), NumberStyles.None, CultureInfo.InvariantCulture, out int tokens)
                || tokens < 128 || tokens > 128000)
                throw new AiConfigurationException("实际输出上限应在 128～128000 tokens 之间。");
            return tokens;
        }

        public static int Estimate(JObject request)
        {
            // Count the unescaped input and instructions that the model actually reads, plus the
            // output schema and all remaining request settings conservatively as separate content.
            var remainder = (JObject)request.DeepClone();
            remainder.Remove("input"); remainder.Remove("instructions");
            int content = PromptTokenCounter.Count((string)request["input"])
                + PromptTokenCounter.Count((string)request["instructions"])
                + PromptTokenCounter.Count(remainder.ToString(Formatting.None));
            // Provider tokenizers and server-side formatting can differ. Reserve 5% plus an envelope.
            return content + (content + 19) / 20 + EnvelopeReserveTokens + ReservedOutput(request);
        }

        public static void Apply(JObject request, int limit)
        {
            if (Estimate(request) <= limit) return;
            JObject input = null;
            try { input = JObject.Parse((string)request["input"]); }
            catch (JsonException) { }
            var history = input?["state"]?["duel_history"] as JObject;
            var events = history?["events"] as JArray;
            if (events == null || events.Count == 0)
                throw new AiConfigurationException("当前局面、卡组和候选已超过上下文上限，请提高高级设置中的上下文上限。");
            var original = (JArray)events.DeepClone();
            int omitted = (int?)history["omitted_events"] ?? 0;
            void Drop(int count)
            {
                var retained = new JArray();
                for (int i = count; i < original.Count; i++) retained.Add(original[i].DeepClone());
                history["events"] = retained;
                history["omitted_events"] = omitted + count;
                history["complete"] = false;
                history["truncation_reason"] = "total_prompt_budget";
                request["input"] = input.ToString(Formatting.None);
            }
            // All non-history content is preserved, including retry feedback and the current candidates.
            Drop(original.Count);
            if (Estimate(request) > limit)
                throw new AiConfigurationException("当前局面、卡组和候选已超过上下文上限，请提高高级设置中的上下文上限。");
            int low = 1, high = original.Count;
            while (low < high)
            {
                int middle = low + (high - low) / 2;
                Drop(middle);
                if (Estimate(request) <= limit) high = middle;
                else low = middle + 1;
            }
            Drop(low);
            // Verify the actual final serialized input; do not rely solely on per-event estimates.
            while (Estimate(request) > limit && low < original.Count) Drop(++low);
            if (Estimate(request) > limit) throw new AiConfigurationException("上下文上限不足。");
        }
    }
}
