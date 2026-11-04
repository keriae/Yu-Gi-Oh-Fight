using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;

namespace MDPro3
{
    public static class DOTweenExtensions
    {
        public static UniTask WaitAsync(
            this Sequence sequence,
            CancellationToken cancellationToken = default
        )
        {
            if (sequence == null || !sequence.active || cancellationToken.IsCancellationRequested)
                return UniTask.CompletedTask;

            var utcs = new UniTaskCompletionSource();

            TweenCallback originalOnComplete = sequence.onComplete;
            TweenCallback originalOnKill = sequence.onKill;
            sequence.OnComplete(() =>
            {
                try
                {
                    originalOnComplete?.Invoke();
                }
                finally
                {
                    utcs.TrySetResult();
                }
            });

            sequence.Play();

            cancellationToken.RegisterWithoutCaptureExecutionContext(() =>
            {
                sequence.Kill();
                utcs.TrySetCanceled(cancellationToken);
            });

            sequence.OnKill(() =>
            {
                try { originalOnKill?.Invoke(); }
                finally
                {
                    if (!utcs.Task.Status.IsCompleted())
                        utcs.TrySetCanceled();
                }
            });

            return utcs.Task;
        }
    }
}
