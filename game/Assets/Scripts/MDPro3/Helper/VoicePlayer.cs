using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using MDPro3.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static MDPro3.VoiceController;

namespace MDPro3.Duel
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
            var condition = OcgCore.condition;
            var chara = Config.Get(condition + "Character0", defaultCharacter);
            var language = Utility.Language.GetConfig();

            if (language != loadedLanguage || heroString != chara)
            {
                var originalChara = CharacterSelector.characters.GetCharacterOriginalId(chara);

                heroString = chara;
                var dataPath = jsonPath + Utility.Language.Japanese + "/voice/V" + originalChara + ".json";
                var txt = File.ReadAllText(dataPath);
                heroVoices = JsonConvert.DeserializeObject<VoicesData>(txt);

                dataPath = jsonPath + language + "/voice/SN" + originalChara + ".json";
                txt = File.ReadAllText(dataPath);
                heroLines = JsonConvert.DeserializeObject<LinesData>(txt);
            }

            chara = Config.Get(condition + "Character1", defaultCharacter);
            if (language != loadedLanguage || rivalString != chara)
            {
                var originalChara = CharacterSelector.characters.GetCharacterOriginalId(chara);

                rivalString = chara;
                var dataPath = jsonPath + Utility.Language.Japanese + "/voice/V" + originalChara + ".json";
                var txt = File.ReadAllText(dataPath);
                rivalVoices = JsonConvert.DeserializeObject<VoicesData>(txt);

                dataPath = jsonPath + language + "/voice/SN" + originalChara + ".json";
                txt = File.ReadAllText(dataPath);
                rivalLines = JsonConvert.DeserializeObject<LinesData>(txt);
            }

            loadedLanguage = language;
        }

        public static List<string>[] GetVoicePaths(List<VoiceData> data)
        {
            var returnValue = new List<string>[data.Count];
            for (int i = 0; i < data.Count; i++)
                returnValue[i] = new List<string>();
            for (int i = 0; i < data.Count; i++)
            {
                for (int j = 0; j < data[i].num; j++)
                {
                    var path = voicePath + data[i].name[..5] + Program.STRING_SLASH + data[i].name + "_" + j + ".ogg";
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
            var tempStrings = new List<string>();

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
            Debug.LogErrorFormat("Did not find Subcategory {0}-{1} in {2}", sub, subInCase, entry.rawKvp.First().Key[..8]);
            return null;
        }

        public static string GetVoiceBySituation(VoiceInfoEntry entry, int situation)
        {
            var returnValue = Tools.GetRandomDictionaryElement(entry.rawKvp).Value.shortName;
            var tempStrings = new List<string>();
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
            var returnValue = Tools.GetRandomDictionaryElement(entry.rawKvp).Value.shortName;
            if (entrySp == null)
                return returnValue;
            var tempStrings = new List<string>();
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
                Debug.LogWarning("String key for GetVoiceNum is null. ");
                return 1;
            }

            if (target.NumVoices.TryGetValue(key, out var numVoices))
                return numVoices;
            else
            {
                Debug.LogFormat("Did not Find NumVoices \'{0}\' in {1} ", key, target.BeforeDuel.rawKvp.First().Key[..5]);
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
            data.name = Tools.GetRandomDictionaryElement(target.FinishDamage.rawKvp).Value.shortName;
            data.num = VoicePlayer.GetVoiceNum(target, data.name);
            data.isHero = isHero;
            data.wait = false;
            data.delay = 0;
            return data;
        }

        public static VoiceData GetBeforeCardEffectData(VoicesData target, bool isHero)
        {
            var data = new VoiceData();
            data.name = Tools.GetRandomDictionaryElement(target.BeforeCardEffect.rawKvp).Value.shortName;
            data.num = VoicePlayer.GetVoiceNum(target, data.name);
            data.isHero = isHero;
            data.wait = true;
            data.delay = 0;
            return data;
        }

        public static VoiceData GetBeforeSummonData(VoicesData target, bool isHero)
        {
            var data = new VoiceData();
            data.name = Tools.GetRandomDictionaryElement(target.BeforeSummon.rawKvp).Value.shortName;
            data.num = VoicePlayer.GetVoiceNum(target, data.name);
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
            card = VoicePlayer.GetCidDefaultAltCard(card);
            var cid = Cid2Ydk.GetCID(card);
            if (cid == card)
                return returnValue;

            var tempStrings = new List<string>();
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
            var data = CardsManager.Get(card);
            if (card == 89631139 || data.Alias == 89631139)//青眼白龙
                return 89631141;
            if (card == 46986414 || data.Alias == 46986414)//黑魔术师
                return 46986417;
            return card;
        }

        public static void ExportAllCardsNotFound()
        {
            var ids = new List<int>();

            var folder = jsonPath + Language.SimplifiedChinese + "/voice/";
            var jsons = Directory.GetFiles(folder);
            var vjsons = new List<string>();
            foreach (var json in jsons)
            {
                var fileName = Path.GetFileName(json);
                if (fileName.StartsWith("V"))
                    vjsons.Add(json);
            }

            foreach (var v in vjsons)
            {
                var data = JsonConvert.DeserializeObject<VoicesData>(File.ReadAllText(v));
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
                if (!Cid2Ydk.HaveCid(id))
                    text += id + Program.STRING_LINE_BREAK;
            }
            File.WriteAllText("Data/Duel Links Ids.txt", text);
        }

        //0 
        //1 wining
        //2 losing
        public static int LeadingStateOfHero()
        {
            //TODO

            if (OcgCore.life0 >= OcgCore.lpLimit / 2)
            {
                if (OcgCore.life1 < OcgCore.lpLimit / 2)
                    return 1;
            }
            else
            {
                if (OcgCore.life1 >= OcgCore.lpLimit / 2)
                    return 2;

                if (OcgCore.life0 >= OcgCore.lpLimit / 4 && OcgCore.life1 < OcgCore.lpLimit / 4)
                    return 1;

                if (OcgCore.life0 < OcgCore.lpLimit / 4)
                    return 2;
            }
            return 0;
        }

        public static int LeadingStateOfRival()
        {
            //TODO

            if (OcgCore.life1 >= OcgCore.lpLimit / 2)
            {
                if (OcgCore.life0 < OcgCore.lpLimit / 2)
                    return 1;
            }
            else
            {
                if (OcgCore.life0 >= OcgCore.lpLimit / 2)
                    return 2;

                if (OcgCore.life1 >= OcgCore.lpLimit / 4 && OcgCore.life0 < OcgCore.lpLimit / 4)
                    return 1;

                if (OcgCore.life1 < OcgCore.lpLimit / 4)
                    return 2;
            }
            return 0;
        }

        public static bool NeedBeforeCardEffect(bool isHero)
        {
            if (OcgCore.cardsInChain.Count == 0)
                return false;
            if ((OcgCore.cardsInChain[^1].p.controller == 0) == isHero)
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
        public Dictionary<string, VoiceInfo> rawKvp;
    }

    [Serializable]
    public class VoicesData
    {
        public int DummyFlag;
        public Dictionary<string, int> NumVoices;
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

        public VoiceInfoEntry GetCategoryEntry(Category category)
        {
            switch (category)
            {
                case Category.BeforeDuel:
                    return BeforeDuel;
                case Category.DuelStart:
                    return DuelStart;
                case Category.TurnStart:
                    return TurnStart;
                case Category.Draw:
                    return Draw;
                case Category.DestinyDraw:
                    return DestinyDraw;
                case Category.BeforeCardEffect:
                    return BeforeCardEffect;
                case Category.CardEffect:
                    return CardEffect;
                case Category.MainMagicTrap:
                    return MainMagicTrap;
                case Category.MainMonsterEffect:
                    return MainMonsterEffect;
                case Category.BeforeSummon:
                    return BeforeSummon;
                case Category.Summon:
                    return Summon;
                case Category.None:
                    return None;
                case Category.MainMonsterSummon:
                    return MainMonsterSummon;
                case Category.BattleStart:
                    return BattleStart;
                case Category.BeforeAttackNormal:
                    return BeforeAttackNormal;
                case Category.BeforeAttackFinish:
                    return BeforeAttackFinish;
                case Category.Attack:
                    return Attack;
                case Category.DirectAttack:
                    return DirectAttack;
                case Category.MainMonsterAttack:
                    return MainMonsterAttack;
                case Category.CardSet:
                    return CardSet;
                case Category.TurnEnd:
                    return TurnEnd;
                case Category.Damage:
                    return Damage;
                case Category.FinishDamage:
                    return FinishDamage;
                case Category.CostDamage:
                    return CostDamage;
                case Category.BigDamage:
                    return BigDamage;
                case Category.AfterDamage:
                    return AfterDamage;
                case Category.AfterBigDamage:
                    return AfterBigDamage;
                case Category.Win:
                    return Win;
                case Category.Lose:
                    return Lose;
                case Category.Taunt:
                    return Taunt;
                case Category.Surprise:
                    return Surprise;
                case Category.Title:
                    return Title;
                case Category.Skill:
                    return Skill;
                case Category.Chat:
                    return Chat;
                case Category.CharaChange:
                    return CharaChange;
                case Category.SwitchToPartner:
                    return SwitchToPartner;
                case Category.BeforeMainSummon:
                    return BeforeMainSummon;
                case Category.RidingDuelStart:
                    return RidingDuelStart;
                case Category.CoinTossOfMagicTrap:
                    return CoinTossOfMagicTrap;
                case Category.CoinTossOfMonster:
                    return CoinTossOfMonster;
                case Category.BeforeDimensionDuel:
                    return BeforeDimensionDuel;
                case Category.DimensionDuelStart:
                    return DimensionDuelStart;
                case Category.Transformation:
                    return Transformation;
                case Category.ActionDuelStart:
                    return ActionDuelStart;
                case Category.ActionCard:
                    return ActionCard;
                case Category.BeforeMainReincarnationSummon:
                    return BeforeMainReincarnationSummon;
                case Category.MainMonsterReincarnationSummon:
                    return MainMonsterReincarnationSummon;
                case Category.RushDuelStart:
                    return RushDuelStart;
                case Category.RidingRushDuelStart:
                    return RidingRushDuelStart;

                default:
                    return null;

            }
        }

        public List<VoiceInfoEntry> GetEntryWithCard()
        {
            var returnValue = new List<VoiceInfoEntry>()
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
        public Dictionary<string, LineInfo> info;
    }

    public class SimpleVoiceData
    {
        public int category;
        public int subCategory;
        public bool isMe;
        public bool inHand;

        public static SimpleVoiceData GetCardEffectSubCategory(BinaryReader r, bool fromHand, int fallbackCode = 0)
        {
            var returnValue = new SimpleVoiceData
            {
                category = (int)Category.CardEffect,
                subCategory = (int)CardEffectSub.General
            };

            r.BaseStream.Seek(0, 0);
            var code = r.ReadInt32();
            if (code == 0)
                code = fallbackCode;
            var gps = r.ReadGPS();
            returnValue.isMe = gps.controller == 0;
            returnValue.inHand = (gps.location & (uint)CardLocation.Hand) > 0;

            var c = CardsManager.Get(code);

            if (GameCard.InPendulumZoneIf(gps, code) && fromHand)
                returnValue.subCategory = (int)CardEffectSub.PendulumScale;
            else if (GameCard.InPendulumZoneIf(gps, code))
                returnValue.subCategory = (int)CardEffectSub.PendulumEffect;
            else if (c.HasType(CardType.Monster))
                returnValue.subCategory = (int)CardEffectSub.MonsterEffect;

            if ((gps.location & (uint)CardLocation.MonsterZone) == 0)
            {
                if (c.HasType(CardType.Spell))
                {
                    returnValue.subCategory = (int)CardEffectSub.Magic;
                    if (c.HasType(CardType.QuickPlay))
                        returnValue.subCategory = (int)CardEffectSub.QuickPlayMagic;
                    if (c.HasType(CardType.Continuous))
                        returnValue.subCategory = (int)CardEffectSub.PermanentMagic;
                    if (c.HasType(CardType.Equip))
                        returnValue.subCategory = (int)CardEffectSub.EquipMagic;
                    if (c.HasType(CardType.Ritual))
                        returnValue.subCategory = (int)CardEffectSub.RitualMagic;
                    if (c.HasType(CardType.Field))
                        returnValue.subCategory = (int)CardEffectSub.FieldMagic;
                }
                if (c.HasType(CardType.Trap))
                {
                    returnValue.subCategory = (int)CardEffectSub.Trap;
                    if (c.HasType(CardType.Continuous))
                        returnValue.subCategory = (int)CardEffectSub.PermanentTrap;
                    if (c.HasType(CardType.Counter))
                        returnValue.subCategory = (int)CardEffectSub.CounterTrap;
                }
            }
            return returnValue;
        }
    }
}
