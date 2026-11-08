using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI.PropertyOverrider
{
	public class PlatformTweenImageOverrider : PropertyOverriderBase<TweenImage>
	{
		[SerializeField]
		private TweenImage m_TargetTween;

		[SerializeField]
		private OverrideSpriteArrayProperty m_Frames;

		protected override TweenImage GetTargetComponent()
		{
			return null;
		}

		public override void Import(TweenImage target, YgomSystem.Utility.DeviceInfo.PlatformType platformType)
		{
		}

		public override void Export(TweenImage target, YgomSystem.Utility.DeviceInfo.PlatformType platformType)
		{
		}

		public PlatformTweenImageOverrider()
		{
			//((PropertyOverriderBase<>)(object)this)._002Ector();
		}
	}
}
