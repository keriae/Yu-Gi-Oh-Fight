using System;
using System.IO;
using SFB;
using UnityEngine;

namespace YuGiOhFight.Client.UI.Client
{
    internal partial class FightClient
    {
        private Services.ReplayVideoExportOptions replayExportDraft;
        private void ShowReplayExport(string path)
        {
            if (replayExportDraft == null) replayExportDraft = new Services.ReplayVideoExportOptions();
            var draft = replayExportDraft;
            var box = ShowDialog("导出回放视频", 680, 500, true);
            FightUI.Text(box, FightText.Keep(Path.GetFileNameWithoutExtension(path)), 28, 82, 624, 36, 20);
            ExportChoice(box, "分辨率", draft.Width + " × " + draft.Height, 136, () => SettingsChoose("分辨率",
                new[] { "1280x720", "1920x1080", "2560x1440", "3840x2160" }, new[] { "1280 × 720 · HD", "1920 × 1080 · Full HD", "2560 × 1440 · QHD", "3840 × 2160 · 4K" }, value =>
                { var size = value.Split('x'); draft.Width = int.Parse(size[0]); draft.Height = int.Parse(size[1]); ShowReplayExport(path); }, draft.Width + "x" + draft.Height));
            ExportChoice(box, "FPS", draft.Fps.ToString(), 200, () => SettingsChoose("FPS", new[] { "24", "30", "60", "120" }, new[] { "24 FPS", "30 FPS", "60 FPS", "120 FPS" }, value => { draft.Fps = int.Parse(value); ShowReplayExport(path); }, draft.Fps.ToString()));
            ExportChoice(box, "画质", draft.Quality == "high" ? "高清" : draft.Quality == "medium" ? "均衡" : "小文件", 264,
                () => SettingsChoose("画质", new[] { "high", "medium", "small" }, new[] { "高清", "均衡", "小文件" }, value => { draft.Quality = value; ShowReplayExport(path); }, draft.Quality));
            FightUI.SwitchRow(box, "包含游戏音频", draft.Audio, 28, 328, 624, 46, () => { draft.Audio = !draft.Audio; ShowReplayExport(path); });
            FightUI.Text(box, "全屏渲染导出 · 导出期间按 Esc 取消", 28, 384, 624, 36, 16, FightUI.Muted);
            FightUI.Button(box, "取消", 324, 432, 144, 48, CloseDialog);
            FightUI.Button(box, "选择保存位置", 484, 432, 168, 48, () =>
            {
                StandaloneFileBrowser.SaveFilePanelAsync(FightText.Get("导出回放视频"), Environment.GetFolderPath(Environment.SpecialFolder.Desktop), Path.GetFileNameWithoutExtension(path) + ".mp4", "mp4", output =>
                {
                    if (string.IsNullOrWhiteSpace(output)) return;
                    Action begin = () => Try(() =>
                    {
                        draft.Output = output; draft.Overwrite = File.Exists(output);
                        draft.Validate();
                        var export = new Services.ReplayVideoExportOptions { Width = draft.Width, Height = draft.Height, Fps = draft.Fps, Quality = draft.Quality, Audio = draft.Audio, Output = output, Overwrite = draft.Overwrite };
                        CloseDialog();
                        StartCore(() => Services.ReplayVideoExport.Start(ReplayName(path), export, () =>
                        {
                            Runtime.Program.instance.replay.GetUI<ServantUI.ReplaySelectorUI>().KF_Replay(ReplayName(path));
                            Runtime.Program.instance.ocgcore.returnServant = Runtime.Program.instance.menu;
                        }, (message, error) =>
                        {
                            Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.menu);
                            ShowStatus(message, error);
                        }));
                    });
                    if (File.Exists(output)) Confirm("覆盖导出文件", "目标文件已存在，是否覆盖？", begin); else begin();
                });
            }, true);
        }
        private void ExportChoice(Transform box, string label, string value, float y, Action choose)
        {
            FightUI.Text(box, label, 28, y, 180, 48, 20);
            var button = FightUI.Button(box, value, 224, y, 428, 48, choose);
            StyleSettingsChoice(button, 428, 48);
        }
    }
}
