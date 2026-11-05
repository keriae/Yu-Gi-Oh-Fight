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
using UnityEngine.EventSystems;
using YRP = Percy.YRP;

namespace MDPro3.UI.Client
{
    internal sealed class FightSelectionCorners : UnityEngine.UI.MaskableGraphic
    {
        protected override void Awake() { base.Awake(); raycastTarget = false; }
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vertices)
        {
            vertices.Clear();
            var rect = rectTransform.rect;
            float inset = 5, length = 6;
            float right = rect.xMax - inset, top = rect.yMax - inset;
            float left = rect.xMin + inset, bottom = rect.yMin + inset;
            FightFrame.Segment(vertices, new Vector2(right - length, top), new Vector2(right, top), 1, color);
            FightFrame.Segment(vertices, new Vector2(right, top), new Vector2(right, top - length), 1, color);
            FightFrame.Segment(vertices, new Vector2(left, bottom + length), new Vector2(left, bottom), 1, color);
            FightFrame.Segment(vertices, new Vector2(left, bottom), new Vector2(left + length, bottom), 1, color);
        }
    }

    internal partial class FightClient
    {
        private string academyCategory = "全部", selectedPuzzle, selectedReplay;
        private RectTransform academyGrid, academyPreview, fileList, filePreview;
        private DeckAcademyDeck academySelection;
        private TMP_InputField academyDirectorySearch;
        private bool replayLoading, replaySortTime = true;
        private string replayLoadError;
        private List<string> libraryFiles;
        private sealed class LibraryPreviewItem
        {
            internal object Value;
            internal UnityEngine.UI.Button Button;
            internal TextMeshProUGUI Heading;
            internal FightIcon[] Icons;
            internal UnityEngine.UI.Image Divider;
            internal RectTransform SelectionCorner;
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
            replayScroll = new LibraryScrollMemory(), tutorialScroll = new LibraryScrollMemory(),
            academyPreviewScroll = new LibraryScrollMemory(), replayPreviewScroll = new LibraryScrollMemory();
        private readonly Dictionary<DeckAcademyDeck, Dictionary<int, LibraryScrollMemory>> tutorialReadingPositions
            = new Dictionary<DeckAcademyDeck, Dictionary<int, LibraryScrollMemory>>();
        private readonly Dictionary<string, LibraryScrollMemory> puzzleReadingPositions
            = new Dictionary<string, LibraryScrollMemory>(StringComparer.Ordinal);

        private static string AcademyLessonCardNames(DeckAcademyDeck entry)
        {
            var codes = (entry.starter_hand ?? Array.Empty<int>()).Concat(entry.starter_cards ?? Array.Empty<int>())
                .Concat((entry.steps ?? Array.Empty<DeckAcademyStep>()).Where(step => step != null)
                    .SelectMany(step => step.card_codes ?? Array.Empty<int>()))
                .Concat(entry.end_board?.codes ?? Array.Empty<int>()).Distinct();
            return DeckAcademy.CardNames(codes);
        }
        private TMP_InputField AcademyDirectorySearchField(string placeholder, float width, float height, Action refresh, float y = 96)
        {
            UnityEngine.UI.Button clear = null;
            var input = FightUI.Input(Body, search, placeholder, 0, y, width, height, value =>
            {
                search = value;
                if (clear != null) clear.gameObject.SetActive(!string.IsNullOrEmpty(value));
                refresh();
            });
            FightUI.FocusKey(input, page + ":search");
            FightUI.Icon(input.transform, "search", 12, (height - 18) / 2, 18, 18, FightUI.Muted);
            FightUI.Place(input.textViewport, 40, 5, width - 82, height - 10);
            foreach (var text in input.textViewport.GetComponentsInChildren<TextMeshProUGUI>())
                text.rectTransform.sizeDelta = new Vector2(width - 82, height - 10);
            clear = FightUI.FocusKey(FightUI.Button(input.transform, "", width - 36, (height - 28) / 2, 28, 28,
                () => ClearAcademyDirectorySearch()), page + ":search:clear");
            clear.name = "Clear " + page + " search";
            FightUI.Icon(clear.transform, "close", 7, 7, 14, 14, FightUI.Muted);
            FightUI.Hint(clear.gameObject, "清除搜索，继续浏览目录");
            clear.gameObject.SetActive(!string.IsNullOrEmpty(search));
            FightUI.Hint(input.gameObject, "忽略首尾空格；清除后可继续输入。");
            academyDirectorySearch = input;
            return input;
        }
        private void ClearAcademyDirectorySearch(bool clearCategory = false)
        {
            if (academyDirectorySearch == null) return;
            if (clearCategory && academyCategory != "全部")
            {
                academyCategory = "全部"; search = ""; Rebuild();
            }
            else academyDirectorySearch.text = "";
            academyDirectorySearch.Select(); academyDirectorySearch.ActivateInputField(); academyDirectorySearch.caretPosition = 0;
        }
        private void ReturnAcademyDirectory(string destination, object entry)
        {
            Navigate(destination);
            if (page == destination && (destination == "academy" || destination == "puzzles"))
                StartCoroutine(RestoreAcademyDirectoryFocus(destination, entry));
        }
        private IEnumerator RestoreAcademyDirectoryFocus(string destination, object entry)
        {
            yield return null;
            if (page != destination || modal != null || EventSystem.current == null || !clientCanvas.enabled) yield break;
            // A click or key selection made during the return frame owns focus.
            var current = EventSystem.current.currentSelectedGameObject;
            if (current != null && current.activeInHierarchy && current.transform.IsChildOf(root)
                && current.GetComponent<UnityEngine.UI.Selectable>() is UnityEngine.UI.Selectable active && active.IsInteractable()) yield break;
            var rows = destination == "academy" ? academyPreviewItems : puzzlePreviewItems;
            var original = rows.FirstOrDefault(item => Equals(item.Value, entry));
            if (original == null || original.Button == null || !original.Button.IsInteractable()) yield break;
            original.Button.Select();
            RevealClientControl(original.Button);
        }

        private void RenderAcademy()
        {
            FightUI.TextAction(Body, "残局挑战  →", BodyWidth - 184, 4, 184, 44, () => Navigate("puzzles"));
            float previewW = Compact ? 300 : 368, gridW = BodyWidth - previewW - Gap;
            float searchW = Mathf.Min(380, gridW * .34f);
            AcademyDirectorySearchField("搜索主题或卡名", searchW, 44, RenderAcademyGrid, 0);
            var cats = new[] { "全部" }.Concat(academy.decks.Select(DeckAcademy.Category).Distinct()).ToList();
            float x = searchW + Gap, width = (gridW - x) / cats.Count;
            for (int i = 0; i < cats.Count; i++)
            {
                var cat = cats[i];
                LibraryTab(Body, cat, cat == academyCategory, x + i * width, 0, width - 4,
                    () => { academyCategory = cat; Rebuild(); });
            }
            academyGrid = FightUI.Node(Body, "Academy Grid", 0, 64, gridW, BodyHeight - 64);
            academyPreview = FightUI.Panel(Body, gridW + Gap, 56, previewW, BodyHeight - 56, FightUI.PanelColor, false, "Learning preview");
            RenderAcademyGrid();
        }
        private void RenderAcademyGrid()
        {
            if (academyGrid == null) return;
            string query = search.Trim();
            BeginLibraryRefresh(academyScroll, query + "\t" + academyCategory);
            FightUI.Clear(academyGrid);
            academyPreviewItems.Clear();
            var items = academy.decks.Where(d => DeckAcademy.Matches(d, query, academyCategory == "全部" ? "" : academyCategory, query.Length == 0 ? null : AcademyLessonCardNames(d) + " " + FightText.Get(d.name + " " + d.description + " " + string.Join(" ", d.tags ?? Array.Empty<string>())))).ToList();
            if (academySelection == null || !items.Contains(academySelection)) academySelection = items.FirstOrDefault();
            int cols = Compact ? 3 : LargeViewport ? 6 : 4;
            const float cardGap = 16;
            float gridW = academyGrid.rect.width, cw = (gridW - cardGap * (cols - 1)) / cols;
            float coverHeight = cw * .75f, ch = coverHeight + 148;
            var c = FightUI.Scroll(academyGrid, 0, 0, gridW, academyGrid.rect.height, Mathf.Ceil((float)items.Count / cols) * (ch + cardGap), out var scroll);
            scroll.name = "Academy directory navigation";
            if (items.Count == 0)
            {
                LibraryEmpty(c, academy.decks.Count == 0 ? "尚未添加教学" : "没有符合条件的教程",
                    "",
                    gridW, academyGrid.rect.height, "diamond", academy.decks.Count == 0 ? (Action)null
                        : () => ClearAcademyDirectorySearch(true), "清除筛选  →");
            }
            for (int i = 0; i < items.Count; i++)
            {
                var d = items[i]; bool selected = d == academySelection;
                var card = LibrarySelection(c, i % cols * (cw + cardGap), i / cols * (ch + cardGap), cw, ch, selected,
                    () => { PreviewAcademy(d); OpenTutorial(d); });
                FightUI.FocusKey(card, "academy:lesson:" + d.id);
                FightUI.PreviewOnFocus(card, () => PreviewAcademy(d));
                var deck = DeckAcademy.Template(d);
                FightUI.Art(card.transform, DeckLibrary.CoverFor(deck), 5, 5, cw - 10, coverHeight);
                LibraryTag(card.transform, (d.steps?.Length ?? 0) + " 步骤", 12, 12, Mathf.Min(94, cw - 24));
                var title = FightUI.Text(card.transform, d.name, 12, coverHeight + 17, cw - 24, 66, Compact ? 21 : 23, FightUI.Foreground);
                title.enableWordWrapping = true; title.enableAutoSizing = true;
                title.fontSizeMin = Compact ? 18 : 20; title.fontSizeMax = Compact ? 21 : 23;
                FightUI.Hint(card.gameObject, d.name + " · 点击查看完整教程");
                string category = DeckAcademy.Category(d);
                string difficulty = string.IsNullOrWhiteSpace(d.difficulty) ? category : category + " · " + d.difficulty;
                FightUI.Text(card.transform, difficulty, 12, coverHeight + 86, cw - 24, 22, 14, FightUI.Muted);
                FightUI.Line(card.transform, 12, coverHeight + 111, cw - 24, selected ? FightUI.GoldBorder : FightUI.Border);
                FightUI.Text(card.transform, (d.starter_hand?.Length ?? 0) + " 张固定起手", 12, coverHeight + 118, cw - 136, 24, 14, FightUI.Muted);
                FightUI.Text(card.transform, "查看教程  →", cw - 120, coverHeight + 118, 108, 24, 14, FightUI.Gold, TextAlignmentOptions.Right);
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
            if (academySelection == null) { LibraryEmpty(academyPreview, "暂无教学预览", "", w, h, "diamond"); return; }
            var entry = academySelection; var deck = DeckAcademy.Template(entry);
            float contentW = w - 16, artH = Compact ? Mathf.Min(88, (contentW - 10) * .75f) : (contentW - 10) * .75f;
            BeginLibraryRefresh(academyPreviewScroll, entry.id ?? entry.name);
            var content = FightUI.Scroll(academyPreview, 8, 8, contentW, h - 132, h - 132, out var readingScroll);
            readingScroll.name = "Academy preview reading";
            FightUI.Art(content, DeckLibrary.CoverFor(deck), 5, 5, contentW - 10, artH);
            FightUI.Line(content, 0, artH + 5, contentW, FightUI.GoldBorder);
            float titleY = artH + (Compact ? 14 : 20);
            var title = LibraryWrappedText(content, entry.name, 12, titleY, contentW - 24, Compact ? 80 : 100, Compact ? 24 : 30, FightUI.Active);
            float metaY = titleY + title.rectTransform.sizeDelta.y + (Compact ? 10 : 16);
            string metadata = (entry.steps?.Length ?? 0) + " 个步骤  ·  " + DeckAcademy.Category(entry);
            if (!string.IsNullOrWhiteSpace(entry.difficulty)) metadata += "  ·  " + entry.difficulty;
            var meta = LibraryWrappedText(content, metadata, 12, metaY, contentW - 24, 80, Compact ? 15 : 17, FightUI.Muted);
            float goalY = metaY + meta.rectTransform.sizeDelta.y + (Compact ? 16 : 24);
            ModuleDivider(content, "学习目标", 12, goalY, contentW - 24, 18);
            string goal = !string.IsNullOrWhiteSpace(entry.end_board?.text) ? entry.end_board.text
                : !string.IsNullOrWhiteSpace(entry.description) ? entry.description : "按展开路线完成固定起手练习。";
            var goalText = LibraryWrappedText(content, goal, 12, goalY + 38, contentW - 24, float.PositiveInfinity, Compact ? 16 : 18, FightUI.Foreground);
            goalText.name = "Academy learning goal";
            goalText.overflowMode = TextOverflowModes.Overflow;
            float handY = goalY + 38 + goalText.rectTransform.sizeDelta.y + (Compact ? 54 : 64);
            var hand = entry.starter_hand ?? new int[0]; float cw = (contentW - 24 - 6 * 4) / 5;
            float viewportHeight = h - 132, handTop = handY - 30, handBottom = handY + 12 + cw * 1.45f + 24;
            if (hand.Length > 0 && handTop < viewportHeight && handBottom > viewportHeight)
            {
                // Keep the fixed-hand heading and complete card faces together.
                // A thin strip of cropped cards is not a useful first-screen preview.
                handY = viewportHeight + 48;
            }
            ModuleDivider(content, hand.Length == 5 ? "固定五张起手" : hand.Length + " 张固定起手", 12, handY - 30, contentW - 24, 19);
            for (int i = 0; i < Mathf.Min(5, hand.Length); i++)
            {
                int code = hand[i];
                var image = FightUI.Art(content, code, 12 + i * (cw + 6), handY + 12, cw, cw * 1.45f, true);
                FightUI.CardButton(image, () => ShowCard(code));
                FightUI.Hint(image.gameObject, DeckAcademy.CardName(code));
            }
            content.sizeDelta = new Vector2(contentW, Mathf.Max(h - 132, handY + cw * 1.45f + 36));
            CompleteLibraryRefresh(readingScroll, academyPreviewScroll);
            FightUI.Line(academyPreview, 20, h - 124, w - 40, FightUI.GoldBorder);
            var practice = FightUI.Button(academyPreview, "开始固定起手练习  →", 20, h - 108, w - 40, 48,
                () => { OpenTutorial(entry); StartCore(() => DeckAcademy.StartPractice(entry)); }, true, DeckAcademy.CanPractice(entry));
            float secondaryW = (w - 48) / 2;
            FightUI.TextAction(academyPreview, "完整教程  →", 20, h - 52, secondaryW, 42, () => OpenTutorial(entry));
            var replay = FightUI.TextAction(academyPreview, "展开演示  →", 28 + secondaryW, h - 52, secondaryW, 42,
                () => { OpenTutorial(entry); StartCore(() => DeckAcademy.PlayReplay(entry)); }, DeckAcademy.CanReplay(entry));
            replay.name = "观看展开演示  →";
            ExplainUnavailableLessonAction(practice, "练习资源暂不可用", "此教学的固定起手练习资源暂不可用。");
            ExplainUnavailableLessonAction(replay, "演示暂不可用", "此教学的展开演示资源暂不可用。");
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
            PageTitle(tutorial.name);
            FightUI.Button(Body, "返回", BodyWidth - 140, 0, 140, 48, () => ReturnAcademyDirectory(tutorialOrigin, tutorial));
            float navW = Compact ? 220 : 272, summaryW = Compact ? 240 : 336;
            float contentW = Mathf.Min(LargeViewport ? 1240 : BodyWidth, BodyWidth - navW - summaryW - Gap * 2), panelH = BodyHeight - 156;
            float workspaceX = (BodyWidth - contentW - navW - summaryW - Gap * 2) / 2;
            var left = FightUI.Panel(Body, workspaceX, 64, navW, panelH, FightUI.PanelColor, false);
            ModuleDivider(left, "展开路线", 16, 16, navW - 32);
            float segmentW = (navW - 32 - (count + 1) * 4) / (count + 2);
            for (int i = 0; i < count + 2; i++)
            {
                var segment = FightUI.Panel(left, 16 + i * (segmentW + 4), 60, segmentW, 2, i == tutorialStep + 1 ? FightUI.Active : FightUI.Border, false, "Reading position");
                segment.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            }
            BeginLibraryRefresh(tutorialScroll, tutorial.id);
            var list = FightUI.Scroll(left, 8, 70, navW - 16, panelH - 146, ((tutorial.steps?.Length ?? 0) + 2) * 72, out var routeScroll);
            routeScroll.name = "Tutorial route navigation";
            var routeRows = new List<RectTransform>();
            routeRows.Add(TutorialStepRow(list, "起手与构筑", "总览", tutorialStep == -1, 0, navW - 24, () => { tutorialStep = -1; Rebuild(); }));
            for (int i = 0; i < count; i++)
            {
                int step = i;
                routeRows.Add(TutorialStepRow(list, tutorial.steps[i].title, (i + 1).ToString("00"), tutorialStep == i, 0, navW - 24, () => { tutorialStep = step; Rebuild(); }));
            }
            routeRows.Add(TutorialStepRow(list, "终场与注意事项", "终场", tutorialStep == count, 0, navW - 24, () => { tutorialStep = count; Rebuild(); }));
            // Measure every title first, then place the route using the final
            // row geometry. Selection reveal must use those same dimensions.
            float routeY = 4, selectedRow = 4, selectedHeight = 64;
            for (int i = 0; i < routeRows.Count; i++)
            {
                var row = routeRows[i];
                row.anchoredPosition = new Vector2(4, -routeY);
                if (i == tutorialStep + 1) { selectedRow = routeY; selectedHeight = row.sizeDelta.y; }
                routeY += row.sizeDelta.y + 8;
            }
            list.sizeDelta = new Vector2(navW - 16, Mathf.Max(panelH - 146, routeY - 4));
            CompleteLibraryRefresh(routeScroll, tutorialScroll, tutorialScroll.SelectedStep != tutorialStep ? selectedRow : -1, selectedHeight);
            tutorialScroll.SelectedStep = tutorialStep;
            FightUI.Line(left, 12, panelH - 68, navW - 24, FightUI.GoldBorder);
            FightUI.Button(left, "上一步", 12, panelH - 56, (navW - 36) / 2, 44, () => { tutorialStep--; Rebuild(); }, false, tutorialStep >= 0);
            FightUI.Button(left, "下一步", 24 + (navW - 36) / 2, panelH - 56, (navW - 36) / 2, 44, () => { tutorialStep++; Rebuild(); }, false, tutorialStep < count);
            var center = FightUI.Panel(Body, workspaceX + navW + Gap, 64, contentW, panelH);
            var c = FightUI.Scroll(center, 8, 8, contentW - 16, panelH - 16, panelH - 16, out var chapterScroll);
            chapterScroll.name = "Tutorial chapter reading";
            float proseY;
            if (tutorialStep < 0)
            {
                int handCount = tutorial.starter_hand?.Length ?? 0;
                ModuleDivider(c, handCount == 5 ? "固定五张起手" : handCount + " 张固定起手", 16, 14, contentW - 48, 24);
                proseY = DrawTutorialCards(c, tutorial.starter_hand ?? new int[0], 64, contentW - 16) + 12;
                ModuleDivider(c, "构筑思路", 16, proseY, contentW - 48, 24);
                proseY = TutorialProse(c, string.IsNullOrWhiteSpace(tutorial.description)
                    ? "此教学暂未提供构筑说明，可从左侧展开路线查看操作步骤。" : tutorial.description, proseY + 44, contentW - 48);
            }
            else if (tutorialStep < count)
            {
                var step = tutorial.steps[tutorialStep];
                float cardsY = TutorialHeading(c, step.title, contentW - 48, tutorialStep + 1, count);
                var stageCards = FightUI.Text(c, "涉及 " + (step.card_codes?.Length ?? 0) + " 张卡片", 24, cardsY, contentW - 64, 32, 14, FightUI.Muted);
                stageCards.name = "Tutorial stage cards";
                cardsY += 36;
                proseY = DrawTutorialCards(c, step.card_codes ?? new int[0], cardsY, contentW - 16) + 18;
                TutorialProse(c, string.IsNullOrWhiteSpace(step.text)
                    ? "此步骤暂未提供文字说明，可查看涉及卡片并继续阅读展开路线。" : step.text, proseY, contentW - 48);
            }
            else
            {
                ModuleDivider(c, "终场目标", 16, 14, contentW - 48, 24);
                proseY = TutorialProse(c, string.IsNullOrWhiteSpace(tutorial.end_board?.text)
                    ? "完成展开路线后，可结合终场卡片检查场面。" : tutorial.end_board.text, 58, contentW - 48);
                var cautions = tutorial.cautions?.Where(text => !string.IsNullOrWhiteSpace(text)).ToArray() ?? Array.Empty<string>();
                if (cautions.Length > 0)
                {
                    ModuleDivider(c, "注意事项", 16, proseY + 18, contentW - 48, 22);
                    proseY = TutorialCautions(c, cautions, proseY + 68, contentW - 48);
                }
                if ((tutorial.end_board?.codes?.Length ?? 0) > 0)
                {
                    ModuleDivider(c, "终场卡片", 16, proseY + 18, contentW - 48, 22);
                    DrawTutorialCards(c, tutorial.end_board.codes, proseY + 68, contentW - 16);
                }
            }
            // Each chapter owns its reading position. Generic page restoration
            // must not carry the previous chapter's offset into a new step.
            CompleteLibraryRefresh(chapterScroll, TutorialReadingPosition());
            var summary = FightUI.Panel(Body, workspaceX + navW + contentW + Gap * 2, 64, summaryW, panelH);
            FightUI.Icon(summary, "diamond", 18, 25, 10, 10, FightUI.GoldBorder);
            var deckHeading = FightUI.Text(summary, "教学构筑", 38, 16, summaryW - 142, 32, Compact ? 17 : 23, FightUI.Foreground);
            deckHeading.name = "Tutorial deck heading";
            deckHeading.enableWordWrapping = false; deckHeading.enableAutoSizing = true;
            deckHeading.fontSizeMin = 13; deckHeading.fontSizeMax = Compact ? 17 : 23;
            FightUI.Line(summary, 18, 54, summaryW - 36, FightUI.Border);
            var fullDeck = FightUI.TextAction(summary, "查看 →", summaryW - 92, 13, 74, 32, ShowTutorialDeck);
            fullDeck.name = "查看完整构筑"; FightUI.Hint(fullDeck.gameObject, "查看完整教学构筑与真实卡片");
            var sc = FightUI.Scroll(summary, 8, 70, summaryW - 16, panelH - 78, panelH - 78);
            var deck = DeckAcademy.Template(tutorial);
            float summaryArtH = (summaryW - 40) * .75f;
            FightUI.Art(sc, DeckLibrary.CoverFor(deck), 12, 8, summaryW - 40, summaryArtH);
            float sy = summaryArtH + 24;
            var summaryName = LibraryWrappedText(sc, tutorial.name, 14, sy, summaryW - 44, 120, 23, FightUI.Foreground);
            sy += summaryName.rectTransform.rect.height + 12;
            FightUI.Text(sc, "主 " + deck.Main.Count + "  ·  额 " + deck.Extra.Count + "  ·  副 " + deck.Side.Count, 14, sy, summaryW - 44, 30, 16, FightUI.Gold);
            sy += 48;
            if (!string.IsNullOrWhiteSpace(tutorial.regulation))
            {
                var regulation = LibraryWrappedText(sc, tutorial.regulation, 14, sy, summaryW - 44, 80, 16, FightUI.Muted);
                sy += regulation.rectTransform.sizeDelta.y + 16;
            }
            ModuleDivider(sc, "终场目标", 14, sy, summaryW - 44, 20);
            float endY = TutorialProse(sc, tutorial.end_board?.text ?? "按教学步骤完成展开。", sy + 48, summaryW - 44, 17);
            sc.sizeDelta = new Vector2(summaryW - 16, endY + 20);
            var actions = FightUI.Panel(Body, 0, BodyHeight - 74, BodyWidth, 74, FightUI.PanelColor, false, "Tutorial actions");
            FightUI.Line(actions, 0, 0, BodyWidth, FightUI.GoldBorder);
            float bw = Mathf.Min(LargeViewport ? 420 : BodyWidth, (BodyWidth - 48) / 3);
            float actionsX = (BodyWidth - bw * 3 - 24) / 2;
            FightUI.Button(actions, "复制到我的卡组", actionsX, 12, bw, 52, () => Try(() => OpenEditor(DeckAcademy.CreateEditableCopy(tutorial))));
            var practice = FightUI.Button(actions, "开始固定起手练习  →", actionsX + 12 + bw, 12, bw, 52, () => StartCore(() => DeckAcademy.StartPractice(tutorial)), true, DeckAcademy.CanPractice(tutorial));
            var replay = FightUI.Button(actions, "观看展开演示", actionsX + 24 + bw * 2, 12, bw, 52, () => StartCore(() => DeckAcademy.PlayReplay(tutorial)), false, DeckAcademy.CanReplay(tutorial));
            ExplainUnavailableLessonAction(practice, "练习资源暂不可用", "此教学的固定起手练习资源暂不可用。");
            ExplainUnavailableLessonAction(replay, "演示资源暂不可用", "此教学的展开演示资源暂不可用。");
        }
        private static void ExplainUnavailableLessonAction(UnityEngine.UI.Button button, string caption, string detail)
        {
            if (button.interactable) return;
            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = caption;
            FightUI.Hint(button.gameObject, detail);
        }
        private LibraryScrollMemory TutorialReadingPosition()
        {
            if (!tutorialReadingPositions.TryGetValue(tutorial, out var chapters))
                tutorialReadingPositions[tutorial] = chapters = new Dictionary<int, LibraryScrollMemory>();
            if (!chapters.TryGetValue(tutorialStep, out var memory))
                chapters[tutorialStep] = memory = new LibraryScrollMemory();
            return memory;
        }
        private float TutorialHeading(RectTransform parent, string title, float width, int step, int count)
        {
            var progress = FightUI.Text(parent, "步骤 " + step.ToString("00") + " / " + count.ToString("00"), 16, 14, width, 28, 15, FightUI.Gold);
            progress.characterSpacing = .8f;
            float headingY = Compact ? 40 : 52;
            var heading = FightUI.Text(parent, title, 16, headingY, width, 96, Compact ? 24 : 26, FightUI.Foreground);
            heading.enableWordWrapping = true; heading.overflowMode = TextOverflowModes.Overflow; heading.fontStyle = FontStyles.Bold;
            float height = Mathf.Max(32, heading.GetPreferredValues(heading.text, width, float.PositiveInfinity).y + 2);
            heading.rectTransform.sizeDelta = new Vector2(width, height);
            FightUI.Line(parent, 16, headingY + height + (Compact ? 8 : 12), width, FightUI.Border);
            return headingY + height + (Compact ? 18 : 30);
        }
        private float TutorialProse(RectTransform parent, string text, float y, float width, float size = 20)
        {
            var t = FightUI.Text(parent, text, 16, y, width, 160, size, FightUI.Foreground);
            t.overflowMode = TextOverflowModes.Overflow; t.lineSpacing = 8; t.paragraphSpacing = 6;
            float height = Mathf.Max(48, t.GetPreferredValues(t.text, width, 0).y + 16);
            t.rectTransform.sizeDelta = new Vector2(width, height);
            parent.sizeDelta = new Vector2(parent.sizeDelta.x, Mathf.Max(parent.sizeDelta.y, y + height + 24));
            return y + height;
        }
        private float TutorialCautions(RectTransform parent, string[] cautions, float y, float width)
        {
            for (int i = 0; i < cautions.Length; i++)
            {
                var note = FightUI.Panel(parent, 16, y, width, 64, FightUI.Raised, false, "Tutorial caution");
                var text = FightUI.Text(note, cautions[i], 48, 12, width - 62, 64, Compact ? 18 : 20, FightUI.Foreground);
                text.enableWordWrapping = true; text.overflowMode = TextOverflowModes.Overflow;
                text.lineSpacing = 4; text.paragraphSpacing = 4;
                float textHeight = text.GetPreferredValues(text.text, width - 62, float.PositiveInfinity).y + 2;
                text.rectTransform.sizeDelta = new Vector2(width - 62, textHeight);
                float height = Mathf.Max(56, textHeight + 24);
                note.sizeDelta = new Vector2(width, height);
                FightUI.Panel(note, 0, 0, 2, height, FightUI.GoldBorder, false, "Caution accent");
                FightUI.Text(note, (i + 1).ToString("00"), 10, 12, 28, 28, 14, FightUI.Gold, TextAlignmentOptions.Center);
                y += height + 10;
            }
            parent.sizeDelta = new Vector2(parent.sizeDelta.x, Mathf.Max(parent.sizeDelta.y, y + 24));
            return y - 10;
        }
        private float DrawTutorialCards(RectTransform parent, int[] codes, float y, float width)
        {
            if (codes.Length == 0) return y;
            float cw = Mathf.Min(Compact ? 100 : 144, (width - 48 - 12 * Mathf.Min(4, codes.Length - 1)) / Mathf.Max(1, Mathf.Min(5, codes.Length)));
            var names = codes.Select(DeckAcademy.CardName).ToArray();
            var printedNames = names.Select(name => FightUI.BalancedLines(name, Mathf.Max(4, Mathf.FloorToInt(cw / 15)))).ToArray();
            // Exceptional long words need a wider card, rather than a tiny
            // caption. The existing horizontal reader handles wider rows.
            var measure = FightUI.RawText(parent, "", 0, 0, cw, 32, 11);
            measure.enableWordWrapping = false; measure.overflowMode = TextOverflowModes.Overflow;
            measure.rectTransform.sizeDelta = new Vector2(10000, 10000); measure.gameObject.SetActive(false);
            foreach (string name in printedNames)
            {
                measure.text = name;
                cw = Mathf.Max(cw, Mathf.Ceil(TutorialCaptionWidth(measure) + 2));
            }
            Destroy(measure.gameObject);
            float ch = cw * 1.45f;
            float captionH = Compact ? 36 : 48;
            var row = FightUI.Scroll(parent, 24, y, width - 48, ch + captionH + 12, ch + captionH + 12, out var scroll);
            scroll.horizontal = true; scroll.vertical = false;
            if (scroll.verticalScrollbar != null) scroll.verticalScrollbar.gameObject.SetActive(false);
            scroll.verticalScrollbar = null;
            row.sizeDelta = new Vector2(Mathf.Max(width - 48, codes.Length * (cw + 12) - 12), ch + captionH + 12);
            var captions = new List<TextMeshProUGUI>();
            for (int i = 0; i < codes.Length; i++)
            {
                var code = codes[i]; var img = FightUI.Art(row, code, i * (cw + 12), 0, cw, ch, true);
                var label = LibraryWrappedText(row, printedNames[i], i * (cw + 12), ch + 10, cw, captionH, Compact ? 14 : 15, FightUI.Foreground, TextAlignmentOptions.Top, localize: false);
                // BalancedLines owns line breaks; fit whole words inside each
                // card column instead of letting TMP split them a second time.
                label.enableWordWrapping = false; label.enableAutoSizing = false;
                label.overflowMode = TextOverflowModes.Overflow;
                float naturalWidth = TutorialCaptionWidth(label);
                if (naturalWidth > cw - 2) label.fontSize = Mathf.Max(11, label.fontSize * (cw - 2) / naturalWidth);
                TutorialCaptionWidth(label);
                label.overflowMode = TextOverflowModes.Ellipsis;
                captionH = Mathf.Max(captionH, label.GetPreferredValues(label.text, cw, float.PositiveInfinity).y + 2);
                captions.Add(label);
                FightUI.RawHint(label.gameObject, names[i]);
                label.raycastTarget = true;
                var captionAction = label.gameObject.AddComponent<UnityEngine.UI.Button>();
                captionAction.targetGraphic = label; captionAction.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
                var captionColors = captionAction.colors;
                captionColors.highlightedColor = captionColors.selectedColor = FightUI.Cyan;
                captionAction.colors = captionColors;
                captionAction.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
                FightUI.FocusKey(captionAction, "tutorial:card-name:" + i + ":" + code);
                captionAction.onClick.AddListener(() => { captionAction.Select(); ShowCard(code); });
                var faceButton = FightUI.CardButton(img, () => ShowCard(code));
                FightUI.FocusKey(faceButton, "tutorial:card-face:" + i + ":" + code);
                FightUI.KeyboardProxy(captionAction, faceButton);
            }
            // Give the whole card row one measured caption baseline, including
            // earlier cards when a later card has the longest printed name.
            foreach (var caption in captions) caption.rectTransform.sizeDelta = new Vector2(cw, captionH);
            float readingHeight = ch + captionH + 12;
            bool overflow = row.sizeDelta.x > width - 47;
            row.sizeDelta = new Vector2(row.sizeDelta.x, readingHeight);
            scroll.GetComponent<RectTransform>().sizeDelta = new Vector2(width - 48, readingHeight + (overflow ? 16 : 0));
            scroll.viewport.sizeDelta = new Vector2(width - 48, readingHeight);
            parent.sizeDelta = new Vector2(parent.sizeDelta.x, Mathf.Max(parent.sizeDelta.y, y + readingHeight + 24));
            if (!overflow) return y + readingHeight;
            TutorialCardScrollbar(scroll, width - 48, readingHeight + 2);
            return y + readingHeight + 20;
        }
        private static float TutorialCaptionWidth(TextMeshProUGUI text)
        {
            // New TMP components need their mesh initialized before their font
            // metrics include the publisher font's fallback glyphs and spacing.
            var alignment = text.alignment;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.ForceMeshUpdate(true, true);
            float width = 0;
            for (int line = 0; line < text.textInfo.lineCount; line++)
                width = Mathf.Max(width, text.textInfo.lineInfo[line].maxAdvance);
            text.alignment = alignment;
            text.ForceMeshUpdate(true, true);
            return width;
        }
        private static void TutorialCardScrollbar(UnityEngine.UI.ScrollRect scroll, float width, float y)
        {
            var rail = FightUI.Panel(scroll.transform, 0, y, width, 14, Color.clear, false, "Tutorial card scrollbar");
            var track = FightUI.Panel(rail, 0, 5, width, 4, FightUI.Border, false, "Track");
            track.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            var handle = FightUI.Node(rail, "Handle", 0, 0, width, 14);
            FightUI.Stretch(handle);
            var thumb = FightUI.Panel(handle, 0, 5, width, 4, FightUI.GoldBorder, false, "Thumb");
            FightUI.Stretch(thumb); thumb.offsetMin = new Vector2(0, 5); thumb.offsetMax = new Vector2(0, -5);
            var bar = rail.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();
            bar.direction = UnityEngine.UI.Scrollbar.Direction.LeftToRight;
            bar.handleRect = handle; bar.targetGraphic = thumb.GetComponent<UnityEngine.UI.Image>();
            scroll.horizontalScrollbar = bar;
            scroll.horizontalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHide;
        }
        private void RenderPuzzles()
        {
            if (page == "puzzle") { RenderPuzzleDetail(); return; }
            PageTitle("残局挑战");
            libraryFiles = Directory.Exists(Program.PATH_PUZZLE) ? Directory.GetFiles(Program.PATH_PUZZLE, "*.lua", SearchOption.AllDirectories).Where(p => !p.Replace("\\", "/").Contains("/Academy/")).OrderBy(Path.GetFileName).ToList() : new List<string>();
            AcademyDirectorySearchField("搜索残局名称", BodyWidth * .58f, 48, DrawPuzzleFiles);
            fileList = FightUI.Node(Body, "Puzzles", 0, 160, BodyWidth * .6f, BodyHeight - 160);
            filePreview = FightUI.Panel(Body, BodyWidth * .6f + Gap, 96, BodyWidth * .4f - Gap, BodyHeight - 96); DrawPuzzleFiles();
        }
        private void DrawPuzzleFiles()
        {
            string query = search.Trim();
            BeginLibraryRefresh(puzzleScroll, query);
            FightUI.Clear(fileList);
            puzzlePreviewItems.Clear();
            var items = libraryFiles.Where(p => Path.GetFileNameWithoutExtension(p).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            if (selectedPuzzle != null && !items.Contains(selectedPuzzle)) selectedPuzzle = null;
            var c = FightUI.Scroll(fileList, 0, 0, fileList.rect.width, fileList.rect.height, items.Count * 92, out var scroll);
            scroll.name = "Puzzle directory navigation";
            if (items.Count == 0) LibraryEmpty(c, libraryFiles.Count == 0 ? "尚未添加残局" : "没有符合条件的残局", "", fileList.rect.width, fileList.rect.height, "swords", libraryFiles.Count == 0 ? (Action)null : () => ClearAcademyDirectorySearch());
            for (int i = 0; i < items.Count; i++)
            {
                var p = items[i]; bool selected = selectedPuzzle == p;
                var row = LibrarySelection(c, 1, i * 92 + 1, fileList.rect.width - 6, 82, selected, () => { PreviewPuzzle(p); selectedPuzzle = p; Navigate("puzzle"); });
                FightUI.FocusKey(row, "puzzles:file:" + Path.GetFullPath(p));
                FightUI.PreviewOnFocus(row, () => PreviewPuzzle(p));
                var icon = FightUI.Icon(row.transform, "swords", 20, 24, 30, 30, selected ? FightUI.Active : FightUI.GoldBorder);
                var title = LibraryWrappedText(row.transform, Path.GetFileNameWithoutExtension(p), 70, 8, fileList.rect.width - 120, 38, 23, selected ? FightUI.Active : FightUI.Foreground, localize: false);
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
            float copyH = Mathf.Max(64, h - artH - (h >= 400 ? 108 : 28));
            var copy = FightUI.Scroll(filePreview, 8, artH + 12, w - 16, copyH, copyH);
            string puzzleName = selectedPuzzle != null ? Path.GetFileNameWithoutExtension(selectedPuzzle)
                : libraryFiles.Count == 0 ? "尚未添加残局" : !string.IsNullOrWhiteSpace(search) ? "暂无残局预览" : "选择一个残局";
            var previewName = LibraryWrappedText(copy, puzzleName, 16, 14, w - 48, 96, 28, FightUI.Active);
            FightUI.Hint(previewName.gameObject, puzzleName);
            float detailY = 14 + previewName.rectTransform.sizeDelta.y + 24;
            if (selectedPuzzle != null)
            {
                FightUI.Text(copy, "资源来源", 16, detailY, w - 48, 24, 14, FightUI.Muted);
                var source = LibraryWrappedText(copy, Path.GetFileName(Path.GetDirectoryName(selectedPuzzle)), 16, detailY + 30, w - 48, 80, 18, FightUI.Foreground);
                copy.sizeDelta = new Vector2(w - 16, Mathf.Max(copyH, detailY + 30 + source.rectTransform.sizeDelta.y + 24));
            }
            else
            {
                copy.sizeDelta = new Vector2(w - 16, copyH);
            }
            if (h >= 400)
            {
                FightUI.Line(filePreview, 24, h - 88, w - 48, FightUI.Border);
                var view = FightUI.Button(filePreview, "查看挑战  →", 24, h - 68, w - 48, 48, () => Navigate("puzzle"), true, selectedPuzzle != null && File.Exists(selectedPuzzle));
                if (!view.interactable) FightUI.Hint(view.gameObject, "先从左侧目录选择一个可用的残局。");
            }
        }
        private void RenderPuzzleDetail()
        {
            if (selectedPuzzle == null || !File.Exists(selectedPuzzle)) { Navigate("puzzles"); return; }
            FightUI.TextAction(Body, "返回残局库  →", BodyWidth - 184, 4, 184, 44, () => ReturnAcademyDirectory("puzzles", selectedPuzzle));
            float infoW = Compact ? 332 : 400, previewW = BodyWidth - infoW - Gap, panelH = BodyHeight - 148;
            var visual = FightUI.Panel(Body, 0, 64, previewW, panelH, FightUI.PanelColor, true, "Challenge preview");
            float artH = Mathf.Max(90, panelH - 2);
            FightUI.Asset(visual, "puzzle.png", 1, 1, previewW - 2, artH);
            FightUI.Panel(visual, 1, 1, previewW - 2, artH, new Color(0, .02f, .04f, .45f), false);
            FightUI.Icon(visual, "swords", previewW / 2 - 34, Mathf.Max(28, artH / 2 - 44), 68, 68, FightUI.Active);
            var info = FightUI.Panel(Body, previewW + Gap, 64, infoW, panelH, FightUI.PanelColor, false);
            var information = FightUI.Scroll(info, 8, 8, infoW - 16, panelH - 16, panelH - 16, out var readingScroll);
            readingScroll.name = "Puzzle information reading";
            var puzzleTitle = LibraryWrappedText(information, Path.GetFileNameWithoutExtension(selectedPuzzle), 16, 16, infoW - 48, 120, 28, FightUI.Active, localize: false);
            FightUI.RawHint(puzzleTitle.gameObject, Path.GetFileNameWithoutExtension(selectedPuzzle));
            float sourceY = 16 + puzzleTitle.rectTransform.sizeDelta.y + 28;
            string description = ReadPuzzleDescription(selectedPuzzle);
            if (!string.IsNullOrWhiteSpace(description))
            {
                ModuleDivider(information, "题目说明", 16, sourceY, infoW - 48, 20);
                sourceY = TutorialProse(information, description, sourceY + 46, infoW - 48, 18) + 34;
                information.GetComponentsInChildren<TextMeshProUGUI>().Last().name = "Puzzle authored description";
            }
            FightUI.Line(information, 16, sourceY - 12, infoW - 48, FightUI.Border);
            FightUI.Text(information, "资源来源", 16, sourceY, infoW - 48, 24, 15, FightUI.Muted);
            var sourceName = LibraryWrappedText(information, Path.GetFileName(Path.GetDirectoryName(selectedPuzzle)), 16, sourceY + 32, infoW - 48, 80, 19, FightUI.Foreground);
            information.sizeDelta = new Vector2(infoW - 16, Mathf.Max(panelH - 16, sourceY + 32 + sourceName.rectTransform.sizeDelta.y + 24));
            string readingKey = Path.GetFullPath(selectedPuzzle);
            if (!puzzleReadingPositions.TryGetValue(readingKey, out var memory))
                puzzleReadingPositions[readingKey] = memory = new LibraryScrollMemory();
            CompleteLibraryRefresh(readingScroll, memory);
            FightUI.Button(Body, "开始挑战  →", BodyWidth - 272, BodyHeight - 64, 272, 56, () => StartCore(() => { Program.instance.puzzle.StartPuzzle(selectedPuzzle.Substring(0, selectedPuzzle.Length - 4)); if (Program.instance.currentServant == Program.instance.ocgcore) Program.instance.ocgcore.returnServant = Program.instance.menu; }), true);
        }
        private static string ReadPuzzleDescription(string path)
        {
            // Read the same authored message block used by the native puzzle
            // selector. Never evaluate Lua or include the separate solution.
            try
            {
                var lines = File.ReadAllText(path).Replace("\r", "").Split('\n');
                int start = Array.FindIndex(lines, line => line.TrimStart().StartsWith("--[[message", StringComparison.Ordinal));
                if (start < 0) return null;
                int end = Array.FindIndex(lines, start + 1, line => line.TrimStart().StartsWith("]]", StringComparison.Ordinal));
                return end > start ? string.Join("\n", lines.Skip(start + 1).Take(end - start - 1)).Trim() : null;
            }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
        }
        private void RenderReplays()
        {
            var sort = FightUI.Button(Body, replaySortTime ? "时间 · 最新优先" : "名称 · 升序", BodyWidth - 192, 0, 192, 48, () => { replaySortTime = !replaySortTime; Rebuild(); });
            FightUI.Hint(sort.gameObject, replaySortTime ? "当前按修改时间排序；点击切换为名称升序。" : "当前按名称升序排列；点击切换为最新优先。");
            libraryFiles = Directory.Exists(Program.PATH_REPLAY) ? Directory.GetFiles(Program.PATH_REPLAY, "*", SearchOption.AllDirectories).Where(p => p.EndsWith(".yrp3d") || p.EndsWith(".yrp")).ToList() : new List<string>();
            libraryFiles = replaySortTime ? libraryFiles.OrderByDescending(File.GetLastWriteTime).ToList() : libraryFiles.OrderBy(Path.GetFileName).ToList();
            AcademyDirectorySearchField("搜索回放名称", BodyWidth * .56f, 48, DrawReplayFiles, 0);
            fileList = FightUI.Node(Body, "Replays", 0, 64, BodyWidth * .57f, BodyHeight - 64);
            filePreview = FightUI.Panel(Body, BodyWidth * .57f + Gap, 56, BodyWidth * .43f - Gap, BodyHeight - 56); DrawReplayFiles();
            if (Program.instance.replay.servantUI == null && !replayLoading && replayLoadError == null) StartCoroutine(LoadReplayUI());
        }
        private IEnumerator LoadReplayUI()
        {
            bool retryMissingUI = replayLoadError != null && Program.instance.replay.servantUI == null
                && Program.instance.replay.showing;
            replayLoading = true; replayLoadError = null;
            if (page == "replays") RenderReplayPreview();
            // Show ignores an already-showing servant. Reset only a timed-out
            // missing UI; Hide also invalidates the previous async load version.
            if (retryMissingUI) Program.instance.replay.Hide(Program.instance.menu.Depth);
            Program.instance.ShiftToServant(Program.instance.replay);
            float deadline = Time.realtimeSinceStartup + 20f;
            while (Program.instance.replay.servantUI == null && Time.realtimeSinceStartup < deadline) yield return null;
            replayLoading = false;
            replayLoadError = Program.instance.replay.servantUI == null ? "录像界面加载超时，请重试。" : null;
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
            string query = search.Trim();
            BeginLibraryRefresh(replayScroll, query + "\t" + replaySortTime);
            FightUI.Clear(fileList); var items = libraryFiles.Where(p => Path.GetFileNameWithoutExtension(p).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            replayPreviewItems.Clear();
            if (selectedReplay != null && !items.Contains(selectedReplay)) selectedReplay = null;
            var c = FightUI.Scroll(fileList, 0, 0, fileList.rect.width, fileList.rect.height, items.Count * 96 + 36, out var scroll);
            scroll.name = "Replay directory navigation";
            if (items.Count == 0) LibraryEmpty(c, libraryFiles.Count == 0 ? "暂无回放" : "没有符合条件的回放", "", fileList.rect.width, fileList.rect.height, "replay", libraryFiles.Count == 0 ? (Action)null : () => ClearAcademyDirectorySearch());
            else { FightUI.Text(c, "回放名称", 68, 2, fileList.rect.width - 180, 28, 15, FightUI.Muted); FightUI.Text(c, "格式", fileList.rect.width - 94, 2, 76, 28, 15, FightUI.Muted, TextAlignmentOptions.Center); }
            for (int i = 0; i < items.Count; i++)
            {
                var p = items[i]; var row = LibrarySelection(c, 1, i * 96 + 36, fileList.rect.width - 6, 84, selectedReplay == p, () => PreviewReplay(p));
                FightUI.FocusKey(row, "replays:file:" + Path.GetFullPath(p));
                FightUI.PreviewOnFocus(row, () => PreviewReplay(p));
                var icon = FightUI.Icon(row.transform, "replay", 20, 24, 28, 28, selectedReplay == p ? FightUI.Active : FightUI.GoldBorder);
                var title = LibraryWrappedText(row.transform, Path.GetFileNameWithoutExtension(p), 68, 8, fileList.rect.width - 180, 38, 22, selectedReplay == p ? FightUI.Active : FightUI.Foreground, localize: false);
                LibraryTag(row.transform, Path.GetExtension(p).TrimStart('.').ToUpperInvariant(), fileList.rect.width - 104, 27, 86);
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
                if (replayUI == null) { replayError = replayLoadError ?? "正在读取录像信息…"; return; }
                parsed = replayUI.CacheYRP(ReplayName(path));
                bool readableMetadata = parsed != null && (parsed.ID == 0x31707279 || parsed.ID == 0x32707279)
                    && parsed.playerData != null && parsed.playerData.Count >= 2;
                bool readableOffline = readableMetadata && parsed.gameData != null && parsed.gameData.Count > 0;
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
            if (selectedReplay == null || !File.Exists(selectedReplay))
            {
                string emptyTitle = libraryFiles.Count == 0 ? "暂无录像预览" : !string.IsNullOrWhiteSpace(search) ? "暂无匹配录像预览" : "尚未选中录像";
                LibraryEmpty(filePreview, emptyTitle, "", w, h, "replay"); return;
            }
            string replayPath = selectedReplay;
            ReadClientReplay(replayPath, out var parsed, out bool playable, out bool godAvailable, out string replayError);
            bool reading = replayError == "正在读取录像信息…";
            float actionsY = h - 198;
            bool retryLoading = Program.instance.replay.servantUI == null && replayLoadError != null;
            ModuleDivider(filePreview, reading ? "正在读取录像" : retryLoading ? "录像加载未完成" : !playable ? "录像不可播放"
                : parsed?.playerData != null ? "双方构筑" : "录像内容", 24, 22, w - 48, 20);
            BeginLibraryRefresh(replayPreviewScroll, replayPath);
            float readingHeight = Mathf.Max(48, actionsY - 84);
            var meta = FightUI.Scroll(filePreview, 16, 70, w - 32, readingHeight, readingHeight, out var previewScroll);
            previewScroll.name = "Replay preview reading";
            var title = LibraryWrappedText(meta, Path.GetFileNameWithoutExtension(replayPath), 8, 8, w - 48, float.PositiveInfinity, Compact ? 24 : 28, FightUI.Active, localize: false);
            title.name = "Replay full title"; title.overflowMode = TextOverflowModes.Overflow;
            FightUI.RawHint(title.gameObject, Path.GetFileNameWithoutExtension(replayPath));
            float dateY = 8 + title.rectTransform.sizeDelta.y + 10;
            var date = FightUI.Text(meta, File.GetLastWriteTime(replayPath).ToString("yyyy-MM-dd HH:mm"), 8, dateY, Mathf.Max(120, w - 170), 28, 14, FightUI.Muted);
            FightUI.Hint(date.gameObject, "文件最后修改时间");
            LibraryTag(meta, Path.GetExtension(replayPath).TrimStart('.').ToUpperInvariant(), w - 136, dateY - 1, 96);
            string directory = Path.GetDirectoryName(ReplayName(replayPath));
            var source = LibraryWrappedText(meta, "来源目录 · " + (string.IsNullOrEmpty(directory) ? "回放目录" : directory.Replace('\\', '/')),
                8, dateY + 30, w - 48, float.PositiveInfinity, 14, FightUI.Muted);
            source.name = "Replay source directory";
            float sectionY = dateY + 30 + source.rectTransform.sizeDelta.y + 12, metaY = sectionY + 18;
            FightUI.Line(meta, 8, sectionY, w - 48, FightUI.Border);
            if (parsed?.playerData != null)
            {
                float playerY = metaY;
                for (int i = 0; i < parsed.playerData.Count; i++)
                {
                    var player = parsed.playerData[i]; var playerDeck = new Deck(player.main, player.extra, new List<int>());
                    var row = LibrarySelection(meta, 8, playerY, w - 48, 70, false, () => ShowReplayDeck(player));
                    string playerName = string.IsNullOrWhiteSpace(player.name) ? FightText.Get("玩家 ") + (i + 1) + FightText.Get(" · 名称未提供") : player.name;
                    var name = LibraryWrappedText(row.transform, playerName, 48, 8, w - 124, 240, 18, FightUI.Foreground, localize: false);
                    name.overflowMode = TextOverflowModes.Overflow;
                    float nameHeight = name.GetPreferredValues(playerName, w - 124, float.PositiveInfinity).y + 2;
                    name.rectTransform.sizeDelta = new Vector2(w - 124, nameHeight);
                    float rowHeight = Mathf.Max(70, nameHeight + 50);
                    row.GetComponent<RectTransform>().sizeDelta = new Vector2(w - 48, rowHeight);
                    FightUI.Text(row.transform, (i + 1).ToString("00"), 10, (rowHeight - 28) / 2, 28, 28, 16, FightUI.GoldBorder, TextAlignmentOptions.Center);
                    FightUI.Text(row.transform, "主 " + playerDeck.Main.Count + "  ·  额 " + playerDeck.Extra.Count, 48, nameHeight + 16, w - 124, 26, 14, FightUI.Muted);
                    FightUI.Icon(row.transform, "arrow", w - 78, (rowHeight - 18) / 2, 18, 18, FightUI.GoldBorder);
                    FightUI.RawHint(name.gameObject, playerName + FightText.Get(" · 点击查看录像构筑"));
                    playerY += rowHeight + 8;
                }
                meta.sizeDelta = new Vector2(w - 32, Mathf.Max(readingHeight, playerY + 8));
            }
            else
            {
                string detail = replayError ?? (replayLoading ? "正在读取录像信息…" : "此录像未保存双方构筑。");
                var explanation = LibraryWrappedText(meta, detail, 8, metaY, w - 48, float.PositiveInfinity, 18, !playable && !reading ? FightUI.Danger : FightUI.Muted);
                explanation.lineSpacing = 4;
                float explanationHeight = explanation.GetPreferredValues(FightText.Get(detail), w - 48, float.PositiveInfinity).y + 4;
                explanation.rectTransform.sizeDelta = new Vector2(w - 48, explanationHeight);
                meta.sizeDelta = new Vector2(w - 32, Mathf.Max(readingHeight, metaY + explanationHeight + 16));
            }
            CompleteLibraryRefresh(previewScroll, replayPreviewScroll);
            FightUI.Line(filePreview, 24, actionsY, w - 48, FightUI.Border);
            var playButton = FightUI.Button(filePreview, retryLoading ? "重新加载录像界面  →" : "播放录像  →", 24, h - 182, w - 48, 48,
                () => { if (retryLoading) { if (!replayLoading) StartCoroutine(LoadReplayUI()); } else PlayClientReplay(false); }, true, playable || retryLoading);
            if (!playable && !retryLoading)
            {
                playButton.GetComponentInChildren<TextMeshProUGUI>().text = reading ? "正在读取录像…" : "录像暂不可播放";
                FightUI.Hint(playButton.gameObject, replayError ?? "此录像暂时无法播放。");
            }
            var godButton = FightUI.Button(filePreview, "上帝视角", 24, h - 122, w - 48, 40, () => PlayClientReplay(true), false, godAvailable);
            if (!godAvailable) godButton.GetComponentInChildren<TextMeshProUGUI>().text = "上帝视角 · 需离线记录";
            FightUI.Hint(godButton.gameObject, godAvailable ? "从可读取的离线录像还原双方视角" : "仅可读取的离线回放支持上帝视角");
            FightUI.Button(filePreview, "重命名", 24, h - 66, (w - 60) / 2, 40, () => Prompt("重命名回放", "新名称", Path.GetFileNameWithoutExtension(replayPath), name => RenameClientReplay(replayPath, name)));
            var delete = FightUI.Button(filePreview, "删除…", (w + 12) / 2, h - 66, (w - 60) / 2, 40, () => Confirm("删除回放", "删除录像“" + FightText.Keep(Path.GetFileNameWithoutExtension(replayPath)) + "”？", () => { File.Delete(replayPath); selectedReplay = null; Rebuild(); }));
            delete.GetComponentInChildren<TextMeshProUGUI>().color = FightUI.Danger;
        }
        private void RenameClientReplay(string replayPath, string name)
        {
            CheckName(name);
            var target = Path.Combine(Path.GetDirectoryName(replayPath), name + Path.GetExtension(replayPath));
            if (string.Equals(Path.GetFullPath(target), Path.GetFullPath(replayPath), StringComparison.Ordinal))
            { ShowStatus("回放名称未改变。"); return; }
            if (File.Exists(target)) throw new IOException("该名称已存在。");
            File.Move(replayPath, target);
            selectedReplay = target;
            bool clearedSearch = name.IndexOf(search.Trim(), StringComparison.OrdinalIgnoreCase) < 0;
            if (clearedSearch) search = "";
            Rebuild();
            var selected = replayPreviewItems.FirstOrDefault(item => Equals(item.Value, target));
            if (selected != null) RevealLibraryFocusedControl(selected.Button.GetComponent<RectTransform>());
            ShowStatus(clearedSearch ? "回放已重命名；已清除搜索以显示新名称。" : "回放已重命名。");
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
            SetLibrarySelection(button, false);
            return new LibraryPreviewItem { Value = value, Button = button, Heading = heading, Divider = divider, Icons = icons,
                SelectionCorner = button.transform.Find("Selection corners") as RectTransform };
        }
        private static void SetLibrarySelection(UnityEngine.UI.Button button, bool selected)
        {
            if (button == null) return;
            button.targetGraphic.color = selected ? FightUI.Raised : FightUI.PanelColor;
            var outline = button.GetComponent<UnityEngine.UI.Outline>();
            if (outline != null)
            {
                outline.enabled = true;
                outline.effectColor = selected ? FightUI.GoldBorder : FightUI.Border;
            }
            var frame = button.GetComponentInChildren<FightFrame>();
            if (frame != null) frame.ButtonFrame = true;
            var corners = button.transform.Find("Selection corners") as RectTransform;
            if (corners == null)
            {
                corners = FightUI.Node(button.transform, "Selection corners", 0, 0, 0, 0);
                FightUI.Stretch(corners);
                var layout = corners.gameObject.AddComponent<UnityEngine.UI.LayoutElement>(); layout.ignoreLayout = true;
                var marker = corners.gameObject.AddComponent<FightSelectionCorners>();
                marker.color = FightUI.Gold; marker.raycastTarget = false;
            }
            corners.SetAsLastSibling();
            corners.gameObject.SetActive(selected);
        }
        private static void HighlightLibraryPreview(List<LibraryPreviewItem> rows, object value)
        {
            foreach (var row in rows)
            {
                if (row.Button == null) continue;
                bool selected = Equals(row.Value, value);
                SetLibrarySelection(row.Button, selected);
                if (row.Heading != null) row.Heading.color = selected ? FightUI.Active : FightUI.Foreground;
                if (row.Divider != null) row.Divider.color = selected ? FightUI.GoldBorder : FightUI.Border;
                foreach (var icon in row.Icons)
                    if (icon != null) icon.color = selected ? FightUI.Active : FightUI.GoldBorder;
            }
        }
        private static void LibraryTag(Transform parent, string label, float x, float y, float width)
        {
            var tag = FightUI.Panel(parent, x, y, width, 28, FightUI.Hex("07121DE8"), false, "Library metadata");
            tag.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            var text = FightUI.Text(tag, label, 6, 0, width - 12, 28, 14, FightUI.Gold, TextAlignmentOptions.Center);
            text.raycastTarget = false; text.characterSpacing = .4f;
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
            var b = rt.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic = rt.GetComponent<UnityEngine.UI.Image>();
            b.onClick.AddListener(() => action?.Invoke());
            var colors = b.colors; colors.highlightedColor = new Color(1.3f, 1.35f, 1.4f); colors.selectedColor = new Color(1.2f, 1.25f, 1.3f); colors.fadeDuration = .16f; b.colors = colors;
            if (frame) SetLibrarySelection(b, selected);
            var border = rt.GetComponentInChildren<FightFrame>();
            if (border != null) rt.gameObject.AddComponent<FightButtonFeedback>().Bind(border);
            return b;
        }
        private void LibraryEmpty(Transform parent, string title, string detail, float width, float height, string icon, Action clear = null, string clearCaption = "清除搜索  →")
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
                    if (!string.IsNullOrEmpty(detail) && compactActionY - compactDetailY >= 20)
                        FightUI.Text(parent, detail, 12, compactDetailY, width - 24, compactActionY - compactDetailY - 2, 14, FightUI.Muted, TextAlignmentOptions.Center);
                    float actionWidth = Mathf.Min(168, width - 24);
                    FightUI.TextAction(parent, clearCaption, (width - actionWidth) / 2, compactActionY, actionWidth, actionHeight, clear);
                }
                else if (!string.IsNullOrEmpty(detail) && height - compactDetailY >= 20)
                    FightUI.Text(parent, detail, 12, compactDetailY, width - 24, height - compactDetailY - 6, 14, FightUI.Muted, TextAlignmentOptions.Center);
                return;
            }
            bool shortPanel = height < 240;
            float cy = shortPanel ? 18 : Mathf.Min(152, Mathf.Max(58, height * .28f)), iconSize = shortPanel ? 28 : 48;
            FightUI.Icon(parent, icon, (width - iconSize) / 2, shortPanel ? 6 : cy - 28, iconSize, iconSize, FightUI.GoldBorder);
            FightUI.Text(parent, title, 24, shortPanel ? 42 : cy + 46, width - 48, shortPanel ? 32 : 44, shortPanel ? 21 : 25, FightUI.Foreground, TextAlignmentOptions.Center);
            float detailY = shortPanel ? 82 : cy + 106;
            float actionY = Mathf.Min(height - 44, string.IsNullOrEmpty(detail) ? shortPanel ? 98 : cy + 112 : shortPanel ? 130 : cy + 204);
            float detailH = clear == null ? Mathf.Min(80, height - detailY - 12) : Mathf.Min(80, actionY - detailY - 8);
            if (!string.IsNullOrEmpty(detail) && detailH >= 24) FightUI.Text(parent, detail, 32, detailY, width - 64, detailH, height < 176 ? 15 : 16, FightUI.Muted, TextAlignmentOptions.Center);
            if (clear != null && height >= 140) FightUI.TextAction(parent, clearCaption, (width - 168) / 2, actionY, 168, 40, clear);
        }
        private RectTransform TutorialStepRow(Transform parent, string label, string number, bool selected, float y, float width, Action action)
        {
            var b = LibrarySelection(parent, 4, y, width, 64, selected, action);
            var text = FightUI.Text(b.transform, label, 58, 9, width - 70, 48, 17, selected ? FightUI.Active : FightUI.Foreground, TextAlignmentOptions.MidlineLeft);
            text.enableWordWrapping = true; text.overflowMode = TextOverflowModes.Overflow;
            if (selected) text.fontStyle = FontStyles.Bold;
            // Fit only a complete Latin word. The full title can still gain
            // lines, and CJK prose is never treated as one oversized word.
            var words = new System.Text.RegularExpressions.Regex(
                @"[A-Za-z0-9\u00C0-\u024F\u1E00-\u1EFF]+(?:[.'’:/<>-][A-Za-z0-9\u00C0-\u024F\u1E00-\u1EFF]+)*").Matches(text.text, 0);
            if (words.Count > 0)
            {
                string title = text.text; var bounds = text.rectTransform.sizeDelta;
                text.enableWordWrapping = false; text.enableAutoSizing = false;
                text.rectTransform.sizeDelta = new Vector2(10000, 10000);
                float wordWidth = 0;
                foreach (System.Text.RegularExpressions.Match word in words)
                { text.text = word.Value; wordWidth = Mathf.Max(wordWidth, TutorialCaptionWidth(text)); }
                float available = width - 72;
                if (wordWidth > available) text.fontSize = Mathf.Max(12, text.fontSize * available / wordWidth);
                text.text = title; text.enableWordWrapping = true; text.rectTransform.sizeDelta = bounds;
                text.ForceMeshUpdate(true, true);
            }
            float textHeight = Mathf.Max(46, text.GetPreferredValues(text.text, width - 70, float.PositiveInfinity).y + 2);
            float rowHeight = Mathf.Max(64, textHeight + 18);
            text.rectTransform.sizeDelta = new Vector2(width - 70, textHeight);
            var row = b.GetComponent<RectTransform>(); row.sizeDelta = new Vector2(width, rowHeight);
            var marker = FightUI.Panel(b.transform, 8, (rowHeight - 36) / 2, 38, 36, selected ? FightUI.Hex("2A2B29") : FightUI.Background, false, "Route marker");
            marker.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            string markerLabel = number == "总览" ? "00" : number == "终场" ? "✓" : number;
            FightUI.RawText(marker, markerLabel, 2, 0, 34, 36, 15, selected ? FightUI.Active : FightUI.Muted, TextAlignmentOptions.Center);
            FightUI.Hint(text.gameObject, label);
            return row;
        }
        private TextMeshProUGUI LibraryWrappedText(Transform parent, string value, float x, float y, float width, float maxHeight, float size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft, bool localize = true)
        {
            var text = localize ? FightUI.Text(parent, value, x, y, width, maxHeight, size, color, alignment)
                : FightUI.RawText(parent, value, x, y, width, maxHeight, size, color, alignment);
            text.enableWordWrapping = true; text.lineSpacing = 2;
            float height = Mathf.Clamp(text.GetPreferredValues(text.text, width, float.PositiveInfinity).y + 2, size + 6, maxHeight);
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
