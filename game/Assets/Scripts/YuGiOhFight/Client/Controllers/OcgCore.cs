using Cysharp.Threading.Tasks;
using DG.Tweening;


using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


using YuGiOhFight.Client.Runtime.Network;


namespace YuGiOhFight.Client.Controllers
{
    public class OcgCore : Servant
    {

        public Duel.DuelBGManager DuelBGManager => messageDispatcher.duel.duelBGManager;
        public List<GameObject> AllGameObjects => messageDispatcher.duel.duelBGManager.allGameObjects;
        public List<UI.PlaceSelector> Places => messageDispatcher.duel.duelBGManager.places;
        public static List<Duel.GameCard> materialCards = new();

        public static bool inPuzzle;
        public static bool isTag;
        public static int playerType;
        public static bool isFirst;
        public static bool isObserver;
        public static int MasterRule;
        public const int MAX_LIFE_POINTS_DISPLAY = 99_999_999;
        public static int life0;
        public static int life1;
        public static int lpLimit = 8000;
        public static int timeLimit = 180;
        public static int turns;
        public static bool myTurn = true;
        public static string name_0 = string.Empty;
        public static string name_0_c = string.Empty;
        public static string name_0_tag = string.Empty;
        public static string name_1 = string.Empty;
        public static string name_1_c = string.Empty;
        public static string name_1_tag = string.Empty;
        public static int cookie_matchKill;
        public static string winReason = string.Empty;
        public static bool duelEnded;
        public static MDPro3.Duel.YGOSharp.DuelPhase duelPhase = MDPro3.Duel.YGOSharp.DuelPhase.Draw;
        public static DuelResult duelResult = DuelResult.DisLink;
        public static Condition condition = Condition.N;
        public static ChainCondition chainCondition = ChainCondition.Smart;

        public static string ES_hint;
        public static int ES_max;
        public static int ES_min;
        public static int ES_level;
        public static bool ES_overFlow;
        public static string ES_selectHint;
        public static int Es_selectMSGHintData;
        public static int Es_selectMSGHintPlayer;
        public static int Es_selectMSGHintType;
        public static List<int> ES_searchCodes = new();
        public static string ES_selectUnselectHint = string.Empty;
        public static bool ES_selectCardFromFieldFirstFlag = false;
        public static int ES_sortSum;
        public static string ES_turnString = string.Empty;

        public static bool surrendered;
        public static bool tagSurrendered;
        public static bool deckReserved;
        public static bool cantCheckGrave;
        public static bool inPendulumSummon;

        public static List<Duel.GameCard> cards = new();
        public static List<Duel.GameCard> tempCards = new();
        public static int chainSolvingIndex;
        public static Duel.GameCard chainSolvingCard;
        public static List<Duel.GameCard> cardsInChain = new();
        public static List<int> codesInChain = new();
        public static List<uint> controllerInChain = new();
        public static List<int> negatedInChain = new();
        public static List<Duel.GameCard> cardsBeTarget = new();
        public static List<Duel.GameCard> cardsInSelection = new();
        public static List<Duel.GameCard> cardsMustBeSelected = new();
        public static List<string> confirmedCards = new();
        public static Duel.GameCard attackingCard;
        public static Duel.GameCard summonCard;
        public static Duel.GameCard lastMoveCard;
        public static Duel.GameCard lastConfirmedCard;

        public static int mySummonCount;
        public static int mySpSummonCount;
        public static int opSummonCount;
        public static int opSpSummonCount;
        public static List<int> myActivated = new();
        public static List<int> opActivated = new();

        public delegate void ResponseHandler(byte[] buffer);
        public static ResponseHandler handler = null;
        public static MDPro3.Duel.YGOSharp.GameMessage currentMessage = MDPro3.Duel.YGOSharp.GameMessage.Waiting;
        public static MDPro3.Duel.YGOSharp.GameMessage lastMessage = MDPro3.Duel.YGOSharp.GameMessage.Waiting;
        public static List<Package> packages = new();
        public static List<Package> allPackages = new();
        public static bool pause;
        public static bool NoMoreWait
        {
            get
            {
                return noMoreWait;
            }
            set
            {
                noMoreWait = value;
                //Debug.Log($"Set noMoreWait: {value}");
            }
        }
        private static bool noMoreWait;

        public static Action endingAction;
        public static Action<int> nextMoveAction;
        public static float nextMoveActionDuration;
        public static Presentation.Elements.ElementObjectManager nextMoveManager;
        public static Action nextEventAction;
        public static Action nextNegateAction;
        public static Action nextNegateAction_Additional;
        public static float nextNegateAction_AdditionalTime;
        public static Presentation.Elements.ElementObjectManager nextNegateAction_AdditionalManager;
        public static Action startCard;
        public Action onSurrenderConfirmed;

        public static Material myProtector;
        public static Material opProtector;

        public static bool needRefreshMyHand = true;
        public static bool needRefreshOpHand = true;
        public static List<Duel.GameCard> myHandCards = new();
        public static List<Duel.GameCard> opHandCards = new();
        public static List<Duel.GameCard> myPreHandCards = new();
        public static List<Duel.GameCard> opPreHandCards = new();

        public static int movingToMyGrave = 0;
        public static int movingToMyExclude = 0;
        public static int movingToOpGrave = 0;
        public static int movingToOpExclude = 0;
        public static uint nextMoveMessageController;

        public static bool nextMoveNeedCode;

        #region Config Params

        public static readonly Vector3 myPosition = new(0, 15, -25);
        public static readonly Vector3 opPosition = new(0, 15, 25);

        #endregion

        #region Hand Control

        private static readonly float handCellX = 30f;
        private static float clickInPosition;
        private static int handCount;
        public static float handOffset;
        public static float lastHandOffset;
        public static bool clickingHandCard;
        public static bool handCardDraged;
        public static bool addingCardToMyHand;
        public static bool cardLeavingMyHand;
        public static bool dragingCard;
        private static bool hideMyHandCard;
        public static bool HideMyHandCard
        {
            get { return hideMyHandCard; }
            set
            {
                hideMyHandCard = value;
                Runtime.Program.instance.ocgcore.RefreshMyHandCardPosition();
            }
        }
        private static bool hideOpHandCard;
        public static bool HideOpHandCard
        {
            get { return hideOpHandCard; }
            set
            {
                hideOpHandCard = value;
                Runtime.Program.instance.ocgcore.RefreshOpHandCardPosition();
            }
        }

        #endregion

        public static UI.DuelButton btnConfirm;
        public static UI.DuelButton btnCancel;

        #region Reference
        [Header("OcgCore")]
        public MeshRenderer greenBackground;
        [HideInInspector] public UI.PopupDuel currentPopup;

        public static bool mycardDuel;
        public static MDPro3.Duel.YGOSharp.Deck sideReference = new();
        public static MDPro3.Duel.YGOSharp.Deck sideChangedDeck;
        public static bool inputMode;
        public static bool Accing;
        public static bool CurrentReplayUseYRP2;
        public static bool CurrentReplayGodView;

        #endregion

        #region Servants

        public override int Depth => -1;
        protected override bool ShowLine => false;

        public override void Initialize()
        {
            base.Initialize();
            Runtime.SystemEvent.OnResolutionChange += RefreshHandCardPositionInstant;
        }

        public static int NormalizeLifePointsForDisplay(int value)
        {
            if (value < 0)
                return 0;
            if (value > MAX_LIFE_POINTS_DISPLAY)
                return MAX_LIFE_POINTS_DISPLAY;
            return value;
        }

        public static string FormatLifePoints(int value)
        {
            return NormalizeLifePointsForDisplay(value).ToString();
        }

        public void LoadDuelButton()
        {
            if(btnConfirm == null)
            {
                btnConfirm = Runtime.ABLoader.LoadMasterDuelGameObject("DuelButton").GetComponent<UI.DuelButton>();
                btnConfirm.response.Add(-4);
                btnConfirm.hint = MDPro3.InterString.Get("确认");
                btnConfirm.type = UI.ButtonType.Decide;
                btnConfirm.Hide();
            }
            if(btnCancel == null)
            {
                btnCancel = Runtime.ABLoader.LoadMasterDuelGameObject("DuelButton").GetComponent<UI.DuelButton>();
                btnCancel.response.Add(-5);
                btnCancel.hint = MDPro3.InterString.Get("取消");
                btnCancel.type = UI.ButtonType.Cancel;
                btnCancel.Hide();
            }
        }

        protected override void ApplyShowArrangement(int preDepth)
        {
            CloseCharaFace();
            servantUI.gameObject.SetActive(true);
            servantUI.Show(true);
            Runtime.Program.instance.ui_.chatPanel.Hide();
            UI.ServantUI.OcgCoreUI ui = servantUI as UI.ServantUI.OcgCoreUI;
            ui.CG.alpha = 0f;
            ui.CG.blocksRaycasts = false;

            // Dispose the preceding duel before starting this entry's asset generation.
            _ = ProcessMessage();
            _ = messageDispatcher.duel.duelBGManager.LoadAssetsAsync();

            returnAction = null;
        }

        protected override void ApplyHideArrangement(int preDepth)
        {
            messageLoopVersion++;
            CloseCharaFace();
            GetUI<UI.ServantUI.OcgCoreUI>()?.DuelErrorLog.Clear();
            _ = messageDispatcher.duel.duelBGManager.ExitDuelAsync();
        }

        public override void OnExit()
        {
            // Local CPU and hand tests own an isolated host. Retire it before EOF
            // and scene changes so late packets cannot reopen the departed duel.
            if (condition == Condition.Duel && (RoomServant.FromSolo || RoomServant.FromHandTest)
                && TcpHelper.joinedAddress == "127.0.0.1" && (MDPro3.Net.YgoServer.ServerRunning() || TcpHelper.tcpClient != null))
            {
                MDPro3.Net.YgoServer.StopServer();
                TcpHelper.DisconnectIntentional();
            }
            base.OnExit();
            CloseConnection();
            CurrentReplayGodView = false;
            GetUI<UI.ServantUI.OcgCoreUI>()?.OnNor();
            sideChangedDeck = null;
        }

