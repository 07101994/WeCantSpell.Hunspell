using System;
using WeCantSpell.Hunspell.Infrastructure;
using System.Collections.Generic;

namespace WeCantSpell.Hunspell
{
    public sealed class CharacterSet : ArrayWrapper<char>
    {
        public static readonly CharacterSet Empty = new CharacterSet(ArrayEx<char>.Empty);

        public static CharacterSet Create(string values) => values == null ? Empty : TakeArray(values.ToCharArray());

        public static CharacterSet Create(char value) => TakeArray(new[] { value });

        internal static CharacterSet Create(ReadOnlySpan<char> values) => TakeArray(values.ToArray());

        internal static CharacterSet TakeArray(char[] values)
        {
#if DEBUG
            if (values == null) throw new ArgumentNullException(nameof(values));
#endif

            Array.Sort(values);
            return new CharacterSet(values);
        }

        private CharacterSet(char[] values)
            : base(values)
        {
            mask = default;
            for (var i = 0; i < values.Length; i++)
            {
                unchecked
                {
                    mask |= values[i];
                }
            }
        }

        private readonly char mask;

        public bool Contains(char value) =>
            unchecked((value & mask) != default)
            &&
            Array.BinarySearch(items, value) >= 0;

        public string GetCharactersAsString() => new string(items);

        public sealed class Comparer : IEqualityComparer<CharacterSet>
        {
            public static readonly Comparer Default = new Comparer();

            public bool Equals(CharacterSet x, CharacterSet y) => SequenceEquals(x, y);

            public int GetHashCode(CharacterSet obj) => ArrayEx<char>.GetHashCode(obj.items);

            public static bool SequenceEquals(CharacterSet x, CharacterSet y)
            {
                if (x is null)
                {
                    return y is null;
                }

                if (y is null)
                {
                    return false;
                }

                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                return SequenceEquals(x.items, y.items);
            }

            internal static bool SequenceEquals(char[] x, char[] y) =>
                x.Length == y.Length && x.AsSpan().SequenceEqual(y.AsSpan());
        }
    }
}
