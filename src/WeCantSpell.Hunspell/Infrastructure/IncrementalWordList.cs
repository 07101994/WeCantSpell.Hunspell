using System;
using System.Collections.Generic;

namespace WeCantSpell.Hunspell.Infrastructure
{
    class IncrementalWordList
    {
        public IncrementalWordList()
        {
            Words = new List<WordEntryDetail>();
            WNum = 0;
        }

        private IncrementalWordList(List<WordEntryDetail> words, int wNum)
        {
#if DEBUG
            if (words == null) throw new ArgumentNullException(nameof(words));
            if (WNum < 0) throw new ArgumentOutOfRangeException(nameof(wNum));
#endif
            Words = words;
            WNum = wNum;
        }

        public readonly List<WordEntryDetail> Words;

        public readonly int WNum;

        public void SetCurrent(WordEntryDetail value)
        {
            if (WNum == Words.Count)
            {
                Words.Add(value);
            }
            else if (WNum < Words.Count)
            {
                Words[WNum] = value;
            }
            else
            {
                AppendForCurrent(value);
            }
        }

        public void ClearCurrent()
        {
            if (WNum < Words.Count)
            {
                Words[WNum] = default;
            }
        }

        public bool CheckIfCurrentIsNotNull() => CheckIfNotNull(WNum);

        public bool CheckIfNextIsNotNull() => CheckIfNotNull(WNum + 1);

        public bool ContainsFlagAt(int wordIndex, FlagValue flag)
        {
#if DEBUG
            if (wordIndex < 0) throw new ArgumentOutOfRangeException(nameof(wordIndex));
#endif

            if (wordIndex < Words.Count)
            {
                var detail = Words[wordIndex];
                if (detail != null)
                {
                    return detail.ContainsFlag(flag);
                }
            }

            return false;
        }

        public IncrementalWordList CreateIncremented() => new IncrementalWordList(Words, WNum + 1);

        private bool CheckIfNotNull(int index) => (index < Words.Count) && Words[index] != null;

        private void AppendForCurrent(WordEntryDetail value)
        {
            for (var i = WNum - Words.Count; i > 0; i--)
            {
                Words.Add(default);
            }

            Words.Add(value);
        }
    }
}
