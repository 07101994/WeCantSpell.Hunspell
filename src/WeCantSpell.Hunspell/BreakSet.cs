using System;
using System.Collections.Generic;
using System.Linq;
using WeCantSpell.Hunspell.Infrastructure;

namespace WeCantSpell.Hunspell
{
    public sealed class BreakSet : ArrayWrapper<string>
    {
        public static readonly BreakSet Empty = new BreakSet(ArrayEx<string>.Empty);

        public static BreakSet Create(List<string> breaks) => TakeArray(breaks?.ToArray());

        public static BreakSet Create(IEnumerable<string> breaks) => TakeArray(breaks?.ToArray());

        internal static BreakSet TakeArray(string[] breaks) => breaks == null || breaks.Length == 0 ? Empty : new BreakSet(breaks);

        private BreakSet(string[] breaks)
            : base(breaks)
        {
        }

        /// <summary>
        /// Test break points for a recursion limit.
        /// </summary>
        internal bool TestRecursionLimit(string scw, int maxRecursionLimit)
        {
            int nbr = 0;

            if (!string.IsNullOrEmpty(scw))
            {
                foreach (var breakEntry in items)
                {
                    if (string.IsNullOrEmpty(breakEntry))
                    {
                        continue;
                    }

                    int pos = scw.IndexOf(breakEntry, StringComparison.Ordinal);
                    while (pos >= 0)
                    {
                        nbr++;

                        if (nbr >= maxRecursionLimit)
                        {
                            return true;
                        }

                        pos = scw.IndexOf(breakEntry, pos + breakEntry.Length, StringComparison.Ordinal);
                    }
                }
            }

            return false;
        }
    }
}
