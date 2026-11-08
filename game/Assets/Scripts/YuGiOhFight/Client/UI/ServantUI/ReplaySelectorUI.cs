using Percy;
using System;
using System.Collections.Generic;
using System.IO;
using SevenZip.Compression.LZMA;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using TMPro;


using YuGiOhFight.Client.Runtime.Network;


namespace YuGiOhFight.Client.UI.ServantUI
{
    public class ReplaySelectorUI : ServantUI
    {

        #region Elements

        private const string LABEL_SR = "ScrollRect";
        private ScrollRect m_ScrollRect;
        private ScrollRect ScrollRect =>
            m_ScrollRect = m_ScrollRect != null ? m_ScrollRect
            : Manager.GetElement<ScrollRect>(LABEL_SR);

        private const string LABEL_TXT_OVERVIEW = "TextOverview";
        private TextMeshProUGUI m_TextOverview;
        public TextMeshProUGUI TextOverview =>
            m_TextOverview = m_TextOverview != null ? m_TextOverview
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_OVERVIEW);

        private const string LABEL_SBN_PLAYER0 = "ButtonPlayer0";
        private SelectionButton m_ButtonPlayer0;
        public SelectionButton ButtonPlayer0 =>
            m_ButtonPlayer0 = m_ButtonPlayer0 != null ? m_ButtonPlayer0
            : Manager.GetElement<SelectionButton>(LABEL_SBN_PLAYER0);

        private const string LABEL_SBN_PLAYER1 = "ButtonPlayer1";
        private SelectionButton m_ButtonPlayer1;
        public SelectionButton ButtonPlayer1 =>
            m_ButtonPlayer1 = m_ButtonPlayer1 != null ? m_ButtonPlayer1
            : Manager.GetElement<SelectionButton>(LABEL_SBN_PLAYER1);

        private const string LABEL_SBN_PLAYER2 = "ButtonPlayer2";
        private SelectionButton m_ButtonPlayer2;
        public SelectionButton ButtonPlayer2 =>
            m_ButtonPlayer2 = m_ButtonPlayer2 != null ? m_ButtonPlayer2
            : Manager.GetElement<SelectionButton>(LABEL_SBN_PLAYER2);

        private const string LABEL_SBN_PLAYER3 = "ButtonPlayer3";
        private SelectionButton m_ButtonPlayer3;
        public SelectionButton ButtonPlayer3 =>
            m_ButtonPlayer3 = m_ButtonPlayer3 != null ? m_ButtonPlayer3
            : Manager.GetElement<SelectionButton>(LABEL_SBN_PLAYER3);

        private const string LABEL_SBN_SORT = "ButtonSort";
        private SelectionButton m_ButtonSort;
        private SelectionButton ButtonSort =>
            m_ButtonSort = m_ButtonSort != null ? m_ButtonSort
            : Manager.GetElement<SelectionButton>(LABEL_SBN_SORT);

        private const string LABEL_SBN_GODVIEW = "ButtonGodView";
        private SelectionButton m_ButtonGodView;
        public SelectionButton ButtonGodView =>
            m_ButtonGodView = m_ButtonGodView != null ? m_ButtonGodView
            : Manager.GetElement<SelectionButton>(LABEL_SBN_GODVIEW);

        private const string LABEL_IMG_HOVER = "ButtonHover";
        private Image m_ImageHover;
        public Image ImageHover =>
            m_ImageHover = m_ImageHover != null ? m_ImageHover
            : Manager.GetElement<Image>(LABEL_IMG_HOVER);

        private const string LABEL_IMG_OUT = "ButtonOut";
        private Image m_ImageOut;
        public Image ImageOut =>
            m_ImageOut = m_ImageOut != null ? m_ImageOut
            : Manager.GetElement<Image>(LABEL_IMG_OUT);

        #endregion

        public SuperScrollView superScrollView;
        private readonly Dictionary<string, YRP> cachedYRPs = new();
        private bool sortByName = true;
        private Runtime.PercyOCG percy;

