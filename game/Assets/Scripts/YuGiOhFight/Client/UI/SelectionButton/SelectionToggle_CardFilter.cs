namespace YuGiOhFight.Client.UI
{
    public class SelectionToggle_CardFilter : SelectionToggle
    {
        public static SelectionToggle_CardFilter Instance;

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            SetClickEvent(() =>
            {
                Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().CardCollectionView.ShowFilters();
            });
        }

        protected override void OnClick()
        {
        }
    }
}
