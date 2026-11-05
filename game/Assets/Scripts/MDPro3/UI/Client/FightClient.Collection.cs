using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using MDPro3.Duel;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using YgomSystem.Effect;

namespace MDPro3.UI.Client
{
    internal partial class FightClient
    {
        // A private camera stage keeps previews out of the duel scene and its camera stack.
        private const int CollectionLayer = 31;
        private const float CollectionTabsTop = 12, CollectionContentTop = 76, AppearanceContentTop = 136;
        private static readonly Vector3 CollectionOrigin = new Vector3(10000, 0, 0);
        private GameObject collectionStage, collectionModel, collectionStatusBackdrop;
        private Camera collectionCamera;
        private RenderTexture collectionTexture;
        private Mate collectionMate;
        private RawImage collectionSurface;
        private TextMeshProUGUI collectionLoading, appearanceCatalogTitle;
        private Button collectionUseMateButton, collectionLeftButton, collectionRightButton, appearanceMateButton;
        private readonly Dictionary<Button, int> collectionBrowseIds = new Dictionary<Button, int>();
        private int collectionPendingMateTap, collectionPendingCutinPlay;
        private readonly LibraryScrollMemory mateScroll = new LibraryScrollMemory(), animationScroll = new LibraryScrollMemory(),
            appearanceScroll = new LibraryScrollMemory(), characterScroll = new LibraryScrollMemory();
        private readonly HashSet<Image> collectionPendingIcons = new HashSet<Image>();
        private readonly List<Transform> collectionIsolationTransforms = new List<Transform>();
        private readonly HashSet<Transform> collectionKnownTransforms = new HashSet<Transform>();
        private readonly List<Renderer> collectionEnvironmentRenderers = new List<Renderer>();
        private readonly List<Renderer> collectionMateBodyRenderers = new List<Renderer>();
        private bool collectionMateFraming, collectionMateFramingPreparing;
        private RectTransform libraryLeftPanel, libraryRightPanel, librarySidePanel, libraryStagePanel;
        private RectTransform libraryFaceOverlay, libraryFrameOverlay, librarySleeveOverlay;
        private TextMeshProUGUI libraryCaption, libraryDescription, libraryPreviewDetail, libraryPreviewBadge;
        private RectTransform libraryAutoPlayAction, libraryPreviewAction;
        private readonly Dictionary<RectTransform, LibraryNodeLayout> libraryNodeLayouts = new Dictionary<RectTransform, LibraryNodeLayout>();
        private readonly Dictionary<ScrollRect, float> libraryReflowScrollPositions = new Dictionary<ScrollRect, float>();
        private sealed class LibraryNodeLayout
        {
            internal Vector2 Position, Size, ParentSize;
        }
        private bool LibraryLargeViewport => W >= 2400;
        private int collectionGeneration, collectionPlayGeneration;
        private int collectionDirectoryGeneration;
        private bool collectionDirectoryBuilding;
        private readonly Dictionary<Items.ItemType, List<Items.Item>> collectionAvailableItems = new Dictionary<Items.ItemType, List<Items.Item>>();
        private bool collectionPreviewLoaded, collectionPreviewError, collectionProtectorReady, collectionProtectorError;
        private bool collectionPreviewNeedsProtector;
        private float collectionLoadStarted;
        private readonly Dictionary<string, Texture2D> collectionProtectorTextures = new Dictionary<string, Texture2D>();
        private readonly SemaphoreSlim collectionProtectorLoader = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim collectionMateLoader = new SemaphoreSlim(1, 1);
        private sealed class CollectionProtectorIcon
        {
            internal RawImage Image;
            internal TextMeshProUGUI Status;
            internal bool IsPreview, Resolved, HasPixels, Unavailable;
        }
        private readonly List<CollectionProtectorIcon> collectionProtectorIcons = new List<CollectionProtectorIcon>();
        private bool collectionAutoPlay, collectionPlaying, collectionPreviewFilteredOut;
        private float collectionPlayUntil, collectionNextPlay;
        private int collectionMateId, collectionCutinId;
        private string collectionSearch = "", animationSearch = "", appearanceSearch = "";
        private RectTransform collectionList;
        private readonly List<AsyncOperationHandle<Sprite>> collectionSpriteHandles = new List<AsyncOperationHandle<Sprite>>();
        private List<Card> collectionCutins;
        private readonly HashSet<int> collectionNativeCutins = new HashSet<int>();
        private string appearanceCondition = "Duel", appearanceCategory = "Protector";
        private int appearancePlayer, appearanceCase;
        private bool appearanceSync;
        private AppearanceDraft appearanceDraft;
        private Button appearanceApplyButton, appearanceDiscardButton, appearanceReviewButton;
        private Button appearanceSyncButton, appearanceOverrideButton;
        private Button appearanceLocateButton, appearanceRestoreButton, appearanceSelectedResource;
        private RectTransform appearanceScopeBar, appearanceApplyBar;
        private readonly List<Button> appearanceScopeButtons = new List<Button>();
        private readonly Dictionary<string, FightIcon> appearanceCategoryIndicators = new Dictionary<string, FightIcon>();
        private TextMeshProUGUI appearanceDirtyLabel;
        private string appearanceSaveError;
        private AudioSource collectionVoiceSource;
        private AudioClip collectionVoiceClip;
        private Button collectionVoiceButton;
        private TMP_InputField collectionSearchInput;
        private string collectionVoicePath, collectionVoiceError;
        private bool collectionVoiceLoading;
        private int collectionVoiceRequest;
        private float collectionVoiceLoadStarted;
        private static readonly string[] AppearanceConditions = { "Duel", "Watch", "Replay" };
        private static readonly string[] AppearanceConditionNames = { "对战", "观战", "回放" };
        private static readonly string[] AppearanceCategories = { "PlayerName", "Face", "Frame", "Case", "Protector", "Field", "Grave", "Stand", "Mate", "Wallpaper", "Character" };
        private static readonly string[] AppearanceCategoryNames = { "玩家名称", "头像", "头像框", "卡盒", "卡套", "场地", "墓地外观", "伙伴台座", "伙伴", "壁纸", "角色与语音" };

        private sealed class AppearanceDraft
        {
            internal readonly Dictionary<string, string> Values = new Dictionary<string, string>();
            internal readonly Dictionary<string, string> Original = new Dictionary<string, string>();
            internal bool Voice, OriginalVoice, Override, OriginalOverride;
            internal bool Dirty => Voice != OriginalVoice || Override != OriginalOverride || Values.Any(v => v.Value != Original[v.Key]);
        }
        internal bool AppearanceDirty => appearanceDraft != null && appearanceDraft.Dirty;
        internal bool CollectionPreviewReady => collectionPreviewLoaded && collectionModel != null
            && collectionTexture != null && collectionTexture.IsCreated()
            && (!collectionPreviewNeedsProtector || collectionProtectorReady)
            && (page != "appearance" || CollectionVisibleProtectorIconsReady) && CollectionVisibleResourceIconsReady;
        internal bool CollectionCatalogReady => !collectionDirectoryBuilding;
        internal bool CollectionVisibleResourceIconsReady => !collectionPendingIcons.Any(image => image != null && CollectionIconVisible(image.rectTransform));
        internal bool CollectionPreviewFailed => collectionPreviewError || collectionProtectorError;
#if FIGHT_UI_PROOF
        internal bool CollectionProtectorPixelsReady => collectionPreviewNeedsProtector && collectionProtectorReady;
#endif
        internal bool CollectionVisibleProtectorIconsReady => collectionProtectorIcons
            .Where(i => !i.IsPreview && i.Image != null && CollectionIconVisible(i.Image.rectTransform))
            .All(i => i.Resolved && (i.Unavailable || (i.HasPixels && i.Image.texture != null && i.Image.color.a > .1f)));

