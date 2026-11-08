namespace YuGiOhFight.Client.Presentation.Willow
{
	public class ActiveObjectPlayableBehaviour : UnityEngine.Playables.PlayableBehaviour
	{
		public string m_name;

		public UnityEngine.Playables.Playable m_root;

		public UnityEngine.GameObject m_owner;

		public UnityEngine.GameObject m_parentObject;

		public CustomTimelineObject m_relayObject;

		public UnityEngine.GameObject m_prefab;

		public bool m_isFullLifespan;

		public CustomTimelineController controller;

        private CustomTimelineController m_controller;

		private UnityEngine.GameObject m_object;

		private System.Collections.Generic.List<UnityEngine.ParticleSystem> m_listParticle;

		private System.Collections.Generic.List<Ara.AraTrail> m_listTrail;

		private double m_prevTime;

		private double m_duration;

		private bool m_isPlay;

		private bool m_isKeepObject;

		public const string kPrefixTempPrefab = "_____tmp_playing_";

		public bool m_isLookAtCamera;

		UnityEngine.Transform needRotate;
        UnityEngine.Transform m_parent;

        private bool isFullLifespan => false;

		public override void OnGraphStart(UnityEngine.Playables.Playable playable)
		{
        }

        public override void OnGraphStop(UnityEngine.Playables.Playable playable)
		{
        }

        public override void OnBehaviourPlay(UnityEngine.Playables.Playable playable, UnityEngine.Playables.FrameData info)
		{
            if (m_prefab == null)
                return;
            if(m_object == null)
            {
                m_object = UnityEngine.Object.Instantiate(m_prefab);
                if (Runtime.Program.instance.currentServant == Runtime.Program.instance.mate)
                    Utility.Tools.ChangeLayer(m_object, "DuelOverlay2D");

                if (needRotate != null)
                    m_object.transform.SetParent(needRotate, false);
                else if (m_parent != null)
                    m_object.transform.SetParent(m_parent, false);
                else
                {
                    if (m_relayObject != null)
                    {
                        if (m_relayObject.transform.GetChild(0).childCount > 0)
                        {
                            //needRotate = m_relayObject.transform.GetChild(0).GetChild(0);
                            m_object.transform.SetParent(needRotate, false);
                        }
                        else
                            m_object.transform.SetParent(m_relayObject.transform.GetChild(0), false);
                    }
                    else
                    {
                        foreach (CustomTimelineObject t in controller.transform.GetComponentsInChildren<CustomTimelineObject>(true))
                            if (t.name.ToLower().Contains(m_prefab.name.ToLower()) && t.name.ToLower().Contains("relay_"))
                            {
                                m_relayObject = t;
                                break;
                            }
                    }
                    if (m_relayObject != null)
                    {
                        if (m_relayObject.transform.GetChild(0).childCount > 0)
                            m_object.transform.SetParent(m_relayObject.transform.GetChild(0).GetChild(0), false);
                        else
                            m_object.transform.SetParent(m_relayObject.transform.GetChild(0), false);
                    }
                    else
                    {
                        if (m_parentObject != null)
                        {
                            m_object.transform.SetParent(m_parentObject.transform, false);
                        }
                        else
                        {
                            UnityEngine.Transform parent = null;
                            foreach (UnityEngine.Transform t in controller.transform.parent.GetComponentsInChildren<UnityEngine.Transform>(true))
                            {
                                if (t.name.ToLower() == m_name.ToLower())
                                    parent = t;
                            }
                            if (parent != null)
                                m_object.transform.SetParent(parent, false);
                            else
                                m_object.transform.SetParent(controller.transform.parent, false);
                        }
                    }
                    m_parent = m_object.transform.parent;
                    if (m_prefab.name.Contains("04766_01J_dattack_01"))
                        needRotate = m_parent;
                }
            }
            else
                m_object.SetActive(true);
        }

        public override void OnBehaviourPause(UnityEngine.Playables.Playable playable, UnityEngine.Playables.FrameData info)
		{
            //if (m_object != null && !m_isFullLifespan)
            //    m_object.SetActive(false);
            if (m_object != null && m_isFullLifespan)
                m_object.SetActive(false);
            else
                UnityEngine.Object.Destroy(m_object);
        }

        public override void PrepareFrame(UnityEngine.Playables.Playable playable, UnityEngine.Playables.FrameData info)
		{
            //if (m_parentObject != null && m_isLookAtCamera)
            //             m_parentObject.transform.LookAt(Program.instance.camera_.cameraMain.transform);
            if (needRotate != null)
            {
                if(Runtime.Program.instance.currentServant == Runtime.Program.instance.ocgcore)
                    needRotate.LookAt(Runtime.Program.instance.camera_.cameraMain.transform);
                else
                    needRotate.LookAt(Runtime.Program.instance.camera_.cameraDuelOverlay2D.transform);
            }
        }

        private static void GetComponentRoots<T>(UnityEngine.Transform t, System.Collections.Generic.ICollection<T> roots) where T : UnityEngine.Component
		{
		}
	}
}
