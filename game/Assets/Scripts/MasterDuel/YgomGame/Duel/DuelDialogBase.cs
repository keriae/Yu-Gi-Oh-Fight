using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


using YgomSystem.Utility;

namespace YgomGame.Duel
{
	public abstract class DuelDialogBase : DuelUIBase
	{
		[SerializeField]
		protected GameObject prefabDialogBase;

		private YuGiOhFight.Client.Presentation.Elements.ElementObjectManager dialogBaseUI;

		private YuGiOhFight.Client.Presentation.UI.Selector baseSelector;

		private YuGiOhFight.Client.Presentation.UI.SelectionItem screenItem;

		protected bool fieldViewing;

		private Transform fieldViewRoot;

		private Image fieldViewIconOn;

		private Image fieldViewIconOff;

		private YuGiOhFight.Client.Presentation.UI.TweenPositionTo tweenStartFieldView;

		private const float fieldViewToPosOffset = 18f;

		private const string tweenLabelStartFieldView = "StartFieldView";

		private const string tweenLabelFinishFieldView = "FinishFieldView";

		private bool useCardInfo;

		[SerializeField]
		private GameObject prefabUI;

		protected YuGiOhFight.Client.Presentation.Elements.ElementObjectManager ui;

		protected YuGiOhFight.Client.Presentation.UI.Selector selector;

		protected RectTransform dialog;

		protected List<YuGiOhFight.Client.Presentation.UI.Selector> selectors;

		protected abstract bool useFieldView { get; }

		protected override UITransitionUtil.BlockType openCloseBlockType => default(UITransitionUtil.BlockType);

		public override void Initialize(RunEffectWorker effectWorker)
		{
		}

		protected virtual void CreateUI()
		{
		}

		private void CreateBaseUI()
		{
		}

		protected void SetupFieldViewButton(YuGiOhFight.Client.Presentation.Elements.ElementObjectManager eom, string buttonLabel, string shortcutLabel, string iconOnLabel, string iconOffLabel)
		{
		}

		protected void SetDialogSize(Vector2 size)
		{
		}

		private void OnChangeFieldView(bool fieldViewing, bool isAbort)
		{
		}

		protected virtual void StartFieldView()
		{
		}

		protected virtual void FinishFieldView(bool isAbort)
		{
		}

		public virtual void Abort()
		{
		}

		public abstract void OnCancel();

		protected override void Open(Action openedCallback = null)
		{
		}

		protected override void Close(Action closedCallback = null)
		{
		}

		protected override void ShowUI()
		{
		}

		protected override void HideUI()
		{
		}

		public void OpenCardInfo(int mixid, int efx, bool effectidflag)
		{
		}

		private void OnChangeDuelLogOpenClose(bool isOpen)
		{
		}
	}
}