        private void RenderCollection()
        {
            CleanupCollectionPreview();
            float tabY = CollectionTabsTop;
            CollectionTab(Body, "伙伴", "collection", 0, tabY, 120);
            CollectionTab(Body, "召唤动画", "animations", 132, tabY, 156);
            CollectionTab(Body, "外观装扮", "appearance", 300, tabY, 156);
            if (page == "appearance") RenderAppearance();
            else if (page == "animations") RenderAnimations(CollectionContentTop);
            else RenderMates(CollectionContentTop);
            CaptureLibraryNodeLayouts();
            ReflowLibraryViewport(W, H);
        }
        private void CollectionTab(Transform parent, string label, string route, float x, float y, float width)
        {
            LibraryTab(parent, label, page == route, x, y, width, () => Navigate(route));
        }
        private async void PopulateCollectionDirectory(RectTransform content, int count, Action<int> create, Action complete)
        {
            int generation = collectionGeneration, request = ++collectionDirectoryGeneration;
            collectionDirectoryBuilding = count > 0;
            // Build the new page and its navigation controls before a large catalog.
            // A complete installed animation catalog has nearly 500 entries.
            await UniTask.NextFrame();
            int index = 0;
            while (index < count && content != null && CollectionIconRequestCurrent(generation) && request == collectionDirectoryGeneration)
            {
                float deadline = Time.realtimeSinceStartup + .003f;
                int added = 0;
                do { create(index++); added++; }
                while (index < count && added < 8 && Time.realtimeSinceStartup < deadline);
                if (index < count) await UniTask.NextFrame();
            }
            if (content == null || !CollectionIconRequestCurrent(generation) || request != collectionDirectoryGeneration) return;
            collectionDirectoryBuilding = false;
            complete();
        }
        private void CompleteCollectionDirectory(ScrollRect scroll, LibraryScrollMemory memory, float width, float height)
        {
            if (scroll == null) return;
            float position = memory.Position;
            // Record new nodes once, then apply the live size if a resize happened
            // while the directory was being filled.
            CaptureLibraryNodeLayouts();
            if (libraryLeftPanel != null) { width = libraryLeftPanel.rect.width; height = libraryLeftPanel.rect.height; }
            ReflowLibraryDirectory(scroll, width, height);
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = position;
            memory.Position = position;
        }
        private TMP_InputField CollectionSearchField(Transform parent, string value, string placeholder, float x, float y, float width, float height, Action<string> changed)
        {
            Button clear = null;
            var input = FightUI.Input(parent, value, placeholder, x, y, width, height, next =>
            {
                if (clear != null) clear.gameObject.SetActive(!string.IsNullOrEmpty(next));
                changed(next);
            });
            FightUI.Icon(input.transform, "search", 12, (height - 18) / 2, 18, 18, FightUI.Muted);
            clear = FightUI.FocusKey(FightUI.Button(input.transform, "", width - 36, (height - 28) / 2, 28, 28, () =>
            {
                ClearCollectionSearch();
            }), "collection:search:clear");
            clear.name = "Clear collection search";
            FightUI.Icon(clear.transform, "close", 7, 7, 14, 14, FightUI.Muted);
            FightUI.Hint(clear.gameObject, "清除搜索，显示完整目录");
            clear.gameObject.SetActive(!string.IsNullOrEmpty(value));
            collectionSearchInput = input;
            FightUI.Hint(input.gameObject, "按名称或编号筛选，忽略首尾空格；清除搜索后显示完整目录。");
            ReflowLibraryControl(input.GetComponent<RectTransform>());
            return input;
        }
        private void ClearCollectionSearch()
        {
            if (collectionSearchInput == null) return;
            collectionSearchInput.text = "";
            collectionSearchInput.Select(); collectionSearchInput.ActivateInputField();
        }
        private void CollectionCatalogDivider(Transform parent, float width)
        {
            var line = FightUI.Panel(parent, 16, 47, width - 32, 1, FightUI.Border, false, "Collection catalog divider");
            line.GetComponent<Image>().raycastTarget = false;
        }
        private void CollectionCardCaption(Transform parent, float width, float artHeight, bool selected, float captionHeight = 60)
        {
            var surface = FightUI.Panel(parent, 3, artHeight, width - 6, captionHeight - 4,
                new Color(.025f, .05f, .07f, .62f), false, "Resource caption surface");
            surface.GetComponent<Image>().raycastTarget = false;
            var divider = FightUI.Panel(parent, 8, artHeight, width - 16, 1,
                selected ? FightUI.GoldBorder : FightUI.Border, false, "Resource caption divider");
            divider.GetComponent<Image>().raycastTarget = false;
        }
        private void StyleAppearanceScopeButton(Button button, bool selected)
        {
            button.GetComponent<Image>().color = selected ? FightUI.Hex("1D2B35") : FightUI.PanelColor;
            button.GetComponent<Outline>().effectColor = selected ? FightUI.Active : FightUI.Border;
            var caption = button.GetComponentInChildren<TextMeshProUGUI>();
            caption.color = selected ? FightUI.Active : FightUI.Muted;
            caption.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
        }
        private int LibraryGridColumns(float width, float gap) => Mathf.Max(2,
            Mathf.Max(Mathf.RoundToInt((width - 32 + gap) / (168 + gap)), Mathf.CeilToInt((width - 32 + gap) / (190 + gap))));
        private float LibraryBrowseWidth(string route) => route == "animations"
            ? Mathf.Min(720, BodyWidth * .36f) : BodyWidth * .48f;
        private float AppearanceSideWidth => Compact ? 152 : 180;
        private float AppearancePreviewWidth => Mathf.Min(880, BodyWidth * .31f);
        private float LibraryListTop => page == "appearance" && appearanceCategory == "Character" ? 144 : 110;
        private float CharacterCardCaptionSize(float viewportHeight) => viewportHeight < 180 ? 14 : 16;
        private float CharacterCardCaptionHeight(float viewportHeight)
        {
            float size = CharacterCardCaptionSize(viewportHeight);
            // Use the live CJK font's two-line box rather than assuming two glyph heights.
            float measured = appearanceCatalogTitle == null ? size * 2.5f
                : appearanceCatalogTitle.GetPreferredValues("国\n国", float.PositiveInfinity, float.PositiveInfinity).y
                    * size / appearanceCatalogTitle.fontSize;
            return Mathf.Max(viewportHeight < 180 ? 52 : 60, measured + 12);
        }
        private float CharacterCardPortraitHeight(float cellWidth, float viewportHeight, float captionHeight)
            => Mathf.Min(cellWidth * 1.1f, Mathf.Max(26, viewportHeight - captionHeight - 16));
        private float LibraryCaptionWidth(float width) => width - (libraryPreviewBadge == null ? 32 : 110);
        private float LibraryCaptionHeight(float width) => Mathf.Clamp(libraryCaption.GetPreferredValues(
            libraryCaption.text, LibraryCaptionWidth(width), float.PositiveInfinity).y + 4, libraryCaption.fontSize + 6, 104);
        private float LibraryDescriptionHeight(float width) => libraryDescription == null ? 42 : Mathf.Max(42,
            libraryDescription.GetPreferredValues(libraryDescription.text, width, float.PositiveInfinity).y + 4);
        private void AddLibraryPreviewBadge(Transform parent, string value, float width, bool pending = false)
        {
            libraryPreviewBadge = FightUI.Text(parent, value, width - 82, 20, 66, 26, 13, pending ? FightUI.Active : FightUI.Muted, TextAlignmentOptions.MidlineRight);
            FightUI.Hint(libraryPreviewBadge.gameObject, page == "appearance" ? "选择素材会更新预览；点击应用外观后保存。" : "当前为资源预览。");
        }
        private Rect LibraryStageBounds(float width, float height)
        {
            float y = 28 + LibraryCaptionHeight(width), availableH = Mathf.Max(86, height - y - 112);
            float stageW = width - 32, stageH;
            if (page == "animations")
            {
                stageH = Mathf.Min(800, Mathf.Min(availableH, stageW * 9 / 16));
                stageW = stageH * 16 / 9;
            }
            else if (page == "collection")
            {
                stageH = Mathf.Min(620, Mathf.Min(availableH, stageW / 1.4f));
                stageW = Mathf.Min(960, stageH * 1.4f);
            }
            else if (appearanceCategory == "Mate")
            {
                // Use the whole preview column, reserving the interaction row and its bottom inset.
                stageH = Mathf.Min(620, Mathf.Min(Mathf.Max(86, height - y - 64), stageW / 1.4f));
                stageW = Mathf.Min(960, stageW);
            }
            else
            {
                float reserve = appearanceCategory == "Case" ? 64 : 24;
                stageH = Mathf.Min(680, Mathf.Min(Mathf.Max(64, height - y - reserve), stageW * .82f));
            }
            return new Rect((width - stageW) / 2, y, stageW, stageH);
        }
        private Rect LibraryPreviewStatusBounds(float panelWidth, Rect stage)
        {
            float height = Mathf.Min(88, stage.height - 12);
            bool mate = page == "appearance" && appearanceCategory == "Mate";
            return new Rect(mate ? 16 : stage.x + 12, stage.y + (stage.height - height) / 2,
                mate ? panelWidth - 32 : stage.width - 24, height);
        }
        private void CaptureLibraryNodeLayouts()
        {
            if (Body == null) return;
            foreach (var rect in Body.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect == Body || libraryNodeLayouts.ContainsKey(rect)) continue;
                var parent = rect.parent as RectTransform;
                libraryNodeLayouts[rect] = new LibraryNodeLayout
                {
                    Position = new Vector2(rect.anchoredPosition.x, -rect.anchoredPosition.y), Size = rect.rect.size,
                    ParentSize = parent == null ? Vector2.zero : parent.rect.size
                };
            }
        }
        private void ReflowLibraryChildren(RectTransform parent, float width, float height)
        {
            if (parent == null) return;
            foreach (RectTransform rect in parent)
            {
                if (rect.anchorMin != rect.anchorMax || !libraryNodeLayouts.TryGetValue(rect, out var saved)) continue;
                float x = saved.Position.x, y = saved.Position.y, w = saved.Size.x, h = saved.Size.y;
                if (w >= saved.ParentSize.x * .55f) w = Mathf.Max(24, width - (saved.ParentSize.x - w));
                else if (x > saved.ParentSize.x * .55f) x = width - (saved.ParentSize.x - x);
                if (saved.ParentSize.y >= 160 && y >= saved.ParentSize.y - 100) y = height - (saved.ParentSize.y - y);
                FightUI.Place(rect, x, y, w, h);
                ReflowLibraryControl(rect);
            }
        }
        private void ReflowLibraryControl(RectTransform rect)
        {
            var input = rect.GetComponent<TMP_InputField>();
            if (input != null)
            {
                bool search = input.GetComponentsInChildren<FightIcon>().Any(icon => icon.Kind == "search");
                var clear = rect.Find("Clear collection search") as RectTransform;
                float inset = search ? 40 : 12, w = rect.rect.width - inset - (clear == null ? 12 : 44), h = rect.rect.height - 10;
                FightUI.Place(input.textViewport, inset, 5, w, h);
                FightUI.Place(input.textComponent.rectTransform, 0, 0, w, h);
                if (input.placeholder != null) FightUI.Place(input.placeholder.rectTransform, 0, 0, w, h);
                if (clear != null) FightUI.Place(clear, rect.rect.width - 36, (rect.rect.height - 28) / 2, 28, 28);
            }
            else if (rect.GetComponent<Button>() != null)
            {
                var button = rect.GetComponent<Button>();
                if (button.GetComponentInChildren<FightToggleGraphic>(true) != null)
                {
                    FightUI.ReflowSwitchRow(button, rect.rect.width, rect.rect.height);
                    return;
                }
                ReflowLibraryChildren(rect, rect.rect.width, rect.rect.height);
                if (rect.name != "Library selection")
                    foreach (RectTransform child in rect) if (child.GetComponent<TextMeshProUGUI>() != null)
                        FightUI.Place(child, child.anchoredPosition.x, 0, child.rect.width, rect.rect.height);
            }
        }
        internal void ReflowLibraryViewport(float oldW, float oldH)
        {
            if (Body == null || libraryLeftPanel == null || libraryRightPanel == null
                || (page != "collection" && page != "animations" && page != "appearance")) return;
            var inputs = Body.GetComponentsInChildren<TMP_InputField>(true).Where(input => input.isFocused)
                .Select(input => new EditorInputAnchor(input)).ToArray();
            CaptureLibraryNodeLayouts();
            var scrolls = Body.GetComponentsInChildren<ScrollRect>();
            foreach (var scroll in scrolls)
                if (!libraryReflowScrollPositions.ContainsKey(scroll) || scroll.content.rect.height > scroll.viewport.rect.height + 1)
                    libraryReflowScrollPositions[scroll] = scroll.verticalNormalizedPosition;
            ReflowLibraryChildren(Body, BodyWidth, BodyHeight);
            float top = page == "appearance" ? AppearanceContentTop : CollectionContentTop;
            float panelH = Mathf.Max(220, BodyHeight - top - (page == "appearance" ? 84 : 0));
            float leftW, rightW;
            if (page == "appearance")
            {
                float sideW = AppearanceSideWidth;
                rightW = AppearancePreviewWidth;
                leftW = BodyWidth - sideW - Gap * 2 - rightW;
                FightUI.Place(librarySidePanel, 0, top, sideW, panelH);
                ReflowLibraryChildren(librarySidePanel, sideW, panelH);
                var categories = librarySidePanel.GetComponentInChildren<ScrollRect>();
                if (categories != null)
                {
                    ReflowLibraryScroll(categories, 0, 0, sideW, panelH);
                    ReflowLibraryChildren(categories.content, sideW, categories.content.rect.height);
                }
                FightUI.Place(libraryLeftPanel, sideW + Gap, top, leftW, panelH);
                FightUI.Place(libraryRightPanel, BodyWidth - rightW, top, rightW, panelH);
                ReflowAppearanceBars();
            }
            else
            {
                leftW = LibraryBrowseWidth(page);
                rightW = BodyWidth - leftW - Gap;
                FightUI.Place(libraryLeftPanel, 0, top, leftW, panelH);
                FightUI.Place(libraryRightPanel, leftW + Gap, top, rightW, panelH);
            }
            ReflowLibraryChildren(libraryLeftPanel, leftW, panelH);
            ReflowLibraryChildren(libraryRightPanel, rightW, panelH);
            if (page == "appearance") ReflowAppearanceCatalogActions(leftW);
            float listTop = LibraryListTop;
            if (page == "appearance" && appearanceCategory == "Character")
            {
                float controlW = (leftW - 44) / 2;
                var voice = libraryLeftPanel.GetComponentsInChildren<Button>().FirstOrDefault(button => button.GetComponent<FightFocusKey>()?.Key == "collection:appearance:voice");
                if (voice != null && collectionVoiceButton != null)
                {
                    FightUI.Place(collectionVoiceButton.GetComponent<RectTransform>(), 28 + controlW, 54, controlW, 32);
                    FightUI.Place(voice.GetComponent<RectTransform>(), 16, 54, controlW, 32);
                    ReflowLibraryControl(voice.GetComponent<RectTransform>());
                    ReflowLibraryControl(collectionVoiceButton.GetComponent<RectTransform>());
                }
                var input = libraryLeftPanel.GetComponentsInChildren<TMP_InputField>().FirstOrDefault(field => field.transform.parent == libraryLeftPanel);
                if (input != null)
                {
                    FightUI.Place(input.GetComponent<RectTransform>(), 16, 94, leftW - 32, 38);
                    ReflowLibraryControl(input.GetComponent<RectTransform>());
                }
            }
            if (collectionList != null)
            {
                FightUI.Place(collectionList, 0, listTop, leftW, panelH - listTop);
                var directory = collectionList.GetComponentInChildren<ScrollRect>();
                if (directory != null)
                {
                    ReflowLibraryScroll(directory, 8, 0, leftW - 16, panelH - listTop - 8);
                    ReflowLibraryDirectory(directory, leftW, panelH);
                }
                else ReflowLibraryChildren(collectionList, leftW, panelH - listTop);
            }
            ReflowLibraryPreview(rightW, panelH);
            Canvas.ForceUpdateCanvases();
            for (int i = 0; i < scrolls.Length; i++) if (scrolls[i] != null)
                scrolls[i].verticalNormalizedPosition = scrolls[i].content.rect.height <= scrolls[i].viewport.rect.height + 1 ? 1 : libraryReflowScrollPositions[scrolls[i]];
            if (page == "appearance" && librarySidePanel != null)
            {
                var categoryScroll = librarySidePanel.GetComponentInChildren<ScrollRect>();
                if (categoryScroll != null)
                {
                    int index = Array.IndexOf(AppearanceCategories, appearanceCategory);
                    var categories = categoryScroll.content.GetComponentsInChildren<Button>().Where(button => button.transform.parent == categoryScroll.content).ToArray();
                    if (index >= 0 && index < categories.Length) RevealLibraryFocusedControl(categories[index].GetComponent<RectTransform>());
                }
            }
            var selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(Body)) RevealLibraryFocusedControl(selected.GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases();
            foreach (var input in inputs) input.Restore();
        }
        private void ReflowLibraryScroll(ScrollRect scroll, float x, float y, float width, float height)
        {
            FightUI.Place(scroll.GetComponent<RectTransform>(), x, y, width, height);
            FightUI.Place(scroll.viewport, 0, 0, width, height);
            scroll.content.sizeDelta = new Vector2(width, scroll.content.rect.height);
            if (scroll.verticalScrollbar != null)
            {
                var rail = scroll.verticalScrollbar.GetComponent<RectTransform>();
                FightUI.Place(rail, width - 15, 2, 14, height - 4);
                foreach (RectTransform child in rail) if (child.name == "Track") FightUI.Place(child, 10, 0, 4, height - 4);
            }
        }
        private void ReflowLibraryDirectory(ScrollRect directory, float width, float panelHeight)
        {
            // Keep the full scroll range while entries are arriving in batches.
            if (collectionDirectoryBuilding) return;
            var cards = directory.content.GetComponentsInChildren<Button>().Where(button => button.name == "Library selection" && button.transform.parent == directory.content).ToArray();
            if (cards.Length == 0) { ReflowLibraryEmpty(directory.content, width - 16, directory.viewport.rect.height); return; }
            if (page == "animations")
            {
                float rowY = 8;
                for (int i = 0; i < cards.Length; i++)
                {
                    var row = cards[i].GetComponent<RectTransform>();
                    float rowW = width - 32;
                    var title = row.Find("Animation resource title")?.GetComponent<TextMeshProUGUI>();
                    float titleH = title == null ? 40 : Mathf.Max(40, title.GetPreferredValues(title.text, rowW - 120, float.PositiveInfinity).y + 4);
                    float rowH = Mathf.Max(82, titleH + 42);
                    FightUI.Place(row, 8, rowY, rowW, rowH); ReflowLibraryChildren(row, rowW, rowH);
                    if (title != null) FightUI.Place(title.rectTransform, 80, 7, rowW - 120, titleH);
                    var identifier = row.Find("Animation resource id") as RectTransform;
                    if (identifier != null) FightUI.Place(identifier, 80, titleH + 10, rowW - 120, 22);
                    var play = row.Find("Icon play") as RectTransform;
                    if (play != null) FightUI.Place(play, rowW - 28, (rowH - 24) / 2, 20, 24);
                    rowY += rowH + 10;
                }
                directory.content.sizeDelta = new Vector2(width - 16, Mathf.Max(directory.viewport.rect.height, rowY + 8));
                return;
            }
            bool mates = page == "collection", character = page == "appearance" && appearanceCategory == "Character";
            float gap = mates ? 12 : 10;
            int columns = LibraryGridColumns(width, gap);
            float cellW = (width - 32 - (columns - 1) * gap) / columns;
            float viewportH = panelHeight - LibraryListTop - 8;
            float minimumCaptionH = character ? CharacterCardCaptionHeight(viewportH) : 60;
            float captionSize = mates ? 17 : character ? CharacterCardCaptionSize(viewportH) : 16;
            float artH = mates ? cellW : character ? CharacterCardPortraitHeight(cellW, viewportH, minimumCaptionH)
                : Mathf.Min(appearanceCategory == "Protector" ? cellW * 1.43f : cellW, Mathf.Max(84, panelHeight - 190));
            // One long resource name expands only its grid row. Measure the
            // actual caption rather than inserting breaks into names and IDs.
            var captionHeights = new float[Mathf.CeilToInt(cards.Length / (float)columns)];
            for (int i = 0; i < cards.Length; i++)
            {
                float measured = minimumCaptionH;
                foreach (Transform child in cards[i].transform)
                {
                    var text = child.GetComponent<TextMeshProUGUI>();
                    var hint = text == null ? null : text.GetComponent<FightTooltip>();
                    if (hint == null || string.IsNullOrEmpty(hint.Hint)) continue;
                    text.fontSize = captionSize; text.text = hint.Hint;
                    measured = Mathf.Max(measured, text.GetPreferredValues(text.text, cellW - (mates ? 14 : 10), float.PositiveInfinity).y + 12);
                }
                int row = i / columns;
                captionHeights[row] = Mathf.Max(captionHeights[row], measured);
            }
            float rowTop = 8;
            for (int i = 0; i < cards.Length; i++)
            {
                float captionH = captionHeights[i / columns], cellH = artH + captionH;
                var card = cards[i].GetComponent<RectTransform>();
                FightUI.Place(card, 8 + i % columns * (cellW + gap), rowTop, cellW, cellH);
                foreach (RectTransform child in card)
                {
                    if (child.name == "Resource icon" || child.name == "Protector texture preview" || child.name == "No appearance selected")
                    {
                        float imageH = mates ? cellW - 16 : artH - 14;
                        float imageW = !mates && !character && appearanceCategory == "Protector" ? imageH / 1.43f : mates ? imageH : character ? cellW - 14 : Mathf.Min(cellW - 14, imageH);
                        FightUI.Place(child, (cellW - imageW) / 2, mates ? 8 : 7, imageW, imageH);
                        ReflowLibraryIcon(child);
                    }
                    else if (child.name == "Resource caption surface") FightUI.Place(child, 3, artH, cellW - 6, captionH - 4);
                    else if (child.name == "Resource caption divider") FightUI.Place(child, 8, artH, cellW - 16, 1);
                    else if (child.name == "Resource selection state")
                    {
                        FightUI.Place(child, cellW - 68, 8, 60, 22);
                        var badge = child.GetComponentInChildren<TextMeshProUGUI>();
                        if (badge != null) FightUI.Place(badge.rectTransform, 2, 0, 56, 22);
                    }
                    else if (character && child.GetComponent<Image>() != null)
                        FightUI.Place(child, 7, 7, cellW - 14, artH - 14);
                    var text = child.GetComponent<TextMeshProUGUI>();
                    if (text == null) continue;
                    var hint = text.GetComponent<FightTooltip>();
                    if (hint != null && !string.IsNullOrEmpty(hint.Hint))
                    {
                        text.fontSize = captionSize;
                        text.text = hint.Hint;
                        FightUI.Place(child, mates ? 7 : 5, artH, cellW - (mates ? 14 : 10), captionH - 8);
                    }
                }
                if (i % columns == columns - 1 || i == cards.Length - 1) rowTop += cellH + 12;
            }
            directory.content.sizeDelta = new Vector2(width - 16, Mathf.Max(directory.viewport.rect.height, rowTop + 8));
        }
        private void ReflowLibraryIcon(RectTransform icon)
        {
            foreach (RectTransform child in icon)
            {
                if (child.GetComponent<TextMeshProUGUI>() != null) FightUI.Place(child, 0, 0, icon.rect.width, icon.rect.height);
                else if (child.GetComponent<FightIcon>() != null)
                {
                    float size = Mathf.Min(icon.rect.width, icon.rect.height) * .3f;
                    FightUI.Place(child, (icon.rect.width - size) / 2, (icon.rect.height - size) / 2, size, size);
                }
            }
        }
        private void ReflowLibraryEmpty(RectTransform content, float width, float height)
        {
            content.sizeDelta = new Vector2(width, height);
            bool shortPanel = height < 240;
            float cy = shortPanel ? 18 : Mathf.Min(152, Mathf.Max(58, height * .28f)), iconSize = shortPanel ? 28 : 48;
            var texts = content.GetComponentsInChildren<TextMeshProUGUI>().Where(text => text.transform.parent == content).ToArray();
            bool hasDetail = texts.Length > 1;
            float actionY = Mathf.Min(height - 44, hasDetail ? shortPanel ? 130 : cy + 204 : shortPanel ? 98 : cy + 112);
            foreach (RectTransform child in content)
            {
                if (child.GetComponent<FightIcon>() != null) FightUI.Place(child, (width - iconSize) / 2, shortPanel ? 6 : cy - 28, iconSize, iconSize);
                if (child.GetComponent<Button>() != null)
                {
                    FightUI.Place(child, (width - 168) / 2, actionY, 168, 40);
                    ReflowLibraryControl(child);
                }
            }
            if (texts.Length == 0) return;
            texts[0].fontSize = shortPanel ? 21 : 25;
            FightUI.Place(texts[0].rectTransform, 24, shortPanel ? 42 : cy + 46, width - 48, shortPanel ? 32 : 44);
            if (!hasDetail) return;
            float detailY = shortPanel ? 82 : cy + 106;
            bool hasAction = content.GetComponentsInChildren<Button>().Any(button => button.transform.parent == content);
            float detailH = hasAction ? Mathf.Clamp(actionY - detailY - 8, 24, 80) : shortPanel ? Mathf.Max(28, height - 90) : 80;
            FightUI.Place(texts[1].rectTransform, 32, detailY, width - 64, detailH);
        }
        private void ReflowLibraryPreview(float width, float height)
        {
            if (libraryStagePanel == null || collectionSurface == null || collectionCamera == null || libraryCaption == null) return;
            libraryCaption.enableAutoSizing = true; libraryCaption.fontSizeMin = 18;
            libraryCaption.fontSizeMax = page == "appearance" ? 22 : 25; libraryCaption.maxVisibleLines = 3;
            float captionH = LibraryCaptionHeight(width);
            FightUI.Place(libraryCaption.rectTransform, 16, 16, LibraryCaptionWidth(width), captionH);
            if (libraryPreviewBadge != null) FightUI.Place(libraryPreviewBadge.rectTransform, width - 82, 20, 66, 26);
            var stage = LibraryStageBounds(width, height);
            FightUI.Place(libraryStagePanel, stage.x, stage.y, stage.width, stage.height);
            FightUI.Place(collectionSurface.rectTransform, 0, 0, stage.width, stage.height);
            foreach (RectTransform child in libraryStagePanel) if (child.name == "Display stage")
                FightUI.Place(child, 1, 1, stage.width - 2, stage.height - 2);
            collectionCamera.aspect = stage.width / stage.height; collectionCamera.ResetProjectionMatrix();
            var status = LibraryPreviewStatusBounds(width, stage);
            if (collectionStatusBackdrop != null) FightUI.Place(collectionStatusBackdrop.GetComponent<RectTransform>(), status.x, status.y, status.width, status.height);
            if (collectionLoading != null) FightUI.Place(collectionLoading.rectTransform, 12, 8, status.width - 24, status.height - 16);
            float actionY = stage.yMax + 14;
            if (page == "collection")
            {
                float actionX = stage.width >= 340 ? stage.x : 16, actionWidth = stage.width >= 340 ? stage.width : width - 32;
                if (collectionUseMateButton != null) FightUI.Place(collectionUseMateButton.GetComponent<RectTransform>(), actionX, actionY, 184, 44);
                if (collectionLeftButton != null) FightUI.Place(collectionLeftButton.GetComponent<RectTransform>(), actionX + actionWidth - 140, actionY, 64, 44);
                if (collectionRightButton != null) FightUI.Place(collectionRightButton.GetComponent<RectTransform>(), actionX + actionWidth - 64, actionY, 64, 44);
                if (libraryDescription != null) FightUI.Place(libraryDescription.rectTransform, actionX, actionY + 48, actionWidth, LibraryDescriptionHeight(actionWidth));
                return;
            }
            if (page == "animations")
            {
                if (libraryAutoPlayAction != null) FightUI.Place(libraryAutoPlayAction, stage.width >= 342 ? stage.x : 16, actionY, 186, 44);
                if (libraryPreviewAction != null)
                {
                    FightUI.Place(libraryPreviewAction, stage.width >= 342 ? stage.xMax - 144 : width - 160, actionY, 144, 44);
                    ReflowLibraryControl(libraryPreviewAction);
                }
                if (libraryDescription != null) FightUI.Place(libraryDescription.rectTransform, stage.x, actionY + 48, stage.width, LibraryDescriptionHeight(stage.width));
                return;
            }
            if (libraryFaceOverlay != null) { FightUI.Place(libraryFaceOverlay, stage.x + 12, stage.y + 12, 58, 58); ReflowLibraryIcon(libraryFaceOverlay); }
            if (libraryFrameOverlay != null) { FightUI.Place(libraryFrameOverlay, stage.x + 8, stage.y + 8, 66, 66); ReflowLibraryIcon(libraryFrameOverlay); }
            float sleeveH = Mathf.Min(112, stage.height - 16), sleeveW = sleeveH * .7f;
            if (librarySleeveOverlay != null) { FightUI.Place(librarySleeveOverlay, stage.xMax - sleeveW - 12, stage.yMax - sleeveH - 12, sleeveW, sleeveH); ReflowLibraryIcon(librarySleeveOverlay); }
            if (appearanceMateButton != null) FightUI.Place(appearanceMateButton.GetComponent<RectTransform>(), stage.xMax - 92, actionY, 92, 34);
            if (libraryPreviewDetail != null) FightUI.Place(libraryPreviewDetail.rectTransform, 16, actionY,
                width - (appearanceCategory == "Mate" ? 124 : 32), appearanceCategory == "Mate" ? 34 : 52);
            if (libraryPreviewAction != null)
            {
                FightUI.Place(libraryPreviewAction, 16, actionY, width - 32, 36);
                ReflowLibraryControl(libraryPreviewAction);
            }
        }
        private RectTransform AddLibraryAppearanceOverlay(RectTransform parent, string id, Items.ItemType type, float x, float y, float width, float height, bool required = false)
        {
            int index = parent.childCount; CollectionItemImage(parent, id, type, x, y, width, height, required);
            return parent.GetChild(index) as RectTransform;
        }
        private void RevealLibraryFocusedControl(RectTransform rect)
        {
            if (rect == null) return;
            var scroll = rect.GetComponentInParent<ScrollRect>();
            if (scroll == null || scroll.content == null || scroll.viewport == null || !scroll.vertical) return;
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            float bottom = scroll.viewport.InverseTransformPoint(corners[0]).y, top = scroll.viewport.InverseTransformPoint(corners[1]).y;
            float offset = bottom < scroll.viewport.rect.yMin ? scroll.viewport.rect.yMin - bottom : top > scroll.viewport.rect.yMax ? scroll.viewport.rect.yMax - top : 0;
            scroll.content.anchoredPosition += new Vector2(0, offset);
        }
        private void RenderMates(float y)
        {
            var available = AvailableCollectionItems(Items.ItemType.Mate);
            if (collectionMateId == 0 && available.Count > 0) collectionMateId = available[0].id;
            float leftW = LibraryBrowseWidth("collection"), rightX = leftW + Gap, rightW = BodyWidth - rightX, panelH = BodyHeight - y;
            var left = FightUI.Panel(Body, 0, y, leftW, panelH, FightUI.PanelColor, false);
            libraryLeftPanel = left;
            var heading = FightUI.Text(left, "伙伴目录", 16, 10, leftW - 32, 32, 21, FightUI.Foreground);
            heading.fontStyle = FontStyles.Bold; CollectionCatalogDivider(left, leftW);
            CollectionSearchField(left, collectionSearch, "搜索伙伴名称或编号", 16, 56, leftW - 32, 42, v => { collectionSearch = v; DrawMateGrid(available, libraryLeftPanel.rect.width, libraryLeftPanel.rect.height); });
            collectionList = FightUI.Node(left, "Mate catalog", 0, 110, leftW, panelH - 110);
            DrawMateGrid(available, leftW, panelH);
            var right = FightUI.Panel(Body, rightX, y, rightW, panelH, FightUI.PanelColor, true);
            libraryRightPanel = right;
            var item = available.FirstOrDefault(i => i.id == collectionMateId);
            AddLibraryPreviewBadge(right, "预览", rightW);
            var mateCaption = LibraryWrappedText(right, item.id == 0 ? FightText.Get("暂无可用伙伴") : CollectionDisplayName(item), 16, 16, LibraryCaptionWidth(rightW), 104, 25, FightUI.Active, localize: false);
            libraryCaption = mateCaption;
            FightUI.RawHint(mateCaption.gameObject, item.id == 0 ? FightText.Get("暂无可用伙伴") : CollectionDisplayName(item));
            var stage = LibraryStageBounds(rightW, panelH);
            CreateCollectionStage(right, stage.x, stage.y, stage.width, stage.height, false);
            collectionUseMateButton = FightUI.Button(right, "选用并前往外观", stage.x, stage.yMax + 14, 184, 44, () =>
            {
                appearanceCategory = "Mate"; appearanceSearch = "";
                EnsureAppearanceDraft(); appearanceDraft.Values["Mate"] = collectionMateId.ToString();
                Navigate("appearance");
            }, true, item.id != 0);
            collectionUseMateButton.name = "选用伙伴";
            collectionLeftButton = FightUI.Button(right, "左转", stage.xMax - 140, stage.yMax + 14, 64, 44, () => RotateCollectionMate(-30), false, false);
            collectionRightButton = FightUI.Button(right, "右转", stage.xMax - 64, stage.yMax + 14, 64, 44, () => RotateCollectionMate(30), false, false);
            libraryDescription = LibraryWrappedText(right, "", stage.x, stage.yMax + 62, stage.width, 42, 14, FightUI.Muted);
            RefreshCollectionPreviewContext();
            FightUI.Hint(collectionUseMateButton.gameObject, "将所选伙伴加入当前外观草稿，前往外观页确认范围并应用。");
            if (item.id != 0) LoadCollectionMate(item.id, collectionGeneration);
            else CollectionPreviewStatus("尚未安装伙伴资源");
        }
        private void DrawMateGrid(List<Items.Item> available, float width, float panelHeight)
        {
            if (collectionList == null) return;
            BeginLibraryRefresh(mateScroll, collectionSearch);
            FightUI.Clear(collectionList); collectionBrowseIds.Clear();
            var items = available.Where(i => CollectionMatches(CollectionDisplayName(i), collectionSearch, i.id.ToString())).ToList();
            collectionPreviewFilteredOut = collectionMateId != 0 && !items.Any(item => item.id == collectionMateId);
            int columns = LibraryGridColumns(width, 12);
            float cellW = (width - 32 - (columns - 1) * 12) / columns, cellH = cellW + 60;
            var content = FightUI.Scroll(collectionList, 8, 0, width - 16, panelHeight - 118, Mathf.Ceil(items.Count / (float)columns) * (cellH + 12) + 16, out var scroll);
            scroll.name = "Mate directory navigation";
            if (items.Count == 0) LibraryEmpty(content, available.Count == 0 ? "尚未安装伙伴资源" : "没有匹配的伙伴", "", width - 16, panelHeight - 118, "diamond", available.Count == 0 ? (Action)null : ClearCollectionSearch);
            PopulateCollectionDirectory(content, items.Count, i =>
            {
                var item = items[i]; float x = 8 + i % columns * (cellW + 12), y = 8 + i / columns * (cellH + 12);
                var b = LibrarySelection(content, x, y, cellW, cellH, item.id == collectionMateId, () =>
                {
                    PreviewCollectionMate(item);
                    collectionPendingMateTap = item.id;
                    PlayPendingCollectionAction();
                });
                collectionBrowseIds[b] = item.id;
                CollectionCardCaption(b.transform, cellW, cellW, item.id == collectionMateId);
                FightUI.PreviewOnFocus(b, () => PreviewCollectionMate(item));
                CollectionItemImage(b.transform, item.id.ToString(), Items.ItemType.Mate, 8, 8, cellW - 16, cellW - 16);
                string displayName = CollectionDisplayName(item);
                var name = LibraryWrappedText(b.transform, displayName, 7, cellW, cellW - 14, 52, 17, item.id == collectionMateId ? FightUI.Active : FightUI.Foreground, TextAlignmentOptions.Center, localize: false);
                FightUI.RawHint(name.gameObject, displayName);
            }, () => CompleteCollectionDirectory(scroll, mateScroll, width, panelHeight));
            RefreshCollectionPreviewContext();
            CompleteLibraryRefresh(scroll, mateScroll);
            CaptureLibraryNodeLayouts();
        }
        private void HighlightCollectionEntry(int id)
        {
            foreach (var entry in collectionBrowseIds)
            {
                var button = entry.Key;
                if (button == null) continue;
                bool selected = entry.Value == id;
                SetLibrarySelection(button, selected);
                var divider = button.transform.Find("Resource caption divider");
                if (divider != null) divider.GetComponent<Image>().color = selected ? FightUI.GoldBorder : FightUI.Border;
                var play = button.transform.Find("Icon play");
                if (play != null) play.GetComponent<FightIcon>().color = selected ? FightUI.Gold : FightUI.Muted;
                foreach (var text in button.GetComponentsInChildren<TextMeshProUGUI>())
                    if (text.GetComponent<FightTooltip>() != null) text.color = selected ? FightUI.Active : FightUI.Foreground;
            }
        }
        private void PreviewCollectionMate(Items.Item item)
        {
            if (page != "collection" || collectionStage == null || collectionMateId == item.id && !CollectionPreviewFailed) return;
            collectionMateId = item.id; collectionPendingMateTap = 0;
            collectionPreviewFilteredOut = false; RefreshCollectionPreviewContext();
            HighlightCollectionEntry(item.id);
            libraryCaption.text = CollectionDisplayName(item); FightUI.RawHint(libraryCaption.gameObject, libraryCaption.text);
            ClearCollectionModel();
            collectionPreviewLoaded = collectionPreviewError = false;
            collectionLoadStarted = Time.unscaledTime;
            CollectionPreviewStatus("正在加载伙伴…");
            ReflowLibraryPreview(libraryRightPanel.rect.width, libraryRightPanel.rect.height);
            LoadCollectionMate(item.id, collectionGeneration);
        }
        private void PreviewCollectionAnimation(Card card)
        {
            if (page != "animations" || collectionStage == null || collectionCutinId == card.Id && !CollectionPreviewFailed) return;
            collectionCutinId = card.Id; collectionPendingCutinPlay = 0;
            collectionPreviewFilteredOut = false;
            HighlightCollectionEntry(card.Id);
            libraryCaption.text = card.Name; FightUI.RawHint(libraryCaption.gameObject, card.Name);
            ClearCollectionModel();
            ReflowLibraryPreview(libraryRightPanel.rect.width, libraryRightPanel.rect.height);
            LoadCollectionAnimation(card.Id, false);
        }
        private void PlayPendingCollectionAction()
        {
            if (CollectionPreviewFailed)
            {
                collectionPendingMateTap = collectionPendingCutinPlay = 0;
                return;
            }
            if (page == "collection" && collectionPendingMateTap == collectionMateId && collectionPendingMateTap != 0
                && collectionPreviewLoaded && collectionMate != null)
            {
                collectionPendingMateTap = 0;
                collectionMate.Play(Mate.MateAction.Tap);
            }
            if (page == "animations" && collectionPendingCutinPlay == collectionCutinId && collectionPendingCutinPlay != 0
                && CollectionPreviewReady)
            {
                collectionPendingCutinPlay = 0;
                PlayCollectionAnimation();
            }
        }

