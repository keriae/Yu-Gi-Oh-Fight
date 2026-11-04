using System.Collections.Generic;
using UnityEngine;

namespace YgomSystem.UI
{
	public class TweenSpriteColor : Tween
	{
		[ColorLabelString]
		[SerializeField]
		public string fromLabel;

		[SerializeField]
		public Color from;

		[ColorLabelString]
		[SerializeField]
		public string toLabel;

		[SerializeField]
		public Color to;

		public bool isOverride;

		public bool isRecusive;

		private List<KeyValuePair<SpriteRenderer, Color>> childGraps;

		protected override void CaptureFrom()
		{
            if (isRecusive)
            {
                childGraps = new List<KeyValuePair<SpriteRenderer, Color>>();
                var renderers = GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    var r = renderers[i];
                    if (r != null)
                        childGraps.Add(new KeyValuePair<SpriteRenderer, Color>(r, r.color));
                }
            }
            else
            {
                childGraps = null;
            }
        }

		protected override void OnSetValue(float par)
		{
            Color color = Color.Lerp(from, to, par);
            if (isRecusive && childGraps != null)
            {
                for (int i = 0; i < childGraps.Count; i++)
                {
                    var pair = childGraps[i];
                    if (pair.Key == null)
                        continue;
                    pair.Key.color = color;
                }
            }
            else
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.color = color;
            }
        }
    }
}
