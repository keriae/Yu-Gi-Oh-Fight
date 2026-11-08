using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI.PropertyOverrider
{
	public abstract class PropertyOverriderBase<Target> : PlatformPropertyOverriderInterface, IPlatformPropertyOverrider where Target : Component
	{
		[SerializeField]
		private PlatformOverriderGroup m_Group;

		[SerializeField]
		private string m_SwitchLabel;

		[SerializeField]
		[HideInInspector]
		private OverrideMode m_OverrideMode;

		private bool m_IsDone;

		public override OverrideMode overrideMode
		{
			get
			{
				return default(OverrideMode);
			}
			set
			{
			}
		}

		public bool isDone => false;

		private void Awake()
		{
		}

		protected virtual Target GetTargetComponent()
		{
			return null;
		}

		private YgomSystem.Utility.DeviceInfo.PlatformType GetCurrentPlatformType()
		{
			return default(YgomSystem.Utility.DeviceInfo.PlatformType);
		}

		public override void ApplyImmediate()
		{
		}

		public override void ApplyImmediate(YgomSystem.Utility.DeviceInfo.PlatformType platformType)
		{
		}

		public override void Import()
		{
		}

		public override void Import(YgomSystem.Utility.DeviceInfo.PlatformType platformType)
		{
		}

		public override void Export()
		{
		}

		public override void Export(YgomSystem.Utility.DeviceInfo.PlatformType platformType)
		{
		}

		public abstract void Export(Target target, YgomSystem.Utility.DeviceInfo.PlatformType platformType);

		public abstract void Import(Target target, YgomSystem.Utility.DeviceInfo.PlatformType platformType);
	}
}
