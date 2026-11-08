namespace YuGiOhFight.Client.Runtime
{
    public class PercyOCG
    {
        public static string HintInGame = Percy.Ygopro.HintInGame;

        public static bool godMode;

        private static string error = "Error occurred.";

        private static System.IntPtr _buffer;

        //private object locker = new();

        public Percy.Ygopro ygopro;

        public PercyOCG()
        {
            _buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(1024 * 256); // 256 KiB
            error = MDPro3.InterString.Get("YGOPro旧版的回放崩溃了！您可以选择使用永不崩溃的新版回放。");
            ygopro = new Percy.Ygopro(ReceiveHandler, CardHandler, ScriptHandler, ChatHandler);
            //ygopro.m_log = a => UnityEngine.Debug.Log(a);
        }

        private Percy.CardData CardHandler(long code)
        {
            var card = MDPro3.Duel.YGOSharp.CardsManager.Get((int)code);
            var returnValue = new Percy.CardData
            {
                Code = card.Id,
                Alias = card.Alias,
                Attack = card.Attack,
                Attribute = card.Attribute,
                Defense = card.Defense,
                Level = card.Level,
                LScale = card.LScale,
                Race = card.Race,
                RScale = card.RScale,
                Type = card.Type,
                LinkMarker = card.LinkMarker
            };
            returnValue.ConvertLongToSetCode(card.Setcode);
            return returnValue;
        }

        private Percy.ScriptData ScriptHandler(string fileName)
        {
            byte[] content;
            Percy.ScriptData ret;
            ret.buffer = System.IntPtr.Zero;
            ret.len = 0;
            var fileName2 = fileName.TrimStart('.', '/');

            if ((fileName.StartsWith(Program.PATH_PUZZLE)
                || fileName.StartsWith(Program.PATH_TEMP_FOLDER))
                && System.IO.File.Exists(fileName))
                content = System.IO.File.ReadAllBytes(fileName);
            else
                content = MDPro3.IO.FileManager.GetFile(fileName2);
            if (content != null)
            {
                System.Runtime.InteropServices.Marshal.Copy(content, 0, _buffer, content.Length);
                ret.buffer = _buffer;
                ret.len = content.Length;
            }

            return ret;
        }

        private void ChatHandler(string result)
        {
            //var p = new BinaryMaster();
            //p.writer.Write((byte)GameMessage.sibyl_chat);
            ////result = result.Replace("Error Occurred.", error);
            //p.writer.WriteUnicode(result, result.Length + 1);
            //ReceiveHandler(p.Get());

            Program.instance.ocgcore.StocMessage_Error(result);
        }

        private void ReceiveHandler(byte[] buffer)
        {
            var bufferR = new byte[buffer.Length + 1];
            bufferR[0] = 1;
            buffer.CopyTo(bufferR, 1);
            Network.TcpHelper.AddDateJumoLine(bufferR);
        }

        public void Dispose()
        {
            ygopro.Dispose();
        }

        public void Response(byte[] resp)
        {
            //UnityEngine.Debug.Log(Program.instance.ocgcore.currentMessage + ": " + BitConverter.ToString(resp));
            ygopro.Response(resp);
        }

        public void StartPuzzle(string path)
        {
            if (!ygopro.StartPuzzle(path))
            {
                MessageManager.Cast(MDPro3.InterString.Get("启动残局<#FF0000>[?]</color>失败。", path));
                return;
            }
            else
            {
                MDPro3.Config.SetBool(path[..^4] + "_Enter", true);
                MDPro3.Config.Save();

                Controllers.OcgCore.condition = Controllers.OcgCore.Condition.Duel;
                Controllers.OcgCore.isFirst = true;
                Program.instance.ocgcore.returnServant = Controllers.DeckEditor.ToHandTest ? global::YuGiOhFight.Client.Runtime.Program.instance.deckEditor : global::YuGiOhFight.Client.Runtime.Program.instance.puzzle;
                Controllers.OcgCore.timeLimit = 0;
                Controllers.OcgCore.inPuzzle = true;
                Program.instance.ShiftToServant(Program.instance.ocgcore);
                Controllers.OcgCore.handler = Response;
            }
        }

        public void StartAI()
        {
            //Program.instance.ocgcore.handler = Response;
        }

        public void StartDuel()
        {

        }

    }
}
