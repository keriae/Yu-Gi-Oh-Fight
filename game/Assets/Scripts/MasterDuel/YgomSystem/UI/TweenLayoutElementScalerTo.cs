using UnityEngine;

namespace YgomSystem.UI
{
	public class TweenLayoutElementScalerTo : Tween
	{
		[SerializeField]
		private float m_ToWidthScale;

		[SerializeField]
		private float m_ToHeightScale;

		private float m_FromWidthScale;

		private float m_FromHeightScale;

		private LayoutElementScaler m_LayoutElementScalerCache;

		private LayoutElementScaler layoutElementScaler => m_LayoutElementScalerCache != null ? m_LayoutElementScalerCache : (m_LayoutElementScalerCache = GetComponent<LayoutElementScaler>());

		protected override void CaptureFrom()
		{
            var scaler = layoutElementScaler;
            if (scaler == null)
                return;
            m_FromWidthScale = scaler.widthScale;
            m_FromHeightScale = scaler.heightScale;
        }

		protected override void OnSetValue(float par)
		{
            var scaler = layoutElementScaler;
            if (scaler == null)
                return;
            scaler.widthScale = Mathf.Lerp(m_FromWidthScale, m_ToWidthScale, par);
            scaler.heightScale = Mathf.Lerp(m_FromHeightScale, m_ToHeightScale, par);
        }
	}
}
