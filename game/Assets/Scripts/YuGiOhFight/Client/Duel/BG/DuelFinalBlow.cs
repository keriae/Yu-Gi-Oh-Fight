namespace YuGiOhFight.Client.Duel
{
    public class DuelFinalBlow : LoopTrackManager
    {
        public void Destroy()
        {
            StopLoop();
            Destroy(gameObject, 0.5f);
        }
    }
}