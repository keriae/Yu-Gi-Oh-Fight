using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	[DisallowMultipleComponent]
	public class LoadingIcon : MonoBehaviour
	{
		[SerializeField]
		private GameObject[] m_HandleTargets;

		private System.Collections.Generic.List<ILoadingIconHandler> m_Handlers;

		[SerializeField]
		private GameObject m_LoadingIconPref;

		[SerializeField]
		private bool m_LoadingIconStretch;

		[SerializeField]
		private GameObject m_LoadingCoverPref;

		[SerializeField]
		private GameObject m_LoadingCoverLocator;

		[SerializeField]
		private System.Collections.Generic.List<GameObject> m_LoadingTweenTargets;

		private GameObject m_LoadingIcon;

		private GameObject m_LoadingCover;

		private System.Collections.IEnumerator m_Sequence;

		public GameObject loadingCoverLocator => null;

		public void AssignHandler(ILoadingIconHandler handler)
		{
		}

		private void GetHandlersState(out bool completed, out bool visible)
		{
			completed = default(bool);
			visible = default(bool);
		}

		private void OnEnable()
		{
		}

		private void OnDisable()
		{
		}

		private void Start()
		{
		}

		private void LateUpdate()
		{
		}

		private System.Collections.IEnumerator ySequence()
		{
			return null;
		}

		private void OnSourceChanged()
		{
		}

		private void Terminate()
		{
		}

		private System.Collections.IEnumerator yPlayRoutines(System.Collections.IEnumerator[] routines)
		{
			return null;
		}

		private System.Collections.IEnumerator yPlayShowLoadingIcon()
		{
			return null;
		}

		private System.Collections.IEnumerator yPlayHideLoadingIcon()
		{
			return null;
		}

		private System.Collections.IEnumerator yPlayShowLoadingCover()
		{
			return null;
		}

		private System.Collections.IEnumerator yPlayHideCoverIcon()
		{
			return null;
		}

		private System.Collections.IEnumerator yPlayLoadStartTarget()
		{
			return null;
		}

		private System.Collections.IEnumerator yPlayLoadEndTarget()
		{
			return null;
		}

		private void TerminateTweenTarget()
		{
		}

		private void TweenStopLabel(GameObject target, bool includeChildren = false, params string[] labels)
		{
		}
	}
}
