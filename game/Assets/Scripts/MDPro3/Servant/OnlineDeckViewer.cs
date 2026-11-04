using DG.Tweening;
using MDPro3;
using MDPro3.UI;
using MDPro3.Net;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using MDPro3.UI.PropertyOverride;
using MDPro3.UI.ServantUI;

namespace MDPro3.Servant
{
    public class OnlineDeckViewer : Servant
    {

        public static OnlineDeck.OnlineDeckData[] decks;
        [HideInInspector] public SelectionToggle_DeckOnline lastSelectedDeckItem;

        public override int Depth => 5;
        protected override bool ShowLine => true;

        #region Servant
        public override void Initialize()
        {
            returnServant = Program.instance.deckSelector;
            base.Initialize();
        }

        protected override void ApplyShowArrangement(int preDepth)
        {
            base.ApplyShowArrangement(preDepth);
            RefreshList();
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !UserInput.NeedDefaultSelect())
                return;

            lastSelectedDeckItem.GetSelectable().Select();
        }

        public override void PerFrameFunction()
        {
            if (NeedResponseInput())
            {
                if (UserInput.MouseRightDown || UserInput.WasCancelPressed)
                    OnReturn();
                if (UserInput.WasGamepadButtonWestPressed)
                {
                    AudioManager.PlaySE("SE_MENU_SELECT_01");
                    GetUI<OnlineDeckViewerUI>().InputDeckName.ActivateInputField();
                }
                if (UserInput.WasGamepadButtonNorthPressed)
                {
                    AudioManager.PlaySE("SE_MENU_SELECT_01");
                    GetUI<OnlineDeckViewerUI>().InputDeckAuthor.ActivateInputField();
                }
            }
        }

        #endregion

        public void RefreshList()
        {
            decks = null;
            StartCoroutine(RefreshAsync());
        }

        private IEnumerator RefreshAsync()
        {
            var task = OnlineDeck.FetchSimpleDeckList(10000
                , GetUI<OnlineDeckViewerUI>().InputDeckName.text
                , GetUI<OnlineDeckViewerUI>().InputDeckAuthor.text);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
            {
                decks = task.Result;
                if (decks == null)
                {
                    MessageManager.Cast(InterString.Get("网络异常，获取在线卡组列表失败。"));
                    yield break;
                }
                GetUI<OnlineDeckViewerUI>().Print();
            }
            else
                MessageManager.Cast(InterString.Get("网络异常，获取在线卡组列表失败。"));
        }
    }
}
