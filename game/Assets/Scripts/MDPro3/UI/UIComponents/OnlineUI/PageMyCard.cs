using MDPro3.Net;
using MDPro3.Servant;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YgomSystem.ElementSystem;


namespace MDPro3.UI
{
    public class PageMyCard : MonoBehaviour
    {

        #region Elements

        private ElementObjectManager m_Manager;
        private ElementObjectManager Manager =>
            m_Manager = m_Manager != null ? m_Manager 
            : GetComponent<ElementObjectManager>();

        private const string LABEL_GO_PAGELOGIN = "PageLogin";
        private GameObject m_PageLogin;
        public GameObject PageLogin =>
            m_PageLogin = m_PageLogin != null ? m_PageLogin
            : Manager.GetElement(LABEL_GO_PAGELOGIN);

        private const string LABEL_IPT_ACCOUNT = "PageLogin/InputFieldAccount";
        private TMP_InputField m_InputAccount;
        private TMP_InputField InputAccount =>
            m_InputAccount = m_InputAccount != null ? m_InputAccount
            : Manager.GetNestedElement<TMP_InputField>(LABEL_IPT_ACCOUNT);

        private const string LABEL_IPT_PASSWORD = "PageLogin/InputFieldPassword";
        private TMP_InputField m_InputPassword;
        private TMP_InputField InputPassword =>
            m_InputPassword = m_InputPassword != null ? m_InputPassword
            : Manager.GetNestedElement<TMP_InputField>(LABEL_IPT_PASSWORD);

        private const string LABEL_SBN_LOGIN = "PageLogin/ButtonLogin";
        private SelectionButton m_ButtonLogin;
        private SelectionButton ButtonLogin =>
            m_ButtonLogin = m_ButtonLogin != null ? m_ButtonLogin
            : Manager.GetNestedElement<SelectionButton>(LABEL_SBN_LOGIN);

        private const string LABEL_GO_PAGEFUNCTION = "PageFunction";
        private GameObject m_PageFunction;
        public GameObject PageFunction =>
            m_PageFunction = m_PageFunction != null ? m_PageFunction
            : Manager.GetElement(LABEL_GO_PAGEFUNCTION);

        private const string LABEL_MONO_USERPROFILE = "PageFunction/UserProfile";
        private UserProfile m_UserProfile;
        public UserProfile UserProfile =>
            m_UserProfile = m_UserProfile != null ? m_UserProfile
            : Manager.GetNestedElement<UserProfile>(LABEL_MONO_USERPROFILE);

        private const string LABEL_SBN_DECKSELECTOR = "PageFunction/DeckSelector";
        private SelectionButton_DeckSelector m_ButtonDeckSelector;
        public SelectionButton_DeckSelector ButtonDeckSelector =>
            m_ButtonDeckSelector = m_ButtonDeckSelector != null ? m_ButtonDeckSelector
            : Manager.GetNestedElement<SelectionButton_DeckSelector>(LABEL_SBN_DECKSELECTOR);

        private const string LABEL_MONO_WATCHLIST = "PageFunction/WatchList";
        private WatchListHandler m_WatchList;
        public WatchListHandler WatchList =>
            m_WatchList = m_WatchList != null ? m_WatchList
            : Manager.GetNestedElement<WatchListHandler>(LABEL_MONO_WATCHLIST);

        private const string LABEL_IPT_DUELIST = "PageFunction/InputFieldDuelist";
        private TMP_InputField m_InputDuelist;
        private TMP_InputField InputDuelist =>
            m_InputDuelist = m_InputDuelist != null ? m_InputDuelist
            : Manager.GetNestedElement<TMP_InputField>(LABEL_IPT_DUELIST);

        private const string LABEL_SBN_AMATCH = "PageFunction/ButtonAMatch";
        private SelectionButton m_ButtonAMatch;
        public SelectionButton ButtonAMatch =>
            m_ButtonAMatch = m_ButtonAMatch != null ? m_ButtonAMatch
            : Manager.GetNestedElement<SelectionButton>(LABEL_SBN_AMATCH);

        private const string LABEL_SBN_EMatch = "PageFunction/ButtonEMatch";
        private SelectionButton m_ButtonEMatch;
        public SelectionButton ButtonEMatch =>
            m_ButtonEMatch = m_ButtonEMatch != null ? m_ButtonEMatch
            : Manager.GetNestedElement<SelectionButton>(LABEL_SBN_EMatch);

        #endregion

        public void OnMyCardRegister()
        {
            Application.OpenURL("https://accounts.moecube.com/signup");
        }

        public void OnMyCardLogin()
        {
            Program.instance.online.LoginMyCard(InputAccount.text, InputPassword.text);
        }

        public void OnExitLogin()
        {
            List<string> tasks = new()
            {
                InterString.Get("退出登录"),
                InterString.Get("是否确认退出登录？"),
                InterString.Get("确认"),
                InterString.Get("取消")
            };
            UIManager.ShowPopupYesOrNo(tasks, ExitLogin, null);
        }

        private void ExitLogin()
        {
            Config.Set("MyCardToken", Config.STRING_NO);
            Config.Save();
            MyCard.account = null;
            MyCard.CloseAthleticWatchListWebSocket();

            ActivePageLogin();
        }

        public void SelectDefault()
        {
            if (PageLogin.activeSelf)
                ButtonLogin.GetSelectable().Select();
            else
                ButtonAMatch.GetSelectable().Select();
        }

        public void OnDeckSelect()
        {
            Program.instance.deckSelector.SwitchCondition(DeckSelector.Condition.MyCard);
            Program.instance.ShiftToServant(Program.instance.deckSelector);
        }

        public void OnEntertainMatch()
        {
            Program.instance.online.EntertainMatch();
        }

        public void SelectLastWatchItem()
        {
            if (Program.instance.online.lastSelectedWatchItem != null)
            {
                UserInput.NextSelectionIsAxis = true;
                Program.instance.online.lastSelectedWatchItem.GetSelectable().Select();
            }
        }

        public void OnAthleticMatch()
        {
            Program.instance.online.AthleticMatch();
        }

        public void ActivePageLogin()
        {
            PageLogin.SetActive(true);
            PageFunction.SetActive(false);

            InputAccount.text = string.Empty;
            InputPassword.text = string.Empty;
        }

        public void ActivePageFunction()
        {
            PageLogin.SetActive(false);
            PageFunction.SetActive(true);
        }

        public void DisablePageLogin()
        {
            PageLogin.SetActive(false);
        }

    }
}
