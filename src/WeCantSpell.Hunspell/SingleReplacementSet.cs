using System.Collections.Generic;
using System.Linq;
using WeCantSpell.Hunspell.Infrastructure;

namespace WeCantSpell.Hunspell
{
    public sealed class SingleReplacementSet : ArrayWrapper<SingleReplacement>
    {
        public static readonly SingleReplacementSet Empty = new SingleReplacementSet(ArrayEx<SingleReplacement>.Empty);

        public static SingleReplacementSet Create(IEnumerable<SingleReplacement> replacements) => TakeArray(replacements?.ToArray());

        internal static SingleReplacementSet TakeArray(SingleReplacement[] replacements) =>
            replacements == null || replacements.Length == 0 ? Empty : new SingleReplacementSet(replacements);

        private SingleReplacementSet(SingleReplacement[] replacements)
            : base(replacements)
        {
        }
    }
}
