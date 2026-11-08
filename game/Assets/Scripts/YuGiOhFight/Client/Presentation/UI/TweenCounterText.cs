using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class TweenCounterText : Tween
	{
		[SerializeField]
		public string format;

		[SerializeField]
		public float from;

		[SerializeField]
		public float to;

		private MDText text;

		private TMPro.TMP_Text tmpText;

		protected override void CaptureFrom()
		{
            text = GetComponent<MDText>();
            tmpText = GetComponent<TMPro.TMP_Text>();
        }

		protected override void OnSetValue(float par)
		{
            if (tmpText == null)
                tmpText = GetComponent<TMPro.TMP_Text>();
            if (tmpText == null)
                return;

            tmpText.text = GetCurrentCounterText(par);
        }

		private string GetCurrentCounterText(float par)
		{
            float value = Mathf.Lerp(from, to, par);
            if (string.IsNullOrEmpty(format))
                return value.ToString();
            return string.Format(format, value);
        }
	}
}
