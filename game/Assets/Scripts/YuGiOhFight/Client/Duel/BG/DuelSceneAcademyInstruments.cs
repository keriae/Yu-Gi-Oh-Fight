using UnityEngine;

namespace YuGiOhFight.Client.Duel
{
    internal static partial class DuelSceneGeometry
    {
        private static void BuildAcademyInstrument(int kind, Instrument p, DuelSceneFieldModel model)
        {
            // Porcelain, brushed silver and blue inlays echo the academy's
            // white architecture while keeping the recessed mechanisms clear.
            p.Body.Atlas(ArtCell(6)); p.Body.Tint(new Color(.96f, .97f, .98f)); p.Body.Finish(.045f);
            p.Base.Atlas(ArtCell(14)); p.Base.Tint(new Color(.57f, .64f, .72f)); p.Base.Finish(.045f);
            p.Trim.Atlas(ArtCell(1)); p.Trim.Tint(new Color(.88f, .93f, .98f)); p.Trim.Finish(.16f);
            p.Recess.Atlas(ArtCell(4)); p.Recess.Tint(new Color(.63f, .74f, .88f)); p.Recess.Finish(.02f);
            p.Glow.Tint(Color.white);
            if (kind >= 2) BuildAcademyWell(kind, p);
            else BuildAcademyDial(p, model);
        }

        private static void BuildAcademyDial(Instrument p, DuelSceneFieldModel model)
        {
            const int segments = 160;
            p.Screen.Atlas(ArtCell(0)); p.Screen.Tint(new Color(.97f, .98f, 1)); p.Screen.Finish(.015f);
            p.Base.Ring(0, 2.25f, .035f, .133f, .029f, segments);
            p.Body.Ring(0, 2.225f, .127f, .281f, .036f, segments);
            p.Trim.Ring(2.185f, 2.220f, .217f, .263f, .010f, segments);
            p.Body.Ring(1.625f, 2.167f, .271f, .466f, .044f, segments);
            p.Trim.Ring(2.019f, 2.174f, .439f, .531f, .023f, segments);
            p.Recess.Ring(1.981f, 2.020f, .494f, .512f, .004f, segments);
            p.Glow.Ring(1.995f, 2.014f, .516f, .528f, .002f, segments);
            p.Body.Ring(1.640f, 1.984f, .476f, .506f, .008f, segments);
            p.Trim.Ring(1.962f, 1.978f, .507f, .518f, .003f, segments);
            p.Trim.Ring(1.625f, 1.675f, .488f, .519f, .007f, segments);
            p.Screen.Ring(0, 1.642f, .475f, .492f, .008f, segments);
            // Dark blue graduations stay legible against the ivory chapter
            // ring at the native dial size. Every fourth mark is a full division.
            for (int index = 0; index < 48; index++)
            {
                bool major = index % 4 == 0;
                AcademyRadialBar(p.Recess, major ? 1.823f : 1.870f, index * 7.5f,
                    .516f, major ? .048f : .026f, major ? .218f : .110f, .012f);
            }
            AcademyFasteners(p, 2.093f, .512f, 4, 45);
            AcademyDialCrests(p, .512f);
            // The shared live-font layout has the same clear area as the city
            // dial; this light face uses the academy's navy text palette.
            model.Display = new Vector3(0, .527f, 0);
            model.DisplaySize = new Vector2(2.90f, 2.25f);
        }

        private static void BuildAcademyWell(int kind, Instrument p)
        {
            const int segments = 160;
            p.Screen.Atlas(ArtCell(2));
            p.Screen.Tint(kind == 2 ? new Color(.66f, .78f, .93f) : new Color(.78f, .69f, .93f));
            p.Screen.Finish(.02f);
            p.Base.Ring(0, 2.25f, .035f, .133f, .029f, segments);
            p.Body.Ring(1.483f, 2.225f, .127f, .267f, .035f, segments);
            p.Trim.Ring(2.185f, 2.220f, .221f, .267f, .010f, segments);
            p.Body.Ring(1.483f, 2.167f, .261f, .466f, .044f, segments);
            p.Trim.Ring(2.019f, 2.174f, .439f, .526f, .022f, segments);
            p.Recess.Ring(1.981f, 2.020f, .489f, .505f, .004f, segments);
            p.Body.Ring(1.614f, 1.984f, .469f, .496f, .008f, segments);
            p.Trim.Ring(1.952f, 1.970f, .499f, .511f, .003f, segments);
            // The navy liner and low bed form an actual cavity over .33 units
            // deep. The silver mouth and its thin light remain separate surfaces.
            p.Recess.Ring(1.412f, 1.483f, .170f, .484f, .020f, segments);
            p.Trim.Ring(1.427f, 1.556f, .448f, .522f, .021f, segments);
            p.Recess.Ring(1.552f, 1.617f, .480f, .513f, .008f, segments);
            p.Glow.Ring(1.558f, 1.602f, .517f, .530f, .003f, segments);
            p.Screen.Ring(0, 1.412f, .165f, .184f, .006f, segments);
            AcademyWellReflection(p.Glow, 1.075f, 1.385f, .202f, segments);
            AcademyFasteners(p, 1.820f, .490f, 8, 22.5f, 1.14f);
            // Four bordered blue panels recall the academy's facade bays.
            // Small divisions between them make the open deck feel machined.
            for (int index = 0; index < 12; index++)
            {
                float degrees = index * 30f;
                if (index % 3 == 0)
                {
                    AcademyRadialBar(p.Trim, 1.808f, degrees, .508f, .158f, .244f, .012f);
                    AcademyRadialBar(p.Recess, 1.808f, degrees, .518f, .110f, .195f, .006f);
                }
                else AcademyRadialBar(p.Recess, 1.830f, degrees, .503f, .028f, .098f, .009f);
            }
        }

