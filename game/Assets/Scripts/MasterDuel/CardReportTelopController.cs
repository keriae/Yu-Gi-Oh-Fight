using System.Runtime.CompilerServices;
using UnityEngine;

using YgomGame.Stats;


public class CardReportTelopController : MonoBehaviour
{
	public enum Step
	{
		Idle = 0,
		Opening = 1,
		Showing = 2,
		Closing = 3
	}

	private YuGiOhFight.Client.Presentation.Elements.ElementObjectManager m_EOManager;

	//private RawImage m_CardPicture;

	private TMPro.TextMeshProUGUI m_ItemName;

	private TMPro.TextMeshProUGUI m_ItemBody;

	private GameObject m_Effect1;

	private GameObject m_Effect2;

	private GameObject m_Bg0;

	private GameObject m_Bg1;

	private GameObject m_Bg2;

	private YuGiOhFight.Client.Presentation.UI.Tween m_Tween;

	private int m_Countdown;

	public Step step
	{
		[CompilerGenerated]
		get
		{
			return default(Step);
		}
		[CompilerGenerated]
		private set
		{
		}
	}

	private void Awake()
	{
	}

	private void FixedUpdate()
	{
	}

	public void SetStateContent(int cardid, string message, string messageNum, string messageUnit, CardStatsData.CARD_STATS_EFFECT_TYPE effecttype)
	{
	}

	public void Show(int duration)
	{
	}

	public void Hide()
	{
	}

	public void HideEffect()
	{
	}

	public void OnHide()
	{
	}

	public void OnShow()
	{
	}
}
