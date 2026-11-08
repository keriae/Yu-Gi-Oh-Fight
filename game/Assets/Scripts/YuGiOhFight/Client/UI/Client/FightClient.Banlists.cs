using System;
using System.Collections.Generic;
using System.Linq;


using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI.Client
{
    internal partial class FightClient
    {
        private Action dialogAfterClose;

        private void RenderGlobalBanlistHelp(Transform parent, float width, ref float y)
        {
            var hint = SettingsWrappedText(parent, "卡组构筑、人机对战和 AI 对战默认使用此表；对战设置可单独指定。保存设置后生效。",
                0, y, width, 44, 16, FightUI.Muted);
            y += hint.rectTransform.sizeDelta.y + 12;
            FightUI.FocusKey(FightUI.Button(parent, "查看禁限表", 0, y, 200, 44, () =>
                OpenBanlistViewer(settingsValues[MDPro3.Duel.YGOSharp.BanlistManager.DEFAULT_LIST_CONFIG], name =>
                {
                    SettingsChanged(MDPro3.Duel.YGOSharp.BanlistManager.DEFAULT_LIST_CONFIG, name);
                    RenderSettings();
                }, "设为默认")), "settings:banlist:view");
            y += 64;
        }

        private void OpenEditorBanlist()
        {
            if (Controllers.DeckEditor.Banlist.isCredit) { clientEditorUI?.OnRegulation(); return; }
            OpenBanlistViewer(Controllers.DeckEditor.Banlist.Name, name => clientEditorUI?.SetClientRegulation(name), "用此表构筑");
        }

        private RectTransform OpenRetainedDialog(Func<RectTransform> open)
        {
            // Keep each underlying dialog's inputs, scrolling and close continuation.
            var previousModal = modal;
            var previousBox = clientModalBox;
            var previousSize = clientModalSize;
            var previousOrigin = dialogOriginFocus;
            var previousAfterClose = dialogAfterClose;
            var previousFocus = EventSystem.current?.currentSelectedGameObject;
            if (previousModal != null) previousModal.gameObject.SetActive(false);
            modal = null; dialogOriginFocus = null; dialogAfterClose = null;
            var box = open();
            dialogAfterClose = () =>
            {
                if (previousModal == null) return;
                modal = previousModal; clientModalBox = previousBox; clientModalSize = previousSize;
                dialogAfterClose = previousAfterClose;
                dialogOriginFocus = previousOrigin; previousModal.gameObject.SetActive(true);
                if (previousFocus != null && previousFocus.activeInHierarchy && EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(previousFocus);
            };
            return box;
        }

        private void OpenBanlistViewer(string initialName, Action<string> chosen = null, string actionLabel = "选择此表")
        {
            float width = Mathf.Min(1200, W - 48), height = Mathf.Min(900, H - 48);
            var box = OpenRetainedDialog(() => ShowDialog("禁限卡表", width, height));

            var names = MDPro3.Duel.YGOSharp.BanlistManager.GetAllNames().ToArray();
            string listName = MDPro3.Duel.YGOSharp.BanlistManager.IsDuelBanlistName(initialName) ? initialName : MDPro3.Duel.YGOSharp.BanlistManager.DefaultBanlistName;
            MDPro3.Duel.YGOSharp.Banlist list = MDPro3.Duel.YGOSharp.BanlistManager.GetByName(listName);
            int category = 0, selectedCode = 0;
            string query = "";
            float inner = width - 56, inspectorWidth = Compact ? 246 : 292, gridX = inspectorWidth + 20;
            float gridWidth = inner - gridX;
            var frames = new List<Tuple<int, Outline>>();
            var readingPositions = new Dictionary<int, float>();
            var main = FightUI.Node(box, "Banlist contents", 28, 80, inner, height - 108);
            var contents = FightUI.Node(main, "Banlist results", 0, 0, inner, height - 108);
            var chooser = FightUI.Panel(box, 28, 132, inner, height - 160, FightUI.Raised, false, "Banlist chooser");
            chooser.gameObject.SetActive(false);
            Action render = null;
            Action renderChoices = null;
            string nameQuery = "";
            var nameRows = FightUI.Scroll(chooser, 12, 68, inner - 24, height - 240, height - 240, out var nameScroll);
            renderChoices = () =>
            {
                FightUI.Clear(nameRows);
                var matches = names.Where(name => name.IndexOf(nameQuery.Trim(), StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                for (int i = 0; i < matches.Length; i++)
                {
                    string name = matches[i];
                    var button = FightUI.FocusKey(FightUI.Button(nameRows, BattleBanlistLabel(name), 8, i * 52, inner - 56, 44, () =>
                    {
                        listName = name; list = MDPro3.Duel.YGOSharp.BanlistManager.GetByName(name); category = 0; selectedCode = 0;
                        readingPositions.Clear();
                        chooser.gameObject.SetActive(false); render();
                    }, localize: false), "banlist:name:" + name);
                    SetLibrarySelection(button, name == listName);
                }
                if (matches.Length == 0) FightUI.Text(nameRows, "没有匹配项", 8, 8, inner - 56, 44, 18, FightUI.Muted);
                nameRows.sizeDelta = new Vector2(inner - 24, Mathf.Max(height - 240, matches.Length * 52));
                nameScroll.verticalNormalizedPosition = 1;
            };
            FightUI.FocusKey(FightUI.Input(chooser, "", "搜索禁限表名称", 12, 12, inner - 172, 44,
                text => { nameQuery = text; renderChoices(); }), "banlist:name:search");
            FightUI.Button(chooser, "取消", inner - 148, 12, 136, 44, () => chooser.gameObject.SetActive(false));

            render = () =>
            {
                FightUI.Clear(contents);
                var selection = FightUI.FocusKey(FightUI.Button(contents, BattleBanlistLabel(listName), 0, 0, inner - (chosen == null ? 0 : 164), 44, () =>
                {
                    renderChoices(); chooser.gameObject.SetActive(true); chooser.SetAsLastSibling();
                }, localize: false), "banlist:select");
                FightUI.RawHint(selection.gameObject, BattleBanlistLabel(listName));
                if (chosen != null) FightUI.FocusKey(FightUI.Button(contents, actionLabel, inner - 152, 0, 152, 44,
                    () => { CloseDialog(); chosen(listName); }, true), "banlist:apply");

                var groups = new[] { list.BannedIds.Concat(list.LimitedIds).Concat(list.SemiLimitedIds).Concat(list.UnlimitedIds).Distinct().ToArray(),
                    list.BannedIds.Distinct().ToArray(), list.LimitedIds.Distinct().ToArray(), list.SemiLimitedIds.Distinct().ToArray(), list.UnlimitedIds.Distinct().ToArray() };
                var labels = new[] { "全部", "禁止（0张）", "限制（1张）", "准限制（2张）", "允许（3张）" };
                int tabCount = groups[4].Length == 0 ? 4 : 5;
                for (int i = 0; i < tabCount; i++)
                {
                    int tabIndex = i;
                    float tabWidth = (inner - (tabCount - 1) * 8) / tabCount;
                    FightUI.FocusKey(FightUI.Tab(contents, FightText.Get(labels[i]) + " · " + groups[i].Length, category == i,
                        i * (tabWidth + 8), 56, tabWidth, 42, () => { category = tabIndex; selectedCode = 0; render(); }), "banlist:category:" + i);
                }
                var cardIds = groups[category];
                var matches = string.IsNullOrWhiteSpace(query) ? cardIds.ToList() : MDPro3.Duel.YGOSharp.CardsManager.Search(query, cardIds).Select(card => card.Id).ToList();
                if (selectedCode == 0 || !matches.Contains(selectedCode)) selectedCode = matches.FirstOrDefault();
                var inspector = FightUI.Panel(contents, 0, 112, inspectorWidth, height - 220, FightUI.PanelColor, false, "Banlist card inspector");
                var rows = FightUI.Scroll(contents, gridX, 168, gridWidth, height - 276, height - 276, out var scroll);
                frames.Clear();
                Action<int> inspect = code =>
                {
                    if (selectedCode == code && inspector.childCount > 0) return;
                    selectedCode = code;
                    RenderClientCardInspector(inspector, code, frames, readingPositions, "banlist", "选择卡片查看详情",
                        cardCode => OpenRetainedDialog(() => { ShowCard(cardCode); return clientModalBox; }));
                };
                float y = 12;
                int columns = Mathf.Max(4, Mathf.FloorToInt((gridWidth - 32) / (Compact ? 90 : 112)));
                float cardWidth = (gridWidth - 32 - (columns - 1) * 10) / columns, cardHeight = cardWidth * 1.46f;
                var matched = new HashSet<int>(matches);
                for (int group = 1; group < groups.Length; group++)
                {
                    if (category != 0 && category != group) continue;
                    var codes = groups[group].Where(matched.Contains).ToArray();
                    if (codes.Length == 0) continue;
                    FightUI.Text(rows, labels[group], 16, y, gridWidth - 110, 34, Compact ? 22 : 25, FightUI.Foreground);
                    FightUI.Text(rows, codes.Length + " 张", gridWidth - 94, y, 78, 34, 17, FightUI.Gold, TextAlignmentOptions.MidlineRight);
                    FightUI.Line(rows, 16, y + 40, gridWidth - 32); y += 56;
                    for (int i = 0; i < codes.Length; i++)
                    {
                        int code = codes[i]; float x = 16 + i % columns * (cardWidth + 10), cy = y + i / columns * (cardHeight + 14);
                        var image = FightUI.Art(rows, code, x, cy, cardWidth, cardHeight, true);
                        var button = FightUI.FocusKey(FightUI.CardButton(image, () => inspect(code)), "banlist:card:" + code);
                        FightUI.PreviewOnFocus(button, () => inspect(code));
                        frames.Add(Tuple.Create(code, image.transform.parent.GetComponent<Outline>()));
                        FightUI.RawHint(button.gameObject, MDPro3.Duel.YGOSharp.CardsManager.Get(code).Name);
                        var badge = FightUI.Panel(image.transform, 4, 4, 26, 26, FightUI.Background, false, "Allowed copies");
                        FightUI.Text(badge, (group - 1).ToString(), 0, 0, 26, 26, 17,
                            group == 1 ? FightUI.Danger : group == 2 ? FightUI.Gold : FightUI.Active, TextAlignmentOptions.Center);
                    }
                    y += Mathf.Ceil((float)codes.Length / columns) * (cardHeight + 14) + 30;
                }
                if (matches.Count == 0) FightUI.Text(rows, "没有匹配项", 16, 16, gridWidth - 32, 64, 18, FightUI.Muted);
                rows.sizeDelta = new Vector2(gridWidth, Mathf.Max(height - 276, y));
                scroll.verticalNormalizedPosition = 1;
                inspect(selectedCode);
            };
            render();
            FightUI.FocusKey(FightUI.Input(main, query, "搜索卡名、卡号或效果", gridX, 112, gridWidth, 40,
                text => { query = text; render(); }), "banlist:card:search");
        }
    }
}
