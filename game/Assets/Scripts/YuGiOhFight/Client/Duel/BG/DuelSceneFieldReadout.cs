using System;
using UnityEngine;

namespace YuGiOhFight.Client.Duel
{
    // Values are rebuilt as shallow solid strokes in the circular face.
    // This object has no canvas, font texture, billboard, or input handler.
    internal sealed class DuelSceneFieldReadout
    {
        internal Mesh Mesh;
        internal int Kind;
        internal Vector2 Size;
        internal Color Metal;
        internal string LastMain, LastAbove;
        internal float Charge = -1;

        internal void Set(string main, string above = "", float charge = -1)
        {
            main = main ?? ""; above = above ?? "";
            if (LastMain == main && LastAbove == above && Mathf.Abs(Charge - charge) < .005f) return;
            LastMain = main; LastAbove = above;
            Charge = charge;
            DuelSceneGeometry.WriteInstrumentInscription(this, main, above);
        }
    }

    internal static partial class DuelSceneGeometry
    {
        internal static void BuildInstrumentInscription(DuelSceneFieldModel model, int kind, string builder,
            Material material, Action<UnityEngine.Object> own)
        {
            var mesh = new Mesh { name = "Solid inset instrument inscription", hideFlags = HideFlags.DontSave };
            own(mesh);
            var node = new GameObject("Solid inset instrument inscription");
            node.transform.SetParent(model.Root, false);
            node.transform.localPosition = model.Display;
            node.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = node.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            model.Readout = new DuelSceneFieldReadout { Mesh = mesh, Kind = kind,
                Size = model.DisplaySize, Metal = builder == DuelSceneCatalog.EgyptId ? new Color(.96f, .82f, .56f)
                    : builder == DuelSceneCatalog.AcademyId ? new Color(.14f, .21f, .25f) : new Color(.96f, .84f, .61f) };
            model.Readout.Set("");
        }

        internal static void WriteInstrumentInscription(DuelSceneFieldReadout readout, string main, string above)
        {
            var surface = new Surface();
            var size = readout.Size; var centre = Vector3.zero;
            if (readout.Kind == 1)
            {
                string title = main.ToUpperInvariant();
                SolidGlyphRow(surface, centre, title,
                    size.x * .97f, size.y * .52f, readout.Metal, .08f);
                SolidGlyphRow(surface, centre + new Vector3(0, 0, size.y * .39f), above.ToUpperInvariant(),
                    size.x * .78f, size.y * .24f, readout.Metal);
            }
            else
            {
                SolidGlyphRow(surface, centre, main, size.x * .82f, size.y * .75f, readout.Metal);
                if (readout.Charge >= 0)
                {
                    float width = size.x * .78f;
                    var track = centre + new Vector3(0, -.009f, -size.y * .46f);
                    surface.Tint(new Color(.13f, .16f, .18f));
                    surface.BevelBox(track, new Vector3(width, .035f, .067f), .007f);
                    float amount = Mathf.Clamp01(readout.Charge);
                    if (amount > .001f)
                    {
                        surface.Tint(amount <= .15f ? new Color(.96f, .53f, .28f) : readout.Metal);
                        surface.BevelBox(track + new Vector3(-width * (1 - amount) * .5f, .02f, 0),
                            new Vector3(width * amount, .023f, .045f), .005f);
                    }
                }
            }
            surface.WriteMesh(readout.Mesh);
        }

        private static void SolidGlyphRow(Surface surface, Vector3 centre, string text, float width, float height, Color metal,
            float strokeRatio = .062f)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            float advance = Mathf.Min(height * .76f, width / Mathf.Max(1, text.Length));
            float glyphWidth = advance * .66f;
            float glyphHeight = Mathf.Min(height, advance / .76f);
            float stroke = glyphHeight * strokeRatio;
            float channelBevel = Mathf.Min(.005f, stroke * .10f);
            float insetBevel = Mathf.Min(.006f, stroke * .10f);
            for (int index = 0; index < text.Length; index++)
            {
                string segments = SolidGlyphSegments(text[index]);
                float x = (index - (text.Length - 1) * .5f) * advance - glyphWidth * .5f;
                for (int segment = 0; segment + 1 < segments.Length; segment += 2)
                {
                    Vector3 Point(char digit, float y)
                    {
                        int site = digit - '0';
                        return centre + new Vector3(x + site % 3 * glyphWidth * .5f,
                            y, (site / 3 * .5f - .5f) * glyphHeight);
                    }
                    var one = Point(segments[segment], 0); var two = Point(segments[segment + 1], 0);
                    // The channel and its insert have closed bevelled side faces,
                    // sharing the socket depth and remaining fixed to its surface.
                    var direction = two - one;
                    float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                    var middle = (one + two) * .5f;
                    surface.Tint(new Color(.06f, .075f, .08f));
                    surface.BevelBox(middle + Vector3.up * -.028f,
                        new Vector3(stroke * 2.35f, .023f, direction.magnitude + stroke * 1.3f), channelBevel, yaw);
                    surface.Tint(metal); surface.Finish(.09f);
                    surface.BevelBox(middle + Vector3.up * -.006f,
                        new Vector3(stroke * 1.8f, .035f, direction.magnitude + stroke), insetBevel, yaw);
                }
            }
        }

        // A compact, deliberately machined stencil alphabet; every segment is a
        // closed bevelled prism with top, side and end faces rather than a quad.
        private static string SolidGlyphSegments(char letter) => letter switch
        {
            '0' => "68822006", '1' => "7828", '2' => "6885533002", '3' => "68823502",
            '4' => "633582", '5' => "6863355202", '6' => "6806355220", '7' => "6882",
            '8' => "6882200635", '9' => "6882536302",
            'A' => "06688235", 'B' => "066885355220", 'C' => "686002", 'D' => "066778822110",
            'E' => "68600235", 'F' => "686035", 'G' => "6860022535", 'H' => "068235", 'I' => "687102",
            'L' => "6002", 'M' => "06644882", 'N' => "066282", 'O' => "68822006", 'P' => "06688553",
            'R' => "0668855342", 'S' => "6863355202", 'T' => "6871", 'U' => "600228",
            'V' => "6118", 'W' => "6001144228", 'Y' => "644841", '—' => "35", '-' => "35", ':' => "7471", _ => ""
        };
    }
}
