using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using YGOSharp.OCGWrapper;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI
{
    /// <summary>Immutable submitted choices, distinct from confirmed execution and current card locations.</summary>
    public sealed class ResponsesDecisionMemory
    {
        private readonly Queue<JObject> recent = new Queue<JObject>();
        private static readonly HashSet<string> PendingOperations = new HashSet<string>(StringComparer.Ordinal)
            { "activate", "summon", "special_summon", "attack", "set_card", "change_position" };
        private JObject pendingOperation;
        private JObject resolvingEffect;

        private static int ChoiceIndex(JToken token, int count)
        {
            if (token?.Type != JTokenType.Integer
                || !long.TryParse(token.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long index)
                || index < 0 || index >= count) return -1;
            return (int)index;
        }

        private static JArray SelectedChoices(JObject prompt, JObject answer)
        {
            var selected = new JArray();
            var choices = prompt?["choices"] as JArray;
            if (choices == null || answer == null) return selected;
            if (answer["choice"] != null)
            {
                int index = ChoiceIndex(answer["choice"], choices.Count);
                if (index >= 0) selected.Add(choices[index].DeepClone());
            }
            else if (answer["selected"] is JArray indexes)
            {
                foreach (var token in indexes)
                {
                    int index = ChoiceIndex(token, choices.Count);
                    if (index >= 0) selected.Add(choices[index].DeepClone());
                }
            }
            else if (answer["amounts"] is JArray amounts)
            {
                for (int i = 0; i < choices.Count && i < amounts.Count; i++)
                {
                    var choice = choices[i].DeepClone();
                    if (choice is JObject item) item["selected_amount"] = amounts[i].DeepClone();
                    else choice = new JObject { ["candidate"] = choice, ["selected_amount"] = amounts[i].DeepClone() };
                    selected.Add(choice);
                }
            }
            return selected;
        }

        /// <summary>Call only after local validation, including for automatic unique selections.</summary>
        public void Record(string kind, string action, JObject prompt, JObject answer, int turn, string phase)
        {
            var constraints = prompt == null ? new JObject() : (JObject)prompt.DeepClone();
            constraints.Remove("choices");
            var selected = SelectedChoices(prompt, answer);
            var entry = new JObject
            {
                ["kind"] = kind, ["action"] = action, ["turn"] = turn, ["phase"] = phase,
                ["status"] = "submitted_selection", ["selected_choices"] = selected,
                ["constraints"] = constraints, ["answer"] = answer?.DeepClone()
            };
            recent.Enqueue(entry);
            while (recent.Count > 16) recent.Dequeue();

            // Each major decision starts an independent operation. A declined activation
            // question also ends the preceding tentative operation rather than reviving it.
            string promptOperation = constraints["operation"]?.Type == JTokenType.String ? (string)constraints["operation"] : null;
            bool activationQuestion = promptOperation == "activate";
            bool confirmationQuestion = kind == "发动效果" && promptOperation == "confirm_effect_offer";
            if (kind == "主要阶段" || kind == "战斗阶段" || kind == "连锁" || activationQuestion || confirmationQuestion)
                pendingOperation = null;
            // A confirmation offer with an unspecified effect is not a confirmed source,
            // regardless of whether the player accepts or declines it.
            if (confirmationQuestion) return;

            var source = selected.Count == 1 && answer?["choice"] != null ? selected[0] as JObject : null;
            string operation = source?["operation"]?.Type == JTokenType.String ? (string)source["operation"] : null;
            if (activationQuestion)
            {
                operation = ChoiceIndex(answer?["choice"], 2) == 1 ? "activate" : null;
                source = operation == null ? null : source;
            }
            if (source == null || string.IsNullOrEmpty(operation)) return;
            if (!PendingOperations.Contains(operation))
            {
                pendingOperation = null;
                return;
            }
            pendingOperation = new JObject
            {
                ["operation"] = operation, ["kind"] = kind, ["turn"] = turn, ["phase"] = phase,
                ["status"] = "submitted_selection", ["source_location_semantics"] = "snapshot_at_selection",
                ["source_choice"] = source.DeepClone(), ["source_details"] = constraints.DeepClone()
            };
        }

        /// <summary>Observe before normal handlers, preserving the protocol reader's position.</summary>
        public void Observe(GameMessage message, BinaryReader packet, Duel duel)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            long position = packet.BaseStream.Position;
            try
            {
                switch (message)
                {
                    case GameMessage.Start:
                        recent.Clear(); pendingOperation = resolvingEffect = null;
                        break;
                    case GameMessage.NewTurn:
                    case GameMessage.NewPhase:
                    case GameMessage.ChainEnd:
                    case GameMessage.Win:
                    case GameMessage.Retry:
                        pendingOperation = resolvingEffect = null;
                        break;
                    case GameMessage.Chaining:
                    case GameMessage.Summoned:
                    case GameMessage.SpSummoned:
                    case GameMessage.FlipSummoned:
                    case GameMessage.Set:
                    case GameMessage.PosChange:
                    case GameMessage.Attack:
                        pendingOperation = null;
                        break;
                    case GameMessage.ChainSolving:
                        pendingOperation = null;
                        int link = packet.ReadByte();
                        var chains = duel?.CurrentChainInfo;
                        resolvingEffect = null;
                        if (chains == null || link <= 0 || link > chains.Count || chains[link - 1] == null) break;
                        var info = chains[link - 1];
                        // ChainInfo stores the publicly announced activation, not an inferred
                        // identity from a card's later hidden location or mutable view.
                        resolvingEffect = new JObject
                        {
                            ["status"] = "engine_resolving", ["link"] = link, ["player"] = info.ActivatePlayer,
                            ["card_id"] = info.ActivateId, ["card_name"] = NamedCard.Get(info.ActivateId)?.Name ?? "",
                            ["card_text"] = NamedCard.Get(info.ActivateId)?.Description ?? "",
                            ["effect_id"] = info.ActivateDescription,
                            ["effect"] = ResponsesDecisionController.Describe(info.ActivateDescription),
                            ["activation_controller"] = info.ActivateController,
                            ["activation_location"] = info.ActivateLocation.ToString(),
                            ["activation_sequence"] = info.ActivateSequence,
                            ["activation_position"] = ((CardPosition)info.ActivatePosition).ToString(),
                            ["source_location_semantics"] = "snapshot_at_activation"
                        };
                        break;
                    case GameMessage.ChainSolved:
                        int solvedLink = packet.ReadByte();
                        if (resolvingEffect != null && (int)resolvingEffect["link"] == solvedLink)
                            resolvingEffect = null;
                        break;
                }
            }
            catch (EndOfStreamException)
            {
                // A malformed context packet must not leave a tentative source or block
                // the existing protocol handler; it remains responsible for the packet.
                pendingOperation = resolvingEffect = null;
            }
            finally { packet.BaseStream.Position = position; }
        }

        public JObject Snapshot()
        {
            return new JObject
            {
                ["recent_decisions"] = new JArray(recent.Select(entry => entry.DeepClone())),
                ["pending_operation"] = pendingOperation?.DeepClone(),
                ["resolving_effect"] = resolvingEffect?.DeepClone()
            };
        }
    }
}
