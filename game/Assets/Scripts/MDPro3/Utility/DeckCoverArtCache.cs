using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MDPro3.Utility
{
    /// <summary>Keeps recently viewed deck art alive while its selector is open.</summary>
    internal sealed class DeckCoverArtCache : IDisposable
    {
        private const int RetainedCoverLimit = 64;
        private readonly Dictionary<int, Entry> entries = new();
        private long lastUse;
        private bool disposed;

        public int Count => entries.Count;
        public int LoadCount { get; private set; }

        internal sealed class Entry
        {
            internal DeckCoverArtCache Owner;
            internal int Code;
            internal Texture2D Texture;
            internal Task<Texture2D> LoadingTask;
            internal readonly CancellationTokenSource Cancellation = new();
            internal int Users;
            internal long LastUse;
            internal bool Retired;
        }

        public Entry Acquire(int code)
        {
            if (disposed) throw new ObjectDisposedException(nameof(DeckCoverArtCache));
            if (entries.TryGetValue(code, out var entry)
                && entry.LoadingTask.IsCompleted && entry.Texture == null)
            {
                Retire(entry);
                entry = null;
            }
            if (entry == null)
            {
                entry = new Entry { Owner = this, Code = code, Users = 1, LastUse = ++lastUse };
                entries.Add(code, entry);
                ++LoadCount;
                entry.LoadingTask = LoadAsync(entry);
                Trim();
            }
            else
            {
                ++entry.Users;
                entry.LastUse = ++lastUse;
            }
            return entry;
        }

        public void Release(Entry entry)
        {
            if (entry == null || entry.Owner != this || entry.Users == 0) return;
            --entry.Users;
            if (!disposed) Trim();
        }

        private async Task<Texture2D> LoadAsync(Entry entry)
        {
            try
            {
                // A recycled row cancels only its wait. Other rows and a later
                // visit can still use this one disk read and decoded texture.
                var art = await CardImageLoader.LoadArtAsync(entry.Code, false, entry.Cancellation.Token);
                if (entry.Retired || art == null)
                {
                    CardImageLoader.ReleaseArt(entry.Code);
                    return null;
                }
                entry.Texture = art;
                return art;
            }
            finally
            {
                if (entry.Retired) entry.Cancellation.Dispose();
            }
        }

        private void Trim()
        {
            while (entries.Count > RetainedCoverLimit)
            {
                Entry oldest = null;
                foreach (var entry in entries.Values)
                    if (entry.Users == 0 && (oldest == null || entry.LastUse < oldest.LastUse))
                        oldest = entry;
                if (oldest == null) return; // Visible rows keep their textures.
                Retire(oldest);
            }
        }

        private void Retire(Entry entry)
        {
            if (entry.Retired) return;
            entry.Retired = true;
            entries.Remove(entry.Code);
            entry.Cancellation.Cancel();
            if (entry.Texture != null)
            {
                entry.Texture = null;
                CardImageLoader.ReleaseArt(entry.Code);
            }
            if (entry.LoadingTask != null && entry.LoadingTask.IsCompleted)
                entry.Cancellation.Dispose();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            // Retire removes dictionary entries; iterate a snapshot on exit.
            foreach (var entry in new List<Entry>(entries.Values)) Retire(entry);
        }
    }
}
