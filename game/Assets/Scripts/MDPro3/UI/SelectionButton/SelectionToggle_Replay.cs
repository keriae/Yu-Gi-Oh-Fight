using MDPro3.UI.ServantUI;
using Percy;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MDPro3.UI
{
    public class SelectionToggle_Replay : SelectionToggle_ScrollRectItem
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

        public string replayName;
        private YRP yrp;

        public override void Refresh()
        {
            base.Refresh();

            Title.text = replayName;
            yrp = Program.instance.replay.GetUI<ReplaySelectorUI>().CacheYRP(replayName);

            NumBadge.SetActive(false);
            TextClear.SetActive(false);
            Art.SetArt(yrp == null ? 0 : yrp.playerData[0].main[0]);
        }

        protected override void CallToggleOnEvent()
        {
            base.CallToggleOnEvent();
            Program.instance.replay.lastSelectedReplayItem = this;
            Program.instance.replay.GetUI<ReplaySelectorUI>().superScrollView.selected = index;

            var ui = Program.instance.replay.GetUI<ReplaySelectorUI>();
            if (yrp == null)
            {
                ui.TextOverview.text = string.Empty;
                ui.ButtonPlayer0.gameObject.SetActive(false);
                ui.ButtonPlayer1.gameObject.SetActive(false);
                ui.ButtonPlayer2.gameObject.SetActive(false);
                ui.ButtonPlayer3.gameObject.SetActive(false);
            }
            else
            {
                ui.ButtonPlayer0.gameObject.SetActive(true);
                ui.ButtonPlayer1.gameObject.SetActive(true);
                ui.ButtonPlayer2.gameObject.SetActive(true);
                ui.ButtonPlayer3.gameObject.SetActive(true);

                var description = "";
                bool tag = false;
                if ((yrp.opt & 0x20) > 0)
                {
                    description += StringHelper.GetUnsafe(1246) + Program.STRING_LINE_BREAK;//双打模式
                    tag = true;
                }
                description += StringHelper.GetUnsafe((int)(1259 + (yrp.opt >> 16))) + Program.STRING_LINE_BREAK;//规则
                description += StringHelper.GetUnsafe(1231) + yrp.StartLp + Program.STRING_LINE_BREAK;//初始基本分：
                description += StringHelper.GetUnsafe(1232) + yrp.StartHand + Program.STRING_LINE_BREAK;//初始手卡数：
                description += StringHelper.GetUnsafe(1233) + yrp.DrawCount + Program.STRING_LINE_BREAK;//每回合抽卡：
                if ((yrp.opt & 0x10) > 0)
                    description += StringHelper.GetUnsafe(1230) + Program.STRING_LINE_BREAK;

                ui.ButtonPlayer0.SetButtonText(yrp.playerData[0].name);
                ui.ButtonPlayer1.SetButtonText(yrp.playerData[1].name);
                if (tag)
                {
                    ui.ButtonPlayer2.SetButtonText(yrp.playerData[2].name);
                    ui.ButtonPlayer3.SetButtonText(yrp.playerData[3].name);
                }
                else
                {
                    ui.ButtonPlayer2.gameObject.SetActive(false);
                    ui.ButtonPlayer3.gameObject.SetActive(false);
                }

                ui.TextOverview.text = description;
            }
        }

        protected override void CallSubmitEvent()
        {
            Program.instance.replay.GetUI<ReplaySelectorUI>().KF_Replay(replayName);
        }

        protected override void OnNavigation(AxisEventData eventData)
        {
            base.OnNavigation(eventData);

            if (eventData.moveDir == MoveDirection.Right)
            {
                UserInput.NextSelectionIsAxis = true;
                var deckButton = Program.instance.replay.GetUI<ReplaySelectorUI>().ButtonPlayer0;
                if (deckButton.gameObject.activeSelf)
                    deckButton.GetSelectable().Select();
                else
                    Program.instance.replay.GetUI<ReplaySelectorUI>().ButtonGodView.GetSelectable().Select();
            }
        }
    }
}
