namespace YuGiOhFight.Client.Presentation.Effects
{
	public class ScreenFiller : UnityEngine.MonoBehaviour
	{
		private Elements.ElementObjectManager elements;

		private UnityEngine.SpriteRenderer fillSprite;

		private static ScreenFiller instance;

		private float time;

		private float fadeTime;

		private const float DefaultFadeTime = 0.2f;

		private UnityEngine.Color startColor;

		private UnityEngine.Color targetColor;

		private bool isLock;

		public void Initialize()
		{
		}

		public void SetSpriteColor(UnityEngine.Color color)
		{
		}

		public void StartFade(UnityEngine.Color targetColor, float fadeTime = 0.2f)
		{
		}

		private void UpdateFade(bool force = false)
		{
		}

		private void SetLock(bool isLock)
		{
		}

		public static void SetFillColor(UnityEngine.Color color)
		{
		}

		public static void StartFillFade(UnityEngine.Color color, float fadeTime = 0.2f)
		{
		}

		public static void SetFillLock(bool isLock)
		{
		}

		private void CheckEnabled()
		{
		}

		private void Update()
		{
		}
	}
}
