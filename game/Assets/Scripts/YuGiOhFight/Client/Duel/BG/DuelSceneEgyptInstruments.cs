using UnityEngine;

namespace YuGiOhFight.Client.Duel
{
    internal static partial class DuelSceneGeometry
    {
        private static void BuildEgyptInstrument(int kind, Instrument p, DuelSceneFieldModel model)
        {
            // The sandstone plinth, lapis face and gold inlay are shared by all
            // four temple fixtures. Only the narrow pile light carries identity.
            p.Body.Atlas(ArtCell(0)); p.Body.Tint(new Color(.96f, .94f, .88f)); p.Body.Finish(.035f);
            p.Base.Atlas(ArtCell(7)); p.Base.Tint(new Color(.83f, .82f, .77f)); p.Base.Finish(.025f);
            p.Recess.Atlas(ArtCell(4)); p.Recess.Tint(new Color(.66f, .70f, .73f)); p.Recess.Finish(.015f);
            p.Screen.Atlas(ArtCell(2)); p.Screen.Tint(new Color(.72f, .81f, .96f)); p.Screen.Finish(.035f);
            p.Trim.Atlas(ArtCell(1)); p.Trim.Tint(new Color(.99f, .95f, .84f)); p.Trim.Finish(.16f);
            p.Glow.Tint(Color.white);
            if (kind >= 2) BuildEgyptWell(kind, p);
            else BuildEgyptDial(p, model);
        }

        private static void BuildEgyptDial(Instrument p, DuelSceneFieldModel model)
        {
            const int segments = 160;
            // A broad stone chapter ring rests on two exposed temple steps.
            // Fine gold rails reveal the bevels without making a metal housing.
            p.Base.Ring(0, 2.25f, .035f, .135f, .030f, segments);
            p.Body.Ring(0, 2.235f, .125f, .267f, .037f, segments);
            p.Trim.Ring(2.183f, 2.219f, .238f, .277f, .009f, segments);
            p.Body.Ring(1.625f, 2.180f, .260f, .476f, .044f, segments);
            p.Trim.Ring(2.087f, 2.197f, .449f, .533f, .022f, segments);
            p.Recess.Ring(1.972f, 2.086f, .497f, .510f, .003f, segments);
            p.Glow.Ring(2.040f, 2.066f, .512f, .525f, .002f, segments);
            p.Body.Ring(1.641f, 1.973f, .472f, .504f, .009f, segments);
            p.Trim.Ring(1.946f, 1.966f, .508f, .520f, .003f, segments);
            p.Trim.Ring(1.626f, 1.675f, .488f, .519f, .007f, segments);
            p.Screen.Ring(0, 1.642f, .473f, .492f, .008f, segments);

            for (int index = 0; index < 48; index++)
            {
                bool major = index % 4 == 0;
                float degrees = index * 7.5f;
                EgyptRadialBar(p.Trim, major ? 1.816f : 1.864f, degrees,
                    .515f, major ? .047f : .026f, major ? .214f : .108f, .014f);
                // The four cardinal caps echo the lintel of a temple doorway.
                if (index % 12 == 0)
                    EgyptRadialBar(p.Trim, 1.910f, degrees, .516f, .103f, .025f, .015f);
            }
            EgyptWingedSun(p.Trim, new Vector3(0, .500f, 1.404f), 0);
            EgyptWingedSun(p.Trim, new Vector3(0, .500f, -1.404f), 180);
            EgyptStoneFlutes(p, 2.180f, .362f, 24);
            model.Display = new Vector3(0, .527f, 0);
            model.DisplaySize = new Vector2(2.90f, 2.25f);
        }

        private static void BuildEgyptWell(int kind, Instrument p)
        {
            const int segments = 160;
            p.Base.Ring(0, 2.25f, .035f, .135f, .030f, segments);
            p.Body.Ring(1.485f, 2.235f, .125f, .267f, .036f, segments);
            p.Trim.Ring(2.183f, 2.219f, .238f, .278f, .009f, segments);
            p.Body.Ring(1.485f, 2.180f, .260f, .476f, .044f, segments);
            p.Trim.Ring(2.087f, 2.197f, .449f, .533f, .022f, segments);
            p.Recess.Ring(1.972f, 2.086f, .496f, .510f, .003f, segments);
            p.Body.Ring(1.617f, 1.973f, .472f, .503f, .009f, segments);
            p.Trim.Ring(1.946f, 1.966f, .507f, .519f, .003f, segments);

            // The dark liner and lapis bed sit .336 units below the gold lip.
            // The visible aperture stays identical for grave and banished cards.
            p.Recess.Ring(1.414f, 1.485f, .172f, .487f, .019f, segments);
            p.Trim.Ring(1.427f, 1.556f, .449f, .522f, .021f, segments);
            p.Recess.Ring(1.552f, 1.617f, .481f, .513f, .008f, segments);
            p.Glow.Ring(1.558f, 1.603f, .517f, .531f, .003f, segments);
            p.Screen.Tint(kind == 2 ? new Color(.68f, .80f, .96f) : new Color(.80f, .74f, .95f));
            p.Screen.Ring(0, 1.414f, .166f, .186f, .006f, segments);
            SoftCityWellLight(p.Glow, 1.075f, 1.386f, .204f, segments);

            // Lotus reliefs and inset lapis stones alternate around the deck;
            // the ornament remains outside the aperture and within its gold rails.
            for (int index = 0; index < 4; index++)
            {
                float degrees = index * 90;
                var centre = EgyptRadialPoint(1.800f, degrees, .510f);
                EgyptLotus(p.Trim, centre, 90 - degrees);
                EgyptLapisSeal(p, EgyptRadialPoint(1.812f, degrees + 45, 0));
            }
            EgyptStoneFlutes(p, 2.180f, .362f, 24);
        }

