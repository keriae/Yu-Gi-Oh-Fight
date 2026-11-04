using MDPro3.Duel.YGOSharp;
using MDPro3.Net;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    public class OnlineServantUI : ServantUI
    {

        #region Elements

        private const string LABEL_STG_LEGACY = "ToggleLegacy";
        private SelectionToggle_Online m_ToggleLegacy;
        private SelectionToggle_Online ToggleLegacy =>
            m_ToggleLegacy = m_ToggleLegacy != null ? m_ToggleLegacy
            : Manager.GetElement<SelectionToggle_Online>(LABEL_STG_LEGACY);

        private const string LABEL_STG_HOST = "ToggleLocal";
        private SelectionToggle_Online m_ToggleHost;
        private SelectionToggle_Online ToggleHost =>
            m_ToggleHost = m_ToggleHost != null ? m_ToggleHost
            : Manager.GetElement<SelectionToggle_Online>(LABEL_STG_HOST);

        private const string LABEL_STG_MyCard = "ToggleMyCard";
        private SelectionToggle_Online m_ToggleMyCard;
        private SelectionToggle_Online ToggleMyCard =>
            m_ToggleMyCard = m_ToggleMyCard != null ? m_ToggleMyCard
            : Manager.GetElement<SelectionToggle_Online>(LABEL_STG_MyCard);

        private const string LABEL_MONO_PAGELEGACY = "PageLegacy";
        private PageLegacy m_PageLegacy;
        public PageLegacy PageLegacy =>
            m_PageLegacy = m_PageLegacy != null ? m_PageLegacy
            : Manager.GetElement<PageLegacy>(LABEL_MONO_PAGELEGACY);

        private const string LABEL_MONO_PAGEHOST = "PageHost";
        private PageHost m_PageHost;
        public PageHost PageHost =>
            m_PageHost = m_PageHost != null ? m_PageHost
            : Manager.GetElement<PageHost>(LABEL_MONO_PAGEHOST);

        private const string LABEL_MONO_PAGEMYCARD = "PageMyCard";
        private PageMyCard m_PageMyCard;
        public PageMyCard PageMyCard =>
            m_PageMyCard = m_PageMyCard != null ? m_PageMyCard
            : Manager.GetElement<PageMyCard>(LABEL_MONO_PAGEMYCARD);

        #endregion

        private void Awake()
        {
            ToggleLegacy.SetToggleOn();
        }

        public override void ShowEvent()
        {
            base.ShowEvent();

            PageLegacy.PrintAddresses();
        }

        public void SelectLastSelectable(Selectable lastSelectable)
        {
            if (PageLegacy.gameObject.activeSelf)
            {
                if (lastSelectable != null && lastSelectable.transform.IsChildOf(PageLegacy.transform))
                    lastSelectable.Select();
                else
                    PageLegacy.SelectDefault();
            }
            else if (PageHost.gameObject.activeSelf)
            {
                if (lastSelectable != null && lastSelectable.transform.IsChildOf(PageHost.transform))
                    lastSelectable.Select();
                else
                    PageHost.SelectDefault();
            }
            else if (PageMyCard.gameObject.activeSelf)
            {
                if (lastSelectable != null && lastSelectable.transform.IsChildOf(PageMyCard.transform))
                    lastSelectable.Select();
                else
                    PageMyCard.SelectDefault();
            }
        }

        public void PageLeft()
        {
            ToggleLegacy.OnLeftSelection();
        }

        public void PageRight()
        {
            ToggleLegacy.OnRightSelection();
        }
    }
}
