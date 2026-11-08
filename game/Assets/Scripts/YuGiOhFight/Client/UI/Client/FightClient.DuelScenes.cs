using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using YuGiOhFight.Client.Duel;

namespace YuGiOhFight.Client.UI.Client
{
    internal partial class FightClient
    {
        private void RenderDuelSceneSettings(Transform parent, float width, ref float y)
        {
            SettingsSection(parent, width, "对战场景", ref y);
            bool pending = settingsValues[DuelSceneCatalog.EnabledKey] != settingsOriginal[DuelSceneCatalog.EnabledKey]
                || settingsValues[DuelSceneCatalog.SceneKey] != settingsOriginal[DuelSceneCatalog.SceneKey];
            var view = FightDuelSceneSummary.Create(parent, 0, y, width,
                settingsValues[DuelSceneCatalog.EnabledKey] == "1", settingsValues[DuelSceneCatalog.SceneKey], false, pending,
                () =>
                {
                    SettingsChanged(DuelSceneCatalog.EnabledKey, settingsValues[DuelSceneCatalog.EnabledKey] == "1" ? "0" : "1");
                    RenderSettings(); FocusRenderedSettingsField(DuelSceneCatalog.EnabledKey);
                }, OpenDuelSceneChooser, "settings:field:");
            settingsFieldAnchors[DuelSceneCatalog.EnabledKey] = view.Switch.GetComponent<RectTransform>();
            settingsFieldAnchors[DuelSceneCatalog.SceneKey] = view.Choose.GetComponent<RectTransform>();
            y += view.LayoutHeight + 8;
        }

        private void OpenDuelSceneChooser()
        {
            var box = ShowDialog("选择对战场景", 1240, 820);
            var chooser = box.gameObject.AddComponent<FightDuelSceneChooser>();
            chooser.Bind(box, settingsValues[DuelSceneCatalog.SceneKey], settingsOriginal[DuelSceneCatalog.SceneKey],
                settingsValues[DuelSceneCatalog.EnabledKey] == "1", false, id =>
                {
                    CloseDialog(); SettingsChanged(DuelSceneCatalog.SceneKey, id);
                    RenderSettings(); FocusRenderedSettingsField(DuelSceneCatalog.SceneKey);
                }, CloseDialog, () => new Vector2(W, H), () => root.localScale.x, backdrop: modal);
        }
    }

    // A preference preview is deliberately separate from the active duel and
    // the collection's live camera. Local screenshots share FightAsset's cache.
    internal sealed class FightDuelScenePreview
    {
        internal readonly RectTransform Root;
        private readonly RawImage image;
        private readonly TextMeshProUGUI placeholder;
        private Texture texture;
        private string placeholderLabel;

        internal FightDuelScenePreview(Transform parent)
        {
            Root = FightUI.Panel(parent, 0, 0, 160, 90, FightUI.Raised, true, "Duel scene preview");
            Root.GetComponent<Image>().raycastTarget = false;
            image = FightUI.Node(Root, "Scene screenshot", 0, 0, 160, 90).gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            placeholder = FightUI.RawText(Root, "", 12, 12, 136, 66, 16, FightUI.Muted, TextAlignmentOptions.Center);
            placeholder.raycastTarget = false;
        }

        internal void Show(string id, bool overview = false)
        {
            var definition = DuelSceneCatalog.Find(id);
            texture = null;
            if (definition != null)
            {
                try
                {
                    var file = DuelSceneCatalog.ContentFile(overview ? definition.OverviewPath : definition.PreviewPath);
                    if (System.IO.File.Exists(file))
                    {
                        var candidate = FightAsset.Acquire(file);
                        if (candidate != null && candidate.width > 2 && candidate.height > 2) texture = candidate;
                    }
                }
                catch (Exception) { /* A missing or unreadable preview keeps its named placeholder. */ }
            }
            image.texture = texture; image.gameObject.SetActive(texture != null);
            placeholder.gameObject.SetActive(texture == null);
            placeholderLabel = definition == null ? FightText.Get("场景不可用\n请重新选择")
                : FightText.Get(definition.Name) + "\n" + FightText.Get("预览暂不可用");
            Layout(-Root.anchoredPosition.y, Root.anchoredPosition.x, Root.rect.width, Root.rect.height);
        }

