using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class ScrollEventSender : MonoBehaviour, UnityEngine.EventSystems.IInitializePotentialDragHandler, UnityEngine.EventSystems.IEventSystemHandler, UnityEngine.EventSystems.IBeginDragHandler, UnityEngine.EventSystems.IDragHandler, UnityEngine.EventSystems.IEndDragHandler, UnityEngine.EventSystems.IScrollHandler
	{
		[SerializeField]
		private UnityEngine.UI.ScrollRect m_TargetScrollRect;

		public void OnInitializePotentialDrag(UnityEngine.EventSystems.PointerEventData eventData)
		{
		}

		public void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
		{
		}

		public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData eventData)
		{
		}

		public void OnEndDrag(UnityEngine.EventSystems.PointerEventData eventData)
		{
		}

		public void OnScroll(UnityEngine.EventSystems.PointerEventData eventData)
		{
		}
	}
}
