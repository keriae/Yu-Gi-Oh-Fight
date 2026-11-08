using System.IO;
using MDPro3.Duel.YGOSharp;

namespace MDPro3.AI
{
    /// <summary>Copies AI A's visible game state without consuming its protocol reader.</summary>
    public static class ResponsesSpectatorFeed
    {
        public static YuGiOhFight.Client.Runtime.Network.Package Capture(int function, BinaryReader reader)
        {
            switch ((GameMessage)function)
            {
                case GameMessage.Retry:
                case GameMessage.SelectBattleCmd:
                case GameMessage.SelectIdleCmd:
                case GameMessage.SelectEffectYn:
                case GameMessage.SelectYesNo:
                case GameMessage.SelectOption:
                case GameMessage.SelectCard:
                case GameMessage.SelectChain:
                case GameMessage.SelectPlace:
                case GameMessage.SelectPosition:
                case GameMessage.SelectTribute:
                case GameMessage.SortChain:
                case GameMessage.SelectCounter:
                case GameMessage.SelectSum:
                case GameMessage.SelectDisfield:
                case GameMessage.SortCard:
                case GameMessage.SelectUnselect:
                case GameMessage.RockPaperScissors:
                case GameMessage.AnnounceRace:
                case GameMessage.AnnounceAttrib:
                case GameMessage.AnnounceCard:
                case GameMessage.AnnounceNumber:
                    return null;
            }
            var position = reader.BaseStream.Position;
            byte[] data;
            try { data = reader.ReadBytes((int)(reader.BaseStream.Length - position)); }
            finally { reader.BaseStream.Position = position; }
            // Keep AI A's native seat even when it goes second; the high bit
            // retains spectator controls while LocalPlayer maps A to the bottom.
            if (function == (int)GameMessage.Start && data.Length > 0) data[0] |= 0x10;
            return new YuGiOhFight.Client.Runtime.Network.Package { Function = function, Data = new YuGiOhFight.Client.Runtime.Network.BinaryMaster(data) };
        }
    }
}
