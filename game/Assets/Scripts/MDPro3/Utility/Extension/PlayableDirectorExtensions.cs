using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.Playables;

namespace MDPro3
{
    public static class PlayableDirectorExtensions
    {
        public static UniTask WaitAsync(
            this PlayableDirector director, bool needPlay = true,
            CancellationToken cancellationToken = default)
        {
            if (director == null)
                return UniTask.CompletedTask;

            director.extrapolationMode = DirectorWrapMode.None;
            if(needPlay)
                director.Play();

            return UniTask.WaitUntil(() =>
            {
                return director == null || director.state != PlayState.Playing;
            }, cancellationToken: cancellationToken);
        }

        public static UniTask WaitToTimeAsync(
            this PlayableDirector director, double time, bool needPlay = true,
            CancellationToken cancellationToken = default)
        {
            if (director == null)
                return UniTask.CompletedTask;

            director.extrapolationMode = DirectorWrapMode.None;
            if (needPlay)
                director.Play();

            return UniTask.WaitUntil(() =>
            {
                return director == null || director.state != PlayState.Playing || director.time > time;
            }, cancellationToken: cancellationToken);
        }

        public static async UniTask AutoDestroy(this PlayableDirector director, bool needPlay = true)
        {
            await director.WaitAsync(needPlay);
            if(director != null)
                UnityEngine.Object.Destroy(director.gameObject);
        }

    }
}

