using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

using static YuGiOhFight.Client.UI.ChatPanel;


namespace YuGiOhFight.Client.UI.ServantUI
{
    public class RoomServantUI : ServantUI
    {

        #region Elements

        private const string LABEL_TXT_ROOMINFO = "TextRoomInfo";
        private TextMeshProUGUI m_TextRoomInfo;
        private TextMeshProUGUI TextRoomInfo =>
            m_TextRoomInfo = m_TextRoomInfo != null ? m_TextRoomInfo
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_ROOMINFO);

        private const string LABEL_SBN_DECKSELECTOR = "DeckSelector";
        private SelectionButton_DeckSelector m_ButtonDeckSelector;
        private SelectionButton_DeckSelector ButtonDeckSelector =>
            m_ButtonDeckSelector = m_ButtonDeckSelector != null ? m_ButtonDeckSelector
            : Manager.GetElement<SelectionButton_DeckSelector>(LABEL_SBN_DECKSELECTOR);

        private const string LABEL_SBN_ADDBOT = "ButtonAddBot";
        private SelectionButton m_ButtonAddBot;
        private SelectionButton ButtonAddBot =>
            m_ButtonAddBot = m_ButtonAddBot != null ? m_ButtonAddBot
            : Manager.GetElement<SelectionButton>(LABEL_SBN_ADDBOT);

        private const string LABEL_SBN_PLAYER0 = "ButtonPlayer0";
        private SelectionButton_RoomPlayer m_ButtonPlayer0;
        private SelectionButton_RoomPlayer ButtonPlayer0 =>
            m_ButtonPlayer0 = m_ButtonPlayer0 != null ? m_ButtonPlayer0
            : Manager.GetElement<SelectionButton_RoomPlayer>(LABEL_SBN_PLAYER0);

        private const string LABEL_SBN_PLAYER1 = "ButtonPlayer1";
        private SelectionButton_RoomPlayer m_ButtonPlayer1;
        private SelectionButton_RoomPlayer ButtonPlayer1 =>
            m_ButtonPlayer1 = m_ButtonPlayer1 != null ? m_ButtonPlayer1
            : Manager.GetElement<SelectionButton_RoomPlayer>(LABEL_SBN_PLAYER1);

        private const string LABEL_SBN_PLAYER2 = "ButtonPlayer2";
        private SelectionButton_RoomPlayer m_ButtonPlayer2;
        private SelectionButton_RoomPlayer ButtonPlayer2 =>
            m_ButtonPlayer2 = m_ButtonPlayer2 != null ? m_ButtonPlayer2
            : Manager.GetElement<SelectionButton_RoomPlayer>(LABEL_SBN_PLAYER2);

        private const string LABEL_SBN_PLAYER3 = "ButtonPlayer3";
        private SelectionButton_RoomPlayer m_ButtonPlayer3;
        private SelectionButton_RoomPlayer ButtonPlayer3 =>
            m_ButtonPlayer3 = m_ButtonPlayer3 != null ? m_ButtonPlayer3
            : Manager.GetElement<SelectionButton_RoomPlayer>(LABEL_SBN_PLAYER3);

        private const string LABEL_SBN_TODUEL = "ButtonToDuel";
        private SelectionButton m_ButtonToDuel;
        private SelectionButton ButtonToDuel =>
            m_ButtonToDuel = m_ButtonToDuel != null ? m_ButtonToDuel
            : Manager.GetElement<SelectionButton>(LABEL_SBN_TODUEL);

        private const string LABEL_SBN_READY = "ButtonReady";
        private SelectionButton m_ButtonReady;
        private SelectionButton ButtonReady =>
            m_ButtonReady = m_ButtonReady != null ? m_ButtonReady
            : Manager.GetElement<SelectionButton>(LABEL_SBN_READY);

        private const string LABEL_SBN_TOWATCH = "ButtonToWatch";
        private SelectionButton m_ButtonToWatch;
        private SelectionButton ButtonToWatch =>
            m_ButtonToWatch = m_ButtonToWatch != null ? m_ButtonToWatch
            : Manager.GetElement<SelectionButton>(LABEL_SBN_TOWATCH);

        private const string LABEL_SBN_START = "ButtonStart";
        private SelectionButton m_ButtonStart;
        private SelectionButton ButtonStart =>
            m_ButtonStart = m_ButtonStart != null ? m_ButtonStart
            : Manager.GetElement<SelectionButton>(LABEL_SBN_START);

        #endregion

