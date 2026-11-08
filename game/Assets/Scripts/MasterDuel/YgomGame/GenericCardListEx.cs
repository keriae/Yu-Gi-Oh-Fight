using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using YgomGame.Duel;


namespace YgomGame
{
	public class GenericCardListEx : MonoBehaviour, YuGiOhFight.Client.Presentation.UI.IGenericScrollViewSupport
	{
		protected YuGiOhFight.Client.Presentation.Elements.ElementObjectManager m_EoManager;

		protected YuGiOhFight.Client.Presentation.UI.GenericScrollView m_ScrollView;

		protected RawImage m_OriginCard;

		protected YuGiOhFight.Client.Presentation.UI.SelectionButton m_OriginCardSbtn;

		protected List<int> m_Datalist;

		protected CardInfoForGenericCardListEx m_CardInfoForRelativeCard;

		protected int m_OriginCardid;

		protected int m_ShowingCardid;

		protected bool m_IsVisible;

		protected YuGiOhFight.Client.Presentation.UI.SelectionButton m_RelativeCardSbtn;

		protected bool m_IsFading;

		protected TMPro.TextMeshProUGUI m_TitleText;

		protected FullScreenUiBg m_FullScreenUiBg;

		protected YuGiOhFight.Client.Presentation.UI.UiSwitchTweenAnimationController m_UiSwitchTweenAnimationController;

		public static void Create(Transform parent, UnityAction<int> onClick, bool ismobile, UnityAction<GenericCardListEx> onFinish)
		{
		}

		public void SetFullScreenUiBg(FullScreenUiBg fullScreenUiBg)
		{
		}

		public void Show()
		{
		}

		public void Close(bool closebg = true)
		{
		}

		public void ShowRelativeCardByCardid(int cardid)
		{
		}

		protected void Initialize(UnityAction<int> onClick, bool ismobile)
		{
		}

		private void InitCardInfo(UnityAction<int> onClick, bool ismobile)
		{
		}

		private void InitTween(GameObject root, GameObject scrollview)
		{
		}

		private void SetRelativeCardList(int cardid, bool updatelist = true)
		{
		}

		private void UpdateScrollView()
		{
		}

		public void OnItemSetData(GameObject gob, int dataindex)
		{
		}

		public void OnItemExit(GameObject gob, int dataindex)
		{
		}

		public void OnItemInitialize(GameObject gob)
		{
		}

		public void OnGsvStanby()
		{
		}
	}
}
