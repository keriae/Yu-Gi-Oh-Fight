using System;

using UnityEngine;


namespace YuGiOhFight.Client.Presentation.Willow.InGameField
{
	[Serializable]
#if UNITY_EDITOR
	[CreateAssetMenu]
#endif
	public class ParameterContainer : ParameterAsset
	{
		[SerializeField]
		private System.Collections.Generic.List<ParameterAsset> m_childParameterList;

		[NonSerialized]
		[HideInInspector]
		private System.Collections.Generic.List<ParameterAsset> m_cacheChildParameterList;

		[NonSerialized]
		[HideInInspector]
		private System.Collections.Generic.Dictionary<string, ParameterAsset> m_cachedChildParameterMap;

		private void __internalAwake()
		{
		}

		public System.Collections.Generic.List<ParameterAsset> GetRootParameters()
		{
			return m_childParameterList;
		}

		public System.Collections.Generic.Dictionary<string, ParameterAsset> GetRootParametersMap()
		{
			return m_cachedChildParameterMap;
        }

		private void UpdateRootParameterCache()
		{
		}

		private void UpdateRootParameterCacheMap()
		{
		}

		public void AddMonsterModelParameter()
		{
		}

		public void AddMonsterLocationParameter()
		{
		}

		public void AddMonsterLocationOffsetParameter()
		{
		}

		public void AddMonsterRootLocationParameter()
		{
		}

		public void AddMonsterSkillEffectParentParameter()
		{
		}

		private void CreateDummyAsset()
		{
		}

		private ParameterAsset CreateChildParameter(Type type, string assetName = "", ParameterContainer parent = null, Type supportType = null)
		{
			return null;
		}

		private string GetVariableAssetName(string assetName, Type type)
		{
			return null;
		}

		private ParameterAsset CreateParameterAssetInstance(string parameterAssetName, Type type, ParameterContainer parent = null, Type supportType = null)
		{
			return null;
		}

		private void SetParentContainer(ParameterAsset parameterAsset, ParameterContainer parent)
		{
		}

		private void AddParameterAsSubAsset(ParameterAsset parameterAsset)
		{
		}
	}
}
