using UnityEngine;


namespace YuGiOhFight.Client.Presentation.Timeline
{
	public class PlayableLoopFlag : MonoBehaviour
	{
		[SerializeField]
		private bool m_IsLoop;

		public static bool GetLoopFlag(UnityEngine.Playables.PlayableDirector director)
		{
			return false;
		}

		public static void SetLoopFlag(UnityEngine.Playables.PlayableDirector director, bool value)
		{
		}
	}
}
