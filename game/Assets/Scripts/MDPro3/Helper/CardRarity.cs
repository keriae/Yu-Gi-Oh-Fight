using MDPro3.Duel.YGOSharp;
using MDPro3.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MDPro3
{
    public static class CardRarity
    {
        public enum Rarity
        {
            Unknown = 0,
            Normal = 1,
            Shine = 2,
            Royal = 4,
            Gold = 8,
            Millennium = 16,
        }

        private const string PATH = Program.PATH_DATA + "Rarity.json";
        private static RarityCards cards;
        private static bool initialized = false;
        private static string currentBookList = string.Empty;
        private static List<string> listNames;
        private static bool listNamesDirty = true;

        public static string GetFilePath()
        {
            return PATH;
        }

        // 供 OnlineService 开机同步使用：以服务器返回的 Rarity.json 文本覆盖本地并刷新内存缓存
        public static void ApplyServerContent(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            File.WriteAllText(PATH, json);
            try
            {
                cards = JsonConvert.DeserializeObject<RarityCards>(json);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            if (cards == null)
                cards = new RarityCards();
            EnsureBookmarkData();
            initialized = true;
            listNamesDirty = true;
        }

        private static void Initialize()
        {
            if(initialized) return;

            if (!File.Exists(PATH))
            {
                cards = new RarityCards();
                initialized = true;
                return;
            }

            var json = File.ReadAllText(PATH);
            try
            {
                cards = JsonConvert.DeserializeObject<RarityCards>(json);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                cards = new RarityCards();
            }
            EnsureBookmarkData();
            initialized = true;
        }

        private static void EnsureBookmarkData()
        {
            cards ??= new RarityCards();
            cards.ShineCards ??= new List<int>();
            cards.RoyalCards ??= new List<int>();
            cards.GoldCards ??= new List<int>();
            cards.MillenniumCards ??= new List<int>();
            cards.BookCards ??= new List<int>();
            cards.BookmarkLists ??= new List<BookmarkListData>();

            var normalizedBookCards = new List<int>();
            foreach (var card in cards.BookCards)
                if (!normalizedBookCards.Contains(card))
                    normalizedBookCards.Add(card);
            cards.BookCards = normalizedBookCards;

            var normalizedLists = new List<BookmarkListData>();
            foreach (var source in cards.BookmarkLists)
            {
                if (source == null)
                    continue;

                var name = NormalizeBookmarkListName(source.Name);
                if (name == string.Empty)
                    continue;

                BookmarkListData target = null;
                foreach (var existing in normalizedLists)
                {
                    if (string.Equals(existing.Name, name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        target = existing;
                        break;
                    }
                }

                if (target == null)
                {
                    target = new BookmarkListData(name);
                    normalizedLists.Add(target);
                }

                source.Cards ??= new List<int>();
                foreach (var card in source.Cards)
                {
                    if (!target.Cards.Contains(card))
                        target.Cards.Add(card);
                }
            }

            cards.BookmarkLists = normalizedLists;
        }

        public static void SetRarity(int card, Rarity rarity)
        {
            Initialize();
            cards.ShineCards.Remove(card);
            cards.RoyalCards.Remove(card);
            cards.GoldCards.Remove(card);
            cards.MillenniumCards.Remove(card);
            switch(rarity)
            {
                case Rarity.Shine:
                    cards.ShineCards.Add(card);
                    break;
                case Rarity.Royal:
                    cards.RoyalCards.Add(card);
                    break;
                case Rarity.Gold:
                    cards.GoldCards.Add(card);
                    break;
                case Rarity.Millennium:
                    cards.MillenniumCards.Add(card);
                    break;
            }
            Save();
        }

        public static Rarity GetRarity(int card)
        {
            Initialize();

            if (cards.ShineCards.Contains(card))
                return Rarity.Shine;
            if (cards.RoyalCards.Contains(card))
                return Rarity.Royal;
            if (cards.GoldCards.Contains(card))
                return Rarity.Gold;
            if (cards.MillenniumCards.Contains(card))
                return Rarity.Millennium;
            return Rarity.Normal;
        }

        public static void BookmarkCard(int card)
        {
            Initialize();
            if(currentBookList == string.Empty)
            {
                if (!cards.BookCards.Contains(card))
                    cards.BookCards.Add(card);
            }
            else
            {
                var list = GetBookmarkList(currentBookList);
                if (list == null)
                    return;
                if(!list.Cards.Contains(card))
                    list.Cards.Add(card);
            }
            Save();
        }

        public static int ToggleCardTo(int card, string listName)
        {
            Initialize();
            int result;
            if (listName == string.Empty)
            {
                if (cards.BookCards.Contains(card))
                {
                    cards.BookCards.Remove(card);
                    result = 2;
                }
                else
                {
                    cards.BookCards.Add(card);
                    result = 1;
                }
            }
            else
            {
                var list = GetBookmarkList(listName);
                if (list == null)
                    result = 0;
                else if (list.Cards.Contains(card))
                {
                    list.Cards.Remove(card);
                    result = 2;
                }
                else
                {
                    list.Cards.Add(card);
                    result = 1;
                }
            }
            Save();
            return result;
        }

        public static void UnbookmarkCard(int card)
        {
            Initialize();

            if(currentBookList == string.Empty)
            {
                cards.BookCards.Remove(card);
            }
            else
            {
                var list = GetBookmarkList(currentBookList);
                if (list == null)
                    return;
                list.Cards.Remove(card);
            }
            Save();
        }

        public static bool CardBookmarked(int card)
        {
            Initialize();
            if(currentBookList == string.Empty)
                return cards.BookCards.Contains(card);
            else
            {
                var list = GetBookmarkList(currentBookList);
                if (list == null) 
                    return false;
                return list.Cards.Contains(card);
            }
        }

        public static bool BookmarkListExists(string listName)
        {
            Initialize();
            return GetBookmarkList(listName) != null;
        }

        public static bool CardInBookmarkList(int card, string listName)
        {
            Initialize();

            if (listName == string.Empty)
                return cards.BookCards.Contains(card);
            else
            {
                var list = GetBookmarkList(listName);
                if (list == null)
                    return false;
                return list.Cards.Contains(card);
            }
        }

        public static List<string> GetBookmarkListNames()
        {
            Initialize();

            if(listNames == null || listNamesDirty)
            {
                listNames = new List<string>();
                foreach (var list in cards.BookmarkLists)
                    listNames.Add(list.Name);
                listNamesDirty = false;
            }

            return listNames;
        }

        public static bool AddBookmarkList(string listName)
        {
            Initialize();
            var normalized = NormalizeBookmarkListName(listName);
            if (normalized == string.Empty || GetBookmarkList(normalized) != null)
                return false;
            cards.BookmarkLists.Add(new BookmarkListData(normalized));
            listNamesDirty = true;
            Save();
            return true;
        }

        public static bool RenameBookmarkList(string oldName, string newName)
        {
            Initialize();
            var list = GetBookmarkList(oldName);
            var normalized = NormalizeBookmarkListName(newName);
            if (list == null || normalized == string.Empty)
                return false;
            var duplicate = GetBookmarkList(normalized);
            if (duplicate != null && duplicate != list)
                return false;
            list.Name = normalized;
            listNamesDirty = true;
            Save();
            return true;
        }

        public static bool DeleteBookmarkList(string listName)
        {
            Initialize();
            var list = GetBookmarkList(listName);
            if (list == null)
                return false;
            cards.BookmarkLists.Remove(list);
            if(currentBookList == listName)
                currentBookList = string.Empty;
            listNamesDirty = true;
            Save();
            return true;
        }

        private static void BookSort(List<int> targetCards)
        {
            Initialize();
            List<Card> cs = new();
            foreach (var code in targetCards)
            {
                var card = CardsManager.GetCardRaw(code);
                if(card != null)
                    cs.Add(card);
            }
            cs.Sort(CardsManager.GetSort(CardCollectionView._SortOrder));
            targetCards.Clear();
            foreach (var card in cs)
                targetCards.Add(card.Id);
        }

        public static List<int> GetBookCards()
        {
            return GetBookCards(currentBookList);
        }

        public static List<int> GetBookCards(string listName)
        {
            Initialize();
            var normalized = NormalizeBookmarkListName(listName);
            if (normalized == string.Empty)
            {
                BookSort(cards.BookCards);
                return cards.BookCards;
            }

            var list = GetBookmarkList(normalized);
            if (list == null)
                return new List<int>();
            BookSort(list.Cards);
            return list.Cards;
        }

        public static void SetCurrentBookListName(string listName)
        {
            Initialize();

            if (listName == string.Empty || GetBookmarkListNames().Contains(listName))
                currentBookList = listName;
        }

        public static string GetCurrentBookListName()
        {
            return currentBookList;
        }

        public static void Save()
        {
            Initialize();
            File.WriteAllText(PATH, JsonConvert.SerializeObject(cards, Formatting.Indented));
        }

        private static string NormalizeBookmarkListName(string listName)
        {
            return listName == null ? string.Empty : listName.Trim();
        }

        private static BookmarkListData GetBookmarkList(string listName)
        {
            var normalized = NormalizeBookmarkListName(listName);
            if (normalized == string.Empty)
                return null;

            foreach (var list in cards.BookmarkLists)
                if (string.Equals(list.Name, normalized, StringComparison.InvariantCultureIgnoreCase))
                    return list;
            return null;
        }

    }

    [Serializable]
    public class RarityCards
    {
        public List<int> ShineCards;
        public List<int> RoyalCards;
        public List<int> GoldCards;
        public List<int> MillenniumCards;
        public List<int> BookCards;
        public List<BookmarkListData> BookmarkLists;
        public RarityCards()
        {
            ShineCards = new List<int>();
            RoyalCards = new List<int>();
            GoldCards = new List<int>();
            MillenniumCards = new List<int>();
            BookCards = new List<int>();
            BookmarkLists = new List<BookmarkListData>();
        }
    }

    [Serializable]
    public class BookmarkListData
    {
        public string Name;
        public List<int> Cards;

        public BookmarkListData()
        {
            Name = string.Empty;
            Cards = new List<int>();
        }

        public BookmarkListData(string name)
        {
            Name = name;
            Cards = new List<int>();
        }
    }
}
