using System.Runtime.CompilerServices;


namespace YuGiOhFight.Client.Presentation.Effects
{
	public class SpriteScalerManager : UnityEngine.MonoBehaviour
	{
		private UnityEngine.Vector2 currentScreenSize;

		private static System.Collections.Generic.List<SpriteScaler> scalerList;

		public static bool reqUpdate
		{
			[CompilerGenerated]
			get
			{
				return false;
			}
			[CompilerGenerated]
			set
			{
			}
		}

		private void Update()
		{
		}

		public static void Add(SpriteScaler screen)
		{
		}

		public static void Remove(SpriteScaler screen)
		{
		}
	}
}
