using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI.Client
{
    internal partial class FightClient
    {
        private sealed class ClientDeck
        {
            internal string Name, Path, Group;
            internal MDPro3.Utility.DeckLibraryEntry Recommendation;
        }
        private List<ClientDeck> deckItems;
        private RectTransform deckGrid, deckPreview;
        private TMP_InputField deckSearchInput;
        private string deckGroup = "全部";
        private bool pickerPersonalOnly;
        private MDPro3.Utility.DeckLibraryEntry detailEntry;
        private string personalDetailPath;
        private string detailOrigin = "recommended";
        private int deckGridColumns;
        private float deckGridWidth, deckGridHeight;
        private RectTransform detailInspector;
        private int detailInspectedCode;
        private string detailReadingDeckPath;
        private readonly Dictionary<int, float> detailCardReadingPositions = new Dictionary<int, float>();
        private readonly Dictionary<int, float> cardReadingPositions = new Dictionary<int, float>();
        private readonly HashSet<string> deckTagGroups = new HashSet<string>();
        private readonly List<Tuple<int, Outline>> detailCardFrames = new List<Tuple<int, Outline>>();
        private readonly List<Tuple<ClientDeck, RectTransform>> deckBrowseFrames = new List<Tuple<ClientDeck, RectTransform>>();

        private string[] DeckBrowseTags(ClientDeck deck)
        {
            MDPro3.Utility.DeckLibraryEntry entry = deck.Recommendation;
            if (entry == null) return Array.Empty<string>();
            return entry.Tags.Length > 0 ? entry.Tags : entry.Academy?.tags ?? Array.Empty<string>();
        }

        private bool DeckMatchesGroup(ClientDeck deck, string group)
            => group == "全部" || (page == "picker" && !pickerPersonalOnly && group == "我的" ? deck.Recommendation == null
                : deckTagGroups.Contains(group) ? DeckBrowseTags(deck).Contains(group) : deck.Group == group);

        private void ModuleDivider(Transform parent, string title, float x, float y, float width, float size = 20)
        {
            FightUI.Icon(parent, "diamond", x, y + 9, 10, 10, FightUI.GoldBorder);
            FightUI.Text(parent, title, x + 20, y, width - 20, 32, size, FightUI.Foreground);
            FightUI.Line(parent, x, y + 38, width, FightUI.Border);
        }

        private Button DeckTab(Transform parent, string label, float x, float y, float width, bool selected, Action clicked)
            => FightUI.Tab(parent, label, selected, x, y, width, 48, clicked);

        private void DeckGroupRow(Transform parent, string group, int count, float y, float width)
        {
            bool active = deckGroup == group;
            var row = FightUI.Panel(parent, 4, y, width, 48, active ? FightUI.Raised : Color.clear, true, group);
            bool personalGroup = group != "全部" && group != "默认分组" && !(page == "picker" && !pickerPersonalOnly && group == "我的")
                && (page == "decks" || page == "picker") && Directory.Exists(Path.Combine(Runtime.Program.PATH_DECK, group));
            string caption = group == "全部" ? "全部卡组" : personalGroup ? FightText.Keep(group) : group;
            var groupLabel = FightUI.Text(row, caption, 14, 0, width - 56, 48, Compact ? 17 : 19,
                active ? FightUI.Active : FightUI.Muted, TextAlignmentOptions.MidlineLeft);
            groupLabel.enableWordWrapping = false; groupLabel.enableAutoSizing = true;
            groupLabel.fontSizeMin = 15; groupLabel.fontSizeMax = Compact ? 17 : 19;
            FightUI.Text(row, count.ToString(), width - 40, 0, 28, 48, 15,
                active ? FightUI.Gold : FightUI.Muted, TextAlignmentOptions.MidlineRight);
            var button = row.gameObject.AddComponent<Button>(); button.targetGraphic = row.GetComponent<Image>();
            SetLibrarySelection(button, active);
            row.GetComponent<Outline>().enabled = active;
            if (!active) button.targetGraphic.color = Color.clear;
            FightFrame frame = row.GetComponentInChildren<FightFrame>(); frame.ButtonFrame = true;
            row.gameObject.AddComponent<FightButtonFeedback>().Bind(frame);
            FightUI.Hint(row.gameObject, caption);
            button.onClick.AddListener(() => { deckGroup = group; Rebuild(); });
        }

        private void DeckCountStrip(Transform parent, MDPro3.Duel.YGOSharp.Deck deck, float x, float y, float width, float height = 56)
        {
            string[] labels = { "主卡组", "额外卡组", "副卡组" };
            int[] values = { deck.Main.Count, deck.Extra.Count, deck.Side.Count };
            float cellWidth = width / 3;
            for (int i = 0; i < 3; i++)
            {
                if (i > 0) FightUI.Panel(parent, x + i * cellWidth, y + 8, 1, height - 16, FightUI.Border, false);
                var number = FightUI.Text(parent, values[i].ToString(), x + i * cellWidth + 4, y, cellWidth - 8, 30, 24,
                    FightUI.Foreground, TextAlignmentOptions.Center);
                number.fontStyle = FontStyles.Bold;
                FightUI.Text(parent, labels[i], x + i * cellWidth + 4, y + 32, cellWidth - 8, 24, 14,
                    FightUI.Muted, TextAlignmentOptions.Center);
            }
        }

        private List<ClientDeck> PersonalDecks()
        {
            var list = new List<ClientDeck>();
            if (!Directory.Exists(Runtime.Program.PATH_DECK)) return list;
            foreach (var dir in new[] { Runtime.Program.PATH_DECK }.Concat(Directory.GetDirectories(Runtime.Program.PATH_DECK).OrderBy(Path.GetFileName)))
                foreach (var path in Directory.GetFiles(dir, "*.ydk").OrderBy(Path.GetFileName))
                {
                    if (MDPro3.Utility.DeckLibrary.IsBundledTemplate(path)) continue;
                    list.Add(new ClientDeck { Name = PersonalName(path), Path = Path.GetFullPath(path), Group = Path.GetFullPath(dir) == Path.GetFullPath(Runtime.Program.PATH_DECK) ? "默认分组" : Path.GetFileName(dir) });
                }
            return list;
        }
        private string PersonalName(string path) => Path.ChangeExtension(Path.GetFullPath(path)
            .Substring(Path.GetFullPath(Runtime.Program.PATH_DECK).TrimEnd(Path.DirectorySeparatorChar).Length + 1), null);
        private static string PersonalDeckGroupCaption(string path)
        {
            string directory = Path.GetDirectoryName(Path.GetFullPath(path));
            bool rootGroup = string.Equals(directory, Path.GetFullPath(Runtime.Program.PATH_DECK)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
            return rootGroup ? "默认分组" : FightText.Keep(Path.GetFileName(directory));
        }
        private List<ClientDeck> PickerDecks()
        {
            // Personal decks are files, including separately named copies of the same
            // construction. Reuse the library page's snapshot rather than its AI aliases.
            var list = PersonalDecks();
            foreach (MDPro3.AI.ResponsesDeckChoice choice in MDPro3.AI.ResponsesDeckLibrary.Load(Runtime.Program.PATH_DECK).Choices)
            {
                MDPro3.Utility.DeckLibraryEntry entry = recommendations.FirstOrDefault(item => SameDeckPath(item.SourcePath, choice.Path));
                if (entry == null) continue;
                list.Add(new ClientDeck { Name = entry.Name, Path = choice.Path,
                    Group = entry.Page == MDPro3.Utility.DeckLibraryPage.Classic ? "经典" : "竞技", Recommendation = entry });
            }
            return list;
        }
        private void PickDeck(string slot, string origin, Action<string> commit, bool personalOnly = false)
        {
            pickerSlot = slot; pickerOrigin = origin; pickerCommit = commit; pickerPersonalOnly = personalOnly; Navigate("picker");
        }
        private void SetCurrent(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) throw new FileNotFoundException("卡组文件不存在。");
            MDPro3.Config.SetConfigDeck(PersonalName(path)); MDPro3.Config.Save(); ShowStatus("已设为当前卡组：" + FightText.Keep(PersonalName(path)));
            cpuDeck = PersonalName(path);
        }
        private void RenderDecks()
        {
            bool picker = page == "picker", mine = page == "decks";
            if (mine || (picker && pickerPersonalOnly)) deckItems = PersonalDecks();
            else if (picker)
                deckItems = PickerDecks();
            else deckItems = recommendations.Where(e => e.Page == (page == "competitive" ? MDPro3.Utility.DeckLibraryPage.Competitive : MDPro3.Utility.DeckLibraryPage.Classic)).Select(e => new ClientDeck { Name = e.Name, Path = e.SourcePath, Group = e.IsCurated ? "精选构筑" : "教学构筑", Recommendation = e }).ToList();
            if (picker) PageTitle("选择卡组 · " + pickerSlot);
            if (!picker)
            {
                DeckTab(Body, "我的", 0, 0, 112, mine, () => Navigate("decks"));
                DeckTab(Body, "推荐 · 经典", 128, 0, 148, page == "recommended", () => Navigate("recommended"));
                DeckTab(Body, "推荐 · 竞技", 292, 0, 148, page == "competitive", () => Navigate("competitive"));
            }
            float groupsW = Compact ? 150 : 200, previewW = Compact ? 294 : 360, gridX = groupsW + Gap;
            deckGridWidth = BodyWidth - groupsW - previewW - Gap * 2; deckGridHeight = BodyHeight - 124;
            Button clearSearch = null;
            var searchField = FightUI.Input(Body, search, mine ? "搜索我的卡组" : "搜索主题、版本或构筑标签", gridX, 64, deckGridWidth, 48,
                v => { search = v; if (clearSearch != null) clearSearch.interactable = v.Length > 0; RenderDeckGrid(); });
            deckSearchInput = searchField;
            FightUI.Icon(searchField.transform, "search", 14, 14, 20, 20, FightUI.Muted);
            FightUI.Place(searchField.textViewport, 44, 5, deckGridWidth - 96, 38);
            foreach (var label in searchField.textViewport.GetComponentsInChildren<TextMeshProUGUI>())
                FightUI.Place(label.rectTransform, 0, 0, deckGridWidth - 96, 38);
            FightUI.FocusKey(searchField, "deck-browse:search");
            clearSearch = FightUI.Button(Body, "清空搜索", gridX + deckGridWidth - 44, 66, 40, 44, () =>
            {
                ClearDeckBrowseSearch();
            }, false, search.Length > 0);
            FightUI.FocusKey(clearSearch, "deck-browse:clear-search");
            FightUI.Hint(clearSearch.gameObject, "清空关键词，保留当前分组并继续输入");
            clearSearch.GetComponent<Image>().color = Color.clear; clearSearch.GetComponent<Outline>().enabled = false;
            clearSearch.GetComponentInChildren<TextMeshProUGUI>().gameObject.SetActive(false);
            FightUI.Icon(clearSearch.transform, "close", 12, 14, 16, 16, FightUI.Muted);
            var groups = FightUI.Panel(Body, 0, 64, groupsW, BodyHeight - 64, FightUI.PanelColor, false);
            var groupNames = (picker && !pickerPersonalOnly ? new[] { "全部", "我的" } : new[] { "全部" })
                .Concat(deckItems.Select(d => d.Group).Distinct()).Distinct().ToList();
            deckTagGroups.Clear();
            if (!mine && !picker)
            {
                var tags = deckItems.SelectMany(d => DeckBrowseTags(d).Distinct()).Where(tag => !string.IsNullOrEmpty(tag) && tag.Any(c => c >= '\u4e00' && c <= '\u9fff'))
                    .GroupBy(tag => tag).Where(group => group.Count() > 1 && group.Count() < deckItems.Count)
                    .OrderByDescending(group => group.Count()).ThenBy(group => group.Key).Take(12).Select(group => group.Key).ToList();
                if (tags.Count > 0) { foreach (var tag in tags) deckTagGroups.Add(tag); groupNames = new[] { "全部" }.Concat(tags).ToList(); }
            }
            ModuleDivider(groups, mine ? "我的分组" : deckTagGroups.Count > 0 ? "战术标签" : "构筑来源", 14, 14, groupsW - 28, 18);
            if (mine && Directory.Exists(Runtime.Program.PATH_DECK))
                groupNames = groupNames.Concat(Directory.GetDirectories(Runtime.Program.PATH_DECK).Select(Path.GetFileName)).Distinct().ToList();
            if (!groupNames.Contains(deckGroup)) deckGroup = "全部";
            float firstGroupActionY = BodyHeight - 262;
            float groupListHeight = mine ? firstGroupActionY - 80 : BodyHeight - 154;
            var gl = FightUI.Scroll(groups, 4, 64, groupsW - 8, Mathf.Max(64, groupListHeight), groupNames.Count * 54);
            for (int i = 0; i < groupNames.Count; i++)
            {
                var group = groupNames[i];
                DeckGroupRow(gl, group, deckItems.Count(d => DeckMatchesGroup(d, group)), i * 54, groupsW - 16);
            }
            if (mine)
            {
                FightUI.Line(groups, 14, firstGroupActionY - 18, groupsW - 28);
                FightUI.Button(groups, "+ 新建卡组", 10, firstGroupActionY, groupsW - 20, 44, NewDeckDialog, true);
                FightUI.TextAction(groups, "+ 新建分组", 16, BodyHeight - 208, groupsW - 32, 40, () => Prompt("新建分组", "分组名称", "", name => { CheckGroupName(name); Directory.CreateDirectory(Path.Combine(Runtime.Program.PATH_DECK, name)); Rebuild(); }));
                FightUI.TextAction(groups, "管理分组", 16, BodyHeight - 160, groupsW - 32, 40, ManageGroup, deckGroup != "全部" && deckGroup != "默认分组");
            }
            deckGrid = FightUI.Node(Body, "Deck Grid", gridX, 124, deckGridWidth, deckGridHeight);
            deckPreview = FightUI.Panel(Body, BodyWidth - previewW, 64, previewW, BodyHeight - 64, FightUI.PanelColor, false);
            FightUI.Line(deckPreview, 0, 0, previewW, FightUI.GoldBorder);
            deckGridColumns = Compact ? 3 : LargeViewport ? 6 : 4;
            RenderDeckGrid();
        }

        private bool DeckMatchesSearch(ClientDeck deck, string[] terms)
            => deck.Recommendation != null ? terms.All(term => (MDPro3.Utility.DeckLibrary.SearchText(deck.Recommendation) + " " + FightText.Get(MDPro3.Utility.DeckLibrary.SearchText(deck.Recommendation))).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                : terms.All(term => deck.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);

        private void ClearDeckBrowseSearch()
        {
            if (deckSearchInput == null) return;
            deckSearchInput.text = "";
            deckSearchInput.Select(); deckSearchInput.ActivateInputField(); deckSearchInput.caretPosition = 0;
        }

        private void SearchAllDeckGroups()
        {
            deckGroup = "全部"; Rebuild();
            if (deckSearchInput == null) return;
            // Continue the retained query instead of replacing it on first focus.
            deckSearchInput.onFocusSelectAll = false;
            deckSearchInput.Select(); deckSearchInput.ActivateInputField(); deckSearchInput.caretPosition = deckSearchInput.text.Length;
        }

        private void RenderDeckGrid(bool retainScroll = false)
        {
            if (deckGrid == null) return;
            var oldScroll = deckGrid.GetComponentInChildren<ScrollRect>();
            float scrollPosition = retainScroll && oldScroll != null ? oldScroll.verticalNormalizedPosition : 1;
            FightUI.Clear(deckGrid); deckBrowseFrames.Clear();
            var terms = search.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var items = deckItems.Where(d => DeckMatchesGroup(d, deckGroup) && DeckMatchesSearch(d, terms)).ToList();
            if (page != "picker" && items.Count > 0 && !items.Any(d => d.Path == selectedPath))
            {
                var currentPath = Runtime.Program.PATH_DECK + MDPro3.Config.GetConfigDeckName() + ".ydk";
                selectedPath = (items.FirstOrDefault(d => SameDeckPath(currentPath, d.Path)) ?? items[0]).Path;
            }
            float cellW = (deckGridWidth - 16 * (deckGridColumns - 1)) / deckGridColumns;
            float coverHeight = cellW * .75f, cellH = coverHeight + 148;
            var scroll = FightUI.Scroll(deckGrid, 0, 0, deckGridWidth, deckGridHeight, Mathf.Ceil((float)items.Count / deckGridColumns) * (cellH + 16), out var gridScroll);
            if (items.Count == 0)
            {
                bool emptyLibrary = page == "decks" && deckItems.Count == 0;
                bool emptyPersonalPicker = page == "picker" && pickerPersonalOnly && deckItems.Count == 0;
                float middle = Mathf.Max(20, (deckGridHeight - 300) / 2);
                var illustration = FightUI.Asset(scroll, "decks.png", (deckGridWidth - 220) / 2, middle, 220, 108);
                illustration.color = new Color(.65f, .65f, .65f, .8f);
                FightUI.Text(scroll, emptyLibrary ? "尚未创建卡组" : emptyPersonalPicker ? "暂无个人卡组" : "没有匹配的构筑", 20, middle + 128, deckGridWidth - 40, 40, Compact ? 23 : 27, FightUI.Foreground, TextAlignmentOptions.Center);
                float actionWidth = Mathf.Min(180, (deckGridWidth - 48) / 2);
                if (emptyLibrary || emptyPersonalPicker)
                {
                    FightUI.Button(scroll, "创建卡组", (deckGridWidth - actionWidth * 2 - 16) / 2, middle + 184, actionWidth, 48, NewDeckDialog, true);
                    FightUI.Button(scroll, emptyPersonalPicker ? "返回" : "浏览推荐", (deckGridWidth + 16) / 2, middle + 184, actionWidth, 48, () => Navigate(emptyPersonalPicker ? pickerOrigin : "recommended"));
                }
                else
                {
                    int elsewhere = deckGroup == "全部" ? 0 : deckItems.Count(d => DeckMatchesSearch(d, terms));
                    float recoveryY = middle + 184;
                    if (elsewhere > 0)
                    {
                        FightUI.Text(scroll, "其他分组中有符合当前搜索的卡组。", 20, middle + 178, deckGridWidth - 40, 40, 17, FightUI.Muted, TextAlignmentOptions.Center);
                        recoveryY = middle + 232;
                    }
                    bool clearKeyword = terms.Length > 0;
                    int actionCount = (clearKeyword ? 1 : 0) + (elsewhere > 0 || !clearKeyword ? 1 : 0);
                    float recoveryWidth = Mathf.Min(210, (deckGridWidth - 48 - (actionCount - 1) * 16) / actionCount);
                    float recoveryX = (deckGridWidth - actionCount * recoveryWidth - (actionCount - 1) * 16) / 2;
                    if (clearKeyword)
                    {
                        var clear = FightUI.Button(scroll, "清空关键词", recoveryX, recoveryY, recoveryWidth, 48, ClearDeckBrowseSearch, true);
                        FightUI.FocusKey(clear, "deck-browse:empty-clear-search");
                        FightUI.Hint(clear.gameObject, "清空搜索，保留当前分组并继续输入");
                        recoveryX += recoveryWidth + 16;
                    }
                    if (elsewhere > 0 || !clearKeyword)
                    {
                        var all = FightUI.Button(scroll, clearKeyword ? "在全部卡组中查找" : "查看全部卡组", recoveryX, recoveryY, recoveryWidth, 48, SearchAllDeckGroups, !clearKeyword);
                        FightUI.FocusKey(all, "deck-browse:empty-all-groups");
                        FightUI.Hint(all.gameObject, "切换到全部卡组，保留当前搜索词");
                    }
                }
            }
            for (int i = 0; i < items.Count; i++)
            {
                ClientDeck d = items[i]; bool selected = selectedPath == d.Path;
                var card = FightUI.Panel(scroll, (i % deckGridColumns) * (cellW + 16), (i / deckGridColumns) * (cellH + 16), cellW, cellH,
                    selected ? FightUI.Raised : FightUI.PanelColor, true, "Deck " + d.Name);
                var outline = card.GetComponent<Outline>(); outline.effectColor = selected ? FightUI.Active : FightUI.Border;
                deckBrowseFrames.Add(Tuple.Create(d, card));
                try
                {
                    MDPro3.Duel.YGOSharp.Deck deck = new MDPro3.Duel.YGOSharp.Deck(d.Path);
                    FightUI.Art(card, d.Recommendation?.CoverCard ?? MDPro3.Utility.DeckLibrary.CoverFor(deck), 5, 5, cellW - 10, coverHeight);
                    for (int band = 0; band < 4; band++)
                    {
                        var shade = FightUI.Panel(card, 5, coverHeight - 27 + band * 8, cellW - 10, 8,
                            new Color(.02f, .04f, .06f, .08f + band * .09f), false, "Cover shade");
                        shade.GetComponent<Image>().raycastTarget = false;
                    }
                    FightUI.Line(card, 5, coverHeight + 6, cellW - 10, FightUI.Border);
                    FightIcon indicator = FightUI.Icon(card, "diamond", cellW - 25, coverHeight - 16, 12, 12, FightUI.Active);
                    indicator.name = "Preview indicator"; indicator.gameObject.SetActive(selected);
                    bool current = SameDeckPath(Runtime.Program.PATH_DECK + MDPro3.Config.GetConfigDeckName() + ".ydk", d.Path);
                    if (d.Recommendation != null)
                    {
                        var badge = FightUI.Panel(card, 12, 12, 62, 24, FightUI.Background, false);
                        FightUI.Text(badge, "只读模板", 0, 0, 62, 24, 12, FightUI.Muted, TextAlignmentOptions.Center);
                    }
                    if (current)
                    {
                        var badge = FightUI.Panel(card, cellW - 84, 12, 72, 24, FightUI.Background, false);
                        FightUI.Text(badge, "当前使用", 0, 0, 72, 24, 13, FightUI.Active, TextAlignmentOptions.Center);
                    }
                    var title = FightUI.RawText(card, d.Recommendation != null ? FightText.Get(d.Name) : d.Name, 12, coverHeight + 17, cellW - 24, 66, Compact ? 21 : 23, FightUI.Foreground);
                    title.name = "Deck tile title"; title.fontStyle = FontStyles.Bold;
                    title.enableWordWrapping = true; title.enableAutoSizing = true;
                    title.fontSizeMin = Compact ? 18 : 20; title.fontSizeMax = Compact ? 21 : 23;
                    FightUI.RawHint(card.gameObject, d.Recommendation != null ? FightText.Get(d.Name) : d.Name);
                    FightUI.Text(card, d.Recommendation != null ? d.Recommendation.Variant ?? d.Group
                        : PersonalDeckGroupCaption(d.Path), 12, coverHeight + 86, cellW - 24, 22, 14, FightUI.Muted);
                    FightUI.Line(card, 12, coverHeight + 111, cellW - 24, FightUI.Border);
                    FightUI.Text(card, "主 " + deck.Main.Count + "   额外 " + deck.Extra.Count + "   副 " + deck.Side.Count,
                        12, coverHeight + 118, cellW - 24, 24, 14, FightUI.Muted);
                }
                catch (Exception e) { FightUI.Text(card, (d.Recommendation != null ? d.Name : FightText.Keep(d.Name)) + "\n" + e.Message, 12, 12, cellW - 24, cellH - 24, 18, FightUI.Danger); }
                var button = card.gameObject.AddComponent<Button>(); button.targetGraphic = card.GetComponent<Image>();
                var colors = button.colors; colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f); colors.fadeDuration = .16f; button.colors = colors;
                card.gameObject.AddComponent<FightButtonFeedback>().Bind(card.GetComponentInChildren<FightFrame>());
                FightUI.PreviewOnFocus(button, () => PreviewDeck(d));
                button.onClick.AddListener(() =>
                {
                    if (page == "picker") CommitPickedDeck(d.Path);
                    else OpenDeckDetail(d);
                });
            }
            Canvas.ForceUpdateCanvases();
            gridScroll.verticalNormalizedPosition = Mathf.Clamp01(scrollPosition);
            RenderDeckPreview(items.FirstOrDefault(d => d.Path == selectedPath));
        }
        private void PreviewDeck(ClientDeck deck)
        {
            if (selectedPath == deck.Path) return;
            selectedPath = deck.Path;
            foreach (var item in deckBrowseFrames)
            {
                if (item.Item2 == null) continue;
                bool selected = item.Item1.Path == selectedPath;
                item.Item2.GetComponent<Image>().color = selected ? FightUI.Raised : FightUI.PanelColor;
                item.Item2.GetComponent<Outline>().effectColor = selected ? FightUI.Active : FightUI.Border;
                var indicator = item.Item2.Find("Preview indicator");
                if (indicator != null) indicator.gameObject.SetActive(selected);
            }
            RenderDeckPreview(deck);
        }

        private void RenderDeckPreview(ClientDeck d)
        {
            FightUI.Clear(deckPreview); float w = deckPreview.rect.width, h = deckPreview.rect.height;
            ModuleDivider(deckPreview, "构筑预览", 18, 16, w - 36, 20);
            if (d == null)
            {
                float emptyTop = Mathf.Max(76, ((page == "picker" ? h - 96 : h) - 218) / 2);
                FightUI.Icon(deckPreview, "deck", (w - 48) / 2, emptyTop, 48, 48, FightUI.GoldBorder);
                bool emptyLibrary = (page == "decks" || page == "picker" && pickerPersonalOnly) && deckItems.Count == 0;
                string emptyTitle = deckBrowseFrames.Count == 0 ? emptyLibrary ? "尚未创建卡组" : "暂无可预览构筑" : "选择一套卡组";
                FightUI.Text(deckPreview, emptyTitle, 20, emptyTop + 72, w - 40, 44, 23, FightUI.Foreground, TextAlignmentOptions.Center);
                if (page == "picker")
                {
                    FightUI.Button(deckPreview, "返回", 18, h - 64, w - 36, 48, () => Navigate(pickerOrigin));
                }
                return;
            }
            Try(() =>
            {
                MDPro3.Duel.YGOSharp.Deck deck = new MDPro3.Duel.YGOSharp.Deck(d.Path);
                bool templatePreview = page != "picker" && d.Recommendation != null;
                float actionsY = templatePreview ? h - 82 : h - 138;
                FightUI.Line(deckPreview, 18, actionsY, w - 36, FightUI.GoldBorder);
                float countsY = actionsY - 72;
                DeckCountStrip(deckPreview, deck, 18, countsY + 4, w - 36);
                float previewHeight = Mathf.Max(100, countsY - 82);
                var c = FightUI.Scroll(deckPreview, 8, 70, w - 16, previewHeight, 500);
                float artHeight = Compact ? 64 : 176;
                FightUI.Art(c, d.Recommendation?.CoverCard ?? MDPro3.Utility.DeckLibrary.CoverFor(deck), 10, 0, w - 36, artHeight);
                var title = FightUI.RawText(c, d.Recommendation != null ? FightText.Get(d.Name) : d.Name, 10, artHeight + (Compact ? 10 : 12), w - 36, 96, Compact ? 23 : 27, FightUI.Foreground);
                title.enableWordWrapping = true; title.enableAutoSizing = true;
                title.fontSizeMin = Compact ? 20 : 23; title.fontSizeMax = Compact ? 23 : 27;
                float titleHeight = Mathf.Clamp(title.GetPreferredValues(title.text, w - 36, float.PositiveInfinity).y + 6, 34, 92);
                title.rectTransform.sizeDelta = new Vector2(w - 36, titleHeight);
                FightUI.RawHint(title.gameObject, d.Recommendation != null ? FightText.Get(d.Name) : d.Name);
                float y = artHeight + (Compact ? 14 : 18) + titleHeight;
                FightUI.Text(c, d.Recommendation != null ? "推荐模板 · 只读" : "我的卡组 · "
                    + PersonalDeckGroupCaption(d.Path), 10, y, w - 36, 24, 15, FightUI.Muted);
                y += Compact ? 28 : 44;
                // Keep a section heading whole at the initial viewport edge, including long names.
                if (y < previewHeight && y + 40 > previewHeight) y = previewHeight + 12;
                if (!string.IsNullOrWhiteSpace(d.Recommendation?.Description))
                {
                    ModuleDivider(c, "构筑思路", 10, y, w - 36, 18);
                    var description = FightUI.Text(c, d.Recommendation.Description, 10, y + 48, w - 36, 180, 17, FightUI.Muted);
                    description.lineSpacing = 5; description.paragraphSpacing = 8;
                    description.overflowMode = TextOverflowModes.Overflow;
                    float descriptionHeight = Mathf.Max(68, description.GetPreferredValues(description.text, w - 36, 0).y + 12);
                    description.rectTransform.sizeDelta = new Vector2(w - 36, descriptionHeight);
                    y += 60 + descriptionHeight;
                }
                if (d.Recommendation?.Academy != null)
                {
                    FightUI.TextAction(c, "基础教程  →", 10, y, w - 36, 40, () => OpenTutorial(d.Recommendation.Academy));
                    y += 48;
                }
                c.sizeDelta = new Vector2(w - 16, y + 12);
                if (page == "picker")
                {
                    float secondaryWidth = (w - 44) / 2;
                    FightUI.Button(deckPreview, "返回", 18, h - 120, secondaryWidth, 44, () => Navigate(pickerOrigin));
                    var inspect = FightUI.Button(deckPreview, "查看完整构筑", 26 + secondaryWidth, h - 120, secondaryWidth, 44, () => OpenDeckDetail(d));
                    var caption = inspect.GetComponentInChildren<TextMeshProUGUI>();
                    caption.enableAutoSizing = true; caption.fontSizeMin = 15; caption.fontSizeMax = 19;
                    FightUI.Hint(inspect.gameObject, "查看完整构筑");
                    FightUI.Button(deckPreview, "选择这套卡组", 18, h - 64, w - 36, 48, () => CommitPickedDeck(d.Path), true);
                }
                else if (templatePreview)
                {
                    FightUI.Button(deckPreview, "查看完整构筑  →", 18, h - 64, w - 36, 48, () => OpenDeckDetail(d), true);
                }
                else if (d.Recommendation == null)
                {
                    bool current = SameDeckPath(Runtime.Program.PATH_DECK + MDPro3.Config.GetConfigDeckName() + ".ydk", d.Path);
                    FightUI.Button(deckPreview, current ? "当前使用" : "设为当前", 18, h - 120, w - 122, 44, () => Try(() => { SetCurrent(d.Path); RenderDeckGrid(true); }), false, !current);
                    FightUI.Button(deckPreview, "更多", w - 92, h - 120, 74, 44, () => DeckActions(d));
                    FightUI.Button(deckPreview, "编辑卡组  →", 18, h - 64, w - 36, 48, () => OpenEditor(PersonalName(d.Path)), true);
                }
            });
        }
        private void OpenDeckDetail(ClientDeck deck)
        {
            Try(() =>
            {
                selectedPath = deck.Path;
                detailEntry = deck.Recommendation;
                personalDetailPath = detailEntry == null ? deck.Path : null;
                detailOrigin = page;
                Navigate("deck-detail");
            });
        }

        private void CommitPickedDeck(string path)
        {
            Try(() =>
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) throw new FileNotFoundException("卡组文件不存在。");
                var origin = pickerOrigin;
                pickerCommit?.Invoke(path);
                Navigate(origin);
            });
        }

        private void RenderDeckDetail()
        {
            string path = detailEntry?.SourcePath ?? personalDetailPath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) { Navigate(detailOrigin); return; }
            string name = detailEntry?.Name ?? PersonalName(path);
            bool sameReadingDeck = SameDeckPath(path, detailReadingDeckPath);
            if (!sameReadingDeck) { detailCardReadingPositions.Clear(); detailReadingDeckPath = path; }
            PageTitle(name, detailEntry != null);
            MDPro3.Duel.YGOSharp.Deck deck = new MDPro3.Duel.YGOSharp.Deck(path);
            int cover = detailEntry?.CoverCard ?? MDPro3.Utility.DeckLibrary.CoverFor(deck);
            float inspectorWidth = Compact ? 218 : LargeViewport ? 352 : 280, summaryWidth = Compact ? 254 : LargeViewport ? 408 : 336;
            float cardsWidth = BodyWidth - inspectorWidth - summaryWidth - Gap * 2;
            float top = 94, contentHeight = BodyHeight - top - 70;
            detailInspector = FightUI.Panel(Body, 0, top, inspectorWidth, contentHeight, FightUI.PanelColor, false);
            detailCardFrames.Clear();
            var cards = FightUI.Scroll(Body, inspectorWidth + Gap, top, cardsWidth, contentHeight, 1200);
            DrawDeckCards(cards, deck, cardsWidth, RenderDeckDetailInspector, true);
            var allCodes = deck.Main.Concat(deck.Extra).Concat(deck.Side);
            int initialCard = sameReadingDeck && allCodes.Contains(detailInspectedCode) ? detailInspectedCode
                : allCodes.Contains(cover) ? cover : allCodes.FirstOrDefault();
            RenderDeckDetailInspector(initialCard);
            var summaryPanel = FightUI.Panel(Body, BodyWidth - summaryWidth, top, summaryWidth, contentHeight, FightUI.PanelColor, false);
            float coverHeight = Compact ? 112 : 168;
            FightUI.Art(summaryPanel, cover, 16, 16, summaryWidth - 32, coverHeight);
            float countsY = coverHeight + 34;
            DeckCountStrip(summaryPanel, deck, 16, countsY, summaryWidth - 32);
            FightUI.Line(summaryPanel, 16, countsY + 68, summaryWidth - 32, FightUI.GoldBorder);
            float summaryTop = countsY + 82;
            var summary = FightUI.Scroll(summaryPanel, 0, summaryTop, summaryWidth, contentHeight - summaryTop - 8, 650);
            float y = 8;
            string source = detailEntry != null ? "推荐 · " + (detailEntry.Page == MDPro3.Utility.DeckLibraryPage.Classic ? "经典" : "竞技") + " · 只读模板"
                : "我的卡组 · " + PersonalDeckGroupCaption(path);
            FightUI.Text(summary, source, 16, y, summaryWidth - 32, 30, 15, FightUI.Muted); y += 46;
            if (!string.IsNullOrWhiteSpace(detailEntry?.Description))
            {
                ModuleDivider(summary, "构筑思路", 16, y, summaryWidth - 32, 21); y += 52;
                string description = detailEntry.Description;
                var descriptionText = FightUI.Text(summary, description, 16, y, summaryWidth - 32, 360, 18, FightUI.Foreground);
                descriptionText.lineSpacing = 5; descriptionText.paragraphSpacing = 10;
                descriptionText.overflowMode = TextOverflowModes.Overflow;
                float descriptionHeight = descriptionText.GetPreferredValues(descriptionText.text, summaryWidth - 32, float.PositiveInfinity).y + 12;
                descriptionText.rectTransform.sizeDelta = new Vector2(summaryWidth - 32, descriptionHeight); y += descriptionHeight + 24;
            }
            if (detailEntry?.Tags.Length > 0)
            {
                var tags = FightUI.Text(summary, string.Join(" · ", detailEntry.Tags), 16, y, summaryWidth - 32, 70, 15, FightUI.Muted);
                tags.overflowMode = TextOverflowModes.Overflow;
                float tagsHeight = tags.GetPreferredValues(tags.text, summaryWidth - 32, float.PositiveInfinity).y + 8;
                tags.rectTransform.sizeDelta = new Vector2(summaryWidth - 32, tagsHeight); y += tagsHeight + 24;
            }
            if (detailEntry?.Academy != null)
            {
                ModuleDivider(summary, "基础教程", 16, y, summaryWidth - 32, 20); y += 50;
                var tutorialHint = FightUI.Text(summary, "教程使用同主题基础构筑，起手与当前推荐可能不同。", 16, y, summaryWidth - 32, 86, 16, FightUI.Muted);
                tutorialHint.overflowMode = TextOverflowModes.Overflow;
                float tutorialHintHeight = tutorialHint.GetPreferredValues(tutorialHint.text, summaryWidth - 32, float.PositiveInfinity).y + 8;
                tutorialHint.rectTransform.sizeDelta = new Vector2(summaryWidth - 32, tutorialHintHeight); y += tutorialHintHeight + 16;
                FightUI.Button(summary, "查看基础教程", 16, y, summaryWidth - 32, 44, () => OpenTutorial(detailEntry.Academy)); y += 64;
            }
            summary.sizeDelta = new Vector2(summaryWidth, y + 16);
            FightUI.Line(Body, 0, BodyHeight - 58, BodyWidth);
            if (detailOrigin == "picker")
            {
                FightUI.Button(Body, "返回", BodyWidth - 356, BodyHeight - 48, 144, 44, () => Navigate("picker"));
                FightUI.Button(Body, "选择这套卡组", BodyWidth - 196, BodyHeight - 48, 196, 44, () => CommitPickedDeck(path), true);
            }
            else if (detailEntry != null)
            {
                FightUI.Button(Body, "返回推荐", BodyWidth - 356, BodyHeight - 48, 144, 44, () => Navigate(detailOrigin));
                FightUI.Button(Body, "复制到我的  →", BodyWidth - 196, BodyHeight - 48, 196, 44, () => Try(() => OpenRecommendationCopy(detailEntry)), true);
            }
            else
            {
                ClientDeck personal = new ClientDeck { Name = name, Path = path, Group = Path.GetFileName(Path.GetDirectoryName(path)) };
                bool current = SameDeckPath(Runtime.Program.PATH_DECK + MDPro3.Config.GetConfigDeckName() + ".ydk", path);
                FightUI.Button(Body, "返回卡组", BodyWidth - 606, BodyHeight - 48, 144, 44, () => Navigate(detailOrigin));
                FightUI.Button(Body, current ? "当前使用" : "设为当前", BodyWidth - 446, BodyHeight - 48, 144, 44, () => Try(() => { SetCurrent(path); Rebuild(); }), false, !current);
                FightUI.Button(Body, "更多", BodyWidth - 286, BodyHeight - 48, 74, 44, () => DeckActions(personal));
                FightUI.Button(Body, "编辑卡组  →", BodyWidth - 196, BodyHeight - 48, 196, 44, () => OpenEditor(PersonalName(path)), true);
            }
        }

        private void OpenRecommendationCopy(MDPro3.Utility.DeckLibraryEntry entry)
        {
            var name = MDPro3.Utility.DeckLibrary.CreateEditableCopy(entry);
            OpenEditor(name);
            if (page == "editor" && editorRelativeName == name) ShowStatus("已复制到我的：" + FightText.Keep(name));
        }

        private void RenderDeckDetailInspector(int code)
        {
            if (detailInspector == null) return;
            if (detailInspectedCode == code && detailInspector.childCount > 0) return;
            detailInspectedCode = code;
            RenderClientCardInspector(detailInspector, code, detailCardFrames, detailCardReadingPositions,
                "deck-detail", "构筑暂无卡片", ShowCard);
        }

        private void RenderClientCardInspector(RectTransform panel, int code, List<Tuple<int, Outline>> frames,
            Dictionary<int, float> readingPositions, string focusPrefix, string emptyLabel, Action<int> readCard)
        {
            FightUI.Clear(panel);
            foreach (var frame in frames)
                if (frame.Item2 != null) { frame.Item2.enabled = frame.Item1 == code; frame.Item2.effectColor = FightUI.Active; }
            float width = panel.rect.width, height = panel.rect.height;
            if (code <= 0)
            {
                float emptyTop = Mathf.Max(24, (height - 104) / 2);
                FightUI.Icon(panel, "deck", (width - 40) / 2, emptyTop, 40, 40, FightUI.GoldBorder);
                FightUI.Text(panel, emptyLabel, 16, emptyTop + 64, width - 32, 40, 20, FightUI.Foreground, TextAlignmentOptions.Center);
                return;
            }
            float readingHeight = height - 62;
            var inspector = FightUI.Scroll(panel, 0, 0, width, readingHeight, readingHeight, out var readingScroll);
            MDPro3.Duel.YGOSharp.Card card = MDPro3.Duel.YGOSharp.CardsManager.Get(code);
            var name = FightUI.RawText(inspector, card.Name, 16, 14, width - 32, 100, Compact ? 21 : 25, FightUI.Foreground);
            float oneLineWidth = name.GetPreferredValues(card.Name, float.PositiveInfinity, float.PositiveInfinity).x;
            if (Compact && oneLineWidth > width - 32 && oneLineWidth <= (width - 32) * 1.12f)
                name.fontSize = Mathf.Max(19, name.fontSize * (width - 32) / oneLineWidth);
            name.enableWordWrapping = true; name.overflowMode = TextOverflowModes.Overflow;
            float nameHeight = name.GetPreferredValues(card.Name, width - 32, float.PositiveInfinity).y + 6;
            name.rectTransform.sizeDelta = new Vector2(width - 32, nameHeight);
            FightUI.RawHint(name.gameObject, card.Name);
            float artTop = 14 + nameHeight + 26;
            FightUI.Line(inspector, 16, artTop - 18, width - 32, FightUI.GoldBorder);
            float artWidth = Mathf.Min(width - 56, Compact ? 108 : 176), artHeight = artWidth * 1.46f;
            FightUI.Art(inspector, code, (width - artWidth) / 2, artTop, artWidth, artHeight, true);
            float y = artTop + artHeight + 18;
            string type = card.HasType(MDPro3.Duel.YGOSharp.CardType.Monster) ? card.GetAttributeString() + "  " + card.GetTypeForUI() : card.GetTypeForUI();
            var typeText = FightUI.Text(inspector, type, 16, y, width - 32, 64, 15, FightUI.Muted);
            float typeHeight = typeText.GetPreferredValues(typeText.text, width - 32, float.PositiveInfinity).y + 8;
            typeText.rectTransform.sizeDelta = new Vector2(width - 32, typeHeight); y += typeHeight;
            string parameters = EditorCardParameters(card);
            var parameterText = FightUI.Text(inspector, parameters, 16, y, width - 32, 80, 16, FightUI.Gold);
            float parametersHeight = parameterText.GetPreferredValues(parameterText.text, width - 32, float.PositiveInfinity).y + 10;
            parameterText.rectTransform.sizeDelta = new Vector2(width - 32, parametersHeight); y += parametersHeight + 8;
            FightUI.Line(inspector, 16, y - 4, width - 32);
            FightUI.Text(inspector, "卡片效果", 16, y + 4, width - 32, 28, 15, FightUI.Gold); y += 40;
            var text = FightUI.RawText(inspector, card.Desc, 16, y, width - 38, 800, Compact ? 16 : 18, FightUI.Foreground);
            text.lineSpacing = 5; text.paragraphSpacing = 8;
            text.overflowMode = TextOverflowModes.Overflow;
            float textHeight = text.GetPreferredValues(card.Desc, width - 38, float.PositiveInfinity).y + 12;
            text.rectTransform.sizeDelta = new Vector2(width - 38, textHeight);
            inspector.sizeDelta = new Vector2(width, Mathf.Max(y + textHeight + 16, readingHeight));
            FightUI.Line(panel, 12, height - 60, width - 24, FightUI.GoldBorder);
            var read = FightUI.FocusKey(FightUI.Button(panel, "阅读完整卡文", 12, height - 48, width - 24, 40, () => readCard(code)), focusPrefix + ":read-card");
            FightUI.Hint(read.gameObject, "放大查看当前卡片；每张卡片保留独立的阅读位置");
            Canvas.ForceUpdateCanvases();
            readingScroll.verticalNormalizedPosition = readingPositions.TryGetValue(code, out var position) ? position : 1;
            readingScroll.onValueChanged.AddListener(_ => readingPositions[code] = Mathf.Clamp01(readingScroll.verticalNormalizedPosition));
        }

        private void DrawDeckCards(RectTransform parent, MDPro3.Duel.YGOSharp.Deck deck, float width, Action<int> inspected = null, bool groupCopies = false)
        {
            float y = 16;
            int columns = Mathf.Max(4, Mathf.FloorToInt((width - 32) / (Compact ? 90 : 112)));
            float cardWidth = (width - 32 - (columns - 1) * 10) / columns, cardHeight = cardWidth * 1.46f;
            foreach (var part in new[] { Tuple.Create("主卡组", deck.Main), Tuple.Create("额外卡组", deck.Extra), Tuple.Create("副卡组", deck.Side) })
            {
                FightUI.Text(parent, part.Item1, 16, y, width - 100, 34, Compact ? 22 : 25, FightUI.Foreground);
                FightUI.Text(parent, part.Item2.Count + " 张", width - 94, y, 78, 34, 17, FightUI.Gold, TextAlignmentOptions.MidlineRight);
                FightUI.Line(parent, 16, y + 40, width - 32); y += 56;
                if (part.Item2.Count == 0)
                {
                    FightUI.Text(parent, "未配置" + part.Item1, 16, y + 4, width - 32, 42, 16, FightUI.Muted);
                    y += 68; continue;
                }
                var codes = groupCopies ? part.Item2.Distinct().ToList() : part.Item2;
                for (int i = 0; i < codes.Count; i++)
                {
                    int code = codes[i]; float x = 16 + i % columns * (cardWidth + 10), cy = y + i / columns * (cardHeight + 14);
                    var image = FightUI.Art(parent, code, x, cy, cardWidth, cardHeight, true);
                    var button = FightUI.CardButton(image, () => { if (inspected != null) inspected(code); else ShowCard(code); });
                    if (inspected != null) FightUI.PreviewOnFocus(button, () => inspected(code));
                    if (groupCopies && inspected != null) detailCardFrames.Add(Tuple.Create(code, image.transform.parent.GetComponent<Outline>()));
                    if (groupCopies)
                    {
                        int copies = part.Item2.Count(card => card == code);
                        var badge = FightUI.Panel(image.transform, cardWidth - 34, cardHeight - 28, 30, 24, FightUI.Background, false);
                        FightUI.Text(badge, "×" + copies, 0, 0, 30, 24, 14, FightUI.Active, TextAlignmentOptions.Center);
                    }
                }
                y += Mathf.Ceil((float)codes.Count / columns) * (cardHeight + 14) + 30;
            }
            parent.sizeDelta = new Vector2(width, y);
        }
        private void ShowCard(int code)
        {
            MDPro3.Duel.YGOSharp.Card card = MDPro3.Duel.YGOSharp.CardsManager.Get(code);
            float width = Mathf.Min(820, W - 48), height = Mathf.Min(700, H - 48);
            var box = ShowDialog("卡片详情", width, height);
            var name = FightUI.RawText(box, card.Name, 28, 84, width - 56, 100, 24, FightUI.Foreground);
            name.overflowMode = TextOverflowModes.Overflow; name.lineSpacing = 4;
            float nameHeight = name.GetPreferredValues(card.Name, width - 56, float.PositiveInfinity).y + 8;
            name.rectTransform.sizeDelta = new Vector2(width - 56, nameHeight);
            float top = 84 + nameHeight + 16, contentHeight = Mathf.Max(120, height - top - 28);
            FightUI.Line(box, 28, top - 8, width - 56, FightUI.GoldBorder);
            float factsWidth = Compact ? 204 : 228, effectX = factsWidth + 52, effectWidth = width - effectX - 28;
            var factsContent = FightUI.Scroll(box, 28, top, factsWidth, contentHeight, contentHeight);
            float artWidth = Mathf.Min(factsWidth - 16, Mathf.Max(92, contentHeight - 146) / 1.46f), artHeight = artWidth * 1.46f;
            FightUI.Art(factsContent, code, (factsWidth - artWidth) / 2, 0, artWidth, artHeight, true);
            string type = card.HasType(MDPro3.Duel.YGOSharp.CardType.Monster) ? card.GetAttributeString() + "  " + card.GetTypeForUI() : card.GetTypeForUI();
            var facts = FightUI.Text(factsContent, type + "\n" + EditorCardParameters(card), 4, artHeight + 18, factsWidth - 20, 130, 17, FightUI.Gold);
            facts.lineSpacing = 5; facts.overflowMode = TextOverflowModes.Overflow;
            float factsHeight = facts.GetPreferredValues(facts.text, factsWidth - 20, float.PositiveInfinity).y + 12;
            facts.rectTransform.sizeDelta = new Vector2(factsWidth - 20, factsHeight);
            factsContent.sizeDelta = new Vector2(factsWidth, Mathf.Max(contentHeight, artHeight + 34 + factsHeight));
            FightUI.Text(box, "卡片效果", effectX, top, effectWidth, 30, 17, FightUI.Gold);
            var c = FightUI.Scroll(box, effectX, top + 38, effectWidth, contentHeight - 38, contentHeight - 38, out var scroll);
            scroll.name = "Card detail reading";
            var text = FightUI.RawText(c, card.Desc, 0, 2, effectWidth - 22, 790, 20, FightUI.Foreground);
            text.lineSpacing = 7; text.paragraphSpacing = 10;
            text.overflowMode = TextOverflowModes.Overflow;
            float textHeight = text.GetPreferredValues(card.Desc, effectWidth - 22, float.PositiveInfinity).y + 12;
            text.rectTransform.sizeDelta = new Vector2(effectWidth - 22, textHeight);
            c.sizeDelta = new Vector2(effectWidth, Mathf.Max(contentHeight - 38, textHeight + 8));
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = cardReadingPositions.TryGetValue(code, out var saved) ? saved : 1;
            scroll.onValueChanged.AddListener(_ => cardReadingPositions[code] = Mathf.Clamp01(scroll.verticalNormalizedPosition));
        }
        private void CheckName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name == "." || name == ".." || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Contains("/") || name.Contains("\\")) throw new ArgumentException("请填写有效名称，不包含路径分隔符。");
        }
        private void CheckGroupName(string name)
        {
            CheckName(name);
            if (name == "全部" || name == "我的" || name == "默认分组")
                throw new ArgumentException("此名称用于系统分组，请使用其它分组名称。");
        }
        private void Prompt(string title, string label, string value, Action<string> accepted)
        {
            var box = ShowDialog(title, 620, 338); string text = value;
            FightUI.Text(box, label, 28, 86, 560, 32, 20, FightUI.Muted);
            var error = FightUI.Text(box, "", 28, 190, 560, 44, 15, FightUI.Danger);
            TMP_InputField field = null;
            field = FightUI.Input(box, value, label, 28, 128, 560, 48, v =>
            {
                text = v; error.text = "";
                if (field != null) field.GetComponent<Outline>().effectColor = FightUI.Border;
            });
            FightUI.Line(box, 28, 246, 560);
            FightUI.Button(box, "取消", 260, 266, 144, 48, CloseDialog);
            var originalModal = modal;
            FightUI.Button(box, title.StartsWith("新建", StringComparison.Ordinal) ? "创建" : title.StartsWith("复制", StringComparison.Ordinal) ? "复制" : "保存", 424, 266, 164, 48, () =>
            {
                try { accepted(text.Trim()); if (modal == originalModal) CloseDialog(); }
                catch (Exception failure)
                {
                    error.text = failure.Message; field.GetComponent<Outline>().effectColor = FightUI.Danger;
                    field.Select(); field.ActivateInputField();
                }
            }, true);
            field.Select(); field.ActivateInputField();
        }
        private void NewDeckDialog()
        {
            var box = ShowDialog("新建卡组", 620, 440); string name = "新卡组"; bool import = false;
            string group = deckGroup != "全部" && deckGroup != "默认分组" ? deckGroup : "";
            FightUI.Text(box, "名称", 28, 86, 260, 28, 19, FightUI.Muted);
            FightUI.Text(box, "保存到 " + (group.Length == 0 ? "默认分组" : FightText.Keep(group)), 292, 86, 296, 28, 15, FightUI.Muted, TextAlignmentOptions.TopRight);
            var error = FightUI.Text(box, "", 28, 284, 560, 56, 15, FightUI.Danger);
            TMP_InputField nameField = null;
            nameField = FightUI.Input(box, name, "卡组名称", 28, 122, 560, 48, v =>
            {
                name = v; error.text = "";
                if (nameField != null) nameField.GetComponent<Outline>().effectColor = FightUI.Border;
            });
            var importHost = FightUI.Node(box, "Import choice", 28, 190, 560, 48);
            Action renderImport = null;
            renderImport = () =>
            {
                FightUI.Clear(importHost);
                FightUI.SwitchRow(importHost, "从剪贴板导入 YDK / YDKE", import, 0, 0, 560, 48,
                    () => { import = !import; error.text = ""; renderImport(); });
            };
            renderImport();
            FightUI.Text(box, "关闭导入时创建空构筑；开启后使用剪贴板中的卡表。", 28, 246, 560, 28, 15, FightUI.Muted);
            FightUI.Line(box, 28, 352, 560);
            FightUI.Button(box, "取消", 192, 372, 144, 48, CloseDialog);
            FightUI.Button(box, "创建并编辑", 352, 372, 236, 48, () =>
            {
                bool nameAccepted = false;
                try
                {
                    name = name.Trim(); CheckName(name);
                    var relative = string.IsNullOrEmpty(group) ? name : group + "/" + name; var path = Runtime.Program.PATH_DECK + relative + ".ydk";
                    if (File.Exists(path)) throw new IOException("该名称已存在，请使用其它名称。");
                    nameAccepted = true;
                    MDPro3.Duel.YGOSharp.Deck deck = new MDPro3.Duel.YGOSharp.Deck();
                    if (import)
                    {
                        string source = GUIUtility.systemCopyBuffer;
                        if (source.Contains("ydke://")) deck = MDPro3.YdkeConverter.Ydke2Deck(source);
                        else if (source.Contains("ygotype=deck&v=1&d=")) deck = MDPro3.Net.DeckShareURL.UriToDeck(new Uri(source.Trim()));
                        else if (source.Contains("#main")) deck = new MDPro3.Duel.YGOSharp.Deck(source, "", "");
                        else throw new ArgumentException("剪贴板没有可识别的 YDK 或 YDKE 构筑。");
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, deck.GetYDK()); CloseDialog(); OpenEditor(relative);
                    if (page == "editor" && editorRelativeName == relative) ShowStatus((import ? "已导入个人卡组：" : "已创建个人卡组：") + FightText.Keep(relative));
                }
                catch (Exception failure)
                {
                    error.text = failure.Message;
                    if (!nameAccepted)
                    {
                        nameField.GetComponent<Outline>().effectColor = FightUI.Danger;
                        nameField.Select(); nameField.ActivateInputField();
                    }
                }
            }, true);
            nameField.Select(); nameField.ActivateInputField();
        }
        private void DeckActions(ClientDeck d)
        {
            var box = ShowDialog("卡组操作 · " + FightText.Keep(d.Name), 620, 530); int row = 0;
            Action<string, Action> add = (label, action) =>
            {
                var actionButton = FightUI.Button(box, label, 28, 86 + row++ * 56, 564, 48, () => { CloseDialog(); Try(action); });
                if (label.StartsWith("删除", StringComparison.Ordinal)) FightUI.StyleDestructiveButton(actionButton);
            };
            add("重命名", () => Prompt("重命名卡组", "新名称", Path.GetFileNameWithoutExtension(d.Path), name => { CheckName(name); MoveDeck(d.Path, Path.Combine(Path.GetDirectoryName(d.Path), name + ".ydk")); Rebuild(); }));
            add("复制独立副本", () => { var name = MDPro3.Utility.DeckLibrary.CreateEditableCopy(File.ReadAllBytes(d.Path), d.Name.Replace("/", " · ")); selectedPath = Path.GetFullPath(Runtime.Program.PATH_DECK + name + ".ydk"); Rebuild(); ShowStatus("已复制：" + FightText.Keep(name)); });
            add("移动到分组", () => ChooseGroup(d, false));
            add("复制到分组", () => ChooseGroup(d, true));
            add("复制 YDKE 卡组码", () => { GUIUtility.systemCopyBuffer = MDPro3.YdkeConverter.DeckToYdke(new MDPro3.Duel.YGOSharp.Deck(d.Path)); ShowStatus("YDKE 卡组码已复制"); });
            add("导出 YDK 文件", () => ExportDeck(d));
            add("删除卡组…", () => Confirm("删除卡组", "删除“" + FightText.Keep(d.Name) + "”？该操作会删除此独立文件。", () => { File.Delete(d.Path); UpdateDeckReferences(d.Path, null); selectedPath = null; Rebuild(); ShowStatus("已删除卡组"); }));
        }
        private void ChooseGroup(ClientDeck d, bool copy)
        {
            var groups = new[] { "默认分组" }.Concat(Directory.GetDirectories(Runtime.Program.PATH_DECK).Select(Path.GetFileName)).ToList();
            var box = ShowDialog(copy ? "复制到分组" : "移动到分组", 620, Mathf.Min(550, 144 + groups.Count * 56));
            var list = FightUI.Scroll(box, 24, 86, 570, Mathf.Min(410, groups.Count * 56), groups.Count * 56);
            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                var destinationDirectory = Path.GetFullPath(Path.Combine(Runtime.Program.PATH_DECK, group == "默认分组" ? "" : group)).TrimEnd(Path.DirectorySeparatorChar);
                bool currentGroup = string.Equals(Path.GetDirectoryName(Path.GetFullPath(d.Path)), destinationDirectory, StringComparison.OrdinalIgnoreCase);
                string caption = group == "默认分组" ? group : FightText.Keep(group);
                var choice = FightUI.Button(list, caption + (currentGroup ? " · 当前分组" : ""), 4, i * 56, 554, 48, () => Try(() =>
                {
                    var target = Path.Combine(Runtime.Program.PATH_DECK, group == "默认分组" ? "" : group, Path.GetFileName(d.Path));
                    if (File.Exists(target)) throw new IOException("目标分组已有同名文件。");
                    if (copy) File.Copy(d.Path, target); else MoveDeck(d.Path, target); CloseDialog(); Rebuild();
                }), false, !currentGroup);
                FightUI.Hint(choice.gameObject, caption + (currentGroup ? "\n卡组已在此分组中" : ""));
            }
        }
        private void MoveDeck(string source, string target)
        {
            if (File.Exists(target)) throw new IOException("目标名称已存在。");
            File.Move(source, target); UpdateDeckReferences(source, target); selectedPath = Path.GetFullPath(target);
        }
        private void UpdateDeckReferences(string before, string after)
        {
            var current = Runtime.Program.PATH_DECK + MDPro3.Config.GetConfigDeckName() + ".ydk";
            if (Path.GetFullPath(current) == Path.GetFullPath(before)) { MDPro3.Config.SetConfigDeck(after == null ? "" : PersonalName(after)); MDPro3.Config.Save(); }
            if (cpuDeck != null && Path.GetFullPath(Runtime.Program.PATH_DECK + cpuDeck + ".ydk") == Path.GetFullPath(before)) cpuDeck = after == null ? "" : PersonalName(after);
            MDPro3.AI.ResponsesAiConfig config = MDPro3.AI.ResponsesAiConfigStore.Load();
            if (SameDeckPath(config.DeckA, before)) config.DeckA = after ?? "";
            if (SameDeckPath(config.DeckB, before)) config.DeckB = after ?? "";
            MDPro3.AI.ResponsesAiConfigStore.SaveChoices(config.DeckA, config.DeckB, config.AiVsAi);
            if (SameDeckPath(battle.DeckA, before)) battle.DeckA = after ?? ""; if (SameDeckPath(battle.DeckB, before)) battle.DeckB = after ?? "";
            if (!string.IsNullOrEmpty(cpuCustomDeck) && SameDeckPath(Runtime.Program.PATH_DECK + cpuCustomDeck + ".ydk", before)) cpuCustomDeck = after == null ? "" : PersonalName(after);
        }
        private bool SameDeckPath(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            try { return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }
        private void ExportDeck(ClientDeck d)
        {
            string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Path.GetFileName(d.Path));
            Prompt("导出 YDK", "保存到完整文件路径", defaultPath, path =>
            {
                var target = Path.GetFullPath(path);
                Action save = () => { Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(d.Path, target, true); ShowStatus("已导出到 " + FightText.Keep(target)); };
                if (File.Exists(target)) Confirm("覆盖导出文件", "目标文件已存在，是否覆盖？", save); else save();
            });
        }
        private void ManageGroup()
        {
            string group = deckGroup, directory = Path.Combine(Runtime.Program.PATH_DECK, group);
            if (group == "默认分组" || group == "全部" || !Directory.Exists(directory)) return;
            var box = ShowDialog("管理分组 · " + FightText.Keep(group), 620, 360);
            FightUI.Button(box, "重命名分组", 28, 88, 564, 48, () => Prompt("重命名分组", "新名称", group, name =>
            {
                CheckGroupName(name); var target = Path.Combine(Runtime.Program.PATH_DECK, name); if (Directory.Exists(target)) throw new IOException("该分组已经存在。");
                var files = Directory.GetFiles(directory, "*.ydk"); Directory.Move(directory, target);
                foreach (var file in files) UpdateDeckReferences(file, Path.Combine(target, Path.GetFileName(file)));
                deckGroup = name; Rebuild();
            }));
            FightUI.Button(box, "复制分组", 28, 152, 564, 48, () => Prompt("复制分组", "新分组名称", group + " 副本", name =>
            {
                CheckGroupName(name); var target = Path.Combine(Runtime.Program.PATH_DECK, name); if (Directory.Exists(target)) throw new IOException("该分组已经存在。");
                Directory.CreateDirectory(target); foreach (var file in Directory.GetFiles(directory, "*.ydk")) File.Copy(file, Path.Combine(target, Path.GetFileName(file)), false);
                deckGroup = name; Rebuild();
            }));
            var decks = Directory.GetFiles(directory, "*.ydk");
            var deleteGroup = FightUI.Button(box, "删除分组…", 28, 216, 564, 48, () => Confirm("删除分组", "删除分组“" + FightText.Keep(group) + "”及其中 " + decks.Length + " 套卡组？", () =>
            {
                foreach (var file in decks) { File.Delete(file); UpdateDeckReferences(file, null); }
                if (!Directory.EnumerateFileSystemEntries(directory).Any()) Directory.Delete(directory);
                deckGroup = "全部"; Rebuild(); ShowStatus("已删除分组中的卡组；其它文件保持原样。");
            }));
            FightUI.StyleDestructiveButton(deleteGroup);
        }
    }
}
