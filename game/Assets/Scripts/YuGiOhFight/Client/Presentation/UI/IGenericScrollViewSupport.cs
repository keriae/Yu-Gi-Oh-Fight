namespace YuGiOhFight.Client.Presentation.UI
{
	public interface IGenericScrollViewSupport
	{
		void OnItemSetData(UnityEngine.GameObject gob, int dataindex);

		void OnItemExit(UnityEngine.GameObject gob, int dataindex);

		void OnItemInitialize(UnityEngine.GameObject gob);

		void OnGsvStanby();
	}
}
