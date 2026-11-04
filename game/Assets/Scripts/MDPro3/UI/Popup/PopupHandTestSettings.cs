using System;
using System.Collections.Generic;
using System.Reflection;
using MDPro3.Servant;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YgomSystem.UI;

namespace MDPro3.UI.Popup
{
    public class PopupHandTestSettings : Popup
    {
        private const string SOLO_TOGGLE_TEXT_NAME = "#Text_SoloToggle";
        private const string TITLE = "手牌测试设置";
        private const string BUTTON_EXIT = "退出";
        private const string BUTTON_START = "开始";
        private const string LABEL_NO_SHUFFLE = "不洗切卡组";
        private const string LABEL_STARTING_HAND = "初始手牌数：";
        private const string LABEL_LIFE_POINTS = "基本分：";
        private const string LABEL_RULE = "规则：";
        private const string LABEL_SAVE_REPLAY = "退出时保存回放";
        private static readonly Color DestructiveTextColor = new(0.827451f, 0f, 0f, 1f);

        private static readonly FieldInfo NavigationEventField =
            typeof(SelectionButton).GetField("navigationEvent", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo NonPersistentNavigationEventAddedField =
            typeof(SelectionButton).GetField("nonPersistentNavigationEventAdded", BindingFlags.Instance | BindingFlags.NonPublic);

        public HandTestSettings settings;
        public GameObject templateRoot;
        public Func<HandTestSettings, bool> startAction;

        private RectTransform content;
        private TextMeshProUGUI frameTextTemplate;
        private SelectionToggle toggleNoShuffle;
        private SelectionToggle toggleSaveReplay;
        private TMP_InputField inputStartingHand;
        private TMP_InputField inputLifePoints;
        private SelectionButton buttonRule;
        private bool uiBuilt;

        protected override void InitializeSelections()
        {
            args = new List<string> { InterString.Get(TITLE) };
            base.InitializeSelections();

            content = Manager.GetElement<RectTransform>("Content");
            content.anchoredPosition = Vector2.zero;
            Manager.GetElement<SelectionButton>("DecideButton").SetButtonText(InterString.Get(BUTTON_START));
            Manager.GetElement<SelectionButton>("CancelButton").SetButtonText(InterString.Get(BUTTON_EXIT));
            ApplyDestructiveCancelStyle();

            var icon = Manager.GetElement("Icon");
            if (icon != null)
                icon.SetActive(false);

            frameTextTemplate = Manager.GetElement<TextMeshProUGUI>("FrameText");
            if (frameTextTemplate != null)
                frameTextTemplate.transform.parent.gameObject.SetActive(false);

            BuildSettingsPanel();
            ApplySettingsToControls();

            lastSelectable = toggleNoShuffle != null
                ? toggleNoShuffle.GetComponent<Selectable>()
                : Manager.GetElement<SelectionButton>("DecideButton").GetComponent<Selectable>();
        }

        public override void Hide()
        {
            PersistSettings();
            base.Hide();
        }

        protected override void OnDecide()
        {
            PersistSettings();
            if (startAction == null || startAction.Invoke(settings.Clone()))
                Hide();
        }

        protected override void OnCancel()
        {
            Hide();
        }

        private void BuildSettingsPanel()
        {
            if (uiBuilt)
                return;

            uiBuilt = true;
            var buttonGroup = Manager.GetElement<SelectionButton>("DecideButton").transform.parent;
            var settingsRoot = CreateVerticalLayout("SettingsRoot", content);
            settingsRoot.SetSiblingIndex(buttonGroup.GetSiblingIndex());

            toggleNoShuffle = CloneToggleControl("ToggleLockHand", settingsRoot, InterString.Get(LABEL_NO_SHUFFLE));
            inputStartingHand = CreateLabeledInputRow("RowStartingHand", "LabelStartingHand", settingsRoot
                , InterString.Get(LABEL_STARTING_HAND), 240f, 2);
            inputLifePoints = CreateLabeledInputRow("RowLifePoints", "LabelLifePoints", settingsRoot
                , InterString.Get(LABEL_LIFE_POINTS), 240f, 6);
            buttonRule = CreateLabeledRuleRow(settingsRoot, InterString.Get(LABEL_RULE));
            toggleSaveReplay = CloneToggleControl("ToggleLockHand", settingsRoot, InterString.Get(LABEL_SAVE_REPLAY));

            SetAutomaticNavigation(settingsRoot);
        }

        private void ApplySettingsToControls()
        {
            if (settings == null)
                settings = HandTestSettings.Load();
            settings.Clamp();

            SetToggle(toggleNoShuffle, settings.NoShuffleDeck);
            SetToggle(toggleSaveReplay, settings.SaveReplayWhenLeaving);

            if (inputStartingHand != null)
                inputStartingHand.text = settings.StartingHand.ToString();
            if (inputLifePoints != null)
                inputLifePoints.text = settings.LifePoints.ToString();
            if (buttonRule != null)
                buttonRule.SetButtonText(GetRuleButtonText());
        }

        private void PersistSettings()
        {
            settings = ReadSettingsFromControls();
            settings.Save();
        }

        private HandTestSettings ReadSettingsFromControls()
        {
            var nextSettings = settings != null ? settings.Clone() : HandTestSettings.Load();
            if (toggleNoShuffle != null)
                nextSettings.NoShuffleDeck = toggleNoShuffle.isOn;
            if (toggleSaveReplay != null)
                nextSettings.SaveReplayWhenLeaving = toggleSaveReplay.isOn;
            if (inputStartingHand != null && int.TryParse(inputStartingHand.text, out var parsedHand))
                nextSettings.StartingHand = parsedHand;
            if (inputLifePoints != null && int.TryParse(inputLifePoints.text, out var parsedLifePoints))
                nextSettings.LifePoints = parsedLifePoints;

            nextSettings.Clamp();
            if (inputStartingHand != null)
                inputStartingHand.text = nextSettings.StartingHand.ToString();
            if (inputLifePoints != null)
                inputLifePoints.text = nextSettings.LifePoints.ToString();
            return nextSettings;
        }

        private void OpenRuleSelection()
        {
            var selections = new List<string>
            {
                InterString.Get(LABEL_RULE),
                string.Empty,
                HandTestSettings.GetRuleDisplayName(HandTestSettings.DefaultMasterRule)
            };
            UIManager.ShowPopupSelection(selections, OnRuleSelected, null);
        }

        private void OnRuleSelected()
        {
            settings = ReadSettingsFromControls();
            settings.MasterRule = HandTestSettings.DefaultMasterRule;
            if (buttonRule != null)
                buttonRule.SetButtonText(GetRuleButtonText());
        }

        private RectTransform CreateVerticalLayout(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);

            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 14f;
            layout.padding = new RectOffset(48, 48, 28, 16);

            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rect;
        }

