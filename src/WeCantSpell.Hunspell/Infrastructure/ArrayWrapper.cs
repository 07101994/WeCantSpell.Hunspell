using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace WeCantSpell.Hunspell.Infrastructure
{
    public abstract class ArrayWrapper<T> : IReadOnlyList<T>
    {
        protected ArrayWrapper(T[] items)
        {
            this.items = items ?? throw new ArgumentNullException(nameof(items));
            IsEmpty = items.Length == 0;
        }

        internal readonly T[] items;

        public bool IsEmpty { get; }

        public bool HasItems => !IsEmpty;

        public ref readonly T this[int index] => ref items[index];

        public int Count => items.Length;

        T IReadOnlyList<T>.this[int index] => items[index];

        public ReadOnlySpan<T>.Enumerator GetEnumerator() => new ReadOnlySpan<T>(items).GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => items.AsEnumerable().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();
    }
}
