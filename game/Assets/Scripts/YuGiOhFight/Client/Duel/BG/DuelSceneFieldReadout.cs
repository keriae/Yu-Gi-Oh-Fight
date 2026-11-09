using System;
using TMPro;
using UnityEngine;

namespace YuGiOhFight.Client.Duel
{
    // Readouts remain fixed to the physical face and have no input handler.
    internal sealed class DuelSceneFieldReadout
    {
        internal Mesh Mesh;
        internal int Kind;
        internal Vector2 Size;
        internal Color Metal, Accent, Track;
        internal bool FineDial;
        internal TextMeshPro FaceMain, FaceAbove;
        internal Vector3 FaceOrigin;
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
            Material material, Action<UnityEngine.Object> own, TextMeshPro nativeText = null)
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
                Size = model.DisplaySize, FineDial = builder == DuelSceneCatalog.CityId
                    || builder == DuelSceneCatalog.AcademyId || builder == DuelSceneCatalog.EgyptId,
                Metal = builder == DuelSceneCatalog.EgyptId ? new Color(.96f, .82f, .56f)
                    : builder == DuelSceneCatalog.AcademyId ? new Color(.12f, .20f, .30f) : new Color(.96f, .84f, .61f),
                Accent = builder == DuelSceneCatalog.AcademyId ? new Color(.42f, .55f, .68f)
                    : builder == DuelSceneCatalog.EgyptId ? new Color(.80f, .62f, .31f) : new Color(.71f, .57f, .33f),
                Track = builder == DuelSceneCatalog.AcademyId ? new Color(.66f, .73f, .80f)
                    : new Color(.12f, .16f, .20f) };
            if (model.Readout.FineDial && nativeText != null && nativeText.font != null
                && nativeText.fontSharedMaterial != null && nativeText.fontSharedMaterial.shader != null
                && nativeText.fontSharedMaterial.shader.isSupported)
            {
                // Reuse the native field's loaded distance-field font and atlas.
                // Only the material belongs to this dial; the font stays borrowed.
                var lettering = new Material(nativeText.fontSharedMaterial)
                    { name = builder + " inset face lettering", renderQueue = 3001 };
                own(lettering);
                bool ivory = builder == DuelSceneCatalog.AcademyId;
                if (lettering.HasProperty("_FaceColor")) lettering.SetColor("_FaceColor", Color.white);
                if (lettering.HasProperty("_OutlineColor")) lettering.SetColor("_OutlineColor", ivory
                    ? new Color(.69f, .76f, .82f) : new Color(.035f, .048f, .065f));
                if (lettering.HasProperty("_OutlineWidth")) lettering.SetFloat("_OutlineWidth", ivory ? .02f : .045f);
                if (lettering.HasProperty("_FaceDilate")) lettering.SetFloat("_FaceDilate", .015f);
                if (lettering.HasProperty("_ZTestMode")) lettering.SetFloat("_ZTestMode", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                model.Readout.FaceOrigin = model.Display + Vector3.up * .006f;
                model.Readout.FaceMain = InstrumentFaceLabel(model.Root, nativeText.font, lettering, "Phase inscription", 1.4f, ivory);
                model.Readout.FaceAbove = InstrumentFaceLabel(model.Root, nativeText.font, lettering, "Turn inscription", 4.5f, ivory);
            }
            model.Readout.Set("");
        }

        private static TextMeshPro InstrumentFaceLabel(Transform parent, TMP_FontAsset font, Material material,
            string name, float spacing, bool ivory)
        {
            var node = new GameObject(name); node.transform.SetParent(parent, false);
            var label = node.AddComponent<TextMeshPro>();
            label.font = font; label.fontSharedMaterial = material;
            label.fontSize = 36; label.enableAutoSizing = false; label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow; label.richText = false;
            label.raycastTarget = false;
            label.alignment = TextAlignmentOptions.Center; label.characterSpacing = spacing;
            label.fontStyle = FontStyles.Normal; label.color = Color.white;
            label.enableVertexGradient = true;
            var top = ivory ? new Color(.22f, .33f, .45f) : new Color(1, .91f, .69f);
            var bottom = ivory ? new Color(.08f, .14f, .22f) : new Color(.78f, .59f, .31f);
            label.colorGradient = new VertexGradient(top, top, bottom, bottom);
            label.rectTransform.sizeDelta = new Vector2(200, 80);
            label.transform.localRotation = Quaternion.Euler(90, 0, 0);
            label.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            label.GetComponent<MeshRenderer>().receiveShadows = false;
            return label;
        }

        private static void FitInstrumentFaceLabel(TextMeshPro label, string value, Vector3 origin, float z, float width, float height)
        {
            label.text = value; label.transform.localScale = Vector3.one;
            label.ForceMeshUpdate(true, true);
            var bounds = label.textBounds;
            bool visible = !string.IsNullOrWhiteSpace(value) && bounds.size.x > .0001f && bounds.size.y > .0001f;
            label.GetComponent<MeshRenderer>().enabled = visible;
            if (!visible) return;
            float scale = Mathf.Min(width / bounds.size.x, height / bounds.size.y);
            label.transform.localScale = Vector3.one * scale;
            label.transform.localPosition = origin + new Vector3(0, 0, z)
                - label.transform.localRotation * (bounds.center * scale);
        }

        internal static void WriteInstrumentInscription(DuelSceneFieldReadout readout, string main, string above)
        {
            var surface = new Surface();
            var size = readout.Size; var centre = Vector3.zero;
            if (readout.FaceMain != null)
            {
                FitInstrumentFaceLabel(readout.FaceMain, main.ToUpperInvariant(), readout.FaceOrigin,
                    -.10f, size.x * .91f, size.y * (readout.Kind == 1 ? .32f : .43f));
                FitInstrumentFaceLabel(readout.FaceAbove, readout.Kind == 1 ? above.ToUpperInvariant() : "TIME",
                    readout.FaceOrigin, .71f, size.x * .67f, size.y * .15f);
                surface.Tint(readout.Accent); surface.Finish(.12f);
                surface.BevelBox(new Vector3(0, .006f, .395f), new Vector3(1.22f, .010f, .014f), .003f);
                // A restrained seal and twin strokes connect the lettering
                // to each theme's outer crest without crowding the live value.
                surface.BevelBox(new Vector3(0, .006f, -.81f), new Vector3(.105f, .012f, .105f), .005f, 45);
                for (int side = -1; side <= 1; side += 2)
                    surface.BevelBox(new Vector3(side * .245f, .006f, -.81f), new Vector3(.22f, .010f, .016f), .003f);
                if (readout.Kind == 0 && readout.Charge >= 0)
                {
                    float amount = Mathf.Clamp01(readout.Charge), width = size.x * .62f;
                    surface.Tint(readout.Track);
                    surface.BevelBox(new Vector3(0, .005f, -.67f), new Vector3(width, .012f, .048f), .003f);
                    if (amount > .001f)
                    {
                        surface.Tint(amount <= .15f ? new Color(.96f, .53f, .28f) : readout.Metal);
                        surface.BevelBox(new Vector3(-width * (1 - amount) * .5f, .014f, -.67f),
                            new Vector3(width * amount, .010f, .023f), .002f);
                    }
                }
                surface.WriteMesh(readout.Mesh);
                return;
            }
            if (readout.Kind == 1)
            {
                string title = main.ToUpperInvariant();
                SolidGlyphRow(surface, centre + new Vector3(0, 0, readout.FineDial ? -size.y * .045f : 0), title,
                    size.x * .97f, size.y * .52f, readout.Metal, readout.FineDial ? .065f : .08f, readout.FineDial);
                SolidGlyphRow(surface, centre + new Vector3(0, 0, size.y * .39f), above.ToUpperInvariant(),
                    size.x * .78f, size.y * .24f, readout.Metal, .062f, readout.FineDial);
                if (readout.FineDial)
                {
                    surface.Tint(readout.Metal * .73f);
                    surface.BevelBox(new Vector3(0, -.009f, size.y * .255f),
                        new Vector3(size.x * .54f, .014f, .014f), .003f);
                    surface.BevelBox(new Vector3(0, -.009f, -size.y * .39f),
                        new Vector3(size.x * .22f, .014f, .018f), .003f);
                }
            }
            else
            {
                SolidGlyphRow(surface, centre, main, size.x * .82f, size.y * .75f, readout.Metal, .062f, readout.FineDial);
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
            float strokeRatio = .062f, bool fine = false)
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
                        new Vector3(stroke * (fine ? 1.95f : 2.35f), .023f, direction.magnitude + stroke * 1.3f), channelBevel, yaw);
                    surface.Tint(metal); surface.Finish(.09f);
                    surface.BevelBox(middle + Vector3.up * -.006f,
                        new Vector3(stroke * (fine ? 1.5f : 1.8f), .035f, direction.magnitude + stroke), insetBevel, yaw);
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
