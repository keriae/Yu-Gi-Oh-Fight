using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MDPro3.AI;
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
        private readonly Dictionary<string, string> cpuNumberDrafts = new Dictionary<string, string>();
        private readonly HashSet<string> invalidCpuFields = new HashSet<string>();
        private DeckAcademyDeck BlueEyes => academy.decks.FirstOrDefault(d => d.id == "blue-eyes")
            ?? academy.decks.FirstOrDefault(d => d.name.Contains("青眼"));
        private void RenderHome()
        {
            bool large = W >= 2400;
            float side = Compact ? 340 : 504;
            float main = W - side - Gap * 2;
            float homeH = H - Footer - BodyTop, mainH = homeH - Gap;
            float heroH = Mathf.Min(Compact ? 330 : large ? 620 : 520, mainH * .68f);
            var hero = FightUI.Panel(Body, 0, 0, main, heroH, FightUI.Background, true, "Blue-Eyes feature");
            // Preserve the dragon's face and silhouette when the feature becomes
            // wider. The artwork's dark left edge joins the text surface instead
            // of cropping the top and bottom of a stretched panorama.
            float illustrationW = Mathf.Min(main - 2, (heroH - 2) * (2736f / 1040f));
            FightUI.Asset(hero, "hero.png", main - illustrationW - 1, 1, illustrationW, heroH - 2);
            float pad = Compact ? 32 : large ? 64 : 56;
            FightUI.Text(hero, "经典主题 · 青眼", pad, heroH * .29f, main * .5f, 36, Compact ? 18 : 24, FightUI.Muted);
            FightUI.HeroTitle(hero, "唤醒传说中的白龙", pad, heroH * .40f, main * .68f, Compact ? 80 : 108, Compact ? 40 : 60);
            float actionY = heroH * .75f;
            bool canPractice = BlueEyes != null && DeckAcademy.CanPractice(BlueEyes);
            var practice = FightUI.Button(hero, "开始练习  →", pad, actionY, Compact ? 192 : 248, Compact ? 56 : 64,
                () => StartCore(() => DeckAcademy.StartPractice(BlueEyes)), true, canPractice);
            if (!canPractice) FightUI.Hint(practice.gameObject, "青眼练习资源暂不可用，请检查本地教学资源。");
            FightUI.TextAction(hero, "查看构筑", pad + (Compact ? 220 : 280), actionY, Compact ? 160 : 200, Compact ? 56 : 64,
                () => { detailEntry = recommendations.FirstOrDefault(e => e.Academy?.id == BlueEyes?.id); detailOrigin = "home"; if (detailEntry != null) Navigate("deck-detail"); });
            float sectionY = heroH + (Compact ? 14 : 20);
            FightUI.Text(Body, "探索决斗", 16, sectionY, Compact ? 126 : 150, 40, Compact ? 24 : 28, FightUI.Gold);
            FightUI.OrnamentLine(Body, Compact ? 150 : 184, sectionY + 18, main - (Compact ? 166 : 200));
            float cardY = heroH + Gap + (Compact ? 40 : 48), cardW = (main - Gap * 2) / 3;
            float captionHeight = Compact ? 64 : 80;
            float cardH = Mathf.Max(150, Mathf.Min(mainH - cardY,
                Mathf.Min(large ? 480 : 300, cardW * .56f + captionHeight)));
            string[] titles = { "卡组学院", "推荐卡组", "残局挑战" };
            string[] route = { "academy", "recommended", "puzzles" }, assets = { "academy.png", "decks.png", "puzzle.png" };
            for (int i = 0; i < 3; i++)
            {
                int j = i;
                var card = FightUI.Panel(Body, i * (cardW + Gap), cardY, cardW, cardH, FightUI.PanelColor, true, titles[i]);
                float captionH = captionHeight;
                float imageH = cardH - captionH - 1;
                FightUI.Asset(card, assets[i], 1, 1, cardW - 2, imageH);
                HomeShade(card, 1, imageH - 36, cardW - 2, 36, FightUI.PanelColor);
                FightUI.Line(card, 1, cardH - captionH, cardW - 2, FightUI.GoldBorder);
                var tileTitle = FightUI.Text(card, titles[i], Compact ? 18 : 28, cardH - captionH + 12, cardW - 52, 36, Compact ? 22 : 27, FightUI.Gold);
                tileTitle.fontStyle = FontStyles.Bold;
                FightUI.Icon(card, "arrow", cardW - 50, cardH - 50, 28, 28, FightUI.Gold);
                BindHomeCard(card, () => Navigate(route[j]));
            }
            var right = FightUI.Panel(Body, main + Gap, 0, side, homeH, FightUI.PanelColor, false, "Personal sidebar");
            FightUI.Panel(right, 0, 0, 1, homeH, FightUI.Border, false, "Sidebar divider");
            var content = FightUI.Scroll(right, 0, 0, side, homeH, homeH);
            float p = Compact ? 16 : 32, inner = side - p * 2;
            float avatarSize = Compact ? 54 : 96;
            HomeAvatar(content, p + (Compact ? 2 : 8), Compact ? 8 : 18, avatarSize, 0);
            var name = Config.Get("DuelPlayerName0", "").Trim(); if (string.IsNullOrEmpty(name)) name = "决斗者";
            float nameX = p + avatarSize + (Compact ? 16 : 28);
            float nameY = Compact ? 11 : 33;
            var playerName = HomeDeckTitle(content, name, nameX, nameY, side - nameX - p, 38, Compact ? 24 : 28, FightUI.Foreground);
            float profileDividerY = Mathf.Max(Compact ? 70 : 136, nameY + playerName.rectTransform.sizeDelta.y + 16);
            FightUI.Line(content, p, profileDividerY, inner);
            float deckTitleY = profileDividerY + (Compact ? 12 : 20);
            FightUI.Text(content, "当前卡组", p, deckTitleY, Compact ? 140 : 164, 36, Compact ? 22 : 28, FightUI.Gold);
            FightUI.OrnamentLine(content, p + (Compact ? 144 : 176), deckTitleY + 17, inner - (Compact ? 144 : 176));
            string active = Config.GetConfigDeckName(), path = Program.PATH_DECK + active + Program.EXPANSION_YDK;
            bool exists = !string.IsNullOrEmpty(active) && File.Exists(path);
            float deckY = deckTitleY + (Compact ? 32 : 46), deckH = Compact ? 88 : 144, thumb = Compact ? 68 : 120;
            var deckPanel = FightUI.Panel(content, p, deckY, inner, deckH, FightUI.Raised, false, "Current deck");
            if (exists) FightUI.Art(deckPanel, DeckLibrary.CoverFor(new Deck(path)), 12, 12, thumb, thumb);
            else FightUI.Panel(deckPanel, 12, 12, thumb, thumb, FightUI.Background, true, "No current deck");
            float deckNameX = thumb + 32;
            var deckName = HomeDeckTitle(deckPanel, exists ? active : "尚未选择卡组", deckNameX, Compact ? 9 : 20,
                inner - deckNameX - 16, Compact ? 42 : 68, Compact ? 19 : 25, FightUI.Gold);
            deckH = Mathf.Max(deckH, (Compact ? 9 : 20) + deckName.rectTransform.sizeDelta.y + 16);
            deckPanel.sizeDelta = new Vector2(inner, deckH);
            float by = Mathf.Max(Compact ? 214 : 362, deckY + deckH + (Compact ? 12 : 16)), buttonH = Compact ? 42 : 50;
            var editCurrent = FightUI.Button(content, "编辑卡组", p, by, (inner - 16) / 2, buttonH, () => OpenEditor(active), false, exists);
            if (!exists) FightUI.Hint(editCurrent.gameObject, "请先更换或选择当前卡组。");
            FightUI.Button(content, "更换卡组", p + (inner + 16) / 2, by, (inner - 16) / 2, buttonH,
                () => PickDeck("当前使用", "home", picked => SetCurrent(picked), true));
            float sectionDividerY = Mathf.Max(Compact ? 270 : 440, by + buttonH + (Compact ? 14 : 24));
            FightUI.Line(content, p, sectionDividerY, inner);
            float tutorialY = sectionDividerY + (Compact ? 14 : 18), tutorialH = Compact ? 168 : 250, tutorialPad = Compact ? 16 : 20;
            var teaching = FightUI.Panel(content, p, tutorialY, inner, tutorialH, FightUI.Raised, false, "Blue-Eyes opening");
            FightUI.Text(teaching, "学习青眼展开", tutorialPad, Compact ? 10 : 16, inner - tutorialPad * 2, 36, Compact ? 20 : 26, FightUI.Gold);
            float handW = (inner - tutorialPad * 2 - 24) / 5, handH = handW * 1.45f, handY = Compact ? 44 : 68;
            if (BlueEyes?.starter_hand != null)
                for (int i = 0; i < Mathf.Min(5, BlueEyes.starter_hand.Length); i++)
                    FightUI.Art(teaching, BlueEyes.starter_hand[i], tutorialPad + i * (handW + 6), handY, handW, handH, true);
            FightUI.Line(teaching, tutorialPad, Compact ? 122 : 196, inner - tutorialPad * 2);
            var handPractice = FightUI.TextAction(teaching, "固定起手练习  →", tutorialPad, Compact ? 128 : 206, inner - tutorialPad * 2, Compact ? 40 : 44,
                () => StartCore(() => DeckAcademy.StartPractice(BlueEyes)), canPractice);
            if (!canPractice)
            {
                FightUI.Hint(handPractice.gameObject, "青眼练习资源暂不可用，请检查本地教学资源。");
                if (BlueEyes?.starter_hand == null || BlueEyes.starter_hand.Length == 0)
                    FightUI.Text(teaching, "练习资源暂不可用", tutorialPad, handY, inner - tutorialPad * 2, handH, 16, FightUI.Muted, TextAlignmentOptions.Center);
            }
            float shortcuts = tutorialY + tutorialH + (Compact ? 14 : 24), shortcutH = Compact ? 46 : 60;
            HomeShortcut(content, "◇   AI 对战       →", p, shortcuts, inner, shortcutH, () => Navigate(battle.AiVsAi ? "spectator" : "ai"));
            HomeShortcut(content, "▷   观看回放       →", p, shortcuts + shortcutH + (Compact ? 14 : 20), inner, shortcutH, () => Navigate("replays"));
            content.sizeDelta = new Vector2(side, Mathf.Max(homeH, shortcuts + shortcutH * 2 + (Compact ? 24 : 40)));
        }
        private static Sprite homeCircleMask, homeCircleRing;
        private void HomeAvatar(Transform parent, float x, float y, float size, int player)
        {
            if (homeCircleMask == null)
            {
                var mask = new Texture2D(96, 96, TextureFormat.RGBA32, false);
                var ring = new Texture2D(96, 96, TextureFormat.RGBA32, false);
                for (int py = 0; py < 96; py++) for (int px = 0; px < 96; px++)
                {
                    float radius = Vector2.Distance(new Vector2(px + .5f, py + .5f), new Vector2(48, 48));
                    mask.SetPixel(px, py, new Color(1, 1, 1, Mathf.Clamp01(47 - radius)));
                    float alpha = Mathf.Clamp01(48 - radius) * Mathf.Clamp01(radius - 44.8f);
                    ring.SetPixel(px, py, new Color(1, 1, 1, alpha));
                }
                mask.Apply(); ring.Apply();
                homeCircleMask = Sprite.Create(mask, new Rect(0, 0, 96, 96), new Vector2(.5f, .5f));
                homeCircleRing = Sprite.Create(ring, new Rect(0, 0, 96, 96), new Vector2(.5f, .5f));
            }
            var node = FightUI.Node(parent, "Player avatar", x, y, size, size);
            var shape = node.gameObject.AddComponent<Image>(); shape.sprite = homeCircleMask; shape.color = FightUI.Raised; shape.raycastTarget = false;
            var clip = node.gameObject.AddComponent<Mask>(); clip.showMaskGraphic = true;
            var picture = FightUI.Node(node, "Profile portrait", 1, 1, size - 2, size - 2).gameObject.AddComponent<Image>();
            picture.color = Color.white; picture.raycastTarget = false; picture.preserveAspect = true;
            var face = player == 0 ? Appearance.duelFace0 : Appearance.duelFace1;
            if (face != null) picture.sprite = face; else LoadHomePortrait(picture, player);
            var edge = FightUI.Node(parent, "Avatar ring", x, y, size, size).gameObject.AddComponent<Image>();
            edge.sprite = homeCircleRing; edge.color = FightUI.Gold; edge.raycastTarget = false;
        }
        private async void LoadHomePortrait(Image picture, int player)
        {
            try
            {
                string fallback = Program.items.faces.Count > 0 ? Program.items.faces[0].id.ToString() : "1010001";
                var sprite = await Program.items.LoadConcreteItemIconAsync(Config.Get("DuelFace" + player, fallback), Items.ItemType.Face, player);
                if (picture != null && sprite != null) picture.sprite = sprite;
            }
            catch (Exception error) { Debug.LogWarning("[FightClient] portrait: " + error.Message); }
        }
        private void HomeShortcut(Transform parent, string label, float x, float y, float w, float h, Action action)
        {
            var button = FightUI.Button(parent, label, x, y, w, h, action);
            button.GetComponent<Outline>().effectColor = FightUI.Border;
            foreach (var text in button.GetComponentsInChildren<TextMeshProUGUI>())
            {
                text.text = label.Contains("AI 对战") ? "AI 对战" : "观看回放";
                text.alignment = TextAlignmentOptions.MidlineLeft;
                FightUI.Place(text.rectTransform, 56, 0, w - 104, h);
            }
            FightUI.Icon(button.transform, label.Contains("AI 对战") ? "swords" : "replay", 18, (h - 28) / 2, 28, 28, FightUI.Gold);
            FightUI.Icon(button.transform, "arrow", w - 48, (h - 28) / 2, 28, 28, FightUI.Gold);
        }
        private void RenderModes()
        {
            bool large = W >= 2400;
            PageTitle("选择决斗模式");
            float gap = Compact ? 18 : 32, cw = Mathf.Min(large ? 760 : 448, (BodyWidth - gap * 2) / 3);
            float startX = (BodyWidth - cw * 3 - gap * 2) / 2;
            float workspaceWidth = cw * 3 + gap * 2;
            float h = Mathf.Min(large ? 800 : 580, BodyHeight - 204);
            float y = large ? Mathf.Max(116, (BodyHeight - h - 100) / 2) : Compact ? 106 : 116;
            if (large) AlignHomeWorkspaceHeading(startX, workspaceWidth);
            string[] titles = { "人机对战", "AI 对战", "匹配对战" };
            string[] artwork = { "hero.png", "academy.png", "decks.png" };
            for (int i = 0; i < 3; i++)
            {
                int j = i;
                var card = FightUI.Panel(Body, startX + (cw + gap) * i, y, cw, h,
                    FightUI.PanelColor, true, titles[i]);
                card.GetComponent<Outline>().effectColor = i == 2 ? FightUI.Border : FightUI.GoldBorder;
                float artH = h - 92;
                var illustration = FightUI.Asset(card, artwork[i], 1, 1, cw - 2, artH);
                if (i == 2) illustration.color = new Color(.4f, .43f, .46f, 1);
                HomeShade(card, 1, artH - 80, cw - 2, 80, FightUI.PanelColor);
                if (i == 2) HomeBadge(card, "筹备中", 18, 18, 90, 30, FightUI.Muted);
                FightUI.Icon(card, i == 0 ? "duel" : i == 1 ? "swords" : "diamond", 24, artH + 31, 30, 30,
                    i == 2 ? FightUI.Muted : FightUI.Gold);
                var title = FightUI.Text(card, titles[i], 70, artH + 24, cw - 126, 44,
                    Compact ? 27 : 32, i == 2 ? FightUI.Muted : FightUI.Gold);
                title.fontStyle = FontStyles.Bold;
                if (i < 2) FightUI.Icon(card, "arrow", cw - 48, artH + 34, 24, 24, FightUI.Gold);
                BindHomeCard(card, () => { if (j < 2) Navigate(j == 0 ? "cpu" : battle.AiVsAi ? "spectator" : "ai"); });
                if (i == 2)
                {
                    card.GetComponent<Button>().interactable = false;
                    FightUI.Hint(card.gameObject, "多人匹配正在筹备中，暂时无法开始。");
                }
            }
            float actionX = large ? startX : 0;
            float actionY = large ? y + h + 40 : BodyHeight - 64;
            FightUI.TextAction(Body, "返回首页", actionX, actionY, 140, 56, () => Navigate("home"));
        }
        private void RenderCpu()
        {
            bool large = W >= 2400;
            PageTitle("人机对战准备");
            float listW = Compact ? 272 : 448, setupW = Compact ? 330 : large ? 480 : 400;
            float workspaceWidth = large ? Mathf.Min(2040, BodyWidth) : BodyWidth, workspaceX = (BodyWidth - workspaceWidth) / 2;
            float centerX = workspaceX + listW + Gap, centerW = workspaceWidth - listW - setupW - Gap * 2, setupX = workspaceX + workspaceWidth - setupW;
            float height = large ? Mathf.Min(920, BodyHeight - 180) : BodyHeight - (Compact ? 162 : 180);
            float top = large ? Mathf.Max(96, (BodyHeight - height - 88) / 2) : Compact ? 88 : 96;
            float actionY = large ? top + height + 32 : BodyHeight - 62;
            if (large) AlignHomeWorkspaceHeading(workspaceX, workspaceWidth);
            var bots = SoloSelector.bots;
            var roster = FightUI.Panel(Body, workspaceX, top, listW, height);
            FightUI.Text(roster, "电脑对手", 20, 15, listW - 112, 36, Compact ? 22 : 26, FightUI.Gold);
            FightUI.Text(roster, bots.Count + " 位", listW - 90, 19, 68, 30, 16, FightUI.Muted, TextAlignmentOptions.MidlineRight);
            FightUI.OrnamentLine(roster, 20, 57, listW - 40);
            float rowH = Compact ? 76 : 88;
            var list = FightUI.Scroll(roster, 8, 68, listW - 16, height - 76, bots.Count * rowH);
            for (int i = 0; i < bots.Count; i++)
            {
                int j = i; var bot = bots[i];
                var row = FightUI.Panel(list, 4, i * rowH + 3, listW - 24, rowH - 6,
                    i == selectedBot ? FightUI.Raised : FightUI.PanelColor, false, bot.name);
                if (i == selectedBot)
                {
                    FightUI.Panel(row, 0, 8, 2, rowH - 22, FightUI.Active, false);
                    FightUI.Icon(row, "diamond", listW - 52, 14, 12, 12, FightUI.Active);
                }
                float thumb = Compact ? 48 : 58;
                if (bot.main0 > 0) FightUI.Art(row, bot.main0, 12, 10, thumb, thumb);
                var botName = FightUI.Text(row, bot.name, thumb + 26, 8, listW - thumb - 78, 32,
                    Compact ? 18 : 21, i == selectedBot ? FightUI.Active : FightUI.Foreground);
                FightUI.Hint(botName.gameObject, bot.name);
                FightUI.Text(row, (bot.desc ?? "").Split('\n').FirstOrDefault(), thumb + 26, 39,
                    listW - thumb - 66, 25, Compact ? 15 : 16, FightUI.Muted);
                BindHomeCard(row, () => { selectedBot = j; Rebuild(); });
            }
            if (bots.Count == 0)
            {
                FightUI.Icon(Body, "duel", centerX + (centerW - 56) / 2, top + 60, 56, 56, FightUI.GoldBorder);
                FightUI.Text(Body, "暂时没有可用的电脑对手", centerX, top + 148, centerW, 48, 24, FightUI.Muted, TextAlignmentOptions.Center);
                FightUI.TextAction(Body, "返回模式", workspaceX, actionY, 140, 56, () => Navigate("modes"));
                return;
            }
            selectedBot = Mathf.Clamp(selectedBot, 0, bots.Count - 1); var selected = bots[selectedBot];
            var opponentFrame = FightUI.Panel(Body, centerX, top, centerW, height, FightUI.PanelColor, true, "Opponent briefing");
            opponentFrame.GetComponent<Outline>().effectColor = FightUI.Border;
            var opponent = FightUI.Scroll(opponentFrame, 1, 1, centerW - 2, height - 2, height - 2);
            float artH = Mathf.Min(Compact ? 208 : large ? 420 : 370, height * .54f);
            FightUI.Asset(opponent, "hero.png", 0, 0, centerW - 2, artH);
            HomeShade(opponent, 0, artH - 72, centerW - 2, 72, FightUI.PanelColor);
            FightUI.OrnamentLine(opponent, 24, artH + 12, centerW - 50);
            float selectedThumb = Compact ? 48 : 62;
            if (selected.main0 > 0) FightUI.Art(opponent, selected.main0, 24, artH + 32, selectedThumb, selectedThumb);
            float summaryX = 40 + selectedThumb;
            var opponentName = HomeDeckTitle(opponent, selected.name, summaryX, artH + 30, centerW - summaryX - 24,
                42, Compact ? 24 : 30, FightUI.Gold, true);
            float roleY = Mathf.Max(artH + 69, artH + 34 + opponentName.rectTransform.sizeDelta.y);
            float descriptionY = roleY + 16;
            var description = FightUI.Text(opponent, selected.desc, 24, descriptionY, centerW - 50,
                Mathf.Max(70, height - descriptionY - 24), Compact ? 17 : 20);
            description.enableWordWrapping = true;
            float textH = description.GetPreferredValues(selected.desc ?? "", centerW - 50, 2000).y;
            description.rectTransform.sizeDelta = new Vector2(centerW - 50, Mathf.Max(70, textH));
            opponent.sizeDelta = new Vector2(centerW - 2, Mathf.Max(height - 2, descriptionY + textH + 28));
            var setup = FightUI.Panel(Body, setupX, top, setupW, height, FightUI.PanelColor, true, "Player preparation");
            setup.GetComponent<Outline>().effectColor = FightUI.Border;
            var c = FightUI.Scroll(setup, 1, 1, setupW - 2, height - 2, height - 2);
            float padding = Compact ? 20 : 24, inner = setupW - padding * 2 - 2;
            FightUI.Text(c, "我的卡组", padding, 14, inner, 32, Compact ? 22 : 26, FightUI.Gold);
            FightUI.OrnamentLine(c, padding, 52, inner);
            string playerDeckPath = Program.PATH_DECK + cpuDeck + Program.EXPANSION_YDK;
            bool playerDeckExists = !string.IsNullOrEmpty(cpuDeck) && File.Exists(playerDeckPath);
            float preview = Compact ? 68 : 104, deckTop = 68, nameX = padding + preview + 16;
            Deck playerDeck = playerDeckExists ? new Deck(playerDeckPath) : null;
            if (playerDeckExists) FightUI.Art(c, DeckLibrary.CoverFor(playerDeck), padding, deckTop, preview, preview);
            else HomeDeckBack(c, padding, deckTop, preview, preview);
            var deckName = HomeDeckTitle(c, playerDeckExists ? cpuDeck : "尚未选择构筑", nameX, deckTop - 2,
                setupW - nameX - padding - 2, Compact ? 48 : 68, Compact ? 18 : 23, FightUI.Foreground);
            float deckActionY = deckTop + Mathf.Max(Compact ? 41 : 70, deckName.rectTransform.sizeDelta.y + (Compact ? 4 : 8));
            FightUI.TextAction(c, playerDeckExists ? "更换卡组  →" : "选择卡组  →", nameX, deckActionY,
                setupW - nameX - padding - 2, Compact ? 30 : 36,
                () => PickDeck("玩家卡组", "cpu", path => { cpuDeck = PersonalName(path); SetCurrent(path); }, true));
            float ruleY = Mathf.Max(Compact ? 146 : 210, deckActionY + (Compact ? 38 : 52));
            bool needsComputerDeck = selected.command != null && selected.command.Contains("Lucky");
            bool customDeckExists = !string.IsNullOrEmpty(cpuCustomDeck) && File.Exists(Program.PATH_DECK + cpuCustomDeck + Program.EXPANSION_YDK);
            if (needsComputerDeck)
            {
                var computerDeck = FightUI.FocusKey(FightUI.Button(c, "电脑卡组：" + (string.IsNullOrEmpty(cpuCustomDeck) ? "尚未选择" : cpuCustomDeck), padding, ruleY, inner, 48,
                    () => PickDeck("电脑卡组", "cpu", path => cpuCustomDeck = PersonalName(path), true)), "cpu:computer-deck");
                var caption = computerDeck.GetComponentInChildren<TextMeshProUGUI>();
                caption.enableWordWrapping = true; caption.maxVisibleLines = 2;
                float labelHeight = Mathf.Max(48, Mathf.Min(caption.GetPreferredValues("电脑卡组", float.PositiveInfinity, float.PositiveInfinity).y * 2 + 4,
                    caption.GetPreferredValues(caption.text, inner - 20, float.PositiveInfinity).y + 2));
                computerDeck.GetComponent<RectTransform>().sizeDelta = new Vector2(inner, labelHeight);
                caption.rectTransform.sizeDelta = new Vector2(inner - 20, labelHeight);
                FightUI.Hint(computerDeck.gameObject, string.IsNullOrEmpty(cpuCustomDeck) ? "此电脑对手需要选择自定义卡组。" : cpuCustomDeck);
                ruleY += labelHeight + 14;
            }
            FightUI.Line(c, padding, ruleY, inner);
            FightUI.Text(c, "对局规则", padding, ruleY + 12, inner, 32, Compact ? 21 : 24, FightUI.Gold);
            float numberW = (inner - 16) / 3;
            bool customDeckMissing = needsComputerDeck && !customDeckExists;
            bool playerDeckLegal = playerDeckExists && (cpuNoCheck || playerDeck.Check(BanlistManager.currentBanList, true, true) == 0);
            TextMeshProUGUI readiness = null; Button start = null;
            Action updateReadiness = () =>
            {
                string issue = !playerDeckExists ? "请先选择玩家卡组" : customDeckMissing ? string.IsNullOrEmpty(cpuCustomDeck) ? "请先为电脑选择自定义卡组" : "电脑卡组文件缺失，请重新选择" : !playerDeckLegal ? "玩家构筑不符合当前规则，请检查卡数与禁限卡" : invalidCpuFields.Count > 0 ? "请修正标红的对局规则" : "";
                bool ready = playerDeckLegal && !customDeckMissing && invalidCpuFields.Count == 0;
                if (readiness != null) { readiness.text = issue; readiness.color = ready ? FightUI.Muted : FightUI.Danger; }
                if (start != null) { start.interactable = ready; FightUI.Hint(start.gameObject, ready ? "" : issue); }
            };
            Number(c, "生命值", cpuLp, padding, ruleY + 54, numberW, 1, int.MaxValue, v => cpuLp = v, updateReadiness);
            Number(c, "起手", cpuHand, padding + numberW + 8, ruleY + 54, numberW, 0, 40, v => cpuHand = v, updateReadiness);
            Number(c, "每回合抽卡", cpuDraw, padding + (numberW + 8) * 2, ruleY + 54, numberW, 0, 40, v => cpuDraw = v, updateReadiness);
            float switchesY = ruleY + 148, switchH = Compact ? 34 : 44, switchGap = Compact ? 3 : 6;
            FightUI.SwitchRow(c, "不检查构筑", cpuNoCheck, padding, switchesY, inner, switchH,
                () => { cpuNoCheck = !cpuNoCheck; Rebuild(); });
            FightUI.SwitchRow(c, "不洗牌", cpuNoShuffle, padding, switchesY + switchH + switchGap, inner, switchH,
                () => { cpuNoShuffle = !cpuNoShuffle; Rebuild(); });
            c.sizeDelta = new Vector2(setupW - 2, Mathf.Max(height - 2, switchesY + switchH * 2 + switchGap + 14));
            readiness = FightUI.Text(Body, "", workspaceX + 168, actionY + 4, workspaceWidth - 480, 40, 17, FightUI.Muted);
            FightUI.TextAction(Body, "返回模式", workspaceX, actionY, 140, 56, () => Navigate("modes"));
            start = FightUI.Button(Body, "开始对战  →", workspaceX + workspaceWidth - 256, actionY, 256, 56, () => Try(() =>
            {
                if (invalidCpuFields.Count > 0) throw new InvalidOperationException("请输入有效整数：" + string.Join("、", invalidCpuFields));
                if (string.IsNullOrEmpty(cpuDeck) || !File.Exists(Program.PATH_DECK + cpuDeck + ".ydk")) throw new InvalidOperationException("请先选择玩家卡组。");
                if (needsComputerDeck && (string.IsNullOrEmpty(cpuCustomDeck) || !File.Exists(Program.PATH_DECK + cpuCustomDeck + Program.EXPANSION_YDK))) throw new InvalidOperationException("请先为电脑选择有效的自定义卡组。");
                if (!cpuNoCheck && new Deck(Program.PATH_DECK + cpuDeck + ".ydk").Check(BanlistManager.currentBanList, true, true) != 0) throw new InvalidOperationException("玩家构筑不符合当前规则，请检查卡数和禁限卡。");
                if (cpuLp < 1 || cpuHand < 0 || cpuHand > 40 || cpuDraw < 0 || cpuDraw > 40) throw new InvalidOperationException("请填写有效的对局规则。");
                Config.SetConfigDeck(cpuDeck); Config.Save();
                int port = int.TryParse(Config.Get("LocalDuelPort", SoloSelector.port), out var portNumber) ? portNumber : 7911;
                ShowStatus("正在连接本地电脑对手…");
                coreSourcePage = page;
                Program.instance.solo.StartClientDuel(selectedBot, cpuCustomDeck, port, cpuLp, cpuHand, cpuDraw, false, cpuNoCheck, cpuNoShuffle);
            }), true);
            updateReadiness();
        }
        private void Number(Transform parent, string label, int value, float x, float y, float width, int minimum, int maximum, Action<int> changed, Action validationChanged)
        {
            var caption = FightUI.Text(parent, label, x, y, width, 25, width < 112 ? 15 : 17, FightUI.Muted);
            caption.enableWordWrapping = false;
            if (!cpuNumberDrafts.ContainsKey(label)) cpuNumberDrafts[label] = value.ToString();
            var error = FightUI.Text(parent, "", x, y + 68, width, 22, 14, FightUI.Danger);
            TMP_InputField input = null;
            Action<string> validate = t =>
            {
                cpuNumberDrafts[label] = t;
                bool parsed = int.TryParse(t, out var n), valid = parsed && n >= minimum && n <= maximum;
                if (valid) { invalidCpuFields.Remove(label); changed(n); error.text = ""; }
                else { invalidCpuFields.Add(label); error.text = !parsed ? "请输入整数" : maximum == int.MaxValue ? "至少为 1" : minimum + "–" + maximum; }
                if (input != null) input.GetComponent<Outline>().effectColor = valid ? FightUI.Border : FightUI.Danger;
                validationChanged?.Invoke();
            };
            input = FightUI.Input(parent, cpuNumberDrafts[label], "整数", x, y + 28, width, 38, validate);
            FightUI.Hint(input.gameObject, label + "：" + (maximum == int.MaxValue ? "至少为 1 的整数" : minimum + "–" + maximum + " 之间的整数"));
            validate(cpuNumberDrafts[label]);
        }
        private void RenderAI()
        {
            bool large = W >= 2400;
            bool spectate = page == "spectator";
            battle.AiVsAi = spectate;
            PageTitle("AI 对战");
            float workspaceWidth = large ? Mathf.Min(1900, BodyWidth) : BodyWidth;
            float workspaceX = (BodyWidth - workspaceWidth) / 2;
            if (large) AlignHomeWorkspaceHeading(workspaceX, workspaceWidth);
            float tabsY = Compact ? 60 : 72, tabW = Compact ? 176 : 232;
            FightUI.Tab(Body, "玩家 vs AI", !spectate, workspaceX, tabsY, tabW, 44, () => Navigate("ai"));
            FightUI.Tab(Body, "AI vs AI", spectate, workspaceX + tabW + 8, tabsY, tabW, 44, () => Navigate("spectator"));
            var saved = ResponsesAiConfigStore.Load(); string reason = null; try { saved.Validate(); } catch (Exception e) { reason = e.Message; }
            float serviceW = Compact ? 490 : 640, serviceX = workspaceX + workspaceWidth - serviceW;
            var service = FightUI.Panel(Body, serviceX, 4, serviceW, 56, FightUI.PanelColor, false, "AI service status");
            FightUI.Panel(service, 0, 10, 2, 36, reason == null ? FightUI.GoldBorder : FightUI.Danger, false);
            FightUI.Icon(service, "diamond", 18, 16, 18, 18, reason == null ? FightUI.Gold : FightUI.Danger);
            FightUI.Text(service, "AI 服务 · " + GetAiServiceStatus(saved), 48, 9, serviceW - 240, 36,
                Compact ? 20 : 23, reason == null ? FightUI.Gold : FightUI.Danger);
            FightUI.TextAction(service, "管理 AI 服务  →", serviceW - 198, 8, 184, 40, () => OpenSettings("ai", page));
            float outer = large ? workspaceX : Compact ? 0 : 56, center = Compact ? 80 : large ? 128 : 144;
            float cardY = Compact ? 128 : large ? 170 : 150, cardH = Mathf.Max(256, BodyHeight - cardY - (Compact ? 128 : 152));
            if (large) cardH = Mathf.Min(cardH, 780);
            float surfaceWidth = large ? workspaceWidth : BodyWidth - outer * 2;
            float cw = (surfaceWidth - center) / 2;
            DrawBattleDeck(outer, cardY, cw, cardH, spectate ? "AI A · 下方视角" : "玩家", battle.DeckA, selected => battle.DeckA = selected);
            DrawBattleDeck(outer + cw + center, cardY, cw, cardH, spectate ? "AI B" : "AI 对手", battle.DeckB, selected => battle.DeckB = selected);
            float versusX = outer + cw + 8, versusW = center - 16, versusY = cardY + cardH * .40f;
            FightUI.OrnamentLine(Body, versusX + versusW * .5f - 7, versusY - 26, 14);
            FightUI.HeroTitle(Body, "VS", versusX, versusY, versusW, 82, Compact ? 38 : 56).alignment = TextAlignmentOptions.Center;
            FightUI.Icon(Body, "diamond", versusX + versusW * .5f - 5, versusY + 82, 10, 10, FightUI.GoldBorder);
            float ruleY = cardY + cardH + Gap, ruleH = Compact ? 36 : 44;
            var rules = FightUI.Panel(Body, outer, ruleY, surfaceWidth, ruleH, FightUI.PanelColor, false, "Duel rules summary");
            FightUI.Line(rules, 0, 0, surfaceWidth);
            FightUI.Icon(rules, spectate ? "replay" : "duel", Compact ? 16 : 24, (ruleH - 22) / 2, 22, 22, FightUI.GoldBorder);
            FightUI.Text(rules, spectate ? "AI A 在下方 · 可见 A 手牌 · 对手未公开信息保持隐藏" : "单局对战    ·    全卡池    ·    无禁限表    ·    不换边", 50, 0,
                surfaceWidth - 74, ruleH, Compact ? 16 : 19, FightUI.Muted, TextAlignmentOptions.Center);
            float actionY = large ? ruleY + ruleH + 36 : BodyHeight - 64;
            FightUI.TextAction(Body, "返回模式", workspaceX, actionY, 140, 56, () => Navigate("modes"));
            bool validA = !string.IsNullOrEmpty(battle.DeckA) && File.Exists(battle.DeckA), validB = !string.IsNullOrEmpty(battle.DeckB) && File.Exists(battle.DeckB);
            bool ready = reason == null && validA && validB;
            string startReason = reason != null ? "请先配置 AI 服务" : !validA && !validB ? "请选择双方卡组" : !validA ? "请选择" + (spectate ? " AI A" : "玩家") + "卡组" : !validB ? "请选择" + (spectate ? " AI B" : " AI 对手") + "卡组" : "";
            FightUI.Text(Body, startReason, workspaceX + 168, actionY + 8, workspaceWidth - 480, 44, 17, ready ? FightUI.Muted : FightUI.Gold);
            var begin = FightUI.Button(Body, spectate ? "开始观战  →" : "开始对战  →", workspaceX + workspaceWidth - 256, actionY, 256, 56, () => Try(() =>
            {
                var effective = ResponsesAiConfigStore.Load(); effective.Validate();
                ResponsesAiConfigStore.SaveChoices(battle.DeckA, battle.DeckB, spectate);
                ShowStatus(spectate ? "正在启动 AI 观战…" : "正在启动 AI 对战…");
                coreSourcePage = page;
                ResponsesDuelSession.Start(effective, spectate, battle.DeckA, battle.DeckB);
                Program.instance.ocgcore.returnServant = Program.instance.menu;
            }), true, ready);
            if (!ready) FightUI.Hint(begin.gameObject, startReason + "；当前卡组选择会保留。");
        }
        private void AlignHomeWorkspaceHeading(float x, float width)
        {
            // Called immediately after PageTitle, while only its heading exists
            // as a direct text child of the page.
            foreach (Transform child in Body)
            {
                var text = child.GetComponent<TextMeshProUGUI>();
                if (text == null) continue;
                var rect = text.rectTransform;
                FightUI.Place(rect, x, -rect.anchoredPosition.y, Mathf.Min(rect.rect.width, width), rect.rect.height);
            }
        }
        private void DrawBattleDeck(float x, float y, float w, float h, string title, string path, Action<string> chosen)
        {
            var panel = FightUI.Panel(Body, x, y, w, h, FightUI.PanelColor, true, title + " deck");
            bool opponent = title == "AI B" || title == "AI 对手";
            bool exists = !string.IsNullOrEmpty(path) && File.Exists(path);
            float pad = Compact ? 20 : 28, avatar = Compact ? 44 : 64, headerY = Compact ? 14 : 20;
            HomeAvatar(panel, pad, headerY, avatar, opponent ? 1 : 0);
            FightUI.Text(panel, title, pad + avatar + 16, headerY - 2, w - pad * 2 - avatar - 16,
                40, Compact ? 22 : 28, FightUI.Foreground);
            float headerBottom = headerY + avatar;
            if (!title.StartsWith("AI"))
            {
                string player = Config.Get("DuelPlayerName0", "决斗者");
                if (string.IsNullOrWhiteSpace(player)) player = "决斗者";
                float roleY = headerY + (Compact ? 30 : 44);
                var playerRole = HomeDeckTitle(panel, player, pad + avatar + 16, roleY,
                    w - pad * 2 - avatar - 20, 26, Compact ? 16 : 17, FightUI.Muted);
                headerBottom = Mathf.Max(headerBottom, roleY + playerRole.rectTransform.sizeDelta.y);
            }
            float artY = Mathf.Max(Compact ? 78 : 112, headerBottom + 12), artH = Mathf.Max(96, h - artY - (Compact ? 74 : 92)), artW = w - pad * 2;
            var artFrame = FightUI.Panel(panel, pad, artY, artW, artH, FightUI.Raised, true, "Battle construction");
            artFrame.GetComponent<Outline>().effectColor = FightUI.Border;
            if (exists)
            {
                var deck = new Deck(path);
                FightUI.Art(artFrame, DeckLibrary.CoverFor(deck), 1, 1, artW - 2, artH - 2);
                HomeShade(artFrame, 1, Mathf.Max(0, artH - (Compact ? 128 : 164)), artW - 2,
                    Mathf.Min(artH - 2, Compact ? 128 : 164), FightUI.Background);
                float nameY = artH - (Compact ? 108 : 144);
                var name = HomeDeckTitle(artFrame, Path.GetFileNameWithoutExtension(path), 20, nameY,
                    artW - 40, Compact ? 72 : 102, Compact ? 22 : 30, FightUI.Foreground, true);
                FightUI.Text(artFrame, "主卡组 " + deck.Main.Count + "   ·   额外 " + deck.Extra.Count + "   ·   副卡 " + deck.Side.Count,
                    20, artH - (Compact ? 30 : 44), artW - 40, 26, Compact ? 15 : 18, FightUI.Gold);
            }
            else
            {
                float backH = Mathf.Min(Compact ? 88 : 180, artH - (Compact ? 54 : 84)), backW = backH * .69f;
                float captionH = Compact ? 34 : 44;
                float emptyTop = Mathf.Max(0, (artH - backH - captionH - 12) * .5f);
                HomeDeckBack(artFrame, (artW - backW) * .5f, emptyTop, backW, backH);
                FightUI.Text(artFrame, "选择卡组，准备出战", 16, emptyTop + backH + 12, artW - 32,
                    captionH, Compact ? 19 : 25, FightUI.Muted, TextAlignmentOptions.Center);
            }
            float actionsY = h - (Compact ? 52 : 64), actionH = Compact ? 38 : 46;
            float chooseW = Compact ? 178 : 224;
            FightUI.Button(panel, "选择卡组  →", pad, actionsY, chooseW, actionH, () => PickDeck(title, page, chosen));
            FightUI.TextAction(panel, "查看构筑", pad + chooseW + 24, actionsY, w - pad * 2 - chooseW - 24,
                actionH, () => ShowBattleDeck(path), exists);
        }
        private static Sprite homeShade;
        private void HomeShade(Transform parent, float x, float y, float w, float h, Color tint)
        {
            if (homeShade == null)
            {
                var texture = new Texture2D(2, 96, TextureFormat.RGBA32, false);
                texture.wrapMode = TextureWrapMode.Clamp; texture.filterMode = FilterMode.Bilinear;
                for (int py = 0; py < 96; py++)
                {
                    float alpha = Mathf.Pow(1f - py / 95f, .7f);
                    for (int px = 0; px < 2; px++) texture.SetPixel(px, py, new Color(1, 1, 1, alpha));
                }
                texture.Apply();
                homeShade = Sprite.Create(texture, new Rect(0, 0, 2, 96), new Vector2(.5f, .5f));
                homeShade.name = "Fight artwork lower veil";
            }
            var image = FightUI.Node(parent, "Artwork veil", x, y, w, h).gameObject.AddComponent<Image>();
            image.sprite = homeShade; image.color = tint; image.raycastTarget = false;
        }
        private void HomeBadge(Transform parent, string label, float x, float y, float w, float h, Color tint)
        {
            var chip = FightUI.Panel(parent, x, y, w, h, new Color(.025f, .045f, .055f, .92f), false, label + " badge");
            chip.GetComponent<Image>().raycastTarget = false;
            var edge = tint; edge.a = .48f;
            FightUI.Line(chip, 0, h - 1, w, edge);
            var text = FightUI.Text(chip, label, 6, 0, w - 12, h, Compact ? 14 : 16, tint, TextAlignmentOptions.Center);
            text.raycastTarget = false;
        }
        private void HomeDeckBack(Transform parent, float x, float y, float w, float h)
        {
            var back = FightUI.Node(parent, "Empty construction slot", x, y, w, h).gameObject.AddComponent<FightCardBack>();
            back.color = FightUI.Gold; back.raycastTarget = false;
        }
        private TextMeshProUGUI HomeDeckTitle(Transform parent, string value, float x, float y, float width, float height, float size, Color color, bool bold = false)
        {
            var title = FightUI.Text(parent, value, x, y, width, height, size, color);
            if (bold) title.fontStyle = FontStyles.Bold;
            var single = title.GetPreferredValues(value, float.PositiveInfinity, float.PositiveInfinity);
            bool wraps = single.x > width || (value ?? "").Contains("\n");
            title.enableWordWrapping = wraps; title.maxVisibleLines = 2;
            float lineHeight = title.GetPreferredValues("国", float.PositiveInfinity, float.PositiveInfinity).y;
            float preferred = wraps ? title.GetPreferredValues(value, width, float.PositiveInfinity).y + 2 : single.y + 2;
            title.rectTransform.sizeDelta = new Vector2(width, wraps ? Mathf.Min(lineHeight * 2 + 4, Mathf.Max(height, preferred)) : Mathf.Max(lineHeight + 2, Mathf.Min(height, preferred)));
            FightUI.Hint(title.gameObject, value);
            return title;
        }
        private TextMeshProUGUI HomeWrappedText(Transform parent, string value, float x, float y, float width, float height, float size, Color color)
        {
            var text = FightUI.Text(parent, value, x, y, width, height, size, color);
            text.enableWordWrapping = true;
            text.rectTransform.sizeDelta = new Vector2(width, Mathf.Max(height, text.GetPreferredValues(value, width, float.PositiveInfinity).y + 2));
            return text;
        }
        private void BindHomeCard(RectTransform card, Action action)
        {
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            var colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(1.20f, 1.24f, 1.28f);
            colors.selectedColor = new Color(1.18f, 1.18f, 1.12f); colors.pressedColor = new Color(.82f, .88f, .92f);
            colors.fadeDuration = .16f; button.colors = colors;
            button.onClick.AddListener(() => action?.Invoke());
            var frame = card.GetComponentInChildren<FightFrame>();
            if (frame != null) card.gameObject.AddComponent<FightButtonFeedback>().Bind(frame);
        }
        private void ShowBattleDeck(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) { ShowStatus("卡组文件不存在，请重新选择。"); return; }
            var width = Mathf.Min(W - 64, 1200); var height = H - 80;
            var box = ShowDialog(System.IO.Path.GetFileNameWithoutExtension(path) + " · 只读构筑", width, height);
            var content = FightUI.Scroll(box, 24, 88, width - 48, height - 112, 1200);
            DrawDeckCards(content, new Deck(path), width - 48);
        }
    }
}
