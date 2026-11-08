using DG.Tweening;


using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;


using YuGiOhFight.Client.Runtime.Network;


namespace YuGiOhFight.Client.UI
{
    public class ChatPanel : SidePanel
    {
        protected override bool Permanent => true;
        protected override float TransitionTime => 0.4f;
        [Header("Chat Panel")]
        public TMP_InputField input;
        [SerializeField] private ScrollRect scrollRect;

        private GameObject chatItemMe;
        private GameObject chatItemOp;
        private GameObject chatItemSystem;
        private List<GameObject> chatItems = new();
        private struct ChatMessage
        {
            public int player;
            public string content;
        }
        private readonly List<ChatMessage> cachedMessages = new();

        protected override void Awake()
        {
            base.Awake();

            var handle = Addressables.LoadAssetAsync<GameObject>("UI/ChatItemMe.prefab");
            handle.Completed += (result) =>
            {
                chatItemMe = result.Result;
            };
            var handle2 = Addressables.LoadAssetAsync<GameObject>("UI/ChatItemOp.prefab");
            handle2.Completed += (result) =>
            {
                chatItemOp = result.Result;
            };
            var handle3 = Addressables.LoadAssetAsync<GameObject>("UI/ChatItemSystem.prefab");
            handle3.Completed += (result) =>
            {
                chatItemSystem = result.Result;
            };
        }

        public void Switch()
        {
            if (showing)
            {
                if(!input.isFocused)
                    HideWithSound();
            }
            else
                Show();
        }

        public void Show(bool takeOver = true)
        {
            base.Show();
            if (Runtime.Program.instance.currentServant != Runtime.Program.instance.room
                && !MDPro3.Utility.DeviceInfo.OnMobile())
                input.Select();

            scrollRect.DOVerticalNormalizedPos(0f, 0f);
        }

        protected override void Update()
        {
            if (!showing) return;
            if (!NeedResponse()) return;

            if ((Runtime.UserInput.WasCancelPressed
                || Runtime.UserInput.MouseRightDown)
                && Runtime.Program.instance.currentServant == Runtime.Program.instance.ocgcore)
                HideWithSound();
        }

        protected override bool NeedResponse()
        {
            if (input.isFocused)
                return false;
            return base.NeedResponse() || Runtime.Program.instance.room.showing;
        }

        public void OnSend()
        {
            if (input.text == string.Empty)
            {
                Hide();
                return;
            }
            OnChat(input.text);
        }

        public void OnChat(string content)
        {
            if (content == string.Empty)
                return;
            TcpHelper.CtosMessage_Chat(content);
            Runtime.Program.instance.ui_.chatPanel.ClearInputField();
        }

        public string GetInputFieldText()
        {
            return input.text;
        }

        public void ClearInputField()
        {
            input.text = string.Empty;
        }

        public enum PlayerPosition
        {
            Me,
            MyTag,
            Op,
            OpTag,
            WatchMe, // 本机处于观战时的一号位
            WatchMyTag,
            WatchOp,
            WatchOpTag,
            Other
        }

