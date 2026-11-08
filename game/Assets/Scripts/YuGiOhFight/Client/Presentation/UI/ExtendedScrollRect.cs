using System.Runtime.CompilerServices;
using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class ExtendedScrollRect : UnityEngine.UI.ScrollRect, UnityEngine.EventSystems.IBeginDragHandler, UnityEngine.EventSystems.IEventSystemHandler, UnityEngine.EventSystems.IEndDragHandler, UnityEngine.EventSystems.IPointerDownHandler
	{
		private const float WHEELSCROLL_MINMAGNITUDE = 0.0001f;

		private const float k_BaseDeltaSec = 0.01666f;

		private const float k_MinDeltaModify = 0.3f;

		private GameObject barObjHorizontal;

		private GameObject barObjVertical;

		private Tweens.TweenContainer tweenContainerH;

		private Tweens.TweenContainer tweenContainerV;

		private CanvasGroup barCanvasGrpH;

		private CanvasGroup barCanvasGrpV;

		private bool dragScrollEnabled;

		private bool fadeEnabled;

		private bool isHorizontalShowing;

		private bool isVerticalShowing;

		protected bool dragging;

		private Vector2 m_WheelScrollVelocity;

		private Vector2 m_TargetPos;

		private Vector2 m_AnchorPos;

		private System.Collections.IEnumerator m_yMoveContentImpl;

		public System.Action onStopScrollCallback;

		[SerializeField]
		private bool m_IsIgnoreNotTagetAxis;

		[SerializeField]
		private float ignoreRate;

		[SerializeField]
		private bool m_StopOnPointerDown;

		[SerializeField]
		private bool m_ApplyDeltaTime;

		public int dragBlockCounter
		{
			[CompilerGenerated]
			get
			{
				return 0;
			}
			[CompilerGenerated]
			set
			{
			}
		}

		public bool isDragBlocked => false;

		public Vector2 targetPos => default(Vector2);

		public bool isMoving => false;

		public bool isAutoScroll => false;

		public void SetIgnoreNotTagetAxis(bool isIgnore, float thres)
		{
		}

		protected override void Start()
		{
		}

		private void Initialize()
		{
		}

		private void Update()
		{
		}

		public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
		{
		}

		public override void OnInitializePotentialDrag(UnityEngine.EventSystems.PointerEventData eventData)
		{
		}

		public override void OnBeginDrag(UnityEngine.EventSystems.PointerEventData eventData)
		{
		}

		public override void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
		{
		}

		public override void OnEndDrag(UnityEngine.EventSystems.PointerEventData eventData)
		{
		}

		public override void OnScroll(UnityEngine.EventSystems.PointerEventData data)
		{
		}

		public void ScrollByDelta(Vector2 delta)
		{
		}

		public void ScrollByTargetPos(Vector2 targetPos)
		{
		}

		public void StopAutoScroll(bool isInvokeCallback = true)
		{
		}

		protected System.Collections.IEnumerator MoveContentImpl()
		{
			return null;
		}

		public void ActiveAnalogPadScroll(SelectorManager.AnalogType analogType)
		{
		}

		private void SetTargetAlpha(GameObject target, float alpha)
		{
		}

		private void ShowScrollBar()
		{
		}

		private void HideScrollBar()
		{
		}

		public override void StopMovement()
		{
		}

		public void ScrollByVerticalNormalizedPos(float dst, bool overrideTarget = true)
		{
		}

		public void ScrollByHorizontalNormalizedPos(float dst, bool overrideTarget = true)
		{
		}

		public void ScrollByNormalizedPos(Vector2 dst, bool overrideTarget = true)
		{
		}

		protected override void OnDestroy()
		{
		}
	}
}