        internal void Layout(float y, float x, float width, float height)
        {
            FightUI.Place(Root, x, y, width, height);
            bool thumbnail = width < 96 || height < 56;
            placeholder.text = thumbnail ? "…" : placeholderLabel;
            FightUI.Place(placeholder.rectTransform, thumbnail ? 0 : 12, thumbnail ? 0 : 12,
                thumbnail ? width : Mathf.Max(1, width - 24), thumbnail ? height : Mathf.Max(1, height - 24));
            if (texture == null) return;
            float ratio = (float)texture.width / texture.height;
            float fitWidth = Mathf.Min(width, height * ratio), fitHeight = fitWidth / ratio;
            FightUI.Place(image.rectTransform, (width - fitWidth) / 2, (height - fitHeight) / 2, fitWidth, fitHeight);
        }
    }

    internal sealed class FightDuelSceneSummary : MonoBehaviour
    {
        internal const float Height = 270;
        internal Button Switch, Choose;
        internal float LayoutHeight => panel.rect.height;
        private RectTransform panel, inner;
        private FightDuelScenePreview preview;
        private TextMeshProUGUI nameLabel, description, hint, status, currentDuel;
        private bool inDuel;
        private float previousWidth, previousDensity;

        internal static FightDuelSceneSummary Create(Transform parent, float x, float y, float width, bool enabled,
            string id, bool inDuel, bool pending, Action toggle, Action choose, string focusPrefix)
        {
            var panel = FightUI.Node(parent, "Duel scene preference", x, y, width, Height);
            var view = panel.gameObject.AddComponent<FightDuelSceneSummary>(); view.panel = panel; view.inDuel = inDuel;
            view.inner = FightUI.Node(panel, "Scene preference layout", 0, 0, width, Height);
            view.Switch = FightUI.FocusKey(FightUI.SwitchRow(view.inner, "3D 对战场景", enabled, 0, 0, width, 54, toggle), focusPrefix + DuelSceneCatalog.EnabledKey);
            view.preview = new FightDuelScenePreview(view.inner); view.preview.Show(id, true);
            var definition = DuelSceneCatalog.Find(id);
            bool available = DuelSceneCatalog.IsAvailable(definition);
            view.nameLabel = FightUI.Text(view.inner, definition?.Name ?? "场景不可用", 244, 78, width - 244, 32, 22, FightUI.Gold);
            view.nameLabel.enableWordWrapping = false; view.nameLabel.enableAutoSizing = true;
            view.nameLabel.fontSizeMin = 18; view.nameLabel.fontSizeMax = 22;
            FightUI.Hint(view.nameLabel.gameObject, definition?.Name ?? "场景不可用");
            string statusText = inDuel ? pending ? "下局应用" : enabled ? "本局使用中" : "开启后使用" : pending ? "待保存" : "已保存";
            view.status = FightUI.Text(view.inner, statusText, 244, 74, 164, 26, 16, pending ? FightUI.Gold : FightUI.Muted, TextAlignmentOptions.MidlineRight);
            if (inDuel)
            {
                var manager = Runtime.Program.instance.ocgcore.DuelBGManager;
                string actual = manager != null && !manager.Scene3DActive ? FightText.Get("简洁场地")
                    : FightText.Get(DuelSceneCatalog.Find(manager?.ActiveSceneId ?? DuelSceneCatalog.DefaultId)?.Name ?? "场景不可用");
                view.currentDuel = FightUI.RawText(view.inner, FightText.Get("本局：") + actual + " · " + FightText.Get("本局使用中"), 0, 64, width, 26, 16, FightUI.Muted);
            }
            string description = !available ? "场景资源不可用" : !enabled ? inDuel ? "下局：简洁场地；开启后使用此场景" : "开启后使用此场景" : definition.Style;
            view.description = FightUI.Text(view.inner, description, 244, 118, width - 244, 42, 16, available ? FightUI.Muted : FightUI.Danger);
            FightUI.Hint(view.description.gameObject, description);
            view.Choose = FightUI.FocusKey(FightUI.Button(view.inner, "选择场景", 244, 170, 196, 44, choose), focusPrefix + DuelSceneCatalog.SceneKey);
            string message = inDuel ? "当前对局不变；下次进入对战、观战或回放时应用。"
                : "保存后，下一次进入对战、观战或回放时应用。";
            if (!enabled) message = FightText.Get("已关闭 3D 环境；下次开启时使用此场景。") + " "
                + FightText.Get(inDuel ? "当前对局不变。" : "修改仍需保存。");
            if (!available && enabled) message = "场景资源不可用；进入对局时使用经典场地。原设置保留，请重新选择。";
            else if (!available) message = "3D 保持关闭。场景资源不可用，原选择保留。";
            view.hint = FightUI.Text(view.inner, message, 0, 226, width, 44, 16, FightUI.Muted);
            FightUI.Hint(view.Switch.gameObject, "关闭只简化环境；棋盘、卡牌与对局操作保持可用。保存后，下次进入对局应用。");
            view.Reflow(); return view;
        }

