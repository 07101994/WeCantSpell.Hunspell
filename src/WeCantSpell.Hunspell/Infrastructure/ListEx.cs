using System;
using System.Collections.Generic;

namespace WeCantSpell.Hunspell.Infrastructure
{
    static class ListEx
    {
        public static T Dequeue<T>(this List<T> @this)
        {
#if DEBUG
            if (@this == null) throw new ArgumentNullException(nameof(@this));
            if (@this.Count == 0) throw new ArgumentOutOfRangeException(nameof(@this));
#endif

            var lastIndex = @this.Count - 1;
            var item = @this[lastIndex];
            @this.RemoveAt(lastIndex);
            return item;
        }
    }
}
