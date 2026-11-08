using System;

using UnityEngine;


namespace YuGiOhFight.Client.Presentation.Tweens
{
	public class TweenRunner : MonoBehaviour
	{
		[Serializable]
		public class TweenGroup
		{
			[SerializeField]
			private string m_Label;

			[SerializeField]
			private System.Collections.Generic.List<UI.Tween> m_Tweens;

			public string label
			{
				get { return m_Label; }
				set { m_Label = value; }
			}

			public System.Collections.Generic.List<UI.Tween> tweens
			{
				get { return m_Tweens; }
				set { m_Tweens = value; }
			}
		}

		[SerializeField]
		private System.Collections.Generic.List<TweenGroup> m_TweenGroups;

		[SerializeField]
		private string m_PlayLabel;

		public void TweenCollect(bool includeChildren = false)
		{
            m_TweenGroups = new System.Collections.Generic.List<TweenGroup>();
            UI.Tween[] tweens = includeChildren
                ? GetComponentsInChildren<UI.Tween>(true)
                : GetComponents<UI.Tween>();

            System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<UI.Tween>> dict = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<UI.Tween>>();
            for (int i = 0; i < tweens.Length; i++)
            {
                UI.Tween t = tweens[i];
                if (t == null)
                    continue;
                if (!dict.TryGetValue(t.label, out System.Collections.Generic.List<UI.Tween> list))
                {
                    list = new System.Collections.Generic.List<UI.Tween>();
                    dict[t.label] = list;
                }
                list.Add(t);
            }

            foreach (System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.List<UI.Tween>> kv in dict)
            {
                m_TweenGroups.Add(new TweenGroup { label = kv.Key, tweens = kv.Value });
            }
        }

		public System.Collections.Generic.List<UI.Tween> TryGetTweens(string label)
		{
            if (m_TweenGroups == null)
                return new System.Collections.Generic.List<UI.Tween>();

            for (int i = 0; i < m_TweenGroups.Count; i++)
            {
                if (m_TweenGroups[i].label == label)
                    return m_TweenGroups[i].tweens ?? new System.Collections.Generic.List<UI.Tween>();
            }
            return new System.Collections.Generic.List<UI.Tween>();
        }

		public void Evaluate(string playLabel)
		{
            System.Collections.Generic.List<UI.Tween> tweens = TryGetTweens(playLabel);
            for (int i = 0; i < tweens.Count; i++)
            {
                UI.Tween t = tweens[i];
                if (t != null)
                    t.GotoAndPlay(0f);
            }
        }

		public void PlayOverride(string playLabel)
		{
            m_PlayLabel = playLabel;
            System.Collections.Generic.List<UI.Tween> tweens = TryGetTweens(playLabel);
            for (int i = 0; i < tweens.Count; i++)
            {
                UI.Tween t = tweens[i];
                if (t != null)
                    t.Play();
            }
        }
	}
}