        private void RotateCollectionMate(float degrees)
        {
            if (collectionMate == null || collectionMate.parent == null) return;
            collectionMate.parent.Rotate(Vector3.up, degrees, Space.Self);
        }

        private void RenderAnimations(float y)
        {
            LoadCollectionCutinCatalog();
            if (collectionCutinId == 0 && collectionCutins.Count > 0) collectionCutinId = collectionCutins[0].Id;
            float leftW = LibraryBrowseWidth("animations"), rightX = leftW + Gap, rightW = BodyWidth - rightX;
            var left = FightUI.Panel(Body, 0, y, leftW, BodyHeight - y, FightUI.PanelColor, false);
            libraryLeftPanel = left;
            var heading = FightUI.Text(left, "召唤动画", 16, 14, leftW - 32, 32, 21, FightUI.Foreground);
            heading.fontStyle = FontStyles.Bold; CollectionCatalogDivider(left, leftW);
            CollectionSearchField(left, animationSearch, "搜索卡名或编号", 16, 56, leftW - 32, 42, v => { animationSearch = v; DrawAnimationList(libraryLeftPanel.rect.width, libraryLeftPanel.rect.height); });
            collectionList = FightUI.Node(left, "Animation catalog", 0, 110, leftW, BodyHeight - y - 110);
            DrawAnimationList(leftW, BodyHeight - y);
            var right = FightUI.Panel(Body, rightX, y, rightW, BodyHeight - y, FightUI.Raised);
            libraryRightPanel = right;
            var card = collectionCutins.FirstOrDefault(c => c.Id == collectionCutinId);
            AddLibraryPreviewBadge(right, "预览", rightW);
            libraryCaption = LibraryWrappedText(right, card == null ? FightText.Get("暂无可用召唤动画") : card.Name, 16, 16, LibraryCaptionWidth(rightW), 104, 25, FightUI.Gold, localize: false);
            FightUI.RawHint(libraryCaption.gameObject, libraryCaption.text);
            var stage = LibraryStageBounds(rightW, BodyHeight - y);
            CreateCollectionStage(right, stage.x, stage.y, stage.width, stage.height, true);
            var autoPlay = FightUI.FocusKey(FightUI.SwitchRow(right, "自动播放", collectionAutoPlay, stage.x, stage.yMax + 14, 186, 44,
                () =>
                {
                    collectionAutoPlay = !collectionAutoPlay; collectionNextPlay = Time.unscaledTime;
                    FightUI.SetSwitchValue(libraryAutoPlayAction.GetComponent<Button>(), collectionAutoPlay);
                }), "collection:animation:auto-play");
            autoPlay.interactable = card != null;
            libraryAutoPlayAction = autoPlay.GetComponent<RectTransform>();
            libraryPreviewAction = FightUI.FocusKey(FightUI.Button(right, "播放动画", stage.xMax - 144, stage.yMax + 14, 144, 44,
                () =>
                {
                    if (CollectionPreviewFailed)
                    {
                        var selected = collectionCutins.FirstOrDefault(c => c.Id == collectionCutinId);
                        if (selected != null) PreviewCollectionAnimation(selected);
                    }
                    else PlayCollectionAnimation();
                }, true, false), "collection:animation:play").GetComponent<RectTransform>();
            libraryDescription = LibraryWrappedText(right, "", stage.x, stage.yMax + 62, stage.width, 42, 14, FightUI.Muted);
            RefreshCollectionPreviewContext();
            if (card != null) LoadCollectionAnimation(collectionCutinId, false);
            else CollectionPreviewStatus("尚未安装召唤动画资源");
        }
        private void DrawAnimationList(float width, float panelHeight)
        {
            if (collectionList == null) return;
            BeginLibraryRefresh(animationScroll, animationSearch);
            FightUI.Clear(collectionList); collectionBrowseIds.Clear();
            var cards = collectionCutins.Where(c => CollectionMatches(c.Name, animationSearch, c.Id.ToString())).ToList();
            collectionPreviewFilteredOut = collectionCutinId != 0 && !cards.Any(card => card.Id == collectionCutinId);
            var content = FightUI.Scroll(collectionList, 8, 0, width - 16, panelHeight - 118, cards.Count * 92 + 16, out var scroll);
            scroll.name = "Animation directory navigation";
            if (cards.Count == 0) LibraryEmpty(content, collectionCutins.Count == 0 ? "尚未安装召唤动画" : "没有匹配的动画", "", width - 16, panelHeight - 118, "replay", collectionCutins.Count == 0 ? (Action)null : ClearCollectionSearch);
            float rowY = 8;
            PopulateCollectionDirectory(content, cards.Count, i =>
            {
                var card = cards[i]; var b = LibrarySelection(content, 8, rowY, width - 32, 82, card.Id == collectionCutinId, () =>
                {
                    PreviewCollectionAnimation(card);
                    collectionPendingCutinPlay = card.Id;
                    PlayPendingCollectionAction();
                });
                collectionBrowseIds[b] = card.Id;
                FightUI.PreviewOnFocus(b, () => PreviewCollectionAnimation(card));
                FightUI.Art(b.transform, card.Id, 8, 8, 60, 60);
                var name = LibraryWrappedText(b.transform, card.Name, 80, 7, width - 152, 160, 18, card.Id == collectionCutinId ? FightUI.Active : FightUI.Foreground, TextAlignmentOptions.MidlineLeft, localize: false);
                name.name = "Animation resource title";
                FightUI.RawHint(name.gameObject, card.Name);
                FightUI.Text(b.transform, "卡片编号 " + card.Id, 80, 50, width - 152, 22, 13, FightUI.Muted, TextAlignmentOptions.MidlineLeft).name = "Animation resource id";
                FightUI.Icon(b.transform, "play", width - 60, 28, 20, 24, card.Id == collectionCutinId ? FightUI.Gold : FightUI.Muted);
                float titleH = Mathf.Max(40, name.GetPreferredValues(name.text, width - 152, float.PositiveInfinity).y + 4);
                float rowH = Mathf.Max(82, titleH + 42);
                FightUI.Place(b.GetComponent<RectTransform>(), 8, rowY, width - 32, rowH);
                FightUI.Place(name.rectTransform, 80, 7, width - 152, titleH);
                FightUI.Place(b.transform.Find("Animation resource id") as RectTransform, 80, titleH + 10, width - 152, 22);
                FightUI.Place(b.transform.Find("Icon play") as RectTransform, width - 60, (rowH - 24) / 2, 20, 24);
                rowY += rowH + 10;
            }, () => CompleteCollectionDirectory(scroll, animationScroll, width, panelHeight));
            RefreshCollectionPreviewContext();
            CompleteLibraryRefresh(scroll, animationScroll);
            CaptureLibraryNodeLayouts();
        }
        private void LoadCollectionCutinCatalog()
        {
            if (collectionCutins != null) return;
            collectionCutins = new List<Card>();
            var found = new HashSet<int>();
            string folder = Program.root + "MonsterCutin", diy = Program.root + "MonsterCutin2";
            if (Directory.Exists(folder))
                foreach (var directory in Directory.GetDirectories(folder))
                    if (int.TryParse(Path.GetFileName(directory), out int id) && Directory.GetFiles(directory).Length > 0)
                    { var card = CardsManager.GetCard(id); if (card != null && found.Add(id)) { collectionCutins.Add(card); collectionNativeCutins.Add(id); } }
            if (Directory.Exists(diy))
                foreach (var file in Directory.GetFiles(diy))
                    if (int.TryParse(Path.GetFileName(file), out int id))
                    { var card = CardsManager.GetCard(id); if (card != null && found.Add(id)) collectionCutins.Add(card); }
            collectionCutins.Sort(CardsManager.ComparisonOfCard());
        }
        private void PlayCollectionAnimation()
        {
            if (collectionCutinId != 0 && CollectionPreviewReady) LoadCollectionAnimation(collectionCutinId, true);
        }
        private async void LoadCollectionAnimation(int id, bool play)
        {
            int generation = collectionGeneration, request = ++collectionPlayGeneration;
            collectionPlaying = true;
            collectionPlayUntil = float.PositiveInfinity;
            collectionLoadStarted = Time.unscaledTime;
            collectionPreviewLoaded = collectionPreviewError = false;
            CollectionPreviewStatus("正在加载动画…");
            GameObject model = null;
            try
            {
                await UniTask.NextFrame();
                if (!CollectionRequestCurrent(generation) || request != collectionPlayGeneration) return;
                // Instantiate under an inactive parent so ScreenEffect cannot change the live duel cameras.
                var prefab = collectionNativeCutins.Contains(id) ? await ABLoader.LoadMonsterCutinAsync(id, true, false)
                    : await ABLoader.LoadFromFileAsync("MonsterCutin2/" + id, true, false);
                if (!CollectionRequestCurrent(generation) || request != collectionPlayGeneration) return;
                if (prefab == null) throw new InvalidOperationException("动画资源无法加载");
                ClearCollectionModel();
                collectionModel = new GameObject("Summon preview"); collectionModel.transform.SetParent(collectionStage.transform, false);
                collectionModel.SetActive(false);
                ApplyCollectionCutinCamera(prefab);
                model = Instantiate(prefab, collectionModel.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.SetActive(true);
                PrepareCollectionObject(model);
                // Attribute effects use the same bundled art as the real summon sequence.
                var card = CardsManager.GetCard(id);
                string background = card.IsAttribute(CardAttribute.Dark) ? "SummonMonster_Bgdak_S2" : card.IsAttribute(CardAttribute.Light) ? "SummonMonster_Bglit_S2"
                    : card.IsAttribute(CardAttribute.Earth) ? "SummonMonster_Bgeah_S2" : card.IsAttribute(CardAttribute.Water) ? "SummonMonster_Bgwtr_S2"
                    : card.IsAttribute(CardAttribute.Fire) ? "SummonMonster_Bgfie_S2" : card.IsAttribute(CardAttribute.Wind) ? "SummonMonster_Bgwid_S2" : "SummonMonster_Bgdve_S2";
                AddCollectionCutinEffect(background);
                AddCollectionCutinEffect("SummonMonster_Thunder_power");
                collectionModel.SetActive(true);
                await UniTask.NextFrame();
                if (!CollectionRequestCurrent(generation) || request != collectionPlayGeneration) return;
                var directors = collectionModel.GetComponentsInChildren<PlayableDirector>(true);
                if (directors.Length == 0 || model.GetComponentsInChildren<Renderer>(true).Length == 0)
                    throw new InvalidOperationException("动画资源缺少可预览的时间线或渲染组件");
                foreach (var director in directors)
                {
                    director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
                    director.extrapolationMode = DirectorWrapMode.Hold;
                    if (play) { director.time = 0; director.Play(); }
                    else { director.time = .8; director.Evaluate(); director.Pause(); }
                }
                if (!play) foreach (var particles in collectionModel.GetComponentsInChildren<ParticleSystem>(true)) particles.Simulate(.8f, false, true, true);
                // Timeline activation and skeleton meshes settle on the following rendered frame.
                await UniTask.NextFrame();
                if (!CollectionRequestCurrent(generation) || request != collectionPlayGeneration) return;
                PrepareCollectionObject(collectionModel);
                CollectionPreviewStatus(null);
                collectionPlaying = play;
                collectionPlayUntil = Time.unscaledTime + CutinViewer.CUTIN_PLAY_TIME;
                collectionNextPlay = Time.unscaledTime + CutinViewer.CUTIN_PLAY_TIME + 1.4f;
            }
            catch (Exception e)
            {
                if (model != null && (collectionModel == null || !model.transform.IsChildOf(collectionModel.transform))) Destroy(model);
                if (!CollectionRequestCurrent(generation) || request != collectionPlayGeneration) return;
                collectionPlaying = false; collectionNextPlay = Time.unscaledTime + 4;
                CollectionPreviewStatus("动画加载失败 · 可选择其他资源", true); Debug.LogWarning("[FightCollection] " + e);
            }
        }
        private void ApplyCollectionCutinCamera(GameObject prefab)
        {
            var setting = prefab.GetComponentsInChildren<ScreenEffect>(true).LastOrDefault(e => e.cameraViewType == ScreenEffect.ViewType.View2D && e.camera2DSize > 0);
            if (setting == null) return;
            collectionCamera.orthographic = true;
            collectionCamera.orthographicSize = setting.camera2DSize;
            collectionCamera.transform.localPosition = setting.cameraPosition;
            collectionCamera.transform.localEulerAngles = setting.cameraAngle;
        }
        private void AddCollectionCutinEffect(string name)
        {
            if (!ABLoader.mdCached) return;
            // The core helper instantiates in the live scene. Reuse its cached bundle while keeping this clone inactive until prepared.
            var bundle = typeof(ABLoader).GetField("mdBundleOutDuel", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as AssetBundle;
            var prefab = bundle == null ? null : bundle.LoadAsset<GameObject>(name);
            if (prefab == null) return;
            var effect = Instantiate(prefab, collectionModel.transform, false); PrepareCollectionObject(effect);
        }

        private void RenderAppearance()
        {
            EnsureAppearanceDraft();
            appearanceScopeBar = FightUI.Node(Body, "Appearance scope toolbar", 0, CollectionContentTop, BodyWidth, 44);
            for (int i = 0; i < AppearanceConditions.Length; i++)
            {
                string condition = AppearanceConditions[i];
                var button = FightUI.Button(appearanceScopeBar, AppearanceConditionNames[i], i * 104, 0, 92, 42, () => ChangeAppearanceSlot(condition, appearancePlayer));
                StyleAppearanceScopeButton(button, condition == appearanceCondition);
                appearanceScopeButtons.Add(button);
            }
            for (int i = 0; i < 2; i++)
            {
                int player = i; var button = FightUI.Button(appearanceScopeBar, "玩家 " + (i + 1), 344 + i * 116, 0, 104, 42, () => ChangeAppearanceSlot(appearanceCondition, player));
                StyleAppearanceScopeButton(button, player == appearancePlayer);
                appearanceScopeButtons.Add(button);
            }
            appearanceOverrideButton = FightUI.FocusKey(FightUI.SwitchRow(appearanceScopeBar, "覆盖卡组外观 · 全局", appearanceDraft.Override, BodyWidth - 280, 0, 280, 42,
                () => { appearanceDraft.Override = !appearanceDraft.Override; Rebuild(); }), "collection:appearance:override");
            FightUI.Hint(appearanceOverrideButton.gameObject, "全局开关。启用后，对战玩家 1 使用这里配置的卡套、场地、墓地、伙伴台座与伙伴；关闭时使用卡组自带外观。应用后生效。");
            float y = AppearanceContentTop, footerH = 84, contentH = Mathf.Max(220, BodyHeight - y - footerH);
            float sideW = AppearanceSideWidth, previewW = AppearancePreviewWidth, gridX = sideW + Gap, gridW = BodyWidth - gridX - previewW - Gap;
            var sidebar = FightUI.Panel(Body, 0, y, sideW, contentH);
            librarySidePanel = sidebar;
            const float rowH = 46;
            var categoryList = FightUI.Scroll(sidebar, 0, 0, sideW, contentH, (AppearanceCategories.Length + 1) * rowH + 24, out var categoryScroll);
            // A fitting desktop list reports normalized position zero; restoring it at 720p would scroll this list to the bottom.
            categoryScroll.name = "Appearance category navigation";
            for (int i = 0; i < AppearanceCategories.Length; i++)
            {
                string category = AppearanceCategories[i]; bool selected = category == appearanceCategory;
                var button = LibrarySelection(categoryList, 8, 10 + i * rowH, sideW - 16, rowH - 2, selected, () => { appearanceCategory = category; appearanceSearch = ""; Rebuild(); }, false);
                FightUI.FocusKey(button, "collection:appearance:category:" + category);
                FightUI.Text(button.transform, AppearanceCategoryNames[i], 14, 0, sideW - 40, rowH - 2, 17, selected ? FightUI.Active : FightUI.Muted, TextAlignmentOptions.MidlineLeft);
                var indicator = FightUI.Icon(button.transform, "diamond", sideW - 32, 18, 7, 7, FightUI.Active);
                indicator.gameObject.SetActive(AppearanceCategoryDirty(category)); appearanceCategoryIndicators[category] = indicator;
            }
            float revealCategory = Mathf.Max(0, (Array.IndexOf(AppearanceCategories, appearanceCategory) + 1) * rowH + 12 - contentH);
            categoryList.anchoredPosition = new Vector2(0, Mathf.Ceil(revealCategory / rowH) * rowH);
            var middle = FightUI.Panel(Body, gridX, y, gridW, contentH);
            libraryLeftPanel = middle;
            RenderAppearanceOptions(middle, gridW, contentH);
            var right = FightUI.Panel(Body, BodyWidth - previewW, y, previewW, contentH);
            libraryRightPanel = right;
            RenderAppearancePreview(right, previewW, contentH);
            appearanceApplyBar = FightUI.Panel(Body, 0, BodyHeight - 68, BodyWidth, 68, FightUI.Raised, false, "Appearance apply bar");
            FightUI.Line(appearanceApplyBar, 16, 0, BodyWidth - 32, FightUI.Border);
            appearanceSyncButton = FightUI.FocusKey(FightUI.SwitchRow(appearanceApplyBar, "同步到其他情境", appearanceSync, 16, 12, 224, 44,
                () => { appearanceSync = !appearanceSync; Rebuild(); }), "collection:appearance:sync");
            FightUI.Hint(appearanceSyncButton.gameObject, "应用时，将本次修改同步到其他情境的同一玩家槽位。角色语音开关影响该情境全部玩家；壁纸和覆盖卡组外观为全局设置。");
            appearanceDirtyLabel = FightUI.Text(appearanceApplyBar, "", 260, 19, BodyWidth - 732, 30, 16, FightUI.Active);
            appearanceDirtyLabel.enableWordWrapping = false; appearanceDirtyLabel.overflowMode = TextOverflowModes.Ellipsis;
            appearanceReviewButton = FightUI.FocusKey(FightUI.Button(appearanceApplyBar, "查看修改", BodyWidth - 452, 12, 112, 44,
                ShowAppearanceChanges, false, AppearanceDirty), "collection:appearance:review");
            FightUI.Hint(appearanceReviewButton.gameObject, "查看本次各项修改的原值、新值和应用范围。");
            appearanceDiscardButton = FightUI.Button(appearanceApplyBar, "取消修改", BodyWidth - 328, 12, 140, 44, () => { DiscardAppearance(); Rebuild(); }, false, AppearanceDirty);
            appearanceApplyButton = FightUI.Button(appearanceApplyBar, "应用外观", BodyWidth - 176, 12, 160, 44, () => { if (SaveAppearance()) Rebuild(); }, true, AppearanceDirty);
            UpdateAppearanceDirtyUI();
        }
        private void ReflowAppearanceBars()
        {
            if (appearanceScopeBar != null)
            {
                FightUI.Place(appearanceScopeBar, 0, CollectionContentTop, BodyWidth, 44);
                for (int i = 0; i < appearanceScopeButtons.Count; i++)
                {
                    var rect = appearanceScopeButtons[i].GetComponent<RectTransform>();
                    FightUI.Place(rect, i < 3 ? i * 104 : 344 + (i - 3) * 116, 0, i < 3 ? 92 : 104, 42);
                    ReflowLibraryControl(rect);
                }
                if (appearanceOverrideButton != null)
                {
                    var rect = appearanceOverrideButton.GetComponent<RectTransform>();
                    FightUI.Place(rect, BodyWidth - 280, 0, 280, 42); ReflowLibraryControl(rect);
                }
            }
            if (appearanceApplyBar == null) return;
            FightUI.Place(appearanceApplyBar, 0, BodyHeight - 68, BodyWidth, 68);
            if (appearanceSyncButton != null)
            {
                var rect = appearanceSyncButton.GetComponent<RectTransform>();
                FightUI.Place(rect, 16, 12, 224, 44); ReflowLibraryControl(rect);
            }
            if (appearanceDirtyLabel != null) FightUI.Place(appearanceDirtyLabel.rectTransform, 260, 19, BodyWidth - 732, 30);
            if (appearanceReviewButton != null) FightUI.Place(appearanceReviewButton.GetComponent<RectTransform>(), BodyWidth - 452, 12, 112, 44);
            if (appearanceDiscardButton != null) FightUI.Place(appearanceDiscardButton.GetComponent<RectTransform>(), BodyWidth - 328, 12, 140, 44);
            if (appearanceApplyButton != null) FightUI.Place(appearanceApplyButton.GetComponent<RectTransform>(), BodyWidth - 176, 12, 160, 44);
        }
        private void UpdateAppearanceDirtyUI()
        {
            if (appearanceApplyButton != null)
            {
                appearanceApplyButton.interactable = AppearanceDirty;
                var caption = appearanceApplyButton.GetComponentInChildren<TextMeshProUGUI>();
                if (caption != null) caption.text = appearanceSaveError == null ? "应用外观" : "重试应用";
                FightUI.Hint(appearanceApplyButton.gameObject, appearanceSaveError == null
                    ? "保存本次修改并应用到所示范围。" : "使用已保留的外观草稿再次保存。" + appearanceSaveError);
            }
            if (appearanceDiscardButton != null) appearanceDiscardButton.interactable = AppearanceDirty;
            if (appearanceReviewButton != null) appearanceReviewButton.interactable = AppearanceDirty;
            if (appearanceRestoreButton != null) appearanceRestoreButton.interactable = AppearanceCategoryDirty(appearanceCategory);
            if (appearanceDirtyLabel != null)
            {
                int changes = appearanceDraft == null ? 0 : appearanceDraft.Values.Count(v => v.Value != appearanceDraft.Original[v.Key])
                    + (appearanceDraft.Voice != appearanceDraft.OriginalVoice ? 1 : 0) + (appearanceDraft.Override != appearanceDraft.OriginalOverride ? 1 : 0);
                appearanceDirtyLabel.text = appearanceSaveError != null ? "应用失败 · 修改已保留"
                    : AppearanceDirty ? "● " + changes + " 项未应用 · " + AppearanceChangeScope(true) : "已应用 · " + AppearanceCategoryScope(appearanceCategory, false);
                appearanceDirtyLabel.color = appearanceSaveError != null ? FightUI.Danger : AppearanceDirty ? FightUI.Active : FightUI.Muted;
                FightUI.Hint(appearanceDirtyLabel.gameObject, appearanceSaveError ?? "应用范围：" + (AppearanceDirty ? AppearanceChangeScope() : AppearanceCategoryScope(appearanceCategory, false)) + "。仅保存本次修改的项目。");
            }
            if (libraryPreviewBadge != null)
            {
                bool pending = AppearanceCategoryDirty(appearanceCategory);
                libraryPreviewBadge.text = appearanceCategory == "Case" ? "预览" : pending ? "待应用" : "已应用";
                libraryPreviewBadge.color = pending ? FightUI.Active : FightUI.Muted;
                FightUI.Hint(libraryPreviewBadge.gameObject, appearanceCategory == "Case" ? "卡盒随卡组保存，此处显示预览。"
                    : (pending ? "待应用：" : "已应用：") + AppearanceCategoryScope(appearanceCategory, pending) + (pending ? "。点击应用外观后保存。" : "。"));
            }
            foreach (var category in appearanceCategoryIndicators)
                if (category.Value != null) category.Value.gameObject.SetActive(AppearanceCategoryDirty(category.Key));
            if (appearanceCategory == "PlayerName" && libraryCaption != null && appearanceDraft != null)
            {
                var name = appearanceDraft.Values["PlayerName"];
                libraryCaption.text = string.IsNullOrEmpty(name) ? FightText.Get("使用默认名称") : name;
                FightUI.RawHint(libraryCaption.gameObject, libraryCaption.text);
                ReflowLibraryPreview(libraryRightPanel.rect.width, libraryRightPanel.rect.height);
            }
        }
        private void EnsureAppearanceDraft()
        {
            if (appearanceDraft != null) return;
            appearanceSaveError = null;
            appearanceDraft = new AppearanceDraft();
            foreach (var category in AppearanceCategories.Where(c => c != "Case"))
            {
                string key = category == "Wallpaper" ? "Wallpaper" : appearanceCondition + category + appearancePlayer;
                string fallback = category == "PlayerName" ? "" : category == "Character" ? VoicePlayer.defaultCharacter : DefaultAppearanceId(category);
                string value = ReadAppearanceConfig(key, fallback);
                appearanceDraft.Values[category] = value; appearanceDraft.Original[category] = value;
            }
            appearanceDraft.Voice = appearanceDraft.OriginalVoice = ReadAppearanceConfig(appearanceCondition + "Voice", "0") == "1";
            appearanceDraft.Override = appearanceDraft.OriginalOverride = ReadAppearanceConfig("OverrideDeckAppearance", "0") == "1";
        }
        private static string ReadAppearanceConfig(string key, string fallback) => Config.Have(key) ? Config.Get(key, fallback) : fallback;
        private string DefaultAppearanceId(string category)
        {
            var items = CollectionItems(AppearanceItemType(category));
            var first = items?.FirstOrDefault(i => i.id > 9999);
            return first.HasValue && first.Value.id != 0 ? first.Value.id.ToString() : "0";
        }
        private void ChangeAppearanceSlot(string condition, int player)
        {
            if (condition == appearanceCondition && player == appearancePlayer) return;
            Action change = () => { appearanceCondition = condition; appearancePlayer = player; appearanceDraft = null; appearanceSync = false; appearanceSearch = ""; Rebuild(); };
            if (!AppearanceDirty) { change(); return; }
            AppearanceUnsavedDialog(change);
        }
        internal bool TryLeaveAppearance(string next)
        {
            if (page != "appearance" || next == "appearance" || !AppearanceDirty) return true;
            AppearanceUnsavedDialog(() => NavigateNow(next)); return false;
        }
        private void AppearanceUnsavedDialog(Action continued)
        {
            var box = ShowDialog("外观尚未应用", 650, 250);
            var scope = FightUI.Text(box, "应用范围：" + AppearanceChangeScope(), 28, 84, 590, 72, 18);
            scope.enableWordWrapping = true;
            FightUI.Button(box, "继续编辑", 28, 172, 164, 48, CloseDialog);
            var discard = FightUI.Button(box, "放弃修改", 208, 172, 164, 48, () => { DiscardAppearance(); CloseDialog(); continued(); });
            FightUI.StyleDestructiveButton(discard);
            FightUI.Button(box, "应用并继续", 388, 172, 234, 48, () => { if (SaveAppearance()) { CloseDialog(); continued(); } }, true);
        }
        internal void DiscardAppearance() { appearanceDraft = null; appearanceSync = false; EnsureAppearanceDraft(); }
        private void ShowAppearanceChanges()
        {
            if (!AppearanceDirty) return;
            float width = Mathf.Min(760, W - 48), height = Mathf.Min(600, H - 48), inner = width - 56;
            var box = ShowDialog("本次外观修改", width, height);
            var intro = FightUI.Text(box, "应用范围：" + AppearanceChangeScope(), 28, 80, inner, 56, 17, FightUI.Muted);
            intro.enableWordWrapping = true;
            float introHeight = Mathf.Max(40, intro.GetPreferredValues(intro.text, inner, float.PositiveInfinity).y + 4);
            intro.rectTransform.sizeDelta = new Vector2(inner, introHeight);
            float listTop = 80 + introHeight + 12, listHeight = height - listTop - 28;
            var content = FightUI.Scroll(box, 28, listTop, inner, listHeight, listHeight, out var scroll);
            scroll.name = "Appearance change review navigation";
            float y = 0;
            foreach (string category in AppearanceCategories.Where(category => category != "Case"))
            {
                if (appearanceDraft.Values[category] == appearanceDraft.Original[category]) continue;
                RenderAppearanceChange(content, inner, category, AppearanceCategoryNames[Array.IndexOf(AppearanceCategories, category)],
                    AppearanceValueName(category, appearanceDraft.Original[category]), AppearanceValueName(category, appearanceDraft.Values[category]),
                    category == "Wallpaper" ? "全局菜单壁纸" : AppearanceSlotChangeScope(), ref y);
            }
            if (appearanceDraft.Voice != appearanceDraft.OriginalVoice)
                RenderAppearanceChange(content, inner, "Voice", "角色语音", appearanceDraft.OriginalVoice ? "开启" : "关闭",
                    appearanceDraft.Voice ? "开启" : "关闭", AppearanceVoiceChangeScope(), ref y);
            if (appearanceDraft.Override != appearanceDraft.OriginalOverride)
                RenderAppearanceChange(content, inner, "Override", "覆盖卡组外观", appearanceDraft.OriginalOverride ? "开启" : "关闭",
                    appearanceDraft.Override ? "开启" : "关闭", "全局覆盖卡组外观", ref y);
            content.sizeDelta = new Vector2(inner, Mathf.Max(listHeight, y));
        }
        private string AppearanceSlotChangeScope() => (appearanceSync ? "对战、观战、回放"
            : AppearanceConditionNames[Array.IndexOf(AppearanceConditions, appearanceCondition)]) + " · 玩家 " + (appearancePlayer + 1);
        private string AppearanceVoiceChangeScope() => (appearanceSync ? "对战、观战、回放"
            : AppearanceConditionNames[Array.IndexOf(AppearanceConditions, appearanceCondition)]) + " · 全部玩家";
        private void RenderAppearanceChange(Transform parent, float width, string key, string label, string original, string next, string scope, ref float y)
        {
            var row = FightUI.Node(parent, "Appearance review row " + key, 0, y, width - 20, 100);
            var title = FightUI.Text(row, label + " · " + scope, 0, 0, width - 20, 32, 18, FightUI.Gold);
            title.enableWordWrapping = true;
            title.rectTransform.sizeDelta = new Vector2(width - 20, Mathf.Max(32, title.GetPreferredValues(title.text, width - 20, float.PositiveInfinity).y + 4));
            var values = FightUI.Text(row, "已应用：" + original + "\n待应用：" + next, 0, title.rectTransform.sizeDelta.y + 8, width - 20, 56, 17);
            values.name = "Appearance review values " + key;
            values.richText = false; values.enableWordWrapping = true;
            values.rectTransform.sizeDelta = new Vector2(width - 20, values.GetPreferredValues(values.text, width - 20, float.PositiveInfinity).y + 4);
            float rowHeight = title.rectTransform.sizeDelta.y + values.rectTransform.sizeDelta.y + 26;
            row.sizeDelta = new Vector2(width - 20, rowHeight);
            FightUI.Line(row, 0, rowHeight - 8, width - 20, FightUI.Border);
            y += rowHeight + 12;
        }
        private string AppearanceValueName(string category, string value)
        {
            if (category == "PlayerName") return string.IsNullOrEmpty(value) ? "使用默认名称" : FightText.Keep(value);
            if (category == "Character") return value == "0" ? "使用默认角色" : FightText.Keep(CharacterName(value)) + "（" + value + "）";
            if (value == "0") return AppearanceAllowsNone(category) ? "不显示" : category == "Field" ? "使用默认场地"
                : category == "Grave" ? "使用默认墓地外观" : "使用默认外观";
            if (value == Items.CODE_SAME.ToString()) return "与场地一致";
            if (value == Items.CODE_RANDOM.ToString()) return "随机外观";
            var item = CollectionItems(AppearanceItemType(category)).FirstOrDefault(entry => entry.id.ToString() == value);
            if (item.id == 0) return "素材未安装（" + value + "）";
            string name = item.name;
            return string.IsNullOrWhiteSpace(name) || name.Trim().Equals(Items.STRING_NULL, StringComparison.OrdinalIgnoreCase)
                ? "名称未提供（" + value + "）" : FightText.Keep(name) + "（" + value + "）";
        }
        internal bool SaveAppearance()
        {
            if (appearanceDraft == null || !appearanceDraft.Dirty) return true;
            try
            {
                string scope = AppearanceChangeScope();
                var changed = appearanceDraft.Values.Where(v => v.Value != appearanceDraft.Original[v.Key]).ToArray();
                var updates = new Dictionary<string, string>();
                foreach (var value in changed)
                {
                    if (value.Key == "Wallpaper") updates["Wallpaper"] = value.Value;
                    else foreach (string condition in appearanceSync ? AppearanceConditions : new[] { appearanceCondition })
                        updates[condition + value.Key + appearancePlayer] = string.IsNullOrEmpty(value.Value) ? Config.EMPTY_STRING : value.Value;
                }
                if (appearanceDraft.Voice != appearanceDraft.OriginalVoice)
                    foreach (string condition in appearanceSync ? AppearanceConditions : new[] { appearanceCondition }) updates[condition + "Voice"] = appearanceDraft.Voice ? "1" : "0";
                if (appearanceDraft.Override != appearanceDraft.OriginalOverride) updates["OverrideDeckAppearance"] = appearanceDraft.Override ? "1" : "0";
                Config.SaveValues(updates);
                if (changed.Any(v => v.Key == "Wallpaper")) { UIManager.currentWallpaper = appearanceDraft.Values["Wallpaper"]; Program.instance.ui_.ChangeWallpaper(UIManager.currentWallpaper); }
                if (changed.Any(v => v.Key == "Face" || v.Key == "Frame" || v.Key == "Protector")) RefreshAppearanceAssets();
                appearanceDraft = null; EnsureAppearanceDraft(); ShowStatus("外观已应用 · " + scope); return true;
            }
            catch (Exception e) { appearanceSaveError = "外观保存失败：" + e.Message; UpdateAppearanceDirtyUI(); ShowStatus(appearanceSaveError); return false; }
        }
        private async void RefreshAppearanceAssets()
        {
            try { await Program.instance.appearance.LoadSettingAssets(); }
            catch (Exception e) { Debug.LogWarning("[FightCollection] refresh: " + e); ShowStatus("配置已保存，外观资源将在下次加载时刷新"); }
        }
        private string AppearanceCategoryScope(string category, bool pending)
        {
            if (category == "Wallpaper") return "全局菜单壁纸";
            if (category == "Case") return "卡组中的卡盒";
            string conditions = pending && appearanceSync ? "对战、观战、回放" : AppearanceConditionNames[Array.IndexOf(AppearanceConditions, appearanceCondition)];
            if (category == "Character" && pending && appearanceDraft != null && appearanceDraft.Voice != appearanceDraft.OriginalVoice)
            {
                if (appearanceDraft.Values["Character"] == appearanceDraft.Original["Character"]) return conditions + " · 全部玩家的角色语音";
                return conditions + " · 玩家 " + (appearancePlayer + 1) + "；语音影响全部玩家";
            }
            return conditions + (category == "Voice" ? " · 全部玩家" : " · 玩家 " + (appearancePlayer + 1));
        }
        private string AppearanceChangeScope(bool compact = false)
        {
            if (!AppearanceDirty) return AppearanceCategoryScope(appearanceCategory, false);
            bool slot = appearanceDraft.Values.Any(v => v.Key != "Wallpaper" && v.Value != appearanceDraft.Original[v.Key]);
            bool voice = appearanceDraft.Voice != appearanceDraft.OriginalVoice;
            bool wallpaper = AppearanceCategoryDirty("Wallpaper"), overwrite = appearanceDraft.Override != appearanceDraft.OriginalOverride;
            var scopes = new List<string>();
            if (slot || voice)
            {
                string conditions = appearanceSync ? (compact ? "全部情境" : "对战、观战、回放") : AppearanceConditionNames[Array.IndexOf(AppearanceConditions, appearanceCondition)];
                scopes.Add(conditions + (slot ? " · 玩家 " + (appearancePlayer + 1) : " · 全部玩家"));
                if (slot && voice) scopes.Add(compact ? "语音全部玩家" : "上述情境的角色语音影响全部玩家");
            }
            if (wallpaper || overwrite)
            {
                if (compact && (slot || voice || wallpaper && overwrite)) scopes.Add("全局");
                else
                {
                    if (wallpaper) scopes.Add("全局菜单壁纸");
                    if (overwrite) scopes.Add("全局覆盖卡组外观");
                }
            }
            return string.Join(compact ? " + " : "；", scopes);
        }
        private bool AppearanceCategoryDirty(string category)
        {
            if (appearanceDraft == null) return false;
            bool changed = appearanceDraft.Values.TryGetValue(category, out var current) && appearanceDraft.Original.TryGetValue(category, out var original) && current != original;
            return changed || category == "Character" && appearanceDraft.Voice != appearanceDraft.OriginalVoice;
        }
        private void AddAppearanceResourceState(Button button, string category, string id, bool selected, float width)
        {
            bool applied = category != "Case" && appearanceDraft.Original.TryGetValue(category, out var original) && original == id;
            if (!selected && !applied) return;
            string state = category == "Case" ? "预览" : applied ? "已应用" : "待应用";
            var badge = FightUI.Panel(button.transform, width - 68, 8, 60, 22, new Color(.025f, .05f, .07f, .94f), false, "Resource selection state");
            badge.GetComponent<Image>().raycastTarget = false;
            FightUI.Text(badge, state, 2, 0, 56, 22, 12, selected && !applied ? FightUI.Active : FightUI.Muted, TextAlignmentOptions.Center);
            string scope = AppearanceCategoryScope(category, !applied);
            FightUI.Hint(button.gameObject, state + " · " + scope + (category == "Case" ? "。卡盒在卡组中保存。" : "。选择后点击应用外观保存。"));
        }
        private void RenderAppearanceOptions(RectTransform middle, float width, float height)
        {
            string label = AppearanceCategoryNames[Array.IndexOf(AppearanceCategories, appearanceCategory)];
            var heading = FightUI.Text(middle, label, 16, 14, width - 240, 34, 24, FightUI.Gold);
            appearanceCatalogTitle = heading;
            heading.name = "Appearance catalog title";
            heading.fontStyle = FontStyles.Bold; CollectionCatalogDivider(middle, width);
            appearanceRestoreButton = FightUI.FocusKey(FightUI.Button(middle, "还原此项", width - 224, 14, 96, 32,
                RestoreCurrentAppearance, false, AppearanceCategoryDirty(appearanceCategory)), "collection:appearance:restore-current");
            FightUI.Hint(appearanceRestoreButton.gameObject, appearanceCategory == "Character"
                ? "只还原当前情境、玩家的已应用角色和该情境的语音开关，保留其他外观修改。"
                : "只还原当前类别的已应用设置，保留其他外观修改。");
            appearanceLocateButton = appearanceCategory == "PlayerName" ? null : FightUI.FocusKey(FightUI.Button(middle, "定位选择", width - 116, 14, 96, 32,
                LocateCurrentAppearance), "collection:appearance:locate-current");
            if (appearanceLocateButton != null) FightUI.Hint(appearanceLocateButton.gameObject, "回到当前预览对应的目录条目；清除筛选并将它滚动到可见位置。");
            ReflowAppearanceCatalogActions(width);
            if (appearanceCategory == "PlayerName")
            {
                FightUI.Text(middle, appearancePlayer == 0 ? "留空使用原有名称" : "留空使用真实名称", 16, 128, width - 32, 32, 16, FightUI.Muted);
                FightUI.Input(middle, appearanceDraft.Values["PlayerName"], "输入玩家名称", 16, 66, width - 32, 50, v => { appearanceDraft.Values["PlayerName"] = v; UpdateAppearanceDirtyUI(); });
                return;
            }
            if (appearanceCategory == "Character") { RenderAppearanceCharacters(middle, width, height); return; }
            CollectionSearchField(middle, appearanceSearch, "搜索素材名称或编号", 16, 56, width - 32, 42, v => { appearanceSearch = v; DrawAppearanceGrid(middle.rect.width, middle.rect.height); });
            collectionList = FightUI.Node(middle, "Appearance catalog", 0, 110, width, height - 110);
            DrawAppearanceGrid(width, height);
        }
        private void DrawAppearanceGrid(float width, float height)
        {
            if (collectionList == null) return;
            appearanceSelectedResource = null;
            BeginLibraryRefresh(appearanceScroll, appearanceCategory + "\t" + appearanceCondition + appearancePlayer + "\t" + appearanceSearch);
            FightUI.Clear(collectionList);
            var type = AppearanceItemType(appearanceCategory);
            var available = AvailableCollectionItems(type);
            var options = new List<Items.Item>();
            if (AppearanceAllowsNone(appearanceCategory)) options.Add(new Items.Item { id = 0, name = "不显示", description = "隐藏此项外观。" });
            if (appearanceCategory == "Grave" || appearanceCategory == "Stand") options.Add(new Items.Item { id = Items.CODE_SAME, name = "与场地一致", description = "使用当前场地对应的外观。" });
            options.AddRange(available);
            var items = options.Where(i => CollectionMatches(CollectionDisplayName(i), appearanceSearch, i.id.ToString())).ToList();
            int columns = LibraryGridColumns(width, 10);
            float cellW = (width - 32 - (columns - 1) * 10) / columns;
            float artH = Mathf.Min(appearanceCategory == "Protector" ? cellW * 1.43f : cellW, Mathf.Max(84, height - 190));
            float cellH = artH + 60;
            var content = FightUI.Scroll(collectionList, 8, 0, width - 16, height - 118, Mathf.Ceil(items.Count / (float)columns) * (cellH + 12) + 16, out var scroll);
            scroll.name = "Appearance resource navigation";
            if (items.Count == 0) LibraryEmpty(content, options.Count == 0 ? "尚未安装此类素材" : "没有匹配的素材", "", width - 16, height - 118, "diamond", options.Count == 0 ? (Action)null : ClearCollectionSearch);
            PopulateCollectionDirectory(content, items.Count, i =>
            {
                var item = items[i]; bool selected = appearanceCategory == "Case" ? item.id == appearanceCase : item.id.ToString() == appearanceDraft.Values[appearanceCategory];
                var b = LibrarySelection(content, 8 + i % columns * (cellW + 10), 8 + i / columns * (cellH + 12), cellW, cellH, selected,
                    () => { if (appearanceCategory == "Case") appearanceCase = item.id; else appearanceDraft.Values[appearanceCategory] = item.id.ToString(); Rebuild(); });
                FightUI.FocusKey(b, "collection:appearance:item:" + appearanceCategory + ":" + item.id);
                if (selected) appearanceSelectedResource = b;
                CollectionCardCaption(b.transform, cellW, artH, selected);
                float artW = appearanceCategory == "Protector" ? (artH - 14) / 1.43f : Mathf.Min(cellW - 14, artH - 14);
                CollectionItemImage(b.transform, item.id.ToString(), type, (cellW - artW) / 2, 7, artW, artH - 14);
                string displayName = CollectionDisplayName(item);
                var name = LibraryWrappedText(b.transform, displayName, 5, artH, cellW - 10, 52, 16, selected ? FightUI.Active : FightUI.Foreground, TextAlignmentOptions.Center, localize: false);
                FightUI.RawHint(name.gameObject, displayName);
                AddAppearanceResourceState(b, appearanceCategory, item.id.ToString(), selected, cellW);
            }, () => CompleteCollectionDirectory(scroll, appearanceScroll, width, height));
            CompleteLibraryRefresh(scroll, appearanceScroll);
            CaptureLibraryNodeLayouts();
        }

        private void ReflowAppearanceCatalogActions(float width)
        {
            var heading = libraryLeftPanel == null ? null : libraryLeftPanel.Find("Appearance catalog title") as RectTransform;
            if (heading != null) FightUI.Place(heading, 16, 14, width - 240, 34);
            if (appearanceRestoreButton != null)
            {
                FightUI.Place(appearanceRestoreButton.GetComponent<RectTransform>(), width - 224, 14, 96, 32);
                ReflowLibraryControl(appearanceRestoreButton.GetComponent<RectTransform>());
            }
            if (appearanceLocateButton != null)
            {
                FightUI.Place(appearanceLocateButton.GetComponent<RectTransform>(), width - 116, 14, 96, 32);
                ReflowLibraryControl(appearanceLocateButton.GetComponent<RectTransform>());
            }
        }

        private async void LocateCurrentAppearance()
        {
            if (page != "appearance" || collectionList == null || libraryLeftPanel == null) return;
            appearanceSearch = "";
            if (collectionSearchInput != null)
            {
                collectionSearchInput.SetTextWithoutNotify("");
                var clear = collectionSearchInput.transform.Find("Clear collection search");
                if (clear != null) clear.gameObject.SetActive(false);
            }
            if (appearanceCategory == "Character") DrawCharacterGrid(libraryLeftPanel.rect.width, libraryLeftPanel.rect.height);
            else DrawAppearanceGrid(libraryLeftPanel.rect.width, libraryLeftPanel.rect.height);
            int generation = collectionGeneration, request = collectionDirectoryGeneration;
            while (collectionDirectoryBuilding && CollectionIconRequestCurrent(generation) && request == collectionDirectoryGeneration)
                await UniTask.NextFrame();
            if (!CollectionIconRequestCurrent(generation) || request != collectionDirectoryGeneration) return;
            if (appearanceSelectedResource == null)
            {
                ShowStatus("当前选择未安装到目录，可选择其他素材。");
                return;
            }
            var directory = appearanceSelectedResource.GetComponentInParent<ScrollRect>();
            if (directory != null) directory.StopMovement();
            appearanceSelectedResource.Select();
            Canvas.ForceUpdateCanvases();
            RevealLibraryFocusedControl(appearanceSelectedResource.GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases();
        }

        private void RestoreCurrentAppearance()
        {
            if (appearanceDraft == null || !AppearanceCategoryDirty(appearanceCategory)) return;
            appearanceDraft.Values[appearanceCategory] = appearanceDraft.Original[appearanceCategory];
            if (appearanceCategory == "Character") appearanceDraft.Voice = appearanceDraft.OriginalVoice;
            appearanceSaveError = null;
            Rebuild();
            if (appearanceCategory != "PlayerName") LocateCurrentAppearance();
            ShowStatus("已还原本类已应用设置，其他修改仍保留。");
        }

        private void RenderAppearancePreview(RectTransform right, float width, float height)
        {
            string currentName = appearanceCategory == "PlayerName" ? appearanceDraft.Values["PlayerName"] : appearanceCategory == "Character" ? CharacterName(appearanceDraft.Values["Character"]) : SelectedAppearanceName();
            AddLibraryPreviewBadge(right, appearanceCategory == "Case" ? "预览" : AppearanceCategoryDirty(appearanceCategory) ? "待应用" : "已应用", width, AppearanceCategoryDirty(appearanceCategory));
            var caption = LibraryWrappedText(right, string.IsNullOrEmpty(currentName) ? FightText.Get("使用默认名称") : currentName, 16, 16, LibraryCaptionWidth(width), 104, 22, FightUI.Gold, localize: false);
            libraryCaption = caption;
            FightUI.RawHint(caption.gameObject, string.IsNullOrEmpty(currentName) ? FightText.Get("使用默认名称") : currentName);
            var stage = LibraryStageBounds(width, height);
            CreateCollectionStage(right, stage.x, stage.y, stage.width, stage.height, false);
            collectionCamera.orthographic = false; collectionCamera.fieldOfView = 40;
            collectionCamera.transform.position = CollectionOrigin + new Vector3(0, 95, -55);
            collectionCamera.transform.eulerAngles = new Vector3(62, 0, 0);
            if (appearanceCategory != "Mate")
            {
                libraryFaceOverlay = AddLibraryAppearanceOverlay(right, appearanceDraft.Values["Face"], Items.ItemType.Face, stage.x + 12, stage.y + 12, 58, 58);
                libraryFrameOverlay = AddLibraryAppearanceOverlay(right, appearanceDraft.Values["Frame"], Items.ItemType.Frame, stage.x + 8, stage.y + 8, 66, 66);
                float sleeveH = Mathf.Min(112, stage.height - 16), sleeveW = sleeveH * .7f;
                librarySleeveOverlay = AddLibraryAppearanceOverlay(right, appearanceDraft.Values["Protector"], Items.ItemType.Protector, stage.xMax - sleeveW - 12, stage.yMax - sleeveH - 12, sleeveW, sleeveH, true);
            }
            // The status must also sit above the avatar and sleeve overlays while it is visible.
            if (collectionStatusBackdrop != null) collectionStatusBackdrop.transform.SetAsLastSibling();
            LoadAppearanceScene(collectionGeneration);
            float actionY = stage.yMax + 14;
            if (appearanceCategory == "Mate") appearanceMateButton = FightUI.Button(right, "互动", stage.xMax - 92, actionY, 92, 34,
                () => { if (collectionMate != null) collectionMate.Play(Mate.MateAction.Tap); }, false, false);
            if (appearanceCategory == "Case")
            {
                libraryPreviewAction = FightUI.Button(right, "卡组管理", 16, actionY, width - 32, 36, () => Navigate("decks")).GetComponent<RectTransform>();
                FightUI.Hint(libraryPreviewAction.gameObject, "卡盒随卡组保存；前往卡组管理修改。");
            }
        }
        private Items.Item SelectedAppearanceItem()
        {
            int.TryParse(appearanceCategory == "Case" ? appearanceCase.ToString() : appearanceDraft.Values[appearanceCategory], out int id);
            if (id == 0) return new Items.Item { name = AppearanceAllowsNone(appearanceCategory) ? "不显示" : appearanceCategory == "Field" ? "使用默认场地" : appearanceCategory == "Grave" ? "使用默认墓地外观" : "使用默认外观" };
            if (id == Items.CODE_SAME) return new Items.Item { id = id, name = "与场地一致" };
            if (id == Items.CODE_RANDOM) return new Items.Item { id = id, name = "随机外观" };
            var item = CollectionItems(AppearanceItemType(appearanceCategory)).FirstOrDefault(i => i.id == id);
            return item.id != 0 ? item : new Items.Item { id = id, name = "素材未安装（" + id + "）" };
        }
        private string SelectedAppearanceName()
        {
            string id = appearanceCategory == "Case" ? appearanceCase.ToString() : appearanceDraft.Values[appearanceCategory];
            // Item.name localizes ID 0 as "none" and would overwrite these category-specific meanings.
            if (id == "0" && !AppearanceAllowsNone(appearanceCategory))
                return appearanceCategory == "Field" ? "使用默认场地" : appearanceCategory == "Grave" ? "使用默认墓地外观" : "使用默认外观";
            return CollectionDisplayName(SelectedAppearanceItem());
        }
        private static string CollectionDisplayName(Items.Item item)
        {
            if (item.id == 0) return "不显示";
            if (item.id == Items.CODE_SAME) return "与场地一致";
            if (item.id == Items.CODE_RANDOM) return "随机外观";
            string name = item.name;
            return string.IsNullOrWhiteSpace(name) || name.Trim().Equals(Items.STRING_NULL, StringComparison.OrdinalIgnoreCase)
                ? "名称未提供（" + item.id + "）" : name;
        }
        private async void LoadAppearanceScene(int generation)
        {
            GameObject field = null;
            try
            {
                await UniTask.NextFrame();
                if (!CollectionRequestCurrent(generation)) return;
                string fieldPath = ConcreteAppearancePath("Field");
                if (!File.Exists(Program.root + "MasterDuel/" + fieldPath + "_near")) { CollectionPreviewStatus("场地资源尚未安装", true); return; }
                field = await ABLoader.LoadFromFileAsync("MasterDuel/" + fieldPath + "_near", true, true);
                if (!CollectionRequestCurrent(generation)) { if (field != null) Destroy(field); return; }
                if (field == null) throw new InvalidOperationException("场地资源无法加载");
                collectionModel = field; field.transform.SetParent(collectionStage.transform, false); PrepareCollectionObject(field);
                var unavailable = new List<string>();
                var graveAnchor = CollectionFind(field.transform, "POS_Grave_near");
                var standAnchor = CollectionFind(field.transform, "POS_AvatarStand_near");
                var mateAnchor = CollectionFind(field.transform, "POS_Avatar_near");
                await LoadAppearancePart("Grave", graveAnchor, generation, unavailable);
                if (!CollectionRequestCurrent(generation)) return;
                var stand = await LoadAppearancePart("Stand", standAnchor, generation, unavailable);
                if (!CollectionRequestCurrent(generation)) return;
                if (stand != null) mateAnchor = CollectionFind(stand.transform, "POS_Avatar_near") ?? mateAnchor;
                int.TryParse(appearanceDraft.Values["Mate"], out int mateId);
                if (mateId != 0 && mateAnchor != null && AvailableCollectionItems(Items.ItemType.Mate).Any(i => i.id == mateId))
                {
                    var mate = await LoadCollectionMateResource(mateId, generation);
                    if (!CollectionRequestCurrent(generation)) { if (mate != null) Destroy(mate.gameObject); return; }
                    if (mate == null) throw new InvalidOperationException("伙伴资源无法加载");
                    Transform previewAnchor = mateAnchor;
                    if (appearanceCategory == "Mate")
                    {
                        // Keep the field clone for the scene context, but give the inspected mate a stable stage.
                        var anchor = new GameObject("Mate appearance preview anchor");
                        anchor.transform.SetParent(collectionStage.transform, false);
                        previewAnchor = anchor.transform;
                    }
                    collectionMate = mate; mate.parent = previewAnchor; mate.transform.SetParent(previewAnchor, false);
                    if (appearanceCategory == "Mate") mate.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    mate.gameObject.SetActive(true); PrepareCollectionObject(mate.gameObject);
                    await UniTask.NextFrame(); if (!CollectionRequestCurrent(generation)) return; mate.Play(Mate.MateAction.Entry);
                    if (appearanceCategory == "Mate")
                    {
                        HideAppearanceEnvironment();
                        // Entry updates the animator pose after its trigger; fit only after those updates have rendered.
                        await UniTask.NextFrame(); if (!CollectionRequestCurrent(generation)) return;
                        await UniTask.NextFrame(); if (!CollectionRequestCurrent(generation)) return;
                        collectionCamera.orthographic = true;
                        collectionCamera.transform.localEulerAngles = new Vector3(0, 180, 0);
                        HideAppearanceEnvironment();
                        if (!await PrepareCollectionMateFraming(mate, generation)) return;
                    }
                }
                else if (mateId != 0) unavailable.Add("伙伴");
                CollectionPreviewStatus(unavailable.Count == 0 ? null : string.Join("、", unavailable) + "预览不可用 · 可选择其他素材", unavailable.Count > 0);
            }
            catch (Exception e)
            {
                if (field != null && collectionModel != field) Destroy(field);
                if (CollectionRequestCurrent(generation)) { CollectionPreviewStatus("预览资源加载失败", true); Debug.LogWarning("[FightCollection] scene: " + e); }
            }
        }
        private async UniTask<GameObject> LoadAppearancePart(string category, Transform anchor, int generation, List<string> unavailable)
        {
            if (AppearanceAllowsNone(category) && appearanceDraft.Values[category] == "0") return null;
            string path = ConcreteAppearancePath(category);
            if (anchor == null || string.IsNullOrEmpty(path) || !File.Exists(Program.root + "MasterDuel/" + path + "_near"))
            { unavailable.Add(category == "Grave" ? "墓地外观" : "伙伴台座"); return null; }
            var part = await ABLoader.LoadFromFileAsync("MasterDuel/" + path + "_near", true, true);
            if (!CollectionRequestCurrent(generation) || anchor == null) { if (part != null) Destroy(part); return null; }
            if (part != null) { part.transform.SetParent(anchor, false); PrepareCollectionObject(part); Tools.PlayAnimation(part.transform, "StartToPhase1"); }
            else unavailable.Add(category == "Grave" ? "墓地外观" : "伙伴台座");
            return part;
        }
        private string ConcreteAppearancePath(string category)
        {
            var list = CollectionItems(AppearanceItemType(category));
            string id = appearanceDraft.Values[category];
            if (id == "0")
            {
                if (AppearanceAllowsNone(category)) return "";
                id = DefaultAppearanceId(category);
            }
            if (id == Items.CODE_SAME.ToString())
            {
                string field = appearanceDraft.Values["Field"];
                if (field == "0") field = DefaultAppearanceId("Field");
                id = Program.items.GetSameCode(AppearanceItemType(category), field);
            }
            var match = list.FirstOrDefault(i => i.id.ToString() == id);
            return match.id != 0 ? match.path : "";
        }
        private static bool AppearanceAllowsNone(string category) => category == "Mate" || category == "Stand" || category == "Wallpaper";

        private void RenderAppearanceCharacters(RectTransform middle, float width, float height)
        {
            float controlW = (width - 44) / 2;
            var voice = FightUI.FocusKey(FightUI.SwitchRow(middle, "角色语音", appearanceDraft.Voice, 16, 54, controlW, 32,
                () => { appearanceDraft.Voice = !appearanceDraft.Voice; Rebuild(); }), "collection:appearance:voice");
            FightUI.Hint(voice.gameObject, "应用后影响" + (appearanceSync ? "对战、观战、回放" : "当前情境") + "的全部玩家。试听不受此开关影响。");
            collectionVoicePath = FindCollectionVoice(appearanceDraft.Values["Character"]);
            collectionVoiceButton = FightUI.FocusKey(FightUI.Button(middle, "试听语音", 28 + controlW, 54, controlW, 32,
                () => PreviewCollectionVoice(collectionVoicePath), false, collectionVoicePath != null), "collection:appearance:voice-preview");
            RefreshCollectionVoiceAction();
            CollectionSearchField(middle, appearanceSearch, "搜索角色名称或编号", 16, 94, width - 32, 38, v => { appearanceSearch = v; DrawCharacterGrid(middle.rect.width, middle.rect.height); });
            collectionList = FightUI.Node(middle, "Character catalog", 0, LibraryListTop, width, height - LibraryListTop);
            DrawCharacterGrid(width, height);
        }
        private void DrawCharacterGrid(float width, float height)
        {
            appearanceSelectedResource = null;
            BeginLibraryRefresh(characterScroll, appearanceCondition + appearancePlayer + "\t" + appearanceSearch);
            FightUI.Clear(collectionList);
            var data = CharacterSelector.characters;
            if (data == null) { FightUI.Text(collectionList, "角色资源正在加载，请稍后重试。", 16, 20, width - 32, 64, 18, FightUI.Muted); return; }
            var available = Enumerable.Range(0, 10).SelectMany(i => data.GetSeriesCharacters(i.ToString("00"))).Where(c => !c.notReady && CollectionSpriteExists("sn" + c.id)).ToList();
            var characters = available.Where(c => CollectionMatches(data.GetName(c.id), appearanceSearch, c.id)).ToList();
            int columns = LibraryGridColumns(width, 10);
            float top = LibraryListTop, cellW = (width - 32 - (columns - 1) * 10) / columns;
            float viewportH = height - top - 8, captionH = CharacterCardCaptionHeight(viewportH);
            float portraitH = CharacterCardPortraitHeight(cellW, viewportH, captionH), cellH = portraitH + captionH;
            float captionSize = CharacterCardCaptionSize(viewportH);
            var content = FightUI.Scroll(collectionList, 8, 0, width - 16, height - top - 8, Mathf.Ceil(characters.Count / (float)columns) * (cellH + 12) + 16, out var scroll);
            scroll.name = "Character resource navigation";
            if (characters.Count == 0) LibraryEmpty(content, available.Count == 0 ? "尚未安装角色资源" : "没有匹配的角色", "", width - 16, height - top - 8, "diamond", available.Count == 0 ? (Action)null : ClearCollectionSearch);
            PopulateCollectionDirectory(content, characters.Count, i =>
            {
                var character = characters[i]; bool selected = character.id == appearanceDraft.Values["Character"];
                var button = LibrarySelection(content, 8 + i % columns * (cellW + 10), 8 + i / columns * (cellH + 12), cellW, cellH, selected, () => { appearanceDraft.Values["Character"] = character.id; Rebuild(); });
                FightUI.FocusKey(button, "collection:appearance:item:Character:" + character.id);
                if (selected) appearanceSelectedResource = button;
                CollectionCardCaption(button.transform, cellW, portraitH, selected, captionH);
                var image = CollectionBlankImage(button.transform, 7, 7, cellW - 14, portraitH - 14); LoadCollectionCharacterSprite(image, "sn" + character.id, collectionGeneration);
                string originalName = data.GetName(character.id);
                var name = LibraryWrappedText(button.transform, originalName, 5, portraitH, cellW - 10, captionH - 8, captionSize, selected ? FightUI.Active : FightUI.Foreground, TextAlignmentOptions.Center, localize: false);
                FightUI.RawHint(name.gameObject, originalName);
                AddAppearanceResourceState(button, "Character", character.id, selected, cellW);
            }, () => CompleteCollectionDirectory(scroll, characterScroll, width, height));
            CompleteLibraryRefresh(scroll, characterScroll);
            CaptureLibraryNodeLayouts();
        }
        private static string CharacterName(string id) => CharacterSelector.characters == null ? id : CharacterSelector.characters.GetName(id);
        private static string FindCollectionVoice(string id)
        {
            if (CharacterSelector.characters == null) return null;
            string original = CharacterSelector.characters.GetCharacterOriginalId(id), folder = "Sound/Voice/V" + original;
            if (!Directory.Exists(folder)) return null;
            return Directory.GetFiles(folder, "*.ogg").OrderBy(p => p, StringComparer.Ordinal).FirstOrDefault();
        }
        private async void PreviewCollectionVoice(string path)
        {
            if (path == null || collectionVoiceLoading) return;
            if (collectionVoiceSource != null && collectionVoiceSource.isPlaying)
            {
                collectionVoiceSource.Stop(); RefreshCollectionVoiceAction(); return;
            }
            int generation = collectionGeneration, request = ++collectionVoiceRequest;
            collectionVoiceLoading = true; collectionVoiceError = null; collectionVoiceLoadStarted = Time.unscaledTime;
            RefreshCollectionVoiceAction();
            AudioClip clip = null;
            try
            {
                clip = await AudioManager.LoadAudioFileUniAsync(path, AudioType.OGGVORBIS);
                if (!CollectionRequestCurrent(generation) || request != collectionVoiceRequest) { if (clip != null) Destroy(clip); return; }
                if (clip == null) throw new IOException("本地试听音频无法读取，请检查角色语音资源。");
                if (collectionVoiceSource == null) collectionVoiceSource = collectionStage.AddComponent<AudioSource>();
                collectionVoiceSource.Stop(); if (collectionVoiceClip != null) Destroy(collectionVoiceClip);
                collectionVoiceClip = clip; collectionVoiceSource.clip = clip; collectionVoiceSource.spatialBlend = 0; collectionVoiceSource.volume = 1; collectionVoiceSource.Play();
            }
            catch (Exception e)
            {
                if (clip != null && collectionVoiceClip != clip) Destroy(clip);
                if (CollectionRequestCurrent(generation) && request == collectionVoiceRequest)
                {
                    collectionVoiceError = "语音试听失败：" + e.Message; ShowStatus(collectionVoiceError);
                }
            }
            finally
            {
                if (CollectionRequestCurrent(generation) && request == collectionVoiceRequest)
                {
                    collectionVoiceLoading = false; RefreshCollectionVoiceAction();
                }
            }
        }
        private void RefreshCollectionVoiceAction()
        {
            if (collectionVoiceButton == null) return;
            bool playing = collectionVoiceSource != null && collectionVoiceSource.isPlaying;
            var caption = collectionVoiceButton.GetComponentInChildren<TextMeshProUGUI>();
            string label = collectionVoicePath == null ? "暂无试听音频" : collectionVoiceLoading ? "加载语音…"
                : playing ? "停止试听" : collectionVoiceError != null ? "重试试听" : "试听语音";
            if (caption != null)
            {
                if (caption.text != label) caption.text = label;
                caption.color = collectionVoiceError != null ? FightUI.Danger : playing ? FightUI.Active
                    : collectionVoicePath == null || collectionVoiceLoading ? FightUI.Muted : FightUI.Gold;
            }
            // Disabling a selected Unity Button clears EventSystem selection.
            // The loading guard rejects duplicate requests while retaining focus.
            collectionVoiceButton.interactable = collectionVoicePath != null;
            FightUI.Hint(collectionVoiceButton.gameObject, collectionVoicePath == null ? "尚未安装此角色的试听音频。"
                : collectionVoiceLoading ? "正在加载此角色的本地试听音频。" : playing ? "停止本次试听。角色语音开关在应用后影响对局。"
                : collectionVoiceError ?? "播放此角色的本地试听音频。试听不会改变角色语音开关。");
        }

        private void CreateCollectionStage(Transform parent, float x, float y, float width, float height, bool animation)
        {
            collectionPreviewLoaded = collectionPreviewError = collectionProtectorReady = collectionProtectorError = collectionPreviewNeedsProtector = false;
            collectionLoadStarted = Time.unscaledTime;
            var box = FightUI.Panel(parent, x, y, width, height, FightUI.Background, true, "Live resource preview");
            libraryStagePanel = box;
            if (!animation)
            {
                var backdrop = FightUI.Node(box, "Display stage", 1, 1, width - 2, height - 2).gameObject.AddComponent<FightCollectionBackdrop>();
                backdrop.raycastTarget = false;
            }
            var pixels = FightUI.Node(box, "Preview Image", 0, 0, width, height);
            collectionSurface = pixels.gameObject.AddComponent<RawImage>(); collectionSurface.raycastTarget = false;
            var parentRect = parent as RectTransform;
            var status = LibraryPreviewStatusBounds(parentRect == null ? width : parentRect.rect.width, new Rect(x, y, width, height));
            var statusBox = FightUI.Panel(parent, status.x, status.y, status.width, status.height, new Color(.015f, .035f, .055f, .96f), false, "Preview status backdrop");
            statusBox.GetComponent<Image>().raycastTarget = false; collectionStatusBackdrop = statusBox.gameObject;
            collectionLoading = FightUI.Text(statusBox, "正在加载预览…", 12, 8, status.width - 24, status.height - 16,
                height < 110 || page == "appearance" && appearanceCategory == "Mate" ? 17 : 20, FightUI.Foreground, TextAlignmentOptions.Center);
            collectionLoading.enableWordWrapping = true;
            collectionStage = new GameObject("Fight isolated preview"); collectionStage.transform.position = CollectionOrigin;
            var cameraObject = new GameObject("Preview camera", typeof(Camera)); cameraObject.transform.SetParent(collectionStage.transform, false);
            collectionCamera = cameraObject.GetComponent<Camera>();
            var source = animation ? Program.instance.camera_.cameraDuelOverlayEffect2D : Program.instance.camera_.cameraDuelOverlay2D;
            if (source != null) collectionCamera.CopyFrom(source);
            collectionCamera.enabled = true; collectionCamera.clearFlags = CameraClearFlags.SolidColor; collectionCamera.backgroundColor = animation ? FightUI.Background : new Color(0, 0, 0, 0);
            collectionCamera.cullingMask = 1 << CollectionLayer; collectionCamera.rect = new Rect(0, 0, 1, 1); collectionCamera.depth = -20;
            collectionCamera.nearClipPlane = .1f; collectionCamera.farClipPlane = 1200; collectionCamera.allowHDR = true; collectionCamera.allowMSAA = false;
            var data = cameraObject.AddComponent<UniversalAdditionalCameraData>(); data.renderType = CameraRenderType.Base; data.renderPostProcessing = false; data.cameraStack.Clear();
            // Fit both texture dimensions with one scale so short previews keep their display ratio.
            float textureScale = Mathf.Min(Mathf.Max(1, 128f / Mathf.Max(width, height)), Mathf.Min(1600f / width, 1000f / height));
            collectionTexture = new RenderTexture(Mathf.Max(1, Mathf.RoundToInt(width * textureScale)), Mathf.Max(1, Mathf.RoundToInt(height * textureScale)), 24, RenderTextureFormat.ARGB32);
            collectionTexture.name = "Fight collection preview"; collectionTexture.Create(); collectionCamera.targetTexture = collectionTexture; collectionSurface.texture = collectionTexture;
            collectionCamera.aspect = width / height; collectionCamera.ResetProjectionMatrix();
            if (animation && source != null)
            {
                collectionCamera.transform.position = CollectionOrigin + source.transform.position - Program.instance.container_2D.position;
                collectionCamera.transform.rotation = source.transform.rotation;
            }
            else { collectionCamera.orthographic = true; collectionCamera.orthographicSize = 20; collectionCamera.transform.localPosition = new Vector3(0, 19, 200); collectionCamera.transform.localEulerAngles = new Vector3(0, 180, 0); }
            foreach (var angle in new[] { new Vector3(123, -28, -40), new Vector3(-80, -140, 0) })
            {
                var lightObject = new GameObject("Preview light", typeof(Light)); lightObject.transform.SetParent(collectionStage.transform, false); lightObject.transform.eulerAngles = angle;
                var light = lightObject.GetComponent<Light>(); light.type = LightType.Directional; light.color = new Color(1, .92f, .8f); light.intensity = .9f; light.cullingMask = 1 << CollectionLayer;
            }
        }
        private async UniTask<Mate> LoadCollectionMateResource(int id, int generation, int? request = null)
        {
            // Folder-based mates open several bundles without the file loader's
            // per-path lock. Fast hover changes must retire queued requests before
            // opening another copy, including requests from the appearance page.
            await collectionMateLoader.WaitAsync();
            try
            {
                if (!CollectionRequestCurrent(generation) || request.HasValue && request.Value != collectionPlayGeneration) return null;
                return await ABLoader.LoadMateAsync(id, true);
            }
            finally { collectionMateLoader.Release(); }
        }
        private async void LoadCollectionMate(int id, int generation)
        {
            int request = ++collectionPlayGeneration;
            Mate mate = null;
            try
            {
                await UniTask.NextFrame();
                if (!CollectionRequestCurrent(generation) || request != collectionPlayGeneration) return;
                mate = await LoadCollectionMateResource(id, generation, request);
                if (!CollectionRequestCurrent(generation) || request != collectionPlayGeneration) { if (mate != null) Destroy(mate.gameObject); return; }
                if (mate == null) throw new InvalidOperationException("伙伴资源无法加载");
                collectionModel = new GameObject("Mate preview anchor"); collectionModel.transform.SetParent(collectionStage.transform, false);
                collectionMate = mate; mate.parent = collectionModel.transform; mate.transform.SetParent(collectionModel.transform, false); mate.gameObject.SetActive(true); PrepareCollectionObject(mate.gameObject);
                await UniTask.NextFrame(); if (!CollectionRequestCurrent(generation) || request != collectionPlayGeneration) return;
                mate.Play(Mate.MateAction.Entry);
                if (!await PrepareCollectionMateFraming(mate, generation) || request != collectionPlayGeneration) return;
                CollectionPreviewStatus(null);
            }
            catch (Exception e) { if (mate != null && collectionMate != mate) Destroy(mate.gameObject); if (CollectionRequestCurrent(generation) && request == collectionPlayGeneration) { CollectionPreviewStatus("伙伴加载失败 · 可选择其他资源", true); Debug.LogWarning("[FightCollection] mate: " + e); } }
        }
        private void FitCollectionMate(Transform target)
        {
            Bounds bounds = new Bounds(); bool found = false;
            foreach (var renderer in target.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || renderer is ParticleSystemRenderer) continue;
                Bounds b = renderer.bounds;
                if (!CollectionMateBoundsValid(b)) continue;
                if (!found) { bounds = b; found = true; } else bounds.Encapsulate(b);
            }
            // Current / next clips cover the actual Entry transition, without enlarging the view for unrelated attacks.
            // Their imported bounds include the complete motion, so later poses keep the same stable camera framing.
            foreach (var animator in target.GetComponentsInChildren<Animator>())
            {
                if (!animator.enabled || animator.runtimeAnimatorController == null) continue;
                for (int layer = 0; layer < animator.layerCount; layer++)
                {
                    foreach (var info in animator.GetCurrentAnimatorClipInfo(layer))
                        if (info.clip != null) IncludeCollectionMateLocalBounds(animator.transform, info.clip.localBounds, ref bounds, ref found);
                    foreach (var info in animator.GetNextAnimatorClipInfo(layer))
                        if (info.clip != null) IncludeCollectionMateLocalBounds(animator.transform, info.clip.localBounds, ref bounds, ref found);
                }
            }
            if (!found) return;
            var cameraTransform = collectionCamera.transform;
            Vector3 extents = bounds.extents, right = cameraTransform.right, up = cameraTransform.up;
            // Project the world bounds onto the preview camera so an angled mate view also fits completely.
            float halfWidth = Mathf.Abs(right.x) * extents.x + Mathf.Abs(right.y) * extents.y + Mathf.Abs(right.z) * extents.z;
            float halfHeight = Mathf.Abs(up.x) * extents.x + Mathf.Abs(up.y) * extents.y + Mathf.Abs(up.z) * extents.z;
            collectionCamera.orthographicSize = Mathf.Max(.15f, Mathf.Max(halfHeight, halfWidth / Mathf.Max(.01f, collectionCamera.aspect))) * 1.15f;
            cameraTransform.position = bounds.center - cameraTransform.forward * 200;
            // The model stays complete while leaving room for entry / tap motion.
            cameraTransform.position += up * halfHeight * .08f;
        }
        private async UniTask<bool> PrepareCollectionMateFraming(Mate mate, int generation)
        {
            CollectionPreviewStatus("正在准备伙伴预览…");
            collectionMateFraming = collectionMateFramingPreparing = true;
            FitCollectionMate(mate.transform);
            // Entry may animate the root / bones beyond its imported bounds. Observe its complete motion once,
            // then keep that zoom while following the body instead of retaining the first bowed pose's centre.
            await UniTask.NextFrame(); if (!CollectionRequestCurrent(generation) || collectionMate != mate) return false;
            await UniTask.NextFrame(); if (!CollectionRequestCurrent(generation) || collectionMate != mate) return false;
            float duration = .35f;
            foreach (var animator in mate.GetComponentsInChildren<Animator>())
            {
                if (!animator.enabled || animator.runtimeAnimatorController == null) continue;
                for (int layer = 0; layer < animator.layerCount; layer++)
                {
                    bool transition = animator.IsInTransition(layer);
                    var state = transition ? animator.GetNextAnimatorStateInfo(layer) : animator.GetCurrentAnimatorStateInfo(layer);
                    var clips = transition ? animator.GetNextAnimatorClipInfo(layer) : animator.GetCurrentAnimatorClipInfo(layer);
                    bool idle = clips.Length > 0 && clips.All(info => info.clip == null || CollectionMateIdleClip(info.clip.name));
                    // A looping idle does not postpone ready for an entire loop. Only the entering action's
                    // remaining non-looping motion needs to be observed before freezing the reserved zoom.
                    if (!idle) duration = Mathf.Max(duration, state.length * Mathf.Max(0, 1 - state.normalizedTime));
                }
            }
            float until = Time.unscaledTime + Mathf.Min(8, duration) + .2f;
            while (Time.unscaledTime < until)
            {
                await UniTask.NextFrame();
                if (!CollectionRequestCurrent(generation) || collectionMate != mate) return false;
            }
            collectionMateFramingPreparing = false;
            return true;
        }
        private static bool CollectionMateIdleClip(string name)
        {
            string lower = (name ?? "").ToLowerInvariant();
            return !lower.Contains("entry") && (lower.Contains("idle") || lower.Contains("wait") || lower.Contains("normal"));
        }
        private void LateUpdate()
        {
            if (!collectionMateFraming || collectionMate == null || collectionCamera == null || !collectionCamera.orthographic) return;
            collectionMateBodyRenderers.Clear();
            collectionMate.gameObject.GetComponentsInChildren<Renderer>(false, collectionMateBodyRenderers);
            Bounds bounds = new Bounds(); bool found = false;
            foreach (var renderer in collectionMateBodyRenderers)
            {
                if (renderer == null || !renderer.enabled || renderer is ParticleSystemRenderer || !CollectionMateBoundsValid(renderer.bounds)) continue;
                if (!found) { bounds = renderer.bounds; found = true; } else bounds.Encapsulate(renderer.bounds);
            }
            if (!found) return;
            var cameraTransform = collectionCamera.transform;
            Vector3 extents = bounds.extents, right = cameraTransform.right, up = cameraTransform.up;
            float halfWidth = Mathf.Abs(right.x) * extents.x + Mathf.Abs(right.y) * extents.y + Mathf.Abs(right.z) * extents.z;
            float halfHeight = Mathf.Abs(up.x) * extents.x + Mathf.Abs(up.y) * extents.y + Mathf.Abs(up.z) * extents.z;
            float required = Mathf.Max(.15f, Mathf.Max(halfHeight, halfWidth / Mathf.Max(.01f, collectionCamera.aspect)));
            // Keep the accumulated zoom; normal idle motion uses its reserved margin. A larger new action can
            // expand it once, but physical window fitting never drives this framing or recreates its resources.
            if (collectionMateFramingPreparing || required > collectionCamera.orthographicSize * .94f)
                collectionCamera.orthographicSize = Mathf.Max(collectionCamera.orthographicSize, required * 1.15f);
            cameraTransform.position = bounds.center - cameraTransform.forward * 200 + up * halfHeight * .08f;
        }
        private static bool CollectionMateBoundsValid(Bounds bounds)
        {
            float size = bounds.size.sqrMagnitude;
            return size >= .001f && size <= 10000000 && !float.IsNaN(size) && !float.IsInfinity(size)
                && !float.IsNaN(bounds.center.sqrMagnitude) && !float.IsInfinity(bounds.center.sqrMagnitude);
        }
        private static void IncludeCollectionMateLocalBounds(Transform source, Bounds local, ref Bounds bounds, ref bool found)
        {
            if (!CollectionMateBoundsValid(local)) return;
            var transformed = new Bounds(source.TransformPoint(local.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
                transformed.Encapsulate(source.TransformPoint(local.center + new Vector3(local.extents.x * ((i & 1) == 0 ? -1 : 1),
                    local.extents.y * ((i & 2) == 0 ? -1 : 1), local.extents.z * ((i & 4) == 0 ? -1 : 1))));
            if (!CollectionMateBoundsValid(transformed)) return;
            if (!found) { bounds = transformed; found = true; } else bounds.Encapsulate(transformed);
        }
        private void HideAppearanceEnvironment()
        {
            if (page != "appearance" || appearanceCategory != "Mate" || collectionMate == null || collectionModel == null) return;
            // The mate is a sibling of the field clone, so field, stand, grave and their particles cannot obscure it.
            // Reuse the buffer, but still discover renderers every tick: a timeline may add
            // a Renderer to an existing node or re-enable a previously hidden component.
            collectionEnvironmentRenderers.Clear();
            collectionModel.GetComponentsInChildren<Renderer>(true, collectionEnvironmentRenderers);
            foreach (var renderer in collectionEnvironmentRenderers) if (renderer != null) renderer.enabled = false;
        }
        private void PrepareCollectionObject(GameObject obj)
        {
            foreach (Transform t in obj.GetComponentsInChildren<Transform>(true)) { t.gameObject.layer = CollectionLayer; collectionKnownTransforms.Add(t); }
            foreach (var camera in obj.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
            foreach (var listener in obj.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
            foreach (var effect in obj.GetComponentsInChildren<ScreenEffect>(true)) { effect.enabled = false; Destroy(effect); }
        }
        private bool CollectionRequestCurrent(int generation) => this != null && generation == collectionGeneration && collectionStage != null && (page == "collection" || page == "animations" || page == "appearance") && Program.instance.currentServant != Program.instance.ocgcore;
        // The option grid is constructed before the camera stage. Icon requests only depend on the page and its generation.
        private bool CollectionIconRequestCurrent(int generation) => this != null && generation == collectionGeneration && (page == "collection" || page == "animations" || page == "appearance") && Program.instance.currentServant != Program.instance.ocgcore;
        private void CollectionPreviewStatus(string message, bool failed = false)
        {
            collectionPreviewLoaded = string.IsNullOrEmpty(message);
            collectionPreviewError = failed;
            if (collectionPreviewLoaded && collectionProtectorError) message = "卡套预览加载失败 · 可选择其他素材";
            if (collectionStatusBackdrop != null) collectionStatusBackdrop.SetActive(!string.IsNullOrEmpty(message));
            if (collectionLoading != null) { collectionLoading.text = message ?? ""; collectionLoading.gameObject.SetActive(!string.IsNullOrEmpty(message)); }
        }
        private void ClearCollectionModel() { if (collectionModel != null) { collectionModel.SetActive(false); Destroy(collectionModel); } collectionModel = null; collectionMate = null; collectionMateFraming = collectionMateFramingPreparing = false; collectionEnvironmentRenderers.Clear(); collectionKnownTransforms.RemoveWhere(t => t == null); }
        internal void CleanupCollectionPreview()
        {
            collectionDirectoryGeneration++; collectionDirectoryBuilding = false;
            collectionGeneration++; collectionPlayGeneration++; collectionPlaying = false; collectionPreviewFilteredOut = false;
            collectionVoiceRequest++; collectionVoiceLoading = false; collectionVoiceError = collectionVoicePath = null; collectionVoiceButton = null;
            collectionSearchInput = null;
            collectionPendingMateTap = collectionPendingCutinPlay = 0; collectionBrowseIds.Clear();
            collectionMateFraming = collectionMateFramingPreparing = false; collectionMateBodyRenderers.Clear();
            collectionPreviewLoaded = collectionPreviewError = collectionProtectorReady = collectionProtectorError = collectionPreviewNeedsProtector = false;
            if (collectionVoiceSource != null) collectionVoiceSource.Stop(); collectionVoiceSource = null;
            if (collectionVoiceClip != null) Destroy(collectionVoiceClip); collectionVoiceClip = null;
            if (collectionCamera != null) { collectionCamera.enabled = false; collectionCamera.targetTexture = null; }
            if (collectionSurface != null) collectionSurface.texture = null;
            if (collectionStage != null) { collectionStage.SetActive(false); Destroy(collectionStage); }
            collectionStage = collectionModel = collectionStatusBackdrop = null; collectionMate = null; collectionCamera = null; collectionSurface = null; collectionLoading = null; collectionList = null;
            libraryLeftPanel = libraryRightPanel = librarySidePanel = libraryStagePanel = null;
            libraryFaceOverlay = libraryFrameOverlay = librarySleeveOverlay = null;
            libraryCaption = libraryDescription = libraryPreviewDetail = libraryPreviewBadge = null;
            appearanceLocateButton = appearanceRestoreButton = appearanceSelectedResource = null;
            libraryAutoPlayAction = libraryPreviewAction = null;
            libraryNodeLayouts.Clear(); libraryReflowScrollPositions.Clear();
            appearanceApplyButton = appearanceDiscardButton = appearanceReviewButton = appearanceSyncButton = appearanceOverrideButton = collectionUseMateButton = collectionLeftButton = collectionRightButton = appearanceMateButton = null; appearanceDirtyLabel = null;
            appearanceScopeBar = appearanceApplyBar = null; appearanceScopeButtons.Clear(); appearanceCategoryIndicators.Clear();
            appearanceCatalogTitle = null; collectionPendingIcons.Clear();
            if (collectionTexture != null) { collectionTexture.Release(); Destroy(collectionTexture); collectionTexture = null; }
            foreach (var handle in collectionSpriteHandles) if (handle.IsValid()) Addressables.Release(handle); collectionSpriteHandles.Clear();
            collectionProtectorIcons.Clear();
            collectionIsolationTransforms.Clear(); collectionKnownTransforms.Clear(); collectionEnvironmentRenderers.Clear();
            foreach (var texture in collectionProtectorTextures.Values) if (texture != null) Destroy(texture); collectionProtectorTextures.Clear();
            if (page != "animations") collectionAutoPlay = false;
        }
        internal void TickCollectionPreview()
        {
            if (collectionStage == null) return;
            if (collectionVoiceLoading && Time.unscaledTime - collectionVoiceLoadStarted > 28)
            {
                collectionVoiceRequest++; collectionVoiceLoading = false; collectionVoiceError = "语音加载超时";
                ShowStatus(collectionVoiceError);
            }
            RefreshCollectionVoiceAction();
            RefreshCollectionActions();
            PlayPendingCollectionAction();
            if ((!collectionPreviewLoaded || (collectionPreviewNeedsProtector && !collectionProtectorReady)) && !CollectionPreviewFailed && Time.unscaledTime - collectionLoadStarted > 28)
            { collectionPlaying = false; CollectionPreviewStatus("预览加载超时 · 请选择其他资源或稍后重试", true); }
            // Some timelines create nested children after loading. Keep checking transforms,
            // without allocating a hierarchy array on every tick.
            collectionIsolationTransforms.Clear();
            collectionStage.GetComponentsInChildren<Transform>(true, collectionIsolationTransforms);
            foreach (Transform t in collectionIsolationTransforms)
            {
                if (t.gameObject.layer != CollectionLayer) t.gameObject.layer = CollectionLayer;
                if (collectionKnownTransforms.Add(t))
                {
                    if (t != collectionStage.transform && t.GetComponent<Camera>() != collectionCamera) PrepareCollectionObject(t.gameObject);
                }
            }
            HideAppearanceEnvironment();
            if (page != "animations") return;
            if (collectionPlaying && Time.unscaledTime >= collectionPlayUntil)
            {
                collectionPlaying = false;
                if (collectionModel != null) foreach (var director in collectionModel.GetComponentsInChildren<PlayableDirector>(true)) { director.time = .8; director.Evaluate(); director.Pause(); }
            }
            if (collectionAutoPlay && !collectionPlaying && Time.unscaledTime >= collectionNextPlay) PlayCollectionAnimation();
        }
        private void RefreshCollectionActions()
        {
            bool mateReady = collectionPreviewLoaded && !CollectionPreviewFailed && collectionMate != null;
            foreach (var button in new[] { collectionLeftButton, collectionRightButton, appearanceMateButton })
                if (button != null)
                {
                    button.interactable = mateReady;
                    FightUI.Hint(button.gameObject, mateReady ? (button == collectionLeftButton ? "将伙伴向左旋转" : button == collectionRightButton ? "将伙伴向右旋转" : "播放所选伙伴的互动动作")
                        : page == "appearance" && appearanceDraft != null && appearanceDraft.Values["Mate"] == "0" ? "选择一个伙伴后可播放互动"
                        : CollectionPreviewFailed ? "伙伴预览不可用，请选择其他资源" : "伙伴加载完成后可操作");
                }
            if (collectionUseMateButton != null) collectionUseMateButton.interactable = collectionMateId != 0;
            if (page == "animations" && libraryPreviewAction != null)
            {
                var play = libraryPreviewAction.GetComponent<Button>();
                play.interactable = collectionCutinId != 0 && (CollectionPreviewReady || CollectionPreviewFailed);
                var caption = play.GetComponentInChildren<TextMeshProUGUI>();
                string label = CollectionPreviewFailed ? "重试预览" : collectionPlaying && CollectionPreviewReady ? "重新播放" : "播放动画";
                if (caption.text != label) caption.text = label;
                FightUI.Hint(play.gameObject, CollectionPreviewFailed ? "重新加载所选动画" : CollectionPreviewReady ? "播放所选召唤动画" : "动画加载完成后可播放");
            }
            RefreshCollectionPreviewContext();
        }
        private void RefreshCollectionPreviewContext()
        {
            if (page != "collection" && page != "animations") return;
            bool animation = page == "animations", playing = animation && collectionPlaying && CollectionPreviewReady;
            if (libraryPreviewBadge != null)
            {
                libraryPreviewBadge.text = playing ? "播放中" : collectionPreviewFilteredOut ? "筛选外" : "预览";
                FightUI.Hint(libraryPreviewBadge.gameObject, collectionPreviewFilteredOut
                    ? "当前预览不在筛选结果中。清除搜索后可在完整目录中找到它。" : "当前为资源预览。");
            }
            if (libraryDescription == null) return;
            string description = animation
                ? collectionCutinId == 0 ? "" : "卡片编号 " + collectionCutinId
                : collectionMateId == 0 ? "" : "伙伴编号 " + collectionMateId;
            if (libraryDescription.text != description) libraryDescription.text = description;
            FightUI.Hint(libraryDescription.gameObject, description.Replace('\n', '。'));
        }
        private Image CollectionBlankImage(Transform parent, float x, float y, float width, float height)
        {
            var rt = FightUI.Node(parent, "Resource icon", x, y, width, height); var image = rt.gameObject.AddComponent<Image>(); image.raycastTarget = false; image.preserveAspect = true; image.color = Color.clear; return image;
        }
        private void CollectionItemImage(Transform parent, string id, Items.ItemType type, float x, float y, float width, float height, bool requiredForPreview = false)
        {
            if (id == "0")
            {
                var empty = FightUI.Panel(parent, x, y, width, height, FightUI.PanelColor, false, "No appearance selected");
                empty.GetComponent<Image>().raycastTarget = false;
                float size = Mathf.Min(width, height) * .3f;
                FightUI.Icon(empty, "diamond", (width - size) / 2, (height - size) / 2, size, size, FightUI.GoldBorder);
                return;
            }
            if (type == Items.ItemType.Protector)
            {
                var rt = FightUI.Node(parent, "Protector texture preview", x, y, width, height);
                var sleeve = rt.gameObject.AddComponent<RawImage>(); sleeve.raycastTarget = false; sleeve.color = FightUI.Raised;
                var icon = new CollectionProtectorIcon { Image = sleeve, IsPreview = requiredForPreview };
                icon.Status = FightUI.Text(rt, "正在加载", 0, 0, width, height, 14, FightUI.Muted, TextAlignmentOptions.Center);
                collectionProtectorIcons.Add(icon);
                if (requiredForPreview) { collectionPreviewNeedsProtector = true; collectionProtectorReady = collectionProtectorError = false; }
                LoadCollectionProtector(icon, id, collectionGeneration);
                return;
            }
            var image = CollectionBlankImage(parent, x, y, width, height); LoadCollectionItemSprite(image, id, type);
        }
        private async void LoadCollectionProtector(CollectionProtectorIcon icon, string id, int generation)
        {
            var image = icon.Image;
            try
            {
                // Wait for the completed grid layout and navigation scroll restoration before prioritizing visible entries.
                await UniTask.NextFrame();
                // Load the visible sleeves first; hundreds of offscreen materials should not block the current selection.
                while (image != null && CollectionIconRequestCurrent(generation) && !CollectionIconVisible(image.rectTransform)) await UniTask.Delay(100, ignoreTimeScale: true);
                if (image == null || !CollectionIconRequestCurrent(generation)) return;
                var texture = await LoadCollectionProtectorTexture(id, generation);
                if (image == null || !CollectionIconRequestCurrent(generation)) return;
                if (texture == null) throw new InvalidOperationException("卡套纹理尚未安装或没有可见像素");
                image.texture = texture; image.material = null; image.color = Color.white;
                icon.Resolved = icon.HasPixels = true;
                if (icon.Status != null) icon.Status.gameObject.SetActive(false);
                if (icon.IsPreview) collectionProtectorReady = true;
            }
            catch (Exception e)
            {
                if (image == null || !CollectionIconRequestCurrent(generation)) return;
                image.texture = Texture2D.whiteTexture; image.material = null; image.color = FightUI.Raised;
                icon.Resolved = icon.Unavailable = true;
                if (icon.Status != null) { icon.Status.text = "缩略图不可用"; icon.Status.gameObject.SetActive(true); }
                if (icon.IsPreview) { collectionProtectorError = true; CollectionPreviewStatus("卡套预览加载失败 · 可选择其他素材", true); }
                Debug.LogWarning("[FightCollection] protector: " + e.Message);
            }
        }
        private async UniTask<Texture2D> LoadCollectionProtectorTexture(string id, int generation)
        {
            await collectionProtectorLoader.WaitAsync();
            try
            {
                if (!CollectionIconRequestCurrent(generation)) return null;
                if (collectionProtectorTextures.TryGetValue(id, out var cached) && cached != null) return cached;
                Texture2D result = null;
                string iconName = "ProtectorIcon" + id, iconPath = Program.root + "MasterDuel/Protector/" + id + "/" + iconName;
                // These native icon bundles are installed beside PMat, but are not registered in the Addressables catalog.
                if (File.Exists(iconPath))
                {
                    var bundle = await AssetBundle.LoadFromFileAsync(iconPath);
                    if (bundle != null)
                    {
                        try
                        {
                            var request = bundle.LoadAllAssetsAsync(); await request;
                            if (!CollectionIconRequestCurrent(generation)) return null;
                            var sprite = request.allAssets.OfType<Sprite>().OrderByDescending(s => s.name == iconName).FirstOrDefault();
                            if (sprite != null)
                            {
                                var region = sprite.rect; try { region = sprite.textureRect; } catch (UnityException) { }
                                result = CopyCollectionTexture(sprite.texture, region);
                            }
                            if (!CollectionTextureHasVisiblePixels(result))
                            {
                                if (result != null) Destroy(result); result = null;
                                foreach (var source in request.allAssets.OfType<Texture2D>().OrderByDescending(t => t.name == iconName))
                                {
                                    result = CopyCollectionTexture(source);
                                    if (CollectionTextureHasVisiblePixels(result)) break;
                                    if (result != null) Destroy(result); result = null;
                                }
                            }
                        }
                        finally { bundle.Unload(false); }
                    }
                }
                if (result == null)
                {
                    var material = await ABLoader.LoadProtectorMaterial(id, CancellationToken.None);
                    if (!CollectionIconRequestCurrent(generation)) return null;
                    if (material != null)
                        foreach (string name in material.GetTexturePropertyNames().OrderByDescending(n => n.IndexOf("Protector", StringComparison.OrdinalIgnoreCase) >= 0 || n == "_MainTex"))
                        {
                            var source = material.GetTexture(name); if (source == null || source.width <= 4 || source.height <= 4) continue;
                            result = CopyCollectionTexture(source);
                            if (CollectionTextureHasVisiblePixels(result)) break;
                            if (result != null) Destroy(result); result = null;
                        }
                }
                if (result != null) { result.name = "Fight protector " + id; collectionProtectorTextures[id] = result; }
                return result;
            }
            finally { collectionProtectorLoader.Release(); }
        }
        private static Texture2D CopyCollectionTexture(Texture source, Rect? sourceRect = null)
        {
            if (source == null) return null;
            var region = sourceRect ?? new Rect(0, 0, source.width, source.height);
            float scale = Mathf.Min(1, 512f / Mathf.Max(region.width, region.height));
            int width = Mathf.Max(1, Mathf.RoundToInt(region.width * scale)), height = Mathf.Max(1, Mathf.RoundToInt(region.height * scale));
            var previous = RenderTexture.active; var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            Texture2D copy = null;
            try
            {
                Graphics.Blit(source, target, new Vector2(region.width / source.width, region.height / source.height), new Vector2(region.x / source.width, region.y / source.height));
                RenderTexture.active = target; copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(0, 0, width, height), 0, 0, false); copy.Apply(false, false); return copy;
            }
            catch { if (copy != null) Destroy(copy); throw; }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); }
        }
        private static bool CollectionTextureHasVisiblePixels(Texture2D texture)
        {
            if (texture == null) return false;
            var pixels = texture.GetPixels32(); int stride = Mathf.Max(1, pixels.Length / 4096), visible = 0, samples = 0;
            int minR = 255, minG = 255, minB = 255, minA = 255, maxR = 0, maxG = 0, maxB = 0, maxA = 0;
            for (int i = 0; i < pixels.Length; i += stride)
            {
                var p = pixels[i]; samples++;
                if (p.a >= 16 && Mathf.Max(p.r, Mathf.Max(p.g, p.b)) >= 16) visible++;
                minR = Mathf.Min(minR, p.r); minG = Mathf.Min(minG, p.g); minB = Mathf.Min(minB, p.b); minA = Mathf.Min(minA, p.a);
                maxR = Mathf.Max(maxR, p.r); maxG = Mathf.Max(maxG, p.g); maxB = Mathf.Max(maxB, p.b); maxA = Mathf.Max(maxA, p.a);
            }
            return visible >= Mathf.Max(4, samples / 100) && Mathf.Max(Mathf.Max(maxR - minR, maxG - minG), Mathf.Max(maxB - minB, maxA - minA)) > 12;
        }
        private static bool CollectionIconVisible(RectTransform image)
        {
            var scroll = image.GetComponentInParent<ScrollRect>();
            if (scroll == null || scroll.viewport == null) return true;
            var corners = new Vector3[4]; image.GetWorldCorners(corners);
            var lower = scroll.viewport.InverseTransformPoint(corners[0]); var upper = scroll.viewport.InverseTransformPoint(corners[2]);
            return scroll.viewport.rect.Overlaps(Rect.MinMaxRect(lower.x, lower.y, upper.x, upper.y));
        }
        private async void LoadCollectionItemSprite(Image image, string id, Items.ItemType type)
        {
            int generation = collectionGeneration; collectionPendingIcons.Add(image);
            try
            {
                await UniTask.NextFrame();
                while (image != null && CollectionIconRequestCurrent(generation) && !CollectionIconVisible(image.rectTransform)) await UniTask.Delay(100, ignoreTimeScale: true);
                if (image == null || !CollectionIconRequestCurrent(generation)) return;
                var sprite = await Program.items.TryLoadItemIconAsync(id, type);
                if (image == null || !CollectionIconRequestCurrent(generation)) return;
                if (sprite != null) { image.sprite = sprite; image.color = Color.white; }
                else CollectionIconPlaceholder(image, "暂无缩略图");
            }
            catch (Exception e) { if (image != null && CollectionIconRequestCurrent(generation)) CollectionIconPlaceholder(image, "预览不可用"); Debug.LogWarning("[FightCollection] icon: " + e.Message); }
            finally { collectionPendingIcons.Remove(image); }
        }
        private async void LoadCollectionCharacterSprite(Image image, string address, int generation)
        {
            var handle = default(AsyncOperationHandle<Sprite>); bool retained = false; collectionPendingIcons.Add(image);
            try
            {
                await UniTask.NextFrame();
                while (image != null && CollectionIconRequestCurrent(generation) && !CollectionIconVisible(image.rectTransform)) await UniTask.Delay(100, ignoreTimeScale: true);
                if (!CollectionIconRequestCurrent(generation) || image == null) return;
                handle = Addressables.LoadAssetAsync<Sprite>(address);
                var sprite = await handle;
                if (!CollectionIconRequestCurrent(generation) || image == null) return;
                collectionSpriteHandles.Add(handle); retained = true;
                if (sprite != null) { image.sprite = sprite; image.color = Color.white; }
                else CollectionIconPlaceholder(image, "暂无缩略图");
            }
            catch { if (image != null && CollectionIconRequestCurrent(generation)) CollectionIconPlaceholder(image, "预览不可用"); }
            finally
            {
                if (!retained && handle.IsValid()) Addressables.Release(handle);
                collectionPendingIcons.Remove(image);
            }
        }
        private static void CollectionIconPlaceholder(Image image, string message)
        {
            image.color = FightUI.Raised;
            FightUI.Text(image.transform, message, 0, 0, image.rectTransform.rect.width, image.rectTransform.rect.height, 14, FightUI.Muted, TextAlignmentOptions.Center);
        }
        private static bool CollectionMatches(string name, string term, string id = null) => string.IsNullOrWhiteSpace(term)
            || (name ?? "").IndexOf(term.Trim(), StringComparison.OrdinalIgnoreCase) >= 0
            || (id ?? "").IndexOf(term.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        private static bool CollectionSpriteExists(string address) => Addressables.ResourceLocators.Any(locator => locator.Locate(address, typeof(Sprite), out var locations) && locations.Count > 0);
        private List<Items.Item> AvailableCollectionItems(Items.ItemType type)
        {
            if (collectionAvailableItems.TryGetValue(type, out var cached)) return cached;
            var list = CollectionItems(type);
            // Resources are installed at startup. Category switches and draft
            // changes should reuse that inventory instead of probing every file.
            var available = list == null ? new List<Items.Item>() : list.Where(i => !i.notReady && i.id > 9999 && CollectionItemAvailable(i, type)).ToList();
            collectionAvailableItems[type] = available;
            return available;
        }
        private static bool CollectionItemAvailable(Items.Item item, Items.ItemType type)
        {
            string path = Program.root + "MasterDuel/" + item.path;
            if (type == Items.ItemType.Mate) return item.path != null && (item.path.EndsWith("_Folder") ? Directory.Exists(path.Replace("_Folder", "")) : File.Exists(path));
            if (type == Items.ItemType.Mat || type == Items.ItemType.Grave || type == Items.ItemType.Stand) return File.Exists(path + "_near");
            if (type == Items.ItemType.Wallpaper) return File.Exists(path) || Directory.Exists(path);
            if (type == Items.ItemType.Protector) return File.Exists(path) || Directory.Exists(path) || Directory.Exists(Program.root + "MasterDuel/Protector/" + item.id);
            return CollectionSpriteExists(Items.GetIconAddress(item.id.ToString()));
        }
        private List<Items.Item> CollectionItems(Items.ItemType type)
        {
            switch (type)
            {
                case Items.ItemType.Face: return Program.items.faces; case Items.ItemType.Frame: return Program.items.frames;
                case Items.ItemType.Protector: return Program.items.protectors; case Items.ItemType.Mat: return Program.items.mats;
                case Items.ItemType.Grave: return Program.items.graves; case Items.ItemType.Stand: return Program.items.stands;
                case Items.ItemType.Mate: return Program.items.mates; case Items.ItemType.Case: return Program.items.cases;
                case Items.ItemType.Wallpaper: return Program.items.wallpapers; default: return new List<Items.Item>();
            }
        }
        private static Items.ItemType AppearanceItemType(string category)
        {
            switch (category)
            {
                case "Face": return Items.ItemType.Face; case "Frame": return Items.ItemType.Frame; case "Protector": return Items.ItemType.Protector;
                case "Field": return Items.ItemType.Mat; case "Grave": return Items.ItemType.Grave; case "Stand": return Items.ItemType.Stand;
                case "Mate": return Items.ItemType.Mate; case "Case": return Items.ItemType.Case; case "Wallpaper": return Items.ItemType.Wallpaper; default: return Items.ItemType.Unknown;
            }
        }
        private static Transform CollectionFind(Transform parent, string name) => parent.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
    }
    internal sealed class FightCollectionBackdrop : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); var r = rectTransform.rect;
            mesh.AddVert(new Vector2(r.xMin, r.yMin), FightUI.Hex("101C27"), Vector2.zero);
            mesh.AddVert(new Vector2(r.xMin, r.yMax), FightUI.Hex("06111C"), Vector2.zero);
            mesh.AddVert(new Vector2(r.xMax, r.yMax), FightUI.Hex("06111C"), Vector2.zero);
            mesh.AddVert(new Vector2(r.xMax, r.yMin), FightUI.Hex("101C27"), Vector2.zero);
            mesh.AddTriangle(0, 1, 2); mesh.AddTriangle(0, 2, 3);
            var center = new Vector2(r.center.x, r.yMin + r.height * .25f);
            const int count = 64;
            for (int ring = 5; ring >= 1; ring--)
            {
                var points = new Vector2[count]; float scale = ring / 5f;
                for (int i = 0; i < count; i++)
                {
                    float a = i * (float)Math.PI * 2 / count;
                    points[i] = center + new Vector2(Mathf.Cos(a) * r.width * .32f * scale, Mathf.Sin(a) * r.height * .12f * scale);
                }
                var tint = FightUI.Hex("356B7E"); tint.a = ring == 5 ? .24f : .07f;
                FightFrame.Polygon(mesh, points, ring == 5 ? 1 : .6f, tint);
            }
            var seam = FightUI.Hex("39505D"); seam.a = .12f;
            FightFrame.Segment(mesh, new Vector2(r.xMin + 18, center.y), new Vector2(r.xMax - 18, center.y), 1, seam);
        }
    }

}
