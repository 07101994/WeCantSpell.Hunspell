using System;

namespace WeCantSpell.Hunspell.Infrastructure
{
    static class MemoryEx
    {
        public delegate bool SplitPartHandler(ReadOnlySpan<char> part, int index);

        public static int IndexOf(this ReadOnlySpan<char> @this, char value, int startIndex)
        {
            var result = @this.Slice(startIndex).IndexOf(value);
            if (result >= 0)
            {
                result += startIndex;
            }

            return result;
        }

        public static int IndexOfAny(this ReadOnlySpan<char> @this, char value0, char value1, int startIndex)
        {
            var result = @this.Slice(startIndex).IndexOfAny(value0, value1);
            if (result >= 0)
            {
                result += startIndex;
            }

            return result;
        }

        public static int IndexOfAny(this ReadOnlySpan<char> @this, CharacterSet chars)
        {
            if (chars.HasItems)
            {
                if (chars.Count == 1)
                {
                    return @this.IndexOf(chars[0]);
                }
                
                if (chars.Count == 2)
                {
                    return @this.IndexOfAny(chars[0], chars[1]);
                }

                for (var searchLocation = 0; searchLocation < @this.Length; searchLocation++)
                {
                    if (chars.Contains(@this[searchLocation]))
                    {
                        return searchLocation;
                    }
                }
            }

            return -1;
        }

        public static bool EqualsOrdinalIgnoreCase(this ReadOnlySpan<char> @this, string value) =>
            value != null && @this.Equals(value.AsSpan(), StringComparison.OrdinalIgnoreCase);

        public static bool EqualsLimited(this ReadOnlySpan<char> @this, ReadOnlySpan<char> value, int lengthLimit) =>
            @this.Limit(lengthLimit).SequenceEqual(value.Limit(lengthLimit));

        public static bool ContainsAny(this ReadOnlySpan<char> @this, char value0, char value1) =>
            @this.IndexOfAny(value0, value1) >= 0;

        public static bool Contains(this ReadOnlySpan<char> @this, char value) =>
            @this.IndexOf(value) >= 0;

        public static bool Contains(this ReadOnlySpan<char> @this, ReadOnlySpan<char> value) =>
            @this.IndexOf(value) >= 0;

        public static bool StartsWithOrdinalIgnoreCase(this ReadOnlySpan<char> @this, string value)
        {
#if DEBUG
            if (value == null) throw new ArgumentNullException(nameof(value));
#endif
            return @this.StartsWith(value.AsSpan(), StringComparison.OrdinalIgnoreCase);
        }

        public static bool StartsWith(this ReadOnlySpan<char> @this, char value) =>
            !@this.IsEmpty && @this[0] == value;

        public static bool EndsWith(this ReadOnlySpan<char> @this, char value) =>
            !@this.IsEmpty && @this[@this.Length - 1] == value;

        public static bool Split(this ReadOnlySpan<char> @this, char value0, SplitPartHandler partHandler)
        {
            int partIndex = 0;
            int startIndex = 0;
            int commaIndex;
            int partLength;
            while ((commaIndex = @this.IndexOf(value0, startIndex)) >= 0)
            {
                partLength = commaIndex - startIndex;
                if (partLength > 0)
                {
                    if (!partHandler(@this.Slice(startIndex, partLength), partIndex))
                    {
                        return false;
                    }

                    partIndex++;
                }

                startIndex = commaIndex + 1;
            }

            partLength = @this.Length - startIndex;
            if (partLength > 0)
            {
                return partHandler(@this.Slice(startIndex, partLength), partIndex);
            }

            return true;
        }

        public static bool Split(this ReadOnlySpan<char> @this, char value0, char value1, SplitPartHandler partHandler)
        {
            int partIndex = 0;
            int startIndex = 0;
            int commaIndex;
            int partLength;
            while ((commaIndex = @this.IndexOfAny(value0, value1, startIndex)) >= 0)
            {
                partLength = commaIndex - startIndex;
                if (partLength > 0)
                {
                    if (!partHandler(@this.Slice(startIndex, partLength), partIndex++))
                    {
                        return false;
                    }
                }

                startIndex = commaIndex + 1;
            }

            partLength = @this.Length - startIndex;
            if (partLength > 0)
            {
                return partHandler(@this.Slice(startIndex, partLength), partIndex);
            }

            return true;
        }

        public static bool SplitOnTabOrSpace(this ReadOnlySpan<char> @this, SplitPartHandler partHandler) =>
            @this.Split(' ', '\t', partHandler);

        public static string ToStringWithout(this ReadOnlySpan<char> @this, char value)
        {
            var searchIndex = @this.IndexOf(value);
            if (searchIndex < 0)
            {
                return @this.ToString();
            }

            if (searchIndex == @this.Length - 1)
            {
                return @this.Slice(0, searchIndex).ToString();
            }

            var builder = StringBuilderPool.Get(@this.Length - 1);
            builder.Append(@this.Slice(0, searchIndex));

            for (searchIndex++; searchIndex < @this.Length; searchIndex++)
            {
                ref readonly var c = ref @this[searchIndex];
                if (value != c)
                {
                    builder.Append(c);
                }
            }

            return StringBuilderPool.GetStringAndReturn(builder);
        }

        public static ReadOnlySpan<char> GetReversed(this ReadOnlySpan<char> @this)
        {
            if (@this.Length <= 1)
            {
                return @this;
            }

            var result = @this.ToString();
            var indexLimit = result.Length / 2;
            unsafe
            {
                fixed (char* p = result)
                {
                    for (int i = 0, otherI = result.Length - 1; i < indexLimit; i++, otherI--)
                    {
                        ReferenceHelpers.Swap(ref p[i], ref p[otherI]);
                    }
                }
            }
            return result.AsSpan();
        }

        public static ReadOnlySpan<char> Limit(this ReadOnlySpan<char> @this, int maxLength)
        {
#if DEBUG
            if (maxLength < 0) throw new ArgumentOutOfRangeException(nameof(maxLength));
#endif
            return @this.Length > maxLength ? @this.Slice(0, maxLength) : @this;
        }

        public static string ConcatString(this ReadOnlySpan<char> @this, string value)
        {
#if DEBUG
            if (value == null) throw new ArgumentNullException(nameof(value));
#endif
            if (@this.IsEmpty)
            {
                return value;
            }
            if (value.Length == 0)
            {
                return @this.ToString();
            }

            var builder = StringBuilderPool.Get(@this.Length + value.Length);
            builder.Append(@this);
            builder.Append(value);
            return StringBuilderPool.GetStringAndReturn(builder);
        }

        public static string ConcatString(this ReadOnlySpan<char> @this, char value) => @this.ConcatString(value.ToString());
    }
}
