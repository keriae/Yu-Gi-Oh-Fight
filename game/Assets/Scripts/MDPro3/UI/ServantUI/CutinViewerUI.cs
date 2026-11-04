using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using MDPro3.UI.PropertyOverride;

namespace MDPro3.UI.ServantUI
{
    public class CutinViewerUI : ServantUI
    {

        #region Elements

        private const string LABEL_SR = "ScrollRect";
        private ScrollRect m_ScrollRect;
        private ScrollRect ScrollRect =>
            m_ScrollRect = m_ScrollRect != null ? m_ScrollRect
            : Manager.GetElement<ScrollRect>(LABEL_SR);

        private const string LABEL_IPT = "InputField";
        private TMP_InputField m_Input;
        private TMP_InputField Input =>
            m_Input = m_Input != null ? m_Input
            : Manager.GetElement<TMP_InputField>(LABEL_IPT);

        private const string LABEL_SBN_AUTOPLAY = "ButtonAutoPlay";
        private SelectionButton m_ButtonAutoPlay;
        public SelectionButton ButtonAutoPlay =>
            m_ButtonAutoPlay = m_ButtonAutoPlay != null ? m_ButtonAutoPlay
            : Manager.GetElement<SelectionButton>(LABEL_SBN_AUTOPLAY);

        #endregion

        public SuperScrollView superScrollView;

        private void Awake()
        {
            Print();
        }

        public void Print(string search = "")
        {
            superScrollView?.Clear();
            var tasks = new List<string[]>();
            foreach (var card in CutinViewer.cards)
            {
                if (card.Name.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0 || card.Id.ToString() == search)
                {
                    string code = card.Id.ToString();
                    string cardName = card.Name;
                    string[] task = new string[] { code, cardName };
                    tasks.Add(task);
                }
            }
            var handle = Addressables.LoadAssetAsync<GameObject>("UI/ItemCutin.prefab");
            handle.Completed += (result) =>
            {
                var itemWidth = PropertyOverrider.NeedMobileLayout() ? 460f : 360f;
                var itemHeight = PropertyOverrider.NeedMobileLayout() ? 80f : 40f;

                superScrollView = new SuperScrollView(
                    1,
                    itemWidth,
                    itemHeight,
                    0,
                    0,
                    result.Result,
                    ItemOnListRefresh,
                    ScrollRect, 4);
                superScrollView.Print(tasks);
                if (superScrollView.items.Count > 0)
                {
                    Program.instance.cutin.lastSelectedCutinItem = superScrollView.items[0].gameObject.GetComponent<SelectionToggle_Cutin>();
                }
            };
        }

        private void ItemOnListRefresh(string[] task, GameObject item)
        {
            var handler = item.GetComponent<SelectionToggle_Cutin>();
            handler.code = int.Parse(task[0]);
            handler.cardName = task[1];
            handler.Refresh();
        }

        public void FocusOnInputField()
        {
            Input.ActivateInputField();
        }

        public void OnAutoPlay()
        {
            Program.instance.cutin.AutoPlay();
        }

        public void SelectLastCutinItem()
        {
            UserInput.NextSelectionIsAxis = true;
            Program.instance.cutin.lastSelectedCutinItem.GetSelectable().Select();
        }

    }
}
