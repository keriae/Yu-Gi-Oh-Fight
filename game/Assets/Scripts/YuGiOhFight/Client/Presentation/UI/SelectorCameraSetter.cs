namespace YuGiOhFight.Client.Presentation.UI
{
	public class SelectorCameraSetter : UnityEngine.MonoBehaviour
	{
		public enum FindType
		{
			ByParentCanvas = 0,
			ByScreenEffect = 1
		}

		public FindType findType;

		public Effects.ScreenEffect targetScreenEffect;

		private void Start()
		{
		}

		private UnityEngine.Camera FindCamera()
		{
			return null;
		}

		public static UnityEngine.Camera FindCameraByParentCanvas(UnityEngine.GameObject target)
		{
			return null;
		}

		public static UnityEngine.Camera FindCameraByScreenEffect(UnityEngine.GameObject target, Effects.ScreenEffect screenEffect)
		{
			return null;
		}
	}
}
