using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI.InfinityScroll
{
	public class InfinityScrollView : MonoBehaviour, UnityEngine.EventSystems.IBeginDragHandler, UnityEngine.EventSystems.IEventSystemHandler
	{
		[SerializeField]
		private string m_ELabelTemplate;

		[SerializeField]
		private string[] m_ELabelAdditionalTemplates;

		[SerializeField]
		private EntityPoolSettings m_EntityPoolSettings;

		[SerializeField]
		private EntitySelectorSettings m_EntitySelectorSettings;

		private EntityPoolController m_EntityPool;

		private EntitySelectorController m_EntitySelector;

		public System.Action<GameObject> onCreatedEntityCallback;

		public System.Action<GameObject> onActivateEntityCallback;

		public System.Action<GameObject, int> onUpdateEntityCallback;

		public System.Action<GameObject, int, bool, bool> onFocusEntityCallback;

		public System.Action<GameObject> onDeactivateEntityCallback;

		public System.Action<GameObject, int, bool> onRemoveEntityCallback;

		public EntityPoolSettings entityPoolSettings => null;

		public EntityPoolController.WaitFocusData waitFocusData => null;

		public string eLabelTemplate
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public string[] eLabelAdditionalTemplates => null;

		public bool isReady => false;

		public bool isCompletedReserve => false;

		public bool isMoving => false;

		public System.Collections.Generic.IReadOnlyList<GameObject> activeEntityList => null;

		public bool isHorizontal => false;

		public int dataCount => 0;

		public bool useViewportSize
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public System.Func<int, (bool, float)> customMoveContentToFitDataFunc
		{
			set
			{
			}
		}

		public int constraintCount => 0;

		public int activeEntityLineCount => 0;

		public System.Func<Vector2, Vector2, bool> dragStarterFunc
		{
			set
			{
			}
		}

		public System.Func<bool> onSelectorSelectoredFunc
		{
			set
			{
			}
		}

		public int defaultIdx
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		public int currentIdx => 0;

		public int currentSubIdxX => 0;

		public int currentSubIdxY => 0;

		public int contentCount => 0;

		public int beginIdx => 0;

		public int endIdx => 0;

		public System.Func<int, bool> isSelectableDataIndexFunc
		{
			set
			{
			}
		}

		public System.Func<GameObject, System.Collections.Generic.IReadOnlyList<(SelectionItem, int, int)>> customCollectSelectionItemsFunc
		{
			set
			{
			}
		}

		public System.Func<SelectionItem, PadInputDirection, bool> customEdgeTransitionFunc
		{
			set
			{
			}
		}

		public System.Func<SelectionItem, PadInputDirection, bool> customInnerTransitionFunc
		{
			set
			{
			}
		}

		public System.Func<GameObject, int, bool, bool> customOnFocusSelectFunc
		{
			set
			{
			}
		}

		private void LateUpdate()
		{
		}

		public void Initialize()
		{
		}

		public void Initialize(System.Action onCompleteCallback)
		{
		}

		public void Initialize(GameObject template = null, System.Action onCompleteCallback = null)
		{
		}

		public void Initialize(System.Collections.Generic.List<GameObject> templates = null, System.Action onCompleteCallback = null)
		{
		}

		public void Initialize(EntityPoolController entityPool = null, EntitySelectorController entitySelector = null, System.Action onCompleteCallback = null)
		{
		}

		public void Initialize(EntityPoolController entityPool = null, EntitySelectorController entitySelector = null, System.Collections.Generic.List<GameObject> templates = null, System.Action onCompleteCallback = null)
		{
		}

		private void OnCreateEntity(GameObject entity)
		{
		}

		private void OnActivateEntity(GameObject entity)
		{
		}

		private void OnUpdateEntity(GameObject entity, int dataindex)
		{
		}

		private void OnFocusEntity(GameObject entity, int dataindex, bool selectItem, bool isInitializeSelect = false)
		{
		}

		private void OnDeactivateEntity(GameObject entity)
		{
		}

		private void OnRemoveEntity(GameObject entity, int dataindex, bool isTop)
		{
		}

		private void OnDisable()
		{
		}

		public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData eventData)
		{
		}

		public void ReserveTemplate(int templateIdx = 0, int asyncPerFrame = 0, System.Action onComplete = null)
		{
		}

		public void TerminateReserve()
		{
		}

		public void UpdateDataCount(int dataCount, System.Collections.Generic.List<int> templateList = null)
		{
		}

		public void UpdateDataCountAsync(int dataCount, System.Collections.Generic.List<int> templateList = null, int updatePerFrame = 1, System.Action onComplete = null)
		{
		}

		public void UpdateData()
		{
		}

		public void ResetContentPosition()
		{
		}

		public bool FocusItemByDataIndex(int dataindex, bool selectItem = true, bool isInitializeSelect = false, System.Action onComplete = null)
		{
			return false;
		}

		public bool TryFocusInnerViewport(bool selectItem, bool isInitializeSelect = false, System.Action onComplete = null)
		{
			return false;
		}

		public float GetTemplateSizeAlongScrollDirection(int templateIdx)
		{
			return 0f;
		}

		public GameObject GetEntityByDataIndex(int dataindex)
		{
			return null;
		}

		public T GetEntityByDataIndex<T>(int dataindex)
		{
			return default(T);
		}

		public int GetLineByDataIndex(int dataindex)
		{
			return 0;
		}

		public int GetDataIndexByEntity(GameObject entity)
		{
			return 0;
		}

		public int GetTemplateIndexByDataIndex(int dataindex)
		{
			return 0;
		}

		public int GetTemplateIndexByEntity(GameObject entity)
		{
			return 0;
		}

		public int GetSideIndex(int frontIdx, PadInputDirection sideDirection)
		{
			return 0;
		}

		public bool IsEdgeByDataIndex(int dataindex, PadInputDirection direction)
		{
			return false;
		}

		public void ImmediateApplyMovement()
		{
		}

		public void UpdateContentPos()
		{
		}

		public void StopAutoScroll()
		{
		}

		public string GetTemplateLabelByDataIndex(int dataindex)
		{
			return null;
		}

		public string GetTemplateLabelByTemplateIndex(int templateindex)
		{
			return null;
		}

		public void UpdatePaddingSize()
		{
		}

		public void UpdateViewportRectSize()
		{
		}

		public GameObject GetEntityBySelectionItem(SelectionItem selectionItem)
		{
			return null;
		}

		public System.Collections.Generic.List<SelectionItem> GetSelectionItemsByEntity(GameObject entity)
		{
			return null;
		}

		public bool TrySelectIdx(int dataIdx, int xPos = 0, int yPos = 0, bool initializeSelection = false)
		{
			return false;
		}

		public bool JumpToDirection(PadInputDirection direction, bool selectItem = true, bool isInitializeSelect = false, int jumpLength = 0)
		{
			return false;
		}

		public bool IsFractionIndex(int dataindex)
		{
			return false;
		}

		public System.Collections.Generic.Dictionary<SelectionItem, int> GetSelectionItemXMap()
		{
			return null;
		}

		public System.Collections.Generic.Dictionary<SelectionItem, int> GetSelectionItemYMap()
		{
			return null;
		}

		public bool IsSelectableDataIndex(int dataIndex)
		{
			return false;
		}

		public bool IsRegistedSelectionItem(SelectionItem selectionItem)
		{
			return false;
		}

		public void RegistSelectionItem(GameObject entity, SelectionItem selectionItem, int xPos, int yPos, int dataindex = -1)
		{
		}

		public void UnregistSelectionItem(GameObject entity = null, SelectionItem selectionItem = null)
		{
		}
	}
}
