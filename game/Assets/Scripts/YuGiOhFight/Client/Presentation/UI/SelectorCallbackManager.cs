namespace YuGiOhFight.Client.Presentation.UI
{
	public class SelectorCallbackManager
	{
		private class CallbackInfo
		{
			public SelectorManager.KeyType keyTypeMain;

			public SelectorManager.KeyType keyTypeSub;

			public SelectorManager.MouseType mouseType;

			public SelectorManager.AnalogType analogType;

			public SelectorManager.KeyStatus status;

			public System.Collections.Generic.List<CallbackPack> packs;

			private static System.Collections.Generic.List<uint> resultID;

			public CallbackInfo(SelectorManager.KeyType main = SelectorManager.KeyType.None, SelectorManager.KeyType sub = SelectorManager.KeyType.None, SelectorManager.MouseType mouse = SelectorManager.MouseType.None, SelectorManager.AnalogType analog = SelectorManager.AnalogType.None, SelectorManager.KeyStatus status = SelectorManager.KeyStatus.OnPush)
			{
			}

			public bool Match(SelectorManager.KeyType main = SelectorManager.KeyType.None, SelectorManager.KeyType sub = SelectorManager.KeyType.None, SelectorManager.MouseType mouse = SelectorManager.MouseType.None, SelectorManager.AnalogType analog = SelectorManager.AnalogType.None, SelectorManager.KeyStatus status = SelectorManager.KeyStatus.OnPush)
			{
				return false;
			}

			public bool Equals(SelectorManager.KeyType main = SelectorManager.KeyType.None, SelectorManager.KeyType sub = SelectorManager.KeyType.None, SelectorManager.MouseType mouse = SelectorManager.MouseType.None, SelectorManager.AnalogType analog = SelectorManager.AnalogType.None, SelectorManager.KeyStatus status = SelectorManager.KeyStatus.OnPush)
			{
				return false;
			}

			public void AddCallback(uint id, SelectionItem item, System.Func<bool> callback)
			{
			}

			public void AddCallback(uint id, int priority, System.Func<bool> callback)
			{
			}

			public bool InvokeCallback(int priorityMin, SelectionItem item = null)
			{
				return false;
			}

			public System.Collections.Generic.List<uint> RemoveCallback(SelectionItem item)
			{
				return null;
			}

			public System.Collections.Generic.List<uint> RemoveCallback(SelectionItem item, System.Func<bool> callback)
			{
				return null;
			}

			public System.Collections.Generic.List<uint> RemoveCallback(int priority, System.Func<bool> callback)
			{
				return null;
			}

			public System.Collections.Generic.List<uint> RemoveCallback(uint id)
			{
				return null;
			}

			public System.Collections.Generic.List<uint> ClearCallback()
			{
				return null;
			}

			public bool Exist(uint id)
			{
				return false;
			}

			public void Cleanup()
			{
			}

			public override string ToString()
			{
				return null;
			}
		}

		private class CallbackPack
		{
			public uint id;

			public SelectionItem item;

			public System.Func<bool> callback;

			public int priority;

			public CallbackPack(uint id, SelectionItem item, System.Func<bool> callback)
			{
			}

			public CallbackPack(uint id, int priority, System.Func<bool> callback)
			{
			}

			public void ClearCallback()
			{
			}

			public override string ToString()
			{
				return null;
			}
		}

		private System.Collections.Generic.List<CallbackInfo> selectedCallbacks;

		private System.Collections.Generic.List<CallbackInfo> shortcutCallbacks;

		private System.Collections.Generic.List<uint> packIDList;

		private uint createCount;

		private static System.Collections.Generic.List<uint> resultID;

		private int activeClusterPriorityMin;

		public uint AddSelectedCallback(SelectionItem item, SelectorManager.KeyStatus status, SelectorManager.KeyType main, SelectorManager.KeyType sub, SelectorManager.MouseType mouse, SelectorManager.AnalogType analog, System.Func<bool> callback)
		{
			return 0u;
		}

		public System.Collections.Generic.List<uint> ClearSelectedCallback(SelectionItem item)
		{
			return null;
		}

		public System.Collections.Generic.List<uint> RemoveSelectedCallback(SelectionItem item, SelectorManager.KeyStatus status, SelectorManager.KeyType main, SelectorManager.KeyType sub, SelectorManager.MouseType mouse, SelectorManager.AnalogType analog, System.Func<bool> callback)
		{
			return null;
		}

		public System.Collections.Generic.List<uint> RemoveSelectedCallback(SelectionItem item, SelectorManager.KeyStatus status, SelectorManager.KeyType main, SelectorManager.KeyType sub, SelectorManager.MouseType mouse, SelectorManager.AnalogType analog)
		{
			return null;
		}

		public System.Collections.Generic.List<uint> RemoveCallback(uint packID)
		{
			return null;
		}

		public uint AddShortcutCallback(SelectionItem item, SelectorManager.KeyStatus status, SelectorManager.KeyType main, SelectorManager.KeyType sub, SelectorManager.MouseType mouse, SelectorManager.AnalogType analog, System.Func<bool> callback)
		{
			return 0u;
		}

		public System.Collections.Generic.List<uint> ClearShortcutCallback(SelectionItem item)
		{
			return null;
		}

		public System.Collections.Generic.List<uint> RemoveShortcutCallback(SelectionItem item, SelectorManager.KeyStatus status, SelectorManager.KeyType main, SelectorManager.KeyType sub, SelectorManager.MouseType mouse, SelectorManager.AnalogType analog, System.Func<bool> callback)
		{
			return null;
		}

		public System.Collections.Generic.List<uint> RemoveShortcutCallback(SelectionItem item, SelectorManager.KeyStatus status, SelectorManager.KeyType main, SelectorManager.KeyType sub, SelectorManager.MouseType mouse, SelectorManager.AnalogType analog)
		{
			return null;
		}

		public uint AddShortcutCallback(int priority, SelectorManager.KeyStatus status, SelectorManager.KeyType main, SelectorManager.KeyType sub, SelectorManager.MouseType mouse, SelectorManager.AnalogType analog, System.Func<bool> callback)
		{
			return 0u;
		}

		public System.Collections.Generic.List<uint> RemoveShortcutCallback(int priority, SelectorManager.KeyStatus status, SelectorManager.KeyType main, SelectorManager.KeyType sub, SelectorManager.MouseType mouse, SelectorManager.AnalogType analog, System.Func<bool> callback)
		{
			return null;
		}

		private CallbackInfo GetCallbackInfo(System.Collections.Generic.List<CallbackInfo> targetList, SelectorManager.KeyStatus status, SelectorManager.KeyType main = SelectorManager.KeyType.None, SelectorManager.KeyType sub = SelectorManager.KeyType.None, SelectorManager.MouseType mouse = SelectorManager.MouseType.None, SelectorManager.AnalogType analog = SelectorManager.AnalogType.None)
		{
			return null;
		}

		private uint IssueCallbackPackID()
		{
			return 0u;
		}

		public bool InvokeSelectedCallback(SelectionItem selectedItem, SelectorManager.KeyStatus status, SelectorManager.KeyType main, SelectorManager.KeyType sub, SelectorManager.MouseType mouse, SelectorManager.AnalogType analog)
		{
			return false;
		}

		public bool InvokeShortcutCallback(SelectorManager.KeyStatus status, SelectorManager.KeyType main, SelectorManager.KeyType sub, SelectorManager.MouseType mouse, SelectorManager.AnalogType analog)
		{
			return false;
		}

		public void Cleanup()
		{
		}

		public void SetActiveClusterPriority(int min, bool force)
		{
		}
	}
}
