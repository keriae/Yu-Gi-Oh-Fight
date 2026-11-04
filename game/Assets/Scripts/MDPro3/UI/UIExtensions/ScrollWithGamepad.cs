using UnityEngine;
using UnityEngine.UI;
using MDPro3.Servant;

namespace MDPro3.UI
{
    [RequireComponent(typeof(Scrollbar))]
    public class ScrollWithGamepad : MonoBehaviour
    {
        private enum ResponseScrollWheel
        {
            Left,
            Right
        }
        [SerializeField] ResponseScrollWheel responseScrollWheel;

        private enum Direction
        {
            Vertical,
            Horizontal
        }
        [SerializeField] Direction direction;

        [SerializeField] float speed = 2.0f;

        private Scrollbar m_Scrollbar;
        private Scrollbar Scrollbar => 
            m_Scrollbar = m_Scrollbar != null ? m_Scrollbar
            : GetComponent<Scrollbar>();

        [SerializeField] Servant.Servant parentServant;

        private void Update()
        {
            if (Scrollbar == null)
                return;
            if (parentServant == null || !parentServant.NeedResponseInput())
                return;

            var offsets = responseScrollWheel == ResponseScrollWheel.Left
                ? UserInput.LeftScrollWheel : UserInput.RightScrollWheel;

            var offset = direction == Direction.Vertical ? offsets.y : offsets.x;

            Scrollbar.value = Mathf.Clamp01(Scrollbar.value + offset * speed * ScrollSpeed.WheelScale * Time.unscaledDeltaTime);
        }
    }
}
