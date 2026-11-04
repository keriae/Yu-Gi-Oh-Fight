using MDPro3.Duel.YGOSharp;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MDPro3
{
    public static class YdkeConverter
    {
        public static string ydkeHeader = "ydke://";

        public static Deck Ydke2Deck(string ydkeString)
        {
            if (string.IsNullOrWhiteSpace(ydkeString))
                return null;

            ydkeString = ydkeString.Trim();

            var headerIndex = ydkeString.IndexOf(ydkeHeader, StringComparison.Ordinal);
            if (headerIndex < 0)
                return null;

            ydkeString = ydkeString[(headerIndex + ydkeHeader.Length)..];

            var sections = ydkeString.Split('!');
            if (sections.Length < 3)
            {
                //throw new ArgumentException("Invalid YDKE format");
                return null;
            }

            Deck result;
            try
            {
                result = new Deck
                {
                    Main = DecodeSection(sections[0]),
                    Extra = DecodeSection(sections[1]),
                    Side = DecodeSection(sections[2])
                };
            }
            catch (FormatException)
            {
                return null;
            }

            // Replace pre-release ids with official ids (if mapping exists).
            YdkIdHelper.NormalizeDeck(result);

            return result;
        }

        private static List<int> DecodeSection(string base64Section)
        {
            if (string.IsNullOrEmpty(base64Section))
                return new List<int>();

            var decodedBytes = Convert.FromBase64String(base64Section);
            var cardIds = new List<int>();

            for (int i = 0; i < decodedBytes.Length; i += 4)
            {
                if (i + 4 > decodedBytes.Length) break;

                var cardId = BitConverter.ToUInt32(decodedBytes, i);
                cardIds.Add((int)cardId);
            }

            return cardIds;
        }

        public static string DeckToYdke(Deck deck)
        {
            if (deck == null)
                return string.Empty;

            deck.Main ??= new();
            deck.Extra ??= new();
            deck.Side ??= new();

            var main = EncodeSection(deck.Main);
            var extra = EncodeSection(deck.Extra);
            var side = EncodeSection(deck.Side);
            return $"{ydkeHeader}{main}!{extra}!{side}!";
        }

        private static string EncodeSection(List<int> deck)
        {
            var bytes = new List<byte>();

            foreach (var cardId in deck)
            {
                bytes.Add((byte)(cardId & 0xFF));
                bytes.Add((byte)((cardId >> 8) & 0xFF));
                bytes.Add((byte)((cardId >> 16) & 0xFF));
                bytes.Add((byte)(cardId >> 24));
            }

            return Convert.ToBase64String(bytes.ToArray());
        }
    }
}
