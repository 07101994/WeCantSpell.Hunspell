using System;
using System.Collections;
using System.Collections.Generic;
using WeCantSpell.Hunspell.Infrastructure;

namespace WeCantSpell.Hunspell
{
    sealed class WordEntryDictionary : IEnumerable<KeyValuePair<ReadOnlyMemory<char>, WordEntryDetail[]>>
    {
        public WordEntryDictionary()
        {
            var comparer = new TextMemoryComparer();
            Trie = new StringTrie<WordEntryDetail[]>();
            Hash = new Dictionary<ReadOnlyMemory<char>, WordEntryDetail[]>(comparer);
        }

        StringTrie<WordEntryDetail[]> Trie;

        Dictionary<ReadOnlyMemory<char>, WordEntryDetail[]> Hash;

        public bool IsEmpty => Hash.Count == 0;

        public Dictionary<ReadOnlyMemory<char>, WordEntryDetail[]>.KeyCollection Keys => Hash.Keys;

        public Dictionary<ReadOnlyMemory<char>, WordEntryDetail[]>.ValueCollection Values => Hash.Values;

        public WordEntryDetail[] this[ReadOnlyMemory<char> key]
        {
            get => Hash[key];
            set
            {
                Hash[key] = value;
                Trie[key] = value;
            }
        }

        public WordEntryDetail[] this[string key]
        {
            get => this[key.AsMemory()];
            set { this[key.AsMemory()] = value; }
        }

        [Obsolete]
        public WordEntryDetail[] this[ReadOnlySpan<char> key]
        {
            get => this[key.ToString()];
            set { this[key.ToString()] = value; }
        }

        public void Add(ReadOnlyMemory<char> key, WordEntryDetail[] value)
        {
            Hash.Add(key, value);
            Trie.Add(key, value);
        }

        public void Add(string key, WordEntryDetail[] value) => Add(key.AsMemory(), value);

        [Obsolete]
        public void Add(ReadOnlySpan<char> key, WordEntryDetail[] value) => Add(key.ToString(), value);

        public bool TryGetValue(ReadOnlyMemory<char> key, out WordEntryDetail[] value) => Hash.TryGetValue(key, out value);

        public bool TryGetValue(string key, out WordEntryDetail[] value) => TryGetValue(key.AsMemory(), out value);

        public bool ContainsKey(ReadOnlyMemory<char> key) => Hash.ContainsKey(key);

        public bool ContainsKey(string key) => ContainsKey(key.AsMemory());

        [Obsolete]
        public bool ContainsKey(ReadOnlySpan<char> key) => Trie.ContainsKey(key);

        public Enumerator GetEnumerator() => GetEnumerator(maxDepth: -1);

        public Enumerator GetEnumerator(int maxDepth) => new Enumerator(this, maxDepth);

        IEnumerator<KeyValuePair<ReadOnlyMemory<char>, WordEntryDetail[]>> IEnumerable<KeyValuePair<ReadOnlyMemory<char>, WordEntryDetail[]>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<KeyValuePair<ReadOnlyMemory<char>, WordEntryDetail[]>>
        {
            public Enumerator(WordEntryDictionary dictionary, int maxDepth)
            {
                enumerator = dictionary.Hash.GetEnumerator();
                this.maxDepth = maxDepth;
            }

            Dictionary<ReadOnlyMemory<char>, WordEntryDetail[]>.Enumerator enumerator;

            readonly int maxDepth;

            public KeyValuePair<ReadOnlyMemory<char>, WordEntryDetail[]> Current => enumerator.Current;

            object IEnumerator.Current => throw new NotImplementedException();

            public bool MoveNext()
            {
                if (maxDepth == -1)
                {
                    return enumerator.MoveNext();
                }
                else
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.Key.Length <= maxDepth)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            public void Dispose()
            {
                enumerator.Dispose();
            }

            void IEnumerator.Reset()
            {
                ((IEnumerator)enumerator).Reset();
            }

        }
    }
}
