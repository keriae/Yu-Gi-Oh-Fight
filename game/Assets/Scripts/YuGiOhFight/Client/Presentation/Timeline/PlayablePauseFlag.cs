using UnityEngine;


namespace YuGiOhFight.Client.Presentation.Timeline
{
	public class PlayablePauseFlag : MonoBehaviour
	{
		[SerializeField]
		private bool m_IsPause;

		public static bool GetPauseFlag(UnityEngine.Playables.PlayableDirector director)
		{
			return false;
		}

		public static void SetPauseFlag(UnityEngine.Playables.PlayableDirector director, bool value)
		{
		}
	}
}
