using UnityEngine;

namespace YuGiOhFight.Client.Duel
{
    internal static partial class DuelSceneGeometry
    {
        private static void SceneInstrumentTints(string builder, Instrument p)
        {
            bool egypt = builder == DuelSceneCatalog.EgyptId, academy = builder == DuelSceneCatalog.AcademyId;
            // Every fixture shares one quiet material. The theme changes the
            // surface, while the blue/purple well lights identify the two piles.
            p.Body.Tint(egypt ? new Color(.94f, .88f, .75f) : academy ? new Color(.94f, .96f, .98f) : new Color(.82f, .88f, .94f));
            p.Body.Finish(.045f);
            if (!egypt && !academy) p.Body.Atlas(ArtCell(1));
            p.Base.Tint(egypt ? new Color(.75f, .71f, .61f) : academy ? new Color(.78f, .83f, .89f) : new Color(.68f, .75f, .83f));
            p.Recess.Tint(new Color(.68f, .77f, .84f));
            p.Recess.Atlas(ArtCell(3));
            p.Screen.Atlas(ArtCell(academy ? 0 : egypt ? 2 : 4));
            p.Screen.Tint(academy ? new Color(.98f, .99f, 1) : new Color(.83f, .90f, .94f));
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
