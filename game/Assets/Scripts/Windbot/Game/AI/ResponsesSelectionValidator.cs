using System;
using System.Collections.Generic;
using System.Linq;

namespace WindBot.Game.AI
{
    public struct ResponsesSumValue
    {
        public int Primary;
        public int Alternative;
        public ResponsesSumValue(int primary, int alternative = 0)
        {
            Primary = primary;
            Alternative = alternative;
        }
    }

    /// <summary>Validates protocol selections against the values supplied by the original rule core.</summary>
    public static class ResponsesSelectionValidator
    {
        public static bool ValidIndices(IList<int> selected, int count, int min, int max, bool cancelable)
        {
            if (selected == null || selected.Any(i => i < 0 || i >= count)
                || selected.Distinct().Count() != selected.Count)
                return false;
            return selected.Count == 0 && cancelable || selected.Count >= min && selected.Count <= max;
        }

        public static bool ValidCounters(IList<int> used, IList<int> capacities, int quantity)
        {
            if (used == null || capacities == null || used.Count != capacities.Count)
                return false;
            long total = 0;
            for (int i = 0; i < used.Count; i++)
            {
                if (used[i] < 0 || used[i] > capacities[i]) return false;
                total += used[i];
            }
            return total == quantity;
        }

        public static bool ValidTributes(IList<int> selected, IList<int> releaseValues, int min, int max, bool cancelable)
        {
            if (!ValidIndices(selected, releaseValues.Count, 0, max, cancelable)) return false;
            if (selected.Count == 0 && cancelable) return true;
            return selected.Sum(i => releaseValues[i]) >= min;
        }

        // Protocol semantics are taken from ygopro-core/playerop.cpp select_with_sum_limit:
        // https://github.com/Fluorohydride/ygopro-core/blob/master/playerop.cpp
        // Mandatory materials contribute their full alternative values; they do not count against optional min/max.
        public static bool ValidSum(IList<ResponsesSumValue> optional, IList<int> selected,
            IList<ResponsesSumValue> mandatory, int target, int min, int max, bool exact)
        {
            if (!ValidIndices(selected, optional.Count, exact ? min : 0, exact ? max : optional.Count, false))
                return false;
            var values = new List<ResponsesSumValue>(mandatory ?? new ResponsesSumValue[0]);
            foreach (int index in selected) values.Add(optional[index]);
            if (values.Count == 0) return false;
            if (exact)
            {
                // A compact subset-sum bitset includes every primary/alternative mix without
                // allocating millions of HashSet nodes for large material lists.
                long baseline = 0, ceiling = 0;
                var differences = new List<int>();
                foreach (var value in values)
                {
                    int first = value.Primary > 0 ? value.Primary : value.Alternative;
                    if (first <= 0) return false;
                    int second = value.Alternative > 0 ? value.Alternative : first;
                    int smaller = Math.Min(first, second), larger = Math.Max(first, second);
                    baseline += smaller;
                    ceiling += larger;
                    differences.Add(larger - smaller);
                }
                if (target < baseline || target > ceiling) return false;
                int gap = checked((int)(target - baseline));
                var bits = new ulong[(gap >> 6) + 1];
                bits[0] = 1;
                foreach (int difference in differences)
                {
                    if (difference == 0 || difference > gap) continue;
                    int wordShift = difference >> 6, bitShift = difference & 63;
                    for (int word = bits.Length - 1; word >= wordShift; word--)
                    {
                        int source = word - wordShift;
                        ulong shifted = bits[source] << bitShift;
                        if (bitShift != 0 && source > 0) shifted |= bits[source - 1] >> (64 - bitShift);
                        bits[word] |= shifted;
                    }
                }
                return (bits[gap >> 6] & (1UL << (gap & 63))) != 0;
            }
            long low = 0, high = 0;
            int smallest = int.MaxValue;
            foreach (var value in values)
            {
                int smaller = value.Alternative > 0 ? Math.Min(value.Primary, value.Alternative) : value.Primary;
                low += smaller;
                high += Math.Max(value.Primary, value.Alternative);
                smallest = Math.Min(smallest, smaller);
            }
            return high >= target && low - smallest < target;
        }

        public static bool ValidZones(IList<int> selected, IList<uint> masks, int count)
        {
            if (!ValidIndices(selected, masks.Count, count, count, false)) return false;
            uint occupied = 0;
            foreach (int index in selected)
            {
                uint mask = masks[index];
                // The two visual sides of each extra monster zone refer to one physical zone.
                if ((mask & (1u << 5)) != 0) mask |= 1u << 22;
                if ((mask & (1u << 6)) != 0) mask |= 1u << 21;
                if ((mask & (1u << 21)) != 0) mask |= 1u << 6;
                if ((mask & (1u << 22)) != 0) mask |= 1u << 5;
                if ((occupied & mask) != 0) return false;
                occupied |= mask;
            }
            return true;
        }
    }
}
