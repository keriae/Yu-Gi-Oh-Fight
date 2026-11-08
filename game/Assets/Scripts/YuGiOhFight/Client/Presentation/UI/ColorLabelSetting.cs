using System;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class ColorLabelSetting : UnityEngine.ScriptableObject
	{
		[Serializable]
		public class ValueContainer
		{
			public string label;

			public UnityEngine.Color color;

			public ValueContainer Copy()
			{
				return null;
			}
		}

		public System.Collections.Generic.List<ValueContainer> list;

		private System.Collections.Generic.Dictionary<string, ValueContainer> m_labelMap;

		public void Setup()
		{
		}

		public ValueContainer Get(string label)
		{
			return null;
		}

		public bool Get(string label, out UnityEngine.Color res)
		{
			res = default(UnityEngine.Color);
			return false;
		}
	}
}