        public void KF_Replay(string name, bool god = false)
        {
            Services.DeckAcademy.NotifyReplayStarted(name);
            Controllers.OcgCore.CurrentReplayGodView = god;
            string fileName = Runtime.Program.PATH_REPLAY + name + (name.EndsWith(Runtime.Program.EXPANSION_YRP) ? string.Empty : Runtime.Program.EXPANSION_YRP3D);
            if (!File.Exists(fileName))
            {
                fileName = fileName.Replace(Runtime.Program.EXPANSION_YRP3D, Runtime.Program.EXPANSION_YRP);
                if (!File.Exists(fileName))
                    return;
            }
            bool yrp3d = fileName.Length > 6 && fileName.ToLower().Substring(fileName.Length - 6, 6) == Runtime.Program.EXPANSION_YRP3D;
            try
            {
                if (yrp3d)
                {
                    if (god)
                    {
                        Runtime.MessageManager.Cast(MDPro3.InterString.Get("您正在观看旧版的回放（上帝视角），不保证稳定性。"));
                        percy?.Dispose();
                        percy = new Runtime.PercyOCG();
                        var replays = GetYRPBuffer(fileName);
                        var collections = TcpHelper.GetPackages(percy.ygopro.GetYRP3dBuffer(GetYRP(replays[^1])));
                        Services.ReplayAppearance.PreserveMetadata(collections, TcpHelper.ReadPackagesInRecord(fileName));
                        PushCollection(collections);
                    }
                    else
                    {
                        var replays = GetYRPBuffer(fileName);
                        if (replays.Count == 0)
                            Controllers.OcgCore.CurrentReplayUseYRP2 = true;
                        else
                            Controllers.OcgCore.CurrentReplayUseYRP2 = GetYRP(replays[^1]).IsNew();

                        var collection = TcpHelper.ReadPackagesInRecord(fileName);
                        PushCollection(collection);
                    }
                }
                else
                {
                    Runtime.MessageManager.Cast(MDPro3.InterString.Get("您正在观看旧版的回放（上帝视角），不保证稳定性。"));
                    percy?.Dispose();
                    percy = new Runtime.PercyOCG();
                    var collections = TcpHelper.GetPackages(percy.ygopro.GetYRP3dBuffer(GetYRP(File.ReadAllBytes(fileName))));
                    PushCollection(collections);
                }
            }
            catch (ReplayConversionException e)
            {
                Debug.LogWarning("[Replay] " + e.Message);
                if (yrp3d)
                {
                    var recorded = TcpHelper.ReadPackagesInRecord(fileName);
                    if (recorded.Exists(packet => packet.Function == (int)MDPro3.Duel.YGOSharp.GameMessage.Start
                        || packet.Function == (int)MDPro3.Duel.YGOSharp.GameMessage.ReloadField))
                    {
                        Controllers.OcgCore.CurrentReplayGodView = false;
                        PushCollection(recorded);
                        Runtime.MessageManager.Cast("此录像无法重建上帝视角，已按原记录播放。");
                        return;
                    }
                }
                Runtime.MessageManager.Cast("此旧录像无法重新模拟，请使用原始 YRP3D 录像播放。");
            }
            catch(Exception e)
            {
                Debug.LogWarning("[Replay] " + e.Message);
                Runtime.MessageManager.Cast(MDPro3.InterString.Get("回放没有录制完整。"));
            }
        }

        private List<byte[]> GetYRPBuffer(string path)
        {
            if (path.EndsWith(Runtime.Program.EXPANSION_YRP))
                return new List<byte[]>() { File.ReadAllBytes(path) };
            var returnValue = new List<byte[]>();
            try
            {
                var collection = TcpHelper.ReadPackagesInRecord(path);
                foreach (Runtime.Network.Package item in collection)
                    if (item.Function == (int)MDPro3.Duel.YGOSharp.GameMessage.sibyl_replay)
                    {
                        var replay = item.Data.reader.ReadToEnd();
                        // TODO: don't include other replays
                        returnValue.Add(replay);
                    }
            }
            catch (Exception e) { Debug.LogError(e); }
            return returnValue;
        }

