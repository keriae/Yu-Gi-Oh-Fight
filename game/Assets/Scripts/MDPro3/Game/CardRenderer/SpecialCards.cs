
using MDPro3.Duel.YGOSharp;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;

namespace MDPro3
{
    public static class SpecialCards
    {
        [Serializable]
        private class SpecialCardLists
        {
            public int[] FinalAttackBlueEyes;
            public int[] FinalAttackDarkM;
            public int[] FinalAttackRedEyes;
            public int[] FinalAttackObelisk;
            public int[] FinalAttackRa;
            public int[] FinalAttackSlifer;

            public int[] FrameObelisk;
            public int[] FrameRa;
            public int[] FrameSlifer;
            public int[] FrameRaviel;
            public int[] FrameUria;
            public int[] FrameHamon;

            public int[] LevelZeroMonsters;
            public Dictionary<int, int[]> SameCutins;

            public SpecialCardLists()
            {
                FinalAttackBlueEyes = new int[]
                {
                    89631139,   //青眼白龙
                    53347303,   //青眼光龙
                    22804410,   //渊眼白龙
                    38517737,   //青眼亚白龙
                    30576089,   //青眼喷气龙
                    9433350,    //罪 青眼白龙
                    53183600,   //青眼卡通龙
                    23995346,   //青眼究极龙
                    43228023,   //青眼究极亚龙
                    56532353,   //真青眼究极龙
                    2129638,    //青眼双爆裂龙
                    11443677    //青眼暴君龙
                };

                FinalAttackDarkM = new int[]
                {
                    46986414,   //黑魔术师
                    92377303,   //黑衣大贤者
                    342673,       //黑色魔术师-黑魔术师
                    21296502,   //卡通黑魔术师
                    29436665,   //黑魔导执行官
                    35191415,   //黑幻想之魔术师
                    38033121,   //黑魔术少女
                    90960358,   //卡通黑魔术少女
                    50237654    //超魔导师-黑魔术师徒
                };

                FinalAttackRedEyes = new int[]
                {
                    74677422,   //真红眼黑龙
                    96561011,   //真红眼暗龙
                    64335804,   //真红眼黑钢龙
                    18491580,   //真红眼亚黑龙
                    55343236,   //罪 真红眼黑龙
                    6556909,     //真红之魂
                };

                FinalAttackObelisk = new int[]
                {
                    10000000   //巨神兵
                };

                FinalAttackRa = new int[]
                {
                    10000010,   //翼神龙
                    10000080,   //蛋
                    10000090,   //不死鸟
                };

                FinalAttackSlifer = new int[]
                {
                    10000020   //天空龙
                };

                FrameObelisk = new int[]
                {
                    10000000   //巨神兵
                };

                FrameRa = new int[]
                {
                    10000010   //翼神龙
                };

                FrameSlifer = new int[]
                {
                    10000020   //天空龙
                };

                FrameRaviel = new int[]
                {
                    69890967   //幻魔皇 拉比艾尔
                };

                FrameUria = new int[]
                {
                    6007213    //神炎皇 乌利亚
                };

                FrameHamon = new int[]
                {
                    32491822   //降雷皇 哈蒙
                };

                LevelZeroMonsters = new int[]
                {
                    1686814,
                    90884403,
                    26973555,
                    43490025,
                    65305468,
                    52653092,
                    41522092,
                };

                SameCutins = new Dictionary<int, int[]>
                {
                    { 89631139, new int[] { 89631155 } },   // 青眼白龙1
                    { 89631141, new int[] { 89631142, 89631148, 89631151, 89631154 } },   // 青眼白龙2
                    { 89943723, new int[] { 89943725 } },   // 新宇侠
                    { 46986417, new int[] { 46986424, 46986426, 46986430 } },   // 黑魔术师
                    { 74677424, new int[] { 74677425 } },   // 真红眼黑龙
                    { 44508094, new int[] { 44508096 } },   // 星尘龙
                    { 84013237, new int[] { 84013240 } },   // 霍普
                    { 16178681, new int[] { 16178684 } },   // 异色眼
                    { 5043010, new int[] { 5043013 } },   // 防火龙
                    { 35405755, new int[] { 35405756 } },   // 天童
                    { 32828466, new int[] { 32828467 } },   // 醒醒侠
                    { 37818794, new int[] { 37818795 } },   // 超魔导龙骑士
                    { 44146295, new int[] { 44146296 } },   // 冰剑龙
                    { 35952884, new int[] { 35952885 } },   // 流天类星龙
                    { 6218704, new int[] { 6218705 } },   // 霸王天龙
                    { 10000000, new int[] { 10000004 } },   // 巨神兵
                    { 10000010, new int[] { 10000014 } },   // 翼神龙
                    { 10000020, new int[] { 10000024 } },   // 天空龙
                    { 12381100, new int[] { 12381101 } },   // 究极龙魔导
                    { 13331639, new int[] { 13331640 } },   // 霸王龙 扎克
                    { 22850702, new int[] { 22850703 } },   // 混沌之双翼
                    { 31801517, new int[] { 31801518 } },   // 光子龙皇
                    { 98127546, new int[] { 98127547 } },   // 冥神
                    { 98630720, new int[] { 98630721 } },   // 前托枪管龙
                    { 99585850, new int[] { 99585851 } },   // 真红莲超新星龙
                };
            }

