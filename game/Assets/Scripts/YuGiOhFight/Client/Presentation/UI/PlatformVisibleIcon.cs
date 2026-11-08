namespace YuGiOhFight.Client.Presentation.UI
{
	public class PlatformVisibleIcon : PlatformVisibleIconBase
	{
		[EnumFlags]
		public YgomSystem.Utility.DeviceInfo.Platform platformFlags;

		[EnumFlags]
		public SelectorManager.InputDevice inputDeviceFlags;

		protected override bool IsDispPlatform()
		{
			return false;
		}
	}
}
