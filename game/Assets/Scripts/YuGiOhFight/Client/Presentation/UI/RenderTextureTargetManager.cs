namespace YuGiOhFight.Client.Presentation.UI
{
	public class RenderTextureTargetManager : UnityEngine.MonoBehaviour
	{
		private int targetCount;

		private UnityEngine.GameObject lightObject;

		private System.Collections.Generic.List<int> useIdxList;

		private System.Collections.Generic.List<int> removeIdxList;

		private System.Collections.Generic.List<RenderTextureTarget> renderTextureTargetList;

		private static RenderTextureTargetManager instance;

		private static bool isQuitting;

		public static RenderTextureTargetManager Instance => null;

		private void OnApplicationQuit()
		{
		}

		public static void Reboot()
		{
		}

		public static void CreateManager()
		{
		}

		public static int Create(System.Action<RenderTextureTarget> onFinish)
		{
			return 0;
		}

		public static void DestroyTarget(int id)
		{
		}

		public static UnityEngine.Camera GetTargetCamera(int id)
		{
			return null;
		}

		private int GetUnusedId()
		{
			return 0;
		}
	}
}
