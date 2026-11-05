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
        private readonly string[] filterGroupLabels = { "卡片类型", "属性", "魔法与陷阱", "种族", "怪兽能力", "禁限状态", "卡池", "效果标签", "稀有度", "召唤动画", "动态卡图", "扩展卡面", "连接箭头" };
        private TextMeshProUGUI filterCountText, filterErrorText;
        private Button filterPackButton, filterApplyButton, filterSummaryButton;
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
            var categoryDivider = FightUI.Panel(box, side + 28, 84, 1, height - 244, FightUI.Border, false, "Filter category divider");
            categoryDivider.GetComponent<Image>().raycastTarget = false;
            string[] titles = { "类型", "属性与种族", "数值范围", "效果标签", "禁限与卡池", "外观与演出" };
            for (int i = 0; i < titles.Length; i++)
            {
                int category = i; bool selected = filterCategory == i;
                var row = FightUI.Panel(box, 24, 84 + i * 58, side, 48, selected ? FightUI.Raised : Color.clear, true, titles[i]);
                row.GetComponent<Outline>().enabled = selected;
                row.GetComponent<Outline>().effectColor = FightUI.GoldBorder;
                FightUI.Text(row, titles[i], 14, 0, side - 54, 48, 18, selected ? FightUI.Active : FightUI.Muted, TextAlignmentOptions.MidlineLeft);
                int count = FilterCategoryCount(i);
                var countBadge = FightUI.Panel(row, side - 36, 12, 28, 24, FightUI.ActionFill, false, "Condition count badge");
                countBadge.GetComponent<Image>().raycastTarget = false; countBadge.gameObject.SetActive(count > 0);
                filterCategoryCountTexts[i] = FightUI.Text(row, count > 0 ? count.ToString() : "", side - 34, 0, 24, 48, 15, FightUI.Gold, TextAlignmentOptions.MidlineRight);
                var button = row.gameObject.AddComponent<Button>(); button.targetGraphic = row.GetComponent<Image>();
                FightUI.FocusKey(button, "filter-category:" + category);
                var frame = row.GetComponentInChildren<FightFrame>(); frame.ButtonFrame = true;
                row.gameObject.AddComponent<FightButtonFeedback>().Bind(frame);
                button.onClick.AddListener(() => { filterCategory = category; RenderEditorFilters(); });
            }
            filterContent = FightUI.Scroll(box, contentX, 84, contentWidth, height - 244, 1600, out filterScroll);
            float y = 8; TMP_InputField firstInvalidRange = null;
            int[][] groups = { new[] { 0, 2, 4 }, new[] { 1, 3, 12 }, Array.Empty<int>(), new[] { 7 }, new[] { 5, 6 }, new[] { 8, 9, 10, 11 } };
            foreach (int group in groups[filterCategory])
            {
                ModuleDivider(filterContent, filterGroupLabels[group], 8, y, contentWidth - 16, 20); y += 52;
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
                        else
                        {
                            int validValue;
                            if (string.IsNullOrWhiteSpace(value) || int.TryParse(value, out validValue) && validValue >= (fieldIndex >= 2 && fieldIndex <= 5 ? -2 : 0))
                                invalidFilterRangeIndices.Remove(fieldIndex);
                        }
                        if (minimum != null) minimum.GetComponent<Outline>().effectColor = invalidFilterRangeIndices.Contains(index) ? FightUI.Danger : FightUI.Border;
                        if (maximum != null) maximum.GetComponent<Outline>().effectColor = invalidFilterRangeIndices.Contains(index + 1) ? FightUI.Danger : FightUI.Border;
                        if (rangeError != null) rangeError.text = invalidFilterRangeIndices.Contains(index) || invalidFilterRangeIndices.Contains(index + 1) ? rangeMessage : "";
                        if (invalidFilterRangeIndices.Count == 0)
                        {
                            filterError = null;
                        }
                        UpdateFilterCount();
                    };
                    minimum = FightUI.Input(filterContent, filterRanges[index], "最低 · 不限", x, rowY + 42, fieldWidth, 48, v => update(index, v));
                    FightUI.FocusKey(minimum, "filter-range:" + index);
                    FightUI.Hint(minimum.gameObject, filterRangeLabels[i] + "最低值；留空表示不限");
                    FightUI.Text(filterContent, "—", x + fieldWidth + 2, rowY + 42, 38, 48, 18, FightUI.Muted, TextAlignmentOptions.Center);
                    maximum = FightUI.Input(filterContent, filterRanges[index + 1], "最高 · 不限", x + fieldWidth + 42, rowY + 42, fieldWidth, 48, v => update(index + 1, v));
                    FightUI.FocusKey(maximum, "filter-range:" + (index + 1));
                    FightUI.Hint(maximum.gameObject, filterRangeLabels[i] + "最高值；留空表示不限");
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
            filterCountText = FightUI.Text(box, "", contentX, height - 132, contentWidth - 156, 28, 16, FightUI.Muted);
            filterSummaryButton = FightUI.FocusKey(FightUI.TextAction(box, "查看全部条件  →", width - 184, height - 134, 156, 30, OpenFilterSummary), "filter:summary");
            FightUI.Hint(filterSummaryButton.gameObject, "逐条阅读全部筛选条件，保留当前草稿与分类位置");
            filterErrorText = FightUI.Text(box, string.IsNullOrEmpty(filterError) ? "" : FilterErrorSummary(), contentX, height - 102, contentWidth, 26, 15,
                string.IsNullOrEmpty(filterError) ? FightUI.Muted : FightUI.Danger);
            filterErrorText.enableWordWrapping = false; filterErrorText.overflowMode = TextOverflowModes.Ellipsis;
            FightUI.FocusKey(FightUI.TextAction(box, "重置筛选", 24, height - 72, side, 48, () => { filterMasks = new long[13]; filterRanges = Enumerable.Repeat("", 12).ToArray(); filterPack = ""; filterError = null; invalidFilterRangeIndices.Clear(); RenderEditorFilters(); }), "filter:reset");
            var cancel = FightUI.FocusKey(FightUI.Button(box, "取消", width - 344, height - 72, 140, 48, CloseDialog), "filter:cancel");
            FightUI.Hint(cancel.gameObject, "放弃本次筛选草稿；已应用条件、搜索词和构筑保持不变");
            filterApplyButton = FightUI.FocusKey(FightUI.Button(box, "应用筛选", width - 188, height - 72, 160, 48, ApplyEditorFilters, true), "filter:apply");
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
            if (selected) FightUI.Icon(panel, "check", 48, 6, 10, 10, FightUI.Active);
            FightUI.Text(panel, label, 4, 38, 56, 24, 14, selected ? FightUI.Active : FightUI.Muted, TextAlignmentOptions.Center);
            var button = panel.gameObject.AddComponent<Button>(); button.targetGraphic = panel.GetComponent<Image>();
            FightUI.FocusKey(button, "filter-option:12:" + code);
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
            FightUI.FocusKey(button, "filter-option:" + group + ":" + code);
            var frame = panel.GetComponentInChildren<FightFrame>(); frame.ButtonFrame = true;
            panel.gameObject.AddComponent<FightButtonFeedback>().Bind(frame);
            FightUI.Hint(panel.gameObject, label);
            button.onClick.AddListener(() => { filterMasks[group] ^= code; RenderEditorFilters(); });
        }
        private int FilterConditionCount() => clientFilterOptions.Count(o => (filterMasks[o.Group] & o.Code) != 0)
            + filterRanges.Count(v => !string.IsNullOrWhiteSpace(v)) + (string.IsNullOrEmpty(filterPack) ? 0 : 1);

        private bool FilterDraftHasChanges()
        {
            var applied = CardCollectionView.filters;
            for (int i = 0; i < filterMasks.Length; i++)
                if (filterMasks[i] != (applied.Count > i ? applied[i] : 0)) return true;
            for (int i = 0; i < filterRanges.Length; i++)
            {
                long draft = -233;
                if (!string.IsNullOrWhiteSpace(filterRanges[i]))
                {
                    if (!int.TryParse(filterRanges[i], out var parsed) || parsed < (i >= 2 && i <= 5 ? -2 : 0)) return true;
                    draft = parsed;
                }
                if (draft != (applied.Count > i + 13 ? applied[i + 13] : -233)) return true;
            }
            return !string.Equals(filterPack, CardCollectionView.packName ?? "", StringComparison.Ordinal);
        }

        private string FilterErrorSummary()
        {
            if (filterError == "范围起点不能大于终点。" && invalidFilterRangeIndices.Count > 0)
                return filterRangeLabels[invalidFilterRangeIndices.OrderBy(index => index).First() / 2] + "：" + filterError;
            return filterError ?? "";
        }

        private bool FilterDraftNeedsCorrection()
        {
            for (int i = 0; i < filterRanges.Length; i += 2)
            {
                int lower = 0, upper = 0, minimum = i >= 2 && i <= 4 ? -2 : 0;
                bool noLower = string.IsNullOrWhiteSpace(filterRanges[i]), noUpper = string.IsNullOrWhiteSpace(filterRanges[i + 1]);
                if (!noLower && (!int.TryParse(filterRanges[i], out lower) || lower < minimum)) return true;
                if (!noUpper && (!int.TryParse(filterRanges[i + 1], out upper) || upper < minimum)) return true;
                if (!noLower && !noUpper && lower > upper) return true;
            }
            return false;
        }

        private List<string> FilterSelectionParts()
        {
            var parts = new List<string>();
            for (int group = 0; group < filterMasks.Length; group++)
            {
                var selected = clientFilterOptions.Where(option => option.Group == group && (filterMasks[group] & option.Code) != 0)
                    .Select(option => option.Label).Distinct().ToArray();
                if (selected.Length > 0) parts.Add(filterGroupLabels[group] + "：" + string.Join("、", selected));
            }
            for (int i = 0; i < filterRangeLabels.Length; i++)
            {
                string lower = filterRanges[i * 2]?.Trim(), upper = filterRanges[i * 2 + 1]?.Trim();
                bool hasLower = !string.IsNullOrEmpty(lower), hasUpper = !string.IsNullOrEmpty(upper);
                if (!hasLower && !hasUpper) continue;
                if ((i == 1 || i == 2) && lower == "-2") lower = "-2（?）";
                if ((i == 1 || i == 2) && upper == "-2") upper = "-2（?）";
                parts.Add(filterRangeLabels[i] + "：" + (hasLower && hasUpper ? lower == upper ? lower : lower + "–" + upper : hasLower ? "≥ " + lower : "≤ " + upper));
            }
            if (!string.IsNullOrEmpty(filterPack)) parts.Add("卡包：" + filterPack);
            return parts;
        }

        private string FilterSelectionSummary()
        {
            var parts = FilterSelectionParts();
            return parts.Count > 0 ? string.Join("；", parts) : "未选择条件";
        }

        private void OpenFilterSummary()
        {
            CacheFilterScroll();
            float width = Mathf.Min(820, W - 48), height = Mathf.Min(660, H - 48);
            var box = ShowDialog("复核筛选条件", width, height, true);
            bool invalid = FilterDraftNeedsCorrection(), pending = FilterDraftHasChanges();
            FightUI.Text(box, (invalid ? "待修正" : pending ? "待应用" : "当前筛选") + " · " + FilterConditionCount() + " 项条件",
                28, 84, width - 56, 30, 18, invalid ? FightUI.Danger : FightUI.Gold);
            FightUI.Line(box, 28, 122, width - 56, FightUI.Border);
            var content = FightUI.Scroll(box, 24, 138, width - 48, height - 232, height - 232, out var scroll);
            scroll.name = "Filter complete summary";
            var parts = FilterSelectionParts();
            if (parts.Count == 0) parts.Add("未选择条件");
            if (!string.IsNullOrEmpty(filterError)) parts.Insert(0, FilterErrorSummary());
            float y = 6;
            foreach (var part in parts)
            {
                var label = FightUI.Text(content, part, 12, y, width - 94, 100, 18, part == FilterErrorSummary() ? FightUI.Danger : FightUI.Foreground);
                label.lineSpacing = 5; label.paragraphSpacing = 8; label.overflowMode = TextOverflowModes.Overflow;
                float rowHeight = Mathf.Max(32, label.GetPreferredValues(label.text, width - 94, float.PositiveInfinity).y + 12);
                label.rectTransform.sizeDelta = new Vector2(width - 94, rowHeight);
                FightUI.Line(content, 12, y + rowHeight + 8, width - 94, FightUI.Border);
                y += rowHeight + 28;
            }
            content.sizeDelta = new Vector2(width - 48, Mathf.Max(height - 232, y + 8));
            FightUI.Line(box, 28, height - 80, width - 56, FightUI.GoldBorder);
            FightUI.FocusKey(FightUI.Button(box, "取消筛选", 28, height - 64, 144, 44, CloseDialog), "filter-summary:cancel");
            FightUI.FocusKey(FightUI.Button(box, "返回修改", width - 356, height - 64, 144, 44, () =>
            {
                RenderEditorFilters();
                filterSummaryButton.Select();
            }), "filter-summary:return");
            FightUI.FocusKey(FightUI.Button(box, "应用筛选", width - 196, height - 64, 168, 44, ApplyEditorFilters, true), "filter-summary:apply");
        }

        private void UpdateFilterCount()
        {
            if (filterCountText != null)
            {
                int count = FilterConditionCount();
                bool pending = FilterDraftHasChanges();
                bool invalid = FilterDraftNeedsCorrection();
                filterCountText.text = (invalid ? "待修正" : pending ? "待应用" : "当前筛选") + " · " + count + " 项条件";
                filterCountText.color = invalid ? FightUI.Danger : count > 0 || pending ? FightUI.Gold : FightUI.Muted;
                string summary = FilterSelectionSummary();
                FightUI.Hint(filterCountText.gameObject, summary + "\n修改后点击“应用筛选”；搜索关键词会保留。");
                if (filterApplyButton != null) FightUI.Hint(filterApplyButton.gameObject, "应用以下条件，保留搜索关键词：\n" + summary);
                if (filterErrorText != null)
                {
                    filterErrorText.text = !string.IsNullOrEmpty(filterError) ? FilterErrorSummary() : summary;
                    filterErrorText.color = !string.IsNullOrEmpty(filterError) ? FightUI.Danger : FightUI.Muted;
                    FightUI.Hint(filterErrorText.gameObject, summary + "\n修改后点击“应用筛选”；搜索关键词会保留。");
                }
            }
            for (int i = 0; i < filterCategoryCountTexts.Length; i++)
                if (filterCategoryCountTexts[i] != null)
                {
                    int count = FilterCategoryCount(i);
                    filterCategoryCountTexts[i].text = count > 0 ? count.ToString() : "";
                    var badge = filterCategoryCountTexts[i].transform.parent.Find("Condition count badge");
                    if (badge != null) badge.gameObject.SetActive(count > 0);
                }
        }
        private void ChooseFilterPack()
        {
            CacheFilterScroll();
            var width = Mathf.Min(900, W - 48); var height = Mathf.Min(680, H - 48);
            var box = ShowDialog("选择卡包", width, height); var packs = new[] { "" }.Concat(PacksManager.packs.Select(p => p.fullName)).Distinct().ToArray();
            float viewportHeight = height - 224;
            var c = FightUI.Scroll(box, 24, 144, width - 48, viewportHeight, packs.Length * 58, out var packScroll);
            TMP_InputField searchInput = null;
            Button clearSearch = null;
            Action clearPackSearch = () =>
            {
                searchInput.text = "";
                searchInput.Select(); searchInput.ActivateInputField(); searchInput.caretPosition = 0;
            };
            Action<string> renderPacks = keyword =>
            {
                FightUI.Clear(c);
                if (clearSearch != null) clearSearch.interactable = keyword.Length > 0;
                var terms = keyword.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                var matches = packs.Where(pack => terms.All(term => (pack.Length == 0 ? "所有卡包" : pack).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
                float y = 0, selectedTop = -1, selectedHeight = 0;
                for (int i = 0; i < matches.Length; i++)
                {
                    string pack = matches[i];
                    bool selected = pack == filterPack;
                    var button = FightUI.Button(c, pack.Length == 0 ? "所有卡包" : pack, 8, y, width - 64, 48, () => { filterPack = pack; ReturnFromFilterPack(); }, selected);
                    FightUI.FocusKey(button, "filter-pack-choice:" + pack);
                    var caption = button.GetComponentInChildren<TextMeshProUGUI>();
                    caption.fontSize = 18; caption.lineSpacing = 4;
                    caption.enableWordWrapping = true; caption.overflowMode = TextOverflowModes.Overflow;
                    float captionWidth = width - 124;
                    float rowHeight = Mathf.Max(48, caption.GetPreferredValues(caption.text, captionWidth, float.PositiveInfinity).y + 18);
                    FightUI.Place(button.GetComponent<RectTransform>(), 8, y, width - 64, rowHeight);
                    FightUI.Place(caption.rectTransform, 42, 8, captionWidth, rowHeight - 16);
                    caption.alignment = TextAlignmentOptions.MidlineLeft;
                    if (selected)
                    {
                        selectedTop = y; selectedHeight = rowHeight;
                        FightUI.Icon(button.transform, "check", 16, (rowHeight - 14) / 2, 14, 14, FightUI.Active);
                    }
                    FightUI.Hint(button.gameObject, pack.Length == 0 ? "所有卡包" : pack);
                    y += rowHeight + 10;
                }
                if (matches.Length == 0)
                {
                    FightUI.Text(c, "没有匹配的卡包", 16, 24, width - 80, 52, 18, FightUI.Muted);
                    FightUI.FocusKey(FightUI.Button(c, "清空关键词", 16, 92, 180, 44, clearPackSearch), "filter-pack:empty-clear-search");
                    y = 148;
                }
                float contentHeight = Mathf.Max(viewportHeight, y);
                c.sizeDelta = new Vector2(width - 48, contentHeight);
                Canvas.ForceUpdateCanvases(); packScroll.StopMovement();
                float scrollRange = Mathf.Max(0, contentHeight - viewportHeight);
                float selectedOffset = Mathf.Max(0, selectedTop - (viewportHeight - selectedHeight) / 2);
                packScroll.verticalNormalizedPosition = selectedTop >= 0 && scrollRange > 0 ? 1 - Mathf.Clamp01(selectedOffset / scrollRange) : 1;
            };
            searchInput = FightUI.Input(box, "", "搜索卡包名称", 24, 84, width - 48, 44, renderPacks);
            FightUI.FocusKey(searchInput, "filter-pack:search");
            FightUI.Place(searchInput.textViewport, 10, 5, width - 110, 34);
            foreach (var label in searchInput.textViewport.GetComponentsInChildren<TextMeshProUGUI>())
                FightUI.Place(label.rectTransform, 0, 0, width - 110, 34);
            clearSearch = FightUI.FocusKey(FightUI.Button(box, "清空搜索", width - 70, 84, 44, 44, clearPackSearch, false, false), "filter-pack:clear-search");
            clearSearch.GetComponent<Image>().color = Color.clear; clearSearch.GetComponent<Outline>().enabled = false;
            clearSearch.GetComponentInChildren<TextMeshProUGUI>().gameObject.SetActive(false);
            FightUI.Icon(clearSearch.transform, "close", 14, 14, 16, 16, FightUI.Muted);
            FightUI.Hint(clearSearch.gameObject, "清空关键词，保留当前卡包草稿并继续输入");
            FightUI.Hint(searchInput.gameObject, "按卡包名称搜索；多个关键词需同时匹配");
            renderPacks("");
            FightUI.Button(box, "返回筛选", width - 188, height - 64, 160, 44, ReturnFromFilterPack);
            searchInput.Select(); searchInput.ActivateInputField();
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
                var box = ShowDialog("当前卡组尚未保存", 640, 192);
                FightUI.Button(box, "取消", 28, 112, 160, 48, CloseDialog);
                var discard = FightUI.Button(box, "放弃修改", 208, 112, 160, 48, () => { DiscardEditor(); OpenClientHandTest(); });
                FightUI.StyleDestructiveButton(discard);
                FightUI.Button(box, "保存并测试", 388, 112, 224, 48, () => SaveEditorThen(OpenClientHandTest), true); return;
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
