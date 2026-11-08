namespace YuGiOhFight.Client.Presentation.UI.PropertyOverrider
{
	public abstract class PlatformPropertyOverriderInterface : UnityEngine.MonoBehaviour, IPlatformPropertyOverrider
	{
		public abstract OverrideMode overrideMode { get; set; }

		public abstract void ApplyImmediate();

		public abstract void ApplyImmediate(YgomSystem.Utility.DeviceInfo.PlatformType platformType);

		public abstract void Export();

		public abstract void Export(YgomSystem.Utility.DeviceInfo.PlatformType platformType);

		public abstract void Import();

		public abstract void Import(YgomSystem.Utility.DeviceInfo.PlatformType platformType);
	}
}
