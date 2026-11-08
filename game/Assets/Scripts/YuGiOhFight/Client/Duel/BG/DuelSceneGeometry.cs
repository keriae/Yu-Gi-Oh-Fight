using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace YuGiOhFight.Client.Duel
{
    // Theme surfaces share the existing card coordinates, but never supply
    // gameplay anchors, colliders or camera settings.
    internal static partial class DuelSceneGeometry
    {
        private const float Pi = 3.14159265358979323846f;
        private const float DegreesToRadians = Pi / 180f;
        internal static bool Supports(string builder)
            => builder == "classic" || builder == "battle-academy" || builder == "ancient-egypt" || builder == "skyscraper";

        // The atlas is a 4 x 4 grid, indexed left-to-right from the upper left.
        // A small inset prevents neighbouring materials bleeding when minified.
        private static Rect ArtCell(int cell)
        {
            const float inset = .004f;
            return new Rect((cell % 4) * .25f + inset, (3 - cell / 4) * .25f + inset,
                .25f - inset * 2, .25f - inset * 2);
        }

        private sealed class Surface
        {
            private const float TextureScale = 8;
            private static readonly Vector3 bakeDirection = new Vector3(-.3f, 1, -.35f).normalized;
            private static readonly Vector3 bakeHalfDirection = new Vector3(-.16f, .92f, -.48f).normalized;
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector3> normals = new List<Vector3>();
            private readonly List<Vector2> uvs = new List<Vector2>();
            private readonly List<Color> colors = new List<Color>();
            private readonly List<int> indices = new List<int>();
            private Color tint = Color.white;
            private bool emissive;
            private Rect? atlasCell;
            private float surfaceGlint;
            internal void Atlas(Rect cell) => atlasCell = cell;
            internal void Emissive(bool value) => emissive = value;
            internal void Finish(float glint) => surfaceGlint = glint;

            internal void Tint(Color value) => tint = value;

            internal void ExpandBounds(ref Vector3 minimum, ref Vector3 maximum)
            {
                foreach (var vertex in vertices)
                {
                    minimum = new Vector3(Mathf.Min(minimum.x, vertex.x), Mathf.Min(minimum.y, vertex.y), Mathf.Min(minimum.z, vertex.z));
                    maximum = new Vector3(Mathf.Max(maximum.x, vertex.x), Mathf.Max(maximum.y, vertex.y), Mathf.Max(maximum.z, vertex.z));
                }
            }

            internal void BevelBox(Vector3 centre, Vector3 size, float bevel, float yaw = 0)
            {
                var half = size * .5f;
                bevel = Mathf.Min(bevel, Mathf.Min(half.y * .7f, Mathf.Min(half.x, half.z) * .45f));
                float cut = Mathf.Min(Mathf.Min(half.x, half.z) * .4f, bevel * 2);
                var outline = Octagon(half.x, half.z, cut);
                var inset = Octagon(half.x - bevel, half.z - bevel, cut * .7f);
                Vector3 Point(Vector2 footprint, float y)
                    => centre + Rotate(new Vector3(footprint.x, y, footprint.y), yaw);
                for (int edge = 0; edge < outline.Length; edge++)
                {
                    int next = (edge + 1) % outline.Length;
                    Vector3 a = Point(inset[edge], -half.y), b = Point(inset[next], -half.y);
                    Vector3 c = Point(outline[edge], -half.y + bevel), d = Point(outline[next], -half.y + bevel);
                    Vector3 e = Point(outline[edge], half.y - bevel), f = Point(outline[next], half.y - bevel);
                    Vector3 g = Point(inset[edge], half.y), h = Point(inset[next], half.y);
                    // A narrow dark foot and exposed upper bevel belong to
                    // this box, rather than to its height in the whole scene.
                    Quad(a, b, d, c, .8f, .8f, .84f, .84f);
                    Quad(c, d, f, e, .84f, .84f, 1, 1);
                    Quad(e, f, h, g, 1.02f, 1.02f, 1.12f, 1.12f);
                    Triangle(centre + new Vector3(0, half.y, 0), g, h, 1, 1.1f, 1.1f);
                    Triangle(centre - new Vector3(0, half.y, 0), b, a, .8f, .8f, .8f);
                }
            }

            private static Vector2[] Octagon(float x, float z, float cut)
                => new[] {
                    new Vector2(-x, -z + cut), new Vector2(-x, z - cut),
                    new Vector2(-x + cut, z), new Vector2(x - cut, z),
                    new Vector2(x, z - cut), new Vector2(x, -z + cut),
                    new Vector2(x - cut, -z), new Vector2(-x + cut, -z)
                };

            private static Vector3 Rotate(Vector3 point, float yaw)
            {
                float angle = yaw * DegreesToRadians;
                float cosine = Mathf.Cos(angle), sine = Mathf.Sin(angle);
                return new Vector3(point.x * cosine + point.z * sine, point.y, point.z * cosine - point.x * sine);
            }

            internal void Ring(float inner, float outer, float bottom, float top, float bevel, int segments)
                => RingAt(Vector3.zero, inner, outer, bottom, top, bevel, segments);

            internal void RingAt(Vector3 centre, float inner, float outer, float bottom, float top, float bevel, int segments)
            {
                bevel = Mathf.Min(bevel, Mathf.Min((outer - inner) * .4f, (top - bottom) * .4f));
                var profile = new[] {
                    new Vector2(inner + bevel, top), new Vector2(outer - bevel, top),
                    new Vector2(outer, top - bevel), new Vector2(outer, bottom + bevel),
                    new Vector2(outer - bevel, bottom), new Vector2(inner + bevel, bottom),
                    new Vector2(inner, bottom + bevel), new Vector2(inner, top - bevel)
                };
                float middleRadius = (inner + outer) * .5f;
                for (int segment = 0; segment < segments; segment++)
                {
                    float a = Mathf.Lerp(0, 360, (float)segment / segments) * DegreesToRadians;
                    float b = Mathf.Lerp(0, 360, (float)(segment + 1) / segments) * DegreesToRadians;
                    float distance = 0;
                    for (int edge = 0; edge < profile.Length; edge++)
                    {
                        var next = profile[(edge + 1) % profile.Length];
                        var current = profile[edge];
                        float nextDistance = distance + (next - current).magnitude;
                        // Profile roles keep deck tops quiet while exposing
                        // bevels and the lower contact of each individual riser.
                        float currentShade = 1, nextShade = 1;
                        if (edge == 1) { currentShade = 1.12f; nextShade = 1.02f; }
                        else if (edge == 2) nextShade = .8f;
                        else if (edge >= 3 && edge <= 5) currentShade = nextShade = .8f;
                        else if (edge == 6) { currentShade = .8f; nextShade = .95f; }
                        else if (edge == 7) { currentShade = .95f; nextShade = 1.1f; }
                        Quad(Radial(centre, current, a), Radial(centre, current, b), Radial(centre, next, b), Radial(centre, next, a),
                            new Vector2(a * middleRadius / TextureScale, distance / TextureScale), new Vector2(b * middleRadius / TextureScale, distance / TextureScale),
                            new Vector2(b * middleRadius / TextureScale, nextDistance / TextureScale), new Vector2(a * middleRadius / TextureScale, nextDistance / TextureScale),
                            currentShade, currentShade, nextShade, nextShade);
                        distance = nextDistance;
                    }
                }
            }

            private static Vector3 Radial(Vector3 centre, Vector2 point, float angle)
                => centre + new Vector3(Mathf.Cos(angle) * point.x, point.y, Mathf.Sin(angle) * point.x);

            internal void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float sa = 1, float sb = 1, float sc = 1, float sd = 1)
            {
                var normal = Vector3.Cross(b - a, c - a).normalized;
                Quad(a, b, c, d, Project(a, normal), Project(b, normal), Project(c, normal), Project(d, normal), sa, sb, sc, sd);
            }

            internal void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud,
                float sa = 1, float sb = 1, float sc = 1, float sd = 1)
            {
                int first = vertices.Count;
                var normal = Vector3.Cross(b - a, c - a).normalized;
                Add(a, normal, ua, sa); Add(b, normal, ub, sb); Add(c, normal, uc, sc); Add(d, normal, ud, sd);
                indices.Add(first); indices.Add(first + 1); indices.Add(first + 2);
                indices.Add(first); indices.Add(first + 2); indices.Add(first + 3);
            }

            internal void Triangle(Vector3 a, Vector3 b, Vector3 c, float sa = 1, float sb = 1, float sc = 1)
            {
                int first = vertices.Count;
                var normal = Vector3.Cross(b - a, c - a).normalized;
                Add(a, normal, Project(a, normal), sa); Add(b, normal, Project(b, normal), sb); Add(c, normal, Project(c, normal), sc);
                indices.Add(first); indices.Add(first + 1); indices.Add(first + 2);
            }

            private static Vector2 Project(Vector3 vertex, Vector3 normal)
            {
                if (Mathf.Abs(normal.y) > .5f) return new Vector2(vertex.x, vertex.z) / TextureScale;
                if (Mathf.Abs(normal.x) > Mathf.Abs(normal.z)) return new Vector2(vertex.z, vertex.y) / TextureScale;
                return new Vector2(vertex.x, vertex.y) / TextureScale;
            }

            private void Add(Vector3 vertex, Vector3 normal, Vector2 uv, float localShade)
            {
                if (atlasCell.HasValue)
                {
                    var cell = atlasCell.Value;
                    // Instruments fit within one continuous pure-material
                    // cell. Positive offsets prevent per-vertex wrapping
                    // from tearing stripes across fan or quad triangles.
                    float u = uv.x + .5f;
                    float v = uv.y + (Mathf.Abs(normal.y) > .5f ? .5f : .1f);
                    uv = new Vector2(cell.x + Mathf.Clamp01(u) * cell.width, cell.y + Mathf.Clamp01(v) * cell.height);
                }
                vertices.Add(vertex); normals.Add(normal); uvs.Add(uv);
                // The retained unlit shader multiplies its texture by vertex
                // colour. Face orientation and helper-local contact weights
                // give depth without changing illumination or using world height.
                float directional = Mathf.Clamp01(Vector3.Dot(normal, bakeDirection));
                // Quiet base colour carries no painted lighting. A broad key
                // and a small metal glint reveal the actual bevels and normals.
                float highlight = surfaceGlint * Mathf.Pow(Mathf.Clamp01(Vector3.Dot(normal, bakeHalfDirection)), 20);
                // Helper weights above one used to flatten the exposed bevel
                // and deck into the same clipped white. Solid faces retain
                // their directional difference; light strips keep their gain.
                float contactWeight = emissive ? localShade : Mathf.Min(localShade, 1);
                float shade = Mathf.Clamp01(contactWeight * (emissive ? 1 : .63f + .34f * directional + highlight));
                colors.Add(new Color(shade * tint.r, shade * tint.g, shade * tint.b, tint.a));
            }

            internal void WriteMesh(Mesh mesh)
            {
                mesh.Clear();
                mesh.indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
                mesh.vertices = vertices.ToArray(); mesh.normals = normals.ToArray();
                mesh.uv = uvs.ToArray(); mesh.colors = colors.ToArray(); mesh.triangles = indices.ToArray();
                mesh.RecalculateBounds();
            }

            internal MeshRenderer Publish(Transform parent, string name, Material material, Action<UnityEngine.Object> own)
            {
                if (vertices.Count == 0) return null;
                var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
                own(mesh); WriteMesh(mesh);
                var node = new GameObject(name);
                node.transform.SetParent(parent, false);
                node.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = node.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                return renderer;
            }
        }
    }
}
