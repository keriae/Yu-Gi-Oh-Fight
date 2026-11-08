using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	[ExecuteAlways]
	public class UGUIParticle : UnityEngine.UI.MaskableGraphic
	{
		private Camera m_RenderCameraCache;

		private ParticleSystemRenderer m_ParticleSystemRendererCache;

		private Mesh m_Mesh;

		private System.Collections.Generic.List<Vector3> m_Vertices;

		private System.Collections.Generic.List<Vector3> m_UVs;

		private System.Collections.Generic.List<int> m_Triangles;

		private System.Collections.Generic.List<Color32> m_Colors32;

		private Camera renderCamera => null;

		public ParticleSystemRenderer particleSystemRenderer => null;

		public override Texture mainTexture => null;

		public override Material material
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		protected override void Start()
		{
		}

		private void Update()
		{
		}

		protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
		{
		}

		protected override void OnDidApplyAnimationProperties()
		{
		}
	}
}
