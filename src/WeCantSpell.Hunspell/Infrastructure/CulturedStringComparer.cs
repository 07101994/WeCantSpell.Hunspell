using System;
using System.Globalization;

namespace WeCantSpell.Hunspell.Infrastructure
{
    /// <summary>
    /// Provides the ability to compare text using a configured culture.
    /// </summary>
    sealed class CulturedStringComparer : StringComparer
    {
        public CulturedStringComparer(CultureInfo culture)
        {
            compareInfo = culture.CompareInfo;
        }

        private readonly CompareInfo compareInfo;

        public override int Compare(string x, string y) => compareInfo.Compare(x, y);

        public override bool Equals(string x, string y) => Compare(x, y) == 0;

        public override int GetHashCode(string obj)
        {
#if NO_COMPAREINFO_HASHCODE
            return 0;
#else
            return compareInfo.GetHashCode(obj, CompareOptions.None);
#endif
        }
    }
}
