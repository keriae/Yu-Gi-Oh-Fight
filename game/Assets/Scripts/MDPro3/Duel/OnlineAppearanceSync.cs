using MDPro3.Duel.YGOSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDPro3
{
    public struct OnlineAppearanceData
    {
        public int Case;
        public int Protector;
        public int Field;
        public int Grave;
        public int Stand;
        public int Mate;
        public int Face;
        public int Frame;
    }

    public static class OnlineAppearanceSync
    {
        private const int DefaultCase = 1080001;
        private const int DefaultProtector = 1070001;
        private const int DefaultField = 1090001;
        private const int DefaultGrave = 1100001;
        private const int DefaultStand = 1110001;
        private const int DefaultMate = 1000001;
        private const int DefaultFace = 1010001;
        private const int DefaultFrame = 1030001;

        public static bool IsSyncMessage(string content)
        {
            return TryExtractPayload(content, out _);
        }

        public static string BuildMessage(Deck deck)
        {
            var data = BuildValidatedData(deck);
            var ints = new int[]
            { data.Case, data.Protector, data.Field, data.Grave, data.Stand, data.Mate, data.Face, data.Frame };
            return ZeroWidthIntsCodec.Encode(ints);
        }

        public static string BuildMessageForLocalPlayer(Deck deck)
        {
            var source = BuildSourceDeckForLocalPlayer(deck);
            return BuildMessage(source);
        }

        public static bool TryParse(string content, out OnlineAppearanceData data)
        {
            data = default;
            if (!TryExtractPayload(content, out var payload))
                return false;

            data = new OnlineAppearanceData
            {
                Case = EnsureValidCode(payload[0], Items.ItemType.Case, DefaultCase),
                Protector = EnsureValidCode(payload[1], Items.ItemType.Protector, DefaultProtector),
                Field = EnsureValidCode(payload[2], Items.ItemType.Mat, DefaultField),
                Grave = EnsureValidCode(payload[3], Items.ItemType.Grave, DefaultGrave),
                Stand = EnsureValidCode(payload[4], Items.ItemType.Stand, DefaultStand),
                Mate = EnsureValidCode(payload[5], Items.ItemType.Mate, DefaultMate),
                Face = EnsureValidCode(payload[6], Items.ItemType.Face, DefaultFace),
                Frame = EnsureValidCode(payload[7], Items.ItemType.Frame, DefaultFrame),
            };
            return true;
        }

        private static bool TryExtractPayload(string content, out int[] payload)
        {
            payload = ZeroWidthIntsCodec.Decode(content);
            if (payload == null) return false;
            return true;
        }

        public static bool IsValid(OnlineAppearanceData data)
        {
            return IsValidCode(data.Case, Items.ItemType.Case) &&
                   IsValidCode(data.Protector, Items.ItemType.Protector) &&
                   IsValidCode(data.Field, Items.ItemType.Mat) &&
                   IsValidCode(data.Grave, Items.ItemType.Grave) &&
                   IsValidCode(data.Stand, Items.ItemType.Stand) &&
                   IsValidCode(data.Mate, Items.ItemType.Mate) &&
                   IsValidCode(data.Face, Items.ItemType.Face) &&
                   IsValidCode(data.Frame, Items.ItemType.Frame);
        }

        private static OnlineAppearanceData BuildValidatedData(Deck deck)
        {
            var source = deck ?? new Deck();
            var faceFallback = GetDefaultCode(Items.ItemType.Face, DefaultFace);
            var frameFallback = GetDefaultCode(Items.ItemType.Frame, DefaultFrame);
            return new OnlineAppearanceData
            {
                Case = EnsureValidCode(source.Case, Items.ItemType.Case, DefaultCase),
                Protector = EnsureValidCode(source.Protector, Items.ItemType.Protector, DefaultProtector),
                Field = EnsureValidCode(source.Field, Items.ItemType.Mat, DefaultField),
                Grave = EnsureValidCode(source.Grave, Items.ItemType.Grave, DefaultGrave),
                Stand = EnsureValidCode(source.Stand, Items.ItemType.Stand, DefaultStand),
                Mate = EnsureValidCode(source.Mate, Items.ItemType.Mate, DefaultMate),
                Face = EnsureValidCode(ReadConfigCode("DuelFace0", faceFallback), Items.ItemType.Face, faceFallback),
                Frame = EnsureValidCode(ReadConfigCode("DuelFrame0", frameFallback), Items.ItemType.Frame, frameFallback),
            };
        }

        private static Deck BuildSourceDeckForLocalPlayer(Deck deck)
        {
            var source = deck ?? new Deck();
            if (!Config.GetBool("OverrideDeckAppearance", false))
                return source;

            var result = new Deck
            {
                Case = source.Case,
                Protector = ReadConfigCode("DuelProtector0", source.Protector),
                Field = ReadConfigCode("DuelField0", source.Field),
                Grave = ReadConfigCode("DuelGrave0", source.Grave),
                Stand = ReadConfigCode("DuelStand0", source.Stand),
                Mate = ReadConfigCode("DuelMate0", source.Mate),
            };
            return result;
        }

        private static int ReadConfigCode(string key, int fallback)
        {
            var raw = Config.Get(key, fallback.ToString());
            return int.TryParse(raw, out var code) ? code : fallback;
        }

        private static int EnsureValidCode(int code, Items.ItemType type, int fallback)
        {
            var normalized = NormalizeSpecialCode(code, type, fallback);
            if (IsSupportedSpecialCode(normalized, type))
                return normalized;
            return IsValidCode(normalized, type) ? normalized : fallback;
        }

        private static int NormalizeSpecialCode(int code, Items.ItemType type, int fallback)
        {
            if (code == Items.CODE_RANDOM)
            {
                if (Program.items != null)
                    return Program.items.GetRandomItem(type).id;
                return fallback;
            }

            if (IsSupportedSpecialCode(code, type))
                return code;

            if (code == Items.CODE_SAME || code == Items.CODE_NONE || code == Items.CODE_DIY)
                return fallback;

            return code;
        }

        private static bool IsValidCode(int code, Items.ItemType type)
        {
            if (IsSupportedSpecialCode(code, type))
                return true;

            var items = Program.items;
            if (items == null)
                return false;

            return type switch
            {
                Items.ItemType.Case => ContainsItem(items.cases, code),
                Items.ItemType.Face => ContainsItem(items.faces, code),
                Items.ItemType.Frame => ContainsItem(items.frames, code),
                Items.ItemType.Protector => ContainsItem(items.protectors, code),
                Items.ItemType.Mat => ContainsItem(items.mats, code),
                Items.ItemType.Grave => ContainsItem(items.graves, code),
                Items.ItemType.Stand => ContainsItem(items.stands, code),
                Items.ItemType.Mate => ContainsItem(items.mates, code),
                _ => false,
            };
        }

        private static bool IsSupportedSpecialCode(int code, Items.ItemType type)
        {
            return code switch
            {
                Items.CODE_DIY => type == Items.ItemType.Face || type == Items.ItemType.Frame,
                Items.CODE_SAME => type == Items.ItemType.Grave || type == Items.ItemType.Stand,
                Items.CODE_NONE => type == Items.ItemType.Stand || type == Items.ItemType.Mate,
                _ => false,
            };
        }

        private static int GetDefaultCode(Items.ItemType type, int fallback)
        {
            var items = Program.items;
            if (items == null)
                return fallback;

            return type switch
            {
                Items.ItemType.Face => GetFirstItemId(items.faces, fallback),
                Items.ItemType.Frame => GetFirstItemId(items.frames, fallback),
                _ => fallback,
            };
        }

        private static int GetFirstItemId(List<Items.Item> list, int fallback)
        {
            return list != null && list.Count > 0 ? list[0].id : fallback;
        }

        private static bool ContainsItem(List<Items.Item> list, int id)
        {
            if (list == null)
                return false;

            return list.Any(item => item.id == id);
        }
    }

    public static class ZeroWidthIntsCodec
    {
        private static readonly int[] DefaultValues = {
            1080001, 1070001, 1090001, 1100001, 1110001, 1000001, 1010001, 1030001
        };

        private static readonly char[] Bit3Char = {
            '\u200B', // 000
            '\u200C', // 001
            '\u200D', // 010
            '\u200E', // 011
            '\u200F', // 100
            '\u202A', // 101
            '\u202B', // 110
            '\u202C'  // 111
        };
        private static readonly Dictionary<char, byte> Char2Bit3 = new Dictionary<char, byte>
        {
            { '\u200B', 0 }, { '\u200C', 1 }, { '\u200D', 2 }, { '\u200E', 3 },
            { '\u200F', 4 }, { '\u202A', 5 }, { '\u202B', 6 }, { '\u202C', 7 }
        };

        private const string START_MARKER = "\u200D\u200D";

        public static string Encode(int[] ints)
        {
            if (ints == null)
                throw new ArgumentNullException(nameof(ints));
            if (ints.Length != DefaultValues.Length)
                throw new ArgumentException($"数组长度必须为 {DefaultValues.Length}");

            byte[] bytes = EncodeDiffs(ints);
            return EncodeToZeroWidth(bytes);
        }

        public static int[] Decode(string hiddenMessage)
        {
            if (string.IsNullOrEmpty(hiddenMessage))
                return null;

            byte[] bytes = DecodeFromZeroWidth(hiddenMessage);
            if (bytes == null)
                return null;

            return DecodeDiffs(bytes);
        }

        private static byte[] EncodeDiffs(int[] ints)
        {
            using var ms = new System.IO.MemoryStream();
            for (int i = 0; i < ints.Length; i++)
            {
                long diff = (long)ints[i] - DefaultValues[i];
                ulong zigzag = EncodeZigzag(diff);
                WriteVarint(ms, zigzag);
            }
            return ms.ToArray();
        }

        private static int[] DecodeDiffs(byte[] data)
        {
            using var ms = new System.IO.MemoryStream(data);
            var result = new int[DefaultValues.Length];
            for (int i = 0; i < result.Length; i++)
            {
                ulong zigzag = ReadVarint(ms);
                long diff = DecodeZigzag(zigzag);
                long val = DefaultValues[i] + diff;
                if (val < int.MinValue || val > int.MaxValue)
                    return null;
                result[i] = (int)val;
            }
            return result;
        }

        private static ulong EncodeZigzag(long n)
        {
            return (ulong)((n << 1) ^ (n >> 63));
        }

        private static long DecodeZigzag(ulong n)
        {
            return (long)((n >> 1) ^ (0 - (n & 1)));
        }

        private static void WriteVarint(System.IO.Stream stream, ulong value)
        {
            while (value >= 0x80)
            {
                stream.WriteByte((byte)((value & 0x7F) | 0x80));
                value >>= 7;
            }
            stream.WriteByte((byte)value);
        }

        private static ulong ReadVarint(System.IO.Stream stream)
        {
            ulong result = 0;
            int shift = 0;
            while (true)
            {
                int b = stream.ReadByte();
                if (b == -1) throw new System.IO.EndOfStreamException();
                result |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0) break;
                shift += 7;
                if (shift >= 64) throw new InvalidOperationException("Varint too long");
            }
            return result;
        }

        private static string EncodeToZeroWidth(byte[] bytes)
        {
            var bits = new List<bool>(bytes.Length * 8);
            foreach (byte b in bytes)
            {
                for (int i = 0; i < 8; i++)
                    bits.Add((b & (1 << i)) != 0);
            }

            var sb = new StringBuilder();
            sb.Append(START_MARKER);
            for (int i = 0; i < bits.Count; i += 3)
            {
                int value = 0;
                if (i + 0 < bits.Count && bits[i + 0]) value |= 1;
                if (i + 1 < bits.Count && bits[i + 1]) value |= 2;
                if (i + 2 < bits.Count && bits[i + 2]) value |= 4;
                sb.Append(Bit3Char[value]);
            }
            return sb.ToString();
        }

        private static byte[] DecodeFromZeroWidth(string hiddenMessage)
        {
            int markerPos = hiddenMessage.IndexOf(START_MARKER, StringComparison.Ordinal);
            if (markerPos == -1)
                return null;

            string dataPart = hiddenMessage.Substring(markerPos + START_MARKER.Length);
            var bits = new List<bool>();
            foreach (char c in dataPart)
            {
                if (!Char2Bit3.TryGetValue(c, out byte threeBits))
                    return null;
                bits.Add((threeBits & 1) != 0);
                bits.Add((threeBits & 2) != 0);
                bits.Add((threeBits & 4) != 0);
            }

            int byteCount = (bits.Count + 7) / 8;
            byte[] bytes = new byte[byteCount];
            for (int i = 0; i < bits.Count; i++)
            {
                if (bits[i])
                    bytes[i / 8] |= (byte)(1 << (i % 8));
            }
            return bytes;
        }
    }
}
