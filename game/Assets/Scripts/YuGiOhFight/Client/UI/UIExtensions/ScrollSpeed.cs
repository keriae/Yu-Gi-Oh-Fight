using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
{
    // Run before EventSystem so newly loaded views use the same wheel speed on
    // their first input. Dragging and scrollbar positions keep their native behaviour.
    [DefaultExecutionOrder(-10000)]
    public class ScrollSpeed : MonoBehaviour
    {
        public const float WheelScale = 0.1f;
        private const float MaximumSensitivity = 2.4f;
        private static readonly Dictionary<ScrollRect, float> original = new();
        private static readonly List<ScrollRect> destroyed = new();

        public static void Apply(GameObject root)
        {
            foreach (var scroll in root.GetComponentsInChildren<ScrollRect>(true))
                Apply(scroll);
        }

        private static void Apply(ScrollRect scroll)
        {
            if (!original.TryGetValue(scroll, out var sensitivity))
            {
                sensitivity = scroll.scrollSensitivity;
                original.Add(scroll, sensitivity);
            }
            scroll.scrollSensitivity = Mathf.Min(sensitivity * WheelScale, MaximumSensitivity);
        }

        private void Update()
        {
            if (Mouse.current == null || Mouse.current.scroll.ReadValue() == Vector2.zero)
                return;

            // A few bundle widgets are created without UIManager.Translate.
            // Discover them only during wheel input, rather than scanning each frame.
            foreach (var scroll in FindObjectsByType<ScrollRect>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Apply(scroll);

            destroyed.Clear();
            foreach (var pair in original)
                if (pair.Key == null)
                    destroyed.Add(pair.Key);
            foreach (var scroll in destroyed)
                original.Remove(scroll);
        }

        private void OnDestroy()
        {
            original.Clear();
            destroyed.Clear();
        }
    }
}
