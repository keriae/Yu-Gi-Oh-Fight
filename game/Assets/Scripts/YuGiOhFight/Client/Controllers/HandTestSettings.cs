using UnityEngine;


namespace YuGiOhFight.Client.Controllers
{
    public class HandTestSettings
    {
        private const string CONFIG_NO_SHUFFLE = "HandTestNoShuffleDeck";
        private const string CONFIG_STARTING_HAND = "HandTestStartingHand";
        private const string CONFIG_LIFE_POINTS = "HandTestLifePoints";
        private const string CONFIG_MASTER_RULE = "HandTestMasterRule";
        private const string CONFIG_SAVE_REPLAY = "HandTestSaveReplay";

        public const int DefaultStartingHand = 5;
        public const int DefaultMasterRule = 5;

        private const int DefaultLifePoint = 8000;
        private const int DefaultDrawCount = 1;
        private const int DefaultTimeLimit = 0;

        public bool NoShuffleDeck;
        public int StartingHand = DefaultStartingHand;
        public int LifePoints = DefaultLifePoint;
        public int MasterRule = DefaultMasterRule;
        public bool SaveReplayWhenLeaving = true;

        public static HandTestSettings Load()
        {
            HandTestSettings settings = new HandTestSettings
            {
                NoShuffleDeck = GetBool(CONFIG_NO_SHUFFLE, true),
                StartingHand = GetInt(CONFIG_STARTING_HAND, DefaultStartingHand),
                LifePoints = GetInt(CONFIG_LIFE_POINTS, DefaultLifePoint),
                MasterRule = GetInt(CONFIG_MASTER_RULE, DefaultMasterRule),
                SaveReplayWhenLeaving = GetBool(CONFIG_SAVE_REPLAY, true)
            };
            settings.Clamp();
            return settings;
        }

        public HandTestSettings Clone()
        {
            return new HandTestSettings
            {
                NoShuffleDeck = NoShuffleDeck,
                StartingHand = StartingHand,
                LifePoints = LifePoints,
                MasterRule = MasterRule,
                SaveReplayWhenLeaving = SaveReplayWhenLeaving
            };
        }

        public void Save()
        {
            Clamp();
            MDPro3.Config.SetBool(CONFIG_NO_SHUFFLE, NoShuffleDeck);
            MDPro3.Config.Set(CONFIG_STARTING_HAND, StartingHand.ToString());
            MDPro3.Config.Set(CONFIG_LIFE_POINTS, LifePoints.ToString());
            MDPro3.Config.Set(CONFIG_MASTER_RULE, MasterRule.ToString());
            MDPro3.Config.SetBool(CONFIG_SAVE_REPLAY, SaveReplayWhenLeaving);
            MDPro3.Config.Save();
        }

        public void Clamp()
        {
            StartingHand = Mathf.Clamp(StartingHand, 1, 60);
            LifePoints = Mathf.Max(1, LifePoints);
            MasterRule = DefaultMasterRule;
        }

        public string GetRuleDisplayName()
        {
            return GetRuleDisplayName(MasterRule);
        }

        public static string GetRuleDisplayName(int masterRule)
        {
            return Presentation.StringHelper.GetUnsafe(1259 + Mathf.Clamp(masterRule, 1, DefaultMasterRule));
        }

        public string BuildServerArgs(int port)
        {
            Clamp();
            return string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}",
                port.ToString(),
                "-1",
                "5",
                "0",
                MasterRule.ToString(),
                "T",
                NoShuffleDeck ? "T" : "F",
                LifePoints.ToString(),
                StartingHand.ToString(),
                DefaultDrawCount.ToString(),
                DefaultTimeLimit.ToString(),
                "0");
        }

        private static int GetInt(string key, int defaultValue)
        {
            if (MDPro3.Config.Have(key) && int.TryParse(MDPro3.Config.Get(key, defaultValue.ToString()), out var parsed))
                return parsed;
            return defaultValue;
        }

        private static bool GetBool(string key, bool defaultValue)
            => MDPro3.Config.Have(key) ? MDPro3.Config.GetBool(key, defaultValue) : defaultValue;
    }
}
