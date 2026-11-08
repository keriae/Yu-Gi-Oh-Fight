namespace YuGiOhFight.Client.Presentation.Fields
{
	public class BgAvatarChangeEffect : UnityEngine.MonoBehaviour
	{
		private System.Action delayCallback;

		private bool delayCheck;

		private float playtime;

		private float sePan;

		private Elements.ElementObjectManager manager;

		private UnityEngine.ParticleSystem toMainObj;

		private UnityEngine.ParticleSystem toSubObj;

		public float delay;

		public string toMainLabel;

		public string toSubLabel;

		public string toMainSELabel;

		public string toSubSELabel;

		private void Awake()
		{
		}

		private void Update()
		{
		}

		public void PlayEffect(bool toMain, System.Action callback = null)
		{
		}

		public void TraceMainCameraSetting(UnityEngine.Camera target)
		{
		}

		public void SetSePan(float pan)
		{
		}
	}
}
