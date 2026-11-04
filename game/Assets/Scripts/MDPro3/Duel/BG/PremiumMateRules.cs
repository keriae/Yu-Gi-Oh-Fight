using System.Collections.Generic;
using System.Linq;

namespace MDPro3.Duel
{
    public enum PremiumMateBehavior
    {
        LaundryBattlePhaseRoundTrip,
        GaiaExtraDeckPermanent,
        ShuraigLpThreshold,
        RayeBattlePhaseAndDirectAttack,
        FiendsmithExtraDeckOrEquipPermanent,
        IpSpTurnParity
    }

    public sealed class PremiumMateRule
    {
        public int BaseId { get; }
        public int SubId { get; }
        public IReadOnlyList<int> VariantIds { get; }
        public PremiumMateBehavior Behavior { get; }
        public int LpThreshold { get; }

        public PremiumMateRule(
            int baseId,
            int subId,
            PremiumMateBehavior behavior,
            int lpThreshold = 0,
            IReadOnlyList<int> extraVariantIds = null)
        {
            BaseId = baseId;
            SubId = subId;
            Behavior = behavior;
            LpThreshold = lpThreshold;

            var variants = new List<int> { subId };
            if (extraVariantIds != null)
                foreach (var variantId in extraVariantIds)
                    if (variantId > 0 && variantId != baseId && !variants.Contains(variantId))
                        variants.Add(variantId);

            VariantIds = variants;
        }
    }

    public static class PremiumMateRules
    {
        private static readonly List<PremiumMateRule> _rules = new()
        {
            new PremiumMateRule(1000020, 1000021, PremiumMateBehavior.LaundryBattlePhaseRoundTrip),
            new PremiumMateRule(1003001, 1003101, PremiumMateBehavior.GaiaExtraDeckPermanent),
            new PremiumMateRule(1003002, 1003102, PremiumMateBehavior.ShuraigLpThreshold, 3000),
            new PremiumMateRule(1003003, 1003203, PremiumMateBehavior.RayeBattlePhaseAndDirectAttack, 0, new[] { 1003103 }),
            new PremiumMateRule(1003004, 1003104, PremiumMateBehavior.FiendsmithExtraDeckOrEquipPermanent),
            new PremiumMateRule(1003005, 1003105, PremiumMateBehavior.IpSpTurnParity),
        };

        private static readonly Dictionary<int, PremiumMateRule> _ruleByAnyId = _rules
            .SelectMany(rule => rule.VariantIds
                .Select(id => new KeyValuePair<int, PremiumMateRule>(id, rule))
                .Prepend(new KeyValuePair<int, PremiumMateRule>(rule.BaseId, rule)))
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        private static readonly Dictionary<int, PremiumMateRule> _ruleByBaseId = _rules
            .ToDictionary(rule => rule.BaseId, rule => rule);

        public static IReadOnlyList<PremiumMateRule> All => _rules;

        public static bool TryGetRule(int mateId, out PremiumMateRule rule)
        {
            return _ruleByAnyId.TryGetValue(mateId, out rule);
        }

        public static bool TryGetRuleByBaseId(int mateId, out PremiumMateRule rule)
        {
            return _ruleByBaseId.TryGetValue(mateId, out rule);
        }

        public static bool IsPremiumMateId(int mateId)
        {
            return _ruleByAnyId.ContainsKey(mateId);
        }

        public static bool IsPremiumBaseId(int mateId)
        {
            return _ruleByBaseId.ContainsKey(mateId);
        }

        public static bool IsPremiumVariantId(int mateId)
        {
            return TryGetRule(mateId, out var rule) && rule.VariantIds.Contains(mateId);
        }

        public static int GetBaseMateId(int mateId)
        {
            return TryGetRule(mateId, out var rule) ? rule.BaseId : mateId;
        }

        public static int GetSubMateId(int mateId)
        {
            return TryGetRule(mateId, out var rule) ? rule.SubId : mateId;
        }

        public static List<Items.Item> FilterAppearanceMateItems(IEnumerable<Items.Item> source)
        {
            return source.Where(item => !IsPremiumVariantId(item.id)).ToList();
        }
    }
}
