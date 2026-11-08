using Cysharp.Threading.Tasks;


using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static YuGiOhFight.Client.Presentation.Voice.VoicePlayer;


using YuGiOhFight.Client.Runtime.Network;


namespace YuGiOhFight.Client.Duel
{
    public class VoiceMessage : MessageProcessor
    {
        public VoiceMessage(MessageDispatcher dispatcher) : base(dispatcher) { }

        #region Parameters

        private bool isFirst;
        private int MasterRule;
        private int life0;
        private int life1;
        private int turns;
        private bool myTurn;
        private MDPro3.Duel.YGOSharp.DuelPhase duelPhase;

        private readonly List<GameCard> cardsInChain = new();
        private readonly List<GameCard> cardsBeTarget = new();

        private bool ignoreNextChaining;
        private bool inPendulumSummon;
        private bool ignoreNextPendulumSummon;
        private bool lastVoiceIsRelease;

        private readonly List<VoiceData> voiceData = new();

        #endregion

        #region Tools

        private void LogDebug(string text)
        {
            Runtime.Program.Debug(text);
        }

        private void DebugNoCard()
        {
            LogDebug($"[Duel Voice]: Not found card for {(Controllers.OcgCore.currentMessage)}.");
        }

        private void ResetState()
        {
            ignoreNextChaining = false;
            inPendulumSummon = false;
            ignoreNextPendulumSummon = false;
            lastVoiceIsRelease = false;

            cardsInChain.Clear();
            cardsBeTarget.Clear();
        }

