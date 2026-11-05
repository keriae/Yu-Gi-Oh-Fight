using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MDPro3.Servant;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace MDPro3.UI.Client
{
    internal sealed partial class FightDuelMenu
    {
        private int appearanceSide;
        private string AppearanceSlot => appearanceSide == 2 ? "0Tag" : appearanceSide == 3 ? "1Tag" : appearanceSide.ToString();

        private void ShowAppearance()
        {
            string prefix = OcgCore.condition.ToString();
            var panel = ShowDialog("外观与角色", 600);
            var scopes = RoomServant.Mode == 2 ? new[] { "我方", "对方", "我方队友", "对方队友" } : new[] { "我方", "对方" };
            if (appearanceSide >= scopes.Length) appearanceSide = 0;
            float tabWidth = (580 - (scopes.Length - 1) * 12) / scopes.Length;
            for (int i = 0; i < scopes.Length; i++)
            {
                int side = i; FightUI.Tab(panel, scopes[i], appearanceSide == i, 24 + i * (tabWidth + 12), 84, tabWidth, 42, () => { appearanceSide = side; ShowAppearance(); });
            }
            FightUI.Text(panel, "外观设置于下一场对局生效。", 24, 134, 580, 30, 15, FightUI.Muted);
            var content = FightUI.Scroll(panel, 20, 178, 588, 394, 900);
            float offset = 0;
            if (OcgCore.condition == OcgCore.Condition.Duel && appearanceSide == 0)
            {
                FightUI.SwitchRow(content, "使用此外观覆盖卡组外观", Config.GetBool("OverrideDeckAppearance", false), 4, 0, 562, 44,
                    () => { Config.SetBool("OverrideDeckAppearance", !Config.GetBool("OverrideDeckAppearance", false)); Config.Save(); ShowAppearance(); });
                offset = 58;
            }
            FightUI.Text(content, "决斗者名称", 4, offset, 560, 26, 17, FightUI.Gold);
            string nameKey = prefix + "PlayerName" + AppearanceSlot;
            var nameInput = FightUI.Input(content, (Config.Get(nameKey, "") == Config.EMPTY_STRING ? "" : Config.Get(nameKey, "")), "使用默认名称", 4, offset + 34, 562, 44,
                next => Config.Set(nameKey, string.IsNullOrWhiteSpace(next) ? Config.EMPTY_STRING : next));
            nameInput.onEndEdit.AddListener(_ => Config.Save());
            FightUI.FocusKey(nameInput, "duel:appearance:name");
            float y = offset + 96;
            foreach (var entry in new[] { ("Character", "角色"), ("Face", "头像"), ("Frame", "头像框"), ("Protector", "卡套"), ("Field", "场地"), ("Grave", "墓地外观"), ("Stand", "伙伴台座"), ("Mate", "伙伴"), ("Wallpaper", "壁纸") })
            {
                var field = entry;
                FightUI.Text(content, field.Item2, 4, y, 150, 44, 17, FightUI.Foreground, TextAlignmentOptions.MidlineLeft);
                string key = field.Item1 == "Wallpaper" ? "Wallpaper" : prefix + field.Item1 + AppearanceSlot;
                string current = Config.Get(key, "0");
                string caption = field.Item1 == "Character" ? CharacterSelector.characters?.GetName(current) ?? current
                    : ItemOptions(field.Item1).FirstOrDefault(item => item.id.ToString() == current).name;
                if (current == Items.CODE_RANDOM.ToString()) caption = FightText.Get("随机");
                else if (current == Items.CODE_SAME.ToString()) caption = FightText.Get("与场地相同");
                else if (current == "0" && field.Item1 == "Character") caption = FightText.Get("使用默认");
                else if (current == "0" && (field.Item1 == "Mate" || field.Item1 == "Stand" || field.Item1 == "Wallpaper")) caption = FightText.Get("不显示");
                else if (string.IsNullOrWhiteSpace(caption) || caption == Items.STRING_NULL) caption = current == "0" ? FightText.Get("使用默认") : current;
                FightUI.FocusKey(FightUI.Button(content, caption, 168, y, 398, 44, () => ChooseAppearance(field.Item1, field.Item2, key, current), localize: false), "duel:appearance:" + field.Item1);
                y += 56;
            }
            content.sizeDelta = new Vector2(588, y + 12);
        }
        private static Items.ItemType ItemType(string category) => category switch
        {
            "Face" => Items.ItemType.Face, "Frame" => Items.ItemType.Frame, "Protector" => Items.ItemType.Protector,
            "Field" => Items.ItemType.Mat, "Grave" => Items.ItemType.Grave, "Stand" => Items.ItemType.Stand,
            "Mate" => Items.ItemType.Mate, "Wallpaper" => Items.ItemType.Wallpaper, _ => Items.ItemType.Unknown
        };
        private static List<Items.Item> ItemOptions(string category)
        {
            var items = Program.items;
            return category switch { "Face" => items.faces, "Frame" => items.frames, "Protector" => items.protectors,
                "Field" => items.mats, "Grave" => items.graves, "Stand" => items.stands, "Mate" => items.mates,
                "Wallpaper" => items.wallpapers, _ => new List<Items.Item>() };
        }
        private static bool ItemInstalled(Items.Item item, string category)
        {
            if (item.notReady || item.id <= 9999) return false;
            string path = Program.root + "MasterDuel/" + item.path;
            if (category == "Field" || category == "Grave" || category == "Stand") return File.Exists(path + "_near");
            if (category == "Mate") return item.path != null && (item.path.EndsWith("_Folder", StringComparison.Ordinal) ? Directory.Exists(path.Replace("_Folder", "")) : File.Exists(path));
            if (category == "Wallpaper") return File.Exists(path) || Directory.Exists(path);
            if (category == "Protector") return File.Exists(path) || Directory.Exists(path) || Directory.Exists(Program.root + "MasterDuel/Protector/" + item.id);
            return Addressables.ResourceLocators.Any(locator => locator.Locate(Items.GetIconAddress(item.id.ToString()), typeof(Sprite), out var locations) && locations.Count > 0);
        }
        private void ChooseAppearance(string category, string label, string key, string current)
        {
            var panel = ShowDialog("选择" + label, 600);
            var list = FightUI.Scroll(panel, 20, 144, 588, 428, 1000);
            Action<string> draw = term =>
            {
                FightUI.Clear(list);
                List<(string Id, string Name)> choices;
                if (category == "Character")
                {
                    var data = CharacterSelector.characters;
                    choices = data == null ? new List<(string, string)>() : Enumerable.Range(0, 10).SelectMany(i => data.GetSeriesCharacters(i.ToString("00")))
                        .Where(item => !item.notReady && Addressables.ResourceLocators.Any(locator => locator.Locate("sn" + item.id, typeof(Sprite), out var locations) && locations.Count > 0))
                        .Select(item => (item.id, data.GetName(item.id))).ToList();
                }
                else choices = ItemOptions(category).Where(item => ItemInstalled(item, category)).Select(item => (item.id.ToString(), item.name)).ToList();
                choices = choices.Where(item => string.IsNullOrWhiteSpace(term) || (item.Name ?? "").IndexOf(term.Trim(), StringComparison.OrdinalIgnoreCase) >= 0 || item.Id.Contains(term.Trim())).ToList();
                if (category != "Character") choices.Insert(0, (Items.CODE_RANDOM.ToString(), FightText.Get("随机")));
                if (category == "Grave" || category == "Stand") choices.Insert(0, (Items.CODE_SAME.ToString(), FightText.Get("与场地相同")));
                if (category == "Mate" || category == "Stand" || category == "Wallpaper") choices.Insert(0, ("0", FightText.Get("不显示")));
                const float cellW = 134, cellH = 178;
                for (int i = 0; i < choices.Count; i++)
                {
                    var item = choices[i]; bool selected = item.Id == current;
                    var tile = FightUI.FocusKey(FightUI.Button(list, "", 4 + i % 4 * 144, i / 4 * 190, cellW, cellH, () =>
                    {
                        Config.Set(key, item.Id); Config.Save();
                        if (category == "Wallpaper") { UIManager.currentWallpaper = item.Id; Program.instance.ui_.ChangeWallpaper(item.Id); }
                        ShowAppearance();
                    }, selected), "duel:appearance-item:" + item.Id);
                    var image = FightUI.Node(tile.transform, "Duel appearance image", 8, 8, cellW - 16, 112).gameObject.AddComponent<Image>();
                    image.preserveAspect = true; image.raycastTarget = false; image.color = Color.clear;
                    if (!int.TryParse(item.Id, out int number) || number > 9999 || category == "Character") image.gameObject.AddComponent<FightAiProfileImage>().Load(image, item.Id, category, ItemType(category));
                    else FightUI.Icon(tile.transform, "diamond", 44, 44, 46, 46, FightUI.GoldBorder);
                    var caption = FightUI.RawText(tile.transform, item.Name, 8, 126, cellW - 16, 44, 14, FightUI.Foreground, TextAlignmentOptions.Center);
                    caption.enableWordWrapping = true; caption.enableAutoSizing = true; caption.fontSizeMin = 11; caption.fontSizeMax = 14;
                    FightUI.RawHint(tile.gameObject, item.Name);
                }
                if (choices.Count == 0) FightUI.Text(list, "没有匹配素材", 12, 54, 548, 60, 20, FightUI.Muted, TextAlignmentOptions.Center);
                list.sizeDelta = new Vector2(588, Mathf.Max(428, Mathf.Ceil(choices.Count / 4f) * 190));
            };
            FightUI.FocusKey(FightUI.Input(panel, "", "搜索素材名称或编号", 24, 86, 580, 42, draw), "duel:appearance-search");
            draw("");
        }

    }
}
