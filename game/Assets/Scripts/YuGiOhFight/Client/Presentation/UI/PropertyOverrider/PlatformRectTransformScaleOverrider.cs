using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI.PropertyOverrider
{
	public class PlatformRectTransformScaleOverrider : PropertyOverriderBase<RectTransform>
	{
		[SerializeField]
		private OverrideVector3Property m_Scale;

		public override void Import(RectTransform target, YgomSystem.Utility.DeviceInfo.PlatformType platformType)
		{
		}

		public override void Export(RectTransform target, YgomSystem.Utility.DeviceInfo.PlatformType platformType)
		{
		}

		public PlatformRectTransformScaleOverrider()
		{
			//((PropertyOverriderBase<>)(object)this)._002Ector();
		}
	}
}
