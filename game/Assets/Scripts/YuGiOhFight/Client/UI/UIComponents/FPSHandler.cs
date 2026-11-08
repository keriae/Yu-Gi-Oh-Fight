using UnityEngine;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
{
    public class FPSHandler : UIHandler
    {
        private double m_lastUpdateShowTime;
        private const double UpdateInterval = 0.5;
        private int m_frames = 0;
        private bool m_sampling;
        private readonly Vector3[] m_corners = new Vector3[4];

        public Text text;

        public override void Initialize()
        {
            ResetSample();
        }

        private void OnEnable()
        {
            ResetSample();
        }

        private void ResetSample()
        {
            m_frames = 0;
            m_sampling = false;
            SetLabel("— FPS");
        }

        private void SetLabel(string label)
        {
            if (text == null)
                return;
            if (text.text != label)
            {
                text.text = label;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                    Mathf.Max(text.rectTransform.rect.width, text.preferredWidth + 12f));
            }
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            if (text == null || text.canvas == null)
                return;
            var canvas = text.canvas.rootCanvas.transform as RectTransform;
            var parent = text.rectTransform.parent as RectTransform;
            if (canvas == null || parent == null)
                return;
            text.rectTransform.GetWorldCorners(m_corners);
            var left = canvas.InverseTransformPoint(m_corners[0]).x;
            var right = canvas.InverseTransformPoint(m_corners[2]).x;
            var offset = Mathf.Max(0f, canvas.rect.xMin + 8f - left)
                + Mathf.Min(0f, canvas.rect.xMax - 8f - right);
            if (Mathf.Abs(offset) < 0.01f)
                return;
            var delta = parent.InverseTransformVector(canvas.TransformVector(new Vector3(offset, 0f, 0f)));
            text.rectTransform.anchoredPosition += new Vector2(delta.x, delta.y);
        }

        public override void PerframeFunction()
        {
            // UIManager invokes this handler even while its object is hidden.
            if (!isActiveAndEnabled || text == null)
                return;
            var now = Time.realtimeSinceStartupAsDouble;
            if (!m_sampling)
            {
                m_lastUpdateShowTime = now;
                m_sampling = true;
                return;
            }
            m_frames++;
            var elapsed = now - m_lastUpdateShowTime;
            if (elapsed >= UpdateInterval)
            {
                var fps = Mathf.RoundToInt((float)(m_frames / elapsed));
                m_lastUpdateShowTime = now;
                m_frames = 0;
                SetLabel(fps + " FPS");
            }
        }
    }
}
