using System.Runtime.CompilerServices;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class SelectorCluster
	{
		public int priority;

		public System.Collections.Generic.List<SelectorGroup> groups;

		private SelectionItem preSelectedItem;

		protected PadInputDirection currentPadDirection;

		protected float padInputContinueTime;

		protected static UnityEngine.Vector2 directionUp;

		protected static UnityEngine.Vector2 directionUpRight;

		protected static UnityEngine.Vector2 directionRight;

		protected static UnityEngine.Vector2 directionDownRight;

		protected static UnityEngine.Vector2 directionDown;

		protected static UnityEngine.Vector2 directionDownLeft;

		protected static UnityEngine.Vector2 directionLeft;

		protected static UnityEngine.Vector2 directionUpLeft;

		public bool goThrough => false;

		public int goThroughCounter
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

		public SelectionItem currentItem
		{
			[CompilerGenerated]
			get
			{
				return null;
			}
			[CompilerGenerated]
			private set
			{
			}
		}

		public Selector currentSelector => null;

		public bool activated
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

		public bool protectActivation
		{
			[CompilerGenerated]
			get
			{
				return false;
			}
			[CompilerGenerated]
			set
			{
			}
		}

		public SelectorCluster(int priority)
		{
		}

		public void AddGroup(SelectorGroup group)
		{
		}

		public void RemoveGroup(SelectorGroup group)
		{
		}

		public SelectionItem.UpdateItemStatus UpdateAllGroups()
		{
			return default(SelectionItem.UpdateItemStatus);
		}

		public SelectorGroup GetGroup(string label)
		{
			return null;
		}

		public bool SelectItem(SelectionItem item, bool initializeSelection = false, bool force = false)
		{
			return false;
		}

		public void DeselectCurrentItem(bool savePreSelectedItem)
		{
		}

		public SelectionItem GetSelectionItem(UnityEngine.Vector2 view_position)
		{
			return null;
		}

		public SelectionItem GetHighestPrioritySelectableItem()
		{
			return null;
		}

		public bool SelectHighestPriorityItem()
		{
			return false;
		}

		public void ChangeSelectionItem(UnityEngine.Vector2 position, UnityEngine.Vector2 normalized_direction, bool check_loop, float angle, bool igonre_current_item)
		{
		}

		public (SelectionItem, float) GetSelectionItem(UnityEngine.Vector2 position, UnityEngine.Vector2 normalized_direction, bool check_loop, float angle, SelectionItem ignore_item = null)
		{
			return default((SelectionItem, float));
		}

		public SelectionItem GetDefaultItem()
		{
			return null;
		}

		public bool SelectDefaultItem()
		{
			return false;
		}

		public SelectionItem GetPreSelectedItem()
		{
			return null;
		}

		public bool SelectPreSelectedItem()
		{
			return false;
		}

		private UnityEngine.Vector2 GetScreenEdge(UnityEngine.Vector2 position, UnityEngine.Vector2 ray_direction)
		{
			return default(UnityEngine.Vector2);
		}

		public void Activate()
		{
		}

		public void Deactivate()
		{
		}

		public void Refresh()
		{
		}
	}
}
