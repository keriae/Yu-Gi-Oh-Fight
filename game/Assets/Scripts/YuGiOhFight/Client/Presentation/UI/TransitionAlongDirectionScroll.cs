using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class TransitionAlongDirectionScroll : MonoBehaviour
	{
		[SerializeField]
		private System.Collections.Generic.List<TransitionAlongDirection> m_TransitionAlongDirections;

		private void Start()
		{
		}

		private bool OnEdgeTransition(SelectionItem selectionItem, PadInputDirection direction)
		{
			return false;
		}
	}
}
