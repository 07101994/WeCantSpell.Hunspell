﻿using System;
using System.Globalization;

#if !NO_INLINE
using System.Runtime.CompilerServices;
#endif

namespace WeCantSpell.Hunspell.Infrastructure
{
    static class IntEx
    {
        private static readonly NumberFormatInfo InvariantNumberFormat = CultureInfo.InvariantCulture.NumberFormat;

        public static bool TryParseInvariant(string text, out int value) =>
            int.TryParse(text, NumberStyles.Integer, InvariantNumberFormat, out value);

        public static bool TryParseInvariant(ReadOnlySpan<char> text, out int value) =>
            text.Length == 1
                ? TryParseInvariant(text[0], out value)
                : TryParseInvariant(text.ToString(), out value);

        public static int? TryParseInvariant(ReadOnlySpan<char> text) =>
            TryParseInvariant(text, out int value) ? (int?)value : null;

#if !NO_INLINE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool InversePostfixIncrement(ref bool b)
        {
            if (b)
            {
                return false;
            }
            else
            {
                b = true;
                return true;
            }
        }

        private static bool TryParseInvariant(char character, out int value)
        {
            if (character >= '0' && character <= '9')
            {
                value = character - '0';
                return true;
            }

            value = default;
            return false;
        }
    }
}
