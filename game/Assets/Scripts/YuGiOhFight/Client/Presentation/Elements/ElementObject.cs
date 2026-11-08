using System.Runtime.CompilerServices;


namespace YuGiOhFight.Client.Presentation.Elements
{
	public class ElementObject : UnityEngine.MonoBehaviour
	{
		public string label;

		public UnityEngine.Events.UnityAction callbackVoid;

		public UnityEngine.Events.UnityAction<int> callbackInt;

		public UnityEngine.Events.UnityAction<float> callbackFloat;

		public UnityEngine.Events.UnityAction<UnityEngine.Vector2> callbackVector2;

		public UnityEngine.Events.UnityAction<string> callbackString;

		public UnityEngine.Events.UnityAction<bool> callbackBool;

		public ElementObjectManager parentManager
		{
			[CompilerGenerated]
			get
			{
				return null;
			}
			[CompilerGenerated]
			private set
			{
			}
		}

		private void OnValidate()
		{
		}

		public void Setup()
		{
		}

		public void ElementCallback()
		{
		}

		public void ElementCallback(float value)
		{
		}

		public void ElementCallback(int value)
		{
		}

		public void ElementCallback(UnityEngine.Vector2 value)
		{
		}

		public void ElementCallback(string value)
		{
		}

		public void ElementCallback(bool value)
		{
		}

		private void OnDestroy()
		{
		}
	}
}
