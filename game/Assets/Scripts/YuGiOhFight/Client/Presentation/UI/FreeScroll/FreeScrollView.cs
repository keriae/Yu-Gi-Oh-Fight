namespace YuGiOhFight.Client.Presentation.UI.FreeScroll
{
	public class FreeScrollView : UnityEngine.MonoBehaviour
	{
		private UnityEngine.Canvas m_ScrollRectCanvas;

		private SelectionItem m_ScrollRectSelectionItem;

		private UnityEngine.UI.ScrollRect m_ScrollRect;

		private System.Collections.Generic.List<SelectionItem> m_ContainSelections;

		private System.Collections.Generic.List<SelectionItem> m_InnerViewPortSelections;

		private System.Collections.Generic.List<SelectionItem> m_SortTargetSelections;

		private System.Collections.Generic.Dictionary<SelectionItem, float> m_SortAmounts;

		private bool m_DirtyViewPort;

		private UnityEngine.Vector2 m_LastInputAnalogMain;

		private UnityEngine.Vector2 m_LastInputAnalogSub;

		private float m_LastSelectByAnalogTime;

		private float m_RepeatSelectByAnalogCnt;

		public void AddSelections(System.Collections.Generic.IReadOnlyList<SelectionItem> selections)
		{
		}

		private System.Collections.IEnumerator Start()
		{
			return null;
		}

		private void Update()
		{
		}

		private void ResetAnalogSelectTime()
		{
		}

		public void TrySelectDefault(bool initializeSelection)
		{
		}

		private void ScrollMovement(UnityEngine.Vector2 dir, bool byAnalog = false, bool initializeSelection = false)
		{
		}

		private void OnScroll(UnityEngine.Vector2 vec)
		{
		}

		private void OnInputAnalog(UnityEngine.Vector2 vec, bool isMain)
		{
		}

		private void OnInputUp()
		{
		}

		private void OnInputDown()
		{
		}

		private void OnInputLeft()
		{
		}

		private void OnInputRight()
		{
		}
	}
}
