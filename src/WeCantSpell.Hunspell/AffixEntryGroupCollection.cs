using System.Collections.Generic;
using System.Linq;
using WeCantSpell.Hunspell.Infrastructure;

namespace WeCantSpell.Hunspell
{
    public sealed class AffixEntryGroupCollection<TEntry> : ArrayWrapper<AffixEntryGroup<TEntry>>
        where TEntry : AffixEntry
    {
        public static readonly AffixEntryGroupCollection<TEntry> Empty = new AffixEntryGroupCollection<TEntry>(ArrayEx<AffixEntryGroup<TEntry>>.Empty);

        public static AffixEntryGroupCollection<TEntry> Create(IEnumerable<AffixEntryGroup<TEntry>> entries) => TakeArray(entries?.ToArray());

        internal static AffixEntryGroupCollection<TEntry> TakeArray(AffixEntryGroup<TEntry>[] entries) =>
            entries == null || entries.Length == 0 ? Empty : new AffixEntryGroupCollection<TEntry>(entries);

        private AffixEntryGroupCollection(AffixEntryGroup<TEntry>[] entries) : base(entries)
        {
        }
    }
}
