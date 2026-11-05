using System;

namespace MDPro3.AI
{
    /// <summary>Keeps the native 20-character name limit and disambiguates matching duelists.</summary>
    internal static class AiDuelistNames
    {
        internal static string ForSide(string name, string otherName, bool sideA)
        {
            string display = Limit(name, 20);
            if (otherName == null || !string.Equals(display, Limit(otherName, 20), StringComparison.OrdinalIgnoreCase))
                return display;
            string suffix = sideA ? " · A" : " · B";
            return Limit(name, 20 - suffix.Length) + suffix;
        }

        private static string Limit(string name, int maximum)
        {
            string value = string.IsNullOrWhiteSpace(name) ? "AI 分身" : name.Trim();
            int length = Math.Min(value.Length, maximum);
            if (length > 0 && char.IsHighSurrogate(value[length - 1])) length--;
            return value.Substring(0, length);
        }
    }
}
