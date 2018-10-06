using System;
using System.Collections.Generic;
using System.Linq;
using WeCantSpell.Hunspell.Infrastructure;

namespace WeCantSpell.Hunspell
{
    public class PatternSet : ArrayWrapper<PatternEntry>
    {
        public static readonly PatternSet Empty = new PatternSet(ArrayEx<PatternEntry>.Empty);

        public static PatternSet Create(IEnumerable<PatternEntry> patterns) => TakeArray(patterns?.ToArray());

        internal static PatternSet TakeArray(PatternEntry[] patterns) =>
            patterns == null || patterns.Length == 0 ? Empty : new PatternSet(patterns);

        private PatternSet(PatternEntry[] patterns)
            : base(patterns)
        {
        }

        /// <summary>
        /// Forbid compoundings when there are special patterns at word bound.
        /// </summary>
        internal bool Check(ReadOnlySpan<char> word, int pos, WordEntry r1, WordEntry r2, bool affixed)
        {
#if DEBUG
            if (r1 == null) throw new ArgumentNullException(nameof(r1));
            if (r2 == null) throw new ArgumentNullException(nameof(r2));
            if (pos >= word.Length) throw new ArgumentOutOfRangeException(nameof(pos));
#endif

            var wordAfterPos = word.Slice(pos);

            foreach (var patternEntry in items)
            {
                if (
                    HunspellTextFunctions.IsSubset(patternEntry.Pattern2, wordAfterPos)
                    &&
                    (
                        patternEntry.Condition.IsZero
                        ||
                        r1.ContainsFlag(patternEntry.Condition)
                    )
                    &&
                    (
                        patternEntry.Condition2.IsZero
                        ||
                        r2.ContainsFlag(patternEntry.Condition2)
                    )
                )
                {
                    // zero length pattern => only TESTAFF
                    // zero pattern (0/flag) => unmodified stem (zero affixes allowed)

                    if (patternEntry.Pattern.Length == 0)
                    {
                        return true;
                    }

                    var other = patternEntry.Pattern[0] == '0' ? r1.Word : patternEntry.Pattern;
                    if (other.Length <= pos && word.Slice(pos - other.Length).StartsWith(other.AsSpan()))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
