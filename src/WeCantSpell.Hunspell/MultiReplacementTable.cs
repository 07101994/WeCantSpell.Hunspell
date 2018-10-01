using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WeCantSpell.Hunspell.Infrastructure;

namespace WeCantSpell.Hunspell
{
    public class MultiReplacementTable : IReadOnlyDictionary<string, MultiReplacementEntry>
    {
        public static readonly MultiReplacementTable Empty = new MultiReplacementTable(new Dictionary<string, MultiReplacementEntry>(0));

        public static MultiReplacementTable Create(IEnumerable<KeyValuePair<string, MultiReplacementEntry>> replacements) =>
            TakeDictionary(replacements?.ToDictionary(s => s.Key ?? throw new ArgumentException(nameof(replacements)), s => s.Value));

        internal static MultiReplacementTable TakeDictionary(Dictionary<string, MultiReplacementEntry> replacements) =>
            replacements == null || replacements.Count == 0 ? Empty : new MultiReplacementTable(replacements);

        private MultiReplacementTable(Dictionary<string, MultiReplacementEntry> replacements)
        {
            this.replacements = replacements;


            using (var keyEnumerator = replacements.Keys.GetEnumerator())
            {
                int keyLength;
                if (!keyEnumerator.MoveNext())
                {
                    minKeyLength = 0;
                    maxKeyLength = 0;
                }
                else
                {
                    keyLength = keyEnumerator.Current.Length;
                    minKeyLength = keyLength;
                    maxKeyLength = keyLength;
                }

                while (keyEnumerator.MoveNext())
                {
                    keyLength = keyEnumerator.Current.Length;
                    if (keyLength < minKeyLength)
                    {
                        minKeyLength = keyLength;
                    }
                    else if (maxKeyLength < keyLength)
                    {
                        maxKeyLength = keyLength;
                    }
                }
            }
        }

        private readonly Dictionary<string, MultiReplacementEntry> replacements;

        private readonly int minKeyLength;

        private readonly int maxKeyLength;

        public MultiReplacementEntry this[string key] => replacements[key];

        public int Count => replacements.Count;

        public bool HasReplacements => replacements.Count != 0;

        public IEnumerable<string> Keys => replacements.Keys;

        public IEnumerable<MultiReplacementEntry> Values => replacements.Values;

        public bool ContainsKey(string key) => replacements.ContainsKey(key);

        public bool TryGetValue(string key, out MultiReplacementEntry value) => replacements.TryGetValue(key, out value);

        internal bool TryConvert(string text, out string converted)
        {
            if (!string.IsNullOrEmpty(text))
            {
                var appliedConversion = false;
                var convertedBuilder = StringBuilderPool.Get(text.Length);
                for (var i = 0; i < text.Length; i++)
                {
                    var replacementEntry = FindLargestMatchingConversion(text.AsSpan(i));
                    if (replacementEntry != null)
                    {
                        var replacementText = replacementEntry.ExtractReplacementText(text.Length - i, i == 0);
                        if (!string.IsNullOrEmpty(replacementText))
                        {
                            convertedBuilder.Append(replacementText);
                            i += replacementEntry.Pattern.Length - 1;
                            appliedConversion = true;
                            continue;
                        }
                    }

                    convertedBuilder.Append(text[i]);
                }

                if (appliedConversion)
                {
                    converted = StringBuilderPool.GetStringAndReturn(convertedBuilder);
                    return true;
                }

                StringBuilderPool.Return(convertedBuilder);
            }

            converted = text;
            return false;
        }

        /// <summary>
        /// Finds a conversion matching the longest version of the given <paramref name="text"/> from the left.
        /// </summary>
        /// <param name="text">The text to find a matching input conversion for.</param>
        /// <returns>The best matching input conversion.</returns>
        /// <seealso cref="MultiReplacementEntry"/>
        internal MultiReplacementEntry FindLargestMatchingConversion(ReadOnlySpan<char> text)
        {
            var minIteration = Math.Max(minKeyLength, 1);
            for (var searchLength = Math.Min(maxKeyLength, text.Length); searchLength >= minIteration; searchLength--)
            {
                if (replacements.TryGetValue(text.Slice(0, searchLength).ToString(), out var entry))
                {
                    return entry;
                }
            }

            return null;
        }

        internal Dictionary<string, MultiReplacementEntry>.Enumerator GetEnumerator() => replacements.GetEnumerator();

        IEnumerator<KeyValuePair<string, MultiReplacementEntry>> IEnumerable<KeyValuePair<string, MultiReplacementEntry>>.GetEnumerator() => replacements.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => replacements.GetEnumerator();
    }
}
