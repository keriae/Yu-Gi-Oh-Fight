using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI.PropertyOverrider
{
	public class PlatformActiveOverrider : PropertyOverriderBase<Transform>
	{
		[SerializeField]
		private OverrideBoolProperty m_Active;

		public override void Import(Transform target, YgomSystem.Utility.DeviceInfo.PlatformType platformType)
		{
		}

		public override void Export(Transform target, YgomSystem.Utility.DeviceInfo.PlatformType platformType)
		{
		}

		public PlatformActiveOverrider()
		{
		}

        private void Start()
        {
			if (m_Active.m_DefaultValue && Runtime.Program.root != Runtime.Program.PATH_ROOT_WINDOWS64)
				Destroy(gameObject);
            if (m_Active.m_MobileValue && Runtime.Program.root == Runtime.Program.PATH_ROOT_WINDOWS64)
                Destroy(gameObject);
        }
    }
}
