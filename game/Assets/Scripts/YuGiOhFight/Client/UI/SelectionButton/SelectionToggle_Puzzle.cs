using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;


namespace YuGiOhFight.Client.UI
{
    public class SelectionToggle_Puzzle : SelectionToggle_ScrollRectItem
    {

        #region Elements

        private const string LABEL_TXT_TITLE = "Title";
        private TextMeshProUGUI m_Title;
        private TextMeshProUGUI Title =>
            m_Title = m_Title != null ? m_Title
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_TITLE);

        private const string LABEL_ART = "Image";
        private ArtRawImageHandler m_Art;
        private ArtRawImageHandler Art =>
            m_Art = m_Art != null ? m_Art
            : Manager.GetElement<ArtRawImageHandler>(LABEL_ART);

        private const string LABEL_GO_NUMBADGE = "NumBadge";
        private GameObject m_NumBadge;
        private GameObject NumBadge =>
            m_NumBadge = m_NumBadge != null ? m_NumBadge
            : Manager.GetElement(LABEL_GO_NUMBADGE);

        private const string LABEL_GO_TEXTCLEAR = "TextClear";
        private GameObject m_TextClear;
        private GameObject TextClear =>
            m_TextClear = m_TextClear != null ? m_TextClear
            : Manager.GetElement(LABEL_GO_TEXTCLEAR);

        #endregion

        public ServantUI.PuzzleSelectorUI.Puzzle puzzle;

        private string GetPuzzlePath()
        {
            return Runtime.Program.PATH_PUZZLE + puzzle.pathPrefix + puzzle.name;
        }

        public override void Refresh()
        {
            base.Refresh();
            Title.text = ServantUI.PuzzlePortalUI.GetPuzzleDisplayName(puzzle.name, puzzle.namePattern);
            Art.SetArt(int.Parse(puzzle.firstCard));
            RefreshBadge();
        }

        public void RefreshBadge()
        {
            NumBadge.SetActive(!MDPro3.Config.GetBool(puzzle.GetEnterConfigKey(), false));
            TextClear.SetActive(MDPro3.Config.GetBool(puzzle.GetClearConfigKey(), false));
        }

        protected override void CallToggleOnEvent()
        {
            base.CallToggleOnEvent();

            Runtime.Program.instance.puzzle.GetUI<ServantUI.PuzzleSelectorUI>().superScrollView.selected = index;
            Runtime.Program.instance.puzzle.GetUI<ServantUI.PuzzleSelectorUI>().SetOverview(puzzle.description + Runtime.Program.STRING_LINE_BREAK + puzzle.solution);

            Runtime.Program.instance.puzzle.GetUI<ServantUI.PuzzleSelectorUI>().Art.SetArt(int.Parse(puzzle.firstCard));
            Runtime.Program.instance.puzzle.currentPuzzle = GetPuzzlePath();
            Runtime.Program.instance.puzzle.lastPuzzleItem = this;
        }

        protected override void CallSubmitEvent()
        {
            base.CallSubmitEvent();
            Runtime.Program.instance.puzzle.StartCurrentPuzzle();
        }

        protected override void OnNavigation(AxisEventData eventData)
        {
            base.OnNavigation(eventData);

            if (eventData.moveDir == MoveDirection.Right)
            {
                Runtime.UserInput.NextSelectionIsAxis = true;
                Runtime.Program.instance.puzzle.GetUI<ServantUI.PuzzleSelectorUI>()
                    .ButtonPlay.GetSelectable().Select();
            }
        }
    }
}
