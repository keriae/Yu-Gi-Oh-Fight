using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YuGiOhFight.Client.UI.Client
{
    // The native message manager still owns queue order, lifetime and movement.
    // These non-modal notices use the same quiet reading surface as the rule prompts.
    internal sealed class FightDuelMessageSkin : MonoBehaviour
    {
        private RectTransform rect;
        private TextMeshProUGUI text;
        private Image plate;
        private ScenePalette palette;
        private CanvasGroup visibility;
        private string scene;
        private float width, height;
        internal float QueueSpacing => height + 12;

        internal static bool Attach(GameObject item, bool centered)
        {
            if (item == null || !FightDuelPopupSkin.SceneThemeActive) return false;
            var skin = item.AddComponent<FightDuelMessageSkin>();
            skin.Bind(centered);
            return true;
        }

        private void Bind(bool centered)
        {
            // Native notices live on UIBlur, whose camera is disabled until
            // UIBlurPlus. Scene notices use the always-visible UI without blur.
            var ui = Runtime.Program.instance.ui_;
            transform.SetParent(ui.transform, false);
            transform.SetAsLastSibling();
            rect = GetComponent<RectTransform>();
            text = GetComponentInChildren<TextMeshProUGUI>(true);
            scene = Runtime.Program.instance.ocgcore.DuelBGManager.ActiveSceneId;
            palette = ScenePalette.DialogFor(scene);
            visibility = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            visibility.blocksRaycasts = false; visibility.interactable = false;
            foreach (var fitter in GetComponentsInChildren<ContentSizeFitter>(true)) fitter.enabled = false;
            foreach (var layout in GetComponentsInChildren<LayoutGroup>(true)) layout.enabled = false;
            foreach (var image in GetComponentsInChildren<Image>(true)) image.enabled = false;
            var surface = FightUI.Node(transform, "Fight scene notice material", 0, 0, 1, 1);
            FightUI.Stretch(surface); surface.SetAsFirstSibling();
            plate = surface.gameObject.AddComponent<Image>(); plate.raycastTarget = false;
            plate.sprite = FightSkin.ButtonSurface; plate.overrideSprite = null; plate.material = null;
            plate.type = Image.Type.Sliced;
            plate.pixelsPerUnitMultiplier = 1;
            var tint = palette.Panel; tint.a = palette.Kind == 0 ? .80f : .74f; plate.color = tint;
            if (text != null)
            {
                var font = Runtime.Program.instance.ui_.tmpFont;
                if (font != null) { text.font = font; text.fontSharedMaterial = font.material; }
                text.enableAutoSizing = false; text.enableVertexGradient = false;
                text.fontSize = 22; text.fontStyle = FontStyles.Normal;
                text.enableWordWrapping = true; text.overflowMode = TextOverflowModes.Ellipsis;
                text.alignment = TextAlignmentOptions.MidlineLeft; text.raycastTarget = false;
                FightUI.Stretch(text.rectTransform);
                text.rectTransform.offsetMin = new Vector2(22, 12);
                text.rectTransform.offsetMax = new Vector2(-22, -12);
            }
            rect.anchorMin = rect.anchorMax = new Vector2(centered ? .5f : 1, 1);
            rect.pivot = new Vector2(centered ? .5f : 1, 1);
            // The reader's first title band begins below the upper safe area.
            // Keep short confirmations there instead of covering the card name.
            if (centered) rect.anchoredPosition = new Vector2(0, -48);
            foreach (var node in GetComponentsInChildren<Transform>(true))
                node.gameObject.layer = ui.gameObject.layer;
            Measure();
        }

        private void Measure()
        {
            var parent = rect != null ? rect.parent as RectTransform : null;
            if (rect == null || parent == null) return;
            width = Mathf.Min(560, Mathf.Max(180, parent.rect.width - 48));
            height = Mathf.Max(64, text != null ? text.GetPreferredValues(text.text, width - 44, float.PositiveInfinity).y + 24 : 64);
            // Normal notices retain their entire measured text. Exceptionally
            // long server messages keep a usable reading area on small screens.
            height = Mathf.Min(height, Mathf.Max(64, parent.rect.height - 200));
            rect.sizeDelta = new Vector2(width, height);
        }

        private void LateUpdate()
        {
            if (plate == null || palette == null) return;
            visibility.alpha = FightDuelPopupSkin.SceneThemeActive
                && Runtime.Program.instance.ocgcore.DuelBGManager.ActiveSceneId == scene ? 1 : 0;
            Measure();
            if (text != null)
            {
                text.color = palette.Text;
                var colors = text.GetComponent<Presentation.UI.ColorContainerGraphic>();
                if (colors != null) colors.baseColor = palette.Text;
            }
        }
    }
}
