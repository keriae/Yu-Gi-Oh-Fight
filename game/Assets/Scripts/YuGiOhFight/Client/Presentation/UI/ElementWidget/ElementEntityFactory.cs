using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI.ElementWidget
{
	public class ElementEntityFactory : MonoBehaviour
	{
		[SerializeField]
		private string m_ELabelTemplate;

		[SerializeField]
		private string[] m_ELabelAdditionalTemplates;

		[SerializeField]
		private Transform m_WidgetRoot;

		[SerializeField]
		private bool m_UseTemplateRoot;

		private System.Collections.Generic.List<GameObject> m_Templates;

		private System.Collections.Generic.Dictionary<GameObject, int> m_EntityToDataIndexTable;

		private System.Collections.Generic.Dictionary<int, GameObject> m_DataIndexToEntityTable;

		private System.Collections.Generic.List<System.Collections.Generic.Stack<GameObject>> m_FreeEntityStack;

		private System.Collections.Generic.List<GameObject> m_ActiveEntityList;

		private int m_DataCount;

		private System.Collections.Generic.List<int> m_TemplateIdxList;

		public System.Action<GameObject, int> onCreatedEntityCallback;

		public System.Action<GameObject> onActivateEntityCallback;

		public System.Action<GameObject, int> onUpdateEntityCallback;

		public System.Action<GameObject> onDeactivateEntityCallback;

		public Transform widgetRoot => null;

		public int dataCount => 0;

		public GameObject GetEntityByDataIndex(int dataindex)
		{
			return null;
		}

		public int GetDataIndexByEntity(GameObject entity)
		{
			return 0;
		}

		public void Initialize()
		{
		}

		public void UpdateDataCount(int dataCount, System.Collections.Generic.List<int> templateIdxList = null)
		{
		}

		public void UpdateData()
		{
		}

		private GameObject CreateItem(int templateIdx)
		{
			return null;
		}

		private GameObject AddItem(int templateIdx, int dataIndex)
		{
			return null;
		}

		private void RemoveItem(int dataIndex)
		{
		}
	}
}
