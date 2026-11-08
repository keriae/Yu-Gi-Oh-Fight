namespace YuGiOhFight.Client.Presentation.UI
{
	//[CreateAssetMenu]
	public class TweenColorInfo : TweenGenerateInfo
	{
		[ColorLabelString]
		public string fromLabel;

		public UnityEngine.Color from;

		[ColorLabelString]
		public string toLabel;

		public UnityEngine.Color to;

		public bool isOverride;

		public bool isRecusive;
	}
}
