using UnityEngine;


namespace YuGiOhFight.Client.UI
{
    public class SelectionToggle_CardCollectionTab : SelectionToggle
    {
        [Header("SelectionToggle CardCollectionTab")]
        [SerializeField] private CardCollectionView.Area area;
        internal CardCollectionView.Area ClientArea => area;
        private CardCollectionView collection;
        private CardCollectionView Collection => collection != null ? collection
            : collection = FindCollection();

        private CardCollectionView FindCollection()
        {
            for (var current = transform; current != null; current = current.parent)
            {
                CardCollectionView owner = current.GetComponent<CardCollectionView>();
                if (owner != null) return owner;
            }
            return null;
        }

        protected override void Awake()
        {
            base.Awake();
            exclusiveToggle = true;
            canToggleOffSelf = false;
            toggleWhenSelected = true;
            // Child Start can run before Addressables publishes the new editor UI.
            // Keep the owning pool from this prefab, including when it is mounted.
            collection = FindCollection();
        }

        private void Start()
        {
            CardCollectionView owner = Collection;
            if (owner != null && owner.defaultArea == area)
                SetToggleOn();
        }

        protected override void CallToggleOnEvent()
        {
            CardCollectionView owner = Collection;
            if (owner == null) return;
            base.CallToggleOnEvent();
            owner.ShowArea(area);
            foreach (SelectionToggle_CardCollectionTab toggle in transform.parent.GetComponentsInChildren<SelectionToggle_CardCollectionTab>(true))
                toggle.SetShortcut(area);
        }

        protected void SetShortcut(CardCollectionView.Area area)
        {
            var show = (int)this.area == ((int)area + 1) % 3;
            Manager.GetElement<ShortcutIcon>("ShortcutIcon").Show = show;
        }

        public void OnRightSelection()
        {
            Runtime.AudioManager.PlaySE(SoundLabelClick);
            var rightSelectable = Selectable.navigation.selectOnRight;
            if (isOn)
            {
                if (rightSelectable != null)
                    rightSelectable.GetComponent<SelectionToggle_CardCollectionTab>().SetToggleOn();
            }
            else
            {
                if (rightSelectable != null)
                    rightSelectable.GetComponent<SelectionToggle_CardCollectionTab>().OnRightSelection();
            }
        }
    }
}
