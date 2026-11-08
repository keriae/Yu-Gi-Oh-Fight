using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace YgomGame.Duel
{
	public class DuelEndMessage : MonoBehaviour
	{
		private YuGiOhFight.Client.Presentation.UI.SelectionButton nextButton;

		private TMP_Text nextButtonText;

		private GameObject messageBase;

		private TextMeshProUGUI messageText;

		private TextMeshProUGUI player0Name;

		private TextMeshProUGUI player1Name;

		private Transform player0Icon;

		private Transform player1Icon;

		private YuGiOhFight.Client.Presentation.UI.SelectionButton profileCard0Button;

		private YuGiOhFight.Client.Presentation.UI.SelectionButton profileCard1Button;

		private GameObject player0PlatformRoot;

		private GameObject player1PlatformRoot;

		private Image player0PlatformIcon;

		private Image player1PlatformIcon;

		private TMP_Text player0PlatformID;

		private TMP_Text player1PlatformID;

		private YuGiOhFight.Client.Presentation.UI.SelectionButton retryButton;

		private GameObject player0WinIcon;

		private GameObject player1WinIcon;

		private GameObject autoGoNextMessageRoot;

		private TMP_Text autoGoNextMessageText;


		private Dictionary<string, object> profileDataMyself;

		private Dictionary<string, object> profileDataRival;

		private const string currentPlatformIconPath = "Images/PlatformIcon/<_PLATFORM_>/CurrentPlatformS";

		public GameObject profileCardObj
		{
			[CompilerGenerated]
			get
			{
				return null;
			}
			[CompilerGenerated]
			private set
			{
			}
		}

		public static bool IsNextButtonClicked
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

		public void Initialize()
		{
		}

		public void Show()
		{
		}

		public void Hide()
		{
		}

		public bool IsShowing()
		{
			return false;
		}

		public void Play(string label)
		{
		}

		public bool IsPlaying(string label)
		{
			return false;
		}

		private void OnNextButton()
		{
		}

		private void OnRetryButton()
		{
		}

		private void OnClickProfileCardButton(Dictionary<string, object> profileData)
		{
		}


		private void SetMessage(string message, bool disp = true)
		{
		}

		public void UpdateAutoGoNextMessage(float currentTime)
		{
		}

		public void SelectEndMessageBtn()
		{
		}

		private void SelectNearestBtnImpl()
		{
		}

		private void SetDispMessage(bool disp)
		{
		}
	}
}
