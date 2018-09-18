using System;
using System.Runtime.InteropServices;
using System.Text;

#if !NO_INLINE
using System.Runtime.CompilerServices;
#endif

namespace WeCantSpell.Hunspell.Infrastructure
{
    static class StringBuilderEx
    {

#if !NO_INLINE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static void Swap(this StringBuilder @this, int indexA, int indexB)
        {
#if DEBUG
            if (indexA < 0 || indexA > @this.Length) throw new ArgumentOutOfRangeException(nameof(indexA));
            if (indexB < 0 || indexB > @this.Length) throw new ArgumentOutOfRangeException(nameof(indexB));
#endif

            var temp = @this[indexA];
            @this[indexA] = @this[indexB];
            @this[indexB] = temp;
        }

        public static string ToStringTerminated(this StringBuilder @this)
        {
            var terminatedIndex = @this.IndexOfNullChar();
            return terminatedIndex >= 0
                ? @this.ToString(0, terminatedIndex)
                : @this.ToString();
        }

        public static string ToStringTerminated(this StringBuilder @this, int startIndex)
        {
            var terminatedIndex = @this.IndexOfNullChar(startIndex);
            if (terminatedIndex < 0)
            {
                terminatedIndex = @this.Length;
            }

            return @this.ToString(startIndex, terminatedIndex - startIndex);
        }

        public static int IndexOfNullChar(this StringBuilder @this, int offset = 0)
        {
            for (; offset < @this.Length; offset++)
            {
                if (@this[offset] == '\0')
                {
                    return offset;
                }
            }

            return -1;
        }

#if !NO_INLINE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static char GetCharOrTerminator(this StringBuilder @this, int index) =>
            index < @this.Length ? @this[index] : '\0';

        public static void RemoveChars(this StringBuilder @this, CharacterSet chars)
        {
            var nextWriteLocation = 0;
            for (var searchLocation = 0; searchLocation < @this.Length; searchLocation++)
            {
                var c = @this[searchLocation];
                if (!chars.Contains(c))
                {
                    @this[nextWriteLocation] = c;
                    nextWriteLocation++;
                }
            }

            @this.Remove(nextWriteLocation, @this.Length - nextWriteLocation);
        }

        public static void Reverse(this StringBuilder @this)
        {
            if (@this == null || @this.Length <= 1)
            {
                return;
            }

            var stopIndex = @this.Length / 2;
            for (int i = 0, otherI = @this.Length - 1; i < stopIndex; i++, otherI--)
            {
                @this.Swap(i, otherI);
            }
        }

        public static void Replace(this StringBuilder @this, int index, int removeCount, string replacement)
        {
            if (replacement.Length <= removeCount)
            {
                for (var i = 0; i < replacement.Length; i++)
                {
                    @this[index + i] = replacement[i];
                }

                if (replacement.Length != removeCount)
                {
                    @this.Remove(index + replacement.Length, removeCount - replacement.Length);
                }
            }
            else
            {
                @this.Remove(index, removeCount);
                @this.Insert(index, replacement);
            }
        }

        public static bool StartsWith(this StringBuilder builder, char c) =>
            builder.Length != 0 && builder[0] == c;

        public static bool EndsWith(this StringBuilder builder, char c) =>
            builder.Length != 0 && builder[builder.Length - 1] == c;

        public static string ToStringWithInsert(this StringBuilder builder, int index, char value)
        {
#if DEBUG
            if (index < 0 || index > builder.Length) throw new ArgumentOutOfRangeException(nameof(index));
#endif

            var buffer = new char[builder.Length + 1];
            buffer[index] = value;

            if (index == 0)
            {
                builder.CopyTo(0, buffer, 1, builder.Length);
            }
            else
            {
                builder.CopyTo(0, buffer, 0, index);
                if (index != builder.Length)
                {
                    builder.CopyTo(index, buffer, index + 1, builder.Length - index);
                }
            }

            return new string(buffer);
        }

        public static StringBuilder Append(this StringBuilder builder, ReadOnlySpan<char> value)
        {
#if DEBUG
            if (builder == null) throw new ArgumentNullException(nameof(builder));
#endif

            if (!value.IsEmpty)
            {
#if !NO_SB_POINTERS
                unsafe
                {
                    fixed (char* start = &MemoryMarshal.GetReference(value))
                    {
                        builder.Append(start, value.Length);
                    }
                }
#else
                for (var i = 0; i < value.Length; i++)
                {
                    builder.Append(value[i]);
                }
#endif
            }

            return builder;
        }
    }
}
