using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YgomSystem.ElementSystem;
using MDPro3.UI.ServantUI;
using static MDPro3.Servant.OcgCore;

namespace MDPro3.UI.Client
{
    // Presentation only: retain the native controls, response handlers, icons,
    // dynamic stat colours, card text and safe-area geometry.
    internal sealed class FightDuelHudSkin : MonoBehaviour
    {
        private bool applied;

        internal static void Attach(OcgCoreUI ui)
        {
            if (ui == null || ui.Root == null) return;
            var skin = ui.GetComponent<FightDuelHudSkin>() ?? ui.gameObject.AddComponent<FightDuelHudSkin>();
            skin.Apply(ui);
        }

        private void Apply(OcgCoreUI ui)
        {
            if (applied) return;
            applied = true;
            StylePlayerName(ui.TextPlayer0Name);
            StylePlayerName(ui.TextPlayer1Name);
            // The original LP font and damage animation remain authoritative.
            if (ui.TextPlayer0LP != null) ui.TextPlayer0LP.color = FightUI.Foreground;
            if (ui.TextPlayer1LP != null) ui.TextPlayer1LP.color = FightUI.Foreground;
            if (ui.TextHint != null)
            {
                ui.TextHint.color = FightUI.CyanText;
                ui.TextHint.lineSpacing = 3;
            }
            if (ui.Hint != null && ui.Hint.TryGetComponent<Image>(out var hintPlate))
                hintPlate.color = new Color(.025f, .055f, .08f, .96f);

            foreach (var control in ui.Root.GetComponentsInChildren<Selectable>(true))
            {
                if (!IsHudControl(control.name)) continue;
                var feedback = control.GetComponent<FightDuelControlFeedback>()
                    ?? control.gameObject.AddComponent<FightDuelControlFeedback>();
                feedback.Bind(control, ui);
            }
            StyleDescription(ui.CardDescription);
        }

        private static bool IsHudControl(string name)
            => name == "ButtonStop" || name == "ButtonPlay" || name == "ButtonAcc" || name == "ButtonNor"
            || name == "ButtonTiming" || name == "ButtonLog" || name == "ButtonAttachedEffect"
            || name == "ButtonSetting" || name == "ButtonSettings";

        private static void StylePlayerName(TextMeshProUGUI text)
        {
            if (text == null) return;
            text.color = FightUI.Foreground;
            text.characterSpacing = .5f;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.fontSizeMax = text.fontSize;
            text.fontSizeMin = Mathf.Min(text.fontSize, 22);
            text.enableAutoSizing = true;
            // Empty Hint deliberately uses the current displayed name on hover.
            if (text.GetComponent<FightTooltip>() == null) text.gameObject.AddComponent<FightTooltip>();
        }

        private static void StyleDescription(CardDescription description)
        {
            if (description == null) return;
            var manager = description.GetComponent<ElementObjectManager>();
            if (manager == null) return;
            var window = manager.GetElement<RectTransform>("Window");
            if (window == null) return;
            var backing = window.GetComponent<Image>();
            if (backing != null) backing.color = FightUI.PanelColor;
            else
            {
                var backdrop = FightUI.Node(window, "Fight card information backdrop", 0, 0, 1, 1);
                FightUI.Stretch(backdrop);
                backdrop.SetAsFirstSibling();
                backing = backdrop.gameObject.AddComponent<Image>();
                backing.color = new Color(.025f, .055f, .08f, .97f);
                backing.raycastTarget = false;
                backdrop.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            }
            var edge = FightUI.Node(window, "Fight card information frame", 0, 0, 1, 1);
            FightUI.Stretch(edge);
            var outline = edge.gameObject.AddComponent<FightOutline>(); outline.effectColor = FightUI.GoldBorder;
            edge.gameObject.AddComponent<FightFrame>().Bind(outline);
            var effect = manager.GetElement<TextMeshProUGUI>("TextDescription");
            if (effect != null)
            {
                effect.color = FightUI.Foreground;
                effect.lineSpacing = 5;
                effect.paragraphSpacing = 8;
                // CardDescription.Show owns the user's configured font size and
                // its rich text highlights for changed and inactive effects.
            }
            var cardButton = manager.GetElement<Button>("CardButton");
            if (cardButton != null) FightUI.Hint(cardButton.gameObject, "查看完整卡片信息");
            foreach (var scrollbar in window.GetComponentsInChildren<Scrollbar>(true))
            {
                if (scrollbar.targetGraphic == null) continue;
                scrollbar.targetGraphic.color = FightUI.GoldBorder;
                var colours = scrollbar.colors;
                colours.normalColor = Color.white;
                colours.highlightedColor = FightUI.Active;
                colours.pressedColor = FightUI.Cyan;
                colours.selectedColor = FightUI.CyanText;
                colours.disabledColor = new Color(.45f, .45f, .45f, .5f);
                colours.fadeDuration = .12f;
                scrollbar.colors = colours;
            }
        }
    }

