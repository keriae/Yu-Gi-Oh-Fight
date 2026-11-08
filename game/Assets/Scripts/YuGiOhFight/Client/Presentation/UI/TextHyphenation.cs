namespace YuGiOhFight.Client.Presentation.UI
{
	public class TextHyphenation : UnityEngine.MonoBehaviour
	{
		private bool doCallbackProc;

		private bool doFirstWrap;

		private MDText _Text;

		private string proctext;

		private string orgtext;

		private static System.Collections.Generic.SortedDictionary<char, bool> frontmap;

		private static System.Collections.Generic.SortedDictionary<char, bool> backmap;

		private static readonly string RITCH_TEXT_REPLACE;

		private static readonly char[] HYP_FRONT;

		private static readonly char[] HYP_BACK;

		private void DirtyLayoutCallback()
		{
		}

		private void Awake()
		{
		}

		private void Update()
		{
		}

		private System.Collections.Generic.List<string> SplitWrap(string txt)
		{
			return null;
		}

		private string WrapText(float rectwidth)
		{
			return null;
		}
	}
}
