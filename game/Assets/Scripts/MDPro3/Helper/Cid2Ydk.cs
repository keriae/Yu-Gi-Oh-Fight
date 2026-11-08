using System;
using System.IO;
using System.Collections.Generic;

using System.Text.RegularExpressions;
using UnityEngine;
using MDPro3.Duel.YGOSharp;
using Newtonsoft.Json;

namespace MDPro3
{
    public static class Cid2Ydk
    {
        private const string cardsPath = "Data/cards.json";
        private const string cardsAltPath = "Data/cards_Alt.json";
        private const string cardsLitePath = "Data/cards_Lite.json";

        private static bool initialized;
        private static Dictionary<string, Id2Ydk> dic;

        private static void Initialize()
        {
            if(initialized) return;

            dic = ReadMapping(cardsLitePath);
            if (dic == null)
            {
                // The offline client does not require the optional Master Duel
                // identifier tables to display characters or their descriptions.
                dic = ReadMapping(cardsPath) ?? new Dictionary<string, Id2Ydk>();
                var altDic = ReadMapping(cardsAltPath);
                if (altDic != null)
                    foreach(var key in altDic.Keys)
                        dic[key] = altDic[key];

                if (dic.Count > 0)
                    try { File.WriteAllText(cardsLitePath, JsonConvert.SerializeObject(dic)); }
                    catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
                    {
                        Debug.LogWarning("Unable to cache card identifier mappings: " + error.Message);
                    }
            }

            initialized = true;
        }

        private static Dictionary<string, Id2Ydk> ReadMapping(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, Id2Ydk>>(File.ReadAllText(path));
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is JsonException)
            {
                Debug.LogWarning("Unable to read card identifier mappings: " + error.Message);
                return null;
            }
        }

        public static bool HaveCid(int cid)
        {
            Initialize();
            return dic.ContainsKey(cid.ToString());
        }

        public static int GetYDK(int cid)
        {
            Initialize();
            if (dic.ContainsKey(cid.ToString()))
                return dic[cid.ToString()].id;
            return cid;
        }

        public static int GetCID(int ydk)
        {
            Initialize();
            var card = CardsManager.Get(ydk);
            if(card.Alias != 0)
                ydk = card.Alias;
            foreach(var value in dic.Values)
                if(value.id == ydk || value.id == card.Id)
                    return value.cid;
            return ydk;
        }

        public static string ReplaceWithCardName(string origin)
        {
            if (string.IsNullOrEmpty(origin)) return origin ?? string.Empty;
            origin = origin.Replace(" get=\'name\'", string.Empty);
            if (!Regex.IsMatch(origin, @"<card mrk='(\d+)'/>")) return origin;
            Initialize();
            return Regex.Replace(origin, @"<card mrk='(\d+)'/>", EvaluatorGetNameFromNumber);
        }

        private static string EvaluatorGetNameFromNumber(Match match)
        {
            string numberString = match.Groups[1].Value;
            if (!int.TryParse(numberString, out var cid)) return numberString;
            dic.TryGetValue(cid.ToString(), out var code);
            var card = CardsManager.GetCardRaw(code != null ? code.id : cid);
            return card != null && !string.IsNullOrEmpty(card.Name) ? card.Name : numberString;
        }

        public class Id2Ydk
        {
            public int cid;
            public int id;
        }
    }
}
