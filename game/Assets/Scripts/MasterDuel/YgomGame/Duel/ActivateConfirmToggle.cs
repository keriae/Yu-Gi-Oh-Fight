using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using YgomGame.Settings;


namespace YgomGame.Duel
{
	public class ActivateConfirmToggle : MonoBehaviour
	{
		private DuelHUD duelHUD;

		private YuGiOhFight.Client.Presentation.Elements.ElementObjectManager ui;

		private YuGiOhFight.Client.Presentation.UI.SelectionButton button;

		private YuGiOhFight.Client.Presentation.UI.SelectionItem shortcut;

		private GameObject autoIcon;

		private GameObject onIcon;

		private GameObject offIcon;

		private GameObject shortcutIcon;

		private Image autoIconImage;

		private Image onIconImage;

		private Image offIconImage;


		private float setOnTimer;

		private float setOffTimer;

		private const float setDelayTime = 0.2f;

		private bool reqDelaySet;

		private bool callbackRegisted;

		private List<uint> callbackIDs;

		public bool forceCancelMode
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

		public void Initialize(YuGiOhFight.Client.Presentation.Elements.ElementObjectManager ui, DuelHUD duelHUD)
		{
		}

		private void OnEnable()
		{
		}

		private void OnDisable()
		{
		}

		private void Update()
		{
		}

		public void OnClickToggle(bool playActionAnime)
		{
		}


		public void SetDisp(bool disp)
		{
		}

		public void SetManualType(SettingsUtil.DuelParam.MANUAL_TYPE type)
		{
		}


	}
}
