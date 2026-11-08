using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YgomGame.Utility;


namespace YgomGame
{
	public class PopUpTextForSelectionList : MonoBehaviour
	{
		private enum Status
		{
			Opening = 0,
			Showing = 1,
			Closing = 2,
			Free = 3
		}

		private Dictionary<YuGiOhFight.Client.Presentation.UI.SelectionButton, string> m_PupUpBtnTabel;

		private float m_MaxWidth;

		private Coroutine m_CurrentCoroutine;

		private YuGiOhFight.Client.Presentation.UI.SelectionButton m_TargetSbtn;

		private Image m_Arrow;

		private YuGiOhFight.Client.Presentation.UI.TweenAlpha m_TweenAlphaTo;

		private YuGiOhFight.Client.Presentation.UI.TweenScaleTo m_TweenScaleRootTo;

		private YuGiOhFight.Client.Presentation.UI.TweenScaleTo m_TweenScaleArrowTo;

		private Status m_Status;

		private YuGiOhFight.Client.Presentation.Elements.ElementObjectManager m_EOManager;

		private TMPro.TextMeshProUGUI m_PopUpText;

		private RectTransform m_Rt;

		private AdaptiveTextContainer adaptiveTextContainer;

		public void RegistPopUpCallback(YuGiOhFight.Client.Presentation.UI.SelectionButton sbtn, string text)
		{
		}

		public void OnDisappear()
		{
		}

		public void Initialize()
		{
		}

		private void RegistPopUpCallbackImpl(YuGiOhFight.Client.Presentation.UI.SelectionButton sbtn, string text)
		{
		}

		public void SetMaxWidth(int maxWidth)
		{
		}

		private IEnumerator Show(string text)
		{
			return null;
		}

		private void Update()
		{
		}

		private void StopAllTween()
		{
		}
	}
}