        private static Vector3 EgyptRadialPoint(float radius, float degrees, float height)
        {
            float angle = degrees * DegreesToRadians;
            return new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
        }

        private static void EgyptRadialBar(Surface surface, float radius, float degrees, float height,
            float width, float length, float depth)
            => surface.BevelBox(EgyptRadialPoint(radius, degrees, height),
                new Vector3(width, depth, length), Mathf.Min(.004f, depth * .2f), 90 - degrees);

        private static void EgyptStoneFlutes(Instrument p, float radius, float height, int count)
        {
            // Fine warm grooves break up the stone riser rather than adding
            // exposed screws or a continuous dark mechanical skirt.
            p.Recess.Atlas(ArtCell(7)); p.Recess.Tint(new Color(.84f, .80f, .71f)); p.Recess.Finish(.015f);
            for (int index = 0; index < count; index++)
                EgyptRadialBar(p.Recess, radius, index * 360f / count + 7.5f,
                    height, .026f, .022f, .102f);
        }

        private static Vector3 EgyptOrnamentPoint(Vector3 centre, float yaw, float x, float z, float y = 0)
        {
            float angle = yaw * DegreesToRadians;
            float cosine = Mathf.Cos(angle), sine = Mathf.Sin(angle);
            return centre + new Vector3(x * cosine + z * sine, y, z * cosine - x * sine);
        }

        private static void EgyptWingedSun(Surface surface, Vector3 centre, float yaw)
        {
            // Paired winged suns sit beyond the live text's upper and lower
            // edges. The stepped feathers read clearly at the native dial size.
            surface.RingAt(new Vector3(centre.x, 0, centre.z), .059f, .090f,
                centre.y - .008f, centre.y + .010f, .003f, 32);
            surface.RingAt(new Vector3(centre.x, 0, centre.z), 0, .044f,
                centre.y - .007f, centre.y + .008f, .003f, 24);
            for (int side = -1; side <= 1; side += 2)
            {
                surface.BevelBox(EgyptOrnamentPoint(centre, yaw, side * .180f, .021f),
                    new Vector3(.205f, .016f, .027f), .003f, yaw);
                for (int feather = 0; feather < 3; feather++)
                    EgyptPetal(surface, EgyptOrnamentPoint(centre, yaw,
                            side * (.152f + feather * .078f), -.034f - feather * .014f),
                        yaw + side * 20, .033f, .114f + feather * .025f, .016f);
                surface.BevelBox(EgyptOrnamentPoint(centre, yaw, side * .347f, .021f),
                    new Vector3(.111f, .016f, .027f), .003f, yaw + side * 16);
            }
        }

        private static void EgyptLotus(Surface surface, Vector3 centre, float yaw)
        {
            // Five tapered raised petals replace the city's fasteners. Their
            // feet join one stem and a small stepped base, like a lotus capital.
            EgyptPetal(surface, EgyptOrnamentPoint(centre, yaw, 0, .027f), yaw, .064f, .220f, .022f);
            for (int side = -1; side <= 1; side += 2)
            {
                EgyptPetal(surface, EgyptOrnamentPoint(centre, yaw, side * .063f, .009f),
                    yaw + side * 25, .045f, .192f, .020f);
                EgyptPetal(surface, EgyptOrnamentPoint(centre, yaw, side * .101f, -.020f),
                    yaw + side * 40, .038f, .146f, .018f);
            }
            surface.BevelBox(EgyptOrnamentPoint(centre, yaw, 0, -.095f),
                new Vector3(.184f, .020f, .025f), .003f, yaw);
            surface.BevelBox(EgyptOrnamentPoint(centre, yaw, 0, -.130f),
                new Vector3(.101f, .018f, .023f), .003f, yaw);
        }

        private static void EgyptPetal(Surface surface, Vector3 centre, float yaw,
            float width, float length, float depth)
        {
            var outline = new[] {
                new Vector2(0, -length * .5f), new Vector2(-width * .45f, -length * .14f),
                new Vector2(-width * .5f, length * .15f), new Vector2(0, length * .5f),
                new Vector2(width * .5f, length * .15f), new Vector2(width * .45f, -length * .14f)
            };
            for (int edge = 0; edge < outline.Length; edge++)
            {
                var current = outline[edge]; var next = outline[(edge + 1) % outline.Length];
                var a = EgyptOrnamentPoint(centre, yaw, current.x, current.y, -depth * .5f);
                var b = EgyptOrnamentPoint(centre, yaw, next.x, next.y, -depth * .5f);
                var c = EgyptOrnamentPoint(centre, yaw, next.x * .87f, next.y * .92f, depth * .5f);
                var d = EgyptOrnamentPoint(centre, yaw, current.x * .87f, current.y * .92f, depth * .5f);
                surface.Quad(a, b, c, d, .80f, .80f, 1.08f, 1.08f);
                surface.Triangle(centre + Vector3.up * (depth * .5f), d, c, 1, 1.08f, 1.08f);
            }
        }

        private static void EgyptLapisSeal(Instrument p, Vector3 centre)
        {
            p.Trim.RingAt(centre, .064f, .094f, .498f, .525f, .006f, 12);
            // This surface's UVs and colour are baked per vertex, allowing a
            // turquoise seal to share the face renderer without another object.
            p.Screen.Atlas(ArtCell(3)); p.Screen.Tint(new Color(.74f, .85f, .96f));
            p.Screen.RingAt(centre, 0, .067f, .500f, .523f, .005f, 12);
        }
    }
}
