using System.Collections;

using UnityEngine;


namespace YuGiOhFight.Client.Controllers
{
    public class OnlineDeckViewer : Servant
    {

        public static MDPro3.Net.OnlineDeck.OnlineDeckData[] decks;
        [HideInInspector] public UI.SelectionToggle_DeckOnline lastSelectedDeckItem;

        public override int Depth => 5;
        protected override bool ShowLine => true;

        #region Servant
        public override void Initialize()
        {
            returnServant = Runtime.Program.instance.deckSelector;
            base.Initialize();
        }

        protected override void ApplyShowArrangement(int preDepth)
        {
            base.ApplyShowArrangement(preDepth);
            RefreshList();
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !Runtime.UserInput.NeedDefaultSelect())
                return;

            lastSelectedDeckItem.GetSelectable().Select();
        }

        public override void PerFrameFunction()
        {
            if (NeedResponseInput())
            {
                if (Runtime.UserInput.MouseRightDown || Runtime.UserInput.WasCancelPressed)
                    OnReturn();
                if (Runtime.UserInput.WasGamepadButtonWestPressed)
                {
                    Runtime.AudioManager.PlaySE("SE_MENU_SELECT_01");
                    GetUI<UI.ServantUI.OnlineDeckViewerUI>().InputDeckName.ActivateInputField();
                }
                if (Runtime.UserInput.WasGamepadButtonNorthPressed)
                {
                    Runtime.AudioManager.PlaySE("SE_MENU_SELECT_01");
                    GetUI<UI.ServantUI.OnlineDeckViewerUI>().InputDeckAuthor.ActivateInputField();
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
            var task = MDPro3.Net.OnlineDeck.FetchSimpleDeckList(10000
                , GetUI<UI.ServantUI.OnlineDeckViewerUI>().InputDeckName.text
                , GetUI<UI.ServantUI.OnlineDeckViewerUI>().InputDeckAuthor.text);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
            {
                decks = task.Result;
                if (decks == null)
                {
                    Runtime.MessageManager.Cast(MDPro3.InterString.Get("网络异常，获取在线卡组列表失败。"));
                    yield break;
                }
                GetUI<UI.ServantUI.OnlineDeckViewerUI>().Print();
            }
            else
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("网络异常，获取在线卡组列表失败。"));
        }
    }
}