        private YRP GetYRP(byte[] buffer)
        {
            var returnValue = new YRP();
            try
            {
                var reader = new BinaryReader(new MemoryStream(buffer));
                returnValue.ID = reader.ReadInt32();
                returnValue.Version = reader.ReadInt32();
                returnValue.Flag = reader.ReadInt32();
                returnValue.Seed = reader.ReadUInt32();
                returnValue.DataSize = reader.ReadInt32();
                returnValue.Hash = reader.ReadInt32();
                returnValue.Props = reader.ReadBytes(8);
                if (returnValue.ID == 0x32707279) // REPLAY_ID_YRP2
                {
                    for (int i = 0; i < 8; i++)
                    {
                        returnValue.SeedsV2[i] = reader.ReadUInt32();
                    }
                    for (int i = 0; i < 4; i++) // other flags, unused for now
                    {
                        reader.ReadUInt32();
                    }
                }
                var raw = reader.ReadToEnd();
                if ((returnValue.Flag & 0x1) > 0)
                {
                    var lzma = new Decoder();
                    lzma.SetDecoderProperties(returnValue.Props);
                    var decompressed = new MemoryStream();
                    lzma.Code(new MemoryStream(raw), decompressed, raw.LongLength, returnValue.DataSize, null);
                    raw = decompressed.ToArray();
                }

                reader = new BinaryReader(new MemoryStream(raw));
                if ((returnValue.Flag & 0x2) > 0)
                {
                    Controllers.RoomServant.Mode = 2;
                    returnValue.playerData.Add(new YRP.PlayerData());
                    returnValue.playerData.Add(new YRP.PlayerData());
                    returnValue.playerData.Add(new YRP.PlayerData());
                    returnValue.playerData.Add(new YRP.PlayerData());
                    returnValue.playerData[0].name = reader.ReadUnicode(20);
                    returnValue.playerData[1].name = reader.ReadUnicode(20);
                    returnValue.playerData[2].name = reader.ReadUnicode(20);
                    returnValue.playerData[3].name = reader.ReadUnicode(20);
                    returnValue.StartLp = reader.ReadInt32();
                    returnValue.StartHand = reader.ReadInt32();
                    returnValue.DrawCount = reader.ReadInt32();
                    returnValue.opt = reader.ReadUInt32();
                    Controllers.OcgCore.MasterRule = (int)(returnValue.opt >> 16);
                    for (var i = 0; i < 4; i++)
                    {
                        var count = reader.ReadInt32();
                        for (var i2 = 0; i2 < count; i2++) returnValue.playerData[i].main.Add(reader.ReadInt32());
                        count = reader.ReadInt32();
                        for (var i2 = 0; i2 < count; i2++) returnValue.playerData[i].extra.Add(reader.ReadInt32());
                    }
                }
                else
                {
                    returnValue.playerData.Add(new YRP.PlayerData());
                    returnValue.playerData.Add(new YRP.PlayerData());
                    returnValue.playerData[0].name = reader.ReadUnicode(20);
                    returnValue.playerData[1].name = reader.ReadUnicode(20);
                    returnValue.StartLp = reader.ReadInt32();
                    returnValue.StartHand = reader.ReadInt32();
                    returnValue.DrawCount = reader.ReadInt32();
                    returnValue.opt = reader.ReadUInt32();
                    Controllers.OcgCore.MasterRule = (int)(returnValue.opt >> 16);
                    for (var i = 0; i < 2; i++)
                    {
                        var count = reader.ReadInt32();
                        for (var i2 = 0; i2 < count; i2++) returnValue.playerData[i].main.Add(reader.ReadInt32());
                        count = reader.ReadInt32();
                        for (var i2 = 0; i2 < count; i2++) returnValue.playerData[i].extra.Add(reader.ReadInt32());
                    }
                }
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                    returnValue.gameData.Add(reader.ReadBytes(reader.ReadByte()));
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }

            return returnValue;
        }

