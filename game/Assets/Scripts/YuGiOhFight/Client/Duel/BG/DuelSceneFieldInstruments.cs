using UnityEngine;

namespace YuGiOhFight.Client.Duel
{
    internal static partial class DuelSceneGeometry
    {
        private static void SceneInstrumentTints(string builder, Instrument p)
        {
            // These builders configure every surface before emitting geometry.
            if (builder == DuelSceneCatalog.AcademyId || builder == DuelSceneCatalog.EgyptId) return;
            if (builder == DuelSceneCatalog.CityId)
            {
                p.Body.Atlas(ArtCell(0)); p.Body.Tint(new Color(.71f, .79f, .87f)); p.Body.Finish(.09f);
                p.Base.Atlas(ArtCell(7)); p.Base.Tint(new Color(.55f, .63f, .71f)); p.Base.Finish(.05f);
                p.Recess.Atlas(ArtCell(4)); p.Recess.Tint(new Color(.38f, .46f, .55f)); p.Recess.Finish(.025f);
                p.Screen.Atlas(ArtCell(2)); p.Screen.Tint(new Color(.67f, .77f, .87f)); p.Screen.Finish(.02f);
                p.Trim.Atlas(ArtCell(1)); p.Trim.Tint(new Color(.86f, .85f, .77f)); p.Trim.Finish(.14f);
                p.Glow.Tint(Color.white);
                return;
            }
            // The generic fallback retains quiet metal surfaces and pile lights.
            p.Body.Tint(new Color(.82f, .88f, .94f));
            p.Body.Finish(.045f);
            p.Body.Atlas(ArtCell(1));
            p.Base.Tint(new Color(.68f, .75f, .83f));
            p.Recess.Tint(new Color(.68f, .77f, .84f));
            p.Recess.Atlas(ArtCell(3));
            p.Screen.Atlas(ArtCell(4));
            p.Screen.Tint(new Color(.83f, .90f, .94f));
            p.Glow.Tint(Color.white);
        }

        private static void BuildSceneInstrument(int kind, Instrument p, DuelSceneFieldModel model)
        {
            const float radius = 2.25f;
            p.Base.Ring(0, radius, .04f, .14f, .018f, 72);
            bool well = kind >= 2;
            p.Body.Ring(well ? 1.64f : 0, radius, .14f, .50f, .080f, 72);
            if (well)
            {
                // Both piles use the same circular opening and low stone rim.
                // A single narrow blue/violet ring replaces labels and ornaments.
                p.Recess.Ring(0, 1.64f, .105f, .13f, 0, 72);
                p.Glow.Ring(1.640f, 1.760f, .504f, .520f, .003f, 72);
                SoftWellLight(p.Glow, 1.08f, 1.625f, .143f);
            }
            else
            {
                // The turn and clock are flat circular faces. Their live text
                // is engraved directly in the disc, without a rectangular socket.
                p.Screen.Ring(0, 1.99f, .501f, .508f, 0, 72);
                p.Glow.Ring(2.060f, 2.108f, .513f, .520f, .002f, 72);
                model.Display = new Vector3(0, .516f, 0);
                model.DisplaySize = new Vector2(3.20f, 2.18f);
            }
        }

        private static void BuildCityInstrument(int kind, Instrument p, DuelSceneFieldModel model)
        {
            if (kind >= 2) BuildCityWell(kind, p);
            else BuildCityDial(p, model);
        }

        private static void BuildCityWell(int kind, Instrument p)
        {
            const int segments = 160;
            // Matching steel skirts and exposed brass chamfers give the pile
            // sockets the same machined finish as the city's phase instrument.
            p.Body.Tint(new Color(.82f, .87f, .92f)); p.Body.Finish(.12f);
            p.Trim.Tint(new Color(.97f, .91f, .77f)); p.Trim.Finish(.18f);
            p.Recess.Tint(new Color(.36f, .42f, .50f)); p.Recess.Finish(.04f);
            p.Screen.Tint(kind == 2 ? new Color(.69f, .78f, .91f) : new Color(.77f, .69f, .89f));
            p.Base.Ring(0, 2.25f, .035f, .135f, .030f, segments);
            p.Body.Ring(1.485f, 2.225f, .125f, .263f, .033f, segments);
            p.Trim.Ring(2.180f, 2.214f, .238f, .278f, .009f, segments);
            p.Body.Ring(1.485f, 2.16f, .257f, .466f, .047f, segments);
            p.Trim.Ring(2.017f, 2.174f, .438f, .526f, .022f, segments);
            p.Recess.Ring(1.970f, 2.017f, .487f, .503f, .004f, segments);
            p.Body.Ring(1.617f, 1.973f, .468f, .496f, .008f, segments);
            p.Trim.Ring(1.944f, 1.961f, .500f, .512f, .003f, segments);
            // A deep dark liner remains visible beneath the turned brass mouth.
            // The bed is over .33 units below the lip rather than a raised button.
            p.Recess.Ring(1.414f, 1.485f, .172f, .487f, .020f, segments);
            p.Trim.Ring(1.427f, 1.556f, .449f, .522f, .021f, segments);
            p.Recess.Ring(1.552f, 1.617f, .481f, .513f, .008f, segments);
            p.Glow.Ring(1.558f, 1.603f, .517f, .531f, .003f, segments);
            p.Screen.Ring(0, 1.414f, .166f, .186f, .006f, segments);
            SoftCityWellLight(p.Glow, 1.075f, 1.386f, .204f, segments);
            // The screws sit on the exposed deck, not beneath the tall outer rim.
            // Eight slightly larger heads retain their shape at the pile's size.
            CityRimFasteners(p, 1.820f, .489f, 8, 22.5f, 1.16f);
            for (int index = 0; index < 4; index++)
            {
                float degrees = index * 90;
                CityRadialBar(p.Trim, 1.790f, degrees, .510f, .050f, .255f, .013f);
                CityRadialBar(p.Trim, 1.812f, degrees - 3.5f, .510f, .038f, .160f, .013f);
                CityRadialBar(p.Trim, 1.812f, degrees + 3.5f, .510f, .038f, .160f, .013f);
            }
        }

