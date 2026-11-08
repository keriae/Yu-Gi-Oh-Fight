using System.Globalization;
using MDPro3.Duel.YGOSharp;

namespace MDPro3.AI
{
    /// <summary>Local duel rules shared by the two selected AI duelists.</summary>
    public sealed class ResponsesDuelOptions
    {
        public int LifePoints { get; set; } = 8000;
        public int StartingHand { get; set; } = 5;
        public int DrawPerTurn { get; set; } = 1;
        public bool NoShuffle { get; set; }
        public int MasterRule { get; set; } = 5;
        public string BanlistName { get; set; } = BanlistManager.DefaultBanlistName;

        public ResponsesDuelOptions Clone() => (ResponsesDuelOptions)MemberwiseClone();

        public void Validate()
        {
            if (LifePoints < 1) throw new AiConfigurationException("初始生命值应在 1～2147483647 之间。");
            if (StartingHand < 0 || StartingHand > 40) throw new AiConfigurationException("起始手牌应在 0～40 张之间。");
            if (DrawPerTurn < 0 || DrawPerTurn > 40) throw new AiConfigurationException("每回合抽卡应在 0～40 张之间。");
            if (MasterRule < 1 || MasterRule > 5) throw new AiConfigurationException("请选择有效的决斗规则。");
            ResolveBanlist();
        }

        public Banlist ResolveBanlist()
        {
            if (BanlistName == BanlistManager.EMPTY_LIST_NAME) return null;
            if (BanlistManager.Banlists != null)
                foreach (var list in BanlistManager.Banlists)
                    if (list.Name == BanlistName && !list.isCredit) return list;
            throw new AiConfigurationException("所选限卡表不可用，请在对战设置中重新选择。");
        }

        public string ToServerArguments(int port)
        {
            Validate();
            int listIndex = ResolveBanlist() == null ? -1 : BanlistManager.GetIndexByName(BanlistName);
            return string.Join(" ", port.ToString(CultureInfo.InvariantCulture), listIndex.ToString(CultureInfo.InvariantCulture), MasterRule.ToString(CultureInfo.InvariantCulture),
                "0", "F", "F", NoShuffle ? "T" : "F", LifePoints.ToString(CultureInfo.InvariantCulture),
                StartingHand.ToString(CultureInfo.InvariantCulture), DrawPerTurn.ToString(CultureInfo.InvariantCulture), "0", "0");
        }
    }
}
