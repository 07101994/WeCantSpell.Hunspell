﻿using System;
using System.Collections.Generic;
using System.IO;
using WeCantSpell.Hunspell.Infrastructure;
using System.Linq;

#if !NO_INLINE
using System.Runtime.CompilerServices;
#endif

#if !NO_ASYNC
using System.Threading;
using System.Threading.Tasks;
#endif

namespace WeCantSpell.Hunspell
{
    public sealed partial class WordList
    {
        internal const int MaxWordLen = 100;

        public static WordList CreateFromStreams(Stream dictionaryStream, Stream affixStream) =>
            WordListReader.Read(dictionaryStream, affixStream);

#if !NO_IO_FILE
        public static WordList CreateFromFiles(string dictionaryFilePath) =>
            WordListReader.ReadFile(dictionaryFilePath);

        public static WordList CreateFromFiles(string dictionaryFilePath, string affixFilePath) =>
            WordListReader.ReadFile(dictionaryFilePath, affixFilePath);
#endif

#if !NO_ASYNC
        public static async Task<WordList> CreateFromStreamsAsync(Stream dictionaryStream, Stream affixStream) =>
            await WordListReader.ReadAsync(dictionaryStream, affixStream).ConfigureAwait(false);

#if !NO_IO_FILE
        public static async Task<WordList> CreateFromFilesAsync(string dictionaryFilePath) =>
            await WordListReader.ReadFileAsync(dictionaryFilePath).ConfigureAwait(false);

        public static async Task<WordList> CreateFromFilesAsync(string dictionaryFilePath, string affixFilePath) =>
            await WordListReader.ReadFileAsync(dictionaryFilePath, affixFilePath).ConfigureAwait(false);
#endif

#endif

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
                wordListBuilder.InitializeEntriesByRoot(0);
            }

            var entryDetail = WordEntryDetail.Default;

            foreach (var word in words)
            {
                if (!wordListBuilder.EntryDetailsByRoot.TryGetValue(word, out List<WordEntryDetail> entryDetails) || entryDetails == null)
                {
                    wordListBuilder.EntryDetailsByRoot.Add(word, entryDetails = new List<WordEntryDetail>());
                }

                entryDetails.Add(entryDetail);
            }

            return wordListBuilder.MoveToImmutable();
        }

        private WordList(AffixConfig affix) =>
            Affix = affix;

        public AffixConfig Affix { get; private set; }

        private Dictionary<string, WordEntryDetail[]> EntriesByRoot { get; set; }

        private FlagSet NGramRestrictedFlags { get; set; }

        private Dictionary<string, WordEntryDetail[]> NGramRestrictedEntries { get; set; }

        public IEnumerable<WordEntry> NGramAllowedEntries =>
            (NGramRestrictedEntries == null || NGramRestrictedEntries.Count == 0)
            ? AllEntries
            : GetAllNGramAllowedEntries();

        private IEnumerable<WordEntry> GetAllNGramAllowedEntries() =>
            EntriesByRoot.SelectMany(rootPair =>
            {
                if (NGramRestrictedEntries.TryGetValue(rootPair.Key, out WordEntryDetail[] restrictedDetails))
                {
                    return rootPair.Value
                        .Where(d => !restrictedDetails.Contains(d))
                        .Select(d => new WordEntry(rootPair.Key, d));
                }
                else
                {
                    return rootPair.Value
                        .Select(d => new WordEntry(rootPair.Key, d));
                }
            });

        public IEnumerable<WordEntry> AllEntries => EntriesByRoot.SelectMany(pair => pair.Value.Select(d => new WordEntry(pair.Key, d)));

        public IEnumerable<string> RootWords => EntriesByRoot.Keys;

        public bool HasEntries => EntriesByRoot.Count != 0;

        public WordEntrySet this[string rootWord] => FindEntriesByRootWord(rootWord);

        public bool Check(string word) => new QueryCheck(word, this).Check();

        public SpellCheckResult CheckDetails(string word) => new QueryCheck(word, this).CheckDetails();

        public IEnumerable<string> Suggest(string word) => new QuerySuggest(word, this).Suggest();

        public WordEntrySet FindEntriesByRootWord(string rootWord)
        {
            var details = FindEntryDetailsByRootWord(rootWord);
            return details.Length == 0
                ? WordEntrySet.Empty
                : WordEntrySet.Create(rootWord, details);
        }

        internal WordEntry FindFirstEntryByRootWord(string rootWord)
        {
            var details = FindEntryDetailsByRootWord(rootWord);
            return details.Length == 0
                ? null
                : new WordEntry(rootWord, details[0]);
        }

        internal WordEntryDetail[] FindEntryDetailsByRootWord(string rootWord)
        {
            return (rootWord == null || !EntriesByRoot.TryGetValue(rootWord, out WordEntryDetail[] details))
                ? ArrayEx<WordEntryDetail>.Empty
                : details;
        }

        internal WordEntryDetail FindFirstEntryDetailByRootWord(string rootWord)
        {
            var details = FindEntryDetailsByRootWord(rootWord);
            return details.Length == 0
                ? null
                : details[0];
        }

        internal bool ContainsEntriesForRootWord(string rootWord)
        {
            return rootWord != null && EntriesByRoot.ContainsKey(rootWord);
        }
    }
}
