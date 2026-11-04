using UnityEngine;

namespace YgomSystem.UI
{
	public class TweenSpeedWriter : MonoBehaviour
	{
		[SerializeField]
		private string m_Label;

		[SerializeField]
		private Tween[] m_TargetTweens;

		[SerializeField]
		private float[] m_OriginalDurations;

		[SerializeField]
		private float m_Speed;

		private bool m_Ready;

		private void Initialize(string label = null)
		{
            if (m_Ready)
                return;

            if (label == null)
                label = m_Label;

            var found = GetComponents<Tween>();
            var list = new System.Collections.Generic.List<Tween>();
            for (int i = 0; i < found.Length; i++)
            {
                var t = found[i];
                if (t != null && (string.IsNullOrEmpty(label) || t.label == label))
                    list.Add(t);
            }
            m_TargetTweens = list.ToArray();
            m_OriginalDurations = new float[m_TargetTweens.Length];
            for (int i = 0; i < m_TargetTweens.Length; i++)
                m_OriginalDurations[i] = m_TargetTweens[i].duration;

            m_Ready = true;
        }

		private void WriteSpeed(float speed = 1f)
		{
            Initialize(m_Label);
            m_Speed = speed;
            if (m_TargetTweens == null || m_OriginalDurations == null)
                return;

            for (int i = 0; i < m_TargetTweens.Length; i++)
            {
                var t = m_TargetTweens[i];
                if (t == null)
                    continue;
                if (i < m_OriginalDurations.Length)
                    t.duration = speed > 0f ? m_OriginalDurations[i] / speed : m_OriginalDurations[i];
            }
        }

		private static void InnerTargetWriteSpeed(GameObject target, float speed = 1f, string label = null)
		{
            if (target == null)
                return;
            var writer = target.GetComponent<TweenSpeedWriter>();
            if (writer != null)
                writer.WriteSpeed(speed);

            var tweens = Tween.GetTweenTarget(target, label, false);
            for (int i = 0; i < tweens.Count; i++)
            {
                var t = tweens[i];
                if (t != null)
                {
                    float original = t.duration;
                    t.duration = speed > 0f ? original / speed : original;
                }
            }
        }

		public static void TargetWriteSpeed(GameObject target, float speed = 1f, string label = null, bool includeChildren = false)
		{
            if (target == null)
                return;
            if (includeChildren)
            {
                var writers = target.GetComponentsInChildren<TweenSpeedWriter>(true);
                for (int i = 0; i < writers.Length; i++)
                {
                    if (writers[i] != null)
                        writers[i].WriteSpeed(speed);
                }
            }
            InnerTargetWriteSpeed(target, speed, label);
        }
	}
}
