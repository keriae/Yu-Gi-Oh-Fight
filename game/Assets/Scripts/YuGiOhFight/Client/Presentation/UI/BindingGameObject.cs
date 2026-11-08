using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class BindingGameObject : MonoBehaviour
	{
		[SerializeField]
		protected string prefabPath;

		[HideInInspector]
		public bool IsDone;

		[SerializeField]
		private bool immediate;

		[SerializeField]
		public bool StartOnAwake;

		protected bool IsSetParent;

		protected string basePath;

		protected System.Collections.Generic.Dictionary<string, object> pathArgs;

		private System.Action<GameObject> onCreated;

		public string PrefabPath
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public void Rebind(bool force = false)
		{
		}

		private void Awake()
		{
		}

		protected void SetOnCreated(System.Action<GameObject> act)
		{
		}

		public void SetImmediate(bool immediate)
		{
		}

		public void LoadGameObject()
		{
		}

		protected void SplitPath()
		{
		}

		protected virtual void PostModifiyByArgs(GameObject go)
		{
		}
	}
}
