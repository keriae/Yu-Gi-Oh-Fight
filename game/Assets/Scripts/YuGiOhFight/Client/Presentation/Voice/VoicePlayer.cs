using Newtonsoft.Json;
using System;


using System.Linq;


using YuGiOhFight.Client.Runtime.Network;


namespace YuGiOhFight.Client.Presentation.Voice
{
    public static class VoicePlayer
    {
        private const string voicePath = "Sound/Voice/";
        private const string customVoicePath = "Sound/CustomVoice/";
        private const string jsonPath = "Data/locales/";
        public const string defaultCharacter = "0001";

        public static string loadedLanguage;
        public static string heroString;
        public static string rivalString;
        public static int heroCode => int.Parse(heroString);
        public static int rivalCode => int.Parse(rivalString);

        public static VoicesData heroVoices;
        public static VoicesData rivalVoices;
        public static LinesData heroLines;
        public static LinesData rivalLines;

        public struct VoiceData
        {
            public string name;
            public int num;
            public bool isHero;
            public bool wait;
            public float delay;
        }

        public static void LoadData()
        {
            Controllers.OcgCore.Condition condition = Controllers.OcgCore.condition;
            var chara = Services.ResponsesDuelSession.AppearanceForSide(0, "Character", Services.ReplayAppearance.Get(condition + "Character0", defaultCharacter));
            var language = MDPro3.Utility.Language.GetConfig();

            if (language != loadedLanguage || heroString != chara)
            {
                var originalChara = Controllers.CharacterSelector.characters.GetCharacterOriginalId(chara);

                heroString = chara;
                var dataPath = jsonPath + MDPro3.Utility.Language.Japanese + "/voice/V" + originalChara + ".json";
                var txt = System.IO.File.ReadAllText(dataPath);
                heroVoices = JsonConvert.DeserializeObject<VoicesData>(txt);

                dataPath = jsonPath + language + "/voice/SN" + originalChara + ".json";
                txt = System.IO.File.ReadAllText(dataPath);
                heroLines = JsonConvert.DeserializeObject<LinesData>(txt);
            }

            chara = Services.ResponsesDuelSession.AppearanceForSide(1, "Character", Services.ReplayAppearance.Get(condition + "Character1", defaultCharacter));
            if (language != loadedLanguage || rivalString != chara)
            {
                var originalChara = Controllers.CharacterSelector.characters.GetCharacterOriginalId(chara);

                rivalString = chara;
                var dataPath = jsonPath + MDPro3.Utility.Language.Japanese + "/voice/V" + originalChara + ".json";
                var txt = System.IO.File.ReadAllText(dataPath);
                rivalVoices = JsonConvert.DeserializeObject<VoicesData>(txt);

                dataPath = jsonPath + language + "/voice/SN" + originalChara + ".json";
                txt = System.IO.File.ReadAllText(dataPath);
                rivalLines = JsonConvert.DeserializeObject<LinesData>(txt);
            }

            loadedLanguage = language;
        }

        public static System.Collections.Generic.List<string>[] GetVoicePaths(System.Collections.Generic.List<VoiceData> data)
        {
            var returnValue = new System.Collections.Generic.List<string>[data.Count];
            for (int i = 0; i < data.Count; i++)
                returnValue[i] = new System.Collections.Generic.List<string>();
            for (int i = 0; i < data.Count; i++)
            {
                for (int j = 0; j < data[i].num; j++)
                {
                    var path = voicePath + data[i].name[..5] + Runtime.Program.STRING_SLASH + data[i].name + "_" + j + ".ogg";
                    returnValue[i].Add(path);
                }
            }

            return returnValue;
        }

        public static LineInfo GetLine(string voiceName, bool isHero)
        {
            if (heroLines == null || rivalLines == null)
                return null;

            var target = isHero ? heroLines : rivalLines;
            if (target.info.TryGetValue(voiceName, out var line))
                return line;
            else if ((!isHero ? heroLines : rivalLines).info.TryGetValue(voiceName, out var line2))
                return line2;
            else
                return null;
        }

