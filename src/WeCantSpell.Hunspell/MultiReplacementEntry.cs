using WeCantSpell.Hunspell.Infrastructure;
using System;
using System.Collections.Generic;

namespace WeCantSpell.Hunspell
{
    public sealed class MultiReplacementEntry : ReplacementEntry
    {
        public MultiReplacementEntry(string pattern)
            : base(pattern)
        {
        }

        public MultiReplacementEntry(string pattern, ReplacementValueType type, string value)
            : base(pattern)
        {
            Set(type, value);
        }

        private string med;
        private string ini;
        private string fin;
        private string isol;

        public override string Med => med;

        public override string Ini => ini;

        public override string Fin => fin;

        public override string Isol => isol;

        public override string this[ReplacementValueType type]
        {
            get
            {
                switch (type)
                {
                    case ReplacementValueType.Med: return med;
                    case ReplacementValueType.Ini: return ini;
                    case ReplacementValueType.Fin: return fin;
                    case ReplacementValueType.Isol: return isol;
                    default: throw new ArgumentOutOfRangeException(nameof(type));
                }
            }
        }

        public MultiReplacementEntry With(ReplacementValueType type, string value)
        {
            var result = Clone();
            result.Set(type, value);
            return result;
        }

        internal void Set(ReplacementValueType type, string value)
        {
            switch (type)
            {
                case ReplacementValueType.Med:
                    med = value;
                    break;
                case ReplacementValueType.Ini:
                    ini = value;
                    break;
                case ReplacementValueType.Fin:
                    fin = value;
                    break;
                case ReplacementValueType.Isol:
                    isol = value;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        internal MultiReplacementEntry Clone() =>
            new MultiReplacementEntry(Pattern)
            {
                med = med,
                ini = ini,
                fin = fin,
                isol = isol
            };
    }

    static class MultiReplacementEntryExtensions
    {
        public static bool AddReplacementEntry(this Dictionary<string, MultiReplacementEntry> list, string pattern1, string pattern2)
        {
            if (string.IsNullOrEmpty(pattern1) || pattern2 == null)
            {
                return false;
            }

            var trailingUnderscore = pattern1.Length > 1 && pattern1.EndsWith('_');
            ReplacementValueType type;
            if (pattern1.StartsWith('_'))
            {
                if (trailingUnderscore)
                {
                    pattern1 = pattern1.Substring(1, pattern1.Length - 2).Replace('_', ' ');
                    type = ReplacementValueType.Isol;
                }
                else
                {
                    pattern1 = pattern1.Substring(1).Replace('_', ' ');
                    type = ReplacementValueType.Ini;
                }
            }
            else
            {
                if (trailingUnderscore)
                {
                    pattern1 = pattern1.Substring(0, pattern1.Length - 1).Replace('_', ' ');
                    type = ReplacementValueType.Fin;
                }
                else
                {
                    pattern1 = pattern1.Replace('_', ' ');
                    type = ReplacementValueType.Med;
                }
            }

            pattern2 = pattern2.Replace('_', ' ');

            // find existing entry
            if (list.TryGetValue(pattern1, out MultiReplacementEntry entry))
            {
                entry.Set(type, pattern2);
            }
            else
            {
                // make a new entry if none exists
                entry = new MultiReplacementEntry(pattern1, type, pattern2);
                list.Add(pattern1, entry);
            }

            return true;
        }
    }
}
