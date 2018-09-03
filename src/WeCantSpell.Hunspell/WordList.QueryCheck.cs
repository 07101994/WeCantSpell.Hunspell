using System;
using System.Linq;
using WeCantSpell.Hunspell.Infrastructure;

namespace WeCantSpell.Hunspell
{
    public partial class WordList
    {
        private sealed class QueryCheck : Query
        {
            public QueryCheck(WordList wordList)
                : base(wordList)
            {
            }

            public SpellCheckResult CheckDetails(ReadOnlyMemory<char> word)
            {
                if (word.IsEmpty || word.Length >= MaxWordUtf8Len || !WordList.HasEntries)
                {
                    return new SpellCheckResult(false);
                }
                if (word.Span.EqualsOrdinal(DefaultXmlToken.AsSpan()))
                {
                    // Hunspell supports XML input of the simplified API (see manual)
                    return new SpellCheckResult(true);
                }

                // input conversion
                if (!Affix.InputConversions.HasReplacements || !Affix.InputConversions.TryConvert(word, out ReadOnlyMemory<char> convertedWord))
                {
                    convertedWord = word;
                }

                var scw = CleanWord2(convertedWord, out CapitalizationType capType, out int abbv);
                if (scw.IsEmpty)
                {
                    return new SpellCheckResult(false);
                }

                if (HunspellTextFunctions.IsNumericWord(word.Span))
                {
                    // allow numbers with dots, dashes and commas (but forbid double separators: "..", "--" etc.)
                    return new SpellCheckResult(true);
                }

                var resultType = SpellCheckResultType.None;
                string root = null;
                WordEntry rv = null;

                if (capType == CapitalizationType.Huh || capType == CapitalizationType.HuhInit || capType == CapitalizationType.None)
                {
                    if (capType == CapitalizationType.HuhInit)
                    {
                        resultType |= SpellCheckResultType.OrigCap;
                    }

                    rv = CheckWord(scw, ref resultType, out root);
                    if (abbv != 0 && rv == null)
                    {
                        rv = CheckWord(scw.Span.ConcatString('.'), ref resultType, out root);
                    }
                }
                else if (capType == CapitalizationType.All)
                {
                    rv = CheckDetailsAllCap(abbv, ref scw, ref resultType, out root);
                }

                if (capType == CapitalizationType.Init || (capType == CapitalizationType.All && rv == null))
                {
                    rv = CheckDetailsInitCap(abbv, capType, ref scw, ref resultType, out root);
                }

                if (rv != null)
                {
                    if (rv.ContainsFlag(Affix.Warn))
                    {
                        resultType |= SpellCheckResultType.Warn;

                        if (Affix.ForbidWarn)
                        {
                            return new SpellCheckResult(root, resultType, false);
                        }
                    }

                    return new SpellCheckResult(root, resultType, true);
                }

                // recursive breaking at break points
                if (Affix.BreakPoints.HasItems && !EnumEx.HasFlag(resultType, SpellCheckResultType.Forbidden))
                {
                    // calculate break points for recursion limit
                    if (Affix.BreakPoints.FindRecursionLimit(scw.Span) >= 10)
                    {
                        return new SpellCheckResult(root, resultType, false);
                    }

                    // check boundary patterns (^begin and end$)
                    foreach (var breakEntry in Affix.BreakPoints)
                    {
                        if (breakEntry.Length == 1 || breakEntry.Length > scw.Length)
                        {
                            continue;
                        }

                        var pLastIndex = breakEntry.Length - 1;
                        if (
                            breakEntry.StartsWith('^')
                            && scw.Span.Limit(pLastIndex).EqualsOrdinal(breakEntry.AsSpan(1))
                            && Check(scw.Slice(pLastIndex))
                        )
                        {
                            return new SpellCheckResult(root, resultType, true);
                        }

                        if (breakEntry.EndsWith('$'))
                        {
                            var wlLessBreakIndex = scw.Length - breakEntry.Length + 1;
                            if (
                                scw.Span.Slice(wlLessBreakIndex).Limit(pLastIndex).EqualsOrdinal(breakEntry.AsSpan().Limit(pLastIndex))
                                && Check(scw.Slice(0, wlLessBreakIndex))
                            )
                            {
                                return new SpellCheckResult(root, resultType, true);
                            }
                        }
                    }

                    // other patterns
                    foreach (var breakEntry in Affix.BreakPoints)
                    {
                        var found = scw.Span.IndexOf(breakEntry.AsSpan());
                        var remainingLength = scw.Length - breakEntry.Length;
                        if (found > 0 && found < remainingLength)
                        {
                            var found2 = scw.Span.IndexOf(breakEntry.AsSpan(), found + 1);
                            // try to break at the second occurance
                            // to recognize dictionary words with wordbreak
                            if (found2 > 0 && (found2 < remainingLength))
                            {
                                found = found2;
                            }

                            if (!Check(scw.Slice(found + breakEntry.Length)))
                            {
                                continue;
                            }

                            // examine 2 sides of the break point
                            if (Check(scw.Slice(0, found)))
                            {
                                return new SpellCheckResult(root, resultType, true);
                            }

                            // LANG_hu: spec. dash rule
                            if (Affix.IsHungarian && "-".Equals(breakEntry, StringComparison.Ordinal))
                            {
                                if (Check(scw.Slice(0, found + 1)))
                                {
                                    return new SpellCheckResult(root, resultType, true);
                                }
                            }
                        }
                    }

                    // other patterns (break at first break point)
                    foreach (var breakEntry in Affix.BreakPoints)
                    {
                        var found = scw.Span.IndexOf(breakEntry.AsSpan());
                        var remainingLength = scw.Length - breakEntry.Length;
                        if (found > 0 && found < remainingLength)
                        {
                            if (!Check(scw.Slice(found + breakEntry.Length)))
                            {
                                continue;
                            }

                            // examine 2 sides of the break point
                            if (Check(scw.Slice(0, found)))
                            {
                                return new SpellCheckResult(root, resultType, true);
                            }

                            // LANG_hu: spec. dash rule
                            if (Affix.IsHungarian && "-".Equals(breakEntry, StringComparison.Ordinal))
                            {
                                if (Check(scw.Slice(0, found + 1)))
                                {
                                    return new SpellCheckResult(root, resultType, true);
                                }
                            }
                        }
                    }
                }

                return new SpellCheckResult(root, resultType, false);
            }

