using Cysharp.Threading.Tasks;


namespace YuGiOhFight.Client.Duel
{
    public class MessageDispatcher
    {
        public readonly LogMessage log;
        public readonly VoiceMessage voice;
        public readonly DuelMessage duel;

        public bool playerResponed;

        public MessageDispatcher()
        {
            log = new(this);
            voice = new(this);
            duel = new(this);
        }

        protected Runtime.Network.Package lastPackage;

        public async UniTask Process(Runtime.Network.Package p)
        {
            Controllers.OcgCore core = Runtime.Program.instance == null ? null : Runtime.Program.instance.ocgcore;
            var loopVersion = core == null ? -1 : core.MessageLoopVersion;
            bool CurrentLoop() => core != null && core.showing && core.MessageLoopVersion == loopVersion;
            if (!CurrentLoop()) return;

            if (p.Function != (int)MDPro3.Duel.YGOSharp.GameMessage.Retry)
                lastPackage = p;

            playerResponed = false;

            await log.Process(p);
            if (!CurrentLoop()) return;
            await voice.Process(p);
            if (!CurrentLoop()) return;
            await duel.Process(p);
        }

        public async UniTask RetryMessage()
        {
            if (lastPackage == null)
                return;
            await Process(lastPackage);
        }

        public void Dispose()
        {
            log.Dispose();
            voice.Dispose();
            duel.Dispose();
        }
    }
}
