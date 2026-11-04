using UnityEngine;

namespace MDPro3.UI.PropertyOverride
{
    public class PropertyOverrider_RectTransformScale : PropertyOverrider
    {
        [SerializeField] private OverrideProperty_Vector3 m_Scale;

        protected override void DefaultOverride()
        {
            base.DefaultOverride();
            var target = GetComponent<RectTransform>();
            target.localScale = m_Scale.m_DefaultValue;
        }

        protected override void MobileOverride()
        {
            base.MobileOverride();
            var target = GetComponent<RectTransform>();
            target.localScale = m_Scale.m_MobileValue;
        }

    }
}
