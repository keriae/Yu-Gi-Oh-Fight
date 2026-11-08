using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI.PropertyOverrider
{
	public class PlatformSpriteRendererOverrider : PropertyOverriderBase<SpriteRenderer>
	{
		[SerializeField]
		private OverrideSpriteProperty m_Sprite;

		public OverrideSpriteProperty sprite => null;

		public override void Import(SpriteRenderer target, YgomSystem.Utility.DeviceInfo.PlatformType platformType)
		{
		}

		public override void Export(SpriteRenderer target, YgomSystem.Utility.DeviceInfo.PlatformType platformType)
		{
		}

		public PlatformSpriteRendererOverrider()
		{
			//((PropertyOverriderBase<>)(object)this)._002Ector();
		}
	}
}
