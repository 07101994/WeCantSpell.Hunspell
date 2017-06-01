﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WeCantSpell.Hunspell.Infrastructure
{
    public class ListWrapper<T> :
#if NO_READONLYCOLLECTIONS
        IEnumerable<T>
#else
        IReadOnlyList<T>
#endif
    {
        protected readonly List<T> items;

        protected ListWrapper(List<T> items)
        {
            this.items = items ?? throw new ArgumentNullException(nameof(items));
            HasItems = items.Count != 0;
            IsEmpty = !HasItems;
        }

        public T this[int index]
        {
#if !NO_INLINE
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            get => items[index];
        }

        public int Count
        {
#if !NO_INLINE
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            get => items.Count;
        }

        public bool HasItems { get; }

        public bool IsEmpty { get; }

#if !NO_INLINE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public Enumerator GetEnumerator() => new Enumerator(items);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)items).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();

        public struct Enumerator
        {
            private readonly List<T> values;

            private int index;

#if !NO_INLINE
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            public Enumerator(List<T> values)
            {
                this.values = values;
                index = -1;
            }

            public T Current
            {
#if !NO_INLINE
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
                get => values[index];
            }

#if !NO_INLINE
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            public bool MoveNext() => ++index < values.Count;
        }

        public class Comparer : IEqualityComparer<ListWrapper<T>>
        {
            public static readonly Comparer Default = new Comparer();

            public Comparer() =>
                ListComparer = ListComparer<T>.Default;

            public Comparer(IEqualityComparer<T> valueComparer) =>
                ListComparer = new ListComparer<T>(valueComparer);

            private ListComparer<T> ListComparer { get; }

            public bool Equals(ListWrapper<T> x, ListWrapper<T> y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }
                if (x == null || y == null)
                {
                    return false;
                }

                return ListComparer.Equals(x.items, y.items);
            }

            public int GetHashCode(ListWrapper<T> obj) => ListComparer.GetHashCode(obj.items);
        }
    }
}
