using System;


namespace YuGiOhFight.Client.Presentation.UI
{
	[Serializable]
	public class ColorLabelProperty
	{
		[ColorLabelString]
		public string label;

		public UnityEngine.Color rgba;

		public UnityEngine.Color GetColor()
		{
			return default(UnityEngine.Color);
		}
	}
}
