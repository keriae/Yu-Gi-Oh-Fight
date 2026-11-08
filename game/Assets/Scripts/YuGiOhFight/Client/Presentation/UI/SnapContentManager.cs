namespace YuGiOhFight.Client.Presentation.UI
{
	public class SnapContentManager : UnityEngine.MonoBehaviour, UnityEngine.EventSystems.IBeginDragHandler, UnityEngine.EventSystems.IEventSystemHandler, UnityEngine.EventSystems.IEndDragHandler
	{
		private const int TEMPLATE_NUM = 3;

		private int templateNum;

		private ScrollRectPageSnap m_pageSnap;

		private UnityEngine.UI.ScrollRect m_scrollRect;

		private int m_pastIdx;

		public int currentPage;

		public int m_maxPage;

		public string templateLabel;

		private System.Collections.Generic.List<UnityEngine.GameObject> templateGOList;

		public System.Action<UnityEngine.GameObject> onCreatedEntityCallback;

		public System.Action<UnityEngine.GameObject, int> onSetEntityCallback;

		public System.Action onPageChangedCallBack;

		private int m_pageBuff;

		private bool m_isTouching;

		private bool m_isLoop;

		public UnityEngine.UI.ScrollRect.ScrollRectEvent onValueChanged => null;

		public void InitializeContent(int maxPage, System.Action onCompleteCallBack = null, bool isLoop = false)
		{
		}

		public void SetFirstPage(int firstPage = 0)
		{
		}

		public bool ToNextPage()
		{
			return false;
		}

		public bool ToPrevPage()
		{
			return false;
		}

		public void SetScrollDisabled()
		{
		}

		public bool IsContainViewport(UnityEngine.GameObject entity)
		{
			return false;
		}

		private void OnIdxChanged()
		{
		}

		public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData eventData)
		{
		}

		public void OnEndDrag(UnityEngine.EventSystems.PointerEventData eventData)
		{
		}

		private void OnReleaseDrag(int idx)
		{
		}

		private void ChangePage(int diff)
		{
		}

		private void RearrangeTemplate(int idxDiff)
		{
		}
	}
}
