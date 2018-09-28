using WeCantSpell.Hunspell.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WeCantSpell.Hunspell
{
    public struct CharacterCondition :
        IEquatable<CharacterCondition>
    {
        public static readonly CharacterCondition AllowNone = new CharacterCondition(CharacterSet.Empty, false);

        public static readonly CharacterCondition AllowAny = new CharacterCondition(CharacterSet.Empty, true);

        internal static CharacterCondition TakeArray(char[] characters, bool restricted) =>
            new CharacterCondition(characters, restricted);

        public static CharacterCondition Create(char character, bool restricted) =>
            new CharacterCondition(character, restricted);

        public static CharacterCondition Create(IEnumerable<char> characters, bool restricted) =>
            TakeArray(characters == null ? ArrayEx<char>.Empty : characters.ToArray(), restricted);

        public static CharacterConditionGroup Parse(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return CharacterConditionGroup.Empty;
            }

            var conditions = new List<CharacterCondition>(text.Length);

            for (int searchIndex = 0; searchIndex < text.Length; searchIndex++)
            {
                var c = text[searchIndex];
                if (c != '[')
                {
                    conditions.Add(ParseSingle(c));
                    continue;
                }

                var endIndex = searchIndex + 1;
                for (; endIndex < text.Length && text[endIndex] != ']'; endIndex++) ;

                if (endIndex < text.Length)
                {
                    conditions.Add(ParseFromClass(text.AsSpan(searchIndex + 1, endIndex - searchIndex - 1)));
                    searchIndex = endIndex;
                    continue;
                }
                else
                {
                    return CharacterConditionGroup.Empty; // Invalid sequence detected
                }
            }

            return CharacterConditionGroup.Create(conditions);
        }

        private static CharacterCondition ParseSingle(char singleChar) =>
            singleChar == '.'
                ? AllowAny
                : Create(singleChar, false);

        private static CharacterCondition ParseFromClass(ReadOnlySpan<char> text)
        {
            if (text.IsEmpty)
            {
                return AllowNone;
            }

            var restricted = text[0] == '^';
            if (text.Length == 1)
            {
                return restricted ? AllowAny : Create(text[0], false);
            }

            if (restricted)
            {
                return text.Length == 2
                    ? Create(text[1], true)
                    : TakeArray(text.Slice(1).ToArray(), true);
            }

            return TakeArray(text.ToArray(), false);
        }

        public CharacterCondition(CharacterSet characters, bool restricted)
        {
            Characters = characters;
            Restricted = restricted;
        }

        private CharacterCondition(char character, bool restricted)
            : this(CharacterSet.Create(character), restricted)
        {
        }

        private CharacterCondition(char[] characters, bool restricted)
            : this(CharacterSet.TakeArray(characters), restricted)
        {
        }

        public CharacterSet Characters { get; }

        /// <summary>
        /// Indicates that the <see cref="Characters"/> are restricted when <c>true</c>.
        /// </summary>
        public bool Restricted { get; }

        public bool IsMatch(char c) =>
            (Characters != null && Characters.Contains(c)) ^ Restricted;

        public bool AllowsAny => Restricted && (Characters == null || Characters.Count == 0);

        public bool PermitsSingleCharacter => !Restricted && Characters != null && Characters.Count == 1;

        public string GetEncoded()
        {
            if (AllowsAny)
            {
                return ".";
            }

            if (PermitsSingleCharacter)
            {
                return Characters[0].ToString();
            }

            var lettersText = (Characters == null || Characters.Count == 0)
                ? string.Empty
                : Characters.GetCharactersAsString();

            return (Restricted ? "[^" : "[") + lettersText + "]";
        }

        public override string ToString() => GetEncoded();

        public bool Equals(CharacterCondition other) =>
            Restricted == other.Restricted && CharacterSet.Comparer.SequenceEquals(Characters, other.Characters);

        public override bool Equals(object obj) => obj is CharacterCondition cc && Equals(cc);

        public override int GetHashCode() => CharacterSet.Comparer.Default.GetHashCode(Characters);
    }
}
