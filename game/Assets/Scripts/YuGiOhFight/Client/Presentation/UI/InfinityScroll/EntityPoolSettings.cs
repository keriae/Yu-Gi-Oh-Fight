using System;
using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI.InfinityScroll
{
	[Serializable]
	public class EntityPoolSettings
	{
		public enum Alignment
		{
			Begin = 0,
			Center = 1,
			End = 2
		}

		[SerializeField]
		private UnityEngine.UI.GridLayoutGroup.Axis m_ScrollAxis;

		[SerializeField]
		private Alignment m_Alignment;

		[SerializeField]
		private bool m_InstantiateAllTemplates;

		public UnityEngine.UI.GridLayoutGroup.Axis scrollAxis => default(UnityEngine.UI.GridLayoutGroup.Axis);

		public Alignment alignment => default(Alignment);

		public bool isInstaintiateAllTemplates
		{
			get
			{
				return false;
			}
			set
			{
			}
		}
	}
}
