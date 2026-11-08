using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static YuGiOhFight.Client.UI.CardCollectionView;


namespace YuGiOhFight.Client.UI
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
            ((Popup.PopupSearchOrder)Runtime.Program.instance.ui_.currentPopupB).lastSelectedToggle = this;
        }

        protected override void CallSubmitEvent()
        {
            _SortOrder = sortOrder;
            CardCollectionView view = Runtime.Program.instance.deckEditor.GetUI<ServantUI.DeckEditorUI>().CardCollectionView;
            view.SetSortIcon(GetIconSprite());
            view.SetSortText(GetSortText());
            if(view.area == Area.Collection)
                view.PrintSearchCards();
            else
                view.PrintBookmarkCards();
            Runtime.Program.instance.ui_.currentPopupB.Hide();
        }

        protected string GetSortText()
        {
            return sortOrder switch
            {
                SortOrder.ByType => MDPro3.InterString.Get("种类"),
                SortOrder.ByTypeReverse => MDPro3.InterString.Get("种类"),
                SortOrder.ByLevelUp => MDPro3.InterString.Get("等级·阶级·连接"),
                SortOrder.ByLevelDown => MDPro3.InterString.Get("等级·阶级·连接"),
                SortOrder.ByAttackUp => MDPro3.InterString.Get("攻击力"),
                SortOrder.ByAttackDown => MDPro3.InterString.Get("攻击力"),
                SortOrder.ByDefenceUp => MDPro3.InterString.Get("守备力"),
                SortOrder.ByDefenceDown => MDPro3.InterString.Get("守备力"),
                SortOrder.ByRarityUp => MDPro3.InterString.Get("稀有度"),
                SortOrder.ByRarityDown => MDPro3.InterString.Get("稀有度"),
                SortOrder.ByGPUp => MDPro3.InterString.Get("Genesys分数"),
                SortOrder.ByGPDown => MDPro3.InterString.Get("Genesys分数"),
                _ => string.Empty
            };
        }
        protected override void OnClick()
        {
            if (!Selectable.IsInteractable())
            {
                Runtime.AudioManager.PlaySE(SoundLabelClickInactive);
                return;
            }
            Runtime.AudioManager.PlaySE(SoundLabelClick);
            SetToggleOn();
            CallSubmitEvent();
        }

        protected override void OnSubmit()
        {
            OnClick();
        }
    }
}
