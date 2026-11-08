using System;
using System.Collections.Generic;
using System.Linq;


using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


namespace YuGiOhFight.Client.UI.Client
{
    internal partial class FightClient
    {
        private void DrawBattleOptionsEntry(float x, float y, bool ai)
        {
            var button = FightUI.FocusKey(FightUI.Button(Body, "对战设置", x, y, 168, 44, () => OpenBattleOptions(ai)), "battle:options");
            FightUI.Hint(button.gameObject, "配置限卡表、决斗规则、生命值、手牌和洗牌");
        }
        private static string ReadBattleBanlistOverride(string key)
        {
            // Older keys were seeded on reads, so they cannot identify a manual override.
            string name = MDPro3.Config.Get(key + "Override", "");
            return MDPro3.Duel.YGOSharp.BanlistManager.IsDuelBanlistName(name) ? name : "";
        }
        private static string ReadBattleBanlistName(string key)
        {
            string name = ReadBattleBanlistOverride(key);
            return name.Length == 0 ? MDPro3.Duel.YGOSharp.BanlistManager.DefaultBanlistName : name;
        }
        private static string ReadCpuBanlistName() => ReadBattleBanlistName("CpuDuelBanlist");
        private static string BattleBanlistLabel(string name) => name == MDPro3.Duel.YGOSharp.BanlistManager.EMPTY_LIST_NAME ? FightText.Get("无（N/A）") : name;
        private static MDPro3.AI.ResponsesDuelOptions ReadAiBattleOptions()
        {
            int value(string key, int fallback) => int.TryParse(MDPro3.Config.Get(key, fallback.ToString()), out int parsed) ? parsed : fallback;
            MDPro3.AI.ResponsesDuelOptions options = new MDPro3.AI.ResponsesDuelOptions
            {
                LifePoints = value("AIDuelLP", 8000), StartingHand = value("AIDuelHand", 5), DrawPerTurn = value("AIDuelDraw", 1),
                MasterRule = value("AIDuelMasterRule", 5), NoShuffle = MDPro3.Config.Get("AIDuelNoShuffle", "0") == "1",
                BanlistName = ReadBattleBanlistName("AIDuelBanlist")
            };
            try { options.Validate(); return options; } catch (MDPro3.AI.AiConfigurationException) { return new MDPro3.AI.ResponsesDuelOptions(); }
        }
        private void OpenBattleOptions(bool ai)
        {
            float width = Mathf.Min(820, W - 48), height = ai ? 598 : 656;
            MDPro3.AI.ResponsesDuelOptions options = ai ? ReadAiBattleOptions() : new MDPro3.AI.ResponsesDuelOptions { LifePoints = cpuLp, StartingHand = cpuHand, DrawPerTurn = cpuDraw, NoShuffle = cpuNoShuffle, BanlistName = ReadCpuBanlistName() };
            string banlistOverride = ReadBattleBanlistOverride(ai ? "AIDuelBanlist" : "CpuDuelBanlist");
            bool noCheck = cpuNoCheck;
            var box = ShowDialog("对战设置", width, height);
            var inner = width - 56;
            FightUI.Text(box, "决斗规则", 28, 84, inner, 32, 20, FightUI.Gold);
            var ruleNames = new[] { "大师规则 1", "大师规则 2", "大师规则 3", "新大师规则", "2020 规则" };
            var tabs = new List<Button>();
            float ruleW = (inner - 32) / 5;
            for (int i = 0; i < 5; i++)
            {
                int rule = i + 1;
                var tab = FightUI.FocusKey(FightUI.Button(box, ruleNames[i], 28 + i * (ruleW + 8), 126, ruleW, 44, () =>
                {
                    options.MasterRule = rule;
                    for (int j = 0; j < tabs.Count; j++) SetLibrarySelection(tabs[j], options.MasterRule == j + 1);
                }, false, ai), "battle:options:rule:" + rule);
                tabs.Add(tab); SetLibrarySelection(tab, options.MasterRule == rule);
            }
            FightUI.Text(box, "限卡表", 28, 196, inner, 30, 18, FightUI.Muted);
            Button regulation = null;
            Action refreshRegulation = () =>
            {
                string label = banlistOverride.Length == 0 ? FightText.Get("使用全局默认") + " · " + BattleBanlistLabel(options.BanlistName) : BattleBanlistLabel(options.BanlistName);
                regulation.GetComponentInChildren<TextMeshProUGUI>().text = label;
                FightUI.RawHint(regulation.gameObject, label);
            };
            regulation = FightUI.FocusKey(FightUI.Button(box, BattleBanlistLabel(options.BanlistName), 28, 234, inner - 156, 44, () =>
            {
                var selections = new List<string> { FightText.Get("限卡表"), string.Empty };
                selections.Add(FightText.Get("使用全局默认"));
                selections.AddRange(MDPro3.Duel.YGOSharp.BanlistManager.GetAllNames().Select(BattleBanlistLabel));
                Runtime.UIManager.ShowPopupSelection(selections, () =>
                {
                    if (regulation == null) return;
                    string selected = EventSystem.current.currentSelectedGameObject.GetComponent<SelectionButton>().GetButtonText();
                    banlistOverride = selected == FightText.Get("使用全局默认") ? ""
                        : selected == FightText.Get("无（N/A）") ? MDPro3.Duel.YGOSharp.BanlistManager.EMPTY_LIST_NAME : selected;
                    options.BanlistName = banlistOverride.Length == 0 ? MDPro3.Duel.YGOSharp.BanlistManager.DefaultBanlistName : banlistOverride;
                    refreshRegulation();
                });
            }, localize: false), "battle:options:banlist");
            refreshRegulation();
            FightUI.FocusKey(FightUI.Button(box, "查看禁限表", width - 172, 234, 144, 44, () => OpenBanlistViewer(options.BanlistName)), "battle:options:banlist:view");
            var errors = new HashSet<string>(); Button apply = null;
            var fields = new Dictionary<string, TMP_InputField>();
            float fieldW = (inner - 32) / 3;
            Action<string, string, int, int, int, float, Action<int>> number = (key, label, initial, min, max, x, changed) =>
            {
                FightUI.Text(box, label, x, 302, fieldW, 30, 18, FightUI.Muted);
                var error = FightUI.Text(box, "", x, 388, fieldW, 28, 15, FightUI.Danger);
                TMP_InputField input = null;
                input = FightUI.FocusKey(FightUI.Input(box, initial.ToString(), "整数", x, 340, fieldW, 44, text =>
                {
                    bool valid = int.TryParse(text, out int parsed) && parsed >= min && parsed <= max;
                    if (valid) { errors.Remove(key); changed(parsed); } else errors.Add(key);
                    error.text = valid ? "" : key == "lp" ? "请输入正整数" : "请输入 " + min + "～" + max + " 的整数";
                    if (input != null) input.GetComponent<Outline>().effectColor = valid ? FightUI.Border : FightUI.Danger;
                    if (apply != null) apply.interactable = errors.Count == 0;
                }), "battle:options:" + key);
                input.keyboardType = TouchScreenKeyboardType.NumberPad; fields[key] = input;
                FightUI.Hint(input.gameObject, min + "～" + max);
            };
            number("lp", "初始生命值", options.LifePoints, 1, int.MaxValue, 28, value => options.LifePoints = value);
            number("hand", "起始手牌", options.StartingHand, 0, 40, 28 + fieldW + 16, value => options.StartingHand = value);
            number("draw", "每回合抽卡", options.DrawPerTurn, 0, 40, 28 + (fieldW + 16) * 2, value => options.DrawPerTurn = value);
            Button shuffle = null;
            shuffle = FightUI.FocusKey(FightUI.SwitchRow(box, "不洗牌", options.NoShuffle, 28, 430, inner, 48,
                () => { options.NoShuffle = !options.NoShuffle; FightUI.SetSwitchValue(shuffle, options.NoShuffle); }), "battle:options:no-shuffle");
            Button check = null;
            if (!ai)
            {
                check = FightUI.FocusKey(FightUI.SwitchRow(box, "不检查构筑", noCheck, 28, 488, inner, 48,
                    () => { noCheck = !noCheck; FightUI.SetSwitchValue(check, noCheck); }), "battle:options:no-check");
            }
            FightUI.TextAction(box, "恢复默认", 28, height - 72, 160, 44, () =>
            {
                options.MasterRule = 5; options.NoShuffle = false; noCheck = false;
                banlistOverride = ""; options.BanlistName = MDPro3.Duel.YGOSharp.BanlistManager.DefaultBanlistName; refreshRegulation();
                fields["lp"].text = "8000"; fields["hand"].text = "5"; fields["draw"].text = "1";
                FightUI.SetSwitchValue(shuffle, false);
                foreach (var tab in tabs) SetLibrarySelection(tab, tab == tabs[4]);
                if (check != null) FightUI.SetSwitchValue(check, false);
            });
            FightUI.FocusKey(FightUI.Button(box, "取消", width - 356, height - 72, 152, 44, CloseDialog), "battle:options:cancel");
            apply = FightUI.FocusKey(FightUI.Button(box, "应用", width - 188, height - 72, 160, 44, () => Try(() =>
            {
                if (errors.Count != 0) return;
                options.Validate();
                if (ai)
                {
                    MDPro3.Config.Set("AIDuelLP", options.LifePoints.ToString()); MDPro3.Config.Set("AIDuelHand", options.StartingHand.ToString());
                    MDPro3.Config.Set("AIDuelDraw", options.DrawPerTurn.ToString()); MDPro3.Config.Set("AIDuelMasterRule", options.MasterRule.ToString());
                    MDPro3.Config.Set("AIDuelNoShuffle", options.NoShuffle ? "1" : "0");
                    MDPro3.Config.Set("AIDuelBanlistOverride", banlistOverride); MDPro3.Config.Save();
                }
                else
                {
                    cpuLp = options.LifePoints; cpuHand = options.StartingHand; cpuDraw = options.DrawPerTurn;
                    cpuNoCheck = noCheck; cpuNoShuffle = options.NoShuffle; invalidCpuFields.Clear(); cpuNumberDrafts.Clear();
                    MDPro3.Config.Set("CpuDuelBanlistOverride", banlistOverride); MDPro3.Config.Save();
                }
                CloseDialog(); Rebuild();
            }), true), "battle:options:apply");
        }
    }
}
