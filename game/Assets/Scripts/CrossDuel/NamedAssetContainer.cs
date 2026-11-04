using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Willow
{
	[CreateAssetMenu]
	public class NamedAssetContainer : AssetContainer
	{
		[SerializeField]
		private string[] m_keys;

		protected override void OnEnable()
		{
			RebuildTable();
        }

		protected override void RebuildTable()
		{
			m_table.Clear();
			if (m_keys == null || m_container == null)
				return;

			var count = Math.Min(m_keys.Length, m_container.Length);
			for (int i = 0; i < count; i++)
			{
				if (string.IsNullOrEmpty(m_keys[i]))
					continue;

				m_table[m_keys[i]] = m_container[i];
			}
		}

		public string[] AllNamedAssetNames()
		{
			return m_keys ?? Array.Empty<string>();
		}

		public void Set<T>(string name, T value) where T : Object
		{
			if (string.IsNullOrEmpty(name))
				return;

			m_keys ??= Array.Empty<string>();
			EnsureContainerLength(m_keys.Length);

			var updated = false;
			for (int i = 0; i < m_keys.Length; i++)
			{
				if (m_keys[i] != name)
					continue;

				m_container[i] = value;
				updated = true;
			}

			if (!updated)
			{
				var index = m_keys.Length;
				Array.Resize(ref m_keys, index + 1);
				m_keys[index] = name;
				EnsureContainerLength(index + 1);
				m_container[index] = value;
			}

			m_table[name] = value;
		}

		private void EnsureContainerLength(int length)
		{
			if (m_container == null)
			{
				m_container = new Object[length];
				return;
			}

			if (m_container.Length < length)
				Array.Resize(ref m_container, length);
		}
	}
}
