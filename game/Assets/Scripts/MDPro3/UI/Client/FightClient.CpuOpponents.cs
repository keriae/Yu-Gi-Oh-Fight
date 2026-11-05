using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MDPro3.Servant;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WindBot.Game.AI;

namespace MDPro3.UI.Client
{
    internal partial class FightClient
    {
        private static Dictionary<string, string> cpuExecutorDeckFiles;

        // Command names are executor names, not filenames: e.g. Level VIII -> AI_Level8.
        private static string CpuOpponentDeckPath(SoloSelector.BotInfo bot)
        {
            if (bot == null || string.IsNullOrEmpty(bot.command)) return null;
            var arguments = Tools.SplitWithPreservedQuotes(bot.command.Replace("'", "\""));
            if (arguments.Any(argument => argument.StartsWith("Random=", StringComparison.Ordinal))) return null;
            string executor = arguments.FirstOrDefault(argument => argument.StartsWith("Deck=", StringComparison.Ordinal))?.Substring(5);
            if (string.IsNullOrEmpty(executor) || executor == "Lucky") return null;
            if (cpuExecutorDeckFiles == null)
            {
                cpuExecutorDeckFiles = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var type in typeof(DeckAttribute).Assembly.GetTypes())
                {
                    if (!typeof(Executor).IsAssignableFrom(type)) continue;
                    foreach (DeckAttribute attribute in type.GetCustomAttributes(typeof(DeckAttribute), false))
                        cpuExecutorDeckFiles[attribute.Name] = attribute.File;
                }
            }
            if (!cpuExecutorDeckFiles.TryGetValue(executor, out string filename)) return null;
            string path = Path.Combine("Data/Windbot/Decks", filename + Program.EXPANSION_YDK);
            return File.Exists(path) ? path : null;
        }

        private static string CpuOpponentDetails(SoloSelector.BotInfo bot) => (bot?.desc ?? "").Trim();

        private void OpenCpuOpponentPicker()
        {
            var bots = SoloSelector.bots;
            float width = Mathf.Min(860, W - 48), height = Mathf.Min(720, H - 48), inner = width - 56;
            var box = ShowDialog("选择人机模式", width, height);
            float viewportHeight = height - 172;
            var content = FightUI.Scroll(box, 24, 144, width - 48, viewportHeight, viewportHeight, out var scroll);
            scroll.name = "CPU opponent roster";
            TMP_InputField search = null;
            Button clear = null;
            Action draw = () =>
            {
                FightUI.Clear(content);
                string query = (cpuOpponentSearch ?? "").Trim();
                var matches = Enumerable.Range(0, bots.Count).Where(index => string.IsNullOrEmpty(query)
                    || (bots[index].name ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                    || CpuOpponentDetails(bots[index]).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                if (clear != null) clear.gameObject.SetActive(!string.IsNullOrEmpty(cpuOpponentSearch));
                float y = 8, rowWidth = width - 78;
                foreach (int index in matches)
                {
                    var bot = bots[index]; bool selected = index == selectedBot;
                    var row = FightUI.Panel(content, 8, y, rowWidth, 112,
                        selected ? FightUI.Raised : FightUI.PanelColor, true, "CPU opponent " + index);
                    row.GetComponent<Outline>().effectColor = selected ? FightUI.Gold : FightUI.Border;
                    const float thumb = 64, textX = 96;
                    var name = HomeDeckTitle(row, bot.name, textX, 14, rowWidth - textX - 44, 34, 22,
                        selected ? FightUI.Active : FightUI.Foreground);
                    name.richText = false; name.raycastTarget = false;
                    float detailY = 14 + name.rectTransform.rect.height + 8;
                    string detail = CpuOpponentDetails(bot);
                    float detailHeight = 0;
                    if (!string.IsNullOrEmpty(detail))
                    {
                        var description = FightUI.Text(row, detail, textX, detailY, rowWidth - textX - 40, 60, 16, FightUI.Muted);
                        description.richText = false; description.raycastTarget = false; description.overflowMode = TextOverflowModes.Overflow;
                        detailHeight = description.GetPreferredValues(detail, rowWidth - textX - 40, float.PositiveInfinity).y + 4;
                        description.rectTransform.sizeDelta = new Vector2(rowWidth - textX - 40, detailHeight);
                    }
                    float rowHeight = Mathf.Max(112, detailY + detailHeight + 16);
                    row.sizeDelta = new Vector2(rowWidth, rowHeight);
                    if (bot.main0 > 0) FightUI.Art(row, bot.main0, 16, (rowHeight - thumb) / 2, thumb, thumb);
                    else FightUI.Icon(row, "duel", 24, (rowHeight - 48) / 2, 48, 48, FightUI.GoldBorder);
                    FightUI.Icon(row, selected ? "check" : "arrow", rowWidth - 34, (rowHeight - 22) / 2, 22, 22, FightUI.Gold);
                    BindHomeCard(row, () => { selectedBot = index; CloseDialog(); Rebuild(); });
                    FightUI.FocusKey(row.GetComponent<Button>(), "cpu:opponent:" + index);
                    FightUI.Hint(row.gameObject, bot.name + (detail.Length > 0 ? "\n" + detail : ""));
                    y += rowHeight + 12;
                }
                if (matches.Length == 0)
                {
                    FightUI.Text(content, bots.Count == 0 ? "暂无可用电脑对手" : "没有匹配的电脑对手", 12, 24, width - 72, 48, 21, FightUI.Muted, TextAlignmentOptions.Center);
                    if (query.Length > 0)
                        FightUI.FocusKey(FightUI.TextAction(content, "清空搜索", 12, 88, width - 72, 40, () =>
                        {
                            search.text = ""; search.Select(); search.ActivateInputField();
                        }), "cpu:opponent-empty-clear");
                }
                content.sizeDelta = new Vector2(width - 48, Mathf.Max(viewportHeight, y + 8));
                scroll.StopMovement(); scroll.verticalNormalizedPosition = 1;
            };
            search = FightUI.FocusKey(FightUI.Input(box, cpuOpponentSearch, "搜索名称或说明", 28, 84, inner - 54, 44,
                value => { cpuOpponentSearch = value; draw(); }), "cpu:opponent-search");
            clear = FightUI.Button(box, "", width - 72, 84, 44, 44, () =>
            {
                search.text = ""; search.Select(); search.ActivateInputField();
            });
            FightUI.Icon(clear.transform, "close", 12, 12, 20, 20, FightUI.Gold);
            FightUI.FocusKey(clear, "cpu:opponent-search-clear"); FightUI.Hint(clear.gameObject, "清空对手搜索");
            draw();
        }
    }
}
