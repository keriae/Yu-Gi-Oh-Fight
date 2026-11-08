using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using YuGiOhFight.Client.UI.Client;

namespace YuGiOhFight.Client.Duel
{
    // The themed instruments are real meshes. Native handlers keep their original
    // objects and colliders; only their presentation and fitted hit volumes change.
    internal sealed class DuelSceneFieldProps : MonoBehaviour
    {
        private sealed class Plate
        {
            internal DuelSceneFixtureLayout.Fixture Layout;
            internal Transform Anchor;
            internal DuelSceneFieldModel Model;
            internal BoxCollider NativeHit;
            internal GraveBehaviour NativeGrave;
            internal Vector3 NativeCenter, NativeSize, LastCenter, LastSize;
            internal int Side = -1, Kind, LastCount = -1;
            internal float HitPlaneY;
            internal bool Fitted;
        }

        private readonly List<Plate> plates = new List<Plate>();
        private readonly List<UnityEngine.Object> resources = new List<UnityEngine.Object>();
        private readonly Dictionary<Renderer, bool> nativeRenderers = new Dictionary<Renderer, bool>();
        private readonly Plate[] portals = new Plate[4];
        private DuelSceneFixtureLayout layout;
        private DuelScenePhotoBackdrop backdrop;
        private Material surfaceMaterial;
        private Material inscriptionMaterial, shadowMaterial;
        private Plate phase, timer;
        private TextMeshPro timerText, phaseMain;
        private Color themeLight;
        private Vector3 cameraPosition;
        private Quaternion cameraRotation;
        private float cameraAspect = -1, cameraFov = -1;
        private bool released;

        internal static DuelSceneFieldProps Create(Transform parent, string builder, GameObject phaseObject,
            UI.TimerHandler timerHandler, Presentation.Fields.BgEffectManager nearGrave,
            Presentation.Fields.BgEffectManager farGrave, DuelScenePhotoBackdrop backdrop)
        {
            if (!DuelSceneCatalog.IsIntegratedTheme(builder) || backdrop == null) return null;
            var root = new GameObject("Themed physical field instruments"); root.transform.SetParent(parent, false);
            var result = root.AddComponent<DuelSceneFieldProps>();
            try { result.Initialize(builder, phaseObject, timerHandler, nearGrave, farGrave, backdrop); return result; }
            catch (Exception error)
            {
                result.Release(); Destroy(root);
                Debug.LogWarning("[FightClient] Physical field instruments unavailable: " + error.GetType().Name);
                return null;
            }
        }

        private void Initialize(string builder, GameObject phaseObject, UI.TimerHandler timerHandler,
            Presentation.Fields.BgEffectManager nearGrave, Presentation.Fields.BgEffectManager farGrave,
            DuelScenePhotoBackdrop source)
        {
            backdrop = source; layout = DuelSceneFixtureLayout.Load(builder);
            themeLight = builder == DuelSceneCatalog.AcademyId ? new Color(.42f, .78f, 1)
                : builder == DuelSceneCatalog.EgyptId ? new Color(.42f, .83f, .83f) : new Color(1, .66f, .30f);
            BuildMaterials(builder);
            if (phaseObject != null)
            {
                var manager = phaseObject.GetComponent<Presentation.Elements.ElementObjectManager>();
                phaseMain = manager.GetElement<TextMeshPro>("Text");
                RegisterNative(phaseObject);
                phase = MakePlate(builder, "phase", manager.GetElement<Transform>("Button"), 1);
            }
            if (timerHandler != null)
            {
                var manager = timerHandler.GetComponent<Presentation.Elements.ElementObjectManager>();
                timerText = manager.GetElement<TextMeshPro>("Text");
                RegisterNative(timerHandler.gameObject);
                timer = MakePlate(builder, "timer", manager.GetElement<Renderer>("Timer").transform, 0);
            }
            var graves = new[] { nearGrave, farGrave };
            for (int side = 0; side < 2; side++)
            {
                if (graves[side] == null) continue;
                for (int removed = 0; removed < 2; removed++)
                {
                    var anchor = graves[side].GetElement<Transform>(removed == 0 ? "GraveIn" : "ExcludeIn").parent;
                    string id = (side == 0 ? "near-" : "far-") + (removed == 0 ? "grave" : "banish");
                    var plate = MakePlate(builder, id, anchor, removed == 0 ? 2 : 3);
                    plate.Side = side; plate.NativeGrave = graves[side].GetComponent<GraveBehaviour>();
                    portals[side * 2 + removed] = plate;
                }
            }
            SuppressNative();
        }

