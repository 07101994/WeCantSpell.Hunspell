using System;
using System.Text;
using System.Threading;

namespace WeCantSpell.Hunspell.Infrastructure
{
    static class StringBuilderPool
    {
        private const int MaxCachedBuilderCapacity = WordList.MaxWordLen;

        private static StringBuilder Cache;

        public static StringBuilder Get()
        {
            var taken = Interlocked.Exchange(ref Cache, null);
            return taken != null
                ? taken.Clear()
                : new StringBuilder();
        }

        public static StringBuilder Get(string value) =>
            GetClearedBuilderWithCapacity(value.Length).Append(value);

        public static StringBuilder Get(string value, int capacity) =>
            GetClearedBuilderWithCapacity(capacity).Append(value);

        public static StringBuilder Get(int capacity) =>
            GetClearedBuilderWithCapacity(capacity);

        public static StringBuilder Get(ReadOnlySpan<char> value) =>
            GetClearedBuilderWithCapacity(value.Length).Append(value);

        public static void Return(StringBuilder builder)
        {
#if DEBUG
            if (builder == null) throw new ArgumentNullException(nameof(builder));
#endif

            if (builder.Capacity <= MaxCachedBuilderCapacity)
            {
                Volatile.Write(ref Cache, builder);
            }
        }

        public static string GetStringAndReturn(StringBuilder builder)
        {
            var value = builder.ToString();
            Return(builder);
            return value;
        }

        private static StringBuilder GetClearedBuilderWithCapacity(int minimumCapacity)
        {
            var taken = Interlocked.Exchange(ref Cache, null);
            return (taken != null && taken.Capacity >= minimumCapacity)
                ? taken.Clear()
                :  new StringBuilder(minimumCapacity);
        }
    }
}
