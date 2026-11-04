using UnityEngine;
using YgomSystem.Effect;

namespace YgomSystem.UI
{
	public class TweenGraphMatFloat : Tween
	{
		[SerializeField]
		public string field;

		[SerializeField]
		public float from;

		[SerializeField]
		public float to;

		private MaterialSetterGraphWriter m_TargetWriter;

		protected override void OnSetValue(float par)
		{
            if (m_TargetWriter == null)
                m_TargetWriter = GetComponent<MaterialSetterGraphWriter>();
            if (m_TargetWriter == null || m_TargetWriter.targetMaterial == null || string.IsNullOrEmpty(field))
                return;

            m_TargetWriter.targetMaterial.SetFloat(field, Mathf.Lerp(from, to, par));
        }
	}
}
