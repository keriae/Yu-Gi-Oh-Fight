using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using YuGiOhFight.Client.UI.Client;
using CardLocation = MDPro3.Duel.YGOSharp.CardLocation;
using CardPosition = MDPro3.Duel.YGOSharp.CardPosition;

namespace YuGiOhFight.Client.Duel
{
    // The corner engravings use the same coordinates as the actual card models.
    // They provide a quiet court grid without taking over the themed floor.
    internal sealed class DuelSceneCourtMarkings : MonoBehaviour
    {
        private MeshRenderer markings;
        private readonly List<Vector3> zones = new List<Vector3>(22);
        private readonly List<MeshRenderer> contacts = new List<MeshRenderer>(22);
        private readonly bool[] occupied = new bool[22];
        private readonly bool[] defence = new bool[22];

        internal static DuelSceneCourtMarkings Create(Transform parent, string builder, Action<UnityEngine.Object> own)
        {
            if (!DuelSceneCatalog.IsIntegratedTheme(builder)) return null;
            var shader = Shader.Find("Sprites/Default");
            if (shader == null || !shader.isSupported) return null;
            var root = new GameObject("Themed duel court engravings"); root.transform.SetParent(parent, false);
            var result = root.AddComponent<DuelSceneCourtMarkings>();
            var white = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "Court engraving white pixel" };
            own(white); white.SetPixel(0, 0, Color.white); white.Apply(false, true);
            var material = new Material(shader) { name = builder + " subtle court engravings", renderQueue = 3000 };
            own(material); material.SetTexture("_MainTex", white); material.SetColor("_Color", Color.white);
            result.BuildMarkings(builder, material, own);
            var shadowMaterial = new Material(shader) { name = "Court card contact shadows", renderQueue = 2999 };
            own(shadowMaterial); shadowMaterial.SetTexture("_MainTex", white); shadowMaterial.SetColor("_Color", Color.white);
            result.BuildContacts(shadowMaterial, own);
            return result;
        }

        private void BuildMarkings(string builder, Material material, Action<UnityEngine.Object> own)
        {
            var vertices = new List<Vector3>(720);
            var colors = new List<Color>(720);
            var triangles = new List<int>(1080);
            var positions = new HashSet<long>();
            bool academy = builder == DuelSceneCatalog.AcademyId, egypt = builder == DuelSceneCatalog.EgyptId;
            Color ink = academy ? new Color(.16f, .29f, .39f, .30f)
                : egypt ? new Color(.38f, .29f, .17f, .34f) : new Color(.59f, .65f, .69f, .34f);
            Color extraInk = academy ? new Color(.25f, .51f, .67f, .36f)
                : egypt ? new Color(.19f, .43f, .44f, .37f) : new Color(.67f, .53f, .34f, .42f);
            for (uint side = 0; side < 2; side++)
            {
                for (uint row = 0; row < 2; row++)
                {
                    uint count = row == 0 ? 7u : 5u;
                    for (uint sequence = 0; sequence < count; sequence++)
                    {
                        var gps = new GPS { controller = side, location = (uint)(row == 0 ? CardLocation.MonsterZone : CardLocation.SpellZone),
                            sequence = sequence, position = (int)CardPosition.FaceUpAttack };
                        var centre = GameCard.GetCardPosition(gps); centre.y = .039f;
                        long key = ((long)Mathf.RoundToInt(centre.x * 100) << 32) ^ (uint)Mathf.RoundToInt(centre.z * 100);
                        if (!positions.Add(key)) continue;
                        zones.Add(centre);
                        Color color = row == 0 && sequence >= 5 ? extraInk : ink;
                        const float halfWidth = 3.48f, halfDepth = 3.86f, stroke = .105f, length = .98f;
                        for (int x = -1; x <= 1; x += 2)
                            for (int z = -1; z <= 1; z += 2)
                            {
                                var corner = centre + new Vector3(x * halfWidth, 0, z * halfDepth);
                                Line(vertices, colors, triangles, corner, corner - Vector3.right * x * length, stroke, color);
                                Line(vertices, colors, triangles, corner, corner - Vector3.forward * z * length, stroke, color);
                            }
                    }
                }
            }
            Color divider = ink; divider.a *= .56f;
            Line(vertices, colors, triangles, new Vector3(-21.2f, .039f, 0), new Vector3(-12.8f, .039f, 0), .071f, divider);
            Line(vertices, colors, triangles, new Vector3(-4.40f, .039f, 0), new Vector3(4.40f, .039f, 0), .071f, divider);
            Line(vertices, colors, triangles, new Vector3(12.8f, .039f, 0), new Vector3(21.2f, .039f, 0), .071f, divider);
            var mesh = new Mesh { name = "Twenty-two duel zone corner engravings", vertices = vertices.ToArray(),
                colors = colors.ToArray(), triangles = triangles.ToArray() };
            own(mesh); mesh.RecalculateBounds();
            markings = Publish(transform, "Duel zone corner engravings", mesh, material); markings.enabled = false;
        }

