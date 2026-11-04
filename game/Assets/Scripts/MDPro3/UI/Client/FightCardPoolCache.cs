using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MDPro3.Utility;
using UnityEngine;

namespace MDPro3.UI.Client
{
    // One bounded cache per mounted editor. Cells retain their own references,
    // so evicting a cache entry cannot destroy a texture still on screen.
    internal sealed class FightCardPoolCache : IDisposable
    {
        private sealed class Entry
        {
            internal Texture Texture;
            internal Task<Texture> Pending;
            internal long Used, Bytes;
            internal int Waiters;
        }
        private const long Budget = 160L * 1024 * 1024;
        private const int Capacity = 128;
        private readonly Dictionary<int, Entry> entries = new Dictionary<int, Entry>();
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private long clock, bytes;
        private bool disposed;
        internal int Loads { get; private set; }
        internal int Hits { get; private set; }
        internal int Count => entries.Count;

        internal bool TryAcquire(int code, out Texture texture)
        {
            texture = null;
            if (disposed) return false;
            if (entries.TryGetValue(code, out var entry)) entry.Used = ++clock;
            if (!CardImageLoader.TryAcquireStaticCard(code, out texture)) return false;
            if (entry == null && CardImageLoader.TryAcquireStaticCard(code, out var retained))
            {
                entry = new Entry { Texture = retained, Used = ++clock, Bytes = (long)retained.width * retained.height * 4, Pending = Task.FromResult(retained) };
                entries.Add(code, entry); bytes += entry.Bytes; Trim(code);
            }
            Hits++;
            return true;
        }

        internal async UniTask<Texture> AcquireAsync(int code, CancellationToken token)
        {
            if (disposed) return null;
            if (TryAcquire(code, out var ready)) return ready;
            if (!entries.TryGetValue(code, out var entry))
            {
                entry = new Entry { Used = ++clock };
                entries.Add(code, entry);
                entry.Pending = LoadAsync(code, entry);
            }
            entry.Waiters++;
            try
            {
                await entry.Pending.AsUniTask().AttachExternalCancellation(token);
                token.ThrowIfCancellationRequested();
                return TryAcquire(code, out ready) ? ready : null;
            }
            finally { entry.Waiters--; if (!disposed) Trim(code); }
        }

        private async Task<Texture> LoadAsync(int code, Entry entry)
        {
            Loads++;
            try
            {
                var texture = await CardImageLoader.LoadCardAsync(code, false, lifetime.Token, true);
                if (disposed)
                {
                    if (texture != null) CardImageLoader.ReleaseCard(code, true);
                    return null;
                }
                if (texture == null) { entries.Remove(code); return null; }
                entry.Texture = texture;
                entry.Bytes = (long)texture.width * texture.height * 4;
                bytes += entry.Bytes;
                Trim(code);
                return texture;
            }
            catch (OperationCanceledException) { entries.Remove(code); return null; }
            catch { entries.Remove(code); throw; }
        }

        private void Trim(int retained)
        {
            while (bytes > Budget || entries.Count > Capacity)
            {
                int oldest = 0; long used = long.MaxValue;
                foreach (var pair in entries)
                    if (pair.Key != retained && pair.Value.Texture != null && pair.Value.Waiters == 0 && pair.Value.Used < used)
                    { oldest = pair.Key; used = pair.Value.Used; }
                if (oldest == 0) break;
                bytes -= entries[oldest].Bytes;
                entries.Remove(oldest);
                CardImageLoader.ReleaseCard(oldest, true);
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true; lifetime.Cancel();
            foreach (var pair in entries)
                if (pair.Value.Texture != null) CardImageLoader.ReleaseCard(pair.Key, true);
            entries.Clear(); bytes = 0; lifetime.Dispose();
        }
    }
}
