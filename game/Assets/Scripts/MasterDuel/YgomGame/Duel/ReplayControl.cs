using System;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace YgomGame.Duel
{
	public class ReplayControl : MonoBehaviour
	{
		private DuelHUD duelHUD;

		private YuGiOhFight.Client.Presentation.Elements.ElementObjectManager ui;

		private YuGiOhFight.Client.Presentation.UI.SelectionButton playButton;

		private YuGiOhFight.Client.Presentation.UI.SelectionButton pauseButton;

		private GameObject message;

		private GameObject realtimeReplay;

		private GameObject ffIconOn;

		private GameObject ffIconOff;

		private bool initialized;

		public Action<bool> OnChangeReplayPause
		{
			[CompilerGenerated]
			get
			{
				return null;
			}
			[CompilerGenerated]
			set
			{
			}
		}

		public Action OnFastReplay
		{
			[CompilerGenerated]
			get
			{
				return null;
			}
			[CompilerGenerated]
			set
			{
			}
		}

		public bool IsPause
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

		private void Awake()
		{
		}

		public void Initialize(YuGiOhFight.Client.Presentation.Elements.ElementObjectManager ui, DuelHUD duelHUD, GameObject replayMessage)
		{
		}

		private void Update()
		{
		}

		public void OnTapPause()
		{
		}

		public void OnTapPlay()
		{
		}

		public void OnTapFast()
		{
		}

		private void UpdateFFIcon()
		{
		}

		public void SetDisp(bool disp)
		{
		}
	}
}
