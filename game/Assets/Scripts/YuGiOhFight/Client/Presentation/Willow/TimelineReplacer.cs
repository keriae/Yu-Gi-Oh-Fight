using System;


namespace YuGiOhFight.Client.Presentation.Willow
{
	[Serializable]
	public class TimelineReplacer
	{
		[Serializable]
		public class BindTrackInfo
		{
			public int m_index;

			public string m_type;

			public string m_name;

			public BindTrackInfo(int idx, string type, string name)
			{
			}
		}

		public UnityEngine.Playables.PlayableDirector m_currentDirector;

		public BindTrackInfo[] m_bindTrackInfo;

		public string[] m_bindTransformTweenTrackName;

		public CustomTimelineController.DataReferenceTarget[] m_listReferenceTarget;

		public CustomTimelineController.DataReferenceTarget[] m_listReferenceTransformTweenClipStart;

		public CustomTimelineController.DataReferenceTarget[] m_listReferenceTransformTweenClipEnd;

		private UnityEngine.Object[] m_asset;

		private System.Collections.Generic.IEnumerable<UnityEngine.Playables.PlayableBinding> m_bindingAll;

		private string[] monsterHipsNameList => null;

		public bool IsValid()
		{
			return false;
		}

		private void SetBind<T>(string trackName, T obj) where T : UnityEngine.Object
		{
		}

		private void SetBind<T>(int id, T obj) where T : UnityEngine.Object
		{
		}

		public void Set(UnityEngine.GameObject bindTarget = null, UnityEngine.Transform moveTarget = null, UnityEngine.Transform start = null, UnityEngine.Transform end = null)
		{
		}
	}
}
