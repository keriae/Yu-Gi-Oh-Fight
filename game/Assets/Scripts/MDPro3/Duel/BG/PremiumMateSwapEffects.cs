using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MDPro3.Duel
{
    public sealed class PremiumMateSwapEffect
    {
        public int BaseId { get; }
        public float DelaySeconds { get; }
        public float ToSubDelaySeconds { get; }
        public float ToBaseDelaySeconds { get; }
        public bool UseChangeMotion { get; }
        public bool UseTransitionDelayAsMinimum { get; }
        public string EffectAssetPath { get; }
        public string ToSubEffectAssetPath { get; }
        public string ToBaseEffectAssetPath { get; }
        public string ToSubEffectLabel { get; }
        public string ToBaseEffectLabel { get; }
        public bool PreferEffectLabelPlayback { get; }
        public bool PlayChangeOnTargetMate { get; }
        public bool PlayChangeOnBothMates { get; }
        public bool PreferNonChangeNextWhenChangeTriggerMissing { get; }
        public bool CompensateCurrentTriggerQueueDelay { get; }
        public float MaxCurrentTriggerQueueDelaySeconds { get; }
        public bool UseUnscaledSwapTiming { get; }
        public float NextMotionDelaySeconds { get; }
        public float SourceToEffectDelaySeconds { get; }
        public float NextMotionLeadInSeconds { get; }
        public float NextMotionMinDurationSeconds { get; }
        public IReadOnlyList<string> ToSubTriggerPriority { get; }
        public IReadOnlyList<string> ToBaseTriggerPriority { get; }
        public IReadOnlyList<string> ToSubCurrentTriggerPriority { get; }
        public IReadOnlyList<string> ToBaseCurrentTriggerPriority { get; }
        public IReadOnlyList<string> ToSubNextTriggerPriority { get; }
        public IReadOnlyList<string> ToBaseNextTriggerPriority { get; }
        public IReadOnlyList<string> DuelStartTriggerPriority { get; }

        private static readonly string[] DefaultToSubTriggers = { "Change", "Change1", "ChangePreHide", "Change2" };
        private static readonly string[] DefaultToBaseTriggers = { "Change2", "ChangeBack", "ChangePreHide", "Change" };

        public PremiumMateSwapEffect(
            int baseId,
            float delaySeconds,
            bool useChangeMotion,
            string effectAssetPath,
            bool useTransitionDelayAsMinimum = true,
            float toSubDelaySeconds = -1f,
            float toBaseDelaySeconds = -1f,
            IReadOnlyList<string> toSubTriggerPriority = null,
            IReadOnlyList<string> toBaseTriggerPriority = null,
            IReadOnlyList<string> toSubCurrentTriggerPriority = null,
            IReadOnlyList<string> toBaseCurrentTriggerPriority = null,
            IReadOnlyList<string> toSubNextTriggerPriority = null,
            IReadOnlyList<string> toBaseNextTriggerPriority = null,
            IReadOnlyList<string> duelStartTriggerPriority = null,
            string toSubEffectAssetPath = null,
            string toBaseEffectAssetPath = null,
            string toSubEffectLabel = null,
            string toBaseEffectLabel = null,
            bool preferEffectLabelPlayback = false,
            bool playChangeOnTargetMate = false,
            bool playChangeOnBothMates = false,
            bool preferNonChangeNextWhenChangeTriggerMissing = false,
            bool compensateCurrentTriggerQueueDelay = false,
            float maxCurrentTriggerQueueDelaySeconds = 0f,
            bool useUnscaledSwapTiming = false,
            float nextMotionDelaySeconds = 0f,
            float sourceToEffectDelaySeconds = 0f,
            float nextMotionLeadInSeconds = 0f,
            float nextMotionMinDurationSeconds = 0f)
        {
            BaseId = baseId;
            DelaySeconds = Mathf.Max(0f, delaySeconds);
            ToSubDelaySeconds = toSubDelaySeconds >= 0f ? toSubDelaySeconds : DelaySeconds;
            ToBaseDelaySeconds = toBaseDelaySeconds >= 0f ? toBaseDelaySeconds : DelaySeconds;
            UseChangeMotion = useChangeMotion;
            UseTransitionDelayAsMinimum = useTransitionDelayAsMinimum;
            EffectAssetPath = effectAssetPath;
            ToSubEffectAssetPath = toSubEffectAssetPath ?? effectAssetPath;
            ToBaseEffectAssetPath = toBaseEffectAssetPath ?? effectAssetPath;
            ToSubEffectLabel = toSubEffectLabel;
            ToBaseEffectLabel = toBaseEffectLabel;
            PreferEffectLabelPlayback = preferEffectLabelPlayback;
            PlayChangeOnTargetMate = playChangeOnTargetMate;
            PlayChangeOnBothMates = playChangeOnBothMates;
            PreferNonChangeNextWhenChangeTriggerMissing = preferNonChangeNextWhenChangeTriggerMissing;
            CompensateCurrentTriggerQueueDelay = compensateCurrentTriggerQueueDelay;
            MaxCurrentTriggerQueueDelaySeconds = Mathf.Max(0f, maxCurrentTriggerQueueDelaySeconds);
            UseUnscaledSwapTiming = useUnscaledSwapTiming;
            NextMotionDelaySeconds = Mathf.Max(0f, nextMotionDelaySeconds);
            SourceToEffectDelaySeconds = Mathf.Max(0f, sourceToEffectDelaySeconds);
            NextMotionLeadInSeconds = Mathf.Max(0f, nextMotionLeadInSeconds);
            NextMotionMinDurationSeconds = Mathf.Max(0f, nextMotionMinDurationSeconds);
            ToSubTriggerPriority = toSubTriggerPriority != null && toSubTriggerPriority.Count > 0
                ? toSubTriggerPriority.ToArray()
                : DefaultToSubTriggers;
            ToBaseTriggerPriority = toBaseTriggerPriority != null && toBaseTriggerPriority.Count > 0
                ? toBaseTriggerPriority.ToArray()
                : DefaultToBaseTriggers;
            ToSubCurrentTriggerPriority = toSubCurrentTriggerPriority != null && toSubCurrentTriggerPriority.Count > 0
                ? toSubCurrentTriggerPriority.ToArray()
                : ToSubTriggerPriority;
            ToBaseCurrentTriggerPriority = toBaseCurrentTriggerPriority != null && toBaseCurrentTriggerPriority.Count > 0
                ? toBaseCurrentTriggerPriority.ToArray()
                : ToBaseTriggerPriority;
            ToSubNextTriggerPriority = toSubNextTriggerPriority != null && toSubNextTriggerPriority.Count > 0
                ? toSubNextTriggerPriority.ToArray()
                : ToSubCurrentTriggerPriority;
            ToBaseNextTriggerPriority = toBaseNextTriggerPriority != null && toBaseNextTriggerPriority.Count > 0
                ? toBaseNextTriggerPriority.ToArray()
                : ToBaseCurrentTriggerPriority;
            DuelStartTriggerPriority = duelStartTriggerPriority != null && duelStartTriggerPriority.Count > 0
                ? duelStartTriggerPriority.ToArray()
                : ToSubTriggerPriority;
        }

        public string GetEffectAssetPath(bool toSub)
        {
            return toSub ? ToSubEffectAssetPath : ToBaseEffectAssetPath;
        }

        public bool HasChangeEffectAsset(bool toSub)
        {
            var effectAssetPath = GetEffectAssetPath(toSub);
            if (string.IsNullOrEmpty(effectAssetPath))
                return false;

            var fullPath = Path.Combine(Program.root, effectAssetPath);
            return File.Exists(fullPath) || Directory.Exists(fullPath);
        }
    }

    public static class PremiumMateSwapEffects
    {
        private static readonly PremiumMateSwapEffect _default = new(0, 0.35f, true, string.Empty);

        private static readonly Dictionary<int, PremiumMateSwapEffect> _effectsByBaseId = new()
        {
            { 1000020, new PremiumMateSwapEffect(
                1000020,
                0.45f,
                true,
                "MasterDuel/Mate/fxp_14759_change_001",
                toSubEffectLabel: "HumanToDragon",
                toBaseEffectLabel: "DragonToHuman",
                useUnscaledSwapTiming: true) },
            { 1003001, new PremiumMateSwapEffect(
                1003001,
                1.30f,
                true,
                "MasterDuel/Mate/fxp_04044_change_001",
                useTransitionDelayAsMinimum: false,
                toSubTriggerPriority: new[] { "HorseToDragon", "Change", "Change2", "Change1", "ChangePreHide" },
                toBaseTriggerPriority: new[] { "DragonToHorse", "Change", "ChangeBack", "Change2", "ChangePreHide" },
                toSubEffectLabel: "HorseToDragon",
                toBaseEffectLabel: "DragonToHorse",
                preferEffectLabelPlayback: true) },
            { 1003002, new PremiumMateSwapEffect(
                1003002,
                0.75f,
                true,
                string.Empty,
                useTransitionDelayAsMinimum: false,
                toSubTriggerPriority: new[] { "Change", "Change2", "Change1", "ChangePreHide" },
                toBaseTriggerPriority: new[] { "Change", "ChangeBack", "Change2", "ChangePreHide" },
                toSubCurrentTriggerPriority: new[] { "ChangePreHide", "Change1", "Change2", "ChangeBack", "Change" },
                toSubNextTriggerPriority: new[] { "Change", "Change2", "Change1", "ChangePreHide" },
                playChangeOnTargetMate: true,
                playChangeOnBothMates: true) },
            { 1003003, new PremiumMateSwapEffect(
                1003003,
                1.60f,
                true,
                "MasterDuel/Mate/fxp_M13679_gate_02",
                toSubDelaySeconds: 0.90f,
                toBaseDelaySeconds: 1.95f,
                toBaseEffectAssetPath: string.Empty,
                toSubEffectLabel: "EngageToKagari",
                toBaseEffectLabel: "KagariToEngage",
                useUnscaledSwapTiming: true,
                duelStartTriggerPriority: new[] { "Change", "Change2", "Change1", "ChangePreHide" },
                playChangeOnTargetMate: true,
                playChangeOnBothMates: true,
                preferNonChangeNextWhenChangeTriggerMissing: true) },
            { 1003004, new PremiumMateSwapEffect(
                1003004,
                2.80f,
                true,
                "MasterDuel/Mate/fxp_M20196_flash",
                useTransitionDelayAsMinimum: false,
                toSubEffectLabel: "M20196ToM20215",
                toBaseEffectLabel: "M20215ToM20196",
                preferEffectLabelPlayback: false,
                playChangeOnTargetMate: true,
                playChangeOnBothMates: true,
                preferNonChangeNextWhenChangeTriggerMissing: false,
                compensateCurrentTriggerQueueDelay: true,
                maxCurrentTriggerQueueDelaySeconds: 1.50f,
                useUnscaledSwapTiming: true,
                nextMotionDelaySeconds: 0.45f,
                sourceToEffectDelaySeconds: 0.18f,
                nextMotionLeadInSeconds: 0.22f,
                nextMotionMinDurationSeconds: 1.85f,
                toSubNextTriggerPriority: new[] { "Change", "Entry", "Normal", "Change1", "ChangePreHide", "Change2" },
                toBaseNextTriggerPriority: new[] { "Change2", "Change", "Entry", "Normal", "ChangeBack", "ChangePreHide" }) },
            { 1003005, new PremiumMateSwapEffect(
                1003005,
                1.35f,
                true,
                string.Empty,
                useTransitionDelayAsMinimum: false,
                toSubTriggerPriority: new[] { "Change", "Change1", "ChangePreHide", "Change2" },
                toBaseTriggerPriority: new[] { "Change2", "ChangeBack", "ChangePreHide", "Change" },
                toSubCurrentTriggerPriority: new[] { "Change", "Change1", "ChangePreHide", "Change2" },
                toBaseCurrentTriggerPriority: new[] { "Change2", "ChangeBack", "ChangePreHide", "Change" },
                toSubNextTriggerPriority: new[] { "Entry", "Normal", "Change", "Change1", "ChangePreHide", "Change2" },
                toBaseNextTriggerPriority: new[] { "Entry", "Normal", "Change2", "ChangeBack", "ChangePreHide", "Change" },
                playChangeOnTargetMate: false,
                playChangeOnBothMates: false) },
        };

        public static PremiumMateSwapEffect GetOrDefault(int mateId)
        {
            var baseId = PremiumMateRules.GetBaseMateId(mateId);
            return _effectsByBaseId.TryGetValue(baseId, out var effect) ? effect : _default;
        }

        public static bool TryGet(int mateId, out PremiumMateSwapEffect effect)
        {
            var baseId = PremiumMateRules.GetBaseMateId(mateId);
            return _effectsByBaseId.TryGetValue(baseId, out effect);
        }
    }
}