        public static string GetVoiceBySubCategory(VoiceInfoEntry entry, int sub, int subInCase, int patternIndex, bool patternRestrict = false)
        {
            var tempStrings = new System.Collections.Generic.List<string>();

            foreach (var e in entry.rawKvp)
                if (e.Value.subCategoryIndex == sub)
                {
                    if (e.Value.patternIndex == patternIndex)
                        return e.Value.shortName;
                    else
                        tempStrings.Add(e.Value.shortName);
                }
            if (tempStrings.Count == 0)
                foreach (var e in entry.rawKvp)
                    if (e.Value.subCategoryIndex == subInCase)
                    {
                        if (e.Value.patternIndex == patternIndex)
                            return e.Value.shortName;
                        else
                            tempStrings.Add(e.Value.shortName);
                    }

            if (patternRestrict)
                return null;

            if (tempStrings.Count > 0)
                return tempStrings[UnityEngine.Random.Range(0, tempStrings.Count)];
            UnityEngine.Debug.LogErrorFormat("Did not find Subcategory {0}-{1} in {2}", sub, subInCase, entry.rawKvp.First().Key[..8]);
            return null;
        }

        public static string GetVoiceBySituation(VoiceInfoEntry entry, int situation)
        {
            var returnValue = Utility.Tools.GetRandomDictionaryElement(entry.rawKvp).Value.shortName;
            var tempStrings = new System.Collections.Generic.List<string>();
            foreach (var e in entry.rawKvp)
                if (e.Value.situations != null && e.Value.situations.Length > 0)
                    if (Array.IndexOf(e.Value.situations, situation) >= 0)
                        tempStrings.Add(e.Value.shortName);
            if (tempStrings.Count > 0)
                returnValue = tempStrings[UnityEngine.Random.Range(0, tempStrings.Count)];
            return returnValue;
        }

        public static string GetVoiceByDuelist(VoiceInfoEntry entry, VoiceInfoEntry entrySp, int duelist)
        {
            var returnValue = Utility.Tools.GetRandomDictionaryElement(entry.rawKvp).Value.shortName;
            if (entrySp == null)
                return returnValue;
            var tempStrings = new System.Collections.Generic.List<string>();
            foreach (var e in entrySp.rawKvp)
                for (var i = 0; i < e.Value.duelists.Length; i++)
                    if (Convert.ToInt32(e.Value.duelists[i]) == duelist)
                        tempStrings.Add(e.Value.shortName);
            if (tempStrings.Count > 0)
                returnValue = tempStrings[UnityEngine.Random.Range(0, tempStrings.Count)];
            return returnValue;
        }

        private static bool DataHaveVoice(VoiceInfoEntry entry, int sub, int patternIndex)
        {
            if (entry == null)
                return false;

            foreach (var e in entry.rawKvp)
                if (e.Value.subCategoryIndex == sub)
                    if (e.Value.patternIndex == patternIndex)
                        return true;
            return false;
        }

        public static int GetVoiceNum(VoicesData target, string key)
        {
            if (key == null)
            {
                UnityEngine.Debug.LogWarning("String key for GetVoiceNum is null. ");
                return 1;
            }

            if (target.NumVoices.TryGetValue(key, out var numVoices))
                return numVoices;
            else
            {
                UnityEngine.Debug.LogFormat("Did not Find NumVoices \'{0}\' in {1} ", key, target.BeforeDuel.rawKvp.First().Key[..5]);
                return 1;
            }
        }

        public static bool DamageIsBig(int damage)
        {
            if (damage >= 2000)
                return true;
            else
                return false;
        }

        public static VoiceData GetFinishDamageVoiceData(VoicesData target, bool isHero)
        {
            var data = new VoiceData();
            data.name = Utility.Tools.GetRandomDictionaryElement(target.FinishDamage.rawKvp).Value.shortName;
            data.num = GetVoiceNum(target, data.name);
            data.isHero = isHero;
            data.wait = false;
            data.delay = 0;
            return data;
        }

        public static VoiceData GetBeforeCardEffectData(VoicesData target, bool isHero)
        {
            var data = new VoiceData();
            data.name = Utility.Tools.GetRandomDictionaryElement(target.BeforeCardEffect.rawKvp).Value.shortName;
            data.num = GetVoiceNum(target, data.name);
            data.isHero = isHero;
            data.wait = true;
            data.delay = 0;
            return data;
        }

        public static VoiceData GetBeforeSummonData(VoicesData target, bool isHero)
        {
            var data = new VoiceData();
            data.name = Utility.Tools.GetRandomDictionaryElement(target.BeforeSummon.rawKvp).Value.shortName;
            data.num = GetVoiceNum(target, data.name);
            data.isHero = isHero;
            data.wait = true;
            data.delay = 0;
            return data;
        }

