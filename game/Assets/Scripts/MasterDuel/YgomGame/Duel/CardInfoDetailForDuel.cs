using UnityEngine;
using UnityEngine.Events;


namespace YgomGame.Duel
{
	public class CardInfoDetailForDuel : CardInfoDetail
	{
		protected const string PATH_PREHABFORDUEL = "Prefabs/Duel/UI/CardInfoDetailForDuel";

		private YuGiOhFight.Client.Presentation.UI.ElementWidget.ToggleWidget m_OriginInfoToggle;

		private YuGiOhFight.Client.Presentation.UI.SelectionButton m_RelativeCardButton;


		private bool m_BlcokRelatriveCard;


		protected override void InitializeBase()
		{
		}

		public new void Show()
		{
		}

		private void InitToggle()
		{
		}

		private void SetCardByOriginalInfo()
		{
		}
	}
}
