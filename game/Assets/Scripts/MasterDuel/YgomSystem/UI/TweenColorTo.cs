using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YgomSystem.UI
{
	public class TweenColorTo : Tween
	{
		[SerializeField]
		public ColorLabelProperty toLabel;

		[SerializeField]
		public Color to;

		public bool isRecusive;

		private List<KeyValuePair<Graphic, Color>> childGraps;

		private Color fromColor;

		protected override void CaptureFrom()
		{
            if (toLabel != null && !string.IsNullOrEmpty(toLabel.label))
                to = toLabel.GetColor();

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
                var g = GetComponent<Graphic>();
                fromColor = g != null ? g.color : Color.white;
            }
        }

		protected override void OnSetValue(float par)
		{
            if (isRecusive && childGraps != null)
            {
                for (int i = 0; i < childGraps.Count; i++)
                {
                    var pair = childGraps[i];
                    if (pair.Key == null)
                        continue;
                    pair.Key.color = Color.Lerp(pair.Value, to, par);
                }
            }
            else
            {
                var g = GetComponent<Graphic>();
                if (g != null)
                    g.color = Color.Lerp(fromColor, to, par);
            }
        }
	}
}