        private void LateUpdate()
        {
            if (panel != null) Reflow();
        }

        internal void Reflow()
        {
            float density = Mathf.Max(1, 1 / Mathf.Max(.01f, panel.lossyScale.x));
            float width = panel.rect.width / density;
            bool narrow = width < 620;
            float height = (narrow ? 314 : Height) + (inDuel ? 30 : 0);
            float delta = height * density - panel.rect.height;
            if (Mathf.Approximately(previousWidth, width) && Mathf.Approximately(previousDensity, density) && Mathf.Abs(delta) < .01f) return;
            previousWidth = width; previousDensity = density;
            if (Mathf.Abs(delta) > .01f)
            {
                float oldBottom = -panel.anchoredPosition.y + panel.rect.height;
                var content = panel.parent as RectTransform;
                if (content != null)
                {
                    foreach (Transform sibling in content)
                    {
                        var rect = sibling as RectTransform;
                        if (rect != null && rect != panel && -rect.anchoredPosition.y >= oldBottom - 1)
                            rect.anchoredPosition -= new Vector2(0, delta);
                    }
                    content.sizeDelta += new Vector2(0, delta);
                }
                panel.sizeDelta = new Vector2(panel.rect.width, height * density);
            }
            FightUI.Place(inner, 0, 0, width, height); inner.localScale = new Vector3(density, density, 1);
            FightUI.ReflowSwitchRow(Switch, width, 54);
            float offset = inDuel ? 30 : 0;
            if (currentDuel != null) FightUI.Place(currentDuel.rectTransform, 0, 64, width, 26);
            float imageWidth = Mathf.Min(narrow ? 160 : 224, width * (narrow ? .34f : .28f)), textX = imageWidth + 20;
            preview.Layout(74 + offset, 0, imageWidth, imageWidth * 9 / 16);
            float statusWidth = Mathf.Min(164, (width - textX) * .42f);
            FightUI.Place(status.rectTransform, narrow ? textX : width - statusWidth, 74 + offset, narrow ? width - textX : statusWidth, 26);
            status.alignment = narrow ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;
            FightUI.Place(nameLabel.rectTransform, textX, (narrow ? 104 : 74) + offset, narrow ? width - textX : width - textX - statusWidth - 12, 34);
            FightUI.Place(description.rectTransform, textX, (narrow ? 144 : 116) + offset, width - textX, narrow ? 40 : 42);
            float chooseWidth = narrow ? width : Mathf.Min(196, width - textX);
            FightUI.Place(Choose.GetComponent<RectTransform>(), narrow ? 0 : textX, (narrow ? 194 : 170) + offset, chooseWidth, 44);
            var caption = Choose.GetComponentInChildren<TextMeshProUGUI>();
            FightUI.Place(caption.rectTransform, 10, 0, chooseWidth - 20, 44);
            caption.enableWordWrapping = false; caption.enableAutoSizing = true; caption.fontSizeMin = 16; caption.fontSizeMax = 19;
            FightUI.Place(hint.rectTransform, 0, (narrow ? 250 : 226) + offset, width, narrow ? 64 : 44);
        }
    }

