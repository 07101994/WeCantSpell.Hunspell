using System;
using System.Collections.Generic;

namespace WeCantSpell.Hunspell.Infrastructure
{
    sealed class StringDeduper
    {
        public StringDeduper()
            : this(StringComparer.Ordinal)
        {
        }

        public StringDeduper(IEqualityComparer<string> comparer)
        {
            lookup = new Dictionary<string, string>(comparer);
        }

        private readonly Dictionary<string, string> lookup;

        public string GetEqualOrAdd(string item)
        {
            if (string.IsNullOrEmpty(item))
            {
                return string.Empty;
            }

            if (lookup.TryGetValue(item, out string existing))
            {
                return existing;
            }
            else
            {
                lookup[item] = item;
                return item;
            }
        }
    }
}
