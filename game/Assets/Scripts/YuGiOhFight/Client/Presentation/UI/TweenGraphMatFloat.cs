using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class TweenGraphMatFloat : Tween
	{
		[SerializeField]
		public string field;

		[SerializeField]
		public float from;

		[SerializeField]
		public float to;

		private Effects.MaterialSetterGraphWriter m_TargetWriter;

		protected override void OnSetValue(float par)
		{
            if (m_TargetWriter == null)
                m_TargetWriter = GetComponent<Effects.MaterialSetterGraphWriter>();
            if (m_TargetWriter == null || m_TargetWriter.targetMaterial == null || string.IsNullOrEmpty(field))
                return;

            m_TargetWriter.targetMaterial.SetFloat(field, Mathf.Lerp(from, to, par));
        }
	}
}