        public override void PerFrameFunction()
        {
            if (!showing || UI.Client.FightDuelMenu.BlocksInput || UI.Client.FightOpeningCoin.IsShowing)
                return;

            if (DuelBGManager == null)
                return;

            if (!Services.ReplayVideoExport.Busy && !EventSystem.current.IsPointerOverGameObject()
                && Runtime.UserInput.HoverObject == null
                && Runtime.UserInput.MouseLeftUp)
            {
                GetUI<UI.ServantUI.OcgCoreUI>().CardDescription.Hide();
                GetUI<UI.ServantUI.OcgCoreUI>().CardList.Hide();
            }

            #region Background

            if (DuelBGManager.HoveringField0() && Runtime.UserInput.MouseLeftUp)
                DuelBGManager.TapField0();
            else if (DuelBGManager.HoveringField1() && Runtime.UserInput.MouseLeftUp)
                DuelBGManager.TapField1();
            else if (DuelBGManager.HoveringMate0() && Runtime.UserInput.MouseLeftUp)
                DuelBGManager.TapMate0();
            else if (DuelBGManager.HoveringMate1() && Runtime.UserInput.MouseLeftUp)
                DuelBGManager.TapMate1();
            else
            {
                DuelBGManager.PlayMate0Random();
                DuelBGManager.PlayMate1Random();
            }

            #endregion

            #region HandOffset

            if (GetMyHandCount() > 10)
            {
                if (Runtime.UserInput.MouseLeftDown
                    && Runtime.UserInput.HoverObject != null
                    && Runtime.UserInput.HoverObject.name == "CardModel"
                    && Runtime.UserInput.HoverObject.GetComponent<Duel.GameCardMono>().cookieCard.p.controller == 0
                    && (Runtime.UserInput.HoverObject.GetComponent<Duel.GameCardMono>().cookieCard.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Hand) > 0)
                {
                    clickInPosition = Runtime.UserInput.MousePos.x;
                    clickingHandCard = true;
                    handCount = GetMyHandCount();
                }
                if (clickingHandCard)
                {
                    var currentOffset = lastHandOffset + Runtime.UserInput.MousePos.x - clickInPosition;
                    var currentHandCellX = Runtime.UIManager.ScreenLengthWithScalerX(handCellX);
                    handOffset = currentOffset > (handCount * currentHandCellX) ?
                        handCount * currentHandCellX :
                        Math.Abs(currentOffset) > (handCount * currentHandCellX) ?
                        -(handCount * currentHandCellX) :
                        currentOffset;
                }
                if (!dragingCard && Math.Abs(handOffset - lastHandOffset) > 10)
                    dragingCard = true;

                if (Runtime.UserInput.MouseLeftUp)
                {
                    handCardDraged = false;
                    if (clickingHandCard)
                    {
                        clickingHandCard = false;
                        if (lastHandOffset != handOffset)
                        {
                            handCardDraged = true;
                            lastHandOffset = handOffset;
                        }
                    }
                    if (dragingCard)
                    {
                        dragingCard = false;
                        RefreshHandCardPositionInstant();
                    }
                }
            }
            else
            {
                if (handOffset != 0)
                {
                    handOffset = 0;
                    lastHandOffset = 0;
                    RefreshHandCardPositionInstant();
                }
            }

            #endregion

            #region Hot Key

            if (Services.ReplayVideoExport.Busy) return;

            // Full-screen readers and native auxiliary prompts own their input.
            // Keep the preceding battle/hand animation work running while
            // preventing their cancel/navigation keys reaching a rule popup.
            if (UI.Client.FightDuelPopupSkin.SceneThemeActive
                && (Runtime.UIManager.InputBlocker != null || Runtime.Program.instance.ui_.currentPopupB != null))
                return;

            if (Runtime.UserInput.MouseRightDown || Runtime.UserInput.WasCancelPressed)
            {
                returnAction?.Invoke();
            }
            if (Runtime.UserInput.MouseLeftDown)
            {
                DuelBGManager.HideEquipLine();
                DuelBGManager.HideTargetLines();
            }

            if (inputMode)
                return;

            if (Keyboard.current != null)
            {
                // Timing
                if (Keyboard.current.aKey.wasPressedThisFrame)
                {
                    chainCondition = ChainCondition.All - 1;
                    GetUI<UI.ServantUI.OcgCoreUI>().OnTiming();
                }
                else if (Keyboard.current.sKey.wasPressedThisFrame)
                {
                    chainCondition = ChainCondition.No - 1;
                    GetUI<UI.ServantUI.OcgCoreUI>().OnTiming();
                }
                else if (Keyboard.current.dKey.wasPressedThisFrame)
                {
                    chainCondition = ChainCondition.Smart - 1;
                    GetUI<UI.ServantUI.OcgCoreUI>().OnTiming();
                }

                // Log
                if (Keyboard.current.tabKey.wasPressedThisFrame)
                {
                    GetUI<UI.ServantUI.OcgCoreUI>().OnLog();
                }

                // Green
                if (Keyboard.current.gKey.wasPressedThisFrame)
                {
                    if (greenOn)
                        GreenBackgroundOff();
                    else
                        GreenBackgroundOn();
                }

                if (greenOn)
                {
                    if (Keyboard.current.numpad0Key.wasPressedThisFrame
                        || Keyboard.current.digit0Key.wasPressedThisFrame)
                    {
                        greenBackground.material.color = Color.black;
                    }
                    else if (Keyboard.current.numpad1Key.wasPressedThisFrame
                        || Keyboard.current.digit1Key.wasPressedThisFrame)
                    {
                        greenBackground.material.color = Color.red;
                    }
                    else if (Keyboard.current.numpad2Key.wasPressedThisFrame
                        || Keyboard.current.digit2Key.wasPressedThisFrame)
                    {
                        greenBackground.material.color = new Color(1f, 0.5f, 0f);
                    }
                    else if (Keyboard.current.numpad3Key.wasPressedThisFrame
                        || Keyboard.current.digit3Key.wasPressedThisFrame)
                    {
                        greenBackground.material.color = new Color(1f, 1f, 0f);
                    }
                    else if (Keyboard.current.numpad4Key.wasPressedThisFrame
                        || Keyboard.current.digit4Key.wasPressedThisFrame)
                    {
                        greenBackground.material.color = Color.green;
                    }
                    else if (Keyboard.current.numpad5Key.wasPressedThisFrame
                        || Keyboard.current.digit5Key.wasPressedThisFrame)
                    {
                        greenBackground.material.color = new Color(0f, 1f, 1f);
                    }
                    else if (Keyboard.current.numpad6Key.wasPressedThisFrame
                        || Keyboard.current.digit6Key.wasPressedThisFrame)
                    {
                        greenBackground.material.color = Color.blue;
                    }
                    else if (Keyboard.current.numpad7Key.wasPressedThisFrame
                        || Keyboard.current.digit7Key.wasPressedThisFrame)
                    {
                        greenBackground.material.color = new Color(0.6f, 0f, 1f);
                    }
                    else if (Keyboard.current.numpad8Key.wasPressedThisFrame
                        || Keyboard.current.digit8Key.wasPressedThisFrame)
                    {
                        greenBackground.material.color = Color.gray;
                    }
                    else if (Keyboard.current.numpad9Key.wasPressedThisFrame
                        || Keyboard.current.digit9Key.wasPressedThisFrame)
                    {
                        greenBackground.material.color = Color.white;
                    }
                }
            }

            #endregion

        }

        private bool greenOn;
        public void GreenBackgroundOn()
        {
            greenBackground.gameObject.SetActive(true);
            GetUI<UI.ServantUI.OcgCoreUI>().CG.alpha = 0f;
            GetUI<UI.ServantUI.OcgCoreUI>().CG.blocksRaycasts = false;
            Runtime.CameraManager.DuelOverlay3DPlus();

            greenOn = true;
        }

        public void GreenBackgroundOff()
        {
            greenBackground.gameObject.SetActive(false);
            GetUI<UI.ServantUI.OcgCoreUI>().CG.alpha = 1f;
            GetUI<UI.ServantUI.OcgCoreUI>().CG.blocksRaycasts = true;
            Runtime.CameraManager.DuelOverlay3DMinus();

            greenOn = false;
        }

        public void ToChat()
        {
            Runtime.Program.instance.ui_.chatPanel.Hide();
        }

#endregion

        #region Button Function

        public void OnAnnounceCard(string input)
        {
            var datas = MDPro3.Duel.YGOSharp.CardsManager.AnnounceSearch(input, ES_searchCodes);
            var max = datas.Count;
            if (max > 49)
                max = 40;
            List<Duel.GameCard> cards = new List<Duel.GameCard>();
            for (var i = 0; i < max; i++)
            {
                Duel.GPS p = new Duel.GPS
                {
                    controller = 0,
                    location = (uint)MDPro3.Duel.YGOSharp.CardLocation.Search,
                    sequence = (uint)i,
                    position = 0
                };
                Duel.GameCard card = GCS_Create(p);
                card.SetData(datas[i]);
                cards.Add(card);
            }
            currentPopup.whenQuitDo = () =>
            {
                GetUI<UI.ServantUI.OcgCoreUI>().ShowPopupSelectCard
                (MDPro3.InterString.Get("请选择需要宣言的卡片。"), cards, 1, 1, true, false);
            };
        }

        public void ClearAnnounceCards()
        {
            List<Duel.GameCard> needClean = new List<Duel.GameCard>();
            foreach (Duel.GameCard card in cards)
                if (card.p.location == (uint)MDPro3.Duel.YGOSharp.CardLocation.Search)
                    needClean.Add(card);
            foreach (Duel.GameCard card in needClean)
            {
                cards.Remove(card);
                card.Dispose();
            }
        }

        public void CloseConnection()
        {
            Services.ResponsesDuelSession.Stop();
            if (TcpHelper.tcpClient != null)
            {
                if (TcpHelper.tcpClient.Connected)
                {
                    TcpHelper.tcpClient.Client.Shutdown(0);
                    TcpHelper.tcpClient.Close();
                }
                TcpHelper.tcpClient = null;
            }
        }

        private bool DuelEndNeedExit()
        {
            if (Runtime.Program.instance.room.duelEnded)
                return true;
            if(TcpHelper.tcpClient == null)
                return true;
            if(!TcpHelper.tcpClient.Connected)
                return true;
            if (surrendered)
            {
                if(RoomServant.Mode == 0)
                    return true;
                if (RoomServant.Mode == 1)
                    return false;
                if (RoomServant.Mode == 2)
                    return tagSurrendered;
            }

            return false;
        }

        public void OnDuelResultConfirmed(bool manual = false)
        {
            RoomServant.JoinWithReconnect = false;

            if(condition != Condition.Watch)
            {
                if (DuelEndNeedExit())
                {
                    surrendered = false;
                    RoomServant.NeedSide = false;
                    RoomServant.SideWaitingObserver = false;
                    if (Runtime.Program.instance.currentSubServant != null)
                    {
                        Runtime.Program.instance.currentSubServant.Hide(-1);
                        Runtime.Program.instance.currentSubServant = null;
                    }
                    OnExit();
                    return;
                }
            }

            if (RoomServant.NeedSide)
            {
                RoomServant.NeedSide = false;
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("卡片历史中为您准备了对手上一局使用过的卡。"));
                Runtime.Program.instance.deckEditor.SwitchCondition(DeckEditor.Condition.ChangeSide);
                Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.deckEditor);
                return;
            }

            if (condition == Condition.Watch)
            {
                if (manual)
                {
                    surrendered = false;
                    RoomServant.NeedSide = false;
                    RoomServant.SideWaitingObserver = false;
                    if (Runtime.Program.instance.currentSubServant != null)
                    {
                        Runtime.Program.instance.currentSubServant.Hide(-1);
                        Runtime.Program.instance.currentSubServant = null;
                    }
                    TcpHelper.CtosMessage_LeaveGame();
                    OnExit();
                }
                else
                {
                    if (duelEnded)
                    {
                        if (TcpHelper.tcpClient == null
                            || !TcpHelper.tcpClient.Connected)
                            OnExit();
                        else
                            Hide(0);
                    }
                    else
                    {
                        DuelBGManager.ResetFields();
                    }
                }
                return;
            }

