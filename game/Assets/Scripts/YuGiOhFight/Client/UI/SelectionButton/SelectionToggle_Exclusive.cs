namespace YuGiOhFight.Client.UI
{
    public class SelectionToggle_Exclusive : SelectionToggle
    {
        protected override void Awake()
        {
            base.Awake();
            exclusiveToggle = true;
            canToggleOffSelf = false;
        }
    }

}
