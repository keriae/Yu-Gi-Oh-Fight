using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using MDPro3.AI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YGOSharp.OCGWrapper;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI
{
    /// <summary>Responses decisions use the existing WindBot view and the original core's legal candidates.</summary>
    public sealed class ResponsesDecisionController : IDisposable
    {
        private readonly GameClient game;
        private readonly Duel duel;
        private readonly ResponsesAiClient client;
        private readonly ResponsesDecisionMemory memory = new ResponsesDecisionMemory();
        private readonly ResponsesDuelHistory duelHistory;
        private JArray ownDeckMain, ownDeckExtra;
        private readonly Random random = new Random(Guid.NewGuid().GetHashCode());

        public ResponsesDecisionController(GameClient game, Duel duel)
        {
            this.game = game;
            this.duel = duel;
            duelHistory = new ResponsesDuelHistory(duel, game.ResponsesConfig.PromptBudgetTokens);
            client = new ResponsesAiClient(game.ResponsesConfig);
        }

        public void SetDeck(Deck deck)
        {
            ownDeckMain = deck == null ? null : Catalog(deck.Cards);
            ownDeckExtra = deck == null ? null : Catalog(deck.ExtraCards);
        }
        public void RecordDuelEvent(GameMessage message, BinaryReader packet)
        {
            memory.Observe(message, packet, duel);
            duelHistory.Observe(message, packet);
        }
        public void Dispose() { client.Dispose(); }

        public static bool IsVisible(ClientCard card, bool promptRevealed = false)
        {
            if (card == null || card.Id == 0) return false;
            if (promptRevealed || card.Controller == 0) return true;
            if (card.Location == CardLocation.Hand || card.Location == CardLocation.Deck) return false;
            if (card.Location == CardLocation.Extra || card.Location == CardLocation.Removed)
                return card.IsFaceup();
            if (card.Location == CardLocation.Grave || card.Location == CardLocation.Overlay) return true;
            return card.IsFaceup();
        }

        private static string TypeNames(int type)
        {
            return string.Join("/", Enum.GetValues(typeof(CardType)).Cast<CardType>()
                .Where(t => (type & (int)t) != 0).Select(t => t.ToString()));
        }

        private static JArray LinkArrows(int markers)
        {
            return new JArray(Enum.GetValues(typeof(CardLinkMarker)).Cast<CardLinkMarker>()
                .Where(direction => (markers & (int)direction) != 0).Select(direction => direction.ToString()));
        }

        private static JArray LinkedZones(ClientCard card)
        {
            // WindBot's bitmap is relative to the source controller, while prompt players
            // are already normalized. Shared extra zones have two references to one space.
            int mask = card.GetLinkedZones();
            var zones = new JArray();
            var seen = new HashSet<int>();
            for (int side = 0; side < 2; side++)
                for (int sequence = 0; sequence < 7; sequence++)
                {
                    if ((mask & (1 << (side * 16 + sequence))) == 0) continue;
                    int controller = side == 0 ? card.Controller : 1 - card.Controller;
                    int normalizedSequence = sequence;
                    // Canonicalize a shared EMZ to player 0; preserve the other player's alias.
                    if (sequence >= 5 && controller == 1)
                    { controller = 0; normalizedSequence = 11 - sequence; }
                    if (!seen.Add(controller * 16 + normalizedSequence)) continue;
                    var zone = new JObject { ["controller"] = controller,
                        ["location"] = CardLocation.MonsterZone.ToString(), ["sequence"] = normalizedSequence };
                    if (normalizedSequence >= 5)
                        zone["shared_with"] = new JObject { ["controller"] = 1,
                            ["location"] = CardLocation.MonsterZone.ToString(), ["sequence"] = 11 - normalizedSequence };
                    zones.Add(zone);
                }
            return new JArray(zones.OrderBy(zone => (int)zone["controller"] * 16 + (int)zone["sequence"]));
        }

        public static JObject CardView(ClientCard card, bool promptRevealed = false)
        {
            if (card == null) return null;
            var item = new JObject
            {
                ["controller"] = card.Controller,
                ["location"] = card.Location.ToString(),
                ["sequence"] = card.Sequence,
                ["position"] = ((CardPosition)card.Position).ToString()
            };
            if ((card.Location & CardLocation.Overlay) != 0) item["overlay_sequence"] = card.SelectSeq;
            if (!IsVisible(card, promptRevealed))
            {
                item["hidden"] = true;
                return item;
            }
            item["id"] = card.Id;
            item["name"] = card.Name ?? "";
            item["text"] = card.Data?.Description ?? "";
            item["type"] = TypeNames(card.Type != 0 ? card.Type : card.Data?.Type ?? 0);
            bool baseOnly = card.Type == 0 && card.Data != null;
            item["attack"] = baseOnly ? card.Data.Attack : card.Attack;
            item["defense"] = baseOnly ? card.Data.Defense : card.Defense;
            item["level"] = baseOnly ? (card.Data.HasType(CardType.Xyz) || card.Data.HasType(CardType.Link) ? 0 : card.Data.Level) : card.Level;
            item["rank"] = baseOnly && card.Data.HasType(CardType.Xyz) ? card.Data.Level : card.Rank;
            item["link"] = baseOnly && card.Data.HasType(CardType.Link) ? card.Data.Level : card.LinkCount;
            item["link_markers"] = baseOnly ? card.Data.LinkMarker : card.LinkMarker;
            if (((baseOnly ? card.Data.Type : card.Type) & (int)CardType.Link) != 0)
            {
                item["link_arrows"] = LinkArrows((int)item["link_markers"]);
                // A catalog or temporary card provides printed arrows, not an existing field relation.
                if (!baseOnly && card.LinkCount > 0 && card.Location == CardLocation.MonsterZone
                    && card.IsFaceup() && card.Controller >= 0 && card.Controller <= 1
                    && card.Sequence >= 0 && card.Sequence < 7)
                    item["linked_zones"] = LinkedZones(card);
            }
            item["attribute"] = ((CardAttribute)(baseOnly ? card.Data.Attribute : card.Attribute)).ToString();
            item["race"] = ((CardRace)(baseOnly ? card.Data.Race : card.Race)).ToString();
            item["left_scale"] = baseOnly ? card.Data.LScale : card.LScale;
            item["right_scale"] = baseOnly ? card.Data.RScale : card.RScale;
            item["disabled"] = card.Disabled != 0;
            item["overlays"] = new JArray(card.Overlays ?? new List<int>());
            return item;
        }

        private static JArray Views(IEnumerable<ClientCard> cards)
        {
            return new JArray((cards ?? Enumerable.Empty<ClientCard>()).Where(c => c != null).Select(c => CardView(c)));
        }

        public JObject State()
        {
            var result = new JObject
            {
                ["turn"] = duel.Turn,
                ["turn_player"] = duel.Player,
                ["phase"] = duel.Phase.ToString(),
                ["you"] = 0,
                ["chain"] = Views(duel.CurrentChain),
                ["chain_targets"] = Views(duel.ChainTargets)
            };
            var players = new JArray();
            for (int p = 0; p < 2; p++)
            {
                var f = duel.Fields[p];
                var field = new JObject
                {
                    ["player"] = p,
                    ["lp"] = f.LifePoints,
                    ["deck_count"] = f.Deck?.Count ?? 0,
                    ["hand_count"] = f.Hand?.Count ?? 0,
                    ["extra_count"] = f.ExtraDeck?.Count ?? 0,
                    ["under_attack"] = f.UnderAttack,
                    ["battle_monster"] = CardView(f.BattlingMonster),
                    ["monsters"] = Views(f.MonsterZone),
                    ["spells"] = Views(f.SpellZone),
                    ["graveyard"] = Views(f.Graveyard),
                    ["banished"] = Views(f.Banished),
                    ["extra_faceup"] = Views((f.ExtraDeck ?? new List<ClientCard>()).Where(c => c.IsFaceup())),
                    ["player_effects"] = new JArray((f.HintDescriptions ?? new HashSet<int>()).Select(Describe))
                };
                // Do not serialize the opponent's hand/extra deck, even if a previous packet populated a code.
                if (p == 0) field["hand"] = Views(f.Hand);
                players.Add(field);
            }
            result["players"] = players;
            result["solving_chain"] = duel.SolvingChainIndex;
            result["chain_effects"] = new JArray((duel.CurrentChainInfo ?? new List<ChainInfo>()).Select((info, index) =>
                new JObject
                {
                    ["link"] = index + 1, ["player"] = info.ActivatePlayer,
                    ["card_id"] = info.ActivateId,
                    ["card_name"] = NamedCard.Get(info.ActivateId)?.Name ?? "",
                    ["card_text"] = NamedCard.Get(info.ActivateId)?.Description ?? "",
                    ["effect"] = Describe(info.ActivateDescription),
                    ["effect_id"] = info.ActivateDescription,
                    ["activation_controller"] = info.ActivateController,
                    ["activation_location"] = info.ActivateLocation.ToString(),
                    ["activation_sequence"] = info.ActivateSequence,
                    ["activation_position"] = ((CardPosition)info.ActivatePosition).ToString(),
                    ["negated"] = duel.NegatedChainIndexList.Contains(index + 1)
                }));
            if (ownDeckMain != null)
            {
                result["own_deck_main"] = ownDeckMain.DeepClone();
                result["own_deck_extra"] = ownDeckExtra.DeepClone();
            }
            var decisions = memory.Snapshot();
            result["recent_decisions"] = decisions["recent_decisions"];
            decisions.Remove("recent_decisions");
            result["decision_context"] = decisions;
            result["chain_resolution_order"] = new JArray(Enumerable.Range(1, duel.CurrentChainInfo.Count).Reverse());
            result["chain_resolution_order_scope"] = "full_announced_chain_not_remaining_links";
            result["summoning_cards"] = Views(duel.SummoningCards);
            result["last_summoned_cards"] = Views(duel.LastSummonedCards);
            result["duel_history"] = duelHistory.Snapshot();
            return result;
        }

        private static JArray Catalog(IEnumerable<NamedCard> cards)
        {
            return new JArray((cards ?? Enumerable.Empty<NamedCard>()).GroupBy(c => c.Id).Select(group =>
            {
                var c = group.First();
                var item = new JObject
                {
                    ["id"] = c.Id, ["name"] = c.Name, ["copies"] = group.Count(),
                    ["text"] = c.Description, ["type"] = TypeNames(c.Type),
                    ["level"] = c.HasType(CardType.Xyz) || c.HasType(CardType.Link) ? 0 : c.Level,
                    ["rank"] = c.HasType(CardType.Xyz) ? c.Level : 0,
                    ["link"] = c.HasType(CardType.Link) ? c.Level : 0,
                    ["attack"] = c.Attack, ["defense"] = c.Defense,
                    ["attribute"] = ((CardAttribute)c.Attribute).ToString(),
                    ["race"] = ((CardRace)c.Race).ToString(), ["left_scale"] = c.LScale, ["right_scale"] = c.RScale,
                    ["link_markers"] = c.LinkMarker
                };
                if (c.HasType(CardType.Link)) item["link_arrows"] = LinkArrows(c.LinkMarker);
                return item;
            }));
        }

        public static string Describe(int desc)
        {
            if (desc <= 0) return "effect";
            if (desc < 10000)
            {
                string system = YuGiOhFight.Client.Presentation.StringHelper.Get("system", desc);
                return string.IsNullOrEmpty(system) ? "hint " + desc : system;
            }
            var card = MDPro3.Duel.YGOSharp.CardsManager.GetCardRaw(desc >> 4);
            var text = card?.Str;
            int index = desc & 15;
            return text != null && index < text.Length && !string.IsNullOrEmpty(text[index])
                ? text[index] : "effect " + desc + " (card " + (desc >> 4) + ", effect " + index + ")";
        }

        private JArray CardChoices(IList<ClientCard> cards)
        {
            var choices = new JArray();
            for (int i = 0; i < cards.Count; i++)
                choices.Add(new JObject { ["index"] = i, ["card"] = CardView(cards[i], true) });
            return choices;
        }

        private JObject Request(string kind, JObject prompt, JObject schema, Func<JObject, bool> validate,
            string constraint, Func<JObject, string> summary)
        {
            var input = ResponsesDuelPrompt.Build(State(), kind, prompt);
            for (int attempt = 0; attempt < 3; attempt++)
            {
                game.ResponsesCancellation.ThrowIfCancellationRequested();
                game.ResponsesStatus?.Invoke("AI 思考中（" + kind + "）");
                string output = client.RequestAsync(ResponsesDuelPrompt.InstructionsFor(kind), input.ToString(Formatting.None), schema,
                    game.ResponsesCancellation).GetAwaiter().GetResult();
                JObject selected = null;
                try { selected = JObject.Parse(output); }
                catch (JsonException) { }
                bool valid = false;
                try { valid = selected != null && validate(selected); }
                catch (Exception error) when (error is FormatException || error is InvalidCastException
                    || error is ArgumentException || error is OverflowException) { }
                if (valid)
                {
                    string action = summary(selected);
                    RecordDecision(kind, action, prompt, selected);
                    game.ResponsesDecision?.Invoke(kind, action);
                    game.ResponsesStatus?.Invoke(action);
                    return selected;
                }
                input["validation_error"] = "The previous response was not a legal selection. " + constraint;
                input["previous_response"] = output.Length <= 4096 ? output : output.Substring(0, 4096);
            }
            throw new InvalidOperationException("AI 连续返回不合法的选择，对战已停止。请重试或调整模型。" );
        }

        private void RecordDecision(string kind, string action, JObject prompt, JObject answer)
        {
            memory.Record(kind, action, prompt, answer, duel.Turn, duel.Phase.ToString());
        }

        private static void NumberChoices(JArray choices)
        {
            for (int i = 0; i < choices.Count; i++)
                if (choices[i] is JObject choice) choice["index"] = i;
        }

        private static JObject ObjectSchema(string property, JObject item)
        {
            return new JObject
            {
                ["type"] = "object", ["properties"] = new JObject { [property] = item },
                ["required"] = new JArray(property), ["additionalProperties"] = false
            };
        }

        private static JObject IndexSchema(int count)
        {
            var schema = new JObject { ["type"] = "integer" };
            // Structured Outputs limits enum cardinality. Larger declare-card prompts validate locally.
            if (count <= 512) schema["enum"] = new JArray(Enumerable.Range(0, count));
            else { schema["minimum"] = 0; schema["maximum"] = count - 1; }
            return schema;
        }

        public int Choice(string kind, JArray choices, JObject details = null)
        {
            if (choices.Count == 0) throw new InvalidOperationException("规则核心没有可选操作。" );
            NumberChoices(choices);
            var prompt = details ?? new JObject();
            prompt["choices"] = choices;
            if (choices.Count == 1)
            {
                RecordDecision(kind, ChoiceLabel(choices[0] as JObject), prompt, new JObject { ["choice"] = 0 });
                return 0;
            }
            var result = Request(kind, prompt, ObjectSchema("choice", IndexSchema(choices.Count)),
                r => r["choice"]?.Type == JTokenType.Integer && (int)r["choice"] >= 0 && (int)r["choice"] < choices.Count,
                "Return one choice index in [0," + (choices.Count - 1) + "].",
                r => ChoiceLabel(choices[(int)r["choice"]] as JObject));
            return (int)result["choice"];
        }

        private static string ChoiceLabel(JObject choice)
        {
            if (choice == null) return "选择";
            var card = choice["card"] as JObject;
            return (string)choice["action"] + (card == null ? "" : " · " + (string)card["name"])
                + ((string)choice["label"] ?? "");
        }

        private int[] SelectIndices(string kind, JArray choices, int min, int max, bool cancelable,
            JObject details = null, Func<int[], bool> additionalValidation = null, bool permutation = false)
        {
            NumberChoices(choices);
            var prompt = details ?? new JObject();
            prompt["choices"] = choices;
            prompt["min"] = min; prompt["max"] = max; prompt["cancelable"] = cancelable;
            prompt["ordered"] = permutation;
            if (choices.Count == 0 || max == 0)
            {
                var empty = new int[0];
                if (ResponsesSelectionValidator.ValidIndices(empty, choices.Count, min, max, cancelable)
                    && (additionalValidation == null || additionalValidation(empty)))
                { RecordDecision(kind, kind + "：无选择", prompt, new JObject { ["selected"] = new JArray() }); return empty; }
                throw new InvalidOperationException("规则核心没有满足约束的候选。");
            }
            // An empty selection is another legal result when finishing/canceling is allowed.
            if (!cancelable && choices.Count == 1 && min == 1 && max == 1
                && (additionalValidation == null || additionalValidation(new[] { 0 })))
            {
                RecordDecision(kind, kind + "：" + ChoiceLabel(choices[0] as JObject), prompt,
                    new JObject { ["selected"] = new JArray(0) });
                return new[] { 0 };
            }
            var schema = ObjectSchema("selected", new JObject
            {
                ["type"] = "array", ["items"] = IndexSchema(choices.Count),
                ["minItems"] = cancelable ? 0 : min, ["maxItems"] = max
            });
            var result = Request(kind, prompt, schema, r =>
            {
                var selected = ReadIntegers(r["selected"]);
                return ResponsesSelectionValidator.ValidIndices(selected, choices.Count, min, max, cancelable)
                    && (additionalValidation == null || additionalValidation(selected));
            }, "selected must contain unique legal indexes, meet min/max (or [] only when cancelable), and all prompt constraints.",
            r => kind + "：" + string.Join("、", ReadIntegers(r["selected"]).Select(i => ChoiceLabel(choices[i] as JObject))));
            return ReadIntegers(result["selected"]);
        }

        private static int[] ReadIntegers(JToken token)
        {
            var array = token as JArray;
            if (array == null || array.Any(t => t.Type != JTokenType.Integer)) return null;
            return array.Select(t => (int)t).ToArray();
        }

        public bool YesNo(string kind, JObject details = null)
        {
            bool replay = kind == "继续攻击";
            return Choice(kind, new JArray(
                new JObject { ["index"] = 0, ["action"] = replay ? "停止攻击" : "否", ["answer"] = false },
                new JObject { ["index"] = 1, ["action"] = replay ? "继续攻击" : "是", ["answer"] = true }), details) == 1;
        }

        public bool EffectYesNo(ClientCard card, int desc, int rawDescription)
        {
            return YesNo("发动效果", new JObject { ["card"] = CardView(card, true),
                ["effect"] = Describe(desc), ["effect_id"] = rawDescription > 10000 ? (JToken)rawDescription : JValue.CreateNull(),
                ["raw_description"] = rawDescription,
                ["confirmation_kind"] = rawDescription == 221 ? "trigger_offer" : rawDescription == 0 ? "unspecified_effect" : "specific_effect",
                ["operation"] = rawDescription > 10000 ? "activate" : "confirm_effect_offer" });
        }

        public int RockPaperScissors()
        {
            // Independent fair samples avoid deterministic identical models tying forever.
            // This also handles chance-based RPS card effects without spending a model request.
            return random.Next(1, 4);
        }

        public bool SelectFirst()
        {
            return Choice("先后手", new JArray(new JObject { ["action"] = "后攻" },
                new JObject { ["action"] = "先攻" })) == 1;
        }

        public MainPhaseAction Main(MainPhase main)
        {
            var choices = new JArray();
            var actions = new List<MainPhaseAction>();
            Action<IList<ClientCard>, MainPhaseAction.MainAction, string> add = (cards, action, text) =>
            {
                foreach (var c in cards)
                {
                    actions.Add(new MainPhaseAction(action, c.ActionIndex));
                    choices.Add(new JObject { ["action"] = text, ["card"] = CardView(c, true),
                        ["operation"] = action == MainPhaseAction.MainAction.Summon ? "summon"
                            : action == MainPhaseAction.MainAction.SpSummon ? "special_summon"
                            : action == MainPhaseAction.MainAction.Repos ? "change_position" : "set_card" });
                }
            };
            add(main.SummonableCards, MainPhaseAction.MainAction.Summon, "通常召唤");
            add(main.SpecialSummonableCards, MainPhaseAction.MainAction.SpSummon, "特殊召唤");
            add(main.ReposableCards, MainPhaseAction.MainAction.Repos, "改变表示形式");
            add(main.MonsterSetableCards, MainPhaseAction.MainAction.SetMonster, "盖放怪兽");
            add(main.SpellSetableCards, MainPhaseAction.MainAction.SetSpell, "盖放魔法/陷阱");
            for (int i = 0; i < main.ActivableCards.Count; i++)
            {
                var c = main.ActivableCards[i];
                int desc = main.ActivableDescs[i];
                // The original protocol's activation index is the list index, including duplicate card effects.
                actions.Add(new MainPhaseAction(MainPhaseAction.MainAction.Activate, i));
                choices.Add(new JObject { ["action"] = "发动效果", ["card"] = CardView(c, true),
                    ["effect"] = Describe(desc), ["effect_id"] = desc, ["operation"] = "activate" });
            }
            if (main.CanBattlePhase)
            {
                actions.Add(new MainPhaseAction(MainPhaseAction.MainAction.ToBattlePhase));
                choices.Add(new JObject { ["action"] = "进入战斗阶段", ["operation"] = "advance_phase" });
            }
            if (main.CanEndPhase)
            {
                actions.Add(new MainPhaseAction(MainPhaseAction.MainAction.ToEndPhase));
                choices.Add(new JObject { ["action"] = "结束回合", ["operation"] = "end_turn" });
            }
            return actions[Choice("主要阶段", choices)];
        }

        public BattlePhaseAction Battle(BattlePhase battle)
        {
            var choices = new JArray();
            var actions = new List<BattlePhaseAction>();
            for (int i = 0; i < battle.ActivableCards.Count; i++)
            {
                var c = battle.ActivableCards[i];
                actions.Add(new BattlePhaseAction(BattlePhaseAction.BattleAction.Activate, new[] { i }));
                choices.Add(new JObject { ["action"] = "发动效果", ["card"] = CardView(c, true),
                    ["effect"] = Describe(battle.ActivableDescs[i]), ["effect_id"] = battle.ActivableDescs[i], ["operation"] = "activate" });
            }
            for (int i = 0; i < battle.AttackableCards.Count; i++)
            {
                var c = battle.AttackableCards[i];
                actions.Add(new BattlePhaseAction(BattlePhaseAction.BattleAction.Attack, new[] { 0, i }));
                choices.Add(new JObject { ["action"] = "攻击", ["card"] = CardView(c, true),
                    ["can_direct_attack"] = c.CanDirectAttack, ["operation"] = "attack" });
            }
            if (battle.CanMainPhaseTwo)
            {
                actions.Add(new BattlePhaseAction(BattlePhaseAction.BattleAction.ToMainPhaseTwo));
                choices.Add(new JObject { ["action"] = "进入主要阶段2", ["operation"] = "advance_phase" });
            }
            if (battle.CanEndPhase)
            {
                actions.Add(new BattlePhaseAction(BattlePhaseAction.BattleAction.ToEndPhase));
                choices.Add(new JObject { ["action"] = "结束回合", ["operation"] = "end_turn" });
            }
            return actions[Choice("战斗阶段", choices)];
        }

        public IList<ClientCard> Cards(IList<ClientCard> cards, int min, int max, int hint, bool cancelable)
        {
            int[] indexes = SelectIndices("选择卡片", CardChoices(cards), min, Math.Min(max, cards.Count), cancelable,
                new JObject { ["hint"] = Describe(hint), ["hint_id"] = hint, ["purpose"] = SelectionPurpose(hint) });
            return indexes.Select(i => cards[i]).ToList();
        }

        public IList<ClientCard> ToggleCards(IList<ClientCard> cards, int selectableCount, int hint, bool finishable, bool cancelable,
            int overallMin = 0, int overallMax = 0)
        {
            var choices = CardChoices(cards);
            for (int i = 0; i < choices.Count; i++)
            {
                choices[i]["action"] = i < selectableCount ? "选择" : "取消已选";
                choices[i]["already_selected"] = i >= selectableCount;
            }
            var indexes = SelectIndices("选择/取消材料", choices, 1, 1, finishable || cancelable,
                new JObject { ["hint"] = Describe(hint), ["finishable"] = finishable,
                    ["overall_min"] = overallMin, ["overall_max"] = overallMax,
                    ["listed_selected_count"] = cards.Count - selectableCount, ["hint_id"] = hint,
                    ["purpose"] = SelectionPurpose(hint),
                    ["instruction"] = "Choose one card to toggle, or [] to finish/cancel when permitted." });
            return indexes.Select(i => cards[i]).ToList();
        }

        public int Chain(IList<ClientCard> cards, IList<int> descs, IList<bool> forces, int timing)
        {
            var choices = new JArray();
            var indexes = new List<int>();
            for (int i = 0; i < cards.Count; i++)
            {
                indexes.Add(i);
                choices.Add(new JObject { ["action"] = "连锁发动", ["card"] = CardView(cards[i], true),
                    ["effect"] = Describe(descs[i]), ["effect_id"] = descs[i],
                    ["forced"] = forces[i], ["operation"] = "activate" });
            }
            if (!forces.Any(f => f))
            {
                indexes.Add(-1); choices.Add(new JObject { ["action"] = "不连锁", ["operation"] = "pass" });
            }
            return indexes[Choice("连锁", choices, new JObject { ["timing"] = timing })];
        }

        public IList<int> Counters(int type, int quantity, IList<ClientCard> cards, IList<int> capacities)
        {
            var choices = CardChoices(cards);
            for (int i = 0; i < choices.Count; i++) choices[i]["capacity"] = capacities[i];
            var prompt = new JObject { ["choices"] = choices, ["counter_type"] = type, ["total"] = quantity,
                ["instruction"] = "Return one nonnegative used-counter amount per candidate, within capacity and summing exactly to total." };
            var result = Request("移除指示物", prompt, ObjectSchema("amounts", new JObject
            {
                ["type"] = "array", ["minItems"] = cards.Count, ["maxItems"] = cards.Count,
                ["items"] = new JObject { ["type"] = "integer", ["minimum"] = 0 }
            }), r => ResponsesSelectionValidator.ValidCounters(ReadIntegers(r["amounts"]), capacities, quantity),
            "amounts must have one entry per candidate, each within capacity, and sum exactly to total.",
            r => "移除指示物：" + string.Join(",", ReadIntegers(r["amounts"])));
            return ReadIntegers(result["amounts"]);
        }

        public IList<ClientCard> Sort(IList<ClientCard> cards, bool chainOrder = false)
        {
            var indexes = SelectIndices(chainOrder ? "排列连锁" : "排列卡片", CardChoices(cards), cards.Count, cards.Count, false,
                new JObject { ["operation"] = chainOrder ? "order_chain" : "order_cards",
                    ["instruction"] = chainOrder
                        ? "Return all candidate indexes exactly once in order of placement on the chain: first has the lowest link and resolves last."
                        : "Return all candidate indexes exactly once in desired top-to-bottom order." }, null, true);
            return indexes.Select(i => cards[i]).ToList();
        }

        public int Option(IList<int> options)
        {
            return Choice("效果选项", new JArray(options.Select((desc, index) =>
                new JObject { ["index"] = index, ["label"] = Describe(desc), ["effect_id"] = desc })));
        }

        public int Place(int cardId, int player, CardLocation location, int available)
        {
            var choices = new JArray();
            var bits = new List<int>();
            for (int i = 0; i < 8; i++)
                if ((available & (1 << i)) != 0)
                {
                    bits.Add(1 << i);
                    choices.Add(new JObject { ["action"] = "选择区域", ["sequence"] = i });
                }
            return bits[Choice("选择区域", choices, new JObject { ["card_id"] = cardId,
                ["player"] = player, ["location"] = location.ToString() })];
        }

        public IList<int> Zones(IList<uint> masks, JArray choices, int count, int hint, bool cancelable, JObject details = null)
        {
            var prompt = details ?? new JObject();
            if (prompt["placing_card"] == null) prompt["hint"] = Describe(hint);
            prompt["hint_id"] = hint;
            prompt["instruction"] = "Select exactly count distinct physical zones.";
            return SelectIndices("选择场地区域", choices, count, count, cancelable,
                prompt,
                ids => ids.Length == 0 && cancelable || ResponsesSelectionValidator.ValidZones(ids, masks, count));
        }

        private static string SelectionPurpose(int hint)
        {
            // A core hint describes the requested selection, not whether it pays a cost or resolves an effect.
            switch (hint)
            {
                case HintMsg.Release: case HintMsg.Tribute: return "tribute";
                case HintMsg.Discard: return "discard";
                case HintMsg.Destroy: return "destroy";
                case HintMsg.Remove: return "banish";
                case HintMsg.ToGrave: return "send_to_graveyard";
                case HintMsg.AddToHand: return "add_to_hand";
                case HintMsg.SpSummon: return "special_summon";
                case HintMsg.FusionMaterial: return "fusion_material";
                case HintMsg.SynchroMaterial: return "synchro_material";
                case HintMsg.XyzMaterial: return "xyz_material";
                case HintMsg.LinkMaterial: return "link_material";
                case HintMsg.RemoveXyz: return "detach_overlay";
                case HintMsg.AttackTarget: return "attack_target";
                case HintMsg.Target: return "effect_target";
                default: return "unspecified";
            }
        }

        public CardPosition Position(int cardId, IList<CardPosition> positions)
        {
            return positions[Choice("表示形式", new JArray(positions.Select(p =>
                new JObject { ["action"] = p.ToString() })), new JObject { ["card_id"] = cardId,
                    ["card_name"] = NamedCard.Get(cardId)?.Name })];
        }

        private static ResponsesSumValue SumValue(ClientCard card)
        { return new ResponsesSumValue(card.OpParam1, card.OpParam2); }

        public IList<ClientCard> Sum(IList<ClientCard> cards, IList<ClientCard> mandatory,
            int target, int min, int max, int hint, bool exact)
        {
            var choices = CardChoices(cards);
            for (int i = 0; i < cards.Count; i++)
            {
                choices[i]["primary"] = cards[i].OpParam1;
                choices[i]["alternative"] = cards[i].OpParam2;
            }
            var mandatoryViews = CardChoices(mandatory ?? new List<ClientCard>());
            for (int i = 0; i < mandatoryViews.Count; i++)
            {
                mandatoryViews[i]["primary"] = mandatory[i].OpParam1;
                mandatoryViews[i]["alternative"] = mandatory[i].OpParam2;
            }
            var values = cards.Select(SumValue).ToArray();
            var mustValues = (mandatory ?? new List<ClientCard>()).Select(SumValue).ToArray();
            var indexes = SelectIndices("数值/等级合计", choices, exact ? min : 0,
                exact ? Math.Min(max, cards.Count) : cards.Count, false,
                new JObject { ["target"] = target, ["exact"] = exact, ["mandatory"] = mandatoryViews,
                    ["hint"] = Describe(hint), ["hint_id"] = hint, ["purpose"] = SelectionPurpose(hint), ["instruction"] = exact
                        ? "Mandatory cards always contribute but do not count against min/max. Choose one primary or nonzero alternative value for every selected/mandatory card so the total equals target."
                        : "Mandatory cards contribute. Choose a minimal sufficient subset: sum(max(primary,alternative))>=target and sum(min(primary,nonzero alternative))-smallest_minimum<target." },
                selected => ResponsesSelectionValidator.ValidSum(values, selected, mustValues, target, min, max, exact));
            return indexes.Select(i => cards[i]).ToList();
        }

        public IList<ClientCard> Tribute(IList<ClientCard> cards, int min, int max, int hint, bool cancelable)
        {
            var choices = CardChoices(cards);
            var weights = cards.Select(c => c.OpParam1).ToArray();
            for (int i = 0; i < cards.Count; i++) choices[i]["release_value"] = weights[i];
            var indexes = SelectIndices("选择祭品", choices, 0, Math.Min(max, cards.Count), cancelable,
                new JObject { ["minimum_release_value"] = min, ["hint"] = Describe(hint),
                    ["hint_id"] = hint, ["purpose"] = "tribute",
                    ["instruction"] = "Total release_value must meet minimum_release_value; max is the number of cards, not summed tribute weight." },
                selected => ResponsesSelectionValidator.ValidTributes(selected, weights, min, max, cancelable));
            return indexes.Select(i => cards[i]).ToList();
        }

        public int AnnounceCard(IList<int> available)
        {
            // Names remain compact even when the rule core allows the whole card database.
            return available[Choice("宣言卡名", new JArray(available.Select((id, index) => new JObject
                { ["index"] = index, ["card_id"] = id, ["label"] = NamedCard.Get(id)?.Name ?? id.ToString() })))];
        }

        public int Number(IList<int> numbers)
        {
            return Choice("宣言数值", new JArray(numbers.Select(number => new JObject { ["label"] = number.ToString() })));
        }

        public IList<T> Announce<T>(int count, IList<T> available, string kind)
        {
            var indexes = SelectIndices(kind, new JArray(available.Select(v => new JObject { ["label"] = v.ToString() })),
                count, count, false);
            return indexes.Select(i => available[i]).ToList();
        }
    }
}
