using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class UISpriteMaskLocator : UnityEngine.EventSystems.UIBehaviour
	{
		[SerializeField]
		private SpriteMask m_SpriteMaskPref;

		[SerializeField]
		private SpriteMask m_SpriteMask;

		private GameObject m_SpriteMaskRoot;

		private System.Collections.Generic.List<UnityEngine.UI.RectMask2D> m_Masks;

		private System.Collections.Generic.List<ParticleSystemRenderer> m_Particles;

		private Vector3 m_LastWorldPos;

		private bool m_Dirty;

		protected override void Start()
		{
		}

		protected override void OnEnable()
		{
		}

		private void LateUpdate()
		{
		}

		protected override void OnDisable()
		{
		}

		private void RefreshComponents()
		{
		}

		private void RefreshTransform()
		{
		}

		private void SettingSpriteMask(UnityEngine.UI.RectMask2D selfMask)
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

		protected override void OnDestroy()
		{
		}
	}
}
