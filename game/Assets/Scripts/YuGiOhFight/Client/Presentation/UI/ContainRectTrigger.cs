using System.Runtime.CompilerServices;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class ContainRectTrigger : UnityEngine.MonoBehaviour
	{
		private UnityEngine.RectTransform m_Container;

		private UnityEngine.RectTransform m_Target;

		public bool isContain
		{
			[CompilerGenerated]
			get
			{
				return false;
			}
			[CompilerGenerated]
			private set
			{
			}
		}

		public event System.Action<bool> onChangeEvent
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

		public static ContainRectTrigger Attach(UnityEngine.RectTransform target, UnityEngine.RectTransform container, System.Action<bool> onChangeCallback = null)
		{
			return null;
		}

		private void Start()
		{
		}

		private void LateUpdate()
		{
		}

		private bool IsInViewport()
		{
			return false;
		}
	}
}
