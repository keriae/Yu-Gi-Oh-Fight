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
        private string cpuOpponentSearch = "";
        private static Sprite homeShade;
        private DeckAcademyDeck BlueEyes => academy.decks.FirstOrDefault(d => d.id == "blue-eyes")
            ?? academy.decks.FirstOrDefault(d => d.name.Contains("青眼"));
        private void RenderHome()
        {
            bool large = W >= 2400;
            float side = Compact ? 340 : 504;
            float main = BodyWidth - side - Gap;
            float homeH = BodyHeight, mainH = homeH;
            float cardW = (main - Gap * 2) / 3, captionHeight = Compact ? 78 : 94;
            float naturalCardH = cardW * (9f / 16f) + captionHeight + 1;
            if (UsesFullscreenLayout) naturalCardH = Mathf.Min(naturalCardH, homeH * .45f);
            float heroH = UsesFullscreenLayout ? Mathf.Min(large ? 780 : Compact ? 330 : 600,
                Mathf.Min(main / (2736f / 1040f), mainH - Gap * 3 - naturalCardH))
                : Mathf.Min(Compact ? 330 : large ? 620 : 520, mainH * .68f);
            // Spend taller fullscreen space on the two content rows, keeping
            // the feature panorama and exploration covers landscape. Asset()
            // crops texture coordinates uniformly instead of stretching art.
            float cardH = UsesFullscreenLayout ? Mathf.Max(naturalCardH,
                Mathf.Min(mainH - heroH - Gap * 3, cardW * .75f + captionHeight + 1)) : Mathf.Max(150, Mathf.Min(mainH - heroH - Gap,
                Mathf.Min(large ? 480 : 300, cardW * .56f + captionHeight)));
            float homeY = UsesFullscreenLayout ? Mathf.Min(Gap * 2, Mathf.Max(0, (homeH - heroH - Gap - cardH) * .5f)) : 0;
            if (UsesFullscreenLayout) homeH = heroH + Gap + cardH;
            var hero = FightUI.Panel(Body, 0, homeY, main, heroH, FightUI.Background, true, "Blue-Eyes feature");
            // Preserve the dragon's face and silhouette when the feature becomes
            // wider. The artwork's dark left edge joins the text surface instead
            // of cropping the top and bottom of a stretched panorama.
            float illustrationW = Mathf.Min(main - 2, (heroH - 2) * (2736f / 1040f));
            FightUI.Asset(hero, "hero.png", main - illustrationW - 1, 1, illustrationW, heroH - 2);
            float pad = Compact ? 32 : large ? 64 : 56;
            FightUI.Panel(hero, pad, heroH * .29f + 7, 2, Compact ? 18 : 23, FightUI.Gold, false,
                "Feature inscription").GetComponent<Image>().raycastTarget = false;
            FightUI.Text(hero, "经典主题 · 青眼", pad + 14, heroH * .29f, main * .5f, 36, Compact ? 18 : 24, FightUI.Muted);
            FightUI.HeroTitle(hero, "唤醒传说中的白龙", pad, heroH * .40f, main * .68f, Compact ? 80 : 108, Compact ? 40 : 60);
            float actionY = heroH * .75f;
            bool canPractice = BlueEyes != null && DeckAcademy.CanPractice(BlueEyes);
            var practice = FightUI.Button(hero, "开始练习  →", pad, actionY, Compact ? 192 : 248, Compact ? 56 : 64,
                () => StartCore(() => DeckAcademy.StartPractice(BlueEyes)), true, canPractice);
            if (!canPractice) FightUI.Hint(practice.gameObject, "青眼练习资源暂不可用，请检查本地教学资源。");
            else FightUI.Hint(practice.gameObject, "以预设起手进入本地练习，可随时查看分步指导。");
            var featureDeck = BlueEyes == null ? null : recommendations.FirstOrDefault(e => e.Academy?.id == BlueEyes.id);
            var inspect = FightUI.TextAction(hero, "查看构筑", pad + (Compact ? 220 : 280), actionY, Compact ? 160 : 200, Compact ? 56 : 64,
                () => { detailEntry = featureDeck; detailOrigin = "home"; Navigate("deck-detail"); }, featureDeck != null);
            FightUI.Hint(inspect.gameObject, featureDeck != null ? "查看完整卡组与展开说明" : "当前教学构筑资源不可用");
            float cardY = homeY + heroH + Gap;
            string[] titles = { "卡组学院", "推荐卡组", "残局挑战" };
            string[] descriptions = { "固定起手 · 逐步展开", "经典主题 · 竞技构筑", "固定场面 · 破解胜局" };
            string[] route = { "academy", "recommended", "puzzles" }, assets = { "academy.png", "decks.png", "puzzle.png" };
            for (int i = 0; i < 3; i++)
            {
                int j = i;
                var card = FightUI.Panel(Body, i * (cardW + Gap), cardY, cardW, cardH, FightUI.PanelColor, true, titles[i]);
                float imageH = cardH - captionHeight - 1;
                FightUI.Asset(card, assets[i], 1, 1, cardW - 2, imageH);
                HomeShade(card, 1, imageH - 36, cardW - 2, 36, FightUI.PanelColor);
                FightUI.Line(card, 1, cardH - captionHeight, cardW - 2, FightUI.GoldBorder);
                var tileTitle = FightUI.Text(card, titles[i], Compact ? 18 : 28, cardH - captionHeight + 12, cardW - 52, 36, Compact ? 22 : 27, FightUI.Gold);
                tileTitle.fontStyle = FontStyles.Bold;
                var inset = Compact ? 18 : 28;
                FightUI.Text(card, descriptions[i], inset, cardH - captionHeight + (Compact ? 44 : 53), cardW - inset - 50,
                    22, Compact ? 13 : 16, FightUI.Muted).raycastTarget = false;
                FightUI.Line(card, inset, cardH - 12, Mathf.Min(54, cardW * .2f), FightUI.GoldBorder);
                FightUI.Icon(card, "arrow", cardW - 50, cardH - captionHeight + 17, 28, 28, FightUI.Gold);
                BindHomeCard(card, () => Navigate(route[j]));
                FightUI.Hint(card.gameObject, descriptions[i]);
            }
            var right = FightUI.Panel(Body, main + Gap, homeY, side, homeH, FightUI.PanelColor, false, "Personal sidebar");
            FightUI.Panel(right, 0, 0, 1, homeH, FightUI.Border, false, "Sidebar divider");
            var content = FightUI.Scroll(right, 0, 0, side, homeH, homeH);
            float p = Compact ? 16 : 32, inner = side - p * 2;
            float avatarSize = Compact ? 54 : 96;
            HomeAvatar(content, p + (Compact ? 2 : 8), Compact ? 8 : 18, avatarSize, 0);
            var name = Config.Get("DuelPlayerName0", "").Trim(); if (string.IsNullOrEmpty(name)) name = FightText.Get("决斗者");
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
            Deck currentDeck = exists ? new Deck(path) : null;
            if (exists) FightUI.Art(deckPanel, DeckLibrary.CoverFor(currentDeck), 12, 12, thumb, thumb);
            else
            {
                FightUI.Panel(deckPanel, 12, 12, thumb, thumb, FightUI.Background, true, "No current deck");
                FightUI.Icon(deckPanel, "deck", 12 + thumb * .25f, 12 + thumb * .25f, thumb * .5f, thumb * .5f, FightUI.GoldBorder);
            }
            float deckNameX = thumb + 32;
            var deckName = HomeDeckTitle(deckPanel, exists ? active : FightText.Get("尚未选择卡组"), deckNameX, Compact ? 9 : 20,
                inner - deckNameX - 16, Compact ? 42 : 68, Compact ? 19 : 25, FightUI.Gold);
            float countsY = Mathf.Max(Compact ? 57 : 100, (Compact ? 9 : 20) + deckName.rectTransform.sizeDelta.y + 7);
            if (exists) FightUI.Text(deckPanel, "主 " + currentDeck.Main.Count + "  ·  额外 " + currentDeck.Extra.Count + "  ·  副 " + currentDeck.Side.Count,
                deckNameX, countsY, inner - deckNameX - 16, 24, Compact ? 13 : 17, FightUI.Muted);
            deckH = Mathf.Max(deckH, countsY + (exists ? 32 : 16));
            deckPanel.sizeDelta = new Vector2(inner, deckH);
            if (exists)
            {
                BindHomeCard(deckPanel, () => ShowBattleDeck(path));
                FightUI.FocusKey(deckPanel.GetComponent<Button>(), "home:current-deck-inspect");
                FightUI.Hint(deckPanel.gameObject, "查看当前卡组的完整构筑 · " + FightText.Keep(active));
            }
            float by = Mathf.Max(Compact ? 214 : 362, deckY + deckH + (Compact ? 12 : 16)), buttonH = Compact ? 42 : 50;
            var editCurrent = FightUI.Button(content, "编辑卡组", p, by, (inner - 16) / 2, buttonH, () => OpenEditor(active), false, exists);
            if (!exists) FightUI.Hint(editCurrent.gameObject, "请先更换或选择当前卡组。");
            FightUI.Button(content, exists ? "更换卡组" : "选择卡组", p + (inner + 16) / 2, by, (inner - 16) / 2, buttonH,
                () => PickDeck("当前使用", "home", picked => SetCurrent(picked), true), !exists);
            float sectionDividerY = Mathf.Max(Compact ? 270 : 440, by + buttonH + (Compact ? 14 : 24));
            FightUI.Line(content, p, sectionDividerY, inner);
            float tutorialY = sectionDividerY + (Compact ? 14 : 18), tutorialH = Compact ? 168 : 250, tutorialPad = Compact ? 16 : 20;
            var teaching = FightUI.Panel(content, p, tutorialY, inner, tutorialH, FightUI.Raised, false, "Blue-Eyes opening");
            FightUI.Text(teaching, "学习青眼展开", tutorialPad, Compact ? 10 : 16, inner - tutorialPad * 2, 36, Compact ? 20 : 26, FightUI.Gold);
            float handW = (inner - tutorialPad * 2 - 24) / 5, handH = handW * 1.45f, handY = Compact ? 44 : 68;
            if (BlueEyes?.starter_hand != null)
                for (int i = 0; i < Mathf.Min(5, BlueEyes.starter_hand.Length); i++)
                {
                    int code = BlueEyes.starter_hand[i];
                    var image = FightUI.Art(teaching, code, tutorialPad + i * (handW + 6), handY, handW, handH, true);
                    var card = FightUI.CardButton(image, () => ShowCard(code));
                    FightUI.FocusKey(card, "home:starter:" + i);
                    FightUI.Hint(card.gameObject, FightText.Keep(CardsManager.Get(code).Name) + " · 查看卡片");
                }
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
            if (face != null) picture.sprite = face;
            var edge = FightUI.Node(parent, "Avatar ring", x, y, size, size).gameObject.AddComponent<Image>();
            edge.sprite = homeCircleRing; edge.color = FightUI.Gold; edge.raycastTarget = false;
            var frame = FightUI.Node(parent, "Player profile frame", x, y, size, size).gameObject.AddComponent<Image>();
            frame.color = Color.clear; frame.preserveAspect = true; frame.raycastTarget = false;
            LoadHomeAppearance(picture, frame, edge, shape, clip, player);
        }
        private async void LoadHomeAppearance(Image picture, Image frame, Image fallbackRing, Image fallbackShape, Mask clip, int player)
        {
            try
            {
                string fallback = Program.items.faces.Count > 0 ? Program.items.faces[0].id.ToString() : "1010001";
                var sprite = await Program.items.LoadConcreteItemIconAsync(Config.Get("DuelFace" + player, fallback), Items.ItemType.Face, player);
                if (picture != null && sprite != null) picture.sprite = sprite;
                string fallbackFrame = Program.items.frames.Count > 0 ? Program.items.frames[0].id.ToString() : "1030001";
                string frameCode = Config.Get("DuelFrame" + player, fallbackFrame);
                if (frameCode == Items.CODE_RANDOM.ToString() && !string.IsNullOrEmpty(Items.lastRandomFrameID))
                    frameCode = Items.lastRandomFrameID;
                if (frameCode == "0") return;
                var border = await Program.items.LoadConcreteItemIconAsync(frameCode, Items.ItemType.Frame, player);
                if (frame == null || border == null) return;
                // Selected frames use their own outline; the generic circular
                // fallback must not clip square portraits or cover that frame.
                frame.sprite = border; frame.color = Color.white;
                if (clip != null) clip.enabled = false;
                if (fallbackShape != null) fallbackShape.enabled = false;
                if (fallbackRing != null) fallbackRing.enabled = false;
            }
            catch (Exception error) { Debug.LogWarning("[FightClient] player appearance: " + error.Message); }
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
            float y = Compact ? 74 : 84;
            float h = UsesFullscreenLayout ? Mathf.Min(720, Mathf.Min(cw * .75f + 92, BodyHeight - y - 96))
                : Mathf.Min(large ? 800 : 580, BodyHeight - 204);
            float workspaceY = UsesFullscreenLayout ? Mathf.Max(0, (BodyHeight - y - h - 96) * .5f) : 0;
            y += workspaceY;
            if (large || UsesFullscreenLayout) AlignHomeWorkspaceHeading(startX, workspaceWidth, workspaceY);
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
                if (i == 0) FocusDragonArtwork(illustration);
                if (i == 2) illustration.color = new Color(.4f, .43f, .46f, 1);
                HomeShade(card, 1, artH - 80, cw - 2, 80, FightUI.PanelColor);
                if (i == 2) HomeBadge(card, "筹备中", 18, 18, 90, 30, FightUI.Muted);
                FightUI.Icon(card, i == 0 ? "duel" : i == 1 ? "swords" : "diamond", 24, artH + 31, 30, 30,
                    i == 2 ? FightUI.Muted : FightUI.Gold);
                var title = FightUI.Text(card, titles[i], 70, artH + 12, cw - 126, 44,
                    Compact ? 27 : 32, i == 2 ? FightUI.Muted : FightUI.Gold);
                title.fontStyle = FontStyles.Bold;
                if (i < 2) FightUI.Text(card, i == 0 ? "本地 · 预设电脑对手" : "玩家对战 · AI 观战", 70,
                    artH + 60, cw - 90, 24, Compact ? 14 : 17, FightUI.Muted);
                if (i < 2) FightUI.Icon(card, "arrow", cw - 48, artH + 34, 24, 24, FightUI.Gold);
                BindHomeCard(card, () => { if (j < 2) Navigate(j == 0 ? "cpu" : battle.AiVsAi ? "spectator" : "ai"); });
                if (i == 2)
                {
                    card.GetComponent<Button>().interactable = false;
                    FightUI.Hint(card.gameObject, "多人匹配正在筹备中，暂时无法开始。");
                }
            }
            float actionX = large ? startX : 0;
            float actionY = UsesFullscreenLayout || large ? y + h + 40 : BodyHeight - 64;
            FightUI.TextAction(Body, "返回首页", actionX, actionY, 140, 56, () => Navigate("home"));
        }
        private sealed class BattleSetupLayout
        {
            internal float WorkspaceX, WorkspaceWidth, ToolbarY, ActionY, Outer, SurfaceWidth, Center, CardY, CardH, CardW, LaunchSize;
        }
        private BattleSetupLayout GetBattleSetupLayout()
        {
            bool large = W >= 2400;
            float workspace = large ? Mathf.Min(1900, BodyWidth) : BodyWidth, workspaceX = (BodyWidth - workspace) / 2;
            float outer = large ? workspaceX : Compact ? 0 : 40, surface = large ? workspace : BodyWidth - outer * 2;
            float center = Compact ? 184 : 244, cardY = Compact ? 76 : 84;
            float cardW = (surface - center) / 2;
            float cardH = Mathf.Max(256, BodyHeight - cardY - (Compact ? 80 : 96));
            float workspaceY = 0;
            if (UsesFullscreenLayout)
            {
                float coverW = cardW - (Compact ? 40 : 56), headerH = Compact ? 78 : 112, actionsH = Compact ? 74 : 92;
                cardH = Mathf.Min(cardH, Mathf.Min(780, coverW * .75f + headerH + actionsH));
                workspaceY = Mathf.Max(0, (BodyHeight - cardY - cardH - 84) * .5f);
                cardY += workspaceY;
            }
            else if (large) cardH = Mathf.Min(cardH, 866);
            return new BattleSetupLayout { WorkspaceX = workspaceX, WorkspaceWidth = workspace, ToolbarY = workspaceY + 8,
                ActionY = UsesFullscreenLayout || large ? cardY + cardH + 28 : BodyHeight - 64, Outer = outer, SurfaceWidth = surface,
                Center = center, CardY = cardY, CardH = cardH, CardW = cardW, LaunchSize = Compact ? 160 : large ? 218 : 206 };
        }
        private void DrawBattleModeTabs(float x, float y = 8)
        {
            float width = Compact ? 160 : 208;
            FightUI.Tab(Body, "人机对战", page == "cpu", x, y, width, 44, () => Navigate("cpu"));
            FightUI.Tab(Body, "玩家 vs AI", page == "ai", x + width + 8, y, width, 44, () => Navigate("ai"));
            FightUI.Tab(Body, "AI vs AI", page == "spectator", x + (width + 8) * 2, y, width, 44, () => Navigate("spectator"));
        }
        private void RenderCpu()
        {
            var g = GetBattleSetupLayout();
            DrawBattleModeTabs(g.WorkspaceX, g.ToolbarY);
            DrawBattleOptionsEntry(g.WorkspaceX + g.WorkspaceWidth - 168, g.ToolbarY, false);
            string playerPath = string.IsNullOrEmpty(cpuDeck) ? "" : Program.PATH_DECK + cpuDeck + Program.EXPANSION_YDK;
            Deck player = null;
            try { if (File.Exists(playerPath)) player = new Deck(playerPath); } catch (Exception) { }
            bool playerLegal = player != null && (cpuNoCheck || player.Check(BanlistManager.currentBanList, true, true) == 0);
            string playerIssue = player == null ? "请选择玩家卡组。" : playerLegal ? null : "玩家卡组不符合当前构筑规则。";
            DrawBattleDeck(g.Outer, g.CardY, g.CardW, g.CardH, "玩家", playerPath,
                path => { cpuDeck = PersonalName(path); SetCurrent(path); }, playerIssue, personalDeck: true);
            var bots = SoloSelector.bots;
            if (bots.Count == 0)
            {
                DrawBattleDeck(g.Outer + g.CardW + g.Center, g.CardY, g.CardW, g.CardH, "电脑", "", _ => { },
                    chooseProfile: OpenCpuOpponentPicker, identityName: "暂无人机模式", deckAction: OpenCpuOpponentPicker, fallbackArt: 0, inspectDeck: false);
            }
            selectedBot = Mathf.Clamp(selectedBot, 0, Mathf.Max(0, bots.Count - 1));
            var selected = bots.Count > 0 ? bots[selectedBot] : null;
            bool custom = selected?.command != null && selected.command.Contains("Lucky");
            string opponentPath = custom ? string.IsNullOrEmpty(cpuCustomDeck) ? "" : Program.PATH_DECK + cpuCustomDeck + Program.EXPANSION_YDK : CpuOpponentDeckPath(selected);
            string opponentIssue = null;
            if (custom)
            {
                if (string.IsNullOrEmpty(opponentPath) || !File.Exists(opponentPath)) opponentIssue = "请为自选卡组模式选择电脑卡组。";
                else try
                {
                    var deck = new Deck(opponentPath);
                    if (deck.Main.Count == 0 || deck.Main.Count > 60 || deck.Extra.Count > 15 || deck.Side.Count > 15)
                        opponentIssue = "电脑卡组超过支持的卡数范围。";
                }
                catch (Exception) { opponentIssue = "电脑卡组读取失败。"; }
            }
            if (selected != null)
                DrawBattleDeck(g.Outer + g.CardW + g.Center, g.CardY, g.CardW, g.CardH, "电脑", opponentPath,
                    path => cpuCustomDeck = PersonalName(path), opponentIssue, chooseProfile: OpenCpuOpponentPicker,
                    identityName: selected.name, fallbackArt: custom ? 0 : selected.main0,
                    fallbackCaption: custom ? null : CpuOpponentDetails(selected), deckAction: custom ? null : OpenCpuOpponentPicker,
                    personalDeck: custom, inspectDeck: !string.IsNullOrEmpty(opponentPath));
            bool ready = selected != null && playerLegal && opponentIssue == null && invalidCpuFields.Count == 0;
            float versusX = g.Outer + g.CardW + (g.Center - g.LaunchSize) / 2, versusY = g.CardY + (g.CardH - g.LaunchSize) / 2;
            DrawDuelLaunch(Body, versusX, versusY, g.LaunchSize, () => Try(() =>
            {
                if (!ready) return;
                if (!File.Exists(playerPath)) throw new InvalidOperationException("玩家卡组文件不存在。");
                if (custom && !File.Exists(opponentPath)) throw new InvalidOperationException("电脑卡组文件不存在。");
                Config.SetConfigDeck(cpuDeck); Config.Save();
                int port = int.TryParse(Config.Get("LocalDuelPort", SoloSelector.port), out var number) ? number : 7911;
                coreSourcePage = page;
                Program.instance.solo.StartClientDuel(selectedBot, cpuCustomDeck, port, cpuLp, cpuHand, cpuDraw, false, cpuNoCheck, cpuNoShuffle);
            }), ready, "进入人机对战", playerIssue ?? opponentIssue ?? "没有可用的人机模式。");
            float actionY = g.ActionY;
            FightUI.TextAction(Body, "返回模式", g.WorkspaceX, actionY, 140, 56, () => Navigate("modes"));
            if (!ready)
                FightUI.FocusKey(FightUI.TextAction(Body, "查看准备问题", g.WorkspaceX + g.WorkspaceWidth - 208, actionY + 8, 208, 44,
                    () => ShowStatus(playerIssue ?? opponentIssue ?? "请选择人机模式。", true)), "battle:preparation-review");
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
                if (valid) { invalidCpuFields.Remove(label); changed(n); error.text = maximum == int.MaxValue ? "1 或以上" : minimum + "–" + maximum; }
                else { invalidCpuFields.Add(label); error.text = !parsed ? "请输入整数" : maximum == int.MaxValue ? "至少为 1" : minimum + "–" + maximum; }
                error.color = valid ? FightUI.Muted : FightUI.Danger;
                if (input != null) input.GetComponent<Outline>().effectColor = valid ? FightUI.Border : FightUI.Danger;
                validationChanged?.Invoke();
            };
            input = FightUI.Input(parent, cpuNumberDrafts[label], "整数", x, y + 28, width, 38, validate);
            input.keyboardType = TouchScreenKeyboardType.NumberPad;
            FightUI.Hint(input.gameObject, label + "：" + (maximum == int.MaxValue ? "至少为 1 的整数" : minimum + "–" + maximum + " 之间的整数"));
            validate(cpuNumberDrafts[label]);
        }
        private void RenderAI()
        {
            bool large = W >= 2400, spectate = page == "spectator";
            battle.AiVsAi = spectate;
            var g = GetBattleSetupLayout();
            float workspaceWidth = g.WorkspaceWidth, workspaceX = g.WorkspaceX;
            DrawBattleModeTabs(workspaceX, g.ToolbarY);
            FightUI.FocusKey(FightUI.TextAction(Body, "管理 AI 分身  →", workspaceX + workspaceWidth - 408, g.ToolbarY, 216, 44,
                () => OpenAiProfileSettings(battle.ProfileB, page)), "battle:manage-profiles");
            DrawBattleOptionsEntry(workspaceX + workspaceWidth - 168, g.ToolbarY, true);
            var profiles = ReadBattleProfiles(out string loadIssue);
            var profileA = ResolveBattleProfile(profiles, battle.ProfileA);
            var profileB = ResolveBattleProfile(profiles, battle.ProfileB);
            if (profileA != null && string.IsNullOrEmpty(battle.ProfileA)) battle.ProfileA = profileA.Id;
            if (profileB != null && string.IsNullOrEmpty(battle.ProfileB)) battle.ProfileB = profileB.Id;
            string serviceA = spectate ? BattleProfileIssue(profileA) : null, serviceB = BattleProfileIssue(profileB);
            string reason = loadIssue ?? (serviceA != null ? (profileA?.Name ?? "AI") + "：" + serviceA
                : serviceB != null ? (profileB?.Name ?? "AI") + "：" + serviceB : null);
            float outer = g.Outer, center = g.Center, cardY = g.CardY, cardH = g.CardH, surfaceWidth = g.SurfaceWidth, cw = g.CardW;
            string issueA = ResponsesDuelSession.DeckPreparationIssue(battle.DeckA), issueB = ResponsesDuelSession.DeckPreparationIssue(battle.DeckB);
            DrawBattleDeck(outer, cardY, cw, cardH, spectate ? "AI A · 下方视角" : "玩家", battle.DeckA,
                selected => battle.DeckA = selected, issueA, spectate ? profileA : null, spectate ? (Action)(() => OpenBattleProfilePicker(true)) : null);
            DrawBattleDeck(outer + cw + center, cardY, cw, cardH, spectate ? "AI B" : "AI 对手", battle.DeckB,
                selected => battle.DeckB = selected, issueB, profileB, () => OpenBattleProfilePicker(false));
            bool ready = reason == null && issueA == null && issueB == null;
            float size = g.LaunchSize;
            float versusX = outer + cw + (center - size) / 2, versusY = cardY + (cardH - size) * .5f;
            FightUI.OrnamentLine(Body, versusX + size * .5f - 16, versusY - 32, 32);
            DrawDuelLaunch(Body, versusX, versusY, size, () => Try(() =>
            {
                var effectiveA = spectate ? AiDuelistProfileStore.Get(battle.ProfileA) : null;
                var effectiveB = AiDuelistProfileStore.Get(battle.ProfileB);
                if (effectiveB == null || spectate && effectiveA == null) throw new AiConfigurationException("请选择有效的 AI 分身。");
                ResponsesAiConfigStore.SaveChoices(battle.DeckA, battle.DeckB, spectate, battle.ProfileA, battle.ProfileB);
                ShowStatus(spectate ? "正在启动 AI 观战…" : "正在启动 AI 对战…"); coreSourcePage = page;
                ResponsesDuelSession.Start(effectiveA, effectiveB, spectate, battle.DeckA, battle.DeckB, ReadAiBattleOptions());
                Program.instance.ocgcore.returnServant = Program.instance.menu;
            }), ready, spectate ? "进入 AI 观战" : "进入 AI 对战", reason ?? issueA ?? issueB);
            float actionY = g.ActionY;
            FightUI.TextAction(Body, "返回模式", workspaceX, actionY, 140, 56, () => Navigate("modes"));
            if (!ready) FightUI.FocusKey(FightUI.TextAction(Body, "查看准备问题  →", workspaceX + workspaceWidth - 208, actionY + 8, 208, 44,
                () => OpenBattlePreparation(reason, issueA, issueB, spectate)), "battle:preparation-review");
        }
        private static System.Collections.Generic.List<AiDuelistProfile> ReadBattleProfiles(out string issue)
        {
            try { issue = null; return AiDuelistProfileStore.LoadAll(); }
            catch (AiConfigurationException error) { issue = error.Message; return new System.Collections.Generic.List<AiDuelistProfile>(); }
        }
        private static AiDuelistProfile ResolveBattleProfile(System.Collections.Generic.List<AiDuelistProfile> profiles, string id)
            => string.IsNullOrEmpty(id) ? profiles.FirstOrDefault() : profiles.FirstOrDefault(profile => profile.Id == id);
        private static string BattleProfileIssue(AiDuelistProfile profile)
        {
            if (profile == null) return "尚未选择 AI 分身。";
            try { profile.Config.Validate(); return null; }
            catch (AiConfigurationException error) { return error.Message; }
        }
        private void OpenBattleProfilePicker(bool sideA)
        {
            var profiles = ReadBattleProfiles(out string issue);
            float width = Mathf.Min(820, W - 48), height = Mathf.Min(720, H - 48);
            var box = ShowDialog("选择 AI 分身", width, height);
            float viewportHeight = height - 172;
            var content = FightUI.Scroll(box, 24, 144, width - 48, viewportHeight, viewportHeight, out var scroll);
            string selected = sideA ? battle.ProfileA : battle.ProfileB;
            float rowH = 108;
            Action<string> draw = query =>
            {
                FightUI.Clear(content);
                var matches = profiles.Where(profile => CollectionMatches(profile.Name, query, profile.Config.Model)).ToArray();
                for (int i = 0; i < matches.Length; i++)
                {
                    var profile = matches[i];
                    var row = FightUI.Panel(content, 8, 8 + i * rowH, width - 78, rowH - 10,
                        profile.Id == selected ? FightUI.Raised : FightUI.PanelColor, true, "AI 分身 · " + profile.Name);
                    DrawBattleProfileAvatar(row, profile, 16, 16, 62);
                    var name = FightUI.RawText(row, profile.Name, 96, 12, width - 242, 36, 23, profile.Id == selected ? FightUI.Gold : FightUI.Foreground);
                    name.richText = false; name.raycastTarget = false;
                    var model = FightUI.RawText(row, profile.Config.Model, 96, 54, width - 242, 30, 16,
                        FightUI.Muted); model.richText = false; model.raycastTarget = false;
                    FightUI.Icon(row, profile.Id == selected ? "check" : "arrow", width - 124, 36, 24, 24, FightUI.Gold);
                    BindHomeCard(row, () => Try(() =>
                    {
                        ResponsesAiConfigStore.SaveChoices(battle.DeckA, battle.DeckB, battle.AiVsAi,
                            sideA ? profile.Id : battle.ProfileA, sideA ? battle.ProfileB : profile.Id);
                        if (sideA) battle.ProfileA = profile.Id; else battle.ProfileB = profile.Id;
                        CloseDialog(); Rebuild();
                    }));
                    FightUI.FocusKey(row.GetComponent<Button>(), "battle:profile-option:" + profile.Id);
                    FightUI.Hint(row.gameObject, "选择 " + FightText.Keep(profile.Name) + " · " + FightText.Keep(profile.Config.Model));
                }
                content.sizeDelta = new Vector2(width - 48, Mathf.Max(viewportHeight, matches.Length * rowH + 16));
                scroll.StopMovement(); scroll.verticalNormalizedPosition = 1;
                if (matches.Length == 0) FightUI.Text(content, profiles.Count == 0 ? issue ?? "暂无 AI 分身" : "没有匹配的分身",
                    20, 28, width - 100, 100, 21, FightUI.Muted);
            };
            FightUI.FocusKey(FightUI.Input(box, "", "搜索分身名称或模型", 28, 84, width - 288, 44, draw), "battle:profile-search");
            FightUI.FocusKey(FightUI.TextAction(box, "+ 创建 / 编辑分身", width - 244, 84, 216, 44,
                () => { CloseDialog(); OpenAiProfileSettings(selected, page); }), "battle:profile-editor");
            draw("");
        }
        private async void DrawBattleProfileAvatar(Transform parent, AiDuelistProfile profile, float x, float y, float size)
        {
            var portrait = FightUI.Node(parent, "AI profile portrait", x, y, size, size).gameObject.AddComponent<Image>();
            portrait.color = Color.white; portrait.preserveAspect = true; portrait.raycastTarget = false;
            var frame = FightUI.Node(parent, "AI profile frame", x, y, size, size).gameObject.AddComponent<Image>();
            frame.color = Color.clear; frame.preserveAspect = true; frame.raycastTarget = false;
            try
            {
                string id = profile != null && profile.Appearance.TryGetValue("Face", out var face) && face != "0" ? face : "1010001";
                var sprite = await Program.items.LoadConcreteItemIconAsync(id, Items.ItemType.Face, 1);
                if (portrait != null) portrait.sprite = sprite;
                if (profile != null && profile.Appearance.TryGetValue("Frame", out var border) && border != "0")
                {
                    var edge = await Program.items.LoadConcreteItemIconAsync(border, Items.ItemType.Frame, 1);
                    if (frame != null && edge != null) { frame.sprite = edge; frame.color = Color.white; }
                }
            }
            catch (Exception error) { Debug.LogWarning("[FightClient] AI profile portrait: " + error.Message); }
        }
        private static ResponsesAiConfig ReadBattleService(out string issue, out bool unreadable)
        {
            ResponsesAiConfig saved; issue = null; unreadable = false;
            try { saved = ResponsesAiConfigStore.Load(); }
            catch (AiConfigurationException error)
            { issue = error.Message; unreadable = true; return new ResponsesAiConfig(); }
            try { saved.Validate(); }
            catch (AiConfigurationException error) { issue = error.Message; }
            return saved;
        }

        private void OpenBattlePreparation(string serviceIssue, string issueA, string issueB, bool spectate)
        {
            float width = Mathf.Min(780, W - 48), height = Mathf.Min(650, H - 48);
            var box = ShowDialog("对战准备检查", width, height);
            float viewportHeight = height - 124;
            var content = FightUI.Scroll(box, 24, 96, width - 48, viewportHeight, viewportHeight, out var scroll);
            scroll.name = "Battle preparation details";
            float y = 8;
            Action<string, string, string, Action> row = (title, issue, key, fix) =>
            {
                FightUI.Text(content, title + " · " + (issue == null ? "已就绪" : "待修正"), 8, y, width - 82, 32, 20,
                    issue == null ? FightUI.CyanText : FightUI.Danger);
                y += 42;
                string detail = issue ?? (key == "service" ? "所选 AI 分身的配置有效；可在设置中测试连接。" : "符合当前 AI 对战构筑规则。");
                var text = FightUI.Text(content, detail, 8, y, width - 84, 80, 18, FightUI.Foreground);
                text.name = "Battle preparation issue " + key;
                text.text = detail; text.richText = false; text.lineSpacing = 4; text.overflowMode = TextOverflowModes.Overflow;
                float textHeight = text.GetPreferredValues(FightText.Get(detail), width - 84, float.PositiveInfinity).y + 8;
                text.rectTransform.sizeDelta = new Vector2(width - 84, textHeight); y += textHeight + 8;
                if (issue != null)
                {
                    FightUI.FocusKey(FightUI.TextAction(content, key == "service" ? "管理 AI 分身  →" : "更换" + title + "卡组  →", 8, y, width - 84, 38, fix),
                        "battle:preparation-fix:" + key);
                    y += 50;
                }
                FightUI.Line(content, 8, y, width - 84, FightUI.Border); y += 22;
            };
            var profiles = ReadBattleProfiles(out _);
            var profileA = ResolveBattleProfile(profiles, battle.ProfileA);
            var profileB = ResolveBattleProfile(profiles, battle.ProfileB);
            string sideAName = spectate ? profileA?.Name ?? "AI" : Config.Get("DuelPlayerName0", "决斗者");
            string sideBName = profileB?.Name ?? "AI";
            row("AI 分身", serviceIssue, "service", () => { CloseDialog(); OpenAiProfileSettings(spectate && BattleProfileIssue(profileA) != null ? battle.ProfileA : battle.ProfileB, page); });
            row(sideAName, issueA, "a", () => { CloseDialog(); PickDeck(sideAName, page, path => battle.DeckA = path); });
            row(sideBName, issueB, "b", () => { CloseDialog(); PickDeck(sideBName, page, path => battle.DeckB = path); });
            content.sizeDelta = new Vector2(width - 48, Mathf.Max(viewportHeight, y + 8));
        }

        private void AlignHomeWorkspaceHeading(float x, float width, float verticalOffset = 0)
        {
            // Called immediately after PageTitle, while only its heading exists
            // as a direct text child of the page.
            foreach (Transform child in Body)
            {
                var text = child.GetComponent<TextMeshProUGUI>();
                if (text == null) continue;
                var rect = text.rectTransform;
                FightUI.Place(rect, x, -rect.anchoredPosition.y + verticalOffset, Mathf.Min(rect.rect.width, width), rect.rect.height);
            }
        }
        private void DrawBattleDeck(float x, float y, float w, float h, string title, string path, Action<string> chosen, string issue = null, AiDuelistProfile profile = null, Action chooseProfile = null, string identityName = null, int fallbackArt = 0, string fallbackCaption = null, Action deckAction = null, bool personalDeck = false, bool inspectDeck = true)
        {
            var panel = FightUI.Panel(Body, x, y, w, h, FightUI.PanelColor, true, title + " deck");
            bool opponent = title == "AI B" || title == "AI 对手" || title == "电脑";
            bool exists = !string.IsNullOrEmpty(path) && File.Exists(path);
            Deck preview = null;
            if (exists)
            {
                try { preview = new Deck(path); }
                catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
                { issue = issue ?? error.Message; }
            }
            float pad = Compact ? 20 : 28, avatar = Compact ? 44 : 64, headerY = Compact ? 14 : 20;
            float headerBottom = headerY + avatar;
            if (chooseProfile != null)
            {
                var identity = FightUI.Panel(panel, pad - 6, headerY - 6, w - pad * 2 + 12, avatar + 12, Color.clear, false, "AI 分身选择");
                if (identityName != null)
                {
                    if (fallbackArt > 0) FightUI.Art(identity, fallbackArt, 6, 6, avatar, avatar);
                    else HomeAvatar(identity, 6, 6, avatar, 1);
                }
                else DrawBattleProfileAvatar(identity, profile, 6, 6, avatar);
                float textX = avatar + 20;
                var name = FightUI.RawText(identity, identityName ?? profile?.Name ?? FightText.Get("选择 AI 分身"), textX, 0, identity.rect.width - textX - 32,
                    Compact ? 32 : 40, Compact ? 21 : 27, FightUI.Foreground); name.richText = false; name.raycastTarget = false;
                name.name = "Battle duelist name";
                var role = FightUI.Text(identity, identityName != null ? "电脑" : "AI", textX, Compact ? 30 : 42,
                    identity.rect.width - textX - 32, 28, Compact ? 14 : 16, FightUI.Muted);
                role.name = "Battle duelist type"; role.raycastTarget = false;
                FightUI.Icon(identity, "arrow", identity.rect.width - 26, avatar * .4f, 18, 18, FightUI.Gold);
                BindHomeCard(identity, chooseProfile);
                FightUI.FocusKey(identity.GetComponent<Button>(), "battle:profile:" + (opponent ? "b" : "a"));
                FightUI.Hint(identity.gameObject, identityName != null ? "选择人机模式" : "更换 AI 分身" + (profile != null ? "\n模型：" + profile.Config.Model : ""));
            }
            else
            {
                HomeAvatar(panel, pad, headerY, avatar, 0);
                string player = Config.Get("DuelPlayerName0", ""); if (string.IsNullOrWhiteSpace(player)) player = FightText.Get("决斗者");
                var playerName = FightUI.RawText(panel, player, pad + avatar + 16, headerY - 2,
                    w - pad * 2 - avatar - 16, 40, Compact ? 22 : 28, FightUI.Foreground);
                playerName.name = "Battle duelist name"; playerName.richText = false;
                float roleY = headerY + (Compact ? 30 : 44);
                var playerRole = HomeDeckTitle(panel, FightText.Get("玩家"), pad + avatar + 16, roleY, w - pad * 2 - avatar - 20, 26, Compact ? 16 : 17, FightUI.Muted);
                playerRole.name = "Battle duelist type";
                headerBottom = Mathf.Max(headerBottom, roleY + playerRole.rectTransform.sizeDelta.y);
            }
            float artY = Mathf.Max(Compact ? 78 : 112, headerBottom + 12), artH = Mathf.Max(96, h - artY - (Compact ? 74 : 92)), artW = w - pad * 2;
            FightUI.Line(panel, pad, artY - 8, artW);
            FightUI.Panel(panel, 0, headerY + 4, 2, avatar - 8, opponent ? FightUI.Gold : FightUI.Cyan, false,
                "Player identity accent").GetComponent<Image>().raycastTarget = false;
            var artFrame = FightUI.Panel(panel, pad, artY, artW, artH, FightUI.Raised, true, "Battle construction");
            artFrame.GetComponent<Outline>().effectColor = FightUI.Border;
            if (preview != null)
            {
                var deck = preview;
                FightUI.Art(artFrame, DeckLibrary.CoverFor(deck), 1, 1, artW - 2, artH - 2);
                HomeShade(artFrame, 1, Mathf.Max(0, artH - (Compact ? 128 : 164)), artW - 2,
                    Mathf.Min(artH - 2, Compact ? 128 : 164), FightUI.Background);
                float nameY = artH - (Compact ? 108 : 144);
                var name = HomeDeckTitle(artFrame, identityName != null ? Path.GetFileNameWithoutExtension(path).Replace("AI_", "") : Path.GetFileNameWithoutExtension(path), 20, nameY,
                    artW - 40, Compact ? 72 : 102, Compact ? 22 : 30, FightUI.Foreground, true);
                var counts = FightUI.Text(artFrame, "主卡组 " + deck.Main.Count + "   ·   额外 " + deck.Extra.Count + "   ·   副卡 " + deck.Side.Count,
                    20, artH - (Compact ? 30 : 44), artW - 40, 26, Compact ? 15 : 18, issue == null ? FightUI.Gold : FightUI.Danger);
                FightUI.Hint(counts.gameObject, issue ?? "构筑符合当前 AI 对战规则。");
            }
            else if (fallbackArt > 0 || fallbackCaption != null)
            {
                if (fallbackArt > 0) FightUI.Art(artFrame, fallbackArt, 1, 1, artW - 2, artH - 2);
                else FocusDragonArtwork(FightUI.Asset(artFrame, "hero.png", 1, 1, artW - 2, artH - 2));
                if (!string.IsNullOrWhiteSpace(fallbackCaption))
                {
                    string detail = fallbackCaption.Split('\n').FirstOrDefault();
                    HomeShade(artFrame, 1, Mathf.Max(0, artH - 88), artW - 2, 88, FightUI.Background);
                    var caption = FightUI.Text(artFrame, detail, 20, artH - 64, artW - 40, 52, Compact ? 17 : 20, FightUI.Foreground);
                    caption.richText = false; FightUI.Hint(caption.gameObject, fallbackCaption);
                }
            }
            else
            {
                float backH = Mathf.Min(Compact ? 88 : 180, artH - (Compact ? 54 : 84)), backW = backH * .69f;
                float captionH = Compact ? 34 : 44;
                float emptyTop = Mathf.Max(0, (artH - backH - captionH - 12) * .5f);
                HomeDeckBack(artFrame, (artW - backW) * .5f, emptyTop, backW, backH);
                var empty = FightUI.Text(artFrame, exists ? "卡组读取失败" : "选择卡组", 16, emptyTop + backH + 12, artW - 32,
                    captionH, Compact ? 19 : 25, exists ? FightUI.Danger : FightUI.Muted, TextAlignmentOptions.Center);
                if (exists) FightUI.Hint(empty.gameObject, issue);
            }
            string pickerName = identityName ?? profile?.Name ?? (chooseProfile != null ? "AI" : Config.Get("DuelPlayerName0", "决斗者"));
            BindHomeCard(artFrame, deckAction ?? (() => PickDeck(pickerName, page, chosen, personalDeck)));
            FightUI.FocusKey(artFrame.GetComponent<Button>(), "battle:choose:" + title);
            FightUI.Hint(artFrame.gameObject, deckAction != null ? "选择人机模式" : exists ? "更换卡组" : "选择卡组");
            float actionsY = h - (Compact ? 48 : 60), actionH = Compact ? 36 : 44;
            if (inspectDeck) FightUI.TextAction(panel, "查看构筑", w - pad - 116, actionsY, 116, actionH, () => ShowBattleDeck(path), preview != null);
        }
        private static void FocusDragonArtwork(RawImage artwork)
        {
            // Wide hero art is also used in narrow preparations. Bias its crop
            // toward the dragon instead of the original empty copy space.
            if (artwork == null || artwork.texture == null) return;
            var uv = artwork.uvRect;
            if (uv.width < .99f) artwork.uvRect = new Rect(Mathf.Min(1 - uv.width, (1 - uv.width) * .83f), uv.y, uv.width, uv.height);
        }
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
            var title = FightUI.RawText(parent, value, x, y, width, height, size, color);
            if (bold) title.fontStyle = FontStyles.Bold;
            var single = title.GetPreferredValues(value, float.PositiveInfinity, float.PositiveInfinity);
            bool wraps = single.x > width || (value ?? "").Contains("\n");
            title.enableWordWrapping = wraps; title.maxVisibleLines = 2;
            float lineHeight = title.GetPreferredValues("国", float.PositiveInfinity, float.PositiveInfinity).y;
            float preferred = wraps ? title.GetPreferredValues(value, width, float.PositiveInfinity).y + 2 : single.y + 2;
            title.rectTransform.sizeDelta = new Vector2(width, wraps ? Mathf.Min(lineHeight * 2 + 4, Mathf.Max(height, preferred)) : Mathf.Max(lineHeight + 2, Mathf.Min(height, preferred)));
            FightUI.RawHint(title.gameObject, value);
            return title;
        }
        private TextMeshProUGUI HomeWrappedText(Transform parent, string value, float x, float y, float width, float height, float size, Color color)
        {
            var text = FightUI.Text(parent, value, x, y, width, height, size, color);
            text.enableWordWrapping = true;
            text.rectTransform.sizeDelta = new Vector2(width, Mathf.Max(height, text.GetPreferredValues(text.text, width, float.PositiveInfinity).y + 2));
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
            Deck deck;
            try { deck = new Deck(path); }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            { ShowStatus("无法读取卡组：" + error.Message, true); return; }
            var width = Mathf.Min(W - 64, 1200); var height = H - 80;
            var box = ShowDialog(FightText.Keep(System.IO.Path.GetFileNameWithoutExtension(path)) + " · 只读构筑", width, height);
            var content = FightUI.Scroll(box, 24, 88, width - 48, height - 112, 1200);
            DrawDeckCards(content, deck, width - 48);
        }
    }
}