    // This modal keeps one set of controls through display changes. Its
    // backdrop fills the drawable even when the page has a 16:9 letterbox.
    internal sealed class FightDuelSceneChooser : MonoBehaviour
    {
        private sealed class SceneCard
        {
            internal DuelSceneDefinition Definition;
            internal Button Button;
            internal FightDuelScenePreview Preview;
            internal TextMeshProUGUI Name, Style, Status, Check;
            internal RectTransform Accent;
            internal FightTooltip Hint;
        }
        private readonly List<SceneCard> cards = new List<SceneCard>();
        private RectTransform panel, backdrop, contents, detail, footer, headerLine;
        private ScrollRect cardScroll;
        private TextMeshProUGUI header, subtitle, sceneName, sceneStyle, sceneStatus, sceneDescription, scopeNote, footerHint;
        private Button close, cancel, confirm, overviewButton, duelButton, lastCard;
        private FightDuelScenePreview preview;
        private Func<Vector2> viewportSize;
        private Func<float> viewportScale;
        private Action layoutChanged;
        private string selectedId, savedId;
        private Vector2 previousSize;
        private float previousDensity;
        private bool compact, overview = true, inDuel;
        private GameObject focusedControl;
        private Selectable safeFocus;

        internal void Bind(RectTransform box, string current, string saved, bool enabled, bool inDuel,
            Action<string> accepted, Action cancelled, Func<Vector2> size, Func<float> scale,
            Action reflowed = null, RectTransform backdrop = null)
        {
            panel = box; selectedId = current; savedId = saved; this.inDuel = inDuel;
            viewportSize = size; viewportScale = scale; layoutChanged = reflowed; this.backdrop = backdrop;
            header = panel.GetComponentsInChildren<TextMeshProUGUI>().First(text => text.transform.parent == panel);
            header.enableWordWrapping = false; header.enableAutoSizing = true; header.fontSizeMin = 22; header.fontSizeMax = 27;
            close = panel.GetComponentsInChildren<Button>().First(button => button.transform.parent == panel);
            headerLine = panel.Cast<Transform>().First(child => child.name == "Divider") as RectTransform;
            FightUI.FocusKey(close, "scene:close");
            subtitle = FightUI.Text(panel, enabled ? "为下一场对局选择环境" : "3D 保持关闭，场景选择会保留", 20, 50, 800, 24, 16, FightUI.Muted);
            contents = FightUI.Node(panel, "Scene chooser contents", 20, 88, 1000, 400);
            FightUI.Scroll(contents, 0, 0, 350, 400, 400, out cardScroll);
            cardScroll.GetComponent<Image>().color = Color.clear;
            detail = FightUI.Node(contents, "Selected scene details", 370, 0, 600, 400);
            overviewButton = FightUI.FocusKey(FightUI.Button(detail, "场景全景", 0, 0, 200, 44, () => SetView(true)), "scene:view:overview");
            duelButton = FightUI.FocusKey(FightUI.Button(detail, "对局视角", 208, 0, 200, 44, () => SetView(false)), "scene:view:duel");
            preview = new FightDuelScenePreview(detail);
            sceneName = FightUI.Text(detail, "", 0, 0, 500, 32, 22, FightUI.Gold);
            sceneName.enableWordWrapping = false; sceneName.enableAutoSizing = true;
            sceneName.fontSizeMin = 16; sceneName.fontSizeMax = 24;
            sceneStyle = FightUI.Text(detail, "", 0, 0, 500, 24, 16, FightUI.Muted);
            sceneStatus = FightUI.Text(detail, "", 0, 0, 112, 24, 16, FightUI.Muted, TextAlignmentOptions.MidlineRight);
            sceneDescription = FightUI.Text(detail, "", 0, 0, 500, 44, 16, FightUI.Muted);
            scopeNote = FightUI.Text(detail, "", 0, 0, 500, 24, 16, FightUI.Muted);
            foreach (var definition in DuelSceneCatalog.Definitions)
            {
                var card = new SceneCard { Definition = definition };
                // Unavailable entries remain browsable. Only confirmation is
                // disabled, so keyboard users can read their status and preview.
                card.Button = FightUI.FocusKey(FightUI.Button(cardScroll.content, definition.Name, 2, 0, 344, 112,
                    () => SelectScene(definition.Id)), "scene:card:" + definition.Id);
                card.Button.GetComponentInChildren<TextMeshProUGUI>().gameObject.SetActive(false);
                card.Button.GetComponentInChildren<FightFrame>().InputFrame = true;
                card.Accent = FightUI.Panel(card.Button.transform, 3, 6, 3, 100, definition.AccentColor, false, "Scene accent");
                card.Accent.GetComponent<Image>().raycastTarget = false;
                card.Preview = new FightDuelScenePreview(card.Button.transform); card.Preview.Show(definition.Id, true);
                card.Name = FightUI.Text(card.Button.transform, definition.Name, 100, 8, 220, 26, 18, FightUI.Foreground);
                card.Name.enableWordWrapping = false; card.Name.enableAutoSizing = true; card.Name.fontSizeMin = 16; card.Name.fontSizeMax = 18;
                card.Style = FightUI.Text(card.Button.transform, definition.Style, 100, 42, 220, 24, 16, FightUI.Muted);
                card.Status = FightUI.Text(card.Button.transform, "", 100, 78, 196, 24, 16, FightUI.Muted);
                card.Check = FightUI.Text(card.Button.transform, "✓", 312, 78, 20, 24, 16, FightUI.Gold, TextAlignmentOptions.Center);
                card.Hint = FightUI.Hint(card.Button.gameObject, "");
                card.Hint.enabled = false;
                cards.Add(card);
            }
            footer = FightUI.Node(panel, "Scene chooser actions", 20, 600, 1000, 76);
            string hint = inDuel ? "当前对局不变；关闭对局设置时保存。" : "确认后仍需保存设置。";
            if (!enabled) hint = inDuel ? "保持 3D 关闭；关闭菜单时保存选择。" : "保持 3D 关闭；确认后仍需保存设置。";
            footerHint = FightUI.Text(footer, hint, 0, 0, 1000, 24, 16, FightUI.Muted);
            cancel = FightUI.FocusKey(FightUI.Button(footer, "取消", 620, 28, 160, 48, cancelled), "scene:cancel");
            confirm = FightUI.FocusKey(FightUI.Button(footer, enabled ? "使用此场景" : "保存场景选择", 800, 28, 200, 48,
                () => { if (DuelSceneCatalog.IsAvailable(DuelSceneCatalog.Find(selectedId))) accepted(selectedId); }, true), "scene:confirm");
            SelectScene(current); Reflow();
            lastCard = cards.FirstOrDefault(card => card.Definition.Id == current)?.Button ?? cards.FirstOrDefault()?.Button;
            RefreshFocusLinks();
            var initial = lastCard != null ? lastCard : cancel;
            safeFocus = initial;
            initial.Select(); FightUI.RevealControl(initial);
        }