        private static void BuildCityDial(Instrument p, DuelSceneFieldModel model)
        {
            const int segments = 160;
            // A recessed navy face sits inside a broad graduated metal chapter
            // ring. Separate risers expose the brass chamfer and steel skirt.
            p.Body.Tint(new Color(.82f, .87f, .92f)); p.Body.Finish(.12f);
            p.Trim.Tint(new Color(.97f, .91f, .77f)); p.Trim.Finish(.18f);
            p.Screen.Tint(new Color(.78f, .84f, .92f));
            p.Base.Ring(0, 2.25f, .035f, .135f, .030f, segments);
            p.Body.Ring(0, 2.225f, .125f, .265f, .035f, segments);
            p.Trim.Ring(2.180f, 2.214f, .238f, .277f, .009f, segments);
            p.Body.Ring(1.625f, 2.16f, .260f, .466f, .050f, segments);
            p.Trim.Ring(2.017f, 2.174f, .438f, .533f, .025f, segments);
            p.Recess.Ring(1.960f, 2.014f, .502f, .513f, .003f, segments);
            p.Glow.Ring(1.990f, 2.014f, .516f, .529f, .002f, segments);
            p.Body.Ring(1.641f, 1.966f, .476f, .506f, .008f, segments);
            p.Trim.Ring(1.947f, 1.963f, .509f, .519f, .003f, segments);
            p.Trim.Ring(1.626f, 1.675f, .488f, .519f, .007f, segments);
            p.Screen.Ring(0, 1.642f, .475f, .492f, .008f, segments);
            // At the actual 130px dial size, major divisions remain about 1.5px
            // wide. Shorter intermediate marks retain a clear gap to both rails.
            for (int index = 0; index < 48; index++)
            {
                bool major = index % 4 == 0;
                CityRadialBar(p.Trim, major ? 1.823f : 1.870f, index * 7.5f,
                    .516f, major ? .048f : .026f, major ? .218f : .110f, .012f);
            }
            CityRimFasteners(p, 2.093f, .514f, 4, 45);
            CityDialCrown(p.Trim, .509f);
            model.Display = new Vector3(0, .527f, 0);
            model.DisplaySize = new Vector2(2.90f, 2.25f);
        }

        private static void CityRimFasteners(Instrument p, float radius, float height, int count, float offset = 22.5f, float size = 1)
        {
            for (int index = 0; index < count; index++)
            {
                float degrees = index * 360f / count + offset;
                float angle = degrees * DegreesToRadians;
                var centre = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                p.Recess.RingAt(centre, 0, .078f * size, height - .015f, height + .016f, .007f, 16);
                p.Trim.RingAt(centre, 0, .052f * size, height + .012f, height + .038f, .006f, 16);
                p.Recess.BevelBox(centre + Vector3.up * (height + .041f),
                    new Vector3(.017f * size, .007f, .062f * size), .002f, 90 - degrees);
            }
        }

        private static void CityRadialBar(Surface surface, float radius, float degrees, float height,
            float width, float length, float depth)
        {
            float angle = degrees * DegreesToRadians;
            surface.BevelBox(new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius),
                new Vector3(width, depth, length), Mathf.Min(.004f, depth * .2f), 90 - degrees);
        }

        private static void CityDialCrown(Surface surface, float height)
        {
            // Three little rising fins echo the skyline on the upper and lower
            // edges of the dial; the live text stays in the clear central face.
            for (int side = -1; side <= 1; side += 2)
                for (int fin = -1; fin <= 1; fin++)
                    surface.BevelBox(new Vector3(fin * .120f, height, side * 1.39f),
                        new Vector3(.055f, .013f, fin == 0 ? .22f : .15f), .003f);
            for (int side = -1; side <= 1; side += 2)
                surface.BevelBox(new Vector3(0, height, side * 1.275f),
                    new Vector3(.40f, .013f, .018f), .003f);
        }

        private static void SoftCityWellLight(Surface surface, float inner, float outer, float height, int segments)
        {
            for (int segment = 0; segment < segments; segment++)
            {
                float a = segment * Pi * 2 / segments, b = (segment + 1) * Pi * 2 / segments;
                Vector3 Point(float radius, float angle)
                    => new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
                // Restrained blue/violet spill lights only the recessed bed's
                // edge; the narrow emissive line at the mouth carries identity.
                surface.Quad(Point(inner, a), Point(inner, b), Point(outer, b), Point(outer, a),
                    .055f, .055f, .22f, .22f);
            }
        }

        private static void SoftWellLight(Surface surface, float inner, float outer, float height)
        {
            const int segments = 72;
            for (int segment = 0; segment < segments; segment++)
            {
                float a = segment * Pi * 2 / segments, b = (segment + 1) * Pi * 2 / segments;
                Vector3 Point(float radius, float angle)
                    => new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
                // Low radial gain creates a soft colour wash on the dark bed.
                // The response texture supplies blue for grave and purple for banish.
                surface.Quad(Point(inner, a), Point(inner, b), Point(outer, b), Point(outer, a),
                    .08f, .08f, .40f, .40f);
            }
        }
    }
}
