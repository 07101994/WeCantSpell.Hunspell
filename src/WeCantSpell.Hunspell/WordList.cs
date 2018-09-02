using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WeCantSpell.Hunspell.Infrastructure;

#if !NO_INLINE
using System.Runtime.CompilerServices;
#endif

namespace WeCantSpell.Hunspell
{
    public sealed partial class WordList
    {
        internal const int MaxWordLen = 100;

        public static WordList CreateFromStreams(Stream dictionaryStream, Stream affixStream) =>
            WordListReader.Read(dictionaryStream, affixStream);

        public static WordList CreateFromFiles(string dictionaryFilePath) =>
            WordListReader.ReadFile(dictionaryFilePath);

        public static WordList CreateFromFiles(string dictionaryFilePath, string affixFilePath) =>
            WordListReader.ReadFile(dictionaryFilePath, affixFilePath);

        public static async Task<WordList> CreateFromStreamsAsync(Stream dictionaryStream, Stream affixStream) =>
            await WordListReader.ReadAsync(dictionaryStream, affixStream).ConfigureAwait(false);

        public static async Task<WordList> CreateFromFilesAsync(string dictionaryFilePath) =>
            await WordListReader.ReadFileAsync(dictionaryFilePath).ConfigureAwait(false);

        public static async Task<WordList> CreateFromFilesAsync(string dictionaryFilePath, string affixFilePath) =>
            await WordListReader.ReadFileAsync(dictionaryFilePath, affixFilePath).ConfigureAwait(false);

        public static WordList CreateFromWords(IEnumerable<string> words) =>
            CreateFromWords(words, affix: null);

        public static WordList CreateFromWords(IEnumerable<string> words, AffixConfig affix)
        {
            if (words == null)
            {
                words = Enumerable.Empty<string>();
            }

            var wordListBuilder = new Builder(affix ?? new AffixConfig.Builder().MoveToImmutable());

            if (words is IList<string> wordsAsList)
            {
                wordListBuilder.InitializeEntriesByRoot(wordsAsList.Count);
            }
            else
            {
                wordListBuilder.InitializeEntriesByRoot(-1);
            }

            var entryDetail = WordEntryDetail.Default;

            foreach (var word in words)
            {
                wordListBuilder.Add(word, entryDetail);
            }

            return wordListBuilder.MoveToImmutable();
        }

        private WordList(AffixConfig affix)
        {
            Affix = affix;
        }

        public AffixConfig Affix { get; private set; }

        public SingleReplacementSet AllReplacements { get; private set; }

        public IEnumerable<string> RootWords => EntriesByRoot.Keys.Select(k => k.ToString());

        public bool HasEntries => !EntriesByRoot.IsEmpty;

        public bool ContainsEntriesForRootWord(ReadOnlySpan<char> rootWord) =>
            EntriesByRoot.ContainsKey(rootWord);

        public WordEntryDetail[] this[string rootWord] =>
            (rootWord != null)
                ? (WordEntryDetail[])FindEntryDetailsByRootWord(rootWord).Clone()
                : ArrayEx<WordEntryDetail>.Empty;

        private StringTrie<WordEntryDetail[]> EntriesByRoot { get; set; }

        private FlagSet NGramRestrictedFlags { get; set; }

        private IEnumerable<KeyValuePair<ReadOnlyMemory<char>, WordEntryDetail[]>> GetNGramAllowedDetails(Func<ReadOnlyMemory<char>, bool> rootFilter, int maxDepth) =>
            (NGramRestrictedDetails == null || NGramRestrictedDetails.Count == 0)
            ? GetAllEntriesByRoot(rootFilter, maxDepth)
            : GetAllNGramAllowedEntries(rootFilter, maxDepth);

        private Dictionary<string, WordEntryDetail[]> NGramRestrictedDetails { get; set; }

        public bool Check(string word) => new QueryCheck(this).Check(word);

        public SpellCheckResult CheckDetails(ReadOnlySpan<char> word) => new QueryCheck(this).CheckDetails(word);