        private async UniTask PlayVoiceAsync()
        {
            int faceVersion = Core.CharacterFacePresentationVersion;
            var data = new List<VoiceData>(voiceData);
            string hero = heroString;
            string rival = rivalString;
            bool IsCurrent() => Core.showing && NeedVoice()
                && faceVersion == Core.CharacterFacePresentationVersion
                && hero == heroString && rival == rivalString;
            var paths = GetVoicePaths(data);
            var clips = new List<AudioClip>[paths.Length];
            for (int i = 0; i < clips.Length; i++)
                clips[i] = new List<AudioClip>();

            for (int i = 0; i < paths.Length; i++)
            {
                for (int j = 0; j < paths[i].Count; j++)
                {
                    try
                    {
                        var clip = await Runtime.AudioManager.LoadAudioFileUniAsync(paths[i][j], AudioType.OGGVORBIS);
                        clips[i].Add(clip);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }

            for (int i = 0; i < clips.Length; i++)
            {
                for (int j = 0; j < clips[i].Count; j++)
                {
                    if (j == 0)
                        await UniTask.WaitForSeconds(data[i].delay);
                    if (!IsCurrent()) return;

                    Presentation.Voice.LineInfo line = GetLine(Path.GetFileNameWithoutExtension(paths[i][j]), data[i].isHero);
                    if (line != null)
                    {
                        var item = Runtime.ABLoader.LoadMasterDuelGameObject(data[i].isHero ? "DuelChatItemMe" : "DuelChatItemOp");

                        item.transform.SetParent(Core.servantUI.Root, false);
                        UI.ChatItemHandler handler = item.GetComponent<UI.ChatItemHandler>();
                        handler.text = line.text;

                        if (clips[i][j] == null)
                        {
                            Debug.LogError("Voice File " + paths[i][j] + " not Found!");
                            return;
                        }

                        handler.time = clips[i][j].length;
                        handler.frame = line.frame;
                        if (data[i].isHero)
                        {
                            if (Core.duelChat0 != null)
                                Core.duelChat0.BeGray();
                            Core.duelChat0 = handler;
                        }
                        else
                        {
                            if (Core.duelChat1 != null)
                                Core.duelChat1.BeGray();
                            Core.duelChat1 = handler;
                        }

                        Core.SetCharacterFace(data[i].isHero ? hero : rival, line.face, data[i].isHero, 0f);
                        Core.SetCharacterFace(data[i].isHero ? hero : rival, 1, data[i].isHero, clips[i][j].length - 0.1f);
                    }

                    Runtime.AudioManager.PlayVoice(clips[i][j]);

                    if (data[i].wait)
                        await UniTask.WaitForSeconds(clips[i][j].length);
                }
            }
        }

        private bool NeedVoice()
        {
            return MDPro3.Config.GetBool(Controllers.OcgCore.condition + "Voice", false);
        }

        private static bool DamageIsBig(int damage)
        {
            if (damage >= 2000)
                return true;
            else
                return false;
        }

        private static bool TryGetRandomVoice(Presentation.Voice.VoiceInfoEntry entry, out string voiceName)
        {
            voiceName = null;
            if (entry == null || entry.rawKvp == null || entry.rawKvp.Count == 0)
                return false;

            voiceName = Utility.Tools.GetRandomDictionaryElement(entry.rawKvp).Value.shortName;
            return !string.IsNullOrEmpty(voiceName);
        }

        private int LocalPlayer(int player)
        {
            if (player == 0 || player == 1)
            {
                if (isFirst)
                    return player;
                return 1 - player;
            }
            return player;
        }

        private static Presentation.Voice.VoicesData GetVoiceSet(bool isHero)
        {
            return isHero ? heroVoices : rivalVoices;
        }

        private int GetLeadingState(bool isHero)
        {
            return isHero ? LeadingStateOfHero() : LeadingStateOfRival();
        }

        private static int ResolveVoiceCardCode(GameCard card, int code)
        {
            if (code != 0 || card == null)
                return code;
            return card.GetValidData().Id;
        }

        #endregion

        #region Message Process

        public override async UniTask Process(Package p)
        {
            // Static character portraits remain visible with voices muted.
            // Unchanged selections are deduplicated inside the core.
            Core.SetCharacterDefaultFace();
            if (NeedVoice())
            {
                LoadData();
            }
            else
            {
                return;
            }

            voiceData.Clear();

            await base.Process(p);

            if (voiceData.Count == 0)
                return;

            var voiceTask = PlayVoiceAsync();
            var clickTask = UniTask.WaitUntil(() => Runtime.UserInput.MouseLeftDown);

            await UniTask.WhenAny(voiceTask, clickTask);
        }

        protected override UniTask GameMessage_Start(BinaryReader reader)
        {
            ResetState();

            isFirst = (reader.ReadByte() & 0xF) == 0;
            if (reader.BaseStream.Length > 17)
                MasterRule = reader.ReadByte() + 1;
            life0 = reader.ReadInt32();
            life1 = reader.ReadInt32();
            turns = 0;

            VoiceData data = new();
            data.name = GetVoiceByDuelist(heroVoices.BeforeDuel, heroVoices.BeforeDuelSp, rivalCode);
            data.num = GetVoiceNum(heroVoices, data.name);
            data.isHero = true;
            data.wait = true;
            data.delay = 0f;
            voiceData.Add(data);

            VoiceData data2 = new();
            data2.name = GetVoiceByDuelist(rivalVoices.BeforeDuel, rivalVoices.BeforeDuelSp, heroCode);
            data2.num = GetVoiceNum(rivalVoices, data2.name);
            data2.isHero = false;
            data2.wait = true;
            data2.delay = 0f;
            voiceData.Add(data2);

            VoiceData data3 = new();
            data3.name = Utility.Tools.GetRandomDictionaryElement(heroVoices.DuelStart.rawKvp).Value.shortName;
            data3.num = GetVoiceNum(heroVoices, data3.name);
            data3.isHero = true;
            data3.wait = false;
            data3.delay = 1f;
            voiceData.Add(data3);

            VoiceData data4 = new();
            data4.name = Utility.Tools.GetRandomDictionaryElement(rivalVoices.DuelStart.rawKvp).Value.shortName;
            data4.num = GetVoiceNum(rivalVoices, data4.name);
            data4.isHero = false;
            data4.wait = true;
            data4.delay = 0f;
            voiceData.Add(data4);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_ReloadField(BinaryReader reader)
        {
            ResetState();

            if (Controllers.OcgCore.inPuzzle)
            {
                isFirst = true;
                myTurn = true;
            }

            MasterRule = reader.ReadByte() + 1;
            if (MasterRule > 255)
                MasterRule -= 255;

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_NewTurn(BinaryReader reader)
        {
            turns++;
            myTurn = isFirst ? (turns % 2 != 0) : (turns % 2 == 0);

            if (turns == 1)
                return UniTask.CompletedTask;

            Presentation.Voice.VoicesData targetData = GetVoiceSet(myTurn);
            var leadingState = GetLeadingState(myTurn);

            VoiceData data = new VoiceData
            {
                name = GetVoiceBySituation(targetData.TurnStart, leadingState),
                isHero = myTurn,
                wait = true,
                delay = 0f,
            };
            data.num = GetVoiceNum(targetData, data.name);
            voiceData.Add(data);
            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_NewPhase(BinaryReader reader)
        {
            duelPhase = (MDPro3.Duel.YGOSharp.DuelPhase)reader.ReadUInt16();
            if(duelPhase != MDPro3.Duel.YGOSharp.DuelPhase.BattleStart && duelPhase != MDPro3.Duel.YGOSharp.DuelPhase.End)
                return UniTask.CompletedTask;

            Presentation.Voice.VoicesData targetData = GetVoiceSet(myTurn);
            VoiceData data = new VoiceData();
            if(duelPhase == MDPro3.Duel.YGOSharp.DuelPhase.BattleStart)
                data.name = Utility.Tools.GetRandomDictionaryElement(targetData.BattleStart.rawKvp).Value.shortName;
            else if(duelPhase == MDPro3.Duel.YGOSharp.DuelPhase.End)
            {
                var leadingState = GetLeadingState(myTurn);
                data.name = GetVoiceBySituation(targetData.TurnEnd, leadingState);
            }
            data.num = GetVoiceNum(targetData, data.name);
            data.isHero = myTurn;
            data.wait = true;
            data.delay = 0f;
            voiceData.Add(data);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Win(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            if(player == 2)
                return UniTask.CompletedTask;

            Presentation.Voice.VoicesData targetData;
            Presentation.Voice.VoicesData targetData2;

            if (player == 0)
            {
                targetData = heroVoices;
                targetData2 = rivalVoices;
            }
            else
            {
                targetData = rivalVoices;
                targetData2 = heroVoices;
            }

            VoiceData data = new VoiceData();
            data.name = GetVoiceByDuelist(targetData.Win, targetData.WinSp, player == 0 ? rivalCode : heroCode);
            data.num = GetVoiceNum(targetData, data.name);
            data.isHero = player == 0;
            data.wait = true;
            data.delay = 0f;
            voiceData.Add(data);

            VoiceData data2 = new VoiceData();
            data2.name = GetVoiceByDuelist(targetData2.Lose, targetData2.LoseSp, player == 0 ? heroCode : rivalCode);
            data2.num = GetVoiceNum(targetData2, data2.name);
            data2.isHero = !data.isHero;
            data2.wait = true;
            data2.delay = 0f;
            voiceData.Add(data2);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_PosChange(BinaryReader reader)
        {
            Package nextPack = Controllers.OcgCore.GetNextPackage();
            if(nextPack == null)
                return UniTask.CompletedTask;
            //仅检测盖卡打开
            if ((MDPro3.Duel.YGOSharp.GameMessage)nextPack.Function != MDPro3.Duel.YGOSharp.GameMessage.Chaining)
                return UniTask.CompletedTask;

            /*var code = */reader.ReadUInt32();
            GPS gps = reader.ReadGPS();
            GameCard card = Core.GCS_Get(gps);

            nextPack.Data.reader.BaseStream.Seek(0, 0);
            var code = ResolveVoiceCardCode(card, nextPack.Data.reader.ReadInt32());
            GPS nextGps = nextPack.Data.reader.ReadGPS();
            Presentation.Voice.VoicesData target = GetVoiceSet(nextGps.InMyControl());
            VoiceData data = GetVoiceByCard(target, target.MainMonsterEffect, code, 0, nextGps.InMyControl());
            if (data.name != string.Empty)
            {
                voiceData.Add(data);
                ignoreNextChaining = true;
                return UniTask.CompletedTask;
            }

            data = GetVoiceByCard(target, target.MainMagicTrap, code, 0, nextGps.InMyControl());
            if (data.name != string.Empty)
            {
                voiceData.Add(data);
                ignoreNextChaining = true;
                return UniTask.CompletedTask;
            }

            if (NeedBeforeCardEffect(gps.InMyControl()))
                voiceData.Add(GetBeforeCardEffectData(target, gps.InMyControl()));

            data = new VoiceData();
            data.name = GetVoiceBySubCategory(target.CardEffect, (int)MDPro3.VoiceController.CardEffectSub.Reverse, (int)MDPro3.VoiceController.CardEffectSub.Reverse, 0);
            data.num = GetVoiceNum(target, data.name);
            data.isHero = gps.InMyControl();
            data.wait = true;
            data.delay = 0f;
            voiceData.Add(data);

            Presentation.Voice.SimpleVoiceData simple = Presentation.Voice.SimpleVoiceData.GetCardEffectSubCategory(nextPack.Data.reader, false, code);
            VoiceData data2 = new VoiceData();
            data2.name = GetVoiceBySubCategory(target.CardEffect, simple.subCategory, (int)MDPro3.VoiceController.CardEffectSub.Magic, 0);
            data2.num = GetVoiceNum(target, data2.name);
            data2.isHero = data.isHero;
            data2.wait = true;
            data2.delay = 0f;
            voiceData.Add(data2);

            ignoreNextChaining = true;

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Move(BinaryReader reader)
        {
            var code = (int)reader.ReadUInt32();
            GPS from = reader.ReadGPS();
            GPS to = reader.ReadGPS();
            var reason = reader.ReadUInt32();
            GameCard card = Core.GCS_Get(from);
            if(card == null)
            {
                DebugNoCard();
                return UniTask.CompletedTask;
            }

            MDPro3.Duel.YGOSharp.Card cardData = card.GetData();
            Package nextPack = Controllers.OcgCore.GetNextPackage();
            if(nextPack == null)
                return UniTask.CompletedTask;
            nextPack.Data.reader.BaseStream.Seek(0, 0);

            var category = 0;
            var subCategory = 0;
            var subInCase = 0;
            var patternIndex = 0;
            var fromHand = false;
            var isMe = to.InMyControl();

            if ((reason & (uint)MDPro3.Duel.YGOSharp.CardReason.RELEASE) > 0
                && card.GetData().HasType(MDPro3.Duel.YGOSharp.CardType.Monster))
            {
                if (lastVoiceIsRelease)
                    return UniTask.CompletedTask;
                category = (int)MDPro3.VoiceController.Category.Summon;
                subCategory = (int)MDPro3.VoiceController.SummonSub.Release;
                subInCase = subCategory;
                lastVoiceIsRelease = true;
            }
            else
                lastVoiceIsRelease = false;

            if ((MDPro3.Duel.YGOSharp.GameMessage)nextPack.Function == MDPro3.Duel.YGOSharp.GameMessage.Summoning)
            {
                code = nextPack.Data.reader.ReadInt32();
                category = (int)MDPro3.VoiceController.Category.Summon;
                subCategory = (int)MDPro3.VoiceController.SummonSub.Normal;

                isMe = from.InMyControl();
                Presentation.Voice.VoicesData targetDataT = GetVoiceSet(isMe);
                VoiceData data = GetVoiceByCard(targetDataT, targetDataT.MainMonsterSummon, code, 0, isMe);
                if (data.name != string.Empty)
                {
                    voiceData.Add(data);
                    return UniTask.CompletedTask;
                }
                voiceData.Add(GetBeforeSummonData(targetDataT, isMe));
            }

            if ((MDPro3.Duel.YGOSharp.GameMessage)nextPack.Function == MDPro3.Duel.YGOSharp.GameMessage.SpSummoning)
            {
                code = nextPack.Data.reader.ReadInt32();
                cardData = MDPro3.Duel.YGOSharp.CardsManager.Get(code);

                category = (int)MDPro3.VoiceController.Category.Summon;
                subCategory = (int)MDPro3.VoiceController.SummonSub.Special;
                subInCase = subCategory;
                if (Controllers.OcgCore.materialCards.Count > 0)
                {
                    patternIndex = -1;

                    if (cardData.HasType(MDPro3.Duel.YGOSharp.CardType.Link))
                        subCategory = (int)MDPro3.VoiceController.SummonSub.Link;
                    else if (cardData.HasType(MDPro3.Duel.YGOSharp.CardType.Fusion))
                        subCategory = (int)MDPro3.VoiceController.SummonSub.Fusion;
                    else if (cardData.HasType(MDPro3.Duel.YGOSharp.CardType.Synchro))
                        subCategory = (int)MDPro3.VoiceController.SummonSub.Sync;
                    else if (cardData.HasType(MDPro3.Duel.YGOSharp.CardType.Xyz))
                        subCategory = (int)MDPro3.VoiceController.SummonSub.Xyz;
                    else if (cardData.HasType(MDPro3.Duel.YGOSharp.CardType.Ritual))
                        subCategory = (int)MDPro3.VoiceController.SummonSub.Ritual;
                }
                else if (inPendulumSummon)
                {
                    if (ignoreNextPendulumSummon)
                        return UniTask.CompletedTask;
                    subCategory = (int)MDPro3.VoiceController.SummonSub.Pendulum;
                }

                if (from.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand)
                    && subCategory == (int)MDPro3.VoiceController.SummonSub.Special)
                {
                    fromHand = true;
                    isMe = from.InMyControl();
                }

                Presentation.Voice.VoicesData targetDataT = GetVoiceSet(isMe);

                if (subCategory != (int)MDPro3.VoiceController.SummonSub.Special)
                {
                    VoiceData data = GetVoiceByCard(targetDataT, targetDataT.BeforeMainSummon, code, 0, isMe);
                    if (data.name != string.Empty)
                        voiceData.Add(data);
                }

                VoiceData dataT = GetVoiceByCard(targetDataT, targetDataT.MainMonsterSummon, code, 0, isMe);
                if (dataT.name != string.Empty)
                {
                    if (subCategory != (int)MDPro3.VoiceController.SummonSub.Special && voiceData.Count == 0)
                    {
                        var voiceShortName = GetVoiceBySubCategory(targetDataT.Summon, subCategory, (int)MDPro3.VoiceController.SummonSub.Special, 1, true);
                        if (voiceShortName != null)
                        {
                            VoiceData data = new VoiceData();
                            data.name = voiceShortName;
                            data.num = GetVoiceNum(targetDataT, data.name);
                            data.isHero = isMe;
                            data.wait = true;
                            data.delay = 0f;
                            voiceData.Add(data);
                        }
                    }
                    voiceData.Add(dataT);
                    return UniTask.CompletedTask;
                }
            }

            if ((MDPro3.Duel.YGOSharp.GameMessage)nextPack.Function == MDPro3.Duel.YGOSharp.GameMessage.Set)
            {
                category = (int)MDPro3.VoiceController.Category.CardSet;
                if (to.InLocation(MDPro3.Duel.YGOSharp.CardLocation.MonsterZone))
                    subCategory = (int)MDPro3.VoiceController.CardSetSub.Monster;
                else
                    subCategory = (int)MDPro3.VoiceController.CardSetSub.MagicTrap;
                subInCase = subCategory;
            }

            if ((MDPro3.Duel.YGOSharp.GameMessage)nextPack.Function == MDPro3.Duel.YGOSharp.GameMessage.Chaining)
            {
                if (!to.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Onfield))
                    return UniTask.CompletedTask;
                ignoreNextChaining = true;

                code = ResolveVoiceCardCode(card, nextPack.Data.reader.ReadInt32());
                GPS gps = nextPack.Data.reader.ReadGPS();

                Presentation.Voice.VoicesData targetDataT = GetVoiceSet(gps.InMyControl());
                VoiceData data = GetVoiceByCard(targetDataT, targetDataT.MainMonsterEffect, code, 0, gps.InMyControl());
                if (data.name != string.Empty)
                {
                    voiceData.Add(data);
                    return UniTask.CompletedTask;
                }
                data = GetVoiceByCard(targetDataT, targetDataT.MainMagicTrap, code, 0, gps.InMyControl());
                if (data.name != string.Empty)
                {
                    voiceData.Add(data);
                    return UniTask.CompletedTask;
                }

                if (from.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand))
                {
                    fromHand = true;
                    isMe = from.InMyControl();
                }

                if (NeedBeforeCardEffect(isMe))
                    voiceData.Add(GetBeforeCardEffectData(GetVoiceSet(isMe), isMe));

                Presentation.Voice.SimpleVoiceData simple = Presentation.Voice.SimpleVoiceData.GetCardEffectSubCategory(nextPack.Data.reader, fromHand, code);
                category = simple.category;
                subCategory = simple.subCategory;
                subInCase = (int)MDPro3.VoiceController.CardEffectSub.Magic;
                if (subCategory == (int)MDPro3.VoiceController.CardEffectSub.PendulumScale)
                    fromHand = false;
            }

            Presentation.Voice.VoicesData targetData = GetVoiceSet(isMe);
            if(category == 0)
                return UniTask.CompletedTask;
            if (fromHand)
            {
                VoiceData data = new VoiceData();
                data.name = GetVoiceBySubCategory(targetData.CardEffect, (int)MDPro3.VoiceController.CardEffectSub.FromHand, (int)MDPro3.VoiceController.CardEffectSub.FromHand, 0);
                data.num = GetVoiceNum(targetData, data.name);
                data.isHero = isMe;
                data.wait = true;
                data.delay = 0f;
                voiceData.Add(data);
            }
            VoiceData data2 = new VoiceData();
            data2.name = GetVoiceBySubCategory(targetData.GetCategoryEntry((MDPro3.VoiceController.Category)category), subCategory, subInCase, patternIndex);
            data2.num = GetVoiceNum(targetData, data2.name);
            data2.isHero = isMe;
            data2.wait = true;
            data2.delay = 0f;
            voiceData.Add(data2);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Chaining(BinaryReader reader)
        {
            if (ignoreNextChaining)
            {
                ignoreNextChaining = false;
                return UniTask.CompletedTask;
            }

            var code = (int)reader.ReadUInt32();
            GPS gps = reader.ReadGPS();
            GameCard card = Core.GCS_Get(gps);
            if(card == null)
            {
                DebugNoCard();
            }
            cardsInChain.Add(card);
            code = ResolveVoiceCardCode(card, code);

            Presentation.Voice.SimpleVoiceData simple = Presentation.Voice.SimpleVoiceData.GetCardEffectSubCategory(reader, false, code);
            Presentation.Voice.VoicesData targetData = simple.isMe ? heroVoices : rivalVoices;
            if (NeedBeforeCardEffect(simple.isMe))
                voiceData.Add(GetBeforeCardEffectData(targetData, simple.isMe));

            VoiceData data = GetVoiceByCard(targetData, targetData.MainMonsterEffect, code, 0, simple.isMe);
            if(data.name != string.Empty)
            {
                voiceData.Add(data);
                return UniTask.CompletedTask;
            }

            data = GetVoiceByCard(targetData, targetData.MainMagicTrap, code, 0, simple.isMe);
            if (data.name != string.Empty)
            {
                voiceData.Add(data);
                return UniTask.CompletedTask;
            }

            if (simple.inHand)
            {
                VoiceData data2 = new VoiceData();
                data2.name = GetVoiceBySubCategory(targetData.CardEffect, (int)MDPro3.VoiceController.CardEffectSub.FromHand, (int)MDPro3.VoiceController.CardEffectSub.FromHand, 0);
                data2.num = GetVoiceNum(targetData, data2.name);
                data2.isHero = simple.isMe;
                data2.wait = true;
                data2.delay = 0f;
                voiceData.Add(data2);
            }

            VoiceData data3 = new VoiceData();
            data3.name = GetVoiceBySubCategory(targetData.CardEffect, simple.subCategory, (int)MDPro3.VoiceController.CardEffectSub.Magic, 0);
            data3.num = GetVoiceNum(targetData, data3.name);
            data3.isHero = simple.isMe;
            data3.wait = true;
            data3.delay = 0f;
            voiceData.Add(data3);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Draw(BinaryReader reader)
        {
            if(duelPhase != MDPro3.Duel.YGOSharp.DuelPhase.Draw)
                return UniTask.CompletedTask;
            if(turns == 0)
                return UniTask.CompletedTask;
            if(turns == 1 && MasterRule > 2)
                return UniTask.CompletedTask;

            var player = LocalPlayer(reader.ReadByte());
            var isHero = player == 0;
            Presentation.Voice.VoicesData targetData = GetVoiceSet(isHero);
            var leadingState = GetLeadingState(isHero);

            VoiceData data = new VoiceData();
            data.name = GetVoiceBySituation(targetData.Draw, leadingState);
            data.num = GetVoiceNum(targetData, data.name);
            data.isHero = isHero;
            data.wait = true;
            data.delay = 0f;
            voiceData.Add(data);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Damage(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            var value = reader.ReadInt32();
            var isHero = player == 0;
            Presentation.Voice.VoicesData targetData = GetVoiceSet(isHero);
            var cacheLP = isHero ? life0 : life1;

            if (isHero)
                life0 -= value;
            else
                life1 -= value;

            if (value >= cacheLP)
            {
                voiceData.Add(GetFinishDamageVoiceData(targetData, isHero));
                return UniTask.CompletedTask;
            }

            VoiceData data = new VoiceData();
            if(DamageIsBig(value))
                data.name = Utility.Tools.GetRandomDictionaryElement(targetData.BigDamage.rawKvp).Value.shortName;
            else
                data.name = Utility.Tools.GetRandomDictionaryElement(targetData.Damage.rawKvp).Value.shortName;
            data.num = GetVoiceNum(targetData, data.name);
            data.isHero = isHero;
            data.wait = false;
            data.delay = 0f;
            voiceData.Add(data);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_PayLpCost(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            var value = reader.ReadInt32();
            var isHero = player == 0;
            Presentation.Voice.VoicesData targetData = GetVoiceSet(isHero);
            var cacheLP = isHero ? life0 : life1;
            if (isHero)
                life0 -= value;
            else
                life1 -= value;

            if (value >= cacheLP)
            {
                voiceData.Add(GetFinishDamageVoiceData(targetData, isHero));
                return UniTask.CompletedTask;
            }

            VoiceData data = new VoiceData();
            if (!TryGetRandomVoice(targetData.CostDamage, out var voiceName)
                && (!DamageIsBig(value) || !TryGetRandomVoice(targetData.BigDamage, out voiceName))
                && !TryGetRandomVoice(targetData.Damage, out voiceName))
                return UniTask.CompletedTask;

            data.name = voiceName;
            data.num = GetVoiceNum(targetData, data.name);
            data.isHero = isHero;
            data.wait = false;
            data.delay = 0f;
            voiceData.Add(data);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Recover(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            var value = reader.ReadInt32();
            if (player == 0)
                life0 += value;
            else
                life1 += value;

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_LpUpdate(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            var value = reader.ReadInt32();
            var isHero = player == 0;
            Presentation.Voice.VoicesData targetData = GetVoiceSet(isHero);

            var diff = (isHero ? life0 : life1) - value;
            if (isHero)
                life0 = value;
            else
                life1 = value;

            if (value == 0)
            {
                voiceData.Add(GetFinishDamageVoiceData(targetData, isHero));
                return UniTask.CompletedTask;
            }

            if (diff <= 0)
                return UniTask.CompletedTask;

            VoiceData data = new VoiceData();
            if (DamageIsBig(diff))
                data.name = Utility.Tools.GetRandomDictionaryElement(targetData.BigDamage.rawKvp).Value.shortName;
            else
                data.name = Utility.Tools.GetRandomDictionaryElement(targetData.Damage.rawKvp).Value.shortName;
            data.num = GetVoiceNum(targetData, data.name);
            data.isHero = isHero;
            data.wait = false;
            data.delay = 0f;
            voiceData.Add(data);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Attack(BinaryReader reader)
        {
            GPS from = reader.ReadGPS();
            GPS to = reader.ReadGPS();

            GameCard attackCard = Core.GCS_Get(from);
            if(attackCard == null)
            {
                DebugNoCard();
                return UniTask.CompletedTask;
            }

            bool directAttack = true;
            var value = attackCard.GetData().Attack;
            GameCard attackedCard = Core.GCS_Get(to);

            if(attackedCard != null)
            {
                directAttack = false;
                if (attackedCard.p.InPosition(MDPro3.Duel.YGOSharp.CardPosition.Attack))
                    value = attackCard.GetData().Attack - attackedCard.GetData().Attack;
                else
                    value = 0;
            }

            bool finalBlow = value >= (from.InMyControl() ? life1 : life0);
            var isHero = from.InMyControl();
            Presentation.Voice.VoicesData targetData = GetVoiceSet(isHero);
            var attackCode = ResolveVoiceCardCode(attackCard, 0);

            VoiceData data = GetVoiceByCard(targetData, targetData.MainMonsterAttack, attackCode, 0, isHero);
            if (data.name != string.Empty)
            {
                voiceData.Add(data);
                return UniTask.CompletedTask;
            }

            data = new VoiceData();
            data.name = Utility.Tools.GetRandomDictionaryElement(finalBlow ? targetData.BeforeAttackFinish.rawKvp : targetData.BeforeAttackNormal.rawKvp).Value.shortName;
            data.num = GetVoiceNum(targetData, data.name);
            data.isHero = isHero;
            data.wait = true;
            data.delay = 0f;
            voiceData.Add(data);

            VoiceData data2 = new VoiceData();
            data2.name = Utility.Tools.GetRandomDictionaryElement(directAttack ? targetData.DirectAttack.rawKvp : targetData.Attack.rawKvp).Value.shortName;
            data2.num = GetVoiceNum(targetData, data2.name);
            data2.isHero = data.isHero;
            data2.wait = true;
            data2.delay = 0f;
            voiceData.Add(data2);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_BecomeTarget(BinaryReader reader)
        {
            var count = reader.ReadByte();

            for (int i = 0; i < count; i++)
            {
                GPS gps = reader.ReadGPS();
                GameCard card = Core.GCS_Get(gps);

                if (card == null)
                {
                    DebugNoCard();
                    continue;
                }
                cardsBeTarget.Add(card);

                if (duelPhase == MDPro3.Duel.YGOSharp.DuelPhase.Main1 || duelPhase == MDPro3.Duel.YGOSharp.DuelPhase.Main2)
                    if (cardsInChain.Count == 0)
                        if (cardsBeTarget.Count == 2)
                            if (cardsBeTarget[0].InPendulumZone())
                                if (cardsBeTarget[1].InPendulumZone())
                                    if (cardsBeTarget[0].p.controller == cardsBeTarget[1].p.controller)
                                        inPendulumSummon = true;
            }

            if(!inPendulumSummon)
                return UniTask.CompletedTask;

            Presentation.Voice.VoicesData targetData = myTurn ? heroVoices : rivalVoices;

            VoiceData data = new VoiceData();
            data.name = GetVoiceBySubCategory(targetData.Summon, (int)MDPro3.VoiceController.SummonSub.Pendulum, (int)MDPro3.VoiceController.SummonSub.Pendulum, 1, true);
            if (data.name == null)
                return UniTask.CompletedTask;

            data.num = GetVoiceNum(targetData, data.name);
            data.isHero = myTurn;
            data.wait = false;
            data.delay = 0f;
            voiceData.Add(data);
            ignoreNextPendulumSummon = true;

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_UpdateData(BinaryReader reader)
        {
            if (inPendulumSummon)
            {
                inPendulumSummon = false;
                ignoreNextPendulumSummon = false;
                cardsBeTarget.Clear();
            }

            return UniTask.CompletedTask;
        }


        #endregion

    }
}