        public void AddChatItem(int player, string content)
        {
            if (Controllers.RoomServant.CoreShowing == 1)
            {
                cachedMessages.Add(new ChatMessage
                {
                    player = player,
                    content = content
                });
                return;
            }
            if (Controllers.RoomServant.CoreShowing == 2 && cachedMessages.Count > 0)
            {
                var cacehd = new List<ChatMessage>(cachedMessages);
                cachedMessages.Clear();
                for (int i = 0; i < cacehd.Count; i++)
                    AddChatItem(cacehd[i].player, cacehd[i].content);
            }
            if (player == -2)
                return;
            var nickName = GetPlayerName(player);
            GameObject item = null;
            PlayerPosition position = GetPlayerPosition(player);
            switch (position)
            {
                case PlayerPosition.Me:
                    item = Instantiate(chatItemMe);
                    item.transform.GetChild(2).GetComponent<Image>().material = Controllers.Appearance.duelFrameMat0;
                    item.transform.GetChild(2).GetComponent<Image>().sprite = Controllers.Appearance.duelFace0;
                    break;
                case PlayerPosition.MyTag:
                    item = Instantiate(chatItemMe);
                    item.transform.GetChild(2).GetComponent<Image>().material = Controllers.Appearance.duelFrameMat0Tag;
                    item.transform.GetChild(2).GetComponent<Image>().sprite = Controllers.Appearance.duelFace0Tag;
                    break;
                case PlayerPosition.Op:
                    item = Instantiate(chatItemOp);
                    item.transform.GetChild(2).GetComponent<Image>().material = Controllers.Appearance.duelFrameMat1;
                    item.transform.GetChild(2).GetComponent<Image>().sprite = Controllers.Appearance.duelFace1;
                    break;
                case PlayerPosition.OpTag:
                    item = Instantiate(chatItemOp);
                    item.transform.GetChild(2).GetComponent<Image>().material = Controllers.Appearance.duelFrameMat1Tag;
                    item.transform.GetChild(2).GetComponent<Image>().sprite = Controllers.Appearance.duelFace1Tag;
                    break;
                case PlayerPosition.WatchMe:
                    item = Instantiate(chatItemMe);
                    item.transform.GetChild(2).GetComponent<Image>().material = Controllers.Appearance.watchFrameMat0;
                    item.transform.GetChild(2).GetComponent<Image>().sprite = Controllers.Appearance.watchFace0;
                    break;
                case PlayerPosition.WatchMyTag:
                    item = Instantiate(chatItemMe);
                    item.transform.GetChild(2).GetComponent<Image>().material = Controllers.Appearance.watchFrameMat0Tag;
                    item.transform.GetChild(2).GetComponent<Image>().sprite = Controllers.Appearance.watchFace0Tag;
                    break;
                case PlayerPosition.WatchOp:
                    item = Instantiate(chatItemOp);
                    item.transform.GetChild(2).GetComponent<Image>().material = Controllers.Appearance.watchFrameMat1;
                    item.transform.GetChild(2).GetComponent<Image>().sprite = Controllers.Appearance.watchFace1;
                    break;
                case PlayerPosition.WatchOpTag:
                    item = Instantiate(chatItemOp);
                    item.transform.GetChild(2).GetComponent<Image>().material = Controllers.Appearance.watchFrameMat1Tag;
                    item.transform.GetChild(2).GetComponent<Image>().sprite = Controllers.Appearance.watchFace1Tag;
                    break;
                case PlayerPosition.Other:
                    item = Instantiate(chatItemSystem);
                    break;
            }
            item.transform.GetChild(0).GetComponent<Text>().text = nickName + ":";
            item.transform.GetChild(1).GetComponent<Text>().text = content;
            if (position == PlayerPosition.Other)
            {
                item.transform.GetChild(0).GetComponent<Text>().text = string.Empty;
                item.transform.GetChild(1).GetComponent<Text>().text = string.Empty;
                item.transform.GetChild(2).GetComponent<Text>().text = content;
            }
            item.transform.SetParent(scrollRect.content, false);
            item.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -chatItems.Count * 150);
            chatItems.Add(item);
            scrollRect.content.sizeDelta = new Vector2(0, chatItems.Count * 150);
            scrollRect.DOVerticalNormalizedPos(0, 0.2f);

            Package p = new Package
            {
                Function = (int)MDPro3.Duel.YGOSharp.GameMessage.sibyl_chat,
                Data = new BinaryMaster()
            };
            p.Data.writer.Write(player);
            p.Data.writer.WriteUnicode(content, content.Length + 1);
            TcpHelper.AddRecordLine(p);
            if (Runtime.Program.instance.ocgcore.showing)
                Runtime.Program.instance.ocgcore.Chat(player, content);
            if (Runtime.Program.instance.online.showing)
                Runtime.MessageManager.Cast(content);
        }

        private static int GetRoomPlayerIndex(int player)
        {
            if (!Runtime.Program.instance.ocgcore.showing)
                return player;
            if (player > -1 && player < 4)
            {
                var swapMask = Controllers.RoomServant.Mode == 2 ? 2 : 1;
                if (Controllers.RoomServant.SelfType == 7)
                {
                    if (!Controllers.OcgCore.isFirst)
                        return player ^ swapMask;
                    else
                        return player;
                }
                if (InFirst() && !Controllers.OcgCore.isFirst)
                    return player ^ swapMask;
                if(!InFirst() && Controllers.OcgCore.isFirst)
                    return player ^ swapMask;
            }
            return player;
        }

