using MDPro3.UI.PropertyOverride;
using UnityEngine;

namespace MDPro3.UI.PropertyOverride
{
    public class PropertyOverrider_Transform : PropertyOverrider
    {
        [SerializeField] private OverrideProperty_Vector3 m_Position;
        [SerializeField] private OverrideProperty_Quaternion m_Rotation;
        [SerializeField] private OverrideProperty_Vector3 m_Scale;

        protected override void DefaultOverride()
        {
            base.DefaultOverride();
            transform.localPosition = m_Position.m_DefaultValue;
            transform.localRotation = m_Rotation.m_DefaultValue;
            transform.localScale = m_Scale.m_DefaultValue;
        }

        protected override void MobileOverride()
        {
            base.MobileOverride();
            transform.localPosition = m_Position.m_MobileValue;
            transform.localRotation = m_Rotation.m_MobileValue;
            transform.localScale = m_Scale.m_MobileValue;
        }
    }
}

