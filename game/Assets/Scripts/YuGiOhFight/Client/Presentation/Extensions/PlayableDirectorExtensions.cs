namespace YuGiOhFight.Client.Presentation.Extensions
{
    public static class PlayableDirectorExtensions
    {
        public static Cysharp.Threading.Tasks.UniTask WaitAsync(
            this UnityEngine.Playables.PlayableDirector director, bool needPlay = true,
            System.Threading.CancellationToken cancellationToken = default)
        {
            if (director == null)
                return Cysharp.Threading.Tasks.UniTask.CompletedTask;

            director.extrapolationMode = UnityEngine.Playables.DirectorWrapMode.None;
            if(needPlay)
                director.Play();

            return Cysharp.Threading.Tasks.UniTask.WaitUntil(() =>
            {
                return director == null || director.state != UnityEngine.Playables.PlayState.Playing;
            }, cancellationToken: cancellationToken);
        }

        public static Cysharp.Threading.Tasks.UniTask WaitToTimeAsync(
            this UnityEngine.Playables.PlayableDirector director, double time, bool needPlay = true,
            System.Threading.CancellationToken cancellationToken = default)
        {
            if (director == null)
                return Cysharp.Threading.Tasks.UniTask.CompletedTask;

            director.extrapolationMode = UnityEngine.Playables.DirectorWrapMode.None;
            if (needPlay)
                director.Play();

            return Cysharp.Threading.Tasks.UniTask.WaitUntil(() =>
            {
                return director == null || director.state != UnityEngine.Playables.PlayState.Playing || director.time > time;
            }, cancellationToken: cancellationToken);
        }

        public static async Cysharp.Threading.Tasks.UniTask AutoDestroy(this UnityEngine.Playables.PlayableDirector director, bool needPlay = true)
        {
            await director.WaitAsync(needPlay);
            if(director != null)
                UnityEngine.Object.Destroy(director.gameObject);
        }

    }
}