        private static bool InFirst()
        {
            if(Controllers.RoomServant.Mode < 2)
                return Controllers.RoomServant.SelfType == 0;
            else
                return Controllers.RoomServant.SelfType == 0 || Controllers.RoomServant.SelfType == 1;
        }

        private static string GetPlayerConfigName(PlayerPosition position)
        {
            return position switch
            {
                PlayerPosition.Me => ReadPlayerConfigName("DuelPlayerName0"),
                PlayerPosition.MyTag => ReadPlayerConfigName("DuelPlayerName0Tag"),
                PlayerPosition.Op => ReadPlayerConfigName("DuelPlayerName1"),
                PlayerPosition.OpTag => ReadPlayerConfigName("DuelPlayerName1Tag"),
                PlayerPosition.WatchMe => ReadPlayerConfigName("WatchPlayerName0"),
                PlayerPosition.WatchMyTag => ReadPlayerConfigName("WatchPlayerName0Tag"),
                PlayerPosition.WatchOp => ReadPlayerConfigName("WatchPlayerName1"),
                PlayerPosition.WatchOpTag => ReadPlayerConfigName("WatchPlayerName1Tag"),
                _ => string.Empty,
            };
        }

        private static string ReadPlayerConfigName(string key) => MDPro3.Config.Have(key)
            ? MDPro3.Config.Get(key, MDPro3.Config.EMPTY_STRING) : MDPro3.Config.EMPTY_STRING;

        public static string GetPlayerName(int player)
        {
            PlayerPosition playerPosition = GetPlayerPosition(player);
            player = GetRoomPlayerIndex(player);
            string nickName = string.Empty;
            switch (player)
            {
                case -1: //local name
                    nickName = MDPro3.Config.Get("DuelPlayerName0", MDPro3.Config.EMPTY_STRING);
                    break;
                case 0: //from host
                case 1: //from client
                case 2: //host tag
                case 3: //client tag
                    nickName = Controllers.RoomServant.players[player].name;
                    var configName = GetPlayerConfigName(playerPosition);
                    if (configName.Length > 0)
                        nickName = configName;
                    break;
                case 7: //observer
                    nickName += MDPro3.InterString.Get("观战者");
                    break;
                case 8: //system custom message, no prefix.
                    nickName += "[System]";
                    break;
                case 9: //error message
                    nickName += "[Script error]";
                    break;
                default: //from watcher or unknown
                    nickName += "[---]";
                    break;
            }
            return nickName;
        }

        public static PlayerPosition GetPlayerPosition(int player)
        {
            player = GetRoomPlayerIndex(player);
            PlayerPosition position;
            if (player < 4)
            {
                if (Controllers.RoomServant.Mode < 2)
                {
                    if (Controllers.RoomServant.SelfType != 7)
                    {
                        if (Controllers.RoomServant.SelfType == player)
                            position = PlayerPosition.Me;
                        else
                            position = PlayerPosition.Op;
                    }
                    else
                    {
                        if (player == 0)
                            position = PlayerPosition.WatchMe;
                        else
                            position = PlayerPosition.WatchOp;
                    }
                }
                else
                {
                    if (Controllers.RoomServant.SelfType != 7)
                    {
                        if (Controllers.RoomServant.SelfType == player)
                            position = PlayerPosition.Me;
                        else if ((Controllers.RoomServant.SelfType + player) % 4 == 1)
                            position = PlayerPosition.MyTag;
                        else
                        {
                            if (player == 0 || player == 2)
                                position = PlayerPosition.Op;
                            else
                                position = PlayerPosition.OpTag;
                        }
                    }
                    else
                    {
                        if (player == 0)
                            position = PlayerPosition.WatchMe;
                        else if (player == 1)
                            position = PlayerPosition.WatchMyTag;
                        else if (player == 2)
                            position = PlayerPosition.WatchOp;
                        else
                            position = PlayerPosition.WatchOpTag;
                    }
                }
            }
            else
                position = PlayerPosition.Other;
            return position;
        }

    }
}