        private SelectionToggle CloneToggleControl(string templateName, Transform parent, string labelText)
        {
            var template = FindTemplate(templateName);
            var toggle = UnityEngine.Object.Instantiate(template.gameObject, parent, false).GetComponent<SelectionToggle>();
            toggle.name = templateName + "Clone";
            ResetSelectionButtonNavigation(toggle);
            PrepareStandaloneControl(toggle.GetComponent<RectTransform>(), 44f);
            ApplyToggleLabel(toggle, labelText);
            ConfigureControlText(toggle.transform);
            return toggle;
        }

        private TMP_InputField CreateLabeledInputRow(
            string rowName,
            string labelName,
            Transform parent,
            string labelText,
            float preferredWidth,
            int characterLimit)
        {
            var row = CreateLabeledRow(rowName, parent);
            CreateLabel(labelName, row, labelText);

            var template = FindTemplate("InputFieldHand");
            var inputField = UnityEngine.Object.Instantiate(template.gameObject, row, false).GetComponent<TMP_InputField>();
            inputField.name = "InputFieldHandClone";
            ResetSelectionButtonNavigation(inputField.GetComponent<SelectionButton>());
            PrepareFieldControl(inputField.GetComponent<RectTransform>(), preferredWidth, 50f);
            inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            inputField.characterLimit = characterLimit;
            CleanupInputFieldTemplateText(inputField);
            if (inputField.textComponent != null)
            {
                inputField.textComponent.enableAutoSizing = true;
                inputField.textComponent.fontSizeMin = 20f;
                inputField.textComponent.fontSizeMax = 30f;
                inputField.textComponent.alignment = TextAlignmentOptions.Center;
            }
            return inputField;
        }

        private SelectionButton CreateLabeledRuleRow(Transform parent, string labelText)
        {
            var row = CreateLabeledRow("RowRule", parent);
            CreateLabel("LabelRule", row, labelText);

            var buttonTemplate = Manager.GetElement<SelectionButton>("DecideButton");
            var button = UnityEngine.Object.Instantiate(buttonTemplate.gameObject, row, false).GetComponent<SelectionButton>();
            button.name = "ButtonRuleClone";
            ResetSelectionButtonNavigation(button);
            button.RemoveAllListeners();
            if (button.TryGetComponent<Button>(out var uiButton))
                uiButton.onClick.RemoveAllListeners();
            PrepareFieldControl(button.GetComponent<RectTransform>(), 240f, 50f);
            button.SetClickEvent(OpenRuleSelection);
            button.SetButtonText(GetRuleButtonText());
            ConfigureControlText(button.transform);
            return button;
        }