        private List<SelectionButton_RoomPlayer> roomPlayers;
        private readonly Dictionary<int, Sprite> syncedFaces = new();
        private readonly Dictionary<int, Material> syncedFrames = new();
        private readonly Dictionary<int, int> syncedFaceIds = new();
        private readonly Dictionary<int, int> syncedFrameIds = new();

        private void Awake()
        {
            roomPlayers = new List<SelectionButton_RoomPlayer>()
            { ButtonPlayer0, ButtonPlayer1, ButtonPlayer2, ButtonPlayer3 };

            Realize();
        }

        public override void AfterShowEvent()
        {
            base.AfterShowEvent();
            if (Runtime.Program.instance.currentServant != Runtime.Program.instance.room)
            {
                ShutDown();
                Runtime.Program.instance.ui_.chatPanel.Hide();
            }
        }

        public void SelectMiddleSelectableFromRight()
        {
            if (!gameObject.activeSelf)
                return;

            Runtime.UserInput.NextSelectionIsAxis = true;

            if (ButtonStart.gameObject.activeSelf)
                ButtonStart.GetSelectable().Select();
            else
                ButtonToWatch.GetSelectable().Select();
        }

        public void SelectMiddleFromLeft()
        {

        }

        public void Realize()
        {
            var roomInfo = string.Empty;
            var rn = Runtime.Program.STRING_LINE_BREAK;
            if (Controllers.RoomServant.FromLocalHost)
            {
                foreach (var ip in Utility.Tools.GetLocalIPv4())
                    roomInfo += MDPro3.InterString.Get("本机地址：") + MDPro3.Utility.Language.GetBlankIfNeed() + ip + rn;
                roomInfo += MDPro3.InterString.Get("端口：") + MDPro3.Utility.Language.GetBlankIfNeed() + "7911" + rn;
            }
            roomInfo += Presentation.StringHelper.GetUnsafe(1227) + MDPro3.Utility.Language.GetBlankIfNeed() + Presentation.StringHelper.GetUnsafe(1244 + Controllers.RoomServant.Mode) + rn;//决斗模式：
            roomInfo += Presentation.StringHelper.GetUnsafe(1236) + MDPro3.Utility.Language.GetBlankIfNeed() + Presentation.StringHelper.GetUnsafe(1259 + Controllers.OcgCore.MasterRule) + rn;//规则：
            roomInfo += Presentation.StringHelper.GetUnsafe(1225) + MDPro3.Utility.Language.GetBlankIfNeed() + Presentation.StringHelper.GetUnsafe(1481 + Controllers.RoomServant.Rule) + rn;//卡片允许：
            roomInfo += Presentation.StringHelper.GetUnsafe(1226) + MDPro3.Utility.Language.GetBlankIfNeed() + MDPro3.Duel.YGOSharp.BanlistManager.GetName(Controllers.RoomServant.LFList) + rn;//禁限卡表
            roomInfo += Presentation.StringHelper.GetUnsafe(1231) + MDPro3.Utility.Language.GetBlankIfNeed() + Controllers.RoomServant.StartLp + rn;//初始基本分：
            roomInfo += Presentation.StringHelper.GetUnsafe(1232) + MDPro3.Utility.Language.GetBlankIfNeed() + Controllers.RoomServant.StartHand + rn;//初始手卡数：
            roomInfo += Presentation.StringHelper.GetUnsafe(1233) + MDPro3.Utility.Language.GetBlankIfNeed() + Controllers.RoomServant.DrawCount + rn;//每回合抽卡：
            roomInfo += Presentation.StringHelper.GetUnsafe(1237) + MDPro3.Utility.Language.GetBlankIfNeed() + Controllers.RoomServant.TimeLimit + rn;//每回合时间：
            roomInfo += Presentation.StringHelper.GetUnsafe(1253) + MDPro3.Utility.Language.GetBlankIfNeed() + Controllers.RoomServant.ObserverCount + rn;//当前观战人数：
            if (Controllers.RoomServant.NoCheckDeck) roomInfo += Presentation.StringHelper.GetUnsafe(1229) + rn;//不检查卡组
            if (Controllers.RoomServant.NoShuffleDeck) roomInfo += Presentation.StringHelper.GetUnsafe(1230);//不洗切卡组
            TextRoomInfo.text = roomInfo;

            if (!Controllers.Appearance.loaded)
            {
                foreach(SelectionButton_RoomPlayer rp in roomPlayers)
                    rp.gameObject.SetActive(false);
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                if (Controllers.RoomServant.players[i] == null)
                    roomPlayers[i].gameObject.SetActive(false);
                else
                {
                    roomPlayers[i].gameObject.SetActive(true);
                    roomPlayers[i].SetButtonText(Controllers.RoomServant.players[i].name);
                    roomPlayers[i].SetReadyIcon(Controllers.RoomServant.players[i].ready);
                    roomPlayers[i].SetButtonTextColor(Controllers.RoomServant.SelfType == i ? Color.cyan : Color.white);

                    PlayerPosition position = GetPlayerPosition(i);
                    switch (position)
                    {
                        case PlayerPosition.Me:
                            roomPlayers[i].GetAvatar().material = Controllers.Appearance.duelFrameMat0;
                            roomPlayers[i].GetAvatar().sprite = Controllers.Appearance.duelFace0;
                            break;
                        case PlayerPosition.MyTag:
                            roomPlayers[i].GetAvatar().material = Controllers.Appearance.duelFrameMat0Tag;
                            roomPlayers[i].GetAvatar().sprite = Controllers.Appearance.duelFace0Tag;
                            break;
                        case PlayerPosition.Op:
                            roomPlayers[i].GetAvatar().material = Controllers.Appearance.duelFrameMat1;
                            roomPlayers[i].GetAvatar().sprite = Controllers.Appearance.duelFace1;
                            break;
                        case PlayerPosition.OpTag:
                            roomPlayers[i].GetAvatar().material = Controllers.Appearance.duelFrameMat1Tag;
                            roomPlayers[i].GetAvatar().sprite = Controllers.Appearance.duelFace1Tag;
                            break;
                        case PlayerPosition.WatchMe:
                            roomPlayers[i].GetAvatar().material = Controllers.Appearance.watchFrameMat0;
                            roomPlayers[i].GetAvatar().sprite = Controllers.Appearance.watchFace0;
                            break;
                        case PlayerPosition.WatchMyTag:
                            roomPlayers[i].GetAvatar().material = Controllers.Appearance.watchFrameMat0Tag;
                            roomPlayers[i].GetAvatar().sprite = Controllers.Appearance.watchFace0Tag;
                            break;
                        case PlayerPosition.WatchOp:
                            roomPlayers[i].GetAvatar().material = Controllers.Appearance.watchFrameMat1;
                            roomPlayers[i].GetAvatar().sprite = Controllers.Appearance.watchFace1;
                            break;
                        case PlayerPosition.WatchOpTag:
                            roomPlayers[i].GetAvatar().material = Controllers.Appearance.watchFrameMat1Tag;
                            roomPlayers[i].GetAvatar().sprite = Controllers.Appearance.watchFace1Tag;
                            break;
                    }
                    TryApplyOnlineAvatar(i);
                }
            }
            if (Controllers.RoomServant.IsHost)
            {
                ButtonStart.gameObject.SetActive(true);
                ButtonAddBot.gameObject.SetActive(true);
            }
            else
            {
                ButtonStart.gameObject.SetActive(false);
                ButtonAddBot.gameObject.SetActive(false);
            }

            if (Controllers.RoomServant.FromSolo)
                ButtonAddBot.gameObject.SetActive(false);

            if (Controllers.RoomServant.SelfType == 7)
                ButtonReady.gameObject.SetActive(false);
            else
                ButtonReady.gameObject.SetActive(true);

        }

