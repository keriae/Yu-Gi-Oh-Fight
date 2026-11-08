using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
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

		private System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<UnityEngine.UI.Graphic, Color>> childGraps;

		protected override void CaptureFrom()
		{
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
            }
        }

		protected override void OnSetValue(float par)
		{
            Color color = Color.Lerp(from, to, par);
            if (isRecusive && childGraps != null)
            {
                for (int i = 0; i < childGraps.Count; i++)
                {
                    System.Collections.Generic.KeyValuePair<UnityEngine.UI.Graphic, Color> pair = childGraps[i];
                    if (pair.Key == null)
                        continue;
                    pair.Key.color = color;
                }
            }
            else
            {
                UnityEngine.UI.Graphic g = GetComponent<UnityEngine.UI.Graphic>();
                if (g != null)
                    g.color = color;
            }
        }
	}
}
