using System;
using UnityEngine;
using UnityEngine.UI;


using YgomSystem.Utility;

namespace YgomGame.Duel
{
	public abstract class DuelInfoDialogBase : DuelTransitionUIBase
	{
		public enum Place
		{
			Near = 0,
			Nearer = 1,
			Center = 2,
			Farer = 3,
			Far = 4,
			Farest = 5
		}

		protected class OperationInfo : OperationInfoBase
		{
			public static OperationInfo CloseOperation(DuelInfoDialogBase dialog)
			{
				return null;
			}

			public static OperationInfo SetPlaceOperation(DuelInfoDialogBase dialog, Place place, bool immediate)
			{
				return null;
			}
		}

		[SerializeField]
		protected GameObject prefabUI;

		protected Action cancelCallback;

		protected Action decisionCallback;

		protected Action closeCallback;

		protected YuGiOhFight.Client.Presentation.Elements.ElementObjectManager ui;

		protected YuGiOhFight.Client.Presentation.UI.Selector selector;

		protected TMPro.TextMeshProUGUI textMessage;

		protected ContentSizeFitter dialogFitter;

		protected GameObject dialogObject;

		private Place currentPlace;

		private Place targetPlace;

		private float timer;

		private const float placeChangeTime = 1f;

		protected override UITransitionUtil.BlockType openCloseBlockType => default(UITransitionUtil.BlockType);

		public void ReqClose()
		{
		}

		public void ReqSetPlace(Place place, bool immediate)
		{
		}

		public override void Initialize(RunEffectWorker effectWorker)
		{
		}

		protected abstract void CreateUI();

		private void OnClickCancel()
		{
		}

		private void OnClickDecision()
		{
		}

		protected override void ShowUI()
		{
		}

		protected override void HideUI()
		{
		}

		protected void Open(string message, Place place, bool cancelable, Action cancelCallback, Action closeCallback = null, bool decidable = false, Action decisionCallback = null)
		{
		}

		protected void SetPlace(Place place, bool immediate)
		{
		}

		protected override void UpdateUI()
		{
		}

		private void UpdatePlace()
		{
		}

		private void Close()
		{
		}
	}
}
