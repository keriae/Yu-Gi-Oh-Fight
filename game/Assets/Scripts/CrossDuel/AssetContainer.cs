using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Willow
{
	[CreateAssetMenu]
	public class AssetContainer : ScriptableObject
	{
		[SerializeField]
		protected Object[] m_container;

		protected readonly Dictionary<string, Object> m_table = new Dictionary<string, Object>();

		protected virtual void OnEnable()
		{
			RebuildTable();
		}

		protected virtual void RebuildTable()
		{
			m_table.Clear();
			if (m_container == null)
				return;

			foreach (var asset in m_container)
			{
				if (asset == null)
					continue;

				m_table[asset.name] = asset;
			}
		}

		public int ObjectCount()
		{
			return m_container?.Length ?? 0;
		}

		public string[] AllAssetNames()
		{
			return m_table.Keys.ToArray();
		}

		public T Get<T>(string name) where T : Object
		{
			if(m_table.TryGetValue(name, out var asset))
				return asset as T;
			else
				return null;
		}

		public bool TryGet<T>(string name, out T asset) where T : Object
		{
			if (m_table.TryGetValue(name, out var obj) && obj is T typedAsset)
			{
				asset = typedAsset;
                return true;
            }
			else
			{
				asset = null;
                return false;
            }
        }
    }
}
