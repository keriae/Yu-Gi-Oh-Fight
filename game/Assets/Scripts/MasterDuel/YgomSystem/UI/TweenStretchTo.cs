using System;
using UnityEngine;

namespace YgomSystem.UI
{
	public class TweenStretchTo : Tween
	{
		[Serializable]
		public struct StretchOffset
		{
			public float left;

			public float right;

			public float top;

			public float bottom;

			public static StretchOffset operator +(StretchOffset a, StretchOffset b)
			{
                return new StretchOffset { left = a.left + b.left, right = a.right + b.right, top = a.top + b.top, bottom = a.bottom + b.bottom };
            }

			public static StretchOffset operator *(StretchOffset a, StretchOffset b)
			{
                return new StretchOffset { left = a.left * b.left, right = a.right * b.right, top = a.top * b.top, bottom = a.bottom * b.bottom };
            }

			public static StretchOffset operator *(StretchOffset a, float b)
			{
                return new StretchOffset { left = a.left * b, right = a.right * b, top = a.top * b, bottom = a.bottom * b };
            }

			public static StretchOffset operator -(StretchOffset a, StretchOffset b)
			{
                return new StretchOffset { left = a.left - b.left, right = a.right - b.right, top = a.top - b.top, bottom = a.bottom - b.bottom };
            }
		}

		protected StretchOffset m_From;

		[SerializeField]
		public StretchOffset to;

		[SerializeField]
		public bool ResetFromOnEnable;

		private RectTransform m_RectTransform_Field;

		protected RectTransform m_RectTransform => m_RectTransform_Field != null ? m_RectTransform_Field : (m_RectTransform_Field = GetComponent<RectTransform>());

		private void OnEnable()
		{
            if (ResetFromOnEnable)
                CaptureFrom();
        }

		protected override void CaptureFrom()
		{
            var rt = m_RectTransform;
            m_From = new StretchOffset
            {
                left = rt.offsetMin.x,
                right = -rt.offsetMax.x,
                top = -rt.offsetMax.y,
                bottom = rt.offsetMin.y
            };
        }

		protected override void OnSetValue(float par)
		{
            var rt = m_RectTransform;
            StretchOffset crnt = m_From + (to - m_From) * par;
            rt.offsetMin = new Vector2(crnt.left, crnt.bottom);
            rt.offsetMax = new Vector2(-crnt.right, -crnt.top);
        }
	}
}
