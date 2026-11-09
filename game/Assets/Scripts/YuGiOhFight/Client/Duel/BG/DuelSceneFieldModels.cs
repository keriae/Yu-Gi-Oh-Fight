using System;
using UnityEngine;

namespace YuGiOhFight.Client.Duel
{
    internal sealed class DuelSceneFieldModel
    {
        internal Transform Root;
        internal Vector3 Display;
        internal Vector2 DisplaySize;
        internal DuelSceneFieldReadout Readout;
        internal Vector3 Minimum = Vector3.one * float.MaxValue, Maximum = Vector3.one * float.MinValue;
        internal Vector2 Footprint;
        internal float ChangeResponse => changePulse;
        internal MeshRenderer Glow;
        private MaterialPropertyBlock block;
        private Texture2D responseTexture;
        private float lastStrength = -1;
        private float responseLevel = -1;
        private Color lastColor;
        private float changePulse, changeTarget;

        internal void SignalChange() => changeTarget = 1;

        private static float Approach(float current, float target, float maxDelta)
        {
            float step = Mathf.Max(0, maxDelta);
            return current < target ? Mathf.Min(current + step, target) : Mathf.Max(current - step, target);
        }

        internal float EaseResponse(float target, float delta)
        {
            target = Mathf.Clamp01(target);
            if (responseLevel < 0) responseLevel = target;
            responseLevel = Approach(responseLevel, target, Mathf.Min(.05f, Mathf.Max(0, delta)) * 4.5f);
            return responseLevel;
        }

        internal void TickResponse(float delta)
        {
            delta = Mathf.Min(.05f, Mathf.Max(0, delta));
            changePulse = Approach(changePulse, changeTarget, delta * 6);
            changeTarget = Approach(changeTarget, 0, delta * 1.25f);
        }

        internal void InitializeResponse(Action<UnityEngine.Object> own)
        {
            if (Glow == null) return;
            var material = Glow.sharedMaterial;
            // The bundled VC graph exposes only _Texture2D. Its narrow physical
            // light inlays get an owned pixel; all other AI surfaces stay intact.
            if (!material.HasProperty("_Texture2D") || material.HasProperty("_EmissionColor")
                || material.HasProperty("_BaseColor") || material.HasProperty("_Color")) return;
            responseTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                { name = "Physical instrument response light", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            own(responseTexture);
        }

        internal void SetResponse(Color color, float strength)
        {
            if (Glow == null || Mathf.Abs(lastStrength - strength) < .025f && lastColor == color) return;
            lastStrength = strength; lastColor = color;
            if (block == null) block = new MaterialPropertyBlock();
            if (responseTexture != null)
            {
                float brightness = .74f + strength * .60f;
                responseTexture.SetPixel(0, 0, new Color(color.r * brightness, color.g * brightness, color.b * brightness, 1));
                responseTexture.Apply(false, false);
            }
            {
                var light = Glow;
                light.GetPropertyBlock(block);
                var material = light.sharedMaterial;
                if (responseTexture != null) block.SetTexture("_Texture2D", responseTexture);
                if (material.HasProperty("_EmissionColor")) ResponseVector(block, "_EmissionColor", color * (1.1f + strength * 1.4f));
                if (material.HasProperty("_BaseColor")) ResponseVector(block, "_BaseColor", Color.Lerp(Color.white, color, .14f + strength * .28f));
                if (material.HasProperty("_Color")) ResponseVector(block, "_Color", Color.Lerp(Color.white, color, .14f + strength * .28f));
                light.SetPropertyBlock(block);
            }
        }

        private static void ResponseVector(MaterialPropertyBlock block, string key, Color value)
            => block.SetVector(key, new Vector4(value.r, value.g, value.b, value.a));
    }

    internal static partial class DuelSceneGeometry
    {
        // Surfaces are batched by material role so the fine dial details do not
        // add individual renderers or change the native instrument identity.
        private sealed class Instrument
        {
            private static readonly int[] AtlasRoles = { 0, 4, 6, 7, 10, 1 };
            internal readonly Surface[] Parts = new Surface[6];
            internal Surface Body => Parts[0];
            internal Surface Screen => Parts[1];
            internal Surface Recess => Parts[2]; internal Surface Base => Parts[3];
            internal Surface Glow => Parts[4];
            internal Surface Trim => Parts[5];
            internal Instrument()
            {
                for (int index = 0; index < Parts.Length; index++)
                {
                    Parts[index] = new Surface(); Parts[index].Atlas(ArtCell(AtlasRoles[index]));
                    Parts[index].Finish(.018f);
                }
                Glow.Emissive(true);
                Recess.Atlas(ArtCell(4)); Recess.Finish(0);
            }
        }

        internal static DuelSceneFieldModel BuildFieldModel(string builder, int kind, Transform parent,
            Material material, Action<UnityEngine.Object> own)
        {
            var root = new GameObject("Integrated physical instrument " + kind);
            root.transform.SetParent(parent, false);
            var model = new DuelSceneFieldModel { Root = root.transform };
            var part = new Instrument();
            SceneInstrumentTints(builder, part);
            if (builder == DuelSceneCatalog.CityId) BuildCityInstrument(kind, part, model);
            else if (builder == DuelSceneCatalog.AcademyId) BuildAcademyInstrument(kind, part, model);
            else if (builder == DuelSceneCatalog.EgyptId) BuildEgyptInstrument(kind, part, model);
            else BuildSceneInstrument(kind, part, model);
            for (int role = 0; role < part.Parts.Length; role++)
            {
                part.Parts[role].ExpandBounds(ref model.Minimum, ref model.Maximum);
                var renderer = part.Parts[role].Publish(root.transform, "Physical instrument part " + role, material, own);
                if (renderer == null) continue;
                if (role == 4) model.Glow = renderer;
            }
            model.Footprint = new Vector2(4.5f, 4.5f);
            return model;
        }
    }
}
