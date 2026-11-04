using UnityEngine;
using System;

namespace MDPro3.UI
{
    public class EventSEPlayer : MonoBehaviour
    {
        public static float LastEventTime { get; private set; } = float.NegativeInfinity;
        public static string LastEventLabel { get; private set; } = string.Empty;

        private void PlayAnimationEventSe(string se)
        {
            RegisterEvent(se);
            AudioManager.PlaySE(se, 0.4f);
        }
        private void NewEvent(string se)
        {
            RegisterEvent(se);
            AudioManager.PlaySE(se, 0.4f);
        }

        public static bool HasRecentEvent(string expectedPrefix, float sinceTime)
        {
            if (LastEventTime < sinceTime)
                return false;
            if (string.IsNullOrEmpty(expectedPrefix))
                return true;

            return !string.IsNullOrEmpty(LastEventLabel)
                && LastEventLabel.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase);
        }

        private static void RegisterEvent(string se)
        {
            LastEventTime = Time.unscaledTime;
            LastEventLabel = se ?? string.Empty;
        }
    }
}
