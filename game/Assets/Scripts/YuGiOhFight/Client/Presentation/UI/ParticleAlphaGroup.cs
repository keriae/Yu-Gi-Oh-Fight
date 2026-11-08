using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class ParticleAlphaGroup : MonoBehaviour
	{
		[SerializeField]
		private float m_Alpha;

		private System.Collections.Generic.List<ParticleAlphaGroupTarget> m_Targets;

		public float alpha
		{
			get
			{
				return 0f;
			}
			set
			{
			}
		}

		public void AssignTarget(ParticleAlphaGroupTarget target)
		{
		}

		public void RemoveTarget(ParticleAlphaGroupTarget target)
		{
		}
	}
}
