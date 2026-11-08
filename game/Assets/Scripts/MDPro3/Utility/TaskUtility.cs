using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;


namespace MDPro3.Utility
{
    public static class TaskUtility
    {
        public static async Task WaitWhile(Func<bool> condition)
        {
            while(condition())
            {
                await WaitOneFrame();
            }
        }

        public static async Task WaitUntil(Func<bool> condition)
        {
            while (!condition())
            {
                await WaitOneFrame();
            }
        }

        public static async Task WaitOneFrame()
        {
            await Task.Yield();
            if(!Application.isPlaying)
                throw new OperationCanceledException();
        }

        public static async Task WaitOneFrame(GameObject gameObject)
        {
            await Task.Yield();
            if (!Application.isPlaying || gameObject == null)
                throw new OperationCanceledException();
        }

        public static async Task WaitOneFrame(CancellationToken token)
        {
            await Task.Yield();
            if(!Application.isPlaying)
                throw new OperationCanceledException();
            token.ThrowIfCancellationRequested();
        }

        public static async Task WaitOneFrame(GameObject gameObject, CancellationToken token)
        {
            await Task.Yield();
            if (!Application.isPlaying || gameObject == null)
                throw new OperationCanceledException();
            token.ThrowIfCancellationRequested();
        }

    }
}
