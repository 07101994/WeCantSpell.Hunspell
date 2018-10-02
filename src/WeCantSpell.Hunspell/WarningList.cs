using System.Collections.Generic;
using System.Linq;
using WeCantSpell.Hunspell.Infrastructure;

namespace WeCantSpell.Hunspell
{
    public sealed class WarningList : ArrayWrapper<string>
    {
        public static WarningList Create(IEnumerable<string> warnings) => new WarningList(warnings?.ToArray() ?? ArrayEx<string>.Empty);

        private WarningList(string[] warnings)
            : base(warnings)
        {
        }
    }
}
