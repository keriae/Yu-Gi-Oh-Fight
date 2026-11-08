using System;
using System.Collections.Generic;
using UnityEngine;


namespace YgomGame.Duel
{
	public class DuelStatusViewer : MonoBehaviour
	{
		public enum UpdateStatusType
		{
			None = 0,
			OnTurnChange = 1
		}

		public struct DuelStatusSideInfo
		{
			public bool canIDoSummon;

			public bool canIDoSpecialSummon;

			public int canIDoPutMonster;

			public int totalAtk;
		}

		public struct DuelStatusInfo
		{
			public DuelStatusSideInfo nearStatus;

			public DuelStatusSideInfo farStatus;

			public Engine.Phase phase;
		}

		[SerializeField]
		private GameObject prefabUI;

		private YuGiOhFight.Client.Presentation.Elements.ElementObjectManager ui;

		private DuelGameObjectManager goManager;

		private YuGiOhFight.Client.Presentation.Elements.ElementObjectManager buttonUI;

		private YuGiOhFight.Client.Presentation.UI.Selector selector;

		private YuGiOhFight.Client.Presentation.UI.SelectionButton openButton;

		private SummonInfoViewer summonInfoViewer;

		private bool opening;

		private bool reqOpen;

		private List<DeckCardPlace> deckCardPlaces;

		private List<GraveCardPlace> graveCardPlaces;

		private List<HandCardPlace> handCardPlaces;

		private bool initialized;

		private bool initStatus;

		private const string LABEL_ON = "On";

		private const string LABEL_OFF = "Off";

		private const string prefabPath = "Prefabs/Duel/DuelStatusViewer";

		public static void Create(DuelGameObjectManager goManager, DuelHUD duelHUD, Transform parent, YuGiOhFight.Client.Presentation.Elements.ElementObjectManager buttonUI, Action<DuelStatusViewer> finishCallback)
		{
		}

		private void Initialize(DuelGameObjectManager goManager, DuelHUD duelHUD, YuGiOhFight.Client.Presentation.Elements.ElementObjectManager buttonUI)
		{
		}

		public void Open()
		{
		}

		public void Close()
		{
		}

		private void SetShowStatusDetail(bool show)
		{
		}

		private void SetShowStatusDetail(int player, bool show)
		{
		}

		private void OnDestroy()
		{
		}

		private void Update()
		{
		}

		private void InitStatus()
		{
		}

		public void UpdateStatus(DuelStatusInfo status, UpdateStatusType updateType = UpdateStatusType.None)
		{
		}

		private void UpdateStatus(SharedDefinition.Location location, DuelStatusSideInfo info, Engine.Phase phase, UpdateStatusType updateType = UpdateStatusType.None)
		{
		}

		public static DuelStatusInfo GetDuelStatusInfo()
		{
			return default(DuelStatusInfo);
		}

		private static DuelStatusSideInfo GetDuelStatusInfo(int player)
		{
			return default(DuelStatusSideInfo);
		}

		private void UpdateButton()
		{
		}

		public void SetDispButton(bool disp)
		{
		}
	}
}
