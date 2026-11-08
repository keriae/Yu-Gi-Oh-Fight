using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI.PropertyOverrider
{
	public class PlatformTextOverrider : PropertyOverriderBase<MDText>
	{
		[SerializeField]
		private OverrideIntProperty m_FontSize;

		[SerializeField]
		private OverrideBoolProperty m_BestFit;

		[SerializeField]
		private OverrideIntProperty m_MinSize;

		[SerializeField]
		private OverrideIntProperty m_MaxSize;

		public OverrideIntProperty fontSize => null;

		public OverrideBoolProperty bestFit => null;

		public OverrideIntProperty minSize => null;

		public OverrideIntProperty maxSize => null;

		public override void Import(MDText target, YgomSystem.Utility.DeviceInfo.PlatformType platformType)
		{
		}

		public override void Export(MDText target, YgomSystem.Utility.DeviceInfo.PlatformType platformType)
		{
		}

		public PlatformTextOverrider()
		{
			//((PropertyOverriderBase<>)(object)this)._002Ector();
		}
	}
}
