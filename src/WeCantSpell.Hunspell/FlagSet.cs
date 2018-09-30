using System;
using System.Collections.Generic;
using System.Linq;
using WeCantSpell.Hunspell.Infrastructure;

namespace WeCantSpell.Hunspell
{
    public sealed class FlagSet : ArrayWrapper<FlagValue>, IEquatable<FlagSet>
    {
        public static readonly FlagSet Empty = new FlagSet(ArrayEx<FlagValue>.Empty);

        public static FlagSet Create(IEnumerable<FlagValue> given) => TakeArray(given?.ToArray());

        public static FlagSet Union(FlagSet a, FlagSet b)
        {
            var aItems = a.items;
            var bItems = b.items;

            if (aItems.Length == 0)
            {
                return bItems.Length == 0 ? Empty : b;
            }
            else if(bItems.Length == 0)
            {
                return a;
            }

            var aIndex = 0;
            var aItem = aItems[0];
            var bIndex = 0;
            var bItem = bItems[0];
            var items = new FlagValue[a.items.Length + b.items.Length];
            var writeIndex = 0;

            while (true)
            {
                if (aItem <= bItem)
                {
                    items[writeIndex++] = aItem;
                    aIndex++;
                    if (aIndex >= aItems.Length)
                    {
                        break;
                    }

                    aItem = aItems[aIndex];
                }
                else
                {
                    items[writeIndex++] = bItem;
                    bIndex++;
                    if (bIndex >= bItems.Length)
                    {
                        break;
                    }

                    bItem = bItems[bIndex];
                }
            }

            if (aIndex < aItems.Length)
            {
                var len = aItems.Length - aIndex;
                Array.Copy(aItems, aIndex, items, writeIndex, len);
                writeIndex += len;
            }
            else if(bIndex < bItems.Length)
            {
                var len = bItems.Length - bIndex;
                Array.Copy(bItems, bIndex, items, writeIndex, len);
                writeIndex += len;
            }

            if (writeIndex < items.Length)
            {
                Array.Resize(ref items, writeIndex);
            }

            return new FlagSet(items);
        }

        public static bool ContainsAny(FlagSet a, FlagSet b)
        {
            if (a != null && a.HasItems && b != null && b.HasItems)
            {
                var aItems = a.items;
                var bItems = b.items;

                if (a.Count == 1)
                {
                    return b.Contains(aItems[0]);
                }
                if (b.Count == 1)
                {
                    return a.Contains(bItems[0]);
                }

                if (a.Count > b.Count)
                {
                    ReferenceHelpers.Swap(ref a, ref b);
                }

                var bLow = bItems[0];
                var bHigh = bItems[bItems.Length - 1];

                foreach (var aItem in aItems)
                {
                    if (aItem >= bLow)
                    {
                        if (aItem > bHigh)
                        {
                            break;
                        }

                        if (b.Contains(aItem))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        internal static FlagSet TakeArray(FlagValue[] values)
        {
            if (values == null || values.Length == 0)
            {
                return Empty;
            }

            Array.Sort(values);

            return new FlagSet(values);
        }

        internal static FlagSet Union(FlagSet set, FlagValue value)
        {
            var valueIndex = Array.BinarySearch(set.items, value);
            if (valueIndex >= 0)
            {
                return set;
            }

            valueIndex = ~valueIndex; // locate the best insertion point

            var newItems = new FlagValue[set.items.Length + 1];
            if (valueIndex >= set.items.Length)
            {
                Array.Copy(set.items, newItems, set.items.Length);
                newItems[set.items.Length] = value;
            }
            else
            {
                Array.Copy(set.items, newItems, valueIndex);
                newItems[valueIndex] = value;
                Array.Copy(set.items, valueIndex, newItems, valueIndex + 1, set.items.Length - valueIndex);
            }

            return new FlagSet(newItems);
        }

        private FlagSet(FlagValue[] values)
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

        public bool Contains(FlagValue value)
        {
            if (value.IsZero || IsEmpty)
            {
                return false;
            }
            if (items.Length == 1)
            {
                return value.Equals(items[0]);
            }

            return (unchecked(value & mask) != default)
                && value >= items[0]
                && value <= items[items.Length - 1]
                && Array.BinarySearch(items, value) >= 0;
        }

        public bool ContainsAny(FlagSet values) => ContainsAny(this, values);

        public bool ContainsAny(FlagValue a, FlagValue b) =>
            HasItems && (Contains(a) || Contains(b));

        public bool ContainsAny(FlagValue a, FlagValue b, FlagValue c) =>
            HasItems && (Contains(a) || Contains(b) || Contains(c));

        public bool ContainsAny(FlagValue a, FlagValue b, FlagValue c, FlagValue d) =>
            HasItems && (Contains(a) || Contains(b) || Contains(c) || Contains(d));

        public bool Equals(FlagSet other) => !(other is null) && Comparer.SequenceEquals(other.items, items);

        public override bool Equals(object obj) => Equals(obj as FlagSet);

        public override int GetHashCode() => ArrayEx<FlagValue>.GetHashCode(items);

        public sealed class Comparer : IEqualityComparer<FlagSet>
        {
            public static readonly Comparer Default = new Comparer();

            public bool Equals(FlagSet x, FlagSet y) => SequenceEquals(x, y);

            public int GetHashCode(FlagSet obj) => ArrayEx<FlagValue>.GetHashCode(obj.items);

            public static bool SequenceEquals(FlagSet x, FlagSet y)
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

            internal static bool SequenceEquals(FlagValue[] x, FlagValue[] y) =>
                x.Length == y.Length && x.AsSpan().SequenceEqual(y.AsSpan());
        }
    }
}
