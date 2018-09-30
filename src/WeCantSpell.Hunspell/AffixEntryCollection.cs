using System.Collections.Generic;
using System.Linq;
using WeCantSpell.Hunspell.Infrastructure;

namespace WeCantSpell.Hunspell
{
    public sealed class AffixEntryCollection<TEntry> : ArrayWrapper<TEntry>
        where TEntry : AffixEntry
    {
        public static readonly AffixEntryCollection<TEntry> Empty = new AffixEntryCollection<TEntry>(ArrayEx<TEntry>.Empty);

        public static AffixEntryCollection<TEntry> Create(List<TEntry> entries) => TakeArray(entries?.ToArray());

        public static AffixEntryCollection<TEntry> Create(IEnumerable<TEntry> entries) => TakeArray(entries?.ToArray());

        internal static AffixEntryCollection<TEntry> TakeArray(TEntry[] entries) =>
            entries == null ? Empty : new AffixEntryCollection<TEntry>(entries);

        private AffixEntryCollection(TEntry[] entries) : base(entries)
        {
        }
    }
}
