using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class ScrollRectPageSnap : MonoBehaviour, UnityEngine.EventSystems.IInitializePotentialDragHandler, UnityEngine.EventSystems.IEventSystemHandler, UnityEngine.EventSystems.IBeginDragHandler, UnityEngine.EventSystems.IEndDragHandler, UnityEngine.EventSystems.IDragHandler, UnityEngine.EventSystems.IScrollHandler
	{
		[SerializeField]
		public int m_HorizontalPages;

		[SerializeField]
		public int m_VerticalPages;

		public bool wheelScrollEnable;

		[SerializeField]
		public Tween.Easing easing;

		[SecField]
		[SerializeField]
		public float duration;

		[SerializeField]
		public float sensitivity;

		[SerializeField]
		public float backlash;

		[SerializeField]
		public UnityEngine.Events.UnityEvent onPageChanged;

		[SerializeField]
		public UnityEngine.Events.UnityEvent onPageTotalChanged;

		public System.Action<int> onReleaseDragCallback;

		private int draggingId;

		private UnityEngine.UI.ScrollRect scrollrect;

		private float tweenTime;

		private float tweenMaxTime;

		private Vector2 startNormalized;

		private Vector2 endNormalized;

		private int vpage;

		private int hpage;

		private int vspage;

		private int hspage;

		private System.Collections.Generic.List<bool> hpageEnableList;

		private System.Collections.Generic.List<bool> vpageEnableList;

		private bool isBarDragging;

		public bool dragScrollEnabled;

		public int horizontalPages
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		public int verticalPages
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		public int hPage
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		public int vPage
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		public bool isDragging => false;

		public bool isTweening => false;

		public UnityEngine.UI.ScrollRect target => null;

		private void Start()
		{
		}

		public void OnScroll(UnityEngine.EventSystems.PointerEventData eventData)
		{
		}

		public void OnInitializePotentialDrag(UnityEngine.EventSystems.PointerEventData eventData)
		{
		}

		public void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
		{
		}

		public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData eventData)
		{
		}

		public int calcPageIndex(float norm, int pages, int startpage)
		{
			return 0;
		}

		public void OnEndDrag(UnityEngine.EventSystems.PointerEventData eventData)
		{
		}

		public void ReleaseDrag(Vector2 delta)
		{
		}

		private void Awake()
		{
		}

		private void OnStartBarDrag()
		{
		}

		private void OnBarDragging()
		{
		}

		private void OnEndBarDrag()
		{
		}

		private void CalcPositionToPage()
		{
		}

		private void Update()
		{
		}

		private void ChangeNormalized(float ex, float ey)
		{
		}

		public void SetVPageEnable(bool enable, int page = -1)
		{
		}

		public void SetHPageEnable(bool enable, int page = -1)
		{
		}

		public void ForceSnap()
		{
		}

		public void LoopHorizontal()
		{
		}

		public void PseudoLoopHorizontalView()
		{
		}
	}
}
