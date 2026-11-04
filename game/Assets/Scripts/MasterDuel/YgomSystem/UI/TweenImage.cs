using UnityEngine;
using UnityEngine.UI;

namespace YgomSystem.UI
{
	public class TweenImage : Tween
	{
		[SerializeField]
		public Sprite[] frames;

		private Image image;

		protected override void CaptureFrom()
		{
            image = GetComponent<Image>();
        }

		protected override void OnSetValue(float par)
		{
            if (frames == null || frames.Length == 0)
                return;
            if (image == null)
                image = GetComponent<Image>();
            if (image == null)
                return;

            int idx = Mathf.Clamp(Mathf.FloorToInt(par * frames.Length), 0, frames.Length - 1);
            if (image.sprite != frames[idx])
                image.sprite = frames[idx];
        }
	}
}
