using System;

namespace MDPro3
{
    // Printed rarities from pack.db; independent of the user's foil material.
    internal static class CardPrintRarity
    {
        internal static string Primary(string rarities)
        {
            if (string.IsNullOrWhiteSpace(rarities)) return string.Empty;
            int comma = rarities.IndexOf(',');
            return (comma < 0 ? rarities : rarities.Substring(0, comma)).Trim().ToUpperInvariant();
        }

        internal static string Label(string rarities)
        {
            string primary = Primary(rarities);
            if (primary.Length == 0) return "?";
            int coating = primary.LastIndexOf('+');
            if (coating >= 0) primary = primary.Substring(coating + 1);
            return primary == "20TH SE" ? "20SE" : primary == "10000 SE" ? "10KSE" : primary;
        }

        internal static int Rank(string rarities)
        {
            string primary = Primary(rarities);
            // Parallel, Millennium and Kaiba coatings retain the base rarity.
            int plus = primary.LastIndexOf('+');
            if (plus >= 0) primary = primary.Substring(plus + 1);
            return primary switch
            {
                "N" or "C" or "M" or "KC" => 0,
                "R" => 1,
                "P" => 2,
                "SR" => 3,
                "UR" => 4,
                "UL" => 5,
                "GR" => 6,
                "SE" => 7,
                "CR" => 8,
                "PSE" => 9,
                "HR" => 10,
                "EXSE" => 11,
                "GSE" => 12,
                "GMR" => 13,
                "20TH SE" => 14,
                "QCSE" => 15,
                "10000 SE" => 16,
                "STAR" => 17,
                _ => -1
            };
        }

        internal static int Compare(string left, string right, bool descending)
        {
            int a = Rank(left), b = Rank(right);
            // Missing data always follows known rarities, in either direction.
            if (a < 0 || b < 0) return (a < 0).CompareTo(b < 0);
            return descending ? b.CompareTo(a) : a.CompareTo(b);
        }
    }
}
