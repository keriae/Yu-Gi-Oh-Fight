using UnityEngine;

namespace MDPro3.UI.PropertyOverride
{
    public class PropertyOverrider_UIScrollToSelection : PropertyOverrider
    {
        [SerializeField] private OverrideProperty_Float m_TopPadding;
        [SerializeField] private OverrideProperty_Float m_BottomPadding;

        protected override void DefaultOverride()
        {
            base.DefaultOverride();
            var target = GetComponent<UIScrollToSelection>();
            target.topPadding = m_TopPadding.m_DefaultValue;
            target.bottomPadding = m_BottomPadding.m_DefaultValue;
        }

        protected override void MobileOverride()
        {
            base.MobileOverride();
            var target = GetComponent<UIScrollToSelection>();
            target.topPadding = m_TopPadding.m_MobileValue;
            target.bottomPadding = m_BottomPadding.m_MobileValue;
        }
    }
}

