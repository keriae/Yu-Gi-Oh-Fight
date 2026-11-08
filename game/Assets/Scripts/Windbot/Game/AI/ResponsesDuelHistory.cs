using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MDPro3.AI;
using YGOSharp.OCGWrapper;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI
{
    /// <summary>Snapshot the duel-log events received by this player, independently of spectator UI timing.</summary>
    public sealed class ResponsesDuelHistory
    {
        private readonly Duel duel;
        private readonly int retentionBudgetTokens;
        private readonly Queue<Tuple<JObject, int>> events = new Queue<Tuple<JObject, int>>();
        private int tokens;
        private int total;
        private int gaps;

        public ResponsesDuelHistory(Duel duel, int retentionBudgetTokens = 200000)
        { this.duel = duel; this.retentionBudgetTokens = retentionBudgetTokens; }

        private int Local(int player) => player < 2 ? (duel.IsFirst ? player : 1 - player) : player;

        private JObject Location(BinaryReader reader, bool position = true, bool shortLocation = false)
        {
            var result = new JObject
            {
                ["player"] = Local(reader.ReadByte()),
                ["location"] = ((CardLocation)reader.ReadByte()).ToString(),
                ["sequence"] = (int)reader.ReadByte()
            };
            if (!shortLocation)
            {
                var value = reader.ReadByte();
                if (position) result["position"] = ((CardPosition)value).ToString();
                else if (((CardLocation)Enum.Parse(typeof(CardLocation), (string)result["location"]) & CardLocation.Overlay) != 0)
                    result["overlay_sequence"] = (int)value;
            }
            // Deck order is not part of the model's information, including historical movements.
            if ((string)result["location"] == CardLocation.Deck.ToString()) result.Remove("sequence");
            return result;
        }

        private ClientCard Existing(JObject location)
        {
            if (location["sequence"] == null) return null;
            return duel.GetCard((int)location["player"],
                (int)(CardLocation)Enum.Parse(typeof(CardLocation), (string)location["location"]),
                (int)location["sequence"], (int?)location["overlay_sequence"] ?? 0);
        }

        private bool Visible(JObject location)
        {
            var zone = (CardLocation)Enum.Parse(typeof(CardLocation), (string)location["location"]);
            if (zone == 0) return false;
            if ((int)location["player"] == 0) return true;
            if ((zone & CardLocation.Overlay) != 0 || zone == CardLocation.Grave) return true;
            if (zone == CardLocation.Hand || zone == CardLocation.Deck) return false;
            var position = location["position"] != null
                ? (CardPosition)Enum.Parse(typeof(CardPosition), (string)location["position"])
                : (CardPosition)(Existing(location)?.Position ?? 0);
            return (position & CardPosition.FaceUp) != 0;
        }

        private JObject Card(JObject location, int code = 0, bool revealed = false, bool fallback = true)
        {
            var result = (JObject)location.DeepClone();
            bool visible = revealed || Visible(location);
            code &= 0x7fffffff;
            // Never reuse a cached hidden card's ID. Explicit reveal packets alone can override visibility.
            if (code == 0 && visible && !revealed && fallback) code = Existing(location)?.Id ?? 0;
            if (code > 0 && visible)
            {
                result["id"] = code;
                result["name"] = NamedCard.Get(code)?.Name ?? "";
            }
            else result["hidden"] = true;
            return result;
        }

        private JObject Chain(int link)
        {
            var result = new JObject { ["link"] = link };
            if (link > 0 && link <= duel.CurrentChainInfo.Count)
            {
                var info = duel.CurrentChainInfo[link - 1];
                result["player"] = info.ActivatePlayer;
                result["card_id"] = info.ActivateId;
                result["card_name"] = NamedCard.Get(info.ActivateId)?.Name ?? "";
                result["effect_id"] = info.ActivateDescription;
                result["effect"] = ResponsesDecisionController.Describe(info.ActivateDescription);
            }
            return result;
        }

        private void Add(GameMessage kind, JObject detail, int? turn = null, string phase = null)
        {
            var entry = new JObject { ["event_id"] = ++total, ["turn"] = turn ?? duel.Turn,
                ["phase"] = phase ?? duel.Phase.ToString(), ["event"] = kind.ToString() };
            foreach (var property in detail.Properties()) entry[property.Name] = property.Value.DeepClone();
            if (kind != GameMessage.Start && duel.SolvingChainIndex > 0)
                entry["resolving_chain"] = duel.SolvingChainIndex;
            int size = PromptTokenCounter.Count(entry.ToString(Formatting.None)) + 1;
            events.Enqueue(Tuple.Create(entry, size));
            tokens += size;
            while (tokens > retentionBudgetTokens) tokens -= events.Dequeue().Item2;
        }

        public JObject Snapshot()
        {
            return new JObject
            {
                ["perspective"] = 0, ["order"] = "oldest_first",
                ["complete"] = total == events.Count && gaps == 0, ["omitted_events"] = total - events.Count,
                ["unparsed_events"] = gaps,
                ["events"] = new JArray(events.Select(e => e.Item1.DeepClone()))
            };
        }

        /// <summary>Observe before the normal handler changes cards; always restore its reader position.</summary>
        public void Observe(GameMessage kind, BinaryReader reader)
        {
            long original = reader.BaseStream.Position;
            try
            {
                JObject detail;
                int code, player, count;
                switch (kind)
                {
                    case GameMessage.Start:
                        events.Clear(); tokens = total = gaps = 0;
                        bool first = (reader.ReadByte() & 0xf) == 0;
                        reader.ReadByte(); // master rule
                        int lp0 = reader.ReadInt32(), lp1 = reader.ReadInt32();
                        Add(kind, new JObject { ["lp"] = new JArray(first ? lp0 : lp1, first ? lp1 : lp0) }, 0, "None");
                        return;
                    case GameMessage.NewTurn:
                        Add(kind, new JObject { ["player"] = Local(reader.ReadByte()),
                            ["lp"] = new JArray(duel.Fields[0].LifePoints, duel.Fields[1].LifePoints) }, duel.Turn + 1, "None");
                        return;
                    case GameMessage.NewPhase:
                        Add(kind, new JObject(), phase: ((DuelPhase)reader.ReadInt16()).ToString());
                        return;
                    case GameMessage.Draw:
                        player = Local(reader.ReadByte()); count = reader.ReadByte();
                        detail = new JObject { ["player"] = player, ["count"] = count };
                        // Opponent draws disclose quantity only, even if a packet or cached view has an ID.
                        if (player == 0)
                        {
                            var drawn = new JArray();
                            for (int i = 0; i < count; i++) drawn.Add(Card(new JObject
                                { ["player"] = player, ["location"] = "Hand" }, reader.ReadInt32(), fallback: false));
                            detail["cards"] = drawn;
                        }
                        Add(kind, detail); return;
                    case GameMessage.Move:
                        code = reader.ReadInt32();
                        var from = Location(reader); var to = Location(reader);
                        var reason = (MDPro3.Duel.YGOSharp.CardReason)reader.ReadUInt32();
                        // A public card moving into a hidden zone remains a legitimate historical observation.
                        var identity = Visible(from) ? Card(from, code) : Card(to, code, fallback: false);
                        Add(kind, new JObject { ["card"] = identity, ["from"] = from, ["to"] = to,
                            ["reason"] = string.Join("/", Enum.GetValues(typeof(MDPro3.Duel.YGOSharp.CardReason))
                                .Cast<MDPro3.Duel.YGOSharp.CardReason>().Where(r => (reason & r) != 0).Select(r => r.ToString())),
                            ["reason_flags"] = (uint)reason }); return;
                    case GameMessage.Set:
                    case GameMessage.Summoning:
                    case GameMessage.SpSummoning:
                    case GameMessage.FlipSummoning:
                        code = reader.ReadInt32();
                        Add(kind, new JObject { ["card"] = Card(Location(reader), code) }); return;
                    case GameMessage.Summoned:
                    case GameMessage.SpSummoned:
                    case GameMessage.FlipSummoned:
                        Add(kind, new JObject { ["player"] = duel.LastSummonPlayer,
                            ["cards"] = new JArray(duel.SummoningCards.Where(c => c != null).Select(c =>
                                Card(new JObject { ["player"] = c.Controller, ["location"] = c.Location.ToString(),
                                    ["sequence"] = c.Sequence, ["position"] = ((CardPosition)c.Position).ToString() }, c.Id))) }); return;
                    case GameMessage.Chaining:
                        code = reader.ReadInt32();
                        var source = Location(reader, position: false);
                        player = Local(reader.ReadByte());
                        reader.ReadInt16(); // triggering location/sequence
                        int effect = reader.ReadInt32();
                        int link = reader.ReadByte();
                        Add(kind, new JObject { ["player"] = player, ["link"] = link,
                            ["card"] = Card(source, code, revealed: true), ["effect_id"] = effect,
                            ["effect"] = ResponsesDecisionController.Describe(effect) }); return;
                    case GameMessage.Chained:
                    case GameMessage.ChainSolving:
                    case GameMessage.ChainSolved:
                    case GameMessage.ChainNegated:
                    case GameMessage.ChainDisabled:
                        Add(kind, Chain(reader.ReadByte())); return;
                    case GameMessage.ChainEnd:
                        Add(kind, new JObject()); return;
                    case GameMessage.BecomeTarget:
                    case GameMessage.RandomSelected:
                        detail = new JObject { ["link"] = duel.SolvingChainIndex > 0
                            ? duel.SolvingChainIndex : duel.CurrentChainInfo.Count };
                        if (kind == GameMessage.RandomSelected) detail["player"] = Local(reader.ReadByte());
                        count = reader.ReadByte();
                        var targets = new JArray();
                        for (int i = 0; i < count; i++) targets.Add(Card(Location(reader, position: false)));
                        detail["cards"] = targets; Add(kind, detail); return;
                    case GameMessage.ConfirmCards:
                    case GameMessage.ConfirmDecktop:
                    case GameMessage.ConfirmExtratop:
                        player = Local(reader.ReadByte());
                        if (kind == GameMessage.ConfirmCards) reader.ReadByte(); // skip panel
                        count = reader.ReadByte();
                        var revealed = new JArray();
                        for (int i = 0; i < count; i++)
                        {
                            code = reader.ReadInt32();
                            revealed.Add(Card(Location(reader, shortLocation: true), code, revealed: true));
                        }
                        Add(kind, new JObject { ["player"] = player, ["cards"] = revealed }); return;
                    case GameMessage.PosChange:
                        code = reader.ReadInt32(); var before = Location(reader);
                        var after = (JObject)before.DeepClone(); after["position"] = ((CardPosition)reader.ReadByte()).ToString();
                        Add(kind, new JObject { ["card"] = Visible(before) ? Card(before, code) : Card(after, code),
                            ["from"] = before, ["to"] = after }); return;
                    case GameMessage.Swap:
                        code = reader.ReadInt32(); var left = Location(reader);
                        int otherCode = reader.ReadInt32(); var right = Location(reader);
                        var leftTo = (JObject)right.DeepClone(); leftTo["position"] = left["position"].DeepClone();
                        var rightTo = (JObject)left.DeepClone(); rightTo["position"] = right["position"].DeepClone();
                        Add(kind, new JObject { ["cards"] = new JArray(Card(left, code), Card(right, otherCode)),
                            ["from"] = new JArray(left, right), ["to"] = new JArray(leftTo, rightTo) }); return;
                    case GameMessage.Attack:
                        var attacker = Location(reader, position: false); var target = Location(reader, position: false);
                        bool direct = (string)target["location"] == "0";
                        Add(kind, new JObject { ["attacker"] = Card(attacker), ["direct"] = direct,
                            ["target"] = direct ? null : Card(target) }); return;
                    case GameMessage.AttackDisabled:
                    case GameMessage.DamageStepStart:
                    case GameMessage.DamageStepEnd:
                        Add(kind, new JObject()); return;
                    case GameMessage.Damage:
                    case GameMessage.PayLpCost:
                    case GameMessage.Recover:
                    case GameMessage.LpUpdate:
                        player = Local(reader.ReadByte()); int value = reader.ReadInt32();
                        int oldLp = duel.Fields[player].LifePoints;
                        int newLp = kind == GameMessage.LpUpdate ? value : kind == GameMessage.Recover
                            ? oldLp + value : Math.Max(0, oldLp - value);
                        Add(kind, new JObject { ["player"] = player, ["value"] = value,
                            ["lp_before"] = oldLp, ["lp_after"] = newLp }); return;
                    case GameMessage.Equip:
                    case GameMessage.CardTarget:
                    case GameMessage.CancelTarget:
                        Add(kind, new JObject { ["card"] = Card(Location(reader, position: false)),
                            ["target"] = Card(Location(reader, position: false)) }); return;
                    case GameMessage.Unequip:
                        Add(kind, new JObject { ["card"] = Card(Location(reader, position: false)) }); return;
                    case GameMessage.AddCounter:
                    case GameMessage.RemoveCounter:
                        int counter = reader.ReadUInt16(); var counterCard = Location(reader, shortLocation: true);
                        Add(kind, new JObject { ["card"] = Card(counterCard), ["counter_type"] = counter,
                            ["count"] = reader.ReadUInt16() }); return;
                    case GameMessage.ShuffleDeck:
                    case GameMessage.ShuffleHand:
                    case GameMessage.ShuffleExtra:
                    case GameMessage.SwapGraveDeck:
                        Add(kind, new JObject { ["player"] = Local(reader.ReadByte()) }); return;
                    case GameMessage.ReverseDeck:
                        Add(kind, new JObject()); return;
                    case GameMessage.ShuffleSetCard:
                        Add(kind, new JObject { ["location"] = ((CardLocation)reader.ReadByte()).ToString(),
                            ["count"] = (int)reader.ReadByte() }); return;
                    case GameMessage.Win:
                        Add(kind, new JObject { ["winner"] = Local(reader.ReadByte()), ["reason"] = (int)reader.ReadByte() }); return;
                }
            }
            catch (EndOfStreamException)
            {
                // An unknown/truncated event layout must not consume or block the normal protocol handler.
                gaps++;
                Add(kind, new JObject { ["history_gap"] = true });
            }
            finally { reader.BaseStream.Position = original; }
        }
    }
}