            var selections = new List<string>
            {
                MDPro3.InterString.Get("投降"),
                MDPro3.InterString.Get("您确定要投降吗？"),
                MDPro3.InterString.Get("是"),
                MDPro3.InterString.Get("否")
            };
            Runtime.UIManager.ShowPopupYesOrNo(selections, ActionSurrender, ActionCancelSurrender);
        }

        internal void ConfirmAuxiliaryExit()
        {
            if (condition == Condition.Duel && !Runtime.Program.instance.room.duelEnded)
            {
                RoomServant.JoinWithReconnect = false;
                ActionSurrender();
            }
            else OnDuelResultConfirmed(true);
        }

        private void ActionSurrender()
        {
            onSurrenderConfirmed?.Invoke();
            onSurrenderConfirmed = null;
            surrendered = true;
            if (TcpHelper.tcpClient != null && TcpHelper.tcpClient.Connected)
            {
                TcpHelper.CtosMessage_Surrender();
                Runtime.Program.instance.ExitCurrentServant();
                if (RoomServant.Mode == 2 && !tagSurrendered)
                    Runtime.MessageManager.Cast(MDPro3.InterString.Get("您发起了投降。"));
            }
            else
                OnExit();
        }

        private void ActionCancelSurrender()
        {
            onSurrenderConfirmed = null;
        }

        #endregion

        #region Message

        public Duel.MessageDispatcher messageDispatcher = new();
        private int messageLoopVersion;
        internal int MessageLoopVersion => messageLoopVersion;

        public void AddPackage(Package p)
        {
            TcpHelper.AddRecordLine(p);
            packages.Add(p);
            allPackages.Add(p);
            CheckMessageIsNeedInstanceResponse(p);
        }

        private void CheckMessageIsNeedInstanceResponse(Package p)
        {
            if(p.Function == (int)MDPro3.Duel.YGOSharp.GameMessage.Win)
            {
                messageDispatcher.playerResponed = true;
            }
        }

        public void FlushPackages(List<Package> packs)
        {
            packages.Clear();
            packages = null;
            packages = packs;
            replayPlaybackEndedNotified = false;
            allPackages.Clear();
            foreach (Package p in packages)
                allPackages.Add(p);
        }

        public void SendReturn(byte[] buffer, float delay = 0f)
        {
            _ = SendReturnAsync(buffer, delay);
        }

        private async UniTask SendReturnAsync(byte[] buffer, float delay = 0f)
        {
            await UniTask.WaitForSeconds(delay);
            handler?.Invoke(buffer);
            DuelBGManager.ClearResponse();
            messageDispatcher.playerResponed = true;
        }

        public void OnResend()
        {
            BinaryMaster binaryMaster = new BinaryMaster();
            binaryMaster.writer.Write(-1);
            SendReturn(binaryMaster.Get());
        }

        public void StocMessage_TeammateSurrender()
        {
            if (surrendered)
                return;
            tagSurrendered = true;
            Runtime.MessageManager.Cast(MDPro3.InterString.Get("队友发起了投降。"));
        }

        public void StocMessage_TimeLimit(BinaryReader r)
        {
            _ = TimeConfirmAsync(r);
        }

        public bool needTimeConfirm;

        private async UniTask TimeConfirmAsync(BinaryReader r)
        {
            needTimeConfirm = false;
            DuelBGManager?.PauseTime();
            int player = LocalPlayer(r.ReadByte());
            r.ReadByte();
            int timeLimit = r.ReadInt16();

            if(player == 0)
                await UniTask.WaitUntil(() => needTimeConfirm);
            TcpHelper.CtosMessage_TimeConfirm();
            DuelBGManager?.SetTimeLimit(player, timeLimit);
        }

        public void StocMessage_Error(string error)
        {
            _ = ShowErrorMessageAsync(error);
        }

        private async UniTask ShowErrorMessageAsync(string error)
        {
            var errorLoopVersion = messageLoopVersion;
            await UniTask.SwitchToMainThread();
            bool CurrentErrorLoop() => this != null && showing && errorLoopVersion == messageLoopVersion;
            await UniTask.WaitWhile(() => CurrentErrorLoop() && servantUI == null);
            if (!CurrentErrorLoop())
                return;
            GetUI<UI.ServantUI.OcgCoreUI>().DuelErrorLog.Show(error);
        }

        public static bool ShouldShowPlayerMessages()
        {
            if (condition == Condition.Duel && !MDPro3.Config.GetBool("DuelPlayerMessage", true))
                return false;
            if (condition == Condition.Watch && !MDPro3.Config.GetBool("WatchPlayerMessage", true))
                return false;
            if (condition == Condition.Replay && !MDPro3.Config.GetBool("ReplayPlayerMessage", true))
                return false;
            return true;
        }

        public static bool ShouldShowSystemMessages()
        {
            if (condition == Condition.Duel && !MDPro3.Config.GetBool("DuelSystemMessage", true))
                return false;
            if (condition == Condition.Watch && !MDPro3.Config.GetBool("WatchSystemMessage", true))
                return false;
            if (condition == Condition.Replay && !MDPro3.Config.GetBool("ReplaySystemMessage", true))
                return false;
            return true;
        }

        public bool GetMessageConfig(int player)
        {
            if (player < 4 || player == 7)
            {
                if (!ShouldShowPlayerMessages())
                    return false;
            }
            else
            {
                if (!ShouldShowSystemMessages())
                    return false;
            }
            return true;
        }

        public void ForceMSquit()
        {
            Package p = new Package
            {
                Function = (int)MDPro3.Duel.YGOSharp.GameMessage.sibyl_quit
            };
            packages.Add(p);
        }

        public bool InIgnoranceReplay()
        {
            return condition != Condition.Duel;
        }

        public static Package GetNextPackage()
        {
            int target = 1;
            while (packages.Count > target)
            {
                if (packages[target].Function != (int)MDPro3.Duel.YGOSharp.GameMessage.UpdateData
                    && packages[target].Function != (int)MDPro3.Duel.YGOSharp.GameMessage.UpdateCard)
                    return packages[target];
                target++;
            }
            return null;
        }

        public static bool NextMessageIs(MDPro3.Duel.YGOSharp.GameMessage message)
        {
            Package p = GetNextPackage();
            if(p == null) return false;
            return p.Function == (int)message;
        }

        public static MDPro3.Duel.YGOSharp.CardLocation NextMessageIsMovingToLocation()
        {
            Package p = GetNextPackage();
            if (p == null)
                return MDPro3.Duel.YGOSharp.CardLocation.Unknown;

            if (p.Function == (int)MDPro3.Duel.YGOSharp.GameMessage.Move)
            {
                var r = p.Data.reader;
                r.BaseStream.Seek(0, 0);
                r.ReadInt32();
                r.ReadGPS();
                Duel.GPS to = r.ReadGPS();
                nextMoveMessageController = to.controller;
                return (MDPro3.Duel.YGOSharp.CardLocation)to.location;
            }
            return MDPro3.Duel.YGOSharp.CardLocation.Unknown;
        }

        public static MDPro3.Duel.YGOSharp.CardLocation NextMessageIsMovingFromLocation()
        {
            Package p = GetNextPackage();
            if (p == null)
                return MDPro3.Duel.YGOSharp.CardLocation.Unknown;

            if (p.Function == (int)MDPro3.Duel.YGOSharp.GameMessage.Move)
            {
                var r = p.Data.reader;
                r.BaseStream.Seek(0, 0);
                r.ReadInt32();
                Duel.GPS from = r.ReadGPS();
                return (MDPro3.Duel.YGOSharp.CardLocation)from.location;
            }
            return MDPro3.Duel.YGOSharp.CardLocation.Unknown;
        }


        public static bool NextMessageIsMovingFrom(MDPro3.Duel.YGOSharp.CardLocation location)
        {
            Package p = GetNextPackage();
            if (p == null)
                return false;

            if (p.Function == (int)MDPro3.Duel.YGOSharp.GameMessage.Move)
            {
                var r = p.Data.reader;
                r.BaseStream.Seek(0, 0);
                r.ReadInt32();
                Duel.GPS from = r.ReadGPS();
                if ((from.location & (uint)location) > 0)
                    return true;
            }
            return false;

        }

        public static bool NextMessageIsMovingTo(MDPro3.Duel.YGOSharp.CardLocation location, uint player)
        {
            Package p = GetNextPackage();
            if (p == null)
                return false;

            if (p.Function == (int)MDPro3.Duel.YGOSharp.GameMessage.Move)
            {
                var r = p.Data.reader;
                r.BaseStream.Seek(0, 0);
                r.ReadInt32();
                r.ReadGPS();
                Duel.GPS to = r.ReadGPS();
                if (player == to.controller && (to.location & (uint)location) > 0)
                    return true;
            }
            return false;
        }

        public static bool NextMessageIsMovingToGrave(uint player)
        {
            var count = player == 0 ? movingToMyGrave : movingToOpGrave;
            if (count >= 4)
                return false;
            if (NextMessageIsMovingTo(MDPro3.Duel.YGOSharp.CardLocation.Grave, player))
                return true;
            return false;
        }

        public static bool NextMessageIsMovingToExclude(uint player)
        {
            var count = player == 0 ? movingToMyExclude : movingToOpExclude;
            if (count >= 4)
                return false;
            if (NextMessageIsMovingTo(MDPro3.Duel.YGOSharp.CardLocation.Removed, player))
                return true;
            return false;
        }

        public int SyncNextMoveState(Duel.GPS from, Duel.GPS to)
        {
            if (!from.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Grave)
                && to.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Grave))
            {
                MDPro3.Duel.YGOSharp.CardLocation location = NextMessageIsMovingToLocation();
                if(location == MDPro3.Duel.YGOSharp.CardLocation.Grave)
                    return NextMessageIsMovingToGrave(nextMoveMessageController) ? 1 : 0;
                else if (location == MDPro3.Duel.YGOSharp.CardLocation.Removed)
                    return NextMessageIsMovingToExclude(nextMoveMessageController) ? 1 : 0;
                else
                    return 0;
            }
            else if (!from.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Removed)
                && to.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Removed))
            {
                MDPro3.Duel.YGOSharp.CardLocation location = NextMessageIsMovingToLocation();
                if (location == MDPro3.Duel.YGOSharp.CardLocation.Removed)
                    return NextMessageIsMovingToExclude(nextMoveMessageController) ? 1 : 0;
                else if (location == MDPro3.Duel.YGOSharp.CardLocation.Grave)
                    return NextMessageIsMovingToGrave(nextMoveMessageController) ? 1 : 0;
                else
                    return 0;
            }
            else if(!from.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Deck)
                && to.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Deck))
            {
                MDPro3.Duel.YGOSharp.CardLocation toLocation = NextMessageIsMovingToLocation();
                MDPro3.Duel.YGOSharp.CardLocation fromLocation = NextMessageIsMovingFromLocation();
                if (from.location == (uint)fromLocation && to.location == (uint)toLocation)
                    return 2;
                else
                    return 0;
            }

            return 0;
        }

        private async UniTask ProcessMessage()
        {
            var loopVersion = ++messageLoopVersion;
            bool CurrentLoop() => this != null && showing && loopVersion == messageLoopVersion;
            Services.DeckAcademyDeck pacingDeck = null;
            List<Package> pacingPackages = null;
            var pacingFrame = -1;
            var pacingBoundary = -1;
            var pacingDeadline = 0f;
            messageDispatcher.Dispose();
            replayPlaybackEndedNotified = false;

            try
            {
                while (CurrentLoop())
                {
                    if(!messageDispatcher.duel.duelBGManager.loaded)
                    {
                        await UniTask.Yield();
                        continue;
                    }

                    if (Services.DeckAcademy.HasActiveReplay
                        && Services.DeckAcademy.HasReplayTimeline(Services.DeckAcademy.ActiveDeck, allPackages.Count))
                    {
                        Services.DeckAcademyDeck entry = Services.DeckAcademy.ActiveDeck;
                        var frame = allPackages.Count - packages.Count - 1;
                        if (!ReferenceEquals(pacingDeck, entry)
                            || !ReferenceEquals(pacingPackages, packages) || frame < pacingFrame)
                        {
                            pacingDeck = entry;
                            pacingPackages = packages;
                            pacingBoundary = -1;
                            pacingDeadline = 0f;
                        }
                        pacingFrame = frame;
                        if (frame >= entry.steps[0].replay_frame.Value)
                        {
                            var page = Services.DeckAcademy.ReplayGuidePage(entry, frame);
                            var boundary = entry.steps[page].replay_frame.Value;
                            if (boundary != pacingBoundary)
                            {
                                pacingBoundary = boundary;
                                pacingDeadline = Services.ReplayVideoExport.PlaybackClock + 3f;
                            }
                        }
                        // Hold the actual board and guide at each recorded
                        // boundary. Replay acceleration does not shorten this.
                        await UniTask.WaitWhile(() => CurrentLoop() && Services.DeckAcademy.HasActiveReplay
                            && !Services.DeckAcademy.ReplayStepRequested && ReferenceEquals(pacingPackages, packages)
                            && frame == allPackages.Count - packages.Count - 1
                            && Services.ReplayVideoExport.PlaybackClock < pacingDeadline);
                        if (!CurrentLoop())
                            break;
                        if (!ReferenceEquals(pacingPackages, packages)
                            || frame != allPackages.Count - packages.Count - 1)
                            continue;
                    }
                    else
                    {
                        pacingDeck = null;
                        pacingPackages = null;
                        pacingFrame = -1;
                        pacingBoundary = -1;
                        pacingDeadline = 0f;
                    }

                    await UniTask.WaitWhile(() => CurrentLoop() && pause);
                    if (!CurrentLoop())
                        break;
                    if (pacingDeck != null && (!ReferenceEquals(pacingPackages, packages)
                        || pacingFrame != allPackages.Count - packages.Count - 1))
                        continue;

                    if (packages.Count == 0)
                    {
                        await UniTask.Yield();
                        continue;
                    }

                    NoMoreWait = false;

                    Package currentPackage = packages[0];
                    currentMessage = (MDPro3.Duel.YGOSharp.GameMessage)currentPackage.Function;

#if UNITY_EDITOR
                    if (Program.instance.debugGameMessage)
                    {
                        if (currentMessage != GameMessage.UpdateData)
                            Debug.Log($"GameMessage: {currentMessage}");
                    }
#endif
                    try
                    {
                        if (condition == Condition.Replay)
                            Services.ReplayVideoExport.PacketStart((int)currentMessage, allPackages.Count - packages.Count, allPackages.Count);
                        var messageTask = messageDispatcher.Process(packages[0]);
                        var breakTask = UniTask.WaitUntil(() => NoMoreWait);
                        await UniTask.WhenAny(messageTask, breakTask);
                    }
                    catch (Exception e) { Debug.Log(e); }

                    if (!CurrentLoop())
                        break;

                    // Initialization must create the fields and HUD before
                    // capture can begin. Hold subsequent replay packets at
                    // this boundary so loading speed cannot hide first actions.
                    if (condition == Condition.Replay && Services.ReplayVideoExport.Busy
                        && !Services.ReplayVideoExport.Capturing
                        && (currentMessage == MDPro3.Duel.YGOSharp.GameMessage.Start
                            || currentMessage == MDPro3.Duel.YGOSharp.GameMessage.ReloadField
                            || UI.Client.FightStartupPresentation.NativeFrameReady(this)))
                    {
                        await UniTask.WaitWhile(() => CurrentLoop() && Services.ReplayVideoExport.Busy
                            && !Services.ReplayVideoExport.Capturing);
                        if (!CurrentLoop()) break;
                    }

                    lastMessage = currentMessage;
                    if (packages.Count == 0)
                    {
                        NotifyReplayPlaybackEnded();
                        break;
                    }
                    packages.RemoveAt(0);
                    var deferAcademyCompletion = Services.DeckAcademy.HasActiveReplay && packages.Count == 0
                        && !Services.DeckAcademy.ReplayStepRequested
                        && Services.DeckAcademy.HasReplayTimeline(Services.DeckAcademy.ActiveDeck, allPackages.Count)
                        && Services.DeckAcademy.ActiveDeck.steps[Services.DeckAcademy.ActiveDeck.steps.Length - 1].replay_frame.Value
                            == allPackages.Count - 1;
                    if (condition == Condition.Replay)
                        Services.ReplayVideoExport.Packet((int)currentMessage, allPackages.Count - packages.Count, allPackages.Count);
                    if (condition == Condition.Replay)
                        Services.DeckAcademy.NotifyReplayPacketProcessed((int)currentMessage,
                            allPackages.Count - packages.Count, allPackages.Count, deferAcademyCompletion);

                    if (condition == Condition.Replay && packages.Count == 0)
                    {
                        if (deferAcademyCompletion)
                        {
                            var completionDeadline = Services.ReplayVideoExport.PlaybackClock + 3f;
                            await UniTask.WaitWhile(() => CurrentLoop() && Services.DeckAcademy.HasActiveReplay
                                && packages.Count == 0 && !Services.DeckAcademy.ReplayStepRequested
                                && Services.ReplayVideoExport.PlaybackClock < completionDeadline);
                            await UniTask.WaitWhile(() => CurrentLoop() && packages.Count == 0 && pause);
                            if (!CurrentLoop())
                                break;
                            if (packages.Count > 0)
                                continue;
                            if (Services.DeckAcademy.ReplayStepRequested)
                                GetUI<UI.ServantUI.OcgCoreUI>().OnStop();
                            Services.DeckAcademy.CompleteReplay();
                        }
                        NotifyReplayPlaybackEnded();
                        break;
                    }
                }
            }
            catch (Exception e) { Debug.Log(e); }
        }

        private bool replayPlaybackEndedNotified;

        public void NotifyReplayPlaybackEnded()
        {
            if (condition != Condition.Replay || replayPlaybackEndedNotified)
                return;

            replayPlaybackEndedNotified = true;
            Services.ReplayVideoExport.NotifyEnded();
            Runtime.MessageManager.Cast(MDPro3.InterString.Get("回放播放结束。"));
        }

        #endregion

        #region PracticalizeTools

        public static int LocalPlayer(int p)
        {
            if (p == 0 || p == 1)
            {
                if (isFirst)
                    return p;
                return 1 - p;
            }
            return p;
        }

        public static int[] GetSelectLevelSum(List<Duel.GameCard> cards)
        {
            var sum1 = 0;
            foreach (Duel.GameCard card in cards)
                sum1 += card.levelForSelect_1;
            var sum2 = 0;
            foreach (Duel.GameCard card in cards)
                sum2 += card.levelForSelect_2;
            return new int[] { sum1, sum2 };
        }

        public static bool CheckSelectableInSum(List<Duel.GameCard> cards, Duel.GameCard card, List<Duel.GameCard> selectedCards, int max)
        {
            if (selectedCards.Count >= max)
                return false;
            bool returnValue = false;
            var sum = GetSelectLevelSum(selectedCards);
            if (sum[0] + card.levelForSelect_1 == ES_level || sum[1] + card.levelForSelect_2 == ES_level)
                return true;
            if (sum[0] + card.levelForSelect_1 > ES_level || sum[1] + card.levelForSelect_2 > ES_level)
                return false;

            var newSelectedCards = new List<Duel.GameCard>(selectedCards) { card };
            foreach (Duel.GameCard c in cards)
                if (!newSelectedCards.Contains(c))
                {
                    returnValue = CheckSelectableInSum(cards, c, newSelectedCards, max);
                    if (returnValue)
                        return true;
                }
            return returnValue;
        }

        public static bool TypeMatchReason(int type, int reason)
        {
            if ((type & (uint)MDPro3.Duel.YGOSharp.CardType.Ritual) > 0 && (reason & (uint)MDPro3.Duel.YGOSharp.CardReason.Ritual) > 0)
                return true;
            if ((type & (uint)MDPro3.Duel.YGOSharp.CardType.Fusion) > 0 && (reason & (uint)MDPro3.Duel.YGOSharp.CardReason.Fusion) > 0)
                return true;
            if ((type & (uint)MDPro3.Duel.YGOSharp.CardType.Synchro) > 0 && (reason & (uint)MDPro3.Duel.YGOSharp.CardReason.Synchro) > 0)
                return true;
            if ((type & (uint)MDPro3.Duel.YGOSharp.CardType.Xyz) > 0 && (reason & (uint)MDPro3.Duel.YGOSharp.CardReason.Xyz) > 0)
                return true;
            if ((type & (uint)MDPro3.Duel.YGOSharp.CardType.Link) > 0 && (reason & (uint)MDPro3.Duel.YGOSharp.CardReason.Link) > 0)
                return true;
            return false;
        }

        public Duel.GameCard GCS_Create(Duel.GPS p, bool temp = false)
        {
            Duel.GameCard c = Runtime.Program.instance.container_3D.gameObject.AddComponent<Duel.GameCard>();
            c.p = p;
            cards.Add(c);

            if (temp)
                tempCards.Add(c);
            return c;
        }

        public Duel.GameCard GCS_Get(Duel.GPS p)
        {
            Duel.GameCard c = null;
            if ((p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Overlay) > 0)
            {
                for (var i = 0; i < cards.Count; i++)
                    if (cards[i].p.location == p.location)
                        if (cards[i].p.controller == p.controller)
                            if (cards[i].p.sequence == p.sequence)
                                if (cards[i].p.position == p.position)
                                {
                                    c = cards[i];
                                    break;
                                }
            }
            else
            {
                for (var i = 0; i < cards.Count; i++)
                    if (cards[i].p.location == p.location)
                        if (cards[i].p.controller == p.controller)
                            if (cards[i].p.sequence == p.sequence)
                            {
                                c = cards[i];
                                break;
                            }
            }

            if (p.location == 0) c = null;
            return c;
        }

        public List<Duel.GameCard> GCS_GetLocationCards(int controller, int location)
        {
            var cardsInLocation = new List<Duel.GameCard>();
            for (var i = 0; i < cards.Count; i++)
                if (!tempCards.Contains(cards[i]))
                    if (cards[i].p.location == location)
                        if (cards[i].p.controller == controller)
                            cardsInLocation.Add(cards[i]);
            return cardsInLocation;
        }

        public List<Duel.GameCard> GCS_GetOverlays(Duel.GameCard c)
        {
            var overlays = new List<Duel.GameCard>();
            if (c != null)
                if ((c.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Overlay) == 0)
                    for (var i = 0; i < cards.Count; i++)
                        if ((cards[i].p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Overlay) > 0)
                            if (cards[i].p.controller == c.p.controller)
                                if ((cards[i].p.location | (uint)MDPro3.Duel.YGOSharp.CardLocation.Overlay) ==
                                    (c.p.location | (uint)MDPro3.Duel.YGOSharp.CardLocation.Overlay))
                                    if (cards[i].p.sequence == c.p.sequence)
                                        overlays.Add(cards[i]);
            return overlays;
        }

        public void GCS_CreateBundle(int count, int controller, MDPro3.Duel.YGOSharp.CardLocation location)
        {
            for (var i = 0; i < count; i++)
            {
                GCS_Create(new Duel.GPS
                {
                    controller = (uint)controller,
                    location = (uint)location,
                    position = (int)MDPro3.Duel.YGOSharp.CardPosition.FaceDownAttack,
                    sequence = (uint)i
                });
            }
        }

        public List<Duel.GameCard> GCS_ResizeBundle(int count, int player, MDPro3.Duel.YGOSharp.CardLocation location)
        {
            var cardBow = new List<Duel.GameCard>();
            var waterOutOfBow = new List<Duel.GameCard>();
            for (var i = 0; i < cards.Count; i++)
                if ((cards[i].p.location & (uint)location) > 0)
                    if (cards[i].p.controller == player)
                    {
                        if (cardBow.Count < count)
                            cardBow.Add(cards[i]);
                        else
                            waterOutOfBow.Add(cards[i]);
                    }
            foreach (Duel.GameCard card in waterOutOfBow)
            {
                cards.Remove(card);
                if ((card.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Hand) > 0)
                    card.AnimationShuffle(0.15f);
                else
                    card.Dispose();
            }
            while (cardBow.Count < count)
            {
                Duel.GameCard card = GCS_Create(new Duel.GPS
                {
                    controller = (uint)player,
                    location = (uint)location,
                    position = (int)MDPro3.Duel.YGOSharp.CardPosition.FaceDownAttack,
                    sequence = (uint)cardBow.Count
                });
                cardBow.Add(card);
            }
            foreach (Duel.GameCard card in cardBow)
            {
                card.EraseData();
                card.p.position = (int)MDPro3.Duel.YGOSharp.CardPosition.FaceDownAttack;
            }
            return cardBow;
        }

        public void ArrangeCards()
        {
            //sort
            cards.Sort((left, right) =>
            {
                var a = 1;
                if (left.p.controller > right.p.controller)
                {
                    a = 1;
                }
                else if (left.p.controller < right.p.controller)
                {
                    a = -1;
                }
                else
                {
                    if (left.p.location == (uint)MDPro3.Duel.YGOSharp.CardLocation.Hand && right.p.location != (uint)MDPro3.Duel.YGOSharp.CardLocation.Hand)
                    {
                        a = -1;
                    }
                    else if (left.p.location != (uint)MDPro3.Duel.YGOSharp.CardLocation.Hand && right.p.location == (uint)MDPro3.Duel.YGOSharp.CardLocation.Hand)
                    {
                        a = 1;
                    }
                    else
                    {
                        if ((left.p.location | (uint)MDPro3.Duel.YGOSharp.CardLocation.Overlay) >
                            (right.p.location | (uint)MDPro3.Duel.YGOSharp.CardLocation.Overlay))
                        {
                            a = -1;
                        }
                        else if ((left.p.location | (uint)MDPro3.Duel.YGOSharp.CardLocation.Overlay) <
                                 (right.p.location | (uint)MDPro3.Duel.YGOSharp.CardLocation.Overlay))
                        {
                            a = 1;
                        }
                        else
                        {
                            if (left.p.sequence > right.p.sequence)
                            {
                                a = 1;
                            }
                            else if (left.p.sequence < right.p.sequence)
                            {
                                a = -1;
                            }
                            else
                            {
                                if ((left.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Overlay) >
                                    (right.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Overlay))
                                {
                                    a = -1;
                                }
                                else if ((left.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Overlay) <
                                         (right.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Overlay))
                                {
                                    a = 1;
                                }
                                else
                                {
                                    if (left.p.position > right.p.position)
                                        a = 1;
                                    else if (left.p.position < right.p.position) a = -1;
                                }
                            }
                        }
                    }
                }
                return a;
            });

            /////rebuild
            uint preController = 9999;
            uint preLocation = 9999;
            uint preSequence = 9999;

            uint sequenceWriter = 0;
            var positionWriter = 0;

            for (var i = 0; i < cards.Count; i++)
                if (cards[i])
                {
                    if (preController != cards[i].p.controller) sequenceWriter = 0;
                    if ((preLocation | (uint)MDPro3.Duel.YGOSharp.CardLocation.Overlay) != (cards[i].p.location | (uint)MDPro3.Duel.YGOSharp.CardLocation.Overlay))
                        sequenceWriter = 0;
                    if (preSequence != cards[i].p.sequence) positionWriter = 0;

                    if ((cards[i].p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone) == 0)
                        if ((cards[i].p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.SpellZone) == 0)
                            cards[i].p.sequence = sequenceWriter;


                    if ((cards[i].p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Overlay) > 0)
                    {
                        cards[i].p.position = positionWriter;
                        positionWriter++;
                    }
                    else
                    {
                        sequenceWriter++;
                    }

                    preController = cards[i].p.controller;
                    preLocation = cards[i].p.location;
                    preSequence = cards[i].p.sequence;
                }
        }

        public int GetMyHandCount()
        {
            if (needRefreshMyHand)
            {
                myHandCards = new List<Duel.GameCard>(myPreHandCards);
                foreach (Duel.GameCard card in cards)
                    if (card.p.controller == 0)
                        if ((card.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Hand) > 0)
                            if (!myHandCards.Contains(card))
                                myHandCards.Add(card);
                needRefreshMyHand = false;
            }
            return myHandCards.Count;
        }

        public int GetOpHandCount()
        {
            if (needRefreshOpHand)
            {
                opHandCards = new List<Duel.GameCard>(opPreHandCards);
                foreach (Duel.GameCard card in cards)
                    if (card.p.controller != 0)
                        if ((card.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Hand) > 0)
                            if (!opHandCards.Contains(card))
                                opHandCards.Add(card);
                needRefreshOpHand = false;
            }
            return opHandCards.Count;
        }

        public int GetLocationCardCount(MDPro3.Duel.YGOSharp.CardLocation location, uint controller)
        {
            int count = 0;
            foreach (Duel.GameCard card in cards)
                if ((card.p.location & (uint)location) > 0 && card.p.controller == controller)
                    count++;
            return count;
        }

        public Package GetNamePacket()
        {
            Package p__ = new Package
            {
                Function = (int)MDPro3.Duel.YGOSharp.GameMessage.sibyl_name,
                Data = new BinaryMaster()
            };
            p__.Data.writer.WriteUnicode(name_0, 50);
            p__.Data.writer.WriteUnicode(name_0_tag, 50);
            p__.Data.writer.WriteUnicode(name_0_c != "" ? name_0_c : name_0, 50);
            p__.Data.writer.WriteUnicode(name_1, 50);
            p__.Data.writer.WriteUnicode(name_1_tag, 50);
            p__.Data.writer.WriteUnicode(name_1_c != "" ? name_1_c : name_1, 50);
            p__.Data.writer.Write(MasterRule);
            Services.ReplayAppearance.Write(p__.Data.writer);
            return p__;
        }

        public bool GetAutoInfo()
        {
            if (condition == Condition.Duel
                && !MDPro3.Config.GetBool("DuelAutoInfo", true))
                return false;
            if (condition == Condition.Watch
                && !MDPro3.Config.GetBool("WatchAutoInfo", true))
                return false;
            if (condition == Condition.Replay
                && !MDPro3.Config.GetBool("ReplayAutoInfo", true))
                return false;

            return true;
        }

        public void RefreshAllCardsLabel()
        {
            if (!showing)
                return;
            foreach (Duel.GameCard card in cards)
            {
                card.RefreshLabel();
            }
        }

        public bool CurrentChainDisabled(int currentChain)
        {
            for (int i = 0; i < packages.Count; i++)
            {
                if ((MDPro3.Duel.YGOSharp.GameMessage)packages[i].Function == MDPro3.Duel.YGOSharp.GameMessage.ChainDisabled)
                {
                    var r = packages[i].Data.reader;
                    r.BaseStream.Seek(0, 0);
                    if (r.ReadByte() == currentChain)
                        return true;
                }
                if ((MDPro3.Duel.YGOSharp.GameMessage)packages[i].Function == MDPro3.Duel.YGOSharp.GameMessage.ChainSolved)
                    return false;
            }
            return false;
        }

        public int GetNextConfirmedCardCode()
        {
            for (int i = 0; i < packages.Count; i++)
            {
                if ((MDPro3.Duel.YGOSharp.GameMessage)packages[i].Function == MDPro3.Duel.YGOSharp.GameMessage.ConfirmCards)
                {
                    var reader = packages[i].Data.reader;
                    reader.BaseStream.Seek(0, 0);
                    reader.ReadByte();
                    if (condition != Condition.Replay || CurrentReplayUseYRP2)
                        reader.ReadByte();
                    reader.ReadByte();

                    return reader.ReadInt32();
                }
            }
            return 0;
        }

        public int GetUpdateDataIdByGameCard(Duel.GameCard card)
        {
            for (int i = 0; i < packages.Count; i++)
            {
                if ((MDPro3.Duel.YGOSharp.GameMessage)packages[i].Function == MDPro3.Duel.YGOSharp.GameMessage.UpdateData)
                {
                    var reader = packages[i].Data.reader;
                    reader.BaseStream.Seek(0, 0);
                    var player = LocalPlayer(reader.ReadChar());
                    var location = reader.ReadChar();
                    if (player != card.p.controller)
                        continue;
                    if((location & card.p.location) == 0)
                        continue;
                    while (true)
                    {
                        var len = reader.ReadInt32();
                        if (len == 4) continue;
                        var pos = reader.BaseStream.Position;

                        var flag = reader.ReadInt32();
                        var code = 0;
                        if((flag & (int)MDPro3.Duel.YGOSharp.Query.Code) != 0)
                            code = reader.ReadInt32();
                        if ((flag & (int)MDPro3.Duel.YGOSharp.Query.Position) != 0)
                        {
                            Duel.GPS gps = reader.ReadGPS();
                            Duel.GameCard cardToRefresh = Runtime.Program.instance.ocgcore.GCS_Get(gps);
                            if (cardToRefresh != null && cardToRefresh == card)
                                return code;
                            else
                            {
                                reader.BaseStream.Position = pos + len - 4;
                                continue;
                            }
                        }
                    }
                }
            }

            return 0;
        }

        #endregion

        #region Practicalize

        public void Chat(int player, string content)
        {
            if (!GetMessageConfig(player))
                return;
            if (player == 7 || player < 4)
                Runtime.MessageManager.Cast(UI.ChatPanel.GetPlayerName(player) + ": " + content);
            else
                Runtime.MessageManager.Cast(content);
        }


        public static string lastDuelLog;
        public static void PrintDuelLog(string content)
        {
            lastDuelLog = content;
            if (!ShouldShowSystemMessages())
                return;
            Runtime.MessageManager.Cast(content);
        }

        public void SetFace()
        {
            // A name/tag/duel change retires delayed expressions and face loads
            // from the preceding presentation, even when both characters match.
            CloseCharaFace();
            if (condition == Condition.Duel)
            {
                var selfType = RoomServant.SelfType;
                if (GetUI<UI.ServantUI.OcgCoreUI>().TextPlayer0Name.text == name_0)
                {
                    if (isTag)
                    {
                        if (selfType == 0 || selfType == 2)
                        {
                            GetUI<UI.ServantUI.OcgCoreUI>().AvatarPlayer0.material = Appearance.duelFrameMat0;
                            SetFaceWhenCharaOff(Appearance.duelFace0, 0);
                        }
                        else
                        {
                            GetUI<UI.ServantUI.OcgCoreUI>().AvatarPlayer0.material = Appearance.duelFrameMat0Tag;
                            SetFaceWhenCharaOff(Appearance.duelFace0Tag, 0);
                        }
                    }
                    else
                    {
                        GetUI<UI.ServantUI.OcgCoreUI>().AvatarPlayer0.material = Appearance.duelFrameMat0;
                        SetFaceWhenCharaOff(Appearance.duelFace0, 0);
                    }
                }
                else
                {
                    if (selfType == 0 || selfType == 2)
                    {
                        GetUI<UI.ServantUI.OcgCoreUI>().AvatarPlayer0.material = Appearance.duelFrameMat0Tag;
                        SetFaceWhenCharaOff(Appearance.duelFace0Tag, 0);
                    }
                    else
                    {
                        GetUI<UI.ServantUI.OcgCoreUI>().AvatarPlayer0.material = Appearance.duelFrameMat0;
                        SetFaceWhenCharaOff(Appearance.duelFace0, 0);
                    }
                }
                if (GetUI<UI.ServantUI.OcgCoreUI>().TextPlayer1Name.text == name_1)
                {
                    GetUI<UI.ServantUI.OcgCoreUI>().AvatarPlayer1.material = Appearance.duelFrameMat1;
                    SetFaceWhenCharaOff(Appearance.duelFace1, 1);
                }
                else
                {
                    GetUI<UI.ServantUI.OcgCoreUI>().AvatarPlayer1.material = Appearance.duelFrameMat1Tag;
                    SetFaceWhenCharaOff(Appearance.duelFace1Tag, 1);
                }

                _ = ApplyOnlineOpponentFaceAsync();
            }
            else if (condition == Condition.Watch)
            {
                if (GetUI<UI.ServantUI.OcgCoreUI>().TextPlayer0Name.text == name_0)
                {
                    GetUI<UI.ServantUI.OcgCoreUI>().AvatarPlayer0.material = Appearance.watchFrameMat0;
                    SetFaceWhenCharaOff(Appearance.watchFace0, 0);
                }
                else
                {
                    GetUI<UI.ServantUI.OcgCoreUI>().AvatarPlayer0.material = Appearance.watchFrameMat0Tag;
                    SetFaceWhenCharaOff(Appearance.watchFace0Tag, 0);
                }
                if (GetUI<UI.ServantUI.OcgCoreUI>().TextPlayer1Name.text == name_1)
                {
                    GetUI<UI.ServantUI.OcgCoreUI>().AvatarPlayer1.material = Appearance.watchFrameMat1;
                    SetFaceWhenCharaOff(Appearance.watchFace1, 1);
                }
                else
                {
                    GetUI<UI.ServantUI.OcgCoreUI>().AvatarPlayer1.material = Appearance.watchFrameMat1Tag;
                    SetFaceWhenCharaOff(Appearance.watchFace1Tag, 1);
                }
                if (Services.ResponsesDuelSession.UsesAiAPerspective)
                {
                    _ = ApplyAiSpectatorFaceAsync(0);
                    _ = ApplyAiSpectatorFaceAsync(1);
                }
            }
            else if (condition == Condition.Replay)
            {
                if (GetUI<UI.ServantUI.OcgCoreUI>().TextPlayer0Name.text == name_0)
                {
                    GetUI<UI.ServantUI.OcgCoreUI>().AvatarPlayer0.material = Appearance.replayFrameMat0;
                    SetFaceWhenCharaOff(Appearance.replayFace0, 0);
                }
                else
                {
                    GetUI<UI.ServantUI.OcgCoreUI>().AvatarPlayer0.material = Appearance.replayFrameMat0Tag;
                    SetFaceWhenCharaOff(Appearance.replayFace0Tag, 0);
                }
                if (GetUI<UI.ServantUI.OcgCoreUI>().TextPlayer1Name.text == name_1)
                {
                    GetUI<UI.ServantUI.OcgCoreUI>().AvatarPlayer1.material = Appearance.replayFrameMat1;
                    SetFaceWhenCharaOff(Appearance.replayFace1, 1);
                }
                else
                {
                    GetUI<UI.ServantUI.OcgCoreUI>().AvatarPlayer1.material = Appearance.replayFrameMat1Tag;
                    SetFaceWhenCharaOff(Appearance.replayFace1Tag, 1);
                }
            }

            _ = SetMyCardFace();
            SetCharacterDefaultFace();
        }

        private async UniTask ApplyOnlineOpponentFaceAsync()
        {
            if (condition != Condition.Duel)
                return;
            int generation = faceGeneration;

            var useTagOpponent = isTag && GetUI<UI.ServantUI.OcgCoreUI>().TextPlayer1Name.text != name_1;
            if (!RoomServant.TryGetOnlineAppearanceForOpponent(useTagOpponent, out MDPro3.OnlineAppearanceData appearance))
            {
                if (!useTagOpponent || !RoomServant.TryGetOnlineAppearanceForOpponent(false, out appearance))
                    return;
            }

            var iconPlayer = useTagOpponent ? 3 : 1;
            var frameCode = appearance.Frame.ToString();
            var frameSprite = await Runtime.Program.items.LoadConcreteItemIconAsync(frameCode, Presentation.Assets.Items.ItemType.Frame, iconPlayer);
            Material frameMaterial;
            if (appearance.Frame == Presentation.Assets.Items.CODE_DIY)
                frameMaterial = Appearance.matForFace == null ? null : new Material(Appearance.matForFace);
            else
                frameMaterial = await Runtime.ABLoader.LoadFrameMaterial(frameCode);
            if (frameMaterial != null && frameSprite != null)
                frameMaterial.SetTexture("_ProfileFrameTex", frameSprite.texture);

            var faceSprite = await Runtime.Program.items.LoadConcreteItemIconAsync(appearance.Face.ToString(), Presentation.Assets.Items.ItemType.Face, iconPlayer);

            if (!showing || condition != Condition.Duel || generation != faceGeneration)
                return;
            var latestUseTagOpponent = isTag && GetUI<UI.ServantUI.OcgCoreUI>().TextPlayer1Name.text != name_1;
            if (latestUseTagOpponent != useTagOpponent)
                return;

            if (!RoomServant.TryGetOnlineAppearanceForOpponent(useTagOpponent, out MDPro3.OnlineAppearanceData latest))
            {
                if (!useTagOpponent || !RoomServant.TryGetOnlineAppearanceForOpponent(false, out latest))
                    return;
            }
            if (latest.Face != appearance.Face || latest.Frame != appearance.Frame)
                return;

            if (frameMaterial != null)
                GetUI<UI.ServantUI.OcgCoreUI>().AvatarPlayer1.material = frameMaterial;
            if (faceSprite != null)
                SetFaceWhenCharaOff(faceSprite, 1);
        }

        private async UniTask ApplyAiSpectatorFaceAsync(int side)
        {
            if (condition != Condition.Watch || !Services.ResponsesDuelSession.UsesAiAPerspective
                || !RoomServant.TryGetOnlineAppearanceForPlayer(side, out MDPro3.OnlineAppearanceData appearance)) return;
            int generation = faceGeneration;
            string profileName = side == 0 ? name_0 : name_1;
            var frameCode = appearance.Frame.ToString();
            var frameSprite = await Runtime.Program.items.LoadConcreteItemIconAsync(frameCode, Presentation.Assets.Items.ItemType.Frame, side);
            var frameMaterial = appearance.Frame == Presentation.Assets.Items.CODE_DIY
                ? Appearance.matForFace == null ? null : new Material(Appearance.matForFace)
                : await Runtime.ABLoader.LoadFrameMaterial(frameCode);
            if (frameMaterial != null && frameSprite != null)
                frameMaterial.SetTexture("_ProfileFrameTex", frameSprite.texture);
            var faceSprite = await Runtime.Program.items.LoadConcreteItemIconAsync(appearance.Face.ToString(), Presentation.Assets.Items.ItemType.Face, side);
            if (!showing || generation != faceGeneration || condition != Condition.Watch || !Services.ResponsesDuelSession.UsesAiAPerspective
                || profileName != (side == 0 ? name_0 : name_1)
                || !RoomServant.TryGetOnlineAppearanceForPlayer(side, out MDPro3.OnlineAppearanceData latest)
                || latest.Face != appearance.Face || latest.Frame != appearance.Frame) return;
            var avatar = side == 0 ? GetUI<UI.ServantUI.OcgCoreUI>().AvatarPlayer0 : GetUI<UI.ServantUI.OcgCoreUI>().AvatarPlayer1;
            if (frameMaterial != null) avatar.material = frameMaterial;
            if (faceSprite != null) SetFaceWhenCharaOff(faceSprite, side);
        }

        private async UniTask SetMyCardFace()
        {
            if (Services.MyCard.account == null || !mycardDuel)
                return;

            int generation = faceGeneration;
            var ui = GetUI<UI.ServantUI.OcgCoreUI>();
            string playerName = ui.TextPlayer0Name.text;
            string opponentName = ui.TextPlayer1Name.text;
            var avatar = await Services.MyCard.GetAvatarAsync(playerName);
            if (!showing || !mycardDuel || generation != faceGeneration
                || playerName != ui.TextPlayer0Name.text || opponentName != ui.TextPlayer1Name.text) return;
            if (avatar != null)
                SetFaceWhenCharaOff(Runtime.TextureManager.Texture2Sprite(avatar), 0, true);

            avatar = await Services.MyCard.GetAvatarAsync(opponentName);
            if (!showing || !mycardDuel || generation != faceGeneration
                || playerName != ui.TextPlayer0Name.text || opponentName != ui.TextPlayer1Name.text) return;
            if (avatar != null)
                SetFaceWhenCharaOff(Runtime.TextureManager.Texture2Sprite(avatar), 1, true);
        }


        private Sprite mySprite;
        private Sprite opSprite;

        private void SetFaceWhenCharaOff(Sprite sprite, int player, bool externalFace = false)
        {
            // A late room-sync icon must not replace an already loaded MyCard
            // avatar. SetFace resets this ownership when the duelist changes.
            if (customCharacterFace[player] && !externalFace) return;
            if (player == 0)
                mySprite = sprite;
            else
                opSprite = sprite;

            if (externalFace) customCharacterFace[player] = true;
            if (!characterFaceActive[player]) SetFallbackFace(player);
            RefreshCharacterFace(player);
        }

        public void CloseCharaFace()
        {
            faceGeneration++;
            characterVoiceVersion++;
            for (int side = 0; side < 2; side++)
            {
                characterFaceVersion[side]++;
                characterFaceContext[side] = null;
                characterFaceActive[side] = false;
                customCharacterFace[side] = false;
                SetFallbackFace(side);
            }
            charaFaceSetting = false;
        }

        public void CheckCharaFace()
        {
            if (!showing)
                return;

            // The voice setting controls expressions, not the selected portrait.
            SetCharacterDefaultFace();
        }

        public void SetLP(int player, int val, bool first = false)
        {
            if (first)
            {
                GetUI<UI.ServantUI.OcgCoreUI>().TextPlayer0LP.text = FormatLifePoints(life0);
                GetUI<UI.ServantUI.OcgCoreUI>().TextPlayer1LP.text = FormatLifePoints(life1);
            }
            else
            {
                AnimationLpChange(player, val);
            }
        }

        private Sequence AnimationLpChange(int player, int val)
        {
            TextMeshProUGUI text;
            int targetLP;
            if (player == 0)
            {
                text = GetUI<UI.ServantUI.OcgCoreUI>().TextPlayer0LP;
                targetLP = life0;
            }
            else
            {
                text = GetUI<UI.ServantUI.OcgCoreUI>().TextPlayer1LP;
                targetLP = life1;
            }

            int origin = targetLP - val;
            var sequence = DOTween.Sequence();
            var obj = Runtime.ABLoader.LoadMasterDuelGameObject("DuelLpText");
            var popupText = obj.GetComponent<TextMeshProUGUI>();
            var popupRect = obj.GetComponent<RectTransform>();
            popupText.text = Math.Abs(val).ToString();
            obj.transform.SetParent(GetUI<UI.ServantUI.OcgCoreUI>().RectPopup, false);
            var uiWidth = Screen.width * (float)1080 / Screen.height;
            Color color = Duel.DuelLog.damageColor;
            string seType = "COUNT";
            bool sceneFeedback = UI.Client.FightDuelPopupSkin.SceneThemeActive;
            float fontSize = sceneFeedback ? 72 : 120;
            if (sceneFeedback)
            {
                popupText.fontSize = fontSize;
                popupText.text = (val > 0 ? "+" : val < 0 ? "-" : string.Empty) + Math.Abs(val).ToString();
            }
            if (val > 0)
            {
                color = Duel.DuelLog.recoverColor;
                seType = "RECOVERY";
            }

            popupText.color = color;
            if (player == 0)
            {
                popupRect.anchoredPosition = new Vector2(0, -400);
                obj.transform.localScale = Vector3.zero;
                float targetX = -(uiWidth / 2 - 325);
                sequence.Append(obj.transform.DOScale(1, 0.1f));
                sequence.AppendInterval(0.6f);
                sequence.Append(popupRect.DOAnchorPosX(targetX, 0.2f));
                sequence.Join(DOTween.To(() => fontSize, x =>
                {
                    fontSize = x;
                    popupText.fontSize = (int)fontSize;
                }, 40, 0.2f));
                sequence.Append(popupRect.DOAnchorPosY(-490, 0.2f));

                if(val < 0)
                {
                    GetUI<UI.ServantUI.OcgCoreUI>().DamageFrame.Show();
                }
            }
            else
            {
                popupRect.anchoredPosition = new Vector2(0, 400);
                obj.transform.localScale = Vector3.zero;
                float targetX = uiWidth / 2 - 225;
                sequence.Append(obj.transform.DOScale(1, 0.1f));
                sequence.AppendInterval(0.6f);
                sequence.Append(popupRect.DOAnchorPosX(targetX, 0.2f));
                sequence.Join(DOTween.To(() => fontSize, x =>
                {
                    fontSize = x;
                    popupText.fontSize = (int)fontSize;
                }, 40, 0.2f));
                sequence.Append(popupRect.DOAnchorPosY(450, 0.2f));
            }
            sequence.Join(popupText.DOFade(0, 0.2f));
            var lpTweenStart = sequence.Duration(false);

            sequence.Append(text.DOColor(color, 0.1f));
            sequence.Join(text.transform.DOScale(1.3f, 0.2f));
            sequence.AppendInterval(0.8f);
            sequence.Append(text.transform.DOScale(1f, 0.2f));
            sequence.Join(text.DOColor(Color.white, 0.2f));

            float flp = origin;
            sequence.Insert(lpTweenStart,
                DOTween.To(() => flp, x =>
                {
                    flp = x;
                    text.text = FormatLifePoints((int)flp);
                }, NormalizeLifePointsForDisplay(targetLP), 1.2f));
            sequence.InsertCallback(lpTweenStart, () =>
            {
                Runtime.AudioManager.PlaySE("SE_LP_" + seType + (player == 0 ? "_PLAYER" : "_RIVAL"));
                Destroy(obj);
            });

            return sequence;
        }

        private void RefreshHandCardPositionInstant()
        {
            hideMyHandCard = false;
            if (showing)
                foreach (Duel.GameCard card in cards)
                    card.SetHandDefault();
        }

        public void RefreshHandCardPosition()
        {
            if (showing)
                foreach (Duel.GameCard card in cards)
                    card.SetHandToDefault();
        }

        public void RefreshMyHandCardPosition()
        {
            if (showing)
                foreach (Duel.GameCard card in cards)
                    if(card.p.InMyControl())
                        card.SetHandToDefault();
        }

        public void RefreshOpHandCardPosition()
        {
            if (showing)
                foreach (Duel.GameCard card in cards)
                    if (!card.p.InMyControl())
                        card.SetHandToDefault();
        }


        string fieldHint;
        int fieldMin;
        int fieldMax;
        bool fieldExitable;
        bool fieldSendable;
        [HideInInspector] public int fieldCounterCount;

        public void FieldSelect(string hint, List<Duel.GameCard> cards, int min, int max, bool exitable, bool sendable)
        {
            foreach (UI.PlaceSelector place in Places)
                place.InitializeSelectCardInThisZone(cards);
            fieldHint = string.IsNullOrEmpty(hint) ? MDPro3.InterString.Get("请选择卡片") : hint;
            fieldMin = min;
            fieldMax = max;
            fieldExitable = exitable;
            fieldSendable = sendable;
            fieldCounterCount = 0;

            if (currentMessage == MDPro3.Duel.YGOSharp.GameMessage.SelectCard
                || currentMessage == MDPro3.Duel.YGOSharp.GameMessage.SelectCounter)
                GetUI<UI.ServantUI.OcgCoreUI>().SetHint(fieldHint + ": " + 0 + Runtime.Program.STRING_SLASH + fieldMax);
            else if (currentMessage == MDPro3.Duel.YGOSharp.GameMessage.SelectSum)
            {
                if (!ES_overFlow)
                    foreach (UI.PlaceSelector place in Places)
                        if (place.cardSelecting)
                            if (!place.cardSelected)
                                if (CheckSelectableInSum(cardsInSelection, place.cookieCard, cardsMustBeSelected, ES_max + cardsMustBeSelected.Count))
                                    place.CardInThisZoneSelectable();
                                else
                                    place.CardInThisZoneUnselectable();
                GetUI<UI.ServantUI.OcgCoreUI>().SetHint(fieldHint + ": " + GetSelectLevelSum(cardsMustBeSelected)[0] + Runtime.Program.STRING_SLASH + ES_level);
            }
            else
            {
                if (!string.IsNullOrEmpty(fieldHint))
                    GetUI<UI.ServantUI.OcgCoreUI>().SetHint(fieldHint);
            }

            RefreshButton();
        }

        public void FieldSelectRefresh(Duel.GameCard card)
        {
            var selected = new List<Duel.GameCard>();
            foreach (UI.PlaceSelector place in Places)
                if (place.cardSelecting)
                    if (place.cardSelected)
                        selected.Add(place.cookieCard);
            if (currentMessage == MDPro3.Duel.YGOSharp.GameMessage.SelectSum)
            {
                var sum = GetSelectLevelSum(selected);
                if ((ES_overFlow && (ES_level <= sum[0] || ES_level <= sum[1]))
                    ||
                    (!ES_overFlow && (ES_level == sum[0] || ES_level == sum[1])))
                    fieldSendable = true;
                else
                    fieldSendable = false;
                if (!ES_overFlow)
                {
                    if (sum[0] == ES_level || sum[1] == ES_level)
                    {
                        FieldSelectedSend();
                        return;
                    }
                    else
                    {
                        foreach (UI.PlaceSelector place in Places)
                            if (place.cardSelecting)
                                if (!place.cardSelected)
                                    if (CheckSelectableInSum(cardsInSelection, place.cookieCard, selected, ES_max + cardsMustBeSelected.Count))
                                        place.CardInThisZoneSelectable();
                                    else
                                        place.CardInThisZoneUnselectable();
                    }
                }
                RefreshButton();
                GetUI<UI.ServantUI.OcgCoreUI>().SetHint(fieldHint + ": " + GetSelectLevelSum(selected)[0] + Runtime.Program.STRING_SLASH + ES_level);
            }
            else if (currentMessage == MDPro3.Duel.YGOSharp.GameMessage.SelectCounter)
            {
                fieldCounterCount++;
                card.counterSelected++;
                GetUI<UI.ServantUI.OcgCoreUI>().SetHint(fieldHint + ": " + fieldCounterCount + Runtime.Program.STRING_SLASH + fieldMax);
                if (fieldCounterCount == ES_min)
                {
                    FieldSelectedSend();
                    return;
                }
                foreach (UI.PlaceSelector place in Places)
                    if (place.cardSelecting)
                    {
                        if (place.cookieCard.counterCanCount > place.cookieCard.counterSelected)
                            place.CardInThisZoneSelectable();
                        else
                            place.CardInThisZoneUnselectable();
                    }
            }
            else if (currentMessage == MDPro3.Duel.YGOSharp.GameMessage.SelectTribute)
            {
                var sum = 0;
                foreach (Duel.GameCard c in selected)
                    sum += c.levelForSelect_1;
                if (selected.Count >= fieldMax)
                    FieldSelectedSend();
                else if (sum >= fieldMin)
                {
                    fieldSendable = true;
                    RefreshButton();
                }
            }
            else
            {
                if (selected.Count >= fieldMin)
                    fieldSendable = true;
                else
                    fieldSendable = false;
                if (selected.Count >= fieldMax)
                    FieldSelectedSend();
                else
                {
                    foreach (UI.PlaceSelector place in Places)
                        if (place.cardSelecting)
                            if (!place.cardSelected)
                                place.CardInThisZoneSelectable();
                    RefreshButton();
                }
                if (currentMessage == MDPro3.Duel.YGOSharp.GameMessage.SelectCard)
                    GetUI<UI.ServantUI.OcgCoreUI>().SetHint(fieldHint + ": " + selected.Count + Runtime.Program.STRING_SLASH + fieldMax);
            }
        }

        private void RefreshButton()
        {
            if (fieldSendable)
            {
                btnConfirm.Show();
                if (currentMessage == MDPro3.Duel.YGOSharp.GameMessage.SelectUnselect)
                    btnCancel.Hide();
            }
            else
                btnConfirm.Hide();
            if (fieldExitable)
            {
                if (currentMessage == MDPro3.Duel.YGOSharp.GameMessage.SelectUnselect && fieldSendable)
                {
                }
                else
                    btnCancel.Show();
            }
            else
                btnCancel.Hide();
        }

        public void FieldSelectedSend()
        {
            var selected = new List<Duel.GameCard>();
            foreach (UI.PlaceSelector place in Places)
                if (place.cardSelecting)
                    if (place.cardSelected)
                        selected.Add(place.cookieCard);
            BinaryMaster binaryMaster = new BinaryMaster();
            if (currentMessage == MDPro3.Duel.YGOSharp.GameMessage.SelectUnselect && selected.Count == 0)
                binaryMaster.writer.Write(-1);
            else if (currentMessage == MDPro3.Duel.YGOSharp.GameMessage.SelectCounter)
                for (var i = 0; i < cardsInSelection.Count; i++)
                    binaryMaster.writer.Write((short)cardsInSelection[i].counterSelected);
            else if (currentMessage == MDPro3.Duel.YGOSharp.GameMessage.SelectSum)
            {
                binaryMaster.writer.Write((byte)selected.Count);
                foreach (Duel.GameCard card in cardsMustBeSelected)
                    binaryMaster.writer.Write((byte)card.selectPtr);
                foreach (Duel.GameCard card in selected)
                    if (!cardsMustBeSelected.Contains(card))
                        binaryMaster.writer.Write((byte)card.selectPtr);
            }
            else
            {
                binaryMaster.writer.Write((byte)selected.Count);
                foreach (Duel.GameCard card in selected)
                    binaryMaster.writer.Write((byte)card.selectPtr);
            }
            SendReturn(binaryMaster.Get());
        }

        public void FieldSelectedCancel()
        {
            if (currentMessage == MDPro3.Duel.YGOSharp.GameMessage.SelectCounter)
            {
                foreach (Duel.GameCard card in cardsInSelection)
                    card.counterSelected = 0;
                fieldCounterCount = 0;
                GetUI<UI.ServantUI.OcgCoreUI>().SetHint(fieldHint + ": " + 0 + Runtime.Program.STRING_SLASH + fieldMax);
                foreach (UI.PlaceSelector place in Places)
                    if (place.cardSelecting)
                        place.CardInThisZoneSelectable();
            }
            else
            {
                BinaryMaster binaryMaster = new BinaryMaster();
                binaryMaster.writer.Write(-1);
                SendReturn(binaryMaster.Get());
            }
        }

        public void SetExDeckTop(Duel.GameCard card)
        {
            if (DuelBGManager == null)
                return;
            DuelBGManager.SetExDeckTop(card);
        }

        public void UpdateExDeckTop(uint controller)
        {
            if (DuelBGManager == null)
                return;
            DuelBGManager.UpdateExDeckTop(controller);
        }

        public void SetBgTimeScale(float timeScale)
        {
            if (DuelBGManager == null)
                return;
            DuelBGManager.SetBgTimeScale(timeScale);
        }

        public void PlayGraveEffect(Duel.GPS p, bool isIn)
        {
            if (DuelBGManager == null)
                return;
            DuelBGManager.PlayGraveEffect(p, isIn);
        }

        public long GetAllAtk(bool mySide)
        {
            long allAttack = 0;
            var monsters = GCS_GetLocationCards(mySide ? 0 : 1, (int)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone);
            foreach (Duel.GameCard card in monsters)
                if ((card.p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.FaceUpAttack) > 0)
                    allAttack += MDPro3.Duel.YGOSharp.Card.NormalizeBattleValue(card.GetData().Attack, false);
            return allAttack;
        }

        private bool PlayerLosing()
        {
            if (myTurn)
            {
                if (GetAllAtk(true) - GetAllAtk(false) > life1)
                {
                    var defenseCount = 0;
                    var monsters = GCS_GetLocationCards(1, (int)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone);
                    foreach (Duel.GameCard card in monsters)
                        if ((card.p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.Defence) > 0)
                            defenseCount++;
                    if (defenseCount == 0)
                        return true;
                }
            }
            else if (!myTurn)
            {
                if (GetAllAtk(false) - GetAllAtk(true) > life0)
                {
                    var defenseCount = 0;
                    var monsters = GCS_GetLocationCards(0, (int)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone);
                    foreach (Duel.GameCard card in monsters)
                        if ((card.p.position & (uint)MDPro3.Duel.YGOSharp.CardPosition.Defence) > 0)
                            defenseCount++;
                    if (defenseCount == 0)
                        return true;
                }
            }
            return false;
        }

        public void ShowEquipLine(Vector3 start, Vector3 end)
        {
            if (DuelBGManager == null)
                return;

            DuelBGManager.ShowEquipLine(start, end);
        }

        public void ShowTargetLines(Vector3 start, List<Duel.GameCard> targets)
        {
            if (DuelBGManager == null)
                return;

            DuelBGManager.ShowTargetLines(start, targets);
        }

        public Transform GetFieldTransform(uint player)
        {
            return (player == 0 ? DuelBGManager.field0Manager.transform : DuelBGManager.field1Manager.transform);
        }

        public Presentation.Elements.ElementObjectManager GetDeckModel(uint player, MDPro3.Duel.YGOSharp.CardLocation location)
        {
            if(location == MDPro3.Duel.YGOSharp.CardLocation.Deck)
                return (player == 0 ? DuelBGManager.myDeck : DuelBGManager.opDeck);
            else
                return (player == 0 ? DuelBGManager.myExtra : DuelBGManager.opExtra);
        }

        public void SetDeckModelActive(Presentation.Elements.ElementObjectManager deck, bool active)
        {
            deck.GetElement("CardShuffleTop").SetActive(active);
        }

        #endregion

        #region Voice

        public UI.ChatItemHandler duelChat0;
        public UI.ChatItemHandler duelChat1;

        public bool NeedVoice()
        {
            return MDPro3.Config.GetBool(condition + "Voice", false);
        }

        public void SetCharacterFace(string chara, int id, bool isMe, float delay = 0f)
        {
            int side = isMe ? 0 : 1;
            RefreshCharacterFace(side);
            string context = GetCharacterFaceContext(side, out var selectedCharacter);
            if (!showing || !NeedVoice() || selectedCharacter == null || chara != selectedCharacter)
                return;
            // An immediate expression begins a new voice line. Its delayed
            // neutral face shares the version, and cannot reset a newer line.
            if (delay <= 0f) characterFaceVersion[side]++;
            StartCoroutine(SetCharacterFaceAsync(chara, id, side, Mathf.Max(0f, delay),
                faceGeneration, characterFaceVersion[side], context, true));
        }

        public bool charaFaceSetting;
        public Dictionary<string, Sprite> cachedCharaFaces = new Dictionary<string, Sprite>();
        private const string DefaultFaceCode = "1010001";
        private int faceGeneration;
        private int characterVoiceVersion;
        public int CharacterFacePresentationVersion => characterVoiceVersion;
        private readonly int[] characterFaceVersion = new int[2];
        private readonly string[] characterFaceContext = new string[2];
        private readonly bool[] characterFaceActive = new bool[2];
        private readonly bool[] customCharacterFace = new bool[2];

        private string GetFaceSlot(int side)
        {
            if (!isTag) return side.ToString();
            var ui = GetUI<UI.ServantUI.OcgCoreUI>();
            bool tag = side == 0 ? ui.TextPlayer0Name.text != name_0 : ui.TextPlayer1Name.text != name_1;
            if (condition == Condition.Duel && side == 0 && RoomServant.SelfType != 0 && RoomServant.SelfType != 2)
                tag = !tag;
            return side + (tag ? "Tag" : "");
        }

        private string GetSelectedFaceCode(int side, string slot)
        {
            string code = Services.ReplayAppearance.Get(condition + "Face" + slot, DefaultFaceCode);
            if (condition == Condition.Replay) return code;
            code = Services.ResponsesDuelSession.AppearanceForSide(side, "Face", code);
            if (condition == Condition.Duel && side == 1)
            {
                bool tag = slot.EndsWith("Tag", StringComparison.Ordinal);
                if (RoomServant.TryGetOnlineAppearanceForOpponent(tag, out var appearance)
                    || tag && RoomServant.TryGetOnlineAppearanceForOpponent(false, out appearance))
                    code = appearance.Face.ToString();
            }
            else if (condition == Condition.Watch && Services.ResponsesDuelSession.UsesAiAPerspective
                && RoomServant.TryGetOnlineAppearanceForPlayer(side, out var appearance))
                code = appearance.Face.ToString();
            return code;
        }

        private string GetCharacterFaceContext(int side, out string character)
        {
            string slot = GetFaceSlot(side);
            string face = GetSelectedFaceCode(side, slot);
            character = Services.ReplayAppearance.Get(condition + "Character" + side, Presentation.Voice.VoicePlayer.defaultCharacter);
            if (condition != Condition.Replay)
                character = Services.ResponsesDuelSession.AppearanceForSide(side, "Character", character);
            if (string.IsNullOrEmpty(character) || character == "0")
                character = Presentation.Voice.VoicePlayer.defaultCharacter;
            string context = condition + ":" + slot + ":" + face + ":" + character + ":" + NeedVoice() + ":" + customCharacterFace[side];
            // A concrete Face remains the user's avatar. The default face is
            // replaced by the selected Character, independently on each side.
            if (face != DefaultFaceCode || customCharacterFace[side] || condition == Condition.N)
                character = null;
            return context;
        }

        private void SetFallbackFace(int side)
        {
            var ui = GetUI<UI.ServantUI.OcgCoreUI>();
            if (ui == null) return;
            var avatar = side == 0 ? ui.AvatarPlayer0 : ui.AvatarPlayer1;
            if (avatar != null) avatar.sprite = side == 0 ? mySprite : opSprite;
        }

        private void RefreshCharacterFace(int side)
        {
            if (!showing) return;
            string context = GetCharacterFaceContext(side, out var character);
            if (context == characterFaceContext[side]) return;
            characterFaceContext[side] = context;
            characterVoiceVersion++;
            characterFaceVersion[side]++;
            characterFaceActive[side] = false;
            charaFaceSetting = characterFaceActive[0] || characterFaceActive[1];
            SetFallbackFace(side);
            if (character == null) return;
            StartCoroutine(SetCharacterFaceAsync(character, 1, side, 0f,
                faceGeneration, characterFaceVersion[side], context, false));
        }

        private bool CharacterFaceRequestIsCurrent(int side, int generation, int version, string context, bool expression)
        {
            return showing && generation == faceGeneration && version == characterFaceVersion[side]
                && context == characterFaceContext[side]
                && context == GetCharacterFaceContext(side, out var character) && character != null
                && (!expression || NeedVoice());
        }

        private IEnumerator SetCharacterFaceAsync(string chara, int id, int side, float delay,
            int generation, int version, string context, bool expression)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (!CharacterFaceRequestIsCurrent(side, generation, version, context, expression)) yield break;

            if (id == 0)
                id = 1;

            var address = "sn" + chara + "_3_" + id;
            if (!cachedCharaFaces.TryGetValue(address, out var sprite))
            {
                var handle = Addressables.LoadAssetAsync<Sprite>(address);
                yield return handle;

                try
                {
                    if (CharacterFaceRequestIsCurrent(side, generation, version, context, expression)
                        && handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded
                        && handle.Result != null && handle.Result.texture != null
                        && !cachedCharaFaces.TryGetValue(address, out sprite))
                    {
                        var source = handle.Result.texture;
                        var texture = source.width != source.height
                            ? Runtime.TextureManager.GetCroppingTex(source, 80, 0, 320, 240)
                            : Runtime.TextureManager.CreateCenteredTexture(source, 280, 0, 10);
                        Runtime.TextureManager.ReplaceTransparentPixelsWithColor(texture, Color.black);
                        sprite = Runtime.TextureManager.Texture2Sprite(texture);
                        cachedCharaFaces[address] = sprite;
                    }
                }
                catch (Exception error)
                {
                    Debug.LogWarning("[FightClient] Character portrait unavailable: " + error.GetType().Name);
                }
                finally
                {
                    // The displayed sprite owns its copied texture, so no
                    // Addressables handle needs to outlive this request.
                    if (handle.IsValid()) Addressables.Release(handle);
                }
            }

            if (!CharacterFaceRequestIsCurrent(side, generation, version, context, expression)) yield break;
            if (sprite == null)
            {
                if (id != 1)
                    StartCoroutine(SetCharacterFaceAsync(chara, 1, side, 0f, generation, version, context, false));
                else
                {
                    characterFaceActive[side] = false;
                    charaFaceSetting = characterFaceActive[0] || characterFaceActive[1];
                    SetFallbackFace(side);
                }
                yield break;
            }
            var ui = GetUI<UI.ServantUI.OcgCoreUI>();
            var avatar = side == 0 ? ui.AvatarPlayer0 : ui.AvatarPlayer1;
            avatar.sprite = sprite;
            characterFaceActive[side] = true;
            charaFaceSetting = characterFaceActive[0] || characterFaceActive[1];
        }

        public void SetCharacterDefaultFace()
        {
            RefreshCharacterFace(0);
            RefreshCharacterFace(1);
        }

        #endregion

        #region Enum

        public enum DuelResult
        {
            DisLink,
            Win,
            Lose,
            Draw
        }

        public enum Condition
        {
            N,
            Duel,
            Watch,
            Replay
        }

        public enum ChainCondition
        {
            No = 0,
            All = 1,
            Smart = 2,
        }

        #endregion

    }
}