        public static VoiceData GetVoiceByCard(VoicesData target, VoiceInfoEntry entry, int card, int engineparam, bool isMe)
        {
            var returnValue = new VoiceData();
            returnValue.name = string.Empty;

            if (entry == null)
                return returnValue;
            card = GetCidDefaultAltCard(card);
            var cid = MDPro3.Cid2Ydk.GetCID(card);
            if (cid == card)
                return returnValue;

            var tempStrings = new System.Collections.Generic.List<string>();
            foreach (var value in entry.rawKvp.Values)
            {
                if (value.cards == null)
                    continue;
                if (value.cards.Contains(cid))
                {
                    tempStrings.Add(value.shortName);
                    if (value.engineparams != null && value.engineparams.Contains(engineparam))
                    {
                        tempStrings.Clear();
                        tempStrings.Add(value.shortName);
                        break;
                    }
                }
            }
            if (tempStrings.Count > 0)
                returnValue.name = tempStrings[UnityEngine.Random.Range(0, tempStrings.Count)];
            if (returnValue.name != string.Empty)
            {
                returnValue.num = GetVoiceNum(target, returnValue.name);
                returnValue.isHero = isMe;
                returnValue.wait = true;
                returnValue.delay = 0f;
            }

            return returnValue;
        }

        public static int GetCidDefaultAltCard(int card)
        {
            var data = MDPro3.Duel.YGOSharp.CardsManager.Get(card);
            if (card == 89631139 || data.Alias == 89631139)//青眼白龙
                return 89631141;
            if (card == 46986414 || data.Alias == 46986414)//黑魔术师
                return 46986417;
            return card;
        }

        public static void ExportAllCardsNotFound()
        {
            var ids = new System.Collections.Generic.List<int>();

            var folder = jsonPath + MDPro3.Utility.Language.SimplifiedChinese + "/voice/";
            var jsons = System.IO.Directory.GetFiles(folder);
            var vjsons = new System.Collections.Generic.List<string>();
            foreach (var json in jsons)
            {
                var fileName = System.IO.Path.GetFileName(json);
                if (fileName.StartsWith("V"))
                    vjsons.Add(json);
            }

            foreach (var v in vjsons)
            {
                var data = JsonConvert.DeserializeObject<VoicesData>(System.IO.File.ReadAllText(v));
                var cardEntries = data.GetEntryWithCard();
                foreach (var entry in cardEntries)
                {
                    if (entry == null)
                        continue;
                    foreach (var value in entry.rawKvp.Values)
                    {
                        if (value.cards == null)
                            continue;

                        foreach (var card in value.cards)
                        {
                            if (!ids.Contains(card))
                                ids.Add(card);
                        }
                    }
                }
            }

            var text = string.Empty;
            foreach (var id in ids)
            {
                if (!MDPro3.Cid2Ydk.HaveCid(id))
                    text += id + Runtime.Program.STRING_LINE_BREAK;
            }
            System.IO.File.WriteAllText("Data/Duel Links Ids.txt", text);
        }

        //0
        //1 wining
        //2 losing
        public static int LeadingStateOfHero()
        {
            //TODO

            if (Controllers.OcgCore.life0 >= Controllers.OcgCore.lpLimit / 2)
            {
                if (Controllers.OcgCore.life1 < Controllers.OcgCore.lpLimit / 2)
                    return 1;
            }
            else
            {
                if (Controllers.OcgCore.life1 >= Controllers.OcgCore.lpLimit / 2)
                    return 2;

                if (Controllers.OcgCore.life0 >= Controllers.OcgCore.lpLimit / 4 && Controllers.OcgCore.life1 < Controllers.OcgCore.lpLimit / 4)
                    return 1;

                if (Controllers.OcgCore.life0 < Controllers.OcgCore.lpLimit / 4)
                    return 2;
            }
            return 0;
        }

        public static int LeadingStateOfRival()
        {
            //TODO

            if (Controllers.OcgCore.life1 >= Controllers.OcgCore.lpLimit / 2)
            {
                if (Controllers.OcgCore.life0 < Controllers.OcgCore.lpLimit / 2)
                    return 1;
            }
            else
            {
                if (Controllers.OcgCore.life0 >= Controllers.OcgCore.lpLimit / 2)
                    return 2;

                if (Controllers.OcgCore.life1 >= Controllers.OcgCore.lpLimit / 4 && Controllers.OcgCore.life0 < Controllers.OcgCore.lpLimit / 4)
                    return 1;

                if (Controllers.OcgCore.life1 < Controllers.OcgCore.lpLimit / 4)
                    return 2;
            }
            return 0;
        }

