using System;
using System.Collections.Generic;
using UnityEngine;


namespace YgomGame.Duel
{
	public class DuelDiceDialog : DuelDialogBase
	{
		private class DiceInfo
		{
			public YuGiOhFight.Client.Presentation.Elements.ElementObjectManager dice;

			public int index;

			public int pip;
		}

		private Action<List<int>, bool> resultCallback;

		private List<int> result;

		private int selectNum;

		private TMPro.TextMeshProUGUI textMessage;

		private List<DiceInfo> diceList;

		private GameObject counterArea;

		private TMPro.TextMeshProUGUI textCounter;

		private YuGiOhFight.Client.Presentation.UI.SelectionButton decideButton;

		private const string prefabPath = "Prefabs/Duel/DuelDiceDialog";

		private int currentResultIndex => 0;

		protected override bool useFieldView => false;

		public static void Create(RunEffectWorker effectWorker, Transform parent, Action<DuelDiceDialog> finishCallback)
		{
		}

		protected override void CreateUI()
		{
		}

		private void SelectItem(int index)
		{
		}

		private void SelectDecideButton()
		{
		}

		private void SetResult(int result, int index)
		{
		}

		private void CancelLastResult()
		{
		}

		private void CancelResult(int result)
		{
		}

		private void UpdateDiceList()
		{
		}

		private void UpdateCounter()
		{
		}

		private void UpdateDecideButton()
		{
		}

		protected override void OnClosed()
		{
		}

		protected override void ShowUI()
		{
		}

		protected override void HideUI()
		{
		}

		public override void Abort()
		{
		}

		public override void OnCancel()
		{
		}

		public void Open(string message, List<int> selectionList, int selectNum, Action<List<int>, bool> resultCallback, Action openCallback)
		{
		}

		private void SetupDiceList(List<int> selectionList)
		{
		}
	}
}
