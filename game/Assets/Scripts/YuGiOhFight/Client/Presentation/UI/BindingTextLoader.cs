using System.Runtime.CompilerServices;
using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class BindingTextLoader : MonoBehaviour, YgomGame.Menu.Common.IAsyncProgressContent, ILoadingIconHandler
	{
		[SerializeField]
		private Binding[] m_BindingTexts;

		private System.Collections.Generic.List<string> m_LoadTextGroups;

		private System.Collections.IEnumerator m_Routine;

		public bool visible => false;

		public event System.Action onReloadEvent
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public bool IsDone()
		{
			return false;
		}

		private void Awake()
		{
		}

		private void Update()
		{
		}

		public void ProgressUpdate()
		{
		}

		private System.Collections.IEnumerator yRoutine()
		{
			return null;
		}

		private string GetGroupId(Binding target)
		{
			return null;
		}

		private string GetGroupId(BindingTextMeshProUGUI target)
		{
			return null;
		}

		private string GetGroupId(BindingTextMeshPro target)
		{
			return null;
		}

		private string GetGruopId(Binding binding, string textId)
		{
			return null;
		}

		private void OnDestroy()
		{
		}
	}
}
