using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI.ServantUI
{
    public class OnlineDeckViewerUI : ServantUI
    {

        #region Elements

        private const string LABEL_SR = "ScrollRect";
        private ScrollRect m_ScrollRect;
        private ScrollRect ScrollRect =>
            m_ScrollRect = m_ScrollRect != null ? m_ScrollRect
            : Manager.GetElement<ScrollRect>(LABEL_SR);

        private const string LABEL_TXT_DECKNUMVALUE = "TextDeckNumValue";
        private TextMeshProUGUI m_TextDeckNumValue;
        private TextMeshProUGUI TextDeckNumValue =>
            m_TextDeckNumValue = m_TextDeckNumValue != null ? m_TextDeckNumValue
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_DECKNUMVALUE);

        private const string LABEL_IPT_DECKNAME = "InputFieldDeckName";
        private TMP_InputField m_InputDeckName;
        public TMP_InputField InputDeckName =>
            m_InputDeckName = m_InputDeckName != null ? m_InputDeckName
            : Manager.GetElement<TMP_InputField>(LABEL_IPT_DECKNAME);

        private const string LABEL_IPT_DECKAUTHOR = "InputFieldDeckAuthor";
        private TMP_InputField m_InputDeckAuthor;
        public TMP_InputField InputDeckAuthor =>
            m_InputDeckAuthor = m_InputDeckAuthor != null ? m_InputDeckAuthor
            : Manager.GetElement<TMP_InputField>(LABEL_IPT_DECKAUTHOR);

        #endregion

        public SuperScrollView superScrollView;

        protected override void AfterHideEvent()
        {
            base.AfterHideEvent();

            superScrollView?.Clear();
        }

        public void OnSearchSubmit()
        {
            Runtime.Program.instance.onlineDeckViewer.RefreshList();
        }

        public void Print()
        {
            superScrollView?.Clear();
            TextDeckNumValue.text = Controllers.OnlineDeckViewer.decks.Length.ToString();

            var scale = MDPro3.Config.GetUIScale();

            var handle = Addressables.LoadAssetAsync<GameObject>("UI/ItemDeckOnline.prefab");
            handle.Completed += (result) =>
            {
                var itemWidth = PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 336f : 260f;
                var itemHeight = PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 300f : 232f;
                var space = PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 30f : 24f;
                var bottomPadding = (PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 196f : 150f) - space;
                superScrollView = new SuperScrollView(
                    -1,
                    itemWidth + space,
                    itemHeight + space,
                    10,
                    bottomPadding,
                    result.Result,
                    ItemOnListRefresh,
                    ScrollRect);
                List<string[]> tasks = new();
                foreach (MDPro3.Net.OnlineDeck.OnlineDeckData deck in Controllers.OnlineDeckViewer.decks)
                {
                    var task = new string[10]
                    {
                        deck.deckName,
                        deck.deckContributor,
                        deck.deckId,
                        deck.deckCase == 0 ? "1080001" : deck.deckCase.ToString(),
                        deck.deckCoverCard1.ToString(),
                        deck.deckCoverCard2.ToString(),
                        deck.deckCoverCard3.ToString(),
                        deck.deckProtector == 0 ? "1070001" : deck.deckProtector.ToString(),
                        deck.deckLike.ToString(),
                        deck.GetOnlineDeckLocalTime().ToString()
                    };
                    tasks.Add(task);
                }
                superScrollView.Print(tasks);
                if (superScrollView.items.Count > 0)
                {
                    Runtime.Program.instance.onlineDeckViewer.lastSelectedDeckItem
                        = superScrollView.items[0].gameObject.GetComponent<SelectionToggle_DeckOnline>();
                    if (Cursor.lockState == CursorLockMode.Locked)
                        Runtime.Program.instance.onlineDeckViewer.Select();
                }
            };
        }

        private void ItemOnListRefresh(string[] task, GameObject item)
        {
            SelectionToggle_DeckOnline handler = item.GetComponent<SelectionToggle_DeckOnline>();
            handler.deckName = task[0];
            handler.deckAuthor = task[1];
            handler.deckId = task[2];
            handler.deckCase = int.Parse(task[3]);
            handler.card0 = int.Parse(task[4]);
            handler.card1 = int.Parse(task[5]);
            handler.card2 = int.Parse(task[6]);
            handler.protector = task[7];
            handler.like = int.Parse(task[8]);
            handler.lastDate = task[9];
            handler.Refresh();
        }
    }
}