        public SpellCheckResult CheckDetails(string word) => CheckDetails(word.AsSpan());

        public IEnumerable<string> Suggest(string word) => new QuerySuggest(this).Suggest(word);

        internal WordEntry FindFirstEntryByRootWord(string rootWord)
        {
#if DEBUG
            if (rootWord == null) throw new ArgumentNullException(nameof(rootWord));
#endif
            var details = FindEntryDetailsByRootWord(rootWord);
            return details.Length == 0
                ? null
                : new WordEntry(rootWord, details[0]);
        }

        internal WordEntry FindFirstEntryByRootWord(ReadOnlySpan<char> rootWord)
        {
            var details = FindEntryDetailsByRootWord(rootWord);
            return details.Length == 0
                ? null
                : details[0].ToEntry(rootWord.ToString());
        }

        internal WordEntryDetail[] FindEntryDetailsByRootWord(string rootWord)
        {
            return (rootWord == null || !EntriesByRoot.TryGetValue(rootWord, out WordEntryDetail[] details))
                ? ArrayEx<WordEntryDetail>.Empty
                : details;
        }

        internal WordEntryDetail[] FindEntryDetailsByRootWord(ReadOnlySpan<char> rootWord)
        {
            return !EntriesByRoot.TryGetValue(rootWord, out WordEntryDetail[] details)
                ? ArrayEx<WordEntryDetail>.Empty
                : details;
        }

        internal WordEntryDetail FindFirstEntryDetailByRootWord(string rootWord)
        {
#if DEBUG
            if (rootWord == null) throw new ArgumentNullException(nameof(rootWord));
#endif

            return EntriesByRoot.TryGetValue(rootWord, out WordEntryDetail[] details) && details.Length != 0
                ? details[0]
                : null;
        }

        internal WordEntryDetail FindFirstEntryDetailByRootWord(ReadOnlySpan<char> rootWord)
        {
            return EntriesByRoot.TryGetValue(rootWord, out WordEntryDetail[] details) && details.Length != 0
                ? details[0]
                : null;
        }

        private IEnumerable<KeyValuePair<ReadOnlyMemory<char>, WordEntryDetail[]>> GetAllEntriesByRoot(Func<ReadOnlyMemory<char>, bool> rootFilter, int maxDepth)
        {
            using (var entriesEnumerator = EntriesByRoot.GetEnumerator(maxDepth))
            {
                while (entriesEnumerator.MoveNext())
                {
                    var rootPair = entriesEnumerator.Current;
                    if (rootFilter(rootPair.Key))
                    {
                        yield return rootPair;
                    }
                }
            }
        }

        private IEnumerable<KeyValuePair<ReadOnlyMemory<char>, WordEntryDetail[]>> GetAllNGramAllowedEntries(Func<ReadOnlyMemory<char>, bool> rootFilter, int maxDepth)
        {
            using (var entriesEnumerator = EntriesByRoot.GetEnumerator(maxDepth))
            {
                while (entriesEnumerator.MoveNext())
                {
                    var rootPair = entriesEnumerator.Current;
                    if (rootFilter(rootPair.Key))
                    {
                        if (NGramRestrictedDetails.TryGetValue(rootPair.Key.ToString(), out WordEntryDetail[] restrictedDetails))
                        {
                            WordEntryDetail[] filteredValues;
                            if (restrictedDetails.Length == 0)
                            {
                                filteredValues = rootPair.Value;
                            }
                            else if (restrictedDetails.Length == rootPair.Value.Length)
                            {
                                filteredValues = ArrayEx<WordEntryDetail>.Empty;
                            }
                            else
                            {
                                filteredValues = rootPair.Value.Where(d => !restrictedDetails.Contains(d)).ToArray();
                            }

                            yield return new KeyValuePair<ReadOnlyMemory<char>, WordEntryDetail[]>(rootPair.Key, filteredValues);
                        }
                        else
                        {
                            yield return rootPair;
                        }
                    }
                }
            }
        }
    }
}
