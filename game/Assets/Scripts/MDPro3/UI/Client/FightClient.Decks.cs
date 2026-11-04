using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MDPro3.AI;
using MDPro3.Net;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using MDPro3.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI.Client
{
    internal partial class FightClient
    {
        private sealed class ClientDeck
        {
            internal string Name, Path, Group;
            internal DeckLibraryEntry Recommendation;
        }
        private List<ClientDeck> deckItems;
        private RectTransform deckGrid, deckPreview;
        private string deckGroup = "全部";
        private bool pickerPersonalOnly;
        private DeckLibraryEntry detailEntry;
        private string personalDetailPath;
        private string detailOrigin = "recommended";
        private int deckGridColumns;
        private float deckGridWidth, deckGridHeight;
        private RectTransform detailInspector;
        private int detailInspectedCode;
        private readonly HashSet<string> deckTagGroups = new HashSet<string>();
        private readonly List<Tuple<int, Outline>> detailCardFrames = new List<Tuple<int, Outline>>();
        private readonly List<Tuple<ClientDeck, RectTransform>> deckBrowseFrames = new List<Tuple<ClientDeck, RectTransform>>();

        private string[] DeckBrowseTags(ClientDeck deck)
        {
            var entry = deck.Recommendation;
            if (entry == null) return Array.Empty<string>();
            return entry.Tags.Length > 0 ? entry.Tags : entry.Academy?.tags ?? Array.Empty<string>();
        }

        private bool DeckMatchesGroup(ClientDeck deck, string group)
            => group == "全部" || (deckTagGroups.Contains(group) ? DeckBrowseTags(deck).Contains(group) : deck.Group == group);

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
            row.GetComponent<Outline>().enabled = false;
            if (active) FightUI.Panel(row, 0, 8, 2, 32, FightUI.Active, false);
            FightUI.Text(row, group == "全部" ? "全部卡组" : group, 14, 0, width - 56, 48, Compact ? 17 : 19,
                active ? FightUI.Active : FightUI.Muted, TextAlignmentOptions.MidlineLeft);
            FightUI.Text(row, count.ToString(), width - 40, 0, 28, 48, 15,
                active ? FightUI.Gold : FightUI.Muted, TextAlignmentOptions.MidlineRight);
            var button = row.gameObject.AddComponent<Button>(); button.targetGraphic = row.GetComponent<Image>();
            var frame = row.GetComponentInChildren<FightFrame>(); frame.ButtonFrame = true;
            row.gameObject.AddComponent<FightButtonFeedback>().Bind(frame);
            FightUI.Hint(row.gameObject, group == "全部" ? "全部卡组" : group);
            button.onClick.AddListener(() => { deckGroup = group; Rebuild(); });
        }

        private void DeckCountStrip(Transform parent, Deck deck, float x, float y, float width, float height = 56)
        {
            string[] labels = { "主卡组", "额外", "副卡组" };
            int[] values = { deck.Main.Count, deck.Extra.Count, deck.Side.Count };
            float cellWidth = width / 3;
            for (int i = 0; i < 3; i++)
            {
                if (i > 0) FightUI.Panel(parent, x + i * cellWidth, y + 8, 1, height - 16, FightUI.Border, false);
                FightUI.Text(parent, values[i].ToString(), x + i * cellWidth + 4, y, cellWidth - 8, 30, 24,
                    FightUI.Foreground, TextAlignmentOptions.Center);
                FightUI.Text(parent, labels[i], x + i * cellWidth + 4, y + 32, cellWidth - 8, 24, 14,
                    FightUI.Muted, TextAlignmentOptions.Center);
            }
        }

        private List<ClientDeck> PersonalDecks()
        {
            var list = new List<ClientDeck>();
            if (!Directory.Exists(Program.PATH_DECK)) return list;
            foreach (var dir in new[] { Program.PATH_DECK }.Concat(Directory.GetDirectories(Program.PATH_DECK).OrderBy(Path.GetFileName)))
                foreach (var path in Directory.GetFiles(dir, "*.ydk").OrderBy(Path.GetFileName))
                {
                    if (DeckLibrary.IsUntouchedRecommendation(path, recommendations)) continue;
                    list.Add(new ClientDeck { Name = PersonalName(path), Path = Path.GetFullPath(path), Group = Path.GetFullPath(dir) == Path.GetFullPath(Program.PATH_DECK) ? "默认分组" : Path.GetFileName(dir) });
                }
            return list;
        }
        private string PersonalName(string path) => Path.GetFullPath(path).Substring(Path.GetFullPath(Program.PATH_DECK).TrimEnd(Path.DirectorySeparatorChar).Length + 1).Replace(".ydk", "");
        private void PickDeck(string slot, string origin, Action<string> commit, bool personalOnly = false)
        {
            pickerSlot = slot; pickerOrigin = origin; pickerCommit = commit; pickerPersonalOnly = personalOnly; Navigate("picker");
        }
        private void SetCurrent(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) throw new FileNotFoundException("卡组文件不存在。");
            Config.SetConfigDeck(PersonalName(path)); Config.Save(); ShowStatus("已设为当前卡组：" + PersonalName(path));
            cpuDeck = PersonalName(path);
        }
        private void RenderDecks()
        {
            bool picker = page == "picker", mine = page == "decks";
            if (mine || (picker && pickerPersonalOnly)) deckItems = PersonalDecks();
            else if (picker)
                deckItems = ResponsesDeckLibrary.Load(Program.PATH_DECK).Choices.Select(c => new ClientDeck { Name = c.Name, Path = c.Path, Group = c.Name.StartsWith("我的") ? "我的" : c.Name.StartsWith("经典") ? "经典" : "竞技", Recommendation = recommendations.FirstOrDefault(e => e.SourcePath == c.Path) }).ToList();
            else deckItems = recommendations.Where(e => e.Page == (page == "competitive" ? DeckLibraryPage.Competitive : DeckLibraryPage.Classic)).Select(e => new ClientDeck { Name = e.Name, Path = e.SourcePath, Group = e.IsCurated ? "精选构筑" : "教学构筑", Recommendation = e }).ToList();
            PageTitle(picker ? "选择卡组 · " + pickerSlot : mine ? "我的卡组" : page == "competitive" ? "推荐 · 竞技构筑" : "推荐 · 经典构筑");
            if (!picker)
            {
                DeckTab(Body, "我的", BodyWidth - 440, 0, 112, mine, () => Navigate("decks"));
                DeckTab(Body, "推荐 · 经典", BodyWidth - 312, 0, 148, page == "recommended", () => Navigate("recommended"));
                DeckTab(Body, "推荐 · 竞技", BodyWidth - 148, 0, 148, page == "competitive", () => Navigate("competitive"));
            }
            float groupsW = Compact ? 150 : 200, previewW = Compact ? 294 : 360, gridX = groupsW + Gap;
            deckGridWidth = BodyWidth - groupsW - previewW - Gap * 2; deckGridHeight = BodyHeight - 154;
            Button clearSearch = null;
            var searchField = FightUI.Input(Body, search, mine ? "搜索我的卡组" : "搜索主题、版本或构筑标签", gridX, 94, deckGridWidth, 48,
                v => { search = v; if (clearSearch != null) clearSearch.interactable = v.Length > 0; RenderDeckGrid(); });
            searchField.textViewport.offsetMax = new Vector2(searchField.textViewport.offsetMax.x - 40, searchField.textViewport.offsetMax.y);
            clearSearch = FightUI.Button(Body, "清空搜索", gridX + deckGridWidth - 44, 96, 40, 44, () => searchField.text = "", false, search.Length > 0);
            clearSearch.GetComponent<Image>().color = Color.clear; clearSearch.GetComponent<Outline>().enabled = false;
            clearSearch.GetComponentInChildren<TextMeshProUGUI>().gameObject.SetActive(false);
            FightUI.Icon(clearSearch.transform, "close", 12, 14, 16, 16, FightUI.Muted);
            var groups = FightUI.Panel(Body, 0, 94, groupsW, BodyHeight - 94, FightUI.PanelColor, false);
            var groupNames = new[] { "全部" }.Concat(deckItems.Select(d => d.Group).Distinct()).ToList();
            deckTagGroups.Clear();
            if (!mine && !picker)
            {
                var tags = deckItems.SelectMany(d => DeckBrowseTags(d).Distinct()).Where(tag => !string.IsNullOrEmpty(tag) && tag.Any(c => c >= '\u4e00' && c <= '\u9fff'))
                    .GroupBy(tag => tag).Where(group => group.Count() > 1 && group.Count() < deckItems.Count)
                    .OrderByDescending(group => group.Count()).ThenBy(group => group.Key).Take(12).Select(group => group.Key).ToList();
                if (tags.Count > 0) { foreach (var tag in tags) deckTagGroups.Add(tag); groupNames = new[] { "全部" }.Concat(tags).ToList(); }
            }
            ModuleDivider(groups, mine ? "我的分组" : deckTagGroups.Count > 0 ? "战术标签" : "构筑来源", 14, 14, groupsW - 28, 18);
            if (mine && Directory.Exists(Program.PATH_DECK))
                groupNames = groupNames.Concat(Directory.GetDirectories(Program.PATH_DECK).Select(Path.GetFileName)).Distinct().ToList();
            if (!groupNames.Contains(deckGroup)) deckGroup = "全部";
            float firstGroupActionY = BodyHeight - 292;
            float groupListHeight = mine ? firstGroupActionY - 80 : BodyHeight - 184;
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
                FightUI.TextAction(groups, "+ 新建分组", 16, BodyHeight - 238, groupsW - 32, 40, () => Prompt("新建分组", "分组名称", "", name => { CheckName(name); Directory.CreateDirectory(Path.Combine(Program.PATH_DECK, name)); Rebuild(); }));
                FightUI.TextAction(groups, "管理分组", 16, BodyHeight - 190, groupsW - 32, 40, ManageGroup, deckGroup != "全部" && deckGroup != "默认分组");
            }
            deckGrid = FightUI.Node(Body, "Deck Grid", gridX, 154, deckGridWidth, deckGridHeight);
            deckPreview = FightUI.Panel(Body, BodyWidth - previewW, 94, previewW, BodyHeight - 94, FightUI.PanelColor, false);
            FightUI.Line(deckPreview, 0, 0, previewW, FightUI.GoldBorder);
            deckGridColumns = Compact ? 3 : LargeViewport ? 6 : 4;
            RenderDeckGrid();
        }
        private void RenderDeckGrid(bool retainScroll = false)
        {
            if (deckGrid == null) return;
            var oldScroll = deckGrid.GetComponentInChildren<ScrollRect>();
            float scrollPosition = retainScroll && oldScroll != null ? oldScroll.verticalNormalizedPosition : 1;
            FightUI.Clear(deckGrid); deckBrowseFrames.Clear();
            var terms = search.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var items = deckItems.Where(d => DeckMatchesGroup(d, deckGroup) && (d.Recommendation != null ? DeckLibrary.Matches(d.Recommendation, search) : terms.All(t => d.Name.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0))).ToList();
            if (page != "picker" && items.Count > 0 && !items.Any(d => d.Path == selectedPath))
            {
                var currentPath = Program.PATH_DECK + Config.GetConfigDeckName() + ".ydk";
                selectedPath = (items.FirstOrDefault(d => SameDeckPath(currentPath, d.Path)) ?? items[0]).Path;
            }
            float cellW = (deckGridWidth - 16 * (deckGridColumns - 1)) / deckGridColumns;
            float coverHeight = cellW * .75f, cellH = coverHeight + 148;
            var scroll = FightUI.Scroll(deckGrid, 0, 0, deckGridWidth, deckGridHeight, Mathf.Ceil((float)items.Count / deckGridColumns) * (cellH + 16), out var gridScroll);
            if (items.Count == 0)
            {
                bool emptyLibrary = page == "decks" && deckItems.Count == 0;
                float middle = Mathf.Max(20, (deckGridHeight - 300) / 2);
                var illustration = FightUI.Asset(scroll, "decks.png", (deckGridWidth - 220) / 2, middle, 220, 108);
                illustration.color = new Color(.65f, .65f, .65f, .8f);
                FightUI.Text(scroll, emptyLibrary ? "你的第一套构筑，从这里开始" : "没有找到匹配的构筑", 20, middle + 128, deckGridWidth - 40, 40, Compact ? 23 : 27, FightUI.Foreground, TextAlignmentOptions.Center);
                FightUI.Text(scroll, emptyLibrary ? "从推荐模板开始，或自由创建你的卡组。" : "调整关键词或切换分组，再试一次。", 20, middle + 178, deckGridWidth - 40, 40, 17, FightUI.Muted, TextAlignmentOptions.Center);
                float actionWidth = Mathf.Min(180, (deckGridWidth - 48) / 2);
                if (emptyLibrary)
                {
                    FightUI.Button(scroll, "创建卡组", (deckGridWidth - actionWidth * 2 - 16) / 2, middle + 232, actionWidth, 48, NewDeckDialog, true);
                    FightUI.Button(scroll, "浏览推荐", (deckGridWidth + 16) / 2, middle + 232, actionWidth, 48, () => Navigate("recommended"));
                }
                else FightUI.Button(scroll, "清除搜索与分组", (deckGridWidth - 210) / 2, middle + 232, 210, 48, () => { search = ""; deckGroup = "全部"; Rebuild(); });
            }
            for (int i = 0; i < items.Count; i++)
            {
                var d = items[i]; bool selected = selectedPath == d.Path;
                var card = FightUI.Panel(scroll, (i % deckGridColumns) * (cellW + 16), (i / deckGridColumns) * (cellH + 16), cellW, cellH,
                    selected ? FightUI.Raised : FightUI.PanelColor, true, "Deck " + d.Name);
                var outline = card.GetComponent<Outline>(); outline.effectColor = selected ? FightUI.Active : FightUI.Border;
                deckBrowseFrames.Add(Tuple.Create(d, card));
                try
                {
                    var deck = new Deck(d.Path);
                    FightUI.Art(card, d.Recommendation?.CoverCard ?? DeckLibrary.CoverFor(deck), 5, 5, cellW - 10, coverHeight);
                    bool current = SameDeckPath(Program.PATH_DECK + Config.GetConfigDeckName() + ".ydk", d.Path);
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
                    var title = FightUI.Text(card, d.Name, 12, coverHeight + 17, cellW - 24, 66, Compact ? 21 : 23, FightUI.Foreground);
                    title.enableWordWrapping = true; title.enableAutoSizing = true;
                    title.fontSizeMin = Compact ? 18 : 20; title.fontSizeMax = Compact ? 21 : 23;
                    FightUI.Hint(card.gameObject, d.Name);
                    FightUI.Text(card, d.Recommendation?.Variant ?? d.Group, 12, coverHeight + 86, cellW - 24, 22, 14, FightUI.Muted);
                    FightUI.Line(card, 12, coverHeight + 111, cellW - 24, FightUI.Border);
                    FightUI.Text(card, "主 " + deck.Main.Count + "   额外 " + deck.Extra.Count + "   副 " + deck.Side.Count,
                        12, coverHeight + 118, cellW - 24, 24, 14, FightUI.Muted);
                }
                catch (Exception e) { FightUI.Text(card, d.Name + "\n" + e.Message, 12, 12, cellW - 24, cellH - 24, 18, FightUI.Danger); }
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
            }
            RenderDeckPreview(deck);
        }

        private void RenderDeckPreview(ClientDeck d)
        {
            FightUI.Clear(deckPreview); float w = deckPreview.rect.width, h = deckPreview.rect.height;
            ModuleDivider(deckPreview, "构筑预览", 18, 16, w - 36, 20);
            if (d == null)
            {
                float emptyTop = Mathf.Max(76, ((page == "picker" ? h - 96 : h) - 170) / 2);
                FightUI.Icon(deckPreview, "deck", (w - 48) / 2, emptyTop, 48, 48, FightUI.GoldBorder);
                FightUI.Text(deckPreview, "选择一套卡组", 20, emptyTop + 72, w - 40, 44, 23, FightUI.Foreground, TextAlignmentOptions.Center);
                if (page == "picker")
                {
                    FightUI.Button(deckPreview, "返回", 18, h - 64, w - 36, 48, () => Navigate(pickerOrigin));
                }
                return;
            }
            Try(() =>
            {
                var deck = new Deck(d.Path);
                float actionsY = page != "picker" && d.Recommendation != null ? h - 16 : h - 138;
                FightUI.Line(deckPreview, 18, actionsY, w - 36, FightUI.GoldBorder);
                float countsY = actionsY - 72;
                DeckCountStrip(deckPreview, deck, 18, countsY + 4, w - 36);
                float previewHeight = Mathf.Max(100, countsY - 82);
                var c = FightUI.Scroll(deckPreview, 8, 70, w - 16, previewHeight, 500);
                float artHeight = Compact ? 64 : 176;
                FightUI.Art(c, d.Recommendation?.CoverCard ?? DeckLibrary.CoverFor(deck), 10, 0, w - 36, artHeight);
                var title = FightUI.Text(c, d.Name, 10, artHeight + (Compact ? 10 : 12), w - 36, 96, Compact ? 23 : 27, FightUI.Foreground);
                title.enableWordWrapping = true; title.enableAutoSizing = true;
                title.fontSizeMin = Compact ? 20 : 23; title.fontSizeMax = Compact ? 23 : 27;
                float titleHeight = Mathf.Clamp(title.GetPreferredValues(d.Name, w - 36, float.PositiveInfinity).y + 6, 34, 92);
                title.rectTransform.sizeDelta = new Vector2(w - 36, titleHeight);
                FightUI.Hint(title.gameObject, d.Name);
                float y = artHeight + (Compact ? 14 : 18) + titleHeight;
                FightUI.Text(c, d.Recommendation != null ? "推荐模板 · 只读" : "我的卡组 · " + d.Group, 10, y, w - 36, 24, 15, FightUI.Muted);
                y += Compact ? 28 : 44;
                // Keep a section heading whole at the initial viewport edge, including long names.
                if (y < previewHeight && y + 40 > previewHeight) y = previewHeight + 12;
                if (!string.IsNullOrWhiteSpace(d.Recommendation?.Description))
                {
                    ModuleDivider(c, "构筑思路", 10, y, w - 36, 18);
                    var description = FightUI.Text(c, d.Recommendation.Description, 10, y + 48, w - 36, 180, 17, FightUI.Muted);
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
                    FightUI.Button(deckPreview, "返回", 18, h - 120, w - 36, 44, () => Navigate(pickerOrigin));
                    FightUI.Button(deckPreview, "查看完整构筑", 18, h - 64, w - 36, 48, () => OpenDeckDetail(d));
                }
                else if (d.Recommendation == null)
                {
                    bool current = SameDeckPath(Program.PATH_DECK + Config.GetConfigDeckName() + ".ydk", d.Path);
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
            PageTitle(name);
            var deck = new Deck(path);
            int cover = detailEntry?.CoverCard ?? DeckLibrary.CoverFor(deck);
            float inspectorWidth = Compact ? 218 : LargeViewport ? 352 : 280, summaryWidth = Compact ? 254 : LargeViewport ? 408 : 336;
            float cardsWidth = BodyWidth - inspectorWidth - summaryWidth - Gap * 2;
            float top = 94, contentHeight = BodyHeight - top - 70;
            detailInspector = FightUI.Panel(Body, 0, top, inspectorWidth, contentHeight, FightUI.PanelColor, false);
            detailCardFrames.Clear();
            var cards = FightUI.Scroll(Body, inspectorWidth + Gap, top, cardsWidth, contentHeight, 1200);
            DrawDeckCards(cards, deck, cardsWidth, RenderDeckDetailInspector, true);
            int initialCard = deck.Main.Contains(cover) || deck.Extra.Contains(cover) ? cover : deck.Main.FirstOrDefault();
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
            string source = detailEntry != null ? "推荐 · " + (detailEntry.Page == DeckLibraryPage.Classic ? "经典" : "竞技") + " · 只读模板"
                : "我的卡组 · " + (Path.GetDirectoryName(Path.GetFullPath(path)) == Path.GetFullPath(Program.PATH_DECK).TrimEnd(Path.DirectorySeparatorChar)
                    ? "默认分组" : Path.GetFileName(Path.GetDirectoryName(path)));
            FightUI.Text(summary, source, 16, y, summaryWidth - 32, 30, 15, FightUI.Muted); y += 46;
            if (!string.IsNullOrWhiteSpace(detailEntry?.Description))
            {
                ModuleDivider(summary, "构筑思路", 16, y, summaryWidth - 32, 21); y += 52;
                string description = detailEntry.Description;
                var descriptionText = FightUI.Text(summary, description, 16, y, summaryWidth - 32, 360, 18, FightUI.Foreground);
                descriptionText.overflowMode = TextOverflowModes.Overflow;
                float descriptionHeight = descriptionText.GetPreferredValues(description, summaryWidth - 32, float.PositiveInfinity).y + 12;
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
                var personal = new ClientDeck { Name = name, Path = path, Group = Path.GetFileName(Path.GetDirectoryName(path)) };
                bool current = SameDeckPath(Program.PATH_DECK + Config.GetConfigDeckName() + ".ydk", path);
                FightUI.Button(Body, "返回卡组", BodyWidth - 606, BodyHeight - 48, 144, 44, () => Navigate(detailOrigin));
                FightUI.Button(Body, current ? "当前使用" : "设为当前", BodyWidth - 446, BodyHeight - 48, 144, 44, () => Try(() => { SetCurrent(path); Rebuild(); }), false, !current);
                FightUI.Button(Body, "更多", BodyWidth - 286, BodyHeight - 48, 74, 44, () => DeckActions(personal));
                FightUI.Button(Body, "编辑卡组  →", BodyWidth - 196, BodyHeight - 48, 196, 44, () => OpenEditor(PersonalName(path)), true);
            }
        }

        private void OpenRecommendationCopy(DeckLibraryEntry entry)
        {
            var name = DeckLibrary.CreateEditableCopy(entry);
            OpenEditor(name);
            if (page == "editor" && editorRelativeName == name) ShowStatus("已复制到我的：" + name);
        }

        private void RenderDeckDetailInspector(int code)
        {
            if (detailInspector == null) return;
            if (detailInspectedCode == code && detailInspector.childCount > 0) return;
            FightUI.Clear(detailInspector); detailInspectedCode = code;
            foreach (var frame in detailCardFrames)
                if (frame.Item2 != null) { frame.Item2.enabled = frame.Item1 == code; frame.Item2.effectColor = FightUI.Active; }
            float width = detailInspector.rect.width, height = detailInspector.rect.height;
            if (code <= 0)
            {
                FightUI.Text(detailInspector, "暂无卡片", 16, 24, width - 32, 70, 18, FightUI.Muted); return;
            }
            var inspector = FightUI.Scroll(detailInspector, 0, 0, width, height, height);
            var card = CardsManager.Get(code);
            var name = FightUI.Text(inspector, card.Name, 16, 14, width - 32, 100, Compact ? 21 : 25, FightUI.Foreground);
            float oneLineWidth = name.GetPreferredValues(card.Name, float.PositiveInfinity, float.PositiveInfinity).x;
            if (Compact && oneLineWidth > width - 32 && oneLineWidth <= (width - 32) * 1.12f)
                name.fontSize = Mathf.Max(19, name.fontSize * (width - 32) / oneLineWidth);
            name.enableWordWrapping = true; name.overflowMode = TextOverflowModes.Overflow;
            float nameHeight = name.GetPreferredValues(card.Name, width - 32, float.PositiveInfinity).y + 6;
            name.rectTransform.sizeDelta = new Vector2(width - 32, nameHeight);
            FightUI.Hint(name.gameObject, card.Name);
            float artTop = 14 + nameHeight + 26;
            FightUI.Line(inspector, 16, artTop - 18, width - 32, FightUI.GoldBorder);
            float artWidth = Mathf.Min(width - 56, Compact ? 108 : 176), artHeight = artWidth * 1.46f;
            FightUI.Art(inspector, code, (width - artWidth) / 2, artTop, artWidth, artHeight, true);
            float y = artTop + artHeight + 18;
            string type = card.HasType(CardType.Monster) ? card.GetAttributeString() + "  " + card.GetTypeForUI() : card.GetTypeForUI();
            var typeText = FightUI.Text(inspector, type, 16, y, width - 32, 64, 15, FightUI.Muted);
            float typeHeight = typeText.GetPreferredValues(type, width - 32, float.PositiveInfinity).y + 8;
            typeText.rectTransform.sizeDelta = new Vector2(width - 32, typeHeight); y += typeHeight;
            string parameters = EditorCardParameters(card);
            var parameterText = FightUI.Text(inspector, parameters, 16, y, width - 32, 80, 16, FightUI.Gold);
            float parametersHeight = parameterText.GetPreferredValues(parameters, width - 32, float.PositiveInfinity).y + 10;
            parameterText.rectTransform.sizeDelta = new Vector2(width - 32, parametersHeight); y += parametersHeight + 8;
            FightUI.Line(inspector, 16, y - 4, width - 32);
            var text = FightUI.Text(inspector, card.Desc, 16, y, width - 38, 800, Compact ? 16 : 18, FightUI.Foreground);
            text.overflowMode = TextOverflowModes.Overflow;
            float textHeight = text.GetPreferredValues(card.Desc, width - 38, float.PositiveInfinity).y + 12;
            text.rectTransform.sizeDelta = new Vector2(width - 38, textHeight);
            inspector.sizeDelta = new Vector2(width, Mathf.Max(y + textHeight + 16, height));
        }

        private void DrawDeckCards(RectTransform parent, Deck deck, float width, Action<int> inspected = null, bool groupCopies = false)
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
            var card = CardsManager.Get(code); var box = ShowDialog(card.Name, 660, 570);
            FightUI.Art(box, code, 28, 90, 220, 320, true);
            var c = FightUI.Scroll(box, 274, 90, 354, 438, 438);
            string type = card.HasType(CardType.Monster) ? card.GetAttributeString() + "  " + card.GetTypeForUI() : card.GetTypeForUI();
            var facts = FightUI.Text(c, type + "\n" + EditorCardParameters(card), 12, 0, 322, 130, 17, FightUI.Gold);
            float factsHeight = facts.GetPreferredValues(facts.text, 322, float.PositiveInfinity).y + 20;
            facts.rectTransform.sizeDelta = new Vector2(322, factsHeight);
            FightUI.Line(c, 12, factsHeight, 322);
            var text = FightUI.Text(c, card.Desc, 12, factsHeight + 20, 322, 790, 18, FightUI.Foreground);
            text.overflowMode = TextOverflowModes.Overflow;
            float textHeight = text.GetPreferredValues(card.Desc, 322, float.PositiveInfinity).y + 12;
            text.rectTransform.sizeDelta = new Vector2(322, textHeight);
            c.sizeDelta = new Vector2(354, Mathf.Max(438, factsHeight + 32 + textHeight));
        }
        private void CheckName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name == "." || name == ".." || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Contains("/") || name.Contains("\\")) throw new ArgumentException("请填写有效名称，不包含路径分隔符。");
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
            FightUI.Text(box, "保存到 " + (group.Length == 0 ? "默认分组" : group), 292, 86, 296, 28, 15, FightUI.Muted, TextAlignmentOptions.TopRight);
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
                    var relative = string.IsNullOrEmpty(group) ? name : group + "/" + name; var path = Program.PATH_DECK + relative + ".ydk";
                    if (File.Exists(path)) throw new IOException("该名称已存在，请使用其它名称。");
                    nameAccepted = true;
                    Deck deck = new Deck();
                    if (import)
                    {
                        string source = GUIUtility.systemCopyBuffer;
                        if (source.Contains("ydke://")) deck = YdkeConverter.Ydke2Deck(source);
                        else if (source.Contains("ygotype=deck&v=1&d=")) deck = DeckShareURL.UriToDeck(new Uri(source.Trim()));
                        else if (source.Contains("#main")) deck = new Deck(source, "", "");
                        else throw new ArgumentException("剪贴板没有可识别的 YDK 或 YDKE 构筑。");
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, deck.GetYDK()); CloseDialog(); OpenEditor(relative);
                    if (page == "editor" && editorRelativeName == relative) ShowStatus((import ? "已导入个人卡组：" : "已创建个人卡组：") + relative);
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
            var box = ShowDialog("卡组操作 · " + d.Name, 620, 530); int row = 0;
            Action<string, Action> add = (label, action) => { FightUI.Button(box, label, 28, 86 + row++ * 56, 564, 48, () => { CloseDialog(); Try(action); }); };
            add("重命名", () => Prompt("重命名卡组", "新名称", Path.GetFileNameWithoutExtension(d.Path), name => { CheckName(name); MoveDeck(d.Path, Path.Combine(Path.GetDirectoryName(d.Path), name + ".ydk")); Rebuild(); }));
            add("复制独立副本", () => { var name = DeckLibrary.CreateEditableCopy(File.ReadAllBytes(d.Path), d.Name.Replace("/", " · ")); selectedPath = Path.GetFullPath(Program.PATH_DECK + name + ".ydk"); Rebuild(); ShowStatus("已复制：" + name); });
            add("移动到分组", () => ChooseGroup(d, false));
            add("复制到分组", () => ChooseGroup(d, true));
            add("复制 YDKE 卡组码", () => { GUIUtility.systemCopyBuffer = YdkeConverter.DeckToYdke(new Deck(d.Path)); ShowStatus("YDKE 卡组码已复制"); });
            add("导出 YDK 文件", () => ExportDeck(d));
            add("删除卡组…", () => Confirm("删除卡组", "删除“" + d.Name + "”？该操作会删除此独立文件。", () => { File.Delete(d.Path); UpdateDeckReferences(d.Path, null); selectedPath = null; Rebuild(); ShowStatus("已删除卡组"); }));
        }
        private void ChooseGroup(ClientDeck d, bool copy)
        {
            var groups = new[] { "默认分组" }.Concat(Directory.GetDirectories(Program.PATH_DECK).Select(Path.GetFileName)).ToList();
            var box = ShowDialog(copy ? "复制到分组" : "移动到分组", 620, Mathf.Min(550, 144 + groups.Count * 56));
            var list = FightUI.Scroll(box, 24, 86, 570, Mathf.Min(410, groups.Count * 56), groups.Count * 56);
            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i]; FightUI.Button(list, group, 4, i * 56, 554, 48, () => Try(() =>
                {
                    var target = Path.Combine(Program.PATH_DECK, group == "默认分组" ? "" : group, Path.GetFileName(d.Path));
                    if (File.Exists(target)) throw new IOException("目标分组已有同名文件。");
                    if (copy) File.Copy(d.Path, target); else MoveDeck(d.Path, target); CloseDialog(); Rebuild();
                }));
            }
        }
        private void MoveDeck(string source, string target)
        {
            if (File.Exists(target)) throw new IOException("目标名称已存在。");
            File.Move(source, target); UpdateDeckReferences(source, target); selectedPath = Path.GetFullPath(target);
        }
        private void UpdateDeckReferences(string before, string after)
        {
            var current = Program.PATH_DECK + Config.GetConfigDeckName() + ".ydk";
            if (Path.GetFullPath(current) == Path.GetFullPath(before)) { Config.SetConfigDeck(after == null ? "" : PersonalName(after)); Config.Save(); }
            if (cpuDeck != null && Path.GetFullPath(Program.PATH_DECK + cpuDeck + ".ydk") == Path.GetFullPath(before)) cpuDeck = after == null ? "" : PersonalName(after);
            var config = ResponsesAiConfigStore.Load();
            if (SameDeckPath(config.DeckA, before)) config.DeckA = after ?? "";
            if (SameDeckPath(config.DeckB, before)) config.DeckB = after ?? "";
            ResponsesAiConfigStore.SaveChoices(config.DeckA, config.DeckB, config.AiVsAi);
            if (SameDeckPath(battle.DeckA, before)) battle.DeckA = after ?? ""; if (SameDeckPath(battle.DeckB, before)) battle.DeckB = after ?? "";
            if (!string.IsNullOrEmpty(cpuCustomDeck) && SameDeckPath(Program.PATH_DECK + cpuCustomDeck + ".ydk", before)) cpuCustomDeck = after == null ? "" : PersonalName(after);
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
                Action save = () => { Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(d.Path, target, true); ShowStatus("已导出到 " + target); };
                if (File.Exists(target)) Confirm("覆盖导出文件", "目标文件已存在，是否覆盖？", save); else save();
            });
        }
        private void ManageGroup()
        {
            string group = deckGroup, directory = Path.Combine(Program.PATH_DECK, group);
            if (group == "默认分组" || group == "全部" || !Directory.Exists(directory)) return;
            var box = ShowDialog("管理分组 · " + group, 620, 360);
            FightUI.Button(box, "重命名分组", 28, 88, 564, 48, () => Prompt("重命名分组", "新名称", group, name =>
            {
                CheckName(name); var target = Path.Combine(Program.PATH_DECK, name); if (Directory.Exists(target)) throw new IOException("该分组已经存在。");
                var files = Directory.GetFiles(directory, "*.ydk"); Directory.Move(directory, target);
                foreach (var file in files) UpdateDeckReferences(file, Path.Combine(target, Path.GetFileName(file)));
                deckGroup = name; Rebuild();
            }));
            FightUI.Button(box, "复制分组", 28, 152, 564, 48, () => Prompt("复制分组", "新分组名称", group + " 副本", name =>
            {
                CheckName(name); var target = Path.Combine(Program.PATH_DECK, name); if (Directory.Exists(target)) throw new IOException("该分组已经存在。");
                Directory.CreateDirectory(target); foreach (var file in Directory.GetFiles(directory, "*.ydk")) File.Copy(file, Path.Combine(target, Path.GetFileName(file)), false);
                deckGroup = name; Rebuild();
            }));
            var decks = Directory.GetFiles(directory, "*.ydk");
            FightUI.Button(box, "删除分组…", 28, 216, 564, 48, () => Confirm("删除分组", "删除分组“" + group + "”及其中 " + decks.Length + " 套卡组？", () =>
            {
                foreach (var file in decks) { File.Delete(file); UpdateDeckReferences(file, null); }
                if (!Directory.EnumerateFileSystemEntries(directory).Any()) Directory.Delete(directory);
                deckGroup = "全部"; Rebuild(); ShowStatus("已删除分组中的卡组；其它文件保持原样。");
            }));
        }
    }
}
