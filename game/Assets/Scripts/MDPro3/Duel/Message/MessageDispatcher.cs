using Cysharp.Threading.Tasks;
using MDPro3.Duel.YGOSharp;

namespace MDPro3.Duel
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

        protected Package lastPackage;

        public async UniTask Process(Package p)
        {
            if (p.Function != (int)GameMessage.Retry)
                lastPackage = p;

            playerResponed = false;

            await log.Process(p);
            await voice.Process(p);
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
