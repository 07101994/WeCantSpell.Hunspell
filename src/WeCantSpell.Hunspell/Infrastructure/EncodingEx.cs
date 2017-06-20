﻿using System;
using System.Text;

namespace WeCantSpell.Hunspell.Infrastructure
{
    internal static class EncodingEx
    {
        public static readonly Encoding DefaultReadEncoding = Encoding.UTF8;

        public static Encoding GetEncodingByName(string encodingName) =>
            GetEncodingByName(new StringSlice(encodingName));

        public static Encoding GetEncodingByName(StringSlice encodingName)
        {
            if (encodingName.IsEmpty)
            {
                return null;
            }

            if (encodingName.Equals("UTF-8", StringComparison.OrdinalIgnoreCase) || encodingName.Equals("UTF8", StringComparison.OrdinalIgnoreCase))
            {
                return Encoding.UTF8;
            }

            var encodingNameString = encodingName.ToString();
            try
            {
                return Encoding.GetEncoding(encodingNameString);
            }
            catch (ArgumentException)
            {
                return GetEncodingByAlternateNames(encodingNameString);
            }
        }

        private static Encoding GetEncodingByAlternateNames(string encodingName)
        {
            var spaceIndex = encodingName.IndexOf(' ');
            if (spaceIndex > 0)
            {
                return GetEncodingByName(encodingName.Subslice(0, spaceIndex));
            }

            if (encodingName.Length >= 4 && encodingName.StartsWith("ISO") && encodingName[3] != '-')
            {
                return GetEncodingByName(encodingName.Insert(3, "-"));
            }

            return null;
        }
    }
}