        private void SetView(bool useOverview)
        {
            overview = useOverview;
            preview.Show(selectedId, overview);
            StyleViewButtons();
        }

        private void StyleViewButtons()
        {
            foreach (var button in new[] { overviewButton, duelButton })
            {
                bool selected = button == (overview ? overviewButton : duelButton);
                button.GetComponent<Outline>().effectColor = selected ? FightUI.Gold : FightUI.Border;
                button.GetComponent<Image>().color = selected ? FightUI.ActionFill : FightUI.Raised;
                button.GetComponentInChildren<FightFrame>().InputFrame = true;
            }
        }

        private void SelectScene(string id)
        {
            selectedId = id; preview.Show(id, overview);
            var definition = DuelSceneCatalog.Find(id);
            bool available = DuelSceneCatalog.IsAvailable(definition);
            sceneName.text = FightText.Get(definition?.Name ?? "场景不可用");
            sceneStyle.text = FightText.Get(definition?.Style ?? "请重新选择");
            sceneStatus.text = FightText.Get(available ? "当前选择" : "资源不可用");
            sceneStatus.color = available ? FightUI.Gold : FightUI.Danger;
            sceneDescription.text = FightText.Get(available ? definition.Description : "场景资源不可用。可继续浏览，暂时无法使用；原设置保留。");
            bool classic = definition?.Builder == DuelSceneCatalog.DefaultId;
            scopeNote.text = definition == null ? string.Empty : FightText.Get(classic ? "收藏场地外观" : "独立场景 · 专属演出与界面");
            FightUI.Hint(scopeNote.gameObject, classic ? "经典场地沿用双方的收藏外观。"
                : "场地、环境、天空、伙伴台与墓地采用场景专属设计，操作弹框随场景切换；卡套沿用收藏设置。");
            FightUI.Hint(sceneName.gameObject, definition?.Name ?? "场景不可用");
            FightUI.Hint(sceneStyle.gameObject, definition?.Style ?? "请重新选择");
            FightUI.Hint(sceneDescription.gameObject, sceneDescription.text);
            FightUI.Hint(sceneStatus.gameObject, sceneDescription.text);
            confirm.interactable = available;
            foreach (var card in cards)
            {
                bool selected = card.Definition.Id == id;
                var outline = card.Button.GetComponent<Outline>(); outline.enabled = true;
                outline.effectColor = selected ? FightUI.Gold : FightUI.Border;
                card.Button.GetComponent<Image>().color = selected ? FightUI.ActionFill : FightUI.Raised;
                card.Check.gameObject.SetActive(selected);
                bool installed = DuelSceneCatalog.IsAvailable(card.Definition);
                card.Status.text = FightText.Get(!installed ? "资源不可用" : card.Definition.Id == savedId
                    ? card.Definition.Id == DuelSceneCatalog.DefaultId ? inDuel ? "默认 · 当前设置" : "默认 · 已保存" : inDuel ? "当前设置" : "已保存"
                    : card.Definition.Id == DuelSceneCatalog.DefaultId ? "默认" : "");
                card.Status.color = installed ? FightUI.Muted : FightUI.Danger;
            }
            StyleViewButtons(); Canvas.ForceUpdateCanvases(); UpdateCardHints(); RefreshFocusLinks();
        }

