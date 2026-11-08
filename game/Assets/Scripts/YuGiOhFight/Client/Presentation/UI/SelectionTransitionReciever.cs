namespace YuGiOhFight.Client.Presentation.UI
{
	public class SelectionTransitionReciever : UnityEngine.MonoBehaviour
	{
		public System.Collections.Generic.List<SelectionItem> m_ThrowTargets;

		private SelectionItem m_RecieverCache;

		public SelectionItem recieverItem => null;

		private void OnEnable()
		{
		}

		public bool ThrowToNearTarget()
		{
			return false;
		}

		private void OnSelected()
		{
		}
	}
}
