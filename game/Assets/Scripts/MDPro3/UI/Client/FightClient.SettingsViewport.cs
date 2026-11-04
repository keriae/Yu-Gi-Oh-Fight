using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI.Client
{
    internal partial class FightClient
    {
        private sealed class SettingsViewportItem
        {
            internal RectTransform Rect;
            internal Vector2 Position, Size;
            internal float Top => -Position.y;
            internal float Bottom => Top + Size.y;
        }
        private RectTransform settingsLayoutBody, settingsLayoutWorkspace, settingsLayoutRail, settingsLayoutRailTitle, settingsLayoutRailDivider;
        private RectTransform settingsLayoutRailIcon, settingsLayoutMainDivider, settingsLayoutHeadingDivider;
        private RectTransform settingsLayoutActions, settingsLayoutActionsDivider, settingsLayoutReset, settingsLayoutCancel, settingsLayoutSave;
        private RectTransform settingsLayoutAiLeft, settingsLayoutAiRight;
        private TextMeshProUGUI settingsLayoutHeading;
        private readonly List<SettingsViewportItem> settingsLayoutItems = new List<SettingsViewportItem>();
        private float settingsLayoutReaderWidth, settingsLayoutContentHeight;
        private bool settingsLayoutFullscreen, settingsHasWindowScroll;
        private float settingsWindowScrollPosition;
        private float SettingsSidebarWidth => Compact ? 196 : 232;
        private float SettingsWorkspaceInset => Compact ? 24 : 32;
        private float SettingsHeadingHeight => Compact ? 80 : 92;
        private const float SettingsActionsHeight = 80;
        private const float AiSettingsPaneGap = 32, AiSettingsFieldHeight = 44, AiSettingsRowHeight = 68, AiSettingsJsonHeight = 176;
        private float SettingsFormWidth(float available, bool advanced) => Mathf.Min(available, advanced ? 1480 : 1080);

        private void CaptureSettingsViewportLayout(RectTransform content)
        {
            settingsLayoutBody = Body;
            settingsLayoutReaderWidth = content.rect.width;
            settingsLayoutContentHeight = content.rect.height;
            settingsLayoutFullscreen = UsesFullscreenLayout;
            settingsHasWindowScroll = false;
            settingsLayoutItems.Clear();
            foreach (Transform child in content)
            {
                var rect = child as RectTransform;
                if (rect != null) settingsLayoutItems.Add(new SettingsViewportItem
                    { Rect = rect, Position = rect.anchoredPosition, Size = rect.sizeDelta });
            }
        }

        // A mode change rearranges the existing work surface. In particular it
        // never recreates a TMP field, setting row or the searchable modal.
        private void ReflowSettingsViewport(float oldWidth, float oldHeight)
        {
            if (page != "settings" || Body == null || Body != settingsLayoutBody || settingsScroll == null
                || settingsScroll.content == null || settingsLayoutRail == null || settingsLayoutWorkspace == null) return;
            float normalized = settingsScroll.verticalNormalizedPosition;
            if (UsesFullscreenLayout && !settingsLayoutFullscreen)
            {
                settingsWindowScrollPosition = normalized;
                settingsHasWindowScroll = true;
            }
            else if (!UsesFullscreenLayout && settingsLayoutFullscreen && settingsHasWindowScroll)
                normalized = settingsWindowScrollPosition;
            settingsLayoutFullscreen = UsesFullscreenLayout;
            settingsScroll.StopMovement();
            foreach (var item in settingsLayoutItems)
                if (item.Rect != null)
                {
                    item.Rect.anchoredPosition = item.Position;
                    item.Rect.sizeDelta = item.Size;
                }

            float sidebar = SettingsSidebarWidth, inset = SettingsWorkspaceInset, headingHeight = SettingsHeadingHeight;
            float available = BodyWidth - sidebar;
            float readerWidth = settingsLayoutReaderWidth, contentHeight = settingsLayoutContentHeight;
            SettingsViewportItem left = null, right = null;
            foreach (var item in settingsLayoutItems)
            {
                if (item.Rect == settingsLayoutAiLeft) left = item;
                if (item.Rect == settingsLayoutAiRight) right = item;
            }
            bool advancedPanes = settingsCategory == "ai" && aiAdvanced && left != null && right != null;
            if (advancedPanes)
            {
                // Each pane keeps its authored field widths and type size. A
                // compact editor can use the two panes side by side in full
                // screen; an editor opened full screen can stack on return.
                float parallelWidth = left.Size.x + AiSettingsPaneGap + right.Size.x;
                bool parallel = !Compact && parallelWidth + inset * 2 + 24 <= available;
                float rightTop = parallel ? left.Top : left.Bottom + AiSettingsPaneGap;
                FightUI.Place(right.Rect, parallel ? left.Size.x + AiSettingsPaneGap : 0, rightTop, right.Size.x, right.Size.y);
                float originalBottom = Mathf.Max(left.Bottom, right.Bottom);
                float updatedBottom = parallel ? Mathf.Max(left.Bottom, rightTop + right.Size.y) : rightTop + right.Size.y;
                float delta = updatedBottom - originalBottom;
                foreach (var item in settingsLayoutItems)
                    if (item.Rect != null && item.Rect != left.Rect && item.Rect != right.Rect && item.Top >= originalBottom - 1)
                        item.Rect.anchoredPosition = new Vector2(item.Position.x, item.Position.y - delta);
                readerWidth = (parallel ? parallelWidth : Mathf.Max(left.Size.x, right.Size.x)) + 24;
                contentHeight += delta;
                float addedHeight = 0;
                foreach (var item in settingsLayoutItems)
                {
                    if (item.Rect == null || item.Rect == left.Rect || item.Rect == right.Rect) continue;
                    bool afterPanes = item.Top >= originalBottom - 1;
                    if (afterPanes && addedHeight > 0)
                        item.Rect.anchoredPosition -= new Vector2(0, addedHeight);
                    float usableWidth = Mathf.Max(32, readerWidth - 24 - item.Position.x);
                    if (item.Size.x <= usableWidth) continue;
                    var text = item.Rect.GetComponent<TextMeshProUGUI>();
                    if (text != null)
                    {
                        float height = item.Size.y;
                        if (afterPanes)
                        {
                            height = Mathf.Max(height, text.GetPreferredValues(text.text, usableWidth, float.PositiveInfinity).y + 4);
                            if (text == settingsAiTestLabel) height = Mathf.Min(160, height);
                            addedHeight += height - item.Size.y;
                        }
                        item.Rect.sizeDelta = new Vector2(usableWidth, height);
                    }
                    else if (item.Size.y <= 2)
                        item.Rect.sizeDelta = new Vector2(usableWidth, item.Size.y);
                }
                contentHeight += addedHeight;
            }

            float formWidth = SettingsFormWidth(available, advancedPanes);
            if (advancedPanes) formWidth = Mathf.Min(available, Mathf.Max(formWidth, readerWidth + inset * 2));
            float readerScale = Mathf.Min(1, (formWidth - inset * 2) / Mathf.Max(1, readerWidth));
            readerScale = Mathf.Max(.01f, readerScale);
            float physicalReaderWidth = readerWidth * readerScale;
            float workspaceX = (BodyWidth - sidebar - formWidth) / 2;
            float readerInset = (formWidth - physicalReaderWidth) / 2;
            float readerX = sidebar + readerInset;
            float actionsY = BodyHeight - SettingsActionsHeight, viewportHeight = Mathf.Max(160, actionsY - headingHeight - 24);

            FightUI.Place(settingsLayoutWorkspace, workspaceX, 0, sidebar + formWidth, BodyHeight);
            FightUI.Place(settingsLayoutRail, 0, 0, sidebar, BodyHeight);
            FightUI.Place(settingsLayoutRailIcon, 22, 27, 22, 22);
            FightUI.Place(settingsLayoutRailTitle, 56, 20, sidebar - 72, 40);
            FightUI.Place(settingsLayoutRailDivider, 20, headingHeight - 1, sidebar - 40, 1);
            FightUI.Place(settingsLayoutMainDivider, sidebar, 0, 1, BodyHeight);
            var categoryButtons = settingsLayoutRail.GetComponentsInChildren<Button>();
            for (int i = 0; i < categoryButtons.Length; i++)
            {
                var button = categoryButtons[i];
                FightUI.Place(button.GetComponent<RectTransform>(), 12, headingHeight + 12 + i * 56, sidebar - 24, 46);
                var label = button.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) FightUI.Place(label.rectTransform, 18, 0, sidebar - 60, 46);
            }
            if (settingsLayoutHeading != null)
            {
                settingsLayoutHeading.fontSize = Compact ? 28 : 32;
                FightUI.Place(settingsLayoutHeading.rectTransform, readerX, 20, physicalReaderWidth - 260, 44);
            }
            if (settingsDirtyLabel != null) FightUI.Place(settingsDirtyLabel.rectTransform, readerX + physicalReaderWidth - 260, 26, 260, 32);
            FightUI.Place(settingsLayoutHeadingDivider, readerX, headingHeight - 1, physicalReaderWidth, 1);
            FightUI.Place(settingsLayoutActions, sidebar, actionsY, formWidth, SettingsActionsHeight);
            FightUI.Place(settingsLayoutActionsDivider, readerInset, 0, physicalReaderWidth, 1);
            if (settingsLayoutReset != null)
            {
                bool resetIsButton = settingsLayoutReset.GetComponent<Button>() != null;
                FightUI.Place(settingsLayoutReset, readerInset, 16, resetIsButton ? 170 : Mathf.Max(150, physicalReaderWidth - 480), resetIsButton ? 48 : 56);
            }
            FightUI.Place(settingsLayoutCancel, readerInset + physicalReaderWidth - 400, 16, 180, 48);
            FightUI.Place(settingsLayoutSave, readerInset + physicalReaderWidth - 208, 16, 208, 48);

            FightUI.Place(settingsScroll.GetComponent<RectTransform>(), readerX, headingHeight + 12, physicalReaderWidth, viewportHeight);
            FightUI.Place(settingsScroll.viewport, 0, 0, physicalReaderWidth, viewportHeight);
            settingsScroll.content.sizeDelta = new Vector2(readerWidth, Mathf.Max(contentHeight, viewportHeight / readerScale));
            settingsScroll.content.localScale = new Vector3(readerScale, readerScale, 1);
            var scrollbar = settingsScroll.verticalScrollbar;
            if (scrollbar != null)
            {
                FightUI.Place(scrollbar.GetComponent<RectTransform>(), physicalReaderWidth - 15, 2, 14, viewportHeight - 4);
                var track = scrollbar.transform.Find("Track") as RectTransform;
                if (track != null) FightUI.Place(track, 10, 0, 4, viewportHeight - 4);
            }
            Canvas.ForceUpdateCanvases();
            settingsScroll.verticalNormalizedPosition = normalized;
            settingsScrollPosition = settingsScroll.verticalNormalizedPosition;
        }
    }
}
