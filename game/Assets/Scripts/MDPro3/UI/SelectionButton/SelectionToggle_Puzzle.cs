using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;
using MDPro3.Duel.YGOSharp;

namespace MDPro3.UI
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

        public PuzzleSelectorUI.Puzzle puzzle;

        private string GetPuzzlePath()
        {
            return Program.PATH_PUZZLE + puzzle.pathPrefix + puzzle.name;
        }

        public override void Refresh()
        {
            base.Refresh();
            Title.text = PuzzlePortalUI.GetPuzzleDisplayName(puzzle.name, puzzle.namePattern);
            Art.SetArt(int.Parse(puzzle.firstCard));
            RefreshBadge();
        }

        public void RefreshBadge()
        {
            NumBadge.SetActive(!Config.GetBool(puzzle.GetEnterConfigKey(), false));
            TextClear.SetActive(Config.GetBool(puzzle.GetClearConfigKey(), false));
        }

        protected override void CallToggleOnEvent()
        {
            base.CallToggleOnEvent();

            Program.instance.puzzle.GetUI<PuzzleSelectorUI>().superScrollView.selected = index;
            Program.instance.puzzle.GetUI<PuzzleSelectorUI>().SetOverview(puzzle.description + Program.STRING_LINE_BREAK + puzzle.solution);

            Program.instance.puzzle.GetUI<PuzzleSelectorUI>().Art.SetArt(int.Parse(puzzle.firstCard));
            Program.instance.puzzle.currentPuzzle = GetPuzzlePath();
            Program.instance.puzzle.lastPuzzleItem = this;
        }

        protected override void CallSubmitEvent()
        {
            base.CallSubmitEvent();
            Program.instance.puzzle.StartCurrentPuzzle();
        }

        protected override void OnNavigation(AxisEventData eventData)
        {
            base.OnNavigation(eventData);

            if (eventData.moveDir == MoveDirection.Right)
            {
                UserInput.NextSelectionIsAxis = true;
                Program.instance.puzzle.GetUI<PuzzleSelectorUI>()
                    .ButtonPlay.GetSelectable().Select();
            }
        }
    }
}
