using System;


namespace YuGiOhFight.Client.Presentation.UI.PropertyOverrider
{
	[Serializable]
	public class OverridePropertyBase<T>
	{
		public T m_DefaultValue;

		public T m_MobileValue;

		public T GetPlatformValue()
		{
			return default(T);
		}

		public T GetPlatformValue(YgomSystem.Utility.DeviceInfo.PlatformType platformType)
		{
			return default(T);
		}

		public void SetPlatformValue(YgomSystem.Utility.DeviceInfo.PlatformType platformType, T value)
		{
		}
	}
}
