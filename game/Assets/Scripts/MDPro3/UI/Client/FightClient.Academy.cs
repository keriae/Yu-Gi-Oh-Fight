using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;
using MDPro3.Utility;
using TMPro;
using UnityEngine;
using YRP = Percy.YRP;

namespace MDPro3.UI.Client
{
    internal partial class FightClient
    {
        private string academyCategory = "全部", selectedPuzzle, selectedReplay;
        private RectTransform academyGrid, academyPreview, fileList, filePreview;
        private DeckAcademyDeck academySelection;
        private bool replayLoading, replaySortTime = true;
        private List<string> libraryFiles;
        private sealed class LibraryPreviewItem
        {
            internal object Value;
            internal UnityEngine.UI.Button Button;
            internal TextMeshProUGUI Heading;
            internal FightIcon[] Icons;
            internal UnityEngine.UI.Image Divider;
            internal RectTransform Accent;
        }
        private readonly List<LibraryPreviewItem> academyPreviewItems = new List<LibraryPreviewItem>(),
            puzzlePreviewItems = new List<LibraryPreviewItem>(), replayPreviewItems = new List<LibraryPreviewItem>();
        private sealed class LibraryScrollMemory
        {
            internal string Condition;
            internal float Position = 1;
            internal int SelectedStep = int.MinValue;
        }
        private readonly LibraryScrollMemory academyScroll = new LibraryScrollMemory(), puzzleScroll = new LibraryScrollMemory(),
            replayScroll = new LibraryScrollMemory(), tutorialScroll = new LibraryScrollMemory();