        public static bool NeedBeforeCardEffect(bool isHero)
        {
            if (Controllers.OcgCore.cardsInChain.Count == 0)
                return false;
            if ((Controllers.OcgCore.cardsInChain[^1].p.controller == 0) == isHero)
                return false;
            return true;
            //TODO
        }

    }

    [Serializable]
    public class VoiceInfo
    {
        public string fullName;
        public string shortName;
        public string voiceIdx;
        public int[] cards;
        public object[] duelists;
        public int[] skills;
        public int[] situations;
        public int[] engineparams;
        public object groupingKeys;
        public int subCategoryIndex;
        public int patternIndex;
    }

    [Serializable]
    public class VoiceInfoEntry
    {
        public System.Collections.Generic.Dictionary<string, VoiceInfo> rawKvp;
    }

    [Serializable]
    public class VoicesData
    {
        public int DummyFlag;
        public System.Collections.Generic.Dictionary<string, int> NumVoices;
        public int labelver;
        public VoiceInfoEntry BeforeDuel;
        public VoiceInfoEntry BeforeDuelSp;
        public VoiceInfoEntry DuelStart;
        public VoiceInfoEntry TurnStart;
        [JsonProperty("TurnStart.01")]
        public VoiceInfoEntry TurnStart01;
        public VoiceInfoEntry Draw;
        [JsonProperty("Draw.01")]
        public VoiceInfoEntry Draw01;
        public VoiceInfoEntry DestinyDraw;
        public VoiceInfoEntry BeforeCardEffect;
        public VoiceInfoEntry CardEffect;
        public VoiceInfoEntry MainMagicTrap;
        public VoiceInfoEntry MainMonsterEffect;
        public VoiceInfoEntry BeforeSummon;
        public VoiceInfoEntry Summon;
        public VoiceInfoEntry None;
        public VoiceInfoEntry MainMonsterSummon;
        public VoiceInfoEntry BattleStart;
        public VoiceInfoEntry BeforeAttackNormal;
        public VoiceInfoEntry BeforeAttackFinish;
        public VoiceInfoEntry Attack;
        public VoiceInfoEntry DirectAttack;
        public VoiceInfoEntry MainMonsterAttack;
        public VoiceInfoEntry CardSet;
        public VoiceInfoEntry TurnEnd;
        public VoiceInfoEntry Damage;
        public VoiceInfoEntry FinishDamage;
        public VoiceInfoEntry CostDamage;
        public VoiceInfoEntry BigDamage;
        public VoiceInfoEntry AfterDamage;
        public VoiceInfoEntry AfterBigDamage;
        public VoiceInfoEntry Win;
        public VoiceInfoEntry WinSp;
        public VoiceInfoEntry Lose;
        public VoiceInfoEntry LoseSp;
        public VoiceInfoEntry Taunt;
        public VoiceInfoEntry Surprise;
        public VoiceInfoEntry Title;
        public VoiceInfoEntry Skill;
        public VoiceInfoEntry Chat;
        public VoiceInfoEntry CharaChange;
        public VoiceInfoEntry SwitchToPartner;
        public VoiceInfoEntry BeforeMainSummon;
        public VoiceInfoEntry RidingDuelStart;
        public VoiceInfoEntry CoinTossOfMagicTrap;
        public VoiceInfoEntry CoinTossOfMonster;
        public VoiceInfoEntry BeforeDimensionDuel;
        public VoiceInfoEntry DimensionDuelStart;
        public VoiceInfoEntry Transformation;
        public VoiceInfoEntry ActionDuelStart;
        public VoiceInfoEntry ActionCard;
        public VoiceInfoEntry BeforeMainReincarnationSummon;
        public VoiceInfoEntry MainMonsterReincarnationSummon;
        public VoiceInfoEntry RushDuelStart;
        public VoiceInfoEntry RidingRushDuelStart;

