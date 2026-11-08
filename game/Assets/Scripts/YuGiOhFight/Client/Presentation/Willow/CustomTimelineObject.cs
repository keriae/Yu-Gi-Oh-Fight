using System.Runtime.CompilerServices;


namespace YuGiOhFight.Client.Presentation.Willow
{
	public class CustomTimelineObject : UnityEngine.MonoBehaviour, UnityEngine.Timeline.ITimeControl
	{
		private UnityEngine.GameObject m_parentObject;

		private UnityEngine.Transform m_transform;

		private UnityEngine.Transform m_transformParent;

		private UnityEngine.Transform m_transformCamera;

		private UnityEngine.Transform m_transformAnim;

		private UnityEngine.Transform m_transformChild;

		private bool m_init;

		private UnityEngine.Animations.PositionConstraint m_positionConstraint;

		private UnityEngine.Animations.RotationConstraint m_rotationConstraint;

		public bool isSyncPosition
		{
			[CompilerGenerated]
			get
			{
				return false;
			}
			[CompilerGenerated]
			set
			{
			}
		}

		public bool isSyncRotation
		{
			[CompilerGenerated]
			get
			{
				return false;
			}
			[CompilerGenerated]
			set
			{
			}
		}

		public bool isSyncScale
		{
			[CompilerGenerated]
			get
			{
				return false;
			}
			[CompilerGenerated]
			set
			{
			}
		}

		public bool isLookAtCamera
		{
			[CompilerGenerated]
			get
			{
				return false;
			}
			[CompilerGenerated]
			set
			{
			}
		}

		public bool isIgnoreInitRotation
		{
			[CompilerGenerated]
			get
			{
				return false;
			}
			[CompilerGenerated]
			set
			{
			}
		}

		public bool isIgnoreInitScale
		{
			[CompilerGenerated]
			get
			{
				return false;
			}
			[CompilerGenerated]
			set
			{
			}
		}

		public bool isRelayChild
		{
			[CompilerGenerated]
			get
			{
				return false;
			}
			[CompilerGenerated]
			set
			{
			}
		}

		private UnityEngine.Transform myTransform => null;

		public UnityEngine.Transform transformChild => null;

		public UnityEngine.Transform transformSync => null;

		public UnityEngine.GameObject targetCameraObject => null;

		public UnityEngine.GameObject targetParentObject => null;

		private UnityEngine.Animations.PositionConstraint positionConstraint => null;

		private UnityEngine.Animations.RotationConstraint rotationConstraint => null;

		public static UnityEngine.Camera cameraSubstituteOfMain
		{
			[CompilerGenerated]
			get
			{
				return null;
			}
			[CompilerGenerated]
			set
			{
			}
		}

		public void OnControlTimeStart()
		{
		}

		public void OnControlTimeStop()
		{
		}

		public void SetTime(double time)
		{
		}

		private void OnValidate()
		{
		}

		private static void GetComponentRoots<T>(UnityEngine.Transform t, System.Collections.Generic.ICollection<T> roots) where T : UnityEngine.Component
		{
		}

		public void SetParentObject(UnityEngine.GameObject parent)
		{
		}

		private bool UpdateTransform(UnityEngine.Transform target, bool isPosition, bool isRotation, bool isScale)
		{
			return false;
		}

		private void LookAtCamera(UnityEngine.Transform target)
		{
		}

		private void ClearConstrain()
		{
		}

		private void SetConstrain(bool isActive)
		{
		}
	}
}
