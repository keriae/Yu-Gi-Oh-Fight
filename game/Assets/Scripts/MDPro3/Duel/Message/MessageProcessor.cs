using Cysharp.Threading.Tasks;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MDPro3.Duel
{
    public abstract class MessageProcessor
    {
        protected MessageDispatcher dispatcher;

        public MessageProcessor(MessageDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        protected OcgCore Core => Program.instance.ocgcore;

        public virtual void Dispose()
        {
        }

        protected virtual bool MessageNeedResponse(GameMessage message)
        {
            if(message == GameMessage.SelectBattleCmd
                || message == GameMessage.SelectIdleCmd
                || message == GameMessage.SelectEffectYn
                || message == GameMessage.SelectYesNo
                || message == GameMessage.SelectOption
                || message == GameMessage.SelectCard
                || message == GameMessage.SelectChain
                || message == GameMessage.SelectPlace
                || message == GameMessage.SelectPosition
                || message == GameMessage.SelectTribute
                || message == GameMessage.SortChain
                || message == GameMessage.SelectCounter
                || message == GameMessage.SelectSum
                || message == GameMessage.SelectDisfield
                || message == GameMessage.SortCard
                || message == GameMessage.SelectUnselect
                || message == GameMessage.AnnounceRace
                || message == GameMessage.AnnounceAttrib
                || message == GameMessage.AnnounceCard
                || message == GameMessage.AnnounceNumber
                )
                return true;
            return false;
        }

        public virtual async UniTask Process(Package p)
        {
            try
            {
                var reader = p.Data.reader;
                reader.BaseStream.Seek(0, 0);
                var message = (GameMessage)p.Function;
                switch (message)
                {
                    case GameMessage.Retry:
                        await GameMessage_Retry(reader);
                        break;
                    case GameMessage.Hint:
                        await GameMessage_Hint(reader);
                        break;
                    case GameMessage.Waiting:
                        await GameMessage_Waiting(reader);
                        break;
                    case GameMessage.Start:
                        await GameMessage_Start(reader);
                        break;
                    case GameMessage.Win:
                        await GameMessage_Win(reader);
                        break;
                    case GameMessage.UpdateData:
                        await GameMessage_UpdateData(reader);
                        break;
                    case GameMessage.UpdateCard:
                        await GameMessage_UpdateCard(reader);
                        break;
                    case GameMessage.RequestDeck:
                        await GameMessage_RequestDeck(reader);
                        break;
                    case GameMessage.SelectBattleCmd:
                        await GameMessage_SelectBattleCmd(reader);
                        break;
                    case GameMessage.SelectIdleCmd:
                        await GameMessage_SelectIdleCmd(reader);
                        break;
                    case GameMessage.SelectEffectYn:
                        await GameMessage_SelectEffectYn(reader);
                        break;
                    case GameMessage.SelectYesNo:
                        await GameMessage_SelectYesNo(reader);
                        break;
                    case GameMessage.SelectOption:
                        await GameMessage_SelectOption(reader);
                        break;
                    case GameMessage.SelectCard:
                        await GameMessage_SelectCard(reader);
                        break;
                    case GameMessage.SelectChain:
                        await GameMessage_SelectChain(reader);
                        break;
                    case GameMessage.SelectPlace:
                        await GameMessage_SelectPlace(reader);
                        break;
                    case GameMessage.SelectPosition:
                        await GameMessage_SelectPosition(reader);
                        break;
                    case GameMessage.SelectTribute:
                        await GameMessage_SelectTribute(reader);
                        break;
                    case GameMessage.SortChain:
                        await GameMessage_SortChain(reader);
                        break;
                    case GameMessage.SelectCounter:
                        await GameMessage_SelectCounter(reader);
                        break;
                    case GameMessage.SelectSum:
                        await GameMessage_SelectSum(reader);
                        break;
                    case GameMessage.SelectDisfield:
                        await GameMessage_SelectDisfield(reader);
                        break;
                    case GameMessage.SortCard:
                        await GameMessage_SortCard(reader);
                        break;
                    case GameMessage.SelectUnselect:
                        await GameMessage_SelectUnselect(reader);
                        break;
                    case GameMessage.ConfirmDecktop:
                        await GameMessage_ConfirmDecktop(reader);
                        break;
                    case GameMessage.ConfirmCards:
                        await GameMessage_ConfirmCards(reader);
                        break;
                    case GameMessage.ShuffleDeck:
                        await GameMessage_ShuffleDeck(reader);
                        break;
                    case GameMessage.ShuffleHand:
                        await GameMessage_ShuffleHand(reader);
                        break;
                    case GameMessage.RefreshDeck:
                        await GameMessage_RefreshDeck(reader);
                        break;
                    case GameMessage.SwapGraveDeck:
                        await GameMessage_SwapGraveDeck(reader);
                        break;
                    case GameMessage.ShuffleSetCard:
                        await GameMessage_ShuffleSetCard(reader);
                        break;
                    case GameMessage.ReverseDeck:
                        await GameMessage_ReverseDeck(reader);
                        break;
                    case GameMessage.DeckTop:
                        await GameMessage_DeckTop(reader);
                        break;
                    case GameMessage.ShuffleExtra:
                        await GameMessage_ShuffleExtra(reader);
                        break;
                    case GameMessage.NewTurn:
                        await GameMessage_NewTurn(reader);
                        break;
                    case GameMessage.NewPhase:
                        await GameMessage_NewPhase(reader);
                        break;
                    case GameMessage.ConfirmExtratop:
                        await GameMessage_ConfirmExtratop(reader);
                        break;
                    case GameMessage.Move:
                        await GameMessage_Move(reader);
                        break;
                    case GameMessage.PosChange:
                        await GameMessage_PosChange(reader);
                        break;
                    case GameMessage.Set:
                        await GameMessage_Set(reader);
                        break;
                    case GameMessage.Swap:
                        await GameMessage_Swap(reader);
                        break;
                    case GameMessage.FieldDisabled:
                        await GameMessage_FieldDisabled(reader);
                        break;
                    case GameMessage.Summoning:
                        await GameMessage_Summoning(reader);
                        break;
                    case GameMessage.Summoned:
                        await GameMessage_Summoned(reader);
                        break;
                    case GameMessage.SpSummoning:
                        await GameMessage_SpSummoning(reader);
                        break;
                    case GameMessage.SpSummoned:
                        await GameMessage_SpSummoned(reader);
                        break;
                    case GameMessage.FlipSummoning:
                        await GameMessage_FlipSummoning(reader);
                        break;
                    case GameMessage.FlipSummoned:
                        await GameMessage_FlipSummoned(reader);
                        break;
                    case GameMessage.Chaining:
                        await GameMessage_Chaining(reader);
                        break;
                    case GameMessage.Chained:
                        await GameMessage_Chained(reader);
                        break;
                    case GameMessage.ChainSolving:
                        await GameMessage_ChainSolving(reader);
                        break;
                    case GameMessage.ChainSolved:
                        await GameMessage_ChainSolved(reader);
                        break;
                    case GameMessage.ChainEnd:
                        await GameMessage_ChainEnd(reader);
                        break;
                    case GameMessage.ChainNegated:
                        await GameMessage_ChainNegated(reader);
                        break;
                    case GameMessage.ChainDisabled:
                        await GameMessage_ChainDisabled(reader);
                        break;
                    case GameMessage.CardSelected:
                        await GameMessage_CardSelected(reader);
                        break;
                    case GameMessage.RandomSelected:
                        await GameMessage_RandomSelected(reader);
                        break;
                    case GameMessage.BecomeTarget:
                        await GameMessage_BecomeTarget(reader);
                        break;
                    case GameMessage.Draw:
                        await GameMessage_Draw(reader);
                        break;
                    case GameMessage.Damage:
                        await GameMessage_Damage(reader);
                        break;
                    case GameMessage.Recover:
                        await GameMessage_Recover(reader);
                        break;
                    case GameMessage.Equip:
                        await GameMessage_Equip(reader);
                        break;
                    case GameMessage.LpUpdate:
                        await GameMessage_LpUpdate(reader);
                        break;
                    case GameMessage.Unequip:
                        await GameMessage_Unequip(reader);
                        break;
                    case GameMessage.CardTarget:
                        await GameMessage_CardTarget(reader);
                        break;
                    case GameMessage.CancelTarget:
                        await GameMessage_CancelTarget(reader);
                        break;
                    case GameMessage.PayLpCost:
                        await GameMessage_PayLpCost(reader);
                        break;
                    case GameMessage.AddCounter:
                        await GameMessage_AddCounter(reader);
                        break;
                    case GameMessage.RemoveCounter:
                        await GameMessage_RemoveCounter(reader);
                        break;
                    case GameMessage.Attack:
                        await GameMessage_Attack(reader);
                        break;
                    case GameMessage.Battle:
                        await GameMessage_Battle(reader);
                        break;
                    case GameMessage.AttackDisabled:
                        await GameMessage_AttackDisabled(reader);
                        break;
                    case GameMessage.DamageStepStart:
                        await GameMessage_DamageStepStart(reader);
                        break;
                    case GameMessage.DamageStepEnd:
                        await GameMessage_DamageStepEnd(reader);
                        break;
                    case GameMessage.MissedEffect:
                        await GameMessage_MissedEffect(reader);
                        break;
                    case GameMessage.BeChainTarget:
                        await GameMessage_BeChainTarget(reader);
                        break;
                    case GameMessage.CreateRelation:
                        await GameMessage_CreateRelation(reader);
                        break;
                    case GameMessage.ReleaseRelation:
                        await GameMessage_ReleaseRelation(reader);
                        break;
                    case GameMessage.TossCoin:
                        await GameMessage_TossCoin(reader);
                        break;
                    case GameMessage.TossDice:
                        await GameMessage_TossDice(reader);
                        break;
                    case GameMessage.RockPaperScissors:
                        await GameMessage_RockPaperScissors(reader);
                        break;
                    case GameMessage.HandResult:
                        await GameMessage_HandResult(reader);
                        break;
                    case GameMessage.AnnounceRace:
                        await GameMessage_AnnounceRace(reader);
                        break;
                    case GameMessage.AnnounceAttrib:
                        await GameMessage_AnnounceAttrib(reader);
                        break;
                    case GameMessage.AnnounceCard:
                        await GameMessage_AnnounceCard(reader);
                        break;
                    case GameMessage.AnnounceNumber:
                        await GameMessage_AnnounceNumber(reader);
                        break;
                    case GameMessage.CardHint:
                        await GameMessage_CardHint(reader);
                        break;
                    case GameMessage.TagSwap:
                        await GameMessage_TagSwap(reader);
                        break;
                    case GameMessage.ReloadField:
                        await GameMessage_ReloadField(reader);
                        break;
                    case GameMessage.AiName:
                        await GameMessage_AiName(reader);
                        break;
                    case GameMessage.ShowHint:
                        await GameMessage_ShowHint(reader);
                        break;
                    case GameMessage.PlayerHint:
                        await GameMessage_PlayerHint(reader);
                        break;
                    case GameMessage.MatchKill:
                        await GameMessage_MatchKill(reader);
                        break;
                    case GameMessage.CustomMsg:
                        await GameMessage_CustomMsg(reader);
                        break;
                    case GameMessage.DuelWinner:
                        await GameMessage_DuelWinner(reader);
                        break;
                    case GameMessage.sibyl_chat:
                        await GameMessage_sibyl_chat(reader);
                        break;
                    case GameMessage.sibyl_replay:
                        await GameMessage_sibyl_replay(reader);
                        break;
                    case GameMessage.sibyl_clear:
                        await GameMessage_sibyl_clear(reader);
                        break;
                    case GameMessage.sibyl_delay:
                        await GameMessage_sibyl_delay(reader);
                        break;
                    case GameMessage.sibyl_book:
                        await GameMessage_sibyl_book(reader);
                        break;
                    case GameMessage.sibyl_name:
                        await GameMessage_sibyl_name(reader);
                        break;
                    case GameMessage.sibyl_quit:
                        await GameMessage_sibyl_quit(reader);
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw e;
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