        private RectTransform CreateLabeledRow(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;

            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 18f;

            var element = go.GetComponent<LayoutElement>();
            element.preferredHeight = 50f;
            return rect;
        }

        private TextMeshProUGUI CreateLabel(string name, Transform parent, string text)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            var layout = go.GetComponent<LayoutElement>();
            layout.preferredWidth = 190f;
            layout.minWidth = 160f;

            var label = go.GetComponent<TextMeshProUGUI>();
            if (frameTextTemplate != null)
            {
                label.font = frameTextTemplate.font;
                label.fontSharedMaterial = frameTextTemplate.fontSharedMaterial;
            }
            label.text = text;
            label.color = Color.white;
            label.fontSize = 30f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 20f;
            label.fontSizeMax = 30f;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            return label;
        }

        private string GetRuleButtonText()
        {
            if (settings == null)
                settings = HandTestSettings.Load();
            return settings.GetRuleDisplayName();
        }

        private Transform FindTemplate(string templateName)
        {
            return FindTemplate(templateRoot, templateName, true);
        }

        private static Transform FindTemplate(GameObject root, string templateName, bool throwIfMissing)
        {
            if (root == null)
            {
                if (throwIfMissing)
                    throw new InvalidOperationException("Hand test settings template root is missing.");
                return null;
            }

            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == templateName)
                    return child;

            if (throwIfMissing)
                throw new InvalidOperationException("Missing hand test settings template: " + templateName);
            return null;
        }

        private void ApplyDestructiveCancelStyle()
        {
            var cancelButton = Manager.GetElement<SelectionButton>("CancelButton");
            if (cancelButton == null)
                return;

            var targetText = FindFirstComponent<TMP_Text>(cancelButton.transform);
            if (targetText != null)
            {
                targetText.color = DestructiveTextColor;
                var targetTextColor = targetText.GetComponent<ColorContainerGraphic>();
                if (targetTextColor != null)
                {
                    targetTextColor.baseColor = DestructiveTextColor;
                    targetTextColor.SetColor(ColorContainer.SelectMode.Unselected, ColorContainer.StatusMode.Normal, true);
                }
            }

            var hoverCorner = FindNamedImage(cancelButton.transform, "Corner");
            if (hoverCorner != null && hoverCorner.TryGetComponent<ColorContainerGraphic>(out var hoverCornerColor))
                hoverCornerColor.baseColor = DestructiveTextColor;
        }

        private static Image FindNamedImage(Transform root, string name)
        {
            foreach (var image in root.GetComponentsInChildren<Image>(true))
                if (image.name == name)
                    return image;
            return null;
        }

        private static T FindFirstComponent<T>(Transform root) where T : Component
        {
            foreach (var component in root.GetComponentsInChildren<T>(true))
                return component;
            return null;
        }

        private static void CleanupInputFieldTemplateText(TMP_InputField inputField)
        {
            if (inputField == null)
                return;

            var inputText = inputField.textComponent;
            var placeholderText = inputField.placeholder as TMP_Text;
            foreach (var text in inputField.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text == inputText)
                    continue;

                if (text == placeholderText)
                {
                    text.text = string.Empty;
                    text.gameObject.SetActive(false);
                    continue;
                }

                text.text = string.Empty;
                text.gameObject.SetActive(false);
            }
        }

        private static void PrepareStandaloneControl(RectTransform rect, float preferredHeight)
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
            layout.preferredHeight = preferredHeight;
            layout.flexibleWidth = 1f;
        }

        private static void PrepareFieldControl(RectTransform rect, float preferredWidth, float preferredHeight)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;

            var layout = rect.GetComponent<LayoutElement>();
            if (layout == null)
                layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = preferredWidth;
            layout.minWidth = preferredWidth;
            layout.preferredHeight = preferredHeight;
            layout.flexibleWidth = 1f;
        }

        private static void ConfigureControlText(Transform root)
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

        private static void ApplyToggleLabel(SelectionToggle toggle, string labelText)
        {
            if (toggle == null)
                return;

            toggle.SetButtonText(labelText);
            foreach (var text in toggle.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (text.name == SOLO_TOGGLE_TEXT_NAME)
                    text.text = labelText;
        }

        private static void ResetSelectionButtonNavigation(SelectionButton selectionButton)
        {
            if (selectionButton == null)
                return;

            NavigationEventField?.SetValue(selectionButton, new SelectionButtonNavigationEvent());
            NonPersistentNavigationEventAddedField?.SetValue(selectionButton, false);
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

        private static void SetToggle(SelectionToggle toggle, bool isOn)
        {
            if (toggle == null)
                return;

            if (isOn)
                toggle.SetToggleOn(false);
            else
                toggle.SetToggleOff(false);
        }
    }
}
