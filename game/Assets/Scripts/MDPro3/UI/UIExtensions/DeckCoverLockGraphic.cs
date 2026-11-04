using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI
{
    /// <summary>Small static lock badge for immutable recommendation cards.</summary>
    public sealed class DeckCoverLockGraphic : MaskableGraphic
    {
        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = rectTransform.rect;
            var scale = Mathf.Min(rect.width / 20f, rect.height / 22f);
            var origin = rect.center - new Vector2(10f, 11f) * scale;

            // A filled body and an open shackle, rendered without assets or animation.
            AddQuad(mesh, origin, scale, new Rect(3f, 2f, 14f, 11f), color);
            AddQuad(mesh, origin, scale, new Rect(4.5f, 11f, 2f, 4f), color);
            AddQuad(mesh, origin, scale, new Rect(13.5f, 11f, 2f, 4f), color);
            const int segments = 12;
            for (var i = 0; i < segments; i++)
            {
                var angle0 = (float)System.Math.PI * i / segments;
                var angle1 = (float)System.Math.PI * (i + 1) / segments;
                var center = new Vector2(10f, 15f);
                var start = mesh.currentVertCount;
                AddVertex(mesh, origin, scale, center + new Vector2(Mathf.Cos(angle0), Mathf.Sin(angle0)) * 5.5f, color);
                AddVertex(mesh, origin, scale, center + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * 5.5f, color);
                AddVertex(mesh, origin, scale, center + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * 3.5f, color);
                AddVertex(mesh, origin, scale, center + new Vector2(Mathf.Cos(angle0), Mathf.Sin(angle0)) * 3.5f, color);
                mesh.AddTriangle(start, start + 1, start + 2);
                mesh.AddTriangle(start, start + 2, start + 3);
            }

            var keyholeColor = new Color(0.07f, 0.11f, 0.14f, color.a);
            AddQuad(mesh, origin, scale, new Rect(9f, 5f, 2f, 5f), keyholeColor);
        }

        private static void AddVertex(VertexHelper mesh, Vector2 origin, float scale, Vector2 position, Color tint)
        {
            mesh.AddVert(origin + position * scale, tint, Vector2.zero);
        }

        private static void AddQuad(VertexHelper mesh, Vector2 origin, float scale, Rect rect, Color tint)
        {
            var start = mesh.currentVertCount;
            AddVertex(mesh, origin, scale, new Vector2(rect.xMin, rect.yMin), tint);
            AddVertex(mesh, origin, scale, new Vector2(rect.xMin, rect.yMax), tint);
            AddVertex(mesh, origin, scale, new Vector2(rect.xMax, rect.yMax), tint);
            AddVertex(mesh, origin, scale, new Vector2(rect.xMax, rect.yMin), tint);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
