using System;
using System.Collections.Generic;
using UnityEngine;
using YgomSystem.UI;

namespace YgomSystem.Utility
{
	public class TweenRunner : MonoBehaviour
	{
		[Serializable]
		public class TweenGroup
		{
			[SerializeField]
			private string m_Label;

			[SerializeField]
			private List<Tween> m_Tweens;

			public string label
			{
				get { return m_Label; }
				set { m_Label = value; }
			}

			public List<Tween> tweens
			{
				get { return m_Tweens; }
				set { m_Tweens = value; }
			}
		}

		[SerializeField]
		private List<TweenGroup> m_TweenGroups;

		[SerializeField]
		private string m_PlayLabel;

		public void TweenCollect(bool includeChildren = false)
		{
            m_TweenGroups = new List<TweenGroup>();
            Tween[] tweens = includeChildren
                ? GetComponentsInChildren<Tween>(true)
                : GetComponents<Tween>();

            var dict = new Dictionary<string, List<Tween>>();
            for (int i = 0; i < tweens.Length; i++)
            {
                var t = tweens[i];
                if (t == null)
                    continue;
                if (!dict.TryGetValue(t.label, out var list))
                {
                    list = new List<Tween>();
                    dict[t.label] = list;
                }
                list.Add(t);
            }

            foreach (var kv in dict)
            {
                m_TweenGroups.Add(new TweenGroup { label = kv.Key, tweens = kv.Value });
            }
        }

		public List<Tween> TryGetTweens(string label)
		{
            if (m_TweenGroups == null)
                return new List<Tween>();

            for (int i = 0; i < m_TweenGroups.Count; i++)
            {
                if (m_TweenGroups[i].label == label)
                    return m_TweenGroups[i].tweens ?? new List<Tween>();
            }
            return new List<Tween>();
        }

		public void Evaluate(string playLabel)
		{
            var tweens = TryGetTweens(playLabel);
            for (int i = 0; i < tweens.Count; i++)
            {
                var t = tweens[i];
                if (t != null)
                    t.GotoAndPlay(0f);
            }
        }

		public void PlayOverride(string playLabel)
		{
            m_PlayLabel = playLabel;
            var tweens = TryGetTweens(playLabel);
            for (int i = 0; i < tweens.Count; i++)
            {
                var t = tweens[i];
                if (t != null)
                    t.Play();
            }
        }
	}
}
