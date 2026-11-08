namespace YuGiOhFight.Client.Presentation.Effects
{
	public class ScreenEffect : UnityEngine.MonoBehaviour
	{
		public enum ViewType
		{
			View2D = 0,
			View3D = 1
		}

		public bool useCameraSetting;

		public bool useMainCameraSetting;

		public UnityEngine.Vector3 cameraPosition;

		public UnityEngine.Vector3 cameraAngle;

		public ViewType cameraViewType;

		public float camera2DSize;

		public bool overUI;

		public bool setupOnAwake;

		public UnityEngine.Camera targetCamera
		{
			get
			{
                if (cameraViewType == ViewType.View3D)
				{
					if(cameraAngle == UnityEngine.Vector3.zero)
                        return Runtime.Program.instance.camera_.cameraDuelOverlayEffect3D;
                    else
                        return Runtime.Program.instance.camera_.cameraDuelOverlay3D;
                }
                else
				{
                    if (overUI)
                        return Runtime.Program.instance.camera_.cameraDuelOverlayEffect2D;
					else
                        return Runtime.Program.instance.camera_.cameraDuelOverlay2D;
                }
            }
		}

		private void OnEnable()
		{
            _ = DelaySetupAsync();
        }

        private async Cysharp.Threading.Tasks.UniTask DelaySetupAsync()
        {
            await Cysharp.Threading.Tasks.UniTask.Yield(destroyCancellationToken);
            Setup();
        }

        public void Setup()
		{
            if (useMainCameraSetting)
            {
                cameraViewType = ViewType.View3D;
                cameraPosition = Runtime.CameraManager.mainCameraDefaultPosition;
                cameraAngle = Runtime.CameraManager.mainCameraDefaultAngle;
                overUI = false;
            }

            if (targetCamera == Runtime.Program.instance.camera_.cameraDuelOverlayEffect2D)
			{
                Runtime.CameraManager.DuelOverlayEffect2DPlus();
				SetupLayer("DuelOverlayEffect2D");
            }
            else if (targetCamera == Runtime.Program.instance.camera_.cameraDuelOverlay2D)
			{
                Runtime.CameraManager.DuelOverlay2DPlus();
                SetupLayer("DuelOverlay2D");
            }
            else if (targetCamera == Runtime.Program.instance.camera_.cameraDuelOverlayEffect3D)
			{
                Runtime.CameraManager.DuelOverlayEffect3DPlus();
                SetupLayer("DuelOverlayEffect3D");
            }
            else if (targetCamera == Runtime.Program.instance.camera_.cameraDuelOverlay3D)
			{
                Runtime.CameraManager.DuelOverlay3DPlus();
                SetupLayer("DuelOverlay3D");
            }
            SetupCamera(targetCamera);
        }

        public void SetupCamera(UnityEngine.Camera target)
		{
            if (target == Runtime.Program.instance.camera_.cameraDuelOverlay3D && Runtime.CameraManager.overlaySticking)
                return;
			target.transform.localPosition = cameraPosition;
			target.transform.localEulerAngles = cameraAngle;
			if (target.name.Contains("2D"))
			{
                target.orthographicSize = camera2DSize;
            }
        }

        public void TraceCameraSetting(UnityEngine.Camera target, string viewInfoLabel = "Top")
		{
		}

		public static void TraceMainCameraSetting(UnityEngine.Camera target)
		{
		}

		public void SetupLayer(int layer)
		{
		}
        public void SetupLayer(string layerName)
        {
			Utility.Tools.ChangeLayer(gameObject, layerName);
        }

        public void SetupSpriteScaler()
		{
		}

		public void SetupSpriteScaler(float screenWidth, float screenHeight, UnityEngine.Camera setupCamera = null)
		{
		}

        private void OnDisable()
        {
            Runtime.CameraManager manager = Runtime.Program.instance != null ? Runtime.Program.instance.camera_ : null;
            if (manager == null) return;
            if (targetCamera == manager.cameraDuelOverlayEffect2D)
            {
                Runtime.CameraManager.DuelOverlayEffect2DMinus();
            }
            else if (targetCamera == manager.cameraDuelOverlay2D)
            {
                Runtime.CameraManager.DuelOverlay2DMinus();
            }
            else if (targetCamera == manager.cameraDuelOverlayEffect3D)
            {
                Runtime.CameraManager.DuelOverlayEffect3DMinus();
            }
            else if (targetCamera == manager.cameraDuelOverlay3D)
            {
                Runtime.CameraManager.DuelOverlay3DMinus();
            }
        }
    }
}
