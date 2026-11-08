using System;
using System.IO;
using UnityEngine;

namespace YuGiOhFight.Client.Duel
{
    // Every coordinate is a pixel in the camera-aligned court background, with a top-left origin.
    // Quadrilaterals use TL, TR, BR, BL so camera cropping never changes the annotations.
    [Serializable]
    internal sealed class DuelSceneFixtureLayout
    {
        public int width = 0, height = 0;
        public Fixture[] fixtures = null;

        [Serializable]
        internal sealed class Fixture
        {
            public string id = null;
            public float[] hit = null;
        }

        internal static DuelSceneFixtureLayout Load(string builder)
        {
            string path = DuelSceneCatalog.ContentFile("duel-scenes/" + builder + "-fixture-layout-v3.json");
            var result = JsonUtility.FromJson<DuelSceneFixtureLayout>(File.ReadAllText(path));
            if (result == null || result.width < 1 || result.height < 1 || result.fixtures == null)
                throw new InvalidDataException("Invalid fixture image layout.");
            string[] ids = { "far-grave", "far-banish", "timer", "phase", "near-banish", "near-grave" };
            foreach (var id in ids)
            {
                var fixture = result.Get(id);
                if (fixture == null) throw new InvalidDataException("Missing fixture annotation: " + id);
                result.ValidateQuad(fixture.hit);
            }
            return result;
        }

        internal Fixture Get(string id)
        {
            Fixture found = null;
            foreach (var fixture in fixtures)
                if (fixture != null && fixture.id == id)
                {
                    if (found != null) throw new InvalidDataException("Duplicate fixture annotation: " + id);
                    found = fixture;
                }
            return found;
        }

        private void ValidateQuad(float[] quad)
        {
            if (quad == null || quad.Length == 0)
                throw new InvalidDataException("Missing fixture quadrilateral.");
            if (quad.Length != 8) throw new InvalidDataException("A fixture quadrilateral must have four corners.");
            ValidatePoints(quad);
            float area = 0;
            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) % 4;
                area += quad[i * 2] * quad[next * 2 + 1] - quad[next * 2] * quad[i * 2 + 1];
            }
            if (Mathf.Abs(area) < 1) throw new InvalidDataException("Empty fixture quadrilateral.");
        }

        private void ValidatePoints(float[] points)
        {
            for (int i = 0; i < points.Length; i += 2)
                if (float.IsNaN(points[i]) || float.IsInfinity(points[i]) || float.IsNaN(points[i + 1]) || float.IsInfinity(points[i + 1])
                    || points[i] < 0 || points[i] > width || points[i + 1] < 0 || points[i + 1] > height)
                    throw new InvalidDataException("Fixture annotation lies outside the image.");
        }
    }
}
