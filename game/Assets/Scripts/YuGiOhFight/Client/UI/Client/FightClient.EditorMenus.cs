using System;
using System.Linq;

using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI.Client
{
    internal partial class FightClient
    {
        private Vector2 MeasureEditorMenuRow(RectTransform box, float width, string leftTitle, string leftSubtitle, string rightTitle, string rightSubtitle)
        {
            var measure = FightUI.Text(box, "", 0, 0, width - 74, 100, 20);
            measure.gameObject.SetActive(false);
            float titleHeight = Mathf.Max(28,
                Mathf.Max(measure.GetPreferredValues(FightText.Get(leftTitle), width - 74, float.PositiveInfinity).y,
                    measure.GetPreferredValues(FightText.Get(rightTitle), width - 74, float.PositiveInfinity).y) + 2);
            measure.fontSize = 14;
            float subtitleHeight = string.IsNullOrEmpty(leftSubtitle) && string.IsNullOrEmpty(rightSubtitle) ? 0 : Mathf.Max(24,
                Mathf.Max(measure.GetPreferredValues(FightText.Get(leftSubtitle), width - 74, float.PositiveInfinity).y,
                    measure.GetPreferredValues(FightText.Get(rightSubtitle), width - 74, float.PositiveInfinity).y) + 2);
            Destroy(measure.gameObject);
            return new Vector2(titleHeight, subtitleHeight);
        }

        private float EditorMenuItemHeight(Vector2 metrics) => Mathf.Max(72, 18 + metrics.x + metrics.y);

        private void EditorMenuItem(RectTransform box, string title, string subtitle, string icon,
            float x, float y, float width, Action action, bool destructive = false, bool enabled = true, Vector2? measured = null)
        {
            var metrics = measured ?? new Vector2(28, 24);
            float height = EditorMenuItemHeight(metrics);
            var button = FightUI.Button(box, title, x, y, width, height, () => { CloseDialog(); Try(action); }, false, enabled);
            FightUI.FocusKey(button, "editor-menu:" + title);
            button.GetComponentInChildren<TextMeshProUGUI>().gameObject.SetActive(false);
            var color = !enabled ? FightUI.Muted : destructive ? FightUI.Danger : FightUI.Gold;
            FightIcon symbol = FightUI.Icon(button.transform, icon, 16, (height - 28) / 2, 28, 28, color);
            var label = FightUI.Text(button.transform, title, 58, metrics.y > 0 ? 8 : (height - metrics.x) / 2, width - 74, metrics.x, 20,
                !enabled ? FightUI.Muted : destructive ? color : FightUI.Foreground, TextAlignmentOptions.MidlineLeft);
            label.raycastTarget = false;
            TextMeshProUGUI explanation = null;
            if (!string.IsNullOrEmpty(subtitle))
            {
                explanation = FightUI.Text(button.transform, subtitle, 58, 10 + metrics.x, width - 74, metrics.y, 14, FightUI.Muted, TextAlignmentOptions.MidlineLeft);
                explanation.enableWordWrapping = true;
                explanation.raycastTarget = false;
            }
            if (!enabled)
            {
                symbol.canvasRenderer.SetAlpha(.48f);
                label.canvasRenderer.SetAlpha(.7f);
                // The unavailable reason is still useful reading content.
                if (explanation != null) explanation.canvasRenderer.SetAlpha(1f);
            }
            if (destructive) FightUI.StyleDestructiveButton(button);
            if (!string.IsNullOrEmpty(subtitle)) FightUI.Hint(button.gameObject, subtitle);
        }

        private void OpenEditorSectionJump()
        {
            if (clientEditorUI == null || !clientEditorUI.DeckView.deckLoaded) return;
            var box = ShowDialog("定位构筑区域", 480, 314);
            DeckView view = clientEditorUI.DeckView;
            var locations = new[] { DeckView.DeckLocation.MainDeck, DeckView.DeckLocation.ExtraDeck, DeckView.DeckLocation.SideDeck };
            var names = new[] { "主卡组", "额外卡组", "副卡组" };
            for (int i = 0; i < locations.Length; i++)
            {
                DeckView.DeckLocation location = locations[i];
                var button = FightUI.Button(box, names[i] + " · " + view.GetDeckLocationCount(location) + " 张", 28, 86 + i * 66, box.rect.width - 56, 52,
                    () => { CloseDialog(); JumpToEditorSection(location); });
                FightUI.FocusKey(button, "editor-section:" + location);
                FightUI.Hint(button.gameObject, "滚动到" + names[i] + "标题");
            }
        }

        private void JumpToEditorSection(DeckView.DeckLocation location)
        {
            EditorDeckSection section = editorDeckSections.FirstOrDefault(item => item.Location == location);
            var scroll = clientEditorUI?.DeckView?.ScrollRect;
            if (section == null || scroll?.content == null || scroll.viewport == null) return;
            Canvas.ForceUpdateCanvases();
            float range = Mathf.Max(0, scroll.content.rect.height - scroll.viewport.rect.height);
            float offset = Mathf.Clamp(-section.Root.anchoredPosition.y, 0, range);
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = range > 0 ? 1 - offset / range : 1;
        }

        internal void OpenEditorMore()
        {
            if (clientEditorUI == null || !clientEditorUI.DeckView.deckLoaded) return;
            ServantUI.DeckEditorUI ui = clientEditorUI;
            var box = ShowDialog("卡组操作", 760, 476);
            var w = (box.rect.width - 72) / 2;
            bool dirty = ui.DeckView.GetDirty();
            int cardCount = ui.DeckView.mainCount + ui.DeckView.extraCount + ui.DeckView.sideCount;
            string resetSubtitle = dirty ? "恢复打开时的构筑" : "当前没有未保存的修改";
            string clearSubtitle = cardCount > 0 ? "移除全部 " + cardCount + " 张卡片" : "当前构筑已经为空";
            var rows = new[]
            {
                MeasureEditorMenuRow(box, w, "展开教程", "", "手牌测试设置", ""),
                MeasureEditorMenuRow(box, w, "YDKE 导入 / 导出", "", "分享卡组", ""),
                MeasureEditorMenuRow(box, w, "复制卡组", "", "打乱顺序", ""),
                MeasureEditorMenuRow(box, w, "重置修改", resetSubtitle, "清空构筑", clearSubtitle)
            };
            float secondY = 88 + EditorMenuItemHeight(rows[0]) + 12;
            float thirdY = secondY + EditorMenuItemHeight(rows[1]) + 12;
            float dividerY = thirdY + EditorMenuItemHeight(rows[2]) + 20, destructiveY = dividerY + 16;
            float height = destructiveY + EditorMenuItemHeight(rows[3]) + 12;
            FightUI.Place(box, (W - box.rect.width) / 2, (H - height) / 2, box.rect.width, height);
            clientModalSize = new Vector2(box.rect.width, height);
            EditorMenuItem(box, "展开教程", "", "book", 28, 88, w, OpenEditorTutorial, measured: rows[0]);
            EditorMenuItem(box, "手牌测试设置", "", "play", 44 + w, 88, w, OpenClientHandTest, measured: rows[0]);
            EditorMenuItem(box, "YDKE 导入 / 导出", "", "deck", 28, secondY, w, OpenEditorYdke, measured: rows[1]);
            EditorMenuItem(box, "分享卡组", "", "share", 44 + w, secondY, w, ui.OnShare, measured: rows[1]);
            EditorMenuItem(box, "复制卡组", "", "deck", 28, thirdY, w, ui.OnCopy, measured: rows[2]);
            EditorMenuItem(box, "打乱顺序", "", "shuffle", 44 + w, thirdY, w, ui.OnRandom, measured: rows[2]);
            FightUI.Line(box, 28, dividerY, box.rect.width - 56, FightUI.Border);
            EditorMenuItem(box, "重置修改", resetSubtitle, "refresh", 28, destructiveY, w,
                () => Confirm("重置当前修改", "将恢复打开时的构筑，尚未保存的修改会被放弃。", ui.OnReset), true, dirty, rows[3]);
            EditorMenuItem(box, "清空构筑", clearSubtitle, "close", 44 + w, destructiveY, w,
                () => Confirm("清空当前构筑", "将移除当前构筑中的全部卡片，保存后才会写入文件。", ui.OnClearDeck), true, cardCount > 0, rows[3]);
        }

        private void OpenEditorYdke()
        {
            var box = ShowDialog("YDKE 导入 / 导出", 760, 440);
            MDPro3.Duel.YGOSharp.Deck deck = clientEditorUI.DeckView.FromObjectDeckToCodedDeck();
            var exported = MDPro3.YdkeConverter.DeckToYdke(deck);
            FightUI.Text(box, "当前构筑 · 主卡 " + deck.Main.Count + " · 额外 " + deck.Extra.Count + " · 副卡 " + deck.Side.Count,
                28, 88, box.rect.width - 56, 28, 17, FightUI.Gold);
            FightUI.Text(box, "YDKE 文本", 28, 122, 280, 22, 15, FightUI.Muted);
            var input = FightUI.Input(box, exported, "ydke://…", 28, 148, 704, 170, null, false, true);
            FightUI.FocusKey(input, "editor-ydke:input");
            var error = FightUI.Text(box, "导入会替换当前构筑，文件在保存后更新。", 28, 327, 704, 30, 16, FightUI.Muted);
            Button importButton = null;
            input.onValueChanged.AddListener(value =>
            {
                error.text = "导入会替换当前构筑，文件在保存后更新。"; error.color = FightUI.Muted;
                input.GetComponent<Outline>().effectColor = FightUI.Border;
                if (importButton != null) importButton.interactable = !string.IsNullOrWhiteSpace(value);
            });
            FightUI.TextAction(box, "粘贴剪贴板", 568, 118, 164, 30, () =>
            {
                input.text = GUIUtility.systemCopyBuffer ?? "";
                input.Select(); input.ActivateInputField(); input.caretPosition = input.text.Length;
            });
            FightUI.Button(box, "取消", 28, 370, 144, 44, CloseDialog);
            Button copyButton = null;
            copyButton = FightUI.Button(box, "复制当前构筑", 320, 370, 188, 44, () =>
            {
                GUIUtility.systemCopyBuffer = exported;
                copyButton.GetComponentInChildren<TextMeshProUGUI>().text = "已复制";
            });
            FightUI.Hint(copyButton.gameObject, "复制编辑器中的当前构筑，包含尚未保存的修改");
            importButton = FightUI.Button(box, "导入构筑", 524, 370, 208, 44, () =>
            {
                MDPro3.Duel.YGOSharp.Deck imported = MDPro3.YdkeConverter.Ydke2Deck(input.text);
                if (imported == null || imported.Main.Concat(imported.Extra).Concat(imported.Side).Any(code => MDPro3.Duel.YGOSharp.CardsManager.Get(code).Id == 0))
                {
                    error.text = "文本无效或包含未知卡片，请检查 YDKE 格式。"; error.color = FightUI.Danger;
                    input.GetComponent<Outline>().effectColor = FightUI.Danger;
                    input.Select(); input.ActivateInputField(); return;
                }
                CloseDialog(); clientEditorUI.DeckView.ImportCardLists(imported);
            }, true, !string.IsNullOrWhiteSpace(input.text));
        }

        private void OpenEditorCardMore(MDPro3.Duel.YGOSharp.Card card)
        {
            if (clientEditorUI == null) return;
            ServantUI.DeckEditorUI ui = clientEditorUI;
            var box = ShowDialog("卡片操作", 660, 528);
            var name = FightUI.RawText(box, card.Name, 28, 86, box.rect.width - 56, 104, 22, FightUI.Foreground);
            name.enableWordWrapping = true; name.enableAutoSizing = true;
            name.fontSizeMin = 20; name.fontSizeMax = 22;
            float nameHeight = Mathf.Clamp(name.GetPreferredValues(card.Name, box.rect.width - 56, float.PositiveInfinity).y + 6, 40, 104);
            name.rectTransform.sizeDelta = new Vector2(box.rect.width - 56, nameHeight);
            float offset = nameHeight - 40 + 28;
            FightUI.RawHint(name.gameObject, card.Name);
            int copies = ui.DeckView.GetCardCount(card.Id);
            FightUI.Text(box, copies > 0 ? "当前构筑中有 " + copies + " 张同名卡片" : "当前构筑尚未加入这张卡片", 28, 90 + nameHeight, box.rect.width - 244, 24, 15, copies > 0 ? FightUI.Gold : FightUI.Muted);
            var locate = FightUI.TextAction(box, "定位这张卡  ↗", box.rect.width - 200, 86 + nameHeight, 172, 32,
                () => LocateEditorDeckCard(card), FindEditorDeckCard(card) != null);
            FightUI.FocusKey(locate, "editor-card:locate");
            FightUI.Hint(locate.gameObject, copies > 0 ? "滚动并聚焦构筑中的这张卡片，保留卡文阅读位置" : "构筑中暂无这张卡片，加入后可以定位");
            var w = (box.rect.width - 72) / 2;
            bool canRemove = copies > 0;
            var bookmarked = MDPro3.CardRarity.CardBookmarked(card.Id);
            string removeSubtitle = canRemove ? "" : "构筑中暂无这张卡片", bookmarkTitle = bookmarked ? "取消收藏" : "收藏卡片";
            var firstRow = MeasureEditorMenuRow(box, w, "加入构筑", "", "移除卡片", removeSubtitle);
            var secondRow = MeasureEditorMenuRow(box, w, "关联卡片", "", bookmarkTitle, "");
            float secondY = 144 + offset + EditorMenuItemHeight(firstRow) + 12;
            float dividerY = secondY + EditorMenuItemHeight(secondRow) + 24;
            float height = dividerY + 132;
            FightUI.Place(box, (W - box.rect.width) / 2, (H - height) / 2, box.rect.width, height);
            clientModalSize = new Vector2(box.rect.width, height);
            EditorMenuItem(box, "加入构筑", "", "deck", 28, 144 + offset, w, () => ui.AddCard(card), measured: firstRow);
            EditorMenuItem(box, "移除卡片", removeSubtitle, "close", 44 + w, 144 + offset, w,
                () => ui.RemoveCard(card), enabled: canRemove, measured: firstRow);
            EditorMenuItem(box, "关联卡片", "", "search", 28, secondY, w, () => ui.ShowRelatedCard(card), measured: secondRow);
            EditorMenuItem(box, bookmarkTitle, "", "bookmark", 44 + w, secondY, w,
                () => { if (bookmarked) ui.UnbookmarkCard(card.Id); else ui.BookmarkCard(card.Id); }, measured: secondRow);
            FightUI.Line(box, 28, dividerY, box.rect.width - 56);
            FightUI.Text(box, "卡面样式", 28, dividerY + 16, 604, 30, 17, FightUI.Gold);
            var rarities = new[] { MDPro3.CardRarity.Rarity.Normal, MDPro3.CardRarity.Rarity.Shine, MDPro3.CardRarity.Rarity.Royal, MDPro3.CardRarity.Rarity.Gold, MDPro3.CardRarity.Rarity.Millennium };
            var labels = new[] { "普通", "闪卡", "皇室", "黄金", "千年" };
            var bw = (box.rect.width - 88) / 5;
            for (int i = 0; i < rarities.Length; i++)
            {
                MDPro3.CardRarity.Rarity rarity = rarities[i];
                bool selected = MDPro3.CardRarity.GetRarity(card.Id) == rarity;
                var rarityButton = FightUI.Button(box, labels[i], 28 + i * (bw + 8), dividerY + 64, bw, 44, () =>
                {
                    MDPro3.CardRarity.SetRarity(card.Id, rarity);
                    ui.CardCollectionView.RefreshRarity(card.Id); ui.DeckView.RefreshRarity(card.Id);
                    ui.CardDetailView.RefreshRarity(card.Id); CloseDialog();
                }, selected);
                FightUI.FocusKey(rarityButton, "editor-rarity:" + rarity);
                if (selected)
                {
                    FightUI.Icon(rarityButton.transform, "check", 10, 16, 12, 12, FightUI.Active);
                    FightUI.Place(rarityButton.GetComponentInChildren<TextMeshProUGUI>().rectTransform, 24, 0, bw - 30, 44);
                }
                FightUI.Hint(rarityButton.gameObject, (selected ? "当前卡面样式：" : "设为卡面样式：") + labels[i]);
            }
        }
    }
}
