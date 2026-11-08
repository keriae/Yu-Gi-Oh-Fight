namespace YuGiOhFight.Client.Presentation.UI
{
	public class uGuiParticleBase : UnityEngine.UI.MaskableGraphic
	{
		private struct param
		{
			public UnityEngine.Vector2 vel;

			public float rot;

			public float scale;

			public UnityEngine.Color col;

			public int sid;
		}

		private class prim
		{
			public uGuiParticleBase pbase;

			public float delay;

			public float time;

			public float lifetime;

			public param src;

			public param dst;

			private float pt;

			private UnityEngine.Vector2 ps;

			public UnityEngine.Color color => default(UnityEngine.Color);

			public UnityEngine.Vector2 pos => default(UnityEngine.Vector2);

			public float scale => 0f;

			public float rot => 0f;

			public int sid => 0;

			public prim(uGuiParticleBase pbase_, float lifetime_, float delaytime_, UnityEngine.Vector2 pos_, UnityEngine.Vector2 vel, float rot, float scale, UnityEngine.Color color, int sid, UnityEngine.Vector2 dstVel, float dstRot, float dstScale, UnityEngine.Color dstColor, int dstSid)
			{
			}

			public bool update(float tm)
			{
				return false;
			}

			public static UnityEngine.Vector2[] CreateUvs(int divide, UnityEngine.Texture tex)
			{
				return null;
			}

			public bool AddVert(UnityEngine.UI.VertexHelper vh, UnityEngine.Vector2[] uvs)
			{
				return false;
			}
		}

		public UnityEngine.Texture texture;

		public int divide;

		public UnityEngine.AnimationCurve colorCurve;

		public UnityEngine.AnimationCurve velocityCurve;

		public UnityEngine.AnimationCurve scaleCurve;

		private System.Collections.Generic.List<prim> particles;

		private UnityEngine.Vector2[] uvs;

		public override UnityEngine.Texture mainTexture => null;

		protected virtual void Update()
		{
		}

		public void AddParticle(float lifetime, float delaytime, UnityEngine.Vector2 pos, UnityEngine.Vector2 velocity, float rot, float scale, UnityEngine.Color color, int sid, UnityEngine.Vector2 endVelocity, float endRot, float endScale, UnityEngine.Color endColor, int endSid)
		{
		}

		protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
		{
		}
	}
}
