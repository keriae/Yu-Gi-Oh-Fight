namespace YuGiOhFight.Client.Presentation.UI
{
	public abstract class AbstractInputBlocker : UnityEngine.MonoBehaviour
	{
		protected bool blocking;

		protected abstract int blockPriority { get; }

		private void OnEnable()
		{
		}

		private void OnDisable()
		{
		}
	}
}
