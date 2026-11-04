using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YgomSystem.UI
{
	public class TweenAlpha : Tween
	{
		[SerializeField]
		public float from;

		[SerializeField]
		public float to;

		public bool isRecusive;

		private CanvasGroup canvasGroup;

		private List<KeyValuePair<Graphic, Color>> childGraps;

		protected override void CaptureFrom()
		{
            canvasGroup = GetComponent<CanvasGroup>();
            if (isRecusive)
            {
                childGraps = new List<KeyValuePair<Graphic, Color>>();
                var graphics = GetComponentsInChildren<Graphic>(true);
                for (int i = 0; i < graphics.Length; i++)
                {
                    var g = graphics[i];
                    if (g != null)
                        childGraps.Add(new KeyValuePair<Graphic, Color>(g, g.color));
                }
            }
            else
            {
                childGraps = null;
            }
        }

		protected override void OnSetValue(float par)
		{
            float alpha = Mathf.Lerp(from, to, par);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
                return;
            }

            if (isRecusive && childGraps != null)
            {
                for (int i = 0; i < childGraps.Count; i++)
                {
                    var pair = childGraps[i];
                    if (pair.Key == null)
                        continue;
                    var c = pair.Value;
                    c.a = alpha;
                    pair.Key.color = c;
                }
            }
            else
            {
                var g = GetComponent<Graphic>();
                if (g != null)
                {
                    var c = g.color;
                    c.a = alpha;
                    g.color = c;
                }
            }
        }
	}
}
