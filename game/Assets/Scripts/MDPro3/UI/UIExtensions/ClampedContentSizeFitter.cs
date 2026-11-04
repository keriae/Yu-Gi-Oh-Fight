using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI
{
    public class ClampedContentSizeFitter : ContentSizeFitter
    {
        [SerializeField] private float m_MaxWidth = -1f;
        public float maxWidth { get => m_MaxWidth; set => m_MaxWidth = value; }

        [SerializeField] private float m_MinWidth = 0f;
        public float minWidth { get => m_MinWidth; set => m_MinWidth = value; }

        private DrivenRectTransformTracker m_Tracker;
        private RectTransform m_Rect;
        private RectTransform RectTransform => m_Rect ??= GetComponent<RectTransform>();

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }

        protected override void OnDisable()
        {
            m_Tracker.Clear();
            base.OnDisable();
        }

        public override void SetLayoutHorizontal()
        {
            m_Tracker.Clear();

            if (horizontalFit == FitMode.Unconstrained)
            {
                m_Tracker.Add(this, RectTransform, DrivenTransformProperties.None);
                return;
            }

            m_Tracker.Add(this, RectTransform, DrivenTransformProperties.SizeDeltaX);

            // 计算原始目标宽度
            float targetWidth = horizontalFit == FitMode.MinSize ?
                LayoutUtility.GetMinWidth(RectTransform) :
                LayoutUtility.GetPreferredWidth(RectTransform);

            // 应用最小和最大宽度约束
            if (m_MinWidth > 0)
                targetWidth = Mathf.Max(targetWidth, m_MinWidth); // 确保不小于最小值
            if (m_MaxWidth > 0)
                targetWidth = Mathf.Min(targetWidth, m_MaxWidth); // 确保不大于最大值

            RectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                targetWidth
            );
        }
    }
}