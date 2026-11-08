namespace YuGiOhFight.Client.Presentation.UI
{
	public interface ILoadingIconHandler
	{
		bool visible { get; }

		event System.Action onReloadEvent;

		bool IsDone();
	}
}
