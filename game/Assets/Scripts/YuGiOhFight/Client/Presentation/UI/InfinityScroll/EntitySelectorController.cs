using System.Runtime.CompilerServices;


namespace YuGiOhFight.Client.Presentation.UI.InfinityScroll
{
	public class EntitySelectorController
	{
		private EntitySelectorSettings m_SelectorSettings;

		private InfinityScrollView m_Owner;

		private Selector m_Selector;

		private UnityEngine.UI.GridLayoutGroup.Axis m_ScrollAxis;

		private readonly System.Collections.Generic.Dictionary<SelectionItem, int> m_SelectionItemXMap;

		private readonly System.Collections.Generic.Dictionary<SelectionItem, int> m_SelectionItemYMap;

		private readonly System.Collections.Generic.Dictionary<UnityEngine.GameObject, System.Collections.Generic.List<SelectionItem>> m_EntitySelectionItemsMap;

		private System.Collections.Generic.List<SelectionItem> m_TmpSearchSelectionItems;

		private System.Collections.Generic.List<SelectionItem> m_InitializedSelectionItems;

		public System.Func<UnityEngine.GameObject, System.Collections.Generic.IReadOnlyList<(SelectionItem, int, int)>> customCollectSelectionItemsFunc;

		public System.Func<int, bool> isSelectableDataIndexFunc;

		public System.Func<SelectionItem, PadInputDirection, bool> customEdgeTransitionFunc;

		public System.Func<SelectionItem, PadInputDirection, bool> customInnerTransitionFunc;

		public System.Func<UnityEngine.GameObject, int, bool, bool> customOnFocusSelectFunc;

		public System.Func<UnityEngine.Vector2, UnityEngine.Vector2, bool> m_DragStarterFunc;

		public System.Func<bool> m_OnSelectorSelectedFunc;

		public System.Collections.Generic.Dictionary<SelectionItem, int> SelectionItemXMap => null;

		public System.Collections.Generic.Dictionary<SelectionItem, int> SelectionItemYMap => null;

		public bool isReady => false;

		public UnityEngine.GameObject selectedEntity => null;

		public int selectedEntityIndex => 0;

		public System.Func<UnityEngine.Vector2, UnityEngine.Vector2, bool> dragStarterFunc
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

		public int currentIdx
		{
			[CompilerGenerated]
			get
			{
				return 0;
			}
			[CompilerGenerated]
			private set
			{
			}
		}

		public int currentSubIdxX
		{
			[CompilerGenerated]
			get
			{
				return 0;
			}
			[CompilerGenerated]
			private set
			{
			}
		}

		public int currentSubIdxY
		{
			[CompilerGenerated]
			get
			{
				return 0;
			}
			[CompilerGenerated]
			private set
			{
			}
		}

		public void Initialize(EntitySelectorSettings selectorSettings, InfinityScrollView owner, Selector selector, System.Action onComplete = null, UnityEngine.UI.GridLayoutGroup.Axis axis = UnityEngine.UI.GridLayoutGroup.Axis.Vertical)
		{
		}

		public void OnCreateEntity(UnityEngine.GameObject entity)
		{
		}

		public void OnActivateEntity(UnityEngine.GameObject entity)
		{
		}

		public void OnUpdateEntity(UnityEngine.GameObject entity, int dataindex)
		{
		}

		public void OnFocusEntity(UnityEngine.GameObject entity, int dataindex, bool selectItem, bool isInitializeSelect = false)
		{
		}

		public void OnDeactivateEntity(UnityEngine.GameObject entity)
		{
		}

		private void SetTransitionModeAsEdge(SelectionItem selectionItem, PadInputDirection direction)
		{
		}

		private void SetTransitionModeAsFraction(SelectionItem selectionItem, PadInputDirection direction)
		{
		}

		private void SetTransitionModeAsInner(SelectionItem selectionItem, PadInputDirection direction)
		{
		}

		private void OnSelectedItem(SelectionItem selectionItem)
		{
		}

		private bool OnSelectorSelected()
		{
			return false;
		}

		private void OnEntityPadInput(SelectionItem selectionItem, PadInputDirection direction)
		{
		}

		public void ResetContentPosition()
		{
		}

		public bool IsSelectableDataIndex(int dataIndex)
		{
			return false;
		}

		public int GetSelectableSideIndex(int dataIndex, PadInputDirection direction)
		{
			return 0;
		}

		public bool IsEdgeSelectionItem(SelectionItem selectionItem, PadInputDirection direction)
		{
			return false;
		}

		public UnityEngine.GameObject GetEntityBySelectionItem(SelectionItem selectionItem)
		{
			return null;
		}

		public System.Collections.Generic.List<SelectionItem> GetSelectionItemsByEntity(UnityEngine.GameObject entity)
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

		public bool IsRegistedSelectionItem(SelectionItem selectionItem)
		{
			return false;
		}

		private void InitSelectionItems(SelectionItem selectionItem)
		{
		}

		public void RegistSelectionItem(UnityEngine.GameObject entity, SelectionItem selectionItem, int xPos, int yPos, int dataindex = -1)
		{
		}

		public void UnregistSelectionItem(UnityEngine.GameObject entity = null, SelectionItem selectionItem = null)
		{
		}
	}
}
