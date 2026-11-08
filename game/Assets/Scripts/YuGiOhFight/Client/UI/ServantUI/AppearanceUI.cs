using System.Collections.Generic;

using TMPro;
using UnityEngine;

using UnityEngine.UI;
using static YuGiOhFight.Client.Controllers.Appearance;


namespace YuGiOhFight.Client.UI.ServantUI
{
    public class AppearanceUI : ServantUI
    {

        #region Elements

        private const string LABEL_SR = "ScrollRect";
        private ScrollRect m_ScrollRect;
        public ScrollRect ScrollRect =>
            m_ScrollRect = m_ScrollRect != null ? m_ScrollRect
            : Manager.GetElement<ScrollRect>(LABEL_SR);
        private CanvasGroup m_ScrollRectCG;
        private CanvasGroup ScrollRectCG =>
            m_ScrollRectCG = m_ScrollRectCG != null ? m_ScrollRectCG
            : ScrollRect.GetComponent<CanvasGroup>();

        private const string LABEL_TXT_DETAILTITLE = "TextDetailTitle";
        private TextMeshProUGUI m_TextDetailTitle;
        private TextMeshProUGUI TextDetailTitle =>
            m_TextDetailTitle = m_TextDetailTitle != null ? m_TextDetailTitle
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_DETAILTITLE);

