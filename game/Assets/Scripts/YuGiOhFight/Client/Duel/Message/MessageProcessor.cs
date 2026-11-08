using Cysharp.Threading.Tasks;


using System;

using System.IO;
using UnityEngine;


namespace YuGiOhFight.Client.Duel
{
    public abstract class MessageProcessor
    {
        protected MessageDispatcher dispatcher;

        public MessageProcessor(MessageDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        protected Controllers.OcgCore Core => Runtime.Program.instance.ocgcore;

        public virtual void Dispose()
        {
        }

        protected virtual bool MessageNeedResponse(MDPro3.Duel.YGOSharp.GameMessage message)
        {
            if(message == MDPro3.Duel.YGOSharp.GameMessage.SelectBattleCmd
                || message == MDPro3.Duel.YGOSharp.GameMessage.SelectIdleCmd
                || message == MDPro3.Duel.YGOSharp.GameMessage.SelectEffectYn
                || message == MDPro3.Duel.YGOSharp.GameMessage.SelectYesNo
                || message == MDPro3.Duel.YGOSharp.GameMessage.SelectOption
                || message == MDPro3.Duel.YGOSharp.GameMessage.SelectCard
                || message == MDPro3.Duel.YGOSharp.GameMessage.SelectChain
                || message == MDPro3.Duel.YGOSharp.GameMessage.SelectPlace
                || message == MDPro3.Duel.YGOSharp.GameMessage.SelectPosition
                || message == MDPro3.Duel.YGOSharp.GameMessage.SelectTribute
                || message == MDPro3.Duel.YGOSharp.GameMessage.SortChain
                || message == MDPro3.Duel.YGOSharp.GameMessage.SelectCounter
                || message == MDPro3.Duel.YGOSharp.GameMessage.SelectSum
                || message == MDPro3.Duel.YGOSharp.GameMessage.SelectDisfield
                || message == MDPro3.Duel.YGOSharp.GameMessage.SortCard
                || message == MDPro3.Duel.YGOSharp.GameMessage.SelectUnselect
                || message == MDPro3.Duel.YGOSharp.GameMessage.AnnounceRace
                || message == MDPro3.Duel.YGOSharp.GameMessage.AnnounceAttrib
                || message == MDPro3.Duel.YGOSharp.GameMessage.AnnounceCard
                || message == MDPro3.Duel.YGOSharp.GameMessage.AnnounceNumber
                )
                return true;
            return false;
        }

        public virtual async UniTask Process(Runtime.Network.Package p)
        {
            Controllers.OcgCore core = Runtime.Program.instance == null ? null : Core;
            var loopVersion = core == null ? -1 : core.MessageLoopVersion;
            try
            {
                var reader = p.Data.reader;
                reader.BaseStream.Seek(0, 0);
                MDPro3.Duel.YGOSharp.GameMessage message = (MDPro3.Duel.YGOSharp.GameMessage)p.Function;
                switch (message)
                {
                    case MDPro3.Duel.YGOSharp.GameMessage.Retry:
                        await GameMessage_Retry(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.Hint:
                        await GameMessage_Hint(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.Waiting:
                        await GameMessage_Waiting(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.Start:
                        await GameMessage_Start(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.Win:
                        await GameMessage_Win(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.UpdateData:
                        await GameMessage_UpdateData(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.UpdateCard:
                        await GameMessage_UpdateCard(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.RequestDeck:
                        await GameMessage_RequestDeck(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.SelectBattleCmd:
                        await GameMessage_SelectBattleCmd(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.SelectIdleCmd:
                        await GameMessage_SelectIdleCmd(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.SelectEffectYn:
                        await GameMessage_SelectEffectYn(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.SelectYesNo:
                        await GameMessage_SelectYesNo(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.SelectOption:
                        await GameMessage_SelectOption(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.SelectCard:
                        await GameMessage_SelectCard(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.SelectChain:
                        await GameMessage_SelectChain(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.SelectPlace:
                        await GameMessage_SelectPlace(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.SelectPosition:
                        await GameMessage_SelectPosition(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.SelectTribute:
                        await GameMessage_SelectTribute(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.SortChain:
                        await GameMessage_SortChain(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.SelectCounter:
                        await GameMessage_SelectCounter(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.SelectSum:
                        await GameMessage_SelectSum(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.SelectDisfield:
                        await GameMessage_SelectDisfield(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.SortCard:
                        await GameMessage_SortCard(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.SelectUnselect:
                        await GameMessage_SelectUnselect(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.ConfirmDecktop:
                        await GameMessage_ConfirmDecktop(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.ConfirmCards:
                        await GameMessage_ConfirmCards(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.ShuffleDeck:
                        await GameMessage_ShuffleDeck(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.ShuffleHand:
                        await GameMessage_ShuffleHand(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.RefreshDeck:
                        await GameMessage_RefreshDeck(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.SwapGraveDeck:
                        await GameMessage_SwapGraveDeck(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.ShuffleSetCard:
                        await GameMessage_ShuffleSetCard(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.ReverseDeck:
                        await GameMessage_ReverseDeck(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.DeckTop:
                        await GameMessage_DeckTop(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.ShuffleExtra:
                        await GameMessage_ShuffleExtra(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.NewTurn:
                        await GameMessage_NewTurn(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.NewPhase:
                        await GameMessage_NewPhase(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.ConfirmExtratop:
                        await GameMessage_ConfirmExtratop(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.Move:
                        await GameMessage_Move(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.PosChange:
                        await GameMessage_PosChange(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.Set:
                        await GameMessage_Set(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.Swap:
                        await GameMessage_Swap(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.FieldDisabled:
                        await GameMessage_FieldDisabled(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.Summoning:
                        await GameMessage_Summoning(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.Summoned:
                        await GameMessage_Summoned(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.SpSummoning:
                        await GameMessage_SpSummoning(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.SpSummoned:
                        await GameMessage_SpSummoned(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.FlipSummoning:
                        await GameMessage_FlipSummoning(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.FlipSummoned:
                        await GameMessage_FlipSummoned(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.Chaining:
                        await GameMessage_Chaining(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.Chained:
                        await GameMessage_Chained(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.ChainSolving:
                        await GameMessage_ChainSolving(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.ChainSolved:
                        await GameMessage_ChainSolved(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.ChainEnd:
                        await GameMessage_ChainEnd(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.ChainNegated:
                        await GameMessage_ChainNegated(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.ChainDisabled:
                        await GameMessage_ChainDisabled(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.CardSelected:
                        await GameMessage_CardSelected(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.RandomSelected:
                        await GameMessage_RandomSelected(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.BecomeTarget:
                        await GameMessage_BecomeTarget(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.Draw:
                        await GameMessage_Draw(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.Damage:
                        await GameMessage_Damage(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.Recover:
                        await GameMessage_Recover(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.Equip:
                        await GameMessage_Equip(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.LpUpdate:
                        await GameMessage_LpUpdate(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.Unequip:
                        await GameMessage_Unequip(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.CardTarget:
                        await GameMessage_CardTarget(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.CancelTarget:
                        await GameMessage_CancelTarget(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.PayLpCost:
                        await GameMessage_PayLpCost(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.AddCounter:
                        await GameMessage_AddCounter(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.RemoveCounter:
                        await GameMessage_RemoveCounter(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.Attack:
                        await GameMessage_Attack(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.Battle:
                        await GameMessage_Battle(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.AttackDisabled:
                        await GameMessage_AttackDisabled(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.DamageStepStart:
                        await GameMessage_DamageStepStart(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.DamageStepEnd:
                        await GameMessage_DamageStepEnd(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.MissedEffect:
                        await GameMessage_MissedEffect(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.BeChainTarget:
                        await GameMessage_BeChainTarget(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.CreateRelation:
                        await GameMessage_CreateRelation(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.ReleaseRelation:
                        await GameMessage_ReleaseRelation(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.TossCoin:
                        await GameMessage_TossCoin(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.TossDice:
                        await GameMessage_TossDice(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.RockPaperScissors:
                        await GameMessage_RockPaperScissors(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.HandResult:
                        await GameMessage_HandResult(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.AnnounceRace:
                        await GameMessage_AnnounceRace(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.AnnounceAttrib:
                        await GameMessage_AnnounceAttrib(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.AnnounceCard:
                        await GameMessage_AnnounceCard(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.AnnounceNumber:
                        await GameMessage_AnnounceNumber(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.CardHint:
                        await GameMessage_CardHint(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.TagSwap:
                        await GameMessage_TagSwap(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.ReloadField:
                        await GameMessage_ReloadField(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.AiName:
                        await GameMessage_AiName(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.ShowHint:
                        await GameMessage_ShowHint(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.PlayerHint:
                        await GameMessage_PlayerHint(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.MatchKill:
                        await GameMessage_MatchKill(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.CustomMsg:
                        await GameMessage_CustomMsg(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.DuelWinner:
                        await GameMessage_DuelWinner(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.sibyl_chat:
                        await GameMessage_sibyl_chat(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.sibyl_replay:
                        await GameMessage_sibyl_replay(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.sibyl_clear:
                        await GameMessage_sibyl_clear(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.sibyl_delay:
                        await GameMessage_sibyl_delay(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.sibyl_book:
                        await GameMessage_sibyl_book(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.sibyl_name:
                        await GameMessage_sibyl_name(reader);
                        break;
                    case MDPro3.Duel.YGOSharp.GameMessage.sibyl_quit:
                        await GameMessage_sibyl_quit(reader);
                        break;
                }
            }
            catch (OperationCanceledException) when (core == null || !core.showing || core.MessageLoopVersion != loopVersion)
            {
                // Exit and a replacement message loop cancel the departed
                // duel's animation tasks as part of normal teardown.
                return;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        protected virtual UniTask GameMessage_Retry(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_Hint(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_Waiting(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_Start(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_Win(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_UpdateData(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_UpdateCard(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_RequestDeck(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_SelectBattleCmd(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_SelectIdleCmd(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_SelectEffectYn(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_SelectYesNo(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_SelectOption(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_SelectCard(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_SelectChain(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_SelectPlace(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_SelectPosition(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_SelectTribute(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_SortChain(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_SelectCounter(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_SelectSum(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_SelectDisfield(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_SortCard(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_SelectUnselect(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_ConfirmDecktop(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_ConfirmCards(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_ShuffleDeck(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_ShuffleHand(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_RefreshDeck(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_SwapGraveDeck(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_ShuffleSetCard(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_ReverseDeck(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_DeckTop(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_ShuffleExtra(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_NewTurn(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_NewPhase(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_ConfirmExtratop(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_Move(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_PosChange(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_Set(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_Swap(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_FieldDisabled(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_Summoning(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_Summoned(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_SpSummoning(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_SpSummoned(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_FlipSummoning(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_FlipSummoned(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_Chaining(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_Chained(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_ChainSolving(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_ChainSolved(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_ChainEnd(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_ChainNegated(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_ChainDisabled(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_CardSelected(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_RandomSelected(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_BecomeTarget(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_Draw(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_Damage(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_Recover(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_Equip(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_LpUpdate(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_Unequip(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_CardTarget(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_CancelTarget(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_PayLpCost(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_AddCounter(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_RemoveCounter(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_Attack(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_Battle(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_AttackDisabled(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_DamageStepStart(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_DamageStepEnd(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_MissedEffect(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_BeChainTarget(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_CreateRelation(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_ReleaseRelation(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_TossCoin(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_TossDice(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_RockPaperScissors(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_HandResult(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_AnnounceRace(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_AnnounceAttrib(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_AnnounceCard(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_AnnounceNumber(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_CardHint(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_TagSwap(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_ReloadField(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_AiName(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_ShowHint(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_PlayerHint(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_MatchKill(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_CustomMsg(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_DuelWinner(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_sibyl_chat(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_sibyl_replay(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_sibyl_clear(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_sibyl_delay(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_sibyl_book(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_sibyl_name(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask GameMessage_sibyl_quit(BinaryReader reader)
        {
            return UniTask.CompletedTask;
        }

    }
}
