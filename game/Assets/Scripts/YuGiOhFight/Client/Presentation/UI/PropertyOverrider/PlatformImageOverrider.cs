using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI.PropertyOverrider
{
	public class PlatformImageOverrider : PropertyOverriderBase<UnityEngine.UI.Image>
	{
		[SerializeField]
		private OverrideSpriteProperty m_SourceImage;

		[SerializeField]
		private OverrideMaterialProperty m_Material;

		public OverrideSpriteProperty sprite => null;

		public OverrideMaterialProperty material => null;

		public override void Import(UnityEngine.UI.Image target, YgomSystem.Utility.DeviceInfo.PlatformType platformType)
		{
		}

		public override void Export(UnityEngine.UI.Image target, YgomSystem.Utility.DeviceInfo.PlatformType platformType)
		{
		}

		public PlatformImageOverrider()
		{
			//((PropertyOverriderBase<>)(object)this)._002Ector();
		}
	}
}
