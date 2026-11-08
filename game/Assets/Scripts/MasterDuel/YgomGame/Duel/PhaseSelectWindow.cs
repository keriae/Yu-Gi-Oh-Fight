using UnityEngine;
using UnityEngine.Events;


namespace YgomGame.Duel
{
	public class PhaseSelectWindow : MonoBehaviour
	{
		private const string LABEL_EO_WINDOW = "Window";

		private const string LABEL_EO_CONTENT = "Content";

		private const string LABEL_EO_BUTTON_CLOSE = "ButtonClose";

		private const string LABEL_EO_BUTTONPHASE = "ButtonPhase";

		private const string LABEL_EO_TEXTTITLE = "TextTitle";

		private const string LABEL_TW_IN = "In";

		private const string LABEL_TW_OUT = "Out";

		private const string LABEL_TW_CURRENT = "Current";

		private const string LABEL_TW_NORMAL = "Normal";

		private const string LABEL_TW_SELECT = "select";


		private YuGiOhFight.Client.Presentation.Elements.ElementObjectManager m_RootManager;

		private YuGiOhFight.Client.Presentation.UI.SelectionButton m_ButtonClose;

		private YuGiOhFight.Client.Presentation.UI.SelectionButton[] m_ButtonPhases;

		private const int m_PhaseNum = 6;

		public bool isOpened => false;


		private void SetPhaseButton()
		{
		}

		public void OpenWindow()
		{
		}

		public void CloseWindow()
		{
		}
	}
}
