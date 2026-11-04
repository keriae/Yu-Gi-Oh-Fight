using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI
{
    /// <summary>A static cut-corner selection border that does not intercept input.</summary>
    public sealed class SelectionEdgeHighlight : MaskableGraphic
    {
        private const string ChildName = "SelectionEdgeHighlight";
        private const float BorderWidth = 2.5f;
        private const float GlowWidth = 3.5f;
        private static readonly Color SelectedColor = new Color(0.75f, 1f, 0.16f, 1f);

        public static void SetSelected(SelectionButton button, bool selected)
        {
            if (button == null)
                return;

            var child = button.transform.Find(ChildName);
            var highlight = child != null ? child.GetComponent<SelectionEdgeHighlight>() : null;
            if (highlight == null)
            {
                if (!selected)
                    return;

                var go = new GameObject(ChildName, typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(LayoutElement), typeof(SelectionEdgeHighlight));
                go.transform.SetParent(button.transform, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                go.GetComponent<LayoutElement>().ignoreLayout = true;
                highlight = go.GetComponent<SelectionEdgeHighlight>();
                highlight.color = SelectedColor;
            }

            highlight.raycastTarget = false;
            highlight.transform.SetAsLastSibling();
            highlight.gameObject.SetActive(selected);
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnEnable()
        {
            raycastTarget = false;
            base.OnEnable();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = rectTransform.rect;
            if (rect.width <= BorderWidth * 2f || rect.height <= BorderWidth * 2f)
                return;

            var cut = Mathf.Min(18f, Mathf.Min(rect.width, rect.height) * 0.35f);
            var outside = Contour(rect, cut, -GlowWidth);
            var edge = Contour(rect, cut, 0f);
            var inside = Contour(rect, cut, BorderWidth);
            AddRing(mesh, outside, edge, 0f, 0.24f);
            AddRing(mesh, edge, inside, 1f, 1f);
        }

        private static Vector2[] Contour(Rect rect, float cut, float inset)
        {
            var left = rect.xMin + inset;
            var right = rect.xMax - inset;
            var bottom = rect.yMin + inset;
            var top = rect.yMax - inset;
            // Keep diagonal edges parallel when insetting the polygon.
            var corner = Mathf.Max(0f, cut - inset * (2f - Mathf.Sqrt(2f)));
            return new[]
            {
                new Vector2(left + corner, top),
                new Vector2(right, top),
                new Vector2(right, bottom + corner),
                new Vector2(right - corner, bottom),
                new Vector2(left, bottom),
                new Vector2(left, top - corner)
            };
        }

        private void AddRing(VertexHelper mesh, Vector2[] outer, Vector2[] inner, float outerAlpha, float innerAlpha)
        {
            var outerColor = color;
            outerColor.a *= outerAlpha;
            var innerColor = color;
            innerColor.a *= innerAlpha;
            for (var i = 0; i < outer.Length; i++)
            {
                var next = (i + 1) % outer.Length;
                var start = mesh.currentVertCount;
                mesh.AddVert(outer[i], outerColor, Vector2.zero);
                mesh.AddVert(outer[next], outerColor, Vector2.zero);
                mesh.AddVert(inner[next], innerColor, Vector2.zero);
                mesh.AddVert(inner[i], innerColor, Vector2.zero);
                mesh.AddTriangle(start, start + 1, start + 2);
                mesh.AddTriangle(start, start + 2, start + 3);
            }
        }
    }
}