            private WordEntry CheckDetailsAllCap(int abbv, ref ReadOnlyMemory<char> scw, ref SpellCheckResultType resultType, out string root)
            {
                resultType |= SpellCheckResultType.OrigCap;
                var rv = CheckWord(scw, ref resultType, out root);
                if (rv != null)
                {
                    return rv;
                }

                if (abbv != 0)
                {
                    rv = CheckWord(scw.Span.ConcatString('.'), ref resultType, out root);
                    if (rv != null)
                    {
                        return rv;
                    }
                }

                // Spec. prefix handling for Catalan, French, Italian:
                // prefixes separated by apostrophe (SANT'ELIA -> Sant'+Elia).
                var textInfo = TextInfo;
                var apos = scw.Span.IndexOf('\'');
                if (apos >= 0)
                {
                    scw = HunspellTextFunctions.MakeAllSmall(scw, Affix.Culture);

                    // conversion may result in string with different len than before MakeAllSmall2 so re-scan
                    if (apos < scw.Length - 1)
                    {
                        scw = scw.Slice(0, apos + 1).Span.ConcatString(HunspellTextFunctions.MakeInitCap(scw.Slice(apos + 1).Span, textInfo)).AsMemory();
                        rv = CheckWord(scw, ref resultType, out root);
                        if (rv != null)
                        {
                            return rv;
                        }

                        scw = HunspellTextFunctions.MakeInitCap(scw.Span, textInfo).AsMemory();
                        rv = CheckWord(scw, ref resultType, out root);
                        if (rv != null)
                        {
                            return rv;
                        }
                    }
                }

                if (Affix.CheckSharps && scw.Span.Contains("SS".AsSpan()))
                {
                    scw = HunspellTextFunctions.MakeAllSmall(scw, Affix.Culture);
                    var u8buffer = scw;
                    rv = SpellSharps(ref u8buffer, ref resultType, out root);
                    if (rv == null)
                    {
                        scw = HunspellTextFunctions.MakeInitCap(scw, textInfo);
                        rv = SpellSharps(ref scw, ref resultType, out root);
                    }

                    if (abbv != 0 && rv == null)
                    {
                        u8buffer = u8buffer.Span.ConcatString('.').AsMemory();
                        rv = SpellSharps(ref u8buffer, ref resultType, out root);
                        if (rv == null)
                        {
                            u8buffer = scw.Span.ConcatString('.').AsMemory();
                            rv = SpellSharps(ref u8buffer, ref resultType, out root);
                        }
                    }
                }

                return rv;
            }

