using Cysharp.Threading.Tasks;
using DG.Tweening;
using MDPro3.Servant;
using MDPro3.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MDPro3
{
    public class CameraManager : Manager
    {
        public Camera cameraMain;
        public Camera camera2D;
        public Camera cameraDuelOverlay3D;
        public Camera cameraDuelOverlayEffect3D;
        public Camera cameraDuelOverlay2D;
        public Camera cameraDuelOverlayEffect2D;
        public Camera cameraUI;
        public Camera cameraUIBlur;
        public GameObject light;
        public SpriteRenderer black;

        public UniversalRenderPipelineAsset urpAsset;
        public UniversalRendererData forwardRendererData;
        public UniversalRenderPipelineAsset urpAssetForUI;
        public UniversalRendererData forwardRendererDataForUI;

        public static Vector3 mainCameraDefaultPosition = new(0f, 95f, -37f);
        public static Vector3 mainCameraDefaultAngle = new(70f, 0f, 0f);
        public static int DuelOverlay2DCount = 0;
        public static int DuelOverlayEffect2DCount = 0;
        public static int DuelOverlay3DCount = 0;
        public static int DuelOverlayEffect3DCount = 0;
        public static int uiBlurCount = 0;
        public static bool overlaySticking;
        private float nextRenderDiagnosticCheck;
        private float nextRenderDiagnosticSnapshot;

        public override void PerFrameFunction()
        {
            // The initial four snapshots miss issues that occur during later animations.
            // Keep this read-only probe opt-in and rate limited; it never changes lighting.
            if (Time.realtimeSinceStartup < nextRenderDiagnosticCheck)
                return;
            nextRenderDiagnosticCheck = Time.realtimeSinceStartup + 2f;
            if (!(Environment.GetEnvironmentVariable("MDPRO3_RENDER_DIAGNOSTICS") == "1"
                || File.Exists("Config/render-diagnostics.enabled"))
                || cameraMain == null || !cameraMain.gameObject.activeInHierarchy
                || Time.realtimeSinceStartup < nextRenderDiagnosticSnapshot)
                return;
            nextRenderDiagnosticSnapshot = Time.realtimeSinceStartup + 10f;
            LogRenderDiagnosticsSnapshot("ongoing-duel");
        }

        public override void Initialize()
        {
            base.Initialize();
            urpAsset = Resources.Load<UniversalRenderPipelineAsset>("Settings/URPAsset");
            forwardRendererData = Resources.Load<UniversalRendererData>("Settings/URPAsset_Renderer");
            urpAssetForUI = Resources.Load<UniversalRenderPipelineAsset>("Settings/URPAssetForUI");
            forwardRendererDataForUI = Resources.Load<UniversalRendererData>("Settings/URPAssetForUI_Renderer");

            ShiftTo2D();
            ChangeCameraFOV();
            DuelOverlay2DMinus();
            DuelOverlay3DMinus();
            DuelOverlayEffect2DMinus();
            DuelOverlayEffect3DMinus();
            UIBlurMinus();
            SystemEvent.OnResolutionChange += ChangeCameraFOV;
        }

        public static void ChangeCameraFOV()
        {
            if (DeviceInfo.OnMobile())
            {
                float aspect = (float)Screen.width * 9 / Screen.height;
                if (aspect > 16)
                {
                    Program.instance.camera_.cameraMain.fieldOfView = 30 + 16 - aspect;
                    Program.instance.camera_.cameraDuelOverlay3D.fieldOfView = Program.instance.camera_.cameraMain.fieldOfView;
                }
                else
                {
                    Program.instance.camera_.cameraMain.fieldOfView = 30;
                    Program.instance.camera_.cameraDuelOverlay3D.fieldOfView = 30;
                }
            }
            else
            {
                var camera = Program.instance.camera_;
                if (camera == null || camera.cameraMain == null || camera.cameraDuelOverlay3D == null)
                    return;

                var aspect = Tools.GetScreenAspectRatio();
                var ultraWideT = Mathf.InverseLerp(16f / 9f, 32f / 9f, aspect);
                var duelFov = Mathf.Lerp(30f, 33f, ultraWideT);
                camera.cameraMain.fieldOfView = duelFov;
                camera.cameraDuelOverlay3D.fieldOfView = duelFov;

                if (Program.instance.ocgcore != null && Program.instance.ocgcore.showing)
                {
                    camera.cameraMain.transform.localPosition = mainCameraDefaultPosition;
                    if (!overlaySticking)
                        camera.cameraDuelOverlay3D.transform.localPosition = mainCameraDefaultPosition;
                }
            }
        }

        public static void ShiftTo2D()
        {
            Program.instance.camera_.cameraMain.gameObject.SetActive(false);
            Program.instance.camera_.light.SetActive(false);
            Program.instance.camera_.camera2D.gameObject.SetActive(true);
            QualitySettings.SetQualityLevel(6);
            RuntimeFramePacing.SetDuelRendering(false);
            SettingServant.SetFpsToConfig();
        }

        public static void ShiftTo3D()
        {
            Program.instance.camera_.cameraMain.gameObject.SetActive(true);
            Program.instance.camera_.light.SetActive(true);
            Program.instance.camera_.camera2D.gameObject.SetActive(false);
            QualitySettings.SetQualityLevel((int)Config.GetFloat("Quality", 2f));
            RuntimeFramePacing.SetDuelRendering(true);
            SettingServant.SetFpsToConfig();
            if (Environment.GetEnvironmentVariable("MDPRO3_RENDER_DIAGNOSTICS") == "1"
                || File.Exists("Config/render-diagnostics.enabled"))
                _ = Program.instance.camera_.LogRenderDiagnosticsAsync();
        }

        private async UniTask LogRenderDiagnosticsAsync()
        {
            LogRenderDiagnosticsSnapshot("ShiftTo3D");
            await UniTask.NextFrame();
            LogRenderDiagnosticsSnapshot("next-frame-1");
            await UniTask.NextFrame();
            LogRenderDiagnosticsSnapshot("next-frame-2");
            await UniTask.Delay(1000, true);
            LogRenderDiagnosticsSnapshot("one-second-later");
        }

        private void LogRenderDiagnosticsSnapshot(string stage)
        {
            if (this == null)
                return;
            try
            {
                var prefix = $"[MDPro3RenderDiagnostics] utc={DateTime.UtcNow:O} stage={stage} frame={Time.frameCount}";
                var pipeline = GraphicsSettings.currentRenderPipeline;
                var pipelineName = pipeline == null ? "null" : $"{pipeline.name} ({pipeline.GetType().FullName})";
                var sun = RenderSettings.sun;
                var sunName = sun == null ? "null" : RenderDiagnosticPath(sun.transform);
                Debug.Log($"{prefix} configuredQuality={(int)Config.GetFloat("Quality", 2f)} pixelLightCount={QualitySettings.pixelLightCount} pipeline={pipelineName} shaderPipeline={Shader.globalRenderPipeline} sun={sunName} mainLightColor={Shader.GetGlobalVector("_MainLightColor")} mainLightPosition={Shader.GetGlobalVector("_MainLightPosition")} blackAlpha={(black == null ? -1f : black.color.a)}");
                var activeTarget = RenderTexture.active;
                var mainTarget = cameraMain == null ? null : cameraMain.targetTexture;
                var shadowTexture = Shader.GetGlobalTexture("_MainLightShadowmapTexture");
                Debug.Log($"{prefix} activeRenderTexture={(activeTarget == null ? "null" : activeTarget.name + " " + activeTarget.width + "x" + activeTarget.height)} mainCameraTarget={(mainTarget == null ? "null" : mainTarget.name)} mainLightShadowParams={Shader.GetGlobalVector("_MainLightShadowParams")} shadowTexture={(shadowTexture == null ? "null" : shadowTexture.name + " " + shadowTexture.width + "x" + shadowTexture.height)} uiBlurCount={uiBlurCount} timeScale={Time.timeScale}");

                var cameras = new[] { cameraMain, camera2D, cameraDuelOverlay3D, cameraDuelOverlayEffect3D,
                    cameraDuelOverlay2D, cameraDuelOverlayEffect2D, cameraUI, cameraUIBlur };
                foreach (var camera in cameras)
                {
                    if (camera == null)
                        continue;
                    Debug.Log($"{prefix} camera={RenderDiagnosticPath(camera.transform)} enabled={camera.enabled} activeSelf={camera.gameObject.activeSelf} activeInHierarchy={camera.gameObject.activeInHierarchy} cullingMask=0x{camera.cullingMask:X8} depth={camera.depth} clearFlags={camera.clearFlags}");
                }

                var sceneLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var sceneLight in sceneLights)
                    Debug.Log($"{prefix} light={RenderDiagnosticPath(sceneLight.transform)} enabled={sceneLight.enabled} activeSelf={sceneLight.gameObject.activeSelf} activeInHierarchy={sceneLight.gameObject.activeInHierarchy} type={sceneLight.type} intensity={sceneLight.intensity} color={sceneLight.color} renderingLayerMask=0x{sceneLight.renderingLayerMask:X8} cullingMask=0x{sceneLight.cullingMask:X8} shadows={sceneLight.shadows}");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[MDPro3RenderDiagnostics] stage={stage} logging failed: {exception}");
            }
        }

        internal void LogAcademyReplayRenderDiagnosticsSnapshot(string stage)
        {
            if (!DeckAcademy.HasActiveReplay
                || !(Environment.GetEnvironmentVariable("MDPRO3_RENDER_DIAGNOSTICS") == "1"
                    || File.Exists("Config/render-diagnostics.enabled")))
                return;

            LogRenderDiagnosticsSnapshot(stage);
            try
            {
                var prefix = $"[MDPro3RenderDiagnostics] stage={stage} frame={Time.frameCount}";
                var activeTexture = RenderTexture.active;
                Debug.Log($"{prefix} activeRenderTexture={(activeTexture == null ? "null" : activeTexture.name + " " + activeTexture.width + "x" + activeTexture.height)}");
                var container = Program.instance.container_3D;
                if (container == null)
                    return;
                foreach (var renderer in container.GetComponentsInChildren<Renderer>(true))
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material == null || material.shader == null
                            || !material.shader.name.StartsWith("Shader Graphs/UnlitRecieveShadow", StringComparison.Ordinal))
                            continue;
                        var textures = string.Join(";", material.GetTexturePropertyNames().Select(property =>
                        {
                            var texture = material.GetTexture(property);
                            return property + "=" + (texture == null ? "null" : texture.name + " " + texture.width + "x" + texture.height);
                        }));
                        var values = new List<string>();
                        for (var propertyIndex = 0; propertyIndex < material.shader.GetPropertyCount(); propertyIndex++)
                        {
                            var property = material.shader.GetPropertyName(propertyIndex);
                            var propertyType = material.shader.GetPropertyType(propertyIndex);
                            if (propertyType == ShaderPropertyType.Color)
                                values.Add(property + "=" + material.GetColor(property));
                            else if (propertyType == ShaderPropertyType.Float || propertyType == ShaderPropertyType.Range)
                                values.Add(property + "=" + material.GetFloat(property));
                        }
                        Debug.Log($"{prefix} renderer={RenderDiagnosticPath(renderer.transform)} enabled={renderer.enabled} active={renderer.gameObject.activeInHierarchy} visible={renderer.isVisible} receiveShadows={renderer.receiveShadows} material={material.name} shader={material.shader.name} shaderSupported={material.shader.isSupported} keywords={string.Join(",", material.shaderKeywords)} textures={textures} values={string.Join(";", values)}");
                    }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[MDPro3RenderDiagnostics] stage={stage} academy logging failed: {exception}");
            }
        }

        private static string RenderDiagnosticPath(Transform target)
        {
            var path = target.name;
            while (target.parent != null)
            {
                target = target.parent;
                path = target.name + "/" + path;
            }
            return path;
        }

        public static void Overlay3DReset()
        {
            Program.instance.camera_.cameraDuelOverlay3D.transform.localPosition = mainCameraDefaultPosition;
            Program.instance.camera_.cameraDuelOverlay3D.transform.localEulerAngles = new Vector3(70, 0, 0);
        }

        public static void DuelOverlay2DPlus()
        {
            DuelOverlay2DCount++;
            SetOverlayActive(manager => manager.cameraDuelOverlay2D, true);
        }

        public static void DuelOverlay2DMinus()
        {
            DuelOverlay2DCount--;
            if (DuelOverlay2DCount < 0)
                DuelOverlay2DCount = 0;
            if (DuelOverlay2DCount == 0)
                SetOverlayActive(manager => manager.cameraDuelOverlay2D, false);
        }

        public static void DuelOverlayEffect2DPlus()
        {
            DuelOverlayEffect2DCount++;
            SetOverlayActive(manager => manager.cameraDuelOverlayEffect2D, true);
        }

        public static void DuelOverlayEffect2DMinus()
        {
            DuelOverlayEffect2DCount--;
            if (DuelOverlayEffect2DCount < 0)
                DuelOverlayEffect2DCount = 0;
            if (DuelOverlayEffect2DCount == 0)
                SetOverlayActive(manager => manager.cameraDuelOverlayEffect2D, false);
        }

        public static void DuelOverlay3DPlus()
        {
            DuelOverlay3DCount++;
            SetOverlayActive(manager => manager.cameraDuelOverlay3D, true);
        }

        public static void DuelOverlay3DMinus()
        {
            DuelOverlay3DCount--;
            if (DuelOverlay3DCount < 0)
                DuelOverlay3DCount = 0;
            if (DuelOverlay3DCount == 0)
                SetOverlayActive(manager => manager.cameraDuelOverlay3D, false);
        }

        public static void DuelOverlayEffect3DPlus()
        {
            DuelOverlayEffect3DCount++;
            SetOverlayActive(manager => manager.cameraDuelOverlayEffect3D, true);
        }

        public static void DuelOverlayEffect3DMinus()
        {
            DuelOverlayEffect3DCount--;
            if (DuelOverlayEffect3DCount < 0)
                DuelOverlayEffect3DCount = 0;
            if (DuelOverlayEffect3DCount == 0)
                SetOverlayActive(manager => manager.cameraDuelOverlayEffect3D, false);
        }

        public static void UIBlurPlus()
        {
            uiBlurCount++;
            SetOverlayActive(manager => manager.cameraUIBlur, true);
        }

        public static void UIBlurMinus()
        {
            uiBlurCount--;
            if (uiBlurCount < 0)
                uiBlurCount = 0;
            if (uiBlurCount == 0)
            {
                SetOverlayActive(manager => manager.cameraUIBlur, false);
            }
        }

        private static void SetOverlayActive(Func<CameraManager, Camera> select, bool active)
        {
            // ScreenEffect.OnDisable may run after Unity has destroyed cameras.
            // Keep the reference counts balanced while skipping retired objects.
            var program = Program.instance;
            if (program == null || program.camera_ == null) return;
            var camera = select(program.camera_);
            if (camera != null) camera.gameObject.SetActive(active);
        }

        #region Black Transition

        private static Sequence blackTransition;

        public static void ResetDuelBlack()
        {
            blackTransition?.Kill();
            blackTransition = null;
            var backdrop = Program.instance?.camera_?.black;
            if (backdrop == null) return;
            var color = backdrop.color;
            color.a = 0f;
            backdrop.color = color;
        }

        private static Sequence BeginBlackTransition()
        {
            blackTransition?.Kill();
            blackTransition = DOTween.Sequence();
            return blackTransition;
        }

        public static void BlackInOut(float delay, float inTime, float time, float outTime)
        {
            var sequence = BeginBlackTransition();
            sequence.AppendInterval(delay);
            sequence.Append(Program.instance.camera_.black.DOFade(0.75f, inTime));
            sequence.AppendInterval(time);
            sequence.Append(Program.instance.camera_.black.DOFade(0, outTime));
        }

        public static void BlackIn(float delay, float inTime)
        {
            var sequence = BeginBlackTransition();
            sequence.AppendInterval(delay);
            sequence.Append(Program.instance.camera_.black.DOFade(0.75f, inTime));
        }

        public static void BlackOut(float delay, float outTime)
        {
            var sequence = BeginBlackTransition();
            sequence.AppendInterval(delay);
            sequence.Append(Program.instance.camera_.black.DOFade(0f, outTime));
        }

        #endregion

        public static void ShakeCamera(bool heavy = false)
        {
            if (heavy)
            {
                Program.instance.camera_.cameraMain.DOShakePosition(0.4f, 5, 100);
            }
            else
            {
                Program.instance.camera_.cameraMain.DOShakePosition(0.2f, 0.5f, 50);
            }
        }

        public static void Duel3DOverlayStickWithMain(bool stick)
        {
            if (stick)
            {
                overlaySticking = true;
                Program.instance.camera_.cameraDuelOverlay3D.transform.SetParent(Program.instance.camera_.cameraMain.transform, false);
                Program.instance.camera_.cameraDuelOverlay3D.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            else
            {
                overlaySticking = false;
                Program.instance.camera_.cameraDuelOverlay3D.transform.SetParent(Program.instance.camera_.transform, false);
                Program.instance.camera_.cameraDuelOverlay3D.transform.SetLocalPositionAndRotation
                    (mainCameraDefaultPosition, Quaternion.Euler(mainCameraDefaultAngle));
            }
        }
    }
}
