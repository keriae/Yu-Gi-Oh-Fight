using MDPro3.IO;
using MDPro3.Servant;
using MDPro3.UI;
using MDPro3.Utility;
using Mono.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace MDPro3.Duel.YGOSharp
{
    internal static class CardsManager
    {
        public static string nullName = string.Empty;
        public static string nullString = string.Empty;
        public static string nameInSearch = string.Empty;
        public static Dictionary<int, Card> _cards = new();
        public static Dictionary<int, Card> _cardsForRender = new();
        private static readonly CompareInfo SearchCompareInfo = CultureInfo.InvariantCulture.CompareInfo;
        private const CompareOptions SearchCompareOptions =
            CompareOptions.IgnoreCase | CompareOptions.IgnoreWidth | CompareOptions.IgnoreKanaType;

        public static List<string> setNameHead = new()
        {
            "带有"
        };

        public static List<string> setNameTail = new()
        {
            "、",
            "卡",
            "怪兽",
            "魔法",
            "陷阱",
            "通常",
            "效果怪兽",
            "融合",
            "仪式",
            "灵魂",
            "同盟",
            "二重",
            "调整",
            "同调",
            "衍生物",
            "速攻",
            "永续",
            "装备",
            "场地",
            "反击",
            "反转",
            "卡通",
            "超量",
            "灵摆",
            "连接",
        };

        internal static void Initialize()
        {
            nullName = InterString.Get("未知卡片");

            var preCards = _cards;
            var preRenderCards = _cardsForRender;

            var language = Language.GetConfig();
            var cardLanguage = Language.GetCardConfig();

            _cards = new();
            if(!TryLoadCardsForLanguage(_cards, language))
            {
                UnityEngine.Debug.LogError("Load CDB Error.");
                _cards = preCards;
            }

            if (language == cardLanguage)
            {
                _cardsForRender = _cards;
            }
            else
            {
                _cardsForRender = new();
                if (!TryLoadCardsForLanguage(_cardsForRender, cardLanguage))
                {
                    UnityEngine.Debug.LogError("Load CDB Error.");
                    _cardsForRender = preRenderCards;
                }
            }

            UpdateSetNames();
            PacksManager.Initialize();
        }

        internal static void UpdateSetNames()
        {
            foreach (var item in _cards)
            {
                Card card = item.Value;
                card.strSetName = StringHelper.GetSetName(card.Setcode);
            }
        }

        internal static Card GetCard(int id)
        {
            if (_cards.ContainsKey(id))
                return _cards[id].Clone();
            return null;
        }

        internal static Card GetRenderCard(int id)
        {
            if (_cardsForRender.ContainsKey(id))
                return _cardsForRender[id].Clone();
            return null;
        }

        internal static Card GetCardRaw(int id)
        {
            if (_cards.ContainsKey(id))
                return _cards[id];
            return null;
        }

        internal static Card Get(int id, bool noneIsZero = false)
        {
            var returnValue = new Card();
            if (id > 0)
            {
                for (int i = 0; i < 10; i++)
                {
                    returnValue = GetCard(id - i);
                    if (returnValue != null)
                    {
                        break;
                    }
                }

                if (returnValue == null)
                {
                    returnValue = new Card();
                    if (!noneIsZero)
                    {
                        returnValue.Id = id;
                        returnValue.Desc = id.ToString();
                    }
                }
            }

            return returnValue;
        }

        internal static List<Card> GetAllCards()
        {
            var returnValue = new List<Card>();
            foreach (var card in _cards)
                returnValue.Add(card.Value);
            return returnValue;
        }

        internal static List<int> GetAllCardCodes()
        {
            var returnValue = new List<int>();
            foreach (var card in _cards)
                returnValue.Add(card.Key);
            return returnValue;
        }

        internal static int GetAlias(int id)
        {
            var data = GetCardRaw(id);
            if(data == null)
                return 0;
            return data.Alias;
        }

        private static bool TryLoadCardsForLanguage(IDictionary<int, Card> targetCards, string language)
        {
            string databaseFullPath = ResolveCardsDatabasePath(language);
            if (!TryLoadCDB(databaseFullPath, targetCards, isPreCards: false))
                return false;

            string rushDatabaseFullPath = ResolveLocalizedRushDatabasePath(language);
            if (!string.IsNullOrEmpty(rushDatabaseFullPath))
                TryLoadCDB(rushDatabaseFullPath, targetCards, isPreCards: false);

            if (!Config.GetBool("Expansions", true))
                return true;

            if (Directory.Exists(Program.PATH_EXPANSIONS))
                foreach (var cdb in Directory.GetFiles(Program.PATH_EXPANSIONS, "*.cdb"))
                    TryLoadCDB(cdb, targetCards, isPreCards: false);

            var cdbs = FileManager.GetFilesByExtensions(new string[] { ".cdb" });
            foreach (var cdb in cdbs)
            {
                if (File.Exists(cdb))
                    TryLoadCDB(cdb, targetCards, false);
                else
                {
                    if (FileManager.TryExtractFileTo(cdb, Program.PATH_TEMP_FOLDER, out var zipName))
                    {
                        var fileName = Path.GetFileName(cdb);
                        var newName = Path.Combine(Program.PATH_TEMP_FOLDER, fileName);
                        bool fromPrereleasePack = zipName != null && Path.GetFileName(zipName)
                            .Equals("ygopro-super-pre.ypk", StringComparison.OrdinalIgnoreCase);
                        bool isPreCards = fromPrereleasePack && fileName.StartsWith("test-release", StringComparison.OrdinalIgnoreCase);
                        TryLoadCDB(newName, targetCards, isPreCards);
                    }
                }
            }

            return true;
        }

        private static string ResolveCardsDatabasePath(string language)
        {
            string databaseFullPath = Program.PATH_LOCALES + language + "/cards.cdb";
            if (!File.Exists(databaseFullPath))
                databaseFullPath = Program.PATH_LOCALES + Language.SimplifiedChinese + "/cards.cdb";
            return databaseFullPath;
        }

        private static string ResolveLocalizedRushDatabasePath(string language)
        {
            string databaseFullPath = Program.PATH_LOCALES + language + "/rush_cards.cdb";
            if (!File.Exists(databaseFullPath))
                return string.Empty;
            return databaseFullPath;
        }

        private static bool TryLoadCDB(
            string databaseFullPath,
            IDictionary<int, Card> targetCards,
            bool isPreCards)
        {
            try
            {
                using SqliteConnection connection = new("Data Source=" + databaseFullPath);
                connection.Open();

                using IDbCommand command =
                    new SqliteCommand("SELECT datas.*, texts.* FROM datas,texts WHERE datas.id=texts.id;", connection);
                using IDataReader reader = command.ExecuteReader();
                while (reader.Read())
                    LoadCard(reader, targetCards, isPreCards);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void LoadCard(IDataRecord reader, IDictionary<int, Card> targetCards, bool isPreCards = false)
        {
            Card card = new(reader) 
            {
                isPre = isPreCards
            };

            targetCards[card.Id] = card;
        }

        public static List<string> GetMiddleStrings(string str, string start, string end)
        {
            List<string> returnValue = new List<string>();
            var reg = new Regex("(?<=(" + start + "))[.\\s\\S]*?(?=(" + end + "))", RegexOptions.RightToLeft);
            while (reg.Match(str).Value != "")
            {
                string s = reg.Match(str).Value;
                returnValue.Add(s);
                str = str.Replace(start + s + end, "");
            }
            return returnValue;
        }

        private static List<string> GetSetNamesInDescription(string input)
        {
            var returnValue = new List<string>();
            foreach(string s in setNameHead)
            {
                List<string> setNames = GetMiddleStrings(input, s + "「", "」");
                for (int i = 0; i < setNames.Count; i++)
                {
                    if (returnValue.Contains(setNames[i]) == false)
                        returnValue.Add(setNames[i]);
                }
            }
            foreach (string s in setNameTail)
            {
                List<string> setNames = GetMiddleStrings(input, "「", "」" + s);
                for (int i = 0; i < setNames.Count; i++)
                {
                    if (returnValue.Contains(setNames[i]) == false)
                        returnValue.Add(setNames[i]);
                }
            }
            return returnValue;
        }

        private static bool MatchWithSearchOptions(string source, string keyword)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(keyword))
                return false;

            try
            {
                if (Regex.Replace(source, keyword, "miaowu", RegexOptions.IgnoreCase) != source)
                    return true;
            }
            catch (ArgumentException)
            {
                // Keep searching with a literal fallback when regex input is invalid.
            }

            return SearchCompareInfo.IndexOf(source, keyword, SearchCompareOptions) >= 0;
        }

        private static bool MatchCardId(int cardId, string keyword)
        {
            return !string.IsNullOrEmpty(keyword)
                && SearchCompareInfo.Compare(cardId.ToString(), keyword, SearchCompareOptions) == 0;
        }

        private static bool MatchCardSearch(Card card, string[] strings)
        {
            foreach (string s in strings)
            {
                if (s.StartsWith("@"))
                {
                    if (!MatchWithSearchOptions(card.strSetName, s.Substring(1, s.Length - 1)))
                        return false;
                }
                else if (
                        s != ""
                        && !MatchWithSearchOptions(card.Name, s)
                        && !MatchWithSearchOptions(card.Desc, s)
                        && !MatchWithSearchOptions(card.strSetName, s)
                        && !MatchCardId(card.Id, s)
                        )
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MatchCardFilters(Card card, List<long> filters, Banlist banlist, string pack)
        {
            if (filters == null || filters.Count == 0)
                return pack == string.Empty || card.packFullName == pack;

            bool pass = false;
            bool isRushDuel = card.IsRushDuelCard();

            //CardType
            if (filters[0] == 0)
                pass = true;
            if (!pass)
            {
                if (filters[0].HasType(CardType.Spell) && card.HasType(CardType.Spell))
                    pass = true;
                else if (filters[0].HasType(CardType.Trap) && card.HasType(CardType.Trap))
                    pass = true;
            }

            if (!pass && filters[0].HasFlag(card.Type))
            {
                if (isRushDuel 
                    && filters[0].HasType(CardType.Fusion) 
                    && !filters[0].HasType(CardType.Ritual)
                    && card.HasType(CardType.Ritual))
                {
                }
                else if (filters[0].HasType(CardType.Ritual))
                {
                    if (card.HasType(CardType.Monster))
                        pass = true;
                }
                else
                    pass = true;
            }

            if (!pass)
                return false;

            //Attribute
            pass = false;
            if (filters[1] == 0)
                pass = true;
            if (!pass && (card.HasType(CardType.Monster) && filters[1].HasFlag(card.Attribute)))
                pass = true;
            if (!pass)
                return false;

            //SpellType
            pass = false;
            if (filters[2] == 0)
                pass = true;
            if (!pass && card.HasAnyType(CardType.Spell, CardType.Trap))
            {
                if (filters[2].HasType(CardType.Spell))
                    if (card.Type == 2)
                        pass = true;
                if (!pass && filters[2].HasType(CardType.Field))
                    if (card.HasType(CardType.Field))
                        pass = true;
                if (!pass && filters[2].HasType(CardType.Equip))
                    if (card.HasType(CardType.Equip))
                        pass = true;
                if (!pass && filters[2].HasType(CardPro3Type.ContinuousSpell))
                    if (card.HasAllTypes(CardType.Spell, CardType.Continuous))
                        pass = true;
                if (!pass && filters[2].HasType(CardType.QuickPlay))
                    if (card.HasType(CardType.QuickPlay))
                        pass = true;
                if (!pass && filters[2].HasType(CardType.Ritual))
                    if (card.HasAllTypes(CardType.Spell, CardType.Ritual))
                        pass = true;
                if (!pass && filters[2].HasType(CardType.Trap))
                {
                    if (card.HasType(CardType.Trap)
                        && !card.HasType(CardType.Continuous)
                        && !card.HasType(CardType.Counter))
                        pass = true;
                }
                if (!pass && filters[2].HasType(CardPro3Type.ContinuousTrap))
                    if (card.HasAllTypes(CardType.Trap, CardType.Continuous))
                        pass = true;
                if (!pass && filters[2].HasType(CardType.Counter))
                    if (card.HasType(CardType.Counter))
                        pass = true;
            }
            if (!pass)
                return false;

            //Race
            pass = false;
            if (filters[3] == 0)
                pass = true;
            if (!pass && card.HasType(CardType.Monster) && filters[3].HasFlag(card.Race))
                pass = true;
            if (!pass)
                return false;

            //Ability
            pass = false;
            if (filters[4] == 0)
                pass = true;
            if (!pass && card.HasType(CardType.Monster) && filters[4].HasFlag(card.Type))
                pass = true;
            if (!pass && filters[4].HasType(CardPro3Type.NonEffect))
                if (card.HasType(CardType.Monster) && !card.HasType(CardType.Effect))
                    pass = true;
            if (!pass)
                return false;

            //Limit
            pass = false;
            if (filters[5] == 0)
                pass = true;
            if (!pass)
            {
                var permit = banlist.GetQuantity(card.Id);
                if ((filters[5] & 0x1) > 0 && permit == 0)
                    pass = true;
                else if ((filters[5] & 0x2) > 0 && permit == 1)
                    pass = true;
                else if ((filters[5] & 0x4) > 0 && permit == 2)
                    pass = true;
                else if ((filters[5] & 0x8) > 0 && permit == 3)
                    pass = true;
            }
            if (!pass)
                return false;

            //Pool
            pass = false;
            if (filters[6] == 0)
                pass = true;
            if (!pass)
            {
                if (filters[6].InPool(CardPool.DIY) && (card.isPre || card.IsRushDuelCard()))
                {}
                else if (filters[6].HasFlag(card.Ot))
                    pass = true;
                if (!pass && filters[6].InPool(CardPool.OCG_Only) && (card.Ot & 1) == 1 && (card.Ot & 2) == 0)
                    pass = true;
                if (!pass && filters[6].InPool(CardPool.TCG_Only) && (card.Ot & 1) == 0 && (card.Ot & 2) == 2)
                    pass = true;
                if (!pass && filters[6].InPool(CardPool.Non_Only) && (card.Ot & 3) == 3)
                    pass = true;
                if (!pass && filters[6].InPool(CardPool.PreRelease) && card.isPre)
                    pass = true;
                if (!pass && filters[6].InPool(CardPool.RushDuel) && card.IsRushDuelCard())
                    pass = true;
            }
            if (!pass)
                return false;

            //Effect
            pass = false;
            if (filters[7] == 0)
                pass = true;
            if (!pass && filters[7].HasFlag(card.Category))
                pass = true;
            if (!pass)
                return false;

            //Rarity
            pass = false;
            if (filters[8] == 0 || filters[8] == 7)
                pass = true;
            if (!pass)
                if (filters[8].HasFlag((long)CardRarity.GetRarity(card.Id)))
                    pass = true;
            if (!pass)
                return false;

            //Cutin
            pass = false;
            if (filters[9] == 0 || filters[9] == 3)
                pass = true;
            if (!pass)
            {
                bool found = false;
                foreach (var c in CutinViewer.cards)
                    if (c.Id == card.Id)
                    {
                        found = true;
                        break;
                    }
                if (filters[9].HasFlag(1) && found)
                    pass = true;
                if (filters[9].HasFlag(2) && !found)
                    pass = true;
            }
            if (!pass)
                return false;

            //Video Art
            pass = false;
            if (filters[10] == 0 || filters[10] == 3)
                pass = true;
            if (!pass)
            {
                if (filters[10] == 1 && ResourceManager.CardHasVideo(card.Id))
                    pass = true;
                if (filters[10] == 2 && !ResourceManager.CardHasVideo(card.Id))
                    pass = true;
            }
            if (!pass)
                return false;

            //Over Frame
            pass = false;
            if (filters[11] == 0 || filters[11] == 3)
                pass = true;
            if (!pass)
            {
                if (filters[11] == 1 && ResourceManager.CardHasOverFrame(card.Id))
                    pass = true;
                if (filters[11] == 2 && !ResourceManager.CardHasOverFrame(card.Id))
                    pass = true;
            }
            if (!pass)
                return false;

            //Link Markers
            pass = false;
            if (filters[12] == 0)
                pass = true;
            if (!pass)
                if (card.HasType(CardType.Link))
                {
                    pass = true;
                    for (int i = 0; i < 9; i++)
                    {
                        if ((filters[12] >> i & 1) > 0 && (card.LinkMarker >> i & 1) == 0)
                            pass = false;
                    }
                }
            if (!pass)
                return false;

            if (!JudgeInt(
                (int)filters[13],
                (int)filters[14],
                card.Level))
                return false;
            if (!JudgeInt(
                (int)filters[15],
                (int)filters[16],
                card.Attack))
                return false;
            if (!JudgeInt(
                (int)filters[17],
                (int)filters[18],
                card.Defense))
                return false;
            if (!JudgeInt(
                (int)filters[19],
                (int)filters[20],
                card.LScale))
                return false;
            if (!CheckGenesysCredit(
                (int)filters[21],
                (int)filters[22],
                card))
                return false;
            if (!JudgeInt(
                (int)filters[23],
                (int)filters[24],
                card.year))
                return false;

            return pack == string.Empty || card.packFullName == pack;
        }

        private static IEnumerable<Card> EnumerateCards(IEnumerable<int> sourceIds)
        {
            var visited = new HashSet<int>();
            foreach (var id in sourceIds)
            {
                if (!visited.Add(id))
                    continue;

                var card = GetCardRaw(id);
                if (card != null)
                    yield return card;
            }
        }

        private static List<Card> SearchInternal
        (
            string getName,
            List<long> filters,
            Banlist banlist,
            string pack,
            IEnumerable<Card> sourceCards
        )
        {
            var returnValue = new List<Card>();
            string[] strings = getName.Split(new[] { ' ', '\u3000' }, StringSplitOptions.None);
            nameInSearch = getName;
            foreach (var card in sourceCards)
            {
                if (card == null || card.HasType(CardType.Token))
                    continue;
                if (!MatchCardSearch(card, strings))
                    continue;
                if (!MatchCardFilters(card, filters, banlist, pack))
                    continue;
                returnValue.Add(card);
            }

            return returnValue;
        }

        internal static List<Card> Search
        (
            string getName,
            List<long> filters,
            Banlist banlist,
            string pack
        )
        {
            return SearchInternal(getName, filters, banlist, pack, _cards.Values);
        }

        internal static List<Card> Search
        (
            string getName,
            List<long> filters,
            Banlist banlist,
            string pack,
            IEnumerable<int> sourceIds
        )
        {
            return SearchInternal(getName, filters, banlist, pack, EnumerateCards(sourceIds));
        }

        internal static List<Card> Search(string getName, IEnumerable<int> sourceIds)
        {
            return SearchInternal(getName, null, null, string.Empty, EnumerateCards(sourceIds));
        }

        private static bool CheckGenesysCredit(int min, int max, Card card)
        {
            if(min == -233 && max == -233)
                return true;
            return JudgeInt(min, max, card.GetCredit());
        }

        internal static List<Card> AnnounceSearch(string announced, List<int> searchCodes)
        {
            List<Card> returnValue = new List<Card>();
            foreach(var item in _cards)
            {
                Card card = item.Value;
                if(announced == ""
                    || MatchWithSearchOptions(card.Name, announced)
                    || MatchWithSearchOptions(card.strSetName, announced)
                    || MatchCardId(card.Id, announced))
                {
                    if(searchCodes.Count == 0 || IsDeclarable(card, searchCodes))
                        returnValue.Add(card);
                }
            }
            nameInSearch = announced;
            returnValue.Sort(ComparisonOfCard());
            nameInSearch = "";
            return returnValue;
        }

        internal static List<Card> RelatedSearch(int code)
        {
            var generated = RelatedCardsManager.Search(code);
            if (generated != null)
                return generated;

            var cards = new List<Card>();
            var card = GetCard(code);
            if (card == null)
                return cards;
            card.strSetName = StringHelper.GetSetName(card.Setcode).Replace("【", "").Replace("】", "");

            List<string> names = new List<string>();
            names.Add(card.Name);
            List<string> setNames = GetSetNamesInDescription(card.Desc);
            if (!setNames.Contains(card.strSetName))
                setNames.Add(card.strSetName);

            var matches = GetMiddleStrings(card.Desc, "「", "」");
            foreach (var match in matches)
                if (!names.Contains(match.ToString()))
                    names.Add(match.ToString());
            foreach (string s in setNames)
                if (names.Contains(s))
                    names.Remove(s);

            names.Remove("");
            setNames.Remove("");

            string result = "";
            foreach(string s in setNames)
                result += s + Program.STRING_LINE_BREAK;

            result = "";
            foreach (string s in names)
                result += s + Program.STRING_LINE_BREAK;

            List<int> setCodes = new List<int>();
            foreach (var s in setNames)
                setCodes.Add(StringHelper.GetSetNameCode(s));

            foreach (var item in _cards)
            {
                if (card.Id != item.Value.Id && !item.Value.HasType(CardType.Token))
                {
                    bool pass = false;
                    foreach (var n in names)
                    {
                        if (
                            Regex.Replace(item.Value.Name, n, "miaowu", RegexOptions.IgnoreCase) != item.Value.Name
                            || Regex.Replace(item.Value.Desc, "「" + n + "」", "miaowu", RegexOptions.IgnoreCase) != item.Value.Desc
                            || Regex.Replace(item.Value.strSetName, n, "miaowu", RegexOptions.IgnoreCase) != item.Value.strSetName
                            )
                        {
                            pass = true;
                            break;
                        }
                    }
                    if (pass == false)
                    {
                        for (int i = 0; i < setNames.Count; i++)
                        {
                            if (
                                Regex.Replace(item.Value.Desc, "「" + setNames[i] + "」", "miaowu", RegexOptions.IgnoreCase) != item.Value.Desc
                                //|| Regex.Replace(item.Value.strSetName, setNames[i], "miaowu", RegexOptions.IgnoreCase) != item.Value.strSetName
                                || (setCodes[i] != 0)
                                    && (setCodes[i] - item.Value.Setcode == 0 || ~Math.Abs(setCodes[i] - item.Value.Setcode) == 0x999)
                                )
                            {
                                pass = true;
                                break;
                            }
                        }
                    }
                    if (pass)
                        cards.Add(item.Value);
                }
            }
            cards.Sort(ComparisonOfCard());
            return cards;
        }

        private static bool JudgeInt(int min, int max, int raw)
        {
            bool re = true;
            if (min == -233 && max == -233)
            {
                re = true;
            }

            if (min == -233 && max != -233)
            {
                re = max == raw;
            }

            if (min != -233 && max == -233)
            {
                re = min == raw;
            }

            if (min != -233 && max != -233)
            {
                re = min <= raw && raw <= max;
            }

            return re;
        }

        private static bool IsDeclarable(Card card, List<int> getsearchCode)
        {
            Stack<int> stack = new Stack<int>();
            for (int i = 0; i < getsearchCode.Count; i++)
            {
                switch (getsearchCode[i])
                {
                    case (int)SearchCode.OPCODE_ADD:
                        if (stack.Count >= 2)
                        {
                            int rhs = stack.Pop();
                            int lhs = stack.Pop();
                            stack.Push(lhs + rhs);
                        }

                        break;
                    case (int)SearchCode.OPCODE_SUB:
                        if (stack.Count >= 2)
                        {
                            int rhs = stack.Pop();
                            int lhs = stack.Pop();
                            stack.Push(lhs - rhs);
                        }

                        break;
                    case (int)SearchCode.OPCODE_MUL:
                        if (stack.Count >= 2)
                        {
                            int rhs = stack.Pop();
                            int lhs = stack.Pop();
                            stack.Push(lhs * rhs);
                        }

                        break;
                    case (int)SearchCode.OPCODE_DIV:
                        if (stack.Count >= 2)
                        {
                            int rhs = stack.Pop();
                            int lhs = stack.Pop();
                            stack.Push(lhs / rhs);
                        }

                        break;
                    case (int)SearchCode.OPCODE_AND:
                        if (stack.Count >= 2)
                        {
                            int rhs = stack.Pop();
                            int lhs = stack.Pop();
                            bool b0 = rhs != 0;
                            bool b1 = lhs != 0;
                            if (b0 && b1)
                            {
                                stack.Push(1);
                            }
                            else
                            {
                                stack.Push(0);
                            }
                        }

                        break;
                    case (int)SearchCode.OPCODE_OR:
                        if (stack.Count >= 2)
                        {
                            int rhs = stack.Pop();
                            int lhs = stack.Pop();
                            bool b0 = rhs != 0;
                            bool b1 = lhs != 0;
                            if (b0 || b1)
                            {
                                stack.Push(1);
                            }
                            else
                            {
                                stack.Push(0);
                            }
                        }

                        break;
                    case (int)SearchCode.OPCODE_NEG:
                        if (stack.Count >= 1)
                        {
                            int rhs = stack.Pop();
                            stack.Push(-rhs);
                        }

                        break;
                    case (int)SearchCode.OPCODE_NOT:
                        if (stack.Count >= 1)
                        {
                            int rhs = stack.Pop();
                            bool b0 = rhs != 0;
                            if (b0)
                            {
                                stack.Push(0);
                            }
                            else
                            {
                                stack.Push(1);
                            }
                        }

                        break;
                    case (int)SearchCode.OPCODE_ISCODE:
                        if (stack.Count >= 1)
                        {
                            int code = stack.Pop();
                            bool b0 = code == card.Id;
                            if (b0)
                            {
                                stack.Push(1);
                            }
                            else
                            {
                                stack.Push(0);
                            }
                        }

                        break;
                    case (int)SearchCode.OPCODE_ISSETCARD:
                        if (stack.Count >= 1)
                        {
                            if (IfSetCard(stack.Pop(), card.Setcode))
                            {
                                stack.Push(1);
                            }
                            else
                            {
                                stack.Push(0);
                            }
                        }

                        break;
                    case (int)SearchCode.OPCODE_ISTYPE:
                        if (stack.Count >= 1)
                        {
                            if ((stack.Pop() & card.Type) > 0)
                            {
                                stack.Push(1);
                            }
                            else
                            {
                                stack.Push(0);
                            }
                        }

                        break;
                    case (int)SearchCode.OPCODE_ISRACE:
                        if (stack.Count >= 1)
                        {
                            if ((stack.Pop() & card.Race) > 0)
                            {
                                stack.Push(1);
                            }
                            else
                            {
                                stack.Push(0);
                            }
                        }

                        break;
                    case (int)SearchCode.OPCODE_ISATTRIBUTE:
                        if (stack.Count >= 1)
                        {
                            if ((stack.Pop() & card.Attribute) > 0)
                            {
                                stack.Push(1);
                            }
                            else
                            {
                                stack.Push(0);
                            }
                        }

                        break;
                    default:
                        stack.Push(getsearchCode[i]);
                        break;
                }
            }

            if (stack.Count != 1 || stack.Pop() == 0)
                return false;
            return
                card.Id == (int)TwoNameCards.CARD_MARINE_DOLPHIN
                ||
                card.Id == (int)TwoNameCards.CARD_TWINKLE_MOSS
                ||
                (!(card.Alias != 0)
                 && ((card.Type & ((int)CardType.Monster + (int)CardType.Token)))
                 != ((int)CardType.Monster + (int)CardType.Token));
        }

        internal static bool IfSetCard(int setCodeToAnalyse, long setCodeFromCard)
        {
            bool res = false;
            int settype = setCodeToAnalyse & 0xfff;
            int setsubtype = setCodeToAnalyse & 0xf000;
            long sc = setCodeFromCard;
            while (sc != 0)
            {
                if ((sc & 0xfff) == settype && (sc & 0xf000 & setsubtype) == setsubtype)
                    res = true;
                sc = sc >> 16;
            }

            return res;
        }

        internal static Comparison<Card> GetSort(CardCollectionView.SortOrder sortOrder)
        {
            return sortOrder switch
            {
                CardCollectionView.SortOrder.ByType => ComparisonOfCard(),
                CardCollectionView.SortOrder.ByTypeReverse => ComparisonOfCardReverse(),
                CardCollectionView.SortOrder.ByLevelUp => ComparisonOfCard_LV_Up(),
                CardCollectionView.SortOrder.ByLevelDown => ComparisonOfCard_LV_Down(),
                CardCollectionView.SortOrder.ByAttackUp => ComparisonOfCard_ATK_Up(),
                CardCollectionView.SortOrder.ByAttackDown => ComparisonOfCard_ATK_Down(),
                CardCollectionView.SortOrder.ByDefenceUp => ComparisonOfCard_DEF_Up(),
                CardCollectionView.SortOrder.ByDefenceDown => ComparisonOfCard_DEF_Down(),
                CardCollectionView.SortOrder.ByRarityUp => ComparisonOfCard_Rarity_Up(),
                CardCollectionView.SortOrder.ByRarityDown => ComparisonOfCard_Rarity_Down(),
                CardCollectionView.SortOrder.ByGPUp => ComparisonOfCard_GP_Up(),
                CardCollectionView.SortOrder.ByGPDown => ComparisonOfCard_GP_Down(),
                _ => ComparisonOfCard(),
            };
        }

        internal static Comparison<Card> ComparisonOfCard()
        {
            return (left, right) =>
            {
                int a = 1;
                if (left.Name == nameInSearch && right.Name != nameInSearch)
                {
                    a = -1;
                }
                else if (right.Name == nameInSearch && left.Name != nameInSearch)
                {
                    a = 1;
                }
                else
                {
                    if ((left.Type & 7) < (right.Type & 7))
                    {
                        a = -1;
                    }
                    else if ((left.Type & 7) > (right.Type & 7))
                    {
                        a = 1;
                    }
                    else
                    {
                        //if ((left.Type >> 3) > (right.Type >> 3))
                        //{
                        //    a = 1;
                        //}
                        //else if ((left.Type >> 3) < (right.Type >> 3))
                        //{
                        //    a = -1;
                        //}
                        if ((left.Type & 0x58020f0) < (right.Type & 0x58020f0))
                        {
                            a = -1;
                        }
                        else if ((left.Type & 0x58020f0) > (right.Type & 0x58020f0))
                        {
                            a = 1;
                        }
                        else
                        {
                            if (left.Level > right.Level)
                            {
                                a = -1;
                            }
                            else if (left.Level < right.Level)
                            {
                                a = 1;
                            }
                            else
                            {
                                if (left.Attack > right.Attack)
                                {
                                    a = -1;
                                }
                                else if (left.Attack < right.Attack)
                                {
                                    a = 1;
                                }
                                else
                                {
                                    if (left.Attribute > right.Attribute)
                                    {
                                        a = 1;
                                    }
                                    else if (left.Attribute < right.Attribute)
                                    {
                                        a = -1;
                                    }
                                    else
                                    {
                                        if (left.Race > right.Race)
                                        {
                                            a = 1;
                                        }
                                        else if (left.Race < right.Race)
                                        {
                                            a = -1;
                                        }
                                        else
                                        {
                                            if (left.Category > right.Category)
                                            {
                                                a = 1;
                                            }
                                            else if (left.Category < right.Category)
                                            {
                                                a = -1;
                                            }
                                            else
                                            {
                                                if (left.Id > right.Id)
                                                {
                                                    a = 1;
                                                }
                                                else if (left.Id < right.Id)
                                                {
                                                    a = -1;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return a;
            };
        }

        internal static Comparison<Card> ComparisonOfCardReverse()
        {
            return (left, right) =>
            {
                int a = -1;
                if (left.Name == nameInSearch && right.Name != nameInSearch)
                {
                    a = -1;
                }
                else if (right.Name == nameInSearch && left.Name != nameInSearch)
                {
                    a = 1;
                }
                else
                {
                    if ((left.Type & 7) < (right.Type & 7))
                    {
                        a = -1;
                    }
                    else if ((left.Type & 7) > (right.Type & 7))
                    {
                        a = 1;
                    }
                    else
                    {
                        //if ((left.Type >> 3) > (right.Type >> 3))
                        //{
                        //    a = -1;
                        //}
                        //else if ((left.Type >> 3) < (right.Type >> 3))
                        //{
                        //    a = 1;
                        //}
                        if ((left.Type & 0x58020f0) < (right.Type & 0x58020f0))
                        {
                            a = 1;
                        }
                        else if ((left.Type & 0x58020f0) > (right.Type & 0x58020f0))
                        {
                            a = -1;
                        }
                        else
                        {
                            if (left.Level > right.Level)
                            {
                                a = -1;
                            }
                            else if (left.Level < right.Level)
                            {
                                a = 1;
                            }
                            else
                            {
                                if (left.Attack > right.Attack)
                                {
                                    a = -1;
                                }
                                else if (left.Attack < right.Attack)
                                {
                                    a = 1;
                                }
                                else
                                {
                                    if (left.Attribute > right.Attribute)
                                    {
                                        a = 1;
                                    }
                                    else if (left.Attribute < right.Attribute)
                                    {
                                        a = -1;
                                    }
                                    else
                                    {
                                        if (left.Race > right.Race)
                                        {
                                            a = 1;
                                        }
                                        else if (left.Race < right.Race)
                                        {
                                            a = -1;
                                        }
                                        else
                                        {
                                            if (left.Category > right.Category)
                                            {
                                                a = 1;
                                            }
                                            else if (left.Category < right.Category)
                                            {
                                                a = -1;
                                            }
                                            else
                                            {
                                                if (left.Id > right.Id)
                                                {
                                                    a = 1;
                                                }
                                                else if (left.Id < right.Id)
                                                {
                                                    a = -1;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return a;
            };
        }

        internal static Comparison<Card> ComparisonOfCard_ATK_Down()
        {
            return (left, right) =>
            {
                int a = -1;
                if (left.Name == nameInSearch && right.Name != nameInSearch)
                {
                    a = -1;
                }
                else if (right.Name == nameInSearch && left.Name != nameInSearch)
                {
                    a = 1;
                }
                else
                {
                    if ((left.Type & 7) < (right.Type & 7))
                    {
                        a = -1;
                    }
                    else if ((left.Type & 7) > (right.Type & 7))
                    {
                        a = 1;
                    }
                    else
                    {
                        if (left.Attack > right.Attack)
                        {
                            a = -1;
                        }
                        else if (left.Attack < right.Attack)
                        {
                            a = 1;
                        }
                        else
                        {
                            //if ((left.Type >> 3) > (right.Type >> 3))
                            //{
                            //    a = 1;
                            //}
                            //else if ((left.Type >> 3) < (right.Type >> 3))
                            //{
                            //    a = -1;
                            //}
                            if ((left.Type & 0x58020f0) < (right.Type & 0x58020f0))
                            {
                                a = -1;
                            }
                            else if ((left.Type & 0x58020f0) > (right.Type & 0x58020f0))
                            {
                                a = 1;
                            }
                            else
                            {
                                if (left.Level > right.Level)
                                {
                                    a = -1;
                                }
                                else if (left.Level < right.Level)
                                {
                                    a = 1;
                                }
                                else
                                {
                                    if (left.Attribute > right.Attribute)
                                    {
                                        a = 1;
                                    }
                                    else if (left.Attribute < right.Attribute)
                                    {
                                        a = -1;
                                    }
                                    else
                                    {
                                        if (left.Race > right.Race)
                                        {
                                            a = 1;
                                        }
                                        else if (left.Race < right.Race)
                                        {
                                            a = -1;
                                        }
                                        else
                                        {
                                            if (left.Category > right.Category)
                                            {
                                                a = 1;
                                            }
                                            else if (left.Category < right.Category)
                                            {
                                                a = -1;
                                            }
                                            else
                                            {
                                                if (left.Id > right.Id)
                                                {
                                                    a = 1;
                                                }
                                                else if (left.Id < right.Id)
                                                {
                                                    a = -1;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return a;
            };
        }

        internal static Comparison<Card> ComparisonOfCard_ATK_Up()
        {
            return (left, right) =>
            {
                int a = -1;
                if (left.Name == nameInSearch && right.Name != nameInSearch)
                {
                    a = -1;
                }
                else if (right.Name == nameInSearch && left.Name != nameInSearch)
                {
                    a = 1;
                }
                else
                {
                    if ((left.Type & 7) < (right.Type & 7))
                    {
                        a = -1;
                    }
                    else if ((left.Type & 7) > (right.Type & 7))
                    {
                        a = 1;
                    }
                    else
                    {
                        if (left.Attack > right.Attack)
                        {
                            a = 1;
                        }
                        else if (left.Attack < right.Attack)
                        {
                            a = -1;
                        }
                        else
                        {
                            //if ((left.Type >> 3) > (right.Type >> 3))
                            //{
                            //    a = 1;
                            //}
                            //else if ((left.Type >> 3) < (right.Type >> 3))
                            //{
                            //    a = -1;
                            //}
                            if ((left.Type & 0x58020f0) < (right.Type & 0x58020f0))
                            {
                                a = -1;
                            }
                            else if ((left.Type & 0x58020f0) > (right.Type & 0x58020f0))
                            {
                                a = 1;
                            }
                            else
                            {
                                if (left.Level > right.Level)
                                {
                                    a = 1;
                                }
                                else if (left.Level < right.Level)
                                {
                                    a = -1;
                                }
                                else
                                {
                                    if (left.Attribute > right.Attribute)
                                    {
                                        a = 1;
                                    }
                                    else if (left.Attribute < right.Attribute)
                                    {
                                        a = -1;
                                    }
                                    else
                                    {
                                        if (left.Race > right.Race)
                                        {
                                            a = 1;
                                        }
                                        else if (left.Race < right.Race)
                                        {
                                            a = -1;
                                        }
                                        else
                                        {
                                            if (left.Category > right.Category)
                                            {
                                                a = 1;
                                            }
                                            else if (left.Category < right.Category)
                                            {
                                                a = -1;
                                            }
                                            else
                                            {
                                                if (left.Id > right.Id)
                                                {
                                                    a = 1;
                                                }
                                                else if (left.Id < right.Id)
                                                {
                                                    a = -1;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return a;
            };
        }

        internal static Comparison<Card> ComparisonOfCard_DEF_Down()
        {
            return (left, right) =>
            {
                int a = -1;
                if (left.Name == nameInSearch && right.Name != nameInSearch)
                {
                    a = -1;
                }
                else if (right.Name == nameInSearch && left.Name != nameInSearch)
                {
                    a = 1;
                }
                else
                {
                    if ((left.Type & 7) < (right.Type & 7))
                    {
                        a = -1;
                    }
                    else if ((left.Type & 7) > (right.Type & 7))
                    {
                        a = 1;
                    }
                    else
                    {
                        if (left.Defense > right.Defense)
                        {
                            a = -1;
                        }
                        else if (left.Defense < right.Defense)
                        {
                            a = 1;
                        }
                        else
                        {
                            //if ((left.Type >> 3) > (right.Type >> 3))
                            //{
                            //    a = 1;
                            //}
                            //else if ((left.Type >> 3) < (right.Type >> 3))
                            //{
                            //    a = -1;
                            //}
                            if ((left.Type & 0x58020f0) < (right.Type & 0x58020f0))
                            {
                                a = -1;
                            }
                            else if ((left.Type & 0x58020f0) > (right.Type & 0x58020f0))
                            {
                                a = 1;
                            }
                            else
                            {
                                if (left.Level > right.Level)
                                {
                                    a = -1;
                                }
                                else if (left.Level < right.Level)
                                {
                                    a = 1;
                                }
                                else
                                {
                                    if (left.Attribute > right.Attribute)
                                    {
                                        a = 1;
                                    }
                                    else if (left.Attribute < right.Attribute)
                                    {
                                        a = -1;
                                    }
                                    else
                                    {
                                        if (left.Race > right.Race)
                                        {
                                            a = 1;
                                        }
                                        else if (left.Race < right.Race)
                                        {
                                            a = -1;
                                        }
                                        else
                                        {
                                            if (left.Category > right.Category)
                                            {
                                                a = 1;
                                            }
                                            else if (left.Category < right.Category)
                                            {
                                                a = -1;
                                            }
                                            else
                                            {
                                                if (left.Id > right.Id)
                                                {
                                                    a = 1;
                                                }
                                                else if (left.Id < right.Id)
                                                {
                                                    a = -1;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return a;
            };
        }

        internal static Comparison<Card> ComparisonOfCard_DEF_Up()
        {
            return (left, right) =>
            {
                int a = -1;
                if (left.Name == nameInSearch && right.Name != nameInSearch)
                {
                    a = -1;
                }
                else if (right.Name == nameInSearch && left.Name != nameInSearch)
                {
                    a = 1;
                }
                else
                {
                    if ((left.Type & 7) < (right.Type & 7))
                    {
                        a = -1;
                    }
                    else if ((left.Type & 7) > (right.Type & 7))
                    {
                        a = 1;
                    }
                    else
                    {
                        if (left.Defense > right.Defense)
                        {
                            a = 1;
                        }
                        else if (left.Defense < right.Defense)
                        {
                            a = -1;
                        }
                        else
                        {
                            //if ((left.Type >> 3) > (right.Type >> 3))
                            //{
                            //    a = 1;
                            //}
                            //else if ((left.Type >> 3) < (right.Type >> 3))
                            //{
                            //    a = -1;
                            //}
                            if ((left.Type & 0x58020f0) < (right.Type & 0x58020f0))
                            {
                                a = -1;
                            }
                            else if ((left.Type & 0x58020f0) > (right.Type & 0x58020f0))
                            {
                                a = 1;
                            }
                            else
                            {
                                if (left.Level > right.Level)
                                {
                                    a = 1;
                                }
                                else if (left.Level < right.Level)
                                {
                                    a = -1;
                                }
                                else
                                {
                                    if (left.Attribute > right.Attribute)
                                    {
                                        a = 1;
                                    }
                                    else if (left.Attribute < right.Attribute)
                                    {
                                        a = -1;
                                    }
                                    else
                                    {
                                        if (left.Race > right.Race)
                                        {
                                            a = 1;
                                        }
                                        else if (left.Race < right.Race)
                                        {
                                            a = -1;
                                        }
                                        else
                                        {
                                            if (left.Category > right.Category)
                                            {
                                                a = 1;
                                            }
                                            else if (left.Category < right.Category)
                                            {
                                                a = -1;
                                            }
                                            else
                                            {
                                                if (left.Id > right.Id)
                                                {
                                                    a = 1;
                                                }
                                                else if (left.Id < right.Id)
                                                {
                                                    a = -1;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return a;
            };
        }

        internal static Comparison<Card> ComparisonOfCard_LV_Down()
        {
            return (left, right) =>
            {
                int a = -1;
                if (left.Name == nameInSearch && right.Name != nameInSearch)
                {
                    a = -1;
                }
                else if (right.Name == nameInSearch && left.Name != nameInSearch)
                {
                    a = 1;
                }
                else
                {
                    if ((left.Type & 7) < (right.Type & 7))
                    {
                        a = -1;
                    }
                    else if ((left.Type & 7) > (right.Type & 7))
                    {
                        a = 1;
                    }
                    else
                    {
                        if (left.Level > right.Level)
                        {
                            a = -1;
                        }
                        else if (left.Level < right.Level)
                        {
                            a = 1;
                        }
                        else
                        {
                            //if ((left.Type >> 3) > (right.Type >> 3))
                            //{
                            //    a = 1;
                            //}
                            //else if ((left.Type >> 3) < (right.Type >> 3))
                            //{
                            //    a = -1;
                            //}
                            if ((left.Type & 0x58020f0) < (right.Type & 0x58020f0))
                            {
                                a = -1;
                            }
                            else if ((left.Type & 0x58020f0) > (right.Type & 0x58020f0))
                            {
                                a = 1;
                            }
                            else
                            {
                                if (left.Attack > right.Attack)
                                {
                                    a = -1;
                                }
                                else if (left.Attack < right.Attack)
                                {
                                    a = 1;
                                }
                                else
                                {
                                    if (left.Attribute > right.Attribute)
                                    {
                                        a = 1;
                                    }
                                    else if (left.Attribute < right.Attribute)
                                    {
                                        a = -1;
                                    }
                                    else
                                    {
                                        if (left.Race > right.Race)
                                        {
                                            a = 1;
                                        }
                                        else if (left.Race < right.Race)
                                        {
                                            a = -1;
                                        }
                                        else
                                        {
                                            if (left.Category > right.Category)
                                            {
                                                a = 1;
                                            }
                                            else if (left.Category < right.Category)
                                            {
                                                a = -1;
                                            }
                                            else
                                            {
                                                if (left.Id > right.Id)
                                                {
                                                    a = 1;
                                                }
                                                else if (left.Id < right.Id)
                                                {
                                                    a = -1;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return a;
            };
        }

        internal static Comparison<Card> ComparisonOfCard_LV_Up()
        {
            return (left, right) =>
            {
                int a = -1;
                if (left.Name == nameInSearch && right.Name != nameInSearch)
                {
                    a = -1;
                }
                else if (right.Name == nameInSearch && left.Name != nameInSearch)
                {
                    a = 1;
                }
                else
                {
                    if ((left.Type & 7) < (right.Type & 7))
                    {
                        a = -1;
                    }
                    else if ((left.Type & 7) > (right.Type & 7))
                    {
                        a = 1;
                    }
                    else
                    {
                        if (left.Level > right.Level)
                        {
                            a = 1;
                        }
                        else if (left.Level < right.Level)
                        {
                            a = -1;
                        }
                        else
                        {
                            //if ((left.Type >> 3) > (right.Type >> 3))
                            //{
                            //    a = 1;
                            //}
                            //else if ((left.Type >> 3) < (right.Type >> 3))
                            //{
                            //    a = -1;
                            //}
                            if ((left.Type & 0x58020f0) < (right.Type & 0x58020f0))
                            {
                                a = -1;
                            }
                            else if ((left.Type & 0x58020f0) > (right.Type & 0x58020f0))
                            {
                                a = 1;
                            }
                            else
                            {
                                if (left.Attack > right.Attack)
                                {
                                    a = 1;
                                }
                                else if (left.Attack < right.Attack)
                                {
                                    a = -1;
                                }
                                else
                                {
                                    if (left.Attribute > right.Attribute)
                                    {
                                        a = 1;
                                    }
                                    else if (left.Attribute < right.Attribute)
                                    {
                                        a = -1;
                                    }
                                    else
                                    {
                                        if (left.Race > right.Race)
                                        {
                                            a = 1;
                                        }
                                        else if (left.Race < right.Race)
                                        {
                                            a = -1;
                                        }
                                        else
                                        {
                                            if (left.Category > right.Category)
                                            {
                                                a = 1;
                                            }
                                            else if (left.Category < right.Category)
                                            {
                                                a = -1;
                                            }
                                            else
                                            {
                                                if (left.Id > right.Id)
                                                {
                                                    a = 1;
                                                }
                                                else if (left.Id < right.Id)
                                                {
                                                    a = -1;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return a;
            };
        }

        internal static Comparison<Card> ComparisonOfCard_Rarity_Up()
        {
            return (left, right) =>
            {
                int a = -1;
                if (left.Name == nameInSearch && right.Name != nameInSearch)
                {
                    a = -1;
                }
                else if (right.Name == nameInSearch && left.Name != nameInSearch)
                {
                    a = 1;
                }
                else
                {
                    var rarity = CardRarity.GetRarity(left.Id);
                    if ((int)CardRarity.GetRarity(left.Id) > (int)CardRarity.GetRarity(right.Id))
                    {
                        a = 1;
                    }
                    else if ((int)CardRarity.GetRarity(left.Id) < (int)CardRarity.GetRarity(right.Id))
                    {
                        a = -1;
                    }
                    else
                    {
                        if ((left.Type & 7) < (right.Type & 7))
                        {
                            a = -1;
                        }
                        else if ((left.Type & 7) > (right.Type & 7))
                        {
                            a = 1;
                        }
                        else
                        {
                            //if ((left.Type >> 3) > (right.Type >> 3))
                            //{
                            //    a = 1;
                            //}
                            //else if ((left.Type >> 3) < (right.Type >> 3))
                            //{
                            //    a = -1;
                            //}
                            if ((left.Type & 0x58020f0) < (right.Type & 0x58020f0))
                            {
                                a = -1;
                            }
                            else if ((left.Type & 0x58020f0) > (right.Type & 0x58020f0))
                            {
                                a = 1;
                            }
                            else
                            {
                                if (left.Level > right.Level)
                                {
                                    a = -1;
                                }
                                else if (left.Level < right.Level)
                                {
                                    a = 1;
                                }
                                else
                                {
                                    if (left.Attack > right.Attack)
                                    {
                                        a = -1;
                                    }
                                    else if (left.Attack < right.Attack)
                                    {
                                        a = 1;
                                    }
                                    else
                                    {
                                        if (left.Attribute > right.Attribute)
                                        {
                                            a = 1;
                                        }
                                        else if (left.Attribute < right.Attribute)
                                        {
                                            a = -1;
                                        }
                                        else
                                        {
                                            if (left.Race > right.Race)
                                            {
                                                a = 1;
                                            }
                                            else if (left.Race < right.Race)
                                            {
                                                a = -1;
                                            }
                                            else
                                            {
                                                if (left.Category > right.Category)
                                                {
                                                    a = 1;
                                                }
                                                else if (left.Category < right.Category)
                                                {
                                                    a = -1;
                                                }
                                                else
                                                {
                                                    if (left.Id > right.Id)
                                                    {
                                                        a = 1;
                                                    }
                                                    else if (left.Id < right.Id)
                                                    {
                                                        a = -1;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return a;
            };
        }

        internal static Comparison<Card> ComparisonOfCard_Rarity_Down()
        {
            return (left, right) =>
            {
                int a = -1;
                if (left.Name == nameInSearch && right.Name != nameInSearch)
                {
                    a = -1;
                }
                else if (right.Name == nameInSearch && left.Name != nameInSearch)
                {
                    a = 1;
                }
                else
                {
                    var rarity = CardRarity.GetRarity(left.Id);
                    if ((int)CardRarity.GetRarity(left.Id) > (int)CardRarity.GetRarity(right.Id))
                    {
                        a = -1;
                    }
                    else if ((int)CardRarity.GetRarity(left.Id) < (int)CardRarity.GetRarity(right.Id))
                    {
                        a = 1;
                    }
                    else
                    {
                        if ((left.Type & 7) < (right.Type & 7))
                        {
                            a = -1;
                        }
                        else if ((left.Type & 7) > (right.Type & 7))
                        {
                            a = 1;
                        }
                        else
                        {
                            //if ((left.Type >> 3) > (right.Type >> 3))
                            //{
                            //    a = 1;
                            //}
                            //else if ((left.Type >> 3) < (right.Type >> 3))
                            //{
                            //    a = -1;
                            //}
                            if ((left.Type & 0x58020f0) < (right.Type & 0x58020f0))
                            {
                                a = -1;
                            }
                            else if ((left.Type & 0x58020f0) > (right.Type & 0x58020f0))
                            {
                                a = 1;
                            }
                            else
                            {
                                if (left.Level > right.Level)
                                {
                                    a = -1;
                                }
                                else if (left.Level < right.Level)
                                {
                                    a = 1;
                                }
                                else
                                {
                                    if (left.Attack > right.Attack)
                                    {
                                        a = -1;
                                    }
                                    else if (left.Attack < right.Attack)
                                    {
                                        a = 1;
                                    }
                                    else
                                    {
                                        if (left.Attribute > right.Attribute)
                                        {
                                            a = 1;
                                        }
                                        else if (left.Attribute < right.Attribute)
                                        {
                                            a = -1;
                                        }
                                        else
                                        {
                                            if (left.Race > right.Race)
                                            {
                                                a = 1;
                                            }
                                            else if (left.Race < right.Race)
                                            {
                                                a = -1;
                                            }
                                            else
                                            {
                                                if (left.Category > right.Category)
                                                {
                                                    a = 1;
                                                }
                                                else if (left.Category < right.Category)
                                                {
                                                    a = -1;
                                                }
                                                else
                                                {
                                                    if (left.Id > right.Id)
                                                    {
                                                        a = 1;
                                                    }
                                                    else if (left.Id < right.Id)
                                                    {
                                                        a = -1;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return a;
            };
        }

        internal static Comparison<Card> ComparisonOfCard_GP_Up()
        {
            return (left, right) =>
            {
                int a = 1;
                if(left.GetCredit() < right.GetCredit())
                {
                    a = -1;
                }
                else if (left.GetCredit() > right.GetCredit())
                {
                    a = 1;
                }
                else
                {
                    if (left.Name == nameInSearch && right.Name != nameInSearch)
                    {
                        a = -1;
                    }
                    else if (right.Name == nameInSearch && left.Name != nameInSearch)
                    {
                        a = 1;
                    }
                    else
                    {
                        if ((left.Type & 7) < (right.Type & 7))
                        {
                            a = -1;
                        }
                        else if ((left.Type & 7) > (right.Type & 7))
                        {
                            a = 1;
                        }
                        else
                        {
                            //if ((left.Type >> 3) > (right.Type >> 3))
                            //{
                            //    a = 1;
                            //}
                            //else if ((left.Type >> 3) < (right.Type >> 3))
                            //{
                            //    a = -1;
                            //}
                            if ((left.Type & 0x58020f0) < (right.Type & 0x58020f0))
                            {
                                a = -1;
                            }
                            else if ((left.Type & 0x58020f0) > (right.Type & 0x58020f0))
                            {
                                a = 1;
                            }
                            else
                            {
                                if (left.Level > right.Level)
                                {
                                    a = -1;
                                }
                                else if (left.Level < right.Level)
                                {
                                    a = 1;
                                }
                                else
                                {
                                    if (left.Attack > right.Attack)
                                    {
                                        a = -1;
                                    }
                                    else if (left.Attack < right.Attack)
                                    {
                                        a = 1;
                                    }
                                    else
                                    {
                                        if (left.Attribute > right.Attribute)
                                        {
                                            a = 1;
                                        }
                                        else if (left.Attribute < right.Attribute)
                                        {
                                            a = -1;
                                        }
                                        else
                                        {
                                            if (left.Race > right.Race)
                                            {
                                                a = 1;
                                            }
                                            else if (left.Race < right.Race)
                                            {
                                                a = -1;
                                            }
                                            else
                                            {
                                                if (left.Category > right.Category)
                                                {
                                                    a = 1;
                                                }
                                                else if (left.Category < right.Category)
                                                {
                                                    a = -1;
                                                }
                                                else
                                                {
                                                    if (left.Id > right.Id)
                                                    {
                                                        a = 1;
                                                    }
                                                    else if (left.Id < right.Id)
                                                    {
                                                        a = -1;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return a;
            };
        }

        internal static Comparison<Card> ComparisonOfCard_GP_Down()
        {
            return (left, right) =>
            {
                int a = 1;
                if (left.GetCredit() < right.GetCredit())
                {
                    a = 1;
                }
                else if (left.GetCredit() > right.GetCredit())
                {
                    a = -1;
                }
                else
                {
                    if (left.Name == nameInSearch && right.Name != nameInSearch)
                    {
                        a = -1;
                    }
                    else if (right.Name == nameInSearch && left.Name != nameInSearch)
                    {
                        a = 1;
                    }
                    else
                    {
                        if ((left.Type & 7) < (right.Type & 7))
                        {
                            a = -1;
                        }
                        else if ((left.Type & 7) > (right.Type & 7))
                        {
                            a = 1;
                        }
                        else
                        {
                            //if ((left.Type >> 3) > (right.Type >> 3))
                            //{
                            //    a = 1;
                            //}
                            //else if ((left.Type >> 3) < (right.Type >> 3))
                            //{
                            //    a = -1;
                            //}
                            if ((left.Type & 0x58020f0) < (right.Type & 0x58020f0))
                            {
                                a = -1;
                            }
                            else if ((left.Type & 0x58020f0) > (right.Type & 0x58020f0))
                            {
                                a = 1;
                            }
                            else
                            {
                                if (left.Level > right.Level)
                                {
                                    a = -1;
                                }
                                else if (left.Level < right.Level)
                                {
                                    a = 1;
                                }
                                else
                                {
                                    if (left.Attack > right.Attack)
                                    {
                                        a = -1;
                                    }
                                    else if (left.Attack < right.Attack)
                                    {
                                        a = 1;
                                    }
                                    else
                                    {
                                        if (left.Attribute > right.Attribute)
                                        {
                                            a = 1;
                                        }
                                        else if (left.Attribute < right.Attribute)
                                        {
                                            a = -1;
                                        }
                                        else
                                        {
                                            if (left.Race > right.Race)
                                            {
                                                a = 1;
                                            }
                                            else if (left.Race < right.Race)
                                            {
                                                a = -1;
                                            }
                                            else
                                            {
                                                if (left.Category > right.Category)
                                                {
                                                    a = 1;
                                                }
                                                else if (left.Category < right.Category)
                                                {
                                                    a = -1;
                                                }
                                                else
                                                {
                                                    if (left.Id > right.Id)
                                                    {
                                                        a = 1;
                                                    }
                                                    else if (left.Id < right.Id)
                                                    {
                                                        a = -1;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return a;
            };
        }

    }

    internal static class PacksManager
    {
        public class PackName
        {
            public string fullName;
            public string shortName;
            public int year;
            public int month;
            public int day;
        }

        public static List<PackName> packs = new();
        private static readonly Dictionary<string, string> pacDic = new();
        private const string PATH = "Data/pack";

        internal static void Initialize()
        {
            if (Directory.Exists(PATH))
            {
                var fileInfos = new DirectoryInfo(PATH).GetFiles();
                foreach (var file in fileInfos)
                    if (file.Name.ToLower().EndsWith(".db"))
                        LoadDataBase(PATH + Program.STRING_SLASH + file.Name);
                InitializeSec();
            }
        }

        internal static void LoadDataBase(string filePath)
        {
            using SqliteConnection connection = new("Data Source=" + filePath);
            connection.Open();
            using IDbCommand command = new SqliteCommand("SELECT pack.* FROM pack;", connection);
            using IDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                try
                {
                    int Id = (int)reader.GetInt64(0);
                    Card c = CardsManager.GetCardRaw(Id);
                    if (c != null)
                    {
                        string temp = reader.GetString(1);
                        c.packFullName = reader.GetString(2);
                        string[] mats = temp.Split('-');
                        if (mats.Length > 1)
                            c.packShortName = mats[0];
                        else
                            c.packShortName = c.packFullName.Length > 10 ? c.packFullName.Substring(0, 10) + "..." : c.packFullName;
                        c.reality = reader.GetString(3);
                        temp = reader.GetString(4);
                        mats = temp.Split('/');
                        if (mats.Length == 3)
                        {
                            c.month = int.Parse(mats[0]);
                            c.day = int.Parse(mats[1]);
                            c.year = int.Parse(mats[2]);
                        }
                        mats = temp.Split('-');
                        if (mats.Length == 3)
                        {
                            c.year = int.Parse(mats[0]);
                            c.month = int.Parse(mats[1]);
                            c.day = int.Parse(mats[2]);
                        }
                        c.packFullName = c.year + "-" + c.month.ToString("D2") + "-" + c.day.ToString("D2") + " " + c.packFullName;

                        if (!pacDic.ContainsKey(c.packFullName))
                        {
                            pacDic.Add(c.packFullName, c.packShortName);
                            PackName p = new PackName();
                            p.day = c.day;
                            p.year = c.year;
                            p.month = c.month;
                            p.fullName = c.packFullName;
                            p.shortName = c.packShortName;
                            packs.Add(p);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        internal static void InitializeSec()
        {
            packs.Sort((left, right) =>
            {
                if (left.year > right.year)
                {
                    return -1;
                }

                if (left.year < right.year)
                {
                    return 1;
                }

                if (left.month > right.month)
                {
                    return -1;
                }

                if (left.month < right.month)
                {
                    return 1;
                }

                if (left.day > right.day)
                {
                    return -1;
                }

                if (left.day < right.day)
                {
                    return 1;
                }

                return 1;
            });
        }

    }

}
