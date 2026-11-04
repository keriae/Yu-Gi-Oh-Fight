using System;
using System.Linq;
using MDPro3.Duel.YGOSharp;
using TMPro;
using UnityEngine;

namespace MDPro3.UI.Client
{
    internal partial class FightClient
    {
        private void EditorMenuItem(RectTransform box, string title, string subtitle, string icon,
            float x, float y, float width, Action action, bool destructive = false)
        {
            var button = FightUI.Button(box, title, x, y, width, 56, () => { CloseDialog(); Try(action); });
            button.GetComponentInChildren<TextMeshProUGUI>().gameObject.SetActive(false);
            var color = destructive ? FightUI.Danger : FightUI.Gold;
            FightUI.Icon(button.transform, icon, 16, 14, 28, 28, color);
            var label = FightUI.Text(button.transform, title, 58, 0, width - 74, 56, 20, destructive ? color : FightUI.Foreground, TextAlignmentOptions.MidlineLeft);
            label.raycastTarget = false;
            FightUI.Hint(button.gameObject, subtitle);
        }

        internal void OpenEditorMore()
        {
            if (clientEditorUI == null || !clientEditorUI.DeckView.deckLoaded) return;
            var ui = clientEditorUI;
            var box = ShowDialog("卡组操作", 760, 384);
            var w = (box.rect.width - 72) / 2;
            EditorMenuItem(box, "展开教程", "查阅教学构筑与练习", "book", 28, 88, w, OpenEditorTutorial);
            EditorMenuItem(box, "手牌测试设置", "设置起手张数与测试方式", "play", 44 + w, 88, w, OpenClientHandTest);
            EditorMenuItem(box, "YDKE 导入 / 导出", "通过文本交换完整构筑", "deck", 28, 156, w, OpenEditorYdke);
            EditorMenuItem(box, "分享卡组", "打开分享页并复制链接", "share", 44 + w, 156, w, ui.OnShare);
            EditorMenuItem(box, "复制卡组", "创建副本，保存后写入个人卡库", "deck", 28, 224, w, ui.OnCopy);
            EditorMenuItem(box, "打乱顺序", "随机排列当前构筑中的卡片", "shuffle", 44 + w, 224, w, ui.OnRandom);
            FightUI.Line(box, 28, 292, box.rect.width - 56);
            EditorMenuItem(box, "重置修改", "恢复打开时的构筑", "refresh", 28, 304, w,
                () => Confirm("重置当前修改", "将恢复打开时的构筑，尚未保存的修改会被放弃。", ui.OnReset), true);
            EditorMenuItem(box, "清空构筑", "移除主卡、额外卡与副卡", "close", 44 + w, 304, w,
                () => Confirm("清空当前构筑", "将移除当前构筑中的全部卡片，保存后才会写入文件。", ui.OnClearDeck), true);
        }

        private void OpenEditorYdke()
        {
            var box = ShowDialog("YDKE 导入 / 导出", 760, 440);
            var exported = YdkeConverter.DeckToYdke(clientEditorUI.DeckView.FromObjectDeckToCodedDeck());
            var input = FightUI.Input(box, exported, "ydke://…", 28, 148, 704, 170, null, false, true);
            var error = FightUI.Text(box, "导入会替换当前构筑，文件在保存后更新。", 28, 327, 704, 30, 16, FightUI.Muted);
            FightUI.Button(box, "取消", 28, 370, 144, 44, CloseDialog);
            FightUI.Button(box, "复制当前构筑", 320, 370, 188, 44, () =>
            { GUIUtility.systemCopyBuffer = exported; ShowStatus("已复制当前构筑的 YDKE 文本。"); });
            FightUI.Button(box, "导入构筑", 524, 370, 208, 44, () =>
            {
                var deck = YdkeConverter.Ydke2Deck(input.text);
                if (deck == null || deck.Main.Concat(deck.Extra).Concat(deck.Side).Any(code => CardsManager.Get(code).Id == 0))
                { error.text = "文本无效或包含未知卡片，请检查 YDKE 格式。"; error.color = FightUI.Danger; return; }
                CloseDialog(); clientEditorUI.DeckView.ImportCardLists(deck);
            }, true);
        }

        private void OpenEditorCardMore(Card card)
        {
            if (clientEditorUI == null) return;
            var ui = clientEditorUI;
            var box = ShowDialog("卡片操作", 660, 504);
            var name = FightUI.Text(box, card.Name, 28, 86, 604, 40, 22, FightUI.Foreground);
            FightUI.Hint(name.gameObject, card.Name);
            var w = (box.rect.width - 72) / 2;
            EditorMenuItem(box, "加入构筑", "增加一张当前卡片", "deck", 28, 144, w, () => ui.AddCard(card));
            EditorMenuItem(box, "移除卡片", "从构筑中移除一张", "close", 44 + w, 144, w, () => ui.RemoveCard(card));
            EditorMenuItem(box, "关联卡片", "在右侧卡池查看相关卡片", "search", 28, 212, w, () => ui.ShowRelatedCard(card));
            var bookmarked = CardRarity.CardBookmarked(card.Id);
            EditorMenuItem(box, bookmarked ? "取消收藏" : "收藏卡片", "管理个人常用卡片", "bookmark", 44 + w, 212, w,
                () => { if (bookmarked) ui.UnbookmarkCard(card.Id); else ui.BookmarkCard(card.Id); });
            FightUI.Line(box, 28, 328, box.rect.width - 56);
            FightUI.Text(box, "卡面样式", 28, 344, 604, 30, 17, FightUI.Gold);
            var rarities = new[] { CardRarity.Rarity.Normal, CardRarity.Rarity.Shine, CardRarity.Rarity.Royal, CardRarity.Rarity.Gold, CardRarity.Rarity.Millennium };
            var labels = new[] { "普通", "闪卡", "皇室", "黄金", "千年" };
            var bw = (box.rect.width - 88) / 5;
            for (int i = 0; i < rarities.Length; i++)
            {
                var rarity = rarities[i];
                FightUI.Button(box, labels[i], 28 + i * (bw + 8), 386, bw, 44, () =>
                {
                    CardRarity.SetRarity(card.Id, rarity);
                    ui.CardCollectionView.RefreshRarity(card.Id); ui.DeckView.RefreshRarity(card.Id);
                    ui.CardDetailView.RefreshRarity(card.Id); CloseDialog();
                }, CardRarity.GetRarity(card.Id) == rarity);
            }
            FightUI.Text(box, "卡面样式与收藏会自动保存", 28, 454, 604, 24, 15, FightUI.Muted);
        }
    }
}
