using TMPro;
using UnityEngine;
using UnityEngine.UI;

using static YuGiOhFight.Client.Controllers.DeckSelector;

using System.Collections.Generic;

using DG.Tweening;

using System.Text;
using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.IO;
using System.Linq;


namespace YuGiOhFight.Client.UI.ServantUI
{
    public class DeckSelectorUI : ServantUI
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

        private const string LABEL_TXT_DECKNUMVALUE = "TextDeckNumValue";
        private TextMeshProUGUI m_TextDeckNumValue;
        private TextMeshProUGUI TextDeckNumValue =>
            m_TextDeckNumValue = m_TextDeckNumValue != null ? m_TextDeckNumValue
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_DECKNUMVALUE);

        private const string LABEL_STG_PICKUPCARD = "TogglePickupCard";
        private SelectionToggle m_TogglePickupCard;
        public SelectionToggle TogglePickupCard =>
            m_TogglePickupCard = m_TogglePickupCard != null ? m_TogglePickupCard
            : Manager.GetElement<SelectionToggle>(LABEL_STG_PICKUPCARD);

        private const string LABEL_SBN_ONLINE = "ButtonOnline";
        private SelectionButton m_ButtonOnline;
        public SelectionButton ButtonOnline =>
            m_ButtonOnline = m_ButtonOnline != null ? m_ButtonOnline
            : Manager.GetElement<SelectionButton>(LABEL_SBN_ONLINE);

        private const string LABEL_RT_HEADER = "Header";
        private RectTransform m_Header;
        private RectTransform Header =>
            m_Header = m_Header != null ? m_Header
            : Manager.GetElement<RectTransform>(LABEL_RT_HEADER);

        private const string LABEL_RT_FOOTER = "Footer";
        private RectTransform m_Footer;
        private RectTransform Footer =>
            m_Footer = m_Footer != null ? m_Footer
            : Manager.GetElement<RectTransform>(LABEL_RT_FOOTER);

        private const string LABEL_SBN_TYPE = "ButtonType";
        private SelectionButton m_ButtonType;
        public SelectionButton ButtonType =>
            m_ButtonType = m_ButtonType != null ? m_ButtonType
            : Manager.GetElement<SelectionButton>(LABEL_SBN_TYPE);

        private const string LABEL_SBN_DELETE = "ButtonDelete";
        private SelectionButton m_ButtonDelete;
        private SelectionButton ButtonDelete =>
            m_ButtonDelete = m_ButtonDelete != null ? m_ButtonDelete
            : Manager.GetElement<SelectionButton>(LABEL_SBN_DELETE);

        private const string LABEL_SBN_CANCEL = "ButtonCancel";
        private SelectionButton m_ButtonCancel;
        private SelectionButton ButtonCancel =>
            m_ButtonCancel = m_ButtonCancel != null ? m_ButtonCancel
            : Manager.GetElement<SelectionButton>(LABEL_SBN_CANCEL);

        private const string LABEL_SBN_CONFIRM = "ButtonConfirm";
        private SelectionButton m_ButtonConfirm;
        private SelectionButton ButtonConfirm =>
            m_ButtonConfirm = m_ButtonConfirm != null ? m_ButtonConfirm
            : Manager.GetElement<SelectionButton>(LABEL_SBN_CONFIRM);

        #endregion

        public SuperScrollView superScrollView;
        public string deckType = string.Empty;
        private string selectedType;
        public Dictionary<string, MDPro3.Duel.YGOSharp.Deck> decks = new();
        public bool buttonLayoutSwitching;
        public static string deckInUse;
        public MDPro3.Utility.DeckLibraryPage LibraryPage { get; private set; } = MDPro3.Utility.DeckLibraryPage.Mine;
        private List<MDPro3.Utility.DeckLibraryEntry> recommendations;
        private readonly Dictionary<string, MDPro3.Utility.DeckLibraryEntry> recommendedDecks = new();
        private readonly Dictionary<MDPro3.Utility.DeckLibraryPage, SelectionButton> pageButtons = new();
        private RectTransform pageTabs;
        private int printVersion;
        private AsyncOperationHandle<GameObject> deckPrefabHandle;
        private MDPro3.Utility.DeckCoverArtCache coverCache = new();

        protected override void OnDestroy()
        {
            ++printVersion;
            coverCache?.Dispose();
            if (superScrollView != null && m_ScrollRect != null && m_ScrollRect.verticalScrollbar != null)
                m_ScrollRect.verticalScrollbar.onValueChanged.RemoveListener(superScrollView.OnScrollBarChange);
            if (deckPrefabHandle.IsValid()) Addressables.Release(deckPrefabHandle);
            base.OnDestroy();
        }
        private string pendingSelection;
        private TextMeshProUGUI emptyResults;
        private bool IsRecommendationPage => condition == Condition.ForEdit && LibraryPage != MDPro3.Utility.DeckLibraryPage.Mine;

        private void EnsurePageTabs()
        {
            if (pageTabs != null) return;
            var tabs = new GameObject("DeckLibraryTabs", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            tabs.transform.SetParent(ScrollRect.transform, false);
            pageTabs = tabs.GetComponent<RectTransform>();
            pageTabs.anchorMin = new Vector2(0f, 1f);
            pageTabs.anchorMax = Vector2.one;
            pageTabs.pivot = new Vector2(0.5f, 1f);
            pageTabs.offsetMin = new Vector2(26f, -64f);
            pageTabs.offsetMax = new Vector2(-42f, -8f);
            tabs.GetComponent<Image>().color = new Color(0.012f, 0.025f, 0.05f, 0.98f);
            var group = tabs.GetComponent<HorizontalLayoutGroup>();
            group.spacing = 12f;
            group.padding = new RectOffset(8, 8, 4, 4);
            group.childControlHeight = group.childControlWidth = true;
            group.childForceExpandHeight = group.childForceExpandWidth = true;
            var labels = new[] { "我的", "推荐·竞技", "推荐·经典" };
            for (var index = 0; index < labels.Length; index++)
            {
                MDPro3.Utility.DeckLibraryPage page = (MDPro3.Utility.DeckLibraryPage)index;
                SelectionButton button = Popup.PopupDeckAcademy.CreateButton(ButtonType, pageTabs, labels[index],
                    () => SwitchLibraryPage(page), 48f, compact: true);
                button.name = "DeckPage" + page;
                foreach (var label in button.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    label.fontSize = label.fontSizeMax = 26f;
                    label.fontSizeMin = 20f;
                }
                foreach (var graphic in button.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
                var image = button.GetComponent<Image>() ?? button.gameObject.AddComponent<Image>();
                image.raycastTarget = true;
                button.GetSelectable().targetGraphic = image;
                pageButtons.Add(page, button);
            }
            emptyResults = Instantiate(TextDeckNumValue, ScrollRect.transform, false);
            emptyResults.name = "NoDeckResults";
            emptyResults.text = "没有匹配的卡组";
            emptyResults.fontSize = 26f;
            emptyResults.alignment = TextAlignmentOptions.Center;
            emptyResults.raycastTarget = false;
            var emptyRect = emptyResults.rectTransform;
            emptyRect.anchorMin = new Vector2(0f, 0.5f);
            emptyRect.anchorMax = new Vector2(1f, 0.5f);
            emptyRect.anchoredPosition = Vector2.zero;
            emptyRect.sizeDelta = new Vector2(0f, 54f);
            emptyResults.gameObject.SetActive(false);
            Input.onValueChanged.AddListener(Print);
        }

        public void SwitchLibraryPage(MDPro3.Utility.DeckLibraryPage page)
        {
            if (condition != Condition.ForEdit || buttonLayoutSwitching) return;
            if (LibraryPage == page) return;
            LibraryPage = page;
            Input.SetTextWithoutNotify(string.Empty);
            RefreshList();
        }

        public override void ShowEvent()
        {
            base.ShowEvent();
            EnsurePageTabs();
            if (condition != Condition.ForEdit) LibraryPage = MDPro3.Utility.DeckLibraryPage.Mine;
            pageTabs.gameObject.SetActive(condition == Condition.ForEdit);

            switch (condition)
            {
                case Condition.ForEdit:
                    ButtonOnline.gameObject.SetActive(false);
                    Title.text = MDPro3.InterString.Get("编辑卡组");
                    break;
                case Condition.ForDuel:
                    ButtonOnline.gameObject.SetActive(false);
                    Title.text = MDPro3.InterString.Get("选择卡组");
                    break;
                case Condition.ForSolo:
                    ButtonOnline.gameObject.SetActive(false);
                    Title.text = MDPro3.InterString.Get("选择卡组");
                    break;
                case Condition.MyCard:
                    ButtonOnline.gameObject.SetActive(false);
                    Title.text = MDPro3.InterString.Get("选择卡组");
                    break;
            }

            ShowDefaultButtons();
        }

        public override void AfterShowEvent()
        {
            base.AfterShowEvent();
            RefreshList();
        }

        protected override void AfterHideEvent()
        {
            base.AfterHideEvent();

            MDPro3.Config.Save();
            TogglePickupCard.SetToggleOff();
            ++printVersion;
            ClearList();
            coverCache?.Dispose();
            coverCache = null;
        }

        private void ClearList()
        {
            if (Runtime.Program.instance != null && Runtime.Program.instance.deckSelector != null)
                Runtime.Program.instance.deckSelector.lastSelectedDeckItem = null;
            if (superScrollView == null) return;
            ScrollRect.verticalScrollbar.onValueChanged.RemoveListener(superScrollView.OnScrollBarChange);
            superScrollView.Clear();
            superScrollView = null;
        }

        public void RefreshList()
        {
            EnsurePageTabs();
            decks.Clear();
            recommendedDecks.Clear();
            try { recommendations ??= MDPro3.Utility.DeckLibrary.Load(); }
            catch (Exception error) { Runtime.MessageManager.Cast("推荐卡组加载失败：" + error.Message); }
            ShowDefaultButtons();
            TogglePickupCard.SetToggleOff();
            ButtonType.SetButtonText(GetTypeName());

            if (!Directory.Exists(Runtime.Program.PATH_DECK))
                Directory.CreateDirectory(Runtime.Program.PATH_DECK);
            if (IsRecommendationPage)
            {
                foreach (MDPro3.Utility.DeckLibraryEntry entry in recommendations ?? new List<MDPro3.Utility.DeckLibraryEntry>())
                {
                    if (entry.Page != LibraryPage) continue;
                    recommendedDecks.Add(entry.Id, entry);
                    decks.Add(entry.Id, new MDPro3.Duel.YGOSharp.Deck(entry.SourcePath));
                }
                Print(Input.text);
                return;
            }
            var path = GetCurrentTypePath();
            if (!Directory.Exists(path)) { deckType = string.Empty; path = Runtime.Program.PATH_DECK; }
            var files = Directory.GetFiles(path, "*.ydk");
            var fileList = files.ToList();
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                fileName = fileName[..^4];
                if (fileName == (pendingSelection ?? MDPro3.Config.GetConfigDeckName(false)))
                {
                    fileList.Remove(file);
                    fileList.Insert(0, file);
                    break;
                }
            }
            foreach (var deck in fileList)
            {
                if (condition == Condition.ForEdit && deckType == string.Empty
                    && recommendations != null && MDPro3.Utility.DeckLibrary.IsBundledTemplate(deck)) continue;
                var name = Path.GetFileName(deck);
                name = name[..^4];
                decks.Add(name, new MDPro3.Duel.YGOSharp.Deck(deck));
            }

            Print(Input.text);
        }

        public void ActivateInputField()
        {
            Input.ActivateInputField();
        }

        public void Print(string search = "")
        {
            SwitchToDefaultLayout();
            ClearList();
            var version = ++printVersion;

            if (!deckPrefabHandle.IsValid()) deckPrefabHandle = Addressables.LoadAssetAsync<GameObject>("UI/ItemDeck.prefab");
            var handle = deckPrefabHandle;
            handle.Completed += (result) =>
            {
                if (this == null || version != printVersion || result.Result == null) return;
                var itemWidth = PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 336f : 260f;
                var itemHeight = PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 300f : 232f;
                var space = PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 30f : 24f;
                var bottomPadding = (PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 196f : 150f) - space;
                superScrollView = new SuperScrollView(
                    -1,
                    itemWidth + space,
                    itemHeight + space,
                    condition == Condition.ForEdit ? 78f : 10f,
                    bottomPadding,
                    result.Result,
                    ItemOnListRefresh,
                    Manager.GetElement<ScrollRect>("ScrollRect"));
                List<string[]> tasks = new();
                if (!IsRecommendationPage) tasks.Add(new string[11] { string.Empty, "0", "0", "0", "0", "0", "0", "", "0", "0", "" });
                foreach (var deck in decks)
                {
                    recommendedDecks.TryGetValue(deck.Key, out MDPro3.Utility.DeckLibraryEntry entry);
                    if (entry != null ? !MDPro3.Utility.DeckLibrary.Matches(entry, search)
                        : deck.Key.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    var task = new string[11]
                    {
                        entry?.Name ?? deck.Key,
                        deck.Value.Case.ToString(),
                        "0", "0", "0",
                        deck.Value.Protector.ToString(),
                        "0",//For Delete
                        deck.Value.deckId,
                        (entry?.CoverCard ?? MDPro3.Utility.DeckLibrary.CoverFor(deck.Value)).ToString(),
                        entry == null ? "0" : "1",
                        entry?.Id ?? deck.Key
                    };
                    if (deck.Value.Pickup.Count > 0)
                        task[2] = deck.Value.Pickup[0].ToString();
                    if (deck.Value.Pickup.Count > 1)
                        task[3] = deck.Value.Pickup[1].ToString();
                    if (deck.Value.Pickup.Count > 2)
                        task[4] = deck.Value.Pickup[2].ToString();
                    tasks.Add(task);
                }
                superScrollView.Print(tasks);
                pageTabs.SetAsLastSibling();
                emptyResults.gameObject.SetActive(tasks.All(task => string.IsNullOrEmpty(task[0])));
                if (tasks.Count > 0)
                {
                    var selectIndex = pendingSelection == null ? 0 : tasks.FindIndex(task => task[10] == pendingSelection);
                    if (selectIndex < 0) selectIndex = 0;
                    pendingSelection = null;
                    Runtime.Program.instance.deckSelector.lastSelectedDeckItem = (SelectionToggle_Deck)superScrollView.GetItemByIndex(selectIndex);
                }
                if (Cursor.lockState == CursorLockMode.Locked)
                    Runtime.Program.instance.deckSelector.Select();
                UpdateDeckNum();
            };
        }

        private void ItemOnListRefresh(string[] task, GameObject item)
        {
            SelectionToggle_Deck handler = item.GetComponent<SelectionToggle_Deck>();
            handler.coverCache = coverCache ??= new MDPro3.Utility.DeckCoverArtCache();
            handler.deckName = task[0];
            handler.deckCase = int.Parse(task[1]);
            handler.card0 = int.Parse(task[2]);
            handler.card1 = int.Parse(task[3]);
            handler.card2 = int.Parse(task[4]);
            handler.protector = task[5];
            handler.isOn = task[6] != "0";
            handler.coverCard = int.Parse(task[8]);
            handler.isReadOnly = task[9] == "1";
            handler.sourceKey = task[10];
            handler.isCreateButton = string.IsNullOrEmpty(task[0]);
            handler.Refresh();
        }

        public void OnRecommendationSelected(string sourceKey)
        {
            if (!IsRecommendationPage || !recommendedDecks.TryGetValue(sourceKey, out MDPro3.Utility.DeckLibraryEntry entry)) return;
            Controllers.DeckEditor.Recommendation = entry;
            Runtime.Program.instance.deckEditor.SwitchCondition(Controllers.DeckEditor.Condition.RecommendationDeck, entry.Name, new MDPro3.Duel.YGOSharp.Deck(entry.SourcePath));
            Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.deckEditor);
        }

        public string CopyRecommendationToMine(string sourceKey)
        {
            MDPro3.Utility.DeckLibraryEntry entry = recommendations?.FirstOrDefault(value => value.Id == sourceKey);
            if (entry == null || condition != Condition.ForEdit) return null;
            try
            {
                var name = MDPro3.Utility.DeckLibrary.CreateEditableCopy(entry);
                LibraryPage = MDPro3.Utility.DeckLibraryPage.Mine;
                deckType = string.Empty;
                pendingSelection = name;
                Input.SetTextWithoutNotify(string.Empty);
                RefreshList();
                Runtime.MessageManager.Toast("已复制到我的：" + name);
                return name;
            }
            catch (Exception error) { Runtime.MessageManager.Cast("复制卡组失败：" + error.Message); return null; }
        }

        public void PrepareMineOnReturn()
        {
            LibraryPage = MDPro3.Utility.DeckLibraryPage.Mine;
            deckType = string.Empty;
            Input.SetTextWithoutNotify(string.Empty);
        }

        public bool PickupShowing
        {
            get { return m_pickupShowing; }
            set
            {
                m_pickupShowing = value;
                DeckHover();
            }
        }
        private bool m_pickupShowing = false;
        public void DeckHover()
        {
            if (superScrollView == null)
                return;

            foreach (SuperScrollView.Item item in superScrollView.items)
            {
                if (item.gameObject == null)
                    continue;
                SelectionToggle_Deck handler = item.gameObject.GetComponent<SelectionToggle_Deck>();

                if (PickupShowing)
                    handler.ShowPickup(true);
                else
                    handler.HidePickup(true);
            }
        }

        public void DeckCreate()
        {
            if (IsRecommendationPage) return;
            SwitchToDefaultLayout();
            var selections = new List<string>()
            {
                MDPro3.InterString.Get("请输入卡组名。@n创建卡组时会自动导入剪切板中的卡组码。"),
                string.Empty
            };
            Runtime.UIManager.ShowPopupInput(selections, DeckCheck, null, TmpInputValidation.ValidationType.Path);
        }

        private void DeckCheck(string deckName)
        {
            var path = GetDeckPath(deckName);

            if (File.Exists(path))
            {
                deckInUse = deckName;
                List<string> tasks = new()
                {
                    MDPro3.InterString.Get("该卡组名已存在"),
                    MDPro3.InterString.Get("该卡组名的文件已存在，是否直接覆盖创建？"),
                    MDPro3.InterString.Get("覆盖"),
                    MDPro3.InterString.Get("取消")
                };
                DOTween.To(v => { }, 0, 0, 0.6f).OnComplete(() =>
                {
                    Runtime.UIManager.ShowPopupYesOrNo(tasks, DeckFileCreateWithName, null);
                });
            }
            else
                DeckFileCreate(deckName);
        }

        private void DeckFileCreateWithName()
        {
            DeckFileCreate(deckInUse);
        }

        private void DeckFileCreate(string deckName)
        {
            try
            {
                var path = GetDeckPath(deckName);
                File.Create(path).Close();

                string clipBoard = GUIUtility.systemCopyBuffer;
                if (clipBoard.Contains("#main"))
                    File.WriteAllText(path!, clipBoard, Encoding.UTF8);
                else if (clipBoard.Contains("ygotype=deck&v=1&d="))
                {
                    var uri = new Uri(clipBoard);
                    MDPro3.Duel.YGOSharp.Deck deck = MDPro3.Net.DeckShareURL.UriToDeck(uri);
                    deck.type = deckType;
                    deck.Save(deckName, DateTime.UtcNow);
                }
                else if (clipBoard.Contains(MDPro3.YdkeConverter.ydkeHeader))
                {
                    MDPro3.Duel.YGOSharp.Deck deck = MDPro3.YdkeConverter.Ydke2Deck(clipBoard);
                    deck.type = deckType;
                    deck.Save(deckName, DateTime.UtcNow);
                }
                MDPro3.Config.SetConfigDeck(deckName, true);
                RefreshList();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("创建卡组失败！请检查文件夹权限。"));
            }
        }

        private void DeleteOnlineDecks(List<string> ids)
        {
            if (Services.MyCard.account == null || !MDPro3.Config.GetBool("SyncDeck", true))
                return;
            _ = MDPro3.Net.OnlineDeck.DeleteDecks(ids);
        }

        public void OnOnlineDeckView()
        {
            if (condition == Condition.ForEdit)
                Runtime.UIManager.ShowPopupDeckAcademy();
        }

        public void OnShowPickup()
        {
            PickupShowing = true;
        }

        public void OnHidePickup()
        {
            PickupShowing = false;
        }

        #region Deck Type

        public void OnType()
        {
            if (IsRecommendationPage) return;
            var folders = GetAllDeckTypes();
            var types = new List<string>
            {
                MDPro3.InterString.Get("卡组分组"),
                MDPro3.InterString.Get("彩色选项为功能选项，白色选项为卡组分组。"),
                "g:" + MDPro3.InterString.Get("新建分组"),
                "r:" + MDPro3.InterString.Get("删除分组"),
                "b:" + MDPro3.InterString.Get("重命名分组"),
                "b:" + MDPro3.InterString.Get("移动到分组"),
                "b:" + MDPro3.InterString.Get("复制到分组"),
                MDPro3.InterString.Get("默认分组")
            };
            types.AddRange(folders);

            Runtime.UIManager.ShowPopupSelection(types, OnTypeChange, null);
        }

        private void OnTypeChange()
        {
            SelectionButton button = UnityEngine.EventSystems.EventSystem.current.
                currentSelectedGameObject.GetComponent<SelectionButton>();
            var color = button.GetButtonTextColor();
            string selected = button.GetButtonText();

            if(color != Color.white)
            {
                if (selected == MDPro3.InterString.Get("新建分组"))
                    AddDeckType();
                if (selected == MDPro3.InterString.Get("删除分组"))
                    RemoveDeckType();
                if (selected == MDPro3.InterString.Get("重命名分组"))
                    RenameDeckType();
                if (selected == MDPro3.InterString.Get("移动到分组"))
                    MoveToType();
                if (selected == MDPro3.InterString.Get("复制到分组"))
                    CopyToType();
                return;
            }

            var type = selected;
            if(selected == MDPro3.InterString.Get("默认分组"))
                type = string.Empty;
            deckType = type;
            RefreshList();
        }

        private void AddDeckType()
        {
            var selections = new List<string>()
                {
                    MDPro3.InterString.Get("添加新卡组分组"),
                    string.Empty
                };
            Runtime.UIManager.ShowPopupInput(selections, AddDeckType, null, TmpInputValidation.ValidationType.Path);
        }

        private void AddDeckType(string type)
        {
            if(type == string.Empty)
                return;

            var path = Runtime.Program.PATH_DECK + type + "/";
            if (Directory.Exists(path))
            {
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("该分组已存在！"));
                return;
            }
            Directory.CreateDirectory(path);
            deckType = type;
            RefreshList();
        }

        private void RemoveDeckType()
        {
            var folders = GetAllDeckTypes();
            var types = new List<string>
            {
                MDPro3.InterString.Get("删除分组"),
                string.Empty,
            };
            types.AddRange(folders);
            Runtime.UIManager.ShowPopupSelection(types, DeleteDeckType, null);
        }

        private void DeleteDeckType()
        {
            var type = UnityEngine.EventSystems.EventSystem.current.
                currentSelectedGameObject.GetComponent<SelectionButton>().GetButtonText();
            var path = Runtime.Program.PATH_DECK + type + "/";
            if (!Directory.Exists(path))
                return;
            var files = Directory.GetFiles(path, "*.ydk");
            if(files.Length > 0)
            {
                var selections = new List<string>
                {
                    MDPro3.InterString.Get("删除分组"),
                    MDPro3.InterString.Get("该卡组分类包含[?]个卡组，是否确认删除？", files.Length.ToString()),
                    MDPro3.InterString.Get("确认"),
                    MDPro3.InterString.Get("取消")
                };
                Runtime.UIManager.ShowPopupYesOrNo(selections, () => DeleteDeckType(type), null);
            }
            else
            {
                DeleteDeckType(type);
            }
        }

        private void DeleteDeckType(string type)
        {
            var path = Runtime.Program.PATH_DECK + type + "/";
            DeleteOnlineDecks(GetAllDeckIds(path));
            var files = Directory.GetFiles(path, "*.ydk");
            foreach (var file in files)
                File.Delete(file);
            Directory.Delete(path);
            if (deckType == type)
                deckType = string.Empty;
            RefreshList();
        }

        private List<string> GetAllDeckIds(string dir)
        {
            var value = new List<string>();
            var files = Directory.GetFiles(dir, "*.ydk");
            foreach (var file in files)
            {
                MDPro3.Duel.YGOSharp.Deck deck = new MDPro3.Duel.YGOSharp.Deck(file);
                value.Add(deck.deckId);
                Debug.Log("--: " + deck.deckId);
            }

            return value;
        }

        private void RenameDeckType()
        {
            var folders = GetAllDeckTypes();
            var types = new List<string>
            {
                MDPro3.InterString.Get("重命名分组"),
                string.Empty,
            };
            types.AddRange(folders);
            Runtime.UIManager.ShowPopupSelection(types, RenameType, null);
        }

        private void RenameType()
        {
            selectedType = UnityEngine.EventSystems.EventSystem.current.
                currentSelectedGameObject.GetComponent<SelectionButton>().GetButtonText();
            var selections = new List<string>()
                {
                    MDPro3.InterString.Get("重命名分组"),
                    selectedType
                };
            Runtime.UIManager.ShowPopupInput(selections, CheckRenamedDeckType, null, TmpInputValidation.ValidationType.Path);
        }

        private void CheckRenamedDeckType(string newType)
        {
            if (newType == selectedType)
                return;
            var allTypes = GetAllDeckTypes();
            bool exists = allTypes.Any(type=> type != selectedType && type == newType);
            if (exists)
            {
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("该分组已存在！"));
                return;
            }

            var path = Runtime.Program.PATH_DECK + selectedType;
            var newPath = Runtime.Program.PATH_DECK + newType;
            Directory.Move(path, newPath);
            var files = Directory.GetFiles(newPath, "*.ydk");

            foreach (var file in files)
            {
                MDPro3.Duel.YGOSharp.Deck deck = new MDPro3.Duel.YGOSharp.Deck(file)
                {
                    type = newType
                };
                deck.Save(Path.GetFileNameWithoutExtension(file), DateTime.UtcNow, true, false);
            }

            deckType = newType;
            RefreshList();
        }

        private void MoveToType()
        {
            var folders = GetAllDeckTypes(deckType);
            var types = new List<string>
            {
                MDPro3.InterString.Get("移动到分组"),
                string.Empty
            };
            if (deckType != string.Empty)
                types.Add(MDPro3.InterString.Get("默认分组"));
            types.AddRange(folders);
            Runtime.UIManager.ShowPopupSelection(types, OnMoveToType, null);
        }

        private void CopyToType()
        {
            var folders = GetAllDeckTypes(deckType);
            var types = new List<string>
            {
                MDPro3.InterString.Get("复制到分组"),
                string.Empty
            };
            if (deckType != string.Empty)
                types.Add(MDPro3.InterString.Get("默认分组"));
            types.AddRange(folders);
            Runtime.UIManager.ShowPopupSelection(types, OnCopyToType, null);
        }

        private string GetCurrentTypePath()
        {
            if (deckType == string.Empty)
                return Runtime.Program.PATH_DECK;
            else
                return Runtime.Program.PATH_DECK + deckType + "/";
        }

        private string GetTypeName(string type)
        {
            if(type == string.Empty)
                return MDPro3.InterString.Get("默认分组");
            else
                return type;
        }

        private string GetTypeName()
        {
            return GetTypeName(deckType);
        }

        private string GetTypePath(string type)
        {
            if (type == string.Empty)
                return Runtime.Program.PATH_DECK;
            else
                return Runtime.Program.PATH_DECK + $"{type}/";
        }

        private string GetDeckPath(string deckName, string type)
        {
            return GetTypePath(type) + deckName + Runtime.Program.EXPANSION_YDK;
        }

        private string GetDeckPath(string deckName)
        {
            return GetDeckPath(deckName, deckType);
        }

        private string[] GetAllDeckTypes(string excludeType = null)
        {
            var folders = Directory.GetDirectories(Runtime.Program.PATH_DECK);
            var types = folders.Select(f => Path.GetFileName(f)).ToArray();
            if(excludeType != null)
                types = types.Where(t => t != excludeType).ToArray();
            return types;
        }

        #endregion

        #region Delete Deck

        public void OnDelete()
        {
            if (IsRecommendationPage) return;
            if (buttonLayoutSwitching) return;
            SwitchButtonLayouts(LayoutType.Delete);
        }

        private void OnDeleteConfirm()
        {
            if (buttonLayoutSwitching) return;

            var deleteIndexs = new List<int>();
            var deleteIds = new List<string>();
            for (int i = 0; i < superScrollView.items.Count; i++)
                if (superScrollView.items[i].args[6] != "0")
                {
                    File.Delete(GetDeckPath(superScrollView.items[i].args[0]));
                    deleteIndexs.Add(i);
                    deleteIds.Add(superScrollView.items[i].args[7]);
                }
            var lastSelect = Runtime.Program.instance.deckSelector.lastSelectedDeckItem.index;
            int removedCount = 0;
            for (int i = 0; i < deleteIndexs.Count; i++)
            {
                superScrollView.RemoveAt(deleteIndexs[i] - removedCount);
                removedCount++;
            }
            Runtime.Program.instance.deckSelector.lastSelectedDeckItem = (SelectionToggle_Deck)superScrollView.GetItemByIndex(lastSelect);
            if (Cursor.lockState == CursorLockMode.Locked)
                Runtime.Program.instance.deckSelector.Select();
            DeleteOnlineDecks(deleteIds);

            SwitchToDefaultLayout();
            UpdateDeckNum();
        }

        #endregion

        #region Move Deck to Type

        private void OnMoveToType()
        {
            if (buttonLayoutSwitching) return;
            selectedType = UnityEngine.EventSystems.EventSystem.current.
                currentSelectedGameObject.GetComponent<SelectionButton>().GetButtonText();
            if(selectedType == MDPro3.InterString.Get("默认分组"))
                selectedType = string.Empty;
            SwitchButtonLayouts(LayoutType.MoveToType);
        }

        private void OnMoveToTypeConfirm()
        {
            if (buttonLayoutSwitching) return;

            var moveIndexs = new List<int>();
            for (int i = 0; i < superScrollView.items.Count; i++)
                if (superScrollView.items[i].args[6] != "0")
                {
                    var filePath = GetDeckPath(superScrollView.items[i].args[0]);
                    var newPath = GetDeckPath(superScrollView.items[i].args[0], selectedType);
                    if(File.Exists(newPath))
                    {
                        Runtime.MessageManager.Cast(MDPro3.InterString.Get("操作失败，目标分组已存在同名卡组：[[?]]。", superScrollView.items[i].args[0]));
                        continue;
                    }

                    MDPro3.Duel.YGOSharp.Deck deck = new MDPro3.Duel.YGOSharp.Deck(filePath)
                    {
                        type = selectedType
                    };
                    deck.Save(superScrollView.items[i].args[0], DateTime.UtcNow, true, false);
                    File.Delete(filePath);
                    moveIndexs.Add(i);
                }
            var lastSelect = Runtime.Program.instance.deckSelector.lastSelectedDeckItem.index;
            int removedCount = 0;
            for (int i = 0; i < moveIndexs.Count; i++)
            {
                superScrollView.RemoveAt(moveIndexs[i] - removedCount);
                removedCount++;
            }
            Runtime.Program.instance.deckSelector.lastSelectedDeckItem = (SelectionToggle_Deck)superScrollView.GetItemByIndex(lastSelect);
            if (Cursor.lockState == CursorLockMode.Locked)
                Runtime.Program.instance.deckSelector.Select();

            SwitchToDefaultLayout();
            UpdateDeckNum();
        }

        #endregion

        #region Copy Deck to Type

        private void OnCopyToType()
        {
            if (buttonLayoutSwitching) return;
            selectedType = UnityEngine.EventSystems.EventSystem.current.
                currentSelectedGameObject.GetComponent<SelectionButton>().GetButtonText();
            if (selectedType == MDPro3.InterString.Get("默认分组"))
                selectedType = string.Empty;
            SwitchButtonLayouts(LayoutType.CopyToType);
        }

        private void OnCopyToTypeConfirm()
        {
            if (buttonLayoutSwitching) return;

            for (int i = 0; i < superScrollView.items.Count; i++)
                if (superScrollView.items[i].args[6] != "0")
                {
                    var filePath = GetDeckPath(superScrollView.items[i].args[0]);
                    var newPath = GetDeckPath(superScrollView.items[i].args[0], selectedType);
                    if (File.Exists(newPath))
                    {
                        Runtime.MessageManager.Cast(MDPro3.InterString.Get("操作失败，目标分组已存在同名卡组：[[?]]。", superScrollView.items[i].args[0]));
                        continue;
                    }

                    MDPro3.Duel.YGOSharp.Deck deck = new MDPro3.Duel.YGOSharp.Deck(filePath)
                    {
                        deckId = string.Empty,
                        type = selectedType
                    };
                    deck.Save(superScrollView.items[i].args[0], DateTime.UtcNow, true, false);
                }
            if (Cursor.lockState == CursorLockMode.Locked)
                Runtime.Program.instance.deckSelector.Select();
            SwitchToDefaultLayout();
        }

        #endregion

        #region Switch Layout

        private enum LayoutType
        {
            Default,
            Delete,
            MoveToType,
            CopyToType
        }

        private LayoutType layoutType;

        private void ShowDefaultButtons()
        {
            ButtonDelete.gameObject.SetActive(!IsRecommendationPage);
            ButtonCancel.gameObject.SetActive(false);
            ButtonOnline.SetButtonText("卡组学院 / 教程");
            ButtonOnline.gameObject.SetActive(condition == Condition.ForEdit);
            ButtonConfirm.gameObject.SetActive(false);
            Input.gameObject.SetActive(true);
            ButtonType.gameObject.SetActive(!IsRecommendationPage);
            if (pageTabs != null)
            {
                pageTabs.gameObject.SetActive(condition == Condition.ForEdit);
                foreach (var pair in pageButtons) SelectionEdgeHighlight.SetSelected(pair.Value, pair.Key == LibraryPage);
            }
        }

        private void ShowDeleteButtons()
        {
            ButtonDelete.gameObject.SetActive(false);
            ButtonCancel.gameObject.SetActive(true);
            ButtonOnline.gameObject.SetActive(false);
            ButtonConfirm.gameObject.SetActive(true);
            Input.gameObject.SetActive(false);
            ButtonType.gameObject.SetActive(false);

            ButtonCancel.ShowIcon(true);
            ButtonConfirm.SetButtonText(MDPro3.InterString.Get("确认删除"));
        }

        private void ShowMoveOrCopyButtons()
        {
            ButtonDelete.gameObject.SetActive(false);
            ButtonCancel.gameObject.SetActive(true);
            ButtonOnline.gameObject.SetActive(false);
            ButtonConfirm.gameObject.SetActive(true);
            Input.gameObject.SetActive(false);
            ButtonType.gameObject.SetActive(false);

            ButtonCancel.ShowIcon(false);
            ButtonConfirm.SetButtonText(MDPro3.InterString.Get("确认"));
        }

        private void UpdateDeckNum()
        {
            TextDeckNumValue.text = superScrollView == null ? decks.Count.ToString()
                : superScrollView.items.Count(item => !string.IsNullOrEmpty(item.args[0])).ToString();
        }

        private void SwitchToDefaultLayout()
        {
            HideAllToggles();
            SwitchButtonLayouts(LayoutType.Default);
        }

        private void SwitchButtonLayouts(LayoutType layoutType)
        {
            if (this.layoutType == layoutType)
                return;
            buttonLayoutSwitching = true;
            this.layoutType = layoutType;

            var header = Manager.GetElement<RectTransform>("Header");
            var footer = Manager.GetElement<RectTransform>("Footer");
            Runtime.UIManager.HideExitButton(0.2f);

            DOTween.Sequence()
                .Append(header.DOAnchorPosY(PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 130f : 120f, 0.2f).OnComplete(() =>
                {
                    if (layoutType == LayoutType.Default)
                        ShowDefaultButtons();
                    else if (layoutType == LayoutType.Delete)
                        ShowDeleteButtons();
                    else
                        ShowMoveOrCopyButtons();
                    Runtime.UIManager.ShowExitButton(0.3f, Ease.OutQuart);
                    if (layoutType != LayoutType.Default)
                        foreach (SuperScrollView.Item item in superScrollView.items)
                        {
                            if (item.gameObject == null)
                                continue;
                            item.gameObject.GetComponent<SelectionToggle_Deck>().ShowToggle();
                        }
                    Title.text = layoutType switch
                    {
                        LayoutType.MoveToType => MDPro3.InterString.Get("移动到分组") + $" [{GetTypeName(selectedType)}]",
                        LayoutType.CopyToType => MDPro3.InterString.Get("复制到分组") + $" [{GetTypeName(selectedType)}]",
                        LayoutType.Delete => MDPro3.InterString.Get("删除卡组"),
                        _ => MDPro3.InterString.Get("编辑卡组")
                    };
                }))
                .Join(footer.DOAnchorPosY(PropertyOverride.PropertyOverrider.NeedMobileLayout() ? -186f : -140f, 0.2f))
                .Append(header.DOAnchorPosY(0f, 0.3f).SetEase(Ease.OutQuart))
                .Join(footer.DOAnchorPosY(0f, 0.3f).SetEase(Ease.OutQuart)).OnComplete(() =>
                {
                    buttonLayoutSwitching = false;
                });
        }

        private void HideAllToggles()
        {
            if (superScrollView == null || superScrollView.items == null)
                return;

            foreach (SuperScrollView.Item item in superScrollView.items)
                item.args[6] = "0";
            foreach (SuperScrollView.Item item in superScrollView.items)
            {
                if (item.gameObject == null)
                    continue;
                item.gameObject.GetComponent<SelectionToggle_Deck>().HideToggle();
            }
        }

        public void OnConfirm()
        {
            if (IsRecommendationPage) return;
            if (layoutType == LayoutType.Delete)
                OnDeleteConfirm();
            else if (layoutType == LayoutType.MoveToType)
                OnMoveToTypeConfirm();
            else if (layoutType == LayoutType.CopyToType)
                OnCopyToTypeConfirm();
        }

        public void OnCancel()
        {
            if (buttonLayoutSwitching) return;
            SwitchToDefaultLayout();
        }

        #endregion

    }
}
