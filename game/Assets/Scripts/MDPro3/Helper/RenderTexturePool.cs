using System.Collections.Generic;
using UnityEngine;

public static class RenderTexturePool
{
    private static readonly Dictionary<(int width, int height), Queue<RenderTexture>> _pool = new();

    public static RenderTexture Get(int width, int height)
    {
        var key = (width, height);
        if (_pool.TryGetValue(key, out var queue) && queue.Count > 0)
        {
            var rt = queue.Dequeue();
            if (rt != null && rt.IsCreated())
            {
                return rt;
            }
        }

        return CreateNew(width, height);
    }

    public static void Return(RenderTexture rt)
    {
        if (rt == null) return;
        RenderTexture.active = null;

        if (!rt.IsCreated())
            return;

        var key = (rt.width, rt.height);
        if (!_pool.ContainsKey(key))
            _pool[key] = new Queue<RenderTexture>();
        _pool[key].Enqueue(rt);
    }

    private static RenderTexture CreateNew(int width, int height)
    {
        RenderTexture rt = new(width, height, 0, RenderTextureFormat.ARGB32)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            autoGenerateMips = false
        };
        rt.Create();
        return rt;
    }

    public static void Clear()
    {
        foreach (var queue in _pool.Values)
        {
            while (queue.Count > 0)
            {
                var rt = queue.Dequeue();
                if (rt != null && rt.IsCreated())
                {
                    rt.Release();
                }
                Object.Destroy(rt);
            }
        }
        _pool.Clear();
    }
}