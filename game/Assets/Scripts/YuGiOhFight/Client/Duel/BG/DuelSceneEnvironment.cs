using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace YuGiOhFight.Client.Duel
{
    // The owned visual theme keeps the field controllers and interaction anchors.
    internal sealed class DuelSceneEnvironment : MonoBehaviour
    {
        private readonly List<Object> ownedResources = new List<Object>();
        private readonly Dictionary<Renderer, bool> authoredRenderers = new Dictionary<Renderer, bool>();
        private readonly List<Renderer> retiredRenderers = new List<Renderer>();
        private readonly Dictionary<GameObject, bool> mateAnchorStates = new Dictionary<GameObject, bool>();
        private GameObject celestialSphere;
        private GameObject nearField;
        private GameObject farField;
        private bool sphereWasActive;
        private bool suppressClassic;
        private bool suppressMates;
        private bool disposed;
        private DuelSceneBindings bindings;
        private DuelScenePhotoBackdrop backdrop;

        internal bool HasOwnTheme => backdrop != null && !disposed;
        internal bool MatesHidden => suppressMates && !disposed;
        internal DuelScenePhotoBackdrop Backdrop => backdrop;

        internal static DuelSceneEnvironment Create(Transform parent, GameObject near, GameObject far, GameObject sphere,
            bool completeTheme, DuelSceneBindings bindings)
        {
            var root = new GameObject("Fight duel environment");
            try
            {
                root.transform.SetParent(parent, false);
                var environment = root.AddComponent<DuelSceneEnvironment>();
                environment.celestialSphere = sphere;
                environment.nearField = near;
                environment.farField = far;
                environment.sphereWasActive = sphere != null && sphere.activeSelf;
                environment.bindings = bindings;
                foreach (var anchor in bindings.MateAnchors)
                    if (anchor != null) environment.mateAnchorStates.Add(anchor.gameObject, anchor.gameObject.activeSelf);
                environment.RegisterThemeVisuals(near, completeTheme);
                environment.RegisterThemeVisuals(far, completeTheme);
                if (completeTheme)
                    foreach (var cosmetic in bindings.CosmeticRoots) environment.RegisterVisuals(cosmetic);
                return environment;
            }
            catch { Object.Destroy(root); throw; }
        }

        internal async UniTask<bool> BuildAsync(bool enabled, DuelSceneDefinition scene, Func<bool> stillCurrent)
        {
            try
            {
                if (enabled && scene.Builder != DuelSceneCatalog.DefaultId)
                {
                    var geometry = new GameObject("Scene " + scene.Id);
                    geometry.transform.SetParent(transform, false);
                    geometry.SetActive(false);
                    await UniTask.Yield();
                    if (disposed || !stillCurrent()) { Retire(); return false; }
                    Material template = FindSurfaceMaterial(nearField, farField);
                    if (template == null) throw new InvalidOperationException("No supported opaque field surface material is loaded.");
                    Material landscape = Surface(template, Solid(Color.white), "Fine art template");
                    // The court artwork stays free of baked interactive fixtures.
                    // Native-rule instruments and zone markings occupy real space.
                    backdrop = DuelScenePhotoBackdrop.Create(geometry.transform, scene.Builder, landscape, resource => ownedResources.Add(resource));
                    DuelSceneCourtMarkings.Create(geometry.transform, scene.Builder, resource => ownedResources.Add(resource));
                    // Only the fallback template belongs to this environment;
                    // borrowed field templates are absent from ownedResources.
                    // Each surface clone now owns its state independently.
                    if (ownedResources.Remove(template)) Object.Destroy(template);
                    InitializeSurfaceProperties(geometry);
                    if (disposed || !stillCurrent()) { Retire(); return false; }
                    geometry.SetActive(true);
                }
                if (disposed || !stillCurrent()) { Retire(); return false; }
                suppressClassic = !enabled || scene.Builder != DuelSceneCatalog.DefaultId;
                suppressMates = enabled && DuelSceneCatalog.IsIntegratedTheme(scene.Builder);
                ApplyVisibility();
                return true;
            }
            catch (Exception error)
            {
                Retire();
                if (stillCurrent())
                    Debug.LogWarning("[FightClient] duel scene construction failed ("
                        + (error is InvalidOperationException || error is InvalidDataException ? error.Message : error.GetType().Name)
                        + "); using classic environment.");
                return false;
            }
        }

        private T Own<T>(T resource) where T : Object
        {
            ownedResources.Add(resource); return resource;
        }

        private Texture2D Solid(Color color)
        {
            var texture = Own(new Texture2D(1, 1, TextureFormat.RGBA32, false));
            texture.name = "Fight scene solid surface";
            texture.SetPixel(0, 0, color); texture.Apply(false, true);
            return texture;
        }

        private Material FindSurfaceMaterial(GameObject near, GameObject far)
        {
            var sources = new IEnumerable<Material>[]
            {
                GetSurfaceMaterials(near), GetSurfaceMaterials(far), Resources.FindObjectsOfTypeAll<Material>()
            };
            // A near-field unlit fallback must not hide a vertex-colour
            // template already loaded by the far field or another resource.
            foreach (var materials in sources)
            {
                var material = SelectSurfaceMaterial(materials);
                if (material != null) return material;
            }
            // Own only the template material. The audited shader remains
            // owned by its fixed bundle across duel entries.
            var shader = Runtime.ABLoader.LoadDuelSceneVertexColourShader();
            if (shader != null)
            {
                var template = Own(new Material(shader));
                template.name = "Fight scene vertex colour template";
                if (template.renderQueue < 3000
                    && (!template.HasProperty("_Surface") || template.GetFloat("_Surface") < .5f)
                    && (template.HasProperty("_BaseMap") || template.HasProperty("_Texture2D"))) return template;
                ownedResources.Remove(template); Object.Destroy(template);
            }
            // A complete theme needs vertex colour for both landscape colour
            // and baked shading. Failure restores the intact classic field.
            return null;
        }

        private static List<Material> GetSurfaceMaterials(GameObject field)
        {
            // The field has already loaded these exact native material/shader
            // objects. Do not assume an opened shader bundle registers every
            // asset for Shader.Find, or discard authored shader keywords.
            var materials = new List<Material>();
            if (field != null)
                foreach (var renderer in field.GetComponentsInChildren<Renderer>(true)) materials.AddRange(renderer.sharedMaterials);
            return materials;
        }

        private static Material SelectSurfaceMaterial(IEnumerable<Material> materials)
        {
            foreach (var material in materials)
                if (material != null && material.shader != null && material.shader.name == "Shader Graphs/UnlitRecieveShadow_vc"
                    && material.shader.isSupported && material.renderQueue < 3000
                    && (!material.HasProperty("_Surface") || material.GetFloat("_Surface") < .5f)
                    && (material.HasProperty("_BaseMap") || material.HasProperty("_Texture2D"))) return material;
            return null;
        }

        private Material Surface(Material template, Texture texture, string part)
        {
            var material = Own(new Material(template));
            material.name = "Fight scene " + part;
            string textureProperty = material.HasProperty("_BaseMap") ? "_BaseMap" : "_Texture2D";
            material.SetTexture(textureProperty, texture);
            material.SetTextureScale(textureProperty, Vector2.one);
            material.SetTextureOffset(textureProperty, Vector2.zero);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 0);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1);
            // Retain the template's compiled keyword variant. Stone and solid
            // textures are opaque, so authored alpha testing still passes.
            material.renderQueue = 2000;
            return material;
        }

        private static void InitializeSurfaceProperties(GameObject geometry)
        {
            foreach (var renderer in geometry.GetComponentsInChildren<Renderer>(true))
            {
                var material = renderer.sharedMaterial;
                if (material == null) continue;
                var block = new MaterialPropertyBlock();
                // Older bundled shaders can retain stale Metal SRP constants
                // after cloning. Submit the owned texture through the renderer
                // even when this graph has no shadow-distance property.
                string textureProperty = material.HasProperty("_BaseMap") ? "_BaseMap"
                    : material.HasProperty("_Texture2D") ? "_Texture2D" : material.HasProperty("_MainTex") ? "_MainTex" : null;
                if (textureProperty != null && material.GetTexture(textureProperty) != null)
                    block.SetTexture(textureProperty, material.GetTexture(textureProperty));
                if (material.HasProperty("_ShadowSoftDistance"))
                    block.SetFloat("_ShadowSoftDistance", material.GetFloat("_ShadowSoftDistance"));
                renderer.SetPropertyBlock(block);
            }
        }

        private void RegisterThemeVisuals(GameObject field, bool completeTheme)
        {
            if (field == null) return;
            // Bound stands and portals are registered separately. Their mate
            // anchor subtrees and all controller identities remain intact.
            foreach (Transform child in field.transform)
            {
                if (child.name.StartsWith("POS_", StringComparison.Ordinal) || child.name == "GetLightDummy")
                {
                    // Keep anchor transforms and lighting probes, but hide
                    // their authored cube markers on the clean theme floor.
                    if (completeTheme) RegisterVisuals(child.gameObject);
                    continue;
                }
                // Simplified mode keeps the original playable surface and pile
                // sockets. A complete theme supplies these surfaces itself.
                if (!completeTheme && (child.name == "duelfield" || child.name == "deck" || child.name == "boundary")) continue;
                RegisterVisuals(child.gameObject);
            }
        }

        private void RegisterVisuals(GameObject root)
        {
            if (root == null) return;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                if (!bindings.ContainsMate(renderer.transform) && !authoredRenderers.ContainsKey(renderer))
                    authoredRenderers.Add(renderer, renderer.enabled);
        }

        internal void SuppressAuthoredEffect(GameObject effect)
        {
            if (!HasOwnTheme) return;
            RegisterVisuals(effect); ApplyVisibility();
        }

        private void LateUpdate() => ApplyVisibility();

        internal void SetOpeningProgress(float battle) => backdrop?.SetBattleProgress(battle);

        internal void ApplyVisibility()
        {
            if (!suppressClassic || disposed) return;
            // Disable the original parent anchors so loaded forms and any later
            // premium swaps inherit the hidden state without changing mate logic.
            if (suppressMates)
                foreach (var anchor in mateAnchorStates.Keys)
                    if (anchor != null && anchor.activeSelf) anchor.SetActive(false);
            if (celestialSphere != null && celestialSphere.activeSelf) celestialSphere.SetActive(false);
            retiredRenderers.Clear();
            foreach (var renderer in authoredRenderers.Keys)
                if (renderer == null) retiredRenderers.Add(renderer);
                else if (renderer.enabled) renderer.enabled = false;
            foreach (var renderer in retiredRenderers) authoredRenderers.Remove(renderer);
        }

        internal void Retire() => Release(true);

        private void Release(bool destroyRoot)
        {
            if (disposed) return;
            disposed = true;
            if (suppressClassic)
            {
                if (suppressMates)
                    foreach (var pair in mateAnchorStates)
                        if (pair.Key != null) pair.Key.SetActive(pair.Value);
                foreach (var pair in authoredRenderers)
                    if (pair.Key != null) pair.Key.enabled = pair.Value;
                if (celestialSphere != null) celestialSphere.SetActive(sphereWasActive);
            }
            authoredRenderers.Clear(); retiredRenderers.Clear(); bindings = null; backdrop = null;
            mateAnchorStates.Clear();
            celestialSphere = nearField = farField = null;
            if (destroyRoot) gameObject.SetActive(false);
            foreach (var resource in ownedResources)
                if (resource != null) Object.Destroy(resource);
            ownedResources.Clear();
            if (destroyRoot) Object.Destroy(gameObject);
        }

        private void OnDestroy() => Release(false);
    }
}
