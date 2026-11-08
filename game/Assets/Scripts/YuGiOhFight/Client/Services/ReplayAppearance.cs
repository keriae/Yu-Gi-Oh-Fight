using System;
using System.Collections.Generic;
using System.IO;
using MDPro3.Duel.YGOSharp;
using YuGiOhFight.Client.Controllers;
using YuGiOhFight.Client.Runtime.Network;
using YuGiOhFight.Client.Presentation.Assets;

namespace YuGiOhFight.Client.Services
{
    // Optional extension of sibyl_name, after the six names and master rule.
    // Older clients still read the original header; older recordings have no extension.
    public static class ReplayAppearance
    {
        private const int NameHeaderSize = 6 * 50 * 2 + sizeof(int);
        private const int Magic = 0x50414659; // YFAP
        private static readonly string[] Slots = { "0", "1", "0Tag", "1Tag" };
        private static readonly string[] Categories = { "Face", "Frame", "Protector", "Field", "Grave", "Stand", "Mate", "Character" };
        private static readonly string[] Defaults = { "1010001", "1030001", "1070001", "1090001", "1100001", "1110001", "1000001", "0001" };
        private static Dictionary<string, string> captured = new();
        private static Dictionary<string, string> recorded = new();
        private static readonly Dictionary<string, (string Requested, string Resolved)> selections = new();

        public static void BeginAssetLoad() => selections.Clear();

        public static string Get(string key, string fallback)
        {
            if (OcgCore.condition == OcgCore.Condition.Replay && key.StartsWith("Replay", StringComparison.Ordinal)
                && recorded.TryGetValue(key.Substring(6), out var value)) return value;
            string raw = MDPro3.Config.Get(key, fallback);
            string category = key.StartsWith("Replay", StringComparison.Ordinal) ? key.Substring(6)
                : key.StartsWith("Duel", StringComparison.Ordinal) ? key.Substring(4)
                : key.StartsWith("Watch", StringComparison.Ordinal) ? key.Substring(5) : "";
            category = category.TrimEnd('0', '1').Replace("0Tag", "").Replace("1Tag", "");
            return Resolve(key, category, raw);
        }

        public static int ResolveDeck(string category, int code)
            => int.Parse(Resolve("Deck" + category, category, code.ToString()));

        private static string Resolve(string key, string category, string raw)
        {
            if (raw != Items.CODE_RANDOM.ToString() || Runtime.Program.items == null) return raw;
            if (selections.TryGetValue(key, out var selection) && selection.Requested == raw) return selection.Resolved;
            var type = category switch
            {
                "Face" => Items.ItemType.Face,
                "Frame" => Items.ItemType.Frame,
                "Protector" => Items.ItemType.Protector,
                "Field" => Items.ItemType.Mat,
                "Grave" => Items.ItemType.Grave,
                "Stand" => Items.ItemType.Stand,
                "Mate" => Items.ItemType.Mate,
                _ => Items.ItemType.Unknown
            };
            if (type == Items.ItemType.Unknown) return raw;
            string resolved = Runtime.Program.items.GetRandomItem(type).id.ToString();
            selections[key] = (raw, resolved);
            return resolved;
        }

        // Capture at entry, before settings or the AI session can change during/after the duel.
        public static void Capture(Deck deck)
        {
            if (OcgCore.condition == OcgCore.Condition.Replay) return;
            var result = new Dictionary<string, string>();
            foreach (string slot in Slots)
                for (int i = 0; i < Categories.Length; i++)
                    result[Categories[i] + slot] = Get(OcgCore.condition + Categories[i] + slot, Defaults[i]);

            // Local tag settings are relative to the viewer; replay names use team order.
            if (OcgCore.condition == OcgCore.Condition.Duel && RoomServant.Mode == 2
                && (RoomServant.SelfType == 1 || RoomServant.SelfType == 3))
                foreach (string category in new[] { "Face", "Frame" })
                    (result[category + "0"], result[category + "0Tag"]) = (result[category + "0Tag"], result[category + "0"]);

            if (deck != null && !MDPro3.Config.GetBool("OverrideDeckAppearance", false))
            {
                result["Protector0"] = ResolveDeck("Protector", deck.Protector).ToString();
                result["Field0"] = ResolveDeck("Field", deck.Field).ToString();
                result["Grave0"] = ResolveDeck("Grave", deck.Grave).ToString();
                result["Stand0"] = ResolveDeck("Stand", deck.Stand).ToString();
                result["Mate0"] = ResolveDeck("Mate", deck.Mate).ToString();
            }

            for (int side = 0; side < 2; side++)
            {
                string slot = side.ToString();
                result["Character" + slot] = ResponsesDuelSession.AppearanceForSide(side, "Character", result["Character" + slot]);
                for (int tag = 0; tag < 2; tag++)
                {
                    // The local duelist's face comes from settings; opponents and spectators use sync data.
                    if (side == 0 && OcgCore.condition == OcgCore.Condition.Duel) continue;
                    if (!RoomServant.TryGetOnlineAppearanceForSide(side, tag != 0, out var appearance)) continue;
                    string suffix = slot + (tag != 0 ? "Tag" : "");
                    result["Face" + suffix] = appearance.Face.ToString();
                    result["Frame" + suffix] = appearance.Frame.ToString();
                    result["Protector" + suffix] = appearance.Protector.ToString();
                    result["Field" + suffix] = appearance.Field.ToString();
                    result["Grave" + suffix] = appearance.Grave.ToString();
                    result["Stand" + suffix] = appearance.Stand.ToString();
                    result["Mate" + suffix] = appearance.Mate.ToString();
                }
            }
            captured = result;
        }

        public static void Write(BinaryWriter writer)
        {
            if (captured.Count == 0) return;
            writer.Write(Magic);
            writer.Write((byte)1);
            foreach (string slot in Slots)
                foreach (string category in Categories)
                    writer.Write(captured[category + slot]);
        }

        public static void Prepare(List<Package> packages)
        {
            // Always reset, including when opening an old recording after a new one.
            recorded = new Dictionary<string, string>();
            foreach (var package in packages)
            {
                if (package.Function != (int)GameMessage.sibyl_name) continue;
                var values = Read(package.Data.Get());
                if (values == null) continue;
                recorded = values;
                break;
            }
        }

        private static Dictionary<string, string> Read(byte[] data)
        {
            if (data.Length < NameHeaderSize + sizeof(int) + 1) return null;
            try
            {
                using var reader = new BinaryReader(new MemoryStream(data));
                reader.BaseStream.Position = NameHeaderSize;
                if (reader.ReadInt32() != Magic || reader.ReadByte() != 1) return null;
                var result = new Dictionary<string, string>();
                foreach (string slot in Slots)
                    foreach (string category in Categories)
                    {
                        string value = reader.ReadString();
                        if (value.Length > 16 || !int.TryParse(value, out _)) return null;
                        result[category + slot] = value;
                    }
                return result;
            }
            catch (IOException) { return null; }
            catch (ArgumentException) { return null; }
        }

        public static void PreserveMetadata(List<Package> converted, List<Package> source)
        {
            var metadata = source.Find(package => package.Function == (int)GameMessage.sibyl_name);
            if (metadata == null) return;
            converted.RemoveAll(package => package.Function == (int)GameMessage.sibyl_name);
            int start = converted.FindIndex(package => package.Function == (int)GameMessage.Start
                || package.Function == (int)GameMessage.ReloadField);
            converted.Insert(start < 0 ? 0 : start + 1, metadata);
        }
    }
}