        private static void AcademyFasteners(Instrument p, float radius, float height,
            int count, float offset, float scale = 1)
        {
            for (int index = 0; index < count; index++)
            {
                float degrees = index * 360f / count + offset;
                float angle = degrees * DegreesToRadians;
                var centre = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                p.Recess.RingAt(centre, 0, .082f * scale, height - .017f, height + .011f, .006f, 24);
                p.Trim.RingAt(centre, 0, .058f * scale, height + .009f, height + .034f, .006f, 24);
                for (int slot = 0; slot < 2; slot++)
                    p.Recess.BevelBox(centre + Vector3.up * (height + .036f),
                        new Vector3(.014f * scale, .005f, .068f * scale), .001f, 90 - degrees + slot * 90);
            }
        }

        private static void AcademyRadialBar(Surface surface, float radius, float degrees,
            float height, float width, float length, float depth)
        {
            float angle = degrees * DegreesToRadians;
            surface.BevelBox(new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius),
                new Vector3(width, depth, length), Mathf.Min(.004f, depth * .2f), 90 - degrees);
        }

        private static void AcademyDialCrests(Instrument p, float height)
        {
            // Two compact shield-and-book inlays leave the live title, value
            // and divider clear. Their silver leaves echo an academic laurel.
            for (int side = -1; side <= 1; side += 2)
            {
                AcademyShield(p.Trim, side, height, .148f, 1.488f, 1.344f, 1.218f);
                AcademyShield(p.Recess, side, height + .004f, .119f, 1.465f, 1.349f, 1.254f);
                for (int page = -1; page <= 1; page += 2)
                    p.Trim.BevelBox(new Vector3(side * page * .047f, height + .010f, side * 1.390f),
                        new Vector3(.071f, .009f, .076f), .002f, page * 11);
                p.Trim.BevelBox(new Vector3(0, height + .012f, side * 1.389f),
                    new Vector3(.011f, .009f, .087f), .002f);
                for (int leaf = 0; leaf < 3; leaf++)
                    for (int wing = -1; wing <= 1; wing += 2)
                        p.Trim.BevelBox(new Vector3(side * wing * (.208f + leaf * .014f),
                            height + .004f, side * (1.282f + leaf * .078f)),
                            new Vector3(.038f, .011f, .083f), .002f, wing * (38 - leaf * 12));
            }
        }

        private static void AcademyShield(Surface surface, int side, float height,
            float halfWidth, float top, float shoulder, float tip)
        {
            Vector3 Point(float x, float z) => new Vector3(side * x, height, side * z);
            var centre = Point(0, shoulder);
            var outline = new[] { Point(-halfWidth, top), Point(halfWidth, top),
                Point(halfWidth, shoulder), Point(0, tip), Point(-halfWidth, shoulder) };
            for (int edge = 0; edge < outline.Length; edge++)
                surface.Triangle(centre, outline[edge], outline[(edge + 1) % outline.Length]);
        }

        private static void AcademyWellReflection(Surface surface, float inner, float outer,
            float height, int segments)
        {
            for (int segment = 0; segment < segments; segment++)
            {
                float a = segment * Pi * 2 / segments, b = (segment + 1) * Pi * 2 / segments;
                Vector3 Point(float radius, float angle)
                    => new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
                surface.Quad(Point(inner, a), Point(inner, b), Point(outer, b), Point(outer, a),
                    .05f, .05f, .20f, .20f);
            }
        }
    }
}