            private WordEntry CheckDetailsInitCap(int abbv, CapitalizationType capType, ref ReadOnlyMemory<char> scw, ref SpellCheckResultType resultType, out string root)
            {
                ReadOnlyMemory<char> u8buffer = HunspellTextFunctions.MakeAllSmall(scw, Affix.Culture);
                scw = HunspellTextFunctions.MakeInitCap(u8buffer, TextInfo);

                resultType |= SpellCheckResultType.OrigCap;
                if (capType == CapitalizationType.Init)
                {
                    resultType |= SpellCheckResultType.InitCap;
                }

                var rv = CheckWord(scw, ref resultType, out root);

                if (capType == CapitalizationType.Init)
                {
                    resultType &= ~SpellCheckResultType.InitCap;
                }

                // forbid bad capitalization
                // (for example, ijs -> Ijs instead of IJs in Dutch)
                // use explicit forms in dic: Ijs/F (F = FORBIDDENWORD flag)

                if (EnumEx.HasFlag(resultType, SpellCheckResultType.Forbidden))
                {
                    rv = null;
                    return rv;
                }

                if (capType == CapitalizationType.All && rv != null && IsKeepCase(rv))
                {
                    rv = null;
                }

                if (rv != null || (!Affix.CultureUsesDottedI && scw.Span.StartsWith('İ')))
                {
                    return rv;
                }

                rv = CheckWord(u8buffer, ref resultType, out root);

                if (abbv != 0 && rv == null)
                {
                    u8buffer = u8buffer.Span.ConcatString('.').AsMemory();
                    rv = CheckWord(u8buffer, ref resultType, out root);
                    if (rv == null)
                    {
                        u8buffer = scw.Span.ConcatString('.').AsMemory();
                        if (capType == CapitalizationType.Init)
                        {
                            resultType |= SpellCheckResultType.InitCap;
                        }

                        rv = CheckWord(u8buffer, ref resultType, out root);

                        if (capType == CapitalizationType.Init)
                        {
                            resultType &= ~SpellCheckResultType.InitCap;
                        }

                        if (capType == CapitalizationType.All && rv != null && IsKeepCase(rv))
                        {
                            rv = null;
                        }

                        return rv;
                    }
                }

                if (
                    rv != null
                    &&
                    IsKeepCase(rv)
                    &&
                    (
                        capType == CapitalizationType.All
                        ||
                        // if CHECKSHARPS: KEEPCASE words with \xDF  are allowed in INITCAP form, too.
                        !(Affix.CheckSharps && u8buffer.Span.Contains('ß'))
                    )
                )
                {
                    rv = null;
                }

                return rv;
            }

            /// <summary>
            /// Recursive search for right ss - sharp s permutations
            /// </summary>
            private WordEntry SpellSharps(ref ReadOnlyMemory<char> @base, ref SpellCheckResultType info, out string root) =>
                SpellSharps(ref @base, 0, 0, 0, ref info, out root);

            /// <summary>
            /// Recursive search for right ss - sharp s permutations
            /// </summary>
            private WordEntry SpellSharps(ref ReadOnlyMemory<char> @base, int nPos, int n, int repNum, ref SpellCheckResultType info, out string root)
            {
                var pos = @base.Span.IndexOf("ss".AsSpan(), nPos);
                if (pos >= 0 && n < MaxSharps)
                {
                    var baseBuilder = StringBuilderPool.Get(@base);
                    baseBuilder[pos] = 'ß';
                    baseBuilder.Remove(pos + 1, 1);
                    @base = baseBuilder.ToString().AsMemory();

                    var h = SpellSharps(ref @base, pos + 1, n + 1, repNum + 1, ref info, out root);
                    if (h != null)
                    {
                        return h;
                    }

                    baseBuilder.Clear();
                    baseBuilder.Append(@base);
                    baseBuilder[pos] = 's';
                    baseBuilder.Insert(pos + 1, 's');
                    @base = StringBuilderPool.GetStringAndReturn(baseBuilder).AsMemory();

                    h = SpellSharps(ref @base, pos + 2, n + 1, repNum, ref info, out root);
                    if (h != null)
                    {
                        return h;
                    }
                }
                else if (repNum > 0)
                {
                    return CheckWord(@base, ref info, out root);
                }

                root = null;
                return null;
            }

            private bool IsKeepCase(WordEntry rv) => rv.ContainsFlag(Affix.KeepCase);
        }
    }
}
