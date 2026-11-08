using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI.PropertyOverrider
{
	public class PlatformInfinityScrollViewOverrider : PropertyOverriderBase<InfinityScroll.InfinityScrollView>
	{
		[SerializeField]
		private OverrideStringProperty m_ELabelTemplate;

		public override void Import(InfinityScroll.InfinityScrollView target, YgomSystem.Utility.DeviceInfo.PlatformType platformType)
		{
		}

		public override void Export(InfinityScroll.InfinityScrollView target, YgomSystem.Utility.DeviceInfo.PlatformType platformType)
		{
		}

		public PlatformInfinityScrollViewOverrider()
		{
			//((PropertyOverriderBase<>)(object)this)._002Ector();
		}
	}
}
