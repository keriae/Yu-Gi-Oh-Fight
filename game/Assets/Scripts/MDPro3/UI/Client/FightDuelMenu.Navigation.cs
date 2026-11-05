using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MDPro3.UI.Client
{
    internal sealed partial class FightDuelMenu
    {
        private Selectable[] FocusControls()
        {
            var scope = dialog != null ? dialog : window;
            return scope == null ? new Selectable[0] : scope.GetComponentsInChildren<Selectable>()
                .Where(item => item.IsActive() && item.IsInteractable() && !(item is Scrollbar)).ToArray();
        }
        private void Focus(Selectable control)
        {
            if (control == null || EventSystem.current == null) return;
            control.Select(); focusedControl = control.gameObject;
            Canvas.ForceUpdateCanvases(); FightUI.RevealControl(control);
        }
        private void KeepFocusInScope()
        {
            if (!Showing || UIManager.InputBlocker != this || EventSystem.current == null) return;
            var scope = dialog != null ? dialog : window;
            var current = EventSystem.current.currentSelectedGameObject;
            if (current == null || scope == null) { focusedControl = null; return; }
            if (!current.transform.IsChildOf(scope))
            {
                var retained = focusedControl == null ? null : focusedControl.GetComponent<Selectable>();
                Focus(retained != null && retained.IsActive() && retained.transform.IsChildOf(scope) ? retained : FocusControls().FirstOrDefault());
            }
            else if (current != focusedControl)
            {
                focusedControl = current;
                var control = current.GetComponent<Selectable>();
                if (control != null) FightUI.RevealControl(control);
            }
        }
        // Explicit links keep directional input inside the topmost dialog.
        // Horizontal slider input remains available for adjusting its value.
        private void RefreshNavigation()
        {
            var controls = FocusControls();
            var positions = controls.Select(FocusPosition).ToArray();
            for (int index = 0; index < controls.Length; index++)
            {
                var control = controls[index];
                var navigation = new Navigation { mode = Navigation.Mode.Explicit };
                navigation.selectOnUp = NearestControl(controls, positions, index, Vector2.up);
                navigation.selectOnDown = NearestControl(controls, positions, index, Vector2.down);
                if (!(control is Slider) && !(control is TMP_InputField))
                {
                    navigation.selectOnLeft = NearestControl(controls, positions, index, Vector2.left);
                    navigation.selectOnRight = NearestControl(controls, positions, index, Vector2.right);
                }
                control.navigation = navigation;
            }
        }
        private static Selectable NearestControl(Selectable[] controls, Vector2[] positions, int from, Vector2 direction)
        {
            var origin = positions[from]; float best = 0; Selectable result = null;
            for (int index = 0; index < controls.Length; index++)
            {
                if (index == from) continue;
                Vector2 delta = positions[index] - origin;
                float projection = Vector2.Dot(delta, direction);
                if (projection <= .01f) continue;
                float score = projection / delta.sqrMagnitude;
                if (score > best) { best = score; result = controls[index]; }
            }
            return result;
        }
        private static Vector2 FocusPosition(Selectable control)
        {
            var rect = control.transform as RectTransform;
            var position = rect.TransformPoint(rect.rect.center);
            foreach (var scroll in control.GetComponentsInParent<ScrollRect>())
                if (scroll.content != null && control.transform.IsChildOf(scroll.content))
                    position -= scroll.content.parent.TransformVector(scroll.content.anchoredPosition);
            return position;
        }
    }
}
