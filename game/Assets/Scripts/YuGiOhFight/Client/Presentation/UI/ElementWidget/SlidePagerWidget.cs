using System.Runtime.CompilerServices;
using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI.ElementWidget
{
	public class SlidePagerWidget : ElementWidgetBehaviourBase<SlidePagerWidget>
	{
		private readonly string k_ELabelIndicatorTemplate;

		private readonly string k_ELabelPrevButton;

		private readonly string k_ELabelNextButton;

		[SerializeField]
		private string m_TweenIndicatorOn;

		[SerializeField]
		private string m_TweenIndicatorOff;

		private ScrollRectPageSnap m_PageSnap;

		private SelectionButton m_PrevButton;

		private SelectionButton m_NextButton;

		private System.Collections.Generic.List<GameObject> m_Indicators;

		private Coroutine m_yMovePageRoutine;

		private Selector m_Selector;

		private InfinityScroll.InfinityScrollView m_Isv;

		private System.Collections.Generic.List<SelectionItem> m_TmpOrderItems;

		public int startPage;

		public bool isLoop;

		public ScrollRectPageSnap pageSnap => null;

		public SelectionButton prevButton => null;

		public SelectionButton nextButton => null;

		public event System.Action onPageChanged
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

		private bool IsValidIdx(int idx)
		{
			return false;
		}

		private bool IsEnablePrev()
		{
			return false;
		}

		private bool IsEnableNext()
		{
			return false;
		}

		public static SlidePagerWidget Create(Elements.ElementObjectManager eom)
		{
			return null;
		}

		protected override void CollectComponents()
		{
		}

		private void Start()
		{
		}

		public void SetPageTotal(int pageTotal)
		{
		}

		private void PlayIndicatorTween(bool immediate = false)
		{
		}

		private void UpdateButtons()
		{
		}

		public void InitMovePage(Selector seletor, InfinityScroll.InfinityScrollView isv)
		{
		}

		public bool MovePage(int dstPage)
		{
			return false;
		}

		private void OnClickNext()
		{
		}

		private void OnClickPrev()
		{
		}

		private void OnPageChanged()
		{
		}

		private System.Collections.IEnumerator yMovePage(int dstPage, System.Action onComplete = null)
		{
			return null;
		}

		public SlidePagerWidget()
		{
			//((ElementWidgetBehaviourBase<>)(object)this)._002Ector();
		}
	}
}
