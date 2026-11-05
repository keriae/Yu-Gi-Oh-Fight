using MDPro3.Duel;
using MDPro3.UI.ServantUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI.Client
{
    // Log records, card clicks and scroll culling remain owned by DuelLog.
    // Only this auxiliary drawer receives the client surface and typography.
    internal sealed class FightDuelLogSkin : MonoBehaviour
    {
        private DuelLog log;
        private RectTransform baseRect;
        private RectTransform decoration;
        private TextMeshProUGUI emptyTitle;
        internal static void Attach(DuelLog owner)
        {
            var skin = owner.GetComponent<FightDuelLogSkin>() ?? owner.gameObject.AddComponent<FightDuelLogSkin>();
            skin.Bind(owner);
        }
        private void Bind(DuelLog owner)
        {
            if (log != null) return;
            log = owner; baseRect = log.AuxiliaryRect;
            if (baseRect == null || log.AuxiliaryScroll == null) return;
            // Preserve the drawer's authored animation anchors and card record
            // geometry; add breathing room around its existing content.
            float drawerWidth = Mathf.Max(baseRect.rect.width, baseRect.sizeDelta.x);
            if (drawerWidth <= 0) drawerWidth = 326;
            var backing = baseRect.GetComponent<Image>();
            if (backing != null) { backing.sprite = FightSkin.Surface; backing.type = Image.Type.Sliced; backing.material = null; backing.color = FightUI.PanelColor; }
            foreach (var graphic in baseRect.GetComponentsInChildren<Image>(true))
            {
                if (graphic == backing || graphic.GetComponentInParent<Scrollbar>() != null) continue;
                var mask = graphic.GetComponent<Mask>();
                if (mask != null) { mask.showMaskGraphic = false; continue; }
                if (graphic.transform.IsChildOf(log.AuxiliaryScroll.content)) continue;
                graphic.material = null; graphic.color = Color.clear;
            }
            decoration = FightUI.Node(baseRect, "Fight log decoration", 0, 0, drawerWidth, baseRect.rect.height);
            FightUI.Stretch(decoration); decoration.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var panel = FightUI.Panel(decoration, 0, 0, drawerWidth, baseRect.rect.height, FightUI.PanelColor, true, "Fight log surface");
            FightUI.Stretch(panel); panel.GetComponent<Image>().raycastTarget = false;
            decoration.SetAsFirstSibling();
            var top = FightUI.Node(baseRect, "Fight log heading", 0, 0, drawerWidth, 72);
            top.anchorMin = new Vector2(0, 1); top.anchorMax = Vector2.one; top.pivot = new Vector2(.5f, 1); top.sizeDelta = new Vector2(0, 72); top.anchoredPosition = Vector2.zero;
            top.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            FightUI.Text(top, "对局日志", 18, 18, drawerWidth - 76, 34, 24, FightUI.Gold);
            var close = FightUI.Button(top, "", drawerWidth - 52, 14, 36, 36, () => Program.instance.ocgcore.GetUI<OcgCoreUI>().OnLog());
            FightUI.Icon(close.transform, "close", 11, 11, 14, 14, FightUI.Gold);
            FightUI.Hint(close.gameObject, "收起对局日志");
            FightUI.Line(top, 16, 68, drawerWidth - 32, FightUI.Border);
            var viewport = log.AuxiliaryScroll.GetComponent<RectTransform>();
            var min = viewport.offsetMin; var max = viewport.offsetMax;
            viewport.offsetMin = new Vector2(min.x, Mathf.Max(min.y, 48));
            viewport.offsetMax = new Vector2(max.x, Mathf.Min(max.y, -76));
            var footer = FightUI.Node(baseRect, "Fight log footer", 0, 0, drawerWidth, 44);
            footer.anchorMin = Vector2.zero; footer.anchorMax = new Vector2(1, 0); footer.pivot = new Vector2(.5f, 0); footer.sizeDelta = new Vector2(0, 44); footer.anchoredPosition = Vector2.zero;
            footer.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            FightUI.Line(footer, 16, 0, drawerWidth - 32, FightUI.Border);
            FightUI.Text(footer, "Tab · 对局日志", 18, 8, drawerWidth - 144, 26, 12, FightUI.Muted);
            FightUI.Button(footer, "最新记录", drawerWidth - 124, 5, 108, 32, () => log.AuxiliaryLatest());
            var empty = FightUI.Node(baseRect, "Fight log empty", 16, 110, drawerWidth - 32, 190);
            emptyTitle = FightUI.Text(empty, "暂无对局记录", 0, 16, drawerWidth - 32, 44, 20, FightUI.Gold, TextAlignmentOptions.Center);
            FightUI.Text(empty, "操作记录会按时间顺序显示。", 0, 72, drawerWidth - 32, 78, 16, FightUI.Muted, TextAlignmentOptions.Center);
            foreach (var scrollbar in baseRect.GetComponentsInChildren<Scrollbar>(true))
            {
                if (scrollbar.targetGraphic != null) scrollbar.targetGraphic.color = FightUI.GoldBorder;
                var colors = scrollbar.colors; colors.normalColor = Color.white; colors.highlightedColor = FightUI.Active; colors.pressedColor = FightUI.Cyan; scrollbar.colors = colors;
            }
        }
        private void LateUpdate()
        {
            if (log == null || emptyTitle == null) return;
            bool empty = log.AuxiliaryScroll.content.childCount == 0;
            emptyTitle.transform.parent.gameObject.SetActive(empty);
        }
        internal static void StyleRecord(GameObject record)
        {
            foreach (var text in record.GetComponentsInChildren<Text>(true))
            {
                // Keep semantic damage/recovery colors and native card names.
                Color original = text.color;
                bool red = original.r > .7f && original.g < .4f && original.b < .4f;
                bool blue = original.b > .65f && original.r < .4f;
                text.color = red ? FightUI.Danger : blue ? FightUI.CyanText : FightUI.Foreground;
                text.raycastTarget = text.GetComponent<Button>() != null;
            }
            foreach (var image in record.GetComponentsInChildren<Image>(true))
            {
                string name = image.name;
                if (name == "Image Side" || name.Contains("Background"))
                { image.material = null; image.sprite = FightSkin.Surface; image.type = Image.Type.Sliced; }
            }
        }
    }
}
