using System.Collections.Generic;
using System.Linq;
using WeCantSpell.Hunspell.Infrastructure;

namespace WeCantSpell.Hunspell
{
    public sealed class PhoneTable : ArrayWrapper<PhoneticEntry>
    {
        public static readonly PhoneTable Empty = new PhoneTable(ArrayEx<PhoneticEntry>.Empty);

        public static PhoneTable Create(IEnumerable<PhoneticEntry> entries) => TakeArray(entries?.ToArray());

        internal static PhoneTable TakeArray(PhoneticEntry[] entries) => entries == null || entries.Length == 0 ? Empty : new PhoneTable(entries);

        private PhoneTable(PhoneticEntry[] entries)
            : base(entries)
        {
        }
    }
}
