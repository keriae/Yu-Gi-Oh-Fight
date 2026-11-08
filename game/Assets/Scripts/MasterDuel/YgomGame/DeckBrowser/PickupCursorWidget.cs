using System;
using UnityEngine;


namespace YgomGame.DeckBrowser
{
	public class PickupCursorWidget : YuGiOhFight.Client.Presentation.UI.ElementWidget.ElementWidgetBehaviourBase<PickupCursorWidget>
	{
		private const string k_PrefPath = "Prefabs/UI/DeckBrowser/Optionals/PickupCursor";

		private readonly string k_ELabelText;

		private bool m_IsActive;

		private GameObject m_GameObject;

		private int m_PickupId;

		private TMPro.TextMeshProUGUI m_Text;

		public Action onClickedCallback;

		public Action onCreated;

		public bool isActive => false;

		public int pickupId
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		public void Initialize(YuGiOhFight.Client.Presentation.Elements.ElementObjectManager eom, int pickupId = -1)
		{
		}

		public static void Create(Transform parent, Action<PickupCursorWidget> onCreated, int pickupId = -1)
		{
		}

		private void SetPickupId(int pickupId)
		{
		}

		public PickupCursorWidget()
		{
			//((ElementWidgetBehaviourBase<>)(object)this)._002Ector();
		}
	}
}
