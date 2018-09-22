using System;
using System.Threading;

namespace WeCantSpell.Hunspell.Infrastructure
{
    static class ArrayEx<T>
    {
#if NO_ARRAY_EMPTY
        public static readonly T[] Empty = new T[0];
#else
        public static readonly T[] Empty = System.Array.Empty<T>();
#endif

        private const int MaxCachePoolBufferLength = 128;

        private static T[] BufferPoolCache;

        public static int GetHashCode(T[] obj)
        {
            if (obj is null)
            {
                return 0;
            }

            if (obj.Length == 0)
            {
                return 17;
            }

            unchecked
            {
                int hash = (17 * 31) + obj.Length.GetHashCode();

                hash = (hash * 31) + GetHashCode(obj[0]);

                if (obj.Length > 1)
                {
                    if (obj.Length > 2)
                    {
                        hash = (hash * 31) + GetHashCode(obj[obj.Length / 2]);
                    }

                    hash = (hash * 31) + GetHashCode(obj[obj.Length - 1]);
                }

                return hash;
            }
        }

        private static int GetHashCode(T obj) =>
            obj == null ? 0 : obj.GetHashCode();

        public static T[] GetFromPool(int minLength)
        {
            var taken = Interlocked.Exchange(ref BufferPoolCache, null);
            return (taken != null && taken.Length >= minLength)
                ? taken
                : new T[minLength];
        }

        public static void RetrunToPool(T[] buffer)
        {
#if DEBUG
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
#endif
            if (buffer.Length <= MaxCachePoolBufferLength)
            {
                Interlocked.Exchange(ref BufferPoolCache, buffer);
            }
        }
    }
}