        public VoiceInfoEntry GetCategoryEntry(MDPro3.VoiceController.Category category)
        {
            switch (category)
            {
                case MDPro3.VoiceController.Category.BeforeDuel:
                    return BeforeDuel;
                case MDPro3.VoiceController.Category.DuelStart:
                    return DuelStart;
                case MDPro3.VoiceController.Category.TurnStart:
                    return TurnStart;
                case MDPro3.VoiceController.Category.Draw:
                    return Draw;
                case MDPro3.VoiceController.Category.DestinyDraw:
                    return DestinyDraw;
                case MDPro3.VoiceController.Category.BeforeCardEffect:
                    return BeforeCardEffect;
                case MDPro3.VoiceController.Category.CardEffect:
                    return CardEffect;
                case MDPro3.VoiceController.Category.MainMagicTrap:
                    return MainMagicTrap;
                case MDPro3.VoiceController.Category.MainMonsterEffect:
                    return MainMonsterEffect;
                case MDPro3.VoiceController.Category.BeforeSummon:
                    return BeforeSummon;
                case MDPro3.VoiceController.Category.Summon:
                    return Summon;
                case MDPro3.VoiceController.Category.None:
                    return None;
                case MDPro3.VoiceController.Category.MainMonsterSummon:
                    return MainMonsterSummon;
                case MDPro3.VoiceController.Category.BattleStart:
                    return BattleStart;
                case MDPro3.VoiceController.Category.BeforeAttackNormal:
                    return BeforeAttackNormal;
                case MDPro3.VoiceController.Category.BeforeAttackFinish:
                    return BeforeAttackFinish;
                case MDPro3.VoiceController.Category.Attack:
                    return Attack;
                case MDPro3.VoiceController.Category.DirectAttack:
                    return DirectAttack;
                case MDPro3.VoiceController.Category.MainMonsterAttack:
                    return MainMonsterAttack;
                case MDPro3.VoiceController.Category.CardSet:
                    return CardSet;
                case MDPro3.VoiceController.Category.TurnEnd:
                    return TurnEnd;
                case MDPro3.VoiceController.Category.Damage:
                    return Damage;
                case MDPro3.VoiceController.Category.FinishDamage:
                    return FinishDamage;
                case MDPro3.VoiceController.Category.CostDamage:
                    return CostDamage;
                case MDPro3.VoiceController.Category.BigDamage:
                    return BigDamage;
                case MDPro3.VoiceController.Category.AfterDamage:
                    return AfterDamage;
                case MDPro3.VoiceController.Category.AfterBigDamage:
                    return AfterBigDamage;
                case MDPro3.VoiceController.Category.Win:
                    return Win;
                case MDPro3.VoiceController.Category.Lose:
                    return Lose;
                case MDPro3.VoiceController.Category.Taunt:
                    return Taunt;
                case MDPro3.VoiceController.Category.Surprise:
                    return Surprise;
                case MDPro3.VoiceController.Category.Title:
                    return Title;
                case MDPro3.VoiceController.Category.Skill:
                    return Skill;
                case MDPro3.VoiceController.Category.Chat:
                    return Chat;
                case MDPro3.VoiceController.Category.CharaChange:
                    return CharaChange;
                case MDPro3.VoiceController.Category.SwitchToPartner:
                    return SwitchToPartner;
                case MDPro3.VoiceController.Category.BeforeMainSummon:
                    return BeforeMainSummon;
                case MDPro3.VoiceController.Category.RidingDuelStart:
                    return RidingDuelStart;
                case MDPro3.VoiceController.Category.CoinTossOfMagicTrap:
                    return CoinTossOfMagicTrap;
                case MDPro3.VoiceController.Category.CoinTossOfMonster:
                    return CoinTossOfMonster;
                case MDPro3.VoiceController.Category.BeforeDimensionDuel:
                    return BeforeDimensionDuel;
                case MDPro3.VoiceController.Category.DimensionDuelStart:
                    return DimensionDuelStart;
                case MDPro3.VoiceController.Category.Transformation:
                    return Transformation;
                case MDPro3.VoiceController.Category.ActionDuelStart:
                    return ActionDuelStart;
                case MDPro3.VoiceController.Category.ActionCard:
                    return ActionCard;
                case MDPro3.VoiceController.Category.BeforeMainReincarnationSummon:
                    return BeforeMainReincarnationSummon;
                case MDPro3.VoiceController.Category.MainMonsterReincarnationSummon:
                    return MainMonsterReincarnationSummon;
                case MDPro3.VoiceController.Category.RushDuelStart:
                    return RushDuelStart;
                case MDPro3.VoiceController.Category.RidingRushDuelStart:
                    return RidingRushDuelStart;

                default:
                    return null;

            }
        }