        private void UpdateCardHints()
        {
            foreach (var card in cards)
            {
                // Use the rendered result, including name auto-sizing, rather
                // than displaying the same copy again on every focused card.
                card.Name.ForceMeshUpdate(); card.Style.ForceMeshUpdate(); card.Status.ForceMeshUpdate();
                bool available = DuelSceneCatalog.IsAvailable(card.Definition);
                bool needed = !available || card.Name.isTextTruncated || card.Style.isTextTruncated || card.Status.isTextTruncated;
                bool newlyEnabled = needed && !card.Hint.enabled;
                card.Hint.Hint = FightText.Get(card.Definition.Name) + " · " + FightText.Get(card.Definition.Style)
                    + (string.IsNullOrEmpty(card.Status.text) ? "" : " · " + card.Status.text)
                    + (available ? "" : "\n" + FightText.Get("场景资源不可用。可继续浏览，暂时无法使用；原设置保留。"));
                card.Hint.enabled = needed;
                if (newlyEnabled && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == card.Button.gameObject)
                    card.Hint.OnSelect(new BaseEventData(EventSystem.current));
            }
        }

        private void LateUpdate()
        {
            if (panel == null || !panel.gameObject.activeInHierarchy || viewportSize == null) return;
            Reflow();
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return;
            var current = eventSystem.currentSelectedGameObject;
            var control = current == null ? null : current.GetComponent<Selectable>();
            // A pointer click on the shade or image may clear Unity selection.
            // Restore the navigation position without submitting that control;
            // an actively selected scrollbar keeps its pointer interaction.
            if (control == null || !current.transform.IsChildOf(panel) || !control.IsActive() || !control.IsInteractable())
            {
                control = ValidFocus(safeFocus) ? safeFocus : ValidFocus(lastCard) ? lastCard : cancel;
                control.Select(); current = control.gameObject;
            }
            var card = cards.FirstOrDefault(item => item.Button == control);
            if (card != null && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                card.Hint.OnDeselect(new BaseEventData(eventSystem));
                card.Button.onClick.Invoke();
            }
            if (current == focusedControl) return;
            focusedControl = current;
            if (control == null || control is Scrollbar || !control.IsActive() || !control.IsInteractable()) return;
            safeFocus = control;
            if (card != null) { lastCard = card.Button; RefreshFocusLinks(); }
            FightUI.RevealControl(control);
        }

        private bool ValidFocus(Selectable control) => control != null && !(control is Scrollbar)
            && control.IsActive() && control.IsInteractable() && control.transform.IsChildOf(panel);

