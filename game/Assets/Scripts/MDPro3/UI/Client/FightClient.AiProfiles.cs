using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using MDPro3.AI;
using MDPro3.Servant;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace MDPro3.UI.Client
{
    internal partial class FightClient
    {
        private AiDuelistProfile aiProfileDraft;
        private readonly List<AiDuelistProfile> aiProfiles = new List<AiDuelistProfile>();
        private bool aiAppearance;
        private sealed class AiProfileTestResult { internal string Request, Status, Detail; }
        private readonly Dictionary<string, AiProfileTestResult> aiProfileTests = new Dictionary<string, AiProfileTestResult>();
        private static readonly string[] AiProfileAppearanceCategories = { "Face", "Frame", "Protector", "Field", "Mate", "Character", "Grave", "Stand", "Case" };
        private static readonly string[] AiProfileAppearanceLabels = { "头像", "头像框", "卡套", "决斗场地", "伙伴", "角色", "墓地", "伙伴底座", "卡盒" };

        internal void OpenAiProfileSettings(string id, string origin = null)
        {
            OpenSettings("ai", origin);
            if (string.IsNullOrEmpty(id) || !aiProfiles.Any(profile => profile.Id == id) || aiProfileDraft?.Id == id) { RenderSettings(); return; }
            ChangeAiProfile(id);
        }

        private void LoadAiProfilesDraft()
        {
            string current = aiProfileDraft?.Id;
            aiProfiles.Clear(); aiProfileDraft = null;
            try
            {
                aiProfiles.AddRange(AiDuelistProfileStore.LoadAll());
                var selected = aiProfiles.FirstOrDefault(p => p.Id == current);
                if (selected == null)
                {
                    var choices = ResponsesAiConfigStore.Load();
                    selected = aiProfiles.FirstOrDefault(p => p.Id == choices.ProfileB) ?? aiProfiles.FirstOrDefault();
                }
                if (selected == null) throw new AiConfigurationException("尚无 AI 分身，请新建一个分身。");
                SetAiProfileDraft(selected);
            }
            catch (AiConfigurationException error)
            {
                aiLoadError = error.Message; aiSettingsDraft = new ResponsesAiConfig();
                ResetAiServiceDraft();
            }
        }

        private void SetAiProfileDraft(AiDuelistProfile profile)
        {
            CancelSettingsTest(); aiProfileDraft = profile.Clone(); aiSettingsDraft = aiProfileDraft.Config.Clone();
            aiLoadError = null; settingsSaveError = null;
            foreach (var key in settingsErrors.Keys.Where(IsAiSettingsKey).ToArray()) settingsErrors.Remove(key);
            ResetAiServiceDraft();
        }
        private void ResetAiServiceDraft()
        {
            aiTimeout = aiSettingsDraft.TimeoutSeconds.ToString(); aiOutput = aiSettingsDraft.MaxOutputTokens.ToString(); aiBudget = aiSettingsDraft.PromptBudgetTokens.ToString();
            aiHeaders.Clear(); visibleHeaders.Clear(); aiHeaderIds.Clear(); aiKeyVisible = false;
            try { aiHeaders.AddRange(AiConfigForm.ReadHeaders(aiSettingsDraft.ExtraHeadersJson)); }
            catch (AiConfigurationException error) { settingsErrors["headers"] = error.Message; }
            aiSettingsOriginal = AiDraftSignature();
            aiTestStatus = aiProfileDraft != null && aiProfileTests.TryGetValue(aiProfileDraft.Id, out var result)
                && result.Request == AiRequestSignature(aiSettingsDraft) ? result.Detail : "尚未测试";
        }
        private string AiProfileDraftSignature() => aiProfileDraft == null ? "" : aiProfileDraft.Id + "\n" + aiProfileDraft.Name + "\n"
            + string.Join("\n", aiProfileDraft.Appearance.OrderBy(pair => pair.Key).Select(pair => pair.Key + "=" + pair.Value));
        private bool AiProfileDirty => aiSettingsDraft != null && AiDraftSignature() != aiSettingsOriginal;
        private void RememberAiProfileTest(ResponsesAiConfig config, string status, string detail)
        {
            if (aiProfileDraft != null) aiProfileTests[aiProfileDraft.Id] = new AiProfileTestResult { Request = AiRequestSignature(config), Status = status, Detail = detail };
        }
        internal string GetAiProfileStatus(AiDuelistProfile profile)
        {
            if (profile == null) return "未选择";
            try { profile.Config.Validate(); } catch (AiConfigurationException) { return "未配置"; }
            return aiProfileTests.TryGetValue(profile.Id, out var result) && result.Request == AiRequestSignature(profile.Config)
                ? result.Status : "已配置未测试";
        }
        private void CommitAiProfileDraft(ResponsesAiConfig config)
        {
            if (aiProfileDraft == null) throw new AiConfigurationException("请先选择一个 AI 分身。");
            var next = aiProfileDraft.Clone(); next.Name = next.Name.Trim(); next.Config = config.Clone();
            AiDuelistProfileStore.Save(next); aiProfileDraft = next; aiSettingsDraft = config;
            aiSettingsOriginal = AiDraftSignature(); aiLoadError = null;
            aiProfiles.Clear(); aiProfiles.AddRange(AiDuelistProfileStore.LoadAll());
        }
        private bool SaveAiProfileDraft()
        {
            var next = ReadAiSettings(false); if (next == null) return false;
            try { CommitAiProfileDraft(next); UpdateSettingsDirtyLabel(); return true; }
            catch (Exception error) { settingsSaveError = error.Message; ShowStatus("分身保存失败：" + error.Message); UpdateSettingsDirtyLabel(); return false; }
        }
        private void ContinueAiProfileChange(Action continued)
        {
            if (!AiProfileDirty) { continued(); return; }
            var box = ShowDialog("当前分身尚未保存", 650, 278);
            var text = FightUI.Text(box, "「" + aiProfileDraft.Name + "」", 28, 84, 594, 84, 18, FightUI.Foreground);
            text.richText = false; text.enableWordWrapping = true;
            FightUI.Button(box, "继续编辑", 28, 200, 164, 48, CloseDialog);
            FightUI.Button(box, "放弃并继续", 208, 200, 164, 48, () => { CloseDialog(); continued(); });
            FightUI.Button(box, "保存并继续", 388, 200, 234, 48, () => { CloseDialog(); if (SaveAiProfileDraft()) continued(); }, true);
        }
        private void ChangeAiProfile(string id)
        {
            if (aiProfileDraft?.Id == id) { CloseDialog(); return; }
            ContinueAiProfileChange(() => Try(() =>
            {
                var selected = AiDuelistProfileStore.Get(id);
                if (selected == null) throw new AiConfigurationException("这个分身已删除，请重新选择。");
                CloseDialog(); SetAiProfileDraft(selected); settingsScrollPosition = 1; RenderSettings();
            }));
        }
        private void NewAiProfile()
        {
            ContinueAiProfileChange(() => Try(() =>
            {
                CloseDialog(); var profile = AiDuelistProfileStore.Create();
                aiProfiles.Clear(); aiProfiles.AddRange(AiDuelistProfileStore.LoadAll());
                aiAppearance = aiAdvanced = false; SetAiProfileDraft(profile); settingsScrollPosition = 1; RenderSettings();
                FocusRenderedSettingsField("profileName"); ShowStatus("已新建 AI 分身。");
            }));
        }
        private void DuplicateAiProfile()
        {
            if (aiProfileDraft == null) return;
            string id = aiProfileDraft.Id;
            ContinueAiProfileChange(() => Try(() =>
            {
                CloseDialog(); var profile = AiDuelistProfileStore.Duplicate(id);
                aiProfiles.Clear(); aiProfiles.AddRange(AiDuelistProfileStore.LoadAll());
                SetAiProfileDraft(profile); settingsScrollPosition = 1; RenderSettings(); FocusRenderedSettingsField("profileName");
                ShowStatus("AI 分身已复制。");
            }));
        }
        private void DeleteAiProfile()
        {
            if (aiProfileDraft == null || aiProfiles.Count < 2) return;
            string id = aiProfileDraft.Id;
            var box = ShowDialog("删除 AI 分身", 620, 268);
            var text = FightUI.Text(box, "删除「" + aiProfileDraft.Name + "」及其 AI 配置、外观和已记住的凭据？", 28, 84, 564, 86, 18);
            text.richText = false; text.enableWordWrapping = true;
            FightUI.Button(box, "保留分身", 252, 196, 160, 48, CloseDialog);
            var remove = FightUI.Button(box, "删除分身", 428, 196, 164, 48, () => Try(() =>
            {
                AiDuelistProfileStore.Delete(id); aiProfileTests.Remove(id);
                aiProfiles.Clear(); aiProfiles.AddRange(AiDuelistProfileStore.LoadAll());
                CloseDialog(); SetAiProfileDraft(aiProfiles[0]); settingsScrollPosition = 1; RenderSettings(); ShowStatus("分身已删除。");
            }));
            remove.GetComponent<Outline>().effectColor = FightUI.Danger;
        }
        private void ShowAiProfileRoster()
        {
            Try(() => { aiProfiles.Clear(); aiProfiles.AddRange(AiDuelistProfileStore.LoadAll()); });
            float width = Mathf.Min(850, W - 48), height = Mathf.Min(650, H - 48), inner = width - 56;
            var box = ShowDialog("选择 AI 分身", width, height);
            float viewportHeight = height - 172;
            var list = FightUI.Scroll(box, 28, 144, inner, viewportHeight, viewportHeight, out var scroll);
            Action<string> draw = query =>
            {
                FightUI.Clear(list); var items = aiProfiles.Where(p => CollectionMatches(p.Name, query, p.Config.Model)).ToArray();
                float cardWidth = (inner - 14) / 2;
                for (int i = 0; i < items.Length; i++)
                {
                    var item = items[i]; bool selected = item.Id == aiProfileDraft?.Id;
                    var card = FightUI.Button(list, "", i % 2 * (cardWidth + 14), i / 2 * 110, cardWidth, 96, () => ChangeAiProfile(item.Id));
                    card.name = "AI profile roster " + item.Id; FightUI.FocusKey(card, "settings:ai:profile:" + item.Id);
                    card.GetComponent<Image>().color = selected ? FightUI.Raised : FightUI.PanelColor;
                    card.GetComponent<Outline>().effectColor = selected ? FightUI.Gold : FightUI.Border;
                    AiProfilePortrait(card.transform, item, 12, 14, 66);
                    var name = FightUI.Text(card.transform, item.Name, 92, 16, cardWidth - 132, 34, 20, selected ? FightUI.Gold : FightUI.Foreground);
                    name.richText = false; name.enableAutoSizing = true; name.fontSizeMin = 15; name.fontSizeMax = 20;
                    var model = FightUI.Text(card.transform, item.Config.Model, 92, 52, cardWidth - 106, 24, 14, FightUI.Muted); model.richText = false; model.overflowMode = TextOverflowModes.Ellipsis;
                    if (selected) FightUI.Icon(card.transform, "check", cardWidth - 32, 24, 16, 16, FightUI.Active);
                    FightUI.Hint(card.gameObject, item.Name + " · " + item.Config.Model);
                }
                if (items.Length == 0) FightUI.Text(list, "没有匹配的分身", 0, 20, inner, 52, 20, FightUI.Muted, TextAlignmentOptions.Center);
                list.sizeDelta = new Vector2(inner, Mathf.Max(viewportHeight, Mathf.Ceil(items.Length / 2f) * 110)); scroll.verticalNormalizedPosition = 1;
            };
            FightUI.FocusKey(FightUI.Input(box, "", "搜索分身名称或模型", 28, 84, inner - 196, 44, draw), "settings:ai:profile-search");
            FightUI.Button(box, "+ 新建分身", width - 208, 84, 180, 44, NewAiProfile, true);
            draw("");
        }
        private void RenderAiProfileSettings(Transform parent, float width, ref float y)
        {
            if (aiProfileDraft == null)
            {
                y += SettingsWrappedText(parent, aiLoadError ?? "请新建一个 AI 分身。", 0, y, width, 68, 18, FightUI.Danger).rectTransform.sizeDelta.y + 18;
                FightUI.Button(parent, "重新读取", 0, y, 180, 44, () => { LoadAiProfilesDraft(); RenderSettings(); });
                FightUI.Button(parent, "+ 新建分身", 196, y, 180, 44, NewAiProfile); y += 64; return;
            }
            var identity = FightUI.Panel(parent, 0, y, width, 116, new Color(.045f, .078f, .101f, .72f), true, "AI profile identity");
            identity.GetComponent<Outline>().effectColor = FightUI.GoldBorder;
            AiProfilePortrait(identity, aiProfileDraft, 20, 16, 84);
            var name = FightUI.FocusKey(FightUI.Input(identity, aiProfileDraft.Name, "给这个分身取个名字", 124, 16, width - 148, 44,
                value => { aiProfileDraft.Name = value; settingsErrors.Remove("profileName"); ClearSettingsFieldError("profileName"); UpdateSettingsDirtyLabel(); }), "settings:ai:profile-name");
            name.characterLimit = 64; settingsInputs["profileName"] = name; settingsFieldAnchors["profileName"] = name.GetComponent<RectTransform>();
            float buttonWidth = (width - 148 - 30) / 4;
            FightUI.FocusKey(FightUI.Button(identity, "切换分身", 124, 74, buttonWidth, 32, ShowAiProfileRoster), "settings:ai:profile-switch");
            FightUI.FocusKey(FightUI.Button(identity, "+ 新建", 134 + buttonWidth, 74, buttonWidth, 32, NewAiProfile), "settings:ai:profile-create");
            FightUI.FocusKey(FightUI.Button(identity, "复制分身", 144 + buttonWidth * 2, 74, buttonWidth, 32, DuplicateAiProfile), "settings:ai:profile-duplicate");
            var remove = FightUI.FocusKey(FightUI.Button(identity, "删除", 154 + buttonWidth * 3, 74, buttonWidth, 32, DeleteAiProfile, false, aiProfiles.Count > 1), "settings:ai:profile-delete");
            FightUI.Hint(remove.gameObject, aiProfiles.Count > 1 ? "删除当前分身及其配置和凭据。" : "至少保留一个 AI 分身。");
            y += 128; RenderSettingsError(parent, width, "profileName", ref y);
            FightUI.Tab(parent, "AI 连接", !aiAdvanced && !aiAppearance, 0, y, 160, 44, () => { aiAppearance = aiAdvanced = false; settingsScrollPosition = 1; RenderSettings(); }, settingsTest == null);
            FightUI.Tab(parent, "高级参数", aiAdvanced && !aiAppearance, 172, y, 160, 44, () => { aiAppearance = false; aiAdvanced = true; settingsScrollPosition = 1; RenderSettings(); }, settingsTest == null);
            FightUI.Tab(parent, "分身外观", aiAppearance, 344, y, 160, 44, () => { aiAppearance = true; aiAdvanced = false; settingsScrollPosition = 1; RenderSettings(); }, settingsTest == null);
            FightUI.Line(parent, 0, y + 52, width, FightUI.Border); y += 76;
            if (aiAppearance) RenderAiProfileAppearance(parent, width, ref y); else RenderAiSettings(parent, width, ref y);
        }
        private static string AiProfileAppearanceValue(AiDuelistProfile profile, string category)
            => profile?.Appearance != null && profile.Appearance.TryGetValue(category, out var value) ? value : "0";
        private void AiProfilePortrait(Transform parent, AiDuelistProfile profile, float x, float y, float size)
        {
            var node = FightUI.Panel(parent, x, y, size, size, new Color(.035f, .070f, .095f, 1), true, "AI profile portrait");
            node.GetComponent<Outline>().effectColor = FightUI.GoldBorder;
            float iconSize = size * .38f;
            FightUI.Icon(node, "diamond", (size - iconSize) / 2, (size - iconSize) / 2, iconSize, iconSize, FightUI.Active);
            AiProfileItemImage(node, "Face", AiProfileAppearanceValue(profile, "Face"), 3, 3, size - 6, size - 6);
            AiProfileItemImage(node, "Frame", AiProfileAppearanceValue(profile, "Frame"), 0, 0, size, size);
        }
        private void RenderAiProfileAppearance(Transform parent, float width, ref float y)
        {
            int columns = width >= 800 ? 3 : 2;
            float cellWidth = (width - (columns - 1) * 16) / columns;
            for (int i = 0; i < AiProfileAppearanceCategories.Length; i++)
            {
                string category = AiProfileAppearanceCategories[i], label = AiProfileAppearanceLabels[i], value = AiProfileAppearanceValue(aiProfileDraft, category);
                var tile = FightUI.Button(parent, "", i % columns * (cellWidth + 16), y + i / columns * 156, cellWidth, 140, () => ChooseAiProfileAppearance(category, label));
                tile.name = "AI profile appearance " + category; FightUI.FocusKey(tile, "settings:ai:appearance:" + category);
                tile.GetComponent<Image>().color = new Color(.043f, .073f, .094f, .65f); tile.GetComponent<Outline>().effectColor = FightUI.Border;
                AiProfileItemImage(tile.transform, category, value, 14, 20, 74, 98);
                var title = FightUI.Text(tile.transform, label + "  ›", 108, 22, cellWidth - 120, 30, 19, FightUI.Gold);
                var caption = FightUI.Text(tile.transform, AiProfileAppearanceName(category, value), 108, 60, cellWidth - 120, 60, 16, FightUI.Foreground);
                caption.richText = false; caption.enableWordWrapping = true; caption.enableAutoSizing = true; caption.fontSizeMin = 13; caption.fontSizeMax = 16; caption.maxVisibleLines = 3;
                FightUI.Hint(tile.gameObject, label + "：" + AiProfileAppearanceName(category, value) + "，点击更换。");
            }
            y += Mathf.Ceil(AiProfileAppearanceCategories.Length / (float)columns) * 156 + 12;
        }
        private string AiProfileAppearanceName(string category, string value)
        {
            if (value == "0") return category == "Mate" || category == "Stand" ? "不显示" : "使用默认";
            if (category == "Character") return CharacterName(value);
            return AppearanceValueName(category, value);
        }
        private void ChooseAiProfileAppearance(string category, string label)
        {
            if (category == "Character")
            {
                var data = CharacterSelector.characters;
                if (data == null) { ShowStatus("角色资源正在加载，请稍后重试。"); return; }
                var characters = Enumerable.Range(0, 10).SelectMany(i => data.GetSeriesCharacters(i.ToString("00")))
                    .Where(c => !c.notReady && CollectionSpriteExists("sn" + c.id)).ToArray();
                var ids = new[] { "0" }.Concat(characters.Select(c => c.id)).ToArray();
                var names = new[] { "使用默认角色" }.Concat(characters.Select(c => data.GetName(c.id))).ToArray();
                SettingsChoose("分身角色", ids, names, value => { aiProfileDraft.Appearance[category] = value; UpdateSettingsDirtyLabel(); RenderSettings(); }, AiProfileAppearanceValue(aiProfileDraft, category)); return;
            }
            float width = Mathf.Min(850, W - 48), height = Mathf.Min(680, H - 48), inner = width - 56;
            var box = ShowDialog("选择分身" + label, width, height);
            var options = new List<Items.Item> { new Items.Item { id = 0 } };
            options.AddRange(AvailableCollectionItems(AppearanceItemType(category)));
            float viewportHeight = height - 172;
            var list = FightUI.Scroll(box, 28, 144, inner, viewportHeight, viewportHeight, out var scroll);
            Action<string> draw = query =>
            {
                FightUI.Clear(list);
                var items = options.Where(i => CollectionMatches(i.id == 0 ? AiProfileAppearanceName(category, "0") : CollectionDisplayName(i), query, i.id.ToString())).ToArray();
                int columns = 4; float cellWidth = (inner - 36) / columns, cellHeight = category == "Protector" ? 194 : 164;
                for (int i = 0; i < items.Length; i++)
                {
                    var item = items[i]; string id = item.id.ToString(); bool selected = id == AiProfileAppearanceValue(aiProfileDraft, category);
                    var tile = FightUI.Button(list, "", i % columns * (cellWidth + 12), i / columns * (cellHeight + 12), cellWidth, cellHeight,
                        () => { aiProfileDraft.Appearance[category] = id; CloseDialog(); UpdateSettingsDirtyLabel(); RenderSettings(); });
                    FightUI.FocusKey(tile, "settings:ai:appearance-item:" + category + ":" + id);
                    tile.GetComponent<Image>().color = selected ? FightUI.Raised : FightUI.PanelColor;
                    tile.GetComponent<Outline>().effectColor = selected ? FightUI.Gold : FightUI.Border;
                    float artHeight = cellHeight - 62;
                    AiProfileItemImage(tile.transform, category, id, 8, 8, cellWidth - 16, artHeight - 10);
                    var caption = FightUI.Text(tile.transform, AiProfileAppearanceName(category, id), 6, artHeight + 4, cellWidth - 12, 54, 15, selected ? FightUI.Active : FightUI.Foreground, TextAlignmentOptions.Center);
                    caption.richText = false; caption.enableWordWrapping = true; caption.enableAutoSizing = true; caption.fontSizeMin = 12; caption.fontSizeMax = 15;
                    FightUI.Hint(tile.gameObject, AiProfileAppearanceName(category, id));
                }
                if (items.Length == 0) FightUI.Text(list, "没有匹配素材", 0, 20, inner, 52, 18, FightUI.Muted, TextAlignmentOptions.Center);
                list.sizeDelta = new Vector2(inner, Mathf.Max(viewportHeight, Mathf.Ceil(items.Length / 4f) * (cellHeight + 12))); scroll.verticalNormalizedPosition = 1;
            };
            FightUI.FocusKey(FightUI.Input(box, "", "搜索素材名称或编号", 28, 84, inner, 44, draw), "settings:ai:appearance-search");
            draw("");
        }
        private void AiProfileItemImage(Transform parent, string category, string id, float x, float y, float width, float height)
        {
            var node = FightUI.Node(parent, "AI profile item image " + category, x, y, width, height);
            var image = node.gameObject.AddComponent<Image>(); image.preserveAspect = true; image.raycastTarget = false; image.color = Color.clear;
            if (id == "0")
            {
                if (category == "Face" || category == "Frame") return;
                float size = Mathf.Min(width, height) * .3f;
                FightUI.Icon(node, "diamond", (width - size) / 2, (height - size) / 2, size, size, FightUI.GoldBorder); return;
            }
            node.gameObject.AddComponent<FightAiProfileImage>().Load(image, id, category, AppearanceItemType(category));
        }
    }
    internal sealed class FightAiProfileImage : MonoBehaviour
    {
        private AsyncOperationHandle<Sprite> characterHandle;
        private bool hasCharacterHandle;
        private Texture2D protectorTexture;
        private Sprite protectorSprite;
        private static readonly SemaphoreSlim ProtectorLoader = new SemaphoreSlim(1, 1);
        internal async void Load(Image image, string id, string category, Items.ItemType type)
        {
            try
            {
                await UniTask.NextFrame();
                while (image != null && !ProfileImageVisible(image.rectTransform)) await UniTask.Delay(100, ignoreTimeScale: true);
                if (this == null || image == null) return;
                Sprite sprite;
                if (category == "Character")
                {
                    characterHandle = Addressables.LoadAssetAsync<Sprite>("sn" + id); hasCharacterHandle = true; sprite = await characterHandle;
                }
                else if (category == "Protector") sprite = await LoadProtectorIcon(id);
                else sprite = await Program.items.TryLoadItemIconAsync(id, type);
                if (this == null || image == null) return;
                if (sprite != null) { image.sprite = sprite; image.color = Color.white; }
                else Placeholder(image);
            }
            catch { if (this != null && image != null) Placeholder(image); }
        }
        private static bool ProfileImageVisible(RectTransform image)
        {
            var scroll = image.GetComponentInParent<ScrollRect>(); if (scroll == null || scroll.viewport == null) return true;
            var corners = new Vector3[4]; image.GetWorldCorners(corners);
            var lower = scroll.viewport.InverseTransformPoint(corners[0]); var upper = scroll.viewport.InverseTransformPoint(corners[2]);
            return scroll.viewport.rect.Overlaps(Rect.MinMaxRect(lower.x, lower.y, upper.x, upper.y));
        }
        private static void Placeholder(Image image)
        {
            image.color = FightUI.Raised;
            var text = FightUI.Text(image.transform, "暂无预览", 0, 0, image.rectTransform.rect.width, image.rectTransform.rect.height, 13, FightUI.Muted, TextAlignmentOptions.Center); text.raycastTarget = false;
        }
        private async UniTask<Sprite> LoadProtectorIcon(string id)
        {
            await ProtectorLoader.WaitAsync();
            try
            {
                if (this == null) return null;
                string name = "ProtectorIcon" + id, path = Program.root + "MasterDuel/Protector/" + id + "/" + name;
                Texture2D copy = null;
                if (File.Exists(path))
                {
                    var bundle = await AssetBundle.LoadFromFileAsync(path);
                    if (bundle != null)
                    {
                        try
                        {
                            var request = bundle.LoadAllAssetsAsync(); await request;
                            if (this == null) return null;
                            var sprite = request.allAssets.OfType<Sprite>().OrderByDescending(value => value.name == name).FirstOrDefault();
                            if (sprite != null)
                            {
                                Rect region = sprite.rect; try { region = sprite.textureRect; } catch (UnityException) { }
                                copy = CopyProtectorTexture(sprite.texture, region);
                            }
                            else
                            {
                                var texture = request.allAssets.OfType<Texture2D>().OrderByDescending(value => value.name == name).FirstOrDefault();
                                if (texture != null) copy = CopyProtectorTexture(texture, new Rect(0, 0, texture.width, texture.height));
                            }
                        }
                        finally { bundle.Unload(true); }
                    }
                }
                if (copy == null)
                {
                    var material = await ABLoader.LoadProtectorMaterial(id, CancellationToken.None);
                    if (this == null) return null;
                    var source = material == null ? null : material.GetTexturePropertyNames()
                        .OrderByDescending(value => value.IndexOf("Protector", StringComparison.OrdinalIgnoreCase) >= 0 || value == "_MainTex")
                        .Select(material.GetTexture).FirstOrDefault(value => value != null && value.width > 4 && value.height > 4);
                    if (source != null) copy = CopyProtectorTexture(source, new Rect(0, 0, source.width, source.height));
                }
                if (this == null) { if (copy != null) Destroy(copy); return null; }
                if (copy == null) return await Program.items.TryLoadItemIconAsync(id, Items.ItemType.Protector);
                protectorTexture = copy; protectorSprite = Sprite.Create(copy, new Rect(0, 0, copy.width, copy.height), new Vector2(.5f, .5f));
                return protectorSprite;
            }
            finally { ProtectorLoader.Release(); }
        }
        private static Texture2D CopyProtectorTexture(Texture source, Rect region)
        {
            float scale = Mathf.Min(1, 384f / Mathf.Max(region.width, region.height));
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
        private void OnDestroy()
        {
            if (hasCharacterHandle && characterHandle.IsValid()) Addressables.Release(characterHandle);
            if (protectorSprite != null) Destroy(protectorSprite);
            if (protectorTexture != null) Destroy(protectorTexture);
        }
    }
}
