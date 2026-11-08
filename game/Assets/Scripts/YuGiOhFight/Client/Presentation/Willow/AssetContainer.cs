using System.Linq;
using UnityEngine;


namespace YuGiOhFight.Client.Presentation.Willow
{
#if UNITY_EDITOR
	[CreateAssetMenu]
#endif
	public class AssetContainer : ScriptableObject
	{
		[SerializeField]
		protected Object[] m_container;

		protected readonly System.Collections.Generic.Dictionary<string, Object> m_table = new System.Collections.Generic.Dictionary<string, Object>();

		protected virtual void OnEnable()
		{
			RebuildTable();
		}

		protected virtual void RebuildTable()
		{
			m_table.Clear();
			if (m_container == null)
				return;

			foreach (Object asset in m_container)
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
			if(m_table.TryGetValue(name, out Object asset))
				return asset as T;
			else
				return null;
		}

		public bool TryGet<T>(string name, out T asset) where T : Object
		{
			if (m_table.TryGetValue(name, out Object obj) && obj is T typedAsset)
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
