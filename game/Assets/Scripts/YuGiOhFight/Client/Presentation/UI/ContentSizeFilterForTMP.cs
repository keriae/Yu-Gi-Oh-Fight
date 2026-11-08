using System.Runtime.CompilerServices;

using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class ContentSizeFilterForTMP : MonoBehaviour
	{
		[SerializeField]
		private float minWidth;

		[SerializeField]
		private float minHeight;

		[SerializeField]
		private TMPro.TextMeshProUGUI m_TargetTmp;

		private RectTransform rectTransform;

		private bool m_UpdateSize;

		public bool EnableWidthControl;

		public bool EnableHeightControl;

		public event UnityEngine.Events.UnityAction onResized
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		private void Awake()
		{
		}

		private void Update()
		{
		}
	}
}
