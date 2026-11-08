using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace YuGiOhFight.Client.Runtime
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

        internal static float BattleFieldOfView()
        {
            var background = Program.instance?.ocgcore?.DuelBGManager;
            float basis = background != null && background.Scene3DActive && Duel.DuelSceneCatalog.IsIntegratedTheme(background.ActiveSceneId)
                ? background.ActiveSceneId == Duel.DuelSceneCatalog.EgyptId ? 38 : 36 : 30;
            if (MDPro3.Utility.DeviceInfo.OnMobile())
                return Mathf.Min(basis, basis + 16 - (float)Screen.width * 9 / Screen.height);
            return basis + Mathf.Lerp(0, basis == 30 ? 3 : 2, Mathf.InverseLerp(16f / 9f, 32f / 9f, Utility.Tools.GetScreenAspectRatio()));
        }

        public static void ChangeCameraFOV()
        {
            var program = Program.instance;
            var camera = program?.camera_;
            if (camera == null || camera.cameraMain == null || camera.cameraDuelOverlay3D == null)
                return;

            float duelFov = BattleFieldOfView();
            camera.cameraMain.fieldOfView = duelFov;
            camera.cameraDuelOverlay3D.fieldOfView = duelFov;

            if (!MDPro3.Utility.DeviceInfo.OnMobile() && program.ocgcore != null && program.ocgcore.showing)
            {
                camera.cameraMain.transform.localPosition = mainCameraDefaultPosition;
                if (!overlaySticking)
                    camera.cameraDuelOverlay3D.transform.localPosition = mainCameraDefaultPosition;
            }
        }

        public static void ShiftTo2D()
        {
            Program.instance.camera_.cameraMain.gameObject.SetActive(false);
            Program.instance.camera_.light.SetActive(false);
            Program.instance.camera_.camera2D.gameObject.SetActive(true);
            QualitySettings.SetQualityLevel(6);
            MDPro3.Utility.RuntimeFramePacing.SetDuelRendering(false);
            Controllers.SettingServant.SetFpsToConfig();
        }

        public static void ShiftTo3D()
        {
            Program.instance.camera_.cameraMain.gameObject.SetActive(true);
            Program.instance.camera_.light.SetActive(true);
            Program.instance.camera_.camera2D.gameObject.SetActive(false);
            QualitySettings.SetQualityLevel((int)MDPro3.Config.GetFloat("Quality", 2f));
            MDPro3.Utility.RuntimeFramePacing.SetDuelRendering(true);
            Controllers.SettingServant.SetFpsToConfig();
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
            Program program = Program.instance;
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
