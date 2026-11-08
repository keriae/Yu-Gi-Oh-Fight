using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class TweenColorTo : Tween
	{
		[SerializeField]
		public ColorLabelProperty toLabel;

		[SerializeField]
		public Color to;

		public bool isRecusive;

		private System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<UnityEngine.UI.Graphic, Color>> childGraps;

		private Color fromColor;

		protected override void CaptureFrom()
		{
            if (toLabel != null && !string.IsNullOrEmpty(toLabel.label))
                to = toLabel.GetColor();

            if (isRecusive)
            {
                childGraps = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<UnityEngine.UI.Graphic, Color>>();
                var graphics = GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
                for (int i = 0; i < graphics.Length; i++)
                {
                    UnityEngine.UI.Graphic g = graphics[i];
                    if (g != null)
                        childGraps.Add(new System.Collections.Generic.KeyValuePair<UnityEngine.UI.Graphic, Color>(g, g.color));
                }
            }
            else
            {
                childGraps = null;
                UnityEngine.UI.Graphic g = GetComponent<UnityEngine.UI.Graphic>();
                fromColor = g != null ? g.color : Color.white;
            }
        }

		protected override void OnSetValue(float par)
		{
            if (isRecusive && childGraps != null)
            {
                for (int i = 0; i < childGraps.Count; i++)
                {
                    System.Collections.Generic.KeyValuePair<UnityEngine.UI.Graphic, Color> pair = childGraps[i];
                    if (pair.Key == null)
                        continue;
                    pair.Key.color = Color.Lerp(pair.Value, to, par);
                }
            }
            else
            {
                UnityEngine.UI.Graphic g = GetComponent<UnityEngine.UI.Graphic>();
                if (g != null)
                    g.color = Color.Lerp(fromColor, to, par);
            }
        }
	}
}
