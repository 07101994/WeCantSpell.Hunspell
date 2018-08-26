﻿using System;

namespace WeCantSpell.Hunspell.Infrastructure
{
    static class ReadOnlySpanEx
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
            for (var searchLocation = 0; searchLocation < @this.Length; searchLocation++)
            {
                var c = @this[searchLocation];
                if (chars.Contains(c))
                {
                    return searchLocation;
                }
            }

            return -1;
        }

        public static bool ContainsAny(this ReadOnlySpan<char> @this, char value0, char value1) =>
            @this.IndexOfAny(value0, value1) >= 0;

        public static bool Contains(this ReadOnlySpan<char> @this, char value) =>
            @this.IndexOf(value) >= 0;

        public static bool StartsWith(this ReadOnlySpan<char> @this, char value) =>
            !@this.IsEmpty && @this[0] == value;

        public static bool EndsWith(this ReadOnlySpan<char> @this, char value) =>
            !@this.IsEmpty && @this[@this.Length - 1] == value;

        public static bool EqualsOrdinal(this ReadOnlySpan<char> @this, string value) =>
            value != null && @this.Equals(value.AsSpan(), StringComparison.OrdinalIgnoreCase);

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
            return partLength > 0
                && partHandler(@this.Slice(startIndex, partLength), partIndex);
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
            return partLength > 0
                && partHandler(@this.Slice(startIndex, partLength), partIndex);
        }

        public static bool SplitOnTabOrSpace(this ReadOnlySpan<char> @this, SplitPartHandler partHandler) =>
            @this.Split(' ', '\t', partHandler);

        public static ReadOnlySpan<char> Remove(this ReadOnlySpan<char> @this, char value)
        {
            var removeIndex = @this.IndexOf(value);
            if (removeIndex < 0)
            {
                return @this;
            }

            // TODO: use firstRemoveIndex to optimize

            return @this.ToString().Replace(value.ToString(), string.Empty).AsSpan();
        }

        public static ReadOnlySpan<char> Replace(this ReadOnlySpan<char> @this, char oldChar, char newChar)
        {
            var replaceIndex = @this.IndexOf(oldChar);
            if (replaceIndex < 0)
            {
                return @this;
            }

            // TODO: use replaceIndex to optimize

            return @this.ToString().Replace(oldChar, newChar).AsSpan();
        }

        public static ReadOnlySpan<char> Replace(this ReadOnlySpan<char> @this, string oldText, string newText)
        {
            var replaceIndex = @this.IndexOf(oldText.AsSpan());
            if (replaceIndex < 0)
            {
                return @this;
            }

            // TODO: use replaceIndex to optimize

            return @this.ToString().Replace(oldText, newText).AsSpan();
        }

        public static string Reversed(this ReadOnlySpan<char> @this)
        {
            if (@this.IsEmpty)
            {
                return string.Empty;
            }

            if (@this.Length == 1)
            {
                return @this.ToString();
            }

            var chars = new char[@this.Length];
            var lastIndex = @this.Length - 1;
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = @this[lastIndex - i];
            }

            return new string(chars);
        }

        public static ReadOnlySpan<char> Limit(this ReadOnlySpan<char> @this, int maxLength)
        {
#if DEBUG
            if (maxLength < 0) throw new ArgumentOutOfRangeException(nameof(maxLength));
#endif
            return @this.Length > maxLength ? @this.Slice(0, maxLength) : @this;
        }
    }
}
