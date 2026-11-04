using System.IO;
using System.Collections.Concurrent;

namespace MDPro3
{
    public static class Pools
    {

        #region MemoryStream

        private const int MAX_STREAM_SIZE = 1024 * 1024 * 10;
        private static readonly ConcurrentBag<MemoryStream> streamPool = new();

        public static MemoryStream GetStream()
        {
            if (streamPool.TryTake(out var stream))
            {
                stream.SetLength(0);
                return stream;
            }
            return new MemoryStream();
        }

        public static void ReturnStream(MemoryStream stream)
        {
            if (stream.Capacity > MAX_STREAM_SIZE)
            {
                stream.Dispose();
                return;
            }
            stream.SetLength(0);
            streamPool.Add(stream);
        }

        #endregion


    }
}