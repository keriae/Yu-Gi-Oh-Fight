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
            if (scope == null) { focusedControl = null; return; }
            var currentControl = current == null ? null : current.GetComponent<Selectable>();
            bool inScope = current != null && current.transform.IsChildOf(scope);
            bool navigable = NavigableControl(currentControl, scope);
            // Pointer clicks on non-selectable content can clear Unity's
            // selection. Remember its last control without reactivating a
            // text field or snapping the scroll while the pointer is in use.
            // Scrollbar dragging also selects a control, but should preserve
            // the last navigable row for subsequent keyboard/controller input.
            if ((current == null || inScope && !navigable) && !NavigationRequested()) return;
            if (!inScope || !navigable)
            {
                var retained = focusedControl == null ? null : focusedControl.GetComponent<Selectable>();
                Focus(NavigableControl(retained, scope) ? retained : FocusControls().FirstOrDefault());
            }
            else if (current != focusedControl)
            {
                focusedControl = current;
                FightUI.RevealControl(currentControl);
            }
        }
        private static bool NavigableControl(Selectable control, Transform scope) => control != null && !(control is Scrollbar)
            && control.IsActive() && control.IsInteractable() && control.transform.IsChildOf(scope);
        private static bool NavigationRequested()
        {
            if (UserInput.MoveInput.sqrMagnitude > .0001f || UserInput.WasSubmitPressed) return true;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            return keyboard != null && (keyboard.upArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame
                || keyboard.leftArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame
                || keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame);
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
