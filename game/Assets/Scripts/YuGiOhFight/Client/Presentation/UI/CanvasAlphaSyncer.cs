using System;

using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class CanvasAlphaSyncer : UnityEngine.EventSystems.UIBehaviour
	{
		[Serializable]
		private class TargetData
		{
			public Renderer renderer;

			public string alphaParamName;
		}

		[SerializeField]
		private System.Collections.Generic.List<TargetData> m_Targets;

		private float m_LastAlpha;

		private bool m_CanvasGroupDirty;

		private System.Collections.Generic.List<CanvasGroup> m_CanvasGroups;

		protected override void OnBeforeTransformParentChanged()
		{
		}

		protected override void OnCanvasGroupChanged()
		{
		}

		protected override void OnCanvasHierarchyChanged()
		{
		}

		protected override void OnRectTransformDimensionsChange()
		{
		}

		protected override void OnTransformParentChanged()
		{
		}

		private void OnDirtyAlpha()
		{
		}

		private void OnDirtyTransformTree()
		{
		}

		private void LateUpdate()
		{
		}

		public void Apply()
		{
		}

		private void CollectCanvasGroups()
		{
		}

		private float CalcSourceAlpha()
		{
			return 0f;
		}
	}
}
