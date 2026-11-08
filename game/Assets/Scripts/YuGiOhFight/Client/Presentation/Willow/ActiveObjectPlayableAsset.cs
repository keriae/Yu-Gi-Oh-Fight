using System;


namespace YuGiOhFight.Client.Presentation.Willow
{
	[Serializable]
	public class ActiveObjectPlayableAsset : UnityEngine.Playables.PlayableAsset, UnityEngine.Timeline.ITimelineClipAsset
	{
		public string m_name;

		public UnityEngine.ExposedReference<UnityEngine.GameObject> m_parentObject;

		public UnityEngine.GameObject m_prefab;

		public bool m_isFullLifespan;

		public bool m_onCreateRelayObject;

		public UnityEngine.ExposedReference<CustomTimelineObject> m_relayObject;

		public bool m_isSyncPosition;

		public bool m_isSyncRotation;

		public bool m_isSyncScale;

		public bool m_isLookAtCamera;

		public bool m_isIgnoreInitRotation;

		public bool m_isIgnoreInitScale;

		public bool m_isRelayChild;

		public UnityEngine.Timeline.ClipCaps clipCaps => default(UnityEngine.Timeline.ClipCaps);

		public override UnityEngine.Playables.Playable CreatePlayable(UnityEngine.Playables.PlayableGraph graph, UnityEngine.GameObject go)
		{
            UnityEngine.Playables.ScriptPlayable<ActiveObjectPlayableBehaviour> playable = UnityEngine.Playables.ScriptPlayable<ActiveObjectPlayableBehaviour>.Create(graph);
			ActiveObjectPlayableBehaviour behaviour = playable.GetBehaviour();
			behaviour.m_parentObject = m_parentObject.Resolve(graph.GetResolver());
            behaviour.m_relayObject = m_relayObject.Resolve(graph.GetResolver());
            behaviour.m_prefab = m_prefab;
			behaviour.m_isFullLifespan = m_isFullLifespan;
			behaviour.m_isLookAtCamera = m_isLookAtCamera;


            behaviour.controller = go.transform.parent.GetComponent<CustomTimelineController>();
			behaviour.m_name = m_name;
			return playable;
		}
	}
}