        public void RefreshDeckSelector()
        {
            ButtonDeckSelector.SetConfigDeck(MDPro3.InterString.Get("请点击此处选择卡组"));
        }

        public void OnReady()
        {
            if (Controllers.RoomServant.players[Controllers.RoomServant.SelfType] == null)
                return;
            if (Controllers.RoomServant.players[Controllers.RoomServant.SelfType].ready)
                Runtime.Network.TcpHelper.CtosMessage_HsNotReady();
            else
            {
                var deckPath = Runtime.Program.PATH_DECK + MDPro3.Config.GetConfigDeckName() + Runtime.Program.EXPANSION_YDK;
                if (File.Exists(deckPath))
                {
                    Runtime.Network.TcpHelper.CtosMessage_UpdateDeck(new MDPro3.Duel.YGOSharp.Deck(deckPath));
                    Runtime.Network.TcpHelper.CtosMessage_HsReady();
                }
                else
                    Runtime.MessageManager.Cast(MDPro3.InterString.Get("请先选择有效的卡组。"));
            }
        }

        public void OnToDuel()
        {
            Runtime.Network.TcpHelper.CtosMessage_HsToDuelist();
        }

        public void OnToObserver()
        {
            Runtime.Network.TcpHelper.CtosMessage_HsToObserver();
        }

