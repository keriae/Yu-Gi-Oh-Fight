using System.Runtime.CompilerServices;
using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class SelectionItem : MonoBehaviour
	{
		public enum DragStatus
		{
			Begin = 0,
			Dragging = 1,
			End = 2
		}

		public enum HoldStatus
		{
			Begin = 0,
			Holding = 1,
			End = 2
		}

		public class DragEvent : UnityEngine.Events.UnityEvent<DragStatus, Vector2>
		{
			public DragEvent()
			{
				//((UnityEvent<T0, T1>)(object)this)._002Ector();
			}
		}

		public class HoldEvent : UnityEngine.Events.UnityEvent<HoldStatus, Vector2>
		{
			public HoldEvent()
			{
				//((UnityEvent<T0, T1>)(object)this)._002Ector();
			}
		}

		public class AnalogEvent
		{
			private System.Collections.Generic.List<System.Func<Vector2, bool>> callbackList;

			public void Add(System.Func<Vector2, bool> func)
			{
			}

			public void Remove(System.Func<Vector2, bool> func)
			{
			}

			public void Clear()
			{
			}

			public bool Invoke(Vector2 analogInput)
			{
				return false;
			}
		}

		public delegate void InputCallback();

		public enum TransitionMode
		{
			Automatic = 0,
			Manual = 1,
			None = 2,
			SelectorParent = 3,
			SelectorChild = 4,
			SelectorManual = 5
		}

		public enum RegistStatus
		{
			None = 0,
			Previsional = 1,
			Registed = 2
		}

		public enum UpdateItemStatus
		{
			Unknown = 0,
			None = 1,
			Inactive = 2,
			InvokeShortcutCallback = 4,
			InvokeSelectedCallback = 8,
			InvokeClickCallback = 16,
			Invalid = 3,
			InvokeNotClickCallback = 12,
			InvokeAnyCallback = 28
		}

		private Selector _selector;

		[SerializeField]
		private bool defaultItem;

		[SerializeField]
		private float _selectionAngle;

		protected UnityEngine.Events.UnityEvent onSelectedCallback;

		protected UnityEngine.Events.UnityEvent onDeselectedCallback;

		protected System.Collections.Generic.Dictionary<(SelectorManager.AnalogType, SelectorManager.KeyType), AnalogEvent> onSelectedAnalogInputCallback;

		protected System.Collections.Generic.Dictionary<(SelectorManager.AnalogType, SelectorManager.KeyType), AnalogEvent> onShortCutAnalogInputCallback;

		protected UnityEngine.Events.UnityEvent onPointerEnterCallback;

		protected UnityEngine.Events.UnityEvent onPointerExitCallback;

		protected UnityEngine.Events.UnityEvent onPointerDownCallback;

		protected UnityEngine.Events.UnityEvent onPointerClickCallback;

		protected UnityEngine.Events.UnityEvent onPointerDounbleClickCallback;

		protected UnityEngine.Events.UnityEvent onPointerUpCallback;

		protected System.Func<Vector2, Vector2, bool> dragStarter;

		protected DragEvent onDragCallback;

		protected System.Func<float, bool> holdStarter;

		protected HoldEvent onHoldCallback;

		[SerializeField]
		protected bool _selectable;

		[SerializeField]
		protected bool _interactable;

		protected bool pointerDown;

		protected bool pointerEntering;

		[SerializeField]
		protected bool _useDoubleClick;

		[SerializeField]
		protected TransitionMode upMode;

		[SerializeField]
		protected TransitionMode downMode;

		[SerializeField]
		protected TransitionMode rightMode;

		[SerializeField]
		protected TransitionMode leftMode;

		[SerializeField]
		protected SelectionItem upItem;

		[SerializeField]
		protected SelectionItem downItem;

		[SerializeField]
		protected SelectionItem rightItem;

		[SerializeField]
		protected SelectionItem leftItem;

		[SerializeField]
		protected Selector upSelector;

		[SerializeField]
		protected Selector downSelector;

		[SerializeField]
		protected Selector rightSelector;

		[SerializeField]
		protected Selector leftSelector;

		private RegistStatus registStatus;

		protected float _priority;

		private CanvasRenderer canvasRenderer;

		protected System.Collections.Generic.List<uint> callbackIDList;

		public Selector selector => null;

		public bool isDefaultItem => false;

		public float selectionAngle
		{
			get
			{
				return 0f;
			}
			set
			{
			}
		}

		public bool isSelected
		{
			[CompilerGenerated]
			get
			{
				return false;
			}
			[CompilerGenerated]
			private set
			{
			}
		}

		public bool selectable
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public virtual bool interactable
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public bool isActivate => false;

		public bool isClusterActivated => false;

		public bool useDoubleClick
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public bool isRegisted => false;

		public float priority
		{
			get
			{
				return 0f;
			}
			set
			{
			}
		}

		public virtual Vector2 viewCenterPosition => default(Vector2);

		public event InputCallback upInputCallback
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public event InputCallback downInputCallback
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public event InputCallback rightInputCallback
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public event InputCallback leftInputCallback
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public event InputCallback upTransitionFailedCallback
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public event InputCallback downTransitionFailedCallback
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public event InputCallback rightTransitionFailedCallback
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public event InputCallback leftTransitionFailedCallback
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		protected virtual void Awaked()
		{
		}

		protected virtual void Enabled()
		{
		}

		protected virtual void Disabled()
		{
		}

		protected virtual void Destroyed()
		{
		}

		private void Awake()
		{
		}

		private void OnEnable()
		{
		}

		private void OnDisable()
		{
		}

		private void OnDestroy()
		{
		}

		public virtual UpdateItemStatus UpdateItem()
		{
			return default(UpdateItemStatus);
		}

		public virtual UpdateItemStatus UpdateSelectedItem()
		{
			return default(UpdateItemStatus);
		}

		public virtual Vector2 GetClosestPoint(Vector2 base_position, Vector2 direction, bool contains_check = true)
		{
			return default(Vector2);
		}

		public virtual bool IsRectContains(Vector2 rect_point0, Vector2 rect_point1, Vector2 rect_point2, Vector2 rect_point3, bool containedComplete)
		{
			return false;
		}

		public virtual bool IsContainsPoint(Vector2 view_position)
		{
			return false;
		}

		public bool Select(bool initializeSelection = false, bool force = false)
		{
			return false;
		}

		public bool Reselect(bool initializeSelection = false)
		{
			return false;
		}

		public virtual bool OnSelected(bool initializeSelection = false)
		{
			return false;
		}

		public virtual bool OnDeselected()
		{
			return false;
		}

		public void SetOnSelectedCallback(UnityEngine.Events.UnityAction on_selected_callback)
		{
		}

		public void RemoveAllOnSelectedCallback()
		{
		}

		public void SetOnDeselectedCallback(UnityEngine.Events.UnityAction on_deselected_callback)
		{
		}

		public void RemoveAllOnDeselectedCallback()
		{
		}

		protected void SetKeyCallback(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub, System.Func<bool> callback, SelectorManager.KeyStatus keyStatus, bool isSelected)
		{
		}

		protected void ClearKeyCallback(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub, System.Func<bool> callback, SelectorManager.KeyStatus keyStatus, bool isSelected)
		{
		}

		protected void ClearKeyCallbackAll(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub, SelectorManager.KeyStatus keyStatus, bool isSelected)
		{
		}

		public void SetOnSelectedKeyDownCallback(SelectorManager.KeyType key_type, System.Func<bool> callback)
		{
		}

		public void SetOnSelectedKeyDownCallback(SelectorManager.KeyType key_type, UnityEngine.Events.UnityAction callback, bool exclusiveFunc = false)
		{
		}

		public void SetOnSelectedKeyDownCallback(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub, System.Func<bool> callback)
		{
		}

		public void SetOnSelectedKeyDownCallback(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub, UnityEngine.Events.UnityAction callback, bool exclusiveFunc = false)
		{
		}

		public void ClearOnSelectedKeyDownCallback(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub, System.Func<bool> callback)
		{
		}

		public void ClearOnSelectedKeyDownCallbackAll(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub)
		{
		}

		public void SetOnSelectedKeyCallback(SelectorManager.KeyType key_type, System.Func<bool> callback)
		{
		}

		public void SetOnSelectedKeyCallback(SelectorManager.KeyType key_type, UnityEngine.Events.UnityAction callback, bool exclusiveFunc = false)
		{
		}

		public void SetOnSelectedKeyCallback(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub, System.Func<bool> callback)
		{
		}

		public void SetOnSelectedKeyCallback(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub, UnityEngine.Events.UnityAction callback, bool exclusiveFunc = false)
		{
		}

		public void ClearOnSelectedKeyCallback(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub, System.Func<bool> callback)
		{
		}

		public void ClearOnSelectedKeyCallbackAll(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub)
		{
		}

		public void SetOnSelectedKeyUpCallback(SelectorManager.KeyType key_type, System.Func<bool> callback)
		{
		}

		public void SetOnSelectedKeyUpCallback(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub, System.Func<bool> callback)
		{
		}

		public void ClearOnSelectedKeyUpCallback(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub, System.Func<bool> callback)
		{
		}

		public void ClearOnSelectedKeyUpCallbackAll(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub)
		{
		}

		public void SetOnShortCutKeyDownCallback(SelectorManager.KeyType key_type, System.Func<bool> callback)
		{
		}

		public void SetOnShortCutKeyDownCallback(SelectorManager.KeyType key_type, UnityEngine.Events.UnityAction callback, bool exclusiveFunc = false)
		{
		}

		public void SetOnShortCutKeyDownCallback(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub, System.Func<bool> callback)
		{
		}

		public void SetOnShortCutKeyDownCallback(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub, UnityEngine.Events.UnityAction callback, bool exclusiveFunc = false)
		{
		}

		public void ClearOnShortCutKeyDownCallback(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub, System.Func<bool> callback)
		{
		}

		public void ClearOnShortCutKeyDownCallbackAll(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub)
		{
		}

		public void SetOnShortCutKeyCallback(SelectorManager.KeyType key_type, System.Func<bool> callback)
		{
		}

		public void SetOnShortCutKeyCallback(SelectorManager.KeyType key_type, UnityEngine.Events.UnityAction callback, bool exclusiveFunc = false)
		{
		}

		public void SetOnShortCutKeyCallback(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub, System.Func<bool> callback)
		{
		}

		public void SetOnShortCutKeyCallback(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub, UnityEngine.Events.UnityAction callback, bool exclusiveFunc = false)
		{
		}

		public void ClearOnShortCutKeyCallback(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub, System.Func<bool> callback)
		{
		}

		public void ClearOnShortCutKeyCallbackAll(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub)
		{
		}

		public void SetOnShortCutKeyUpCallback(SelectorManager.KeyType key_type, System.Func<bool> callback)
		{
		}

		public void SetOnShortCutKeyUpCallback(SelectorManager.KeyType key_type, UnityEngine.Events.UnityAction callback, bool exclusiveFunc = false)
		{
		}

		public void SetOnShortCutKeyUpCallback(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub, System.Func<bool> callback)
		{
		}

		public void SetOnShortCutKeyUpCallback(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub, UnityEngine.Events.UnityAction callback, bool exclusiveFunc = false)
		{
		}

		public void ClearOnShortCutKeyUpCallback(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub, System.Func<bool> callback)
		{
		}

		public void ClearOnShortCutKeyUpCallbackAll(SelectorManager.KeyType key_type_main, SelectorManager.KeyType key_type_sub)
		{
		}

		public void SetOnSelectedAnalogInputCallback(SelectorManager.AnalogType analog_type, System.Func<Vector2, bool> callback)
		{
		}

		public void SetOnSelectedAnalogInputCallback(SelectorManager.AnalogType analog_type, System.Action<Vector2> callback, bool exclusiveFunc = false)
		{
		}

		public void SetOnSelectedAnalogInputCallback(SelectorManager.AnalogType analog_type, SelectorManager.KeyType subKeyType, System.Action<Vector2> callback, bool exclusiveFunc = false)
		{
		}

		public void SetOnSelectedAnalogInputCallback(SelectorManager.AnalogType analog_type, SelectorManager.KeyType subKeyType, System.Func<Vector2, bool> callback)
		{
		}

		public bool InvokeOnSelectedAnalogInputCallback()
		{
			return false;
		}

		public void SetOnShortCutAnalogInputCallback(SelectorManager.AnalogType analog_type, System.Func<Vector2, bool> callback)
		{
		}

		public void SetOnShortCutAnalogInputCallback(SelectorManager.AnalogType analog_type, System.Action<Vector2> callback, bool exclusiveFunc = false)
		{
		}

		public void SetOnShortCutAnalogInputCallback(SelectorManager.AnalogType analog_type, SelectorManager.KeyType subKeyType, System.Action<Vector2> callback, bool exclusiveFunc = false)
		{
		}

		public void SetOnShortCutAnalogInputCallback(SelectorManager.AnalogType analog_type, SelectorManager.KeyType subKeyType, System.Func<Vector2, bool> callback)
		{
		}

		public bool InvokeOnShortCutAnalogInputCallback()
		{
			return false;
		}

		public void SetOnSelectedMouseClickCallback(SelectorManager.MouseType mouse_type, UnityEngine.Events.UnityAction<bool> callback, bool exclusiveFunc = true)
		{
		}

		public void SetOnSelectedMouseClickCallback(SelectorManager.MouseType mouse_type, System.Func<bool, bool> callback)
		{
		}

		public void SetOnSelectedMousePushCallback(SelectorManager.MouseType mouse_type, UnityEngine.Events.UnityAction<bool> callback, bool exclusiveFunc = true)
		{
		}

		public void SetOnSelectedMousePushCallback(SelectorManager.MouseType mouse_type, System.Func<bool, bool> callback)
		{
		}

		public void SetOnShortCutMouseClickCallback(SelectorManager.MouseType mouse_type, UnityEngine.Events.UnityAction<bool> callback, bool exclusiveFunc = true)
		{
		}

		public void SetOnShortCutMouseClickCallback(SelectorManager.MouseType mouse_type, System.Func<bool, bool> callback)
		{
		}

		public void SetOnShortCutMousePushCallback(SelectorManager.MouseType mouse_type, UnityEngine.Events.UnityAction<bool> callback, bool exclusiveFunc = true)
		{
		}

		public void SetOnShortCutMousePushCallback(SelectorManager.MouseType mouse_type, System.Func<bool, bool> callback)
		{
		}

		private void SetMouseCallback(SelectorManager.KeyStatus status, SelectorManager.MouseType mouse_type, System.Func<bool, bool> callback, bool isSelected)
		{
		}

		protected void ClearMouseCallback(SelectorManager.MouseType mouse_type, System.Func<bool> callback, SelectorManager.KeyStatus keyStatus, bool isSelected)
		{
		}

		protected void ClearMouseCallbackAll(SelectorManager.MouseType mouse_type, SelectorManager.KeyStatus keyStatus, bool isSelected)
		{
		}

		public void SetOnPointerEnterCallback(UnityEngine.Events.UnityAction callback)
		{
		}

		public virtual bool OnPointerEnter()
		{
			return false;
		}

		public void SetOnPointerExitCallback(UnityEngine.Events.UnityAction callback)
		{
		}

		public virtual bool OnPointerExit()
		{
			return false;
		}

		public void SetOnPointerDownCallback(UnityEngine.Events.UnityAction callback)
		{
		}

		public virtual bool OnPointerDown()
		{
			return false;
		}

		public void SetOnPointerClickCallback(UnityEngine.Events.UnityAction callback)
		{
		}

		public virtual bool OnPointerClick()
		{
			return false;
		}

		public virtual bool OnPointerDoubleClick()
		{
			return false;
		}

		public void SetOnPointerUpCallback(UnityEngine.Events.UnityAction callback)
		{
		}

		public virtual bool OnPointerUp()
		{
			return false;
		}

		public void SetDragStarter(System.Func<Vector2, Vector2, bool> drag_starter)
		{
		}

		public bool IsDragStart(Vector2 screen_point, Vector2 pressed_point)
		{
			return false;
		}

		public void SetOnDragCallback(UnityEngine.Events.UnityAction<DragStatus, Vector2> callback)
		{
		}

		public void ClearOnDragCallback()
		{
		}

		public virtual bool OnDrag(DragStatus drag_status, Vector2 screen_position)
		{
			return false;
		}

		public void SetAsVerticalScrollItem(float dragThreshold = 0.01f)
		{
		}

		public void SetAsHorizontalScrollItem(float dragThreshold = 0.01f)
		{
		}

		public void SetAsMultiScrollItem(float dragThreshold = 0.01f)
		{
		}

		private void SetAsScrollItem(bool verticalScroll, bool horizontalScroll, float dragThreshold = 0.01f)
		{
		}

		public void SetHoldStarter(System.Func<float, bool> hold_starter)
		{
		}

		public bool IsHoldStart(float holdTime)
		{
			return false;
		}

		public void SetOnHoldCallback(UnityEngine.Events.UnityAction<HoldStatus, Vector2> callback)
		{
		}

		public void ClearOnHoldCallback()
		{
		}

		public virtual bool OnHold(HoldStatus hold_status, Vector2 screen_position)
		{
			return false;
		}

		public TransitionMode GetTransitionMode(PadInputDirection direction)
		{
			return default(TransitionMode);
		}

		public void SetTransitionMode(PadInputDirection direction, TransitionMode mode)
		{
		}

		public SelectionItem GetManualTransitionItem(PadInputDirection direction)
		{
			return null;
		}

		public void SetManualTransitionItem(PadInputDirection direction, SelectionItem item, bool set_mode_manual = true)
		{
		}

		public Selector GetManualTransitionSelector(PadInputDirection direction)
		{
			return null;
		}

		public void SetManualTransitionSelector(PadInputDirection direction, Selector selector, bool set_mode_manual = true)
		{
		}

		public void InvokeInputCallback(PadInputDirection direction)
		{
		}

		public void AddInputCallback(PadInputDirection direction, InputCallback callback)
		{
		}

		public void RemoveInputCallback(PadInputDirection direction, InputCallback callback)
		{
		}

		public void RemoveInputCallbackAll(PadInputDirection direction)
		{
		}

		public void AddTransitionFailedCallback(PadInputDirection direction, InputCallback callback)
		{
		}

		public void RemoveTransitionFailedCallback(PadInputDirection direction, InputCallback callback)
		{
		}

		public void RemoveTransitionFailedCallbackAll(PadInputDirection direction)
		{
		}

		public void InvokeTransitionFailedCallback(PadInputDirection direction)
		{
		}

		public void InputDirection(PadInputDirection direction)
		{
		}

		public void SetResistStatus(RegistStatus regist_status)
		{
		}

		public void SetAsDefaultItem(bool isDefaultItem)
		{
		}

		public void SetupDepth()
		{
		}
	}
}
