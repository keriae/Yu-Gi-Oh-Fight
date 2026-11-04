using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MDPro3.AI
{
    /// <summary>Local o200k_base BPE count for budgeting. A provider may use another tokenizer.</summary>
    public static class PromptTokenCounter
    {
        public const string EncodingName = "o200k_base";
        private static readonly Lazy<Dictionary<string, int>> Ranks = new Lazy<Dictionary<string, int>>(ReadRanks);
        private static readonly Regex Pieces = new Regex(
            @"[^\r\n\p{L}\p{N}]?[\p{Lu}\p{Lt}\p{Lm}\p{Lo}\p{M}]*[\p{Ll}\p{Lm}\p{Lo}\p{M}]+(?i:'s|'t|'re|'ve|'m|'ll|'d)?"
            + @"|[^\r\n\p{L}\p{N}]?[\p{Lu}\p{Lt}\p{Lm}\p{Lo}\p{M}]+[\p{Ll}\p{Lm}\p{Lo}\p{M}]*(?i:'s|'t|'re|'ve|'m|'ll|'d)?"
            + @"|\p{N}{1,3}| ?[^\s\p{L}\p{N}]+[\r\n/]*|\s*[\r\n]+|\s+(?!\S)|\s+",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static string ByteString(byte[] bytes)
        {
            var chars = new char[bytes.Length];
            for (int i = 0; i < bytes.Length; i++) chars[i] = (char)bytes[i];
            return new string(chars);
        }

        private static Dictionary<string, int> ReadRanks()
        {
            var result = new Dictionary<string, int>(200000, StringComparer.Ordinal);
            using (var source = typeof(PromptTokenCounter).Assembly.GetManifestResourceStream("MDPro3.AI.o200k_base.tiktoken"))
            {
                if (source == null) throw new InvalidOperationException("本地 Prompt tokenizer 资源缺失，请重新构建应用。");
                using (var reader = new StreamReader(source, Encoding.ASCII))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        int split = line.IndexOf(' ');
                        result.Add(ByteString(Convert.FromBase64String(line.Substring(0, split))), int.Parse(line.Substring(split + 1)));
                    }
                }
            }
            return result;
        }

        private struct Pair
        {
            public int Index, Next, End, NextEnd, Rank;
        }

        private sealed class Heap
        {
            private readonly List<Pair> items = new List<Pair>();
            public int Count => items.Count;
            private static bool Before(Pair a, Pair b) => a.Rank < b.Rank || (a.Rank == b.Rank && a.Index < b.Index);
            public void Push(Pair item)
            {
                int index = items.Count;
                items.Add(item);
                while (index > 0)
                {
                    int parent = (index - 1) / 2;
                    if (!Before(item, items[parent])) break;
                    items[index] = items[parent]; index = parent;
                }
                items[index] = item;
            }
            public Pair Pop()
            {
                var result = items[0]; var last = items[items.Count - 1];
                items.RemoveAt(items.Count - 1);
                if (items.Count == 0) return result;
                int index = 0;
                while (index * 2 + 1 < items.Count)
                {
                    int child = index * 2 + 1;
                    if (child + 1 < items.Count && Before(items[child + 1], items[child])) child++;
                    if (!Before(items[child], last)) break;
                    items[index] = items[child]; index = child;
                }
                items[index] = last;
                return result;
            }
        }

        private static int CountPiece(string text, Dictionary<string, int> ranks)
        {
            string piece = ByteString(Encoding.UTF8.GetBytes(text));
            if (ranks.ContainsKey(piece)) return 1;
            int length = piece.Length;
            var next = new int[length]; var previous = new int[length]; var end = new int[length];
            var heap = new Heap();
            void Offer(int index)
            {
                if (index < 0 || end[index] == 0 || next[index] >= length) return;
                int right = next[index];
                if (ranks.TryGetValue(piece.Substring(index, end[right] - index), out int rank))
                    heap.Push(new Pair { Index = index, Next = right, End = end[index], NextEnd = end[right], Rank = rank });
            }
            for (int i = 0; i < length; i++) { next[i] = i + 1; previous[i] = i - 1; end[i] = i + 1; }
            for (int i = 0; i < length - 1; i++) Offer(i);
            int count = length;
            while (heap.Count > 0)
            {
                var pair = heap.Pop(); int left = pair.Index, right = pair.Next;
                if (end[left] != pair.End || next[left] != right || end[right] != pair.NextEnd) continue;
                end[left] = end[right]; next[left] = next[right]; end[right] = 0;
                if (next[left] < length) previous[next[left]] = left;
                count--;
                Offer(previous[left]); Offer(left);
            }
            return count;
        }

        public static int Count(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            var ranks = Ranks.Value;
            int count = 0, consumed = 0;
            foreach (Match match in Pieces.Matches(text, 0))
            {
                // Conservatively count any Unicode code units not covered by the local regex implementation.
                if (match.Index > consumed) count += Encoding.UTF8.GetByteCount(text.Substring(consumed, match.Index - consumed));
                count += CountPiece(match.Value, ranks);
                consumed = match.Index + match.Length;
            }
            if (consumed < text.Length) count += Encoding.UTF8.GetByteCount(text.Substring(consumed));
            return count;
        }
    }
}
