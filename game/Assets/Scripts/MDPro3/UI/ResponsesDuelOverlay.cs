using MDPro3.AI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI
{
    public sealed class ResponsesDuelOverlay : MonoBehaviour
    {
        private TextMeshProUGUI status;
        private string previous;

        public void Initialize(TextMeshProUGUI template)
        {
            var panel = GetComponent<RectTransform>();
            panel.anchorMin = panel.anchorMax = new Vector2(1f, 1f);
            panel.pivot = new Vector2(1f, 1f);
            panel.anchoredPosition = new Vector2(-20f, -170f);
            panel.sizeDelta = new Vector2(330f, 112f);
            gameObject.AddComponent<Image>().color = new Color(0.018f, 0.038f, 0.068f, 0.94f);
            var text = new GameObject("AIStatus", typeof(RectTransform), typeof(TextMeshProUGUI));
            text.transform.SetParent(transform, false);
            status = text.GetComponent<TextMeshProUGUI>();
            status.font = template.font;
            status.fontSharedMaterial = template.fontSharedMaterial;
            status.fontSize = 21f;
            status.color = Color.white;
            status.richText = false;
            status.raycastTarget = false;
            status.overflowMode = TextOverflowModes.Ellipsis;
            status.rectTransform.anchorMin = new Vector2(0f, 0.42f);
            status.rectTransform.anchorMax = Vector2.one;
            status.rectTransform.offsetMin = new Vector2(12f, 0f);
            status.rectTransform.offsetMax = new Vector2(-12f, -8f);
            var action = new GameObject("StopAI", typeof(RectTransform), typeof(Image), typeof(Button));
            action.transform.SetParent(transform, false);
            action.GetComponent<Image>().color = new Color(0.12f, 0.22f, 0.34f, 1f);
            var actionRect = action.GetComponent<RectTransform>();
            actionRect.anchorMin = new Vector2(0f, 0f);
            actionRect.anchorMax = new Vector2(1f, 0.38f);
            actionRect.offsetMin = new Vector2(12f, 8f);
            actionRect.offsetMax = new Vector2(-12f, 0f);
            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(action.transform, false);
            var stopText = label.GetComponent<TextMeshProUGUI>();
            stopText.font = template.font;
            stopText.fontSharedMaterial = template.fontSharedMaterial;
            stopText.fontSize = 21f;
            stopText.alignment = TextAlignmentOptions.Center;
            stopText.text = "停止 AI 对战";
            stopText.color = Color.white;
            stopText.raycastTarget = false;
            stopText.rectTransform.anchorMin = Vector2.zero;
            stopText.rectTransform.anchorMax = Vector2.one;
            stopText.rectTransform.offsetMin = stopText.rectTransform.offsetMax = Vector2.zero;
            action.GetComponent<Button>().onClick.AddListener(() =>
            {
                ResponsesDuelSession.Stop();
                Program.instance.ocgcore.OnExit();
            });
            Refresh();
        }

        public void Refresh()
        {
            var visible = ResponsesDuelSession.IsActive && Program.instance.ocgcore.showing;
            gameObject.SetActive(visible);
            if (visible && previous != ResponsesDuelSession.Status)
            {
                previous = ResponsesDuelSession.Status;
                status.text = previous;
            }
        }
    }
}
