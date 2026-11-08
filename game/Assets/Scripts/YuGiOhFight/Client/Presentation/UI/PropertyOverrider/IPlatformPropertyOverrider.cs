namespace YuGiOhFight.Client.Presentation.UI.PropertyOverrider
{
	public interface IPlatformPropertyOverrider
	{
		OverrideMode overrideMode { get; set; }

		void ApplyImmediate();

		void ApplyImmediate(YgomSystem.Utility.DeviceInfo.PlatformType platformType);

		void Import();

		void Import(YgomSystem.Utility.DeviceInfo.PlatformType platformType);

		void Export();

		void Export(YgomSystem.Utility.DeviceInfo.PlatformType platformType);
	}
}