        private void Reflow()
        {
            Vector2 viewport = viewportSize();
            float scale = Mathf.Max(.01f, viewportScale());
            float density = Mathf.Max(1, 1 / scale);
            var drawable = new Vector2(Mathf.Max(1, Screen.width) / scale, Mathf.Max(1, Screen.height) / scale);
            var size = new Vector2(Mathf.Min(1240, drawable.x / density - 48), Mathf.Min(820, drawable.y / density - 48));
            // The main modal is the shade's child. Resizing its shade changes
            // that local origin; duel menus have the shade as a sibling instead.
            if (backdrop != null) FightUI.Place(backdrop, (viewport.x - drawable.x) / 2, (viewport.y - drawable.y) / 2, drawable.x, drawable.y);
            Vector2 parentSize = backdrop != null && panel.parent == backdrop ? drawable : viewport;
            FightUI.Place(panel, (parentSize.x - size.x * density) / 2, (parentSize.y - size.y * density) / 2, size.x, size.y);
            panel.localScale = new Vector3(density, density, 1);
            if (size == previousSize && Mathf.Approximately(density, previousDensity)) return;
            var current = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            previousSize = size; previousDensity = density; compact = size.x < 1000 || size.y < 600;
            float inset = compact ? 20 : 24;
            float width = size.x - inset * 2, contentY = compact ? 88 : 112;
            float height = size.y - contentY - (compact ? 100 : 106);
            FightUI.Place(header.rectTransform, inset, compact ? 12 : 20, width - 60, 36);
            FightUI.Place(close.GetComponent<RectTransform>(), size.x - inset - 44, compact ? 12 : 18, 44, 44);
            FightUI.Place(headerLine, inset, compact ? 80 : 72, width, 1);
            FightUI.Place(subtitle.rectTransform, inset, compact ? 50 : 80, width, 24);
            FightUI.Place(contents, inset, contentY, width, height);
            FightUI.Place(footer, inset, size.y - (compact ? 88 : 94), width, compact ? 76 : 82);
            FightUI.Place(footerHint.rectTransform, 0, 0, width, compact ? 24 : 30);
            float actionY = compact ? 28 : 34, actionWidth = Mathf.Min(200, (width - 16) / 2), cancelWidth = Mathf.Min(160, (width - 16) / 2);
            LayoutButton(cancel, width - actionWidth - cancelWidth - 16, actionY, cancelWidth, 48);
            LayoutButton(confirm, width - actionWidth, actionY, actionWidth, 48);
            float listWidth = compact ? size.x < 700 ? 248 : 280 : 360;
            float gap = compact ? 16 : 28, detailWidth = width - listWidth - gap;
            float rowHeight = compact ? size.x < 700 || height < 280 ? 76 : 80 : 112;
            float rowGap = compact ? 6 : 12, pitch = rowHeight + rowGap;
            float listHeight = cards.Count * pitch - (cards.Count > 0 ? rowGap : 0) + 4;
            ResizeScroll(cardScroll, 0, 0, listWidth, height, listHeight);
            FightUI.Place(detail, listWidth + gap, 0, detailWidth, height);
            for (int index = 0; index < cards.Count; index++)
            {
                var card = cards[index]; float cardWidth = listWidth - 22;
                FightUI.Place(card.Button.GetComponent<RectTransform>(), 2, index * pitch + 2, cardWidth, rowHeight);
                FightUI.Place(card.Accent, 3, 4, 3, rowHeight - 8);
                float thumbnail = compact ? rowHeight == 76 ? 40 : 52 : 88;
                float textX = thumbnail + 16, textWidth = cardWidth - textX - 8;
                card.Preview.Layout((rowHeight - thumbnail * 9 / 16) / 2, 8, thumbnail, thumbnail * 9 / 16);
                float nameY = compact ? rowHeight == 76 ? 2 : 4 : 8;
                float styleY = compact ? nameY + 24 : 44, statusY = compact ? nameY + 48 : 80;
                card.Name.fontSize = compact ? 16 : 18; card.Name.fontSizeMax = compact ? 16 : 18;
                FightUI.Place(card.Name.rectTransform, textX, nameY, textWidth, compact ? 24 : 28);
                FightUI.Place(card.Style.rectTransform, textX, styleY, textWidth, 24);
                FightUI.Place(card.Status.rectTransform, textX, statusY, textWidth - 24, 24);
                FightUI.Place(card.Check.rectTransform, cardWidth - 26, statusY, 20, 24);
            }
            float badgeWidth = Mathf.Min(112, detailWidth * .4f);
            if (compact)
            {
                FightUI.Place(sceneName.rectTransform, 0, 0, detailWidth - badgeWidth - 8, 28);
                FightUI.Place(sceneStatus.rectTransform, detailWidth - badgeWidth, 2, badgeWidth, 24);
                FightUI.Place(sceneStyle.rectTransform, 0, 34, detailWidth, 24);
                LayoutButton(overviewButton, 0, 68, (detailWidth - 8) / 2, 44);
                LayoutButton(duelButton, (detailWidth + 8) / 2, 68, (detailWidth - 8) / 2, 44);
                float imageHeight = Mathf.Max(0, Mathf.Min(detailWidth * 9 / 16, height - 152));
                float imageWidth = imageHeight * 16 / 9;
                preview.Root.gameObject.SetActive(imageHeight >= 32);
                preview.Layout(124, (detailWidth - imageWidth) / 2, imageWidth, imageHeight);
                sceneDescription.gameObject.SetActive(false);
                FightUI.Place(scopeNote.rectTransform, 0, height - 24, detailWidth, 24);
            }
            else
            {
                LayoutButton(overviewButton, 0, 0, (detailWidth - 12) / 2, 44);
                LayoutButton(duelButton, (detailWidth + 12) / 2, 0, (detailWidth - 12) / 2, 44);
                float imageHeight = Mathf.Max(0, Mathf.Min(detailWidth * 9 / 16, height - 214));
                float imageWidth = imageHeight * 16 / 9;
                preview.Root.gameObject.SetActive(imageHeight >= 32);
                preview.Layout(56, (detailWidth - imageWidth) / 2, imageWidth, imageHeight);
                float imageBottom = 56 + imageHeight;
                FightUI.Place(sceneName.rectTransform, 0, imageBottom + 12, detailWidth - badgeWidth - 8, 32);
                FightUI.Place(sceneStatus.rectTransform, detailWidth - badgeWidth, imageBottom + 16, badgeWidth, 24);
                FightUI.Place(sceneStyle.rectTransform, 0, imageBottom + 50, detailWidth, 24);
                sceneDescription.gameObject.SetActive(true);
                FightUI.Place(sceneDescription.rectTransform, 0, imageBottom + 82, detailWidth, 40);
                FightUI.Place(scopeNote.rectTransform, 0, height - 24, detailWidth, 24);
            }
            StyleViewButtons(); Canvas.ForceUpdateCanvases(); UpdateCardHints(); layoutChanged?.Invoke(); RefreshFocusLinks();
            if (current != null && current.activeInHierarchy && current.transform.IsChildOf(panel))
            {
                var control = current.GetComponent<Selectable>();
                if (control != null && !(control is Scrollbar) && control.IsInteractable()) FightUI.RevealControl(control);
            }
        }

