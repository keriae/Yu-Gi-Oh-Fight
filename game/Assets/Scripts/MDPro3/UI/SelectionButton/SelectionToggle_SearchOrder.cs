using MDPro3.UI.ServantUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static MDPro3.UI.CardCollectionView;

namespace MDPro3.UI
{
    public class SelectionToggle_SearchOrder : SelectionToggle
    {
        [Header("SelectionToggle SearchOrder")]
        [SerializeField] private SortOrder sortOrder = SortOrder.ByType;

        protected override void Awake()
        {
            base.Awake();

            exclusiveToggle = true;
            canToggleOffSelf = false;
        }

        private void Start()
        {
            if (sortOrder == _SortOrder)
            {
                SetToggleOn();
                EventSystem.current.SetSelectedGameObject(gameObject);
            }
        }

        protected override void CallHoverOnEvent()
        {
            base.CallHoverOnEvent();
            ((Popup.PopupSearchOrder)Program.instance.ui_.currentPopupB).lastSelectedToggle = this;
        }

        protected override void CallSubmitEvent()
        {
            _SortOrder = sortOrder;
            var view = Program.instance.deckEditor.GetUI<DeckEditorUI>().CardCollectionView;
            view.SetSortIcon(GetIconSprite());
            view.SetSortText(GetSortText());
            if(view.area == Area.Collection)
                view.PrintSearchCards();
            else
                view.PrintBookmarkCards();
            Program.instance.ui_.currentPopupB.Hide();
        }

        protected string GetSortText()
        {
            return sortOrder switch
            {
                SortOrder.ByType => InterString.Get("种类"),
                SortOrder.ByTypeReverse => InterString.Get("种类"),
                SortOrder.ByLevelUp => InterString.Get("等级·阶级·连接"),
                SortOrder.ByLevelDown => InterString.Get("等级·阶级·连接"),
                SortOrder.ByAttackUp => InterString.Get("攻击力"),
                SortOrder.ByAttackDown => InterString.Get("攻击力"),
                SortOrder.ByDefenceUp => InterString.Get("守备力"),
                SortOrder.ByDefenceDown => InterString.Get("守备力"),
                SortOrder.ByRarityUp => InterString.Get("稀有度"),
                SortOrder.ByRarityDown => InterString.Get("稀有度"),
                SortOrder.ByGPUp => InterString.Get("Genesys分数"),
                SortOrder.ByGPDown => InterString.Get("Genesys分数"),
                _ => string.Empty
            };
        }
        protected override void OnClick()
        {
            if (!Selectable.IsInteractable())
            {
                AudioManager.PlaySE(SoundLabelClickInactive);
                return;
            }
            AudioManager.PlaySE(SoundLabelClick);
            SetToggleOn();
            CallSubmitEvent();
        }

        protected override void OnSubmit()
        {
            OnClick();
        }
    }
}