        public YRP CacheYRP(string replay)
        {
            if (cachedYRPs.ContainsKey(replay))
                return cachedYRPs[replay];
            YRP yrp;
            if (File.Exists(Runtime.Program.PATH_REPLAY + replay))
                yrp = GetYRP(File.ReadAllBytes(Runtime.Program.PATH_REPLAY + replay));
            else
            {
                var buffer = GetYRPBuffer(Runtime.Program.PATH_REPLAY + replay + Runtime.Program.EXPANSION_YRP3D);
                if (buffer.Count == 0)
                    yrp = null;
                else
                    yrp = GetYRP(buffer[0]);
            }
            if (yrp != null)
                cachedYRPs.Add(replay, yrp);
            return yrp;
        }

        private void PushCollection(List<Runtime.Network.Package> collection)
        {
            Services.ReplayAppearance.Prepare(collection);
            Runtime.Program.instance.ocgcore.returnServant = Runtime.Program.instance.replay;
            Controllers.OcgCore.handler = a => { };
            Controllers.OcgCore.name_0 = MDPro3.Config.Get("ReplayPlayerName0", MDPro3.Config.EMPTY_STRING);
            Controllers.OcgCore.name_0_tag = MDPro3.Config.Get("ReplayPlayerName0Tag", "---");
            Controllers.OcgCore.name_0_c = Controllers.OcgCore.name_0;
            Controllers.OcgCore.name_1 = MDPro3.Config.Get("ReplayPlayerName1", MDPro3.Config.EMPTY_STRING);
            Controllers.OcgCore.name_1_tag = MDPro3.Config.Get("ReplayPlayerName1Tag", "---");
            Controllers.OcgCore.name_1_c = Controllers.OcgCore.name_1;
            Controllers.OcgCore.timeLimit = 240;
            Controllers.OcgCore.lpLimit = 8000;
            Controllers.OcgCore.isFirst = true;
            Controllers.OcgCore.condition = Controllers.OcgCore.Condition.Replay;
            Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.ocgcore);
            Runtime.Program.instance.ocgcore.FlushPackages(collection);
        }

        private void SelectZero()
        {
            SelectionToggle_Replay item0 = superScrollView.items[0].gameObject.GetComponent<SelectionToggle_Replay>();
            item0.SetToggleOn();
            Runtime.Program.instance.replay.lastSelectedReplayItem = item0;
        }

        public void Print()
        {
            superScrollView?.Clear();

            if (!Directory.Exists(Runtime.Program.PATH_REPLAY))
                Directory.CreateDirectory(Runtime.Program.PATH_REPLAY);
            var fileInfos = new DirectoryInfo(Runtime.Program.PATH_REPLAY).GetFiles();
            if (sortByName)
                Array.Sort(fileInfos, Utility.Tools.CompareName);
            else
                Array.Sort(fileInfos, Utility.Tools.CompareTime);

            List<string[]> tasks = new List<string[]>();
            int count = 0;
            for (int i = 0; i < fileInfos.Length; i++)
            {
                if (fileInfos[i].Name.EndsWith(Runtime.Program.EXPANSION_YRP3D))
                {
                    var task = new string[] { count.ToString(), fileInfos[i].Name.Replace(Runtime.Program.EXPANSION_YRP3D, string.Empty) };
                    tasks.Add(task);
                    count++;
                }
                else if (fileInfos[i].Name.EndsWith(Runtime.Program.EXPANSION_YRP))
                {
                    var task = new string[] { count.ToString(), fileInfos[i].Name };
                    tasks.Add(task);
                    count++;
                }
            }

            var handle = Addressables.LoadAssetAsync<GameObject>("UI/ItemReplay.prefab");
            handle.Completed += (result) =>
            {
                var itemHeight = PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 180f : 150f;
                float topPadding = PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 148f : 134f;
                float space = itemHeight - (PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 152f : 122f);
                float bottomPadding = (PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 64f : 54f) - space;
                superScrollView = new SuperScrollView(
                        1,
                        700,
                        itemHeight,
                        topPadding,
                        bottomPadding,
                        result.Result,
                        ItemOnListRefresh,
                        Manager.GetElement<ScrollRect>("ScrollRect"));
                superScrollView.Print(tasks);
                if (tasks.Count > 0)
                    SelectZero();
            };
        }

        private void ItemOnListRefresh(string[] task, GameObject item)
        {
            SelectionToggle_Replay handler = item.GetComponent<SelectionToggle_Replay>();
            handler.index = int.Parse(task[0]);
            handler.replayName = task[1];
            handler.Refresh();
        }

        public void OnRename()
        {
            var selections = new List<string>()
            {
                MDPro3.InterString.Get("请输入新的回放名称"),
                superScrollView.items[superScrollView.selected].args[1].Replace(Runtime.Program.EXPANSION_YRP, string.Empty)
            };
            Runtime.UIManager.ShowPopupInput(selections, ReplayRename, null, TmpInputValidation.ValidationType.Path);
        }

        private void ReplayRename(string newName)
        {
            string replay = superScrollView.items[superScrollView.selected].args[1];
            if (replay.EndsWith(Runtime.Program.EXPANSION_YRP))
                File.Move(Runtime.Program.PATH_REPLAY + replay, Runtime.Program.PATH_REPLAY + newName + Runtime.Program.EXPANSION_YRP);
            else
                File.Move(Runtime.Program.PATH_REPLAY + replay + Runtime.Program.EXPANSION_YRP3D, Runtime.Program.PATH_REPLAY + newName + Runtime.Program.EXPANSION_YRP3D);
            Print();
        }

        public void OnPlay()
        {
            KF_Replay(superScrollView.items[superScrollView.selected].args[1]);
        }

        public void OnGodView()
        {
            KF_Replay(superScrollView.items[superScrollView.selected].args[1], true);
        }

        public void OnDelete()
        {
            var replay = superScrollView.items[superScrollView.selected].args[1];
            if (File.Exists(Runtime.Program.PATH_REPLAY + replay))
                File.Delete(Runtime.Program.PATH_REPLAY + replay);
            else
                File.Delete(Runtime.Program.PATH_REPLAY + replay + Runtime.Program.EXPANSION_YRP3D);
            Runtime.MessageManager.Cast(MDPro3.InterString.Get("已删除回放「[?]」。", replay));
            Print();
        }

        public void OnSort()
        {
            sortByName = !sortByName;
            if (sortByName)
                ButtonSort.SetButtonText(MDPro3.InterString.Get("名称排序"));
            else
                ButtonSort.SetButtonText(MDPro3.InterString.Get("时间排序"));

            Print();
        }

        public void OnDeck(int player)
        {
            var replay = superScrollView.items[superScrollView.selected].args[1];
            var yrp = cachedYRPs[replay];
            replay = replay.Replace(Runtime.Program.EXPANSION_YRP, string.Empty);

            var deckName = replay + "_" + yrp.playerData[player].name;
            MDPro3.Duel.YGOSharp.Deck deck = new MDPro3.Duel.YGOSharp.Deck(yrp.playerData[player].main, yrp.playerData[player].extra, new List<int>());

            Runtime.Program.instance.deckEditor.SwitchCondition(Controllers.DeckEditor.Condition.ReplayDeck, deckName, deck);
            Runtime.Program.instance.ShiftToServant(Runtime.Program.instance.deckEditor);
        }

        public void SelectLastReplayItem()
        {
            Runtime.UserInput.NextSelectionIsAxis = true;
            if (Runtime.Program.instance.replay.lastSelectedReplayItem != null)
                Runtime.Program.instance.replay.lastSelectedReplayItem.GetSelectable().Select();
        }

    }
}
