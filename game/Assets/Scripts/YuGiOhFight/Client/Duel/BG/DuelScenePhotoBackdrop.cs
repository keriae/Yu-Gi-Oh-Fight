using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace YuGiOhFight.Client.Duel
{
    // Architecture and the quiet court surface are AI-authored clean plates.
    // Playing zones and instruments are separate geometry with live state.
    internal sealed class DuelScenePhotoBackdrop : MonoBehaviour
    {
        private Camera camera;
        private Material overviewMaterial;
        private MeshRenderer overviewRenderer;
        private Texture2D battleTexture, overviewTexture;
        private Material battleMaterial;
        private Mesh battleMesh, overviewMesh;
        private bool blended;
        private float progress = 1;
        private float fittedAspect = -1, fittedZoom = -1;
        internal Camera SceneCamera => camera;

        internal static DuelScenePhotoBackdrop Create(Transform scene, string builder, Material template,
            Action<UnityEngine.Object> own)
        {
            var root = new GameObject("Fine scene art and atmosphere");
            root.transform.SetParent(scene, false);
            var result = root.AddComponent<DuelScenePhotoBackdrop>();
            result.camera = Runtime.Program.instance.camera_.cameraMain;
            result.battleTexture = Load(builder, "court-v3", own);
            result.overviewTexture = Load(builder, "overview", own);
            var spriteShader = Shader.Find("Sprites/Default");
            result.blended = spriteShader != null && spriteShader.isSupported;
            // Transparent sprite plates must render after the skybox. Opaque
            // cards have already written nearer depth and remain in front.
            int queue = result.blended ? 2998 : 1000;
            result.battleMaterial = result.Material(template, result.blended ? spriteShader : null, result.battleTexture, "Battle clean plate", queue, own);
            result.overviewMaterial = result.Material(template, result.blended ? spriteShader : null, result.overviewTexture, "Establishing clean plate", queue + 1, own);
            result.battleMesh = result.Plane(root.transform, "Battle atmosphere", result.battleMaterial, own, out _);
            result.overviewMesh = result.Plane(root.transform, "Establishing atmosphere", result.overviewMaterial, own, out result.overviewRenderer);
            result.SetBattleProgress(1);
            return result;
        }

        private static Texture2D Load(string builder, string role, Action<UnityEngine.Object> own)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = builder + " " + role };
            own(texture);
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(DuelSceneCatalog.ContentFile("duel-scenes/" + builder + "-" + role + ".png")), true))
                throw new InvalidDataException("Unable to decode clean scene art.");
            texture.wrapMode = TextureWrapMode.Clamp; texture.filterMode = FilterMode.Bilinear;
            return texture;
        }

        private Material Material(Material template, Shader shader, Texture texture, string name, int queue, Action<UnityEngine.Object> own)
        {
            var material = shader != null ? new Material(shader) : new Material(template);
            own(material); material.name = name; material.renderQueue = queue;
            string property = material.HasProperty("_MainTex") && shader != null ? "_MainTex"
                : material.HasProperty("_BaseMap") ? "_BaseMap" : "_Texture2D";
            material.SetTexture(property, texture);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            return material;
        }

        private Mesh Plane(Transform parent, string name, Material material, Action<UnityEngine.Object> own, out MeshRenderer renderer)
        {
            var mesh = new Mesh { name = name,
                vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(-.5f, .5f, 0), new Vector3(.5f, .5f, 0), new Vector3(.5f, -.5f, 0) },
                normals = new[] { new Vector3(0, 0, -1), new Vector3(0, 0, -1), new Vector3(0, 0, -1), new Vector3(0, 0, -1) },
                colors = new[] { Color.white, Color.white, Color.white, Color.white },
                uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right }, triangles = new[] { 0, 1, 2, 0, 2, 3 } };
            own(mesh); mesh.RecalculateBounds();
            var root = new GameObject(name); root.transform.SetParent(parent, false);
            // The front layer is slightly nearer, so the opaque fallback also
            // presents the overview without depth fighting.
            root.transform.localPosition = name == "Establishing atmosphere" ? new Vector3(0, 0, -1) : Vector3.zero;
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            renderer = root.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return mesh;
        }

        internal void SetBattleProgress(float value)
        {
            float next = Mathf.Clamp01(value);
            progress = next;
            if (overviewRenderer == null) return;
            overviewRenderer.enabled = blended ? progress < 1 : progress < .5f;
            if (blended && overviewMaterial.HasProperty("_Color"))
                overviewMaterial.SetColor("_Color", new Color(1, 1, 1, 1 - progress));
        }

        private void LateUpdate()
        {
            if (camera == null || !camera.isActiveAndEnabled) return;
            float distance = camera.farClipPlane * .92f;
            float height = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * .5f) * distance * 2;
            transform.SetPositionAndRotation(camera.transform.position + camera.transform.forward * distance, camera.transform.rotation);
            transform.localScale = new Vector3(height * camera.aspect, height, 1);
            float zoom = 1 + progress * .09f;
            if (Mathf.Abs(fittedAspect - camera.aspect) > .0001f || Mathf.Abs(fittedZoom - zoom) > .0001f)
            {
                Fit(battleMesh, battleTexture, camera.aspect, 1);
                Fit(overviewMesh, overviewTexture, camera.aspect, zoom);
                fittedAspect = camera.aspect; fittedZoom = zoom;
            }
        }

        internal Vector2 ImagePixelToViewport(Vector2 pixel)
        {
            var crop = Crop(battleTexture, camera.aspect, 1);
            return new Vector2((pixel.x / battleTexture.width - crop.x) / crop.width,
                (1 - pixel.y / battleTexture.height - crop.y) / crop.height);
        }

        private static Rect Crop(Texture texture, float frameAspect, float zoom)
        {
            float imageAspect = (float)texture.width / texture.height;
            float u = Mathf.Min(1, frameAspect / imageAspect) / zoom;
            float v = Mathf.Min(1, imageAspect / frameAspect) / zoom;
            float left = (1 - u) * .5f, bottom = (1 - v) * .5f;
            return new Rect(left, bottom, u, v);
        }

        private static void Fit(Mesh mesh, Texture texture, float frameAspect, float zoom)
        {
            var crop = Crop(texture, frameAspect, zoom);
            mesh.uv = new[] { new Vector2(crop.xMin, crop.yMin), new Vector2(crop.xMin, crop.yMax),
                new Vector2(crop.xMax, crop.yMax), new Vector2(crop.xMax, crop.yMin) };
        }
    }
}
