namespace YuGiOhFight.Client.Presentation.UI
{
	public class FreeTextFilterCache : UnityEngine.MonoBehaviour
	{
		private static FreeTextFilterCache s_Instance;

		private System.Collections.Generic.List<FreeTextFilter> m_ReferencedFilteres;

		private System.Collections.Generic.List<string> m_CachedFreeTexts;

		private System.Collections.Generic.Dictionary<string, string> m_FilteredTextMap;

		private System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<FreeTextFilter>> m_FilterRequests;

		private readonly int k_CacheReleaseLine;

		private readonly int k_CacheReleaseCount;

		public System.Collections.Generic.IReadOnlyList<FreeTextFilter> referencedFilteres => null;

		public System.Collections.Generic.IReadOnlyList<string> cachedFreeTexts => null;

		public System.Collections.Generic.IReadOnlyDictionary<string, string> filteredTextMap => null;

		private static FreeTextFilterCache GetInstance()
		{
			return null;
		}

		private static void DestroyInstance()
		{
		}

		public static FreeTextFilterCache AssignReference(FreeTextFilter freeTextFilter)
		{
			return null;
		}

		public void RemoveReference(FreeTextFilter freeTextFilter)
		{
		}

		public void RequestFilter(FreeTextFilter owner, string freeText)
		{
		}

		private void ExecuteFilterAsync(string freeText)
		{
		}
	}
}
