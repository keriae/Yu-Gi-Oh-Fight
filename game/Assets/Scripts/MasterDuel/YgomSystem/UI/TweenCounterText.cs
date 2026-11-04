using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YgomSystem.UI
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

		private TMP_Text tmpText;

		protected override void CaptureFrom()
		{
            text = GetComponent<MDText>();
            tmpText = GetComponent<TMP_Text>();
        }

		protected override void OnSetValue(float par)
		{
            if (tmpText == null)
                tmpText = GetComponent<TMP_Text>();
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
