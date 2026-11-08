namespace YuGiOhFight.Client.Presentation.UI.ElementWidget
{
	public class ElementWidgetBase : YgomGame.Menu.Common.IAsyncProgressContainer
	{
		private System.Collections.Generic.List<YgomGame.Menu.Common.IAsyncProgressContent> m_AsyncProgressContents;

		public readonly Elements.ElementObjectManager eom;

		public System.Collections.Generic.IReadOnlyList<YgomGame.Menu.Common.IAsyncProgressContent> asyncProgressContents => null;

		public UnityEngine.GameObject gameObject => null;

		public UnityEngine.Transform transform => null;

		public virtual bool visible
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public ElementWidgetBase(Elements.ElementObjectManager eom)
		{
		}

		public virtual void Clear()
		{
		}

		protected void AssignProgressContent(YgomGame.Menu.Common.IAsyncProgressContent progressContent)
		{
		}

		public static implicit operator bool(ElementWidgetBase exists)
		{
			return false;
		}
	}
}
