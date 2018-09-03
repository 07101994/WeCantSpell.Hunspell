using System;
using System.Collections.Generic;

namespace WeCantSpell.Hunspell.Infrastructure
{
    sealed class TextMemoryComparer :
        IEqualityComparer<ReadOnlyMemory<char>>,
        IComparer<ReadOnlyMemory<char>>
    {
        public int Compare(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y) => Compare(x.Span, y.Span);

        public int Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y) => x.SequenceCompareTo(y);

        public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y) => Equals(x.Span, y.Span);

        public bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y) => x.SequenceEqual(y);

        public int GetHashCode(ReadOnlyMemory<char> obj) => GetHashCode(obj.Span);

        public int GetHashCode(ReadOnlySpan<char> obj)
        {
            if (obj.IsEmpty)
            {
                return 0;
            }

            int hash = 17;
            var length = obj.Length;
            hash = unchecked((hash * 31) + length.GetHashCode());
            hash = unchecked((hash * 31) + obj[0].GetHashCode());
            if (length > 1)
            {
                hash = unchecked((hash * 31) + obj[length - 1].GetHashCode());
                if (length > 2)
                {
                    hash = unchecked((hash * 31) + obj[length / 2].GetHashCode());
                }
            }

            return hash;
        }
    }
}