        public System.Collections.Generic.List<VoiceInfoEntry> GetEntryWithCard()
        {
            var returnValue = new System.Collections.Generic.List<VoiceInfoEntry>()
            {
                MainMagicTrap,
                MainMonsterEffect,
                MainMonsterSummon,
                MainMonsterAttack,
                BeforeMainSummon,
                BeforeMainReincarnationSummon,
                MainMonsterReincarnationSummon
            };
            return returnValue;
        }

    }

    [Serializable]
    public class LineInfo
    {
        public int face;
        public int frame;
        public int cutin;
        public int[] card;
        public int[] duelist;
        public string text;
    }

    [Serializable]
    public class LinesData
    {
        public string _GRP_;
        public string _LNG_;
        public System.Collections.Generic.Dictionary<string, LineInfo> info;
    }

    public class SimpleVoiceData
    {
        public int category;
        public int subCategory;
        public bool isMe;
        public bool inHand;

        public static SimpleVoiceData GetCardEffectSubCategory(System.IO.BinaryReader r, bool fromHand, int fallbackCode = 0)
        {
            var returnValue = new SimpleVoiceData
            {
                category = (int)MDPro3.VoiceController.Category.CardEffect,
                subCategory = (int)MDPro3.VoiceController.CardEffectSub.General
            };

            r.BaseStream.Seek(0, 0);
            var code = r.ReadInt32();
            if (code == 0)
                code = fallbackCode;
            Duel.GPS gps = r.ReadGPS();
            returnValue.isMe = gps.controller == 0;
            returnValue.inHand = (gps.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Hand) > 0;

            var c = MDPro3.Duel.YGOSharp.CardsManager.Get(code);

            if (Duel.GameCard.InPendulumZoneIf(gps, code) && fromHand)
                returnValue.subCategory = (int)MDPro3.VoiceController.CardEffectSub.PendulumScale;
            else if (Duel.GameCard.InPendulumZoneIf(gps, code))
                returnValue.subCategory = (int)MDPro3.VoiceController.CardEffectSub.PendulumEffect;
            else if (c.HasType(MDPro3.Duel.YGOSharp.CardType.Monster))
                returnValue.subCategory = (int)MDPro3.VoiceController.CardEffectSub.MonsterEffect;

            if ((gps.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.MonsterZone) == 0)
            {
                if (c.HasType(MDPro3.Duel.YGOSharp.CardType.Spell))
                {
                    returnValue.subCategory = (int)MDPro3.VoiceController.CardEffectSub.Magic;
                    if (c.HasType(MDPro3.Duel.YGOSharp.CardType.QuickPlay))
                        returnValue.subCategory = (int)MDPro3.VoiceController.CardEffectSub.QuickPlayMagic;
                    if (c.HasType(MDPro3.Duel.YGOSharp.CardType.Continuous))
                        returnValue.subCategory = (int)MDPro3.VoiceController.CardEffectSub.PermanentMagic;
                    if (c.HasType(MDPro3.Duel.YGOSharp.CardType.Equip))
                        returnValue.subCategory = (int)MDPro3.VoiceController.CardEffectSub.EquipMagic;
                    if (c.HasType(MDPro3.Duel.YGOSharp.CardType.Ritual))
                        returnValue.subCategory = (int)MDPro3.VoiceController.CardEffectSub.RitualMagic;
                    if (c.HasType(MDPro3.Duel.YGOSharp.CardType.Field))
                        returnValue.subCategory = (int)MDPro3.VoiceController.CardEffectSub.FieldMagic;
                }
                if (c.HasType(MDPro3.Duel.YGOSharp.CardType.Trap))
                {
                    returnValue.subCategory = (int)MDPro3.VoiceController.CardEffectSub.Trap;
                    if (c.HasType(MDPro3.Duel.YGOSharp.CardType.Continuous))
                        returnValue.subCategory = (int)MDPro3.VoiceController.CardEffectSub.PermanentTrap;
                    if (c.HasType(MDPro3.Duel.YGOSharp.CardType.Counter))
                        returnValue.subCategory = (int)MDPro3.VoiceController.CardEffectSub.CounterTrap;
                }
            }
            return returnValue;
        }
    }
}
