using System;
using System.Collections.Generic;
using System.Linq;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace MDPro3.UI.Client
{
    internal partial class FightClient
    {
        private sealed class ClientFilterOption
        {
            internal int Group;
            internal long Code;
            internal string Label;
        }
        private readonly List<ClientFilterOption> clientFilterOptions = new List<ClientFilterOption>();
        private long[] filterMasks = new long[13];
        private string[] filterRanges = new string[12];
        private string filterPack = "";
        private int filterCategory;
        private string filterError;
        private RectTransform filterContent;
        private ScrollRect filterScroll;
        private int renderedFilterCategory = -1;
        private readonly Dictionary<int, float> filterScrollPositions = new Dictionary<int, float>();
        private readonly HashSet<int> invalidFilterRangeIndices = new HashSet<int>();
        private readonly string[] filterRangeLabels = { "等级 / 阶级 / LINK", "攻击力", "守备力", "灵摆刻度", "卡片 GP", "发行年份" };
        private TextMeshProUGUI filterCountText, filterErrorText;
        private Button filterPackButton;
        private readonly TextMeshProUGUI[] filterCategoryCountTexts = new TextMeshProUGUI[6];
        private HandTestSettings clientHandTestDraft;
        private string clientHandTestHand, clientHandTestLp;

        internal void OpenEditorFilters()
        {
            var current = CardCollectionView.filters;
            filterMasks = Enumerable.Range(0, 13).Select(i => current.Count > i ? current[i] : 0).ToArray();
            filterRanges = Enumerable.Range(13, 12).Select(i => current.Count > i && current[i] != -233 ? current[i].ToString() : "").ToArray();
            filterPack = CardCollectionView.packName ?? ""; filterCategory = 0; filterError = null;
            filterScrollPositions.Clear(); invalidFilterRangeIndices.Clear(); filterScroll = null; renderedFilterCategory = -1;
            if (clientFilterOptions.Count > 0) { RenderEditorFilters(); return; }
            var box = ShowDialog("筛选卡片", 1120, Mathf.Min(820, H - 48));
            FightUI.Text(box, "正在读取卡片筛选条件…", 28, 92, box.rect.width - 56, 48, 20, FightUI.Muted);
            var waiting = modal;
            // Read the shipping prefab's exact bit masks and translated labels.
            // It supplies metadata only; all visible controls belong to this client.
            var handle = Addressables.InstantiateAsync("Popup/PopupSearchFilter.prefab");
            handle.Completed += loaded =>
            {
                if (loaded.Status != AsyncOperationStatus.Succeeded || loaded.Result == null)
                {
                    if (this != null && modal == waiting) { CloseDialog(); ShowStatus("筛选资源加载失败，请重试。"); }
                    Addressables.Release(loaded); return;
                }
                loaded.Result.SetActive(false);
                try
                {
                    foreach (var toggle in loaded.Result.GetComponentsInChildren<SelectionToggle_SearchFilter>(true))
                        if (toggle.group >= 0 && toggle.group < 13 && toggle.filterCode != 0)
                            clientFilterOptions.Add(new ClientFilterOption { Group = toggle.group, Code = toggle.filterCode, Label = toggle.GetButtonText() });
                    foreach (var type in new[] { CardType.Monster, CardType.Spell, CardType.Trap })
                        if (!clientFilterOptions.Any(option => option.Group == 0 && option.Code == (long)type))
                            clientFilterOptions.Add(new ClientFilterOption { Group = 0, Code = (long)type, Label = type == CardType.Monster ? "怪兽" : type == CardType.Spell ? "魔法" : "陷阱" });
                }
                finally { Addressables.ReleaseInstance(loaded.Result); }
                if (this != null && modal == waiting && IsClientEditor) RenderEditorFilters();
            };
        }

        private void RenderEditorFilters()
        {
            CacheFilterScroll();
            var width = Mathf.Min(1120, W - 48); var height = Mathf.Min(820, H - 48);
            var box = ShowDialog("筛选卡片", width, height, true);
            filterPackButton = null;
            FightUI.Line(box, 24, 64, width - 48, FightUI.GoldBorder);
            var actions = FightUI.Panel(box, 16, height - 144, width - 32, 128, FightUI.PanelColor, false);
            FightUI.Line(actions, 0, 0, width - 32, FightUI.GoldBorder);
            float side = Compact ? 176 : 200, contentX = side + 40, contentWidth = width - contentX - 28;
            string[] titles = { "类型", "属性与种族", "数值范围", "效果标签", "禁限与卡池", "外观与演出" };
            for (int i = 0; i < titles.Length; i++)
            {
                int category = i; bool selected = filterCategory == i;
                var row = FightUI.Panel(box, 24, 84 + i * 58, side, 48, selected ? FightUI.Raised : Color.clear, true, titles[i]);
                row.GetComponent<Outline>().enabled = false;
                if (selected) FightUI.Panel(row, 0, 8, 2, 32, FightUI.Active, false);
                FightUI.Text(row, titles[i], 14, 0, side - 54, 48, 18, selected ? FightUI.Active : FightUI.Muted, TextAlignmentOptions.MidlineLeft);
                int count = FilterCategoryCount(i);
                filterCategoryCountTexts[i] = FightUI.Text(row, count > 0 ? count.ToString() : "", side - 34, 0, 24, 48, 15, FightUI.Gold, TextAlignmentOptions.MidlineRight);
                var button = row.gameObject.AddComponent<Button>(); button.targetGraphic = row.GetComponent<Image>();
                var frame = row.GetComponentInChildren<FightFrame>(); frame.ButtonFrame = true;
                row.gameObject.AddComponent<FightButtonFeedback>().Bind(frame);
                button.onClick.AddListener(() => { filterCategory = category; RenderEditorFilters(); });
            }
            filterContent = FightUI.Scroll(box, contentX, 84, contentWidth, height - 244, 1600, out filterScroll);
            float y = 8; TMP_InputField firstInvalidRange = null;
            int[][] groups = { new[] { 0, 2, 4 }, new[] { 1, 3, 12 }, Array.Empty<int>(), new[] { 7 }, new[] { 5, 6 }, new[] { 8, 9, 10, 11 } };
            string[] groupTitles = { "卡片类型", "属性", "魔法与陷阱", "种族", "怪兽能力", "禁限状态", "卡池", "效果标签", "稀有度", "召唤动画", "动态卡图", "扩展卡面", "连接箭头" };
            foreach (int group in groups[filterCategory])
            {
                ModuleDivider(filterContent, groupTitles[group], 8, y, contentWidth - 16, 20); y += 52;
                var options = clientFilterOptions.Where(o => o.Group == group).ToList();
                if (group == 0)
                {
                    float basicWidth = (contentWidth - 40) / 3;
                    FilterChip(filterContent, 0, (long)CardType.Monster, "怪兽", 8, y, basicWidth);
                    FilterChip(filterContent, 0, (long)CardType.Spell, "魔法", 20 + basicWidth, y, basicWidth);
                    FilterChip(filterContent, 0, (long)CardType.Trap, "陷阱", 32 + basicWidth * 2, y, basicWidth);
                    y += 66;
                    options = options.Where(option => option.Code != (long)CardType.Monster && option.Code != (long)CardType.Spell && option.Code != (long)CardType.Trap).ToList();
                    FightUI.Text(filterContent, "怪兽子类型", 8, y, contentWidth - 16, 26, 15, FightUI.Muted); y += 34;
                }
                int columns = Mathf.Max(2, Mathf.FloorToInt((contentWidth - 16) / 168));
                float cellWidth = (contentWidth - 16 - (columns - 1) * 12) / columns;
                if (group == 12)
                {
                    // OCG link-marker bits use the keypad arrangement with no center.
                    long[] arrows = { 64, 128, 256, 8, 0, 32, 1, 2, 4 };
                    string[] words = { "左上", "上", "右上", "左", "", "右", "左下", "下", "右下" };
                    float[] angles = { 135, 90, 45, 180, 0, 0, 225, 270, 315 };
                    for (int i = 0; i < arrows.Length; i++)
                    {
                        float x = 8 + i % 3 * 76, cy = y + i / 3 * 78;
                        if (arrows[i] == 0)
                        {
                            var center = FightUI.Panel(filterContent, x, cy, 64, 68, FightUI.Background, false);
                            center.GetComponent<Image>().raycastTarget = false;
                            var label = FightUI.Text(center, "LINK", 0, 20, 64, 28, 14, FightUI.GoldBorder, TextAlignmentOptions.Center); label.raycastTarget = false;
                            continue;
                        }
                        FilterArrowChip(filterContent, arrows[i], words[i], angles[i], x, cy);
                    }
                    FightUI.Text(filterContent, "连接箭头", 256, y + 20, contentWidth - 272, 32, 20, FightUI.Foreground);
                    y += 254;
                }
                else
                {
                    for (int i = 0; i < options.Count; i++)
                    {
                        var option = options[i];
                        FilterChip(filterContent, group, option.Code, option.Label, 8 + i % columns * (cellWidth + 12), y + i / columns * 58, cellWidth);
                    }
                    y += Mathf.Ceil((float)options.Count / columns) * 58 + 20;
                }
            }
            if (filterCategory == 2)
            {
                float rangeWidth = (contentWidth - 32) / 2, fieldWidth = (rangeWidth - 42) / 2;
                for (int i = 0; i < filterRangeLabels.Length; i++)
                {
                    int index = i * 2; float x = 8 + i % 2 * (rangeWidth + 16), rowY = y + i / 2 * 120;
                    FightUI.Text(filterContent, filterRangeLabels[i], x, rowY, rangeWidth, 32, 19, FightUI.Foreground);
                    TMP_InputField minimum = null, maximum = null; TextMeshProUGUI rangeError = null;
                    string rangeMessage = filterError;
                    Action<int, string> update = (fieldIndex, value) =>
                    {
                        filterRanges[fieldIndex] = value;
                        if (rangeMessage == "范围起点不能大于终点。")
                        {
                            int lower = 0, upper = 0, smallest = index >= 2 && index <= 4 ? -2 : 0;
                            bool noMinimum = string.IsNullOrWhiteSpace(filterRanges[index]), noMaximum = string.IsNullOrWhiteSpace(filterRanges[index + 1]);
                            bool validMinimum = noMinimum || int.TryParse(filterRanges[index], out lower) && lower >= smallest;
                            bool validMaximum = noMaximum || int.TryParse(filterRanges[index + 1], out upper) && upper >= smallest;
                            if (validMinimum && validMaximum && (noMinimum || noMaximum || lower <= upper))
                            { invalidFilterRangeIndices.Remove(index); invalidFilterRangeIndices.Remove(index + 1); }
                        }
                        else invalidFilterRangeIndices.Remove(fieldIndex);
                        if (minimum != null) minimum.GetComponent<Outline>().effectColor = invalidFilterRangeIndices.Contains(index) ? FightUI.Danger : FightUI.Border;
                        if (maximum != null) maximum.GetComponent<Outline>().effectColor = invalidFilterRangeIndices.Contains(index + 1) ? FightUI.Danger : FightUI.Border;
                        if (rangeError != null) rangeError.text = invalidFilterRangeIndices.Contains(index) || invalidFilterRangeIndices.Contains(index + 1) ? rangeMessage : "";
                        if (invalidFilterRangeIndices.Count == 0)
                        {
                            filterError = null;
                            if (filterErrorText != null) { filterErrorText.text = "选择条件后应用到卡池，当前搜索会保留。"; filterErrorText.color = FightUI.Muted; }
                        }
                        UpdateFilterCount();
                    };
                    minimum = FightUI.Input(filterContent, filterRanges[index], "最低 · 不限", x, rowY + 42, fieldWidth, 48, v => update(index, v));
                    FightUI.Text(filterContent, "—", x + fieldWidth + 2, rowY + 42, 38, 48, 18, FightUI.Muted, TextAlignmentOptions.Center);
                    maximum = FightUI.Input(filterContent, filterRanges[index + 1], "最高 · 不限", x + fieldWidth + 42, rowY + 42, fieldWidth, 48, v => update(index + 1, v));
                    if (invalidFilterRangeIndices.Contains(index)) { minimum.GetComponent<Outline>().effectColor = FightUI.Danger; if (firstInvalidRange == null) firstInvalidRange = minimum; }
                    if (invalidFilterRangeIndices.Contains(index + 1)) { maximum.GetComponent<Outline>().effectColor = FightUI.Danger; if (firstInvalidRange == null) firstInvalidRange = maximum; }
                    rangeError = FightUI.Text(filterContent, invalidFilterRangeIndices.Contains(index) || invalidFilterRangeIndices.Contains(index + 1) ? filterError : "", x, rowY + 94, rangeWidth, 24, 14, FightUI.Danger);
                }
                y += 372;
                FightUI.Text(filterContent, "留空表示不限；攻击力与守备力支持 -2（?）。", 8, y, contentWidth - 16, 48, 16, FightUI.Muted); y += 64;
            }
            if (filterCategory == 4)
            {
                ModuleDivider(filterContent, "卡包", 8, y, contentWidth - 16, 23); y += 52;
                filterPackButton = FightUI.FocusKey(FightUI.Button(filterContent, string.IsNullOrEmpty(filterPack) ? "所有卡包   ▾" : filterPack + "   ▾", 8, y, contentWidth - 16, 48, ChooseFilterPack), "filter-pack");
                FightUI.Hint(filterPackButton.gameObject, string.IsNullOrEmpty(filterPack) ? "所有卡包" : filterPack); y += 68;
            }
            filterContent.sizeDelta = new Vector2(contentWidth, Mathf.Max(y + 8, height - 244));
            filterCountText = FightUI.Text(box, "", contentX, height - 132, contentWidth, 28, 16, FightUI.Muted);
            filterErrorText = FightUI.Text(box, filterError ?? "", contentX, height - 102, contentWidth, 26, 15, FightUI.Danger);
            FightUI.TextAction(box, "重置筛选", 24, height - 72, side, 48, () => { filterMasks = new long[13]; filterRanges = Enumerable.Repeat("", 12).ToArray(); filterPack = ""; filterError = null; invalidFilterRangeIndices.Clear(); RenderEditorFilters(); });
            FightUI.Button(box, "取消", width - 344, height - 72, 140, 48, CloseDialog);
            FightUI.Button(box, "应用筛选", width - 188, height - 72, 160, 48, ApplyEditorFilters, true);
            Canvas.ForceUpdateCanvases();
            filterScroll.verticalNormalizedPosition = filterScrollPositions.TryGetValue(filterCategory, out var savedScroll) ? savedScroll : 1;
            renderedFilterCategory = filterCategory;
            UpdateFilterCount();
            if (firstInvalidRange != null) { firstInvalidRange.Select(); firstInvalidRange.ActivateInputField(); RevealClientControl(firstInvalidRange); }
        }

        private void CacheFilterScroll()
        {
            if (filterScroll != null && renderedFilterCategory >= 0) filterScrollPositions[renderedFilterCategory] = filterScroll.verticalNormalizedPosition;
        }

        private int FilterCategoryCount(int category)
        {
            int[][] groups = { new[] { 0, 2, 4 }, new[] { 1, 3, 12 }, Array.Empty<int>(), new[] { 7 }, new[] { 5, 6 }, new[] { 8, 9, 10, 11 } };
            return clientFilterOptions.Count(option => groups[category].Contains(option.Group) && (filterMasks[option.Group] & option.Code) != 0)
                + (category == 2 ? filterRanges.Count(value => !string.IsNullOrWhiteSpace(value)) : 0) + (category == 4 && !string.IsNullOrEmpty(filterPack) ? 1 : 0);
        }

        private void FilterArrowChip(Transform parent, long code, string label, float angle, float x, float y)
        {
            bool selected = (filterMasks[12] & code) != 0;
            var panel = FightUI.Panel(parent, x, y, 64, 68, selected ? FightUI.Raised : FightUI.PanelColor, true, "连接箭头 " + label);
            panel.GetComponent<Outline>().effectColor = selected ? FightUI.Active : FightUI.Border;
            var arrow = FightUI.Icon(panel, "arrow", 20, 8, 24, 24, selected ? FightUI.Active : FightUI.Muted);
            arrow.rectTransform.pivot = new Vector2(.5f, .5f);
            arrow.rectTransform.anchoredPosition = new Vector2(32, -20);
            arrow.rectTransform.localEulerAngles = new Vector3(0, 0, angle);
            FightUI.Text(panel, label, 4, 38, 56, 24, 14, selected ? FightUI.Active : FightUI.Muted, TextAlignmentOptions.Center);
            var button = panel.gameObject.AddComponent<Button>(); button.targetGraphic = panel.GetComponent<Image>();
            button.onClick.AddListener(() => { filterMasks[12] ^= code; RenderEditorFilters(); });
            var frame = panel.GetComponentInChildren<FightFrame>(); frame.ButtonFrame = true; panel.gameObject.AddComponent<FightButtonFeedback>().Bind(frame);
            FightUI.Hint(panel.gameObject, "匹配连接箭头：" + label);
        }

        private void FilterChip(Transform parent, int group, long code, string label, float x, float y, float width)
        {
            bool selected = (filterMasks[group] & code) != 0;
            var panel = FightUI.Panel(parent, x, y, width, 46, selected ? FightUI.Raised : FightUI.PanelColor, true, label);
            panel.GetComponent<Outline>().enabled = false;
            FightUI.Line(panel, 0, 45, width, selected ? FightUI.GoldBorder : FightUI.Border);
            var checkbox = FightUI.Panel(panel, 10, 14, 18, 18, selected ? FightUI.GoldBorder : FightUI.Background, true);
            checkbox.GetComponent<Outline>().effectColor = selected ? FightUI.Active : FightUI.Border;
            if (selected) FightUI.Icon(checkbox, "check", 3, 3, 12, 12, FightUI.Active);
            FightUI.Text(panel, label, 38, 0, width - 46, 46, 17, selected ? FightUI.Active : FightUI.Foreground, TextAlignmentOptions.MidlineLeft);
            var button = panel.gameObject.AddComponent<Button>(); button.targetGraphic = panel.GetComponent<Image>();
            var frame = panel.GetComponentInChildren<FightFrame>(); frame.ButtonFrame = true;
            panel.gameObject.AddComponent<FightButtonFeedback>().Bind(frame);
            button.onClick.AddListener(() => { filterMasks[group] ^= code; RenderEditorFilters(); });
        }
        private int FilterConditionCount() => clientFilterOptions.Count(o => (filterMasks[o.Group] & o.Code) != 0)
            + filterRanges.Count(v => !string.IsNullOrWhiteSpace(v)) + (string.IsNullOrEmpty(filterPack) ? 0 : 1);
        private void UpdateFilterCount()
        {
            if (filterCountText != null) filterCountText.text = "已启用 " + FilterConditionCount() + " 项条件 · 应用后保留当前搜索";
            for (int i = 0; i < filterCategoryCountTexts.Length; i++)
                if (filterCategoryCountTexts[i] != null)
                {
                    int count = FilterCategoryCount(i);
                    filterCategoryCountTexts[i].text = count > 0 ? count.ToString() : "";
                }
        }
        private void ChooseFilterPack()
        {
            CacheFilterScroll();
            var width = Mathf.Min(900, W - 48); var height = Mathf.Min(680, H - 48);
            var box = ShowDialog("选择卡包", width, height); var packs = new[] { "" }.Concat(PacksManager.packs.Select(p => p.fullName)).Distinct().ToArray();
            var c = FightUI.Scroll(box, 24, 84, width - 48, height - 164, packs.Length * 58);
            for (int i = 0; i < packs.Length; i++)
            {
                string pack = packs[i];
                var button = FightUI.Button(c, pack.Length == 0 ? "所有卡包" : pack, 8, i * 58, width - 64, 48, () => { filterPack = pack; ReturnFromFilterPack(); });
                FightUI.Hint(button.gameObject, pack.Length == 0 ? "所有卡包" : pack);
            }
            FightUI.Button(box, "返回筛选", width - 188, height - 64, 160, 44, ReturnFromFilterPack);
        }
        private void ReturnFromFilterPack()
        {
            RenderEditorFilters();
            if (filterPackButton == null) return;
            filterPackButton.Select(); RevealClientControl(filterPackButton);
        }
        private void ApplyEditorFilters()
        {
            invalidFilterRangeIndices.Clear();
            var values = new List<long>(filterMasks);
            for (int i = 0; i < filterRanges.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(filterRanges[i])) { values.Add(-233); continue; }
                if (!int.TryParse(filterRanges[i], out var value) || value < (i >= 2 && i <= 5 ? -2 : 0))
                { invalidFilterRangeIndices.Add(i); filterCategory = 2; filterError = filterRangeLabels[i / 2] + "需填写有效整数。"; RenderEditorFilters(); return; }
                values.Add(value);
            }
            for (int i = 13; i < 25; i += 2)
                if (values[i] != -233 && values[i + 1] != -233 && values[i] > values[i + 1])
                { invalidFilterRangeIndices.Add(i - 13); invalidFilterRangeIndices.Add(i - 12); filterCategory = 2; filterError = "范围起点不能大于终点。"; RenderEditorFilters(); return; }
            bool active = FilterConditionCount() > 0;
            CardCollectionView.filters = active ? values : new List<long>(); CardCollectionView.packName = filterPack;
            if (SelectionToggle_CardFilter.Instance != null)
            {
                if (active) SelectionToggle_CardFilter.Instance.SetToggleOn(); else SelectionToggle_CardFilter.Instance.SetToggleOff();
            }
            var view = clientEditorUI?.CardCollectionView;
            if (view != null)
            {
                if (view.area == CardCollectionView.Area.Bookmark) view.PrintBookmarkCards(); else view.PrintSearchCards();
            }
            CloseDialog(); ShowStatus("已应用 " + FilterConditionCount() + " 项筛选条件。");
        }

        internal void OpenClientHandTest()
        {
            if (clientEditorUI == null || !clientEditorUI.DeckView.deckLoaded) { ShowStatus("卡组仍在加载，请稍后再试。"); return; }
            if (EditorDirty)
            {
                var box = ShowDialog("测试前处理未保存修改", 640, 290);
                FightUI.Text(box, "保存当前构筑后开始，或放弃修改并测试已保存构筑。", 28, 80, 580, 70, 20);
                FightUI.Button(box, "取消", 28, 208, 160, 48, CloseDialog);
                FightUI.Button(box, "放弃修改", 208, 208, 160, 48, () => { DiscardEditor(); OpenClientHandTest(); });
                FightUI.Button(box, "保存并继续", 388, 208, 224, 48, () => SaveEditorThen(OpenClientHandTest), true); return;
            }
            clientHandTestDraft = HandTestSettings.Load(); clientHandTestHand = clientHandTestDraft.StartingHand.ToString(); clientHandTestLp = clientHandTestDraft.LifePoints.ToString();
            RenderClientHandTest();
        }
        private void RenderClientHandTest()
        {
            var box = ShowDialog("手牌测试设置", 680, 550);
            var deck = clientEditorUI.DeckView;
            FightUI.Text(box, "主 " + deck.mainCount + "  ·  额外 " + deck.extraCount + "  ·  副 " + deck.sideCount, 318, 76, 334, 30, 16, FightUI.Gold, TextAlignmentOptions.MidlineRight);
            FightUI.Text(box, "初始手牌数", 28, 120, 294, 30, 20); FightUI.Text(box, "起始 LP", 358, 120, 294, 30, 20);
            var handError = FightUI.Text(box, "", 28, 214, 294, 24, 15, FightUI.Danger);
            var lpError = FightUI.Text(box, "", 358, 214, 294, 24, 15, FightUI.Danger);
            var error = FightUI.Text(box, "", 28, 410, 624, 44, 16, FightUI.Danger);
            TMP_InputField handField = null, lpField = null;
            Action clearGeneralError = () => error.text = "";
            handField = FightUI.Input(box, clientHandTestHand, "1–60", 28, 160, 294, 48, v =>
            {
                clientHandTestHand = v; handError.text = "";
                if (handField != null) handField.GetComponent<Outline>().effectColor = FightUI.Border;
                clearGeneralError();
            });
            lpField = FightUI.Input(box, clientHandTestLp, "大于 0", 358, 160, 294, 48, v =>
            {
                clientHandTestLp = v; lpError.text = "";
                if (lpField != null) lpField.GetComponent<Outline>().effectColor = FightUI.Border;
                clearGeneralError();
            });
            FightUI.Text(box, "规则：大师规则 5", 28, 246, 624, 34, 21, FightUI.Gold);
            var shuffleHost = FightUI.Node(box, "Shuffle choice", 28, 292, 624, 44);
            var replayHost = FightUI.Node(box, "Replay choice", 28, 348, 624, 44);
            Action renderShuffle = null, renderReplay = null;
            renderShuffle = () =>
            {
                FightUI.Clear(shuffleHost);
                FightUI.SwitchRow(shuffleHost, "不洗切卡组", clientHandTestDraft.NoShuffleDeck, 0, 0, 624, 44,
                    () => { clientHandTestDraft.NoShuffleDeck = !clientHandTestDraft.NoShuffleDeck; renderShuffle(); });
            };
            renderReplay = () =>
            {
                FightUI.Clear(replayHost);
                FightUI.SwitchRow(replayHost, "退出时保存回放", clientHandTestDraft.SaveReplayWhenLeaving, 0, 0, 624, 44,
                    () => { clientHandTestDraft.SaveReplayWhenLeaving = !clientHandTestDraft.SaveReplayWhenLeaving; renderReplay(); });
            };
            renderShuffle(); renderReplay();
            FightUI.Line(box, 28, 460, 624);
            FightUI.Button(box, "取消", 292, 476, 160, 48, CloseDialog);
            FightUI.Button(box, "开始测试", 468, 476, 184, 48, () =>
            {
                bool validHand = int.TryParse(clientHandTestHand, out var hand) && hand >= 1 && hand <= 60;
                bool validLp = int.TryParse(clientHandTestLp, out var lp) && lp >= 1;
                handError.text = validHand ? "" : "请输入 1–60 的整数。";
                lpError.text = validLp ? "" : "LP 需为大于 0 的整数。";
                handField.GetComponent<Outline>().effectColor = validHand ? FightUI.Border : FightUI.Danger;
                lpField.GetComponent<Outline>().effectColor = validLp ? FightUI.Border : FightUI.Danger;
                if (!validHand || !validLp)
                {
                    error.text = "请修正标记的测试设置。"; error.color = FightUI.Danger;
                    var invalid = !validHand ? handField : lpField;
                    invalid.Select(); invalid.ActivateInputField();
                    return;
                }
                if (clientEditorUI.DeckView.mainCount == 0) { error.text = "请先加入主卡组卡片。"; error.color = FightUI.Danger; return; }
                clientHandTestDraft.StartingHand = hand; clientHandTestDraft.LifePoints = lp;
                try
                {
                    var snapshot = clientHandTestDraft.Clone();
                    coreSourcePage = page;
                    if (clientEditorUI.StartClientHandTest(snapshot)) { snapshot.Save(); CloseDialog(); ShowStatus("正在启动手牌测试…"); }
                }
                catch (Exception failure) { error.text = failure.Message; error.color = FightUI.Danger; }
            }, true);
        }
    }
}
