using MDPro3.Duel.YGOSharp;
using MDPro3.IO;
using MDPro3.Servant;
using Percy;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MDPro3
{
    public class PercyOCG
    {
        public static string HintInGame = Ygopro.HintInGame;

        public static bool godMode;

        private static string error = "Error occurred.";

        private static IntPtr _buffer;

        //private object locker = new();

        public Ygopro ygopro;

        public PercyOCG()
        {
            _buffer = Marshal.AllocHGlobal(1024 * 256); // 256 KiB
            error = InterString.Get("YGOPro旧版的回放崩溃了！您可以选择使用永不崩溃的新版回放。");
            ygopro = new Ygopro(ReceiveHandler, CardHandler, ScriptHandler, ChatHandler);
            //ygopro.m_log = a => UnityEngine.Debug.Log(a);
        }

        private CardData CardHandler(long code)
        {
            var card = CardsManager.Get((int)code);
            var returnValue = new CardData
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

        private ScriptData ScriptHandler(string fileName)
        {
            byte[] content;
            ScriptData ret;
            ret.buffer = IntPtr.Zero;
            ret.len = 0;
            var fileName2 = fileName.TrimStart('.', '/');

            if ((fileName.StartsWith(Program.PATH_PUZZLE) 
                || fileName.StartsWith(Program.PATH_TEMP_FOLDER))
                && File.Exists(fileName))
                content = File.ReadAllBytes(fileName);
            else
                content = FileManager.GetFile(fileName2);
            if (content != null)
            {
                Marshal.Copy(content, 0, _buffer, content.Length);
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

            MDPro3.Program.instance.ocgcore.StocMessage_Error(result);
        }

        private void ReceiveHandler(byte[] buffer)
        {
            var bufferR = new byte[buffer.Length + 1];
            bufferR[0] = 1;
            buffer.CopyTo(bufferR, 1);
            TcpHelper.AddDateJumoLine(bufferR);
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
                MessageManager.Cast(InterString.Get("启动残局<#FF0000>[?]</color>失败。", path));
                return;
            }
            else
            {
                Config.SetBool(path[..^4] + "_Enter", true);
                Config.Save();

                OcgCore.condition = OcgCore.Condition.Duel;
                OcgCore.isFirst = true;
                Program.instance.ocgcore.returnServant = DeckEditor.ToHandTest ? Program.instance.deckEditor : Program.instance.puzzle;
                OcgCore.timeLimit = 0;
                OcgCore.inPuzzle = true;
                Program.instance.ShiftToServant(Program.instance.ocgcore);
                OcgCore.handler = Response;
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
