using System.Collections.Generic;
using System.Linq;
using WeCantSpell.Hunspell.Infrastructure;

namespace WeCantSpell.Hunspell
{
    public sealed class CompoundRuleSet : ArrayWrapper<CompoundRule>
    {
        public static readonly CompoundRuleSet Empty = TakeArray(ArrayEx<CompoundRule>.Empty);

        public static CompoundRuleSet Create(IEnumerable<CompoundRule> rules) =>
            rules == null ? Empty : TakeArray(rules.ToArray());

        internal static CompoundRuleSet TakeArray(CompoundRule[] rules) =>
            rules == null ? Empty : new CompoundRuleSet(rules);

        private CompoundRuleSet(CompoundRule[] rules)
            : base(rules)
        {
        }

        internal bool EntryContainsRuleFlags(WordEntryDetail details)
        {
            if (details != null && details.HasFlags)
            {
                foreach(var rule in items)
                {
                    if (rule.ContainsRuleFlagForEntry(details))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal bool CompoundCheck(IncrementalWordList words, bool all)
        {
            var bt = 0;
            var btinfo = new List<MetacharData>
            {
                new MetacharData()
            };

            MetacharData currentBt;

            foreach (var compoundRule in items)
            {
                var pp = 0; // pattern position
                var wp = 0; // "words" position
                var ok = true;
                var ok2 = true;
                do
                {
                    while (pp < compoundRule.Count && wp <= words.WNum)
                    {
                        if (compoundRule.IsValidWildcardAtIndex(pp + 1))
                        {
                            var wend = compoundRule[pp + 1] == '?' ? wp : words.WNum;
                            ok2 = true;
                            pp += 2;
                            currentBt = btinfo[bt];
                            currentBt.btpp = pp;
                            currentBt.btwp = wp;

                            while (wp <= wend)
                            {
                                if (!words.ContainsFlagAt(wp, compoundRule[pp - 2]))
                                {
                                    ok2 = false;
                                    break;
                                }

                                wp++;
                            }

                            if (wp <= words.WNum)
                            {
                                ok2 = false;
                            }

                            currentBt.btnum = wp - currentBt.btwp;

                            if (currentBt.btnum > 0)
                            {
                                bt++;
                                btinfo.Add(new MetacharData());
                            }
                            if (ok2)
                            {
                                break;
                            }
                        }
                        else
                        {
                            ok2 = true;
                            if (!words.ContainsFlagAt(wp, compoundRule[pp]))
                            {
                                ok = false;
                                break;
                            }

                            pp++;
                            wp++;

                            if (compoundRule.Count <= pp && wp <= words.WNum)
                            {
                                ok = false;
                            }
                        }
                    }

                    if (ok && ok2)
                    {
                        var r = pp;
                        while (compoundRule.IsValidWildcardAtIndex(r + 1))
                        {
                            r += 2;
                        }

                        if (compoundRule.Count <= r)
                        {
                            return true;
                        }
                    }

                    // backtrack
                    if (bt > 0)
                    {
                        ok = true;

                        do
                        {
                            currentBt = btinfo[bt - 1];
                            currentBt.btnum--;
                        }
                        while ((currentBt.btnum < 0) && (--bt != 0));

                        pp = currentBt.btpp;
                        wp = currentBt.btwp + currentBt.btnum;
                    }

                }
                while (bt > 0);

                if (ok && ok2)
                {
                    if (!all || compoundRule.Count <= pp)
                    {
                        return true;
                    }

                    // check zero ending
                    while (compoundRule.IsValidWildcardAtIndex(pp + 1))
                    {
                        pp += 2;
                    }

                    if (compoundRule.Count <= pp)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private class MetacharData
        {
            /// <summary>
            /// Metacharacter (*, ?) position for backtracking.
            /// </summary>
            public int btpp;
            /// <summary>
            /// Word position for metacharacters.
            /// </summary>
            public int btwp;
            /// <summary>
            /// Number of matched characters in metacharacter.
            /// </summary>
            public int btnum;
        }
    }
}
