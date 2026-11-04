using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI.Popup
{
    public class PopupYdke : Popup
    {
        private const string YDKE_BUTTON_EXIT = "退出";

        public Action importAction;
        public Action exportAction;

        private MDPro3.UI.SelectionButton importButton;

        protected override void InitializeSelections()
        {
            args = new List<string> { $"YDKE {InterString.Get("导入导出")}" };
            base.InitializeSelections();

            var content = Manager.GetElement<RectTransform>("Content");
            content.anchoredPosition = Vector2.zero;

            var icon = Manager.GetElement("Icon");
            if (icon != null)
                icon.SetActive(false);

            var frameText = Manager.GetElement<TextMeshProUGUI>("FrameText");
            if (frameText != null)
                frameText.transform.parent.gameObject.SetActive(false);

            BuildButtonPanel(content);
            lastSelectable = importButton.GetComponent<Selectable>();
        }

        private void BuildButtonPanel(RectTransform content)
        {
            var buttonTemplate = Manager.GetElement<MDPro3.UI.SelectionButton>("DecideButton");
            var buttonGroup = buttonTemplate.transform.parent.gameObject;

            var root = new GameObject("YdkeButtons", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            root.transform.SetParent(content, false);

            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);

            var layout = root.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 18f;
            layout.padding = new RectOffset(48, 48, 28, 28);

            var fitter = root.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            importButton = CreateActionButton(buttonTemplate, root.transform, InterString.Get("导入"), importAction);
            CreateActionButton(buttonTemplate, root.transform, InterString.Get("导出"), exportAction);
            CreateActionButton(buttonTemplate, root.transform, InterString.Get(YDKE_BUTTON_EXIT), Hide);

            buttonGroup.SetActive(false);
            SetAutomaticNavigation(root.transform);
        }

        private static MDPro3.UI.SelectionButton CreateActionButton(
            MDPro3.UI.SelectionButton template,
            Transform parent,
            string text,
            Action action)
        {
            var button = UnityEngine.Object.Instantiate(template.gameObject, parent, false).GetComponent<MDPro3.UI.SelectionButton>();
            button.name = "Button" + text;
            button.RemoveAllListeners();
            if (button.TryGetComponent<Button>(out var uiButton))
                uiButton.onClick.RemoveAllListeners();
            button.SetClickEvent(() => action?.Invoke());
            button.SetButtonText(text);
            ConfigureYdkeButton(button.GetComponent<RectTransform>());
            ConfigureYdkeControlText(button.transform);
            return button;
        }

        private static void ConfigureYdkeButton(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(0f, rect.sizeDelta.y);

            var layout = rect.GetComponent<LayoutElement>();
            if (layout == null)
                layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 58f;
            layout.flexibleWidth = 1f;
        }

        private static void ConfigureYdkeControlText(Transform root)
        {
            foreach (var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                text.enableAutoSizing = true;
                text.fontSizeMin = 18f;
                text.fontSizeMax = Mathf.Max(text.fontSize, 30f);
                text.enableWordWrapping = false;
                text.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        private void SetAutomaticNavigation(Transform root)
        {
            foreach (var selectable in root.GetComponentsInChildren<Selectable>(true))
            {
                var navigation = selectable.navigation;
                navigation.mode = Navigation.Mode.Automatic;
                selectable.navigation = navigation;
            }
        }
    }
}
