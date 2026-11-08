using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class TweenAlpha : Tween
	{
		[SerializeField]
		public float from;

		[SerializeField]
		public float to;

		public bool isRecusive;

		private CanvasGroup canvasGroup;

		private System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<UnityEngine.UI.Graphic, Color>> childGraps;

		protected override void CaptureFrom()
		{
            canvasGroup = GetComponent<CanvasGroup>();
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
                    System.Collections.Generic.KeyValuePair<UnityEngine.UI.Graphic, Color> pair = childGraps[i];
                    if (pair.Key == null)
                        continue;
                    Color c = pair.Value;
                    c.a = alpha;
                    pair.Key.color = c;
                }
            }
            else
            {
                UnityEngine.UI.Graphic g = GetComponent<UnityEngine.UI.Graphic>();
                if (g != null)
                {
                    Color c = g.color;
                    c.a = alpha;
                    g.color = c;
                }
            }
        }
	}
}
