using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI.Client
{
    internal sealed partial class FightDuelMenu
    {
        private List<int> viewedCards = new();
        private int selectedCard, selectedIndex, deckMainCount, deckExtraCount;
        private bool saving;
        private ScrollRect cardReaderScroll, cardMetadataScroll;
        private int readingCard;
        private float readingPosition = 1, metadataReadingPosition = 1;

        private void CaptureCardReading()
        {
            if (cardReaderScroll != null) readingPosition = cardReaderScroll.verticalNormalizedPosition;
            if (cardMetadataScroll != null) metadataReadingPosition = cardMetadataScroll.verticalNormalizedPosition;
        }
        private void ResetCardReading()
        {
            cardReaderScroll = cardMetadataScroll = null; readingCard = 0; readingPosition = metadataReadingPosition = 1;
        }

        private void OpenDeck()
        {
            // TcpHelper.deck is the actual deck submitted for this session, including
            // AI profile, academy and hand-test entries; homepage selection can differ.
            MDPro3.Duel.YGOSharp.Deck deck = Controllers.OcgCore.sideChangedDeck ?? Runtime.Network.TcpHelper.deck;
            if (deck == null) { Runtime.MessageManager.Toast(FightText.Get("本次卡组尚未加载。")); return; }
            viewedCards = deck.Main.Concat(deck.Extra).Concat(deck.Side).ToList();
            deckMainCount = deck.Main.Count; deckExtraCount = deck.Extra.Count;
            selectedCard = viewedCards.FirstOrDefault(); selectedIndex = 0;
            Open("deck", "settings");
        }
        private string EffectsSignature()
        {
            CardList list = Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>()?.CardList;
            return list == null ? "" : string.Join(",", list.AuxiliaryEffects.Select(item => item.Key + ":" + item.Value));
        }
        private void RenderEffects()
        {
            CardList list = Runtime.Program.instance.ocgcore.GetUI<ServantUI.OcgCoreUI>()?.CardList;
            var effects = list == null ? new KeyValuePair<int, int>[0] : list.AuxiliaryEffects.ToArray();
            viewedCards = effects.Select(item => item.Key).ToList();
            if (!viewedCards.Contains(selectedCard)) selectedCard = viewedCards.FirstOrDefault();
            selectedIndex = viewedCards.IndexOf(selectedCard);
            FightUI.Text(body, "当前生效的附加效果", 0, 0, 570, 30, 20, FightUI.Gold);
            FightUI.RawText(body, effects.Length.ToString(), 500, 0, 80, 30, 16, FightUI.Muted, TextAlignmentOptions.MidlineRight);
            var content = FightUI.Scroll(body, 0, 42, 572, 392, effects.Length * 118, out scroll);
            for (int i = 0; i < effects.Length; i++)
            {
                var effect = effects[i]; MDPro3.Duel.YGOSharp.Card card = MDPro3.Duel.YGOSharp.CardsManager.Get(effect.Key);
                var row = FightUI.Panel(content, 4, i * 118, 548, 106, selectedCard == effect.Key ? FightUI.Raised : FightUI.Background, true, "Attached effect " + effect.Key);
                var image = FightUI.Art(row, effect.Key, 12, 8, 62, 90, true);
                FightUI.FocusKey(FightUI.CardButton(image, () => SelectCard(effect.Key)), "duel:effect-art:" + effect.Key);
                FightUI.RawText(row, card.Name, 92, 14, 432, 34, 19, FightUI.Foreground);
                string owner = effect.Value == 1 ? "我方" : effect.Value == 2 ? "对方" : "双方";
                FightUI.Text(row, owner, 92, 58, 400, 28, 15, effect.Value == 2 ? FightUI.Danger : FightUI.CyanText);
                var button = FightUI.FocusKey(row.gameObject.AddComponent<Button>(), "duel:effect:" + effect.Key);
                button.targetGraphic = row.GetComponent<Image>(); button.onClick.AddListener(() => SelectCard(effect.Key));
            }
            if (effects.Length == 0)
            {
                FightUI.Icon(content, "diamond", 252, 74, 56, 56, FightUI.GoldBorder);
                FightUI.Text(content, "暂无附加效果", 24, 146, 524, 38, 24, FightUI.Gold, TextAlignmentOptions.Center);
                FightUI.Text(content, "卡片产生附加效果后，会显示在这里。", 24, 198, 524, 76, 18, FightUI.Muted, TextAlignmentOptions.Center);
            }
            scroll.verticalNormalizedPosition = scrollPosition;
            RenderCardReader(body, 596, 0, 468, 434);
        }
        private void RenderDeck()
        {
            FightUI.Text(body, "本次对局使用的构筑", 0, 0, 550, 30, 20, FightUI.Gold);
            FightUI.Text(body, viewedCards.Count + " 张", 480, 0, 92, 30, 16, FightUI.Muted, TextAlignmentOptions.MidlineRight);
            var content = FightUI.Scroll(body, 0, 42, 572, 392, 1000, out scroll);
            int offset = 0; float top = 0;
            foreach (var section in new[] { ("主卡组", deckMainCount), ("额外卡组", deckExtraCount), ("副卡组", viewedCards.Count - deckMainCount - deckExtraCount) })
            {
                if (section.Item2 == 0) continue;
                FightUI.Text(content, section.Item1, 8, top, 440, 30, 16, FightUI.Gold);
                FightUI.RawText(content, section.Item2.ToString(), 474, top, 76, 30, 16, FightUI.Muted, TextAlignmentOptions.MidlineRight);
                top += 38;
                for (int local = 0; local < section.Item2; local++)
                {
                    int index = offset + local, code = viewedCards[index]; float x = 8 + local % 5 * 110, y = top + local / 5 * 148;
                    var image = FightUI.Art(content, code, x, y, 92, 134, true);
                    var button = FightUI.FocusKey(FightUI.CardButton(image, () => { selectedCard = code; selectedIndex = index; Render(); }), "duel:deck-card:" + index);
                    var outline = image.transform.parent.GetComponent<Outline>(); outline.enabled = selectedIndex == index;
                    FightUI.RawHint(button.gameObject, MDPro3.Duel.YGOSharp.CardsManager.Get(code).Name);
                }
                top += Mathf.Ceil(section.Item2 / 5f) * 148 + 20; offset += section.Item2;
            }
            content.sizeDelta = new Vector2(572, Mathf.Max(392, top)); scroll.verticalNormalizedPosition = scrollPosition;
            RenderCardReader(body, 596, 0, 468, 434);
        }
        private void SelectCard(int code)
        {
            selectedCard = code; selectedIndex = viewedCards.IndexOf(code); Render();
        }
        private void RenderCardReader(Transform parent, float x, float y, float w, float h)
        {
            float position = readingCard == selectedCard ? readingPosition : 1;
            float metadataPosition = readingCard == selectedCard ? metadataReadingPosition : 1;
            readingCard = selectedCard; readingPosition = position; metadataReadingPosition = metadataPosition;
            cardReaderScroll = cardMetadataScroll = null;
            var reader = FightUI.Panel(parent, x, y, w, h, new Color(.019f, .042f, .057f, .7f), true, "Duel card reader");
            if (selectedCard <= 0)
            { FightUI.Text(reader, "选择一张卡片查看详情", 20, 136, w - 40, 80, 20, FightUI.Muted, TextAlignmentOptions.Center); return; }
            MDPro3.Duel.YGOSharp.Card card = MDPro3.Duel.YGOSharp.CardsManager.Get(selectedCard);
            var title = FightUI.RawText(reader, card.Name, 20, 16, w - 40, 54, 23, FightUI.Active);
            title.enableWordWrapping = true; title.enableAutoSizing = true; title.fontSizeMin = 18; title.fontSizeMax = 23;
            var image = FightUI.Art(reader, selectedCard, 20, 88, 106, 154, true);
            FightUI.CardButton(image, ShowLargeCard);
            cardMetadataScroll = RenderCardMetadata(reader, card, 144, 88, w - 164, 112, 16, false);
            cardMetadataScroll.verticalNormalizedPosition = metadataPosition;
            AddCardReadingControl(cardMetadataScroll, "duel:read:metadata");
            FightUI.Button(reader, "完整卡片信息", 144, 212, w - 164, 40, ShowCardDialog);
            var description = FightUI.Scroll(reader, 18, 270, w - 36, h - 288, 600, out cardReaderScroll);
            var text = FightUI.RawText(description, card.GetDescription(), 8, 0, w - 68, 600, 17, FightUI.Foreground);
            text.enableWordWrapping = true; text.overflowMode = TextOverflowModes.Overflow; text.lineSpacing = 4;
            float descriptionHeight = Mathf.Max(h - 288, text.GetPreferredValues(text.text, w - 68, float.PositiveInfinity).y + 18);
            description.sizeDelta = new Vector2(w - 36, descriptionHeight); text.rectTransform.sizeDelta = new Vector2(w - 68, descriptionHeight);
            cardReaderScroll.verticalNormalizedPosition = position;
            AddCardReadingControl(cardReaderScroll, "duel:read:description");
        }
        private void ShowCardDialog()
        {
            MDPro3.Duel.YGOSharp.Card card = MDPro3.Duel.YGOSharp.CardsManager.Get(selectedCard); var panel = ShowDialog("完整卡片信息", 600);
            var title = FightUI.RawText(panel, card.Name, 24, 86, 580, 50, 23, FightUI.Active);
            title.enableWordWrapping = true; title.enableAutoSizing = true; title.fontSizeMin = 18; title.fontSizeMax = 23;
            var image = FightUI.Art(panel, selectedCard, 24, 150, 160, 232, true);
            FightUI.CardButton(image, ShowLargeCard);
            var metadata = RenderCardMetadata(panel, card, 208, 150, 396, 132, 17, true);
            AddCardReadingControl(metadata, "duel:read:full-metadata");
            FightUI.Button(panel, "放大卡图", 208, 296, 188, 44, ShowLargeCard);
            FightUI.Button(panel, "保存卡图", 412, 296, 192, 44, () => SaveCardImage(image));
            var content = FightUI.Scroll(panel, 20, 398, 588, 174, 800, out var descriptionScroll);
            var description = FightUI.RawText(content, card.GetDescription(), 8, 0, 560, 800, 18, FightUI.Foreground);
            description.overflowMode = TextOverflowModes.Overflow; description.lineSpacing = 4;
            float textHeight = Mathf.Max(174, description.GetPreferredValues(description.text, 560, float.PositiveInfinity).y + 18);
            content.sizeDelta = new Vector2(588, textHeight); description.rectTransform.sizeDelta = new Vector2(560, textHeight);
            AddCardReadingControl(descriptionScroll, "duel:read:full-description");
            RefreshNavigation();
        }
        private static string CardMetadata(MDPro3.Duel.YGOSharp.Card card, bool includeId)
        {
            string type = card.GetTypeForUI();
            var lines = new List<string>();
            if (card.HasType(MDPro3.Duel.YGOSharp.CardType.Monster))
            {
                lines.Add(card.GetAttributeString() + "  " + type);
                MDPro3.Duel.YGOSharp.Card.LevelType levelType = card.GetLevelType();
                string level = levelType == MDPro3.Duel.YGOSharp.Card.LevelType.Link ? "LINK-" + card.GetLinkCount()
                    : FightText.Get(levelType == MDPro3.Duel.YGOSharp.Card.LevelType.Rank ? "阶级 " : "等级 ") + card.Level;
                lines.Add(level + "  ·  ATK " + card.GetAttackString()
                    + (levelType == MDPro3.Duel.YGOSharp.Card.LevelType.Link ? "" : " / DEF " + card.GetDefenseString()));
                if (card.HasType(MDPro3.Duel.YGOSharp.CardType.Pendulum)) lines.Add(FightText.Get("灵摆刻度") + " " + card.LScale + " / " + card.RScale);
            }
            else
            {
                string subType = card.GetSpellTrapType();
                lines.Add(string.IsNullOrEmpty(subType) ? type : type + "  " + subType);
            }
            string sets = card.GetSetNameWithBracket();
            if (!string.IsNullOrEmpty(sets)) lines.Add(sets);
            if (includeId) lines.Add(Duel.CardDescription.GetDisplayedCardIdWithBracket(card));
            return string.Join("\n", lines);
        }
        private static ScrollRect RenderCardMetadata(Transform parent, MDPro3.Duel.YGOSharp.Card card, float x, float y, float width, float height, float size, bool includeId)
        {
            var content = FightUI.Scroll(parent, x, y, width, height, height, out var metadataScroll);
            metadataScroll.name = "Duel card metadata";
            metadataScroll.GetComponent<Image>().color = Color.clear;
            var text = FightUI.RawText(content, CardMetadata(card, includeId), 4, 0, width - 24, height, size, FightUI.Muted);
            text.name = "Duel card metadata text";
            text.enableWordWrapping = true; text.overflowMode = TextOverflowModes.Overflow; text.lineSpacing = 2;
            text.ForceMeshUpdate();
            float contentHeight = Mathf.Max(height, text.GetPreferredValues(text.text, width - 24, float.PositiveInfinity).y + 10);
            content.sizeDelta = new Vector2(width, contentHeight);
            text.rectTransform.sizeDelta = new Vector2(width - 24, contentHeight);
            metadataScroll.verticalNormalizedPosition = 1;
            return metadataScroll;
        }
        private void ShowLargeCard()
        {
            var panel = ShowDialog("卡片预览", 640, nested: dialog != null);
            FightUI.Art(panel, selectedCard, 133, 88, 362, 524, true);
            RefreshNavigation();
        }
        private void SaveCardImage(RawImage image)
        {
            if (saving) return;
            if (image == null || image.texture == null) { Runtime.MessageManager.Toast(FightText.Get("卡图正在加载，请稍候。")); return; }
            saving = true;
            RenderTexture target = null; Texture2D copy = null; var previous = RenderTexture.active;
            try
            {
                var texture = image.texture;
                target = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(texture, target); RenderTexture.active = target;
                copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); copy.Apply();
                Directory.CreateDirectory(Runtime.Program.PATH_CARD_PIC);
                File.WriteAllBytes(Path.Combine(Runtime.Program.PATH_CARD_PIC, selectedCard + ".png"), copy.EncodeToPNG());
                Runtime.MessageManager.Toast(FightText.Get("卡图已保存。"));
            }
            catch (Exception error) { Debug.LogWarning("[FightDuelMenu] card export: " + error.GetType().Name); Runtime.MessageManager.Toast(FightText.Get("卡图保存失败。")); }
            finally { RenderTexture.active = previous; if (target != null) RenderTexture.ReleaseTemporary(target); if (copy != null) Destroy(copy); saving = false; }
        }
    }
}