        private static void ResizeScroll(ScrollRect scroll, float x, float y, float width, float height, float contentHeight)
        {
            FightUI.Place(scroll.GetComponent<RectTransform>(), x, y, width, height);
            FightUI.Place(scroll.viewport, 0, 0, width, height);
            scroll.content.sizeDelta = new Vector2(width, Mathf.Max(height, contentHeight));
            FightUI.Place(scroll.verticalScrollbar.GetComponent<RectTransform>(), width - 15, 2, 14, height - 4);
            var track = scroll.verticalScrollbar.transform.Find("Track") as RectTransform;
            if (track != null) FightUI.Place(track, 10, 0, 4, height - 4);
        }

        private static void LayoutButton(Button button, float x, float y, float width, float height)
        {
            FightUI.Place(button.GetComponent<RectTransform>(), x, y, width, height);
            var caption = button.GetComponentInChildren<TextMeshProUGUI>();
            FightUI.Place(caption.rectTransform, 10, 0, width - 20, height);
            caption.enableWordWrapping = false; caption.enableAutoSizing = true; caption.fontSizeMin = 16; caption.fontSizeMax = 19;
        }

        private static void Link(Selectable control, Selectable up, Selectable down, Selectable left, Selectable right)
            => control.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = up, selectOnDown = down, selectOnLeft = left, selectOnRight = right };

        private void RefreshFocusLinks()
        {
            if (cancel == null) return;
            var retained = lastCard != null ? lastCard : cards.FirstOrDefault()?.Button ?? cancel;
            var action = confirm.IsInteractable() ? confirm : cancel;
            for (int index = 0; index < cards.Count; index++)
                Link(cards[index].Button, index == 0 ? close : cards[index - 1].Button,
                    index == cards.Count - 1 ? action : cards[index + 1].Button, cancel, overviewButton);
            Link(close, cancel, retained, retained, overviewButton);
            Link(overviewButton, close, retained, retained, duelButton);
            Link(duelButton, close, retained, overviewButton, action);
            Link(cancel, retained, close, retained, confirm.IsInteractable() ? confirm : overviewButton);
            Link(confirm, retained, close, cancel, duelButton);
        }
    }
}