        public void OnStart()
        {
            Runtime.Network.TcpHelper.CtosMessage_HsStart();
        }

        public void OnKick(int player)
        {
            Runtime.Network.TcpHelper.CtosMessage_HsKick(player);
        }

        public void OnAddBot()
        {
            if (Runtime.Program.instance.room.RoomIsFull())
            {
                Runtime.MessageManager.Toast(MDPro3.InterString.Get("房间已满，无法继续添加AI。"));
            }
            else
            {
                Runtime.Program.instance.solo.SwitchCondition(Controllers.SoloSelector.Condition.ForRoom);
                Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.solo);
            }
        }

        public void OnSelectDeck()
        {
            if (!Runtime.Program.instance.room.CanChangeDeck())
                return;
            Runtime.Program.instance.deckSelector.SwitchCondition(Controllers.DeckSelector.Condition.ForDuel);
            Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.deckSelector);
        }

        private void TryApplyOnlineAvatar(int player)
        {
            if (!Controllers.RoomServant.TryGetOnlineAppearanceForPlayer(player, out MDPro3.OnlineAppearanceData appearance))
                return;
            var iconPlayer = GetIconPlayerIndex(GetPlayerPosition(player));

            if (syncedFaceIds.TryGetValue(player, out var cachedFaceId) &&
                syncedFrameIds.TryGetValue(player, out var cachedFrameId) &&
                cachedFaceId == appearance.Face &&
                cachedFrameId == appearance.Frame &&
                syncedFaces.TryGetValue(player, out var cachedFace) &&
                syncedFrames.TryGetValue(player, out var cachedFrame) &&
                cachedFace != null &&
                cachedFrame != null)
            {
                roomPlayers[player].GetAvatar().sprite = cachedFace;
                roomPlayers[player].GetAvatar().material = cachedFrame;
                return;
            }

            _ = ApplyOnlineAvatarAsync(player, appearance, iconPlayer);
        }

        private async UniTask ApplyOnlineAvatarAsync(int player, MDPro3.OnlineAppearanceData appearance, int iconPlayer)
        {
            var frameCode = appearance.Frame.ToString();
            var frameSprite = await Runtime.Program.items.LoadConcreteItemIconAsync(frameCode, Presentation.Assets.Items.ItemType.Frame, iconPlayer);
            Material frameMaterial;
            if (appearance.Frame == Presentation.Assets.Items.CODE_DIY)
                frameMaterial = Controllers.Appearance.matForFace == null ? null : new Material(Controllers.Appearance.matForFace);
            else
                frameMaterial = await Runtime.ABLoader.LoadFrameMaterial(frameCode);
            if (frameMaterial != null && frameSprite != null)
                frameMaterial.SetTexture("_ProfileFrameTex", frameSprite.texture);

            var faceSprite = await Runtime.Program.items.LoadConcreteItemIconAsync(appearance.Face.ToString(), Presentation.Assets.Items.ItemType.Face, iconPlayer);
            if (!Controllers.RoomServant.TryGetOnlineAppearanceForPlayer(player, out MDPro3.OnlineAppearanceData latest))
                return;
            if (latest.Face != appearance.Face || latest.Frame != appearance.Frame)
                return;
            if (player < 0 || player >= roomPlayers.Count)
                return;
            if (!roomPlayers[player].gameObject.activeInHierarchy)
                return;

            if (frameMaterial != null)
                roomPlayers[player].GetAvatar().material = frameMaterial;
            if (faceSprite != null)
                roomPlayers[player].GetAvatar().sprite = faceSprite;

            syncedFaceIds[player] = appearance.Face;
            syncedFrameIds[player] = appearance.Frame;
            syncedFaces[player] = faceSprite;
            syncedFrames[player] = frameMaterial;
        }

        private static int GetIconPlayerIndex(PlayerPosition position)
        {
            switch (position)
            {
                case PlayerPosition.Op:
                case PlayerPosition.WatchOp:
                    return 1;
                case PlayerPosition.MyTag:
                case PlayerPosition.WatchMyTag:
                    return 2;
                case PlayerPosition.OpTag:
                case PlayerPosition.WatchOpTag:
                    return 3;
                default:
                    return 0;
            }
        }

    }
}
