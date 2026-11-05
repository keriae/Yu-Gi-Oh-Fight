using System;
using System.Collections.Generic;
using MDPro3.AI;
using MDPro3.Servant;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI.Client
{
    internal partial class FightClient
    {
        private void DrawBattleOptionsEntry(float x, float y, bool ai)
        {
            var button = FightUI.FocusKey(FightUI.Button(Body, "对战设置", x, y, 168, 44, () => OpenBattleOptions(ai)), "battle:options");
            FightUI.Hint(button.gameObject, "配置决斗规则、生命值、手牌和洗牌");
        }
        private static ResponsesDuelOptions ReadAiBattleOptions()
        {
            int value(string key, int fallback) => int.TryParse(Config.Get(key, fallback.ToString()), out int parsed) ? parsed : fallback;
            var options = new ResponsesDuelOptions
            {
                LifePoints = value("AIDuelLP", 8000), StartingHand = value("AIDuelHand", 5), DrawPerTurn = value("AIDuelDraw", 1),
                MasterRule = value("AIDuelMasterRule", 5), NoShuffle = Config.Get("AIDuelNoShuffle", "0") == "1"
            };
            try { options.Validate(); return options; } catch (AiConfigurationException) { return new ResponsesDuelOptions(); }
        }
        private void OpenBattleOptions(bool ai)
        {
            float width = Mathf.Min(820, W - 48), height = ai ? 492 : 550;
            var options = ai ? ReadAiBattleOptions() : new ResponsesDuelOptions { LifePoints = cpuLp, StartingHand = cpuHand, DrawPerTurn = cpuDraw, NoShuffle = cpuNoShuffle };
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
            var errors = new HashSet<string>(); Button apply = null;
            var fields = new Dictionary<string, TMP_InputField>();
            float fieldW = (inner - 32) / 3;
            Action<string, string, int, int, int, float, Action<int>> number = (key, label, initial, min, max, x, changed) =>
            {
                FightUI.Text(box, label, x, 196, fieldW, 30, 18, FightUI.Muted);
                var error = FightUI.Text(box, "", x, 282, fieldW, 28, 15, FightUI.Danger);
                TMP_InputField input = null;
                input = FightUI.FocusKey(FightUI.Input(box, initial.ToString(), "整数", x, 234, fieldW, 44, text =>
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
            shuffle = FightUI.FocusKey(FightUI.SwitchRow(box, "不洗牌", options.NoShuffle, 28, 324, inner, 48,
                () => { options.NoShuffle = !options.NoShuffle; FightUI.SetSwitchValue(shuffle, options.NoShuffle); }), "battle:options:no-shuffle");
            if (!ai)
            {
                Button check = null;
                check = FightUI.SwitchRow(box, "不检查构筑", noCheck, 28, 382, inner, 48,
                    () => { noCheck = !noCheck; FightUI.SetSwitchValue(check, noCheck); });
            }
            FightUI.TextAction(box, "恢复默认", 28, height - 72, 160, 44, () =>
            {
                options.MasterRule = 5; options.NoShuffle = false; noCheck = false;
                fields["lp"].text = "8000"; fields["hand"].text = "5"; fields["draw"].text = "1";
                FightUI.SetSwitchValue(shuffle, false);
                foreach (var tab in tabs) SetLibrarySelection(tab, tab == tabs[4]);
                var check = box.GetComponentsInChildren<Button>();
                foreach (var button in check) if (button.name == "不检查构筑") FightUI.SetSwitchValue(button, false);
            });
            FightUI.FocusKey(FightUI.Button(box, "取消", width - 356, height - 72, 152, 44, CloseDialog), "battle:options:cancel");
            apply = FightUI.FocusKey(FightUI.Button(box, "应用", width - 188, height - 72, 160, 44, () => Try(() =>
            {
                if (errors.Count != 0) return;
                options.Validate();
                if (ai)
                {
                    Config.Set("AIDuelLP", options.LifePoints.ToString()); Config.Set("AIDuelHand", options.StartingHand.ToString());
                    Config.Set("AIDuelDraw", options.DrawPerTurn.ToString()); Config.Set("AIDuelMasterRule", options.MasterRule.ToString());
                    Config.Set("AIDuelNoShuffle", options.NoShuffle ? "1" : "0"); Config.Save();
                }
                else
                {
                    cpuLp = options.LifePoints; cpuHand = options.StartingHand; cpuDraw = options.DrawPerTurn;
                    cpuNoCheck = noCheck; cpuNoShuffle = options.NoShuffle; invalidCpuFields.Clear(); cpuNumberDrafts.Clear();
                }
                CloseDialog(); Rebuild();
            }), true), "battle:options:apply");
        }
    }
}
