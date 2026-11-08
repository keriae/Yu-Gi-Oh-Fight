using System.Collections.Generic;

using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI.ServantUI
{
    public class MateViewerUI : ServantUI
    {

        #region Elements

        private const string LABEL_SR = "ScrollRect";
        private ScrollRect m_ScrollRect;
        private ScrollRect ScrollRect =>
            m_ScrollRect = m_ScrollRect != null ? m_ScrollRect
            : Manager.GetElement<ScrollRect>(LABEL_SR);

        private const string LABEL_SBN_INTERACTION = "ButtonInteraction";
        private SelectionButton m_ButtonInteraction;
        private SelectionButton ButtonInteraction =>
            m_ButtonInteraction = m_ButtonInteraction != null ? m_ButtonInteraction
            : Manager.GetElement<SelectionButton>(LABEL_SBN_INTERACTION);

        private const string LABEL_IPT = "InputField";
        private TMP_InputField m_Input;
        private TMP_InputField Input =>
            m_Input = m_Input != null ? m_Input
            : Manager.GetElement<TMP_InputField>(LABEL_IPT);

        #endregion

        private static readonly List<int> crossDuelMates = new();
        private static readonly List<MDPro3.Duel.YGOSharp.Card> cards = new();
        public SuperScrollView superScrollView;

        private void Awake()
        {
#if UNITY_ANDROID || UNITY_STANDALONE_LINUX
            var files = Directory.GetFiles(Program.root + "CrossDuel", "*.bundle");
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file).Replace(".bundle", "");
                crossDuelMates.Add(int.Parse(fileName));
            }
#endif
            Load();
        }

        public void Load()
        {
            cards.Clear();
            for (int i = 0; i < crossDuelMates.Count; i++)
            {
                MDPro3.Duel.YGOSharp.Card card = MDPro3.Duel.YGOSharp.CardsManager.Get(crossDuelMates[i], true);
                if (card.Id == 0)
                {
                    card.Id = crossDuelMates[i];
                    card.Name = GetRushDuelMateName(crossDuelMates[i]);
                }
                cards.Add(card);
            }
            cards.Sort(MDPro3.Duel.YGOSharp.CardsManager.ComparisonOfCard());
            Print();
        }

        public void Print(string search = "")
        {
            superScrollView?.Clear();
            var tasks = new List<string[]>();
            foreach (MDPro3.Duel.YGOSharp.Card card in cards)
            {
                if (card.Name.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string[] task = new string[] { card.Id.ToString(), card.Name };
                    tasks.Add(task);
                }
            }
            foreach (Presentation.Assets.Items.Item mate in Runtime.Program.items.mates)
            {
                if (!string.IsNullOrEmpty(mate.name)
                    && mate.name.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0
                    && !mate.notReady)
                {
                    string[] task = new string[] { mate.id.ToString(), mate.name };
                    tasks.Add(task);
                }
            }
            var handle = Addressables.LoadAssetAsync<GameObject>("UI/ItemMate.prefab");
            handle.Completed += (result) =>
            {
                var itemWidth = PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 460f : 360f;
                var itemHeight = PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 80f : 40f;

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
                    Runtime.Program.instance.mate.lastSelectedMateItem
                    = superScrollView.items[0].gameObject.GetComponent<SelectionToggle_Mate>();
            };
        }

        private void ItemOnListRefresh(string[] task, GameObject item)
        {
            SelectionToggle_Mate handler = item.GetComponent<SelectionToggle_Mate>();
            handler.code = int.Parse(task[0]);
            handler.mateName = task[1];
            handler.Refresh();
        }

        public static string GetRushDuelMateName(int code)
        {
            return code switch
            {
                120105001 => MDPro3.InterString.Get("七星道魔术师"),
                120105010 => MDPro3.InterString.Get("落单使魔"),
                120110001 => MDPro3.InterString.Get("连击龙 齿车戒龙"),
                120110006 => MDPro3.InterString.Get("双刃龙"),
                120110010 => MDPro3.InterString.Get("掌上小龙"),
                120115001 => MDPro3.InterString.Get("七星道魔女"),
                120120018 => MDPro3.InterString.Get("耳语妖精"),
                120120025 => MDPro3.InterString.Get("龙队翻盘击球手"),
                120120024 => MDPro3.InterString.Get("龙队布局投球手"),
                120120029 => MDPro3.InterString.Get("魔将 雅灭鲁拉"),
                120120003 => MDPro3.InterString.Get("古之守护龟"),
                120130016 => MDPro3.InterString.Get("七星道法师"),
                120130026 => MDPro3.InterString.Get("斗将 难得斯"),
                120140023 => MDPro3.InterString.Get("王家魔族·骨肉皮"),
                120145014 => MDPro3.InterString.Get("火星心少女"),
                120150002 => MDPro3.InterString.Get("超魔机神 大霸道王"),
                120155019 => MDPro3.InterString.Get("祭神 莫多丽娜"),
                _ => string.Empty
            };
        }

        public void OnMateTap()
        {
            if (Runtime.Program.instance.mate.mate == null)
                return;
            Runtime.Program.instance.mate.mate.Play(Presentation.Mate.MateAction.Tap);
        }

        public void FocusOnInputField()
        {
            Input.ActivateInputField();
        }

        public void SelectButtonInteract()
        {
            ButtonInteraction.GetSelectable().Select();
        }

        public void SelectLastMateItem()
        {
            Runtime.Program.instance.mate.lastSelectedMateItem.GetSelectable().Select();
        }
    }
}
