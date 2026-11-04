using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YgomSystem.UI
{
	public class TweenColor : Tween
	{
		[SerializeField]
		[ColorLabelString]
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

		private List<KeyValuePair<Graphic, Color>> childGraps;

		protected override void CaptureFrom()
		{
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
                var g = GetComponent<Graphic>();
                if (g != null)
                    g.color = color;
            }
        }
	}
}
