using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	[ExecuteAlways]
	public class ModifyEdgeVertScale : UnityEngine.UI.BaseMeshEffect
	{
		[SerializeField]
		private Vector2 m_UpperLeft;

		[SerializeField]
		private Vector2 m_UpperRight;

		[SerializeField]
		private Vector2 m_LowerLeft;

		[SerializeField]
		private Vector2 m_LowerRight;

		private System.Collections.Generic.List<UIVertex> m_VertsCache;

		public override void ModifyMesh(UnityEngine.UI.VertexHelper vh)
		{
		}
	}
}
