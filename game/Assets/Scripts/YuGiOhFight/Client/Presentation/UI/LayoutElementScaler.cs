using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class LayoutElementScaler : UnityEngine.EventSystems.UIBehaviour, UnityEngine.UI.ILayoutElement
	{
		[SerializeField]
		private int m_LayoutPriority;

		[SerializeField]
		private float m_WidthScale;

		[SerializeField]
		private float m_HeightScale;

		private UnityEngine.UI.ILayoutElement m_LayoutElementCache;

		public UnityEngine.UI.ILayoutElement layoutElement => null;

		public int layoutPriority
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		public float widthScale
		{
			get
			{
				return 0f;
			}
			set
			{
			}
		}

		public float heightScale
		{
			get
			{
				return 0f;
			}
			set
			{
			}
		}

		public float minWidth => 0f;

		public float preferredWidth => 0f;

		public float flexibleWidth => 0f;

		public float minHeight => 0f;

		public float preferredHeight => 0f;

		public float flexibleHeight => 0f;

		public void CalculateLayoutInputHorizontal()
		{
		}

		public void CalculateLayoutInputVertical()
		{
		}

		protected void SetDirty()
		{
		}

		private System.Collections.IEnumerator DelayedSetDirty(RectTransform rectTransform)
		{
			return null;
		}
	}
}
