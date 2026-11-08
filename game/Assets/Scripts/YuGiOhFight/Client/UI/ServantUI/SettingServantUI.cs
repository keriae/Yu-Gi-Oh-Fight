using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

using ShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution;


namespace YuGiOhFight.Client.UI.ServantUI
{
    public class SettingServantUI : ServantUI
    {
        [Header("Setting Servant UI")]
        [SerializeField] private SelectionToggle_Setting defaultToggle;

        #region Elements

        #region Toggles

        private const string LABEL_STG_SYSTEM = "ToggleSystem";
        private SelectionToggle_Setting m_ToggleSystem;
        private SelectionToggle_Setting ToggleSystem =>
            m_ToggleSystem = m_ToggleSystem != null ? m_ToggleSystem
            : Manager.GetElement<SelectionToggle_Setting>(LABEL_STG_SYSTEM);

        private const string LABEL_STG_DUEL = "ToggleDuel";
        private SelectionToggle_Setting m_ToggleDuel;
        private SelectionToggle_Setting ToggleDuel =>
            m_ToggleDuel = m_ToggleDuel != null ? m_ToggleDuel
            : Manager.GetElement<SelectionToggle_Setting>(LABEL_STG_DUEL);

        private const string LABEL_STG_WATCH = "ToggleWatch";
        private SelectionToggle_Setting m_ToggleWatch;
        private SelectionToggle_Setting ToggleWatch =>
            m_ToggleWatch = m_ToggleWatch != null ? m_ToggleWatch
            : Manager.GetElement<SelectionToggle_Setting>(LABEL_STG_WATCH);

        private const string LABEL_STG_REPLAY = "ToggleReplay";
        private SelectionToggle_Setting m_ToggleReplay;
        private SelectionToggle_Setting ToggleReplay =>
            m_ToggleReplay = m_ToggleReplay != null ? m_ToggleReplay
            : Manager.GetElement<SelectionToggle_Setting>(LABEL_STG_REPLAY);

        private const string LABEL_STG_PORT = "TogglePort";
        private SelectionToggle_Setting m_TogglePort;
        private SelectionToggle_Setting TogglePort =>
            m_TogglePort = m_TogglePort != null ? m_TogglePort
            : Manager.GetElement<SelectionToggle_Setting>(LABEL_STG_PORT);

        private const string LABEL_STG_EXPANSIONS = "ToggleExpansions";
        private SelectionToggle_Setting m_ToggleExpansions;
        private SelectionToggle_Setting ToggleExpansions =>
            m_ToggleExpansions = m_ToggleExpansions != null ? m_ToggleExpansions
            : Manager.GetElement<SelectionToggle_Setting>(LABEL_STG_EXPANSIONS);

        private const string LABEL_STG_ABOUT = "ToggleAbout";
        private SelectionToggle_Setting m_ToggleAbout;
        private SelectionToggle_Setting ToggleAbout =>
            m_ToggleAbout = m_ToggleAbout != null ? m_ToggleAbout
            : Manager.GetElement<SelectionToggle_Setting>(LABEL_STG_ABOUT);

        private const string LABEL_SBN_CHECK_DECKS = "ButtonCheckDecks";
        private SelectionButton m_ButtonCheckDecks;
        private SelectionButton ButtonCheckDecks =>
            m_ButtonCheckDecks = m_ButtonCheckDecks != null ? m_ButtonCheckDecks
            : Manager.GetElement<SelectionButton>(LABEL_SBN_CHECK_DECKS);


        private const string LABEL_SBN_RETRY = "ButtonRetry";
        private SelectionButton m_ButtonRetry;
        private SelectionButton ButtonRetry =>
            m_ButtonRetry = m_ButtonRetry != null ? m_ButtonRetry
            : Manager.GetElement<SelectionButton>(LABEL_SBN_RETRY);

        private const string LABEL_SBN_SURRENDER = "ButtonSurrender";
        private SelectionButton m_ButtonSurrender;
        private SelectionButton ButtonSurrender =>
            m_ButtonSurrender = m_ButtonSurrender != null ? m_ButtonSurrender
            : Manager.GetElement<SelectionButton>(LABEL_SBN_SURRENDER);

        #endregion

        #region System

        private const string LABEL_SBN_BGM = "BGM";
        private SelectionButton_Setting m_ButtonBGM;
        private SelectionButton_Setting ButtonBGM =>
            m_ButtonBGM = m_ButtonBGM != null ? m_ButtonBGM
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_BGM);