    internal sealed class FightDuelControlFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        private Selectable control;
        private OcgCoreUI ui;
        private FightDuelFocusRing ring;
        private FightTooltip hint;
        private bool hover, focus, pressed;
        private string previousHint;

        internal void Bind(Selectable target, OcgCoreUI owner)
        {
            control = target; ui = owner;
            if (ring == null)
            {
                var overlay = FightUI.Node(target.transform, "Fight HUD focus ring", 0, 0, 1, 1);
                FightUI.Stretch(overlay);
                overlay.offsetMin = new Vector2(-3, -3); overlay.offsetMax = new Vector2(3, 3);
                overlay.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                ring = overlay.gameObject.AddComponent<FightDuelFocusRing>();
                ring.raycastTarget = false; ring.color = new Color(0, 0, 0, 0);
            }
            hint = target.GetComponent<FightTooltip>() ?? target.gameObject.AddComponent<FightTooltip>();
            RefreshHint();
        }

        private void LateUpdate()
        {
            if (control == null || ring == null) return;
            bool available = control.IsInteractable();
            var target = !available ? new Color(.36f, .4f, .44f, .3f)
                : focus ? FightUI.Cyan : hover ? FightUI.Active : FightUI.GoldBorder;
            target.a = !available ? .3f : pressed ? .45f : focus || hover ? .95f : .22f;
            var next = Color.Lerp(ring.color, target, Mathf.Clamp01(Time.unscaledDeltaTime * 12));
            if (Mathf.Abs(next.r - target.r) + Mathf.Abs(next.g - target.g)
                + Mathf.Abs(next.b - target.b) + Mathf.Abs(next.a - target.a) < .005f) next = target;
            if (ring.color != next) ring.color = next;
            RefreshHint();
        }

        private void RefreshHint()
        {
            if (control == null || hint == null) return;
            string message = control.name switch
            {
                "ButtonStop" => "暂停播放",
                "ButtonPlay" => "继续播放",
                "ButtonAcc" => "加速播放 · 使用对局设置中的速度",
                "ButtonNor" => "恢复正常速度",
                "ButtonTiming" => chainCondition switch
                {
                    ChainCondition.No => "连锁响应：关闭\n单击切换响应方式",
                    ChainCondition.Smart => "连锁响应：自动\n单击切换响应方式",
                    _ => "连锁响应：始终确认\n单击切换响应方式"
                },
                "ButtonLog" => ui != null && ui.DuelLog != null && ui.DuelLog.showing ? "收起对局日志" : "查看对局日志",
                "ButtonAttachedEffect" => "查看已附加效果",
                _ => "对局设置"
            };
            if (message == previousHint) return;
            previousHint = message; hint.Hint = message;
        }

        public void OnPointerEnter(PointerEventData eventData) { hover = true; }
        public void OnPointerExit(PointerEventData eventData) { hover = false; pressed = false; }
        public void OnPointerDown(PointerEventData eventData) { if (eventData.button == PointerEventData.InputButton.Left) pressed = true; }
        public void OnPointerUp(PointerEventData eventData) { pressed = false; }
        public void OnSelect(BaseEventData eventData) { focus = true; }
        public void OnDeselect(BaseEventData eventData) { focus = false; pressed = false; }
        private void OnDisable() { hover = focus = pressed = false; }
    }

    internal sealed class FightDuelFocusRing : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            var rect = rectTransform.rect;
            if (rect.width <= 0 || rect.height <= 0 || color.a <= .001f) return;
            var radius = new Vector2(rect.width * .5f - 1, rect.height * .5f - 1);
            var inner = new Vector2(Mathf.Max(0, radius.x - 1.5f), Mathf.Max(0, radius.y - 1.5f));
            const int segments = 48;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * (float)Math.PI * 2 / segments;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                vertices.AddVert(rect.center + Vector2.Scale(direction, radius), color, Vector2.zero);
                vertices.AddVert(rect.center + Vector2.Scale(direction, inner), color, Vector2.zero);
            }
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                vertices.AddTriangle(i * 2, next * 2, i * 2 + 1);
                vertices.AddTriangle(i * 2 + 1, next * 2, next * 2 + 1);
            }
        }
    }
}
