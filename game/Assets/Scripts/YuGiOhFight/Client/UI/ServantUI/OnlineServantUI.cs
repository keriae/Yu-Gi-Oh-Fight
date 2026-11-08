using UnityEngine.UI;


namespace YuGiOhFight.Client.UI.ServantUI
{
    public class OnlineServantUI : ServantUI
    {

        #region Elements

        private const string LABEL_STG_SERVER_ADDRESSES = "ToggleLegacy";
        private SelectionToggle_Online m_ToggleServerAddresses;
        private SelectionToggle_Online ToggleServerAddresses =>
            m_ToggleServerAddresses = m_ToggleServerAddresses != null ? m_ToggleServerAddresses
            : Manager.GetElement<SelectionToggle_Online>(LABEL_STG_SERVER_ADDRESSES);

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

        private const string LABEL_MONO_SERVER_ADDRESS_PAGE = "PageLegacy";
        private ServerAddressPage m_ServerAddressPage;
        public ServerAddressPage ServerAddressPage =>
            m_ServerAddressPage = m_ServerAddressPage != null ? m_ServerAddressPage
            : Manager.GetElement<ServerAddressPage>(LABEL_MONO_SERVER_ADDRESS_PAGE);

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
            ToggleServerAddresses.SetToggleOn();
        }

        public override void ShowEvent()
        {
            base.ShowEvent();

            ServerAddressPage.PrintAddresses();
        }

        public void SelectLastSelectable(Selectable lastSelectable)
        {
            if (ServerAddressPage.gameObject.activeSelf)
            {
                if (lastSelectable != null && lastSelectable.transform.IsChildOf(ServerAddressPage.transform))
                    lastSelectable.Select();
                else
                    ServerAddressPage.SelectDefault();
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
            ToggleServerAddresses.OnLeftSelection();
        }

        public void PageRight()
        {
            ToggleServerAddresses.OnRightSelection();
        }
    }
}
