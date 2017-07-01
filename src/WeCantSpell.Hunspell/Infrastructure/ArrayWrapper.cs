﻿using System;
using System.Collections;
using System.Collections.Generic;

#if !NO_INLINE
using System.Runtime.CompilerServices;
#endif

namespace WeCantSpell.Hunspell.Infrastructure
{
    public class ArrayWrapper<T> :
#if NO_READONLYCOLLECTIONS
        IEnumerable<T>
#else
        IReadOnlyList<T>
#endif
    {
        protected ArrayWrapper(T[] items)
        {
            this.items = items ?? throw new ArgumentNullException(nameof(items));
            IsEmpty = items.Length == 0;
        }

        internal readonly T[] items;

        public bool IsEmpty
        {
#if !NO_INLINE
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            get;
        }

        public bool HasItems
        {
#if !NO_INLINE
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            get => !IsEmpty;
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
            get => items.Length;
        }

#if !NO_INLINE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public Enumerator GetEnumerator() => new Enumerator(items);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)items).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();

        public struct Enumerator
        {
            private readonly T[] values;

            private int index;

#if !NO_INLINE
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            public Enumerator(T[] values)
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
            public bool MoveNext() => (++index) < values.Length;
        }

        public class ArrayWrapperComparer<TValue, TCollection> :
            IEqualityComparer<TCollection>
            where TCollection : ArrayWrapper<TValue>
        {
            public ArrayWrapperComparer() =>
                arrayComparer = ArrayComparer<TValue>.Default;

            private ArrayComparer<TValue> arrayComparer;

#if !NO_INLINE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            public bool Equals(TCollection x, TCollection y) =>
                arrayComparer.Equals(x.items, y.items);

#if !NO_INLINE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            public int GetHashCode(TCollection obj) =>
                arrayComparer.GetHashCode(obj.items);
        }
    }
}
