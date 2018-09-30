using System.Collections.Generic;
using System.Linq;
using WeCantSpell.Hunspell.Infrastructure;

namespace WeCantSpell.Hunspell
{
    public sealed class CompoundRule : ArrayWrapper<FlagValue>
    {
        public static readonly CompoundRule Empty = new CompoundRule(ArrayEx<FlagValue>.Empty);

        public static CompoundRule Create(List<FlagValue> values) => values == null ? Empty : TakeArray(values.ToArray());

        public static CompoundRule Create(IEnumerable<FlagValue> values) => values == null ? Empty : TakeArray(values.ToArray());

        internal static CompoundRule TakeArray(FlagValue[] values) => values == null ? Empty : new CompoundRule(values);

        private CompoundRule(FlagValue[] values)
            : base(values)
        {
        }

        internal bool IsValidWildcardAtIndex(int index) => index >= 0 && index < Count && this[index].IsWildcard;

        internal bool ContainsRuleFlagForEntry(WordEntryDetail details)
        {
            foreach (var flag in items)
            {
                if (!flag.IsWildcard && details.ContainsFlag(flag))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