        private const string LABEL_SBN_SE = "SE";
        private SelectionButton_Setting m_ButtonSE;
        private SelectionButton_Setting ButtonSE =>
            m_ButtonSE = m_ButtonSE != null ? m_ButtonSE
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_SE);

        private const string LABEL_SBN_VOICE = "Voice";
        private SelectionButton_Setting m_ButtonVoice;
        private SelectionButton_Setting ButtonVoice =>
            m_ButtonVoice = m_ButtonVoice != null ? m_ButtonVoice
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_VOICE);

        private const string LABEL_SBN_SCREENMODE = "ScreenMode";
        private SelectionButton_Setting m_ButtonScreenMode;
        private SelectionButton_Setting ButtonScreenMode =>
            m_ButtonScreenMode = m_ButtonScreenMode != null ? m_ButtonScreenMode
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_SCREENMODE);

        private const string LABEL_SBN_RESOLUTION = "Resolution";
        private SelectionButton_Setting m_ButtonResolution;
        private SelectionButton_Setting ButtonResolution =>
            m_ButtonResolution = m_ButtonResolution != null ? m_ButtonResolution
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_RESOLUTION);

        private const string LABEL_SBN_SCALE = "Scale";
        private SelectionButton_Setting m_ButtonScale;
        private SelectionButton_Setting ButtonScale =>
            m_ButtonScale = m_ButtonScale != null ? m_ButtonScale
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_SCALE);

        private const string LABEL_SBN_QUALITY = "Quality";
        private SelectionButton_Setting m_ButtonQuality;
        private SelectionButton_Setting ButtonQuality =>
            m_ButtonQuality = m_ButtonQuality != null ? m_ButtonQuality
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_QUALITY);

        private const string LABEL_SBN_FAA = "FAA";
        private SelectionButton_Setting m_ButtonFAA;
        private SelectionButton_Setting ButtonFAA =>
            m_ButtonFAA = m_ButtonFAA != null ? m_ButtonFAA
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_FAA);

        private const string LABEL_SBN_AAA = "AAA";
        private SelectionButton_Setting m_ButtonAAA;
        private SelectionButton_Setting ButtonAAA =>
            m_ButtonAAA = m_ButtonAAA != null ? m_ButtonAAA
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_AAA);

        private const string LABEL_SBN_SHADOW = "Shadow";
        private SelectionButton_Setting m_ButtonShadow;
        private SelectionButton_Setting ButtonShadow =>
            m_ButtonShadow = m_ButtonShadow != null ? m_ButtonShadow
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_SHADOW);

        private const string LABEL_SBN_FPS = "FPS";
        private SelectionButton_Setting m_ButtonFPS;
        private SelectionButton_Setting ButtonFPS =>
            m_ButtonFPS = m_ButtonFPS != null ? m_ButtonFPS
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_FPS);

        private const string LABEL_SBN_SHOWFPS = "ShowFPS";
        private SelectionButton_Setting m_ButtonShowFPS;
        private SelectionButton_Setting ButtonShowFPS =>
            m_ButtonShowFPS = m_ButtonShowFPS != null ? m_ButtonShowFPS
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_SHOWFPS);

        private const string LABEL_SBN_RUMBLE = "Rumble";
        private SelectionButton_Setting m_ButtonRumble;
        private SelectionButton_Setting ButtonRumble =>
            m_ButtonRumble = m_ButtonRumble != null ? m_ButtonRumble
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_RUMBLE);

        private const string LABEL_SBN_Confirm = "Confirm";
        private SelectionButton_Setting m_ButtonConfirm;
        private SelectionButton_Setting ButtonConfirm =>
            m_ButtonConfirm = m_ButtonConfirm != null ? m_ButtonConfirm
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_Confirm);

        private const string LABEL_SBN_LAYOUT = "Layout";
        private SelectionButton_Setting m_ButtonLayout;
        private SelectionButton_Setting ButtonLayout =>
            m_ButtonLayout = m_ButtonLayout != null ? m_ButtonLayout
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_LAYOUT);

        private const string LABEL_SBN_SYNC_DECK = "SyncDeck";
        private SelectionButton_Setting m_ButtonSyncDeck;
        private SelectionButton_Setting ButtonSyncDeck =>
            m_ButtonSyncDeck = m_ButtonSyncDeck != null ? m_ButtonSyncDeck
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_SYNC_DECK);

        private const string LABEL_SBN_BACKGROUND = "Background";
        private SelectionButton_Setting m_ButtonBackground;
        private SelectionButton_Setting ButtonBackground =>
            m_ButtonBackground = m_ButtonBackground != null ? m_ButtonBackground
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_BACKGROUND);

        private const string LABEL_SBN_BGMBY = "BgmBy";
        private SelectionButton_Setting m_ButtonBgmBy;
        private SelectionButton_Setting ButtonBgmBy =>
            m_ButtonBgmBy = m_ButtonBgmBy != null ? m_ButtonBgmBy
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_BGMBY);

        private const string LABEL_SBN_CARDSTYLE = "CardStyle";
        private SelectionButton_Setting m_ButtonCardStyle;
        private SelectionButton_Setting ButtonCardStyle =>
            m_ButtonCardStyle = m_ButtonCardStyle != null ? m_ButtonCardStyle
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_CARDSTYLE);

        private const string LABEL_SBN_VIDEO_CARD = "VideoCard";
        private SelectionButton_Setting m_ButtonVideoCard;
        private SelectionButton_Setting ButtonVideoCard =>
            m_ButtonVideoCard = m_ButtonVideoCard != null ? m_ButtonVideoCard
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_VIDEO_CARD);

        private const string LABEL_SBN_CARDLANGUAGE = "CardLanguage";
        private SelectionButton_Setting m_ButtonCardLanguage;
        private SelectionButton_Setting ButtonCardLanguage =>
            m_ButtonCardLanguage = m_ButtonCardLanguage != null ? m_ButtonCardLanguage
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_CARDLANGUAGE);

        private const string LABEL_SBN_LANGUAGE = "Language";
        private SelectionButton_Setting m_ButtonLanguage;
        private SelectionButton_Setting ButtonLanguage =>
            m_ButtonLanguage = m_ButtonLanguage != null ? m_ButtonLanguage
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_LANGUAGE);

        #endregion

        #region Duel

        private const string LABEL_SBN_DUELAPPEARANCE = "DuelAppearance";
        private SelectionButton_Setting m_ButtonDuelAppearance;
        private SelectionButton_Setting ButtonDuelAppearance =>
            m_ButtonDuelAppearance = m_ButtonDuelAppearance != null ? m_ButtonDuelAppearance
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELAPPEARANCE);

        private const string LABEL_SBN_WATCHAPPEARANCE = "WatchAppearance";
        private SelectionButton_Setting m_ButtonWatchAppearance;
        private SelectionButton_Setting ButtonWatchAppearance =>
            m_ButtonWatchAppearance = m_ButtonWatchAppearance != null ? m_ButtonWatchAppearance
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHAPPEARANCE);

        private const string LABEL_SBN_REPLAYAPPEARANCE = "ReplayAppearance";
        private SelectionButton_Setting m_ButtonReplayAppearance;
        private SelectionButton_Setting ButtonReplayAppearance =>
            m_ButtonReplayAppearance = m_ButtonReplayAppearance != null ? m_ButtonReplayAppearance
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYAPPEARANCE);

        private const string LABEL_SBN_DUELCHARACTER = "DuelCharacter";
        private SelectionButton_Setting m_ButtonDuelCharacter;
        private SelectionButton_Setting ButtonDuelCharacter =>
            m_ButtonDuelCharacter = m_ButtonDuelCharacter != null ? m_ButtonDuelCharacter
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELCHARACTER);

        private const string LABEL_SBN_WATCHCHARACTER = "WatchCharacter";
        private SelectionButton_Setting m_ButtonWatchCharacter;
        private SelectionButton_Setting ButtonWatchCharacter =>
            m_ButtonWatchCharacter = m_ButtonWatchCharacter != null ? m_ButtonWatchCharacter
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHCHARACTER);

        private const string LABEL_SBN_REPLAYCHARACTER = "ReplayCharacter";
        private SelectionButton_Setting m_ButtonReplayCharacter;
        private SelectionButton_Setting ButtonReplayCharacter =>
            m_ButtonReplayCharacter = m_ButtonReplayCharacter != null ? m_ButtonReplayCharacter
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYCHARACTER);

        private const string LABEL_SBN_DUELVOICE = "DuelVoice";
        private SelectionButton_Setting m_ButtonDuelVoice;
        private SelectionButton_Setting ButtonDuelVoice =>
            m_ButtonDuelVoice = m_ButtonDuelVoice != null ? m_ButtonDuelVoice
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELVOICE);

        private const string LABEL_SBN_WATCHVOICE = "WatchVoice";
        private SelectionButton_Setting m_ButtonWatchVoice;
        private SelectionButton_Setting ButtonWatchVoice =>
            m_ButtonWatchVoice = m_ButtonWatchVoice != null ? m_ButtonWatchVoice
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHVOICE);

        private const string LABEL_SBN_REPLAYVOICE = "ReplayVoice";
        private SelectionButton_Setting m_ButtonReplayVoice;
        private SelectionButton_Setting ButtonReplayVoice =>
            m_ButtonReplayVoice = m_ButtonReplayVoice != null ? m_ButtonReplayVoice
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYVOICE);

        private const string LABEL_SBN_DUELCLOSEUP = "DuelCloseup";
        private SelectionButton_Setting m_ButtonDuelCloseup;
        private SelectionButton_Setting ButtonDuelCloseup =>
            m_ButtonDuelCloseup = m_ButtonDuelCloseup != null ? m_ButtonDuelCloseup
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELCLOSEUP);

        private const string LABEL_SBN_WATCHCLOSEUP = "WatchCloseup";
        private SelectionButton_Setting m_ButtonWatchCloseup;
        private SelectionButton_Setting ButtonWatchCloseup =>
            m_ButtonWatchCloseup = m_ButtonWatchCloseup != null ? m_ButtonWatchCloseup
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHCLOSEUP);

        private const string LABEL_SBN_REPLAYCLOSEUP = "ReplayCloseup";
        private SelectionButton_Setting m_ButtonReplayCloseup;
        private SelectionButton_Setting ButtonReplayCloseup =>
            m_ButtonReplayCloseup = m_ButtonReplayCloseup != null ? m_ButtonReplayCloseup
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYCLOSEUP);

        private const string LABEL_SBN_DUELPENDULUM = "DuelPendulum";
        private SelectionButton_Setting m_ButtonDuelPendulum;
        private SelectionButton_Setting ButtonDuelPendulum =>
            m_ButtonDuelPendulum = m_ButtonDuelPendulum != null ? m_ButtonDuelPendulum
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELPENDULUM);

        private const string LABEL_SBN_WATCHPENDULUM = "WatchPendulum";
        private SelectionButton_Setting m_ButtonWatchPendulum;
        private SelectionButton_Setting ButtonWatchPendulum =>
            m_ButtonWatchPendulum = m_ButtonWatchPendulum != null ? m_ButtonWatchPendulum
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHPENDULUM);

        private const string LABEL_SBN_REPLAYPENDULUM = "ReplayPendulum";
        private SelectionButton_Setting m_ButtonReplayPendulum;
        private SelectionButton_Setting ButtonReplayPendulum =>
            m_ButtonReplayPendulum = m_ButtonReplayPendulum != null ? m_ButtonReplayPendulum
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYPENDULUM);

        private const string LABEL_SBN_DUELCUTIN = "DuelCutin";
        private SelectionButton_Setting m_ButtonDuelCutin;
        private SelectionButton_Setting ButtonDuelCutin =>
            m_ButtonDuelCutin = m_ButtonDuelCutin != null ? m_ButtonDuelCutin
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELCUTIN);

        private const string LABEL_SBN_WATCHCUTIN = "WatchCutin";
        private SelectionButton_Setting m_ButtonWatchCutin;
        private SelectionButton_Setting ButtonWatchCutin =>
            m_ButtonWatchCutin = m_ButtonWatchCutin != null ? m_ButtonWatchCutin
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHCUTIN);

        private const string LABEL_SBN_REPLAYCUTIN = "ReplayCutin";
        private SelectionButton_Setting m_ButtonReplayCutin;
        private SelectionButton_Setting ButtonReplayCutin =>
            m_ButtonReplayCutin = m_ButtonReplayCutin != null ? m_ButtonReplayCutin
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYCUTIN);

        private const string LABEL_SBN_DUELEFFECT = "DuelEffect";
        private SelectionButton_Setting m_ButtonDuelEffect;
        private SelectionButton_Setting ButtonDuelEffect =>
            m_ButtonDuelEffect = m_ButtonDuelEffect != null ? m_ButtonDuelEffect
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELEFFECT);

        private const string LABEL_SBN_WATCHEFFECT = "WatchEffect";
        private SelectionButton_Setting m_ButtonWatchEffect;
        private SelectionButton_Setting ButtonWatchEffect =>
            m_ButtonWatchEffect = m_ButtonWatchEffect != null ? m_ButtonWatchEffect
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHEFFECT);

        private const string LABEL_SBN_REPLAYEFFECT = "ReplayEffect";
        private SelectionButton_Setting m_ButtonReplayEffect;
        private SelectionButton_Setting ButtonReplayEffect =>
            m_ButtonReplayEffect = m_ButtonReplayEffect != null ? m_ButtonReplayEffect
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYEFFECT);

        private const string LABEL_SBN_DUELCHAIN = "DuelChain";
        private SelectionButton_Setting m_ButtonDuelChain;
        private SelectionButton_Setting ButtonDuelChain =>
            m_ButtonDuelChain = m_ButtonDuelChain != null ? m_ButtonDuelChain
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELCHAIN);

        private const string LABEL_SBN_WATCHCHAIN = "WatchChain";
        private SelectionButton_Setting m_ButtonWatchChain;
        private SelectionButton_Setting ButtonWatchChain =>
            m_ButtonWatchChain = m_ButtonWatchChain != null ? m_ButtonWatchChain
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHCHAIN);

        private const string LABEL_SBN_REPLAYCHAIN = "ReplayChain";
        private SelectionButton_Setting m_ButtonReplayChain;
        private SelectionButton_Setting ButtonReplayChain =>
            m_ButtonReplayChain = m_ButtonReplayChain != null ? m_ButtonReplayChain
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYCHAIN);

        private const string LABEL_SBN_DUELDICE = "DuelDice";
        private SelectionButton_Setting m_ButtonDuelDice;
        private SelectionButton_Setting ButtonDuelDice =>
            m_ButtonDuelDice = m_ButtonDuelDice != null ? m_ButtonDuelDice
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELDICE);

        private const string LABEL_SBN_WATCHDICE = "WatchDice";
        private SelectionButton_Setting m_ButtonWatchDice;
        private SelectionButton_Setting ButtonWatchDice =>
            m_ButtonWatchDice = m_ButtonWatchDice != null ? m_ButtonWatchDice
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHDICE);

        private const string LABEL_SBN_REPLAYDICE = "ReplayDice";
        private SelectionButton_Setting m_ButtonReplayDice;
        private SelectionButton_Setting ButtonReplayDice =>
            m_ButtonReplayDice = m_ButtonReplayDice != null ? m_ButtonReplayDice
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYDICE);

        private const string LABEL_SBN_DUELCOIN = "DuelCoin";
        private SelectionButton_Setting m_ButtonDuelCoin;
        private SelectionButton_Setting ButtonDuelCoin =>
            m_ButtonDuelCoin = m_ButtonDuelCoin != null ? m_ButtonDuelCoin
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELCOIN);

        private const string LABEL_SBN_WATCHCOIN = "WatchCoin";
        private SelectionButton_Setting m_ButtonWatchCoin;
        private SelectionButton_Setting ButtonWatchCoin =>
            m_ButtonWatchCoin = m_ButtonWatchCoin != null ? m_ButtonWatchCoin
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHCOIN);

        private const string LABEL_SBN_REPLAYCOIN = "ReplayCoin";
        private SelectionButton_Setting m_ButtonReplayCoin;
        private SelectionButton_Setting ButtonReplayCoin =>
            m_ButtonReplayCoin = m_ButtonReplayCoin != null ? m_ButtonReplayCoin
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYCOIN);

        private const string LABEL_SBN_DUELAUTOINFO = "DuelAutoInfo";
        private SelectionButton_Setting m_ButtonDuelAutoInfo;
        private SelectionButton_Setting ButtonDuelAutoInfo =>
            m_ButtonDuelAutoInfo = m_ButtonDuelAutoInfo != null ? m_ButtonDuelAutoInfo
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELAUTOINFO);

        private const string LABEL_SBN_WATCHAUTOINFO = "WatchAutoInfo";
        private SelectionButton_Setting m_ButtonWatchAutoInfo;
        private SelectionButton_Setting ButtonWatchAutoInfo =>
            m_ButtonWatchAutoInfo = m_ButtonWatchAutoInfo != null ? m_ButtonWatchAutoInfo
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHAUTOINFO);

        private const string LABEL_SBN_REPLAYAUTOINFO = "ReplayAutoInfo";
        private SelectionButton_Setting m_ButtonReplayAutoInfo;
        private SelectionButton_Setting ButtonReplayAutoInfo =>
            m_ButtonReplayAutoInfo = m_ButtonReplayAutoInfo != null ? m_ButtonReplayAutoInfo
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYAUTOINFO);

        private const string LABEL_SBN_DUELFACEDOWN = "DuelFaceDown";
        private SelectionButton_Setting m_ButtonDuelFaceDown;
        private SelectionButton_Setting ButtonDuelFaceDown =>
            m_ButtonDuelFaceDown = m_ButtonDuelFaceDown != null ? m_ButtonDuelFaceDown
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELFACEDOWN);

        private const string LABEL_SBN_WATCHFACEDOWN = "WatchFaceDown";
        private SelectionButton_Setting m_ButtonWatchFaceDown;
        private SelectionButton_Setting ButtonWatchFaceDown =>
            m_ButtonWatchFaceDown = m_ButtonWatchFaceDown != null ? m_ButtonWatchFaceDown
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHFACEDOWN);

        private const string LABEL_SBN_REPLAYFACEDOWN = "ReplayFaceDown";
        private SelectionButton_Setting m_ButtonReplayFaceDown;
        private SelectionButton_Setting ButtonReplayFaceDown =>
            m_ButtonReplayFaceDown = m_ButtonReplayFaceDown != null ? m_ButtonReplayFaceDown
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYFACEDOWN);

        private const string LABEL_SBN_DUELPLAYERMESSAGE = "DuelPlayerMessage";
        private SelectionButton_Setting m_ButtonDuelPlayerMessage;
        private SelectionButton_Setting ButtonDuelPlayerMessage =>
            m_ButtonDuelPlayerMessage = m_ButtonDuelPlayerMessage != null ? m_ButtonDuelPlayerMessage
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELPLAYERMESSAGE);

        private const string LABEL_SBN_WATCHPLAYERMESSAGE = "WatchPlayerMessage";
        private SelectionButton_Setting m_ButtonWatchPlayerMessage;
        private SelectionButton_Setting ButtonWatchPlayerMessage =>
            m_ButtonWatchPlayerMessage = m_ButtonWatchPlayerMessage != null ? m_ButtonWatchPlayerMessage
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHPLAYERMESSAGE);

        private const string LABEL_SBN_REPLAYPLAYERMESSAGE = "ReplayPlayerMessage";
        private SelectionButton_Setting m_ButtonReplayPlayerMessage;
        private SelectionButton_Setting ButtonReplayPlayerMessage =>
            m_ButtonReplayPlayerMessage = m_ButtonReplayPlayerMessage != null ? m_ButtonReplayPlayerMessage
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYPLAYERMESSAGE);

        private const string LABEL_SBN_DUELSYSTEMMESSAGE = "DuelSystemMessage";
        private SelectionButton_Setting m_ButtonDuelSystemMessage;
        private SelectionButton_Setting ButtonDuelSystemMessage =>
            m_ButtonDuelSystemMessage = m_ButtonDuelSystemMessage != null ? m_ButtonDuelSystemMessage
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELSYSTEMMESSAGE);

        private const string LABEL_SBN_WATCHSYSTEMMESSAGE = "WatchSystemMessage";
        private SelectionButton_Setting m_ButtonWatchSystemMessage;
        private SelectionButton_Setting ButtonWatchSystemMessage =>
            m_ButtonWatchSystemMessage = m_ButtonWatchSystemMessage != null ? m_ButtonWatchSystemMessage
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHSYSTEMMESSAGE);

        private const string LABEL_SBN_REPLAYSYSTEMMESSAGE = "ReplaySystemMessage";
        private SelectionButton_Setting m_ButtonReplaySystemMessage;
        private SelectionButton_Setting ButtonReplaySystemMessage =>
            m_ButtonReplaySystemMessage = m_ButtonReplaySystemMessage != null ? m_ButtonReplaySystemMessage
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYSYSTEMMESSAGE);

        private const string LABEL_SBN_DUELACC = "DuelAcc";
        private SelectionButton_Setting m_ButtonDuelAcc;
        private SelectionButton_Setting ButtonDuelAcc =>
            m_ButtonDuelAcc = m_ButtonDuelAcc != null ? m_ButtonDuelAcc
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELACC);

        private const string LABEL_SBN_WATCHACC = "WatchAcc";
        private SelectionButton_Setting m_ButtonWatchAcc;
        private SelectionButton_Setting ButtonWatchAcc =>
            m_ButtonWatchAcc = m_ButtonWatchAcc != null ? m_ButtonWatchAcc
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHACC);

        private const string LABEL_SBN_REPLAYACC = "ReplayAcc";
        private SelectionButton_Setting m_ButtonReplayAcc;
        private SelectionButton_Setting ButtonReplayAcc =>
            m_ButtonReplayAcc = m_ButtonReplayAcc != null ? m_ButtonReplayAcc
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYACC);

        private const string LABEL_SBN_DUELAUTOACC = "DuelAutoAcc";
        private SelectionButton_Setting m_ButtonDuelAutoAcc;
        private SelectionButton_Setting ButtonDuelAutoAcc =>
            m_ButtonDuelAutoAcc = m_ButtonDuelAutoAcc != null ? m_ButtonDuelAutoAcc
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELAUTOACC);

        private const string LABEL_SBN_WATCHAUTOACC = "WatchAutoAcc";
        private SelectionButton_Setting m_ButtonWatchAutoAcc;
        private SelectionButton_Setting ButtonWatchAutoAcc =>
            m_ButtonWatchAutoAcc = m_ButtonWatchAutoAcc != null ? m_ButtonWatchAutoAcc
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHAUTOACC);

        private const string LABEL_SBN_REPLAYAUTOACC = "ReplayAutoAcc";
        private SelectionButton_Setting m_ButtonReplayAutoAcc;
        private SelectionButton_Setting ButtonReplayAutoAcc =>
            m_ButtonReplayAutoAcc = m_ButtonReplayAutoAcc != null ? m_ButtonReplayAutoAcc
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYAUTOACC);

        private const string LABEL_SBN_TIMING = "Timing";
        private SelectionButton_Setting m_ButtonTiming;
        private SelectionButton_Setting ButtonTiming =>
            m_ButtonTiming = m_ButtonTiming != null ? m_ButtonTiming
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_TIMING);

        private const string LABEL_SBN_AUTORPS = "AutoRPS";
        private SelectionButton_Setting m_ButtonAutoRPS;
        private SelectionButton_Setting ButtonAutoRPS =>
            m_ButtonAutoRPS = m_ButtonAutoRPS != null ? m_ButtonAutoRPS
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_AUTORPS);

        #endregion

        #region Expansion

        private const string LABEL_SBN_EXPANSIONSUPPORT = "ExpansionSupport";
        private SelectionButton_Setting m_ButtonExpansionSupport;
        private SelectionButton_Setting ButtonExpansionSupport =>
            m_ButtonExpansionSupport = m_ButtonExpansionSupport != null ? m_ButtonExpansionSupport
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_EXPANSIONSUPPORT);

        private const string LABEL_SBN_UPDATEPRERELEASE = "UpdatePrerelease";
        private SelectionButton_Setting m_ButtonUpdatePrerelease;
        public SelectionButton_Setting ButtonUpdatePrerelease =>
            m_ButtonUpdatePrerelease = m_ButtonUpdatePrerelease != null ? m_ButtonUpdatePrerelease
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_UPDATEPRERELEASE);

        private const string LABEL_SBN_DOWNLOAD_YPK = "DonwloadYPK";
        private SelectionButton_Setting m_ButtonDownloadYPK;
        public SelectionButton_Setting ButtonDownloadYPK =>
            m_ButtonDownloadYPK = m_ButtonDownloadYPK != null ? m_ButtonDownloadYPK
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DOWNLOAD_YPK);

        #endregion

        #endregion

        public override void Initialize(Controllers.Servant servant)
        {
            base.Initialize(servant);

            InitializeVolume();
            InitializeScreenMode();
            InitializeResolution();
            InitializeScale();
            InitializeQuality();
            InitializeFAA();
            InitializeAAA();
            InitializeShadow();
            InitializeFPS();
            InitializeShowFPS();
            InitializeRumble();
            InitializeConfirm();
            InitializeLayout();
            InitializeSyncDeck();
            InitializeBackground();
            InitializeBgmBy();
            InitializeCardStyle();
            InitializeVideoCard();
            InitializeCardLanguage();
            InitializeLanguage();

            InitializeAppearance();
            InitializeCharacter();
            InitializeVoice();
            InitializeCloseup();
            InitializeSummon();
            InitializePendulum();
            InitializeCutin();
            InitializeEffect();
            InitializeChain();
            InitializeDice();
            InitializeCoin();
            InitializeAutoInfo();
            InitializeFaceDown();
            InitializePlayerMessage();
            InitializeSystemMessage();
            InitializeAcc();
            InitializeAutoAcc();
            InitializeTimming();
            InitializeAutoRPS();

            InitializeExpansions();
        }

        public override void SelectDefaultSelectable()
        {
            defaultToggle.GetSelectable().Select();
            defaultToggle.ScrollRectToTop();
        }

        public override void ShowEvent()
        {
            base.ShowEvent();

            if (Runtime.Program.instance.currentServant == Runtime.Program.instance.ocgcore)
            {
                if (Controllers.OcgCore.condition == Controllers.OcgCore.Condition.Duel)
                {
                    ToggleDuel.gameObject.SetActive(true);
                    ToggleWatch.gameObject.SetActive(false);
                    ToggleReplay.gameObject.SetActive(false);

                    var canRetrySolo = Controllers.RoomServant.FromSolo;
                    ButtonRetry.gameObject.SetActive(canRetrySolo);
                    if (canRetrySolo)
                        ButtonRetry.SetButtonText(MDPro3.InterString.Get("重试"));
                    ButtonSurrender.gameObject.SetActive(true);
                    ButtonSurrender.SetButtonText(MDPro3.InterString.Get("投降"));

                    bool needShowCheckDecks = !Controllers.OcgCore.inPuzzle;
                    ButtonCheckDecks.gameObject.SetActive(needShowCheckDecks);
                }
                else if (Controllers.OcgCore.condition == Controllers.OcgCore.Condition.Watch)
                {
                    ToggleDuel.gameObject.SetActive(false);
                    ToggleWatch.gameObject.SetActive(true);
                    ToggleReplay.gameObject.SetActive(false);

                    ButtonRetry.gameObject.SetActive(false);
                    ButtonSurrender.gameObject.SetActive(true);
                    ButtonSurrender.SetButtonText(MDPro3.InterString.Get("退出观战"));
                    ButtonCheckDecks.gameObject.SetActive(false);
                }
                else if (Controllers.OcgCore.condition == Controllers.OcgCore.Condition.Replay)
                {
                    ToggleDuel.gameObject.SetActive(false);
                    ToggleWatch.gameObject.SetActive(false);
                    ToggleReplay.gameObject.SetActive(true);

                    ButtonRetry.gameObject.SetActive(false);
                    ButtonSurrender.gameObject.SetActive(true);
                    ButtonSurrender.SetButtonText(MDPro3.InterString.Get("退出回放"));
                    ButtonCheckDecks.gameObject.SetActive(false);
                }
                TogglePort.gameObject.SetActive(false);
                ToggleExpansions.gameObject.SetActive(false);
            }
            else
            {
                ToggleDuel.gameObject.SetActive(true);
                ToggleWatch.gameObject.SetActive(true);
                ToggleReplay.gameObject.SetActive(true);
                TogglePort.gameObject.SetActive(true);
                ToggleExpansions.gameObject.SetActive(true);

                ButtonRetry.gameObject.SetActive(false);
                ButtonSurrender.gameObject.SetActive(false);
                ButtonCheckDecks.gameObject.SetActive(false);
            }
        }

        protected override void HideEvent()
        {
            base.HideEvent();
            Save();
            if (Runtime.Program.instance.ocgcore.showing)
            {
                Runtime.UIManager.HideBlackBack(Runtime.Program.instance.setting.TransitionTime);
                Runtime.UIManager.HideExitButton(Runtime.Program.instance.setting.TransitionTime);
            }

            if (videoCardConfigChanged)
            {
                videoCardConfigChanged = false;
                Runtime.SystemEvent.CallVideoCardConfigChangeEvent();
            }
        }

        protected override void AfterHideEvent()
        {
            base.AfterHideEvent();
            if (Runtime.Program.instance.ocgcore.showing)
                Runtime.UIManager.ShowFPSLeft();
        }

        private void Save()
        {
            //Non-WholeNumbers Slider Value Need be saved here;

            MDPro3.Config.SetFloat("BgmVol", GetBGMVolum());
            MDPro3.Config.SetFloat("SEVol", GetSEVolum());
            MDPro3.Config.SetFloat("VoiceVol", GetVoiceVolum());

            MDPro3.Config.SetFloat("Scale", ButtonScale.GetSliderValue());
            FpsSave();

            MDPro3.Config.SetFloat("DuelAcc", ButtonDuelAcc.GetSliderValue());
            MDPro3.Config.SetFloat("WatchAcc", ButtonWatchAcc.GetSliderValue());
            MDPro3.Config.SetFloat("ReplayAcc", ButtonReplayAcc.GetSliderValue());

            MDPro3.Config.Save();
        }

        #region System

        #region Volume

        private void InitializeVolume()
        {
            ButtonBGM.SetSliderEvent(OnBgmVolChange);
            ButtonBGM.SetSliderValue(MDPro3.Config.GetFloat("BgmVol", 0.7f));

            ButtonSE.SetSliderEvent(OnSeVolChange);
            ButtonSE.SetSliderValue(MDPro3.Config.GetFloat("SEVol", 0.7f));

            ButtonVoice.SetSliderEvent(OnVoiceVolChange);
            ButtonVoice.SetSliderValue(MDPro3.Config.GetFloat("VoiceVol", 0.7f));
            ButtonVoice.SetClickEvent(OnPlayTestVoice);
        }
        public float GetBGMVolum()
        {
            return ButtonBGM.GetSliderValue();
        }
        public float GetSEVolum()
        {
            return ButtonSE.GetSliderValue();
        }
        public float GetVoiceVolum()
        {
            return ButtonVoice.GetSliderValue();
        }
        private void OnPlayTestVoice()
        {
            Runtime.AudioManager.PlayVoiceByResourcePath("VOICE/VoiceSample");
        }
        private void OnBgmVolChange(float vol)
        {
            Runtime.AudioManager.SetBGMVol(vol);
        }
        private void OnSeVolChange(float vol)
        {
            Runtime.AudioManager.SetSeVol(vol);
        }
        private void OnVoiceVolChange(float vol)
        {
            Runtime.AudioManager.SetVoiceVol(vol);
        }

        #endregion

        #region Screen Mode

        private void InitializeScreenMode()
        {
            ButtonScreenMode.SetClickEvent(OnScreenModeChange);
            string value = MDPro3.Config.Get("ScreenMode", "1");
            if (value == "0")
            {
                ButtonScreenMode.SetModeText(MDPro3.InterString.Get("独占全屏"));
                ButtonScreenMode.SetNoteText(MDPro3.InterString.Get("独占全屏（仅Windows端有效）"));
            }
            else if (value == "1")
            {
                ButtonScreenMode.SetModeText(MDPro3.InterString.Get("窗口全屏"));
                ButtonScreenMode.SetNoteText(MDPro3.InterString.Get("全屏显示"));
            }
            else
            {
                ButtonScreenMode.SetModeText(MDPro3.InterString.Get("窗口化"));
                ButtonScreenMode.SetNoteText(MDPro3.InterString.Get("窗口化（仅桌面端有效）"));
            }
        }
        private void OnScreenModeChange()
        {
            List<string> selections = new()
            {
                MDPro3.InterString.Get("显示模式"),
                string.Empty,
                MDPro3.InterString.Get("独占全屏"),
                MDPro3.InterString.Get("窗口全屏"),
                MDPro3.InterString.Get("窗口化")
            };
            Runtime.UIManager.ShowPopupSelection(selections, ScreenModeChange);
        }
        private void ScreenModeChange()
        {
            string selected = EventSystem.current.currentSelectedGameObject
                .GetComponent<SelectionButton>().GetButtonText();

            if (selected == MDPro3.InterString.Get("独占全屏"))
            {
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.ExclusiveFullScreen);
                ButtonScreenMode.SetModeText(MDPro3.InterString.Get("独占全屏"));
                ButtonScreenMode.SetNoteText(MDPro3.InterString.Get("独占全屏（仅Windows端有效）"));
            }
            else if (selected == MDPro3.InterString.Get("窗口全屏"))
            {
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
                ButtonScreenMode.SetModeText(MDPro3.InterString.Get("窗口全屏"));
                ButtonScreenMode.SetNoteText(MDPro3.InterString.Get("全屏显示"));
            }
            else
            {
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, false);
                ButtonScreenMode.SetModeText(MDPro3.InterString.Get("窗口化"));
                ButtonScreenMode.SetNoteText(MDPro3.InterString.Get("窗口化（仅桌面端有效）"));
            }

            MDPro3.Config.Set("ScreenMode", SaveScreenMode(selected));
        }
        private string SaveScreenMode(string value)
        {
            string returnValue = "1";
            if (value == MDPro3.InterString.Get("独占全屏"))
                returnValue = "0";
            else if (value == MDPro3.InterString.Get("窗口全屏"))
                returnValue = "1";
            else if (value == MDPro3.InterString.Get("窗口化"))
                returnValue = "2";
            return returnValue;
        }

        #endregion

        #region Resolution

        private void InitializeResolution()
        {
            ButtonResolution.SetClickEvent(OnResolutionChange);
            SetResolutionText();
            Runtime.SystemEvent.OnResolutionChange += SetResolutionText;
        }
        private void OnResolutionChange()
        {
            List<string> selections = new()
            {
                MDPro3.InterString.Get("分辨率"),
                string.Empty
            };
            foreach (var resolution in Screen.resolutions)
            {
                string selection = Regex.Split(resolution.ToString(), " @ ")[0];
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
                int height = int.Parse(Regex.Split(selection, " x ")[0]);
                int width = int.Parse(Regex.Split(selection, " x ")[1]);
                if (height > width)
                    (width, height) = (height, width);
                if (height > 540)
                {
                    string r = (width * 540 / height).ToString() + " x " + 540.ToString();
                    if(!selections.Contains(r))
                        selections.Add(r);
                }
                if(height > 720)
                {
                    string r = (width * 720 / height).ToString() + " x " + 720.ToString();
                    if (!selections.Contains(r))
                        selections.Add(r);
                }
                if (height > 1080)
                {
                    string r = (width * 1080 / height).ToString() + " x " + 1080.ToString();
                    if (!selections.Contains(r))
                        selections.Add(r);
                }
                if (height > 1200)
                {
                    string r = (width * 1200 / height).ToString() + " x " + 1200.ToString();
                    if (!selections.Contains(r))
                        selections.Add(r);
                }
                if (height > 1440)
                {
                    string r = (width * 1440 / height).ToString() + " x " + 1440.ToString();
                    if (!selections.Contains(r))
                        selections.Add(r);
                }
                if (height > 1600)
                {
                    string r = (width * 1600 / height).ToString() + " x " + 1600.ToString();
                    if (!selections.Contains(r))
                        selections.Add(r);
                }
                if (height > 1800)
                {
                    string r = (width * 1800 / height).ToString() + " x " + 1800.ToString();
                    if (!selections.Contains(r))
                        selections.Add(r);
                }
                if (height > 2160)
                {
                    string r = (width * 2160 / height).ToString() + " x " + 2160.ToString();
                    if (!selections.Contains(r))
                        selections.Add(r);
                }
                selection = width.ToString() + " x " + height.ToString();
#endif
                if (!selections.Contains(selection))
                    selections.Add(selection);
            }

            //selections.Add("3600 x 1620");

            Runtime.UIManager.ShowPopupSelection(selections, ResolutioChange);
        }
        private void ResolutioChange()
        {
            string selected = EventSystem.current.currentSelectedGameObject
                .GetComponent<SelectionButton>().GetButtonText();
            Screen.SetResolution(int.Parse(Regex.Split(selected, " x ")[0]), int.Parse(Regex.Split(selected, " x ")[1]), Screen.fullScreen);
            MDPro3.Config.Set("Resolution", selected);
        }
        private void SetResolutionText()
        {
            ButtonResolution.SetModeText($"{Screen.width} x {Screen.height}");
        }

        #endregion

        #region Scale

        private void InitializeScale()
        {
            ButtonScale.SetSliderEvent(OnScaleChange);
            ButtonScale.SetSliderValue(GetScale());
        }
        private void OnScaleChange(float vol)
        {
            string value = vol.ToString();
            value = value.Length > 4 ? value.Substring(0, 4) : value;
            ButtonScale.SetModeText(value);
            Runtime.Program.instance.camera_.urpAsset.renderScale = float.Parse(value);
        }
        public static float GetScale()
        {
            var defau = 1f;
#if UNITY_ANDROID || UNITY_IOS
            defau = 0.5f;
#endif
            return MDPro3.Config.GetFloat("Scale", defau);
        }

        #endregion

        #region Quality

        private void InitializeQuality()
        {
            ButtonQuality.SetSliderEvent(OnQualityChange);
            var configQuality = MDPro3.Config.GetFloat("Quality", 2f);
            ButtonQuality.SetSliderValue(configQuality);
        }
        private void OnQualityChange(float value)
        {
            string qualityText = (int)value switch
            {
                0 => MDPro3.InterString.Get("非常低"),
                1 => MDPro3.InterString.Get("低"),
                2 => MDPro3.InterString.Get("中等"),
                3 => MDPro3.InterString.Get("高"),
                4 => MDPro3.InterString.Get("非常高"),
                5 => MDPro3.InterString.Get("极致"),
                _ => MDPro3.InterString.Get("中等"),
            };
            MDPro3.Config.SetFloat("Quality", value);
            ButtonQuality.SetModeText(qualityText);
        }

        #endregion

        #region FAA

        private void InitializeFAA()
        {
            ButtonFAA.SetSliderEvent(OnFAAChange);
            ButtonFAA.SetSliderValue(MDPro3.Config.GetFloat("FAA", 1f));
        }

        private void OnFAAChange(float value)
        {
            value = 1f;

            var modeText = "Off";
            switch ((int)value)
            {
                case 1:
                    modeText = MDPro3.InterString.Get("Off");
                    Runtime.Program.instance.camera_.urpAsset.msaaSampleCount = 1;
                    Runtime.Program.instance.camera_.urpAssetForUI.msaaSampleCount = 1;
                    break;
                case 2:
                    modeText = "MSAA 2x";
                    Runtime.Program.instance.camera_.urpAsset.msaaSampleCount = 2;
                    Runtime.Program.instance.camera_.urpAssetForUI.msaaSampleCount = 2;
                    break;
                case 3:
                    modeText = "MSAA 4x";
                    Runtime.Program.instance.camera_.urpAsset.msaaSampleCount = 4;
                    Runtime.Program.instance.camera_.urpAssetForUI.msaaSampleCount = 4;
                    break;
                case 4:
                    modeText = "MSAA 8x";
                    Runtime.Program.instance.camera_.urpAsset.msaaSampleCount = 8;
                    Runtime.Program.instance.camera_.urpAssetForUI.msaaSampleCount = 8;
                    break;
            }

            ButtonFAA.SetModeText(modeText);
            MDPro3.Config.SetFloat("FAA", value);
        }

        #endregion

        #region AAA

        private void InitializeAAA()
        {
            ButtonAAA.SetSliderEvent(OnAAAChange);
            ButtonAAA.SetSliderValue(MDPro3.Config.GetFloat("AAA", 0f));
        }
        private void OnAAAChange(float value)
        {
            ButtonAAA.SetModeText(ChangeAAA(value));
            MDPro3.Config.SetFloat("AAA", value);
        }
        public static string ChangeAAA(float value)
        {
            var cameraData3D = Runtime.Program.instance.camera_.cameraMain.GetUniversalAdditionalCameraData();

            var modeText = "Off";
            switch ((int)value)
            {
                case 0:
                    modeText = MDPro3.InterString.Get("Off");
                    cameraData3D.antialiasing = AntialiasingMode.None;
                    break;
                case 1:
                    modeText = "FAA";
                    cameraData3D.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                    break;
                case 2:
                    modeText = "SMAA Low";
                    cameraData3D.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    cameraData3D.antialiasingQuality = AntialiasingQuality.Low;
                    break;
                case 3:
                    modeText = "SMAA Medium";
                    cameraData3D.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    cameraData3D.antialiasingQuality = AntialiasingQuality.Medium;
                    break;
                case 4:
                    modeText = "SMAA High";
                    cameraData3D.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    cameraData3D.antialiasingQuality = AntialiasingQuality.High;
                    break;
                case 5:
                    modeText = "TAA";
                    cameraData3D.antialiasing = AntialiasingMode.TemporalAntiAliasing;
                    Runtime.Program.instance.camera_.urpAsset.msaaSampleCount = 1;
                    Runtime.Program.instance.camera_.urpAssetForUI.msaaSampleCount = 1;
                    break;
            }

            return modeText;
        }

        #endregion

        #region Shadow

        private void InitializeShadow()
        {
            ButtonShadow.SetSliderEvent(OnShadowChange);
            ButtonShadow.SetSliderValue(MDPro3.Config.GetFloat("Shadow", 0f));
        }
        private void OnShadowChange(float value)
        {
            ButtonShadow.SetModeText(ChangeShadow(value));
            MDPro3.Config.SetFloat("Shadow", value);
        }
        public static string ChangeShadow(float value)
        {
            SROptions sr = new();
            var modeText = MDPro3.InterString.Get("非常低");
            switch ((int)value)
            {
                case 0:
                    modeText = MDPro3.InterString.Get("非常低");
                    sr.MainLightShadowResolution = ShadowResolution._256;
                    sr.SupportsSoftShadows = false;
                    break;
                case 1:
                    modeText = MDPro3.InterString.Get("低");
                    sr.MainLightShadowResolution = ShadowResolution._512;
                    sr.SupportsSoftShadows = false;
                    break;
                case 2:
                    modeText = MDPro3.InterString.Get("中等");
                    sr.MainLightShadowResolution = ShadowResolution._1024;
                    sr.SupportsSoftShadows = false;
                    break;
                case 3:
                    modeText = MDPro3.InterString.Get("高");
                    sr.MainLightShadowResolution = ShadowResolution._2048;
                    sr.SupportsSoftShadows = true;
                    break;
                case 4:
                    modeText = MDPro3.InterString.Get("非常高");
                    sr.MainLightShadowResolution = ShadowResolution._4096;
                    sr.SupportsSoftShadows = true;
                    break;
            }

            return modeText;
        }

        #endregion

        #region FPS

        private void InitializeFPS()
        {
            ButtonFPS.SetSliderEvent(OnFpsChange);

            var configFPS = GetFPS();
            if (configFPS == 0)
                configFPS = 29;
            ButtonFPS.SetSliderValue(configFPS);
        }
        private void OnFpsChange(float value)
        {
            var fps = ChangeFPS(value);
            ButtonFPS.SetModeText(fps == 0 ? MDPro3.InterString.Get("无限制") : fps.ToString());
        }
        public static int ChangeFPS(float value)
        {
            if (value == 29f)
                value = 0f;
            // Keep the slider's configured value independent of temporary menu/background caps.
            MDPro3.Utility.RuntimeFramePacing.SetConfiguredFrameRate((int)value);
            return (int)value;
        }
        public static int GetFPS()
        {
            var defau = 60f;
            if (MDPro3.Utility.DeviceInfo.OnMobile())
                defau = 30f;
            return (int)MDPro3.Config.GetFloat("FPS", defau);
        }
        private void FpsSave()
        {
            var config = ButtonFPS.GetSliderValue();
            if (config == 29f)
                config = 0f;
            MDPro3.Config.SetFloat("FPS", config);
        }

        #endregion

        #region ShowFPS

        private void InitializeShowFPS()
        {
            ButtonShowFPS.SetClickEvent(OnShowFPSClicked);

            var config = MDPro3.Config.GetBool("ShowFPS", true);
            ButtonShowFPS.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));
            ChangeShowFPS();
        }
        private void OnShowFPSClicked()
        {
            var config = MDPro3.Config.GetBool("ShowFPS", true);
            MDPro3.Config.SetBool("ShowFPS", !config);
            ButtonShowFPS.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            ChangeShowFPS();
        }
        public static void ChangeShowFPS()
        {
            if(MDPro3.Config.GetBool("ShowFPS", true))
                Runtime.UIManager.ShowFPS();
            else
                Runtime.UIManager.HideFPS();
        }

        #endregion

        #region Rumble

        private void InitializeRumble()
        {
            ButtonRumble.SetClickEvent(OnRumbleClicked);
            var config = MDPro3.Config.GetBool("Rumble", true);
            ButtonRumble.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));
        }
        private void OnRumbleClicked()
        {
            var config = MDPro3.Config.GetBool("Rumble", true);
            ButtonRumble.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("Rumble", !config);
        }

        #endregion

        #region Confirm

        private void InitializeConfirm()
        {
            ButtonConfirm.SetClickEvent(OnConfirmClicked);
            var config = MDPro3.Config.GetBool("Confirm", false);
            ButtonConfirm.SetModeText(MDPro3.InterString.Get(config ? "左" : "右"));
        }

        private void OnConfirmClicked()
        {
            var config = MDPro3.Config.GetBool("Confirm", false);
            ButtonConfirm.SetModeText(MDPro3.InterString.Get(config ? "右" : "左"));
            MDPro3.Config.SetBool("Confirm", !config);
        }

        #endregion

        #region Layout

        private void InitializeLayout()
        {
            ButtonLayout.SetClickEvent(OnLayoutClicked);
            SetLayoutMode(MDPro3.Config.GetFloat("Layout", 0f));
        }
        private void OnLayoutClicked()
        {
            if (Runtime.Program.instance.ocgcore.showing)
            {
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("决斗中不能更改此选项。"));
                return;
            }

            List<string> selections = new()
            {
                MDPro3.InterString.Get("UI布局"),
                string.Empty,
                MDPro3.InterString.Get("自动判断"),
                MDPro3.InterString.Get("桌面布局"),
                MDPro3.InterString.Get("移动布局")
            };
            Runtime.UIManager.ShowPopupSelection(selections, LayoutChange);
        }
        private void LayoutChange()
        {
            string selected = EventSystem.current.currentSelectedGameObject
                .GetComponent<SelectionButton>().GetButtonText();
            ButtonLayout.SetModeText(selected);

            var config = 0f;
            if (selected == MDPro3.InterString.Get("桌面布局"))
                config = 1f;
            else if (selected == MDPro3.InterString.Get("移动布局"))
                config = 2f;
            MDPro3.Config.SetFloat("Layout", config);

            Runtime.UIManager.ChangeLayout();
        }
        private void SetLayoutMode(float config)
        {
            var value = MDPro3.InterString.Get("自动判断");
            if (config == 1f)
                value = MDPro3.InterString.Get("桌面布局");
            else if (config == 2f)
                value = MDPro3.InterString.Get("移动布局");
            ButtonLayout.SetModeText(value);
        }

        #endregion

        #region SyncDeck

        private void InitializeSyncDeck()
        {
            ButtonSyncDeck.SetClickEvent(OnSyncDeckClicked);
            var config = MDPro3.Config.GetBool("SyncDeck", true);
            ButtonSyncDeck.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));
        }
        private void OnSyncDeckClicked()
        {
            var config = MDPro3.Config.GetBool("SyncDeck", true);
            ButtonSyncDeck.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("SyncDeck", !config);
        }

        #endregion

        #region Background

        private void InitializeBackground()
        {
            ButtonBackground.SetClickEvent(OnBackgroundClicked);
            ChangeBackgroundModeText();
        }
        private void ChangeBackgroundModeText()
        {
            var id = int.Parse(MDPro3.Config.Get("Background", "0"));
            var value = MDPro3.InterString.Get("随机");
            if (id != 0)
                if (!Runtime.BackgroundManager.backgrounds.TryGetValue(id, out value))
                    value = "Classic";
            if (string.IsNullOrEmpty(value))
                value = MDPro3.InterString.Get("随机");
            ButtonBackground.SetModeText(value);
        }
        private void OnBackgroundClicked()
        {
            List<string> selections = new()
            {
                MDPro3.InterString.Get("更换背景"),
                string.Empty,
                MDPro3.InterString.Get("随机"),
            };
            foreach (var background in Runtime.BackgroundManager.backgrounds)
                selections.Add(background.Value);

            Runtime.UIManager.ShowPopupSelection(selections, BackgroundChange);
        }
        private void BackgroundChange()
        {
            string selected = EventSystem.current.currentSelectedGameObject
                .GetComponent<SelectionButton>().GetButtonText();
            var id = Runtime.Program.instance.background_.GetIDByName(selected);
            MDPro3.Config.Set("Background", id.ToString());
            Runtime.Program.instance.background_.Change(id);
            ChangeBackgroundModeText();
        }

        #endregion

        #region BgmBy

        private void InitializeBgmBy()
        {
            ButtonBgmBy.SetClickEvent(OnBgmByClicked);
            var config = MDPro3.Config.GetBool("BGMbyMySide", true);
            ButtonBgmBy.SetModeText(MDPro3.InterString.Get(config ? "我方" : "对方"));
        }

        private void OnBgmByClicked()
        {
            var config = MDPro3.Config.GetBool("BGMbyMySide", true);
            ButtonBgmBy.SetModeText(MDPro3.InterString.Get(config ? "对方" : "我方"));
            MDPro3.Config.SetBool("BGMbyMySide", !config);
        }

        #endregion

        #region CardStyle

        private void InitializeCardStyle()
        {
            ButtonCardStyle.SetClickEvent(OnCardStyleChange);
            ButtonCardStyle.SetModeText(MDPro3.Config.Get("CardStyle", Presentation.CardRenderer.CardStyle.OCG_TCG.ToString()));
        }

        private void OnCardStyleChange()
        {
            if (Runtime.Program.instance.ocgcore.showing)
            {
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("决斗中不能更改此选项。"));
                return;
            }

            List<string> selections = new()
            {
                MDPro3.InterString.Get("卡图风格"),
                string.Empty
            };
            var values = Enum.GetValues(typeof(Presentation.CardRenderer.CardStyle));
            foreach (var value in values)
                selections.Add(value.ToString());
            Runtime.UIManager.ShowPopupSelection(selections, ChangeCardStyle);
        }

        private void ChangeCardStyle()
        {
            string selected = EventSystem.current.currentSelectedGameObject
                .GetComponent<SelectionButton>().GetButtonText();
            var currrent = MDPro3.Config.Get("CardStyle", selected);
            MDPro3.Config.Set("CardStyle", selected);
            if(currrent != selected)
            {
                ButtonCardStyle.SetModeText(selected);
                Runtime.UIManager.ChangeLanguage();
                MaterialLoader.ClearCachedOverFrameMasks();
            }
        }

        #endregion

        #region Video Card

        private bool videoCardConfigChanged;

        private void InitializeVideoCard()
        {
            ButtonVideoCard.SetClickEvent(OnVideoCardClicked);

            var config = MDPro3.Config.GetBool("VideoCard", true);
            ButtonVideoCard.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));
        }

        private void OnVideoCardClicked()
        {
            var config = MDPro3.Config.GetBool("VideoCard", true);
            MDPro3.Config.SetBool("VideoCard", !config);
            ButtonVideoCard.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            videoCardConfigChanged = true;
        }

        #endregion

        #region CardLanguage

        private void InitializeCardLanguage()
        {
            ButtonCardLanguage.SetClickEvent(OnCardLanguageClicked);
            ButtonCardLanguage.SetModeText(MDPro3.InterString.Get(MDPro3.Utility.Language.GetCardConfig()));
        }
        private void OnCardLanguageClicked()
        {
            if (Runtime.Program.instance.ocgcore.showing)
            {
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("决斗中不能更改此选项。"));
                return;
            }

            List<string> selections = new()
            {
                MDPro3.InterString.Get("卡图语言"),
                string.Empty
            };
            DirectoryInfo[] infos = new DirectoryInfo(Runtime.Program.PATH_LOCALES).GetDirectories();
            foreach (DirectoryInfo info in infos)
                if (!incorrectlyAddedFolder.Contains(info.Name))
                    selections.Add(MDPro3.InterString.Get(info.Name));
            Runtime.UIManager.ShowPopupSelection(selections, ChangeCardLanguage);
        }
        private void ChangeCardLanguage()
        {
            string selected = EventSystem.current.currentSelectedGameObject
                .GetComponent<SelectionButton>().GetButtonText();
            ButtonCardLanguage.SetModeText(selected);
            MDPro3.Utility.Language.SetCardConfig(MDPro3.InterString.GetOriginal(selected));
            Runtime.UIManager.ChangeLanguage();
        }

        #endregion

        #region Language

        private readonly List<string> incorrectlyAddedFolder = new()
        {
            "ko-KRIDS",
        };

        private void InitializeLanguage()
        {
            ButtonLanguage.SetClickEvent(OnLanguageClicked);
            ButtonLanguage.SetModeText(MDPro3.InterString.Get(MDPro3.Utility.Language.GetConfig()));
        }
        private void OnLanguageClicked()
        {
            if (Runtime.Program.instance.ocgcore.showing)
            {
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("决斗中不能更改此选项。"));
                return;
            }

            List<string> selections = new()
            {
                MDPro3.InterString.Get("语言"),
                string.Empty
            };
            DirectoryInfo[] infos = new DirectoryInfo(Runtime.Program.PATH_LOCALES).GetDirectories();
            foreach (DirectoryInfo info in infos)
                if(!incorrectlyAddedFolder.Contains(info.Name))
                    selections.Add(MDPro3.InterString.Get(info.Name));
            Runtime.UIManager.ShowPopupSelection(selections, OnLanguageSelection);
        }
        private void OnLanguageSelection()
        {
            string selected = EventSystem.current.
                currentSelectedGameObject.GetComponent<SelectionButton>().GetButtonText();
            ButtonLanguage.SetModeText(selected);
            MDPro3.Utility.Language.SetConfig(MDPro3.InterString.GetOriginal(selected));
            Runtime.UIManager.ChangeLanguage();
        }

        #endregion

        #endregion

        #region Duel

        #region Appearance

        private void InitializeAppearance()
        {
            ButtonDuelAppearance.SetClickEvent(OnDuelAppearcanceClick);
            ButtonWatchAppearance.SetClickEvent(OnWatchAppearcanceClick);
            ButtonReplayAppearance.SetClickEvent(OnReplayAppearcanceClick);
            RefreshAppearanceModeText();
        }
        private void OnDuelAppearcanceClick()
        {
            Runtime.Program.instance.appearance.SwitchCondition(Controllers.Appearance.Condition.Duel);
            if (Runtime.Program.instance.currentSubServant == Runtime.Program.instance.setting)
                Runtime.Program.instance.ShowSubServant(Runtime.Program.instance.appearance);
            else
                Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.appearance);
        }
        private void OnWatchAppearcanceClick()
        {
            Runtime.Program.instance.appearance.SwitchCondition(Controllers.Appearance.Condition.Watch);
            if (Runtime.Program.instance.currentSubServant == Runtime.Program.instance.setting)
                Runtime.Program.instance.ShowSubServant(Runtime.Program.instance.appearance);
            else
                Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.appearance);
        }
        private void OnReplayAppearcanceClick()
        {
            Runtime.Program.instance.appearance.SwitchCondition(Controllers.Appearance.Condition.Replay);
            if (Runtime.Program.instance.currentSubServant == Runtime.Program.instance.setting)
                Runtime.Program.instance.ShowSubServant(Runtime.Program.instance.appearance);
            else
                Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.appearance);
        }
        public void RefreshAppearanceModeText()
        {
            ButtonDuelAppearance.SetModeText(MDPro3.Config.Get("DuelPlayerName0", MDPro3.Config.EMPTY_STRING));
            ButtonWatchAppearance.SetModeText(MDPro3.Config.Get("WatchPlayerName0", MDPro3.Config.EMPTY_STRING));
            ButtonReplayAppearance.SetModeText(MDPro3.Config.Get("ReplayPlayerName0", MDPro3.Config.EMPTY_STRING));
        }

        #endregion

        #region Character

        private void InitializeCharacter()
        {
            ButtonDuelCharacter.SetClickEvent(OnDuelCharacterClick);
            ButtonWatchCharacter.SetClickEvent(OnWatchCharacterClick);
            ButtonReplayCharacter.SetClickEvent(OnReplayCharacterClick);
            RefreshCharacterName();
        }
        private void OnDuelCharacterClick()
        {
            Runtime.Program.instance.character.SwitchCondition(Controllers.CharacterSelector.Condition.Duel);
            if (Runtime.Program.instance.currentSubServant == Runtime.Program.instance.setting)
                Runtime.Program.instance.ShowSubServant(Runtime.Program.instance.character);
            else
                Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.character);
        }
        private void OnWatchCharacterClick()
        {
            Runtime.Program.instance.character.SwitchCondition(Controllers.CharacterSelector.Condition.Watch);
            if (Runtime.Program.instance.currentSubServant == Runtime.Program.instance.setting)
                Runtime.Program.instance.ShowSubServant(Runtime.Program.instance.character);
            else
                Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.character);
        }
        private void OnReplayCharacterClick()
        {
            Runtime.Program.instance.character.SwitchCondition(Controllers.CharacterSelector.Condition.Replay);
            if (Runtime.Program.instance.currentSubServant == Runtime.Program.instance.setting)
                Runtime.Program.instance.ShowSubServant(Runtime.Program.instance.character);
            else
                Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.character);
        }
        public void RefreshCharacterName()
        {
            if (Controllers.CharacterSelector.characters == null)
                return;

            var characterName = Controllers.CharacterSelector.characters.GetName(MDPro3.Config.Get("DuelCharacter0", Presentation.Voice.VoicePlayer.defaultCharacter));
            ButtonDuelCharacter.SetModeText(characterName);

            characterName = Controllers.CharacterSelector.characters.GetName(MDPro3.Config.Get("WatchCharacter0", Presentation.Voice.VoicePlayer.defaultCharacter));
            ButtonWatchCharacter.SetModeText(characterName);

            characterName = Controllers.CharacterSelector.characters.GetName(MDPro3.Config.Get("ReplayCharacter0", Presentation.Voice.VoicePlayer.defaultCharacter));
            ButtonReplayCharacter.SetModeText(characterName);
        }

        #endregion

        #region Voice

        private void InitializeVoice()
        {
            var config = MDPro3.Config.GetBool("DuelVoice", false);
            ButtonDuelVoice.SetClickEvent(OnDuelVoiceClick);
            ButtonDuelVoice.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("WatchVoice", false);
            ButtonWatchVoice.SetClickEvent(OnWatchVoiceClick);
            ButtonWatchVoice.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("ReplayVoice", false);
            ButtonReplayVoice.SetClickEvent(OnReplayVoiceClick);
            ButtonReplayVoice.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelVoiceClick()
        {
            var config = MDPro3.Config.GetBool("DuelVoice", false);
            ButtonDuelVoice.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("DuelVoice", !config);

            Runtime.Program.instance.ocgcore.CheckCharaFace();
        }
        private void OnWatchVoiceClick()
        {
            var config = MDPro3.Config.GetBool("WatchVoice", false);
            ButtonWatchVoice.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("WatchVoice", !config);

            Runtime.Program.instance.ocgcore.CheckCharaFace();
        }
        private void OnReplayVoiceClick()
        {
            var config = MDPro3.Config.GetBool("ReplayVoice", false);
            ButtonReplayVoice.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("ReplayVoice", !config);

            Runtime.Program.instance.ocgcore.CheckCharaFace();
        }

        #endregion

        #region Closeup

        private void InitializeCloseup()
        {
            var config = MDPro3.Config.GetBool("DuelCloseup", false);
            ButtonDuelCloseup.SetClickEvent(OnDuelCloseupClick);
            ButtonDuelCloseup.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("WatchCloseup", false);
            ButtonWatchCloseup.SetClickEvent(OnWatchCloseupClick);
            ButtonWatchCloseup.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("ReplayCloseup", false);
            ButtonReplayCloseup.SetClickEvent(OnReplayCloseupClick);
            ButtonReplayCloseup.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelCloseupClick()
        {
            var config = MDPro3.Config.GetBool("DuelCloseup", false);
            ButtonDuelCloseup.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("DuelCloseup", !config);
            Runtime.Program.instance.ocgcore.RefreshAllCardsLabel();
        }
        private void OnWatchCloseupClick()
        {
            var config = MDPro3.Config.GetBool("WatchCloseup", false);
            ButtonWatchCloseup.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("WatchCloseup", !config);
            Runtime.Program.instance.ocgcore.RefreshAllCardsLabel();
        }
        private void OnReplayCloseupClick()
        {
            var config = MDPro3.Config.GetBool("ReplayCloseup", false);
            ButtonReplayCloseup.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("ReplayCloseup", !config);
            Runtime.Program.instance.ocgcore.RefreshAllCardsLabel();
        }

        #endregion

        #region Summon

        private void InitializeSummon()
        {
            SelectionButton_Setting button = Manager.GetElement<SelectionButton_Setting>("DuelSummon");
            button.SetClickEvent(OnDuelSummonClick);
            var config = MDPro3.Config.GetBool("DuelSummon", true);
            button.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            button = Manager.GetElement<SelectionButton_Setting>("WatchSummon");
            button.SetClickEvent(OnWatchSummonClick);
            config = MDPro3.Config.GetBool("WatchSummon", true);
            button.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            button = Manager.GetElement<SelectionButton_Setting>("ReplaySummon");
            button.SetClickEvent(OnReplaySummonClick);
            config = MDPro3.Config.GetBool("ReplaySummon", true);
            button.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelSummonClick()
        {
            SelectionButton_Setting button = Manager.GetElement<SelectionButton_Setting>("DuelSummon");
            var config = MDPro3.Config.GetBool("DuelSummon", true);
            button.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("DuelSummon", !config);
        }
        private void OnWatchSummonClick()
        {
            SelectionButton_Setting button = Manager.GetElement<SelectionButton_Setting>("WatchSummon");
            var config = MDPro3.Config.GetBool("WatchSummon", true);
            button.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("WatchSummon", !config);
        }
        private void OnReplaySummonClick()
        {
            SelectionButton_Setting button = Manager.GetElement<SelectionButton_Setting>("ReplaySummon");
            var config = MDPro3.Config.GetBool("ReplaySummon", true);
            button.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("ReplaySummon", !config);
        }
        #endregion

        #region Pendulum

        private void InitializePendulum()
        {
            var config = MDPro3.Config.GetBool("DuelPendulum", true);
            ButtonDuelPendulum.SetClickEvent(OnDuelPendulumClick);
            ButtonDuelPendulum.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("WatchPendulum", true);
            ButtonWatchPendulum.SetClickEvent(OnWatchPendulumClick);
            ButtonWatchPendulum.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("ReplayPendulum", true);
            ButtonReplayPendulum.SetClickEvent(OnReplayPendulumClick);
            ButtonReplayPendulum.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelPendulumClick()
        {
            var config = MDPro3.Config.GetBool("DuelPendulum", true);
            ButtonDuelPendulum.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("DuelPendulum", !config);
        }
        private void OnWatchPendulumClick()
        {
            var config = MDPro3.Config.GetBool("WatchPendulum", true);
            ButtonWatchPendulum.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("WatchPendulum", !config);
        }
        private void OnReplayPendulumClick()
        {
            var config = MDPro3.Config.GetBool("ReplayPendulum", true);
            ButtonReplayPendulum.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("ReplayPendulum", !config);
        }

        #endregion

        #region Cutin

        private void InitializeCutin()
        {
            var config = MDPro3.Config.GetBool("DuelCutin", true);
            ButtonDuelCutin.SetClickEvent(OnDuelCutinClick);
            ButtonDuelCutin.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("WatchCutin", true);
            ButtonWatchCutin.SetClickEvent(OnWatchCutinClick);
            ButtonWatchCutin.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("ReplayCutin", true);
            ButtonReplayCutin.SetClickEvent(OnReplayCutinClick);
            ButtonReplayCutin.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelCutinClick()
        {
            var config = MDPro3.Config.GetBool("DuelCutin", true);
            ButtonDuelCutin.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("DuelCutin", !config);
        }
        private void OnWatchCutinClick()
        {
            var config = MDPro3.Config.GetBool("WatchCutin", true);
            ButtonWatchCutin.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("WatchCutin", !config);
        }
        private void OnReplayCutinClick()
        {
            var config = MDPro3.Config.GetBool("ReplayCutin", true);
            ButtonReplayCutin.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("ReplayCutin", !config);
        }

        #endregion

        #region Effect

        private void InitializeEffect()
        {
            var config = MDPro3.Config.GetBool("DuelEffect", true);
            ButtonDuelEffect.SetClickEvent(OnDuelEffectClick);
            ButtonDuelEffect.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("WatchEffect", true);
            ButtonWatchEffect.SetClickEvent(OnWatchEffectClick);
            ButtonWatchEffect.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("ReplayEffect", true);
            ButtonReplayEffect.SetClickEvent(OnReplayEffectClick);
            ButtonReplayEffect.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelEffectClick()
        {
            var config = MDPro3.Config.GetBool("DuelEffect", true);
            ButtonDuelEffect.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("DuelEffect", !config);
        }
        private void OnWatchEffectClick()
        {
            var config = MDPro3.Config.GetBool("WatchEffect", true);
            ButtonWatchEffect.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("WatchEffect", !config);
        }
        private void OnReplayEffectClick()
        {
            var config = MDPro3.Config.GetBool("ReplayEffect", true);
            ButtonReplayEffect.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("ReplayEffect", !config);
        }

        #endregion

        #region Chain

        private void InitializeChain()
        {
            var config = MDPro3.Config.GetBool("DuelChain", true);
            ButtonDuelChain.SetClickEvent(OnDuelChainClick);
            ButtonDuelChain.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("WatchChain", true);
            ButtonWatchChain.SetClickEvent(OnWatchChainClick);
            ButtonWatchChain.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("ReplayChain", true);
            ButtonReplayChain.SetClickEvent(OnReplayChainClick);
            ButtonReplayChain.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelChainClick()
        {
            var config = MDPro3.Config.GetBool("DuelChain", true);
            ButtonDuelChain.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("DuelChain", !config);
        }
        private void OnWatchChainClick()
        {
            var config = MDPro3.Config.GetBool("WatchChain", true);
            ButtonWatchChain.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("WatchChain", !config);
        }
        private void OnReplayChainClick()
        {
            var config = MDPro3.Config.GetBool("ReplayChain", true);
            ButtonReplayChain.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("ReplayChain", !config);
        }

        #endregion

        #region Dice

        private void InitializeDice()
        {
            var config = MDPro3.Config.GetBool("DuelDice", true);
            ButtonDuelDice.SetClickEvent(OnDuelDiceClick);
            ButtonDuelDice.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("WatchDice", true);
            ButtonWatchDice.SetClickEvent(OnWatchDiceClick);
            ButtonWatchDice.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("ReplayDice", true);
            ButtonReplayDice.SetClickEvent(OnReplayDiceClick);
            ButtonReplayDice.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelDiceClick()
        {
            var config = MDPro3.Config.GetBool("DuelDice", true);
            ButtonDuelDice.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("DuelDice", !config);
        }
        private void OnWatchDiceClick()
        {
            var config = MDPro3.Config.GetBool("WatchDice", true);
            ButtonWatchDice.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("WatchDice", !config);
        }
        private void OnReplayDiceClick()
        {
            var config = MDPro3.Config.GetBool("ReplayDice", true);
            ButtonReplayDice.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("ReplayDice", !config);
        }

        #endregion

        #region Coin

        private void InitializeCoin()
        {
            var config = MDPro3.Config.GetBool("DuelCoin", true);
            ButtonDuelCoin.SetClickEvent(OnDuelCoinClick);
            ButtonDuelCoin.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("WatchCoin", true);
            ButtonWatchCoin.SetClickEvent(OnWatchCoinClick);
            ButtonWatchCoin.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("ReplayCoin", true);
            ButtonReplayCoin.SetClickEvent(OnReplayCoinClick);
            ButtonReplayCoin.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelCoinClick()
        {
            var config = MDPro3.Config.GetBool("DuelCoin", true);
            ButtonDuelCoin.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("DuelCoin", !config);
        }
        private void OnWatchCoinClick()
        {
            var config = MDPro3.Config.GetBool("WatchCoin", true);
            ButtonWatchCoin.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("WatchCoin", !config);
        }
        private void OnReplayCoinClick()
        {
            var config = MDPro3.Config.GetBool("ReplayCoin", true);
            ButtonReplayCoin.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("ReplayCoin", !config);
        }

        #endregion

        #region AutoInfo

        private void InitializeAutoInfo()
        {
            var config = MDPro3.Config.GetBool("DuelAutoInfo", true);
            ButtonDuelAutoInfo.SetClickEvent(OnDuelAutoInfoClick);
            ButtonDuelAutoInfo.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("WatchAutoInfo", true);
            ButtonWatchAutoInfo.SetClickEvent(OnWatchAutoInfoClick);
            ButtonWatchAutoInfo.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("ReplayAutoInfo", true);
            ButtonReplayAutoInfo.SetClickEvent(OnReplayAutoInfoClick);
            ButtonReplayAutoInfo.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelAutoInfoClick()
        {
            var config = MDPro3.Config.GetBool("DuelAutoInfo", true);
            ButtonDuelAutoInfo.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("DuelAutoInfo", !config);
        }
        private void OnWatchAutoInfoClick()
        {
            var config = MDPro3.Config.GetBool("WatchAutoInfo", true);
            ButtonWatchAutoInfo.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("WatchAutoInfo", !config);
        }
        private void OnReplayAutoInfoClick()
        {
            var config = MDPro3.Config.GetBool("ReplayAutoInfo", true);
            ButtonReplayAutoInfo.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("ReplayAutoInfo", !config);
        }

        #endregion

        #region FaceDown

        private void InitializeFaceDown()
        {
            var config = MDPro3.Config.GetBool("DuelFaceDown", true);
            ButtonDuelFaceDown.SetClickEvent(OnDuelFaceDownClick);
            ButtonDuelFaceDown.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("WatchFaceDown", true);
            ButtonWatchFaceDown.SetClickEvent(OnWatchFaceDownClick);
            ButtonWatchFaceDown.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("ReplayFaceDown", true);
            ButtonReplayFaceDown.SetClickEvent(OnReplayFaceDownClick);
            ButtonReplayFaceDown.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelFaceDownClick()
        {
            var config = MDPro3.Config.GetBool("DuelFaceDown", true);
            ButtonDuelFaceDown.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("DuelFaceDown", !config);

            foreach (Duel.GameCard card in Controllers.OcgCore.cards)
                card.ShowFaceDownCardOrNot(card.NeedShowFaceDownCard());
        }
        private void OnWatchFaceDownClick()
        {
            var config = MDPro3.Config.GetBool("WatchFaceDown", true);
            ButtonWatchFaceDown.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("WatchFaceDown", !config);

            foreach (Duel.GameCard card in Controllers.OcgCore.cards)
                card.ShowFaceDownCardOrNot(card.NeedShowFaceDownCard());
        }
        private void OnReplayFaceDownClick()
        {
            var config = MDPro3.Config.GetBool("ReplayFaceDown", true);
            ButtonReplayFaceDown.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("ReplayFaceDown", !config);

            foreach (Duel.GameCard card in Controllers.OcgCore.cards)
                card.ShowFaceDownCardOrNot(card.NeedShowFaceDownCard());
        }

        #endregion

        #region PlayerMessage

        private void InitializePlayerMessage()
        {
            var config = MDPro3.Config.GetBool("DuelPlayerMessage", true);
            ButtonDuelPlayerMessage.SetClickEvent(OnDuelPlayerMessageClick);
            ButtonDuelPlayerMessage.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("WatchPlayerMessage", true);
            ButtonWatchPlayerMessage.SetClickEvent(OnWatchPlayerMessageClick);
            ButtonWatchPlayerMessage.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("ReplayPlayerMessage", true);
            ButtonReplayPlayerMessage.SetClickEvent(OnReplayPlayerMessageClick);
            ButtonReplayPlayerMessage.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelPlayerMessageClick()
        {
            var config = MDPro3.Config.GetBool("DuelPlayerMessage", true);
            ButtonDuelPlayerMessage.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("DuelPlayerMessage", !config);
        }
        private void OnWatchPlayerMessageClick()
        {
            var config = MDPro3.Config.GetBool("WatchPlayerMessage", true);
            ButtonWatchPlayerMessage.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("WatchPlayerMessage", !config);
        }
        private void OnReplayPlayerMessageClick()
        {
            var config = MDPro3.Config.GetBool("ReplayPlayerMessage", true);
            ButtonReplayPlayerMessage.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("ReplayPlayerMessage", !config);
        }

        #endregion

        #region SystemMessage

        private void InitializeSystemMessage()
        {
            var config = MDPro3.Config.GetBool("DuelSystemMessage", true);
            ButtonDuelSystemMessage.SetClickEvent(OnDuelSystemMessageClick);
            ButtonDuelSystemMessage.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("WatchSystemMessage", true);
            ButtonWatchSystemMessage.SetClickEvent(OnWatchSystemMessageClick);
            ButtonWatchSystemMessage.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("ReplaySystemMessage", true);
            ButtonReplaySystemMessage.SetClickEvent(OnReplaySystemMessageClick);
            ButtonReplaySystemMessage.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelSystemMessageClick()
        {
            var config = MDPro3.Config.GetBool("DuelSystemMessage", true);
            ButtonDuelSystemMessage.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("DuelSystemMessage", !config);
        }
        private void OnWatchSystemMessageClick()
        {
            var config = MDPro3.Config.GetBool("WatchSystemMessage", true);
            ButtonWatchSystemMessage.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("WatchSystemMessage", !config);
        }
        private void OnReplaySystemMessageClick()
        {
            var config = MDPro3.Config.GetBool("ReplaySystemMessage", true);
            ButtonReplaySystemMessage.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("ReplaySystemMessage", !config);
        }

        #endregion

        #region Acc

        private void InitializeAcc()
        {
            var config = MDPro3.Config.GetFloat("DuelAcc", 2f);
            ButtonDuelAcc.SetSliderEvent(OnDuelAccChange);
            ButtonDuelAcc.SetSliderValue(config);

            config = MDPro3.Config.GetFloat("WatchAcc", 2f);
            ButtonWatchAcc.SetSliderEvent(OnWatchAccChange);
            ButtonWatchAcc.SetSliderValue(config);

            config = MDPro3.Config.GetFloat("ReplayAcc", 2f);
            ButtonReplayAcc.SetSliderEvent(OnReplayAccChange);
            ButtonReplayAcc.SetSliderValue(config);
        }
        private void OnDuelAccChange(float value)
        {
            string result = value.ToString();
            ButtonDuelAcc.SetModeText(result.Length > 4 ? result[..4] : result);
            if (Runtime.Program.instance.ocgcore.showing)
                if (Controllers.OcgCore.condition == Controllers.OcgCore.Condition.Duel)
                    if (Controllers.OcgCore.Accing)
                        Runtime.Program.instance.ocgcore.GetUI<OcgCoreUI>().OnAcc();
        }
        private void OnWatchAccChange(float value)
        {
            string result = value.ToString();
            ButtonWatchAcc.SetModeText(result.Length > 4 ? result[..4] : result);
            if (Runtime.Program.instance.ocgcore.showing)
                if (Controllers.OcgCore.condition == Controllers.OcgCore.Condition.Watch)
                    if (Controllers.OcgCore.Accing)
                        Runtime.Program.instance.ocgcore.GetUI<OcgCoreUI>().OnAcc();
        }
        private void OnReplayAccChange(float value)
        {
            string result = value.ToString();
            ButtonReplayAcc.SetModeText(result.Length > 4 ? result[..4] : result);
            if (Runtime.Program.instance.ocgcore.showing)
                if (Controllers.OcgCore.condition == Controllers.OcgCore.Condition.Replay)
                    if (Controllers.OcgCore.Accing)
                        Runtime.Program.instance.ocgcore.GetUI<OcgCoreUI>().OnAcc();
        }

        #endregion

        #region AutoAcc

        private void InitializeAutoAcc()
        {
            var config = MDPro3.Config.GetBool("DuelAutoAcc", true);
            ButtonDuelAutoAcc.SetClickEvent(OnDuelAutoAccClick);
            ButtonDuelAutoAcc.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("WatchAutoAcc", true);
            ButtonWatchAutoAcc.SetClickEvent(OnWatchAutoAccClick);
            ButtonWatchAutoAcc.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));

            config = MDPro3.Config.GetBool("ReplayAutoAcc", true);
            ButtonReplayAutoAcc.SetClickEvent(OnReplayAutoAccClick);
            ButtonReplayAutoAcc.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelAutoAccClick()
        {
            var config = MDPro3.Config.GetBool("DuelAutoAcc", true);
            ButtonDuelAutoAcc.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("DuelAutoAcc", !config);
        }
        private void OnWatchAutoAccClick()
        {
            var config = MDPro3.Config.GetBool("WatchAutoAcc", true);
            ButtonWatchAutoAcc.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("WatchAutoAcc", !config);
        }
        private void OnReplayAutoAccClick()
        {
            var config = MDPro3.Config.GetBool("ReplayAutoAcc", true);
            ButtonReplayAutoAcc.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("ReplayAutoAcc", !config);
        }

        #endregion

        #region Timming

        private void InitializeTimming()
        {
            var config = MDPro3.Config.GetBool(MDPro3.Config.LABEL_TIMING, false);
            ButtonTiming.SetClickEvent(OnTimmingClick);
            ButtonTiming.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));
        }
        private void OnTimmingClick()
        {
            var config = MDPro3.Config.GetBool(MDPro3.Config.LABEL_TIMING, false);
            ButtonTiming.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool(MDPro3.Config.LABEL_TIMING, !config);
        }

        #endregion

        #region AutoRPS

        private void InitializeAutoRPS()
        {
            var config = MDPro3.Config.GetBool("AutoRPS", false);
            ButtonAutoRPS.SetClickEvent(OnAutoRPSClick);
            ButtonAutoRPS.SetModeText(MDPro3.InterString.Get(config ? "开" : "关"));
        }
        private void OnAutoRPSClick()
        {
            var config = MDPro3.Config.GetBool("AutoRPS", false);
            ButtonAutoRPS.SetModeText(MDPro3.InterString.Get(config ? "关" : "开"));
            MDPro3.Config.SetBool("AutoRPS", !config);
        }

        #endregion

        #endregion

        #region Port

        public void OnImport()
        {
            if (Runtime.Program.instance.ocgcore.showing)
            {
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("决斗中不能进行此操作。"));
                return;
            }

            Runtime.PortHelper.ImportFiles();
        }
        public void OnImportBG()
        {
            Runtime.PortHelper.ImportBG();
        }
        public void OnExportDeck()
        {
            Runtime.PortHelper.ExportAllDecks();
        }
        public void OnExportReplay()
        {
            Runtime.PortHelper.ExportAllReplays();
        }
        public void OnExportPicture()
        {
            Runtime.PortHelper.ExportAllPictures();
        }
        public void OnClearPicture()
        {
            if (Runtime.Program.instance.ocgcore.showing)
            {
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("决斗中不能进行此操作。"));
                return;
            }

            var selections = new List<string>
            {
                MDPro3.InterString.Get("确定清空"),
                MDPro3.InterString.Get("是否确认删除所有导入的卡图？"),
                MDPro3.InterString.Get("确认"),
                MDPro3.InterString.Get("取消")
            };
            Runtime.UIManager.ShowPopupYesOrNo(selections, () =>
            {
                if (!Directory.Exists(Runtime.Program.PATH_ALT_ART))
                    Directory.CreateDirectory(Runtime.Program.PATH_ALT_ART);
                foreach (var file in Directory.GetFiles(Runtime.Program.PATH_ALT_ART))
                    File.Delete(file);
            }, null);
        }
        public void OnClearArtVideos()
        {
            if (Runtime.Program.instance.ocgcore.showing)
            {
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("决斗中不能进行此操作。"));
                return;
            }

            var selections = new List<string>
            {
                MDPro3.InterString.Get("确定清空"),
                MDPro3.InterString.Get("是否确认删除所有的动态卡图？@n您可以在系统设置中选择关闭动态卡图的显示。"),
                MDPro3.InterString.Get("确认"),
                MDPro3.InterString.Get("取消")
            };
            Runtime.UIManager.ShowPopupYesOrNo(selections, () =>
            {
                if (!Directory.Exists(Runtime.Program.PATH_VIDEO_ART))
                    Directory.CreateDirectory(Runtime.Program.PATH_ALT_ART);
                foreach (var file in Directory.GetFiles(Runtime.Program.PATH_VIDEO_ART))
                    File.Delete(file);
                MDPro3.IO.ResourceManager.ClearCardVideos();
            }, null);
        }
        #endregion

        #region Expansion

        private void InitializeExpansions()
        {
            var config = MDPro3.Config.GetBool("Expansions", true);
            ButtonExpansionSupport.SetModeText(MDPro3.InterString.Get(config ? "是" : "否"));
        }

        public void OnSupportExpansions()
        {
            if (Runtime.Program.instance.ocgcore.showing)
            {
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("决斗中不能更改此选项。"));
                return;
            }

            var config = MDPro3.Config.GetBool("Expansions", true);
            ButtonExpansionSupport.SetModeText(MDPro3.InterString.Get(config ? "否" : "是"));
            MDPro3.Config.SetBool("Expansions", !config);

            Runtime.Program.instance.InitializeForDataChange();
        }

        public void OnClearExpansions()
        {
            if (Runtime.Program.instance.ocgcore.showing)
            {
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("决斗中不能进行此操作。"));
                return;
            }

            var selections = new List<string>
            {
                MDPro3.InterString.Get("确定清空"),
                MDPro3.InterString.Get("是否确认删除所有导入的扩展卡包？"),
                MDPro3.InterString.Get("确认"),
                MDPro3.InterString.Get("取消")
            };
            Runtime.UIManager.ShowPopupYesOrNo(selections, () =>
            {
                MDPro3.IO.FileManager.Dispose();
                if (!Directory.Exists(Runtime.Program.PATH_EXPANSIONS))
                    Directory.CreateDirectory(Runtime.Program.PATH_EXPANSIONS);
                foreach (var file in Directory.GetFiles(Runtime.Program.PATH_EXPANSIONS))
                    File.Delete(file);
                Runtime.Program.instance.InitializeForDataChange();
            }, null);
        }

        public void OnUpdatePrerelease()
        {
            if (Runtime.Program.instance.ocgcore.showing)
            {
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("决斗中不能进行此操作。"));
                return;
            }
            Runtime.Program.instance.setting.UpdatePrerelease();
        }

        public void OnDownloadYPK()
        {
            if (Runtime.Program.instance.ocgcore.showing)
            {
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("决斗中不能进行此操作。"));
                return;
            }

            Runtime.Program.instance.setting.DownloadYPK();
        }

        #endregion

        #region About

        private static string GetAboutText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var result = text.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd();
            if (result.EndsWith("_END_", StringComparison.Ordinal))
                result = result[..^"_END_".Length].TrimEnd();
            return result;
        }

        public void OnAboutGame()
        {
            var address = MDPro3.Utility.Language.UseChinese() ? "AboutGame" : "AboutGame_en";
            var handle = Addressables.LoadAssetAsync<TextAsset>(address);
            handle.Completed += (result) =>
            {
                var selections = new List<string>()
                {
                    MDPro3.InterString.Get("关于游戏"),
                    GetAboutText(result.Result.text)
                };
                if (MDPro3.Utility.Language.UseChinese())
                    Runtime.UIManager.ShowPopupText(selections);
                else
                    Runtime.UIManager.ShowPopupText(selections, TMPro.HorizontalAlignmentOptions.Left);
            };
        }
        public void OnAboutVersion()
        {
            var address = MDPro3.Utility.Language.UseChinese() ? "AboutVersion" : "AboutVersion_en";
            var handle = Addressables.LoadAssetAsync<TextAsset>(address);
            handle.Completed += (result) =>
            {
                var selections = new List<string>()
                {
                    MDPro3.InterString.Get("关于版本号"),
                    GetAboutText(result.Result.text)
                };
                Runtime.UIManager.ShowPopupText(selections, TMPro.HorizontalAlignmentOptions.Left);
            };
        }
        public void OnAboutUpdate()
        {
            var address = MDPro3.Utility.Language.UseChinese() ? "AboutUpdate" : "AboutUpdate_en";
            var handle = Addressables.LoadAssetAsync<TextAsset>(address);
            handle.Completed += (result) =>
            {
                var selections = new List<string>()
                {
                    MDPro3.InterString.Get("关于更新"),
                    GetAboutText(result.Result.text)
                };
                if (MDPro3.Utility.Language.UseChinese())
                    Runtime.UIManager.ShowPopupText(selections);
                else
                    Runtime.UIManager.ShowPopupText(selections, TMPro.HorizontalAlignmentOptions.Left);
            };
        }
        public void OnUpdateContent()
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>("UpdateContent");
            handle.Completed += (result) =>
            {
                var selections = new List<string>()
                {
                    MDPro3.InterString.Get("更新内容"),
                    result.Result.text
                };
                Runtime.UIManager.ShowPopupText(selections, TMPro.HorizontalAlignmentOptions.Left);
            };
        }

        #endregion

        public void OnSurrender()
        {
            Runtime.Program.instance.ocgcore.OnDuelResultConfirmed(true);
        }

        public void OnRetry()
        {
            if (Controllers.OcgCore.condition != Controllers.OcgCore.Condition.Duel || !Controllers.RoomServant.FromSolo)
                return;
            if (!Runtime.Program.instance.solo.CanRetryLastSoloDuel())
                return;

            Runtime.Program.instance.ocgcore.onSurrenderConfirmed = Runtime.Program.instance.solo.QueueRetryLastSoloDuel;
            Runtime.Program.instance.ocgcore.OnDuelResultConfirmed(true);
        }

        public void OnCheckDecks()
        {
            Runtime.Program.instance.setting.OnExit();
            MDPro3.Duel.YGOSharp.Deck deck = Controllers.OcgCore.sideChangedDeck;
            if(deck == null)
            {
                var deckName = MDPro3.Config.GetConfigDeckName();
                var deckPath = Path.Combine(Runtime.Program.PATH_DECK, deckName + Runtime.Program.EXPANSION_YDK);
                deck = new MDPro3.Duel.YGOSharp.Deck(deckPath);
            }

            Runtime.UIManager.ShowDeckBrowserUIForDuel(deck);
        }

    }

    public partial class SROptions
    {
        private UniversalRenderPipelineAsset urpa;
        private Type universalRenderPipelineAssetType;
        private FieldInfo mainLightShadowmapResolutionFieldInfo;
        private FieldInfo supportsSoftShadowsFieldInfo;

        private void InitializeShadowMapFieldInfo()
        {
            urpa = Resources.Load<UniversalRenderPipelineAsset>("Settings/URPAsset");
            universalRenderPipelineAssetType = urpa.GetType();
            mainLightShadowmapResolutionFieldInfo = universalRenderPipelineAssetType.GetField("m_MainLightShadowmapResolution", BindingFlags.Instance | BindingFlags.NonPublic);
            supportsSoftShadowsFieldInfo = universalRenderPipelineAssetType.GetField("m_SoftShadowsSupported", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public ShadowResolution MainLightShadowResolution
        {
            get
            {
                if (mainLightShadowmapResolutionFieldInfo == null)
                {
                    InitializeShadowMapFieldInfo();
                }
                return (ShadowResolution)mainLightShadowmapResolutionFieldInfo.GetValue(urpa);
            }
            set
            {
                if (mainLightShadowmapResolutionFieldInfo == null)
                {
                    InitializeShadowMapFieldInfo();
                }
                mainLightShadowmapResolutionFieldInfo.SetValue(urpa, value);
            }
        }

        public bool SupportsSoftShadows
        {
            get
            {
                if (mainLightShadowmapResolutionFieldInfo == null)
                {
                    InitializeShadowMapFieldInfo();
                }
                return (bool)supportsSoftShadowsFieldInfo.GetValue(urpa);
            }
            set
            {
                if (mainLightShadowmapResolutionFieldInfo == null)
                {
                    InitializeShadowMapFieldInfo();
                }
                supportsSoftShadowsFieldInfo.SetValue(urpa, value);
            }
        }
    }
}
