using System;
using System.Collections.Generic;
using System.Linq;
using WeCantSpell.Hunspell.Infrastructure;

namespace WeCantSpell.Hunspell
{
    public sealed class MorphSet : ArrayWrapper<string>, IEquatable<MorphSet>
    {
        public static readonly MorphSet Empty = TakeArray(ArrayEx<string>.Empty);

        public static MorphSet Create(IEnumerable<string> morphs) => morphs == null ? Empty : TakeArray(morphs.ToArray());

        internal static MorphSet TakeArray(string[] morphs) => morphs == null ? Empty : new MorphSet(morphs);

        internal static void ApplyReverseInPlace(string[] morphs)
        {
#if DEBUG
            if (morphs == null) throw new ArgumentNullException(nameof(morphs));
#endif
            if (morphs.Length == 1)
            {
                morphs[0] = morphs[0].GetReversed();
                return;
            }

            for (var i = 0; i < morphs.Length; i++)
            {
                ref var morph = ref morphs[i];
                morph = morph.GetReversed();
            }

            Array.Reverse(morphs);
        }

        private MorphSet(string[] morphs)
            : base(morphs)
        {
        }

        internal string Join(string seperator) => string.Join(seperator, items);

        public bool Equals(MorphSet other) => !(other is null) && Comparer.SequenceEquals(other.items, items);

        public override bool Equals(object obj) => Equals(obj as MorphSet);

        public override int GetHashCode() => ArrayEx<string>.GetHashCode(items);

        public sealed class Comparer : IEqualityComparer<MorphSet>
        {
            public static readonly Comparer Default = new Comparer();

            public bool Equals(MorphSet x, MorphSet y) => SequenceEquals(x, y);

            public int GetHashCode(MorphSet obj) => ArrayEx<string>.GetHashCode(obj.items);

            public static bool SequenceEquals(MorphSet x, MorphSet y)
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

            internal static bool SequenceEquals(string[] x, string[] y)
            {
                if (x.Length != y.Length)
                {
                    return false;
                }

                for (var i = 0; i < x.Length; i++)
                {
                    if (!StringComparer.Ordinal.Equals(x[i], y[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
