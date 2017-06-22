﻿using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace WeCantSpell.Hunspell.Infrastructure
{
    internal static class StringBuilderPool
    {
        private const int MaxCachedBuilderCapacity = WordList.MaxWordLen;

        private static StringBuilder Cache;

#if !NO_INLINE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static StringBuilder Get() => GetClearedBuilder();

        public static StringBuilder Get(string value) =>
            GetClearedBuilderWithCapacity(value.Length).Append(value);

        public static StringBuilder Get(string value, int capacity) =>
            GetClearedBuilderWithCapacity(capacity).Append(value);

#if !NO_INLINE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static StringBuilder Get(int capacity) =>
            GetClearedBuilderWithCapacity(capacity);

#if !NO_INLINE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static StringBuilder Get(string value, int valueStartIndex, int valueLength) =>
            Get(value, valueStartIndex, valueLength, valueLength);

        public static StringBuilder Get(string value, int valueStartIndex, int valueLength, int capacity) =>
            GetClearedBuilderWithCapacity(capacity).Append(value, valueStartIndex, valueLength);

        internal static StringBuilder Get(StringSlice value) =>
            Get(value.Text, value.Offset, value.Length, value.Length);

#if !NO_INLINE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static void Return(StringBuilder builder)
        {
#if DEBUG
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
#endif

            if (builder.Capacity <= MaxCachedBuilderCapacity)
            {
#if NO_VOLATILE
                Cache = builder;
#else
                Volatile.Write(ref Cache, builder);
#endif
            }
        }

#if !NO_INLINE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static string GetStringAndReturn(StringBuilder builder)
        {
            var value = builder.ToString();
            Return(builder);
            return value;
        }

#if !NO_INLINE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static StringBuilder GetClearedBuilder()
        {
            var taken = Interlocked.Exchange(ref Cache, null);
            return taken != null
                ? taken.Clear()
                : new StringBuilder();
        }

#if !NO_INLINE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static StringBuilder GetClearedBuilderWithCapacity(int minimumCapacity)
        {
            var taken = Interlocked.Exchange(ref Cache, null);
            return (taken != null && taken.Capacity >= minimumCapacity)
                ? taken.Clear()
                :  new StringBuilder(minimumCapacity);
        }
    }
}