        private static void Line(List<Vector3> vertices, List<Color> colors, List<int> triangles,
            Vector3 from, Vector3 to, float width, Color color)
        {
            var direction = to - from;
            var across = Vector3.Cross(Vector3.up, direction).normalized * width * .5f;
            int index = vertices.Count;
            vertices.Add(from - across); vertices.Add(to - across); vertices.Add(to + across); vertices.Add(from + across);
            for (int vertex = 0; vertex < 4; vertex++) colors.Add(color);
            triangles.Add(index); triangles.Add(index + 1); triangles.Add(index + 2);
            triangles.Add(index); triangles.Add(index + 2); triangles.Add(index + 3);
        }

        private void BuildContacts(Material material, Action<UnityEngine.Object> own)
        {
            const int segments = 32;
            var vertices = new Vector3[segments * 2 + 1]; var colors = new Color[vertices.Length];
            var triangles = new int[segments * 9];
            colors[0] = new Color(0, 0, 0, .19f);
            for (int segment = 0; segment < segments; segment++)
            {
                float a = segment * 3.14159265358979323846f * 2 / segments;
                var radius = new Vector3(Mathf.Cos(a) * 3.73f, 0, Mathf.Sin(a) * 4.43f);
                vertices[segment + 1] = radius * .73f; vertices[segment + 1 + segments] = radius;
                colors[segment + 1] = new Color(0, 0, 0, .15f); colors[segment + 1 + segments] = Color.clear;
                int next = (segment + 1) % segments, index = segment * 9;
                triangles[index] = 0; triangles[index + 1] = next + 1; triangles[index + 2] = segment + 1;
                triangles[index + 3] = segment + 1; triangles[index + 4] = next + 1; triangles[index + 5] = next + 1 + segments;
                triangles[index + 6] = segment + 1; triangles[index + 7] = next + 1 + segments; triangles[index + 8] = segment + 1 + segments;
            }
            var mesh = new Mesh { name = "Soft field card contact shadow", vertices = vertices, colors = colors, triangles = triangles };
            own(mesh); mesh.RecalculateBounds();
            foreach (var zone in zones)
            {
                var renderer = Publish(transform, "Field card contact", mesh, material);
                renderer.transform.localPosition = new Vector3(zone.x, .025f, zone.z); renderer.enabled = false;
                contacts.Add(renderer);
            }
        }

        private static MeshRenderer Publish(Transform parent, string name, Mesh mesh, Material material)
        {
            var node = new GameObject(name); node.transform.SetParent(parent, false);
            node.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = node.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return renderer;
        }

        private void LateUpdate()
        {
            var program = Runtime.Program.instance;
            var core = program != null ? program.ocgcore : null;
            bool showing = FightSceneCinematic.GameplayVisible && core != null && core.showing && program.currentServant == core;
            if (markings != null && markings.enabled != showing) markings.enabled = showing;
            Array.Clear(occupied, 0, occupied.Length); Array.Clear(defence, 0, defence.Length);
            if (showing)
                foreach (var card in Controllers.OcgCore.cards)
                {
                    if (card == null || card.p == null || card.model == null || !card.model.activeInHierarchy
                        || card.p.InLocation(CardLocation.Overlay) || !card.p.InLocation(CardLocation.MonsterZone, CardLocation.SpellZone)) continue;
                    var position = GameCard.GetCardPosition(card.p);
                    for (int index = 0; index < zones.Count; index++)
                    {
                        var delta = zones[index] - position;
                        if (delta.x * delta.x + delta.z * delta.z > .04f) continue;
                        occupied[index] = true;
                        defence[index] = card.p.InLocation(CardLocation.MonsterZone) && card.p.InPosition(CardPosition.Defence);
                        break;
                    }
                }
            for (int index = 0; index < contacts.Count; index++)
            {
                contacts[index].enabled = showing && occupied[index];
                contacts[index].transform.localRotation = defence[index] ? Quaternion.Euler(0, 90, 0) : Quaternion.identity;
            }
        }
    }
}