        private T Own<T>(T resource) where T : UnityEngine.Object { resources.Add(resource); return resource; }

        private void BuildMaterials(string builder)
        {
            var shader = Runtime.ABLoader.LoadDuelSceneVertexColourShader();
            if (shader == null) throw new InvalidOperationException("No supported physical instrument shader.");
            var atlas = Own(new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = builder + " instrument surface atlas" });
            if (!ImageConversion.LoadImage(atlas, File.ReadAllBytes(DuelSceneCatalog.ContentFile(
                "duel-scenes/" + builder + "-field-materials-v4.png")), true))
                throw new InvalidDataException("Invalid instrument surface atlas.");
            atlas.wrapMode = TextureWrapMode.Clamp; atlas.filterMode = FilterMode.Bilinear;
            var white = Own(new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "Solid inscription texture" });
            white.SetPixel(0, 0, Color.white); white.Apply(false, true);
            surfaceMaterial = SurfaceMaterial(shader, atlas, builder + " circular fixture surface");
            inscriptionMaterial = SurfaceMaterial(shader, white, builder + " solid inscriptions");
            var sprite = Shader.Find("Sprites/Default");
            if (sprite != null && sprite.isSupported)
            {
                shadowMaterial = Own(new Material(sprite) { name = "Soft instrument contact shadow", renderQueue = 3000 });
                shadowMaterial.SetTexture("_MainTex", white);
                shadowMaterial.SetColor("_Color", Color.white);
            }
        }

        private Material SurfaceMaterial(Shader shader, Texture texture, string name)
        {
            var material = Own(new Material(shader) { name = name, renderQueue = 2000 });
            string property = material.HasProperty("_BaseMap") ? "_BaseMap" : "_Texture2D";
            material.SetTexture(property, texture);
            material.SetTextureScale(property, Vector2.one); material.SetTextureOffset(property, Vector2.zero);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_ShadowSoftDistance")) material.SetFloat("_ShadowSoftDistance", 1);
            return material;
        }

        private Plate MakePlate(string builder, string id, Transform anchor, int kind)
        {
            var plate = new Plate { Layout = layout.Get(id), Anchor = anchor, Kind = kind };
            plate.Model = DuelSceneGeometry.BuildFieldModel(builder, kind, transform, surfaceMaterial, resource => resources.Add(resource));
            if (kind < 2)
                DuelSceneGeometry.BuildInstrumentInscription(plate.Model, kind, builder, inscriptionMaterial, resource => resources.Add(resource));
            plate.Model.InitializeResponse(resource => resources.Add(resource));
            AddContactShadow(plate.Model);
            plate.Model.Root.gameObject.SetActive(false);
            plates.Add(plate); return plate;
        }

        private void AddContactShadow(DuelSceneFieldModel model)
        {
            if (shadowMaterial == null) return;
            const int segments = 40;
            var vertices = new Vector3[segments * 2 + 1];
            var colors = new Color[vertices.Length]; var uv = new Vector2[vertices.Length];
            var triangles = new int[segments * 9];
            float floor = model.Minimum.y + .007f;
            vertices[0] = new Vector3(0, floor, 0); colors[0] = new Color(0, 0, 0, .28f);
            for (int index = 0; index < segments; index++)
            {
                float angle = index * 3.14159265358979323846f * 2 / segments;
                var radius = new Vector3(Mathf.Cos(angle) * model.Footprint.x * .5f, 0,
                    Mathf.Sin(angle) * model.Footprint.y * .5f);
                vertices[1 + index] = radius * .80f + Vector3.up * floor;
                vertices[1 + segments + index] = radius * 1.12f + Vector3.up * floor;
                colors[1 + index] = new Color(0, 0, 0, .23f); colors[1 + segments + index] = Color.clear;
                int next = (index + 1) % segments, t = index * 9;
                triangles[t] = 0; triangles[t + 1] = 1 + next; triangles[t + 2] = 1 + index;
                triangles[t + 3] = 1 + index; triangles[t + 4] = 1 + next; triangles[t + 5] = 1 + segments + next;
                triangles[t + 6] = 1 + index; triangles[t + 7] = 1 + segments + next; triangles[t + 8] = 1 + segments + index;
            }
            var mesh = Own(new Mesh { name = "Soft contact shadow", vertices = vertices, colors = colors, uv = uv, triangles = triangles });
            mesh.RecalculateBounds();
            var node = new GameObject("Soft contact shadow"); node.transform.SetParent(model.Root, false);
            node.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = node.AddComponent<MeshRenderer>(); renderer.sharedMaterial = shadowMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        }

        private void RegisterNative(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                if (!nativeRenderers.ContainsKey(renderer)) nativeRenderers.Add(renderer, renderer.enabled);
        }
        private void SuppressNative()
        { foreach (var pair in nativeRenderers) if (pair.Key != null) pair.Key.enabled = false; }

        private void LateUpdate()
        {
            if (released || backdrop == null) return;
            SuppressNative();
            var program = Runtime.Program.instance;
            var core = program != null ? program.ocgcore : null;
            bool showing = FightSceneCinematic.GameplayVisible && core != null && core.showing && program.currentServant == core;
            var camera = backdrop.SceneCamera;
            // Hidden opening cameras must not consume the next gameplay refit.
            bool refit = showing && camera != null && (cameraAspect != camera.aspect || cameraFov != camera.fieldOfView
                || cameraPosition != camera.transform.position || cameraRotation != camera.transform.rotation);
            if (refit)
            {
                cameraAspect = camera.aspect; cameraFov = camera.fieldOfView;
                cameraPosition = camera.transform.position; cameraRotation = camera.transform.rotation;
            }
            foreach (var plate in plates)
            {
                bool visible = showing && (plate != timer || timerText != null
                    && timerText.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(timerText.text));
                if (plate.Model.Root.gameObject.activeSelf != visible) plate.Model.Root.gameObject.SetActive(visible);
                if (!visible) { if (refit) plate.Fitted = false; continue; }
                if (refit || !plate.Fitted) FitModel(plate);
                FitNativeHitVolume(plate);
                bool hover = plate.Anchor != null && Runtime.UserInput.HoverObject == plate.Anchor.gameObject;
                if (plate.Kind == 1) hover &= plate.NativeHit != null && plate.NativeHit.enabled
                    && plate.NativeHit.gameObject.activeInHierarchy && PhaseAvailable(core);
                bool press = hover && Runtime.UserInput.MouseLeftPressing;
                float activity = plate.Side < 0 || (plate.Side == 0) == Controllers.OcgCore.myTurn ? .30f : .08f;
                if (plate.Side >= 0)
                {
                    int count = core.GetLocationCardCount(plate.Kind == 2 ? MDPro3.Duel.YGOSharp.CardLocation.Grave
                        : MDPro3.Duel.YGOSharp.CardLocation.Removed, (uint)plate.Side);
                    if (plate.LastCount != count)
                    {
                        if (plate.LastCount >= 0) plate.Model.SignalChange();
                        plate.LastCount = count;
                    }
                }
                plate.Model.TickResponse(Time.unscaledDeltaTime);
                Color light = plate.Kind == 2 ? new Color(.30f, .62f, 1)
                    : plate.Kind == 3 ? new Color(.72f, .45f, 1)
                    : plate.Kind == 1 && !Controllers.OcgCore.myTurn ? new Color(1, .40f, .31f) : themeLight;
                float response = plate.Model.EaseResponse(press ? 1 : hover ? .78f : activity + plate.Model.ChangeResponse * .65f, Time.unscaledDeltaTime);
                plate.Model.SetResponse(light, response);
            }
            if (!showing) return;
            if (phase != null)
            {
                string caption = FixedPhaseCaption(phaseMain != null ? phaseMain.text : "");
                string round = "TURN " + Mathf.Max(1, Controllers.OcgCore.turns);
                if (phase.Model.Readout.LastMain != caption || phase.Model.Readout.LastAbove != round) phase.Model.SignalChange();
                phase.Model.Readout.Set(caption, round);
            }
            if (timer != null)
            {
                string value = timerText != null && timerText.gameObject.activeInHierarchy ? timerText.text : "";
                float charge = -1;
                if (int.TryParse(value, out int seconds))
                {
                    seconds = Mathf.Max(0, seconds); value = seconds.ToString();
                    charge = (float)seconds / Mathf.Max(1, Controllers.OcgCore.timeLimit);
                }
                timer.Model.Readout.Set(value, charge: charge);
            }
        }

        private static bool PhaseAvailable(Controllers.OcgCore core)
        {
            return (UI.PhaseButtonHandler.battlePhase || UI.PhaseButtonHandler.main2Phase || UI.PhaseButtonHandler.endPhase)
                && core != null && core.currentPopup == null && !FightDuelMenu.BlocksInput && !FightOpeningCoin.IsShowing
                && Runtime.UIManager.InputBlocker == null && EventSystem.current != null && !EventSystem.current.IsPointerOverGameObject();
        }

        // Match each compact model's full motion envelope to its layout rectangle.
        // Refit only when the gameplay camera changes, including aspect-ratio changes.
        private void FitModel(Plate plate)
        {
            var quad = plate.Layout.hit;
            if (quad == null || quad.Length != 8) return;
            Vector2 low = Vector2.one * float.MaxValue, high = Vector2.one * float.MinValue;
            for (int corner = 0; corner < 4; corner++)
            {
                var point = backdrop.ImagePixelToViewport(new Vector2(quad[corner * 2], quad[corner * 2 + 1]));
                low = Vector2.Min(low, point); high = Vector2.Max(high, point);
            }
            var target = (low + high) * .5f; var extent = (high - low) * .96f;
            if (!RayToHorizontalPlane(target, .025f, out var centre)
                || !RayToHorizontalPlane(new Vector2(low.x, target.y), .025f, out var left)
                || !RayToHorizontalPlane(new Vector2(high.x, target.y), .025f, out var right)) return;
            var targetCentre = centre;
            float scale = Mathf.Max(.01f, (right - left).magnitude / Mathf.Max(.1f, plate.Model.Footprint.x));
            var model = plate.Model; var middle = (model.Minimum + model.Maximum) * .5f;
            var parentScale = model.Root.parent.lossyScale;
            for (int iteration = 0; iteration < 4; iteration++)
            {
                model.Root.rotation = Quaternion.identity;
                model.Root.localScale = new Vector3(scale / Mathf.Max(.001f, Mathf.Abs(parentScale.x)),
                    scale / Mathf.Max(.001f, Mathf.Abs(parentScale.y)), scale / Mathf.Max(.001f, Mathf.Abs(parentScale.z)));
                model.Root.position = centre - new Vector3(middle.x, model.Minimum.y, middle.z) * scale;
                ProjectBounds(model, out var projectedLow, out var projectedHigh);
                var projectedExtent = projectedHigh - projectedLow;
                if (projectedExtent.x < .00001f || projectedExtent.y < .00001f) return;
                var projectedMiddle = (projectedLow + projectedHigh) * .5f;
                if (RayToHorizontalPlane(projectedMiddle, .025f, out var correction)) centre += targetCentre - correction;
                float adjustment = Mathf.Min(extent.x / projectedExtent.x, extent.y / projectedExtent.y);
                if (iteration < 3) scale *= Mathf.Clamp(adjustment, .5f, 2f);
            }
            model.Root.position = centre - new Vector3(middle.x, model.Minimum.y, middle.z) * scale;
            plate.Fitted = true;
        }

        private void ProjectBounds(DuelSceneFieldModel model, out Vector2 low, out Vector2 high)
        {
            low = Vector2.one * float.MaxValue; high = Vector2.one * float.MinValue;
            for (int corner = 0; corner < 8; corner++)
            {
                var local = new Vector3((corner & 1) == 0 ? model.Minimum.x : model.Maximum.x,
                    (corner & 2) == 0 ? model.Minimum.y : model.Maximum.y, (corner & 4) == 0 ? model.Minimum.z : model.Maximum.z);
                if (!TryProjectViewport(model.Root.TransformPoint(local), out var point)) continue;
                low = Vector2.Min(low, point); high = Vector2.Max(high, point);
            }
        }

        private bool TryProjectViewport(Vector3 world, out Vector2 point)
        {
            point = Vector2.zero;
            var camera = backdrop != null ? backdrop.SceneCamera : null;
            if (camera == null) return false;
            var relative = world - camera.transform.position;
            float depth = Vector3.Dot(relative, camera.transform.forward);
            if (depth <= .00001f) return false;
            float tangent = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * .5f);
            point = new Vector2(.5f + Vector3.Dot(relative, camera.transform.right) / (2 * depth * tangent * camera.aspect),
                .5f + Vector3.Dot(relative, camera.transform.up) / (2 * depth * tangent));
            return true;
        }

        private void FitNativeHitVolume(Plate plate)
        {
            if (!plate.Fitted || plate.Anchor == null) return;
            var scale = plate.Anchor.lossyScale;
            if (Mathf.Abs(scale.x) < .01f || Mathf.Abs(scale.y) < .01f || Mathf.Abs(scale.z) < .01f) return;
            if (plate.NativeHit == null)
            {
                var hit = plate.Anchor.GetComponent<BoxCollider>();
                if (hit == null || hit.bounds.size.sqrMagnitude < .0001f) return;
                if (plate.NativeGrave != null)
                {
                    if (!plate.NativeGrave.TryGetInitialHitVolume(hit, out plate.NativeCenter, out plate.NativeSize)) return;
                }
                else
                {
                    var bounds = hit.bounds;
                    plate.NativeCenter = plate.Anchor.InverseTransformPoint(bounds.center);
                    var local = plate.Anchor.InverseTransformVector(bounds.size);
                    plate.NativeSize = new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z));
                }
                plate.NativeHit = hit;
                // The classic field-root hover cubes reach world Y=10. Hidden
                // art still retains those colliders, so a ground-level mesh
                // volume can lose the closest-hit raycast to an invisible cube.
                // A thin, elevated plate follows the visible model pixels and
                // avoids the enlarged silhouette of a tall oblique-camera box.
                plate.HitPlaneY = Mathf.Max(10.25f, hit.bounds.center.y + hit.bounds.size.y * .5f + .05f);
            }
            var minimum = Vector3.one * float.MaxValue; var maximum = Vector3.one * float.MinValue;
            ProjectBounds(plate.Model, out var low, out var high);
            for (int corner = 0; corner < 4; corner++)
            {
                var viewport = new Vector2((corner & 1) == 0 ? low.x : high.x, (corner & 2) == 0 ? low.y : high.y);
                if (!RayToHorizontalPlane(viewport, plate.HitPlaneY, out var world)) return;
                var point = plate.Anchor.InverseTransformPoint(world);
                minimum = Vector3.Min(minimum, point); maximum = Vector3.Max(maximum, point);
            }
            var nextCentre = (minimum + maximum) * .5f;
            var nextSize = maximum - minimum + new Vector3(.12f / Mathf.Abs(scale.x), .30f / Mathf.Abs(scale.y), .12f / Mathf.Abs(scale.z));
            if ((nextCentre - plate.LastCenter).sqrMagnitude < .000001f && (nextSize - plate.LastSize).sqrMagnitude < .000001f) return;
            plate.NativeHit.center = nextCentre; plate.NativeHit.size = nextSize;
            plate.LastCenter = nextCentre; plate.LastSize = nextSize;
        }

        private bool RayToHorizontalPlane(Vector2 viewport, float height, out Vector3 point)
        {
            point = Vector3.zero;
            var camera = backdrop != null ? backdrop.SceneCamera : null;
            if (camera == null) return false;
            float tangent = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * .5f);
            var direction = camera.transform.forward + camera.transform.right * ((viewport.x - .5f) * 2 * tangent * camera.aspect)
                + camera.transform.up * ((viewport.y - .5f) * 2 * tangent);
            if (Mathf.Abs(direction.y) < .00001f) return false;
            float distance = (height - camera.transform.position.y) / direction.y;
            if (distance <= 0) return false;
            point = camera.transform.position + direction * distance; return true;
        }

        internal bool TryGetPilePosition(int side, bool banish, out Vector3 position)
        {
            position = Vector3.zero;
            if (released || side < 0 || side > 1 || backdrop == null) return false;
            var plate = portals[side * 2 + (banish ? 1 : 0)];
            if (plate == null) return false;
            if (plate.Fitted)
            {
                // Card movement retains its native Y=5 offsets. Projecting the
                // actual lower tray/well bed onto that plane keeps arrivals
                // inside the opening rather than visually above the rim.
                var bed = new Vector3(0, .13f, 0);
                if (TryProjectViewport(plate.Model.Root.TransformPoint(bed), out var viewport))
                    return RayToHorizontalPlane(viewport, 5f, out position);
            }
            var quad = plate.Layout.hit;
            if (quad == null || quad.Length != 8) return false;
            var pixel = Vector2.zero;
            for (int corner = 0; corner < 4; corner++) pixel += new Vector2(quad[corner * 2], quad[corner * 2 + 1]);
            return RayToHorizontalPlane(backdrop.ImagePixelToViewport(pixel * .25f), 5f, out position);
        }

        private static string FixedPhaseCaption(string source)
        {
            return (source ?? "").Trim().ToLowerInvariant() switch
            {
                "draw" => "DRAW", "standby" => "STANDBY", "main1" => "MAIN 1", "main2" => "MAIN 2",
                "battle" => "BATTLE", "end" => "END",
                _ => Controllers.OcgCore.duelPhase switch
                {
                    MDPro3.Duel.YGOSharp.DuelPhase.Draw => "DRAW", MDPro3.Duel.YGOSharp.DuelPhase.Standby => "STANDBY",
                    MDPro3.Duel.YGOSharp.DuelPhase.Main1 => "MAIN 1", MDPro3.Duel.YGOSharp.DuelPhase.Main2 => "MAIN 2",
                    MDPro3.Duel.YGOSharp.DuelPhase.End => "END",
                    MDPro3.Duel.YGOSharp.DuelPhase.BattleStart or MDPro3.Duel.YGOSharp.DuelPhase.BattleStep
                        or MDPro3.Duel.YGOSharp.DuelPhase.Damage or MDPro3.Duel.YGOSharp.DuelPhase.DamageCal
                        or MDPro3.Duel.YGOSharp.DuelPhase.Battle => "BATTLE", _ => ""
                }
            };
        }

        internal void Retire() { Release(); gameObject.SetActive(false); Destroy(gameObject); }
        private void Release()
        {
            if (released) return; released = true;
            foreach (var pair in nativeRenderers) if (pair.Key != null) pair.Key.enabled = pair.Value;
            foreach (var plate in plates)
                if (plate.NativeHit != null) { plate.NativeHit.center = plate.NativeCenter; plate.NativeHit.size = plate.NativeSize; }
            nativeRenderers.Clear(); plates.Clear();
            foreach (var resource in resources) if (resource != null) Destroy(resource);
            resources.Clear();
        }
        private void OnDestroy() => Release();
    }
}
