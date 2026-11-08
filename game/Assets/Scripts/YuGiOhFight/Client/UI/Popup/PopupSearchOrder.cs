using UnityEngine;
using UnityEngine.EventSystems;


namespace YuGiOhFight.Client.UI.Popup
{
    public class PopupSearchOrder : Popup
    {
        [Header("Popup Selection")]
        public SelectionToggle_SearchOrder lastSelectedToggle;

        public override void Show()
        {
            base.Show();
            Runtime.AudioManager.PlaySE("SE_SYS_VERIFY");
            //foreach (var toggle in transform.GetComponentsInChildren<ToggleForSearchOrder>())
            //    if (toggle.sortOrder == Program.instance.editDeck.sortOrder)
            //    {
            //        toggle.SwitchOnWithoutAction();
            //        break;
            //    }
        }

        protected override void SelectLastSelected()
        {
            EventSystem.current.SetSelectedGameObject(lastSelectedToggle.gameObject);
        }

        public void SelectLastItem()
        {
            Runtime.UserInput.NextSelectionIsAxis = true;
            SelectLastSelected();
        }
    }
}