        private void RenderAcademy()
        {
            PageTitle("卡组学院");
            FightUI.TextAction(Body, "残局挑战  →", BodyWidth - 184, 4, 184, 44, () => Navigate("puzzles"));
            float previewW = Compact ? 300 : 368, gridW = BodyWidth - previewW - Gap;
            float searchW = Mathf.Min(380, gridW * .34f);
            FightUI.Input(Body, search, "搜索主题或卡名", 0, 96, searchW, 44, v => { search = v; RenderAcademyGrid(); });
            var cats = new[] { "全部" }.Concat(academy.decks.Select(DeckAcademy.Category).Distinct()).ToList();
            float x = searchW + Gap, width = (gridW - x) / cats.Count;
            for (int i = 0; i < cats.Count; i++)
            {
                var cat = cats[i];
                LibraryTab(Body, cat, cat == academyCategory, x + i * width, 96, width - 4,
                    () => { academyCategory = cat; Rebuild(); });
            }
            academyGrid = FightUI.Node(Body, "Academy Grid", 0, 160, gridW, BodyHeight - 160);
            academyPreview = FightUI.Panel(Body, gridW + Gap, 96, previewW, BodyHeight - 96, FightUI.PanelColor, false, "Learning preview");
            RenderAcademyGrid();
        }
        private void RenderAcademyGrid()
        {
            if (academyGrid == null) return;
            BeginLibraryRefresh(academyScroll, search + "\t" + academyCategory);
            FightUI.Clear(academyGrid);
            academyPreviewItems.Clear();
            var items = academy.decks.Where(d => DeckAcademy.Matches(d, search, academyCategory == "全部" ? "" : academyCategory)).ToList();
            if (academySelection == null || !items.Contains(academySelection)) academySelection = items.FirstOrDefault();
            int cols = Compact ? 3 : LargeViewport ? 6 : 4;
            const float cardGap = 16;
            float gridW = academyGrid.rect.width, cw = (gridW - cardGap * (cols - 1)) / cols;
            float coverHeight = cw * .75f, ch = coverHeight + 148;
            var c = FightUI.Scroll(academyGrid, 0, 0, gridW, academyGrid.rect.height, Mathf.Ceil((float)items.Count / cols) * (ch + cardGap), out var scroll);
            scroll.name = "Academy directory navigation";
            if (items.Count == 0)
            {
                LibraryEmpty(c, "没有符合条件的教程", "试试其他主题，或清除搜索条件。", gridW, academyGrid.rect.height, "diamond");
                FightUI.TextAction(c, "清除筛选  →", (gridW - 180) / 2, Mathf.Min(academyGrid.rect.height - 52, 326), 180, 44, () => { search = ""; academyCategory = "全部"; Rebuild(); });
            }
            for (int i = 0; i < items.Count; i++)
            {
                var d = items[i]; bool selected = d == academySelection;
                var card = LibrarySelection(c, i % cols * (cw + cardGap), i / cols * (ch + cardGap), cw, ch, selected,
                    () => { PreviewAcademy(d); OpenTutorial(d); });
                FightUI.PreviewOnFocus(card, () => PreviewAcademy(d));
                var deck = DeckAcademy.Template(d);
                FightUI.Art(card.transform, DeckLibrary.CoverFor(deck), 5, 5, cw - 10, coverHeight);
                var title = FightUI.Text(card.transform, d.name, 12, coverHeight + 17, cw - 24, 66, Compact ? 21 : 23, FightUI.Foreground);
                title.enableWordWrapping = true; title.enableAutoSizing = true;
                title.fontSizeMin = Compact ? 18 : 20; title.fontSizeMax = Compact ? 21 : 23;
                FightUI.Hint(card.gameObject, d.name);
                string category = DeckAcademy.Category(d);
                string difficulty = string.IsNullOrWhiteSpace(d.difficulty) ? category : category + " · " + d.difficulty;
                FightUI.Text(card.transform, difficulty, 12, coverHeight + 86, cw - 24, 22, 14, FightUI.Muted);
                FightUI.Line(card.transform, 12, coverHeight + 111, cw - 24, selected ? FightUI.GoldBorder : FightUI.Border);
                FightUI.Text(card.transform, (d.steps?.Length ?? 0) + " 步骤  ·  固定起手", 12, coverHeight + 118, cw - 24, 24, 14, FightUI.Muted);
                var divider = card.transform.Find("Divider").GetComponent<UnityEngine.UI.Image>();
                academyPreviewItems.Add(CreateLibraryPreviewItem(card, d, title, divider));
            }
            CompleteLibraryRefresh(scroll, academyScroll);
            HighlightLibraryPreview(academyPreviewItems, academySelection);
            RenderAcademyPreview();
        }
        private void PreviewAcademy(DeckAcademyDeck entry)
        {
            if (page != "academy" || academySelection == entry) return;
            academySelection = entry;
            HighlightLibraryPreview(academyPreviewItems, entry);
            RenderAcademyPreview();
        }
        private void RenderAcademyPreview()
        {
            if (academyPreview == null) return;
            FightUI.Clear(academyPreview); float w = academyPreview.rect.width, h = academyPreview.rect.height;
            if (academySelection == null) { LibraryEmpty(academyPreview, "选择一套教学", "从目录中选择主题，查看起手与学习路线。", w, h, "diamond"); return; }
            var entry = academySelection; var deck = DeckAcademy.Template(entry);
            float contentW = w - 16, artH = (contentW - 10) * .75f;
            var content = FightUI.Scroll(academyPreview, 8, 8, contentW, h - 132, h - 132);
            FightUI.Art(content, DeckLibrary.CoverFor(deck), 5, 5, contentW - 10, artH);
            FightUI.Line(content, 0, artH + 5, contentW, FightUI.GoldBorder);
            LibraryWrappedText(content, entry.name, 12, artH + 20, contentW - 24, 48, Compact ? 26 : 30, FightUI.Active);
            FightUI.Text(content, (entry.steps?.Length ?? 0) + " 个步骤  ·  " + DeckAcademy.Category(entry), 12, artH + 76, contentW - 24, 30, 17, FightUI.Muted);
            float handY = artH + 134;
            ModuleDivider(content, "固定五张起手", 12, handY - 30, contentW - 24, 19);
            var hand = entry.starter_hand ?? new int[0]; float cw = (contentW - 24 - 6 * 4) / 5;
            for (int i = 0; i < Mathf.Min(5, hand.Length); i++) FightUI.Art(content, hand[i], 12 + i * (cw + 6), handY + 12, cw, cw * 1.45f, true);
            content.sizeDelta = new Vector2(contentW, Mathf.Max(h - 132, handY + cw * 1.45f + 32));
            FightUI.Line(academyPreview, 20, h - 124, w - 40, FightUI.GoldBorder);
            FightUI.Button(academyPreview, "开始固定起手练习  →", 20, h - 108, w - 40, 48,
                () => { OpenTutorial(entry); StartCore(() => DeckAcademy.StartPractice(entry)); }, true, DeckAcademy.CanPractice(entry));
            FightUI.TextAction(academyPreview, "观看展开演示  →", 20, h - 52, w - 40, 42,
                () => { OpenTutorial(entry); StartCore(() => DeckAcademy.PlayReplay(entry)); }, DeckAcademy.CanReplay(entry));
        }
        internal void OpenTutorial(DeckAcademyDeck entry)
        {
            if (entry == null) { Navigate("academy"); return; }
            tutorialOrigin = page == "tutorial" ? tutorialOrigin : page; tutorial = entry; tutorialStep = -1; Navigate("tutorial");
        }
        private void RenderTutorial()
        {
            if (tutorial == null) { Navigate("academy"); return; }
            int count = tutorial.steps?.Length ?? 0;
            tutorialStep = Mathf.Clamp(tutorialStep, -1, count);
            PageTitle("学院 / " + tutorial.name);
            FightUI.Button(Body, "返回", BodyWidth - 140, 0, 140, 48, () => Navigate(tutorialOrigin));
            float navW = Compact ? 220 : 272, summaryW = Compact ? 240 : 336;
            float contentW = Mathf.Min(LargeViewport ? 1240 : BodyWidth, BodyWidth - navW - summaryW - Gap * 2), panelH = BodyHeight - 188;
            float workspaceX = (BodyWidth - contentW - navW - summaryW - Gap * 2) / 2;
            var left = FightUI.Panel(Body, workspaceX, 96, navW, panelH, FightUI.PanelColor, false);
            ModuleDivider(left, "展开路线", 16, 16, navW - 32);
            BeginLibraryRefresh(tutorialScroll, tutorial.id);
            var list = FightUI.Scroll(left, 8, 70, navW - 16, panelH - 146, ((tutorial.steps?.Length ?? 0) + 2) * 72, out var routeScroll);
            routeScroll.name = "Tutorial route navigation";
            TutorialStepRow(list, "起手与构筑", "总览", tutorialStep == -1, 4, navW - 24, () => { tutorialStep = -1; Rebuild(); });
            for (int i = 0; i < count; i++)
            {
                int step = i;
                TutorialStepRow(list, tutorial.steps[i].title, (i + 1).ToString("00"), tutorialStep == i, 68 + i * 72, navW - 24, () => { tutorialStep = step; Rebuild(); });
            }
            TutorialStepRow(list, "终场与注意事项", "终场", tutorialStep == count, 68 + count * 72, navW - 24, () => { tutorialStep = count; Rebuild(); });
            float selectedRow = tutorialStep < 0 ? 4 : 68 + tutorialStep * 72;
            CompleteLibraryRefresh(routeScroll, tutorialScroll, tutorialScroll.SelectedStep != tutorialStep ? selectedRow : -1, 64);
            tutorialScroll.SelectedStep = tutorialStep;
            FightUI.Line(left, 12, panelH - 68, navW - 24, FightUI.GoldBorder);
            FightUI.Button(left, "上一步", 12, panelH - 56, (navW - 36) / 2, 44, () => { tutorialStep--; Rebuild(); }, false, tutorialStep >= 0);
            FightUI.Button(left, "下一步", 24 + (navW - 36) / 2, panelH - 56, (navW - 36) / 2, 44, () => { tutorialStep++; Rebuild(); }, false, tutorialStep < count);
            var center = FightUI.Panel(Body, workspaceX + navW + Gap, 96, contentW, panelH);
            var c = FightUI.Scroll(center, 8, 8, contentW - 16, panelH - 16, panelH - 16);
            float proseY;
            if (tutorialStep < 0)
            {
                ModuleDivider(c, "固定五张起手", 16, 14, contentW - 48, 24);
                proseY = DrawTutorialCards(c, tutorial.starter_hand ?? new int[0], 64, contentW - 16) + 12;
                ModuleDivider(c, "构筑思路", 16, proseY, contentW - 48, 24);
                proseY = TutorialProse(c, tutorial.description, proseY + 44, contentW - 48);
            }
            else if (tutorialStep < count)
            {
                var step = tutorial.steps[tutorialStep];
                float cardsY = TutorialHeading(c, "步骤 " + (tutorialStep + 1) + " / " + count + " · " + step.title, contentW - 48);
                proseY = DrawTutorialCards(c, step.card_codes ?? new int[0], cardsY, contentW - 16) + 18;
                TutorialProse(c, step.text, proseY, contentW - 48);
            }
            else
            {
                ModuleDivider(c, "终场与注意事项", 16, 14, contentW - 48, 24);
                proseY = DrawTutorialCards(c, tutorial.end_board?.codes ?? new int[0], 68, contentW - 16) + 18;
                proseY = TutorialProse(c, tutorial.end_board?.text ?? "教学未提供终场描述。", proseY, contentW - 48);
                TutorialProse(c, string.Join("\n\n", tutorial.cautions ?? new string[0]), proseY + 28, contentW - 48);
            }
            var summary = FightUI.Panel(Body, workspaceX + navW + contentW + Gap * 2, 96, summaryW, panelH);
            ModuleDivider(summary, "教学构筑", 18, 16, summaryW - 36, 23);
            var fullDeck = FightUI.TextAction(summary, "查看 →", summaryW - 92, 13, 74, 32, ShowTutorialDeck);
            fullDeck.name = "查看完整构筑"; FightUI.Hint(fullDeck.gameObject, "查看完整教学构筑与真实卡片");
            var sc = FightUI.Scroll(summary, 8, 70, summaryW - 16, panelH - 78, panelH - 78);
            var deck = DeckAcademy.Template(tutorial);
            float summaryArtH = (summaryW - 40) * .75f;
            FightUI.Art(sc, DeckLibrary.CoverFor(deck), 12, 8, summaryW - 40, summaryArtH);
            float sy = summaryArtH + 24;
            var summaryName = LibraryWrappedText(sc, tutorial.name, 14, sy, summaryW - 44, 82, 23, FightUI.Foreground);
            sy += summaryName.rectTransform.rect.height + 12;
            FightUI.Text(sc, "主 " + deck.Main.Count + "  ·  额 " + deck.Extra.Count + "  ·  副 " + deck.Side.Count, 14, sy, summaryW - 44, 30, 16, FightUI.Gold);
            sy += 48;
            if (!string.IsNullOrWhiteSpace(tutorial.regulation))
            {
                FightUI.Text(sc, tutorial.regulation, 14, sy, summaryW - 44, 28, 16, FightUI.Muted);
                sy += 40;
            }
            ModuleDivider(sc, "终场目标", 14, sy, summaryW - 44, 20);
            float endY = TutorialProse(sc, tutorial.end_board?.text ?? "按教学步骤完成展开。", sy + 48, summaryW - 44, 17);
            sc.sizeDelta = new Vector2(summaryW - 16, endY + 20);
            var actions = FightUI.Panel(Body, 0, BodyHeight - 74, BodyWidth, 74, FightUI.Raised);
            FightUI.Line(actions, 0, 0, BodyWidth, FightUI.GoldBorder);
            float bw = Mathf.Min(LargeViewport ? 420 : BodyWidth, (BodyWidth - 48) / 3);
            float actionsX = (BodyWidth - bw * 3 - 24) / 2;
            FightUI.Button(actions, "复制到我的卡组", actionsX, 12, bw, 52, () => Try(() => OpenEditor(DeckAcademy.CreateEditableCopy(tutorial))));
            FightUI.Button(actions, "开始固定起手练习  →", actionsX + 12 + bw, 12, bw, 52, () => StartCore(() => DeckAcademy.StartPractice(tutorial)), true, DeckAcademy.CanPractice(tutorial));
            FightUI.Button(actions, "观看展开演示", actionsX + 24 + bw * 2, 12, bw, 52, () => StartCore(() => DeckAcademy.PlayReplay(tutorial)), false, DeckAcademy.CanReplay(tutorial));
        }
        private float TutorialHeading(RectTransform parent, string title, float width)
        {
            FightUI.Icon(parent, "diamond", 16, 23, 10, 10, FightUI.GoldBorder);
            var heading = FightUI.Text(parent, title, 36, 14, width - 20, 96, 24, FightUI.Foreground);
            heading.enableWordWrapping = true; heading.overflowMode = TextOverflowModes.Overflow;
            float height = Mathf.Max(32, heading.GetPreferredValues(title ?? "", width - 20, float.PositiveInfinity).y + 2);
            heading.rectTransform.sizeDelta = new Vector2(width - 20, height);
            FightUI.Line(parent, 16, 14 + height + 8, width, FightUI.Border);
            return 14 + height + 24;
        }
        private float TutorialProse(RectTransform parent, string text, float y, float width, float size = 20)
        {
            var t = FightUI.Text(parent, text, 16, y, width, 160, size, FightUI.Foreground);
            t.overflowMode = TextOverflowModes.Overflow; t.lineSpacing = 7;
            float height = Mathf.Max(48, t.GetPreferredValues(text ?? "", width, 0).y + 16);
            t.rectTransform.sizeDelta = new Vector2(width, height);
            parent.sizeDelta = new Vector2(parent.sizeDelta.x, Mathf.Max(parent.sizeDelta.y, y + height + 24));
            return y + height;
        }
        private float DrawTutorialCards(RectTransform parent, int[] codes, float y, float width)
        {
            if (codes.Length == 0) return y;
            float cw = Mathf.Min(Compact ? 100 : 144, (width - 48 - 12 * Mathf.Min(4, codes.Length - 1)) / Mathf.Max(1, Mathf.Min(5, codes.Length))), ch = cw * 1.45f;
            var row = FightUI.Scroll(parent, 24, y, width - 48, ch + 64, ch + 64, out var scroll);
            scroll.horizontal = true; scroll.vertical = false; row.sizeDelta = new Vector2(Mathf.Max(width - 48, codes.Length * (cw + 12)), ch + 64);
            for (int i = 0; i < codes.Length; i++)
            {
                var code = codes[i]; var img = FightUI.Art(row, code, i * (cw + 12), 0, cw, ch, true);
                var label = LibraryWrappedText(row, FightUI.BalancedLines(DeckAcademy.CardName(code), Mathf.Max(4, Mathf.FloorToInt(cw / 16))), i * (cw + 12), ch + 8, cw, 52, 16, FightUI.Foreground, TextAlignmentOptions.Center);
                FightUI.Hint(label.gameObject, DeckAcademy.CardName(code));
                FightUI.CardButton(img, () => ShowCard(code));
            }
            return y + ch + 64;
        }
        private void RenderPuzzles()
        {
            if (page == "puzzle") { RenderPuzzleDetail(); return; }
            PageTitle("残局挑战");
            libraryFiles = Directory.Exists(Program.PATH_PUZZLE) ? Directory.GetFiles(Program.PATH_PUZZLE, "*.lua", SearchOption.AllDirectories).Where(p => !p.Replace("\\", "/").Contains("/Academy/")).OrderBy(Path.GetFileName).ToList() : new List<string>();
            FightUI.Input(Body, search, "搜索残局名称", 0, 96, BodyWidth * .58f, 48, v => { search = v; DrawPuzzleFiles(); });
            fileList = FightUI.Node(Body, "Puzzles", 0, 164, BodyWidth * .6f, BodyHeight - 164);
            filePreview = FightUI.Panel(Body, BodyWidth * .6f + Gap, 96, BodyWidth * .4f - Gap, BodyHeight - 96); DrawPuzzleFiles();
        }
        private void DrawPuzzleFiles()
        {
            BeginLibraryRefresh(puzzleScroll, search);
            FightUI.Clear(fileList);
            puzzlePreviewItems.Clear();
            var items = libraryFiles.Where(p => Path.GetFileNameWithoutExtension(p).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            var c = FightUI.Scroll(fileList, 0, 0, fileList.rect.width, fileList.rect.height, items.Count * 92, out var scroll);
            scroll.name = "Puzzle directory navigation";
            if (items.Count == 0) LibraryEmpty(c, libraryFiles.Count == 0 ? "尚未添加残局" : "没有符合条件的残局", libraryFiles.Count == 0 ? "添加本地残局后，即可在这里选择挑战。" : "试试其他名称，或清除搜索条件。", fileList.rect.width, fileList.rect.height, "swords", libraryFiles.Count == 0 ? (Action)null : () => { search = ""; Rebuild(); });
            for (int i = 0; i < items.Count; i++)
            {
                var p = items[i]; bool selected = selectedPuzzle == p;
                var row = LibrarySelection(c, 1, i * 92 + 1, fileList.rect.width - 6, 82, selected, () => { PreviewPuzzle(p); selectedPuzzle = p; Navigate("puzzle"); });
                FightUI.PreviewOnFocus(row, () => PreviewPuzzle(p));
                var icon = FightUI.Icon(row.transform, "swords", 20, 24, 30, 30, selected ? FightUI.Active : FightUI.GoldBorder);
                var title = LibraryWrappedText(row.transform, Path.GetFileNameWithoutExtension(p), 70, 8, fileList.rect.width - 120, 38, 23, selected ? FightUI.Active : FightUI.Foreground);
                FightUI.Text(row.transform, Path.GetFileName(Path.GetDirectoryName(p)), 70, 49, fileList.rect.width - 120, 26, 15, FightUI.Muted);
                var arrow = FightUI.Icon(row.transform, "arrow", fileList.rect.width - 38, 30, 20, 20, selected ? FightUI.Active : FightUI.GoldBorder);
                puzzlePreviewItems.Add(CreateLibraryPreviewItem(row, p, title, null, icon, arrow));
            }
            CompleteLibraryRefresh(scroll, puzzleScroll);
            HighlightLibraryPreview(puzzlePreviewItems, selectedPuzzle);
            RenderPuzzlePreview();
        }
        private void PreviewPuzzle(string path)
        {
            if (page != "puzzles" || selectedPuzzle == path) return;
            selectedPuzzle = path;
            HighlightLibraryPreview(puzzlePreviewItems, path);
            RenderPuzzlePreview();
        }
        private void RenderPuzzlePreview()
        {
            if (filePreview == null || page != "puzzles") return;
            FightUI.Clear(filePreview); float w = filePreview.rect.width, h = filePreview.rect.height, artH = Compact ? 168 : 246;
            FightUI.Asset(filePreview, "puzzle.png", 0, 0, w, artH);
            FightUI.Line(filePreview, 0, artH, w, FightUI.GoldBorder);
            string puzzleName = selectedPuzzle == null ? "寻找逆转的最后一步" : Path.GetFileNameWithoutExtension(selectedPuzzle);
            var previewName = LibraryWrappedText(filePreview, puzzleName, 24, artH + 26, w - 48, 66, 28, FightUI.Active);
            FightUI.Hint(previewName.gameObject, puzzleName);
        }
        private void RenderPuzzleDetail()
        {
            if (selectedPuzzle == null || !File.Exists(selectedPuzzle)) { Navigate("puzzles"); return; }
            PageTitle("准备挑战");
            FightUI.TextAction(Body, "返回残局库  →", BodyWidth - 184, 4, 184, 44, () => Navigate("puzzles"));
            float infoW = Compact ? 332 : 400, previewW = BodyWidth - infoW - Gap, panelH = BodyHeight - 190;
            var visual = FightUI.Panel(Body, 0, 106, previewW, panelH, FightUI.PanelColor, true, "Challenge preview");
            float artH = Mathf.Max(90, panelH - 118);
            FightUI.Asset(visual, "puzzle.png", 1, 1, previewW - 2, artH);
            FightUI.Panel(visual, 1, 1, previewW - 2, artH, new Color(0, .02f, .04f, .45f), false);
            FightUI.Icon(visual, "swords", previewW / 2 - 34, Mathf.Max(28, artH / 2 - 44), 68, 68, FightUI.Active);
            FightUI.Line(visual, 20, artH + 1, previewW - 40, FightUI.GoldBorder);
            FightUI.Text(visual, Path.GetFileNameWithoutExtension(selectedPuzzle), 24, artH + 20, previewW - 48, 40, Compact ? 25 : 30, FightUI.Foreground, TextAlignmentOptions.Center);
            var info = FightUI.Panel(Body, previewW + Gap, 106, infoW, panelH, FightUI.PanelColor, false);
            ModuleDivider(info, "挑战信息", 24, 20, infoW - 48, 22);
            var puzzleTitle = LibraryWrappedText(info, Path.GetFileNameWithoutExtension(selectedPuzzle), 24, 82, infoW - 48, 78, 28, FightUI.Active);
            FightUI.Hint(puzzleTitle.gameObject, Path.GetFileNameWithoutExtension(selectedPuzzle));
            FightUI.Text(info, "资源来源", 24, 174, infoW - 48, 24, 15, FightUI.Muted);
            FightUI.Text(info, Path.GetFileName(Path.GetDirectoryName(selectedPuzzle)), 24, 206, infoW - 48, 30, 19, FightUI.Foreground);
            FightUI.Button(Body, "开始挑战  →", BodyWidth - 272, BodyHeight - 64, 272, 56, () => StartCore(() => { Program.instance.puzzle.StartPuzzle(selectedPuzzle.Substring(0, selectedPuzzle.Length - 4)); if (Program.instance.currentServant == Program.instance.ocgcore) Program.instance.ocgcore.returnServant = Program.instance.menu; }), true);
        }
        private void RenderReplays()
        {
            PageTitle("回放库");
            FightUI.Button(Body, replaySortTime ? "按时间排序" : "按名称排序", BodyWidth - 192, 0, 192, 48, () => { replaySortTime = !replaySortTime; Rebuild(); });
            libraryFiles = Directory.Exists(Program.PATH_REPLAY) ? Directory.GetFiles(Program.PATH_REPLAY, "*", SearchOption.AllDirectories).Where(p => p.EndsWith(".yrp3d") || p.EndsWith(".yrp")).ToList() : new List<string>();
            libraryFiles = replaySortTime ? libraryFiles.OrderByDescending(File.GetLastWriteTime).ToList() : libraryFiles.OrderBy(Path.GetFileName).ToList();
            FightUI.Input(Body, search, "搜索回放名称", 0, 96, BodyWidth * .56f, 48, v => { search = v; DrawReplayFiles(); });
            fileList = FightUI.Node(Body, "Replays", 0, 164, BodyWidth * .57f, BodyHeight - 164);
            filePreview = FightUI.Panel(Body, BodyWidth * .57f + Gap, 96, BodyWidth * .43f - Gap, BodyHeight - 96); DrawReplayFiles();
            if (Program.instance.replay.servantUI == null && !replayLoading) StartCoroutine(LoadReplayUI());
        }
        private IEnumerator LoadReplayUI()
        {
            replayLoading = true; Program.instance.ShiftToServant(Program.instance.replay);
            for (int frame = 0; frame < 600 && Program.instance.replay.servantUI == null; frame++) yield return null;
            replayLoading = false;
            if (page == "replays") DrawReplayFiles();
        }
        private string ReplayName(string path)
        {
            var relative = Path.GetFullPath(path).Substring(Path.GetFullPath(Program.PATH_REPLAY).TrimEnd(Path.DirectorySeparatorChar).Length + 1);
            return relative.EndsWith(".yrp3d") ? relative.Substring(0, relative.Length - 6) : relative;
        }
        private void DrawReplayFiles()
        {
            if (fileList == null || page != "replays") return;
            BeginLibraryRefresh(replayScroll, search + "\t" + replaySortTime);
            FightUI.Clear(fileList); var items = libraryFiles.Where(p => Path.GetFileNameWithoutExtension(p).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            replayPreviewItems.Clear();
            if (selectedReplay != null && !items.Contains(selectedReplay)) selectedReplay = null;
            var c = FightUI.Scroll(fileList, 0, 0, fileList.rect.width, fileList.rect.height, items.Count * 96 + 36, out var scroll);
            scroll.name = "Replay directory navigation";
            if (items.Count == 0) LibraryEmpty(c, libraryFiles.Count == 0 ? "暂无回放" : "没有符合条件的回放", libraryFiles.Count == 0 ? "完成一场决斗后可保存录像。" : "试试其他名称，或清除搜索条件。", fileList.rect.width, fileList.rect.height, "replay", libraryFiles.Count == 0 ? (Action)null : () => { search = ""; Rebuild(); });
            else { FightUI.Text(c, "回放名称", 68, 2, fileList.rect.width - 180, 28, 15, FightUI.Muted); FightUI.Text(c, "格式", fileList.rect.width - 94, 2, 76, 28, 15, FightUI.Muted, TextAlignmentOptions.Center); }
            for (int i = 0; i < items.Count; i++)
            {
                var p = items[i]; var row = LibrarySelection(c, 1, i * 96 + 36, fileList.rect.width - 6, 84, selectedReplay == p, () => { PreviewReplay(p); selectedReplay = p; PlayClientReplay(false); });
                FightUI.PreviewOnFocus(row, () => PreviewReplay(p));
                var icon = FightUI.Icon(row.transform, "replay", 20, 24, 28, 28, selectedReplay == p ? FightUI.Active : FightUI.GoldBorder);
                var title = LibraryWrappedText(row.transform, Path.GetFileNameWithoutExtension(p), 68, 8, fileList.rect.width - 180, 38, 22, selectedReplay == p ? FightUI.Active : FightUI.Foreground);
                FightUI.Text(row.transform, Path.GetExtension(p).TrimStart('.').ToUpperInvariant(), fileList.rect.width - 104, 26, 86, 30, 15, FightUI.Gold, TextAlignmentOptions.Center);
                FightUI.Text(row.transform, File.GetLastWriteTime(p).ToString("yyyy-MM-dd  HH:mm"), 68, 48, fileList.rect.width - 180, 26, 15, FightUI.Muted);
                replayPreviewItems.Add(CreateLibraryPreviewItem(row, p, title, null, icon));
            }
            CompleteLibraryRefresh(scroll, replayScroll);
            HighlightLibraryPreview(replayPreviewItems, selectedReplay);
            RenderReplayPreview();
        }
        private void PreviewReplay(string path)
        {
            if (page != "replays" || selectedReplay == path) return;
            selectedReplay = path;
            HighlightLibraryPreview(replayPreviewItems, path);
            RenderReplayPreview();
        }
        private void ReadClientReplay(string path, out YRP parsed, out bool playable, out bool godAvailable, out string replayError)
        {
            parsed = null; playable = godAvailable = false; replayError = null;
            if (path == null || !File.Exists(path)) { replayError = "录像文件不存在。"; return; }
            try
            {
                var replayUI = Program.instance.replay.GetUI<ReplaySelectorUI>();
                if (replayUI == null) { replayError = "正在读取录像信息…"; return; }
                parsed = replayUI.CacheYRP(ReplayName(path));
                bool readableMetadata = parsed != null && (parsed.ID == 0x31707279 || parsed.ID == 0x32707279)
                    && parsed.playerData != null && parsed.playerData.Count >= 2;
                bool readableOffline = readableMetadata && parsed.gameData.Count > 0;
                bool recordedEvents = path.EndsWith(Program.EXPANSION_YRP3D, StringComparison.OrdinalIgnoreCase)
                    && TcpHelper.ReadPackagesInRecord(path).Any(package => package.Function != (int)GameMessage.sibyl_replay
                        && Enum.IsDefined(typeof(GameMessage), package.Function));
                playable = recordedEvents || readableOffline;
                godAvailable = readableOffline;
                if (!playable) replayError = "此录像没有可读取的决斗记录，可能未录制完整。";
                if (!readableMetadata || !playable) parsed = null;
            }
            catch (Exception e) { parsed = null; playable = godAvailable = false; replayError = "录像文件读取失败，请选择其他录像。"; Debug.LogWarning("[FightClient] replay: " + e.Message); }
        }
        private void RenderReplayPreview()
        {
            if (filePreview == null || page != "replays") return;
            FightUI.Clear(filePreview); float w = filePreview.rect.width, h = filePreview.rect.height;
            if (selectedReplay == null || !File.Exists(selectedReplay)) { LibraryEmpty(filePreview, "选择一段录像", "查看录像信息与双方构筑，复盘每一步决策。", w, h, "replay"); return; }
            LibraryWrappedText(filePreview, Path.GetFileNameWithoutExtension(selectedReplay), 24, 22, w - 48, 64, 28, FightUI.Active);
            FightUI.Text(filePreview, File.GetLastWriteTime(selectedReplay).ToString("yyyy-MM-dd HH:mm") + " · 文件修改时间", 24, 96, w - 48, 28, 16, FightUI.Muted);
            ReadClientReplay(selectedReplay, out var parsed, out _, out bool godAvailable, out string replayError);
            ModuleDivider(filePreview, "双方构筑", 24, 140, w - 48, 20);
            var meta = FightUI.Scroll(filePreview, 16, 190, w - 32, Mathf.Max(60, h - 342), 260);
            if (parsed?.playerData != null)
            {
                for (int i = 0; i < parsed.playerData.Count; i++)
                {
                    var player = parsed.playerData[i]; var playerDeck = new Deck(player.main, player.extra, new List<int>());
                    var row = LibrarySelection(meta, 8, i * 78, w - 48, 70, false, () => ShowReplayDeck(player));
                    FightUI.Text(row.transform, (i + 1).ToString("00"), 10, 20, 28, 28, 16, FightUI.GoldBorder, TextAlignmentOptions.Center);
                    LibraryWrappedText(row.transform, string.IsNullOrEmpty(player.name) ? "玩家 " + (i + 1) + " · 名称未提供" : player.name, 48, 5, w - 124, 32, 18, FightUI.Foreground);
                    FightUI.Text(row.transform, "主 " + playerDeck.Main.Count + "  ·  额 " + playerDeck.Extra.Count, 48, 40, w - 124, 26, 14, FightUI.Muted);
                    FightUI.Icon(row.transform, "arrow", w - 78, 24, 18, 18, FightUI.GoldBorder);
                }
            }
            else FightUI.Text(meta, replayError ?? (replayLoading ? "正在读取录像信息…" : "此录像未提供双方构筑信息。"), 8, 8, w - 48, 80, 18, FightUI.Muted);
            if (parsed?.playerData != null) meta.sizeDelta = new Vector2(w - 32, Mathf.Max(meta.rect.height, parsed.playerData.Count * 78));
            FightUI.Button(filePreview, "重命名", 24, h - 132, (w - 60) / 2, 40, () => Prompt("重命名回放", "新名称", Path.GetFileNameWithoutExtension(selectedReplay), name => { CheckName(name); var target = Path.Combine(Path.GetDirectoryName(selectedReplay), name + Path.GetExtension(selectedReplay)); if (File.Exists(target)) throw new IOException("该名称已存在。"); File.Move(selectedReplay, target); selectedReplay = target; Rebuild(); }));
            FightUI.Button(filePreview, "删除…", (w + 12) / 2, h - 132, (w - 60) / 2, 40, () => Confirm("删除回放", "删除此录像文件？", () => { File.Delete(selectedReplay); selectedReplay = null; Rebuild(); }));
            var godButton = FightUI.Button(filePreview, "上帝视角", 24, h - 72, w - 48, 44, () => PlayClientReplay(true), false, godAvailable);
            FightUI.Hint(godButton.gameObject, godAvailable ? "从可读取的离线录像还原双方视角" : "仅可读取的离线回放支持上帝视角");
        }
        private void PlayClientReplay(bool god)
        {
            string path = selectedReplay;
            ReadClientReplay(path, out _, out bool playable, out bool godAvailable, out string error);
            if (!playable || god && !godAvailable) { ShowStatus(error ?? "此录像不支持上帝视角。"); return; }
            StartCore(() => { Program.instance.replay.GetUI<ReplaySelectorUI>().KF_Replay(ReplayName(path), god); if (Program.instance.currentServant == Program.instance.ocgcore) Program.instance.ocgcore.returnServant = Program.instance.menu; });
        }
        private static LibraryPreviewItem CreateLibraryPreviewItem(UnityEngine.UI.Button button, object value,
            TextMeshProUGUI heading, UnityEngine.UI.Image divider = null, params FightIcon[] icons)
        {
            var rect = button.GetComponent<RectTransform>();
            var accent = button.transform.Find("Selection accent") as RectTransform
                ?? FightUI.Panel(button.transform, 1, 9, 2, rect.rect.height - 18, FightUI.Active, false, "Selection accent");
            accent.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            return new LibraryPreviewItem { Value = value, Button = button, Heading = heading, Divider = divider, Icons = icons, Accent = accent };
        }
        private static void HighlightLibraryPreview(List<LibraryPreviewItem> rows, object value)
        {
            foreach (var row in rows)
            {
                if (row.Button == null) continue;
                bool selected = Equals(row.Value, value);
                row.Button.targetGraphic.color = selected ? FightUI.Raised : FightUI.PanelColor;
                var outline = row.Button.GetComponent<UnityEngine.UI.Outline>();
                if (outline != null) outline.effectColor = selected ? FightUI.GoldBorder : FightUI.Border;
                if (row.Heading != null) row.Heading.color = selected ? FightUI.Active : FightUI.Foreground;
                if (row.Divider != null) row.Divider.color = selected ? FightUI.GoldBorder : FightUI.Border;
                if (row.Accent != null) row.Accent.gameObject.SetActive(selected);
                foreach (var icon in row.Icons)
                    if (icon != null) icon.color = selected ? FightUI.Active : FightUI.GoldBorder;
            }
        }
        private static void BeginLibraryRefresh(LibraryScrollMemory memory, string condition)
        {
            if (memory.Condition == condition) return;
            memory.Condition = condition; memory.Position = 1; memory.SelectedStep = int.MinValue;
        }
        private static void CompleteLibraryRefresh(UnityEngine.UI.ScrollRect scroll, LibraryScrollMemory memory, float revealY = -1, float revealHeight = 0)
        {
            // Keep these named directories out of the generic page scroll
            // restoration: a filter resets them, while a selection retains them.
            float position = memory.Position;
            Canvas.ForceUpdateCanvases();
            float range = Mathf.Max(0, scroll.content.rect.height - scroll.viewport.rect.height);
            if (range <= 1) position = 1;
            else if (revealY >= 0)
            {
                float offset = (1 - position) * range;
                if (revealY < offset + 6) offset = Mathf.Max(0, revealY - 6);
                else if (revealY + revealHeight > offset + scroll.viewport.rect.height - 6)
                    offset = revealY + revealHeight - scroll.viewport.rect.height + 6;
                position = 1 - Mathf.Clamp(offset, 0, range) / range;
            }
            scroll.verticalNormalizedPosition = position;
            memory.Position = position;
            scroll.onValueChanged.AddListener(value =>
            {
                if (scroll.content == null || scroll.viewport == null) return;
                memory.Position = scroll.content.rect.height <= scroll.viewport.rect.height + 1 ? 1 : Mathf.Clamp01(value.y);
            });
        }
        private void LibraryTab(Transform parent, string label, bool selected, float x, float y, float width, Action action)
        {
            var b = LibrarySelection(parent, x, y, width, 44, selected, action, false);
            FightUI.Text(b.transform, label, 6, 0, width - 12, 42, 17, selected ? FightUI.Active : FightUI.Muted, TextAlignmentOptions.Center);
            if (selected) FightUI.Line(b.transform, 10, 43, width - 20, FightUI.Active);
        }
        private UnityEngine.UI.Button LibrarySelection(Transform parent, float x, float y, float width, float height, bool selected, Action action, bool frame = true)
        {
            var rt = FightUI.Panel(parent, x, y, width, height, selected ? FightUI.Raised : FightUI.PanelColor, frame, "Library selection");
            if (frame) rt.GetComponent<UnityEngine.UI.Outline>().effectColor = selected ? FightUI.GoldBorder : FightUI.Border;
            var b = rt.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic = rt.GetComponent<UnityEngine.UI.Image>();
            b.onClick.AddListener(() => action?.Invoke());
            var colors = b.colors; colors.highlightedColor = new Color(1.3f, 1.35f, 1.4f); colors.selectedColor = new Color(1.2f, 1.25f, 1.3f); colors.fadeDuration = .16f; b.colors = colors;
            if (selected && frame) FightUI.Panel(rt, 1, 9, 2, height - 18, FightUI.Active, false, "Selection accent");
            var border = rt.GetComponentInChildren<FightFrame>();
            if (border != null) rt.gameObject.AddComponent<FightButtonFeedback>().Bind(border);
            return b;
        }
        private void LibraryEmpty(Transform parent, string title, string detail, float width, float height, string icon, Action clear = null)
        {
            if (height < 140)
            {
                float titleHeight = height < 100 ? 24 : 28;
                FightUI.Text(parent, title, 12, 6, width - 24, titleHeight, height < 100 ? 17 : 18, FightUI.Foreground, TextAlignmentOptions.Center);
                float compactDetailY = 6 + titleHeight + 4;
                if (clear != null)
                {
                    float actionHeight = Mathf.Min(40, Mathf.Max(28, height - titleHeight - 18));
                    float compactActionY = Mathf.Max(6 + titleHeight + 6, height - actionHeight - 6);
                    if (compactActionY - compactDetailY >= 20)
                        FightUI.Text(parent, detail, 12, compactDetailY, width - 24, compactActionY - compactDetailY - 2, 14, FightUI.Muted, TextAlignmentOptions.Center);
                    float actionWidth = Mathf.Min(168, width - 24);
                    FightUI.TextAction(parent, "清除搜索  →", (width - actionWidth) / 2, compactActionY, actionWidth, actionHeight, clear);
                }
                else if (height - compactDetailY >= 20)
                    FightUI.Text(parent, detail, 12, compactDetailY, width - 24, height - compactDetailY - 6, 14, FightUI.Muted, TextAlignmentOptions.Center);
                return;
            }
            bool shortPanel = height < 240;
            float cy = shortPanel ? 18 : Mathf.Min(152, Mathf.Max(58, height * .28f)), iconSize = shortPanel ? 28 : 48;
            FightUI.Icon(parent, icon, (width - iconSize) / 2, shortPanel ? 6 : cy - 28, iconSize, iconSize, FightUI.GoldBorder);
            FightUI.Text(parent, title, 24, shortPanel ? 42 : cy + 46, width - 48, shortPanel ? 32 : 44, shortPanel ? 21 : 25, FightUI.Foreground, TextAlignmentOptions.Center);
            float detailY = shortPanel ? 82 : cy + 106;
            float actionY = Mathf.Min(height - 44, shortPanel ? 130 : cy + 204);
            float detailH = clear == null ? (shortPanel ? Mathf.Max(28, height - 90) : 80) : Mathf.Clamp(actionY - detailY - 8, 24, 80);
            FightUI.Text(parent, detail, 32, detailY, width - 64, detailH, height < 176 ? 15 : 16, FightUI.Muted, TextAlignmentOptions.Center);
            if (clear != null && height >= 140) FightUI.TextAction(parent, "清除搜索  →", (width - 168) / 2, actionY, 168, 40, clear);
        }
        private void TutorialStepRow(Transform parent, string label, string number, bool selected, float y, float width, Action action)
        {
            var b = LibrarySelection(parent, 4, y, width, 64, selected, action, false);
            if (selected) FightUI.Panel(b.transform, 0, 6, 2, 52, FightUI.Active, false);
            FightUI.Text(b.transform, number, 8, 17, 36, 30, number.Length > 2 ? 13 : 16, selected ? FightUI.Active : FightUI.GoldBorder, TextAlignmentOptions.Center);
            var text = FightUI.Text(b.transform, label, 52, 10, width - 62, 48, 17, selected ? FightUI.Active : FightUI.Foreground, TextAlignmentOptions.MidlineLeft);
            text.enableWordWrapping = true;
            FightUI.Hint(text.gameObject, label);
        }
        private TextMeshProUGUI LibraryWrappedText(Transform parent, string value, float x, float y, float width, float maxHeight, float size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
        {
            var text = FightUI.Text(parent, value, x, y, width, maxHeight, size, color, alignment);
            text.enableWordWrapping = true; text.lineSpacing = 2;
            float height = Mathf.Clamp(text.GetPreferredValues(value ?? "", width, float.PositiveInfinity).y + 2, size + 6, maxHeight);
            text.rectTransform.sizeDelta = new Vector2(width, alignment == TextAlignmentOptions.Center ? maxHeight : height);
            return text;
        }
        private void ShowTutorialDeck()
        {
            if (tutorial == null) return;
            var entry = tutorial; var deck = DeckAcademy.Template(entry);
            float width = Mathf.Min(W - 64, 1200);
            var dialog = ShowDialog(entry.name + " · 教学构筑", width, H - 80);
            var cards = FightUI.Scroll(dialog, 24, 88, width - 48, H - 252, 1000);
            DrawDeckCards(cards, deck, width - 48, null, true);
            FightUI.Button(dialog, "复制到我的卡组", width - 280, H - 154, 252, 48, () => Try(() => OpenEditor(DeckAcademy.CreateEditableCopy(entry))), true);
        }
        private void ShowReplayDeck(YRP.PlayerData player)
        {
            var deck = new Deck(player.main, player.extra, new List<int>()); float width = Mathf.Min(W - 64, 1200);
            var p = ShowDialog((player.name ?? "玩家") + " · 录像构筑（只读）", width, H - 80);
            var c = FightUI.Scroll(p, 24, 88, width - 48, H - 248, 1000); DrawDeckCards(c, deck, width - 48);
            FightUI.Button(p, "复制到我的卡组", width - 280, H - 154, 252, 48, () => Try(() => OpenEditor(DeckLibrary.CreateEditableCopy(System.Text.Encoding.UTF8.GetBytes(deck.GetYDK()), player.name ?? "回放构筑"))), true);
        }
    }
}