        private const string LABEL_TXT_DETAILSETTING = "TextDetailSetting";
        private TextMeshProUGUI m_TextDetailSetting;
        private TextMeshProUGUI TextDetailSetting =>
            m_TextDetailSetting = m_TextDetailSetting != null ? m_TextDetailSetting
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_DETAILSETTING);

        private const string LABEL_TXT_DETAILDESCRIPTION = "TextDetailDescription";
        private TextMeshProUGUI m_TextDetailDescription;
        private TextMeshProUGUI TextDetailDescription =>
            m_TextDetailDescription = m_TextDetailDescription != null ? m_TextDetailDescription
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_DETAILDESCRIPTION);

        private const string LABEL_IMG = "Image";
        private Image m_Image;
        public Image Image =>
            m_Image = m_Image != null ? m_Image
            : Manager.GetElement<Image>(LABEL_IMG);

        private const string LABEL_RIMG = "RawImage";
        private RawImage m_RawImage;
        public RawImage RawImage =>
            m_RawImage = m_RawImage != null ? m_RawImage
            : Manager.GetElement<RawImage>(LABEL_RIMG);

        private const string LABEL_TXT_HOVER = "TextHover";
        private TextMeshProUGUI m_TextHover;
        private TextMeshProUGUI TextHover =>
            m_TextHover = m_TextHover != null ? m_TextHover
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_HOVER);

        private const string LABEL_GO_NAMETABLE = "NameTable";
        private GameObject m_NameTable;
        private GameObject NameTable =>
            m_NameTable = m_NameTable != null ? m_NameTable
            : Manager.GetElement(LABEL_GO_NAMETABLE);

        private const string LABEL_STG_PAGE00 = "Page00PlayerName";
        private SelectionToggle_AppearanceGenre m_Page00;
        private SelectionToggle_AppearanceGenre Page00PlayerName =>
            m_Page00 = m_Page00 != null ? m_Page00
            : Manager.GetElement<SelectionToggle_AppearanceGenre>(LABEL_STG_PAGE00);

        private const string LABEL_STG_PAGE01 = "Page01Wallpaper";
        private SelectionToggle_AppearanceGenre m_Page01;
        private SelectionToggle_AppearanceGenre Page01Wallpaper =>
            m_Page01 = m_Page01 != null ? m_Page01
            : Manager.GetElement<SelectionToggle_AppearanceGenre>(LABEL_STG_PAGE01);

        private const string LABEL_STG_PAGE02 = "Page02Face";
        private SelectionToggle_AppearanceGenre m_Page02;
        private SelectionToggle_AppearanceGenre Page02Face =>
            m_Page02 = m_Page02 != null ? m_Page02
            : Manager.GetElement<SelectionToggle_AppearanceGenre>(LABEL_STG_PAGE02);

        private const string LABEL_STG_PAGE03 = "Page03Frame";
        private SelectionToggle_AppearanceGenre m_Page03;
        private SelectionToggle_AppearanceGenre Page03Frame =>
            m_Page03 = m_Page03 != null ? m_Page03
            : Manager.GetElement<SelectionToggle_AppearanceGenre>(LABEL_STG_PAGE03);

        private const string LABEL_STG_PAGE04 = "Page04Case";
        private SelectionToggle_AppearanceGenre m_Page04;
        private SelectionToggle_AppearanceGenre Page04Case =>
            m_Page04 = m_Page04 != null ? m_Page04
            : Manager.GetElement<SelectionToggle_AppearanceGenre>(LABEL_STG_PAGE04);

        private const string LABEL_STG_PAGE05 = "Page05Protector";
        private SelectionToggle_AppearanceGenre m_Page05;
        private SelectionToggle_AppearanceGenre Page05Protector =>
            m_Page05 = m_Page05 != null ? m_Page05
            : Manager.GetElement<SelectionToggle_AppearanceGenre>(LABEL_STG_PAGE05);

        private const string LABEL_STG_PAGE06 = "Page06Field";
        private SelectionToggle_AppearanceGenre m_Page06;
        private SelectionToggle_AppearanceGenre Page06Field =>
            m_Page06 = m_Page06 != null ? m_Page06
            : Manager.GetElement<SelectionToggle_AppearanceGenre>(LABEL_STG_PAGE06);

        private const string LABEL_STG_PAGE07 = "Page07Grave";
        private SelectionToggle_AppearanceGenre m_Page07;
        private SelectionToggle_AppearanceGenre Page07Grave =>
            m_Page07 = m_Page07 != null ? m_Page07
            : Manager.GetElement<SelectionToggle_AppearanceGenre>(LABEL_STG_PAGE07);

        private const string LABEL_STG_PAGE08 = "Page08Stand";
        private SelectionToggle_AppearanceGenre m_Page08;
        private SelectionToggle_AppearanceGenre Page08Stand =>
            m_Page08 = m_Page08 != null ? m_Page08
            : Manager.GetElement<SelectionToggle_AppearanceGenre>(LABEL_STG_PAGE08);

        private const string LABEL_STG_PAGE09 = "Page09Mate";
        private SelectionToggle_AppearanceGenre m_Page09;
        private SelectionToggle_AppearanceGenre Page09Mate =>
            m_Page09 = m_Page09 != null ? m_Page09
            : Manager.GetElement<SelectionToggle_AppearanceGenre>(LABEL_STG_PAGE09);

        private const string LABEL_STG_PAGE10 = "Page10Pickup";
        private SelectionToggle_AppearanceGenre m_Page10;
        private SelectionToggle_AppearanceGenre Page10Pickup =>
            m_Page10 = m_Page10 != null ? m_Page10
            : Manager.GetElement<SelectionToggle_AppearanceGenre>(LABEL_STG_PAGE10);

        private const string LABEL_STG_OVERWRITE = "ToggleOverwrite";
        private SelectionToggle m_ToggleOverwrite;
        private SelectionToggle ToggleOverwrite =>
            m_ToggleOverwrite = m_ToggleOverwrite != null ? m_ToggleOverwrite
            : Manager.GetElement<SelectionToggle>(LABEL_STG_OVERWRITE);

        private const string LABEL_STG_PLAYER0 = "TogglePlayer0";
        private SelectionToggle_AppearancePlayer m_TogglePlayer0;
        private SelectionToggle_AppearancePlayer TogglePlayer0 =>
            m_TogglePlayer0 = m_TogglePlayer0 != null ? m_TogglePlayer0
            : Manager.GetElement<SelectionToggle_AppearancePlayer>(LABEL_STG_PLAYER0);

        private const string LABEL_TXT_INPUTHINT = "TextInputHint";
        private TextMeshProUGUI m_TextInputHint;
        private TextMeshProUGUI TextInputHint =>
            m_TextInputHint = m_TextInputHint != null ? m_TextInputHint
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_INPUTHINT);

        private const string LABEL_MONO_DECKPICKUP = "DeckPickup";
        private DeckPickup m_DeckPickup;
        private DeckPickup DeckPickup =>
            m_DeckPickup = m_DeckPickup != null ? m_DeckPickup
            : Manager.GetElement<DeckPickup>(LABEL_MONO_DECKPICKUP);

        private const string LABEL_IPT_PLAYERNAME = "InputFieldPlayerName";
        private TMP_InputField m_InputPlayerName;
        public TMP_InputField InputPlayerName =>
            m_InputPlayerName = m_InputPlayerName != null ? m_InputPlayerName
            : Manager.GetElement<TMP_InputField>(LABEL_IPT_PLAYERNAME);

        private const string LABEL_MONO_ITEM_APPEARANCE = "ItemAppearance";
        private GameObject m_Template;
        private GameObject Template =>
            m_Template = m_Template != null ? m_Template
            : Manager.GetElement(LABEL_MONO_ITEM_APPEARANCE);

        private const string LABEL_MONO_APPEARANCE_DETAIL = "Details";
        private AppearanceDetail m_Detail;
        public AppearanceDetail Detail =>
            m_Detail = m_Detail != null ? m_Detail
            : Manager.GetElement<AppearanceDetail>(LABEL_MONO_APPEARANCE_DETAIL);

        #endregion

        public static string currentContent = "PlayerName";
        private static List<Presentation.Assets.Items.Item> targetItems;
        private static List<GameObject> currentList;
        private static readonly List<GameObject> onlyOpSideShowItems = new();

        private static readonly List<GameObject> wallpapers = new();
        private static readonly List<GameObject> faces = new();
        private static readonly List<GameObject> frames = new();
        private static readonly List<GameObject> protectors = new();
        private static readonly List<GameObject> mats = new();
        private static readonly List<GameObject> graves = new();
        private static readonly List<GameObject> stands = new();
        private static readonly List<GameObject> mates = new();
        private static readonly List<GameObject> cases = new();

        private readonly Dictionary<string, List<GameObject>> pools = new()
        {
            { "Wallpaper", wallpapers },
            { "Face", faces },
            { "Frame", frames },
            { "Protector", protectors },
            { "Field", mats },
            { "Grave", graves },
            { "Stand", stands },
            { "Mate", mates },
            { "Case", cases },
        };

        private void Awake()
        {
            if (MDPro3.Config.GetBool("OverrideDeckAppearance", false))
                ToggleOverwrite.SetToggleOn();
            else
                ToggleOverwrite.SetToggleOff();
        }

        public override void SelectDefaultSelectable()
        {
            if (condition == Condition.DeckEditor)
                Page04Case.GetSelectable().Select();
            else
                Page00PlayerName.GetSelectable().Select();
        }

        public override void ShowEvent()
        {
            base.ShowEvent();

            switch (condition)
            {
                case Condition.Duel:
                    Title.text = MDPro3.InterString.Get("决斗外观");
                    break;
                case Condition.Watch:
                    Title.text = MDPro3.InterString.Get("观战外观");
                    break;
                case Condition.Replay:
                    Title.text = MDPro3.InterString.Get("回放外观");
                    break;
                case Condition.DeckEditor:
                    Title.text = MDPro3.InterString.Get("卡组外观");
                    break;
            }

            Page00PlayerName.gameObject.SetActive(condition != Condition.DeckEditor);
            Page01Wallpaper.gameObject.SetActive(condition != Condition.DeckEditor);
            Page02Face.gameObject.SetActive(condition != Condition.DeckEditor);
            Page03Frame.gameObject.SetActive(condition != Condition.DeckEditor);
            Page04Case.gameObject.SetActive(condition == Condition.DeckEditor);
            Page10Pickup.gameObject.SetActive(condition == Condition.DeckEditor);

            if (condition == Condition.DeckEditor)
            {
                Page10Pickup.GetSelectable().Select();
                Page10Pickup.SetToggleOn();
            }
            else
            {
                Page00PlayerName.GetSelectable().Select();
                Page00PlayerName.SetToggleOn();
            }

            TogglePlayer0.SetToggleOn();
        }

        protected override void HideEvent()
        {
            base.HideEvent();

            if (condition != Condition.DeckEditor)
            {
                Runtime.Program.instance.setting.GetUI<SettingServantUI>().RefreshAppearanceModeText();

                if (Runtime.UIManager.currentWallpaper != MDPro3.Config.Get("Wallpaper", Runtime.Program.items.wallpapers[0].id.ToString()))
                {
                    Runtime.UIManager.currentWallpaper = MDPro3.Config.Get("Wallpaper", Runtime.Program.items.wallpapers[0].id.ToString());
                    Runtime.Program.instance.ui_.ChangeWallpaper(Runtime.UIManager.currentWallpaper);
                }
            }
        }

        protected override void AfterHideEvent()
        {
            base.AfterHideEvent();

            foreach (var pool in pools)
            {
                foreach (var item in pool.Value)
                    item.GetComponent<SelectionToggle_AppearanceItem>().Dispose();
                pool.Value.Clear();
            }
            MDPro3.Config.Save();
        }

        public void SavePlayerName(string nameValue)
        {
            MDPro3.Config.Set(condition.ToString() + "PlayerName" + player, nameValue == string.Empty ? MDPro3.Config.EMPTY_STRING : nameValue);
        }

        public bool CanSwitchPlayer()
        {
            return TogglePlayer0.gameObject.activeSelf;
        }

        public void OnPlayerLeft()
        {
            TogglePlayer0.OnLeftSelection();
        }

        public void OnPlayerRight()
        {
            TogglePlayer0.OnRightSelection();
        }

        public void SetHoverText(string hover)
        {
            TextHover.text = hover;
        }

        public void ShowItems(string type)
        {
            currentContent = type;
            pools.TryGetValue(currentContent, out currentList);
            if (condition == Condition.DeckEditor)
                TogglePlayer0.transform.parent.gameObject.SetActive(false);
            else
                TogglePlayer0.transform.parent.gameObject.SetActive(true);

            DeckPickup.gameObject.SetActive(currentContent == "Pickup");

            if (currentContent == "PlayerName")
            {
                ScrollRectCG.alpha = 0;
                ScrollRectCG.blocksRaycasts = false;
                Detail.Hide();
                NameTable.SetActive(true);

                InputPlayerName.text = MDPro3.Config.Get(condition.ToString() + currentContent + player, MDPro3.Config.EMPTY_STRING);
                if (player == "0")
                    TextInputHint.text = MDPro3.InterString.Get("请输入您的昵称：");
                else if (player == "1")
                    TextInputHint.text = MDPro3.InterString.Get("请输入对方的昵称，留空则显示真实昵称：");
                else if (player == "0Tag")
                    TextInputHint.text = MDPro3.InterString.Get("请输入您的队友的昵称，留空则显示真实昵称：");
                else if (player == "1Tag")
                    TextInputHint.text = MDPro3.InterString.Get("请输入对方的队友的昵称，留空则显示真实昵称：");
                return;
            }
            else if (currentContent == "Pickup")
            {
                ScrollRectCG.alpha = 0f;
                ScrollRectCG.blocksRaycasts = false;
                Detail.Hide();

                DeckPickup.gameObject.SetActive(true);
                DeckPickup.SetDeck(Controllers.DeckEditor.Deck);
                return;
            }
            else if (currentContent == "Wallpaper")
            {
                TogglePlayer0.transform.parent.gameObject.SetActive(false);
            }

            ScrollRectCG.alpha = 1.0f;
            ScrollRectCG.blocksRaycasts = true;
            Detail.Show();
            NameTable.SetActive(false);
            DeckPickup.gameObject.SetActive(false);

            targetItems = currentContent switch
            {
                "Wallpaper" => Runtime.Program.items.wallpapers,
                "Face" => Runtime.Program.items.faces,
                "Frame" => Runtime.Program.items.frames,
                "Protector" => Runtime.Program.items.protectors,
                "Field" => Runtime.Program.items.mats,
                "Grave" => Runtime.Program.items.graves,
                "Stand" => Runtime.Program.items.stands,
                "Mate" => Runtime.Program.items.mates,
                "Case" => Runtime.Program.items.cases,
                _ => Runtime.Program.items.mates,
            };
            foreach (var pool in pools)
                if (pool.Key != currentContent)
                    foreach (var item in pool.Value)
                        item.GetComponent<SelectionToggle_AppearanceItem>().Hide();

            if (currentList.Count == 0)
            {
                int itemCount = 0;
                foreach (Presentation.Assets.Items.Item itemInfo in targetItems)
                {
                    if (currentContent == "Mate" && Duel.PremiumMateRules.IsPremiumVariantId(itemInfo.id))
                        continue;
                    if (itemInfo.notReady) continue;

                    GameObject item = Instantiate(Template);
                    item.SetActive(true);
                    SelectionToggle_AppearanceItem itemMono = item.GetComponent<SelectionToggle_AppearanceItem>();
                    itemMono.index = itemCount++;
                    itemMono.itemID = itemInfo.id;
                    itemMono.description = itemInfo.description;
                    itemMono.itemName = itemInfo.name;
                    itemMono.path = Presentation.Assets.Items.GetIconAddress(itemMono.itemID.ToString());
                    itemMono.transform.SetParent(ScrollRect.content, false);
                    itemMono.Refresh();
                    currentList.Add(item);
                }

#if UNITY_ANDROID || UNITY_STANDALONE_LINUX
                if (currentContent == "Mate")
                {
                    var files = new DirectoryInfo(Program.root + "CrossDuel").GetFiles("*.bundle");
                    for (int i = 0; i < files.Length; i++)
                    {
                        int code = int.Parse(files[i].Name.Replace(".bundle", string.Empty));
                        var card = CardsManager.Get(code, true);
                        GameObject item = Instantiate(Template);
                        item.SetActive(true);
                        var itemMono = item.GetComponent<SelectionToggle_AppearanceItem>();
                        itemMono.index = itemCount++;
                        itemMono.itemID = code;
                        if (card.Id == 0)
                            itemMono.itemName = MateViewerUI.GetRushDuelMateName(code);
                        else
                            itemMono.itemName = card.Name;
                        itemMono.description = card.GetDescription();
                        itemMono.path = string.Empty;
                        itemMono.transform.SetParent(ScrollRect.content, false);
                        itemMono.Refresh();
                        currentList.Add(item);
                    }
                }
#endif

                if (condition != Condition.DeckEditor)
                {
                    if (Runtime.Program.items.ListHaveNone(targetItems))
                    {
                        GameObject item = Instantiate(Template);
                        item.SetActive(true);
                        SelectionToggle_AppearanceItem itemMono = item.GetComponent<SelectionToggle_AppearanceItem>();
                        itemMono.index = itemCount++;
                        itemMono.itemID = Presentation.Assets.Items.CODE_NONE;
                        itemMono.description = MDPro3.InterString.Get("该项设置将设置为无。");
                        itemMono.itemName = MDPro3.InterString.Get("不设置");
                        itemMono.path = Presentation.Assets.Items.PATH_ICON_NONE;
                        itemMono.transform.SetParent(ScrollRect.content, false);
                        itemMono.Refresh();
                        currentList.Add(item);
                    }

                    if (Runtime.Program.items.ListHaveRandom(targetItems))
                    {
                        GameObject item = Instantiate(Template);
                        item.SetActive(true);
                        SelectionToggle_AppearanceItem itemMono = item.GetComponent<SelectionToggle_AppearanceItem>();
                        itemMono.index = itemCount++;
                        itemMono.itemID = Presentation.Assets.Items.CODE_RANDOM;
                        itemMono.description = MDPro3.InterString.Get("该项设置将随机设置。");
                        itemMono.itemName = MDPro3.InterString.Get("随机");
                        itemMono.path = Presentation.Assets.Items.PATH_ICON_RANDOM;
                        itemMono.transform.SetParent(ScrollRect.content, false);
                        itemMono.Refresh();
                        currentList.Add(item);
                    }

                    if (Runtime.Program.items.ListHaveSame(targetItems))
                    {
                        GameObject item = Instantiate(Template);
                        item.SetActive(true);
                        SelectionToggle_AppearanceItem itemMono = item.GetComponent<SelectionToggle_AppearanceItem>();
                        itemMono.index = itemCount++;
                        itemMono.itemID = Presentation.Assets.Items.CODE_SAME;
                        itemMono.description = MDPro3.InterString.Get("该项设置将与场地设置保持一致。");
                        itemMono.itemName = MDPro3.InterString.Get("一致");
                        itemMono.path = Presentation.Assets.Items.PATH_ICON_SAME;
                        itemMono.transform.SetParent(ScrollRect.content, false);
                        itemMono.Refresh();
                        currentList.Add(item);
                    }

                    if (Runtime.Program.items.ListHaveDIY(targetItems))
                    {
                        GameObject item = Instantiate(Template);
                        item.SetActive(true);
                        SelectionToggle_AppearanceItem itemMono = item.GetComponent<SelectionToggle_AppearanceItem>();
                        itemMono.index = itemCount++;
                        itemMono.itemID = Presentation.Assets.Items.CODE_DIY;
                        itemMono.description = MDPro3.InterString.Get("我方头像：") +
                                                                Runtime.Program.PATH_DIY + meString + Runtime.Program.EXPANSION_PNG + "\n" +
                                                                MDPro3.InterString.Get("对方头像：") +
                                                                Runtime.Program.PATH_DIY + opString + Runtime.Program.EXPANSION_PNG + "\n" +
                                                                MDPro3.InterString.Get("我方队友头像：") +
                                                                Runtime.Program.PATH_DIY + meTagString + Runtime.Program.EXPANSION_PNG + "\n" +
                                                                MDPro3.InterString.Get("对方队友头像：") +
                                                                Runtime.Program.PATH_DIY + opTagString + Runtime.Program.EXPANSION_PNG;
                        itemMono.itemName = MDPro3.InterString.Get("自定义");
                        itemMono.path = Presentation.Assets.Items.PATH_ICON_DIY;
                        itemMono.transform.SetParent(ScrollRect.content, false);
                        itemMono.Refresh();
                        currentList.Add(item);
                    }

                    if (targetItems == Runtime.Program.items.mats)
                    {
                        GameObject item = Instantiate(Template);
                        item.SetActive(true);
                        SelectionToggle_AppearanceItem itemMono = item.GetComponent<SelectionToggle_AppearanceItem>();
                        itemMono.index = itemCount++;
                        itemMono.itemID = Presentation.Assets.Items.CODE_SAME;
                        itemMono.description = MDPro3.InterString.Get("该项设置将与我方场地设置保持一致。");
                        itemMono.itemName = MDPro3.InterString.Get("一致");
                        itemMono.path = Presentation.Assets.Items.PATH_ICON_SAME;
                        itemMono.transform.SetParent(ScrollRect.content, false);
                        itemMono.Refresh();
                        currentList.Add(item);
                        onlyOpSideShowItems.Add(item);
                    }
                }
            }

            foreach (var item in currentList)
            {
                SelectionToggle_AppearanceItem itemMono = item.GetComponent<SelectionToggle_AppearanceItem>();
                if (currentContent == "Mate" && Duel.PremiumMateRules.IsPremiumVariantId(itemMono.itemID))
                {
                    itemMono.Hide();
                    continue;
                }

                if (player.Contains("0") && onlyOpSideShowItems.Contains(item))
                    itemMono.Hide();
                else
                    itemMono.Show();
            }

            foreach (var item in currentList)
            {
                SelectionToggle_AppearanceItem itemMono = item.GetComponent<SelectionToggle_AppearanceItem>();

                if (currentContent == "Wallpaper")
                {
                    if (itemMono.itemID.ToString() == MDPro3.Config.Get("Wallpaper", targetItems[0].id.ToString()))
                    {
                        itemMono.SetToggleOn();
                        break;
                    }
                }
                else
                {
                    var itemID = itemMono.itemID;
                    if (currentContent == "Mate" && Duel.PremiumMateRules.IsPremiumVariantId(itemID))
                        continue;

                    if (condition == Condition.DeckEditor)
                    {
                        if (currentContent == "Mate")
                        {
                            var selectedMate = Duel.PremiumMateRules.GetBaseMateId(Controllers.DeckEditor.Deck.Mate);
                            if (itemID == selectedMate)
                            {
                                itemMono.SetToggleOn();
                                break;
                            }
                        }
                        else if (itemID == Controllers.DeckEditor.Deck.Case
                            || itemID == Controllers.DeckEditor.Deck.Protector
                            || itemID == Controllers.DeckEditor.Deck.Field
                            || itemID == Controllers.DeckEditor.Deck.Grave
                            || itemID == Controllers.DeckEditor.Deck.Stand
                            || itemID == Controllers.DeckEditor.Deck.Mate)
                        {
                                itemMono.SetToggleOn();
                                break;
                        }
                    }
                    else
                    {
                        var selectedCode = MDPro3.Config.Get(condition.ToString() + currentContent + player, targetItems[0].id.ToString());
                        if (currentContent == "Mate" && int.TryParse(selectedCode, out var selectedMateCode))
                            selectedCode = Duel.PremiumMateRules.GetBaseMateId(selectedMateCode).ToString();

                        if (itemID.ToString() == selectedCode)
                        {
                            itemMono.SetToggleOn();
                            break;
                        }
                    }
                }
            }
        }

        public void SwitchPlayer(string player)
        {
            Controllers.Appearance.player = player;
            if (condition == Condition.Duel && player == "0")
                ToggleOverwrite.gameObject.SetActive(true);
            else
                ToggleOverwrite.gameObject.SetActive(false);
            ShowItems(currentContent);
        }

        public void SetOverride(bool over)
        {
            MDPro3.Config.SetBool("OverrideDeckAppearance", over);
        }

        public int GetCurrentGenreCount()
        {
            foreach (var pool in pools)
                if (pool.Key == currentContent)
                    return pool.Value.Count;
            return 0;
        }

        public GameObject GetCurrentContentItem()
        {
            if (currentContent == "PlayerName")
                return InputPlayerName.gameObject;
            if (currentContent == "Pickup")
                return null;

            if (Runtime.Program.instance.appearance.lastSelectedItem != null
                && Runtime.Program.instance.appearance.lastSelectedItem.gameObject.activeSelf)
                return Runtime.Program.instance.appearance.lastSelectedItem.gameObject;
            return ScrollRect.content.GetChild(0).gameObject;
        }

        public void SelectPlayerNameToggle()
        {
            Runtime.UserInput.NextSelectionIsAxis = true;
            Page00PlayerName.GetSelectable().Select();
        }

        public bool InPickupPage()
        {
            return currentContent == "Pickup";
        }

        public void OnPickupChange()
        {
            Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.deckBrowser);
        }

    }
}
