using System.Collections.Generic;
using System.Linq;
using WeCantSpell.Hunspell.Infrastructure;

namespace WeCantSpell.Hunspell
{
    public sealed class MapEntry : ArrayWrapper<string>
    {
        public static readonly MapEntry Empty = new MapEntry(ArrayEx<string>.Empty);

        public static MapEntry Create(IEnumerable<string> values) => TakeArray(values?.ToArray());

        internal static MapEntry TakeArray(string[] values) => values == null || values.Length == 0 ? Empty : new MapEntry(values);

        private MapEntry(string[] values)
            : base(values)
        {
        }
    }
}