            public bool ApplyDefaults()
            {
                var defaults = new SpecialCardLists();
                bool changed = false;

                changed |= ApplyDefault(ref FinalAttackBlueEyes, defaults.FinalAttackBlueEyes);
                changed |= ApplyDefault(ref FinalAttackDarkM, defaults.FinalAttackDarkM);
                changed |= ApplyDefault(ref FinalAttackRedEyes, defaults.FinalAttackRedEyes);
                changed |= ApplyDefault(ref FinalAttackObelisk, defaults.FinalAttackObelisk);
                changed |= ApplyDefault(ref FinalAttackRa, defaults.FinalAttackRa);
                changed |= ApplyDefault(ref FinalAttackSlifer, defaults.FinalAttackSlifer);
                changed |= ApplyDefault(ref FrameObelisk, defaults.FrameObelisk);
                changed |= ApplyDefault(ref FrameRa, defaults.FrameRa);
                changed |= ApplyDefault(ref FrameSlifer, defaults.FrameSlifer);
                changed |= ApplyDefault(ref FrameRaviel, defaults.FrameRaviel);
                changed |= ApplyDefault(ref FrameUria, defaults.FrameUria);
                changed |= ApplyDefault(ref FrameHamon, defaults.FrameHamon);
                changed |= ApplyDefault(ref LevelZeroMonsters, defaults.LevelZeroMonsters);

                return changed;
            }

            private static bool ApplyDefault(ref int[] target, int[] defaultValue)
            {
                if (target != null || target.Length == 0)
                    return false;
                target = defaultValue;
                return true;
            }
        }

        private const string PATH_JSON = Program.PATH_DATA + "SpecialCards.json";
        private static SpecialCardLists lists;
        private static SpecialCardLists Lists
        {
            get
            {
                if (lists == null)
                    Initialize();
                return lists;
            }
        }

        private static void Initialize()
        {
            if (!File.Exists(PATH_JSON))
            {
                lists = new SpecialCardLists();
                SaveJson(lists);
                return;
            }

            var json = File.ReadAllText(PATH_JSON);
            bool parsedFromFile = false;
            bool shouldWriteBack = false;
            try
            {
                lists = JsonConvert.DeserializeObject<SpecialCardLists>(json);
                parsedFromFile = true;
                if (lists == null)
                {
                    lists = new SpecialCardLists();
                    shouldWriteBack = true;
                }
            }
            catch (JsonReaderException ex)
            {
                MessageManager.Cast("Failed to parse SpecialCards.json: " + ex.Message);
                lists = new SpecialCardLists();
            }
            finally
            {
                lists ??= new SpecialCardLists();
                shouldWriteBack |= lists.ApplyDefaults();
                if (parsedFromFile && shouldWriteBack)
                    SaveJson(lists);
            }
        }

        private static void SaveJson(SpecialCardLists lists)
        {
            var json = JsonConvert.SerializeObject(lists, Formatting.Indented);
            File.WriteAllText(PATH_JSON, json);
        }

        public static bool IsLevelZeroMonster(this Card data)
        {
            var code = data.GetOriginalID();
            return Contains(Lists.LevelZeroMonsters, code);
        }

        public static int GetOriginalCutinCode(int code)
        {
            foreach (var pair in Lists.SameCutins)
            {
                if (Contains(pair.Value, code))
                    return pair.Key;
            }
            return code;
        }

        public static FinalAttackType GetFinalAttackType(int code)
        {
            var data = CardsManager.Get(code);
            var id = data.GetOriginalID();

            if (Contains(Lists.FinalAttackBlueEyes, id))
                return FinalAttackType.BlueEyes;
            if (Contains(Lists.FinalAttackDarkM, id))
                return FinalAttackType.DarkM;
            if (Contains(Lists.FinalAttackRedEyes, id))
                return FinalAttackType.RedEyes;
            if (Contains(Lists.FinalAttackObelisk, id))
                return FinalAttackType.Obelisk;
            if (Contains(Lists.FinalAttackRa, id))
                return FinalAttackType.Ra;
            if (Contains(Lists.FinalAttackSlifer, id))
                return FinalAttackType.Slifer;

            return FinalAttackType.Normal;
        }

        public static CardFrameType GetFrameType(Card data)
        {
            var id = data.Id;

            if (Contains(Lists.FrameObelisk, id))
                return CardFrameType.Obelisk;
            if (Contains(Lists.FrameRa, id))
                return CardFrameType.Ra;
            if (Contains(Lists.FrameSlifer, id))
                return CardFrameType.Slifer;
            if (Contains(Lists.FrameRaviel, id))
                return CardFrameType.Raviel;
            if (Contains(Lists.FrameUria, id))
                return CardFrameType.Uria;
            if (Contains(Lists.FrameHamon, id))
                return CardFrameType.Hamon;

            return CardFrameType.Normal;
        }

        public enum FinalAttackType
        {
            Normal,
            BlueEyes,
            DarkM,
            RedEyes,
            Obelisk,
            Ra,
            Slifer,
        }

        public enum CardFrameType
        {
            Normal,
            Obelisk,
            Ra,
            Slifer,
            Raviel,
            Uria,
            Hamon,
        }

        private static bool Contains(int[] list, int code)
        {
            return list != null && Array.Exists(list, c => c == code);
        }

    }
}
