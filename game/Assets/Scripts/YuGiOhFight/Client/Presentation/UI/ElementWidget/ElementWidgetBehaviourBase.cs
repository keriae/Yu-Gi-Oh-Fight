namespace YuGiOhFight.Client.Presentation.UI.ElementWidget
{
	public class ElementWidgetBehaviourBase<T> : UnityEngine.MonoBehaviour where T : ElementWidgetBehaviourBase<T>
	{
		private Elements.ElementObjectManager m_EomCache;

		public Elements.ElementObjectManager eom => null;

		protected static T InnerCreate(Elements.ElementObjectManager eom)
		{
			return null;
		}

		protected virtual void CollectComponents()
		{
		}
	}
}
